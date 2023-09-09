using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Machine
{
    /// <summary>
    /// Class RollText.
    /// Implements the <see cref="System.Windows.Forms.UserControl" />
    /// </summary>
    /// <seealso cref="System.Windows.Forms.UserControl" />
    public class RollText : UserControl
    {
        #region // 字段

        /// <summary>
        /// The rect text
        /// </summary>
        Rectangle rectText;
        /// <summary>
        /// The timer
        /// </summary>
        Timer _timer;
        /// <summary>
        /// The move step
        /// </summary>
        private int _moveStep = 5;
        /// <summary>
        /// The move sleep time
        /// </summary>
        private int _moveSleepTime = 100;
        /// <summary>
        /// 文本改变是否重新从边缘运动
        /// </summary>
        private bool _isChangeReset = false;
        /// <summary>
        /// 滚动文本列表
        /// </summary>
        private List<string> _lstRollText;
        /// <summary>
        /// 滚动文本列表索引
        /// </summary>
        private int _rollTextIdx;
        /// <summary>
        /// 循环滚动文本
        /// </summary>
        private bool _cycleRoll = true;

        #endregion

        #region // 属性

        [Description("文本改变是否重新从边缘运动"), Category("自定义")]
        public bool ISChangeReset
        {
            get { return _isChangeReset; }
            set
            {
                _isChangeReset = value;
            }
        }

        /// <summary>
        /// Gets or sets the move sleep time.
        /// </summary>
        /// <value>The move sleep time.</value>
        [Description("每次滚动间隔时间，越小速度越快"), Category("自定义")]
        public int MoveSleepTime
        {
            get { return _moveSleepTime; }
            set
            {
                if(value <= 0)
                {
                    return;
                }

                _moveSleepTime = value;
                _timer.Interval = value;
            }
        }

        /// <summary>
        /// Gets or sets the cycle roll.
        /// </summary>
        [Description("是否循环滚动：True循环，此时需要手动清空滚动文字；False不循环自动清空"), Category("自定义")]
        public bool CycleRoll
        {
            get
            {
                return _cycleRoll;
            }

            set
            {
                this._cycleRoll = value;
            }
        }

        #endregion

        #region // 构造函数

        /// <summary>
        /// Initializes a new instance of the <see cref="RollText" /> class.
        /// </summary>
        public RollText()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.DoubleBuffer, true);
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.Selectable, true);
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.SetStyle(ControlStyles.UserPaint, true);

            this.Size = new Size(450, 30);
            _timer = new Timer();
            _timer.Interval = 100;
            _timer.Tick += timer_Tick;
            this.TextChanged += RollText_TextChanged;
            this.SizeChanged += RollText_SizeChanged;
            this.VisibleChanged += RollText_VisibleChanged;
            this.ForeColor = Color.FromArgb(255, 77, 59);
            this._lstRollText = new List<string>();
        }

        #endregion

        #region // 方法

        /// <summary>
        /// 添加滚动文字
        /// </summary>
        /// <param name="txt"></param>
        public void AddRollText(string txt)
        {
            this._lstRollText.Add(txt);
            if(string.IsNullOrEmpty(Text))
            {
                this._rollTextIdx = 0;
                Text = txt;
            }
        }

        /// <summary>
        /// 清空滚动文字
        /// </summary>
        /// <param name="txt"></param>
        public void ClearRollText()
        {
            this._lstRollText.Clear();
        }

        /// <summary>
        /// Handles the Tick event of the m_timer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void timer_Tick(object sender, EventArgs e)
        {
            if(rectText == Rectangle.Empty)
            {
                return;
            }

            rectText.X = rectText.X - _moveStep;
            if(rectText.Right < rectText.Width / 10)
            {
                //rectText.X = _isChangeReset ? 0 : this.Width / 2;
                this._rollTextIdx++;
                if(this._rollTextIdx >= this._lstRollText.Count)
                {
                    if(this.CycleRoll)
                    {
                        this._rollTextIdx = 0;
                    }
                    else
                    {
                        ClearRollText();
                    }
                }
                if(this._lstRollText.Count > 0)
                {
                    Text = this._lstRollText[this._rollTextIdx];
                }
                else
                {
                    this._rollTextIdx = 0;
                    Text = string.Empty;
                }
            }
            Refresh();
        }

        /// <summary>
        /// Handles the TextChanged event of the RollText control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">instance containing the event data.</param>
        private void RollText_TextChanged(object sender, EventArgs e)
        {
            if(!string.IsNullOrEmpty(Text))
            {
                Graphics g = this.CreateGraphics();
                var size = g.MeasureString(Text, this.Font);
                rectText = new Rectangle(_isChangeReset ? 0 : this.Width / 2, (this.Height - rectText.Height) / 2 + 1, (int)size.Width, (int)size.Height);
            }
            else
            {
                rectText = Rectangle.Empty;
            }
        }

        /// <summary>
        /// Handles the VisibleChanged event of the RollText control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void RollText_VisibleChanged(object sender, EventArgs e)
        {
            _timer.Enabled = this.Visible;
        }

        /// <summary>
        /// Handles the SizeChanged event of the UCRollText control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void RollText_SizeChanged(object sender, EventArgs e)
        {
            if(rectText != Rectangle.Empty)
            {
                rectText.Y = (this.Height - rectText.Height) / 2 + 1;
            }
        }

        /// <summary>
        /// 引发 <see cref="E:System.Windows.Forms.Control.Paint" /> 事件。
        /// </summary>
        /// <param name="e">包含事件数据的 <see cref="T:System.Windows.Forms.PaintEventArgs" />。</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if(rectText != Rectangle.Empty)
            {
                e.Graphics.DrawString(Text, Font, new SolidBrush(ForeColor), rectText.Location);
            }
        }

        #endregion
    }
}
