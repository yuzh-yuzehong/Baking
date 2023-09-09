using HelperLibrary;
using System;
using System.Diagnostics;
using SystemControlLibrary;

namespace Machine
{
    /// <summary>
    /// 下料线体
    /// </summary>
    class RunProcessOffloadLine : RunProcess
    {
        #region // 枚举：步骤，模组数据，报警列表

        protected new enum InitSteps
        {
            Init_DataRecover = 0,
            Init_CheckBattery,
            Init_CheckCylinder,
            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,
            // 前端传递到后端
            Auto_TransferBattery,

            Auto_WorkEnd,
        }

        private enum ModDef
        {
            PlacePos_0 = 0,
            PlacePos_1,
            PlacePos_ALL,
            BufferPos_0 = PlacePos_ALL,
            BufferPos_1,
            BufferPos_ALL,
            PlacePos_Buffer_ALL = BufferPos_ALL,
        }

        private enum MsgID
        {
            Start = ModuleMsgID.OffloadLineMsgStartID,
            TransferTimeout,
            SendTimeout,
        }
        #endregion

        #region // 字段，属性

        #region // IO

        private int IRotateCylPush;      // 旋转气缸，推出到位
        private int IRotateCylPull;      // 旋转气缸，拉回到位
        private int IPushCylPush;        // 推出气缸，推出到位
        private int IPushCylPull;        // 推出气缸，拉回到位
        private int IPlaceHasBat;        // 放料位有料感应
        private int IPlaceOut;           // 放料位出口有料感应
        private int IBufferEnter;        // 缓存位进料感应
        private int IBufferInpos;        // 缓存位到位感应
        private int IBufferOut;          // 缓存位出料感应
        private int[] IBufferHasBat;     // 缓存位有料感应

        private int IRequireOffLoad;     // 对接：请求放料

        private int ORotateCylPush;      // 旋转气缸，推出
        private int ORotateCylPull;      // 旋转气缸，拉回
        private int OPushCylPush;        // 推出气缸，推出
        private int OPushCylPull;        // 推出气缸，拉回

        private int OFrontMotor;         // 前端电机
        private int OAfterMotor;         // 后端电机

        private int OPlacingBattery;     // 对接：放料中

        #endregion

        #region // 电机
        #endregion

        #region // ModuleEx.cfg配置
        #endregion

        #region // 模组参数

        #endregion

        #region // 模组数据

        private bool[] afterMotorON;       // 后段电机ON标记：0代表Run，1代表监测线程
        private bool monitorOut;        // 监测电池出：运行时置true，停止时置false

        #endregion

        #endregion

