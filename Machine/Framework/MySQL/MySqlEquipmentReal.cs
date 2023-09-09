using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace Machine
{
    /// <summary>
    /// equipment_real_data           设备状态实时表
    /// </summary>
    static class MySqlEquipmentReal
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
            catch (System.Exception ex)
            {
                Def.WriteLog("MySqlEquipmentReal.Open()", ex.Message);
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
                    Def.WriteLog("MySqlEquipmentReal.Reconnect()", ex.Message);
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
            string sql = @"CREATE TABLE IF NOT EXISTS equipment_real_data(id bigint primary key not null auto_increment
                , equipment_id VarChar(50) NOT NULL, process_code VarChar(50) NOT NULL, state_code  VarChar(50) NOT NULL
                , state_name VarChar(50) NOT NULL, update_time datetime NOT NULL, ud1 VarChar(50)
                , ud2 VarChar(50), ud3 VarChar(50), read_flag Tinyint(1) NOT NULL DEFAULT 0) ENGINE=InnoDB DEFAULT CHARSET=utf8";

            MySqlHelper.ExecuteNonQuery(mySql, CommandType.Text, sql);
        }

        public static int UpdateRecord(EquipmentRealData data)
        {
            if(mySql.State != ConnectionState.Open)
            {
                return -1;
            }
            int result = -1;
            lock(dataLock)
            {
                string sql = $"SELECT COUNT(*) FROM equipment_real_data WHERE equipment_id = @equipment_id";
                List<MySqlParameter> sqlPara = new List<MySqlParameter>();
                sqlPara.Add(new MySqlParameter("@equipment_id", data.equipment_id));

                int count = Convert.ToInt32(MySqlHelper.ExecuteScalar(mySql, CommandType.Text, sql, sqlPara.ToArray()));
                if(count > 0)
                {
                    sql = @"UPDATE equipment_real_data SET state_code = @state_code, state_name = @state_name, update_time = @update_time
                                        , read_flag = @read_flag WHERE equipment_id = @equipment_id";

                    sqlPara.Add(new MySqlParameter("@read_flag", data.read_flag));
                    sqlPara.Add(new MySqlParameter("@state_code", data.state_code));
                    sqlPara.Add(new MySqlParameter("@state_name", data.state_name));
                    sqlPara.Add(new MySqlParameter("@update_time", data.update_time));
                }
                else
                {
                    sql = @"INSERT INTO equipment_real_data(equipment_id, process_code, state_code, state_name, update_time) 
                                        VALUES(@equipment_id, @process_code, @state_code, @state_name, @update_time)";

                    sqlPara.Add(new MySqlParameter("@process_code", data.process_code));
                    sqlPara.Add(new MySqlParameter("@state_code", data.state_code));
                    sqlPara.Add(new MySqlParameter("@state_name", data.state_name));
                    sqlPara.Add(new MySqlParameter("@update_time", data.update_time));
                }

                result = MySqlHelper.ExecuteNonQuery(mySql, CommandType.Text, sql, sqlPara.ToArray());
            }

            return result;
        }

    }

    public struct EquipmentRealData
    {
        public string equipment_id;     // 设备编号
        public string process_code;     // 工序编码
        public string state_code;       // 状态代码
        public string state_name;       // 状态名称
        public DateTime update_time;    // 更新时间
        public string ud1;              // 预留
        public string ud2;              // 预留
        public string ud3;              // 预留
        public bool read_flag;          // 读写标记位：默认0代表未同步   MES同步后变为1
    }
}
