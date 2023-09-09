using System.Data;
using System.Diagnostics;
using SystemControlLibrary;

namespace Machine
{
    public static class DataBaseLog
    {
        #region // 数据库对象字段

        static DataBaseAccess dataBase;    // 数据库连接对象
        static bool isOpen;                // 数据库打开标志
        static object dbLock;              // 数据库同步锁

        #endregion

        #region // 数据库记录集信息枚举

        /// <summary>
        /// 数据库表
        /// </summary>
        public enum LogTableType
        {
            DryingOvenLog = 0,      // 干燥炉Log
            ParameterLog,           // 参数修改Log
            RobotLog,               // 机器人指令Log
            MotorLog,               // 电机点位修改Log

            End,
        };

        /// <summary>
        /// DryingOvenLog记录表中列项
        /// </summary>
        public enum DryingOvenLogColumn
        {
            FormulaID = 0,          // 产品ID
            Operater,               // 操作者
            OptDate,                // 时间

            OvenID,                 // 干燥炉ID
            OvenName,               // 干燥炉名称
            OptMode,                // 模式：手动/自动
            OvenAction,             // 指令动作

            End,
        };

        /// <summary>
        /// ParameterLog记录表中列项
        /// </summary>
        public enum ParameterLogColumn
        {
            FormulaID = 0,          // 产品ID
            Operater,               // 操作者
            OptDate,                // 时间

            ModuleID,               // 模组ID
            ModuleName,             // 模组名称
            ParmName,               // 参数
            OldValue,               // 原值
            NewValue,               // 新值

            End,
        };

        /// <summary>
        /// RobotLog记录表中列项
        /// </summary>
        public enum RobotLogColumn
        {
            FormulaID = 0,          // 产品ID
            Operater,               // 操作者
            OptDate,                // 时间

            RobotID,                // 机器人ID
            RobotName,              // 机器人名称
            OptMode,                // 模式：手动/自动
            SendRecv,               // 发送/接收
            RobotAction,            // 指令动作

            End
        };

        /// <summary>
        /// MotorLog记录表中列项
        /// </summary>
        public enum MotorLogColumn
        {
            FormulaID = 0,          // 产品ID
            Operater,               // 操作者
            OptDate,                // 时间

            MotorID,                // 电机ID
            MotorName,              // 电机名称
            OptMode,                // 模式：手动/自动
            OptAction,              // 指令动作
            OldValue,               // 原位置
            NewValue,               // 新位置

            End
        };

        #endregion

        #region // 表字段样式

        /// <summary>
        /// DryingOvenLog表字段样式
        /// </summary>
        public class OvenLogFormula
        {
            public OvenLogFormula()
            {
            }
            public OvenLogFormula(int _formulaID, int _ovenID, string _ovenName, string _operater, string _date, string _mode, string _action)
            {
                this.formulaID = _formulaID;
                this.ovenID = _ovenID;
                this.ovenName = _ovenName;
                this.operater = _operater;
                this.date = _date;
                this.mode = _mode;
                this.action = _action;
            }

            public int formulaID;          // 当前设备产品配方
            public string operater;        // 操作者
            public string date;            // 时间
            public int ovenID;             // 干燥炉ID
            public string ovenName;        // 干燥炉名称
            public string mode;            // 模式：手动/自动
            public string action;          // 指令动作
        }

        /// <summary>
        /// RobotLog表字段样式
        /// </summary>
        public class RobotLogFormula
        {
            public RobotLogFormula() { }
            public RobotLogFormula(int _formulaID, int _robotID, string _robotName, string _operator, string _date, string _mode, string _sendRecv, string _action)
            {
                this.formulaID = _formulaID;
                this.robotID = _robotID;
                this.robotName = _robotName;
                this.operater = _operator;
                this.date = _date;
                this.mode = _mode;
                this.sendRecv = _sendRecv;
                this.action = _action;
            }

            public int formulaID;          // 当前设备产品配方
            public string operater;        // 操作者
            public string date;            // 时间
            public int robotID;            // 机器人ID
            public string robotName;       // 机器人名
            public string mode;            // 模式：手动/自动
            public string sendRecv;        // 发送/接收
            public string action;          // 指令动作
        }

