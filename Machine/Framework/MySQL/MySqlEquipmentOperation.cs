using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace Machine
{
    /// <summary>
    /// equipment_operation_record         设备运行记录表
    /// </summary>
    static class MySqlEquipmentOperation
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
                Def.WriteLog("MySqlEquipmentOperation.Open()", ex.Message);
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
                    Def.WriteLog("MySqlEquipmentOperation.Reconnect()", ex.Message);
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
            string sql = @"CREATE TABLE IF NOT EXISTS equipment_operation_record(id bigint primary key not null auto_increment
                , equipment_id VarChar(50) NOT NULL, process_code VarChar(50) NOT NULL, state_code  VarChar(50) NOT NULL
                , state_name VarChar(50) NOT NULL, start_date datetime NOT NULL, end_date datetime DEFAULT NULL
                , read_flag Tinyint(1) NOT NULL DEFAULT 0) ENGINE=InnoDB DEFAULT CHARSET=utf8";

            MySqlHelper.ExecuteNonQuery(mySql, CommandType.Text, sql);
        }

        /// <summary>
        /// 插入记录
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static int InsertRecord(EquipmentOperationRecord data)
        {
            if(mySql.State != ConnectionState.Open)
            {
                return -1;
            }
            int result = -1;
            lock(dataLock)
            {
                try
                {
                    string sql = @"SELECT COUNT(*) FROM equipment_operation_record WHERE equipment_id=@equipment_id AND process_code=@process_code AND state_code=@state_code AND end_date is null";
                    List<MySqlParameter> sqlPara = new List<MySqlParameter>();
                    sqlPara.Add(new MySqlParameter("@equipment_id", data.equipment_id));
                    sqlPara.Add(new MySqlParameter("@process_code", data.process_code));
                    sqlPara.Add(new MySqlParameter("@state_code", data.state_code));
                    // 无相同的记录，先更新结束时间，再插入
                    if (Convert.ToInt32(MySqlHelper.ExecuteScalar(mySql, CommandType.Text, sql, sqlPara.ToArray())) < 1)
                    {
                        // 先更新上一条记录结束时间
                        UpdatePreviousRecordEndDate(data);

                        // 插入时无结束时间
                        sql = @"INSERT INTO equipment_operation_record(equipment_id, process_code, state_code, state_name, start_date) 
                                                        VALUES(@equipment_id, @process_code, @state_code, @state_name, @start_date)";

                        sqlPara.Add(new MySqlParameter("@state_name", data.state_name));
                        sqlPara.Add(new MySqlParameter("@start_date", data.start_date));
                        result = MySqlHelper.ExecuteNonQuery(mySql, CommandType.Text, sql, sqlPara.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());

                }
               
            }
            return result;
        }

        /// <summary>
        /// 更新上一条记录的结束时间
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private static int UpdatePreviousRecordEndDate(EquipmentOperationRecord data)
        {
            if(mySql.State != ConnectionState.Open)
            {
                return -1;
            }
            string sql = @"UPDATE equipment_operation_record SET end_date = @end_date WHERE equipment_id = @equipment_id AND process_code = @process_code AND end_date is null";
            
            List<MySqlParameter> sqlPara = new List<MySqlParameter>();
            sqlPara.Add(new MySqlParameter("@end_date", data.start_date));
            sqlPara.Add(new MySqlParameter("@equipment_id", data.equipment_id));
            sqlPara.Add(new MySqlParameter("@process_code", data.process_code));

            return MySqlHelper.ExecuteNonQuery(mySql, CommandType.Text, sql, sqlPara.ToArray());
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
                string sql = @"DELETE FROM equipment_operation_record WHERE read_flag<>0 AND end_date BETWEEN @startTime AND @endTime";

                List<MySqlParameter> sqlPara = new List<MySqlParameter>();
                sqlPara.Add(new MySqlParameter("@startTime", startTime));
                sqlPara.Add(new MySqlParameter("@endTime", endTime));

                result = MySqlHelper.ExecuteNonQuery(mySql, CommandType.Text, sql, sqlPara.ToArray());
            }
            return result;
        }

    }

    public struct EquipmentOperationRecord
    {
        public string equipment_id;     // 设备编号
        public string process_code;     // 工序编码
        public string state_code;       // 状态代码
        public string state_name;       // 状态名称
        public DateTime start_date;     // 开始时间
        public DateTime end_date;       // 结束时间
        public bool read_flag;          // 读写标记位：默认0代表未同步   MES同步后变为1
    }
}
