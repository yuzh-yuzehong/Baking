using System;
using System.Drawing;
using System.Windows.Forms;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    public partial class MesInfoPage : FormEx
    {
        public MesInfoPage()
        {
            InitializeComponent();

            CreateListViewModule();
        }

        #region // 字段

        DateTimePicker dtPicker;
        DataGridViewCell dgvCell;

        #endregion

        /// <summary>
        /// 创建模组列表
        /// </summary>
        private void CreateListViewModule()
        {
            string timeFormat = "HH:mm:ss";
            this.dtPicker = new DateTimePicker();
            this.dtPicker.Visible = false;
            this.dtPicker.Format = DateTimePickerFormat.Custom;
            this.dtPicker.CustomFormat = timeFormat;
            this.dtPicker.Leave += DtPicker_Leave;
            this.dtPicker.ShowUpDown = true;

            // 设置表格
            DataGridViewNF[] dgvNF = new DataGridViewNF[] { this.dataGridViewShift, this.dataGridViewResources };
            for(int i = 0; i < dgvNF.Length; i++)
            {
                dgvNF[i].SetViewStatus();
                dgvNF[i].ReadOnly = false;
                dgvNF[i].AllowUserToAddRows = true;         // 可以添加行
                dgvNF[i].AllowUserToDeleteRows = true;      // 可以删除行
                dgvNF[i].EditMode = DataGridViewEditMode.EditOnEnter;
            }
            // 资源信息
            DataGridViewNF dgv = this.dataGridViewResources;
            DataGridViewTextBoxColumn txtBoxCol = new DataGridViewTextBoxColumn();
            txtBoxCol.HeaderText = "资源名";
            int idx = dgv.Columns.Add(txtBoxCol);
            txtBoxCol = new DataGridViewTextBoxColumn();
            txtBoxCol.HeaderText = "设备编码";
            idx = dgv.Columns.Add(txtBoxCol);
            txtBoxCol = new DataGridViewTextBoxColumn();
            txtBoxCol.HeaderText = "设备名称";
            idx = dgv.Columns.Add(txtBoxCol);
            txtBoxCol = new DataGridViewTextBoxColumn();
            txtBoxCol.HeaderText = "工序编码";
            idx = dgv.Columns.Add(txtBoxCol);
            txtBoxCol = new DataGridViewTextBoxColumn();
            txtBoxCol.HeaderText = "工序名称";
            idx = dgv.Columns.Add(txtBoxCol);
            txtBoxCol = new DataGridViewTextBoxColumn();
            txtBoxCol.HeaderText = "工段";
            idx = dgv.Columns.Add(txtBoxCol);
            foreach(DataGridViewColumn item in dgv.Columns)
            {
                item.SortMode = DataGridViewColumnSortMode.NotSortable;     // 禁止列排序
            }
            dgv.Columns[0].ReadOnly = true;     // 禁止首列编辑
            // 添加需要配置的资源ID
            int colIdx, rowIdx;
            for(int ovenIdx = 0; ovenIdx < MesResources.OvenCavity.GetLength(0); ovenIdx++)
            {
                for(int cavityIdx = 0; cavityIdx < MesResources.OvenCavity.GetLength(1); cavityIdx++)
                {
                    colIdx = 0;
                    rowIdx = dgv.Rows.Add();
                    dgv.Rows[rowIdx].Height = 25;        // 行高度
                    dgv.Rows[rowIdx].Cells[colIdx++].Value = $"Oven{ovenIdx}_Cavity{cavityIdx}";
                    dgv.Rows[rowIdx].Cells[colIdx++].Value = MesResources.OvenCavity[ovenIdx, cavityIdx].EquipmentID;
                    dgv.Rows[rowIdx].Cells[colIdx++].Value = MesResources.OvenCavity[ovenIdx, cavityIdx].EquipmentName;
                    dgv.Rows[rowIdx].Cells[colIdx++].Value = MesResources.OvenCavity[ovenIdx, cavityIdx].ProcessID;
                    dgv.Rows[rowIdx].Cells[colIdx++].Value = MesResources.OvenCavity[ovenIdx, cavityIdx].ProcessName;
                    dgv.Rows[rowIdx].Cells[colIdx++].Value = MesResources.OvenCavity[ovenIdx, cavityIdx].WorkSection;
                }
            }
            string[] strRes = new string[] { "Group", "OnloadBindPallet", "OffloadUnbindPallet", "Heartbeat" };
            ResourcesStruct[] rs = new ResourcesStruct[] { MesResources.Group, MesResources.Onload, MesResources.Offload, MesResources.Heartbeat };
            for(int i = 0; i < rs.Length; i++)
            {
                colIdx = 0;
                rowIdx = dgv.Rows.Add();
                dgv.Rows[rowIdx].Height = 25;        // 行高度
                dgv.Rows[rowIdx].Cells[colIdx++].Value = strRes[i];
                dgv.Rows[rowIdx].Cells[colIdx++].Value = rs[i].EquipmentID;
                dgv.Rows[rowIdx].Cells[colIdx++].Value = rs[i].EquipmentName;
                dgv.Rows[rowIdx].Cells[colIdx++].Value = rs[i].ProcessID;
                dgv.Rows[rowIdx].Cells[colIdx++].Value = rs[i].ProcessName;
                dgv.Rows[rowIdx].Cells[colIdx++].Value = rs[i].WorkSection;
            }
            this.textBoxHeartbeat.Text = MesResources.HeartbeatInterval.ToString();

            // 班次信息
            dgv = this.dataGridViewShift;
            // 项
            txtBoxCol = new DataGridViewTextBoxColumn();
            txtBoxCol.HeaderText = "班次编码";
            idx = dgv.Columns.Add(txtBoxCol);
            txtBoxCol = new DataGridViewTextBoxColumn();
            txtBoxCol.HeaderText = "班次名称";
            idx = dgv.Columns.Add(txtBoxCol);
            txtBoxCol = new DataGridViewTextBoxColumn();
            txtBoxCol.HeaderText = "开始时间";
            txtBoxCol.ValueType = typeof(System.DateTime);
            txtBoxCol.DefaultCellStyle.Format = timeFormat;
            idx = dgv.Columns.Add(txtBoxCol);
            txtBoxCol = new DataGridViewTextBoxColumn();
            txtBoxCol.HeaderText = "结束时间";
            txtBoxCol.ValueType = typeof(System.DateTime);
            txtBoxCol.DefaultCellStyle.Format = timeFormat;
            idx = dgv.Columns.Add(txtBoxCol);
            foreach(DataGridViewColumn item in dgv.Columns)
            {
                item.SortMode = DataGridViewColumnSortMode.NotSortable;     // 禁止列排序
            }
            dgv.CellClick += MesInfoPage_CellClick;
            dgv.Controls.Add(this.dtPicker);
            // 添加已有的班次信息
            foreach(var item in OperationShifts.Shifts)
            {
                colIdx = 0;
                rowIdx = dgv.Rows.Add();
                dgv.Rows[rowIdx].Height = 25;        // 行高度
                dgv.Rows[rowIdx].Cells[colIdx++].Value = item.Code;
                dgv.Rows[rowIdx].Cells[colIdx++].Value = item.Name;
                dgv.Rows[rowIdx].Cells[colIdx++].Value = item.Start;
                dgv.Rows[rowIdx].Cells[colIdx++].Value = item.End;
            }

            // FTP信息
            this.textBoxFTPFilePath.Text = FTPDefine.FilePath;
            this.textBoxFTPUser.Text = FTPDefine.User;
            this.textBoxFTPPassword.Text = FTPDefine.Password;

        }

        private void DtPicker_Leave(object sender, EventArgs e)
        {
            if (null != this.dgvCell)
            {
                this.dgvCell.Value = this.dtPicker.Value;
            }
            this.dtPicker.Visible = false;
        }

        private void MesInfoPage_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // 2、3列为班次时间
            if ((2 == e.ColumnIndex) || (3 == e.ColumnIndex))
            {
                this.dgvCell = this.dataGridViewShift.CurrentCell;
                Rectangle rect = this.dataGridViewShift.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
                this.dtPicker.Size = new Size(rect.Width, rect.Height);
                this.dtPicker.Location = new Point(rect.X, rect.Y);
                if ((null == this.dgvCell.Value) || ("" == this.dgvCell.Value.ToString()))
                {
                    this.dtPicker.Value = DateTime.Now;
                }
                else
                {
                    this.dtPicker.Value = Convert.ToDateTime(this.dgvCell.Value);
                }
                this.dtPicker.Visible = true;
            }
            else
            {
                this.dtPicker.Visible = false;
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            string value;

            // 资源信息
            DataGridViewNF dgv = this.dataGridViewResources;
            ResourcesStruct[] rs = new ResourcesStruct[] { MesResources.Group, MesResources.Onload, MesResources.Offload, MesResources.Heartbeat };
            for(int i = 0; i < dgv.RowCount; i++)
            {
                int idx = 0;
                value = (null != dgv.Rows[i].Cells[0].Value) ? dgv.Rows[i].Cells[0].Value.ToString() : "";
                if(string.IsNullOrEmpty(value))
                {
                    continue;
                }
                if (i < ((int)OvenInfoCount.OvenCount * (int)OvenRowCol.MaxRow))
                {
                    idx++;
                    MesResources.OvenCavity[i / (int)OvenRowCol.MaxRow, i % (int)OvenRowCol.MaxRow].EquipmentID = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    idx++;
                    MesResources.OvenCavity[i / (int)OvenRowCol.MaxRow, i % (int)OvenRowCol.MaxRow].EquipmentName = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    idx++;
                    MesResources.OvenCavity[i / (int)OvenRowCol.MaxRow, i % (int)OvenRowCol.MaxRow].ProcessID = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    idx++;
                    MesResources.OvenCavity[i / (int)OvenRowCol.MaxRow, i % (int)OvenRowCol.MaxRow].ProcessName = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    idx++;
                    MesResources.OvenCavity[i / (int)OvenRowCol.MaxRow, i % (int)OvenRowCol.MaxRow].WorkSection = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                }
                else
                {
                    idx++;
                    MesResources.Group.EquipmentID = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    idx++;
                    MesResources.Group.EquipmentName = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    idx++;
                    MesResources.Group.ProcessID = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    idx++;
                    MesResources.Group.ProcessName = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    idx++;
                    MesResources.Group.WorkSection = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    i++;    // 已用一行数据
                    idx = 0;
                    idx++;
                    MesResources.Onload.EquipmentID = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    idx++;
                    MesResources.Onload.EquipmentName = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    idx++;
                    MesResources.Onload.ProcessID = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    idx++;
                    MesResources.Onload.ProcessName = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    idx++;
                    MesResources.Onload.WorkSection = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    i++;    // 已用一行数据
                    idx = 0;
                    idx++;
                    MesResources.Offload.EquipmentID = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    idx++;
                    MesResources.Offload.EquipmentName = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    idx++;
                    MesResources.Offload.ProcessID = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    idx++;
                    MesResources.Offload.ProcessName = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    idx++;
                    MesResources.Offload.WorkSection = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    i++;    // 已用一行数据
                    idx = 0;
                    idx++;
                    MesResources.Heartbeat.EquipmentID = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    idx++;
                    MesResources.Heartbeat.EquipmentName = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    idx++;
                    MesResources.Heartbeat.ProcessID = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    idx++;
                    MesResources.Heartbeat.ProcessName = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                    idx++;
                    MesResources.Heartbeat.WorkSection = (null != dgv.Rows[i].Cells[idx].Value) ? dgv.Rows[i].Cells[idx].Value.ToString() : "";
                }
            }
            int.TryParse(this.textBoxHeartbeat.Text, out MesResources.HeartbeatInterval);
            MesResources.WriteConfig();

            // 班次信息
            ShiftStruct shift = new ShiftStruct();

            OperationShifts.Shifts.Clear();

            dgv = this.dataGridViewShift;
            for(int i = 0; i < dgv.RowCount; i++)
            {
                shift.Code = (null != dgv.Rows[i].Cells[0].Value) ? dgv.Rows[i].Cells[0].Value.ToString() : "";
                if(string.IsNullOrEmpty(shift.Code))
                {
                    continue;
                }
                shift.Name = (null != dgv.Rows[i].Cells[1].Value) ? dgv.Rows[i].Cells[1].Value.ToString() : "";
                value = (null != dgv.Rows[i].Cells[2].Value) ? dgv.Rows[i].Cells[2].Value.ToString() : "";
                DateTime.TryParse(value, out shift.Start);
                value = (null != dgv.Rows[i].Cells[3].Value) ? dgv.Rows[i].Cells[3].Value.ToString() : "";
                DateTime.TryParse(value, out shift.End);

                OperationShifts.Shifts.Add(shift);
            }
            OperationShifts.WriteConfig();

            // FTP信息
            FTPDefine.FilePath = (null != this.textBoxFTPFilePath.Text) ? this.textBoxFTPFilePath.Text : "";
            FTPDefine.User = (null != this.textBoxFTPUser.Text) ? this.textBoxFTPUser.Text : "";
            FTPDefine.Password = (null != this.textBoxFTPPassword.Text) ? this.textBoxFTPPassword.Text : "";
            FTPDefine.WriteConfig();

            Def.WriteLog("MesInfoPage", "修改并保存参数");
        }

        private void MesInfoPage_Load(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// 释放线程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void DisposeForm()
        {
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
            UserFormula user = new UserFormula();
            MachineCtrl.GetInstance().dbRecord.GetCurUser(ref user);
            try
            {
                if ((MCState.MCInitializing == mc) || (MCState.MCRunning == mc) || (level >= UserLevelType.USER_MAINTENANCE) || (!user.userName.Contains("MES")))
                {
                    SetUIEnable(UIEnable.AllDisabled);
                }
                else if (level < UserLevelType.USER_MAINTENANCE || (user.userName.Contains("MES")))
                {
                    SetUIEnable(UIEnable.AllEnabled);
                }
            }
            catch(System.Exception ex)
            {
                Def.WriteLog("MesParameterPage.UpdataUIEnable()", ex.Message, HelperLibrary.LogType.Error);
            }
            base.UpdataUIEnable(mc, level);
        }

        /// <summary>
        /// 设置界面控件使能
        /// </summary>
        /// <param name="mc"></param>
        /// <param name="level"></param>
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

                            this.dataGridViewResources.ReadOnly = !en;
                            this.dataGridViewResources.Columns[0].ReadOnly = true;     // 禁止首列编辑
                            this.dataGridViewShift.Enabled = en;
                            this.buttonSave.Enabled = en;
                            this.textBoxHeartbeat.Enabled = en;
                            this.textBoxFTPFilePath.Enabled = en;
                            this.textBoxFTPUser.Enabled = en;
                            this.textBoxFTPPassword.Enabled = en;

                            break;
                        }
                    default:
                        break;
                }
            }));
        }
    }
}
