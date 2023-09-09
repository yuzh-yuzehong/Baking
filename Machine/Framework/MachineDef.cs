using HelperLibrary;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using SystemControlLibrary;
using Excel = Microsoft.Office.Interop.Excel;

namespace Machine
{
    #region // 设备中所有模组的ID

    /// <summary>
    /// 设备中所有模组的ID
    /// </summary>
    public enum RunID
    {
        Invalid = -1,      // 无效

        // 上料
        OnloadLine = 0,
        OnloadRecv,
        OnloadScan,
        OnloadRobot,
        OnloadNG,
        OnloadFake,

        // 调度
        Transfer,
        ManualOperate,
        PalletBuffer,

        // 下料
        OffloadBattery,
        OffloadNG,
        OffloadDetect,
        OffloadLine,
        CoolingSystem,
        CoolingOffload,

        // 干燥炉
        DryOven0,
        DryOvenALL = DryOven0 + OvenInfoCount.OvenCount,

        RunIDEnd,
    }
    #endregion


    #region // 运行数据保存类型

    /// <summary>
    /// 运行数据保存类型
    /// </summary>
    public enum SaveType
    {
        AutoStep = 0x01 << 0,      // 步骤（自动流程步骤）
        Variables = 0x01 << 1,     // 变量（成员变量）
        SignalEvent = 0x01 << 2,   // 信号
        Battery = 0x01 << 3,       // 电池（抓手||缓存||假电池||NG||暂存）
        Pallet = 0x01 << 4,        // 治具（夹具||料框）
        Cylinder = 0x01 << 5,      // 气缸状态
        Motor = 0x01 << 6,         // 电机位置
        Robot = 0x01 << 7,         // 机器人位置
        Cavity = 0x01 << 8,        // 干燥炉腔体数据
    };
    #endregion


    #region // 模组事件状态

    /// <summary>
    /// 事件状态
    /// </summary>
    public enum EventStatus
    {
        Invalid = 0,      // 无效状态
        Require,          // 请求状态
        Response,         // 响应状态
        Ready,            // 准备状态
        Start,            // 开始状态
        Finished,         // 完成状态
        Cancel,           // 取消状态
    };
    #endregion


    #region // 模组事件枚举（禁止改变顺序！！！）

    /// <summary>
    /// 模组事件（禁止改变顺序！！！）
    /// </summary>
    public enum EventList
    {
        Invalid = -1,                          // 信号无效

        // RunProcessOnloadRecv
        OnloadRecvSendBattery = 0,
        // RunProcessOnloadScan
        OnloadScanSendBattery,
        // RunProcessOnloadLine
        OnloadLinePickBattery,
        // RunProcessOnloadFake
        OnloadFakePickBattery,
        // RunProcessOnloadNG
        OnloadNGPlaceBattery,
        // RunProcessOnloadRobot
        OnloadPlaceEmptyPallet,           // 上料区放空夹具
        OnloadPlaceNGPallet,              // 上料区放NG非空夹具，转盘
        OnLoadPlaceDetectFakePallet,      // 上料区放待检测含假电池夹具（未取走假电池的夹具）
        OnloadPlaceReputFakePallet,       // 上料区放待回炉含假电池夹具（已取走假电池，待重新放回假电池的夹具）
        OnloadPickNGEmptyPallet,          // 上料区取NG空夹具
        OnloadPickOKFullPallet,           // 上料区取OK无假电池满夹具
        OnloadPickOKFakeFullPallet,       // 上料区取OK带假电池满夹具
        OnLoadPickWaitResultPallet,       // 上料区取等待水含量结果夹具（已取待测假电池的夹具）
        OnloadPickRebakeFakePallet,       // 上料区取回炉假电池夹具（已放回假电池的夹具）
        OnloadPickPlaceEnd,               // 上料区信号结束

        // RunProcessManualOperate
        ManualPlaceNGEmptyPallet,         // 人工操作台放NG空夹具
        ManualPickEmptyPallet,            // 人工操作台取OK空夹具
        ManualPickPlaceEnd,               // 人工操作台信号结束

        // RunProcessPalletBuffer
        PalletBufferPlaceEmptyPallet,           // 缓存架放空夹具
        PalletBufferPlaceNGEmptyPallet,         // 缓存架放NG空夹具
        PalletBufferPickEmptyPallet,            // 缓存架取空夹具
        PalletBufferPickNGEmptyPallet,          // 缓存架取NG空夹具
        PalletBufferPickPlaceEnd,               // 缓存架信号结束

