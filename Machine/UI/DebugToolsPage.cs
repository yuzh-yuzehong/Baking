using System;
using System.Drawing;
using System.Windows.Forms;

namespace Machine
{
    public partial class DebugToolsPage : FormEx
    {
        public DebugToolsPage()
        {
            InitializeComponent();

            CreateTabPage();
        }

        private void DebugToolsPage_Load(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// 销毁自定义非托管资源
        /// </summary>
        public override void DisposeForm()
        {
            foreach(Control tab in this.tabControl.Controls)
            {
                foreach(Control form in tab.Controls)
                {
                    if (form is FormEx)
                    {
                        ((FormEx)form).DisposeForm();
                    }
                }
            }
        }

        /// <summary>
        /// 当设备状态或用户权限改变时，更新UI界面的使能
        /// </summary>
        /// <param name="enable"></param>
        public override void UpdataUIEnable(SystemControlLibrary.MCState mc, SystemControlLibrary.UserLevelType level)
        {
            TabPage tp = this.tabControl.SelectedTab;
            foreach(Control form in tp.Controls)
            {
                if(form is FormEx)
                {
                    ((FormEx)form).UpdataUIEnable(mc, level);
                }
            }
        }

        private void CreateTabPage()
        {
            Form form = new RobotPage();
            form.TopLevel = false;
            form.Dock = DockStyle.Fill;
            form.Show();
            this.tabPageRobot.Controls.Add(form);

            form = new DryingOvenPage();
            form.TopLevel = false;
            form.Dock = DockStyle.Fill;
            form.Show();
            this.tabPageDryingOven.Controls.Add(form);

            form = new OtherDebugPage();
            form.TopLevel = false;
            form.Dock = DockStyle.Fill;
            form.Show();
            this.tabPageOther.Controls.Add(form);

            foreach(Control item in this.tabControl.Controls)
            {
                item.BackColor = Color.Transparent;
            }
        }

        private void DebugToolsPage_VisibleChanged(object sender, EventArgs e)
        {
            tabControl_SelectedIndexChanged(sender, e);
        }

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            TabPage tp = this.tabControl.SelectedTab;
            for(int i = 0; i < this.tabControl.Controls.Count; i++)
            {
                foreach(Control form in this.tabControl.Controls[i].Controls)
                {
                    ((FormEx)form).UIVisibleChanged(tp.Controls.Contains(form));
                }
            }
        }
    }
}
