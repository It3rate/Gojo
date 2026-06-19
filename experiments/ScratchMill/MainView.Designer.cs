//namespace ScratchMill
//{
//  partial class MainView
//  {
//    /// <summary>
//    ///  Required designer variable.
//    /// </summary>
//    private System.ComponentModel.IContainer components = null;

//    /// <summary>
//    ///  Clean up any resources being used.
//    /// </summary>
//    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
//    protected override void Dispose(bool disposing)
//    {
//      if (disposing && (components != null))
//      {
//        components.Dispose();
//      }
//      base.Dispose(disposing);
//    }

//		#region Windows Form Designer generated code

//		/// <summary>
//		///  Required method for Designer support - do not modify
//		///  the contents of this method with the code editor.
//		/// </summary>
//		private void InitializeComponent()
//		{
//			corePanel = new Panel();
//			cbLayerPicker = new ComboBox();
//			cbRecuts = new CheckBox();
//			cbEdgeProbe = new CheckBox();
//			cbEdgeScratches = new CheckBox();
//			checkBox6 = new CheckBox();
//			cbDrill = new CheckBox();
//			cbMask = new CheckBox();
//			cbCleanup = new CheckBox();
//			cbTracks = new CheckBox();
//			cbAutolevel = new CheckBox();
//			cbProbe = new CheckBox();
//			btMask = new Button();
//			btDrill = new Button();
//			btRecut = new Button();
//			btEdgeProbe = new Button();
//			btEdgeCuts = new Button();
//			btCleanup = new Button();
//			btTracks = new Button();
//			btLastMetadata = new Button();
//			btOutline = new Button();
//			pnLeft = new Panel();
//			btClear = new Button();
//			btOutput = new Button();
//			txOutput = new TextBox();
//			btSettings = new Button();
//			btLoad = new Button();
//			btCopy = new Button();
//			btInfo = new Button();
//			btParams = new Button();
//			txInput = new TextBox();
//			pnRight = new Panel();
//			btTest = new Button();
//			lbSpeed = new Label();
//			lbSteps = new Label();
//			btUnlock = new Button();
//			btStop = new Button();
//			btRun = new Button();
//			btNormal = new Button();
//			btZeroAll = new Button();
//			btZeroX = new Button();
//			lbText = new Label();
//			btZeroY = new Button();
//			tbZPos = new TextBox();
//			btZeroZ = new Button();
//			tbYPos = new TextBox();
//			btX0Y0 = new Button();
//			txSpeed = new TextBox();
//			btRapid = new Button();
//			txStepsXY = new TextBox();
//			btHome = new Button();
//			btPrecise = new Button();
//			tbXPos = new TextBox();
//			btZPlus = new Button();
//			btZMinus = new Button();
//			btYMinus = new Button();
//			btYPlus = new Button();
//			btXPlus = new Button();
//			btXMinus = new Button();
//			btProbe = new Button();
//			btAutolevel = new Button();
//			corePanel.SuspendLayout();
//			pnLeft.SuspendLayout();
//			pnRight.SuspendLayout();
//			SuspendLayout();
//			// 
//			// corePanel
//			// 
//			corePanel.Controls.Add(cbLayerPicker);
//			corePanel.Controls.Add(cbRecuts);
//			corePanel.Controls.Add(cbEdgeProbe);
//			corePanel.Controls.Add(cbEdgeScratches);
//			corePanel.Controls.Add(checkBox6);
//			corePanel.Controls.Add(cbDrill);
//			corePanel.Controls.Add(cbMask);
//			corePanel.Controls.Add(cbCleanup);
//			corePanel.Controls.Add(cbTracks);
//			corePanel.Controls.Add(cbAutolevel);
//			corePanel.Controls.Add(cbProbe);
//			corePanel.Controls.Add(btMask);
//			corePanel.Controls.Add(btDrill);
//			corePanel.Controls.Add(btRecut);
//			corePanel.Controls.Add(btEdgeProbe);
//			corePanel.Controls.Add(btEdgeCuts);
//			corePanel.Controls.Add(btCleanup);
//			corePanel.Controls.Add(btTracks);
//			corePanel.Controls.Add(btLastMetadata);
//			corePanel.Controls.Add(btOutline);
//			corePanel.Controls.Add(pnLeft);
//			corePanel.Controls.Add(pnRight);
//			corePanel.Controls.Add(btProbe);
//			corePanel.Controls.Add(btAutolevel);
//			corePanel.Dock = DockStyle.Fill;
//			corePanel.Location = new Point(0, 0);
//			corePanel.Margin = new Padding(2);
//			corePanel.Name = "corePanel";
//			corePanel.Size = new Size(1098, 672);
//			corePanel.TabIndex = 0;
//			// 
//			// cbLayerPicker
//			// 
//			cbLayerPicker.Anchor = AnchorStyles.Top | AnchorStyles.Right;
//			cbLayerPicker.BackColor = Color.FromArgb(255, 255, 222);
//			cbLayerPicker.Font = new Font("Segoe UI", 9F);
//			cbLayerPicker.FormattingEnabled = true;
//			cbLayerPicker.Items.AddRange(new object[] { "Front", "Back" });
//			cbLayerPicker.Location = new Point(764, 1);
//			cbLayerPicker.Margin = new Padding(4, 2, 2, 2);
//			cbLayerPicker.Name = "cbLayerPicker";
//			cbLayerPicker.Size = new Size(90, 23);
//			cbLayerPicker.TabIndex = 17;
//			cbLayerPicker.Text = "Front";
//			cbLayerPicker.SelectedIndexChanged += cbLayerPicker_SelectedIndexChanged;
//			// 
//			// cbRecuts
//			// 
//			cbRecuts.Anchor = AnchorStyles.Top | AnchorStyles.Right;
//			cbRecuts.AutoSize = true;
//			cbRecuts.CheckAlign = ContentAlignment.MiddleRight;
//			cbRecuts.Checked = true;
//			cbRecuts.CheckState = CheckState.Checked;
//			cbRecuts.Location = new Point(742, 167);
//			cbRecuts.Name = "cbRecuts";
//			cbRecuts.Size = new Size(15, 14);
//			cbRecuts.TabIndex = 16;
//			cbRecuts.UseVisualStyleBackColor = true;
//			cbRecuts.CheckedChanged += OnCheckedChanged;
//			// 
//			// cbEdgeProbe
//			// 
//			cbEdgeProbe.Anchor = AnchorStyles.Top | AnchorStyles.Right;
//			cbEdgeProbe.AutoSize = true;
//			cbEdgeProbe.CheckAlign = ContentAlignment.MiddleRight;
//			cbEdgeProbe.Checked = true;
//			cbEdgeProbe.CheckState = CheckState.Checked;
//			cbEdgeProbe.Location = new Point(743, 57);
//			cbEdgeProbe.Name = "cbEdgeProbe";
//			cbEdgeProbe.Size = new Size(15, 14);
//			cbEdgeProbe.TabIndex = 16;
//			cbEdgeProbe.UseVisualStyleBackColor = true;
//			cbEdgeProbe.CheckedChanged += OnCheckedChanged;
//			// 
//			// cbEdgeScratches
//			// 
//			cbEdgeScratches.Anchor = AnchorStyles.Top | AnchorStyles.Right;
//			cbEdgeScratches.AutoSize = true;
//			cbEdgeScratches.CheckAlign = ContentAlignment.MiddleRight;
//			cbEdgeScratches.Checked = true;
//			cbEdgeScratches.CheckState = CheckState.Checked;
//			cbEdgeScratches.Location = new Point(743, 251);
//			cbEdgeScratches.Name = "cbEdgeScratches";
//			cbEdgeScratches.Size = new Size(15, 14);
//			cbEdgeScratches.TabIndex = 16;
//			cbEdgeScratches.UseVisualStyleBackColor = true;
//			cbEdgeScratches.CheckedChanged += OnCheckedChanged;
//			// 
//			// checkBox6
//			// 
//			checkBox6.Anchor = AnchorStyles.Top | AnchorStyles.Right;
//			checkBox6.AutoSize = true;
//			checkBox6.CheckAlign = ContentAlignment.MiddleRight;
//			checkBox6.Checked = true;
//			checkBox6.CheckState = CheckState.Checked;
//			checkBox6.Location = new Point(743, 251);
//			checkBox6.Name = "checkBox6";
//			checkBox6.Size = new Size(15, 14);
//			checkBox6.TabIndex = 16;
//			checkBox6.UseVisualStyleBackColor = true;
//			checkBox6.CheckedChanged += OnCheckedChanged;
//			// 
//			// cbDrill
//			// 
//			cbDrill.Anchor = AnchorStyles.Top | AnchorStyles.Right;
//			cbDrill.AutoSize = true;
//			cbDrill.CheckAlign = ContentAlignment.MiddleRight;
//			cbDrill.Checked = true;
//			cbDrill.CheckState = CheckState.Checked;
//			cbDrill.Location = new Point(743, 224);
//			cbDrill.Name = "cbDrill";
//			cbDrill.Size = new Size(15, 14);
//			cbDrill.TabIndex = 16;
//			cbDrill.UseVisualStyleBackColor = true;
//			cbDrill.CheckedChanged += OnCheckedChanged;
//			// 
//			// cbMask
//			// 
//			cbMask.Anchor = AnchorStyles.Top | AnchorStyles.Right;
//			cbMask.AutoSize = true;
//			cbMask.CheckAlign = ContentAlignment.MiddleRight;
//			cbMask.Checked = true;
//			cbMask.CheckState = CheckState.Checked;
//			cbMask.Location = new Point(743, 196);
//			cbMask.Name = "cbMask";
//			cbMask.Size = new Size(15, 14);
//			cbMask.TabIndex = 16;
//			cbMask.UseVisualStyleBackColor = true;
//			cbMask.CheckedChanged += OnCheckedChanged;
//			// 
//			// cbCleanup
//			// 
//			cbCleanup.Anchor = AnchorStyles.Top | AnchorStyles.Right;
//			cbCleanup.AutoSize = true;
//			cbCleanup.CheckAlign = ContentAlignment.MiddleRight;
//			cbCleanup.Checked = true;
//			cbCleanup.CheckState = CheckState.Checked;
//			cbCleanup.Location = new Point(743, 139);
//			cbCleanup.Name = "cbCleanup";
//			cbCleanup.Size = new Size(15, 14);
//			cbCleanup.TabIndex = 16;
//			cbCleanup.UseVisualStyleBackColor = true;
//			cbCleanup.CheckedChanged += OnCheckedChanged;
//			// 
//			// cbTracks
//			// 
//			cbTracks.Anchor = AnchorStyles.Top | AnchorStyles.Right;
//			cbTracks.AutoSize = true;
//			cbTracks.CheckAlign = ContentAlignment.MiddleRight;
//			cbTracks.Checked = true;
//			cbTracks.CheckState = CheckState.Checked;
//			cbTracks.Location = new Point(743, 113);
//			cbTracks.Name = "cbTracks";
//			cbTracks.Size = new Size(15, 14);
//			cbTracks.TabIndex = 16;
//			cbTracks.UseVisualStyleBackColor = true;
//			cbTracks.CheckedChanged += OnCheckedChanged;
//			// 
//			// cbAutolevel
//			// 
//			cbAutolevel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
//			cbAutolevel.AutoSize = true;
//			cbAutolevel.CheckAlign = ContentAlignment.MiddleRight;
//			cbAutolevel.Checked = true;
//			cbAutolevel.CheckState = CheckState.Checked;
//			cbAutolevel.Location = new Point(743, 85);
//			cbAutolevel.Name = "cbAutolevel";
//			cbAutolevel.Size = new Size(15, 14);
//			cbAutolevel.TabIndex = 16;
//			cbAutolevel.UseVisualStyleBackColor = true;
//			cbAutolevel.CheckedChanged += OnCheckedChanged;
//			// 
//			// cbProbe
//			// 
//			cbProbe.Anchor = AnchorStyles.Top | AnchorStyles.Right;
//			cbProbe.AutoSize = true;
//			cbProbe.CheckAlign = ContentAlignment.MiddleRight;
//			cbProbe.Checked = true;
//			cbProbe.CheckState = CheckState.Checked;
//			cbProbe.Location = new Point(743, 31);
//			cbProbe.Name = "cbProbe";
//			cbProbe.Size = new Size(15, 14);
//			cbProbe.TabIndex = 16;
//			cbProbe.UseVisualStyleBackColor = true;
//			cbProbe.CheckedChanged += OnCheckedChanged;
//			// 
//			// btMask
//			// 
//			btMask.Anchor = AnchorStyles.Top | AnchorStyles.Right;
//			btMask.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
//			btMask.Location = new Point(764, 191);
//			btMask.Name = "btMask";
//			btMask.Size = new Size(89, 22);
//			btMask.TabIndex = 15;
//			btMask.Text = "Mask";
//			btMask.UseVisualStyleBackColor = true;
//			btMask.Click += OnMaskRemoval;
//			// 
//			// btDrill
//			// 
//			btDrill.Anchor = AnchorStyles.Top | AnchorStyles.Right;
//			btDrill.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
//			btDrill.Location = new Point(764, 220);
//			btDrill.Name = "btDrill";
//			btDrill.Size = new Size(89, 22);
//			btDrill.TabIndex = 14;
//			btDrill.Text = "Drills";
//			btDrill.UseVisualStyleBackColor = true;
//			btDrill.Click += OnDrills;
//			// 
//			// btRecut
//			// 
//			btRecut.Anchor = AnchorStyles.Top | AnchorStyles.Right;
//			btRecut.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
//			btRecut.Location = new Point(764, 163);
//			btRecut.Name = "btRecut";
//			btRecut.Size = new Size(89, 22);
//			btRecut.TabIndex = 14;
//			btRecut.Text = "Recuts";
//			btRecut.UseVisualStyleBackColor = true;
//			btRecut.Click += OnRecuts;
//			// 
//			// btEdgeProbe
//			// 
//			btEdgeProbe.Anchor = AnchorStyles.Top | AnchorStyles.Right;
//			btEdgeProbe.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
//			btEdgeProbe.Location = new Point(764, 52);
//			btEdgeProbe.Name = "btEdgeProbe";
//			btEdgeProbe.Size = new Size(89, 22);
//			btEdgeProbe.TabIndex = 14;
//			btEdgeProbe.Text = "Edge Probe";
//			btEdgeProbe.UseVisualStyleBackColor = true;
//			btEdgeProbe.Click += OnEdgeProbe;
//			// 
//			// btEdgeCuts
//			// 
//			btEdgeCuts.Anchor = AnchorStyles.Top | AnchorStyles.Right;
//			btEdgeCuts.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
//			btEdgeCuts.Location = new Point(764, 247);
//			btEdgeCuts.Name = "btEdgeCuts";
//			btEdgeCuts.Size = new Size(89, 22);
//			btEdgeCuts.TabIndex = 14;
//			btEdgeCuts.Text = "Edge Cuts";
//			btEdgeCuts.UseVisualStyleBackColor = true;
//			btEdgeCuts.Click += OnEdgeCuts;
//			// 
//			// btCleanup
//			// 
//			btCleanup.Anchor = AnchorStyles.Top | AnchorStyles.Right;
//			btCleanup.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
//			btCleanup.Location = new Point(764, 134);
//			btCleanup.Name = "btCleanup";
//			btCleanup.Size = new Size(89, 22);
//			btCleanup.TabIndex = 14;
//			btCleanup.Text = "Cleanup";
//			btCleanup.UseVisualStyleBackColor = true;
//			btCleanup.Click += OnCleanup;
//			// 
//			// btTracks
//			// 
//			btTracks.Anchor = AnchorStyles.Top | AnchorStyles.Right;
//			btTracks.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
//			btTracks.Location = new Point(764, 109);
//			btTracks.Name = "btTracks";
//			btTracks.Size = new Size(89, 22);
//			btTracks.TabIndex = 14;
//			btTracks.Text = "Tracks";
//			btTracks.UseVisualStyleBackColor = true;
//			btTracks.Click += OnTracks;
//			// 
//			// btLastMetadata
//			// 
//			btLastMetadata.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
//			btLastMetadata.Location = new Point(246, 26);
//			btLastMetadata.Name = "btLastMetadata";
//			btLastMetadata.Size = new Size(89, 22);
//			btLastMetadata.TabIndex = 14;
//			btLastMetadata.Text = "Last Metadata";
//			btLastMetadata.UseVisualStyleBackColor = true;
//			btLastMetadata.Click += btLastMetadata_Click;
//			// 
//			// btOutline
//			// 
//			btOutline.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
//			btOutline.Location = new Point(246, 0);
//			btOutline.Name = "btOutline";
//			btOutline.Size = new Size(89, 22);
//			btOutline.TabIndex = 14;
//			btOutline.Text = "Scan Outline";
//			btOutline.UseVisualStyleBackColor = true;
//			btOutline.Click += btOutline_Click;
//			// 
//			// pnLeft
//			// 
//			pnLeft.BorderStyle = BorderStyle.FixedSingle;
//			pnLeft.Controls.Add(btClear);
//			pnLeft.Controls.Add(btOutput);
//			pnLeft.Controls.Add(txOutput);
//			pnLeft.Controls.Add(btSettings);
//			pnLeft.Controls.Add(btLoad);
//			pnLeft.Controls.Add(btCopy);
//			pnLeft.Controls.Add(btInfo);
//			pnLeft.Controls.Add(btParams);
//			pnLeft.Controls.Add(txInput);
//			pnLeft.Dock = DockStyle.Left;
//			pnLeft.Location = new Point(0, 0);
//			pnLeft.Name = "pnLeft";
//			pnLeft.Size = new Size(240, 672);
//			pnLeft.TabIndex = 13;
//			// 
//			// btClear
//			// 
//			btClear.Font = new Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
//			btClear.Location = new Point(185, 586);
//			btClear.Name = "btClear";
//			btClear.Size = new Size(41, 21);
//			btClear.TabIndex = 13;
//			btClear.Text = "Clear";
//			btClear.UseVisualStyleBackColor = true;
//			btClear.Click += btClear_Click;
//			// 
//			// btOutput
//			// 
//			btOutput.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
//			btOutput.Location = new Point(5, 637);
//			btOutput.Margin = new Padding(2);
//			btOutput.Name = "btOutput";
//			btOutput.Size = new Size(119, 24);
//			btOutput.TabIndex = 12;
//			btOutput.Text = "Output Folder";
//			btOutput.UseVisualStyleBackColor = true;
//			btOutput.Click += btOutput_Click;
//			// 
//			// txOutput
//			// 
//			txOutput.Location = new Point(5, 46);
//			txOutput.Multiline = true;
//			txOutput.Name = "txOutput";
//			txOutput.ScrollBars = ScrollBars.Vertical;
//			txOutput.Size = new Size(224, 505);
//			txOutput.TabIndex = 9;
//			// 
//			// btSettings
//			// 
//			btSettings.Location = new Point(5, 11);
//			btSettings.Name = "btSettings";
//			btSettings.Size = new Size(59, 29);
//			btSettings.TabIndex = 1;
//			btSettings.Text = "Settings";
//			btSettings.UseVisualStyleBackColor = true;
//			btSettings.Click += OnSettings;
//			// 
//			// btLoad
//			// 
//			btLoad.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
//			btLoad.Location = new Point(151, 637);
//			btLoad.Margin = new Padding(2);
//			btLoad.Name = "btLoad";
//			btLoad.Size = new Size(78, 24);
//			btLoad.TabIndex = 11;
//			btLoad.Text = "Load";
//			btLoad.UseVisualStyleBackColor = true;
//			btLoad.Click += btLoad_Click;
//			// 
//			// btCopy
//			// 
//			btCopy.Font = new Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
//			btCopy.Location = new Point(5, 586);
//			btCopy.Name = "btCopy";
//			btCopy.Size = new Size(44, 21);
//			btCopy.TabIndex = 1;
//			btCopy.Text = "Copy";
//			btCopy.UseVisualStyleBackColor = true;
//			btCopy.Click += OnCopyOutput;
//			// 
//			// btInfo
//			// 
//			btInfo.Location = new Point(135, 11);
//			btInfo.Name = "btInfo";
//			btInfo.Size = new Size(44, 29);
//			btInfo.TabIndex = 1;
//			btInfo.Text = "Info";
//			btInfo.UseVisualStyleBackColor = true;
//			btInfo.Click += OnInfo;
//			// 
//			// btParams
//			// 
//			btParams.Location = new Point(70, 11);
//			btParams.Name = "btParams";
//			btParams.Size = new Size(59, 29);
//			btParams.TabIndex = 1;
//			btParams.Text = "Params";
//			btParams.UseVisualStyleBackColor = true;
//			btParams.Click += OnParameters;
//			// 
//			// txInput
//			// 
//			txInput.AcceptsReturn = true;
//			txInput.Location = new Point(5, 557);
//			txInput.Multiline = true;
//			txInput.Name = "txInput";
//			txInput.PlaceholderText = "Console Input";
//			txInput.Size = new Size(224, 23);
//			txInput.TabIndex = 10;
//			txInput.WordWrap = false;
//			txInput.Enter += OnTxInputFocus;
//			txInput.KeyDown += OnInputKeyDown;
//			txInput.Leave += OnTxInputLeave;
//			// 
//			// pnRight
//			// 
//			pnRight.BorderStyle = BorderStyle.FixedSingle;
//			pnRight.Controls.Add(btTest);
//			pnRight.Controls.Add(lbSpeed);
//			pnRight.Controls.Add(lbSteps);
//			pnRight.Controls.Add(btUnlock);
//			pnRight.Controls.Add(btStop);
//			pnRight.Controls.Add(btRun);
//			pnRight.Controls.Add(btNormal);
//			pnRight.Controls.Add(btZeroAll);
//			pnRight.Controls.Add(btZeroX);
//			pnRight.Controls.Add(lbText);
//			pnRight.Controls.Add(btZeroY);
//			pnRight.Controls.Add(tbZPos);
//			pnRight.Controls.Add(btZeroZ);
//			pnRight.Controls.Add(tbYPos);
//			pnRight.Controls.Add(btX0Y0);
//			pnRight.Controls.Add(txSpeed);
//			pnRight.Controls.Add(btRapid);
//			pnRight.Controls.Add(txStepsXY);
//			pnRight.Controls.Add(btHome);
//			pnRight.Controls.Add(btPrecise);
//			pnRight.Controls.Add(tbXPos);
//			pnRight.Controls.Add(btZPlus);
//			pnRight.Controls.Add(btZMinus);
//			pnRight.Controls.Add(btYMinus);
//			pnRight.Controls.Add(btYPlus);
//			pnRight.Controls.Add(btXPlus);
//			pnRight.Controls.Add(btXMinus);
//			pnRight.Dock = DockStyle.Right;
//			pnRight.Location = new Point(858, 0);
//			pnRight.Name = "pnRight";
//			pnRight.Size = new Size(240, 672);
//			pnRight.TabIndex = 12;
//			// 
//			// btTest
//			// 
//			btTest.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
//			btTest.Location = new Point(2, 642);
//			btTest.Margin = new Padding(2);
//			btTest.Name = "btTest";
//			btTest.Size = new Size(86, 24);
//			btTest.TabIndex = 13;
//			btTest.Text = "Test Patterns";
//			btTest.UseVisualStyleBackColor = true;
//			btTest.Click += btTest_Click;
//			// 
//			// lbSpeed
//			// 
//			lbSpeed.AutoSize = true;
//			lbSpeed.Location = new Point(121, 413);
//			lbSpeed.Name = "lbSpeed";
//			lbSpeed.Size = new Size(39, 15);
//			lbSpeed.TabIndex = 12;
//			lbSpeed.Text = "Speed";
//			// 
//			// lbSteps
//			// 
//			lbSteps.AutoSize = true;
//			lbSteps.Location = new Point(16, 413);
//			lbSteps.Name = "lbSteps";
//			lbSteps.Size = new Size(35, 15);
//			lbSteps.TabIndex = 12;
//			lbSteps.Text = "Steps";
//			// 
//			// btUnlock
//			// 
//			btUnlock.Location = new Point(24, 46);
//			btUnlock.Name = "btUnlock";
//			btUnlock.Size = new Size(75, 23);
//			btUnlock.TabIndex = 11;
//			btUnlock.Text = "Unlock";
//			btUnlock.UseVisualStyleBackColor = true;
//			btUnlock.Click += btUnlock_Click;
//			// 
//			// btStop
//			// 
//			btStop.BackColor = Color.FromArgb(255, 192, 192);
//			btStop.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
//			btStop.Location = new Point(130, 11);
//			btStop.Name = "btStop";
//			btStop.Size = new Size(97, 86);
//			btStop.TabIndex = 10;
//			btStop.Text = "STOP";
//			btStop.UseVisualStyleBackColor = false;
//			btStop.Click += OnStop;
//			// 
//			// btRun
//			// 
//			btRun.BackColor = Color.FromArgb(192, 255, 192);
//			btRun.Location = new Point(24, 11);
//			btRun.Name = "btRun";
//			btRun.Size = new Size(75, 23);
//			btRun.TabIndex = 9;
//			btRun.Text = "RUN";
//			btRun.UseVisualStyleBackColor = false;
//			btRun.Click += btRun_Click;
//			// 
//			// btNormal
//			// 
//			btNormal.Location = new Point(168, 316);
//			btNormal.Name = "btNormal";
//			btNormal.Size = new Size(59, 29);
//			btNormal.TabIndex = 1;
//			btNormal.Text = "Normal";
//			btNormal.UseVisualStyleBackColor = true;
//			btNormal.Click += OnNormal;
//			// 
//			// btZeroAll
//			// 
//			btZeroAll.Location = new Point(160, 140);
//			btZeroAll.Name = "btZeroAll";
//			btZeroAll.Size = new Size(67, 29);
//			btZeroAll.TabIndex = 1;
//			btZeroAll.Text = "Zero All";
//			btZeroAll.UseVisualStyleBackColor = true;
//			btZeroAll.Click += OnZeroAll;
//			// 
//			// btZeroX
//			// 
//			btZeroX.Location = new Point(24, 128);
//			btZeroX.Name = "btZeroX";
//			btZeroX.Size = new Size(48, 29);
//			btZeroX.TabIndex = 1;
//			btZeroX.Text = "ZeroX";
//			btZeroX.UseVisualStyleBackColor = true;
//			btZeroX.Click += OnZeroX;
//			// 
//			// lbText
//			// 
//			lbText.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
//			lbText.Location = new Point(16, 643);
//			lbText.Margin = new Padding(2, 0, 2, 0);
//			lbText.Name = "lbText";
//			lbText.Size = new Size(217, 23);
//			lbText.TabIndex = 0;
//			lbText.Text = "Status";
//			lbText.TextAlign = ContentAlignment.TopRight;
//			// 
//			// btZeroY
//			// 
//			btZeroY.Location = new Point(24, 163);
//			btZeroY.Name = "btZeroY";
//			btZeroY.Size = new Size(48, 29);
//			btZeroY.TabIndex = 1;
//			btZeroY.Text = "ZeroY";
//			btZeroY.UseVisualStyleBackColor = true;
//			btZeroY.Click += OnZeroY;
//			// 
//			// tbZPos
//			// 
//			tbZPos.AcceptsReturn = true;
//			tbZPos.BackColor = Color.Ivory;
//			tbZPos.BorderStyle = BorderStyle.None;
//			tbZPos.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold);
//			tbZPos.ForeColor = Color.RoyalBlue;
//			tbZPos.Location = new Point(83, 203);
//			tbZPos.Margin = new Padding(8);
//			tbZPos.Name = "tbZPos";
//			tbZPos.Size = new Size(61, 19);
//			tbZPos.TabIndex = 8;
//			tbZPos.Text = "0.00";
//			tbZPos.TextAlign = HorizontalAlignment.Right;
//			// 
//			// btZeroZ
//			// 
//			btZeroZ.Location = new Point(24, 198);
//			btZeroZ.Name = "btZeroZ";
//			btZeroZ.Size = new Size(48, 29);
//			btZeroZ.TabIndex = 1;
//			btZeroZ.Text = "ZeroZ";
//			btZeroZ.UseVisualStyleBackColor = true;
//			btZeroZ.Click += OnZeroZ;
//			// 
//			// tbYPos
//			// 
//			tbYPos.AcceptsReturn = true;
//			tbYPos.BackColor = Color.Ivory;
//			tbYPos.BorderStyle = BorderStyle.None;
//			tbYPos.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold);
//			tbYPos.ForeColor = Color.RoyalBlue;
//			tbYPos.Location = new Point(83, 169);
//			tbYPos.Margin = new Padding(8);
//			tbYPos.Name = "tbYPos";
//			tbYPos.Size = new Size(61, 19);
//			tbYPos.TabIndex = 8;
//			tbYPos.Text = "0.00";
//			tbYPos.TextAlign = HorizontalAlignment.Right;
//			// 
//			// btX0Y0
//			// 
//			btX0Y0.Location = new Point(160, 175);
//			btX0Y0.Name = "btX0Y0";
//			btX0Y0.Size = new Size(67, 29);
//			btX0Y0.TabIndex = 1;
//			btX0Y0.Text = "Go X0Y0";
//			btX0Y0.UseVisualStyleBackColor = true;
//			btX0Y0.Click += OnMoveToXYZero;
//			// 
//			// txSpeed
//			// 
//			txSpeed.AcceptsReturn = true;
//			txSpeed.BackColor = Color.Ivory;
//			txSpeed.BorderStyle = BorderStyle.None;
//			txSpeed.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold);
//			txSpeed.ForeColor = Color.RoyalBlue;
//			txSpeed.Location = new Point(166, 409);
//			txSpeed.Margin = new Padding(8);
//			txSpeed.Name = "txSpeed";
//			txSpeed.Size = new Size(56, 19);
//			txSpeed.TabIndex = 8;
//			txSpeed.Text = "1000";
//			txSpeed.TextAlign = HorizontalAlignment.Right;
//			txSpeed.TextChanged += OnSpeedChanged;
//			// 
//			// btRapid
//			// 
//			btRapid.Location = new Point(168, 281);
//			btRapid.Name = "btRapid";
//			btRapid.Size = new Size(59, 29);
//			btRapid.TabIndex = 1;
//			btRapid.Text = "Rapid";
//			btRapid.UseVisualStyleBackColor = true;
//			btRapid.Click += OnRapid;
//			// 
//			// txStepsXY
//			// 
//			txStepsXY.AcceptsReturn = true;
//			txStepsXY.BackColor = Color.Ivory;
//			txStepsXY.BorderStyle = BorderStyle.None;
//			txStepsXY.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold);
//			txStepsXY.ForeColor = Color.RoyalBlue;
//			txStepsXY.Location = new Point(54, 409);
//			txStepsXY.Margin = new Padding(8);
//			txStepsXY.Name = "txStepsXY";
//			txStepsXY.Size = new Size(45, 19);
//			txStepsXY.TabIndex = 8;
//			txStepsXY.Text = "2";
//			txStepsXY.TextAlign = HorizontalAlignment.Right;
//			// 
//			// btHome
//			// 
//			btHome.Anchor = AnchorStyles.Top | AnchorStyles.Right;
//			btHome.Location = new Point(24, 75);
//			btHome.Name = "btHome";
//			btHome.Size = new Size(75, 22);
//			btHome.TabIndex = 1;
//			btHome.Text = "Home";
//			btHome.UseVisualStyleBackColor = true;
//			btHome.Click += OnHome;
//			// 
//			// btPrecise
//			// 
//			btPrecise.Location = new Point(168, 353);
//			btPrecise.Name = "btPrecise";
//			btPrecise.Size = new Size(59, 29);
//			btPrecise.TabIndex = 1;
//			btPrecise.Text = "Precise";
//			btPrecise.UseVisualStyleBackColor = true;
//			btPrecise.Click += OnPrecise;
//			// 
//			// tbXPos
//			// 
//			tbXPos.AcceptsReturn = true;
//			tbXPos.BackColor = Color.Ivory;
//			tbXPos.BorderStyle = BorderStyle.None;
//			tbXPos.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold);
//			tbXPos.ForeColor = Color.RoyalBlue;
//			tbXPos.Location = new Point(83, 133);
//			tbXPos.Margin = new Padding(8);
//			tbXPos.Name = "tbXPos";
//			tbXPos.Size = new Size(61, 19);
//			tbXPos.TabIndex = 8;
//			tbXPos.Text = "0.00";
//			tbXPos.TextAlign = HorizontalAlignment.Right;
//			// 
//			// btZPlus
//			// 
//			btZPlus.Location = new Point(121, 281);
//			btZPlus.Name = "btZPlus";
//			btZPlus.Size = new Size(35, 50);
//			btZPlus.TabIndex = 7;
//			btZPlus.Text = "+Z";
//			btZPlus.UseVisualStyleBackColor = true;
//			btZPlus.Click += OnJogZPlus;
//			// 
//			// btZMinus
//			// 
//			btZMinus.Location = new Point(121, 337);
//			btZMinus.Name = "btZMinus";
//			btZMinus.Size = new Size(35, 45);
//			btZMinus.TabIndex = 6;
//			btZMinus.Text = "-Z";
//			btZMinus.UseVisualStyleBackColor = true;
//			btZMinus.Click += OnJogZMinus;
//			// 
//			// btYMinus
//			// 
//			btYMinus.Location = new Point(40, 351);
//			btYMinus.Name = "btYMinus";
//			btYMinus.Size = new Size(48, 31);
//			btYMinus.TabIndex = 5;
//			btYMinus.Text = "-Y";
//			btYMinus.UseVisualStyleBackColor = true;
//			btYMinus.Click += OnJogYMinus;
//			// 
//			// btYPlus
//			// 
//			btYPlus.Location = new Point(40, 281);
//			btYPlus.Name = "btYPlus";
//			btYPlus.Size = new Size(48, 31);
//			btYPlus.TabIndex = 2;
//			btYPlus.Text = "+Y";
//			btYPlus.UseVisualStyleBackColor = true;
//			btYPlus.Click += OnJogYPlus;
//			// 
//			// btXPlus
//			// 
//			btXPlus.Location = new Point(67, 316);
//			btXPlus.Name = "btXPlus";
//			btXPlus.Size = new Size(48, 31);
//			btXPlus.TabIndex = 4;
//			btXPlus.Text = "+X";
//			btXPlus.UseVisualStyleBackColor = true;
//			btXPlus.Click += OnJogXPlus;
//			// 
//			// btXMinus
//			// 
//			btXMinus.Location = new Point(14, 316);
//			btXMinus.Name = "btXMinus";
//			btXMinus.Size = new Size(48, 31);
//			btXMinus.TabIndex = 3;
//			btXMinus.Text = "-X";
//			btXMinus.UseVisualStyleBackColor = true;
//			btXMinus.Click += OnJogXMinus;
//			// 
//			// btProbe
//			// 
//			btProbe.Anchor = AnchorStyles.Top | AnchorStyles.Right;
//			btProbe.Location = new Point(764, 26);
//			btProbe.Name = "btProbe";
//			btProbe.Size = new Size(89, 22);
//			btProbe.TabIndex = 1;
//			btProbe.Text = "Probe";
//			btProbe.UseVisualStyleBackColor = true;
//			btProbe.Click += OnProbe;
//			// 
//			// btAutolevel
//			// 
//			btAutolevel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
//			btAutolevel.Location = new Point(764, 80);
//			btAutolevel.Name = "btAutolevel";
//			btAutolevel.Size = new Size(89, 22);
//			btAutolevel.TabIndex = 1;
//			btAutolevel.Text = "AutoLevel";
//			btAutolevel.UseVisualStyleBackColor = true;
//			btAutolevel.Click += OnAutolevel;
//			// 
//			// MainView
//			// 
//			AutoScaleDimensions = new SizeF(7F, 15F);
//			AutoScaleMode = AutoScaleMode.Font;
//			ClientSize = new Size(1098, 672);
//			Controls.Add(corePanel);
//			Margin = new Padding(2);
//			MinimumSize = new Size(996, 631);
//			Name = "MainView";
//			Text = "Scratch Miller";
//			corePanel.ResumeLayout(false);
//			corePanel.PerformLayout();
//			pnLeft.ResumeLayout(false);
//			pnLeft.PerformLayout();
//			pnRight.ResumeLayout(false);
//			pnRight.PerformLayout();
//			ResumeLayout(false);
//		}

