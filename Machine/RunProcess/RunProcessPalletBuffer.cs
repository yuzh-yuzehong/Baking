using HelperLibrary;
using System.Diagnostics;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    /// <summary>
    /// 夹具缓存架
    /// </summary>
    class RunProcessPalletBuffer : RunProcess
    {
        #region // 枚举：步骤，模组数据，报警列表

        protected new enum InitSteps
        {
            Init_DataRecover = 0,

            Init_CheckPalllet,

            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,

            // 等待取放完成
            Auto_WaitPickPlaceFinish,

            Auto_WorkEnd,
        }

        private enum ModDef
        {

        }
        #endregion

        #region // 字段，属性

        #region // IO

        private int[] IPalletKeepFlatLeft;      // 夹具放平检测左
        private int[] IPalletKeepFlatRight;     // 夹具放平检测右
        private int[] IPalletHasCheck;          // 夹具位有夹具检测
        private int[] IPalletInposCheck;        // 夹具到位检测

        #endregion

        #region // 电机
        #endregion

        #region // ModuleEx.cfg配置
        #endregion

        #region // 模组参数

        public bool[] BufferEnable { get; private set; }    // 缓存架使能：TRUE启用，FALSE禁用

        #endregion

        #region // 模组数据

        private EventList operateEvent;       // 当前操作事件

        #endregion

        #endregion

        public RunProcessPalletBuffer(int runId) : base(runId)
        {
            InitBatteryPalletSize(0, (int)ModuleMaxPallet.PalletBuffer);

            PowerUpRestart();

            InitParameter();
            // 参数
            for(int i = 0; i < this.BufferEnable.Length; i++)
            {
                InsertVoidParameter(("BufferEnable" + i), ((i + 1).ToString() + "层缓存架使能"), "缓存架使能：TRUE启用，FALSE禁用", BufferEnable[i], RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_OPER);
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
                        this.nextInitStep = InitSteps.Init_CheckPalllet;
                        break;
                    }
                case InitSteps.Init_CheckPalllet:
                    {
                        CurMsgStr("检查夹具状态", "Check sensor");
                        for(int i = 0; i < (int)ModuleMaxPallet.PalletBuffer; i++)
                        {
                            if(!PalletKeepFlat(i, (this.Pallet[i].State > PalletStatus.Invalid), true))
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
                Sleep(10);
            }

            switch((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");

                        #region // 设置检查取放请求

                        for(int idx = 0; idx < this.Pallet.Length; idx++)
                        {
                            EventList modEvent = EventList.Invalid;
                            EventStatus state = EventStatus.Invalid;
                        int pos =-1;
                            // 有空位 -》 请求放
                            if(PalletStatus.Invalid == this.Pallet[idx].State)
                            {
                                // 缓存架放空夹具
                                modEvent = EventList.PalletBufferPlaceEmptyPallet;
                                state = GetEvent(this, modEvent,ref pos);
                                if((EventStatus.Invalid == state) || (EventStatus.Finished == state)||(EventStatus.Require==state&&pos!=idx)) {
                                    SetEvent(this, modEvent, EventStatus.Require, idx);
                                }
                                // 缓存架放NG空夹具
                                modEvent = EventList.PalletBufferPlaceNGEmptyPallet;
                                state = GetEvent(this, modEvent,ref pos);
                                if((EventStatus.Invalid == state) || (EventStatus.Finished == state)||(EventStatus.Require==state&&pos!=idx)) {
                                    SetEvent(this, modEvent, EventStatus.Require, idx);
                                }
                            }
                            // 缓存架取空夹具
                            if((PalletStatus.OK == this.Pallet[idx].State) && this.Pallet[idx].IsEmpty())
                            {
                                modEvent = EventList.PalletBufferPickEmptyPallet;
                                state = GetEvent(this, modEvent,ref pos);
                                if((EventStatus.Invalid == state) || (EventStatus.Finished == state)||(EventStatus.Require==state&&pos!=idx)) {
                                    SetEvent(this, modEvent, EventStatus.Require, idx);
                                }
                            }
                            // 缓存架取NG空夹具
                            if((PalletStatus.NG == this.Pallet[idx].State) && this.Pallet[idx].IsEmpty())
                            {
                                modEvent = EventList.PalletBufferPickNGEmptyPallet;
                                pos =-1;
                                state = GetEvent(this, modEvent,ref pos);
                                if((EventStatus.Invalid == state) || (EventStatus.Finished == state)||(EventStatus.Require==state&&pos!=idx)) {
                                    SetEvent(this, modEvent, EventStatus.Require, idx);
                                }
                            }
                        }
                        #endregion

                        // 调度机器人响应
                        for(EventList i = EventList.PalletBufferPlaceEmptyPallet; i < EventList.PalletBufferPickPlaceEnd; i++)
                        {
                            if(EventStatus.Response == GetEvent(this, i))
                            {
                                this.operateEvent = i;
                                this.nextAutoStep = AutoSteps.Auto_WaitPickPlaceFinish;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                return;
                            }
                        }

                        break;
                    }
                case AutoSteps.Auto_WaitPickPlaceFinish:
                    {
                        CurMsgStr("等待取放操作完成", "wait pick or place pallet finish");
                        int pos = -1;
                        if(EventStatus.Response == GetEvent(this, this.operateEvent, ref pos))
                        {
                            SetEvent(this, this.operateEvent, EventStatus.Ready, pos);
                        }
                        else if(EventStatus.Finished == GetEvent(this, this.operateEvent, ref pos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep | SaveType.Pallet, pos);
                            break;
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

        #region // 运行数据读写

        public override void InitRunData()
        {
            this.operateEvent = EventList.Invalid;

            base.InitRunData();
        }

        public override void LoadRunData()
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);

            this.operateEvent = (EventList)iniStream.ReadInt(section, "operateEvent", (int)this.operateEvent);

            base.LoadRunData();
        }

        public override void SaveRunData(SaveType saveType, int index = -1)
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);

            if(SaveType.Variables == (SaveType.Variables & saveType))
            {
                iniStream.WriteInt(section, "operateEvent", (int)this.operateEvent);
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
            if(!base.InitializeConfig(module))
            {
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// 初始化通用组参数 + 模组参数
        /// </summary>
        protected override void InitParameter()
        {
            this.BufferEnable = new bool[(int)ModuleMaxPallet.PalletBuffer];
            this.BufferEnable.Initialize();

            base.InitParameter();
        }

        /// <summary>
        /// 读取通用组参数 + 模组参数
        /// </summary>
        /// <returns></returns>
        public override bool ReadParameter()
        {
            for(int i = 0; i < (int)BufferEnable.Length; i++)
            {
                this.BufferEnable[i] = ReadBoolParameter(this.RunModule, ("BufferEnable" + i), this.BufferEnable[i]);
            }

            return base.ReadParameter();
        }

        #endregion

        #region // IO及电机

        /// <summary>
        /// 初始化IO及电机
        /// </summary>
        protected override void InitModuleIOMotor()
        {
            int maxPlt = (int)ModuleMaxPallet.PalletBuffer;
            this.IPalletKeepFlatLeft = new int[maxPlt];
            this.IPalletKeepFlatRight = new int[maxPlt];
            this.IPalletHasCheck = new int[maxPlt];
            this.IPalletInposCheck = new int[maxPlt];
            for(int i = 0; i < maxPlt; i++)
            {
                this.IPalletKeepFlatLeft[i] = AddInput("IPalletKeepFlatLeft" + i);
                this.IPalletKeepFlatRight[i] = AddInput("IPalletKeepFlatRight" + i);
                this.IPalletHasCheck[i] = AddInput("IPalletHasCheck" + i);
                this.IPalletInposCheck[i] = AddInput("IPalletInposCheck" + i);
            }
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
            if(pltIdx < 0 || pltIdx > (int)ModuleMaxPallet.PalletBuffer)
            {
                return false;
            }
            if(!InputState(IPalletHasCheck[pltIdx], hasPlt)
                || !InputState(IPalletInposCheck[pltIdx], hasPlt)
                || !InputState(IPalletKeepFlatLeft[pltIdx], hasPlt)
                || !InputState(IPalletKeepFlatRight[pltIdx], hasPlt))
            {
                if(alarm)
                {
                    CheckInputState(IPalletHasCheck[pltIdx], hasPlt);
                    CheckInputState(IPalletInposCheck[pltIdx], hasPlt);
                    CheckInputState(IPalletKeepFlatLeft[pltIdx], hasPlt);
                    CheckInputState(IPalletKeepFlatRight[pltIdx], hasPlt);
                }
                return false;
            }
            return true;
        }

        #endregion
        
        #region // 添加删除夹具

        public override void ManualAddPallet(int pltIdx, int maxRow, int maxCol, PalletStatus pltState, BatteryStatus batState)
        {
            if (PalletStatus.OK == pltState)
            {
                this.Pallet[pltIdx].State = PalletStatus.OK;
                this.Pallet[pltIdx].SetRowCol(maxRow, maxCol);
                SaveRunData(SaveType.Pallet, pltIdx);
            }
        }

        public override void ManualClearPallet(int pltIdx)
        {
            this.Pallet[pltIdx].Release();
            SaveRunData(SaveType.Pallet, pltIdx);
        }
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
                SaveRunData(SaveType.AutoStep);
            }
        }
        #endregion
    }
}
