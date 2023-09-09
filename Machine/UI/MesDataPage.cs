using HelperLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SystemControlLibrary;
using static Machine.MesBill;
using static Machine.RunProcessDryingOven;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine.UI
{
    public partial class MesDataPage : FormEx
    {
        public MesDataPage()
        {
            InitializeComponent();

            CreatePara();
        }
        // 定时器
        System.Timers.Timer timerUpdata;

        List<string> OutBankData;
        string filePath;
        string copy_filePath;
        string formulaNo;    
        RunProcess run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRecv);
        RunProcess runProcess = new RunProcess(Convert.ToInt32(RunID.OnloadRecv));

        //MesData mesData = new MesData();


        private void MesDataPage_Load(object sender, EventArgs e)
        {
            this.dataGridViewIn.Columns.Add("BatteryCode", "电芯条码");
            this.dataGridViewIn.Columns.Add("billNo", "工单");
            this.dataGridViewIn.Columns.Add("Result", "进站结果");
            this.dataGridViewIn.Columns.Add("ResultMsg", "结果说明");
            this.dataGridViewIn.Columns.Add("Time", "时间");

            this.dataGridViewOut.Columns.Add("BatteryCode", "电芯条码");
            this.dataGridViewOut.Columns.Add("billNo", "工单");
            this.dataGridViewOut.Columns.Add("Result", "出站结果");
            this.dataGridViewOut.Columns.Add("ResultMsg", "结果说明");
            this.dataGridViewOut.Columns.Add("Time", "时间");

            //隐藏工艺参数控件
            this.tabPage4.Hide();
            tabControl1.TabPages.Remove(tabPage4);

            // 开启定时器
            this.timerUpdata = new System.Timers.Timer();
            //this.timerUpdata.Elapsed += TimerUpdata_MesInfo;
            timerUpdata.Elapsed += UpdateModuleState1;
            timerUpdata.Elapsed += UpdateModuleState2;
            MesData.ReadApplyTechTime();
            this.timerUpdata.Interval = 1000;         // 间隔时间
            this.timerUpdata.AutoReset = true;       // 设置是执行一次（false）还是一直执行(true)；
            this.timerUpdata.Start();                // 开始执行定时器

            this.textBoxEquipmentCode.Text = MesResources.Onload.EquipmentID;
            this.textBoxProcessCode.Text = MesResources.Onload.ProcessID;
            this.textBoxBillNo.Text = MesResources.BillNo;
            this.textBoxBillNum.Text = MesResources.BillNum;
            this.textBox1.Text = MesResources.Group.EquipmentID;
            this.textBox2.Text = MesResources.Group.ProcessID;
            this.txtTechProParamFormalVerify.Text = MesDefine.GetMesCfg(MesInterface.TechProParamFormalVerify).mesUri;  //配方效验
            this.txtEPTechProParamFormalVerify.Text = MesDefine.GetMesCfg(MesInterface.EPTechProParamFormalVerify).mesUri;  //设备参数效验
            this.txtGetBillInfo.Text = MesDefine.GetMesCfg(MesInterface.GetBillInfo).mesUri; //获取工单信息
            this.txtGetBillInfoList.Text = MesDefine.GetMesCfg(MesInterface.GetBillInfoList).mesUri;  //获取工单队列
            this.txtTrayVerifity.Text = MesDefine.GetMesCfg(MesInterface.TrayVerifity).mesUri;  //托盘效验
            this.txtCellVerifity.Text = MesDefine.GetMesCfg(MesInterface.BakingMaterialVerifity).mesUri; //电芯进站效验
            this.txtBindCellToTray.Text = MesDefine.GetMesCfg(MesInterface.SaveTrayAndBarcodeRecord).mesUri;  //绑盘信息上传
            this.txtBakingResult.Text = MesDefine.GetMesCfg(MesInterface.SaveBakingResultRecord).mesUri;   //baking开始结束信息上传
            this.txtProductRecordList.Text = MesDefine.GetMesCfg(MesInterface.SavePR_ProductRecordList).mesUri;  //baking上传履历记录
            this.txtSaveUnBindTrayResult.Text = MesDefine.GetMesCfg(MesInterface.TrayUnbundlingRecord).mesUri;   //托盘解绑
            this.txtApplyTechTime.Text = MesData.MesApplyTechTime.ToString();


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
                    //MesDefine.ReadConfig(MesInterface.ApplyTechProParam);
                    UpdataMesConfig();      // 加载参数
                    UpdataMesParam(MesInterface.ApplyTechProParam);
                    this.timerUpdata.Start();                // 开始执行定时器
                    //UpdataUIEnable(MachineCtrl.GetInstance().RunsCtrl.GetMCState());
                    UpdataUIEnable(MachineCtrl.GetInstance().RunsCtrl.GetMCState(), MachineCtrl.GetInstance().dbRecord.UserLevel());
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
                //UpdataUIEnable(mc);
                if ((MCState.MCInitializing == mc) || (MCState.MCRunning == mc) || (level >= UserLevelType.USER_MAINTENANCE)|| (!user.userName.Contains("MES")) /*(!user.userName.Equals("MES", StringComparison.OrdinalIgnoreCase))*/)
                {
                    SetUIEnable(UIEnable.AllDisabled);
                }
                //else if (level < UserLevelType.USER_MAINTENANCE || (user.userName.Equals("MES", StringComparison.OrdinalIgnoreCase)))

                else if (level < UserLevelType.USER_MAINTENANCE || (user.userName.Contains("MES")))
                {
                    SetUIEnable(UIEnable.AllEnabled);
                }
            }
            catch (System.Exception ex)
            {
                Def.WriteLog($"GetBillInfoList.MesParameterPage.UpdataUIEnable()", ex.Message, LogType.Error);
            }
            base.UpdataUIEnable(mc, level);
        }

        public void UpdataUIEnable(SystemControlLibrary.MCState mc)
        {
            UserFormula user = new UserFormula();
            MachineCtrl.GetInstance().dbRecord.GetCurUser(ref user);
            try
            {
                if ((MCState.MCInitializing == mc) || (MCState.MCRunning == mc)
                    || string.IsNullOrEmpty(user.userName) || (!user.userName.Contains("MES")))
                {
                    SetUIEnable(UIEnable.AllDisabled);
                }
                //else if (!string.IsNullOrEmpty(user.userName) && user.userName.Equals("MES", StringComparison.OrdinalIgnoreCase))
                else if (!string.IsNullOrEmpty(user.userName) && user.userName.Contains("MES"))
                {
                    SetUIEnable(UIEnable.AllEnabled);
                }
            }
            catch (System.Exception ex)
            {
                Def.WriteLog($"GetBillInfoList.MesParameterPage.UpdataUIEnable()", ex.Message, LogType.Error);
            }
        }

        /// <summary>
        /// 设置界面控件使能
        /// </summary>
        /// <param name="uiEN"></param>
        private void SetUIEnable(UIEnable uiEN)
        {
            this.Invoke(new Action(() =>
            {
                bool en = (uiEN == UIEnable.AllEnabled);

                //this.buttonGetBillNo.Enabled = en;
                //this.buttonGetBillInfoList.Enabled = en;
                this.textBoxBillNo.Enabled = en;
                this.textBoxBillNum.Enabled = en;
                this.textBoxEquipmentCode.Enabled = en;
                this.textBoxProcessCode.Enabled = en;
                this.textBox1.Enabled = en;
                this.textBox2.Enabled = en;
                this.txtGetBillInfo.Enabled = en;
                this.txtGetBillInfoList.Enabled = en;
                this.txtTrayVerifity.Enabled = en;
                this.txtCellVerifity.Enabled = en;
                this.txtBindCellToTray.Enabled = en;
                this.txtBakingResult.Enabled = en;
                this.txtProductRecordList.Enabled = en;
                this.txtSaveUnBindTrayResult.Enabled = en;
                this.txtCodeRule.Enabled = en;
                this.txtCodeLength.Enabled = en;
                this.txtTimeoutDuration.Enabled = en;
                this.txtTimesNumber.Enabled = en;
                this.checkBox1.Enabled = en;
                this.checkBox2.Enabled = en;
                this.checkBox2.Enabled = en;
                this.btnSave.Enabled = en;
                this.txtTechProParamFormalVerify.Enabled = en;
                this.txtEPTechProParamFormalVerify.Enabled = en;
                this.txtApplyTechTime.Enabled = en;

            }));
        }

        private void UpdataMesConfig()
        {

            this.BeginInvoke(new Action(() =>
            {
                this.DgvBillInfoList.Rows.Clear();
                MesBill.ReadBillConfig();
                MesData.ReadRuleEnableConfig();
                MesData.ReadTime();
                this.txtTimeoutDuration.Text = MesData.mesinterfaceTimeOut.ToString(); ;
                this.txtTimesNumber.Text = MesData.mesFrequency.ToString();
                this.checkBox1.Checked = MesData.CodeRule;
                this.checkBox2.Checked = MesData.TimeOutCheck;
                foreach (DataGridViewColumn item in DgvBillInfoList.Columns)
                {
                    item.SortMode = DataGridViewColumnSortMode.NotSortable;     // 禁止列排序
                }
                //MesRecipeStruct recipeStruct = new MesRecipeStruct();
                foreach (var item in MesBill.infos)
                {
                    int index = this.DgvBillInfoList.Rows.Add();
                    this.DgvBillInfoList.Rows[index].Height = 25;        // 行高度
                    int cellIdx = 0;
                    this.DgvBillInfoList.Rows[index].Cells[cellIdx++].Value = item.Bill_No;
                    this.DgvBillInfoList.Rows[index].Cells[cellIdx++].Value = item.Bill_Num;
                    this.DgvBillInfoList.Rows[index].Cells[cellIdx++].Value = item.Unit;
                    this.DgvBillInfoList.Rows[index].Cells[cellIdx++].Value = item.Bill_State;
                }
                }));
        }

        private void CreatePara()
        {
            // 设置表格
            DataGridViewNF[] dgv = new DataGridViewNF[] { this.dataGridViewNF1 };
            for (int i = 0; i < dgv.Length; i++)
            {
                dgv[i].SetViewStatus();
                dgv[i].ReadOnly = true;
                dgv[i].AllowUserToAddRows = false;         // 可以添加行
                dgv[i].AllowUserToDeleteRows = false;      // 可以删除行
                dgv[i].EditMode = DataGridViewEditMode.EditOnEnter;
                // 项
                DataGridViewTextBoxColumn txtBoxCol = new DataGridViewTextBoxColumn();
                txtBoxCol.HeaderText = "参数编码";
                int idx = dgv[i].Columns.Add(txtBoxCol);
                //dgv[i].Columns[idx].FillWeight = 50;     // 宽度占比权重
                txtBoxCol = new DataGridViewTextBoxColumn();
                txtBoxCol.HeaderText = "参数名称";
                idx = dgv[i].Columns.Add(txtBoxCol);
                //dgv[i].Columns[idx].FillWeight = 50;
                txtBoxCol = new DataGridViewTextBoxColumn();
                txtBoxCol.HeaderText = "单位";
                idx = dgv[i].Columns.Add(txtBoxCol);
                //dgv[i].Columns[idx].FillWeight = 50;
                txtBoxCol = new DataGridViewTextBoxColumn();
                txtBoxCol.HeaderText = "参数上限值";
                idx = dgv[i].Columns.Add(txtBoxCol);
                //dgv[i].Columns[idx].FillWeight = 50;
                txtBoxCol = new DataGridViewTextBoxColumn();
                txtBoxCol.HeaderText = "参数设定值";
                idx = dgv[i].Columns.Add(txtBoxCol);
                //dgv[i].Columns[idx].FillWeight = 50;
                txtBoxCol = new DataGridViewTextBoxColumn();
                txtBoxCol.HeaderText = "参数下限值";
                idx = dgv[i].Columns.Add(txtBoxCol);
                //dgv[i].Columns[idx].FillWeight = 50;
                //txtBoxCol = new DataGridViewTextBoxColumn();
                //txtBoxCol.HeaderText = "映射的程序参数名";
                //idx = dgv[i].Columns.Add(txtBoxCol);
                //dgv[i].Columns[idx].FillWeight = 50;
                foreach (DataGridViewColumn item in dgv[i].Columns)
                {
                    item.SortMode = DataGridViewColumnSortMode.NotSortable;     // 禁止列排序
                }
            }
        }

        //显示工艺配方库
        private void UpdataMesParam(MesInterface mes)
        {
            //MesDefine.ReadConfig(mes);
            MesConfig cfg = MesDefine.GetMesCfg(mes);
            int num = 1;
            //if (null != cfg)
            {
                this.BeginInvoke(new Action(() =>
                {
                    //cfg.mesUri = MesInterface.ApplyTechProParam.ToString();
                    //MesDefine.ReadConfig(mes);
                    if (cfg.parameter.Count > 0)
                    {
                        this.dataGridView1.Rows.Clear();
                        foreach (var item in cfg.parameter.Values)
                        {
                            int index = this.dataGridView1.Rows.Add();
                            this.dataGridView1.Rows[index].Height = 50;        // 行高度
                            int cellIdx = 0;

                            this.dataGridView1.Rows[index].Cells[cellIdx++].Value = num++.ToString();
                            this.dataGridView1.Rows[index].Cells[cellIdx++].Value = item.FormulaNo;  //配方编号
                            this.dataGridView1.Rows[index].Cells[cellIdx++].Value = item.Version+".0";    //版本
                            this.dataGridView1.Rows[index].Cells[cellIdx++].Value = item.ProductNo;
                            this.dataGridView1.Rows[index].Cells[cellIdx++].Value = item.ProductName;
                            this.dataGridView1.Rows[index].Cells[cellIdx++].Value = item.DeliveryTime;
                            this.dataGridView1.Rows[index].Cells[cellIdx++].Value = item.ExecutionTime;
                            if (item.InUse == "use")
                            {
                                this.dataGridView1.Rows[index].DefaultCellStyle.BackColor = Color.Green;
                                this.dataGridView1.Rows[index].Cells[cellIdx++].Value = "使用中";
                            }
                            else
                            {
                                this.dataGridView1.Rows[index].Cells[cellIdx++].Value = "";
                            }
                            
                        }
                    }
                    else
                    {
                        this.dataGridView1.Rows.Clear();
                    }
                }));
            }
        }

        //显示配方明细参数
        private void UpdataMesDetailParam(MesInterface mes,string formulaNo)
        {
            MesConfig cfg = MesDefine.GetMesCfg(mes);
            if (null != cfg)
            {
                this.BeginInvoke(new Action(() =>
                {
                    if (cfg.parameter.Count > 0)
                    {
                        this.dataGridViewNF1.Rows.Clear();
                        foreach (var item in cfg.parameter.Values)
                        {
                            if(item.FormulaNo == formulaNo)
                            {
                                foreach (var item1 in item.Param)
                                {
                                    int index = this.dataGridViewNF1.Rows.Add();
                                    this.dataGridViewNF1.Rows[index].Height = 25;        // 行高度
                                    int cellIdx = 0;
                                    this.dataGridViewNF1.Rows[index].Cells[cellIdx++].Value = item1.ParamCode.ToString();
                                    this.dataGridViewNF1.Rows[index].Cells[cellIdx++].Value = item1.ParamName;  //配方编号
                                    this.dataGridViewNF1.Rows[index].Cells[cellIdx++].Value = item1.ParamUnit;    //版本
                                    this.dataGridViewNF1.Rows[index].Cells[cellIdx++].Value = item1.ParamUpper;
                                    this.dataGridViewNF1.Rows[index].Cells[cellIdx++].Value = item1.ParamValue;
                                    this.dataGridViewNF1.Rows[index].Cells[cellIdx++].Value = item1.ParamLower;
                                }
                            }
                        }
                    }
                }));
            }
        }

        //点击显示配方库的配方参数明细
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex>-1)
            {
                this.formulaNo = this.dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString();
            }
            UpdataMesDetailParam(MesInterface.ApplyTechProParam, formulaNo);
        }

        //配方参数校验————执行按钮
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            //点击按钮在第八行
            if (e.ColumnIndex == 8)
            {
                string msg = "";
                string Formula_No = "";
                string Product_No = "";
                string Versions = "";
                bool result = false;
                ResourcesStruct rs = new ResourcesStruct();
                string fNo = this.dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString();
                DialogResult dialogResult =  MessageBox.Show("是否确定执行该配方，确定执行后配方将覆盖机台参数？","配方参数校验",MessageBoxButtons.OKCancel);
                if (dialogResult ==DialogResult.OK)
                {
                    //MesDefine.ReadConfig(MesInterface.ApplyTechProParam);
                    MesConfig cfg = MesDefine.GetMesCfg(MesInterface.ApplyTechProParam);

                    //获取数据
                    foreach (var item in cfg.parameter.Values)
                    {
                        if (item.FormulaNo == fNo)
                        {
                            Formula_No = item.FormulaNo;
                            Product_No = item.ProductNo;
                            Versions = item.Version;

                        }
                    }
                    //MES超时重传三次
                    for (int i = 0; i < (MesData.mesFrequency == 0 ? 3 : MesData.mesFrequency); i++)
                    {
                        //MES配方参数校验
                        if (!MachineCtrl.GetInstance().EquMesTechProParamFormalVerify(rs, Formula_No, Product_No, Versions, ref msg))
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
                }
                else if (dialogResult == DialogResult.Cancel)
                {
                    return;
                }

                if (result)
                {
                    msg = $"MES配方参数校验成功\r\n";
                    IniFile.WriteString(fNo, "executionTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Def.GetAbsPathName(Def.MesParameterCfg));
                    IniFile.WriteString(fNo, "inuse", "use", Def.GetAbsPathName(Def.MesParameterCfg));
                    //获取数据
                    MesConfig cfg = MesDefine.GetMesCfg(MesInterface.ApplyTechProParam);
                    foreach (var item in cfg.parameter.Values)
                    {
                        if (item.FormulaNo != fNo)
                        {
                            IniFile.WriteString(item.FormulaNo, "inuse", "unuse", Def.GetAbsPathName(Def.MesParameterCfg));
                        }
                    }
                    MesDefine.ReadConfig(MesInterface.ApplyTechProParam);
                    UpdataMesParam(MesInterface.ApplyTechProParam);
                    ShowMsgBox.Show(msg, MessageType.MsgMessage);
                }
            }
        }


        //获取工单信息
        private void buttonGetBillNo_Click(object sender, EventArgs e)
        {
            string msg, billNo, billNum, equipmentID, processID;
            msg = billNo = billNum = equipmentID = processID = "";
            equipmentID = this.textBoxEquipmentCode.Text.ToString();
            processID = this.textBoxProcessCode.Text.ToString();
            for (int i = 0; i <(MesData.mesFrequency==0 ? 3:MesData.mesFrequency); i++)
            {
                //获取工单
                if (!MachineCtrl.GetInstance().MesGetBillNO(equipmentID, processID,ref msg, out billNo, out billNum))
                {
                    //获取报警字符串是否包含"超时"二字,如果没有则跳出循环
                    if (!msg.Contains("超时"))
                    {
                        break;
                    }
                    if (i == 2)
                    {
                        ShowMsgBox.ShowDialog($"MES获取工单信息接口超时失败3次，请检查MES连接状态", MessageType.MsgAlarm);
                    }
                }
                else
                {
                    msg = $"工单获取成功\r\n工单号：{billNo}\r\n工单数量：{billNum}";
                    ShowMsgBox.Show(msg, MessageType.MsgMessage);
                    
                    break;
                }
            }
        }

        // 获取工单队列
        private void buttonGetBillInfoList_Click(object sender, EventArgs e)
        {
            try
            {
                string equipmentID, processID;
                equipmentID = processID = "";
                MesInfo mesInfo = new MesInfo();
                MesBill.WriteConfig(equipmentID, processID, ref mesInfo);

                DataGridView dgvBillInfoList = this.DgvBillInfoList;
                dgvBillInfoList.Rows.Clear();

                foreach (DataGridViewColumn item in DgvBillInfoList.Columns)
                {
                    item.SortMode = DataGridViewColumnSortMode.NotSortable;     // 禁止列排序
                }

                foreach (var param in mesInfo.billInfo)
                {
                    int cellIdx = 0;
                    int index = dgvBillInfoList.Rows.Add();
                    dgvBillInfoList.Rows[index].Height = 25;        // 行高度
                    dgvBillInfoList.Rows[index].Cells[cellIdx++].Value = param.Bill_No;
                    dgvBillInfoList.Rows[index].Cells[cellIdx++].Value = param.Bill_Num;
                    dgvBillInfoList.Rows[index].Cells[cellIdx++].Value = param.Unit;
                    dgvBillInfoList.Rows[index].Cells[cellIdx++].Value = param.Bill_State;
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.ToString());
            }
           
        }
        

        /// <summary>
        /// 读取csv
        /// </summary>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool ReadCsv(string path, out List<string> data)
        {
            StreamReader sr;
            data = new List<string>();
            try
            {
                using (sr = new StreamReader(path, Encoding.GetEncoding("GB2312")))
                {
                    string str = "";
                    while ((str = sr.ReadLine()) != null)
                    {
                        data.Add(str);
                    }
                }
                //data.RemoveAt(0);
                //data.Reverse();
            }
            catch (Exception ex)
            {
                if (ex.ToString().Contains("正由另一进程使用"))
                    return false;
                foreach (Process process in Process.GetProcesses())
                {
                    if (process.ProcessName.ToUpper().Equals("EXCEL"))
                        process.Kill();
                }
                GC.Collect();
                Console.WriteLine(ex.StackTrace);
                using (sr = new StreamReader(path, Encoding.GetEncoding("GB2312")))
                {
                    string str = "";
                    while ((str = sr.ReadLine()) != null)
                    {
                        data.Add(str);
                    }
                }
            }
            return true;
        }


        private void UpdateModuleState1(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (false == MachineCtrl.GetPullInExCsvFileState(ref filePath) || filePath == "")
                    return;

                // 使用委托更新UI
                this.Invoke(new Action(() =>
                {
                    if (ReadCsv(filePath, out OutBankData))
                    {

                        //当文件更换时删除所有行
                        if (filePath != copy_filePath)
                        {
                            copy_filePath = filePath;

                            while (this.dataGridViewIn.Rows.Count > 1)
                            {
                                for (int index = 0; index < this.dataGridViewIn.Rows.Count; index++)
                                {
                                    this.dataGridViewIn.Rows.Remove(dataGridViewIn.Rows[index]);
                                }
                            }
                        }

                        foreach (DataGridViewColumn item in dataGridViewIn.Columns)
                        {
                            item.SortMode = DataGridViewColumnSortMode.NotSortable;     // 禁止列排序
                        }

                        ////将读取到的信息顺序反转
                        //OutBankData.Reverse();
                        for (int i = this.dataGridViewIn.Rows.Count; i < OutBankData.Count - 1; i++)            // 添加行数据
                        {
                            int index = this.dataGridViewIn.Rows.Add();
                            this.dataGridViewIn.Rows[index].Height = 20;        // 行高度                            
                                                                                
                            int j = 0;
                            this.dataGridViewIn.Rows[index].Cells[j].Value = OutBankData[i + 1].Split(',')[0];
                            this.dataGridViewIn.Rows[index].Cells[j + 1].Value = OutBankData[i + 1].Split(',')[8];
                            this.dataGridViewIn.Rows[index].Cells[j + 2].Value = Convert.ToInt32(OutBankData[i + 1].Split(',')[6]) == 1 ? "NG" : "OK";
                            this.dataGridViewIn.Rows[index].Cells[j + 3].Value = OutBankData[i + 1].Split(',')[7];
                            this.dataGridViewIn.Rows[index].Cells[j + 4].Value = OutBankData[i + 1].Split(',')[1];

                            if (this.dataGridViewIn.Rows[index].Cells[j + 2].Value.ToString()=="NG")
                            {
                                this.dataGridViewIn.Rows[index].DefaultCellStyle.BackColor = Color.IndianRed;
                            }
                        }
                        
                        //将Datagridview的信息按降序排列，让最新的信息显示在上面
                        dataGridViewIn.Sort(dataGridViewIn.Columns[4], ListSortDirection.Descending);
                    }
                }))
                ;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("FixtureMaintainPage.UpdateModuleState " + ex.Message);
            }

        }

        private void UpdateModuleState2(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (false == MachineCtrl.GetOutExCsvFileState(ref filePath) || filePath == "")
                    return;

                // 使用委托更新UI
                this.Invoke(new Action(() =>
                {
                    if (ReadCsv(filePath, out OutBankData))
                    {

                        //当文件更换时删除所有行
                        if (filePath != copy_filePath)
                        {
                            copy_filePath = filePath;

                            while (this.dataGridViewOut.Rows.Count > 1)
                            {
                                for (int index = 0; index < this.dataGridViewOut.Rows.Count; index++)
                                {

                                    this.dataGridViewOut.Rows.Remove(dataGridViewOut.Rows[index]);
                                }
                            }
                        }
                    }

                    foreach (DataGridViewColumn item in dataGridViewOut.Columns)
                    {
                        item.SortMode = DataGridViewColumnSortMode.NotSortable;     // 禁止列排序
                    }

                    ////将读取到的信息顺序反转
                    //OutBankData.Reverse();
                    for (int i = this.dataGridViewOut.Rows.Count; i < OutBankData.Count - 1; i++)            // 添加行数据
                    {
                        int index = this.dataGridViewOut.Rows.Add();
                        this.dataGridViewOut.Rows[index].Height = 20;        // 行高度                            

                        int j = 0;
                        this.dataGridViewOut.Rows[index].Cells[j].Value = OutBankData[i + 1].Split(',')[0];
                        this.dataGridViewOut.Rows[index].Cells[j + 1].Value = OutBankData[i + 1].Split(',')[6];
                        this.dataGridViewOut.Rows[index].Cells[j + 2].Value = Convert.ToInt32(OutBankData[i + 1].Split(',')[11]) == 1 ? "NG" : "OK";
                        this.dataGridViewOut.Rows[index].Cells[j + 3].Value = OutBankData[i + 1].Split(',')[12];
                        this.dataGridViewOut.Rows[index].Cells[j + 4].Value = OutBankData[i + 1].Split(',')[1];
                        if (this.dataGridViewOut.Rows[index].Cells[j + 2].Value.ToString() == "NG")
                        {
                            this.dataGridViewOut.Rows[index].DefaultCellStyle.BackColor = Color.IndianRed;
                        }
                    }

                        //将Datagridview的信息按降序排列，让最新的信息显示在上面
                        dataGridViewOut.Sort(dataGridViewOut.Columns[4], ListSortDirection.Descending);
                }))
                ;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("FixtureMaintainPage.UpdateModuleState " + ex.Message);
            }

        }

        
        //保存
        private void btnSave_Click(object sender, EventArgs e)
        {

            //条码规则保存
            //run.WriteParameter(RunID.OnloadRecv.ToString(), "codeType", this.txtCodeRule.Text.ToString());
            //run.WriteParameter(RunID.OnloadRecv.ToString(), "codeLength", this.txtCodeLength.Text.ToString());
            MesData.CodeRule = this.checkBox1.Checked;
            MesData.TimeOutCheck = this.checkBox2.Checked;

            //MesData.MesUrl = this.txtGetBillInfo.Text.ToString();
            MesData.WriteConfig();
            if (!string.IsNullOrEmpty(this.txtTimeoutDuration.Text.ToString()) && !string.IsNullOrEmpty(this.txtTimesNumber.Text.ToString()) && !string.IsNullOrEmpty(this.txtApplyTechTime.Text.ToString()))
            {
                //超时时间
                MesData.mesinterfaceTimeOut = Convert.ToInt32(this.txtTimeoutDuration.Text);
                MesData.mesFrequency = Convert.ToInt32(this.txtTimesNumber.Text);
                MesData.WriteTime();
                //工艺效验时长
                MesData.MesApplyTechTime = Convert.ToInt32(this.txtApplyTechTime.Text);
                MesData.WriteApplyTechTime();
            }
            else
            {
                MessageBox.Show("保存失败，输入框不能为空！！！");
                return;
            }
        }

        private void tabPage5_Enter(object sender, EventArgs e)
        {
            //this.txtCodeRule.Text = run.ReadStringParameter(RunID.OnloadRecv.ToString(), "codeType", "");
            //this.txtCodeLength.Text = run.ReadIntParameter(RunID.OnloadRecv.ToString(), "codeLength", 16).ToString();

        }
        //工艺参数申请
        private void buttonGetApplyTechProParamFormal_Click(object sender, EventArgs e)
        {
            string msg = "";
            //超时次数
            for (int i = 0; i < (MesData.mesFrequency == 0 ? 3 : MesData.mesFrequency); i++)
            {
                //工艺参数申请
                if (!MachineCtrl.GetInstance().MesGetBillParameter(ref msg))
                {
                    //获取报警字符串是否包含"超时"二字,如果没有则跳出循环
                    if (!msg.Contains("超时"))
                    {
                        break;
                    }
                    if (i == 2)
                    {
                        ShowMsgBox.ShowDialog($"MES工艺参数申请接口超时失败3次，请检查MES连接状态", MessageType.MsgAlarm);
                        break;
                    }
                }
                else
                {
                    msg = $"工艺参数获取成功！！！";
                    ShowMsgBox.Show(msg, MessageType.MsgMessage);
                    break;
                }
            }
            
        }

        //删除配方
        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {

            string fNo = this.dataGridView1.CurrentRow.Cells[1].Value.ToString();
            DialogResult dialogResult = MessageBox.Show("是否确定删除该配方？", "配方参数", MessageBoxButtons.OKCancel);
            if (dialogResult == DialogResult.OK)
            {
                string section = fNo;
                string file = Def.GetAbsPathName(Def.MesParameterCfg);
                
                IniFile.EmptySection(section, file);
                IniFile.DeleteSection(section,file);
                IniFile.DeleteKey(MesInterface.ApplyTechProParam.ToString(),fNo,file);
                  MesDefine.ReadConfig(MesInterface.ApplyTechProParam);
                UpdataMesParam(MesInterface.ApplyTechProParam);
            }else if (dialogResult==DialogResult.Cancel)
            {
                return;
            }
        }

        //TEST:托盘校验
        private void button1_Click(object sender, EventArgs e)
        {
            RunProcessOnloadRobot run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
            string pltcode = "";
            string msg = "";
            if (!run.MesCheckPalletStatus(pltcode,ref msg))
            {
                ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
            }
            else
            {
                ShowMsgBox.ShowDialog("成功", MessageType.MsgMessage);
            }
        }
        //TEST:来料校验
        private void button2_Click(object sender, EventArgs e)
        {
            RunProcessOnloadRecv run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRecv) as RunProcessOnloadRecv;
            bool updataParam = false;
            Battery bat = new Battery();
            bat.Code = "";
            string msg = "";
            if (!run.MesCheckBatteryStatus(bat, out updataParam, ref msg))
            {
                ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
            }
            else
            {
                ShowMsgBox.ShowDialog("成功", MessageType.MsgMessage);
            }
        }
        //TEST:绑盘上传
        private void button3_Click(object sender, EventArgs e)
        {
            RunProcessOnloadRobot run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
            string msg = "";

            if (!run.MesBindPalletInfo(run.Pallet[0], ref msg))
            {
                ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
            }
            else
            {
                ShowMsgBox.ShowDialog("成功", MessageType.MsgMessage);
            }
        }
        //TEST:Baking开始与结束
        private void button4_Click(object sender, EventArgs e)
        {
            int rowIdx = 1;
            string msg = "";
            RunProcessDryingOven run = MachineCtrl.GetInstance().GetModule(RunID.DryOven0+1) as RunProcessDryingOven;
            if (!run.MesBakingStatusInfo(rowIdx, BakingType.Normal_start, ref msg))
            {
                ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
            }
            else
            {
                ShowMsgBox.ShowDialog("成功", MessageType.MsgMessage);
            }
        }
        
        //TEST:解绑上传
        private void button6_Click(object sender, EventArgs e)
        {
            string str = "";
            string msg = "";
            RunProcessDryingOven run = MachineCtrl.GetInstance().GetModule(RunID.DryOven0 + 1) as RunProcessDryingOven;
            if (!MachineCtrl.GetInstance().MesUnbindPalletInfo(str, MesResources.Heartbeat, ref msg))
            {
                ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
            }
            else
            {
                ShowMsgBox.ShowDialog("成功", MessageType.MsgMessage);
            }
        }
        //TEST:生产履历
        private void button7_Click(object sender, EventArgs e)
        {
            int cavityIdx = 1;
            string msg = "";
            double[,] waterContentValue = new double[(int)OvenRowCol.MaxRow, 3];
            string operatecode = "";
            for (int i = 0; i < 3; i++)
            {
                waterContentValue[cavityIdx, i] = 333.00;
            }
            Pallet[] plt = new Pallet[(int)OvenRowCol.MaxCol];


            RunProcessDryingOven run = MachineCtrl.GetInstance().GetModule(RunID.DryOven0 + 1) as RunProcessDryingOven;
            if (!run.MesProductionRecord(MesResources.OvenCavity[1,1], cavityIdx, waterContentValue, plt, true, ref msg, ref operatecode))
            {
                ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
            }
            else
            {
                ShowMsgBox.ShowDialog("成功", MessageType.MsgMessage);
            }
        }
    }
}
