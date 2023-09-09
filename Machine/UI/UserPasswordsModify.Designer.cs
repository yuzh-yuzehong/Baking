namespace Machine
{
    partial class UserPasswordsModify
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
            this.tablePanelUserPasswords = new System.Windows.Forms.TableLayoutPanel();
            this.SuspendLayout();
            // 
            // tablePanelUserPasswords
            // 
            this.tablePanelUserPasswords.ColumnCount = 2;
            this.tablePanelUserPasswords.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelUserPasswords.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelUserPasswords.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tablePanelUserPasswords.Location = new System.Drawing.Point(0, 0);
            this.tablePanelUserPasswords.Name = "tablePanelUserPasswords";
            this.tablePanelUserPasswords.RowCount = 2;
            this.tablePanelUserPasswords.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelUserPasswords.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelUserPasswords.Size = new System.Drawing.Size(482, 303);
            this.tablePanelUserPasswords.TabIndex = 0;
            // 
            // UserPasswordsModify
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(482, 303);
            this.Controls.Add(this.tablePanelUserPasswords);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UserPasswordsModify";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "UserPasswordsModify";
            this.TopMost = true;
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tablePanelUserPasswords;
    }
}