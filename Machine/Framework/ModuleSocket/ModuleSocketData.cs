using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using SystemControlLibrary;

namespace Machine
{
    /// <summary>
    /// 序列化对象类
    /// </summary>
    [System.Serializable]
    public class ModuleSocketData
    {
        #region // 字段

        public object dataLock { get; private set; }             // 数据互斥锁

        // 模组所在设备数据
        public int machineID;                                    // 设备ID：MachineCtrl.MachineID，只初始化一次
        public int machineState;                                 // 设备状态：SystemControlLibrary.MCState枚举值
        public bool[] safeDoor;                                  // 设备安全门状态：true已打开，false已关闭
        public int onloadCount;                                  // 上料计数
        public int onScanNGCount;                                // 扫码NG计数
        public int offloadCount;                                 // 下料计数
        public int bakedNGCount;                                 // 烘烤NG计数
        public string mesBillNo;                                 // MES下发工单号
        public string mesBillNum;                                // MES下发工单号包含数量
        public long mesBillParamDate;                            // MES下发参数时间，与mesBillParam联动
        public Dictionary<string, MesRecipeStruct> mesBillParam;// MES下发参数

        // 模组数据
        public Dictionary<RunID, bool> moduleEnable;             // 模组使能
        public Dictionary<RunID, bool> moduleRunning;            // 模组运行状态
        public Dictionary<RunID, bool> deviceIsConnect;          // 干燥炉/机器人连接状态：true连接，false断开
        public Dictionary<RunID, int[]> cavityState;             // 干燥炉工作状态
        public Dictionary<RunID, uint[]> cavityTime;             // 干燥炉工作时间
        public Dictionary<RunID, bool[]> cavityEnable;           // 干燥炉腔体使能：true启用，false禁用
        public Dictionary<RunID, bool[]> cavityPressure;         // 干燥炉腔体保压：true启用，false禁用
        public Dictionary<RunID, bool[]> cavityTransfer;         // 干燥炉腔体转移：true启用，false禁用
        public Dictionary<RunID, int[]> cavitySamplingCycle;     // 腔体抽检周期：每N次放一次假电池夹具抽检
        public Dictionary<RunID, int[]> cavityHeartCycle;        // 腔体加热次数：当前第N次加热
        public Dictionary<RunID, double[,]> waterContentValue;   // 腔体水含量值
        public Dictionary<RunID, bool[]> pltPosEnable;           // 上下料/缓存架夹具位使能：true启用，false禁用
        public Dictionary<RunID, int[]> pltPosSenser;            // 夹具位传感器状态：0未知，1为OFF，2为ON，3错误（和enum OvenStatus枚举中夹具状态对应）
        public Dictionary<RunID, Pallet[]> pallet;               // 夹具数据
        public Dictionary<RunID, Battery[]> battery;             // 电池数据
        public Dictionary<RunID, BatteryLine> batteryLine;       // 电池线数据（冷却系统电池）
        public Dictionary<RunID, RobotActionInfo[]> robotAction; // 机器人当前动作：0自动，1手动
        public Dictionary<RunID, bool> robotRunning;             // 机器人运行中：true运动中，false停止中
        public Dictionary<RunID, ModuleEvent[]> moduleEvent;     // 模组事件

        #endregion


        #region // 方法

        /// <summary>
        /// 初始化空实例：调用CreateData()创建数据
        /// </summary>
        public ModuleSocketData()
        {
            this.dataLock = new object();
        }

