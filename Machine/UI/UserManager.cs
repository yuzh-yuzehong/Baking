using HelperLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    public partial class UserManager : Form
    {
        public UserManager()
        {
            InitializeComponent();

            // 创建用户管理视图
            CreateUserManagerView();
        }

        #region // 字段

        TextBox txtUserName;
        TextBox txtUserPW;
        ComboBox cmbUserLevel;

        DataBaseRecord dbRecord;
        #endregion

        /// <summary>
        /// 创建用户管理视图
        /// </summary>
        private void CreateUserManagerView()
        {
            int row, col, index;
            float fHig = (float)0.0;
            row = col = index = 0;

            #region // 界面分栏
            row = 1;
            col = 2;
            fHig = (float)(100.0 / row);
            this.tablePanelUserManager.RowCount = row;
            this.tablePanelUserManager.ColumnCount = col;
            this.tablePanelUserManager.Padding = new Padding(0, 10, 0, 0);
            // 设置行列风格
            for(int i = 0; i < this.tablePanelUserManager.RowCount; i++)
            {
                if(i < this.tablePanelUserManager.RowStyles.Count)
                {
                    this.tablePanelUserManager.RowStyles[i] = new RowStyle(SizeType.Percent, fHig);
                }
                else
                {
                    this.tablePanelUserManager.RowStyles.Add(new RowStyle(SizeType.Percent, fHig));
                }
            }
            for(int i = this.tablePanelUserManager.ColumnStyles.Count; i < col; i++)
            {
                this.tablePanelUserManager.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, fHig));
            }
            this.tablePanelUserManager.ColumnStyles[0] = new ColumnStyle(SizeType.Percent, (float)(65.0));
            this.tablePanelUserManager.ColumnStyles[1] = new ColumnStyle(SizeType.Percent, (float)(35.0));
            #endregion

            #region // 设置表格
            this.dataGridViewUser.ReadOnly = true;        // 只读不可编辑
            this.dataGridViewUser.MultiSelect = false;    // 禁止多选，只可单选
            this.dataGridViewUser.AutoGenerateColumns = false;        // 禁止创建列
            this.dataGridViewUser.AllowUserToAddRows = false;         // 禁止添加行
            this.dataGridViewUser.AllowUserToDeleteRows = false;      // 禁止删除行
            this.dataGridViewUser.AllowUserToResizeRows = false;      // 禁止行改变大小
            this.dataGridViewUser.RowHeadersVisible = false;          // 行表头不可见
            this.dataGridViewUser.Dock = DockStyle.Fill;              // 填充
            this.dataGridViewUser.EditMode = DataGridViewEditMode.EditProgrammatically;           // 软件编辑模式
            this.dataGridViewUser.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;     // 自动改变列宽
            this.dataGridViewUser.SelectionMode = DataGridViewSelectionMode.FullRowSelect;        // 整行选中
            this.dataGridViewUser.RowsDefaultCellStyle.BackColor = Color.WhiteSmoke;              // 偶数行颜色
            this.dataGridViewUser.AlternatingRowsDefaultCellStyle.BackColor = Color.GhostWhite;   // 奇数行颜色
            // 表头
            this.dataGridViewUser.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font(this.dataGridViewUser.ColumnHeadersDefaultCellStyle.Font.FontFamily, 10);
            this.dataGridViewUser.ColumnHeadersHeight = 35;
            this.dataGridViewUser.Columns.Add("index", "序号");
            this.dataGridViewUser.Columns.Add("userID", "用户ID");
            this.dataGridViewUser.Columns.Add("userName", "用户名");
            this.dataGridViewUser.Columns.Add("userPW", "用户密码");
            this.dataGridViewUser.Columns.Add("userLevel", "用户等级");
            foreach(DataGridViewColumn item in this.dataGridViewUser.Columns)
            {
                item.SortMode = DataGridViewColumnSortMode.NotSortable;     // 禁止列排序
            }
            #endregion

            #region // 操作栏
            row = 6;
            col = 2;
            fHig = (float)(100.0 / row);
            this.tablePanelOperation.RowCount = row;
            this.tablePanelOperation.ColumnCount = col;
            this.tablePanelOperation.Padding = new Padding(0, 20, 0, 0);
            // 设置行列风格
            for(int i = 0; i < this.tablePanelOperation.RowCount; i++)
            {
                if(i < this.tablePanelOperation.RowStyles.Count)
                {
                    this.tablePanelOperation.RowStyles[i] = new RowStyle(SizeType.Percent, fHig);
                }
                else
                {
                    this.tablePanelOperation.RowStyles.Add(new RowStyle(SizeType.Percent, fHig));
                }
            }
            for(int i = this.tablePanelOperation.ColumnStyles.Count; i < col; i++)
            {
                this.tablePanelOperation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, fHig));
            }
            this.tablePanelOperation.ColumnStyles[0] = new ColumnStyle(SizeType.Percent, (float)(100.0 / 10 * 3.5));
            this.tablePanelOperation.ColumnStyles[1] = new ColumnStyle(SizeType.Percent, (float)(100.0 / 10 * 6.5));
            // 添加控件
            index = 0;
            Label lbl = new Label();
            lbl.Text = "用户名：";
            this.tablePanelOperation.Controls.Add(lbl, 0, index);
            this.txtUserName = new TextBox();
            this.txtUserName.Text = string.Empty;
            this.tablePanelOperation.Controls.Add(this.txtUserName, 1, index++);
            lbl = new Label();
            lbl.Text = "密码：";
            this.tablePanelOperation.Controls.Add(lbl, 0, index);
            this.txtUserPW = new TextBox();
            this.txtUserPW.UseSystemPasswordChar = false;
            this.tablePanelOperation.Controls.Add(this.txtUserPW, 1, index++);
            lbl = new Label();
            lbl.Text = "等级：";
            this.tablePanelOperation.Controls.Add(lbl, 0, index);
            this.cmbUserLevel = new ComboBox();
            this.cmbUserLevel.Sorted = false;
            this.cmbUserLevel.DropDownStyle = ComboBoxStyle.DropDownList;
            this.tablePanelOperation.Controls.Add(this.cmbUserLevel, 1, index++);
            Button btn = new Button();
            btn.Text = "添加";
            btn.Click += Btn_Click_AddUser;
            this.tablePanelOperation.Controls.Add(btn, 0, index++);
            btn = new Button();
            btn.Text = "修改";
            btn.Click += Btn_Click_ModifyUser;
            this.tablePanelOperation.Controls.Add(btn, 0, index++);
            btn = new Button();
            btn.Text = "删除";
            btn.Click += Btn_Click_DeleteUser;
            this.tablePanelOperation.Controls.Add(btn, 0, index++);

            foreach(Control item in this.tablePanelOperation.Controls)
            {
                if((item is Label) || (item is TextBox) || (item is ComboBox))
                {
                    item.Font = new Font(item.Font.FontFamily, 12);
                    item.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                }
                else if(item is Button)
                {
                    this.tablePanelOperation.SetColumnSpan(item, 2);
                    ((Button)item).Dock = DockStyle.Fill;
                }
            }

            #endregion
        }

        /// <summary>
        /// 设置用户管理所需的数据库
        /// </summary>
        /// <param name="db"></param>
        public void SetUserManagerInfo(DataBaseRecord db, List<UserFormula> userList)
        {
            this.dbRecord = db;

            UpdataUserInfo();
            // 设置用户等级列表
            for(UserLevelType level = UserLevelType.USER_ADMIN; level < UserLevelType.USER_LOGOUT; level++)
            {
                this.cmbUserLevel.Items.Add((int)level);
            }
        }

        /// <summary>
        /// 更改选择用户时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridViewUser_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                DataGridView dgv = sender as DataGridView;
                if(null != dgv)
                {
                    if(dgv.CurrentRow.Cells[2].Value != null)
                    {
                        this.txtUserName.Text = dgv.CurrentRow.Cells[2].Value.ToString();
                    }
                    //this.txtUserPW.Text = dgv.CurrentRow.Cells[3].Tag.ToString();
                    if(dgv.CurrentRow.Cells[4].Value != null)
                    {
                        this.cmbUserLevel.SelectedIndex = Convert.ToInt32(dgv.CurrentRow.Cells[4].Value.ToString());
                    }
                }
            }
            catch (System.Exception ex)
            {
                Trace.Write("更改用户时捕获到异常：" + ex.ToString());
            }
        }

        /// <summary>
        /// 添加用户
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_Click_AddUser(object sender, EventArgs e)
        {
            try
            {
                if(txtUserName.Text.Length <= 0)
                {
                    txtUserName.Focus();
                    MessageBox.Show("请输入用户名", "错误");
                    return;
                }
                string msgInfo = $"确定添加【{txtUserName.Text}】用户吗？";
                if (MessageBox.Show(msgInfo, "确认", MessageBoxButtons.YesNo) != DialogResult.Yes)
                {
                    return;
                }
                UserLevelType userLvl = (UserLevelType)this.cmbUserLevel.SelectedIndex;
                if ("MES" == this.txtUserName.Text)
                {
                    userLvl = UserLevelType.USER_MAINTENANCE;
                }
                this.dbRecord.AddUserInfo(new UserFormula(Def.GetGUID(), this.txtUserName.Text, this.txtUserPW.Text, userLvl));
                this.txtUserName.Text = "";
                this.txtUserPW.Text = "";
                this.cmbUserLevel.SelectedIndex = -1;
                UpdataUserInfo();
            }
            catch (System.Exception ex)
            {
                ShowMsgBox.ShowDialog($"无法添加此用户，请检查用户信息\r\n{ex.Message}", MessageType.MsgAlarm);
            }
        }

        /// <summary>
        /// 修改用户
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_Click_ModifyUser(object sender, EventArgs e)
        {
            try
            {
                if (txtUserName.Text.Length <= 0)
                {
                    txtUserName.Focus();
                    MessageBox.Show("请输入用户名或选择用户", "错误");
                    return;
                }
                string msgInfo = $"确定修改【{txtUserName.Text}】用户信息吗？";
                if (MessageBox.Show(msgInfo, "确认", MessageBoxButtons.YesNo) != DialogResult.Yes)
                {
                    return;
                }
                this.dbRecord.ModifyUserInfo(new UserFormula(this.dataGridViewUser.CurrentRow.Cells[1].Value.ToString(), this.txtUserName.Text, this.txtUserPW.Text, (UserLevelType)this.cmbUserLevel.SelectedIndex));
                this.txtUserName.Text = "";
                this.txtUserPW.Text = "";
                this.cmbUserLevel.SelectedIndex = -1;
                UpdataUserInfo();
            }
            catch(System.Exception ex)
            {
                ShowMsgBox.ShowDialog($"无法添加此用户，请检查用户信息\r\n{ex.Message}", MessageType.MsgAlarm);
            }
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_Click_DeleteUser(object sender, EventArgs e)
        {
            try
            {
                if (txtUserName.Text.Length <= 0)
                {
                    txtUserName.Focus();
                    MessageBox.Show("请输入用户名或选择用户", "错误");
                    return;
                }
                string msgInfo = $"确定删除【{txtUserName.Text}】用户吗？";
                if (MessageBox.Show(msgInfo, "提示", MessageBoxButtons.YesNo) != DialogResult.Yes)
                {
                    return;
                }
                this.dbRecord.DeleteUserInfo(new UserFormula(this.dataGridViewUser.CurrentRow.Cells[1].Value.ToString(), this.txtUserName.Text, this.txtUserPW.Text, (UserLevelType)this.cmbUserLevel.SelectedIndex));
                this.txtUserName.Text = "";
                this.txtUserPW.Text = "";
                this.cmbUserLevel.SelectedIndex = -1;
                UpdataUserInfo();
            }
            catch(System.Exception ex)
            {
                ShowMsgBox.ShowDialog($"无法添加此用户，请检查用户信息\r\n{ex.Message}", MessageType.MsgAlarm);
            }
        }

        /// <summary>
        /// 更新用户列表
        /// </summary>
        private void UpdataUserInfo()
        {
            List<UserFormula> userList = new List<UserFormula>();
            if (this.dbRecord.GetUserList(ref userList))
            {
                this.dataGridViewUser.Rows.Clear();
                // 设置用户列表
                foreach(var item in userList)
                {
                    int index = this.dataGridViewUser.Rows.Add();
                    this.dataGridViewUser.Rows[index].Height = 30;

                    this.dataGridViewUser.Rows[index].Cells[0].Value = index + 1;
                    this.dataGridViewUser.Rows[index].Cells[1].Value = item.userID;
                    this.dataGridViewUser.Rows[index].Cells[2].Value = item.userName;
                    this.dataGridViewUser.Rows[index].Cells[3].Value = "●●●●●●";
                    this.dataGridViewUser.Rows[index].Cells[3].Tag = item.userPassword;
                    this.dataGridViewUser.Rows[index].Cells[4].Value = (int)item.userLevel;
                }
            }
        }
    }
}
