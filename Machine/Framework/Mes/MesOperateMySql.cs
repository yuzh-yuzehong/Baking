using HelperLibrary;
using System;
using System.Collections.Generic;

namespace Machine
{
    /// <summary>
    /// MES操作数据库增删改查
    /// </summary>
    static class MesOperateMySql
    {
        /// <summary>
        /// 打开MySql
        /// </summary>
        /// <returns></returns>
        public static bool OpenMesMySql()
        {
            string section = MachineCtrl.GetInstance().MachineName;

            string MySqlDB = IniFile.ReadString(section, "MySqlDB", "MES", Def.GetAbsPathName(Def.ModuleExCfg));
            string MySqlIP = IniFile.ReadString(section, "MySqlIP", "localhost", Def.GetAbsPathName(Def.ModuleExCfg));
            int MySqlPort = IniFile.ReadInt(section, "MySqlPort", 3306, Def.GetAbsPathName(Def.ModuleExCfg));
            string MySqlUser = IniFile.ReadString(section, "MySqlUser", "root", Def.GetAbsPathName(Def.ModuleExCfg));
            string MySqlPassword = IniFile.ReadString(section, "MySqlPassword", "123456", Def.GetAbsPathName(Def.ModuleExCfg));

            if(!MySqlEquipmentReal.Open(MySqlDB, MySqlIP, MySqlPort, MySqlUser, MySqlPassword))
            {
                ShowMsgBox.ShowDialog("MySQL数据库连接失败", MessageType.MsgAlarm);
                return false;
            }
            MySqlEquipmentReal.CreateTable();
            if(!MySqlEquipmentOperation.Open(MySqlDB, MySqlIP, MySqlPort, MySqlUser, MySqlPassword))
            {
                ShowMsgBox.ShowDialog("MySQL数据库连接失败", MessageType.MsgAlarm);
                return false;
            }
            MySqlEquipmentOperation.CreateTable();
            if(!MySqlEquipmentAlarm.Open(MySqlDB, MySqlIP, MySqlPort, MySqlUser, MySqlPassword))
            {
                ShowMsgBox.ShowDialog("MySQL数据库连接失败", MessageType.MsgAlarm);
                return false;
            }
            MySqlEquipmentAlarm.CreateTable();
            if(!MySqlProductionRecord.Open(MySqlDB, MySqlIP, MySqlPort, MySqlUser, MySqlPassword))
            {
                ShowMsgBox.ShowDialog("MySQL数据库连接失败", MessageType.MsgAlarm);
                return false;
            }
            MySqlProductionRecord.CreateTable();
            if(!MySqlRealData.Open(MySqlDB, MySqlIP, MySqlPort, MySqlUser, MySqlPassword))
            {
                ShowMsgBox.ShowDialog("MySQL数据库连接失败", MessageType.MsgAlarm);
                return false;
            }
            MySqlRealData.CreateTable();
            //if (!MySqlFeedingRecord.Open(MySqlDB, MySqlIP, MySqlPort, MySqlUser, MySqlPassword))
            //{
            //    ShowMsgBox.ShowDialog("MySQL数据库连接失败", MessageType.MsgAlarm);
            //    return false;
            //}
            //MySqlFeedingRecord.CreateTable();
            //if (!MySqlUploadingRecord.Open(MySqlDB, MySqlIP, MySqlPort, MySqlUser, MySqlPassword))
            //{
            //    ShowMsgBox.ShowDialog("MySQL数据库连接失败", MessageType.MsgAlarm);
            //    return false;
            //}
            //MySqlUploadingRecord.CreateTable();


            return true;
        }

        /// <summary>
        /// 关闭MySql连接
        /// </summary>
        public static void CloseMesMySql()
        {
            MySqlEquipmentAlarm.Close();
            MySqlEquipmentOperation.Close();
            MySqlEquipmentReal.Close();
            MySqlProductionRecord.Close();
            MySqlRealData.Close();
        }

