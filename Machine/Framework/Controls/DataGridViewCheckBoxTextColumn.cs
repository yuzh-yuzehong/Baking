using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace Machine
{
    public class DataGridViewCheckBoxTextColumn : DataGridViewColumn
    {
        public DataGridViewCheckBoxTextColumn() : base()
        {
            CellTemplate = new DataGridViewCheckBoxTextCell();
        }

        public override DataGridViewCell CellTemplate
        {
            get
            {
                return base.CellTemplate;
            }
            set
            {
                if(value != null && !value.GetType().IsAssignableFrom(typeof(DataGridViewCheckBoxTextCell)))
                {
                    throw new Exception("这个列里面必须绑定DataGridViewCheckBoxTextCell");
                }
                base.CellTemplate = value;
            }
        }

        public override object Clone()
        {
            DataGridViewCheckBoxTextColumn col = (DataGridViewCheckBoxTextColumn)base.Clone();
            col.Text = Text;
            return col;
        }

        public string Text { set; get; }

        public bool Checked
        {
            get
            {
                return Convert.ToBoolean(CellTemplate.Value);
            }
            set
            {
                CellTemplate.Value = value;
            }
        }

        public Color ForeColor
        {
            get
            {
                return CellTemplate.Style.ForeColor;
            }
            set
            {
                CellTemplate.Style.ForeColor = value;
            }
        }
    }


    public class DataGridViewCheckBoxTextCell : DataGridViewCell
    {
        public DataGridViewCheckBoxTextCell() : base() { }

        #region // 字段属性

        private static Type defaultEditType = typeof(DataGridViewCheckBoxTextControl);
        private static Type defaultValueType = typeof(System.Boolean);

        public override Type EditType
        {
            get { return defaultEditType; }
        }

        public string Text { set; get; }

        public bool Checked
        {
            get
            {
                return Convert.ToBoolean(Value);
            }
            set
            {
                if ((null == Value) || !ReadOnly)
                {
                    Value = value;
                    SetValue(RowIndex, Value);
                }
            }
        }
        
        public Color ForeColor
        {
            get
            {
                return Style.ForeColor;
            }
            set
            {
                Style.ForeColor = value;
            }
        }

        /// <summary>
        /// 单元格边框颜色
        /// </summary>
        private Color CellBorderColor { get { return Color.FromArgb(172, 168, 153); } }

        /// <summary>
        /// <summary>
        /// 当前复选框的状态
        /// </summary>
        private CheckBoxState CheckState { set; get; }

        public override Type ValueType
        {
            get
            {
                Type valueType = base.ValueType;
                if(valueType != null)
                {
                    return valueType;
                }
                return defaultValueType;
            }
        }

        public override object DefaultNewRowValue
        {
            get
            {
                return true;
            }
        }
        
        #endregion

        protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex, DataGridViewElementStates cellState, object value, object formattedValue, string errorText, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
        {
            value = false;
            var check = (Boolean)value;
            if(paintParts == DataGridViewPaintParts.Background || paintParts == DataGridViewPaintParts.All)
            {
                graphics.FillRectangle(new SolidBrush(cellStyle.BackColor), cellBounds);
            }
            if(paintParts == DataGridViewPaintParts.Border || paintParts == DataGridViewPaintParts.All)
            {
                graphics.DrawRectangle(new Pen(CellBorderColor), cellBounds);
            }
            if(paintParts == DataGridViewPaintParts.SelectionBackground || Selected)
            {
                graphics.FillRectangle(new SolidBrush(cellStyle.SelectionBackColor), cellBounds);
            }
            var col = OwningColumn as DataGridViewCheckBoxTextColumn;
            if(col != null && !string.IsNullOrEmpty(Text))
            {
                graphics.DrawString(Text
                    , cellStyle.Font
                    , new SolidBrush(Selected ? cellStyle.SelectionForeColor : cellStyle.ForeColor)
                    , new Point(cellBounds.X + 25, cellBounds.Y + cellBounds.Height / 4));
            }
            if(Checked)
            {
                CheckState = ReadOnly ? CheckBoxState.CheckedDisabled : CheckBoxState.CheckedNormal;
            }
            else
            {
                CheckState = ReadOnly ? CheckBoxState.UncheckedDisabled : CheckBoxState.UncheckedNormal;
            }
            CheckBoxRenderer.DrawCheckBox(graphics, new Point(cellBounds.X + 4, cellBounds.Y + cellBounds.Height / 4), CheckState);
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);
        }
    }


    public class DataGridViewCheckBoxTextControl : CheckBox, IDataGridViewEditingControl
    {
        /// <summary>
        /// 当前所在表格
        /// </summary>
        private DataGridView dataGridViewControl { set; get; }
        /// <summary>
        /// 值是否发生更改
        /// </summary>
        private bool ValueChanged { set; get; }
        /// <summary>
        /// 当前所在行
        /// </summary>
        private int RowIndex { set; get; }

        protected override void OnCheckedChanged(EventArgs e)
        {
            ValueChanged = true;
            this.EditingControlDataGridView.NotifyCurrentCellDirty(true);
            base.OnCheckedChanged(e);
        }

        public void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle)
        {
            Font = dataGridViewCellStyle.Font;
            ForeColor = dataGridViewCellStyle.ForeColor;
            BackColor = dataGridViewCellStyle.BackColor;
        }

        public DataGridView EditingControlDataGridView
        {
            get
            {
                return dataGridViewControl;
            }
            set
            {
                dataGridViewControl = value;
            }
        }

        public object EditingControlFormattedValue
        {
            get
            {
                return GetEditingControlFormattedValue(DataGridViewDataErrorContexts.Formatting);
            }
            set
            {
                Checked = value == null ? false : (bool)value;
            }
        }

        public int EditingControlRowIndex
        {
            get
            {
                return RowIndex;
            }
            set
            {
                RowIndex = value;
            }
        }

        public bool EditingControlValueChanged
        {
            get
            {
                return ValueChanged;
            }
            set
            {
                ValueChanged = value;
            }
        }

        public bool EditingControlWantsInputKey(Keys keyData, bool dataGridViewWantsInputKey)
        {
            switch(keyData & Keys.KeyCode)
            {
                case Keys.LButton:
                    return !dataGridViewWantsInputKey;
            }
            return !dataGridViewWantsInputKey;
        }

        public Cursor EditingPanelCursor
        {
            get { return Cursors.Default; }
        }

        public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context)
        {
            return this.Checked;
        }

        public void PrepareEditingControlForEdit(bool selectAll)
        {

        }

        public bool RepositionEditingControlOnValueChange
        {
            get { return false; }
        }
    }
}