        // RunProcessDryingOven
        DryOvenPlaceEmptyPallet,                // 干燥炉放空夹具
        DryOvenPlaceNGPallet,                   // 干燥炉放NG非空夹具
        DryOvenPlaceNGEmptyPallet,              // 干燥炉放NG空夹具
        DryOvenPlaceOnlOKFullPallet,            // 干燥炉放上料完成OK满夹具
        DryOvenPlaceOnlOKFakeFullPallet,        // 干燥炉放上料完成OK带假电池满夹具
        DryOvenPlaceRebakeFakePallet,           // 干燥炉放回炉假电池夹具（已放回假电池的夹具）
        DryOvenPlaceWaitResultPallet,           // 干燥炉放等待水含量结果夹具（已取待测假电池的夹具）
        DryOvenPickEmptyPallet,                 // 干燥炉取空夹具
        DryOvenPickNGPallet,                    // 干燥炉取NG非空夹具
        DryOvenPickNGEmptyPallet,               // 干燥炉取NG空夹具
        DryOvenPickDetectFakePallet,            // 干燥炉取待检测含假电池夹具（未取走假电池的夹具）
        DryOvenPickReputFakePallet,             // 干燥炉取待回炉含假电池夹具（已取走假电池，待重新放回假电池的夹具）
        DryOvenPickDryFinishPallet,             // 干燥炉取干燥完成夹具（等待下料）
        DryOvenPickTransferPallet,              // 干燥炉转移取夹具：取来源炉腔
        DryOvenPlaceTransferPallet,             // 干燥炉转移放夹具：放至目的炉腔
        DryOvenPickPlaceEnd,					// 干燥炉信号结束

        // RunProcessOffloadBattery
        OffLoadPlaceDryFinishPallet,           // 下料区放干燥完成夹具
        OffLoadPlaceDetectFakePallet,          // 下料区放待检测含假电池夹具（未取走假电池的夹具）
        OffLoadPlaceNGPallet,                  // 下料区放NG夹具（非空）
        OffLoadPickEmptyPallet,                // 下料区取空夹具
        OffLoadPickWaitResultPallet,           // 下料区取等待水含量结果夹具（已取待测假电池的夹具）
        OffLoadPickRebakeFakePallet,           // 干燥炉取待回炉含假电池夹具（已取走假电池，待重新放回假电池的夹具）
        OffLoadPickNGPallet,                   // 下料区取NG夹具（非空）
        OffLoadPickNGEmptyPallet,              // 下料区取NG空夹具
        OffLoadPickPlaceEnd,                   // 下料区信号结束

        // RunProcessOffloadDetectFake
        OffLoadPlaceDetectBattery,             // 下料放待检测电池    
        // RunProcessOffloadNG
        OffLoadPlaceNGBattery,                 // 下料放烘烤NG电池
        // RunProcessOffloadLine
        OffLoadLinePlaceBattery,               // 下料放下料线电池
        // RunProcessCoolingSystem
        CoolingSystemPlaceBattery,             // 冷却系统放电池
        CoolingSystemPickBattery,              // 冷却系统取电池
        CoolingSystemPickPlaceEnd,

        EventEnd,
    };
    #endregion


    #region // 模组事件结构

    /// <summary>
    /// 模组事件
    /// </summary>
    [System.Serializable]
    public struct ModuleEvent
    {
        public EventList Event;
        public EventStatus State;
        public int Pos;

        public ModuleEvent(EventList modEvent, EventStatus eventState, int eventPos)
        {
            this.Event = modEvent;
            this.State = eventState;
            this.Pos = eventPos;
        }
    };
    #endregion


    #region // 模组中电机点位

    /// <summary>
    /// 模组中电机点位
    /// </summary>
    public enum MotorPosition
    {
        Invalid = -1,

        // RunProcessOnloadRobot
        Onload_LinePickPos = 0,     // 来料取料位间距
        Onload_ScanPalletPos,       // 夹具扫码位间距
        Onload_PalletPos,           // 夹具放料位间距
        Onload_BufferPos,           // 暂存位间距
        Onload_ScanFakePos,         // 假电池扫码位间距
        Onload_FakePos,             // 假电池取料位间距
        Onload_NGPos,               // NG输出放料位间距
        Onload_Pos_End,             // 结束

