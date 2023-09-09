using System;

namespace Machine
{
    class BarcodeScan : BaseThread
    {
        #region // 字段

        private bool isSocket;
        private ComPort comPort;
        private TcpSocket client;
        private bool recvFinish;
        private string recvBuffer;
        private string adderInfo;

        #endregion


        #region // 方法

        /// <summary>
        /// 构造函数
        /// </summary>
        public BarcodeScan()
        {
            this.isSocket = false;
            this.comPort = new ComPort();
            this.client = new TcpSocket();
            this.recvBuffer = string.Empty;
            this.adderInfo = string.Empty;
        }

        /// <summary>
        /// 扫码器的连接状态
        /// </summary>
        /// <returns></returns>
        public bool IsConnect()
        {
            if (isSocket)
            {
                return this.client.IsConnect();
            }
            else
            {
                return this.comPort.IsOpen();
            }
        }

        /// <summary>
        /// 扫码器以网口通讯连接
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool ConnectSocket(string ip, int port)
        {
            if(IsConnect())
            {
                return true;
            }
            if(this.client.Connect(ip, port))
            {
                isSocket = true;
                adderInfo = string.Format("{0}:{1}", ip, port);
                return InitThread("BarcodeScan Socket " + adderInfo);
            }
            return false;
        }

        /// <summary>
        /// 扫码器以串口通讯连接
        /// </summary>
        /// <param name="com">串口号</param>
        /// <param name="port">串口波特率</param>
        /// <param name="linefeed">换行符</param>
        /// <returns></returns>
        public bool ConnectCom(int com, int port, string linefeed = "\r\n")
        {
            if (IsConnect())
            {
                return true;
            }
            if(comPort.Open(com, port))
            {
                isSocket = false;
                adderInfo = string.Format("COM{0}:{1}", com, port);
                this.comPort.SetLinefeed(linefeed);
                return InitThread("BarcodeScan Com " + adderInfo);
            }
            return false;
        }

        /// <summary>
        /// 断开通讯
        /// </summary>
        /// <returns></returns>
        public bool Disconnect()
        {
            //this.adderInfo = string.Empty;
            if(isSocket)
            {
                this.client.Disconnect();
                return ReleaseThread();
            }
            else
            {
                this.comPort.Close();
                return ReleaseThread();
            }
        }

        /// <summary>
        /// 地址信息
        /// </summary>
        /// <returns></returns>
        public string AdderInfo()
        {
            return this.adderInfo;
        }

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool Send(string sendText)
        {
            WriteLog(" Send: -> " + sendText.TrimEnd('\r', '\n', '\0'));
            this.recvFinish = false;
            this.recvBuffer = string.Empty;
            if(isSocket)
            {
                return this.client.Send(sendText);
            }
            else
            {
                return this.comPort.Write(sendText);
            }
        }

        /// <summary>
        /// 接收
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool Recv(ref string recvText, int timeout)
        {
            DateTime time = DateTime.Now;
            while((DateTime.Now - time).TotalMilliseconds < timeout)
            {
                if (this.recvFinish)
                {
                    recvText = this.recvBuffer;
                    return true;
                }
                Sleep(1);
            }
            return false;
        }

        protected override void RunWhile()
        {
            if (!IsConnect())
            {
                return;
            }
            if (isSocket)
            {
                byte[] buf = new byte[1024];
                if(client.Recv(ref buf) > 0)
                {
                    this.recvBuffer += System.Text.Encoding.Default.GetString(buf).TrimEnd('\r', '\n', '\0');
                    WriteLog(string.Format("Client Recv: <- {0}【{1}位】", recvBuffer, recvBuffer.Length));
                    this.recvFinish = true;
                }
            }
            else
            {
                //byte[] buf = new byte[1024];
                //if(comPort.Read(ref buf) > 0)
                this.recvBuffer = comPort.ReadLine();
                if(this.recvBuffer.Length > 0)
                {
                    //this.recvBuffer += System.Text.Encoding.Default.GetString(buf).TrimEnd('\r', '\n', '\0');
                    WriteLog(string.Format("Com Recv: <- {0}【{1}位】", recvBuffer, recvBuffer.Length));
                    this.recvFinish = true;
                }
            }
        }

        #endregion
    }
}
