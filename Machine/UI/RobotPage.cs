using HelperLibrary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    public partial class RobotPage : FormEx
    {
        #region // 字段

        private Dictionary<OnloadRobotStation, RobotFormula> onLoadRobotInfo;              // 上料机器人工位信息
        private Dictionary<TransferRobotStation, RobotFormula> transferRobotInfo;          // 调度机器人工位信息
        private bool runWhileRun;               // 指示线程运行
        private Task runWhileTask;              // 任务运行线程
        private bool robotRunning;              // 机器人运行中
        private RobotIndexID robotIndex;        // 机器人索引
        private RobotActionInfo robotAction;    // 机器人需运行指令

        /// <summary>
        /// 按钮使能状态
        /// </summary>
        private enum BtnEnable
        {
            OnloadConnectDisabled,
            OnloadConnectEnabled,
            OnloadAllDisabled,
            OnloadAllEnabled,
            OnlyHomeEnable,
            OnlyHomeUpEnable,

            TransferConnectDisabled,
            TransferConnectEnabled,
            TransferAllDisabled,
            TransferAllEnabled,
            OnlyPickinEnable,
            OnlyPickoutEnable,
            OnlyPlaceinEnable,
            OnlyPlaceoutEnable,
        }

        #endregion

        public RobotPage()
        {
            InitializeComponent();

            CreateRobotList();
        }

        #region // 加载及销毁窗体

        private void RobotPage_Load(object sender, EventArgs e)
        {
            try
            {
                if(this.comboBoxOnloadRobot.Items.Count <= 0)
                {
                    SetUIEnable(BtnEnable.OnloadConnectDisabled);
                    SetUIEnable(BtnEnable.OnloadAllDisabled);
                }
                if(this.comboBoxTransferRobot.Items.Count <= 0)
                {
                    SetUIEnable(BtnEnable.TransferConnectDisabled);
                    SetUIEnable(BtnEnable.TransferAllDisabled);
                }
                this.robotAction = new RobotActionInfo();
                if ((this.comboBoxOnloadRobot.Items.Count > 0) || (this.comboBoxTransferRobot.Items.Count > 0))
                {
                    // 创建任务线程
                    this.runWhileRun = true;
                    this.runWhileTask = new Task(RunWhileThread, TaskCreationOptions.LongRunning);
                    this.runWhileTask.Start();
                    Def.WriteLog("RobotPage", $"RunWhileThread() id.{this.runWhileTask.Id} Start running.", LogType.Success);
                }
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("RobotPage", string.Format("RunWhileThread() create fail: {0}", ex.ToString()));
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void DisposeForm()
        {
            try
            {
                // 等待操作线程结束
                this.runWhileRun = false;
                if(null != this.runWhileTask)
                {
                    this.runWhileTask.Wait();
                    Def.WriteLog("RobotPage", $"RunWhileThread() id.{this.runWhileTask.Id} end.");
                }
            }
            catch(System.Exception ex)
            {
                Def.WriteLog("RobotPage", string.Format("RunWhileThread() Release fail: {0}", ex.ToString()));
            }
        }

        /// <summary>
        /// UI界面可见性发生改变
        /// </summary>
        /// <param name="show">是否在前台显示</param>
        public override void UIVisibleChanged(bool show)
        {
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
        /// <param name="mc">j设备运行状态</param>
        /// <param name="level">用户等级</param>
        public override void UpdataUIEnable(SystemControlLibrary.MCState mc, SystemControlLibrary.UserLevelType level)
        {
            try
            {
                #region // 判断按钮禁用/启用

                if((MCState.MCInitializing == mc) || (MCState.MCRunning == mc))
                {
                    SetUIEnable(BtnEnable.OnloadConnectDisabled);
                    SetUIEnable(BtnEnable.OnloadAllDisabled);
                    SetUIEnable(BtnEnable.TransferConnectDisabled);
                    SetUIEnable(BtnEnable.TransferAllDisabled);
                }
                else
                {
                    if(this.comboBoxOnloadRobot.Items.Count > 0)
                    {
                        RunProcessOnloadRobot run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
                        if(null != run)
                        {
                            RobotActionInfo rbtInfo = run.GetRobotActionInfo(false);
                            if(RobotOrder.DOWN == rbtInfo.order)
                            {
                                SetUIEnable(BtnEnable.OnlyHomeUpEnable);
                            }
                            else if(RobotOrder.INVALID == rbtInfo.order)
                            {
                                SetUIEnable(BtnEnable.OnlyHomeEnable);
                            }
                            else if(RobotOrder.DOWN != rbtInfo.order)
                            {
                                SetUIEnable(BtnEnable.OnloadAllEnabled);
                            }
                            SetUIEnable(BtnEnable.OnloadConnectEnabled);
                        }
                    }
                    if(this.comboBoxTransferRobot.Items.Count > 0)
                    {
                        RunProcessRobotTransfer run = MachineCtrl.GetInstance().GetModule(RunID.Transfer) as RunProcessRobotTransfer;
                        if(null != run)
                        {
                            RobotActionInfo rbtInfo = run.GetRobotActionInfo(false);
                            if(RobotOrder.PICKIN == rbtInfo.order)
                            {
                                SetUIEnable(BtnEnable.OnlyPickoutEnable);
                            }
                            else if(RobotOrder.PLACEIN == rbtInfo.order)
                            {
                                SetUIEnable(BtnEnable.OnlyPlaceoutEnable);
                            }
                            // 无夹具只能取
                            else if(run.PalletKeepFlat(0, false, false))
                            {
                                SetUIEnable(BtnEnable.OnlyPickinEnable);
                            }
                            // 有夹具只能放
                            else if(run.PalletKeepFlat(0, true, false))
                            {
                                SetUIEnable(BtnEnable.OnlyPlaceinEnable);
                            }
                            else if(!run.PalletKeepFlat(0, false, false) && !run.PalletKeepFlat(0, true, false))
                            {
                                //SetBtnEnable(BtnEnable.TransferAllDisabled);
                                SetUIEnable(BtnEnable.OnlyPlaceinEnable);
                                string msg = $"夹具放平感应错误，维护界面-{run.RunName}的两个放平感应器应该同时ON或同时OFF";
                                msg += "\r\n处理方式：请停机检查放平感应器，确保两个放平感应器同时ON或同时OFF";
                                ShowMsgBox.Show(msg, MessageType.MsgAlarm);
                            }
                            else
                            {
                                SetUIEnable(BtnEnable.TransferAllEnabled);
                            }
                            SetUIEnable(BtnEnable.TransferConnectEnabled);
                        }
                    }
                }
                #endregion
            }
            catch(System.Exception ex)
            {
                Def.WriteLog("RobotPage.UpdataUIEnable()", ex.Message, LogType.Error);
            }
        }

        /// <summary>
        /// 创建机器人列表
        /// </summary>
        private void CreateRobotList()
        {
            #region // 设置表格

            DataGridView[] dgv = new DataGridView[] { this.dataGridViewOnloadStation, this.dataGridViewTransferStation };
            for(int i = 0; i < dgv.Length; i++)
            {
                dgv[i].ReadOnly = true;        // 只读不可编辑
                dgv[i].MultiSelect = false;    // 禁止多选，只可单选
                dgv[i].AutoGenerateColumns = false;        // 禁止创建列
                dgv[i].AllowUserToAddRows = false;         // 禁止添加行
                dgv[i].AllowUserToDeleteRows = false;      // 禁止删除行
                dgv[i].AllowUserToResizeRows = false;      // 禁止行改变大小
                dgv[i].RowHeadersVisible = false;          // 行表头不可见
                dgv[i].ColumnHeadersVisible = false;       // 列表头不可见
                dgv[i].Dock = DockStyle.Fill;              // 填充
                dgv[i].EditMode = DataGridViewEditMode.EditProgrammatically;           // 软件编辑模式
                dgv[i].AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;     // 自动改变列宽
                dgv[i].SelectionMode = DataGridViewSelectionMode.FullRowSelect;        // 整行选中
                dgv[i].RowsDefaultCellStyle.BackColor = Color.WhiteSmoke;              // 偶数行颜色
                dgv[i].AlternatingRowsDefaultCellStyle.BackColor = Color.GhostWhite;   // 奇数行颜色
                dgv[i].Columns.Add("station", "工位列表");
                foreach(DataGridViewColumn item in dgv[i].Columns)
                {
                    item.SortMode = DataGridViewColumnSortMode.NotSortable;     // 禁止列排序
                }
            }
            #endregion

            #region // 限定控件最大范围

            TableLayoutPanel[] tlp = new TableLayoutPanel[] { this.tableLayoutPanelOnload, this.tableLayoutPanelTransfer };
            for(int i = 0; i < tlp.Length; i++)
            {
                foreach(Control item in tlp[i].Controls)
                {
                    Button btn = item as Button;
                    if(null != btn)
                    {
                        btn.MaximumSize = new Size(180, 50);
                    }
                }
            }
            ComboBox[] cbo = new ComboBox[] { this.comboBoxOnloadRobotRow, this.comboBoxOnloadRobotCol, this.comboBoxTransferRobotRow, this.comboBoxTransferRobotCol };
            for(int i = 0; i < cbo.Length; i++)
            {
                cbo[i].MaximumSize = new Size(180, 0);
            }
            #endregion

            RunProcessOnloadRobot onloadRobot = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
            if (null != onloadRobot)
            {
                this.comboBoxOnloadRobot.Items.Add(onloadRobot.RunName);
            }
            if (this.comboBoxOnloadRobot.Items.Count > 0)
            {
                this.comboBoxOnloadRobot.SelectedIndex = 0;
            }

            RunProcessRobotTransfer transferRobot = MachineCtrl.GetInstance().GetModule(RunID.Transfer) as RunProcessRobotTransfer;
            if (null != transferRobot)
            {
                this.comboBoxTransferRobot.Items.Add(transferRobot.RunName);
            }
            if (this.comboBoxTransferRobot.Items.Count > 0)
            {
                this.comboBoxTransferRobot.SelectedIndex = 0;
            }
        }

        #endregion

        #region // 界面使能

        /// <summary>
        /// 设置界面控件使能
        /// </summary>
        /// <param name="btnEN"></param>
        private void SetUIEnable(BtnEnable btnEN)
        {
            this.Invoke(new Action(() =>
            {
                switch(btnEN)
                {
                    case BtnEnable.OnloadConnectDisabled:
                    case BtnEnable.OnloadConnectEnabled:
                        {
                            bool en = (btnEN == BtnEnable.OnloadConnectEnabled);

                            this.buttonOnloadRobotConnect.Enabled = en;
                            this.buttonOnloadRobotDisconnect.Enabled = en;
                            break;
                        }
                    case BtnEnable.OnloadAllDisabled:
                    case BtnEnable.OnloadAllEnabled:
                        {
                            bool en = (btnEN == BtnEnable.OnloadAllEnabled);

                            this.buttonOnloadRobotHome.Enabled = en;
                            this.buttonOnloadRobotMove.Enabled = en;
                            this.buttonOnloadRobotDown.Enabled = en;
                            this.buttonOnloadRobotUp.Enabled = en;
                            break;
                        }
                    case BtnEnable.OnlyHomeEnable:
                        {
                            bool en = false;

                            this.buttonOnloadRobotHome.Enabled = true;
                            this.buttonOnloadRobotMove.Enabled = en;
                            this.buttonOnloadRobotDown.Enabled = en;
                            this.buttonOnloadRobotUp.Enabled = en;
                            break;
                        }
                    case BtnEnable.OnlyHomeUpEnable:
                        {
                            bool en = false;

                            this.buttonOnloadRobotHome.Enabled = true;
                            this.buttonOnloadRobotMove.Enabled = en;
                            this.buttonOnloadRobotDown.Enabled = en;
                            this.buttonOnloadRobotUp.Enabled = true;
                            break;
                        }

                    case BtnEnable.TransferConnectDisabled:
                    case BtnEnable.TransferConnectEnabled:
                        {
                            bool en = (btnEN == BtnEnable.TransferConnectEnabled);

                            this.buttonTransferRobotConnect.Enabled = en;
                            this.buttonTransferRobotDisconnect.Enabled = en;
                            break;
                        }
                    case BtnEnable.TransferAllDisabled:
                    case BtnEnable.TransferAllEnabled:
                        {
                            bool en = (btnEN == BtnEnable.TransferAllEnabled);

                            this.buttonTransferRobotMove.Enabled = en;
                            this.buttonTransferRobotPickIn.Enabled = en;
                            this.buttonTransferRobotPickOut.Enabled = en;
                            this.buttonTransferRobotPlaceIn.Enabled = en;
                            this.buttonTransferRobotPlaceOut.Enabled = en;
                            break;
                        }
                    case BtnEnable.OnlyPickinEnable:
                        {
                            bool en = false;

                            this.buttonTransferRobotMove.Enabled = true;
                            this.buttonTransferRobotPickIn.Enabled = true;
                            this.buttonTransferRobotPickOut.Enabled = en;
                            this.buttonTransferRobotPlaceIn.Enabled = en;
                            this.buttonTransferRobotPlaceOut.Enabled = en;
                            break;
                        }
                    case BtnEnable.OnlyPickoutEnable:
                        {
                            bool en = false;

                            this.buttonTransferRobotMove.Enabled = en;
                            this.buttonTransferRobotPickIn.Enabled = en;
                            this.buttonTransferRobotPickOut.Enabled = true;
                            this.buttonTransferRobotPlaceIn.Enabled = en;
                            this.buttonTransferRobotPlaceOut.Enabled = en;
                            break;
                        }
                    case BtnEnable.OnlyPlaceinEnable:
                        {
                            bool en = false;

                            this.buttonTransferRobotMove.Enabled = true;
                            this.buttonTransferRobotPickIn.Enabled = en;
                            this.buttonTransferRobotPickOut.Enabled = en;
                            this.buttonTransferRobotPlaceIn.Enabled = true;
                            this.buttonTransferRobotPlaceOut.Enabled = en;
                            break;
                        }
                    case BtnEnable.OnlyPlaceoutEnable:
                        {
                            bool en = false;

                            this.buttonTransferRobotMove.Enabled = en;
                            this.buttonTransferRobotPickIn.Enabled = en;
                            this.buttonTransferRobotPickOut.Enabled = en;
                            this.buttonTransferRobotPlaceIn.Enabled = en;
                            this.buttonTransferRobotPlaceOut.Enabled = true;
                            break;
                        }
                    default:
                        break;
                }
            }));
        }

        #endregion

        #region // 上料机器人操作

        private void comboBoxOnloadRobot_SelectedIndexChanged(object sender, EventArgs e)
        {
            RunProcessOnloadRobot onloadRobot = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
            if (null != onloadRobot)
            {
                if (null == this.onLoadRobotInfo)
                {
                    this.onLoadRobotInfo = new Dictionary<OnloadRobotStation, RobotFormula>();
                    int rbtID = (int)onloadRobot.RobotID;
                    if (rbtID <= (int)RobotIndexID.Invalid || rbtID >= (int)RobotIndexID.End)
                    {
                        return;
                    }
                    int formulaID = Def.GetProductFormula();
                    string rbtName = RobotDef.RobotIDName[rbtID];
                    List<RobotFormula> listStation = new List<RobotFormula>();
                    MachineCtrl.GetInstance().dbRecord.GetRobotStationList(Def.GetProductFormula(), rbtID, ref listStation);
                    foreach (var item in listStation)
                    {
                        this.onLoadRobotInfo.Add((OnloadRobotStation)item.stationID, item);
                    }
                    for(OnloadRobotStation i = OnloadRobotStation.HomeStatioin; i < OnloadRobotStation.StationEnd; i++)
                    {
                        int index = this.dataGridViewOnloadStation.Rows.Add();
                        this.dataGridViewOnloadStation.Rows[index].Height = 35;        // 行高度
                        this.dataGridViewOnloadStation.Rows[index].Cells[0].Value = this.onLoadRobotInfo[i].stationName;
                    }
                }
                labelOnloadRobotIP.Text = onloadRobot.RobotIPInfo();
                this.labelOnloadConnectState.Text = onloadRobot.RobotIsConnect() ? "已连接" : "已断开";
            }
        }

        private void dataGridViewOnloadRobotStation_SelectionChanged(object sender, EventArgs e)
        {
            this.comboBoxOnloadRobotRow.Items.Clear();
            this.comboBoxOnloadRobotCol.Items.Clear();

            int station = this.dataGridViewOnloadStation.CurrentRow.Index + 1;
            if(this.onLoadRobotInfo.ContainsKey((OnloadRobotStation)station))
            {
                int row = this.onLoadRobotInfo[(OnloadRobotStation)station].maxRow;
                int col = this.onLoadRobotInfo[(OnloadRobotStation)station].maxCol;

                for(int i = 1; i < row + 1; i++)
                {
                    this.comboBoxOnloadRobotRow.Items.Add(i);
                }
                for(int i = 1; i < col + 1; i++)
                {
                    this.comboBoxOnloadRobotCol.Items.Add(i);
                }
                this.comboBoxOnloadRobotRow.SelectedIndex = 0;
                this.comboBoxOnloadRobotCol.SelectedIndex = 0;
            }
        }

        // 移动指令
        private void OnloadRobotMove(RobotOrder order)
        {
            if(!this.robotRunning)
            {
                int station = this.dataGridViewOnloadStation.CurrentRow.Index + 1;
                int row = this.comboBoxOnloadRobotRow.SelectedIndex;
                int col = this.comboBoxOnloadRobotCol.SelectedIndex;

                this.robotIndex = RobotIndexID.Onload;
                this.robotAction.SetData(station, row, col, order, this.onLoadRobotInfo[(OnloadRobotStation)station].stationName);

                this.robotRunning = true;
            }
            else
            {
                ShowMsgBox.ShowDialog("机器人动作中，请稍后再操作...\r\n\r\n提示：若机器人无动作请查看是否有动作弹窗未确认", MessageType.MsgMessage);
            }
        }

        // 连接
        private void buttonOnloadRobotConnect_Click(object sender, EventArgs e)
        {
            RunProcessOnloadRobot onloadRobot = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
            if (null != onloadRobot)
            {
                if (onloadRobot.RobotConnect(true))
                {
                    this.labelOnloadConnectState.Text = "已连接";
                    ShowMsgBox.ShowDialog("上料机器人连接成功！！！", MessageType.MsgMessage);
                }
                else
                {                    
                    ShowMsgBox.ShowDialog("上料机器人连接失败！！！", MessageType.MsgMessage);
                }
            }
        }
        // 断开
        private void buttonOnloadRobotDisconnect_Click(object sender, EventArgs e)
        {
            RunProcessOnloadRobot onloadRobot = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
            if (null != onloadRobot)
            {
                if (onloadRobot.RobotConnect(false))
                {
                    this.labelOnloadConnectState.Text = "已断开";
                    ShowMsgBox.ShowDialog("上料机器人断开连接成功", MessageType.MsgMessage);
                }
            }
        }
        // 原点
        private void buttonOnloadRobotHome_Click(object sender, EventArgs e)
        {
            OnloadRobotMove(RobotOrder.HOME);
        }
        // 移动
        private void buttonOnloadRobotMove_Click(object sender, EventArgs e)
        {
            OnloadRobotMove(RobotOrder.MOVE);
        }
        // 下降
        private void buttonOnloadRobotDown_Click(object sender, EventArgs e)
        {
            OnloadRobotMove(RobotOrder.DOWN);
        }
        // 上升
        private void buttonOnloadRobotUp_Click(object sender, EventArgs e)
        {
            OnloadRobotMove(RobotOrder.UP);
        }
        #endregion

        #region // 调度机器人操作

        private void comboBoxTransferRobot_SelectedIndexChanged(object sender, EventArgs e)
        {
            RunProcessRobotTransfer transferRobot = MachineCtrl.GetInstance().GetModule(RunID.Transfer) as RunProcessRobotTransfer;
            if (null != transferRobot)
            {
                if (null == this.transferRobotInfo)
                {
                    this.transferRobotInfo = new Dictionary<TransferRobotStation, RobotFormula>();
                    int rbtID = (int)transferRobot.RobotID;
                    if (rbtID <= (int)RobotIndexID.Invalid || rbtID >= (int)RobotIndexID.End)
                    {
                        return;
                    }
                    int formulaID = Def.GetProductFormula();
                    string rbtName = RobotDef.RobotIDName[rbtID];
                    List<RobotFormula> listStation = new List<RobotFormula>();
                    MachineCtrl.GetInstance().dbRecord.GetRobotStationList(Def.GetProductFormula(), rbtID, ref listStation);
                    foreach (var item in listStation)
                    {
                        this.transferRobotInfo.Add((TransferRobotStation)item.stationID, item);
                    }
                    for(TransferRobotStation i = TransferRobotStation.OnloadStation; i < TransferRobotStation.StationEnd; i++)
                    {
                        int index = this.dataGridViewTransferStation.Rows.Add();
                        this.dataGridViewTransferStation.Rows[index].Height = 35;        // 行高度
                        this.dataGridViewTransferStation.Rows[index].Cells[0].Value = this.transferRobotInfo[i].stationName;
                    }
                }                
            }
            labelTransferRobotIP.Text = transferRobot.RobotIPInfo();
            this.labelTransferRobotConnectState.Text = transferRobot.RobotIsConnect() ? "已连接" : "已断开";
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            this.comboBoxTransferRobotRow.Items.Clear();
            this.comboBoxTransferRobotCol.Items.Clear();

            int station = this.dataGridViewTransferStation.CurrentRow.Index + 1;
            if(this.transferRobotInfo.ContainsKey((TransferRobotStation)station))
            {
                int row = this.transferRobotInfo[(TransferRobotStation)station].maxRow;
                int col = this.transferRobotInfo[(TransferRobotStation)station].maxCol;

                for(int i = 1; i < row + 1; i++)
                {
                    this.comboBoxTransferRobotRow.Items.Add(i);
                }

                for(int i = 1; i < col + 1; i++)
                {
                    this.comboBoxTransferRobotCol.Items.Add(i);
                }

                this.comboBoxTransferRobotRow.SelectedIndex = 0;
                this.comboBoxTransferRobotCol.SelectedIndex = 0;
            }
        }

        // 移动指令
        private void TransferRobotMove(RobotOrder order)
        {
            if(!this.robotRunning)
            {
                int station = this.dataGridViewTransferStation.CurrentRow.Index + 1;
                int row = this.comboBoxTransferRobotRow.SelectedIndex;
                int col = this.comboBoxTransferRobotCol.SelectedIndex;

                this.robotIndex = RobotIndexID.Transfer;
                this.robotAction.SetData(station, row, col, order, this.transferRobotInfo[(TransferRobotStation)station].stationName);

                this.robotRunning = true;
            }
            else
            {
                ShowMsgBox.ShowDialog("机器人动作中，请稍后再操作...\r\n\r\n提示：若机器人无动作请查看是否有动作弹窗未确认", MessageType.MsgMessage);
            }
        }

        // 连接
        private void buttonTransferRobotConnect_Click(object sender, EventArgs e)
        {
            RunProcessRobotTransfer transferRobot = MachineCtrl.GetInstance().GetModule(RunID.Transfer) as RunProcessRobotTransfer;
            if (null != transferRobot)
            {
                if (transferRobot.RobotConnect(true))
                {
                    this.labelTransferRobotConnectState.Text = "已连接";
                    ShowMsgBox.ShowDialog("调度机器人连接成功", MessageType.MsgMessage);
                }
                else
                {                   
                    ShowMsgBox.ShowDialog("调度机器人连接失败", MessageType.MsgMessage);
                }
            }
        }
        // 断开
        private void buttonTransferRobotDisconnect_Click(object sender, EventArgs e)
        {
            RunProcessRobotTransfer transferRobot = MachineCtrl.GetInstance().GetModule(RunID.Transfer) as RunProcessRobotTransfer;
            if (null != transferRobot)
            {
                if (transferRobot.RobotConnect(false))
                {
                    this.labelTransferRobotConnectState.Text = "已断开";
                    ShowMsgBox.ShowDialog("调度机器人断开连接成功", MessageType.MsgMessage);
                }
            }
        }
        // 移动
        private void buttonTransferRobotMove_Click(object sender, EventArgs e)
        {
            TransferRobotMove(RobotOrder.MOVE);
        }
        // 取进
        private void buttonTransferRobotPickIn_Click(object sender, EventArgs e)
        {
            TransferRobotMove(RobotOrder.PICKIN);
        }
        // 取出
        private void buttonTransferRobotPickOut_Click(object sender, EventArgs e)
        {
            TransferRobotMove(RobotOrder.PICKOUT);
        }
        // 放进
        private void buttonTransferRobotPlaceIn_Click(object sender, EventArgs e)
        {
            TransferRobotMove(RobotOrder.PLACEIN);
        }
        // 放出
        private void buttonTransferRobotPlaceOut_Click(object sender, EventArgs e)
        {
            TransferRobotMove(RobotOrder.PLACEOUT);
        }
        #endregion

        #region // 后台线程

        private void RunWhileThread()
        {
            while(this.runWhileRun)
            {
                try
                {
                    if (this.robotRunning)
                    {
                        RunWhile();
                        this.robotRunning = false;

                        UpdataUIEnable(MCState.MCStopRun, MachineCtrl.GetInstance().dbRecord.UserLevel());
                    }
                }
                catch (System.Exception ex)
                {
                    this.robotRunning = false;
                    ShowMsgBox.ShowDialog(ex.Message, MessageType.MsgAlarm);
                }
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// 后台循环执行函数
        /// </summary>
        private void RunWhile()
        {
            if(!MachineCtrl.GetInstance().ClientIsConnect())
            {
                ShowMsgBox.ShowDialog("模组服务器未连接，无法获取安全门状态，不能操作机器人", MessageType.MsgAlarm);
                return;
            }
            if(MachineCtrl.GetInstance().SafeDoorState)
            {
                ShowMsgBox.ShowDialog("安全门打开时不能操作机器人", MessageType.MsgAlarm);
                return;
            }
            MCState mcState = MachineCtrl.GetInstance().RunsCtrl.GetMCState();
            if((MCState.MCInitComplete != mcState) && (MCState.MCStopRun != mcState))
            {
                string msg = $"上位机软件非【初始化完成】或【运行停止】状态，不能操作{RobotDef.RobotIDName[(int)this.robotIndex]}";
                msg += $"\r\n处理方法：请先按启动按钮将上位机软件初始化！";
                ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                return;
            }
            switch(this.robotIndex)
            {
                #region // 上料机器人
                case RobotIndexID.Onload:
                    {
                        RunProcessOnloadRobot run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
                        if(null != run)
                        {
                            Action<bool> uiDelegate = delegate (bool isConnect)
                            {
                                this.labelOnloadConnectState.Text = isConnect ? "已连接" : "已断开";
                            };
                            this.Invoke(uiDelegate, run.RobotIsConnect());
                            if(!run.RobotIsConnect())
                            {
                                ShowMsgBox.ShowDialog("请先连接机器人再操作", MessageType.MsgMessage);
                                break;
                            }
                            if((this.robotAction.station <= (int)OnloadRobotStation.InvalidStatioin)
                                || (this.robotAction.row < 0) || (this.robotAction.col < 0))
                            {
                                ShowMsgBox.ShowDialog("请选择正确工位行列，再操作机器人", MessageType.MsgWarning);
                                break;
                            }
                            if(run.RobotManulAvoid((OnloadRobotStation)this.robotAction.station, this.robotAction.row, this.robotAction.col, this.robotAction.order))
                            {
                                if(this.robotAction.order == RobotOrder.HOME)
                                {
                                    string msg = RobotDef.RobotIDName[(int)robotIndex] + " 是否执行<回零>动作";
                                    if(DialogResult.Yes == ShowMsgBox.ShowDialog(msg, MessageType.MsgQuestion))
                                    {
                                        if(MachineCtrl.GetInstance().SafeDoorState)
                                        {
                                            ShowMsgBox.ShowDialog("安全门打开时不能操作机器人", MessageType.MsgAlarm);
                                            return;
                                        }
                                        bool result = run.RobotHome(OptMode.Manual);
                                        ShowMsgBox.ShowDialog(("上料机器人<回零>" + (result ? "成功！" : "失败！")), MessageType.MsgMessage);
                                    }
                                    break;
                                }
                                else
                                {
                                    string msg = string.Format("{0} 是否执行<{1}-{2}-{3}-{4}>动作"
                                        , RobotDef.RobotIDName[(int)robotIndex]
                                        , robotAction.stationName, robotAction.row + 1, robotAction.col + 1
                                        , RobotDef.RobotOrderName[(int)robotAction.order]);
                                    if(DialogResult.Yes == ShowMsgBox.ShowDialog(msg, MessageType.MsgQuestion))
                                    {
                                        if(MachineCtrl.GetInstance().SafeDoorState)
                                        {
                                            ShowMsgBox.ShowDialog("安全门打开时不能操作机器人", MessageType.MsgAlarm);
                                            return;
                                        }
                                        int[] robotCmd = new int[(int)RobotCmdFormat.End];
                                        int speed = (RobotOrder.DOWN == robotAction.order) ? (run.RobotLowSpeed / 2) : run.RobotLowSpeed;
                                        if(run.GetRobotCmd((OnloadRobotStation)robotAction.station, robotAction.row, robotAction.col, speed, robotAction.order, ref robotCmd))
                                        {
                                            bool result = run.RobotMove(robotCmd, true, OptMode.Manual);

                                            msg = string.Format("{0}<{1}-{2}-{3}-{4}>{5}"
                                                , RobotDef.RobotIDName[(int)robotIndex]
                                                , robotAction.stationName, robotAction.row + 1, robotAction.col + 1
                                                , RobotDef.RobotOrderName[(int)robotAction.order]
                                                , (result ? "成功！" : "失败！"));
                                            ShowMsgBox.ShowDialog(msg, MessageType.MsgMessage);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    }
                #endregion

                #region // 调度机器人
                case RobotIndexID.Transfer:
                    {
                        RunProcessRobotTransfer run = MachineCtrl.GetInstance().GetModule(RunID.Transfer) as RunProcessRobotTransfer;
                        if(null != run)
                        {
                            Action<bool> uiDelegate = delegate (bool isConnect)
                            {
                                this.labelTransferRobotConnectState.Text = isConnect ? "已连接" : "已断开";
                            };
                            this.Invoke(uiDelegate, run.RobotIsConnect());
                            if(!run.RobotIsConnect())
                            {
                                ShowMsgBox.ShowDialog("请先连接机器人再操作", MessageType.MsgMessage);
                                break;
                            }
                            if((this.robotAction.station <= (int)TransferRobotStation.InvalidStatioin)
                                || (this.robotAction.row < 0) || (this.robotAction.col < 0))
                            {
                                ShowMsgBox.ShowDialog("请选择正确工位行列，再操作机器人", MessageType.MsgWarning);
                                break;
                            }
                            if(run.RobotManulAvoid((TransferRobotStation)this.robotAction.station, this.robotAction.row, this.robotAction.col, this.robotAction.order))
                            {
                                string msg = string.Format("{0} 是否执行<{1}-{2}-{3}-{4}>动作"
                                    , RobotDef.RobotIDName[(int)robotIndex]
                                    , robotAction.stationName, robotAction.row + 1, robotAction.col + 1
                                    , RobotDef.RobotOrderName[(int)robotAction.order]);
                                if(DialogResult.Yes == ShowMsgBox.ShowDialog(msg, MessageType.MsgQuestion))
                                {
                                    if(MachineCtrl.GetInstance().SafeDoorState)
                                    {
                                        ShowMsgBox.ShowDialog("安全门打开时不能操作机器人", MessageType.MsgAlarm);
                                        return;
                                    }
                                    int[] robotCmd = new int[(int)RobotCmdFormat.End];
                                    int speed = (RobotOrder.MOVE == robotAction.order) ? run.RobotLowSpeed : run.RobotLowSpeed / 2;
                                    if(run.GetRobotCmd((TransferRobotStation)robotAction.station, robotAction.row, robotAction.col, speed, robotAction.order, ref robotCmd))
                                    {
                                        bool result = run.RobotMove(robotCmd, true, OptMode.Manual);

                                        msg = string.Format("{0}<{1}-{2}-{3}-{4}>{5}"
                                            , RobotDef.RobotIDName[(int)robotIndex]
                                            , robotAction.stationName, robotAction.row + 1, robotAction.col + 1
                                            , RobotDef.RobotOrderName[(int)robotAction.order]
                                            , (result ? "成功！" : "失败！"));
                                        ShowMsgBox.ShowDialog(msg, MessageType.MsgMessage);
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    }
                    #endregion
            }
            this.robotAction.Release();
        }

        #endregion

    }
}
