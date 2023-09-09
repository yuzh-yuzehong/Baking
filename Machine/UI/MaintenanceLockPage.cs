using HelperLibrary;
using System;
using System.Windows.Forms;

namespace Machine
{
    public partial class MaintenanceLockPage : Form
    {
        #region // 字段

        bool pageClose;
        bool pageLock;
        string pageLockPW;

        #endregion

        public MaintenanceLockPage()
        {
            InitializeComponent();

            this.pageClose = false;
            this.pageLock = false;
        }
        
        private void buttonLock_Click(object sender, EventArgs e)
        {
            if (this.pageLock)
            {
                if((this.pageLockPW == this.textBox1.Text) || (this.pageLockPW == this.textBox2.Text)
                    || MachineCtrl.GetInstance().dbRecord.UserLogin(this.textBox1.Text, this.textBox2.Text))
                {
                    this.pageClose = true;
                    this.Close();
                }
                else
                {
                    ShowMsgBox.ShowDialog("两次输入密码不一致，请重新输入\r\n\r\n或输入管理员名称及密码强制解锁", MessageType.MsgAlarm);
                }
            }
            else
            {
                if(!string.IsNullOrEmpty(this.textBox1.Text) && (this.textBox1.Text == this.textBox2.Text))
                {
                    this.pageLock = true;
                    this.pageLockPW = this.textBox1.Text;

                    this.textBox1.Text = this.textBox2.Text = "";
                    this.labelTip.Text = "请挂牌上锁！";
                    this.buttonLock.Text = "解除维护";
                }
                else
                {
                    ShowMsgBox.ShowDialog("密码为空或两次输入密码不一致，请重新输入", MessageType.MsgAlarm);
                }
            }
        }

        private void MaintenanceLockPage_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!this.pageClose)
            {
                e.Cancel = true;
            }
        }
    }
}
