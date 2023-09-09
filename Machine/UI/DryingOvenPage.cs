using HelperLibrary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using SystemControlLibrary;

namespace Machine
{
    public partial class DryingOvenPage : FormEx
    {
        #region // 字段

        private RunProcessDryingOven dryingOvenRun;     // 干燥炉模组
        private System.Timers.Timer timerUpdata;        // 界面更新定时器
        private DryOvenCmd dryOvenCmd;                  // 干燥炉指令
        private OvenStatus dryOvenCmdState;             // 干燥炉操作打开
        private bool runWhileRun;                       // 操作线程运行
        private Task runWhileTask;                      // 操作线程
        private int cavityIndex;                        // 炉层索引
        private Chart chartTemp;                        // 夹具加热温度曲线

        #endregion

        public DryingOvenPage()
        {
            InitializeComponent();

            CreateDryingOvenList();

            // 创建参数列表
            CreateParameterListView();
            // 创建状态列表
            CreateStateListView();
            // 创建温度列表      
            CreateTempListView();
            // 创建报警列表
            CreateAlarmListView();
        }

        #region // 创建列表视图

        private void CreateDryingOvenList()
        {
            for(RunID i = RunID.DryOven0; i < RunID.DryOvenALL; i++)
            {
                RunProcessDryingOven run = MachineCtrl.GetInstance().GetModule(i) as RunProcessDryingOven;
                if(null != run)
                {
                    this.comboBoxDryingOven.Items.Add(run.RunName);
                }
            }
            if(this.comboBoxDryingOven.Items.Count > 0)
            {
                this.comboBoxDryingOven.SelectedIndex = 0;
            }
            this.radioButton1.Checked = true;

            #region // 限定控件最大范围

            TableLayoutPanel[] tlp = new TableLayoutPanel[] { this.tableLayoutPanel3 };
            for(int i = 0; i < tlp.Length; i++)
            {
                foreach(Control item in tlp[i].Controls)
                {
                    Button btn = item as Button;
                    if(null != btn)
                    {
                        btn.MaximumSize = new Size(100, 30);
                    }
                }
            }
            #endregion
        }

        private void CreateParameterListView()
        {
            int width = this.listViewParameter.ClientSize.Width / 10;
            // 设置表格
            this.listViewParameter.View = View.Details;        // 带标题的表格
            this.listViewParameter.GridLines = true;           // 显示行列网格线
            this.listViewParameter.FullRowSelect = true;       // 整行选中

            this.listViewParameter.Font = new Font(this.listViewParameter.Font.FontFamily, 11);

            this.listViewParameter.Columns.Add("参数名", width * 7, HorizontalAlignment.Center);
            this.listViewParameter.Columns.Add("参数", width * 3, HorizontalAlignment.Center);

            // 设置表格高度
            ImageList iList = new ImageList();
            iList.ImageSize = new System.Drawing.Size(1, 30);
            this.listViewParameter.SmallImageList = iList;
            // 设置表格项
            this.listViewParameter.BeginUpdate();      // 数据更新，UI暂时挂起，直到EndUpdate绘制控件，可以有效避免闪烁并大大提高加载速度

            ListViewItem item = new ListViewItem();
            item.Text = "1)设定温度";
            item.SubItems.Add("0");
            this.listViewParameter.Items.Add(item);

            item = new ListViewItem();
            item.Text = "2)温度上限";
            item.SubItems.Add("0");
            this.listViewParameter.Items.Add(item);

            item = new ListViewItem();
            item.Text = "3)温度下限";
            item.SubItems.Add("0");
            this.listViewParameter.Items.Add(item);

            item = new ListViewItem();
            item.Text = "4)预热时间";
            item.SubItems.Add("0");
            this.listViewParameter.Items.Add(item);

            item = new ListViewItem();
            item.Text = "5)真空加热时间";
            item.SubItems.Add("0");
            this.listViewParameter.Items.Add(item);

            item = new ListViewItem();
            item.Text = "6)开门破真空时长";
            item.SubItems.Add("0");
            this.listViewParameter.Items.Add(item);

            item = new ListViewItem();
            item.Text = "7)开门真空压力";
            item.SubItems.Add("0");
            this.listViewParameter.Items.Add(item);

            item = new ListViewItem();
            item.Text = "8)A状态抽真空时间";
            item.SubItems.Add("0");
            this.listViewParameter.Items.Add(item);

            item = new ListViewItem();
            item.Text = "9)A状态真空压力";
            item.SubItems.Add("0");
            this.listViewParameter.Items.Add(item);

            item = new ListViewItem();
            item.Text = "10)B状态抽真空时间";
            item.SubItems.Add("0");
            this.listViewParameter.Items.Add(item);

            item = new ListViewItem();
            item.Text = "11)B状态真空压力";
            item.SubItems.Add("0");
            this.listViewParameter.Items.Add(item);

            item = new ListViewItem();
            item.Text = "12)呼吸充干燥气时间";
            item.SubItems.Add("0");
            this.listViewParameter.Items.Add(item);

            item = new ListViewItem();
            item.Text = "13)呼吸充干燥气压力";
            item.SubItems.Add("0");
            this.listViewParameter.Items.Add(item);

            item = new ListViewItem();
            item.Text = "14)呼吸充干燥气保持时间";
            item.SubItems.Add("0");
            this.listViewParameter.Items.Add(item);

            item = new ListViewItem();
            item.Text = "15)呼吸时间间隔";
            item.SubItems.Add("0");
            this.listViewParameter.Items.Add(item);

            item = new ListViewItem();
            item.Text = "16)呼吸循环次数";
            item.SubItems.Add("0");
            this.listViewParameter.Items.Add(item);

            item = new ListViewItem();
            item.Text = "17)发热板数";
            item.SubItems.Add("0");
            this.listViewParameter.Items.Add(item);

            item = new ListViewItem();
            item.Text = "18)最大NG发热板数";
            item.SubItems.Add("0");
            this.listViewParameter.Items.Add(item);

            item = new ListViewItem();
            item.Text = "19)加热前抽真空时间";
            item.SubItems.Add("0");
            this.listViewParameter.Items.Add(item);

            item = new ListViewItem();
            item.Text = "20)加热前充干燥气压力";
            item.SubItems.Add("0");
            this.listViewParameter.Items.Add(item);

            // 调整列宽
            this.listViewParameter.Columns[0].Width = -2;

            this.listViewParameter.EndUpdate();        // 结束数据处理，UI界面一次性绘制。
        }

