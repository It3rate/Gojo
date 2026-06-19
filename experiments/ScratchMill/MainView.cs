//using MathDemo;
//using ScratchMill.Comm;
//using ScratchMill.GCode;
//using ScratchMill.KiCad;
//using ScratchMill.Machine;
//using ScratchMill.Macros;
//using ScratchMill.Properties;
//using ScratchMill.Utils;
//using SkiaSharp.Views.Desktop;
//using System.Diagnostics;
//using System.Text.RegularExpressions;
//using static System.Reflection.Metadata.BlobBuilder;

namespace ScratchMill;

public partial class MainView //: Form
{
//	private readonly Slides _demos;
//	private readonly CoreRenderer _renderer;
//	private readonly SKControl _control;
//	private readonly MouseAgent _mouseAgent;
//	private Runner _runner;
//	private Workspace _workspace;
//	private SKWorkspaceMapper? _workspaceMapper;
//	public GrblComm Comm => _demos.Comm;
//	public ScratchJobs Jobs => _demos.Jobs;

//	public static string _lowResolution = "0.#";
	public static MainView? Instance { get; private set; }
//	public MainView()
//	{
//		InitializeComponent();
//		Settings.Default.SettingsLoaded += Default_SettingsLoaded;
//		Instance = this;
//		DoubleBuffered = true;
//		KeyPreview = true;
//		_renderer = CoreRenderer.Instance;

//		_control = new SKControl();
//		_control.Width = Width;
//		_control.Height = Height;
//		_control.Dock = DockStyle.Fill;
//		_renderer.Width = Width;
//		_renderer.Height = Height;
//		corePanel.Controls.Add(_control);

//		_control.PaintSurface += DrawOnPaintSurface;
//		_control.MouseDown += OnMouseDown;
//		_control.MouseMove += OnMouseMove;
//		_control.MouseUp += OnMouseUp;
//		_control.MouseDoubleClick += OnMouseDoubleClick;
//		_control.MouseWheel += OnMouseWheel;
//		_control.PreviewKeyDown += OnPreviewKeyDown;

//		KeyDown += OnKeyDown;
//		KeyUp += OnKeyUp;
//		_workspace = new Workspace(Brain.ActiveBrain);


//		var comm = new GrblComm();
//		_demos = new Slides(_workspace.Brain, comm);

//		_mouseAgent = new MouseAgent(_workspace, _renderer, _demos);
//		_runner = _mouseAgent.Runner;
//		_runner.OnContextStringChanged += _runner_OnContextStringChanged;
//		_ = Execute(null, 50);

//		comm.MessageReceivedEvent += OnMessageReceived;
//		comm.UpdateStateEvent += OnUpdateState;
//		comm.Unlock();

//		var lastPath = Settings.Default.lastPath;
//		Trace.WriteLine(lastPath);
//	}

//	private void MainView_Move(object? sender, EventArgs e)
//	{
//		Settings.Default.windowPosition = this.Location;
//		Settings.Default.Save();
//	}

//	private void MainView_Resize(object? sender, EventArgs e)
//	{
//		Settings.Default.windowSize = this.Size;
//		Settings.Default.Save();
//	}

//	private void Default_SettingsLoaded(object sender, System.Configuration.SettingsLoadedEventArgs e)
//	{
//		if (Settings.Default.windowSize != Size.Empty)
//		{
//			Size = Settings.Default.windowSize;
//		}
//		if (Settings.Default.windowPosition != Point.Empty)
//		{
//			Location = Settings.Default.windowPosition;
//		}
//		Settings.Default.SettingsLoaded -= Default_SettingsLoaded;
//		Resize += MainView_Resize;
//		Move += MainView_Move;
//	}

//	private void OnUpdateState(object? sender, EventArgs e)
//	{
//		AppendSingleLineSafe(tbXPos, Comm.GrblState.XPos.ToString(_lowResolution));
//		AppendSingleLineSafe(tbYPos, Comm.GrblState.YPos.ToString(_lowResolution));
//		AppendSingleLineSafe(tbZPos, Comm.GrblState.ZPos.ToString("0.##"));
//	}

//	private void _runner_OnContextStringChanged(object? sender, EventArgs e)
//	{
//		lbText.Text = _runner.EquationText;
//	}

//	private void DrawOnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
//	{
//		_renderer.DrawOnCanvas(e.Surface.Canvas);
//	}
//	public async Task Execute(Action? action, int timeoutInMilliseconds)
//	{
//		await Task.Delay(timeoutInMilliseconds);
//		ReloadTest();
//		NeedsUpdate();
//	}
//	public void PreviousTest()
//	{
//		_runner.HasUpdated = false;
//		_workspaceMapper = _demos.PreviousTest(_mouseAgent);
//		_runner.HasUpdated = true;
//	}
//	public void ReloadTest()
//	{
//		_runner.HasUpdated = false;
//		_workspaceMapper = _demos.Reload(_mouseAgent);
//		_runner.HasUpdated = true;
//	}
//	public void NextTest()
//	{
//		_runner.HasUpdated = false;
//		_workspaceMapper = _demos.NextTest(_mouseAgent);
//		_runner.HasUpdated = true;
//	}


//	private void OnMouseDown(object? sender, MouseEventArgs e) { if (_mouseAgent.MouseDown(e.ToMouseArgs())) { NeedsUpdate(); } }
//	private void OnMouseMove(object? sender, MouseEventArgs e) { if (_mouseAgent.MouseMove(e.ToMouseArgs())) { NeedsUpdate(); } }
//	private void OnMouseUp(object? sender, MouseEventArgs e) { if (_mouseAgent.MouseUp(e.ToMouseArgs())) { NeedsUpdate(); } }
//	private void OnMouseDoubleClick(object? sender, MouseEventArgs e) { if (_mouseAgent.MouseDoubleClick(e.ToMouseArgs())) { NeedsUpdate(); } }
//	private void OnMouseWheel(object? sender, MouseEventArgs e) { if (_mouseAgent.MouseWheel(e.ToMouseArgs())) { NeedsUpdate(); } }


//	private void OnPreviewKeyDown(object? sender, PreviewKeyDownEventArgs e)
//	{
//		if (!_mouseAgent.HasFocus) return;

//		if (e.KeyData == System.Windows.Forms.Keys.Right ||
//			e.KeyData == System.Windows.Forms.Keys.Left ||
//			e.KeyData == System.Windows.Forms.Keys.Down ||
//			e.KeyData == System.Windows.Forms.Keys.Up) // todo: can't figure out why arrow keys aren't passed, passing manually for now but may lead to double invoke.
//		{
//			var ea = new KeyEventArgs(e.KeyData);
//			if (_mouseAgent.KeyDown(ea.ToKeyArgs())) { NeedsUpdate(); }
//		}
//	}
//	private void OnKeyDown(object? sender, KeyEventArgs e)
//	{
//		if (!_mouseAgent.HasFocus) return;

//		if (_mouseAgent.KeyDown(e.ToKeyArgs())) { NeedsUpdate(); }
//		e.SuppressKeyPress = true; // ### Don't do this if eventually using menus etc. This supresses the alt'n sound the system gives thinking it can't find a menu item.
//	}
//	private void OnKeyUp(object? sender, KeyEventArgs e)
//	{
//		if (!_mouseAgent.HasFocus) return;
//		if (_mouseAgent == null || _mouseAgent.KeyUp(e.ToKeyArgs()))
//		{
//			NeedsUpdate();
//		}
//	}

//	public void NeedsUpdate()
//	{
//		_control.Invalidate();
//		_runner.NeedsUpdate();
//	}

//	float step = 5;
//	float zstep = 2;
//	MotionKind curMotion = MotionKind.Normal;
//	int rateRapid = 4000;
//	int rateNormal = 2000;
//	int ratePrecise = 500;
//	int rate = 2000;
//	private void OnRapid(object sender, EventArgs e)
//	{
//		curMotion = MotionKind.Rapid;
//		step = 15f;
//		zstep = 10;
//		rate = rateRapid;
//		txStepsXY.Text = step.ToString("0.#");
//		txSpeed.Text = rate.ToString();
//	}
//	private void OnNormal(object sender, EventArgs e)
//	{
//		curMotion = MotionKind.Normal;
//		step = 2;
//		zstep = 1;
//		rate = rateNormal;
//		txStepsXY.Text = step.ToString("0.#");
//		txSpeed.Text = rate.ToString();
//	}
//	private void OnPrecise(object sender, EventArgs e)
//	{
//		curMotion = MotionKind.Precise;
//		step = .5f;
//		zstep = .05f;
//		rate = ratePrecise;
//		txStepsXY.Text = step.ToString("0.###");
//		txSpeed.Text = rate.ToString();
//	}
//	private async void OnJogXPlus(object sender, EventArgs e)
//	{
//		await Comm.Jog("X", step, rate);
//	}
//	private async void OnJogXMinus(object sender, EventArgs e)
//	{
//		await Comm.Jog("X", -step, rate);
//	}
//	private async void OnJogYPlus(object sender, EventArgs e)
//	{
//		await Comm.Jog("Y", step, rate);
//	}
//	private async void OnJogYMinus(object sender, EventArgs e)
//	{
//		await Comm.Jog("Y", -step, rate);
//	}
//	private async void OnJogZPlus(object sender, EventArgs e)
//	{
//		await Comm.Jog("Z", zstep, rate);
//	}
//	private async void OnJogZMinus(object sender, EventArgs e)
//	{
//		await Comm.Jog("Z", -zstep, rate);
//	}

//	private async void OnMoveToXYZero(object sender, EventArgs e)
//	{
//		await Comm.MoveToX0Y0Z4(rate);
//	}
//	private async void OnHome(object sender, EventArgs e)
//	{
//		await Comm.HomeTask();
//	}

//	private async void OnSettings(object sender, EventArgs e)
//	{
//		await Comm.GetSettings();
//	}

//	private async void OnParameters(object sender, EventArgs e)
//	{
//		await Comm.GetParameters();
//	}

//	private async void OnInfo(object sender, EventArgs e)
//	{
//		_prevMsg = "";
//		await Comm.GetInfoUser();
//	}

//	public void SetLayer(BoardSide side) => BoardSide = side;
//	public BoardSide BoardSide
//	{
//		get => Jobs.Side;
//		set
//		{
//			if (value != Jobs.Side)
//			{
//				Jobs.Side = value;
//				_demos.ParsePath(Slides.UserPath);
//				NeedsUpdate();
//			}
//		}
//	}
//	private async void OnProbe(object sender, EventArgs e)
//	{
//		await _demos.Sender.RunProbeAndZero();
//	}
//	private async void OnEdgeProbe(object sender, EventArgs e)
//	{
//		await _demos.Sender.RunEdgeProbe();
//		NeedsUpdate();
//	}
//	private async void OnAutolevel(object sender, EventArgs e)
//	{
//		if (_demos.GCodeWriter != null)
//		{
//			await _demos.Sender.RunAutolevel();
//		}
//	}
//	private async void OnTracks(object sender, EventArgs e)
//	{
//		if (_demos.GCodeWriter != null)
//		{
//			await _demos.Sender.RunGCodeForStateAndCurrentLayer(JobState.TopTracks); // autoconverts layer
//		}
//	}
//	private async void OnCleanup(object sender, EventArgs e)
//	{
//		if (_demos.GCodeWriter != null)
//		{
//			await _demos.Sender.RunGCodeForStateAndCurrentLayer(JobState.TopCleanup);
//		}
//	}
//	private async void OnRecuts(object sender, EventArgs e)
//	{
//		if (_demos.GCodeWriter != null)
//		{
//			await _demos.Sender.RunGCodeForStateAndCurrentLayer(JobState.TopContinuity);
//			await _demos.Sender.RunGCodeForStateAndCurrentLayer(JobState.TopRecut);
//		}
//	}
//	private async void OnMaskRemoval(object sender, EventArgs e)
//	{
//		if (_demos.GCodeWriter != null)
//		{
//			await _demos.Sender.RunGCodeForStateAndCurrentLayer(JobState.TopMaskRemoval);
//		}
//	}
//	private async void OnDrills(object sender, EventArgs e)
//	{
//		if (_demos.GCodeWriter != null)
//		{
//			await _demos.Sender.RunGCodeForStateAndCurrentLayer(JobState.TopDrills);
//		}
//	}
//	private async void OnEdgeCuts(object sender, EventArgs e)
//	{
//		if (_demos.GCodeWriter != null)
//		{
//			await _demos.Sender.RunGCodeForStateAndCurrentLayer(JobState.TopEdgeScratches);
//		}
//	}


//	private void OnMessageSent(object sender, string msg)
//	{
//		AppendMultilineSafe(txOutput, msg);
//	}
//	private void OnMessageReceived(object sender, string msg)
//	{
//		AppendMultilineSafe(txOutput, msg);
//		_lastMessageReceived = msg;
//	}
//	private void AppendSingleLineSafe(TextBox tb, string msg)
//	{
//		if (tb.InvokeRequired)
//		{
//			tb.BeginInvoke((MethodInvoker)delegate { tb.Text = msg; });
//		}
//		else
//		{
//			tb.Text = msg;
//		}
//	}

//	string _lastMessageReceived = "";
//	private void AppendMultilineSafe(TextBox tb, string msg)
//	{
//		if (_lastMessageReceived.ToUpperInvariant().StartsWith("<ALARM") && msg.ToUpperInvariant().StartsWith("<ALARM"))
//		{
//			return;
//		}

//		if (tb.InvokeRequired)
//		{
//			tb.BeginInvoke((MethodInvoker)delegate { AppendText(tb, msg); });
//		}
//		else
//		{
//			AppendText(tb, msg);
//		}
//	}
//	private string _prevMsg = "";
//	private void AppendText(TextBox tb, string msg, int maxLines = 500)
//	{
//		if (!string.IsNullOrEmpty(msg) && !tb.IsDisposed)
//		{
//			if (msg == _prevMsg)
//			{
//				tb.AppendText("+");
//			}
//			else
//			{
//				string normalized = Regex.Replace(msg, @"\r\n|\r|\n", "\n");
//				string normMsg = normalized.Replace("\n", Environment.NewLine);
//				tb.AppendText(normMsg);
//			}

//			if (tb.Lines.Length > maxLines)
//			{
//				var linesToKeep = tb.Lines.Take(maxLines).ToArray();
//				tb.Lines = linesToKeep;
//			}
//			_prevMsg = msg;
//		}
//	}
//	public static string ConvertToLocalNewLine(string input)
//	{
//		if (string.IsNullOrEmpty(input))
//			return input;

//		// Normalize line endings to "\n"
//		string normalized = Regex.Replace(input, @"\r\n|\r|\n", "\n");

//		// Replace "\n" with the local newline character
//		string localNewLine = Environment.NewLine;
//		string result = normalized.Replace("\n", localNewLine);

//		return result;
//	}

//	private void OnCopyOutput(object sender, EventArgs e)
//	{
//		if (txOutput != null)
//		{
//			Clipboard.SetText(txOutput.Text);
//		}
//	}

//	private void OnInputKeyDown(object sender, KeyEventArgs e)
//	{
//		if (e.KeyCode == System.Windows.Forms.Keys.Enter)
//		{
//			e.Handled = true;
//			Comm.SendMessage(txInput.Text);
//			txInput.Text = "";
//		}
//	}

//	private void OnTxInputFocus(object sender, EventArgs e)
//	{
//		_mouseAgent.HasFocus = false;
//	}

//	private void OnTxInputLeave(object sender, EventArgs e)
//	{
//		_mouseAgent.HasFocus = true;
//	}

//	private async void OnZeroX(object sender, EventArgs e)
//	{
//		await Comm.ZeroOutAxis(true, false, false);
//	}
//	private async void OnZeroY(object sender, EventArgs e)
//	{
//		await Comm.ZeroOutAxis(false, true, false);
//	}
//	private async void OnZeroXY(object sender, EventArgs e)
//	{
//		await Comm.ZeroOutAxis(true, true, false);
//	}
//	private async void OnZeroZ(object sender, EventArgs e)
//	{
//		await Comm.ZeroOutAxis(false, false, true);
//	}
//	private async void OnZeroAll(object sender, EventArgs e)
//	{
//		await Comm.ZeroOutAxis(true, true, true);
//	}
//	private void btLoad_Click(object sender, EventArgs e)
//	{
//		using (OpenFileDialog openFileDialog = new OpenFileDialog())
//		{
//			openFileDialog.Title = "Select a KiCad Project";
//			openFileDialog.Filter = "KiCad Project (*.kicad_pro)|*.kicad_pro";
//			//openFileDialog.InitialDirectory = @"C:\";
//			openFileDialog.Multiselect = false;

//			var dialogResult = openFileDialog.ShowDialog();
//			if (dialogResult == DialogResult.OK)
//			{
//				var path = openFileDialog.FileName;
//				ParseNewProject(path);

//			}
//		}
//	}
//	public KcPcb? ParseNewProject(string path, bool rememberPath = true)
//	{
//		var result = _demos.ParseNewProject(path, rememberPath);
//		Invalidate();
//		return result;
//	}

//	private void btOutput_Click(object sender, EventArgs e)
//	{
//		using (OpenFileDialog openFileDialog = new OpenFileDialog())
//		{
//			using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
//			{
//				folderDialog.Description = "Select an output folder";
//				folderDialog.ShowNewFolderButton = true;
//				if (Path.Exists(Slides.UserTargetDir))
//				{
//					folderDialog.InitialDirectory = Slides.UserTargetDir;
//				}
//				if (folderDialog.ShowDialog() == DialogResult.OK)
//				{
//					string selectedFolder = folderDialog.SelectedPath;
//					if (Path.Exists(selectedFolder))
//					{
//						Slides.UserTargetDir = selectedFolder;
//					}
//				}
//			}
//			openFileDialog.Title = "Select an output folder.";
//			openFileDialog.Filter = "KiCad Project (*.kicad_pro)|*.kicad_pro";
//			//openFileDialog.InitialDirectory = @"C:\";
//			openFileDialog.Multiselect = false;

//			if (openFileDialog.ShowDialog() == DialogResult.OK)
//			{
//				string filePath = openFileDialog.FileName;

//				// get front copper for now
//				var folderPath = Path.GetDirectoryName(filePath);
//				if (folderPath != null)
//				{
//					var files = Directory.GetFiles(folderPath, "*.kicad_pcb");

//					if (files.Length > 0)
//					{
//						try
//						{
//							_demos.ParseNewProject(filePath);
//						}
//						catch (Exception ex)
//						{
//							MessageBox.Show($"Error parsing file: {ex.Message}");
//						}

//					}
//				}
//			}
//		}
//	}

//	private async void btRun_Click(object sender, EventArgs e)
//	{
//		try
//		{
//			await _demos.Sender.RunJob(_demos.Jobs);
//		}
//		catch (OperationCanceledException tce)
//		{
//			Trace.WriteLine("Job cancelled: " + tce.Message);
//		}
//		catch (GrblException ex)
//		{
//			Trace.WriteLine($"GRBL error: {ex.Message}");
//		}
//		catch (Exception ex)
//		{
//			Trace.WriteLine($"Unexpected error: {ex.Message}");
//		}
//	}

//	private async void OnStop(object sender, EventArgs e)
//	{
//		_demos.Sender.CancelJob();
//		Trace.WriteLine("Cancellation requested by user.");
//		await Comm.StopGrblAsync();
//	}
//	private async void btUnlock_Click(object sender, EventArgs e)
//	{
//		await Comm.Unlock();
//	}

//	private async void btOutline_Click(object sender, EventArgs e)
//	{
//		await _demos.Sender.TraceOutline();
//	}

//	private async void btLastMetadata_Click(object sender, EventArgs e)
//	{
//		await _demos.Sender.EdgeProbe.LoadData();
//		_demos.Sender.Autolevel.LoadData();
//		NeedsUpdate();
//	}

//	private void OnCheckedChanged(object sender, EventArgs e)
//	{
//		if (sender == cbProbe)
//		{
//			btProbe.Enabled = cbProbe.Checked;
//			_demos.Jobs.IncludeProbe = cbProbe.Checked;
//		}
//		else if (sender == cbEdgeProbe)
//		{
//			btEdgeProbe.Enabled = cbEdgeProbe.Checked;
//			_demos.Jobs.IncludeEdgeProbe = cbEdgeProbe.Checked;
//		}
//		else if (sender == cbAutolevel)
//		{
//			btAutolevel.Enabled = cbAutolevel.Checked;
//			_demos.Jobs.IncludeAutolevel = cbAutolevel.Checked;
//		}
//		else if (sender == cbTracks)
//		{
//			btTracks.Enabled = cbTracks.Checked;
//			_demos.Jobs.IncludeTrackPaths = cbTracks.Checked;
//		}
//		else if (sender == cbCleanup)
//		{
//			btCleanup.Enabled = cbCleanup.Checked;
//			_demos.Jobs.IncludeCleanupPaths = cbCleanup.Checked;
//		}
//		else if (sender == cbRecuts)
//		{
//			btRecut.Enabled = cbRecuts.Checked;
//			_demos.Jobs.IncludeRecuts = cbCleanup.Checked;
//		}
//		else if (sender == cbMask)
//		{
//			btMask.Enabled = cbMask.Checked;
//			_demos.Jobs.IncludeMaskRemovalPaths = cbMask.Checked;
//		}
//		else if (sender == cbDrill)
//		{
//			btDrill.Enabled = cbDrill.Checked;
//			_demos.Jobs.IncludeDrillPaths = cbDrill.Checked;
//		}
//		else if (sender == cbEdgeScratches)
//		{
//			btEdgeCuts.Enabled = cbEdgeScratches.Checked;
//			_demos.Jobs.IncludeEdgeCutsPaths = cbEdgeScratches.Checked;
//		}
//		NeedsUpdate();
//	}

//	private void cbLayerPicker_SelectedIndexChanged(object sender, EventArgs e)
//	{
//		if (sender == cbLayerPicker)
//		{
//			SetSide();
//		}
//	}
//	public void SetSide()
//	{
//		if (InvokeRequired) // ensure main thread
//		{
//			Invoke(new Action(SetSide));
//		}
//		else
//		{
//			BoardSide = cbLayerPicker.SelectedIndex == 0 ? BoardSide.Front : BoardSide.Back;
//			NeedsUpdate();
//		}
//	}

//	private void btClear_Click(object sender, EventArgs e)
//	{
//		txOutput.Clear();
//	}

//	private async void btTest_Click(object sender, EventArgs e)
//	{
//		TestPatterns tp = new TestPatterns(_demos.Sender, "");
//		await tp.RunTestPatterns();
//	}

//	private void OnSpeedChanged(object sender, EventArgs e)
//	{
//		var speed = float.Parse(txSpeed.Text, System.Globalization.NumberStyles.Float);
//		switch(curMotion)
//		{
//			case MotionKind.Rapid:
//				rateRapid = (int)speed;
//				rate = rateRapid;
//				break;
//			case MotionKind.Normal:
//				rateNormal = (int)speed;
//				rate = rateNormal;
//				break;
//			case MotionKind.Precise:
//				ratePrecise = (int)speed;
//				rate = ratePrecise;
//				break;
//		}
//	}
//	private enum MotionKind
//	{
//		Rapid,
//		Normal,
//		Precise
//	}
}