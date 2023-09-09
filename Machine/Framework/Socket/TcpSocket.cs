using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Machine
{
    public class TcpSocket
    {
        #region // 字段

        /// <summary>
        /// 套接字
        /// </summary>
        private Socket sSocket;
        /// <summary>
        /// 服务端IP
        /// </summary>
        private string strIP;
        /// <summary>
        /// 服务端端口
        /// </summary>
        private int nPort;

        #endregion


        #region // 方法

        /// <summary>
        /// 构造函数
        /// </summary>
        public TcpSocket()
        {
            sSocket = null;
            strIP = "127.0.0.1";
            nPort = 5378;
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="ip">服务器地址</param>
        /// <param name="port">服务器端口</param>
        public bool Connect(string ip, int port)
        {
            try
            {
                if(null == ip)
                {
                    return false;
                }

                if(null != sSocket)
                {
                    return sSocket.Connected;
                }

                this.strIP = ip;
                this.nPort = port;
                IPEndPoint severAddr = new IPEndPoint(IPAddress.Parse(this.strIP), this.nPort);
                sSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sSocket.Connect(severAddr);
                
                if (sSocket.Connected)
                {
                    return true;
                }
                else
                {
                    Disconnect();
                    string strInfo = this.strIP + ":" + this.nPort + " 连接失败！";
                    Trace.WriteLine(strInfo);
                    return false;
                }
            }
            catch (SocketException ex)
            {
                Disconnect();
                StringBuilder strInfo = new StringBuilder();
                strInfo.AppendFormat("ClientSocket.Connect() {0}:{1} {2}(错误代码:{3})", this.strIP, this.nPort, ex.Message, ex.ErrorCode);
                Trace.WriteLine(strInfo.ToString());
                return false;
            }
            catch (Exception ex)
            {
                Disconnect();
                StringBuilder strInfo = new StringBuilder();
                strInfo.AppendFormat("ClientSocket.Connect() {0}:{1} {2}", this.strIP, this.nPort, ex.ToString());
                Trace.WriteLine(strInfo.ToString());
                return false;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public bool Disconnect()
        {
            try
            {
                if (null == sSocket)
                {
                    return true;
                }

                if (sSocket.Connected)
                {
                    // 正常关闭
                    sSocket.Shutdown(SocketShutdown.Both);
                    Thread.Sleep(10);
                }

                // 关闭套接字
                sSocket.Close();
                sSocket = null;
                return true;
            }
            catch (SocketException ex)
            {
                StringBuilder strInfo = new StringBuilder();
                strInfo.AppendFormat("ClientSocket.Disconnect() {0}:{1} {2}(错误代码:{3})", this.strIP, this.nPort, ex.Message, ex.ErrorCode);
                Trace.WriteLine(strInfo.ToString());
                return false;
            }
            catch (Exception ex)
            {
                StringBuilder strInfo = new StringBuilder();
                strInfo.AppendFormat("ClientSocket.Disconnect() {0}:{1} {2}", this.strIP, this.nPort, ex.ToString());
                Trace.WriteLine(strInfo.ToString());
                return false;
            }
        }

        /// <summary>
        /// 指示连接状态
        /// </summary>
        public bool IsConnect()
        {
            if(null != sSocket)
            {
                return sSocket.Connected;
            }
            return false;
        }

        /// <summary>
        /// 发送字符串
        /// </summary>
        /// <param name="strMsg">字符串</param>
        public bool Send(string strMsg)
        {
            try
            {
                if (null == sSocket || !sSocket.Connected)
                {
                    return false;
                }

                if (null == strMsg)
                {
                    return false;
                }

                // 发送数据
                return sSocket.Send(Encoding.Default.GetBytes(strMsg)) > -1;
            }
            catch (SocketException ex)
            {
                Disconnect();
                StringBuilder strInfo = new StringBuilder();
                strInfo.AppendFormat("ClientSocket.Send() {0}:{1} {2}(错误代码:{3})", this.strIP, this.nPort, ex.Message, ex.ErrorCode);
                Trace.WriteLine(strInfo.ToString());
                return false;
            }
            catch (Exception ex)
            {
                Disconnect();
                StringBuilder strInfo = new StringBuilder();
                strInfo.AppendFormat("ClientSocket.Send() {0}:{1} {2}", this.strIP, this.nPort, ex.ToString());
                Trace.WriteLine(strInfo.ToString());
                return false;
            }
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="byBuf">数据</param>
        /// <param name="nSize">字节数</param>
        public bool Send(byte[] byBuf, int nSize)
        {
            try
            {
                if (null == sSocket || !sSocket.Connected)
                {
                    return false;
                }

                if (null == byBuf || nSize <= 0)
                {
                    return false;
                }

                // 发送数据
                return (sSocket.Send(byBuf, nSize, SocketFlags.None) >= nSize);
            }
            catch (SocketException ex)
            {
                Disconnect();
                StringBuilder strInfo = new StringBuilder();
                strInfo.AppendFormat("ClientSocket.Send() {0}:{1} {2}(错误代码:{3})", this.strIP, this.nPort, ex.Message, ex.ErrorCode);
                Trace.WriteLine(strInfo.ToString());
                return false;
            }
            catch (Exception ex)
            {
                Disconnect();
                StringBuilder strInfo = new StringBuilder();
                strInfo.AppendFormat("ClientSocket.Send() {0}:{1} {2}", this.strIP, this.nPort, ex.ToString());
                Trace.WriteLine(strInfo.ToString());
                return false;
            }
        }

        /// <summary>
        /// 接收回调函数
        /// </summary>
        /// <param name="buffer">缓冲</param>
        /// <returns></returns>
        public int Recv(ref byte[] buffer)
        {
            try
            {
                if (null != sSocket && sSocket.Connected)
                {
                    return sSocket.Receive(buffer);
                }
            }
            catch (SocketException ex)
            {
                Disconnect();
                StringBuilder strInfo = new StringBuilder();
                strInfo.AppendFormat("ClientSocket.Send() {0}:{1} {2}(错误代码:{3})", this.strIP, this.nPort, ex.Message, ex.ErrorCode);
                Trace.WriteLine(strInfo.ToString());
            }
            catch (Exception ex)
            {
                Disconnect();
                StringBuilder strInfo = new StringBuilder();
                strInfo.AppendFormat("ClientSocket.Send() {0}:{1} {2}", this.strIP, this.nPort, ex.ToString());
                Trace.WriteLine(strInfo.ToString());
            }
            return 0;
        }

        /// <summary>
        /// 获取IP
        /// </summary>
        public string GetIP()
        {
            return this.strIP;
        }

        /// <summary>
        /// 获取端口
        /// </summary>
        public int GetPort()
        {
            return this.nPort;
        }

        /// <summary>
        /// 使用ping命令检查IP是否可用
        /// </summary>
        /// <param name="ip">要检查的IP地址</param>
        /// <param name="timeOut">超时时间：毫秒ms</param>
        /// <returns></returns>
        public static bool PingCheck(string ip, int timeOut)
        {
            try
            {
                Ping ping = new Ping();
                PingReply pingReply = ping.Send(ip, timeOut);
                return (pingReply.Status == IPStatus.Success);
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine(string.Format("ClientSocket.Send() {0} Exception: {1}", ip, ex.Message));
            }
            return false;
        }
        
        #endregion
    }
}
