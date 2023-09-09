namespace Machine
{
    partial class OverViewPage
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelView = new System.Windows.Forms.Label();
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.dataGridViewTotalData = new Machine.DataGridViewNF();
            this.tableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewTotalData)).BeginInit();
            this.SuspendLayout();
            // 
            // labelView
            // 
            this.labelView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelView.Location = new System.Drawing.Point(41, 0);
            this.labelView.Name = "labelView";
            this.labelView.Size = new System.Drawing.Size(211, 255);
            this.labelView.TabIndex = 0;
            this.labelView.Text = "动画绘图区";
            this.labelView.Paint += new System.Windows.Forms.PaintEventHandler(this.labelView_Paint);
            this.labelView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.labelView_MouseDown);
            this.labelView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.labelView_MouseMove);
            // 
            // tableLayoutPanel
            // 
            this.tableLayoutPanel.ColumnCount = 2;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 85F));
            this.tableLayoutPanel.Controls.Add(this.labelView, 1, 0);
            this.tableLayoutPanel.Controls.Add(this.dataGridViewTotalData, 0, 0);
            this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 1;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.Size = new System.Drawing.Size(255, 255);
            this.tableLayoutPanel.TabIndex = 1;
            // 
            // dataGridViewTotalData
            // 
            this.dataGridViewTotalData.BackgroundColor = System.Drawing.SystemColors.Control;
            this.dataGridViewTotalData.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewTotalData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewTotalData.Location = new System.Drawing.Point(3, 3);
            this.dataGridViewTotalData.Name = "dataGridViewTotalData";
            this.dataGridViewTotalData.RowTemplate.Height = 27;
            this.dataGridViewTotalData.Size = new System.Drawing.Size(32, 249);
            this.dataGridViewTotalData.TabIndex = 1;
            this.dataGridViewTotalData.MouseDown += new System.Windows.Forms.MouseEventHandler(this.dataGridViewTotalData_MouseDown);
            // 
            // OverViewPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.ClientSize = new System.Drawing.Size(255, 255);
            this.Controls.Add(this.tableLayoutPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "OverViewPage";
            this.Text = "OverViewPage";
            this.Load += new System.EventHandler(this.OverViewPage_Load);
            this.VisibleChanged += new System.EventHandler(this.OverViewPage_VisibleChanged);
            this.tableLayoutPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewTotalData)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label labelView;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private DataGridViewNF dataGridViewTotalData;
    }
}