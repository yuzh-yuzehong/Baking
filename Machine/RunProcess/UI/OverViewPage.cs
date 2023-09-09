using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SystemControlLibrary;

namespace Machine
{
    public partial class OverViewPage : FormEx
    {
        public OverViewPage()
        {
            InitializeComponent();

            CreateTotalDataView();
        }

        #region // 字段

        /// <summary>
        /// 界面更新定时器
        /// </summary>
        private System.Timers.Timer timerUpdata;

        TipDlg tip;
        bool tipShow;
        Point lastMovePos;
        DateTime lastMoveTime;
        bool updating;

        // MES信息
        Rectangle rectMesInfo;
        // 上料
        Rectangle rectOnloadLine;
        Rectangle rectOnloadRecv;
        Rectangle rectOnloadScan;
        Rectangle[] rectOnloadRbtPlt;
        Rectangle rectOnloadRbtFinger;
        Rectangle rectOnloadRbtBuffer;
        Rectangle rectOnloadNG;
        Rectangle rectOnloadFake;
        Rectangle rectManualOperate;
        Rectangle[] rectPltBufferPlt;

        // 调度
        Rectangle rectTransfer;

        // 下料
        Rectangle[] rectOffloadBatPlt;
        Rectangle rectOffloadBatFinger;
        Rectangle rectOffloadBatBuffer;
        Rectangle rectOffloadNG;
        Rectangle rectOffloadDetect;
        Rectangle rectOffloadLine;
        Rectangle rectCoolingSystem;
        Rectangle rectCoolingFinger;
        Rectangle rectCoolingBuffer;

        // 干燥炉：炉子,腔体/夹具
        Rectangle[,] rectDryOvenCavity;
        Rectangle[,] rectDryOvenPlt;

        #endregion

        #region // 加载及销毁窗体

        /// <summary>
        /// 加载窗体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OverViewPage_Load(object sender, EventArgs e)
        {
            this.lastMovePos = new Point(0, 0);
            this.lastMoveTime = DateTime.Now;
            this.updating = false;

            InitModuleRectangle();

            this.labelView.Text = "";

            // 开启定时器
            this.timerUpdata = new System.Timers.Timer();
            this.timerUpdata.Elapsed += UpdataOverViewPage;
            this.timerUpdata.Interval = 500;         // 间隔时间
            this.timerUpdata.AutoReset = true;       // 设置是执行一次（false）还是一直执行(true)；
            this.timerUpdata.Start();                // 开始执行定时器
        }

        /// <summary>
        /// 销毁自定义非托管资源
        /// </summary>
        public override void DisposeForm()
        {
            // 关闭定时器
            if(null != this.timerUpdata)
            {
                this.timerUpdata.Stop();
                while (this.updating)
                {
                    System.Threading.Thread.Sleep(1);
                }
            }
        }

        /// <summary>
        /// 解决窗体绘图时闪烁
        /// </summary>
        /// <param name="e">System.Windows.Forms.CreateParams，包含创建控件的句柄时所需的创建参数。</param>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
            /// <param name="e"></param>
        }

        /// <summary>
        /// 界面隐藏时停止绘图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OverViewPage_VisibleChanged(object sender, EventArgs e)
        {
        }

        #endregion

        #region // 重绘

        /// <summary>
        /// 触发重绘，使其更新界面动画
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdataOverViewPage(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.labelView.Invalidate();
            if((DateTime.Now - this.lastMoveTime).TotalSeconds >= 10)
            {
                try
                {
                    if(this.tipShow)
                    {
                        this.Invoke(new Action(() => { this.tip.Close(); }));
                        this.tipShow = false;
                    }
                }
                catch (System.Exception ex)
                {
                    Def.WriteLog("OverViewPage", "TipDlg is invalid: " + ex.Message);
                }
            }
            UpdataTotalData();
        }

        /// <summary>
        /// 重绘事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void labelView_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // 画刷，绘笔
            SolidBrush sBrush = new SolidBrush(Color.Transparent);
            Pen pen = new Pen(Color.Black, 1);

            // 分割标准
            Rectangle rcFrame = e.ClipRectangle;
            rcFrame.Inflate(-10, -10);
            //g.DrawRectangle(pen, rcFrame);
            double frameWidth = rcFrame.Width / 100.0;
            double frameHight = rcFrame.Height / 100.0;

            Rectangle rcArea;

            // MES信息
            rcArea = new Rectangle((int)(rcFrame.X), (int)(rcFrame.Y ), (int)(frameWidth * 99.0), (int)(frameHight * 5.0));
            //g.DrawRectangle(pen, rcArea);
            DrawMesInfo(g, pen, rcArea);

            // 人工操作台
            rcArea = new Rectangle((int)(rcFrame.X + frameWidth * 22.5), (int)(rcFrame.Y + frameHight * 90.0), (int)(frameWidth * 11.0), (int)(frameHight * 10.0));
            //g.DrawRectangle(pen, rcArea);
            DrawManualOperate(g, pen, rcArea);

            // 夹具缓存架
            rcArea = new Rectangle((int)(rcFrame.X + frameWidth * 22.5), (int)(rcFrame.Y + frameHight * 60.0), (int)(frameWidth * 11.0), (int)(frameHight * 30.0));
            //g.DrawRectangle(pen, rcArea);
            DrawPalletBuffer(g, pen, rcArea);

            // 干燥炉组1
            rcArea = new Rectangle((int)(rcFrame.X), (int)(rcFrame.Y + frameHight * 5.0), (int)(frameWidth * 99.0), (int)(frameHight * 40.0));
            g.DrawRectangle(pen, rcArea);
            DrawOvenGroup1(g, pen, rcArea);

            // 调度
            rcArea = new Rectangle((int)(rcFrame.X + frameWidth), (int)(rcFrame.Y + frameHight * 46.0), (int)(frameWidth * 98), (int)(frameHight * 10));
            //g.DrawRectangle(pen, rcArea);
            DrawTransfer(g, pen, rcArea);

            // 上料区
            rcArea = new Rectangle((rcFrame.X), (int)(rcFrame.Y + (frameHight * 60.0)), (int)(frameWidth * 23.0), (int)(frameHight * 40.0));
            g.DrawRectangle(pen, rcArea);
            DrawOnload(g, pen, rcArea);

            // 干燥炉组0
            rcArea = new Rectangle((int)(rcFrame.X + frameWidth * 33.0), (int)(rcFrame.Y + frameHight * 60.0), (int)(frameWidth * 44.0), (int)(frameHight * 40.0));
            g.DrawRectangle(pen, rcArea);
            DrawOvenGroup0(g, pen, rcArea);

