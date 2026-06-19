using ScratchMill.GCode;
using ScratchMill.Machine;
using ScratchMill.Properties;
using SkiaSharp;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Timers;

namespace ScratchMill.Comm;

public class GrblComm : IDisposable
{
    public GrblState GrblState { get; } = new GrblState();
    private bool _requireHome = false;
    private ComPort _port;
    public bool LastMsgWasOk;
    public bool IsAwaitingProbe { get; set; } = false;
    public bool IsConnected => _port.IsConncected;

    public delegate void StringEventHandler(object sender, string e);
    public event StringEventHandler? MessageSentEvent;
    public event StringEventHandler? MessageReceivedEvent;
    public event StringEventHandler? ProbeEvent;
    public event EventHandler? UpdateStateEvent;
    public bool ProbeEventHasListeners => ProbeEvent != null;


    public SKPoint CurrentPos => GrblState.CurrentPos;
    public SKPoint CurrentMPos => GrblState.CurrentMPos;
    public int XMDir => GrblState.MachineSettings.XMDir;
    public int YMDir => GrblState.MachineSettings.YMDir;
    public int ZMDir => GrblState.MachineSettings.ZMDir;

    private System.Timers.Timer _timer;
    protected string _resolution => GCodeWriter._resolution;

    public GrblComm()
    {
        Application.ApplicationExit += OnApplicationExit;

        float[] lastMPositions = [Settings.Default.mXPos, Settings.Default.mYPos, Settings.Default.mZPos];

        _port = new ComPort();
        _port.FindGrblController(ReadSerial, out string portName);
        if (_port.IsConncected)
        {
            GrblState.HasHomed = false;
        }

        _timer = new System.Timers.Timer(500);
        _timer.Elapsed += OnInfoTimer;
        _timer.AutoReset = true;
        _timer.Enabled = true;
    }

    #region Com Port

    private bool _ignoreStateInfo = true;
    private readonly StringBuilder _responseBuffer = new StringBuilder();
    public void ReadSerial(object sender, SerialDataReceivedEventArgs e)
    {
        while (_port.CanContinue && _port.TryDequeueResponse(out string data))
        {
            try
            {
                _responseBuffer.Append(data);
                string[] lines = _responseBuffer.ToString().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                _responseBuffer.Clear();
                if (!lines[lines.Length - 1].EndsWith("\n"))
                {
                    _responseBuffer.Append(lines[lines.Length - 1]); // Keep partial line
                    lines = lines.Take(lines.Length - 1).ToArray();
                }

                foreach (string recLine in lines)
                {
                    if (_wasStopped)
                    {
                        _responseBuffer.Clear();
                        break;
                    }

                    if (!string.IsNullOrEmpty(recLine))
                    {
                        var messageKind = GrblState.ParseLine(recLine);

                        if (messageKind.StartsWith("PRB"))
                        {
                            ProbeEvent?.Invoke(this, recLine);
                        }
                        else if (messageKind == "STATE" && UpdateStateEvent != null)
                        {
                            UpdateStateEvent(this, EventArgs.Empty);
                        }
                        else if (messageKind == "SETTING")
                        {
                            messageKind = "OK";
                        }
                        LastMsgWasOk = messageKind == "OK";
                        var ignoreMsg = LastMsgWasOk | ((messageKind == "STATE") && _ignoreStateInfo);
                        if (!ignoreMsg)
                        {
                            if (MessageReceivedEvent != null)
                            {
                                MessageReceivedEvent(this, recLine + "\n");
                                if (recLine.Contains("error"))
                                {
                                    Trace.WriteLine("*** " + recLine);
                                }
                            }
                        }
                        _ignoreStateInfo = true;
                    }
                }
            }
            catch (TimeoutException)
            {
            }
            _wasStopped = false;
        }
    }
    #endregion
    #region Commands
    private bool _hasTicked = false;
    private async void OnInfoTimer(object? sender, ElapsedEventArgs e)
    {
        if (!_hasTicked)
        {
            _hasTicked = true;
            _ignoreStateInfo = false;
            //await UnlockTask();
            await GetSettings();// AMsg("$$");
            await GetInfo();
            await GetParameters();
        }
        else if (GrblState.MachineSettings.HomingCycleEnabled && _requireHome && GrblState.HasCheckedforHome == true && GrblState.HasHomed == false)
        {
            GrblState.HasHomed = true;
            await HomeTask();
        }
        else if (!GrblState.IsSendingProgram && !IsAwaitingProbe)
        {
            _ignoreStateInfo = true;
            await GetInfo();
        }
    }
    public async Task GetSettings()
    {
        await AMsg("$$");
    }
    public async Task GetParameters()
    {
        await AMsg("$#");
    }
    public async Task GetInfoUser()
    {
        _ignoreStateInfo = false;
        await GetInfo();
    }
    public async Task GetInfo()
    {
        await AMsg("?");
    }
    public async Task Unlock()
    {
        await AMsg("$X");
    }
    public async Task UnlockTask()
    {
        await AMsg("$X");
    }
    public async Task HomeTask(CancellationToken cancelToken = default)
    {
        await AMsg("$H", cancelToken, 20);
        GrblState.HasHomed = true;
    }
    public async Task Home()
    {
        await AMsg("$H", timeoutSeconds: 20);
        GrblState.HasHomed = true;
    }
    public async Task MoveToUserActionHeight(CancellationToken cancelToken)
    {
        await AMsg("M5; stop spindle");
        await AMsg("G0 Y60 Z50 F1200", cancelToken);
    }
    public async Task MoveToX0Y0(int rate = -1)
    {
        var feedrate = rate == -1 ? "" : $"F{rate}";
        await AMsg($"G0X0Y0{feedrate}");
    }
    public async Task MoveToX0Y0Z4(int rate = -1)
    {
        var feedrate = rate == -1 ? "" : $"F{rate}";
        await AMsg($"G0X0Y0Z4{feedrate}");
    }
    public async Task MoveToX0Y0Z4Task(CancellationToken cancelToken, int rate = -1)
    {
        var feedrate = rate == -1 ? "" : $"F{rate}";
        await AMsg($"G0X0Y0Z4{feedrate}");
    }
    public async Task Jog(string axis, float stepSize, int rate = -1)
    {

        // note: feedrate is ignored for G0, speed is set by $110, $111, $112 for x,y,z
        var feedrate = rate == -1 ? "" : $"F{rate.ToString(_resolution)}";
        var mDir = (axis == "X") ? XMDir : (axis == "Y") ? YMDir : ZMDir;

        await AMsg($"G91 G0 {axis}{stepSize * mDir} {feedrate}");
        await AMsg("G90");
    }
    public async Task ZeroOutAxis(bool zeroX, bool zeroY, bool zeroZ)
    {
        var zeroOutString = GCodeWriter.ZeroOutAxis(zeroX, zeroY, zeroZ);
        await AMsg(zeroOutString);
        await GetParameters();
    }
    public async Task ZeroXY(CancellationToken cancelToken = default)
    {
        if (_port.IsConncected)
        {
            await AMsg("G90 G54 G10 L20 P0 X0 Y0; zero axis", cancelToken);
            await GetParameters();
        }
    }
    public async Task ZeroXYFromSavedMachinePosition(float savedMachineX, float savedMachineY, CancellationToken cancelToken = default)
    {
        await AMsg($"G90 G10 L2 P1 X{savedMachineX} Y{savedMachineY}; restore zero position", cancelToken);
        await GetParameters();
    }

