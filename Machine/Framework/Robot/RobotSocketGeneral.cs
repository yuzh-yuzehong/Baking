using HelperLibrary;
using System;
using log4net;

namespace Machine
{
    /// <summary>
    /// 通用机器人通讯类：
    ///     通讯指令格式(英文,逗号分割)：工位号,行,列,速度,动作,标志
    ///     1,Send-Request: 工位号,行,列,速度,动作,END
    ///     2,Recv-Response: 工位号,行,列,速度,动作,标志
    ///     3,Recv-Finish: 工位号,行,列,速度,动作,标志
    /// </summary>
    class RobotSocketGeneral : BaseThread
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        #region // 字段

        private TcpSocket client;
        private bool recvFinish;
        private byte[] recvBuffer;
        private int[] recvCmd;
        //private LogFile logFile;
        private int robotID;
        private string robotName;

        #endregion


        #region // 方法

        public RobotSocketGeneral()
        {
            client = new TcpSocket();
            recvFinish = false;
            recvBuffer = new byte[256];
            recvCmd = new int[(int)RobotCmdFormat.End];
            //this.logFile = new LogFile();
        }

        /// <summary>
        /// 设置机器人的ID及名称
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        public void SetRobotInfo(int id, string name)
        {
            this.robotID = id;
            this.robotName = name;
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
            if (!IsConnect() && this.client.Connect(ip, port))
            {
                //this.logFile.SetFileInfo(Def.GetAbsPathName(string.Format("Log\\RobotLog\\{0}\\", ip)), 2, 7);
                if (string.IsNullOrEmpty(this.robotName))
                {
                    this.robotName = string.Format("{0}:{1} Robot", ip, port);
                }
                InitThread(this.robotName);
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
            ReleaseThread();
            return result;
        }

        /// <summary>
        /// 发送机器人指令
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public bool Send(int[] cmd, OptMode mode)
        {
            if (SendToRobot(cmd, mode))
            {
                DateTime time = DateTime.Now;
                while((DateTime.Now - time).TotalSeconds < 10.0)
                {
                    if(this.recvFinish)
                    {
                        return CompareSendRecvCmd(cmd, this.recvCmd);
                    }
                    Sleep(1);
                }
                log.Debug("Send timeout");
            }
            log.Debug("Send failed");
            return false;
        }

        /// <summary>
        /// 发送机器人指令，并等待完成
        /// </summary>
        /// <param name="cmd">指令</param>
        /// <param name="recv">接收缓存</param>
        /// <param name="delayTime">防呆时间：s</param>
        /// <returns></returns>
        public bool SendAndWait(int[] cmd, ref int[] recvBuf, OptMode mode, double delayTime = 30.0)
        {
            if (!Send(cmd, mode))
            {
                return false;
            }
            this.recvFinish = false;    // 等待二次接收完成
            DateTime time = DateTime.Now;
            while((DateTime.Now - time).TotalSeconds < delayTime)
            {
                if (GetReceiveResult(ref recvBuf))
                {
                    return true;
                }
                Sleep(1);
            }
            return false;
        }

        /// <summary>
        /// 获取接收的指令
        /// </summary>
        /// <param name="recvBuf"></param>
        /// <returns></returns>
        public bool GetReceiveResult(ref int[] recvBuf)
        {
            if(this.recvFinish)
            {
                for(int i = 0; i < this.recvCmd.Length; i++)
                {
                    recvBuf[i] = this.recvCmd[i];
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 发送指令到机器人
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        private bool SendToRobot(int[] cmd, OptMode mode)
        {
            string strCmd = GetRobotCmd(cmd);
            OutputLog(true, strCmd, mode);
            this.recvFinish = false;
            return this.client.Send(strCmd);
        }

        /// <summary>
        /// 比较发送及接收的指令是否相同
        /// </summary>
        /// <param name="sendCmd"></param>
        /// <param name="recCmd"></param>
        /// <returns></returns>
        private bool CompareSendRecvCmd(int[] sendCmd, int[] recCmd)
        {
            string cmd = "";
            string ack = "";

            for(int i = 0; i < sendCmd.Length; i++)
            {
                cmd += string.Format("{0},", sendCmd[i]);
                ack += string.Format("{0},", recCmd[i]);
                if (sendCmd[i] != recCmd[i])
                {
                    if ((recCmd[(int)RobotCmdFormat.Result] != (int)RobotOrder.MOVING) 
                        && (recCmd[(int)RobotCmdFormat.Result] != (int)RobotOrder.FINISH))
                    {
                        log.DebugFormat("CompareSendRecvCmd: {0},{1}", cmd, ack);
                        log.DebugFormat("CompareSendRecvCmd: {0},{1}", recCmd[(int)RobotCmdFormat.Result], (int)RobotOrder.MOVING);
                        return false;
                    }
                }
            }
            log.DebugFormat("CompareSendRecvCmd: {0}-{1}", cmd, ack);
            return true;
        }

        /// <summary>
        /// 输出机器人指令Log
        /// </summary>
        /// <param name="sendLog"></param>
        /// <param name="log"></param>
        private void OutputLog(bool sendLog, string log, OptMode mode)
        {
            //string msg = string.Format(" {0} {1}\t", (sendLog ? "Send : ->" : "Recv : <-"), log);
            //this.logFile.WriteLog(DateTime.Now, this.robotTaskName, msg, LogType.Success);
            DataBaseLog.AddRobotLog(new DataBaseLog.RobotLogFormula(Def.GetProductFormula(), robotID, robotName
                , MachineCtrl.GetInstance().OperaterID, DateTime.Now.ToString(Def.DateFormal), mode.ToString()
                , sendLog ? "Send" : "Recv", log.TrimEnd('\r', '\n', '\0', '\t')));
        }

        /// <summary>
        /// 获取机器人指令：station,row,col,speed,order,end
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        private string GetRobotCmd(int[] cmd)
        {
            string strCmd = "";
            if(cmd.Length == (int)RobotCmdFormat.End)
            {
                strCmd = string.Format("{0},{1},{2},{3},{4},END\r\n"
                    , cmd[(int)RobotCmdFormat.Station], cmd[(int)RobotCmdFormat.StationRow], cmd[(int)RobotCmdFormat.StationCol]
                    , cmd[(int)RobotCmdFormat.Speed], ((RobotOrder)cmd[(int)RobotCmdFormat.Order]).ToString());
            }
            return strCmd;
        }

        protected override void RunWhile()
        {
            if (!IsConnect())
            {
                return;
            }
            Array.Clear(this.recvBuffer, 0, this.recvBuffer.Length);
            int recvLen = this.client.Recv(ref this.recvBuffer);
            if (recvLen > 0)
            {
                string recvBuf = System.Text.Encoding.UTF8.GetString(this.recvBuffer, 0, recvLen);
                if(recvBuf.Length > 2) OutputLog(false, recvBuf, OptMode.Auto);

                string[] data = recvBuf.Split(',');
                if(data.Length == (int)RobotCmdFormat.End)
                {
                    for(int i = 0; i < data.Length; i++)
                    {
                        if(i < (int)RobotCmdFormat.Order)
                        {
                            this.recvCmd[i] = Convert.ToInt32(data[i]);
                        }
                        else if(i >= (int)RobotCmdFormat.Order)
                        {
                            for(RobotOrder idx = RobotOrder.HOME; idx < RobotOrder.ORDER_END; idx++)
                            {
                                if(data[i].Contains(idx.ToString()))
                                {
                                    this.recvCmd[i] = (int)idx;
                                }
                            }
                        }
                    }
                    this.recvFinish = true;
                }
            }
        }

        #endregion
    }
}
