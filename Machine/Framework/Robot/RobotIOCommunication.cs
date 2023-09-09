using HelperLibrary;
using System;
using System.Threading;
using SystemControlLibrary;

namespace Machine
{
    /// <summary>
    /// 适用机器人进行IO通讯：
    ///     1，测试RRRobot
    /// </summary>
    class RobotIOCommunication
    {
        #region // 字段属性

        private bool isConnected;
        private int[] stationInputs;
        private int[] stationOutputs;
        private int[] posInputs;
        private int[] posOutputs;
        private int[] orderInputs;
        private int[] orderOutputs;
        private int[] resultInputs;
        private int[] resultOutputs;
        private LogFile logFile;

        #endregion


        #region // 设置通讯IO点

        /// <summary>
        /// 工位IO（从低到高位），四位
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="output"></param>
        public void SetStationIO(int[] inputs, int[] outputs)
        {
            this.stationInputs = inputs;
            this.stationOutputs = outputs;
        }

        /// <summary>
        /// 位置IO（从低到高位），七位，即工位的具体位置
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="output"></param>
        public void SetPosIO(int[] inputs, int[] outputs)
        {
            this.posInputs = inputs;
            this.posOutputs = outputs;
        }

        /// <summary>
        /// 指令IO（从低到高位），三位，即在工位的动作
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="output"></param>
        public void SetOrderIO(int[] inputs, int[] outputs)
        {
            this.orderInputs = inputs;
            this.orderOutputs = outputs;
        }

        /// <summary>
        /// 结果IO（从低到高位），二位，即机器人运行中/错误/完成结果
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="output"></param>
        public void SetResultIO(int[] inputs, int[] outputs)
        {
            this.resultInputs = inputs;
            this.resultOutputs = outputs;
        }

        #endregion


        #region // 方法
        
        public RobotIOCommunication()
        {
            this.isConnected = false;
            this.logFile = new LogFile();
        }