        /// <summary>
        /// ParameterLog表字段样式
        /// </summary>
        public class ParameterLogFormula
        {
            public ParameterLogFormula() { }
            public ParameterLogFormula(int _formulaID, int _moduleID, string _operater, string _date, string _moduleName, string _paraName, string _oldValue, string _newValue)
            {
                this.formulaID = _formulaID;
                this.operater = _operater;
                this.date = _date;
                this.moduleID = _moduleID;
                this.moduleName = _moduleName;
                this.paraName = _paraName;
                this.oldValue = _oldValue;
                this.newValue = _newValue;
            }

            public int formulaID;          // 当前设备产品配方
            public string operater;        // 操作者
            public string date;            // 时间
            public int moduleID;           // 模组ID
            public string moduleName;      // 模组名
            public string paraName;            // 参数名
            public string oldValue;        // 原值
            public string newValue;        // 新值
        }

        /// <summary>
        /// MotorLog表样式
        /// </summary>
        public class MotorLogFormula
        {
            public MotorLogFormula() { }
            public MotorLogFormula(int _formulaID, string _operater, string _date, int _motorID, string _motorName, string _mode, string _action, string _oldValue, string _newValue)
            {
                this.formulaID = _formulaID;
                this.operater = _operater;
                this.date = _date;
                this.motorID = _motorID;
                this.motorName = _motorName;
                this.mode = _mode;
                this.action = _action;
                this.oldValue = _oldValue;
                this.newValue = _newValue;
            }

            public int formulaID;          // 当前设备产品配方
            public string operater;        // 操作者
            public string date;            // 时间
            public int motorID;           // 电机ID
            public string motorName;      // 电机名
            public string mode;            // 模式：手动/自动
            public string action;          // 指令动作
            public string oldValue;        // 原值
            public string newValue;        // 新值
        }

        #endregion

        #region // 连接操作

        /// <summary>
        /// 连接打开数据库
        /// </summary>
        /// <param name="path"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static bool OpenDataBase(string path, string password)
        {
            if (null == dbLock)
            {
                dbLock = new object();
            }
            if (null == dataBase)
            {
                dataBase = new DataBaseAccess();
            }
            isOpen = dataBase.Connect(path, password);
            return isOpen;
        }

        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        /// <returns></returns>
        public static bool CloseDataBase()
        {
            return dataBase.DisConnect();
        }

        #endregion

        #region // 表操作

        /// <summary>
        /// 查询表是否存在
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static bool CheckTable(LogTableType table)
        {
            if(!isOpen)
            {
                return false;
            }
            string sql = $"SELECT * FROM {table}";
            return (null != dataBase.ExecuteDataTable(sql, null));
        }

        /// <summary>
        /// 创建表
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static bool CreateTable(LogTableType table)
        {
            if(!isOpen)
            {
                return false;
            }

            string sql, title;
            sql = title = string.Empty;
            switch(table)
            {
                case LogTableType.DryingOvenLog:
                    {
                        for(DryingOvenLogColumn i = 0; i < DryingOvenLogColumn.End; i++)
                        {
                            title += $"[{i}] TEXT,";
                        }
                        break;
                    }
                case LogTableType.ParameterLog:
                    {
                        for(ParameterLogColumn i = 0; i < ParameterLogColumn.End; i++)
                        {
                            title += $"[{i}] TEXT,";
                        }
                        break;
                    }
                case LogTableType.RobotLog:
                    {
                        for(RobotLogColumn i = 0; i < RobotLogColumn.End; i++)
                        {
                            title += $"[{i}] TEXT,";
                        }
                        break;
                    }
                case LogTableType.MotorLog:
                    {
                        for(MotorLogColumn i = 0; i < MotorLogColumn.End; i++)
                        {
                            title += $"[{i}] TEXT,";
                        }
                        break;
                    }

                default:
                    return false;
            }
            sql = $"CREATE TABLE {table}({title.TrimEnd(',')})";
            return (null != dataBase.ExecuteDataTable(sql, null));
        }

        /// <summary>
        /// 清除表数据
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static bool ClearTable(LogTableType table)
        {
            if(!isOpen)
            {
                return false;
            }
            string sql = $"DELETE FROM {table}";
            return (null != dataBase.ExecuteDataTable(sql, null));
        }

