using HelperLibrary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;
using log4net;
using System.Text.RegularExpressions;

namespace Machine
{
    /// <summary>
    /// 上料机器人
    /// </summary>
    class RunProcessOnloadRobot : RunProcess
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        #region // 枚举：步骤，模组数据，报警列表

        protected new enum InitSteps
        {
            Init_DataRecover = 0,

            Init_CheckFinger,
            Init_CheckPallet,
            Init_RobotConnect,
            Init_RobotHome,
            Init_MotorHome,
            Init_ScannerConnect,

            Init_End,
        }
        
        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,

            // 避让大机器人
            Auto_RobotMoveAvoidPos,
            Auto_WaitActionFinish,

            // 取：NG夹具转盘，取待测假电池
            Auto_CalcPalletPickPos,
            Auto_PalletPickPosSetEvent,
            Auto_PalletPickPosPickMove,
            Auto_PalletPickPosPickDown,
            Auto_PalletPickPosFingerAction,
            Auto_PalletPickPosPickUp,
            Auto_PalletPickPosCheckFinger,

            // 取：来料线
            Auto_CalcOnlinePickPos,
            Auto_OnlinePosSetEvent,
            Auto_OnlinePosPickMove,
            Auto_OnlinePosPickDown,
            Auto_OnlinePosFingerAction,
            Auto_OnlinePosPickUp,
            Auto_OnlinePosCheckFinger,

            // 取：假电池线
            Auto_CalcFakePickPos,
            Auto_FakePosSetEvent,
            Auto_FakeScanPosMove,
            Auto_FakeScanPosDown,
            Auto_FakeScanPosScanCode,
            Auto_FakeScanPosUp,
            Auto_FakePosPickMove,
            Auto_FakePosPickDown,
            Auto_FakePosFingerAction,
            Auto_FakePosPickUp,
            Auto_FakePosCheckFinger,

            // 夹具扫码
            Auto_PalletScanCodeMove,
            Auto_PalletScanCodeDown,
            Auto_PalletScanCodeAction,
            Auto_PalletScanCodeUp,

            // 暂存：可取可防，主要看抓手操作
            Auto_CalcBufferPos,
            Auto_BufferPosSetEvent,
            Auto_BufferPosMove,
            Auto_BufferPosDown,
            Auto_BufferPosFingerAction,
            Auto_BufferPosUp,
            Auto_BufferPosCheckFinger,

            // 计算放位置
            Auto_CalcPlacePos,

            // 放：夹具
            Auto_CalcPalletPlacePos,
            Auto_PalletPlacePosSetEvent,
            Auto_PalletPlacePosPlaceMove,
            Auto_PalletPlacePosPlaceDown,
            Auto_PalletPlacePosFingerAction,
            Auto_PalletPlacePosPlaceUp,
            Auto_PalletPlacePosCheckFinger,
            Auto_MesUpdataCount,

            // 放：NG线
            Auto_CalcNGLinePlacePos,
            Auto_NGLinePosSetEvent,
            Auto_NGLinePosPlaceMove,
            Auto_NGLinePosPlaceDown,
            Auto_NGLinePosPlaceAction,
            Auto_NGLinePosPlaceUp,
            Auto_NGLinePosCheckFinger,

