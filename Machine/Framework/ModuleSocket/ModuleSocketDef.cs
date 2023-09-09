namespace Machine
{
    /// <summary>
    /// MCS通讯的缓冲大小：Module Client Server Buffer
    /// </summary>
    enum MCSBuffer
    {
        Send = 1024 * 1024,
        Recv = 1024 * 1024,
    }

    /// <summary>
    /// 打包数据类型
    /// </summary>
    enum PacketType
    {
        ReadAll = 0xffff,                   // 读取所有数据
        SetPallet = 0x01 << 0,              // 设置夹具数据
        SetEvent = 0x01 << 1,               // 设置信号数据
        SetWaterContent = 0x01 << 2,        // 设置腔体水含量数据
    }

    public struct PacketHeader
    {
        public static byte[] header = new byte[4] { 0x4d, 0x43, 0x53, 0x00 };    // 4字节：头固定"MCS"：Module Client Server
        public uint cmdType;                    // 4字节：写的数据类型：PackDataType值
        public uint length;                     // 4字节：数据长度，包含10字节头
        public uint crcCode;                    // 4字节：CRC校验码
    }

    class ModuleSocketDef
    {
    }
}
