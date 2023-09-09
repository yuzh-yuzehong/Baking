namespace Machine
{
    /// <summary>
    /// 干燥炉腔体工艺设置参数
    /// </summary>
    public struct CavityParameter
    {
        #region // 字段

        public float SetTempValue;                // 1)设定温度：摄氏度
        public float TempUpperlimit;              // 2)温度上限：摄氏度
        public float TempLowerlimit;              // 3)温度下限：摄氏度
        public uint PreheatTime;                  // 4)预热时间：分钟
        public uint VacHeatTime;                  // 5)真空加热时间：分钟
        public uint OpenDoorBlowTime;             // 6)开门破真空时长：分钟
        public uint OpenDoorVacPressure;          // 7)开门真空压力：Pa
        public uint AStateVacTime;                // 8)A状态抽真空时间：分钟
        public uint AStateVacPressure;            // 9)A状态真空压力：Pa
        public uint BStateVacTime;                // 10)B状态抽真空时间：分钟
        public uint BStateVacPressure;            // 11)B状态真空压力：Pa
        public uint BStateBlowAirTime;            // 12)呼吸充干燥气时间：分钟
        public uint BStateBlowAirPressure;        // 13)呼吸充干燥气压力：Pa
        public uint BStateBlowAirKeepTime;        // 14)呼吸充干燥气保持时间：分钟
        public uint BreathTimeInterval;           // 15)呼吸时间间隔：分钟
        public uint BreathCycleTimes;             // 16)呼吸循环次数：次
        public uint HeatPlate;                    // 17)发热板数：块
        public uint MaxNGHeatPlate;               // 18)最大NG发热板数：块
        public uint HeatPreVacTime;               // 19)加热前抽真空时间：分钟
        public uint HeatPreBlow;                  // 20)加热前充干燥气压力：Pa

        #endregion

        #region // 方法

        public void Release()
        {
            this.SetTempValue = 0;
            this.TempUpperlimit = 0;
            this.TempLowerlimit = 0;
            this.PreheatTime = 0;
            this.VacHeatTime = 0;
            this.OpenDoorBlowTime = 0;
            this.OpenDoorVacPressure = 0;
            this.AStateVacTime = 0;
            this.AStateVacPressure = 0;
            this.BStateVacTime = 0;
            this.BStateVacPressure = 0;
            this.BStateBlowAirTime = 0;
            this.BStateBlowAirPressure = 0;
            this.BStateBlowAirKeepTime = 0;
            this.BreathTimeInterval = 0;
            this.BreathCycleTimes = 0;
            this.HeatPlate = 0;
            this.MaxNGHeatPlate = 0;
            this.HeatPreVacTime = 0;
            this.HeatPreBlow = 0;
        }

        /// <summary>
        /// 从sourceParameter拷贝内容到本对象
        /// </summary>
        /// <param name="sourceParameter"></param>
        public void Copy(CavityParameter sourceParameter)
        {
            this.SetTempValue = sourceParameter.SetTempValue;
            this.TempUpperlimit = sourceParameter.TempUpperlimit;
            this.TempLowerlimit = sourceParameter.TempLowerlimit;
            this.PreheatTime = sourceParameter.PreheatTime;
            this.VacHeatTime = sourceParameter.VacHeatTime;
            this.OpenDoorBlowTime = sourceParameter.OpenDoorBlowTime;
            this.OpenDoorVacPressure = sourceParameter.OpenDoorVacPressure;
            this.AStateVacTime = sourceParameter.AStateVacTime;
            this.AStateVacPressure = sourceParameter.AStateVacPressure;
            this.BStateVacTime = sourceParameter.BStateVacTime;
            this.BStateVacPressure = sourceParameter.BStateVacPressure;
            this.BStateBlowAirTime = sourceParameter.BStateBlowAirTime;
            this.BStateBlowAirPressure = sourceParameter.BStateBlowAirPressure;
            this.BStateBlowAirKeepTime = sourceParameter.BStateBlowAirKeepTime;
            this.BreathTimeInterval = sourceParameter.BreathTimeInterval;
            this.BreathCycleTimes = sourceParameter.BreathCycleTimes;
            this.HeatPlate = sourceParameter.HeatPlate;
            this.MaxNGHeatPlate = sourceParameter.MaxNGHeatPlate;
            this.HeatPreVacTime = sourceParameter.HeatPreVacTime;
            this.HeatPreBlow = sourceParameter.HeatPreBlow;
        }

        #endregion
    }

    public class CavityData
    {
        #region // 字段

        // 工艺参数
        public CavityParameter parameter;

        // 干燥炉状态
        public short doorState;              // 炉门状态
        public short safetyCurtain;          // 安全光幕
        public short[] palletState;          // 夹具状态
        public short[] heatingState;         // 夹具加热状态
        public short vacValveState;          // 真空阀状态
        public short blowValveState;         // 破真空阀状态
        public uint workTime;                // 工作时间
        public uint workState;               // 工作状态
        public uint pressureState;           // 保压状态
        public uint vacPressure;             // 真空压力
        public short faultReset;             // 故障复位
        public short mcDoorState;            // 调度设备门禁状态：发送
        public double[,,] tempValue;         // 温度值：夹具 - 控温/巡检 - 发热板通道

        // 干燥炉报警信息
        public bool doorAlarm;              // 炉门报警
        public bool vacAlarm;               // 真空报警
        public double vacAlarmValue;        // 真空报警值
        public bool blowAlarm;              // 破真空报警
        public bool vacuometerAlarm;        // 真空计报警
        public short[,] tempAlarm;          // 温度报警：夹具-发热板
        public double[,] tempAlarmValue;    // 温度报警温度值：夹具-发热板
        public bool faultAlarm;             // 系统故障报警
        public bool[] controlAlarm;         // 机械温控报警：夹具
        public bool[] pallletAlarm;         // 夹具放平检测报警：夹具
        #endregion


        #region // 方法

        public CavityData()
        {
            Release();
        }

        public void Release()
        {
            this.parameter.Release();
            this.doorState = (short)OvenStatus.Unknown;
            this.safetyCurtain = (short)OvenStatus.Unknown;
            if (null == this.palletState)
            {
                this.palletState = new short[(int)OvenRowCol.MaxCol];
            }
            for(int i = 0; i < this.palletState.Length; i++)
            {
                this.palletState[i] = (short)OvenStatus.Unknown;
            }
            if(null == this.heatingState)
            {
                this.heatingState = new short[(int)OvenRowCol.MaxCol];
            }
            for(int i = 0; i < this.heatingState.Length; i++)
            {
                this.heatingState[i] = (short)OvenStatus.Unknown;
            }
            this.vacValveState = (short)OvenStatus.Unknown;
            this.blowValveState = (short)OvenStatus.Unknown;
            this.workTime = 0;
            this.workState = (uint)OvenStatus.Unknown;
            this.pressureState = (uint)OvenStatus.Unknown;
            this.vacPressure = (uint)OvenStatus.Unknown;
            this.faultReset = (short)OvenStatus.Unknown;
            this.mcDoorState = (short)OvenStatus.Unknown;
            if(null == this.tempValue)
            {
                this.tempValue = new double[(int)OvenRowCol.MaxCol, 2, (int)OvenInfoCount.HeatPanelCount];
            }
            for(int idx0 = 0; idx0 < this.tempValue.GetLength(0); idx0++)
            {
                for(int idx1 = 0; idx1 < this.tempValue.GetLength(1); idx1++)
                {
                    for(int idx2 = 0; idx2 < this.tempValue.GetLength(2); idx2++)
                    {
                        this.tempValue[idx0, idx1, idx2] = 0.0;
                    }
                }
            }
            this.doorAlarm = false;
            this.vacAlarm = false;
            this.vacAlarmValue = 0;
            this.blowAlarm = false;
            this.vacuometerAlarm = false;
            if (null == this.tempAlarm)
            {
                this.tempAlarm = new short[(int)OvenRowCol.MaxCol, (int)OvenInfoCount.HeatPanelCount];
            }
            for(int idx0 = 0; idx0 < this.tempAlarm.GetLength(0); idx0++)
            {
                for(int idx1 = 0; idx1 < this.tempAlarm.GetLength(1); idx1++)
                {
                    this.tempAlarm[idx0, idx1] = (short)OvenTmpAlarm.Normal;
                }
            }
            if(null == this.tempAlarmValue)
            {
                this.tempAlarmValue = new double[(int)OvenRowCol.MaxCol, (int)OvenInfoCount.HeatPanelCount];
            }
            for(int idx0 = 0; idx0 < this.tempAlarmValue.GetLength(0); idx0++)
            {
                for(int idx1 = 0; idx1 < this.tempAlarmValue.GetLength(1); idx1++)
                {
                    this.tempAlarmValue[idx0, idx1] = 0.0;
                }
            }
            this.faultAlarm = false;
            if(null == this.controlAlarm)
            {
                this.controlAlarm = new bool[(int)OvenRowCol.MaxCol];
            }
            for(int idx = 0; idx < this.controlAlarm.Length; idx++)
            {
                this.controlAlarm[idx] = false;
            }
            if(null == this.pallletAlarm)
            {
                this.pallletAlarm = new bool[(int)OvenRowCol.MaxCol];
            }
            for(int idx = 0; idx < this.pallletAlarm.Length; idx++)
            {
                this.pallletAlarm[idx] = false;
            }
        }

        public void Copy(CavityData sourceCavity)
        {
            this.parameter.Copy(sourceCavity.parameter);
            this.doorState = sourceCavity.doorState;
            this.safetyCurtain = sourceCavity.safetyCurtain;
            for(int i = 0; i < this.palletState.Length; i++)
            {
                this.palletState[i] = sourceCavity.palletState[i];
            }
            for(int i = 0; i < this.heatingState.Length; i++)
            {
                this.heatingState[i] = sourceCavity.heatingState[i];
            }
            this.vacValveState = sourceCavity.vacValveState;
            this.blowValveState = sourceCavity.blowValveState;
            this.workTime = sourceCavity.workTime;
            this.workState = sourceCavity.workState;
            this.pressureState = sourceCavity.pressureState;
            this.vacPressure = sourceCavity.vacPressure;
            this.faultReset = sourceCavity.faultReset;
            this.mcDoorState = sourceCavity.mcDoorState;
            for(int idx0 = 0; idx0 < this.tempValue.GetLength(0); idx0++)
            {
                for(int idx1 = 0; idx1 < this.tempValue.GetLength(1); idx1++)
                {
                    for(int idx2 = 0; idx2 < this.tempValue.GetLength(2); idx2++)
                    {
                        this.tempValue[idx0, idx1, idx2] = sourceCavity.tempValue[idx0, idx1, idx2];
                    }
                }
            }
            this.doorAlarm = sourceCavity.doorAlarm;
            this.vacAlarm = sourceCavity.vacAlarm;
            this.vacAlarmValue = sourceCavity.vacAlarmValue;
            this.blowAlarm = sourceCavity.blowAlarm;
            this.vacuometerAlarm = sourceCavity.vacuometerAlarm;
            for(int idx0 = 0; idx0 < this.tempAlarm.GetLength(0); idx0++)
            {
                for(int idx1 = 0; idx1 < this.tempAlarm.GetLength(1); idx1++)
                {
                    this.tempAlarm[idx0, idx1] = sourceCavity.tempAlarm[idx0, idx1];
                }
            }
            for(int idx0 = 0; idx0 < this.tempAlarmValue.GetLength(0); idx0++)
            {
                for(int idx1 = 0; idx1 < this.tempAlarmValue.GetLength(1); idx1++)
                {
                    this.tempAlarmValue[idx0, idx1] = sourceCavity.tempAlarmValue[idx0, idx1];
                }
            }
            this.faultAlarm = sourceCavity.faultAlarm;
            for(int idx = 0; idx < this.controlAlarm.Length; idx++)
            {
                this.controlAlarm[idx] = sourceCavity.controlAlarm[idx];
                this.pallletAlarm[idx] = sourceCavity.pallletAlarm[idx];
            }
        }
        #endregion
    }

    public class DryingOvenData
    {
        #region // 字段

        private object dataLock;            // 互斥锁
        public bool DataError;              // 数据获取出错

        // 和干燥炉交互数据
        public short RemoteState;           // 干燥炉远程状态：1关闭，2打开
        public short MCDoorState;           // 调度门禁状态：1关闭，2打开
        public CavityData[] CavityDatas;    // 腔体数据

        #endregion


        #region // 方法

        public DryingOvenData()
        {
            this.dataLock = new object();
            if(null == this.CavityDatas)
            {
                this.CavityDatas = new CavityData[(int)OvenRowCol.MaxRow];
                for(int i = 0; i < (int)OvenRowCol.MaxRow; i++)
                {
                    this.CavityDatas[i] = new CavityData();
                }
            }
            Release();
        }

        public void Release()
        {
            lock(dataLock)
            {
                this.DataError = false;
                this.RemoteState = 0;
                this.MCDoorState = 0;
                for(int i = 0; i < (int)OvenRowCol.MaxRow; i++)
                {
                    this.CavityDatas[i].Release();
                }
            }
        }

        public void Copy(DryingOvenData sourceData)
        {
            lock(dataLock)
            {
                this.DataError = sourceData.DataError;
                this.RemoteState = sourceData.RemoteState;
                this.MCDoorState = sourceData.MCDoorState;
                for(int i = 0; i < (int)OvenRowCol.MaxRow; i++)
                {
                    this.CavityDatas[i].Copy(sourceData.CavityDatas[i]);
                }
            }
        }
        #endregion
    }
}
