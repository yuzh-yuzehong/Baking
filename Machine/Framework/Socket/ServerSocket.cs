using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace Machine
{
    class ServerSocket
    {
        #region // 字段

        /// <summary>
        /// 套接字
        /// </summary>
        private Socket sSocket;
        /// <summary>
        /// 服务端IP
        /// </summary>
        private string strIPInfo;
        /// <summary>
        /// 监听线程
        /// </summary>
        private Task taskWatch;
        /// <summary>
        /// 监听线程运行标志
        /// </summary>
        private bool taskWatchRunning;
        /// <summary>
        /// 客户端连接集合
        /// </summary>
        private List<Socket> clientSocket;
        /// <summary>
        /// 客户端通讯线程集合
        /// </summary>
        private List<Task> clientTask;

        #endregion


        #region // 方法

        public ServerSocket()
        {
            this.sSocket = null;
            this.taskWatch = null;
            this.taskWatchRunning = false;
            this.clientSocket = new List<Socket>();
            this.clientTask = new List<Task>();
            
        }

        /// <summary>
        /// 创建服务器
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool CreateServer(string ip, int port)
        {
            try
            {
                if(null == ip)
                {
                    return false;
                }

                if(null != sSocket)
                {
                    return sSocket.IsBound;
                }

                this.strIPInfo = string.Format("{0}:{1}", ip, port);
                IPEndPoint severAddr = new IPEndPoint(IPAddress.Parse(ip), port);
                this.sSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.sSocket.Bind(severAddr);
                this.sSocket.Listen(10);  // 最大连接数
                this.taskWatchRunning = true;
                this.taskWatch = new Task(WatchRunWhile, TaskCreationOptions.LongRunning);
                this.taskWatch.Start();
                WriteLog($"CreateServer() Success. Task[{this.taskWatch.Id}] Listening...");
                return true;
            }
            catch(SocketException ex)
            {
                CloseServer();
                WriteLog(string.Format("CreateServer() error {0}(错误代码:{1})", ex.Message, ex.ErrorCode));
                return false;
            }
            catch (Exception ex)
            {
                CloseServer();
                WriteLog(string.Format("CreateServer() error {0}", ex.Message));
                return false;
            }
        }

        /// <summary>
        /// 关闭服务器
        /// </summary>
        /// <returns></returns>
        public bool CloseServer()
        {
            try
            {
                if(null == this.sSocket)
                {
                    return true;
                }

                if(this.sSocket.Connected)
                {
                    // 正常关闭
                    this.sSocket.Shutdown(SocketShutdown.Both);
                    System.Threading.Thread.Sleep(10);
                }

                // 关闭套接字
                this.sSocket.Close();
                this.sSocket = null;
                // 监视线程结束
                this.taskWatchRunning = false;
                if (taskWatch != null)
                {
                    this.taskWatch.Wait();
                }
                // 所有连接关闭
                for(int i = 0; i < this.clientSocket.Count; i++)
                {
                    if(this.clientSocket[i].Connected)
                    {
                        this.clientSocket[i].Shutdown(SocketShutdown.Both);
                        this.clientSocket[i].Close();
                    }
                }
                this.clientSocket.Clear();
                // 所有响应线程结束
                Task.WaitAll(this.clientTask.ToArray());
                if (taskWatch != null)
                {
                    WriteLog($"CloseServer() Success. Task[{this.taskWatch.Id}] Listen end.");
                }
                else
                {
                    WriteLog($"CloseServer() Success. Task[null] Listen end.");
                }
                return true;
            }
            catch(SocketException ex)
            {
                WriteLog(string.Format("CloseServer() error {0}(错误代码:{1})", ex.Message, ex.ErrorCode));
                return false;
            }
            catch(Exception ex)
            {
                WriteLog(string.Format("CloseServer() error {0}", ex.Message));
                return false;
            }
        }

        /// <summary>
        /// 输出调试信息
        /// </summary>
        /// <param name="msg"></param>
        protected void WriteLog(string msg)
        {
            Def.WriteLog(string.Format("ServerSocket {0}", this.strIPInfo), msg);
        }

        /// <summary>
        /// 监视线程
        /// </summary>
        public void WatchRunWhile()
        {
            Socket client = null;
            while(this.taskWatchRunning)
            {
                try
                {
                    client = this.sSocket.Accept();

                    string clientInfo = "";

                    // 移除已经断开的连接
                    for(int i = 0; i < this.clientSocket.Count; i++)
                    {
                        if (!this.clientSocket[i].Connected)
                        {
                            clientInfo = this.clientSocket[i].RemoteEndPoint.ToString();
                            this.clientSocket.RemoveAt(i);
                            this.clientTask[i].Wait();
                            this.clientTask.RemoveAt(i);
                            WriteLog(clientInfo + " 已断开，结束等待响应");
                        }
                    }

                    // 保存新连接
                    this.clientSocket.Add(client);
                    //this.clientTask.Add(Task.Factory.StartNew(RecvThread, client));
                    Task recvTask = new Task(RecvThread, client, TaskCreationOptions.LongRunning);
                    recvTask.Start();
                    this.clientTask.Add(recvTask);
                    clientInfo = client.RemoteEndPoint.ToString();
                    WriteLog($" {clientInfo} 成功连接，接收线程{recvTask.Id}启动");
                }
                catch (Exception ex)
                {
                    // 套接字监听异常
                    WriteLog($"WatchRunWhile: {ex.Message}\r\n{ex.StackTrace}");
                    continue;
                }

                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// 接收线程
        /// </summary>
        /// <param name="objClient"></param>
        private void RecvThread(object objClient)
        {
            try
            {
                Socket client = objClient as Socket;
                WriteLog($"{client.RemoteEndPoint.ToString()} RecvThread start...");
                while(client.Connected)
                {
                    Recv(client);
                    Thread.Sleep(1);
                }
            }
            catch(SocketException ex)
            {
                WriteLog($"RecvThread() error {ex.Message}(错误代码:{ex.ErrorCode})\r\n{ex.StackTrace}");
            }
            catch(Exception ex)
            {
                WriteLog($"RecvThread() error {ex.Message}\r\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 响应接收，发送数据
        /// </summary>
        /// <param name="client"></param>
        /// <param name="recv"></param>
        /// <param name="recvSize"></param>
        protected virtual void Recv(Socket client)
        {

        }

        #endregion

    }
}
