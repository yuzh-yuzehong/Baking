using HelperLibrary;
using System;
using System.Diagnostics;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    /// <summary>
    /// 电池下料
    /// </summary>
    class RunProcessOffloadBattery : RunProcess
    {
        #region // 枚举：步骤，模组数据，报警列表

        protected new enum InitSteps
        {
            Init_DataRecver = 0,

            Init_CheckFingerState,
            Init_CheckPalletState,
            Init_MotorZHome,
            Init_MotorZMoveSafePos,
            Init_MotorXYUome,
            Init_MotorXYUMoveSafePos,
            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,

            // 避让大机器人
            Auto_MoveAvoidPos,
            Auto_WaitAvoidFinish,
            
            // 取：夹具
            Auto_CalcPalletPickPos,
            Auto_PalletPickPosXYUMove,
            Auto_PalletPickPosCheckFinger,
            Auto_PalletPickPosZDown,
            Auto_PalletPickPosFingerAction,
            Auto_PalletPickPosZUp,
            Auto_PalletPickPosEndCheck,

            // 计算放料位
            Auto_CalcPlacePos,

            // 暂存：可取可防，主要看抓手操作
            Auto_CalcBufferPos,
            Auto_BufferPosXYUMove,
            Auto_BufferPosZDown,
            Auto_BufferPosFingerAction,
            Auto_BufferPosZUp,
            Auto_BufferPosEndCheck,

            // 放：下料线体
            Auto_CalcOffloadPlacePos,
            Auto_OffloadPlacePosSetEvent,
            Auto_OffloadPlacePosXYUMove,
            Auto_OffloadPlacePosZDown,
            Auto_OffloadPlacePosFingerAction,
            Auto_OffloadPlacePosZUp,
            Auto_OffloadPlacePosEndCheck,

            // 放：下料待检测
            Auto_CalcPlaceDetectPos,
            Auto_PlaceDetectPosSetEvent,
            Auto_PlaceDetectPosXYUMove,
            Auto_PlaceDetectPosZDown,
            Auto_PlaceDetectPosFingerAction,
            Auto_PlaceDetectPosZUp,
            Auto_PlaceDetectPosEndCheck,

            // 放：下料放NG
            Auto_CalcPlaceNGPos,
            Auto_PlaceNGPosSetEvent,
            Auto_PlaceNGPosXYUMove,
            Auto_PlaceNGPosZDown,
            Auto_PlaceNGPosFingerAction,
            Auto_PlaceNGPosZUp,
            Auto_PlaceNGPosEndCheck,

            Auto_WorkEnd

        }

        protected enum ModDef
        {
            //  抓手  缓存
            Finger_0 = 0,
            Finger_1,
            Finger_ALL,
            Buffer_0 = Finger_ALL,
            Buffer_1,
            Buffer_ALL,
            Finger_Buffer_ALL = Buffer_ALL,
        };

        private enum MsgID
        {
            Start = ModuleMsgID.OffloadBatteryMsgStartID,
            MotorDelayStop,
            UpdataCount,
            MotorPosRangeErr,
        }

        #endregion

        #region // 取放位置结构体
        private struct PickPlacePos
        {
            #region //字段
            public MotorPosition station;           // 站号
            public int pltIdx;                     // 夹具索引           
            public int row;                         // 行索引
            public int col;                         // 列索引   
            public ModDef finger;               // 抓手索引         
            public bool fingerClose;                // 抓手关闭
            #endregion

            #region //方法
            public void SetData(MotorPosition curStation, int curPalIdex, int curRow, int curCol, ModDef curFinger, bool curFingerClose)
            {
                this.station = curStation;
                this.pltIdx = curPalIdex;
                this.row = curRow;
                this.col = curCol;
                this.finger = curFinger;
                this.fingerClose = curFingerClose;
            }

            public void Release()
            {
                this.station = MotorPosition.Invalid;
                this.pltIdx = -1;
                this.row = -1;
                this.col = -1;
                this.finger = ModDef.Finger_ALL;
                this.fingerClose = false;
            }
            #endregion
        };
        #endregion

        #region // 字段，属性

        #region // IO
        private int[] IFingerOpen;             // 输入夹爪气缸松开检测
        private int[] IFingerClose;            // 输入夹爪气缸夹紧检测
        private int[] IFingerBatCheck;         // 输入夹爪有料感应检测 
        private int[] IBufferCheck;            // 输入缓存位有料检测
        private int[] IPalKeepFlatLeft;        // 夹具放平检测左
        private int[] IPalKeepFlatRight;       // 夹具放平检测右
        private int[] IPalHasCheck;            // 夹具位有夹具检测
        private int[] IPalInposCheck;          // 夹具到位检测
        private int IRotatePush;               // 下料旋转气缸推出到位
        private int IRotatePull;               // 下料旋转气缸回退到位
        private int IFingerDelay;               // 抓手碰撞防呆检测

        private int[] OFingerOpen;             // 输出夹爪气缸打开
        private int[] OFingerClose;            // 输出夹爪气缸关闭
        private int[] OPalletAlarm;            // 夹具位报警
        private int ORotatePush;               // 下料旋转气缸推出
        private int ORotatePull;               // 下料旋转气缸回退
        #endregion

        #region // 电机
        private int MotorX;         // X轴电机
        private int MotorY;         // Y轴电机
        private int MotorZ;         // Z轴电机
        private int MotorU;         // 调宽电机
        #endregion

        #region // ModuleEx.cfg配置

        #endregion

        #region // 模组参数

        bool placeNGPallet;                // 下料放NG夹具下NG电池：TRUE启用，FALSE禁用
        bool detectFakeBat;                // 下料测试待测假电池：TRUE启用，FALSE禁用
        double motorXMaxPos;               // X轴最大行程
        double motorYMaxPos;               // Y轴最大行程
        double motorZMaxPos;               // Z轴最大行程
        double motorUMaxPos;               // U轴最大行程
        double pickPosXDis;                // 取位置X轴方向间距
        double pickPosYDis;                // 取位置Y轴方向间距   
        double placePosXDis;               // 放位置X轴方向间距
        double placePosYDis;               // 放位置Y轴方向间距
        double offLoadNGDis;               // 下料NG间距
        int offLoadAddPatBat;              // 下料随机生成夹具电池：0不生成，1生成夹具1，2生成夹具2
        bool randNGBat;                    // 生成随机NG电池

        #endregion

        #region // 模组数据

        // 配置关联模组
        private RunProcessOffloadLine offloadLine;          // 下料线体：OffloadLine = 
        private RunProcessOffloadDetectFake offloadDetect;  // 待测电池：OffloadDetect = 
        private RunProcessOffloadNG offloadNG;              // 下料NG线：OffloadNG = 

        private bool motorNeedStop;                   // 电机停止标志 
        private int OffLoadPalIdx;                    // 标记当前正在下料的夹具   
        private EventList avoidEvent;	              // 当前需避让信号        
        private PickPlacePos pickPos;                 // 取位置
        private PickPlacePos placePos;                // 放位置              

        private RobotActionInfo XYZAutoAction;        // 下料三坐标自动动作信息
        private RobotActionInfo XYZDebugAction;       // 下料三坐标手动调试动作信息

        #endregion

        #endregion

        public RunProcessOffloadBattery(int runId) : base(runId)
        {
            InitBatteryPalletSize((int)ModDef.Finger_Buffer_ALL, (int)ModuleMaxPallet.OffloadBattery);

            PowerUpRestart();

            InitParameter();
            //模组参数
            InsertVoidParameter("placeNGPallet", "下料下NG夹具", "下料放NG夹具下NG电池：TRUE启用，FALSE禁用", placeNGPallet, RecordType.RECORD_BOOL, ParameterLevel.PL_IDLE_ADMIN);
            InsertVoidParameter("detectFakeBat", "测试假电池", "下料测试待测水含量电池：TRUE启用，FALSE禁用", detectFakeBat, RecordType.RECORD_BOOL, ParameterLevel.PL_IDLE_ADMIN);
            InsertVoidParameter("motorXMaxPos", "X轴行程", "X轴能到达的最大行程(mm)", motorXMaxPos, RecordType.RECORD_DOUBLE, ParameterLevel.PL_IDLE_ADMIN);
            InsertVoidParameter("motorYMaxPos", "Y轴行程", "Y轴能到达的最大行程(mm)", motorYMaxPos, RecordType.RECORD_DOUBLE, ParameterLevel.PL_IDLE_ADMIN);
            InsertVoidParameter("motorZMaxPos", "Z轴行程", "Z轴能到达的最大行程(mm)", motorZMaxPos, RecordType.RECORD_DOUBLE, ParameterLevel.PL_IDLE_ADMIN);
            InsertVoidParameter("motorUMaxPos", "U轴行程", "U轴能到达的最大行程(mm)", motorUMaxPos, RecordType.RECORD_DOUBLE, ParameterLevel.PL_IDLE_ADMIN);
            InsertVoidParameter("pickPosXDis", "取位置X轴间距", "取位置X轴方向间距(mm)", pickPosXDis, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("pickPosYDis", "取位置Y轴间距", "取位置Y轴方向间距(mm)", pickPosYDis, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);
            //InsertVoidParameter("placePosXDis", "放位置X轴间距", "放位置X轴方向间距(mm)", placePosXDis, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_ADMIN);
            //InsertVoidParameter("placePosYDis", "放位置Y轴间距", "放位置Y轴方向间距(mm)", placePosYDis, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_ADMIN);
            //InsertVoidParameter("offLoadNGDis", "下料NG电池位间距", "下料NG电池位间距（mm）", offLoadNGDis, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_ADMIN);
            if(Def.IsNoHardware())
            {
                InsertVoidParameter("offLoadAddPatBat", "添加夹具电池", "夹具生成随机电池（测试用）：0不生成，1生成夹具1，2生成夹具2", offLoadAddPatBat, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
                InsertVoidParameter("randNGBat", "生成随机NG电池", "生成夹具同时生成随机NG电池", randNGBat, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
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
            if (!IsModuleEnable())
            {
                base.InitOperation();
                InitFinished();
                return;
            }

            switch ((InitSteps)this.nextInitStep)
            {
                case InitSteps.Init_DataRecver:
                    {
                        CurMsgStr("数据恢复", "Data recover");

                        if (MachineCtrl.GetInstance().DataRecover)
                        {
                            LoadRunData();
                        }
                        this.nextInitStep = InitSteps.Init_CheckFingerState;
                        break;
                    }
                case InitSteps.Init_CheckFingerState:
                    {
                        CurMsgStr("检查电池状态", "Check batery sensor");

                        for (ModDef i = ModDef.Finger_0; i < ModDef.Finger_ALL; i++)
                        {
                            if (!FingerCheck(i, (FingerBat(i).Type > BatteryStatus.Invalid)))
                            {
                                return;
                            }
                        }

                        for (ModDef i = ModDef.Buffer_0; i < ModDef.Buffer_ALL; i++)
                        {
                            if (!BufferCheck(i, (BufferBat(i).Type > BatteryStatus.Invalid)))
                            {
                                return;
                            }
                        }
                        this.nextInitStep = InitSteps.Init_CheckPalletState;
                        break;
                    }
                case InitSteps.Init_CheckPalletState:
                    {
                        CurMsgStr("检查夹具状态", "Check batery sensor");

                        for(int i = 0; i < (int)ModuleMaxPallet.OffloadBattery; i++)
                        {
                            if(!PalletKeepFlat(i, (this.Pallet[i].State > PalletStatus.Invalid)))
                            {
                                return;
                            }
                        }
                        this.nextInitStep = InitSteps.Init_MotorZHome;
                        break;
                    }
                case InitSteps.Init_MotorZHome:
                    {
                        CurMsgStr("电机Z轴回零", "Motor Z home");

                        if (MotorHome(this.MotorZ))
                        {
                            this.nextInitStep = InitSteps.Init_MotorZMoveSafePos;
                        }
                        break;
                    }
                case InitSteps.Init_MotorZMoveSafePos:
                    {
                        CurMsgStr("电机Z轴到安全位", "Motor Z move to safety position");

                        if(MotorZMove(MotorPosition.OffLoad_SafetyPos))
                        {
                            this.nextInitStep = InitSteps.Init_MotorXYUome;
                        }
                        break;
                    }
                case InitSteps.Init_MotorXYUome:
                    {
                        CurMsgStr("电机XYU轴回零", "Motor X Y U home");

                        int[] motor = { this.MotorX, this.MotorY, this.MotorU };
                        if (MotorsHome(motor, 3))
                        {
                            this.nextInitStep = InitSteps.Init_MotorXYUMoveSafePos;
                        }
                        break;
                    }
                case InitSteps.Init_MotorXYUMoveSafePos:
                    {
                        CurMsgStr("电机XYU轴到安全位", "Motor XYU move to safety position");

                        if (MotorXYUMove((int)MotorPosition.OffLoad_SafetyPos, 0, 0))
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
            if (!IsModuleEnable())
            {
                CurMsgStr("模组禁用", "Moudle not enable");
                Sleep(100);
                return;
            }

            if (Def.IsNoHardware())
            {
                Sleep(50);
            }

            #region // 自动步骤开始前的检查步骤

            switch((CheckSteps)this.AutoCheckStep)
            {
                case CheckSteps.Check_WorkStart:
                    {
                        CurMsgStr("检查电机停机前位置", "Check motor auto pos");
                        if(!CheckMotorAutoPos(this.XYZAutoAction))
                        {
                            return;
                        }
                        this.AutoCheckStep = CheckSteps.Check_WorkEnd;
                        break;
                    }
            }
            #endregion

            switch((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");

                        #region // 设置检查取放请求
                        for(int pltIdx = 0; pltIdx < (int)ModuleMaxPallet.OffloadBattery; pltIdx++)
                        {
                            // 夹具已取完，置OK状态
                            if ((PalletStatus.WaitOffload == this.Pallet[pltIdx].State) && this.Pallet[pltIdx].IsEmpty())
                            {
                                this.Pallet[pltIdx].State = PalletStatus.OK;
                                this.Pallet[pltIdx].Stage = PalletStage.Offload;
                                SaveRunData(SaveType.Pallet);
                            }
                            EventList modEvent = EventList.Invalid;
                            EventStatus state = EventStatus.Invalid;
                        int pos =-1;
                            // 有空位
                            if (PalletStatus.Invalid == this.Pallet[pltIdx].State)
                            {
                            // 下料区放干燥完成夹具
                                modEvent = EventList.OffLoadPlaceDryFinishPallet;
                                state = GetEvent(this, modEvent,ref pos );
                                if((EventStatus.Invalid == state) || (EventStatus.Finished == state)||(EventStatus.Require ==state&& pos != pltIdx))
                                {
                                    SetEvent(this, modEvent, EventStatus.Require, pltIdx);
                                }
                                // 请求 下料区放NG夹具（非空）
                                if(this.placeNGPallet)
                                {
                                    modEvent = EventList.OffLoadPlaceNGPallet;
                                    state = GetEvent(this, modEvent,ref pos);
                                    if((EventStatus.Invalid == state) || (EventStatus.Finished == state)||(EventStatus.Require==state&&pos!=pltIdx)) {
                                        SetEvent(this, modEvent, EventStatus.Require, pltIdx);
                                    }
                                }
                                // 下料区放待检测含假电池夹具（未取走假电池的夹具）
                                if(this.detectFakeBat)
                                {
                                    modEvent = EventList.OffLoadPlaceDetectFakePallet;
                                    state = GetEvent(this, modEvent,ref pos);
                                    if((EventStatus.Invalid == state) || (EventStatus.Finished == state)||(EventStatus.Require==state&&pos!=pltIdx)) {
                                        SetEvent(this, modEvent, EventStatus.Require, pltIdx);
                                    }
                                }
                            }
                            // 下料区取空夹具
                            else if ((PalletStatus.OK == this.Pallet[pltIdx].State) && this.Pallet[pltIdx].IsEmpty())
                            {
                                modEvent = EventList.OffLoadPickEmptyPallet;
                                state = GetEvent(this, modEvent,ref pos);
                                if((EventStatus.Invalid == state) || (EventStatus.Finished == state)||(EventStatus.Require==state&&pos!=pltIdx)) {
                                    SetEvent(this, modEvent, EventStatus.Require, pltIdx);
                                }
                            }
                            // 有NG夹具
                            else if (PalletStatus.NG == this.Pallet[pltIdx].State)
                            {
                                modEvent = this.Pallet[pltIdx].IsEmpty() ? EventList.OffLoadPickNGEmptyPallet : EventList.OffLoadPickNGPallet;
                                state = GetEvent(this, modEvent,ref pos);
                                if((EventStatus.Invalid == state) || (EventStatus.Finished == state)||(EventStatus.Require==state&&pos!=pltIdx)) {
                                    SetEvent(this, modEvent, EventStatus.Require, pltIdx);
                                }
                            }
                            // 下料区取等待水含量结果夹具（已取待测假电池的夹具）
                            else if((PalletStatus.WaitResult == this.Pallet[pltIdx].State) && !this.Pallet[pltIdx].IsEmpty())
                            {
                                modEvent = EventList.OffLoadPickWaitResultPallet;
                                state = GetEvent(this, modEvent,ref pos);
                                if((EventStatus.Invalid == state) || (EventStatus.Finished == state)||(EventStatus.Require==state&&pos!=pltIdx)) {
                                    SetEvent(this, modEvent, EventStatus.Require, pltIdx);
                                }
                            }
                        }
                        #endregion

                        #region // 有取放已响应
                        for(EventList i = EventList.OffLoadPlaceDryFinishPallet; i < EventList.OffLoadPickPlaceEnd; i++)
                        {
                            if (EventStatus.Response == GetEvent(this, i))
                            {
                                this.avoidEvent = i;
                                this.nextAutoStep = AutoSteps.Auto_MoveAvoidPos;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                return;
                            }
                        }
                        #endregion

                        #region //人工添加夹具   测试用
                        if(this.offLoadAddPatBat > 0 && offLoadAddPatBat <= (int)ModuleMaxPallet.OffloadBattery && Pallet[offLoadAddPatBat - 1].IsEmpty())
                        {
                            System.Random rnd = new System.Random();
                            this.Pallet[this.offLoadAddPatBat - 1].Release();
                            this.Pallet[this.offLoadAddPatBat - 1].State = PalletStatus.WaitOffload;
                            this.Pallet[this.offLoadAddPatBat - 1].Stage = PalletStage.Baked;
                            for (int row = 0; row < (int)this.Pallet[this.offLoadAddPatBat - 1].MaxRow; row++)
                            {
                                for (int col = 0; col < (int)this.Pallet[this.offLoadAddPatBat - 1].MaxCol; col++)
                                {
                                    if ((0 == row) && (0 == col) && (1 == this.offLoadAddPatBat))
                                    {
                                        continue;
                                    }
                                    Pallet[this.offLoadAddPatBat - 1].Battery[row, col].Type = this.randNGBat ? (BatteryStatus)rnd.Next(1, rnd.Next(1, 4)) : BatteryStatus.OK;
                                    Pallet[this.offLoadAddPatBat - 1].Battery[row, col].Code = $"CODE{rnd.Next(100000000, 900000000)}T{rnd.Next(100000000, 900000000)}";
                                }
                            }
                            //this.offLoadAddPatBat = 3 - this.offLoadAddPatBat;
                            this.offLoadAddPatBat = -1;
                        }
                        #endregion

                        // 取待测水含量电池
                        if (CalcPickDetectFakeBat(ref this.pickPos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPalletPickPos;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            break;
                        }
                        // 取料判断
                        else if(CalcWaitOffloadPallet(ref this.OffLoadPalIdx) && CalcPalletPickPos(this.OffLoadPalIdx, ref this.pickPos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPalletPickPos;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            break;
                        }
                        break;
                    }

                #region // 避让大机器人
                case AutoSteps.Auto_MoveAvoidPos:
                    {
                        this.msgChs = "避让机器人移动到安全位";
                        this.msgEng = "Avoid robot move to safe pos";
                        CurMsgStr(this.msgChs, this.msgEng);

                        if(MotorXYZAvoid())
                        {
                            int pltIdx = -1;
                            if(EventStatus.Response == GetEvent(this, this.avoidEvent, ref pltIdx))
                            {
                                switch(this.avoidEvent)
                                {
                                    case EventList.OffLoadPlaceDryFinishPallet:
                                    case EventList.OffLoadPlaceDetectFakePallet:
                                    case EventList.OffLoadPlaceNGPallet:
                                        {
                                            if(!PalletKeepFlat(pltIdx, false, true))
                                            {
                                                return;
                                            }
                                            break;
                                        }
                                    case EventList.OffLoadPickEmptyPallet:
                                    case EventList.OffLoadPickWaitResultPallet:
                                    case EventList.OffLoadPickRebakeFakePallet:
                                    case EventList.OffLoadPickNGPallet:
                                    case EventList.OffLoadPickNGEmptyPallet:
                                        {
                                            if(!PalletKeepFlat(pltIdx, true, true))
                                            {
                                                return;
                                            }
                                            break;
                                        }
                                }
                                if(SetEvent(this, this.avoidEvent, EventStatus.Ready, pltIdx))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_WaitAvoidFinish;
                                    SaveRunData(SaveType.AutoStep);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_WaitAvoidFinish:
                    {
                        this.msgChs = "等待避让完成";
                        this.msgEng = "Wait avoid finish";
                        CurMsgStr(this.msgChs, this.msgEng);

                        int pltIdx = -1;
                        if(EventStatus.Finished == GetEvent(this, this.avoidEvent, ref pltIdx))
                        {
                            //if (SetEvent(this, this.avoidEvent, EventStatus.Finished, pltIdx))
                            {
                                this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                #endregion

                #region // 取：夹具
                case AutoSteps.Auto_CalcPalletPickPos:
                    {
                        CurMsgStr("计算夹具取料位", "calculate pallet pick pos");
                        this.nextAutoStep = AutoSteps.Auto_PalletPickPosXYUMove;
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                case AutoSteps.Auto_PalletPickPosXYUMove:
                    {
                        this.msgChs = string.Format("电机XYU移动到[{0}号夹具{1}行-{2}列]取料位", pickPos.pltIdx + 1, pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Motor XYU move to [{0} pallet {1}row - {2}col] pick pos", pickPos.pltIdx + 1, pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(RotatePush(false, false) && MotorXYUMove(pickPos.station, pickPos.row, pickPos.col))
                        {
                            this.nextAutoStep = AutoSteps.Auto_PalletPickPosCheckFinger;
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletPickPosCheckFinger:
                    {
                        this.msgChs = string.Format("[{0}号夹具{1}行-{2}列]取料前检查抓手", pickPos.pltIdx + 1, pickPos.row, pickPos.col);
                        this.msgEng = string.Format("[{0} pallet {1}row - {2}col] pick pos check finger", pickPos.pltIdx + 1, pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerCheck(pickPos.finger, !pickPos.fingerClose))
                        {
                            this.nextAutoStep = AutoSteps.Auto_PalletPickPosZDown;
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletPickPosZDown:
                    {
                        this.msgChs = string.Format("[{0}号夹具{1}行-{2}列]取料位Z轴下降", pickPos.pltIdx + 1, pickPos.row, pickPos.col);
                        this.msgEng = string.Format("[{0} pallet {1}row - {2}col] pick pos motor Z down", pickPos.pltIdx + 1, pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerClose(pickPos.finger, !pickPos.fingerClose) && RotatePush(false, true)
                            && CheckStationDownSafe(pickPos))
                        {
                            if (MotorZMove(pickPos.station))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PalletPickPosFingerAction;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletPickPosFingerAction:
                    {
                        this.msgChs = string.Format("[{0}号夹具{1}行-{2}列]取料抓手关闭", pickPos.pltIdx + 1, pickPos.row, pickPos.col);
                        this.msgEng = string.Format("[{0} pallet {1}row - {2}col] pick finger close", pickPos.pltIdx + 1, pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerClose(pickPos.finger, pickPos.fingerClose))
                        {
                            switch(pickPos.finger)
                            {
                                case ModDef.Finger_0:
                                    {
                                        this.Battery[(int)ModDef.Finger_0].Copy(this.Pallet[pickPos.pltIdx].Battery[pickPos.row, pickPos.col]);
                                        if(BatteryStatus.Fake == this.Pallet[pickPos.pltIdx].Battery[pickPos.row, pickPos.col].Type)
                                        {
                                            this.Battery[(int)ModDef.Finger_0].Type = BatteryStatus.Detect;

                                            this.Pallet[pickPos.pltIdx].Battery[pickPos.row, pickPos.col].Type = BatteryStatus.FakeTag;
                                            this.Pallet[pickPos.pltIdx].State = PalletStatus.WaitResult;
                                        }
                                        else
                                        {
                                            this.Pallet[pickPos.pltIdx].Battery[pickPos.row, pickPos.col].Release();
                                        }
                                        break;
                                    }
                                case ModDef.Finger_1:
                                    {
                                        this.Battery[(int)ModDef.Finger_1].Copy(this.Pallet[pickPos.pltIdx].Battery[pickPos.row + 1, pickPos.col]);
                                        if(BatteryStatus.Fake == this.Pallet[pickPos.pltIdx].Battery[pickPos.row, pickPos.col].Type)
                                        {
                                            this.Battery[(int)ModDef.Finger_1].Type = BatteryStatus.Detect;

                                            this.Pallet[pickPos.pltIdx].Battery[pickPos.row, pickPos.col].Type = BatteryStatus.FakeTag;
                                            this.Pallet[pickPos.pltIdx].State = PalletStatus.WaitResult;
                                        }
                                        else
                                        {
                                            this.Pallet[pickPos.pltIdx].Battery[pickPos.row + 1, pickPos.col].Release();
                                        }
                                        break;
                                    }
                                case ModDef.Finger_ALL:
                                    {
                                        for(ModDef i = ModDef.Finger_0; i < pickPos.finger; i++)
                                        {
                                            this.Battery[(int)i].Copy(this.Pallet[pickPos.pltIdx].Battery[pickPos.row + (int)i, pickPos.col]);
                                            this.Pallet[pickPos.pltIdx].Battery[pickPos.row + (int)i, pickPos.col].Release();
                                        }
                                        break;
                                    }
                                default:
                                    return;
                            }
                            this.nextAutoStep = AutoSteps.Auto_PalletPickPosZUp;
                            SaveRunData(SaveType.AutoStep | SaveType.Battery | SaveType.Pallet, pickPos.pltIdx);
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletPickPosZUp:
                    {
                        this.msgChs = string.Format("[{0}号夹具{1}行-{2}列]取料位Z轴上升到安全位", pickPos.pltIdx + 1, pickPos.row, pickPos.col);
                        this.msgEng = string.Format("[{0} pallet {1}row - {2}col] pick pos motor Z up", pickPos.pltIdx + 1, pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(MotorZMove(MotorPosition.OffLoad_SafetyPos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_PalletPickPosEndCheck;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletPickPosEndCheck:
                    {
                        this.msgChs = string.Format("[{0}号夹具{1}行-{2}列]取料后检查抓手", pickPos.pltIdx + 1, pickPos.row, pickPos.col);
                        this.msgEng = string.Format("[{0} pallet {1}row - {2}col] pick end check finger", pickPos.pltIdx + 1, pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerCheck(pickPos.finger, pickPos.fingerClose))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                #endregion

                #region // 计算放料位
                case AutoSteps.Auto_CalcPlacePos:
                    {
                        CurMsgStr("计算放料位", "Calc place battery pos");
                        if (CalcPlaceDetectFakeBat(ref placePos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPlaceDetectPos;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }
                        // 优先放NG电池
                        else if((BatteryStatus.NG == FingerBat(ModDef.Finger_0).Type) || (BatteryStatus.NG == FingerBat(ModDef.Finger_1).Type))
                        {
                            if(CalcPlaceNGBattery(ref placePos))
                            {
                                this.nextAutoStep = AutoSteps.Auto_CalcPlaceNGPos;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            }
                        }
                        else if (CalcPlaceOffLoadLine(ref placePos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcOffloadPlacePos;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }
                        else if (CalcFingerBufferMatchesPos(ref placePos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcBufferPos;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }
                        else if (CalcCoolingSystemPlacePos(ref placePos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcOffloadPlacePos;
                            SaveRunData(SaveType.AutoStep|SaveType.Variables);
                        }
                        else if (FingerBatCount() < 1)
                        {
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep);
                        }
                        else
                        {
                            #region // 有取放已响应
                            for(EventList i = EventList.OffLoadPlaceDryFinishPallet; i < EventList.OffLoadPickPlaceEnd; i++)
                            {
                                if(EventStatus.Response == GetEvent(this, i))
                                {
                                    this.avoidEvent = i;
                                    this.nextAutoStep = AutoSteps.Auto_MoveAvoidPos;
                                    SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                    return;
                                }
                            }
                            #endregion
                        }
                        break;
                    }
                #endregion

                #region // 暂存：可取可防，主要看抓手操作
                case AutoSteps.Auto_CalcBufferPos:
                    {
                        CurMsgStr("计算暂存取放料位", "Calc place bufffer pos");
                        this.nextAutoStep = AutoSteps.Auto_BufferPosXYUMove;
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                case AutoSteps.Auto_BufferPosXYUMove:
                    {
                        this.msgChs = string.Format("电机XYU移动到[暂存位{0}]", placePos.pltIdx);
                        this.msgEng = string.Format("Motor XYU move to [bufffer pos {0}]", placePos.pltIdx);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(MotorXYUMove(placePos.station, placePos.row, placePos.col))
                        {
                            this.nextAutoStep = AutoSteps.Auto_BufferPosZDown;
                        }
                        break;
                    }
                case AutoSteps.Auto_BufferPosZDown:
                    {
                        this.msgChs = string.Format("[暂存位{0}]放料位Z轴下降", placePos.pltIdx);
                        this.msgEng = string.Format("[bufffer pos {0}] motor Z down", placePos.pltIdx);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(RotatePush(true, true) && CheckStationDownSafe(placePos) && MotorZMove(placePos.station))
                        {
                            this.nextAutoStep = AutoSteps.Auto_BufferPosFingerAction;
                        }
                        break;
                    }
                case AutoSteps.Auto_BufferPosFingerAction:
                    {
                        this.msgChs = string.Format("[暂存位{0}]放料抓手打开", placePos.pltIdx);
                        this.msgEng = string.Format("[bufffer pos {0}] Finger open", placePos.pltIdx);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerClose(placePos.finger, placePos.fingerClose))
                        {
                            switch(placePos.finger)
                            {
                                case ModDef.Finger_0:
                                    {
                                        // 夹爪0只能取放1/2
                                        int bufIdx = (int)placePos.station - (int)MotorPosition.OffLoad_BufferPos_1 + (int)ModDef.Buffer_0;
                                        // 取
                                        if(placePos.fingerClose)
                                        {
                                            this.Battery[(int)ModDef.Finger_0].Copy(this.Battery[bufIdx]);
                                            this.Battery[bufIdx].Release();
                                        }
                                        // 放
                                        else
                                        {
                                            this.Battery[bufIdx].Copy(this.Battery[(int)ModDef.Finger_0]);
                                            this.Battery[(int)ModDef.Finger_0].Release();
                                        }
                                        break;
                                    }
                                case ModDef.Finger_1:
                                    {
                                        // 夹爪1只能取放0/1
                                        int bufIdx = (int)placePos.station - (int)MotorPosition.OffLoad_BufferPos_0 + (int)ModDef.Buffer_0;
                                        // 取
                                        if(placePos.fingerClose)
                                        {
                                            this.Battery[(int)ModDef.Finger_1].Copy(this.Battery[bufIdx]);
                                            this.Battery[bufIdx].Release();
                                        }
                                        // 放
                                        else
                                        {
                                            this.Battery[bufIdx].Copy(this.Battery[(int)ModDef.Finger_1]);
                                            this.Battery[(int)ModDef.Finger_1].Release();
                                        }
                                        break;
                                    }
                                case ModDef.Finger_ALL:
                                    {
                                        // 取
                                        if(placePos.fingerClose)
                                        {
                                            for(int i = 0; i < (int)ModDef.Finger_ALL; i++)
                                            {
                                                int bufIdx = (int)ModDef.Finger_ALL + i;
                                                this.Battery[(int)i].Copy(this.Battery[bufIdx]);
                                                this.Battery[bufIdx].Release();
                                            }
                                        }
                                        // 放
                                        else
                                        {
                                            for(int i = 0; i < (int)ModDef.Finger_ALL; i++)
                                            {
                                                int bufIdx = (int)ModDef.Finger_ALL + i;
                                                this.Battery[bufIdx].Copy(this.Battery[(int)i]);
                                                this.Battery[(int)i].Release();
                                            }
                                        }
                                        break;
                                    }
                                default:
                                    return;
                            }
                            this.nextAutoStep = AutoSteps.Auto_BufferPosZUp;
                            SaveRunData(SaveType.AutoStep | SaveType.Battery);
                        }
                        break;
                    }
                case AutoSteps.Auto_BufferPosZUp:
                    {
                        this.msgChs = string.Format("[暂存位{0}]Z轴上升", placePos.pltIdx);
                        this.msgEng = string.Format("[bufffer pos {0}] motor Z up", placePos.pltIdx);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(MotorZMove(MotorPosition.OffLoad_SafetyPos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_BufferPosEndCheck;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_BufferPosEndCheck:
                    {
                        this.msgChs = string.Format("[暂存位{0}]取放后检查抓手", placePos.pltIdx);
                        this.msgEng = string.Format("[bufffer pos {0}] check finger", placePos.pltIdx);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerCheck(placePos.finger, placePos.fingerClose))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                #endregion

                #region // 放：下料线体
                case AutoSteps.Auto_CalcOffloadPlacePos:
                    {
                        CurMsgStr("计算放料位", "calculate place pos");
                        this.nextAutoStep = AutoSteps.Auto_OffloadPlacePosSetEvent;
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                case AutoSteps.Auto_OffloadPlacePosSetEvent:
                    {
                        this.msgChs = string.Format("[下料线体{0}行]放料位设置响应信号", placePos.row);
                        this.msgEng = string.Format("[Offload place {0}row] place pos set event", placePos.row);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(EventStatus.Require == GetEvent(this.offloadLine, EventList.OffLoadLinePlaceBattery))
                        {
                            if(SetEvent(this.offloadLine, EventList.OffLoadLinePlaceBattery, EventStatus.Response))
                            {
                                this.nextAutoStep = AutoSteps.Auto_OffloadPlacePosXYUMove;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadPlacePosXYUMove:
                    {
                        this.msgChs = string.Format("电机XYU移动到[下料线体{0}行]放料位", placePos.row);
                        this.msgEng = string.Format("Motor XYU move to [Offload place {0}row] place pos", placePos.row);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(RotatePush(true, false) && MotorXYUMove(placePos.station, placePos.row, placePos.col))
                        {
                            this.nextAutoStep = AutoSteps.Auto_OffloadPlacePosZDown;
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadPlacePosZDown:
                    {
                        this.msgChs = string.Format("[下料线体{0}行]放料位Z轴下降", placePos.row);
                        this.msgEng = string.Format("[Offload place {0}row] place pos motor Z down", placePos.row);
                        CurMsgStr(this.msgChs, this.msgEng);
                        EventStatus state = GetEvent(this.offloadLine, EventList.OffLoadLinePlaceBattery);
                        if((EventStatus.Ready == state) || (EventStatus.Start == state))
                        {
                            if (CheckStationDownSafe(placePos))
                            {
                                SetEvent(this.offloadLine, EventList.OffLoadLinePlaceBattery, EventStatus.Start);
                                if(RotatePush(true, true) && MotorZMove(placePos.station))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_OffloadPlacePosFingerAction;
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadPlacePosFingerAction:
                    {
                        this.msgChs = string.Format("[下料线体{0}行]放料位抓手动作", placePos.row);
                        this.msgEng = string.Format("[Offload place {0}row] place pos finger action", placePos.row);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerClose(placePos.finger, placePos.fingerClose))
                        {
                            RunProcessOffloadLine run = this.offloadLine;
                            switch(placePos.finger)
                            {
                                case ModDef.Finger_ALL:
                                    for(int i = (int)ModDef.Finger_0; i < (int)ModDef.Finger_ALL; i++)
                                    {
                                        run.Battery[i].Copy(this.Battery[i]);
                                        this.Battery[i].Release();
                                    }
                                    break;
                                default:
                                    return;
                            }
                            TotalData.OffloadCount += 2;

                            run.SaveRunData(SaveType.Battery);
                            this.nextAutoStep = AutoSteps.Auto_OffloadPlacePosZUp;
                            SaveRunData(SaveType.AutoStep|SaveType.Battery);
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadPlacePosZUp:
                    {
                        this.msgChs = string.Format("[下料线体{0}行]放料位Z轴上升", placePos.row);
                        this.msgEng = string.Format("[Offload place {0}row] place pos motor Z up", placePos.row);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(MotorZMove(MotorPosition.OffLoad_SafetyPos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_OffloadPlacePosEndCheck;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadPlacePosEndCheck:
                    {
                        this.msgChs = string.Format("[下料线体{0}行]放料后检查抓手", placePos.row);
                        this.msgEng = string.Format("[Offload place {0}row] place pos check finger", placePos.row);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerCheck(placePos.finger, placePos.fingerClose))
                        {
                            SetEvent(this.offloadLine, EventList.OffLoadLinePlaceBattery, EventStatus.Finished);
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                #endregion

                #region // 放：下料待检测
                case AutoSteps.Auto_CalcPlaceDetectPos:
                    {
                        CurMsgStr("计算放待检测电池位", "calculate pallet pick pos");
                        this.nextAutoStep = AutoSteps.Auto_PlaceDetectPosSetEvent;
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                case AutoSteps.Auto_PlaceDetectPosSetEvent:
                    {
                        this.msgChs = string.Format("[待检测位{0}行]放料位设置响应信号", placePos.row);
                        this.msgEng = string.Format("[Detect pos {0}row] place pos set event", placePos.row);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(EventStatus.Require == GetEvent(this.offloadDetect, EventList.OffLoadPlaceDetectBattery))
                        {
                            if(SetEvent(this.offloadDetect, EventList.OffLoadPlaceDetectBattery, EventStatus.Response))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PlaceDetectPosXYUMove;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceDetectPosXYUMove:
                    {
                        this.msgChs = string.Format("电机XYU移动到[待检测位{0}行]放料位", placePos.row);
                        this.msgEng = string.Format("Motor XYU move to [detect pos {0}row] place pos", placePos.row);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(RotatePush(true, false) && MotorXYUMove(placePos.station, placePos.row, placePos.col))
                        {
                            this.nextAutoStep = AutoSteps.Auto_PlaceDetectPosZDown;
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceDetectPosZDown:
                    {
                        this.msgChs = string.Format("[待检测位{0}行]放料位Z轴下降", placePos.row);
                        this.msgEng = string.Format("[detect pos {0}row] place pos motor Z down", placePos.row);
                        CurMsgStr(this.msgChs, this.msgEng);
                        EventStatus state = GetEvent(this.offloadDetect, EventList.OffLoadPlaceDetectBattery);
                        if((EventStatus.Ready == state) || (EventStatus.Start == state))
                        {
                            if(CheckStationDownSafe(placePos))
                            {
                                SetEvent(this.offloadDetect, EventList.OffLoadPlaceDetectBattery, EventStatus.Start);
                                if(RotatePush(true, true) && MotorZMove(placePos.station))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PlaceDetectPosFingerAction;
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceDetectPosFingerAction:
                    {
                        this.msgChs = string.Format("[待检测位{0}行]放料位抓手动作", placePos.row);
                        this.msgEng = string.Format("[detect pos {0}row] place pos finger action", placePos.row);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerClose(placePos.finger, placePos.fingerClose))
                        {
                            RunProcessOffloadDetectFake run = this.offloadDetect;
                            switch(placePos.finger)
                            {
                                case ModDef.Finger_0:
                                    run.Battery[0].Copy(this.Battery[(int)ModDef.Finger_0]);
                                    this.Battery[(int)ModDef.Finger_0].Release();
                                    break;
                                case ModDef.Finger_1:
                                    run.Battery[0].Copy(this.Battery[(int)ModDef.Finger_1]);
                                    this.Battery[(int)ModDef.Finger_1].Release();
                                    break;
                                default:
                                    return;
                            }
                            run.SaveRunData(SaveType.Battery);
                            this.nextAutoStep = AutoSteps.Auto_PlaceDetectPosZUp;
                            SaveRunData(SaveType.AutoStep | SaveType.Battery);
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceDetectPosZUp:
                    {
                        this.msgChs = string.Format("[待检测位{0}行]放料位Z轴上升", placePos.row);
                        this.msgEng = string.Format("[detect pos {0}row] place pos motor Z up", placePos.row);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(MotorZMove(MotorPosition.OffLoad_SafetyPos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_PlaceDetectPosEndCheck;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceDetectPosEndCheck:
                    {
                        this.msgChs = string.Format("[冷却系统{0}行]放料后检查抓手", placePos.row);
                        this.msgEng = string.Format("[cooling system {0}row] place pos check finger", placePos.row);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerCheck(placePos.finger, placePos.fingerClose))
                        {
                            SetEvent(this.offloadDetect, EventList.OffLoadPlaceDetectBattery, EventStatus.Finished);
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                #endregion

                #region // 放：下料放NG
                case AutoSteps.Auto_CalcPlaceNGPos:
                    {
                        CurMsgStr("计算下料放NG电池位", "calculate pallet pick pos");
                        this.nextAutoStep = AutoSteps.Auto_PlaceNGPosSetEvent;
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                case AutoSteps.Auto_PlaceNGPosSetEvent:
                    {
                        this.msgChs = string.Format("[下料放NG位]放料位设置响应信号");
                        this.msgEng = string.Format("[Place NG Pos] place pos set event");
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(EventStatus.Require == GetEvent(this.offloadNG, EventList.OffLoadPlaceNGBattery))
                        {
                            if(SetEvent(this.offloadNG, EventList.OffLoadPlaceNGBattery, EventStatus.Response))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PlaceNGPosXYUMove;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceNGPosXYUMove:
                    {
                        this.msgChs = string.Format("电机XYU移动到[下料放NG位]放料位");
                        this.msgEng = string.Format("Motor XYU move to [Place NG Pos] place pos");
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(RotatePush(true, false) && MotorXYUMove(placePos.station, placePos.row, placePos.col))
                        {
                            this.nextAutoStep = AutoSteps.Auto_PlaceNGPosZDown;
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceNGPosZDown:
                    {
                        this.msgChs = string.Format("[下料放NG位]放料位Z轴下降");
                        this.msgEng = string.Format("[Place NG Pos] place pos motor Z down");
                        CurMsgStr(this.msgChs, this.msgEng);
                        EventStatus state = GetEvent(this.offloadNG, EventList.OffLoadPlaceNGBattery);
                        if((EventStatus.Ready == state) || (EventStatus.Start == state))
                        {
                            if(CheckStationDownSafe(placePos))
                            {
                                SetEvent(this.offloadNG, EventList.OffLoadPlaceNGBattery, EventStatus.Start);
                                if(RotatePush(true, true) && MotorZMove(placePos.station))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PlaceNGPosFingerAction;
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceNGPosFingerAction:
                    {
                        this.msgChs = string.Format("[下料放NG位]放料位抓手动作");
                        this.msgEng = string.Format("[Place NG Pos] place pos finger action");
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerClose(placePos.finger, placePos.fingerClose))
                        {
                            RunProcessOffloadNG run = this.offloadNG;
                            switch(placePos.finger)
                            {
                                case ModDef.Finger_0:
                                    run.Battery[0].Copy(this.Battery[(int)ModDef.Finger_0]);
                                    this.Battery[(int)ModDef.Finger_0].Release();

                                    TotalData.BakedNGCount++;
                                    break;
                                case ModDef.Finger_1:
                                    run.Battery[1].Copy(this.Battery[(int)ModDef.Finger_1]);
                                    this.Battery[(int)ModDef.Finger_1].Release();

                                    TotalData.BakedNGCount++;
                                    break;
                                case ModDef.Finger_ALL:
                                    for(int i = 0; i < (int)ModDef.Finger_ALL; i++)
                                    {
                                        run.Battery[i].Copy(this.Battery[i]);
                                        this.Battery[i].Release();
                                    }

                                    TotalData.BakedNGCount += 2;
                                    break;
                                default:
                                    return;
                            }
                            run.SaveRunData(SaveType.Battery);
                            this.nextAutoStep = AutoSteps.Auto_PlaceNGPosZUp;
                            SaveRunData(SaveType.AutoStep | SaveType.Battery);
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceNGPosZUp:
                    {
                        this.msgChs = string.Format("[下料放NG位]放料位Z轴上升");
                        this.msgEng = string.Format("[Place NG Pos] motor Z up");
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(MotorZMove(MotorPosition.OffLoad_SafetyPos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_PlaceNGPosEndCheck;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceNGPosEndCheck:
                    {
                        this.msgChs = string.Format("[下料放NG位]放料后检查抓手");
                        this.msgEng = string.Format("[Place NG Pos] check finger");
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerCheck(placePos.finger, placePos.fingerClose))
                        {
                            SetEvent(this.offloadNG, EventList.OffLoadPlaceNGBattery, EventStatus.Finished);
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                #endregion

                case AutoSteps.Auto_WorkEnd:
                    {
                        CurMsgStr("工作完成", "Work end");
                        this.nextAutoStep = AutoSteps.Auto_WaitWorkStart;
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
            this.motorNeedStop = true;
            this.OffLoadPalIdx = 0;
            this.pickPos.Release();
            this.placePos.Release();

            if (null == this.XYZAutoAction)
            {
                this.XYZAutoAction = new RobotActionInfo();
            }
            this.XYZAutoAction.Release();

            if (null == this.XYZDebugAction)
            {
                this.XYZDebugAction = new RobotActionInfo();
            }
            this.XYZDebugAction.Release();

            base.InitRunData();
        }

        public override void LoadRunData()
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);
            key = string.Format("OffLoadPalIdx");
            this.OffLoadPalIdx = iniStream.ReadInt(section, key, this.OffLoadPalIdx);
            key = string.Format("avoidEvent");
            this.avoidEvent = (EventList)iniStream.ReadInt(section, key, (int)this.avoidEvent);

            key = string.Format("pickPos.station");
            this.pickPos.station = (MotorPosition)iniStream.ReadInt(section, key, (int)this.pickPos.station);
            key = string.Format("pickPos.PalIdex");
            this.pickPos.pltIdx = iniStream.ReadInt(section, key, this.pickPos.pltIdx);
            key = string.Format("pickPos.row");
            this.pickPos.row = iniStream.ReadInt(section, key, this.pickPos.row);
            key = string.Format("pickPos.col");
            this.pickPos.col = iniStream.ReadInt(section, key, this.pickPos.col);
            key = string.Format("pickPos.finger");
            this.pickPos.finger = (ModDef)iniStream.ReadInt(section, key, (int)this.pickPos.finger);
            key = string.Format("pickPos.fingerClose");
            this.pickPos.fingerClose = iniStream.ReadBool(section, key, this.pickPos.fingerClose);

            key = string.Format("placePos.station");
            this.placePos.station = (MotorPosition)iniStream.ReadInt(section, key, (int)this.placePos.station);
            key = string.Format("placePos.PalIdex");
            this.placePos.pltIdx = iniStream.ReadInt(section, key, this.placePos.pltIdx);
            key = string.Format("placePos.row");
            this.placePos.row = iniStream.ReadInt(section, key, this.placePos.row);
            key = string.Format("placePos.col");
            this.placePos.col = iniStream.ReadInt(section, key, this.placePos.col);
            key = string.Format("placePos.finger");
            this.placePos.finger = (ModDef)iniStream.ReadInt(section, key, (int)this.placePos.finger);
            key = string.Format("placePos.fingerClose");
            this.placePos.fingerClose = iniStream.ReadBool(section, key, this.placePos.fingerClose);

            base.LoadRunData();
        }

        public override void SaveRunData(SaveType saveType, int index = -1)
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);
            if (SaveType.Variables == (SaveType.Variables & saveType))
            {
                iniStream.WriteInt(section, "OffLoadPalIdx", (int)this.OffLoadPalIdx);
                iniStream.WriteInt(section, "avoidEvent", (int)this.avoidEvent);
                string[] posName = new string[] { "pickPos", "placePos" };
                PickPlacePos[] pos = new PickPlacePos[] { pickPos, placePos };
                for (int i = 0; i < pos.Length; i++)
                {
                    key = string.Format("{0}.station", posName[i]);
                    iniStream.WriteInt(section, key, (int)pos[i].station);
                    key = string.Format("{0}.PalIdex", posName[i]);
                    iniStream.WriteInt(section, key, (int)pos[i].pltIdx);
                    key = string.Format("{0}.row", posName[i]);
                    iniStream.WriteInt(section, key, pos[i].row);
                    key = string.Format("{0}.col", posName[i]);
                    iniStream.WriteInt(section, key, pos[i].col);
                    key = string.Format("{0}.finger", posName[i]);
                    iniStream.WriteInt(section, key, (int)pos[i].finger);
                    key = string.Format("{0}.fingerClose", posName[i]);
                    iniStream.WriteBool(section, key, pos[i].fingerClose);

                }
            }
            base.SaveRunData(saveType, index);
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
        
        // 初始化通用组参数 + 模组参数
        protected override void InitParameter()
        {
            this.motorXMaxPos = 0;
            this.motorYMaxPos = 0;
            this.motorZMaxPos = 0;
            this.motorUMaxPos = 0;
            this.pickPosXDis = 0;
            this.pickPosYDis = 0;
            this.placePosXDis = 0;
            this.placePosYDis = 0;
            this.offLoadNGDis = 0;
            this.offLoadAddPatBat = -1;
            this.placeNGPallet = false;
            this.detectFakeBat = false;
            this.randNGBat = false;

            base.InitParameter();
        }
        
        // 读取通用组参数 + 模组参数
        public override bool ReadParameter()
        {
            this.motorXMaxPos = ReadDoubleParameter(this.RunModule, "motorXMaxPos", this.motorXMaxPos);
            this.motorYMaxPos = ReadDoubleParameter(this.RunModule, "motorYMaxPos", this.motorYMaxPos);
            this.motorZMaxPos = ReadDoubleParameter(this.RunModule, "motorZMaxPos", this.motorZMaxPos);
            this.motorUMaxPos = ReadDoubleParameter(this.RunModule, "motorUMaxPos", this.motorUMaxPos);
            this.pickPosXDis = ReadDoubleParameter(this.RunModule, "pickPosXDis", this.pickPosXDis);
            this.pickPosYDis = ReadDoubleParameter(this.RunModule, "pickPosYDis", this.pickPosYDis);
            this.placePosXDis = ReadDoubleParameter(this.RunModule, "placePosXDis", this.placePosXDis);
            this.placePosYDis = ReadDoubleParameter(this.RunModule, "placePosYDis", this.placePosYDis);
            this.offLoadNGDis = ReadDoubleParameter(this.RunModule, "offLoadNGDis", this.offLoadNGDis);
            this.offLoadAddPatBat = ReadIntParameter(this.RunModule, "offLoadAddPatBat", this.offLoadAddPatBat);
            this.placeNGPallet = ReadBoolParameter(this.RunModule, "placeNGPallet", this.placeNGPallet);
            this.detectFakeBat = ReadBoolParameter(this.RunModule, "detectFakeBat", this.detectFakeBat);
            this.randNGBat = ReadBoolParameter(this.RunModule, "randNGBat", this.randNGBat);

            return base.ReadParameter();
        }
        
        //读取本模组相关得模组
        public override void ReadRelatedModule()
        {
            // 下料线体
            this.offloadLine = MachineCtrl.GetInstance().GetModule(RunID.OffloadLine) as RunProcessOffloadLine;
            // 下料NG线
            this.offloadNG = MachineCtrl.GetInstance().GetModule(RunID.OffloadNG) as RunProcessOffloadNG;
            // 待检测电池线体
            this.offloadDetect = MachineCtrl.GetInstance().GetModule(RunID.OffloadDetect) as RunProcessOffloadDetectFake;
        }
        #endregion

        #region // IO及电机

        /// <summary>
        /// 初始化IO及电机
        /// </summary>
        protected override void InitModuleIOMotor()
        {
            int maxFinger = (int)ModDef.Finger_ALL;
            this.IFingerOpen = new int[maxFinger];
            this.IFingerClose = new int[maxFinger];
            this.IFingerBatCheck = new int[maxFinger];
            this.IBufferCheck = new int[maxFinger];
            for (int i = 0; i < maxFinger; i++)
            {
                this.IFingerOpen[i] = AddInput("IFingerOpen" + i);
                this.IFingerClose[i] = AddInput("IFingerClose" + i);
                this.IFingerBatCheck[i] = AddInput("IFingerBatCheck" + i);
                this.IBufferCheck[i] = AddInput("IBufferCheck" + i);
            }
            int maxPlt = (int)ModuleMaxPallet.OffloadBattery;
            this.IPalKeepFlatLeft = new int[maxPlt];
            this.IPalKeepFlatRight = new int[maxPlt];
            this.IPalHasCheck = new int[maxPlt];
            this.IPalInposCheck = new int[maxPlt];
            for (int i = 0; i < maxPlt; i++)
            {
                this.IPalKeepFlatLeft[i] = AddInput("IPalKeepFlatLeft" + i);
                this.IPalKeepFlatRight[i] = AddInput("IPalKeepFlatRight" + i);
                this.IPalHasCheck[i] = AddInput("IPalHasCheck" + i);
                this.IPalInposCheck[i] = AddInput("IPalInposCheck" + i);
            }
            this.IFingerDelay = AddInput("IFingerDelay");
            this.IRotatePush = AddInput("IRotatePush");
            this.IRotatePull = AddInput("IRotatePull");

            this.OPalletAlarm = new int[maxPlt];
            for (int i = 0; i < maxPlt; i++)
            {
                this.OPalletAlarm[i] = AddOutput("OPalletAlarm" + i);
            }
            this.OFingerOpen = new int[maxFinger];
            this.OFingerClose = new int[maxFinger];
            for (int i = 0; i < maxFinger; i++)
            {
                this.OFingerOpen[i] = AddOutput("OFingerOpen" + i);
                this.OFingerClose[i] = AddOutput("OFingerClose" + i);
            }
            this.ORotatePush = AddOutput("ORotatePush");
            this.ORotatePull = AddOutput("ORotatePull");

            this.MotorX = AddMotor("MotorX");
            this.MotorY = AddMotor("MotorY");
            this.MotorZ = AddMotor("MotorZ");
            this.MotorU = AddMotor("MotorU");
        }

        /// <summary>
        /// 夹具放平检测
        /// </summary>
        /// <param name="pltIdx"></param>
        /// <param name="hasPat"></param>
        /// <param name="alarm"></param>
        /// <returns></returns>
        public override bool PalletKeepFlat(int pltIdx, bool hasPat, bool alarm = true)
        {
            if(Def.IsNoHardware())
            {
                return true;
            }

            if(pltIdx < 0 || pltIdx >= (int)ModuleMaxPallet.OffloadBattery)
            {
                return false;
            }

            if(!InputState(IPalHasCheck[pltIdx], hasPat)
                || !InputState(IPalInposCheck[pltIdx], hasPat)
                || !InputState(IPalKeepFlatLeft[pltIdx], hasPat)
                || !InputState(IPalKeepFlatRight[pltIdx], hasPat))
            {
                if(alarm)
                {
                    CheckInputState(IPalHasCheck[pltIdx], hasPat);
                    CheckInputState(IPalInposCheck[pltIdx], hasPat);
                    CheckInputState(IPalKeepFlatLeft[pltIdx], hasPat);
                    CheckInputState(IPalKeepFlatRight[pltIdx], hasPat);
                    OutputAction(OPalletAlarm[pltIdx], true);
                }
                return false;
            }

            return true;
        }

        #endregion

        #region // 电机操作

        /// <summary>
        /// 电机XYZ避让
        /// </summary>
        /// <returns></returns>
        private bool MotorXYZAvoid()
        {
            if(Def.IsNoHardware())
            {
                return true;
            }

            if(this.MotorX < 0 || this.MotorY < 0 || this.MotorU < 0)
            {
                return false;
            }
            string bufpos = "";
            float XcurPos = 0.0f;
            float XbufPos = 0.0f;
            Motors(MotorX).GetLocation((int)MotorPosition.OffLoad_SafetyPos, ref bufpos, ref XbufPos);

            if((int)MotorCode.MotorOK != Motors(MotorX).GetCurPos(ref XcurPos))
            {
                return false;
            }

            float zDestPos = 0;
            Motors(MotorZ).GetLocation((int)MotorPosition.OffLoad_SafetyPos, ref bufpos, ref zDestPos);
            if(!CheckMotorPosRange(Motors(MotorZ), zDestPos, MotorMoveType.MotorMoveAbsMove, 0, motorZMaxPos) 
                || !MotorMove(MotorZ, (int)MotorPosition.OffLoad_SafetyPos))
            {
                return false;
            }

            DataBaseLog.AddMotorLog(new DataBaseLog.MotorLogFormula(Def.GetProductFormula(), MachineCtrl.GetInstance().OperaterID
                , DateTime.Now.ToString(Def.DateFormal), Motors(MotorZ).MotorIdx, Motors(MotorZ).Name, OptMode.Auto.ToString(), "Z轴移动", "", bufpos));

            if(XcurPos < XbufPos)
            {
                if(!CheckMotorPosRange(Motors(MotorX), XbufPos, MotorMoveType.MotorMoveAbsMove, 0, motorXMaxPos)
                    || !MotorMove(MotorX, (int)MotorPosition.OffLoad_SafetyPos))
                {
                    return false;
                }

                DataBaseLog.AddMotorLog(new DataBaseLog.MotorLogFormula(Def.GetProductFormula(), MachineCtrl.GetInstance().OperaterID
                    , DateTime.Now.ToString(Def.DateFormal), Motors(MotorX).MotorIdx, Motors(MotorX).Name, OptMode.Auto.ToString(), "X轴移动", "", bufpos));
            }
            return true;
        }

        /// <summary>
        /// 电机三轴MOVE
        /// </summary>
        /// <param name="station"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        private bool MotorXYUMove(MotorPosition station, int row, int col)
        {
            if(Def.IsNoHardware())
            {
                return true;
            }

            if(this.MotorX < 0 || this.MotorY < 0 || this.MotorU < 0)
            {
                return false;
            }

            float XOffset = 0.0f;
            float YOffset = 0.0f;
            float UOffset = 0.0f;

            switch(station)
            {
                case MotorPosition.OffLoad_PickPltPos1:
                case MotorPosition.OffLoad_PickPltPos2:
                    XOffset = (float)(row * this.pickPosXDis);
                    YOffset = (float)(col * this.pickPosYDis);
                    break;
            }

            int[] mtrIdx = { this.MotorX, this.MotorY, this.MotorU };
            Motor[] mtrs = { Motors(this.MotorX), Motors(this.MotorY), Motors(this.MotorU) };
            int[] loc = { (int)station, (int)station, (int)station };
            float[] offsetPos = { XOffset, YOffset, UOffset };
            float[] destPos = { 0, 0, 0 };
            double[] maxPos = { this.motorXMaxPos, this.motorYMaxPos, this.motorUMaxPos };

            string stationName = "";
            for(int i = 0; i < mtrIdx.Length; i++)
            {
                string locName = "";
                float stationPos = 0;
                mtrs[i].GetLocation((int)station, ref locName, ref stationPos);
                destPos[i] = stationPos + offsetPos[i];
                if (!CheckMotorPosRange(mtrs[i], destPos[i], MotorMoveType.MotorMoveAbsMove, 0, maxPos[i]))
                {
                    return false;
                }
                if(string.IsNullOrEmpty(stationName))
                    stationName = locName;
            }

            this.XYZAutoAction.SetData((int)station, row, col, RobotOrder.MOVE, stationName);
            this.XYZDebugAction.SetData((int)station, row, col, RobotOrder.MOVE, stationName);

            DataBaseLog.AddMotorLog(new DataBaseLog.MotorLogFormula(Def.GetProductFormula(), MachineCtrl.GetInstance().OperaterID
                , DateTime.Now.ToString(Def.DateFormal), Motors(MotorX).MotorIdx, Motors(MotorX).Name, OptMode.Auto.ToString(), "XYU轴自动移动", "", stationName));

            if(MotorsMove(mtrIdx, loc, offsetPos, 3))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 电机Z移动
        /// </summary>
        /// <param name="station"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        private bool MotorZMove(MotorPosition station)
        {
            string stationName = "";
            float ZstationPos = 0.0f;
            Motors(MotorZ).GetLocation((int)station, ref stationName, ref ZstationPos);
            this.XYZAutoAction.order = (MotorPosition.OffLoad_SafetyPos == station) ? RobotOrder.UP : RobotOrder.DOWN;
            this.XYZDebugAction.order = (MotorPosition.OffLoad_SafetyPos == station) ? RobotOrder.UP : RobotOrder.DOWN;

            if(Def.IsNoHardware())
            {
                return true;
            }

            if(this.MotorZ < 0)
            {
                return false;
            }

            if (!CheckMotorPosRange(Motors(MotorZ), ZstationPos, MotorMoveType.MotorMoveAbsMove, 0, motorZMaxPos))
            {
                return false;
            }
            DataBaseLog.AddMotorLog(new DataBaseLog.MotorLogFormula(Def.GetProductFormula(), MachineCtrl.GetInstance().OperaterID
                , DateTime.Now.ToString(Def.DateFormal), Motors(MotorZ).MotorIdx, Motors(MotorZ).Name, OptMode.Auto.ToString(), "Z轴移动", "", stationName));

            return MotorMove(MotorZ, (int)station);
        }

        /// <summary>
        /// 获取动作信息
        /// </summary>
        /// <param name="autoAction"></param>
        /// <returns></returns>
        public RobotActionInfo GetRobotActionInfo(bool autoAction)
        {
            return autoAction ? this.XYZAutoAction : this.XYZDebugAction;
        }

        /// <summary>
        /// 检查电机Z轴是否在安全位
        /// </summary>
        /// <returns></returns>
        public bool CheckMotorZPos(MotorPosition posID)
        {
            if(CheckMotorPos(this.MotorZ, posID, false))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 自动运行时检查目标工位安全状态
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        private bool CheckStationDownSafe(PickPlacePos pos)
        {
            switch(pos.station)
            {
                case MotorPosition.OffLoad_SafetyPos:
                    return true;
                case MotorPosition.OffLoad_PickPltPos1:
                case MotorPosition.OffLoad_PickPltPos2:
                    return PalletKeepFlat(pos.pltIdx, true, true);
                case MotorPosition.OffLoad_PlacePos:
                    {
                        if(null != this.offloadLine)
                        {
                            return this.offloadLine.PlacePosSenserIsSafe() && this.offloadLine.CheckRotateCylSafe();
                        }
                        break;
                    }
                case MotorPosition.OffLoad_BufferPos_0:
                    {
                        // 抓手 - 暂存：true取[10-1*]
                        bool hasBat = pos.fingerClose;
                        if(FingerCheck(ModDef.Finger_0, hasBat) && BufferCheck(ModDef.Buffer_0, hasBat)
                            && FingerCheck(ModDef.Finger_1, !hasBat))
                        {
                            return true;
                        }
                        break;
                    }
                case MotorPosition.OffLoad_BufferPos_1:
                    {
                        if (CheckMotorZPos(pos.station))
                        {
                            return true;
                        }
                        switch(pos.finger)
                        {
                            case ModDef.Finger_0:
                                {
                                    // 抓手 - 暂存：true取[01-10]，false放[10-00]
                                    bool hasBat = pos.fingerClose;
                                    if(FingerCheck(ModDef.Finger_0, !hasBat) && BufferCheck(ModDef.Buffer_0, hasBat)
                                        && FingerCheck(ModDef.Finger_1, hasBat) && BufferCheck(ModDef.Buffer_1, false))
                                    {
                                        return true;
                                    }
                                    break;
                                }
                            case ModDef.Finger_1:
                                {
                                    // 抓手 - 暂存：true取[10-01]，false放[01-00]
                                    bool hasBat = pos.fingerClose;
                                    if(FingerCheck(ModDef.Finger_0, hasBat) && BufferCheck(ModDef.Buffer_0, false)
                                        && FingerCheck(ModDef.Finger_1, !hasBat) && BufferCheck(ModDef.Buffer_1, hasBat))
                                    {
                                        return true;
                                    }
                                    break;
                                }
                            case ModDef.Finger_ALL:
                                {
                                    // 抓手 - 暂存：true取[00-11]
                                    bool hasBat = pos.fingerClose;
                                    if(hasBat && FingerCheck(ModDef.Finger_0, !hasBat) && BufferCheck(ModDef.Buffer_0, hasBat)
                                        && FingerCheck(ModDef.Finger_1, !hasBat) && BufferCheck(ModDef.Buffer_1, hasBat))
                                    {
                                        return true;
                                    }
                                    break;
                                }
                        }
                        break;
                    }
                case MotorPosition.OffLoad_BufferPos_2:
                    {
                        bool hasBat = pos.fingerClose;
                        if(FingerCheck(ModDef.Finger_0, !hasBat) && BufferCheck(ModDef.Buffer_1, hasBat))
                        {
                            return true;
                        }
                        break;
                    }
                case MotorPosition.OffLoad_PlaceDetect:
                    {
                        if(null != this.offloadDetect)
                        {
                            return this.offloadDetect.PlaceSenserIsSafe(true);
                        }
                        break;
                    }
                case MotorPosition.OffLoad_PlaceNG:
                    {
                        if(null != this.offloadNG)
                        {
                            return this.offloadNG.PlaceSenserIsSafe(true);
                        }
                        break;
                    }
                default:
                    break;
            }
            return false;
        }

        /// <summary>
        /// 自动运行开始时电机位置防呆检查
        /// </summary>
        /// <returns></returns>
        private bool CheckMotorAutoPos(RobotActionInfo autoCmd)
        {
            if (Def.IsNoHardware())
            {
                return true;
            }

            #region // station检查XYU
            Motor[] mtrs = { Motors(this.MotorX), Motors(this.MotorY), Motors(this.MotorU) };
            for(int i = 0; i < mtrs.Length; i++)
            {
                string locName , offsetInfo;
                locName = offsetInfo = "";
                float curPos, locPos, offset;
                curPos = locPos = offset = 0;
                mtrs[i].GetCurPos(ref curPos);
                mtrs[i].GetLocation(autoCmd.station, ref locName, ref locPos);
                if((mtrs[i].MotorIdx == this.MotorX) || (mtrs[i].MotorIdx == this.MotorY))
                {
                    switch((MotorPosition)autoCmd.station)
                    {
                        case MotorPosition.OffLoad_PickPltPos1:
                        case MotorPosition.OffLoad_PickPltPos2:
                            if(mtrs[i].MotorIdx == this.MotorX)
                            {
                                offset = (float)(autoCmd.row * this.pickPosXDis);
                                offsetInfo = $"偏移量{offset:0.00}";
                            }
                            else
                            {
                                offset = (float)(autoCmd.col * this.pickPosYDis);
                                offsetInfo = $"偏移量{offset:0.00}";
                            }
                            break;
                    }
                }
                float posErr = Math.Abs(locPos + offset - curPos);
                if(posErr > mtrs[i].PosErrRange)
                {
                    offsetInfo = $"{autoCmd.station} {locName} {offsetInfo}";
                    string msg = $"{mtrs[i].Name}点位被改变！停机前在 {offsetInfo} 位置，当前在 {curPos:0.00} 位置，差距{posErr:0.00} > 精度{mtrs[i].PosErrRange:0.00}";
                    string dispose = $"请先移动 {mtrs[i].Name} 到停机前的 {offsetInfo} 位置再复位启动";
                    ShowMessageBox((int)LibMsgID.MsgMotorLocationChange, msg, dispose, MessageType.MsgWarning);
                    return false;
                }
            }
            #endregion

            #region // order检查Z
            {
                Motor mtrZ = Motors(this.MotorZ);
                string locName = "";
                float curPos, locPos;
                curPos = locPos = 0;
                int mtrZLoc = (autoCmd.order == RobotOrder.DOWN) ? autoCmd.station : (int)MotorPosition.OffLoad_SafetyPos;
                mtrZ.GetCurPos(ref curPos);
                mtrZ.GetLocation(mtrZLoc, ref locName, ref locPos);
                float posErr = Math.Abs(curPos - locPos);
                if(posErr > mtrZ.PosErrRange)
                {
                    string offset = $"{mtrZLoc} {locName}";
                    string msg = $"{mtrZ.Name}点位被改变！停机前在 {offset} 位置，当前在 {curPos:0.00} 位置，差距{posErr:0.00} > 精度{mtrZ.PosErrRange:0.00}";
                    string dispose = $"请先移动 {mtrZ.Name} 到停机前的 {offset} 位置再复位启动";
                    ShowMessageBox((int)LibMsgID.MsgMotorLocationChange, msg, dispose, MessageType.MsgWarning);
                    return false;
                }
            }
            #endregion

            return true;
        }

        #endregion

        #region // 取放料计算

        /// <summary>
        /// 计算等待下料夹具
        /// </summary>
        /// <param name="pltIdx"></param>
        /// <returns></returns>
        private bool CalcWaitOffloadPallet(ref int pltIdx)
        {
            if ((pltIdx < 0) || (PalletStatus.Invalid == this.Pallet[pltIdx].State) || (this.Pallet[pltIdx].IsEmpty()))
            {
                for(int i = 0; i < (int)ModuleMaxPallet.OffloadBattery; i++)
                {
                    if ((PalletStatus.WaitOffload == this.Pallet[i].State)
                        && (PalletStage.Baked == this.Pallet[i].Stage) 
                        && !this.Pallet[i].IsEmpty())
                    {
                        pltIdx = i;
                        string msg="";
                        for (int j = 0; j < (MesData.mesFrequency == 0 ? 3 : MesData.mesFrequency); j++)
                        {
                            //托盘解绑
                            if (!MachineCtrl.GetInstance().MesUnbindPalletInfo(this.Pallet[i].Code, MesResources.Offload, ref msg))
                            {
                                //获取报警字符串是否包含"超时"二字,如果没有则跳出循环
                                if (!msg.Contains("超时"))
                                {
                                    break;
                                }
                                if (j == 2)
                                {
                                    ShowMsgBox.ShowDialog($"MES夹具解绑接口超时失败3次，请检查MES连接状态", MessageType.MsgAlarm);
                                }
                            }
                            else
                            {
                                break;
                            }
                        }

                        //MachineCtrl.GetInstance().MesUnbindPalletInfo(this.Pallet[i].Code, MesResources.Offload,ref msg);
                        return true;
                    }
                    else if((PalletStatus.NG == this.Pallet[i].State)
                        && !this.Pallet[i].IsEmpty())
                    {
                        pltIdx = i;
                        string msg="";
                        for (int j = 0; j < 3; j++)
                        {
                            //托盘解绑
                            if (!MachineCtrl.GetInstance().MesUnbindPalletInfo(this.Pallet[i].Code, MesResources.Offload, ref msg))
                            {
                                //获取报警字符串是否包含"超时"二字,如果没有则跳出循环
                                if (!msg.Contains("超时"))
                                {
                                    break;
                                }
                                if (j == 2)
                                {
                                    ShowMsgBox.ShowDialog($"MES夹具解绑接口超时失败3次，请检查MES连接状态", MessageType.MsgAlarm);
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        //MachineCtrl.GetInstance().MesUnbindPalletInfo(this.Pallet[i].Code, MesResources.Offload,ref msg);
                        return true;
                    }
                }
            }
            else if(((PalletStatus.WaitOffload == this.Pallet[pltIdx].State) || (PalletStatus.NG == this.Pallet[pltIdx].State))
                && !this.Pallet[pltIdx].IsEmpty())
            {
                return true;
            }
            pltIdx = -1;
            return false;
        }

        /// <summary>
        /// 计算夹具取料位置
        /// </summary>
        /// <param name="PltID"></param>
        /// <param name="curPickPos"></param>
        /// <returns></returns>
        private bool CalcPalletPickPos(int pltIdx, ref PickPlacePos curPickPos)
        {
            if ((BatteryStatus.Invalid == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_1).Type))
            {
                //if (PalletStatus.WaitOffload == this.Pallet[pltIdx].State)
                {
                    for(int col = 0; col < this.Pallet[pltIdx].MaxCol; col++)
                    {
                        for(int row = 0; row < this.Pallet[pltIdx].MaxRow - 1; row++)
                        {
                            // 假电池标记
                            if(BatteryStatus.FakeTag == this.Pallet[pltIdx].Battery[row, col].Type)
                            {
                                this.Pallet[pltIdx].Battery[row, col].Release();
                            }
                            // NG
                            if((BatteryStatus.NG == this.Pallet[pltIdx].Battery[row, col].Type) || (BatteryStatus.NG == this.Pallet[pltIdx].Battery[row + 1, col].Type)
                                || ((row + 2 == this.Pallet[pltIdx].MaxRow) && (BatteryStatus.NG == this.Pallet[pltIdx].Battery[row, col].Type)))
                            {
                                if (PalletStatus.NG != this.Pallet[pltIdx].State)
                                {
                                    this.Pallet[pltIdx].State = PalletStatus.NG;
                                    SaveRunData(SaveType.Pallet, pltIdx);
                                }
                            }
                            // 取两个
                            //else if((BatteryStatus.OK == this.Pallet[pltIdx].Battery[row, col].Type)
                            //    && (BatteryStatus.OK == this.Pallet[pltIdx].Battery[row + 1, col].Type))
                            if((BatteryStatus.Invalid != this.Pallet[pltIdx].Battery[row, col].Type)
                                && (BatteryStatus.Invalid != this.Pallet[pltIdx].Battery[row + 1, col].Type))
                            {
                                curPickPos.SetData(MotorPosition.OffLoad_PickPltPos1 + pltIdx, pltIdx, row, col, ModDef.Finger_ALL, true);
                                return true;
                            }
                            // 取一个
                            else if(BatteryStatus.Invalid != this.Pallet[pltIdx].Battery[row, col].Type)
                            {
                                curPickPos.SetData(MotorPosition.OffLoad_PickPltPos1 + pltIdx, pltIdx, row, col, ModDef.Finger_0, true);
                                return true;
                            }
                            else if((row + 2 == this.Pallet[pltIdx].MaxRow) 
                                && (BatteryStatus.Invalid != this.Pallet[pltIdx].Battery[row + 1, col].Type))
                            {
                                curPickPos.SetData(MotorPosition.OffLoad_PickPltPos1 + pltIdx, pltIdx, row, col, ModDef.Finger_1, true);
                                return true;
                            }
                        }
                    }
                    return false;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 计算冷却系统放位置
        /// </summary>
        /// <param name="curPos"></param>
        /// <returns></returns>
        private bool CalcCoolingSystemPlacePos(ref PickPlacePos curPos)
        {
            //RunProcessCoolingSystem run = this.coolingSystem;
            RunProcess run = null;
            if ((null != run) && (EventStatus.Require == GetEvent(run, EventList.CoolingSystemPlaceBattery)))
            {
                for(int row = 0; row < run.BatteryLine.MaxRow - 1; row++)
                {
                    if((BatteryStatus.Invalid == run.BatteryLine.Battery[row, 0].Type)
                        && (BatteryStatus.Invalid == run.BatteryLine.Battery[row + 1, 0].Type))
                    {
                        if((BatteryStatus.OK == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.OK == FingerBat(ModDef.Finger_1).Type))
                        {
                            curPos.SetData(MotorPosition.OffLoad_PlacePos, -1, row, 0, ModDef.Finger_ALL, false);
                            return true;
                        }
                        else if((BatteryStatus.OK == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_1).Type))
                        {
                            curPos.SetData(MotorPosition.OffLoad_PlacePos, -1, row, 0, ModDef.Finger_0, false);
                            return true;
                        }
                        if((BatteryStatus.Invalid == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.OK == FingerBat(ModDef.Finger_1).Type))
                        {
                            curPos.SetData(MotorPosition.OffLoad_PlacePos, -1, row, 0, ModDef.Finger_1, false);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 计算取待测试假电池位置
        /// </summary>
        /// <param name="curPos"></param>
        /// <returns></returns>
        private bool CalcPickDetectFakeBat(ref PickPlacePos curPos)
        {
            int fakeRow, fakeCol;
            fakeRow = fakeCol = -1;
            for(int i = 0; i < (int)ModuleMaxPallet.OffloadBattery; i++)
            {
                if((PalletStatus.Detect == this.Pallet[i].State) && this.Pallet[i].GetFakePos(ref fakeRow, ref fakeCol))
                {
                    if(0 == (fakeRow % 2))
                    {
                        curPos.SetData((MotorPosition.OffLoad_PickPltPos1 + i), i, fakeRow, fakeCol, ModDef.Finger_0, true);
                    }
                    else
                    {
                        curPos.SetData((MotorPosition.OffLoad_PickPltPos1 + i), i, (fakeRow - 1), fakeCol, ModDef.Finger_1, true);
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 计算假电池放料位
        /// </summary>
        /// <param name="curPos"></param>
        /// <returns></returns>
        private bool CalcPlaceDetectFakeBat(ref PickPlacePos curPos)
        {
            RunProcessOffloadDetectFake run = this.offloadDetect;
            if((null != run) && (EventStatus.Require == GetEvent(run, EventList.OffLoadPlaceDetectBattery)))
            {
                if((BatteryStatus.Detect == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_1).Type))
                {
                    curPos.SetData(MotorPosition.OffLoad_PlaceDetect, -1, 0, 0, ModDef.Finger_0, false);
                    return true;
                }
                else if ((BatteryStatus.Invalid == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Detect == FingerBat(ModDef.Finger_1).Type))
                {
                    curPos.SetData(MotorPosition.OffLoad_PlaceDetect, -1, 0, 0, ModDef.Finger_1, false);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 计算放NG电池位置
        /// </summary>
        /// <param name="curPos"></param>
        /// <returns></returns>
        private bool CalcPlaceNGBattery(ref PickPlacePos curPos)
        {
            RunProcessOffloadNG run = this.offloadNG;
            if((null != run) && (EventStatus.Require == GetEvent(run, EventList.OffLoadPlaceNGBattery)))
            {
                // 2个NG
                if((BatteryStatus.NG == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.NG == FingerBat(ModDef.Finger_1).Type))
                {
                    for(int i = 0; i < run.Battery.Length - 1; i++)
                    {
                        if((BatteryStatus.Invalid == run.Battery[i].Type) && run.PlacePosInposIsSafe(i, false)
                            && (BatteryStatus.Invalid == run.Battery[i + 1].Type) && run.PlacePosInposIsSafe(i + 1, false))
                        {
                            curPos.SetData(MotorPosition.OffLoad_PlaceNG, -1, i, 0, ModDef.Finger_ALL, false);
                            return true;
                        }
                    }
                }
                // 抓手0的NG，抓手1非空
                else if((BatteryStatus.NG == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid != FingerBat(ModDef.Finger_1).Type))
                {
                    for(int i = 0; i < run.Battery.Length - 1; i++)
                    {
                        if((BatteryStatus.Invalid == run.Battery[i].Type) && run.PlacePosInposIsSafe(i, false)
                            && (BatteryStatus.Invalid == run.Battery[i + 1].Type) && run.PlacePosInposIsSafe(i + 1, false))
                        {
                            curPos.SetData(MotorPosition.OffLoad_PlaceNG, -1, i, 0, ModDef.Finger_0, false);
                            return true;
                        }
                    }
                }
                // 抓手0的NG，抓手1空
                else if((BatteryStatus.NG == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_1).Type))
                {
                    for(int i = 0; i < run.Battery.Length - 1; i++)
                    {
                        if((BatteryStatus.Invalid == run.Battery[i].Type) && run.PlacePosInposIsSafe(i, false))
                        {
                            curPos.SetData(MotorPosition.OffLoad_PlaceNG, -1, i, 0, ModDef.Finger_0, false);
                            return true;
                        }
                    }
                }
                // 抓手1的NG，抓手0非空
                else if((BatteryStatus.NG == FingerBat(ModDef.Finger_1).Type) && (BatteryStatus.Invalid != FingerBat(ModDef.Finger_0).Type))
                {
                    for(int i = 0; i < run.Battery.Length - 1; i++)
                    {
                        if((BatteryStatus.Invalid == run.Battery[i].Type) && run.PlacePosInposIsSafe(i, false)
                            && (BatteryStatus.Invalid == run.Battery[i + 1].Type) && run.PlacePosInposIsSafe(i + 1, false))
                        {
                            curPos.SetData(MotorPosition.OffLoad_PlaceNG, -1, i, 0, ModDef.Finger_1, false);
                            return true;
                        }
                    }
                }
                // 抓手1的NG，抓手0空
                else if((BatteryStatus.NG == FingerBat(ModDef.Finger_1).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_0).Type))
                {
                    // 抓手1从1起
                    for(int i = 1; i < run.Battery.Length; i++)
                    {
                        if((BatteryStatus.Invalid == run.Battery[i].Type) && run.PlacePosInposIsSafe(i, false))
                        {
                            curPos.SetData(MotorPosition.OffLoad_PlaceNG, -1, i - 1, 0, ModDef.Finger_1, false);
                            return true;
                        }
                    }
                }
                // 无法放，置取消
                SetEvent(run, EventList.OffLoadPlaceNGBattery, EventStatus.Cancel);
            }
            return false;
        }

        /// <summary>
        /// 计算抓手及暂存配对位置
        /// </summary>
        /// <param name="placeplt"></param>
        /// <param name="curPos"></param>
        /// <returns></returns>
        private bool CalcFingerBufferMatchesPos(ref PickPlacePos curPos)
        {
            // 抓手为空，暂存满，取暂存
            if((BatteryStatus.Invalid == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_1).Type))
            {
                // 暂存01都有
                if((BatteryStatus.OK == BufferBat(ModDef.Buffer_0).Type) && (BatteryStatus.OK == BufferBat(ModDef.Buffer_1).Type))
                {
                    curPos.SetData(MotorPosition.OffLoad_BufferPos_1, 2, 0, 0, ModDef.Finger_ALL, true);
                    return true;
                }
            }
            // 抓手0有，暂存有-》抓手1取暂存
            if((BatteryStatus.OK == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_1).Type))
            {
                // 暂存0有
                if(BatteryStatus.OK == BufferBat(ModDef.Buffer_0).Type)
                {
                    curPos.SetData(MotorPosition.OffLoad_BufferPos_0, 1, 0, 0, ModDef.Finger_1, true);
                    return true;
                }
                // 暂存1有
                else if(BatteryStatus.OK == BufferBat(ModDef.Buffer_1).Type)
                {
                    curPos.SetData(MotorPosition.OffLoad_BufferPos_1, 2, 0, 0, ModDef.Finger_1, true);
                    return true;
                }
            }
            // 抓手1有，暂存有-》抓手0取暂存
            if((BatteryStatus.Invalid == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.OK == FingerBat(ModDef.Finger_1).Type))
            {
                // 暂存1有
                if(BatteryStatus.OK == BufferBat(ModDef.Buffer_1).Type)
                {
                    curPos.SetData(MotorPosition.OffLoad_BufferPos_2, 3, 0, 0, ModDef.Finger_0, true);
                    return true;
                }
                // 暂存0有
                else if(BatteryStatus.OK == BufferBat(ModDef.Buffer_0).Type)
                {
                    curPos.SetData(MotorPosition.OffLoad_BufferPos_1, 2, 0, 0, ModDef.Finger_0, true);
                    return true;
                }
            }
            // 抓手有1个，暂存无-》放暂存
            if((BatteryStatus.Invalid == BufferBat(ModDef.Buffer_0).Type) && (BatteryStatus.Invalid == BufferBat(ModDef.Buffer_1).Type))
            {
                if((BatteryStatus.OK == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_1).Type))
                {
                    curPos.SetData(MotorPosition.OffLoad_BufferPos_1, 2, 0, 0, ModDef.Finger_0, false);
                    return true;
                }
                else if((BatteryStatus.Invalid == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.OK == FingerBat(ModDef.Finger_1).Type))
                {
                    curPos.SetData(MotorPosition.OffLoad_BufferPos_1, 2, 0, 0, ModDef.Finger_1, false);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 计算下料线体放OK电池位置
        /// </summary>
        /// <param name="curPos"></param>
        /// <returns></returns>
        private bool CalcPlaceOffLoadLine(ref PickPlacePos curPos)
        {
            RunProcessOffloadLine run = this.offloadLine;
            if((null != run) && (EventStatus.Require == GetEvent(run, EventList.OffLoadLinePlaceBattery)))
            {
                if((BatteryStatus.OK == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.OK == FingerBat(ModDef.Finger_1).Type))
                {
                    curPos.SetData(MotorPosition.OffLoad_PlacePos, -1, 0, 0, ModDef.Finger_ALL, false);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region // 夹爪缓存数据

        /// <summary>
        /// 夹爪电池
        /// </summary>
        /// <param name="finger"></param>
        /// <returns></returns>
        private Battery FingerBat(ModDef finger)
        {
            if (finger < ModDef.Finger_0 || finger >= ModDef.Finger_ALL)
            {
                return null;
            }
            return this.Battery[(int)finger];
        }
        
        /// <summary>
        /// 夹爪电池计数
        /// </summary>
        /// <returns></returns>
        private int FingerBatCount()
        {
            int count = 0;
            for (ModDef i = ModDef.Finger_0; i < ModDef.Finger_ALL; i++)
            {
                if (FingerBat(i).Type > BatteryStatus.Invalid)
                {
                    count++;
                }
            }
            return count;
        }
        
        /// <summary>
        /// 缓存电池
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private Battery BufferBat(ModDef buffer)
        {
            if (buffer < ModDef.Buffer_0 || buffer >= ModDef.Buffer_ALL)
            {
                return null;
            }
            return this.Battery[(int)buffer];
        }

        /// <summary>
        /// 缓存位电池计数
        /// </summary>
        /// <returns></returns>
        private int BufferBatCount()
        {
            int count = 0;
            for (ModDef i = ModDef.Buffer_0; i < ModDef.Buffer_ALL; i++)
            {
                if (BufferBat(i).Type > BatteryStatus.Invalid)
                {
                    count++;
                }
            }
            return count;
        }
        
        #endregion

        #region // 气缸操作

        /// <summary>
        /// 旋转气缸动作
        /// </summary>
        /// <param name="push"></param>
        /// <returns></returns>
        private bool RotatePush(bool push, bool waitState)
        {
            //if(Def.IsNoHardware())
            {
                return true;
            }
            // 检查IO配置
            if((this.IRotatePush < 0) || (this.IRotatePull < 0))
            {
                return false;
            }
            // 操作
            OutputAction(this.ORotatePush, push);
            OutputAction(this.ORotatePull, !push);
            // 检查到位
            if(waitState)
            {
                if(!WaitInputState(IRotatePush, push) || !WaitInputState(IRotatePull, !push))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 夹爪有无电池
        /// </summary>
        /// <param name="finger"></param>
        /// <param name="hasBat"></param>
        /// <returns></returns>
        private bool FingerCheck(ModDef finger, bool hasBat)
        {
            if(Def.IsNoHardware() || this.DryRun)
            {
                return true;
            }
            for(int i = 0; i < IFingerBatCheck.Length; i++)
            {
                if(((ModDef)i == finger) || (ModDef.Finger_ALL == finger))
                {
                    if(!CheckInputState(IFingerBatCheck[i], hasBat))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 夹爪操作  false 为打开  true 为关闭
        /// </summary>
        /// <param name="finger"></param>
        /// <param name="close"></param>
        /// <returns></returns>
        private bool FingerClose(ModDef finger, bool close)
        {
            if(Def.IsNoHardware())
            {
                return true;
            }
            // 检查IO配置
            for(int i = 0; i < IFingerOpen.Length; i++)
            {
                if(((ModDef)i == finger) || (ModDef.Finger_ALL == finger))
                {
                    if(IFingerOpen[i] < 0 || IFingerClose[i] < 0 || OFingerOpen[i] < 0 || OFingerClose[i] < 0)
                    {
                        return false;
                    }
                }
            }
            // 操作
            for(int i = 0; i < IFingerOpen.Length; i++)
            {
                if(((ModDef)i == finger) || (ModDef.Finger_ALL == finger))
                {
                    OutputAction(OFingerClose[i], close);
                    OutputAction(OFingerOpen[i], !close);
                }
            }
            // 检查到位
            for(int i = 0; i < IFingerOpen.Length; i++)
            {
                if(((ModDef)i == finger) || (ModDef.Finger_ALL == finger))
                {
                    if(!(WaitInputState(IFingerClose[i], close) && WaitInputState(IFingerOpen[i], !close)))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 缓存电池检查
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="hasBat"></param>
        /// <returns></returns>
        private bool BufferCheck(ModDef buffer, bool hasBat)
        {
            if(Def.IsNoHardware())
            {
                return true;
            }
            for(int i = 0; i < IBufferCheck.Length; i++)
            {
                if(((ModDef.Buffer_0 + i) == buffer) || (ModDef.Buffer_ALL == buffer))
                {
                    if(!CheckInputState(IBufferCheck[i], hasBat))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        #endregion

        #region // 防呆检查

        /// <summary>
        /// 检查电机需要移动的范围是否合法
        /// </summary>
        /// <param name="motor"></param>
        /// <param name="fValue"></param>
        /// <param name="moveType"></param>
        /// <param name="minPos"></param>
        /// <param name="maxPos"></param>
        /// <returns></returns>
        private bool CheckMotorPosRange(Motor motor, float fValue, MotorMoveType moveType, double minPos, double maxPos)
        {
            float curPos = 0;
            float destPos = 0;
            if((int)MotorCode.MotorOK != motor.GetCurPos(ref curPos))
            {
                return false;
            }
            switch(moveType)
            {
                case MotorMoveType.MotorMoveHome:
                    return true;
                case MotorMoveType.MotorMoveForward:
                case MotorMoveType.MotorMoveBackward:
                    destPos = curPos + fValue;
                    break;
                case MotorMoveType.MotorMoveAbsMove:
                case MotorMoveType.MotorMoveLocation:
                    destPos = fValue;
                    break;
                default:
                    return false;
                    break;
            }
            if((minPos > destPos) || (destPos > maxPos))
            {
                string msg = $"{motor.Name}移动的目标位置为{destPos:0.00}，不在可移动范围{minPos:0.00}～{maxPos:0.00}内，无法移动";
                ShowMessageID((int)MsgID.MotorPosRangeErr, msg, "请检查位置是否可达到", MessageType.MsgAlarm);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 检查输出点位是否可操作
        /// </summary>
        /// <param name="output"></param>
        /// <param name="bOn"></param>
        /// <returns></returns>
        public override bool CheckOutputCanActive(Output output, bool bOn)
        {
            int outNum = -1;
            //夹爪有料，且在高位，禁止打开夹爪
            for (int i = 0; i < 2; i++)
            {
                if ((InputState(IFingerBatCheck[i], true) && OFingerOpen[i] > -1 && Outputs(OFingerOpen[i]) == output))
                {
                    outNum = OFingerOpen[i];
                }
            }
            if (outNum > -1)
            {
                bool inDown = false;
                for(MotorPosition i = MotorPosition.OffLoad_PickPltPos1; i <= MotorPosition.OffLoad_PlaceNG; i++)
                {
                    if(CheckMotorZPos(i))
                    {
                        inDown = true;
                        break;
                    }
                }
                if (!inDown)
                {
                    string msg = string.Format("Z轴不在下降位，夹爪禁止打开！！！");
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
            }
            // 夹爪非安全位禁止旋转
            outNum = -1;
            if(ORotatePush > -1 && Outputs(ORotatePush) == output)
            {
                outNum = ORotatePush;
            }
            else if(ORotatePull > -1 && Outputs(ORotatePull) == output)
            {
                outNum = ORotatePull;
            }
            if ((outNum > -1) && !CheckMotorZPos(MotorPosition.OffLoad_SafetyPos))
            {
                string msg = string.Format("Z轴不在安全位，夹爪禁止旋转操作！！！");
                ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                return false;
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
            #region // 大机器人正在取放

            string msg = "";

            RobotActionInfo rbtAction = MachineCtrl.GetInstance().GetRobotActionInfo(RunID.Transfer, false);
            if(null == rbtAction)
            {
                msg = string.Format("无法获取调度机器人当前动作，不能操作电机\r\n在【其它调试】界面重连模组客户端后再操作");
                ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                return false;
            }
            if((int)TransferRobotStation.OffloadStation == rbtAction.station)
            {
                if(RobotOrder.MOVE != rbtAction.order)
                {
                    msg = string.Format("调度机器人已在下料位取放夹具，不能操作电机\r\n在【机器人调试】界面将大机器人移动到当前工位移动位置后再操作");
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                    return false;
                }
            }
            #endregion

            #region // XYU轴移动检查

            MCState mc = MachineCtrl.GetInstance().RunsCtrl.GetMCState();
            // 电机Z轴不在在安全位，禁止操作XYU轴
            if(MotorX > -1 && Motors(MotorX) == motor)
            {
                if (!CheckMotorZPos(MotorPosition.OffLoad_SafetyPos))
                {
                    msg = string.Format("Z轴不在安全位，禁止操作X轴电机！！！");
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
                if ((mc != MCState.MCInitComplete) && (mc != MCState.MCStopRun))
                {

                }
                if (!CheckMotorPosRange(motor, fValue, moveType, 0, motorXMaxPos))
                {
                    return false;
                }
            }
            if(MotorY > -1 && Motors(MotorY) == motor)
            {
                if(!CheckMotorZPos(MotorPosition.OffLoad_SafetyPos))
                {
                    msg = string.Format("Z轴不在安全位，禁止操作Y轴电机！！！");
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
                if(!CheckMotorPosRange(motor, fValue, moveType, 0, motorYMaxPos))
                {
                    return false;
                }
            }
            if(MotorU > -1 && Motors(MotorU) == motor)
            {
                if(!CheckMotorZPos(MotorPosition.OffLoad_SafetyPos))
                {
                    msg = string.Format("Z轴不在安全位，禁止操作U轴电机！！！");
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
                if(!CheckMotorPosRange(motor, fValue, moveType, 0, motorUMaxPos))
                {
                    return false;
                }
            }
            #endregion

            #region // 构造动作信息
            if(((MotorX > -1 && Motors(MotorX) == motor) || (MotorY > -1 && Motors(MotorY) == motor) || (MotorZ > -1 && Motors(MotorZ) == motor)) && (-1 == nLocation))
            {
                string stationName = "";
                float ZstationPos = 0.0f;
                Motors(MotorZ).GetLocation((int)MotorPosition.OffLoad_SafetyPos, ref stationName, ref ZstationPos);
                this.XYZDebugAction.SetData((int)MotorPosition.OffLoad_SafetyPos, 0, 0, RobotOrder.MOVE, stationName);
            }

            if (MotorX > -1 && Motors(MotorX) == motor)
            {
                string stationName = "";
                float ZstationPos = 0.0f;
                if ((int)MotorPosition.OffLoad_SafetyPos == nLocation)
                {
                    if (CheckMotorPos(this.MotorY, MotorPosition.OffLoad_SafetyPos, false) && CheckMotorPos(this.MotorZ, MotorPosition.OffLoad_SafetyPos, false))
                    {
                        Motors(MotorZ).GetLocation((int)MotorPosition.OffLoad_SafetyPos, ref stationName, ref ZstationPos);
                        this.XYZDebugAction.SetData((int)MotorPosition.OffLoad_SafetyPos, 0, 0, RobotOrder.HOME, stationName);
                    }
                }
                else
                {
                    Motors(MotorZ).GetLocation((int)MotorPosition.OffLoad_SafetyPos, ref stationName, ref ZstationPos);
                    this.XYZDebugAction.SetData((int)MotorPosition.OffLoad_SafetyPos, 0, 0, RobotOrder.MOVE, stationName);
                }
            }

            if (MotorY > -1 && Motors(MotorY) == motor)
            {
                string stationName = "";
                float ZstationPos = 0.0f;
                if ((int)MotorPosition.OffLoad_SafetyPos == nLocation)
                {
                    if (CheckMotorPos(this.MotorX, MotorPosition.OffLoad_SafetyPos, false) && CheckMotorPos(this.MotorZ, MotorPosition.OffLoad_SafetyPos, false))
                    {
                        Motors(MotorZ).GetLocation((int)MotorPosition.OffLoad_SafetyPos, ref stationName, ref ZstationPos);
                        this.XYZDebugAction.SetData((int)MotorPosition.OffLoad_SafetyPos, 0, 0, RobotOrder.HOME, stationName);
                    }
                }
                else
                {
                    Motors(MotorZ).GetLocation((int)MotorPosition.OffLoad_SafetyPos, ref stationName, ref ZstationPos);
                    this.XYZDebugAction.SetData((int)MotorPosition.OffLoad_SafetyPos, 0, 0, RobotOrder.MOVE, stationName);
                }
            }
            #endregion

            #region // Z轴移动检查
            if(MotorZ > -1 && Motors(MotorZ) == motor)
            {
                if(!CheckMotorPosRange(motor, fValue, moveType, 0, motorZMaxPos))
                {
                    return false;
                }
                string stationName = "";
                float ZstationPos = 0.0f;

                if ((int)MotorPosition.OffLoad_SafetyPos == nLocation)
                {
                    if (CheckMotorPos(this.MotorX, MotorPosition.OffLoad_SafetyPos, false) && CheckMotorPos(this.MotorY, MotorPosition.OffLoad_SafetyPos, false))
                    {
                        Motors(MotorZ).GetLocation((int)MotorPosition.OffLoad_SafetyPos, ref stationName, ref ZstationPos);
                        this.XYZDebugAction.SetData((int)MotorPosition.OffLoad_SafetyPos, 0, 0, RobotOrder.HOME, stationName);
                    }
                    else
                    {
                        Motors(MotorZ).GetLocation((int)MotorPosition.OffLoad_SafetyPos, ref stationName, ref ZstationPos);
                        this.XYZDebugAction.SetData((int)MotorPosition.OffLoad_SafetyPos, 0, 0, RobotOrder.UP, stationName);
                    }
                }
                else
                {
                    if (nLocation > -1)
                    {
                        Motors(MotorZ).GetLocation(nLocation, ref stationName, ref ZstationPos);
                        this.XYZDebugAction.SetData(nLocation, 0, 0, RobotOrder.DOWN, stationName);
                    }
                    else
                    {
                        this.XYZDebugAction.order = RobotOrder.DOWN;
                    }

                    if((MotorMoveType.MotorMoveBackward == moveType)
                        || (MotorMoveType.MotorMoveHome == moveType)
                        || ((MotorMoveType.MotorMoveLocation == moveType) && (nLocation == (int)MotorPosition.CoolingOffload_SafetyPos)))
                    {
                        return true;
                    }
                    else if (MotorMoveType.MotorMoveLocation == moveType)
                    {
                        string posName;
                        posName = msg = "";
                        float posValue, curValue;
                        posValue = curValue = 0.0f;
                        int[] mtr = { MotorX, MotorY, MotorU };
                        float[] fOffset = { (float)pickPosXDis, (float)pickPosYDis, 1.0f };
                        if ((int)MotorPosition.OffLoad_PlacePos == nLocation)
                        {
                            fOffset[0] = (float)placePosXDis;
                            fOffset[1] = 1.0f;
                        }
                        for(int i = 0; i < mtr.Length; i++)
                        {
                            if((mtr[i] > -1) && (int)MotorCode.MotorOK == Motors(mtr[i]).GetCurPos(ref curValue))
                            {
                                Motors(mtr[i]).GetLocation(nLocation, ref posName, ref posValue);
                                if(Math.Abs(Math.Abs((curValue - posValue) % fOffset[i]) - Math.Abs(fOffset[i])) > Motors(mtr[i]).PosErrRange
                                    && Math.Abs((curValue - posValue) % fOffset[i]) > Motors(mtr[i]).PosErrRange)
                                {
                                    msg = string.Format("{0}】不在[{1} {2}]位置或此位置的偏移位置！\r\n不能操作Z轴下降到[{1} {2}]！"
                                            , Motors(mtr[i]).Name, nLocation, posName);
                                    ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                    return false;
                                }
                            }
                        }
                        int input = ((int)MotorPosition.OffLoad_PlacePos == nLocation) ? IRotatePush : IRotatePull;
                        if(!InputState(input, true))
                        {
                            msg = string.Format("{0} {1}】感应器非ON，不能操作Z轴下降！"
                                , Inputs(input).Num, Inputs(input).Name);
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            return false;
                        }
                    }
                    else if((MotorMoveType.MotorMoveAbsMove == moveType)
                        || (MotorMoveType.MotorMoveForward == moveType))
                    {
                        if(fValue > 10.0)
                        {
                            ShowMsgBox.ShowDialog("非点位移动，Z轴不能一次下降超过[10.0mm]！", MessageType.MsgAlarm);
                            return false;
                        }
                    }
                }
            }
            #endregion

            return true;
        }

        /// <summary>
        /// 模组防呆监视：请勿加入耗时过长或阻塞操作
        /// </summary>
        public override void MonitorAvoidDie()
        {
            // 下料防撞感应器异常
            if (this.motorNeedStop && !InputState(this.IFingerDelay, false))
            {
                this.motorNeedStop = false;
                MachineCtrl.GetInstance().RunsCtrl.StopAllMotor();
                string msg = string.Format("{0} {1} 感应器ON，夹爪防撞被触发", Inputs(IFingerDelay).Num, Inputs(IFingerDelay).Name);
                ShowMessageID((int)MsgID.MotorDelayStop, msg, "请人工处理撞机问题", MessageType.MsgAlarm);
            }
            else if (!this.motorNeedStop && !InputState(IFingerDelay, true))
            {
                this.motorNeedStop = true;
            }
        }

        /// <summary>
        /// 设备停止后操作
        /// </summary>
        public override void AfterStopAction()
        {
            this.AutoCheckStep = CheckSteps.Check_WorkStart;
        }

        #endregion
        
        #region // 添加删除夹具

        public override void ManualAddPallet(int pltIdx, int maxRow, int maxCol, PalletStatus pltState, BatteryStatus batState)
        {
            if((PalletStatus.OK == pltState) && (PalletStatus.Detect != this.Pallet[pltIdx].State))
            {
                this.Pallet[pltIdx].State = PalletStatus.OK;
                this.Pallet[pltIdx].SetRowCol(maxRow, maxCol);
                SaveRunData(SaveType.Pallet, pltIdx);
            }
        }

        public override void ManualClearPallet(int pltIdx)
        {
            if (PalletStatus.Detect != this.Pallet[pltIdx].State)
            {
                this.Pallet[pltIdx].Release();
                SaveRunData(SaveType.Pallet, pltIdx);
            }
        }
        #endregion

        #region // 上传Mes数据

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
                this.nextAutoStep = AutoSteps.Auto_WaitWorkStart;
                SaveRunData(SaveType.AutoStep | SaveType.Robot);
            }
        }
        #endregion
    }
}

