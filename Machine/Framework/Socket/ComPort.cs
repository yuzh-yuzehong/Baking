using System.Diagnostics;
using System.IO.Ports;

namespace Machine
{
    /// <summary>
    /// 串行端口通讯类
    /// </summary>
    public class ComPort
    {
        #region // 字段

        private SerialPort serialPort;

        #endregion


        #region // 方法

        /// <summary>
        /// 串口是否已打开
        /// </summary>
        /// <returns></returns>
        public bool IsOpen()
        {
            if(null != this.serialPort)
            {
                return this.serialPort.IsOpen;
            }
            return false;
        }

        /// <summary>
        /// 打开指定的串口
        /// </summary>
        /// <param name="com">串口号</param>
        /// <param name="port">串口波特率</param>
        /// <param name="linefeed">换行符</param>
        /// <param name="timeOut">读写超时时间</param>
        /// <returns></returns>
        public bool Open(int com, int port)
        {
            try
            {
                if(null == this.serialPort)
                {
                    this.serialPort = new SerialPort();
                }
                // 设置串口参数
                this.serialPort.PortName = "COM" + com;
                this.serialPort.BaudRate = port;

                // 打开串口
                if(!this.serialPort.IsOpen)
                {
                    this.serialPort.Open();
                }
            }
            catch (System.Exception ex)
            {
                WriteLog(string.Format("CommPort.Open(COM:{0}, Port:{1}) error: {2}", com, port, ex.Message));
            }
            return this.serialPort.IsOpen;
        }

        /// <summary>
        /// 关闭已打开的串口
        /// </summary>
        /// <returns></returns>
        public bool Close()
        {
            try
            {
                if (IsOpen())
                {
                    this.serialPort.Close();
                }
                return true;
            }
            catch (System.Exception ex)
            {
                WriteLog("CommPort.Close() error: " + ex.Message);
            }
            return false;
        }

        /// <summary>
        /// 从已打开的串口中读数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public int Read(ref byte[] buffer)
        {
            try
            {
                if(null != this.serialPort)
                {
                    return this.serialPort.Read(buffer, 0, buffer.Length);
                }
            }
            catch (System.Exception ex)
            {
                WriteLog("CommPort.Read() error: " + ex.Message);
            }
            return 0;
        }

        /// <summary>
        /// 从已打开的串口中读数据
        ///     一直读取到输入缓冲区中的 System.IO.Ports.SerialPort.NewLine 值。
        /// </summary>
        /// <returns></returns>
        public string ReadLine()
        {
            try
            {
                if (null != this.serialPort)
                {
                    return this.serialPort.ReadLine();
                }
            }
            catch (System.Exception ex)
            {
                WriteLog("CommPort.ReadLine() error: " + ex.Message);
            }
            return string.Empty;
        }

        /// <summary>
        /// 写入数据到已打开的串口
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool Write(string text)
        {
            try
            {
                if(null != this.serialPort)
                {
                    this.serialPort.Write(text);
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                WriteLog(string.Format("CommPort.Write({0}) error: {1}", text, ex.Message));
            }
            return false;
        }

        /// <summary>
        /// 设置换行符
        /// </summary>
        /// <param name="linefeed"></param>
        public void SetLinefeed(string linefeed = "\r\n")
        {
            try
            {
                this.serialPort.NewLine = linefeed;
            }
            catch(System.Exception ex)
            {
                WriteLog(string.Format("CommPort.SetLine(linefeed:{0}) error: {1}", linefeed, ex.Message));
            }
        }

        /// <summary>
        /// 设置读写超时时间
        /// </summary>
        /// <param name="readTime"></param>
        /// <param name="writeTime"></param>
        public void SetTimeout(int readTime, int writeTime)
        {
            try
            {
                this.serialPort.ReadTimeout = readTime;
                this.serialPort.WriteTimeout = writeTime;
            }
            catch (System.Exception ex)
            {
                WriteLog(string.Format("CommPort.SetTimeout(ReadTimeout:{0}, WriteTimeout:{1}) error: {2}", readTime, writeTime, ex.Message));
            }
        }

        /// <summary>
        /// 写Log
        /// </summary>
        /// <param name="msg"></param>
        protected void WriteLog(string msg)
        {
            Trace.WriteLine(msg);
        }
        
        #endregion
    }
}