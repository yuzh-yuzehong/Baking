namespace Machine
{
    // 机器人类型
    public enum RobotType
    {
        ABB = 0,         // ABB
        KUKA,            // KUKA
        FANUC,           // FANUC
        END,
    };

    // 机器人动作指令
    public enum RobotOrder
    {
        // 动作指令
        HOME = 0,         // HOME归位
        MOVE,             // MOVE移动
        DOWN,             // DOWN下降
        UP,               // UP上升
        PICKIN,           // 取进
        PICKOUT,          // 取出
        PLACEIN,          // 放进
        PLACEOUT,         // 放出
        POS,              // POS查询机器人位置

        // 发送结束标志指令
        END,              // 机器人发送指令完成

        // 动作反馈指令 
        MOVING,           // 移动中
        FINISH,           // 移动完成
        TIMEOUT,          // 移动超时
        INVALID,          // 结果无效（命令与结果不匹配）
        ERR,              // 结果错误（命令与结果不匹配）

        ORDER_END,
    };

    // 机器人指令样式
    public enum RobotCmdFormat
    {
        Station = 0,       // 工位
        StationRow,        // 行
        StationCol,        // 列
        Speed,             // 速度
        Order,             // 动作
        Result,            // 执行结果

        End,               // 指令结束：最大数量
    };

    // 机器人ID
    public enum RobotIndexID
    {
        Invalid = -1,
        Onload = 0,        // 上料机器人
        Transfer,          // 调度机器人
        End,
    };

    /// <summary>
    /// RunProcessOnloadRobot 上料机器人工位
    /// </summary>
    public enum OnloadRobotStation
    {
        InvalidStatioin = 0,  // 无效
        HomeStatioin,         // 回零位
        OnloadLine,           // 来料取料位
        ScanPalletCode_0,     // 上料夹具扫码1
        ScanPalletCode_1,     // 上料夹具扫码2
        PalletStation_0,      // 上料夹具1
        PalletStation_1,      // 上料夹具2
        BufferStation,        // 暂存工位
        OnloadNGOutput,       // NG电池输出
        OnloadFakeScan,       // 假电池扫码
        OnloadFake,           // 假电池输入

        StationEnd,           // 结束
    };

    /// <summary>
    /// RunProcessRobotTransfer 调度机器人工位
    /// </summary>
    public enum TransferRobotStation
    {
        InvalidStatioin = 0,  // 无效
        OnloadStation,        // 上料工位
        PalletBuffer,         // 夹具缓存架
        ManualOperate,        // 人工操作台
        DryOven_0,            // 干燥炉1
        DryOven_All = DryOven_0 + OvenInfoCount.OvenCount - 1,           // 干燥炉结束
        OffloadStation,       // 下料

        StationEnd,           // 结束
    };

    /// <summary>
    /// 机器人动作信息
    /// </summary>
    [System.Serializable]
    public class RobotActionInfo
    {
        public int station;             // 工位
        public int row;                 // 行
        public int col;                 // 列
        public RobotOrder order;        // 动作指令
        public string stationName;      // 工位名

    	public RobotActionInfo()
        {
            Release();
        }

        public void Release()
        {
            this.station = 0;
            this.row = 0;
            this.col = 0;
            this.order = RobotOrder.INVALID;
            this.stationName = "无效工位";
        }

        public void Copy(RobotActionInfo sourceAction)
        {
            this.station = sourceAction.station;
            this.row = sourceAction.row;
            this.col = sourceAction.col;
            this.order = sourceAction.order;
            this.stationName = sourceAction.stationName;
        }

        public void SetData(int curStation, int curRow, int curCol, RobotOrder curOrder, string curStaionName)
        {
            this.station = curStation;
            this.row = curRow;
            this.col = curCol;
            this.order = curOrder;
            this.stationName = curStaionName;
        }
    };

    public class RobotDef
    {
        #region // 中文名称描述

        /// <summary>
        /// 机器人指令名
        /// </summary>
        public static string[] RobotOrderName = new string[]
        {
            "回零",
            "移动",
            "下降",
            "上升",
            "取进",
            "取出",
            "放进",
            "放出",
            "查询位置",

            "指令结束标识",

            "动作中",
            "完成",
            "超时",
            "无效",
            "错误",
        };

        /// <summary>
        /// 机器人ID名
        /// </summary>
        public static string[] RobotIDName = new string[]
        {
            "上料机器人",
            "调度机器人",
        };
        #endregion

    }
}
