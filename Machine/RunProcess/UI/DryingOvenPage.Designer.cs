namespace Machine
{
    partial class DryingOvenPage
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
            this.tablePanelOvenInfo = new System.Windows.Forms.TableLayoutPanel();
            this.listViewParameter = new System.Windows.Forms.ListView();
            this.listViewTemp = new System.Windows.Forms.ListView();
            this.listViewState = new System.Windows.Forms.ListView();
            this.listViewAlarm = new System.Windows.Forms.ListView();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.labelRemote = new System.Windows.Forms.Label();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.radioButton3 = new System.Windows.Forms.RadioButton();
            this.radioButton4 = new System.Windows.Forms.RadioButton();
            this.labelOvenIP = new System.Windows.Forms.Label();
            this.buttonDisconnect = new System.Windows.Forms.Button();
            this.buttonConnect = new System.Windows.Forms.Button();
            this.buttonSetParameter = new System.Windows.Forms.Button();
            this.buttonErrorReset = new System.Windows.Forms.Button();
            this.buttonWorkStart = new System.Windows.Forms.Button();
            this.label26 = new System.Windows.Forms.Label();
            this.comboBoxDryingOven = new System.Windows.Forms.ComboBox();
            this.labelOvenState = new System.Windows.Forms.Label();
            this.buttonCloseDoor = new System.Windows.Forms.Button();
            this.buttonCloseVac = new System.Windows.Forms.Button();
            this.buttonCloseBlowAir = new System.Windows.Forms.Button();
            this.buttonWorkStop = new System.Windows.Forms.Button();
            this.buttonOpenDoor = new System.Windows.Forms.Button();
            this.buttonOpenVac = new System.Windows.Forms.Button();
            this.buttonOpenBlowAir = new System.Windows.Forms.Button();
            this.labelDryingOven = new System.Windows.Forms.Label();
            this.checkBoxChart = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel1.SuspendLayout();
            this.tablePanelOvenInfo.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 24.14634F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 75.85366F));
            this.tableLayoutPanel1.Controls.Add(this.tablePanelOvenInfo, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel3, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(1);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 625F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1093, 625);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // tablePanelOvenInfo
            // 
            this.tablePanelOvenInfo.ColumnCount = 3;
            this.tablePanelOvenInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tablePanelOvenInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tablePanelOvenInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tablePanelOvenInfo.Controls.Add(this.listViewParameter, 1, 0);
            this.tablePanelOvenInfo.Controls.Add(this.listViewTemp, 2, 0);
            this.tablePanelOvenInfo.Controls.Add(this.listViewState, 0, 0);
            this.tablePanelOvenInfo.Controls.Add(this.listViewAlarm, 0, 1);
            this.tablePanelOvenInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tablePanelOvenInfo.Location = new System.Drawing.Point(264, 1);
            this.tablePanelOvenInfo.Margin = new System.Windows.Forms.Padding(1);
            this.tablePanelOvenInfo.Name = "tablePanelOvenInfo";
            this.tablePanelOvenInfo.RowCount = 2;
            this.tablePanelOvenInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.tablePanelOvenInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tablePanelOvenInfo.Size = new System.Drawing.Size(828, 623);
            this.tablePanelOvenInfo.TabIndex = 13;
            // 
            // listViewParameter
            // 
            this.listViewParameter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewParameter.Location = new System.Drawing.Point(332, 1);
            this.listViewParameter.Margin = new System.Windows.Forms.Padding(1);
            this.listViewParameter.Name = "listViewParameter";
            this.tablePanelOvenInfo.SetRowSpan(this.listViewParameter, 2);
            this.listViewParameter.Size = new System.Drawing.Size(246, 621);
            this.listViewParameter.TabIndex = 5;
            this.listViewParameter.UseCompatibleStateImageBehavior = false;
            // 
            // listViewTemp
            // 
            this.listViewTemp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewTemp.Location = new System.Drawing.Point(580, 1);
            this.listViewTemp.Margin = new System.Windows.Forms.Padding(1);
            this.listViewTemp.Name = "listViewTemp";
            this.tablePanelOvenInfo.SetRowSpan(this.listViewTemp, 2);
            this.listViewTemp.Size = new System.Drawing.Size(247, 621);
            this.listViewTemp.TabIndex = 0;
            this.listViewTemp.UseCompatibleStateImageBehavior = false;
            // 
            // listViewState
            // 
            this.listViewState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewState.Location = new System.Drawing.Point(1, 1);
            this.listViewState.Margin = new System.Windows.Forms.Padding(1);
            this.listViewState.Name = "listViewState";
            this.listViewState.Size = new System.Drawing.Size(329, 371);
            this.listViewState.TabIndex = 2;
            this.listViewState.UseCompatibleStateImageBehavior = false;
            // 
            // listViewAlarm
            // 
            this.listViewAlarm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewAlarm.Location = new System.Drawing.Point(1, 374);
            this.listViewAlarm.Margin = new System.Windows.Forms.Padding(1);
            this.listViewAlarm.Name = "listViewAlarm";
            this.listViewAlarm.Size = new System.Drawing.Size(329, 248);
            this.listViewAlarm.TabIndex = 3;
            this.listViewAlarm.UseCompatibleStateImageBehavior = false;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 4;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel3.Controls.Add(this.labelRemote, 2, 1);
            this.tableLayoutPanel3.Controls.Add(this.radioButton1, 3, 7);
            this.tableLayoutPanel3.Controls.Add(this.radioButton2, 3, 6);
            this.tableLayoutPanel3.Controls.Add(this.radioButton3, 3, 5);
            this.tableLayoutPanel3.Controls.Add(this.radioButton4, 3, 4);
            this.tableLayoutPanel3.Controls.Add(this.labelOvenIP, 0, 2);
            this.tableLayoutPanel3.Controls.Add(this.buttonDisconnect, 2, 3);
            this.tableLayoutPanel3.Controls.Add(this.buttonConnect, 0, 3);
            this.tableLayoutPanel3.Controls.Add(this.buttonSetParameter, 0, 12);
            this.tableLayoutPanel3.Controls.Add(this.buttonErrorReset, 2, 12);
            this.tableLayoutPanel3.Controls.Add(this.buttonWorkStart, 0, 11);
            this.tableLayoutPanel3.Controls.Add(this.label26, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.comboBoxDryingOven, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.labelOvenState, 3, 2);
            this.tableLayoutPanel3.Controls.Add(this.buttonCloseDoor, 2, 8);
            this.tableLayoutPanel3.Controls.Add(this.buttonCloseVac, 2, 9);
            this.tableLayoutPanel3.Controls.Add(this.buttonCloseBlowAir, 2, 10);
            this.tableLayoutPanel3.Controls.Add(this.buttonWorkStop, 2, 11);
            this.tableLayoutPanel3.Controls.Add(this.buttonOpenDoor, 0, 8);
            this.tableLayoutPanel3.Controls.Add(this.buttonOpenVac, 0, 9);
            this.tableLayoutPanel3.Controls.Add(this.buttonOpenBlowAir, 0, 10);
            this.tableLayoutPanel3.Controls.Add(this.labelDryingOven, 0, 4);
            this.tableLayoutPanel3.Controls.Add(this.checkBoxChart, 2, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(1, 1);
            this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(1);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 14;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10.9375F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10.9375F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10.9375F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10.9375F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(261, 623);
            this.tableLayoutPanel3.TabIndex = 12;
            // 
            // labelRemote
            // 
            this.labelRemote.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel3.SetColumnSpan(this.labelRemote, 2);
            this.labelRemote.Location = new System.Drawing.Point(134, 40);
            this.labelRemote.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelRemote.Name = "labelRemote";
            this.labelRemote.Size = new System.Drawing.Size(123, 31);
            this.labelRemote.TabIndex = 36;
            this.labelRemote.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // radioButton1
            // 
            this.radioButton1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.radioButton1.AutoSize = true;
            this.radioButton1.Font = new System.Drawing.Font("宋体", 9F);
            this.radioButton1.Location = new System.Drawing.Point(196, 366);
            this.radioButton1.Margin = new System.Windows.Forms.Padding(1);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(64, 19);
            this.radioButton1.TabIndex = 34;
            this.radioButton1.TabStop = true;
            this.radioButton1.Tag = "0";
            this.radioButton1.Text = "第1层";
            this.radioButton1.UseVisualStyleBackColor = true;
            this.radioButton1.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // radioButton2
            // 
            this.radioButton2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.radioButton2.AutoSize = true;
            this.radioButton2.Font = new System.Drawing.Font("宋体", 9F);
            this.radioButton2.Location = new System.Drawing.Point(196, 301);
            this.radioButton2.Margin = new System.Windows.Forms.Padding(1);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(64, 19);
            this.radioButton2.TabIndex = 32;
            this.radioButton2.TabStop = true;
            this.radioButton2.Tag = "1";
            this.radioButton2.Text = "第2层";
            this.radioButton2.UseVisualStyleBackColor = true;
            this.radioButton2.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // radioButton3
            // 
            this.radioButton3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.radioButton3.AutoSize = true;
            this.radioButton3.Font = new System.Drawing.Font("宋体", 9F);
            this.radioButton3.Location = new System.Drawing.Point(196, 236);
            this.radioButton3.Margin = new System.Windows.Forms.Padding(1);
            this.radioButton3.Name = "radioButton3";
            this.radioButton3.Size = new System.Drawing.Size(64, 19);
            this.radioButton3.TabIndex = 31;
            this.radioButton3.TabStop = true;
            this.radioButton3.Tag = "2";
            this.radioButton3.Text = "第3层";
            this.radioButton3.UseVisualStyleBackColor = true;
            this.radioButton3.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // radioButton4
            // 
            this.radioButton4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.radioButton4.AutoSize = true;
            this.radioButton4.Font = new System.Drawing.Font("宋体", 9F);
            this.radioButton4.Location = new System.Drawing.Point(196, 171);
            this.radioButton4.Margin = new System.Windows.Forms.Padding(1);
            this.radioButton4.Name = "radioButton4";
            this.radioButton4.Size = new System.Drawing.Size(64, 19);
            this.radioButton4.TabIndex = 30;
            this.radioButton4.TabStop = true;
            this.radioButton4.Tag = "3";
            this.radioButton4.Text = "第4层";
            this.radioButton4.UseVisualStyleBackColor = true;
            this.radioButton4.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // labelOvenIP
            // 
            this.labelOvenIP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel3.SetColumnSpan(this.labelOvenIP, 3);
            this.labelOvenIP.Location = new System.Drawing.Point(4, 77);
            this.labelOvenIP.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelOvenIP.Name = "labelOvenIP";
            this.labelOvenIP.Size = new System.Drawing.Size(187, 31);
            this.labelOvenIP.TabIndex = 28;
            this.labelOvenIP.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // buttonDisconnect
            // 
            this.tableLayoutPanel3.SetColumnSpan(this.buttonDisconnect, 2);
            this.buttonDisconnect.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonDisconnect.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonDisconnect.Location = new System.Drawing.Point(131, 112);
            this.buttonDisconnect.Margin = new System.Windows.Forms.Padding(1);
            this.buttonDisconnect.Name = "buttonDisconnect";
            this.buttonDisconnect.Size = new System.Drawing.Size(129, 35);
            this.buttonDisconnect.TabIndex = 27;
            this.buttonDisconnect.Text = "断开";
            this.buttonDisconnect.UseVisualStyleBackColor = true;
            this.buttonDisconnect.Click += new System.EventHandler(this.buttonDisconnect_Click);
            // 
            // buttonConnect
            // 
            this.tableLayoutPanel3.SetColumnSpan(this.buttonConnect, 2);
            this.buttonConnect.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonConnect.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonConnect.Location = new System.Drawing.Point(1, 112);
            this.buttonConnect.Margin = new System.Windows.Forms.Padding(1);
            this.buttonConnect.Name = "buttonConnect";
            this.buttonConnect.Size = new System.Drawing.Size(128, 35);
            this.buttonConnect.TabIndex = 26;
            this.buttonConnect.Text = "连接";
            this.buttonConnect.UseVisualStyleBackColor = true;
            this.buttonConnect.Click += new System.EventHandler(this.buttonConnect_Click);
            // 
            // buttonSetParameter
            // 
            this.tableLayoutPanel3.SetColumnSpan(this.buttonSetParameter, 2);
            this.buttonSetParameter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonSetParameter.Font = new System.Drawing.Font("宋体", 10F);
            this.buttonSetParameter.Location = new System.Drawing.Point(1, 557);
            this.buttonSetParameter.Margin = new System.Windows.Forms.Padding(1);
            this.buttonSetParameter.Name = "buttonSetParameter";
            this.buttonSetParameter.Size = new System.Drawing.Size(128, 35);
            this.buttonSetParameter.TabIndex = 24;
            this.buttonSetParameter.Text = "参数设置";
            this.buttonSetParameter.UseVisualStyleBackColor = true;
            this.buttonSetParameter.Click += new System.EventHandler(this.buttonSetParameter_Click);
            // 
            // buttonErrorReset
            // 
            this.tableLayoutPanel3.SetColumnSpan(this.buttonErrorReset, 2);
            this.buttonErrorReset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonErrorReset.Font = new System.Drawing.Font("宋体", 10F);
            this.buttonErrorReset.Location = new System.Drawing.Point(131, 557);
            this.buttonErrorReset.Margin = new System.Windows.Forms.Padding(1);
            this.buttonErrorReset.Name = "buttonErrorReset";
            this.buttonErrorReset.Size = new System.Drawing.Size(129, 35);
            this.buttonErrorReset.TabIndex = 8;
            this.buttonErrorReset.Text = "解除维修";
            this.buttonErrorReset.UseVisualStyleBackColor = true;
            this.buttonErrorReset.Click += new System.EventHandler(this.buttonErrorReset_Click);
            // 
            // buttonWorkStart
            // 
            this.tableLayoutPanel3.SetColumnSpan(this.buttonWorkStart, 2);
            this.buttonWorkStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonWorkStart.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonWorkStart.Location = new System.Drawing.Point(1, 520);
            this.buttonWorkStart.Margin = new System.Windows.Forms.Padding(1);
            this.buttonWorkStart.Name = "buttonWorkStart";
            this.buttonWorkStart.Size = new System.Drawing.Size(128, 35);
            this.buttonWorkStart.TabIndex = 23;
            this.buttonWorkStart.Text = "启动";
            this.buttonWorkStart.UseVisualStyleBackColor = true;
            this.buttonWorkStart.Click += new System.EventHandler(this.buttonWorkStart_Click);
            // 
            // label26
            // 
            this.label26.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel3.SetColumnSpan(this.label26, 2);
            this.label26.Location = new System.Drawing.Point(4, 3);
            this.label26.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(122, 31);
            this.label26.TabIndex = 1;
            this.label26.Text = "干燥炉编号：";
            this.label26.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // comboBoxDryingOven
            // 
            this.comboBoxDryingOven.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxDryingOven.BackColor = System.Drawing.SystemColors.Window;
            this.tableLayoutPanel3.SetColumnSpan(this.comboBoxDryingOven, 2);
            this.comboBoxDryingOven.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxDryingOven.Font = new System.Drawing.Font("宋体", 12F);
            this.comboBoxDryingOven.ForeColor = System.Drawing.SystemColors.WindowText;
            this.comboBoxDryingOven.FormattingEnabled = true;
            this.comboBoxDryingOven.ItemHeight = 20;
            this.comboBoxDryingOven.Location = new System.Drawing.Point(3, 41);
            this.comboBoxDryingOven.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.comboBoxDryingOven.Name = "comboBoxDryingOven";
            this.comboBoxDryingOven.Size = new System.Drawing.Size(124, 28);
            this.comboBoxDryingOven.TabIndex = 6;
            this.comboBoxDryingOven.SelectedIndexChanged += new System.EventHandler(this.comboBoxDryingOven_SelectedIndexChanged);
            // 
            // labelOvenState
            // 
            this.labelOvenState.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.labelOvenState.Location = new System.Drawing.Point(199, 77);
            this.labelOvenState.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelOvenState.Name = "labelOvenState";
            this.labelOvenState.Size = new System.Drawing.Size(58, 31);
            this.labelOvenState.TabIndex = 12;
            this.labelOvenState.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // buttonCloseDoor
            // 
            this.tableLayoutPanel3.SetColumnSpan(this.buttonCloseDoor, 2);
            this.buttonCloseDoor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonCloseDoor.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonCloseDoor.Location = new System.Drawing.Point(131, 409);
            this.buttonCloseDoor.Margin = new System.Windows.Forms.Padding(1);
            this.buttonCloseDoor.Name = "buttonCloseDoor";
            this.buttonCloseDoor.Size = new System.Drawing.Size(129, 35);
            this.buttonCloseDoor.TabIndex = 16;
            this.buttonCloseDoor.Text = "关门";
            this.buttonCloseDoor.UseVisualStyleBackColor = true;
            this.buttonCloseDoor.Click += new System.EventHandler(this.buttonCloseDoor_Click);
            // 
            // buttonCloseVac
            // 
            this.tableLayoutPanel3.SetColumnSpan(this.buttonCloseVac, 2);
            this.buttonCloseVac.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonCloseVac.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonCloseVac.Location = new System.Drawing.Point(131, 446);
            this.buttonCloseVac.Margin = new System.Windows.Forms.Padding(1);
            this.buttonCloseVac.Name = "buttonCloseVac";
            this.buttonCloseVac.Size = new System.Drawing.Size(129, 35);
            this.buttonCloseVac.TabIndex = 17;
            this.buttonCloseVac.Text = "抽真空关";
            this.buttonCloseVac.UseVisualStyleBackColor = true;
            this.buttonCloseVac.Click += new System.EventHandler(this.buttonCloseVac_Click);
            // 
            // buttonCloseBlowAir
            // 
            this.tableLayoutPanel3.SetColumnSpan(this.buttonCloseBlowAir, 2);
            this.buttonCloseBlowAir.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonCloseBlowAir.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonCloseBlowAir.Location = new System.Drawing.Point(131, 483);
            this.buttonCloseBlowAir.Margin = new System.Windows.Forms.Padding(1);
            this.buttonCloseBlowAir.Name = "buttonCloseBlowAir";
            this.buttonCloseBlowAir.Size = new System.Drawing.Size(129, 35);
            this.buttonCloseBlowAir.TabIndex = 18;
            this.buttonCloseBlowAir.Text = "破真空关";
            this.buttonCloseBlowAir.UseVisualStyleBackColor = true;
            this.buttonCloseBlowAir.Click += new System.EventHandler(this.buttonCloseBlowAir_Click);
            // 
            // buttonWorkStop
            // 
            this.tableLayoutPanel3.SetColumnSpan(this.buttonWorkStop, 2);
            this.buttonWorkStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonWorkStop.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonWorkStop.Location = new System.Drawing.Point(131, 520);
            this.buttonWorkStop.Margin = new System.Windows.Forms.Padding(1);
            this.buttonWorkStop.Name = "buttonWorkStop";
            this.buttonWorkStop.Size = new System.Drawing.Size(129, 35);
            this.buttonWorkStop.TabIndex = 19;
            this.buttonWorkStop.Text = "停止";
            this.buttonWorkStop.UseVisualStyleBackColor = true;
            this.buttonWorkStop.Click += new System.EventHandler(this.buttonWorkStop_Click);
            // 
            // buttonOpenDoor
            // 
            this.tableLayoutPanel3.SetColumnSpan(this.buttonOpenDoor, 2);
            this.buttonOpenDoor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonOpenDoor.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonOpenDoor.Location = new System.Drawing.Point(1, 409);
            this.buttonOpenDoor.Margin = new System.Windows.Forms.Padding(1);
            this.buttonOpenDoor.Name = "buttonOpenDoor";
            this.buttonOpenDoor.Size = new System.Drawing.Size(128, 35);
            this.buttonOpenDoor.TabIndex = 20;
            this.buttonOpenDoor.Text = "开门";
            this.buttonOpenDoor.UseVisualStyleBackColor = true;
            this.buttonOpenDoor.Click += new System.EventHandler(this.buttonOpenDoor_Click);
            // 
            // buttonOpenVac
            // 
            this.tableLayoutPanel3.SetColumnSpan(this.buttonOpenVac, 2);
            this.buttonOpenVac.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonOpenVac.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonOpenVac.Location = new System.Drawing.Point(1, 446);
            this.buttonOpenVac.Margin = new System.Windows.Forms.Padding(1);
            this.buttonOpenVac.Name = "buttonOpenVac";
            this.buttonOpenVac.Size = new System.Drawing.Size(128, 35);
            this.buttonOpenVac.TabIndex = 21;
            this.buttonOpenVac.Text = "抽真空开";
            this.buttonOpenVac.UseVisualStyleBackColor = true;
            this.buttonOpenVac.Click += new System.EventHandler(this.buttonOpenVac_Click);
            // 
            // buttonOpenBlowAir
            // 
            this.tableLayoutPanel3.SetColumnSpan(this.buttonOpenBlowAir, 2);
            this.buttonOpenBlowAir.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonOpenBlowAir.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonOpenBlowAir.Location = new System.Drawing.Point(1, 483);
            this.buttonOpenBlowAir.Margin = new System.Windows.Forms.Padding(1);
            this.buttonOpenBlowAir.Name = "buttonOpenBlowAir";
            this.buttonOpenBlowAir.Size = new System.Drawing.Size(128, 35);
            this.buttonOpenBlowAir.TabIndex = 22;
            this.buttonOpenBlowAir.Text = "破真空开";
            this.buttonOpenBlowAir.UseVisualStyleBackColor = true;
            this.buttonOpenBlowAir.Click += new System.EventHandler(this.buttonOpenBlowAir_Click);
            // 
            // labelDryingOven
            // 
            this.labelDryingOven.AutoSize = true;
            this.tableLayoutPanel3.SetColumnSpan(this.labelDryingOven, 3);
            this.labelDryingOven.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelDryingOven.Location = new System.Drawing.Point(3, 148);
            this.labelDryingOven.Name = "labelDryingOven";
            this.tableLayoutPanel3.SetRowSpan(this.labelDryingOven, 4);
            this.labelDryingOven.Size = new System.Drawing.Size(189, 260);
            this.labelDryingOven.TabIndex = 29;
            this.labelDryingOven.Paint += new System.Windows.Forms.PaintEventHandler(this.labelDryingOven_Paint);
            // 
            // checkBoxChart
            // 
            this.checkBoxChart.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxChart.AutoSize = true;
            this.tableLayoutPanel3.SetColumnSpan(this.checkBoxChart, 2);
            this.checkBoxChart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxChart.Location = new System.Drawing.Point(133, 3);
            this.checkBoxChart.Name = "checkBoxChart";
            this.checkBoxChart.Size = new System.Drawing.Size(125, 31);
            this.checkBoxChart.TabIndex = 35;
            this.checkBoxChart.Text = "温度曲线";
            this.checkBoxChart.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBoxChart.UseVisualStyleBackColor = true;
            this.checkBoxChart.CheckedChanged += new System.EventHandler(this.checkBoxChart_CheckedChanged);
            // 
            // DryingOvenPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1093, 625);
            this.Controls.Add(this.tableLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "DryingOvenPage";
            this.Text = "DryingOvenPage";
            this.Load += new System.EventHandler(this.DryingOvenPage_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tablePanelOvenInfo.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.TableLayoutPanel tablePanelOvenInfo;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.ComboBox comboBoxDryingOven;
        private System.Windows.Forms.Label labelOvenIP;
        private System.Windows.Forms.Label labelOvenState;
        private System.Windows.Forms.Button buttonConnect;
        private System.Windows.Forms.Button buttonDisconnect;
        private System.Windows.Forms.Button buttonCloseDoor;
        private System.Windows.Forms.Button buttonCloseVac;
        private System.Windows.Forms.Button buttonCloseBlowAir;
        private System.Windows.Forms.Button buttonWorkStart;
        private System.Windows.Forms.Button buttonWorkStop;
        private System.Windows.Forms.Button buttonOpenDoor;
        private System.Windows.Forms.Button buttonOpenVac;
        private System.Windows.Forms.Button buttonOpenBlowAir;
        private System.Windows.Forms.Button buttonSetParameter;
        private System.Windows.Forms.Button buttonErrorReset;
        private System.Windows.Forms.ListView listViewTemp;
        private System.Windows.Forms.ListView listViewState;
        private System.Windows.Forms.ListView listViewAlarm;
        private System.Windows.Forms.ListView listViewParameter;
        private System.Windows.Forms.Label labelDryingOven;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton radioButton3;
        private System.Windows.Forms.RadioButton radioButton4;
        private System.Windows.Forms.CheckBox checkBoxChart;
        private System.Windows.Forms.Label labelRemote;
    }
}