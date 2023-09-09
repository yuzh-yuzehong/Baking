using HelperLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;
using log4net;

namespace Machine
{
    public class RunProcess : RunEx
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        #region // 枚举：步骤，模组数据，报警列表

        protected new enum InitSteps
        {
            Init_DataRecover = 0,

            Init_End,
        }

        protected enum CheckSteps
        {
            Check_WorkStart = 0,

            Check_WorkEnd,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,

            Auto_WorkEnd,
        }
        #endregion

        #region // 字段，属性

        #region // IO
        #endregion

        #region // 电机
        #endregion

        #region // ModuleEx.cfg配置
        #endregion

        #region // 模组参数

        public bool OnLoad { get; protected set; }              // 上料使能
        public bool OffLoad { get; protected set; }             // 下料使能
        #endregion

        #region // 模组数据

        public Pallet[] Pallet { get; protected set; }          // 模组中夹具
        public Battery[] Battery { get; protected set; }        // 模组中电池
        public BatteryLine BatteryLine { get; protected set; }   // 模组中电池线：主要用于冷却系统

        public bool InitStepSafe { get; protected set; }        // 初始化安全标识
        public bool AutoStepSafe { get; protected set; }        // 自动运行安全标识
        public object AutoCheckStep { get; protected set; }     // 自动运行时的检查步骤

        public Dictionary<EventList, ModuleEvent> moduleEvent;         // 模组事件

        protected DataBaseRecord dbRecord;    // 数据库记录集
        protected IniStream iniStream;        // 运行数据读写文件流
        protected HttpClient httpClient;      // MES通讯的http对象

        private Dictionary<string, ParameterFormula> insertParameterList;      // 模组中插入的参数集：<参数关键字key, 参数样式>
        public Dictionary<string, ParameterFormula> dataBaseParameterList;    // 数据库中保存的参数集：<参数关键字key, 参数样式>


        #endregion

        #endregion

        public RunProcess(int runId) : base(runId)
        {
            InitBatteryPalletSize(0, 0);

            PowerUpRestart();

            this.insertParameterList = new Dictionary<string, ParameterFormula>();
            this.dataBaseParameterList = new Dictionary<string, ParameterFormula>();
            this.httpClient = new HttpClient();

            InitParameter();
            // 参数
            //InsertGroupParameter("OnLoad", "上料使能", "上料使能：True启用，False禁用", this.OnLoad, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            //InsertGroupParameter("OffLoad", "下料使能", "下料使能：True启用，False禁用", this.OffLoad, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
        }

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
                    Trace.Assert(false, "RunProcess.InitOperation/no this init step");
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

