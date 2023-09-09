using HelperLibrary;
using System;
using System.Windows.Forms;

namespace Machine
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            System.Threading.Mutex appRun = new System.Threading.Mutex(false, Application.ProductName + "FH01.exe");
            if(!appRun.WaitOne(100, false))
            {
                ShowMsgBox.ShowDialog(Application.ProductName + "已经运行！", MessageType.MsgAlarm);
                return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("Main", $"{ex.Message}\r\n{ex.StackTrace}", LogType.Fatal);
            }
        }
    }
}
