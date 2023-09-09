using HelperLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;
using log4net;

namespace Machine
{
    /// <summary>
    /// 调度机器人
    /// </summary>
    class RunProcessRobotTransfer : RunProcess
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        #region // 枚举：步骤，模组数据，报警列表

        protected new enum InitSteps
        {
            Init_DataRecover = 0,

            Init_CheckFinger,
            Init_RobotConnect,

            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,

            // 预设置取放响应信号
            Auto_PrsetPickEvent,
            Auto_PrsetPlaceEvent,

            // 计算取夹具位置
            Auto_CalcPickPalletPos,

            #region // 取

            // 取：上料区
            Auto_PickOnloadMove,
            Auto_PickOnloadSetEvent,
            Auto_PickOnloadIn,
            Auto_PickOnloadGetData,
            Auto_PickOnloadOut,
            Auto_PickOnloadCheckFinger,

            // 取：缓存架
            Auto_PickBufferSetEvent,
            Auto_PickBufferMove,
            Auto_PickBufferIn,
            Auto_PickBufferGetData,
            Auto_PickBufferOut,
            Auto_PickBufferCheckFinger,

            // 取：人工操作台
            Auto_PickManualSetEvent,
            Auto_PickManualMove,
            Auto_PickManualIn,
            Auto_PickManualGetData,
            Auto_PickManualOut,
            Auto_PickManualCheckFinger,

            // 取：干燥炉
            Auto_PickDryOvenSetEvent,
            Auto_PickDryOvenMove,
            Auto_PickDryOvenIn,
            Auto_PickDryOvenGetData,
            Auto_PickDryOvenOut,
            Auto_PickDryOvenCheckFinger,

            // 取：下料区
            Auto_PickOffloadMove,
            Auto_PickOffloadSetEvent,
            Auto_PickOffloadIn,
            Auto_PickOffloadGetData,
            Auto_PickOffloadOut,
            Auto_PickOffloadCheckFinger,

            #endregion

            // 计算放夹具位置
            Auto_CalcPlacePalletPos,

            #region // 放

            // 放：上料区
            Auto_PlaceOnloadMove,
            Auto_PlaceOnloadSetEvent,
            Auto_PlaceOnloadIn,
            Auto_PlaceOnloadSetData,
            Auto_PlaceOnloadOut,
            Auto_PlaceOnloadCheckFinger,

            // 放：缓存架
            Auto_PlaceBufferSetEvent,
            Auto_PlaceBufferMove,
            Auto_PlaceBufferIn,
            Auto_PlaceBufferSetData,
            Auto_PlaceBufferOut,
            Auto_PlaceBufferCheckFinger,

            // 放：人工操作台
            Auto_PlaceManualSetEvent,
            Auto_PlaceManualMove,
            Auto_PlaceManualIn,
            Auto_PlaceManualSetData,
            Auto_PlaceManualOut,
            Auto_PlaceManualCheckFinger,

            // 放：干燥炉
            Auto_PlaceDryOvenSetEvent,
            Auto_PlaceDryOvenMove,
            Auto_PlaceDryOvenIn,
            Auto_PlaceDryOvenSetData,
            Auto_PlaceDryOvenOut,
            Auto_PlaceDryOvenCheckFinger,

            // 放：下料区
            Auto_PlaceOffloadMove,
            Auto_PlaceOffloadSetEvent,
            Auto_PlaceOffloadIn,
            Auto_PlaceOffloadSetData,
            Auto_PlaceOffloadOut,
            Auto_PlaceOffloadCheckFinger,

            #endregion

            #region // 转炉换腔

            // 转炉换腔取：干燥炉
            Auto_TransferPickDryOvenSetEvent,
            Auto_TransferPickDryOvenMove,
            Auto_TransferPickDryOvenIn,
            Auto_TransferPickDryOvenGetData,
            Auto_TransferPickDryOvenOut,
            Auto_TransferPickDryOvenCheckFinger,

            // 转炉换腔放：干燥炉
            Auto_TransferPlaceDryOvenSetEvent,
            Auto_TransferPlaceDryOvenMove,
            Auto_TransferPlaceDryOvenIn,
            Auto_TransferPlaceDryOvenSetData,
            Auto_TransferPlaceDryOvenOut,
            Auto_TransferPlaceDryOvenCheckFinger,
            
            #endregion

            Auto_WorkEnd,
        }

        private enum ModDef
        {

            // 【干燥炉匹配模式】
            //******* 放治具 *******
            PlaceSameAndInvalid = 0,     // 同类型 && 无效
            PlaceInvalidAndInvalid,      // 无效 && 无效
            PlaceInvalidAndOther,        // 无效 && 其他
            PlaceEnd,
            //******* 取治具 *******
            PickSameAndInvalid,         // 同类型 && 无效
            //PickSameAndNotSame,         // 同类型 && !同类型
            PickSameAndOther,           // 同类型 && 其他
            PickEnd,

        }

        private enum MsgID
        {
            Start = ModuleMsgID.RobotTransferMsgStartID,
            RbtActionChange,
            SendRbtMoveCmd,
            RbtMoveCmdError,
            RbtMoveTimeout,
            SafeDoorOpenRbtEStop,
            DestStationStop,
            DestStationSenserErr,
            FingerPltStateErr,
            OvenDoorStateErr,
            OvenSafetyCurtainErrr,

        }
        #endregion

        #region // 取放位置结构体

        private struct PickPlacePos
        {
            #region // 字段
            public RunID runID;
            public TransferRobotStation station;
            public int row;
            public int col;
            public EventList stationEvent;
            #endregion

            #region // 方法

            public void SetData(RunID runId, TransferRobotStation curStation, int curRow, int curCol, EventList curEvent)
            {
                this.runID = runId;
                this.station = curStation;
                this.row = curRow;
                this.col = curCol;
                this.stationEvent = curEvent;
            }

            public void Release()
            {
                this.runID = RunID.Invalid;
                this.station = TransferRobotStation.InvalidStatioin;
                this.row = -1;
                this.col = -1;
                this.stationEvent = EventList.Invalid;
            }
            #endregion
        };

        #endregion

        #region // 字段，属性

        #region // IO

        private int[] IPalletKeepFlat;        // 夹具放平检测
        private int[] IFingerFrontCheck;      // 插料架前检测
        private int[] IFingerSideCheck;       // 插料架侧检测
        private int IRobotPeLimit;            // 机器人地轨正限位
        private int IRobotNeLimit;            // 机器人地轨负限位
        private int IRobotRunning;            // 机器人运行中输入

        private int ORobotEStop;              // 机器人急停
        private int ORobotOil;                // 机器人加导轨油

        #endregion

        #region // 电机
        #endregion

        #region // ModuleEx.cfg配置

        public RobotIndexID RobotID { get; private set; }   // 机器人ID

        private Dictionary<RunID, bool> placeDryOvenOrder;              // 干燥炉放满电池夹具的顺序

        #endregion

        #region // 模组参数

        public int RobotLowSpeed { get; private set; }      // 机器人低速速度：1-80，用以手动调试

        private int robotSpeed;                             // 机器人速度：1-100
        private bool robotEnable;                           // 机器人使能：TRUE启用，FALSE禁用
        private int robotDelay;                             // 机器人防呆时间(s)
        private string robotIP;                             // 机器人IP
        private int robotPort;                              // 机器人IP的Port
        private int offloadDetect;                          // 下待测夹具比例：下料夹具下N次后下一次待测夹具
        private int robotOilRate;                           // 机器人加导轨油频率

        #endregion

        #region // 模组数据

        public bool RobotRunning { get; private set; }      // 机器人运行中

        private PickPlacePos pickPos;                       // 取位置
        private PickPlacePos placePos;                      // 放位置
        private int[] robotCmd;                             // 机器人指令
        private RobotActionInfo robotAutoAction;            // 机器人自动动作信息
        private RobotActionInfo robotDebugAction;           // 机器人手动调试动作信息
        private RobotClient robotClient;                    // 机器人通讯
        private int offloadPltCount;                        // 下正常夹具计数
        private int robotOilCount;                          // 机器人加导轨油的运动计数

        private Dictionary<TransferRobotStation, RobotFormula> robotStationInfo;  // 机器人工位信息
        private Dictionary<string, RobotActionInfo> testRbtStation;

        #endregion

        #endregion

