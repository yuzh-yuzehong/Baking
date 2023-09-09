namespace Machine
{
    partial class ParameterPage
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
            this.propertyGridParameter = new System.Windows.Forms.PropertyGrid();
            this.splitContainerModuleParameter = new System.Windows.Forms.SplitContainer();
            this.dataGridViewModule = new Machine.DataGridViewNF();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerModuleParameter)).BeginInit();
            this.splitContainerModuleParameter.Panel1.SuspendLayout();
            this.splitContainerModuleParameter.Panel2.SuspendLayout();
            this.splitContainerModuleParameter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewModule)).BeginInit();
            this.SuspendLayout();
            // 
            // propertyGridParameter
            // 
            this.propertyGridParameter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGridParameter.Location = new System.Drawing.Point(0, 0);
            this.propertyGridParameter.Name = "propertyGridParameter";
            this.propertyGridParameter.Size = new System.Drawing.Size(210, 250);
            this.propertyGridParameter.TabIndex = 0;
            this.propertyGridParameter.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.propertyGridParameter_PropertyValueChanged);
            // 
            // splitContainerModuleParameter
            // 
            this.splitContainerModuleParameter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerModuleParameter.Font = new System.Drawing.Font("宋体", 10F);
            this.splitContainerModuleParameter.Location = new System.Drawing.Point(0, 0);
            this.splitContainerModuleParameter.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.splitContainerModuleParameter.Name = "splitContainerModuleParameter";
            // 
            // splitContainerModuleParameter.Panel1
            // 
            this.splitContainerModuleParameter.Panel1.Controls.Add(this.dataGridViewModule);
            // 
            // splitContainerModuleParameter.Panel2
            // 
            this.splitContainerModuleParameter.Panel2.Controls.Add(this.propertyGridParameter);
            this.splitContainerModuleParameter.Size = new System.Drawing.Size(413, 250);
            this.splitContainerModuleParameter.SplitterDistance = 200;
            this.splitContainerModuleParameter.SplitterWidth = 3;
            this.splitContainerModuleParameter.TabIndex = 2;
            // 
            // dataGridViewModule
            // 
            this.dataGridViewModule.BackgroundColor = System.Drawing.SystemColors.Control;
            this.dataGridViewModule.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewModule.Location = new System.Drawing.Point(0, 0);
            this.dataGridViewModule.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.dataGridViewModule.Name = "dataGridViewModule";
            this.dataGridViewModule.RowTemplate.Height = 27;
            this.dataGridViewModule.Size = new System.Drawing.Size(200, 250);
            this.dataGridViewModule.TabIndex = 0;
            this.dataGridViewModule.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewModule_CellClick);
            this.dataGridViewModule.SelectionChanged += new System.EventHandler(this.dataGridViewModule_SelectionChanged);
            // 
            // ParameterPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(413, 250);
            this.Controls.Add(this.splitContainerModuleParameter);
            this.Font = new System.Drawing.Font("宋体", 10F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "ParameterPage";
            this.Text = "ParameterPage";
            this.Load += new System.EventHandler(this.ParameterPage_Load);
            this.VisibleChanged += new System.EventHandler(this.ParameterPage_VisibleChanged);
            this.splitContainerModuleParameter.Panel1.ResumeLayout(false);
            this.splitContainerModuleParameter.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerModuleParameter)).EndInit();
            this.splitContainerModuleParameter.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewModule)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PropertyGrid propertyGridParameter;
        private System.Windows.Forms.SplitContainer splitContainerModuleParameter;
        private DataGridViewNF dataGridViewModule;
    }
}