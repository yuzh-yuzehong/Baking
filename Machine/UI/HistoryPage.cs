using HelperLibrary;
using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static Machine.DataBaseLog;
using static SystemControlLibrary.DataBaseRecord;
using SystemControlLibrary;

namespace Machine
{
    public partial class HistoryPage : FormEx
    {
        public HistoryPage()
        {
            InitializeComponent();

            // 创建视图表
            CreateListView();
        }

        #region // 字段

        readonly int PageMaxItem = 50;  // 每页50条数据

        ToolTip toolTip;                // ToolTip
        DataTable dataTable;            // 已查询的记录集
        int selectedPage;               // 已查询的记录集选择的页

        #endregion

        #region // 枚举

        private enum QueryType
        {
            Alarm,
            DryingOvenLog,
            ParameterLog,
            RobotLog,
            MotorLog,
        }
        #endregion

        private void HistoryPage_Load(object sender, EventArgs e)
        {
            // 设置tooTip
            this.toolTip = new ToolTip();
            this.toolTip.SetToolTip(this.textBoxFindID, "查询的具体ID，空则为全部");
            this.toolTip.SetToolTip(this.buttonQuery, "查询当前条件下的所有记录");
            this.toolTip.SetToolTip(this.buttonExport, "导出当前记录到文件");
            this.toolTip.SetToolTip(this.buttonDelete, "删除查询的所有记录");
            this.toolTip.SetToolTip(this.buttonFirst, "显示第一页");
            this.toolTip.SetToolTip(this.buttonPrevious, "显示上一页");
            this.toolTip.SetToolTip(this.buttonNext, "显示下一页");
            this.toolTip.SetToolTip(this.buttonLast, "显示最后一页");

            this.dataTable = new DataTable();
            this.selectedPage = 0;

        }

