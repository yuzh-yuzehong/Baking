namespace Machine
{
    partial class ModuleMonitorPage
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
            this.dataGridViewModule = new Machine.DataGridViewNF();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewModule)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridViewModule
            // 
            this.dataGridViewModule.BackgroundColor = System.Drawing.SystemColors.Control;
            this.dataGridViewModule.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewModule.Location = new System.Drawing.Point(12, 34);
            this.dataGridViewModule.Name = "dataGridViewModule";
            this.dataGridViewModule.RowTemplate.Height = 27;
            this.dataGridViewModule.Size = new System.Drawing.Size(240, 150);
            this.dataGridViewModule.TabIndex = 0;
            // 
            // ModuleMonitorPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(283, 252);
            this.Controls.Add(this.dataGridViewModule);
            this.Font = new System.Drawing.Font("宋体", 11F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "ModuleMonitorPage";
            this.Text = "ModuleMonitorPage";
            this.Load += new System.EventHandler(this.ModuleMonitorPage_Load);
            this.VisibleChanged += new System.EventHandler(this.ModuleMonitorPage_VisibleChanged);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewModule)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DataGridViewNF dataGridViewModule;
    }
}