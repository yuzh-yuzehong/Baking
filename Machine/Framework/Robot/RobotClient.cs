using System;

namespace Machine
{
    public class RobotClient
    {
        #region // 字段
        private RobotType robotType;
        private RobotSocketGeneral client;
        #endregion


        #region // 方法

        public RobotClient()
        {
            this.robotType = RobotType.END;
            this.client = null;
        }

        public bool SetRobotType(string type)
        {
            bool result = false;
            RobotType rbtType = RobotType.END;
            if (Enum.TryParse(type, out rbtType))
            {
                this.robotType = rbtType;
                switch(this.robotType)
                {
                    case RobotType.ABB:
                        break;
                    case RobotType.KUKA:
                    case RobotType.FANUC:
                        result = true;
                        this.client = new RobotSocketGeneral();
                        break;
                    case RobotType.END:
                        break;
                    default:
                        break;
                }
            }
            return result;
        }

        public void SetRobotInfo(int id, string name)
        {
            if (null != this.client)
            {
                this.client.SetRobotInfo(id, name);
            }
        }

        public bool IsConnect()
        {
            bool isConnect = false;
            if(null != this.client)
            {
                switch(this.robotType)
                {
                    case RobotType.ABB:
                        break;
                    case RobotType.KUKA:
                    case RobotType.FANUC:
                        isConnect = this.client.IsConnect();
                        break;
                    case RobotType.END:
                        break;
                    default:
                        break;
                }
            }
            return isConnect;
        }

        public bool Connect(string ip, int port)
        {
            bool result = false;
            if(null != this.client)
            {
                switch(this.robotType)
                {
                    case RobotType.ABB:
                        break;
                    case RobotType.KUKA:
                    case RobotType.FANUC:
                        result = this.client.Connect(ip, port);
                        break;
                    case RobotType.END:
                        break;
                    default:
                        break;
                }
            }
            return result;
        }

        public bool Disconnect()
        {
            bool result = false;
            if(null != this.client)
            {
                switch(this.robotType)
                {
                    case RobotType.ABB:
                        break;
                    case RobotType.KUKA:
                    case RobotType.FANUC:
                        result = this.client.Disconnect();
                        break;
                    case RobotType.END:
                        break;
                    default:
                        break;
                }
            }
            return result;
        }

        public bool Send(int[] cmd, OptMode mode)
        {
            bool result = false;
            if(null != this.client)
            {
                switch(this.robotType)
                {
                    case RobotType.ABB:
                        break;
                    case RobotType.KUKA:
                    case RobotType.FANUC:
                        result = this.client.Send(cmd, mode);
                        break;
                    case RobotType.END:
                        break;
                    default:
                        break;
                }
            }
            return result;
        }

        public bool SendAndWait(int[] cmd, ref int[] recvBuf, OptMode mode, double delayTime = 30.0)
        {
            bool result = false;
            if(null != this.client)
            {
                switch(this.robotType)
                {
                    case RobotType.ABB:
                        break;
                    case RobotType.KUKA:
                    case RobotType.FANUC:
                        result = this.client.SendAndWait(cmd, ref recvBuf, mode, delayTime);
                        break;
                    case RobotType.END:
                        break;
                    default:
                        break;
                }
            }
            return result;
        }

        public bool GetReceiveResult(ref int[] recvBuf)
        {
            bool result = false;
            if(null != this.client)
            {
                switch(this.robotType)
                {
                    case RobotType.ABB:
                        break;
                    case RobotType.KUKA:
                    case RobotType.FANUC:
                        result = this.client.GetReceiveResult(ref recvBuf);
                        break;
                    case RobotType.END:
                        break;
                    default:
                        break;
                }
            }
            return result;
        }

        #endregion
    }
}
