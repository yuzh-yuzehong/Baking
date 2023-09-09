using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace Machine
{
    /// <summary>
    /// uploading_record_sheet      物料卸料记录表
    /// </summary>
    static class MySqlUploadingRecord
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
                Def.WriteLog("MySqlUploadingRecord.Open()", ex.Message);
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
                    Def.WriteLog("MySqlUploadingRecord.Reconnect()", ex.Message);
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
            string sql = @"CREATE TABLE IF NOT EXISTS uploading_record_sheet(id bigint primary key not null auto_increment
                , equipment_id VarChar(50) NOT NULL, process_code VarChar(50) NOT NULL, bar_code  VarChar(50) NOT NULL, bill_no VarChar(50) NOT NULL
                , create_time datetime NOT NULL, number decimal(10,3) NOT NULL, creator VarChar(50) NOT NULL, uploading_time datetime NOT NULL
                , read_flag Tinyint(1) NOT NULL DEFAULT 0) ENGINE=InnoDB DEFAULT CHARSET=utf8";

            MySqlHelper.ExecuteNonQuery(mySql, CommandType.Text, sql);
        }

        public static int InsertRecord(EquipmentUploadingRecord data, Pallet[] plt)
        {
            if(mySql.State != ConnectionState.Open)
            {
                return -1;
            }
            int result = -1;
            lock(dataLock)
            {
                string sql = @"INSERT INTO uploading_record_sheet
                                        (equipment_id, process_code, bar_code, bill_no, create_time, number, creator, uploading_time) 
                                        VALUES(@equipment_id, @process_code, @bar_code, @bill_no, @create_time, @number, @creator, @uploading_time)";

                List<MySqlParameter> sqlPara = new List<MySqlParameter>();
                sqlPara.Add(new MySqlParameter("@equipment_id", data.equipment_id));
                sqlPara.Add(new MySqlParameter("@process_code", data.process_code));;
                sqlPara.Add(new MySqlParameter("@number", data.number));
                sqlPara.Add(new MySqlParameter("@creator", data.creator));
                sqlPara.Add(new MySqlParameter("@bill_no", data.bill_no));;
                sqlPara.Add(new MySqlParameter("@create_time", data.create_time));
                sqlPara.Add(new MySqlParameter("@bar_code", data.bar_code));
                sqlPara.Add(new MySqlParameter("@uploading_time", data.uploading_time));

                MySqlTransaction trans = mySql.BeginTransaction();
                foreach(var item in plt)
                {
                    sqlPara[sqlPara.Count - 1] = new MySqlParameter("@uploading_time", item.UploadingTime);
                    for(int row = 0; row < item.MaxRow; row++)
                    {
                        for(int col = 0; col < item.MaxCol; col++)
                        {
                            if((BatteryStatus.OK == item.Battery[row, col].Type) || (BatteryStatus.NG == item.Battery[row, col].Type))
                            {
                                sqlPara[sqlPara.Count - 1] = new MySqlParameter("@bar_code", item.Battery[row, col].Code);
                                result = MySqlHelper.ExecuteNonQuery(trans, CommandType.Text, sql, sqlPara.ToArray());
                            }
                        }
                    }
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
                string sql = @"DELETE FROM uploading_record_sheet WHERE read_flag<>0 AND uploading_time BETWEEN @startTime AND @endTime";

                List<MySqlParameter> sqlPara = new List<MySqlParameter>();
                sqlPara.Add(new MySqlParameter("@startTime", startTime));
                sqlPara.Add(new MySqlParameter("@endTime", endTime));

                result = MySqlHelper.ExecuteNonQuery(mySql, CommandType.Text, sql, sqlPara.ToArray());
            }
            return result;
        }
    }

    public struct EquipmentUploadingRecord
    {
        public string bar_code;         // 批次条码（半成品批次）
        public string bill_no;          // 工单号
        public DateTime create_time;    // 创建时间
        public string process_code;     // 工序编码
        public string equipment_id;     // 设备编号
        public double number;           // 数量
        public string creator;          // 生产操作员
        public DateTime uploading_time; // 卸料时间
        public bool read_flag;          // 读写标记位：默认0代表未同步   MES同步后变为1
    }
}