        /// <summary>
        /// 删除表
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static bool DeleteTable(LogTableType table)
        {
            if(!isOpen)
            {
                return false;
            }
            string sql = $"DROP TABLE {table}";
            return (null != dataBase.ExecuteDataTable(sql, null));
        }

        #endregion

        #region // DryingOvenLog表操作

        /// <summary>
        /// 添加干燥炉Log
        /// </summary>
        /// <param name="formula"></param>
        /// <returns></returns>
        public static bool AddDryingOvenLog(OvenLogFormula formula)
        {
            if(!isOpen)
            {
                return false;
            }
            lock(dbLock)
            {
                string sql = string.Format("INSERT INTO {0}({1},{2},{3},{4},{5},{6},{7}) VALUES('{8}','{9}','{10}','{11}','{12}','{13}','{14}')"
                    , LogTableType.DryingOvenLog
                    , DryingOvenLogColumn.FormulaID, DryingOvenLogColumn.Operater, DryingOvenLogColumn.OptDate
                    , DryingOvenLogColumn.OvenID, DryingOvenLogColumn.OvenName, DryingOvenLogColumn.OptMode
                    , DryingOvenLogColumn.OvenAction
                    , formula.formulaID, formula.operater, formula.date, formula.ovenID, formula.ovenName, formula.mode, formula.action);
                return (dataBase.ExecuteNonQuery(sql, null) > 0);
            }
        }

        /// <summary>
        /// 删除干燥炉Log
        /// </summary>
        /// <param name="_formulaID"></param>
        /// <param name="_ovenID"></param>
        /// <param name="_startTime"></param>
        /// <param name="_endTime"></param>
        /// <returns></returns>
        public static bool DeleteDryingOvenLog(int _formulaID, int _ovenID, string _startTime, string _endTime)
        {
            if(!isOpen)
            {
                return false;
            }
            lock(dbLock)
            {
                string sql = string.Format("DELETE FROM {0}", LogTableType.DryingOvenLog);
                string info = "";
                if(_ovenID < 0)
                {
                    info = string.Format(" WHERE ({0} = '{1}') AND {2} BETWEEN '{3}' AND '{4}'"
                        , DryingOvenLogColumn.FormulaID, _formulaID
                        , DryingOvenLogColumn.OptDate, _startTime, _endTime);
                }
                else
                {
                    info = string.Format(" WHERE ({0} = '{1}' AND {2} = '{3}') AND {4} BETWEEN '{5}' AND '{6}'"
                        , DryingOvenLogColumn.FormulaID, _formulaID
                        , DryingOvenLogColumn.OvenID, _ovenID
                        , DryingOvenLogColumn.OptDate, _startTime, _endTime);
                }
                sql += info;
                return (dataBase.ExecuteNonQuery(sql, null) > 0);
            }
        }

        /// <summary>
        /// 获取干燥炉Log
        /// </summary>
        /// <param name="_formulaID"></param>
        /// <param name="_ovenID"></param>
        /// <param name="_startTime"></param>
        /// <param name="_endTime"></param>
        /// <param name="alarmTable"></param>
        /// <returns></returns>
        public static bool GetDryingOvenLogList(int _formulaID, int _ovenID, string _startTime, string _endTime, ref DataTable alarmTable)
        {
            if(!isOpen)
            {
                return false;
            }
            try
            {
                lock(dbLock)
                {
                    string sql = string.Format("SELECT * FROM {0}", LogTableType.DryingOvenLog);
                    string info = "";
                    if(_ovenID < 0)
                    {
                        info = string.Format(" WHERE {0} = '{1}' AND {2} BETWEEN '{3}' AND '{4}'"
                            , DryingOvenLogColumn.FormulaID, _formulaID
                            , DryingOvenLogColumn.OptDate, _startTime, _endTime);
                    }
                    else
                    {
                        info = string.Format(" WHERE {0} = '{1}' AND {2} = '{3}' AND {4} BETWEEN '{5}' AND '{6}'"
                            , DryingOvenLogColumn.FormulaID, _formulaID
                            , DryingOvenLogColumn.OvenID, _ovenID
                            , DryingOvenLogColumn.OptDate, _startTime, _endTime);
                    }
                    sql += info;
                    alarmTable = dataBase.ExecuteDataTable(sql, null);
                    return true;
                }
            }
            catch(System.Exception ex)
            {
                Trace.WriteLine("GetDryingOvenLogList( formulaID = " + _formulaID + ", ovenID = " + _ovenID
                    + ", startTime = " + _startTime + ", endTime = " + _endTime + " ) is fail.\r\n" + ex.Message);
            }
            return false;
        }
        #endregion

