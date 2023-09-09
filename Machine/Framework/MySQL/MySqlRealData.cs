using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace Machine
{
    /// <summary>
    /// real_data          工艺参数表
    /// </summary>
    static class MySqlRealData
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
                Def.WriteLog("MySqlRealData.Open()", ex.Message);
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
                    Def.WriteLog("MySqlRealData.Reconnect()", ex.Message);
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
            string sql = @"CREATE TABLE IF NOT EXISTS real_data(id bigint primary key not null auto_increment
                , equipment_id VarChar(50) NOT NULL, process_code VarChar(50) NOT NULL, shift  VarChar(50) NOT NULL, station_id  VarChar(50)
                , tool_code VarChar(50) NOT NULL, tool_name VarChar(50), set_UV decimal(10,3), set_value decimal(10,3)
                , set_LV decimal(10,3), act_value nvarchar(100) NOT NULL, unit VarChar(50) NOT NULL, batch_code VarChar(50) NOT NULL
                , bill_no VarChar(50) NOT NULL, create_date datetime NOT NULL, memo VarChar(50) NOT NULL
                , read_flag Tinyint(1) NOT NULL DEFAULT 0) ENGINE=InnoDB DEFAULT CHARSET=utf8";

            MySqlHelper.ExecuteNonQuery(mySql, CommandType.Text, sql);
        }

        public static int InsertRecord(EquipmentParameter param, List<EquipmentParamData> data)
        {
            if(mySql.State != ConnectionState.Open)
            {
                return -1;
            }
            int result = -1;
            lock(dataLock)
            {
                string sql = @"INSERT INTO real_data(equipment_id, process_code, shift, create_date, memo, station_id, tool_code, tool_name, unit, set_UV, set_value, set_LV, act_value, batch_code, bill_no) 
                                        VALUES(@equipment_id, @process_code, @shift, @create_date, @memo, @station_id, @tool_code, @tool_name, @unit, @set_UV, @set_value, @set_LV, @act_value, @batch_code, @bill_no)";

                List<MySqlParameter> sqlPara = new List<MySqlParameter>();
                sqlPara.Add(new MySqlParameter("@equipment_id", param.equipment_id));
                sqlPara.Add(new MySqlParameter("@process_code", param.process_code));
                sqlPara.Add(new MySqlParameter("@shift", param.shift));
                sqlPara.Add(new MySqlParameter("@create_date", param.create_date));
                sqlPara.Add(new MySqlParameter("@memo", param.memo));
                sqlPara.Add(new MySqlParameter());
                sqlPara.Add(new MySqlParameter());
                sqlPara.Add(new MySqlParameter());
                sqlPara.Add(new MySqlParameter());
                sqlPara.Add(new MySqlParameter());
                sqlPara.Add(new MySqlParameter());
                sqlPara.Add(new MySqlParameter());
                sqlPara.Add(new MySqlParameter());
                sqlPara.Add(new MySqlParameter());
                sqlPara.Add(new MySqlParameter());

                MySqlTransaction trans = mySql.BeginTransaction();
                foreach(var item in data)
                {
                    int idx = 5;
                    sqlPara[idx++] = new MySqlParameter($"@{nameof(item.station_id)}", item.station_id);
                    sqlPara[idx++] = new MySqlParameter($"@{nameof(item.bill_no)}", item.bill_no);
                    sqlPara[idx++] = new MySqlParameter($"@{nameof(item.batch_code)}", item.batch_code);
                    sqlPara[idx++] = new MySqlParameter($"@{nameof(item.tool_code)}", item.tool_code);
                    sqlPara[idx++] = new MySqlParameter($"@{nameof(item.tool_name)}", item.tool_name);
                    sqlPara[idx++] = new MySqlParameter($"@{nameof(item.unit)}", item.unit);
                    if (0 == item.set_UV)
                    {
                        sqlPara[idx++] = new MySqlParameter($"@{nameof(item.set_UV)}", null);
                    } 
                    else
                    {
                        sqlPara[idx++] = new MySqlParameter($"@{nameof(item.set_UV)}", item.set_UV);
                    }
                    if(0 == item.set_value)
                    {
                        sqlPara[idx++] = new MySqlParameter($"@{nameof(item.set_value)}", null);
                    }
                    else
                    {
                        sqlPara[idx++] = new MySqlParameter($"@{nameof(item.set_value)}", item.set_value);
                    }
                    if(0 == item.set_LV)
                    {
                        sqlPara[idx++] = new MySqlParameter($"@{nameof(item.set_LV)}", null);
                    }
                    else
                    {
                        sqlPara[idx++] = new MySqlParameter($"@{nameof(item.set_LV)}", item.set_LV);
                    }
                    sqlPara[idx++] = new MySqlParameter($"@{nameof(item.act_value)}", item.act_value);

                    result = MySqlHelper.ExecuteNonQuery(trans, CommandType.Text, sql, sqlPara.ToArray());
                }
                trans.Commit();
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
                string sql = @"DELETE FROM real_data WHERE read_flag<>0 AND create_date BETWEEN @startTime AND @endTime";

                List<MySqlParameter> sqlPara = new List<MySqlParameter>();
                sqlPara.Add(new MySqlParameter("@startTime", startTime));
                sqlPara.Add(new MySqlParameter("@endTime", endTime));

                result = MySqlHelper.ExecuteNonQuery(mySql, CommandType.Text, sql, sqlPara.ToArray());
            }
            return result;
        }

    }

    public struct EquipmentParameter
    {
        public string equipment_id;     // 设备编号
        public string process_code;     // 工序编码
        public string shift;            // 当前班次
        public DateTime create_date;    // 采集时间
        public string memo;             // 备注
        public bool read_flag;          // 读写标记位：默认0代表未同步   MES同步后变为1
    }

    public struct EquipmentParamData
    {
        public string station_id;       // 腔体位置
        public string bill_no;          // 工单号
        public string batch_code;       // 电芯码
        public string tool_code;        // 参数代码
        public string tool_name;        // 参数名称
        public string unit;             // 单位
        public double set_UV;           // 参数值上限
        public double set_value;        // 参数值
        public double set_LV;           // 参数值下限
        public string act_value;        // 实际值
    }
}
