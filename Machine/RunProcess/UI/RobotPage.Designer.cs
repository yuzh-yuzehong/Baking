namespace Machine
{
    partial class RobotPage
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanelOnload = new System.Windows.Forms.TableLayoutPanel();
            this.comboBoxOnloadRobot = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.labelOnloadRobotIP = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.labelOnloadConnectState = new System.Windows.Forms.Label();
            this.buttonOnloadRobotConnect = new System.Windows.Forms.Button();
            this.buttonOnloadRobotDisconnect = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBoxOnloadRobotRow = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.comboBoxOnloadRobotCol = new System.Windows.Forms.ComboBox();
            this.buttonOnloadRobotHome = new System.Windows.Forms.Button();
            this.buttonOnloadRobotMove = new System.Windows.Forms.Button();
            this.buttonOnloadRobotDown = new System.Windows.Forms.Button();
            this.buttonOnloadRobotUp = new System.Windows.Forms.Button();
            this.dataGridViewOnloadStation = new System.Windows.Forms.DataGridView();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanelTransfer = new System.Windows.Forms.TableLayoutPanel();
            this.comboBoxTransferRobot = new System.Windows.Forms.ComboBox();
            this.labelTransferRobotIP = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.labelTransferRobotConnectState = new System.Windows.Forms.Label();
            this.buttonTransferRobotConnect = new System.Windows.Forms.Button();
            this.buttonTransferRobotDisconnect = new System.Windows.Forms.Button();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.comboBoxTransferRobotRow = new System.Windows.Forms.ComboBox();
            this.comboBoxTransferRobotCol = new System.Windows.Forms.ComboBox();
            this.buttonTransferRobotPickIn = new System.Windows.Forms.Button();
            this.buttonTransferRobotMove = new System.Windows.Forms.Button();
            this.buttonTransferRobotPickOut = new System.Windows.Forms.Button();
            this.buttonTransferRobotPlaceIn = new System.Windows.Forms.Button();
            this.buttonTransferRobotPlaceOut = new System.Windows.Forms.Button();
            this.dataGridViewTransferStation = new System.Windows.Forms.DataGridView();
            this.tableLayoutPanel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanelOnload.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewOnloadStation)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.tableLayoutPanelTransfer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewTransferStation)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.groupBox1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.groupBox2, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(11);
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(800, 500);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tableLayoutPanelOnload);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(14, 14);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(383, 472);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "上料机器人";
            // 
            // tableLayoutPanelOnload
            // 
            this.tableLayoutPanelOnload.ColumnCount = 3;
            this.tableLayoutPanelOnload.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanelOnload.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanelOnload.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanelOnload.Controls.Add(this.comboBoxOnloadRobot, 0, 0);
            this.tableLayoutPanelOnload.Controls.Add(this.label1, 1, 0);
            this.tableLayoutPanelOnload.Controls.Add(this.labelOnloadRobotIP, 2, 0);
            this.tableLayoutPanelOnload.Controls.Add(this.label5, 1, 1);
            this.tableLayoutPanelOnload.Controls.Add(this.labelOnloadConnectState, 2, 1);
            this.tableLayoutPanelOnload.Controls.Add(this.buttonOnloadRobotConnect, 1, 2);
            this.tableLayoutPanelOnload.Controls.Add(this.buttonOnloadRobotDisconnect, 2, 2);
            this.tableLayoutPanelOnload.Controls.Add(this.label2, 1, 3);
            this.tableLayoutPanelOnload.Controls.Add(this.comboBoxOnloadRobotRow, 2, 3);
            this.tableLayoutPanelOnload.Controls.Add(this.label3, 1, 4);
            this.tableLayoutPanelOnload.Controls.Add(this.comboBoxOnloadRobotCol, 2, 4);
            this.tableLayoutPanelOnload.Controls.Add(this.buttonOnloadRobotHome, 1, 6);
            this.tableLayoutPanelOnload.Controls.Add(this.buttonOnloadRobotMove, 2, 6);
            this.tableLayoutPanelOnload.Controls.Add(this.buttonOnloadRobotDown, 1, 7);
            this.tableLayoutPanelOnload.Controls.Add(this.buttonOnloadRobotUp, 2, 7);
            this.tableLayoutPanelOnload.Controls.Add(this.dataGridViewOnloadStation, 0, 1);
            this.tableLayoutPanelOnload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelOnload.Location = new System.Drawing.Point(3, 23);
            this.tableLayoutPanelOnload.Name = "tableLayoutPanelOnload";
            this.tableLayoutPanelOnload.Padding = new System.Windows.Forms.Padding(6);
            this.tableLayoutPanelOnload.RowCount = 10;
            this.tableLayoutPanelOnload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10.04566F));
            this.tableLayoutPanelOnload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 9.589041F));
            this.tableLayoutPanelOnload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanelOnload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanelOnload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanelOnload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanelOnload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanelOnload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanelOnload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanelOnload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanelOnload.Size = new System.Drawing.Size(377, 446);
            this.tableLayoutPanelOnload.TabIndex = 0;
            // 
            // comboBoxOnloadRobot
            // 
            this.comboBoxOnloadRobot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxOnloadRobot.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxOnloadRobot.Font = new System.Drawing.Font("宋体", 12F);
            this.comboBoxOnloadRobot.FormattingEnabled = true;
            this.comboBoxOnloadRobot.ItemHeight = 20;
            this.comboBoxOnloadRobot.Location = new System.Drawing.Point(9, 13);
            this.comboBoxOnloadRobot.Name = "comboBoxOnloadRobot";
            this.comboBoxOnloadRobot.Size = new System.Drawing.Size(140, 28);
            this.comboBoxOnloadRobot.TabIndex = 0;
            this.comboBoxOnloadRobot.SelectedIndexChanged += new System.EventHandler(this.comboBoxOnloadRobot_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 11F);
            this.label1.Location = new System.Drawing.Point(155, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(103, 38);
            this.label1.TabIndex = 1;
            this.label1.Text = "机器人IP信息：";
            // 
            // labelOnloadRobotIP
            // 
            this.labelOnloadRobotIP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.labelOnloadRobotIP.AutoSize = true;
            this.labelOnloadRobotIP.Font = new System.Drawing.Font("宋体", 11F);
            this.labelOnloadRobotIP.Location = new System.Drawing.Point(264, 18);
            this.labelOnloadRobotIP.Name = "labelOnloadRobotIP";
            this.labelOnloadRobotIP.Size = new System.Drawing.Size(104, 19);
            this.labelOnloadRobotIP.TabIndex = 2;
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("宋体", 11F);
            this.label5.Location = new System.Drawing.Point(155, 50);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(103, 38);
            this.label5.TabIndex = 16;
            this.label5.Text = "连接状态：";
            // 
            // labelOnloadConnectState
            // 
            this.labelOnloadConnectState.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.labelOnloadConnectState.AutoSize = true;
            this.labelOnloadConnectState.Font = new System.Drawing.Font("宋体", 11F);
            this.labelOnloadConnectState.Location = new System.Drawing.Point(264, 60);
            this.labelOnloadConnectState.Name = "labelOnloadConnectState";
            this.labelOnloadConnectState.Size = new System.Drawing.Size(104, 19);
            this.labelOnloadConnectState.TabIndex = 3;
            // 
            // buttonOnloadRobotConnect
            // 
            this.buttonOnloadRobotConnect.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonOnloadRobotConnect.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonOnloadRobotConnect.Location = new System.Drawing.Point(155, 93);
            this.buttonOnloadRobotConnect.Name = "buttonOnloadRobotConnect";
            this.buttonOnloadRobotConnect.Size = new System.Drawing.Size(103, 37);
            this.buttonOnloadRobotConnect.TabIndex = 4;
            this.buttonOnloadRobotConnect.Text = "连接";
            this.buttonOnloadRobotConnect.UseVisualStyleBackColor = true;
            this.buttonOnloadRobotConnect.Click += new System.EventHandler(this.buttonOnloadRobotConnect_Click);
            // 
            // buttonOnloadRobotDisconnect
            // 
            this.buttonOnloadRobotDisconnect.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonOnloadRobotDisconnect.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonOnloadRobotDisconnect.Location = new System.Drawing.Point(264, 93);
            this.buttonOnloadRobotDisconnect.Name = "buttonOnloadRobotDisconnect";
            this.buttonOnloadRobotDisconnect.Size = new System.Drawing.Size(104, 37);
            this.buttonOnloadRobotDisconnect.TabIndex = 5;
            this.buttonOnloadRobotDisconnect.Text = "断开";
            this.buttonOnloadRobotDisconnect.UseVisualStyleBackColor = true;
            this.buttonOnloadRobotDisconnect.Click += new System.EventHandler(this.buttonOnloadRobotDisconnect_Click);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("宋体", 11F);
            this.label2.Location = new System.Drawing.Point(155, 145);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(103, 19);
            this.label2.TabIndex = 6;
            this.label2.Text = "工位行：";
            // 
            // comboBoxOnloadRobotRow
            // 
            this.comboBoxOnloadRobotRow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxOnloadRobotRow.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxOnloadRobotRow.Font = new System.Drawing.Font("宋体", 11F);
            this.comboBoxOnloadRobotRow.FormattingEnabled = true;
            this.comboBoxOnloadRobotRow.Location = new System.Drawing.Point(264, 141);
            this.comboBoxOnloadRobotRow.Name = "comboBoxOnloadRobotRow";
            this.comboBoxOnloadRobotRow.Size = new System.Drawing.Size(104, 26);
            this.comboBoxOnloadRobotRow.TabIndex = 8;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("宋体", 11F);
            this.label3.Location = new System.Drawing.Point(155, 188);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(103, 19);
            this.label3.TabIndex = 7;
            this.label3.Text = "工位列:";
            // 
            // comboBoxOnloadRobotCol
            // 
            this.comboBoxOnloadRobotCol.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxOnloadRobotCol.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxOnloadRobotCol.Font = new System.Drawing.Font("宋体", 11F);
            this.comboBoxOnloadRobotCol.FormattingEnabled = true;
            this.comboBoxOnloadRobotCol.Location = new System.Drawing.Point(264, 184);
            this.comboBoxOnloadRobotCol.Name = "comboBoxOnloadRobotCol";
            this.comboBoxOnloadRobotCol.Size = new System.Drawing.Size(104, 26);
            this.comboBoxOnloadRobotCol.TabIndex = 9;
            // 
            // buttonOnloadRobotHome
            // 
            this.buttonOnloadRobotHome.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonOnloadRobotHome.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonOnloadRobotHome.Location = new System.Drawing.Point(155, 265);
            this.buttonOnloadRobotHome.Name = "buttonOnloadRobotHome";
            this.buttonOnloadRobotHome.Size = new System.Drawing.Size(103, 37);
            this.buttonOnloadRobotHome.TabIndex = 14;
            this.buttonOnloadRobotHome.Text = "回零";
            this.buttonOnloadRobotHome.UseVisualStyleBackColor = true;
            this.buttonOnloadRobotHome.Click += new System.EventHandler(this.buttonOnloadRobotHome_Click);
            // 
            // buttonOnloadRobotMove
            // 
            this.buttonOnloadRobotMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonOnloadRobotMove.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonOnloadRobotMove.Location = new System.Drawing.Point(264, 265);
            this.buttonOnloadRobotMove.Name = "buttonOnloadRobotMove";
            this.buttonOnloadRobotMove.Size = new System.Drawing.Size(104, 37);
            this.buttonOnloadRobotMove.TabIndex = 11;
            this.buttonOnloadRobotMove.Text = "移动";
            this.buttonOnloadRobotMove.UseVisualStyleBackColor = true;
            this.buttonOnloadRobotMove.Click += new System.EventHandler(this.buttonOnloadRobotMove_Click);
            // 
            // buttonOnloadRobotDown
            // 
            this.buttonOnloadRobotDown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonOnloadRobotDown.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonOnloadRobotDown.Location = new System.Drawing.Point(155, 308);
            this.buttonOnloadRobotDown.Name = "buttonOnloadRobotDown";
            this.buttonOnloadRobotDown.Size = new System.Drawing.Size(103, 37);
            this.buttonOnloadRobotDown.TabIndex = 12;
            this.buttonOnloadRobotDown.Text = "下降";
            this.buttonOnloadRobotDown.UseVisualStyleBackColor = true;
            this.buttonOnloadRobotDown.Click += new System.EventHandler(this.buttonOnloadRobotDown_Click);
            // 
            // buttonOnloadRobotUp
            // 
            this.buttonOnloadRobotUp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonOnloadRobotUp.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonOnloadRobotUp.Location = new System.Drawing.Point(264, 308);
            this.buttonOnloadRobotUp.Name = "buttonOnloadRobotUp";
            this.buttonOnloadRobotUp.Size = new System.Drawing.Size(104, 37);
            this.buttonOnloadRobotUp.TabIndex = 13;
            this.buttonOnloadRobotUp.Text = "上升";
            this.buttonOnloadRobotUp.UseVisualStyleBackColor = true;
            this.buttonOnloadRobotUp.Click += new System.EventHandler(this.buttonOnloadRobotUp_Click);
            // 
            // dataGridViewOnloadStation
            // 
            this.dataGridViewOnloadStation.BackgroundColor = System.Drawing.SystemColors.Control;
            this.dataGridViewOnloadStation.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewOnloadStation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewOnloadStation.Location = new System.Drawing.Point(9, 52);
            this.dataGridViewOnloadStation.Name = "dataGridViewOnloadStation";
            this.tableLayoutPanelOnload.SetRowSpan(this.dataGridViewOnloadStation, 9);
            this.dataGridViewOnloadStation.RowTemplate.Height = 27;
            this.dataGridViewOnloadStation.Size = new System.Drawing.Size(140, 385);
            this.dataGridViewOnloadStation.TabIndex = 17;
            this.dataGridViewOnloadStation.SelectionChanged += new System.EventHandler(this.dataGridViewOnloadRobotStation_SelectionChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.tableLayoutPanelTransfer);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(403, 14);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(383, 472);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "调度机器人";
            // 
            // tableLayoutPanelTransfer
            // 
            this.tableLayoutPanelTransfer.ColumnCount = 3;
            this.tableLayoutPanelTransfer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanelTransfer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanelTransfer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanelTransfer.Controls.Add(this.comboBoxTransferRobot, 0, 0);
            this.tableLayoutPanelTransfer.Controls.Add(this.labelTransferRobotIP, 2, 0);
            this.tableLayoutPanelTransfer.Controls.Add(this.label4, 1, 0);
            this.tableLayoutPanelTransfer.Controls.Add(this.label6, 1, 1);
            this.tableLayoutPanelTransfer.Controls.Add(this.labelTransferRobotConnectState, 2, 1);
            this.tableLayoutPanelTransfer.Controls.Add(this.buttonTransferRobotConnect, 1, 2);
            this.tableLayoutPanelTransfer.Controls.Add(this.buttonTransferRobotDisconnect, 2, 2);
            this.tableLayoutPanelTransfer.Controls.Add(this.label8, 1, 3);
            this.tableLayoutPanelTransfer.Controls.Add(this.label7, 1, 4);
            this.tableLayoutPanelTransfer.Controls.Add(this.comboBoxTransferRobotRow, 2, 3);
            this.tableLayoutPanelTransfer.Controls.Add(this.comboBoxTransferRobotCol, 2, 4);
            this.tableLayoutPanelTransfer.Controls.Add(this.buttonTransferRobotPickIn, 1, 7);
            this.tableLayoutPanelTransfer.Controls.Add(this.buttonTransferRobotMove, 2, 6);
            this.tableLayoutPanelTransfer.Controls.Add(this.buttonTransferRobotPickOut, 2, 7);
            this.tableLayoutPanelTransfer.Controls.Add(this.buttonTransferRobotPlaceIn, 1, 8);
            this.tableLayoutPanelTransfer.Controls.Add(this.buttonTransferRobotPlaceOut, 2, 8);
            this.tableLayoutPanelTransfer.Controls.Add(this.dataGridViewTransferStation, 0, 1);
            this.tableLayoutPanelTransfer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelTransfer.Location = new System.Drawing.Point(3, 23);
            this.tableLayoutPanelTransfer.Name = "tableLayoutPanelTransfer";
            this.tableLayoutPanelTransfer.Padding = new System.Windows.Forms.Padding(6);
            this.tableLayoutPanelTransfer.RowCount = 10;
            this.tableLayoutPanelTransfer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanelTransfer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanelTransfer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanelTransfer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanelTransfer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanelTransfer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanelTransfer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanelTransfer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanelTransfer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanelTransfer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanelTransfer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelTransfer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelTransfer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelTransfer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelTransfer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelTransfer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelTransfer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelTransfer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelTransfer.Size = new System.Drawing.Size(377, 446);
            this.tableLayoutPanelTransfer.TabIndex = 1;
            // 
            // comboBoxTransferRobot
            // 
            this.comboBoxTransferRobot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxTransferRobot.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxTransferRobot.Font = new System.Drawing.Font("宋体", 12F);
            this.comboBoxTransferRobot.FormattingEnabled = true;
            this.comboBoxTransferRobot.Location = new System.Drawing.Point(9, 13);
            this.comboBoxTransferRobot.Name = "comboBoxTransferRobot";
            this.comboBoxTransferRobot.Size = new System.Drawing.Size(140, 28);
            this.comboBoxTransferRobot.TabIndex = 0;
            this.comboBoxTransferRobot.SelectedIndexChanged += new System.EventHandler(this.comboBoxTransferRobot_SelectedIndexChanged);
            // 
            // labelTransferRobotIP
            // 
            this.labelTransferRobotIP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.labelTransferRobotIP.AutoSize = true;
            this.labelTransferRobotIP.Location = new System.Drawing.Point(264, 19);
            this.labelTransferRobotIP.Name = "labelTransferRobotIP";
            this.labelTransferRobotIP.Size = new System.Drawing.Size(104, 17);
            this.labelTransferRobotIP.TabIndex = 2;
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("宋体", 11F);
            this.label4.Location = new System.Drawing.Point(155, 8);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(103, 38);
            this.label4.TabIndex = 1;
            this.label4.Text = "机器人IP信息：";
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("宋体", 11F);
            this.label6.Location = new System.Drawing.Point(155, 51);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(103, 38);
            this.label6.TabIndex = 18;
            this.label6.Text = "连接状态：";
            // 
            // labelTransferRobotConnectState
            // 
            this.labelTransferRobotConnectState.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.labelTransferRobotConnectState.AutoSize = true;
            this.labelTransferRobotConnectState.Location = new System.Drawing.Point(264, 62);
            this.labelTransferRobotConnectState.Name = "labelTransferRobotConnectState";
            this.labelTransferRobotConnectState.Size = new System.Drawing.Size(104, 17);
            this.labelTransferRobotConnectState.TabIndex = 3;
            // 
            // buttonTransferRobotConnect
            // 
            this.buttonTransferRobotConnect.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonTransferRobotConnect.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonTransferRobotConnect.Location = new System.Drawing.Point(155, 95);
            this.buttonTransferRobotConnect.Name = "buttonTransferRobotConnect";
            this.buttonTransferRobotConnect.Size = new System.Drawing.Size(103, 37);
            this.buttonTransferRobotConnect.TabIndex = 4;
            this.buttonTransferRobotConnect.Text = "连接";
            this.buttonTransferRobotConnect.UseVisualStyleBackColor = true;
            this.buttonTransferRobotConnect.Click += new System.EventHandler(this.buttonTransferRobotConnect_Click);
            // 
            // buttonTransferRobotDisconnect
            // 
            this.buttonTransferRobotDisconnect.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonTransferRobotDisconnect.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonTransferRobotDisconnect.Location = new System.Drawing.Point(264, 95);
            this.buttonTransferRobotDisconnect.Name = "buttonTransferRobotDisconnect";
            this.buttonTransferRobotDisconnect.Size = new System.Drawing.Size(104, 37);
            this.buttonTransferRobotDisconnect.TabIndex = 5;
            this.buttonTransferRobotDisconnect.Text = "断开";
            this.buttonTransferRobotDisconnect.UseVisualStyleBackColor = true;
            this.buttonTransferRobotDisconnect.Click += new System.EventHandler(this.buttonTransferRobotDisconnect_Click);
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("宋体", 11F);
            this.label8.Location = new System.Drawing.Point(155, 147);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(103, 19);
            this.label8.TabIndex = 6;
            this.label8.Text = "工位层：";
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("宋体", 11F);
            this.label7.Location = new System.Drawing.Point(155, 190);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(103, 19);
            this.label7.TabIndex = 7;
            this.label7.Text = "工位列:";
            // 
            // comboBoxTransferRobotRow
            // 
            this.comboBoxTransferRobotRow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxTransferRobotRow.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxTransferRobotRow.Font = new System.Drawing.Font("宋体", 11F);
            this.comboBoxTransferRobotRow.FormattingEnabled = true;
            this.comboBoxTransferRobotRow.Location = new System.Drawing.Point(264, 143);
            this.comboBoxTransferRobotRow.Name = "comboBoxTransferRobotRow";
            this.comboBoxTransferRobotRow.Size = new System.Drawing.Size(104, 26);
            this.comboBoxTransferRobotRow.TabIndex = 8;
            // 
            // comboBoxTransferRobotCol
            // 
            this.comboBoxTransferRobotCol.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxTransferRobotCol.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxTransferRobotCol.Font = new System.Drawing.Font("宋体", 11F);
            this.comboBoxTransferRobotCol.FormattingEnabled = true;
            this.comboBoxTransferRobotCol.Location = new System.Drawing.Point(264, 186);
            this.comboBoxTransferRobotCol.Name = "comboBoxTransferRobotCol";
            this.comboBoxTransferRobotCol.Size = new System.Drawing.Size(104, 26);
            this.comboBoxTransferRobotCol.TabIndex = 9;
            // 
            // buttonTransferRobotPickIn
            // 
            this.buttonTransferRobotPickIn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonTransferRobotPickIn.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonTransferRobotPickIn.Location = new System.Drawing.Point(155, 310);
            this.buttonTransferRobotPickIn.Name = "buttonTransferRobotPickIn";
            this.buttonTransferRobotPickIn.Size = new System.Drawing.Size(103, 37);
            this.buttonTransferRobotPickIn.TabIndex = 12;
            this.buttonTransferRobotPickIn.Text = "取进";
            this.buttonTransferRobotPickIn.UseVisualStyleBackColor = true;
            this.buttonTransferRobotPickIn.Click += new System.EventHandler(this.buttonTransferRobotPickIn_Click);
            // 
            // buttonTransferRobotMove
            // 
            this.buttonTransferRobotMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonTransferRobotMove.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonTransferRobotMove.Location = new System.Drawing.Point(264, 267);
            this.buttonTransferRobotMove.Name = "buttonTransferRobotMove";
            this.buttonTransferRobotMove.Size = new System.Drawing.Size(104, 37);
            this.buttonTransferRobotMove.TabIndex = 11;
            this.buttonTransferRobotMove.Text = "移动";
            this.buttonTransferRobotMove.UseVisualStyleBackColor = true;
            this.buttonTransferRobotMove.Click += new System.EventHandler(this.buttonTransferRobotMove_Click);
            // 
            // buttonTransferRobotPickOut
            // 
            this.buttonTransferRobotPickOut.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonTransferRobotPickOut.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonTransferRobotPickOut.Location = new System.Drawing.Point(264, 310);
            this.buttonTransferRobotPickOut.Name = "buttonTransferRobotPickOut";
            this.buttonTransferRobotPickOut.Size = new System.Drawing.Size(104, 37);
            this.buttonTransferRobotPickOut.TabIndex = 13;
            this.buttonTransferRobotPickOut.Text = "取出";
            this.buttonTransferRobotPickOut.UseVisualStyleBackColor = true;
            this.buttonTransferRobotPickOut.Click += new System.EventHandler(this.buttonTransferRobotPickOut_Click);
            // 
            // buttonTransferRobotPlaceIn
            // 
            this.buttonTransferRobotPlaceIn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonTransferRobotPlaceIn.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonTransferRobotPlaceIn.Location = new System.Drawing.Point(155, 353);
            this.buttonTransferRobotPlaceIn.Name = "buttonTransferRobotPlaceIn";
            this.buttonTransferRobotPlaceIn.Size = new System.Drawing.Size(103, 37);
            this.buttonTransferRobotPlaceIn.TabIndex = 16;
            this.buttonTransferRobotPlaceIn.Text = "放进";
            this.buttonTransferRobotPlaceIn.UseVisualStyleBackColor = true;
            this.buttonTransferRobotPlaceIn.Click += new System.EventHandler(this.buttonTransferRobotPlaceIn_Click);
            // 
            // buttonTransferRobotPlaceOut
            // 
            this.buttonTransferRobotPlaceOut.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonTransferRobotPlaceOut.Font = new System.Drawing.Font("宋体", 11F);
            this.buttonTransferRobotPlaceOut.Location = new System.Drawing.Point(264, 353);
            this.buttonTransferRobotPlaceOut.Name = "buttonTransferRobotPlaceOut";
            this.buttonTransferRobotPlaceOut.Size = new System.Drawing.Size(104, 37);
            this.buttonTransferRobotPlaceOut.TabIndex = 17;
            this.buttonTransferRobotPlaceOut.Text = "放出";
            this.buttonTransferRobotPlaceOut.UseVisualStyleBackColor = true;
            this.buttonTransferRobotPlaceOut.Click += new System.EventHandler(this.buttonTransferRobotPlaceOut_Click);
            // 
            // dataGridViewTransferStation
            // 
            this.dataGridViewTransferStation.BackgroundColor = System.Drawing.SystemColors.Control;
            this.dataGridViewTransferStation.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewTransferStation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewTransferStation.Location = new System.Drawing.Point(9, 52);
            this.dataGridViewTransferStation.Name = "dataGridViewTransferStation";
            this.tableLayoutPanelTransfer.SetRowSpan(this.dataGridViewTransferStation, 9);
            this.dataGridViewTransferStation.RowTemplate.Height = 27;
            this.dataGridViewTransferStation.Size = new System.Drawing.Size(140, 385);
            this.dataGridViewTransferStation.TabIndex = 19;
            this.dataGridViewTransferStation.SelectionChanged += new System.EventHandler(this.dataGridView1_SelectionChanged);
            // 
            // RobotPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 500);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("宋体", 10F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "RobotPage";
            this.Text = "RobotPage";
            this.Load += new System.EventHandler(this.RobotPage_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.tableLayoutPanelOnload.ResumeLayout(false);
            this.tableLayoutPanelOnload.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewOnloadStation)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.tableLayoutPanelTransfer.ResumeLayout(false);
            this.tableLayoutPanelTransfer.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewTransferStation)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelOnload;
        private System.Windows.Forms.ComboBox comboBoxOnloadRobot;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label labelOnloadRobotIP;
        private System.Windows.Forms.Label labelOnloadConnectState;
        private System.Windows.Forms.Button buttonOnloadRobotConnect;
        private System.Windows.Forms.Button buttonOnloadRobotDisconnect;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DataGridView dataGridViewOnloadStation;
        private System.Windows.Forms.ComboBox comboBoxOnloadRobotRow;
        private System.Windows.Forms.ComboBox comboBoxOnloadRobotCol;
        private System.Windows.Forms.Button buttonOnloadRobotMove;
        private System.Windows.Forms.Button buttonOnloadRobotDown;
        private System.Windows.Forms.Button buttonOnloadRobotUp;
        private System.Windows.Forms.Button buttonOnloadRobotHome;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelTransfer;
        private System.Windows.Forms.ComboBox comboBoxTransferRobot;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label labelTransferRobotIP;
        private System.Windows.Forms.Label labelTransferRobotConnectState;
        private System.Windows.Forms.Button buttonTransferRobotConnect;
        private System.Windows.Forms.Button buttonTransferRobotDisconnect;
        private System.Windows.Forms.Button buttonTransferRobotPickIn;
        private System.Windows.Forms.Button buttonTransferRobotPickOut;
        private System.Windows.Forms.Button buttonTransferRobotMove;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.DataGridView dataGridViewTransferStation;
        private System.Windows.Forms.ComboBox comboBoxTransferRobotCol;
        private System.Windows.Forms.ComboBox comboBoxTransferRobotRow;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button buttonTransferRobotPlaceIn;
        private System.Windows.Forms.Button buttonTransferRobotPlaceOut;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
    }
}