        private void CreateStateListView()
        {
            int width = this.listViewState.ClientSize.Width / 100;
            // 设置表格
            this.listViewState.View = View.Details;        // 带标题的表格
            this.listViewState.GridLines = true;           // 显示行列网格线
            this.listViewState.FullRowSelect = true;       // 整行选中                                                          
            this.listViewState.Font = new Font(this.listViewState.Font.FontFamily, 11);
            // 设置标题
            this.listViewState.Columns.Add("状态", width * 50, HorizontalAlignment.Center);      // 设置表格标题
            this.listViewState.Columns.Add("1层", width * 30, HorizontalAlignment.Center);
            this.listViewState.Columns.Add("2层", width * 30, HorizontalAlignment.Center);
            this.listViewState.Columns.Add("3层", width * 30, HorizontalAlignment.Center);
            this.listViewState.Columns.Add("4层", width * 30, HorizontalAlignment.Center);
            
            // 设置表格高度
            ImageList iList = new ImageList();
            iList.ImageSize = new System.Drawing.Size(1, 20);
            this.listViewState.SmallImageList = iList;
            // 设置表格项
            this.listViewState.BeginUpdate();      // 数据更新，UI暂时挂起，直到EndUpdate绘制控件，可以有效避免闪烁并大大提高加载速度
            // 添加状态项
            string[] state = new string[(int)OvenRowCol.MaxRow];
            for(int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            {
                state[i] = "";
            }
            ListViewItem item = new ListViewItem();
            item.Text = "炉门";
            item.SubItems.AddRange(state);
            this.listViewState.Items.Add(item);

            item = new ListViewItem();
            item.Text = "运行状态";
            item.SubItems.AddRange(state);
            this.listViewState.Items.Add(item);

            item = new ListViewItem();
            item.Text = "运行时间";
            item.SubItems.AddRange(state);
            this.listViewState.Items.Add(item);

            item = new ListViewItem();
            item.Text = "真空值";
            item.SubItems.AddRange(state);
            this.listViewState.Items.Add(item);

            item = new ListViewItem();
            item.Text = "保压状态";
            item.SubItems.AddRange(state);
            this.listViewState.Items.Add(item);

            item = new ListViewItem();
            item.Text = "真空阀";
            item.SubItems.AddRange(state);
            this.listViewState.Items.Add(item);

            item = new ListViewItem();
            item.Text = "破真空阀";
            item.SubItems.AddRange(state);
            this.listViewState.Items.Add(item);

            item = new ListViewItem();
            item.Text = "光幕状态";
            item.SubItems.AddRange(state);
            this.listViewState.Items.Add(item);

            item = new ListViewItem();
            item.Text = "炉门报警";
            item.SubItems.AddRange(state);
            this.listViewState.Items.Add(item);

            item = new ListViewItem();
            item.Text = "真空报警";
            item.SubItems.AddRange(state);
            this.listViewState.Items.Add(item);

            item = new ListViewItem();
            item.Text = "真空计报警";
            item.SubItems.AddRange(state);
            this.listViewState.Items.Add(item);

            item = new ListViewItem();
            item.Text = "破真空报警";
            item.SubItems.AddRange(state);
            this.listViewState.Items.Add(item);

            for(int i = 0; i < (int)OvenRowCol.MaxCol; i++)
            {
                item = new ListViewItem();
                item.Text = $"夹具{i + 1}机械温控报警";
                item.SubItems.AddRange(state);
                this.listViewState.Items.Add(item);
            }
            for(int i = 0; i < (int)OvenRowCol.MaxCol; i++)
            {
                item = new ListViewItem();
                item.Text = $"夹具{i + 1}放平检测报警";
                item.SubItems.AddRange(state);
                this.listViewState.Items.Add(item);
            }

            // 调整列宽
            this.listViewState.Columns[0].Width = -2;

            this.listViewState.EndUpdate();        // 结束数据处理，UI界面一次性绘制。
        }

        private void CreateTempListView()
        {
            int width = this.Width / 100;
            // 设置表格
            this.listViewTemp.View = View.Details;        // 带标题的表格
            this.listViewTemp.GridLines = true;           // 显示行列网格线
            this.listViewTemp.FullRowSelect = true;       // 整行选中                                                          
            this.listViewTemp.Font = new Font(this.listViewState.Font.FontFamily, 11);
            // 设置标题
            this.listViewTemp.Columns.Add(" ", -2, HorizontalAlignment.Center);
            this.listViewTemp.Columns.Add("1#控温", -2, HorizontalAlignment.Center);      // 设置表格标题
            this.listViewTemp.Columns.Add("1#巡检", -2, HorizontalAlignment.Center);
            this.listViewTemp.Columns.Add("2#控温", -2, HorizontalAlignment.Center);
            this.listViewTemp.Columns.Add("2#巡检", -2, HorizontalAlignment.Center);

            // 设置表格高度
            ImageList iList = new ImageList();
            iList.ImageSize = new System.Drawing.Size(1, 20);
            this.listViewTemp.SmallImageList = iList;
            // 设置表格项
            this.listViewTemp.BeginUpdate();      // 数据更新，UI暂时挂起，直到EndUpdate绘制控件，可以有效避免闪烁并大大提高加载速度
            for(int i = 0; i < (int)OvenInfoCount.HeatPanelCount; i++)            // 添加10行数据
            {
                ListViewItem item = new ListViewItem();
                item.Text = (i + 1).ToString();
                item.SubItems.Add("0");
                item.SubItems.Add("0");
                item.SubItems.Add("0");
                item.SubItems.Add("0");

                this.listViewTemp.Items.Add(item);
            }
            this.listViewTemp.EndUpdate();        // 结束数据处理，UI界面一次性绘制。
        }

        private void CreateAlarmListView()
        {
            int width = this.Width / 100;
            // 设置表格
            this.listViewAlarm.View = View.Details;        // 带标题的表格
            this.listViewAlarm.GridLines = true;           // 显示行列网格线
            this.listViewAlarm.FullRowSelect = true;       // 整行选中                                                          
            this.listViewAlarm.Font = new Font(this.listViewState.Font.FontFamily, 11);
            // 设置标题
            this.listViewAlarm.Columns.Add("", -2, HorizontalAlignment.Center);
            this.listViewAlarm.Columns.Add("1#报警温度", -2, HorizontalAlignment.Center);
            this.listViewAlarm.Columns.Add("1#报警状态", -2, HorizontalAlignment.Center);

            this.listViewAlarm.Columns.Add("2#报警温度", -2, HorizontalAlignment.Center);
            this.listViewAlarm.Columns.Add("2#报警状态", -2, HorizontalAlignment.Center);

            // 设置表格高度
            ImageList iList = new ImageList();
            iList.ImageSize = new System.Drawing.Size(1, 20);
            this.listViewAlarm.SmallImageList = iList;
            // 设置表格项
            this.listViewAlarm.BeginUpdate();      // 数据更新，UI暂时挂起，直到EndUpdate绘制控件，可以有效避免闪烁并大大提高加载速度
            for(int i = 0; i < (int)OvenInfoCount.HeatPanelCount; i++)            // 添加20行数据
            {
                ListViewItem item = new ListViewItem();
                item.Text = (i + 1).ToString();
                item.SubItems.Add("");
                item.SubItems.Add("");

                item.SubItems.Add("");
                item.SubItems.Add("");

                this.listViewAlarm.Items.Add(item);
            }
            this.listViewAlarm.EndUpdate();        // 结束数据处理，UI界面一次性绘制。
        }

        private void CreateChartView()
        {
            Font font = this.checkBoxChart.Font;

            // 图标控件
            this.chartTemp = new Chart();
            this.chartTemp.Hide();
            this.chartTemp.Dock = DockStyle.Fill;
            this.chartTemp.BackColor = Color.Transparent;

            // 标题
            this.chartTemp.Titles.Add("夹具加热温度曲线");
            this.chartTemp.Titles[0].ForeColor = Color.Black;
            this.chartTemp.Titles[0].Font = new Font(font.FontFamily, 12f, FontStyle.Bold);
            this.chartTemp.Titles[0].Alignment = ContentAlignment.TopCenter;

            // 图表区背景
            this.chartTemp.ChartAreas.Add(new ChartArea());
            this.chartTemp.ChartAreas[0].BackColor = Color.Transparent;
            this.chartTemp.ChartAreas[0].BorderColor = Color.Transparent;
            // X轴标签间距
            this.chartTemp.ChartAreas[0].AxisX.Interval = 60 * 10;
            this.chartTemp.ChartAreas[0].AxisX.LabelStyle.IsStaggered = true;
            this.chartTemp.ChartAreas[0].AxisX.LabelStyle.Angle = -45;
            this.chartTemp.ChartAreas[0].AxisX.TitleFont = new Font(font.FontFamily, 10f, FontStyle.Regular);
            this.chartTemp.ChartAreas[0].AxisX.TitleForeColor = Color.Black;
            // X坐标轴颜色
            this.chartTemp.ChartAreas[0].AxisX.LineColor = Color.LightGray;
            this.chartTemp.ChartAreas[0].AxisX.LabelStyle.ForeColor = Color.Black;
            this.chartTemp.ChartAreas[0].AxisX.LabelStyle.Font = new Font(font.FontFamily, 10f, FontStyle.Regular);
            // X坐标轴标题
            this.chartTemp.ChartAreas[0].AxisX.Title = "时间(秒)";
            this.chartTemp.ChartAreas[0].AxisX.TitleFont = new Font(font.FontFamily, 10f, FontStyle.Regular);
            this.chartTemp.ChartAreas[0].AxisX.TitleForeColor = Color.Black;
            this.chartTemp.ChartAreas[0].AxisX.TextOrientation = TextOrientation.Horizontal;
            this.chartTemp.ChartAreas[0].AxisX.ToolTip = "时间(秒)";
            // X轴网络线条
            this.chartTemp.ChartAreas[0].AxisX.MajorGrid.Enabled = true;
            this.chartTemp.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.Gray;

            // Y轴标签间距
            this.chartTemp.ChartAreas[0].AxisY.Minimum = 10;
            this.chartTemp.ChartAreas[0].AxisY.Interval = 10;
            this.chartTemp.ChartAreas[0].AxisY.LabelStyle.IsStaggered = true;
            this.chartTemp.ChartAreas[0].AxisY.LabelStyle.Angle = 0;
            this.chartTemp.ChartAreas[0].AxisY.TitleFont = new Font(font.FontFamily, 10f, FontStyle.Regular);
            this.chartTemp.ChartAreas[0].AxisY.TitleForeColor = Color.Black;
            // Y坐标轴颜色
            this.chartTemp.ChartAreas[0].AxisY.LineColor = Color.LightGray;
            this.chartTemp.ChartAreas[0].AxisY.LabelStyle.ForeColor = Color.Black;
            this.chartTemp.ChartAreas[0].AxisY.LabelStyle.Font = new Font(font.FontFamily, 10f, FontStyle.Regular);
            // Y坐标轴标题
            this.chartTemp.ChartAreas[0].AxisY.Title = "温度(℃)";
            this.chartTemp.ChartAreas[0].AxisY.TitleFont = new Font(font.FontFamily, 10f, FontStyle.Regular);
            this.chartTemp.ChartAreas[0].AxisY.TitleForeColor = Color.Black;
            this.chartTemp.ChartAreas[0].AxisY.TextOrientation = TextOrientation.Rotated270;
            this.chartTemp.ChartAreas[0].AxisY.ToolTip = "温度(℃)";
            // Y轴网格线条
            this.chartTemp.ChartAreas[0].AxisY.MajorGrid.Enabled = true;
            this.chartTemp.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.Gray;
            this.chartTemp.ChartAreas[0].AxisY2.LineColor = Color.Transparent;
            this.chartTemp.ChartAreas[0].BackGradientStyle = GradientStyle.TopBottom;

            this.chartTemp.Legends.Clear();

            // 曲线
            KnownColor color = KnownColor.ActiveBorder;
            for(int col = 0; col < (int)OvenRowCol.MaxCol; col++)
            {
                for(int i = 0; i < 2 * (int)OvenInfoCount.HeatPanelCount; i++)
                {
                    this.chartTemp.Series.Add($"{col + 1}.{i + 1}");
                }
            }
            foreach(var item in this.chartTemp.Series)
            {
                item.ChartType = SeriesChartType.Line;
                item.Color = Color.FromKnownColor(color++);

                //item.ToolTip = $"{item.Name}:\r\n#VALX - #VAL";   //鼠标移动到对应点显示数值
            }

            this.tablePanelOvenInfo.Controls.Add(this.chartTemp, 0, 0);
            this.tablePanelOvenInfo.SetRowSpan(this.chartTemp, this.tablePanelOvenInfo.RowCount);
            this.tablePanelOvenInfo.SetColumnSpan(this.chartTemp, this.tablePanelOvenInfo.ColumnCount);
        }

        #endregion

        #region // 窗体操作

        /// <summary>
        /// 加载窗体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DryingOvenPage_Load(object sender, EventArgs e)
        {
            if(this.comboBoxDryingOven.Items.Count > 0)
            {
                // 创建温度曲线图
                CreateChartView();

                // 开启定时器
                this.timerUpdata = new System.Timers.Timer();
                this.timerUpdata.Elapsed += UpdataDryingOvenPage;
                this.timerUpdata.Interval = 500;         // 间隔时间
                this.timerUpdata.AutoReset = true;       // 设置是执行一次（false）还是一直执行(true)；
                this.timerUpdata.Start();                // 开始执行定时器
                
                // 创建操作线程
                this.dryOvenCmd = DryOvenCmd.End;
                this.runWhileRun = true;
                this.runWhileTask = new Task(RunWhileThread, TaskCreationOptions.LongRunning);
                this.runWhileTask.Start();
                Def.WriteLog("DryingOvenPage", $"RunWhileThread() id.{this.runWhileTask.Id} Start running.", LogType.Success);
            }
            else
            {
                SetUIEnable(UIEnable.AllDisabled);
                this.checkBoxChart.Enabled = false;
            }
        }

        /// <summary>
        /// 释放线程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void DisposeForm()
        {
            try
            {
                // 关闭定时器
                if(null != this.timerUpdata)
                {
                    this.timerUpdata.Stop();
                }
                // 等待操作线程结束
                this.runWhileRun = false;
                if(null != this.runWhileTask)
                {
                    this.runWhileTask.Wait();
                    Def.WriteLog("DryingOvenPage", string.Format("RunWhileThread() id.{0} end.", this.runWhileTask.Id));
                }
            }
            catch(System.Exception ex)
            {
                Def.WriteLog("DryingOvenPage", string.Format("RunWhileThread() Release fail: {0}", ex.ToString()));
            }
        }

        /// <summary>
        /// UI界面可见性发生改变
        /// </summary>
        /// <param name="show">是否在前台显示</param>
        public override void UIVisibleChanged(bool show)
        {
            // 干燥炉非连接状态，停止定时器
            if((null != this.dryingOvenRun) && !this.dryingOvenRun.DryOvenIsConnect())
            {
                if(null != this.timerUpdata)
                {
                    this.timerUpdata.Stop();
                }
            }
            if(show)
            {
                UpdataUIEnable(MachineCtrl.GetInstance().RunsCtrl.GetMCState(), MachineCtrl.GetInstance().dbRecord.UserLevel());
            }
            else
            {
                UpdataUIEnable(MCState.MCRunning, UserLevelType.USER_LOGOUT);
            }
            base.UIVisibleChanged(show);
        }

        /// <summary>
        /// 当设备状态或用户权限改变时，更新UI界面的使能
        /// </summary>
        /// <param name="enable"></param>
        public override void UpdataUIEnable(SystemControlLibrary.MCState mc, SystemControlLibrary.UserLevelType level)
        {
            if(null == this.dryingOvenRun)
            {
                return;
            }

            try
            {
                if((MCState.MCInitializing == mc) || (MCState.MCRunning == mc))
                {
                    SetUIEnable(UIEnable.AllDisabled);
                }
                else
                {
                    SetUIEnable(UIEnable.AllEnabled);
                }
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("DryingOvenPage.UpdataUIEnable()", ex.Message, LogType.Error);
            }
            base.UpdataUIEnable(mc, level);
        }

        /// <summary>
        /// 解决窗体绘图时闪烁
        /// </summary>
        /// <param name="e">System.Windows.Forms.CreateParams，包含创建控件的句柄时所需的创建参数。</param>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
            /// <param name="e"></param>
        }

