using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Machine
{
    public partial class ModuleMonitorPage : FormEx
    {
        public ModuleMonitorPage()
        {
            InitializeComponent();

            // 创建模组监视表
            CreateModuleListView();
        }

        // 定时器
        System.Timers.Timer timerUpdata;

        /// <summary>
        /// 加载界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ModuleMonitorPage_Load(object sender, EventArgs e)
        {
            // 开启定时器
            timerUpdata = new System.Timers.Timer();
            timerUpdata.Elapsed += UpdateModuleState;
            timerUpdata.Interval = 200;         // 间隔时间
            timerUpdata.AutoReset = true;       // 设置是执行一次（false）还是一直执行(true)；
            timerUpdata.Start();                // 开始执行定时器
        }

        /// <summary>
        /// 销毁自定义非托管资源
        /// </summary>
        public override void DisposeForm()
        {
            if (null != this.timerUpdata)
            {
                // 关闭定时器
                this.timerUpdata.Stop();
            }
        }

        /// <summary>
        /// 界面隐藏时停止更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ModuleMonitorPage_VisibleChanged(object sender, EventArgs e)
        {
            if(null != this.timerUpdata)
            {
                if(this.Visible)
                {
                    this.timerUpdata.Start();
                }
                else
                {
                    this.timerUpdata.Stop();
                }
            }
        }

        /// <summary>
        /// 创建模组监视表
        /// </summary>
        private void CreateModuleListView()
        {
            // 设置表格
            DataGridViewNF[] dgv = new DataGridViewNF[] { this.dataGridViewModule };
            for(int i = 0; i < dgv.Length; i++)
            {
                dgv[i].SetViewStatus();
                // 项
                int idx = dgv[i].Columns.Add("id", "序号");
                dgv[i].Columns[idx].FillWeight = 5;     // 宽度占比权重
                idx = dgv[i].Columns.Add("name", "模组名称");
                dgv[i].Columns[idx].FillWeight = 20;
                idx = dgv[i].Columns.Add("runMsg", "运行信息");
                dgv[i].Columns[idx].FillWeight = 45;
                idx = dgv[i].Columns.Add("runState", "运行状态");
                dgv[i].Columns[idx].FillWeight = 10;
                idx = dgv[i].Columns.Add("modState", "模组状态");
                dgv[i].Columns[idx].FillWeight = 10;
                idx = dgv[i].Columns.Add("ctTime", "CT时间");
                dgv[i].Columns[idx].FillWeight = 10;
                foreach(DataGridViewColumn item in dgv[i].Columns)
                {
                    item.SortMode = DataGridViewColumnSortMode.NotSortable;     // 禁止列排序
                }
            }
            for(int i = 0; i < MachineCtrl.GetInstance().ListRuns.Count; i++)            // 添加行数据
            {
                int index = this.dataGridViewModule.Rows.Add();
                this.dataGridViewModule.Rows[index].Height = 35;        // 行高度
                this.dataGridViewModule.Rows[index].Cells[0].Value = (i + 1).ToString();
                this.dataGridViewModule.Rows[index].Cells[1].Value = MachineCtrl.GetInstance().ListRuns[i].RunName;
            }
        }

        /// <summary>
        /// 更新表格中模组状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateModuleState(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                // 使用委托更新UI
                Action<List<RunProcess>> uiDelegate = delegate (List<RunProcess> listRun)
                {
                    if(null != listRun)
                    {
                        string info = "";
                        for(int i = 0; i < listRun.Count; i++)
                        {
                            this.dataGridViewModule.Rows[i].Cells[2].Value = listRun[i].RunMsg;
                            info = listRun[i].IsRunning() ? "运行中" : "停止";
                            this.dataGridViewModule.Rows[i].Cells[3].Value = info;
                            this.dataGridViewModule.Rows[i].Cells[3].Style.ForeColor = listRun[i].IsRunning() ? Color.Black : Color.Red;
                            info = listRun[i].IsModuleEnable() ? (listRun[i].DryRun ? "空运行" : "使能") : "禁用";
                            this.dataGridViewModule.Rows[i].Cells[4].Value = info;
                            this.dataGridViewModule.Rows[i].Cells[4].Style.ForeColor = listRun[i].IsModuleEnable() ? (listRun[i].DryRun ? Color.Red : Color.Black) : Color.Red;
                            info = listRun[i].ModuleUseTime.ToString("#0.000");
                            this.dataGridViewModule.Rows[i].Cells[5].Value = info;
                        }
                    }
                };
                this.Invoke(uiDelegate, MachineCtrl.GetInstance().ListRuns);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("ModuleMonitorPage.UpdateModuleState " + ex.Message);
            }
        }
    }
}
