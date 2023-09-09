using HelperLibrary;
using System;
using System.Drawing;
using System.Windows.Forms;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    public partial class UserPasswordsModify : Form
    {
        public UserPasswordsModify()
        {
            InitializeComponent();

            CreateUserPasswordsView();
        }

        #region // 字段

        TextBox txtUserName;            // 用户名
        TextBox txtUserOldPW;           // 用户原密码
        TextBox txtUserNewPW;           // 用户新密码
        TextBox txtUserConfirmPW;       // 用户确认密码

        UserFormula userInfo;
        DataBaseRecord dbRecord;
        #endregion

        /// <summary>
        /// 创建用户密码修改视图
        /// </summary>
        private void CreateUserPasswordsView()
        {
            int row, col, index;
            float fHig = (float)0.0;
            row = col = index = 0;

            // 设置表
            row = 6;
            col = 5;
            fHig = (float)(100.0 / row);
            this.tablePanelUserPasswords.RowCount = row;
            this.tablePanelUserPasswords.ColumnCount = col;
            this.tablePanelUserPasswords.Padding = new Padding(0, 20, 0, 0);
            // 设置行列风格
            for(int i = 0; i < this.tablePanelUserPasswords.RowCount; i++)
            {
                if(i < this.tablePanelUserPasswords.RowStyles.Count)
                {
                    this.tablePanelUserPasswords.RowStyles[i] = new RowStyle(SizeType.Percent, fHig);
                }
                else
                {
                    this.tablePanelUserPasswords.RowStyles.Add(new RowStyle(SizeType.Percent, fHig));
                }
            }
            for(int i = this.tablePanelUserPasswords.ColumnStyles.Count; i < col; i++)
            {
                this.tablePanelUserPasswords.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, fHig));
            }
            this.tablePanelUserPasswords.ColumnStyles[0] = new ColumnStyle(SizeType.Percent, (float)(100.0 / 10 * 1));
            this.tablePanelUserPasswords.ColumnStyles[1] = new ColumnStyle(SizeType.Percent, (float)(100.0 / 10 * 3));
            this.tablePanelUserPasswords.ColumnStyles[2] = new ColumnStyle(SizeType.Percent, (float)(100.0 / 10 * 2));
            this.tablePanelUserPasswords.ColumnStyles[3] = new ColumnStyle(SizeType.Percent, (float)(100.0 / 10 * 3));
            this.tablePanelUserPasswords.ColumnStyles[4] = new ColumnStyle(SizeType.Percent, (float)(100.0 / 10 * 1));
            // 添加控件
            index = 0;
            Label lbl = new Label();
            lbl.Text = "用户名：";
            this.tablePanelUserPasswords.Controls.Add(lbl, 1, index);
            this.txtUserName = new TextBox();
            this.txtUserName.ReadOnly = true;
            this.tablePanelUserPasswords.Controls.Add(this.txtUserName, 2, index++);
            lbl = new Label();
            lbl.Text = "原密码：";
            this.tablePanelUserPasswords.Controls.Add(lbl, 1, index);
            this.txtUserOldPW = new TextBox();
            this.txtUserOldPW.Text = string.Empty;
            this.txtUserOldPW.UseSystemPasswordChar = true;
            this.tablePanelUserPasswords.Controls.Add(this.txtUserOldPW, 2, index++);
            lbl = new Label();
            lbl.Text = "新密码：";
            this.tablePanelUserPasswords.Controls.Add(lbl, 1, index);
            this.txtUserNewPW = new TextBox();
            this.txtUserNewPW.Text = string.Empty;
            this.txtUserNewPW.UseSystemPasswordChar = true;
            this.tablePanelUserPasswords.Controls.Add(this.txtUserNewPW, 2, index++);
            lbl = new Label();
            lbl.Text = "确认密码：";
            this.tablePanelUserPasswords.Controls.Add(lbl, 1, index);
            this.txtUserConfirmPW = new TextBox();
            this.txtUserConfirmPW.Text = string.Empty;
            this.txtUserConfirmPW.UseSystemPasswordChar = true;
            this.tablePanelUserPasswords.Controls.Add(this.txtUserConfirmPW, 2, index++);
            index++;    // 间隔一行
            Button btn = new Button();
            btn.Text = "修  改";
            btn.Click += Btn_Click_OK;
            this.AcceptButton = btn;    // 接收Enter按键
            this.tablePanelUserPasswords.Controls.Add(btn, 1, index);
            btn = new Button();
            btn.Text = "取  消";
            btn.Click += Btn_Click_Cancel;
            this.CancelButton = btn;    // 接收Cancel按键
            this.tablePanelUserPasswords.Controls.Add(btn, 3, index);

            foreach(Control item in this.tablePanelUserPasswords.Controls)
            {
                if(item is Label)
                {
                    item.Font = new Font(item.Font.FontFamily, 12);
                    item.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                }
                else if((item is TextBox) || (item is ComboBox))
                {
                    this.tablePanelUserPasswords.SetColumnSpan(item, 2);
                    item.Font = new Font(item.Font.FontFamily, 12);
                    item.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                }
                else if(item is Button)
                {
                    item.Dock = DockStyle.Fill;
                }
            }
        }

        public void SetUserInfo(DataBaseRecord db, UserFormula user)
        {
            this.userInfo = user;
            this.dbRecord = db;

            this.txtUserName.Text = user.userName;
        }

        private void Btn_Click_Cancel(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void Btn_Click_OK(object sender, EventArgs e)
        {
            if (this.userInfo.userPassword.ToString() == this.txtUserOldPW.Text)
            {
                if (this.txtUserNewPW.Text == this.txtUserConfirmPW.Text)
                {
                    try
                    {
                        this.dbRecord.ModifyUserInfo(new UserFormula(this.userInfo.userID, this.txtUserName.Text, this.txtUserNewPW.Text, this.userInfo.userLevel));
                    }
                    catch (System.Exception ex)
                    {
                        ShowMsgBox.ShowDialog($"尝试修改用户信息失败\r\n{ex.Message}", MessageType.MsgAlarm);
                    }
                } 
                else
                {
                    ShowMsgBox.ShowDialog("新密码和二次确认密码不相同，无法更改！", MessageType.MsgAlarm);
                    return;
                }
            }
            else
            {
                ShowMsgBox.ShowDialog("原密码不正确，无法更改！", MessageType.MsgAlarm);
                return;
            }
            DialogResult = DialogResult.OK;
        }
    }
}
