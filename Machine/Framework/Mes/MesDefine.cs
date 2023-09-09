using HelperLibrary;
using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows.Forms;

namespace Machine
{
    #region // MES定义设备状态

    /// <summary>
    /// MES定义设备状态
    /// </summary>
    public enum MesMCState
    {
        Running = 01,
        Waiting,
        Stop,
        Alarm,
        Other,
    }
    #endregion

    #region // MES接口

    /// <summary>
    /// MES接口
    /// </summary>
    public enum MesInterface
    {

        GetBillInfo,                  // 工单信息获取（日志OK)
        GetBillInfoList,              // 工单队列获取（日志OK)
        TrayVerifity,                 // 夹具校验 （日志OK)
        BakingMaterialVerifity,       // 入站校验（日志OK)
        ApplyTechProParam,            // 工艺参数申请（日志OK)         （暂时不用)
        TechProParamFormalVerify,      //配方效验                      （暂时不用)
        EPTechProParamFormalVerify,    //设备参数效验                  （暂时不用)
        SaveTrayAndBarcodeRecord,     // 绑盘上传（日志OK)
        TrayUnbundlingRecord,         // 解绑上传（日志OK)
        SaveFurnaceChamberRecord,     // 绑炉腔上传（日志OK)          （暂时不用)
        SaveBakingResultRecord,       // Baking开始/结束（日志OK)
        SaveWaterContentTestRecord,   // 水分测试上传（日志OK)        （暂时不用)
        SavePR_ProductRecordList,     // 生产履历记录 
        SaveRejectRecord,             // 不良品上报（日志OK)          （暂时不用)
        Heartbeat,                    // MES心跳（日志OK)             （暂时不用)

        End,
    }
    #endregion

    #region // MES下发或上载参数

    /// <summary>
    /// xujia
    /// </summary>
    [System.Serializable]
    public struct MesParameterStruct
    {
        public string Key;              // 映射的程序参数名
        public string Code;             // 参数代码
        public string Name;             // 参数名称 
        public string Unit;             // 参数单位
        public string Upper;            // 参数设定值上限
        public string Value;            // 参数设定值
        public string Lower;            // 参数设定值下限
    }

    /// <summary>
    /// MES下发或上载参数结构
    /// </summary>
    public struct MesRecipeStruct
    {

        //public string RecipeCode;               // 配方编码
        //public string Version;                  // 版本
        //public string ProductCode;              // 产品编码
        //public string LastUpdateOnTime;         // 最后更新时间

        public string FormulaNo;                //配方号
        public string ProductNo;                //产品编码
        public string ProductName;              //产品名称
        public string Version;                  //版本
        public string DeliveryTime;         //下发时间
        public string ExecutionTime;        //执行时间
        public string InUse;                  //使用情况
        
        public List<MesParameterData> Param;    // 参数
    }

    /// <summary>
    /// 参数信息数据
    /// </summary>
    public struct MesParameterData
    {
        //public string ParamCode;                // 参数代码
        //public string Version;                  // 参数版本
        //public string ParamValue;               // 参数值
        //public string ParamUpper;               // 参数上限
        //public string ParamLower;               // 参数下限
        //public string Key;                      // 映射的本程序参数名

        public string ParamCode;       //参数代码
        public string ParamName;       //参数名称
        public string ParamUnit;       //参数单位
        public string ParamUpper;      //参数上限
        public string ParamValue;      //参数中值
        public string ParamLower;      //参数下限
    }
    #endregion







    #region // Mes配置

    /// <summary>
    /// Mes配置
    /// </summary>
    public class MesConfig
    {
        public bool enable;                                           // 接口启用状态
        public string mesUri;                                         // 地址
        public long parameterDate;                                    // 参数集更新时间：DateTime.Now.ToBinary();
        public Dictionary<string, MesRecipeStruct> parameter;         // 参数集：<string参数代码, 参数>
        public string send;                                           // 发送数据
        public string recv;                                           // 接收数据
        public bool updataRS;                                         // 收发数据已更新

        public MesConfig()
        {
            this.parameter = new Dictionary<string, MesRecipeStruct>();
            Clear();
        }

        public void Clear()
        {
            enable = true;
            mesUri = string.Empty;
            parameterDate = 0;
            parameter.Clear();
            send = "";
            recv = "";
            updataRS = false;
        }

        public void Copy(MesConfig mesCfg)
        {
            this.enable = mesCfg.enable;
            this.mesUri = mesCfg.mesUri;
            this.parameterDate = mesCfg.parameterDate;
            this.parameter.Clear();
            foreach(var item in mesCfg.parameter)
            {
                this.parameter.Add(item.Key, item.Value);
            }
        }
    }
    #endregion

    #region // Mes配置参数定义

    /// <summary>
    /// Mes配置参数定义
    /// </summary>
    public static class MesDefine
    {
        private static string[] MesTitle;
        private static MesConfig[] MesCfg;