        // RunProcessOffloadBattery
        OffLoad_SafetyPos = 0,      // 下料区安全位
        OffLoad_PickPltPos1,        // 下料区夹具1取料位
        OffLoad_PickPltPos2,        // 下料区夹具2取料位
        OffLoad_PlacePos,           // 下料区放料位
        OffLoad_BufferPos_0,        // 下料区暂存位1
        OffLoad_BufferPos_1,        // 下料区暂存位2
        OffLoad_BufferPos_2,        // 下料区暂存位3
        OffLoad_PlaceDetect,        // 下料区放待检测电池
        OffLoad_PlaceNG,            // 下料区放NG电池

        // RunProcessCoolingOffload
        CoolingOffload_SafetyPos = 0,   // 冷却下料安全位
        CoolingOffload_PickPos,         // 冷却下料取料位
        CoolingOffload_BufferPos1,      // 冷却下料缓存位1
        CoolingOffload_BufferPos2,      // 冷却下料缓存位2
        CoolingOffload_BufferPos3,      // 冷却下料缓存位3
        CoolingOffload_PlacePos,        // 冷却下料放料位

    }
    #endregion


    #region // 模组中的最大夹具数

    /// <summary>
    /// 模组中的最大夹具数
    /// </summary>
    enum ModuleMaxPallet
    {
        // 上料区夹具：1层，2列
        OnloadRobot = 2,

        // 调度机器人抓手夹具：1层，1列
        TransferRobot = 1,

        // 人工操作台夹具：1层，1列
        ManualOperate = 1,

        // 夹具缓存架夹具：4层，1列
        PalletBuffer = 4,

        // 干燥炉夹具：4层，2列
        DryingOven = OvenRowCol.MaxRow*OvenRowCol.MaxCol,

        //RunProcessBatteryOffload：1层，2列    
        OffloadBattery = 2,

    };
    #endregion


    #region // 模组报警ID范围

    /// <summary>
    /// 模组报警ID范围
    /// </summary>
    enum ModuleMsgID
    {
        // 模组其实ID在库ID后开始
        SystemStartID = LibMsgID.MsgLibIDEnd,
        SystemEndID = SystemStartID + 99,

        // RunProcessOnloadLine
        OnloadRecvMsgStartID,
        OnloadRecvMsgEndID = OnloadRecvMsgStartID + 99,

        // RunProcessOnloadLine
        OnloadLineMsgStartID,
        OnloadLineMsgEndID = OnloadLineMsgStartID + 99,

        // RunProcessOnloadScan
        OnloadScanMsgStartID,
        OnloadScanMsgEndID = OnloadScanMsgStartID + 99,

        // RunProcessOnloadFake
        OnloadFakeMsgStartID,
        OnloadFakeMsgEndID = OnloadFakeMsgStartID + 99,

        // RunProcessOnloadNG
        OnloadNGMsgStartID,
        OnloadNGMsgEndID = OnloadNGMsgStartID + 99,

        // RunProcessOnloadRobot
        OnloadRobotMsgStartID,
        OnloadRobotMsgEndID = OnloadRobotMsgStartID + 99,

        // RunProcessRobotTransfer
        RobotTransferMsgStartID,
        RobotTransferMsgEndID = RobotTransferMsgStartID + 99,

        // RunProcessManualOperate
        ManualOperateMsgStartID,
        ManualOperateMsgEndID = ManualOperateMsgStartID + 99,

        // RunProcessPalletBuffer
        PalletBufferMsgStartID,
        PalletBufferMsgEndID = PalletBufferMsgStartID + 99,

        // RunProcessDryingOven
        DryingOvenMsgStartID,
        DryingOvenMsgEndID = DryingOvenMsgStartID + 99,

        // RunProcessOffloadBattery
        OffloadBatteryMsgStartID,
        OffloadBatteryMsgEndID = OffloadBatteryMsgStartID + 99,

        // RunProcessOffloadDetectFake
        OffloadDetectFakeMsgStartID,
        OffloadDetectFakeMsgEndID = OffloadDetectFakeMsgStartID + 99,

        // RunProcessOffloadNG
        OffloadNGMsgStartID,
        OffloadNGMsgEndID = OffloadNGMsgStartID + 99,

        // RunProcessOffloadLine
        OffloadLineMsgStartID,
        OffloadLineMsgEndID = OffloadLineMsgStartID + 99,

