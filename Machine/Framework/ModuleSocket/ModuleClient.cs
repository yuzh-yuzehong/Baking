using System;
using System.Threading;
using System.Threading.Tasks;

namespace Machine
{
    sealed class ModuleClient : BaseThread
    {
        #region // 字段

        private TcpSocket client;
        private object clientLock;
        private byte[] sendBuffer;
        private byte[] recvBuffer;
        private bool recvFinish;
        private Task readTask;    // 读写任务运行线程
        private int recvBufVaildLen;
        private ModuleSocketData readData;

        #endregion


        #region // 方法

        public ModuleClient()
        {
            this.client = new TcpSocket();
            this.clientLock = new object();
            this.sendBuffer = new byte[(int)MCSBuffer.Send];
            this.recvBuffer = new byte[(int)MCSBuffer.Recv];
            this.recvFinish = false;
            this.recvBufVaildLen = 0;
            this.readData = new ModuleSocketData();
        }

        /// <summary>
        /// 检查是否包含当前runId的模组
        /// </summary>
        /// <param name="mcID"></param>
        /// <param name="runId"></param>
        /// <returns></returns>
        public bool CheckRunID(int mcID, int runId)
        {
            //if (mcID == this.readData.machineID)
            {
                foreach(var item in this.readData.moduleEnable)
                {
                    if (runId == (int)item.Key)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 连接状态
        /// </summary>
        /// <returns></returns>
        public bool IsConnect()
        {
            return this.client.IsConnect();
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="strIP">服务器地址</param>
        /// <param name="nPort">服务器端口</param>
        public bool Connect(string ip, int port)
        {
            if (!TcpSocket.PingCheck(ip, 500))
            {
                return false;
            }
            if(!IsConnect() && this.client.Connect(ip, port))
            {
                InitTaskThread($"ModuleClient[{ip}: {port}] Task");
            }
            return IsConnect();
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <returns></returns>
        public bool Disconnect()
        {
            bool result = this.client.Disconnect();
            ReleaseTaskThread();
            return result;
        }

        /// <summary>
        /// 初始化线程(开始运行)
        /// </summary>
        public bool InitTaskThread(string name)
        {
            try
            {
                this.readTask = new Task(ReadTask, TaskCreationOptions.LongRunning);
                this.readTask.Start();
                WriteLog(name + " SendRunWhile Start running.");
                return InitThread(name);
            }
            catch(System.Exception ex)
            {
                WriteLog("ModuleClient InitTaskThread" + ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 释放线程(终止运行)
        /// </summary>
        public bool ReleaseTaskThread()
        {
            try
            {
                this.readTask.Wait();
                WriteLog("ModuleClient SendRunWhile end.");
                return ReleaseThread();
            }
            catch(System.Exception ex)
            {
                WriteLog("ModuleClient ReleaseTaskThread" + ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 接收线程
        /// </summary>
        protected override void RunWhile()
        {
            if (IsConnect())
            {
                byte[] buffer = new byte[(int)MCSBuffer.Recv];
                int recvSize = this.client.Recv(ref buffer);

                if(recvSize > 0)
                {
                    PackageJoint(buffer, recvSize);
                }
            }
        }

        /// <summary>
        /// 读写任务线程
        /// </summary>
        private void ReadTask()
        {
            while(this.client.IsConnect())
            {
                try
                {
                    if(!SendAndWait((uint)(PacketType.ReadAll), null))
                    {
                        WriteLog($"ModuleClient.SendAndWait {this.client.GetIP()}:{this.client.GetPort()} 发送指令失败");
                    }
                }
                catch (System.Exception ex)
                {
                    this.readData.Release();
                    MachineCtrl.GetInstance().SetModuleSocketData(readData);
                    WriteLog($"ModuleClient.ReadTask() {this.client.GetIP()}:{this.client.GetPort()} -> : {ex.Message}");
                }
                Thread.Sleep(100);
            }
            this.readData.Release();
            MachineCtrl.GetInstance().SetModuleSocketData(readData);
        }

        /// <summary>
        /// 获取发送数据报文
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="sourceData"></param>
        /// <param name="sendBuf"></param>
        /// <param name="sendSize"></param>
        /// <returns></returns>
        private bool GetSendData(uint dataType, ModuleSocketData sourceData, ref byte[] sendBuf, ref int sendSize)
        {
            try
            {
                byte[] buf = new byte[(int)MCSBuffer.Send];
                int size = 0;
                if(null != sourceData)
                {
                    size = sourceData.Serialize(ref buf);
                    buf = GZipCompress.Compress(buf, size);
                    size = buf.Length;
                }
                PacketHeader head = new PacketHeader();
                head.cmdType = dataType;
                head.crcCode = (uint)(Def.CRCCalc(buf, size));

                // 拷贝数据
                int idx = PacketHeader.header.Length;
                PacketHeader.header.CopyTo(sendBuf, 0);
                BitConverter.GetBytes(head.cmdType).CopyTo(sendBuf, idx);
                idx += 4;
                BitConverter.GetBytes(size).CopyTo(sendBuf, idx);
                idx += 4;
                BitConverter.GetBytes(head.crcCode).CopyTo(sendBuf, idx);
                idx += 4;
                Array.Copy(buf, 0, sendBuf, idx, size);
                // 发送数据总大小
                sendSize = size + idx;
                
                return true;
            }
            catch (System.Exception ex)
            {
                WriteLog("ModuleClient GetSendData: " + ex.Message);
            }
            return false;
        }

        /// <summary>
        /// 发送并等待一次接收
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="writeSocket"></param>
        /// <returns></returns>
        public bool SendAndWait(uint dataType, ModuleSocketData writeSocket)
        {
            lock(this.clientLock)
            {
                this.recvFinish = false;
                int sendSize = 0;
                this.sendBuffer.Initialize();
                if(GetSendData(dataType, writeSocket, ref sendBuffer, ref sendSize))
                {
                    if (this.client.Send(sendBuffer, sendSize))
                    {
                        DateTime startTime = DateTime.Now;
                        while(true)
                        {
                            if(this.recvFinish)
                            {
                                return true;
                            }
                            if((DateTime.Now - startTime).TotalSeconds > 5)
                            {
                                break;
                            }
                            Thread.Sleep(1);
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 拼包
        /// </summary>
        /// <param name="recvBuf"></param>
        /// <param name="size"></param>
        private void PackageJoint(byte[] recvBuf, int size)
        {
            int packHead = FindPackageHead(recvBuf, 0, size);
            int packEnd = FindPackageHead(recvBuf, packHead + 1, size);

            // 起始位有报头，且数据包有两个报头：完整报文 + 后报文开始
            if((packHead == 0) && (packEnd > 0))
            {
                // 第一个报文
                Array.Copy(recvBuf, 0, this.recvBuffer, 0, (packEnd - packHead));
                AnalysisPackage(this.recvBuffer, (packEnd - packHead));
                this.recvBufVaildLen = 0;
                // 第二个报文
                Array.Copy(recvBuf, packEnd, recvBuf, 0, (size - packEnd));
                recvBuf[size - packEnd + 1] = Convert.ToByte("\0");
                PackageJoint(this.recvBuffer, (size - packEnd));
            }
            // 起始位仅有一个报头
            else if((packHead == 0) && (packEnd < 0))
            {
                // 仅有一个完整报文
                Array.Copy(recvBuf, 0, this.recvBuffer, 0, size);
                if(AnalysisPackage(this.recvBuffer, size))
                {
                    this.recvBufVaildLen = 0;
                }
                // 报文不完整
                else
                {
                    this.recvBufVaildLen += size;
                }
            }
            // 前报文后续 + 后报文开始
            else if(packHead > 0)
            {
                // 前报文后续
                Array.Copy(recvBuf, 0, this.recvBuffer, this.recvBufVaildLen, packHead);
                AnalysisPackage(this.recvBuffer, (this.recvBufVaildLen + packHead));
                this.recvBufVaildLen = 0;
                // 后报文开始
                Array.Copy(recvBuf, packHead, recvBuf, 0, (size - packHead));
                PackageJoint(recvBuf, (size - packHead));
            }
            // 无报头
            else if(packHead < 0)
            {
                // 前报文后续
                Array.Copy(recvBuf, 0, this.recvBuffer, this.recvBufVaildLen, packHead);
                if(AnalysisPackage(this.recvBuffer, (this.recvBufVaildLen + packHead)))
                {
                    this.recvBufVaildLen = 0;
                }
                // 报文不完整
                else
                {
                    this.recvBufVaildLen += size;
                }
            }
        }

        /// <summary>
        /// 查找报文中是否含有报头
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="startIdx"></param>
        /// <returns></returns>
        private int FindPackageHead(byte[] buf, int startIdx, int bufLen)
        {
            int index = -1;
            for(int i = startIdx; i < bufLen - PacketHeader.header.Length; i++)
            {
                bool find = true;
                byte[] check = new byte[PacketHeader.header.Length];
                Array.Copy(buf, i, check, 0, check.Length);
                for(int j = 0; j < check.Length; j++)
                {
                    if(check[j] != PacketHeader.header[j])
                    {
                        find = false;
                        break;
                    }
                }
                if(find)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        /// <summary>
        /// 解析报文至ModuleSocketData
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private bool AnalysisPackage(byte[] buf, int size)
        {
            PacketHeader head = new PacketHeader();
            int idx = PacketHeader.header.Length;
            head.cmdType = BitConverter.ToUInt32(buf, idx);
            idx += 4;
            head.length = BitConverter.ToUInt32(buf, idx);
            idx += 4;
            head.crcCode = BitConverter.ToUInt32(buf, idx);
            idx += 4;
            if (head.length == (uint)(size - idx))
            {
                byte[] data = new byte[head.length];
                Array.Copy(buf, idx, data, 0, head.length);
                uint crcCheck = (uint)Def.CRCCalc(data, (int)head.length);
                data = GZipCompress.Decompress(data);
                // 校验数据
                if(crcCheck == head.crcCode)
                {
                    if (((int)PacketType.ReadAll == head.cmdType))
                    {
                        this.readData = ModuleSocketData.Deserialize(data, data.Length) as ModuleSocketData;
                        if(null != this.readData)
                        {
                            MachineCtrl.GetInstance().SetModuleSocketData(this.readData);
                        }
                    }
                    this.recvFinish = true;
                    return true;
                }
                else
                {
                    Def.WriteLog("ModuleClient", string.Format("AnalysisPackage校验数据失败,crcCheck:{0}!= head.crcCode{1} ", crcCheck, head.crcCode));
                }
            }
            return false;
        }

        #endregion

    }
}
