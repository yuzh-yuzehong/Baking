using HelperLibrary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json.Converters;
using System.Linq;
using System.Text.RegularExpressions;

namespace Machine
{
    /// <summary>
    /// 干燥炉
    /// </summary>
    class RunProcessDryingOven : RunProcess
    {
        #region // 枚举：步骤，模组数据，报警列表

        protected new enum InitSteps
        {
            Init_DataRecover = 0,

            Init_ConnectDryOven,
            Init_CloseDryOvenDoor,
            Init_OpenDryOvenDoor,
            Init_CheckDryOvenDoor,

            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,

            // 开门取放
            Auto_PrecloseOvenDoor,          // 关闭非开门层的炉门
            Auto_PreblowAir,                // 开门前破真空：仅破当前炉腔
            Auto_OpenOvenDoor,              // 打开炉门
            Auto_CheckPalletState,          // 取放前检查夹具状态
            Auto_WaitActionFinish,          // 等待动作完成
            Auto_FinishedCheckPltState,     // 完成后检查夹具状态
            Auto_CloseOvenDoor,             // 关闭炉门
            Auto_UpdateMesBindCavity,       // 上传绑炉腔信息

            // 启动加热
            Auto_SetOvenWorkStop,           // 发送当前层工作停止
            Auto_CheckPressure,             // 启动前检查真空值
            Auto_SendWorkParameter,         // 发送参数
            Auto_SetOvenWorkStart,          // 发送启动命令
            Auto_UpdateMesWorkStart,        // 上传启动信息

            Auto_WorkEnd,
        }

        public enum BakingType
        {
            Normal_start,     //正常开始
            Normal_End,       //正常结束
            Abnormal_start,   //异常开始
            Abnormal_End,     //异常结束
        }

        private enum MsgID
        {
            Start = ModuleMsgID.DryingOvenMsgStartID,
            CheckDoor,
            RobotFingerIn,
            DoorOpenClose,
            VacOpenClose,
            BlowOpenClose,
            PressureOpenClose,
            WorkStartStop,
            SetParameter,
            FaultReset,
            SetMcDoor,
            DoorAlarm,
            VacAlarm = DoorAlarm + OvenRowCol.MaxRow,
            BlowAlarm = VacAlarm + +OvenRowCol.MaxRow,
            VacuometerAlarm = BlowAlarm + OvenRowCol.MaxRow,
            ControlAlarm = VacuometerAlarm + OvenRowCol.MaxRow * OvenRowCol.MaxCol,
            PltCheckAlarm = ControlAlarm + OvenRowCol.MaxRow * OvenRowCol.MaxCol,
            TempAlarm = PltCheckAlarm + OvenRowCol.MaxRow,
            HeatStop = TempAlarm + OvenRowCol.MaxRow,
            HeatTimeout= HeatStop + OvenRowCol.MaxRow,
            HeatVacAlarm= HeatTimeout + OvenRowCol.MaxRow,
            WorkingOpenDoor,
            WorkingVacuum,
            WorkingBlowAir,
            WorkingPressure,
            WorkingSetParameter,
            OpenDoorPressureAlm,
            OpenMultiDoorAlm,
            PltStateErr,
            RemoteErr,
            OvenDataError,
            BindCavityErr,
            BakingStatusErr,
            WaterValueErr,
            GetBillParaErr,
            ProductionRecordErr,
            RejectNGErr,
            FTPUploadErr,
            MySqlDisconnect,
            WaterValueExceed,
        }

        #endregion

        #region // 取放位置结构体

        private struct PickPlacePos
        {
            #region // 字段
            public int row;
            public int col;
            public EventList operateEvent;
            #endregion

            #region // 方法

            public void SetData(int curRow, int curCol, EventList curEvent)
            {
                this.row = curRow;
                this.col = curCol;
                this.operateEvent = curEvent;
            }

            public void Release()
            {
                this.row = -1;
                this.col = -1;
                this.operateEvent = EventList.Invalid;
            }
            #endregion
        };
        #endregion

        #region // 字段，属性

        #region // IO
        #endregion

        #region // 电机
        #endregion

        #region // ModuleEx.cfg配置
        private int dryingOvenID;           // 干燥炉ID
        #endregion

        #region // 模组参数

        public bool[] CavityEnable { get; private set; }        // 腔体使能：true启用，false禁用
        public bool[] CavityPressure { get; private set; }      // 腔体保压：true启用，false禁用
        public bool[] CavityTransfer { get; private set; }      // 腔体转移：true启用，false禁用
        public int[] CavitySamplingCycle { get; private set; }  // 腔体抽检周期：每N次放一次假电池夹具抽检
        public int[] CavityHeartCycle { get; private set; }     // 腔体加热次数：当前第N次加热

        private int dryingOvenGroup;                    // 干燥炉分组：0左靠近上料，1右靠近上料
        private string localIP;                         // 本机IP
        private string ovenIP;                          // 干燥炉IP
        private int ovenPort;                           // 干燥炉IP的Port
        private CavityParameter cavityParameter;        // 干燥炉工艺参数
        private double waterStandardAnode;              // 阳极水含量标准值：<则合格，>则超标重新回炉干燥
        private double waterStandardCathode;            // 阴极水含量标准值：<则合格，>则超标重新回炉干燥
        private int openDoorDelay;                      // 开门防呆时间：秒
        private int maxWorkTimeRange;                   // 最大工作时间误差范围：分钟min
        private int workDataTime;                       // 加热时数据保存时间间隔：秒
        private bool waitResultPressure;                // 等待测试结果时自动保压：true启用，false禁用

        #endregion

        #region // 模组数据

        public CavityStatus[] CavityState{ get; private set; }             // 腔体状态
        public List<List<List<double>>> PltHeatTemp { get; private set; }  // 夹具加热温度：夹具<发热板<温度值<>>>
        public List<List<List<uint>>> PltHeatTime { get; private set; }    // 夹具加热时间：夹具<发热板<时间值<>>>

        private PickPlacePos operatePos;                // 当前操作位置
        private double[,] waterContentValue;            // 水含量值：炉层,3个水含量（2阴极1阳极）
        private DryingOvenData readOvenData;            // 读取干燥炉数据
        private DryingOvenData writeOvenData;           // 写入干燥炉数据
        private DateTime[] bakingDataStartTime;         // 腔体开始保存干燥数据时间
        private DryingOvenClient ovenClient;            // 干燥炉连接
        private int lineID;                             // 线体ID：从MachineCtrl获取
        private Task runWhileTask;                      // 任务运行线程
        private CavityStatus[] CavityOldState;          // 腔体上一次状态，MES用
        private bool[] mesUpdataState;                  // MES接口上传状态：true成功，false未成功
        private BakingNGType[] bakingNGType;

        private static bool OutLogHasChanged;
        private static string OutExCsvFilePath;

        #endregion

        #endregion

        #region // 构造析构

