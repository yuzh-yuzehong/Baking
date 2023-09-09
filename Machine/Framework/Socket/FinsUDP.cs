using System;

namespace Machine
{
    /// <summary>
    /// FinsUDP通讯类
    /// </summary>
    public class FinsUDP : BaseThread
    {
        #region // 字段

        private UdpSocket client;       // 连接客户端
        private object rwLock;          // 数据锁
        private bool connected;         // 连接成功 && 握手成功
        private byte plcNode;           // PLC节点号
        private byte pcNode;            // PC节点号
        private int recvByteLen;        // 命令需要接收字节长度
        private int curRecvLen;         // 当前已接收长度
        private byte[] recvByte;        // 命令接收字节数据
        private bool recvFinish;        // 命令接收完成

        #endregion

        #region // FinsUDP 协议帧

        /// <summary>
        /// FINS 协议帧，UDP无握手
        /// </summary>
        public enum FinsFrame
        {
            // 协议主体
            ICF = 0,                               // 可以的值为：80(要求有回复)，81（不要求有回复）
            RSV,                                   // 默认 00
            GCT,                                   // 穿过的网络层数量：0层对应02；1层对应01；2层对应00
            DNA,                                   // 目的网络地址 00
            DA1,                                   // 目的节点地址：PLC IP地址的最后一位
            DA2,                                   // 目的单元地址 00
            SNA,                                   // 源网络地址 00
            SA1,                                   // 源节点地址：电脑IP最后一位
            SA2,                                   // 源单元地址 00
            SID,                                   // 站点ID
            RW_CMD,                                // 具体命令：0101（读）；0102 （写）
            MainEnd = RW_CMD + 2,                  // 协议主体结束

            // 读写请求
            RequireZone = MainEnd,                 // 请求区域代码
            WordAddr,                              // 字起首地址(字位置，整数部分)
            BitAddr = WordAddr + 2,                // 位起首地址(位位置，小数部分)
            DataCount,                             // 数量（处理多少个字或者位）
            RequireEnd = DataCount + 2,            // 读写请求结束

            // 读写响应
            ResponseCode = MainEnd,                // 读写响应结束码
            ResponseEnd = ResponseCode + 2,        // 读写响应结束
        };
        #endregion

        #region // 构造函数

        public FinsUDP()
        {
            this.client = new UdpSocket();
            this.rwLock = new object();
            this.connected = false;
            this.recvByte = new byte[5 * 1024];
            this.recvByteLen = 0;
            this.recvFinish = false;
        }

        #endregion

        #region // 命令构造

        private readonly byte[] Hander = new byte[]
        {
            0xC0, 0x00, 0x02, // FINS Udp 接收响应头
        };

        /// <summary>
        /// Fins读写指令生成
        /// </summary>
        /// <param name="rw">读写类型</param>
        /// <param name="mr">寄存器类型</param>
        /// <param name="mt">地址类型</param>
        /// <param name="addr">起始地址</param>
        /// <param name="offset">位地址：00-15,字地址则为00</param>
        /// <param name="count">地址个数,按位读写只能是1</param>
        /// <returns></returns>
        internal byte[] GetFinsCmd(RorW rw, ZoneCode zCode, short addr, short offset, short count)
        {
            byte[] array = new byte[(int)FinsFrame.RequireEnd];

            // Fins UDP命令 读写请求 
            array[(int)FinsFrame.ICF] = 0x80;                                                            // 可以的值为：80(要求有回复)，81（不要求有回复）
            array[(int)FinsFrame.RSV] = 0x00;                                                            // 默认 00
            array[(int)FinsFrame.GCT] = 0x02;                                                            // 穿过的网络层数量：0层对应02；1层对应01；2层对应00
            array[(int)FinsFrame.DNA] = 0x00;                                                            // 目的网络地址 00
            array[(int)FinsFrame.DA1] = plcNode;                                                         // 目的节点地址：PLC IP地址的最后一位
            array[(int)FinsFrame.DA2] = 0x00;                                                            // 目的单元地址 00
            array[(int)FinsFrame.SNA] = 0x00;                                                            // 源网络地址 00
            array[(int)FinsFrame.SA1] = pcNode;                                                          // 源节点地址：电脑IP最后一位
            array[(int)FinsFrame.SA2] = 0x00;                                                            // 源单元地址 00
            array[(int)FinsFrame.SID] = 0x00;                                                            // 站点ID
            array[(int)FinsFrame.RW_CMD] = 0x01;                                                         // 具体命令：0101（读）；0102 （写）
            array[(int)FinsFrame.RW_CMD + 1] = Convert.ToByte((RorW.Read == rw) ? 0x01 : 0x02);          // 具体命令：0101（读）；0102 （写）
            array[(int)FinsFrame.RequireZone] = Convert.ToByte(zCode);                                   // 请求区域代码
            array[(int)FinsFrame.WordAddr] = (byte)(addr / 256);                                         // 字起首地址(字位置，整数部分)
            array[(int)FinsFrame.WordAddr + 1] = (byte)(addr % 256);
            array[(int)FinsFrame.BitAddr] = (byte)(offset);                                              // 位起首地址(位位置，小数部分)
            array[(int)FinsFrame.DataCount] = (byte)(count / 256);                                       // 数量（处理多少个字或者位）
            array[(int)FinsFrame.DataCount + 1] = (byte)(count % 256);

            return array;
        }

