namespace Machine
{

    #region // Fins通讯类型

    /// <summary>
    /// Fins通讯类型
    /// </summary>
    public enum FinsType
    {
        Unknown,

        Tcp,
        Udp,

        End,
    }
    #endregion

    #region // Fins协议寄存器类型

    /// <summary>
    /// 寄存器类型，十六进制表示形式
    /// </summary>
    public enum ZoneCode
    {
        Invalid = 0x00,
        DMBit = 0x02,
        DMWord = 0x82,
        WRBit = 0x31,
        WRWord = 0xB1,
        CIOBit = 0x30,
        CIOWord = 0xB0,
    }
    #endregion

    #region // Fins协议指令的读写类型

    /// <summary>
    /// 区分指令的读写类型
    /// </summary>
    public enum RorW
    {
        Read = 0x0101,
        Write = 0x0102,
    }

    #endregion

}
