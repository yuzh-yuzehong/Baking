using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace Machine
{
    /// <summary>
    /// production_record_sheet      生产批次信息记录表
    /// </summary>
    static class MySqlProductionRecord
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
                Def.WriteLog("MySqlProductionRecord.Open()", ex.Message);
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
                    Def.WriteLog("MySqlProductionRecord.Reconnect()", ex.Message);
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
            string sql = @"CREATE TABLE IF NOT EXISTS production_record_sheet(id bigint primary key not null auto_increment
                , equipment_id VarChar(50) NOT NULL, process_code VarChar(50) NOT NULL, bar_code  VarChar(50) NOT NULL, bill_no VarChar(50) NOT NULL
                , start_date datetime NOT NULL, end_date datetime NOT NULL, shift VarChar(50) NOT NULL, number decimal(10,3) NOT NULL
                , creator VarChar(50) NOT NULL, out_time datetime NOT NULL, out_man VarChar(50) NOT NULL, pre_process VarChar(50) NOT NULL
                , read_flag Tinyint(1) NOT NULL DEFAULT 0) ENGINE=InnoDB DEFAULT CHARSET=utf8";

            MySqlHelper.ExecuteNonQuery(mySql, CommandType.Text, sql);
        }

        public static int InsertRecord(EquipmentProductionRecord data, Pallet[] plt)
        {
            if(mySql.State != ConnectionState.Open)
            {
                return -1;
            }
            int result = -1;
            lock(dataLock)
            {
                string sql = @"INSERT INTO production_record_sheet
                                        (equipment_id, process_code, bar_code, bill_no, start_date, end_date, shift, number, creator, out_time, out_man, pre_process) 
                                        VALUES(@equipment_id, @process_code, @bar_code, @bill_no, @start_date, @end_date, @shift, @number, @creator, @out_time, @out_man, @pre_process)";

                List<MySqlParameter> sqlPara = new List<MySqlParameter>();
                sqlPara.Add(new MySqlParameter("@equipment_id", data.equipment_id));
                sqlPara.Add(new MySqlParameter("@process_code", data.process_code));
                sqlPara.Add(new MySqlParameter("@shift", data.shift));
                sqlPara.Add(new MySqlParameter("@number", data.number));
                sqlPara.Add(new MySqlParameter("@creator", data.creator));
                sqlPara.Add(new MySqlParameter("@out_time", data.out_time));
                sqlPara.Add(new MySqlParameter("@out_man", data.out_man));
                sqlPara.Add(new MySqlParameter("@pre_process", data.pre_process));
                sqlPara.Add(new MySqlParameter("@bill_no", data.bill_no));
                sqlPara.Add(new MySqlParameter("@start_date", data.start_date));
                sqlPara.Add(new MySqlParameter("@end_date", data.end_date));
                sqlPara.Add(new MySqlParameter("@bar_code", data.bar_code));

                MySqlTransaction trans = mySql.BeginTransaction();
                foreach(var item in plt)
                {
                    sqlPara[sqlPara.Count - 3] = new MySqlParameter("@start_date", item.StartDate);
                    sqlPara[sqlPara.Count - 2] = new MySqlParameter("@end_date", item.EndDate);
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
                string sql = @"DELETE FROM production_record_sheet WHERE read_flag<>0 AND end_date BETWEEN @startTime AND @endTime";

                List<MySqlParameter> sqlPara = new List<MySqlParameter>();
                sqlPara.Add(new MySqlParameter("@startTime", startTime));
                sqlPara.Add(new MySqlParameter("@endTime", endTime));

                result = MySqlHelper.ExecuteNonQuery(mySql, CommandType.Text, sql, sqlPara.ToArray());
            }
            return result;
        }
    }

    public struct EquipmentProductionRecord
    {
        public string equipment_id;     // 设备编号
        public string process_code;     // 工序编码
        public string bar_code;         // 批次条码（半成品批次）
        public string bill_no;          // 工单号
        public DateTime start_date;     // 生产开始时间
        public DateTime end_date;       // 生产结束时间
        public DateTime create_time;    // 创建时间
        public string shift;            // 班组名称
        public double number;           // 数量
        public string creator;          // 生产操作员
        public DateTime out_time;       // 转出时间
        public string out_man;          // 转出人
        public string pre_process;      // 前工序批次/条码号
        public bool read_flag;          // 读写标记位：默认0代表未同步   MES同步后变为1
    }
}
