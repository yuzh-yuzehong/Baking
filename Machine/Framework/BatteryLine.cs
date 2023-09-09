namespace Machine
{
    /// <summary>
    /// 电池线默认最大行列
    /// </summary>
    public enum BatteryLineRowCol
    {
        MaxRow = 20,
        MaxCol = 10,
    }

    /// <summary>
    /// 电池线：一般用于冷却系统
    ///     禁用=操作拷贝数据，请使用.Copy()方法拷贝数据
    /// </summary>
    [System.Serializable]
    public class BatteryLine
    {
        #region // 字段属性

        public int MaxRow { get; set; }             // 电池线最大行
        public int MaxCol { get; set; }             // 电池线最大行
        public Battery[,] Battery { get; set; }     // 电池线电池

        private object datalock;

        #endregion


        #region // 方法

        public BatteryLine()
        {
            this.datalock = new object();
            this.MaxRow = (int)BatteryLineRowCol.MaxRow;
            this.MaxCol = (int)BatteryLineRowCol.MaxCol;
            this.Battery = new Battery[this.MaxRow, this.MaxCol];
            for(int row = 0; row < this.MaxRow; row++)
            {
                for(int col = 0; col < this.MaxCol; col++)
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
            lock(this.datalock)
            {
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
        /// 为空
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            lock(this.datalock)
            {
                for(int row = 0; row < this.MaxRow; row++)
                {
                    for(int col = 0; col < this.MaxCol; col++)
                    {
                        if(BatteryStatus.Invalid != this.Battery[row, col].Type)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 为满
        /// </summary>
        /// <returns></returns>
        public bool IsFull()
        {
            lock(this.datalock)
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
        /// 拷贝外部数据到本对象
        /// </summary>
        /// <param name="srcLine">源数据</param>
        public void Copy(BatteryLine srcLine)
        {
            lock(this.datalock)
            {
                this.MaxRow = srcLine.MaxRow;
                this.MaxCol = srcLine.MaxCol;
                for(int row = 0; row < this.MaxRow; row++)
                {
                    for(int col = 0; col < this.MaxCol; col++)
                    {
                        this.Battery[row, col].Copy(srcLine.Battery[row, col]);
                    }
                }
            }
        }

        /// <summary>
        /// 设置电池线最大行列
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        public void SetRowCol(int row, int col)
        {
            lock(this.datalock)
            {
                this.MaxRow = row;
                this.MaxCol = col;
            }
        }

        #endregion
    }
}
