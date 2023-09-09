using System.Diagnostics;
using SystemControlLibrary;
using System;
using HelperLibrary;

namespace Machine
{
    /// <summary>
    /// 待测电池输出
    /// </summary>
    class RunProcessOffloadDetectFake : RunProcess
    {
        #region // 枚举：步骤，模组数据，报警列表
        
        protected new enum InitSteps
        {
            Init_DataRecover = 0,
            Init_CheckBattery,
            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,
            
            // 传递到缓存位
            Auto_SendToBufferPos,
            // 传递到手动取料位
            Auto_SendToManualPos,

            Auto_WorkEnd,
        }

        private enum ModDef
        {
            PlacePos_0 = 0,
            PlacePos_ALL,
        }

        private enum MsgID
        {
            Start = ModuleMsgID.OffloadDetectFakeMsgStartID,
            BufferFull,
            SendTimeout,
        }
        #endregion

        #region // 字段，属性

        #region // IO

        private int IPlacePosSafe;              // 放料位安全
        private int IPlacePosOut;               // 放料位出
        private int IBufferPosInpos;            // 缓存位到位
        private int IManualButton;              // 人工确认

        private int OManualButtonLED;           // 人工确认LED指示灯
        private int OFakeLineAlarm;             // 假电池线报警
        private int OFrontMotor;                // 假电池前端电机
        private int OAfterMotor;                // 假电池后端电机

        #endregion

        #region // 电机
        #endregion

        #region // ModuleEx.cfg配置
        #endregion

        #region // 模组参数
        #endregion

        #region // 模组数据
        #endregion

        #endregion

        public RunProcessOffloadDetectFake(int RunID) : base(RunID)
        {
            InitBatteryPalletSize((int)ModDef.PlacePos_ALL, 0);

            PowerUpRestart();

            InitParameter();
        }

        #region // 模组操作

        protected override void PowerUpRestart()
        {
            base.PowerUpRestart();
            CurMsgStr("准备好", "Ready");

            InitRunData();
        }