        /// <summary>
        /// 检查MySql的连接状态
        /// </summary>
        /// <returns></returns>
        public static bool MySqlIsOpen()
        {
            if (!MySqlEquipmentAlarm.IsOpen())
            {
                return false;
            }
            if(!MySqlEquipmentOperation.IsOpen())
            {
                return false;
            }
            if(!MySqlEquipmentReal.IsOpen())
            {
                return false;
            }
            if(!MySqlProductionRecord.IsOpen())
            {
                return false;
            }
            if(!MySqlRealData.IsOpen())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// MySql服务重连
        /// </summary>
        /// <returns></returns>
        public static bool MySqlReconnect()
        {
            if(!MySqlEquipmentAlarm.Reconnect())
            {
                return false;
            }
            if(!MySqlEquipmentOperation.Reconnect())
            {
                return false;
            }
            if(!MySqlEquipmentReal.Reconnect())
            {
                return false;
            }
            if(!MySqlProductionRecord.Reconnect())
            {
                return false;
            }
            if(!MySqlRealData.Reconnect())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 删除数据库中超期的记录
        /// </summary>
        public static void DeleteRecord(DateTime startDT, DateTime endDT)
        {
            if(MySqlEquipmentAlarm.DeleteRecord(startDT, endDT) > 0)
            {
                Def.WriteLog("DeleteRecord()", $"MySqlEquipmentAlarm 表中{endDT.ToString(Def.DateFormal)}之前的记录已被删除", LogType.Success);
            }
            if(MySqlEquipmentOperation.DeleteRecord(startDT, endDT) > 0)
            {
                Def.WriteLog("DeleteRecord()", $"MySqlEquipmentOperation 表中{endDT.ToString(Def.DateFormal)}之前的记录已被删除", LogType.Success);
            }
            if(MySqlProductionRecord.DeleteRecord(startDT, endDT) > 0)
            {
                Def.WriteLog("DeleteRecord()", $"MySqlProductionRecord 表中{endDT.ToString(Def.DateFormal)}之前的记录已被删除", LogType.Success);
            }
            if(MySqlRealData.DeleteRecord(startDT, endDT) > 0)
            {
                Def.WriteLog("DeleteRecord()", $"MySqlRealData 表中{endDT.ToString(Def.DateFormal)}之前的记录已被删除", LogType.Success);
            }
        }

        /// <summary>
        /// 设备状态实时表
        /// </summary>
        /// <param name="mc"></param>
        public static bool EquipmentReal(MesMCState mc, ResourcesStruct rs)
        {
            EquipmentRealData data = new EquipmentRealData();
            data.equipment_id = rs.EquipmentID;
            data.process_code = rs.ProcessID;
            data.state_code = $"{((int)mc):00}";
            switch(mc)
            {
                case MesMCState.Running:
                    data.state_name = "自动运行";
                    break;
                case MesMCState.Waiting:
                    data.state_name = "待机";
                    break;
                case MesMCState.Stop:
                    data.state_name = "停机";
                    break;
                case MesMCState.Alarm:
                    data.state_name = "报警";
                    break;
                case MesMCState.Other:
                    data.state_name = "其它";
                    break;
                default:
                    break;
            }
            data.update_time = DateTime.Now;

            return MySqlEquipmentReal.UpdateRecord(data) > -1;
        }

        /// <summary>
        /// 设备运行状态履历
        /// </summary>
        /// <param name="mc"></param>
        public static bool EquipmentOperation(MesMCState mc, ResourcesStruct rs)
        {
            EquipmentOperationRecord data = new EquipmentOperationRecord();
            data.equipment_id = rs.EquipmentID;
            data.process_code = rs.ProcessID;
            data.state_code = $"{((int)mc):00}";
            switch(mc)
            {
                case MesMCState.Running:
                    data.state_name = "自动运行";
                    break;
                case MesMCState.Waiting:
                    data.state_name = "待机";
                    break;
                case MesMCState.Stop:
                    data.state_name = "停机";
                    break;
                case MesMCState.Alarm:
                    data.state_name = "报警";
                    break;
                case MesMCState.Other:
                    data.state_name = "其它";
                    break;
                default:
                    break;
            }
            data.start_date = DateTime.Now;
            data.end_date = DateTime.Now;

            return MySqlEquipmentOperation.InsertRecord(data) > -1;
        }

        /// <summary>
        /// 设备报警记录表
        /// </summary>
        /// <param name="record"></param>
        public static bool EquipmentAlarm(int msgID, string msg, int msgType, ResourcesStruct rs)
        {
            EquipmentAlarmRecord data = new EquipmentAlarmRecord();
            data.equipment_id = rs.EquipmentID;
            data.process_code = rs.ProcessID;
            data.alarm_code = msgID.ToString();
            data.alarm_memo = msg.Replace("\r", "").Replace("\r", "");
            data.start_date = DateTime.Now;
            data.end_date = DateTime.Now.AddSeconds(Def.GetRandom(10, 300) / 1.0);

            return MySqlEquipmentAlarm.InsertRecord(data) > -1;
        }

        /// <summary>
        /// 设备报警记录表结束时间更新
        /// </summary>
        /// <param name="endDate"></param>
        public static void EquipmentAlarmEndTime(DateTime endDate)
        {
            MySqlEquipmentAlarm.UpdataEndDate(endDate);
        }

        /// <summary>
        /// 生产批次信息记录表
        /// </summary>
        public static bool ProductionRecord(ResourcesStruct rs, Pallet[] plt)
        {
            var data = new EquipmentProductionRecord();
            string pre_process = "";
            var cfg = MesDefine.GetMesCfg(MesInterface.SavePR_ProductRecordList);
            if((null != cfg) && cfg.parameter.ContainsKey(nameof(data.pre_process)))
            {
                pre_process = cfg.parameter[nameof(data.pre_process)].FormulaNo;
            }
            data.equipment_id = rs.EquipmentID;
            data.process_code = rs.ProcessID;
            data.shift = OperationShifts.Shift().Code;
            data.number = 1;
            data.out_time = DateTime.Now;
            data.create_time = DateTime.Now;
            data.pre_process = pre_process;
            data.out_man = data.creator = MachineCtrl.GetInstance().OperaterID;
            data.bill_no = MesResources.BillNo;

            return MySqlProductionRecord.InsertRecord(data, plt) > -1;
        }

        /// <summary>
        /// 工艺参数表
        /// </summary>
        public static bool RealData(ResourcesStruct rs, List<EquipmentParamData> data)
        {
            var param = new EquipmentParameter();
            param.equipment_id = rs.EquipmentID;
            param.process_code = rs.ProcessID;
            param.shift = OperationShifts.Shift().Code;
            param.create_date = DateTime.Now;
            param.memo = "";

            return MySqlRealData.InsertRecord(param, data) > -1;
        }

    }
}
