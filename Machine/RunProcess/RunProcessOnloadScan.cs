using HelperLibrary;
using System;
using System.Diagnostics;
using SystemControlLibrary;

namespace Machine
{
    /// <summary>
    /// 上料扫码：FH01项目用作中转线
    /// </summary>
    class RunProcessOnloadScan : RunProcess
    {
        #region // 枚举：步骤，模组数据，报警列表

        protected new enum InitSteps
        {
            Init_DataRecover = 0,

            Init_CheckBattery,
            Init_ConnectScanner,

            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,
            
            // 接收电池
            Auto_RecvBatttery,
            // 电池扫码
            Auto_ScanBatteryCode,
            // 发送电池
            Auto_SendBatttery,

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
            Start = ModuleMsgID.OnloadScanMsgStartID,
            RecvTimeout,
            SendTimeout,
            ScanCodeFail,
            ScanCodeTimeout,
            CodeLenError,
            CodeTypeError,
            CheckBattery
        }

        #endregion

        #region // 字段，属性

        #region // IO

        private int IRecvPosEnter;      // 接收位，进入
        private int IRecvPosInpos;      // 接收位，到位
        private int IPositionPush;      // 定位气缸，推出到位
        private int IPositionPull;      // 定位气缸，拉回到位

        private int OPositionPush;      // 定位气缸，推出
        private int OPositionPull;      // 定位气缸，拉回
        private int ORecvPosMotor;      // 取料位电机

        #endregion

        #region // 电机
        #endregion

        #region // ModuleEx.cfg配置
        #endregion

        #region // 模组参数

        private bool conveyerLineEN;        // 上工序联机使能：TRUE启用，FALSE禁用
        private bool[] scanEnable;          // 扫码使能：TRUE启用，FALSE禁用
        private string scanCmd;             // 扫码器的扫码指令
        private bool scanLinefeed;          // 扫码器的扫码结束符
        private string[] barcodeScanIP;     // 扫码器的IP：进行网口通讯则填，否则为空
        private int[] barcodeScanCom;       // 扫码器的COM口：进行串口通讯则填，否则为-1
        private int[] barcodeScanPort;      // 扫码器的Port
        private int codeLength;             // 条码长度：-1则不检查
        private string codeType;            // 条码类别：空则不检查，多种类别以英文逗号(,)分隔
        private string scanNGType;          // 扫码NG字符：空则不检查
        private int scanMaxCount;           // 最大扫码次数：（X≥1）
        private int recvDelay;              // 接收电池延时：毫秒ms
        private bool randNGBat;             // 生成随机NG电池

        #endregion

        #region // 模组数据

        RunProcessOnloadRecv recvBatRun;    // 接收电池模组
        RunProcessOnloadLine pickBatRun;    // 取电池模组：PickBatRun = 

        private BarcodeScan[] barcodeScan;  // 扫码器
        private string[] codeTypeArray;     // 条码类型列表

        #endregion

        #endregion

