using HelperLibrary;
using System;

namespace Machine
{
    public partial class OtherDebugPage : FormEx
    {
        public OtherDebugPage()
        {
            InitializeComponent();

            CreateScanList();
            CreatePalletModuleList();
        }

        #region // 界面

        private void OtherDebugPage_Load(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// 关闭窗口前销毁自定义非托管资源
        /// </summary>
        /// <returns></returns>
        public override void DisposeForm()
        {
        }

        /// <summary>
        /// UI界面可见性发生改变
        /// </summary>
        /// <param name="show">是否在前台显示</param>
        public override void UIVisibleChanged(bool show)
        {
            if (show)
            {
                UpdataUIEnable(MachineCtrl.GetInstance().RunsCtrl.GetMCState(), MachineCtrl.GetInstance().dbRecord.UserLevel());
            }
            else
            {
                UpdataUIEnable(SystemControlLibrary.MCState.MCRunning, SystemControlLibrary.UserLevelType.USER_LOGOUT);
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
            try
            {
                if((SystemControlLibrary.MCState.MCInitializing == mc) || (SystemControlLibrary.MCState.MCRunning == mc))
                {
                    SetUIEnable(UIEnable.AllDisabled);
                }
                else if(level <= SystemControlLibrary.UserLevelType.USER_MAINTENANCE)
                {
                    SetUIEnable(UIEnable.MaintenanceEnabled);
                }
                else
                {
                    SetUIEnable(UIEnable.OperatorEnabled);
                }
            }
            catch(System.Exception ex)
            {
                Def.WriteLog("DryingOvenPage.UpdataUIEnable()", ex.Message, LogType.Error);
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
            this.Invoke(new Action(()=>
            {
                switch(uiEN)
                {
                    case UIEnable.AllDisabled:
                        this.Enabled = false;
                        break;
                    case UIEnable.AllEnabled:
                    case UIEnable.AdminEnabled:
                    case UIEnable.MaintenanceEnabled:
                        this.Enabled = true;

                        this.buttonPalletAdd.Enabled = true;
                        this.buttonPalletClear.Enabled = true;
                        this.buttonPalletFull.Enabled = true;
                        this.buttonPalletNG.Enabled = true;
                        this.buttonResetEvent.Enabled = true;
                        this.buttonServerRestart.Enabled = false;
                        break;
                    case UIEnable.OperatorEnabled:
                        this.Enabled = true;

                        this.buttonPalletAdd.Enabled = false;
                        this.buttonPalletClear.Enabled = false;
                        this.buttonPalletFull.Enabled = false;
                        this.buttonPalletNG.Enabled = false;
                        this.buttonResetEvent.Enabled = false;
                        this.buttonServerRestart.Enabled = false;
                        break;
                    default:
                        break;
                }
            }));
        }

        #endregion

        #region // 扫码器

        private void CreateScanList()
        {
            RunProcess run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRecv);
            if(null != run)
            {
                this.comboBoxScanChose.Items.Add(run.RunName + " - 扫码器1");
                this.comboBoxScanChose.Items.Add(run.RunName + " - 扫码器2");
            }
            run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot);
            if(null != run)
            {
                this.comboBoxScanChose.Items.Add(run.RunName + " - 扫码器");
            }
            if(this.comboBoxScanChose.Items.Count > 0)
            {
                this.comboBoxScanChose.SelectedIndex = 0;
            }
        }

        private void comboBoxScanChose_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            int idx = this.comboBoxScanChose.SelectedIndex;
            if (idx < 2)
            {
                var run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRecv) as RunProcessOnloadRecv;
                if (null != run)
                {
                    this.labelScanAdder.Text = run.ScanAdderInfo(idx);
                    this.labelScanConState.Text = run.ScanIsConnect(idx) ? "已连接" : "断开";
                }
            }
            else
            {
                var run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
                if(null != run)
                {
                    this.labelScanAdder.Text = run.ScanAdderInfo();
                    this.labelScanConState.Text = run.ScanIsConnect() ? "已连接" : "断开";
                }
            }
        }

