using HelperLibrary;
using System;
using System.Diagnostics;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    /// <summary>
    /// 冷却系统
    /// </summary>
    class RunProcessCoolingSystem : RunProcess
    {
        #region // 枚举：步骤，模组数据，报警列表

        protected new enum InitSteps
        {
            Init_DataRecover = 0,

            Init_MotorRHome,

            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,

            Auto_SendToNextCol,
            Auto_SendEndCheckSensor,

            Auto_WorkEnd,
        }

        private enum ModDef
        {
        }

        private enum MsgID
        {
            Start = ModuleMsgID.CoolingSystemMsgStartID,
            FirstColUnsafe,
            LastColUnsafe,
            OffloadBatteryMotorZUnsafe,
            CoolingOffloadMotorZUnsafe,
            MotorRMoveFail,
            MotorRDelay,
            MotorRTimeout,
        }

        #endregion

        #region // 字段，属性

        #region // IO

        private int IFirstColSafe;          // 首列放位置安全：无电池
        private int IFirstColLeave;         // 首列放位置离开
        private int ILastColInpos;          // 末位取位置到位：有电池
        private int ILastColEnter;          // 末位取位置进入
        private int IMotorRInpos;           // 电机R取放料位到位
        private int IMotorRDelay;           // 电机R防呆到位

        private int OMororRPowerON;         // 电机R上电
        private int OMororRForwardMove;     // 电机R正转
        private int OMororRBackwardMove;    // 电机R反转
        private int[] OAirBlower;           // 风机

        #endregion

        #region // 电机

        private int MotorR;     // 旋转电机R
        #endregion

        #region // ModuleEx.cfg配置

        #endregion

        #region // 模组参数

        private int stationRow;         // 冷却系统最大行
        private int stationCol;         // 冷却系统最大列
        
        #endregion

        #region // 模组数据

        #endregion

        #endregion

        public RunProcessCoolingSystem(int runId) : base(runId)
        {
            InitBatteryPalletSize(0, 0, 1);

            PowerUpRestart();

            InitParameter();
            // 参数
            string key;
            key = string.Format("冷却系统最大行：（0 < X ≤ {0}）", (int)BatteryLineRowCol.MaxRow);
            InsertVoidParameter("stationRow", "冷却系统行", key, stationRow, RecordType.RECORD_INT, ParameterLevel.PL_IDLE_ADMIN);
            key = string.Format("冷却系统最大列：（0 < X ≤ {0}）", (int)BatteryLineRowCol.MaxCol);
            InsertVoidParameter("stationCol", "冷却系统列", key, stationCol, RecordType.RECORD_INT, ParameterLevel.PL_IDLE_ADMIN);

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
                        this.nextInitStep = InitSteps.Init_MotorRHome;
                        break;
                    }
                case InitSteps.Init_MotorRHome:
                    {
                        CurMsgStr("冷却系统电机回零", "Motor R home");
                        if (MotorRHome())
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

                        bool offloadClear = false;

                        #region // 设置检查取放请求

                        // 首行非满请求放
                        EventList placeEvt = EventList.CoolingSystemPlaceBattery;
                        EventStatus placestate = GetEvent(this, placeEvt);
                        if (((EventStatus.Invalid == placestate) || (EventStatus.Finished == placestate)) 
                            && !IsFullCol(0) && !offloadClear && CheckInputState(this.IFirstColLeave, false))
                        {
                            SetEvent(this, placeEvt, EventStatus.Require);
                        }
                        // 末行非空请求取
                        EventList pickEvt = EventList.CoolingSystemPickBattery;
                        EventStatus pickState = GetEvent(this, pickEvt);
                        if (((EventStatus.Invalid == pickState) || (EventStatus.Finished == pickState)) 
                            && !IsEmptyCol(this.BatteryLine.MaxCol - 1) && CheckInputState(this.ILastColEnter, false)
                            && CheckInputState(this.ILastColInpos, true))
                        {
                            SetEvent(this, pickEvt, EventStatus.Require);
                        }
                        #endregion

                        #region // 有取放进行中
                        for(EventList i = EventList.CoolingSystemPlaceBattery; i < EventList.CoolingSystemPickPlaceEnd; i++)
                        {
                            pickState = GetEvent(this, i);
                            if(EventStatus.Response == pickState)
                            {
                                SetEvent(this, i, EventStatus.Ready);
                                return;
                            }
                            else if ((EventStatus.Ready == pickState) || (EventStatus.Start == pickState))
                            {
                                continue;
                            }
                        }
                        #endregion

                        // 首满末空，有电池，移动
                        if(((EventStatus.Invalid == placestate) || (EventStatus.Finished == placestate) 
                            || ((EventStatus.Require == placestate) && offloadClear)) 
                            && ((EventStatus.Invalid == pickState) || (EventStatus.Finished == pickState))
                            && !this.BatteryLine.IsEmpty() && IsEmptyCol(this.BatteryLine.MaxCol - 1)
                            && (IsFullCol(0) || offloadClear))
                        {
                            this.nextAutoStep = AutoSteps.Auto_SendToNextCol;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_SendToNextCol:
                    {
                        CurMsgStr("向后传递电池", "Send battery to next col");
                        if (MotorRMove(true))
                        {
                            for(int col = this.BatteryLine.MaxCol - 1; col > 0; col--)
                            {
                                for(int row = 0; row < this.BatteryLine.MaxRow; row++)
                                {
                                    this.BatteryLine.Battery[row, col].Copy(this.BatteryLine.Battery[row, col - 1]);
                                    this.BatteryLine.Battery[row, col - 1].Release();
                                }
                            }
                            this.nextAutoStep = AutoSteps.Auto_SendEndCheckSensor;
                            SaveRunData(SaveType.AutoStep|SaveType.Battery);
                        }
                        break;
                    }
                case AutoSteps.Auto_SendEndCheckSensor:
                    {
                        CurMsgStr("传递电池后检查感应器", "Send end and check sensor status");
                        if (CheckInputState(IFirstColSafe, false) && CheckInputState(IFirstColLeave, false)
                            && CheckInputState(ILastColEnter, false) && CheckInputState(ILastColInpos, !IsEmptyCol(this.stationCol - 1)))
                        {
                            SetEvent(this, EventList.CoolingSystemPlaceBattery, EventStatus.Invalid);
                            SetEvent(this, EventList.CoolingSystemPickBattery, EventStatus.Invalid);

                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep);
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
        
        /// <summary>
        /// 初始化运行数据
        /// </summary>
        public override void InitRunData()
        {
            if (null != this.BatteryLine)
            {
                this.BatteryLine.Release();
            }

            base.InitRunData();
        }

        /// <summary>
        /// 读取运行数据
        /// </summary>
        public override void LoadRunData()
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);

            key = string.Format("BatteryLine.MaxRow");
            this.BatteryLine.MaxRow = iniStream.ReadInt(section, key, this.BatteryLine.MaxRow);
            key = string.Format("BatteryLine.MaxCol");
            this.BatteryLine.MaxCol = iniStream.ReadInt(section, key, this.BatteryLine.MaxCol);
            for(int row = 0; row < this.BatteryLine.MaxRow; row++)
            {
                for(int col = 0; col < this.BatteryLine.MaxCol; col++)
                {
                    key = string.Format("BatteryLine.Battery[{0}, {1}].Type", row, col);
                    this.BatteryLine.Battery[row, col].Type = (BatteryStatus)iniStream.ReadInt(section, key, (int)this.BatteryLine.Battery[row, col].Type);
                    key = string.Format("BatteryLine.Battery[{0}, {1}].NGType", row, col);
                    this.BatteryLine.Battery[row, col].NGType = (BatteryNGStatus)iniStream.ReadInt(section, key, (int)this.BatteryLine.Battery[row, col].NGType);
                    iniStream.WriteInt(section, key, (int)this.BatteryLine.Battery[row, col].NGType);
                    key = string.Format("BatteryLine.Battery[{0}, {1}].Code", row, col);
                    this.BatteryLine.Battery[row, col].Code = iniStream.ReadString(section, key, this.BatteryLine.Battery[row, col].Code);
                }
            }
            base.LoadRunData();
        }

        /// <summary>
        /// 保存运行数据
        /// </summary>
        /// <param name="saveType"></param>
        /// <param name="index"></param>
        public override void SaveRunData(SaveType saveType, int index = -1)
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);

            if(SaveType.Battery == (SaveType.Battery & saveType))
            {
                key = string.Format("BatteryLine.MaxRow");
                iniStream.WriteInt(section, key, this.BatteryLine.MaxRow);
                key = string.Format("BatteryLine.MaxCol");
                iniStream.WriteInt(section, key, this.BatteryLine.MaxCol);
                for(int row = 0; row < this.BatteryLine.MaxRow; row++)
                {
                    for(int col = 0; col < this.BatteryLine.MaxCol; col++)
                    {
                        key = string.Format("BatteryLine.Battery[{0}, {1}].Type", row, col);
                        iniStream.WriteInt(section, key, (int)this.BatteryLine.Battery[row, col].Type);
                        key = string.Format("BatteryLine.Battery[{0}, {1}].NGType", row, col);
                        iniStream.WriteInt(section, key, (int)this.BatteryLine.Battery[row, col].NGType);
                        key = string.Format("BatteryLine.Battery[{0}, {1}].Code", row, col);
                        iniStream.WriteString(section, key, this.BatteryLine.Battery[row, col].Code);
                    }
                }
            }
            base.SaveRunData(saveType, index);
        }
        
        #endregion

        #region // 模组配置及参数

        /// <summary>
        /// 初始化通用组参数 + 模组参数
        /// </summary>
        protected override void InitParameter()
        {
            this.stationRow = (int)BatteryLineRowCol.MaxRow;
            this.stationCol = (int)BatteryLineRowCol.MaxCol;
        }
        
        /// <summary>
        /// 读取通用组参数 + 模组参数
        /// </summary>
        /// <returns></returns>
        public override bool ReadParameter()
        {
            this.stationRow = ReadIntParameter(this.RunModule, "stationRow", this.stationRow);
            this.stationCol = ReadIntParameter(this.RunModule, "stationCol", this.stationCol);
            if ((this.stationRow > 0) && (this.stationRow <= (int)BatteryLineRowCol.MaxRow)
                && (this.stationCol > 0) && (this.stationCol <= (int)BatteryLineRowCol.MaxCol))
            {
                if((this.stationRow != this.BatteryLine.MaxRow) || (this.stationCol != this.BatteryLine.MaxCol))
                {
                    this.BatteryLine.SetRowCol(stationRow, stationCol);
                }
            }

            return base.ReadParameter();
        }

        #endregion

        #region // IO及电机

        /// <summary>
        /// 初始化IO及电机
        /// </summary>
        protected override void InitModuleIOMotor()
        {
            this.IFirstColSafe = AddInput("IFirstColSafe");
            this.IFirstColLeave = AddInput("IFirstColLeave");
            this.ILastColInpos = AddInput("ILastColInpos");
            this.ILastColEnter = AddInput("ILastColEnter");
            this.IMotorRInpos = AddInput("IMotorRInpos");
            this.IMotorRDelay = AddInput("IMotorRDelay");

            this.OMororRPowerON = AddOutput("OMororRPowerON");
            this.OMororRForwardMove = AddOutput("OMororRForwardMove");
            this.OMororRBackwardMove = AddOutput("OMororRBackwardMove");

            this.OAirBlower = new int[2];
            for(int i = 0; i < 2; i++)
            {
                this.OAirBlower[i] = AddOutput("OAirBlower" + i);
            }
            this.MotorR = AddMotor("MotorR");
        }
        
        #endregion

        #region // 电池状态

        public bool IsEmptyCol(int col)
        {
            for(int row = 0; row < this.BatteryLine.MaxRow; row++)
            {
                if (BatteryStatus.Invalid != this.BatteryLine.Battery[row, col].Type)
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsFullCol(int col)
        {
            // 有连续2个空位，即非满
            for(int row = 0; row < this.BatteryLine.MaxRow - 1; row++)
            {
                if((BatteryStatus.Invalid == this.BatteryLine.Battery[row, col].Type)
                    && (BatteryStatus.Invalid == this.BatteryLine.Battery[row + 1, col].Type))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region // 旋转电机操作

        /// <summary>
        /// 旋转电机回零
        /// </summary>
        /// <returns></returns>
        private bool MotorRHome()
        {
            return true;
        }

        /// <summary>
        /// 旋转电机旋转移动一行
        /// </summary>
        /// <returns></returns>
        public bool MotorRMove(bool forward)
        {
            bool canMove = true;
            bool result = false;
            bool checkMororInpos = false;
            MsgID msgID = MsgID.Start;

            #region // 检查能否旋转移动

            if (!forward && !InputState(this.IFirstColSafe, false))
            {
                canMove = false;
                msgID = MsgID.FirstColUnsafe;
            }
            if (forward && !InputState(this.ILastColInpos, false))
            {
                canMove = false;
                msgID = MsgID.LastColUnsafe;
            }
            if (canMove)
            {
                RunProcess run = MachineCtrl.GetInstance().GetModule(RunID.OffloadBattery);
                if(null != run)
                {
                    if(!((RunProcessOffloadBattery)run).CheckMotorZPos(MotorPosition.OffLoad_SafetyPos))
                    {
                        canMove = false;
                        msgID = MsgID.OffloadBatteryMotorZUnsafe;
                    }
                }
                run = MachineCtrl.GetInstance().GetModule(RunID.CoolingOffload);
                if(null != run)
                {
                    if(!((RunProcessCoolingOffload)run).CheckMotorZPos(MotorPosition.CoolingOffload_SafetyPos))
                    {
                        canMove = false;
                        msgID = MsgID.CoolingOffloadMotorZUnsafe;
                    }
                }
            }
            #endregion

            #region // 旋转移动
            OutputAction(OMororRPowerON, true);
            OutputAction((forward ? OMororRBackwardMove : OMororRForwardMove), false);
            DateTime time = DateTime.Now;
            if(InputState(this.IMotorRInpos, true))
            {
                OutputAction((forward ? OMororRForwardMove : OMororRBackwardMove), true);
                while(canMove)
                {
                    // 电机移动到防呆位
                    if(InputState(this.IMotorRDelay, true))
                    {
                        break;
                    }
                    // 超时
                    if((DateTime.Now - time).TotalSeconds > 10)
                    {
                        msgID = MsgID.MotorRTimeout;
                        canMove = false;
                        break;
                    }
                    Sleep(1);
                }
            }
            time = DateTime.Now;
            while(canMove)
            {
                OutputAction((forward ? OMororRForwardMove : OMororRBackwardMove), true);
                // 开始移动
                if(!checkMororInpos)
                {
                    // 开始
                    if(InputState(this.IMotorRInpos, false) || InputState(this.IMotorRDelay, true))
                    {
                        checkMororInpos = true;
                    }
                    // 超时
                    if((DateTime.Now - time).TotalSeconds > 2)
                    {
                        msgID = MsgID.MotorRMoveFail;
                        break;
                    }
                    Sleep(1);
                    continue;
                }
                // 电机移动到位
                if(InputState(this.IMotorRInpos, true))
                {
                    result = true;
                    break;
                }
                // 电机防呆感应器
                if(InputState(this.IMotorRDelay, true) && ((DateTime.Now - time).TotalSeconds > 5))
                {
                    msgID = MsgID.MotorRDelay;
                    break;
                }
                // 超时
                if((DateTime.Now - time).TotalSeconds > 30)
                {
                    msgID = MsgID.MotorRTimeout;
                    break;
                }
                Sleep(1);
            }
            OutputAction(OMororRForwardMove, false);
            OutputAction(OMororRBackwardMove, false);
            #endregion

            #region // 失败报警
            string msg, dispose;
            switch(msgID)
            {
                case MsgID.FirstColUnsafe:
                    msg = string.Format("冷却系统首行有电池，冷却旋转电机不能开始移动");
                    dispose = string.Format("请在首行无电池后再操作");
                    ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
                    break;
                case MsgID.LastColUnsafe:
                    msg = string.Format("冷却系统最后一行有电池，冷却旋转电机不能开始移动");
                    dispose = string.Format("请在最后一行无电池后再操作");
                    ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
                    break;
                case MsgID.OffloadBatteryMotorZUnsafe:
                    msg = string.Format("冷却上料Z轴电机不在安全位，冷却旋转电机不能开始移动");
                    dispose = string.Format("请人工确认安全后将电机移动至安全位");
                    ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
                    break;
                case MsgID.CoolingOffloadMotorZUnsafe:
                    msg = string.Format("冷却下料Z轴电机不在安全位，冷却旋转电机不能开始移动");
                    dispose = string.Format("请人工确认安全后将电机移动至安全位");
                    ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
                    break;
                case MsgID.MotorRMoveFail:
                    msg = string.Format("电机开始移动超时，还未检测到{0} {1}为ON状态", Inputs(IMotorRDelay).Num, Inputs(IMotorRDelay).Name);
                    dispose = string.Format("请人工检查电机，确认后将电机移动至到位感应器位置");
                    ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
                    break;
                case MsgID.MotorRDelay:
                    msg = string.Format("电机移动时，已触碰{0} {1}为ON状态，但是还未检测到{2} {3}为ON状态"
                        , Inputs(IMotorRDelay).Num, Inputs(IMotorRDelay).Name, Inputs(IMotorRInpos).Num, Inputs(IMotorRInpos).Name);
                    dispose = string.Format("请人工检查电机，确认后将电机移动至到位感应器位置");
                    ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
                    break;
                case MsgID.MotorRTimeout:
                    msg = string.Format("电机移动超时，但是还未检测到{0} {1}为ON状态", Inputs(IMotorRInpos).Num, Inputs(IMotorRInpos).Name);
                    dispose = string.Format("请人工检查电机，确认后将电机移动至到位感应器位置");
                    ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
                    break;
                default:
                    break;
            }
            #endregion
            return result;
        }
        
        #endregion

        #region // 防呆检查

        /// <summary>
        /// 检查输出点位是否可操作
        /// </summary>
        /// <param name="output"></param>
        /// <param name="bOn"></param>
        /// <returns></returns>
        public override bool CheckOutputCanActive(Output output, bool bOn)
        {
            if(((this.OMororRForwardMove > -1) && (Outputs(this.OMororRForwardMove) == output))
                || ((this.OMororRBackwardMove > -1) && (Outputs(this.OMororRBackwardMove) == output)))
            {
                // 禁止手动点动这两个输出，使用移动功能
                ShowMsgBox.ShowDialog("禁止手动点动旋转电机得两个输出，使用调试界面得移动功能", MessageType.MsgWarning);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 模组防呆监视：请勿加入耗时过长或阻塞操作
        /// </summary>
        public override void MonitorAvoidDie()
        {
        }
        
        #endregion

    }
}
