using HelperLibrary;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Machine
{
    public class BaseThread : IDisposable
    {
        #region // 字段

        private string taskName;    // 线程名
        private bool isTerminate;   // 指示线程终止
        private Task taskThread;    // 任务运行线程

        #endregion


        #region // 方法

        /// <summary>
        /// 构造函数
        /// </summary>
        public BaseThread()
        {
            this.isTerminate = true;
            this.taskThread = null;
        }

        /// <summary>
        /// 实现 IDisposable 接口
        /// </summary>
        public void Dispose()
        {
            ReleaseThread();
        }

        /// <summary>
        /// 初始化线程(开始运行)
        /// </summary>
        public bool InitThread(string name)
        {
            try
            {
                if (IsTerminate())
                {
                    this.taskName = name;
                    this.isTerminate = false;
                    this.taskThread = new Task(RunThread, TaskCreationOptions.LongRunning);
                    this.taskThread.Start();
                    WriteLog(string.Format("{0} id.{1} Start running.", this.taskName, this.taskThread.Id));
                }
                return true;
            }
            catch (System.Exception ex)
            {
                WriteLog(("BaseThread.InitThread() error : " + ex.Message), LogType.Error);
                return false;
            }
        }

        /// <summary>
        /// 释放线程(终止运行)
        /// </summary>
        public bool ReleaseThread()
        {
            try
            {
                this.isTerminate = true;
                if (null != this.taskThread)
                {
                    this.taskThread.Wait();
                    WriteLog(string.Format("{0} id.{1} end.", this.taskName, this.taskThread.Id));
                    this.taskThread = null;
                }
                return true;
            }
            catch (System.Exception ex)
            {
                WriteLog(("BaseThread.ReleaseThread() error : " + ex.Message), LogType.Error);
                return false;
            }
        }

        /// <summary>
        /// 线程终止状态
        /// </summary>
        public bool IsTerminate()
        {
            return this.isTerminate;
        }

        /// <summary>
        /// 运行线程
        /// </summary>
        private void RunThread()
        {
            while (!IsTerminate())
            {
                try
                {
                    RunWhile();
                }
                catch (System.Exception ex)
                {
                    WriteLog(("BaseThread.RunThread() error : " + ex.Message), LogType.Error);
                }
                Sleep(1);
            }
        }

        /// <summary>
        /// 循环函数
        /// </summary>
        protected virtual void RunWhile()
        {
            Trace.Assert(false, "BaseThread::RunWhile/this thread not enable run.");
        }

        /// <summary>
        /// 输出调试log
        /// </summary>
        /// <param name="msg"></param>
        protected void WriteLog(string msg, LogType type = LogType.Information)
        {
            //Trace.WriteLine(msg);
            Def.WriteLog(this.taskName, msg, type);
        }

        /// <summary>
        /// 线程休眠
        /// </summary>
        /// <param name="millisecondsTimeout"></param>
        protected void Sleep(int millisecondsTimeout)
        {
            Thread.Sleep(millisecondsTimeout);
        }

        #endregion
    }
}