        public RunProcessOnloadScan(int runId) : base(runId)
        {
            InitBatteryPalletSize((int)ModDef.RecvPos_ALL, 0);

            PowerUpRestart();

            InitParameter();
            // 参数
            //InsertVoidParameter("conveyerLineEN", "上工序联机使能", "上工序联机使能：TRUE启用，FALSE禁用", conveyerLineEN, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_ADMIN);
            for(int i = 0; i < this.scanEnable.Length; i++)
            {
                //InsertVoidParameter(("scanEnable" + i), ("扫码器" + (i + 1) + "使能"), "扫码使能：TRUE启用，FALSE禁用", scanEnable[i], RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_ADMIN);
                //InsertVoidParameter(("barcodeScanIP" + i), ("扫码器" + (i + 1) + "的IP"), "扫码器的IP：进行网口通讯则填，否则为空", barcodeScanIP[i], RecordType.RECORD_STRING, ParameterLevel.PL_STOP_ADMIN);
                //InsertVoidParameter(("barcodeScanCom" + i), ("扫码器" + (i + 1) + "的COM口"), "扫码器的COM口：进行串口通讯则填，否则为-1", barcodeScanCom[i], RecordType.RECORD_INT);
                //InsertVoidParameter(("barcodeScanPort" + i), ("扫码器" + (i + 1) + "的端口/波特率"), "扫码器的Port", barcodeScanPort[i], RecordType.RECORD_INT);
            }
            //InsertVoidParameter("scanCmd", "扫码指令", "触发扫码的指令", scanCmd, RecordType.RECORD_STRING, ParameterLevel.PL_STOP_MAIN);
            //InsertVoidParameter("scanLinefeed", "扫码结束符", "扫码器的扫码结束符：true有回车换行结束符，false无结束符", scanLinefeed, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            //InsertVoidParameter("codeLength", "条码长度", "条码长度：-1则不检查", codeLength, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertVoidParameter("codeType", "条码类别", "条码类别：空则不检查，多种类别以英文逗号(,)分隔", codeType, RecordType.RECORD_STRING, ParameterLevel.PL_STOP_MAIN);
            //InsertVoidParameter("scanNGType", "扫码NG字符", "扫码NG时扫码器反馈字符：空则不检查", scanNGType, RecordType.RECORD_STRING, ParameterLevel.PL_STOP_MAIN);
            //InsertVoidParameter("scanMaxCount", "最大扫码次数", "最大扫码次数：（X≥1）", scanMaxCount, RecordType.RECORD_INT, ParameterLevel.PL_STOP_OPER);
            //InsertVoidParameter("recvDelay", "接收电池延时", "接收电池时感应到位后延时：毫秒ms", recvDelay, RecordType.RECORD_INT, ParameterLevel.PL_STOP_OPER);
            //if(Def.IsNoHardware())
            //InsertVoidParameter("randNGBat", "生成NG电池", "生成随机NG电池", randNGBat, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
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
                        if (CheckInputState(IRecvPosInpos, !RecvPosIsEmpty()))
                        {
                            this.nextInitStep = InitSteps.Init_ConnectScanner;
                        }
                        break;
                    }