            Auto_WorkEnd,
        }

        private enum ModDef
        {
            Finger_0 = 0,
            Finger_1,
            Finger_ALL,
            Buffer_0 = Finger_ALL,
            Buffer_1,
            Buffer_ALL,
            Finger_Buffer_ALL = Buffer_ALL,

        }

        private enum MsgID
        {
            Start = ModuleMsgID.OnloadRobotMsgStartID,
            RbtDelayEStop,
            SafeDoorOpenRbtEStop,
            RbtActionChange,
            SendRbtMoveCmd,
            RbtMoveCmdError,
            RbtMoveTimeout,
            BufStationDownAlm,
            ScanCodeFail,
            ScanCodeTimeout,
            CodeLenError,
            CodeTypeError,
            CheckPallet,
            BindPallet,
            UnbindPallet,
            WaitFakeBatTimeout,
            RecvCrash,
            NGPlaceFull,
        }

        #endregion

        #region // 取放位置结构体

        private struct PickPlacePos
        {
            #region // 字段
            public OnloadRobotStation station;
            public int row;
            public int col;
            public ModDef finger;
            public bool fingerClose;
            public MotorPosition motorPos;
            #endregion

            #region // 方法

            public void SetData(OnloadRobotStation curStation, int curRow, int curCol, ModDef curFinger, bool figClose, MotorPosition curMotorPos)
            {
              //  Release();
                this.station = curStation;
                this.row = curRow;
                this.col = curCol;
                this.finger = curFinger;
                this.fingerClose = figClose;
                this.motorPos = curMotorPos;
            }

            public void Release()
            {
                this.station = OnloadRobotStation.InvalidStatioin;
                this.row = -1;
                this.col = -1;
                this.finger = ModDef.Finger_ALL;
                this.fingerClose = false;
                this.motorPos = MotorPosition.Invalid;
            }
            #endregion
        };
        #endregion

        #region // 字段，属性

        #region // IO

        private int[] IPalletKeepFlatLeft;      // 夹具放平检测左：配置取反+逻辑取反
        private int[] IPalletKeepFlatRight;     // 夹具放平检测右：配置取反+逻辑取反
        private int[] IPalletHasCheck;          // 夹具位有夹具检测
        private int[] IPalletInposCheck;        // 夹具到位检测
        private int[] IFingerOpen;              // 抓手打开到位
        private int[] IFingerClose;             // 抓手关闭到位
        private int[] IFingerCheck;             // 抓手有料检查
        private int[] IBufferCheck;             // 暂存有料检查
        private int IFingerDelay;               // 抓手碰撞防呆检测
        private int IRobotRunning;              // 机器人运行中输入

        private int[] OPalletAlarm;             // 夹具位报警
        private int[] OFingerOpen;              // 抓手打开
        private int[] OFingerClose;             // 抓手关闭
        private int ORobotEStop;                // 机器人急停输出

        #endregion

        #region // 电机

        private int MotorU;         // 调宽电机
        #endregion

        #region // ModuleEx.cfg配置

        public RobotIndexID RobotID { get; private set; }   // 机器人ID

        #endregion

        #region // 模组参数

        public int RobotLowSpeed { get; private set; }          // 机器人低速速度：1-80，用以手动调试
        public bool[] PalletPosEnable { get; private set; }     // 夹具位使能：TRUE启用，FALSE禁用

        private int placeFakeRow;           // 夹具放假电池行：机构干涉，暂仅支持第一行位置
        private int placeFakeCol;		    // 夹具放假电池列
        private bool firstFakePlt;          // 优先上假电池夹具：TRUE优先假电池夹具，FALSE优先正常夹具
        private bool placeFakePltMode;      // 上假电池夹具模式：TRUE启用，FALSE禁用
        private bool placeNomalPltMode;     // 上正常夹具模式：TRUE启用，FALSE禁用
        private bool detectFakeBat;         // 上料测试待测假电池：TRUE启用，FALSE禁用
        private bool placeNGPallet;         // 上料放NG夹具转盘NG电池：TRUE启用，FALSE禁用
        private bool tranNGPalletLimit;     // 上料NG夹具转盘限制：TRUE启用，FALSE禁用

        private bool robotEnable;           // 机器人使能：TRUE启用，FALSE禁用
        private int robotSpeed;             // 机器人速度：1-100
        private int robotDelay;             // 机器人防呆时间(s)
        private string robotIP;             // 机器人IP
        private int robotPort;              // 机器人IP的Port
        private bool scanEnable;            // 扫码使能：TRUE启用，FALSE禁用
        private string scanCmd;             // 扫码器的扫码指令
        private bool scanLinefeed;          // 扫码器的扫码结束符
        private string barcodeScanIP;       // 扫码器的IP：进行网口通讯则填，否则为空
        private int barcodeScanCom;         // 扫码器的COM口：进行串口通讯则填，否则为-1
        private int barcodeScanPort;        // 扫码器的Port
        private int codeLength;             // 条码长度：-1则不检查
        private string codeType;            // 条码类别：空则不检查，多种类别以英文逗号(,)分隔
        private string scanNGType;          // 扫码NG字符：空则不检查
        private int scanMaxCount;           // 最大扫码次数：（X≥1）
        private bool scanPalletEnable;      // 扫夹具条码使能：TRUE启用，FALSE禁用
        private bool scanFakeBatEnable;     // 扫假电池条码使能：TRUE启用，FALSE禁用
        private int waitFakeDelay;          // 等待假电池防呆时间(s)

        #endregion

        #region // 模组数据

        // 配置关联模组
        RunProcessOnloadLine pickBatRun;        // 取电池模组：PickBatRun = 
        RunProcessOnloadFake pickFakeRun;       // 取假电池模组：PickFakeRun = 
        RunProcessOnloadNG placeNGRun;          // 放NG电池模组：PlaceNGRun = 

        public bool RobotRunning{ get; private set; }      // 机器人运行中

        private int placePallet;                      // 放夹具索引
        private PickPlacePos pickPos;                 // 取位置
        private PickPlacePos placePos;                // 放位置
        private int[] robotCmd;                       // 机器人指令
        private EventList avoidEvent;                 // 避让大机器人事件
        private RobotActionInfo robotAutoAction;      // 机器人自动动作信息
        private RobotActionInfo robotDebugAction;     // 机器人手动调试动作信息
        private RobotClient robotClient;              // 机器人通讯
        private BarcodeScan barcodeScan;              // 扫码器
        private string[] codeTypeArray;               // 条码类型列表
        private bool robotNeedEStop;                  // 机器人可以触发急停ON
        private Dictionary<OnloadRobotStation, RobotFormula> robotStationInfo;  // 机器人工位信息
        private DateTime stepDelayTime;               // 步骤防呆计时

        #endregion

        #endregion

        public RunProcessOnloadRobot(int runId) : base(runId)
        {
            InitBatteryPalletSize((int)ModDef.Finger_Buffer_ALL, (int)ModuleMaxPallet.OnloadRobot);

            PowerUpRestart();

            InitParameter();
            // 参数
            InsertVoidParameter("detectFakeBat", "测试假电池", "上料测试待测水含量电池：TRUE启用，FALSE禁用", detectFakeBat, RecordType.RECORD_BOOL, ParameterLevel.PL_IDLE_ADMIN);
            InsertVoidParameter("placeNGPallet", "NG夹具转盘", "上料从NG夹具中转移OK电池至OK夹具：TRUE启用，FALSE禁用", placeNGPallet, RecordType.RECORD_BOOL, ParameterLevel.PL_IDLE_ADMIN);
            InsertVoidParameter("tranNGPalletLimit", "NG夹具转盘限制", "上料NG夹具转盘限制：TRUE限制只能同类型夹具转盘，FALSE不限制转盘夹具类型即只要有空夹具便转盘", tranNGPalletLimit, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("firstFakePlt", "优先上假电池夹具", "全检模式下优先上假电池夹具：TRUE优先假电池夹具，FALSE优先正常夹具", firstFakePlt, RecordType.RECORD_BOOL);
            //InsertVoidParameter("placeFakeRow", "夹具放假电池行", "夹具放假电池行：机构干涉，暂仅支持第一行位置", placeFakeRow, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("placeFakeCol", "夹具放假电池列", "夹具放假电池列", placeFakeCol, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertVoidParameter("placeFakePltMode", "上假电池夹具", "上假电池夹具模式：TRUE启用，FALSE禁用", placeFakePltMode, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            //InsertVoidParameter("placeNomalPltMode", "上正常夹具", "上正常夹具模式：TRUE启用，FALSE禁用", placeNomalPltMode, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("robotEnable", "机器人使能", "机器人使能：TRUE启用，FALSE禁用", robotEnable, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("robotSpeed", "机器人速度", "机器人速度：1-100", robotSpeed, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("RobotLowSpeed", "机器人调试速度", "机器人手动调试速度：1-100", RobotLowSpeed, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("robotDelay", "机器人防呆", "机器人防呆时间(s)", robotDelay, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("robotIP", "机器人IP", "机器人IP", robotIP, RecordType.RECORD_STRING, ParameterLevel.PL_STOP_ADMIN);
            InsertVoidParameter("robotPort", "机器人端口", "机器人IP的Port", robotPort, RecordType.RECORD_INT, ParameterLevel.PL_STOP_ADMIN);
            for(int i = 0; i < (int)ModuleMaxPallet.OnloadRobot; i++)
            {
                InsertVoidParameter(("PalletPosEnable" + i), ("夹具位" + (i + 1) + "使能"), "夹具位使能：TRUE启用，FALSE禁用", PalletPosEnable[i], RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_OPER);
            }
            InsertVoidParameter("scanEnable", "扫码器使能", "扫码器使能：TRUE启用，FALSE禁用", scanEnable, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("scanPalletEnable", "扫夹具条码", "扫夹具条码使能：TRUE启用，FALSE禁用", scanPalletEnable, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("scanFakeBatEnable", "扫假电池条码", "扫假电池条码使能：TRUE启用，FALSE禁用", scanFakeBatEnable, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_OPER);
            InsertVoidParameter("scanMaxCount", "最大扫码次数", "最大扫码次数：（X≥1）", scanMaxCount, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("scanCmd", "扫码指令", "触发扫码的指令", scanCmd, RecordType.RECORD_STRING, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("scanLinefeed", "扫码结束符", "扫码器的扫码结束符：true有回车换行结束符，false无结束符", scanLinefeed, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("barcodeScanIP", "扫码器的IP", "扫码器的IP：进行网口通讯则填，否则为空", barcodeScanIP, RecordType.RECORD_STRING, ParameterLevel.PL_STOP_MAIN);
            //InsertVoidParameter("barcodeScanCom", "扫码器的COM口", "扫码器的COM口：进行串口通讯则填，否则为-1", barcodeScanCom, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("barcodeScanPort", "扫码器的Port", "扫码器的端口号/波特率", barcodeScanPort, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("codeLength", "条码长度", "条码长度：-1则不检查", codeLength, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("codeType", "条码类别", "条码类别：空则不检查，多种类别以英文逗号(,)分隔", codeType, RecordType.RECORD_STRING, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("scanNGType", "扫码NG字符", "扫码NG时扫码器反馈字符：空则不检查", scanNGType, RecordType.RECORD_STRING, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("waitFakeDelay", "假电池防呆", "等待假电池防呆时间(s)，超时则报警提示", waitFakeDelay, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);

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

            log.Debug("InitOperation");

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
                        CurMsgStr("检查抓手暂存感应器", "Check finger and buffer sensor");

                        for(ModDef i = ModDef.Finger_0; i < ModDef.Finger_ALL; i++)
                        {
                            if(!FingerCheck(i, (FingerBat(i).Type > BatteryStatus.Invalid), true))
                            {
                                return;
                            }
                        }
                        for(ModDef i = ModDef.Buffer_0; i < ModDef.Buffer_ALL; i++)
                        {
                            if (!BufferCheck(i, (BufferBat(i).Type > BatteryStatus.Invalid), true))
                            {
                                return;
                            }
                        }
                        this.nextInitStep = InitSteps.Init_CheckPallet;
                        break;
                    }
                case InitSteps.Init_CheckPallet:
                    {
                        CurMsgStr("检查夹具感应器", "Check pallet sensor");

                        for(int i = 0; i < (int)ModuleMaxPallet.OnloadRobot; i++)
                        {
                            if (!PalletKeepFlat(i, (this.Pallet[i].State > PalletStatus.Invalid), true))
                            {
                                return;
                            }
                        }
                        this.nextInitStep = InitSteps.Init_RobotConnect;
                        break;
                    }
                case InitSteps.Init_RobotConnect:
                    {
                        CurMsgStr("连接机器人", "Connect robot");

                        if (RobotConnect(true))
                        {
                            this.nextInitStep = InitSteps.Init_RobotHome;
                        }
                        break;
                    }
                case InitSteps.Init_RobotHome:
                    {
                        CurMsgStr("机器人回零", "Robot home");

                        if (RobotHome())
                        {
                            this.nextInitStep = InitSteps.Init_MotorHome;
                        }
                        break;
                    }
                case InitSteps.Init_MotorHome:
                    {
                        CurMsgStr("电机回零", "Motor home");
                        if (MotorHome(this.MotorU))
                        {
                            this.nextInitStep = InitSteps.Init_ScannerConnect;
                        }
                        break;
                    }
                case InitSteps.Init_ScannerConnect:
                    {
                        CurMsgStr("连接扫码枪", "Connect scanner");
                        if (ScanConnect(true))
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

            if (Def.IsNoHardware())
            {
                Sleep(50);
            }

            #region // 自动步骤开始前的检查步骤

            switch((CheckSteps)this.AutoCheckStep)
            {
                case CheckSteps.Check_WorkStart:
                    {
                        CurMsgStr("检查机器人位置", "Check robot pos");
                        if(!CheckRobotPos(robotAutoAction, robotDebugAction))
                        {
                            string msg, disp;
                            msg = string.Format("机器人动作位置被改变");
                            disp = string.Format("请在【机器人调试】界面将 {0} 移动到\r\n<{1}-{2}行-{3}列-{4}>\r\n位置，重新停止-复位-启动！"
                                , RobotDef.RobotIDName[(int)this.RobotID], this.robotAutoAction.stationName
                                , this.robotAutoAction.row + 1, this.robotAutoAction.col + 1, RobotDef.RobotOrderName[(int)this.robotAutoAction.order]);
                            ShowMessageBox((int)MsgID.RbtActionChange, msg, disp, MessageType.MsgWarning);
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

                        bool onloadClear = MachineCtrl.GetInstance().OnloadClear;
                        bool result = false;
                        string msg = "";
                        #region // 设置检查取放请求
                        for (int idx = 0; idx < this.Pallet.Length; idx++)
                        {
                            // 夹具上料完成
                            if ((PalletStatus.OK == this.Pallet[idx].State)
                                && (PalletStage.Onload != this.Pallet[idx].Stage)
                                && ((onloadClear && !this.Pallet[idx].IsEmpty()
                                && (BufferCount() < 1)) || this.Pallet[idx].IsFull())
                                && this.PalletPosEnable[idx])
                            {
                                this.placePallet = -1;
                                //MES超时重传三次
                                for (int i = 0; i < (MesData.mesFrequency == 0 ? 3 : MesData.mesFrequency); i++)
                                {
                                    //MES绑盘上传
                                    if (!MesBindPalletInfo(this.Pallet[idx], ref msg))
                                    {
                                        //获取报警字符串是否包含"超时"二字,如果没有则跳出循环
                                        if (!msg.Contains("超时"))
                                        {
                                            result = false;
                                            break;
                                        }
                                        if (i == 2)
                                        {
                                            result = false;
                                            ShowMsgBox.ShowDialog($"MES绑盘上传接口超时失败3次，请检查MES连接状态", MessageType.MsgAlarm);
                                        }
                                    }
                                    else
                                    {
                                        result = true;
                                        break;
                                    }
                                }


                                // 一次上传绑盘，需去掉放料时绑盘
                                //if (MesBindPalletInfo(this.Pallet[idx]))
                                if (result)
                                {
                                    this.Pallet[idx].Stage = PalletStage.Onload;
                                    SaveRunData(SaveType.Variables | SaveType.Pallet, idx);
                                }
                            }

                            EventList modEvent = EventList.Invalid;
                            EventStatus state = EventStatus.Invalid;
                            // 有空位 -》 请求放
                            if (PalletStatus.Invalid == this.Pallet[idx].State)
                            {
                                // 上料区放空夹具
                                modEvent = EventList.OnloadPlaceEmptyPallet;
                                int pos = -1;
                                state = GetEvent(this, modEvent, ref pos);
                                if ((EventStatus.Invalid == state) || (EventStatus.Finished == state) ||
                                    ((EventStatus.Require == state) && idx != pos))
                                {
                                    SetEvent(this, modEvent, EventStatus.Require, idx);
                                }
                                // 上料区最后一个位置
                                if (((int)ModuleMaxPallet.OnloadRobot - 1) == idx)
                                {
                                    // 上料区放NG非空夹具，转盘
                                    if (this.placeNGPallet)
                                    {
                                        modEvent = EventList.OnloadPlaceNGPallet;
                                        pos = -1;
                                        state = GetEvent(this, modEvent, ref pos);
                                        if ((EventStatus.Invalid == state) || (EventStatus.Finished == state) ||
                                        ((EventStatus.Require == state) && idx != pos))
                                        {
                                            SetEvent(this, modEvent, EventStatus.Require, idx);
                                        }
                                    }
                                    // 上料区放待回炉含假电池夹具（已取走假电池，待重新放回假电池的夹具）
                                    modEvent = EventList.OnloadPlaceReputFakePallet;
                                    pos = -1;
                                    state = GetEvent(this, modEvent, ref pos);
                                    if ((EventStatus.Invalid == state) || (EventStatus.Finished == state) ||
                                    ((EventStatus.Require == state) && idx != pos))
                                    {
                                        SetEvent(this, modEvent, EventStatus.Require, idx);
                                    }
                                    // 上料区放待检测含假电池夹具（未取走假电池的夹具）
                                    if (this.detectFakeBat)
                                    {
                                        modEvent = EventList.OnLoadPlaceDetectFakePallet;
                                        pos = -1;
                                        state = GetEvent(this, modEvent, ref pos);
                                        if ((EventStatus.Invalid == state) || (EventStatus.Finished == state) ||
                                        ((EventStatus.Require == state) && idx != pos))
                                        {
                                            SetEvent(this, modEvent, EventStatus.Require, idx);
                                        }
                                    }
                                }
                            }
                            // 上料区取NG空夹具
                            if ((PalletStatus.NG == this.Pallet[idx].State) && this.Pallet[idx].IsEmpty())
                            {
                                modEvent = EventList.OnloadPickNGEmptyPallet;
                                int pos = -1;
                                state = GetEvent(this, modEvent, ref pos);
                                if ((EventStatus.Invalid == state) || (EventStatus.Finished == state) ||
                                        ((EventStatus.Require == state) && idx != pos))
                                {
                                    SetEvent(this, modEvent, EventStatus.Require, idx);
                                }
                            }
                            // 上料区取OK满夹具
                            if ((PalletStatus.OK == this.Pallet[idx].State)
                                && (PalletStage.Onload == this.Pallet[idx].Stage)
                                && !this.Pallet[idx].IsEmpty() && !this.Pallet[idx].HasFake())
                            {
                                modEvent = EventList.OnloadPickOKFullPallet;
                                int pos = -1;
                                state = GetEvent(this, modEvent, ref pos);
                                if ((EventStatus.Invalid == state) || (EventStatus.Finished == state) ||
                                    ((EventStatus.Require == state) && idx != pos))
                                {
                                    SetEvent(this, modEvent, EventStatus.Require, idx);
                                }
                            }
                            // 上料区取OK带假电池满夹具
                            if ((PalletStatus.OK == this.Pallet[idx].State)
                                && (PalletStage.Onload == this.Pallet[idx].Stage)
                                && this.Pallet[idx].HasFake())
                            {
                                modEvent = EventList.OnloadPickOKFakeFullPallet;
                                int pos = -1;
                                state = GetEvent(this, modEvent, ref pos);
                                if ((EventStatus.Invalid == state) || (EventStatus.Finished == state) ||
                                    ((EventStatus.Require == state) && idx != pos))
                                {
                                    SetEvent(this, modEvent, EventStatus.Require, idx);
                                }
                            }
                            // 上料区取回炉假电池夹具（已放回假电池的夹具）
                            if ((PalletStatus.Rebaking == this.Pallet[idx].State) && this.Pallet[idx].HasFake())
                            {
                                modEvent = EventList.OnloadPickRebakeFakePallet;
                                int pos = -1;
                                state = GetEvent(this, modEvent, ref pos);
                                if ((EventStatus.Invalid == state) || (EventStatus.Finished == state) ||
                                        ((EventStatus.Require == state) && idx != pos))
                                {
                                    SetEvent(this, modEvent, EventStatus.Require, idx);
                                }
                            }
                            // 上料区取等待水含量结果夹具（已取待测假电池的夹具）
                            if ((PalletStatus.WaitResult == this.Pallet[idx].State) && this.Pallet[idx].HasFake())
                            {
                                modEvent = EventList.OnLoadPickWaitResultPallet;
                                int pos = -1;
                                state = GetEvent(this, modEvent, ref pos);
                                if ((EventStatus.Invalid == state) || (EventStatus.Finished == state) ||
                                        ((EventStatus.Require == state) && idx != pos))
                                {
                                    SetEvent(this, modEvent, EventStatus.Require, idx);
                                }
                            }
                        }
                        #endregion

                        #region // 有取放已响应
                        for (EventList i = EventList.OnloadPlaceEmptyPallet; i < EventList.OnloadPickPlaceEnd; i++)
                        {
                            if (EventStatus.Response == GetEvent(this, i))
                            {
                                this.avoidEvent = i;
                                this.nextAutoStep = AutoSteps.Auto_RobotMoveAvoidPos;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                return;
                            }
                        }
                        #endregion

                        #region // 夹具扫码及放电池

                        if(PltNeedScanCode(ref this.pickPos))
                        {
                            this.AutoStepSafe = false;
                            this.nextAutoStep = AutoSteps.Auto_PalletScanCodeMove;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            break;
                        }
                        else if (PltNeedPickDetectFake(ref pickPos))
                        {
                            this.AutoStepSafe = false;
                            this.nextAutoStep = AutoSteps.Auto_CalcPalletPickPos;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            break;
                        }
                        else if (PltNeedReFake())
                        {
                            if(CalcPickFakePos(-1, ref pickPos))
                            {
                                this.AutoStepSafe = false;
                                this.nextAutoStep = AutoSteps.Auto_CalcFakePickPos;
                                SaveRunData(SaveType.AutoStep|SaveType.Variables);
                                break;
                            }
                        }
                        else if (PltNeedBat(ref placePallet))
                        {
                            // 夹具需要转盘
                            if(CalcPickNGPalletPos(placePallet, ref pickPos))
                            {
                                // 暂存位有电池，假电池位取假电池
                                if(IsOnloadFakeRowCol(placePallet) && (BufferCount() > 0) && CalcPickFakePos(placePallet, ref pickPos))
                                {
                                    this.AutoStepSafe = false;
                                    this.nextAutoStep = AutoSteps.Auto_CalcFakePickPos;
                                    SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                    break;
                                }
                                else
                                {
                                    this.AutoStepSafe = false;
                                    this.nextAutoStep = AutoSteps.Auto_CalcPalletPickPos;
                                    SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                    break;
                                }
                            }
                            // 非清尾料 || 暂存非空（此时不管是否清尾料） && 假电池行列
                            else if((!onloadClear || (BufferCount() > 0)) && IsOnloadFakeRowCol(placePallet))
                            {
                                // 暂存位无电池，取OK电池放到暂存配对
                                if(BufferCount() < 1)
                                {
                                    if(CalcPickPos(placePallet, ref pickPos))
                                    {
                                        this.AutoStepSafe = false;
                                        this.nextAutoStep = AutoSteps.Auto_CalcOnlinePickPos;
                                        SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                        break;
                                    }
                                }
                                // 假电池位取假电池
                                else if(CalcPickFakePos(placePallet, ref pickPos))
                                {
                                    this.AutoStepSafe = false;
                                    this.nextAutoStep = AutoSteps.Auto_CalcFakePickPos;
                                    SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                    break;
                                }
                            }
                            // 计算抓手与暂存配对
                            else if(CalcFingerBufferMatchesPos(this.placePallet, ref this.placePos))
                            {
                                this.AutoStepSafe = false;
                                this.nextAutoStep = AutoSteps.Auto_CalcBufferPos;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                break;
                            }
                            // 来料线取电池
                            else if(!onloadClear && CalcPickPos(placePallet, ref pickPos))
                            {
                                this.AutoStepSafe = false;
                                this.nextAutoStep = AutoSteps.Auto_CalcOnlinePickPos;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                break;
                            }
                        }
                        #endregion

                        #region // 无任务时回来料位等待
                        if(!this.AutoStepSafe)
                        {
                            if (GetRobotCmd(OnloadRobotStation.OnloadLine, 0, 0, robotSpeed, RobotOrder.MOVE, ref this.robotCmd)
                                && RobotMotorMove(this.robotCmd, MotorPosition.Onload_LinePickPos))
                            {
                                this.AutoStepSafe = true;
                            }
                        }
                        #endregion
                        break;
                    }

                #region // 避让大机器人
                case AutoSteps.Auto_RobotMoveAvoidPos:
                    {
                        CurMsgStr("机器人移动到避让位", "Robot move to avoid pos");
                        if (RobotHome())
                        {
                            int pltIdx = -1;
                            if(EventStatus.Response == GetEvent(this, this.avoidEvent, ref pltIdx))
                            {
                                switch(this.avoidEvent)
                                {
                                    case EventList.OnloadPlaceEmptyPallet:
                                    case EventList.OnloadPlaceNGPallet:
                                    case EventList.OnloadPlaceReputFakePallet:
                                        {
                                            if (!PalletKeepFlat(pltIdx, false, true))
                                            {
                                                return;
                                            }
                                            break;
                                        }
                                    case EventList.OnloadPickNGEmptyPallet:
                                    case EventList.OnloadPickOKFullPallet:
                                    case EventList.OnloadPickOKFakeFullPallet:
                                    case EventList.OnloadPickRebakeFakePallet:
                                        {
                                            if(!PalletKeepFlat(pltIdx, true, true))
                                            {
                                                return;
                                            }
                                            break;
                                        }
                                }
                                if (SetEvent(this, this.avoidEvent, EventStatus.Ready, pltIdx))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_WaitActionFinish;
                                    SaveRunData(SaveType.AutoStep);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_WaitActionFinish:
                    {
                        CurMsgStr("等待取放动作完成", "Robot wait action finish");
                        int pltIdx = -1;
                        if((EventStatus.Finished == GetEvent(this, this.avoidEvent, ref pltIdx))
                            && ((pltIdx > -1) && (pltIdx < (int)ModuleMaxPallet.OnloadRobot)))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            SaveRunData(SaveType.AutoStep | SaveType.Pallet, pltIdx);
                        }
                        break;
                    }
                #endregion

                #region // 取：NG夹具转盘，取待测假电池
                case AutoSteps.Auto_CalcPalletPickPos:
                    {
                        CurMsgStr("计算NG夹具转盘位", "Calc pick NG pallet pos");
                        this.nextAutoStep = AutoSteps.Auto_PalletPickPosSetEvent;
                        SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        break;
                    }
                case AutoSteps.Auto_PalletPickPosSetEvent:
                    {
                        this.msgChs = string.Format("机器人移动到NG转盘位[{0}-{1}行-{2}列]前发送信号", GetRobotStationName(pickPos.station), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Set event before robot goto pick NG pallet pos[{0}-{1}row-{2}col]", (pickPos.station), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        {
                            this.nextAutoStep = AutoSteps.Auto_PalletPickPosPickMove;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletPickPosPickMove:
                    {
                        this.msgChs = string.Format("机器人移动到NG转盘位[{0}-{1}行-{2}列]", GetRobotStationName(pickPos.station), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Robot goto pick NG pallet pos[{0}-{1}row-{2}col]", (pickPos.station), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(CheckStationSafe(pickPos, RobotOrder.MOVE))
                        {
                            if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.MOVE, ref robotCmd)
                                && RobotMotorMove(robotCmd, pickPos.motorPos))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PalletPickPosPickDown;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletPickPosPickDown:
                    {
                        this.msgChs = string.Format("机器人到NG转盘位[{0}-{1}行-{2}列]下降", GetRobotStationName(pickPos.station), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Robot goto place pos[{0}-{1}row-{2}col] down", (pickPos.station), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(MotorUMove(pickPos.motorPos) && CheckStationSafe(pickPos, RobotOrder.DOWN) 
                            && FingerCheck(pickPos.finger, !pickPos.fingerClose) && FingerClose(pickPos.finger, !pickPos.fingerClose))
                        {
                            if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.DOWN, ref robotCmd)
                                && RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PalletPickPosFingerAction;
                                //SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletPickPosFingerAction:
                    {
                        this.msgChs = string.Format("NG转盘抓手关闭[{0}-{1}行-{2}列]", GetRobotStationName(pickPos.station), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Finger close[{0}-{1}row-{2}col]", (pickPos.station), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerClose(pickPos.finger, pickPos.fingerClose))
                        {
                            int pltIndex = pickPos.station - OnloadRobotStation.PalletStation_0;
                            switch(pickPos.finger)
                            {
                                case ModDef.Finger_0:
                                    {
                                        this.Battery[(int)ModDef.Finger_0].Copy(this.Pallet[pltIndex].Battery[pickPos.row, pickPos.col]);
                                        switch(this.Pallet[pltIndex].State)
                                        {
                                            case PalletStatus.NG:
                                                if (BatteryNGStatus.HighTmp != (BatteryNGStatus.HighTmp & this.Battery[(int)ModDef.Finger_0].NGType))
                                                {
                                                    this.Battery[(int)ModDef.Finger_0].Type = BatteryStatus.OK;
                                                }
                                                this.Pallet[pltIndex].Battery[pickPos.row, pickPos.col].Release();
                                                break;
                                            case PalletStatus.Detect:
                                                this.Pallet[pltIndex].State = PalletStatus.WaitResult;
                                                if(BatteryStatus.Fake == this.Battery[(int)ModDef.Finger_0].Type)
                                                {
                                                    this.Pallet[pltIndex].Battery[pickPos.row, pickPos.col].Type = BatteryStatus.FakeTag;

                                                    this.Battery[(int)ModDef.Finger_0].Type = BatteryStatus.Detect;
                                                }
                                                break;
                                        }
                                        break;
                                    }
                                case ModDef.Finger_1:
                                    {
                                        this.Battery[(int)ModDef.Finger_1].Copy(this.Pallet[pltIndex].Battery[pickPos.row + 1, pickPos.col]);
                                        switch(this.Pallet[pltIndex].State)
                                        {
                                            case PalletStatus.NG:
                                                if(BatteryNGStatus.HighTmp != (BatteryNGStatus.HighTmp & this.Battery[(int)ModDef.Finger_1].NGType))
                                                {
                                                    this.Battery[(int)ModDef.Finger_1].Type = BatteryStatus.OK;
                                                }
                                                this.Pallet[pltIndex].Battery[pickPos.row + 1, pickPos.col].Release();
                                                break;
                                            case PalletStatus.Detect:
                                                this.Pallet[pltIndex].State = PalletStatus.WaitResult;
                                                if(BatteryStatus.Fake == this.Battery[(int)ModDef.Finger_1].Type)
                                                {
                                                    this.Pallet[pltIndex].Battery[pickPos.row + 1, pickPos.col].Type = BatteryStatus.FakeTag;

                                                    this.Battery[(int)ModDef.Finger_1].Type = BatteryStatus.Detect;
                                                }
                                                break;
                                        }
                                        break;
                                    }
                                case ModDef.Finger_ALL:
                                    {
                                        for(ModDef i = ModDef.Finger_0; i < pickPos.finger; i++)
                                        {
                                            Battery bat = this.Pallet[pltIndex].Battery[pickPos.row + (int)i, pickPos.col];
                                            if((BatteryStatus.Invalid != bat.Type))
                                            {
                                                this.Battery[(int)i].Copy(bat);
                                                if((BatteryStatus.Fake != bat.Type) 
                                                    && (BatteryNGStatus.HighTmp != (BatteryNGStatus.HighTmp & this.Battery[(int)i].NGType)))
                                                {
                                                    this.Battery[(int)i].Type = BatteryStatus.OK;
                                                }
                                                bat.Release();
                                            }
                                        }
                                        break;
                                    }
                                default:
                                    return;
                            }
                            this.nextAutoStep = AutoSteps.Auto_PalletPickPosPickUp;
                            SaveRunData(SaveType.AutoStep | SaveType.Battery | SaveType.Pallet, pltIndex);
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletPickPosPickUp:
                    {
                        this.msgChs = string.Format("机器人到NG转盘位[{0}-{1}行-{2}列]上升", GetRobotStationName(pickPos.station), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Robot goto pick NG pallet pos[{0}-{1}row-{2}col] up", (pickPos.station), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.UP, ref robotCmd)
                            && RobotMove(robotCmd, true))
                        {
                            this.nextAutoStep = AutoSteps.Auto_PalletPickPosCheckFinger;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletPickPosCheckFinger:
                    {
                        this.msgChs = string.Format("NG转盘位[{0}-{1}行-{2}列]取料后检查抓手", GetRobotStationName(pickPos.station), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Pick NG pallet pos[{0}-{1}row-{2}col] check finger", (pickPos.station), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerCheck(pickPos.finger, pickPos.fingerClose))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                        }
                        break;
                    }
                #endregion

                #region // 取：来料线
                case AutoSteps.Auto_CalcOnlinePickPos:
                    {
                        CurMsgStr("计算来料取料位", "Calc online pick pos");
                        this.nextAutoStep = AutoSteps.Auto_OnlinePosSetEvent;
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                case AutoSteps.Auto_OnlinePosSetEvent:
                    {
                        this.msgChs = string.Format("机器人移动到取料位[{0}-{1}行-{2}列]前发送信号", GetRobotStationName(pickPos.station), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Set event before robot goto pick pos[{0}-{1}row-{2}col]", (pickPos.station), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(EventStatus.Require == GetEvent(this.pickBatRun, EventList.OnloadLinePickBattery))
                        {
                            if(SetEvent(this.pickBatRun, EventList.OnloadLinePickBattery, EventStatus.Response))
                            {
                                this.nextAutoStep = AutoSteps.Auto_OnlinePosPickMove;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OnlinePosPickMove:
                    {
                        this.msgChs = string.Format("机器人移动到取料位[{0}-{1}行-{2}列]", GetRobotStationName(pickPos.station), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Robot goto pick pos[{0}-{1}row-{2}col]", (pickPos.station), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(CheckStationSafe(pickPos, RobotOrder.MOVE))
                        {
                            if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.MOVE, ref robotCmd)
                                && RobotMotorMove(robotCmd, pickPos.motorPos))
                            {
                                this.nextAutoStep = AutoSteps.Auto_OnlinePosPickDown;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OnlinePosPickDown:
                    {
                        this.msgChs = string.Format("机器人取料位[{0}-{1}行-{2}列]下降", GetRobotStationName(pickPos.station), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Robot goto pick pos[{0}-{1}row-{2}col] down", (pickPos.station), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        EventStatus state = GetEvent(this.pickBatRun, EventList.OnloadLinePickBattery);
                        if((EventStatus.Ready == state) || (EventStatus.Start == state))
                        {
                            if(MotorUMove(pickPos.motorPos) && CheckStationSafe(pickPos, RobotOrder.DOWN) 
                                && FingerCheck(ModDef.Finger_ALL, false) && FingerClose(pickPos.finger, !pickPos.fingerClose))
                            {
                                if(this.pickBatRun.RecvRearEnd())
                                {
                                    // 电池追尾，停机报警
                                    ShowMessageBox((int)MsgID.RecvCrash, "取料线电池追尾", "请检查取料线上电池是否追尾", MessageType.MsgAlarm);
                                }
                                else
                                {
                                    SetEvent(this.pickBatRun, EventList.OnloadLinePickBattery, EventStatus.Start);
                                    if (GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.DOWN, ref robotCmd))
                                    {
                                        if (RobotMove(robotCmd, true))
                                        {
                                            this.nextAutoStep = AutoSteps.Auto_OnlinePosFingerAction;
                                            //SaveRunData(SaveType.AutoStep);
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OnlinePosFingerAction:
                    {
                        this.msgChs = string.Format("取料抓手关闭[{0}-{1}行-{2}列]", GetRobotStationName(pickPos.station), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Finger close[{0}-{1}row-{2}col]", (pickPos.station), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerClose(pickPos.finger, pickPos.fingerClose))
                        {
                            if(null != this.pickBatRun)
                            {
                                for(ModDef i = ModDef.Finger_0; i < ModDef.Finger_ALL; i++)
                                {
                                    this.Battery[(int)i].Copy(this.pickBatRun.Battery[(int)i]);
                                    this.pickBatRun.Battery[(int)i].Release();
                                }
                                this.pickBatRun.SaveRunData(SaveType.Battery);
                            }
                            this.nextAutoStep = AutoSteps.Auto_OnlinePosPickUp;
                            SaveRunData(SaveType.AutoStep | SaveType.Battery);
                        }
                        break;
                    }
                case AutoSteps.Auto_OnlinePosPickUp:
                    {
                        this.msgChs = string.Format("机器人取料位[{0}-{1}行-{2}列]上升", GetRobotStationName(pickPos.station), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Robot goto pick pos[{0}-{1}row-{2}col] up", (pickPos.station), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.UP, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_OnlinePosCheckFinger;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OnlinePosCheckFinger:
                    {
                        this.msgChs = string.Format("来料线位[{0}-{1}行-{2}列]取料检查抓手", GetRobotStationName(pickPos.station), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Pick pos[{0}-{1}row-{2}col] check finger", (pickPos.station), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerCheck(pickPos.finger, pickPos.fingerClose))
                        {
                            EventStatus state = GetEvent(this.pickBatRun, EventList.OnloadLinePickBattery);
                            if((EventStatus.Ready == state) || (EventStatus.Start == state))
                            {
                                SetEvent(this.pickBatRun, EventList.OnloadLinePickBattery, EventStatus.Finished);
                            }
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                #endregion

                #region // 取：假电池线
                case AutoSteps.Auto_CalcFakePickPos:
                    {
                        CurMsgStr("计算假电池线取料位", "Calc onload fake pick pos");
                        this.nextAutoStep = AutoSteps.Auto_FakePosSetEvent;
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                case AutoSteps.Auto_FakePosSetEvent:
                    {
                        this.msgChs = string.Format("机器人移动到假电池位[{0}-{1}行-{2}列]前发送信号", GetRobotStationName(OnloadRobotStation.OnloadFakeScan), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Set event before robot goto pick pos[{0}-{1}row-{2}col]", (OnloadRobotStation.OnloadFakeScan), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(EventStatus.Require == GetEvent(this.pickFakeRun, EventList.OnloadFakePickBattery))
                        {
                            if(SetEvent(this.pickFakeRun, EventList.OnloadFakePickBattery, EventStatus.Response))
                            {
                                if (this.scanFakeBatEnable)
                                {
                                    this.nextAutoStep = AutoSteps.Auto_FakeScanPosMove;
                                }
                                else
                                {
                                    RunProcessOnloadFake run = this.pickFakeRun;
                                    if(null != run)
                                    {
                                        run.Battery[pickPos.row].Code = ("Fake" + Def.GetRandom(11111, 99999));
                                        run.SaveRunData(SaveType.Battery);
                                    }
                                    this.nextAutoStep = AutoSteps.Auto_FakePosPickMove;
                                }
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_FakeScanPosMove:
                    {
                        this.msgChs = string.Format("机器人移动到假电池扫码位[{0}-{1}行-{2}列]", GetRobotStationName(OnloadRobotStation.OnloadFakeScan), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Robot goto scan fakebattery code pos[{0}-{1}row-{2}col]", (OnloadRobotStation.OnloadFakeScan), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        PickPlacePos pos = new PickPlacePos();
                        pos.SetData(OnloadRobotStation.OnloadFakeScan, pickPos.row, pickPos.col, pickPos.finger, pickPos.fingerClose, MotorPosition.Onload_ScanFakePos);
                        if(CheckStationSafe(pos, RobotOrder.MOVE))
                        {
                            if(GetRobotCmd(pos.station, pos.row, pos.col, robotSpeed, RobotOrder.MOVE, ref robotCmd)
                                && RobotMotorMove(robotCmd, pos.motorPos))
                            {
                                this.nextAutoStep = AutoSteps.Auto_FakeScanPosDown;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_FakeScanPosDown:
                    {
                        this.msgChs = string.Format("机器人移动到假电池扫码位[{0}-{1}行-{2}列]下降", GetRobotStationName(OnloadRobotStation.OnloadFakeScan), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Robot goto scan fakebattery code pos[{0}-{1}row-{2}col] down", (OnloadRobotStation.OnloadFakeScan), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        PickPlacePos pos = new PickPlacePos();
                        pos.SetData(OnloadRobotStation.OnloadFakeScan, pickPos.row, pickPos.col, pickPos.finger, pickPos.fingerClose, pickPos.motorPos);
                        if(CheckStationSafe(pos, RobotOrder.DOWN) && FingerCheck(pickPos.finger, !pickPos.fingerClose) && FingerClose(pickPos.finger, !pickPos.fingerClose))
                        {
                            if(GetRobotCmd(pos.station, pos.row, pos.col, robotSpeed, RobotOrder.DOWN, ref robotCmd)
                                && RobotMotorMove(robotCmd, pos.motorPos))
                            {
                                this.nextAutoStep = AutoSteps.Auto_FakeScanPosScanCode;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_FakeScanPosScanCode:
                    {
                        this.msgChs = string.Format("机器人扫假电池扫条码[{0}-{1}行-{2}列]", GetRobotStationName(OnloadRobotStation.OnloadFakeScan), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Robot scan fakebattery code [{0}-{1}row-{2}col]", (OnloadRobotStation.OnloadFakeScan), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        for(int idx = 0; idx < this.scanMaxCount; idx++)
                        {
                            string code = "";
                            if(ScanCode() && GetScanResult(ref code))
                            {
                                if(CheckScanCode(code, (idx >= (this.scanMaxCount-1))))
                                {
                                    RunProcessOnloadFake run = this.pickFakeRun;
                                    if(null != run)
                                    {
                                        run.Battery[pickPos.row].Code = this.scanFakeBatEnable ? code : ("Fake" + Def.GetRandom(11111, 99999));
                                        run.SaveRunData(SaveType.Battery);
                                    }
                                    this.nextAutoStep = AutoSteps.Auto_FakeScanPosUp;
                                    SaveRunData(SaveType.AutoStep);
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_FakeScanPosUp:
                    {
                        this.msgChs = string.Format("机器人移动到假电池扫码位[{0}-{1}行-{2}列]上升", GetRobotStationName(OnloadRobotStation.OnloadFakeScan), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Robot goto scan fakebattery code pos[{0}-{1}row-{2}col] up", (OnloadRobotStation.OnloadFakeScan), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        PickPlacePos pos = new PickPlacePos();
                        pos.SetData(OnloadRobotStation.OnloadFakeScan, pickPos.row, pickPos.col, pickPos.finger, pickPos.fingerClose, pickPos.motorPos);
                        if(CheckStationSafe(pos, RobotOrder.UP))
                        {
                            if(GetRobotCmd(pos.station, pos.row, pos.col, robotSpeed, RobotOrder.UP, ref robotCmd)
                                && RobotMotorMove(robotCmd, pos.motorPos))
                            {
                                this.nextAutoStep = AutoSteps.Auto_FakePosPickMove;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }

                case AutoSteps.Auto_FakePosPickMove:
                    {
                        this.msgChs = string.Format("机器人移动到假电池位[{0}-{1}行-{2}列]", GetRobotStationName(pickPos.station), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Robot goto pick pos[{0}-{1}row-{2}col]", (pickPos.station), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(CheckStationSafe(pickPos, RobotOrder.MOVE))
                        {
                            if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.MOVE, ref robotCmd)
                                && RobotMotorMove(robotCmd, pickPos.motorPos))
                            {
                                this.nextAutoStep = AutoSteps.Auto_FakePosPickDown;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_FakePosPickDown:
                    {
                        this.msgChs = string.Format("机器人假电池位[{0}-{1}行-{2}列]下降", GetRobotStationName(pickPos.station), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Robot goto pick pos[{0}-{1}row-{2}col] down", (pickPos.station), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        EventStatus state = GetEvent(this.pickFakeRun, EventList.OnloadFakePickBattery);
                        if((EventStatus.Ready == state) || (EventStatus.Start == state))
                        {
                            if(MotorUMove(pickPos.motorPos) && CheckStationSafe(pickPos, RobotOrder.DOWN) && FingerCheck(ModDef.Finger_ALL, false))
                            {
                                SetEvent(this.pickFakeRun, EventList.OnloadFakePickBattery, EventStatus.Start);
                                if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.DOWN, ref robotCmd))
                                {
                                    if(RobotMove(robotCmd, true))
                                    {
                                        this.nextAutoStep = AutoSteps.Auto_FakePosFingerAction;
                                        //SaveRunData(SaveType.AutoStep);
                                    }
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_FakePosFingerAction:
                    {
                        this.msgChs = string.Format("取料抓手关闭[{0}-{1}行-{2}列]", GetRobotStationName(pickPos.station), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Finger close[{0}-{1}row-{2}col]", (pickPos.station), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerClose(pickPos.finger, pickPos.fingerClose))
                        {
                            RunProcessOnloadFake run = this.pickFakeRun;
                            if(null != run)
                            {
                                this.Battery[(int)pickPos.finger].Copy(run.Battery[pickPos.row]);
                                run.Battery[pickPos.row].Release();
                                run.SaveRunData(SaveType.Battery);
                            }

                            this.nextAutoStep = AutoSteps.Auto_FakePosPickUp;
                            SaveRunData(SaveType.AutoStep | SaveType.Battery);
                        }
                        break;
                    }
                case AutoSteps.Auto_FakePosPickUp:
                    {
                        this.msgChs = string.Format("机器人假电池位[{0}-{1}行-{2}列]上升", GetRobotStationName(pickPos.station), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Robot goto pick pos[{0}-{1}row-{2}col] up", (pickPos.station), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.UP, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_FakePosCheckFinger;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_FakePosCheckFinger:
                    {
                        this.msgChs = string.Format("假电池位[{0}-{1}行-{2}列]取料后检查抓手", GetRobotStationName(pickPos.station), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Pick pos[{0}-{1}row-{2}col] check finger", (pickPos.station), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerCheck(pickPos.finger, pickPos.fingerClose))
                        {
                            EventStatus state = GetEvent(this.pickFakeRun, EventList.OnloadFakePickBattery);
                            if((EventStatus.Ready == state) || (EventStatus.Start == state))
                            {
                                SetEvent(this.pickFakeRun, EventList.OnloadFakePickBattery, EventStatus.Finished);
                            }
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                #endregion

                #region // 夹具扫码
                case AutoSteps.Auto_PalletScanCodeMove:
                    {
                        this.msgChs = string.Format("机器人移动到夹具扫码位[{0}-{1}行-{2}列]", GetRobotStationName(pickPos.station), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Robot goto pick pos[{0}-{1}row-{2}col]", (pickPos.station), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.MOVE, ref robotCmd))
                        {
                            if(RobotMotorMove(robotCmd, pickPos.motorPos))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PalletScanCodeDown;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletScanCodeDown:
                    {
                        this.msgChs = string.Format("机器人到夹具扫码位[{0}-{1}行-{2}列]下降", GetRobotStationName(pickPos.station), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Robot pick pos[{0}-{1}row-{2}col] down", (pickPos.station), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(MotorUMove(pickPos.motorPos) && CheckStationSafe(pickPos, RobotOrder.DOWN) 
                            && FingerCheck(ModDef.Finger_ALL, false) && FingerClose(ModDef.Finger_ALL, false))
                        {
                            if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.DOWN, ref robotCmd)
                                && RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PalletScanCodeAction;
                                //SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletScanCodeAction:
                    {
                        this.msgChs = string.Format("机器人到夹具扫码位[{0}-{1}行-{2}列]扫码", GetRobotStationName(pickPos.station), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Robot pick pos[{0}-{1}row-{2}col] scan code", (pickPos.station), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        bool scanFinish = false;
                        string msg = "";
                        int pltIdx = (int)pickPos.station - (int)OnloadRobotStation.ScanPalletCode_0;
                        for(int idx = 0; idx < this.scanMaxCount; idx++)
                        {
                            bool scanPltCode = this.scanPalletEnable && this.scanEnable;
                            string code = scanPltCode ? "" : $"PLT{Def.GetRandom(11111111, 99999999)}";
                            if (!scanPltCode)
                            {
                                scanFinish = true;
                                this.Pallet[pltIdx].Code = code;
                                break;
                            }
                            // 触发
                            if(!ScanCode())
                            {
                                return;
                            }
                            // 获取结果
                            if(!GetScanResult(ref code))
                            {
                                if((idx + 1) < this.scanMaxCount)
                                {
                                    continue;
                                }
                            }
                            // 结果判定
                            this.Pallet[pltIdx].Code = code;
                            if(!string.IsNullOrEmpty(this.scanNGType) && (code.IndexOf(this.scanNGType) > -1))
                            {
                                if((idx + 1) >= this.scanMaxCount)
                                {
                                    this.Pallet[pltIdx].State = PalletStatus.NG;
                                    this.Pallet[pltIdx].Code += $"【扫码NG，超过{this.scanMaxCount}次】";
                                    break;
                                }
                            }
                            else if (!string.IsNullOrEmpty(code))
                            {
                                //scanFinish = MesCheckPalletStatus(code,ref msg);
                                //MES超时重传三次
                                for (int i = 0; i < (MesData.mesFrequency == 0 ? 3 : MesData.mesFrequency); i++)
                                {
                                    //MES夹具校验
                                    if (!MesCheckPalletStatus(code, ref msg))
                                    {
                                        //获取报警字符串是否包含"超时"二字,如果没有则跳出循环
                                        if (!msg.Contains("超时"))
                                        {
                                            scanFinish = false;
                                            break;
                                        }
                                        if (i == 2)
                                        {
                                            scanFinish = false;
                                            ShowMsgBox.ShowDialog($"MES夹具校验接口超时失败3次，请检查MES连接状态", MessageType.MsgAlarm);
                                        }
                                    }
                                    else
                                    {
                                        scanFinish = true;
                                        break;
                                    }
                                }
                               
                                if (!scanFinish)
                                {
                                    this.Pallet[pltIdx].State = PalletStatus.NG;
                                    this.Pallet[pltIdx].Code += "【MES NG】";
                                }
                                break;
                            }
                            if((idx + 1) >= this.scanMaxCount)
                            {
                                this.Pallet[pltIdx].State = PalletStatus.NG;
                                this.Pallet[pltIdx].Code += $"【扫码NG，超过{this.scanMaxCount}次】";
                                break;
                            }
                        }
                        if (scanFinish)
                        {
                            this.nextAutoStep = AutoSteps.Auto_PalletScanCodeUp;
                            SaveRunData(SaveType.Pallet, pltIdx);
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletScanCodeUp:
                    {
                        this.msgChs = string.Format("机器人到夹具扫码位[{0}-{1}行-{2}列]上升", GetRobotStationName(pickPos.station), pickPos.row, pickPos.col);
                        this.msgEng = string.Format("Robot pick pos[{0}-{1}row-{2}col] up", (pickPos.station), pickPos.row, pickPos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(pickPos.station, pickPos.row, pickPos.col, robotSpeed, RobotOrder.UP, ref robotCmd))
                        {
                            if(RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                #endregion

                #region // 计算放位置
                case AutoSteps.Auto_CalcPlacePos:
                    {
                        CurMsgStr("计算放位置", "Calc place pos");

                        #region // 有取放已响应：优先处理调度机器人取放事件
                        for(EventList i = EventList.OnloadPlaceEmptyPallet; i < EventList.OnloadPickPlaceEnd; i++)
                        {
                            if(EventStatus.Response == GetEvent(this, i))
                            {
                                this.avoidEvent = i;
                                this.nextAutoStep = AutoSteps.Auto_RobotMoveAvoidPos;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                return;
                            }
                        }
                    #endregion

                    // 放待检测电池
                    if((BatteryStatus.Detect==FingerBat(ModDef.Finger_0).Type)||(BatteryStatus.Detect==FingerBat(ModDef.Finger_1).Type)) {
                        if(CalcPlaceDetectFakePos(ref this.placePos)) {
                            this.nextAutoStep=AutoSteps.Auto_CalcNGLinePlacePos;
                            SaveRunData(SaveType.Variables);
                        }
                    }
                    // 优先放NG电池
                    else if((BatteryStatus.NG==FingerBat(ModDef.Finger_0).Type)||(BatteryStatus.NG==FingerBat(ModDef.Finger_1).Type)) {
                        if(CalcPlaceNGPos(ref this.placePos)) {
                            this.nextAutoStep=AutoSteps.Auto_CalcNGLinePlacePos;
                            SaveRunData(SaveType.Variables);
                        }
                    }
                    // 计算是否放回炉假电池
                    else if(CalcPlaceReFakePos(ref placePos)) {
                        this.nextAutoStep=AutoSteps.Auto_CalcPalletPlacePos;
                        SaveRunData(SaveType.Variables);
                    }
                    // 计算放暂存 || 抓手与暂存配对
                    else if(CalcFingerBufferMatchesPos(this.placePallet , ref this.placePos)) {
                        this.nextAutoStep=AutoSteps.Auto_CalcBufferPos;
                        SaveRunData(SaveType.Variables);
                    }
                    // 放夹具
                    else if(CalcPlacePalletPos(this.placePallet , ref this.placePos)) {
                        this.nextAutoStep=AutoSteps.Auto_CalcPalletPlacePos;
                        SaveRunData(SaveType.Variables);
                    }
                    // 抓手为假电池 && 当前夹具非假电池夹具
                    else if((BatteryStatus.Fake==FingerBat(ModDef.Finger_0).Type)
                        &&!IsOnloadFakeRowCol(this.placePallet)) {
                        if(CalcPlaceNGOutPos(ref placePos)) {
                            this.nextAutoStep=AutoSteps.Auto_CalcNGLinePlacePos;
                            SaveRunData(SaveType.AutoStep|SaveType.Variables);
                        }
                    }
                    // 抓手为空
                    else if((BatteryStatus.Invalid==FingerBat(ModDef.Finger_0).Type)&&(BatteryStatus.Invalid==FingerBat(ModDef.Finger_1).Type)) {
                        this.nextAutoStep=AutoSteps.Auto_WorkEnd;
                    }
                        break;
                    }
                #endregion

                #region // 放：夹具
                case AutoSteps.Auto_CalcPalletPlacePos:
                    {
                        CurMsgStr("计算夹具放料位", "Calc place pallet pos");
                        this.nextAutoStep = AutoSteps.Auto_PalletPlacePosSetEvent;
                        SaveRunData(SaveType.AutoStep|SaveType.Variables);
                        break;
                    }
                case AutoSteps.Auto_PalletPlacePosSetEvent:
                    {
                        this.msgChs = string.Format("机器人移动到放料位[{0}-{1}行-{2}列]前发送信号", GetRobotStationName(placePos.station), placePos.row, placePos.col);
                        this.msgEng = string.Format("Set event before robot goto place pos[{0}-{1}row-{2}col]", (placePos.station), placePos.row, placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        {
                            this.nextAutoStep = AutoSteps.Auto_PalletPlacePosPlaceMove;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletPlacePosPlaceMove:
                    {
                        this.msgChs = string.Format("机器人移动到放料位[{0}-{1}行-{2}列]", GetRobotStationName(placePos.station), placePos.row, placePos.col);
                        this.msgEng = string.Format("Robot goto place pos[{0}-{1}row-{2}col]", (placePos.station), placePos.row, placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if (CheckStationSafe(placePos, RobotOrder.MOVE))
                        {
                            if (GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.MOVE, ref robotCmd)
                                && RobotMotorMove(robotCmd, placePos.motorPos))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PalletPlacePosPlaceDown;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletPlacePosPlaceDown:
                    {
                        this.msgChs = string.Format("机器人到放料位[{0}-{1}行-{2}列]下降", GetRobotStationName(placePos.station), placePos.row, placePos.col);
                        this.msgEng = string.Format("Robot goto place pos[{0}-{1}row-{2}col] down", (placePos.station), placePos.row, placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(MotorUMove(placePos.motorPos) && CheckStationSafe(placePos, RobotOrder.DOWN) && FingerCheck(placePos.finger, !placePos.fingerClose))
                        {
                            if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.DOWN, ref robotCmd)
                                && RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PalletPlacePosFingerAction;
                                //SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletPlacePosFingerAction:
                    {
                        this.msgChs = string.Format("放料抓手打开[{0}-{1}行-{2}列]", GetRobotStationName(placePos.station), placePos.row, placePos.col);
                        this.msgEng = string.Format("Finger open[{0}-{1}row-{2}col]", (placePos.station), placePos.row, placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerClose(placePos.finger, placePos.fingerClose))
                        {
                            int pltIndex = placePos.station - OnloadRobotStation.PalletStation_0;
                            switch(placePos.finger)
                            {
                                case ModDef.Finger_0:
                                    {
                                        if (BatteryStatus.Fake != this.Battery[(int)ModDef.Finger_0].Type)
                                        {
                                            TotalData.OnloadCount++;
                                        }
                                        SaveBatBindPltData(placePos.row, placePos.col, this.Battery[(int)ModDef.Finger_0], pltIndex, this.Pallet[pltIndex].Code);
                                        this.Pallet[pltIndex].Battery[placePos.row, placePos.col].Copy(this.Battery[(int)ModDef.Finger_0]);
                                        this.Battery[(int)ModDef.Finger_0].Release();
                                        break;
                                    }
                                case ModDef.Finger_1:
                                    {
                                        if(BatteryStatus.Fake != this.Battery[(int)ModDef.Finger_0].Type)
                                        {
                                            TotalData.OnloadCount++;
                                        }
                                        SaveBatBindPltData(placePos.row + 1, placePos.col, this.Battery[(int)ModDef.Finger_1], pltIndex, this.Pallet[pltIndex].Code);
                                        this.Pallet[pltIndex].Battery[placePos.row + 1, placePos.col].Copy(this.Battery[(int)ModDef.Finger_1]);
                                        this.Battery[(int)ModDef.Finger_1].Release();
                                        break;
                                    }
                                case ModDef.Finger_ALL:
                                    {
                                        for(ModDef i = ModDef.Finger_0; i < placePos.finger; i++)
                                        {
                                            if(BatteryStatus.Fake != this.Battery[(int)i].Type)
                                            {
                                                TotalData.OnloadCount++;
                                            }
                                            SaveBatBindPltData(placePos.row + (int)i, placePos.col, this.Battery[(int)i], pltIndex, this.Pallet[pltIndex].Code);
                                            this.Pallet[pltIndex].Battery[placePos.row + (int)i, placePos.col].Copy(this.Battery[(int)i]);
                                            this.Battery[(int)i].Release();
                                        }
                                        TotalData.WriteTotalData();
                                        break;
                                    }
                                default:
                                    return;
                            }
                            switch(this.Pallet[pltIndex].State)
                            {
                                case PalletStatus.Detect:
                                    {
                                        this.Pallet[pltIndex].State = PalletStatus.WaitResult;
                                        break;
                                    }
                                case PalletStatus.ReputFake:
                                    {
                                        this.Pallet[pltIndex].State = PalletStatus.Rebaking;
                                        break;
                                    }
                            }
                            this.nextAutoStep = AutoSteps.Auto_PalletPlacePosPlaceUp;
                            SaveRunData(SaveType.AutoStep | SaveType.Battery | SaveType.Pallet, pltIndex);
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletPlacePosPlaceUp:
                    {
                        this.msgChs = string.Format("机器人到放料位[{0}-{1}行-{2}列]上升", GetRobotStationName(placePos.station), placePos.row, placePos.col);
                        this.msgEng = string.Format("Robot goto place pos[{0}-{1}row-{2}col] up", (placePos.station), placePos.row, placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.UP, ref robotCmd)
                            && RobotMove(robotCmd, true))
                        {
                            this.nextAutoStep = AutoSteps.Auto_PalletPlacePosCheckFinger;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletPlacePosCheckFinger:
                    {
                        this.msgChs = string.Format("夹具位[{0}-{1}行-{2}列]放料后检查抓手", GetRobotStationName(placePos.station), placePos.row, placePos.col);
                        this.msgEng = string.Format("Place pos[{0}-{1}row-{2}col] check finger", (placePos.station), placePos.row, placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerCheck(placePos.finger, placePos.fingerClose))
                        {
                            this.nextAutoStep = AutoSteps.Auto_MesUpdataCount;
                        }
                        break;
                    }
                case AutoSteps.Auto_MesUpdataCount:
                    {
                        this.msgChs = string.Format("放料后上传MES计数信息");
                        this.msgEng = string.Format("Updata MES input num");
                        CurMsgStr(this.msgChs, this.msgEng);
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                #endregion

                #region // 暂存：可取可放，主要看抓手操作
                case AutoSteps.Auto_CalcBufferPos:
                    {
                        CurMsgStr("计算暂存放料位", "Calc place bufffer pos");
                        this.nextAutoStep = AutoSteps.Auto_BufferPosSetEvent;
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                case AutoSteps.Auto_BufferPosSetEvent:
                    {
                        this.msgChs = string.Format("机器人移动到暂存放料位[{0}-{1}行-{2}列]前发送信号", GetRobotStationName(placePos.station), placePos.row, placePos.col);
                        this.msgEng = string.Format("Set event before robot goto place bufffer pos[{0}-{1}row-{2}col]", (placePos.station), placePos.row, placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        {
                            this.nextAutoStep = AutoSteps.Auto_BufferPosMove;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_BufferPosMove:
                    {
                        this.msgChs = string.Format("机器人移动到暂存放料位[{0}-{1}行-{2}列]", GetRobotStationName(placePos.station), placePos.row, placePos.col);
                        this.msgEng = string.Format("Robot goto place bufffer pos[{0}-{1}row-{2}col]", (placePos.station), placePos.row, placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(CheckStationSafe(placePos, RobotOrder.MOVE))
                        {
                            if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.MOVE, ref robotCmd)
                                && RobotMotorMove(robotCmd, placePos.motorPos))
                            {
                                this.nextAutoStep = AutoSteps.Auto_BufferPosDown;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_BufferPosDown:
                    {
                        this.msgChs = string.Format("机器人到暂存放料位[{0}-{1}行-{2}列]下降", GetRobotStationName(placePos.station), placePos.row, placePos.col);
                        this.msgEng = string.Format("Robot goto place bufffer pos[{0}-{1}row-{2}col] down", (placePos.station), placePos.row, placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(MotorUMove(placePos.motorPos) && CheckStationSafe(placePos, RobotOrder.DOWN) 
                            && FingerCheck(placePos.finger, !placePos.fingerClose) && FingerClose(placePos.finger, !placePos.fingerClose))
                        {
                            if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.DOWN, ref robotCmd)
                                && RobotMove(robotCmd, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_BufferPosFingerAction;
                                //SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_BufferPosFingerAction:
                    {
                        this.msgChs = string.Format("放料抓手打开[{0}-{1}行-{2}列]", GetRobotStationName(placePos.station), placePos.row, placePos.col);
                        this.msgEng = string.Format("Finger open[{0}-{1}row-{2}col]", (placePos.station), placePos.row, placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerClose(placePos.finger, placePos.fingerClose))
                        {
                            switch(placePos.finger)
                            {
                                case ModDef.Finger_0:
                                    {
                                        int bufIdx = placePos.row + (int)ModDef.Finger_ALL - 1;
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
                                        int bufIdx = placePos.row + (int)ModDef.Finger_ALL;
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
                            this.nextAutoStep = AutoSteps.Auto_BufferPosUp;
                            SaveRunData(SaveType.AutoStep | SaveType.Battery);
                        }
                        break;
                    }
                case AutoSteps.Auto_BufferPosUp:
                    {
                        this.msgChs = string.Format("机器人到放暂存位[{0}-{1}行-{2}列]上升", GetRobotStationName(placePos.station), placePos.row, placePos.col);
                        this.msgEng = string.Format("Robot goto place buffer pos[{0}-{1}row-{2}col] up", (placePos.station), placePos.row, placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.UP, ref robotCmd)
                            && RobotMove(robotCmd, true))
                        {
                            this.nextAutoStep = AutoSteps.Auto_BufferPosCheckFinger;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_BufferPosCheckFinger:
                    {
                        this.msgChs = string.Format("暂存位[{0}-{1}行-{2}列]放料后检查抓手", GetRobotStationName(placePos.station), placePos.row, placePos.col);
                        this.msgEng = string.Format("Place pos[{0}-{1}row-{2}col] check finger", (placePos.station), placePos.row, placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerCheck(placePos.finger, placePos.fingerClose))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                #endregion

                #region // 放：NG线
                case AutoSteps.Auto_CalcNGLinePlacePos:
                {
                    CurMsgStr("计算NG输出线放料位" , "Calc place NG pos");
                    this.nextAutoStep=AutoSteps.Auto_NGLinePosSetEvent;
                    SaveRunData(SaveType.AutoStep|SaveType.Variables);
                    break;
                }
                case AutoSteps.Auto_NGLinePosSetEvent:
                    {
                        this.msgChs = string.Format("机器人移动到NG放料位[{0}-{1}行-{2}列]前发送信号", GetRobotStationName(placePos.station), placePos.row, placePos.col);
                        this.msgEng = string.Format("Set event before robot goto place NG pos[{0}-{1}row-{2}col]", (placePos.station), placePos.row, placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(EventStatus.Require == GetEvent(this.placeNGRun, EventList.OnloadNGPlaceBattery))
                        {
                            if (SetEvent(this.placeNGRun, EventList.OnloadNGPlaceBattery, EventStatus.Response))
                            {
                                this.nextAutoStep = AutoSteps.Auto_NGLinePosPlaceMove;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_NGLinePosPlaceMove:
                    {
                        this.msgChs = string.Format("机器人移动到NG放料位[{0}-{1}行-{2}列]", GetRobotStationName(placePos.station), placePos.row, placePos.col);
                        this.msgEng = string.Format("Robot goto place NG pos[{0}-{1}row-{2}col]", (placePos.station), placePos.row, placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(CheckStationSafe(placePos, RobotOrder.MOVE))
                        {
                            if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.MOVE, ref robotCmd)
                                && RobotMotorMove(robotCmd, placePos.motorPos))
                            {
                                this.nextAutoStep = AutoSteps.Auto_NGLinePosPlaceDown;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_NGLinePosPlaceDown:
                    {
                        this.msgChs = string.Format("机器人到NG放料位[{0}-{1}行-{2}列]下降", GetRobotStationName(placePos.station), placePos.row, placePos.col);
                        this.msgEng = string.Format("Robot goto place NG pos[{0}-{1}row-{2}col] down", (placePos.station), placePos.row, placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        EventStatus state = GetEvent(this.placeNGRun, EventList.OnloadNGPlaceBattery);
                        if ((EventStatus.Ready == state) || (EventStatus.Start == state))
                        {
                            if(MotorUMove(placePos.motorPos) && CheckStationSafe(placePos, RobotOrder.DOWN) && FingerCheck(placePos.finger, !placePos.fingerClose))
                            {
                                SetEvent(this.placeNGRun, EventList.OnloadNGPlaceBattery, EventStatus.Start);
                                if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.DOWN, ref robotCmd)
                                    && RobotMove(robotCmd, true))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_NGLinePosPlaceAction;
                                    //SaveRunData(SaveType.AutoStep);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_NGLinePosPlaceAction:
                    {
                        this.msgChs = string.Format("放料抓手打开[{0}-{1}行-{2}列]", GetRobotStationName(placePos.station), placePos.row, placePos.col);
                        this.msgEng = string.Format("Finger open[{0}-{1}row-{2}col]", (placePos.station), placePos.row, placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerClose(placePos.finger, placePos.fingerClose))
                        {
                            RunProcessOnloadNG run = this.placeNGRun;
                            if(null != run)
                            {
                                switch(placePos.finger)
                                {
                                    case ModDef.Finger_0:
                                        {
                                            run.Battery[placePos.row].Copy(this.Battery[(int)ModDef.Finger_0]);
                                            this.Battery[(int)ModDef.Finger_0].Release();
                                            break;
                                        }
                                    case ModDef.Finger_1:
                                        {
                                            run.Battery[placePos.row + 1].Copy(this.Battery[(int)ModDef.Finger_1]);
                                            this.Battery[(int)ModDef.Finger_1].Release();
                                            break;
                                        }
                                    case ModDef.Finger_ALL:
                                        {
                                            run.Battery[placePos.row].Copy(this.Battery[(int)ModDef.Finger_0]);
                                            this.Battery[(int)ModDef.Finger_0].Release();
                                            run.Battery[placePos.row + 1].Copy(this.Battery[(int)ModDef.Finger_1]);
                                            this.Battery[(int)ModDef.Finger_1].Release();
                                            break;
                                        }
                                    default:
                                        return;
                                }
                                run.SaveRunData(SaveType.Battery);
                            }
                            this.nextAutoStep = AutoSteps.Auto_NGLinePosPlaceUp;
                            SaveRunData(SaveType.AutoStep | SaveType.Battery);
                        }
                        break;
                    }
                case AutoSteps.Auto_NGLinePosPlaceUp:
                    {
                        this.msgChs = string.Format("机器人到NG放料位[{0}-{1}行-{2}列]上升", GetRobotStationName(placePos.station), placePos.row, placePos.col);
                        this.msgEng = string.Format("Robot goto place NG pos[{0}-{1}row-{2}col] up", (placePos.station), placePos.row, placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(GetRobotCmd(placePos.station, placePos.row, placePos.col, robotSpeed, RobotOrder.UP, ref robotCmd)
                            && RobotMove(robotCmd, true))
                        {
                            this.nextAutoStep = AutoSteps.Auto_NGLinePosCheckFinger;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_NGLinePosCheckFinger:
                    {
                        this.msgChs = string.Format("夹具位[{0}-{1}行-{2}列]放料后检查抓手", GetRobotStationName(placePos.station), placePos.row, placePos.col);
                        this.msgEng = string.Format("Place pos[{0}-{1}row-{2}col] check finger", (placePos.station), placePos.row, placePos.col);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if(FingerCheck(placePos.finger, placePos.fingerClose))
                        {
                            EventStatus state = GetEvent(this.placeNGRun, EventList.OnloadNGPlaceBattery);
                            if((EventStatus.Ready == state) || (EventStatus.Start == state))
                            {
                                SetEvent(this.placeNGRun, EventList.OnloadNGPlaceBattery, EventStatus.Finished);
                            }
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
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                default:
                    {
                        Trace.Assert(false, "RunProcessOnloadRobot.AutoOperation/no this run step");
                        break;
                    }
            }
        }
        
        #endregion

        #region // 运行数据读写

        public override void InitRunData()
        {
            this.placePallet = -1;
            this.pickPos.Release();
            this.placePos.Release();
            if(null == this.robotCmd)
            {
                this.robotCmd = new int[(int)RobotCmdFormat.End];
            }
            this.robotCmd.Initialize();
            if (null == this.robotAutoAction)
            {
                this.robotAutoAction = new RobotActionInfo();
            }
            this.robotAutoAction.Release();
            if(null == this.robotDebugAction)
            {
                this.robotDebugAction = new RobotActionInfo();
            }
            this.robotDebugAction.Release();
            if (null == this.robotClient)
            {
                this.robotClient = new RobotClient();
            }
            if (null == this.barcodeScan)
            {
                this.barcodeScan = new BarcodeScan();
            }
            this.avoidEvent = EventList.Invalid;
            this.robotNeedEStop = true;

            base.InitRunData();
        }

        public override void LoadRunData()
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);

            this.placePallet = iniStream.ReadInt(section, "placePallet", this.placePallet);

            key = string.Format("pickPos.station");
            this.pickPos.station = (OnloadRobotStation)iniStream.ReadInt(section, key, (int)this.pickPos.station);
            key = string.Format("pickPos.row");
            this.pickPos.row = iniStream.ReadInt(section, key, this.pickPos.row);
            key = string.Format("pickPos.col");
            this.pickPos.col = iniStream.ReadInt(section, key, this.pickPos.col);
            key = string.Format("pickPos.finger");
            this.pickPos.finger = (ModDef)iniStream.ReadInt(section, key, (int)this.pickPos.finger);
            key = string.Format("pickPos.fingerClose");
            this.pickPos.fingerClose = iniStream.ReadBool(section, key, this.pickPos.fingerClose);
            key = string.Format("pickPos.motorPos");
            this.pickPos.motorPos = (MotorPosition)iniStream.ReadInt(section, key, (int)this.pickPos.motorPos);

            key = string.Format("placePos.station");
            this.placePos.station = (OnloadRobotStation)iniStream.ReadInt(section, key, (int)this.placePos.station);
            key = string.Format("placePos.row");
            this.placePos.row = iniStream.ReadInt(section, key, this.placePos.row);
            key = string.Format("placePos.col");
            this.placePos.col = iniStream.ReadInt(section, key, this.placePos.col);
            key = string.Format("placePos.finger");
            this.placePos.finger = (ModDef)iniStream.ReadInt(section, key, (int)this.placePos.finger);
            key = string.Format("placePos.fingerClose");
            this.placePos.fingerClose = iniStream.ReadBool(section, key, this.placePos.fingerClose);
            key = string.Format("placePos.motorPos");
            this.placePos.motorPos = (MotorPosition)iniStream.ReadInt(section, key, (int)this.placePos.motorPos);

            for(int i = 0; i < this.robotCmd.Length; i++)
            {
                key = string.Format("robotCmd[{0}]", i);
                this.robotCmd[i] = iniStream.ReadInt(section, key, this.robotCmd[i]);
            }
            this.avoidEvent = (EventList)iniStream.ReadInt(section, "avoidEvent", (int)this.avoidEvent);

            base.LoadRunData();
        }

        public override void SaveRunData(SaveType saveType, int index = -1)
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);

            if(SaveType.Variables == (SaveType.Variables & saveType))
            {
                iniStream.WriteInt(section, "placePallet", this.placePallet);

                string[] posName = new string[] { "pickPos", "placePos" };
                PickPlacePos[] pos = new PickPlacePos[] { pickPos, placePos };
                for (int i = 0; i < pos.Length; i++)
			    {
                    key = string.Format("{0}.station", posName[i]);
                    iniStream.WriteInt(section, key, (int)pos[i].station);
                    key = string.Format("{0}.row", posName[i]);
                    iniStream.WriteInt(section, key, pos[i].row);
                    key = string.Format("{0}.col", posName[i]);
                    iniStream.WriteInt(section, key, pos[i].col);
                    key = string.Format("{0}.finger", posName[i]);
                    iniStream.WriteInt(section, key, (int)pos[i].finger);
                    key = string.Format("{0}.fingerClose", posName[i]);
                    iniStream.WriteBool(section, key, pos[i].fingerClose);
                    key = string.Format("{0}.motorPos", posName[i]);
                    iniStream.WriteInt(section, key, (int)pos[i].motorPos);
                }
                iniStream.WriteInt(section, "avoidEvent", (int)this.avoidEvent);
            }
            if(SaveType.Robot == (SaveType.Robot & saveType))
            {
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
        #endregion

        #region // 模组配置及参数

        /// <summary>
        /// 读取模组配置
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public override bool InitializeConfig(string module)
        {
            if (!base.InitializeConfig(module))
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

            return true;
        }

        /// <summary>
        /// 初始化通用组参数 + 模组参数
        /// </summary>
        protected override void InitParameter()
        {
            this.placeFakeRow = 1;
            this.placeFakeCol = 1;
            this.firstFakePlt = true;
            this.placeFakePltMode = false;
            this.placeNomalPltMode = false;
            this.placeNGPallet = false;
            this.tranNGPalletLimit = true;
            this.detectFakeBat = false;
            this.robotEnable = false;
            this.robotSpeed = 10;
            this.RobotLowSpeed = 10;
            this.robotDelay = 60;
            this.robotIP = "";
            this.robotPort = 0;
            this.scanPalletEnable = true;
            if (null == this.PalletPosEnable)
            {
                this.PalletPosEnable = new bool[(int)ModuleMaxPallet.OnloadRobot];
            }
            //this.PalletPosEnable.Initialize();
            this.scanEnable = true;
            this.scanCmd = "Start";
            this.scanLinefeed = true;
            this.barcodeScanIP = string.Empty;
            this.barcodeScanCom = -1;
            this.barcodeScanPort = 0;
            this.codeLength = -1;
            this.codeType = string.Empty;
            this.scanNGType = "ERROR";
            this.scanMaxCount = 1;
            this.RobotRunning = false;
            this.scanFakeBatEnable = true;
            this.waitFakeDelay = 120;
            this.stepDelayTime = DateTime.Now;

            base.InitParameter();
        }

        /// <summary>
        /// 读取通用组参数 + 模组参数
        /// </summary>
        /// <returns></returns>
        public override bool ReadParameter()
        {
            this.placeFakeRow = 1;
            this.placeFakeCol = ReadIntParameter(this.RunModule, "placeFakeCol", this.placeFakeCol);
            this.firstFakePlt = ReadBoolParameter(this.RunModule, "firstFakePlt", this.firstFakePlt);
            this.placeFakePltMode = ReadBoolParameter(this.RunModule, "placeFakePltMode", this.placeFakePltMode);
            this.placeNomalPltMode = ReadBoolParameter(this.RunModule, "placeNomalPltMode", this.placeNomalPltMode);
            this.placeNGPallet = ReadBoolParameter(this.RunModule, "placeNGPallet", this.placeNGPallet);
            this.tranNGPalletLimit = ReadBoolParameter(this.RunModule, "tranNGPalletLimit", this.tranNGPalletLimit);
            this.detectFakeBat = ReadBoolParameter(this.RunModule, "detectFakeBat", this.detectFakeBat);
            this.robotEnable = ReadBoolParameter(this.RunModule, "robotEnable", this.robotEnable);
            this.robotSpeed = ReadIntParameter(this.RunModule, "robotSpeed", this.robotSpeed);
            this.RobotLowSpeed = ReadIntParameter(this.RunModule, "RobotLowSpeed", this.RobotLowSpeed);
            this.robotDelay = ReadIntParameter(this.RunModule, "robotDelay", this.robotDelay);
            this.robotIP = ReadStringParameter(this.RunModule, "robotIP", this.robotIP);
            this.robotPort = ReadIntParameter(this.RunModule, "robotPort", this.robotPort);
            this.scanPalletEnable = ReadBoolParameter(this.RunModule, "scanPalletEnable", this.scanPalletEnable);
            this.scanFakeBatEnable = ReadBoolParameter(this.RunModule, "scanFakeBatEnable", this.scanPalletEnable);

            for(int i = 0; i < (int)ModuleMaxPallet.OnloadRobot; i++)
            {
                this.PalletPosEnable[i] = ReadBoolParameter(this.RunModule, ("PalletPosEnable" + i), this.PalletPosEnable[i]);
            }
            this.scanEnable = ReadBoolParameter(this.RunModule, "scanEnable", this.scanEnable);
            this.scanCmd = ReadStringParameter(this.RunModule, "scanCmd", this.scanCmd);
            this.scanLinefeed = ReadBoolParameter(this.RunModule, "scanLinefeed", this.scanLinefeed);
            this.barcodeScanIP = ReadStringParameter(this.RunModule, "barcodeScanIP", this.barcodeScanIP);
            this.barcodeScanCom = ReadIntParameter(this.RunModule, "barcodeScanCom", this.barcodeScanCom);
            this.barcodeScanPort = ReadIntParameter(this.RunModule, "barcodeScanPort", this.barcodeScanPort);
            this.codeLength = ReadIntParameter(this.RunModule, "codeLength", this.codeLength);
            this.codeType = ReadStringParameter(this.RunModule, "codeType", this.codeType);
            this.codeTypeArray = this.codeType.Split((new char[] { ',' }), StringSplitOptions.RemoveEmptyEntries);
            this.scanNGType = ReadStringParameter(this.RunModule, "scanNGType", this.scanNGType);
            this.scanMaxCount = ReadIntParameter(this.RunModule, "scanMaxCount", this.scanMaxCount);

            return base.ReadParameter();
        }

        /// <summary>
        /// 读取本模组的相关模组
        /// </summary>
        public override void ReadRelatedModule()
        {
            // 取电池模组
            this.pickBatRun = MachineCtrl.GetInstance().GetModule(RunID.OnloadLine) as RunProcessOnloadLine;
            // 假电池线
            this.pickFakeRun = MachineCtrl.GetInstance().GetModule(RunID.OnloadFake) as RunProcessOnloadFake;
            // NG输出线
            this.placeNGRun = MachineCtrl.GetInstance().GetModule(RunID.OnloadNG) as RunProcessOnloadNG;
        }

        #endregion

        #region // IO及电机

        /// <summary>
        /// 初始化IO及电机
        /// </summary>
        protected override void InitModuleIOMotor()
        {
            int maxPlt = (int)ModuleMaxPallet.OnloadRobot;
            this.IPalletKeepFlatLeft = new int[maxPlt];
            this.IPalletKeepFlatRight = new int[maxPlt];
            this.IPalletInposCheck = new int[maxPlt];
            this.IPalletHasCheck = new int[maxPlt];
            for(int i = 0; i < maxPlt; i++)
            {
                this.IPalletKeepFlatLeft[i] = AddInput("IPalletKeepFlatLeft" + i);
                this.IPalletKeepFlatRight[i] = AddInput("IPalletKeepFlatRight" + i);
                this.IPalletInposCheck[i] = AddInput("IPalletInposCheck" + i);
                this.IPalletHasCheck[i] = AddInput("IPalletHasCheck" + i);
            }
            int maxFinger = (int)ModDef.Finger_ALL;
            this.IFingerOpen = new int[maxFinger];
            this.IFingerClose = new int[maxFinger];
            this.IFingerCheck = new int[maxFinger];
            this.IBufferCheck = new int[maxFinger];
            for(int i = 0; i < maxFinger; i++)
            {
                this.IFingerOpen[i] = AddInput("IFingerOpen" + i);
                this.IFingerClose[i] = AddInput("IFingerClose" + i);
                this.IFingerCheck[i] = AddInput("IFingerCheck" + i);
            }
            this.IFingerDelay = AddInput("IFingerDelay");
            for(int i = 0; i < maxFinger; i++)
            {
                this.IBufferCheck[i] = AddInput("IBufferCheck" + i);
            }
            this.IRobotRunning = AddInput("IRobotRunning");

            this.OPalletAlarm = new int[maxPlt];
            for(int i = 0; i < maxPlt; i++)
            {
                this.OPalletAlarm[i] = AddOutput("OPalletAlarm" + i);
            }
            this.OFingerOpen = new int[maxFinger];
            this.OFingerClose = new int[maxFinger];
            for(int i = 0; i < maxFinger; i++)
            {
                this.OFingerOpen[i] = AddOutput("OFingerOpen" + i);
                this.OFingerClose[i] = AddOutput("OFingerClose" + i);
            }
            this.ORobotEStop = AddOutput("ORobotEStop");

            this.MotorU = AddMotor("MotorU");
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
            if (pltIdx < 0 || pltIdx >= (int)ModuleMaxPallet.OnloadRobot)
            {
                return false;
            }
            if (!InputState(IPalletHasCheck[pltIdx], hasPlt)
                || !InputState(IPalletInposCheck[pltIdx], hasPlt)
                || !InputState(IPalletKeepFlatLeft[pltIdx], hasPlt)
                || !InputState(IPalletKeepFlatRight[pltIdx], hasPlt))
            {
                if (alarm)
                {
                    CheckInputState(IPalletHasCheck[pltIdx], hasPlt);
                    CheckInputState(IPalletInposCheck[pltIdx], hasPlt);
                    CheckInputState(IPalletKeepFlatLeft[pltIdx], hasPlt);
                    CheckInputState(IPalletKeepFlatRight[pltIdx], hasPlt);
                    OutputAction(OPalletAlarm[pltIdx], true);
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// 调宽电机的移动
        /// </summary>
        /// <param name="motorLoc"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private bool MotorUMove(MotorPosition motorLoc, float offset = 0)
        {
            if(this.MotorU < 0)
            {
                return true;
            }
            return MotorMove(this.MotorU, (int)motorLoc, offset);
        }
        
        #endregion

        #region // 取放料计算

        /// <summary>
        /// 夹具需要扫码
        /// </summary>
        /// <param name="pPick"></param>
        /// <returns></returns>
        private bool PltNeedScanCode(ref PickPlacePos pPick)
        {
            if(scanPalletEnable)
            {
                // 有电池不能扫码
                for(ModDef i = ModDef.Finger_0; i < ModDef.Finger_ALL; i++)
                {
                    if(FingerBat(i).Type > BatteryStatus.Invalid)
                    {
                        return false;
                    }
                }
                for(int i = 0; i < this.Pallet.Length; i++)
                {
                    if(this.PalletPosEnable[i] && (PalletStatus.OK == this.Pallet[i].State) 
                        && ("" == this.Pallet[i].Code) && this.Pallet[i].IsEmpty())
                    {
                        pPick.SetData((OnloadRobotStation)((int)OnloadRobotStation.ScanPalletCode_0 + i), 0, 0, ModDef.Finger_ALL, false, MotorPosition.Onload_ScanPalletPos);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 夹具需要回炉假电池
        /// </summary>
        /// <returns></returns>
        private bool PltNeedReFake()
        {
            for(int i = 0; i < this.Pallet.Length; i++)
            {
                if(this.PalletPosEnable[i] && (PalletStatus.ReputFake == this.Pallet[i].State) && this.Pallet[i].HasFake())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 夹具需要上电池
        /// </summary>
        /// <param name="pltPos"></param>
        /// <returns></returns>
        private bool PltNeedBat(ref int pltPos)
        {
            if((pltPos < 0) || (PalletStatus.Invalid == this.Pallet[pltPos].State) || this.Pallet[pltPos].IsFull())
            {
                for(int i = 0; i < this.Pallet.Length; i++)
                {
                    if(this.PalletPosEnable[i] && (PalletStatus.OK == this.Pallet[i].State) 
                        && (PalletStage.Invalid == this.Pallet[i].Stage) && !this.Pallet[i].IsFull())
                    {
                        if(placeFakePltMode)
                        {
                            this.Pallet[i].NeedFake = true;
                        }
                        if(placeNomalPltMode)
                        {
                            this.Pallet[i].NeedFake = false;
                        }
                        if (!placeFakePltMode && !placeNomalPltMode)
                        {
                            this.Pallet[i].NeedFake = CalcNeedFakeBatteryPlt(i);
                        }
                        pltPos = i;
                        SaveRunData(SaveType.Pallet, i);
                        return true;
                    }
                }
            }
            else if((pltPos > -1) && this.PalletPosEnable[pltPos]
                && (PalletStatus.OK == this.Pallet[pltPos].State)
                && (PalletStage.Invalid == this.Pallet[pltPos].Stage) 
                && !this.Pallet[pltPos].IsFull())
            {
                return true;
            }
            pltPos = -1;
            return false;
        }

        /// <summary>
        /// 夹具需要取待测假电池
        /// </summary>
        /// <param name="curPickPos"></param>
        /// <returns></returns>
        private bool PltNeedPickDetectFake(ref PickPlacePos curPickPos)
        {
            int fakeRow, fakeCol;
            fakeRow = fakeCol = -1;
            for(int i = 0; i < (int)ModuleMaxPallet.OnloadRobot; i++)
            {
                if((PalletStatus.Detect == this.Pallet[i].State) && this.Pallet[i].GetFakePos(ref fakeRow, ref fakeCol))
                {
                    if (0 == (fakeRow % 2))
                    {
                        curPickPos.SetData((OnloadRobotStation.PalletStation_0 + i), fakeRow, fakeCol, ModDef.Finger_0, true, MotorPosition.Onload_PalletPos);
                    }
                    else
                    {
                        curPickPos.SetData((OnloadRobotStation.PalletStation_0 + i), (fakeRow - 1), fakeCol, ModDef.Finger_1, true, MotorPosition.Onload_PalletPos);
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 计算是否需要上假电池夹具
        /// </summary>
        /// <returns></returns>
        private bool CalcNeedFakeBatteryPlt(int curPltPos)
        {
            int fakePlt, nomalPlt, placePltIdx;
            fakePlt = nomalPlt = placePltIdx = 0;
            RunID id = RunID.Invalid;
            Pallet[] plt = null;
            bool checkAll = true;
            MachineCtrl mc = MachineCtrl.GetInstance();
            
            #region // 判断是否全检
            for(id = RunID.DryOven0; id < RunID.DryOvenALL; id++)
            {
                // 模组非使能 || 模组非运行中
                if(!mc.GetModuleEnable(id) || !mc.GetModuleRunning(id))
                {
                    continue;
                }
                plt = mc.GetModulePallet(id);
                for(int rowIdx = 0; rowIdx < (int)OvenRowCol.MaxRow; rowIdx++)
                {
                    if(mc.GetDryingOvenCavityEnable(id, rowIdx)
                        && !mc.GetDryingOvenCavityPressure(id, rowIdx)
                        && !mc.GetDryingOvenCavityTransfer(id, rowIdx)
                        && (CavityStatus.Normal == mc.GetDryingOvenCavityState(id, rowIdx)))
                    {
                        if (0 != (mc.GetDryingOvenCavityHeartCycle(id, rowIdx) % mc.GetDryingOvenCavitySamplingCycle(id, rowIdx)))
                        {
                            checkAll = false;
                            break;
                        }
                    }
                }
                if (!checkAll)
                {
                    break;
                }
            }
            #endregion

            #region // 全检
            if(checkAll)
            {
                // 获取干燥炉中需要假电池夹具数量
                for(id = RunID.DryOven0; id < RunID.DryOvenALL; id++)
                {
                    // 模组非使能 || 模组非运行中
                    if(!mc.GetModuleEnable(id) || !mc.GetModuleRunning(id))
                    {
                        continue;
                    }
                    plt = mc.GetModulePallet(id);
                    for(int pltIdx = 0; pltIdx < (int)ModuleMaxPallet.DryingOven; pltIdx++)
                    {
                        if(mc.GetDryingOvenCavityEnable(id, (pltIdx / 2))
                            && !mc.GetDryingOvenCavityPressure(id, (pltIdx / 2))
                            && !mc.GetDryingOvenCavityTransfer(id, (pltIdx / 2))
                            && (CavityStatus.Normal == mc.GetDryingOvenCavityState(id, (pltIdx / 2))))
                        {
                            if((PalletStatus.OK == plt[pltIdx].State) && !plt[pltIdx].HasFake() && plt[pltIdx].IsFull())
                            {
                                nomalPlt++;
                            }
                            else if((PalletStatus.OK == plt[pltIdx].State) && plt[pltIdx].HasFake() && plt[pltIdx].IsFull())
                            {
                                fakePlt++;
                            }
                        }
                    }
                }
                // 计算调度机器人插料架夹具
                id = RunID.Transfer;
                plt = mc.GetModulePallet(id);
                if ((null != plt) && (plt.Length > 0))
                {
                    for(int pltIdx = 0; pltIdx < plt.Length; pltIdx++)
                    {
                        if((PalletStatus.OK == plt[pltIdx].State) && !plt[pltIdx].IsEmpty() && !plt[pltIdx].NeedFake)
                        {
                            nomalPlt++;
                        }
                        else if((PalletStatus.OK == plt[pltIdx].State) && !plt[pltIdx].IsEmpty() && plt[pltIdx].NeedFake)
                        {
                            fakePlt++;
                        }
                    }
                }
                // 计算当前在上料已有的假电池夹具
                id = RunID.OnloadRobot;
                plt = mc.GetModulePallet(id);
                for(int pltIdx = 0; pltIdx < (int)ModuleMaxPallet.OnloadRobot; pltIdx++)
                {
                    if((pltIdx != curPltPos) && this.PalletPosEnable[pltIdx])
                    {
                        if((PalletStatus.OK == plt[pltIdx].State) && !plt[pltIdx].IsEmpty() && !plt[pltIdx].NeedFake)
                        {
                            nomalPlt++;
                        }
                        else if((PalletStatus.OK == plt[pltIdx].State) && !plt[pltIdx].IsEmpty() && plt[pltIdx].NeedFake)
                        {
                            fakePlt++;
                        }
                    }
                }
                return this.firstFakePlt ? (nomalPlt >= fakePlt) : (nomalPlt > fakePlt);
            }
            #endregion

            #region // 抽检
            else
            {
                // 获取干燥炉中需要假电池夹具数量
                for(id = RunID.DryOven0; id < RunID.DryOvenALL; id++)
                {
                    // 模组非使能 || 模组非运行中
                    if(!mc.GetModuleEnable(id) || !mc.GetModuleRunning(id))
                    {
                        continue;
                    }
                    plt = mc.GetModulePallet(id);
                    for(int rowIdx = 0; rowIdx < (int)OvenRowCol.MaxRow; rowIdx++)
                    {
                        if(mc.GetDryingOvenCavityEnable(id, rowIdx)
                            && !mc.GetDryingOvenCavityPressure(id, rowIdx)
                            && !mc.GetDryingOvenCavityTransfer(id, rowIdx)
                            && (CavityStatus.Normal == mc.GetDryingOvenCavityState(id, rowIdx))
                            && (0 == (mc.GetDryingOvenCavityHeartCycle(id, rowIdx) % mc.GetDryingOvenCavitySamplingCycle(id, rowIdx))))
                        {
                            int pltIdx = rowIdx * (int)OvenRowCol.MaxCol;
                            // 统计需要假电池夹具
                            if(EventStatus.Require == mc.GetModuleEvent(id, EventList.DryOvenPlaceOnlOKFakeFullPallet, ref placePltIdx))
                            {
                                if(((PalletStatus.OK == plt[pltIdx].State) && !plt[pltIdx].IsEmpty() && !plt[pltIdx].HasFake() && !plt[pltIdx + 1].HasFake())
                                    || ((PalletStatus.OK == plt[pltIdx + 1].State) && !plt[pltIdx + 1].IsEmpty() && !plt[pltIdx + 1].HasFake() && !plt[pltIdx].HasFake()))
                                {
                                    fakePlt++;
                                }
                            }
                            else
                            {
                                for(int colIdx = 0; colIdx < (int)OvenRowCol.MaxCol; colIdx++)
                                {
                                    // 统计已有的正常夹具
                                    if((PalletStatus.OK == plt[pltIdx + colIdx].State) && !plt[pltIdx + colIdx].IsEmpty() && !plt[pltIdx + colIdx].HasFake())
                                    {
                                        nomalPlt++;
                                    }
                                    // 统计已有的假电池夹具
                                    // 统计已有的假电池夹具需要的正常夹具
                                    else if((PalletStatus.OK == plt[pltIdx + colIdx].State) && !plt[pltIdx + colIdx].IsEmpty() && plt[pltIdx + colIdx].HasFake())
                                    {
                                        fakePlt++;
                                        nomalPlt++;
                                    }
                                }
                                // 统计需要的正常夹具：不在抽检次数
                                if(((PalletStatus.OK == plt[pltIdx].State) && !plt[pltIdx].IsEmpty() && (PalletStatus.Invalid == plt[pltIdx + 1].State))
                                    || ((PalletStatus.OK == plt[pltIdx + 1].State) && !plt[pltIdx + 1].IsEmpty() && (PalletStatus.Invalid == plt[pltIdx].State)))
                                {
                                    nomalPlt++;
                                }
                            }
                        }
                    }
                }
                // 计算当前在上料已有的假电池夹具
                id = RunID.OnloadRobot;
                plt = mc.GetModulePallet(id);
                for(int pltIdx = 0; pltIdx < (int)ModuleMaxPallet.OnloadRobot; pltIdx++)
                {
                    if((pltIdx != curPltPos) && this.PalletPosEnable[pltIdx])
                    {
                        if((PalletStatus.OK == plt[pltIdx].State) && !plt[pltIdx].IsEmpty() && plt[pltIdx].NeedFake)
                        {
                            fakePlt--;
                        }
                        else if((PalletStatus.OK == plt[pltIdx].State) && !plt[pltIdx].IsEmpty() && !plt[pltIdx].NeedFake)
                        {
                            nomalPlt--;
                        }
                    }
                }
                return (fakePlt > 0) && (fakePlt > nomalPlt);
            }
            #endregion
            //return this.firstFakePlt ? (fakePlt >= nomalPlt) : (nomalPlt >= fakePlt);
        }

        /// <summary>
        /// 检查是否是上假电池行列
        /// </summary>
        /// <param name="placePlt"></param>
        /// <returns></returns>
        private bool IsOnloadFakeRowCol(int placePlt)
        {
            if(placePlt > -1 && this.Pallet[placePlt].NeedFake)
            {
                for(int row = 0; row < this.Pallet[placePlt].MaxRow; row++)
                {
                    for(int col = 0; col < this.Pallet[placePlt].MaxCol; col++)
                    {
                        if((row == (placeFakeRow - 1)) && (col == (placeFakeCol - 1))
                            && (BatteryStatus.Invalid == this.Pallet[placePlt].Battery[row, col].Type))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 计算放NG电池位置
        /// </summary>
        /// <param name="placePlt"></param>
        /// <param name="curPickPos"></param>
        /// <returns></returns>
        private bool CalcPlaceNgOutPos(ref PickPlacePos curPickPos) {
            RunProcessOnloadNG run = this.placeNGRun;
            //计算夹爪上的电池位置
            ModDef Finger_count =ModDef.Finger_ALL;
            GetFingerBatCount(ref Finger_count);
            if(null==run)
                return false;
            if(EventStatus.Require!=GetEvent(run , EventList.OnloadNGPlaceBattery))
                return false;
            int placeRow = -1;
            if(CalcPlaceNgOutPosFromFinger(run,Finger_count , ref placeRow)) {
                curPickPos.SetData(OnloadRobotStation.OnloadNGOutput , placeRow , 0 , ModDef.Finger_ALL , true , MotorPosition.Onload_FakePos);
                return true;
            }
            return false;
        }
        /// <summary>
        /// 通过夹爪 计算可以存放的NG输出线体的位置
        /// </summary>
        /// <param name="run"></param>
        /// <param name="Finger_count"></param>
        /// <param name="placeRow"></param>
        /// <returns></returns>
        private bool CalcPlaceNgOutPosFromFinger(RunProcessOnloadNG run ,ModDef Finger_count ,ref int placeRow) {
            int count =0;
             switch(Finger_count) {
                case ModDef.Finger_0://  0 = Finger_0 1 = Finger_1  此时夹爪0 不能放在第3位  夹爪1不能放在第0位置
                case ModDef.Finger_1:
                    count=1;
                    break;
                case ModDef.Finger_ALL:
                    count=2;
                    break;
                  default:
                    break;
            }
            bool ret =run.GetPlacePos(count , ref placeRow);
            if(ret) {
                if(ModDef.Finger_0==Finger_count) {
                    if(placeRow>=(int)ModDef.Finger_ALL-1)
                        return false;
                }
                if(ModDef.Finger_1==Finger_count) {
                    if(placeRow<=1)
                        return false;
                    placeRow-=1; //夹爪位置
                }
            }
            return ret;
        }




        /// <summary>
        /// 计算取假电池位置
        /// </summary>
        /// <param name="placePlt"></param>
        /// <param name="curPickPos"></param>
        /// <returns></returns>
        private bool CalcPickFakePos(int placePlt, ref PickPlacePos curPickPos)
        {
            RunProcessOnloadFake run = this.pickFakeRun;
            if(null != run)
            {
                if(EventStatus.Require == GetEvent(run, EventList.OnloadFakePickBattery))
                {
                    int pickRow = -1;
                    if(run.GetPickFakeRow(ref pickRow))
                    {
                        curPickPos.SetData(OnloadRobotStation.OnloadFake, pickRow, 0, ModDef.Finger_0, true, MotorPosition.Onload_FakePos);
                        return true;
                    }
                }
                else if((DateTime.Now - this.stepDelayTime).TotalMinutes > 5)
                {
                    this.stepDelayTime = DateTime.Now;
                }
                else if((DateTime.Now - this.stepDelayTime).TotalSeconds > this.waitFakeDelay)
                {
                    ShowMessageBox((int)MsgID.WaitFakeBatTimeout, "假电池线体无假电池", "请上假电池", MessageType.MsgWarning);
                    this.stepDelayTime = DateTime.Now;
                    SetEvent(run, EventList.OnloadFakePickBattery, EventStatus.Cancel);
                }
            }
            return false;
        }

        /// <summary>
        /// 获取夹具当前需要电池的行列
        /// </summary>
        /// <param name="pltRow"></param>
        /// <param name="pltCol"></param>
        /// <returns></returns>
        private bool GetPalletCurPlaceRowCol(int pltIndex, ref int pltRow, ref int pltCol)
        {
            for(int col = 0; col < this.Pallet[pltIndex].MaxCol; col++)
            {
                for(int row = 0; row < this.Pallet[pltIndex].MaxRow; row++)
                {
                    if((BatteryStatus.Invalid == this.Pallet[pltIndex].Battery[row, col].Type))
                    {
                        pltRow = row;
                        pltCol = col;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 计算取料位置
        /// </summary>
        /// <param name="placePlt"></param>
        /// <param name="curPickPos"></param>
        /// <returns></returns>
        private bool CalcPickPos(int placePlt, ref PickPlacePos curPickPos)
        {
            if(placePlt < 0 || placePlt >= this.Pallet.Length)
            {
                return false;
            }
            int pltRow, pltCol;
            pltRow = pltCol = -1;
            if(GetPalletCurPlaceRowCol(placePlt, ref pltRow, ref pltCol))
            {
                // 来料取
                if((null != this.pickBatRun) && (EventStatus.Require == GetEvent(this.pickBatRun, EventList.OnloadLinePickBattery)))
                {
                    if(this.pickBatRun.RecvPosIsFull())
                    {
                        curPickPos.SetData(OnloadRobotStation.OnloadLine, 0, 0, ModDef.Finger_ALL, true, MotorPosition.Onload_LinePickPos);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 计算NG夹具转盘
        /// </summary>
        /// <param name="placePlt"></param>
        /// <param name="curPickPos"></param>
        /// <returns></returns>
        private bool CalcPickNGPalletPos(int placePlt, ref PickPlacePos curPickPos)
        {
            if(placePlt < 0 || placePlt >= this.Pallet.Length)
            {
                return false;
            }
            // NG转盘：假电池状态相同
            int ngPlt = (int)ModuleMaxPallet.OnloadRobot - 1;
            if((PalletStatus.NG == this.Pallet[ngPlt].State) 
                && (!this.tranNGPalletLimit || (this.Pallet[placePlt].NeedFake == this.Pallet[ngPlt].NeedFake)) // 是否限制同类型夹具转盘
                && !this.Pallet[ngPlt].IsEmpty())
            {
                int pltRow, pltCol;
                pltRow = pltCol = -1;
                if(GetPalletCurPlaceRowCol(placePlt, ref pltRow, ref pltCol))
                {
                    for(int col = 0; col < this.Pallet[ngPlt].MaxCol; col++)
                    {
                        for(int row = 0; row < this.Pallet[ngPlt].MaxRow; row++)
                        {
                            if (BatteryStatus.FakeTag == this.Pallet[ngPlt].Battery[row, col].Type)
                            {
                                this.Pallet[ngPlt].Battery[row, col].Release();
                            }
                            if(BatteryStatus.Invalid != this.Pallet[ngPlt].Battery[row, col].Type)
                            {
                                // 首行首列时，则放夹具必须为空
                                if(((0 == row) || (1 == row)) && (0 == col) && !this.Pallet[placePlt].IsEmpty())
                                {
                                    return false;
                                }
                                else if ((pltCol * this.Pallet[ngPlt].MaxRow + pltRow) > (col * this.Pallet[ngPlt].MaxRow + row))
                                {
                                    return false;
                                }
                                // 夹具为偶数行，需要每次放两个
                                if(false && (0 == row))
                                {
                                    curPickPos.SetData(OnloadRobotStation.PalletStation_0 + ngPlt, row, col, ModDef.Finger_0, true, MotorPosition.Onload_PalletPos);
                                    return true;
                                }
                                else if((this.Pallet[ngPlt].MaxRow - 1) == row)
                                {
                                    curPickPos.SetData(OnloadRobotStation.PalletStation_0 + ngPlt, row - 1, col, ModDef.Finger_1, true, MotorPosition.Onload_PalletPos);
                                    return true;
                                }
                                else if(BatteryStatus.Invalid != this.Pallet[ngPlt].Battery[row + 1, col].Type)
                                {
                                    curPickPos.SetData(OnloadRobotStation.PalletStation_0 + ngPlt, row, col, ModDef.Finger_ALL, true, MotorPosition.Onload_PalletPos);
                                    return true;
                                }
                                else if(BatteryStatus.Invalid == this.Pallet[ngPlt].Battery[row + 1, col].Type)
                                {
                                    curPickPos.SetData(OnloadRobotStation.PalletStation_0 + ngPlt, row, col, ModDef.Finger_0, true, MotorPosition.Onload_PalletPos);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }


        /// <summary>
        /// 计算放NG电池位置
        /// </summary>
        /// <param name="placeplt"></param>
        /// <param name="curPlacePos"></param>
        /// <returns></returns>
        private bool CalcPlaceNGOutPos(ref PickPlacePos curPlacePos) {
            RunProcessOnloadNG run = placeNGRun;
            if((null!=run)&&(EventStatus.Require==GetEvent(run , EventList.OnloadNGPlaceBattery)/*||EventStatus.Cancel== GetEvent(run , EventList.OnloadNGPlaceBattery)*/)) {
                // 抓手0的假电池，抓手1非空
                if((BatteryStatus.OK<FingerBat(ModDef.Finger_0).Type) /*&& (BatteryStatus.Invalid != FingerBat(ModuleData.Finger_1).Type)*/) {
                    for(int i = 0 ; i<run.Battery.Length-1 ; i++) {
                        if((BatteryStatus.Invalid==run.Battery[i].Type)&&run.PlacePosInposIsSafe(i , false)
                            &&(BatteryStatus.Invalid==run.Battery[i+1].Type)&&run.PlacePosInposIsSafe(i+1 , false)) {
                            curPlacePos.SetData(OnloadRobotStation.OnloadNGOutput , i , 0 , ModDef.Finger_0 , false , MotorPosition.Onload_NGPos);
                            return true;
                        }
                    }
                }
                // 抓手0的假电池，抓手1空
                else if((BatteryStatus.OK<FingerBat(ModDef.Finger_0).Type)&&(BatteryStatus.Invalid==FingerBat(ModDef.Finger_1).Type)) {
                    for(int i = 0 ; i<run.Battery.Length-1 ; i++) {
                        if((BatteryStatus.Invalid==run.Battery[i].Type)&&run.PlacePosInposIsSafe(i , false)) {
                            curPlacePos.SetData(OnloadRobotStation.OnloadNGOutput , i , 0 , ModDef.Finger_0 , false , MotorPosition.Onload_NGPos);
                            return true;
                        }
                    }
                }
                // 抓手1的假电池，抓手0非空
                else if((BatteryStatus.OK<FingerBat(ModDef.Finger_1).Type) /*&& (BatteryStatus.Invalid != FingerBat(ModuleData.Finger_0).Type)*/) {
                    for(int i = 0 ; i<run.Battery.Length-1 ; i++) {
                        if((BatteryStatus.Invalid==run.Battery[i].Type)&&run.PlacePosInposIsSafe(i , false)
                            &&(BatteryStatus.Invalid==run.Battery[i+1].Type)&&run.PlacePosInposIsSafe(i+1 , false)) {
                            curPlacePos.SetData(OnloadRobotStation.OnloadNGOutput , i , 0 , ModDef.Finger_1 , false , MotorPosition.Onload_NGPos);
                            return true;
                        }
                    }
                }
                // 抓手1的NG，抓手0空
                else if((BatteryStatus.OK<FingerBat(ModDef.Finger_1).Type)&&(BatteryStatus.Invalid==FingerBat(ModDef.Finger_0).Type)) {
                    // 抓手1从1起
                    for(int i = 1 ; i<run.Battery.Length ; i++) {
                        if((BatteryStatus.Invalid==run.Battery[i].Type)&&run.PlacePosInposIsSafe(i , false)) {
                            curPlacePos.SetData(OnloadRobotStation.OnloadNGOutput , i-1 , 0 , ModDef.Finger_1 , false , MotorPosition.Onload_NGPos);
                            return true;
                        }
                    }
                }
                // 无法放，置取消
               // SetEvent(run , EventList.OnloadNGPlaceBattery , EventStatus.Cancel);
            } else {
                // 无法放，置取消
               // SetEvent(run , EventList.OnloadNGPlaceBattery , EventStatus.Cancel);
            }
            return false;
        }


        /// <summary>
        /// 计算放NG电池位置
        /// </summary>
        /// <param name="placeplt"></param>
        /// <param name="curPlacePos"></param>
        /// <returns></returns>
        private bool CalcPlaceNGPos(ref PickPlacePos curPlacePos) {
            RunProcessOnloadNG run = placeNGRun;
            if((null!=run)&&(EventStatus.Require==GetEvent(run , EventList.OnloadNGPlaceBattery)/*||EventStatus.Cancel== GetEvent(run , EventList.OnloadNGPlaceBattery)*/)) {
                // 2个NG
                if((BatteryStatus.NG==FingerBat(ModDef.Finger_0).Type)&&(BatteryStatus.NG==FingerBat(ModDef.Finger_1).Type)) {
                    for(int i = 0 ; i<run.Battery.Length-1 ; i++) {
                        if((BatteryStatus.Invalid==run.Battery[i].Type)&&run.PlacePosInposIsSafe(i , false)
                            &&(BatteryStatus.Invalid==run.Battery[i+1].Type)&&run.PlacePosInposIsSafe(i+1 , false)) {
                            curPlacePos.SetData(OnloadRobotStation.OnloadNGOutput , i , 0 , ModDef.Finger_ALL , false , MotorPosition.Onload_NGPos);
                            return true;
                        }
                    }
                }
                // 抓手0的NG，抓手1非空
                else if((BatteryStatus.NG==FingerBat(ModDef.Finger_0).Type) /*&& (BatteryStatus.Invalid != FingerBat(ModuleData.Finger_1).Type)*/) {
                    for(int i = 0 ; i<run.Battery.Length-1 ; i++) {
                        if((BatteryStatus.Invalid==run.Battery[i].Type)&&run.PlacePosInposIsSafe(i , false)
                            &&(BatteryStatus.Invalid==run.Battery[i+1].Type)&&run.PlacePosInposIsSafe(i+1 , false)) {
                            curPlacePos.SetData(OnloadRobotStation.OnloadNGOutput , i , 0 , ModDef.Finger_0 , false , MotorPosition.Onload_NGPos);
                            return true;
                        }
                    }
                }
                // 抓手0的NG，抓手1空
                else if((BatteryStatus.NG==FingerBat(ModDef.Finger_0).Type)&&(BatteryStatus.Invalid==FingerBat(ModDef.Finger_1).Type)) {
                    for(int i = 0 ; i<run.Battery.Length-1 ; i++) {
                        if((BatteryStatus.Invalid==run.Battery[i].Type)&&run.PlacePosInposIsSafe(i , false)) {
                            curPlacePos.SetData(OnloadRobotStation.OnloadNGOutput , i , 0 , ModDef.Finger_0 , false , MotorPosition.Onload_NGPos);
                            return true;
                        }
                    }
                }
                // 抓手1的NG，抓手0非空
                else if((BatteryStatus.NG==FingerBat(ModDef.Finger_1).Type) /*&& (BatteryStatus.Invalid != FingerBat(ModuleData.Finger_0).Type)*/) {
                    for(int i = 0 ; i<run.Battery.Length-1 ; i++) {
                        if((BatteryStatus.Invalid==run.Battery[i].Type)&&run.PlacePosInposIsSafe(i , false)
                            &&(BatteryStatus.Invalid==run.Battery[i+1].Type)&&run.PlacePosInposIsSafe(i+1 , false)) {
                            curPlacePos.SetData(OnloadRobotStation.OnloadNGOutput , i , 0 , ModDef.Finger_1 , false , MotorPosition.Onload_NGPos);
                            return true;
                        }
                    }
                }
                // 抓手1的NG，抓手0空
                else if((BatteryStatus.NG==FingerBat(ModDef.Finger_1).Type)&&(BatteryStatus.Invalid==FingerBat(ModDef.Finger_0).Type)) {
                    // 抓手1从1起
                    for(int i = 1 ; i<run.Battery.Length ; i++) {
                        if((BatteryStatus.Invalid==run.Battery[i].Type)&&run.PlacePosInposIsSafe(i , false)) {
                            curPlacePos.SetData(OnloadRobotStation.OnloadNGOutput , i-1 , 0 , ModDef.Finger_1 , false , MotorPosition.Onload_NGPos);
                            return true;
                        }
                    }
                }
                // 无法放，置取消
                //  SetEvent(run , EventList.OnloadNGPlaceBattery , EventStatus.Cancel);
                // 无法放置，报警
                ShowMessageBox((int)MsgID.NGPlaceFull, "NG下料位满", "请先取走NG下料位电池", MessageType.MsgAlarm);
            } else {
                // 无法放，置取消
                //  SetEvent(run, EventList.OnloadNGPlaceBattery, EventStatus.Cancel);
                // 无法放置，报警
                ShowMessageBox((int)MsgID.NGPlaceFull, "NG下料位满", "请先取走NG下料位电池", MessageType.MsgAlarm);
            }
            return false;
        }



        /// <summary>
        /// 计算放回炉假电池位置
        /// </summary>
        /// <param name="curPlacePos"></param>
        /// <returns></returns>
        private bool CalcPlaceReFakePos(ref PickPlacePos curPlacePos)
        {
            for(int i = 0; i < this.Pallet.Length; i++)
            {
                int fakeRow, fakeCol;
                fakeRow = fakeCol = -1;
                if(this.PalletPosEnable[i] && (PalletStatus.ReputFake == this.Pallet[i].State) && this.Pallet[i].GetFakePos(ref fakeRow, ref fakeCol))
                {
                    if ((BatteryStatus.Fake == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_1).Type))
                    {
                        curPlacePos.SetData((OnloadRobotStation.PalletStation_0 + i), fakeRow, fakeCol, ModDef.Finger_0, false, MotorPosition.Onload_PalletPos);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 计算放待测假电池位置
        /// </summary>
        /// <param name="curPlacePos"></param>
        /// <returns></returns>
        private bool CalcPlaceDetectFakePos(ref PickPlacePos curPlacePos)
        {
            RunProcessOnloadNG run = placeNGRun;
            if((null != run) && (EventStatus.Require == GetEvent(run, EventList.OnloadNGPlaceBattery)))
            {
                // 抓手0的待测假电池，抓手1空
                if((BatteryStatus.Detect == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_1).Type))
                {
                    if(BatteryStatus.Invalid == run.Battery[0].Type)
                    {
                        curPlacePos.SetData(OnloadRobotStation.OnloadNGOutput, 0, 0, ModDef.Finger_0, false, MotorPosition.Onload_NGPos);
                        return true;
                    }
                }
                // 抓手0空，抓手1的待测假电池
                else if((BatteryStatus.Invalid == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Detect == FingerBat(ModDef.Finger_1).Type))
                {
                    if(BatteryStatus.Invalid == run.Battery[1].Type)
                    {
                        curPlacePos.SetData(OnloadRobotStation.OnloadNGOutput, 0, 0, ModDef.Finger_1, false, MotorPosition.Onload_NGPos);
                        return true;
                    }
                }
                // 无法放，置取消
                SetEvent(run, EventList.OnloadNGPlaceBattery, EventStatus.Cancel);
            }
            return false;
        }

        /// <summary>
        /// 计算抓手及暂存配对位置
        /// </summary>
        /// <param name="placeplt"></param>
        /// <param name="curPos"></param>
        /// <returns></returns>
        private bool CalcFingerBufferMatchesPos(int placeplt, ref PickPlacePos curPos)
        {
            // 有NG电池，优先放OK电池至暂存
            if(BatteryStatus.NG == FingerBat(ModDef.Finger_0).Type)
            {
                if((BatteryStatus.OK == FingerBat(ModDef.Finger_1).Type) && (BatteryStatus.Invalid == BufferBat(ModDef.Buffer_0).Type))
                {
                    curPos.SetData(OnloadRobotStation.BufferStation, 0, 0, ModDef.Finger_1, false, MotorPosition.Onload_BufferPos);
                    return true;
                }
                return false;
            }
            else if(BatteryStatus.NG == FingerBat(ModDef.Finger_1).Type)
            {
                if((BatteryStatus.OK == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == BufferBat(ModDef.Buffer_1).Type))
                {
                    curPos.SetData(OnloadRobotStation.BufferStation, 2, 0, ModDef.Finger_0, false, MotorPosition.Onload_BufferPos);
                    return true;
                }
                return false;
            }
            // 放OK电池
            int pltRow, pltCol;
            pltRow = pltCol = -1;
            if((placeplt > -1) && GetPalletCurPlaceRowCol(placeplt, ref pltRow, ref pltCol))
            {
                #region // 上料清尾料

                // 上料清尾料：将所有电池都放入夹具
                if (MachineCtrl.GetInstance().OnloadClear)
                {
                    // 暂存0，1都有
                    if((BatteryStatus.Invalid != BufferBat(ModDef.Buffer_0).Type) && (BatteryStatus.Invalid != BufferBat(ModDef.Buffer_1).Type))
                    {
                        // 抓手0，1都没有，取暂存
                        if((BatteryStatus.Invalid == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_1).Type))
                        {
                            curPos.SetData(OnloadRobotStation.BufferStation, 1, 0, ModDef.Finger_ALL, true, MotorPosition.Onload_BufferPos);
                            return true;
                        }
                        // 抓手0没有，取暂存
                        else if((BatteryStatus.Invalid == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid != FingerBat(ModDef.Finger_1).Type))
                        {
                            curPos.SetData(OnloadRobotStation.BufferStation, 2, 0, ModDef.Finger_0, true, MotorPosition.Onload_BufferPos);
                            return true;
                        }
                        // 抓手1没有，取暂存
                        else if((BatteryStatus.Invalid != FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_1).Type))
                        {
                            curPos.SetData(OnloadRobotStation.BufferStation, 0, 0, ModDef.Finger_1, true, MotorPosition.Onload_BufferPos);
                            return true;
                        }
                    }
                    // 暂存0有
                    else if((BatteryStatus.Invalid != BufferBat(ModDef.Buffer_0).Type) && (BatteryStatus.Invalid == BufferBat(ModDef.Buffer_1).Type))
                    {
                        // 抓手0没有，取暂存
                        if(BatteryStatus.Invalid == FingerBat(ModDef.Finger_0).Type)
                        {
                            curPos.SetData(OnloadRobotStation.BufferStation, 1, 0, ModDef.Finger_0, true, MotorPosition.Onload_BufferPos);
                            return true;
                        }
                        // 抓手1没有，取暂存
                        else if((BatteryStatus.Invalid != FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_1).Type))
                        {
                            curPos.SetData(OnloadRobotStation.BufferStation, 0, 0, ModDef.Finger_1, true, MotorPosition.Onload_BufferPos);
                            return true;
                        }
                    }
                    // 暂存1有
                    else if((BatteryStatus.Invalid == BufferBat(ModDef.Buffer_0).Type) && (BatteryStatus.Invalid != BufferBat(ModDef.Buffer_1).Type))
                    {
                        // 抓手0没有，取暂存
                        if(BatteryStatus.Invalid == FingerBat(ModDef.Finger_0).Type)
                        {
                            curPos.SetData(OnloadRobotStation.BufferStation, 2, 0, ModDef.Finger_0, true, MotorPosition.Onload_BufferPos);
                            return true;
                        }
                        // 抓手1没有，取暂存
                        else if((BatteryStatus.Invalid != FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_1).Type))
                        {
                            curPos.SetData(OnloadRobotStation.BufferStation, 1, 0, ModDef.Finger_1, true, MotorPosition.Onload_BufferPos);
                            return true;
                        }
                    }
                    return false;
                }
                #endregion

                #region // 夹具为偶数行，需要每次放两个，屏蔽：首行，放1个
                // 夹具为偶数行，需要每次放两个
                if(false && (0 == pltRow))
                {
                    // 抓手0,1都有，抓手1放入暂存
                    if((BatteryStatus.OK == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.OK == FingerBat(ModDef.Finger_1).Type))
                    {
                        // 暂存都没有
                        if((BatteryStatus.Invalid == BufferBat(ModDef.Buffer_0).Type) && (BatteryStatus.Invalid == BufferBat(ModDef.Buffer_1).Type))
                        {
                            curPos.SetData(OnloadRobotStation.BufferStation, 1, 0, ModDef.Finger_1, false, MotorPosition.Onload_BufferPos);
                            return true;
                        }
                    }
                    // 抓手1有，放入暂存
                    else if((BatteryStatus.Invalid == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.OK == FingerBat(ModDef.Finger_1).Type))
                    {
                        // 暂存0没有
                        if(BatteryStatus.Invalid == BufferBat(ModDef.Buffer_0).Type)
                        {
                            curPos.SetData(OnloadRobotStation.BufferStation, 0, 0, ModDef.Finger_1, false, MotorPosition.Onload_BufferPos);
                            return true;
                        }
                        // 暂存1没有
                        else if(BatteryStatus.Invalid == BufferBat(ModDef.Buffer_1).Type)
                        {
                            curPos.SetData(OnloadRobotStation.BufferStation, 1, 0, ModDef.Finger_1, false, MotorPosition.Onload_BufferPos);
                            return true;
                        }
                    }
                    // 抓手为空，暂存有，取暂存放入夹具
                    else if((BatteryStatus.Invalid == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_1).Type))
                    {
                        // 暂存1有
                        if(BatteryStatus.OK == BufferBat(ModDef.Buffer_1).Type)
                        {
                            curPos.SetData(OnloadRobotStation.BufferStation, 2, 0, ModDef.Finger_0, true, MotorPosition.Onload_BufferPos);
                            return true;
                        }
                        // 暂存0有
                        else if(BatteryStatus.OK == BufferBat(ModDef.Buffer_0).Type)
                        {
                            curPos.SetData(OnloadRobotStation.BufferStation, 1, 0, ModDef.Finger_0, true, MotorPosition.Onload_BufferPos);
                            return true;
                        }
                    }
                }
                #endregion

                #region // 非首行，放2个
                else
                {
                    // 假电池行列，抓手无假电池，暂存为空，放暂存
                    if (IsOnloadFakeRowCol(placeplt))
                    {
                        if(FingerCount() < 1)
                        {
                            return false;
                        }
                        if((BatteryStatus.Invalid == BufferBat(ModDef.Buffer_0).Type) && (BatteryStatus.Invalid == BufferBat(ModDef.Buffer_0).Type))
                        {
                            if((BatteryStatus.OK == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.OK == FingerBat(ModDef.Finger_1).Type))
                            {
                                curPos.SetData(OnloadRobotStation.BufferStation, 1, 0, ModDef.Finger_ALL, false, MotorPosition.Onload_BufferPos);
                                return true;
                            }
                            else if((BatteryStatus.OK == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_1).Type))
                            {
                                curPos.SetData(OnloadRobotStation.BufferStation, 1, 0, ModDef.Finger_0, false, MotorPosition.Onload_BufferPos);
                                return true;
                            }
                            else if((BatteryStatus.Invalid == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.OK == FingerBat(ModDef.Finger_1).Type))
                            {
                                curPos.SetData(OnloadRobotStation.BufferStation, 1, 0, ModDef.Finger_1, false, MotorPosition.Onload_BufferPos);
                                return true;
                            }
                        }
                    }
                    // 抓手为空，暂存满，取暂存放入夹具
                    if((BatteryStatus.Invalid == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_1).Type))
                    {
                        // 暂存01都有
                        if((BatteryStatus.OK == BufferBat(ModDef.Buffer_0).Type) && (BatteryStatus.OK == BufferBat(ModDef.Buffer_1).Type))
                        {
                            curPos.SetData(OnloadRobotStation.BufferStation, 1, 0, ModDef.Finger_ALL, true, MotorPosition.Onload_BufferPos);
                            return true;
                        }
                    }
                    // 抓手0有，暂存有-》抓手1取暂存
                    if((BatteryStatus.Invalid != FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_1).Type))
                    {
                        // 暂存0有
                        if(BatteryStatus.OK == BufferBat(ModDef.Buffer_0).Type)
                        {
                            curPos.SetData(OnloadRobotStation.BufferStation, 0, 0, ModDef.Finger_1, true, MotorPosition.Onload_BufferPos);
                            return true;
                        }
                        // 暂存1有
                        else if(BatteryStatus.OK == BufferBat(ModDef.Buffer_1).Type)
                        {
                            curPos.SetData(OnloadRobotStation.BufferStation, 1, 0, ModDef.Finger_1, true, MotorPosition.Onload_BufferPos);
                            return true;
                        }
                    }
                    // 抓手1有，暂存有-》抓手0取暂存
                    if((BatteryStatus.Invalid == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid != FingerBat(ModDef.Finger_1).Type))
                    {
                        // 暂存1有
                        if(BatteryStatus.OK == BufferBat(ModDef.Buffer_1).Type)
                        {
                            curPos.SetData(OnloadRobotStation.BufferStation, 2, 0, ModDef.Finger_0, true, MotorPosition.Onload_BufferPos);
                            return true;
                        }
                        // 暂存0有
                        else if(BatteryStatus.OK == BufferBat(ModDef.Buffer_0).Type)
                        {
                            curPos.SetData(OnloadRobotStation.BufferStation, 1, 0, ModDef.Finger_0, true, MotorPosition.Onload_BufferPos);
                            return true;
                        }
                    }
                    // 抓手有1个，暂存无-》放暂存
                    if ((BatteryStatus.Invalid == BufferBat(ModDef.Buffer_0).Type) && (BatteryStatus.Invalid == BufferBat(ModDef.Buffer_1).Type))
                    {
                        if((BatteryStatus.OK == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_1).Type))
                        {
                            curPos.SetData(OnloadRobotStation.BufferStation, 1, 0, ModDef.Finger_0, false, MotorPosition.Onload_BufferPos);
                            return true;
                        }
                        else if((BatteryStatus.Invalid == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.OK == FingerBat(ModDef.Finger_1).Type))
                        {
                            curPos.SetData(OnloadRobotStation.BufferStation, 1, 0, ModDef.Finger_1, false, MotorPosition.Onload_BufferPos);
                            return true;
                        }
                    }
                }
                #endregion
            }
            return false;
        }

        /// <summary>
        /// 计算夹具中放电池位置
        /// </summary>
        /// <param name="placePlt"></param>
        /// <param name="curPos"></param>
        /// <returns></returns>
        private bool CalcPlacePalletPos(int placePlt, ref PickPlacePos curPos)
        {
            if (placePlt < 0 || placePlt >= (int)ModuleMaxPallet.OnloadRobot)
            {
                return false;
            }
            OnloadRobotStation station = (OnloadRobotStation)((int)OnloadRobotStation.PalletStation_0 + placePlt);
            // 放假电池
            if(IsOnloadFakeRowCol(placePlt))
            {
                if ((BatteryStatus.Fake == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.OK == FingerBat(ModDef.Finger_1).Type))
                {
                    curPos.SetData(station, this.placeFakeRow - 1, this.placeFakeCol - 1, ModDef.Finger_ALL, false, MotorPosition.Onload_PalletPos);
                    return true;
                }
                else if((BatteryStatus.OK == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Fake == FingerBat(ModDef.Finger_1).Type))
                {
                    curPos.SetData(station, this.placeFakeRow - 1, this.placeFakeCol - 1, ModDef.Finger_ALL, false, MotorPosition.Onload_PalletPos);
                    return true;
                }
                return false;
            }
            // 放OK电池
            int pltRow, pltCol;
            pltRow = pltCol = -1;
            if(GetPalletCurPlaceRowCol(placePlt, ref pltRow, ref pltCol))
            {
                // 上料清尾料
                if (MachineCtrl.GetInstance().OnloadClear)
                {
                    // 抓手2个，放入夹具
                    if((BatteryStatus.OK == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.OK == FingerBat(ModDef.Finger_1).Type))
                    {
                        curPos.SetData(station, pltRow, pltCol, ModDef.Finger_ALL, false, MotorPosition.Onload_PalletPos);
                        return true;
                    }
                    // 抓手0有，放入夹具
                    else if((BatteryStatus.OK == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_1).Type))
                    {
                        curPos.SetData(station, pltRow, pltCol, ModDef.Finger_0, false, MotorPosition.Onload_PalletPos);
                        return true;
                    }
                    // 抓手1有，放入夹具
                    else if((BatteryStatus.Invalid == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.OK == FingerBat(ModDef.Finger_1).Type))
                    {
                        curPos.SetData(station, pltRow, pltCol, ModDef.Finger_1, false, MotorPosition.Onload_PalletPos);
                        return true;
                    }
                }

                // 夹具为偶数行，需要每次放两个
                // 首行，放1个
                if(false && (0 == pltRow))
                {
                    // 抓手0有，放入夹具
                    if((BatteryStatus.OK == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_1).Type))
                    {
                        curPos.SetData(station, pltRow, pltCol, ModDef.Finger_0, false, MotorPosition.Onload_PalletPos);
                        return true;
                    }
                }
                // 非首行，放2个
                else
                {
                    // 抓手2个，放入夹具
                    if((BatteryStatus.OK == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.OK == FingerBat(ModDef.Finger_1).Type))
                    {
                        curPos.SetData(station, pltRow, pltCol, ModDef.Finger_ALL, false, MotorPosition.Onload_PalletPos);
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion
        
        #region // 抓手及暂存

        private bool FingerClose(ModDef finger, bool close)
        {
            if(Def.IsNoHardware())
            {
                return true;
            }
            // 检查IO配置
            for(int i = 0; i < IFingerOpen.Length; i++)
            {
                if (((ModDef)i == finger) || (ModDef.Finger_ALL == finger))
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
                    if (!(WaitInputState(IFingerClose[i], close) && WaitInputState(IFingerOpen[i], !close)))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private Battery FingerBat(ModDef finger)
        {
            if(finger < ModDef.Finger_0 || finger >= ModDef.Finger_ALL)
            {
                return null;
            }
            return this.Battery[(int)finger];
        }
        
        /// <summary>
        /// 获取夹爪上电池计数
        /// </summary>
        /// <param name="finger"></param>
        /// <returns></returns>
        private void GetFingerBatCount(ref ModDef Finger_Count) {
            if(this.Battery[(int)ModDef.Finger_0].Type!=BatteryStatus.Invalid)
                Finger_Count=ModDef.Finger_0;
            if(this.Battery[(int)ModDef.Finger_1].Type!=BatteryStatus.Invalid)
                Finger_Count=ModDef.Finger_1;
            if(this.Battery[(int)ModDef.Finger_0].Type!=BatteryStatus.Invalid&&this.Battery[(int)ModDef.Finger_1].Type!=BatteryStatus.Invalid)
                Finger_Count=ModDef.Finger_ALL;
        }

        private bool FingerCheck(ModDef finger, bool hasBat, bool alarm = true)
        {
            if(Def.IsNoHardware() || this.DryRun)
            {
                return true;
            }

            for(int i = 0; i < IFingerCheck.Length; i++)
            {
                if(((ModDef)i == finger) || (ModDef.Finger_ALL == finger))
                {
                    if(!InputState(IFingerCheck[i], hasBat))
                    {
                        if (alarm)
                        {
                            CheckInputState(IFingerCheck[i], hasBat);
                        }
                        return false;
                    }
                }
            }
            return true;
        }

        private int FingerCount()
        {
            int count = 0;
            for(ModDef i = ModDef.Finger_0; i < ModDef.Finger_ALL; i++)
            {
                if(FingerBat(i).Type > BatteryStatus.Invalid)
                {
                    count++;
                }
            }
            return count;
        }

        private Battery BufferBat(ModDef buffer)
        {
            if (buffer < ModDef.Buffer_0 || buffer >= ModDef.Buffer_ALL)
            {
                return null;
            }
            return this.Battery[(int)buffer];
        }

        private bool BufferCheck(ModDef buffer, bool hasBat, bool alarm = true)
        {
            if(Def.IsNoHardware() || this.DryRun)
            {
                return true;
            }

            for(ModDef i = ModDef.Buffer_0; i < ModDef.Buffer_ALL; i++)
            {
                if((i == buffer) || (ModDef.Buffer_ALL == buffer))
                {
                    int idx = (int)i - (int)ModDef.Buffer_0;
                    if(!InputState(IBufferCheck[idx], hasBat))
                    {
                        if (alarm)
                        {
                            CheckInputState(IBufferCheck[idx], hasBat);
                        }
                        return false;
                    }
                }
            }
            return true;
        }

        private int BufferCount()
        {
            int count = 0;
            for(ModDef i = ModDef.Buffer_0; i < ModDef.Buffer_ALL; i++)
            {
                if (BufferBat(i).Type > BatteryStatus.Invalid)
                {
                    count++;
                }
            }
            return count;
        }

        #endregion

        #region // 机器人操作
        
        /// <summary>
        /// 获取机器人IP信息
        /// </summary>
        /// <returns></returns>
        public string RobotIPInfo()
        {
            return string.Format("{0}:{1}", this.robotIP, this.robotPort);
        }

        /// <summary>
        /// 获取机器人连接状态：true链接，false断开
        /// </summary>
        /// <returns>true链接，false断开</returns>
        public bool RobotIsConnect()
        {
            if (!robotEnable)
            {
                return true;
            }
            return this.robotClient.IsConnect();
        }

        /// <summary>
        /// 连接机器人
        /// </summary>
        /// <param name="connect">true链接，false断开</param>
        /// <returns></returns>
        public bool RobotConnect(bool connect = true)
        {
            if(!robotEnable)
            {
                return true;
            }
            if (connect)
            {
                if (!RobotIsConnect())
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

        /// <summary>
        /// 获取机器人命令
        /// </summary>
        /// <param name="station">工位</param>
        /// <param name="row">工位行</param>
        /// <param name="col">工位列</param>
        /// <param name="speed">速度</param>
        /// <param name="order">动作指令</param>
        /// <param name="rbtCmd">命令缓存</param>
        /// <returns></returns>
        public bool GetRobotCmd(OnloadRobotStation station, int row, int col, int speed, RobotOrder order, ref int[] rbtCmd)
        {
            rbtCmd[(int)RobotCmdFormat.Station] = (int)station;
            rbtCmd[(int)RobotCmdFormat.StationRow] = row + 1;
            rbtCmd[(int)RobotCmdFormat.StationCol] = col + 1;
            rbtCmd[(int)RobotCmdFormat.Speed] = speed;
            rbtCmd[(int)RobotCmdFormat.Order] = (int)order;
            rbtCmd[(int)RobotCmdFormat.Result] = (int)RobotOrder.END;

            // 工位行列非法
            if ((rbtCmd[(int)RobotCmdFormat.Station] < (int)OnloadRobotStation.InvalidStatioin)
                || (rbtCmd[(int)RobotCmdFormat.Station] >= (int)OnloadRobotStation.StationEnd)
                || (rbtCmd[(int)RobotCmdFormat.StationRow] < 0) 
                || (rbtCmd[(int)RobotCmdFormat.StationRow] >= MachineCtrl.GetInstance().PalletMaxRow)
                || (rbtCmd[(int)RobotCmdFormat.StationCol] < 0) 
                || (rbtCmd[(int)RobotCmdFormat.StationCol] > MachineCtrl.GetInstance().PalletMaxCol))
            {
                ShowMsgBox.ShowDialog(string.Format("{0},{1},{2},{3},{4},END\r\n工位行列非法，不能构造机器人指令", station, row, col, speed, order.ToString()), MessageType.MsgAlarm);
                return false;
            }

            MCState state = MachineCtrl.GetInstance().RunsCtrl.GetMCState();
            if ((MCState.MCInitializing == state) || (MCState.MCRunning == state))
            {
                this.robotAutoAction.SetData((int)station, row, col, order, GetRobotStationName(station));
                this.robotDebugAction.SetData((int)station, row, col, order, GetRobotStationName(station));
            }
            else
            {
                this.robotDebugAction.SetData((int)station, row, col, order, GetRobotStationName(station));
            }
            //SaveRunData(SaveType.Robot);
            return true;
        }

        /// <summary>
        /// 机器人回零
        /// </summary>
        /// <returns></returns>
        public bool RobotHome(OptMode mode = OptMode.Auto)
        {
            int[] cmd = new int[(int)RobotCmdFormat.End];
            if (GetRobotCmd(OnloadRobotStation.HomeStatioin, 0, 0, robotSpeed, RobotOrder.HOME, ref cmd))
            {
                return RobotMove(cmd, true, mode);
            }
            return false;
        }

        /// <summary>
        /// 机器人移动
        /// </summary>
        /// <param name="rbtCmd"></param>
        /// <param name="wait"></param>
        /// <returns></returns>
        public bool RobotMove(int[] rbtCmd, bool wait, OptMode mode = OptMode.Auto)
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
            return true;
        }

        /// <summary>
        /// 等待机器人运动完成
        /// </summary>
        /// <param name="rbtCmd">运动命令</param>
        /// <param name="startTime">开始时间</param>
        /// <returns></returns>
        public bool RobotMoveFinish(int[] rbtCmd, DateTime startTime)
        {
            if(!robotEnable)
            {
                return true;
            }
            string msg, dispose;
            int[] recvCmd = new int[(int)RobotCmdFormat.End];
            this.RobotRunning = true;
            while(true)
            {
                if (this.robotClient.GetReceiveResult(ref recvCmd))
                {
                    if ((rbtCmd[(int)RobotCmdFormat.Station] == recvCmd[(int)RobotCmdFormat.Station])
                        && (rbtCmd[(int)RobotCmdFormat.StationRow] == recvCmd[(int)RobotCmdFormat.StationRow])
                        && (rbtCmd[(int)RobotCmdFormat.StationCol] == recvCmd[(int)RobotCmdFormat.StationCol])
                        && (rbtCmd[(int)RobotCmdFormat.Order] == recvCmd[(int)RobotCmdFormat.Order])
                        && ((int)RobotOrder.FINISH == recvCmd[(int)RobotCmdFormat.Result]))
                    {
                        this.RobotRunning = false;
                        log.Debug("RobotMove finish");
                        return true;
                    }
                    if (((int)RobotOrder.INVALID == recvCmd[(int)RobotCmdFormat.Result])
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
                    dispose = string.Format("请检查机器人是否运行");
                    ShowMessageBox((int)MsgID.RbtMoveTimeout, msg, dispose, MessageType.MsgAlarm);
                    break;
                }
                if (!OutputState(this.ORobotEStop, false))
                {
                    this.RobotRunning = false;
                    msg = "机器人急停，退出等待机器人动作完成";
                    Def.WriteLog(this.RunName, msg);
                    break;
                }
                Sleep(1);
            }
            log.DebugFormat("RobotMove error, {0}", msg);
            return false;
        }

        /// <summary>
        /// 机器人电机同时移动且等待完成，仅对GO指令起作用：motorLoc为-1时电机不移动
        /// </summary>
        /// <param name="rbtCmd"></param>
        /// <param name="motorLoc"></param>
        /// <returns></returns>
	    public bool RobotMotorMove(int[] rbtCmd, MotorPosition motorLoc = MotorPosition.Invalid, OptMode mode = OptMode.Auto)
        {
            if(!robotEnable)
            {
                return true;
            }

            if (!RobotMove(rbtCmd, false, mode))
            {
                return false;
            }
            DateTime startTime = DateTime.Now;

            if (((int)RobotOrder.MOVE == rbtCmd[(int)RobotCmdFormat.Order])
                && (motorLoc > MotorPosition.Invalid))
            {
                MotorUMove(motorLoc);
            }

            return RobotMoveFinish(rbtCmd, startTime);
        }

        /// <summary>
        /// 初始化机器人工位
        /// </summary>
        public void InitRobotStation()
        {
            if (null == this.robotStationInfo)
            {
                this.robotStationInfo = new Dictionary<OnloadRobotStation, RobotFormula>();
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
                    this.robotStationInfo.Add((OnloadRobotStation)item.stationID, item);
                }
                for(OnloadRobotStation station = OnloadRobotStation.InvalidStatioin; station < OnloadRobotStation.StationEnd; station++)
                {
                    bool add = false;
                    RobotFormula rbtFormula = new RobotFormula();
                    string stationName = "";
                    int stationID = (int)station;
                    #region // 查找工位是否存在
                    switch(station)
                    {
                        case OnloadRobotStation.InvalidStatioin:
                            {
                                if(!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                    stationName = string.Format("{0}】闲置", stationID);
                                    rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, 1, 1);
                                }
                                break;
                            }
                        case OnloadRobotStation.HomeStatioin:
                            {
                                if(!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                    stationName = string.Format("{0}】回零位", stationID);
                                    rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, 1, 1);
                                }
                                break;
                            }
                        case OnloadRobotStation.OnloadLine:
                            {
                                if(!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                    stationName = string.Format("{0}】来料取料位", stationID);
                                    rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, 1, 1);
                                }
                                break;
                            }
                        case OnloadRobotStation.ScanPalletCode_0:
                        case OnloadRobotStation.ScanPalletCode_1:
                            {
                                if(!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                    stationName = string.Format("{0}】上料夹具{1}扫码", stationID, (stationID - (int)OnloadRobotStation.ScanPalletCode_0 + 1));
                                    rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, 1, 1);
                                }
                                break;
                            }
                        case OnloadRobotStation.PalletStation_0:
                        case OnloadRobotStation.PalletStation_1:
                            {
                                stationName = string.Format("{0}】上料夹具{1}", stationID, (stationID - (int)OnloadRobotStation.PalletStation_0 + 1));
                                int maxRow = MachineCtrl.GetInstance().PalletMaxRow - 1;
                                int maxCol = MachineCtrl.GetInstance().PalletMaxCol;
                                rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, maxRow, maxCol);
                                if(!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                }
                                else
                                {
                                    break;
                                    this.dbRecord.ModifyRobotStation(rbtFormula);
                                }
                                break;
                            }
                        case OnloadRobotStation.BufferStation:
                            {
                                if(!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                    stationName = string.Format("{0}】暂存工位", stationID);
                                    rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, 3, 1);
                                }
                                break;
                            }
                        case OnloadRobotStation.OnloadNGOutput:
                            {
                                if(!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                    stationName = string.Format("{0}】NG电池输出", stationID);
                                    rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, 3, 1);
                                }
                                break;
                            }
                        case OnloadRobotStation.OnloadFakeScan:
                            {
                                if(!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                    stationName = string.Format("{0}】假电池扫码", stationID);
                                    rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, 4, 1);
                                }
                                break;
                            }
                        case OnloadRobotStation.OnloadFake:
                            {
                                if(!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                    stationName = string.Format("{0}】假电池输入", stationID);
                                    rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, 4, 1);
                                }
                                break;
                            }
                        default:
                            break;
                    }
                    #endregion
                    if (add)
                    {
                        this.robotStationInfo.Add(station, rbtFormula);
                        this.dbRecord.AddRobotStation(rbtFormula);
                    }
                }
            }
        }

        /// <summary>
        /// 获取工位名称
        /// </summary>
        /// <param name="station"></param>
        /// <returns></returns>
        public string GetRobotStationName(OnloadRobotStation station)
        {
            string info = "";
            if(this.robotStationInfo.ContainsKey(station))
            {
                info = this.robotStationInfo[station].stationName;
            }
            return info;
        }

        /// <summary>
        /// 获取动作信息
        /// </summary>
        /// <param name="autoAction"></param>
        /// <returns></returns>
        public RobotActionInfo GetRobotActionInfo(bool autoAction)
        {
            return autoAction ? this.robotAutoAction : this.robotDebugAction;
        }

        /// <summary>
        /// 获取机器人是否在安全位
        /// </summary>
        /// <returns></returns>
        public bool RobotInSafePos()
        {
            if(((int)OnloadRobotStation.HomeStatioin == this.robotAutoAction.station)
                && (RobotOrder.HOME == this.robotAutoAction.order))
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
            // 自动在回零位置，手动在回零位置，则不判断
            if((RobotOrder.HOME == autoCmd.order) && (RobotOrder.HOME == debugCmd.order))
            {
                return true;
            }
            else if((autoCmd.station == debugCmd.station)
                && (autoCmd.row == debugCmd.row)
                && (autoCmd.col == debugCmd.col))
            {
                if ((autoCmd.order == debugCmd.order)
                    || ((RobotOrder.MOVE == autoCmd.order) && (RobotOrder.UP == debugCmd.order))
                    || ((RobotOrder.UP == autoCmd.order) && (RobotOrder.MOVE == debugCmd.order)))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 手动防呆
        /// </summary>
        /// <param name="nStation"></param>
        /// <param name="nRow"></param>
        /// <param name="nCol"></param>
        /// <param name="nOrder"></param>
        /// <returns></returns>
        public bool RobotManulAvoid(OnloadRobotStation station, int row, int col, RobotOrder order)
        {
            string msg = "";

            #region // 已有Ready || Start信号 || 调度执行取进或放进

            if (RobotOrder.HOME != order)
            {
                for(EventList i = EventList.OnloadPlaceEmptyPallet; i < EventList.OnloadPickPlaceEnd; i++)
                {
                    EventStatus state = GetEvent(this, i);
                    if((EventStatus.Ready == state) || (EventStatus.Start == state))
                    {
                        msg = string.Format("调度机器人已开始执行取放上料夹具事件，仅能操作上料机器人【{0}】", RobotDef.RobotOrderName[(int)RobotOrder.HOME]);
                        ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                        return false;
                    }
                }
            }
            RobotActionInfo rbtAction = MachineCtrl.GetInstance().GetRobotActionInfo(RunID.Transfer, false);
            if (null == rbtAction)
            {
                msg = string.Format("无法获取调度机器人当前动作，不能操作上料机器人\r\n在【其它调试】界面重连模组客户端后再操作");
                ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                return false;
            }
            if((int)TransferRobotStation.OnloadStation == rbtAction.station)
            {
                if (RobotOrder.MOVE != rbtAction.order)
                {
                    msg = string.Format("调度机器人已在上料位取放夹具，不能操作上料机器人\r\n在【机器人调试】界面将大机器人移动到当前工位移动位置后再操作");
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                    return false;
                }
            }
            MCState mcState = MachineCtrl.GetInstance().GetModuleMCState(RunID.Transfer);
            if((MCState.MCInitComplete != mcState) && (MCState.MCStopRun != mcState))
            {
                msg = string.Format("调度设备非【初始化完成】或【运行停止】状态，不能操作{0}", RobotDef.RobotIDName[(int)this.RobotID]);
                ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                return false;
            }

            #endregion

            #region // 判断动作指令
            switch(order)
            {
                case RobotOrder.HOME:
                    break;
                case RobotOrder.MOVE:
                    {
                        if (RobotOrder.DOWN == this.robotDebugAction.order)
                        {
                            msg = string.Format("{0}\r\n当前在<{1}-{2}行-{3}列-{4}>位置\r\n不能执行<{5}-{6}行-{7}列-{8}>操作", this.RunName, this.robotDebugAction.stationName
                                , this.robotDebugAction.row + 1, this.robotDebugAction.col + 1, RobotDef.RobotOrderName[(int)this.robotDebugAction.order]
                                , GetRobotStationName(station), row + 1, col + 1, RobotDef.RobotOrderName[(int)order]);
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            return false;
                        }
                        break;
                    }
                case RobotOrder.DOWN:
                    {
                        if (((int)station != this.robotDebugAction.station) || (row != this.robotDebugAction.row)
                             || (col != this.robotDebugAction.col) || (RobotOrder.DOWN == this.robotDebugAction.order))
                        {
                            msg = string.Format("{0}\r\n当前在<{1}-{2}行-{3}列-{4}>位置\r\n不能执行<{5}-{6}行-{7}列-{8}>操作", this.RunName, this.robotDebugAction.stationName
                                , this.robotDebugAction.row + 1, this.robotDebugAction.col + 1, RobotDef.RobotOrderName[(int)this.robotDebugAction.order]
                                , GetRobotStationName(station), row + 1, col + 1, RobotDef.RobotOrderName[(int)order]);
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            return false;
                        }
                        break;
                    }
                case RobotOrder.UP:
                    {
                        if(((int)station != this.robotDebugAction.station) || (row != this.robotDebugAction.row)
                             || (col != this.robotDebugAction.col))
                        {
                            msg = string.Format("{0}\r\n当前在<{1}-{2}行-{3}列-{4}>位置\r\n不能执行<{5}-{6}行-{7}列-{8}>操作", this.RunName, this.robotDebugAction.stationName
                                , this.robotDebugAction.row + 1, this.robotDebugAction.col + 1, RobotDef.RobotOrderName[(int)this.robotDebugAction.order]
                                , GetRobotStationName(station), row + 1, col + 1, RobotDef.RobotOrderName[(int)order]);
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            return false;
                        }
                        break;
                    }
                default:
                    ShowMsgBox.ShowDialog(order.ToString() + "非上料机器人动作，不能操作上料机器人", MessageType.MsgWarning);
                    return false;
                    break;
            }
            #endregion

            #region // 判断工位行列
            switch(station)
            {
                case OnloadRobotStation.HomeStatioin:
                    {
                        if(RobotOrder.HOME != order)
                        {
                            msg = string.Format("回零工位仅能执行回零指令！");
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            return false;
                        }
                        break;
                    }
                case OnloadRobotStation.OnloadLine:
                    {
                        if (RobotOrder.MOVE == order)
                        {
                            if (!MotorUMove(MotorPosition.Onload_LinePickPos))
                            {
                                return false;
                            }
                        }
                        else if (RobotOrder.DOWN == order)
                        {
                            if (!CheckMotorPos(this.MotorU, MotorPosition.Onload_LinePickPos))
                            {
                                return false;
                            }
                            if (null != this.pickBatRun)
                            {
                                if(!FingerCheck(ModDef.Finger_ALL, false, false))
                                {
                                    msg = string.Format("{0}抓手有电池，机器人不能在来料取料位下降！", this.RunName);
                                    ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                    return false;
                                }
                                if(!FingerClose(ModDef.Finger_ALL, false))
                                {
                                    msg = string.Format("{0}抓手非打开状态，机器人不能在来料取料位下降！", this.RunName);
                                    ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                    return false;
                                }
                                if(!this.pickBatRun.RecvPosSenserInpos(false))
                                {
                                    msg = string.Format("来料线取料位进入感应器检测非【ON】，机器人不能下降！");
                                    ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                    return false;
                                }
                            }
                        }
                        break;
                    }
                case OnloadRobotStation.ScanPalletCode_0:
                case OnloadRobotStation.ScanPalletCode_1:
                    {
                        if(RobotOrder.MOVE == order)
                        {
                            if(!MotorUMove(MotorPosition.Onload_ScanPalletPos))
                            {
                                return false;
                            }
                        }
                        else if(RobotOrder.DOWN == order)
                        {
                            if(!CheckMotorPos(this.MotorU, MotorPosition.Onload_ScanPalletPos))
                            {
                                return false;
                            }
                            if(!PalletKeepFlat(((int)station - (int)OnloadRobotStation.ScanPalletCode_0), true, true))
                            {
                                return false;
                            }
                            if(!FingerCheck(ModDef.Finger_ALL, false, false))
                            {
                                msg = string.Format("{0}抓手有电池，机器人不能在夹具扫码位下降！", this.RunName);
                                ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                return false;
                            }
                            if (!FingerClose(ModDef.Finger_ALL, false))
                            {
                                msg = string.Format("{0}抓手非打开状态，机器人不能在夹具扫码位下降！", this.RunName);
                                ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                return false;
                            }
                            for(int i = 0; i < this.IFingerOpen.Length; i++)
                            {
                                if(!InputState(IFingerOpen[i], true))
                                {
                                    msg = string.Format("机器人抓手打开感应器非【ON】，机器人不能下降！\r\n保证抓手无料且打开后再下降");
                                    ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                    return false;
                                }
                            }
                        }
                        break;
                    }
                case OnloadRobotStation.PalletStation_0:
                case OnloadRobotStation.PalletStation_1:
                    {
                        if(RobotOrder.MOVE == order)
                        {
                            if(!MotorUMove(MotorPosition.Onload_PalletPos))
                            {
                                return false;
                            }
                        }
                        else if(RobotOrder.DOWN == order)
                        {
                            if(!CheckMotorPos(this.MotorU, MotorPosition.Onload_PalletPos))
                            {
                                return false;
                            }
                            if (!PalletKeepFlat(((int)station - (int)OnloadRobotStation.PalletStation_0), true, true))
                            {
                                return false;
                            }
                        }
                        break;
                    }
                case OnloadRobotStation.BufferStation:
                    {
                        if(RobotOrder.MOVE == order)
                        {
                            if(!MotorUMove(MotorPosition.Onload_BufferPos))
                            {
                                return false;
                            }
                        }
                        else if(RobotOrder.DOWN == order)
                        {
                            if(!CheckMotorPos(this.MotorU, MotorPosition.Onload_BufferPos))
                            {
                                return false;
                            }
                            if (0 == col)
                            {
                                if (FingerCheck(ModDef.Finger_1, true, false))
                                {
                                    if (!BufferCheck(ModDef.Buffer_0, false, false))
                                    {
                                        msg = string.Format("{0}抓手2有电池，缓存1位置有电池，机器人不能下降！", this.RunName);
                                        ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                        return false;
                                    }
                                }
                            }
                            else if (1 == col)
                            {
                                for(int i = 0; i < (int)ModDef.Finger_ALL; i++)
                                {
                                    if(FingerCheck((ModDef.Finger_0 + i), true, false))
                                    {
                                        if(!BufferCheck((ModDef.Buffer_0 + i), false, false))
                                        {
                                            msg = string.Format("{0}抓手{1}有电池，缓存{1}位置有电池，机器人不能下降！", this.RunName, (i + 1));
                                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                            return false;
                                        }
                                    }
                                }
                            }
                            else if (2 == col)
                            {
                                if(FingerCheck(ModDef.Finger_0, true, false))
                                {
                                    if(!BufferCheck(ModDef.Buffer_1, false, false))
                                    {
                                        msg = string.Format("{0}抓手1有电池，缓存2位置有电池，机器人不能下降！", this.RunName);
                                        ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                        return false;
                                    }
                                }
                            }
                        }
                        break;
                    }
                case OnloadRobotStation.OnloadNGOutput:
                    {
                        if(null != this.placeNGRun)
                        {
                            if(!this.placeNGRun.PlaceSenserIsSafe(true))
                            {
                                msg = string.Format("NG输出工位放到位或放料位流出感应器检测非【OFF】，机器人不能下降！");
                                ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                return false;
                            }
                        }
                        if(RobotOrder.MOVE == order)
                        {
                            if(!MotorUMove(MotorPosition.Onload_NGPos))
                            {
                                return false;
                            }
                        }
                        else if(RobotOrder.DOWN == order)
                        {
                            if(!CheckMotorPos(this.MotorU, MotorPosition.Onload_NGPos))
                            {
                                return false;
                            }
                        }
                        break;
                    }
                case OnloadRobotStation.OnloadFakeScan:
                case OnloadRobotStation.OnloadFake:
                    {
                        MotorPosition mtrPos = (OnloadRobotStation.OnloadFakeScan == station) ? MotorPosition.Onload_ScanFakePos : MotorPosition.Onload_FakePos;
                        if(RobotOrder.MOVE == order)
                        {
                            if(!MotorUMove(mtrPos))
                            {
                                return false;
                            }
                        }
                        else if(RobotOrder.DOWN == order)
                        {
                            if(!CheckMotorPos(this.MotorU, mtrPos))
                            {
                                return false;
                            }
                            if(!FingerCheck(ModDef.Finger_ALL, false, false))
                            {
                                msg = string.Format("{0}抓手有电池，机器人不能在假电池位下降！", this.RunName);
                                ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                return false;
                            }
                            if(!FingerClose(ModDef.Finger_ALL, false))
                            {
                                msg = string.Format("{0}抓手非打开状态，机器人不能在夹具扫码位下降！", this.RunName);
                                ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                return false;
                            }
                            if(null != this.pickFakeRun)
                            {
                                if(!this.pickFakeRun.PickPosSenserInpos(true))
                                {
                                    msg = string.Format("假电池工位到位感应器检测非【ON】，机器人不能下降！\r\n是】强制下降，否】取消下降");
                                    if (DialogResult.Yes != ShowMsgBox.ShowDialog(msg, MessageType.MsgQuestion))
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                        break;
                    }
                default:
                    return false;
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
            switch(pos.station)
            {
                case OnloadRobotStation.HomeStatioin:
                    {
                        return true;
                    }
                case OnloadRobotStation.OnloadLine:
                    {
                        if(null != this.pickBatRun)
                        {
                            return this.pickBatRun.RecvPosSenserInpos(true);
                        }
                        break;
                    }
                case OnloadRobotStation.ScanPalletCode_0:
                case OnloadRobotStation.ScanPalletCode_1:
                    {
                        return PalletKeepFlat((int)(pos.station - OnloadRobotStation.ScanPalletCode_0), true, true);
                    }
                case OnloadRobotStation.PalletStation_0:
                case OnloadRobotStation.PalletStation_1:
                    {
                        return PalletKeepFlat((int)(pos.station - OnloadRobotStation.PalletStation_0), true, true);
                    }
                case OnloadRobotStation.BufferStation:
                    {
                        if (RobotOrder.MOVE == order)
                        {
                            return true;
                        }
                        string msg, dispose;
                        msg = dispose = "";
                        if(0 == pos.row)
                        {
                            if(FingerCheck(ModDef.Finger_1, !pos.fingerClose, false))
                            {
                                if(!BufferCheck(ModDef.Buffer_0, pos.fingerClose, false))
                                {
                                    msg = string.Format("{0}抓手2有电池，缓存1位置有电池，机器人不能下降！", this.RunName);
                                    ShowMessageBox((int)MsgID.BufStationDownAlm, msg, dispose, MessageType.MsgAlarm);
                                    return false;
                                }
                                return true;
                            }
                        }
                        else if(1 == pos.row)
                        {
                            for(int i = 0; i < (int)ModDef.Finger_ALL; i++)
                            {
                                // 抓手 && 暂存同时有
                                if(!FingerCheck((ModDef.Finger_0 + i), false, false) && !BufferCheck((ModDef.Buffer_0 + i), false, false))
                                {
                                    msg = string.Format("{0}抓手{1}有电池，缓存{1}位置有电池，机器人不能下降！", this.RunName, (i + 1));
                                    ShowMessageBox((int)MsgID.BufStationDownAlm, msg, dispose, MessageType.MsgAlarm);
                                    return false;
                                }
                            }
                            return true;
                        }
                        else if(2 == pos.row)
                        {
                            if(FingerCheck(ModDef.Finger_0, !pos.fingerClose, false))
                            {
                                if(!BufferCheck(ModDef.Buffer_1, pos.fingerClose, false))
                                {
                                    msg = string.Format("{0}抓手1有电池，缓存2位置有电池，机器人不能下降！", this.RunName);
                                    ShowMessageBox((int)MsgID.BufStationDownAlm, msg, dispose, MessageType.MsgAlarm);
                                    return false;
                                }
                                return true;
                            }
                        }
                        break;
                    }
                case OnloadRobotStation.OnloadNGOutput:
                    {
                        if(null != this.placeNGRun)
                        {
                            switch(pos.finger)
                            {
                                case ModDef.Finger_0:
                                case ModDef.Finger_1:
                                    if(!this.placeNGRun.PlacePosInposIsSafe(pos.row) 
                                        || !this.placeNGRun.PlacePosInposIsSafe(pos.row + 1))
                                    {
                                        return false;
                                    }
                                    break;
                                case ModDef.Finger_ALL:
                                    for(int i = 0; i < (int)ModDef.Finger_ALL; i++)
                                    {
                                        if(!this.placeNGRun.PlacePosInposIsSafe(pos.row + i))
                                        {
                                            return false;
                                        }
                                    }
                                    break;
                                default:
                                    return false;
                                    //break;
                            }
                            return this.placeNGRun.PlaceSenserIsSafe(true);
                        }
                        break;
                    }
                case OnloadRobotStation.OnloadFakeScan:
                case OnloadRobotStation.OnloadFake:
                    {
                        if(null != this.pickFakeRun)
                        {
                            return this.pickFakeRun.PickPosSenserInpos(true);
                        }
                        break;
                    }
                default:
                    break;
            }
            return false;
        }

        #endregion

        #region // 扫码器

        /// <summary>
        /// 扫码器的连接地址信息
        /// </summary>
        /// <returns></returns>
        public string ScanAdderInfo()
        {
            return this.barcodeScan.AdderInfo();
        }

        /// <summary>
        /// 扫码器连接状态
        /// </summary>
        /// <returns></returns>
        public bool ScanIsConnect()
        {
            if(!this.scanEnable)
            {
                return true;
            }
            return this.barcodeScan.IsConnect();
        }

        /// <summary>
        /// 扫码器连接
        /// </summary>
        /// <param name="connect">true连接，false断开</param>
        /// <returns></returns>
        public bool ScanConnect(bool connect = true)
        {
            if(!this.scanEnable)
            {
                return true;
            }
            if(connect)
            {
                if(string.IsNullOrEmpty(this.barcodeScanIP) && (this.barcodeScanCom > -1))
                {
                    return this.barcodeScan.ConnectCom(this.barcodeScanCom, this.barcodeScanPort, (this.scanLinefeed ? "\r\n" : "\n"));
                }
                else if(!string.IsNullOrEmpty(this.barcodeScanIP) && (this.barcodeScanCom < 0))
                {
                    return this.barcodeScan.ConnectSocket(this.barcodeScanIP, this.barcodeScanPort);
                }
            }
            else
            {
                return this.barcodeScan.Disconnect();
            }
            return false;
        }

        /// <summary>
        /// 扫码器触发扫码
        /// </summary>
        /// <returns></returns>
        public bool ScanCode()
        {
            if(!this.scanEnable)
            {
                return true;
            }
            if(!ScanIsConnect())
            {
                if(!ScanConnect(true))
                {
                    return false;
                }
            }
            if(this.barcodeScan.Send(scanCmd + (scanLinefeed ? "\r\n" : "")))
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
        public bool GetScanResult(ref string code, int timeout = 5 * 1000)
        {
            if(!this.scanEnable)
            {
                return true;
            }
            if(this.barcodeScan.Recv(ref code, timeout))
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
        public bool CheckScanCode(string code, bool alm)
        {
            if(!this.scanPalletEnable)
            {
                return true;
            }
            string msg, disp;
            if(!string.IsNullOrEmpty(this.scanNGType) && (code.IndexOf(this.scanNGType) > -1))
            {
                if(alm)
                {
                    msg = string.Format("扫码器扫码失败，扫码器反馈：{0}", code);
                    disp = "请检查当前条码";
                    ShowMessageBox((int)MsgID.CodeTypeError, msg, disp, MessageType.MsgWarning);
                }
                return false;
            }
            if((this.codeLength > -1) && (code.Length != this.codeLength))
            {
                if(alm)
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
                    if(code.StartsWith(item))
                    {
                        result = true;
                        break;
                    }
                }
                if(!result)
                {
                    if(alm)
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
            // 高位防呆
            if (RobotOrder.DOWN != this.robotDebugAction.order)
            {
                bool findOutput = false;
                for(int i = 0; i < (int)ModDef.Finger_ALL; i++)
                {
                    if(!InputState(IFingerCheck[i], false))
                    {
                        if(bOn && (Outputs(OFingerOpen[i]) == output))
                        {
                            findOutput = true;
                            break;
                        }
                        else if (!bOn && (Outputs(OFingerClose[i]) == output))
                        {
                            findOutput = true;
                            break;
                        }
                    }
                }
                if (findOutput)
                {
                    ShowMsgBox.ShowDialog("抓手有料，只能在机器人【下降】时打开抓手", MessageType.MsgWarning);
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
            if (Motors(MotorU) == motor)
            {
                if(this.RobotRunning)
                {
                    ShowMsgBox.ShowDialog("机器人运行中，不能移动调宽电机", MessageType.MsgWarning);
                    return false;
                }
                if ((RobotOrder.HOME != this.robotDebugAction.order) 
                    && (RobotOrder.MOVE != this.robotDebugAction.order)
                    && (RobotOrder.UP != this.robotDebugAction.order))
                {
                    string msg = string.Format("机器人不在【{0}/{1}/{2}】，机器人不能操作调宽电机"
                        , RobotDef.RobotOrderName[(int)RobotOrder.HOME], RobotDef.RobotOrderName[(int)RobotOrder.MOVE], RobotDef.RobotOrderName[(int)RobotOrder.UP]);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 模组防呆监视：请勿加入耗时过长或阻塞操作
        /// </summary>
        public override void MonitorAvoidDie()
        {
            if (this.robotNeedEStop && !InputState(this.IFingerDelay, false))
            {
                OutputAction(this.ORobotEStop, true);
                this.robotNeedEStop = false;
                string msg = string.Format("{0} {1} 感应器ON，急停被触发", Inputs(IFingerDelay).Num, Inputs(IFingerDelay).Name);
                ShowMessageID((int)MsgID.RbtDelayEStop, msg, "请人工处理撞机问题", MessageType.MsgAlarm);
            }
            else if (!this.robotNeedEStop && !InputState(IFingerDelay, true))
            {
                this.robotNeedEStop = true;
            }
            if(this.RobotRunning || !InputState(IRobotRunning, false))
            {
                if(MachineCtrl.GetInstance().SafeDoorState && MachineCtrl.GetInstance().ClientIsConnect())
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

        /// <summary>
        /// 设备停止后操作，如果派生类重写了该函数，它必须调用基实现。
        /// </summary>
        public override void AfterStopAction()
        {
            for(int i = 0; i < OPalletAlarm.Length; i++)
            {
                OutputAction(OPalletAlarm[i], false);
            }
            base.AfterStopAction();
        }

        #endregion
        
        #region // 添加删除夹具

        public override void ManualAddPallet(int pltIdx, int maxRow, int maxCol, PalletStatus pltState, BatteryStatus batState)
        {
            // 仅空夹具
            if (PalletStatus.OK == pltState)
            {
                this.Pallet[pltIdx].State = PalletStatus.OK;
                this.Pallet[pltIdx].SetRowCol(maxRow, maxCol);
                SaveRunData(SaveType.Pallet, pltIdx);
            }
        }

        public override void ManualClearPallet(int pltIdx)
        {
            if (AutoSteps.Auto_WaitWorkStart == (AutoSteps)this.nextAutoStep)
            {
                // 清除夹具 已发送请求 → 禁止删除夹具
                for(EventList i = EventList.OnloadPlaceEmptyPallet; i < EventList.OnloadPickPlaceEnd; i++)
                {
                    int evtPltIdx = -1;
                    EventStatus state = GetEvent(this, i, ref evtPltIdx);
                    if((evtPltIdx == pltIdx) && (EventStatus.Invalid != state) && (EventStatus.Finished != state))
                    {
                        ShowMsgBox.ShowDialog("当前夹具已发送取夹具事件，不能清除夹具", MessageType.MsgWarning);
                        return;
                    }
                }
                this.Pallet[pltIdx].Release();
                SaveRunData(SaveType.Pallet, pltIdx);
            }
            else
            {
                ShowMsgBox.ShowDialog("仅在等待开始信号步骤才能清除夹具", MessageType.MsgWarning);
            }
        }
        #endregion

        #region // 保存数据

        /// <summary>
        /// 保存电池绑定夹具数据
        /// </summary>
        private void SaveBatBindPltData(int batRow, int batCol, Battery bat, int pltIdx, string pltCode)
        {
            string file, title, text;
            file = string.Format(@"{0}\电芯绑定夹具\{1}\夹具位{2}-{3}-{1}.csv"
                    , MachineCtrl.GetInstance().ProductionFilePath, DateTime.Now.ToString("yyyy-MM-dd"), (pltIdx + 1), pltCode);
            title = "日期,时间,夹具条码,电芯行,电芯列,电芯条码,电芯状态";
            text = string.Format("{0},{1},{2},{3},{4},{5}\r\n", DateTime.Now.ToString("yyyy-MM-dd,HH:mm:ss"), pltCode, (batRow + 1), (batCol + 1), bat.Code, bat.Type);
            Def.ExportCsvFile(file, title, text);
        }

        #endregion

        #region // 上传Mes数据

        /// <summary>
        /// 夹具校验
        /// </summary>
        /// <param name="pltCode"></param>
        /// <returns></returns>
        public bool MesCheckPalletStatus(string pltCode,ref string msg)
        {
            DateTime startTime = DateTime.Now;
            int result = -1;
            string mesSend = "";
            string mesRecv = "";
            string mesReturn="";
            if (!MachineCtrl.GetInstance().UpdataMes)
            {
                return true;
            }
            MesInterface mes = MesInterface.TrayVerifity;
            try
            {
                MesConfig mesCfg = MesDefine.GetMesCfg(mes);
                if(null == mesCfg)
                {
                    throw new Exception(mes + " is null.");
                }
                JObject mesData = JObject.Parse(JsonConvert.SerializeObject(new
                {
                    traycode = pltCode,
                    equipment_id = MesResources.Onload.EquipmentID,
                    process_id = MesResources.Onload.ProcessID,
                }));
                mesCfg.send = mesData.ToString();
                mesSend = Regex.Replace(MachineCtrl.RevertJsonString(mesData.ToString()), @"\s", "");

                if (!mesCfg.enable)
                {
                    MesLog.WriteLog(mes, $"stop.Need post: {mesData.ToString(Formatting.None)}", LogType.Information);
                    return true;
                }
                else
                {
                    MesLog.WriteLog(mes, $"mes post: {mesData.ToString(Formatting.None)}", LogType.Information);
                }
                // 离线保存
                if (!MachineCtrl.GetInstance().UpdataMes)
                {
                    MachineCtrl.GetInstance().SaveMesData(MesInterface.TrayVerifity, mesData.ToString());
                    return true;
                }
                mesReturn = this.httpClient.Post(mesCfg.mesUri, mesData.ToString(), MesData.mesinterfaceTimeOut);
                mesRecv = Regex.Replace(MachineCtrl.RevertJsonString(mesReturn), @"\s", "");
                if (null != mesReturn)
                {
                    mesCfg.recv = mesReturn;
                    mesCfg.updataRS = true;
                    JObject jsonReturn = JObject.Parse(mesReturn);
                    // 校验成功返回码0成功，1失败 
                    if(jsonReturn.ContainsKey("status_code"))
                    {
                        result = Convert.ToInt32(jsonReturn["status_code"]);
                        if(0 != result)
                        {
                            msg = $"{MesDefine.GetMesTitle(mes)}【{pltCode}】失败，MES返回错误：{jsonReturn["message"]}";
                            MesLog.WriteLog(mes, $"mes return : {mesReturn}");
                            ShowMessageBox((int)MsgID.CheckPallet, msg, "请检查MES数据参数！", MessageType.MsgAlarm);
                            return false;
                        }
                        MesLog.WriteLog(mes, $"mes return : {mesReturn}", LogType.Success);
                        return true;
                    }
                }
                else
                {
                    MesLog.WriteLog(mes, $"mes return null.");
                }
            }
            catch(System.Exception ex)
            {
                msg = $"构造 {MesDefine.GetMesTitle(mes)} 数据错误：{ex.Message}";
                ShowMessageBox((int)MsgID.CheckPallet, msg, "请检查MES数据参数！", MessageType.MsgAlarm);
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string text = $"{MesResources.Onload.ProcessID},{MesResources.Onload.EquipmentID},{startTime},{second},{DateTime.Now},{result},{msg},{mesSend},{mesRecv}";
                MachineCtrl.SaveLogData("托盘校验", text);
            }
            return false;
        }

        /// <summary>
        /// 绑盘上传，整盘
        /// </summary>
        /// <param name="plt"></param>
        /// <returns></returns>
        public bool MesBindPalletInfo(Pallet plt,ref string msg)
        {
            DateTime startTime = DateTime.Now;
            int result = -1;
            string mesSend = "";
            string mesReturn = "";
            string mesRecv = "";
            //if (!MachineCtrl.GetInstance().UpdataMes)
            //{
            //    return true;
            //}
            MesInterface mes = MesInterface.SaveTrayAndBarcodeRecord;
            try
            {
                MesConfig mesCfg = MesDefine.GetMesCfg(mes);
                if(null == mesCfg)
                {
                    throw new Exception(mes + " is null.");
                }
                JObject mesData = JObject.Parse(JsonConvert.SerializeObject(new
                {
                    equipment_id = MesResources.Onload.EquipmentID,
                    process_id = MesResources.Onload.ProcessID,
                    traycode = plt.Code,
                }));
                int locationNum = 1;
                JArray data = new JArray();
                for(int row = 0; row < plt.MaxRow; row++)
                {
                    for(int col = 0; col < plt.MaxCol; col++)
                    {
                        if(BatteryStatus.OK == plt.Battery[row, col].Type)
                        {
                            JObject bar = JObject.Parse(JsonConvert.SerializeObject(new
                            {
                                bar_code = plt.Battery[row, col].Code,
                                location = locationNum.ToString().PadLeft(2,'0'),
                            }));
                            data.Add(bar);
                            
                        }
                        locationNum++;
                    }

                }
                mesData.Add(nameof(data), data);
                mesCfg.send = mesData.ToString();
                mesSend = Regex.Replace(MachineCtrl.RevertJsonString(mesData.ToString()), @"\s", "");
                MesLog.WriteLog(mes, $"mes post: {mesData.ToString(Formatting.None)}", LogType.Information);
                // 离线保存
                if (!MachineCtrl.GetInstance().UpdataMes)
                {
                    MachineCtrl.GetInstance().SaveMesData(MesInterface.SaveTrayAndBarcodeRecord, mesData.ToString());
                    return true;
                }
                mesReturn = this.httpClient.Post(mesCfg.mesUri, mesData.ToString(), MesData.mesinterfaceTimeOut);
                mesRecv = Regex.Replace(MachineCtrl.RevertJsonString(mesReturn), @"\s", "");
                if (null != mesReturn)
                {
                    mesCfg.recv = mesReturn;
                    mesCfg.updataRS = true;
                    JObject jsonReturn = JObject.Parse(mesReturn);
                    // 校验成功返回码0成功，1失败 
                    if(jsonReturn.ContainsKey("status_code"))
                    {
                        result = Convert.ToInt32(jsonReturn["status_code"]);
                        if(0 != result)
                        {
                            msg = $"{MesDefine.GetMesTitle(mes)}上传失败，MES返回错误：{jsonReturn["message"]}";
                            MesLog.WriteLog(mes, $"mes return : {mesReturn}");
                            ShowMessageBox((int)MsgID.BindPallet, msg, "请检查MES数据参数！", MessageType.MsgAlarm);
                            return false;
                        }
                        MesLog.WriteLog(mes, $"mes return : {mesReturn}", LogType.Success);
                        return true;
                    }
                }
                else
                {
                    MesLog.WriteLog(mes, $"mes return null.");
                }
            }
            catch(System.Exception ex)
            {
                msg = $"构造 {MesDefine.GetMesTitle(mes)} 数据错误：{ex.Message}";
                ShowMessageBox((int)MsgID.BindPallet, msg, "请检查MES数据参数！", MessageType.MsgAlarm);
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string text = $"{MesResources.Onload.ProcessID},{MesResources.Onload.EquipmentID},{startTime},{second},{DateTime.Now},{result},{msg},{mesSend},{mesRecv}";
                MachineCtrl.SaveLogData("绑盘信息上传", text);
            }
            return false;
        }

        #endregion

        #region // 模组信号重置

        /// <summary>
        /// 模组信号重置
        /// </summary>
        public override void ResetModuleEvent()
        {
            if (AutoSteps.Auto_WaitWorkStart != (AutoSteps)this.nextAutoStep)
            {
                ShowMsgBox.ShowDialog("上料模组只能在等待开始信号步骤时重置模组信号", MessageType.MsgWarning);
                return;
            }
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
                this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                SaveRunData(SaveType.AutoStep);
            }
        }
        #endregion
    }
}