        /// <summary>
        /// 创建所有数据
        /// </summary>
        public void CreateData()
        {
            lock(this.dataLock)
            {
                this.machineID = MachineCtrl.GetInstance().MachineID;
                this.machineState = (int)MCState.MCInvalidState;
                this.safeDoor = new bool[(int)SystemIO.SafeDoorIO];
                for(int i = 0; i < this.safeDoor.Length; i++)
                {
                    this.safeDoor[i] = true;
                }
                this.mesBillParam = new Dictionary<string, MesRecipeStruct>();
                this.moduleEnable = new Dictionary<RunID, bool>();
                this.moduleRunning = new Dictionary<RunID, bool>();
                this.deviceIsConnect = new Dictionary<RunID, bool>();
                this.cavityState = new Dictionary<RunID, int[]>();
                this.cavityTime = new Dictionary<RunID, uint[]>();
                this.cavityEnable = new Dictionary<RunID, bool[]>();
                this.cavityPressure = new Dictionary<RunID, bool[]>();
                this.cavityTransfer = new Dictionary<RunID, bool[]>();
                this.cavitySamplingCycle = new Dictionary<RunID, int[]>();
                this.cavityHeartCycle = new Dictionary<RunID, int[]>();
                this.waterContentValue = new Dictionary<RunID, double[,]>();
                this.pltPosEnable = new Dictionary<RunID, bool[]>();
                this.pltPosSenser = new Dictionary<RunID, int[]>();
                this.pallet = new Dictionary<RunID, Pallet[]>();
                this.battery = new Dictionary<RunID, Battery[]>();
                this.batteryLine = new Dictionary<RunID, BatteryLine>();
                this.robotAction = new Dictionary<RunID, RobotActionInfo[]>();
                this.robotRunning = new Dictionary<RunID, bool>();
                this.moduleEvent = new Dictionary<RunID, ModuleEvent[]>();
            }
            Release();
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        public void Release()
        {
            try
            {
                lock(this.dataLock)
                {
                    this.machineState = (int)MCState.MCInvalidState;
                    for(int i = 0; i < this.safeDoor.Length; i++)
                    {
                        this.safeDoor[i] = true;
                    }
                    this.onloadCount = 0;
                    this.onScanNGCount = 0;
                    this.offloadCount = 0;
                    this.bakedNGCount = 0;
                    this.mesBillNo = "";
                    this.mesBillNum = "";
                    this.mesBillParamDate = 0;
                    this.mesBillParam.Clear();

                    RunID[] runId = this.moduleEnable.Keys.ToArray();
                    // 遍历模组
                    for(int i = 0; i < runId.Length; i++)
                    {
                        this.moduleEnable[runId[i]] = false;
                        this.moduleRunning[runId[i]] = false;
                        this.deviceIsConnect[runId[i]] = false;
                        if(this.cavityState.ContainsKey(runId[i]))
                        {
                            // 遍历腔体
                            for(int j = 0; j < this.cavityState[runId[i]].Length; j++)
                            {
                                this.cavityState[runId[i]][j] = (int)CavityStatus.Unknown;
                                this.cavityTime[runId[i]][j] = 0;
                                this.cavityEnable[runId[i]][j] = false;
                                this.cavityPressure[runId[i]][j] = false;
                                this.cavityTransfer[runId[i]][j] = false;
                                this.cavitySamplingCycle[runId[i]][j] = 1;
                                this.cavityHeartCycle[runId[i]][j] = 1;
                                if(null != this.waterContentValue[runId[i]])
                                {
                                    // 遍历腔体的两个水含量
                                    for(int idx = 0; idx < 2; idx++)
                                    {
                                        this.waterContentValue[runId[i]][j, idx] = 0.0;
                                    }
                                }
                            }
                        }
                        if(null != this.pltPosEnable[runId[i]])
                        {
                            for(int j = 0; j < this.pltPosEnable[runId[i]].Length; j++)
                            {
                                this.pltPosEnable[runId[i]][j] = false;
                            }
                        }
                        if(null != this.pltPosSenser[runId[i]])
                        {
                            for(int j = 0; j < this.pltPosSenser[runId[i]].Length; j++)
                            {
                                this.pltPosSenser[runId[i]][j] = (int)OvenStatus.Unknown;
                            }
                        }
                        if(null != this.pallet[runId[i]])
                        {
                            for(int j = 0; j < this.pallet[runId[i]].Length; j++)
                            {
                                this.pallet[runId[i]][j].Release();
                            }
                        }
                        if(null != this.battery[runId[i]])
                        {
                            for(int j = 0; j < this.battery[runId[i]].Length; j++)
                            {
                                this.battery[runId[i]][j].Release();
                            }
                        }
                        if(null != this.batteryLine[runId[i]])
                        {
                            this.batteryLine[runId[i]].Release();
                        }
                        if(null != this.moduleEvent[runId[i]])
                        {
                            for(int j = 0; j < this.moduleEvent[runId[i]].Length; j++)
                            {
                                this.moduleEvent[runId[i]][j].State = EventStatus.Invalid;
                            }
                        }
                        if(null != this.robotAction[runId[i]])
                        {
                            for(int j = 0; j < this.robotAction[runId[i]].Length; j++)
                            {
                                this.robotAction[runId[i]][j].Release();
                            }
                        }
                        this.robotRunning[runId[i]] = true;
                    }
                }
            }
            catch(System.Exception ex)
            {
                Def.WriteLog("ModuleSocketData.Release()", $"{ex.Message}\r\n{ex.StackTrace}", HelperLibrary.LogType.Error);
            }
        }

        /// <summary>
        /// 序列化本类数据至buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public int Serialize(ref byte[] buffer)
        {
            try
            {
                using(MemoryStream memory = new MemoryStream())
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    lock(this.dataLock)
                    {
                        bf.Serialize(memory, this);
                    }
                    memory.Seek(0, SeekOrigin.Begin);
                    memory.Flush();
                    return memory.Read(buffer, 0, Convert.ToInt32(memory.Length));
                }
            }
            catch(System.Exception ex)
            {
                Trace.WriteLine("ModuleSocketData.Serialize error : " + ex.Message);
            }
            return 0;
        }

        /// <summary>
        /// 反序列化buffer数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static object Deserialize(byte[] buffer, int size)
        {
            try
            {
                using(MemoryStream memory = new MemoryStream())
                {
                    memory.Seek(0, SeekOrigin.Begin);
                    memory.Write(buffer, 0, size);
                    memory.Flush();
                    BinaryFormatter bf = new BinaryFormatter();
                    if(memory.Capacity > 0)
                    {
                        memory.Position = 0;
                        return bf.Deserialize(memory);
                    }
                }
            }
            catch(System.Exception ex)
            {
                Trace.WriteLine("ModuleSocketData.Deserialize error : " + ex.Message);
            }
            return null;
        }

        #endregion

    }
}