        #region // RobotLog表操作

        /// <summary>
        /// 添加机器人Log
        /// </summary>
        /// <param name="formula"></param>
        /// <returns></returns>
        public static bool AddRobotLog(RobotLogFormula formula)
        {
            if(!isOpen)
            {
                return false;
            }
            lock(dbLock)
            {
                string sql = string.Format("INSERT INTO {0}({1},{2},{3},{4},{5},{6},{7},{8}) VALUES('{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}')"
                    , LogTableType.RobotLog
                    , RobotLogColumn.FormulaID, RobotLogColumn.Operater, RobotLogColumn.OptDate
                    , RobotLogColumn.RobotID, RobotLogColumn.RobotName, RobotLogColumn.OptMode
                    , RobotLogColumn.SendRecv, RobotLogColumn.RobotAction
                    , formula.formulaID, formula.operater, formula.date, formula.robotID, formula.robotName, formula.mode, formula.sendRecv, formula.action);
                return (dataBase.ExecuteNonQuery(sql, null) > 0);
            }
        }

        /// <summary>
        /// 删除机器人Log
        /// </summary>
        /// <param name="_formulaID"></param>
        /// <param name="_robotID"></param>
        /// <param name="_startTime"></param>
        /// <param name="_endTime"></param>
        /// <returns></returns>
        public static bool DeleteRobotLog(int _formulaID, int _robotID, string _startTime, string _endTime)
        {
            if(!isOpen)
            {
                return false;
            }
            lock(dbLock)
            {
                string sql = string.Format("DELETE FROM {0}", LogTableType.RobotLog);
                string info = "";
                if(_robotID < 0)
                {
                    info = string.Format(" WHERE ({0} = '{1}') AND {2} BETWEEN '{3}' AND '{4}'"
                        , RobotLogColumn.FormulaID, _formulaID
                        , RobotLogColumn.OptDate, _startTime, _endTime);
                }
                else
                {
                    info = string.Format(" WHERE ({0} = '{1}' AND {2} = '{3}') AND {4} BETWEEN '{5}' AND '{6}'"
                        , RobotLogColumn.FormulaID, _formulaID
                        , RobotLogColumn.RobotID, _robotID
                        , RobotLogColumn.OptDate, _startTime, _endTime);
                }
                sql += info;
                return (dataBase.ExecuteNonQuery(sql, null) > 0);
            }
        }

        /// <summary>
        /// 获取机器人Log
        /// </summary>
        /// <param name="_formulaID"></param>
        /// <param name="_robotID"></param>
        /// <param name="_startTime"></param>
        /// <param name="_endTime"></param>
        /// <param name="alarmTable"></param>
        /// <returns></returns>
        public static bool GetRobotLogList(int _formulaID, int _robotID, string _startTime, string _endTime, ref DataTable alarmTable)
        {
            if(!isOpen)
            {
                return false;
            }
            try
            {
                lock(dbLock)
                {
                    string sql = string.Format("SELECT * FROM {0}", LogTableType.RobotLog);
                    string info = "";
                    if(_robotID < 0)
                    {
                        info = string.Format(" WHERE {0} = '{1}' AND {2} BETWEEN '{3}' AND '{4}'"
                            , RobotLogColumn.FormulaID, _formulaID
                            , RobotLogColumn.OptDate, _startTime, _endTime);
                    }
                    else
                    {
                        info = string.Format(" WHERE {0} = '{1}' AND {2} = '{3}' AND {4} BETWEEN '{5}' AND '{6}'"
                            , RobotLogColumn.FormulaID, _formulaID
                            , RobotLogColumn.RobotID, _robotID
                            , RobotLogColumn.OptDate, _startTime, _endTime);
                    }
                    sql += info;
                    alarmTable = dataBase.ExecuteDataTable(sql, null);
                    return true;
                }
            }
            catch(System.Exception ex)
            {
                Trace.WriteLine("GetRobotLogList( formulaID = " + _formulaID + ", _robotID = " + _robotID
                    + ", startTime = " + _startTime + ", endTime = " + _endTime + " ) is fail.\r\n" + ex.Message);
            }
            return false;
        }
        #endregion