        public static MesConfig GetMesCfg(MesInterface mes)
        {
            if (null == MesCfg)
            {
                MesCfg = new MesConfig[(int)MesInterface.End];
                for(int i = 0; i < MesCfg.Length; i++)
                {
                    MesCfg[i] = new MesConfig();
                }
            }
            return MesCfg[(int)mes];
        }

        public static void ReadConfig(MesInterface mes)
        {
            MesConfig mesCfg = GetMesCfg(mes);
            
            if (null == mesCfg)
            {
                return;
            }
            mesCfg.parameter.Clear();
            string file, section, key;
            file = Def.GetAbsPathName(Def.MesParameterCfg);
            section = mes.ToString();
            string cons = "";

            List<string> paramList = new List<string>();
            paramList.Add(nameof(mesCfg.enable));
            mesCfg.enable = /*true;*/IniFile.ReadBool(section, nameof(mesCfg.enable), mesCfg.enable, file);
            paramList.Add(nameof(mesCfg.mesUri));
            mesCfg.mesUri = IniFile.ReadString(section, nameof(mesCfg.mesUri), "", file);

            // 搜索包含的子节点
            string[] sectionKV = IniFile.ReadAllItems(section, file);
            if (sectionKV.Length > 0)
            {
                foreach (var item in sectionKV)
                {
                    string[] kv = item.Split((new char[] { '=' }), StringSplitOptions.RemoveEmptyEntries);
                    if (kv.Length < 1)
                        continue;
                    kv = kv[0].Split('.');
                    if (kv.Length < 1)
                        continue;
                    if (paramList.Contains(kv[0]))
                        continue;
                    paramList.Add(kv[0]);
                    section = kv[0];
                    string[] subSectionKV = IniFile.ReadAllItems(section, file);
                    if (subSectionKV.Length > 0)
                    {
                        MesRecipeStruct mesRecipe = new MesRecipeStruct();
                        mesRecipe.FormulaNo = kv[0];
                        key = "product_no";
                        paramList.Add(key);
                        mesRecipe.ProductNo = IniFile.ReadString(section, key, "", file);
                        key = "product_name";
                        paramList.Add(key);
                        mesRecipe.ProductName = IniFile.ReadString(section, key, "", file);
                        key = "version";
                        paramList.Add(key);
                        mesRecipe.Version = IniFile.ReadString(section, key, "", file);
                        key = "deliveryTime";
                        paramList.Add(key);
                        mesRecipe.DeliveryTime = IniFile.ReadString(section, key, "", file);
                        key = "executionTime";
                        paramList.Add(key);
                        mesRecipe.ExecutionTime = IniFile.ReadString(section, key, "", file);
                        key = "inUse";
                        paramList.Add(key);
                        mesRecipe.InUse = IniFile.ReadString(section, key, "", file);

                        // 参数项解析
                        mesRecipe.Param = new List<MesParameterData>();
                        foreach (var subItem in subSectionKV)
                        {
                            kv = subItem.Split((new char[] { '=' }), StringSplitOptions.RemoveEmptyEntries);
                            if (kv.Length < 1)
                                continue;
                            kv = kv[0].Split('.');
                            if (kv.Length < 1)
                                continue;
                            //if (paramList.Contains(kv[0])&& !kv[0].ToString().Contains("BKD"))
                            if ((paramList.Contains(kv[0]) && kv[0]==cons)|| (kv[0]== "product_no" || kv[0] == "product_name" || kv[0] == "version" || kv[0] == "deliveryTime" || kv[0] == "executionTime" || kv[0] == "executionTime" || kv[0] == "inuse"))
                                continue;
                            paramList.Add(kv[0]);
                            cons = kv[0];
                            MesParameterData param = new MesParameterData();
                            param.ParamCode = kv[0];
                            key = $"{param.ParamCode}.param_name";
                            param.ParamName = IniFile.ReadString(section, key, "", file);
                            key = $"{param.ParamCode}.param_unit";
                            param.ParamUnit = IniFile.ReadString(section, key, "", file);
                            key = $"{param.ParamCode}.param_upper";
                            param.ParamUpper = IniFile.ReadString(section, key, "", file);
                            key = $"{param.ParamCode}.param_value";
                            param.ParamValue = IniFile.ReadString(section, key, "", file);
                            key = $"{param.ParamCode}.param_lower";
                            param.ParamLower = IniFile.ReadString(section, key, "", file);

                            mesRecipe.Param.Add(param);
                        }
                        mesCfg.parameter.Add(mesRecipe.FormulaNo, mesRecipe);
                    }
                }
            }
        }
        //public static void ReadConfig(MesInterface mes)
        //{
        //    MesConfig mesCfg = GetMesCfg(mes);
        //    if (null == mesCfg)
        //    {
        //        return;
        //    }
        //    string file, section, key;
        //    file = Def.GetAbsPathName(Def.MesParameterCfg);
        //    section = mes.ToString();