        public RunProcessOffloadLine(int RunID) : base(RunID)
        {
            InitBatteryPalletSize((int)ModDef.PlacePos_Buffer_ALL, 0);

            PowerUpRestart();

            InitParameter();

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
                        if (CheckInputState(IPlaceHasBat, !PlacePosIsEmpty()))
                        {
                            this.nextInitStep = InitSteps.Init_CheckCylinder;
                        }
                        break;
                    }
                case InitSteps.Init_CheckCylinder:
                    {
                        CurMsgStr("检查气缸状态", "Check cylinder state");
                        if (CheckInputState(IPlaceOut, false))
                        {
                            if(PushCylPush(false) && RotateCylPush(true))
                            {
                                this.nextInitStep = InitSteps.Init_End;
                            }
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
            if (!IsModuleEnable())
            {
                CurMsgStr("模组禁用", "Moudle not enable");
                Sleep(100);
                return;
            }

            if (Def.IsNoHardware())
            {
                Sleep(100);
            }

            if (!this.monitorOut)
            {
                this.monitorOut = true;
                MonitorBufferOut();
            }

            switch ((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");

                        // 放料位无，发送放电池信号
                        EventStatus state = GetEvent(this, EventList.OffLoadLinePlaceBattery);
                        if (PlacePosIsEmpty() && PlacePosSenserIsSafe() &&
                            ((EventStatus.Invalid == state) || (EventStatus.Finished == state)))
                        {
                            OutputAction(OFrontMotor, false);
                            if(PushCylPush(false) && RotateCylPush(true))
                            {
                                SetEvent(this, EventList.OffLoadLinePlaceBattery, EventStatus.Require);
                            }
                            break;
                        }
                        // 放料位无，已响应
                        else if (PlacePosIsEmpty() && (EventStatus.Response == state))
                        {
                            OutputAction(OFrontMotor, false);
                            if(PushCylPush(false) && RotateCylPush(true) && CheckInputState(IPlaceHasBat, false))
                            {
                                SetEvent(this, EventList.OffLoadLinePlaceBattery, EventStatus.Ready);
                            }
                            break;
                        }

                        // 放料位有：放料旋转平台 -> 缓存线
                        if (PlacePosIsFull() && ((EventStatus.Invalid == state) || (EventStatus.Finished == state)))
                        {
                            // 缓存位出无电芯，流入缓存位
                            if(InputState(IBufferEnter, false) && InputState(IBufferOut, false))
                            {
                                this.nextAutoStep = AutoSteps.Auto_TransferBattery;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_TransferBattery: {
                    CurMsgStr("前段传递到后段" , "Front transfer after");
                    if(RotateCylPush(false)&&PushCylPush(true)) {
                        if(InputState(IBufferOut , false)) {
                            OutputAction(OFrontMotor , true);
                        }
                        int inPos = 0;  // 0传递失败，1传递完成，2缓存位出料口有电芯暂时无法传递
                        DateTime time = DateTime.Now;
                        while(true) {

                            // 传递完成
                            //if (InputState(IPlaceHasBat, false) && InputState(IBufferEnter, false) && InputState(IBufferInpos, true))
                            if(InputState(IPlaceHasBat , false)&&InputState(IPlaceOut , false)&&InputState(IBufferEnter , false)) {
                                inPos=1;
                                break;
                            }
                            // 缓存位出料口有电芯，暂时无法传递
                            if(!this.afterMotorON[1]&&InputState(IBufferOut , true)) {
                                inPos=2;
                                break;
                            } else {
                                if(InputState(IBufferOut , false)) {
                                    OutputAction(OFrontMotor , true);
                                }
                            }
                            if(this.DryRun) {
                                Sleep(500);
                                inPos=1;
                                break;
                            }
                            //OutputAction(OAfterMotor, true);
                            this.afterMotorON[0]=true;
                            if((DateTime.Now-time).TotalSeconds>10) {
                                break;
                            }
                            Sleep(1);
                        }
                        OutputAction(OFrontMotor , false);
                        // 关闭后段电机
                        this.afterMotorON[0]=false;
                        if(1==inPos) {
                            for(int i = 0 ; i<(int)ModDef.PlacePos_ALL ; i++) {
                                this.Battery[(int)ModDef.BufferPos_0+i].Copy(this.Battery[i]);
                                this.Battery[i].Release();
                            }
                            if(PushCylPush(false)&&RotateCylPush(true)) {
                                this.nextAutoStep=AutoSteps.Auto_WorkEnd;
                                SaveRunData(SaveType.AutoStep|SaveType.Battery);
                                break;
                            }
                        } else if(0==inPos) {
                            ShowMessageBox((int)MsgID.TransferTimeout , "下料传递电池超时" , "请检查电池是否传递到位" , MessageType.MsgWarning);
                        }
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

        public override bool InitializeConfig(string module)
        {
            if(!base.InitializeConfig(module))
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
            this.monitorOut = false;
            this.afterMotorON = new bool[2];
            Array.Clear(afterMotorON, 0, afterMotorON.Length);

            base.InitParameter();
        }

        /// <summary>
        /// 读取通用模组参数
        /// </summary>
        /// <returns></returns>
        public override bool ReadParameter()
        {
            this.OnLoad = ReadBoolParameter(this.RunModule, "OnLoad", true);
            this.OffLoad = ReadBoolParameter(this.RunModule, "OffLoad", true);

            return base.ReadParameter();
        }
        #endregion

        #region // IO及电机

        /// <summary>
        /// 初始化模组IO及电机
        /// </summary>
        protected override void InitModuleIOMotor()
        {
            this.IRotateCylPush = AddInput("IRotateCylPush");
            this.IRotateCylPull = AddInput("IRotateCylPull");
            this.IPushCylPush = AddInput("IPushCylPush");
            this.IPushCylPull = AddInput("IPushCylPull");
            this.IPlaceHasBat = AddInput("IPlaceHasBat");
            this.IPlaceOut = AddInput("IPlaceOut");
            this.IBufferEnter = AddInput("IBufferEnter");
            this.IBufferInpos = AddInput("IBufferInpos");
            this.IBufferOut = AddInput("IBufferOut");
            this.IRequireOffLoad = AddInput("IRequireOffLoad");
            this.IBufferHasBat = new int[2];
            for(int i = 0; i < this.IBufferHasBat.Length; i++)
            {
                this.IBufferHasBat[i]= AddInput($"IBufferHasBat{i}");
            }

            this.ORotateCylPush = AddOutput("ORotateCylPush");
            this.ORotateCylPull = AddOutput("ORotateCylPull");
            this.OPushCylPush = AddOutput("OPushCylPush");
            this.OPushCylPull = AddOutput("OPushCylPull");
            this.OFrontMotor = AddOutput("OFrontMotor");
            this.OAfterMotor = AddOutput("OAfterMotor");
            this.OPlacingBattery = AddOutput("OPlacingBattery");
        }

        /// <summary>
        /// 旋转气缸动作  为 true 推出  为 false 回退  
        /// </summary>
        /// <param name="push"></param>
        /// <returns></returns>
        protected bool RotateCylPush(bool push)
        {
            if(Def.IsNoHardware())
            {
                return true;
            }
            // 检查IO配置
            if (IRotateCylPush < 0 || IRotateCylPull < 0 || ORotateCylPush < 0 || ORotateCylPull < 0)
            {
                return false;
            }
            // 操作 
            OutputAction(ORotateCylPush, push);
            OutputAction(ORotateCylPull, !push);

            if (!(WaitInput(Inputs(IRotateCylPush), push) && WaitInput(Inputs(IRotateCylPull), !push)))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 推出气缸推出  为 true 推出  为 false 回退
        /// </summary>
        /// <param name="push"></param>
        /// <returns></returns>
        protected bool PushCylPush(bool push)
        {
            if(Def.IsNoHardware())
            {
                return true;
            }
            // 检查IO配置
            if (IPushCylPush < 0 || IPushCylPull < 0 || OPushCylPush < 0 || OPushCylPull < 0)
            {
                return false;
            }
            // 操作 

            // 操作 

            OutputAction(OPushCylPush, push);
            OutputAction(OPushCylPull, !push);

            if (!(WaitInput(Inputs(IPushCylPush), push) && WaitInput(Inputs(IPushCylPull), !push)))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 放料位电池为满
        /// </summary>
        /// <returns></returns>
        public bool PlacePosIsFull()
        {
            for (int i = 0; i < (int)ModDef.PlacePos_ALL; i++)
            {
                if (BatteryStatus.Invalid == this.Battery[i].Type)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 放料位电池位空
        /// </summary>
        /// <returns></returns>
        public bool PlacePosIsEmpty()
        {
            for (int i = 0; i < (int)ModDef.PlacePos_ALL; i++)
            {
                if (BatteryStatus.Invalid != this.Battery[i].Type)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 放电池位感应器检测
        /// </summary>
        /// <returns></returns>
        public bool PlacePosSenserIsSafe()
        {
            if (Def.IsNoHardware())
            {
                return true;
            }
            if (!InputState(IPlaceHasBat, true))
            {
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 放料时 检查气缸感应器安全
        /// </summary>
        /// <returns></returns>
        public bool CheckRotateCylSafe()
        {
            if(!InputState(IRotateCylPush, true) || !InputState(IPushCylPull, true))
            {
                CheckInputState(IRotateCylPush, true);
                CheckInputState(IPushCylPull, true);
                return false;
            }      
            return true;
        }

        /// <summary>
        /// 下料线体有电池
        /// </summary>
        /// <returns></returns>
        private bool OffLineHasBat()
        {
            if (InputState(IBufferEnter, true))
            {
                return true;
            }
            if(InputState(IBufferInpos, true))
            {
                return true;
            }
            for(int i = 0; i < this.IBufferHasBat.Length; i++)
            {
                if(!InputState(IBufferHasBat[i], false))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 电池下料夹具为空
        /// </summary>
        /// <returns></returns>
        private bool OffloadPalletEmpty()
        {
            RunProcessOffloadBattery run = MachineCtrl.GetInstance().GetModule(RunID.OffloadBattery) as RunProcessOffloadBattery;
            if (null != run)
            {
                foreach(var item in run.Pallet)
                {
                    if (item.IsEmpty())
                    {
                        return true;
                    }
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
            // 推出气缸未回退，禁止操作旋转气缸
            if ((ORotateCylPush > -1 && Outputs(ORotateCylPush) == output) || (ORotateCylPull > -1 && Outputs(ORotateCylPull) == output))
            {
                if (!InputState(IPlaceOut, false))
                {
                    string msg = string.Format("【{0}{1}】感应器非ON，禁止操作旋转气缸！！！"
                                , Inputs(IPlaceOut).Num, Inputs(IPlaceOut).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
                if (InputState(IPushCylPull, false))
                {
                    string msg = string.Format("【{0}{1}】感应器非ON，禁止操作旋转气缸！！！"
                                , Inputs(IPushCylPull).Num, Inputs(IPushCylPull).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
                RunProcessOffloadBattery runOffLoadBat = MachineCtrl.GetInstance().GetModule(RunID.OffloadBattery) as RunProcessOffloadBattery;
                if(null != runOffLoadBat)
                {
                    if(!runOffLoadBat.CheckMotorZPos(MotorPosition.OffLoad_SafetyPos))
                    {
                        string msg = string.Format("下料电机Z轴不在安全位，禁止操作旋转气缸！！！");
                        ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                        return false;
                    }
                }
            }
            if((OPushCylPull > -1 && Outputs(OPushCylPull) == output) || (OPushCylPush > -1 && Outputs(OPushCylPush) == output))
            {
                if(!InputState(IPlaceOut, false))
                {
                    string msg = string.Format("【{0}{1}】感应器非ON，禁止操作推出气缸！！！"
                                , Inputs(IPlaceOut).Num, Inputs(IPlaceOut).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
            }

            if ((OAfterMotor > -1 && Outputs(OFrontMotor) == output))
            {
                // 推出气缸 未到位  禁止操作前段电机输出
                if (InputState(IPushCylPush, false))
                {
                    string msg = string.Format("【{0}{1}】感应器状态非ON，禁止操作前段电机！！！"
                               , Inputs(IPushCylPush).Num, Inputs(IPushCylPush).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
                // 旋转气缸 未到位  禁止操作前段电机输出
                if (InputState(IRotateCylPull, false))
                {
                    string msg = string.Format("【{0}{1}】感应器状态非ON，禁止操作前段电机！！！"
                               , Inputs(IRotateCylPull).Num, Inputs(IRotateCylPull).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
            }

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
        /// 设备停止后操作，如果派生类重写了该函数，它必须调用基实现。
        /// </summary>
        public override void AfterStopAction()
        {
            this.monitorOut = false;
        }

        #endregion

        #region // 后段物流线，监测电池出

        /// <summary>
        /// 监测电池出
        /// </summary>
        private async void MonitorBufferOut()
        {
            await System.Threading.Tasks.Task.Delay(1);

            while (this.monitorOut)
            {
                // 缓存位出料感应ON，等待向后工序放料
                if(InputState(IBufferOut, true))
                {
                    // 有请求，则完整执行一次放料
                    if(InputState(IRequireOffLoad, true))
                    {
                        DateTime time = DateTime.Now;
                        this.afterMotorON[1] = true;
                        OutputAction(OAfterMotor, true);
                        OutputAction(OPlacingBattery, true);
                        while(InputState(IBufferOut, true))
                        {
                            if((DateTime.Now - time).TotalSeconds > 10)
                            {
                                break;
                            }
                            Sleep(1);
                        }
                        OutputAction(OPlacingBattery, false);
                        this.afterMotorON[1] = false;
                    }
                }
                // 出料口感应OFF，前段电池流向出料口
                else if(OffloadPalletEmpty()&&InputState(IRequireOffLoad , true))
                {
                    if(OffLineHasBat() && OutputState(OAfterMotor, false))
                    {
                        DateTime time = DateTime.Now;
                        this.afterMotorON[1] = true;
                        OutputAction(OAfterMotor, true);
                        while(InputState(IBufferOut, false))
                        {
                            if((DateTime.Now - time).TotalSeconds > 10)
                            {
                                break;
                            }
                            Sleep(1);
                        }
                        this.afterMotorON[1] = false;
                    }
                }
                OutputAction(OAfterMotor, this.afterMotorON[0]);
                Sleep(1);
            }
            OutputAction(OPlacingBattery, false);
            OutputAction(OAfterMotor, false);
            Array.Clear(afterMotorON, 0, afterMotorON.Length);

            //Def.WriteLog("RunProcessOffloadLine", "MonitorBufferOut() end");
        }

        #endregion

    }
}

