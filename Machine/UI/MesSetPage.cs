using Machine.UI;
using System.Drawing;
using System.Windows.Forms;

namespace Machine
{
    public partial class MesSetPage : FormEx
    {
        public MesSetPage()
        {
            InitializeComponent();

            CreateTabPage();
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
                    if(form is FormEx)
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
            // 水含量上传界面
            FormEx form = new UpDataWaterPage();
            form.TopLevel = false;
            form.Dock = DockStyle.Fill;
            form.Show();
            this.tabPageWaterContent.Controls.Add(form);

            // MES数据界面
            form = new MesDataPage();
            form.TopLevel = false;
            form.Dock = DockStyle.Fill;
            form.Show();
            this.tabPageMesData.Controls.Add(form);

            // Mes信息设置界面
            form = new MesInfoPage();
            form.TopLevel = false;
            form.Dock = DockStyle.Fill;
            form.Show();
            this.tabPageMesInfo.Controls.Add(form);
            // Mes参数
            for(MesInterface mes = 0; mes < MesInterface.End; mes++)
            {
                TabPage tab = new TabPage();
                tab.Location = new System.Drawing.Point(4, 39);
                tab.Margin = new System.Windows.Forms.Padding(4);
                tab.Name = "tabPage" + mes.ToString();
                tab.Padding = new System.Windows.Forms.Padding(4);
                tab.Size = new System.Drawing.Size(421, 218);
                tab.Text = MesDefine.GetMesTitle(mes);
                tab.UseVisualStyleBackColor = true;

                form = new MesParameterPage();
                ((MesParameterPage)form).SetInterface(mes);
                form.TopLevel = false;
                form.Dock = DockStyle.Fill;
                form.Show();
                tab.Controls.Add(form);
                this.tabControl.Controls.Add(tab);
            }

            foreach (Control item in this.tabControl.Controls)
            {
                item.BackColor = Color.Transparent;
            }
        }

        private void MesSetPage_VisibleChanged(object sender, System.EventArgs e)
        {
            tabControl_SelectedIndexChanged(sender, e);
        }

        private void tabControl_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            TabPage tb = this.tabControl.SelectedTab;
            for(int i = 0; i < this.tabControl.Controls.Count; i++)
            {
                foreach(Control form in this.tabControl.Controls[i].Controls)
                {
                    ((FormEx)form).UIVisibleChanged(tb.Controls.Contains(form));
                }
            }
        }
    }
}

    
