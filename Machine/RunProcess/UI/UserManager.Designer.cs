namespace Machine
{
    partial class UserManager
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
            this.tablePanelUserManager = new System.Windows.Forms.TableLayoutPanel();
            this.dataGridViewUser = new System.Windows.Forms.DataGridView();
            this.tablePanelOperation = new System.Windows.Forms.TableLayoutPanel();
            this.tablePanelUserManager.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewUser)).BeginInit();
            this.SuspendLayout();
            // 
            // tablePanelUserManager
            // 
            this.tablePanelUserManager.ColumnCount = 2;
            this.tablePanelUserManager.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.tablePanelUserManager.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tablePanelUserManager.Controls.Add(this.dataGridViewUser, 0, 0);
            this.tablePanelUserManager.Controls.Add(this.tablePanelOperation, 1, 0);
            this.tablePanelUserManager.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tablePanelUserManager.Location = new System.Drawing.Point(0, 0);
            this.tablePanelUserManager.Name = "tablePanelUserManager";
            this.tablePanelUserManager.RowCount = 1;
            this.tablePanelUserManager.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tablePanelUserManager.Size = new System.Drawing.Size(782, 453);
            this.tablePanelUserManager.TabIndex = 0;
            // 
            // dataGridViewUser
            // 
            this.dataGridViewUser.BackgroundColor = System.Drawing.SystemColors.Control;
            this.dataGridViewUser.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewUser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewUser.Location = new System.Drawing.Point(3, 3);
            this.dataGridViewUser.Name = "dataGridViewUser";
            this.dataGridViewUser.RowTemplate.Height = 27;
            this.dataGridViewUser.Size = new System.Drawing.Size(463, 447);
            this.dataGridViewUser.TabIndex = 0;
            this.dataGridViewUser.SelectionChanged += new System.EventHandler(this.dataGridViewUser_SelectionChanged);
            // 
            // tablePanelOperation
            // 
            this.tablePanelOperation.ColumnCount = 2;
            this.tablePanelOperation.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelOperation.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelOperation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tablePanelOperation.Location = new System.Drawing.Point(472, 3);
            this.tablePanelOperation.Name = "tablePanelOperation";
            this.tablePanelOperation.RowCount = 2;
            this.tablePanelOperation.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelOperation.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePanelOperation.Size = new System.Drawing.Size(307, 447);
            this.tablePanelOperation.TabIndex = 1;
            // 
            // UserManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(782, 453);
            this.Controls.Add(this.tablePanelUserManager);
            this.Font = new System.Drawing.Font("宋体", 10F);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UserManager";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "UserManager";
            this.TopMost = true;
            this.tablePanelUserManager.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewUser)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tablePanelUserManager;
        private System.Windows.Forms.DataGridView dataGridViewUser;
        private System.Windows.Forms.TableLayoutPanel tablePanelOperation;
    }
}