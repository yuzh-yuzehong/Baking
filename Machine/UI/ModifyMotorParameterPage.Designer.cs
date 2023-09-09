namespace Machine
{
    partial class ModifyMotorParameterPage
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
            if(disposing && (components != null))
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
            this.tablePanelParaPage = new System.Windows.Forms.TableLayoutPanel();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.tablePanelReset = new System.Windows.Forms.TableLayoutPanel();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.tablePanelMedium = new System.Windows.Forms.TableLayoutPanel();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.tablePanelFast = new System.Windows.Forms.TableLayoutPanel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tablePanelDefault = new System.Windows.Forms.TableLayoutPanel();
            this.buttonSave = new System.Windows.Forms.Button();
            this.tablePanelParaPage.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tablePanelParaPage
            // 
            this.tablePanelParaPage.ColumnCount = 2;
            this.tablePanelParaPage.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelParaPage.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelParaPage.Controls.Add(this.buttonCancel, 1, 2);
            this.tablePanelParaPage.Controls.Add(this.groupBox4, 1, 1);
            this.tablePanelParaPage.Controls.Add(this.groupBox3, 0, 1);
            this.tablePanelParaPage.Controls.Add(this.groupBox2, 1, 0);
            this.tablePanelParaPage.Controls.Add(this.groupBox1, 0, 0);
            this.tablePanelParaPage.Controls.Add(this.buttonSave, 0, 2);
            this.tablePanelParaPage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tablePanelParaPage.Location = new System.Drawing.Point(0, 0);
            this.tablePanelParaPage.Name = "tablePanelParaPage";
            this.tablePanelParaPage.RowCount = 3;
            this.tablePanelParaPage.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.tablePanelParaPage.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.tablePanelParaPage.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tablePanelParaPage.Size = new System.Drawing.Size(752, 533);
            this.tablePanelParaPage.TabIndex = 0;
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.buttonCancel.Location = new System.Drawing.Point(504, 483);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(120, 45);
            this.buttonCancel.TabIndex = 5;
            this.buttonCancel.Text = "取消";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.tablePanelReset);
            this.groupBox4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox4.Location = new System.Drawing.Point(379, 242);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(370, 233);
            this.groupBox4.TabIndex = 3;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "搜索原点速度：";
            // 
            // tablePanelReset
            // 
            this.tablePanelReset.ColumnCount = 2;
            this.tablePanelReset.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelReset.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelReset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tablePanelReset.Location = new System.Drawing.Point(3, 21);
            this.tablePanelReset.Name = "tablePanelReset";
            this.tablePanelReset.RowCount = 2;
            this.tablePanelReset.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelReset.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelReset.Size = new System.Drawing.Size(364, 209);
            this.tablePanelReset.TabIndex = 1;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.tablePanelMedium);
            this.groupBox3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox3.Location = new System.Drawing.Point(3, 242);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(370, 233);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "中等速度：";
            // 
            // tablePanelMedium
            // 
            this.tablePanelMedium.ColumnCount = 2;
            this.tablePanelMedium.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelMedium.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelMedium.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tablePanelMedium.Location = new System.Drawing.Point(3, 21);
            this.tablePanelMedium.Name = "tablePanelMedium";
            this.tablePanelMedium.RowCount = 2;
            this.tablePanelMedium.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelMedium.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelMedium.Size = new System.Drawing.Size(364, 209);
            this.tablePanelMedium.TabIndex = 1;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.tablePanelFast);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(379, 3);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(370, 233);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "快速速度：";
            // 
            // tablePanelFast
            // 
            this.tablePanelFast.ColumnCount = 2;
            this.tablePanelFast.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelFast.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelFast.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tablePanelFast.Location = new System.Drawing.Point(3, 21);
            this.tablePanelFast.Name = "tablePanelFast";
            this.tablePanelFast.RowCount = 2;
            this.tablePanelFast.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelFast.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelFast.Size = new System.Drawing.Size(364, 209);
            this.tablePanelFast.TabIndex = 1;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tablePanelDefault);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(370, 233);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "默认速度：";
            // 
            // tablePanelDefault
            // 
            this.tablePanelDefault.ColumnCount = 2;
            this.tablePanelDefault.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelDefault.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelDefault.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tablePanelDefault.Location = new System.Drawing.Point(3, 21);
            this.tablePanelDefault.Name = "tablePanelDefault";
            this.tablePanelDefault.RowCount = 2;
            this.tablePanelDefault.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelDefault.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelDefault.Size = new System.Drawing.Size(364, 209);
            this.tablePanelDefault.TabIndex = 0;
            // 
            // buttonSave
            // 
            this.buttonSave.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.buttonSave.Location = new System.Drawing.Point(128, 483);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(120, 45);
            this.buttonSave.TabIndex = 4;
            this.buttonSave.Text = "保存";
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            // 
            // ModifyMotorParameterPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(752, 533);
            this.Controls.Add(this.tablePanelParaPage);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ModifyMotorParameterPage";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "ModifyMotorParameterPage";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.ModifyMotorParameterPage_Load);
            this.tablePanelParaPage.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tablePanelParaPage;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.TableLayoutPanel tablePanelReset;
        private System.Windows.Forms.TableLayoutPanel tablePanelMedium;
        private System.Windows.Forms.TableLayoutPanel tablePanelFast;
        private System.Windows.Forms.TableLayoutPanel tablePanelDefault;
    }
}