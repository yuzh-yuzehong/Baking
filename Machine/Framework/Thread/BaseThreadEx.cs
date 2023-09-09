using System;

namespace Machine
{
    public class BaseThreadEx : BaseThread
    {
        #region // 字段

        private Action callbackFunc;      // 委托循环函数

        #endregion


        #region // 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="func">委托方法</param>
        public BaseThreadEx(Action func) : base()
        {
            this.callbackFunc = func;
        }

        #endregion


        #region // 方法

        /// <summary>
        /// 循环函数
        /// </summary>
        protected override void RunWhile()
        {
            if (null != callbackFunc)
            {
                callbackFunc();
            }
        }

        #endregion
    }
}
