using HelperLibrary;
using System;
using System.Diagnostics;

namespace Machine
{
    /// <summary>
    /// 下料NG输出
    /// </summary>
    class RunProcessOffloadNG : RunProcess
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
            // 传递到人工取料位
            Auto_SendToManualPos,

            Auto_WorkEnd,
        }

        private enum ModDef
        {
            PlacePos_0 = 0,
            PlacePos_1,
            PlacePos_ALL,

        }

        private enum MsgID
        {
            Start = ModuleMsgID.OnloadNGMsgStartID,
            BufferFull,
            SendTimeout,
        }

        #endregion

        #region // 字段，属性

        #region // IO

        private int[] IPlacePosInpos;       // 放料位到位感应器
        private int IPlacePosSafe;          // 放料位安全
        private int IPlacePosOut;           // 放料位出
        private int IBufferPosInpos;        // 缓存位到位
        private int IManualButton;          // 人工确认

        private int OManualButtonLED;       // 人工确认LED指示灯
        private int ONGLineAlarm;           // 假电池线报警
        private int ONGLineMotor;           // 假电池线输送电机
        private int ONGBufferMotor;         // 假电池线缓存线输送电机

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

        public RunProcessOffloadNG(int runId) : base(runId)
        {
            InitBatteryPalletSize((int)ModDef.PlacePos_ALL, 0);

            PowerUpRestart();

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
                        this.nextInitStep = InitSteps.Init_CheckBattery;
                        break;
                    }
                case InitSteps.Init_CheckBattery:
                    {
                        CurMsgStr("检查电池状态", "Check sensor");
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
                        // 有空位，发送放电池信号
                        EventStatus state = GetEvent(this, EventList.OffLoadPlaceNGBattery);
                        if(!PlacePosIsFull() && ((EventStatus.Invalid == state) || (EventStatus.Finished == state)))
                        {
                            SetEvent(this, EventList.OffLoadPlaceNGBattery, EventStatus.Require);
                        }
                        // 有空位，已响应
                        else if((EventStatus.Response == state) && !PlacePosIsFull())
                        {
                            if(PlaceSenserIsSafe(true))
                            {
                                OutputAction(ONGLineMotor, false);
                                OutputAction(ONGBufferMotor, false);
                                SetEvent(this, EventList.OffLoadPlaceNGBattery, EventStatus.Ready);
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
                            else if(/*!PlacePosIsEmpty() && */ManualButton(true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_SendToManualPos;
                                break;
                            }
                        }
                        // 缓存位满，请求取料
                        OutputAction(ONGLineAlarm, BufferPosSensorState(true));
                        OutputAction(OManualButtonLED, false);
                        break;
                    }
                case AutoSteps.Auto_SendToBufferPos:
                    {
                        CurMsgStr("传递到缓存位", "Send battery to buffer pos");
                        OutputAction(ONGLineAlarm, true);
                        OutputAction(ONGLineMotor, true);
                        OutputAction(ONGBufferMotor, true);
                        bool sendFin = false;
                        DateTime time = DateTime.Now;
                        while(true)
                        {
                            if(InputState(IPlacePosSafe, false) && InputState(IPlacePosOut, false))
                            {
                                Sleep(200);
                                if(InputState(IPlacePosSafe, false) && InputState(IPlacePosOut, false))
                                {
                                    for(int i = 0; i < (int)ModDef.PlacePos_ALL; i++)
                                    {
                                        this.Battery[i].Release();
                                    }
                                    sendFin = true;
                                    break;
                                }
                            }
                            if(!InputState(IBufferPosInpos, false))
                            {
                                OutputAction(ONGLineMotor, false);
                                OutputAction(ONGBufferMotor, false);
                                ShowMessageBox((int)MsgID.BufferFull, "线体已满", "请先拿走线体最后面的电池后再点击【确定】", MessageType.MsgWarning);
                                OutputAction(ONGLineMotor, true);
                                OutputAction(ONGBufferMotor, true);
                                time = DateTime.Now;
                                break;
                            }
                            if((DateTime.Now - time).TotalSeconds > 10)
                            {
                                break;
                            }
                            Sleep(1);
                        }
                        OutputAction(ONGLineAlarm, false);
                        OutputAction(ONGLineMotor, false);
                        OutputAction(ONGBufferMotor, false);
                        if(!sendFin)
                        {
                            ShowMessageBox((int)MsgID.SendTimeout, "发送电池到缓存位超时", "请检查电池是否到位", MessageType.MsgWarning);
                        }
                        else
                        {
                            if(EventStatus.Cancel == GetEvent(this, EventList.OffLoadPlaceNGBattery))
                            {
                                SetEvent(this, EventList.OffLoadPlaceNGBattery, EventStatus.Invalid);
                            }
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep | SaveType.Battery);
                        }
                        break;
                    }
                case AutoSteps.Auto_SendToManualPos:
                    {
                        CurMsgStr("传递到人工操作位", "Send battery to manual pos");
                        OutputAction(ONGLineAlarm, true);
                        OutputAction(ONGLineMotor, true);
                        OutputAction(ONGBufferMotor, true);
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
                        OutputAction(ONGLineAlarm, false);
                        OutputAction(ONGLineMotor, false);
                        OutputAction(ONGBufferMotor, false);
                        if(!sendFin)
                        {
                            ShowMessageBox((int)MsgID.SendTimeout, "发送电池到人工取电池位超时", "请检查电池是否到位", MessageType.MsgWarning);
                        }
                        if(EventStatus.Cancel == GetEvent(this, EventList.OffLoadPlaceNGBattery))
                        {
                            SetEvent(this, EventList.OffLoadPlaceNGBattery, EventStatus.Invalid);
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

        #region // IO及电机

        /// <summary>
        /// 初始化模组IO及电机
        /// </summary>
        protected override void InitModuleIOMotor()
        {
            int maxPos = (int)ModDef.PlacePos_ALL;
            this.IPlacePosInpos = new int[maxPos];
            for(int i = 0; i < maxPos; i++)
            {
                this.IPlacePosInpos[i] = AddInput("IPlacePosInpos" + i);
            }
            this.IPlacePosOut = AddInput("IPlacePosOut");
            this.IPlacePosSafe = AddInput("IPlacePosSafe");
            this.IBufferPosInpos = AddInput("IBufferPosInpos");
            this.IManualButton = AddInput("IManualButton");

            this.OManualButtonLED = AddOutput("OManualButtonLED");
            this.ONGLineAlarm = AddOutput("ONGLineAlarm");
            this.ONGLineMotor = AddOutput("ONGLineMotor");
            this.ONGBufferMotor = AddOutput("ONGBufferMotor");
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
        /// 人工确认按钮操作状态
        /// </summary>
        /// <param name="isOn"></param>
        /// <returns></returns>
        private bool ManualButton(bool isOn)
        {
            if(Def.IsNoHardware())
            {
                return false;
            }
            if(InputState(IManualButton, isOn))
            {
                for(int i = 0; i < 5; i++)
                {
                    if(!InputState(IManualButton, isOn))
                    {
                        return false;
                    }
                    Sleep(200);
                }
                OutputAction(OManualButtonLED, isOn);
                return true;
            }
            return false;
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

        /// <summary>
        /// 放料位到位感应器安全状态
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public bool PlacePosInposIsSafe(int row, bool alm = true)
        {
            for(int i = 0; i < (int)ModDef.PlacePos_ALL; i++)
            {
                if(i == row)
                {
                    if(!InputState(this.IPlacePosInpos[i], false))
                    {
                        return CheckInputState(this.IPlacePosInpos[i], false);
                    }
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region // 电池数据

        /// <summary>
        /// 放料位电池为满
        /// </summary>
        /// <returns></returns>
        public bool PlacePosIsFull()
        {
            for(int i = 0; i < (int)ModDef.PlacePos_ALL; i++)
            {
                if(BatteryStatus.Invalid == this.Battery[i].Type)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 放料位电池为空
        /// </summary>
        /// <returns></returns>
        public bool PlacePosIsEmpty()
        {
            for(int i = 0; i < (int)ModDef.PlacePos_ALL; i++)
            {
                if(BatteryStatus.Invalid != this.Battery[i].Type)
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

    }
}
