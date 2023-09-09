namespace Machine
{
    partial class DebugToolsPage
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
            this.tabPageRobot = new System.Windows.Forms.TabPage();
            this.tabPageDryingOven = new System.Windows.Forms.TabPage();
            this.tabPageOther = new System.Windows.Forms.TabPage();
            this.tabControl.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabPageRobot);
            this.tabControl.Controls.Add(this.tabPageDryingOven);
            this.tabControl.Controls.Add(this.tabPageOther);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Font = new System.Drawing.Font("宋体", 11F);
            this.tabControl.ItemSize = new System.Drawing.Size(150, 35);
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Margin = new System.Windows.Forms.Padding(1);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(524, 376);
            this.tabControl.TabIndex = 0;
            this.tabControl.SelectedIndexChanged += new System.EventHandler(this.tabControl_SelectedIndexChanged);
            // 
            // tabPageRobot
            // 
            this.tabPageRobot.Location = new System.Drawing.Point(4, 39);
            this.tabPageRobot.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPageRobot.Name = "tabPageRobot";
            this.tabPageRobot.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPageRobot.Size = new System.Drawing.Size(516, 333);
            this.tabPageRobot.TabIndex = 2;
            this.tabPageRobot.Text = "机器人调试";
            this.tabPageRobot.UseVisualStyleBackColor = true;
            // 
            // tabPageDryingOven
            // 
            this.tabPageDryingOven.Location = new System.Drawing.Point(4, 39);
            this.tabPageDryingOven.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPageDryingOven.Name = "tabPageDryingOven";
            this.tabPageDryingOven.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPageDryingOven.Size = new System.Drawing.Size(516, 333);
            this.tabPageDryingOven.TabIndex = 3;
            this.tabPageDryingOven.Text = "干燥炉调试";
            this.tabPageDryingOven.UseVisualStyleBackColor = true;
            // 
            // tabPageOther
            // 
            this.tabPageOther.Location = new System.Drawing.Point(4, 39);
            this.tabPageOther.Name = "tabPageOther";
            this.tabPageOther.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageOther.Size = new System.Drawing.Size(516, 333);
            this.tabPageOther.TabIndex = 4;
            this.tabPageOther.Text = "其它调试";
            this.tabPageOther.UseVisualStyleBackColor = true;
            // 
            // DebugToolsPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(524, 376);
            this.Controls.Add(this.tabControl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "DebugToolsPage";
            this.Text = "DebugToolsPage";
            this.Load += new System.EventHandler(this.DebugToolsPage_Load);
            this.VisibleChanged += new System.EventHandler(this.DebugToolsPage_VisibleChanged);
            this.tabControl.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabPageRobot;
        private System.Windows.Forms.TabPage tabPageDryingOven;
        private System.Windows.Forms.TabPage tabPageOther;
    }
}