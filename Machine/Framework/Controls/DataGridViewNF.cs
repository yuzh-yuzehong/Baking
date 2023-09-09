using System.Drawing;
using System.Windows.Forms;

namespace Machine
{
    /// <summary>
    /// DataGridViewNF（NF=Never/No Flickering）
    /// </summary>
    class DataGridViewNF : DataGridView
    {
        public DataGridViewNF()
        {
            this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();
        }

        /// <summary>
        /// 设置标准视图状态
        /// </summary>
        public void SetViewStatus()
        {
            this.ReadOnly = true;        // 只读不可编辑
            this.MultiSelect = false;    // 禁止多选，只可单选
            this.AutoGenerateColumns = false;        // 禁止创建列
            this.AllowUserToAddRows = false;         // 禁止添加行
            this.AllowUserToDeleteRows = false;      // 禁止删除行
            this.AllowUserToResizeRows = false;      // 禁止行改变大小
            this.RowHeadersVisible = false;          // 行表头不可见
            this.ColumnHeadersVisible = true;        // 列表头可见
            this.Dock = DockStyle.Fill;              // 填充
            this.EditMode = DataGridViewEditMode.EditProgrammatically;           // 软件编辑模式
            this.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;     // 自动改变列宽
            this.SelectionMode = DataGridViewSelectionMode.FullRowSelect;        // 整行选中
            this.RowsDefaultCellStyle.BackColor = Color.WhiteSmoke;              // 偶数行颜色
            this.AlternatingRowsDefaultCellStyle.BackColor = Color.GhostWhite;   // 奇数行颜色
            // 表头
            this.ColumnHeadersDefaultCellStyle.Font = new Font(this.ColumnHeadersDefaultCellStyle.Font.FontFamily, 11, FontStyle.Bold);
            this.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            this.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            this.ColumnHeadersHeight = 35;
        }
    }
}
