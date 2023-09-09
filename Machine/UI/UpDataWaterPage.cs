using HelperLibrary;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SystemControlLibrary;

namespace Machine
{
    public partial class UpDataWaterPage : FormEx
    {
        public UpDataWaterPage()
        {
            InitializeComponent();

            //创建干燥炉列表
            CreateDryingOvenList();
        }

        #region // 界面

        private void CreateDryingOvenList()
        {
            for (RunID id = RunID.DryOven0; id < RunID.DryOvenALL; id++)
            {
                string name = "干燥炉  " + ((int)id - (int)RunID.DryOven0 + 1);
                this.comboBoxDryingID.Items.Add(name);
            }
            if (this.comboBoxDryingID.Items.Count > 0)
            {
                this.comboBoxDryingID.SelectedIndex = 0;
            }

            int tier = (int)OvenRowCol.MaxRow;
            for (int tierID = 0; tierID < tier; tierID++)
            {
                string name = (tierID + 1).ToString() + " 层 ";
                this.comboBoxTierID.Items.Add(name);
            }
            if (this.comboBoxTierID.Items.Count > 0)
            {
                this.comboBoxTierID.SelectedIndex = 0;
            }
            ToolTip tip = new ToolTip();
            tip.SetToolTip(this.textBoxWaterValue1, "只允许数字/小数点");
            tip.SetToolTip(this.textBoxWaterValue2, "只允许数字/小数点");
            tip.SetToolTip(this.textBoxUnbindPlt, "需要解绑的夹具条码\r\n分号;后可以添加解绑类型");
        }

        /// <summary>
        /// UI界面可见性发生改变
        /// </summary>
        /// <param name="show">是否在前台显示</param>
        public override void UIVisibleChanged(bool show)
        {
            if (show)
            {
                this.textBoxOperaterID.Text = MachineCtrl.GetInstance().OperaterID;
                this.textBoxOperaterID.ReadOnly = !string.IsNullOrEmpty(MachineCtrl.GetInstance().OperaterID);

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
            try
            {
                if((MCState.MCInitializing == mc) || (MCState.MCRunning == mc) || (level > UserLevelType.USER_MAINTENANCE))
                {
                    SetUIEnable(UIEnable.AllDisabled);
                }
                else if(level <= UserLevelType.USER_MAINTENANCE)
                {
                    SetUIEnable(UIEnable.AllEnabled);
                }
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("UpDataWaterPage .UpdataUIEnable()", ex.Message, LogType.Error);
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
                bool en = (uiEN == UIEnable.AllEnabled);

                this.buttonGetBillNo.Enabled = true;
                this.buttonParaUpdata.Enabled = en;
                this.buttonUnbindPlt.Enabled = en;
            }));
        }

        #endregion

        #region // 水含量

        /// <summary>
        /// 假电池搜索
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSearch_Click(object sender, EventArgs e)
        {                     
            if (string.IsNullOrWhiteSpace(this.textBoxBatCode.Text))
            {
                ShowMsgBox.ShowDialog("电池条码不能为空", MessageType.MsgWarning);
                return;
            }
            else
            {
                if (!SearchFakeBatPos())
                {
                    ShowMsgBox.ShowDialog("未搜索到电池条码位置", MessageType.MsgWarning);
                }
            }
        }

