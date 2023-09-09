using HelperLibrary;
using System.Diagnostics;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    /// <summary>
    /// 人工操作台
    /// </summary>
    class RunProcessManualOperate : RunProcess
    {
        #region // 枚举：步骤，模组数据，报警列表

        protected new enum InitSteps
        {
            Init_DataRecover = 0,

            Init_CheckPalllet,

            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,

            // 等待人工操作完成
            Auto_WaitManualOperateFinish,
            // 等待取放完成
            Auto_WaitPickPlaceFinish,

            Auto_WorkEnd,
        }

        private enum ModDef
        {

        }

        private enum MsgID
        {
            Start = ModuleMsgID.ManualOperateMsgStartID,
            HasRequireOnload,
            HasRequireOffload,
        }
        
        #endregion

        #region // 字段，属性

        #region // IO

        private int[] IPalletKeepFlat;        // 夹具放平检测
        private int IPalletHasCheck;          // 夹具位有夹具检测
        private int IOnloadButton;            // 人工上夹具确认
        private int IOffloadButton;           // 人工下夹具确认

        private int OOnloadButtonLed;         // 人工上夹具确认Led
        private int OOffloadButtonLed;        // 人工下夹具确认Led
        private int OManualOperateAlarm;      // 人工操作台报警

        #endregion

        #region // 电机
        #endregion

        #region // ModuleEx.cfg配置
        #endregion

        #region // 模组参数

        private bool onloadTest;              // 上夹具模拟测试参数
        private bool offloadTest;             // 下夹具模拟测试参数

        #endregion

        #region // 模组数据

        private EventList operateEvent;       // 当前操作事件

        #endregion

        #endregion

        public RunProcessManualOperate(int runId) : base(runId)
        {
            InitBatteryPalletSize(0, (int)ModuleMaxPallet.ManualOperate);

            PowerUpRestart();

            InitParameter();
            // 参数
            if (Def.IsNoHardware())
            {
                InsertVoidParameter("onloadTest", "上夹具模拟", "上夹具模拟测试参数：TRUE测试，FALSE禁用", onloadTest, RecordType.RECORD_BOOL, ParameterLevel.PL_ALL_OPER);
                InsertVoidParameter("offloadTest", "下夹具模拟", "下夹具模拟测试参数：TRUE测试，FALSE禁用", offloadTest, RecordType.RECORD_BOOL, ParameterLevel.PL_ALL_OPER);
            }
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
                        CurMsgStr("数据恢复", "Data recover");
                        if(MachineCtrl.GetInstance().DataRecover)
                        {
                            LoadRunData();
                        }
                        this.nextInitStep = InitSteps.Init_CheckPalllet;
                        break;
                    }
                case InitSteps.Init_CheckPalllet:
                    {
                        CurMsgStr("检查夹具状态", "Check sensor");
                        if (PalletKeepFlat(0, (this.Pallet[0].State != PalletStatus.Invalid), true))
                        {
                            this.nextInitStep = InitSteps.Init_End;
                        }
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
                Sleep(10);
            }

            switch((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");

                        // 调度机器人响应
                        for(EventList i = EventList.ManualPlaceNGEmptyPallet; i < EventList.ManualPickPlaceEnd; i++)
                        {
                            if(EventStatus.Response == GetEvent(this, i))
                            {
                                this.operateEvent = i;
                                this.nextAutoStep = AutoSteps.Auto_WaitPickPlaceFinish;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                return;
                            }
                        }

                    // 人工操作请求
                    if(((this.onloadTest&&Def.IsNoHardware())||!InputState(IOnloadButton , false))
                        ||((this.offloadTest&&Def.IsNoHardware())||!InputState(IOffloadButton , false))) {
              
                            this.nextAutoStep = AutoSteps.Auto_WaitManualOperateFinish;
                            SaveRunData(SaveType.AutoStep);
                            break;
                        }

                        if(((this.offloadTest&&Def.IsNoHardware()) || (PalletStatus.Invalid != this.Pallet[0].State))
                            && (!Def.IsNoHardware() && PalletKeepFlat(0, false, false)))
                        {
                            this.offloadTest = false;
                            this.Pallet[0].Release();
                            SaveRunData(SaveType.Pallet);
                        }

                        break;
                    }
                case AutoSteps.Auto_WaitPickPlaceFinish:
                    {
                        CurMsgStr("等待取放操作完成", "wait pick or place pallet finish");
                        int pos = -1;
                        if(EventStatus.Response == GetEvent(this, this.operateEvent, ref pos))
                        {
                            OutputAction(OManualOperateAlarm, true);
                            if (PalletKeepFlat(0,(EventList.ManualPickEmptyPallet == this.operateEvent), true))
                            {
                                SetEvent(this, this.operateEvent, EventStatus.Ready, pos);
                            }
                        }
                        else if(EventStatus.Finished == GetEvent(this, this.operateEvent, ref pos))
                        {
                            OutputAction(OOnloadButtonLed, false);
                            OutputAction(OOffloadButtonLed, false);
                            OutputAction(OManualOperateAlarm, false);
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            
                        SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                            break;
                        }
                        break;
                    }
                case AutoSteps.Auto_WaitManualOperateFinish:
                    {
                        CurMsgStr("等待人工操作请求", "wait manual operate finish");
                    if((this.onloadTest&&Def.IsNoHardware())||! ManualButton(IOnloadButton , false)) 
                        {
                        EventStatus state = GetEvent(this, EventList.ManualPlaceNGEmptyPallet);
                        if((EventStatus.Invalid!=state)&&(EventStatus.Finished!=state)) {
                            OutputAction(OManualOperateAlarm , true);
                            ShowMessageBox((int)MsgID.HasRequireOffload , "已经有请求下夹具信号事件，不能触发上夹具请求" , "等待下夹具完成后再请求" , MessageType.MsgWarning);
                            return;
                        }
                        OutputAction(OOnloadButtonLed , true);
                        OutputAction(OManualOperateAlarm , true);
                        state=GetEvent(this , EventList.ManualPickEmptyPallet);
                        if((EventStatus.Invalid==state)||(EventStatus.Finished==state)||(EventStatus.Require==state)) {
                            if(PalletKeepFlat(0 , true , true)) {
                                this.Pallet[0].State=PalletStatus.OK;
                                this.Pallet[0].SetRowCol(MachineCtrl.GetInstance().PalletMaxRow , MachineCtrl.GetInstance().PalletMaxCol);
                                if(SetEvent(this , EventList.ManualPickEmptyPallet , EventStatus.Require , 0)) {
                                    this.onloadTest=false;
                                    this.nextAutoStep=AutoSteps.Auto_WorkEnd;
                                    SaveRunData(SaveType.AutoStep|SaveType.Pallet);
                                    break;
                                }
                            }
                        }
                    } else if((this.offloadTest&&Def.IsNoHardware())||! ManualButton(IOffloadButton , false)) {
                        EventStatus state = GetEvent(this, EventList.ManualPickEmptyPallet);
                        if((EventStatus.Invalid!=state)&&(EventStatus.Finished!=state)) {
                            OutputAction(OManualOperateAlarm , true);
                            ShowMessageBox((int)MsgID.HasRequireOnload , "已经有请求上夹具信号事件，不能触发下夹具请求" , "等待上夹具完成后再请求" , MessageType.MsgWarning);
                            return;
                        }
                        OutputAction(OOffloadButtonLed , true);
                        OutputAction(OManualOperateAlarm , true);
                        state=GetEvent(this , EventList.ManualPlaceNGEmptyPallet);
                        if((EventStatus.Invalid==state)||(EventStatus.Finished==state)||(EventStatus.Require==state )||(EventStatus.Response==state&&Def.IsNoHardware()&&offloadTest)) {
                            if(PalletKeepFlat(0 , false , true)) {
                                this.Pallet[0].Release();
                                if(SetEvent(this , EventList.ManualPlaceNGEmptyPallet , EventStatus.Require , 0)) {
                                    this.offloadTest=false;
                                    this.nextAutoStep=AutoSteps.Auto_WorkEnd;
                                    SaveRunData(SaveType.AutoStep|SaveType.Pallet);
                                    break;
                                }
                            }
                        }
                    } else {
                        OutputAction(OManualOperateAlarm , false);
                        this.nextAutoStep=AutoSteps.Auto_WorkEnd;
                        break;
                    }
                        break;
                    }

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

        #region // 运行数据读写

        public override void InitRunData()
        {
            this.operateEvent = EventList.Invalid;

            base.InitRunData();
        }

        public override void LoadRunData()
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);

            this.operateEvent = (EventList)iniStream.ReadInt(section, "operateEvent", (int)this.operateEvent);

            base.LoadRunData();
        }

        public override void SaveRunData(SaveType saveType, int index = -1)
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);

            if(SaveType.Variables == (SaveType.Variables & saveType))
            {
                iniStream.WriteInt(section, "operateEvent", (int)this.operateEvent);
            }

            base.SaveRunData(saveType, index);
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
            
            return true;
        }

        /// <summary>
        /// 初始化通用组参数 + 模组参数
        /// </summary>
        protected override void InitParameter()
        {
            this.onloadTest = false;
            this.offloadTest = false;

            base.InitParameter();
        }

        /// <summary>
        /// 读取通用组参数 + 模组参数
        /// </summary>
        /// <returns></returns>
        public override bool ReadParameter()
        {
            this.onloadTest = ReadBoolParameter(this.RunModule, "onloadTest", this.onloadTest);
            this.offloadTest = ReadBoolParameter(this.RunModule, "offloadTest", this.offloadTest);

            return base.ReadParameter();
        }

        #endregion

        #region // IO及电机

        /// <summary>
        /// 初始化IO及电机
        /// </summary>
        protected override void InitModuleIOMotor()
        {
            this.IPalletKeepFlat = new int[2];
            for (int i = 0; i < this.IPalletKeepFlat.Length; i++)
			{
                this.IPalletKeepFlat[i] = AddInput("IPalletKeepFlat" + i);
            }
            this.IPalletHasCheck = AddInput("IPalletHasCheck");
            this.IOnloadButton = AddInput("IOnloadButton");
            this.IOffloadButton = AddInput("IOffloadButton");

            this.OOnloadButtonLed = AddOutput("OOnloadButtonLed");
            this.OOffloadButtonLed = AddOutput("OOffloadButtonLed");
            this.OManualOperateAlarm = AddOutput("OManualOperateAlarm");
        }

        /// <summary>
        /// 夹具放平检测
        /// </summary>
        /// <param name="pltIdx"></param>
        /// <param name="hasPlt"></param>
        /// <param name="alarm"></param>
        /// <returns></returns>
        public override bool PalletKeepFlat(int pltIdx, bool hasPlt, bool alarm = true)
        {
            if(pltIdx < 0 || pltIdx >= (int)ModuleMaxPallet.ManualOperate)
            {
                return false;
            }
            if (!InputState(IPalletHasCheck, hasPlt))
            {
                if (alarm)
                {
                    CheckInputState(IPalletHasCheck, hasPlt);
                }
                return false;
            }
            for(int i = 0; i < IPalletKeepFlat.Length; i++)
            {
                if(!InputState(IPalletKeepFlat[i], hasPlt))
                {
                    if(alarm)
                    {
                        CheckInputState(IPalletKeepFlat[i], hasPlt);
                    }
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 人工确认按钮操作状态
        /// </summary>
        /// <param name="isOn"></param>
        /// <returns></returns>
        private bool ManualButton(int buttonIdx, bool isOn)
        {
            if(Def.IsNoHardware())
            {
                return true;
            }
            if(InputState(buttonIdx, isOn))
            {
                for(int i = 0; i < 5; i++)
                {
                    if(!InputState(buttonIdx, isOn))
                    {
                        return false;
                    }
                    Sleep(200);
                }
                return true;
            }
            return false;
        }

        #endregion
        
        #region // 添加删除夹具

        public override void ManualAddPallet(int pltIdx, int maxRow, int maxCol, PalletStatus pltState, BatteryStatus batState)
        {
            if (PalletStatus.OK == pltState)
            {
                this.Pallet[pltIdx].State = PalletStatus.OK;
                this.Pallet[pltIdx].SetRowCol(maxRow, maxCol);
                SaveRunData(SaveType.Pallet, pltIdx);
            }
        }

        public override void ManualClearPallet(int pltIdx)
        {
            for(EventList i = EventList.ManualPlaceNGEmptyPallet; i < EventList.ManualPickPlaceEnd; i++)
            {
                if((EventStatus.Response == GetEvent(this, i)) || (EventStatus.Ready == GetEvent(this, i)) || (EventStatus.Start == GetEvent(this, i)))
                {
                    ShowMsgBox.ShowDialog($"调度机器人已响应到{RunName}取放夹具信号，不能清除夹具", MessageType.MsgWarning);
                    return;
                }
            }
            for(EventList i = EventList.ManualPlaceNGEmptyPallet; i < EventList.ManualPickPlaceEnd; i++)
            {
                if(EventStatus.Require == GetEvent(this, i))
                {
                    SetEvent(this, i, EventStatus.Invalid);
                }
            }
            this.nextAutoStep = AutoSteps.Auto_WaitWorkStart;
            this.Pallet[pltIdx].Release();
            SaveRunData(SaveType.AutoStep | SaveType.Pallet, pltIdx);
        }
        #endregion

        #region // 模组信号重置

        /// <summary>
        /// 模组信号重置
        /// </summary>
        public override void ResetModuleEvent()
        {
            bool needSave = false;
            for(EventList i = EventList.Invalid; i < EventList.EventEnd; i++)
            {
                if(this.moduleEvent.ContainsKey(i))
                {
                    SetEvent(this, i, EventStatus.Invalid);
                    needSave = true;
                }
            }
            if(needSave)
            {
                OutputAction(OOnloadButtonLed, false);
                OutputAction(OOffloadButtonLed, false);
                this.nextAutoStep = AutoSteps.Auto_WaitWorkStart;
                SaveRunData(SaveType.AutoStep);
            }
        }
        #endregion
    }
}
