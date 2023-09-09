using HelperLibrary;
using System;
using System.Diagnostics;

namespace Machine
{
    /// <summary>
    /// 来料取料线
    /// </summary>
    class RunProcessOnloadLine : RunProcess
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
            Auto_PositionPush,


            Auto_WorkEnd,
        }

        private enum ModDef
        {
            RecvPos_0 = 0,
            RecvPos_1,
            RecvPos_ALL,

        }

        private enum MsgID
        {
            Start = ModuleMsgID.OnloadLineMsgStartID,
            RecvTimeout,
            TransferTimeout,
            SendTimeout,
            RecvCrash,
        }

        #endregion

        #region // 字段

        #region // IO

        private int IRecvPosEnter;      // 接收位，进入
        private int IRecvRearEnd;       // 防接收追尾感应检测

        private int[] IRecvPosInpos;    // 接收位，到位
        private int[] IPositionPush;    // 定位气缸，推出到位
        private int[] IPositionPull;    // 定位气缸，拉回到位

        private int ORecvPosMotor;      // 接收位电机
        private int[] OPositionPush;    // 定位气缸，推出
        private int[] OPositionPull;    // 定位气缸，拉回

        #endregion

        #region // 电机
        #endregion

        #region // ModuleEx.cfg配置
        #endregion

        #region // 模组参数
        #endregion

        #region // 模组数据

        RunProcessOnloadScan recvBatRun;    // 接收电池模组

        #endregion

        #endregion

        public RunProcessOnloadLine(int runId) : base(runId)
        {
            InitBatteryPalletSize((int)ModDef.RecvPos_ALL, 0);

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

                        for(int i = 0; i < (int)ModDef.RecvPos_ALL; i++)
                        {
                            if (!CheckInputState(IRecvPosInpos[i], (this.Battery[i].Type > BatteryStatus.Invalid)))
                            {
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
                Sleep(50);
            }

            switch((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");
                        // 有，发送请求信号
                        EventStatus state = GetEvent(this, EventList.OnloadLinePickBattery);
                        if(!RecvPosIsEmpty() && ((EventStatus.Invalid == state) || (EventStatus.Finished == state)))
                        {
                            if (RecvRearEnd())
                            {
                                // 电池追尾，停机报警
                                ShowMessageBox((int)MsgID.RecvCrash, "取料线电池追尾", "请检查取料线上电池是否追尾", MessageType.MsgAlarm);
                            }
                            else
                            {
                                SetEvent(this, EventList.OnloadLinePickBattery, EventStatus.Require);
                            }
                        }
                        // 有，已响应
                        else if (!RecvPosIsEmpty() && (EventStatus.Response == state))
                        {
                            if (RecvPosSenserInpos() && PositionPush(false))
                            {
                                SetEvent(this, EventList.OnloadLinePickBattery, EventStatus.Ready);
                            }
                        }

                        // 取料位无，响应入料
                        if(((EventStatus.Invalid == state) || (EventStatus.Finished == state)) && RecvPosIsEmpty())
                        {
                            if(null != this.recvBatRun)
                            {
                                state = GetEvent(this.recvBatRun, EventList.OnloadScanSendBattery);
                                if(EventStatus.Require == state)
                                {
                                    this.nextAutoStep = AutoSteps.Auto_RecvBatttery;
                                    SaveRunData(SaveType.AutoStep);
                                    break;
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_RecvBatttery:
                    {
                        CurMsgStr("接收电池", "Recv Battery");
                        if((null != this.recvBatRun) && PositionPush(false))
                        {
                            bool recvFin = false;
                            bool rearEndFlag = false;
                            EventStatus state = GetEvent(this.recvBatRun, EventList.OnloadScanSendBattery);
                            if(EventStatus.Require == state)
                            {
                                SetEvent(this.recvBatRun , EventList.OnloadScanSendBattery , EventStatus.Response);
                            }
                            else if((EventStatus.Ready == state)||(EventStatus.Start == state))
                            {
                                DateTime time = DateTime.Now;                         
                                while(true)
                                {
                                    OutputAction(ORecvPosMotor , true);
                                    bool inpos = true;
                                    //防追尾 增加
                                    if(RecvRearEnd())
                                    {
                                        if(!rearEndFlag)
                                        {
                                            rearEndFlag=true;
                                            for(int i = 0 ; i<(int)ModDef.RecvPos_ALL ; i++)
                                            {
                                                this.Battery[i].Copy(this.recvBatRun.Battery[i]);
                                                this.recvBatRun.Battery[i].Release();
                                            }
                                            this.recvBatRun.SaveRunData(SaveType.Battery);
                                            SetEvent(this.recvBatRun , EventList.OnloadScanSendBattery , EventStatus.Finished);
                                        }
            
                                    }
                                    for(int i = 0 ; i<(int)ModDef.RecvPos_ALL ; i++)
                                    {
                                        if(!InputState(IRecvPosInpos[i] , true))
                                        {
                                            inpos=false;
                                            break;
                                        }
                                    }
                                    if(inpos)
                                    {
                                        recvFin=true;
                                        break;
                                    }
                                    if(this.DryRun)
                                    {
                                        Sleep(500);
                                        recvFin=true;
                                        break;
                                    }
                                    if((DateTime.Now-time).TotalSeconds>10)
                                    {
                                        recvFin=false;
                                        break;
                                    }
                                    Sleep(1);
                                }
                                OutputAction(ORecvPosMotor , false);
                                if(!recvFin)
                                {
                                    ShowMessageBox((int)MsgID.RecvTimeout , "接收电池超时" , "请检查电池是否接收到位" , MessageType.MsgWarning);
                                }
                            }
                            if(recvFin)
                            {
                                /*[修改]
                                for(int i = 0 ; i<(int)ModDef.RecvPos_ALL ; i++) {
                                    this.Battery[i].Copy(this.recvBatRun.Battery[i]);
                                    this.recvBatRun.Battery[i].Release();
                                }
                            
                                this.recvBatRun.SaveRunData(SaveType.Battery);
                                SetEvent(this.recvBatRun , EventList.OnloadScanSendBattery , EventStatus.Finished);
                                */
                                if(!rearEndFlag)
                                {
                                    for(int i = 0 ; i<(int)ModDef.RecvPos_ALL ; i++)
                                    {
                                        this.Battery[i].Copy(this.recvBatRun.Battery[i]);
                                        this.recvBatRun.Battery[i].Release();
                                    }
                                    this.recvBatRun.SaveRunData(SaveType.Battery);
                                    SetEvent(this.recvBatRun , EventList.OnloadScanSendBattery , EventStatus.Finished);
                                }
                                this.nextAutoStep=AutoSteps.Auto_PositionPush;
                                SaveRunData(SaveType.AutoStep|SaveType.Battery);

                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PositionPush:
                    {
                        CurMsgStr("定位气缸退出", "Position push");
                        if (PositionPush(true))
                        {
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

        #region // 模组配置及参数

        /// <summary>
        /// 初始化通用组参数 + 模组参数
        /// </summary>
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

        /// <summary>
        /// 读取本模组的相关模组
        /// </summary>
        public override void ReadRelatedModule()
        {
            string value;

            // 来料扫码
            value = IniFile.ReadString(this.RunModule, "RecvBatRun", "", Def.GetAbsPathName(Def.ModuleExCfg));
            this.recvBatRun = MachineCtrl.GetInstance().GetModule(value) as RunProcessOnloadScan;
        }

        #endregion

        #region // IO及电机

        /// <summary>
        /// 初始化模组IO及电机
        /// </summary>
        protected override void InitModuleIOMotor()
        {
            int maxPos = (int)ModDef.RecvPos_ALL;
            this.IRecvPosEnter = AddInput("IRecvPosEnter");
            this.IRecvRearEnd=AddInput(nameof(IRecvRearEnd));
            this.IRecvPosInpos = new int[maxPos];
            this.IPositionPush = new int[maxPos];
            this.IPositionPull = new int[maxPos];
            for(int i = 0; i < maxPos; i++)
            {
                this.IRecvPosInpos[i] = AddInput("IRecvPosInpos" + i);
                this.IPositionPush[i] = AddInput("IPositionPush" + i);
                this.IPositionPull[i] = AddInput("IPositionPull" + i);
            }

            this.ORecvPosMotor = AddOutput("ORecvPosMotor");
            this.OPositionPush = new int[maxPos];
            this.OPositionPull = new int[maxPos];
            for(int i = 0; i < maxPos; i++)
            {
                this.OPositionPush[i] = AddOutput("OPositionPush" + i);
                this.OPositionPull[i] = AddOutput("OPositionPull" + i);
            }
        }

        /// <summary>
        /// 发送位感应器到位
        /// </summary>
        /// <returns></returns>
        public bool RecvPosSenserInpos(bool alm = true)
        {
            if (this.DryRun)
            {
                return true;
            }
            bool inpos = true;
            for(int i = 0; i < (int)ModDef.RecvPos_ALL; i++)
            {
                if (!InputState(IRecvPosInpos[i], true))
                {
                    if (!alm || !CheckInputState(IRecvPosInpos[i], true))
                    {
                        inpos = false;
                        break;
                    }
                }
            }
            return inpos;
        }

        /// <summary>
        /// 发送防追尾感应检测
        /// </summary>
        /// <returns> true - 追尾，false - 未追尾 </returns>
        public bool RecvRearEnd()
        {
            if (this.DryRun)
            {
                return false;
            }
            return InputState(IRecvRearEnd, true);
        }

        /// <summary>
        /// 定位气缸推出
        /// </summary>
        /// <param name="push">true推出，false回退</param>
        /// <returns></returns>
        protected bool PositionPush(bool push)
        {
            //if(Def.IsNoHardware())
            {
                return true;
            }
            // 检查IO配置
            for(int i = 0; i < IPositionPush.Length; i++)
            {
                if((IPositionPush[i] < 0) || (IPositionPull[i] < 0)
                    || (OPositionPush[i] < 0 && OPositionPull[i] < 0))
                {
                    return false;
                }
            }
            // 操作
            for(int i = 0; i < IPositionPush.Length; i++)
            {
                OutputAction(OPositionPush[i], push);
                OutputAction(OPositionPull[i], !push);
            }
            // 检查到位
            for(int i = 0; i < IPositionPush.Length; i++)
            {
                // 仅有其一ON时才认为状态正确
                if(!WaitInputState(IPositionPush[i], push) || !WaitInputState(IPositionPull[i], !push))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region // 电池数据

        /// <summary>
        /// 电池位满
        /// </summary>
        /// <returns></returns>
        public bool RecvPosIsFull()
        {
            for(ModDef i = ModDef.RecvPos_0; i < ModDef.RecvPos_ALL; i++)
            {
                if(BatteryStatus.Invalid == this.Battery[(int)i].Type)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 电池位空
        /// </summary>
        /// <returns></returns>
        public bool RecvPosIsEmpty()
        {
            for(ModDef i = ModDef.RecvPos_0; i < ModDef.RecvPos_ALL; i++)
            {
                if(BatteryStatus.Invalid != this.Battery[(int)i].Type)
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

    }
}