        /// <summary>
        /// 触发重绘，使其更新界面动画
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdataDryingOvenPage(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (this.Visible)
                {
                    this.Invoke(new Action(UpdataDryingOvenData));
                    this.labelDryingOven.Invalidate();
                }
            }
            catch(System.Exception ex)
            {
                Def.WriteLog("DryingOvenPage", "UpdataDryingOvenPage error: " + ex.Message);
            }
        }

        /// <summary>
        /// 绘制干燥炉
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void labelDryingOven_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pen pen = new Pen(Color.Black, 1);

            int ovenRow = (int)OvenRowCol.MaxRow;
            int ovenCol = (int)OvenRowCol.MaxCol;

            Rectangle rcOven = e.ClipRectangle;
            rcOven.Height -= 5;
            rcOven.Width -= 5;
            g.DrawRectangle(pen, rcOven);

            double cavityHig = rcOven.Height / (ovenRow + 1.0);

            int[] pltState = new int[(int)ModuleMaxPallet.DryingOven];
            for(int rowIdx = 0; rowIdx < ovenRow; rowIdx++)
            {
                Rectangle rcCavity = new Rectangle(5, (int)(rcOven.Bottom - cavityHig * (rowIdx + 1) - cavityHig / (ovenRow + 1.0) * (rowIdx + 1)), (int)(rcOven.Width - 10), (int)cavityHig);
                g.DrawRectangle(pen, rcCavity);
                
                for(int colIdx = 0; colIdx < ovenCol; colIdx++)
                {
                    int Idx = rowIdx * ovenCol + colIdx;
                    if (null != this.dryingOvenRun)
                    {
                        pltState[Idx] = MachineCtrl.GetInstance().GetPalletPosSenser((RunID)this.dryingOvenRun.GetRunID(), Idx);
                    }

                    Rectangle rcPal = new Rectangle((int)(rcCavity.Left + 5 * (colIdx + 1) + rcCavity.Width / 2.0 * colIdx), (int)(rcCavity.Top + 5), (int)(rcCavity.Width / 2.0 - 15), (int)(rcCavity.Height - 10));
                    Brush brush = Brushes.Transparent;
                    switch((OvenStatus)pltState[rowIdx * ovenCol + colIdx])
                    {
                        case OvenStatus.PalletNot:
                            break;
                        case OvenStatus.PalletHave:
                            brush = Brushes.DarkGray;
                            break;
                        default:
                            //brush = new HatchBrush(HatchStyle.Cross, Color.DarkGray, Color.Transparent);
                            break;
                    }
                    g.FillRectangle(brush, rcPal);
                    g.DrawRectangle(pen, rcPal);
                }
            }
        }

        /// <summary>
        /// 切换干燥炉
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBoxDryingOven_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox ovenList = sender as ComboBox;
            this.dryingOvenRun = null;
            if (null != ovenList)
            {
                RunID runId = (RunID)(ovenList.SelectedIndex + (int)RunID.DryOven0);
                this.dryingOvenRun = MachineCtrl.GetInstance().GetModule(runId) as RunProcessDryingOven;
                if (null != this.dryingOvenRun)
                {
                    this.labelOvenIP.Text = this.dryingOvenRun.GetDryOvenIPInfo();
                    this.labelOvenState.Text = this.dryingOvenRun.DryOvenIsConnect() ? "已连接" : "已断开";
                    if (this.dryingOvenRun.DryOvenIsConnect())
                    {
                        if(null != this.timerUpdata)
                        {
                            this.timerUpdata.Start();
                        }
                    }
                    if(this.checkBoxChart.Checked)
                    {
                        RefreshPltHeatTemp();
                    }
                }
            }
        }

        /// <summary>
        /// 切换炉层
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void radioButton_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if ((null != rb) && (rb.Checked))
            {
                this.cavityIndex = Convert.ToInt32(rb.Tag);

                if((null != this.dryingOvenRun) && (this.checkBoxChart.Checked))
                {
                    RefreshPltHeatTemp();
                }
            }
        }

        /// <summary>
        /// 设置界面控件使能
        /// </summary>
        /// <param name="uiEN"></param>
        private void SetUIEnable(UIEnable uiEN)
        {
            this.Invoke(new Action(() =>
            {
                switch(uiEN)
                {
                    case UIEnable.AllDisabled:
                    case UIEnable.AllEnabled:
                        {
                            bool en = (uiEN == UIEnable.AllEnabled);
                            
                            this.buttonConnect.Enabled = en;
                            this.buttonDisconnect.Enabled = en;
                            this.buttonOpenDoor.Enabled = en;
                            this.buttonCloseDoor.Enabled = en;
                            this.buttonOpenVac.Enabled = en;
                            this.buttonCloseVac.Enabled = en;
                            this.buttonOpenBlowAir.Enabled = en;
                            this.buttonCloseBlowAir.Enabled = en;
                            this.buttonWorkStart.Enabled = en;
                            this.buttonWorkStop.Enabled = en;
                            this.buttonSetParameter.Enabled = en;
                            this.buttonErrorReset.Enabled = en;
                            break;
                        }
                    default:
                        break;
                }
            }));
        }

        /// <summary>
        /// 切换干燥炉详情/温度曲线
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBoxChart_CheckedChanged(object sender, EventArgs e)
        {
            if(this.checkBoxChart.Checked)
            {
                this.checkBoxChart.Text = "干燥炉详情";

                if(null != this.chartTemp)
                {
                    RefreshPltHeatTemp();
                    this.chartTemp.Show();
                }
            }
            else
            {
                this.checkBoxChart.Text = "温度曲线";

                if(null != this.chartTemp)
                {
                    this.chartTemp.Hide();
                }
            }
        }

        #endregion

        #region // 干燥炉操作

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if(null != this.dryingOvenRun)
            {
                if(!this.dryingOvenRun.DryOvenConnect(true))
                {
                    ShowMsgBox.ShowDialog(this.dryingOvenRun.RunName + "连接失败", MessageType.MsgMessage);
                }
                this.labelOvenState.Text = this.dryingOvenRun.DryOvenIsConnect() ? "已连接" : "已断开";
                if(this.dryingOvenRun.DryOvenIsConnect())
                {
                    if(null != this.timerUpdata)
                    {
                        this.timerUpdata.Start();
                    }
                }
            }
        }

        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            this.BeginInvoke(new Action(() =>
            {
                this.dryingOvenRun.DryOvenConnect(false);
                this.labelOvenState.Text = this.dryingOvenRun.DryOvenIsConnect() ? "已连接" : "已断开";
            }));
        }
        
        private void buttonOpenDoor_Click(object sender, EventArgs e)
        {
            if (DryOvenCmd.End == this.dryOvenCmd)
            {
                this.dryOvenCmdState = OvenStatus.DoorOpen;
                this.dryOvenCmd = DryOvenCmd.DoorOpenClose;
            }
            else
            {
                ShowMsgBox.ShowDialog("干燥炉动作中，请稍后再操作...", MessageType.MsgMessage);
            }
        }

        private void buttonCloseDoor_Click(object sender, EventArgs e)
        {
            if(DryOvenCmd.End == this.dryOvenCmd)
            {
                this.dryOvenCmdState = OvenStatus.DoorClose;
                this.dryOvenCmd = DryOvenCmd.DoorOpenClose;
            }
            else
            {
                ShowMsgBox.ShowDialog("干燥炉动作中，请稍后再操作...", MessageType.MsgMessage);
            }
        }

        private void buttonOpenVac_Click(object sender, EventArgs e)
        {
            if(DryOvenCmd.End == this.dryOvenCmd)
            {
                this.dryOvenCmdState = OvenStatus.VacOpen;
                this.dryOvenCmd = DryOvenCmd.VacOpenClose;
            }
            else
            {
                ShowMsgBox.ShowDialog("干燥炉动作中，请稍后再操作...", MessageType.MsgMessage);
            }
        }

        private void buttonCloseVac_Click(object sender, EventArgs e)
        {
            if(DryOvenCmd.End == this.dryOvenCmd)
            {
                this.dryOvenCmdState = OvenStatus.VacClose;
                this.dryOvenCmd = DryOvenCmd.VacOpenClose;
            }
            else
            {
                ShowMsgBox.ShowDialog("干燥炉动作中，请稍后再操作...", MessageType.MsgMessage);
            }
        }

        private void buttonOpenBlowAir_Click(object sender, EventArgs e)
        {
            if(DryOvenCmd.End == this.dryOvenCmd)
            {
                this.dryOvenCmdState = OvenStatus.BlowOpen;
                this.dryOvenCmd = DryOvenCmd.BlowOpenClose;
            }
            else
            {
                ShowMsgBox.ShowDialog("干燥炉动作中，请稍后再操作...", MessageType.MsgMessage);
            }
        }

        private void buttonCloseBlowAir_Click(object sender, EventArgs e)
        {
            if(DryOvenCmd.End == this.dryOvenCmd)
            {
                this.dryOvenCmdState = OvenStatus.BlowClose;
                this.dryOvenCmd = DryOvenCmd.BlowOpenClose;
            }
            else
            {
                ShowMsgBox.ShowDialog("干燥炉动作中，请稍后再操作...", MessageType.MsgMessage);
            }
        }

        private void buttonWorkStart_Click(object sender, EventArgs e)
        {
            if(DryOvenCmd.End == this.dryOvenCmd)
            {
                this.dryOvenCmdState = OvenStatus.WorkStart;
                this.dryOvenCmd = DryOvenCmd.WorkStartStop;
            }
            else
            {
                ShowMsgBox.ShowDialog("干燥炉动作中，请稍后再操作...", MessageType.MsgMessage);
            }
        }

        private void buttonWorkStop_Click(object sender, EventArgs e)
        {
            if(DryOvenCmd.End == this.dryOvenCmd)
            {
                this.dryOvenCmdState = OvenStatus.WorkStop;
                this.dryOvenCmd = DryOvenCmd.WorkStartStop;
            }
            else
            {
                ShowMsgBox.ShowDialog("干燥炉动作中，请稍后再操作...", MessageType.MsgMessage);
            }
        }

        private void buttonSetParameter_Click(object sender, EventArgs e)
        {
            if(DryOvenCmd.End == this.dryOvenCmd)
            {
                this.dryOvenCmd = DryOvenCmd.SetParameter;
            }
            else
            {
                ShowMsgBox.ShowDialog("干燥炉动作中，请稍后再操作...", MessageType.MsgMessage);
            }
        }

        private void buttonErrorReset_Click(object sender, EventArgs e)
        {
            if (DryOvenCmd.End == this.dryOvenCmd)
            {
                this.dryOvenCmdState = OvenStatus.FaultResetOn;
                this.dryOvenCmd = DryOvenCmd.FaultReset;
            }
            else
            {
                ShowMsgBox.ShowDialog("干燥炉动作中，请稍后再操作...", MessageType.MsgMessage);
            }
        }

        private void RunWhileThread()
        {
            while(this.runWhileRun)
            {
                try
                {
                    if(DryOvenCmd.End != this.dryOvenCmd)
                    {
                        RunWhile();
                        this.dryOvenCmd = DryOvenCmd.End;
                    }
                }
                catch(System.Exception ex)
                {
                    string msg = string.Format("RunWhileThread error: {0}", ex.Message);
                    Def.WriteLog("DryingOvenPage", msg);
                    ShowMsgBox.ShowDialog("DryingOvenPage" + msg, MessageType.MsgAlarm);
                    this.dryOvenCmd = DryOvenCmd.End;
                }
                Thread.Sleep(1);
            }
        }

        private void RunWhile()
        {
            if(null == this.dryingOvenRun)
            {
                ShowMsgBox.ShowDialog("找不到干燥炉，请先选择干燥炉！", MessageType.MsgMessage);
                return;
            }
            if(!this.dryingOvenRun.DryOvenIsConnect() && !Def.IsNoHardware())
            {
                ShowMsgBox.ShowDialog("干燥炉未连接，请先连接干燥炉！", MessageType.MsgMessage);
                return;
            }
            int cavityIdx = this.cavityIndex;
            if(cavityIdx < 0)
            {
                ShowMsgBox.ShowDialog("请先选择干燥炉炉层！", MessageType.MsgMessage);
                return;
            }

            #region // 腔体操作

            CavityData cavityData = new CavityData();
            bool result = false;
            string msg = "";
            switch(this.dryOvenCmd)
            {
                case DryOvenCmd.SetParameter:               // 工艺参数（写）
                    {
                        result = this.dryingOvenRun.DryOvenSetParameter(cavityIdx, cavityData, true, OptMode.Manual);
                        msg = "设置参数" + (result ? "成功" : "失败");
                        break;
                    }
                case DryOvenCmd.WorkStartStop:              // 加热启动/停止（写入）
                    {
                        cavityData.workState = (uint)this.dryOvenCmdState;
                        result = this.dryingOvenRun.DryOvenWorkStart(cavityIdx, cavityData, true, OptMode.Manual);
                        msg = ((OvenStatus.WorkStart == this.dryOvenCmdState) ? "启动加热" : "停止加热") + (result ? "成功" : "失败");
                        break;
                    }
                case DryOvenCmd.DoorOpenClose:              // 炉门打开/关闭（写入）
                    {
                        if(!MachineCtrl.GetInstance().ClientIsConnect())
                        {
                            msg = "模组服务器未连接，无法获取安全门状态，不能操作干燥炉";
                            break;
                        }
                        if(MachineCtrl.GetInstance().SafeDoorState)
                        {
                            msg = "安全门打开时不能操作干燥炉炉门";
                            break;
                        }
                        cavityData.doorState = (short)this.dryOvenCmdState;
                        result = this.dryingOvenRun.DryOvenOpenDoor(cavityIdx, cavityData, true, OptMode.Manual);
                        msg = ((OvenStatus.DoorOpen == this.dryOvenCmdState) ? "打开炉门" : "关闭炉门") + (result ? "成功" : "失败");
                        break;
                    }
                case DryOvenCmd.VacOpenClose:               // 真空打开/关闭（写入）
                    {
                        cavityData.vacValveState = (short)this.dryOvenCmdState;
                        result = this.dryingOvenRun.DryOvenVacuum(cavityIdx, cavityData, true, OptMode.Manual);
                        msg = ((OvenStatus.VacOpen == this.dryOvenCmdState) ? "打开真空" : "关闭真空") + (result ? "成功" : "失败");
                        break;
                    }
                case DryOvenCmd.BlowOpenClose:              // 破真空打开/关闭（写入）
                    {
                        cavityData.blowValveState = (short)this.dryOvenCmdState;
                        result = this.dryingOvenRun.DryOvenBlowAir(cavityIdx, cavityData, true, OptMode.Manual);
                        msg = ((OvenStatus.BlowOpen == this.dryOvenCmdState) ? "打开破真空" : "关闭破真空") + (result ? "成功" : "失败");
                        break;
                    }
                case DryOvenCmd.PressureOpenClose:          // 保压打开/关闭（写入）
                    {
                        cavityData.pressureState = (uint)this.dryOvenCmdState;
                        result = this.dryingOvenRun.DryOvenPressure(cavityIdx, cavityData, true, OptMode.Manual);
                        msg = ((OvenStatus.PressureOpen == this.dryOvenCmdState) ? "打开保压" : "关闭保压") + (result ? "成功" : "失败");
                        break;
                    }
                case DryOvenCmd.FaultReset:                 // 故障复位（写入）
                    {
                        cavityData.faultReset = (short)this.dryOvenCmdState;
                        result = this.dryingOvenRun.DryOvenFaultReset(cavityIdx, cavityData, true, OptMode.Manual);
                        //Thread.Sleep(500);
                        //cavityData.faultReset = (short)OvenStatus.FaultResetOff;
                        //result = this.dryingOvenRun.DryOvenFaultReset(cavityIdx, cavityData, true, OptMode.Manual);
                        //msg = ("故障复位") + (result ? "成功" : "失败");
                        msg = result ? "解除维修成功" : "非维修状态，无法解除维修";
                        break;
                    }
                default:
                    msg = "操作指令错误";
                    break;
            }
            #endregion

            ShowMsgBox.ShowDialog(msg, (result ? MessageType.MsgMessage : MessageType.MsgWarning));
        }

        private void UpdataDryingOvenData()
        {
            this.labelOvenState.Text = this.dryingOvenRun.DryOvenIsConnect() ? "已连接" : "已断开";
            
            CavityData data = this.dryingOvenRun.RCavity(this.cavityIndex);

            if (this.checkBoxChart.Checked)
            {
                return;
            }

            int idx = 0;
            string info = "";

            #region // 参数表

            idx = 0;
            this.listViewParameter.Items[idx++].SubItems[1].Text = data.parameter.SetTempValue.ToString("#0.00");
            this.listViewParameter.Items[idx++].SubItems[1].Text = data.parameter.TempUpperlimit.ToString("#0.00");
            this.listViewParameter.Items[idx++].SubItems[1].Text = data.parameter.TempLowerlimit.ToString("#0.00");
            this.listViewParameter.Items[idx++].SubItems[1].Text = data.parameter.PreheatTime.ToString();
            this.listViewParameter.Items[idx++].SubItems[1].Text = data.parameter.VacHeatTime.ToString();
            this.listViewParameter.Items[idx++].SubItems[1].Text = data.parameter.OpenDoorBlowTime.ToString();
            this.listViewParameter.Items[idx++].SubItems[1].Text = data.parameter.OpenDoorVacPressure.ToString();
            this.listViewParameter.Items[idx++].SubItems[1].Text = data.parameter.AStateVacTime.ToString();
            this.listViewParameter.Items[idx++].SubItems[1].Text = data.parameter.AStateVacPressure.ToString();
            this.listViewParameter.Items[idx++].SubItems[1].Text = data.parameter.BStateVacTime.ToString();
            this.listViewParameter.Items[idx++].SubItems[1].Text = data.parameter.BStateVacPressure.ToString();
            this.listViewParameter.Items[idx++].SubItems[1].Text = data.parameter.BStateBlowAirTime.ToString();
            this.listViewParameter.Items[idx++].SubItems[1].Text = data.parameter.BStateBlowAirPressure.ToString();
            this.listViewParameter.Items[idx++].SubItems[1].Text = data.parameter.BStateBlowAirKeepTime.ToString();
            this.listViewParameter.Items[idx++].SubItems[1].Text = data.parameter.BreathTimeInterval.ToString();
            this.listViewParameter.Items[idx++].SubItems[1].Text = data.parameter.BreathCycleTimes.ToString();
            this.listViewParameter.Items[idx++].SubItems[1].Text = data.parameter.HeatPlate.ToString();
            this.listViewParameter.Items[idx++].SubItems[1].Text = data.parameter.MaxNGHeatPlate.ToString();
            this.listViewParameter.Items[idx++].SubItems[1].Text = data.parameter.HeatPreVacTime.ToString();
            this.listViewParameter.Items[idx++].SubItems[1].Text = data.parameter.HeatPreBlow.ToString();
            #endregion

            #region // 温度表
            // 温度值：夹具 - 控温/巡检 - 发热板通道
            for(int col = 0; col < (int)OvenRowCol.MaxCol; col++)
            {
                int pltIdx = this.dryingOvenRun.DryOvenGroupColIdx(col);
                for(int i = 0; i < (int)OvenInfoCount.HeatPanelCount; i++)
                {
                    for(int j = 0; j < 2; j++)  // 控温/巡检
                    {
                        this.listViewTemp.Items[i].SubItems[col*2 + j + 1].Text = data.tempValue[pltIdx, j, i].ToString("#0.00");
                    }
                }
            }
            #endregion

            #region // 报警表

            string[,] alarmMsg, alarmVlue;
            this.dryingOvenRun.CheckTempAlarm(data, out alarmMsg, out alarmVlue);
            for(int i = 0; i < (int)OvenInfoCount.HeatPanelCount; i++)
            {
                for(int col = 0; col < (int)OvenRowCol.MaxCol; col++)
                {
                    this.listViewAlarm.Items[i].SubItems[col * 2 + 1].Text = alarmVlue[col, i];
                    this.listViewAlarm.Items[i].SubItems[col * 2 + 2].Text = alarmMsg[col, i];
                }
            }
            #endregion
            
            #region // 状态表：最后更新，因为需要全部炉层数据

            for(int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            {
                data = this.dryingOvenRun.RCavity(i);
                idx = 0;
                info = "";
                switch((OvenStatus)data.doorState)
                {
                    case OvenStatus.DoorClose:
                        info = "关闭";
                        break;
                    case OvenStatus.DoorOpen:
                        info = "打开";
                        break;
                    case OvenStatus.DoorAction:
                        info = "动作中";
                        break;
                    default:
                        info = "未知";
                        break;
                }
                this.listViewState.Items[idx++].SubItems[i + 1].Text = info;
                switch((OvenStatus)data.workState)
                {
                    case OvenStatus.WorkStop:
                        info = "停止";
                        break;
                    case OvenStatus.WorkStart:
                        info = "工作中";
                        break;
                    case OvenStatus.WorkOutage:
                        info = "异常断电";
                        break;
                    default:
                        info = "未知";
                        break;
                }
                this.listViewState.Items[idx++].SubItems[i + 1].Text = info;
                this.listViewState.Items[idx++].SubItems[i + 1].Text = data.workTime.ToString();
                this.listViewState.Items[idx++].SubItems[i + 1].Text = data.vacPressure.ToString();
                switch((OvenStatus)data.pressureState)
                {
                    case OvenStatus.PressureClose:
                        info = "关闭";
                        break;
                    case OvenStatus.PressureOpen:
                        info = "打开";
                        break;
                    default:
                        info = "未知";
                        break;
                }
                this.listViewState.Items[idx++].SubItems[i + 1].Text = info;
                switch((OvenStatus)data.vacValveState)
                {
                    case OvenStatus.VacClose:
                        info = "关闭";
                        break;
                    case OvenStatus.VacOpen:
                        info = "打开";
                        break;
                    default:
                        info = "未知";
                        break;
                }
                this.listViewState.Items[idx++].SubItems[i + 1].Text = info;
                switch((OvenStatus)data.blowValveState)
                {
                    case OvenStatus.BlowClose:
                        info = "关闭";
                        break;
                    case OvenStatus.BlowOpen:
                        info = "打开";
                        break;
                    default:
                        info = "未知";
                        break;
                }
                this.listViewState.Items[idx++].SubItems[i + 1].Text = info;
                switch((OvenStatus)data.safetyCurtain)
                {
                    case OvenStatus.SafetyCurtainOff:
                        info = "OFF";
                        break;
                    case OvenStatus.SafetyCurtainOn:
                        info = "ON";
                        break;
                    default:
                        info = "未知";
                        break;
                }
                this.listViewState.Items[idx++].SubItems[i + 1].Text = info;
                this.listViewState.Items[idx++].SubItems[i + 1].Text = (data.doorAlarm ? "报警" : "");
                this.listViewState.Items[idx++].SubItems[i + 1].Text = (data.vacAlarm ? "报警" : "");
                this.listViewState.Items[idx++].SubItems[i + 1].Text = (data.vacuometerAlarm ? "报警" : "");
                this.listViewState.Items[idx++].SubItems[i + 1].Text = (data.blowAlarm ? "报警" : "");
                for(int col = 0; col < (int)OvenRowCol.MaxCol; col++)
                {
                    int pltIdx = this.dryingOvenRun.DryOvenGroupColIdx(col);
                    this.listViewState.Items[idx++].SubItems[i + 1].Text = (data.controlAlarm[pltIdx] ? "报警" : "");
                }
                for(int col = 0; col < (int)OvenRowCol.MaxCol; col++)
                {
                    int pltIdx = this.dryingOvenRun.DryOvenGroupColIdx(col);
                    this.listViewState.Items[idx++].SubItems[i + 1].Text = (data.pallletAlarm[pltIdx] ? "报警" : "");
                }
            }
            #endregion

            #region // 系统信息

            switch((OvenStatus)this.dryingOvenRun.DryOvenRemoteState())
            {
                case OvenStatus.RemoteClose:
                    info = "本地控制";
                    break;
                case OvenStatus.RemoteOpen:
                    info = "远程模式";
                    break;
                default:
                    info = "未知";
                    break;
            }
            this.labelRemote.Text = info + $"[{this.dryingOvenRun.DryOvenMcDoorState()}]";
            #endregion

        }

        private void RefreshPltHeatTemp()
        {
            try
            {
                this.Invoke(new Action(() =>
                {
                    for(int col = 0; col < (int)OvenRowCol.MaxCol; col++)
                    {
                        int pltIdx = this.cavityIndex * (int)OvenRowCol.MaxCol + col;
                        for(int i = 0; i < 2 * (int)OvenInfoCount.HeatPanelCount; i++)
                        {
                            this.chartTemp.Series[pltIdx + i].Points.DataBindXY(this.dryingOvenRun.PltHeatTime[pltIdx][i], this.dryingOvenRun.PltHeatTemp[pltIdx][i]);
                        }
                    }
                }));
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("DryingOvenPage", "RefreshPltHeatTemp() error: " + ex.Message);
            }
        }

        #endregion

    }
}
