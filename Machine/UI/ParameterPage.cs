using HelperLibrary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    public partial class ParameterPage : FormEx
    {
        public ParameterPage()
        {
            InitializeComponent();

            CreateListViewModule();

            // 属性页设置
            this.propertyGridParameter.Font = new Font(this.propertyGridParameter.Font.FontFamily, 11);
            this.propertyGridParameter.PropertySort = PropertySort.Categorized;
            this.propertyGridParameter.ToolbarVisible = false;
        }

        #region // 字段

        /// <summary>
        /// 原选择行索引
        /// </summary>
        int oldRowIndex;
        /// <summary>
        /// 参数修改Log记录
        /// </summary>
        LogFile logFile;

        #endregion

        /// <summary>
        /// 加载界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ParameterPage_Load(object sender, EventArgs e)
        {
            this.oldRowIndex = -1;
            // 参数修改记录文件
            this.logFile = new LogFile();
            this.logFile.SetFileInfo(Def.GetAbsPathName("Log\\ParameterLog\\"), 2, 15);
        }

        /// <summary>
        /// 关闭窗口前销毁自定义非托管资源
        /// </summary>
        public override void DisposeForm()
        {
        }
        
        /// <summary>
        /// 界面隐藏时停止更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ParameterPage_VisibleChanged(object sender, EventArgs e)
        {
            if(this.Visible)
            {
                // MES在线状态
                this.BeginInvoke(new Action(() =>
                {
                    DataGridViewCheckBoxTextCell cBoxCell = this.dataGridViewModule.Rows[0].Cells[2] as DataGridViewCheckBoxTextCell;
                    if(null != cBoxCell)
                    {
                        bool oldReadOnly = cBoxCell.ReadOnly;
                        cBoxCell.ReadOnly = false;
                        cBoxCell.Checked = MachineCtrl.GetInstance().UpdataMes;
                        cBoxCell.Text = cBoxCell.Checked ? "在线生产" : "离线生产";
                        cBoxCell.ForeColor = cBoxCell.Checked ? Color.Black : Color.Red;
                        cBoxCell.ReadOnly = oldReadOnly;
                    }
                }));
                dataGridViewModule_SelectionChanged(sender, e);
                UpdataUIEnable(MachineCtrl.GetInstance().RunsCtrl.GetMCState(), MachineCtrl.GetInstance().dbRecord.UserLevel());
            }
            else
            {
                UpdataUIEnable(MCState.MCRunning, UserLevelType.USER_LOGOUT);
            }
        }

        /// <summary>
        /// 创建模组列表
        /// </summary>
        private void CreateListViewModule()
        {
            // 设置表格
            //this.dataGridViewModule.ReadOnly = true;        // 只读不可编辑：需要更改单元格项，设为可编辑
            this.dataGridViewModule.MultiSelect = false;    // 禁止多选，只可单选
            this.dataGridViewModule.AutoGenerateColumns = false;        // 禁止创建列
            this.dataGridViewModule.AllowUserToAddRows = false;         // 禁止添加行
            this.dataGridViewModule.AllowUserToDeleteRows = false;      // 禁止删除行
            this.dataGridViewModule.AllowUserToResizeRows = false;      // 禁止行改变大小
            this.dataGridViewModule.RowHeadersVisible = false;          // 行表头不可见
            this.dataGridViewModule.Dock = DockStyle.Fill;              // 填充
            this.dataGridViewModule.EditMode = DataGridViewEditMode.EditProgrammatically;           // 软件编辑模式
            this.dataGridViewModule.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;     // 自动改变列宽
            this.dataGridViewModule.SelectionMode = DataGridViewSelectionMode.FullRowSelect;        // 整行选中
            this.dataGridViewModule.RowsDefaultCellStyle.BackColor = Color.WhiteSmoke;              // 偶数行颜色
            this.dataGridViewModule.AlternatingRowsDefaultCellStyle.BackColor = Color.GhostWhite;   // 奇数行颜色
            // 表头
            this.dataGridViewModule.ColumnHeadersDefaultCellStyle.Font = new Font(this.dataGridViewModule.ColumnHeadersDefaultCellStyle.Font.FontFamily, 11, FontStyle.Bold);
            this.dataGridViewModule.ColumnHeadersHeight = 35;
            this.dataGridViewModule.Columns.Add("module", "模组名称");
            DataGridViewCheckBoxTextColumn cBox = new DataGridViewCheckBoxTextColumn();
            cBox.HeaderText = "使能";
            this.dataGridViewModule.Columns.Add(cBox);
            cBox = new DataGridViewCheckBoxTextColumn();
            cBox.HeaderText = "空运行";
            this.dataGridViewModule.Columns.Add(cBox);
            foreach(DataGridViewColumn item in this.dataGridViewModule.Columns)
            {
                item.SortMode = DataGridViewColumnSortMode.NotSortable;     // 禁止列排序
            }

            #region // 模组参数列表
            // 系统
            int index = this.dataGridViewModule.Rows.Add();
            this.dataGridViewModule.Rows[index].Height = 35;        // 行高度
            this.dataGridViewModule.Rows[index].Cells[0].Value = "系统参数";
            DataGridViewCheckBoxTextCell cBoxCell = this.dataGridViewModule.Rows[index].Cells[1] as DataGridViewCheckBoxTextCell;
            if(null != cBoxCell)
            {
                cBoxCell.Checked = MachineCtrl.GetInstance().DataRecover;
                cBoxCell.Text = "数据恢复";
                cBoxCell.ForeColor = cBoxCell.Checked ? Color.Black : Color.Red;
            }
            cBoxCell = this.dataGridViewModule.Rows[index].Cells[2] as DataGridViewCheckBoxTextCell;
            if(null != cBoxCell)
            {
                cBoxCell.Checked = MachineCtrl.GetInstance().UpdataMes;
                cBoxCell.Text = cBoxCell.Checked ? "在线生产" : "离线生产";
                cBoxCell.ForeColor = cBoxCell.Checked ? Color.Black : Color.Red;
            }
            // 模组
            for(int rowIdx = 0; rowIdx < MachineCtrl.GetInstance().ListRuns.Count; rowIdx++)
            {
                RunProcess run = MachineCtrl.GetInstance().ListRuns[rowIdx];
                index = this.dataGridViewModule.Rows.Add();
                this.dataGridViewModule.Rows[index].Height = 35;        // 行高度
                this.dataGridViewModule.Rows[index].Cells[0].Value = run.RunName;
                cBoxCell = this.dataGridViewModule.Rows[index].Cells[1] as DataGridViewCheckBoxTextCell;
                if(null != cBoxCell)
                {
                    cBoxCell.Checked = run.IsModuleEnable();
                    cBoxCell.Text = cBoxCell.Checked ? "使能" : "禁用";
                    cBoxCell.ForeColor = cBoxCell.Checked ? Color.Black : Color.Red;
                }
                cBoxCell = this.dataGridViewModule.Rows[index].Cells[2] as DataGridViewCheckBoxTextCell;
                if(null != cBoxCell)
                {
                    cBoxCell.Checked = run.DryRun;
                    cBoxCell.Text = cBoxCell.Checked ? "空运行" : "正常";
                    cBoxCell.ForeColor = cBoxCell.Checked ? Color.Red : Color.Black;
                }
            }
            #endregion
        }

        /// <summary>
        /// 当选择的模组改变时，更改参数列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridViewModule_SelectionChanged(object sender, EventArgs e)
        {
            if(!this.Visible)
            {
                return;
            }
            PropertyManage pm = null;
            int rowIdx = this.dataGridViewModule.CurrentCell.RowIndex;
            // 属性页数据添加 // 首模组为系统参数
            if(0 == rowIdx)
            {
                pm = MachineCtrl.GetInstance().GetParameterList();
            }
            else
            {
                pm = MachineCtrl.GetInstance().ListRuns[rowIdx - 1].GetParameterList();
            }
            if(null != pm)
            {
                this.propertyGridParameter.SelectedObject = pm;
            }
            UpdataUIEnable(MachineCtrl.GetInstance().RunsCtrl.GetMCState(), MachineCtrl.GetInstance().dbRecord.UserLevel());
        }

        /// <summary>
        /// 属性值已改变，保存
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        private void propertyGridParameter_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            // 保存数据
            int rowIdx = this.dataGridViewModule.CurrentCell.RowIndex;
            int moduleID = -1;
            string moduleName = this.dataGridViewModule.CurrentRow.Cells[0].FormattedValue.ToString();
            try
            {
                DataBaseRecord.UserFormula user = new DataBaseRecord.UserFormula();
                MachineCtrl.GetInstance().dbRecord.GetCurUser(ref user);
                string msgLoc = string.Format("{0} Changed", user.userName);
                string msg = string.Format("{0}[{1}]由{2}改为{3}", e.ChangedItem.Label, e.ChangedItem.PropertyDescriptor.Name, e.OldValue, e.ChangedItem.Value);
                bool result = false;

                // 首模组为系统参数
                if(0 == rowIdx)
                {
                    if (MachineCtrl.GetInstance().CheckParameter(e.ChangedItem.PropertyDescriptor.Name, e.ChangedItem.Value))
                    {
                        MachineCtrl.GetInstance().WriteParameter(e.ChangedItem.PropertyDescriptor.Name, e.ChangedItem.Value.ToString());
                        MachineCtrl.GetInstance().ReadParameter();

                        moduleID = -1;
                        msg = string.Format("{0}.{1}", MachineCtrl.GetInstance().MachineName, msg);
                        result = true;
                    }
                }
                else
                {
                    RunProcess run = MachineCtrl.GetInstance().ListRuns[rowIdx - 1];
                    if((null != run) && run.CheckParameter(e.ChangedItem.PropertyDescriptor.Name, e.ChangedItem.Value))
                    {
                        run.WriteParameter(run.RunModule, e.ChangedItem.PropertyDescriptor.Name, e.ChangedItem.Value.ToString());
                        run.ReadParameter();

                        moduleID = run.GetRunID();
                        msg = string.Format("{0}.{1}", run.RunName, msg);
                        result = true;
                    }
                }
                if (result)
                {
                    this.logFile.WriteLog(DateTime.Now, msgLoc, msg, LogType.Success);
                    DataBaseLog.AddParameterLog(new DataBaseLog.ParameterLogFormula(Def.GetProductFormula(), moduleID
                        , MachineCtrl.GetInstance().OperaterID, DateTime.Now.ToString(Def.DateFormal), moduleName
                        , $"{e.ChangedItem.PropertyDescriptor.DisplayName}[{e.ChangedItem.PropertyDescriptor.Name}]"
                        , e.OldValue.ToString(), e.ChangedItem.Value.ToString()));
                }
                else
                {
                    // 修改失败恢复原值
                    e.ChangedItem.PropertyDescriptor.SetValue(e.ChangedItem.PropertyDescriptor, e.OldValue);
                }
            }
            catch (System.Exception ex)
            {
                string msg = string.Format("{0}[{1}]由{2}改为{3}异常"
                    , e.ChangedItem.Label, e.ChangedItem.PropertyDescriptor.Name, e.OldValue, e.ChangedItem.Value);
                this.logFile.WriteLog(DateTime.Now, "ParameterValueChangedException", msg, LogType.Warning);
                if(0 != rowIdx)
                {
                    RunProcess run = MachineCtrl.GetInstance().ListRuns[rowIdx - 1];
                    if(null != run)
                    {
                        moduleID = run.GetRunID();
                    }
                }
                DataBaseLog.AddParameterLog(new DataBaseLog.ParameterLogFormula(Def.GetProductFormula(), moduleID
                    , MachineCtrl.GetInstance().OperaterID, DateTime.Now.ToString(Def.DateFormal), moduleName
                        , $"{e.ChangedItem.PropertyDescriptor.DisplayName}[{e.ChangedItem.PropertyDescriptor.Name}]"
                        , e.OldValue.ToString(), e.ChangedItem.Value.ToString()));

                // 修改失败恢复原值
                e.ChangedItem.PropertyDescriptor.SetValue(e.ChangedItem.PropertyDescriptor, e.OldValue);
                ShowMsgBox.ShowDialog((e.ChangedItem.Label + " 参数修改失败：" + ex), MessageType.MsgAlarm);
            }
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
                SetUIEnable(mc, level);
            }
            catch(System.Exception ex)
            {
                Def.WriteLog("ParameterPage .UpdataUIEnable()", ex.Message, LogType.Error);
            }
            base.UpdataUIEnable(mc, level);
        }

        /// <summary>
        /// 设置界面控件使能：模组使能及空运行参数是否可更改
        /// </summary>
        /// <param name="enable"></param>
        private void SetUIEnable(MCState mcState, UserLevelType userLevel)
        {
            #region // 模组使能及空运行

            ParameterLevel paramLevel = ParameterLevel.PL_LEVEL_END;
            if(MCState.MCIdle == MachineCtrl.GetInstance().RunsCtrl.GetMCState())
            {
                if(userLevel < UserLevelType.USER_LOGOUT)
                {
                    paramLevel = ParameterLevel.PL_IDLE_ADMIN + (int)userLevel;
                }
            }
            Action<DataGridView> dgvDelegate = delegate (DataGridView dgv)
            {
                if(null != dgv)
                {
                    bool readOnly = (ParameterLevel.PL_IDLE_ADMIN != paramLevel) && (ParameterLevel.PL_IDLE_MAIN != paramLevel);
                    for(int rowIdx = 0; rowIdx < dgv.Rows.Count; rowIdx++)
                    {
                        for(int colIdx = 0; colIdx < dgv.Columns.Count; colIdx++)
                        {
                            DataGridViewCheckBoxTextCell cBoxCell = dgv.Rows[rowIdx].Cells[colIdx] as DataGridViewCheckBoxTextCell;
                            if(null != cBoxCell)
                            {
                                cBoxCell.ReadOnly = readOnly;
                            }
                        }
                    }
                    // MES在线/离线生产，停止状态下即可更改
                    if ((dgv.Rows.Count > 0) && (dgv.Rows[0].Cells[2] is DataGridViewCheckBoxTextCell))
                    {
                        dgv.Rows[0].Cells[2].ReadOnly = (MCState.MCIdle != mcState) && (MCState.MCStopInit != mcState) && (MCState.MCInitComplete != mcState)
                                                        && (MCState.MCStopRun != mcState) || (userLevel > UserLevelType.USER_OPERATOR);
                    }
                    dgv.Refresh();
                }
            };
            this.Invoke(dgvDelegate, this.dataGridViewModule);

            #endregion

            #region // 模组参数

            PropertyManage pm = this.propertyGridParameter.SelectedObject as PropertyManage;
            if(null != pm)
            {
                bool state = true;
                bool oldState = true;
                bool isChange = false;

                foreach(Property item in pm)
                {
                    oldState = item.ReadOnly;

                    switch((ParameterLevel)item.Permissions)
                    {
                        case ParameterLevel.PL_IDLE_ADMIN:
                        case ParameterLevel.PL_IDLE_MAIN:
                        case ParameterLevel.PL_IDLE_OPER:
                            {
                                state = (MCState.MCIdle == mcState) && (item.Permissions - (int)ParameterLevel.PL_IDLE_ADMIN >= (int)userLevel);
                                item.ReadOnly = !state;
                                break;
                            }
                        case ParameterLevel.PL_STOP_ADMIN:
                        case ParameterLevel.PL_STOP_MAIN:
                        case ParameterLevel.PL_STOP_OPER:
                            {
                                state = (MCState.MCIdle == mcState) || (MCState.MCStopInit == mcState)
                                        || (MCState.MCInitComplete == mcState) || (MCState.MCStopRun == mcState);
                                item.ReadOnly = !(state && (item.Permissions - (int)ParameterLevel.PL_STOP_ADMIN >= (int)userLevel));
                                break;
                            }
                        case ParameterLevel.PL_ALL_ADMIN:
                        case ParameterLevel.PL_ALL_MAIN:
                        case ParameterLevel.PL_ALL_OPER:
                            {
                                item.ReadOnly = !(item.Permissions - (int)ParameterLevel.PL_ALL_ADMIN >= (int)userLevel);
                                break;
                            }
                        default:
                            break;
                    }
                    if(oldState != item.ReadOnly)
                    {
                        isChange = true;
                    }
                }
                if (isChange)
                {
                    // 使用委托同步更新参数列表UI
                    this.Invoke(new Action(delegate () { this.propertyGridParameter.Refresh(); }));
                }
            }
            #endregion

        }
        
        /// <summary>
        /// 使能及空运行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridViewModule_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            int rowIdx = this.dataGridViewModule.CurrentCell.RowIndex;
            // 模组第一次改变时仅更改模组索引，其余参数不变
            if(rowIdx != oldRowIndex)
            {
                oldRowIndex = rowIdx;
                return;
            }
            
            int colIdx = this.dataGridViewModule.CurrentCell.ColumnIndex;
            DataGridViewCheckBoxTextCell cBoxCell = this.dataGridViewModule.Rows[rowIdx].Cells[colIdx] as DataGridViewCheckBoxTextCell;
            if(null != cBoxCell)
            {
                if (cBoxCell.ReadOnly)
                {
                    return;
                }
                #region // 系统参数
                if(0 == rowIdx)
                {
                    if(1 == colIdx)
                    {
                        cBoxCell.Checked = !cBoxCell.Checked;
                        cBoxCell.Text = "数据恢复";
                        cBoxCell.ForeColor = cBoxCell.Checked ? Color.Black : Color.Red;
                        if(cBoxCell.Checked)
                        {
                            MachineCtrl.GetInstance().DataRecover = cBoxCell.Checked;
                            //MachineCtrl.GetInstance().SaveSettingParameter();
                            return;
                        }
                        else
                        {
                            if(DialogResult.Yes == ShowMsgBox.ShowDialog("是否取消数据恢复？", MessageType.MsgQuestion))
                            {
                                if(DialogResult.Yes == ShowMsgBox.ShowDialog("取消数据恢复会清除所有运行数据！\r\n请确认是否清除所有运行数据？", MessageType.MsgQuestion))
                                {
                                    foreach(var item in MachineCtrl.GetInstance().ListRuns)
                                    {
                                        item.DeleteRunData();
                                    }
                                    MachineCtrl.GetInstance().DataRecover = cBoxCell.Checked;
                                    //MachineCtrl.GetInstance().SaveSettingParameter();
                                    return;
                                }
                            }
                        }
                        cBoxCell.Checked = !cBoxCell.Checked;
                        cBoxCell.Text = "数据恢复";
                        cBoxCell.ForeColor = cBoxCell.Checked ? Color.Black : Color.Red;
                    }
                    else if(2 == colIdx)
                    {
                        if (MachineCtrl.GetInstance().MesModifyCheck())
                        {
                            cBoxCell.Checked = !cBoxCell.Checked;
                            cBoxCell.Text = cBoxCell.Checked ? "MES在线" : "MES离线";
                            cBoxCell.ForeColor = cBoxCell.Checked ? Color.Black : Color.Red;
                            MachineCtrl.GetInstance().UpdataMes = cBoxCell.Checked;
                            //MachineCtrl.GetInstance().SaveSettingParameter();
                        }
                    }
                }
                #endregion

                #region // 模组参数
                else if(1 == colIdx)
                {
                    rowIdx--;   // 首模组为系统参数
                    cBoxCell.Checked = !cBoxCell.Checked;
                    cBoxCell.Text = cBoxCell.Checked ? "使能" : "禁用";
                    cBoxCell.ForeColor = cBoxCell.Checked ? Color.Black : Color.Red;
                    MachineCtrl.GetInstance().ListRuns[rowIdx].Enable(cBoxCell.Checked);
                    MachineCtrl.GetInstance().ListRuns[rowIdx].SaveConfig();
                }
                else if(2 == colIdx)
                {
                    rowIdx--;   // 首模组为系统参数
                    cBoxCell.Checked = !cBoxCell.Checked;
                    cBoxCell.Text = cBoxCell.Checked ? "空运行" : "正常";
                    cBoxCell.ForeColor = cBoxCell.Checked ? Color.Red : Color.Black;
                    MachineCtrl.GetInstance().ListRuns[rowIdx].DryRun = cBoxCell.Checked;
                    MachineCtrl.GetInstance().ListRuns[rowIdx].SaveConfig();
                }
                #endregion
            }
        }

    }
}