        /// <summary>
        /// 创建视图
        /// </summary>
        private void CreateListView()
        {
            // 设置时间格式
            this.dateTimePickerStart.CustomFormat = Def.DateFormal;
            this.dateTimePickerEnd.CustomFormat = Def.DateFormal;
            // 设置表格
            this.dataGridViewData.ReadOnly = true;        // 只读不可编辑
            this.dataGridViewData.MultiSelect = false;    // 禁止多选，只可单选
            this.dataGridViewData.AutoGenerateColumns = false;        // 禁止创建列
            this.dataGridViewData.AllowUserToAddRows = false;         // 禁止添加行
            this.dataGridViewData.AllowUserToDeleteRows = false;      // 禁止删除行
            this.dataGridViewData.AllowUserToResizeRows = false;      // 禁止行改变大小
            this.dataGridViewData.RowHeadersVisible = false;          // 行表头不可见
            this.dataGridViewData.Dock = DockStyle.Fill;              // 填充
            this.dataGridViewData.EditMode = DataGridViewEditMode.EditProgrammatically;           // 软件编辑模式
            this.dataGridViewData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;     // 自动改变列宽
            this.dataGridViewData.SelectionMode = DataGridViewSelectionMode.FullRowSelect;        // 整行选中
            this.dataGridViewData.RowsDefaultCellStyle.BackColor = Color.WhiteSmoke;              // 偶数行颜色
            this.dataGridViewData.AlternatingRowsDefaultCellStyle.BackColor = Color.GhostWhite;   // 奇数行颜色
            // 表头
            this.dataGridViewData.ColumnHeadersDefaultCellStyle.Font = new Font(this.dataGridViewData.ColumnHeadersDefaultCellStyle.Font.FontFamily, 11, FontStyle.Bold);
            this.dataGridViewData.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            this.dataGridViewData.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            this.dataGridViewData.ColumnHeadersHeight = 35;

            // Type
            this.comboBoxType.Items.Add("报警信息");
            this.comboBoxType.Items.Add("干燥炉操作记录");
            this.comboBoxType.Items.Add("参数修改记录");
            this.comboBoxType.Items.Add("机器人操作记录");
            this.comboBoxType.Items.Add("电机操作记录");

            if (this.comboBoxType.Items.Count > 0)
            {
                this.comboBoxType.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 查询类型改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBoxType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(null != this.dataTable)
            {
                this.dataTable.Clear();
            }
            this.comboBoxModule.Items.Clear();
            this.comboBoxModule.Items.Add("All");
            QueryType queryType = (QueryType)this.comboBoxType.SelectedIndex;
            switch(queryType)
            {
                case QueryType.Alarm:
                    foreach(RunProcess item in MachineCtrl.GetInstance().ListRuns)
                    {
                        this.comboBoxModule.Items.Add(item.RunName);
                    }
                    break;
                case QueryType.DryingOvenLog:
                    for(int i = 0; i < (int)OvenInfoCount.OvenCount; i++)
                    {
                        this.comboBoxModule.Items.Add($"干燥炉{i + 1}");
                    }
                    break;
                case QueryType.ParameterLog:
                    this.comboBoxModule.Items.Add(MachineCtrl.GetInstance().MachineName);
                    foreach(RunProcess item in MachineCtrl.GetInstance().ListRuns)
                    {
                        this.comboBoxModule.Items.Add(item.RunName);
                    }
                    break;
                case QueryType.RobotLog:
                    foreach(var item in RobotDef.RobotIDName)
                    {
                        this.comboBoxModule.Items.Add(item);
                    }
                    break;
                case QueryType.MotorLog:
                    var motors = DeviceManager.GetMotorManager().LstMotors;
                    foreach(var item in motors)
                    {
                        this.comboBoxModule.Items.Add(item.Name);
                    }
                    break;
                default:
                    break;
            }
            if(this.comboBoxModule.Items.Count > 0)
            {
                this.comboBoxModule.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonQuery_Click(object sender, EventArgs e)
        {
            int almID = -1;
            if (!string.IsNullOrEmpty(this.textBoxFindID.Text))
            {
                if(!int.TryParse(this.textBoxFindID.Text, out almID))
                    almID = -1;
            }
            string startTime = this.dateTimePickerStart.Value.ToString(Def.DateFormal);
            string endTime = this.dateTimePickerEnd.Value.ToString(Def.DateFormal);
            int modIdx = this.comboBoxModule.SelectedIndex;
            QueryType queryType = (QueryType)this.comboBoxType.SelectedIndex;
            switch(queryType)
            {
                case QueryType.Alarm:
                    {
                        modIdx = (0 == modIdx) ? -1 : MachineCtrl.GetInstance().ListRuns[modIdx - 1].GetRunID();    // 索引转为RunID
                        if(!MachineCtrl.GetInstance().dbRecord.GetAlarmList(Def.GetProductFormula(), modIdx, almID, startTime, endTime, ref this.dataTable))
                        {
                            return;
                        }
                        if(this.dataTable.Rows.Count > 0)
                        {
                            this.dataTable = this.dataTable.Rows.Cast<DataRow>().OrderBy(r => r[AlarmTable[(int)RecordColumn.ALM_ALARM_TIME]]).CopyToDataTable();
                        }
                        this.dataTable.Columns[(int)RecordColumn.ALM_FORMULA_ID].ColumnName = "产品ID";
                        this.dataTable.Columns[(int)RecordColumn.ALM_INFO_ID].ColumnName = "报警ID";
                        this.dataTable.Columns[(int)RecordColumn.ALM_INFO_MSG].ColumnName = "报警信息";
                        this.dataTable.Columns[(int)RecordColumn.ALM_INFO_TYPE].ColumnName = "报警类型";
                        this.dataTable.Columns[(int)RecordColumn.ALM_MODULE_ID].ColumnName = "模组ID";
                        this.dataTable.Columns[(int)RecordColumn.ALM_MODULE_NAME].ColumnName = "模组名";
                        this.dataTable.Columns[(int)RecordColumn.ALM_ALARM_TIME].ColumnName = "报警时间";

                        this.toolTip.SetToolTip(this.labelPageInfo, this.dataTable.Rows.Count.ToString("共0条记录"));
                        break;
                    }
                case QueryType.DryingOvenLog:
                    {
                        modIdx--;
                        if(!DataBaseLog.GetDryingOvenLogList(Def.GetProductFormula(), modIdx, startTime, endTime, ref this.dataTable))
                        {
                            return;
                        }
                        if(this.dataTable.Rows.Count > 0)
                        {
                            this.dataTable = this.dataTable.Rows.Cast<DataRow>().OrderBy(r => r[DryingOvenLogColumn.OptDate.ToString()]).CopyToDataTable();
                        }
                        this.dataTable.Columns[(int)DryingOvenLogColumn.FormulaID].ColumnName = "产品ID";
                        this.dataTable.Columns[(int)DryingOvenLogColumn.Operater].ColumnName = "操作者";
                        this.dataTable.Columns[(int)DryingOvenLogColumn.OptDate].ColumnName = "时间";
                        this.dataTable.Columns[(int)DryingOvenLogColumn.OvenID].ColumnName = "干燥炉ID";
                        this.dataTable.Columns[(int)DryingOvenLogColumn.OvenName].ColumnName = "干燥炉名称";
                        this.dataTable.Columns[(int)DryingOvenLogColumn.OptMode].ColumnName = "模式：手动/自动";
                        this.dataTable.Columns[(int)DryingOvenLogColumn.OvenAction].ColumnName = "指令动作";

                        this.toolTip.SetToolTip(this.labelPageInfo, this.dataTable.Rows.Count.ToString("共0条记录"));
                        break;
                    }
                case QueryType.ParameterLog:
                    {
                        modIdx -= 2;
                        if(!DataBaseLog.GetParameterLogList(Def.GetProductFormula(), modIdx, startTime, endTime, ref this.dataTable))
                        {
                            return;
                        }
                        if(this.dataTable.Rows.Count > 0)
                        {
                            this.dataTable = this.dataTable.Rows.Cast<DataRow>().OrderBy(r => r[ParameterLogColumn.OptDate.ToString()]).CopyToDataTable();
                        }
                        this.dataTable.Columns[(int)ParameterLogColumn.FormulaID].ColumnName = "产品ID";
                        this.dataTable.Columns[(int)ParameterLogColumn.Operater].ColumnName = "操作者";
                        this.dataTable.Columns[(int)ParameterLogColumn.OptDate].ColumnName = "时间";
                        this.dataTable.Columns[(int)ParameterLogColumn.ModuleID].ColumnName = "模组ID";
                        this.dataTable.Columns[(int)ParameterLogColumn.ModuleName].ColumnName = "模组名称";
                        this.dataTable.Columns[(int)ParameterLogColumn.ParmName].ColumnName = "参数";
                        this.dataTable.Columns[(int)ParameterLogColumn.OldValue].ColumnName = "原值";
                        this.dataTable.Columns[(int)ParameterLogColumn.NewValue].ColumnName = "新值";

                        this.toolTip.SetToolTip(this.labelPageInfo, this.dataTable.Rows.Count.ToString("共0条记录"));
                        break;
                    }
                case QueryType.RobotLog:
                    {
                        modIdx--;
                        if(!DataBaseLog.GetRobotLogList(Def.GetProductFormula(), modIdx, startTime, endTime, ref this.dataTable))
                        {
                            return;
                        }
                        if(this.dataTable.Rows.Count > 0)
                        {
                            this.dataTable = this.dataTable.Rows.Cast<DataRow>().OrderBy(r => r[RobotLogColumn.OptDate.ToString()]).CopyToDataTable();
                        }
                        this.dataTable.Columns[(int)RobotLogColumn.FormulaID].ColumnName = "产品ID";
                        this.dataTable.Columns[(int)RobotLogColumn.Operater].ColumnName = "操作者";
                        this.dataTable.Columns[(int)RobotLogColumn.OptDate].ColumnName = "时间";
                        this.dataTable.Columns[(int)RobotLogColumn.RobotID].ColumnName = "机器人ID";
                        this.dataTable.Columns[(int)RobotLogColumn.RobotName].ColumnName = "机器人名称";
                        this.dataTable.Columns[(int)RobotLogColumn.OptMode].ColumnName = "模式：手动/自动";
                        this.dataTable.Columns[(int)RobotLogColumn.SendRecv].ColumnName = "发送/接收";
                        this.dataTable.Columns[(int)RobotLogColumn.RobotAction].ColumnName = "指令动作";

                        this.toolTip.SetToolTip(this.labelPageInfo, this.dataTable.Rows.Count.ToString("共0条记录"));
                        break;
                    }
                case QueryType.MotorLog:
                    {
                        modIdx--;
                        if(!DataBaseLog.GetMotorLogList(Def.GetProductFormula(), modIdx, startTime, endTime, ref this.dataTable))
                        {
                            return;
                        }
                        if(this.dataTable.Rows.Count > 0)
                        {
                            this.dataTable = this.dataTable.Rows.Cast<DataRow>().OrderBy(r => r[RobotLogColumn.OptDate.ToString()]).CopyToDataTable();
                        }
                        this.dataTable.Columns[(int)MotorLogColumn.FormulaID].ColumnName = "产品ID";
                        this.dataTable.Columns[(int)MotorLogColumn.Operater].ColumnName = "操作者";
                        this.dataTable.Columns[(int)MotorLogColumn.OptDate].ColumnName = "时间";
                        this.dataTable.Columns[(int)MotorLogColumn.MotorID].ColumnName = "电机ID";
                        this.dataTable.Columns[(int)MotorLogColumn.MotorName].ColumnName = "电机名称";
                        this.dataTable.Columns[(int)MotorLogColumn.OptMode].ColumnName = "模式：手动/自动";
                        this.dataTable.Columns[(int)MotorLogColumn.OptAction].ColumnName = "指令动作";
                        this.dataTable.Columns[(int)MotorLogColumn.OldValue].ColumnName = "原值";
                        this.dataTable.Columns[(int)MotorLogColumn.NewValue].ColumnName = "新值";

                        this.toolTip.SetToolTip(this.labelPageInfo, this.dataTable.Rows.Count.ToString("共0条记录"));
                        break;
                    }
                default:
                    break;
            }
            UpdataListInfo(queryType, 0);
        }

        /// <summary>
        /// 导出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonExport_Click(object sender, EventArgs e)
        {
            string xlsFile = @"D:\生产信息\历史记录\";

            #region // 暂时不使用人为指定文件位置

            //SaveFileDialog dlg = new SaveFileDialog();
            ////如果文件名未写后缀名则自动添加     *.*不会自动添加后缀名
            //dlg.AddExtension = true;
            //dlg.Filter = "Excel File|.xls";
            //if(DialogResult.OK == dlg.ShowDialog())
            //{
            //    xlsFile = dlg.FileName;
            //    xlsFile = xlsFile.Remove(xlsFile.LastIndexOf('\\') + 1);
            //}
            #endregion

            if(Def.CreateFilePath(xlsFile))
            {
                string msg = "";

                #region // 保存为csv

                string csvFile, title, csv;
                csvFile = string.Format("{0}{1}.csv", xlsFile, DateTime.Now.ToString("yyyy-MM-dd HHmmss"));
                title = csv = "";
                foreach(DataColumn item in this.dataTable.Columns)
                {
                    title += item.ColumnName + ",";
                }
                for(int rowIdx = 0; rowIdx < this.dataTable.Rows.Count; rowIdx++)
                {
                    for(int colIdx = 0; colIdx < this.dataTable.Columns.Count; colIdx++)
                    {
                        csv += this.dataTable.Rows[rowIdx][colIdx].ToString().Replace("\r", "").Replace("\n", "") + ",";
                    }
                    csv = csv.TrimEnd(',') + "\r\n";
                }
                msg = string.Format("文件：{0}\r\n导出{1}", csvFile, Def.ExportCsvFile(csvFile, title.TrimEnd(','), csv.TrimEnd(',')) ? "成功" : "失败");
                #endregion

                #region // 保存为Excel

                //xlsFile = string.Format("{0}{1}.xlsx", xlsFile, DateTime.Now.ToString("yyyy-MM-dd HHmmss"));
                //msg = string.Format("文件：{0}\r\n导出{1}", xlsFile, Def.ExportExcel(this.dataTable, xlsFile) ? "成功" : "失败");
                #endregion

                ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
            }
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonDelete_Click(object sender, EventArgs e)
        {
            if (MachineCtrl.GetInstance().dbRecord.UserLevel() > SystemControlLibrary.UserLevelType.USER_MAINTENANCE)
            {
                ShowMsgBox.ShowDialog("当前账户无权限删除记录", MessageType.MsgWarning);
                return;
            }
            int almID = -1;
            if(!string.IsNullOrEmpty(this.textBoxFindID.Text))
            {
                if(!int.TryParse(this.textBoxFindID.Text, out almID))
                    almID = -1;
            }
            string startTime = this.dateTimePickerStart.Value.ToString(Def.DateFormal);
            string endTime = this.dateTimePickerEnd.Value.ToString(Def.DateFormal);
            int modIdx = this.comboBoxModule.SelectedIndex;
            QueryType queryType = (QueryType)this.comboBoxType.SelectedIndex;
            switch(queryType)
            {
                case QueryType.Alarm:
                    modIdx = (0 == modIdx) ? -1 : MachineCtrl.GetInstance().ListRuns[modIdx - 1].GetRunID();    // 索引转为RunID
                    MachineCtrl.GetInstance().dbRecord.DeleteAlarmInfo(Def.GetProductFormula(), modIdx, almID, startTime, endTime);
                    break;
                case QueryType.DryingOvenLog:
                    modIdx--;
                    DataBaseLog.DeleteDryingOvenLog(Def.GetProductFormula(), modIdx, startTime, endTime);
                    break;
                case QueryType.ParameterLog:
                    modIdx -= 2;
                    DataBaseLog.DeleteParameterLog(Def.GetProductFormula(), modIdx, startTime, endTime);
                    break;
                case QueryType.RobotLog:
                    modIdx--;
                    DataBaseLog.DeleteRobotLog(Def.GetProductFormula(), modIdx, startTime, endTime);
                    break;
                case QueryType.MotorLog:
                    modIdx--;
                    DataBaseLog.DeleteMotorLog(Def.GetProductFormula(), modIdx, startTime, endTime);
                    break;
                default:
                    break;
            }
            buttonQuery_Click(sender, e);

            string log = $"{MachineCtrl.GetInstance().OperaterID}删除了【{this.comboBoxType.SelectedItem} - {this.comboBoxModule.SelectedItem}】中{startTime}到{endTime}的记录集";
            Def.WriteLog("HistoryPage.Delete()", log, LogType.Success);
        }

        /// <summary>
        /// 第一页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonFirst_Click(object sender, EventArgs e)
        {
            this.selectedPage = 0;
            UpdataListInfo((QueryType)this.comboBoxType.SelectedIndex, this.selectedPage);
        }

        /// <summary>
        /// 上一页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonPrevious_Click(object sender, EventArgs e)
        {
            UpdataListInfo((QueryType)this.comboBoxType.SelectedIndex, (this.selectedPage > 0 ? --selectedPage : selectedPage));
        }

        /// <summary>
        /// 下一页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonNext_Click(object sender, EventArgs e)
        {
            int pageCount = this.dataTable.Rows.Count / PageMaxItem;
            UpdataListInfo((QueryType)this.comboBoxType.SelectedIndex, (this.selectedPage < pageCount ? ++selectedPage : selectedPage));
        }

        /// <summary>
        /// 最后一页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonLast_Click(object sender, EventArgs e)
        {
            this.selectedPage = (this.dataTable.Rows.Count / PageMaxItem);
            UpdataListInfo((QueryType)this.comboBoxType.SelectedIndex, this.selectedPage);
        }

        /// <summary>
        /// 更新记录列表
        /// </summary>
        /// <param name="queryType"></param>
        /// <param name="page"></param>
        void UpdataListInfo(QueryType queryType, int page)
        {
            if(null == this.dataTable)
            {
                return;
            }
            this.dataGridViewData.Columns.Clear();
            int[] wid = null;
            switch(queryType)
            {
                case QueryType.Alarm:
                    wid = new int[(int)RecordColumn.ALM_INFO_END] { 10, 10, 35, 10, 10, 10, 15 };
                    break;
                case QueryType.DryingOvenLog:
                    wid = new int[(int)DryingOvenLogColumn.End] { 5, 5, 15, 10, 10, 10, 45 };
                    break;
                case QueryType.ParameterLog:
                    wid = new int[(int)ParameterLogColumn.End] { 5, 5, 15, 5, 10, 15, 20, 20 };
                    break;
                case QueryType.RobotLog:
                    wid = new int[(int)RobotLogColumn.End] { 5, 5, 15, 10, 10, 10, 10, 35 };
                    break;
                case QueryType.MotorLog:
                    wid = new int[(int)MotorLogColumn.End] { 5, 5, 15, 10, 15, 10, 10, 15, 15 };
                    break;
                default:
                    return;
                    break;
            }
            if(wid.Length == this.dataTable.Columns.Count)
            {
                foreach(DataColumn item in this.dataTable.Columns)
                {
                    int idx = this.dataGridViewData.Columns.Add(item.Ordinal.ToString(), item.ColumnName);
                    this.dataGridViewData.Columns[idx].FillWeight = wid[idx];
                }
            }
            int maxItem = (page + 1) * PageMaxItem;
            if(maxItem >= this.dataTable.Rows.Count)
            {
                maxItem = this.dataTable.Rows.Count;
            }
            for(int i = page * PageMaxItem; i < maxItem; i++)
            {
                this.dataGridViewData.Rows.Add(this.dataTable.Rows[i].ItemArray);
            }

            // 设置页码信息
            int pageCount = this.dataTable.Rows.Count / PageMaxItem + (this.dataTable.Rows.Count % PageMaxItem > 0 ? 1 : 0);
            this.labelPageInfo.Text = string.Format("第{0}页/共{1}页", (pageCount >= page + 1 ? page + 1 : pageCount), pageCount);
        }

    }
}
