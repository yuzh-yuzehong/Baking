using System;
using System.Drawing;
using System.Windows.Forms;

namespace Machine
{
    public partial class ModifyMotorParameterPage : Form
    {
        public ModifyMotorParameterPage()
        {
            InitializeComponent();

            // 创建参数列表
            CreateParameterList();
        }

        #region // 字段

        // 复位速度 - 中等速度 - 快速速度 - 默认速度
        private Label[] lblSpeed;
        private TextBox[] txtSpeed;
        private Label[] lblAccTime;
        private TextBox[] txtAccTime;
        private Label[] lblDecTime;
        private TextBox[] txtDecTime;

        #endregion

        /// <summary>
        /// 窗体加载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ModifyMotorParameterPage_Load(object sender, EventArgs e)
        {
            // 设置提示
            ToolTip tip = new ToolTip();
            tip.SetToolTip(this.tablePanelDefault, "自动运行时使用");
            tip.SetToolTip(this.tablePanelFast, "暂未使用");
            tip.SetToolTip(this.tablePanelMedium, "手动调试电机动作时使用");
            tip.SetToolTip(this.tablePanelReset, "搜索原点时使用");
        }

        /// <summary>
        /// 创建设置列表
        /// </summary>
        private void CreateParameterList()
        {
            lblSpeed = new Label[4];
            txtSpeed = new TextBox[4];
            lblAccTime = new Label[4];
            txtAccTime = new TextBox[4];
            lblDecTime = new Label[4];
            txtDecTime = new TextBox[4];

            int panelIndex = 0;
            foreach(Control group in this.tablePanelParaPage.Controls)
            {
                foreach(var item in group.Controls)
                {
                    TableLayoutPanel panel = item as TableLayoutPanel;
                    if (null != panel)
                    {
                        int row, col;
                        row = col = 3;
                        float fHig = (float)(100.0 / row);

                        panel.RowCount = row;
                        panel.ColumnCount = col;
                        panel.Padding = new Padding(0, 10, 0, 0);
                        // 设置行列风格
                        for(int i = 0; i < panel.RowCount; i++)
                        {
                            if(i < panel.RowStyles.Count)
                            {
                                panel.RowStyles[i] = new RowStyle(SizeType.Percent, fHig);
                            }
                            else
                            {
                                panel.RowStyles.Add(new RowStyle(SizeType.Percent, fHig));
                            }
                        }
                        for(int i = panel.ColumnStyles.Count; i < col; i++)
                        {
                            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, fHig));
                        }
                        panel.ColumnStyles[0] = new ColumnStyle(SizeType.Percent, (float)(35.0));
                        panel.ColumnStyles[1] = new ColumnStyle(SizeType.Percent, (float)(50.0));
                        panel.ColumnStyles[2] = new ColumnStyle(SizeType.Percent, (float)(15.0));
                        // 添加控件
                        int index = 0;
                        lblSpeed[panelIndex] = new Label();
                        lblSpeed[panelIndex].Text = "移动速度：";
                        panel.Controls.Add(lblSpeed[panelIndex], 0, index);
                        txtSpeed[panelIndex] = new TextBox();
                        panel.Controls.Add(txtSpeed[panelIndex], 1, index);
                        Label lbl = new Label();
                        lbl.Text = "mm/s";
                        panel.Controls.Add(lbl, 2, index++);

                        lblAccTime[panelIndex] = new Label();
                        lblAccTime[panelIndex].Text = "加速时间：";
                        panel.Controls.Add(lblAccTime[panelIndex], 0, index);
                        txtAccTime[panelIndex] = new TextBox();
                        panel.Controls.Add(txtAccTime[panelIndex], 1, index);
                        lbl = new Label();
                        lbl.Text = "s";
                        panel.Controls.Add(lbl, 2, index++);

                        lblDecTime[panelIndex] = new Label();
                        lblDecTime[panelIndex].Text = "减速时间：";
                        panel.Controls.Add(lblDecTime[panelIndex], 0, index);
                        txtDecTime[panelIndex] = new TextBox();
                        panel.Controls.Add(txtDecTime[panelIndex], 1, index);
                        lbl = new Label();
                        lbl.Text = "s";
                        panel.Controls.Add(lbl, 2, index++);
                        // 设置位置
                        foreach(Control con in panel.Controls)
                        {
                            if(con is Label)
                            {
                                ((Label)con).Anchor = AnchorStyles.Left | AnchorStyles.Right;
                                ((Label)con).Font = new Font(((Label)con).Font.FontFamily, 10);
                            }
                            else if(con is TextBox)
                            {
                                ((TextBox)con).Dock = DockStyle.Fill;
                                ((TextBox)con).Font = new Font(((TextBox)con).Font.FontFamily, 12);
                            }
                        }

                        panelIndex++;
                        if(panelIndex >= 4)
                        {
                            continue;
                        }
                    }

                }
            }
        }

        /// <summary>
        /// 设置速度参数：4组
        /// </summary>
        /// <param name="speed"></param>
        /// <param name="accTime"></param>
        /// <param name="decTime"></param>
        public void SetSpeedList(float[] speed, float[] accTime, float[] decTime)
        {
            for(int i = 0; i < 4; i++)
            {
                txtSpeed[i].Text = speed[i].ToString("#0.00");
                txtAccTime[i].Text = accTime[i].ToString("#0.00");
                txtDecTime[i].Text = decTime[i].ToString("#0.00");
            }
        }

        /// <summary>
        /// 获取速度参数：4组
        /// </summary>
        /// <param name="speed"></param>
        /// <param name="accTime"></param>
        /// <param name="decTime"></param>
        public void GetSpeedList(ref float[] speed, ref float[] accTime, ref float[] decTime)
        {
            for(int i = 0; i < 4; i++)
            {
                speed[i] = Convert.ToSingle(txtSpeed[i].Text);
                accTime[i] = Convert.ToSingle(txtAccTime[i].Text);
                decTime[i] = Convert.ToSingle(txtDecTime[i].Text);
            }
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSave_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// 取消
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

    }
}
