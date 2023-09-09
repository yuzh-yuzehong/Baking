using HelperLibrary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    public partial class UserLogin : Form
    {
        public UserLogin()
        {
            InitializeComponent();

            // 创建用户管理视图
            CreateUserManagerView();
        }

        #region // 字段

        ComboBox cmbUserList;
        TextBox txtUserPW;

        List<UserFormula> userList;
        DataBaseRecord dbRecord;

        public string userInfo { get; private set; }

        #endregion

        /// <summary>
        /// 创建用户登录视图
        /// </summary>
        private void CreateUserManagerView()
        {
            int row, col, index;
            float fHig = (float)0.0;
            row = col = index = 0;

            // 设置表
            row = 5;
            col = 4;
            fHig = (float)(100.0 / row);
            this.tablePanelUserLogin.RowCount = row;
            this.tablePanelUserLogin.ColumnCount = col;
            this.tablePanelUserLogin.Padding = new Padding(0, 20, 0, 0);
            // 设置行列风格
            for(int i = 0; i < this.tablePanelUserLogin.RowCount; i++)
            {
                if(i < this.tablePanelUserLogin.RowStyles.Count)
                {
                    this.tablePanelUserLogin.RowStyles[i] = new RowStyle(SizeType.Percent, fHig);
                }
                else
                {
                    this.tablePanelUserLogin.RowStyles.Add(new RowStyle(SizeType.Percent, fHig));
                }
            }
            for(int i = this.tablePanelUserLogin.ColumnStyles.Count; i < col; i++)
            {
                this.tablePanelUserLogin.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, fHig));
            }
            this.tablePanelUserLogin.ColumnStyles[0] = new ColumnStyle(SizeType.Percent, (float)(100.0 / 10 * 1));
            this.tablePanelUserLogin.ColumnStyles[1] = new ColumnStyle(SizeType.Percent, (float)(100.0 / 10 * 3));
            this.tablePanelUserLogin.ColumnStyles[2] = new ColumnStyle(SizeType.Percent, (float)(100.0 / 10 * 5));
            this.tablePanelUserLogin.ColumnStyles[3] = new ColumnStyle(SizeType.Percent, (float)(100.0 / 10 * 1));
            // 添加控件
            index = 0;
            Label lbl = new Label();
            lbl.Text = "用户名：";
            this.tablePanelUserLogin.Controls.Add(lbl, 1, index);
            this.cmbUserList = new ComboBox();
            this.cmbUserList.Sorted = false;
            this.cmbUserList.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbUserList.SelectedIndexChanged += new EventHandler(Cmb_Select_Change);
            this.tablePanelUserLogin.Controls.Add(this.cmbUserList, 2, index++);
            lbl = new Label();
            lbl.Text = "密码：";
            this.tablePanelUserLogin.Controls.Add(lbl, 1, index);
            this.txtUserPW = new TextBox();
            this.txtUserPW.Text = string.Empty;
            this.txtUserPW.UseSystemPasswordChar = true;
            this.txtUserPW.TabIndex = 0;
            this.tablePanelUserLogin.Controls.Add(this.txtUserPW, 2, index++);
            index++;    // 间隔一行
            Button btn = new Button();
            btn.Text = "登  录";
            btn.Click += Btn_Click_Login;
            this.AcceptButton = btn;    // 接收Enter按键
            this.tablePanelUserLogin.Controls.Add(btn, 1, index++);

            foreach(Control item in this.tablePanelUserLogin.Controls)
            {
                if((item is Label) || (item is TextBox) || (item is ComboBox))
                {
                    item.Font = new Font(item.Font.FontFamily, 12);
                    item.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                }
                else if(item is Button)
                {
                    this.tablePanelUserLogin.SetColumnSpan(item, 2);
                    item.Dock = DockStyle.Fill;
                }
            }

        }

        /// <summary>
        /// 设置用户
        /// </summary>
        public void SetUserList(DataBaseRecord db, List<UserFormula> userlist)
        {
            this.cmbUserList.Items.Clear();
            foreach(var item in userlist)
            {
                this.cmbUserList.Items.Add(item.userName);
            }
            if (userlist.Count > 0)
            {
                this.cmbUserList.SelectedIndex = 0;
            }
            this.userList = userlist;
            this.dbRecord = db;
        }

        /// <summary>
        /// 登录用户
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_Click_Login(object sender, EventArgs e)
        {
            if (null != this.dbRecord)
            {
                this.userInfo = this.cmbUserList.SelectedItem.ToString();
                if((null != this.cmbUserList.SelectedItem)
                    && MachineCtrl.GetInstance().dbRecord.UserLogin(this.userInfo, this.txtUserPW.Text))
                {
                    DialogResult = DialogResult.OK;
                }
                else
                {
                    ShowMsgBox.ShowDialog("用户名或密码不正确，无法登录！", MessageType.MsgAlarm);
                }
            }
            else
            {
                if(!string.IsNullOrEmpty(this.txtUserPW.Text))
                {
                    this.userInfo = this.txtUserPW.Text;
                    DialogResult = DialogResult.OK;
                }
                else
                {
                    ShowMsgBox.ShowDialog($"{this.cmbUserList.SelectedItem.ToString()}不能为空！", MessageType.MsgWarning);
                }
            }
        }

        private void UserLogin_Activated(object sender, EventArgs e)
        {
            this.txtUserPW.Focus();
        }

        private void Cmb_Select_Change(object sender, EventArgs e)
        {
            this.txtUserPW.Text = "";
            this.txtUserPW.Focus();
        }
    }
}
