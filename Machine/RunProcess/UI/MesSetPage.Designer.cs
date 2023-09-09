namespace Machine
{
    partial class MesSetPage
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
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabPageWaterContent = new System.Windows.Forms.TabPage();
            this.tabPageMesInfo = new System.Windows.Forms.TabPage();
            this.tabControl.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabPageWaterContent);
            this.tabControl.Controls.Add(this.tabPageMesInfo);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.ItemSize = new System.Drawing.Size(150, 35);
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Margin = new System.Windows.Forms.Padding(4);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(429, 261);
            this.tabControl.TabIndex = 0;
            this.tabControl.SelectedIndexChanged += new System.EventHandler(this.tabControl_SelectedIndexChanged);
            // 
            // tabPageWaterContent
            // 
            this.tabPageWaterContent.Location = new System.Drawing.Point(4, 39);
            this.tabPageWaterContent.Margin = new System.Windows.Forms.Padding(4);
            this.tabPageWaterContent.Name = "tabPageWaterContent";
            this.tabPageWaterContent.Padding = new System.Windows.Forms.Padding(4);
            this.tabPageWaterContent.Size = new System.Drawing.Size(421, 218);
            this.tabPageWaterContent.TabIndex = 0;
            this.tabPageWaterContent.Text = "水含量上传";
            this.tabPageWaterContent.UseVisualStyleBackColor = true;
            // 
            // tabPageMesInfo
            // 
            this.tabPageMesInfo.Location = new System.Drawing.Point(4, 39);
            this.tabPageMesInfo.Name = "tabPageMesInfo";
            this.tabPageMesInfo.Size = new System.Drawing.Size(421, 218);
            this.tabPageMesInfo.TabIndex = 1;
            this.tabPageMesInfo.Text = "资源信息";
            this.tabPageMesInfo.UseVisualStyleBackColor = true;
            // 
            // MesSetPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(429, 261);
            this.Controls.Add(this.tabControl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "MesSetPage";
            this.Text = "MesSetPage";
            this.VisibleChanged += new System.EventHandler(this.MesSetPage_VisibleChanged);
            this.tabControl.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabPageWaterContent;
        private System.Windows.Forms.TabPage tabPageMesInfo;
    }
}