        public RunProcessDryingOven(int runId) : base(runId)
        {
            InitBatteryPalletSize(0, (int)ModuleMaxPallet.DryingOven);

            PowerUpRestart();

            this.ovenClient = new DryingOvenClient();
            //this.ovenLogFile = new LogFile();

            InitParameter();
            // 参数
            InsertGroupParameter("DryingOvenGroup", "干燥炉分组", "干燥炉分组：0左靠近上料，1右靠近上料", dryingOvenGroup, RecordType.RECORD_INT, ParameterLevel.PL_IDLE_ADMIN);
            for(int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            {
                InsertVoidParameter(("CavityEnable" + i), ((i + 1) + "层腔体使能"), "腔体使能：true启用，false禁用", CavityEnable[i], RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_OPER);
            }
            for(int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            {
                InsertVoidParameter(("CavityPressure" + i), ((i + 1) + "层腔体保压"), "腔体保压：true启用，false禁用", CavityPressure[i], RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_OPER);
            }
            for(int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            {
                InsertVoidParameter(("CavityTransfer" + i), ((i + 1) + "层腔体转移"), "腔体转移：true启用，false禁用", CavityTransfer[i], RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            }
            for(int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            {
                InsertVoidParameter(("CavitySamplingCycle" + i), ((i + 1) + "腔体抽检周期"), "腔体抽检周期：每N次放一次假电池夹具抽检", CavitySamplingCycle[i], RecordType.RECORD_INT);
            }
            InsertVoidParameter("waitResultPressure", "测试后保压", "测试水含量后自动保压：true启用，false禁用", waitResultPressure, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_OPER);
            InsertVoidParameter("waterStandardAnode", "阳极水含量标准", "阳极水含量标准值：<则合格，>则超标重新回炉干燥", waterStandardAnode, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("waterStandardCathode", "阴极水含量标准", "阴极水含量标准值：<则合格，>则超标重新回炉干燥", waterStandardCathode, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("workDataTime", "加热数据间隔", "加热时数据保存时间间隔：秒", workDataTime, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("openDoorDelay", "开门防呆时间", "开门防呆时间：秒", openDoorDelay, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("maxWorkTimeRange", "工作时间误差", "最大工作时间误差范围：分钟", maxWorkTimeRange, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            // 以下较为固定参数放最后
            InsertVoidParameter("SetTempValue", "1)设定温度", "1)设定温度：摄氏度", cavityParameter.SetTempValue, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("TempUpperlimit", "2)温度上限", "2)温度上限：摄氏度", cavityParameter.TempUpperlimit, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("TempLowerlimit", "3)温度下限", "3)温度下限：摄氏度", cavityParameter.TempLowerlimit, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("PreheatTime", "4)预热时间", "4)预热时间：分钟", cavityParameter.PreheatTime, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("VacHeatTime", "5)加热时间", "5)真空加热时间：分钟", cavityParameter.VacHeatTime, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("OpenDoorBlowTime", "6)开门破真空时长", "6)开门破真空时长：分钟", cavityParameter.OpenDoorBlowTime, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("OpenDoorVacPressure", "7)开门真空压力", "7)开门真空压力：Pa", cavityParameter.OpenDoorVacPressure, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("AStateVacTime", "8)A状态抽真空时间", "8)A状态抽真空时间：分钟", cavityParameter.AStateVacTime, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("AStateVacPressure", "9)A状态真空压力", "9)A状态真空压力：Pa", cavityParameter.AStateVacPressure, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("BStateVacTime", "10)B状态抽真空时间", "10)B状态抽真空时间：分钟", cavityParameter.BStateVacTime, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("BStateVacPressure", "11)B状态真空压力", "11)B状态真空压力：Pa", cavityParameter.BStateVacPressure, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("BStateBlowAirTime", "12)呼吸充干燥气时间", "12)呼吸充干燥气时间：分钟", cavityParameter.BStateBlowAirTime, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("BStateBlowAirPressure", "13)呼吸充干燥气压力", "13)呼吸充干燥气压力：Pa", cavityParameter.BStateBlowAirPressure, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("BStateBlowAirKeepTime", "14)呼吸充干燥气保持时间", "14)呼吸充干燥气保持时间：分钟", cavityParameter.BStateBlowAirKeepTime, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("BreathTimeInterval", "15)呼吸时间间隔", "15)呼吸时间间隔：分钟", cavityParameter.BreathTimeInterval, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("BreathCycleTimes", "16)呼吸循环次数", "16)呼吸循环次数：次", cavityParameter.BreathCycleTimes, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("HeatPlate", "17)发热板数", "17)发热板数：块", cavityParameter.HeatPlate, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("MaxNGHeatPlate", "18)最大NG发热板", "18)最大NG发热板数：块", cavityParameter.MaxNGHeatPlate, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("HeatPreVacTime", "19)加热前抽真空时间", "19)加热前抽真空时间：分钟", cavityParameter.HeatPreVacTime, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("HeatPreBlow", "20)加热前充干燥气压力", "20)加热前充干燥气压力：Pa", cavityParameter.HeatPreBlow, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);

            InsertVoidParameter("ovenIP", "干燥炉IP", "干燥炉IP", ovenIP, RecordType.RECORD_STRING, ParameterLevel.PL_IDLE_ADMIN);
            InsertVoidParameter("ovenPort", "干燥炉端口", "干燥炉IP的Port", ovenPort, RecordType.RECORD_INT, ParameterLevel.PL_IDLE_ADMIN);
        }

        ~RunProcessDryingOven()
        {
            ReleaseThread();
        }
        
        #endregion

        #region // 模组运行

        protected override void PowerUpRestart()
        {
            base.PowerUpRestart();
            CurMsgStr("准备好", "Ready");
            
            InitRunData();
        }

        protected override void InitOperation()
        {
            if(!IsModuleEnable())
            {
                InitFinished();
                return;
            }

            switch((InitSteps)this.nextInitStep)
            {
                case InitSteps.Init_DataRecover:
                    {
                        CurMsgStr("数据恢复", "Data recover");
                        if(MachineCtrl.GetInstance().DataRecover)
                        {
                            LoadRunData();
                        }
                        if (!Def.IsNoHardware())
                        {
                            this.DryRun = false;
                        }
                        this.nextInitStep = InitSteps.Init_ConnectDryOven;
                        break;
                    }

                case InitSteps.Init_ConnectDryOven:
                    {
                        CurMsgStr("连接干燥炉", "Connect drying oven");
                        if (this.DryRun || DryOvenConnect(true))
                        {
                            Sleep(1000);
                            this.nextInitStep = InitSteps.Init_CloseDryOvenDoor;
                        }
                        break;
                    }
                case InitSteps.Init_CloseDryOvenDoor:
                    {
                        for(int i = 0; i < (int)OvenRowCol.MaxRow; i++)
                        {
                            this.msgChs = string.Format("关闭{0}层干燥炉炉门", i + 1);
                            this.msgEng = string.Format("Close drying oven door {0}", i + 1);
                            CurMsgStr(this.msgChs, this.msgEng);
                            // 关门时不能在当前炉层进
                            if(WCavity(i).doorState != (short)OvenStatus.DoorOpen)
                            {
                                if (CheckRobotTransferSafe(i))
                                {
                                    WriteLog("InitOperation()操作：" + this.msgChs);
                                    CavityData cavity = new CavityData();
                                    cavity.doorState = (short)OvenStatus.DoorClose;
                                    if(!this.DryRun && !DryOvenOpenDoor(i, cavity, true))
                                    {
                                        return;
                                    }
                                }
                                else
                                {
                                    ShowMessageBox((int)MsgID.DoorOpenClose, "调度机器人插料架在此干燥炉中，不能开关炉门", "请先移出调度机器人插料架后再启动", MessageType.MsgWarning);
                                    return;
                                }
                            }
                        }
                        this.nextInitStep = InitSteps.Init_OpenDryOvenDoor;
                        break;
                    }
                case InitSteps.Init_OpenDryOvenDoor:
                    {
                        for(int i = 0; i < (int)OvenRowCol.MaxRow; i++)
                        {
                            this.msgChs = string.Format("打开{0}层干燥炉炉门", i + 1);
                            this.msgEng = string.Format("Open drying oven door {0}", i + 1);
                            CurMsgStr(this.msgChs, this.msgEng);
                            // 开门时不能在任何一层进
                            if((WCavity(i).doorState == (short)OvenStatus.DoorOpen) && (RCavity(i).doorState != (short)OvenStatus.DoorOpen))
                            {
                                if (CheckRobotTransferSafe(-1))
                                {
                                    WriteLog("InitOperation()操作：" + this.msgChs);
                                    CavityData cavity = new CavityData();
                                    cavity.doorState = (short)OvenStatus.DoorOpen;
                                    if(!this.DryRun && !DryOvenOpenDoor(i, cavity, true))
                                    {
                                        return;
                                    }
                                }
                                else
                                {
                                    ShowMessageBox((int)MsgID.DoorOpenClose, "调度机器人插料架在此干燥炉中，不能开关炉门", "请先移出调度机器人插料架后再启动", MessageType.MsgWarning);
                                    return;
                                }
                            }
                        }
                        this.nextInitStep = InitSteps.Init_CheckDryOvenDoor;
                        break;
                    }
                case InitSteps.Init_CheckDryOvenDoor:
                    {
                        for(int i = 0; i < (int)OvenRowCol.MaxRow; i++)
                        {
                            this.msgChs = string.Format("检查{0}层干燥炉炉门", i + 1);
                            this.msgEng = string.Format("Check drying oven door {0}", i + 1);
                            CurMsgStr(this.msgChs, this.msgEng);
                            if(!this.DryRun && ((short)OvenStatus.Unknown != WCavity(i).doorState) && (WCavity(i).doorState != RCavity(i).doorState))
                            {
                                string doorState = ((short)OvenStatus.DoorOpen == WCavity(i).doorState) ? "打开" : "关闭";
                                string msg = string.Format("{0}层炉门状态不正确，应该是{1}", i + 1, doorState);
                                string dispose = string.Format("请先将{0}层炉门手动恢复到【{1}】后再继续", i + 1, doorState);
                                ShowMessageBox((int)MsgID.CheckDoor, msg, dispose, MessageType.MsgWarning);
                                return;
                            }
                        }
                        this.nextInitStep = InitSteps.Init_End;
                        break;
                    }

                case InitSteps.Init_End:
                    {
                        CurMsgStr("初始化完成", "Init operation finished");
                        InitFinished();
                        break;
                    }

                default:
                    Trace.Assert(false, "this init step invalid");
                    break;
            }
        }

        protected override void AutoOperation()
        {
            if(!IsModuleEnable())
            {
                CurMsgStr("模组禁用", "Moudle not enable");
                Sleep(100);
                return;
            }

            if(Def.IsNoHardware())
            {
                Sleep(200);
            }

            switch((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");

                        // 遍历所有炉层
                        for(int rowIdx = 0; rowIdx < (int)OvenRowCol.MaxRow; rowIdx++)
                        {
                            #region // 检查等待干燥炉腔
                            if(CheckWaitWorkStart(rowIdx))
                            {
                                // 每次加热启动如果有假电池夹具，则清除水含量值
                                for(int colIdx = 0; colIdx < (int)OvenRowCol.MaxCol; colIdx++)
                                {
                                    if(this.Pallet[rowIdx * (int)OvenRowCol.MaxCol + colIdx].HasFake())
                                    {
                                        for(int i = 0; i < this.waterContentValue.GetLength(1); i++)
                                        {
                                            this.waterContentValue[rowIdx, i] = 0.0;
                                        }
                                    }
                                }
                                // 每次启动，清楚上次加热过程数据
                                if (null != this.PltHeatTemp)
                                {
                                    for(int col = 0; col < (int)OvenRowCol.MaxCol; col++)
                                    {
                                        int idx = rowIdx * (int)OvenRowCol.MaxCol + col;
                                        // 控温、巡检
                                        for(int i = 0; i < 2; i++)
                                        {
                                            // 发热板
                                            for(int j = 0; j < (int)OvenInfoCount.HeatPanelCount; j++)
                                            {
                                                int heatIdx = i * (int)OvenInfoCount.HeatPanelCount + j;
                                                this.PltHeatTemp[idx][heatIdx].Clear();
                                                this.PltHeatTime[idx][heatIdx].Clear();
                                            }
                                        }
                                    }
                                }
                                this.operatePos.SetData(rowIdx, -1, EventList.Invalid);
                                this.nextAutoStep = AutoSteps.Auto_SetOvenWorkStop;
                                SaveRunData(SaveType.AutoStep|SaveType.Variables);
                                break;
                            }
                            #endregion

                            // 检查水含量
                            CheckWaterContentResult(rowIdx, this.waterContentValue);

                            EventList modEvent;
                            EventStatus state;
                            #region // 发送放夹具信号

                            int pltIdx = rowIdx * (int)OvenRowCol.MaxCol;
                            if(CavityEnable[rowIdx] && !CavityPressure[rowIdx] && !CavityTransfer[rowIdx] && (CavityStatus.Normal == CavityState[rowIdx])
                                && ((PalletStatus.Invalid == this.Pallet[pltIdx].State) || (PalletStatus.Invalid == this.Pallet[pltIdx + 1].State)))
                            {
                                // 干燥炉放空夹具
                                modEvent = EventList.DryOvenPlaceEmptyPallet;
                                state = GetEvent(this, modEvent);
                                if((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                {
                                    SetEvent(this, modEvent, EventStatus.Require);
                                }
                                // 干燥炉放NG非空夹具
                                modEvent = EventList.DryOvenPlaceNGPallet;
                                state = GetEvent(this, modEvent);
                                if((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                {
                                    SetEvent(this, modEvent, EventStatus.Require);
                                }
                                // 干燥炉放NG空夹具
                                modEvent = EventList.DryOvenPlaceNGEmptyPallet;
                                state = GetEvent(this, modEvent);
                                if((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                {
                                    SetEvent(this, modEvent, EventStatus.Require);
                                }
                                // 干燥炉放上料完成OK满夹具
                                modEvent = EventList.DryOvenPlaceOnlOKFullPallet;
                                state = GetEvent(this, modEvent);
                                if((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                {
                                    SetEvent(this, modEvent, EventStatus.Require);
                                }
                                if ((0 == (this.CavityHeartCycle[rowIdx] % this.CavitySamplingCycle[rowIdx]))
                                    && (!this.Pallet[pltIdx].HasFake() && !this.Pallet[pltIdx + 1].HasFake()))
                                {
                                    // 干燥炉放上料完成OK带假电池满夹具
                                    modEvent = EventList.DryOvenPlaceOnlOKFakeFullPallet;
                                    state = GetEvent(this, modEvent);
                                    if((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                    {
                                        SetEvent(this, modEvent, EventStatus.Require);
                                    }
                                }
                            }
                            // 干燥炉放等待水含量结果夹具（已取待测假电池的夹具）
                            if(CavityEnable[rowIdx] && !CavityPressure[rowIdx] && !CavityTransfer[rowIdx] && (CavityStatus.WaitDetect == CavityState[rowIdx]))
                            {
                            
                                modEvent = EventList.DryOvenPlaceWaitResultPallet;
                                state = GetEvent(this, modEvent);
                                if((EventStatus.Invalid == state) || (EventStatus.Finished == state)||(EventStatus.Require==state)) {
                                    for(int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                    {
                                        if(PalletStatus.Invalid == this.Pallet[pltIdx + i].State)
                                        {
                                            SetEvent(this, modEvent, EventStatus.Require, (pltIdx + i));
                                        }
                                    }
                                }
                            }
                            // 干燥炉放回炉假电池夹具（已放回假电池的夹具）
                            if(CavityEnable[rowIdx] && !CavityPressure[rowIdx] && !CavityTransfer[rowIdx] && (CavityStatus.WaitRebaking == CavityState[rowIdx])
                                && ((PalletStatus.Invalid == this.Pallet[pltIdx].State) || (PalletStatus.Invalid == this.Pallet[pltIdx + 1].State)))
                            {
                                modEvent = EventList.DryOvenPlaceRebakeFakePallet;
                                state = GetEvent(this, modEvent);
                                if((EventStatus.Invalid == state) || (EventStatus.Finished == state)||(EventStatus.Require==state))
                                {
                                    for(int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                    {
                                        if(PalletStatus.Invalid == this.Pallet[pltIdx + i].State)
                                        {
                                            SetEvent(this, modEvent, EventStatus.Require, (pltIdx + i));
                                        }
                                    }
                                }
                            }
                            // 干燥炉转移放夹具：放至目的炉腔
                            if(CavityEnable[rowIdx] && !CavityPressure[rowIdx] && !CavityTransfer[rowIdx] && (CavityStatus.Normal == CavityState[rowIdx])
                                && ((PalletStatus.Invalid == this.Pallet[pltIdx].State) && (PalletStatus.Invalid == this.Pallet[pltIdx + 1].State)))
                            {
                            int pos =-1;
                                modEvent = EventList.DryOvenPlaceTransferPallet;
                                state = GetEvent(this, modEvent,ref pos);
                                if((EventStatus.Invalid == state) || (EventStatus.Finished == state)||(EventStatus.Require==state && pos!=pltIdx))
                                {
                                    SetEvent(this, modEvent, EventStatus.Require, pltIdx);
                                    this.bakingNGType[rowIdx] = BakingNGType.Abnormal;
                                }
                            }
                            #endregion

                            #region // 发送取夹具信号

                            if(CavityEnable[rowIdx] && !CavityPressure[rowIdx] && !CavityTransfer[rowIdx])
                            {
                                for(int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                {
                                    int pos =-1;
                                    // 干燥炉取空夹具
                                    if ((CavityStatus.Normal == CavityState[rowIdx]) && (PalletStatus.OK == this.Pallet[pltIdx + i].State) && this.Pallet[pltIdx + i].IsEmpty())
                                    {
                                        modEvent = EventList.DryOvenPickEmptyPallet;
                                        pos=-1;
                                        state = GetEvent(this, modEvent,ref pos);
                                        if((EventStatus.Invalid == state) || (EventStatus.Finished == state)||(EventStatus.Require==state&& pos!=pltIdx+i))
                                        {
                                            SetEvent(this, modEvent, EventStatus.Require, (pltIdx + i));
                                        }
                                    }
                                    // 干燥炉取NG非空夹具
                                    if((CavityStatus.Normal == CavityState[rowIdx]) && (PalletStatus.NG == this.Pallet[pltIdx + i].State) && !this.Pallet[pltIdx + i].IsEmpty())
                                    {
                                        modEvent = EventList.DryOvenPickNGPallet;
                                    pos=-1;
                                        state = GetEvent(this, modEvent,ref pos);
                                        if((EventStatus.Invalid == state) || (EventStatus.Finished == state)||(EventStatus.Require==state&&pos !=pltIdx+i))
                                        {
                                            SetEvent(this, modEvent, EventStatus.Require, (pltIdx + i));
                                        }
                                    }
                                    // 干燥炉取NG空夹具
                                    if((CavityStatus.Normal == CavityState[rowIdx]) && (PalletStatus.NG == this.Pallet[pltIdx + i].State) && this.Pallet[pltIdx + i].IsEmpty())
                                    {
                                        modEvent = EventList.DryOvenPickNGEmptyPallet;
                                    pos=-1;
                                        state = GetEvent(this, modEvent,ref pos);
                                        if((EventStatus.Invalid == state) || (EventStatus.Finished == state)||(EventStatus.Require==state&&pos!=pltIdx+i)) {
                                            SetEvent(this, modEvent, EventStatus.Require, (pltIdx + i));
                                        }
                                    }
                                    // 干燥炉取待检测含假电池夹具（未取走假电池的夹具）
                                    if((CavityStatus.WaitDetect == CavityState[rowIdx]) && (PalletStatus.Detect == this.Pallet[pltIdx + i].State) && this.Pallet[pltIdx + i].HasFake())
                                    {
                                        modEvent = EventList.DryOvenPickDetectFakePallet;
                                    pos=-1;
                                        state = GetEvent(this, modEvent,ref pos);
                                        if((EventStatus.Invalid == state) || (EventStatus.Finished == state)||(EventStatus.Require==state&&pos!=pltIdx+i)) {
                                            SetEvent(this, modEvent, EventStatus.Require, (pltIdx + i));
                                        }
                                    }
                                    // 干燥炉取待回炉含假电池夹具（已取走假电池，待重新放回假电池的夹具）
                                    if((CavityStatus.WaitRebaking == CavityState[rowIdx]) && (PalletStatus.ReputFake == this.Pallet[pltIdx + i].State) && this.Pallet[pltIdx + i].HasFake())
                                    {
                                        modEvent = EventList.DryOvenPickReputFakePallet;
                                    pos=-1;
                                        state = GetEvent(this, modEvent,ref pos);
                                        if((EventStatus.Invalid == state) || (EventStatus.Finished == state)||(EventStatus.Require==state&&pos!=pltIdx+i)) {
                                            SetEvent(this, modEvent, EventStatus.Require, (pltIdx + i));
                                        }
                                    }
                                    // 干燥炉取干燥完成夹具（等待下料）
                                    if((CavityStatus.Normal == CavityState[rowIdx]) && (PalletStatus.WaitOffload == this.Pallet[pltIdx + i].State) && !this.Pallet[pltIdx + i].IsEmpty())
                                    {
                                        modEvent = EventList.DryOvenPickDryFinishPallet;
                                    pos=-1;
                                        state = GetEvent(this, modEvent,ref pos);
                                        if((EventStatus.Invalid == state) || (EventStatus.Finished == state)||(EventStatus.Require==state&&pos!=pltIdx+i)) {
                                            SetEvent(this, modEvent, EventStatus.Require, (pltIdx + i));
                                        }
                                    }
                                }
                            }
                            // 干燥炉转移取夹具：取来源炉腔
                            if(CavityEnable[rowIdx] && !CavityPressure[rowIdx] && CavityTransfer[rowIdx])
                            {
                                if (CavityStatus.Maintenance != CavityState[rowIdx])
                                {
                                    SetCavityState(rowIdx, CavityStatus.Maintenance);
                                }
                                string msg = "";
                                bool result = false;
                                //腔体转移前先判断MES接口Baking开始和结束是否上传成功，成功则开始腔体转移
                                for (int i = 0; i < (MesData.mesFrequency == 0 ? 3 : MesData.mesFrequency); i++)
                                {
                                    //Baking开始/结束
                                    if (!MesBakingStatusInfo(rowIdx, BakingType.Abnormal_End, ref msg))
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
                                            ShowMsgBox.ShowDialog($"MESBaking开始/结束接口超时失败3次，请检查MES连接状态", MessageType.MsgAlarm);
                                        }
                                    }
                                    else
                                    {
                                        result = true;
                                        break;
                                    }
                                }

                                //if (MesBakingStatusInfo(rowIdx, BakingType.Abnormal_End,ref msg))
                                if(result)
                                {
                                    for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                    {
                                        if (PalletStatus.Invalid != this.Pallet[pltIdx + i].State)
                                        {
                                            modEvent = EventList.DryOvenPickTransferPallet;
                                            int pos = -1;
                                            state = GetEvent(this, modEvent, ref pos);
                                            if ((EventStatus.Invalid == state) || (EventStatus.Finished == state) || (EventStatus.Require == state && pos != pltIdx + i))
                                            {
                                                SetEvent(this, modEvent, EventStatus.Require, (pltIdx + i));
                                            }
                                        }
                                    }
                                }
                            }
                            #endregion
                            
                        }

                        #region // 有请求已响应
                        for(EventList modEvent = EventList.DryOvenPlaceEmptyPallet; modEvent < EventList.DryOvenPickPlaceEnd; modEvent++)
                        {
                            int pltIdx = -1;
                            if((EventStatus.Response == GetEvent(this, modEvent, ref pltIdx)) 
                                && (-1 < pltIdx) && (pltIdx < (int)OvenRowCol.MaxRow * (int)OvenRowCol.MaxCol))
                            {
                                this.operatePos.SetData(pltIdx / 2, pltIdx % 2, modEvent);

                                this.nextAutoStep = AutoSteps.Auto_PrecloseOvenDoor;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                break;
                            }
                        }
                        #endregion

                        break;
                    }

                #region // 开门取放
                case AutoSteps.Auto_PrecloseOvenDoor:
                    {
                        bool result = true;
                        for(int i = 0; i < (int)OvenRowCol.MaxRow; i++)
                        {
                            if(CavityEnable[i] && !CavityPressure[i] && !CavityTransfer[i])
                            {
                                this.msgChs = string.Format("{0}层炉门预先关闭", i + 1);
                                this.msgEng = string.Format("{0} row oven door is closed in advance", i + 1);
                                CurMsgStr(this.msgChs, this.msgEng);

                                WriteLog("AutoOperation()操作：" + this.msgChs);
                                writeOvenData.CavityDatas[i].doorState = (int)OvenStatus.DoorClose;
                                if(!this.DryRun && !DryOvenOpenDoor(i, writeOvenData.CavityDatas[i], true))
                                {
                                    result = false;
                                    break;
                                }
                            }
                        }
                        if(result)
                        {
                            this.nextAutoStep = AutoSteps.Auto_PreblowAir;
                            SaveRunData(SaveType.AutoStep | SaveType.Cavity);
                        }
                        break;
                    }
                case AutoSteps.Auto_PreblowAir:
                    {
                        this.msgChs = string.Format("{0}层开门前破真空", this.operatePos.row + 1);
                        this.msgEng = string.Format("{0} row open blow air before open door", this.operatePos.row + 1);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if((-1 < this.operatePos.row) && (this.operatePos.row <= (int)OvenRowCol.MaxRow))
                        {
                            if (GetDryOvenData(ref readOvenData))
                            {
                                if(this.DryRun || (RCavity(this.operatePos.row).vacPressure > this.cavityParameter.OpenDoorVacPressure))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_OpenOvenDoor;
                                }
                                else if((int)OvenStatus.BlowOpen != RCavity(this.operatePos.row).blowValveState)
                                {
                                    WriteLog("AutoOperation()操作：" + this.msgChs + "：关闭真空阀，打开破真空阀");
                                    this.writeOvenData.CavityDatas[this.operatePos.row].vacValveState = (int)OvenStatus.VacClose;
                                    DryOvenVacuum(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true);
                                    this.writeOvenData.CavityDatas[this.operatePos.row].blowValveState = (int)OvenStatus.BlowOpen;
                                    DryOvenBlowAir(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OpenOvenDoor:
                    {
                        this.msgChs = string.Format("{0}层开门", this.operatePos.row + 1);
                        this.msgEng = string.Format("{0} row open door", this.operatePos.row + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        WriteLog("AutoOperation()操作：" + this.msgChs);
                        this.writeOvenData.CavityDatas[this.operatePos.row].doorState = (int)OvenStatus.DoorOpen;
                        if(this.DryRun || DryOvenOpenDoor(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true))
                        {
                            this.writeOvenData.CavityDatas[this.operatePos.row].blowValveState = (int)OvenStatus.BlowClose;
                            if(!this.DryRun)
                                DryOvenBlowAir(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true);

                            this.nextAutoStep = AutoSteps.Auto_CheckPalletState;
                            SaveRunData(SaveType.AutoStep | SaveType.Cavity);
                        }
                        break;
                    }
                case AutoSteps.Auto_CheckPalletState:
                    {
                        this.msgChs = string.Format("{0}层开门后检查夹具状态", this.operatePos.row + 1);
                        this.msgEng = string.Format("{0} row check cavity pallet", this.operatePos.row + 1);
                        CurMsgStr(this.msgChs, this.msgEng);
                        int pltIdx = -1;
                        EventStatus state = GetEvent(this, this.operatePos.operateEvent, ref pltIdx);
                        if((EventStatus.Response == state) && (-1 < pltIdx) && (pltIdx <= (int)ModuleMaxPallet.DryingOven))
                        {
                            if (GetDryOvenData(ref readOvenData))
                            {
                                if(!this.DryRun && !PalletKeepFlat(pltIdx, (this.Pallet[pltIdx].State > PalletStatus.Invalid), true))
                                {
                                    return;
                                }
                                else if(SetEvent(this, this.operatePos.operateEvent, EventStatus.Ready, pltIdx))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_WaitActionFinish;
                                    SaveRunData(SaveType.AutoStep);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_WaitActionFinish:
                    {
                        this.msgChs = string.Format("{0}层炉门已打开等待动作完成", this.operatePos.row + 1);
                        this.msgEng = string.Format("{0} row door is opened and wait action finsih", this.operatePos.row + 1);
                        CurMsgStr(this.msgChs, this.msgEng);
                        int pltIdx = -1;
                        EventStatus state = GetEvent(this, this.operatePos.operateEvent, ref pltIdx);
                        if((EventStatus.Finished == state) && ((pltIdx / (int)OvenRowCol.MaxCol) == this.operatePos.row))
                        {
                            switch(this.operatePos.operateEvent)
                            {
                                // 干燥炉放上料完成OK满夹具
                                case EventList.DryOvenPlaceOnlOKFullPallet:
                                // 干燥炉放上料完成OK带假电池满夹具
                                case EventList.DryOvenPlaceOnlOKFakeFullPallet:
                                    {
                                        SetCavityState(this.operatePos.row, CavityStatus.Normal);
                                        break;
                                    }
                                // 干燥炉放回炉假电池夹具（已放回假电池的夹具）
                                case EventList.DryOvenPlaceRebakeFakePallet:
                                    {
                                        for(int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                        {
                                            int idx = this.operatePos.row * (int)OvenRowCol.MaxCol + i;
                                            if((PalletStatus.ReputFake == this.Pallet[idx].State)
                                                || (PalletStatus.Rebaking == this.Pallet[idx].State))
                                            {
                                                this.Pallet[idx].State = PalletStatus.OK;
                                            }
                                        }
                                        SetCavityState(this.operatePos.row, CavityStatus.Normal);
                                        break;
                                    }
                                // 干燥炉放等待水含量结果夹具（已取待测假电池的夹具）
                                case EventList.DryOvenPlaceWaitResultPallet:
                                    {
                                        for(int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                        {
                                            int idx = this.operatePos.row * (int)OvenRowCol.MaxCol + i;
                                            if(PalletStatus.Detect == this.Pallet[idx].State)
                                            {
                                                this.Pallet[idx].State = PalletStatus.WaitResult;
                                            }
                                        }
                                        SetCavityState(this.operatePos.row, CavityStatus.WaitResult);
                                        break;
                                    }
                                default:
                                    break;
                            }
                            this.nextAutoStep = AutoSteps.Auto_FinishedCheckPltState;
                            SaveRunData(SaveType.AutoStep | SaveType.Pallet | SaveType.Cavity);
                        }
                        break;
                    }
                case AutoSteps.Auto_FinishedCheckPltState:
                    {
                        this.msgChs = string.Format("{0}层取放完成后检查夹具状态", this.operatePos.row + 1);
                        this.msgEng = string.Format("{0} row check cavity pallet after action", this.operatePos.row + 1);
                        CurMsgStr(this.msgChs, this.msgEng);
                        int pltIdx = this.operatePos.row * (int)OvenRowCol.MaxCol;
                        if(pltIdx > -1)
                        {
                            if (GetDryOvenData(ref readOvenData))
                            {
                                for(int pltCol = 0; pltCol < (int)OvenRowCol.MaxCol; pltCol++)
                                {
                                    if (!this.DryRun && !PalletKeepFlat(pltIdx + pltCol, (this.Pallet[pltIdx + pltCol].State > PalletStatus.Invalid), true))
                                    {
                                        return;
                                    }
                                }
                                this.nextAutoStep = AutoSteps.Auto_CloseOvenDoor;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_CloseOvenDoor:
                    {
                        this.msgChs = string.Format("{0}层关门", this.operatePos.row + 1);
                        this.msgEng = string.Format("{0} row close door", this.operatePos.row + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        WriteLog("AutoOperation()操作：" + this.msgChs);
                        this.writeOvenData.CavityDatas[this.operatePos.row].doorState = (int)OvenStatus.DoorClose;
                        if(this.DryRun || DryOvenOpenDoor(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true))
                        {
                            this.nextAutoStep = AutoSteps.Auto_UpdateMesBindCavity;
                            SaveRunData(SaveType.AutoStep | SaveType.Cavity);
                        }
                        break;
                    }
                case AutoSteps.Auto_UpdateMesBindCavity:
                    {
                        this.msgChs = $"{this.operatePos.row + 1}层夹具{this.operatePos.col + 1}上传绑炉腔信息";
                        this.msgEng = $"{this.operatePos.row + 1}row pallet {this.operatePos.col + 1}updata mes";
                        CurMsgStr(this.msgChs, this.msgEng);
                        //string msg = "";
                        //bool result = false;
                        int pltIdx = this.operatePos.row * (int)OvenRowCol.MaxCol + this.operatePos.col;
                        switch(this.operatePos.operateEvent)
                        {
                            // 干燥炉放上料完成OK满夹具
                            case EventList.DryOvenPlaceOnlOKFullPallet:
                            // 干燥炉放上料完成OK带假电池满夹具
                            case EventList.DryOvenPlaceOnlOKFakeFullPallet:
                                {
                                    //0413  注释
                                    //for (int i = 0; i < (MesData.mesFrequency == 0 ? 3 : MesData.mesFrequency); i++)
                                    //{
                                    //    //绑炉腔上传
                                    //    if (!MesBindCavityInfo(this.operatePos.row, this.operatePos.col, this.Pallet[pltIdx].Code,ref msg))
                                    //    {
                                    //        //获取报警字符串是否包含"超时"二字,如果没有则跳出循环
                                    //        if (!msg.Contains("超时"))
                                    //        {
                                    //            result = false;
                                    //            break;
                                    //        }
                                    //        if (i == 2)
                                    //        {
                                    //            result = false;
                                    //            ShowMsgBox.ShowDialog($"MES绑炉腔上传接口超时失败3次，请检查MES连接状态", MessageType.MsgAlarm);
                                    //        }
                                    //    }
                                    //    else
                                    //    {
                                    //        result = true;
                                    //        break;
                                    //    }
                                    //}

                                    //if (!result)
                                    //{
                                    //    return;
                                    //}
                                    //if (!MesBindCavityInfo(this.operatePos.row, this.operatePos.col, this.Pallet[pltIdx].Code))
                                    //{
                                    //    return;
                                    //}

                                    break;
                                }
                            default:
                                break;
                        }
                        this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                #endregion

                #region // 启动加热
                case AutoSteps.Auto_SetOvenWorkStop:
                    {
                        this.msgChs = string.Format("{0}层炉腔停止加热", this.operatePos.row + 1);
                        this.msgEng = string.Format("{0} row set cavity work stop", this.operatePos.row + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        this.writeOvenData.CavityDatas[this.operatePos.row].workState = (int)OvenStatus.WorkStop;
                        if(this.DryRun || DryOvenWorkStart(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true))
                        {
                            WriteLog($"AutoOperation()操作：{this.operatePos.row + 1}层炉腔自动加热前预先停止加热");
                            this.nextAutoStep = AutoSteps.Auto_CheckPressure;
                            SaveRunData(SaveType.AutoStep | SaveType.Cavity);
                        }
                        break;
                    }
                case AutoSteps.Auto_CheckPressure:
                    {
                        this.msgChs = string.Format("{0}层启动加热前破真空", this.operatePos.row + 1);
                        this.msgEng = string.Format("{0} row open blow air before work start", this.operatePos.row + 1);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if((-1 < this.operatePos.row) && (this.operatePos.row <= (int)OvenRowCol.MaxRow))
                        {
                            if (GetDryOvenData(ref readOvenData))
                            {
                                if(this.DryRun || (RCavity(this.operatePos.row).vacPressure > this.cavityParameter.AStateVacPressure))
                                {
                                    this.writeOvenData.CavityDatas[this.operatePos.row].blowValveState = (int)OvenStatus.BlowClose;
                                    if(!this.DryRun)
                                        DryOvenBlowAir(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true);

                                    this.nextAutoStep = AutoSteps.Auto_SendWorkParameter;
                                }
                                else if((int)OvenStatus.BlowOpen != RCavity(this.operatePos.row).blowValveState)
                                {
                                    this.writeOvenData.CavityDatas[this.operatePos.row].vacValveState = (int)OvenStatus.VacClose;
                                    DryOvenVacuum(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true);
                                    this.writeOvenData.CavityDatas[this.operatePos.row].blowValveState = (int)OvenStatus.BlowOpen;
                                    DryOvenBlowAir(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_SendWorkParameter:
                    {
                        this.msgChs = string.Format("{0}层设置炉腔参数", this.operatePos.row + 1);
                        this.msgEng = string.Format("{0} row set cavity parameter", this.operatePos.row + 1);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if((-1 < this.operatePos.row) && (this.operatePos.row <= (int)OvenRowCol.MaxRow))
                        {
                            //this.writeOvenData.CavityDatas[this.operatePos.row].cavityParameter.Copy(this.cavityParameter);
                            if(this.DryRun || DryOvenSetParameter(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_SetOvenWorkStart;
                                SaveRunData(SaveType.Cavity);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_SetOvenWorkStart:
                    {
                        this.msgChs = string.Format("{0}层启动加热", this.operatePos.row + 1);
                        this.msgEng = string.Format("{0} row set cavity work start", this.operatePos.row + 1);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if((-1 < this.operatePos.row) && (this.operatePos.row <= (int)OvenRowCol.MaxRow))
                        {
                            //for(int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                            //{
                            //    int idx = this.operatePos.row * (int)OvenRowCol.MaxCol + i;
                            //    this.Pallet[idx].StartDate = DateTime.Now;
                            //    this.Pallet[idx].EndDate = DateTime.MinValue;
                            //}
                            //this.writeOvenData.CavityDatas[this.operatePos.row].workState = (int)OvenStatus.WorkStart;
                            //if(this.DryRun || DryOvenWorkStart(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true))
                            //{
                            //    WriteLog($"AutoOperation()操作：{this.operatePos.row + 1}层炉腔自动加热，发送启动");
                            //    SetCavityState(this.operatePos.row, CavityStatus.Heating);
                            //    this.CavityHeartCycle[this.operatePos.row]++;
                            //    if (this.CavityHeartCycle[this.operatePos.row] >= this.CavitySamplingCycle[this.operatePos.row])
                            //    {
                            //        this.CavityHeartCycle[this.operatePos.row] = 0;
                            //    }

                            this.nextAutoStep = AutoSteps.Auto_UpdateMesWorkStart;
                            //    SaveRunData(SaveType.AutoStep | SaveType.Cavity | SaveType.Variables | SaveType.Pallet);
                            //}
                        }
                        break;
                    }
                case AutoSteps.Auto_UpdateMesWorkStart:
                    {
                        this.msgChs = string.Format("{0}层上传MES启动加热信息", this.operatePos.row + 1);
                        this.msgEng = string.Format("update MES {0} row work start and", this.operatePos.row + 1);
                        CurMsgStr(this.msgChs, this.msgEng);
                        string msg = "";
                        bool result = false;
                        if((-1 < this.operatePos.row) && (this.operatePos.row <= (int)OvenRowCol.MaxRow))
                        {
                            //判断该腔体是否为异常腔体
                            int value = -1;
                            MesData.readBakingNgType(this.RunModule, "BakingType" + operatePos.row, ref value);
                            if (value == Convert.ToInt32( BakingNGType.Abnormal))
                            {
                                for (int i = 0; i < (MesData.mesFrequency == 0 ? 3 : MesData.mesFrequency); i++)
                                {
                                    //Baking开始/结束
                                    if (!MesBakingStatusInfo(this.operatePos.row, BakingType.Abnormal_start, ref msg))
                                    {
                                        //获取报警字符串是否包含"超时"二字,如果没有则跳出循环
                                        if (!msg.Contains("超时"))
                                        {
                                            result = true;
                                            break;
                                        }
                                        if (i == 2)
                                        {
                                            result = false;
                                            ShowMsgBox.ShowDialog($"MESBaking开始/结束接口超时失败3次，请检查MES连接状态", MessageType.MsgAlarm);
                                        }
                                    }
                                    else
                                    {
                                        result = true;
                                        break;
                                    }
                                }

                                //if (MesBakingStatusInfo(this.operatePos.row, BakingType.Abnormal_start,ref msg))

                                //修改为: Baking开始接口是否调用成功，成功则加热   ——0421 wjl
                                if(result)
                                {
                                    for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                    {
                                        int idx = this.operatePos.row * (int)OvenRowCol.MaxCol + i;
                                        this.Pallet[idx].StartDate = DateTime.Now;
                                        this.Pallet[idx].EndDate = DateTime.MinValue;
                                    }
                                    this.writeOvenData.CavityDatas[this.operatePos.row].workState = (int)OvenStatus.WorkStart;
                                    if (this.DryRun || DryOvenWorkStart(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true))
                                    {
                                        WriteLog($"AutoOperation()操作：{this.operatePos.row + 1}层炉腔自动加热，发送启动");
                                        SetCavityState(this.operatePos.row, CavityStatus.Heating);
                                        this.CavityHeartCycle[this.operatePos.row]++;
                                        if (this.CavityHeartCycle[this.operatePos.row] >= this.CavitySamplingCycle[this.operatePos.row])
                                        {
                                            this.CavityHeartCycle[this.operatePos.row] = 0;
                                        }

                                        //this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                        //SaveRunData(SaveType.AutoStep | SaveType.Cavity | SaveType.Variables | SaveType.Pallet);
                                    }

                                    Pallet[] plt = new Pallet[(int)OvenRowCol.MaxCol];
                                    for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                    {
                                        int idx = this.operatePos.row * (int)OvenRowCol.MaxCol + i;
                                        this.Pallet[idx].BakingCount++;
                                        plt[i] = this.Pallet[idx];
                                    }
                                    this.bakingNGType[this.operatePos.row] = BakingNGType.Normal;
                                    this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                    SaveRunData(SaveType.AutoStep | SaveType.Cavity | SaveType.Variables | SaveType.Pallet);

                                    //Pallet[] plt = new Pallet[(int)OvenRowCol.MaxCol];
                                    //for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                    //{
                                    //    int idx = this.operatePos.row * (int)OvenRowCol.MaxCol + i;
                                    //    this.Pallet[idx].BakingCount++;
                                    //    plt[i] = this.Pallet[idx];
                                    //}
                                    //this.bakingNGType[this.operatePos.row] = BakingNGType.Normal;
                                    //this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                    //SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                                }
                            }
                            else
                            {
                                for (int i = 0; i < (MesData.mesFrequency == 0 ? 3 : MesData.mesFrequency); i++)
                                {
                                    //Baking开始/结束
                                    if (!MesBakingStatusInfo(this.operatePos.row, BakingType.Normal_start, ref msg))
                                    {
                                        //获取报警字符串是否包含"超时"二字,如果没有则跳出循环
                                        if (!msg.Contains("超时"))
                                        {
                                            result = true;
                                            break;
                                        }
                                        if (i == 2)
                                        {
                                            result = false;
                                            ShowMsgBox.ShowDialog($"MESBaking开始/结束接口超时失败3次，请检查MES连接状态", MessageType.MsgAlarm);
                                        }
                                    }
                                    else
                                    {
                                        result = true;
                                        break;
                                    }
                                }
                                //if (MesBakingStatusInfo(this.operatePos.row, BakingType.Normal_start))
                                if(result)
                                {

                                    for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                    {
                                        int idx = this.operatePos.row * (int)OvenRowCol.MaxCol + i;
                                        this.Pallet[idx].StartDate = DateTime.Now;
                                        this.Pallet[idx].EndDate = DateTime.MinValue;
                                    }
                                    this.writeOvenData.CavityDatas[this.operatePos.row].workState = (int)OvenStatus.WorkStart;
                                    if (this.DryRun || DryOvenWorkStart(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true))
                                    {
                                        WriteLog($"AutoOperation()操作：{this.operatePos.row + 1}层炉腔自动加热，发送启动");
                                        SetCavityState(this.operatePos.row, CavityStatus.Heating);
                                        this.CavityHeartCycle[this.operatePos.row]++;
                                        if (this.CavityHeartCycle[this.operatePos.row] >= this.CavitySamplingCycle[this.operatePos.row])
                                        {
                                            this.CavityHeartCycle[this.operatePos.row] = 0;
                                        }

                                        //this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                        //SaveRunData(SaveType.AutoStep | SaveType.Cavity | SaveType.Variables | SaveType.Pallet);
                                    }

                                    Pallet[] plt = new Pallet[(int)OvenRowCol.MaxCol];
                                    for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                    {
                                        int idx = this.operatePos.row * (int)OvenRowCol.MaxCol + i;
                                        this.Pallet[idx].BakingCount++;
                                        plt[i] = this.Pallet[idx];
                                    }

                                    this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                    SaveRunData(SaveType.AutoStep | SaveType.Cavity | SaveType.Variables | SaveType.Pallet);
                                }
                            }
                        }
                        break;
                    }
                #endregion

                case AutoSteps.Auto_WorkEnd:
                    {
                        CurMsgStr("工作完成", "Work end");
                        this.nextAutoStep = AutoSteps.Auto_WaitWorkStart;
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                default:
                    {
                        Trace.Assert(false, "RunEx::AutoOperation/no this run step");
                        break;
                    }
            }

        }
        
        #endregion

        #region // 模组配置及参数

        /// <summary>
        /// 读取模组配置
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public override bool InitializeConfig(string module)
        {
            if(!base.InitializeConfig(module))
            {
                return false;
            }

            this.dryingOvenID = IniFile.ReadInt(module, "DryingOvenID", -1, Def.GetAbsPathName(Def.ModuleExCfg));
            this.lineID = MachineCtrl.GetInstance().LineID;
            //this.ovenLogFile.SetFileInfo(Def.GetAbsPathName(string.Format("Log\\DryingOven\\{0}-{1}\\", this.RunModule, this.ovenIP)), 2, 15);

            if (!InitThread())
            {
                ShowMsgBox.ShowDialog((module + " 后台线程初始化失败"), MessageType.MsgWarning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 初始化通用组参数 + 模组参数
        /// </summary>
        protected override void InitParameter()
        {
            this.CavityEnable = new bool[(int)OvenRowCol.MaxRow];
            this.CavityPressure = new bool[(int)OvenRowCol.MaxRow];
            this.CavityTransfer = new bool[(int)OvenRowCol.MaxRow];
            this.CavitySamplingCycle = new int[(int)OvenRowCol.MaxRow];
            for(int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            {
                this.CavityEnable[i] = false;
                this.CavityPressure[i] = false;
                this.CavityTransfer[i] = false;
                this.CavitySamplingCycle[i] = 1;
            }
            this.localIP = "127.0.0.21";
            this.ovenIP = "";
            this.ovenPort = 9600;
            this.cavityParameter.Release();
            this.waterStandardAnode = 300.0;
            this.waterStandardCathode = 300.0;
            this.dryingOvenGroup = 0;
            this.openDoorDelay = 30;
            this.maxWorkTimeRange = 2;
            this.waitResultPressure = false;
            this.workDataTime = 10;
            this.PltHeatTemp = new List<List<List<double>>>();
            this.PltHeatTime = new List<List<List<uint>>>();
            for(int pltIdx = 0; pltIdx < (int)ModuleMaxPallet.DryingOven; pltIdx++)
            {
                this.PltHeatTemp.Add(new List<List<double>>());
                this.PltHeatTime.Add(new List<List<uint>>());
                for(int i = 0; i < 2 * (int)OvenInfoCount.HeatPanelCount; i++)
                {
                    this.PltHeatTemp[pltIdx].Add(new List<double>());
                    this.PltHeatTemp[pltIdx][i].Capacity = 2000;
                    this.PltHeatTime[pltIdx].Add(new List<uint>());
                    this.PltHeatTime[pltIdx][i].Capacity = 2000;
                }
            }
            this.mesUpdataState = new bool[6];
            Array.Clear(this.mesUpdataState, 0, this.mesUpdataState.Length);

            base.InitParameter();
        }

        public override bool CheckParameter(string name, object value)
        {
            // 转炉换腔由false改为true
            for(int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            {
                if ((("CavityTransfer" + i) == name) && Convert.ToBoolean(value))
                {
                    if ((CavityStatus.Normal != this.CavityState[i]) && (CavityStatus.Maintenance != this.CavityState[i]))
                    {
                        ShowMsgBox.ShowDialog($"【腔体转移{name}】参数只能在腔体正常状态及维护状态下修改", MessageType.MsgAlarm);
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 读取通用组参数 + 模组参数
        /// </summary>
        /// <returns></returns>
        public override bool ReadParameter()
        {
            for(int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            {
                this.CavityEnable[i] = ReadBoolParameter(this.RunModule, ("CavityEnable" + i), this.CavityEnable[i]);
                this.CavityPressure[i] = ReadBoolParameter(this.RunModule, ("CavityPressure" + i), this.CavityPressure[i]);
                this.CavityTransfer[i] = ReadBoolParameter(this.RunModule, ("CavityTransfer" + i), this.CavityTransfer[i]);
                this.CavitySamplingCycle[i] = ReadIntParameter(this.RunModule, ("CavitySamplingCycle" + i), this.CavitySamplingCycle[i]);
            }
            //this.localIP
            this.ovenIP = ReadStringParameter(this.RunModule, "ovenIP", this.ovenIP);
            this.ovenPort = ReadIntParameter(this.RunModule, "ovenPort", this.ovenPort);
            this.waterStandardAnode = ReadDoubleParameter(this.RunModule, "waterStandardAnode", this.waterStandardAnode);
            this.waterStandardCathode = ReadDoubleParameter(this.RunModule, "waterStandardCathode", this.waterStandardCathode);
            this.dryingOvenGroup = ReadIntParameter(this.RunModule, "DryingOvenGroup", this.dryingOvenGroup);
            this.openDoorDelay = ReadIntParameter(this.RunModule, "openDoorDelay", this.openDoorDelay);
            this.maxWorkTimeRange = ReadIntParameter(this.RunModule, "maxWorkTimeRange", this.maxWorkTimeRange);
            this.waitResultPressure = ReadBoolParameter(this.RunModule, "waitResultPressure", this.waitResultPressure);
            this.workDataTime = ReadIntParameter(this.RunModule, "workDataTime", this.workDataTime);
            
            // 干燥炉设置参数
            this.cavityParameter.SetTempValue = (float)ReadDoubleParameter(this.RunModule, "SetTempValue", (double)this.cavityParameter.SetTempValue);
            this.cavityParameter.TempUpperlimit = (float)ReadDoubleParameter(this.RunModule, "TempUpperlimit", (double)this.cavityParameter.TempUpperlimit);
            this.cavityParameter.TempLowerlimit = (float)ReadDoubleParameter(this.RunModule, "TempLowerlimit", (double)this.cavityParameter.TempLowerlimit);
            this.cavityParameter.PreheatTime = (uint)ReadIntParameter(this.RunModule, "PreheatTime", (int)this.cavityParameter.PreheatTime);
            this.cavityParameter.VacHeatTime = (uint)ReadIntParameter(this.RunModule, "VacHeatTime", (int)this.cavityParameter.VacHeatTime);
            this.cavityParameter.OpenDoorBlowTime = (uint)ReadIntParameter(this.RunModule, "OpenDoorBlowTime", (int)this.cavityParameter.OpenDoorBlowTime);
            this.cavityParameter.OpenDoorVacPressure = (uint)ReadIntParameter(this.RunModule, "OpenDoorVacPressure", (int)this.cavityParameter.OpenDoorVacPressure);
            this.cavityParameter.AStateVacTime = (uint)ReadIntParameter(this.RunModule, "AStateVacTime", (int)this.cavityParameter.AStateVacTime);
            this.cavityParameter.AStateVacPressure = (uint)ReadIntParameter(this.RunModule, "AStateVacPressure", (int)this.cavityParameter.AStateVacPressure);
            this.cavityParameter.BStateVacTime = (uint)ReadIntParameter(this.RunModule, "BStateVacTime", (int)this.cavityParameter.BStateVacTime);
            this.cavityParameter.BStateVacPressure = (uint)ReadIntParameter(this.RunModule, "BStateVacPressure", (int)this.cavityParameter.BStateVacPressure);
            this.cavityParameter.BStateBlowAirTime = (uint)ReadIntParameter(this.RunModule, "BStateBlowAirTime", (int)this.cavityParameter.BStateBlowAirTime);
            this.cavityParameter.BStateBlowAirPressure = (uint)ReadIntParameter(this.RunModule, "BStateBlowAirPressure", (int)this.cavityParameter.BStateBlowAirPressure);
            this.cavityParameter.BStateBlowAirKeepTime = (uint)ReadIntParameter(this.RunModule, "BStateBlowAirKeepTime", (int)this.cavityParameter.BStateBlowAirKeepTime);
            this.cavityParameter.BreathTimeInterval = (uint)ReadIntParameter(this.RunModule, "BreathTimeInterval", (int)this.cavityParameter.BreathTimeInterval);
            this.cavityParameter.BreathCycleTimes = (uint)ReadIntParameter(this.RunModule, "BreathCycleTimes", (int)this.cavityParameter.BreathCycleTimes);
            this.cavityParameter.HeatPlate = (uint)ReadIntParameter(this.RunModule, "HeatPlate", (int)this.cavityParameter.HeatPlate);
            this.cavityParameter.MaxNGHeatPlate = (uint)ReadIntParameter(this.RunModule, "MaxNGHeatPlate", (int)this.cavityParameter.MaxNGHeatPlate);
            this.cavityParameter.HeatPreVacTime = (uint)ReadIntParameter(this.RunModule, "HeatPreVacTime", (int)this.cavityParameter.HeatPreVacTime);
            this.cavityParameter.HeatPreBlow = (uint)ReadIntParameter(this.RunModule, "HeatPreBlow", (int)this.cavityParameter.HeatPreBlow);

            return base.ReadParameter();
        }

        #endregion

        #region // 运行数据读写

        public override void InitRunData()
        {
            if (null == this.CavityState)
            {
                this.CavityState = new CavityStatus[(int)OvenRowCol.MaxRow];
                for(int i = 0; i < this.CavityState.Length; i++)
                {
                    this.CavityState[i] = new CavityStatus();
                }
            }
            if(null == this.CavityOldState)
            {
                this.CavityOldState = new CavityStatus[(int)OvenRowCol.MaxRow];
                for(int i = 0; i < this.CavityOldState.Length; i++)
                {
                    this.CavityOldState[i] = CavityStatus.Unknown;
                }
            }
            if (null == this.bakingNGType)
            {
                this.bakingNGType = new BakingNGType[(int)OvenRowCol.MaxRow];
                for (int i = 0; i < this.bakingNGType.Length; i++)
                {
                    this.bakingNGType[i] = BakingNGType.Normal;
                }
            }
            if(null == this.CavityHeartCycle)
            {
                this.CavityHeartCycle = new int[(int)OvenRowCol.MaxRow];
            }
            for(int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            {
                this.CavityState[i] = CavityStatus.Normal;
                this.CavityHeartCycle[i] = 0;
            }
            this.operatePos.Release();
            if (null == this.waterContentValue)
            {
                this.waterContentValue = new double[(int)OvenRowCol.MaxRow, 3];
            }
            for(int rowIdx = 0; rowIdx < this.waterContentValue.GetLength(0); rowIdx++)
            {
                for(int i = 0; i < this.waterContentValue.GetLength(1); i++)
                {
                    this.waterContentValue[rowIdx, i] = 0.0;
                }
            }
            if (null == this.readOvenData)
            {
                this.readOvenData = new DryingOvenData();
            }
            this.readOvenData.Release();
            if (null == this.writeOvenData)
            {
                this.writeOvenData = new DryingOvenData();
            }
            this.writeOvenData.Release();
            if(null == this.bakingDataStartTime)
            {
                this.bakingDataStartTime = new DateTime[(int)OvenRowCol.MaxRow];
            }

            base.InitRunData();
        }

        public override void LoadRunData()
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);

            this.operatePos.operateEvent = (EventList)iniStream.ReadInt(section, "operatePos.operateEvent", (int)operatePos.operateEvent);
            this.operatePos.row = iniStream.ReadInt(section, "operatePos.row", operatePos.row);
            this.operatePos.col = iniStream.ReadInt(section, "operatePos.col", operatePos.col);
            for(int rowIdx = 0; rowIdx < this.waterContentValue.GetLength(0); rowIdx++)
            {
                for(int i = 0; i < this.waterContentValue.GetLength(1); i++)
                {
                    key = string.Format("waterContentValue[{0},{1}]", rowIdx, i);
                    this.waterContentValue[rowIdx, i] = iniStream.ReadDouble(section, key, waterContentValue[rowIdx, i]);
                }
            }
            for(int rowIdx = 0; rowIdx < (int)OvenRowCol.MaxRow; rowIdx++)
            {
                // 状态
                this.CavityState[rowIdx] = (CavityStatus)iniStream.ReadInt(section, ("CavityState" + rowIdx), (int)CavityState[rowIdx]);
                this.CavityHeartCycle[rowIdx] = iniStream.ReadInt(section, ("CavityHeartCycle" + rowIdx), CavityHeartCycle[rowIdx]);
                key = string.Format("WCavity({0}).doorState", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].doorState = (short)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).doorState);
                key = string.Format("WCavity({0}).workState", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].workState = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).workState);
                // 参数
                key = string.Format("WCavity({0}).cavityParameter.SetTempValue", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.SetTempValue = (float)iniStream.ReadDouble(section, key, WCavity(rowIdx).parameter.SetTempValue);
                key = string.Format("WCavity({0}).cavityParameter.TempUpperlimit", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.TempUpperlimit = (float)iniStream.ReadDouble(section, key, WCavity(rowIdx).parameter.TempUpperlimit);
                key = string.Format("WCavity({0}).cavityParameter.TempLowerlimit", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.TempLowerlimit = (float)iniStream.ReadDouble(section, key, WCavity(rowIdx).parameter.TempLowerlimit);
                key = string.Format("WCavity({0}).cavityParameter.PreheatTime", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.PreheatTime = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.PreheatTime);
                key = string.Format("WCavity({0}).cavityParameter.VacHeatTime", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.VacHeatTime = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.VacHeatTime);
                key = string.Format("WCavity({0}).cavityParameter.OpenDoorBlowTime", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.OpenDoorBlowTime = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.OpenDoorBlowTime);
                key = string.Format("WCavity({0}).cavityParameter.OpenDoorVacPressure", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.OpenDoorVacPressure = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.OpenDoorVacPressure);
                key = string.Format("WCavity({0}).cavityParameter.AStateVacTime", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.AStateVacTime = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.AStateVacTime);
                key = string.Format("WCavity({0}).cavityParameter.AStateVacPressure", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.AStateVacPressure = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.AStateVacPressure);
                key = string.Format("WCavity({0}).cavityParameter.BStateVacTime", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.BStateVacTime = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BStateVacTime);
                key = string.Format("WCavity({0}).cavityParameter.BStateVacPressure", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.BStateVacPressure = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BStateVacPressure);
                key = string.Format("WCavity({0}).cavityParameter.BStateBlowAirTime", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.BStateBlowAirTime = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BStateBlowAirTime);
                key = string.Format("WCavity({0}).cavityParameter.BStateBlowAirPressure", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.BStateBlowAirPressure = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BStateBlowAirPressure);
                key = string.Format("WCavity({0}).cavityParameter.BStateBlowAirKeepTime", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.BStateBlowAirKeepTime = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BStateBlowAirKeepTime);
                key = string.Format("WCavity({0}).cavityParameter.BreathTimeInterval", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.BreathTimeInterval = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BreathTimeInterval);
                key = string.Format("WCavity({0}).cavityParameter.BreathCycleTimes", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.BreathCycleTimes = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BreathCycleTimes);
                key = string.Format("WCavity({0}).cavityParameter.HeatPlate", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.HeatPlate = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.HeatPlate);
                key = string.Format("WCavity({0}).cavityParameter.MaxNGHeatPlate", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.MaxNGHeatPlate = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.MaxNGHeatPlate);
                key = string.Format("WCavity({0}).cavityParameter.HeatPreVacTime", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.HeatPreVacTime = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.HeatPreVacTime);
                key = string.Format("WCavity({0}).cavityParameter.HeatPreBlow", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.HeatPreBlow = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.HeatPreBlow);
            }

            base.LoadRunData();
        }

        public override void SaveRunData(SaveType saveType, int index = -1)
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);

            if(SaveType.Variables == (SaveType.Variables & saveType))
            {
                iniStream.WriteInt(section, "operatePos.operateEvent", (int)operatePos.operateEvent);
                iniStream.WriteInt(section, "operatePos.row", operatePos.row);
                iniStream.WriteInt(section, "operatePos.col", operatePos.col);
                for(int rowIdx = 0; rowIdx < this.waterContentValue.GetLength(0); rowIdx++)
                {
                    for(int i = 0; i < this.waterContentValue.GetLength(1); i++)
                    {
                        key = string.Format("waterContentValue[{0},{1}]", rowIdx, i);
                        iniStream.WriteDouble(section, key, waterContentValue[rowIdx, i]);
                    }
                }
            }
            if(SaveType.Cavity == (SaveType.Cavity & saveType))
            {
                // 仅保存有用信息
                for(int rowIdx = 0; rowIdx < (int)OvenRowCol.MaxRow; rowIdx++)
                {
                    // 状态
                    iniStream.WriteInt(section, ("CavityState" + rowIdx), (int)CavityState[rowIdx]);
                    iniStream.WriteInt(section, ("CavityHeartCycle" + rowIdx), CavityHeartCycle[rowIdx]);
                    key = string.Format("WCavity({0}).doorState", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).doorState);
                    key = string.Format("WCavity({0}).workState", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).workState);

                    // 参数
                    key = string.Format("WCavity({0}).cavityParameter.SetTempValue", rowIdx);
                    iniStream.WriteDouble(section, key, WCavity(rowIdx).parameter.SetTempValue);
                    key = string.Format("WCavity({0}).cavityParameter.TempUpperlimit", rowIdx);
                    iniStream.WriteDouble(section, key, WCavity(rowIdx).parameter.TempUpperlimit);
                    key = string.Format("WCavity({0}).cavityParameter.TempLowerlimit", rowIdx);
                    iniStream.WriteDouble(section, key, WCavity(rowIdx).parameter.TempLowerlimit);
                    key = string.Format("WCavity({0}).cavityParameter.PreheatTime", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.PreheatTime);
                    key = string.Format("WCavity({0}).cavityParameter.VacHeatTime", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.VacHeatTime);
                    key = string.Format("WCavity({0}).cavityParameter.OpenDoorBlowTime", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.OpenDoorBlowTime);
                    key = string.Format("WCavity({0}).cavityParameter.OpenDoorVacPressure", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.OpenDoorVacPressure);
                    key = string.Format("WCavity({0}).cavityParameter.AStateVacTime", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.AStateVacTime);
                    key = string.Format("WCavity({0}).cavityParameter.AStateVacPressure", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.AStateVacPressure);
                    key = string.Format("WCavity({0}).cavityParameter.BStateVacTime", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BStateVacTime);
                    key = string.Format("WCavity({0}).cavityParameter.BStateVacPressure", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BStateVacPressure);
                    key = string.Format("WCavity({0}).cavityParameter.BStateBlowAirTime", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BStateBlowAirTime);
                    key = string.Format("WCavity({0}).cavityParameter.BStateBlowAirPressure", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BStateBlowAirPressure);
                    key = string.Format("WCavity({0}).cavityParameter.BStateBlowAirKeepTime", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BStateBlowAirKeepTime);
                    key = string.Format("WCavity({0}).cavityParameter.BreathTimeInterval", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BreathTimeInterval);
                    key = string.Format("WCavity({0}).cavityParameter.BreathCycleTimes", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BreathCycleTimes);
                    key = string.Format("WCavity({0}).cavityParameter.HeatPlate", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.HeatPlate);
                    key = string.Format("WCavity({0}).cavityParameter.MaxNGHeatPlate", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.MaxNGHeatPlate);
                    key = string.Format("WCavity({0}).cavityParameter.HeatPreVacTime", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.HeatPreVacTime);
                    key = string.Format("WCavity({0}).cavityParameter.HeatPreBlow", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.HeatPreBlow);
                }
            }

            base.SaveRunData(saveType, index);
        }
        #endregion

        #region // 干燥炉操作

        /// <summary>
        /// 获取干燥炉连接状态
        /// </summary>
        /// <returns></returns>
        public bool DryOvenIsConnect()
        {
            return this.ovenClient.IsConnect();
        }

        /// <summary>
        /// 干燥炉连接
        /// </summary>
        /// <param name="connect"></param>
        /// <returns></returns>
        public bool DryOvenConnect(bool connect)
        {
            if (connect)
            {
                if (!DryOvenIsConnect())
                {
                    byte nodeID = Convert.ToByte(this.localIP.Substring(this.localIP.LastIndexOf('.') + 1));
                    return this.ovenClient.Connect(ovenIP, ovenPort, nodeID);
                }
            } 
            else
            {
                this.ovenClient.Disconnect();
                this.readOvenData.Release();
            }
            return DryOvenIsConnect();
        }
        
        /// <summary>
        /// 获取干燥炉IP信息
        /// </summary>
        /// <returns></returns>
        public string GetDryOvenIPInfo()
        {
            return string.Format("{0}:{1}", this.ovenIP, this.ovenPort);
        }

        /// <summary>
        /// 干燥炉开门/关门
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="cavityData"></param>
        /// <param name="alarm"></param>
        /// <returns></returns>
        public bool DryOvenOpenDoor(int cavityIdx, CavityData cavityData, bool alarm = true, OptMode mode = OptMode.Auto)
        {
            string msg, dispose;
            if ((short)OvenStatus.RemoteOpen != this.readOvenData.RemoteState)
            {
                msg = "干燥炉非远程运行状态，无法远程操作开门、关门";
                dispose = "需要远程操作干燥炉，请先切换至远程运行";
                ShowMessageBox((int)MsgID.RemoteErr, msg, dispose, MessageType.MsgAlarm);
                return false;
            }
            if (!CheckRobotTransferSafe(-1))
            {
                msg = string.Format("调度机器人在{0}层取放进，不能操作炉门", (cavityIdx + 1));
                dispose = string.Format("请将调度机器人操作到安全位后再操作炉门");
                ShowMessageBox((int)MsgID.RobotFingerIn, msg, dispose, MessageType.MsgAlarm);
                return false;
            }
            if ((short)OvenStatus.DoorOpen == cavityData.doorState)
            {
                if ((uint)OvenStatus.WorkStart == RCavity(cavityIdx).workState)
                {
                    msg = string.Format("{0}层腔体干燥中，不能打开炉门", (cavityIdx + 1));
                    dispose = string.Format("请等待烘烤结束后再打开炉门");
                    ShowMessageBox((int)MsgID.WorkingOpenDoor, msg, dispose, MessageType.MsgAlarm);
                    return false;
                }
                if (RCavity(cavityIdx).vacPressure < (this.cavityParameter.OpenDoorVacPressure - 500))
                {
                    msg = string.Format("{0}层腔体当前真空压力为[{1}] < 设置的开门真空压力[{2}]，不能打开炉门\r\n{3}"
                        , (cavityIdx + 1), RCavity(cavityIdx).vacPressure, this.cavityParameter.OpenDoorVacPressure
                        , (RCavity(cavityIdx).parameter.OpenDoorVacPressure < 90000) ? "建议设置开门气压为94000以上" : "");
                    dispose = string.Format("请先破真空后再打开炉门");
                    ShowMessageBox((int)MsgID.OpenDoorPressureAlm, msg, dispose, MessageType.MsgAlarm);
                    return false;
                }
                for(int i = 0; i < (int)OvenRowCol.MaxRow; i++)
                {
                    if((i != cavityIdx) && ((short)OvenStatus.DoorClose != RCavity(i).doorState))
                    {
                        msg = string.Format("{0}层炉门非关闭，不能同时打开两层炉门", (i + 1));
                        dispose = string.Format("请先关闭{0}层炉门后再打开{1}层炉门", (i + 1), (cavityIdx + 1));
                        ShowMessageBox((int)MsgID.OpenMultiDoorAlm, msg, dispose, MessageType.MsgAlarm);
                        return false;
                    }
                }
            }
            WriteLog($"{cavityIdx + 1}层炉门{((uint)OvenStatus.DoorOpen == cavityData.doorState ? "打开" : "关闭")}", mode);
            if (this.ovenClient.SetDryOvenData(DryOvenCmd.DoorOpenClose, cavityIdx, cavityData))
            {
                DateTime startTime = DateTime.Now;
                while(true)
                {
                    if (GetDryOvenData(ref readOvenData))
                    {
                        if(this.readOvenData.CavityDatas[cavityIdx].doorState == cavityData.doorState)
                        {
                            return true;
                        }
                    }
                    if ((DateTime.Now - startTime).TotalSeconds > this.openDoorDelay)
                    {
                        if (alarm)
                        {
                            msg = string.Format("{0}层{1}炉门超时[{2}秒]", (cavityIdx + 1)
                                , ((uint)OvenStatus.DoorOpen == cavityData.doorState ? "打开" : "关闭"), this.openDoorDelay);
                            dispose = "请检查干燥炉是否远程运行";
                            ShowMessageBox((int)MsgID.DoorOpenClose, msg, dispose, MessageType.MsgAlarm);
                        }
                        break;
                    }
                    Sleep(1);
                }
            }
            else if(alarm)
            {
                msg = string.Format("发送{0}层{1}炉门指令失败"
                    , (cavityIdx + 1), ((uint)OvenStatus.DoorOpen == cavityData.doorState ? "打开" : "关闭"));
                dispose = string.Format("请检查干燥炉是否连接");
                ShowMessageBox((int)MsgID.DoorOpenClose, msg, dispose, MessageType.MsgAlarm);
            }
            return false;
        }

        /// <summary>
        /// 打开/关闭干燥炉真空阀
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="cavityData"></param>
        /// <param name="alarm"></param>
        /// <returns></returns>
        public bool DryOvenVacuum(int cavityIdx, CavityData cavityData, bool alarm = true, OptMode mode = OptMode.Auto)
        {
            string msg, dispose;
            if((short)OvenStatus.RemoteOpen != this.readOvenData.RemoteState)
            {
                if (alarm)
                {
                    msg = "干燥炉非远程运行状态，无法远程操作真空阀";
                    dispose = "需要远程操作干燥炉，请先切换至远程运行";
                    ShowMessageBox((int)MsgID.RemoteErr, msg, dispose, MessageType.MsgAlarm);
                }
                return false;
            }
            if((uint)OvenStatus.WorkStart == RCavity(cavityIdx).workState)
            {
                if(alarm)
                {
                    msg = string.Format("{0}层腔体干燥中，不能操作真空阀", (cavityIdx + 1));
                    dispose = string.Format("请等待烘烤结束后再操作真空阀");
                    ShowMessageBox((int)MsgID.WorkingVacuum, msg, dispose, MessageType.MsgAlarm);
                }
                return false;
            }
            if(((uint)OvenStatus.VacOpen == cavityData.vacValveState)
                && ((uint)OvenStatus.BlowOpen == RCavity(cavityIdx).blowValveState))
            {
                if(alarm)
                {
                    msg = string.Format("{0}层腔体破真空阀已打开，不能打开真空阀", (cavityIdx + 1));
                    dispose = string.Format("请先关闭破真空阀后再打开真空阀");
                    ShowMessageBox((int)MsgID.VacOpenClose, msg, dispose, MessageType.MsgAlarm);
                }
                return false;
            }
            WriteLog($"{cavityIdx + 1}层真空阀{((uint)OvenStatus.VacOpen == cavityData.vacValveState ? "打开" : "关闭")}", mode);
            if(this.ovenClient.SetDryOvenData(DryOvenCmd.VacOpenClose, cavityIdx, cavityData))
            {
                DateTime startTime = DateTime.Now;
                while(true)
                {
                    if(GetDryOvenData(ref readOvenData))
                    {
                        if(this.readOvenData.CavityDatas[cavityIdx].vacValveState == cavityData.vacValveState)
                        {
                            return true;
                        }
                    }
                    if((DateTime.Now - startTime).TotalSeconds > this.openDoorDelay / 2)
                    {
                        if(alarm)
                        {
                            msg = string.Format("{0}层{1}真空阀超时[{2}秒]", (cavityIdx + 1)
                                , ((uint)OvenStatus.VacOpen == cavityData.vacValveState ? "打开" : "关闭"), 10);
                            dispose = string.Format("请检查干燥炉是否远程运行");
                            ShowMessageBox((int)MsgID.VacOpenClose, msg, dispose, MessageType.MsgAlarm);
                        }
                        break;
                    }
                    Sleep(1);
                }
            }
            else if(alarm)
            {
                msg = string.Format("发送{0}层{1}真空阀指令失败", (cavityIdx + 1)
                    , ((uint)OvenStatus.VacOpen == cavityData.vacValveState ? "打开" : "关闭"));
                dispose = string.Format("请检查干燥炉是否连接");
                ShowMessageBox((int)MsgID.VacOpenClose, msg, dispose, MessageType.MsgAlarm);
            }
            return false;
        }

        /// <summary>
        /// 打开/关闭干燥炉破真空阀
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="cavityData"></param>
        /// <param name="alarm"></param>
        /// <returns></returns>
        public bool DryOvenBlowAir(int cavityIdx, CavityData cavityData, bool alarm = true, OptMode mode = OptMode.Auto)
        {
            string msg, dispose;
            if((short)OvenStatus.RemoteOpen != this.readOvenData.RemoteState)
            {
                if(alarm)
                {
                    msg = "干燥炉非远程运行状态，无法远程操作破真空阀";
                    dispose = "需要远程操作干燥炉，请先切换至远程运行";
                    ShowMessageBox((int)MsgID.RemoteErr, msg, dispose, MessageType.MsgAlarm);
                }
                return false;
            }
            if((uint)OvenStatus.WorkStart == RCavity(cavityIdx).workState)
            {
                if(alarm)
                {
                    msg = string.Format("{0}层腔体干燥中，不能操作破真空阀", (cavityIdx + 1));
                    dispose = string.Format("请等待烘烤结束后再操作破真空阀");
                    ShowMessageBox((int)MsgID.WorkingBlowAir, msg, dispose, MessageType.MsgAlarm);
                }
                return false;
            }
            if(((uint)OvenStatus.VacOpen == RCavity(cavityIdx).vacValveState)
                && ((uint)OvenStatus.BlowOpen == cavityData.blowValveState))
            {
                if(alarm)
                {
                    msg = string.Format("{0}层腔体真空阀已打开，不能打开破真空阀", (cavityIdx + 1));
                    dispose = string.Format("请先关闭真空阀后再打开破真空阀");
                    ShowMessageBox((int)MsgID.VacOpenClose, msg, dispose, MessageType.MsgAlarm);
                }
                return false;
            }
            WriteLog($"{cavityIdx + 1}层破真空阀{((uint)OvenStatus.BlowOpen == cavityData.blowValveState ? "打开" : "关闭")}", mode);
            if(this.ovenClient.SetDryOvenData(DryOvenCmd.BlowOpenClose, cavityIdx, cavityData))
            {
                DateTime startTime = DateTime.Now;
                while(true)
                {
                    if(GetDryOvenData(ref readOvenData))
                    {
                        if(this.readOvenData.CavityDatas[cavityIdx].blowValveState == cavityData.blowValveState)
                        {
                            return true;
                        }
                    }
                    if((DateTime.Now - startTime).TotalSeconds > this.openDoorDelay / 2)
                    {
                        if(alarm)
                        {
                            msg = string.Format("{0}层{1}破真空阀超时[{2}秒]", (cavityIdx + 1)
                                , ((uint)OvenStatus.BlowOpen == cavityData.blowValveState ? "打开" : "关闭"), 10);
                            dispose = string.Format("请检查干燥炉是否远程运行");
                            ShowMessageBox((int)MsgID.BlowOpenClose, msg, dispose, MessageType.MsgAlarm);
                        }
                        break;
                    }
                    Sleep(1);
                }
            }
            else if(alarm)
            {
                msg = string.Format("发送{0}层{1}破真空阀指令失败", (cavityIdx + 1)
                    , ((uint)OvenStatus.BlowOpen == cavityData.blowValveState ? "打开" : "关闭"));
                dispose = string.Format("请检查干燥炉是否连接");
                ShowMessageBox((int)MsgID.BlowOpenClose, msg, dispose, MessageType.MsgAlarm);
            }
            return false;
        }

        /// <summary>
        /// 打开/关闭干燥炉保压
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="cavityData"></param>
        /// <param name="alarm"></param>
        /// <returns></returns>
        public bool DryOvenPressure(int cavityIdx, CavityData cavityData, bool alarm = true, OptMode mode = OptMode.Auto)
        {
            string msg, dispose;
            if((short)OvenStatus.RemoteOpen != this.readOvenData.RemoteState)
            {
                if(alarm)
                {
                    msg = "干燥炉非远程运行状态，无法远程操作设置保压";
                    dispose = "需要远程操作干燥炉，请先切换至远程运行";
                    ShowMessageID((int)MsgID.RemoteErr, msg, dispose, MessageType.MsgAlarm);
                }
                return false;
            }
            WriteLog($"{cavityIdx + 1}层保压{((uint)OvenStatus.PressureOpen == cavityData.pressureState ? "打开" : "关闭")}", mode);
            if(this.ovenClient.SetDryOvenData(DryOvenCmd.PressureOpenClose, cavityIdx, cavityData))
            {
                DateTime startTime = DateTime.Now;
                while(true)
                {
                    if(GetDryOvenData(ref readOvenData))
                    {
                        if(this.readOvenData.CavityDatas[cavityIdx].pressureState == cavityData.pressureState)
                        {
                            return true;
                        }
                    }
                    if((DateTime.Now - startTime).TotalSeconds > this.openDoorDelay / 10)
                    {
                        if(alarm)
                        {
                            msg = string.Format("{0}层{1}保压超时[{2}秒]", (cavityIdx + 1)
                                , ((uint)OvenStatus.PressureOpen == cavityData.pressureState ? "打开" : "关闭"), 10);
                            dispose = string.Format("请检查干燥炉是否远程运行");
                            ShowMessageID((int)MsgID.PressureOpenClose, msg, dispose, MessageType.MsgAlarm);
                        }
                        break;
                    }
                    Sleep(1);
                }
            }
            else if(alarm)
            {
                msg = string.Format("发送{0}层{1}保压指令失败", (cavityIdx + 1)
                    , ((uint)OvenStatus.PressureOpen == cavityData.pressureState ? "打开" : "关闭"));
                dispose = string.Format("请检查干燥炉是否连接");
                ShowMessageID((int)MsgID.PressureOpenClose, msg, dispose, MessageType.MsgAlarm);
            }
            return false;
        }

        /// <summary>
        /// 启动/停止干燥炉加热
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="cavityData"></param>
        /// <param name="alarm"></param>
        /// <returns></returns>
        public bool DryOvenWorkStart(int cavityIdx, CavityData cavityData, bool alarm = true, OptMode mode = OptMode.Auto)
        {
            string msg, dispose;
            if((short)OvenStatus.RemoteOpen != this.readOvenData.RemoteState)
            {
                msg = "干燥炉非远程运行状态，无法远程操作加热启动/停止";
                dispose = "需要远程操作干燥炉，请先切换至远程运行";
                ShowMessageBox((int)MsgID.RemoteErr, msg, dispose, MessageType.MsgAlarm);
                return false;
            }
            if (((uint)OvenStatus.WorkStart == cavityData.workState) 
                && RCavity(cavityIdx).vacPressure < this.cavityParameter.OpenDoorVacPressure)
            {
                msg = $"干燥炉当前真空压力为{RCavity(cavityIdx).vacPressure} < 开门气压{this.cavityParameter.OpenDoorVacPressure}，可能导致无法操作加热启动";
                dispose = "需要操作干燥炉加热启动，请先破真空操作";
                ShowMessageBox((int)MsgID.WorkStartStop, msg, dispose, MessageType.MsgAlarm);
                return false;
            }
            WriteLog($"{cavityIdx + 1}层加热{((uint)OvenStatus.WorkStart == cavityData.workState ? "启动" : "停止")}", mode);
            if(this.ovenClient.SetDryOvenData(DryOvenCmd.WorkStartStop, cavityIdx, cavityData))
            {
                DateTime startTime = DateTime.Now;
                while(true)
                {
                    if(GetDryOvenData(ref readOvenData))
                    {
                        if(this.readOvenData.CavityDatas[cavityIdx].workState == cavityData.workState)
                        {
                            return true;
                        }
                    }
                    if((DateTime.Now - startTime).TotalSeconds > this.openDoorDelay / 10)
                    {
                        if(alarm)
                        {
                            msg = string.Format("{0}层{1}加热超时[{2}秒]", (cavityIdx + 1)
                                , ((uint)OvenStatus.WorkStart == cavityData.workState ? "启动" : "停止"), 10);
                            dispose = string.Format("请检查干燥炉是否远程运行");
                            ShowMessageBox((int)MsgID.WorkStartStop, msg, dispose, MessageType.MsgAlarm);
                        }
                        break;
                    }
                    Sleep(1);
                }
            }
            else if(alarm)
            {
                msg = string.Format("发送{0}层{1}加热指令失败", (cavityIdx + 1)
                    , ((uint)OvenStatus.WorkStart == cavityData.workState ? "启动" : "停止"));
                dispose = string.Format("请检查干燥炉是否连接");
                ShowMessageBox((int)MsgID.WorkStartStop, msg, dispose, MessageType.MsgAlarm);
            }
            return false;
        }

        /// <summary>
        /// 设置干燥炉参数
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="cavityData"></param>
        /// <param name="alarm"></param>
        /// <returns></returns>
        public bool DryOvenSetParameter(int cavityIdx, CavityData cavityData, bool alarm = true, OptMode mode = OptMode.Auto)
        {
            string msg, dispose;
            if((short)OvenStatus.RemoteOpen != this.readOvenData.RemoteState)
            {
                msg = "干燥炉非远程运行状态，无法远程设置参数";
                dispose = "需要远程操作干燥炉，请先切换至远程运行";
                ShowMessageBox((int)MsgID.RemoteErr, msg, dispose, MessageType.MsgAlarm);
                return false;
            }
            if((uint)OvenStatus.WorkStart == RCavity(cavityIdx).workState)
            {
                msg = string.Format("{0}层腔体干燥中，不能设置参数", (cavityIdx + 1));
                dispose = string.Format("请等待烘烤结束后再设置参数");
                ShowMessageBox((int)MsgID.WorkingSetParameter, msg, dispose, MessageType.MsgAlarm);
                return false;
            }
            if (!UpdataBillParameter(ref cavityData.parameter))
            {
                return false;
            }
            msg = $"{cavityIdx + 1}层炉腔发送参数";
            msg += " 1)SetTempValue:" + cavityData.parameter.SetTempValue;
            msg += " 2)TempUpperlimit:" + cavityData.parameter.TempUpperlimit;
            msg += " 3)TempLowerlimit:" + cavityData.parameter.TempLowerlimit;
            msg += " 4)PreheatTime:" + cavityData.parameter.PreheatTime;
            msg += " 5)VacHeatTime:" + cavityData.parameter.VacHeatTime;
            msg += " 6)OpenDoorBlowTime:" + cavityData.parameter.OpenDoorBlowTime;
            msg += " 7)OpenDoorVacPressure:" + cavityData.parameter.OpenDoorVacPressure;
            msg += " 8)AStateVacTime:" + cavityData.parameter.AStateVacTime;
            msg += " 9)AStateVacPressure:" + cavityData.parameter.AStateVacPressure;
            msg += " 10)BStateVacTime:" + cavityData.parameter.BStateVacTime;
            msg += " 11)BStateVacPressure:" + cavityData.parameter.BStateVacPressure;
            msg += " 12)BStateBlowAirTime:" + cavityData.parameter.BStateBlowAirTime;
            msg += " 13)BStateBlowAirPressure:" + cavityData.parameter.BStateBlowAirPressure;
            msg += " 14)BStateBlowAirKeepTime:" + cavityData.parameter.BStateBlowAirKeepTime;
            msg += " 15)BreathTimeInterval:" + cavityData.parameter.BreathTimeInterval;
            msg += " 16)BreathCycleTimes:" + cavityData.parameter.BreathCycleTimes;
            msg += " 17)HeatPlate:" + cavityData.parameter.HeatPlate;
            msg += " 18)MaxNGHeatPlate:" + cavityData.parameter.MaxNGHeatPlate;
            msg += " 19)HeatPreVacTime:" + cavityData.parameter.HeatPreVacTime;
            msg += " 20)HeatPreBlow:" + cavityData.parameter.HeatPreBlow;
            WriteLog(msg, mode);

            if(this.ovenClient.SetDryOvenData(DryOvenCmd.SetParameter, cavityIdx, cavityData))
            {
                DateTime startTime = DateTime.Now;
                while(true)
                {
                    if(GetDryOvenData(ref readOvenData))
                    {
                        CavityParameter rParm = RCavity(cavityIdx).parameter;
                        CavityParameter wParm = cavityData.parameter;
                        if((rParm.SetTempValue == wParm.SetTempValue)
                            && (rParm.TempUpperlimit == wParm.TempUpperlimit)
                            && (rParm.TempLowerlimit == wParm.TempLowerlimit)
                            && (rParm.PreheatTime == wParm.PreheatTime)
                            && (rParm.VacHeatTime == wParm.VacHeatTime)
                            && (rParm.BStateVacTime == wParm.BStateVacTime)
                            && (rParm.BStateVacPressure == wParm.BStateVacPressure)
                            && (rParm.OpenDoorBlowTime == wParm.OpenDoorBlowTime)
                            && (rParm.OpenDoorVacPressure == wParm.OpenDoorVacPressure)
                            && (rParm.AStateVacTime == wParm.AStateVacTime)
                            && (rParm.AStateVacPressure == wParm.AStateVacPressure)
                            && (rParm.BStateBlowAirTime == wParm.BStateBlowAirTime)
                            && (rParm.BStateBlowAirPressure == wParm.BStateBlowAirPressure)
                            && (rParm.BStateBlowAirKeepTime == wParm.BStateBlowAirKeepTime)
                            && (rParm.BreathTimeInterval == wParm.BreathTimeInterval)
                            && (rParm.BreathCycleTimes == wParm.BreathCycleTimes)
                            && (rParm.HeatPlate == wParm.HeatPlate)
                            && (rParm.MaxNGHeatPlate == wParm.MaxNGHeatPlate)
                            && (rParm.HeatPreVacTime == wParm.HeatPreVacTime)
                            && (rParm.HeatPreBlow == wParm.HeatPreBlow))
                        {
                            return true;
                        }
                    }
                    if((DateTime.Now - startTime).TotalSeconds > this.openDoorDelay / 10)
                    {
                        if(alarm)
                        {
                            msg = string.Format("{0}层发送参数设置超时[{1}秒]", (cavityIdx + 1), 10);
                            dispose = string.Format("请检查干燥炉是否远程运行");
                            ShowMessageBox((int)MsgID.SetParameter, msg, dispose, MessageType.MsgAlarm);
                        }
                        break;
                    }
                    Sleep(1);
                }
            }
            else if(alarm)
            {
                msg = string.Format("{0}层发送参数设置指令失败", (cavityIdx + 1));
                dispose = string.Format("请检查干燥炉是否连接");
                ShowMessageBox((int)MsgID.SetParameter, msg, dispose, MessageType.MsgAlarm);
            }
            return false;
        }

        /// <summary>
        /// 解除干燥炉维修状态
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="cavityData"></param>
        /// <param name="alarm"></param>
        /// <returns></returns>
        public bool DryOvenFaultReset(int cavityIdx, CavityData cavityData, bool alarm = true, OptMode mode = OptMode.Auto)
        {
            WriteLog($"{cavityIdx + 1}层解除维修状态", mode);
            if(CavityStatus.Maintenance == this.CavityState[cavityIdx])
            {
                SetCavityState(cavityIdx, CavityStatus.Normal);
                return true;
            }
            return false;

            string msg, dispose;
            if(this.ovenClient.SetDryOvenData(DryOvenCmd.FaultReset, cavityIdx, cavityData))
            {
                return true;
            }
            else if(alarm)
            {
                msg = string.Format("发送故障复位指令失败");
                dispose = string.Format("请检查干燥炉是否连接");
                ShowMessageBox((int)MsgID.FaultReset, msg, dispose, MessageType.MsgAlarm);
            }
            return false;
        }

        /// <summary>
        /// 发送门禁状态至干燥炉
        /// </summary>
        /// <param name="mcDoorOpen"></param>
        /// <param name="alarm"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public bool DryOvenSetMcDoorState(bool mcDoorOpen, bool alarm = true, OptMode mode = OptMode.Auto)
        {
            CavityData cavityData = new CavityData();
            cavityData.mcDoorState = (short)(mcDoorOpen ? OvenStatus.McDoorOpen : OvenStatus.McDoorClose);
            string msg, dispose;
            //WriteLog($"发送门禁状态至干燥炉 {(mcDoorOpen ? "门开" : "门关")}", mode);
            if(this.ovenClient.SetDryOvenData(DryOvenCmd.SetMcDoor, 0, cavityData))
            {
                DateTime startTime = DateTime.Now;
                while(true)
                {
                    if(GetDryOvenData(ref readOvenData))
                    {
                        if(cavityData.mcDoorState == this.readOvenData.MCDoorState)
                        {
                            return true;
                        }
                    }
                    if((DateTime.Now - startTime).TotalSeconds > 3)
                    {
                        if(alarm)
                        {
                            msg = $"发送门禁状态至干燥炉 {(mcDoorOpen ? "门开" : "门关")}超时";
                            dispose = string.Format("请检查干燥炉是否远程运行");
                            ShowMessageID((int)MsgID.PressureOpenClose, msg, dispose, MessageType.MsgAlarm);
                        }
                        break;
                    }
                    Sleep(1);
                }
            }
            else if(alarm)
            {
                msg = string.Format("发送调度门禁状态指令失败");
                dispose = string.Format("请检查干燥炉是否连接");
                ShowMessageID((int)MsgID.SetMcDoor, msg, dispose, MessageType.MsgAlarm);
            }
            return false;
        }

        /// <summary>
        /// 获取干燥炉数据
        /// </summary>
        /// <param name="ovenData"></param>
        /// <returns></returns>
        private bool GetDryOvenData(ref DryingOvenData ovenData)
        {
            return this.ovenClient.GetDryOvenData(ref ovenData);
        }

        /// <summary>
        /// 更新工艺参数
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private bool UpdataBillParameter(ref CavityParameter param)
        {
            MesConfig mesCfg = MesDefine.GetMesCfg(MesInterface.ApplyTechProParam);
            if(null != mesCfg)
            {
                if (MachineCtrl.GetInstance().UpdataMes && mesCfg.enable)
                {
                    foreach(var item in mesCfg.parameter.Values)
                    {
                        // 0407 注释
                        //switch(item.Key)
                        //{
                        //    case nameof(param.SetTempValue):
                        //        {
                        //            float value = 0f;
                        //            if(float.TryParse(item.Value, out value))
                        //            {
                        //                this.cavityParameter.SetTempValue = value;
                        //                WriteParameter(this.RunModule, item.Key, value.ToString());
                        //            }
                        //            if(float.TryParse(item.Upper, out value))
                        //            {
                        //                this.cavityParameter.TempUpperlimit = value;
                        //                WriteParameter(this.RunModule, item.Key, value.ToString());
                        //            }
                        //            if(float.TryParse(item.Lower, out value))
                        //            {
                        //                this.cavityParameter.TempLowerlimit = value;
                        //                WriteParameter(this.RunModule, item.Key, value.ToString());
                        //            }
                        //            break;
                        //        }
                        //    case nameof(param.BStateVacPressure):
                        //        {
                        //            uint value = 0;
                        //            if(uint.TryParse(item.Value, out value))
                        //            {
                        //                this.cavityParameter.BStateVacPressure = value;
                        //                WriteParameter(this.RunModule, item.Key, value.ToString());
                        //            }
                        //            break;
                        //        }
                        //    case nameof(param.PreheatTime):
                        //        {
                        //            uint value = 0;
                        //            if(uint.TryParse(item.Value, out value))
                        //            {
                        //                this.cavityParameter.PreheatTime = value;
                        //                WriteParameter(this.RunModule, item.Key, value.ToString());
                        //            }
                        //            break;
                        //        }
                        //    case nameof(param.VacHeatTime):
                        //        {
                        //            uint value = 0;
                        //            if(uint.TryParse(item.Value, out value))
                        //            {
                        //                this.cavityParameter.VacHeatTime = value;
                        //                WriteParameter(this.RunModule, item.Key, value.ToString());
                        //            }
                        //            break;
                        //        }
                        //    default:
                        //        break;
                        //}
                    }
                }
                param.Copy(this.cavityParameter);
                return true;
            }
            return false;
        }
        #endregion

        #region // 后台线程

        /// <summary>
        /// 初始化线程(开始运行)
        /// </summary>
        private bool InitThread()
        {
            try
            {
                this.runWhileTask = new Task(RunWhileThread, TaskCreationOptions.LongRunning);
                this.runWhileTask.Start();
                Def.WriteLog("RunProcessDryingOven ", $"InitThread():RunWhileThread = {runWhileTask.Id} start", LogType.Success);
                return true;
            }
            catch(System.Exception ex)
            {
                Def.WriteLog("RunProcessDryingOven", $"InitThread: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 释放线程(终止运行)
        /// </summary>
        private bool ReleaseThread()
        {
            try
            {
                this.runWhileTask.Wait();
                Def.WriteLog("RunProcessDryingOven", $"ReleaseThread():RunWhileThread = {runWhileTask.Id} end", LogType.Success);
                return true;
            }
            catch(System.Exception ex)
            {
                Def.WriteLog("RunProcessDryingOven", $"ReleaseThread: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 后台线程
        /// </summary>
        private void RunWhileThread()
        {
            // 和主线程同生命周期
            while(!IsTerminate())
            {
                try
                {
                    RunWhile();
                }
                catch (System.Exception ex)
                {
                    Def.WriteLog("RunProcessDryingOven", $"RunWhileThread: {ex.Message}\r\n{ex.StackTrace}");
                }
                Sleep(1);
            }
        }

        /// <summary>
        /// 循环函数
        /// </summary>
        protected void RunWhile()
        {
            if(!this.DryRun && !DryOvenIsConnect())
            {
                Sleep(200);
                return;
            }

            if(!GetDryOvenData(ref this.readOvenData))
            {
                return;
            }
            if(this.DryRun)
            {
                RandomFaultState(ref this.readOvenData);
            }

            #region // 通讯已连接，但数据交互错误

            if (this.readOvenData.DataError)
            {
                ShowMessageID((int)MsgID.OvenDataError, "通讯已连接，但不能获取干燥炉数据", "请检查干燥炉是否报警或故障，处理完毕后断开重新连接", MessageType.MsgAlarm);
                return;
            }

            #endregion

            #region // 遍历炉腔状态
            for(int cavityIdx = 0; cavityIdx < (int)OvenRowCol.MaxRow; cavityIdx++)
            {
                CavityData cavity = RCavity(cavityIdx);

                #region // 检查异常报警

                // 炉门异常报警
                if(cavity.doorAlarm)
                {
                    string msg = string.Format("{0}层炉门异常报警", cavityIdx + 1);
                    ShowMessageID((int)MsgID.DoorAlarm + cavityIdx, msg, "请检查干燥炉", MessageType.MsgAlarm);
                }
                // 真空报警
                if(cavity.vacAlarm)
                {
                    string msg = string.Format("{0}层真空异常报警[报警值：{1}]"
                        , cavityIdx + 1, cavity.vacAlarmValue);
                    ShowMessageID((int)MsgID.VacAlarm + cavityIdx, msg, "请检查干燥炉", MessageType.MsgAlarm);
                }
                // 破真空报警
                if(cavity.blowAlarm)
                {
                    string msg = string.Format("{0}层破真空异常报警", cavityIdx + 1);
                    ShowMessageID((int)MsgID.BlowAlarm + cavityIdx, msg, "请检查干燥炉", MessageType.MsgAlarm);
                }
                // 真空计报警
                if(cavity.vacuometerAlarm)
                {
                    string msg = string.Format("{0}层真空计异常报警", cavityIdx + 1);
                    ShowMessageID((int)MsgID.VacuometerAlarm + cavityIdx, msg, "请检查干燥炉", MessageType.MsgAlarm);
                }
                for(int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                {
                    int almID = 0;
                    // 机械温控报警
                    if(cavity.controlAlarm[i])
                    {
                        almID = (int)MsgID.ControlAlarm + cavityIdx * (int)OvenRowCol.MaxCol + i;
                        string msg = $"{cavityIdx + 1}层夹具{i + 1}机械温控报警";
                        ShowMessageID(almID, msg, "请检查干燥炉", MessageType.MsgAlarm);
                    }
                    // 机械温控报警
                    if(cavity.pallletAlarm[i])
                    {
                        almID = (int)MsgID.PltCheckAlarm + cavityIdx * (int)OvenRowCol.MaxCol + i;
                        string msg = $"{cavityIdx + 1}层夹具{i + 1}夹具放平检测报警";
                        ShowMessageID(almID, msg, "请检查干燥炉", MessageType.MsgAlarm);
                    }
                }
                #endregion

                //检查维修状态，调用异常结束接口
                if (CavityStatus.Maintenance == this.CavityState[cavityIdx])
                {
                    string msg = "";
                    string rn = this.RunModule;
                    for (int i = 0; i < 3; i++)
                    {
                        //Baking开始/结束
                        if (!MesBakingStatusInfo(cavityIdx, BakingType.Abnormal_End, ref msg))
                        {
                            //获取报警字符串是否包含"超时"二字,如果没有则跳出循环
                            if (!msg.Contains("超时"))
                            {
                                break;
                            }
                            if (i == 2)
                            {
                                ShowMsgBox.ShowDialog($"MESBaking开始/结束接口超时失败多次，请检查MES连接状态", MessageType.MsgAlarm);
                            }
                        }
                        else
                        {
                            this.bakingNGType[cavityIdx] = BakingNGType.Abnormal;
                            //CavityStatus.Normal = this.CavityState[cavityIdx];
                            MesData.writeBakingNgType(this.RunModule, "BakingType"+ cavityIdx, Convert.ToInt32(BakingNGType.Abnormal));
                            break;
                        }
                        
                    }
                    SetCavityState(cavityIdx, CavityStatus.Normal);

                }

                    // 系统标记加热工作中
                    if (CavityStatus.Heating == this.CavityState[cavityIdx])
                {
                    #region // 检查温度报警
                    
                    // 温度报警
                    string[,] tempAlarm, tempAlarmValue;
                    string msg = "";
                    if(CheckTempAlarm(cavity, out tempAlarm, out tempAlarmValue))
                    {
                        for(int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                        {
                            if(SetCavityPltBatteryNG(cavityIdx, i, cavity))
                            {
                                foreach(var item in this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + i].Battery)
                                {
                                    if (BatteryStatus.NG == item.Type)
                                    {

                                        //for (int j = 0; j < (MesData.mesFrequency == 0 ? 3 : MesData.mesFrequency); j++)
                                        //{
                                        //    //MES不良品上传
                                        //    if (!MesRejectNGRecord(MesResources.OvenCavity[this.dryingOvenID, cavityIdx], MesResources.BillNo, item, ref msg))
                                        //    {
                                        //        //获取报警字符串是否包含"超时"二字,如果没有则跳出循环
                                        //        if (!msg.Contains("超时"))
                                        //        {
                                        //            break;
                                        //        }
                                        //        if (j == 2)
                                        //        {
                                        //            ShowMsgBox.ShowDialog($"MES不良品上传接口超时失败3次，请检查MES连接状态", MessageType.MsgAlarm);
                                        //        }
                                        //    }
                                        //    else
                                        //    {
                                        //        break;
                                        //    }
                                        //}
                                        ////MesRejectNGRecord(MesResources.OvenCavity[this.dryingOvenID, cavityIdx], MesResources.BillNo, item,ref msg);
                                    }
                                }
                            }
                        }

                        msg = string.Format("{0}层温度报警：\r\n", cavityIdx + 1);
                        for(int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                        {
                            msg += string.Format("夹具{0}：\r\n", (cavityIdx * (int)OvenRowCol.MaxCol + i + 1));
                            for(int j = 0; j < (int)OvenInfoCount.HeatPanelCount; j++)
                            {
                                if (!string.IsNullOrEmpty(tempAlarm[i, j]))
                                {
                                    msg += (tempAlarm[i, j] + tempAlarmValue[i, j] + "\r\n");
                                }
                            }
                        }
                        ShowMessageID((int)MsgID.TempAlarm + cavityIdx, msg, "请检查干燥炉", MessageType.MsgAlarm);
                    }
                    #endregion

                    #region // 检查加热完成

                    int pltIdx = cavityIdx * (int)OvenRowCol.MaxCol;
                    // 有夹具NG，则加热停止
                    if((PalletStatus.NG == this.Pallet[pltIdx].State) || (PalletStatus.NG == this.Pallet[pltIdx + 1].State))
                    {
                        WriteLog(string.Format("RunWhile()操作：{0}层炉腔有夹具NG，发送停止", cavityIdx + 1));
                        this.writeOvenData.CavityDatas[cavityIdx].workState = (int)OvenStatus.WorkStop;
                        DryOvenWorkStart(cavityIdx, this.writeOvenData.CavityDatas[cavityIdx], false);
                        SetCavityState(cavityIdx, CavityStatus.Maintenance);

                    }
                    // 无夹具NG　&&　炉腔已停止加热
                    else if((int)OvenStatus.WorkStop == cavity.workState)
                    {
                        
                        bool result = false;
                        uint setTime = (cavity.parameter.PreheatTime + cavity.parameter.VacHeatTime - (uint)this.maxWorkTimeRange);
                        // 加热时间足够
                        if(cavity.workTime >= setTime)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                //Baking开始/结束
                                if (!MesBakingStatusInfo(cavityIdx, BakingType.Normal_End, ref msg))
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
                                        ShowMsgBox.ShowDialog($"MESBaking开始/结束接口超时失败3次，请检查MES连接状态", MessageType.MsgAlarm);
                                    }
                                }
                                else
                                {
                                    result = true;
                                    MesData.writeBakingNgType(this.RunModule, "BakingType" + cavityIdx, Convert.ToInt32(BakingNGType.Normal));
                                    break;
                                }
                            }

                            //if (MesBakingStatusInfo(cavityIdx,BakingType.Normal_End))
                            if(result)
                            {
                                bool hasFake = false;
                                for(int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                {
                                    if(this.Pallet[pltIdx + i].HasFake())
                                    {
                                        hasFake = true;
                                        break;
                                    }
                                }
                                if(hasFake)
                                {
                                    // 设置夹具-腔体状态：有假电池夹具，置为待检测
                                    for(int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                    {
                                        if(PalletStatus.OK == this.Pallet[pltIdx + i].State)
                                        {
                                            this.Pallet[pltIdx + i].State = PalletStatus.Detect;
                                            this.Pallet[pltIdx + i].EndDate = DateTime.Now;
                                        }
                                    }
                                    SetCavityState(cavityIdx, CavityStatus.WaitDetect);
                                }
                                else
                                {
                                    // 设置夹具-腔体状态：无假电池夹具，置为等待结果
                                    for(int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                    {
                                        if(PalletStatus.OK == this.Pallet[pltIdx + i].State)
                                        {
                                            this.Pallet[pltIdx + i].State = PalletStatus.WaitResult;
                                            this.Pallet[pltIdx + i].EndDate = DateTime.Now;
                                        }
                                    }
                                    SetCavityState(cavityIdx, CavityStatus.WaitResult);
                                }
                                SaveRunData(SaveType.Pallet);
                            }
                        }
                        // 加热时间不足 && 且假电池夹具含有NG电池
                        else if((cavity.workTime < setTime) && ((this.Pallet[pltIdx].HasFake() && PltHasNGBat(this.Pallet[pltIdx]))
                            || (this.Pallet[pltIdx + 1].HasFake() && PltHasNGBat(this.Pallet[pltIdx + 1]))))
                        {
                            SetCavityState(cavityIdx, CavityStatus.Maintenance);
                        }
                        // 加热时间不足 && 真空报警
                        else if((cavity.workTime < setTime) && (cavity.vacAlarm))
                        {
                            SetCavityState(cavityIdx, CavityStatus.Maintenance);
                        }
                        // 加热时间不足
                        else if(cavity.workTime < setTime)
                        {
                            SetCavityState(cavityIdx, CavityStatus.Maintenance);

                            msg = string.Format("{0}层异常停止加热，加热时间【{1}】 < 设定时间【{2} + {3}】，请检查！"
                                , (cavityIdx + 1), cavity.workTime, cavity.parameter.PreheatTime, cavity.parameter.VacHeatTime);
                            ShowMessageID((int)MsgID.HeatStop + cavityIdx, msg, "请检查干燥炉", MessageType.MsgWarning);
                        }
                    }
                    #endregion
                }

                #region // 腔体实际加热中

                if((uint)OvenStatus.WorkStart == cavity.workState)
                {
                    // 间隔保存一次
                    if((DateTime.Now - this.bakingDataStartTime[cavityIdx]).TotalSeconds >= (this.workDataTime))
                    {
                        SaveWorkingData(cavityIdx, cavity);
                        this.bakingDataStartTime[cavityIdx] = DateTime.Now;

                        if (null != this.PltHeatTemp)
                        {
                            // 腔体中夹具
                            for(int col = 0; col < (int)OvenRowCol.MaxCol; col++)
                            {
                                int pltIdx = cavityIdx * (int)OvenRowCol.MaxCol + col;
                                // 控温、巡检
                                for(int i = 0; i < 2; i++)
                                {
                                    // 发热板
                                    for(int j = 0; j < (int)OvenInfoCount.HeatPanelCount; j++)
                                    {
                                        int heatIdx = i * (int)OvenInfoCount.HeatPanelCount + j;
                                        this.PltHeatTemp[pltIdx][heatIdx].Add(cavity.tempValue[col, i, j]);
                                        long time = this.workDataTime;
                                        if (this.PltHeatTime[pltIdx][heatIdx].Count > 0)
                                        {
                                            time += this.PltHeatTime[pltIdx][heatIdx][this.PltHeatTime[pltIdx][heatIdx].Count - 1];
                                        }
                                        this.PltHeatTime[pltIdx][heatIdx].Add(Convert.ToUInt32(time));
                                    }
                                }
                            }

                        }
                    }
                    // 加热时间超过设定时间，停止加热
                    if(cavity.workTime > cavity.parameter.PreheatTime + cavity.parameter.VacHeatTime + 30)
                    {
                        //this.writeOvenData.CavityDatas[cavityIdx].workState = (uint)OvenStatus.WorkStop;
                        //DryOvenWorkStart(cavityIdx, this.writeOvenData.CavityDatas[cavityIdx], false);
                        {
                            string msg = string.Format("{0}层腔体已加热{1}分钟，超过设定时间{2} + {3} + 30分钟，触发加热时间防呆提示"
                                , cavityIdx + 1, cavity.workTime, cavity.parameter.PreheatTime, cavity.parameter.VacHeatTime);
                            ShowMessageID((int)MsgID.HeatTimeout + cavityIdx, msg, "请检查干燥炉", MessageType.MsgWarning);
                        }
                    }
                    // 加热过程中，真空阶段未抽真空
                    if((cavity.workTime > cavity.parameter.PreheatTime + cavity.parameter.AStateVacTime + (cavity.parameter.BStateVacTime * 2))
                        && (cavity.vacPressure > cavity.parameter.BStateVacPressure) && (cavity.parameter.BreathCycleTimes < 1))
                    {
                        string msg = string.Format("{0}层腔体已加热{1}分钟，超过设定时间{2} + {3} + {4}分钟后，真空压力{5} > 设定压力值{6}，触发真空防呆提示"
                            , cavityIdx + 1, cavity.workTime, cavity.parameter.PreheatTime, cavity.parameter.AStateVacTime, cavity.parameter.BStateVacTime
                            , cavity.vacPressure, cavity.parameter.BStateVacPressure);
                        ShowMessageID((int)MsgID.HeatVacAlarm + cavityIdx, msg, "请检查干燥炉", MessageType.MsgWarning);
                    }
                }
                #endregion

                #region // 腔体停止中，判断保压，提前破真空

                if(!this.DryRun && (uint)OvenStatus.WorkStop == cavity.workState)
                {
                    // 等待测试结果时自动保压
                    if(this.waitResultPressure
                        && (CavityStatus.WaitResult == this.CavityState[cavityIdx])
                        && ((short)OvenStatus.DoorClose == cavity.doorState))
                    {
                        if (((uint)OvenStatus.PressureOpen != cavity.pressureState))
                        {
                            WriteLog("RunWhile()操作：等待测试结果时自动保压");
                            this.writeOvenData.CavityDatas[cavityIdx].pressureState = (uint)OvenStatus.PressureOpen;
                            if(!DryOvenPressure(cavityIdx, this.writeOvenData.CavityDatas[cavityIdx], true))
                            {
                                string msg = string.Format("{0}层等待测试结果时自动保压设置失败", cavityIdx + 1);
                                ShowMessageID((int)MsgID.PressureOpenClose, msg, "请检查干燥炉是否是远程状态", MessageType.MsgWarning);
                            }
                        }
                    }
                    // 设置保压
                    else if(this.CavityEnable[cavityIdx] && !this.CavityTransfer[cavityIdx] && (CavityStatus.Maintenance != this.CavityState[cavityIdx])
                        && ((uint)(this.CavityPressure[cavityIdx] ? OvenStatus.PressureOpen : OvenStatus.PressureClose) != cavity.pressureState))
                    {
                        //WriteLog("RunWhile()操作：设置保压：" + (this.CavityPressure[cavityIdx] ? "打开" : "关闭"));
                        this.writeOvenData.CavityDatas[cavityIdx].pressureState = (uint)(this.CavityPressure[cavityIdx] ? OvenStatus.PressureOpen : OvenStatus.PressureClose);
                        if(!DryOvenPressure(cavityIdx, this.writeOvenData.CavityDatas[cavityIdx], true))
                        {
                            string msg = string.Format("{0}层{1}保压失败"
                                , (cavityIdx + 1), (this.CavityPressure[cavityIdx] ? "打开" : "关闭"));
                            ShowMessageID((int)MsgID.PressureOpenClose, msg, "请检查干燥炉是否是远程状态", MessageType.MsgWarning);
                        }
                    }
                    // 提前破真空
                    if(this.CavityEnable[cavityIdx] && !this.CavityPressure[cavityIdx] && !this.CavityTransfer[cavityIdx]
                        && (CavityStatus.WaitResult != this.CavityState[cavityIdx])
                        && (CavityStatus.Maintenance != this.CavityState[cavityIdx])
                        && (cavity.vacPressure < (this.cavityParameter.OpenDoorVacPressure - 500))
                        && ((short)OvenStatus.BlowOpen != RCavity(cavityIdx).blowValveState))
                    {
                        //WriteLog($"RunWhile()操作：提前破真空[{cavity.vacPressure}<{this.cavityParameter.OpenDoorVacPressure}]：关闭真空阀，打开破真空阀");
                        this.writeOvenData.CavityDatas[cavityIdx].vacValveState = (short)OvenStatus.VacClose;
                        DryOvenVacuum(cavityIdx, this.writeOvenData.CavityDatas[cavityIdx], false);
                        this.writeOvenData.CavityDatas[cavityIdx].blowValveState = (short)OvenStatus.BlowOpen;
                        if(!DryOvenBlowAir(cavityIdx, this.writeOvenData.CavityDatas[cavityIdx], false))
                        {
                            string msg = string.Format("{0}层真空压力为{1} < {2}，提前破真空时打开破真空失败，请检查干燥炉"
                                , (cavityIdx + 1), cavity.vacPressure, this.cavityParameter.OpenDoorVacPressure);
                            ShowMessageID((int)MsgID.PressureOpenClose, msg, "请检查干燥炉是否是远程状态", MessageType.MsgWarning);
                        }
                    }
                }
                #endregion

                #region // 上报腔体状态给MES

                if (this.CavityState[cavityIdx] != this.CavityOldState[cavityIdx])
                {
                    this.CavityOldState[cavityIdx] = this.CavityState[cavityIdx];

                    MesMCState mesMC = MesMCState.Stop;
                    switch(this.CavityOldState[cavityIdx])
                    {
                        case CavityStatus.Normal:
                        case CavityStatus.WaitRebaking:
                            mesMC = MesMCState.Waiting;
                            break;
                        case CavityStatus.Heating:
                        case CavityStatus.WaitDetect:
                        case CavityStatus.WaitResult:
                            mesMC = MesMCState.Running;
                            break;
                        case CavityStatus.Maintenance:
                            mesMC = MesMCState.Alarm;
                            break;
                    }
                    if(!MesOperateMySql.EquipmentReal(mesMC, MesResources.OvenCavity[this.dryingOvenID, cavityIdx])
                        || !MesOperateMySql.EquipmentOperation(mesMC, MesResources.OvenCavity[this.dryingOvenID, cavityIdx]))
                    {
                        if(!MesOperateMySql.MySqlIsOpen())
                        {
                            ShowMessageID((int)MsgID.MySqlDisconnect, "MySql断开连接", "请确保上料工控机开机，然后等待MySql自动连接", MessageType.MsgAlarm);
                        }
                    }
                }
                #endregion
            }
            #endregion

            #region // 安全门状态写入干燥炉

            bool safeDoor = MachineCtrl.GetInstance().SafeDoorState;
            if((short)(safeDoor ? OvenStatus.McDoorOpen : OvenStatus.McDoorClose) != this.readOvenData.MCDoorState)
            {
                if(DryOvenIsConnect())
                {
                    DryOvenSetMcDoorState(safeDoor, true);
                }
            }
            #endregion

        }
        #endregion

        #region // 腔体工作

        /// <summary>
        /// 检查腔体是否等待工作开始
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        private bool CheckWaitWorkStart(int cavityIdx)
        {
            if(CavityStatus.Normal == this.CavityState[cavityIdx])
            {
                if(CavityEnable[cavityIdx] && !CavityPressure[cavityIdx] && !CavityPressure[cavityIdx])
                {
                    int idx = cavityIdx * (int)OvenRowCol.MaxCol;
                    if((PalletStatus.OK == this.Pallet[idx].State) && (PalletStage.Onload == this.Pallet[idx].Stage) && !this.Pallet[idx].IsEmpty()
                        && (PalletStatus.OK == this.Pallet[idx + 1].State) && (PalletStage.Onload == this.Pallet[idx + 1].Stage) && !this.Pallet[idx + 1].IsEmpty())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 设置腔体状态
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private bool SetCavityState(int cavityIdx, CavityStatus state)
        {
            if((0 > cavityIdx) || (cavityIdx >= (int)PalletRowCol.MaxRow))
            {
                return false;
            }
            if (CavityStatus.Maintenance == state)
            {
                this.CavityEnable[cavityIdx] = false;
                WriteParameter(this.RunModule, ("CavityEnable" + cavityIdx), bool.FalseString);
            }
            this.CavityState[cavityIdx] = state;
            SaveRunData(SaveType.Cavity);
            return true;
        }

        /// <summary>
        /// 设置腔体水含量
        /// </summary>
        /// <param name="nTier"></param>
        /// <param name="dWater"></param>
        public void SetWaterContent(int tier, double[] water)
        {
            if ((tier > -1) && (tier < (int)OvenRowCol.MaxRow) 
                && (water.Length == this.waterContentValue.GetLength(1)))
            {
                for(int i = 0; i < water.Length; i++)
                {
                    this.waterContentValue[tier, i] = water[i];
                }
                SaveRunData(SaveType.Variables);
            }
        }

        /// <summary>
        /// 检查水含量结果：true检查完成
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="waterValue"></param>
        /// <returns>true检查完成，false检查条件不满足</returns>
        private bool CheckWaterContentResult(int cavityIdx, double[,] waterValue)
        {
            for(int i = 0; i < waterValue.GetLength(1); i++)
            {
                if(waterValue[cavityIdx, i] <= 0.0)
                {
                    return false;
                }
            }
            if(CavityStatus.WaitResult == this.CavityState[cavityIdx])
            {
                Pallet[] plt = new Pallet[(int)OvenRowCol.MaxCol];
                // 检查夹具状态
                for(int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                {
                    plt[i] = this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + i];
                    if((PalletStatus.WaitResult != plt[i].State)
                        || (PalletStage.Onload != plt[i].Stage))
                    {
                        return false;
                    }
 
                }
                bool waterOK = true;

                if(waterValue[cavityIdx, 0] > this.waterStandardAnode)
                {
                    waterOK = false;
                    ShowMessageID((int)MsgID.WaterValueExceed, $"阳极水含量{waterValue[cavityIdx, 0]}超标[{this.waterStandardAnode}]", "请回炉", MessageType.MsgWarning);
                }
                for(int i = 1; i < waterValue.GetLength(1); i++)
                {
                    if(waterValue[cavityIdx, i] > this.waterStandardCathode)
                    {
                        waterOK = false;
                        ShowMessageID((int)MsgID.WaterValueExceed, $"阴极水含量{waterValue[cavityIdx, 0]}超标[{this.waterStandardCathode}]", "请回炉", MessageType.MsgWarning);
                        break;
                    }
                }
                try
                {
                    string msg = "";
                    string operatecode = "";
                    bool result = false;
                    SaveMesParamaer(cavityIdx);
                    // 上传成功再置夹具状态
                    if (!this.mesUpdataState[0])
                    {
                        this.mesUpdataState[0] = MesParameterData(MesResources.OvenCavity[this.dryingOvenID, cavityIdx], cavityIdx, waterValue, waterOK);
                    }
                    if(!this.mesUpdataState[1])
                    {
                        this.mesUpdataState[1] = MesOperateMySql.ProductionRecord(MesResources.OvenCavity[this.dryingOvenID, cavityIdx], plt);
                    }
                    if(!this.mesUpdataState[2])
                    {
                        // 0413   注释
                        //for (int i = 0; i < (MesData.mesFrequency == 0 ? 3 : MesData.mesFrequency); i++)
                        //{
                        //    //MES水含量结果上传
                        //    if (!MesWaterValueInfo(cavityIdx, waterValue, (plt[0].BakingCount),ref msg))
                        //    {
                        //        //获取报警字符串是否包含"超时"二字,如果没有则跳出循环
                        //        if (!msg.Contains("超时"))
                        //        {
                        //            result = false;
                        //            break;
                        //        }
                        //        if (i == 2)
                        //        {
                        //            result = false;
                        //            ShowMsgBox.ShowDialog($"MES水含量结果上传接口超时失败3次，请检查MES连接状态", MessageType.MsgAlarm);
                        //        }
                        //    }
                        //    else
                        //    {
                        //        result = true;
                        //        break;
                        //    }
                        //}

                        ////this.mesUpdataState[2] = MesWaterValueInfo(cavityIdx, waterValue, (plt[0].BakingCount));
                        this.mesUpdataState[2] = result;
                    }
                    if(!this.mesUpdataState[3])
                    {
                        this.mesUpdataState[3] = FTPUploadFile(MesResources.OvenCavity[this.dryingOvenID, cavityIdx], cavityIdx);
                    }
                    //上传MES参数表
                    if (!this.mesUpdataState[4])
                    {
                        this.mesUpdataState[4] = FTPUploadMESParamerFile(MesResources.OvenCavity[this.dryingOvenID, cavityIdx], cavityIdx);
                    }
                    if(!this.mesUpdataState[5])
                    {
                        for (int i = 0; i < (MesData.mesFrequency == 0 ? 3 : MesData.mesFrequency); i++)
                        {
                            //MES生产履历记录
                            if (!MesProductionRecord(MesResources.OvenCavity[this.dryingOvenID, cavityIdx], cavityIdx, waterValue, plt, waterOK,ref msg,ref operatecode))
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
                                    ShowMsgBox.ShowDialog($"MES生产履历记录接口超时失败3次，请检查MES连接状态", MessageType.MsgAlarm);
                                }
                            }
                            else
                            {
                                result = true;
                                break;
                            }
                        }

                        //this.mesUpdataState[4] = MesProductionRecord(MesResources.OvenCavity[this.dryingOvenID, cavityIdx], cavityIdx, waterValue, plt, waterOK);
                        this.mesUpdataState[5] = result;
                    }
                    // 0414  注释
                    //foreach(var item in this.mesUpdataState)
                    //{
                    //    if (!item)
                    //    {
                    //        return false;
                    //    }
                    //}
                    Array.Clear(this.mesUpdataState, 0, this.mesUpdataState.Length);

                    // 只判断MES返回  OK情况
                    if (operatecode == "OK")
                    {
                        for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                        {
                            int pltIdx = cavityIdx * (int)OvenRowCol.MaxCol + i;
                            if (PalletStatus.WaitResult == plt[i].State)
                            {
                                plt[i].Stage = PalletStage.Baked;
                                plt[i].State = PalletStatus.WaitOffload;
                            }
                        }
                        SetCavityState(cavityIdx, CavityStatus.Normal);
                        
                    }
                    // 只判断MES返回  NG情况
                    else if (operatecode == "NG")
                    {
                        //if(plt[0].BakingCount >= MachineCtrl.GetInstance().BakingMaxCount)
                        {
                            for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                            {
                                if (PalletStatus.WaitResult == plt[i].State)
                                {
                                    // 假电池已被拿走，清除数据
                                    int fakeRow, fakeCol;
                                    fakeRow = fakeCol = -1;
                                    if (plt[i].GetFakePos(ref fakeRow, ref fakeCol))
                                    {
                                        plt[i].Battery[fakeRow, fakeCol].Release();
                                    }
                                    plt[i].State = PalletStatus.NG;
                                }
                            }
                            SetCavityState(cavityIdx, CavityStatus.Maintenance);
                        }
                    }
                    // 只判断MES返回  复烘情况
                    else if (operatecode == "FH")
                    {
                        for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                        {
                            if (PalletStatus.WaitResult == plt[i].State)
                            {
                                plt[i].State = PalletStatus.ReputFake;
                            }
                        }
                        SetCavityState(cavityIdx, CavityStatus.WaitRebaking);
                        //SaveWaterContentResult(cavityIdx, this.waterContentValue, waterOK);
                        //SaveRunData(SaveType.Pallet);
                        //return true;
                    }
                    SaveWaterContentResult(cavityIdx, this.waterContentValue, waterOK);
                    SaveRunData(SaveType.Pallet);
                    return true;

                }
                catch (System.Exception ex)
                {
                    Def.WriteLog("RunProcessDryingOven", $"CheckWaterContentResult() error : {ex.Message}\r\n{ex.StackTrace}");
                }
            }
            return false;
        }

        /// <summary>
        /// 检查腔体温度报警：夹具-发热板
        /// </summary>
        /// <param name="cavityData">腔体数据</param>
        /// <param name="alarmMsg">腔体夹具报警信息</param>
        /// <param name="alarmValue">腔体夹具报警温度值</param>
        /// <returns></returns>
        public bool CheckTempAlarm(CavityData cavityData, out string[,] alarmMsg, out string[,] alarmValue)
        {
            bool hasAlm = false;
            alarmMsg = new string[(int)OvenRowCol.MaxCol, (int)OvenInfoCount.HeatPanelCount];
            alarmValue = new string[(int)OvenRowCol.MaxCol, (int)OvenInfoCount.HeatPanelCount];
            for(int col = 0; col < (int)OvenRowCol.MaxCol; col++)
            {
                int pltID = (0 == this.dryingOvenGroup) ? col : ((int)OvenRowCol.MaxCol - 1 - col);
                for(int idx = 0; idx < (int)OvenInfoCount.HeatPanelCount; idx++)
                {
                    // 先赋值为空，防止外部不判断为null直接使用导致异常
                    alarmMsg[col, idx] = "";
                    for(int almIdx = 0; almIdx < (int)OvenTmpAlarm.End; almIdx++)
                    {
                        if(cavityData.tempAlarmValue[pltID, idx] < 1.0)
                        {
                            // 没有温度值查询下一个
                            continue;
                        }
                        if ((cavityData.tempAlarm[pltID, idx] & (0x01 << almIdx)) == (0x01 << almIdx))
                        {
                            switch ((OvenTmpAlarm)almIdx)
                            {
                                case OvenTmpAlarm.LowTmp:
                                    alarmMsg[col, idx] += string.Format("{0}低温", (idx + 1));
                                    break;
                                case OvenTmpAlarm.OverTmp:
                                    alarmMsg[col, idx] += string.Format("{0}超温", (idx + 1));
                                    break;
                                case OvenTmpAlarm.HighTmp:
                                    alarmMsg[col, idx] += string.Format("{0}超高温", (idx + 1));
                                    break;
                                case OvenTmpAlarm.Exceptional:
                                    alarmMsg[col, idx] += string.Format("{0}信号异常", (idx + 1));
                                    break;
                                case OvenTmpAlarm.Difference:
                                    alarmMsg[col, idx] += string.Format("{0}温差异常", (idx + 1));
                                    break;
                            }
                            hasAlm = true;
                        }
                    }
                    alarmValue[col, idx] = cavityData.tempAlarmValue[pltID, idx].ToString("#0.00");
                }
            }
            return hasAlm;
        }

        /// <summary>
        /// 随机生成腔体状态：空运行测试
        /// </summary>
        /// <param name="ovenData"></param>
        private void RandomFaultState(ref DryingOvenData ovenData)
        {
            for(int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            {
                double workTime = (DateTime.Now - this.Pallet[i * (int)OvenRowCol.MaxCol].StartDate).TotalMinutes;
                double setTime = (cavityParameter.PreheatTime + cavityParameter.VacHeatTime);
                if((CavityStatus.Heating == this.CavityState[i]) && (workTime >= setTime))
                {
                    ovenData.CavityDatas[i].workState = (uint)OvenStatus.WorkStop;
                    ovenData.CavityDatas[i].workTime = Convert.ToUInt32(setTime);
                    ovenData.CavityDatas[i].parameter.PreheatTime = cavityParameter.PreheatTime;
                    ovenData.CavityDatas[i].parameter.VacHeatTime = cavityParameter.VacHeatTime;
                }
            }
        }

        #endregion

        #region // 腔体数据

        /// <summary>
        /// 干燥炉依据分组，关于col列的实际索引
        /// </summary>
        /// <param name="col"></param>
        /// <returns>返回干燥炉col列的实际索引</returns>
        public int DryOvenGroupColIdx(int col)
        {
            return (0 == this.dryingOvenGroup) ? (col) : (1 - col);
        }

        /// <summary>
        /// 获取干燥炉的远程运行状态
        /// </summary>
        /// <returns></returns>
        public short DryOvenRemoteState()
        {
            return this.readOvenData.RemoteState;
        }

        /// <summary>
        /// 获取干燥炉的远程写入的设备安全门状态
        /// </summary>
        /// <returns></returns>
        public short DryOvenMcDoorState()
        {
            return this.readOvenData.MCDoorState;
        }

        /// <summary>
        /// 读取的腔体数据
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        public CavityData RCavity(int cavityIdx)
        {
            return readOvenData.CavityDatas[cavityIdx];
        }

        /// <summary>
        /// 写入的腔体数据
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        private CavityData WCavity(int cavityIdx)
        {
            return writeOvenData.CavityDatas[cavityIdx];
        }

        #endregion

        #region // IO及电机操作接口

        /// <summary>
        /// 夹具放平检测
        /// </summary>
        /// <param name="pltIdx"></param>
        /// <param name="hasPlt"></param>
        /// <param name="alarm"></param>
        public override bool PalletKeepFlat(int pltIdx, bool hasPlt, bool alarm = true)
        {
            if(pltIdx < 0 || pltIdx >= (int)ModuleMaxPallet.DryingOven)
            {
                return false;
            }
            int cavityIdx = pltIdx / ((int)OvenRowCol.MaxCol);
            int pltCol = (0 == this.dryingOvenGroup) ? (pltIdx % ((int)OvenRowCol.MaxCol)) : (1 - pltIdx % ((int)OvenRowCol.MaxCol));
            if((RCavity(cavityIdx).pallletAlarm[pltCol])
                || (RCavity(cavityIdx).palletState[pltCol] != (short)(hasPlt ? OvenStatus.PalletHave : OvenStatus.PalletNot)))
            {
                if (alarm)
                {
                    ShowMessageID((int)MsgID.PltStateErr, ("夹具状态错误，应该为" + (hasPlt ? "ON" : "OFF")), "请检查干燥炉中夹具状态是否正确", MessageType.MsgAlarm);
                }
                return false;
            }
            return true;
        }

        #endregion

        #region // 安全检查

        /// <summary>
        /// 检查机器人是否在cavityIdx腔体中
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        public bool CheckRobotTransferSafe(int cavityIdx)
        {
            RobotActionInfo action = new RobotActionInfo();
            RunProcessRobotTransfer run = MachineCtrl.GetInstance().GetModule(RunID.Transfer) as RunProcessRobotTransfer;
            if(null != run)
            {
                action = run.GetRobotActionInfo(false);
                for(int i = 0; i < (int)OvenRowCol.MaxRow; i++)
                {
                    if (action.station == ((int)TransferRobotStation.DryOven_0 + this.dryingOvenID))
                    {
                        if((i == cavityIdx) || (cavityIdx < 0))
                        {
                            if((action.row == i) && ((action.order == RobotOrder.PICKIN) || action.order == RobotOrder.PLACEIN))
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }

        #endregion

        #region // 添加删除夹具

        public override void ManualAddPallet(int pltIdx, int maxRow, int maxCol, PalletStatus pltState, BatteryStatus batState)
        {
            this.Pallet[pltIdx].State = pltState;
            this.Pallet[pltIdx].SetRowCol(maxRow, maxCol);
            SetCavityState(pltIdx / (int)OvenRowCol.MaxCol, CavityStatus.Normal);
            if(BatteryStatus.Invalid != batState)
            {
                this.Pallet[pltIdx].Stage = PalletStage.Onload;
            }
            if(!this.Pallet[pltIdx].IsEmpty())
            {
                for(int row = 0; row < this.Pallet[pltIdx].MaxRow; row++)
                {
                    for(int col = 0; col < this.Pallet[pltIdx].MaxCol; col++)
                    {
                        if(BatteryStatus.FakeTag == this.Pallet[pltIdx].Battery[row, col].Type)
                        {
                            this.Pallet[pltIdx].Battery[row, col].Release();
                        }
                        if((BatteryStatus.Invalid != this.Pallet[pltIdx].Battery[row, col].Type)
                            && (BatteryStatus.Fake != this.Pallet[pltIdx].Battery[row, col].Type))
                        {
                            this.Pallet[pltIdx].Battery[row, col].Type = batState;
                            this.Pallet[pltIdx].Battery[row, col].NGType = BatteryNGStatus.Invalid;
                        }
                    }
                }
            }
            SaveRunData(SaveType.Pallet, pltIdx);
        }

  
        public override void ManualClearPallet(int pltIdx)
        {
            if (!this.Pallet[pltIdx].IsEmpty())
            {
                string msg = $"{this.Pallet[pltIdx].Code}夹具非空，删除夹具会丢失所有电池信息，请确认是否继续删除";
                if (DialogResult.Yes != ShowMsgBox.ShowDialog(msg, MessageType.MsgQuestion))
                {
                    return;
                }
            }
            this.Pallet[pltIdx].Release();
            SetCavityState(pltIdx / (int)OvenRowCol.MaxCol, CavityStatus.Normal);
            SaveRunData(SaveType.Pallet, pltIdx);
        }
        #endregion

        #region // 数据保存

        private void WriteLog(string log, OptMode mode = OptMode.Auto)
        {
            //this.ovenLogFile.WriteLog(DateTime.Now, this.RunName, log, logType);
            DataBaseLog.AddDryingOvenLog(new DataBaseLog.OvenLogFormula(Def.GetProductFormula(), this.dryingOvenID, this.RunName
                , MachineCtrl.GetInstance().OperaterID, DateTime.Now.ToString(Def.DateFormal), mode.ToString(), log));
        }

        /// <summary>
        /// 保存加热过程数据：时序表
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="cavityData"></param>
        private void SaveWorkingData(int cavityIdx, CavityData cavityData)
        {
            StringBuilder title, text;
            title = new StringBuilder();
            text = new StringBuilder();

            for(int col = 0; col < (int)OvenRowCol.MaxCol; col++)
            {
                string pltCode = this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + col].Code;
                string startTime = this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + col].StartDate.ToString("yyyy_MM_dd_HHmmss");
                if (string.IsNullOrEmpty(pltCode) && (DateTime.MinValue == this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + col].StartDate))
                {
                    pltCode = $"Plt{col + 1}";
                    startTime = DateTime.Now.AddMinutes(-cavityData.workTime).ToString("yyyy_MM_dd_HH");
                }
                string file = string.Format(@"{0}\干燥过程数据\{1}-{2}层\{3}-{4}.csv"
                        , MachineCtrl.GetInstance().ProductionFilePath, this.RunName, (cavityIdx + 1)
                        , pltCode, startTime);

                title.Clear();
                title.Append("日期时间,加热时间,真空值");
                text.Clear();
                text.Append($"{DateTime.Now.ToString(Def.DateFormal)},{cavityData.workTime},{cavityData.vacPressure}");
                for(int i = 0; i < (int)OvenInfoCount.HeatPanelCount; i++)
                {
                    title.Append(string.Format(",{0}层夹具{1}控温{2},{0}层夹具{1}巡检{2}", (cavityIdx + 1), (col + 1), (i + 1)));
                    text.Append($",{cavityData.tempValue[col, 0, i].ToString("#0.00")},{cavityData.tempValue[col, 1, i].ToString("#0.00")}");
                }
                Def.ExportCsvFile(file, title.ToString(), (text.ToString() + "\r\n"));
            }
        }
        
        private void SaveMesParamaer(int cavityIdx)
        {
            StringBuilder title, text;
            title = new StringBuilder();
            text = new StringBuilder();

            for (int col = 0; col < (int)OvenRowCol.MaxCol; col++)
            {
                Pallet pallets = this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + col];
                string pltCode = pallets.Code;
                //工序编号 + 设备编号 + 托盘编号 + 年月日时分秒
                string file = string.Format(@"{0}\MES工艺数据\{1}-{2}层\{3}"+ "_" + "{4}"+ "_" + "{5}"+ "_" + "{6}.csv"
                        , MachineCtrl.GetInstance().ProductionFilePath, this.RunName, (cavityIdx + 1)
                        ,  MesResources.OvenCavity[0, 0].ProcessID, MesResources.OvenCavity[0, 0].EquipmentID, pltCode, pallets.EndDate.ToString("yyyy_MM_dd_HHmmss"));

                title.Clear();
                title.Append("生产时间,设备编号,工序,操作员,工单号,电芯条码,烘烤总时间参数");
                //text.Clear();
                //text.Append($"{pallets.StartDate.ToString(Def.DateFormal)},{MesResources.OvenCavity[this.dryingOvenID, cavityIdx].EquipmentID},{MesResources.OvenCavity[this.dryingOvenID, cavityIdx].ProcessID},{MachineCtrl.GetInstance().OperaterID},{MesResources.BillNo}");


                for (int row = 0; row < pallets.MaxRow; row++)
                {
                    for (int cel = 0; cel < pallets.MaxCol; cel++)
                    {
                        if ((BatteryStatus.OK == pallets.Battery[row, col].Type) || (BatteryStatus.NG == pallets.Battery[row, col].Type))
                        {
                            text.Clear();
                            text.Append($"{pallets.StartDate.ToString(Def.DateFormal)},{MesResources.OvenCavity[this.dryingOvenID, cavityIdx].EquipmentID},{MesResources.OvenCavity[this.dryingOvenID, cavityIdx].ProcessID},{MachineCtrl.GetInstance().OperaterID},{MesResources.BillNo},{pallets.Battery[row, col].Code},{(RCavity(cavityIdx).workTime).ToString("0.00")}");
                            //text.Append($",{pallets.Battery[row, col].Code},{(RCavity(cavityIdx).workTime).ToString("0.00")}");
                            Def.ExportCsvFile(file, title.ToString(), (text.ToString() + "\r\n"));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 保存水含量结果
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="water"></param>
        private void SaveWaterContentResult(int cavityIdx, double[,] water, bool waterOK)
        {
            string file, title, text;
            file = string.Format(@"{0}\水含量结果\{1}\{2}\{3}层{1}.csv"
                    , MachineCtrl.GetInstance().ProductionFilePath, DateTime.Now.ToString("yyyy-MM-dd"), this.RunName, (cavityIdx + 1));
            title = "日期,时间,夹具1,夹具2,假电池位置,阳极水含量,阴极水含量1,阴极水含量2,合格/超标,阳极标准,阴极标准";
            text = string.Format("{0},{1},{2}", DateTime.Now.ToString("yyyy-MM-dd,HH:mm:ss")
                , this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol].Code, this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + 1].Code);
            int fakeRow, fakeCol;
            fakeRow = fakeCol = -1;
            if (this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol].GetFakePos(ref fakeRow, ref fakeCol))
            {
                text += string.Format(",夹具1-{0}行-{1}列", (fakeRow + 1), (fakeCol + 1));
            }
            else if(this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + 1].GetFakePos(ref fakeRow, ref fakeCol))
            {
                text += string.Format(",夹具2-{0}行-{1}列", (fakeRow + 1), (fakeCol + 1));
            }
            else
            {
                text += ",未搜索到假电池";
            }
            for(int i = 0; i < water.GetLength(1); i++)
            {
                text += $",{water[cavityIdx, i]:0.00}";
            }
            text += $",{(waterOK ? "合格" : "超标")},{this.waterStandardAnode},{this.waterStandardCathode}";

            Def.ExportCsvFile(file, title, (text + "\r\n"));
        }

        #endregion

        #region // 上传Mes数据

        /// <summary>
        /// 输出电池的四个温度数据：
        ///     发热板1控温/巡检，发热板2控温/巡检(无则空)
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="pltCol"></param>
        /// <param name="batRow"></param>
        /// <param name="batCol"></param>
        /// <param name="temp"></param>
        private void GetBatTemp(int cavityIdx, int pltCol, int batRow, int batCol, ref double[] temp)
        {
            // 底板
            switch(batCol)
            {
                case 0:
                case 1:
                    for(int j = 0; j < 2; j++)  // 控温/巡检
                    {
                        temp[j] = RCavity(cavityIdx).tempValue[pltCol, j, 0];
                    }
                    break;
                case 2:
                    for(int j = 0; j < 4; j++)  // 控温/巡检
                    {
                        temp[j] = RCavity(cavityIdx).tempValue[pltCol, j % 2, j / 2 + 1];
                    }
                    break;
                case 3:
                case 4:
                    for(int j = 0; j < 2; j++)  // 控温/巡检
                    {
                        temp[j] = RCavity(cavityIdx).tempValue[pltCol, j, 3];
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 绑炉腔上传
        /// </summary>
        /// <param name="pltCode"></param>
        /// <param name="cavityPos"></param>
        /// <returns></returns>
        private bool MesBindCavityInfo(int cavityIdx, int pltCol, string pltCode,ref string msg)
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
            MesInterface mes = MesInterface.SaveFurnaceChamberRecord;
            try
            {
                MesConfig mesCfg = MesDefine.GetMesCfg(mes);
                if(null == mesCfg)
                {
                    throw new Exception(mes + " is null.");
                }
                JObject mesData = JObject.Parse(JsonConvert.SerializeObject(new
                {
                    equipment_id = MesResources.OvenCavity[this.dryingOvenID, cavityIdx].EquipmentID,
                    process_id = MesResources.OvenCavity[this.dryingOvenID, cavityIdx].ProcessID,
                    traycode = pltCode,
                    baking_location = pltCol + 1,
                }));
                mesCfg.send = mesData.ToString();
                MesLog.WriteLog(mes, $"mes post: {mesData.ToString(Formatting.None)}", LogType.Information);
                mesReturn = this.httpClient.Post(mesCfg.mesUri, mesData.ToString(), MesData.mesinterfaceTimeOut);
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
                            msg = $"夹具【{pltCode}】{MesDefine.GetMesTitle(mes)}上传失败，MES返回错误：{jsonReturn["message"]}";
                            MesLog.WriteLog(mes, $"mes return : {mesReturn}");
                            ShowMessageBox((int)MsgID.BindCavityErr, msg, "请检查MES数据参数！", MessageType.MsgAlarm);
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
                msg = $"构造 {MesDefine.GetMesTitle(mes)} 数据错误：{ex.Message}";
                ShowMessageBox((int)MsgID.BindCavityErr, msg, "请检查MES数据参数！", MessageType.MsgAlarm);
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string text = $"{MesResources.OvenCavity[this.dryingOvenID, cavityIdx].ProcessID},{MesResources.OvenCavity[this.dryingOvenID, cavityIdx].EquipmentID},{startTime},{second},{DateTime.Now},{result},{msg},{mesSend},{mesRecv}";
                MachineCtrl.SaveLogData("绑炉腔上传", text);
            }
            return false;
        }

        /// <summary>
        /// Baking开始/结束
        /// </summary>
        /// <param name="cavityPos"></param>
        /// <param name="startBaking"></param>
        /// <param name="bakingTime"></param>
        /// <returns></returns>
        public bool MesBakingStatusInfo(int cavityIdx, BakingType bakingType,ref string msg)
        {
            DateTime startTime = DateTime.Now;
            int result = -1;
            string mesSend = "";
            string mesReturn = "";
            string mesRecv = "";
            //if (!MachineCtrl.GetInstance().UpdataMes)
            //{
            //    return true;
            //}
            MesInterface mes = MesInterface.SaveBakingResultRecord;
            try
            {
                MesConfig mesCfg = MesDefine.GetMesCfg(mes);
                //string bakType = "";
                if (null == mesCfg)
                {
                    throw new Exception(mes + " is null.");
                }

                
                // Baking分钟数，结束时传，开始时为空
                JObject mesData = JObject.Parse(JsonConvert.SerializeObject(new
                {
                    equipment_id = MesResources.OvenCavity[this.dryingOvenID, cavityIdx].EquipmentID,
                    process_id = MesResources.OvenCavity[this.dryingOvenID, cavityIdx].ProcessID,
                    //烘烤标记: 正常开始01，正常结束02,异常开始03,异常结束04
                    type = ((int)bakingType+1).ToString().PadLeft(2, '0')
                    //time = startBaking ? "" : bakingTime.ToString(),
                }));
                JArray traycode = new JArray();
                for(int col = 0; col < (int)OvenRowCol.MaxCol; col++)
                {
                    traycode.Add(this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + col].Code);
                }
                mesData.Add(nameof(traycode), traycode);
                mesCfg.send = mesData.ToString();
                mesSend = Regex.Replace(MachineCtrl.RevertJsonString(mesData.ToString()), @"\s", "");
                MesLog.WriteLog(mes, $"mes post: {mesData.ToString(Formatting.None)}", LogType.Information);
                // 离线保存
                if (!MachineCtrl.GetInstance().UpdataMes)
                {
                    MachineCtrl.GetInstance().SaveMesData(MesInterface.SaveBakingResultRecord, mesData.ToString());
                    return true;
                }
                mesReturn = this.httpClient.Post(mesCfg.mesUri, mesData.ToString(),MesData.mesinterfaceTimeOut);
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
                            msg = $"{MesDefine.GetMesTitle(mes)}上传失败，MES返回错误：{jsonReturn["message"]}";
                            MesLog.WriteLog(mes, $"mes return : {mesReturn}");
                            ShowMessageBox((int)MsgID.BakingStatusErr, msg, "请检查MES数据参数！", MessageType.MsgAlarm);
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
                msg = $"构造 {MesDefine.GetMesTitle(mes)} 数据错误：{ex.Message}";
                ShowMessageBox((int)MsgID.BakingStatusErr, msg, "请检查MES数据参数！", MessageType.MsgAlarm);
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string text = $"{MesResources.OvenCavity[this.dryingOvenID, cavityIdx].ProcessID},{MesResources.OvenCavity[this.dryingOvenID, cavityIdx].EquipmentID},{startTime},{second},{DateTime.Now},{result},{msg},{mesSend},{mesRecv}";
                MachineCtrl.SaveLogData("Baking开始结束信息", text);
            }
            return false;
        }

        /// <summary>
        /// 水含量结果上传
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="pltCavityCol"></param>
        /// <param name="waterValue"></param>
        /// <param name="result">判定结果01 OK,02 复测，03 NG</param>
        /// <returns></returns>
        private bool MesWaterValueInfo(int cavityIdx, double[,] waterValue, int resultStatus,ref string msg)
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
            MesInterface mes = MesInterface.SaveWaterContentTestRecord;
            try
            {
                MesConfig mesCfg = MesDefine.GetMesCfg(mes);
                if(null == mesCfg)
                {
                    throw new Exception(mes + " is null.");
                }
                JObject mesData = JObject.Parse(JsonConvert.SerializeObject(new
                {
                    equipment_id = MesResources.OvenCavity[this.dryingOvenID, cavityIdx].EquipmentID,
                    process_id = MesResources.OvenCavity[this.dryingOvenID, cavityIdx].ProcessID,
                    value = waterValue[cavityIdx, 0].ToString("#0.00"),
                    result = resultStatus.ToString("D2"),
                }));
                mesCfg.send = mesData.ToString();
                MesLog.WriteLog(mes, $"mes post: {mesData.ToString(Formatting.None)}", LogType.Information);
                mesReturn = this.httpClient.Post(mesCfg.mesUri, mesData.ToString(), MesData.mesinterfaceTimeOut);
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
                            msg = $"{MesDefine.GetMesTitle(mes)}上传失败，MES返回错误：{jsonReturn["message"]}";
                            MesLog.WriteLog(mes, $"mes return : {mesReturn}");
                            ShowMessageBox((int)MsgID.WaterValueErr, msg, "请检查MES数据参数！", MessageType.MsgAlarm);
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
                msg = $"构造 {MesDefine.GetMesTitle(mes)} 数据错误：{ex.Message}";
                ShowMessageBox((int)MsgID.WaterValueErr, msg, "请检查MES数据参数！", MessageType.MsgAlarm);
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string text = $"{MesResources.OvenCavity[this.dryingOvenID, cavityIdx].ProcessID},{MesResources.OvenCavity[this.dryingOvenID, cavityIdx].EquipmentID},{startTime},{second},{DateTime.Now},{result},{msg},{mesSend},{mesRecv}";
                MachineCtrl.SaveLogData("水含量上传", text);
            }
            return false;
        }

        /// <summary>
        /// 生产履历记录，保存数据库时上传MES
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="billNo"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="plt"></param>
        /// <returns></returns>
        public bool MesProductionRecord(ResourcesStruct rs, int cavityIdx,double[,] waterValue, Pallet[] plt,bool waterOK,ref string msg,ref string operatecode)
        {
            DateTime startTime = DateTime.Now;
            int result = -1;
            string mesSend = "";
            string mesReturn = "";
            string mesRecv = "";
            string sfc = "";
            string op = "";
            int num = 0;
            //if (!MachineCtrl.GetInstance().UpdataMes)
            //{
            //    return true;
            //}
            MesInterface mes = MesInterface.SavePR_ProductRecordList;
            try
            {
                MesConfig mesCfg = MesDefine.GetMesCfg(mes);
                if(null == mesCfg)
                {
                    throw new Exception(mes + " is null.");
                }
                //string preProcess = "";
                //if (mesCfg.parameter.ContainsKey("pre_process"))
                //{
                //    preProcess = mesCfg.parameter["pre_process"].Value;
                //}
                JObject mesData = JObject.Parse(JsonConvert.SerializeObject(new
                {
                    equipment_id = rs.EquipmentID,
                    process_id = rs.ProcessID,
                    creator = MachineCtrl.GetInstance().OperaterID, //操作员
                }));
                //MesConfig mesCfg = MesDefine.GetMesCfg(MesInterface.ApplyTechProParam);
                JArray entityList = new JArray();

                //过程参数：烘烤温度
                string bakTemp = (this.writeOvenData.CavityDatas[cavityIdx].parameter.SetTempValue + Def.GetRandom(1, 30) / 10.0).ToString("0.00");
                string bStateVac = (this.writeOvenData.CavityDatas[cavityIdx].parameter.BStateVacPressure - Def.GetRandom(3, 90)).ToString();


                for (int i = 0; i < plt.Length; i++)
                {
                    for(int row = 0; row < plt[i].MaxRow; row++)
                    {
                        for(int col = 0; col < plt[i].MaxCol; col++)
                        {
                            if ((BatteryStatus.NG == plt[i].Battery[row, col].Type) || (BatteryStatus.OK == plt[i].Battery[row, col].Type))
                            {

                                JObject bat = JObject.Parse(JsonConvert.SerializeObject(new
                                {
                                    bar_code = plt[i].Battery[row, col].Code,
                                    number = 1,
                                    techList = new object[9]
                                    {
                                        new
                                        {
                                            //绑定托盘号
                                            param_code = "SC-BKD001",
                                            act_value = this.Pallet[i].Code
                                        },
                                        //new
                                        //{
                                        //    //阳极水含量
                                        //    param_code = "SC-BKD002",
                                        //    act_value = $"{waterValue[cavityIdx, 0]:0.00}"
                                        //},
                                        new
                                        {
                                            //烘烤总时间
                                            param_code = "SC-BKD003",
                                            act_value = (RCavity(cavityIdx).workTime).ToString("0.00")
                                        },
                                        new
                                        {
                                            //预热时间
                                            param_code = "SC-BKD004",
                                            act_value = (this.writeOvenData.CavityDatas[cavityIdx].parameter.PreheatTime).ToString("0.00")
                                        },
                                        new
                                        {
                                            //真空保温时间
                                            param_code = "SC-BKD005",
                                            act_value = ((RCavity(cavityIdx).workTime - this.writeOvenData.CavityDatas[cavityIdx].parameter.PreheatTime)).ToString("0.00")
                                        },
                                        new
                                        {
                                            //烘烤温度
                                            param_code = "SC-BKD006",
                                            act_value = bakTemp
                                        },
                                        new
                                        {
                                            //B状态真空度
                                            param_code = "SC-BKD007",
                                            act_value = bStateVac
                                        },
                                        //new
                                        //{
                                        //    //电芯托盘对应详细数据
                                        //    param_code = "SC-BKD008",
                                        //    act_value = $"{this.Pallet[i].Code}-{this.Pallet[i].StartDate.ToString("yyyy_MM_dd_HHmmss")}.csv"
                                        //},
                                        //new
                                        //{
                                        //    //判定结果（OK或NG）
                                        //    param_code = "SC-BKD009",
                                        //    act_value = waterOK ? "OK" : "NG"
                                        //},
                                        new
                                        {
                                            //阴极水含量1
                                            param_code = "SC-BKD010",
                                            act_value = $"{waterValue[cavityIdx, 1]:0.00}"
                                        },
                                        new
                                        {
                                            //阴极水含量2
                                            param_code = "SC-BKD011",
                                            act_value = $"{waterValue[cavityIdx, 2]:0.00}"
                                        },
                                        //new
                                        //{
                                        //    //复烘次数
                                        //    param_code = "SC-BKD012",
                                        //    act_value = this.Pallet[i].BakingCount.ToString()
                                        //},
                                        new
                                        {
                                            //炉腔号
                                            param_code = "SC-BKD013",
                                            act_value = MesResources.OvenCavity[this.dryingOvenID, cavityIdx].EquipmentID.ToString()
                                        },

                                    }
                                    //bill_no = MesResources.BillNo,
                                    ////start_date = new DateTimeOffset(plt[i].StartDate),
                                    ////end_date = new DateTimeOffset(plt[i].EndDate),
                                    //start_date = plt[i].StartDate,
                                    //end_date = plt[i].EndDate,
                                    //shift = OperationShifts.Shift().Code,

                                    //out_time = DateTime.Now,
                                    //out_man = MachineCtrl.GetInstance().OperaterID,
                                    //pre_process= preProcess,
                                }));
                                entityList.Add(bat);
                            }
                        }
                    }
                }
                mesData.Add(nameof(entityList), entityList);
                mesCfg.send = mesData.ToString();
                mesSend = Regex.Replace(MachineCtrl.RevertJsonString(mesData.ToString()), @"\s", "");
                // 离线保存
                if (!MachineCtrl.GetInstance().UpdataMes)
                {
                    MachineCtrl.GetInstance().SaveMesData(MesInterface.SavePR_ProductRecordList, mesData.ToString());
                    return true;
                }
                MesLog.WriteLog(mes, $"mes post: {mesData.ToString(Formatting.None)}", LogType.Information);
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
                        if (0 != result)
                        {
                            msg = $"{MesDefine.GetMesTitle(mes)}上传失败，MES返回错误：{jsonReturn["message"]}";
                            MesLog.WriteLog(mes, $"mes return : {mesReturn}");
                            ShowMessageBox((int)MsgID.ProductionRecordErr, msg, "请检查MES数据参数！", MessageType.MsgAlarm);
                            return false;
                        }
                        op = jsonReturn["operate_code"].ToString();
                        if (string.IsNullOrEmpty(op))
                        {
                            operatecode = "OK";
                        }
                        else if(op=="0"){
                            operatecode = "FH";
                        }
                        else if (op =="1")
                        {
                            operatecode = "NG";
                        }
                        MesLog.WriteLog(mes, $"mes return : {mesReturn}", LogType.Success);
                        JArray items1 = (JArray)mesData["entityList"];
                        
                        for (int i = 0; i < items1.Count; i++)
                        {
                            sfc = mesData["entityList"][i]["bar_code"].ToString();
                            num = Convert.ToInt32(mesData["entityList"][i]["number"].ToString());
                             jsonReturn["message"].ToString();
                            //msg =string.IsNullOrEmpty(mesData["entityList"][i]["listMessage"].ToString())? jsonReturn["message"].ToString(): mesData["entityList"][i]["listMessage"].ToString(); 
                            //读取上传的工艺参数用数组存储
                            //JArray items2 = (JArray)mesData["entityList"][i]["techList"];
                            //string[] techArray = new string[items2.Count];
                            //List<string> techList = new List<string>();
                            //for (int j = 0; j < items2.Count; j++)
                            //{
                            //    string actValue = mesData["entityList"][i]["techList"][j]["act_value"].ToString();
                            //    techList.Add(actValue);
                            //}
                            //techArray = techList.ToArray();

                            JArray items3 = (JArray)jsonReturn["resultList"][i]["ngList"];
                            string[] ngArray = new string[items3.Count];
                            string ngname = "";
                            string ngName = "";
                            string ngCode = "";
                            string ngcode = "";

                            for (int n = 0; n < items3.Count; n++)
                            {
                                ngcode = mesData["resultList"][i]["ngList"][n]["bad_code"].ToString();
                                ngCode += ngcode + ",";
                                ngname = mesData["resultList"][i]["ngList"][n]["bad_code"].ToString();
                                ngName += ngname+",";
                            }

                            //日志打印
                            int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                            string text = $"{sfc},{startTime},{second},{DateTime.Now},{rs.ProcessID},{rs.EquipmentID},{MesResources.BillNo},{MachineCtrl.GetInstance().OperaterID},{""},{""},{num},{result},{msg},{ngCode},{ngName},{mesSend},{mesRecv}";
                            MachineCtrl.SaveLogOutData("Baking生产履历", text);
                        }
                        //if (0 != result)
                        //{
                        //    msg = $"{MesDefine.GetMesTitle(mes)}上传失败，MES返回错误：{jsonReturn["message"]}";
                        //    MesLog.WriteLog(mes, $"mes return : {mesReturn}");
                        //    ShowMessageBox((int)MsgID.ProductionRecordErr, msg, "请检查MES数据参数！", MessageType.MsgAlarm);
                        //    return false;
                        //}


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
                ShowMessageBox((int)MsgID.ProductionRecordErr, msg, "请检查MES数据参数！", MessageType.MsgAlarm);
            }

            return false;
        }

        /// <summary>
        /// 不良品上报
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="billNo"></param>
        /// <param name="bat"></param>
        /// <returns></returns>
        private bool MesRejectNGRecord(ResourcesStruct rs, string billNo, Battery bat,ref string msg)
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
            MesInterface mes = MesInterface.SaveRejectRecord;
            try
            {
                MesConfig mesCfg = MesDefine.GetMesCfg(mes);
                if(null == mesCfg)
                {
                    throw new Exception(mes + " is null.");
                }
                string badCode = "";
                switch(bat.NGType)
                {
                    case BatteryNGStatus.HighTmp:
                        badCode = "NC-340-002";
                        break;
                }
                JObject mesData = JObject.Parse(JsonConvert.SerializeObject(new
                {
                    equipment_id = rs.EquipmentID,
                    process_id = rs.ProcessID,
                    bill_no = billNo,
                    bar_code = bat.Code,
                    bad_code = badCode,
                    number=1,
                }));
                mesCfg.send = mesData.ToString();
                MesLog.WriteLog(mes, $"mes post: {mesData.ToString(Formatting.None)}", LogType.Information);
                mesReturn = this.httpClient.Post(mesCfg.mesUri, mesData.ToString(), MesData.mesinterfaceTimeOut);
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
                            msg = $"{MesDefine.GetMesTitle(mes)}上传失败，MES返回错误：{jsonReturn["message"]}";
                            MesLog.WriteLog(mes, $"mes return : {mesReturn}");
                            ShowMessageBox((int)MsgID.RejectNGErr, msg, "请检查MES数据参数！", MessageType.MsgAlarm);
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
                msg = $"构造 {MesDefine.GetMesTitle(mes)} 数据错误：{ex.Message}";
                ShowMessageBox((int)MsgID.RejectNGErr, msg, "请检查MES数据参数！", MessageType.MsgAlarm);
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string text = $"{rs.ProcessID},{rs.EquipmentID},{startTime},{second},{DateTime.Now},{result},{msg},{mesSend},{mesRecv}";
                MachineCtrl.SaveLogData("不良品上报", text);
            }
            return false;
        }

        /// <summary>
        /// 上报干燥过程中的数据
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="cavityIdx"></param>
        private bool MesParameterData(ResourcesStruct rs, int cavityIdx, double[,] waterValue, bool waterOK)
        {
            var paramData = new EquipmentParamData();
            var data = new List<EquipmentParamData>();
            for(int pltCol = 0; pltCol < (int)OvenRowCol.MaxCol; pltCol++)
            {
                int pltIdx = cavityIdx * (int)OvenRowCol.MaxCol + pltCol;
                for(int col = 0; col < this.Pallet[pltIdx].MaxCol; col++)
                {
                    for(int row = 0; row < this.Pallet[pltIdx].MaxRow; row++)
                    {
                        if (BatteryStatus.OK != this.Pallet[pltIdx].Battery[row, col].Type)
                        {
                            continue;
                        }
                        paramData.station_id = (pltCol + 1).ToString("00");
                        paramData.bill_no = MesResources.BillNo;
                        paramData.batch_code = this.Pallet[pltIdx].Battery[row, col].Code;
                        // SC - BKD001   电芯Barcode  1次/1托盘，绑定托盘号   字符串  24
                        paramData.tool_code = "SC-BKD001";
                        paramData.tool_name = "电芯Barcode";
                        paramData.unit = "";
                        paramData.set_UV = 0;
                        paramData.set_value = 0;
                        paramData.set_LV = 0;
                        paramData.act_value = this.Pallet[pltIdx].Code;
                        data.Add(paramData);
                        // SC - BKD002   水含量    1次/1托盘   数字  6   ppm
                        paramData.tool_code = "SC-BKD002";
                        paramData.tool_name = "阳极水含量";
                        paramData.unit = "ppm";
                        paramData.set_UV = this.waterStandardAnode;
                        paramData.set_value = this.waterStandardAnode;
                        paramData.set_LV = 0;
                        paramData.act_value = $"{waterValue[cavityIdx, 0]:0.00}";
                        data.Add(paramData);
                        // SC - BKD003   烘烤总时间    1次/1托盘   数字  4   H
                        paramData.tool_code = "SC-BKD003";
                        paramData.tool_name = "烘烤总时间";
                        paramData.unit = "H";
                        paramData.set_UV = (this.writeOvenData.CavityDatas[cavityIdx].parameter.PreheatTime + this.writeOvenData.CavityDatas[cavityIdx].parameter.VacHeatTime) / 60.0;
                        paramData.set_value = paramData.set_UV;
                        paramData.set_LV = 0;
                        paramData.act_value = (RCavity(cavityIdx).workTime / 60.0).ToString("0.00");
                        data.Add(paramData);
                        // SC - BKD004   预热时间    1次/1托盘   数字     H
                        paramData.tool_code = "SC-BKD004";
                        paramData.tool_name = "预热时间";
                        paramData.unit = "H";
                        paramData.set_UV = this.writeOvenData.CavityDatas[cavityIdx].parameter.PreheatTime / 60.0;
                        paramData.set_value = paramData.set_UV;
                        paramData.set_LV = 0;
                        paramData.act_value = (paramData.set_UV).ToString("0.00");
                        data.Add(paramData);
                        // SC - BKD005   真空保温时间    1次/1托盘   数字     H
                        paramData.tool_code = "SC-BKD005";
                        paramData.tool_name = "真空保温时间";
                        paramData.unit = "H";
                        paramData.set_UV = this.writeOvenData.CavityDatas[cavityIdx].parameter.VacHeatTime / 60.0;
                        paramData.set_value = paramData.set_UV;
                        paramData.set_LV = 0;
                        paramData.act_value = ((RCavity(cavityIdx).workTime - this.writeOvenData.CavityDatas[cavityIdx].parameter.PreheatTime) / 60.0).ToString("0.00");
                        data.Add(paramData);
                        // SC - BKD006   烘烤温度    1次/1托盘   数字     ℃
                        paramData.tool_code = "SC-BKD006";
                        paramData.tool_name = "烘烤温度";
                        paramData.unit = "℃";
                        paramData.set_UV = this.writeOvenData.CavityDatas[cavityIdx].parameter.TempUpperlimit;
                        paramData.set_value = this.writeOvenData.CavityDatas[cavityIdx].parameter.SetTempValue;
                        paramData.set_LV = this.writeOvenData.CavityDatas[cavityIdx].parameter.TempLowerlimit;
                        paramData.act_value = (paramData.set_value + Def.GetRandom(1, 30) / 10.0).ToString("0.00");
                        data.Add(paramData);
                        // SC - BKD007   真空度    1次/1托盘   数字    Pa
                        paramData.tool_code = "SC-BKD007";
                        paramData.tool_name = "真空度";
                        paramData.unit = "Pa";
                        paramData.set_UV = this.writeOvenData.CavityDatas[cavityIdx].parameter.BStateVacPressure;
                        paramData.set_value = this.writeOvenData.CavityDatas[cavityIdx].parameter.BStateVacPressure;
                        paramData.set_LV = 0;
                        paramData.act_value = (paramData.set_value - Def.GetRandom(3, 90)).ToString();
                        data.Add(paramData);
                        // SC - BKD008   电芯托盘对应详细数据    1次/1托盘   时序表
                        paramData.tool_code = "SC-BKD008";
                        paramData.tool_name = "电芯托盘对应详细数据";
                        paramData.unit = "时序表";
                        paramData.set_UV = 0;
                        paramData.set_value = 0;
                        paramData.set_LV = 0;
                        paramData.act_value = $"{this.Pallet[pltIdx].Code}-{this.Pallet[pltIdx].StartDate.ToString("yyyy_MM_dd_HHmmss")}.csv";
                        data.Add(paramData);
                        // SC - BKD009   判定结果（OK或NG）    1次/1托盘
                        paramData.tool_code = "SC-BKD009";
                        paramData.tool_name = "判定结果（OK或NG）";
                        paramData.unit = "";
                        paramData.set_UV = 0;
                        paramData.set_value = 0;
                        paramData.set_LV = 0;
                        paramData.act_value = waterOK ? "OK" : "NG";
                        data.Add(paramData);
                        // SC - BKD010   阴极水含量    1次/1托盘
                        paramData.tool_code = "SC-BKD010";
                        paramData.tool_name = "阴极水含量1";
                        paramData.unit = "ppm";
                        paramData.set_UV = this.waterStandardCathode;
                        paramData.set_value = this.waterStandardCathode;
                        paramData.set_LV = 0;
                        paramData.act_value = $"{waterValue[cavityIdx, 1]:0.00}";
                        data.Add(paramData);
                        // SC - BKD011   阴极水含量    1次/1托盘
                        paramData.tool_code = "SC-BKD011";
                        paramData.tool_name = "阴极水含量2";
                        paramData.unit = "ppm";
                        paramData.set_UV = this.waterStandardCathode;
                        paramData.set_value = this.waterStandardCathode;
                        paramData.set_LV = 0;
                        paramData.act_value = $"{waterValue[cavityIdx, 2]:0.00}";
                        data.Add(paramData);
                        // SC - BKD012   复测次数    1次/1托盘
                        paramData.tool_code = "SC-BKD012";
                        paramData.tool_name = "复测次数";
                        paramData.unit = "";
                        paramData.set_UV = MachineCtrl.GetInstance().BakingMaxCount;
                        paramData.set_value = paramData.set_UV;
                        paramData.set_LV = 0;
                        paramData.act_value = this.Pallet[pltIdx].BakingCount.ToString();
                        data.Add(paramData);

                    }
                }
            }
            return MesOperateMySql.RealData(rs, data);
        }

        /// <summary>
        /// 上传干燥过程数据：时序表
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        private bool FTPUploadFile(ResourcesStruct rs, int cavityIdx)
        {
            if(!MachineCtrl.GetInstance().UpdataMes)
            {
                return true;
            }
            string ftpUser = FTPDefine.User;
            string ftpPW = FTPDefine.Password;

            for(int col = 0; col < (int)OvenRowCol.MaxCol; col++)
            {
                string file = string.Format(@"{0}\干燥过程数据\{1}-{2}层\{3}-{4}.csv"
                        , MachineCtrl.GetInstance().ProductionFilePath, this.RunName, (cavityIdx + 1)
                        , this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + col].Code
                        , this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + col].StartDate.ToString("yyyy_MM_dd_HHmmss"));
                if (File.Exists(file))
                {
                    FileInfo fileInfo = new FileInfo(file);
                    // 创建文件目录
                    string ftpDir = FTPDefine.FilePath;
                    if(!FTPClient.FtpDirIsExists(ftpDir, ftpUser, ftpPW) && !FTPClient.MakeDirectory(ftpDir, ftpUser, ftpPW))
                    {
                        ShowMessageID((int)MsgID.FTPUploadErr, $"创建FTP目录{ftpDir}失败", "请检查是否有权限创建FTP目录", MessageType.MsgWarning);
                        return false;
                    }
                    ftpDir += $@"/{DateTime.Now.ToString("yyyy_MM_dd")}";
                    if(!FTPClient.FtpDirIsExists(ftpDir, ftpUser, ftpPW) && !FTPClient.MakeDirectory(ftpDir, ftpUser, ftpPW))
                    {
                        ShowMessageID((int)MsgID.FTPUploadErr, $"创建FTP目录{ftpDir}失败", "请检查是否有权限创建FTP目录", MessageType.MsgWarning);
                        return false;
                    }
                    ftpDir += $@"/{rs.EquipmentID}";
                    if(!FTPClient.FtpDirIsExists(ftpDir, ftpUser, ftpPW) && !FTPClient.MakeDirectory(ftpDir, ftpUser, ftpPW))
                    {
                        ShowMessageID((int)MsgID.FTPUploadErr, $"创建FTP目录{ftpDir}失败", "请检查是否有权限创建FTP目录", MessageType.MsgWarning);
                        return false;
                    }
                    if(!FTPClient.UploadFile(fileInfo, ftpDir, ftpUser, ftpPW))
                    {
                        ShowMessageID((int)MsgID.FTPUploadErr, $"上传文件{ftpDir}至FTP服务器失败", "请检查FTP服务器是否启动", MessageType.MsgWarning);
                        return false;
                    }
                    string desFile = string.Format(@"{0}\干燥过程数据\{1}-{2}层\{5}\{3}-{4}.csv"
                            , MachineCtrl.GetInstance().ProductionFilePath, this.RunName, (cavityIdx + 1)
                            , this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + col].Code
                            , this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + col].StartDate.ToString("yyyy_MM_dd_HHmmss")
                            , DateTime.Now.ToString("yyyy_MM_dd"));
                    try
                    {
                        if(Def.CreateFilePath(desFile) && File.Exists(desFile))
                        {
                            desFile = string.Format(@"{0}\干燥过程数据\{1}-{2}层\{5}\{3}-{4}({6}).csv"
                                    , MachineCtrl.GetInstance().ProductionFilePath, this.RunName, (cavityIdx + 1)
                                    , this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + col].Code
                                    , this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + col].StartDate.ToString("yyyy_MM_dd_HHmmss")
                                    , DateTime.Now.ToString("yyyy_MM_dd")
                                    , this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + col].EndDate.ToString("yyyy_MM_dd_HHmmss"));
                        }
                        File.Move(file, desFile);
                        Def.WriteLog("RunProcessDryingOven", $"FTPUploadFile()上传{fileInfo.Name}至FTP服务器{ftpDir}成功！");
                        
                    }
                    catch (System.Exception ex)
                    {
                        Def.WriteLog("RunProcessDryingOven", $"FTPUploadFile()上传{fileInfo.Name}至FTP服务器{ftpDir}引发异常：{ex.Message}", LogType.Error);
                        return false;
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// FTP 上传MES过程参数表
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        private bool FTPUploadMESParamerFile(ResourcesStruct rs, int cavityIdx)
        {
            if (!MachineCtrl.GetInstance().UpdataMes)
            {
                return true;
            }
            string ftpUser = FTPDefine.User;
            string ftpPW = FTPDefine.Password;

            for (int col = 0; col < (int)OvenRowCol.MaxCol; col++)
            {
                //string file = string.Format(@"{0}\MES工艺数据\{1}-{2}层\{3}.csv"
                //        , MachineCtrl.GetInstance().ProductionFilePath, this.RunName, (cavityIdx + 1)
                //        , this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + col].Code
                //        );

                Pallet pallets = this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + col];
                string file = string.Format(@"{0}\MES工艺数据\{1}-{2}层\{3}" + "_" + "{4}" + "_" + "{5}" + "_" + "{6}.csv"
                        , MachineCtrl.GetInstance().ProductionFilePath, this.RunName, (cavityIdx + 1)
                        ,  MesResources.OvenCavity[0, 0].ProcessID, MesResources.OvenCavity[0, 0].EquipmentID, pallets.Code, pallets.EndDate.ToString("yyyy_MM_dd_HHmmss"));
                if (File.Exists(file))
                {
                    //IP / 厂房 / 拉线 / 工序 / 设备编号 / yyyy - MM（年 - 月）/
                    FileInfo fileInfo = new FileInfo(file);
                    // 创建文件目录
                    string ftpDir = FTPDefine.FilePath;
                    if (!FTPClient.FtpDirIsExists(ftpDir, ftpUser, ftpPW) && !FTPClient.MakeDirectory(ftpDir, ftpUser, ftpPW))
                    {
                        ShowMessageBox((int)MsgID.FTPUploadErr, $"创建FTP目录{ftpDir}失败", "请检查是否有权限创建FTP目录", MessageType.MsgWarning);
                        return false;
                    }
                    ftpDir += $@"/{rs.EquipmentID}";
                    if (!FTPClient.FtpDirIsExists(ftpDir, ftpUser, ftpPW) && !FTPClient.MakeDirectory(ftpDir, ftpUser, ftpPW))
                    {
                        ShowMessageBox((int)MsgID.FTPUploadErr, $"创建FTP目录{ftpDir}失败", "请检查是否有权限创建FTP目录", MessageType.MsgWarning);
                        return false;
                    }
                    ftpDir += $@"/{DateTime.Now.ToString("yyyy_MM")}";
                    if (!FTPClient.FtpDirIsExists(ftpDir, ftpUser, ftpPW) && !FTPClient.MakeDirectory(ftpDir, ftpUser, ftpPW))
                    {
                        ShowMessageBox((int)MsgID.FTPUploadErr, $"创建FTP目录{ftpDir}失败", "请检查是否有权限创建FTP目录", MessageType.MsgWarning);
                        return false;
                    }
                    if (!FTPClient.UploadFile(fileInfo, ftpDir, ftpUser, ftpPW))
                    {
                        ShowMessageBox((int)MsgID.FTPUploadErr, $"上传文件{ftpDir}至FTP服务器失败", "请检查FTP服务器是否启动", MessageType.MsgWarning);
                        return false;
                    }
                    string desFile = string.Format(@"{0}\MES工艺数据\{1}-{2}层\{5}\{3}-{4}.csv"
                            , MachineCtrl.GetInstance().ProductionFilePath, this.RunName, (cavityIdx + 1)
                            , this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + col].Code
                            , this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + col].EndDate.ToString("yyyy_MM_dd_HHmmss")
                            , DateTime.Now.ToString("yyyy_MM_dd"));
                    try
                    {
                        if (Def.CreateFilePath(desFile) && File.Exists(desFile))
                        {
                            desFile = string.Format(@"{0}\MES工艺数据\{1}-{2}层\{5}\{3}-{4}({6}).csv"
                                    , MachineCtrl.GetInstance().ProductionFilePath, this.RunName, (cavityIdx + 1)
                                    , this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + col].Code
                                    , this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + col].EndDate.ToString("yyyy_MM_dd_HHmmss")
                                    , DateTime.Now.ToString("yyyy_MM_dd")
                                    , this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + col].EndDate.ToString("yyyy_MM_dd_HHmmss"));
                        }
                        File.Move(file, desFile);
                        Def.WriteLog("RunProcessDryingOven", $"FTPUploadFile()上传{fileInfo.Name}至FTP服务器{ftpDir}成功！");

                    }
                    catch (System.Exception ex)
                    {
                        Def.WriteLog("RunProcessDryingOven", $"FTPUploadFile()上传{fileInfo.Name}至FTP服务器{ftpDir}引发异常：{ex.Message}", LogType.Error);
                        return false;
                    }
                }
            }
            return true;
        }

        #endregion

        #region // 夹具中电池状态

        /// <summary>
        /// 夹具中含有NG电池
        /// </summary>
        /// <param name="plt"></param>
        /// <returns></returns>
        private bool PltHasNGBat(Pallet plt)
        {
            for(int row = 0; row < plt.MaxRow; row++)
            {
                for(int col = 0; col < plt.MaxCol; col++)
                {
                    if(BatteryStatus.NG == plt.Battery[row, col].Type)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 温度报警时设置腔体中夹具电池NG
        /// </summary>
        /// <param name="cavityData"></param>
        private bool SetCavityPltBatteryNG(int cavityIdx, int pltCol, CavityData cavityData)
        {
            bool hasNG = false;
            int pltIdx = cavityIdx * (int)OvenRowCol.MaxCol + ((0 == this.dryingOvenGroup) ? pltCol : (1 - pltCol));
            Pallet plt = this.Pallet[pltIdx];
            for(int idx = 0; idx < (int)OvenInfoCount.HeatPanelCount; idx++)
            {
                for(OvenTmpAlarm almIdx = OvenTmpAlarm.Normal; almIdx < OvenTmpAlarm.End; almIdx++)
                {
                    #region // 没有NG，查询下一个
                    if(cavityData.tempAlarmValue[pltCol, idx] < 1.0)
                    {
                        // 没有温度值
                        continue;
                    }
                    if((cavityData.tempAlarm[pltCol, idx] & (0x01 << (int)almIdx)) != (0x01 << (int)almIdx))
                    {
                        // 没有当前查询的温度报警状态
                        continue;
                    }
                    #endregion

                    switch(almIdx)
                    {
                        #region // 低温，超温，信号异常：整盘NG
                        case OvenTmpAlarm.LowTmp:
                        case OvenTmpAlarm.OverTmp:
                        case OvenTmpAlarm.Exceptional:
                            {
                                // 置夹具NG
                                if(PalletStatus.Invalid != plt.State)
                                {
                                    this.Pallet[pltIdx].State = PalletStatus.NG;
                                    SaveRunData(SaveType.Pallet);
                                }
                                break;
                            }
                        #endregion

                        #region // 超高温：电芯NG

                        case OvenTmpAlarm.HighTmp:
                            {
                                switch(idx)
                                {
                                    case 3:
                                        {
                                            for(int row = 0; row < plt.MaxRow; row++)
                                            {
                                                if (BatteryStatus.OK == plt.Battery[row, 0].Type)
                                                {
                                                    plt.Battery[row, 0].Type = BatteryStatus.NG;
                                                    plt.Battery[row, 0].NGType = BatteryNGStatus.HighTmp;
                                                }
                                            }
                                            break;
                                        }
                                    case 2:
                                        {
                                            for(int row = 0; row < plt.MaxRow; row++)
                                            {
                                                if(BatteryStatus.OK == plt.Battery[row, 1].Type)
                                                {
                                                    plt.Battery[row, 1].Type = BatteryStatus.NG;
                                                    plt.Battery[row, 1].NGType = BatteryNGStatus.HighTmp;
                                                }
                                                if(BatteryStatus.OK == plt.Battery[row, 2].Type)
                                                {
                                                    plt.Battery[row, 2].Type = BatteryStatus.NG;
                                                    plt.Battery[row, 2].NGType = BatteryNGStatus.HighTmp;
                                                }
                                            }
                                            break;
                                        }
                                    case 1:
                                        {
                                            for(int row = 0; row < plt.MaxRow; row++)
                                            {
                                                if(BatteryStatus.OK == plt.Battery[row, 2].Type)
                                                {
                                                    plt.Battery[row, 2].Type = BatteryStatus.NG;
                                                    plt.Battery[row, 2].NGType = BatteryNGStatus.HighTmp;
                                                }
                                                if(BatteryStatus.OK == this.Pallet[pltIdx].Battery[row, 3].Type)
                                                {
                                                    plt.Battery[row, 3].Type = BatteryStatus.NG;
                                                    plt.Battery[row, 3].NGType = BatteryNGStatus.HighTmp;
                                                }
                                            }
                                            break;
                                        }
                                    case 0:
                                        {
                                            for(int row = 0; row < plt.MaxRow; row++)
                                            {
                                                if(BatteryStatus.OK == plt.Battery[row, 4].Type)
                                                {
                                                    plt.Battery[row, 4].Type = BatteryStatus.NG;
                                                    plt.Battery[row, 4].NGType = BatteryNGStatus.HighTmp;
                                                }
                                            }
                                            break;
                                        }
                                    default:
                                        break;
                                }
                                // 置夹具NG
                                if(PalletStatus.Invalid != plt.State)
                                {
                                    plt.State = PalletStatus.NG;
                                    hasNG = true;
                                }
                                break;
                            }
                        #endregion

                        #region // 温差异常：不NG
                        case OvenTmpAlarm.Difference:
                            {
                                break;
                            }
                            #endregion
                    }
                }
            }
            if(hasNG)
            {
                SaveRunData(SaveType.Pallet, pltIdx);
                return true;
            }
            return false;
        }

        #endregion

        #region // 重写基类接口

        /// <summary>
        /// 插入历史报警记录
        /// </summary>
        /// <param name="msgID">报警ID</param>
        /// <param name="msg">报警信息</param>
        /// <param name="msgType">报警类型</param>
        /// <param name="runModuleID">运行模组ID</param>
        /// <param name="runName">运行模组名</param>
        /// <param name="productFormula">产品参数ID</param>
        /// <param name="curTime">当前时间</param>
        public override void InsertAlarmInfo(int msgID, string msg, int msgType, int runModuleID, string runName, int productFormula, string curTime)
        {
            try
            {
                dbRecord.AddAlarmInfo(new AlarmFormula(productFormula, msgID, msg, msgType, runModuleID, runName, curTime));

                if((int)MessageType.MsgAlarm == msgType)
                {
                    int cavityIdx = -1;
                    int idx = msg.IndexOf("层") - 1;
                    if((idx >= 0) && int.TryParse(msg.Substring(idx, 1), out cavityIdx))
                    {
                        MesOperateMySql.EquipmentAlarm(msgID, msg.Replace("\r", "").Replace("\n", " "), msgType, MesResources.OvenCavity[this.dryingOvenID, cavityIdx - 1]);
                    }
                }
            }
            catch(System.Exception ex)
            {
                Trace.WriteLine("RunProcess.InsertAlarmInfo() error: " + ex.Message);
            }
        }
        #endregion

        #region // 模组信号重置

        /// <summary>
        /// 模组信号重置
        /// </summary>
        public override void ResetModuleEvent()
        {
            for(int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            {
                if (this.CavityEnable[i])
                {
                    ShowMsgBox.ShowDialog($"模组信号重置前需要首先将{this.RunName}所有炉腔全部禁用，当前{i + 1}层炉腔未禁用", MessageType.MsgWarning);
                    return;
                }
                if (!Def.IsNoHardware() && (short)OvenStatus.DoorClose != RCavity(i).doorState)
                {
                    ShowMsgBox.ShowDialog($"模组信号重置前需要首先将{this.RunName}所有炉腔炉门全部关闭，当前{i + 1}层炉腔未关闭", MessageType.MsgWarning);
                    return;
                }
            }
            bool needSave = false;
            for(EventList i = EventList.Invalid; i < EventList.EventEnd; i++)
            {
                if (this.moduleEvent.ContainsKey(i))
                {
                    SetEvent(this, i, EventStatus.Invalid);
                    needSave = true;
                }
            }
            if (needSave)
            {
                this.nextAutoStep = AutoSteps.Auto_WaitWorkStart;
                SaveRunData(SaveType.AutoStep);
            }
        }
        #endregion

    }
}