        /// <summary>
        /// 水含量上传
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonUpData_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.textBoxWaterValue1.Text)
                || string.IsNullOrWhiteSpace(this.textBoxWaterValue2.Text)
                || string.IsNullOrWhiteSpace(this.textBoxWaterValue3.Text))
            {
                ShowMsgBox.ShowDialog("请输入所有的水含量值！！！", MessageType.MsgWarning);
                return;
            }

            TextBox[] txtBox = new TextBox[] { this.textBoxWaterValue1, this.textBoxWaterValue2, this.textBoxWaterValue3 };
            double[] waterValue = new double[txtBox.Length];
            for(int i = 0; i < txtBox.Length; i++)
            {
                if (!double.TryParse(txtBox[i].Text, out waterValue[i]))
                {
                    ShowMsgBox.ShowDialog(string.Format("【{0}】不是正确的数字，请重新输入！", txtBox[i].Text), MessageType.MsgWarning);
                    return;
                }
            }

            int dryingID = this.comboBoxDryingID.SelectedIndex;
            int cavityIdx = this.comboBoxTierID.SelectedIndex;
            if (dryingID > -1 && cavityIdx > -1)
            {
                string msg = "";
                RunID runId = RunID.DryOven0 + dryingID;
                CavityStatus cavityState = MachineCtrl.GetInstance().GetDryingOvenCavityState(runId, cavityIdx);
                if (CavityStatus.WaitResult == cavityState)
                {
                    if (null == MachineCtrl.GetInstance().GetModule(runId) && !MachineCtrl.GetInstance().ClientIsConnect())
                    {
                        msg = $"干燥炉{dryingID + 1}】模组不在此设备，且模组未连接不能在此设备上传水含量";
                        ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                        return;
                    }
                    if (MachineCtrl.GetInstance().SetCavityWaterContent(runId, cavityIdx, waterValue))
                    {
                        if (!Def.IsNoHardware())
                        {
                            msg = $"干燥炉{dryingID + 1} - {cavityIdx + 1}层腔体水含量结果上传成功";
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgMessage);
                        }
                        foreach(var item in txtBox)
                        {
                            item.Clear();
                        }
                    }
                }
                else
                {
                    msg = $"干燥炉{dryingID + 1} - {cavityIdx + 1}层腔体非等待水含量结果状态，不能上传";
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                }
            }
        }

        /// <summary>
        /// 搜索假电池位置
        /// </summary>
        /// <returns></returns>
        private bool SearchFakeBatPos()
        {
            for (int modID = 0; modID < (int)OvenInfoCount.OvenCount; modID++)
            {
                Pallet[] ovenPlt = MachineCtrl.GetInstance().GetModulePallet(RunID.DryOven0 + modID);
                if(null != ovenPlt)
                {
                    for(int Pat = 0; Pat < ovenPlt.Length; Pat++)
                    {
                        int row, col;
                        row = col = -1;
                        if(ovenPlt[Pat].GetFakePos(ref row, ref col))
                        {
                            if(this.textBoxBatCode.Text == ovenPlt[Pat].Battery[row, col].Code)
                            {
                                string info = $"干燥炉{modID + 1}-{Pat / 2 + 1}层夹具{Pat % 2 + 1}-{row + 1}行{col + 1}列";
                                this.textBoxBatInfo.Text = info;

                                if(this.comboBoxDryingID.Items.Count >= modID)
                                {
                                    this.comboBoxDryingID.SelectedIndex = modID;
                                }
                                if(this.comboBoxTierID.Items.Count >= Pat / 2)
                                {
                                    this.comboBoxTierID.SelectedIndex = Pat / 2;
                                }
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 字符校验
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WaterValue_KeyPress(object sender, KeyPressEventArgs e)
        {
            // 数字 && 删除键 && 小数点
            if(!(Char.IsNumber(e.KeyChar)) && e.KeyChar != (char)8 && e.KeyChar != (char)46)
            {
                e.Handled = true;
            }
        }

        #endregion

        #region // 操作员工
        
        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOperaterLogin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.textBoxOperaterID.Text))
            {
                ShowMsgBox.ShowDialog("操作人员工号为空，不能登录！", MessageType.MsgWarning);
                return;
            }
            MachineCtrl.GetInstance().OperaterID = this.textBoxOperaterID.Text;
            this.textBoxOperaterID.ReadOnly = true;
        }

        /// <summary>
        /// 注销
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOperaterLogout_Click(object sender, EventArgs e)
        {
            MachineCtrl.GetInstance().OperaterID = "";
            this.textBoxOperaterID.Clear();
            this.textBoxOperaterID.ReadOnly = false;
        }

        #endregion

        #region // MES操作

        /// <summary>
        /// 工单获取
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonGetBillNo_Click(object sender, EventArgs e)
        {
            //string msg, billNo, billNum;
            //msg = billNo = billNum = "";
            //bool result = MachineCtrl.GetInstance().MesGetBillNO(out billNo, out billNum);
            //if (result)
            //{
            //    msg = $"工单获取成功\r\n工单号：{billNo}\r\n工单数量：{billNum}";
            //    ShowMsgBox.Show(msg, MessageType.MsgMessage);
            //}
        }

        /// <summary>
        /// 夹具解绑
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonUnbindPlt_Click(object sender, EventArgs e)
        {
            // 手动解绑
            string str = this.textBoxUnbindPlt.Text;
            string msg="";

            for (int i = 0; i < (MesData.mesFrequency == 0 ? 3 : MesData.mesFrequency); i++)
            {
                //托盘解绑
                if (!MachineCtrl.GetInstance().MesUnbindPalletInfo(str, MesResources.Heartbeat, ref msg))
                {
                    //获取报警字符串是否包含"超时"二字,如果没有则跳出循环
                    if (!msg.Contains("超时"))
                    {
                        break;
                    }
                    if (i == 2)
                    {
                        ShowMsgBox.ShowDialog($"MES夹具解绑接口超时失败3次，请检查MES连接状态", MessageType.MsgAlarm);
                    }
                }
                else
                {
                    ShowMsgBox.Show("夹具解绑成功",MessageType.MsgMessage);
                    break;
                }
            }
            //bool result = MachineCtrl.GetInstance().MesUnbindPalletInfo(str, MesResources.Heartbeat,);

            
        }

        /// <summary>
        /// 工艺参数获取
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonParaUpdata_Click(object sender, EventArgs e)
        {
            string msg = "";
            bool result = false;
            Dictionary<string, MesParameterStruct> getParam = new Dictionary<string, MesParameterStruct>();


            //MES超时重传三次
            for (int i = 0; i < (MesData.mesFrequency == 0 ? 3 : MesData.mesFrequency); i++)
            {
                //MES工艺参数获取
                if (!MachineCtrl.GetInstance().MesGetBillParameter(ref msg))
                {
                    //获取报警字符串是否包含"超时"二字,如果没有则跳出循环
                    if (!msg.Contains("超时"))
                    {
                        result = false;
                        break;
                    }
                    if (i == 2)
                    {
                        result = false;
                        ShowMsgBox.ShowDialog($"获取MES工艺参数超时失败3次，请检查MES连接状态", MessageType.MsgAlarm);
                    }
                }
                else
                {
                    result = true;
                    break;
                }
            }

            //bool result = MachineCtrl.GetInstance().MesGetBillParameter(getParam,ref msg);
            if (result)
            {
                msg = $"工艺参数获取成功\r\n";
                MesConfig mesCfg = MesDefine.GetMesCfg(MesInterface.ApplyTechProParam);
                if (null != mesCfg)
                {
                    foreach(var item in mesCfg.parameter)
                    {
                        //msg += $"{item.Key}：\r\ncode={item.Value.Code}\r\nname={item.Value.Name}\r\nunit={item.Value.Unit}\r\n";
                        //msg += $"upper={item.Value.Upper}\r\nvalue={item.Value.Value}\r\nlower={item.Value.Lower}\r\n";
                    }
                }
                ShowMsgBox.Show(msg, MessageType.MsgMessage);
            }
        }

        #endregion

        private void buttonGetBillInfoList_Click(object sender, EventArgs e)
        {
            if (!MesData.CodeRule)
            {
                MessageBox.Show("关");
            }
            else
            {
                MessageBox.Show("开");
            }
        }
    }
}

