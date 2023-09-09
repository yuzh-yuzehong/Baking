namespace Machine
{
    public enum OvenInfoCount
    {
        OvenCount = 9,         // 设备中的干燥炉总数

        HeatPanelCount = 4,    // 夹具发热板数
    }

    /// <summary>
    /// 干燥炉最大行列
    /// </summary>
    public enum OvenRowCol
    {
        MaxRow = 4,         // 行，炉腔
        MaxCol = 2,         // 列，一个炉腔中的夹具位
    }

    /// <summary>
    /// 干燥炉腔体状态
    /// </summary>
    public enum CavityStatus
    {
        Unknown = -1,       // 未知状态
        Normal = 0,         // 正常状态
        Heating,            // 加热状态
        WaitDetect,         // 等待测试
        WaitResult,         // 等待结果
        WaitRebaking,       // 等待回炉
        Maintenance,        // 维修状态
    }
    /// <summary>
    /// 干燥炉转盘标记
    /// </summary>
    public enum BakingNGType
    {
        Normal = 0,         // 正常加热状态,
        Abnormal,           // 异常加热状态
    }

    /// <summary>
    /// 干燥炉的信息状态
    /// </summary>
    public enum OvenStatus
    {
        Unknown = 0,        // 未知：未获取

        DoorClose = 1,      // 炉门关
        DoorOpen,           // 炉门开
        DoorAction,         // 炉门动作中

        SafetyCurtainOn = 1,// 安全光幕ON
        SafetyCurtainOff,   // 安全光幕OFF

        WorkStop = 1,       // 待机状态
        WorkStart,          // 工作状态
        WorkOutage,         // 异常断电

        VacClose = 1,       // 真空阀关闭
        VacOpen,            // 真空阀打开

        BlowClose = 1,      // 破真空阀关闭
        BlowOpen,           // 破真空阀打开

        PressureClose = 1,  // 保压关闭
        PressureOpen,       // 保压打开

        PalletNot = 1,      // 无夹具
        PalletHave,         // 有夹具
        PalletErrror,       // 夹具感应器状态错误

        HeatClose = 1,      // 加热关
        HeatOpen,           // 加热开

        FaultResetOn = 1,   // 故障复位On（触发）
        FaultResetOff,      // 故障复位Off

        RemoteClose = 1,    // 远程状态关
        RemoteOpen,         // 远程状态开

        McDoorClose = 1,    // 调度设备门禁关
        McDoorOpen,         // 调度设备门禁开
    }

    /// <summary>
    /// 干燥炉温度报警信息状态
    /// </summary>
    public enum OvenTmpAlarm
    {
        Normal = 0,         // 正常
        LowTmp,             // 低温
        OverTmp,            // 超温（超过温度偏移较小）
        HighTmp,            // 超高温（超过温度偏移太多）
        Exceptional,        // 信号异常
        Difference,         // 温差异常
        End,
    }

    /// <summary>
    /// 干燥炉命令索引
    /// </summary>
    public enum DryOvenCmd
    {
        SenserState = 0,            // 传感器状态（读取）
        RunState,                   // 工作状态（读取）
        RunTemp,                    // 实时温度（读取）
        AlarmValue,                 // 报警值（温度、真空）（读取）
        AlarmState,                 // 报警状态（读取）
        GetParameter,               // 工艺参数（读）
        SystemInfo,                 // 系统信息（读）

        SetParameter,               // 工艺参数（写）
        WorkStartStop,              // 加热启动/停止（写入）
        DoorOpenClose,              // 炉门打开/关闭（写入）
        VacOpenClose,               // 真空打开/关闭（写入）
        BlowOpenClose,              // 破真空打开/关闭（写入）
        PressureOpenClose,          // 保压打开/关闭（写入）
        FaultReset,                 // 故障复位（写入）
        SetMcDoor,                  // 调度设备门禁（写入）

        End,
    }

    /// <summary>
    /// 命令结构
    /// </summary>
    public struct DryOvenFinsCmd
    {
        public ZoneCode zone;         // 区域
        public short wordAddr;        // 字起地址
        public short wordInterval;    // 字地址间隔
        public short bitAddr;         // 位起地址
        public short bitInterval;     // 位地址间隔
        public short count;           // 总数量

        public DryOvenFinsCmd(ZoneCode zoneCode, short wordStartAddr, short wordAddrInterval, short bitStartAddr, short bitAddrInterval, short addrCount)
        {
            this.zone = zoneCode;
            this.wordAddr = wordStartAddr;
            this.wordInterval = wordAddrInterval;
            this.bitAddr = bitStartAddr;
            this.bitInterval = bitAddrInterval;
            this.count = addrCount;
        }
    };

}
