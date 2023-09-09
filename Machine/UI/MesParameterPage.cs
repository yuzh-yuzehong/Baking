using HelperLibrary;
using System;
using System.Windows.Forms;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    public partial class MesParameterPage : FormEx
    {
        #region // 字段

        MesInterface mesInterface;
        System.Timers.Timer timerUpdata;        // 界面更新定时器

        #endregion

        public MesParameterPage()
        {
            InitializeComponent();

            CreateParameterView();
        }

        #region // 加载及销毁窗体

        private void MesParameterPage_Load(object sender, EventArgs e)
        {
            // 开启定时器
            this.timerUpdata = new System.Timers.Timer();
            this.timerUpdata.Elapsed += TimerUpdata_MesInfo;
            this.timerUpdata.Interval = 500;         // 间隔时间
            this.timerUpdata.AutoReset = true;       // 设置是执行一次（false）还是一直执行(true)；
            this.timerUpdata.Start();                // 开始执行定时器
        }

        /// <summary>
        /// 释放线程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void DisposeForm()
        {
            try
            {
                // 关闭定时器
                if(null != this.timerUpdata)
                {
                    this.timerUpdata.Stop();
                }
            }
            catch(System.Exception ex)
            {
                Def.WriteLog("MesParameterPage.DisposeForm()", $"error {ex.Message}\r\n{ex.StackTrace}", HelperLibrary.LogType.Error);
            }
        }

        /// <summary>
        /// UI界面可见性发生改变
        /// </summary>
        /// <param name="show">是否在前台显示</param>
        public override void UIVisibleChanged(bool show)
        {
            if (null != this.timerUpdata)
            {
                if (show)
                {
                    UpdataMesConfig(this.mesInterface);      // 加载参数
                    this.timerUpdata.Start();                // 开始执行定时器
                    UpdataUIEnable(MachineCtrl.GetInstance().RunsCtrl.GetMCState(), MachineCtrl.GetInstance().dbRecord.UserLevel());
                    //UpdataUIEnable(MachineCtrl.GetInstance().RunsCtrl.GetMCState());
                }
                else
                {
                    this.timerUpdata.Stop();
                    SetUIEnable(UIEnable.AllDisabled);
                }
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
                else if (level < UserLevelType.USER_MAINTENANCE || (!user.userName.Contains("MES")))
                {
                    SetUIEnable(UIEnable.AllEnabled);
                }
            }
            catch(System.Exception ex)
            {
                Def.WriteLog($"{this.mesInterface}.MesParameterPage.UpdataUIEnable()", ex.Message, LogType.Error);
            }
            base.UpdataUIEnable(mc, level);
        }

        public void UpdataUIEnable(SystemControlLibrary.MCState mc)
        {
            UserFormula user = new UserFormula();
            MachineCtrl.GetInstance().dbRecord.GetCurUser(ref user);
            try
            {
                if((MCState.MCInitializing == mc) || (MCState.MCRunning == mc) 
                    || string.IsNullOrEmpty(user.userName) || (!user.userName.Equals("MES", StringComparison.OrdinalIgnoreCase)))
                {
                    SetUIEnable(UIEnable.AllDisabled);
                }
                else if(!string.IsNullOrEmpty(user.userName) && user.userName.Equals("MES", StringComparison.OrdinalIgnoreCase))
                {
                    SetUIEnable(UIEnable.AllEnabled);
                }
            }
            catch(System.Exception ex)
            {
                Def.WriteLog($"{this.mesInterface}.MesParameterPage.UpdataUIEnable()", ex.Message, LogType.Error);
            }
        }

        #endregion

        #region // 界面使能

        /// <summary>
        /// 设置界面控件使能
        /// </summary>
        /// <param name="uiEN"></param>
        private void SetUIEnable(UIEnable uiEN)
        {
            this.Invoke(new Action(() =>
            {
                bool en = (uiEN == UIEnable.AllEnabled);

                this.checkBoxEnable.Enabled = en;
                this.textBoxUri.Enabled = en;
                this.dataGridViewParameter.Enabled = en;
                this.buttonSave.Enabled = en;
            }));
        }

        private void CreateParameterView()
        {
            // 设置表格
            DataGridViewNF[] dgv = new DataGridViewNF[] { this.dataGridViewParameter };
            for(int i = 0; i < dgv.Length; i++)
            {
                dgv[i].SetViewStatus();
                dgv[i].ReadOnly = false;
                dgv[i].AllowUserToAddRows = true;         // 可以添加行
                dgv[i].AllowUserToDeleteRows = true;      // 可以删除行
                dgv[i].EditMode = DataGridViewEditMode.EditOnEnter;
                // 项
                DataGridViewTextBoxColumn txtBoxCol = new DataGridViewTextBoxColumn();
                txtBoxCol.HeaderText = "参数代码";
                int idx = dgv[i].Columns.Add(txtBoxCol);
                //dgv[i].Columns[idx].FillWeight = 50;     // 宽度占比权重
                txtBoxCol = new DataGridViewTextBoxColumn();
                txtBoxCol.HeaderText = "参数名称";
                idx = dgv[i].Columns.Add(txtBoxCol);
                //dgv[i].Columns[idx].FillWeight = 50;
                txtBoxCol = new DataGridViewTextBoxColumn();
                txtBoxCol.HeaderText = "参数单位";
                idx = dgv[i].Columns.Add(txtBoxCol);
                //dgv[i].Columns[idx].FillWeight = 50;
                txtBoxCol = new DataGridViewTextBoxColumn();
                txtBoxCol.HeaderText = "参数设定值上限";
                idx = dgv[i].Columns.Add(txtBoxCol);
                //dgv[i].Columns[idx].FillWeight = 50;
                txtBoxCol = new DataGridViewTextBoxColumn();
                txtBoxCol.HeaderText = "参数设定值";
                idx = dgv[i].Columns.Add(txtBoxCol);
                //dgv[i].Columns[idx].FillWeight = 50;
                txtBoxCol = new DataGridViewTextBoxColumn();
                txtBoxCol.HeaderText = "参数设定值下限";
                idx = dgv[i].Columns.Add(txtBoxCol);
                //dgv[i].Columns[idx].FillWeight = 50;
                //txtBoxCol = new DataGridViewTextBoxColumn();
                //txtBoxCol.HeaderText = "映射的程序参数名";
                //idx = dgv[i].Columns.Add(txtBoxCol);
                //dgv[i].Columns[idx].FillWeight = 50;
                foreach(DataGridViewColumn item in dgv[i].Columns)
                {
                    item.SortMode = DataGridViewColumnSortMode.NotSortable;     // 禁止列排序
                }
            }
        }

        #endregion

        #region // 界面数据

        public void SetInterface(MesInterface mes)
        {
            this.mesInterface = mes;
        }

        private void UpdataMesConfig(MesInterface mes)
        {
            MesConfig cfg = MesDefine.GetMesCfg(mes);
            if (null != cfg)
            {
                this.BeginInvoke(new Action(() =>
                {
                    this.checkBoxEnable.Checked = cfg.enable;
                    this.textBoxUri.Text = cfg.mesUri;
                    if (cfg.parameter.Count > 0)
                    {
                        this.dataGridViewParameter.Rows.Clear();
                        // 0406 注释
                        //foreach(var item in cfg.parameter.Values)
                        //{
                        //    int index = this.dataGridViewParameter.Rows.Add();
                        //    this.dataGridViewParameter.Rows[index].Height = 25;        // 行高度
                        //    int cellIdx = 0;
                        //    this.dataGridViewParameter.Rows[index].Cells[cellIdx++].Value = item.Code;
                        //    this.dataGridViewParameter.Rows[index].Cells[cellIdx++].Value = item.Name;
                        //    this.dataGridViewParameter.Rows[index].Cells[cellIdx++].Value = item.Unit;
                        //    this.dataGridViewParameter.Rows[index].Cells[cellIdx++].Value = item.Upper;
                        //    this.dataGridViewParameter.Rows[index].Cells[cellIdx++].Value = item.Value;
                        //    this.dataGridViewParameter.Rows[index].Cells[cellIdx++].Value = item.Lower;
                        //    this.dataGridViewParameter.Rows[index].Cells[cellIdx++].Value = item.Key;
                        //}
                    }
                }));
            }
        }

        private void TimerUpdata_MesInfo(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (this.Visible)
                {
                    UpdataMesData();
                }
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("MesParameterPage.TimerUpdata_MesInfo()", ex.ToString());
            }
        }

        private void checkBoxMesEnable_CheckedChanged(object sender, EventArgs e)
        {
            this.checkBoxEnable.Text = this.checkBoxEnable.Checked ? "接口启用" : "接口停用";
            MesConfig cfg = MesDefine.GetMesCfg(this.mesInterface);
            if(null != cfg)
            {
                cfg.enable = this.checkBoxEnable.Checked;
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            MesConfig cfg = new MesConfig();
            cfg.enable = this.checkBoxEnable.Checked;
            cfg.mesUri = this.textBoxUri.Text;
            cfg.parameterDate = DateTime.Now.ToBinary();

            // 0407 注释
            //DataGridViewNF dgv = this.dataGridViewParameter;
            //for(int i = 0; i < dgv.RowCount; i++)
            //{
            //    int col = 0;
            //    MesParameterStruct param = new MesParameterStruct();
            //    param.Code = (null != dgv.Rows[i].Cells[col].Value) ? dgv.Rows[i].Cells[col].Value.ToString() : "";
            //    col++;
            //    param.Name = (null != dgv.Rows[i].Cells[col].Value) ? dgv.Rows[i].Cells[col].Value.ToString() : "";
            //    col++;
            //    param.Unit = (null != dgv.Rows[i].Cells[col].Value) ? dgv.Rows[i].Cells[col].Value.ToString() : "";
            //    col++;
            //    param.Upper = (null != dgv.Rows[i].Cells[col].Value) ? dgv.Rows[i].Cells[col].Value.ToString() : "";
            //    col++;
            //    param.Value = (null != dgv.Rows[i].Cells[col].Value) ? dgv.Rows[i].Cells[col].Value.ToString() : "";
            //    col++;
            //    param.Lower = (null != dgv.Rows[i].Cells[col].Value) ? dgv.Rows[i].Cells[col].Value.ToString() : "";
            //    col++;
            //    param.Key = (null != dgv.Rows[i].Cells[col].Value) ? dgv.Rows[i].Cells[col].Value.ToString() : "";
            //    col++;
            //    if(string.IsNullOrEmpty(param.Key) || string.IsNullOrEmpty(param.Code))
            //    {
            //        continue;
            //    }
            //    if(cfg.parameter.ContainsKey(param.Code))
            //    {
            //        cfg.parameter[param.Code] = param;
            //        ShowMsgBox.ShowDialog($"{dgv.Columns[0].HeaderText}参数【{param.Code}】重复，请检查！", MessageType.MsgAlarm);
            //    }
            //    else
            //    {
            //        cfg.parameter.Add(param.Code, param);
            //    }
            //}
            MesDefine.GetMesCfg(this.mesInterface).Copy(cfg);
            MesDefine.WriteConfig(this.mesInterface);
        }

        /// <summary>
        /// 更新MES数据
        /// </summary>
        private void UpdataMesData()
        {
            MesConfig cfg = MesDefine.GetMesCfg(this.mesInterface);
            if(null != cfg)
            {
                this.BeginInvoke(new Action(() =>
                {
                    if(cfg.updataRS)
                    {
                        this.textBoxSend.Text = $"{DateTime.Now.ToString(Def.DateFormal)}上传：\r\n\r\n{cfg.send}";
                        this.textBoxRecv.Text = $"{DateTime.Now.ToString(Def.DateFormal)}接收：\r\n\r\n{cfg.recv}";
                        cfg.updataRS = false;
                    }
                }));
            }
        }

        #endregion

    }
}