        // RunProcessCoolingSystem
        CoolingSystemMsgStartID,
        CoolingSystemMsgEndID = CoolingSystemMsgStartID + 99,

    }
    #endregion


    #region // 设备系统IO：按钮-灯塔-安全门

    /// <summary>
    /// 设备系统IO：按钮-灯塔-安全门
    /// </summary>
    enum SystemIO
    {
        ButtonIO = 2,       // 按钮IO数量
        TowerIO = 2,        // 灯塔IO数量
        SafeDoorIO = 4,     // 安全门数量
    }
    #endregion


    #region // 操作模式：手动/自动

    /// <summary>
    /// 操作模式：手动/自动
    /// </summary>
    public enum OptMode
    {
        Auto,
        Manual,
    }

    #endregion


    #region // 系统宏定义类

    /// <summary>
    /// 系统宏定义类
    /// </summary>
    public static class Def
    {
        #region // 系统字段

        /// <summary>
        /// Dump文件夹
        /// </summary>
        public const string DumpFolder = SysDef.DumpFolder;
        /// <summary>
        /// 系统Log文件夹
        /// </summary>
        public const string SystemLogFolder = SysDef.SystemLogFolder;
        /// <summary>
        /// 设备Log文件夹
        /// </summary>
        public const string MachineLogFolder = SysDef.MachineLogFolder;
        /// <summary>
        /// 电机配置文件夹
        /// </summary>
        public const string MotorCfgFolder = SysDef.MotorCfgFolder;
        /// <summary>
        /// 硬件配置文件
        /// </summary>
        public const string HardwareCfg = SysDef.HardwareCfg;
        /// <summary>
        /// 输入配置文件
        /// </summary>
        public const string InputCfg = SysDef.InputCfg;
        /// <summary>
        /// 输出配置文件
        /// </summary>
        public const string OutputCfg = SysDef.OutputCfg;
        /// <summary>
        /// 模组文件
        /// </summary>
        public const string ModuleCfg = SysDef.ModuleCfg;
        /// <summary>
        /// 模组配置文件
        /// </summary>
        public const string ModuleExCfg = SysDef.ModuleExCfg;
        /// <summary>
        /// 以ID报警的配置文件
        /// </summary>
        public const string MessageCfg = SysDef.MessageCfg;
        /// <summary>
        /// 设备参数文件
        /// </summary>
        public const string MachineCfg = SysDef.MachineCfg;
        /// <summary>
        /// 设备本地数据库文件
        /// </summary>
        public const string MachineMdb = SysDef.MachineMdb;
        /// <summary>
        /// 运行数据文件夹
        /// </summary>
        public const string RunDataFolder = "Data\\RunData\\";
        /// <summary>
        /// 运行数据备份文件夹
        /// </summary>
        public const string RunDataBakFolder = "Data\\RunDataBak\\";
        /// <summary>
        /// MES参数文件
        /// </summary>
        public const string MesParameterCfg = "System\\MesParameter.cfg";

        /// <summary>
        /// 系统时间样式
        /// </summary>
        public const string DateFormal = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// 设备Log文件
        /// </summary>
        private static LogFile mcLogFile;

        /// <summary>
        /// 随机数
        /// </summary>
        private static Random random;

        #endregion

        #region // 系统方法

        /// <summary>
        /// 获取设备显示语言：CHS中文，ENG英文
        /// </summary>
        public static string GetLanguage()
        {
            return HelperDef.GetLanguage();
        }

        /// <summary>
        /// 获取设备当前运行方式：TRUE无硬件设备模拟运行，FALSE有硬件运行
        /// </summary>
        public static bool IsNoHardware()
        {
            return HelperDef.IsNoHardware();
        }

        /// <summary>
        /// 当前设备产品配方
        /// </summary>
        public static int GetProductFormula()
        {
            return HelperDef.GetProductFormula();
        }

        /// <summary>
        /// 获取当前相对路径的绝对路径
        /// </summary>
        /// <param name="relPath">相对路径</param>
        /// <returns></returns>
        public static string GetAbsPathName(string relPath)
        {
            return HelperDef.GetAbsPathName(relPath);
        }

        /// <summary>
        /// 创建当前绝对路径
        /// </summary>
        /// <param name="absPath">绝对路径</param>
        /// <returns></returns>
        public static bool CreateFilePath(string absPath)
        {
            // 剔除掉文件名
            return HelperDef.CreateFilePath(absPath.Remove(absPath.LastIndexOf('\\')));
        }

