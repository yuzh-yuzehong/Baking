﻿namespace Machine
{
    partial class MesParameterPage
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.textBoxRecv = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxUri = new System.Windows.Forms.TextBox();
            this.dataGridViewParameter = new Machine.DataGridViewNF();
            this.textBoxSend = new System.Windows.Forms.TextBox();
            this.buttonSave = new System.Windows.Forms.Button();
            this.checkBoxEnable = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewParameter)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 10;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.Controls.Add(this.textBoxRecv, 5, 6);
            this.tableLayoutPanel1.Controls.Add(this.label1, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.textBoxUri, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.dataGridViewParameter, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.textBoxSend, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.checkBoxEnable, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonSave, 8, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 10;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(800, 500);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // textBoxRecv
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.textBoxRecv, 4);
            this.textBoxRecv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxRecv.Location = new System.Drawing.Point(403, 303);
            this.textBoxRecv.Multiline = true;
            this.textBoxRecv.Name = "textBoxRecv";
            this.textBoxRecv.ReadOnly = true;
            this.tableLayoutPanel1.SetRowSpan(this.textBoxRecv, 3);
            this.textBoxRecv.Size = new System.Drawing.Size(314, 144);
            this.textBoxRecv.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(83, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(74, 30);
            this.label1.TabIndex = 0;
            this.label1.Text = "MES接口地址URL:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBoxUri
            // 
            this.textBoxUri.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.textBoxUri, 5);
            this.textBoxUri.Location = new System.Drawing.Point(163, 12);
            this.textBoxUri.Name = "textBoxUri";
            this.textBoxUri.Size = new System.Drawing.Size(394, 25);
            this.textBoxUri.TabIndex = 1;
            // 
            // dataGridViewParameter
            // 
            this.dataGridViewParameter.BackgroundColor = System.Drawing.SystemColors.Control;
            this.dataGridViewParameter.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.tableLayoutPanel1.SetColumnSpan(this.dataGridViewParameter, 9);
            this.dataGridViewParameter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewParameter.Location = new System.Drawing.Point(3, 53);
            this.dataGridViewParameter.Name = "dataGridViewParameter";
            this.tableLayoutPanel1.SetRowSpan(this.dataGridViewParameter, 5);
            this.dataGridViewParameter.RowTemplate.Height = 27;
            this.dataGridViewParameter.Size = new System.Drawing.Size(714, 244);
            this.dataGridViewParameter.TabIndex = 2;
            // 
            // textBoxSend
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.textBoxSend, 4);
            this.textBoxSend.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxSend.Location = new System.Drawing.Point(3, 303);
            this.textBoxSend.Multiline = true;
            this.textBoxSend.Name = "textBoxSend";
            this.textBoxSend.ReadOnly = true;
            this.tableLayoutPanel1.SetRowSpan(this.textBoxSend, 3);
            this.textBoxSend.Size = new System.Drawing.Size(314, 144);
            this.textBoxSend.TabIndex = 4;
            // 
            // buttonSave
            // 
            this.buttonSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSave.Location = new System.Drawing.Point(643, 3);
            this.buttonSave.MaximumSize = new System.Drawing.Size(180, 50);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(74, 44);
            this.buttonSave.TabIndex = 3;
            this.buttonSave.Text = "保存";
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            // 
            // checkBoxEnable
            // 
            this.checkBoxEnable.AutoSize = true;
            this.checkBoxEnable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxEnable.Location = new System.Drawing.Point(3, 3);
            this.checkBoxEnable.Name = "checkBoxEnable";
            this.checkBoxEnable.Size = new System.Drawing.Size(74, 44);
            this.checkBoxEnable.TabIndex = 9;
            this.checkBoxEnable.Text = "接口使能";
            this.checkBoxEnable.UseVisualStyleBackColor = true;
            this.checkBoxEnable.CheckedChanged += new System.EventHandler(this.checkBoxMesEnable_CheckedChanged);
            // 
            // MesParameterPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 500);
            this.Controls.Add(this.tableLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "MesParameterPage";
            this.Text = "MesParameterPage";
            this.Load += new System.EventHandler(this.MesParameterPage_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewParameter)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxUri;
        private DataGridViewNF dataGridViewParameter;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.TextBox textBoxSend;
        private System.Windows.Forms.TextBox textBoxRecv;
        private System.Windows.Forms.CheckBox checkBoxEnable;
    }
}