        private void buttonScanConnect_Click(object sender, System.EventArgs e)
        {
            int idx = this.comboBoxScanChose.SelectedIndex;
            if(idx < 2)
            {
                var run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRecv) as RunProcessOnloadRecv;
                if(null != run)
                {
                    if (!run.ScanConnect(idx, true))
                    {
                        ShowMsgBox.Show("连接失败", MessageType.MsgMessage);
                    }
                    this.labelScanAdder.Text = run.ScanAdderInfo(idx);
                    this.labelScanConState.Text = run.ScanIsConnect(idx) ? "已连接" : "断开";
                }
            }
            else
            {
                var run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
                if(null != run)
                {
                    if (!run.ScanConnect(true))
                    {
                        ShowMsgBox.Show("连接失败", MessageType.MsgMessage);
                    }
                    this.labelScanAdder.Text = run.ScanAdderInfo();
                    this.labelScanConState.Text = run.ScanIsConnect() ? "已连接" : "断开";
                }
            }
        }

        private void buttonScanDisconnect_Click(object sender, System.EventArgs e)
        {
            int idx = this.comboBoxScanChose.SelectedIndex;
            if(idx < 2)
            {
                var run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRecv) as RunProcessOnloadRecv;
                if(null != run)
                {
                    if (!run.ScanConnect(idx, false))
                    {
                        ShowMsgBox.Show("断开失败", MessageType.MsgMessage);
                    }
                    //this.labelScanAdder.Text = run.ScanAdderInfo(idx);
                    this.labelScanConState.Text = run.ScanIsConnect(idx) ? "已连接" : "断开";
                }
            }
            else
            {
                var run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
                if(null != run)
                {
                    if (!run.ScanConnect(false))
                    {
                        ShowMsgBox.Show("断开失败", MessageType.MsgMessage);
                    }
                    //this.labelScanAdder.Text = run.ScanAdderInfo();
                    this.labelScanConState.Text = run.ScanIsConnect() ? "已连接" : "断开";
                }
            }
        }

        private void buttonScanCode_Click(object sender, System.EventArgs e)
        {
            int idx = this.comboBoxScanChose.SelectedIndex;
            if(idx < 2)
            {
                var run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRecv) as RunProcessOnloadRecv;
                if(null != run)
                {
                    if (run.ScanCode(idx))
                    {
                        string code = "";
                        run.GetScanResult(idx, ref code);
                        this.textBoxCodeData.Text = code;
                    }
                }
            }
            else
            {
                var run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
                if(null != run)
                {
                    if(run.ScanCode())
                    {
                        string code = "";
                        run.GetScanResult(ref code);
                        this.textBoxCodeData.Text = code;
                    }
                }
            }
        }

        #endregion

        #region // 模组通讯服务

        private void buttonServerRestart_Click(object sender, System.EventArgs e)
        {
            MachineCtrl.GetInstance().CreateServer();
        }

        private void buttonClientReconnect_Click(object sender, System.EventArgs e)
        {
            if(MachineCtrl.GetInstance().ConnectClient())
            {
                ShowMsgBox.ShowDialog("模组通讯客户端重连服务器成功", MessageType.MsgMessage);
            }
        }

        #endregion

        #region // 添加删除夹具

        private void CreatePalletModuleList()
        {
            string modName = "";
            RunID[] runId = new RunID[]
            {
                RunID.OnloadRobot,
                RunID.PalletBuffer,
                RunID.ManualOperate,
                RunID.Transfer,
                RunID.OffloadBattery,
            };
            RunProcess run = null;
            foreach(var id in runId)
            {
                run = MachineCtrl.GetInstance().GetModule(id);
                if (null != run)
                {
                    modName = $"{run.GetRunID()}:{run.RunName}";
                    this.comboBoxPalletModule.Items.Add(modName);
                    this.comboBoxResetEvent.Items.Add(modName);
                }
            }
            for(RunID id = RunID.DryOven0; id < RunID.DryOvenALL; id++)
            {
                run = MachineCtrl.GetInstance().GetModule(id);
                if(null != run)
                {
                    modName = $"{run.GetRunID()}:{run.RunName}";
                    this.comboBoxPalletModule.Items.Add(modName);
                    this.comboBoxResetEvent.Items.Add(modName);
                }
            }
        }

        private void comboBoxPalletModule_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if(txt.Length > 0)
                {
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if (null != run)
                    {
                        this.comboBoxPalletID.Items.Clear();
                        int pltLen = run.Pallet.Length;
                        for(int i = 0; i < pltLen; i++)
                        {
                            this.comboBoxPalletID.Items.Add((i + 1).ToString());
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("comboBoxPalletModule_SelectedIndexChanged() error : " + ex.Message);
            }
        }

        private void buttonPalletAdd_Click(object sender, System.EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if(txt.Length > 0)
                {
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if(null != run)
                    {
                        int pltIdx = this.comboBoxPalletID.SelectedIndex;
                        if (pltIdx > -1)
                        {
                            run.ManualAddPallet(pltIdx, MachineCtrl.GetInstance().PalletMaxRow, MachineCtrl.GetInstance().PalletMaxCol, PalletStatus.OK, BatteryStatus.Invalid);
                            Def.WriteLog("OtherDebugPage", string.Format("{0}：添加空夹具{1}", run.RunName, (pltIdx + 1)));
                        }
                        else
                        {
                            ShowMsgBox.ShowDialog("夹具索引无效，添加失败", MessageType.MsgWarning);
                        }
                    }
                }
            }
            catch(System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("buttonPalletAdd_Click() error : " + ex.Message);
            }
        }

        private void buttonPalletFull_Click(object sender, EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if(txt.Length > 0)
                {
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if(null != run)
                    {
                        int pltIdx = this.comboBoxPalletID.SelectedIndex;
                        if(pltIdx > -1)
                        {
                            run.ManualAddPallet(pltIdx, MachineCtrl.GetInstance().PalletMaxRow, MachineCtrl.GetInstance().PalletMaxCol, PalletStatus.OK, BatteryStatus.OK);
                            Def.WriteLog("OtherDebugPage", string.Format("{0}：置OK电池{1}", run.RunName, (pltIdx + 1)));
                        }
                        else
                        {
                            ShowMsgBox.ShowDialog("夹具索引无效，添加失败", MessageType.MsgWarning);
                        }
                    }
                }
            }
            catch(System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("buttonPalletAdd_Click() error : " + ex.Message);
            }
        }

        private void buttonPalletClear_Click(object sender, System.EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if(txt.Length > 0)
                {
                    string msg = "删除夹具后必须手动将夹具从系统中插出来并拿走夹具中的电池！且无法添加电池数据！\r\n请确认是否删除夹具！";
                    if (System.Windows.Forms.DialogResult.Yes != ShowMsgBox.ShowDialog(msg, MessageType.MsgQuestion))
                    {
                        return;
                    }
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if(null != run)
                    {
                        int pltIdx = this.comboBoxPalletID.SelectedIndex;
                        if(pltIdx > -1)
                        {
                            run.ManualClearPallet(pltIdx);
                            Def.WriteLog("OtherDebugPage", string.Format("{0}：删除夹具{1}", run.RunName, (pltIdx + 1)));
                        }
                        else
                        {
                            ShowMsgBox.ShowDialog("夹具索引无效，删除失败", MessageType.MsgWarning);
                        }
                    }
                }
            }
            catch(System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("buttonPalletClear_Click() error : " + ex.Message);
            }
        }

        private void buttonPalletNG_Click(object sender, EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if(txt.Length > 0)
                {
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if(null != run)
                    {
                        int pltIdx = this.comboBoxPalletID.SelectedIndex;
                        if(pltIdx > -1)
                        {
                            run.ManualAddPallet(pltIdx, MachineCtrl.GetInstance().PalletMaxRow, MachineCtrl.GetInstance().PalletMaxCol, PalletStatus.NG, BatteryStatus.OK);
                            Def.WriteLog("OtherDebugPage", string.Format("{0}：置NG转盘夹具{1}", run.RunName, (pltIdx + 1)));
                        }
                        else
                        {
                            ShowMsgBox.ShowDialog("夹具索引无效，添加失败", MessageType.MsgWarning);
                        }
                    }
                }
            }
            catch(System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("buttonPalletAdd_Click() error : " + ex.Message);
            }
        }

        #endregion

        #region // 模组信号重置

        private void buttonResetEvent_Click(object sender, EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxResetEvent.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if(txt.Length > 0)
                {
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if(null != run)
                    {
                        run.ResetModuleEvent();
                    }
                }
            }
            catch(System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("buttonPalletAdd_Click() error : " + ex.Message);
            }
        }
        #endregion

    }
}