            //下料区
            rcArea = new Rectangle((int)(rcFrame.X + (frameWidth * 77.0)), (int)(rcFrame.Y + frameHight * 60.0), (int)(frameWidth * 22), (int)(frameHight * 40.0));
            g.DrawRectangle(pen, rcArea);
            DrawOffLoad(g, pen, rcArea);

        }

        #endregion

        #region // 鼠标事件

        /// <summary>
        /// 鼠标提示事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void labelView_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.tipShow)
            {
                this.Invoke(new Action(() => { this.tip.Close(); }));
                this.tipShow = false;
            }
            else
            {
                Point pt = this.labelView.PointToClient(MousePosition);
                if (ShowMesInfo(pt))
                {
                    return;
                }
                else if(ShowOnload(pt))
                {
                    return;
                }
                else if(ShowManualOperate(pt))
                {
                    return;
                }
                else if(ShowOven(pt))
                {
                    return;
                }
                else if(ShowPalletBuffer(pt))
                {
                    return;
                }
                else if(ShowTransfer(pt))
                {
                    return;
                }
                else if(ShowOffLoad(pt))
                {
                    return;
                }
            }
        }

        /// <summary>
        /// 鼠标移动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void labelView_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.Location != this.lastMovePos)
            {
                this.lastMovePos = e.Location;
                this.lastMoveTime = DateTime.Now;

                //labelView_MouseHover(sender, e);
            }
        }
        
        #endregion

        #region // 绘制模组区域

        /// <summary>
        /// MES信息
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        private void DrawMesInfo(Graphics g, Pen pen, Rectangle rect)
        {
            // 模组名字体
            Font font = new Font(this.Font.FontFamily, (float)10.0);

            float wid = (float)(rect.Width / 100.0);
            float hig = (float)(rect.Height / 100.0);

            // MES在线状态
            bool updata = MachineCtrl.GetInstance().UpdataMes;
            Rectangle rc = new Rectangle((int)(rect.X + (hig * 5)), (int)(rect.Y + (hig * 1)), (int)(hig * 80), (int)(hig * 80));
            DrawRect(g, (new Pen((updata ? Brushes.DarkGreen : Brushes.Red))), rc, (updata ? Brushes.DarkGreen : Brushes.Red));
            g.DrawString((updata ? "在线生产" : "离线生产"), font, (updata ? Brushes.DarkGreen : Brushes.Red), (int)(rc.Right + (wid)), (int)(rect.Y + hig * 18.0));

            // 工单及数量
            g.DrawString(($"工单：{MesResources.BillNo}"), font, Brushes.Black, (int)(rc.Right + (wid * 10)), (int)(rect.Y + hig * 18.0));
            g.DrawString(($"工单数量：{MesResources.BillNum}"), font, Brushes.Black, (int)(rc.Right + (wid * 30)), (int)(rect.Y + hig * 18.0));

            rc.Width = (int)(rc.Right + wid * 10 - rc.Left);
            this.rectMesInfo = rc;
        }

        /// <summary>
        /// 上料区
        /// </summary>
        /// <param name="g"></param>
        /// <param name="rect"></param>
        private void DrawOnload(Graphics g, Pen pen, Rectangle rect)
        {
            // 模组名字体
            Font font = new Font(this.Font.FontFamily, (float)10.0);

            float wid = (float)(rect.Width / 100.0);
            float hig = (float)(rect.Height / 100.0);
            Pallet[] arrPallet = null;
            Battery[] arrBattery = null;
            RunID runId = RunID.Invalid;

            // 上料夹具
            runId = RunID.OnloadRobot;
            arrPallet = ModulePallet(runId);
            if (null != arrPallet)
            {
                for(int i = 0; i < arrPallet.Length; i++)
                {
                    int div = (int)(wid * 2 * i + wid * 31 * i);
                    g.DrawString(("夹具" + (i + 1)), font, Brushes.Black, (rect.X + (int)(wid * 8 + div)), (rect.Y + hig * 56));
                    Rectangle rc = new Rectangle((rect.X + (int)(wid + div)), (int)(rect.Y + (hig * 3)), (int)(wid * 31), (int)(hig * 52));
                    DrawPallet(g, pen, rc, arrPallet[i], false, true, true);
                    this.rectOnloadRbtPlt[i] = rc;

                    // 夹具位置使能
                    if(!GetPalletPosEnable(runId, i))
                    {
                        Point[] point = new Point[4];
                        point[0] = new Point(rc.X, rc.Y);
                        point[1] = new Point(rc.X + rc.Width, rc.Y + rc.Height);
                        point[2] = new Point(rc.X, rc.Y + rc.Height);
                        point[3] = new Point(rc.X + rc.Width, rc.Y);
                        g.DrawLines(pen, point);
                    }
                }
                if (MachineCtrl.GetInstance().OnloadClear)
                {
                    g.DrawString("上料清尾料中...", font, Brushes.Red, (rect.X + 10), (rect.Y + rect.Height / 3));
                }
            }
            // 机器人抓手及暂存
            runId = RunID.OnloadRobot;
            arrBattery = ModuleBattery(runId);
            if(null != arrBattery)
            {
                // 抓手 - 暂存
                string[] info = new string[] { "抓手", "暂存" };
                for(int i = 0; i < 2; i++)
                {
                    int div = (int)(wid * 8 * i + wid * 10 * i);
                    g.DrawString(info[i], font, Brushes.Black, (rect.X + wid * 43 + div), (rect.Y + hig * 93));
                    Rectangle rc = new Rectangle((int)(rect.X + (wid * 43 + div)), (int)(rect.Y + (hig * 63)), (int)(wid * 15), (int)(hig * 30));
                    DrawBattery(g, pen, rc, (new Battery[] { arrBattery[i * 2], arrBattery[i * 2 + 1] }), false, true, false);
                    if(0 == i)
                    {
                        this.rectOnloadRbtFinger = rc;
                    }
                    else
                    {
                        this.rectOnloadRbtBuffer = rc;
                    }
                }
            }
            RobotActionInfo rbtAction = GetRobotActionInfo(runId, true);
            if(null != rbtAction)
            {
                Rectangle rc = new Rectangle((rect.X ), (int)(rect.Y - 20), (int)(wid * 6), (int)(wid * 6));
                bool con = GetDeviceIsConnect(runId);
                DrawRect(g, (new Pen((con ? Brushes.DarkGreen : Brushes.Red))), rc, (con ? Brushes.DarkGreen : Brushes.Red));
                string rbtInfo = string.Format("{0}:{1}-{2}行-{3}列-{4}", ModuleName(runId)
                    , rbtAction.stationName, (rbtAction.row + 1), (rbtAction.col + 1), RobotDef.RobotOrderName[(int)rbtAction.order]);
                g.DrawString(rbtInfo, (new Font(font.FontFamily, 11, FontStyle.Bold)), Brushes.Black, (rect.X + wid * 7), (rect.Y - 20));
            }

            // 来料收电池
            runId = RunID.OnloadRecv;
            arrBattery = ModuleBattery(runId);
            if(null != arrBattery)
            {
                g.DrawString("收", font, Brushes.Black, (rect.X + wid), (rect.Y + hig * 93));
                // 接收位
                Rectangle rc = new Rectangle((int)(rect.X + (wid)), (int)(rect.Y + (hig * 63)), (int)(wid * 10), (int)(hig * 30));
                DrawBattery(g, pen, rc, arrBattery, true, true, false);
                this.rectOnloadRecv = rc;
            }
            // 扫码位
            runId = RunID.OnloadScan;
            arrBattery = ModuleBattery(runId);
            if(null != arrBattery)
            {
                g.DrawString(ModuleName(runId), font, Brushes.Black, (rect.X + wid * 11), (rect.Y + hig * 93));
                // 接收位
                Rectangle rc = new Rectangle((int)(rect.X + (wid * 13)), (int)(rect.Y + (hig * 63)), (int)(wid * 10), (int)(hig * 30));
                DrawBattery(g, pen, rc, arrBattery, true, true, false);
                this.rectOnloadScan = rc;
            }
            // 取料线
            runId = RunID.OnloadLine;
            arrBattery = ModuleBattery(runId);
            if(null != arrBattery)
            {
                g.DrawString(ModuleName(runId), font, Brushes.Black, (rect.X + wid * 25), (rect.Y + hig * 93));
                // 取料位
                Rectangle rc = new Rectangle((int)(rect.X + (wid * 25)), (int)(rect.Y + (hig * 63)), (int)(wid * 10), (int)(hig * 30));
                DrawBattery(g, pen, rc, arrBattery, true, true, false);
                this.rectOnloadLine = rc;
            }
            // NG输出
            runId = RunID.OnloadNG;
            arrBattery = ModuleBattery(runId);
            if(null != arrBattery)
            {
                g.DrawString(ModuleName(runId), font, Brushes.Black, (rect.X + wid * 83), (rect.Y + hig * 93));
                Rectangle rc = new Rectangle((rect.X + (int)(wid * 82)), (rect.Y + (int)(hig * 63)), (int)(wid * 15), (int)(hig * 30));
                DrawBattery(g, pen, rc, arrBattery, false, true, false);
                this.rectOnloadNG = rc;
            }
            // 假电池输入
            runId = RunID.OnloadFake;
            arrBattery = ModuleBattery(runId);
            if(null != arrBattery)
            {
                g.DrawString(ModuleName(runId), font, Brushes.Black, (rect.X + wid * 73), (rect.Y + hig * 46));
                int rowBatNum = 4;
                for(int i = 0; i < arrBattery.Length / rowBatNum; i++)
                {
                    Rectangle rc = new Rectangle((rect.X + (int)(wid * 72)), (int)(rect.Y + (hig * 3 + hig * 10.5 * i)), (int)(wid * 25), (int)(hig * 10.5));
                    Battery[] arrBat = new Battery[rowBatNum];
                    for(int j = 0; j < rowBatNum; j++)
                    {
                        arrBat[j] = arrBattery[i * rowBatNum + j];
                    }
                    DrawBattery(g, pen, rc, arrBat, false, true, false);
                }
                this.rectOnloadFake = new Rectangle((rect.X + (int)(wid * 72)), (int)(rect.Y + (hig * 3)), (int)(wid * 25), (int)(hig * 10.5 * arrBattery.Length / rowBatNum));
            }
        }

        /// <summary>
        /// 人工操作台
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        private void DrawManualOperate(Graphics g, Pen pen, Rectangle rect)
        {
            // 模组名字体
            Font font = new Font(this.Font.FontFamily, (float)10.0);

            float wid = (float)(rect.Width / 100.0);
            float hig = (float)(rect.Height / 100.0);
            Pallet[] arrPallet = null;
            RunID runId = RunID.Invalid;

            // 人工操作台
            runId = RunID.ManualOperate;
            arrPallet = ModulePallet(runId);
            if(null != arrPallet)
            {
                if (arrPallet.Length > 0)
                {
                    g.DrawString(ModuleName(runId), font, Brushes.Black, (rect.X + (int)(wid * 25)), (rect.Y + hig * 62));
                    Rectangle rc = new Rectangle((int)(rect.X + wid * 11), (rect.Y + (int)(hig * 8)), (int)(wid * 80), (int)(hig * 52));
                    this.rectManualOperate = rc;
                    DrawPalletRect(g, rc, arrPallet[0]);
                }
            }
        }

        /// <summary>
        /// 干燥炉组0：上料区侧的MachineCtrl.HalfDryingOvens数量的干燥炉
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        private void DrawOvenGroup0(Graphics g, Pen pen, Rectangle rect)
        {
            // 模组名字体
            Font font = new Font(this.Font.FontFamily, (float)10.0);

            int halfOven = MachineCtrl.GetInstance().HalfDryingOvens;
            int ovenCount = halfOven;
            float ovenWid = (float)(rect.Width / (ovenCount + 0.35));
            float hig = (float)(rect.Height / 100.0);
            Pallet[] arrPallet = null;
            RunID runId = RunID.Invalid;
            int ovenRow = (int)OvenRowCol.MaxRow;
            int ovenCol = (int)OvenRowCol.MaxCol;
            for(int ovenIdx = 0; ovenIdx < ovenCount; ovenIdx++)
            {
                runId = RunID.DryOven0 + ovenIdx;
                arrPallet = ModulePallet(runId);
                // 干燥炉
                g.DrawString(ModuleName(runId), font, Brushes.Black, (float)(rect.X + ovenWid * ovenIdx + ovenWid / 10.0 * (ovenIdx + 5.0)), (rect.Y + hig * 2));
                Rectangle rcOven = new Rectangle((int)(rect.X + ovenWid * ovenIdx + ovenWid / 10.0 * (ovenIdx + 3.5)), (int)(rect.Y + hig * 2), (int)(6 * hig), (int)(6 * hig));
                bool con = GetDeviceIsConnect(runId);
                DrawRect(g, (new Pen((con ? Brushes.DarkGreen : Brushes.Red))), rcOven, (con ? Brushes.DarkGreen : Brushes.Red));
                rcOven = new Rectangle((int)(rect.X + (ovenWid * ovenIdx + ovenWid / 10.0 * (ovenIdx + 1))), (int)(rect.Y + (hig * 10.0)), (int)(ovenWid), (int)(hig * 80));
                // 绘制腔体：从下往上
                float rowHig = rcOven.Height / (float)ovenRow;
                for(int rowIdx = 0; rowIdx < ovenRow; rowIdx++)
                {
                    Rectangle rcCavity = new Rectangle((rcOven.X), (int)(rcOven.Y + rowHig * (ovenRow - 1 - rowIdx)), (int)(rcOven.Width), (int)(rowHig));
                    DrawCavity(g, rcCavity, GetOvenCavityTransfer(runId, rowIdx) ? (CavityStatus.Maintenance + 1) : GetCavityState(runId, rowIdx), (rowIdx + 1).ToString());
                    this.rectDryOvenCavity[ovenIdx, rowIdx] = rcCavity;

                    // 腔体中夹具
                    if(null != arrPallet)
                    {
                        float pltWid = rcCavity.Width / (float)ovenCol;
                        for(int pltIdx = 0; pltIdx < ovenCol; pltIdx++)
                        {
                            Rectangle rcPlt = new Rectangle((int)(rcCavity.X + pltWid / 10.0 * (pltIdx + 1) + pltWid * pltIdx), (int)(rcCavity.Y + rcCavity.Height / 20.0 * 3)
                                , (int)(pltWid / 10.0 * 7.5), (int)(rcCavity.Height / 20.0 * 14.0));
                            DrawPalletRect(g, rcPlt, arrPallet[rowIdx * ovenCol + pltIdx], (pltIdx + 1).ToString());
                            this.rectDryOvenPlt[ovenIdx, rowIdx * ovenCol + pltIdx] = rcPlt;
                        }
                    }

                    // 腔体使能
                    if(!GetOvenCavityEnable(runId, rowIdx))
                    {
                        Point[] point = new Point[4];
                        point[0] = new Point(rcCavity.X, rcCavity.Y);
                        point[1] = new Point(rcCavity.X + rcCavity.Width, rcCavity.Y + rcCavity.Height);
                        point[2] = new Point(rcCavity.X, rcCavity.Y + rcCavity.Height);
                        point[3] = new Point(rcCavity.X + rcCavity.Width, rcCavity.Y);
                        g.DrawLines(pen, point);
                    }
                    // 腔体保压
                    if (GetOvenCavityPressure(runId, rowIdx))
                    {
                        Point[] point = new Point[4];
                        for(int i = 0; i < 2; i++)
                        {
                            point[i * 2] = new Point(rcCavity.X, (int)(rcCavity.Y + rcCavity.Height / 3.0 * (i + 1)));
                            point[i * 2 + 1] = new Point((rcCavity.X + rcCavity.Width), (int)(rcCavity.Y + rcCavity.Height / 3.0 * (i + 1)));
                            g.DrawLine(pen, point[i * 2], point[i * 2 + 1]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 夹具缓存架
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        private void DrawPalletBuffer(Graphics g, Pen pen, Rectangle rect)
        {
            // 模组名字体
            Font font = new Font(this.Font.FontFamily, (float)10.0);

            float wid = (float)(rect.Width / 100.0);
            float hig = (float)(rect.Height / 100.0);
            Pallet[] arrPallet = null;
            RunID runId = RunID.Invalid;

            // 夹具缓存架
            runId = RunID.PalletBuffer;
            arrPallet = ModulePallet(runId);
            if(null != arrPallet)
            {
                g.DrawString(ModuleName(runId), font, Brushes.Black, (float)(rect.X + (int)(wid * 18)), (rect.Y + hig*92));
                Rectangle rcPltBuf = new Rectangle((int)(rect.X + (wid * 10.0)), (int)(rect.Y + (hig * 10.0)), (int)(wid * 80), (int)(hig * 80));

                int bufCol = 1;
                int bufRow = (int)ModuleMaxPallet.PalletBuffer / bufCol;
                float rowHig = rcPltBuf.Height / (float)bufRow;
                // 绘制缓存架：从下往上
                for(int rowIdx = 0; rowIdx < bufRow; rowIdx++)
                {
                    Rectangle rcRow = new Rectangle((int)(rcPltBuf.X), (int)(rcPltBuf.Y + rowHig * (bufRow - 1 - rowIdx)), (int)(rcPltBuf.Width), (int)(rowHig));
                    DrawRect(g, pen, rcRow, Brushes.Transparent);

                    // 夹具
                    float pltWid = rcRow.Width / (float)bufCol;
                    for(int pltIdx = 0; pltIdx < bufCol; pltIdx++)
                    {
                        Rectangle rcPlt = new Rectangle((int)(rcRow.X + pltWid / 10.0 * (pltIdx + 1) + pltWid * pltIdx), (int)(rcRow.Y + rcRow.Height / 20.0 * 3)
                            , (int)(pltWid / 10.0 * 7.5), (int)(rcRow.Height / 20.0 * 14.0));
                        DrawPalletRect(g, rcPlt, arrPallet[rowIdx * bufCol + pltIdx], (rowIdx + 1).ToString());
                        this.rectPltBufferPlt[rowIdx * bufCol + pltIdx] = rcPlt;
                    }

                    // 腔体使能
                    if(!GetPalletBufferRowEnable(runId, rowIdx))
                    {
                        Point[] point = new Point[4];
                        point[0] = new Point(rcRow.X, rcRow.Y);
                        point[1] = new Point(rcRow.X + rcRow.Width, rcRow.Y + rcRow.Height);
                        point[2] = new Point(rcRow.X, rcRow.Y + rcRow.Height);
                        point[3] = new Point(rcRow.X + rcRow.Width, rcRow.Y);
                        g.DrawLines(pen, point);
                    }
                }
            }
        }

        /// <summary>
        /// 干燥炉组1：剩余的总数 - MachineCtrl.HalfDryingOvens数量的干燥炉
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        private void DrawOvenGroup1(Graphics g, Pen pen, Rectangle rect)
        {
            // 模组名字体
            Font font = new Font(this.Font.FontFamily, (float)10.0);

            int halfOven = MachineCtrl.GetInstance().HalfDryingOvens;
            int ovenCount = (int)OvenInfoCount.OvenCount - halfOven;
            float ovenWid = (float)(rect.Width / (ovenCount + 0.65));
            float hig = (float)(rect.Height / 100.0);
            Pallet[] arrPallet = null;
            RunID runId = RunID.Invalid;
            int ovenRow = (int)OvenRowCol.MaxRow;
            int ovenCol = (int)OvenRowCol.MaxCol;
            for(int ovenIdx = 0; ovenIdx < ovenCount; ovenIdx++)
            {
                runId = RunID.DryOven0 + halfOven + ovenIdx;
                arrPallet = ModulePallet(runId);
                // 干燥炉
                g.DrawString(ModuleName(runId), font, Brushes.Black, (float)(rect.X + ovenWid * ovenIdx + ovenWid / 10.0 * (ovenIdx + 5.0)), (rect.Y + hig * 92));
                Rectangle rcOven = new Rectangle((int)(rect.X + ovenWid * ovenIdx + ovenWid / 10.0 * (ovenIdx + 3.5)), (int)(rect.Y + hig * 91.5), (int)(6 * hig), (int)(6 * hig));
                bool con = GetDeviceIsConnect(runId);
                DrawRect(g, (new Pen((con ? Brushes.DarkGreen : Brushes.Red))), rcOven, (con ? Brushes.DarkGreen : Brushes.Red));
                rcOven = new Rectangle((int)(rect.X + (ovenWid * ovenIdx + ovenWid / 10.0 * (ovenIdx + 1))), (int)(rect.Y + (hig * 10.0)), (int)(ovenWid), (int)(hig * 80));
                // 绘制腔体：从下往上
                float rowHig = rcOven.Height / (float)ovenRow;
                for(int rowIdx = 0; rowIdx < ovenRow; rowIdx++)
                {
                    Rectangle rcCavity = new Rectangle((rcOven.X), (int)(rcOven.Y + rowHig * (ovenRow - 1 - rowIdx)), (int)(rcOven.Width), (int)(rowHig));
                    DrawCavity(g, rcCavity, GetOvenCavityTransfer(runId, rowIdx) ? (CavityStatus.Maintenance + 1) : GetCavityState(runId, rowIdx), (rowIdx + 1).ToString());
                    this.rectDryOvenCavity[halfOven + ovenIdx, rowIdx] = rcCavity;

                    // 腔体中夹具
                    if(null != arrPallet)
                    {
                        float pltWid = rcCavity.Width / (float)ovenCol;
                        for(int pltIdx = 0; pltIdx < ovenCol; pltIdx++)
                        {
                            Rectangle rcPlt = new Rectangle((int)(rcCavity.X + pltWid / 10.0 * (pltIdx + 1) + pltWid * pltIdx), (int)(rcCavity.Y + rcCavity.Height / 20.0 * 3)
                                , (int)(pltWid / 10.0 * 7.5), (int)(rcCavity.Height / 20.0 * 14.0));
                            DrawPalletRect(g, rcPlt, arrPallet[rowIdx * ovenCol + pltIdx], (pltIdx + 1).ToString());
                            this.rectDryOvenPlt[halfOven + ovenIdx, rowIdx * ovenCol + pltIdx] = rcPlt;
                        }
                    }

                    // 腔体使能
                    if(!GetOvenCavityEnable(runId, rowIdx))
                    {
                        Point[] point = new Point[4];
                        point[0] = new Point(rcCavity.X, rcCavity.Y);
                        point[1] = new Point(rcCavity.X + rcCavity.Width, rcCavity.Y + rcCavity.Height);
                        point[2] = new Point(rcCavity.X, rcCavity.Y + rcCavity.Height);
                        point[3] = new Point(rcCavity.X + rcCavity.Width, rcCavity.Y);
                        g.DrawLines(pen, point);
                    }
                    // 腔体保压
                    if(GetOvenCavityPressure(runId, rowIdx))
                    {
                        Point[] point = new Point[4];
                        for(int i = 0; i < 2; i++)
                        {
                            point[i * 2] = new Point(rcCavity.X, (int)(rcCavity.Y + rcCavity.Height / 3.0 * (i + 1)));
                            point[i * 2 + 1] = new Point((rcCavity.X + rcCavity.Width), (int)(rcCavity.Y + rcCavity.Height / 3.0 * (i + 1)));
                            g.DrawLine(pen, point[i * 2], point[i * 2 + 1]);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 调度
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        private void DrawTransfer(Graphics g, Pen pen, Rectangle rect)
        {
            // 模组名字体
            Font font = new Font(this.Font.FontFamily, (float)10.0);

            float wid = (float)(rect.Width / 100.0);
            float hig = (float)(rect.Height / 100.0);
            Pallet[] arrPallet = null;
            RunID runId = RunID.Invalid;

            // 导轨
            g.DrawRectangle(pen, rect);

            // 插料架
            runId = RunID.Transfer;
            arrPallet = ModulePallet(runId);
            if(null != arrPallet)
            {
                Rectangle rcPlt = new Rectangle((int)(rect.X + wid), (int)(rect.Y + hig * 7), (int)(wid * 10.0), (int)(hig * 88));
                //g.DrawRectangle(pen, rcPlt);
                RobotActionInfo actionInfo = GetRobotActionInfo(runId, true);
                if (null == actionInfo)
                {
                    return;
                }
                int halfOven = MachineCtrl.GetInstance().HalfDryingOvens;
                #region // 调整机器人位置
                if(null != actionInfo)
                {
                    switch((TransferRobotStation)actionInfo.station)
                    {
                        case TransferRobotStation.OnloadStation:
                            rcPlt.Offset((int)(rcPlt.Width / 2 * actionInfo.col), 0);
                            break;
                        case TransferRobotStation.PalletBuffer:
                            rcPlt.Offset((int)(rcPlt.Width / 2 * (4.5 + actionInfo.col)), 0);
                            break;
                        case TransferRobotStation.ManualOperate:
                            rcPlt.Offset((int)(rcPlt.Width / 2 * (4.5 + actionInfo.col)), 0);
                            break;
                        case TransferRobotStation.OffloadStation:
                            rcPlt.Offset((int)(rcPlt.Width / 2 * (16 + actionInfo.col)), 0);
                            break;
                        case TransferRobotStation.StationEnd:
                            break;
                        default:
                            {
                                if((actionInfo.station >= (int)TransferRobotStation.DryOven_0) 
                                    && (actionInfo.station < ((int)TransferRobotStation.DryOven_0 + halfOven)))
                                {
                                    int ovenOffset = actionInfo.station - (int)TransferRobotStation.DryOven_0;
                                    rcPlt.Offset((int)(rcPlt.Width / 2 * (ovenOffset * 2.7 + 7.1 + actionInfo.col)), 0);
                                    break;
                                }
                                else if((actionInfo.station >= (int)TransferRobotStation.DryOven_0 + halfOven)
                                    && (actionInfo.station <= ((int)TransferRobotStation.DryOven_All)))
                                {
                                    int ovenOffset = actionInfo.station - (int)TransferRobotStation.DryOven_0 - halfOven;
                                    rcPlt.Offset((int)(rcPlt.Width / 2 * (ovenOffset * 3.3 + actionInfo.col)), 0);
                                    break;
                                }
                                break;
                            }
                    }
                }
                #endregion
                DrawPalletRect(g, rcPlt, arrPallet[0], string.Format("{0}-{1}-{2}", actionInfo.station, (actionInfo.row + 1), (actionInfo.col + 1)));

                Rectangle rc = new Rectangle((rect.X + rect.Width / 3), (int)(rect.Bottom + 8), (int)(wid * 1.3), (int)(wid * 1.3));
                bool con = GetDeviceIsConnect(runId);
                DrawRect(g, (new Pen((con ? Brushes.DarkGreen : Brushes.Red))), rc, (con ? Brushes.DarkGreen : Brushes.Red));
                string rbtInfo = string.Format("{0}:{1}-{2}行-{3}列-{4}", RobotDef.RobotIDName[(int)RobotIndexID.Transfer], actionInfo.stationName
                    , (actionInfo.row + 1), (actionInfo.col + 1), RobotDef.RobotOrderName[(int)actionInfo.order]);
                g.DrawString(rbtInfo, (new Font(font.FontFamily, 11, FontStyle.Bold)), Brushes.Black, (int)(rect.X + rect.Width / 3 + wid * 1.5), (rect.Bottom + 8));
                this.rectTransfer = rcPlt;
            }
        }

        /// <summary>
        /// 下料区
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        private void DrawOffLoad(Graphics g, Pen pen, Rectangle rect)
        {
            // 模组名字体
            Font font = new Font(this.Font.FontFamily, (float)10.0);

            float wid = (float)(rect.Width / 74.0);
            float hig = (float)(rect.Height / 100.0);
            Pallet[] arrPallet = null;
            Battery[] arrBattery = null;
            RunID runId = RunID.Invalid;

            // 下料夹具
            runId = RunID.OffloadBattery;
            arrPallet = ModulePallet(runId);
            if (null != arrPallet)
            {
                for (int i = 0; i < arrPallet.Length; i++)
                {
                    int div = (int)(wid * 2 * i + wid * 23 * i);
                    g.DrawString(("夹具" + (i + 1)), font, Brushes.Black, (rect.X + (int)(wid * 5 + div)), (rect.Y + hig * 56));
                    Rectangle rc = new Rectangle((rect.X + (int)(wid + div)), (int)(rect.Y + hig * 3), (int)(wid * 23), (int)(hig * 52));
                    DrawPallet(g, pen, rc, arrPallet[i], false, true, true);
                    this.rectOffloadBatPlt[i] = rc;
                }
                //if (MachineCtrl.GetInstance().OffloadClear)
                //{
                //    g.DrawString("下料清尾料中...", font, Brushes.Red, (rect.X + 10), (rect.Y + rect.Height / 2));
                //}
            }
            RobotActionInfo rbtAction = GetRobotActionInfo(runId, true);
            if(null != rbtAction)
            {
                string rbtInfo = string.Format("{0}:{1}-{2}行-{3}列-{4}", ModuleName(runId)
                    , rbtAction.stationName, (rbtAction.row + 1), (rbtAction.col + 1), RobotDef.RobotOrderName[(int)rbtAction.order]);
                g.DrawString(rbtInfo, (new Font(font.FontFamily, 11, FontStyle.Bold)), Brushes.Black, (rect.X), (rect.Y - 20));
            }

            // 下料 抓手  及 暂存
            runId = RunID.OffloadBattery;
            arrBattery = ModuleBattery(runId);
            if (null != arrBattery)
            {
                // 抓手 - 暂存
                string[] info = new string[] { "抓手", "暂存" };
                for (int i = 0; i < 2; i++)
                {
                    int div = (int)(wid * 5 * i + wid * 7 * i);
                    g.DrawString(info[i], font, Brushes.Black, (rect.X + wid  + div), (rect.Y + hig * 93));
                    Rectangle rc = new Rectangle((rect.X + (int)(wid + div)), (rect.Y + (int)(hig * 63)), (int)(wid * 10), (int)(hig * 30));
                    DrawBattery(g, pen, rc, (new Battery[] { arrBattery[i * 2], arrBattery[i * 2 + 1] }), false, true, false);
                    if (0 == i)
                    {
                        this.rectOffloadBatFinger = rc;
                    }
                    else
                    {
                        this.rectOffloadBatBuffer = rc;
                    }
                 }
            }
            // 待检测输出
            runId = RunID.OffloadDetect;
            arrBattery = ModuleBattery(runId);
            if (null != arrBattery)
            {
                g.DrawString(ModuleName(runId), font, Brushes.Black, (rect.X + wid * 32), (rect.Y + hig * 93));
                Rectangle rc = new Rectangle((rect.X + (int)(wid * 32)), (rect.Y + (int)(hig * 63)), (int)(wid * 10), (int)(hig * 30));
                DrawBattery(g, pen, rc, arrBattery, false, true, false);
                this.rectOffloadDetect = rc;
            }
            // 下料NG电池输出
            runId = RunID.OffloadNG;
            arrBattery = ModuleBattery(runId);
            if (null != arrBattery)
            {
                g.DrawString(ModuleName(runId), font, Brushes.Black, (rect.X + wid * 56), (rect.Y + hig * 93));
                Rectangle rc = new Rectangle((rect.X + (int)(wid * 56)), (rect.Y + (int)(hig * 63)), (int)(wid * 15), (int)(hig * 30));
                DrawBattery(g, pen, rc, arrBattery, false, true, false);
                this.rectOffloadNG = rc;
            }
            // 下料物流线
            runId = RunID.OffloadLine;
            arrBattery = ModuleBattery(runId);
            if (null != arrBattery)
            {
                g.DrawString(ModuleName(runId), font, Brushes.Black, (rect.X + wid * 55), (rect.Y + hig * 40));
                for(int i = 0; i < 2; i++)
                {
                    Rectangle rc = new Rectangle((rect.X + (int)(wid * 8 * i + wid * 55)), (rect.Y + (int)(hig * 3)), (int)(wid * 8), (int)(hig * 35));
                    DrawBattery(g, pen, rc, (new Battery[] { arrBattery[i * 2], arrBattery[i * 2 + 1] }), true, true, false);
                }
                this.rectOffloadLine = new Rectangle((rect.X + (int)(wid * 62)), (rect.Y + (int)(hig)), (int)(wid * 6), (int)(hig * 30 * 2));
            }
            // 冷却系统
            runId = RunID.CoolingSystem;
            BatteryLine batLine = ModuleBatteryLine(runId);
            if(null != batLine)
            {
                g.DrawString(ModuleName(runId), font, Brushes.Black, (rect.X + wid * 53), (rect.Y + hig * 92));
                int maxRow = batLine.MaxRow;
                int maxCol = batLine.MaxCol;
                for(int row = 0; row < maxRow; row++)
                {
                    for(int col = 0; col < maxCol; col++)
                    {
                        Rectangle rc = new Rectangle((int)(rect.X + wid * 37 + wid * 37 / maxCol * col), (int)(rect.Y + hig * 37 + hig * 52 / maxRow * (maxRow - 1 - row)), (int)(wid * 37 / maxCol), (int)(hig * 52 / maxRow));
                        DrawBattery(g, pen, rc, (new Battery[] { batLine.Battery[row, col] }), true, false, false);
                    }
                }
                this.rectCoolingSystem = new Rectangle((rect.X + (int)(wid * 37)), (rect.Y + (int)(hig * 15)), (int)(wid * 37), (int)(hig * 30 * 2));
            }
            // 冷却下料
            runId = RunID.CoolingOffload;
            arrBattery = ModuleBattery(runId);
            if(null != arrBattery)
            {
                // 抓手 - 暂存
                string[] info = new string[] { "抓手", "暂存" };
                for(int i = 0; i < 2; i++)
                {
                    int div = (int)(wid * 36 + wid * 5 * i + wid * 7 * i);
                    g.DrawString(info[i], font, Brushes.Black, (rect.X + wid + div), (rect.Y + hig * 32));
                    Rectangle rc = new Rectangle((rect.X + (int)(wid + div)), (rect.Y + (int)(hig)), (int)(wid * 10), (int)(hig * 30));
                    DrawBattery(g, pen, rc, (new Battery[] { arrBattery[i * 2], arrBattery[i * 2 + 1] }), false, true, false);
                    if(0 == i)
                    {
                        this.rectCoolingFinger = rc;
                    }
                    else
                    {
                        this.rectCoolingBuffer = rc;
                    }
                }
            }
        }

        #endregion

        #region // 绘制工具

        /// <summary>
        /// 绘制单个电池
        /// </summary>
        /// <param name="g"></param>
        /// <param name="rect">区域</param>
        /// <param name="batState">电池状态</param>
        /// <param name="withTxet">附带文本</param>
        private void DrawBattery(Graphics g, Pen pen, Rectangle rect, BatteryStatus batState, string withTxet)
        {
            Font font = new Font(this.Font.FontFamily, (float)10.0);
            StringFormat strFormat = new StringFormat();//文本格式
            strFormat.LineAlignment = StringAlignment.Center;//垂直居中
            strFormat.Alignment = StringAlignment.Center;//水平居中
            SolidBrush brush = null;
            switch(batState)
            {
                case BatteryStatus.Invalid:
                    brush = new SolidBrush(Color.Transparent);
                    break;
                case BatteryStatus.OK:
                    brush = new SolidBrush(Color.Green);
                    break;
                case BatteryStatus.NG:
                    brush = new SolidBrush(Color.Red);
                    break;
                case BatteryStatus.Fake:
                    brush = new SolidBrush(Color.Blue);
                    break;
                case BatteryStatus.ReFake:
                    brush = new SolidBrush(Color.BlueViolet);
                    break;
                case BatteryStatus.Detect:
                    brush = new SolidBrush(Color.SteelBlue);
                    break;
                case BatteryStatus.FakeTag:
                    brush = new SolidBrush(Color.DodgerBlue);
                    break;
                default:
                    brush = new SolidBrush(Color.Black);
                    break;
            }
            // 先填充，后绘制，否则会出现白边
            g.FillRectangle(brush, rect);
            g.DrawRectangle(pen, rect);
            g.DrawString(withTxet, font, Brushes.Black, rect, strFormat);
        }

        /// <summary>
        /// 绘制一组电池
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect">区域</param>
        /// <param name="arrBat">电池组</param>
        /// <param name="level">true水平放置，false垂直放置</param>
        /// <param name="withID">带有电池ID</param>
        /// <param name="withCode">带有电池条码</param>
        private void DrawBattery(Graphics g, Pen pen, Rectangle rect, Battery[] arrBat, bool level, bool withID, bool withCode)
        {
            if(null == arrBat)
            {
                return;
            }
            string info = "";
            int length = arrBat.Length;
            int wid = level ? rect.Width : (rect.Width / length);
            int hig = level ? (rect.Height / length) : rect.Height;
            for(int i = 0; i < length; i++)
            {
                int nleft = level ? 0 : (rect.Width / length * i);
                int ntop = level ? (rect.Height / length * i) : 0;
                Rectangle rcBat = new Rectangle((rect.Left + nleft), (rect.Top + ntop), wid, hig);
                info = withID ? (i + 1).ToString() : "";
                info = withCode ? arrBat[i].Code : info;
                DrawBattery(g, pen, rcBat, arrBat[i].Type, info);
            }
        }

        /// <summary>
        /// 绘制夹具，带电池
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect">区域</param>
        /// <param name="pallet">夹具数据</param>
        /// <param name="level">水平绘制电池</param>
        /// <param name="topToDown">从顶到底开始绘制</param>
        /// <param name="leftToRight">从左到右开始绘制</param>
        private void DrawPallet(Graphics g, Pen pen, Rectangle rect, Pallet pallet, bool level, bool topToDown, bool leftToRight)
        {
            int maxRow = (int)pallet.MaxRow;
            int maxCol = (int)pallet.MaxCol;

            Color oldColor = pen.Color;
            // 设置NG夹具画笔颜色
            if(PalletStatus.NG == pallet.State)
            {
                pen.Color = Color.Red;
            }
            if (PalletStatus.Invalid == pallet.State)
            {
                DrawPalletRect(g, rect, pallet);
                return;
            }
            // 绘制电池
            if (level)
            {
                if (topToDown)
                {
                    for(int row = 0; row < maxRow; row++)
                    {
                        for(int col = 0; col < maxCol; col++)
                        {
                            Rectangle rc = new Rectangle((rect.X + rect.Width / maxCol * col), (rect.Y + rect.Height / maxRow * row), (rect.Width / maxCol), (rect.Height / maxRow));
                            DrawBattery(g, pen, rc, (new Battery[] { pallet.Battery[row, col] }), true, false, false);
                        }
                    }
                }
                else
                {
                    for(int row = 0; row < maxRow; row++)
                    {
                        for(int col = 0; col < maxCol; col++)
                        {
                            Rectangle rc = new Rectangle((rect.X + rect.Width / maxCol * col), (rect.Y + rect.Height / maxRow * (maxRow - row)), (rect.Width / maxCol), (rect.Height / maxRow));
                            DrawBattery(g, pen, rc, (new Battery[] { pallet.Battery[row, col] }), true, false, false);
                        }
                    }
                }
            }
            else
            {
                if (topToDown)
                {
                    for(int row = 0; row < maxRow; row++)
                    {
                        for(int col = 0; col < maxCol; col++)
                        {
                            Rectangle rc = new Rectangle((rect.X + rect.Width / maxRow * row), (rect.Y + rect.Height / maxCol * col), (rect.Width / maxRow), (rect.Height / maxCol));
                            DrawBattery(g, pen, rc, (new Battery[] { pallet.Battery[row, col] }), true, false, false);
                        }
                    }
                }
                else
                {
                    if (leftToRight)
                    {
                        for(int row = 0; row < maxRow; row++)
                        {
                            for(int col = 0; col < maxCol; col++)
                            {
                                Rectangle rc = new Rectangle((rect.X + rect.Width / maxRow * row), (rect.Y + rect.Height / maxCol * (maxCol - 1 - col)), (rect.Width / maxRow), (rect.Height / maxCol));
                                DrawBattery(g, pen, rc, (new Battery[] { pallet.Battery[row, col] }), true, false, false);
                            }
                        }
                    }
                    else
                    {
                        for(int row = 0; row < maxRow; row++)
                        {
                            for(int col = 0; col < maxCol; col++)
                            {
                                Rectangle rc = new Rectangle((rect.Right - rect.Width / maxRow * (row + 2)), (rect.Y + rect.Height / maxCol * (maxCol - 1 - col)), (rect.Width / maxRow), (rect.Height / maxCol));
                                DrawBattery(g, pen, rc, (new Battery[] { pallet.Battery[row, col] }), true, false, false);
                            }
                        }
                    }
                }
            }

            // 复原原有画笔颜色
            pen.Color = oldColor;
        }

        /// <summary>
        /// 绘制夹具矩形框，无电池
        /// </summary>
        /// <param name="g"></param>
        /// <param name="rect"></param>
        /// <param name="cavityState"></param>
        /// <param name="withTxet"></param>
        private void DrawPalletRect(Graphics g, Rectangle rect, Pallet pallet, string withTxet = null)
        {
            Pen pen = null;
            Brush brush = null;
            switch(pallet.State)
            {
                case PalletStatus.Invalid:
                    pen = new Pen(Color.Black);
                    brush = Brushes.Transparent;
                    break;
                case PalletStatus.OK:
                    pen = pallet.HasFake() ? (new Pen(Color.Blue, 2)) : (new Pen(Color.Black));
                    brush = pallet.IsEmpty() ? Brushes.DarkGray : Brushes.Green;
                    break;
                case PalletStatus.NG:
                    pen = new Pen(Color.Red, 2);
                    brush = pallet.IsEmpty() ? Brushes.Red : Brushes.DarkRed;
                    break;
                case PalletStatus.Detect:
                case PalletStatus.WaitResult:
                    pen = pallet.HasFake() ? (new Pen(Color.Blue, 2)) : (new Pen(Color.Black));
                    brush = Brushes.Yellow;
                    break;
                case PalletStatus.WaitOffload:
                    pen = pallet.HasFake() ? (new Pen(Color.Blue, 2)) : (new Pen(Color.Black));
                    brush = Brushes.DarkGoldenrod;
                    break;
                case PalletStatus.ReputFake:
                case PalletStatus.Rebaking:
                    pen = pallet.HasFake() ? (new Pen(Color.Blue, 2)) : (new Pen(Color.Black));
                    brush = Brushes.Magenta;
                    break;
                default:
                    break;
            }
            if (null != brush)
            {
                DrawRect(g, pen, rect, brush, Color.Black, withTxet);
            }
        }

        /// <summary>
        /// 绘制腔体状态
        /// </summary>
        /// <param name="g"></param>
        /// <param name="rect"></param>
        /// <param name="cavityState"></param>
        /// <param name="withTxet"></param>
        private void DrawCavity(Graphics g, Rectangle rect, CavityStatus cavityState, string withTxet = null)
        {
            Brush brush = null;
            switch(cavityState)
            {
                case CavityStatus.Normal:
                    brush = Brushes.Transparent;
                    break;
                case CavityStatus.Heating:
                    brush = Brushes.Yellow;
                    break;
                case CavityStatus.WaitDetect:
                    brush = Brushes.Cyan;
                    break;
                case CavityStatus.WaitResult:
                    brush = new HatchBrush(HatchStyle.Cross, Color.Transparent, Color.DarkCyan);
                    break;
                case CavityStatus.WaitRebaking:
                    brush = new HatchBrush(HatchStyle.Cross, Color.Transparent, Color.Magenta);
                    break;
                case CavityStatus.Maintenance:
                    brush = new HatchBrush(HatchStyle.Cross, Color.Transparent, Color.Black);
                    break;
                case CavityStatus.Maintenance + 1:
                    brush = new HatchBrush(HatchStyle.Cross, Color.Transparent, Color.IndianRed);
                    break;
                default:
                    brush = new HatchBrush(HatchStyle.Trellis, Color.Transparent, Color.Black);
                    break;
            }
            if (null != brush)
            {
                DrawRect(g, (new Pen(Color.Black)), rect, brush, Color.Black, withTxet);
            }
        }

        /// <summary>
        /// 绘制一个带颜色的矩形
        /// </summary>
        /// <param name="g"></param>
        /// <param name="rect"></param>
        /// <param name="lineColor">线条颜色</param>
        /// <param name="fillBrush">填充颜色</param>
        /// <param name="textColor">文本颜色</param>
        /// <param name="withTxet">附带文本</param>
        /// <param name="fontSize">文本字体大小</param>
        private void DrawRect(Graphics g, Pen pen, Rectangle rect, Brush fillBrush, Color textColor = new Color(), string withTxet = null, float fontSize = (float)10.0)
        {
            Font font = new Font(this.Font.FontFamily, fontSize);
            StringFormat strFormat = new StringFormat();//文本格式
            strFormat.LineAlignment = StringAlignment.Center;//垂直居中
            strFormat.Alignment = StringAlignment.Center;//水平居中
            Brush txtBrush = new SolidBrush(textColor);
            g.FillRectangle(fillBrush, rect);
            g.DrawRectangle(pen, rect);
            if (null != withTxet)
            {
                g.DrawString(withTxet, font, txtBrush, rect, strFormat);
            }
        }
        #endregion

        #region // 模组数据

        /// <summary>
        /// 获取模组名
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private string ModuleName(RunID id)
        {
            string name = "";
            switch(id)
            {
                case RunID.OnloadRecv:
                    name = "来料收电池";
                    break;
                case RunID.OnloadLine:
                    name = "取料线";
                    break;
                case RunID.OnloadScan:
                    name = "中转";
                    break;
                case RunID.OnloadRobot:
                    name = "上料机器人";
                    break;
                case RunID.OnloadNG:
                    name = "NG输出";
                    break;
                case RunID.OnloadFake:
                    name = "上假电池";
                    break;
                case RunID.Transfer:
                    name = "调度模组";
                    break;
                case RunID.ManualOperate:
                    name = "人工台";
                    break;
                case RunID.PalletBuffer:
                    name = "夹具缓存架";
                    break;
                case RunID.OffloadBattery:
                    name = "电池下料";
                    break;
                case RunID.OffloadNG:
                    name = "NG输出";
                    break;
                case RunID.OffloadDetect:
                    name = "下待测";
                    break;
                case RunID.OffloadLine:
                    name = "下料线";
                    break;
                case RunID.CoolingSystem:
                    name = "冷却系统";
                    break;
                case RunID.CoolingOffload:
                    name = "冷却下料";
                    break;
                default:
                    if(RunID.DryOven0 <= id && id < RunID.DryOvenALL)
                    {
                        name = "干燥炉" + ((int)id - (int)RunID.DryOven0 + 1);
                    }
                    break;
            }
            return name;
        }

        /// <summary>
        /// 获取模组夹具
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private Pallet[] ModulePallet(RunID id)
        {
            return MachineCtrl.GetInstance().GetModulePallet(id);
        }

        /// <summary>
        /// 获取模组电池
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private Battery[] ModuleBattery(RunID id)
        {
            RunProcess run = MachineCtrl.GetInstance().GetModule(id);
            // 模组存在，使用本地数据
            if(null != run)
            {
                return run.Battery;
            }
            // 模组不存在，使用网络数据
            else
            {
//                 ModuleSocketData socketData = MachineCtrl.GetInstance().GetModuleSocketData(id);
//                 if(null != socketData)
//                 {
//                     return socketData.battery;
//                 }
            }
            return null;
        }

        /// <summary>
        /// 获取模组电池线（冷却系统电池）
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private BatteryLine ModuleBatteryLine(RunID id)
        {
            return MachineCtrl.GetInstance().GetModuleBatteryLine(id);
        }

        /// <summary>
        /// 获取干燥炉/机器人连接状态：true连接，false断开
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool GetDeviceIsConnect(RunID id)
        {
            return MachineCtrl.GetInstance().GetDeviceIsConnect(id);
        }

        /// <summary>
        /// 获取干燥炉腔体状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        private CavityStatus GetCavityState(RunID id, int cavityIdx)
        {
            return MachineCtrl.GetInstance().GetDryingOvenCavityState(id, cavityIdx);
        }

        /// <summary>
        /// 获取干燥炉腔体使能状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        private bool GetOvenCavityEnable(RunID id, int cavityIdx)
        {
            return MachineCtrl.GetInstance().GetDryingOvenCavityEnable(id, cavityIdx);
        }

        /// <summary>
        /// 获取干燥炉腔体保压状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        private bool GetOvenCavityPressure(RunID id, int cavityIdx)
        {
            return MachineCtrl.GetInstance().GetDryingOvenCavityPressure(id, cavityIdx);
        }

        /// <summary>
        /// 获取干燥炉腔体转移状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        private bool GetOvenCavityTransfer(RunID id, int cavityIdx)
        {
            return MachineCtrl.GetInstance().GetDryingOvenCavityTransfer(id, cavityIdx);
        }

        /// <summary>
        /// 获取上下料夹具位使能状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pltIdx"></param>
        /// <returns></returns>
        private bool GetPalletPosEnable(RunID id, int pltIdx)
        {
            return MachineCtrl.GetInstance().GetPalletPosEnable(id, pltIdx);
        }

        /// <summary>
        /// 获取缓存架层使能状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="rowIdx"></param>
        /// <returns></returns>
        private bool GetPalletBufferRowEnable(RunID id, int rowIdx)
        {
            return MachineCtrl.GetInstance().GetPalletBufferRowEnable(id, rowIdx);
        }

        /// <summary>
        /// 获取机器人动作信息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="autoAction"></param>
        /// <returns></returns>
        private RobotActionInfo GetRobotActionInfo(RunID id, bool autoAction)
        {
            return MachineCtrl.GetInstance().GetRobotActionInfo(id, autoAction);
        }

        #endregion

        #region // 鼠标悬停提示信息

        void InitModuleRectangle()
        {
            // MES信息
            this.rectMesInfo = new Rectangle();
            // 上料
            this.rectOnloadLine = new Rectangle();
            this.rectOnloadScan = new Rectangle();
            this.rectOnloadRbtPlt = new Rectangle[(int)ModuleMaxPallet.OnloadRobot];
            for(int i = 0; i < this.rectOnloadRbtPlt.Length; i++)
            {
                this.rectOnloadRbtPlt[i] = new Rectangle();
            }
            this.rectOnloadRbtFinger = new Rectangle();
            this.rectOnloadRbtBuffer = new Rectangle();
            this.rectOnloadNG = new Rectangle();
            this.rectOnloadFake = new Rectangle();

            // 调度
            this.rectTransfer = new Rectangle();
            this.rectManualOperate = new Rectangle();
            this.rectPltBufferPlt = new Rectangle[(int)ModuleMaxPallet.PalletBuffer];
            for(int i = 0; i < this.rectPltBufferPlt.Length; i++)
            {
                this.rectPltBufferPlt[i] = new Rectangle();
            }

            // 下料
            this.rectOffloadBatPlt = new Rectangle[(int)ModuleMaxPallet.OffloadBattery];
            for(int i = 0; i < this.rectOffloadBatPlt.Length; i++)
            {
                this.rectOffloadBatPlt[i] = new Rectangle();
            }
            this.rectOffloadBatFinger = new Rectangle();
            this.rectOffloadBatBuffer = new Rectangle();
            this.rectOffloadNG = new Rectangle();
            this.rectOffloadDetect = new Rectangle();
            this.rectOffloadLine = new Rectangle();
            this.rectCoolingSystem = new Rectangle();
            this.rectCoolingFinger = new Rectangle();
            this.rectCoolingBuffer = new Rectangle();

            // 干燥炉：炉子,腔体/夹具
            this.rectDryOvenCavity = new Rectangle[(int)OvenInfoCount.OvenCount, (int)OvenRowCol.MaxRow];
            for(int i = 0; i < this.rectDryOvenCavity.GetLength(0); i++)
            {
                for(int j = 0; j < this.rectDryOvenCavity.GetLength(1); j++)
                {
                    this.rectDryOvenCavity[i, j] = new Rectangle();
                }
            }
            this.rectDryOvenPlt = new Rectangle[(int)OvenInfoCount.OvenCount, (int)ModuleMaxPallet.DryingOven];
            for(int i = 0; i < this.rectDryOvenPlt.GetLength(0); i++)
            {
                for(int j = 0; j < this.rectDryOvenPlt.GetLength(1); j++)
                {
                    this.rectDryOvenPlt[i, j] = new Rectangle();
                }
            }
        }

        /// <summary>
        /// 调整提示窗口位置
        /// </summary>
        void AdjustTipPos(ref Rectangle rcDest)
        {
            // 1.假设窗口显示
            Rectangle rcCurTip = new Rectangle();
            rcCurTip.Width = this.tip.GetContentWidth();
            rcCurTip.Height = this.tip.GetContentHeight();
            rcCurTip.X = Cursor.Position.X - rcCurTip.Width / 2;
            rcCurTip.Y = Cursor.Position.Y - rcCurTip.Height;

            // 2.计算窗口到屏幕上下左右的距离
            Rectangle rcScreen = new Rectangle();
            rcScreen = Screen.GetWorkingArea(this);
            int leftDis = rcCurTip.Left - rcScreen.Left;
            int rightDis = rcScreen.Right - rcCurTip.Right;
            int topDis = rcCurTip.Top - rcScreen.Top;
            int bottomDis = rcScreen.Bottom - rcCurTip.Bottom;

            // 3.计算显示位置
            // 在上方显示
            if(topDis >= 0 && leftDis >= 0 && rightDis >= 0)
            {
                rcCurTip.Offset(0, 0);
            }
            // 在下方显示
            else if((bottomDis >= rcCurTip.Height / 2) && leftDis >= 0 && rightDis >= 0)
            {
                rcCurTip.Offset(0, rcCurTip.Height);
            }
            // 在左边显示
            else if(leftDis >= rcCurTip.Width / 2 && topDis >= 0)
            {
                rcCurTip.Offset(-rcCurTip.Width / 2, 0);
            }
            // 在右边显示
            else if(rightDis >= rcCurTip.Width / 2 && topDis > 0)
            {
                rcCurTip.Offset(rcCurTip.Width / 2, 0);
            }
            // 在右上方显示
            else if(topDis >= 0 && leftDis < 0 && rightDis >= rcCurTip.Width / 2)
            {
                rcCurTip.Offset(rcCurTip.Width / 2, 0);
            }
            // 在左上方显示
            else if(topDis >= 0 && leftDis >= rcCurTip.Width / 2 && rightDis < 0)
            {
                rcCurTip.Offset(-rcCurTip.Width / 2, 0);
            }
            // 在右下方显示
            else if(bottomDis >= rcCurTip.Height && leftDis < 0 && rightDis >= rcCurTip.Width / 2)
            {
                rcCurTip.Offset(rcCurTip.Width / 2, rcCurTip.Height);
            }
            // 在左下方显示
            else if((bottomDis >= rcCurTip.Height / 2) && leftDis >= rcCurTip.Width / 2 && rightDis < 0)
            {
                rcCurTip.Offset(-rcCurTip.Width / 2, rcCurTip.Height);
            }
            // 默认在上方显示
            else
            {
                rcCurTip.Offset(0, 0);
            }
            if (rcCurTip.Size.Width < 300)
            {
                rcCurTip.Size = new Size(300, rcCurTip.Height);
            }

            rcDest.Size = rcCurTip.Size;
            rcDest.Location = rcCurTip.Location;
        }

        bool ShowToolTip(string html)
        {
            try
            {
                this.Invoke(new Action(() =>
                {
                    if (!this.tipShow)
                    {
                        this.tipShow = true;
                        this.tip = new TipDlg();
                        this.tip.SetHtml(html);
                        Rectangle rcTip = new Rectangle();
                        AdjustTipPos(ref rcTip);
                        this.tip.Visible = true;
                        this.tip.Location = rcTip.Location;
                        this.tip.Size = rcTip.Size;
                        this.tip.Show();
                    }
                }));
                return true;
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("OverViewPage", "ShowToolTip()  error: " + ex.Message);
            }
            return false;
        }

        /// <summary>
        /// 创建电池Html表
        /// </summary>
        /// <param name="bat">电池数据</param>
        /// <param name="maxRow">最大行</param>
        /// <param name="maxCol">最大列</param>
        /// <param name="rowDiv">行间距数</param>
        /// <returns></returns>
        string CreateBatRowCol(Battery[,] bat, int maxRow, int maxCol, int rowDiv)
        {
            string html = "";
            if (bat.Length > 0)
            {
                html = ("<table border=1 cellspacing=0 width = 50 border-collapse=collapse>");
                for(int col = 0; col < maxCol; col++)
                {
                    html += ("<tr>");
                    for(int row = 0; row < maxRow; row++)
                    {
                        if ((0 != row) && (0 == (row % rowDiv)))
                        {
                            html += ("<tr>");
                        }
                        string info = "";
                        if(0 == col)
                        {
                            info = (row + 1).ToString();
                        }
                        else if(0 == row)
                        {
                            info = (col + 1).ToString();
                        }
                        string code = string.IsNullOrEmpty(bat[row, col].Code) ? info : bat[row, col].Code;
                        switch(bat[row, col].Type)
                        {
                            case BatteryStatus.NG:                // NG
                                info = string.Format("<td style=\"color:red;\">{0}</td>", code);
                                break;
                            case BatteryStatus.Fake:              // 假电池
                            case BatteryStatus.Detect:            // 待检测假电池
                            case BatteryStatus.FakeTag:            // 待检测假电池
                                info = string.Format("<td style=\"color:blue;\">{0}</td>", code);
                                break;
                            case BatteryStatus.ReFake:            // 回炉假电池
                                info = string.Format("<td style=\"color:darkcyan;\">{0}</td>", code);
                                break;
                            default:
                                info = string.Format("<td>{0}</td>", code);
                                break;
                        }
                        html += info;
                        if(0 == ((row + 1) % rowDiv))
                        {
                            html += ("</tr>");
                        }
                    }
                    html += ("</tr>");
                }
                html += ("</table>");
            }
            return html;
        }

        bool ShowPallet(string title, string subTitle, Pallet plt)
        {
            string html = string.Format("<table border=1 cellspacing=0><tr><th><b>【{0}】</b></th></tr>", title);
            html += string.Format("<tr><td align=center>{0}</td></tr>", subTitle);
            if(plt.State > PalletStatus.Invalid)
            {
                html += "<tr><td><ul>";
                string state, stage;
                state = stage = "";
                switch(plt.State)
                {
                    case PalletStatus.Invalid:
                        state = "无夹具";
                        break;
                    case PalletStatus.OK:
                        state = "有效OK";
                        break;
                    case PalletStatus.NG:
                        state = "有效NG";
                        break;
                    case PalletStatus.Detect:
                        state = "等待检测";
                        break;
                    case PalletStatus.WaitResult:
                        state = "等待结果";
                        break;
                    case PalletStatus.WaitOffload:
                        state = "等待下料";
                        break;
                    case PalletStatus.ReputFake:
                        state = "水含量超标，等待放回假电池";
                        break;
                    case PalletStatus.Rebaking:
                        state = "水含量超标，已放回假电池";
                        break;
                }
                switch(plt.Stage)
                {
                    case PalletStage.Invalid:
                        stage = "无效";
                        break;
                    case PalletStage.Onload:
                        stage = "上料完成";
                        break;
                    case PalletStage.Baked:
                        stage = "干燥完成";
                        break;
                    case PalletStage.Offload:
                        stage = "下料完成";
                        break;
                }
                html += $"<li>{state}[{(int)plt.State}]：{plt.Code}</li>";
                html += $"<li>{(plt.NeedFake ? "假电池夹具" : "正常夹具")}：{stage}[{(int)plt.Stage}]</li>";
                if (plt.SrcStation > -1)
                {
                    html += $"<li>来源：{plt.SrcStation} - {(plt.SrcRow + 1)} - {(plt.SrcCol + 1)}</li>";
                }
                if (plt.StartDate > DateTime.MinValue)
                {
                    html += $"<li>加热开始时间：{plt.StartDate.ToString(Def.DateFormal)}</li>";
                }
                if(plt.EndDate > DateTime.MinValue)
                {
                    html += $"<li>加热结束时间：{plt.EndDate.ToString(Def.DateFormal)}</li>";
                }
                html += $"</ul></td></tr>";
                html += $"<tr><td>{CreateBatRowCol(plt.Battery, plt.MaxRow, plt.MaxCol, plt.MaxRow / 3 + 1)}</td></tr>";
            }
            else
            {
                html += string.Format("<tr><td align=center>无夹具</td></tr>");
            }
            html += "</table>";
            return ShowToolTip(html);
        }

        bool ShowBattery(string title, Battery[] bat, int row, int col)
        {
            string html = string.Format("<table border=1 cellspacing=0><tr><th><b>【{0}】</b></th></tr>", title);
            if((null != bat) && (bat.Length > 0))
            {
                Battery[,] batArray = new Battery[row, col];
                for(int i = 0; i < row; i++)
                {
                    for(int j = 0; j < col; j++)
                    {
                        batArray[i, j] = bat[i * col + j];
                    }
                }
                html += string.Format("<tr><td>{0}</td></tr>", CreateBatRowCol(batArray, row, col, row + 1));
            }
            else
            {
                html += string.Format("<tr><td align=center>无电池</td></tr>");
            }
            html += "</table>";
            return ShowToolTip(html);
        }

        bool ShowBatteryLine(string title, string subTitle, BatteryLine batLine)
        {
            string html = string.Format("<table border=1 cellspacing=0><tr><th><b>【{0}】</b></th></tr>", title);
            html += string.Format("<tr><td align=center>{0}</td></tr>", subTitle);

            html += "<tr><td><ul>";
            html += string.Format("<li>冷却系统：{0}行 - {1}列</li>", batLine.MaxRow, batLine.MaxCol);
            html += string.Format("</ul></td></tr>");

            html += string.Format("<tr><td>{0}</td></tr>", CreateBatRowCol(batLine.Battery, batLine.MaxRow, batLine.MaxCol, batLine.MaxRow / 2));
            html += "</table>";
            return ShowToolTip(html);
        }

        bool ShowCavity(string title, string subTitle, RunID id, int cavityIdx)
        {
            string html = $"<table border=1 cellspacing=0><tr><th><b>【{title}】</b></th></tr>";
            html += string.Format("<tr><td align=center>{0}</td></tr>", subTitle);
            html += "<tr><td><ul>";
            string state = "";
            switch(GetCavityState(id, cavityIdx))
            {
                case CavityStatus.Unknown:
                    state = "未知";
                    break;
                case CavityStatus.Normal:
                    state = "正常";
                    break;
                case CavityStatus.Heating:
                    state = "加热中";
                    break;
                case CavityStatus.WaitDetect:
                    state = "等待检测";
                    break;
                case CavityStatus.WaitResult:
                    state = "待上传水含量";
                    break;
                case CavityStatus.WaitRebaking:
                    state = "等待回炉";
                    break;
                case CavityStatus.Maintenance:
                    state = "维修状态";
                    break;
                default:
                    break;
            }
            uint[,] workTime = MachineCtrl.GetInstance().GetDryingOvenWorkTime();
            html += $"<li>腔体状态：{state}</li>";
            html += $"<li>工作时间：{workTime[(int)id - (int)RunID.DryOven0, cavityIdx]}分钟</li>";
            html += $"<li>抽检周期：{MachineCtrl.GetInstance().GetDryingOvenCavitySamplingCycle(id, cavityIdx)}</li>";
            html += $"<li>加热次数：{MachineCtrl.GetInstance().GetDryingOvenCavityHeartCycle(id, cavityIdx)}</li>";
            html += $"</ul></td></tr>";

            html += "</table>";
            return ShowToolTip(html);
        }

        bool ShowMesInfo(Point pt)
        {
            if (!MachineCtrl.GetInstance().McStopState(MachineCtrl.GetInstance().RunsCtrl.GetMCState()))
            {
                return false;
            }
            if(this.rectMesInfo.Contains(pt))
            {
                if(MachineCtrl.GetInstance().UpdataMes)
                {
                    if (!MachineCtrl.GetInstance().MesModifyCheck())
                    {
                        return true;
                    }
                    //if(MachineCtrl.GetInstance().dbRecord.UserLevel() > UserLevelType.USER_MAINTENANCE)
                    //{
                    //    HelperLibrary.ShowMsgBox.Show("当前用户无权限修改MES状态", HelperLibrary.MessageType.MsgWarning, 5, DialogResult.OK);
                    //    return false;
                    //}
                    //else
                    //{
                    //    if(DialogResult.Yes != HelperLibrary.ShowMsgBox.ShowDialog("是否确定修改MES状态为：离线生产？", HelperLibrary.MessageType.MsgQuestion, 5, DialogResult.No))
                    //    {
                    //        return false;
                    //    }
                    //}
                }
                MachineCtrl.GetInstance().UpdataMes = !MachineCtrl.GetInstance().UpdataMes;
                return true;
            }
            return false;
        }

        bool ShowOnload(Point pt)
        {
            RunID id = RunID.OnloadRobot;
            // 上料夹具
            for(int i = 0; i < this.rectOnloadRbtPlt.Length; i++)
            {
                if (this.rectOnloadRbtPlt[i].Contains(pt))
                {
                    return ShowPallet(ModuleName(id), "夹具" + (i + 1), ModulePallet(id)[i]);
                }
            }
            Battery[] arrBat = ModuleBattery(id);
            // 暂存
            if (this.rectOnloadRbtBuffer.Contains(pt))
            {
                return ShowBattery(ModuleName(id) + " - 暂存", (new Battery[] { arrBat[2], arrBat[3]}), 2, 1);
            }
            // 抓手
            if(this.rectOnloadRbtFinger.Contains(pt))
            {
                return ShowBattery(ModuleName(id) + " - 抓手", (new Battery[] { arrBat[0], arrBat[1] }), 2, 1);
            }

            Battery[] modBat = null;
            // 来料线
            id = RunID.OnloadLine;
            if(this.rectOnloadLine.Contains(pt))
            {
                modBat = ModuleBattery(id);
                return ShowBattery(ModuleName(id), modBat, 1, modBat.Length);
            }
            // 来料接收电池
            id = RunID.OnloadRecv;
            if(this.rectOnloadRecv.Contains(pt))
            {
                modBat = ModuleBattery(id);
                return ShowBattery(ModuleName(id), modBat, 1, modBat.Length);
            }
            // 来料扫码
            id = RunID.OnloadScan;
            if(this.rectOnloadScan.Contains(pt))
            {
                modBat = ModuleBattery(id);
                return ShowBattery(ModuleName(id), modBat, 1, modBat.Length);
            }
            // NG输出
            id = RunID.OnloadNG;
            if(this.rectOnloadNG.Contains(pt))
            {
                modBat = ModuleBattery(id);
                return ShowBattery(ModuleName(id), modBat, modBat.Length, 1);
            }
            // 上假电池
            id = RunID.OnloadFake;
            if(this.rectOnloadFake.Contains(pt))
            {
                return ShowBattery(ModuleName(id), ModuleBattery(id), 4, 4);
            }

            return false;
        }

        bool ShowManualOperate(Point pt)
        {
            RunID id = RunID.ManualOperate;
            // 人工操作台夹具
            if(this.rectManualOperate.Contains(pt))
            {
                return ShowPallet("人工操作台", "夹具状态", ModulePallet(id)[0]);
            }
            return false;
        }

        bool ShowOven(Point pt)
        {
            int ovenCount = (int)OvenInfoCount.OvenCount;
            for(int ovenIdx = 0; ovenIdx < ovenCount; ovenIdx++)
            {
                RunID id = RunID.DryOven0 + ovenIdx;
                // 干燥炉夹具
                for(int i = 0; i < this.rectDryOvenPlt.GetLength(1); i++)
                {
                    if(this.rectDryOvenPlt[ovenIdx, i].Contains(pt))
                    {
                        return ShowPallet(ModuleName(id), string.Format("{0}层夹具{1}", (i / 2 + 1), (i % 2 + 1)), ModulePallet(id)[i]);
                    }
                }
                // 干燥炉腔体
                for(int i = 0; i < this.rectDryOvenCavity.GetLength(1); i++)
                {
                    if(this.rectDryOvenCavity[ovenIdx, i].Contains(pt))
                    {
                        return ShowCavity(ModuleName(id), string.Format("{0}层腔体", (i + 1)), id, i);
                    }
                }
            }
            return false;
        }

        bool ShowPalletBuffer(Point pt)
        {
            RunID id = RunID.PalletBuffer;
            // 夹具缓存架夹具
            for(int i = 0; i < this.rectPltBufferPlt.Length; i++)
            {
                if(this.rectPltBufferPlt[i].Contains(pt))
                {
                    return ShowPallet(ModuleName(id), string.Format("{0}层夹具", (i + 1)), ModulePallet(id)[i]);
                }
            }
            return false;
        }
        
        bool ShowTransfer(Point pt)
        {
            RunID id = RunID.Transfer;
            // 调度机器人夹具
            if(this.rectTransfer.Contains(pt))
            {
                return ShowPallet(ModuleName(id), "插料架夹具", ModulePallet(id)[0]);
            }
            return false;
        }

        bool ShowOffLoad(Point pt)
        {
            RunID id = RunID.OffloadBattery;
            // 下料夹具
            for(int i = 0; i < this.rectOffloadBatPlt.Length; i++)
            {
                if(this.rectOffloadBatPlt[i].Contains(pt))
                {
                    return ShowPallet(ModuleName(id), "夹具" + (i + 1), ModulePallet(id)[i]);
                }
            }
            Battery[] arrBat = ModuleBattery(id);
            // 暂存
            if(this.rectOffloadBatBuffer.Contains(pt))
            {
                return ShowBattery(ModuleName(id) + " - 暂存", (new Battery[] { arrBat[2], arrBat[3] }), 2, 1);
            }
            // 抓手
            if(this.rectOffloadBatFinger.Contains(pt))
            {
                return ShowBattery(ModuleName(id) + " - 抓手", (new Battery[] { arrBat[0], arrBat[1] }), 2, 1);
            }

            // 下料线
            id = RunID.OffloadLine;
            if(this.rectOffloadLine.Contains(pt))
            {
                return ShowBattery(ModuleName(id), ModuleBattery(id), 2, 2);
            }
            Battery[] modBat = null;
            // 下待测假电池线
            id = RunID.OffloadDetect;
            if(this.rectOffloadDetect.Contains(pt))
            {
                modBat = ModuleBattery(id);
                return ShowBattery(ModuleName(id), modBat, modBat.Length, 1);
            }
            // 下料NG输出
            id = RunID.OffloadNG;
            if(this.rectOffloadNG.Contains(pt))
            {
                modBat = ModuleBattery(id);
                return ShowBattery(ModuleName(id), modBat, modBat.Length, 1);
            }
            // 冷却下料
            id = RunID.CoolingOffload;
            arrBat = ModuleBattery(id);
            // 暂存
            if(this.rectCoolingBuffer.Contains(pt))
            {
                return ShowBattery(ModuleName(id) + " - 暂存", (new Battery[] { arrBat[2], arrBat[3] }), 2, 1);
            }
            // 抓手
            if(this.rectCoolingFinger.Contains(pt))
            {
                return ShowBattery(ModuleName(id) + " - 抓手", (new Battery[] { arrBat[0], arrBat[1] }), 2, 1);
            }
            // 冷却系统
            id = RunID.CoolingSystem;
            if(this.rectCoolingSystem.Contains(pt))
            {
                BatteryLine batLine = ModuleBatteryLine(id);
                if (null != batLine)
                {
                    return ShowBatteryLine(ModuleName(id), "电池", batLine);
                }
            }
            return false;
        }

        #endregion

        #region // 计数表

        /// <summary>
        /// 创建计数表
        /// </summary>
        private void CreateTotalDataView()
        {
            // 设置表格
            DataGridViewNF dgv = this.dataGridViewTotalData;
            dgv.SetViewStatus();
            dgv.RowHeadersVisible = false;          // 行表头不可见
            dgv.ColumnHeadersVisible = false;       // 列表头不可见
            // 项
            int idx = dgv.Columns.Add("key", "项");
            dgv.Columns[idx].FillWeight = 65;     // 宽度占比权重
            idx = dgv.Columns.Add("value", "数");
            dgv.Columns[idx].FillWeight = 35;
            foreach(DataGridViewColumn item in dgv.Columns)
            {
                item.SortMode = DataGridViewColumnSortMode.NotSortable;     // 禁止列排序
            }
            // 添加行数据
            dgv.Rows[dgv.Rows.Add()].Cells[0].Value = "计数：";
            dgv.Rows[dgv.Rows.Add()].Cells[0].Value = "上料计数";
            dgv.Rows[dgv.Rows.Add()].Cells[0].Value = "上料扫码NG";
            dgv.Rows[dgv.Rows.Add()].Cells[0].Value = "下料计数";
            dgv.Rows[dgv.Rows.Add()].Cells[0].Value = "烘烤NG";

            idx = dgv.Rows.Add();
            dgv.Rows[idx].Cells[0].Value = "干燥出炉时间：";
            dgv.Rows[idx].Cells[1].Value = "分钟min";
            for(int id = 0; id < (int)OvenInfoCount.OvenCount; id++)
            {
                for(int row = 0; row < (int)OvenRowCol.MaxRow; row++)
                {
                    dgv.Rows[dgv.Rows.Add()].Cells[0].Value = "";
                }
            }

            for(int i = 0; i < dgv.RowCount; i++)
            {
                dgv.Rows[i].Height = 30;
            }

            // 添加用户管理右键菜单
            ContextMenuStrip cms = new ContextMenuStrip();
            cms.Items.Add("清除全部计数");
            cms.Items[0].Click += OverViewPage_Click_ClearTotalData;
            this.dataGridViewTotalData.ContextMenuStrip = cms;
        }

        private void UpdataTotalData()
        {
            try
            {
                // 使用委托更新UI
                this.Invoke(new Action(() => 
                {
                    if (!this.updating)
                    {
                        this.updating = true;
                        int idx = 1;
                        DataGridViewNF dgv = this.dataGridViewTotalData;
                        dgv.Rows[idx++].Cells[1].Value = TotalData.OnloadCount;
                        dgv.Rows[idx++].Cells[1].Value = TotalData.OnScanNGCount;
                        dgv.Rows[idx++].Cells[1].Value = TotalData.OffloadCount;
                        dgv.Rows[idx++].Cells[1].Value = TotalData.BakedNGCount;
                        idx++;
                        uint[,] workTime = MachineCtrl.GetInstance().GetDryingOvenWorkTime();
                        Dictionary<string, uint> workInfo = new Dictionary<string, uint>();
                        for(int id = 0; id < (int)OvenInfoCount.OvenCount; id++)
                        {
                            for(int row = 0; row < (int)OvenRowCol.MaxRow; row++)
                            {
                                workInfo.Add(string.Format("{0}炉 {1}层", id + 1, row + 1), workTime[id, row]);
                                //dgv.Rows[idx].Cells[0].Value = string.Format("{0}炉 {1}层", id + 1, row + 1);
                                //dgv.Rows[idx++].Cells[1].Value = workTime[id, row];
                            }
                        }
                        var result = workInfo.OrderByDescending(p => p.Value).ToDictionary(p => p.Key, o => o.Value);
                        foreach(var item in result)
                        {
                            dgv.Rows[idx].Cells[0].Value = item.Key;
                            dgv.Rows[idx++].Cells[1].Value = item.Value;
                        }
                        this.updating = false;
                    }
                }));
            }
            catch(System.Exception ex)
            {
                Def.WriteLog("OverViewPage.UpdataTotalData() ", $"{ex.Message}\r\n{ex.StackTrace}", HelperLibrary.LogType.Error);
            }
        }

        private void dataGridViewTotalData_MouseDown(object sender, MouseEventArgs e)
        {
            if(MouseButtons.Right == e.Button)
            {
                MCState mcState = MachineCtrl.GetInstance().RunsCtrl.GetMCState();
                bool enable = ((MCState.MCInitializing != mcState) && (MCState.MCRunning != mcState));
                // 添加判断启用哪些右键菜单
                DataGridViewRow dgvRow = this.dataGridViewTotalData.CurrentRow;
                this.dataGridViewTotalData.ContextMenuStrip.Items[0].Enabled = (enable && (null != dgvRow)); // 清除计数
            }
        }

        private void OverViewPage_Click_ClearTotalData(object sender, EventArgs e)
        {
            TotalData.ClearTotalData();
            TotalData.WriteTotalData();
        }

        #endregion

    }
}