        /// <summary>
        /// 连接状态
        /// </summary>
        /// <returns></returns>
        public bool IsConnect()
        {
            return this.isConnected;
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="strIP">服务器地址</param>
        /// <param name="nPort">服务器端口</param>
        public bool Connect(string ip, int port)
        {
            this.isConnected = false;
            for(int i = 0; i < this.stationInputs.Length; i++)
            {
                if ((null == this.stationInputs) || (this.stationInputs[i] < 0)
                    || (null == this.stationOutputs) || (this.stationOutputs[i] < 0)
                    || (this.stationInputs.Length != this.stationOutputs.Length))
                {
                    return false;
                }
            }
            for(int i = 0; i < this.posInputs.Length; i++)
            {
                if((null == this.posInputs) || (this.posInputs[i] < 0)
                    || (null == this.posOutputs) || (this.posOutputs[i] < 0)
                    || (this.posInputs.Length != this.posOutputs.Length))
                {
                    return false;
                }
            }
            for(int i = 0; i < this.orderInputs.Length; i++)
            {
                if((null == this.orderInputs) || (this.orderInputs[i] < 0)
                    || (null == this.orderOutputs) || (this.orderOutputs[i] < 0)
                    || (this.orderInputs.Length != this.orderOutputs.Length))
                {
                    return false;
                }
            }
            for(int i = 0; i < this.resultInputs.Length; i++)
            {
                if((null == this.resultInputs) || (this.resultInputs[i] < 0)
                    || (null == this.resultOutputs) || (this.resultOutputs[i] < 0)
                    || (this.resultInputs.Length != this.resultOutputs.Length))
                {
                    return false;
                }
            }
            this.logFile.SetFileInfo(Def.GetAbsPathName(string.Format("Log\\RobotLog\\RobotIOInterface\\")), 2, 7);
            this.isConnected = true;
            return IsConnect();
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <returns></returns>
        public bool Disconnect()
        {
            this.isConnected = false;
            return this.isConnected;
        }

        /// <summary>
        /// 发送机器人指令
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public bool Send(int[] cmd)
        {
            if(SendToRobot(cmd))
            {
                DateTime time = DateTime.Now;
                while((DateTime.Now - time).TotalSeconds < 5.0)
                {
                    // 收到指令正确可移动
                    if(DeviceManager.Inputs(this.resultInputs[1]).IsOn() && DeviceManager.Inputs(this.resultInputs[0]).IsOff())
                    {
                        if(CompareSendRecvCmd(cmd))
                        {
                            // 结果标志：二位，等待比较确认机器人指令后再发送
                            int order = 0;
                            switch((RobotOrder)cmd[(int)RobotCmdFormat.Result])
                            {
                                case RobotOrder.MOVING:
                                    order = 2;
                                    break;
                                case RobotOrder.FINISH:
                                    order = 3;
                                    break;
                                default:
                                    return false;
                            }
                            for(int i = 0; i < this.resultOutputs.Length; i++)
                            {
                                bool res = (0x0001 == (order & (0x0001 << i))) ? DeviceManager.Outputs(this.resultOutputs[i]).On() : DeviceManager.Outputs(this.resultOutputs[i]).Off();
                            }
                            return true;
                        }
                    }
                    // 收到指令错误
                    else if (DeviceManager.Inputs(this.resultInputs[1]).IsOff() && DeviceManager.Inputs(this.resultInputs[0]).IsOn())
                    {
                        return false;
                    }
                    Thread.Sleep(1);
                }
            }
            return false;
        }

        /// <summary>
        /// 发送机器人指令，并等待完成
        /// </summary>
        /// <param name="cmd">指令</param>
        /// <param name="recv">接收缓存</param>
        /// <param name="delayTime">防呆时间：s</param>
        /// <returns></returns>
        public bool SendAndWait(int[] cmd, ref int[] recvBuf, double delayTime = 30.0)
        {
            if(!Send(cmd))
            {
                return false;
            }
            DateTime time = DateTime.Now;
            while((DateTime.Now - time).TotalSeconds < delayTime)
            {
                if(GetReceiveResult(ref recvBuf))
                {
                    return true;
                }
                Thread.Sleep(1);
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
            if(DeviceManager.Inputs(this.resultInputs[0]).IsOn())
            {
                // 初始化接收
                for(int i = 0; i < recvBuf.Length; i++)
                {
                    recvBuf[i] = 0;
                }
                // 工位：四位
                for(int i = 0; i < this.stationInputs.Length; i++)
                {
                    recvBuf[(int)RobotCmdFormat.Station] |= (DeviceManager.Inputs(stationInputs[i]).IsOn() ? 1 : 0) << i;
                }
                // 位置：列*最大行 + 行：七位
                for(int i = 0; i < this.posInputs.Length; i++)
                {
                    recvBuf[(int)RobotCmdFormat.StationRow] |= (DeviceManager.Inputs(posInputs[i]).IsOn() ? 1 : 0) << i;
                }
                recvBuf[(int)RobotCmdFormat.StationCol] = recvBuf[(int)RobotCmdFormat.StationRow];
                // 指令：三位
                for(int i = 0; i < this.orderInputs.Length; i++)
                {
                    recvBuf[(int)RobotCmdFormat.Order] |= (DeviceManager.Inputs(orderInputs[i]).IsOn() ? 1 : 0) << i;
                }
                // 结果标志：二位
                for(int i = 0; i < this.resultInputs.Length; i++)
                {
                    recvBuf[(int)RobotCmdFormat.Result] |= (DeviceManager.Inputs(resultInputs[i]).IsOn() ? 1 : 0) << i;
                }
                switch(recvBuf[(int)RobotCmdFormat.Result])
                {
                    case 1:
                        recvBuf[(int)RobotCmdFormat.Result] = (int)RobotOrder.ERR;
                        break;
                    case 2:
                        recvBuf[(int)RobotCmdFormat.Result] = (int)RobotOrder.MOVING;
                        break;
                    case 3:
                        recvBuf[(int)RobotCmdFormat.Result] = (int)RobotOrder.FINISH;
                        break;
                    default:
                        recvBuf[(int)RobotCmdFormat.Result] = (int)RobotOrder.ERR;
                        break;
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
        private bool SendToRobot(int[] cmd)
        {
            string strCmd = GetRobotCmd(cmd);
            OutputLog(true, strCmd.TrimEnd('\r', '\n', '\0'));
            // 行列必须一致：代表位置
            if(cmd[(int)RobotCmdFormat.StationRow] != cmd[(int)RobotCmdFormat.StationCol])
            {
                return false;
            }
            // 工位：四位
            for(int i = 0; i < this.stationOutputs.Length; i++)
            {
                bool res = (0x0001 == (cmd[(int)RobotCmdFormat.Station] & (0x0001 << i))) ? DeviceManager.Outputs(this.stationOutputs[i]).On() : DeviceManager.Outputs(this.stationOutputs[i]).Off();
            }
            // 位置：列*最大行 + 行：七位
            for(int i = 0; i < this.posOutputs.Length; i++)
            {
                bool res = (0x0001 == (cmd[(int)RobotCmdFormat.StationRow] & (0x0001 << i))) ? DeviceManager.Outputs(this.posOutputs[i]).On() : DeviceManager.Outputs(this.posOutputs[i]).Off();
            }
            // 指令：三位
            for(int i = 0; i < this.orderOutputs.Length; i++)
            {
                bool res = (0x0001 == (cmd[(int)RobotCmdFormat.Order] & (0x0001 << i))) ? DeviceManager.Outputs(this.orderOutputs[i]).On() : DeviceManager.Outputs(this.orderOutputs[i]).Off();
            }
            // 结果标志：二位，等待确认机器人指令后再发送
            //for(int i = 0; i < this.resultOutputs.Length; i++)
            //{
            //    bool res = (0x0001 == (cmd[(int)RobotCmdFormat.Result] & (0x0001 << i))) ? DeviceManager.Outputs(this.resultOutputs[i]).On() : DeviceManager.Outputs(this.resultOutputs[i]).Off();
            //}
            return true;
        }

        /// <summary>
        /// 比较发送及接收的指令是否相同
        /// </summary>
        /// <param name="sendCmd"></param>
        /// <param name="recCmd"></param>
        /// <returns></returns>
        private bool CompareSendRecvCmd(int[] sendCmd)
        {
            // 工位：四位
            for(int i = 0; i < this.stationOutputs.Length; i++)
            {
                if (DeviceManager.Inputs(this.stationInputs[i]).IsOn() != DeviceManager.Outputs(this.stationOutputs[i]).IsOn())
                {
                    return false;
                }
            }
            // 位置：列*最大行 + 行：七位
            for(int i = 0; i < this.posOutputs.Length; i++)
            {
                if(DeviceManager.Inputs(this.posInputs[i]).IsOn() != DeviceManager.Outputs(this.posOutputs[i]).IsOn())
                {
                    return false;
                }
            }
            // 指令：三位
            for(int i = 0; i < this.orderOutputs.Length; i++)
            {
                if(DeviceManager.Inputs(this.orderInputs[i]).IsOn() != DeviceManager.Outputs(this.orderOutputs[i]).IsOn())
                {
                    return false;
                }
            }
            // 结果标志：二位，不比较
            return true;
        }

        /// <summary>
        /// 输出机器人指令Log
        /// </summary>
        /// <param name="sendLog"></param>
        /// <param name="log"></param>
        private void OutputLog(bool sendLog, string log)
        {
            string msg = string.Format(" {0} {1}\t", (sendLog ? "Send : ->" : "Recv : <-"), log);
            this.logFile.WriteLog(DateTime.Now, "RobotIOInterface", msg, LogType.Success);
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
        
        #endregion
    }
}