                case InitSteps.Init_ConnectScanner:
                    {
                        CurMsgStr("连接扫码枪", "Connect scanner");
                        for(int i = 0; i < barcodeScan.Length; i++)
                        {
                            if (!ScanConnect(i, true))
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
                        // 有 && 已扫码，发送电池请求
                        EventStatus state = GetEvent(this, EventList.OnloadScanSendBattery);
                        if(RecvPosIsFull() && BatScanCodeFinish() && ((EventStatus.Invalid == state) || (EventStatus.Finished == state)))
                        {
                            SetEvent(this, EventList.OnloadScanSendBattery, EventStatus.Require);
                        }
                        else if (!BatScanCodeFinish())
                        {
                            this.nextAutoStep = AutoSteps.Auto_ScanBatteryCode;
                            SaveRunData(SaveType.AutoStep);
                            break;
                        }
                        // 有，已响应
                        else if (RecvPosIsFull() && (EventStatus.Response == state))
                        {
                            if (this.DryRun || CheckInputState(IRecvPosInpos, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_SendBatttery;
                                SaveRunData(SaveType.AutoStep);
                                break;
                            }
                        }
                        // 无 && 入料请求
                        if (RecvPosIsEmpty() && CheckInputState(IRecvPosInpos, false))
                        {
                            state = GetEvent(this.recvBatRun, EventList.OnloadRecvSendBattery);
                            if(PositionPush(true) && (EventStatus.Require == state))
                            {
                                this.nextAutoStep = AutoSteps.Auto_RecvBatttery;
                                SaveRunData(SaveType.AutoStep);
                                break;
                            }
                        }
                        break;
                    }

                #region // 接收电池及扫码

                case AutoSteps.Auto_RecvBatttery:
                    {
                        CurMsgStr("接收电池", "Recv Battery");
                        if((null != this.recvBatRun) && PositionPush(false))
                        {
                            bool recvFin = false;
                            EventStatus state = GetEvent(this.recvBatRun, EventList.OnloadRecvSendBattery);
                            if(EventStatus.Require == state)
                            {
                                SetEvent(this.recvBatRun, EventList.OnloadRecvSendBattery, EventStatus.Response);
                            }
                            else if(((EventStatus.Ready == state) || (EventStatus.Start == state)) && !this.pickBatRun.RecvRearEnd())
                            {
                                DateTime time = DateTime.Now;
                                while(true)
                                {
                                    OutputAction(ORecvPosMotor, true);
                                    if(InputState(IRecvPosInpos, true))
                                    {
                                        recvFin = true;
                                        break;
                                    }
                                    if(this.DryRun)
                                    {
                                        Sleep(500);
                                        recvFin = true;
                                        break;
                                    }
                                    if((DateTime.Now - time).TotalSeconds > 10)
                                    {
                                        recvFin = false;
                                        break;
                                    }
                                    Sleep(1);
                                }
                                OutputAction(ORecvPosMotor, false);
                                if(!recvFin)
                                {
                                    ShowMessageBox((int)MsgID.RecvTimeout, "接收电池超时", "请检查电池是否接收到位", MessageType.MsgWarning);
                                }
                            }
                            if(recvFin)
                            {
                                for(int i = 0; i < (int)ModDef.RecvPos_ALL; i++)
                                {
                                    this.Battery[i].Copy(this.recvBatRun.Battery[i]);
                                    this.recvBatRun.Battery[i].Release();
                                }
                                this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                SaveRunData(SaveType.AutoStep | SaveType.Battery);
                                this.recvBatRun.SaveRunData(SaveType.Battery);
                                SetEvent(this.recvBatRun, EventList.OnloadRecvSendBattery, EventStatus.Finished);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_ScanBatteryCode:
                    {
                        CurMsgStr("电池扫码", "Scan battery code");
                        bool[] result = new bool[(int)ModDef.RecvPos_ALL];
                        for(int i = 0; i < (int)ModDef.RecvPos_ALL; i++)
                        {
                            result[i] = false;
                        }
                        for(int idx = 0; idx < this.scanMaxCount; idx++)
                        {
                            for(int i = 0; i < (int)ModDef.RecvPos_ALL; i++)
                            {
                                if (BatteryStatus.OK != this.Battery[i].Type)
                                {
                                    this.Battery[i].Code = "";
                                    this.Battery[i].Type = BatteryStatus.OK;
                                }
                                if (string.IsNullOrEmpty(this.Battery[i].Code) && !ScanCode(i))
                                {
                                    return; // 发送读取命令失败则直接报警
                                }
                            }
                            Random rd = new Random();
                            for(int batIdx = 0; batIdx < (int)ModDef.RecvPos_ALL; batIdx++)
                            {
                                if(string.IsNullOrEmpty(this.Battery[batIdx].Code))
                                {
                                    string code = string.Empty;
                                    if (!GetScanResult(batIdx, ref code))
                                    {
                                        continue;
                                    }
                                    if ((idx < (this.scanMaxCount - 1)) && !CheckScanCode(batIdx, code, false))
                                    {
                                        continue;
                                    }
                                    else if ((idx == (this.scanMaxCount - 1) && !CheckScanCode(batIdx, code, false)))
                                    {
                                        TotalData.OnScanNGCount++;

                                        this.Battery[batIdx].Type = BatteryStatus.NG;
                                        this.Battery[batIdx].NGType |= BatteryNGStatus.Scan;
                                    }
                                    this.Battery[batIdx].Code = (this.scanEnable[batIdx] ? code : string.Format("CODE{0}T{0}", Def.GetRandom(100000000, 900000000)));
                                    SaveScanBatData(batIdx, code);
                                    if (this.randNGBat)
                                    {
                                        this.Battery[batIdx].Type = (this.scanEnable[batIdx] ? BatteryStatus.OK : (BatteryStatus)rd.Next(1, rd.Next(1, 4)));
                                    }
                                    result[batIdx] = true;
                                }
                            }
                        }
                        if (MachineCtrl.GetInstance().UpdataMes)
                        {
                            for(int batIdx = 0; batIdx < (int)ModDef.RecvPos_ALL; batIdx++)
                            {
                                bool makingHold = false;
                                if(this.scanEnable[batIdx] && (BatteryStatus.OK == this.Battery[batIdx].Type))
                                {
                                    //if (!MesCheckBatteryStatus(this.Battery[batIdx].Code, ref makingHold))
                                    {
                                        if(makingHold)
                                        {
                                            this.Battery[batIdx].Type = BatteryStatus.NG;
                                            this.Battery[batIdx].NGType = BatteryNGStatus.MesNG;
                                        }
                                    }
                                }
                            }
                        }
                        this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                        SaveRunData(SaveType.Battery);
                        break;
                    }
                #endregion

                #region // 发送电池

                case AutoSteps.Auto_SendBatttery:
                    {
                        CurMsgStr("发送电池", "Send Battery");
                        if(CheckInputState(IRecvPosEnter, false) && PositionPush(false))
                        {
                            EventStatus state = GetEvent(this, EventList.OnloadScanSendBattery);
                            if(EventStatus.Response == state)
                            {
                                SetEvent(this, EventList.OnloadScanSendBattery, EventStatus.Ready);
                            }
                            else if(((EventStatus.Ready == state) || (EventStatus.Start == state)) && !this.pickBatRun.RecvRearEnd())
                            {
                                DateTime time = DateTime.Now;
                                while(true)
                                {
                                    OutputAction(ORecvPosMotor, true);
                                    if(InputState(IRecvPosEnter, false) && InputState(IRecvPosInpos, false) 
                                        && (EventStatus.Finished == GetEvent(this, EventList.OnloadScanSendBattery)))
                                    {
                                        break;
                                    }
                                    if((DateTime.Now - time).TotalSeconds > 10)
                                    {
                                        OutputAction(ORecvPosMotor, false);
                                        ShowMessageBox((int)MsgID.SendTimeout, "发送电池到下工序超时", "请检查电池是否到位", MessageType.MsgWarning);
                                        break;
                                    }
                                    Sleep(1);
                                }
                                OutputAction(ORecvPosMotor, false);
                            }
                            if(EventStatus.Finished == state)
                            {
                                PositionPush(true);
                                this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                SaveRunData(SaveType.AutoStep | SaveType.Battery);
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
        /// 初始化通用组参数 + 模组参数
        /// </summary>
        protected override void InitParameter()
        {
            int scanNum = (int)ModDef.RecvPos_ALL;
            this.conveyerLineEN = true;
            this.scanEnable = new bool[scanNum];
            this.barcodeScanIP = new string[scanNum];
            this.barcodeScanCom = new int[scanNum];
            this.barcodeScanPort = new int[scanNum];
            for(int i = 0; i < scanNum; i++)
            {
                this.scanEnable[i] = true;
                this.barcodeScanIP[i] = string.Empty;
                this.barcodeScanCom[i] = -1;
                this.barcodeScanPort[i] = 0;
            }
            this.scanCmd = "Scan";
            this.scanLinefeed = true;
            this.codeLength = -1;
            this.codeType = string.Empty;
            this.scanNGType = "ERROR";
            this.scanMaxCount = 1;
            this.recvDelay = 100;

            this.codeTypeArray = new string[scanNum];
            this.barcodeScan = new BarcodeScan[scanNum];
            for(int i = 0; i < this.barcodeScan.Length; i++)
            {
                this.barcodeScan[i] = new BarcodeScan();
            }
            this.randNGBat = false;

            base.InitParameter();
        }

        /// <summary>
        /// 读取通用组参数 + 模组参数
        /// </summary>
        /// <returns></returns>
        public override bool ReadParameter()
        {
            for(int i = 0; i < this.scanEnable.Length; i++)
            {
                this.scanEnable[i] = ReadBoolParameter(this.RunModule, ("scanEnable" + i), this.scanEnable[i]);
                this.barcodeScanIP[i] = ReadStringParameter(this.RunModule, ("barcodeScanIP" + i), this.barcodeScanIP[i]);
                this.barcodeScanCom[i] = ReadIntParameter(this.RunModule, ("barcodeScanCom" + i), this.barcodeScanCom[i]);
                this.barcodeScanPort[i] = ReadIntParameter(this.RunModule, ("barcodeScanPort" + i), this.barcodeScanPort[i]);
            }
            this.scanCmd = ReadStringParameter(this.RunModule, "scanCmd", this.scanCmd);
            this.scanLinefeed = ReadBoolParameter(this.RunModule, "scanLinefeed", this.scanLinefeed);
            this.codeLength = ReadIntParameter(this.RunModule, "codeLength", this.codeLength);
            this.codeType = ReadStringParameter(this.RunModule, "codeType", this.codeType);
            this.scanNGType = ReadStringParameter(this.RunModule, "scanNGType", this.scanNGType);
            this.codeTypeArray = this.codeType.Split((new char[] { ',' }), StringSplitOptions.RemoveEmptyEntries);
            this.conveyerLineEN = ReadBoolParameter(this.RunModule, "conveyerLineEN", this.conveyerLineEN);
            this.randNGBat = ReadBoolParameter(this.RunModule, "randNGBat", this.randNGBat);
            this.scanMaxCount = ReadIntParameter(this.RunModule, "scanMaxCount", this.scanMaxCount);
            this.recvDelay = ReadIntParameter(this.RunModule, "recvDelay", this.recvDelay);

            return base.ReadParameter();
        }

        /// <summary>
        /// 读取本模组的相关模组
        /// </summary>
        public override void ReadRelatedModule()
        {
            string value;

            this.pickBatRun = MachineCtrl.GetInstance().GetModule(RunID.OnloadLine) as RunProcessOnloadLine;
            // 来料扫码
            value = IniFile.ReadString(this.RunModule, "RecvBatRun", "", Def.GetAbsPathName(Def.ModuleExCfg));
            this.recvBatRun = MachineCtrl.GetInstance().GetModule(value) as RunProcessOnloadRecv;
        }

        #endregion

        #region // IO及电机

        /// <summary>
        /// 初始化模组IO及电机
        /// </summary>
        protected override void InitModuleIOMotor()
        {
            this.IRecvPosEnter = AddInput("IRecvPosEnter");
            this.IRecvPosInpos = AddInput("IRecvPosInpos");
            this.IPositionPush = AddInput("IPositionPush");
            this.IPositionPull = AddInput("IPositionPull");
            
            this.OPositionPush = AddOutput("OPositionPush");
            this.OPositionPull = AddOutput("OPositionPull");
            this.ORecvPosMotor = AddOutput("ORecvPosMotor");
        }

        /// <summary>
        /// 定位气缸推出
        /// </summary>
        /// <param name="push">true推出，false回退</param>
        /// <returns></returns>
        protected bool PositionPush(bool push)
        {
            //if (Def.IsNoHardware())
            {
                return true;
            }
            // 检查IO配置
            if((IPositionPush < 0) || (IPositionPull < 0)
                || (OPositionPush < 0 && OPositionPull < 0))
            {
                return false;
            }
            // 操作
            OutputAction(OPositionPush, push);
            OutputAction(OPositionPull, !push);
            // 检查到位
            // 仅有其一ON时才认为状态正确
            if(!WaitInputState(IPositionPush, push) || !WaitInputState(IPositionPull, !push))
            {
                return false;
            }
            return true;
        }
        
        #endregion

        #region // 电池数据

        /// <summary>
        /// 电池为满
        /// </summary>
        /// <returns></returns>
        public bool RecvPosIsFull()
        {
            for(int i = 0; i < (int)ModDef.RecvPos_ALL; i++)
            {
                if(BatteryStatus.Invalid == this.Battery[i].Type)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 电池为k空
        /// </summary>
        /// <returns></returns>
        public bool RecvPosIsEmpty()
        {
            for(int i = 0; i < (int)ModDef.RecvPos_ALL; i++)
            {
                if(BatteryStatus.Invalid != this.Battery[i].Type)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 电池已扫码
        /// </summary>
        /// <returns></returns>
        public bool BatScanCodeFinish()
        {
            for(int i = 0; i < this.Battery.Length; i++)
            {
                if ((BatteryStatus.OK == this.Battery[i].Type) && string.IsNullOrEmpty(this.Battery[i].Code))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region // 扫码器

        /// <summary>
        /// 扫码器的连接地址信息
        /// </summary>
        /// <returns></returns>
        public string ScanAdderInfo(int index)
        {
            return this.barcodeScan[index].AdderInfo();
        }

        /// <summary>
        /// 扫码器连接状态
        /// </summary>
        /// <param name="index">扫码器索引</param>
        /// <returns></returns>
        public bool ScanIsConnect(int index)
        {
            if(!this.scanEnable[index])
            {
                return true;
            }
            return this.barcodeScan[index].IsConnect();
        }

        /// <summary>
        /// 扫码器连接
        /// </summary>
        /// <param name="index">扫码器索引</param>
        /// <param name="connect">true连接，false断开</param>
        /// <returns></returns>
        public bool ScanConnect(int index, bool connect = true)
        {
            if(!this.scanEnable[index])
            {
                return true;
            }
            if(connect)
            {
                if(string.IsNullOrEmpty(this.barcodeScanIP[index]) && (this.barcodeScanCom[index] > -1))
                {
                    return this.barcodeScan[index].ConnectCom(this.barcodeScanCom[index], this.barcodeScanPort[index], (this.scanLinefeed ? "\r\n" : "\n"));
                }
                else if(!string.IsNullOrEmpty(this.barcodeScanIP[index]) && (this.barcodeScanCom[index] < 0))
                {
                    return this.barcodeScan[index].ConnectSocket(this.barcodeScanIP[index], this.barcodeScanPort[index]);
                }
            }
            else
            {
                return this.barcodeScan[index].Disconnect();
            }
            return false;
        }

        /// <summary>
        /// 扫码器触发扫码
        /// </summary>
        /// <returns></returns>
        public bool ScanCode(int index)
        {
            if(!this.scanEnable[index])
            {
                return true;
            }
            if (this.barcodeScan[index].Send(scanCmd + (scanLinefeed ? "\r\n" : "")))
            {
                return true;
            }
            ShowMessageBox((int)MsgID.ScanCodeFail, "触发扫码失败", "请检查扫码器连接", MessageType.MsgAlarm);
            return false;
        }

        /// <summary>
        /// 获取扫码器扫码结果
        /// </summary>
        /// <param name="code">获取到的条码</param>
        /// <param name="timeout">超时时间</param>
        /// <returns></returns>
        public bool GetScanResult(int index, ref string code, int timeout = 5 * 1000)
        {
            if(!this.scanEnable[index])
            {
                return true;
            }
            if(this.barcodeScan[index].Recv(ref code, timeout))
            {
                return true;
            }
            ShowMessageBox((int)MsgID.ScanCodeTimeout, "获取条码超时", "请检查扫码器", MessageType.MsgAlarm);
            return false;
        }

        /// <summary>
        /// 检查条码内容
        /// </summary>
        /// <param name="code">需要检查的条码</param>
        /// <param name="alm">是否报警</param>
        /// <returns></returns>
        public bool CheckScanCode(int index, string code, bool alm)
        {
            if(!this.scanEnable[index])
            {
                return true;
            }
            string msg, disp;
            if(!string.IsNullOrEmpty(this.scanNGType) && (code.IndexOf(this.scanNGType) > -1))
            {
                if (alm)
                {
                    msg = string.Format("扫码器扫码失败，扫码器反馈：{0}", code);
                    disp = "请检查扫码器";
                    ShowMessageBox((int)MsgID.CodeTypeError, msg, disp, MessageType.MsgWarning);
                }
                return false;
            }
            if ((this.codeLength > -1) && (code.Length != this.codeLength))
            {
                if (alm)
                {
                    msg = string.Format("【{0}】条码长度和【条码长度：{1}】参数不匹配", code, this.codeLength);
                    disp = "请检查扫码器";
                    ShowMessageBox((int)MsgID.CodeLenError, msg, disp, MessageType.MsgAlarm);
                }
                return false;
            }
            if(this.codeTypeArray.Length > 0)
            {
                bool result = false;
                foreach(var item in this.codeTypeArray)
                {
                    if(code.EndsWith(item))
                    {
                        result = true;
                        break;
                    }
                }
                if(!result)
                {
                    if (alm)
                    {
                        msg = string.Format("【{0}】条码未在【条码类型：{1}】参数中找到匹配项", code, this.codeType);
                        disp = "请检查扫码器";
                        ShowMessageBox((int)MsgID.CodeTypeError, msg, disp, MessageType.MsgAlarm);
                    }
                    return false;
                }
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
            return true;
        }
        
        /// <summary>
        /// 模组防呆监视：请勿加入耗时过长或阻塞操作
        /// </summary>
        public override void MonitorAvoidDie()
        {
            base.MonitorAvoidDie();
        }

        /// <summary>
        /// 设备停止后操作，如果派生类重写了该函数，它必须调用基实现。
        /// </summary>
        public override void AfterStopAction()
        {
            base.AfterStopAction();
        }

        #endregion

        #region // 保存数据

        /// <summary>
        /// 保存电池扫码数据
        /// </summary>
        private void SaveScanBatData(int batIdx, string code)
        {
            string file, title, text;
            file = string.Format(@"{0}\电芯扫码\{1}\{1}.csv", MachineCtrl.GetInstance().ProductionFilePath, DateTime.Now.ToString("yyyy-MM-dd"));
            title = "日期,时间,电芯索引,电芯条码";
            text = string.Format("{0},{1},{2}\r\n", DateTime.Now.ToString("yyyy-MM-dd,HH:mm:ss"), (batIdx + 1), code);
            Def.ExportCsvFile(file, title, text);
        }

        #endregion

    }
}
