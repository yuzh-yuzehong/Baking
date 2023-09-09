using System;
using System.Drawing;
using System.Windows.Forms;

namespace Machine
{
    class IOButton : Button
    {
        public IOButton()
        {
            this.FlatStyle = FlatStyle.Flat;                        // 平面样式
            this.FlatAppearance.BorderSize = 1;                     // 边框大小
            this.FlatAppearance.BorderColor = Color.LightGray;      // 边框颜色
            this.BackColor = Color.Transparent;                     // 背景色
            this.MinimumSize = new Size(50, 50);                    // 最小大小
            this.MaximumSize = new Size(550, 50);                    // 最小大小
            this.Font = new Font("微软雅黑", 13);                   // 字体
            //this.TextAlign = ContentAlignment.MiddleCenter;         // 居中

            this.ImageAlign = ContentAlignment.MiddleLeft;          // 图像居中靠左
        }


        #region // 按钮状态

        /// <summary>
        /// 按钮模式：true输出，false输入
        /// </summary>
        /// <param name="btnMode"></param>
        public void SetBtnMode(bool btnMode)
        {
            this.buttonMode = btnMode;
            if (btnMode)
            {
                SetNomalColor(Color.LightSkyBlue, Color.Black);
                SetHoverColor(Color.SkyBlue, Color.Black);
                SetPressColor(Color.DeepSkyBlue, Color.Black);
                SetUnableColor(Color.LightGray, Color.Black);

                SetDefaultColor(Color.LightSkyBlue, Color.Black);
            }
            else
            {
                SetNomalColor(Color.Transparent, Color.Black);
                SetHoverColor(Color.Transparent, Color.Black);
                SetPressColor(Color.Transparent, Color.Black);
                SetUnableColor(Color.Transparent, Color.Black);

                SetDefaultColor(Color.Transparent, Color.Black);
            }
        }

        /// <summary>
        /// 使能：true启用，false禁用
        /// </summary>
        /// <param name="enable"></param>
        public void SetEnable(bool enable)
        {
            this.bEnable = enable;
            if (this.buttonMode)
            {
                if(enable)
                {
                    SetNomalColor(Color.LightSkyBlue, Color.Black);
                    SetHoverColor(Color.SkyBlue, Color.Black);
                    SetPressColor(Color.DeepSkyBlue, Color.Black);
                    SetUnableColor(Color.LightGray, Color.Black);

                    SetDefaultColor(Color.LightSkyBlue, Color.Black);
                }
                else
                {
                    SetNomalColor(Color.Transparent, Color.Black);
                    SetHoverColor(Color.Transparent, Color.Black);
                    SetPressColor(Color.Transparent, Color.Black);
                    SetUnableColor(Color.Transparent, Color.Black);

                    SetDefaultColor(Color.Transparent, Color.Black);
                }
            }
        }

        /// <summary>
        /// 设置按钮LED图片
        /// </summary>
        /// <param name="imgON">ON状态图</param>
        /// <param name="imgOFF">OFF状态图</param>
        public void SetLedImg(Image imgON, Image imgOFF)
        {
            this.imgLedON = imgON;
            this.imgLedOFF = imgOFF;
        }

        /// <summary>
        /// 设置按钮状态：ON/OFF
        /// </summary>
        public void SetState(bool state)
        {
            this.bState = state;
            this.Image = state ? imgLedON : imgLedOFF;
        }

        /// <summary>
        /// 仅设置按钮文本，获取为空
        /// </summary>
        public override string Text
        {
            get
            {
                return base.Text;
            }

            set
            {
                base.Text = "";
                this.buttonText = value;
                this.Invalidate();
            }
        }


        /// <summary>
        /// 设置正常默认状态按钮颜色
        /// </summary>
        /// <param name="clrBk">背景色</param>
        /// <param name="clrTxt">文本色</param>
        protected void SetNomalColor(Color clrBk, Color clrTxt)
        {
            this.clrBkNomal = clrBk;
            this.clrTxtNomal = clrTxt;
        }

