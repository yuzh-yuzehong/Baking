using HelperLibrary;
using System;
using System.Diagnostics;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    /// <summary>
    /// 冷却下料
    /// </summary>
    class RunProcessCoolingOffload : RunProcess
    {
        #region // 枚举：步骤，模组数据，报警列表

        protected new enum InitSteps
        {
            Init_DataRecover = 0,

            Init_MotorZHome,
            Init_MotorZMoveSafe,
            Init_CheckFinger,
            Init_MotorXYUHome,
            Init_MotorXYUMoveSafe,

            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,

            // 取：冷却系统
            Auto_CalcCoolingSystemPickPos,
            Auto_CoolingSystemPosSetEvent,
            Auto_CoolingSystemPosPickMove,
            Auto_CoolingSystemPosPickDown,
            Auto_CoolingSystemPosFingerAction,
            Auto_CoolingSystemPosPickUp,
            Auto_CoolingSystemPosCheckFinger,

            // 计算放料位
            Auto_CalcPlacePos,

            // 暂存：可取可防，主要看抓手操作
            Auto_CalcBufferPos,
            Auto_BufferPosSetEvent,
            Auto_BufferPosMove,
            Auto_BufferPosDown,
            Auto_BufferPosFingerAction,
            Auto_BufferPosUp,
            Auto_BufferPosCheckFinger,

            // 放：下料线
            Auto_CalcOffloadPlacePos,
            Auto_OffloadPosSetEvent,
            Auto_OffloadPosPlaceMove,
            Auto_OffloadPosPlaceDown,
            Auto_OffloadPosFingerAction,
            Auto_OffloadPosPlaceUp,
            Auto_OffloadPosCheckFinger,

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
            Start = ModuleMsgID.CoolingSystemMsgStartID,
        }

        #endregion

        #region // 取放位置结构体

        private struct PickPlacePos
        {
            #region //字段
            public MotorPosition station;           // 站号
            public int row;                         // 行索引
            public int col;                         // 列索引   
            public ModDef finger;               // 抓手索引         
            public bool fingerClose;                // 抓手关闭
            #endregion

            #region //方法
            public void SetData(MotorPosition curStation, int curRow, int curCol, ModDef curFinger, bool curFingerClose)
            {
                this.station = curStation;
                this.row = curRow;
                this.col = curCol;
                this.finger = curFinger;
                this.fingerClose = curFingerClose;
            }

            public void Release()
            {
                this.station = MotorPosition.Invalid;
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

        private int[] IFingerOpen;              // 抓手打开到位
        private int[] IFingerClose;             // 抓手关闭到位
        private int[] IFingerCheck;             // 抓手有料检查
        private int[] IBufferCheck;             // 暂存有料检查
        private int IFingerDelay;               // 抓手防呆
        private int IRotatePush;               // 下料旋转气缸推出到位
        private int IRotatePull;               // 下料旋转气缸回退到位

        private int[] OFingerOpen;              // 抓手打开
        private int[] OFingerClose;             // 抓手关闭
        private int ORotatePush;               // 下料旋转气缸推出
        private int ORotatePull;               // 下料旋转气缸回退

        #endregion

        #region // 电机

        private int MotorX;         // 电机
        private int MotorY;         // 电机
        private int MotorZ;         // 电机
        private int MotorU;         // 电机

        #endregion

        #region // ModuleEx.cfg配置

        #endregion

        #region // 模组参数

        private float pickPosXDis;      // 取料位X方向间距：mm
        private float pickPosYDis;      // 取料位Y方向间距：mm
        private float pickPosYSafeDis;  // 取料位Y方向安全偏移间距：mm

        #endregion

        #region // 模组数据

        // 模组指针
        private RunProcessCoolingSystem coolingSystem;      // 冷却系统：CoolingSystem = 
        private RunProcessOffloadLine offloadLine;          // 下料线：OffloadLine = 


        private PickPlacePos pickPos;                       // 取料位置
        private PickPlacePos placePos;                      // 放料位置

        #endregion

        #endregion

        public RunProcessCoolingOffload(int runId) : base(runId)
        {
            InitBatteryPalletSize((int)ModDef.Finger_Buffer_ALL, 0);

            PowerUpRestart();

            InitParameter();
            // 参数
            InsertVoidParameter("pickPosXDis", "X方向间距", "取料位X方向间距：mm", pickPosXDis, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);
            //InsertVoidParameter("pickPosYDis", "Y方向间距", "取料位Y方向间距：mm", pickPosYDis, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("pickPosYSafeDis", "Y方向安全偏移", "取料位Y方向安全偏移间距：mm", pickPosYSafeDis, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);

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
                        this.nextInitStep = InitSteps.Init_MotorZHome;
                        break;
                    }
                case InitSteps.Init_MotorZHome:
                    {
                        CurMsgStr("电机Z回零", "Motor Z home");
                        if(MotorHome(this.MotorZ))
                        {
                            this.nextInitStep = InitSteps.Init_MotorZMoveSafe;
                        }
                        break;
                    }
                case InitSteps.Init_MotorZMoveSafe:
                    {
                        CurMsgStr("电机Z移动到安全位", "Motor Z move safety pos");
                        if(MotorZMove(MotorPosition.CoolingOffload_SafetyPos))
                        {
                            this.nextInitStep = InitSteps.Init_CheckFinger;
                        }
                        break;
                    }
                case InitSteps.Init_CheckFinger:
                    {
                        CurMsgStr("检查抓手及暂存感应器", "Check finger senser");
                        for(ModDef i = ModDef.Finger_0; i < ModDef.Finger_ALL; i++)
                        {
                            if(!FingerCheck(i, (FingerBat(i).Type > BatteryStatus.Invalid), true))
                            {
                                return;
                            }
                        }
                        for(ModDef i = ModDef.Buffer_0; i < ModDef.Buffer_ALL; i++)
                        {
                            if(!BufferCheck(i, (BufferBat(i).Type > BatteryStatus.Invalid), true))
                            {
                                return;
                            }
                        }

                        this.nextInitStep = InitSteps.Init_MotorXYUHome;
                        break;
                    }
                case InitSteps.Init_MotorXYUHome:
                    {
                        CurMsgStr("电机XYU回零", "Motor XYU home");
                        int[] motorsID = new int[] { this.MotorX, this.MotorY, this.MotorU };
                        if(MotorsHome(motorsID, motorsID.Length))
                        {
                            this.nextInitStep = InitSteps.Init_MotorXYUMoveSafe;
                        }
                        break;
                    }
                case InitSteps.Init_MotorXYUMoveSafe:
                    {
                        CurMsgStr("电机XYU移动到安全位", "Motor XYU move safety pos");
                        if(MotorXYUMove(MotorPosition.CoolingOffload_SafetyPos, 0, 0))
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
            if (Def.IsNoHardware())
            {
                Sleep(50);
            }

            switch((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");

                        // 取料
                        if (CalcPickPos(ref this.pickPos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcCoolingSystemPickPos;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }
                        break;
                    }

                #region // 取：冷却系统
                case AutoSteps.Auto_CalcCoolingSystemPickPos:
                    {
                        CurMsgStr("计算冷却系统取料位", "Calc cooling system pick pos");
                        this.nextAutoStep = AutoSteps.Auto_CoolingSystemPosSetEvent;
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                case AutoSteps.Auto_CoolingSystemPosSetEvent:
                    {
                        CurMsgStr("冷却系统取料设置响应信号", "Cooling system set pick event");
                        if (EventStatus.Require == GetEvent(this.coolingSystem, EventList.CoolingSystemPickBattery))
                        {
                            if (SetEvent(this.coolingSystem, EventList.CoolingSystemPickBattery, EventStatus.Response))
                            {
                                this.nextAutoStep = AutoSteps.Auto_CoolingSystemPosPickMove;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_CoolingSystemPosPickMove:
                    {
                        CurMsgStr("冷却系统取料位移动", "Cooling system pick pos move");
                        if(RotatePush(true, false) && MotorXYUMove(pickPos.station, pickPos.row, pickPos.col))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CoolingSystemPosPickDown;
                        }
                        break;
                    }
                case AutoSteps.Auto_CoolingSystemPosPickDown:
                    {
                        CurMsgStr("冷却系统取料位下降", "Cooling system pick pos down");
                        EventStatus state = GetEvent(this.coolingSystem, EventList.CoolingSystemPickBattery);
                        if((EventStatus.Ready == state) || (EventStatus.Start == state))
                        {
                            SetEvent(this.coolingSystem, EventList.CoolingSystemPickBattery, EventStatus.Start);
                            if(RotatePush(true, true) && MotorZMove(pickPos.station))
                            {
                                this.nextAutoStep = AutoSteps.Auto_CoolingSystemPosFingerAction;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_CoolingSystemPosFingerAction:
                    {
                        CurMsgStr("冷却系统取料位抓手关闭", "Cooling system pick pos finger close");
                        if(FingerClose(pickPos.finger, pickPos.fingerClose))
                        {
                            RunProcessCoolingSystem run = this.coolingSystem;
                            switch(pickPos.finger)
                            {
                                case ModDef.Finger_0:
                                    this.Battery[(int)ModDef.Finger_0].Copy(run.BatteryLine.Battery[pickPos.row, pickPos.col]);
                                    run.BatteryLine.Battery[pickPos.row, pickPos.col].Release();
                                    break;
                                case ModDef.Finger_1:
                                    this.Battery[(int)ModDef.Finger_1].Copy(run.BatteryLine.Battery[pickPos.row + 1, pickPos.col]);
                                    run.BatteryLine.Battery[pickPos.row + 1, pickPos.col].Release();
                                    break;
                                case ModDef.Finger_ALL:
                                    for(int i = 0; i < (int)ModDef.Finger_ALL; i++)
                                    {
                                        this.Battery[i].Copy(run.BatteryLine.Battery[pickPos.row + i, pickPos.col]);
                                        run.BatteryLine.Battery[pickPos.row + i, pickPos.col].Release();
                                    }
                                    break;
                                default:
                                    return;
                            }
                            run.SaveRunData(SaveType.Battery);
                            this.nextAutoStep = AutoSteps.Auto_CoolingSystemPosPickUp;
                            SaveRunData(SaveType.AutoStep|SaveType.Battery);
                        }
                        break;
                    }
                case AutoSteps.Auto_CoolingSystemPosPickUp:
                    {
                        CurMsgStr("冷却系统取料位上升", "Cooling system pick pos up");
                        if(MotorZMove(MotorPosition.CoolingOffload_SafetyPos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CoolingSystemPosCheckFinger;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_CoolingSystemPosCheckFinger:
                    {
                        CurMsgStr("冷却系统取料后检查抓手", "Cooling system pick pos check finger");
                        if(FingerCheck(pickPos.finger, pickPos.fingerClose, true))
                        {
                            string locName = "";
                            float curPos, locPos;
                            curPos = locPos = 0.0f;
                            if(Def.IsNoHardware() || ((int)MotorCode.MotorOK == Motors(this.MotorY).GetCurPos(ref curPos))
                                && ((int)MotorCode.MotorOK == Motors(this.MotorY).GetLocation((int)pickPos.station, ref locName, ref locPos)))
                            {
                                if (curPos > locPos + this.pickPosYSafeDis)
                                {
                                    if(!MotorMove(this.MotorY, (int)pickPos.station, this.pickPosYSafeDis))
                                    {
                                        break;
                                    }
                                }
                                SetEvent(this.coolingSystem, EventList.CoolingSystemPickBattery, EventStatus.Finished);

                                this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                #endregion

                #region // 计算放料位
                case AutoSteps.Auto_CalcPlacePos:
                    {
                        CurMsgStr("计算放料位", "Calc place pos");
                        if (CalcFingerBufferMatchesPos(ref placePos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcBufferPos;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }
                        else if(CalcPlacePos(ref placePos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcOffloadPlacePos;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }
                        else if (FingerCount() < 1)
                        {
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                #endregion

                #region // 暂存：可取可防
                case AutoSteps.Auto_CalcBufferPos:
                    {
                        CurMsgStr("计算缓存取放位", "Calc buffer pos");
                        this.nextAutoStep = AutoSteps.Auto_BufferPosSetEvent;
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                case AutoSteps.Auto_BufferPosSetEvent:
                    {
                        CurMsgStr("缓存位设置响应信号", "Set buffer pos event");
                        this.nextAutoStep = AutoSteps.Auto_BufferPosMove;
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                case AutoSteps.Auto_BufferPosMove:
                    {
                        CurMsgStr("缓存位移动", "Buffer pos move");
                        if (MotorXYUMove(placePos.station, placePos.row, placePos.col))
                        {
                            this.nextAutoStep = AutoSteps.Auto_BufferPosDown;
                        }
                        break;
                    }
                case AutoSteps.Auto_BufferPosDown:
                    {
                        CurMsgStr("缓存位下降", "Buffer pos down");
                        if(MotorZMove(placePos.station))
                        {
                            this.nextAutoStep = AutoSteps.Auto_BufferPosFingerAction;
                        }
                        break;
                    }
                case AutoSteps.Auto_BufferPosFingerAction:
                    {
                        CurMsgStr("缓存位抓手动作", "Buffer pos finger action");
                        if(FingerClose(placePos.finger, placePos.fingerClose))
                        {
                            switch(placePos.finger)
                            {
                                case ModDef.Finger_0:
                                    {
                                        int bufIdx = (int)ModDef.Buffer_0 + (int)placePos.station - (int)MotorPosition.CoolingOffload_BufferPos2;
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
                                        int bufIdx = (int)ModDef.Buffer_0 + (int)placePos.station - (int)MotorPosition.CoolingOffload_BufferPos1;
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
                                                this.Battery[i].Copy(this.Battery[bufIdx]);
                                                this.Battery[bufIdx].Release();
                                            }
                                        }
                                        // 放
                                        else
                                        {
                                            for(int i = 0; i < (int)ModDef.Finger_ALL; i++)
                                            {
                                                int bufIdx = (int)ModDef.Finger_ALL + i;
                                                this.Battery[bufIdx].Copy(this.Battery[i]);
                                                this.Battery[i].Release();
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
                        CurMsgStr("缓存位上升", "Buffer pos up");
                        if(MotorZMove(MotorPosition.CoolingOffload_SafetyPos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_BufferPosCheckFinger;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_BufferPosCheckFinger:
                    {
                        CurMsgStr("缓存位取放料后检查抓手", "Buffer pos Check finger senser");
                        if(FingerCheck(placePos.finger, placePos.fingerClose))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                #endregion

                #region // 放：下料线
                case AutoSteps.Auto_CalcOffloadPlacePos:
                    {
                        CurMsgStr("计算放料位", "Calc offload place pos");
                        this.nextAutoStep = AutoSteps.Auto_OffloadPosSetEvent;
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                case AutoSteps.Auto_OffloadPosSetEvent:
                    {
                        CurMsgStr("放料位设置响应信号", "Set offload place pos event");
                        if (EventStatus.Require == GetEvent(this.offloadLine, EventList.OffLoadLinePlaceBattery))
                        {
                            if (SetEvent(this.offloadLine, EventList.OffLoadLinePlaceBattery, EventStatus.Response))
                            {
                                this.nextAutoStep = AutoSteps.Auto_OffloadPosPlaceMove;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadPosPlaceMove:
                    {
                        CurMsgStr("放料位移动", "Offload place pos move");
                        if (RotatePush(false, false) && MotorXYUMove(placePos.station, placePos.row, placePos.col))
                        {
                            this.nextAutoStep = AutoSteps.Auto_OffloadPosPlaceDown;
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadPosPlaceDown:
                    {
                        CurMsgStr("放料位下降", "Offload place pos down");
                        EventStatus state = GetEvent(this.offloadLine, EventList.OffLoadLinePlaceBattery);
                        if((EventStatus.Ready == state) || (EventStatus.Start == state))
                        {
                            SetEvent(this.offloadLine, EventList.OffLoadLinePlaceBattery, EventStatus.Start);
                            if(RotatePush(false, true) && MotorZMove(placePos.station))
                            {
                                this.nextAutoStep = AutoSteps.Auto_OffloadPosFingerAction;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadPosFingerAction:
                    {
                        CurMsgStr("放料位抓手打开", "Offload place pos finger open");
                        if(FingerClose(placePos.finger, placePos.fingerClose))
                        {
                            RunProcessOffloadLine run = this.offloadLine;
                            switch(placePos.finger)
                            {
                                case ModDef.Finger_ALL:
                                    for(int i = 0; i < (int)ModDef.Finger_ALL; i++)
                                    {
                                        run.Battery[i].Copy(this.Battery[i]);
                                        this.Battery[i].Release();
                                    }
                                    TotalData.OffloadCount += 2;
                                    TotalData.WriteTotalData();

                                    break;
                                default:
                                    return;
                            }
                            run.SaveRunData(SaveType.Battery);
                            this.nextAutoStep = AutoSteps.Auto_OffloadPosPlaceUp;
                            SaveRunData(SaveType.AutoStep|SaveType.Battery);
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadPosPlaceUp:
                    {
                        CurMsgStr("放料位上升", "Offload place pos up");
                        if(MotorZMove(MotorPosition.CoolingOffload_SafetyPos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_OffloadPosCheckFinger;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadPosCheckFinger:
                    {
                        CurMsgStr("放料后检查抓手", "Offload place pos check finger senser");
                        if(FingerCheck(placePos.finger, placePos.fingerClose))
                        {
                            SetEvent(this.offloadLine, EventList.OffLoadLinePlaceBattery, EventStatus.Finished);

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
            this.pickPos.Release();
            this.placePos.Release();

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

            key = string.Format("pickPos.station");
            this.pickPos.station = (MotorPosition)iniStream.ReadInt(section, key, (int)this.pickPos.station);
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

            if(SaveType.Variables == (SaveType.Variables & saveType))
            {
                string[] posName = new string[] { "pickPos", "placePos" };
                PickPlacePos[] pos = new PickPlacePos[] { pickPos, placePos };
                for(int i = 0; i < pos.Length; i++)
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
            this.pickPosXDis = 0.0f;
            this.pickPosYDis = 0.0f;
            this.pickPosYSafeDis = 0.0f;
        }

        /// <summary>
        /// 读取通用组参数 + 模组参数
        /// </summary>
        /// <returns></returns>
        public override bool ReadParameter()
        {
            this.pickPosXDis = (float)ReadDoubleParameter(this.RunModule, "pickPosXDis", this.pickPosXDis);
            this.pickPosYDis = (float)ReadDoubleParameter(this.RunModule, "pickPosYDis", this.pickPosYDis);
            this.pickPosYSafeDis = (float)ReadDoubleParameter(this.RunModule, "pickPosYSafeDis", this.pickPosYSafeDis);

            return base.ReadParameter();
        }

        /// <summary>
        /// 读取本模组的相关模组
        /// </summary>
        public override void ReadRelatedModule()
        {
            string module, value;
            module = this.RunModule;

            // 取电池模组
            this.coolingSystem = MachineCtrl.GetInstance().GetModule(RunID.CoolingSystem) as RunProcessCoolingSystem;
            // 放电池模组
            this.offloadLine = MachineCtrl.GetInstance().GetModule(RunID.OffloadLine) as RunProcessOffloadLine;

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
            this.IFingerCheck = new int[maxFinger];
            this.IBufferCheck = new int[maxFinger];
            for(int i = 0; i < maxFinger; i++)
            {
                this.IFingerOpen[i] = AddInput("IFingerOpen" + i);
                this.IFingerClose[i] = AddInput("IFingerClose" + i);
                this.IFingerCheck[i] = AddInput("IFingerCheck" + i);
            }
            for(int i = 0; i < maxFinger; i++)
            {
                this.IBufferCheck[i] = AddInput("IBufferCheck" + i);
            }
            this.IFingerDelay = AddInput("IFingerDelay");
            this.IRotatePush = AddInput("IRotatePush");
            this.IRotatePull = AddInput("IRotatePull");

            this.OFingerOpen = new int[maxFinger];
            this.OFingerClose = new int[maxFinger];
            for(int i = 0; i < maxFinger; i++)
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

        #endregion

        #region // 电机操作

        private bool MotorZMove(MotorPosition station)
        {
            if (this.MotorZ < 0)
            {
                return true;
            }
            return MotorMove(this.MotorZ, (int)station);
        }

        private bool MotorXYUMove(MotorPosition station, int row, int col)
        {
            int motorsNum = 0;
            int[] motorsID = new int[3];
            int[] posID = new int[] { (int)station, (int)station, (int)station };
            float[] offset = new float[3];
            if(this.MotorX > -1)
            {
                motorsID[motorsNum] = this.MotorX;
                offset[motorsNum] = row * this.pickPosXDis;
                motorsNum++;
            }
            if(this.MotorY > -1)
            {
                motorsID[motorsNum] = this.MotorY;
                //offset[motorsNum] = col * this.pickPosYDis;
                offset[motorsNum] = 0.0f;
                motorsNum++;
            }
            if(this.MotorU > -1)
            {
                motorsID[motorsNum] = this.MotorU;
                offset[motorsNum] = 0.0f;
                motorsNum++;
            }
            return MotorsMove(motorsID, posID, offset, motorsNum);
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

        #endregion

        #region // 抓手及暂存

        /// <summary>
        /// 旋转气缸动作
        /// </summary>
        /// <param name="push"></param>
        /// <returns></returns>
        private bool RotatePush(bool push, bool waitState)
        {
            if(Def.IsNoHardware())
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

        private Battery FingerBat(ModDef finger)
        {
            if(finger < ModDef.Finger_0 || finger >= ModDef.Finger_ALL)
            {
                return null;
            }
            return this.Battery[(int)finger];
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
                        if(alarm)
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
            if(buffer < ModDef.Buffer_0 || buffer >= ModDef.Buffer_ALL)
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
                        if(alarm)
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
                if(BufferBat(i).Type > BatteryStatus.Invalid)
                {
                    count++;
                }
            }
            return count;
        }

        #endregion

        #region // 取放料计算

        /// <summary>
        /// 计算取位置
        /// </summary>
        /// <param name="pick"></param>
        /// <returns></returns>
        private bool CalcPickPos(ref PickPlacePos pick)
        {
            RunProcessCoolingSystem run = this.coolingSystem;
            if ((null != run) && (EventStatus.Require == GetEvent(run, EventList.CoolingSystemPickBattery)))
            {
                int col = run.BatteryLine.MaxCol - 1;
                for(int row = 0; row < run.BatteryLine.MaxRow - 1; row++)
                {
                    if(BatteryStatus.OK == run.BatteryLine.Battery[row, col].Type)
                    {
                        // 2个电池
                        if(BatteryStatus.OK == run.BatteryLine.Battery[row + 1, col].Type)
                        {
                            pick.SetData(MotorPosition.CoolingOffload_PickPos, row, col, ModDef.Finger_ALL, true);
                            return true;
                        }
                        // 1个电池
                        else
                        {
                            pick.SetData(MotorPosition.CoolingOffload_PickPos, row, col, ModDef.Finger_0, true);
                            return true;
                        }
                    }
                    // 仅有最后一行1个电池
                    else if(((row + 1) == (run.BatteryLine.MaxRow - 1)) && (BatteryStatus.OK == run.BatteryLine.Battery[row + 1, col].Type))
                    {
                        pick.SetData(MotorPosition.CoolingOffload_PickPos, row, col, ModDef.Finger_1, true);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 计算抓手及暂存配对位置
        /// </summary>
        /// <param name="curPos"></param>
        /// <returns></returns>
        private bool CalcFingerBufferMatchesPos(ref PickPlacePos curPos)
        {
            //两个爪手都无电池  缓存位有 取缓存位
            if((BatteryStatus.Invalid == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_1).Type))
            {
                if((BatteryStatus.OK == BufferBat(ModDef.Buffer_0).Type) && (BatteryStatus.OK == BufferBat(ModDef.Buffer_1).Type))
                {
                    curPos.SetData(MotorPosition.CoolingOffload_BufferPos2, 0, 0, ModDef.Finger_ALL, true);
                    return true;
                }
            }
            // 抓手0有，缓存有-》取缓存
            else if((BatteryStatus.OK == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.Invalid == FingerBat(ModDef.Finger_1).Type))
            {
                // 缓存0有-》抓手1取缓存0
                if(BatteryStatus.OK == BufferBat(ModDef.Buffer_0).Type)
                {
                    curPos.SetData(MotorPosition.CoolingOffload_BufferPos1, 0, 0, ModDef.Finger_1, true);
                    return true;
                }
                // 缓存1有-》抓手1取缓存1
                else if(BatteryStatus.OK == BufferBat(ModDef.Buffer_1).Type)
                {
                    curPos.SetData(MotorPosition.CoolingOffload_BufferPos2, 0, 0, ModDef.Finger_1, true);
                    return true;
                }
                // 缓存为空-》抓手0放缓存0
                else if(BatteryStatus.Invalid == BufferBat(ModDef.Buffer_0).Type)
                {
                    curPos.SetData(MotorPosition.CoolingOffload_BufferPos2, 0, 0, ModDef.Finger_0, false);
                    return true;
                }
            }
            // 抓手1有，缓存有-》取缓存
            else if((BatteryStatus.Invalid == FingerBat(ModDef.Finger_0).Type) && (BatteryStatus.OK == FingerBat(ModDef.Finger_1).Type))
            {
                // 缓存1有-》抓手0取缓存1
                if(BatteryStatus.OK == BufferBat(ModDef.Buffer_1).Type)
                {
                    curPos.SetData(MotorPosition.CoolingOffload_BufferPos3, 0, 0, ModDef.Finger_0, true);
                    return true;
                }
                // 缓存0有-》抓手0取缓存0
                else if(BatteryStatus.OK == BufferBat(ModDef.Buffer_0).Type)
                {
                    curPos.SetData(MotorPosition.CoolingOffload_BufferPos2, 0, 0, ModDef.Finger_0, true);
                    return true;
                }
                // 缓存为空-》抓手1放缓存1
                else if(BatteryStatus.Invalid == BufferBat(ModDef.Buffer_1).Type)
                {
                    curPos.SetData(MotorPosition.CoolingOffload_BufferPos2, 0, 0, ModDef.Finger_1, false);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 计算放位置
        /// </summary>
        /// <param name="place"></param>
        /// <returns></returns>
        private bool CalcPlacePos(ref PickPlacePos place)
        {
            if (FingerCount() > 0)
            {
                RunProcessOffloadLine run = this.offloadLine;
                if((null != run) && (EventStatus.Require == GetEvent(run, EventList.OffLoadLinePlaceBattery)))
                {
                    place.SetData(MotorPosition.CoolingOffload_PlacePos, 0, 0, ModDef.Finger_ALL, false);
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region // 防呆检查

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
            // 电机Z轴不在在安全位，禁止操作XYU 轴
            if((MotorX > -1 && Motors(MotorX) == motor) || (MotorY > -1 && Motors(MotorY) == motor) || (MotorU > -1 && Motors(MotorU) == motor))
            {
                if(!CheckMotorZPos(MotorPosition.OffLoad_SafetyPos))
                {
                    string msg = string.Format("Z轴不在安全位，禁止操作XYU电机！！！");
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
            }
            if((MotorZ > -1) && (Motors(MotorZ) == motor) && (this.MotorY > -1))
            {
                if((MotorMoveType.MotorMoveBackward == moveType)
                    || (MotorMoveType.MotorMoveHome == moveType)
                    || ((MotorMoveType.MotorMoveLocation == moveType) && (nLocation == (int)MotorPosition.CoolingOffload_SafetyPos)))
                {
                    return true;
                }
                else if(MotorMoveType.MotorMoveLocation == moveType)
                {
                    string posName, msg;
                    posName = msg = "";
                    float posValue, curValue;
                    posValue = curValue = 0.0f;
                    int[] mtr = { MotorX, MotorY, MotorU };
                    float[] fOffset = { pickPosXDis, 1.0f, 1.0f };
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
                    int input = ((int)MotorPosition.CoolingOffload_PlacePos == nLocation) ? IRotatePull : IRotatePush;
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
                else if(MotorMoveType.MotorMoveLocation == moveType)
                {
                    ShowMsgBox.ShowDialog("XYU轴电机未找到保存的位置，不能操作Z轴下降", MessageType.MsgWarning);
                    return false;
                }
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
            // 夹爪非安全位禁止旋转
            int outNum = -1;
            if(ORotatePush > -1 && Outputs(ORotatePush) == output)
            {
                outNum = ORotatePush;
            }
            else if(ORotatePull > -1 && Outputs(ORotatePull) == output)
            {
                outNum = ORotatePull;
            }
            if((outNum > -1) && !CheckMotorZPos(MotorPosition.CoolingOffload_SafetyPos))
            {
                string msg = string.Format("Z轴不在安全位，夹爪禁止旋转操作！！！");
                ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                return false;
            }
            // Y小于安全位，则不能操作旋转气缸
            if((this.MotorY > -1) 
                && ((this.ORotatePush > -1) && (Outputs(ORotatePush) == output) || ((this.ORotatePull > -1) && (Outputs(ORotatePull) == output))))
            {
                string posName, msg;
                posName = msg = "";
                float posValue, curValue;
                posValue = curValue = 0.0f;
                Motors(MotorY).GetLocation((int)MotorPosition.CoolingOffload_PickPos, ref posName, ref posValue);
                if ((int)MotorCode.MotorOK == Motors(MotorY).GetCurPos(ref curValue))
                {
                    if (curValue > posValue)
                    {
                        msg = string.Format("{0}】当前位置[{1}]＞{2}位置[{3}]，不能操作【{4} {5}】"
                            , Motors(MotorY).Name, curValue.ToString("#0.00"), posName, posValue.ToString("#0.00"), output.Num, output.Name);
                        ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion

    }
}