    public async Task WaitForZPosition(float z, double timeoutSeconds, CancellationToken cancelToken)
    {
        var startTime = DateTime.Now;
        while (GrblState.ZPos < z - 2f)
        {
            if (DateTime.Now.Subtract(startTime).TotalSeconds > timeoutSeconds)
            {
                Trace.WriteLine("Timeout waiting for Z move.");
                break;
                //throw new TimeoutException("Timeout waiting for Z move.");
            }
            await GetInfo();
            await Task.Delay(100, cancelToken);
        }
    }
    private bool _wasStopped = false;

    public Task StopGrblAsync()
    {
        try
        {
            LastMsgWasOk = true;
            _wasStopped = true;
            MainView.Instance?.Jobs.CancelJob(); // todo: deuglify

            // Send soft reset (Ctrl-X, ASCII 0x18)
            if (_port.IsOpen)
            {
                _port.Write("\x18");
                Trace.WriteLine("Sent soft reset to GRBL.");
            }

            // Wait for GRBL to reset and become idle
            //await WaitForOkAndIdleAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error stopping GRBL: {ex.Message}");
            throw;
        }
        return Task.CompletedTask;
    }
    public async Task ResetToAbsoluteMode(CancellationToken cancelToken)
    {
        LastMsgWasOk = true;
        await AMsg("G90", cancelToken); // in case didn't complete
    }
    #endregion
    #region Messages
    public void SendMessage(string message, CancellationToken cancelToken = default)
    {
        if (_port.IsOpen)
        {
            try
            {
                if (!message.StartsWith("?"))
                {
                    MessageSentEvent?.Invoke(this, message + Environment.NewLine);
                }
                _port.WriteLine(message);
                //Trace.WriteLine("-- " + message);
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Failed to send message {message}. \n{e.Message}");
            }
        }
        else
        {
            if (!message.StartsWith("?"))
            {
                Trace.WriteLine($"closed msg: {message}");
            }
        }
        cancelToken.ThrowIfCancellationRequested();
    }
    // Modified AMsg to send a single command
    public async Task AMsg(string msg, CancellationToken cancelToken = default, int timeoutSeconds = 10)
    {
        LastMsgWasOk = false;
        SendMessage(msg, cancelToken);
        await WaitForOkAsync(cancelToken, timeoutSeconds); // Wait for ok only, not idle
    }
    public async Task WaitForOkAsync(CancellationToken cancelToken, int timeoutSeconds = 10)
    {
        var startTime = DateTime.Now;
        int count = 0;
        while (!LastMsgWasOk && _port.IsConncected && !_wasStopped)
        {
            if (DateTime.Now.Subtract(startTime).TotalSeconds > timeoutSeconds)
            {
                Trace.WriteLine("Timeout waiting for 'ok' response.");
                LastMsgWasOk = true;
                break;//throw new TimeoutException("Timeout waiting for 'ok' response.");
            }
            if (count == 200 && !LastMsgWasOk)
            {
                // try to generate an ok
                SendMessage("; gen ok", cancelToken);
            }

            await Task.Delay(10, cancelToken);
            count++;
        }
    }
    //   public async Task AMsgAndIdle(string msg, CancellationToken cancelToken)
    //{
    //	LastMsgWasOk = false;
    //	SendMessage(msg, cancelToken);
    //	await WaitForOkAndIdleAsync(cancelToken);
    //}
    // Wait for ok and Idle (used after program completion)
    //   public async Task WaitForOkAndIdleAsync(CancellationToken cancelToken)
    //{
    //	var startTime = DateTime.Now;
    //	const int timeoutSeconds = 10;