        //    List<string> paramList = new List<string>();
        //    paramList.Add(nameof(mesCfg.enable));
        //    mesCfg.enable = /*true;*/IniFile.ReadBool(section, nameof(mesCfg.enable), mesCfg.enable, file);
        //    paramList.Add(nameof(mesCfg.mesUri));
        //    mesCfg.mesUri = IniFile.ReadString(section, nameof(mesCfg.mesUri), "", file);
        //    paramList.Add(nameof(mesCfg.parameterDate));
        //    long.TryParse(IniFile.ReadString(section, nameof(mesCfg.parameterDate), mesCfg.parameterDate.ToString(), file), out mesCfg.parameterDate);

        //    string[] keyValue = IniFile.ReadAllItems(section, file);
        //    if (keyValue.Length > 0)
        //    {
        //        foreach(var item in keyValue)
        //        {
        //            string[] kv = item.Split((new char[] { '=' }), StringSplitOptions.RemoveEmptyEntries);
        //            if(kv.Length < 1)
        //                continue;
        //            kv = kv[0].Split('.');
        //            if(kv.Length < 1)
        //                continue;
        //            if (paramList.Contains(kv[0]))
        //                continue;
        //            paramList.Add(kv[0]);
        //            MesParameterStruct param = new MesParameterStruct();
        //            param.Code = kv[0];
        //            //key = $"{param.Code}.Code";
        //            //param.Code = IniFile.ReadString(section, key, "", file);
        //            key = $"{param.Code}.Name";
        //            param.Name = IniFile.ReadString(section, key, "", file);
        //            key = $"{param.Code}.Unit";
        //            param.Unit = IniFile.ReadString(section, key, "", file);
        //            key = $"{param.Code}.Upper";
        //            param.Upper = IniFile.ReadString(section, key, "", file);
        //            key = $"{param.Code}.Value";
        //            param.Value = IniFile.ReadString(section, key, "", file);
        //            key = $"{param.Code}.Lower";
        //            param.Lower = IniFile.ReadString(section, key, "", file);
        //            key = $"{param.Code}.Key";
        //            param.Key = IniFile.ReadString(section, key, "", file);

        //            mesCfg.parameter.Add(param.Code, param);
        //        }
        //    }
        //}

        public static void WriteConfig(MesInterface mes)
        {
            string file, section, key;
            file = Def.GetAbsPathName(Def.MesParameterCfg);
            section = mes.ToString();

            MesConfig mesCfg = GetMesCfg(mes);

            IniFile.EmptySection(section, file);

            IniFile.WriteBool(section, nameof(mesCfg.enable), mesCfg.enable, file);
            IniFile.WriteString(section, nameof(mesCfg.mesUri), mesCfg.mesUri, file);

            foreach (var item in mesCfg.parameter)
            {
                // 每种配方一个节
                IniFile.WriteString(section, item.Key, item.Key, file);

                IniFile.WriteString(item.Key, "product_no", item.Value.ProductNo, file);
                IniFile.WriteString(item.Key, "product_name", item.Value.ProductName, file);
                IniFile.WriteString(item.Key, "version", item.Value.Version, file);
                IniFile.WriteString(item.Key, "deliverytime", item.Value.DeliveryTime, file);
                IniFile.WriteString(item.Key, "inuse",item.Value.InUse, file);

                if (null != item.Value.Param)
                {
                    foreach (var param in item.Value.Param)
                    {
                        // 配方.参数代码.项
                        key = $"{param.ParamCode}.param_name";
                        IniFile.WriteString(item.Key, key, param.ParamName, file);
                        key = $"{param.ParamCode}.param_unit";
                        IniFile.WriteString(item.Key, key, string.IsNullOrEmpty(param.ParamUnit) ? "" : param.ParamValue, file);
                        key = $"{param.ParamCode}.param_upper";
                        IniFile.WriteString(item.Key, key, string.IsNullOrEmpty(param.ParamUpper) ? "" : param.ParamUpper, file);
                        key = $"{param.ParamCode}.param_value";
                        IniFile.WriteString(item.Key, key, string.IsNullOrEmpty(param.ParamValue) ? "" : param.ParamLower, file);
                        key = $"{param.ParamCode}.param_lower";
                        IniFile.WriteString(item.Key, key, string.IsNullOrEmpty(param.ParamLower) ? "" : param.ParamLower, file);
                    }
                }
            }
        }

        //public static void WriteConfig(MesInterface mes)
        //{
        //    string file, section, key;
        //    file = Def.GetAbsPathName(Def.MesParameterCfg);
        //    section = mes.ToString();

        //    MesConfig mesCfg = GetMesCfg(mes);

        //    IniFile.EmptySection(section, file);

        //    IniFile.WriteBool(section, nameof(mesCfg.enable), mesCfg.enable, file);
        //    IniFile.WriteString(section, nameof(mesCfg.mesUri), mesCfg.mesUri, file);
        //    IniFile.WriteString(section, nameof(mesCfg.parameterDate), mesCfg.parameterDate.ToString(), file);

