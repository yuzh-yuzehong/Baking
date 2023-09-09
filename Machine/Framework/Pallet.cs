using System;


namespace Machine
{
    #region // 夹具属性枚举
    
    /// <summary>
    /// 夹具状态属性
    /// </summary>
    public enum PalletStatus
    {
        // 夹具状态：仅能有一种状态
        Invalid = 0,     // 无效状态
        OK,              // 有效OK状态
        NG,              // 有效NG状态
        Detect,          // 待检测状态
        WaitResult,      // 等待结果（已取走假电池）
        WaitOffload,     // 等待下料
        ReputFake,       // 假电池回炉（水含量超标，待放回假电池）
        Rebaking,        // 等待二次干燥（水含量超标，已放回假电池）
    };

    /// <summary>
    /// 夹具阶段
    /// </summary>
    public enum PalletStage
    {
        Invalid = 0x00,       // 无效阶段
        Onload = 0x01 << 0,   // 上料阶段完成
        Baked = 0x01 << 1,    // 烘烤阶段完成
        Offload = 0x01 << 2,  // 下料阶段完成
    }

    /// <summary>
    /// 夹具默认行列
    /// </summary>
    public enum PalletRowCol
    {
        MaxRow = 14, //14
        MaxCol = 5,
    }

    #endregion

    /// <summary>
    /// 夹具类：
    ///     禁用=操作拷贝数据，请使用.Copy()方法拷贝数据
    /// </summary>
    [System.Serializable]
    public class Pallet
    {
        #region // 字段

        public string Code;                         // 夹具条码
        public PalletStatus State;                  // 夹具状态
        public PalletStage Stage;                   // 夹具阶段
        public int MaxRow { get; private set; }     // 夹具最大行
        public int MaxCol { get; private set; }     // 夹具最大列
        public Battery[,] Battery;                  // 夹具中电池数据[row, col]
        public bool NeedFake;                       // 夹具需要假电池
        public int BakingCount;                     // Baking次数
        public int SrcStation;                      // 来源工位
        public int SrcRow;                          // 来源工位行
        public int SrcCol;                          // 来源工位列
        public DateTime StartDate;                  // 加热开始时间
        public DateTime EndDate;                    // 加热结束时间
        public DateTime FeedingTime;                // 投料时间
        public DateTime UploadingTime;              // 卸料时间

        private object palletLock;                  // 夹具数据安全锁
        #endregion
        
        #region // 方法

        public Pallet()
        {
            palletLock = new object();
            this.MaxRow = (int)PalletRowCol.MaxRow;
            this.MaxCol = (int)PalletRowCol.MaxCol;
            this.Battery = new Battery[(int)PalletRowCol.MaxRow, (int)PalletRowCol.MaxCol];
            for(int row = 0; row < this.Battery.GetLength(0); row++)
            {
                for(int col = 0; col < this.Battery.GetLength(1); col++)
                {
                    this.Battery[row, col] = new Battery();
                }
            }
            Release();
        }

        /// <summary>
        /// 清空数据
        /// </summary>
        public void Release()
        {
            lock(palletLock)
            {
                this.Code = "";
                this.State = PalletStatus.Invalid;
                this.Stage = PalletStage.Invalid;
                this.NeedFake = false;
                this.BakingCount = 0;
                this.SrcStation = -1;
                this.SrcRow = -1;
                this.SrcCol = -1;
                this.StartDate = DateTime.MinValue;
                this.EndDate = DateTime.MinValue;
                for(int row = 0; row < this.MaxRow; row++)
                {
                    for(int col = 0; col < this.MaxCol; col++)
                    {
                        this.Battery[row, col].Release();
                    }
                }
            }
        }

        /// <summary>
        /// 拷贝外部数据到本对象
        /// </summary>
        /// <param name="srcPallet">源数据</param>
        public void Copy(Pallet srcPallet)
        {
            lock(palletLock)
            {
                this.Code = srcPallet.Code;
                this.State = srcPallet.State;
                this.Stage = srcPallet.Stage;
                this.MaxRow = srcPallet.MaxRow;
                this.MaxCol = srcPallet.MaxCol;
                this.NeedFake = srcPallet.NeedFake;
                this.BakingCount = srcPallet.BakingCount;
                this.SrcStation = srcPallet.SrcStation;
                this.SrcRow = srcPallet.SrcRow;
                this.SrcCol = srcPallet.SrcCol;
                this.StartDate = srcPallet.StartDate;
                this.EndDate = srcPallet.EndDate;
                for(int row = 0; row < this.MaxRow; row++)
                {
                    for(int col = 0; col < this.MaxCol; col++)
                    {
                        this.Battery[row, col].Copy(srcPallet.Battery[row, col]);
                    }
                }
            }
        }

        /// <summary>
        /// 设置夹具的最大行列
        /// </summary>
        /// <param name="row">最大行</param>
        /// <param name="col">最大列</param>
        public void SetRowCol(int row, int col)
        {
            lock(this.palletLock)
            {
                this.MaxRow = row;
                this.MaxCol = col;
            }
        }

        /// <summary>
        /// 夹具满
        /// </summary>
        /// <returns></returns>
        public bool IsFull()
        {
            lock(palletLock)
            {
                for(int row = 0; row < this.MaxRow; row++)
                {
                    for(int col = 0; col < this.MaxCol; col++)
                    {
                        if(BatteryStatus.Invalid == this.Battery[row, col].Type)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 夹具空
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            lock(palletLock)
            {
                for(int row = 0; row < this.MaxRow; row++)
                {
                    for(int col = 0; col < this.MaxCol; col++)
                    {
                        if(this.Battery[row, col].Type > BatteryStatus.Invalid)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 是否有假电池
        /// </summary>
        /// <returns></returns>
        public bool HasFake()
        {
            lock(palletLock)
            {
                for(int row = 0; row < this.MaxRow; row++)
                {
                    for(int col = 0; col < this.MaxCol; col++)
                    {
                        if((BatteryStatus.Fake == this.Battery[row, col].Type) 
                            || (BatteryStatus.FakeTag == this.Battery[row, col].Type))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 获取假电池位置
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public bool GetFakePos(ref int row, ref int col)
        {
            lock(palletLock)
            {
                for(int rowIdx = 0; rowIdx < this.MaxRow; rowIdx++)
                {
                    for(int colIdx = 0; colIdx < this.MaxCol; colIdx++)
                    {
                        if((BatteryStatus.Fake == this.Battery[rowIdx, colIdx].Type) 
                            || (BatteryStatus.FakeTag == this.Battery[rowIdx, colIdx].Type))
                        {
                            row = rowIdx;
                            col = colIdx;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        #endregion
    }
}