    //	// Wait for ok
    //	while (!LastMsgWasOk && _port.IsConncected)
    //	{
    //		if (DateTime.Now.Subtract(startTime).TotalSeconds > timeoutSeconds)
    //		{
    //			throw new TimeoutException("Timeout waiting for 'ok' response.");

    //			await Task.Delay(10, cancelToken);
    //		}

    //		// Wait for Idle
    //		startTime = DateTime.Now;
    //		while (GrblState.MachineState != GrblActiveState.Idle)
    //		{
    //			if (DateTime.Now.Subtract(startTime).TotalSeconds > timeoutSeconds)
    //			{
    //				throw new TimeoutException("Timeout waiting for Idle state.");
    //			}
    //		}

    //		LastMsgWasOk = false;
    //		await GetInfo();
    //		await Task.Delay(100, cancelToken);
    //	}
    //}

    #endregion
    #region Application
    public Task<DialogResult> PauseDialogPopup(string msg, bool showSkipButton = false)
    {
        DialogResult result = DialogResult.Cancel;

        if (Application.MessageLoop)
        {
            if (showSkipButton)
            {
                result = ShowCustomMessageBox(msg, showSkipButton);
            }
            else
            {
                result = MessageBox.Show(
                    msg,
                    "Paused",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        else
        {
            // Create an STA thread for the MessageBox
            Thread staThread = new Thread(() =>
            {
                if (showSkipButton)
                {
                    result = ShowCustomMessageBox(msg, showSkipButton);
                }
                else
                {
                    result = MessageBox.Show(
                        msg,
                        "Paused",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            });
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join(); // Wait for the dialog to close
        }
        return Task.FromResult(result);
    }
    private DialogResult ShowCustomMessageBox(string msg, bool showSkipButton)
    {
        using (var form = new Form())
        {
            form.Text = "Paused";
            form.Size = new Size(300, 160);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.TopMost = true;

            var label = new Label
            {
                Text = msg,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 20, 10, 10)
            };
            form.Controls.Add(label);

            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Dock = DockStyle.Bottom,
                Padding = new Padding(10, 0, 10, 10),
                Height = 40
            };
            form.Controls.Add(buttonPanel);

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Size = new Size(75, 25),
                Margin = new Padding(10, 0, 5, 0)
            };
            buttonPanel.Controls.Add(okButton);

            if (showSkipButton)
            {
                var skipButton = new Button
                {
                    Text = "Skip",
                    DialogResult = DialogResult.Ignore,
                    Size = new Size(75, 25),
                    Margin = new Padding(5, 0, 10, 0)
                };
                buttonPanel.Controls.Add(skipButton);
                form.CancelButton = skipButton;
            }
            else
            {
                form.CancelButton = okButton;
            }

            form.AcceptButton = okButton;
            return form.ShowDialog();
        }
    }
    private void OnApplicationExit(object? sender, EventArgs e)
    {
        _port.CloseSerialPort();
    }
    public void Dispose()
    {
        _port.Dispose();
    }
    #endregion
}

public class GrblException : Exception
{
    public GrblException() : base() { }

    public GrblException(string message) : base(message) { }

    public GrblException(string message, Exception innerException) : base(message, innerException) { }
}

public static class TimeoutExension
{
    public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
    {
        var delayTask = Task.Delay(timeout);
        var completedTask = await Task.WhenAny(task, delayTask);
        if (completedTask == delayTask)
        {
            throw new TimeoutException();
        }
        return await task;
    }
}