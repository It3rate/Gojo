using ScratchMill.GCode;
using ScratchMill.Machine;
using SkiaSharp;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace ScratchMill.Comm
{
    public class GrblSender
    {
        public GrblComm Comm;
        public ScratchJobs Jobs { get; private set; }
        public GrblState GrblState => Comm.GrblState;
        private CarveSettings _carveSettings => Jobs.CarveSettings;
        public Probe Probe => Jobs.GetProbeForSide(Jobs.Side);
        public EdgeProbe EdgeProbe => Jobs.GetEdgeProbeForSide(Jobs.Side);
        public Autolevel Autolevel => Jobs.GetAutolevelForSide(Jobs.Side);
        public ContinuityProbe ContinuityProbe => Jobs.GetContinuityTestsForSide(Jobs.Side);
        public SKMatrix Transform => Jobs.GetTransformForSide(Jobs.Side);

        public GCodeWriter? gCodeWriter;
        public DialogResult LastDialogResult { get; private set; } = DialogResult.OK;
        protected string _resolution => GCodeWriter._resolution;
        private int _currentMillDepthIndex;
        private float _currentMillDepth;
        private bool _isCancelled = false;
        public bool IsBusy
        {
            get => GrblState.IsSendingProgram;
            set => GrblState.IsSendingProgram = value;
        }

        public GrblSender(GrblComm comm)
        {
            Comm = comm;
            Jobs = new ScratchJobs(this);
            _bufferUsed = 0;
            gCodeWriter = new GCodeWriter(this);
        }

        private TaskCompletionSource<bool>? _completionSource;
        private CancellationTokenSource? _cts;
        public bool IsRunningFullJob { get; private set; } = false;
        public async Task RunJob(ScratchJobs jobs, CancellationToken externalCancelToken = default)
        {
            if (!Comm.IsConnected)
            {
                throw new InvalidOperationException("GRBL is not connected.");
            }
            IsBusy = true;
            IsRunningFullJob = true;
            Jobs = jobs; // Set jobs for this run
            _isCancelled = false;
            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalCancelToken);
            _completionSource = new TaskCompletionSource<bool>();
            _cts.Token.Register(() => _completionSource?.TrySetCanceled(_cts.Token));

            try
            {
                await ResetAll();
                await Continue(_cts.Token);
                await _completionSource.Task;
            }
            catch (OperationCanceledException)
            {
                await Abort();
                throw;
            }
            catch (Exception ex)
            {
                await Abort();
                _cts.Cancel(); // Cancel on error
                throw new GrblException("Job failed", ex);
            }
            finally
            {
                IsRunningFullJob = false;
                IsBusy = false;
                _cts?.Dispose();
                _cts = null;
                _completionSource = null;
            }
        }
        public void CancelJob()
        {
            _isCancelled = true;
            _cts?.Cancel();
        }
        private async Task SendGrblResetAsync()
        {
            Trace.WriteLine("Sending GRBL reset...");
            await Comm.StopGrblAsync();
        }

        private async Task ResetAll()
        {
            IsBusy = false;
            Jobs.JobState = JobState.Idle;
            await Comm.UnlockTask();
            ClearBuffer();
            _currentMillDepthIndex = 0;
            _currentMillDepth = 0f;
        }
        public async Task Continue(CancellationToken cancelToken)
        {
            Jobs.JobState++;
            try
            {
                switch (Jobs.JobState)
                {
                    case JobState.End:
                        // don't continue
                        break;
                    case JobState.Error:
                        // don't continue
                        break;
                    default:
                        if (Jobs.JobState <= JobState.Complete)
                        {
                            var gcode = gCodeWriter?.GetGCodeCurrentForState(true) ?? "";
                            if (gcode != "")
                            {
                                await SendGCodeFragment(gcode, cancelToken);
                            }
                            await Continue(cancelToken);
                        }
                        break;
                }
            }
            catch (Exception)
            {
                _isCancelled = true;
                CancelJob();
            }
        }
        public async Task RunGCodeForStateAndCurrentLayer(JobState state, CancellationToken cancelToken = default)
        {
            var curState = state.GetVersionForLayer(Jobs.IsTopLayer);
            Jobs.JobState = curState;
            var gcode = gCodeWriter?.GetGCodeCurrentForState(true) ?? "";
            if (gcode != "")
            {
                _isCancelled = false;

                try
                {
                    IsBusy = true;
                    await SendGCodeFragment(gcode + "\n(EndPosition)", cancelToken);
                }
                finally
                {
                    IsBusy = false;
                    Jobs.JobState = JobState.Idle;
                }
            }
        }
        public async Task Abort()
        {
            await ResetAll();
            _isCancelled = true;
            Jobs.JobState = JobState.Error;
            await SendGrblResetAsync();
            await Comm.Unlock();
        }

        public async Task SendGCodeFragment(string gcode, CancellationToken cancelToken)
        {
            var lines = gcode.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(line => line.Trim())
                             .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith(";"))
                             .ToList();

            try
            {
                foreach (var line in lines)
                {
                    if (_isCancelled) break;

                    if (line.StartsWith("%")) continue;

                    var modLine = line;
                    cancelToken.ThrowIfCancellationRequested();
                    // in place macros
                    if (modLine.Contains($"(SafeZMove)"))
                    {
                        modLine = modLine.Replace($"(SafeZMove)", "G00 Z" + _carveSettings.SafeZMove.ToString("0.###"));
                    }
                    if (modLine.Contains($"(ZSpeed)"))
                    {
                        modLine = modLine.Replace($"(ZSpeed)", "G01 F" + _carveSettings.ZSpeed.ToString(_resolution));
                    }
                    if (modLine.Contains($"(HorzScratchSpeed)"))
                    {
                        modLine = modLine.Replace($"(HorzScratchSpeed)", "G01 F" + _carveSettings.HorzScratchSpeed.ToString(_resolution));
                    }


                    if (modLine.StartsWith("(")) // Macros
                    {
                        if (modLine == "(ZeroXY)")
                        {
                            await Comm.ZeroXY(cancelToken);
                        }
                        else if (modLine == "(ProbeAndZeroZ)")// && !Probe.HasCompleted)
                        {
                            await Probe.RunProbeAndZeroZ(cancelToken);
                        }
                        else if (modLine == "(EdgeProbe)")
                        {
                            await EdgeProbe.RunEdgeProbe(cancelToken);
                            if (EdgeProbe.HasError)
                            {
                                Jobs.JobState = JobState.Error;
                            }
                        }
                        else if (modLine == "(Autolevel)")
                        {
                            await Autolevel.RunAutolevel(EdgeProbe.ValidCorners, -1, cancelToken);
                            _carveSettings.SafeZMove = Autolevel.ZMax + 0.5f; // allow for thickness of soldermask, may need to adjust
                        }
                        else if (modLine == "(ContinuityTests)")
                        {
                            await ContinuityProbe.RunProbe(cancelToken);
                        }
                        else if (line.StartsWith("(Pause")) // pause, put up dialog
                        {
                            string pattern = @"\(Pause(\d+)\)";
                            Match match = Regex.Match(line, pattern);
                            if (match.Success)
                            {
                                string numberStr = match.Groups[1].Value;
                                var val = int.Parse(numberStr);
                                Thread.Sleep(val);
                            }
                        }
                        else if (modLine.StartsWith("(Dialog")) // pause, put up dialog
                        {
                            LastDialogResult = DialogResult.OK;
                            var safe = false;
                            var skip = false;
                            var msg = modLine.Substring(7, modLine.Length - 8).Trim(); // remove trailing )
                            if (msg.StartsWith("Safe"))
                            {
                                safe = true;
                                msg = msg.Substring(4, msg.Length - 4).Trim();
                            }
                            if (msg.StartsWith("Skip"))
                            {
                                skip = true;
                                msg = msg.Substring(4, msg.Length - 4).Trim();
                            }

                            if (safe)
                            {
                                LastDialogResult = await ShowDialogFromUserSafe(msg, skip, cancelToken);
                            }
                            else
                            {
                                LastDialogResult = await ShowDialog(msg, skip, cancelToken);
                            }

                            if (LastDialogResult == DialogResult.Ignore)
                            {
                                Jobs.JobState++;
                            }
                        }
                        else if (modLine.StartsWith("(millDepth")) // modify Z on the fly
                        {
                            int endIndex = modLine.IndexOf(')');
                            string numberStr = modLine.Substring(10, endIndex - 10).Trim();
                            if (float.TryParse(numberStr, System.Globalization.NumberStyles.Float,
                                              System.Globalization.CultureInfo.InvariantCulture, out float millDepth))
                            {
                                _currentMillDepth = millDepth;
                            }
                        }
                        else if (modLine.StartsWith("(depthIndex")) // modify Z on the fly
                        {
                            string pattern = @"\(depthIndex([A-Za-z])(\d+)\)";
                            Match match = Regex.Match(modLine, pattern);
                            if (match.Success)
                            {
                                string type = match.Groups[1].Value.ToUpperInvariant();
                                string numberStr = match.Groups[2].Value;
                                if (int.TryParse(numberStr, out int index))
                                {
                                    var depthArray = type == "T" ? _carveSettings.ContourPass.Depths :
                                                     type == "B" ? _carveSettings.BurrPass.Depths :
                                                     type == "R" ? _carveSettings.ContourPass.Depths :
                                                     type == "G" ? _carveSettings.GraphicPass.Depths :
                                                     type == "M" ? _carveSettings.MaskRemovalPass.Depths :
                                                     type == "C" ? _carveSettings.EdgeCutPass.Depths : [];
                                    if (index >= 0 && index < depthArray.Length)
                                    {
                                        _currentMillDepthIndex = index;
                                        _currentMillDepth = depthArray[_currentMillDepthIndex];
                                    }
                                    else
                                    {
                                        Trace.WriteLine("Missing depth data.");
                                        _currentMillDepthIndex = -1;
                                        _currentMillDepth = 0;
                                    }
                                }
                            }
                            modLine = "G01 Z" + _currentMillDepth.ToString(_resolution);
                        }
                        else if (modLine.StartsWith("(EndPosition"))
                        {
                            await Comm.MoveToX0Y0Z4Task(cancelToken);
                        }
                        else if (modLine == "(Complete)")
                        {
                            await Comm.MoveToX0Y0();
                            await Comm.MoveToUserActionHeight(cancelToken);
                        }
                        await Comm.ResetToAbsoluteMode(cancelToken);
                        continue; // skip comments
                    }
                    else
                    {
                        if (modLine.StartsWith("G01 X")) // autolevel adjust
                        {
                            string pattern = @"X(-?\d+\.\d+)\s+Y(-?\d+\.\d+)";
                            Match match = Regex.Match(modLine, pattern);
                            if (match.Success)
                            {
                                string xValue = match.Groups[1].Value; // Captures "18.53"
                                string yValue = match.Groups[2].Value; // Captures "13.608"
                                var x = float.Parse(xValue, System.Globalization.CultureInfo.InvariantCulture);
                                var y = float.Parse(yValue, System.Globalization.CultureInfo.InvariantCulture);
                                var offset = Autolevel.GetZAdjust(x, y);
                                modLine = modLine + " Z" + (_currentMillDepth + offset).ToString(_resolution);
                            }
                        }
                    }

                    await WaitForBufferSpace(modLine, cancelToken);
                    //Trace.WriteLine(modLine);
                    await SendBufferedLine(modLine, cancelToken);
                }
                await Comm.WaitForOkAsync(cancelToken);
            }
            catch (Exception e)
            {
                _isCancelled = true;
                IsBusy = false;
                Trace.WriteLine($"Error running program: {e}");
                throw;
            }
            finally
            {
            }
        }
        public async Task<DialogResult> ShowDialogFromUserSafe(string msg, bool showSkipButton, CancellationToken cancelToken)
        {
            var startY = GrblState.YPos;
            var startZ = GrblState.ZPos;

            await Comm.MoveToUserActionHeight(cancelToken);
            await Comm.WaitForZPosition(50, 10, cancelToken);

            var result = await Comm.PauseDialogPopup(msg, showSkipButton);
            await Comm.AMsg($"G0 Y{startY.ToString(_resolution)}", cancelToken);
            return result;
        }
        public async Task<DialogResult> ShowDialog(string msg, bool showSkipButton, CancellationToken cancelToken)
        {
            var result = await Comm.PauseDialogPopup(msg, showSkipButton);
            return result;
        }

        public async Task RunProbeAndZero(CancellationToken cancelToken = default)
        {
            try
            {
                await Probe.RunProbeAndZeroZ(cancelToken);
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Error running probe: {e}");
            }
        }
        public async Task RunEdgeProbe(CancellationToken cancelToken = default)
        {
            await EdgeProbe.RunEdgeProbe(cancelToken);
        }
        public async Task RunAutolevel(int stepSize = -1, CancellationToken cancelToken = default)
        {
            await Autolevel.RunAutolevel(EdgeProbe.ValidCorners, stepSize, cancelToken);
        }
        public async Task RunContinuityTests(int stepSize = -1, CancellationToken cancelToken = default)
        {
            await ContinuityProbe.RunProbe(cancelToken);
        }
        public async Task TraceOutline(int rate = -1, CancellationToken cancelToken = default)
        {
            var feedrate = rate == -1 ? "" : $"F{rate}";
            var corners = Jobs.GetEdgeProbeForSide(Jobs.Side).ValidCorners.GetPoints();
            var orgPos = new SKPoint(GrblState.XPos, GrblState.YPos);
            await Comm.AMsg($"G90", cancelToken);
            foreach (var corner in corners)
            {
                await Comm.AMsg($"G0X{corner.X}Y{corner.Y}{feedrate}", cancelToken);
            }
            await Comm.AMsg($"G0X{corners[0].X}Y{corners[0].Y}{feedrate}", cancelToken);
            await Comm.AMsg($"G0X{orgPos.X.ToString(_resolution)}Y{orgPos.Y.ToString(_resolution)}{feedrate}", cancelToken);
        }





        #region Buffer
        private string _lastCommand = "";
        private StringBuilder _bufferString = new StringBuilder();
        private int _bufferUsed; // Tracks characters in GRBL's buffer
        private const int MaxBufferSize = 255; // Adjust based on GRBL version (127 or 254)
        public async Task SendBufferedLine(string line, CancellationToken cancelToken)
        {
            await Comm.AMsg(line, cancelToken);
            _bufferString.Append(line);
            _lastCommand = line;
            _bufferUsed += line.Length + 1; // Track buffer usage
        }
        public async Task WaitForBufferSpace(string line, CancellationToken cancelToken)
        {
            while (_bufferUsed + line.Length + 1 > MaxBufferSize) // +1 for newline
            {
                await WaitForOkAsyncBuffered(cancelToken); // may need more time than default in some cases?
            }
        }
        // Wait for ok response
        public async Task WaitForOkAsyncBuffered(CancellationToken cancelToken, int timeoutSeconds = 10)
        {
            await Comm.WaitForOkAsync(cancelToken, timeoutSeconds);

            _bufferUsed = Math.Max(0, _bufferUsed - (_lastCommand.Length + 1)); // Update buffer (simplified)

            if (_bufferString.Length - _lastCommand.Length > 0)
            {
                _bufferString.Remove(_bufferString.Length - _lastCommand.Length, _lastCommand.Length);
            }
            else
            {
                _bufferString.Clear();
            }
            _lastCommand = "";
        }
        public void ClearBuffer()
        {
            _bufferString.Clear();
            _lastCommand = "";
        }
        #endregion
    }
}