        /// <summary>
        /// 设置悬停状态按钮颜色
        /// </summary>
        /// <param name="clrBk">背景色</param>
        /// <param name="clrTxt">文本色</param>
        protected void SetHoverColor(Color clrBk, Color clrTxt)
        {
            this.clrBkHover = clrBk;
            this.clrTxtHover = clrTxt;
        }

        /// <summary>
        /// 设置按下状态按钮颜色
        /// </summary>
        /// <param name="clrBk">背景色</param>
        /// <param name="clrTxt">文本色</param>
        protected void SetPressColor(Color clrBk, Color clrTxt)
        {
            this.clrBkPress = clrBk;
            this.clrTxtPress = clrTxt;
        }

        /// <summary>
        /// 设置禁用状态按钮颜色
        /// </summary>
        /// <param name="clrBk">背景色</param>
        /// <param name="clrTxt">文本色</param>
        protected void SetUnableColor(Color clrBk, Color clrTxt)
        {
            this.clrBkUnable = clrBk;
            this.clrTxtUnable = clrTxt;
        }

        /// <summary>
        /// 设置默认按钮颜色
        /// </summary>
        /// <param name="clrBk"></param>
        /// <param name="clrTxt"></param>
        protected void SetDefaultColor(Color clrBk, Color clrTxt)
        {
            this.BackColor = clrBk;
            this.ForeColor = clrTxt;
        }

        #endregion


        #region // 重绘
        
        /// <summary>
        /// 界面重绘
        /// </summary>
        /// <param name="pevent"></param>
        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);
            try
            {
                Graphics g = pevent.Graphics;
                // 按钮区域
                Rectangle rect = new Rectangle((this.ClientRectangle.X + this.ClientRectangle.Height), this.ClientRectangle.Y, this.ClientRectangle.Width, this.ClientRectangle.Height);
                // 字符串左对齐
                StringFormat strF = new StringFormat();
                strF.Alignment = StringAlignment.Near;
                strF.LineAlignment = StringAlignment.Center;
                g.DrawString(this.buttonText, this.Font, new SolidBrush(this.ForeColor), rect, strF);
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("IOButton OnPaint()", $" error: {ex.Message}\r\n{ex.StackTrace}");
            }
        }

        #endregion


        #region // 鼠标操作

        /// <summary>
        /// 鼠标按下
        /// </summary>
        /// <param name="mevent"></param>
        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            this.BackColor = clrBkPress;
            this.ForeColor = clrTxtPress;

            base.OnMouseDown(mevent);
        }

        /// <summary>
        /// 鼠标抬起
        /// </summary>
        /// <param name="mevent"></param>
        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            this.BackColor = clrBkNomal;
            this.ForeColor = clrTxtNomal;

            base.OnMouseUp(mevent);
        }

        /// <summary>
        /// 鼠标进入
        /// </summary>
        /// <param name="eventargs"></param>
        protected override void OnMouseEnter(EventArgs eventargs)
        {
            this.BackColor = clrBkHover;
            this.ForeColor = clrTxtHover;

            base.OnMouseEnter(eventargs);
        }

        /// <summary>
        /// 鼠标离开
        /// </summary>
        /// <param name="eventargs"></param>
        protected override void OnMouseLeave(EventArgs eventargs)
        {
            this.BackColor = clrBkNomal;
            this.ForeColor = clrTxtNomal;
            
            base.OnMouseLeave(eventargs);
        }

        #endregion


        #region // IO状态
        private Image imgLedON;         // Led ON
        private Image imgLedOFF;        // Led OFF
        private bool bState;            // 状态
        public bool bEnable;            // 使能：true启用，false禁用
        private string buttonText;      // 按钮文本
        private bool buttonMode;        // 按钮模式：true输出，false输入

        private Color clrBkNomal;       // 正常状态，背景
        private Color clrTxtNomal;      // 正常状态，文字
        private Color clrBkHover;       // 悬停状态，背景
        private Color clrTxtHover;      // 悬停状态，文字
        private Color clrBkPress;       // 按下状态，背景
        private Color clrTxtPress;      // 按下状态，文字
        private Color clrBkUnable;      // 禁用状态，背景
        private Color clrTxtUnable;     // 禁用状态，文字
        #endregion
    }
}