        #region // ParameterLog表操作

        /// <summary>
        /// 添加参数修改Log
        /// </summary>
        /// <param name="formula"></param>
        /// <returns></returns>
        public static bool AddParameterLog(ParameterLogFormula formula)
        {
            if(!isOpen)
            {
                return false;
            }
            lock(dbLock)
            {
                string sql = string.Format("INSERT INTO {0}({1},{2},{3},{4},{5},{6},{7},{8}) VALUES('{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}')"
                    , LogTableType.ParameterLog
                    , ParameterLogColumn.FormulaID, ParameterLogColumn.Operater, ParameterLogColumn.OptDate
                    , ParameterLogColumn.ModuleID, ParameterLogColumn.ModuleName, ParameterLogColumn.ParmName
                    , ParameterLogColumn.OldValue, ParameterLogColumn.NewValue
                    , formula.formulaID, formula.operater, formula.date, formula.moduleID, formula.moduleName, formula.paraName, formula.oldValue, formula.newValue);
                return (dataBase.ExecuteNonQuery(sql, null) > 0);
            }
        }

        /// <summary>
        /// 删除参数修改Log
        /// </summary>
        /// <param name="_formulaID"></param>
        /// <param name="_moduleID"></param>
        /// <param name="_startTime"></param>
        /// <param name="_endTime"></param>
        /// <returns></returns>
        public static bool DeleteParameterLog(int _formulaID, int _moduleID, string _startTime, string _endTime)
        {
            if(!isOpen)
            {
                return false;
            }
            lock(dbLock)
            {
                string sql = string.Format("DELETE FROM {0}", LogTableType.ParameterLog);
                string info = "";
                if(_moduleID < -1)
                {
                    info = string.Format(" WHERE ({0} = '{1}') AND {2} BETWEEN '{3}' AND '{4}'"
                        , ParameterLogColumn.FormulaID, _formulaID
                        , ParameterLogColumn.OptDate, _startTime, _endTime);
                }
                else
                {
                    info = string.Format(" WHERE ({0} = '{1}' AND {2} = '{3}') AND {4} BETWEEN '{5}' AND '{6}'"
                        , ParameterLogColumn.FormulaID, _formulaID
                        , ParameterLogColumn.ModuleID, _moduleID
                        , ParameterLogColumn.OptDate, _startTime, _endTime);
                }
                sql += info;
                return (dataBase.ExecuteNonQuery(sql, null) > 0);
            }
        }

        /// <summary>
        /// 获取参数修改Log
        /// </summary>
        /// <param name="_formulaID"></param>
        /// <param name="_moduleID"></param>
        /// <param name="_startTime"></param>
        /// <param name="_endTime"></param>
        /// <param name="alarmTable"></param>
        /// <returns></returns>
        public static bool GetParameterLogList(int _formulaID, int _moduleID, string _startTime, string _endTime, ref DataTable alarmTable)
        {
            if(!isOpen)
            {
                return false;
            }
            try
            {
                lock(dbLock)
                {
                    string sql = string.Format("SELECT * FROM {0}", LogTableType.ParameterLog);
                    string info = "";
                    if(_moduleID < -1)
                    {
                        info = string.Format(" WHERE {0} = '{1}' AND {2} BETWEEN '{3}' AND '{4}'"
                            , ParameterLogColumn.FormulaID, _formulaID
                            , ParameterLogColumn.OptDate, _startTime, _endTime);
                    }
                    else
                    {
                        info = string.Format(" WHERE {0} = '{1}' AND {2} = '{3}' AND {4} BETWEEN '{5}' AND '{6}'"
                            , ParameterLogColumn.FormulaID, _formulaID
                            , ParameterLogColumn.ModuleID, _moduleID
                            , ParameterLogColumn.OptDate, _startTime, _endTime);
                    }
                    sql += info;
                    alarmTable = dataBase.ExecuteDataTable(sql, null);
                    return true;
                }
            }
            catch(System.Exception ex)
            {
                Trace.WriteLine("GetParameterLogList( formulaID = " + _formulaID + ", _moduleID = " + _moduleID
                    + ", startTime = " + _startTime + ", endTime = " + _endTime + " ) is fail.\r\n" + ex.Message);
            }
            return false;
        }
        #endregion