        /// <summary>
        /// 删除文件夹strDir中nDays天以前的文件
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="days"></param>
        public static void DeleteOldFiles(string dir, int days)
        {
            HelperDef.DeleteOldFiles(dir, days);
        }

        /// <summary>
        /// 获取随机数
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <returns></returns>
        public static int GetRandom(int min, int max)
        {
            if (null == random)
            {
                random = new Random();
            }
            return random.Next(min, max);
        }

        /// <summary>
        /// 生成全局不重复GUID
        /// </summary>
        /// <returns></returns>
        public static string GetGUID()
        {
            return SysDef.GetGUID();
        }

        /// <summary>
        /// CRC校验
        /// </summary>
        /// <param name="data">校验数据</param>
        /// <returns>高低8位</returns>
        public static int CRCCalc(byte[] data, int len)
        {
            //计算并填写CRC校验码
            int crc = 0xffff;
            for(int n = 0; n < len; n++)
            {
                byte i;
                crc = crc ^ data[n];
                for(i = 0; i < 8; i++)
                {
                    int TT;
                    TT = crc & 1;
                    crc = crc >> 1;
                    crc = crc & 0x7fff;
                    if(TT == 1)
                    {
                        crc = crc ^ 0xa001;
                    }
                    crc = crc & 0xffff;
                }

            }
            return crc;
        }

        /// <summary>
        /// 导出Excel文件
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool ExportExcel(DataTable dt, string fileName)
        {
            try
            {
                if(dt == null)
                {
                    Trace.WriteLine("Machine.Def.ExportExcel() 数据库为空");
                    return false;
                }

                bool fileSaved = false;
                Excel.Application xlApp = new Excel.Application();
                if(xlApp == null)
                {
                    Trace.WriteLine("Machine.Def.ExportExcel() 无法创建Excel对象，可能您的设备未安装Excel.");
                    return false;
                }
                Excel.Workbooks workbooks = xlApp.Workbooks;
                Excel.Workbook workbook = workbooks.Add(Excel.XlWBATemplate.xlWBATWorksheet);
                Excel.Worksheet worksheet = (Excel.Worksheet)workbook.Worksheets[1];//取得sheet1
                //写入字段
                for(int i = 0; i < dt.Columns.Count; i++)
                {
                    worksheet.Cells[1, i + 1] = dt.Columns[i].ColumnName;
                }
                //写入数值
                for(int r = 0; r < dt.Rows.Count; r++)
                {
                    for(int i = 0; i < dt.Columns.Count; i++)
                    {
                        worksheet.Cells[r + 2, i + 1] = dt.Rows[r][i];
                    }
                    System.Windows.Forms.Application.DoEvents();
                }
                string msg = string.Empty;
                worksheet.Columns.EntireColumn.AutoFit();//列宽自适应。
                if(!string.IsNullOrEmpty(fileName))
                {
                    workbook.Saved = true;
                    workbook.SaveCopyAs(fileName);
                    fileSaved = true;
                }
                xlApp.Quit();
                GC.Collect();//强行销毁
                if(fileSaved && File.Exists(fileName))
                {
                    return true;
                }
            }
            catch(System.Exception ex)
            {
                WriteLog(string.Format("Machine.Def.ExportExcel() 导出文件{0}时出错！", fileName), ex.Message, LogType.Error);
            }
            return false;
        }

        /// <summary>
        /// 导出CSV文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="title"></param>
        /// <param name="fileText"></param>
        /// <param name="encode"></param>
        /// <returns></returns>
        public static bool ExportCsvFile(string fileName, string title, string fileText, Encoding encode = null)
        {
            try
            {
                if(!CreateFilePath(fileName))
                    return false;

                bool writeTitle = false;
                if(!File.Exists(fileName))
                {
                    writeTitle = true;
                }

                using(StreamWriter sw = new StreamWriter(fileName, true, (null == encode ? Encoding.Default : encode)))
                {
                    if(writeTitle)
                    {
                        sw.WriteLine(title);
                    }
                    sw.Write(fileText);

                    sw.Flush();
                }
                return true;
            }
            catch (System.Exception ex)
            {
                WriteLog(string.Format("Machine.Def.ExportCsvFile() 导出文件{0}时出错！", fileName), ex.Message, LogType.Error);
            }
            return false;
        }