        //    foreach(var item in mesCfg.parameter)
        //    {
        //        //key = $"{item.Key}.Code";
        //        //IniFile.WriteString(section, key, item.Value.Code, file);
        //        key = $"{item.Key}.Name";
        //        IniFile.WriteString(section, key, item.Value.Name, file);
        //        key = $"{item.Key}.Unit";
        //        IniFile.WriteString(section, key, item.Value.Unit, file);
        //        key = $"{item.Key}.Upper";
        //        IniFile.WriteString(section, key, item.Value.Upper, file);
        //        key = $"{item.Key}.Value";
        //        IniFile.WriteString(section, key, item.Value.Value, file);
        //        key = $"{item.Key}.Lower";
        //        IniFile.WriteString(section, key, item.Value.Lower, file);
        //        key = $"{item.Key}.Key";
        //        IniFile.WriteString(section, key, item.Value.Key, file);
        //    }
        //}

       

        public static string GetMesTitle(MesInterface mes)
        {
            if (null == MesTitle)
            {
                MesTitle = new string[(int)MesInterface.End]
                {
                    "工单信息获取",
                    "工单队列获取",
                    "夹具校验",
                    "入站校验",
                    "工艺参数申请",
                    "配方效验",
                    "设备参数校验",
                    "绑盘上传",
                    "解绑上传",
                    "绑炉腔上传",
                    "Baking开始/结束",
                    "水分测试上传",
                    "生产履历记录",
                    "不良品上报",
                    "MES心跳",

                };
            }
            return MesTitle[(int)mes];
        }
    }
    #endregion

    public struct MesBillInfo
    {
        public string Bill_No;               // 工单号
        public string Bill_Num;              // 工单数量
        public string Unit;                  // 单位           
        public string Bill_State;            //工单状态
    }

    /// <summary>
    /// 工单队列
    /// </summary>
    public static class MesBill
    {
        public struct MesInfo
        {
            public List<MesBillInfo> billInfo;    // 工单队列参数
        }

        public static List<MesBillInfo> infos;    // 工单队列参数

        //工单队列写入
        public static void WriteConfig(string equipmentID, string processID, ref MesInfo mesRecipeStruct)
        {
            try
            {
                string file, section, key;
                string msg = "";
                file = Def.GetAbsPathName(Def.MesParameterCfg);
                section = "BillInfoList";
                IniFile.EmptySection(section, file);
                bool result = false;
                for (int i = 0; i < (MesData.mesFrequency == 0 ? 3 : MesData.mesFrequency); i++)
                {
                    //工单队列
                    if (!MachineCtrl.GetInstance().MesGetBillInfoList(equipmentID, processID, ref mesRecipeStruct,ref msg))
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
                            ShowMsgBox.ShowDialog($"MES获取工单队列接口超时失败3次，请检查MES连接状态", MessageType.MsgAlarm);
                        }
                    }
                    else
                    {
                        result = true;
                        break;
                    }
                }