        #region // MotorLog表操作

        /// <summary>
        /// 添加MotorLog
        /// </summary>
        /// <param name="formula"></param>
        /// <returns></returns>
        public static bool AddMotorLog(MotorLogFormula formula)
        {
            if(!isOpen)
            {
                return false;
            }
            lock(dbLock)
            {
                string sql = string.Format("INSERT INTO {0}({1},{2},{3},{4},{5},{6},{7},{8},{9}) VALUES('{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}','{18}')"
                    , LogTableType.MotorLog
                    , MotorLogColumn.FormulaID, MotorLogColumn.Operater, MotorLogColumn.OptDate, MotorLogColumn.MotorID
                    , MotorLogColumn.MotorName, MotorLogColumn.OptMode, MotorLogColumn.OptAction, MotorLogColumn.OldValue, MotorLogColumn.NewValue
                    , formula.formulaID, formula.operater, formula.date, formula.motorID, formula.motorName, formula.mode, formula.action, formula.oldValue, formula.newValue);
                return (dataBase.ExecuteNonQuery(sql, null) > 0);
            }
        }

        /// <summary>
        /// 删除MotorLog
        /// </summary>
        /// <param name="_formulaID"></param>
        /// <param name="_motorID"></param>
        /// <param name="_startTime"></param>
        /// <param name="_endTime"></param>
        /// <returns></returns>
        public static bool DeleteMotorLog(int _formulaID, int _motorID, string _startTime, string _endTime)
        {
            if(!isOpen)
            {
                return false;
            }
            lock(dbLock)
            {
                string sql = string.Format("DELETE FROM {0}", LogTableType.MotorLog);
                string info = "";
                if(_motorID < 0)
                {
                    info = string.Format(" WHERE ({0} = '{1}') AND {2} BETWEEN '{3}' AND '{4}'"
                        , MotorLogColumn.FormulaID, _formulaID
                        , MotorLogColumn.OptDate, _startTime, _endTime);
                }
                else
                {
                    info = string.Format(" WHERE ({0} = '{1}' AND {2} = '{3}') AND {4} BETWEEN '{5}' AND '{6}'"
                        , MotorLogColumn.FormulaID, _formulaID
                        , MotorLogColumn.MotorID, _motorID
                        , MotorLogColumn.OptDate, _startTime, _endTime);
                }
                sql += info;
                return (dataBase.ExecuteNonQuery(sql, null) > 0);
            }
        }

        /// <summary>
        /// 获取MotorLog
        /// </summary>
        /// <param name="_formulaID"></param>
        /// <param name="_motorID"></param>
        /// <param name="_startTime"></param>
        /// <param name="_endTime"></param>
        /// <param name="alarmTable"></param>
        /// <returns></returns>
        public static bool GetMotorLogList(int _formulaID, int _motorID, string _startTime, string _endTime, ref DataTable alarmTable)
        {
            if(!isOpen)
            {
                return false;
            }
            try
            {
                lock(dbLock)
                {
                    string sql = string.Format("SELECT * FROM {0}", LogTableType.MotorLog);
                    string info = "";
                    if(_motorID < 0)
                    {
                        info = string.Format(" WHERE {0} = '{1}' AND {2} BETWEEN '{3}' AND '{4}'"
                            , MotorLogColumn.FormulaID, _formulaID
                            , MotorLogColumn.OptDate, _startTime, _endTime);
                    }
                    else
                    {
                        info = string.Format(" WHERE {0} = '{1}' AND {2} = '{3}' AND {4} BETWEEN '{5}' AND '{6}'"
                            , MotorLogColumn.FormulaID, _formulaID
                            , MotorLogColumn.MotorID, _motorID
                            , MotorLogColumn.OptDate, _startTime, _endTime);
                    }
                    sql += info;
                    alarmTable = dataBase.ExecuteDataTable(sql, null);
                    return true;
                }
            }
            catch(System.Exception ex)
            {
                Trace.WriteLine("GetMotorLogList( formulaID = " + _formulaID + ", _motorID = " + _motorID
                    + ", startTime = " + _startTime + ", endTime = " + _endTime + " ) is fail.\r\n" + ex.Message);
            }
            return false;
        }
        #endregion

    }
}