        /// <summary>
        /// 写文本文件：适用于MES文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileText"></param>
        /// <param name="encode"></param>
        /// <returns></returns>
        public static bool WriteText(string fileName, string fileText, Encoding encode = null)
        {
            try
            {
                if(!CreateFilePath(fileName))
                    return false;

                using(StreamWriter sw = new StreamWriter(fileName, true, (null == encode ? Encoding.Default : encode)))
                {
                    sw.WriteLine(fileText);

                    sw.Flush();
                }
                return true;
            }
            catch(System.Exception ex)
            {
                WriteLog(string.Format("Machine.Def.WriteText({0})时出错！", fileName), ex.Message, LogType.Error);
            }
            return false;
        }

        #endregion
        
        #region // Log文件

        /// <summary>
        /// 设置Log信息
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="size">文件大小</param>
        /// <param name="storageLife">文件存储周期</param>
        public static void SetFileInfo(string filePath, long size, int storageLife)
        {
            if (null == mcLogFile)
            {
                mcLogFile = new LogFile();
            }
            mcLogFile.SetFileInfo(filePath, size, storageLife);
        }

        /// <summary>
        /// 输出Log
        /// </summary>
        /// <param name="msglocation">Log的定位信息：一般为，类.方法</param>
        /// <param name="log">Log</param>
        /// <param name="type">Log类型</param>
        public static void WriteLog(string msglocation, string log, LogType type = LogType.Information)
        {
            try
            {
                if(null == mcLogFile)
                {
                    mcLogFile = new LogFile();
                }
                Trace.WriteLine(string.Format("{0}:{1}", msglocation, log));
                mcLogFile.WriteLog(DateTime.Now, msglocation, log, type);
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine("Def.WriteLog() error" + ex.Message);
            }
        }

        #endregion

    }
    #endregion


    #region // 生产统计数据

    /// <summary>
    /// 生产统计数据
    /// </summary>
    public static class TotalData
    {
        public static int OnloadCount;                // 上料总数
        public static int OnScanNGCount;              // 上料扫码NG总数
        public static int OffloadCount;               // 下料总数
        public static int BakedNGCount;               // BakedNG总数

        /// <summary>
        /// 读统计数据
        /// </summary>
        public static void ReadTotalData()
        {
            string file, section;
            file = Def.GetAbsPathName(Def.RunDataFolder + "TotalData.cfg");
            section = "TotalData";

            OnloadCount = IniFile.ReadInt(section, nameof(OnloadCount), 0, file);
            OnScanNGCount = IniFile.ReadInt(section, nameof(OnScanNGCount), 0, file);
            OffloadCount = IniFile.ReadInt(section, nameof(OffloadCount), 0, file);
            BakedNGCount = IniFile.ReadInt(section, nameof(BakedNGCount), 0, file);
        }

        /// <summary>
        /// 保存统计数据
        /// </summary>
        public static void WriteTotalData()
        {
            string file, section;
            file = Def.GetAbsPathName(Def.RunDataFolder + "TotalData.cfg");
            section = "TotalData";

            IniFile.WriteInt(section, nameof(OnloadCount), OnloadCount, file);
            IniFile.WriteInt(section, nameof(OnScanNGCount), OnScanNGCount, file);
            IniFile.WriteInt(section, nameof(OffloadCount), OffloadCount, file);
            IniFile.WriteInt(section, nameof(BakedNGCount), BakedNGCount, file);
        }

        /// <summary>
        /// 清空统计数据
        /// </summary>
        public static void ClearTotalData()
        {
            ShiftStruct shift = OperationShifts.Shift();
            string file, title, text;
            file = string.Format(@"{0}\生产计数\{1}\{1}.csv"
                    , MachineCtrl.GetInstance().ProductionFilePath, DateTime.Now.ToString("yyyy-MM-dd"));
            title = string.Format("日期,时间,操作员,班次,上料计数,上料扫码NG,下料计数,烘烤NG");
            text = string.Format("{0},{1},{2}[{3}],{4},{5},{6},{7}\r\n"
                , DateTime.Now.ToString("yyyy-MM-dd,HH:mm:ss")
                , MachineCtrl.GetInstance().OperaterID, shift.Name, shift.Code
                , OnloadCount, OnScanNGCount, OffloadCount, BakedNGCount);
            Def.ExportCsvFile(file, title, text);

            OnloadCount = 0;
            OnScanNGCount = 0;
            OffloadCount = 0;
            BakedNGCount = 0;
        }

    }
    #endregion


}
