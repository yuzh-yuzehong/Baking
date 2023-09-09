using HelperLibrary;
using System;
using System.Diagnostics;

namespace Machine
{
    /// <summary>
    /// 上假电池
    /// </summary>
    class RunProcessOnloadFake : RunProcess
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

            // 接收电池
            Auto_RecvBatttery,
            // 传递到缓存位
            Auto_SendToBufferPos,
            // 传递到取料位
            Auto_SendToPickPos,

            Auto_WorkEnd,
        }

        private enum ModDef
        {
            PickPos_0 = 0,
            PickPos_1,
            PickPos_2,
            PickPos_3,
            PickPos_ALL,
            // 缓存行：从取料位开始0
            Buffer_Col_0 = PickPos_ALL,
            Buffer_Col_1 = Buffer_Col_0 + PickPos_ALL,
            Buffer_Col_2 = Buffer_Col_1 + PickPos_ALL,
            PickPos_Buffer_ALL = Buffer_Col_2 + PickPos_ALL,
            
        }

        private enum MsgID
        {
            Start = ModuleMsgID.OnloadFakeMsgStartID,
            SendBufPosTimeout,
            SendPickPosTimeout,
        }

        #endregion

        #region // 字段，属性

        #region // IO

        private int[] IPlacePosInpos;       // 放料位到位
        private int[] IPickPosInpos;        // 取料位到位
        private int IPickPosEnter;          // 取料位进入
        private int IBufferPosSafe;         // 放料位安全-假电池已分离
        private int IBufferPosLeave;        // 缓存位离开-进入取料位
        private int IManualButton;          // 人工确认

        private int OManualButtonLED;       // 人工确认LED指示灯
        private int OFakeLineAlarm;         // 假电池线报警
        private int OFakePickMotor;         // 假电池取料线输送电机
        private int OFakeBufferMotor;       // 假电池缓存线输送电机

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

        public RunProcessOnloadFake(int runId) : base(runId)
        {
            InitBatteryPalletSize((int)ModDef.PickPos_Buffer_ALL, 0);

            PowerUpRestart();

            InitParameter();
            // 参数
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
                        if(!CheckInputState(IPickPosEnter, false))
                        {
                            return;
                        }
                        for(int i = 0; i < (int)ModDef.PickPos_ALL; i++)
                        {
                            if(InputState(IPickPosInpos[i], true))
                            {
                                this.Battery[i].Type = BatteryStatus.Fake;
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
                Sleep(10);
            }

            switch((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");
                        // 取料位有，发送取电池信号
                        EventStatus state = GetEvent(this, EventList.OnloadFakePickBattery);
                        if(!PickPosIsEmpty() && ((EventStatus.Invalid == state) || (EventStatus.Finished == state)))
                        {
                            SetEvent(this, EventList.OnloadFakePickBattery, EventStatus.Require);
                        }
                        // 取料位有，已响应
                        else if(!PickPosIsEmpty() && (EventStatus.Response == state) && PickPosSenserInpos())
                        {
                            if (CheckInputState(IPickPosEnter, false))
                            {
                                OutputAction(OFakePickMotor, false);
                                SetEvent(this, EventList.OnloadFakePickBattery, EventStatus.Ready);
                            }
                        }
                        // 有取消信号，置无效信号
                        else if(EventStatus.Cancel == state)
                        {
                            SetEvent(this, EventList.OnloadFakePickBattery, EventStatus.Invalid);
                        }

                        // 缓存位有，取料位无，流入到缓存位
                        if(PickPosIsEmpty() && BufferPosIsEmpty(ModDef.Buffer_Col_0)
                            && (!BufferPosIsEmpty(ModDef.Buffer_Col_1) || !BufferPosIsEmpty(ModDef.Buffer_Col_2)))
                        {
                            this.nextAutoStep = AutoSteps.Auto_SendToBufferPos;
                            SaveRunData(SaveType.AutoStep);
                            break;
                        }
                        // 缓存位有，取料位无，流入到取料位
                        if(PickPosIsEmpty() && !BufferPosIsEmpty(ModDef.Buffer_Col_0) 
                            && ((EventStatus.Invalid == state) || (EventStatus.Finished == state)))
                        {
                            this.nextAutoStep = AutoSteps.Auto_SendToPickPos;
                            SaveRunData(SaveType.AutoStep);
                            break;
                        }
                        // 缓存位无，取料位无，请求入料
                        OutputAction(OFakeLineAlarm, PickPosIsEmpty());
                        // 有人工确认上料信号
                        if(ManualButton(true) && !BufferPosIsFull(ModDef.Buffer_Col_2))
                        {
                            this.nextAutoStep = AutoSteps.Auto_RecvBatttery;
                            break;
                        }
                        OutputAction(OManualButtonLED, false);
                        break;
                    }
                case AutoSteps.Auto_RecvBatttery:
                    {
                        CurMsgStr("接收电池", "Recv Battery");
                        OutputAction(OFakeLineAlarm, false);
                        OutputAction(OFakeBufferMotor, false);
                        bool hasBat = false;
                        for(int i = 0; i < IPlacePosInpos.Length; i++)
                        {
                            if (InputState(IPlacePosInpos[i], true))
                            {
                                this.Battery[(int)ModDef.Buffer_Col_2 + i].Type = BatteryStatus.Fake;
                                hasBat = true;
                            }
                        }
                        hasBat = hasBat && BufferPosIsEmpty(ModDef.Buffer_Col_0);
                        this.nextAutoStep = hasBat ? AutoSteps.Auto_SendToBufferPos : AutoSteps.Auto_WorkEnd;
                        SaveRunData(SaveType.AutoStep | SaveType.Battery);
                        break;
                    }
                case AutoSteps.Auto_SendToBufferPos:
                    {
                        CurMsgStr("传递电池至缓存位", "Send battery to buffer pos");
                        OutputAction(OFakeLineAlarm, false);
                        OutputAction(OFakePickMotor, false);
                        OutputAction(OFakeBufferMotor, true);
                        bool inpos = false;
                        #region // 传递电池
                        DateTime dt = DateTime.Now;
                        while(true)
                        {
                            if (BufferPosIsEmpty(ModDef.Buffer_Col_1))
                            {
                                for(int i = 0; i < (int)ModDef.PickPos_ALL; i++)
                                {
                                    inpos = false;
                                    if(!InputState(IPlacePosInpos[i], false))
                                    {
                                        break;
                                    }
                                    inpos = true;
                                }
                                if(inpos && InputState(IBufferPosSafe, true))
                                {
                                    for(int i = 0; i < 10; i++)
                                    {
                                        if(InputState(IBufferPosLeave, true))
                                        {
                                            break;
                                        }
                                        if(!InputState(IBufferPosSafe, false))
                                        {
                                            Sleep(250);
                                        }
                                    }
                                    break;
                                }
                            }
                            if (InputState(IBufferPosLeave, true))
                            {
                                inpos = true;
                                break;
                            }
                            if ((DateTime.Now - dt).TotalSeconds > 10)
                            {
                                inpos = false;
                                break;
                            }
                            Sleep(1);
                        }
                        OutputAction(OFakeBufferMotor, false);
                        #endregion
                        if(inpos)
                        {
                            if (CheckInputState(IBufferPosSafe, false))
                            {
                                // 3行缓存，移动2次
                                for (int row = 0; row < 2; row++)
                                {
                                    for (int i = 0; i < (int)ModDef.PickPos_ALL; i++)
                                    {
                                        int srcIdx = (row + 1) * (int)ModDef.PickPos_ALL + i;
                                        int destIdx = (row + 2) * (int)ModDef.PickPos_ALL + i;
                                        this.Battery[srcIdx].Copy(this.Battery[destIdx]);
                                        this.Battery[destIdx].Release();
                                    }
                                }
                                this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                SaveRunData(SaveType.AutoStep | SaveType.Battery);
                            }
                        }
                        else
                        {
                            ShowMessageBox((int)MsgID.SendBufPosTimeout, "发送电池到缓存位超时", "请检查电池是否到位", MessageType.MsgWarning);
                        }
                        break;
                    }
                case AutoSteps.Auto_SendToPickPos:
                    {
                        CurMsgStr("传递电池至取料位", "Send battery to pick pos");
                        bool inpos = false;
                        DateTime dt = DateTime.Now;
                        #region // 传递至取料位
                        OutputAction(OFakeLineAlarm, false);
                        OutputAction(OFakePickMotor, true);
                        OutputAction(OFakeBufferMotor, true);
                        while(true)
                        {
                            if(InputState(IBufferPosLeave, false))
                            {
                                //OutputAction(OFakeBufferMotor, false);
                            }
                            for(int i = 0; i < (int)ModDef.PickPos_ALL; i++)
                            {
                                inpos = false;
                                bool state = this.Battery[(int)ModDef.Buffer_Col_0 + i].Type > BatteryStatus.Invalid;
                                if(!InputState(IPickPosInpos[i], state))
                                {
                                    break;
                                }
                                inpos = true;
                            }
                            if(inpos)
                            {
                                break;
                            }
                            if((DateTime.Now - dt).TotalSeconds > 10)
                            {
                                inpos = false;
                                break;
                            }
                            Sleep(1);
                        }
                        OutputAction(OFakePickMotor, false);
                        OutputAction(OFakeBufferMotor, false);
                        #endregion
                        if(inpos)
                        {
                            if(CheckInputState(IPickPosEnter, false))
                            {
                                #region // 传递缓存位电池到位
                                if(!BufferPosIsEmpty(ModDef.Buffer_Col_1))
                                {
                                    OutputAction(OFakeBufferMotor, true);
                                    dt = DateTime.Now;
                                    while(true)
                                    {
                                        if(InputState(IBufferPosLeave, true))
                                        {
                                            break;
                                        }
                                        if((DateTime.Now - dt).TotalSeconds > 3)
                                        {
                                            OutputAction(OFakeBufferMotor, false);
                                            CheckInputState(IBufferPosLeave, true);
                                            return;
                                        }
                                        Sleep(1);
                                    }
                                    OutputAction(OFakeBufferMotor, false);
                                }
                                #endregion

                                // 3行缓存 + 1取料，移动3次
                                for(int row = 0; row < 3; row++)
                                {
                                    for(int i = 0; i < (int)ModDef.PickPos_ALL; i++)
                                    {
                                        int srcIdx = row * (int)ModDef.PickPos_ALL + i;
                                        int destIdx = (row + 1) * (int)ModDef.PickPos_ALL + i;
                                        this.Battery[srcIdx].Copy(this.Battery[destIdx]);
                                        this.Battery[destIdx].Release();
                                    }
                                }
                                this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                SaveRunData(SaveType.AutoStep | SaveType.Battery);
                                break;
                            }
                        }
                        else
                        {
                            for(int i = 0; i < (int)ModDef.PickPos_ALL; i++)
                            {
                                bool state = this.Battery[(int)ModDef.Buffer_Col_0 + i].Type > BatteryStatus.Invalid;
                                if(!CheckInputState(IPickPosInpos[i], state))
                                {
                                    break;
                                }
                            }
                            ShowMessageBox((int)MsgID.SendPickPosTimeout, "发送电池到取料位超时", "请检查电池是否到位", MessageType.MsgWarning);
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

        #region // 模组配置及参数

        protected override void InitParameter()
        {

            base.InitParameter();
        }

        /// <summary>
        /// 读取通用组参数 + 模组参数
        /// </summary>
        /// <returns></returns>
        public override bool ReadParameter()
        {

            return base.ReadParameter();
        }
        #endregion

        #region // IO及电机

        /// <summary>
        /// 初始化模组IO及电机
        /// </summary>
        protected override void InitModuleIOMotor()
        {
            int maxPos = (int)ModDef.PickPos_ALL;
            this.IPickPosInpos = new int[maxPos];
            for (int i = 0; i < maxPos; i++)
			{
                this.IPickPosInpos[i] = AddInput("IPickPosInpos" + i);
            }
            this.IPickPosEnter = AddInput("IPickPosEnter");
            this.IPlacePosInpos = new int[maxPos];
            for(int i = 0; i < maxPos; i++)
            {
                this.IPlacePosInpos[i] = AddInput("IPlacePosInpos" + i);
            }
            this.IBufferPosSafe = AddInput("IBufferPosSafe");
            this.IBufferPosLeave = AddInput("IBufferPosLeave");
            this.IManualButton = AddInput("IManualButton");

            this.OManualButtonLED = AddOutput("OManualButtonLED");
            this.OFakeLineAlarm = AddOutput("OFakeLineAlarm");
            this.OFakePickMotor = AddOutput("OFakePickMotor");
            this.OFakeBufferMotor = AddOutput("OFakeBufferMotor");
        }

        /// <summary>
        /// 电池感应器到位
        /// </summary>
        /// <returns></returns>
        public bool PickPosSenserInpos(bool alm = false)
        {
            if(InputState(IPickPosEnter, false))
            {
                for(int i = 0; i < (int)ModDef.PickPos_ALL; i++)
                {
                    bool hasBat = this.Battery[i].Type > BatteryStatus.Invalid;
                    if(!InputState(IPickPosInpos[i], hasBat))
                    {
                        if(alm) CheckInputState(IPickPosInpos[i], hasBat);
                        return false;
                    }
                }
                return true;
            }
            if(alm) CheckInputState(IPickPosEnter, false);
            return false;
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
                return true;
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
                OutputAction(this.OManualButtonLED, isOn);
                return true;
            }
            return false;
        }

        #endregion

        #region // 电池数据

        /// <summary>
        /// 取料位电池为满
        /// </summary>
        /// <returns></returns>
        private bool PickPosIsFull()
        {
            for(int i = 0; i < (int)ModDef.PickPos_ALL; i++)
            {
                if(BatteryStatus.Invalid == this.Battery[i].Type)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 取料位电池为空
        /// </summary>
        /// <returns></returns>
        private bool PickPosIsEmpty()
        {
            for(int i = 0; i < (int)ModDef.PickPos_ALL; i++)
            {
                if(BatteryStatus.Invalid != this.Battery[i].Type)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 缓存位电池为满
        /// </summary>
        /// <returns></returns>
        private bool BufferPosIsFull(ModDef col = ModDef.Buffer_Col_0)
        {
            int start = (int)col;
            int end = (int)col + (int)ModDef.PickPos_ALL;
            for(int i = start; i < end; i++)
            {
                if(BatteryStatus.Invalid == this.Battery[i].Type)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 缓存位电池为空
        /// </summary>
        /// <returns></returns>
        private bool BufferPosIsEmpty(ModDef col = ModDef.Buffer_Col_0)
        {
            int start = (int)col;
            int end = (int)col + (int)ModDef.PickPos_ALL;
            for(int i = start; i < end; i++)
            {
                if(BatteryStatus.Invalid != this.Battery[i].Type)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 计算取假电池列：固定首行取
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public bool GetPickFakeRow(ref int row)
        {
            for(ModDef i = ModDef.PickPos_0; i < ModDef.PickPos_ALL; i++)
            {
                if(BatteryStatus.Fake == this.Battery[(int)i].Type)
                {
                    row = (int)i;
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region // 防呆检查

        /// <summary>
        /// 模组防呆监视：请勿加入耗时过长或阻塞操作
        /// </summary>
        public override void MonitorAvoidDie()
        {
            if (InputState(IManualButton, true))
            {
                OutputAction(OFakeLineAlarm, false);
            }
        }
        
        #endregion

    }
}