            switch((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");
                        this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                        break;
                    }
                case AutoSteps.Auto_WorkEnd:
                    {
                        CurMsgStr("工作完成", "Work end");
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

        #region // 防呆检查

        /// <summary>
        /// 检查输出点位是否可操作
        /// </summary>
        /// <param name="output"></param>
        /// <param name="bOn"></param>
        /// <returns></returns>
        public virtual bool CheckOutputCanActive(Output output, bool bOn)
        {
            return true;
        }

        /// <summary>
        /// 检查电机是否可移动
        /// </summary>
        /// <param name="motor"></param>
        /// <param name="nLocation"></param>
        /// <param name="fValue"></param>
        /// <param name="moveType"></param>
        /// <returns></returns>
        public virtual bool CheckMotorCanMove(Motor motor, int nLocation, float fValue, MotorMoveType moveType)
        {
            return true;
        }

        /// <summary>
        /// 模组防呆监视：请勿加入耗时过长或阻塞操作
        /// </summary>
        public virtual void MonitorAvoidDie()
        {
        }

        /// <summary>
        /// 设备停止后操作，如果派生类重写了该函数，它必须调用基实现。
        /// </summary>
        public virtual void AfterStopAction()
        {
            this.AutoCheckStep = CheckSteps.Check_WorkStart;
        }

        /// <summary>
        /// 休眠ms
        /// </summary>
        /// <param name="millisecondsTimeout"></param>
        protected void Sleep(int millisecondsTimeout)
        {
            Thread.Sleep(millisecondsTimeout);
        }

        #endregion

        #region // 运行数据读写

        /// <summary>
        /// 初始化电池及夹具大小
        /// </summary>
        /// <param name="batterySize"></param>
        /// <param name="palletSize"></param>
        protected void InitBatteryPalletSize(int batterySize, int palletSize, int batteryLineSize = -1)
        {
            if (batterySize > -1)
            {
                this.Battery = new Battery[batterySize];
                for(int i = 0; i < batterySize; i++)
                {
                    this.Battery[i] = new Battery();
                }
            }
            if (palletSize > -1)
            {
                this.Pallet = new Pallet[palletSize];
                for(int i = 0; i < palletSize; i++)
                {
                    this.Pallet[i] = new Pallet();
                }
            }
            if (batteryLineSize > -1)
            {
                this.BatteryLine = new BatteryLine();
            }
        }
        
        /// <summary>
        /// 初始化运行数据
        /// </summary>
        public virtual void InitRunData()
        {
            this.nextInitStep = InitSteps.Init_DataRecover;
            this.nextAutoStep = AutoSteps.Auto_WaitWorkStart;
            this.InitStepSafe = false;
            this.AutoStepSafe = false;
            this.AutoCheckStep = 0;
            if (null == this.moduleEvent)
            {
                this.moduleEvent = new Dictionary<EventList, ModuleEvent>();
            }
            this.moduleEvent.Clear();
            for(int i = 0; i < this.Battery.Length; i++)
            {
                this.Battery[i].Release();
            }
            for(int i = 0; i < this.Pallet.Length; i++)
            {
                this.Pallet[i].Release();
            }
        }

        /// <summary>
        /// 读取运行数据
        /// </summary>
        public virtual void LoadRunData()
        {
            string section, key, file;
            section = this.RunModule;
            file = $"{Def.GetAbsPathName(Def.RunDataFolder)}{section}.cfg";
            
            this.nextAutoStep = iniStream.ReadInt(section, "nextAutoStep", (int)this.nextAutoStep);
            for (int i = 0; i < (int)EventList.EventEnd; i++)
			{
                key = $"moduleEvent[{i}].Event";
                EventList modEvent = (EventList)iniStream.ReadInt(section, key, (int)EventList.Invalid);
                if (modEvent > EventList.Invalid)
                {
                    key = $"moduleEvent[{i}].State";
                    EventStatus eventState = (EventStatus)iniStream.ReadInt(section, key, (int)EventStatus.Invalid);
                    key = $"moduleEvent[{i}].Pos";
                    int pos = iniStream.ReadInt(section, key, -1);
                    if (this.moduleEvent.ContainsKey(modEvent))
                    {
                        this.moduleEvent[modEvent] = (new ModuleEvent(modEvent, eventState, pos));
                    }
                    else
                    {
                        this.moduleEvent.Add(modEvent, (new ModuleEvent(modEvent, eventState, pos)));
                    }
                }
            }
            for(int i = 0; i < this.Battery.Length; i++)
            {
                key = $"Battery[{i}].Code";
                Battery[i].Code = iniStream.ReadString(section, key, Battery[i].Code);
                key = $"Battery[{i}].Type";
                Battery[i].Type = (BatteryStatus)iniStream.ReadInt(section, key, (int)Battery[i].Type);
                key = $"Battery[{i}].NGType";
                Battery[i].NGType = (BatteryNGStatus)iniStream.ReadInt(section, key, (int)Battery[i].NGType);
            }
            for(int i = 0; i < this.Pallet.Length; i++)
            {
                key = $"Pallet[{i}].Code";
                this.Pallet[i].Code = iniStream.ReadString(section, key, this.Pallet[i].Code);
                key = $"Pallet[{i}].State";
                this.Pallet[i].State = (PalletStatus)iniStream.ReadInt(section, key, (int)this.Pallet[i].State);
                key = $"Pallet[{i}].Stage";
                this.Pallet[i].Stage = (PalletStage)iniStream.ReadInt(section, key, (int)this.Pallet[i].Stage);
                int maxRow, maxCol;
                key = $"Pallet[{i}].MaxRow";
                maxRow = iniStream.ReadInt(section, key, this.Pallet[i].MaxRow);
                key = $"Pallet[{i}].MaxCol";
                maxCol = iniStream.ReadInt(section, key, this.Pallet[i].MaxCol);
                this.Pallet[i].SetRowCol(maxRow, maxCol);
                key = $"Pallet[{i}].NeedFake";
                this.Pallet[i].NeedFake = iniStream.ReadBool(section, key, this.Pallet[i].NeedFake);
                key = $"Pallet[{i}].BakingCount";
                this.Pallet[i].BakingCount = iniStream.ReadInt(section, key, this.Pallet[i].BakingCount);
                key = $"Pallet[{i}].SrcStation";
                this.Pallet[i].SrcStation = iniStream.ReadInt(section, key, this.Pallet[i].SrcStation);
                key = $"Pallet[{i}].SrcRow";
                this.Pallet[i].SrcRow = iniStream.ReadInt(section, key, this.Pallet[i].SrcRow);
                key = $"Pallet[{i}].SrcCol";
                this.Pallet[i].SrcCol = iniStream.ReadInt(section, key, this.Pallet[i].SrcCol);
                key = $"Pallet[{i}].StartDate";
                DateTime.TryParse(iniStream.ReadString(section, key, this.Pallet[i].StartDate.ToString(Def.DateFormal)), out this.Pallet[i].StartDate);
                key = $"Pallet[{i}].EndDate";
                DateTime.TryParse(iniStream.ReadString(section, key, this.Pallet[i].EndDate.ToString(Def.DateFormal)), out this.Pallet[i].EndDate);
                for(int row = 0; row < this.Pallet[i].MaxRow; row++)
                {
                    for(int col = 0; col < this.Pallet[i].MaxCol; col++)
                    {
                        key = $"Pallet[{i}].Battery[{row}, {col}].Code";
                        this.Pallet[i].Battery[row, col].Code = iniStream.ReadString(section, key, this.Pallet[i].Battery[row, col].Code);
                        key = $"Pallet[{i}].Battery[{row}, {col}].Type";
                        this.Pallet[i].Battery[row, col].Type = (BatteryStatus)iniStream.ReadInt(section, key, (int)this.Pallet[i].Battery[row, col].Type);
                        key = $"Pallet[{i}].Battery[{row}, {col}].NGType";
                        this.Pallet[i].Battery[row, col].NGType = (BatteryNGStatus)iniStream.ReadInt(section, key, (int)this.Pallet[i].Battery[row, col].NGType);
                    }
                }
            }
        }

        /// <summary>
        /// 保存运行数据，派生类继承重写此接口时，需在派生类最后调用此基类接口
        /// </summary>
        /// <param name="saveType"></param>
        /// <param name="index"></param>
        public virtual void SaveRunData(SaveType saveType, int index = -1)
        {
            string section, key, file;
            section = this.RunModule;
            file = $"{Def.GetAbsPathName(Def.RunDataFolder)}{section}.cfg";

            if (SaveType.AutoStep == (SaveType.AutoStep & saveType))
            {
                iniStream.WriteInt(this.RunModule, "nextAutoStep", (int)this.nextAutoStep);
            }
            if(SaveType.SignalEvent == (SaveType.SignalEvent & saveType))
            {
                ModuleEvent[] evt = new ModuleEvent[this.moduleEvent.Count];
                this.moduleEvent.Values.CopyTo(evt, 0);
                for(int i = 0; i < evt.Length; i++)
                {
                    key = $"moduleEvent[{(int)evt[i].Event}].Event";
                    iniStream.WriteInt(section, key, (int)evt[i].Event);
                    key = $"moduleEvent[{(int)evt[i].Event}].State";
                    iniStream.WriteInt(section, key, (int)evt[i].State);
                    key = $"moduleEvent[{(int)evt[i].Event}].Pos";
                    iniStream.WriteInt(section, key, evt[i].Pos);
                }
            }
            if(SaveType.Battery == (SaveType.Battery & saveType))
            {
                for(int i = 0; i < this.Battery.Length; i++)
                {
                    key = $"Battery[{i}].Code";
                    iniStream.WriteString(section, key, Battery[i].Code);
                    key = $"Battery[{i}].Type";
                    iniStream.WriteInt(section, key, (int)Battery[i].Type);
                    key = $"Battery[{i}].NGType";
                    iniStream.WriteInt(section, key, (int)Battery[i].NGType);
                }
            }
            if(SaveType.Pallet == (SaveType.Pallet & saveType))
            {
                for(int i = 0; i < this.Pallet.Length; i++)
                {
                    if ((i == index) || (index < 0))
                    {
                        key = $"Pallet[{i}].Code";
                        iniStream.WriteString(section, key, this.Pallet[i].Code);
                        key = $"Pallet[{i}].State";
                        iniStream.WriteInt(section, key, (int)this.Pallet[i].State);
                        key = $"Pallet[{i}].Stage";
                        iniStream.WriteInt(section, key, (int)this.Pallet[i].Stage);
                        key = $"Pallet[{i}].MaxRow";
                        iniStream.WriteInt(section, key, this.Pallet[i].MaxRow);
                        key = $"Pallet[{i}].MaxCol";
                        iniStream.WriteInt(section, key, this.Pallet[i].MaxCol);
                        key = $"Pallet[{i}].NeedFake";
                        iniStream.WriteBool(section, key, this.Pallet[i].NeedFake);
                        key = $"Pallet[{i}].BakingCount";
                        iniStream.WriteInt(section, key, this.Pallet[i].BakingCount);
                        key = $"Pallet[{i}].SrcStation";
                        iniStream.WriteInt(section, key, this.Pallet[i].SrcStation);
                        key = $"Pallet[{i}].SrcRow";
                        iniStream.WriteInt(section, key, this.Pallet[i].SrcRow);
                        key = $"Pallet[{i}].SrcCol";
                        iniStream.WriteInt(section, key, this.Pallet[i].SrcCol);
                        key = $"Pallet[{i}].StartDate";
                        iniStream.WriteString(section, key, this.Pallet[i].StartDate.ToString(Def.DateFormal));
                        key = $"Pallet[{i}].EndDate";
                        iniStream.WriteString(section, key, this.Pallet[i].EndDate.ToString(Def.DateFormal));
                        for(int row = 0; row < this.Pallet[i].MaxRow; row++)
                        {
                            for(int col = 0; col < this.Pallet[i].MaxCol; col++)
                            {
                                key = $"Pallet[{i}].Battery[{row}, {col}].Code";
                                iniStream.WriteString(section, key, this.Pallet[i].Battery[row, col].Code);
                                key = $"Pallet[{i}].Battery[{row}, {col}].Type";
                                iniStream.WriteInt(section, key, (int)this.Pallet[i].Battery[row, col].Type);
                                key = $"Pallet[{i}].Battery[{row}, {col}].NGType";
                                iniStream.WriteInt(section, key, (int)this.Pallet[i].Battery[row, col].NGType);
                            }
                        }
                    }
                }
            }
            iniStream.DataToFile();
        }

        /// <summary>
        /// 删除运行数据
        /// </summary>
        public void DeleteRunData()
        {
            try
            {
                string src = $"{Def.GetAbsPathName(Def.RunDataFolder)}{this.RunModule}.cfg";
                string des = $"{Def.GetAbsPathName(Def.RunDataBakFolder)}{this.RunModule}.cfg";
                if(File.Exists(des))
                {
                    File.Delete(des);
                }
                if(File.Exists(src))
                {
                    File.Move(src, des);
                }
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine("RunProcess.DeleteRunData() fail: " + ex.Message);
            }

            this.iniStream.DeleteFile();
        }

        /// <summary>
        /// 备份运行数据
        /// </summary>
        public void BackupRunData()
        {
            try
            {
                string src = $"{Def.GetAbsPathName(Def.RunDataFolder)}{this.RunModule}.cfg";
                string des = $"{Def.GetAbsPathName(Def.RunDataBakFolder)}{this.RunModule}.cfg";
                if (File.Exists(des))
                {
                    File.Delete(des);
                }
                if (File.Exists(src))
                {
                    //File.Move(src, des);
                    File.Copy(src, des);
                }
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine("RunProcess.BackupRunData() fail: " + ex.Message);
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
            this.RunModule = module;
            this.RunName = IniFile.ReadString(module, "Name", "", Def.GetAbsPathName(Def.ModuleExCfg));
            this.RunClass = IniFile.ReadString(module, "Class", "", Def.GetAbsPathName(Def.ModuleExCfg));

            // 初始化模组中的IO及电机
            InitModuleIOMotor();

            if (null == this.iniStream)
            {
                this.iniStream = new IniStream();
            }
            if(!this.iniStream.OpenRead($"{Def.GetAbsPathName(Def.RunDataFolder)}{module}.cfg"))
            {
                Trace.Assert(false, $"RunProcess{module}.InitializeConfig().iniStream.OpenRead() fail.");
                return false;
            }

            this.dbRecord = MachineCtrl.GetInstance().dbRecord;
            #region // 调用基类InitializeConfig(module);之前先获取参数列表，此接口会读取运行参数；

            List<ParameterFormula> listPara = new List<ParameterFormula>();
            this.dbRecord.GetParameterList(Def.GetProductFormula(), module, ref listPara);
            foreach(var item in listPara)
            {
                this.dataBaseParameterList.Add(item.key, item);
            }
            #endregion

            return base.InitializeConfig(module);
        }

        /// <summary>
        /// 保存模组配置
        /// </summary>
        /// <returns></returns>
        public override bool SaveConfig()
        {
            return base.SaveConfig();
        }

        /// <summary>
        /// 初始化通用组参数 + 模组参数
        /// </summary>
        protected virtual void InitParameter()
        {
            this.OnLoad = false;
            this.OffLoad = false;
        }

        /// <summary>
        /// 获取通用组参数 + 模组参数
        /// </summary>
        /// <returns></returns>
        public PropertyManage GetParameterList()
        {
            PropertyManage pm = this.ParameterProperty;
            foreach(Property item in this.ParameterProperty)
            {
                if (null != pm[item.Name])
                {
                    if (item.Value is int)
                    {
                        pm[item.Name].Value = ReadIntParameter(this.RunModule, item.Name, Convert.ToInt32(item.Value));
                    }
                    else if(item.Value is uint)
                    {
                        pm[item.Name].Value = (uint)ReadIntParameter(this.RunModule, item.Name, Convert.ToInt32(item.Value));
                    }
                    else if(item.Value is short)
                    {
                        pm[item.Name].Value = (short)ReadIntParameter(this.RunModule, item.Name, Convert.ToInt32(item.Value));
                    }
                    else if(item.Value is bool)
                    {
                        pm[item.Name].Value = ReadBoolParameter(this.RunModule, item.Name, Convert.ToBoolean(item.Value));
                    }
                    else if(item.Value is float)
                    {
                        pm[item.Name].Value = ReadDoubleParameter(this.RunModule, item.Name, Convert.ToDouble(item.Value));
                    }
                    else if(item.Value is double)
                    {
                        pm[item.Name].Value = ReadDoubleParameter(this.RunModule, item.Name, Convert.ToDouble(item.Value));
                    }
                    else if(item.Value is string)
                    {
                        pm[item.Name].Value = ReadStringParameter(this.RunModule, item.Name, Convert.ToString(item.Value));
                    }
                    else
                    {
                        string msg = $"{item.DisplayName}】为{item.Value.GetType().ToString()}类型，未找到相匹配类型，无法获取参数值";
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
        /// 读取通用组参数 + 模组参数
        /// </summary>
        /// <returns></returns>
        public override bool ReadParameter()
        {
            this.OnLoad = ReadBoolParameter(this.RunModule, "OnLoad", true);
            this.OffLoad = ReadBoolParameter(this.RunModule, "OffLoad", true);

            return base.ReadParameter();
        }

        /// <summary>
        /// 读取本模组的相关模组
        /// </summary>
        public virtual void ReadRelatedModule()
        {

        }

        /// <summary>
        /// 添加常用组参数
        /// </summary>
        /// <param name="key">属性关键字</param>
        /// <param name="name">显示名称</param>
        /// <param name="description">描述</param>
        /// <param name="value">属性值</param>
        /// <param name="paraLevel">属性权限</param>
        /// <param name="readOnly">属性仅可读</param>
        /// <param name="visible">属性可见性</param>
        protected void InsertGroupParameter(string key, string name, string description, object value, RecordType paraType, ParameterLevel paraLevel = ParameterLevel.PL_STOP_MAIN, bool readOnly = false, bool visible = true)
        {
            this.insertParameterList.Add(key, new ParameterFormula(Def.GetProductFormula(), this.RunModule, name, key, value.ToString(), paraType, paraLevel));
            this.ParameterProperty.Add("常用组参数", key, name, description, value, (int)paraLevel, readOnly, visible);
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
        protected void InsertVoidParameter(string key, string name, string description, object value, RecordType paraType, ParameterLevel paraLevel = ParameterLevel.PL_STOP_MAIN, bool readOnly = false, bool visible = true)
        {
            this.insertParameterList.Add(key, new ParameterFormula(Def.GetProductFormula(), this.RunModule, name, key, value.ToString(), paraType, paraLevel));
            this.ParameterProperty.Add("模组参数", key, name, description, value, (int)paraLevel, readOnly, visible);
        }
        #endregion

        #region // 读IO及电机

        /// <summary>
        /// 初始化IO及电机
        /// </summary>
        protected virtual void InitModuleIOMotor()
        {

        }

        /// <summary>
        /// 添加输入Input
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected int AddInput(string key)
        {
            string value = IniFile.ReadString(this.RunModule, key, "", Def.GetAbsPathName(Def.ModuleExCfg));
            if("" == value)
            {
                IniFile.WriteString(this.RunModule, key, "", Def.GetAbsPathName(Def.ModuleExCfg));
            }
            int index = MachineCtrl.GetInstance().DecodeInputID(value);
            this.inputMap.Add(key, index);
            log.DebugFormat("AddInput: {0}-{1}:{2}", this.RunModule, key, value);
            return index;
        }

        /// <summary>
        /// 添加输出Output
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected int AddOutput(string key)
        {
            string value = IniFile.ReadString(this.RunModule, key, "", Def.GetAbsPathName(Def.ModuleExCfg));
            if("" == value)
            {
                IniFile.WriteString(this.RunModule, key, "", Def.GetAbsPathName(Def.ModuleExCfg));
            }
            int index = MachineCtrl.GetInstance().DecodeOutputID(value);
            this.outputMap.Add(key, index);
            return index;
        }

        /// <summary>
        /// 添加电机Motor
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected int AddMotor(string key)
        {
            string value = IniFile.ReadString(this.RunModule, key, "", Def.GetAbsPathName(Def.ModuleExCfg));
            if("" == value)
            {
                IniFile.WriteString(this.RunModule, key, "", Def.GetAbsPathName(Def.ModuleExCfg));
            }
            int index = MachineCtrl.GetInstance().DecodeMotorID(value);
            this.motorMap.Add(key, index);
            return index;
        }

        #endregion

        #region // IO及电机操作接口

        protected Input Inputs(int index)
        {
            return DeviceManager.Inputs(index);
        }

        protected Output Outputs(int index)
        {
            return DeviceManager.Outputs(index);
        }

        protected Motor Motors(int index)
        {
            return DeviceManager.Motors(index);
        }

        protected bool InputState(int input, bool isOn)
        {
            if (input < 0 || Def.IsNoHardware())
            {
                return true;
            }
            return (isOn ? DeviceManager.Inputs(input).IsOn() : DeviceManager.Inputs(input).IsOff());
        }

        protected bool CheckInputState(int input, bool isOn)
        {
            if(input < 0 || Def.IsNoHardware())
            {
                return true;
            }
            return CheckInput(DeviceManager.Inputs(input), isOn);
        }

        protected bool WaitInputState(int input, bool isOn)
        {
            if(input < 0 || Def.IsNoHardware())
            {
                return true;
            }
            return WaitInput(DeviceManager.Inputs(input), isOn);
        }

        protected bool OutputState(int output, bool isOn)
        {
            if(output < 0 || Def.IsNoHardware())
            {
                return true;
            }
            return (isOn ? DeviceManager.Outputs(output).IsOn() : DeviceManager.Outputs(output).IsOff());
        }

        protected bool OutputAction(int output, bool isOn)
        {
            if(output < 0 || Def.IsNoHardware())
            {
                return true;
            }
            if(isOn ? DeviceManager.Outputs(output).IsOn() : DeviceManager.Outputs(output).IsOff())
            {
                return true;
            }
            return (isOn ? DeviceManager.Outputs(output).On() : DeviceManager.Outputs(output).Off());
        }
        
        protected bool CheckMotorPos(int motorID, MotorPosition posID, bool alarm = true)
        {
            if (Def.IsNoHardware())
            {
                return true;
            }
            if (motorID > -1)
            {
                string posName = "";
                float curPos, destPos;
                curPos = destPos = 0.0f;
                if (((int)MotorCode.MotorOK == Motors(motorID).GetCurPos(ref curPos))
                    && ((int)MotorCode.MotorOK == Motors(motorID).GetLocation((int)posID, ref posName, ref destPos)))
                {
                    if(Math.Abs(curPos - destPos) > Motors(motorID).PosErrRange)
                    {
                        if (alarm)
                        {
                            string msg = string.Format("{0}】不在[{1} {2}]位置！"
                                , Motors(motorID).Name, ((int)posID), posName);
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                        }
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 夹具放平检测
        /// </summary>
        /// <param name="pltIdx"></param>
        /// <param name="hasPlt"></param>
        /// <param name="alarm"></param>
        public virtual bool PalletKeepFlat(int pltIdx, bool hasPlt, bool alarm = true)
        {
            Trace.Assert(false, "RunProcess.PalletKeepFlat() is invallid");
            return false;
        }

        #endregion

        #region // 数据库读写参数

        public int ReadIntParameter(string section, string key, int defaultValue)
        {
            try
            {
                if((section == this.RunModule) && this.dataBaseParameterList.ContainsKey(key))
                {
                    defaultValue = Convert.ToInt32(this.dataBaseParameterList[key].value);
                }
            }
            catch (System.Exception ex)
            {
            	
            }
            return defaultValue;
        }
        
        protected bool ReadBoolParameter(string section, string key, bool defaultValue)
        {
            try
            {
                if((section == this.RunModule) && this.dataBaseParameterList.ContainsKey(key))
                {
                    defaultValue = Convert.ToBoolean(this.dataBaseParameterList[key].value);
                }
            }
            catch (System.Exception ex)
            {
            	
            }
            return defaultValue;
        }

        protected double ReadDoubleParameter(string section, string key, double defaultValue)
        {
            try
            {
                if((section == this.RunModule) && this.dataBaseParameterList.ContainsKey(key))
                {
                    defaultValue = Convert.ToDouble(this.dataBaseParameterList[key].value);
                }
            }
            catch (System.Exception ex)
            {
            	
            }
            return defaultValue;
        }

        public string ReadStringParameter(string section, string key, string defaultValue)
        {
            try
            {
                if((section == this.RunModule) && this.dataBaseParameterList.ContainsKey(key))
                {
                    defaultValue = this.dataBaseParameterList[key].value;
                }
            }
            catch (System.Exception ex)
            {
            	
            }
            return defaultValue;
        }
        
        public bool WriteParameter(string section, string key, string value)
        {
            bool result = false;
            if(section == this.RunModule)
            {
                if(this.insertParameterList.ContainsKey(key))
                {
                    ParameterFormula insertPara, dbPara;
                    insertPara = this.insertParameterList[key];
                    insertPara.module = section;
                    insertPara.value = value;
                    if(this.dataBaseParameterList.ContainsKey(key))
                    {
                        dbPara = this.dataBaseParameterList[key];
                        dbPara.value = insertPara.value;
                        dbPara.level = insertPara.level;
                        result = this.dbRecord.ModifyParameter(dbPara);
                    }
                    else
                    {
                        result = this.dbRecord.AddParameter(insertPara);
                    }

                    #region // 保存之后立即读取

                    List<ParameterFormula> listPara = new List<ParameterFormula>();
                    this.dbRecord.GetParameterList(Def.GetProductFormula(), section, ref listPara);
                    foreach(var item in listPara)
                    {
                        if (this.dataBaseParameterList.ContainsKey(item.key))
                        {
                            this.dataBaseParameterList[item.key] = item;
                        }
                        else
                        {
                            this.dataBaseParameterList.Add(item.key, item);
                        }
                    }
                    #endregion
                }
            }
            return result;
        }
        #endregion

        #region // 信号状态交互

        /// <summary>
        /// 设置run模组的信号状态
        /// </summary>
        /// <param name="run"></param>
        /// <param name="modEvent">模组事件</param>
        /// <param name="eventState">事件状态</param>
        /// <param name="eventPos">事件位置</param>
        /// <returns></returns>
        public bool SetEvent(RunProcess run, EventList modEvent, EventStatus eventState, int eventPos = -1)
        {
            if (null != run)
            {
                ModuleEvent tmpEvent = new ModuleEvent(modEvent, eventState, eventPos);
                if(run.moduleEvent.ContainsKey(modEvent))
                {
                    run.moduleEvent[modEvent] = tmpEvent;
                }
                else
                {
                    run.moduleEvent.Add(modEvent, tmpEvent);
                }
                run.SaveRunData(SaveType.SignalEvent);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取run模组的信号状态
        /// </summary>
        /// <param name="modEvent">模组事件</param>
        /// <returns></returns>
        public EventStatus GetEvent(RunProcess run, EventList modEvent)
        {
            if(null != run)
            {
                if(run.moduleEvent.ContainsKey(modEvent))
                {
                    return run.moduleEvent[modEvent].State;
                }
            }
            return EventStatus.Invalid;
        }

        /// <summary>
        /// 获取run模组的信号状态，包含事件位置
        /// </summary>
        /// <param name="run"></param>
        /// <param name="modEvent">模组事件</param>
        /// <param name="eventPos">事件位置</param>
        /// <returns></returns>
        public EventStatus GetEvent(RunProcess run, EventList modEvent, ref int eventPos)
        {
            if(null != run)
            {
                if(run.moduleEvent.ContainsKey(modEvent))
                {
                    eventPos = run.moduleEvent[modEvent].Pos;
                    return run.moduleEvent[modEvent].State;
                }
            }
            return EventStatus.Invalid;
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

                if ((int)MessageType.MsgAlarm == msgType)
                {
                    MesOperateMySql.EquipmentAlarm(msgID, msg.Replace("\r", "").Replace("\n", " "), msgType, MesResources.Heartbeat);
                }
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine("RunProcess.InsertAlarmInfo() error: " + ex.Message);
            }
        }

        #endregion

        #region // 添加删除夹具

        /// <summary>
        /// 添加夹具
        /// </summary>
        /// <param name="pltIdx">夹具索引</param>
        /// <param name="maxRow">夹具最大行</param>
        /// <param name="maxCol">夹具最大列</param>
        /// <param name="batState">夹具最大列</param>
        /// <param name="pltState">夹具最大列</param>
        public virtual void ManualAddPallet(int pltIdx, int maxRow, int maxCol, PalletStatus pltState, BatteryStatus batState)
        {

        }
        /// <summary>
        /// 更改托盘状态 
        /// </summary>
        /// <param name="pltIdx"></param>
        /// <param name="maxRow"></param>
        /// <param name="maxCol"></param>
        /// <param name="pltState"></param>
        /// <param name="batState"></param>
        public virtual  bool ManualSetPalletState(int pltIdx , PalletStatus pltState ) {
            if(this.Pallet[pltIdx]==null)
                return false;
            this.Pallet[pltIdx].State = pltState;
            SaveRunData(SaveType.Pallet , pltIdx);
            return true;
        }

        /// <summary>
        /// 删除夹具
        /// </summary>
        /// <param name="pltIdx"></param>
        public virtual void ManualClearPallet(int pltIdx)
        {
            
        }
        #endregion

        #region // 模组信号重置

        /// <summary>
        /// 模组信号重置
        /// </summary>
        public virtual void ResetModuleEvent()
        {

        }
        #endregion

        #region // 上传MES

        #endregion
    }
}