        #endregion

        #region // 检查方法

        /// <summary>
        /// 检查命令头中的错误代码
        /// </summary>
        /// <param name="data">PLC命令数据</param>
        /// <returns>指示程序是否可以继续进行</returns>
        private bool CheckError(byte[] data)
        {
            if((data[(int)FinsFrame.RequireZone] != 0) || (data[(int)FinsFrame.RequireZone + 1] != 0))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 检查Fins头
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool CheckHead(byte[] data)
        {
            if((null == data) || (data.Length < (int)FinsFrame.MainEnd))
            {
                return false;
            }
            for(int i = 0; i < Hander.Length; i++)
            {
                if(data[i] != Hander[i])
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region // 接口方法 

        /// <summary>
        /// 连接状态
        /// </summary>
        /// <returns></returns>
        public bool IsConnect()
        {
            return this.connected;
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="ip">PLC的IP地址</param>
        /// <param name="port">PLC的端口号，默认9600</param>
        /// <param name="pcNodeID">PC节点号</param>
        /// <returns></returns>
        public bool Connect(string ip, int port, byte pcNodeID)
        {
            if(!TcpSocket.PingCheck(ip, 500))
            {
                return false;
            }
            if(this.client.Connect(ip, port))
            {
                InitThread(string.Format("{0}: {1} finsNode.{2} recv Task", ip, port, pcNodeID));
                this.pcNode = pcNodeID;
                string node = ip.Remove(0, ip.LastIndexOf('.') + 1);
                this.plcNode = Convert.ToByte(node);
                this.connected = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 断开连接，停止线程
        /// </summary>
        /// <returns></returns>
        public bool Disconnect()
        {
            this.connected = false;
            this.client.Disconnect();
            ReleaseThread();
            return true;
        }

        /// <summary>
        /// 获取IP信息
        /// </summary>
        /// <returns></returns>
        public string GetIPInfo()
        {
            return $"{this.client.GetIP()}:{this.client.GetPort()}";
        }

        /// <summary>
        /// 读值方法（多个连续位）
        /// </summary>
        /// <param name="zCode">地址类型枚举</param>
        /// <param name="wordAddr">字起始地址</param>
        /// <param name="bitAddr">位起始地址</param>
        /// <param name="count">个数</param>
        /// <param name="recvBuffer">返回值</param>
        /// <returns></returns>
        public bool ReadBits(ZoneCode zCode, short wordAddr, short bitAddr, short count, ref byte[] recvBuffer)
        {
            byte[] sBuf = GetFinsCmd(RorW.Read, zCode, wordAddr, bitAddr, count);
            byte[] rBuf = new byte[((int)FinsFrame.ResponseEnd + count * 2)];
            lock(this.rwLock)
            {
                if(SendAndWait(sBuf, sBuf.Length, ref rBuf, rBuf.Length))
                {
                    if(CheckError(rBuf))
                    {
                        Array.Copy(rBuf, (int)FinsFrame.ResponseEnd, recvBuffer, 0, count);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 读值方法（多个连续值）
        /// </summary>
        /// <param name="zCode">地址类型枚举</param>
        /// <param name="wordAddr">字起始地址</param>
        /// <param name="bitAddr">位起始地址</param>
        /// <param name="count">个数</param>
        /// <param name="recvBuffer">返回值</param>
        /// <returns></returns>
        public bool ReadWords(ZoneCode zCode, short wordAddr, short bitAddr, short count, ref byte[] recvBuffer)
        {
            byte[] sBuf = GetFinsCmd(RorW.Read, zCode, wordAddr, bitAddr, count);
            byte[] rBuf = new byte[((int)FinsFrame.ResponseEnd + count * 2)];
            lock(this.rwLock)
            {
                if(SendAndWait(sBuf, sBuf.Length, ref rBuf, rBuf.Length))
                {
                    if(CheckError(rBuf))
                    {
                        Array.Copy(rBuf, (int)FinsFrame.ResponseEnd, recvBuffer, 0, (count * 2));
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
		/// 写值方法（多个连续位值）
		/// </summary>
		/// <param name="zCode">地址类型枚举</param>
        /// <param name="wordAddr">字起始地址</param>
        /// <param name="bitAddr">位起始地址</param>
		/// <param name="count">个数</param>
		/// <param name="data">写入值</param>
		/// <returns></returns>
		public bool WriteBits(ZoneCode zCode, short wordAddr, short bitAddr, short count, byte[] data)
        {
            byte[] cmdBuf = GetFinsCmd(RorW.Write, zCode, wordAddr, bitAddr, count);
            byte[] sBuf = new byte[cmdBuf.Length + count];
            Array.Copy(cmdBuf, 0, sBuf, 0, cmdBuf.Length);
            Array.Copy(data, 0, sBuf, cmdBuf.Length, count);
            byte[] rBuf = new byte[(int)FinsFrame.ResponseEnd];
            lock(this.rwLock)
            {
                if(SendAndWait(sBuf, sBuf.Length, ref rBuf, rBuf.Length))
                {
                    if(CheckError(rBuf))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
		/// 写值方法（多个连续值）
		/// </summary>
		/// <param name="zCode">地址类型枚举</param>
        /// <param name="wordAddr">字起始地址</param>
        /// <param name="bitAddr">位起始地址</param>
		/// <param name="count">个数</param>
		/// <param name="data">写入值</param>
		/// <returns></returns>
		public bool WriteWords(ZoneCode zCode, short wordAddr, short bitAddr, short count, byte[] data)
        {
            byte[] cmdBuf = GetFinsCmd(RorW.Write, zCode, wordAddr, bitAddr, count);
            byte[] sBuf = new byte[cmdBuf.Length + (count * 2)];
            Array.Copy(cmdBuf, 0, sBuf, 0, cmdBuf.Length);
            Array.Copy(data, 0, sBuf, cmdBuf.Length, (count * 2));
            byte[] rBuf = new byte[(int)FinsFrame.ResponseEnd];
            lock(this.rwLock)
            {
                if(SendAndWait(sBuf, sBuf.Length, ref rBuf, rBuf.Length))
                {
                    if(CheckError(rBuf))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 发送数据并等待接收
        /// </summary>
        /// <param name="sendBuffer"></param>
        /// <param name="sendLen"></param>
        /// <param name="recvBuffer"></param>
        /// <param name="recvLen"></param>
        /// <returns></returns>
        private bool SendAndWait(byte[] sendBuffer, int sendLen, ref byte[] recvBuffer, int recvLen)
        {
            this.recvFinish = false;
            Array.Clear(this.recvByte, 0, this.recvByte.Length);
            this.recvByteLen = recvLen;

            if(this.client.Send(sendBuffer, sendLen))
            {
                DateTime startTime = DateTime.Now;
                while((DateTime.Now - startTime).TotalSeconds < 3)
                {
                    if(GetReceiveResult(ref recvBuffer))
                    {
                        return true;
                    }
                    Sleep(1);
                }
            }
            return false;
        }

        /// <summary>
        /// 接收数据线程
        /// </summary>
        protected override void RunWhile()
        {
            if(!this.client.IsConnect())
            {
                return;
            }

            byte[] recvBuf = new byte[5 * 1024];
            int recvSize = this.client.Recv(ref recvBuf, recvBuf.Length);
            if(recvSize > 0)
            {
                if(CheckHead(recvBuf))
                {
                    Array.Copy(recvBuf, 0, recvByte, 0, recvSize);
                    this.curRecvLen = recvSize;
                    if(this.curRecvLen >= this.recvByteLen)
                    {
                        this.curRecvLen = 0;
                        this.recvFinish = true;
                    }
                }
                else if(this.curRecvLen > 0)
                {
                    if(this.curRecvLen + recvSize > this.recvByte.Length)
                    {
                        this.curRecvLen = 0;
                        Array.Clear(this.recvByte, 0, this.recvByte.Length);
                    }
                    else
                    {
                        Array.Copy(recvBuf, 0, this.recvByte, this.curRecvLen, recvSize);
                        this.curRecvLen += recvSize;
                        if(this.curRecvLen >= this.recvByteLen)
                        {
                            this.curRecvLen = 0;
                            this.recvFinish = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取接收的指令
        /// </summary>
        /// <param name="recvBuffer"></param>
        /// <returns></returns>
        private bool GetReceiveResult(ref byte[] recvBuffer)
        {
            if(this.recvFinish)
            {
                Array.Copy(this.recvByte, 0, recvBuffer, 0, this.recvByteLen);
                return true;
            }
            return false;
        }

        #endregion
    }
}