        protected override void InitOperation()
        {
            if (!IsModuleEnable())
            {
                InitFinished();
                return;
            }

            switch ((InitSteps)this.nextInitStep)
            {
                case InitSteps.Init_DataRecover:
                    {
                        CurMsgStr("数据恢复", "Data recover");
                        if (MachineCtrl.GetInstance().DataRecover)
                        {
                            LoadRunData();
                        }
                        this.nextInitStep = InitSteps.Init_CheckBattery;
                        break;
                    }
                case InitSteps.Init_CheckBattery:
                    {
                        CurMsgStr("检查电池状态", "Check battery state");

                        if(!PlaceSenserIsSafe())
                        {
                            for(int i = 0; i < (int)ModDef.PlacePos_ALL; i++)
                            {
                                this.Battery[i].Type = BatteryStatus.NG;
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
            if (!IsModuleEnable())
            {
                CurMsgStr("模组禁用", "Moudle not enable");
                Sleep(100);
                return;
            }

            if (Def.IsNoHardware())
            {
                Sleep(1000);
            }

            switch ((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");
                        // 有空位，发送放电池信号
                        EventStatus state = GetEvent(this, EventList.OffLoadPlaceDetectBattery);
                        if (!PlacePosIsFull() && ((EventStatus.Invalid == state) || (EventStatus.Finished == state)))
                        {
                            SetEvent(this, EventList.OffLoadPlaceDetectBattery, EventStatus.Require);
                        }
                        // 有空位，已响应
                        else if((EventStatus.Response == state) && !PlacePosIsFull())
                        {
                            if(PlaceSenserIsSafe(true))
                            {
                                OutputAction(OFrontMotor, false);
                                OutputAction(OAfterMotor, false);
                                SetEvent(this, EventList.OffLoadPlaceDetectBattery, EventStatus.Ready);                               
                            }
                        }
                        // 放料位满 || 无法放 || 人工请求下，流入缓存位
                        if(BufferPosSensorState(false)
                           && (((EventStatus.Ready != state) && (EventStatus.Start != state))
                               || (EventStatus.Cancel == state)))
                        {
                            if(PlacePosIsFull() || (EventStatus.Cancel == state))
                            {
                                this.nextAutoStep = AutoSteps.Auto_SendToBufferPos;
                                break;
                            }
                            else if(/*!PlacePosIsEmpty() &&*/ ManualButton(true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_SendToManualPos;
                                break;
                            }
                        }
                        // 缓存位满，请求取料
                        OutputAction(OFakeLineAlarm, BufferPosSensorState(true));
                        OutputAction(OManualButtonLED, false);
                        break;
                    }
                case AutoSteps.Auto_SendToBufferPos:
                    {
                        CurMsgStr("传递到缓存位", "Send battery to buffer pos");                       
                        OutputAction(OFakeLineAlarm, true);
                        OutputAction(OFrontMotor, true);
                        OutputAction(OAfterMotor, true);
                        bool sendFin = false;
                        DateTime time = DateTime.Now;
                        while(true)
                        {
                            if(InputState(IPlacePosSafe, false) && InputState(IPlacePosOut, false))
                            {
                                for(int i = 0; i < (int)ModDef.PlacePos_ALL; i++)
                                {
                                    this.Battery[i].Release();
                                }
                                Sleep(200);
                                sendFin = true;
                                break;
                            }
                            if(!InputState(IBufferPosInpos, false))
                            {
                                OutputAction(OFrontMotor, false);
                                OutputAction(OAfterMotor, false);
                                ShowMessageBox((int)MsgID.BufferFull, "线体已满", "请先拿走线体最后面的电池后再点击【确定】", MessageType.MsgWarning);
                                OutputAction(OFrontMotor, true);
                                OutputAction(OAfterMotor, true);
                                time = DateTime.Now;
                                break;
                            }
                            if((DateTime.Now - time).TotalSeconds > 10)
                            {
                                break;
                            }
                            Sleep(1);
                        }
                        OutputAction(OFakeLineAlarm, false);
                        OutputAction(OFrontMotor, false);
                        OutputAction(OAfterMotor, false);
                        if(!sendFin)
                        {
                            ShowMessageBox((int)MsgID.SendTimeout, "发送电池到缓存位超时", "请检查电池是否到位", MessageType.MsgWarning);
                        }
                        else
                        {
                            if(EventStatus.Cancel == GetEvent(this, EventList.OffLoadPlaceDetectBattery))
                            {
                                SetEvent(this, EventList.OffLoadPlaceDetectBattery, EventStatus.Invalid);
                            }
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep | SaveType.Battery);
                        }
                        break;
                    }
                case AutoSteps.Auto_SendToManualPos:
                    {
                        CurMsgStr("传递到人工取料位", "Send battery to manual pick pos");
                        OutputAction(OFakeLineAlarm, true);
                        OutputAction(OFrontMotor, true);
                        OutputAction(OAfterMotor, true);
                        bool sendFin = false;
                        DateTime time = DateTime.Now;
                        while(true)
                        {
                            if(InputState(IPlacePosSafe, false) && InputState(IPlacePosOut, false))
                            {
                                if(InputState(IBufferPosInpos, true))
                                {
                                    sendFin = true;
                                    break;
                                }
                            }
                            if((DateTime.Now - time).TotalSeconds > 10)
                            {
                                if(InputState(IPlacePosSafe, false) && InputState(IPlacePosOut, false))
                                {
                                    sendFin = true;
                                }
                                break;
                            }
                            if(sendFin)
                            {
                                break;
                            }
                            Sleep(1);
                        }
                        OutputAction(OFakeLineAlarm, false);
                        OutputAction(OFrontMotor, false);
                        OutputAction(OAfterMotor, false);
                        if(!sendFin)
                        {
                            ShowMessageBox((int)MsgID.SendTimeout, "发送电池到人工取电池位超时", "请检查电池是否到位", MessageType.MsgWarning);
                        }
                        if(EventStatus.Cancel == GetEvent(this, EventList.OffLoadPlaceDetectBattery))
                        {
                            SetEvent(this, EventList.OffLoadPlaceDetectBattery, EventStatus.Invalid);
                        }
                        if(InputState(IPlacePosSafe, false) && InputState(IPlacePosOut, false))
                        {
                            for(int i = 0; i < (int)ModDef.PlacePos_ALL; i++)
                            {
                                this.Battery[i].Release();
                            }
                        }
                        this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                        SaveRunData(SaveType.AutoStep | SaveType.Battery);
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

        #region // 模组配置及参数
        public override bool InitializeConfig(string module)
        {
            if (!base.InitializeConfig(module))
            {
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// 初始化通用模组参数
        /// </summary>
        protected override void InitParameter()
        {
            base.InitParameter();
        }

        /// <summary>
        /// 读取通用模组参数
        /// </summary>
        /// <returns></returns>
        public override bool ReadParameter()
        {
            this.OnLoad = ReadBoolParameter(this.RunModule, "OnLoad", true);
            this.OffLoad = ReadBoolParameter(this.RunModule, "OffLoad", false);

            return base.ReadParameter();
        }
        #endregion

        #region // IO及电机

        /// <summary>
        /// 初始化模组IO及电机
        /// </summary>
        protected override void InitModuleIOMotor()
        {
            this.IPlacePosSafe = AddInput("IPlacePosSafe");
            this.IPlacePosOut = AddInput("IPlacePosOut");
            this.IBufferPosInpos = AddInput("IBufferPosInpos");
            this.IManualButton = AddInput("IManualButton");

            this.OManualButtonLED = AddOutput("OManualButtonLED");
            this.OFakeLineAlarm = AddOutput("OFakeLineAlarm");
            this.OFrontMotor = AddOutput("OFrontMotor");
            this.OAfterMotor = AddOutput("OAfterMotor");
        }
        
        /// <summary>
        /// 放料位电池为满
        /// </summary>
        /// <returns></returns>
        public bool PlacePosIsFull()
        {
            if (BatteryStatus.Invalid == this.Battery[(int)ModDef.PlacePos_0].Type)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 放料位电池为空
        /// </summary>
        /// <returns></returns>
        public bool PlacePosIsEmpty()
        {
            if (BatteryStatus.Invalid != this.Battery[(int)ModDef.PlacePos_0].Type)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 人工确认按钮操作状态
        /// </summary>
        /// <param name="isOn"></param>
        /// <returns></returns>
        private bool ManualButton(bool isOn)
        {
            if (Def.IsNoHardware() || this.DryRun)
            {
                return true;
            }
            if (InputState(IManualButton, isOn))
            {
                for (int i = 0; i < 5; i++)
                {
                    if (!InputState(IManualButton, isOn))
                    {
                        return false;
                    }
                    Sleep(200);
                }
                OutputAction(OManualButtonLED, !isOn);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 缓存位感应器状态
        /// </summary>
        /// <returns></returns>
        public bool BufferPosSensorState(bool isOn)
        {
            if(!InputState(IBufferPosInpos, isOn))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 放料位传感器状态安全
        /// </summary>
        /// <returns></returns>
        public bool PlaceSenserIsSafe(bool alm = false)
        {
            if(!InputState(IPlacePosSafe, false) || !InputState(IPlacePosOut, false))
            {
                if(alm)
                {
                    CheckInputState(IPlacePosSafe, false);
                    CheckInputState(IPlacePosOut, false);
                }
                return false;
            }
            return true;
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
            /*RunProcessOffloadBattery runOffLoadBat = MachineCtrl.GetInstance().GetModule(RunID.OffloadBattery) as RunProcessOffloadBattery;
            if (null != runOffLoadBat)
            {
                if (!runOffLoadBat.CheckMotorZSafe())
                {
                    string msg = string.Format("下料电机Z轴不在安全位,禁止输出操作！！！");
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
            }*/

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
        public override bool CheckMotorCanMove(Motor motor, int nLocation, float fValue, MotorMoveType moveType)
        {
            return true;
        }

        /// <summary>
        /// 模组防呆监视：请勿加入耗时过长或阻塞操作
        /// </summary>
        public override void MonitorAvoidDie()
        {
        }

        /// <summary>
        /// 设备停止后操作
        /// </summary>
        public override void AfterStopAction()
        {
            this.AutoCheckStep = CheckSteps.Check_WorkStart;
        }

        #endregion
    }
}
