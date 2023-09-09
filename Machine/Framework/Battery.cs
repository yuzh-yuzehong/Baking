namespace Machine
{
    #region // 电池属性枚举
    
    /// <summary>
    /// 电池状态
    /// </summary>
    public enum BatteryStatus
    {
        Invalid = 0,        // 无效
        OK,                 // OK
        NG,                 // NG
        Fake,               // 假电池
        ReFake,             // 回炉假电池
        Detect,             // 待检测假电池
        FakeTag,            // 假电池标记，已被取走，实际无电池

        End,
    }

    /// <summary>
    /// 电池NG类型
    /// </summary>
    public enum BatteryNGStatus
    {
        Invalid = 0,              // 无效
        Scan = 0x01 << 0,         // 扫码NG
        MesNG = 0x01 << 1,        // MESNG
        LowTmp = 0x01 << 2,       // 低温NG
        OverTmp = 0x01 << 3,      // 超温NG
        HighTmp = 0x01 << 4,      // 超高温NG
        ExcTmp = 0x01 << 5,       // 信号异常
        DifTmp = 0x01 << 6,       // 温差异常
    };

    #endregion

    /// <summary>
    /// 电池类：禁用=操作拷贝数据，请使用.Copy()方法拷贝数据
    /// </summary>
    [System.Serializable]
    public class Battery
    {
        #region // 字段

        public BatteryStatus Type;         // 电池类型
        public BatteryNGStatus NGType;     // 电池NG状态
        public string Code;                // 电池条码

        #endregion

        #region // 方法

        public Battery()
        {
            Release();
        }

        /// <summary>
        /// 清除数据
        /// </summary>
        public void Release()
        {
            this.Type = BatteryStatus.Invalid;
            this.NGType = BatteryNGStatus.Invalid;
            this.Code = "";
        }

        /// <summary>
        /// 拷贝外部数据到本对象
        /// </summary>
        /// <param name="srcBattery">源数据</param>
        public void Copy(Battery srcBattery)
        {
            this.Type = srcBattery.Type;
            this.NGType = srcBattery.NGType;
            this.Code = srcBattery.Code;
        }

        #endregion
    }
}
