using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Machine
{
    public class TipDlg : Form
    {
        WebBrowser webbTip;

        public TipDlg()
        {
            InitializeForm();
        }

        private void InitializeForm()
        {
            this.webbTip = new WebBrowser();
            this.SuspendLayout();

            this.webbTip.Dock = DockStyle.Fill;
            this.webbTip.Location = new Point(0, 0);
            this.webbTip.MinimumSize = new Size(200, 20);
            this.webbTip.Name = "webbTip";
            this.webbTip.ScrollBarsEnabled = false;
            this.webbTip.Size = new Size(300, 50);
            this.webbTip.TabIndex = 0;
            this.webbTip.WebBrowserShortcutsEnabled = false;

            this.AutoScaleDimensions = new SizeF(6F, 12F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(300, 50);
            this.Controls.Add(this.webbTip);
            this.FormBorderStyle = FormBorderStyle.None;
            this.Name = "TipDlg";
            this.Opacity = 0.95D;
            this.StartPosition = FormStartPosition.Manual;
            this.Text = "TipDlg";
            this.SizeChanged += new EventHandler(this.TipDlg_SizeChanged);
            this.Controls.Add(this.webbTip);

            this.ResumeLayout(false);
        }

        /// <summary>
        /// 设置窗口属性：顶层显示，无焦点
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_NOACTIVATE = 0x08000000;
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_NOACTIVATE;
                return cp;
            }
        }

        #region // 对外接口

        /// <summary>
        /// 画圆角矩形界面
        /// </summary>
        private void TipDlg_SizeChanged(object sender, EventArgs e)
        {
            int nWinHeight = Height;
            int nWinWidth = Width;
            float fTension = 0.1f;
            int nRadius = 28;

            GraphicsPath oPath = new GraphicsPath();
            Point[] pointPath = new Point[]
            {
                new Point(0, nWinHeight / nRadius),
                new Point(nWinWidth / nRadius, 0),
                new Point(nWinWidth - nWinWidth / nRadius, 0),
                new Point(nWinWidth, nWinHeight / nRadius),
                new Point(nWinWidth, nWinHeight - nWinHeight / nRadius),
                new Point(nWinWidth - nWinWidth / nRadius, nWinHeight),
                new Point(nWinWidth / nRadius, nWinHeight),
                new Point(0, nWinHeight - nWinHeight / nRadius),
            };

            oPath.AddClosedCurve(pointPath, fTension);
            this.Region = new Region(oPath);
        }

        /// <summary>
        /// 获取内容宽度
        /// </summary>
        public int GetContentWidth()
        {
            if(null != webbTip.Document.Body)
            {
                return webbTip.Document.Body.ScrollRectangle.Width;
            }
            return 60;
        }

        /// <summary>
        /// 获取内容宽度
        /// </summary>
        public int GetContentHeight()
        {
            if(null != webbTip.Document.Body)
            {
                return webbTip.Document.Body.ScrollRectangle.Height;
            }
            return 30;
        }

        /// <summary>
        /// 设置Html格式的内容
        /// </summary>
        public bool SetHtml(string strHtml)
        {
            if(null != strHtml)
            {
                this.webbTip.Navigate("about:blank");
                this.webbTip.Document.OpenNew(false);
                this.webbTip.Document.Write(strHtml);
                this.webbTip.Refresh();
            }
            return false;
        }

        #endregion

    }
}