                //bool result = (MachineCtrl.GetInstance().MesGetBillInfoList(equipmentID, processID, ref mesRecipeStruct));
                if (result)
                {
                    msg = $"工单获取成功\r\n";
                    ShowMsgBox.Show(msg, MessageType.MsgMessage);
                    int num = 0;
                    foreach (var param in mesRecipeStruct.billInfo)
                    {
                        key = $"Bill_No" + num;
                        IniFile.WriteString(section, key, param.Bill_No, file);
                        key = $"Bill_Num" + num;
                        IniFile.WriteString(section, key, param.Bill_Num, file);
                        key = $"Unit" + num;
                        IniFile.WriteString(section, key, param.Unit, file);
                        key = $"Bill_State" + num;
                        IniFile.WriteString(section, key, param.Bill_State, file);
                        num++;
                    }
                }
                
            }
            catch (Exception ex)
            {
                string msg = $"工单队列保存数据错误：{ex.Message}";
                ShowMsgBox.ShowDialog(msg + "\r\n处理方式：请检查MES在线是否打开或检查MES数据参数！", MessageType.MsgAlarm);
            }

        }

        /// <summary>
        /// 工单队列读取
        /// </summary>
        public static void ReadBillConfig()
        {
            string file, section;
            file = Def.GetAbsPathName(Def.MesParameterCfg);
            section = "BillInfoList";
            infos = new List<MesBillInfo>();

            try
            {
                MesBillInfo Info = new MesBillInfo();
                int i = 0;
                while (true)
                {

                    Info.Bill_No = IniFile.ReadString(section, "Bill_No" + i, "", file);
                    if ("" == Info.Bill_No)
                    {
                        break;
                    }
                    Info.Bill_Num = IniFile.ReadString(section, "Bill_Num" + i, "", file);
                    Info.Unit = IniFile.ReadString(section, "Unit" + i, "", file);
                    Info.Bill_State = IniFile.ReadString(section, "Bill_State" + i, "", file);
                    infos.Add(Info);
                    i++;
                }
            }
            catch(Exception ex) 
            { 
                Console.WriteLine(ex.ToString());
			}
        }
        
    }

    #region // Mes接口Log

    /// <summary>
    /// Mes接口Log
    /// </summary>
    public static class MesLog
    {
        private static LogFile[] mesLog;

        private static LogFile GetLogFile(MesInterface mes)
        {
            if(null == mesLog)
            {
                mesLog = new LogFile[(int)MesInterface.End];
                for(int i = 0; i < mesLog.Length; i++)
                {
                    mesLog[i] = new LogFile();
                    mesLog[i].SetFileInfo(Def.GetAbsPathName($"Log\\MES\\{(MesInterface)i}\\"), 2, 30);
                }
            }
            return mesLog[(int)mes];
        }

        public static void SetFileInfo(MesInterface mes, string filePath, long size, int storageLife)
        {
            GetLogFile(mes).SetFileInfo(filePath, size, storageLife);
        }

        public static void WriteLog(MesInterface mes, string msgText, LogType msgType = LogType.Error)
        {
            GetLogFile(mes).WriteLog(DateTime.Now, mes.ToString(), msgText, msgType);
        }
    }

    #endregion

    #region // 作业班次

    /// <summary>
    /// 班次包含信息
    /// </summary>
    public struct ShiftStruct
    {
        public string Code;
        public string Name;
        public DateTime Start;
        public DateTime End;
    }

    /// <summary>
    /// 作业班次
    /// </summary>
    public static class OperationShifts
    {
        public static List<ShiftStruct> Shifts;
        private static List<string> ShiftsTime;

        /// <summary>
        /// 获取班次信息
        /// </summary>
        /// <returns></returns>
        public static ShiftStruct Shift()
        {
            ShiftStruct sfift = new ShiftStruct();
            DateTime dt = DateTime.Now;
            foreach(var item in Shifts)
            {
                if(item.Start.Hour <= item.End.Hour)
                {
                    if((item.Start.TimeOfDay <= dt.TimeOfDay) && (dt.TimeOfDay <= (item.End.AddSeconds(1).TimeOfDay)))
                    {
                        sfift = item;
                        break;
                    }
                }
                else
                {
                    if((item.Start.TimeOfDay <= dt.TimeOfDay) || (dt.TimeOfDay <= item.End.AddSeconds(1).TimeOfDay))
                    {
                        sfift = item;
                        break;
                    }
                }
            }
            return sfift;
        }

        /// <summary>
        /// 是否换班
        /// </summary>
        /// <returns></returns>
        public static bool ChangeShift()
        {
            if (ShiftsTime.Contains(DateTime.Now.ToString("HH:mm:ss")))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 读取班次配置
        /// </summary>
        public static void ReadConfig()
        {
            if(null == Shifts)
            {
                Shifts = new List<ShiftStruct>();
                ShiftsTime = new List<string>();
            }
            string file, section, key;
            file = Def.GetAbsPathName(Def.MesParameterCfg);
            section = "Shifts";

            int idx = 0;
            ShiftStruct shift = new ShiftStruct();
            while(true)
            {
                shift.Code = IniFile.ReadString(section, "Code" + idx, "", file);
                if("" == shift.Code)
                {
                    break;
                }
                shift.Name = IniFile.ReadString(section, "Name" + idx, "", file);
                key = IniFile.ReadString(section, "Start" + idx, "", file);
                DateTime.TryParse(key, out shift.Start);
                ShiftsTime.Add(shift.Start.ToString("HH:mm:ss"));
                key = IniFile.ReadString(section, "End" + idx, "", file);
                DateTime.TryParse(key, out shift.End);
                ShiftsTime.Add(shift.End.ToString("HH:mm:ss"));

                Shifts.Add(shift);
                idx++;
            }
        }

        /// <summary>
        /// 保存班次配置
        /// </summary>
        public static void WriteConfig()
        {
            string file, section, key;
            file = Def.GetAbsPathName(Def.MesParameterCfg);
            section = "Shifts";

            IniFile.EmptySection(section, file);
            ShiftsTime.Clear();

            for(int i = 0; i < Shifts.Count; i++)
            {
                IniFile.WriteString(section, "Code" + i, Shifts[i].Code, file);
                IniFile.WriteString(section, "Name" + i, Shifts[i].Name, file);
                key = Shifts[i].Start.ToString("HH:mm:ss");
                IniFile.WriteString(section, "Start" + i, key, file);
                ShiftsTime.Add(key);
                key = Shifts[i].End.ToString("HH:mm:ss");
                IniFile.WriteString(section, "End" + i, key, file);
                ShiftsTime.Add(key);
            }
        }
    }

    #endregion

    // MES功能数据
    public static class MesData
    {
        public static bool CodeRule;                         //条码规则启用
        public static int mesinterfaceTimeOut;               //超时时间
        public static int mesFrequency;                      //超时次数
        public static bool TimeOutCheck;                     //超时是否停机

        public static int MesApplyTechTime;                  //工艺定时效验时长

        public static string MesUrl;


        /// <summary>
        /// 工艺定时效验定时器
        /// </summary>
        public static void MesApplyTechTimeData()
        {
            ReadApplyTechTime();
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Enabled = true;
            timer.Interval = MesApplyTechTime*60000;
            timer.Start();
            timer.Elapsed += new System.Timers.ElapsedEventHandler(MesApplyTime);

        }
        /// <summary>
        /// 工艺定时效验
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void MesApplyTime(object source, ElapsedEventArgs e)
        {
            string msg = "";
            //MesRecipeStruct techRequest = new MesRecipeStruct();
            MesConfig cfg = MesDefine.GetMesCfg(MesInterface.ApplyTechProParam);
            foreach (var item in cfg.parameter.Values)
            {
                if (item.InUse =="use")
                {
                    for (int i = 0; i < (MesData.mesFrequency == 0 ? 3 : MesData.mesFrequency); i++)
                    {
                        //MES设备参数校验
                        if (!MachineCtrl.GetInstance().EquMesEPTechProParamFormalVerify(item, MesResources.BillNo, ref msg))
                        {
                            //获取报警字符串是否包含"超时"二字,如果没有则跳出循环
                            if (!msg.Contains("超时"))
                            {
                                break;
                            }
                            if (i == 2)
                            {
                                ShowMsgBox.ShowDialog($"获取MES设备参数校验超时失败3次，请检查MES连接状态", MessageType.MsgAlarm);
                            }
                        }
                        else
                        {
                            ShowMsgBox.ShowDialog($"定时测试效验成功", MessageType.MsgWarning);
                            break;
                        }
                    }
                    return;
                }
            }
        }



        /// <summary>
        /// 写工艺定时效验时长
        /// </summary>
        public static void WriteApplyTechTime()
        {
            //保存工艺定时效验时长
            string file, section, key;
            file = Def.GetAbsPathName(Def.MesParameterCfg);
            section = "MESData";

            IniFile.WriteInt(section, nameof(MesApplyTechTime), MesApplyTechTime, file);
        }
        /// <summary>
        /// 读工艺定时效验时长
        /// </summary>
        public static void ReadApplyTechTime()
        {
            try
            {
                string file, section, key;
                file = Def.GetAbsPathName(Def.MesParameterCfg);
                section = "MESData";
                MesApplyTechTime = IniFile.ReadInt(section, nameof(MesApplyTechTime), MesApplyTechTime, file);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }



        public static void ReadRuleEnableConfig()
        {
            try
            {
                string file, section, key;
                file = Def.GetAbsPathName(Def.MesParameterCfg);
                section = "MESData";

                key = $"CodeRule.enable";
                CodeRule = IniFile.ReadBool(section, key, true, file);
                key = $"TimeOutCheck.enable";
                TimeOutCheck = IniFile.ReadBool(section, key, true, file);
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.ToString());
            }

        }

        public static void WriteConfig()
        {
            //保存条码规则使能
            string file, section, key;
            file = Def.GetAbsPathName(Def.MesParameterCfg);
            section = "MESData";
            //IniFile.EmptySection(section, file);
            //key = $"CodeRule.enable";
            IniFile.WriteBool(section, "CodeRule.enable", CodeRule, file);
            IniFile.WriteBool(section, "TimeOutCheck.enable", TimeOutCheck, file);
            //保存工单获取接口路径
            //section = "GetBillInfo";
            //IniFile.WriteString(section, "mesUri", MesUrl,file);

        }

        /// <summary>
        /// 写超时时间
        /// </summary>
        public static void WriteTime()
        {
            try
            {
                string file, section, key;
                file = Def.GetAbsPathName(Def.MesParameterCfg);
                section = "MESData";
                key = $"mesinterfaceTimeOut";

                //IniFile.EmptySection(section, file);

                IniFile.WriteInt(section, nameof(mesinterfaceTimeOut), mesinterfaceTimeOut, file);
                IniFile.WriteInt(section, nameof(mesFrequency), mesFrequency, file);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            
        }
        /// <summary>
        /// 读超时时间
        /// </summary>
        public static void ReadTime()
        {
            try
            {
                string file, section, key;
                file = Def.GetAbsPathName(Def.MesParameterCfg);
                section = "MESData";
                MesData.mesinterfaceTimeOut = IniFile.ReadInt(section, nameof(MesData.mesinterfaceTimeOut), mesinterfaceTimeOut, file);
                MesData.mesFrequency = IniFile.ReadInt(section, nameof(MesData.mesFrequency), mesFrequency, file);
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.ToString());
            }

        }


        public static void writeBakingNgType(string runName, string runKey,int value)
        {
            try
            {
                string section, key, file;
                section = runName;
                file = Def.GetAbsPathName(Def.MesParameterCfg);
                IniFile.WriteInt(section, runKey, value, file);
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.ToString());
            }


        }
        public static void readBakingNgType(string runName, string runKey,ref int value)
        {
            try
            {
                string section, key, file;
                section = runName;
                file = Def.GetAbsPathName(Def.MesParameterCfg);
                value = IniFile.ReadInt(section, runKey, 0, file);

            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.ToString()); ;
            }

        }
    }





    #region // Mes资源参数

    /// <summary>
    /// Mes资源参数包含信息
    /// </summary>
    public struct ResourcesStruct
    {
        public string EquipmentID;      // 设备编码
        public string EquipmentName;    // 设备名称
        public string ProcessID;        // 工序编码
        public string ProcessName;      // 工序名称
        public string WorkSection;      // 工段
    }



    /// <summary>
    /// Mes资源参数
    /// </summary>
    public static class MesResources
    {
        public static ResourcesStruct Group;                    // 组资源信息
        public static ResourcesStruct[,] OvenCavity;            // 干燥炉资源信息[干燥炉，炉腔]
        public static ResourcesStruct Onload;                   // 上料资源信息
        public static ResourcesStruct Offload;                  // 下料资源信息
        public static ResourcesStruct Heartbeat;                // 心跳资源信息

        public static int HeartbeatInterval;                    // 心跳时间间隔：秒s

        public static string BillNo;                            // 工单号
        public static string BillNum;                           // 工单号包含的工单数量

        /// <summary>
        /// 读取Mes资源参数配置
        /// </summary>
        public static void ReadConfig()
        {
            try
            {
                if (null == OvenCavity)
                {
                    OvenCavity = new ResourcesStruct[(int)OvenInfoCount.OvenCount, (int)OvenRowCol.MaxRow];
                    for (int ovenIdx = 0; ovenIdx < OvenCavity.GetLength(0); ovenIdx++)
                    {
                        for (int i = 0; i < OvenCavity.GetLength(1); i++)
                        {
                            OvenCavity[ovenIdx, i] = new ResourcesStruct();
                        }
                    }
                }
                string file, section, key;
                file = Def.GetAbsPathName(Def.MesParameterCfg);
                section = "MesResources";

                key = $"Group.EquipmentID";
                Group.EquipmentID = IniFile.ReadString(section, key, "", file);
                key = $"Group.EquipmentName";
                Group.EquipmentName = IniFile.ReadString(section, key, "", file);
                key = $"Group.ProcessID";
                Group.ProcessID = IniFile.ReadString(section, key, "", file);
                key = $"Group.ProcessName";
                Group.ProcessName = IniFile.ReadString(section, key, "", file);
                key = $"Group.WorkSection";
                Group.WorkSection = IniFile.ReadString(section, key, "", file);
                for (int ovenIdx = 0; ovenIdx < OvenCavity.GetLength(0); ovenIdx++)
                {
                    for (int i = 0; i < OvenCavity.GetLength(1); i++)
                    {
                        key = $"OvenCavity[{ovenIdx}, {i}].EquipmentID";
                        OvenCavity[ovenIdx, i].EquipmentID = IniFile.ReadString(section, key, "", file);
                        key = $"OvenCavity[{ovenIdx}, {i}].EquipmentName";
                        OvenCavity[ovenIdx, i].EquipmentName = IniFile.ReadString(section, key, "", file);
                        key = $"OvenCavity[{ovenIdx}, {i}].ProcessID";
                        OvenCavity[ovenIdx, i].ProcessID = IniFile.ReadString(section, key, "", file);
                        key = $"OvenCavity[{ovenIdx}, {i}].ProcessName";
                        OvenCavity[ovenIdx, i].ProcessName = IniFile.ReadString(section, key, "", file);
                        key = $"OvenCavity[{ovenIdx}, {i}].WorkSection";
                        OvenCavity[ovenIdx, i].WorkSection = IniFile.ReadString(section, key, "", file);
                    }
                }
                key = $"Onload.EquipmentID";
                Onload.EquipmentID = IniFile.ReadString(section, key, "", file);
                key = $"Onload.EquipmentName";
                Onload.EquipmentName = IniFile.ReadString(section, key, "", file);
                key = $"Onload.ProcessID";
                Onload.ProcessID = IniFile.ReadString(section, key, "", file);
                key = $"Onload.ProcessName";
                Onload.ProcessName = IniFile.ReadString(section, key, "", file);
                key = $"Onload.WorkSection";
                Onload.WorkSection = IniFile.ReadString(section, key, "", file);

                key = $"Offload.EquipmentID";
                Offload.EquipmentID = IniFile.ReadString(section, key, "", file);
                key = $"Offload.EquipmentName";
                Offload.EquipmentName = IniFile.ReadString(section, key, "", file);
                key = $"Offload.ProcessID";
                Offload.ProcessID = IniFile.ReadString(section, key, "", file);
                key = $"Offload.ProcessName";
                Offload.ProcessName = IniFile.ReadString(section, key, "", file);
                key = $"Offload.WorkSection";
                Offload.WorkSection = IniFile.ReadString(section, key, "", file);

                key = $"Heartbeat.EquipmentID";
                Heartbeat.EquipmentID = IniFile.ReadString(section, key, "", file);
                key = $"Heartbeat.EquipmentName";
                Heartbeat.EquipmentName = IniFile.ReadString(section, key, "", file);
                key = $"Heartbeat.ProcessID";
                Heartbeat.ProcessID = IniFile.ReadString(section, key, "", file);
                key = $"Heartbeat.ProcessName";
                Heartbeat.ProcessName = IniFile.ReadString(section, key, "", file);
                key = $"Heartbeat.WorkSection";
                Heartbeat.WorkSection = IniFile.ReadString(section, key, "", file);

                key = $"HeartbeatInterval";
                HeartbeatInterval = IniFile.ReadInt(section, key, 10, file);
                key = $"BillNo";
                BillNo = IniFile.ReadString(section, key, "", file);
                key = $"BillNum";
                BillNum = IniFile.ReadString(section, key, "", file);
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.ToString());
            }
            
        }

        /// <summary>
        /// 保存Mes资源参数配置
        /// </summary>
        public static void WriteConfig()
        {
            string file, section, key;
            file = Def.GetAbsPathName(Def.MesParameterCfg);
            section = "MesResources";

            IniFile.EmptySection(section, file);

            for(int ovenIdx = 0; ovenIdx < OvenCavity.GetLength(0); ovenIdx++)
            {
                for(int i = 0; i < OvenCavity.GetLength(1); i++)
                {
                    key = $"OvenCavity[{ovenIdx}, {i}].EquipmentID";
                    IniFile.WriteString(section, key, OvenCavity[ovenIdx, i].EquipmentID, file);
                    key = $"OvenCavity[{ovenIdx}, {i}].EquipmentName";
                    IniFile.WriteString(section, key, OvenCavity[ovenIdx, i].EquipmentName, file);
                    key = $"OvenCavity[{ovenIdx}, {i}].ProcessID";
                    IniFile.WriteString(section, key, OvenCavity[ovenIdx, i].ProcessID, file);
                    key = $"OvenCavity[{ovenIdx}, {i}].ProcessName";
                    IniFile.WriteString(section, key, OvenCavity[ovenIdx, i].ProcessName, file);
                    key = $"OvenCavity[{ovenIdx}, {i}].WorkSection";
                    IniFile.WriteString(section, key, OvenCavity[ovenIdx, i].WorkSection, file);
                }
            }
            string[] resStr = new string[] { "Group", "Onload", "Offload", "Heartbeat" };
            ResourcesStruct[] resStruct = new ResourcesStruct[] { Group, Onload, Offload, Heartbeat };
            for(int i = 0; i < resStruct.Length; i++)
            {
                key = $"{resStr[i]}.EquipmentID";
                IniFile.WriteString(section, key, resStruct[i].EquipmentID, file);
                key = $"{resStr[i]}.EquipmentName";
                IniFile.WriteString(section, key, resStruct[i].EquipmentName, file);
                key = $"{resStr[i]}.ProcessID";
                IniFile.WriteString(section, key, resStruct[i].ProcessID, file);
                key = $"{resStr[i]}.ProcessName";
                IniFile.WriteString(section, key, resStruct[i].ProcessName, file);
                key = $"{resStr[i]}.WorkSection";
                IniFile.WriteString(section, key, resStruct[i].WorkSection, file);
            }
            key = $"HeartbeatInterval";
            IniFile.WriteInt(section, key, HeartbeatInterval, file);
            key = $"BillNo";
            IniFile.WriteString(section, key, BillNo, file);
            key = $"BillNum";
            IniFile.WriteString(section, key, BillNum, file);
        }
    }
    #endregion

    #region // FTP配置参数定义

    /// <summary>
    /// FTP配置参数定义
    /// </summary>
    public static class FTPDefine
    {
        public static string FilePath;        // FTP文件路径
        public static string User;            // FTP用户名
        public static string Password;        // FTP密码

        public static void ReadConfig()
        {
            string file, section;
            file = Def.GetAbsPathName(Def.MesParameterCfg);
            section = "FTPClient";

            FilePath = IniFile.ReadString(section, nameof(FilePath), "", file);
            User = IniFile.ReadString(section, nameof(User), "", file);
            Password = IniFile.ReadString(section, nameof(Password), "", file);
        }

        public static void WriteConfig()
        {
            string file, section;
            file = Def.GetAbsPathName(Def.MesParameterCfg);
            section = "FTPClient";

            IniFile.EmptySection(section, file);

            IniFile.WriteString(section, nameof(FilePath), FilePath, file);
            IniFile.WriteString(section, nameof(User), User, file);
            IniFile.WriteString(section, nameof(Password), Password, file);
        }

    }
    #endregion

}
