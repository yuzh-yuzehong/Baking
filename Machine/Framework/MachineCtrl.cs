using HelperLibrary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SystemControlLibrary;
using static Machine.MesBill;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    public class MachineCtrl : ControlInterface
    {
        #region // 枚举：模组数据，报警列表

        private enum MsgID
        {
            Start = ModuleMsgID.SystemStartID,
            DoorAlarm_0,
            DoorAlarm_End = DoorAlarm_0 + 9,
            OnloadSysRunning,
            TransferSysRunning,
            OffloadSysRunning,
            SysRunningEnd = OnloadSysRunning + 9,
            ModuleDisconnect,
            ModuleDisconnectEnd = ModuleDisconnect + 9,
            RobotRun,
            RobotRunningEnd = RobotRun + 9,
            AirPressureAlm,
            HeartbeatErr,
            UnbindPltErr,
            MySqlDisconnect,
        }
        #endregion

        #region // 字段，属性

        #region // IO

        // 设备按钮
        private int[] IStartButton;             // 输入：启动按钮
        private int[] IStopButton;              // 输入：停止按钮
        private int[] IEStopButton;             // 输入：急停按钮
        private int[] IResetButton;             // 输入：复位按钮
        private int[] OStartLed;                // 输出：启动按钮LED
        private int[] OStopLed;                 // 输出：停止按钮LED
        private int[] OResetLed;                // 输出：复位按钮LED
        
        // 灯塔
        private int[] OLightTowerRed;           // 输出：灯塔：红
        private int[] OLightTowerYellow;        // 输出：灯塔：黄
        private int[] OLightTowerGreen;         // 输出：灯塔：绿
        private int[] OLightTowerBuzzer;        // 输出：灯塔：蜂鸣器
        
        // 安全门
        private int[] ISafeDoorState;           // 安全门状态
        private int[] ISafeDoorOpenBtn;         // 安全门开门按钮
        private int[] ISafeDoorCloseBtn;        // 安全门关门按钮
        private int[] OSafeDoorOpenLed;         // 安全门开门按钮LED
        private int[] OSafeDoorCloseLed;        // 安全门关门按钮LED
        private int[] OSafeDoorUnlock;          // 安全门解锁
        private string[] SafeDoorStopModule;    // 安全门打开时停止运行模组
        private int[] SafeDoorDelay;            // 安全门打开延时：毫秒ms

        // 气压报警
        private int IAirPressureAlarm;          // 气压报警

        #endregion

        #region // ModuleEx.cfg配置

        public string MachineName { get; private set; }        // 系统类名
        public int MachineID { get; private set; }             // 设备ID
        public int HalfDryingOvens { get; private set; }       // 一半的干燥炉数量：用以绘图
        public int LineID { get; private set; }                // 线体ID
        public bool AlarmStopMC { get; private set; }          // 报警是否整线停机

        private string machineServerIP;                        // 服务端IP
        private int machineServerPort;                         // 服务端Port
        private List<string> machineClientIP;                  // 客户端IP
        private List<int> machineClientPort;                   // 客户端Port

        #endregion

        #region // 参数

        public int PalletMaxRow { get; private set; }          // 夹具最大行，只能为奇数：（0<X<PalletRowCol.MaxRow）
        public int PalletMaxCol { get; private set; }          // 夹具最大列：（0<X<PalletRowCol.MaxCol）
        public int BakingMaxCount { get; private set; }        // 烘烤最大次数：超过则置NG
        public bool DataRecover { get; set; }                  // 数据恢复
        public bool UpdataMes { get; set; }                    // 上传MES
        public string ProductionFilePath { get; private set; } // 生产信息文件路径
        public bool OnloadClear { get; private set; }          // 上料清尾料

        private int productionFileStorageLife;                 // 生产信息文件存储时间：天
        private string mcLogFilePath;                          // Log文件相对路径文件夹
        private int mcLogFileSize;                             // Log文件大小：兆M
        private int mcLogFileStorageLife;                      // Log文件存储时间：天

        #endregion

        #region // 模组数据

        public bool SafeDoorState { get; private set; }               // 安全门状态：true：已打开；false：关闭
        public List<RunProcess> ListRuns { get; private set; }        // 运行模组
        public DataBaseRecord dbRecord;                               // 数据库记录
        public string OperaterID;                           // 操作者ID
        public bool MaintenanceLock;                        // 维护锁屏

        private static MachineCtrl machineCtrl;             // 设备
        private List<string> listInput;                     // 输入点
        private List<string> listOutput;                    // 输出点
        private List<string> listMotor;                     // 电机

        private Dictionary<string, ParameterFormula> insertParameterList;      // 模组中插入的参数集：<参数关键字key, 参数样式>
        private Dictionary<string, ParameterFormula> dataBaseParameterList;    // 数据库中已保存的参数
        private PropertyManage parameterProperty;           // 参数管理类

        private bool autoConnectCSState;                    // 客户端自动重连服务端状态
        private DateTime autoConnectCSTime;                 // 客户端自动重连服务端计时
        private ModuleServer machineServer;                 // 服务端
        private List<ModuleClient> machineClient;           // 客户端
        private Dictionary<int, ModuleSocketData> moduleSocketData;   // 网络模组数据

        private List<Task> taskList;                        // MachineCtrl中创建的所有Task
        private List<int> MsgList;                          // 非阻塞报警对话框弹出列表
        private bool hasMsgBox;                             // 模组是否有enum MESSAGE_TYPE任意一项信息弹窗：当自动运行有弹窗时，请置为TRUE

        private bool monitorRunning;                        // 监视线程运行中
        private bool resetButtonOff;                        // 复位按钮OFF状态
        private bool startButtonOff;                        // 复位按钮OFF状态
        private DateTime setTowerStart;                     // 设置灯塔计时
        private MCState mcOldState;                         // 设备原状态

        // MES项
        private DateTime mySqlCheckTime;                    // MySql连接状态检查计时
        private DateTime heartbeatTime;                     // Mes心跳计时
        private int heartbeatCount;                         // Mes无心跳计次
        private HttpClient httpClient;                      // MES交互

        private Object mesFileLock;                 // Mes通讯文件锁
        public bool FingerCheckScan;                        //条码规则是否启用

        private static bool PullInLogHasChanged;
        private static string PullInExCsvFilePath;

        private static bool OutLogHasChanged;
        private static string OutExCsvFilePath;


        #endregion

        #endregion

        #region // 设备初始化

        public MachineCtrl()
        {
            this.ListRuns = new List<RunProcess>();
            this.listInput = new List<string>();
            this.listOutput = new List<string>();
            this.listMotor = new List<string>();

            this.DataRecover = true;
            this.UpdataMes = !Def.IsNoHardware();
            this.dbRecord = new DataBaseRecord();
            this.moduleSocketData = new Dictionary<int, ModuleSocketData>();
            this.insertParameterList = new Dictionary<string, ParameterFormula>();
            this.dataBaseParameterList = new Dictionary<string, ParameterFormula>();
            this.parameterProperty = new PropertyManage();
            this.monitorRunning = false;
            this.setTowerStart = DateTime.Now;
            this.OperaterID = "";
            this.MaintenanceLock = false;
            this.httpClient = new HttpClient();
            this.mcOldState = MCState.MCInvalidState;
            this.MsgList = new List<int>();
            this.hasMsgBox = false;
            this.autoConnectCSState = false;
            this.autoConnectCSTime = DateTime.Now;
            this.mesFileLock = new object();

            InitParameter();
            // 添加参数
            string description;
            description = $"夹具最大行，只能为偶数：（0＜X≤{(int)PalletRowCol.MaxRow}）";
            InsertVoidParameter("PalletMaxRow", "夹具最大行", description, PalletMaxRow, RecordType.RECORD_INT, ParameterLevel.PL_IDLE_ADMIN);
            description = $"夹具最大列：（0＜X≤{(int)PalletRowCol.MaxCol}）";
            InsertVoidParameter("PalletMaxCol", "夹具最大列", description, PalletMaxCol, RecordType.RECORD_INT, ParameterLevel.PL_IDLE_ADMIN);
            InsertVoidParameter("BakingMaxCount", "烘烤最大次数", "烘烤最大次数：未超过时则重新烘烤，超过则置NG不再继续烘烤", BakingMaxCount, RecordType.RECORD_INT);
            //InsertVoidParameter("mcLogFilePath", "Log存储路径", "Log存储文件的相对路径", mcLogFilePath, RecordType.RECORD_STRING);
            InsertVoidParameter("mcLogFileSize", "Log文件大小", "Log文件的大小：兆(M)", mcLogFileSize, RecordType.RECORD_INT);
            InsertVoidParameter("mcLogFileStorageLife", "Log存储时间", "Log存储时间：天；超时后删除", mcLogFileStorageLife, RecordType.RECORD_INT);
            InsertVoidParameter("ProductionFilePath", "生产文件路径", "生产信息文件的存储完整路径", ProductionFilePath, RecordType.RECORD_STRING);
            InsertVoidParameter("productionFileStorageLife", "生产文件存储", "生产信息文件存储时间：天", productionFileStorageLife, RecordType.RECORD_INT);
            InsertVoidParameter("OnloadClear", "上料清尾料", "上料清尾料：上料不再取新料，且将暂存电芯放入当前上料夹具", OnloadClear, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_OPER);

        }

        public static MachineCtrl GetInstance()
        {
            if(null == machineCtrl)
            {
                machineCtrl = new MachineCtrl();
            }
            return machineCtrl;
        }

        /// <summary>
        /// 执行与释放或重置非托管资源相关的应用程序定义的任务。
        /// </summary>
        public void Dispose()
        {
            TotalData.WriteTotalData();
            ReleaseThread();
            this.dbRecord.CloseDataBase();
            DataBaseLog.CloseDataBase();
            MesOperateMySql.CloseMesMySql();
            SetTowerButton(MCState.NumMCState);
        }

        public bool Initialize(IntPtr hMsgWnd)
        {
            string section, name;

            #region // input
            for(int index = 0; index < int.MaxValue; index++)
            {
                section = "INPUT" + index;
                name = IniFile.ReadString(section, "Num", "", Def.GetAbsPathName(Def.InputCfg));
                if("" == name)
                {
                    break;
                }
                this.listInput.Add(name);
            }
            #endregion

            #region // output
            for(int index = 0; index < int.MaxValue; index++)
            {
                section = "OUTPUT" + index;
                name = IniFile.ReadString(section, "Num", "", Def.GetAbsPathName(Def.OutputCfg));
                if("" == name)
                {
                    break;
                }
                this.listOutput.Add(name);
            }
            #endregion

            #region // motor
            for(int index = 0; index < int.MaxValue; index++)
            {
                section = string.Format("{0}Motor{1}.cfg", Def.MotorCfgFolder, index);
                name = "Motor" + index;
                if(!File.Exists(section))
                {
                    break;
                }
                this.listMotor.Add(name);
            }
            #endregion

            // 删除已有模组信息，重新创建
            if (File.Exists(Def.ModuleCfg))
            {
                File.Delete(Def.ModuleCfg);
            }

            if(!base.Initialize(hMsgWnd, listMotor.Count, listInput.Count, listOutput.Count))
            {
                return false;
            }

            #region // 电机点位初始化

            int num = DeviceManager.GetMotorManager().MotorsTotal;
            for(int index = 0; index < num; index++)
            {
                if (!LoadMotorLocation(index))
                {
                    return false;
                }
            }
            #endregion

            return true;
        }

        protected override bool InitializeRunThreads(IntPtr hMsgWnd)
        {
            Trace.Assert(null == this.RunsCtrl, "ControlInterface.RunsCtrl is null.");

            #region // 系统配置

            this.AlarmStopMC = IniFile.ReadBool("Run", "AlarmStopMC", false, Def.GetAbsPathName(Def.MachineCfg));
            this.MachineName = IniFile.ReadString("Modules", "MachineName", "System", Def.GetAbsPathName(Def.ModuleExCfg));
            this.MachineID = IniFile.ReadInt("Modules", "MachineID", -1, Def.GetAbsPathName(Def.ModuleExCfg));
            if(this.MachineID < 0)
            {
                ShowMsgBox.ShowDialog("设备编号MachineID未配置，请在ModuleEx.cfg中配置", MessageType.MsgAlarm);
            }
            this.HalfDryingOvens = IniFile.ReadInt("Modules", "HalfDryingOvens", 0, Def.GetAbsPathName(Def.ModuleExCfg));
            this.LineID = IniFile.ReadInt("Modules", "LineID", -1, Def.GetAbsPathName(Def.ModuleExCfg));
            if(this.LineID < 0)
            {
                ShowMsgBox.ShowDialog("线体编号LineID未配置，请在ModuleEx.cfg中配置", MessageType.MsgAlarm);
            }

            #endregion

            #region // 检查文件路径

            // 运行数据路径
            if(!Def.CreateFilePath(Def.GetAbsPathName(Def.RunDataFolder))
                || !Def.CreateFilePath(Def.GetAbsPathName(Def.RunDataBakFolder)))
            {
                Trace.Assert(false, "CreateFilePath( " + Def.GetAbsPathName(Def.RunDataFolder) + " ) fail.");
                return false;
            }
            // Log文件路径
            if(!Def.CreateFilePath(Def.GetAbsPathName(Def.MachineLogFolder))
                || !Def.CreateFilePath(Def.GetAbsPathName(Def.SystemLogFolder)))
            {
                Trace.Assert(false, "CreateFilePath( " + Def.GetAbsPathName(Def.MachineLogFolder) + " ) fail.");
                return false;
            }

            Def.SetFileInfo(mcLogFilePath, mcLogFileSize, mcLogFileStorageLife);

            #endregion

            #region // 检查数据库表

            for(TableType tab = TableType.TABLE_USER; tab < TableType.TABLE_END; tab++)
            {
                if(!this.dbRecord.CheckTable(tab) && !this.dbRecord.CreateTable(tab))
                {
                    Trace.Assert(false, "DataBaseRecord." + tab + "表不存在，请检查");
                    return false;
                }
            }
            #endregion

            #region // 打开Log数据库

            if (!DataBaseLog.OpenDataBase(Def.GetAbsPathName(Def.MachineMdb), ""))
            {
                ShowMsgBox.ShowDialog("Log数据库打开失败，继续操作将不能保存Log信息", MessageType.MsgAlarm);
            }
            for(DataBaseLog.LogTableType tab = 0; tab < DataBaseLog.LogTableType.End; tab++)
            {
                if(!DataBaseLog.CheckTable(tab) && !DataBaseLog.CreateTable(tab))
                {
                    Trace.Assert(false, "DataBaseLog." + tab + "表不存在，请检查");
                    return false;
                }
            }
            #endregion

            #region // MES配置参数，资源班次信息

            for(MesInterface mes = 0; mes < MesInterface.End; mes++)
            {
                MesDefine.ReadConfig(mes);
            }

            MesResources.ReadConfig();
            OperationShifts.ReadConfig();
            FTPDefine.ReadConfig();

            #endregion

            #region // 打开MES的MySql服务

            if(!MesOperateMySql.OpenMesMySql())
            {
                return false;
            }
            #endregion

            #region // 创建模组

            IniFile.WriteString("Module0", "Name", this.MachineName, Def.GetAbsPathName(Def.ModuleCfg));

            string strSection, strKey, strClass;
            strSection = strKey = strClass = "";
            RunProcess runModule = null;
            Dictionary<int, string> checkRunID = new Dictionary<int, string>();
            for(int index = 0; index < int.MaxValue; index++)
            {
                strKey = "Module" + index;
                strSection = IniFile.ReadString("Modules", strKey, "", Def.GetAbsPathName(Def.ModuleExCfg));
                if (string.IsNullOrEmpty(strSection))
                {
                    break;
                }
                strClass = IniFile.ReadString(strSection, "Class", "", Def.GetAbsPathName(Def.ModuleExCfg));
                int runID = index;

                if("RunProcessOffloadBattery" == strClass)
                {
                    runID = (int)RunID.OffloadBattery;
                    runModule = new RunProcessOffloadBattery(runID);
                }
                else if("RunProcessManualOperate" == strClass)
                {
                    runID = (int)RunID.ManualOperate;
                    runModule = new RunProcessManualOperate(runID);
                }
                else if("RunProcessPalletBuffer" == strClass)
                {
                    runID = (int)RunID.PalletBuffer;
                    runModule = new RunProcessPalletBuffer(runID);
                }
                else if("RunProcessOffloadLine" == strClass)
                {
                    runID = (int)RunID.OffloadLine;
                    runModule = new RunProcessOffloadLine(runID);
                }
                else if("RunProcessOffloadNG" == strClass)
                {
                    runID = (int)RunID.OffloadNG;
                    runModule = new RunProcessOffloadNG(runID);
                }
                else if("RunProcessOffloadDetectFake" == strClass)
                {
                    runID = (int)RunID.OffloadDetect;
                    runModule = new RunProcessOffloadDetectFake(runID);
                }
                else if("RunProcessCoolingSystem" == strClass)
                {
                    runID = (int)RunID.CoolingSystem;
                    runModule = new RunProcessCoolingSystem(runID);
                }
                else if("RunProcessCoolingOffload" == strClass)
                {
                    runID = (int)RunID.CoolingOffload;
                    runModule = new RunProcessCoolingOffload(runID);
                }
                else if("RunProcessRobotTransfer" == strClass)
                {
                    runID = (int)RunID.Transfer;
                    runModule = new RunProcessRobotTransfer(runID);
                }
                else if("RunProcessOnloadRobot" == strClass)
                {
                    runID = (int)RunID.OnloadRobot;
                    runModule = new RunProcessOnloadRobot(runID);
                }
                else if("RunProcessOnloadFake" == strClass)
                {
                    runID = (int)RunID.OnloadFake;
                    runModule = new RunProcessOnloadFake(runID);
                }
                else if("RunProcessOnloadNG" == strClass)
                {
                    runID = (int)RunID.OnloadNG;
                    runModule = new RunProcessOnloadNG(runID);
                }
                else if("RunProcessOnloadLine" == strClass)
                {
                    runID = (int)RunID.OnloadLine;
                    runModule = new RunProcessOnloadLine(runID);
                }
                else if("RunProcessOnloadScan" == strClass)
                {
                    runID = (int)RunID.OnloadScan;
                    runModule = new RunProcessOnloadScan(runID);
                }
                else if("RunProcessOnloadRecv" == strClass)
                {
                    runID = (int)RunID.OnloadRecv;
                    runModule = new RunProcessOnloadRecv(runID);
                }
                else if("DryingOven" == strClass)
                {
                    int id = IniFile.ReadInt(strSection, "DryingOvenID", 0, Def.GetAbsPathName(Def.ModuleExCfg));
                    runID = (int)RunID.DryOven0 + id;
                    runModule = new RunProcessDryingOven(runID);
                }
                else
                {
                    runModule = new RunProcess(runID);
                }
                if(!checkRunID.ContainsKey(runID))
                {
                    checkRunID.Add(runID, strSection);
                }
                else
                {
                    ShowMsgBox.ShowDialog((strSection + "模组RunID = " + runID + "已存在，请检查！"), MessageType.MsgAlarm);
                    return false;
                }

                ListRuns.Add(runModule);
                List<int> inputs, outputs, motors;
                runModule.AlarmStopMC(this.AlarmStopMC);
                runModule.InitializeConfig(strSection);
                runModule.GetHardwareConfig(out inputs, out outputs, out motors);
                WriteModuleCfg(index + 1, strSection, inputs, outputs, motors);
            }
            IniFile.WriteInt("Modules", "CountModules", ListRuns.Count + 1, Def.GetAbsPathName(Def.ModuleCfg));
            #endregion

            #region // 创建模组完成后，读取该模组的关联模组
            foreach(RunProcess run in this.ListRuns)
            {
                // 有硬件运行时不能空运行
                if (!Def.IsNoHardware())
                {
                    run.DryRun = false;
                }
                run.ReadRelatedModule();
            }
            #endregion

            #region // 创建RunCtrl

            this.RunsCtrl = new RunCtrl();
            if(null == this.RunsCtrl)
            {
                ShowMsgBox.ShowDialog("创建RunCtrl线程失败", MessageType.MsgAlarm);
                return false;
            }

            if(!this.RunsCtrl.Initialize(ListRuns.Count, (this.ListRuns.ConvertAll<RunEx>(tmp => tmp as RunEx)), (new ManualDebugCheck(this.ListRuns.Count)), hMsgWnd))
            {
                ShowMsgBox.ShowDialog("RunCtrl线程初始化失败", MessageType.MsgAlarm);
                return false;
            }
            // 设置启动前检查，及停止后操作委托
            this.RunsCtrl.beforeStart = BeforeStart;
            this.RunsCtrl.afterStop = AfterStop;
            #endregion

            #region // 读取系统IO，系统设置参数，统计数据

            ReadSystemIO();
            ReadParameter();
            
            // 设备IO读取完成后不再使用，删除输入输出及电机列表
            listInput.Clear();
            listInput = null;
            listOutput.Clear();
            listOutput = null;
            listMotor.Clear();
            listMotor = null;

            // 统计数据
            TotalData.ReadTotalData();

            #endregion

            #region // 模组服务器及客户端

            strKey = IniFile.ReadString(this.MachineName, "machineServerIP", "", Def.GetAbsPathName(Def.ModuleExCfg));
            if(!string.IsNullOrEmpty(strKey))
            {
                this.machineServerIP = strKey;
                this.machineServerPort = IniFile.ReadInt(this.MachineName, "machineServerPort", 5001, Def.GetAbsPathName(Def.ModuleExCfg));
                this.machineServer = new ModuleServer();

                foreach(RunProcess run in this.ListRuns)
                {
                    switch((RunID)run.GetRunID())
                    {
                        case RunID.OnloadRobot:
                        case RunID.Transfer:
                        case RunID.ManualOperate:
                        case RunID.PalletBuffer:
                        case RunID.OffloadBattery:
                        case RunID.CoolingSystem:
                            {
                                this.machineServer.AddServerData((RunID)run.GetRunID());
                                break;
                            }
                        default:
                            {
                                if((RunID.DryOven0 <= (RunID)run.GetRunID()) && ((RunID)run.GetRunID() < RunID.DryOvenALL))
                                {
                                    this.machineServer.AddServerData((RunID)run.GetRunID(), true);
                                }
                                break;
                            }
                    }
                }
                CreateServer();
            }
            this.machineClientIP = new List<string>();
            this.machineClientPort = new List<int>();
            this.machineClient = new List<ModuleClient>();
            int idx = 0;
            do 
            {
                strKey = IniFile.ReadString(this.MachineName, $"machineClientIP{idx}", "", Def.GetAbsPathName(Def.ModuleExCfg));
                if (!string.IsNullOrEmpty(strKey))
                {
                    this.machineClientIP.Add(strKey);
                    this.machineClientPort.Add(IniFile.ReadInt(this.MachineName, $"machineClientPort{idx}", 5001 + idx, Def.GetAbsPathName(Def.ModuleExCfg)));
                    this.machineClient.Add(new ModuleClient());
                }
                idx++;
            } while (!string.IsNullOrEmpty(strKey));

            #endregion

            #region // 监视线程

            if(!InitThread())
            {
                return false;
            }
            #endregion

            return true;
        }

        #endregion

        #region // 报警弹窗

        /// <summary>
        /// 非模态弹窗：非阻塞弹出线程会继续向下执行，一般弹出在Messsage.cfg中配置的报警
        /// </summary>
        /// <param name="msgID">报警ID</param>
        /// <param name="addMsg">附加报警内容</param>
        /// <param name="countdownTime">倒计时时间</param>
        /// <param name="countdownDlgBtn">倒计时完默认按钮</param>
        private async void ShowMessageID(int msgID, string[] addMsg = null, int countdownTime = 0, DialogResult countdownDlgBtn = DialogResult.None)
        {
            try
            {
                #region // 查找是否已有当前报警ID

                if(null != this.MsgList)
                {
                    if(this.MsgList.Contains(msgID))
                    {
                        return; // 已弹窗则跳过
                    }
                }
                // 保存报警ID
                this.MsgList.Add(msgID);

                #endregion

                int msgType;
                bool showDlg;
                string section, msg, msgDisp;

                #region // 读报警ID的内容
                section = string.Format("M{0:D4}", msgID);
                msg = IniFile.ReadString(section, "Name", (section + "未配置报警"), Def.GetAbsPathName(SysDef.MessageCfg));
                msgDisp = IniFile.ReadString(section, "Dispose", "", Def.GetAbsPathName(SysDef.MessageCfg));
                msgType = IniFile.ReadInt(section, "Type", 0, Def.GetAbsPathName(SysDef.MessageCfg));
                showDlg = IniFile.ReadBool(section, "ShowDialog", true, Def.GetAbsPathName(SysDef.MessageCfg));
                #endregion

                #region // 判断报警是否整机停机
                if(MessageType.MsgAlarm == (MessageType)msgType)
                {
                    if(this.AlarmStopMC)
                    {
                        RunsCtrl.Stop();
                    }
                }
                #endregion

                // 后续开始异步执行
                await System.Threading.Tasks.Task.Delay(1);

                #region // 替换内容
                int index = 0;
                string key, value;
                while(true)
                {
                    key = value = "#" + index + "#";
                    if(!msg.Contains(key))
                    {
                        break;
                    }
                    else
                    {
                        if((null != addMsg) && (addMsg.Length > index))
                        {
                            value = addMsg[index];
                            addMsg[index] = "";
                        }
                    }
                    msg = msg.Replace(key, value);
                    index++;
                }

                if(null != addMsg)
                {
                    for(int i = 0; i < addMsg.Length; i++)
                    {
                        if(!string.IsNullOrEmpty(addMsg[i]))
                        {
                            msg += addMsg[i];
                        }
                    }
                }
                #endregion

                #region // 构造报警内容
                string msgData = $"{MachineName}\r\n报警[{msgID:D4}]：{msg}\r\n处理方法：{msgDisp}";
                this.hasMsgBox = true; // 保存报警信息
                this.dbRecord.AddAlarmInfo(new AlarmFormula(Def.GetProductFormula(), msgID, msgData, msgType, (int)RunID.Invalid, MachineName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                #endregion

                #region // 弹窗报警
                if(showDlg)
                {
                    if(countdownTime > 0)
                    {
                        ShowMsgBox.ShowDialog(msgData, (MessageType)msgType, countdownTime, countdownDlgBtn);
                    }
                    else
                    {
                        ShowMsgBox.ShowDialog(msgData, (MessageType)msgType);
                    }
                }
                #endregion
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("MachineCtrl", $"ShowMessageID(string[]) {ex.Message}\r\n{ex.StackTrace}", LogType.Error);
            }
        }

        /// <summary>
        /// 非模态弹窗：非阻塞弹出线程会继续向下执行，弹出自定义报警内容与报警处理方法
        /// </summary>
        /// <param name="msgID">报警ID</param>
        /// <param name="msg">报警内容</param>
        /// <param name="msgDispose">报警处理方法</param>
        /// <param name="msgType">报警类型</param>
        /// <param name="countdownTime">倒计时时间</param>
        /// <param name="countdownDlgBtn">倒计时完默认按钮：OK或YES/NO</param>
        /// <returns>用户响应的弹窗按钮：OK或YES/NO</returns>
        private async void ShowMessageID(int msgID, string msg, string msgDispose, MessageType msgType, int countdownTime = 0, DialogResult countdownDlgBtn = DialogResult.None)
        {
            try
            {
                #region // 判断报警是否整机停机
                if(MessageType.MsgAlarm == msgType)
                {
                    if(this.AlarmStopMC)
                    {
                        RunsCtrl.Stop();
                    }
                }
                #endregion

                // 后续开始异步执行
                await System.Threading.Tasks.Task.Delay(1);

                #region // 查找是否已有当前报警ID

                if(null != this.MsgList)
                {
                    if(this.MsgList.Contains(msgID))
                    {
                        return; // 已弹窗则跳过
                    }
                }
                // 保存报警ID
                this.MsgList.Add(msgID);

                #endregion

                #region // 构造报警内容
                string msgData = $"{MachineName}\r\n报警[{msgID:D4}]：{msg}\r\n处理方法：{msgDispose}";
                this.hasMsgBox = true; // 保存报警信息
                this.dbRecord.AddAlarmInfo(new AlarmFormula(Def.GetProductFormula(), msgID, msgData, (int)msgType, (int)RunID.Invalid, MachineName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                #endregion

                #region // 弹窗报警
                if(countdownTime > 0)
                {
                    ShowMsgBox.ShowDialog(msgData, msgType, countdownTime, countdownDlgBtn);
                }
                else
                {
                    ShowMsgBox.ShowDialog(msgData, msgType);
                }
                #endregion
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("MachineCtrl", $"ShowMessageID(string) {ex.Message}\r\n{ex.StackTrace}", LogType.Error);
            }
        }

        #endregion

        #region // 系统的配置及参数

        /// <summary>
        /// 保存模组配置
        /// </summary>
        /// <param name="index"></param>
        /// <param name="moduleName"></param>
        /// <param name="inputs"></param>
        /// <param name="outputs"></param>
        /// <param name="motors"></param>
        private void WriteModuleCfg(int index, string moduleName, List<int> inputs, List<int> outputs, List<int> motors)
        {
            string section = "Module" + index;
            string path = Def.GetAbsPathName(Def.ModuleCfg);

            // 模组名
            IniFile.WriteString(section, "Name", moduleName, path);

            // 输入
            int count = inputs.Count;
            IniFile.WriteInt(section, "InputCount", count, path);
            for(int i = 0; i < count; i++)
            {
                IniFile.WriteInt(section, ("Input" + i), inputs[i], path);
            }
            // 输出
            count = outputs.Count;
            IniFile.WriteInt(section, "OutputCount", count, path);
            for(int i = 0; i < count; i++)
            {
                IniFile.WriteInt(section, ("Output" + i), outputs[i], path);
            }
            // 电机
            count = motors.Count;
            IniFile.WriteInt(section, "MotorCount", count, path);
            for(int i = 0; i < count; i++)
            {
                IniFile.WriteInt(section, ("Motor" + i), motors[i], path);
            }
        }

        /// <summary>
        /// 添加模组参数
        /// </summary>
        /// <param name="key">属性关键字</param>
        /// <param name="name">显示名称</param>
        /// <param name="description">描述</param>
        /// <param name="value">属性值</param>
        /// <param name="paraLevel">属性权限</param>
        /// <param name="readOnly">属性仅可读</param>
        /// <param name="visible">属性可见性</param>
        protected void InsertVoidParameter(string key, string name, string description, object value, RecordType paraType, ParameterLevel paraLevel = ParameterLevel.PL_STOP_MAIN)
        {
            this.insertParameterList.Add(key, new ParameterFormula(Def.GetProductFormula(), this.MachineName, name, key, value.ToString(), paraType, paraLevel));
            this.parameterProperty.Add("系统参数", key, name, description, value, (int)paraLevel, false, true);
        }

        /// <summary>
        /// 获取参数列表
        /// </summary>
        /// <returns></returns>
        public PropertyManage GetParameterList()
        {
            ReadParameter();
            PropertyManage pm = this.parameterProperty;
            foreach(Property item in this.parameterProperty)
            {
                if(null != pm[item.Name])
                {
                    if(item.Value is int)
                    {
                        pm[item.Name].Value = Convert.ToInt32(GetParameterValue(item.Name, pm[item.Name].Value));
                    }
                    else if(item.Value is uint)
                    {
                        pm[item.Name].Value = Convert.ToUInt32(GetParameterValue(item.Name, pm[item.Name].Value));
                    }
                    else if(item.Value is short)
                    {
                        pm[item.Name].Value = Convert.ToInt16(GetParameterValue(item.Name, pm[item.Name].Value));
                    }
                    else if(item.Value is bool)
                    {
                        pm[item.Name].Value = Convert.ToBoolean(GetParameterValue(item.Name, pm[item.Name].Value));
                    }
                    else if(item.Value is float)
                    {
                        pm[item.Name].Value = Convert.ToSingle(GetParameterValue(item.Name, pm[item.Name].Value));
                    }
                    else if(item.Value is double)
                    {
                        pm[item.Name].Value = Convert.ToDouble(GetParameterValue(item.Name, pm[item.Name].Value));
                    }
                    else if(item.Value is string)
                    {
                        pm[item.Name].Value = Convert.ToString(GetParameterValue(item.Name, pm[item.Name].Value));
                    }
                    else
                    {
                        string msg = string.Format("{0}】为{1}类型，未找到相匹配类型，无法获取参数值", item.DisplayName, item.Value.GetType().ToString());
                        ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    }
                }
            }
            return pm;
        }

        /// <summary>
        /// 修改参数时检查是否可修改
        /// </summary>
        public virtual bool CheckParameter(string name, object value)
        {
            return true;
        }

        /// <summary>
        /// 读系统设置参数
        /// </summary>
        public void ReadParameter()
        {
            #region // 从数据库读取参数

            List<ParameterFormula> listPara = new List<ParameterFormula>();
            this.dbRecord.GetParameterList(Def.GetProductFormula(), this.MachineName, ref listPara);
            foreach(var item in listPara)
            {
                if(this.dataBaseParameterList.ContainsKey(item.key))
                {
                    this.dataBaseParameterList[item.key] = item;
                }
                else
                {
                    this.dataBaseParameterList.Add(item.key, item);
                }
            }
            #endregion

            int maxRow = Convert.ToInt32(GetParameterValue("PalletMaxRow", (int)PalletRowCol.MaxRow));
            int maxCol = Convert.ToInt32(GetParameterValue("PalletMaxCol", (int)PalletRowCol.MaxCol));
            if ((maxRow != this.PalletMaxRow) || (maxCol != this.PalletMaxCol))
            {
                for(int i = 0; i < this.ListRuns.Count; i++)
                {
                    for(int plt = 0; plt < this.ListRuns[i].Pallet.Length; plt++)
                    {
                        this.ListRuns[i].Pallet[plt].SetRowCol(maxRow, maxCol);
                    }
                }
                this.PalletMaxRow = maxRow;
                this.PalletMaxCol = maxCol;
            }
            this.BakingMaxCount = Convert.ToInt32(GetParameterValue("BakingMaxCount", this.BakingMaxCount));
            //this.mcLogFilePath = Def.GetAbsPathName(Def.MachineLogFolder);
            this.mcLogFileSize = Convert.ToInt32(GetParameterValue("mcLogFileSize", 2));
            this.mcLogFileStorageLife = Convert.ToInt32(GetParameterValue("mcLogFileStorageLife", 7));
            this.ProductionFilePath = Convert.ToString(GetParameterValue("ProductionFilePath", @"D:\生产信息"));
            this.productionFileStorageLife = Convert.ToInt32(GetParameterValue("productionFileStorageLife", 30));
            this.OnloadClear = Convert.ToBoolean(GetParameterValue("OnloadClear", false));

        }

        /// <summary>
        /// 保存设置参数
        /// </summary>
        public void WriteParameter(string key, string value)
        {
            if(this.insertParameterList.ContainsKey(key))
            {
                ParameterFormula insertPara, dbPara;
                insertPara = this.insertParameterList[key];
                insertPara.module = this.MachineName;
                insertPara.value = value;
                if(this.dataBaseParameterList.ContainsKey(key))
                {
                    dbPara = this.dataBaseParameterList[key];
                    dbPara.value = insertPara.value;
                    dbPara.level = insertPara.level;
                    this.dbRecord.ModifyParameter(dbPara);
                }
                else
                {
                    this.dbRecord.AddParameter(insertPara);
                }
            }
        }

        /// <summary>
        /// 获取参数值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private object GetParameterValue(string key, object defaultValue)
        {
            if (this.dataBaseParameterList.ContainsKey(key))
            {
                return this.dataBaseParameterList[key].value;
            }
            return defaultValue;
        }

        /// <summary>
        /// 初始化参数
        /// </summary>
        private void InitParameter()
        {
            this.PalletMaxRow = (int)PalletRowCol.MaxRow;
            this.PalletMaxCol = (int)PalletRowCol.MaxCol;
            this.mcLogFilePath = Def.GetAbsPathName(Def.MachineLogFolder);
            this.mcLogFileSize = 2;
            this.mcLogFileStorageLife = 7;
            this.ProductionFilePath = @"D:\生产信息";
            this.productionFileStorageLife = 30;
            this.BakingMaxCount = 3;
            this.OnloadClear = false;
        }

        /// <summary>
        /// 读系统IO
        /// </summary>
        private void ReadSystemIO()
        {
            string module, key, path;
            module = this.MachineName;
            path = Def.GetAbsPathName(Def.ModuleExCfg);
            List<int> inputs, outputs, motors;
            inputs = new List<int>();
            outputs = new List<int>();
            motors = new List<int>();

            int maxCount = (int)SystemIO.ButtonIO;
            #region // 输入：按钮

            this.IStartButton = new int[maxCount];
            this.IStopButton = new int[maxCount];
            this.IEStopButton = new int[maxCount];
            this.IResetButton = new int[maxCount];
            for(int idx = 0; idx < maxCount; idx++)
            {
                key = ("IStartButton" + idx);
                this.IStartButton[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if(this.IStartButton[idx] > -1)
                {
                    inputs.Add(this.IStartButton[idx]);
                }
                key = ("IStopButton" + idx);
                this.IStopButton[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if(this.IStopButton[idx] > -1)
                {
                    inputs.Add(this.IStopButton[idx]);
                }
                key = ("IEStopButton" + idx);
                this.IEStopButton[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if(this.IEStopButton[idx] > -1)
                {
                    inputs.Add(this.IEStopButton[idx]);
                }
                key = ("IResetButton" + idx);
                this.IResetButton[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if(this.IResetButton[idx] > -1)
                {
                    inputs.Add(this.IResetButton[idx]);
                }
            }
            #endregion

            #region // 输出：按钮LED

            this.OStartLed = new int[maxCount];
            this.OStopLed = new int[maxCount];
            this.OResetLed = new int[maxCount];
            for(int idx = 0; idx < maxCount; idx++)
            {
                key = ("OStartLed" + idx);
                this.OStartLed[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if(this.OStartLed[idx] > -1)
                {
                    outputs.Add(this.OStartLed[idx]);
                }
                key = ("OStopLed" + idx);
                this.OStopLed[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if(this.OStopLed[idx] > -1)
                {
                    outputs.Add(this.OStopLed[idx]);
                }
                key = ("OResetLed" + idx);
                this.OResetLed[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if(this.OResetLed[idx] > -1)
                {
                    outputs.Add(this.OResetLed[idx]);
                }
            }
            #endregion

            maxCount = (int)SystemIO.SafeDoorIO;
            #region // 输入：安全门

            this.ISafeDoorState = new int[maxCount];
            this.ISafeDoorOpenBtn = new int[maxCount];
            this.ISafeDoorCloseBtn = new int[maxCount];
            this.SafeDoorStopModule = new string[maxCount];
            this.SafeDoorDelay = new int[maxCount];
            for(int idx = 0; idx < maxCount; idx++)
            {
                key = ("ISafeDoorState" + idx);
                this.ISafeDoorState[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if(this.ISafeDoorState[idx] > -1)
                {
                    inputs.Add(this.ISafeDoorState[idx]);

                    key = ("SafeDoorStopModule" + idx);
                    this.SafeDoorStopModule[idx] = IniFile.ReadString(module, key, "", path);
                    key = ("SafeDoorDelay" + idx);
                    this.SafeDoorDelay[idx] = IniFile.ReadInt(module, key, 500, path);
                }
                key = ("ISafeDoorOpenBtn" + idx);
                this.ISafeDoorOpenBtn[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if(this.ISafeDoorOpenBtn[idx] > -1)
                {
                    inputs.Add(this.ISafeDoorOpenBtn[idx]);
                }
                key = ("ISafeDoorCloseBtn" + idx);
                this.ISafeDoorCloseBtn[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if(this.ISafeDoorCloseBtn[idx] > -1)
                {
                    inputs.Add(this.ISafeDoorCloseBtn[idx]);
                }
            }
            #endregion

            #region // 输出：安全门

            this.OSafeDoorOpenLed = new int[maxCount];
            this.OSafeDoorCloseLed = new int[maxCount];
            this.OSafeDoorUnlock = new int[maxCount];
            for(int idx = 0; idx < maxCount; idx++)
            {
                key = ("OSafeDoorOpenLed" + idx);
                this.OSafeDoorOpenLed[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if(this.OSafeDoorOpenLed[idx] > -1)
                {
                    outputs.Add(this.OSafeDoorOpenLed[idx]);
                }
                key = ("OSafeDoorCloseLed" + idx);
                this.OSafeDoorCloseLed[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if(this.OSafeDoorCloseLed[idx] > -1)
                {
                    outputs.Add(this.OSafeDoorCloseLed[idx]);
                }
                key = ("OSafeDoorUnlock" + idx);
                this.OSafeDoorUnlock[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if(this.OSafeDoorUnlock[idx] > -1)
                {
                    outputs.Add(this.OSafeDoorUnlock[idx]);
                }
            }
            #endregion

            maxCount = (int)SystemIO.TowerIO;
            #region // 输出：灯塔

            this.OLightTowerRed = new int[maxCount];
            this.OLightTowerYellow = new int[maxCount];
            this.OLightTowerGreen = new int[maxCount];
            this.OLightTowerBuzzer = new int[maxCount];
            for(int idx = 0; idx < maxCount; idx++)
            {
                key = ("OLightTowerRed" + idx);
                this.OLightTowerRed[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if(this.OLightTowerRed[idx] > -1)
                {
                    outputs.Add(this.OLightTowerRed[idx]);
                }
                key = ("OLightTowerYellow" + idx);
                this.OLightTowerYellow[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if(this.OLightTowerYellow[idx] > -1)
                {
                    outputs.Add(this.OLightTowerYellow[idx]);
                }
                key = ("OLightTowerGreen" + idx);
                this.OLightTowerGreen[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if(this.OLightTowerGreen[idx] > -1)
                {
                    outputs.Add(this.OLightTowerGreen[idx]);
                }
                key = ("OLightTowerBuzzer" + idx);
                this.OLightTowerBuzzer[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if(this.OLightTowerBuzzer[idx] > -1)
                {
                    outputs.Add(this.OLightTowerBuzzer[idx]);
                }
            }
            #endregion

            #region // 气压报警

            this.IAirPressureAlarm = DecodeInputID(IniFile.ReadString(module, "IAirPressureAlarm", "", path));
            if (this.IAirPressureAlarm > -1)
            {
                inputs.Add(this.IAirPressureAlarm);
            }
            #endregion

            WriteModuleCfg(0, module, inputs, outputs, motors);
        }

        /// <summary>
        /// 加载指定电机的点位
        /// </summary>
        /// <param name="motorID"></param>
        /// <returns></returns>
        internal bool LoadMotorLocation(int motorID)
        {
            List<MotorFormula> motorlist = new List<MotorFormula>();
            if(this.dbRecord.GetMotorPosList(Def.GetProductFormula(), motorID, ref motorlist))
            {
                DeviceManager.GetMotorManager().LstMotors[motorID].DeleteAllLoc();
                motorlist.Sort(delegate (MotorFormula left, MotorFormula right)
                { return left.posID - right.posID; });
                foreach(var item in motorlist)
                {
                    if((int)MotorCode.MotorOK != DeviceManager.GetMotorManager().LstMotors[motorID].AddLocation(item.posID, item.posName, item.posValue))
                    {
                        Def.WriteLog("MachineCtrl", $"M{motorID}电机{item.posID}】{item.posName}点位添加失败", LogType.Error);
                        return false;
                    }
                }
                return true;
            }
            else
            {
                string msg = $"M{motorID}电机点位获取错误，请检查！\r\n确认后软件将会退出";
                //ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                Def.WriteLog("MachineCtrl", msg, LogType.Error);
            }
            return false;
        }

        #endregion

        #region // 系统的运行数据

        #endregion

        #region // 设备运行检查

        /// <summary>
        /// 设备复位，清楚报警等信息
        /// </summary>
        public void MachineReset()
        {
            this.MsgList.Clear();
            this.hasMsgBox = false;
            this.RunsCtrl.Reset();
            MesOperateMySql.EquipmentAlarmEndTime(DateTime.Now);
        }

        /// <summary>
        /// 设备启动前检查是否能启动
        /// </summary>
        /// <returns></returns>
        protected bool BeforeStart()
        {
            if (MCState.MCRunning == GetInstance().RunsCtrl.GetMCState())
            {
                return false;
            }
            if (!Def.IsNoHardware() && string.IsNullOrEmpty(GetInstance().OperaterID))
            {
                string msg, billNo, billNum, equipmentID, processID;
                msg = billNo = billNum = equipmentID = processID = "";

                for (int i = 0; i < (MesData.mesFrequency == 0 ? 3 : MesData.mesFrequency); i++)
                {
                    //获取工单
                    if (!MachineCtrl.GetInstance().MesGetBillNO(equipmentID, processID, ref msg, out billNo, out billNum))
                    {
                        //获取报警字符串是否包含"超时"二字,如果没有则跳出循环
                        if (!msg.Contains("超时"))
                        {
                            break;
                        }
                        if (i == 2)
                        {
                            ShowMsgBox.ShowDialog($"MES获取工单信息接口超时失败3次，请检查MES连接状态", MessageType.MsgAlarm);
                        }
                    }
                    else
                    {
                        msg = $"工单获取成功\r\n工单号：{billNo}\r\n工单数量：{billNum}";
                        ShowMsgBox.Show(msg, MessageType.MsgMessage);

                        break;
                    }
                }

                List<UserFormula> userList = new List<UserFormula>();
                userList.Add(new UserFormula("1", "登录操作人员工号", "", UserLevelType.USER_OPERATOR));
                UserLogin user = new UserLogin();
                user.SetUserList(null, userList);
                if(DialogResult.OK == user.ShowDialog())
                {
                    GetInstance().OperaterID = user.userInfo;
                }
                if (string.IsNullOrEmpty(GetInstance().OperaterID))
                {
                    ShowMsgBox.ShowDialog("未登录操作人员工号，不能启动软件！", MessageType.MsgWarning);
                }
                return false;
            }
            if(!ClientIsConnect()/* && !ConnectClient()*/)
            {
                ShowMsgBox.ShowDialog("请等候模组服务连接后再启动软件...", MessageType.MsgWarning);
                return false;
            }
            if (!MesOperateMySql.MySqlIsOpen())
            {
                ShowMsgBox.ShowDialog("请等候MySql服务连接后再启动软件...", MessageType.MsgWarning);
                return false;
            }
            if(CheckSafeDoorState())
            {
                return false;
            }
            if(StopButtonOn())
            {
                ShowMsgBox.ShowDialog("急停或停止按钮被按下，不能启动软件！", MessageType.MsgWarning);
                return false;
            }
            if(!UpdataMes && !Def.IsNoHardware())
            {
                string msg = string.Format("【离线生产】将不能上传MES，是否继续！");
                if(DialogResult.Yes != ShowMsgBox.ShowDialog(msg, MessageType.MsgQuestion))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 设备停止后进行的操作
        /// </summary>
        protected void AfterStop()
        {
            foreach(var item in this.ListRuns)
            {
                item.AfterStopAction();
            }
        }
        
        #endregion

        #region // 模组数据

        /// <summary>
        /// 获取指定RunModule模组名的模组
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        public RunProcess GetModule(string runModule)
        {
            foreach(RunProcess run in this.ListRuns)
            {
                if((null != run) && (runModule == run.RunModule))
                {
                    return run;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取指定RunModule模组名的模组
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        public RunProcess GetModule(RunID runID)
        {
            foreach(RunProcess run in this.ListRuns)
            {
                if((null != run) && ((int)runID == run.GetRunID()))
                {
                    return run;
                }
            }
            return null;
        }

        /// <summary>
        /// 保存模组通讯数据
        /// </summary>
        /// <param name="socketData"></param>
        public void SetModuleSocketData(ModuleSocketData socketData)
        {
            if (this.moduleSocketData.ContainsKey(socketData.machineID))
            {
                this.moduleSocketData[socketData.machineID] = socketData;
            } 
            else
            {
                this.moduleSocketData.Add(socketData.machineID, socketData);
            }
            switch(socketData.machineID)
            {
                case 0:
                    TotalData.OnloadCount = socketData.onloadCount;
                    TotalData.OnScanNGCount = socketData.onScanNGCount;
                    if (!string.IsNullOrEmpty(socketData.mesBillNo) && !string.IsNullOrEmpty(socketData.mesBillNum))
                    {
                        if((MesResources.BillNo != socketData.mesBillNo) || (MesResources.BillNum != socketData.mesBillNum))
                        {
                            MesResources.BillNo = socketData.mesBillNo;
                            MesResources.BillNum = socketData.mesBillNum;
                            MesResources.WriteConfig();
                        }
                    }
                    MesConfig cfg = MesDefine.GetMesCfg(MesInterface.ApplyTechProParam);
                    if ((null != cfg) && (cfg.parameterDate != socketData.mesBillParamDate))
                    {
                        cfg.parameterDate = socketData.mesBillParamDate;
                        cfg.parameter = socketData.mesBillParam;
                        MesDefine.WriteConfig(MesInterface.ApplyTechProParam);
                    }
                    break;
                case 2:
                    TotalData.OffloadCount = socketData.offloadCount;
                    TotalData.BakedNGCount = socketData.bakedNGCount;
                    break;
            }
        }

        /// <summary>
        /// 获取模组通讯数据
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        public ModuleSocketData GetModuleSocketData(RunID runId)
        {
            ModuleSocketData[] socketData = this.moduleSocketData.Values.ToArray();
            foreach(var item in socketData)
            {
                if (item.moduleEnable.ContainsKey(runId))
                {
                    return item;
                } 
            }
            return null;
        }

        /// <summary>
        /// 获取指定模组所在设备的设备状态
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        public MCState GetModuleMCState(RunID runId)
        {
            // 本地
            RunProcess run = GetInstance().GetModule(runId);
            if(null != run)
            {
                return RunsCtrl.GetMCState();
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if(null != socketData)
                {
                    return (MCState)socketData.machineState;
                }
            }
            return MCState.MCInvalidState;
        }

        /// <summary>
        /// 获取模组使能状态
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        public bool GetModuleEnable(RunID runId)
        {
            // 本地
            RunProcess run = GetInstance().GetModule(runId);
            if(null != run)
            {
                return run.IsModuleEnable();
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if(null != socketData)
                {
                    return socketData.moduleEnable[runId];
                }
            }
            return false;
        }

        /// <summary>
        /// 获取模组运行状态
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        public bool GetModuleRunning(RunID runId)
        {
            // 本地
            RunProcess run = GetInstance().GetModule(runId);
            if(null != run)
            {
                return run.IsRunning();
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if(null != socketData)
                {
                    return socketData.moduleRunning[runId];
                }
            }
            return false;
        }

        /// <summary>
        /// 获取干燥炉/机器人连接状态：true连接，false断开
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        public bool GetDeviceIsConnect(RunID runId)
        {
            // 本地
            RunProcess run = GetInstance().GetModule(runId);
            if(null != run)
            {
                if (run is RunProcessDryingOven)
                {
                    return ((RunProcessDryingOven)run).DryOvenIsConnect();
                }
                else if (run is RunProcessOnloadRobot)
                {
                    return ((RunProcessOnloadRobot)run).RobotIsConnect();
                }
                else if(run is RunProcessRobotTransfer)
                {
                    return ((RunProcessRobotTransfer)run).RobotIsConnect();
                }
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if(null != socketData)
                {
                    return socketData.deviceIsConnect[runId];
                }
            }
            return false;
        }

        /// <summary>
        /// 获取所有干燥炉的运行时间：[干燥炉，炉腔]
        /// </summary>
        /// <returns></returns>
        public uint[,] GetDryingOvenWorkTime()
        {
            bool result = false;
            uint[,] workTime = new uint[(int)OvenInfoCount.OvenCount, (int)OvenRowCol.MaxRow];
            for(int id = 0; id < (int)OvenInfoCount.OvenCount; id++)
            {
                RunProcessDryingOven run = GetModule(id + RunID.DryOven0) as RunProcessDryingOven;
                if(null != run)
                {
                    for(int row = 0; row < (int)OvenRowCol.MaxRow; row++)
                    {
                        if ((CavityStatus.Heating == run.CavityState[row])
                            || (CavityStatus.WaitDetect == run.CavityState[row]))
                        {
                            workTime[id, row] = run.RCavity(row).workTime;
                        }
                        else
                        {
                            workTime[id, row] = 0;
                        }
                    }
                    result = true;
                }
            }
            if (!result)
            {
                ModuleSocketData socketData = GetModuleSocketData(RunID.DryOven0);
                if (null != socketData)
                {
                    RunID[] runId = socketData.cavityTime.Keys.ToArray();
                    for(int i = 0; i < runId.Length; i++)
                    {
                        uint[] cavityTime = socketData.cavityTime[runId[i]];
                        if(null != cavityTime)
                        {
                            for(int row = 0; row < (int)OvenRowCol.MaxRow; row++)
                            {
                                if(((int)CavityStatus.Heating == socketData.cavityState[runId[i]][row])
                                    || ((int)CavityStatus.WaitDetect == socketData.cavityState[runId[i]][row]))
                                {
                                    workTime[(runId[i] - RunID.DryOven0), row] = cavityTime[row];
                                }
                                else
                                {
                                    workTime[(runId[i] - RunID.DryOven0), row] = 0;
                                }
                            }
                            result = true;
                        }
                    }
                }
            }
            return workTime;
        }

        /// <summary>
        /// 获取干燥炉腔体干燥状态
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        public CavityStatus GetDryingOvenCavityState(RunID runId, int cavityIdx)
        {
            // 本地
            RunProcessDryingOven run = GetInstance().GetModule(runId) as RunProcessDryingOven;
            if(null != run)
            {
                return run.CavityState[cavityIdx];
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if(null != socketData)
                {
                    return (CavityStatus)socketData.cavityState[runId][cavityIdx];
                }
            }
            return CavityStatus.Unknown;
        }

        /// <summary>
        /// 获取干燥炉腔体使能状态
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="cavityIdx">腔体索引</param>
        /// <returns></returns>
        public bool GetDryingOvenCavityEnable(RunID runId, int cavityIdx)
        {
            // 本地
            RunProcessDryingOven run = GetInstance().GetModule(runId) as RunProcessDryingOven;
            if(null != run)
            {
                return run.CavityEnable[cavityIdx];
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if(null != socketData)
                {
                    return socketData.cavityEnable[runId][cavityIdx];
                }
            }
            return false;
        }

        /// <summary>
        /// 获取干燥炉腔体保压状态
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="cavityIdx">腔体索引</param>
        /// <returns></returns>
        public bool GetDryingOvenCavityPressure(RunID runId, int cavityIdx)
        {
            // 本地
            RunProcessDryingOven run = GetInstance().GetModule(runId) as RunProcessDryingOven;
            if(null != run)
            {
                return run.CavityPressure[cavityIdx];
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if(null != socketData)
                {
                    return socketData.cavityPressure[runId][cavityIdx];
                }
            }
            return false;
        }

        /// <summary>
        /// 获取干燥炉腔体转移状态
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="cavityIdx">腔体索引</param>
        /// <returns></returns>
        public bool GetDryingOvenCavityTransfer(RunID runId, int cavityIdx)
        {
            // 本地
            RunProcessDryingOven run = GetInstance().GetModule(runId) as RunProcessDryingOven;
            if(null != run)
            {
                return run.CavityTransfer[cavityIdx];
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if(null != socketData)
                {
                    return socketData.cavityTransfer[runId][cavityIdx];
                }
            }
            return false;
        }

        /// <summary>
        /// 获取干燥炉腔体抽检周期
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="cavityIdx">腔体索引</param>
        /// <returns></returns>
        public int GetDryingOvenCavitySamplingCycle(RunID runId, int cavityIdx)
        {
            // 本地
            RunProcessDryingOven run = GetInstance().GetModule(runId) as RunProcessDryingOven;
            if(null != run)
            {
                return run.CavitySamplingCycle[cavityIdx];
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if(null != socketData)
                {
                    return socketData.cavitySamplingCycle[runId][cavityIdx];
                }
            }
            return 1;
        }

        /// <summary>
        /// 获取干燥炉腔体加热次数
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="cavityIdx">腔体索引</param>
        /// <returns></returns>
        public int GetDryingOvenCavityHeartCycle(RunID runId, int cavityIdx)
        {
            // 本地
            RunProcessDryingOven run = GetInstance().GetModule(runId) as RunProcessDryingOven;
            if(null != run)
            {
                return run.CavityHeartCycle[cavityIdx];
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if(null != socketData)
                {
                    return socketData.cavityHeartCycle[runId][cavityIdx];
                }
            }
            return 1;
        }

        /// <summary>
        /// 获取缓存架层使能状态
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="pltIdx">夹具位置索引</param>
        /// <returns></returns>
        public bool GetPalletBufferRowEnable(RunID runId, int rowIdx)
        {
            // 本地
            RunProcessPalletBuffer run = GetInstance().GetModule(runId) as RunProcessPalletBuffer;
            if(null != run)
            {
                return run.BufferEnable[rowIdx];
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if(null != socketData)
                {
                    if(null != socketData.pltPosEnable)
                    {
                        return socketData.pltPosEnable[runId][rowIdx];
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 获取上下料夹具位使能状态
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="pltIdx">夹具位置索引</param>
        /// <returns></returns>
        public bool GetPalletPosEnable(RunID runId, int pltIdx)
        {
            // 本地
            RunProcessOnloadRobot run = GetInstance().GetModule(runId) as RunProcessOnloadRobot;
            if(null != run)
            {
                return run.PalletPosEnable[pltIdx];
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if(null != socketData)
                {
                    return socketData.pltPosEnable[runId][pltIdx];
                }
            }
            return false;
        }

        /// <summary>
        /// 获取模组夹具位感应器状态：0未知，1为OFF，2为ON，3为错误（和enum OvenStatus枚举中夹具状态对应）
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="pltIdx">夹具位置索引</param>
        /// <returns>夹具位感应器状态：0未知，1为OFF，2为ON，3为错误（和enum OvenStatus枚举中夹具状态对应）</returns>
        public int GetPalletPosSenser(RunID runId, int pltIdx)
        {
            // 本地
            RunProcess run = GetInstance().GetModule(runId);
            if(null != run)
            {
                bool hasPlt = run.PalletKeepFlat(pltIdx, true, false);
                bool noPlt = run.PalletKeepFlat(pltIdx, false, false);
                // 无夹具
                if(!hasPlt && noPlt)
                {
                    return (int)OvenStatus.PalletNot;
                }
                // 有夹具
                else if(hasPlt && !noPlt)
                {
                    return (int)OvenStatus.PalletHave;
                }
                // 错误
                else
                {
                    return (int)OvenStatus.PalletErrror;
                }
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if(null != socketData)
                {
                    if(socketData.pltPosSenser[runId].Length > pltIdx)
                    {
                        return socketData.pltPosSenser[runId][pltIdx];
                    }
                }
            }
            return 0;
        }

        /// <summary>
        /// 获取机器人动作信息
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="autoAction"></param>
        /// <returns></returns>
        public RobotActionInfo GetRobotActionInfo(RunID runId, bool autoAction)
        {
            RunProcess run = GetInstance().GetModule(runId);
            // 模组存在，使用本地数据
            if(null != run)
            {
                if(run is RunProcessOnloadRobot)
                {
                    return ((RunProcessOnloadRobot)run).GetRobotActionInfo(autoAction);
                }
                else if(run is RunProcessRobotTransfer)
                {
                    return ((RunProcessRobotTransfer)run).GetRobotActionInfo(autoAction);
                }
                else if(run is RunProcessOffloadBattery)
                {
                    return ((RunProcessOffloadBattery)run).GetRobotActionInfo(autoAction);
                }
            }
            // 模组不存在，使用网络数据
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if(null != socketData)
                {
                    return socketData.robotAction[runId][autoAction ? 0 : 1];
                }
            }
            return null;
        }

        /// <summary>
        /// 获取机器人移动状态：true移动中，false非移动中
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        public bool GetRobotRunning(RunID runId)
        {
            RunProcess run = GetInstance().GetModule(runId);
            // 模组存在，使用本地数据
            if(null != run)
            {
                if(run is RunProcessOnloadRobot)
                {
                    return ((RunProcessOnloadRobot)run).RobotRunning;
                }
                else if(run is RunProcessRobotTransfer)
                {
                    return ((RunProcessRobotTransfer)run).RobotRunning;
                }
            }
            // 模组不存在，使用网络数据
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if(null != socketData)
                {
                    return socketData.robotRunning[runId];
                }
            }
            return false;
        }

        /// <summary>
        /// 设置模组的信号状态
        /// </summary>
        /// <param name="modEvent"></param>
        /// <param name="eventState"></param>
        /// <returns></returns>
        public bool SetModuleEvent(RunID runId, EventList modEvent, EventStatus eventState, int eventPos)
        {
            // 本地
            RunProcess run = GetInstance().GetModule(runId);
            if(null != run)
            {
                return run.SetEvent(run, modEvent, eventState, eventPos);
            }
            // 网络
            else
            {
                ModuleSocketData socketData = new ModuleSocketData();
                ModuleEvent[] evt = new ModuleEvent[1];
                evt[0] = new ModuleEvent(modEvent, eventState, eventPos);
                socketData.moduleEvent = new Dictionary<RunID, ModuleEvent[]>();
                socketData.moduleEvent.Add(runId, evt);
                for(int i = 0; i < this.machineClient.Count; i++)
                {
                    if ((null != this.machineClient[i]) && this.machineClient[i].CheckRunID(-1, (int)runId))
                    {
                        return this.machineClient[i].SendAndWait((uint)PacketType.SetEvent, socketData);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 获取模组的信号状态
        /// </summary>
        /// <param name="modEvent"></param>
        /// <returns></returns>
        public EventStatus GetModuleEvent(RunID runId, EventList modEvent, ref int eventPos)
        {
            // 本地
            RunProcess run = GetInstance().GetModule(runId);
            if(null != run)
            {
                return run.GetEvent(run, modEvent, ref eventPos);
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if(null != socketData)
                {
                    if((null != socketData.moduleEvent) && (null != socketData.moduleEvent[runId]))
                    {
                        for(int i = 0; i < socketData.moduleEvent[runId].Length; i++)
                        {
                            if (modEvent == socketData.moduleEvent[runId][i].Event)
                            {
                                eventPos = socketData.moduleEvent[runId][i].Pos;
                                return socketData.moduleEvent[runId][i].State;
                            }
                        }
                    }
                }
            }
            return EventStatus.Invalid;
        }

        /// <summary>
        /// 获取模组夹具数据
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        public Pallet[] GetModulePallet(RunID runId)
        {
            RunProcess run = GetInstance().GetModule(runId);
            // 模组存在，使用本地数据
            if(null != run)
            {
                return run.Pallet;
            }
            // 模组不存在，使用网络数据
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if(null != socketData)
                {
                    return socketData.pallet[runId];
                }
            }
            return null;
        }

        /// <summary>
        /// 获取模组电池线数据（冷却系统电池）
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        public BatteryLine GetModuleBatteryLine(RunID runId)
        {
            RunProcess run = GetInstance().GetModule(runId);
            // 模组存在，使用本地数据
            if(null != run)
            {
                return run.BatteryLine;
            }
            // 模组不存在，使用网络数据
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if(null != socketData)
                {
                    return socketData.batteryLine[runId];
                }
            }
            return null;
        }

        /// <summary>
        /// 设置模组夹具数据
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="pltIdx"></param>
        /// <param name="pallet"></param>
        /// <returns></returns>
        public bool SetModulePallet(RunID runId, int pltIdx, Pallet pallet)
        {
            // 本地
            RunProcess run = GetInstance().GetModule(runId);
            if(null != run)
            {
                run.Pallet[pltIdx].Copy(pallet);
                run.SaveRunData(SaveType.Pallet);
                return true;
            }
            // 网络
            else
            {
                ModuleSocketData socketData = new ModuleSocketData();
                Pallet[] plt = new Pallet[pltIdx + 1];
                plt[pltIdx] = pallet;
                socketData.pallet = new Dictionary<RunID, Pallet[]>();
                socketData.pallet.Add(runId, plt);
                if(null != socketData.pallet)
                {
                    for(int i = 0; i < this.machineClient.Count; i++)
                    {
                        if((null != this.machineClient[i]) && this.machineClient[i].CheckRunID(-1, (int)runId))
                        {
                            return this.machineClient[i].SendAndWait((uint)PacketType.SetPallet, socketData);
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 设置腔体水含量数据
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="cavityIdx"></param>
        /// <param name="water"></param>
        /// <returns></returns>
        public bool SetCavityWaterContent(RunID runId, int cavityIdx, double[] water)
        {
            // 本地
            RunProcessDryingOven run = GetInstance().GetModule(runId) as RunProcessDryingOven;
            if(null != run)
            {
                run.SetWaterContent(cavityIdx, water);
                return true;
            }
            // 网络
            else
            {
                ModuleSocketData socketData = new ModuleSocketData();
                double[,] waterContent = new double[(int)OvenRowCol.MaxRow, 3];
                for(int i = 0; i < 3; i++)
                {
                    waterContent[cavityIdx, i] = water[i];
                }
                socketData.waterContentValue = new Dictionary<RunID, double[,]>();
                socketData.waterContentValue.Add(runId, waterContent);
                if(null != socketData.waterContentValue)
                {
                    for(int i = 0; i < this.machineClient.Count; i++)
                    {
                        if((null != this.machineClient[i]) && this.machineClient[i].CheckRunID(-1, (int)runId))
                        {
                            return this.machineClient[i].SendAndWait((uint)PacketType.SetWaterContent, socketData);
                        }
                    }
                }
            }
            return false;
        }

        #endregion

        #region // 解析IO及电机配置

        public int DecodeInputID(string strID)
        {
            if (!string.IsNullOrEmpty(strID) && (strID.IndexOf("-1") < 0))
            {
                int id = this.listInput.IndexOf(strID);
                if (id > -1)
                {
                    return id;
                }
                else if(!Def.IsNoHardware())
                {
                    ShowMsgBox.ShowDialog(string.Format("未找到输入配置[{0}]", strID), MessageType.MsgAlarm);
                }
            }
            return -1;
        }

        public int DecodeOutputID(string strID)
        {
            if(!string.IsNullOrEmpty(strID) && (strID.IndexOf("-1") < 0))
            {
                int id = this.listOutput.IndexOf(strID);
                if(id > -1)
                {
                    return id;
                }
                else if(!Def.IsNoHardware())
                {
                    ShowMsgBox.ShowDialog(string.Format("未找到输出配置[{0}]", strID), MessageType.MsgAlarm);
                }
            }
            return -1;
        }

        public int DecodeMotorID(string strID)
        {
            if(!string.IsNullOrEmpty(strID) && (strID.IndexOf("-1") < 0))
            {
                strID = "Motor" + strID.Trim("M".ToCharArray());
                int id = this.listMotor.IndexOf(strID);
                if(id > -1)
                {
                    return id;
                }
                else if(!Def.IsNoHardware())
                {
                    ShowMsgBox.ShowDialog(string.Format("未找到电机文件[{0}]", strID), MessageType.MsgAlarm);
                }
            }
            return -1;
        }

        #endregion

        #region // 线程初始化及释放

        /// <summary>
        /// 初始化线程(开始运行)
        /// </summary>
        private bool InitThread()
        {
            this.taskList = new List<Task>();
            try
            {
                this.monitorRunning = true;

                Task task = new Task(MonitorThread, TaskCreationOptions.LongRunning);
                task.Start();
                this.taskList.Add(task);
                Def.WriteLog("MachineCtrl", $"InitThread():MonitorThread = {task.Id} start", LogType.Success);

                task = new Task(HeartbeatThread, TaskCreationOptions.LongRunning);
                task.Start();
                this.taskList.Add(task);
                this.heartbeatTime = DateTime.Now;
                this.mySqlCheckTime = DateTime.Now;
                this.heartbeatCount = 0;
                Def.WriteLog("MachineCtrl", $"InitThread():HeartbeatThread = {task.Id} start", LogType.Success);

                task = (new Task(ExpirationCheck, TaskCreationOptions.None));
                task.Start();
                this.taskList.Add(task);
                Def.WriteLog("MachineCtrl", $"InitThread():ExpirationCheck = {task.Id} start", LogType.Success);

                return true;
            }
            catch(System.Exception ex)
            {
                Def.WriteLog("MachineCtrl", $"InitThread() error : {ex.Message}", LogType.Error);
            }
            return false;
        }

        /// <summary>
        /// 释放线程(终止运行)
        /// </summary>
        private bool ReleaseThread()
        {
            try
            {
                this.monitorRunning = false;

                Task.WaitAll(this.taskList.ToArray(), 10000);
                Def.WriteLog("MachineCtrl", $"ReleaseThread() All Task end", LogType.Success);

                return true;
            }
            catch(System.Exception ex)
            {
                Def.WriteLog("MachineCtrl", $"ReleaseThread() error: {ex.Message}", LogType.Error);
            }
            return false;
        }

        #endregion

        #region // 监视线程

        /// <summary>
        /// 模组监视线程
        /// </summary>
        private void MonitorThread()
        {
            while(this.monitorRunning)
            {
                try
                {
                    MCState mcState = this.RunsCtrl.GetMCState();

                    if (McStopState(mcState))
                    {
                        if(!autoConnectCSState && (DateTime.Now - autoConnectCSTime).TotalSeconds > 3)
                        {
                            if(!ClientIsConnect(false))
                            {
                                autoConnectCSState = true;
                                AutoConnectClient();
                            }
                            autoConnectCSTime = DateTime.Now;
                        }
                    }
                    else
                    {
                        ClientIsConnect(true);
                    }
                    MachineMonitor(mcState);
                    SafeDoorMonitor(mcState);
                }
                catch (System.Exception ex)
                {
                    Def.WriteLog("MachineCtrl", "MonitorThread()" + ex.Message, LogType.Error);
                }
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// 设备监视
        /// </summary>
        private void MachineMonitor(MCState mcState)
        {
            // 操作灯塔、按钮LED
            if ((DateTime.Now - setTowerStart).TotalMilliseconds >= 200.0)
            {
                SetTowerButton(mcState);
                this.setTowerStart = DateTime.Now;
            }
            // 判断系统按钮
            // 急停-停止
            if (StopButtonOn())
            {
                this.RunsCtrl.Stop();
            }
            // 复位
            if (ResetButtonOn())
            {
                if (!this.resetButtonOff)
                {
                    this.resetButtonOff = true;
                    MachineReset();
                }
            }
            else if(this.resetButtonOff && !ResetButtonOn())
            {
                this.resetButtonOff = false;
            }
            // 启动 && 非维护锁屏状态
            if (StartButtonOn() && !this.MaintenanceLock)
            {
                if(!this.startButtonOff)
                {
                    this.startButtonOff = true;

                    this.RunsCtrl.Start();
                }
            }
            else if(this.startButtonOff && !StartButtonOn())
            {
                this.startButtonOff = false;
            }
//暂时屏蔽气压报警，后续更改
            //if (this.IAirPressureAlarm > -1 && DeviceManager.Inputs(IAirPressureAlarm).IsOn())
            //{
            //    ShowMessageID((int)MsgID.AirPressureAlm, "气压过低报警", "请检查气压", MessageType.MsgAlarm);
            //    if (!McStopState(mcState))
            //    {
            //        this.RunsCtrl.Stop();
            //    }
            //}

            foreach(var item in this.ListRuns)
            {
                item.MonitorAvoidDie();
            }

        }

        #endregion

        #region // 删除过期文件

        /// <summary>
        /// 过期检查线程，只执行一次
        /// </summary>
        private void ExpirationCheck()
        {
            try
            {
                DeleteDirectoryFile(new DirectoryInfo(Def.GetAbsPathName("Log")));
                DeleteDirectoryFile(new DirectoryInfo(this.ProductionFilePath));
                DeleteDataBaseRecord();
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("MachineCtrl", "ExpirationCheck()" + ex.Message, LogType.Error);
            }
        }

        /// <summary>
        /// 删除目录中超期的文件
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        private void DeleteDirectoryFile(DirectoryInfo dir)
        {
            FileInfo[] fileInfo = dir.GetFiles();
            // 遍历文件
            foreach(FileInfo item in fileInfo)
            {
                if ((DateTime.Now - item.CreationTime).TotalDays > this.productionFileStorageLife)
                {
                    File.Delete(item.FullName);
                    Def.WriteLog("DeleteDirectoryFile()", $"{item.FullName} 超过{productionFileStorageLife}天，已被删除", LogType.Success);
                }
            }
            DirectoryInfo[] dirInfo = dir.GetDirectories();
            // 遍历文件夹
            foreach(DirectoryInfo item in dirInfo)
            {
                DeleteDirectoryFile(item);
            }
            // 删除空文件夹
            if((fileInfo.Length < 1) && (dirInfo.Length < 1))
            {
                Directory.Delete(dir.FullName);
            }
        }

        /// <summary>
        /// 删除数据库中超期的记录
        /// </summary>
        private void DeleteDataBaseRecord()
        {
            int formulaId = Def.GetProductFormula();
            DateTime startDT = new DateTime();
            DateTime endDT = DateTime.Now.AddDays(-this.productionFileStorageLife);
            string startTime = startDT.ToString(Def.DateFormal);
            string endTime = endDT.ToString(Def.DateFormal);
            string logEndTime = DateTime.Now.AddDays(-this.mcLogFileStorageLife).ToString(Def.DateFormal);

            if(DataBaseLog.DeleteDryingOvenLog(formulaId, -1, startTime, logEndTime))
            {
                Def.WriteLog("DeleteDataBaseRecord()", $"{DataBaseLog.LogTableType.DryingOvenLog} 表中{logEndTime}之前的记录已被删除", LogType.Success);
            }
            if(DataBaseLog.DeleteParameterLog(formulaId, -1, startTime, endTime))
            {
                Def.WriteLog("DeleteDataBaseRecord()", $"{DataBaseLog.LogTableType.ParameterLog} 表中{endTime}之前的记录已被删除", LogType.Success);
            }
            if(DataBaseLog.DeleteRobotLog(formulaId, -1, startTime, logEndTime))
            {
                Def.WriteLog("DeleteDataBaseRecord()", $"{DataBaseLog.LogTableType.RobotLog} 表中{logEndTime}之前的记录已被删除", LogType.Success);
            }
            if(DataBaseLog.DeleteMotorLog(formulaId, -1, startTime, logEndTime))
            {
                Def.WriteLog("DeleteDataBaseRecord()", $"{DataBaseLog.LogTableType.MotorLog} 表中{logEndTime}之前的记录已被删除", LogType.Success);
            }
            if(this.dbRecord.DeleteAlarmInfo(formulaId, -1, -1, startTime, endTime))
            {
                Def.WriteLog("DeleteDataBaseRecord()", $"{TableName[(int)TableType.TABLE_ALARM]} 表中{endTime}之前的记录已被删除", LogType.Success);
            }

            MesOperateMySql.DeleteRecord(startDT, endDT);
        }

        #endregion

        #region // 设备按钮及灯塔

        bool StartButtonOn()
        {
            foreach(var item in this.IStartButton)
            {
                if (item > -1 && DeviceManager.Inputs(item).IsOn())
                {
                    return true;
                }
            }
            return false;
        }

        bool StopButtonOn()
        {
            for(int i = 0; i < this.IStopButton.Length; i++)
            {
                if (((this.IEStopButton[i] > -1) && DeviceManager.Inputs(IEStopButton[i]).IsOn())
                    || (this.IStopButton[i] > -1) && DeviceManager.Inputs(IStopButton[i]).IsOn())
                {
                    return true;
                }
            }
            return false;
        }

        bool ResetButtonOn()
        {
            foreach(var item in this.IResetButton)
            {
                if(item > -1 && DeviceManager.Inputs(item).IsOn())
                {
                    return true;
                }
            }
            return false;
        }

        void SetTowerButton(MCState mcState)
        {
            if (Def.IsNoHardware())
            {
                return;
            }
            switch(mcState)
            {
                case MCState.MCIdle:
                case MCState.MCInitComplete:
                case MCState.MCStopInit:
                case MCState.MCStopRun:
                    {
                        // 按钮
                        for(int i = 0; i < this.OStartLed.Length; i++)
                        {
                            if(OStartLed[i] > -1)
                                DeviceManager.Outputs(OStartLed[i]).Off();
                            if(OStopLed[i] > -1)
                                DeviceManager.Outputs(OStopLed[i]).On();
                            if(OResetLed[i] > -1)
                                DeviceManager.Outputs(OResetLed[i]).Off();
                        }
                        // 灯塔
                        for(int i = 0; i < this.OLightTowerRed.Length; i++)
                        {
                            if(OLightTowerRed[i] > -1)
                                DeviceManager.Outputs(OLightTowerRed[i]).Off();
                            if(OLightTowerYellow[i] > -1)
                                DeviceManager.Outputs(OLightTowerYellow[i]).On();
                            if(OLightTowerGreen[i] > -1)
                                DeviceManager.Outputs(OLightTowerGreen[i]).Off();
                            if(OLightTowerBuzzer[i] > -1)
                                DeviceManager.Outputs(OLightTowerBuzzer[i]).Off();
                        }
                        break;
                    }
                case MCState.MCInitializing:
                case MCState.MCRunning:
                    {
                        // 有模组报警 || 有弹窗
                        bool hasMsg = (this.RunsCtrl.HasModuleAlarm() || this.RunsCtrl.HasModuleMessage() || this.hasMsgBox);

                        // 按钮
                        for(int i = 0; i < this.OStartLed.Length; i++)
                        {
                            if(OStartLed[i] > -1)
                                DeviceManager.Outputs(OStartLed[i]).On();
                            if(OStopLed[i] > -1)
                                DeviceManager.Outputs(OStopLed[i]).Off();
                            if(OResetLed[i] > -1)
                                DeviceManager.Outputs(OResetLed[i]).Off();
                        }
                        // 灯塔
                        for(int i = 0; i < this.OLightTowerRed.Length; i++)
                        {
                            if(OLightTowerRed[i] > -1)
                            {
                                if (hasMsg)
                                    DeviceManager.Outputs(OLightTowerRed[i]).On();
                                else
                                    DeviceManager.Outputs(OLightTowerRed[i]).Off();
                            }
                            if(OLightTowerYellow[i] > -1)
                                DeviceManager.Outputs(OLightTowerYellow[i]).Off();
                            if(OLightTowerGreen[i] > -1)
                                DeviceManager.Outputs(OLightTowerGreen[i]).On();
                            if(OLightTowerBuzzer[i] > -1)
                            {
                                if (hasMsg && DeviceManager.Outputs(OLightTowerBuzzer[i]).IsOff())
                                    DeviceManager.Outputs(OLightTowerBuzzer[i]).On();
                                else
                                    DeviceManager.Outputs(OLightTowerBuzzer[i]).Off();
                            }
                        }
                        break;
                    }
                case MCState.MCInitErr:
                case MCState.MCRunErr:
                    {
                        // 按钮
                        for(int i = 0; i < this.OStartLed.Length; i++)
                        {
                            if(OStartLed[i] > -1)
                                DeviceManager.Outputs(OStartLed[i]).Off();
                            if(OStopLed[i] > -1)
                                DeviceManager.Outputs(OStopLed[i]).Off();
                            if(OResetLed[i] > -1)
                                DeviceManager.Outputs(OResetLed[i]).On();
                        }
                        // 灯塔
                        for(int i = 0; i < this.OLightTowerRed.Length; i++)
                        {
                            if(OLightTowerRed[i] > -1)
                                DeviceManager.Outputs(OLightTowerRed[i]).On();
                            if(OLightTowerYellow[i] > -1)
                                DeviceManager.Outputs(OLightTowerYellow[i]).Off();
                            if(OLightTowerGreen[i] > -1)
                                DeviceManager.Outputs(OLightTowerGreen[i]).Off();
                            if(OLightTowerBuzzer[i] > -1)
                                DeviceManager.Outputs(OLightTowerBuzzer[i]).On();
                        }
                        break;
                    }
                    // 退出设备时关闭所有系统输出
                case MCState.NumMCState:
                    {
                        // 按钮
                        for(int i = 0; i < this.OStartLed.Length; i++)
                        {
                            if(OStartLed[i] > -1)
                                DeviceManager.Outputs(OStartLed[i]).Off();
                            if(OStopLed[i] > -1)
                                DeviceManager.Outputs(OStopLed[i]).Off();
                            if(OResetLed[i] > -1)
                                DeviceManager.Outputs(OResetLed[i]).Off();
                        }
                        // 灯塔
                        for(int i = 0; i < this.OLightTowerRed.Length; i++)
                        {
                            if(OLightTowerRed[i] > -1)
                                DeviceManager.Outputs(OLightTowerRed[i]).Off();
                            if(OLightTowerYellow[i] > -1)
                                DeviceManager.Outputs(OLightTowerYellow[i]).Off();
                            if(OLightTowerGreen[i] > -1)
                                DeviceManager.Outputs(OLightTowerGreen[i]).Off();
                            if(OLightTowerBuzzer[i] > -1)
                                DeviceManager.Outputs(OLightTowerBuzzer[i]).Off();
                        }
                        break;
                    }
            }
        }

        #endregion

        #region // 安全门

        /// <summary>
        /// 安全门监视
        /// </summary>
        private void SafeDoorMonitor(MCState mcState)
        {
            // 检查安全门
            //if(mcState > MCState.MCIdle)
            {
                this.SafeDoorState = CheckSafeDoorState();
            }
            SafeDoorCanOpen();
        }

        /// <summary>
        /// 检查安全门状态：true：已打开；false：关闭
        /// </summary>
        /// <returns>true：已打开；false：关闭</returns>
        private bool CheckSafeDoorState()
        {
            bool isOpen = false;
            bool mcStop = McStopState(RunsCtrl.GetMCState());
            // 模组所在设备配置的安全门
            for(int door = 0; door < (int)SystemIO.SafeDoorIO; door++)
            {
                if (SafeDoorIsOpen(door))
                {
                    // 空，则停止全部模组
                    if(string.IsNullOrEmpty(SafeDoorStopModule[door]))
                    {
                        if (!mcStop) RunsCtrl.Stop();
                        ShowMessageID((int)MsgID.DoorAlarm_0 + door);
                        return true;
                    }
                    else
                    {
                        foreach(var item in this.ListRuns)
                        {
                            if(SafeDoorStopModule[door].Contains(item.RunModule))
                            {
                                item.ShowMessageID((int)MsgID.DoorAlarm_0 + door);
                                isOpen = true;
                            }
                        }
                    }
                }
            }
            // 服务端的连接状态
            if (!ClientIsConnect())
            {
                return true;
            }
            // 服务端的安全门
            ModuleSocketData socketData = GetModuleSocketData(RunID.OnloadRobot);
            if (null != socketData)
            {
                for(int door = 0; door < (int)SystemIO.SafeDoorIO; door++)
                {
                    if(socketData.safeDoor[door])
                    {
                        if(!mcStop) RunsCtrl.Stop();
                        ShowMessageID((int)MsgID.DoorAlarm_0 + door);
                        return true;
                    }
                }
            }
            socketData = GetModuleSocketData(RunID.OffloadBattery);
            if(null != socketData)
            {
                for(int door = 0; door < (int)SystemIO.SafeDoorIO; door++)
                {
                    if(socketData.safeDoor[door])
                    {
                        if(!mcStop) RunsCtrl.Stop();
                        ShowMessageID((int)MsgID.DoorAlarm_0 + door);
                        return true;
                    }
                }
            }
            return isOpen;
        }

        /// <summary>
        /// 判断安全门是否已打开：true已打开，false已关闭
        /// </summary>
        /// <param name="doorIdx"></param>
        /// <returns></returns>
        public bool SafeDoorIsOpen(int doorIdx)
        {
            if (Def.IsNoHardware())
            {
                return false;
            }
            if (doorIdx > -1 && doorIdx < (int)SystemIO.SafeDoorIO)
            {
                if (ISafeDoorState[doorIdx] > -1)
                {
                    return DeviceManager.Inputs(ISafeDoorState[doorIdx]).IsOn();
                }
            }
            return false;
        }

        /// <summary>
        /// 安全门能否打开
        /// </summary>
        /// <param name="mcState"></param>
        private void SafeDoorCanOpen()
        {
            for(int idx = 0; idx < (int)SystemIO.SafeDoorIO; idx++)
            {
                // 无开门请求，上锁
                if ((OSafeDoorUnlock[idx] > -1) && DeviceManager.Outputs(OSafeDoorUnlock[idx]).IsOn())
                {
                    if ((ISafeDoorOpenBtn[idx] > -1) && DeviceManager.Inputs(ISafeDoorOpenBtn[idx]).IsOff())
                    {
                        DeviceManager.Outputs(OSafeDoorUnlock[idx]).Off();
                    }
                }
                if (ISafeDoorState[idx] > -1)
                {
                    bool canOpen = false;
                    if((ISafeDoorOpenBtn[idx] > -1) && DeviceManager.Inputs(ISafeDoorOpenBtn[idx]).IsOn())
                    {
                        for(int i = 0; i < 10; i++)
                        {
                            if (DeviceManager.Inputs(ISafeDoorOpenBtn[idx]).IsOff())
                            {
                                return;
                            }
                            Thread.Sleep(SafeDoorDelay[idx] / 10);
                        }
                        canOpen = true;
                    }
                    if (canOpen)
                    {
                        if (!ClientIsConnect())
                        {
                            return;
                        }
                        // 有设备在运行中
                        if(!McStopState(GetModuleMCState(RunID.OnloadRobot)))
                        {
                            string msg = "上料机器人设备非停止状态不能打开安全门！";
                            ShowMessageID((int)MsgID.OnloadSysRunning, msg, "请先停止设备运行后再操作", MessageType.MsgWarning);
                            return;
                        }
                        if(!McStopState(GetModuleMCState(RunID.Transfer)))
                        {
                            string msg = "调度机器人设备非停止状态不能打开安全门！";
                            ShowMessageID((int)MsgID.TransferSysRunning, msg, "请先停止设备运行后再操作", MessageType.MsgWarning);
                            return;
                        }
                        if(!McStopState(GetModuleMCState(RunID.OffloadBattery)))
                        {
                            string msg = "下料设备非停止状态不能打开安全门！";
                            ShowMessageID((int)MsgID.OffloadSysRunning, msg, "请先停止设备运行后再操作", MessageType.MsgWarning);
                            return;
                        }
                        if (GetRobotRunning(RunID.OnloadRobot))
                        {
                            string msg = "上料机器人运动中不能打开安全门！";
                            ShowMessageID((int)MsgID.RobotRun, msg, "请先等待机器人运动完成后再操作", MessageType.MsgWarning);
                            return;
                        }
                        if (GetRobotRunning(RunID.Transfer))
                        {
                            string msg = "调度机器人运动中不能打开安全门！";
                            ShowMessageID((int)MsgID.RobotRun + 1, msg, "请先等待机器人运动完成后再操作", MessageType.MsgWarning);
                            return;
                        }
                    }
                    if (OSafeDoorUnlock[idx] > -1)
                    {
                        if (canOpen)
                        {
                            DeviceManager.Outputs(OSafeDoorUnlock[idx]).On();
                        }
                        else
                        {
                            DeviceManager.Outputs(OSafeDoorUnlock[idx]).Off();
                        }
                    }
                    if (canOpen || DeviceManager.Inputs(ISafeDoorState[idx]).IsOff())
                    {
                        if(OSafeDoorOpenLed[idx] > -1)
                        {
                            DeviceManager.Outputs(OSafeDoorOpenLed[idx]).Off();
                        }
                        if(OSafeDoorCloseLed[idx] > -1)
                        {
                            DeviceManager.Outputs(OSafeDoorCloseLed[idx]).On();
                        }
                    }
                    else
                    {
                        if(OSafeDoorOpenLed[idx] > -1)
                        {
                            DeviceManager.Outputs(OSafeDoorOpenLed[idx]).On();
                        }
                        if(OSafeDoorCloseLed[idx] > -1)
                        {
                            DeviceManager.Outputs(OSafeDoorCloseLed[idx]).Off();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 设备停止状态
        /// </summary>
        /// <param name="mcState"></param>
        /// <returns></returns>
        public bool McStopState(MCState mcState)
        {
            if((MCState.MCIdle == mcState) || (MCState.MCStopInit == mcState)
                || (MCState.MCInitComplete == mcState) || (MCState.MCStopRun == mcState))
            {
                return true;
            }
            return false;
        }

        #endregion

        #region // 模组服务端，客户端

        public bool CreateServer()
        {
            if(false && Def.IsNoHardware())
            {
                return true;
            }
            bool result = true;
            this.machineServer.CloseServer();
            if(!this.machineServer.CreateServer(machineServerIP, machineServerPort))
            {
                ShowMsgBox.ShowDialog($"{machineServerIP}:{machineServerPort}】服务端创建失败！", MessageType.MsgWarning);
                result = false;
            }
            return result;
        }

        public bool ConnectClient()
        {
            if (!autoConnectCSState)
            {
                autoConnectCSState = true;
                if(false && Def.IsNoHardware())
                {
                    //autoConnectCSState = false;
                    return true;
                }
                for(int i = 0; i < this.machineClient.Count; i++)
                {
                    if(!this.machineClient[i].Connect(machineClientIP[i], machineClientPort[i]))
                    {
                        ShowMsgBox.ShowDialog($"{machineClientIP[i]}:{machineClientPort[i]}】服务器连接失败！", MessageType.MsgWarning);
                        autoConnectCSState = false;
                        return false;
                    }
                }
            }
            autoConnectCSState = false;
            return true;
        }

        public bool ClientIsConnect(bool almStop = false)
        {
            if(false && Def.IsNoHardware())
            {
                return true;
            }
            for(int i = 0; i < this.machineClient.Count; i++)
            {
                if(!this.machineClient[i].IsConnect())
                {
                    if (almStop)
                    {
                        RunsCtrl.Stop();
                        string msg = $"服务端{machineClientIP[i]}:{machineClientPort[i]}】连接断开！";
                        ShowMessageID((int)MsgID.ModuleDisconnect + i, msg, "请先在【调试工具-其它调试】重连模组服务端", MessageType.MsgAlarm);
                    }
                    return false;
                }
            }
            return true;
        }

        private async void AutoConnectClient()
        {
            await Task.Delay(1);

            for(int i = 0; i < this.machineClient.Count; i++)
            {
                if(!this.machineClient[i].Connect(machineClientIP[i], machineClientPort[i]))
                {
                    break;
                }
            }
            autoConnectCSState = false;
        }

        #endregion

        #region // Mes交互

        /// <summary>
        /// MES心跳线程
        /// </summary>
        private void HeartbeatThread()
        {
            while(this.monitorRunning)
            {
                try
                {
                    // MySql连接断开，则自动重连
                    if((DateTime.Now - mySqlCheckTime).TotalSeconds > 3)
                    {
                        if(!MesOperateMySql.MySqlIsOpen())
                        {
                            if(!MesOperateMySql.MySqlReconnect())
                            {
                                RunsCtrl.Stop();
                                ShowMessageID((int)MsgID.MySqlDisconnect, "MySql断开连接", "请确保上料工控机开机，然后等待MySql自动连接", MessageType.MsgAlarm);
                            }
                        }
                        mySqlCheckTime = DateTime.Now;
                    }
                    // 0413   注释
                    //HeartbeatRunWhile();
                }
                catch(System.Exception ex)
                {
                    Def.WriteLog("MachineCtrl", "HeartbeatThread()" + ex.Message, LogType.Error);
                }
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// MES心跳操作
        /// </summary>
        private void HeartbeatRunWhile()
        {
            if(GetInstance().UpdataMes)
            {
                string msg = "";
                bool result = false;
                // MES心跳
                if((DateTime.Now - heartbeatTime).TotalSeconds >= MesResources.HeartbeatInterval)
                {
                    for (int i = 0; i < (MesData.mesFrequency == 0 ? 3 : MesData.mesFrequency); i++)
                    {
                        if (!MesHeartbeat(heartbeatCount,ref msg))
                        {
                            //获取报警字符串是否包含"超时"二字,如果没有则跳出循环
                            if (!msg.Contains("超时"))
                            {
                                result = false;
                                break;
                            }
                            if (i == 2)
                            {
                                result = false;
                                ShowMsgBox.ShowDialog($"MES夹具解绑接口超时失败3次，请检查MES连接状态", MessageType.MsgAlarm);
                            }
                        }
                        else
                        {
                            result = true;
                            break;
                        }
                    }
                    //if (!MesHeartbeat(heartbeatCount))
                    if (!result)
                    {
                        heartbeatCount++;
                    }
                    else
                    {
                        this.heartbeatCount = 0;
                    }
                    heartbeatTime = DateTime.Now;
                }
            }
            else
            {
                Thread.Sleep(500);
            }

            if(mcOldState != RunsCtrl.GetMCState())
            {
                this.mcOldState = RunsCtrl.GetMCState();

                MesMCState mesMC = MesMCState.Stop;
                switch(this.mcOldState)
                {
                    case MCState.MCIdle:
                    case MCState.MCInitializing:
                    case MCState.MCStopInit:
                        mesMC = MesMCState.Stop;
                        break;
                    case MCState.MCRunning:
                        mesMC = MesMCState.Running;
                        break;
                    case MCState.MCInitComplete:
                    case MCState.MCStopRun:
                        mesMC = MesMCState.Waiting;
                        break;
                    case MCState.MCInitErr:
                    case MCState.MCRunErr:
                        mesMC = MesMCState.Alarm;
                        break;
                    default:
                        break;
                }
                switch(this.MachineID)
                {
                    case 0: // 上料
                        if (!MesOperateMySql.EquipmentReal(mesMC, MesResources.Onload)
                            || !MesOperateMySql.EquipmentOperation(mesMC, MesResources.Onload))
                        {
                            if(!MesOperateMySql.MySqlIsOpen())
                            {
                                ShowMessageID((int)MsgID.MySqlDisconnect, "MySql断开连接", "请确保上料工控机开机，然后等待MySql自动连接", MessageType.MsgAlarm);
                            }
                        }
                        break;
                    case 2: // 下料
                        if(!MesOperateMySql.EquipmentReal(mesMC, MesResources.Offload)
                            || !MesOperateMySql.EquipmentOperation(mesMC, MesResources.Offload))
                        {
                            if(!MesOperateMySql.MySqlIsOpen())
                            {
                                ShowMessageID((int)MsgID.MySqlDisconnect, "MySql断开连接", "请确保上料工控机开机，然后等待MySql自动连接", MessageType.MsgAlarm);
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// MES心跳测试
        /// </summary>
        /// <param name="count">心跳次数</param>
        /// <returns></returns>
        private bool MesHeartbeat(int count,ref string msg)
        {
            DateTime startTime = DateTime.Now;
            int result = -1;
            string mesSend = "";
            string mesReturn = "";
            string mesRecv = "";
            if (!MachineCtrl.GetInstance().UpdataMes)
            {
                return true;
            }
            MesInterface mes = MesInterface.Heartbeat;
            try
            {
                MesConfig mesCfg = MesDefine.GetMesCfg(mes);
                if(null == mesCfg)
                {
                    throw new Exception(mes + " is null.");
                }
                JObject mesData = JObject.Parse(JsonConvert.SerializeObject(new
                {
                    equipment_id = MesResources.Heartbeat.EquipmentID,
                    process_id = MesResources.Heartbeat.ProcessID,
                }));
                mesCfg.send = mesData.ToString();
                mesReturn = this.httpClient.Post(mesCfg.mesUri, mesData.ToString(),MesData.mesinterfaceTimeOut);
                mesRecv = MachineCtrl.RevertJsonString(mesReturn);
                if (null != mesReturn)
                {
                    mesCfg.recv = mesReturn;
                    mesCfg.updataRS = true;
                    JObject jsonReturn = JObject.Parse(mesReturn);
                    // 校验成功返回码0成功，1失败 
                    if(jsonReturn.ContainsKey("status_code"))
                    {
                        result = Convert.ToInt32(jsonReturn["status_code"]);
                        if(0 != result)
                        {
                            if (count >= 2)
                            {
                                msg = $"{MesDefine.GetMesTitle(mes)}异常，MES返回错误：{jsonReturn["message"]}\r\n";
                                MesLog.WriteLog(mes, $"post: {mesData.ToString(Formatting.None)}\r\n return : {mesReturn}");
                                ShowMessageID((int)MsgID.HeartbeatErr, msg, "请检查MES连接状态！", MessageType.MsgAlarm);
                            }
                            return false;
                        }
                        return true;
                    }
                }
                else
                {
                    MesLog.WriteLog(mes, $"post: {mesData.ToString(Formatting.None)}\r\n return null.");
                }
            }
            catch(System.Exception ex)
            {
                msg = $"MES心跳发生异常：{ex.Message}\r\n处理方法：请检查MES连接状态！";
                ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string text = $"{MesResources.Heartbeat.ProcessID},{MesResources.Heartbeat.EquipmentID},{startTime},{second},{DateTime.Now},{result},{msg},{mesSend},{mesRecv}";
                MachineCtrl.SaveLogData("MES心跳", text);
            }
            return false;
        }

        /// <summary>
        /// 工单信息获取
        /// </summary>
        /// <returns></returns>
        public bool MesGetBillNO(string equipmentID, string processID,ref string msg, out string billNo, out string billNum)
        {
            DateTime startTime = DateTime.Now;
            int result = -1;
            //string msg = "";
            string mesSend = "";
            string mesRecv = "";
            //string mesReturn = "";
            billNo = billNum = "";
            //if(!GetInstance().UpdataMes)
            //{
            //    return true;
            //}
            MesInterface mes = MesInterface.GetBillInfo;
            try
            {
                MesConfig mesCfg = MesDefine.GetMesCfg(mes);
                if(null == mesCfg)
                {
                    throw new Exception(mes + " is null.");
                }
                JObject mesData = JObject.Parse(JsonConvert.SerializeObject(new
                {
                    equipment_id = !string.IsNullOrEmpty(equipmentID)? equipmentID : MesResources.Onload.EquipmentID,
                    process_id = !string.IsNullOrEmpty(processID) ? processID : MesResources.Onload.ProcessID,
                }));
                mesCfg.send = mesData.ToString();
                string jsonRequest = JsonConvert.SerializeObject(mesData.ToString());
                mesSend = Regex.Replace(MachineCtrl.RevertJsonString(mesData.ToString()), @"\s", "");
                MesLog.WriteLog(mes, $"mes post: {mesData.ToString(Formatting.None)}", LogType.Information);
                // 离线保存
                if (!MachineCtrl.GetInstance().UpdataMes)
                {
                    MachineCtrl.GetInstance().SaveMesData(MesInterface.GetBillInfo, mesData.ToString());
                    return true;
                }
                string mesReturn = this.httpClient.Post(mesCfg.mesUri, mesData.ToString(), MesData.mesinterfaceTimeOut);
                mesRecv = Regex.Replace( MachineCtrl.RevertJsonString(mesReturn),@"\s","");
                //mesRecv = mesReturn.ToString().Replace("[", "").Replace("]", "").Replace("\r\n", "").Replace("\n","").Replace("\t","").Replace("\\", "").Replace(" ", "").Replace(@"""", "'");
                if (null != mesReturn)
                {
                    mesCfg.recv = mesReturn;
                    mesCfg.updataRS = true;
                    JObject jsonReturn = JObject.Parse(mesReturn);
                    //mesRecv = MachineCtrl.RevertJsonString(jsonReturn);
                    // 校验成功返回码0成功，1失败 
                    if (jsonReturn.ContainsKey("status_code"))
                    {
                        result = Convert.ToInt32(jsonReturn["status_code"]);
                        if(0 != result)
                        {
                            msg = $"{MesDefine.GetMesTitle(mes)}上传失败，MES返回错误：{jsonReturn["message"]}";
                            MesLog.WriteLog(mes, $"mes return : {mesReturn}");
                            ShowMsgBox.ShowDialog(msg + "\r\n处理方式：请检查MES数据参数！", MessageType.MsgAlarm);
                            return false;
                        }
                        else
                        {
                            MesResources.BillNo = billNo = jsonReturn["bill_no"].ToString();
                            MesResources.BillNum = billNum = jsonReturn["bill_num"].ToString();
                            MesResources.WriteConfig();
                        }
                        MesLog.WriteLog(mes, $"mes return : {mesReturn}", LogType.Success);
                        return true;
                    }
                }
                else
                {
                    MesLog.WriteLog(mes, $"mes return null.");
                }
            }
            catch(System.Exception ex)
            {
                msg = $"构造 {MesDefine.GetMesTitle(mes)} 数据错误：{ex.Message}";
                ShowMsgBox.ShowDialog(msg + "\r\n处理方式：请检查MES数据参数！", MessageType.MsgAlarm);
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string text = $"{MesResources.Group.ProcessID},{MesResources.Group.EquipmentID},{startTime},{second},{DateTime.Now},{result},{msg},{mesSend},{mesRecv}";
                MachineCtrl.SaveLogData("工单信息获取", text);
            }
            return false;
        }
        /// <summary>
        /// 工单队列获取
        /// </summary>
        /// <returns></returns>
        public bool MesGetBillInfoList(string equipmentID, string processID,ref MesInfo mesRecipeStruct,ref string msg)
        {
            DateTime startTime = DateTime.Now;
            int result = -1;
            string mesSend = "";
            string mesReturn = "";
            string mesRecv = "";
            int num = -1;
            //if (!GetInstance().UpdataMes)
            //{
            //    return true;
            //}
            MesInterface mes = MesInterface.GetBillInfoList;
            try
            {
                MesConfig mesCfg = MesDefine.GetMesCfg(mes);
                if (null == mesCfg)
                {
                    throw new Exception(mes + " is null.");
                }
                JObject mesData = JObject.Parse(JsonConvert.SerializeObject(new
                {
                    equipment_id = string.IsNullOrEmpty(equipmentID) ? MesResources.Onload.EquipmentID  : equipmentID,
                    process_id = string.IsNullOrEmpty(processID) ? MesResources.Onload.ProcessID  : processID,
                }));
                mesCfg.send = mesData.ToString();
                mesSend = Regex.Replace(MachineCtrl.RevertJsonString(mesData.ToString()), @"\s", "");
                MesLog.WriteLog(mes, $"mes post: {mesData.ToString(Formatting.None)}", LogType.Information);
                // 离线保存
                if (!MachineCtrl.GetInstance().UpdataMes)
                {
                    MachineCtrl.GetInstance().SaveMesData(MesInterface.GetBillInfoList, mesData.ToString());
                    return true;
                }
                mesReturn = this.httpClient.Post(mesCfg.mesUri, mesData.ToString(), MesData.mesinterfaceTimeOut);
                mesRecv = Regex.Replace(MachineCtrl.RevertJsonString(mesReturn), @"\s", "");
                if (null != mesReturn)
                {
                    mesCfg.recv = mesReturn;
                    mesCfg.updataRS = true;
                    JObject jsonReturn = JObject.Parse(mesReturn);

                    // 校验成功返回码0成功，1失败 
                    if (jsonReturn.ContainsKey("status_code"))
                    {
                        result = Convert.ToInt32(jsonReturn["status_code"]);
                        if (0 != result)
                        {
                            msg = $"{MesDefine.GetMesTitle(mes)}上传失败，MES返回错误：{jsonReturn["message"]}";
                            MesLog.WriteLog(mes, $"mes return : {mesReturn}");
                            ShowMsgBox.ShowDialog(msg + "\r\n处理方式：请检查MES数据参数！", MessageType.MsgAlarm);
                            return false;
                        }
                        else
                        {
                            //获取参数的数量
                            num = jsonReturn["data"].Count();

                            mesRecipeStruct.billInfo = new List<MesBillInfo>();
                            for (int i = 0; i < num; i++)
                            {
                                var str = jsonReturn["data"][i].ToString();
                                MesBillInfo InfoList = JsonConvert.DeserializeObject<MesBillInfo>(str);
                                MesBillInfo paramData = new MesBillInfo();
                                paramData.Bill_No = InfoList.Bill_No;
                                paramData.Bill_Num = InfoList.Bill_Num;
                                paramData.Unit = InfoList.Unit;
                                paramData.Bill_State = InfoList.Bill_State;

                                mesRecipeStruct.billInfo.Add(paramData);
                            }
                        }
                        MesLog.WriteLog(mes, $"mes return : {mesReturn}", LogType.Success);
                        return true;
                    }
                }
                else
                {
                    MesLog.WriteLog(mes, $"mes return null.");
                }
            }
            catch (System.Exception ex)
            {
                msg = $"构造 {MesDefine.GetMesTitle(mes)} 数据错误：{ex.Message}";
                ShowMsgBox.ShowDialog(msg + "\r\n处理方式：请检查MES数据参数！", MessageType.MsgAlarm);
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string text = $"{MesResources.Group.ProcessID},{MesResources.Group.EquipmentID},{startTime},{second},{DateTime.Now},{result},{msg},{mesSend},{mesRecv}";
                MachineCtrl.SaveLogData("工单队列获取", text);
            }
            return false;
        }

        /// <summary>
        /// 夹具解绑上传
        /// </summary>
        /// <param name="pltCode"></param>
        /// <returns></returns>
        public bool MesUnbindPalletInfo(string pltCode, ResourcesStruct rs,ref string msg)
        {
            DateTime startTime = DateTime.Now;
            int result = -1;
            string mesSend = "";
            string mesReturn = "";
            string mesRecv = "";
            //if (!GetInstance().UpdataMes)
            //{
            //    return true;
            //}
            MesInterface mes = MesInterface.TrayUnbundlingRecord;
            try
            {
                MesConfig mesCfg = MesDefine.GetMesCfg(mes);
                if(null == mesCfg)
                {
                    throw new Exception(mes + " is null.");
                }
                JObject mesData = JObject.Parse(JsonConvert.SerializeObject(new
                {
                    equipment_id = rs.EquipmentID,
                    process_id = rs.ProcessID,
                    traycode = pltCode,
                }));
                mesCfg.send = mesData.ToString();
                mesSend = Regex.Replace(MachineCtrl.RevertJsonString(mesData.ToString()), @"\s", "");
                MesLog.WriteLog(mes, $"mes post: {mesData.ToString(Formatting.None)}", LogType.Information);
                // 离线保存
                if (!MachineCtrl.GetInstance().UpdataMes)
                {
                    MachineCtrl.GetInstance().SaveMesData(MesInterface.TrayUnbundlingRecord, mesData.ToString());
                    return true;
                }
                mesReturn = this.httpClient.Post(mesCfg.mesUri, mesData.ToString(), MesData.mesinterfaceTimeOut);
                mesRecv = Regex.Replace(MachineCtrl.RevertJsonString(mesReturn), @"\s", "");
                if (null != mesReturn)
                {
                    mesCfg.recv = mesReturn;
                    mesCfg.updataRS = true;
                    JObject jsonReturn = JObject.Parse(mesReturn);
                    // 校验成功返回码0成功，1失败 
                    if(jsonReturn.ContainsKey("status_code"))
                    {
                        result = Convert.ToInt32(jsonReturn["status_code"]);
                        if(0 != result)
                        {
                            msg = $"{MesDefine.GetMesTitle(mes)}上传失败，MES返回错误：{jsonReturn["message"]}！";
                            MesLog.WriteLog(mes, $"mes return : {mesReturn}");
                            //ShowMessageID((int)MsgID.UnbindPltErr, msg, $"请检查{pltCode}是否需要解绑", MessageType.MsgAlarm);
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                            return false;
                        }
                        MesLog.WriteLog(mes, $"mes return : {mesReturn}", LogType.Success);
                        return true;
                    }
                }
                else
                {
                    MesLog.WriteLog(mes, $"mes return null.");
                }
            }
            catch(System.Exception ex)
            {
                msg = $"构造 {MesDefine.GetMesTitle(mes)} 数据错误：{ex.Message}\r\n请检查MES数据参数！";
                ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string text = $"{rs.ProcessID},{rs.EquipmentID},{startTime},{second},{DateTime.Now},{result},{msg},{mesSend},{mesRecv}";
                MachineCtrl.SaveLogData("夹具解绑上传", text);
            }
            return false;
        }

        /// <summary>
        /// 工艺参数获取
        /// </summary>
        /// <param name="getParam"></param>
        /// <returns></returns>
        public bool MesGetBillParameter(ref string msg)
        {
            DateTime startTime = DateTime.Now;
            int result = -1;
            string mesSend = "";
            string mesReturn = "";
            string mesRecv = "";
            //if (null != getParam)
            //    getParam.Clear();
            if (!MachineCtrl.GetInstance().UpdataMes)
            {
                return true;
            }
            MesInterface mes = MesInterface.ApplyTechProParam;
            try
            {
                MesConfig mesCfg = MesDefine.GetMesCfg(mes);
                if (null == mesCfg)
                {
                    throw new Exception(mes + " is null.");
                }
                JObject mesData = JObject.Parse(JsonConvert.SerializeObject(new
                {
                    equipment_id = MesResources.Group.EquipmentID,
                    process_id = MesResources.Group.ProcessID,
                    bill_no = MesResources.BillNo,
                }));
                mesCfg.send = mesData.ToString();
                if (!mesCfg.enable)
                {
                    MesLog.WriteLog(mes, $"stop.Need post: {mesData.ToString(Formatting.None)}", LogType.Information);
                    return true;
                }
                else
                {
                    MesLog.WriteLog(mes, $"mes post: {mesData.ToString(Formatting.None)}", LogType.Information);
                }
                mesReturn = this.httpClient.Post(mesCfg.mesUri, mesData.ToString(), MesData.mesinterfaceTimeOut);
                mesRecv = MachineCtrl.RevertJsonString(mesReturn);
                if (null != mesReturn)
                {
                    JObject jsonReturn = JObject.Parse(mesReturn);

                    mesCfg.recv = jsonReturn.ToString();
                    // 校验成功返回码0成功，1失败 
                    if (jsonReturn.ContainsKey("status_code"))
                    {
                        result = Convert.ToInt32(jsonReturn["status_code"]);
                        if (0 != result)
                        {
                            mesCfg.updataRS = true;     // 失败，置更新标记
                            msg = $"{MesDefine.GetMesTitle(mes)}上传失败，MES返回错误：{jsonReturn["message"]}\r\n处理方式：请检查MES数据参数！";
                            MesLog.WriteLog(mes, $"mes return : {mesReturn}");
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                            return false;
                        }
                        // 更新工艺参数

                        JArray items1 = (JArray)jsonReturn["data"];

                        MesRecipeStruct[] mesrecipe = new MesRecipeStruct[items1.Count];
                        mesCfg.parameter.Clear();
                        for (int i = 0; i < items1.Count; i++)
                        {
                            mesrecipe[i].FormulaNo = jsonReturn["data"][i]["formula_no"].ToString();
                            mesrecipe[i].ProductNo = jsonReturn["data"][i]["product_no"].ToString();
                            mesrecipe[i].ProductName = jsonReturn["data"][i]["product_name"].ToString();
                            mesrecipe[i].Version = jsonReturn["data"][i]["version"].ToString();
                            mesrecipe[i].DeliveryTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            mesrecipe[i].InUse = "unuse";

                            JArray dataParm = JArray.FromObject(jsonReturn["data"][i]["dataParm"]);
                            MesParameterData[] param = new MesParameterData[dataParm.Count];
                            for (int j = 0; j < dataParm.Count; i++)
                            {
                                param[i].ParamCode = jsonReturn["data"][i]["dataParm"][j]["param_code"].ToString();
                                param[i].ParamName = jsonReturn["data"][i]["dataParm"][j]["param_name"].ToString();
                                param[i].ParamValue = jsonReturn["data"][i]["dataParm"][j]["param_unit"].ToString();
                                param[i].ParamUpper = jsonReturn["data"][i]["dataParm"][j]["param_upper"].ToString();
                                param[i].ParamLower = jsonReturn["data"][i]["dataParm"][j]["param_value"].ToString();
                                param[i].ParamLower = jsonReturn["data"][i]["dataParm"][j]["param_lower"].ToString();
                            }
                            foreach (var item in mesrecipe)
                            {
                                if (mesCfg.parameter.ContainsKey(item.FormulaNo))
                                {
                                    mesCfg.parameter[item.FormulaNo] = item;
                                }
                                else
                                {
                                    mesCfg.parameter.Add(item.FormulaNo, item);
                                }
                            }
                            mesrecipe[i].Param = new List<MesParameterData>(param);

                            
                        }
                        MesDefine.WriteConfig(mes);
                        mesCfg.updataRS = true;     // 成功，最后置更新标记
                        MesLog.WriteLog(mes, $"mes return : {mesReturn}", LogType.Success);
                        return true;





                        //JArray data = JArray.FromObject(jsonReturn["data"]);
                        //foreach (var item in data)
                        //{
                        //    JObject paramJson = JObject.FromObject(item);

                        //    MesParameterStruct param = new MesParameterStruct();
                        //    param.Key = "";              // 映射的程序参数名
                        //    param.Code = "";             // 参数代码
                        //    param.Name = "";             // 参数名称
                        //    param.Unit = "";             // 参数单位
                        //    param.Upper = "";            // 参数设定值上限
                        //    param.Value = "";            // 参数设定值
                        //    param.Lower = "";            // 参数设定值下限

                        //    if (paramJson.ContainsKey("Parameter_code"))
                        //        param.Code = paramJson["Parameter_code"].ToString();
                        //    if (paramJson.ContainsKey("Parameter_name"))
                        //        param.Name = paramJson["Parameter_name"].ToString();
                        //    if (paramJson.ContainsKey("Parameter_upper"))
                        //        param.Upper = paramJson["Parameter_upper"].ToString();
                        //    if (paramJson.ContainsKey("Parameter_value"))
                        //        param.Value = paramJson["Parameter_value"].ToString();
                        //    if (paramJson.ContainsKey("Parameter_lower"))
                        //        param.Lower = paramJson["Parameter_lower"].ToString();
                        //    // 只更新已配置的下发参数
                        //    if (mesCfg.parameter.ContainsKey(param.Code))
                        //    {
                        //        param.Unit = mesCfg.parameter[param.Code].Unit;
                        //        param.Key = mesCfg.parameter[param.Code].Key;
                        //        mesCfg.parameter[param.Code] = param;

                        //        if (null != getParam)
                        //            getParam.Add("已更新配置", param);
                        //    }
                        //    else
                        //    {
                        //        if (null != getParam)
                        //            getParam.Add("未配置", param);
                        //    }
                        //}

                        //mesCfg.parameterDate = DateTime.Now.ToBinary();

                    }
                }
                else
                {
                    MesLog.WriteLog(mes, $"mes return null.");
                }
            }
            catch (System.Exception ex)
            {
                msg = $"构造 {MesDefine.GetMesTitle(mes)} 数据错误：{ex.Message}\r\n处理方式：请检查MES数据参数！";
                ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string text = $"{ MesResources.Group.ProcessID},{ MesResources.Group.EquipmentID},{startTime},{second},{DateTime.Now},{result},{msg},{mesSend},{mesRecv}";
                MachineCtrl.SaveLogData("工艺参数获取", text);
            }
            return false;
        }

        //public bool MesGetBillParameter(Dictionary<string, MesParameterStruct> getParam,ref string msg)
        //{
        //    DateTime startTime = DateTime.Now;
        //    int result = -1;
        //    string mesSend = "";
        //    string mesReturn = "";
        //    if (null != getParam)
        //        getParam.Clear();
        //    if(!MachineCtrl.GetInstance().UpdataMes)
        //    {
        //        return true;
        //    }
        //    MesInterface mes = MesInterface.ApplyTechProParam;
        //    try
        //    {
        //        MesConfig mesCfg = MesDefine.GetMesCfg(mes);
        //        if(null == mesCfg)
        //        {
        //            throw new Exception(mes + " is null.");
        //        }
        //        JObject mesData = JObject.Parse(JsonConvert.SerializeObject(new
        //        {
        //            equipment_id = MesResources.Group.EquipmentID,
        //            process_id = MesResources.Group.ProcessID,
        //            bill_no = MesResources.BillNo,
        //        }));
        //        mesCfg.send = mesData.ToString();
        //        if(!mesCfg.enable)
        //        {
        //            MesLog.WriteLog(mes, $"stop.Need post: {mesData.ToString(Formatting.None)}", LogType.Information);
        //            return true;
        //        }
        //        else
        //        {
        //            MesLog.WriteLog(mes, $"mes post: {mesData.ToString(Formatting.None)}", LogType.Information);
        //        }
        //        mesReturn = this.httpClient.Post(mesCfg.mesUri, mesData.ToString(), MesData.mesinterfaceTimeOut);
        //        if(null != mesReturn)
        //        {
        //            JObject jsonReturn = JObject.Parse(mesReturn);

        //            mesCfg.recv = jsonReturn.ToString();
        //            // 校验成功返回码0成功，1失败 
        //            if(jsonReturn.ContainsKey("status_code"))
        //            {
        //                result = Convert.ToInt32(jsonReturn["status_code"]);
        //                if(0 != result)
        //                {
        //                    mesCfg.updataRS = true;     // 失败，置更新标记
        //                    msg = $"{MesDefine.GetMesTitle(mes)}上传失败，MES返回错误：{jsonReturn["message"]}\r\n处理方式：请检查MES数据参数！";
        //                    MesLog.WriteLog(mes, $"mes return : {mesReturn}");
        //                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
        //                    return false;
        //                }
        //                // 更新工艺参数
        //                JArray data = JArray.FromObject(jsonReturn["data"]);
        //                foreach(var item in data)
        //                {
        //                    JObject paramJson = JObject.FromObject(item);

        //                    MesParameterStruct param = new MesParameterStruct();
        //                    param.Key = "";              // 映射的程序参数名
        //                    param.Code = "";             // 参数代码
        //                    param.Name = "";             // 参数名称
        //                    param.Unit = "";             // 参数单位
        //                    param.Upper = "";            // 参数设定值上限
        //                    param.Value = "";            // 参数设定值
        //                    param.Lower = "";            // 参数设定值下限

        //                    if(paramJson.ContainsKey("Parameter_code"))
        //                        param.Code = paramJson["Parameter_code"].ToString();
        //                    if(paramJson.ContainsKey("Parameter_name"))
        //                        param.Name = paramJson["Parameter_name"].ToString();
        //                    if(paramJson.ContainsKey("Parameter_upper"))
        //                        param.Upper = paramJson["Parameter_upper"].ToString();
        //                    if(paramJson.ContainsKey("Parameter_value"))
        //                        param.Value = paramJson["Parameter_value"].ToString();
        //                    if(paramJson.ContainsKey("Parameter_lower"))
        //                        param.Lower = paramJson["Parameter_lower"].ToString();
        //                    // 只更新已配置的下发参数
        //                    if(mesCfg.parameter.ContainsKey(param.Code))
        //                    {
        //                        param.Unit = mesCfg.parameter[param.Code].Unit;
        //                        param.Key = mesCfg.parameter[param.Code].Key;
        //                        mesCfg.parameter[param.Code] = param;

        //                        if(null != getParam)
        //                            getParam.Add("已更新配置", param);
        //                    }
        //                    else
        //                    {
        //                        if(null != getParam)
        //                            getParam.Add("未配置", param);
        //                    }
        //                }
        //                mesCfg.parameterDate = DateTime.Now.ToBinary();
        //                MesDefine.WriteConfig(mes);
        //                mesCfg.updataRS = true;     // 成功，最后置更新标记
        //                MesLog.WriteLog(mes, $"mes return : {mesReturn}", LogType.Success);
        //                return true;
        //            }
        //        }
        //        else
        //        {
        //            MesLog.WriteLog(mes, $"mes return null.");
        //        }
        //    }
        //    catch(System.Exception ex)
        //    {
        //        msg = $"构造 {MesDefine.GetMesTitle(mes)} 数据错误：{ex.Message}\r\n处理方式：请检查MES数据参数！";
        //        ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
        //    }
        //    finally
        //    {
        //        int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
        //        string text = $"{ MesResources.Group.ProcessID},{ MesResources.Group.EquipmentID},{startTime},{second},{DateTime.Now},{result},{msg},{mesSend},{mesReturn}";
        //        MachineCtrl.SaveLogData("工艺参数获取", text);
        //    }
        //    return false;
        //}

        /// <summary>
        /// 设备参数校验
        /// </summary>
        /// <returns></returns>
        public bool EquMesEPTechProParamFormalVerify(MesRecipeStruct mesRecipeStruct, string billNo, ref string msg)
        {
            DateTime startTime = DateTime.Now;
            int result = -1;
            string mesSend = "";
            string mesReturn = "";
            string mesRecv = "";
            if (!GetInstance().UpdataMes)
            {
                return true;
            }
            MesInterface mes = MesInterface.EPTechProParamFormalVerify;
            try
            {
                MesConfig mesCfg = MesDefine.GetMesCfg(mes);
                if (null == mesCfg)
                {
                    throw new Exception(mes + "is null");
                }
                JObject mesData = JObject.Parse(JsonConvert.SerializeObject(new
                {
                    process_id = MesResources.Group.ProcessID,
                    equipment_id = MesResources.Group.EquipmentID,
                    bill_no = billNo,


                }));
                JArray data = new JArray();

                //获取配方集合
                foreach (var item1 in mesRecipeStruct.Param)
                {
                    JObject bar = JObject.Parse(JsonConvert.SerializeObject(new
                    {
                        param_code = item1.ParamCode,    //参数代码
                        param_name = item1.ParamName,    //参数名称
                        param_unit = item1.ParamUnit,    //参数单位
                        param_upper = item1.ParamUpper,  //参数上限
                        param_value = item1.ParamValue,  //参数中值
                        param_lower = item1.ParamLower,  //参数下限
                    }));
                    data.Add(bar);
                }

                //设备参数添加
                mesData.Add(nameof(data), data);
                mesCfg.send = mesData.ToString();
                MesLog.WriteLog(mes, $"设备参数效验{mesCfg.send}", LogType.Success);
                mesReturn = this.httpClient.Post(mesCfg.mesUri, mesData.ToString(), MesData.mesinterfaceTimeOut);
                mesRecv = MachineCtrl.RevertJsonString(mesReturn);
                if (null != mesReturn)
                {
                    mesCfg.recv = mesReturn;
                    mesCfg.updataRS = true;
                    JObject jsonReturn = JObject.Parse(mesReturn);
                    //效验成功返回0
                    if (jsonReturn.ContainsKey("status_code"))
                    {
                        result = Convert.ToInt32("status_code");
                        if (0 != result)
                        {
                            msg = $"{MesDefine.GetMesTitle(mes)}上传失败，MES返回错误：{jsonReturn["message"]}！";
                            MesLog.WriteLog(mes, $"mes return : {mesReturn}");
                            ShowMessageID((int)MsgID.UnbindPltErr, msg, $"请检查配方是否正确", MessageType.MsgAlarm);
                            return false;
                        }
                        MesLog.WriteLog(mes, $"mes return : {mesReturn}", LogType.Success);
                        return true;
                    }
                }
                else
                {
                    MesLog.WriteLog(mes, $"mes return null.");
                }
            }
            catch (Exception ex)
            {
                msg = $"构造 {MesDefine.GetMesTitle(mes)} 数据错误：{ex.Message}\r\n请检查MES数据参数！";
                ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string text = $"{ MesResources.Group.ProcessID},{ MesResources.Group.EquipmentID},{startTime},{second},{DateTime.Now},{result},{msg},{mesSend},{mesRecv}";
                MachineCtrl.SaveLogData("设备参数校验", text);
            }
            return false;

        }

        /// <summary>
        /// 配方效验
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="tf"></param>
        /// <returns></returns>
        public bool EquMesTechProParamFormalVerify(ResourcesStruct rs,string Formula_No, string Product_No,string Versions, ref string msg)
        {
            DateTime startTime = DateTime.Now;
            int result = -1;
            string mesSend = "";
            string mesRecv = "";
            string mesReturn = "";
            if (!GetInstance().UpdataMes)
            {
                return true;
            }
            MesInterface mes = MesInterface.TechProParamFormalVerify;
            try
            {
                MesConfig mesCfg = MesDefine.GetMesCfg(mes);
                MesConfig cfg = MesDefine.GetMesCfg(MesInterface.ApplyTechProParam);
                if (null == mesCfg)
                {
                    throw new Exception(mes + "is null");
                }

                JObject mesData = JObject.Parse(JsonConvert.SerializeObject(new
                {
                    process_id = MesResources.Group.ProcessID,
                    equipment_id = MesResources.Group.EquipmentID,
                    formula_no = Formula_No,
                    product_no = Product_No,
                    version = Versions,
                }));

                JArray data = new JArray();
                foreach (var item in cfg.parameter.Values)
                {
                    if (item.FormulaNo == Formula_No)
                    {
                        //获取配方集合
                        foreach (var item1 in item.Param)
                        {
                            JObject bar = JObject.Parse(JsonConvert.SerializeObject(new
                            {
                                param_code = item1.ParamCode,    //参数代码
                                param_name = item1.ParamName,    //参数名称
                                param_unit = item1.ParamUnit,    //参数单位
                                param_upper = item1.ParamUpper,  //参数上限
                                param_value = item1.ParamValue,  //参数中值
                                param_lower = item1.ParamLower,  //参数下限
                            }));
                            data.Add(bar);
                        }
                    }
                }
                //配方集合添加
                mesData.Add(nameof(data), data);

                mesCfg.send = mesData.ToString();
                mesSend = MachineCtrl.RevertJsonString(mesCfg.send);
                MesLog.WriteLog(mes, $"配方效验{mesCfg.send}", LogType.Success);
                mesReturn = this.httpClient.Post(mesCfg.mesUri, mesData.ToString(), MesData.mesinterfaceTimeOut);
                mesRecv = MachineCtrl.RevertJsonString(mesReturn);
                if (null != mesReturn)
                {
                    mesCfg.recv = mesReturn;
                    mesCfg.updataRS = true;
                    JObject jsonReturn = JObject.Parse(mesReturn);
                    //效验成功返回0
                    if (jsonReturn.ContainsKey("status_code"))
                    {
                        result = Convert.ToInt32("status_code");
                        if (0 != result)
                        {
                            msg = $"{MesDefine.GetMesTitle(mes)}上传失败，MES返回错误：{jsonReturn["message"]}！";
                            MesLog.WriteLog(mes, $"mes return : {mesReturn}");
                            ShowMessageID((int)MsgID.UnbindPltErr, msg, $"请检查配方是否正确", MessageType.MsgAlarm);
                            return false;
                        }
                        MesLog.WriteLog(mes, $"mes return : {mesReturn}", LogType.Success);
                        return true;
                    }
                }
                else
                {
                    MesLog.WriteLog(mes, $"mes return null.");
                }
            }
            catch (Exception ex)
            {
                msg = $"构造 {MesDefine.GetMesTitle(mes)} 数据错误：{ex.Message}\r\n请检查MES数据参数！";
                ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string text = $"{MesResources.Onload.ProcessID},{MesResources.Onload.EquipmentID},{startTime},{second},{DateTime.Now},{result},{msg},{mesSend},{mesRecv}";
                MachineCtrl.SaveLogData("配方校验", text);
            }
            return false;

        }

        /// <summary>
        /// MES状态修改校验
        /// </summary>
        /// <returns></returns>
        public bool MesModifyCheck()
        {
            UserFormula userInfo = new UserFormula();
            GetInstance().dbRecord.GetCurUser(ref userInfo);

            //if (string.IsNullOrEmpty(userInfo.userName) || !userInfo.userName.Equals("MES", StringComparison.OrdinalIgnoreCase))
            if (string.IsNullOrEmpty(userInfo.userName)||!(userInfo.userName.Contains("MES")))
            {
                ShowMsgBox.ShowDialog("请先切换为MES用户后再操作MES设置", MessageType.MsgWarning);
                return false;
            }
            return true;
        }

        private static string fileName = "MESLog";

        /// <summary>
        /// 转换字符串   added by nico 2021.12.21
        /// 
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static string RevertJsonString(string json)
        {
            string retValue = "";
            string[] Aarray = json.Split('\"');
            foreach (var item in Aarray)
            {
                retValue = retValue + "\"" + item + "\"";
            }
            return retValue;
        }

        /// <summary>
        /// 判断入站信息日志是否更新
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool GetPullInExCsvFileState(ref string filePath)
        {
            if (false == PullInLogHasChanged)
            {
                filePath = "";
                return false;
            }
            PullInLogHasChanged = false;
            filePath = PullInExCsvFilePath;
            return true;
        }

        /// <summary>
        /// 判断出站信息日志是否更新
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool GetOutExCsvFileState(ref string filePath)
        {
            if (false == OutLogHasChanged)
            {
                filePath = "";
                return false;
            }
            OutLogHasChanged = false;
            filePath = OutExCsvFilePath;
            return true;
        }

        /// <summary>
        /// 保存通用日志
        /// </summary>
        /// <param name="text"></param>
        public static void SaveLogData(string strName, string text)
        {
            string file, title;
            file = string.Format(@"{0}\{1}\{2}\{3}.csv"
                    , MachineCtrl.GetInstance().ProductionFilePath, fileName, strName, DateTime.Now.ToString("yyyy-MM-dd"));
            title = "工序编号,设备编码,请求时间,耗时(ms),响应时间,返回代码,返回信息,请求JSON,返回JSON";

            Def.ExportCsvFile(file, title, (text + "\r\n"));
        }



        /// <summary>
        /// MES进站接口日志保存
        /// </summary>
        /// <param name="strName"></param>
        /// <param name="text"></param>
        public static void SaveLogPullInData(string strName, string text)
        {
            string file, title;
            file = string.Format(@"{0}\{1}\{2}\{3}.csv"
                    , MachineCtrl.GetInstance().ProductionFilePath, fileName, strName, DateTime.Now.ToString("yyyy-MM-dd"));
            title = "电芯条码,请求时间,耗时(ms),响应时间,工序编码,设备编码,返回代码,返回信息,工单号,工单数量,特殊值,请求JSON,返回JSON";
            PullInExCsvFilePath = file;
            PullInLogHasChanged = true;
            Def.ExportCsvFile(file, title, (text + "\r\n"));
        }

        /// <summary>
        /// MES出站接口日志保存
        /// </summary>
        /// <param name="strName"></param>
        /// <param name="text"></param>
        public static void SaveLogOutData(string strName, string text)
        {
            string file, title;
            file = string.Format(@"{0}\{1}\{2}\{3}.csv"
                    , MachineCtrl.GetInstance().ProductionFilePath, fileName, strName, DateTime.Now.ToString("yyyy-MM-dd"));
            title = "电芯条码,请求时间,耗时(ms),响应时间,工序编号,设备编码,工单号,操作员,工步(可选),是否扣料(可选),数量,返回代码,返回信息,不良代码,不良原因,请求JSON,返回JSON";
            OutExCsvFilePath = file;
            OutLogHasChanged = true;
            Def.ExportCsvFile(file, title, (text + "\r\n"));
        }

        public bool GetMesProcessName(MesInterface mes, ref string strMesName)
        {
            switch (mes)
            {
                case MesInterface.GetBillInfo:
                    {
                        strMesName = "工单信息获取";
                        break;
                    }
                case MesInterface.GetBillInfoList:
                    {
                        strMesName = "工单队列获取";
                        break;
                    }
                case MesInterface.TrayVerifity:
                    {
                        strMesName = "夹具校验";
                        break;
                    }
                case MesInterface.BakingMaterialVerifity:
                    {
                        strMesName = "入站校验";
                        break;
                    }
                case MesInterface.SaveTrayAndBarcodeRecord:
                    {
                        strMesName = "绑盘上传";
                        break;
                    }
                case MesInterface.TrayUnbundlingRecord:
                    {
                        strMesName = "解绑上传";
                        break;
                    }
                case MesInterface.SavePR_ProductRecordList:
                    {
                        strMesName = "生产履历";
                        break;
                    }
                case MesInterface.SaveBakingResultRecord:
                    {
                        strMesName = "Baking开始与结束";
                        break;
                    }
            }
            return true;
        }

        /// <summary>
        /// 保存MES数据，离线上传
        /// </summary>
        /// <param name="mes"></param>
        /// <param name="mesData"></param>
        /// <returns></returns>
        public bool SaveMesData(MesInterface mes, string mesData)
        {
            if (null == this.mesFileLock)
            {
                return false;
            }
            bool result = false;
            lock (this.mesFileLock)
            {
                string mesProcessName = "";
                GetMesProcessName(mes, ref mesProcessName);
                string fileName = string.Format("{0}\\MES离线上传\\{1}\\{2}.mes", GetInstance().ProductionFilePath
                    , mesProcessName, DateTime.Now.ToString("yyyy-MM-dd"));
                string offlinefilename = string.Format("{0}\\MES离线上传\\{1}\\{2}.mes", GetInstance().ProductionFilePath
                    , mesProcessName, "offlinedata");
                Def.WriteText(fileName, mesData);
                Def.WriteText(offlinefilename, mesData);
                result = true;
            }
            return result;
        }

        #endregion

    }
}
