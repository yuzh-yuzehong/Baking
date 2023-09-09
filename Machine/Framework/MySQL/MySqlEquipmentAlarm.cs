using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace Machine
{
    /// <summary>
    /// equipment_alarm_record      设备报警记录表
    /// </summary>
    static class MySqlEquipmentAlarm
    {
        static MySqlConnection mySql;
        static object dataLock;

        public static bool Open(string db, string ip, int port, string user, string password)
        {
            string conInfo = $"database={db}; server={ip}; port={port}; user={user}; password={password};Allow User Variables=True";
            mySql = new MySqlConnection(conInfo);
            try
            {
                mySql.Open();
                dataLock = new object();
            }
            catch(System.Exception ex)
            {
                Def.WriteLog("MySqlEquipmentAlarm.Open()", ex.Message);
            }
            return (mySql.State == ConnectionState.Open);
        }

        public static void Close()
        {
            if(mySql.State == ConnectionState.Open)
            {
                mySql.Close();
            }
        }

        public static bool IsOpen()
        {
            return (mySql.State == ConnectionState.Open);
        }

        public static bool Reconnect()
        {
            if(mySql.State != ConnectionState.Open)
            {
                try
                {
                    mySql.Open();
                }
                catch(System.Exception ex)
                {
                    Def.WriteLog("MySqlEquipmentAlarm.Reconnect()", ex.Message);
                }
                return (mySql.State == ConnectionState.Open);
            }
            return true;
        }

        public static void CreateTable()
        {
            if(mySql.State != ConnectionState.Open)
            {
                return;
            }
            string sql = @"CREATE TABLE IF NOT EXISTS equipment_alarm_record(id bigint primary key not null auto_increment
                , equipment_id VarChar(50) NOT NULL, process_code VarChar(50) NOT NULL, alarm_code  VarChar(50) NOT NULL
                , alarm_memo VarChar(300) NOT NULL, start_date datetime NOT NULL, end_date datetime DEFAULT NULL
                , read_flag Tinyint(1) NOT NULL DEFAULT 0) ENGINE=InnoDB DEFAULT CHARSET=utf8";

            MySqlHelper.ExecuteNonQuery(mySql, CommandType.Text, sql);
        }

        public static int InsertRecord(EquipmentAlarmRecord data)
        {
            if(mySql.State != ConnectionState.Open)
            {
                return -1;
            }
            int result = -1;
            lock(dataLock)
            {
                string sql = @"INSERT INTO equipment_alarm_record(equipment_id, process_code, alarm_code, alarm_memo, start_date, end_date) 
                                        VALUES(@equipment_id, @process_code, @alarm_code, @alarm_memo, @start_date, @end_date)";

                List<MySqlParameter> sqlPara = new List<MySqlParameter>();
                sqlPara.Add(new MySqlParameter("@equipment_id", data.equipment_id));
                sqlPara.Add(new MySqlParameter("@process_code", data.process_code));
                sqlPara.Add(new MySqlParameter("@alarm_code", data.alarm_code));
                sqlPara.Add(new MySqlParameter("@alarm_memo", data.alarm_memo));
                sqlPara.Add(new MySqlParameter("@start_date", data.start_date));
                sqlPara.Add(new MySqlParameter("@end_date", null));

                result = MySqlHelper.ExecuteNonQuery(mySql, CommandType.Text, sql, sqlPara.ToArray());
            }

            return result;
        }

        public static int UpdataEndDate(DateTime endDate)
        {
            if(mySql.State != ConnectionState.Open)
            {
                return -1;
            }
            int result = -1;
            lock(dataLock)
            {
                string sql = @"UPDATE equipment_alarm_record SET end_date = @end_date WHERE id > 0 AND end_date is null";

                List<MySqlParameter> sqlPara = new List<MySqlParameter>();
                sqlPara.Add(new MySqlParameter("@end_date", endDate));

                result = MySqlHelper.ExecuteNonQuery(mySql, CommandType.Text, sql, sqlPara.ToArray());
            }

            return result;
        }

        public static int DeleteRecord(DateTime startTime, DateTime endTime)
        {
            if(mySql.State != ConnectionState.Open)
            {
                return -1;
            }
            int result = -1;
            lock(dataLock)
            {
                string sql = @"DELETE FROM equipment_alarm_record WHERE read_flag<>0 AND end_date BETWEEN @startTime AND @endTime";

                List<MySqlParameter> sqlPara = new List<MySqlParameter>();
                sqlPara.Add(new MySqlParameter("@startTime", startTime));
                sqlPara.Add(new MySqlParameter("@endTime", endTime));

                result = MySqlHelper.ExecuteNonQuery(mySql, CommandType.Text, sql, sqlPara.ToArray());
            }
            return result;
        }

    }

    public struct EquipmentAlarmRecord
    {
        public string equipment_id;     // 设备编号
        public string process_code;     // 工序编码
        public string alarm_code;       // 报警代码
        public string alarm_memo;       // 报警描述
        public DateTime start_date;     // 开始时间
        public DateTime end_date;       // 结束时间
        public bool read_flag;          // 读写标记位：默认0代表未同步   MES同步后变为1
    }
}