        public RunProcessRobotTransfer(int runId) : base(runId)
        {
            InitBatteryPalletSize(0, (int)ModuleMaxPallet.TransferRobot);

            PowerUpRestart();
            
            InitParameter();
            // 参数
            InsertVoidParameter("robotEnable", "机器人使能", "机器人使能：TRUE启用，FALSE禁用", robotEnable, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("robotSpeed", "机器人速度", "机器人速度：1-100", robotSpeed, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("RobotLowSpeed", "机器人手动速度", "机器人手动调试速度：1-80", RobotLowSpeed, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("robotDelay", "机器人防呆", "机器人防呆时间(s)", robotDelay, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("robotIP", "机器人IP", "机器人IP", robotIP, RecordType.RECORD_STRING, ParameterLevel.PL_STOP_ADMIN);
            InsertVoidParameter("robotPort", "机器人端口", "机器人IP的Port", robotPort, RecordType.RECORD_INT, ParameterLevel.PL_STOP_ADMIN);
            InsertVoidParameter("offloadDetect", "下待测夹具比例", "下待测夹具比例：下料夹具下N次后下一次待测夹具", offloadDetect, RecordType.RECORD_INT, ParameterLevel.PL_STOP_OPER);
            InsertVoidParameter("robotOilRate", "加导轨油频率", "机器人加导轨油的频率，即运动N次加油一次", robotOilRate, RecordType.RECORD_INT, ParameterLevel.PL_STOP_OPER);

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
                        this.nextInitStep = InitSteps.Init_CheckFinger;
                        break;
                    }
                case InitSteps.Init_CheckFinger:
                    {
                        CurMsgStr("检查插料架状态", "Check finger sensor");
                        if (PalletKeepFlat(0, (this.Pallet[0].State > PalletStatus.Invalid), true))
                        {
                            this.nextInitStep = InitSteps.Init_RobotConnect;
                        }
                        break;
                    }

                case InitSteps.Init_RobotConnect:
                    {
                        CurMsgStr("连接机器人", "Connect robot");
                        if (RobotConnect(true))
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
                Sleep(500);
            }

            #region // 自动步骤开始前的检查步骤

            switch((CheckSteps)this.AutoCheckStep)
            {
                case CheckSteps.Check_WorkStart:
                    {
                        CurMsgStr("检查机器人位置", "Check robot pos");
                        if(!CheckRobotPos(robotAutoAction, robotDebugAction))
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

                        bool result = false;
                        if (CalcTransferDryOven(ref this.pickPos, ref this.placePos))
                        {
                            result = true;
                        }
                        else if (CalcOnloadPlace(ref this.pickPos, ref this.placePos))
                        {
                            result = true;
                        }
                        else if(CalcOnloadPick(ref this.pickPos, ref this.placePos))
                        {
                            result = true;
                        }
                        else if(CalcOffloadPlace(ref this.pickPos, ref this.placePos))
                        {
                            result = true;
                        }
                        else if(CalcOffloadPick(ref this.pickPos, ref this.placePos))
                        {
                            result = true;
                        }
                        else if(CalcManualPick(ref this.pickPos, ref this.placePos))
                        {
                            result = true;
                        }
                        else if(CalcManualPlace(ref this.pickPos, ref this.placePos))
                        {
                            result = true;
                        }

                        if (result)
                        {
                            this.nextAutoStep = AutoSteps.Auto_PrsetPickEvent;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            break;
                        }

                        break;
                    }

                #region // 预设置取放响应信号
                case AutoSteps.Auto_PrsetPickEvent:
                    {
                        this.msgChs = string.Format("预设置[{0}-{1}-{2}]取料响应信号", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Prseet [{0}-{1}-{2}] pick event", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if (PresetModuleEvent(this.pickPos, EventStatus.Response))
                        {
                            this.nextAutoStep = AutoSteps.Auto_PrsetPlaceEvent;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_PrsetPlaceEvent:
                    {
                        this.msgChs = string.Format("预设置[{0}-{1}-{2}]放料响应信号", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Prseet [{0}-{1}-{2}] place event", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(PresetModuleEvent(this.placePos, EventStatus.Response))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPickPalletPos;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                #endregion

                #region // 计算取夹具位置
                case AutoSteps.Auto_CalcPickPalletPos:
                    {
                        CurMsgStr("计算取夹具位", "Calc pick pallet pos");
                        switch(this.pickPos.stationEvent)
                        {
                            case EventList.OnloadPickNGEmptyPallet:
                            case EventList.OnloadPickOKFullPallet:
                            case EventList.OnloadPickOKFakeFullPallet:
                            case EventList.OnLoadPickWaitResultPallet:
                            case EventList.OnloadPickRebakeFakePallet:
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PickOnloadMove;
                                    SaveRunData(SaveType.AutoStep);
                                    break;
                                }
                            case EventList.ManualPickEmptyPallet:
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PickManualSetEvent;
                                    SaveRunData(SaveType.AutoStep);
                                    break;
                                }
                            case EventList.PalletBufferPickEmptyPallet:
                            case EventList.PalletBufferPickNGEmptyPallet:
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PickBufferSetEvent;
                                    SaveRunData(SaveType.AutoStep);
                                    break;
                                }
                            case EventList.DryOvenPickEmptyPallet:
                            case EventList.DryOvenPickNGPallet:
                            case EventList.DryOvenPickNGEmptyPallet:
                            case EventList.DryOvenPickDetectFakePallet:
                            case EventList.DryOvenPickReputFakePallet:
                            case EventList.DryOvenPickDryFinishPallet:
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PickDryOvenSetEvent;
                                    SaveRunData(SaveType.AutoStep);
                                    break;
                                }
                            case EventList.DryOvenPickTransferPallet:
                                {
                                    this.nextAutoStep = AutoSteps.Auto_TransferPickDryOvenSetEvent;
                                    SaveRunData(SaveType.AutoStep);
                                    break;
                                }
                            case EventList.OffLoadPickEmptyPallet:
                            case EventList.OffLoadPickWaitResultPallet:
                            case EventList.OffLoadPickRebakeFakePallet:
                            case EventList.OffLoadPickNGPallet:
                            case EventList.OffLoadPickNGEmptyPallet:
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PickOffloadMove;
                                    SaveRunData(SaveType.AutoStep);
                                    break;
                                }
                            default:
                                break;
                        }
                        break;
                    }
                #endregion

                #region // 取：上料区
                case AutoSteps.Auto_PickOnloadMove:
                    {
                        this.msgChs = string.Format("机器人移动到[{0}-{1}-{2}]", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot move to [{0}-{1}-{2}]", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.MOVE, ref robotCmd))
                        {
                            if (RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PickOnloadSetEvent;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PickOnloadSetEvent:
                    {
                        this.msgChs = string.Format("机器人设置[{0}-{1}-{2}]响应信号", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot set [{0}-{1}-{2}] station event", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        int pltIdx = -1;
                        if (EventStatus.Require == GetModuleEvent(pickPos.runID, pickPos.stationEvent, ref pltIdx))
                        {
                            if(SetModuleEvent(pickPos.runID, pickPos.stationEvent, EventStatus.Response, pickPos.col))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PickOnloadIn;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PickOnloadIn:
                    {
                        this.msgChs = string.Format("机器人到[{0}-{1}-{2}]取进", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot pickin [{0}-{1}-{2}]", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        int pltPos = -1;
                        EventStatus state = GetModuleEvent(pickPos.runID, pickPos.stationEvent, ref pltPos);
                        if(((EventStatus.Ready == state) || (EventStatus.Start == state)) && (pltPos == pickPos.col))
                        {
                            if (CheckStationSafe(pickPos, RobotOrder.PICKIN))
                            {
                                SetModuleEvent(pickPos.runID, pickPos.stationEvent, EventStatus.Start, pltPos);
                                if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.PICKIN, ref robotCmd)
                                    && RobotMove(robotCmd, true))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PickOnloadGetData;
                                    SaveRunData(SaveType.AutoStep);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PickOnloadGetData:
                    {
                        this.msgChs = string.Format("机器人获取[{0}-{1}-{2}]数据", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot get [{0}-{1}-{2}] station data", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        Pallet tmpPlt = new Pallet();
                        if (GetModulePallet(pickPos.runID, pickPos.col, ref tmpPlt))
                        {
                            tmpPlt.SrcStation = (int)pickPos.station;
                            tmpPlt.SrcRow = pickPos.row;
                            tmpPlt.SrcCol = pickPos.col;
                            this.Pallet[0].Copy(tmpPlt);
                            tmpPlt.Release();
                            if(SetModulePallet(pickPos.runID, pickPos.col, tmpPlt))
                            {
                                SavePickPlacePalletData(this.pickPos, true, this.Pallet[0]);
                                this.nextAutoStep = AutoSteps.Auto_PickOnloadOut;
                                SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PickOnloadOut:
                    {
                        this.msgChs = string.Format("机器人到[{0}-{1}-{2}]取出", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot pickout [{0}-{1}-{2}]", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.PICKOUT, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PickOnloadCheckFinger;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PickOnloadCheckFinger:
                    {
                        this.msgChs = string.Format("机器人在[{0}-{1}-{2}]取料后检查", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Check [{0}-{1}-{2}] station pallet", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if (PalletKeepFlat(0, true))
                        {
                            if (SetModuleEvent(pickPos.runID, pickPos.stationEvent, EventStatus.Finished, pickPos.col))
                            {
                                this.nextAutoStep = AutoSteps.Auto_CalcPlacePalletPos;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                #endregion

                #region // 取：缓存架
                case AutoSteps.Auto_PickBufferSetEvent:
                    {
                        this.msgChs = string.Format("机器人设置[{0}-{1}-{2}]响应信号", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot set [{0}-{1}-{2}] station event", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        //if(SetModuleEvent(pickPos.runID, pickPos.stationEvent, EventStatus.Response, pickPos.col))
                        {
                            this.nextAutoStep = AutoSteps.Auto_PickBufferMove;
                            //SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_PickBufferMove:
                    {
                        this.msgChs = string.Format("机器人移动到[{0}-{1}-{2}]", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot move to [{0}-{1}-{2}]", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.MOVE, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PickBufferIn;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PickBufferIn:
                    {
                        this.msgChs = string.Format("机器人到[{0}-{1}-{2}]取进", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot pickin [{0}-{1}-{2}]", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        int pltPos = -1;
                        EventStatus state = GetModuleEvent(pickPos.runID, pickPos.stationEvent, ref pltPos);
                        if(((EventStatus.Ready == state) || (EventStatus.Start == state)) && (pltPos == (pickPos.row)))
                        {
                            if(CheckStationSafe(pickPos, RobotOrder.PICKIN))
                            {
                                SetModuleEvent(pickPos.runID, pickPos.stationEvent, EventStatus.Start, pltPos);
                                if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.PICKIN, ref robotCmd)
                                    && RobotMove(robotCmd, true))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PickBufferGetData;
                                    SaveRunData(SaveType.AutoStep);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PickBufferGetData:
                    {
                        this.msgChs = string.Format("机器人获取[{0}-{1}-{2}]数据", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot get [{0}-{1}-{2}] station data", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        Pallet tmpPlt = new Pallet();
                        if(GetModulePallet(pickPos.runID, (pickPos.row), ref tmpPlt))
                        {
                            this.Pallet[0].Copy(tmpPlt);
                            tmpPlt.Release();
                            if(SetModulePallet(pickPos.runID, (pickPos.row), tmpPlt))
                            {
                                SavePickPlacePalletData(this.pickPos, true, this.Pallet[0]);
                                this.nextAutoStep = AutoSteps.Auto_PickBufferOut;
                                SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PickBufferOut:
                    {
                        this.msgChs = string.Format("机器人到[{0}-{1}-{2}]取出", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot pickout [{0}-{1}-{2}]", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.PICKOUT, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PickBufferCheckFinger;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PickBufferCheckFinger:
                    {
                        this.msgChs = string.Format("机器人在[{0}-{1}-{2}]取料后检查", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Check [{0}-{1}-{2}] station pallet", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(PalletKeepFlat(0, true))
                        {
                            if(SetModuleEvent(pickPos.runID, pickPos.stationEvent, EventStatus.Finished, (pickPos.row)))
                            {
                                this.nextAutoStep = AutoSteps.Auto_CalcPlacePalletPos;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                #endregion

                #region // 取：人工操作台
                case AutoSteps.Auto_PickManualSetEvent:
                    {
                        this.msgChs = string.Format("机器人设置[{0}-{1}-{2}]响应信号", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot set [{0}-{1}-{2}] station event", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        //if(SetModuleEvent(pickPos.runID, pickPos.stationEvent, EventStatus.Response, pickPos.col))
                        {
                            this.nextAutoStep = AutoSteps.Auto_PickManualMove;
                            //SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_PickManualMove:
                    {
                        this.msgChs = string.Format("机器人移动到[{0}-{1}-{2}]", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot move to [{0}-{1}-{2}]", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.MOVE, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PickManualIn;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PickManualIn:
                    {
                        this.msgChs = string.Format("机器人到[{0}-{1}-{2}]取进", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot pickin [{0}-{1}-{2}]", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        int pltPos = -1;
                        EventStatus state = GetModuleEvent(pickPos.runID, pickPos.stationEvent, ref pltPos);
                        if(((EventStatus.Ready == state) || (EventStatus.Start == state)) && (pltPos == pickPos.col))
                        {
                            if(CheckStationSafe(pickPos, RobotOrder.PICKIN))
                            {
                                SetModuleEvent(pickPos.runID, pickPos.stationEvent, EventStatus.Start, pltPos);
                                if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.PICKIN, ref robotCmd)
                                    && RobotMove(robotCmd, true))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PickManualGetData;
                                    SaveRunData(SaveType.AutoStep);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PickManualGetData:
                    {
                        this.msgChs = string.Format("机器人获取[{0}-{1}-{2}]数据", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot get [{0}-{1}-{2}] station data", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        Pallet tmpPlt = new Pallet();
                        if(GetModulePallet(pickPos.runID, pickPos.col, ref tmpPlt))
                        {
                            this.Pallet[0].Copy(tmpPlt);
                            tmpPlt.Release();
                            if(SetModulePallet(pickPos.runID, pickPos.col, tmpPlt))
                            {
                                SavePickPlacePalletData(this.pickPos, true, this.Pallet[0]);
                                this.nextAutoStep = AutoSteps.Auto_PickManualOut;
                                SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PickManualOut:
                    {
                        this.msgChs = string.Format("机器人到[{0}-{1}-{2}]取出", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot pickout [{0}-{1}-{2}]", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.PICKOUT, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PickManualCheckFinger;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PickManualCheckFinger:
                    {
                        this.msgChs = string.Format("机器人在[{0}-{1}-{2}]取料后检查", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Check [{0}-{1}-{2}] station pallet", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(PalletKeepFlat(0, true))
                        {
                            if(SetModuleEvent(pickPos.runID, pickPos.stationEvent, EventStatus.Finished, pickPos.col))
                            {
                                this.nextAutoStep = AutoSteps.Auto_CalcPlacePalletPos;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                #endregion

                #region // 取：干燥炉
                case AutoSteps.Auto_PickDryOvenSetEvent:
                    {
                        this.msgChs = string.Format("机器人设置[{0}-{1}-{2}]响应信号", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot set [{0}-{1}-{2}] station event", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        //if(SetModuleEvent(pickPos.runID, pickPos.stationEvent, EventStatus.Response, pickPos.col))
                        {
                            this.nextAutoStep = AutoSteps.Auto_PickDryOvenMove;
                            //SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_PickDryOvenMove:
                    {
                        this.msgChs = string.Format("机器人移动到[{0}-{1}-{2}]", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot move to [{0}-{1}-{2}]", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.MOVE, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PickDryOvenIn;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PickDryOvenIn:
                    {
                        this.msgChs = string.Format("机器人到[{0}-{1}-{2}]取进", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot pickin [{0}-{1}-{2}]", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        int pltPos = -1;
                        EventStatus state = GetModuleEvent(pickPos.runID, pickPos.stationEvent, ref pltPos);
                        if(((EventStatus.Ready == state) || (EventStatus.Start == state)) 
                            && (pltPos == (pickPos.row * (int)OvenRowCol.MaxCol + pickPos.col)))
                        {
                            if(CheckStationSafe(pickPos, RobotOrder.PICKIN))
                            {
                                SetModuleEvent(pickPos.runID, pickPos.stationEvent, EventStatus.Start, pltPos);
                                if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.PICKIN, ref robotCmd)
                                    && RobotMove(robotCmd, true))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PickDryOvenGetData;
                                    SaveRunData(SaveType.AutoStep);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PickDryOvenGetData:
                    {
                        this.msgChs = string.Format("机器人获取[{0}-{1}-{2}]数据", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot get [{0}-{1}-{2}] station data", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        Pallet tmpPlt = new Pallet();
                        string msg="";
                        int pltIdx = (this.pickPos.row * (int)OvenRowCol.MaxCol + this.pickPos.col);
                        if(GetModulePallet(pickPos.runID, pltIdx, ref tmpPlt))
                        {
                            this.Pallet[0].Copy(tmpPlt);
                            tmpPlt.Release();
                            if(SetModulePallet(pickPos.runID, pltIdx, tmpPlt))
                            {
                                // 需要转盘的NG夹具，先解绑
                                switch(this.Pallet[0].State)
                                {
                                    case PalletStatus.NG:
                                        if (!this.Pallet[0].IsEmpty())
                                        {
                                            for (int i = 0; i < 3; i++)
                                            {
                                                //托盘解绑
                                                if (!MachineCtrl.GetInstance().MesUnbindPalletInfo(this.Pallet[0].Code, MesResources.Group,ref msg))
                                                {
                                                    //获取报警字符串是否包含"超时"二字,如果没有则跳出循环
                                                    if (!msg.Contains("超时"))
                                                    {
                                                        break;
                                                    }
                                                    if (i == 2)
                                                    {
                                                        ShowMsgBox.ShowDialog($"MES夹具解绑接口超时失败3次，请检查MES连接状态", MessageType.MsgAlarm);
                                                    }
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            }

                                            //MachineCtrl.GetInstance().MesUnbindPalletInfo(this.Pallet[0].Code, MesResources.Group);
                                        }
                                        break;
                                }
                                SavePickPlacePalletData(this.pickPos, true, this.Pallet[0]);
                                this.Pallet[0].SrcStation = (int)pickPos.station;
                                this.Pallet[0].SrcRow = pickPos.row;
                                this.Pallet[0].SrcCol = pickPos.col;
                                this.nextAutoStep = AutoSteps.Auto_PickDryOvenOut;
                                SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PickDryOvenOut:
                    {
                        this.msgChs = string.Format("机器人到[{0}-{1}-{2}]取出", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot pickout [{0}-{1}-{2}]", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.PICKOUT, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PickDryOvenCheckFinger;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PickDryOvenCheckFinger:
                    {
                        this.msgChs = string.Format("机器人在[{0}-{1}-{2}]取料后检查", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Check [{0}-{1}-{2}] station pallet", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(PalletKeepFlat(0, true))
                        {
                            int pltIdx = (this.pickPos.row * (int)OvenRowCol.MaxCol + this.pickPos.col);
                            if(SetModuleEvent(pickPos.runID, pickPos.stationEvent, EventStatus.Finished, pltIdx))
                            {
                                this.nextAutoStep = AutoSteps.Auto_CalcPlacePalletPos;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                #endregion

                #region // 取：下料区
                case AutoSteps.Auto_PickOffloadMove:
                    {
                        this.msgChs = string.Format("机器人移动到[{0}-{1}-{2}]", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot move to [{0}-{1}-{2}]", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.MOVE, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PickOffloadSetEvent;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PickOffloadSetEvent:
                    {
                        this.msgChs = string.Format("机器人设置[{0}-{1}-{2}]响应信号", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot set [{0}-{1}-{2}] station event", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        int pltIdx = -1;
                        if (EventStatus.Require == GetModuleEvent(pickPos.runID, pickPos.stationEvent, ref pltIdx))
                        {
                            if(SetModuleEvent(pickPos.runID, pickPos.stationEvent, EventStatus.Response, pickPos.col))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PickOffloadIn;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PickOffloadIn:
                    {
                        this.msgChs = string.Format("机器人到[{0}-{1}-{2}]取进", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot pickin [{0}-{1}-{2}]", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        int pltPos = -1;
                        EventStatus state = GetModuleEvent(pickPos.runID, pickPos.stationEvent, ref pltPos);
                        if(((EventStatus.Ready == state) || (EventStatus.Start == state)) && (pltPos == pickPos.col))
                        {
                            if(CheckStationSafe(pickPos, RobotOrder.PICKIN))
                            {
                                SetModuleEvent(pickPos.runID, pickPos.stationEvent, EventStatus.Start, pltPos);
                                if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.PICKIN, ref robotCmd)
                                    && RobotMove(robotCmd, true))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PickOffloadGetData;
                                    SaveRunData(SaveType.AutoStep);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PickOffloadGetData:
                    {
                        this.msgChs = string.Format("机器人获取[{0}-{1}-{2}]数据", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot get [{0}-{1}-{2}] station data", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        Pallet tmpPlt = new Pallet();
                        if(GetModulePallet(pickPos.runID, pickPos.col, ref tmpPlt))
                        {
                            this.Pallet[0].Copy(tmpPlt);
                            tmpPlt.Release();
                            if(SetModulePallet(pickPos.runID, pickPos.col, tmpPlt))
                            {
                                SavePickPlacePalletData(this.pickPos, true, this.Pallet[0]);
                                this.nextAutoStep = AutoSteps.Auto_PickOffloadOut;
                                SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PickOffloadOut:
                    {
                        this.msgChs = string.Format("机器人到[{0}-{1}-{2}]取出", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot pickout [{0}-{1}-{2}]", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.PICKOUT, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PickOffloadCheckFinger;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PickOffloadCheckFinger:
                    {
                        this.msgChs = string.Format("机器人在[{0}-{1}-{2}]取料后检查", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Check [{0}-{1}-{2}] station pallet", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(PalletKeepFlat(0, true))
                        {
                            if(SetModuleEvent(pickPos.runID, pickPos.stationEvent, EventStatus.Finished, pickPos.col))
                            {
                                this.nextAutoStep = AutoSteps.Auto_CalcPlacePalletPos;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                #endregion

                #region // 计算放夹具位置
                case AutoSteps.Auto_CalcPlacePalletPos:
                    {
                        CurMsgStr("计算放夹具位", "Calc place pallet pos");
                        switch(this.placePos.stationEvent)
                        {
                            case EventList.OnloadPlaceEmptyPallet:
                            case EventList.OnloadPlaceNGPallet:
                            case EventList.OnLoadPlaceDetectFakePallet:
                            case EventList.OnloadPlaceReputFakePallet:
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PlaceOnloadMove;
                                    SaveRunData(SaveType.AutoStep);
                                    break;
                                }
                            case EventList.ManualPlaceNGEmptyPallet:
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PlaceManualSetEvent;
                                    SaveRunData(SaveType.AutoStep);
                                    break;
                                }
                            case EventList.PalletBufferPlaceEmptyPallet:
                            case EventList.PalletBufferPlaceNGEmptyPallet:
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PlaceBufferSetEvent;
                                    SaveRunData(SaveType.AutoStep);
                                    break;
                                }
                            case EventList.DryOvenPlaceEmptyPallet:
                            case EventList.DryOvenPlaceNGPallet:
                            case EventList.DryOvenPlaceNGEmptyPallet:
                            case EventList.DryOvenPlaceOnlOKFullPallet:
                            case EventList.DryOvenPlaceOnlOKFakeFullPallet:
                            case EventList.DryOvenPlaceRebakeFakePallet:
                            case EventList.DryOvenPlaceWaitResultPallet:
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PlaceDryOvenSetEvent;
                                    SaveRunData(SaveType.AutoStep);
                                    break;
                                }
                            case EventList.DryOvenPlaceTransferPallet:
                                {
                                    this.nextAutoStep = AutoSteps.Auto_TransferPlaceDryOvenSetEvent;
                                    SaveRunData(SaveType.AutoStep);
                                    break;
                                }
                            case EventList.OffLoadPlaceDryFinishPallet:
                            case EventList.OffLoadPlaceDetectFakePallet:
                            case EventList.OffLoadPlaceNGPallet:
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PlaceOffloadMove;
                                    SaveRunData(SaveType.AutoStep);
                                    break;
                                }
                            default:
                                break;
                        }
                        break;
                    }
                #endregion

                #region // 放：上料区
                case AutoSteps.Auto_PlaceOnloadMove:
                    {
                        this.msgChs = string.Format("机器人移动到[{0}-{1}-{2}]", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot move to [{0}-{1}-{2}]", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.MOVE, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PlaceOnloadSetEvent;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceOnloadSetEvent:
                    {
                        this.msgChs = string.Format("机器人设置[{0}-{1}-{2}]响应信号", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot set [{0}-{1}-{2}] station event", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        int pltIdx = -1;
                        if(EventStatus.Require == GetModuleEvent(placePos.runID, placePos.stationEvent, ref pltIdx))
                        {
                            if(SetModuleEvent(placePos.runID, placePos.stationEvent, EventStatus.Response, placePos.col))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PlaceOnloadIn;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceOnloadIn:
                    {
                        this.msgChs = string.Format("机器人到[{0}-{1}-{2}]放进", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot placein [{0}-{1}-{2}]", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        int pltPos = -1;
                        EventStatus state = GetModuleEvent(placePos.runID, placePos.stationEvent, ref pltPos);
                        if(((EventStatus.Ready == state) || (EventStatus.Start == state)) && (pltPos == placePos.col))
                        {
                            if(CheckStationSafe(placePos, RobotOrder.PLACEIN))
                            {
                                SetModuleEvent(placePos.runID, placePos.stationEvent, EventStatus.Start, pltPos);
                                if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.PLACEIN, ref robotCmd)
                                    && RobotMove(robotCmd, true))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PlaceOnloadSetData;
                                    SaveRunData(SaveType.AutoStep);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceOnloadSetData:
                    {
                        this.msgChs = string.Format("机器人设置[{0}-{1}-{2}]数据", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot set [{0}-{1}-{2}] station data", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);

                        switch(this.placePos.stationEvent)
                        {
                            case EventList.OnloadPlaceEmptyPallet:
                                {
                                    this.Pallet[0].Release();
                                    this.Pallet[0].State = PalletStatus.OK;
                                    this.Pallet[0].SetRowCol(MachineCtrl.GetInstance().PalletMaxRow, MachineCtrl.GetInstance().PalletMaxCol);
                                    break;
                                }
                        }
                        if(SetModulePallet(placePos.runID, placePos.col, this.Pallet[0]))
                        {
                            SavePickPlacePalletData(this.placePos, false, this.Pallet[0]);
                            this.Pallet[0].Release();
                            this.nextAutoStep = AutoSteps.Auto_PlaceOnloadOut;
                            SaveRunData(SaveType.AutoStep | SaveType.Pallet | SaveType.Variables);
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceOnloadOut:
                    {
                        this.msgChs = string.Format("机器人到[{0}-{1}-{2}]放出", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot pickout [{0}-{1}-{2}]", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.PLACEOUT, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PlaceOnloadCheckFinger;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceOnloadCheckFinger:
                    {
                        this.msgChs = string.Format("机器人在[{0}-{1}-{2}]放料后检查", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Check [{0}-{1}-{2}] station pallet", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(PalletKeepFlat(0, false))
                        {
                            if(SetModuleEvent(placePos.runID, placePos.stationEvent, EventStatus.Finished, placePos.col))
                            {
                                this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                #endregion

                #region // 放：缓存架
                case AutoSteps.Auto_PlaceBufferSetEvent:
                    {
                        this.msgChs = string.Format("机器人设置[{0}-{1}-{2}]响应信号", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot set [{0}-{1}-{2}] station event", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        //if(SetModuleEvent(placePos.runID, placePos.stationEvent, EventStatus.Response, placePos.col))
                        {
                            this.nextAutoStep = AutoSteps.Auto_PlaceBufferMove;
                            //SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceBufferMove:
                    {
                        this.msgChs = string.Format("机器人移动到[{0}-{1}-{2}]", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot move to [{0}-{1}-{2}]", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.MOVE, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PlaceBufferIn;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceBufferIn:
                    {
                        this.msgChs = string.Format("机器人到[{0}-{1}-{2}]放进", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot placein [{0}-{1}-{2}]", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        int pltPos = -1;
                        EventStatus state = GetModuleEvent(placePos.runID, placePos.stationEvent, ref pltPos);
                        if(((EventStatus.Ready == state) || (EventStatus.Start == state))
                            && (pltPos == (placePos.row)))
                        {
                            if(CheckStationSafe(placePos, RobotOrder.PLACEIN))
                            {
                                SetModuleEvent(placePos.runID, placePos.stationEvent, EventStatus.Start, pltPos);
                                if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.PLACEIN, ref robotCmd)
                                    && RobotMove(robotCmd, true))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PlaceBufferSetData;
                                    SaveRunData(SaveType.AutoStep);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceBufferSetData:
                    {
                        this.msgChs = string.Format("机器人设置[{0}-{1}-{2}]数据", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot set [{0}-{1}-{2}] station data", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(SetModulePallet(placePos.runID, (placePos.row), this.Pallet[0]))
                        {
                            SavePickPlacePalletData(this.placePos, false, this.Pallet[0]);
                            this.Pallet[0].Release();
                            this.nextAutoStep = AutoSteps.Auto_PlaceBufferOut;
                            SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceBufferOut:
                    {
                        this.msgChs = string.Format("机器人到[{0}-{1}-{2}]放出", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot pickout [{0}-{1}-{2}]", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.PLACEOUT, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PlaceBufferCheckFinger;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceBufferCheckFinger:
                    {
                        this.msgChs = string.Format("机器人在[{0}-{1}-{2}]放料后检查", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Check [{0}-{1}-{2}] station pallet", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(PalletKeepFlat(0, false))
                        {
                            if(SetModuleEvent(placePos.runID, placePos.stationEvent, EventStatus.Finished, (placePos.row)))
                            {
                                this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                #endregion

                #region // 放：人工操作台
                case AutoSteps.Auto_PlaceManualSetEvent:
                    {
                        this.msgChs = string.Format("机器人设置[{0}-{1}-{2}]响应信号", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot set [{0}-{1}-{2}] station event", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        //if(SetModuleEvent(placePos.runID, placePos.stationEvent, EventStatus.Response, placePos.col))
                        {
                            this.nextAutoStep = AutoSteps.Auto_PlaceManualMove;
                            //SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceManualMove:
                    {
                        this.msgChs = string.Format("机器人移动到[{0}-{1}-{2}]", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot move to [{0}-{1}-{2}]", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.MOVE, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PlaceManualIn;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceManualIn:
                    {
                        this.msgChs = string.Format("机器人到[{0}-{1}-{2}]放进", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot placein [{0}-{1}-{2}]", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        int pltPos = -1;
                        EventStatus state = GetModuleEvent(placePos.runID, placePos.stationEvent, ref pltPos);
                        if(((EventStatus.Ready == state) || (EventStatus.Start == state)) && (pltPos == placePos.col))
                        {
                            if(CheckStationSafe(placePos, RobotOrder.PLACEIN))
                            {
                                SetModuleEvent(placePos.runID, placePos.stationEvent, EventStatus.Start, pltPos);
                                if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.PLACEIN, ref robotCmd)
                                    && RobotMove(robotCmd, true))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PlaceManualSetData;
                                    SaveRunData(SaveType.AutoStep);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceManualSetData:
                    {
                        this.msgChs = string.Format("机器人设置[{0}-{1}-{2}]数据", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot set [{0}-{1}-{2}] station data", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(SetModulePallet(placePos.runID, placePos.col, this.Pallet[0]))
                        {
                            SavePickPlacePalletData(this.placePos, false, this.Pallet[0]);
                            this.Pallet[0].Release();
                            this.nextAutoStep = AutoSteps.Auto_PlaceManualOut;
                            SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceManualOut:
                    {
                        this.msgChs = string.Format("机器人到[{0}-{1}-{2}]放出", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot pickout [{0}-{1}-{2}]", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.PLACEOUT, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PlaceManualCheckFinger;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceManualCheckFinger:
                    {
                        this.msgChs = string.Format("机器人在[{0}-{1}-{2}]放料后检查", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Check [{0}-{1}-{2}] station pallet", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(PalletKeepFlat(0, false))
                        {
                            if(SetModuleEvent(placePos.runID, placePos.stationEvent, EventStatus.Finished, placePos.col))
                            {
                                this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                #endregion

                #region // 放：干燥炉
                case AutoSteps.Auto_PlaceDryOvenSetEvent:
                    {
                        this.msgChs = string.Format("机器人设置[{0}-{1}-{2}]响应信号", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot set [{0}-{1}-{2}] station event", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        //if(SetModuleEvent(placePos.runID, placePos.stationEvent, EventStatus.Response, placePos.col))
                        {
                            this.nextAutoStep = AutoSteps.Auto_PlaceDryOvenMove;
                            //SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceDryOvenMove:
                    {
                        this.msgChs = string.Format("机器人移动到[{0}-{1}-{2}]", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot move to [{0}-{1}-{2}]", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.MOVE, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PlaceDryOvenIn;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceDryOvenIn:
                    {
                        this.msgChs = string.Format("机器人到[{0}-{1}-{2}]放进", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot placein [{0}-{1}-{2}]", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        int pltPos = -1;
                        EventStatus state = GetModuleEvent(placePos.runID, placePos.stationEvent, ref pltPos);
                        if(((EventStatus.Ready == state) || (EventStatus.Start == state))
                            && (pltPos == (placePos.row * (int)OvenRowCol.MaxCol + placePos.col)))
                        {
                            if(CheckStationSafe(placePos, RobotOrder.PLACEIN))
                            {
                                SetModuleEvent(placePos.runID, placePos.stationEvent, EventStatus.Start, pltPos);
                                if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.PLACEIN, ref robotCmd)
                                    && RobotMove(robotCmd, true))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PlaceDryOvenSetData;
                                    SaveRunData(SaveType.AutoStep);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceDryOvenSetData:
                    {
                        this.msgChs = string.Format("机器人设置[{0}-{1}-{2}]数据", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot set [{0}-{1}-{2}] station data", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(SetModulePallet(placePos.runID, (this.placePos.row * (int)OvenRowCol.MaxCol + this.placePos.col), this.Pallet[0]))
                        {
                            SavePickPlacePalletData(this.placePos, false, this.Pallet[0]);
                            this.Pallet[0].Release();
                            this.nextAutoStep = AutoSteps.Auto_PlaceDryOvenOut;
                            SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceDryOvenOut:
                    {
                        this.msgChs = string.Format("机器人到[{0}-{1}-{2}]放出", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot pickout [{0}-{1}-{2}]", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.PLACEOUT, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PlaceDryOvenCheckFinger;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceDryOvenCheckFinger:
                    {
                        this.msgChs = string.Format("机器人在[{0}-{1}-{2}]放料后检查", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Check [{0}-{1}-{2}] station pallet", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(PalletKeepFlat(0, false))
                        {
                            int pltIdx = (this.placePos.row * (int)OvenRowCol.MaxCol + this.placePos.col);
                            if(SetModuleEvent(placePos.runID, placePos.stationEvent, EventStatus.Finished, pltIdx))
                            {
                                this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                #endregion

                #region // 放：下料区
                case AutoSteps.Auto_PlaceOffloadMove:
                    {
                        this.msgChs = string.Format("机器人移动到[{0}-{1}-{2}]", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot move to [{0}-{1}-{2}]", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.MOVE, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PlaceOffloadSetEvent;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceOffloadSetEvent:
                    {
                        this.msgChs = string.Format("机器人设置[{0}-{1}-{2}]响应信号", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot set [{0}-{1}-{2}] station event", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        int pltIdx = -1;
                        if(EventStatus.Require == GetModuleEvent(placePos.runID, placePos.stationEvent, ref pltIdx))
                        {
                            if(SetModuleEvent(placePos.runID, placePos.stationEvent, EventStatus.Response, placePos.col))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PlaceOffloadIn;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceOffloadIn:
                    {
                        this.msgChs = string.Format("机器人到[{0}-{1}-{2}]放进", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot placein [{0}-{1}-{2}]", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        int pltPos = -1;
                        EventStatus state = GetModuleEvent(placePos.runID, placePos.stationEvent, ref pltPos);
                        if(((EventStatus.Ready == state) || (EventStatus.Start == state)) && (pltPos == placePos.col))
                        {
                            if(CheckStationSafe(placePos, RobotOrder.PLACEIN))
                            {
                                SetModuleEvent(placePos.runID, placePos.stationEvent, EventStatus.Start, pltPos);
                                if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.PLACEIN, ref robotCmd)
                                    && RobotMove(robotCmd, true))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PlaceOffloadSetData;
                                    SaveRunData(SaveType.AutoStep);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceOffloadSetData:
                    {
                        this.msgChs = string.Format("机器人设置[{0}-{1}-{2}]数据", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot set [{0}-{1}-{2}] station data", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(SetModulePallet(placePos.runID, placePos.col, this.Pallet[0]))
                        {
                            SavePickPlacePalletData(this.placePos, false, this.Pallet[0]);
                            this.Pallet[0].Release();
                            this.nextAutoStep = AutoSteps.Auto_PlaceOffloadOut;
                            SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceOffloadOut:
                    {
                        this.msgChs = string.Format("机器人到[{0}-{1}-{2}]放出", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot pickout [{0}-{1}-{2}]", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.PLACEOUT, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PlaceOffloadCheckFinger;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceOffloadCheckFinger:
                    {
                        this.msgChs = string.Format("机器人在[{0}-{1}-{2}]放料后检查", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Check [{0}-{1}-{2}] station pallet", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(PalletKeepFlat(0, false))
                        {
                            if(SetModuleEvent(placePos.runID, placePos.stationEvent, EventStatus.Finished, placePos.col))
                            {
                                this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                #endregion

                #region // 转炉换腔

                // 取：干燥炉
                case AutoSteps.Auto_TransferPickDryOvenSetEvent:
                    {
                        this.msgChs = string.Format("机器人转炉换腔设置[{0}-{1}-{2}]响应信号", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot set transfer [{0}-{1}-{2}] station event", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        //if(SetModuleEvent(pickPos.runID, pickPos.stationEvent, EventStatus.Response, pickPos.col))
                        {
                            this.nextAutoStep = AutoSteps.Auto_TransferPickDryOvenMove;
                            //SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_TransferPickDryOvenMove:
                    {
                        this.msgChs = string.Format("机器人移动到转炉换腔[{0}-{1}-{2}]", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot move to transfer [{0}-{1}-{2}]", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.MOVE, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_TransferPickDryOvenIn;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_TransferPickDryOvenIn:
                    {
                        this.msgChs = string.Format("机器人到转炉换腔[{0}-{1}-{2}]取进", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot pickin transfer [{0}-{1}-{2}]", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        int pltPos = -1;
                        EventStatus state = GetModuleEvent(pickPos.runID, pickPos.stationEvent, ref pltPos);
                        if(((EventStatus.Ready == state) || (EventStatus.Start == state))
                            && ((pltPos / (int)OvenRowCol.MaxCol) == pickPos.row))
                        {
                            if(CheckStationSafe(pickPos, RobotOrder.PICKIN))
                            {
                                SetModuleEvent(pickPos.runID, pickPos.stationEvent, EventStatus.Start, pltPos);
                                if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.PICKIN, ref robotCmd)
                                    && RobotMove(robotCmd, true))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_TransferPickDryOvenGetData;
                                    SaveRunData(SaveType.AutoStep);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_TransferPickDryOvenGetData:
                    {
                        this.msgChs = string.Format("机器人获取转炉换腔[{0}-{1}-{2}]数据", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot transfer get [{0}-{1}-{2}] station data", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        Pallet tmpPlt = new Pallet();
                        int pltIdx = (this.pickPos.row * (int)OvenRowCol.MaxCol + this.pickPos.col);
                        if(GetModulePallet(pickPos.runID, pltIdx, ref tmpPlt))
                        {
                            this.Pallet[0].Copy(tmpPlt);
                            tmpPlt.Release();
                            if(SetModulePallet(pickPos.runID, pltIdx, tmpPlt))
                            {
                                SavePickPlacePalletData(this.pickPos, true, this.Pallet[0]);
                                this.Pallet[0].SrcStation = (int)pickPos.station;
                                this.Pallet[0].SrcRow = pickPos.row;
                                this.Pallet[0].SrcCol = pickPos.col;
                                this.nextAutoStep = AutoSteps.Auto_TransferPickDryOvenOut;
                                SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_TransferPickDryOvenOut:
                    {
                        this.msgChs = string.Format("机器人到转炉换腔[{0}-{1}-{2}]取出", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Robot transfer pickout [{0}-{1}-{2}]", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.PICKOUT, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_TransferPickDryOvenCheckFinger;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_TransferPickDryOvenCheckFinger:
                    {
                        this.msgChs = string.Format("机器人在转炉换腔[{0}-{1}-{2}]取料后检查", GetRobotStationName(this.pickPos.station), this.pickPos.row, this.pickPos.col);
                        this.msgEng = string.Format("Check transfer [{0}-{1}-{2}] station pallet", this.pickPos.station, this.pickPos.row, this.pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(PalletKeepFlat(0, true))
                        {
                            Pallet tmpPlt = new Pallet();
                            int pltIdx = (this.pickPos.row * (int)OvenRowCol.MaxCol + (1 - this.pickPos.col));
                            if(GetModulePallet(pickPos.runID, pltIdx, ref tmpPlt))
                            {
                                if (PalletStatus.Invalid == tmpPlt.State)
                                {
                                    pltIdx = (this.pickPos.row * (int)OvenRowCol.MaxCol + this.pickPos.col);
                                    if(SetModuleEvent(pickPos.runID, pickPos.stationEvent, EventStatus.Finished, pltIdx))
                                    {
                                        this.nextAutoStep = AutoSteps.Auto_TransferPlaceDryOvenSetEvent;
                                        SaveRunData(SaveType.AutoStep);
                                    }
                                }
                                else
                                {
                                    this.nextAutoStep = AutoSteps.Auto_CalcPlacePalletPos;
                                    SaveRunData(SaveType.AutoStep);
                                }
                            }
                        }
                        break;
                    }

                // 放：干燥炉
                case AutoSteps.Auto_TransferPlaceDryOvenSetEvent:
                    {
                        this.msgChs = string.Format("机器人设置转炉换腔[{0}-{1}-{2}]响应信号", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot set transfer [{0}-{1}-{2}] station event", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        //if(SetModuleEvent(placePos.runID, placePos.stationEvent, EventStatus.Response, placePos.col))
                        {
                            this.nextAutoStep = AutoSteps.Auto_TransferPlaceDryOvenMove;
                            //SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_TransferPlaceDryOvenMove:
                    {
                        this.msgChs = string.Format("机器人移动到转炉换腔[{0}-{1}-{2}]", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot move to transfer [{0}-{1}-{2}]", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.MOVE, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_TransferPlaceDryOvenIn;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_TransferPlaceDryOvenIn:
                    {
                        this.msgChs = string.Format("机器人到转炉换腔[{0}-{1}-{2}]放进", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot placein transfer [{0}-{1}-{2}]", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        int pltPos = -1;
                        EventStatus state = GetModuleEvent(placePos.runID, placePos.stationEvent, ref pltPos);
                        if(((EventStatus.Ready == state) || (EventStatus.Start == state))
                            && ((pltPos / (int)OvenRowCol.MaxCol) == placePos.row))
                        {
                            if(CheckStationSafe(placePos, RobotOrder.PLACEIN))
                            {
                                SetModuleEvent(placePos.runID, placePos.stationEvent, EventStatus.Start, pltPos);
                                if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.PLACEIN, ref robotCmd)
                                    && RobotMove(robotCmd, true))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_TransferPlaceDryOvenSetData;
                                    SaveRunData(SaveType.AutoStep);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_TransferPlaceDryOvenSetData:
                    {
                        this.msgChs = string.Format("机器人设置转炉换腔[{0}-{1}-{2}]数据", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot set transfer [{0}-{1}-{2}] station data", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(SetModulePallet(placePos.runID, (this.placePos.row * (int)OvenRowCol.MaxCol + this.placePos.col), this.Pallet[0]))
                        {
                            SavePickPlacePalletData(this.placePos, false, this.Pallet[0]);
                            this.Pallet[0].Release();
                            this.nextAutoStep = AutoSteps.Auto_TransferPlaceDryOvenOut;
                            SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                        }
                        break;
                    }
                case AutoSteps.Auto_TransferPlaceDryOvenOut:
                    {
                        this.msgChs = string.Format("机器人到转炉换腔[{0}-{1}-{2}]放出", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Robot pickout transfer [{0}-{1}-{2}]", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.PLACEOUT, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_TransferPlaceDryOvenCheckFinger;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_TransferPlaceDryOvenCheckFinger:
                    {
                        this.msgChs = string.Format("机器人在转炉换腔[{0}-{1}-{2}]放料后检查", GetRobotStationName(this.placePos.station), this.placePos.row, this.placePos.col);
                        this.msgEng = string.Format("Check transfer [{0}-{1}-{2}] station pallet", this.placePos.station, this.placePos.row, this.placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(PalletKeepFlat(0, false))
                        {
                            // 转移换腔取的位置已经取完成，则置放完成
                            int pos = -1;
                            if(EventStatus.Finished == GetModuleEvent(this.pickPos.runID, this.pickPos.stationEvent, ref pos))
                            {
                                int pltIdx = (this.placePos.row * (int)OvenRowCol.MaxCol + this.placePos.col);
                                if(SetModuleEvent(placePos.runID, placePos.stationEvent, EventStatus.Finished, pltIdx))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                    SaveRunData(SaveType.AutoStep);
                                }
                            }
                            else
                            {
                                this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                SaveRunData(SaveType.AutoStep);
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

                        //执行完操作备份运行数据
                        foreach (var item in MachineCtrl.GetInstance().ListRuns)
                        {
                            item.BackupRunData();
                        }
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

            string type = IniFile.ReadString(this.RunModule, "RobotType", "", Def.GetAbsPathName(Def.ModuleExCfg));
            if(string.IsNullOrEmpty(type) || !this.robotClient.SetRobotType(type))
            {
                string msg = string.Format("RobotType = {0} 配置错误！", type);
                ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
            }
            this.RobotID = (RobotIndexID)IniFile.ReadInt(this.RunModule, "RobotID", (int)RobotIndexID.Invalid, Def.GetAbsPathName(Def.ModuleExCfg));
            if(this.RobotID <= RobotIndexID.Invalid || this.RobotID >= RobotIndexID.End)
            {
                string msg = string.Format("RobotID = {0} 配置错误，应该为{1} < RobotID < {2}", (int)this.RobotID, (int)RobotIndexID.Invalid, (int)RobotIndexID.End);
                ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
            }
            this.robotClient.SetRobotInfo((int)this.RobotID, this.RunName);
            InitRobotStation();

            LoadRobotRunData();

            return true;
        }

        /// <summary>
        /// 初始化通用组参数 + 模组参数
        /// </summary>
        protected override void InitParameter()
        {
            this.robotEnable = false;
            this.robotSpeed = 10;
            this.RobotLowSpeed = 10;
            this.robotDelay = 60;
            this.robotIP = "";
            this.robotPort = 0;
            this.RobotRunning = false;
            this.offloadDetect = 3;
            this.offloadPltCount = 0;
            this.robotOilRate = 50;
            this.robotOilCount = 0;

            base.InitParameter();
        }

        /// <summary>
        /// 读取通用组参数 + 模组参数
        /// </summary>
        /// <returns></returns>
        public override bool ReadParameter()
        {
            this.OnLoad = ReadBoolParameter(this.RunModule, "OnLoad", true);
            this.OffLoad = ReadBoolParameter(this.RunModule, "OffLoad", true);

            this.robotEnable = ReadBoolParameter(this.RunModule, "robotEnable", this.robotEnable);
            this.robotSpeed = ReadIntParameter(this.RunModule, "robotSpeed", this.robotSpeed);
            this.RobotLowSpeed = ReadIntParameter(this.RunModule, "RobotLowSpeed", this.RobotLowSpeed);
            this.robotDelay = ReadIntParameter(this.RunModule, "robotDelay", this.robotDelay);
            this.robotIP = ReadStringParameter(this.RunModule, "robotIP", "");
            this.robotPort = ReadIntParameter(this.RunModule, "robotPort", this.robotPort);
            this.offloadDetect = ReadIntParameter(this.RunModule, "offloadDetect", this.offloadDetect);
            this.robotOilRate = ReadIntParameter(this.RunModule, "robotOilRate", this.robotOilRate);

            return base.ReadParameter();
        }

        /// <summary>
        /// 读取本模组的相关模组
        /// </summary>
        public override void ReadRelatedModule()
        {
            string module, value;
            module = this.RunModule;

            // 读取已配置的，共用真空泵干燥炉分组
            this.placeDryOvenOrder = new Dictionary<RunID, bool>();
            value = IniFile.ReadString(this.RunModule, "PlaceDryOvenOrder", "", Def.GetAbsPathName(Def.ModuleExCfg));
            if(!string.IsNullOrEmpty(value))
            {
                string[] index = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach(var item in index)
                {
                    int id = -1;
                    if (int.TryParse(item, out id) && (id >= 0) && (id < (int)OvenInfoCount.OvenCount))
                    {
                        this.placeDryOvenOrder.Add((RunID)(id + RunID.DryOven0), false);
                    }
                }
            }
            // 分组未配置的干燥炉
            for(RunID id = RunID.DryOven0; id < RunID.DryOvenALL; id++)
            {
                if(!this.placeDryOvenOrder.ContainsKey(id))
                {
                    this.placeDryOvenOrder.Add(id, false);
                }
            }
        }

        #endregion

        #region // IO及电机

        /// <summary>
        /// 初始化IO及电机
        /// </summary>
        protected override void InitModuleIOMotor()
        {
            int maxSenser = 2;
            this.IPalletKeepFlat = new int[maxSenser];
            this.IFingerFrontCheck = new int[maxSenser];
            this.IFingerSideCheck = new int[maxSenser];
            for(int i = 0; i < maxSenser; i++)
            {
                this.IPalletKeepFlat[i] = AddInput("IPalletKeepFlat" + i);
                this.IFingerFrontCheck[i] = AddInput("IFingerFrontCheck" + i);
                this.IFingerSideCheck[i] = AddInput("IFingerSideCheck" + i);
            }

            this.IRobotPeLimit = AddInput("IRobotPeLimit");
            this.IRobotNeLimit = AddInput("IRobotNeLimit");
            this.IRobotRunning = AddInput("IRobotRunning");

            this.ORobotEStop = AddOutput("ORobotEStop");
            this.ORobotOil = AddOutput("ORobotOil");
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
            if(0 != pltIdx)
            {
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
        /// 插料架前端检测：true安全，false非安全有障碍物
        /// </summary>
        /// <returns></returns>
        protected bool FingerFrontCheck()
        {
            for(int i = 0; i < IFingerFrontCheck.Length; i++)
            {
                if (!CheckInputState(IFingerFrontCheck[i], false))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region // 机器人操作
        
        public string RobotIPInfo()
        {
            return string.Format("{0}:{1}", this.robotIP, this.robotPort);
        }

        public bool RobotIsConnect()
        {
            if(!robotEnable)
            {
                return true;
            }
            return this.robotClient.IsConnect();
        }

        public bool RobotConnect(bool connect = true)
        {
            if(!robotEnable)
            {
                return true;
            }
            if(connect)
            {
                if(!RobotIsConnect())
                {
                    return this.robotClient.Connect(robotIP, robotPort);
                }
            }
            else
            {
                return this.robotClient.Disconnect();
            }
            return RobotIsConnect();
        }

        public bool GetRobotCmd(TransferRobotStation station, int row, int col, int speed, RobotOrder order, ref int[] rbtCmd)
        {
            rbtCmd[(int)RobotCmdFormat.Station] = (int)station;
            rbtCmd[(int)RobotCmdFormat.StationRow] = row + 1;
            rbtCmd[(int)RobotCmdFormat.StationCol] = col + 1;
            rbtCmd[(int)RobotCmdFormat.Speed] = speed;
            rbtCmd[(int)RobotCmdFormat.Order] = (int)order;
            rbtCmd[(int)RobotCmdFormat.Result] = (int)RobotOrder.END;

            MCState state = MachineCtrl.GetInstance().RunsCtrl.GetMCState();
            if(MCState.MCRunning == state)
            {
                this.robotAutoAction.SetData((int)station, row, col, order, GetRobotStationName(station));
                this.robotDebugAction.SetData((int)station, row, col, order, GetRobotStationName(station));
            }
            else
            {
                this.robotDebugAction.SetData((int)station, row, col, order, GetRobotStationName(station));
            }
            SaveRunData(SaveType.Robot);
            return true;
        }

        public bool RobotMove(int[] rbtCmd, bool wait = true, OptMode mode = OptMode.Auto)
        {
            if(!robotEnable)
            {
                return true;
            }
            log.DebugFormat("RobotMove: send cmd {0}", rbtCmd);
            if (!this.robotClient.Send(rbtCmd, mode))
            {
                int[] recvCmd = new int[(int)RobotCmdFormat.End];
                string msg = string.Format("机器人反馈移动超时[10秒]");
                string dispose = "";
                if(this.robotClient.GetReceiveResult(ref recvCmd))
                {
                    msg += string.Format("，机器人反馈：{0},{1},{2},{3},{4},{5}"
                    , recvCmd[(int)RobotCmdFormat.Station], recvCmd[(int)RobotCmdFormat.StationRow]
                    , recvCmd[(int)RobotCmdFormat.StationCol], recvCmd[(int)RobotCmdFormat.Speed]
                    , (RobotOrder)recvCmd[(int)RobotCmdFormat.Order], (RobotOrder)recvCmd[(int)RobotCmdFormat.Result]);
                    dispose = "请检查机器人指令是否错误，或检查示教器是否有异常提示或报警";
                }
                else
                {
                    msg += "，机器人无响应反馈";
                    dispose = "请检查机器人示教器，是否在自动运行状态";
                }
                log.DebugFormat("RobotMove: send cmd error, {0}", msg);
                ShowMessageBox((int)MsgID.SendRbtMoveCmd, msg, dispose, MessageType.MsgAlarm);
                return false;
            }
            log.Debug("RobotMove: send cmd success");
            if (wait)
            {
                log.Debug("RobotMove: waiting for executing...");
                return RobotMoveFinish(rbtCmd, DateTime.Now);
            }
            log.Debug("RobotMove: return");
            return false;
        }

        public bool RobotMoveFinish(int[] rbtCmd, DateTime startTime)
        {
            if(!robotEnable)
            {
                return true;
            }
            string msg, dispose;
            int[] recvCmd = new int[(int)RobotCmdFormat.End];
            if ((int)RobotOrder.MOVE == rbtCmd[(int)RobotCmdFormat.Order])
            {
                this.robotOilCount++;       // 运动次数++
            }
            this.RobotRunning = true;
            while(true)
            {
                if(this.robotClient.GetReceiveResult(ref recvCmd))
                {
                    if((rbtCmd[(int)RobotCmdFormat.Station] == recvCmd[(int)RobotCmdFormat.Station])
                        && (rbtCmd[(int)RobotCmdFormat.StationRow] == recvCmd[(int)RobotCmdFormat.StationRow])
                        && (rbtCmd[(int)RobotCmdFormat.StationCol] == recvCmd[(int)RobotCmdFormat.StationCol])
                        && (rbtCmd[(int)RobotCmdFormat.Order] == recvCmd[(int)RobotCmdFormat.Order])
                        && ((int)RobotOrder.FINISH == recvCmd[(int)RobotCmdFormat.Result]))
                    {
                        OutputAction(this.ORobotOil, false);
                        this.RobotRunning = false;
                        log.Debug("RobotMove finish");
                        return true;
                    }
                    if(((int)RobotOrder.INVALID == recvCmd[(int)RobotCmdFormat.Result])
                        || ((int)RobotOrder.ERR == recvCmd[(int)RobotCmdFormat.Result]))
                    {
                        this.RobotRunning = false;
                        msg = string.Format("机器人指令错误[{0},{1},{2},{3},{4},{5}]"
                            , rbtCmd[(int)RobotCmdFormat.Station], rbtCmd[(int)RobotCmdFormat.StationRow]
                            , rbtCmd[(int)RobotCmdFormat.StationCol], rbtCmd[(int)RobotCmdFormat.Speed]
                            , (RobotOrder)rbtCmd[(int)RobotCmdFormat.Order], (RobotOrder)rbtCmd[(int)RobotCmdFormat.Result]);
                        dispose = string.Format("请检查机器人指令");
                        ShowMessageBox((int)MsgID.RbtMoveCmdError, msg, dispose, MessageType.MsgAlarm);
                        break;
                    }
                }
                if((DateTime.Now - startTime).TotalSeconds > this.robotDelay)
                {
                    this.RobotRunning = false;
                    msg = string.Format("机器人运动超时[{0}秒]", this.robotDelay);
                    dispose = string.Format("请检查机器人是否运行完成");
                    ShowMessageBox((int)MsgID.RbtMoveTimeout, msg, dispose, MessageType.MsgAlarm);
                    break;
                }
                if(!OutputState(this.ORobotEStop, false))
                {
                    this.RobotRunning = false;
                    msg = "机器人急停，退出等待机器人动作完成";
                    Def.WriteLog(this.RunName, msg);
                    break;
                }
                
                #region // 导轨加油
                if(this.robotOilCount >= this.robotOilRate)
                {
                    this.robotOilCount = 0;
                    OutputAction(this.ORobotOil, true);
                }
                else if(OutputState(ORobotOil, true) && (DateTime.Now - startTime).TotalSeconds > 30)
                {
                    OutputAction(this.ORobotOil, false);
                }
                #endregion

                Sleep(1);
            }
            OutputAction(this.ORobotOil, false);
            log.DebugFormat("RobotMove error, {0}", msg);
            return false;
        }

        /// <summary>
        /// 初始化机器人工位
        /// </summary>
        public void InitRobotStation()
        {
            if(null == this.robotStationInfo)
            {
                this.robotStationInfo = new Dictionary<TransferRobotStation, RobotFormula>();
                int rbtID = (int)RobotID;
                if(this.RobotID <= RobotIndexID.Invalid || this.RobotID >= RobotIndexID.End)
                {
                    return;
                }
                int formulaID = Def.GetProductFormula();
                string rbtName = RobotDef.RobotIDName[rbtID];
                List<RobotFormula> listStation = new List<RobotFormula>();
                this.dbRecord.GetRobotStationList(Def.GetProductFormula(), (int)RobotID, ref listStation);
                foreach(var item in listStation)
                {
                    this.robotStationInfo.Add((TransferRobotStation)item.stationID, item);
                }
                for(TransferRobotStation station = TransferRobotStation.InvalidStatioin; station < TransferRobotStation.StationEnd; station++)
                {
                    bool add = false;
                    RobotFormula rbtFormula = new RobotFormula();
                    string stationName = "";
                    int stationID = (int)station;
                    #region // 查找工位是否存在
                    switch(station)
                    {
                        case TransferRobotStation.InvalidStatioin:
                            {
                                if(!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                    stationName = string.Format("{0}】闲置", stationID);
                                    rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, 1, 1);
                                }
                                break;
                            }
                        case TransferRobotStation.OnloadStation:
                            {
                                if(!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                    stationName = string.Format("{0}】上料工位", stationID);
                                    rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, 1, (int)ModuleMaxPallet.OnloadRobot);
                                }
                                break;
                            }
                        case TransferRobotStation.PalletBuffer:
                            {
                                if(!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                    stationName = string.Format("{0}】夹具缓存架", stationID);
                                    rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, (int)ModuleMaxPallet.PalletBuffer, 1);
                                }
                                break;
                            }
                        case TransferRobotStation.ManualOperate:
                            {
                                if(!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                    stationName = string.Format("{0}】人工操作台", stationID);
                                    rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, 1, (int)ModuleMaxPallet.ManualOperate);
                                }
                                break;
                            }
                        case TransferRobotStation.OffloadStation:
                            {
                                if(!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                    stationName = string.Format("{0}】下料工位", stationID);
                                    rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, 1, (int)ModuleMaxPallet.OffloadBattery);
                                }
                                break;
                            }
                        default:
                            {
                                if (TransferRobotStation.DryOven_0 <= station && station <= TransferRobotStation.DryOven_All)
                                {
                                    if(!this.robotStationInfo.ContainsKey(station))
                                    {
                                        add = true;
                                        stationName = string.Format("{0}】干燥炉{1}", stationID, (stationID - (int)TransferRobotStation.DryOven_0 + 1));
                                        rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, (int)OvenRowCol.MaxRow, (int)OvenRowCol.MaxCol);
                                    }
                                }
                                break;
                            }
                    }
                    #endregion
                    if(add)
                    {
                        this.robotStationInfo.Add(station, rbtFormula);
                        this.dbRecord.AddRobotStation(rbtFormula);
                    }
                }
            }
        }

        public string GetRobotStationName(TransferRobotStation station)
        {
            string info = "";
            if(this.robotStationInfo.ContainsKey(station))
            {
                info = this.robotStationInfo[station].stationName;
            }
            return info;
        }

        public RobotActionInfo GetRobotActionInfo(bool autoAction)
        {
            return autoAction ? this.robotAutoAction : this.robotDebugAction;
        }

        public bool RobotInSafePos()
        {
            if((RobotOrder.PICKIN != this.robotAutoAction.order)
                && (RobotOrder.PLACEIN != this.robotAutoAction.order))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 自动运行开始时机器人位置防呆检查
        /// </summary>
        /// <returns></returns>
        private bool CheckRobotPos(RobotActionInfo autoCmd, RobotActionInfo debugCmd)
        {
            bool result = true;
            string msg, disp;
            if((autoCmd.station == debugCmd.station) && (autoCmd.row == debugCmd.row)
                && (autoCmd.col == debugCmd.col) && (autoCmd.order == debugCmd.order))
            {
                result = true;
            }
            // 自动在闲置，手动在MOVE位置则不判断
            else if((RobotOrder.INVALID == autoCmd.order) && (RobotOrder.INVALID != debugCmd.order))
            {
                if(RobotOrder.MOVE != debugCmd.order)
                {
                    msg = string.Format("机器人动作位置被改变");
                    disp = string.Format("请在【机器人调试】界面将 {0} 移动到\r\n<最接近工位的{1}>\r\n位置，重新停止-复位-启动！"
                        , RobotDef.RobotIDName[(int)this.RobotID], RobotDef.RobotOrderName[(int)RobotOrder.MOVE]);
                    ShowMessageBox((int)MsgID.RbtActionChange, msg, disp, MessageType.MsgWarning);
                    result = false;
                }
            }
            else if((autoCmd.station != debugCmd.station) || (autoCmd.row != debugCmd.row)
                || (autoCmd.col != debugCmd.col) || (autoCmd.order != debugCmd.order))
            {
                if(((RobotOrder.PICKIN == autoCmd.order) || (RobotOrder.PLACEIN == autoCmd.order)))
                {
                    msg = string.Format("机器人动作位置被改变");
                    disp = string.Format("请在【机器人调试】界面将 {0} 移动到\r\n<{1}-{2}行-{3}列-{4}>\r\n位置，重新停止-复位-启动！"
                        , RobotDef.RobotIDName[(int)this.RobotID], autoCmd.stationName
                        , autoCmd.row + 1, autoCmd.col + 1, RobotDef.RobotOrderName[(int)autoCmd.order]);
                    ShowMessageBox((int)MsgID.RbtActionChange, msg, disp, MessageType.MsgWarning);
                    result = false;
                }
                else if(RobotOrder.MOVE != debugCmd.order)
                {
                    msg = string.Format("机器人动作位置被改变");
                    disp = string.Format("请在【机器人调试】界面将 {0} 移动到\r\n<{1}-{2}行-{3}列-{4}>\r\n位置，重新停止-复位-启动！"
                        , RobotDef.RobotIDName[(int)this.RobotID], autoCmd.stationName
                        , autoCmd.row + 1, autoCmd.col + 1, RobotDef.RobotOrderName[(int)RobotOrder.MOVE]);
                    ShowMessageBox((int)MsgID.RbtActionChange, msg, disp, MessageType.MsgWarning);
                    result = false;
                }
            }
            return result;
        }

        /// <summary>
        /// 手动操作防呆
        /// </summary>
        /// <param name="station"></param>
        /// <param name="nRow"></param>
        /// <param name="nCol"></param>
        /// <param name="nOrder"></param>
        /// <returns></returns>
        public bool RobotManulAvoid(TransferRobotStation station, int row, int col, RobotOrder order)
        {
            string msg = "";
            RunID runId = RunID.Invalid;

            #region // 判断动作指令
            switch(order)
            {
                case RobotOrder.MOVE:
                    {
                        if((RobotOrder.PICKIN == this.robotDebugAction.order) || (RobotOrder.PLACEIN == this.robotDebugAction.order))
                        {
                            msg = string.Format("{0}\r\n当前在<{1}-{2}行-{3}列-{4}>位置\r\n不能执行<{5}-{6}行-{7}列-{8}>操作", this.RunName, this.robotDebugAction.stationName
                                , this.robotDebugAction.row + 1, this.robotDebugAction.col + 1, RobotDef.RobotOrderName[(int)this.robotDebugAction.order]
                                , GetRobotStationName(station), row + 1, col + 1, RobotDef.RobotOrderName[(int)order]);
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            return false;
                        }
                        return true;
                        break;
                    }
                case RobotOrder.PICKIN:
                    {
                        if(((int)station != this.robotDebugAction.station) || (row != this.robotDebugAction.row)
                             || (col != this.robotDebugAction.col) || (RobotOrder.PICKIN == this.robotDebugAction.order) 
                             || (RobotOrder.PLACEIN == this.robotDebugAction.order))
                        {
                            msg = string.Format("{0}\r\n当前在<{1}-{2}行-{3}列-{4}>位置\r\n不能执行<{5}-{6}行-{7}列-{8}>操作", this.RunName, this.robotDebugAction.stationName
                                , this.robotDebugAction.row + 1, this.robotDebugAction.col + 1, RobotDef.RobotOrderName[(int)this.robotDebugAction.order]
                                , GetRobotStationName(station), row + 1, col + 1, RobotDef.RobotOrderName[(int)order]);
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            return false;
                        }
                        if (!FingerFrontCheck())
                        {
                            return false;
                        }
                        bool figPlt = !PalletKeepFlat(0, false, false);
                        if(figPlt)
                        {
                            msg = string.Format("机器人抓手有夹具，不能操作{0}{1}", RobotDef.RobotIDName[(int)this.RobotID], RobotDef.RobotOrderName[(int)order]);
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            return false;
                        }
                        break;
                    }
                case RobotOrder.PICKOUT:
                    {
                        if(((int)station != this.robotDebugAction.station) || (row != this.robotDebugAction.row)
                             || (col != this.robotDebugAction.col) || (RobotOrder.PICKIN != this.robotDebugAction.order))
                        {
                            msg = string.Format("{0}\r\n当前在<{1}-{2}行-{3}列-{4}>位置\r\n不能执行<{5}-{6}行-{7}列-{8}>操作", this.RunName, this.robotDebugAction.stationName
                                , this.robotDebugAction.row + 1, this.robotDebugAction.col + 1, RobotDef.RobotOrderName[(int)this.robotDebugAction.order]
                                , GetRobotStationName(station), row + 1, col + 1, RobotDef.RobotOrderName[(int)order]);
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            return false;
                        }
                        return true;
                        break;
                    }
                case RobotOrder.PLACEIN:
                    {
                        if(((int)station != this.robotDebugAction.station) || (row != this.robotDebugAction.row)
                             || (col != this.robotDebugAction.col) || (RobotOrder.PICKIN == this.robotDebugAction.order)
                             || (RobotOrder.PLACEIN == this.robotDebugAction.order))
                        {
                            msg = string.Format("{0}\r\n当前在<{1}-{2}行-{3}列-{4}>位置\r\n不能执行<{5}-{6}行-{7}列-{8}>操作", this.RunName, this.robotDebugAction.stationName
                                , this.robotDebugAction.row + 1, this.robotDebugAction.col + 1, RobotDef.RobotOrderName[(int)this.robotDebugAction.order]
                                , GetRobotStationName(station), row + 1, col + 1, RobotDef.RobotOrderName[(int)order]);
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            return false;
                        }
                        break;
                    }
                case RobotOrder.PLACEOUT:
                    {
                        if(((int)station != this.robotDebugAction.station) || (row != this.robotDebugAction.row)
                             || (col != this.robotDebugAction.col) || (RobotOrder.PLACEIN != this.robotDebugAction.order))
                        {
                            msg = string.Format("{0}\r\n当前在<{1}-{2}行-{3}列-{4}>位置\r\n不能执行<{5}-{6}行-{7}列-{8}>操作", this.RunName, this.robotDebugAction.stationName
                                , this.robotDebugAction.row + 1, this.robotDebugAction.col + 1, RobotDef.RobotOrderName[(int)this.robotDebugAction.order]
                                , GetRobotStationName(station), row + 1, col + 1, RobotDef.RobotOrderName[(int)order]);
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            return false;
                        }
                        return true;
                        break;
                    }
                default:
                    ShowMsgBox.ShowDialog(string.Format("{0} 非{1}动作，不能操作{1}", order.ToString(), RobotDef.RobotIDName[(int)this.RobotID]), MessageType.MsgWarning);
                    return false;
                    break;
            }
            #endregion

            #region // 判断工位行列夹具状态

            // 目标位夹具：0未知，1为OFF，2为ON，3为错误
            int pltState = (int)OvenStatus.Unknown;
            MCState mcState = MCState.MCIdle;
            switch(station)
            {
                case TransferRobotStation.OnloadStation:
                    {
                        runId = RunID.OnloadRobot;
                        mcState = MachineCtrl.GetInstance().GetModuleMCState(runId);
                        RobotActionInfo rbtAction = MachineCtrl.GetInstance().GetRobotActionInfo(runId, false);
                        if (null == rbtAction)
                        {
                            msg = string.Format("无法获取{0}动作状态，不能操作{1}", GetRobotStationName(station), RobotDef.RobotIDName[(int)this.RobotID]);
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            return false;
                        }
                        else if (RobotOrder.HOME != rbtAction.order)
                        {
                            msg = string.Format("{0}非【{1}】动作状态，不能操作{2}", GetRobotStationName(station)
                                , RobotDef.RobotOrderName[(int)RobotOrder.HOME], RobotDef.RobotIDName[(int)this.RobotID]);
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            return false;
                        }
                        pltState = MachineCtrl.GetInstance().GetPalletPosSenser(runId, col);
                        break;
                    }
                case TransferRobotStation.PalletBuffer:
                    {
                        runId = RunID.PalletBuffer;
                        mcState = MachineCtrl.GetInstance().GetModuleMCState(runId);
                        pltState = MachineCtrl.GetInstance().GetPalletPosSenser(runId, row);
                        break;
                    }
                case TransferRobotStation.ManualOperate:
                    {
                        runId = RunID.ManualOperate;
                        mcState = MachineCtrl.GetInstance().GetModuleMCState(runId);
                        pltState = MachineCtrl.GetInstance().GetPalletPosSenser(runId, col);
                        break;
                    }
                case TransferRobotStation.OffloadStation:
                    {
                        runId = RunID.OffloadBattery;
                        mcState = MachineCtrl.GetInstance().GetModuleMCState(runId);
                        RobotActionInfo rbtAction = MachineCtrl.GetInstance().GetRobotActionInfo(runId, false);
                        if(null == rbtAction)
                        {
                            msg = string.Format("无法获取{0}XYZ轴电机动作状态，不能操作{1}", GetRobotStationName(station), RobotDef.RobotIDName[(int)this.RobotID]);
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            return false;
                        }
                        else if (RobotOrder.HOME != rbtAction.order)
                        {
                            msg = string.Format("{0}XYZ电机不在安全位状态，不能操作{1}", GetRobotStationName(station), RobotDef.RobotIDName[(int)this.RobotID]);
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            return false;
                        }
                        pltState = MachineCtrl.GetInstance().GetPalletPosSenser(runId, col);
                        break;
                    }
                default:
                    {
                        if ((station >= TransferRobotStation.DryOven_0) && (station <= TransferRobotStation.DryOven_All))
                        {
                            runId = RunID.DryOven0 + (int)station - (int)TransferRobotStation.DryOven_0;
                            mcState = MachineCtrl.GetInstance().GetModuleMCState(runId);
                            if((RobotOrder.PICKIN == order) || (RobotOrder.PLACEIN == order))
                            {
                                if((short)OvenStatus.DoorOpen != GetDryingOvenDoorOpenState(runId, row))
                                {
                                    msg = string.Format("{0}-{1}层炉门非【打开】状态，不能操作{2}{3}"
                                        , GetRobotStationName(station), (row + 1), RobotDef.RobotIDName[(int)this.RobotID], RobotDef.RobotOrderName[(int)order]);
                                    ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                    return false;
                                }
                                if((short)OvenStatus.SafetyCurtainOn != GetDryingOvenSafetyCurtain(runId, row))
                                {
                                    msg = string.Format("{0}-{1}层安全光幕非【ON】状态，不能操作{2}{3}"
                                        , GetRobotStationName(station), (row + 1), RobotDef.RobotIDName[(int)this.RobotID], RobotDef.RobotOrderName[(int)order]);
                                    ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                    return false;
                                }
                            }
                            int pltIdx = (row * (int)OvenRowCol.MaxCol + col);
                            pltState = MachineCtrl.GetInstance().GetPalletPosSenser(runId, pltIdx);
                            break;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    }
            }
            if (!PalletKeepFlat(0, false, false) && !PalletKeepFlat(0, true, false))
            {
                msg = string.Format("机器人插料架夹具放平感应器状态不一致，不能取放夹具\r\n请先检查插料架感应器状态，确保正确后再操作！");
                ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                return false;
            }
            if((MCState.MCInitComplete != mcState) && (MCState.MCStopRun != mcState))
            {
                msg = $"{GetRobotStationName(station)}的上位机软件非【初始化完成】或【运行停止】状态，不能操作{RobotDef.RobotIDName[(int)this.RobotID]}";
                msg += $"\r\n处理方法：请先按启动按钮将【{GetRobotStationName(station)}】上位机软件初始化！";
                ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                return false;
            }
            // 目标位夹具：0未知，1为OFF，2为ON，3为错误
            switch((OvenStatus)pltState)
            {
                case OvenStatus.Unknown:
                    {
                        msg = string.Format("无法获取{0}-{1}行{2}列夹具感应器状态，不能操作{3}"
                            , GetRobotStationName(station), (row + 1), (col + 1), RobotDef.RobotIDName[(int)this.RobotID]);
                        ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                        return false;
                        break;
                    }
                case OvenStatus.PalletNot:
                    {
                        // 目标工位无夹具，仅人工操作位可取
                        if((RobotOrder.PICKIN == order))
                        {
                            msg = string.Format("{0}-{1}行{2}列夹具感应器OFF 无夹具，不能操作{3}{4}"
                                , GetRobotStationName(station), (row + 1), (col + 1), RobotDef.RobotIDName[(int)this.RobotID], RobotDef.RobotOrderName[(int)order]);
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            return false;
                        }
                        // 目标工位无夹具，插料架无夹具，禁止放进
                        if((RobotOrder.PLACEIN == order) && !PalletKeepFlat(0, true, false))
                        {
                            msg = string.Format("{0}-{1}行{2}列夹具感应器OFF 无夹具，插料架放平感应OFF 无夹具，不能操作{3}{4}"
                                , GetRobotStationName(station), (row + 1), (col + 1), RobotDef.RobotIDName[(int)this.RobotID], RobotDef.RobotOrderName[(int)order]);
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            return false;
                        }
                        break;
                    }
                case OvenStatus.PalletHave:
                    {
                        if(RobotOrder.PLACEIN == order)
                        {
                            msg = string.Format("{0}-{1}行{2}列夹具感应器ON 有夹具，不能操作{3}{4}"
                                , GetRobotStationName(station), (row + 1), (col + 1), RobotDef.RobotIDName[(int)this.RobotID], RobotDef.RobotOrderName[(int)order]);
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            return false;
                        }
                        break;
                    }
                case OvenStatus.PalletErrror:
                    {
                        msg = string.Format("{0}-{1}行{2}列夹具感应器状态错误请检查，不能操作{3}"
                            , GetRobotStationName(station), (row + 1), (col + 1), RobotDef.RobotIDName[(int)this.RobotID]);
                        ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                        return false;
                        break;
                    }
                default:
                    break;
            }
            #endregion

            return true;
        }

        /// <summary>
        /// 自动运行时检查目标工位安全状态
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        private bool CheckStationSafe(PickPlacePos pos, RobotOrder order)
        {
            if (Def.IsNoHardware())
            {
                return true;
            }
            string msg, dispose;
            msg = dispose = "";
            RunID runId = RunID.Invalid;
            int pltState = 0;
            switch(pos.station)
            {
                case TransferRobotStation.OnloadStation:
                    {
                        runId = RunID.OnloadRobot;
                        pltState = MachineCtrl.GetInstance().GetPalletPosSenser(RunID.OnloadRobot, pos.col);
                        break;
                    }
                case TransferRobotStation.PalletBuffer:
                    {
                        runId = RunID.PalletBuffer;
                        pltState = MachineCtrl.GetInstance().GetPalletPosSenser(RunID.PalletBuffer, pos.row);
                        break;
                    }
                case TransferRobotStation.ManualOperate:
                    {
                        runId = RunID.ManualOperate;
                        pltState = MachineCtrl.GetInstance().GetPalletPosSenser(RunID.ManualOperate, pos.col);
                        break;
                    }
                case TransferRobotStation.OffloadStation:
                    {
                        runId = RunID.OffloadBattery;
                        pltState = MachineCtrl.GetInstance().GetPalletPosSenser(RunID.OffloadBattery, pos.col);
                        break;
                    }
                default:
                    {
                        if ((pos.station >= TransferRobotStation.DryOven_0) && (pos.station <= TransferRobotStation.DryOven_All))
                        {
                            runId = RunID.DryOven0 + ((int)pos.station - (int)TransferRobotStation.DryOven_0);
                            int pltIdx = (pos.row * (int)OvenRowCol.MaxCol + pos.col);
                            pltState = MachineCtrl.GetInstance().GetPalletPosSenser(runId, pltIdx);
                            if((short)OvenStatus.DoorOpen != GetDryingOvenDoorOpenState(runId, pos.row))
                            {
                                msg = string.Format("{0}-{1}层炉门非【打开】状态，不能操作{2}{3}"
                                    , GetRobotStationName(pos.station), (pos.row + 1), RobotDef.RobotIDName[(int)this.RobotID], RobotDef.RobotOrderName[(int)order]);
                                ShowMessageBox((int)MsgID.OvenDoorStateErr, msg, "", MessageType.MsgAlarm);
                                return false;
                            }
                            if((short)OvenStatus.SafetyCurtainOn != GetDryingOvenSafetyCurtain(runId, pos.row))
                            {
                                msg = string.Format("{0}-{1}层安全光幕非【ON】状态，不能操作{2}{3}"
                                    , GetRobotStationName(pos.station), (pos.row + 1), RobotDef.RobotIDName[(int)this.RobotID], RobotDef.RobotOrderName[(int)order]);
                                ShowMessageBox((int)MsgID.OvenSafetyCurtainErrr, msg, "", MessageType.MsgAlarm);
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    }
            }
            if((MCState.MCRunning != MachineCtrl.GetInstance().GetModuleMCState(runId)) 
                && !MachineCtrl.GetInstance().GetModuleRunning(runId))
            {
                msg = string.Format("{0} 模组非自动运行状态，不能自动取放进", GetRobotStationName(pos.station));
                dispose = string.Format("请将 {0} 模组启动运行", GetRobotStationName(pos.station));
                ShowMessageBox((int)MsgID.DestStationStop, msg, dispose, MessageType.MsgAlarm);
                return false;
            }
            // 目标位夹具：0未知，1为OFF，2为ON，3为错误
            if((int)OvenStatus.Unknown == pltState)
            {
                msg = string.Format("无法获取 {0} 工位{1}行{2}列夹具位感应器状态，不能取进", GetRobotStationName(pos.station), (pos.row + 1), (pos.col + 1));
                dispose = string.Format("请检查 {0} 工位感应器正确后再启动软件", GetRobotStationName(pos.station));
                ShowMessageBox((int)MsgID.DestStationSenserErr, msg, dispose, MessageType.MsgAlarm);
                return false;
            }
            if((int)OvenStatus.PalletErrror == pltState)
            {
                msg = string.Format("{0} 工位{1}行{2}列夹具位感应器状态错误，不能取进", GetRobotStationName(pos.station), (pos.row + 1), (pos.col + 1));
                dispose = string.Format("请检查 {0} 工位感应器正确后再启动软件", GetRobotStationName(pos.station));
                ShowMessageBox((int)MsgID.DestStationSenserErr, msg, dispose, MessageType.MsgAlarm);
                return false;
            }
            if(RobotOrder.PICKIN == order)
            {
                if(!PalletKeepFlat(0, false, false))
                {
                    msg = $"{RobotDef.RobotIDName[(int)this.RobotID]} 夹具位感应器状态非【OFF】，不能取进";
                    dispose = string.Format("请检查 {0} 插料架夹具感应器正确后再启动软件", GetRobotStationName(pos.station));
                    ShowMessageBox((int)MsgID.FingerPltStateErr, msg, dispose, MessageType.MsgAlarm);
                    return false;
                }
                if((int)OvenStatus.PalletHave != pltState)
                {
                    msg = string.Format("{0} 工位{1}行{2}列夹具位感应器状态非【ON】，不能取进", GetRobotStationName(pos.station), (pos.row + 1), (pos.col + 1));
                    dispose = string.Format("请检查 {0} 工位感应器正确后再启动软件", GetRobotStationName(pos.station));
                    ShowMessageBox((int)MsgID.DestStationSenserErr, msg, dispose, MessageType.MsgAlarm);
                    return false;
                }
            }
            else if(RobotOrder.PLACEIN == order)
            {
                if(!PalletKeepFlat(0, true, false))
                {
                    msg = $"{RobotDef.RobotIDName[(int)this.RobotID]} 夹具位感应器状态非【ON】，不能取进";
                    dispose = string.Format("请检查 {0} 插料架夹具感应器正确后再启动软件", GetRobotStationName(pos.station));
                    ShowMessageBox((int)MsgID.FingerPltStateErr, msg, dispose, MessageType.MsgAlarm);
                    return false;
                }
                if((int)OvenStatus.PalletNot != pltState)
                {
                    msg = string.Format("{0} 工位{1}行{2}列夹具位感应器状态非【OFF】，不能放进", GetRobotStationName(pos.station), (pos.row + 1), (pos.col + 1));
                    dispose = string.Format("请检查 {0} 工位感应器正确后再启动软件", GetRobotStationName(pos.station));
                    ShowMessageBox((int)MsgID.DestStationSenserErr, msg, dispose, MessageType.MsgAlarm);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 测试机器人工位移动
        /// </summary>
        private void TestRobotStation()
        {
            Random rd = new Random();
            TransferRobotStation station = (TransferRobotStation)rd.Next((int)TransferRobotStation.OnloadStation, (int)TransferRobotStation.StationEnd);
            int row, col;
            row = col = 0;
            if (this.robotStationInfo.ContainsKey(station))
            {
                row = rd.Next(0, this.robotStationInfo[station].maxRow);
                col = rd.Next(0, this.robotStationInfo[station].maxCol);
            }
            string info = string.Format("{0}-{1}-{2}", (int)station, row, col);
            if(!this.testRbtStation.ContainsKey(info))
            {
                this.testRbtStation.Add(info, null);
                if(GetRobotCmd(station, row, col, robotSpeed, RobotOrder.MOVE, ref this.robotCmd))
                {
                    CurMsgStr(string.Format("测试机器人<{0}-{1}-{2}-移动>", GetRobotStationName(station), robotCmd[1], robotCmd[2]), "test robot move");
                    if(RobotMove(robotCmd))
                    {
                        return;
                        if (GetRobotCmd(TransferRobotStation.DryOven_All, 3, 1, robotSpeed, RobotOrder.MOVE, ref this.robotCmd))
                        {
                            CurMsgStr(string.Format("测试机器人<{0}-{1}-{2}-移动>", GetRobotStationName(TransferRobotStation.DryOven_All), robotCmd[1], robotCmd[2]), "test robot move");
                            RobotMove(robotCmd);
                        }
                    }
                }
            }
            else if(this.testRbtStation.Count >= ((int)OvenInfoCount.OvenCount * 8 + 6 + 4 + 1 + 2))
            {
                CurMsgStr("已随机测试完成机器人所有工位", "Moudle not enable");
            }
        }

        #endregion

        #region // 匹配路径

        /// <summary>
        /// 计算从上料取事件
        /// </summary>
        /// <param name="pick"></param>
        /// <param name="place"></param>
        /// <returns></returns>
        private bool CalcOnloadPick(ref PickPlacePos pick, ref PickPlacePos place)
        {
            // 上料区取NG空夹具
            if (SearchOnloadPickPos(EventList.OnloadPickNGEmptyPallet, ref pick))
            {
                // 人工操作台放NG空夹具
                if (SearchManualPlacePos(EventList.ManualPlaceNGEmptyPallet, ref place))
                {
                    return true;
                }
                // 缓存架放NG空夹具
                if (SearchPalletBufferPlacePos(EventList.PalletBufferPlaceNGEmptyPallet, ref place))
                {
                    return true;
                }
            }
            // 优先取无假电池夹具，尽早填充抽检炉腔
            // 上料区取OK无假电池满夹具
            if(SearchOnloadPickPos(EventList.OnloadPickOKFullPallet, ref pick))
            {
                // 干燥炉放
                if (SearchGlobalsDryOven(EventList.DryOvenPlaceOnlOKFullPallet, ref place, false, false))
                {
                    return true;
                }
            }
            // 上料区取OK带假电池满夹具
            if(SearchOnloadPickPos(EventList.OnloadPickOKFakeFullPallet, ref pick))
            {
                // 干燥炉放
                if(SearchGlobalsDryOven(EventList.DryOvenPlaceOnlOKFakeFullPallet, ref place, false, false))
                {
                    return true;
                }
            }
            // 上料区取等待水含量结果夹具（已取待测假电池的夹具）
            if(SearchOnloadPickPos(EventList.OnLoadPickWaitResultPallet, ref pick))
            {
                // 获取夹具原位置信息
                Pallet pickPlt = new Pallet();
                Pallet placePlt = new Pallet();
                if((pick.col > -1 && pick.col < (int)ModuleMaxPallet.OnloadRobot)
                    && GetModulePallet(RunID.OnloadRobot, pick.col, ref pickPlt))
                {
                    if(((int)TransferRobotStation.DryOven_0 <= pickPlt.SrcStation) && (pickPlt.SrcStation <= (int)TransferRobotStation.DryOven_All))
                    {
                        // 原干燥炉
                        RunID ovenId = RunID.DryOven0 + (pickPlt.SrcStation - (int)TransferRobotStation.DryOven_0);
                        int pltIdx = -1;
                        if((EventStatus.Require == GetModuleEvent(ovenId, EventList.DryOvenPlaceWaitResultPallet, ref pltIdx))
                            && (pltIdx > -1 && pltIdx < (int)ModuleMaxPallet.DryingOven)
                            && (pickPlt.SrcRow > -1 && pickPlt.SrcRow < (int)OvenRowCol.MaxRow)
                            && (pickPlt.SrcCol > -1 && pickPlt.SrcCol < (int)OvenRowCol.MaxCol)
                            && GetModuleEnable(ovenId) && GetModuleRunning(ovenId)
                            && GetDryingOvenCavityEnable(ovenId, pickPlt.SrcRow)
                            && !GetDryingOvenCavityPressure(ovenId, pickPlt.SrcRow)
                            && !GetDryingOvenCavityTransfer(ovenId, pickPlt.SrcRow)
                            && (CavityStatus.WaitDetect == GetDryingOvenCavityState(ovenId, pickPlt.SrcRow))
                            && GetModulePallet(ovenId, (pickPlt.SrcRow * (int)OvenRowCol.MaxCol + pickPlt.SrcCol), ref placePlt))
                        {
                            if(PalletStatus.Invalid == placePlt.State)
                            {
                                place.SetData(ovenId, (TransferRobotStation)pickPlt.SrcStation, pickPlt.SrcRow, pickPlt.SrcCol, EventList.DryOvenPlaceWaitResultPallet);
                                return true;
                            }
                        }
                    }
                }
            }
            // 上料区取回炉假电池夹具（已放回假电池的夹具）
            if(SearchOnloadPickPos(EventList.OnloadPickRebakeFakePallet, ref pick))
            {
                // 干燥炉放
                // 获取夹具原位置信息
                Pallet pickPlt = new Pallet();
                Pallet placePlt = new Pallet();
                if((pick.col > -1 && pick.col < (int)ModuleMaxPallet.OnloadRobot)
                    && GetModulePallet(RunID.OnloadRobot, pick.col, ref pickPlt))
                {
                    if(((int)TransferRobotStation.DryOven_0 <= pickPlt.SrcStation) && (pickPlt.SrcStation <= (int)TransferRobotStation.DryOven_All))
                    {
                        // 原干燥炉
                        int pltIdx = -1;
                        RunID ovenId = RunID.DryOven0 + (pickPlt.SrcStation - (int)TransferRobotStation.DryOven_0);
                        if((EventStatus.Require == GetModuleEvent(ovenId, EventList.DryOvenPlaceRebakeFakePallet, ref pltIdx))
                            && (pltIdx > -1 && pltIdx < (int)ModuleMaxPallet.DryingOven)
                            && (pickPlt.SrcRow > -1 && pickPlt.SrcRow < (int)OvenRowCol.MaxRow)
                            && (pickPlt.SrcCol > -1 && pickPlt.SrcCol < (int)OvenRowCol.MaxCol)
                            && GetModuleEnable(ovenId) && GetModuleRunning(ovenId)
                            && GetDryingOvenCavityEnable(ovenId, pickPlt.SrcRow)
                            && !GetDryingOvenCavityPressure(ovenId, pickPlt.SrcRow)
                            && !GetDryingOvenCavityTransfer(ovenId, pickPlt.SrcRow)
                            && (CavityStatus.WaitRebaking == GetDryingOvenCavityState(ovenId, pickPlt.SrcRow))
                            && GetModulePallet(ovenId, (pickPlt.SrcRow * (int)OvenRowCol.MaxCol + pickPlt.SrcCol), ref placePlt))
                        {
                            if(PalletStatus.Invalid == placePlt.State)
                            {
                                place.SetData(ovenId, (TransferRobotStation)pickPlt.SrcStation, pickPlt.SrcRow, pickPlt.SrcCol, EventList.DryOvenPlaceRebakeFakePallet);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 计算上料放事件
        /// </summary>
        /// <param name="pick"></param>
        /// <param name="place"></param>
        /// <returns></returns>
        private bool CalcOnloadPlace(ref PickPlacePos pick, ref PickPlacePos place)
        {
            // 上料区放待回炉含假电池夹具（已取走假电池，待重新放回假电池的夹具）
            if(SearchOnloadPlacePos(EventList.OnloadPlaceReputFakePallet, ref place))
            {
                // 干燥炉取待回炉含假电池夹具（已取走假电池，待重新放回假电池的夹具）
                if(SearchGlobalsDryOven(EventList.DryOvenPickReputFakePallet, ref pick, true, false))
                {
                    return true;
                }
            }
            // 上料区放NG非空夹具，转盘
            if(SearchOnloadPlacePos(EventList.OnloadPlaceNGPallet, ref place))
            {
                // 下料区取NG夹具（非空）
                if (SearchOffloadPickPos(EventList.OffLoadPickNGPallet, ref pick))
                {
                    return true;
                }
                // 干燥炉取NG非空夹具
                if (SearchGlobalsDryOven(EventList.DryOvenPickNGPallet, ref pick, true, false))
                {
                    return true;
                }
            }
            // 上料区放待检测含假电池夹具（未取走假电池的夹具）
            if(SearchOnloadPlacePos(EventList.OnLoadPlaceDetectFakePallet, ref place))
            {
                // 干燥炉取待检测含假电池夹具（未取走假电池的夹具）
                if(SearchGlobalsDryOven(EventList.DryOvenPickDetectFakePallet, ref pick, true, false))
                {
                    if(this.offloadPltCount >= this.offloadDetect)
                    {
                        this.offloadPltCount = 0;
                    }
                    return true;
                }
                else
                {
                    // 下一次下料夹具后，尽快测试水含量
                    this.offloadPltCount = this.offloadDetect - 1;
                }
            }
            // 上料区放空夹具
            if(SearchOnloadPlacePos(EventList.OnloadPlaceEmptyPallet, ref place))
            {
                // 下料区取空夹具
                if(SearchOffloadPickPos(EventList.OffLoadPickEmptyPallet, ref pick))
                {
                    return true;
                }
                // 如果干燥炉空余位较多，则优先取其他位置的空托盘
                if(GetDryOvenEmptyPosCount() > 3)
                {
                    // 人工操作台取OK空夹具
                    if(SearchManualPickPos(EventList.ManualPickEmptyPallet, ref pick))
                    {
                        return true;
                    }
                    // 缓存架取空夹具
                    if(SearchPalletBufferPickPos(EventList.PalletBufferPickEmptyPallet, ref pick))
                    {
                        return true;
                    }
                }
                // 干燥炉取空夹具
                if(SearchGlobalsDryOven(EventList.DryOvenPickEmptyPallet, ref pick, true, false))
                {
                    return true;
                }
                // 缓存架取空夹具
                if(SearchPalletBufferPickPos(EventList.PalletBufferPickEmptyPallet, ref pick))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 计算下料取事件
        /// </summary>
        /// <param name="pick"></param>
        /// <param name="place"></param>
        /// <returns></returns>
        private bool CalcOffloadPick(ref PickPlacePos pick, ref PickPlacePos place)
        {
            // 下料区取空夹具
            if (SearchOffloadPickPos(EventList.OffLoadPickEmptyPallet, ref pick))
            {
                // 上料区放空夹具
                if (SearchOnloadPlacePos(EventList.OnloadPlaceEmptyPallet, ref place))
                {
                    return true;
                }
                // 缓存架放空夹具
                if (SearchPalletBufferPlacePos(EventList.PalletBufferPlaceEmptyPallet, ref place))
                {
                    return true;
                }
                // 干燥炉放空夹具
                if (SearchGlobalsDryOven(EventList.DryOvenPlaceEmptyPallet, ref place, false, true))
                {
                    return true;
                }
            }
            // 下料区取等待水含量结果夹具（已取待测假电池的夹具）
            if (SearchOffloadPickPos(EventList.OffLoadPickWaitResultPallet, ref pick))
            {
                // 获取夹具原位置信息
                Pallet pickPlt = new Pallet();
                Pallet placePlt = new Pallet();
                if ((pick.col > -1 && pick.col < (int)ModuleMaxPallet.OffloadBattery)
                    && GetModulePallet(RunID.OffloadBattery, pick.col, ref pickPlt))
                {
                    if (((int)TransferRobotStation.DryOven_0 <= pickPlt.SrcStation) && (pickPlt.SrcStation <= (int)TransferRobotStation.DryOven_All))
                    {
                        // 原干燥炉
                        RunID ovenId = RunID.DryOven0 + (pickPlt.SrcStation - (int)TransferRobotStation.DryOven_0);
                        int pltIdx = -1;
                        if((EventStatus.Require == GetModuleEvent(ovenId, EventList.DryOvenPlaceWaitResultPallet, ref pltIdx))
                            && (pltIdx > -1 && pltIdx < (int)ModuleMaxPallet.DryingOven)
                            && (pickPlt.SrcRow > -1 && pickPlt.SrcRow < (int)OvenRowCol.MaxRow)
                            && (pickPlt.SrcCol > -1 && pickPlt.SrcCol < (int)OvenRowCol.MaxCol)
                            && GetModuleEnable(ovenId) && GetModuleRunning(ovenId)
                            && GetDryingOvenCavityEnable(ovenId, pickPlt.SrcRow)
                            && !GetDryingOvenCavityPressure(ovenId, pickPlt.SrcRow)
                            && !GetDryingOvenCavityTransfer(ovenId, pickPlt.SrcRow)
                            && (CavityStatus.WaitDetect == GetDryingOvenCavityState(ovenId, pickPlt.SrcRow))
                            && GetModulePallet(ovenId, (pickPlt.SrcRow * (int)OvenRowCol.MaxCol + pickPlt.SrcCol), ref placePlt))
                        {
                            if(PalletStatus.Invalid == placePlt.State)
                            {
                                place.SetData(ovenId, (TransferRobotStation)pickPlt.SrcStation, pickPlt.SrcRow, pickPlt.SrcCol, EventList.DryOvenPlaceWaitResultPallet);
                                return true;
                            }
                        }
                    }
                }
            }
            // 下料区取NG空夹具
            if (SearchOffloadPickPos(EventList.OffLoadPickNGEmptyPallet, ref pick))
            {
                // 人工操作台放NG空夹具
                if (SearchManualPlacePos(EventList.ManualPlaceNGEmptyPallet, ref place))
                {
                    return true;
                }
                // 缓存架放NG空夹具
                if (SearchPalletBufferPlacePos(EventList.PalletBufferPlaceNGEmptyPallet, ref place))
                {
                    return true;
                }
                // 干燥炉放NG空夹具
                if (SearchGlobalsDryOven(EventList.DryOvenPlaceNGEmptyPallet, ref place, false, true))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 计算下料放事件
        /// </summary>
        /// <param name="pick"></param>
        /// <param name="place"></param>
        /// <returns></returns>
        private bool CalcOffloadPlace(ref PickPlacePos pick, ref PickPlacePos place)
        {
            // 下料区放干燥完成夹具
            if ((this.offloadPltCount < this.offloadDetect) && SearchOffloadPlacePos(EventList.OffLoadPlaceDryFinishPallet, ref place))
            {
                // 干燥炉取干燥完成夹具（等待下料）
                if(SearchGlobalsDryOven(EventList.DryOvenPickDryFinishPallet, ref pick, true, false))
                {
                    this.offloadPltCount++;
                    return true;
                }
            }
            // 下料区放待检测含假电池夹具（未取走假电池的夹具）
            if (SearchOffloadPlacePos(EventList.OffLoadPlaceDetectFakePallet, ref place))
            {
                // 干燥炉取待检测含假电池夹具（未取走假电池的夹具）
                if (SearchGlobalsDryOven(EventList.DryOvenPickDetectFakePallet, ref pick, true, false))
                {
                    if (this.offloadPltCount >= this.offloadDetect)
                    {
                        this.offloadPltCount = 0;
                    }
                    return true;
                }
                else
                {
                    // 下一次下料夹具后，尽快测试水含量
                    this.offloadPltCount = this.offloadDetect - 1;
                }
            }
            // 下料区放NG夹具（非空）
            if(SearchOffloadPlacePos(EventList.OffLoadPlaceNGPallet, ref place))
            {
                // 干燥炉取NG非空夹具
                if(SearchGlobalsDryOven(EventList.DryOvenPickNGPallet, ref pick, true, true))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 计算人工台取
        /// </summary>
        /// <param name="pick"></param>
        /// <param name="place"></param>
        /// <returns></returns>
        private bool CalcManualPick(ref PickPlacePos pick, ref PickPlacePos place)
        {
            // 人工操作台取OK空夹具
            if (SearchManualPickPos(EventList.ManualPickEmptyPallet, ref pick)
                && (GetDryOvenEmptyPosCount() > 4))
            {
                // 上料区放空夹具
                if(SearchOnloadPlacePos(EventList.OnloadPlaceEmptyPallet, ref place))
                {
                    return true;
                }
                // 缓存架放空夹具
                if (SearchPalletBufferPlacePos(EventList.PalletBufferPlaceEmptyPallet, ref place))
                {
                    return true;
                }
                // 干燥炉放空夹具
                if (SearchGlobalsDryOven(EventList.DryOvenPlaceEmptyPallet, ref place, false, true))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 计算人工台放
        /// </summary>
        /// <param name="pick"></param>
        /// <param name="place"></param>
        /// <returns></returns>
        private bool CalcManualPlace(ref PickPlacePos pick, ref PickPlacePos place)
        {
            // 人工操作台放NG空夹具
            if (SearchManualPlacePos(EventList.ManualPlaceNGEmptyPallet, ref place))
            {
                // 下料区取NG空夹具
                if (SearchOffloadPickPos(EventList.OffLoadPickNGEmptyPallet, ref pick))
                {
                    return true;
                }
                // 上料区取NG空夹具
                if (SearchOnloadPickPos(EventList.OnloadPickNGEmptyPallet, ref pick))
                {
                    return true;
                }
                // 干燥炉取NG空夹具
                if (SearchGlobalsDryOven(EventList.DryOvenPickNGEmptyPallet, ref pick, true, false))
                {
                    return true;
                }
                // 缓存架取NG空夹具
                if(SearchPalletBufferPickPos(EventList.PalletBufferPickNGEmptyPallet, ref pick))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 计算转炉换腔
        /// </summary>
        /// <param name="pick"></param>
        /// <param name="place"></param>
        /// <returns></returns>
        private bool CalcTransferDryOven(ref PickPlacePos pick, ref PickPlacePos place)
        {
            int pltIdx = -1;
            Pallet plt = new Pallet();
            for(RunID pickId = RunID.DryOven0; pickId < RunID.DryOvenALL; pickId++)
            {
                // 模组使能，运行状态
                if(!GetModuleEnable(pickId) || !GetModuleRunning(pickId))
                {
                 //   return false;
                    continue;
                }
                EventStatus pickState = GetModuleEvent(pickId, EventList.DryOvenPickTransferPallet, ref pltIdx);
                // 非第一次取 || 第一次取
                if((EventStatus.Start == pickState) || (EventStatus.Require == pickState))
                {
                    int pickRow = pltIdx / (int)OvenRowCol.MaxCol;
                    for(int pickCol = 0; pickCol < (int)OvenRowCol.MaxCol; pickCol++)
                    {
                        if(GetDryingOvenCavityEnable(pickId, pickRow) && !GetDryingOvenCavityPressure(pickId, pickRow) && GetDryingOvenCavityTransfer(pickId, pickRow)
                            && GetModulePallet(pickId, (pickRow * (int)OvenRowCol.MaxCol + pickCol), ref plt) && (plt.State > PalletStatus.Invalid))
                        {
                            for(RunID placeId = RunID.DryOven0; placeId < RunID.DryOvenALL; placeId++)
                            {
                                // 模组使能，运行状态，非同一干燥炉转移换腔
                                if(!GetModuleEnable(placeId) || !GetModuleRunning(placeId) || (pickId == placeId))
                                {
                                    continue;
                                }
                                EventStatus placeState = GetModuleEvent(placeId, EventList.DryOvenPlaceTransferPallet, ref pltIdx);
                                if(pickState == placeState)
                                {
                                    int placeRow = pltIdx / (int)OvenRowCol.MaxCol;
                                    // 第一次取，则需要一个空炉腔放
                                    if(EventStatus.Require == pickState)
                                    {
                                        bool emptyCavity = true;
                                        for(int placeCol = 0; placeCol < (int)OvenRowCol.MaxCol; placeCol++)
                                        {
                                            if(GetModulePallet(placeId, (placeRow * (int)OvenRowCol.MaxCol + placeCol), ref plt) && (plt.State != PalletStatus.Invalid))
                                            {
                                                emptyCavity = false;
                                                break;
                                            }
                                        }
                                        if (!emptyCavity)
                                        {
                                            continue;
                                        }
                                    }
                                    for(int placeCol = 0; placeCol < (int)OvenRowCol.MaxCol; placeCol++)
                                    {
                                        if(GetDryingOvenCavityEnable(placeId, placeRow) && !GetDryingOvenCavityPressure(placeId, placeRow) && !GetDryingOvenCavityTransfer(placeId, placeRow)
                                            && GetModulePallet(placeId, (placeRow * (int)OvenRowCol.MaxCol + placeCol), ref plt) && (plt.State == PalletStatus.Invalid))
                                        {
                                            pick.SetData(pickId, (TransferRobotStation.DryOven_0 + (int)pickId - (int)RunID.DryOven0), pickRow, pickCol, EventList.DryOvenPickTransferPallet);
                                            place.SetData(placeId, (TransferRobotStation.DryOven_0 + (int)placeId - (int)RunID.DryOven0), placeRow, placeCol, EventList.DryOvenPlaceTransferPallet);
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        #endregion

        #region // 事件搜索

        /// <summary>
        /// 搜索上料取夹具位置
        /// </summary>
        /// <param name="searchEvent"></param>
        /// <param name="pick"></param>
        /// <returns></returns>
        private bool SearchOnloadPickPos(EventList searchEvent, ref PickPlacePos pick)
        {
            RunID runId = RunID.OnloadRobot;
            int pickPltIdx = -1;
            Pallet plt = new Pallet();
            // 模组使能，运行状态
            if (!GetModuleEnable(runId) || !GetModuleRunning(runId))
            {
                return false;
            }
            // 信号事件状态 || 夹具索引 || 模组夹具数据 || 模组夹具位置 不正确
            if ((EventStatus.Require != GetModuleEvent(runId, searchEvent, ref pickPltIdx))
                || !(pickPltIdx > -1 && pickPltIdx < (int)ModuleMaxPallet.OnloadRobot)
                || !GetModulePallet(runId, pickPltIdx, ref plt)
                || !GetPalletPosEnable(runId, pickPltIdx))
            {
                return false;
            }
            switch(searchEvent)
            {
                // 上料区取NG空夹具
                case EventList.OnloadPickNGEmptyPallet:
                    {
                        if((PalletStatus.NG == plt.State) && (plt.IsEmpty()))
                        {
                            pick.SetData(runId, TransferRobotStation.OnloadStation, 0, pickPltIdx, searchEvent);
                            return true;
                        }
                        break;
                    }
                // 上料区取OK满夹具
                case EventList.OnloadPickOKFullPallet:
                    {
                        //if((PalletStatus.OK == plt.State) && plt.IsFull() && !plt.HasFake())
                        if((PalletStatus.OK == plt.State) && (PalletStage.Onload == plt.Stage) && !plt.IsEmpty() && !plt.HasFake())
                        {
                            pick.SetData(runId, TransferRobotStation.OnloadStation, 0, pickPltIdx, searchEvent);
                            return true;
                        }
                        break;
                    }
                // 上料区取OK带假电池满夹具
                case EventList.OnloadPickOKFakeFullPallet:
                    {
                        //if((PalletStatus.OK == plt.State) && plt.IsFull() && plt.HasFake())
                        if((PalletStatus.OK == plt.State) && (PalletStage.Onload == plt.Stage) && plt.HasFake())
                        {
                            pick.SetData(runId, TransferRobotStation.OnloadStation, 0, pickPltIdx, searchEvent);
                            return true;
                        }
                        break;
                    }
                // 上料区取回炉假电池夹具（已放回假电池的夹具）
                case EventList.OnloadPickRebakeFakePallet:
                    {
                        //if((PalletStatus.Rebaking == plt.State) && plt.IsFull() && plt.HasFake())
                        if((PalletStatus.Rebaking == plt.State) && (PalletStage.Onload == plt.Stage) && plt.HasFake())
                        {
                            pick.SetData(runId, TransferRobotStation.OnloadStation, 0, pickPltIdx, searchEvent);
                            return true;
                        }
                        break;
                    }
                // 上料区取等待水含量结果夹具（已取待测假电池的夹具）
                case EventList.OnLoadPickWaitResultPallet:
                    {
                        if((PalletStatus.WaitResult == plt.State) && (PalletStage.Onload == plt.Stage) && plt.HasFake())
                        {
                            pick.SetData(runId, TransferRobotStation.OnloadStation, 0, pickPltIdx, searchEvent);
                            return true;
                        }
                        break;
                    }
            }
            return false;
        }

        /// <summary>
        /// 搜索上料放夹具
        /// </summary>
        /// <param name="searchEvent"></param>
        /// <param name="place"></param>
        /// <returns></returns>
        private bool SearchOnloadPlacePos(EventList searchEvent, ref PickPlacePos place)
        {
            RunID runId = RunID.OnloadRobot;
            int placePltIdx = -1;
            Pallet plt = new Pallet();
            // 模组使能，运行状态
            if(!GetModuleEnable(runId) || !GetModuleRunning(runId))
            {
                return false;
            }
            // 信号事件状态不正确
            if(EventStatus.Require != GetModuleEvent(runId, searchEvent, ref placePltIdx))
            {
                return false;
            }
            switch(searchEvent)
            {
                // 上料区放空夹具
                case EventList.OnloadPlaceEmptyPallet:
                    {
                        // 预留最后一个位置为放NG及回炉
                        for(placePltIdx = 0; placePltIdx < ((int)ModuleMaxPallet.OnloadRobot); placePltIdx++)
                        {
                            if (GetModulePallet(runId, placePltIdx, ref plt) && GetPalletPosEnable(runId, placePltIdx))
                            {
                                if(PalletStatus.Invalid == plt.State)
                                {
                                    place.SetData(runId, TransferRobotStation.OnloadStation, 0, placePltIdx, searchEvent);
                                    return true;
                                }
                            }
                        }
                        break;
                    }
                // 上料区放NG非空夹具，转盘
                case EventList.OnloadPlaceNGPallet:
                    {
                        placePltIdx = (int)ModuleMaxPallet.OnloadRobot - 1;
                        if((placePltIdx > -1 && placePltIdx < (int)ModuleMaxPallet.OnloadRobot) 
                            && GetModulePallet(runId, placePltIdx, ref plt) && GetPalletPosEnable(runId, placePltIdx))
                        {
                            if(PalletStatus.Invalid == plt.State)
                            {
                                place.SetData(runId, TransferRobotStation.OnloadStation, 0, placePltIdx, searchEvent);
                                return true;
                            }
                        }
                        break;
                    }
                // 上料区放待回炉含假电池夹具（已取走假电池，待重新放回假电池的夹具）
                case EventList.OnloadPlaceReputFakePallet:
                // 上料区放待检测含假电池夹具（未取走假电池的夹具）
                case EventList.OnLoadPlaceDetectFakePallet:
                    {
                        for(placePltIdx = 0; placePltIdx < ((int)ModuleMaxPallet.OnloadRobot); placePltIdx++)
                        {
                            if(GetModulePallet(runId, placePltIdx, ref plt) && GetPalletPosEnable(runId, placePltIdx))
                            {
                                if(PalletStatus.Invalid == plt.State)
                                {
                                    place.SetData(runId, TransferRobotStation.OnloadStation, 0, placePltIdx, searchEvent);
                                    return true;
                                }
                            }
                        }
                        break;

                        // 以下固定位置此项目不用
                        placePltIdx = (int)ModuleMaxPallet.OnloadRobot - 1;
                        if((placePltIdx > -1 && placePltIdx < (int)ModuleMaxPallet.OnloadRobot)
                            && GetModulePallet(runId, placePltIdx, ref plt) && GetPalletPosEnable(runId, placePltIdx))
                        {
                            if(PalletStatus.Invalid == plt.State)
                            {
                                place.SetData(runId, TransferRobotStation.OnloadStation, 0, placePltIdx, searchEvent);
                                return true;
                            }
                        }
                        break;
                    }
            }
            return false;
        }

        /// <summary>
        /// 搜索人工台取夹具
        /// </summary>
        /// <param name="searchEvent"></param>
        /// <param name="pick"></param>
        /// <returns></returns>
        private bool SearchManualPickPos(EventList searchEvent, ref PickPlacePos pick)
        {
            RunID runId = RunID.ManualOperate;
            int pickPltIdx = -1;
            Pallet plt = new Pallet();
            // 模组使能，运行状态
            if(!GetModuleEnable(runId) || !GetModuleRunning(runId))
            {
                return false;
            }
            // 信号事件状态不正确
            if((EventStatus.Require != GetModuleEvent(runId, searchEvent, ref pickPltIdx))
                || !(pickPltIdx > -1 && pickPltIdx < (int)ModuleMaxPallet.ManualOperate)
                || !GetModulePallet(runId, pickPltIdx, ref plt))
            {
                return false;
            }
            switch(searchEvent)
            {
                // 人工操作台取OK空夹具
                case EventList.ManualPickEmptyPallet:
                    {
                        if((PalletStatus.OK == plt.State) && plt.IsEmpty())
                        {
                            pick.SetData(runId, TransferRobotStation.ManualOperate, 0, pickPltIdx, searchEvent);
                            return true;
                        }
                        break;
                    }
            }
            return false;
        }

        /// <summary>
        /// 搜索人工操作台放位置
        /// </summary>
        /// <param name="searchEvent"></param>
        /// <param name="place"></param>
        /// <returns></returns>
        private bool SearchManualPlacePos(EventList searchEvent, ref PickPlacePos place)
        {
            RunID runId = RunID.ManualOperate;
            int placePltIdx = -1;
            Pallet plt = new Pallet();
            // 模组使能，运行状态
            if(!GetModuleEnable(runId) || !GetModuleRunning(runId))
            {
                return false;
            }
            // 信号事件状态不正确
            if((EventStatus.Require != GetModuleEvent(runId, searchEvent, ref placePltIdx))
                || !(placePltIdx > -1 && placePltIdx < (int)ModuleMaxPallet.ManualOperate)
                || !GetModulePallet(runId, placePltIdx, ref plt))
            {
                return false;
            }
            switch(searchEvent)
            {
                case EventList.ManualPlaceNGEmptyPallet:
                    {
                        if (PalletStatus.Invalid == plt.State)
                        {
                            place.SetData(runId, TransferRobotStation.ManualOperate, 0, placePltIdx, searchEvent);
                            return true;
                        }
                        break;
                    }
            }
            return false;
        }

        /// <summary>
        /// 搜索缓存架取夹具
        /// </summary>
        /// <param name="searchEvent"></param>
        /// <param name="pick"></param>
        /// <returns></returns>
        private bool SearchPalletBufferPickPos(EventList searchEvent, ref PickPlacePos pick)
        {
            RunID runId = RunID.PalletBuffer;
            int pickPltIdx = -1;
            Pallet plt = new Pallet();
            // 模组使能，运行状态
            if(!GetModuleEnable(runId) || !GetModuleRunning(runId))
            {
                return false;
            }
            // 信号事件状态不正确
            if((EventStatus.Require != GetModuleEvent(runId, searchEvent, ref pickPltIdx))
                || !(pickPltIdx > -1 && pickPltIdx < (int)ModuleMaxPallet.PalletBuffer)
                || !GetModulePallet(runId, pickPltIdx, ref plt)
                || !GetPalletBufferRowEnable(runId, (pickPltIdx)))
            {
                return false;
            }
            switch(searchEvent)
            {
                // 缓存架取空夹具
                case EventList.PalletBufferPickEmptyPallet:
                    {
                        if((PalletStatus.OK == plt.State) && plt.IsEmpty())
                        {
                            pick.SetData(runId, TransferRobotStation.PalletBuffer, pickPltIdx, 0, searchEvent);
                            return true;
                        }
                        break;
                    }
                // 缓存架取NG空夹具
                case EventList.PalletBufferPickNGEmptyPallet:
                    {
                        if((PalletStatus.NG == plt.State) && plt.IsEmpty())
                        {
                            pick.SetData(runId, TransferRobotStation.PalletBuffer, pickPltIdx, 0, searchEvent);
                            return true;
                        }
                        break;
                    }
            }
            return false;
        }

        /// <summary>
        /// 搜索夹具缓存架放位置
        /// </summary>
        /// <param name="searchEvent"></param>
        /// <param name="place"></param>
        /// <returns></returns>
        private bool SearchPalletBufferPlacePos(EventList searchEvent, ref PickPlacePos place)
        {
            RunID runId = RunID.PalletBuffer;
            int placePltIdx = -1;
            Pallet plt = new Pallet();
            // 模组使能，运行状态
            if(!GetModuleEnable(runId) || !GetModuleRunning(runId))
            {
                return false;
            }
            // 信号事件状态不正确
            if((EventStatus.Require != GetModuleEvent(runId, searchEvent, ref placePltIdx))
                || !(placePltIdx > -1 && placePltIdx < (int)ModuleMaxPallet.PalletBuffer)
                || !GetModulePallet(runId, placePltIdx, ref plt))
            {
                return false;
            }
            switch(searchEvent)
            {
                case EventList.PalletBufferPlaceEmptyPallet:
                case EventList.PalletBufferPlaceNGEmptyPallet:
                    {
                        if(GetPalletBufferRowEnable(runId, (placePltIdx)) && (PalletStatus.Invalid == plt.State))
                        {
                            place.SetData(runId, TransferRobotStation.PalletBuffer, placePltIdx, 0, searchEvent);
                            return true;
                        }
                        break;
                    }
            }
            return false;
        }

        /// <summary>
        /// 搜索干燥炉取夹具
        /// </summary>
        /// <param name="searchEvent"></param>
        /// <param name="pick"></param>
        /// <returns></returns>
        private bool SearchDryOvenPickPos(RunID runId, ModDef searchMode, EventList searchEvent, ref PickPlacePos pick)
        {
            int pickPltIdx = -1;
            Pallet plt = new Pallet();
            int ovenID = (int)runId - (int)RunID.DryOven0;
            // 模组使能，运行状态
            if(!GetModuleEnable(runId) || !GetModuleRunning(runId))
            {
                return false;
            }
            // 信号事件状态不正确
            if((EventStatus.Require != GetModuleEvent(runId, searchEvent, ref pickPltIdx)))
            {
                return false;
            }
            for(int ovenRow = 0; ovenRow < ((int)OvenRowCol.MaxRow); ovenRow++)
            {
                if(GetDryingOvenCavityEnable(runId, ovenRow) && !GetDryingOvenCavityPressure(runId, ovenRow) && !GetDryingOvenCavityTransfer(runId, ovenRow))
                {
                    switch(searchEvent)
                    {
                        // 干燥炉取空夹具
                        case EventList.DryOvenPickEmptyPallet:
                            {
                                if (CavityStatus.Normal == GetDryingOvenCavityState(runId, ovenRow))
                                {
                                    switch(searchMode)
                                    {
                                        case ModDef.PickSameAndInvalid:
                                            {
                                                if((GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.OK == plt.State) && plt.IsEmpty())
                                                    && (GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.Invalid == plt.State)))
                                                {
                                                    pick.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 0, searchEvent);
                                                    return true;
                                                }
                                                else if((GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.OK == plt.State) && plt.IsEmpty())
                                                    && (GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.Invalid == plt.State)))
                                                {
                                                    pick.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 1, searchEvent);
                                                    return true;
                                                }
                                                break;
                                            }
                                        case ModDef.PickSameAndOther:
                                            {
                                                if((GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.OK == plt.State) && plt.IsEmpty()))
                                                {
                                                    pick.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 0, searchEvent);
                                                    return true;
                                                }
                                                else if((GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.OK == plt.State) && plt.IsEmpty()))
                                                {
                                                    pick.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 1, searchEvent);
                                                    return true;
                                                }
                                                break;
                                            }
                                    }
                                }
                                break;
                            }
                        // 干燥炉取NG非空夹具
                        case EventList.DryOvenPickNGPallet:
                            {
                                if(CavityStatus.Normal == GetDryingOvenCavityState(runId, ovenRow))
                                {
                                    switch(searchMode)
                                    {
                                        case ModDef.PickSameAndInvalid:
                                            {
                                                if((GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.NG == plt.State) && !plt.IsEmpty())
                                                    && (GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.Invalid == plt.State)))
                                                {
                                                    pick.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 0, searchEvent);
                                                    return true;
                                                }
                                                else if((GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.NG == plt.State) && !plt.IsEmpty())
                                                    && (GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.Invalid == plt.State)))
                                                {
                                                    pick.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 1, searchEvent);
                                                    return true;
                                                }
                                                break;
                                            }
                                        case ModDef.PickSameAndOther:
                                            {
                                                if(GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.NG == plt.State) && !plt.IsEmpty())
                                                {
                                                    pick.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 0, searchEvent);
                                                    return true;
                                                }
                                                else if(GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.NG == plt.State) && !plt.IsEmpty())
                                                {
                                                    pick.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 1, searchEvent);
                                                    return true;
                                                }
                                                break;
                                            }
                                    }
                                }
                                break;
                            }
                        // 干燥炉取NG空夹具
                        case EventList.DryOvenPickNGEmptyPallet:
                            {
                                if(CavityStatus.Normal == GetDryingOvenCavityState(runId, ovenRow))
                                {
                                    switch(searchMode)
                                    {
                                        case ModDef.PickSameAndInvalid:
                                            {
                                                if((GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.NG == plt.State) && plt.IsEmpty())
                                                    && (GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.Invalid == plt.State)))
                                                {
                                                    pick.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 0, searchEvent);
                                                    return true;
                                                }
                                                else if((GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.NG == plt.State) && plt.IsEmpty())
                                                    && (GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.Invalid == plt.State)))
                                                {
                                                    pick.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 1, searchEvent);
                                                    return true;
                                                }
                                                break;
                                            }
                                        case ModDef.PickSameAndOther:
                                            {
                                                if((GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.NG == plt.State) && plt.IsEmpty()))
                                                {
                                                    pick.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 0, searchEvent);
                                                    return true;
                                                }
                                                else if((GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.NG == plt.State) && plt.IsEmpty()))
                                                {
                                                    pick.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 1, searchEvent);
                                                    return true;
                                                }
                                                break;
                                            }
                                    }
                                }
                                break;
                            }
                        // 干燥炉取待检测含假电池夹具（未取走假电池的夹具）
                        case EventList.DryOvenPickDetectFakePallet:
                            {
                                if(CavityStatus.WaitDetect == GetDryingOvenCavityState(runId, ovenRow))
                                {
                                    if(GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.Detect == plt.State) && plt.HasFake())
                                    {
                                        pick.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 0, searchEvent);
                                        return true;
                                    }
                                    else if(GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.Detect == plt.State) && plt.HasFake())
                                    {
                                        pick.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 1, searchEvent);
                                        return true;
                                    }
                                }
                                break;
                            }
                        // 干燥炉取待回炉含假电池夹具（已取走假电池，待重新放回假电池的夹具）
                        case EventList.DryOvenPickReputFakePallet:
                            {
                                if(CavityStatus.WaitRebaking == GetDryingOvenCavityState(runId, ovenRow))
                                {
                                    if(GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.ReputFake == plt.State) && plt.HasFake())
                                    {
                                        pick.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 0, searchEvent);
                                        return true;
                                    }
                                    else if(GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.ReputFake == plt.State) && plt.HasFake())
                                    {
                                        pick.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 1, searchEvent);
                                        return true;
                                    }
                                }
                                break;
                            }
                        // 干燥炉取干燥完成夹具（等待下料）
                        case EventList.DryOvenPickDryFinishPallet:
                            {
                                if(CavityStatus.Normal == GetDryingOvenCavityState(runId, ovenRow))
                                {
                                    switch(searchMode)
                                    {
                                        case ModDef.PickSameAndInvalid:
                                            {
                                                if((GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStage.Baked == plt.Stage) 
                                                    && (PalletStatus.WaitOffload == plt.State) && !plt.IsEmpty())
                                                    && (GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.Invalid == plt.State)))
                                                {
                                                    pick.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 0, searchEvent);
                                                    return true;
                                                }
                                                else if((GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStage.Baked == plt.Stage) 
                                                    && (PalletStatus.WaitOffload == plt.State) && !plt.IsEmpty())
                                                    && (GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.Invalid == plt.State)))
                                                {
                                                    pick.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 1, searchEvent);
                                                    return true;
                                                }
                                                break;
                                            }
                                        case ModDef.PickSameAndOther:
                                            {
                                                if((GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStage.Baked == plt.Stage) 
                                                    && (PalletStatus.WaitOffload == plt.State) && !plt.IsEmpty()))
                                                {
                                                    pick.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 0, searchEvent);
                                                    return true;
                                                }
                                                else if((GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStage.Baked == plt.Stage) 
                                                    && (PalletStatus.WaitOffload == plt.State) && !plt.IsEmpty()))
                                                {
                                                    pick.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 1, searchEvent);
                                                    return true;
                                                }
                                                break;
                                            }
                                    }
                                }
                                break;
                            }
                        // 干燥炉转移取夹具：取来源炉腔
                        case EventList.DryOvenPickTransferPallet:
                            {
                                break;
                            }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 搜索干燥炉放位置
        /// </summary>
        /// <param name="searchEvent">搜索事件</param>
        /// <param name="place">事件位置</param>
        /// <returns></returns>
        private bool SearchDryOvenPlacePos(RunID runId, ModDef searchMode, EventList searchEvent, ref PickPlacePos place)
        {
            int placePltIdx = -1;
            Pallet plt = new Pallet();
            int ovenID = (int)runId - (int)RunID.DryOven0;
            // 模组使能，运行状态
            if(!GetModuleEnable(runId) || !GetModuleRunning(runId))
            {
                return false;
            }
            // 信号事件状态不正确
            if((EventStatus.Require != GetModuleEvent(runId, searchEvent, ref placePltIdx)))
            {
                return false;
            }
            for(int ovenRow = 0; ovenRow < (int)OvenRowCol.MaxRow; ovenRow++)
            {
                if (GetDryingOvenCavityEnable(runId, ovenRow) && (CavityStatus.Normal == GetDryingOvenCavityState(runId, ovenRow))
                    && !GetDryingOvenCavityPressure(runId, ovenRow) && !GetDryingOvenCavityTransfer(runId, ovenRow))
                {
                    switch(searchMode)
                    {
                        case ModDef.PlaceSameAndInvalid:
                            {
                                switch(searchEvent)
                                {
                                    // 干燥炉放空夹具
                                    case EventList.DryOvenPlaceEmptyPallet:
                                        {
                                            if((GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.Invalid == plt.State))
                                                && (GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.OK == plt.State) && plt.IsEmpty()))
                                            {
                                                place.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 0, searchEvent);
                                                return true;
                                            }
                                            else if((GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.Invalid == plt.State))
                                                && (GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.OK == plt.State) && plt.IsEmpty()))
                                            {
                                                place.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 1, searchEvent);
                                                return true;
                                            }
                                            break;
                                        }
                                    // 干燥炉放NG非空夹具
                                    case EventList.DryOvenPlaceNGPallet:
                                        {
                                            if((GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.Invalid == plt.State))
                                                && (GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.NG == plt.State) && !plt.IsEmpty()))
                                            {
                                                place.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 0, searchEvent);
                                                return true;
                                            }
                                            else if((GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.Invalid == plt.State))
                                                && (GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.NG == plt.State) && !plt.IsEmpty()))
                                            {
                                                place.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 1, searchEvent);
                                                return true;
                                            }
                                            break;
                                        }
                                    // 干燥炉放NG空夹具
                                    case EventList.DryOvenPlaceNGEmptyPallet:
                                        {
                                            if((GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.Invalid == plt.State))
                                                && (GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.NG == plt.State) && plt.IsEmpty()))
                                            {
                                                place.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 0, searchEvent);
                                                return true;
                                            }
                                            else if((GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.Invalid == plt.State))
                                                && (GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.NG == plt.State) && plt.IsEmpty()))
                                            {
                                                place.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 1, searchEvent);
                                                return true;
                                            }
                                            break;
                                        }
                                    // 干燥炉放上料完成OK满夹具
                                    case EventList.DryOvenPlaceOnlOKFullPallet:
                                        {
                                            MachineCtrl mc = MachineCtrl.GetInstance();
                                            if(0 == (mc.GetDryingOvenCavityHeartCycle(runId, ovenRow) % mc.GetDryingOvenCavitySamplingCycle(runId, ovenRow)))
                                            {
                                                if((GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.Invalid == plt.State))
                                                    && (GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.OK == plt.State)
                                                    && ((PalletStage.Onload == plt.Stage) && !plt.IsEmpty()) && plt.HasFake()))
                                                {
                                                    place.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 0, searchEvent);
                                                    return true;
                                                }
                                                else if((GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.Invalid == plt.State))
                                                    && (GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.OK == plt.State)
                                                    && ((PalletStage.Onload == plt.Stage) && !plt.IsEmpty()) && plt.HasFake()))
                                                {
                                                    place.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 1, searchEvent);
                                                    return true;
                                                }
                                            }
                                            else
                                            {
                                                if((GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.Invalid == plt.State))
                                                    && (GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.OK == plt.State)
                                                    && ((PalletStage.Onload == plt.Stage) && !plt.IsEmpty())))
                                                {
                                                    place.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 0, searchEvent);
                                                    return true;
                                                }
                                                else if((GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.Invalid == plt.State))
                                                    && (GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.OK == plt.State)
                                                    && ((PalletStage.Onload == plt.Stage) && !plt.IsEmpty())))
                                                {
                                                    place.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 1, searchEvent);
                                                    return true;
                                                }
                                            }
                                            break;
                                        }
                                    // 干燥炉放上料完成OK带假电池满夹具
                                    case EventList.DryOvenPlaceOnlOKFakeFullPallet:
                                        {
                                            MachineCtrl mc = MachineCtrl.GetInstance();
                                            if (0 == (mc.GetDryingOvenCavityHeartCycle(runId, ovenRow) % mc.GetDryingOvenCavitySamplingCycle(runId, ovenRow)))
                                            {
                                                if((GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.Invalid == plt.State))
                                                    && (GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.OK == plt.State)
                                                    && ((PalletStage.Onload == plt.Stage) && !plt.IsEmpty()) && !plt.HasFake()))
                                                {
                                                    place.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 0, searchEvent);
                                                    return true;
                                                }
                                                else if((GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.Invalid == plt.State))
                                                    && (GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.OK == plt.State)
                                                    && ((PalletStage.Onload == plt.Stage) && !plt.IsEmpty()) && !plt.HasFake()))
                                                {
                                                    place.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 1, searchEvent);
                                                    return true;
                                                }
                                            }
                                            break;
                                        }
                                    // 干燥炉放回炉假电池夹具（已放回假电池的夹具）
                                    case EventList.DryOvenPlaceRebakeFakePallet:
                                    // 干燥炉放等待水含量结果夹具（已取待测假电池的夹具）
                                    case EventList.DryOvenPlaceWaitResultPallet:
                                        {
                                            // 此两种情况位置已在夹具属性中包含，不在查找
                                            break;
                                        }
                                    // 干燥炉转移放夹具：放至目的炉腔
                                    case EventList.DryOvenPlaceTransferPallet:
                                        {
                                            break;
                                        }
                                }
                                break;
                            }
                        case ModDef.PlaceInvalidAndInvalid:
                            {
                                switch(searchEvent)
                                {
                                    // 干燥炉放上料完成OK带假电池满夹具
                                    case EventList.DryOvenPlaceOnlOKFakeFullPallet:
                                        {
                                            MachineCtrl mc = MachineCtrl.GetInstance();
                                            if(0 == (mc.GetDryingOvenCavityHeartCycle(runId, ovenRow) % mc.GetDryingOvenCavitySamplingCycle(runId, ovenRow)))
                                            {
                                                if((GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.Invalid == plt.State))
                                                    && (GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.Invalid == plt.State)))
                                                {
                                                    place.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 0, searchEvent);
                                                    return true;
                                                }
                                                else if((GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.Invalid == plt.State))
                                                    && (GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.Invalid == plt.State)))
                                                {
                                                    place.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 1, searchEvent);
                                                    return true;
                                                }
                                            }
                                            break;
                                        }
                                    default:
                                        {
                                            if((GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.Invalid == plt.State))
                                                && (GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.Invalid == plt.State)))
                                            {
                                                place.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 0, searchEvent);
                                                return true;
                                            }
                                            else if((GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.Invalid == plt.State))
                                                && (GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.Invalid == plt.State)))
                                            {
                                                place.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 1, searchEvent);
                                                return true;
                                            }
                                            break;
                                        }
                                }
                                break;
                            }
                        case ModDef.PlaceInvalidAndOther:
                            {
                                switch(searchEvent)
                                {
                                    // 干燥炉放空夹具
                                    case EventList.DryOvenPlaceEmptyPallet:
                                    // 干燥炉放NG非空夹具
                                    case EventList.DryOvenPlaceNGPallet:
                                    // 干燥炉放NG空夹具
                                    case EventList.DryOvenPlaceNGEmptyPallet:
                                        {
                                            if((GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.Invalid == plt.State)))
                                            {
                                                place.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 0, searchEvent);
                                                return true;
                                            }
                                            else if((GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.Invalid == plt.State)))
                                            {
                                                place.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 1, searchEvent);
                                                return true;
                                            }
                                            break;
                                        }
                                    // 干燥炉放上料完成OK满夹具
                                    case EventList.DryOvenPlaceOnlOKFullPallet:
                                        {
                                            if((GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.Invalid == plt.State)) 
                                                && (GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && ((PalletStatus.OK != plt.State) || plt.IsEmpty())))
                                            {
                                                place.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 0, searchEvent);
                                                return true;
                                            }
                                            else if((GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.Invalid == plt.State))
                                                && (GetModulePallet(runId, (ovenRow * 2), ref plt) && ((PalletStatus.OK != plt.State) || plt.IsEmpty())))
                                            {
                                                place.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 1, searchEvent);
                                                return true;
                                            }
                                            break;
                                        }
                                    // 干燥炉放上料完成OK带假电池满夹具
                                    case EventList.DryOvenPlaceOnlOKFakeFullPallet:
                                        {
                                            MachineCtrl mc = MachineCtrl.GetInstance();
                                            if(0 == (mc.GetDryingOvenCavityHeartCycle(runId, ovenRow) % mc.GetDryingOvenCavitySamplingCycle(runId, ovenRow)))
                                            {
                                                if((GetModulePallet(runId, (ovenRow * 2), ref plt) && (PalletStatus.Invalid == plt.State))
                                                    && (GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && ((PalletStatus.OK != plt.State) || plt.IsEmpty())))
                                                {
                                                    place.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 0, searchEvent);
                                                    return true;
                                                }
                                                else if((GetModulePallet(runId, (ovenRow * 2 + 1), ref plt) && (PalletStatus.Invalid == plt.State))
                                                    && (GetModulePallet(runId, (ovenRow * 2), ref plt) && ((PalletStatus.OK != plt.State) || plt.IsEmpty())))
                                                {
                                                    place.SetData(runId, (TransferRobotStation.DryOven_0 + ovenID), ovenRow, 1, searchEvent);
                                                    return true;
                                                }
                                            }
                                            break;
                                        }
                                    // 干燥炉放回炉假电池夹具（已放回假电池的夹具）
                                    case EventList.DryOvenPlaceRebakeFakePallet:
                                    // 干燥炉放等待水含量结果夹具（已取待测假电池的夹具）
                                    case EventList.DryOvenPlaceWaitResultPallet:
                                        {
                                            // 此两种情况位置已在夹具属性中包含，不在查找
                                            break;
                                        }
                                    // 干燥炉转移夹具：目的炉腔，由调度设置
                                    case EventList.DryOvenPlaceTransferPallet:
                                        {
                                            break;
                                        }
                                }
                                break;
                            }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 搜索全局所有干燥炉
        /// </summary>
        /// <param name="searchEvent">搜索事件</param>
        /// <param name="globalsPos">全局搜索到的事件位置</param>
        /// <param name="pickSearch">true取搜索，false放搜索</param>
        /// <param name="inverseSearch">true从后向前反向搜索，false从前向后正向搜索</param>
        /// <returns></returns>
        private bool SearchGlobalsDryOven(EventList searchEvent, ref PickPlacePos globalsPos, bool pickSearch, bool inverseSearch)
        {
            if(pickSearch)
            {
                for(ModDef mode = ModDef.PickSameAndInvalid; mode < ModDef.PickEnd; mode++)
                {
                    if(inverseSearch)
                    {
                        for(RunID id = RunID.DryOvenALL - 1; id >= RunID.DryOven0; id--)
                        {
                            if (SearchDryOvenPickPos(id, mode, searchEvent, ref globalsPos))
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        for(RunID id = RunID.DryOven0; id < RunID.DryOvenALL; id++)
                        {
                            if(SearchDryOvenPickPos(id, mode, searchEvent, ref globalsPos))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            else
            {
                var ovenOrder = new Dictionary<RunID, bool>(this.placeDryOvenOrder);
                if(inverseSearch)
                {
                    ovenOrder = Enumerable.Reverse(ovenOrder).ToDictionary(p => p.Key, p => p.Value);
                }
                for(ModDef mode = ModDef.PlaceSameAndInvalid; mode < ModDef.PlaceEnd; mode++)
                {
                    foreach(var item in ovenOrder)
                    {
                        switch(searchEvent)
                        {
                            case EventList.DryOvenPlaceOnlOKFullPallet:
                            case EventList.DryOvenPlaceOnlOKFakeFullPallet:
                                if((ModDef.PlaceSameAndInvalid != mode) && item.Value)
                                {
                                    continue;
                                }
                                break;
                        }
                        if(SearchDryOvenPlacePos(item.Key, mode, searchEvent, ref globalsPos))
                        {
                            if (EventList.DryOvenPlaceOnlOKFakeFullPallet == searchEvent)
                            {
                                ResetDryOvenAction();
                            }
                            this.placeDryOvenOrder[item.Key] = true;
                            return true;
                        }
                    }
                }
                switch(searchEvent)
                {
                    case EventList.DryOvenPlaceOnlOKFullPallet:
                    case EventList.DryOvenPlaceOnlOKFakeFullPallet:
                        ResetDryOvenAction();
                        break;
                }
            }
            return false;
        }

        /// <summary>
        /// 搜索下料取夹具
        /// </summary>
        /// <param name="searchEvent"></param>
        /// <param name="pick"></param>
        /// <returns></returns>
        private bool SearchOffloadPickPos(EventList searchEvent, ref PickPlacePos pick)
        {
            RunID runId = RunID.OffloadBattery;
            int pickPltIdx = -1;
            Pallet plt = new Pallet();
            // 模组使能，运行状态
            if(!GetModuleEnable(runId) || !GetModuleRunning(runId))
            {
                return false;
            }
            // 信号事件状态不正确
            if((EventStatus.Require != GetModuleEvent(runId, searchEvent, ref pickPltIdx))
                || !(pickPltIdx > -1 && pickPltIdx < (int)ModuleMaxPallet.OffloadBattery)
                || !GetModulePallet(runId, pickPltIdx, ref plt))
            {
                return false;
            }
            switch(searchEvent)
            {
                // 下料区取空夹具
                case EventList.OffLoadPickEmptyPallet:
                    {
                        if((PalletStatus.OK == plt.State) && (plt.IsEmpty()))
                        {
                            pick.SetData(runId, TransferRobotStation.OffloadStation, 0, pickPltIdx, searchEvent);
                            return true;
                        }
                        break;
                    }
                // 下料区取等待水含量结果夹具（已取待测假电池的夹具）
                case EventList.OffLoadPickWaitResultPallet:
                    {
                        if((PalletStatus.WaitResult == plt.State) && !plt.IsEmpty())
                        {
                            pick.SetData(runId, TransferRobotStation.OffloadStation, 0, pickPltIdx, searchEvent);
                            return true;
                        }
                        break;
                    }
                // 下料区取NG夹具（非空）
                case EventList.OffLoadPickNGPallet:
                    {
                        if((PalletStatus.NG == plt.State) && !plt.IsEmpty())
                        {
                            pick.SetData(runId, TransferRobotStation.OffloadStation, 0, pickPltIdx, searchEvent);
                            return true;
                        }
                        break;
                    }
                // 下料区取NG空夹具
                case EventList.OffLoadPickNGEmptyPallet:
                    {
                        if((PalletStatus.NG == plt.State) && plt.IsEmpty())
                        {
                            pick.SetData(runId, TransferRobotStation.OffloadStation, 0, pickPltIdx, searchEvent);
                            return true;
                        }
                        break;
                    }
            }
            return false;
        }

        /// <summary>
        /// 搜索下料放夹具
        /// </summary>
        /// <param name="searchEvent"></param>
        /// <param name="place"></param>
        /// <returns></returns>
        private bool SearchOffloadPlacePos(EventList searchEvent, ref PickPlacePos place)
        {
            RunID runId = RunID.OffloadBattery;
            int placePltIdx = -1;
            Pallet plt = new Pallet();
            // 模组使能，运行状态
            if(!GetModuleEnable(runId) || !GetModuleRunning(runId))
            {
                return false;
            }
            // 信号事件状态不正确
            if(EventStatus.Require != GetModuleEvent(runId, searchEvent, ref placePltIdx))
            {
                return false;
            }
            switch(searchEvent)
            {
                // 下料区放干燥完成夹具
                case EventList.OffLoadPlaceDryFinishPallet:
                // 下料放NG非空
                case EventList.OffLoadPlaceNGPallet:
                    {
                        for(int i = (int)ModuleMaxPallet.OffloadBattery - 1; i > -1; i--)
                        {
                            if(GetModulePallet(runId, i, ref plt) && (PalletStatus.Invalid == plt.State))
                            {
                                place.SetData(runId, TransferRobotStation.OffloadStation, 0, i, searchEvent);
                                return true;
                            }
                        }
                        break;
                    }
                // 下料区放待检测含假电池夹具（未取走假电池的夹具）
                case EventList.OffLoadPlaceDetectFakePallet:
                    {
                        for(int i = 0; i < (int)ModuleMaxPallet.OffloadBattery; i++)
                        {
                            if (GetModulePallet(runId, i, ref plt) && (PalletStatus.Invalid == plt.State))
                            {
                                place.SetData(runId, TransferRobotStation.OffloadStation, 0, i, searchEvent);
                                return true;
                            }
                        }
                        break;
                    }
            }
            return false;
        }

        /// <summary>
        /// 设置干燥炉取放满夹具状态
        /// </summary>
        private void ResetDryOvenAction()
        {
            foreach(var item in this.placeDryOvenOrder)
            {
                if (item.Value)
                {
                    this.placeDryOvenOrder[item.Key] = false;
                    break;
                }
            }
        }

        #endregion

        #region // 模组信号

        /// <summary>
        /// 设置模组的信号状态
        /// </summary>
        /// <param name="modEvent"></param>
        /// <param name="eventState"></param>
        /// <returns></returns>
        private bool SetModuleEvent(RunID runId, EventList modEvent, EventStatus eventState, int eventPos)
        {
            return MachineCtrl.GetInstance().SetModuleEvent(runId, modEvent, eventState, eventPos);
        }

        /// <summary>
        /// 获取模组的信号状态
        /// </summary>
        /// <param name="modEvent"></param>
        /// <returns></returns>
        public EventStatus GetModuleEvent(RunID runId, EventList modEvent, ref int eventPos)
        {
            return MachineCtrl.GetInstance().GetModuleEvent(runId, modEvent, ref eventPos);
        }

        /// <summary>
        /// 预设置模组信号
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="modEvent"></param>
        /// <param name="eventState"></param>
        /// <param name="eventPos"></param>
        /// <returns></returns>
        private bool PresetModuleEvent(PickPlacePos pos, EventStatus eventState)
        {
            int eventPos = -1;
            // 缓存架
            if(TransferRobotStation.PalletBuffer==pos.station) {
                if(EventStatus.Require==GetModuleEvent(pos.runID , pos.stationEvent , ref eventPos)) {
                    eventPos=pos.row;//(pos.row * (int)OvenRowCol.MaxCol + pos.col);
                    return SetModuleEvent(pos.runID , pos.stationEvent , eventState , eventPos);
                }
                return false;
            }
            // 人工操作台
            else if(TransferRobotStation.ManualOperate==pos.station) {
                if(EventStatus.Require==GetModuleEvent(pos.runID , pos.stationEvent , ref eventPos)) {
                    if(eventPos==pos.col) {
                        return SetModuleEvent(pos.runID , pos.stationEvent , eventState , eventPos);
                    }
                }
                return false;
            }
            // 干燥炉
            else if((TransferRobotStation.DryOven_0<=pos.station)&&(pos.station<=TransferRobotStation.DryOven_All)) {
                if(EventStatus.Require==GetModuleEvent(pos.runID , pos.stationEvent , ref eventPos)) {
                    eventPos=(pos.row*(int)OvenRowCol.MaxCol+pos.col);
                    return SetModuleEvent(pos.runID , pos.stationEvent , eventState , eventPos);
                }
                switch(pos.stationEvent) {
                    case EventList.DryOvenPickTransferPallet:
                    case EventList.DryOvenPlaceTransferPallet: {
                        if(EventStatus.Start==GetModuleEvent(pos.runID , pos.stationEvent , ref eventPos)) {
                            return true;
                        }
                        break;
                    }
                }
                return false;
            } 
            ////上料工位 取NC空托盘 2022.04.03 增加
            //else if(TransferRobotStation.OnloadStation==pos.station) {
            //    if(EventStatus.Require==GetModuleEvent(pos.runID , pos.stationEvent , ref eventPos)) {
            //       return SetModuleEvent(pos.runID , pos.stationEvent , eventState , eventPos);
            //    }
            //    return false;
            //}

            return true;
        }

        /// <summary>
        /// 预设置模组信号
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="modEvent"></param>
        /// <param name="eventState"></param>
        /// <param name="eventPos"></param>
        /// <returns></returns>
        private bool WaitModuleEventPlaceReady(PickPlacePos pos)
        {
            int eventPos = -1;
            // 干燥炉
            if((TransferRobotStation.DryOven_0 <= pos.station) && (pos.station <= TransferRobotStation.DryOven_All))
            {
                if(EventStatus.Ready == GetModuleEvent(pos.runID, pos.stationEvent, ref eventPos))
                {
                    return true;
                }
                return false;
            }
            return true;
        }
        #endregion

        #region // 模组状态

        /// <summary>
        /// 获取模组使能状态
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        private bool GetModuleEnable(RunID runId)
        {
            return MachineCtrl.GetInstance().GetModuleEnable(runId);
        }

        /// <summary>
        /// 获取模组运行状态
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        private bool GetModuleRunning(RunID runId)
        {
            return MachineCtrl.GetInstance().GetModuleRunning(runId);
        }

        /// <summary>
        /// 获取干燥炉腔体干燥状态
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        private CavityStatus GetDryingOvenCavityState(RunID runId, int cavityIdx)
        {
            return MachineCtrl.GetInstance().GetDryingOvenCavityState(runId, cavityIdx);
        }

        /// <summary>
        /// 获取干燥炉炉门状态：enum OvenStatus中炉门的枚举
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="cavityIdx">腔体索引</param>
        /// <returns></returns>
        private short GetDryingOvenDoorOpenState(RunID runId, int cavityIdx)
        {
            // 本地
            RunProcessDryingOven run = MachineCtrl.GetInstance().GetModule(runId) as RunProcessDryingOven;
            if(null != run)
            {
                return run.RCavity(cavityIdx).doorState;
            }
            // 网络
            else
            {
            }
            return (short)OvenStatus.Unknown;
        }

        /// <summary>
        /// 获取干燥炉炉门安全光幕状态：enum OvenStatus中安全光幕的枚举
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="cavityIdx">腔体索引</param>
        /// <returns></returns>
        private short GetDryingOvenSafetyCurtain(RunID runId, int cavityIdx)
        {
            // 本地
            RunProcessDryingOven run = MachineCtrl.GetInstance().GetModule(runId) as RunProcessDryingOven;
            if(null != run)
            {
                return run.RCavity(cavityIdx).safetyCurtain;
            }
            // 网络
            else
            {
            }
            return (short)OvenStatus.Unknown;
        }

        /// <summary>
        /// 获取干燥炉腔体使能状态
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="cavityIdx">腔体索引</param>
        /// <returns></returns>
        private bool GetDryingOvenCavityEnable(RunID runId, int cavityIdx)
        {
            return MachineCtrl.GetInstance().GetDryingOvenCavityEnable(runId, cavityIdx);
        }

        /// <summary>
        /// 获取干燥炉腔体保压状态
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="cavityIdx">腔体索引</param>
        /// <returns></returns>
        private bool GetDryingOvenCavityPressure(RunID runId, int cavityIdx)
        {
            return MachineCtrl.GetInstance().GetDryingOvenCavityPressure(runId, cavityIdx);
        }

        /// <summary>
        /// 获取干燥炉腔体转移状态
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="cavityIdx">腔体索引</param>
        /// <returns></returns>
        private bool GetDryingOvenCavityTransfer(RunID runId, int cavityIdx)
        {
            return MachineCtrl.GetInstance().GetDryingOvenCavityTransfer(runId, cavityIdx);
        }

        /// <summary>
        /// 获取缓存架层使能状态
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="pltIdx">夹具位置索引</param>
        /// <returns></returns>
        private bool GetPalletBufferRowEnable(RunID runId, int cavityIdx)
        {
            return MachineCtrl.GetInstance().GetPalletBufferRowEnable(runId, cavityIdx);
        }

        /// <summary>
        /// 获取上下料夹具位使能状态
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="pltIdx">夹具位置索引</param>
        /// <returns></returns>
        private bool GetPalletPosEnable(RunID runId, int pltIdx)
        {
            return MachineCtrl.GetInstance().GetPalletPosEnable(runId, pltIdx);
        }

        #endregion

        #region // 模组数据

        /// <summary>
        /// 获取模组夹具数据
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="pltIdx"></param>
        /// <param name="pallet"></param>
        /// <returns></returns>
        private bool GetModulePallet(RunID runId, int pltIdx, ref Pallet pallet)
        {
            Pallet[] plt = MachineCtrl.GetInstance().GetModulePallet(runId);
            if ((null != plt) && (plt.Length > pltIdx))
            {
                pallet.Copy(plt[pltIdx]);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 设置模组夹具数据
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="pltIdx"></param>
        /// <param name="pallet"></param>
        /// <returns></returns>
        private bool SetModulePallet(RunID runId, int pltIdx, Pallet pallet)
        {
            return MachineCtrl.GetInstance().SetModulePallet(runId, pltIdx, pallet);
        }

        /// <summary>
        /// 获取干燥炉中空位数量
        /// </summary>
        /// <returns></returns>
        private int GetDryOvenEmptyPosCount()
        {
            int emptyPosCount = 0;
            Pallet plt = new Pallet();
            for (RunID id = RunID.DryOven0; id < RunID.DryOvenALL; id++)
            {
                // 模组非使能 || 模组非运行中
                if(!GetModuleEnable(id) || !GetModuleRunning(id))
                {
                    continue;
                }
                for (int pltIdx = 0; pltIdx < (int)ModuleMaxPallet.DryingOven; pltIdx++)
                {
                    // 腔体状态正常 && 腔体使能
                    if((CavityStatus.Normal == GetDryingOvenCavityState(id, pltIdx/2)) && GetDryingOvenCavityEnable(id, pltIdx/2) 
                        && GetModulePallet(id, pltIdx, ref plt) && (PalletStatus.Invalid == plt.State))
                    {
                        emptyPosCount++;
                    }
                }
            }
            return emptyPosCount;
        }

        #endregion

        #region // 运行数据读写

        public override void InitRunData()
        {
            this.pickPos.Release();
            this.placePos.Release();
            if (null == this.robotCmd)
            {
                this.robotCmd = new int[(int)RobotCmdFormat.End];
            }
            if(null == this.robotAutoAction)
            {
                this.robotAutoAction = new RobotActionInfo();
            }
            //this.robotAutoAction.Release();
            if(null == this.robotDebugAction)
            {
                this.robotDebugAction = new RobotActionInfo();
            }
            //this.robotDebugAction.Release();
            if (null == this.robotClient)
            {
                this.robotClient = new RobotClient();
            }
            if(null == testRbtStation)
            {
                this.testRbtStation = new Dictionary<string, RobotActionInfo>();
            }
            this.testRbtStation.Clear();

            base.InitRunData();
        }

        public override void LoadRunData()
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);

            key = string.Format("pickPos.runID");
            this.pickPos.runID = (RunID)iniStream.ReadInt(section, key, (int)this.pickPos.runID);
            key = string.Format("pickPos.station");
            this.pickPos.station = (TransferRobotStation)iniStream.ReadInt(section, key, (int)this.pickPos.station);
            key = string.Format("pickPos.row");
            this.pickPos.row = iniStream.ReadInt(section, key, this.pickPos.row);
            key = string.Format("pickPos.col");
            this.pickPos.col = iniStream.ReadInt(section, key, this.pickPos.col);
            key = string.Format("pickPos.stationEvent");
            this.pickPos.stationEvent = (EventList)iniStream.ReadInt(section, key, (int)this.pickPos.stationEvent);

            key = string.Format("placePos.runID");
            this.placePos.runID = (RunID)iniStream.ReadInt(section, key, (int)this.placePos.runID);
            key = string.Format("placePos.station");
            this.placePos.station = (TransferRobotStation)iniStream.ReadInt(section, key, (int)this.placePos.station);
            key = string.Format("placePos.row");
            this.placePos.row = iniStream.ReadInt(section, key, this.placePos.row);
            key = string.Format("placePos.col");
            this.placePos.col = iniStream.ReadInt(section, key, this.placePos.col);
            key = string.Format("placePos.stationEvent");
            this.placePos.stationEvent = (EventList)iniStream.ReadInt(section, key, (int)this.placePos.stationEvent);

            LoadRobotRunData();

            base.LoadRunData();
        }

        public override void SaveRunData(SaveType saveType, int index = -1)
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);

            if(SaveType.Variables == (SaveType.Variables & saveType))
            {
                string[] posName = new string[] { "pickPos", "placePos" };
                PickPlacePos[] pos = new PickPlacePos[] { pickPos, placePos };
                for(int i = 0; i < pos.Length; i++)
                {
                    key = string.Format("{0}.runID", posName[i]);
                    iniStream.WriteInt(section, key, (int)pos[i].runID);
                    key = string.Format("{0}.station", posName[i]);
                    iniStream.WriteInt(section, key, (int)pos[i].station);
                    key = string.Format("{0}.row", posName[i]);
                    iniStream.WriteInt(section, key, pos[i].row);
                    key = string.Format("{0}.col", posName[i]);
                    iniStream.WriteInt(section, key, pos[i].col);
                    key = string.Format("{0}.stationEvent", posName[i]);
                    iniStream.WriteInt(section, key, (int)pos[i].stationEvent);
                }
            }
            if(SaveType.Robot == (SaveType.Robot & saveType))
            {
                iniStream.WriteInt(section, "robotOilCount", this.robotOilCount);
                for(int i = 0; i < this.robotCmd.Length; i++)
                {
                    key = string.Format("robotCmd[{0}]", i);
                    iniStream.WriteInt(section, key, this.robotCmd[i]);
                }
                string[] rbtActionName = new string[] { "robotAutoAction", "robotDebugAction" };
                RobotActionInfo[] rbtAction = new RobotActionInfo[] { robotAutoAction, robotDebugAction };
                for(int i = 0; i < rbtAction.Length; i++)
                {
                    key = string.Format("{0}.station", rbtActionName[i]);
                    iniStream.WriteInt(section, key, rbtAction[i].station);
                    key = string.Format("{0}.row", rbtActionName[i]);
                    iniStream.WriteInt(section, key, rbtAction[i].row);
                    key = string.Format("{0}.col", rbtActionName[i]);
                    iniStream.WriteInt(section, key, rbtAction[i].col);
                    key = string.Format("{0}.order", rbtActionName[i]);
                    iniStream.WriteInt(section, key, (int)rbtAction[i].order);
                    key = string.Format("{0}.stationName", rbtActionName[i]);
                    iniStream.WriteString(section, key, rbtAction[i].stationName);
                }
            }
            base.SaveRunData(saveType, index);
        }

        private void LoadRobotRunData()
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);

            this.robotOilCount = iniStream.ReadInt(section, "robotOilCount", robotOilCount);

            for(int i = 0; i < this.robotCmd.Length; i++)
            {
                key = string.Format("robotCmd[{0}]", i);
                this.robotCmd[i] = iniStream.ReadInt(section, key, this.robotCmd[i]);
            }

            key = "robotAutoAction.station";
            this.robotAutoAction.station = iniStream.ReadInt(section, key, robotAutoAction.station);
            key = "robotAutoAction.row";
            this.robotAutoAction.row = iniStream.ReadInt(section, key, robotAutoAction.row);
            key = "robotAutoAction.col";
            this.robotAutoAction.col = iniStream.ReadInt(section, key, robotAutoAction.col);
            key = "robotAutoAction.order";
            this.robotAutoAction.order = (RobotOrder)iniStream.ReadInt(section, key, (int)robotAutoAction.order);
            key = "robotAutoAction.stationName";
            this.robotAutoAction.stationName = iniStream.ReadString(section, key, robotAutoAction.stationName);

            key = "robotDebugAction.station";
            this.robotDebugAction.station = iniStream.ReadInt(section, key, robotDebugAction.station);
            key = "robotDebugAction.row";
            this.robotDebugAction.row = iniStream.ReadInt(section, key, robotDebugAction.row);
            key = "robotDebugAction.col";
            this.robotDebugAction.col = iniStream.ReadInt(section, key, robotDebugAction.col);
            key = "robotDebugAction.order";
            this.robotDebugAction.order = (RobotOrder)iniStream.ReadInt(section, key, (int)robotDebugAction.order);
            key = "robotDebugAction.stationName";
            this.robotDebugAction.stationName = iniStream.ReadString(section, key, robotDebugAction.stationName);
        }
        #endregion

        #region // 防呆检查

        /// <summary>
        /// 模组防呆监视：请勿加入耗时过长或阻塞操作
        /// </summary>
        public override void MonitorAvoidDie()
        {
            if (this.RobotRunning || !InputState(IRobotRunning, false))
            {
                if (MachineCtrl.GetInstance().SafeDoorState && MachineCtrl.GetInstance().ClientIsConnect())
                {
                    // 只急停移动动作
                    if(RobotOrder.MOVE == robotDebugAction.order)
                    {
                        OutputAction(ORobotEStop, true);
                        ShowMessageID((int)MsgID.SafeDoorOpenRbtEStop, "安全门打开，机器人急停！", "请关闭安全门后再操作机器人", MessageType.MsgAlarm);
                    }
                }
            }
        }

        #endregion

        #region // 数据保存

        /// <summary>
        /// 保存取放夹具数据
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="col"></param>
        /// <param name="plt"></param>
        private void SavePickPlacePalletData(PickPlacePos pos, bool pickPlt, Pallet plt)
        {
            string file, title, text, tmpTitle, tmpText;
            file = string.Format(@"{0}\调度取放夹具数据\{1}\取放夹具{1}.csv"
                    , MachineCtrl.GetInstance().ProductionFilePath, DateTime.Now.ToString("yyyy-MM-dd"));
            title = "日期,时间,工位,取或放,行/层,列,夹具状态,夹具条码,电池数量,夹具中电池";
            text = string.Format("{0},{1},{2},{3},{4},{5},{6}", DateTime.Now.ToString("yyyy-MM-dd,HH:mm:ss")
                , GetRobotStationName(pos.station), (pickPlt ? "取" : "放"), (pos.row + 1), (pos.col + 1), plt.State, plt.Code);
            tmpTitle = tmpText = "";
            int batCount = 0;
            for(int batCol = 0; batCol < plt.MaxCol; batCol++)
            {
                tmpText += string.Format("\r\n,,,,,,,,,{0}列", (batCol + 1));
                for(int batRow = 0; batRow < plt.MaxRow; batRow++)
                {
                    if(0 == batCol)
                    {
                        tmpTitle += string.Format(",{0}行", (batRow + 1));
                    }
                    if(BatteryStatus.Fake == plt.Battery[batRow, batCol].Type)
                    {
                        batCount++;
                        tmpText += string.Format(",[{0}]{1}", plt.Battery[batRow, batCol].Type, plt.Battery[batRow, batCol].Code);
                    }
                    else if(BatteryStatus.Invalid != plt.Battery[batRow, batCol].Type)
                    {
                        batCount++;
                        tmpText += string.Format(",{0}", plt.Battery[batRow, batCol].Code);
                    }
                    else
                    {
                        tmpText += ",";
                    }
                }
            }
            title += tmpTitle;
            text += string.Format(",{0}{1}", batCount, (batCount > 0 ? tmpText : ""));
            Def.ExportCsvFile(file, title, (text + "\r\n"));
        }

        #endregion

        #region // 添加删除夹具

        public override void ManualAddPallet(int pltIdx, int maxRow, int maxCol, PalletStatus pltState, BatteryStatus batState)
        {
            ShowMsgBox.ShowDialog($"{this.RunName}模组不能添加夹具！", MessageType.MsgWarning);
        }

        public override void ManualClearPallet(int pltIdx)
        {
            if(AutoSteps.Auto_WaitWorkStart == (AutoSteps)this.nextAutoStep)
            {
                this.Pallet[pltIdx].Release();
                SaveRunData(SaveType.Pallet, pltIdx);
            }
            else
            {
                ShowMsgBox.ShowDialog("仅在等待开始信号步骤才能清除夹具", MessageType.MsgWarning);
            }
        }
        #endregion

        #region // 模组信号重置

        /// <summary>
        /// 模组信号重置
        /// </summary>
        public override void ResetModuleEvent()
        {
            if (this.robotDebugAction.order != RobotOrder.MOVE)
            {
                ShowMsgBox.ShowDialog($"模组信号重置前先将{this.RunName}移动到【移动】位置", MessageType.MsgWarning);
                return;
            }
            this.robotAutoAction.order = this.robotDebugAction.order;
            this.nextAutoStep = AutoSteps.Auto_WaitWorkStart;
            SaveRunData(SaveType.AutoStep | SaveType.Robot);
        }
        #endregion

    }
}