//		#endregion

//		private Panel corePanel;
//		private Label lbText;
//		private Button btZPlus;
//		private Button btZMinus;
//		private Button btYMinus;
//		private Button btXPlus;
//		private Button btXMinus;
//		private Button btYPlus;
//		private Button btZeroAll;
//		private Button btInfo;
//		private Button btParams;
//		private Button btSettings;
//		private Button btHome;
//		private Button btProbe;
//		private Button btAutolevel;
//		private Button btPrecise;
//		private Button btNormal;
//		private Button btRapid;
//		private Button btX0Y0;
//		private TextBox tbZPos;
//		private TextBox tbYPos;
//		private TextBox tbXPos;
//		private Button btZeroZ;
//		private Button btZeroY;
//		private Button btZeroX;
//		private TextBox txInput;
//		private TextBox txOutput;
//		private Button btCopy;
//		private TextBox txSpeed;
//		private TextBox txStepsXY;
//        private Button btLoad;
//		private Button btOutput;
//		private Button btStop;
//		private Button btRun;
//		private Button btUnlock;
//		private Label lbSteps;
//		private Button btOutline;
//		private Label lbSpeed;
//		private Button btDrill;
//		private Button btEdgeCuts;
//		private Button btCleanup;
//		private Button btTracks;
//		private Button btLastMetadata;
//		private Button btMask;
//		private CheckBox cbProbe;
//		private CheckBox cbEdgeScratches;
//		private CheckBox checkBox6;
//		private CheckBox cbDrill;
//		private CheckBox cbMask;
//		private CheckBox cbCleanup;
//		private CheckBox cbTracks;
//		private CheckBox cbAutolevel;
//        private ComboBox cbLayerPicker;
//		private Button btClear;
//        private Button btRecut;
//        private CheckBox cbRecuts;
//        private CheckBox cbEdgeProbe;
//        private Button btEdgeProbe;
//		internal Panel pnRight;
//		internal Panel pnLeft;
//        private Button btTest;
//    }
//}
