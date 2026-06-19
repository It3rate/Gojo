using ScratchMill.Properties;
using SkiaSharp;

namespace ScratchMill.Machine;

public class GrblState
{
    public bool IsSendingProgram { get; set; } = false;
    public GrblActiveState MachineState { get; private set; } = GrblActiveState.Unknown;
    public GrblActiveState MachineTargetState { get; set; } = GrblActiveState.Unknown;
    public MachineSettings MachineSettings { get; private set; } = new MachineSettings();
    public GrblData Data { get; } = new GrblData();
    public MachineJogSettings JogSettings { get; } = new MachineJogSettings();

    public event EventHandler<AlarmEventArgs>? AlarmEvent;

    public int Status { get; private set; } = 0;
    public bool HasValidProbeHit { get; set; } = false;
    public float[] LastProbe => Data.ProbePosition;//{ get; } = { -1f, -1f, -1f };
    public int LineNumber { get; private set; } = 0;
    public float FeedRate { get; private set; } = 0;
    public float ProgrammedRPM { get; private set; } = 0;

    public int BlockBuffersFree { get; private set; } = 0;
    public int RxCharsFree { get; private set; } = 0;

    public float[] Overrides { get; } = { 0.0f, 0.0f, 0.0f };
    public float[] WorkCoordinateOffests { get; } = { 0.0f, 0.0f, 0.0f }; // total offsets, G54+G92+tool
    public float[] ZeroReturnsA { get; } = { 0.0f, 0.0f, 0.0f };
    public float[] ZeroReturnsB { get; } = { 0.0f, 0.0f, 0.0f };
    public float ToolLengthOffset { get; private set; } = 0f;

    public Dictionary<string, float[]> HomePositions = new Dictionary<string, float[]>(); // G54, G55... G59
    public float XMPos => Data.MPosition[0];
    public float YMPos => Data.MPosition[1];
    public float ZMPos => Data.MPosition[2];
    public string CurrentWCS { get; } = "G54";
    public float XPos => HomePositions.ContainsKey(CurrentWCS) ? XMPos - HomePositions[CurrentWCS][0] : XMPos;
    public float YPos => HomePositions.ContainsKey(CurrentWCS) ? YMPos - HomePositions[CurrentWCS][1] : YMPos;
    public float ZPos => HomePositions.ContainsKey(CurrentWCS) ? ZMPos - HomePositions[CurrentWCS][2] : ZMPos;
    public float CurrentZZero => HomePositions.ContainsKey(CurrentWCS) ? HomePositions[CurrentWCS][2] : ZMPos;
    public float[] CurrentHomeXY => [HomePositions[CurrentWCS][0], HomePositions[CurrentWCS][1]];
    public bool HasHomed { get; set; } = false;
    public bool HasCheckedforHome { get; private set; } = false;

    public SKPoint CurrentPos => new(XPos, YPos);
    public SKPoint CurrentMPos => new(XMPos, YMPos);
    public SKPoint MPosToCurrentWCS(SKPoint mPoint)
    {
        var result = mPoint;
        if (HomePositions.ContainsKey(CurrentWCS))
        {
            var offset = HomePositions[CurrentWCS];
            result = new(mPoint.X - offset[0], mPoint.Y - offset[1]);
        }
        return result;
    }

    private float[] _mPostionsLastRun;
    public GrblState()
    {
        _mPostionsLastRun = [Settings.Default.mXPos, Settings.Default.mYPos, Settings.Default.mZPos];
    }
    public bool CheckIfHomedOnRestart()
    {
        if (_mPostionsLastRun.Length == 3 && _mPostionsLastRun[0] != 0 && _mPostionsLastRun[1] != 0 && _mPostionsLastRun[2] != 0)
        {
            if (_mPostionsLastRun[0] == Data.MPosition[0] &&
                _mPostionsLastRun[1] == Data.MPosition[1] &&
                _mPostionsLastRun[2] == Data.MPosition[2])
            {
                HasHomed = true;
            }
            HasCheckedforHome = true;
        }
        return HasHomed;
    }

    public string ParseLine(string line)
    {
        var result = "";
        line = line.ToUpperInvariant();
        if (line.StartsWith("OK"))
        {
            Status = 0;
            result = "OK";
        }
        else if (line.StartsWith("ALARM:"))
        {
            var alarm = line.Substring(6, 1);
            Data.Alarm = int.Parse(alarm);
            if (AlarmEvent != null)
            {
                AlarmEvent?.Invoke(this, new AlarmEventArgs(Data.Alarm));
            }
            result = line;
        }
        else if (line.StartsWith("<"))
        {
            if (line.Contains(">"))
            {
                var info = line.Substring(1, line.LastIndexOf(">") - 1);
                result = ParseInfo(info);
            }
        }
        else if (line.StartsWith("["))
        {
            if (line.Contains("]"))
            {
                var msg = line.Substring(1, line.LastIndexOf("]") - 1);
                result = ParseMessage(msg);
            }
        }
        else if (line.StartsWith("$"))
        {
            result = MachineSettings.ParseLine(line);
        }
        return result;
    }
    private string ParseInfo(string info) // ?   <Idle|WPos:158.330,159.825,95.285|Bf:14,127|FS:0,0|Ov:100,100,100>
    {
        var result = "STATE";
        var parts = info.Split('|');
        MachineState = parts[0].GrblMachineStateFromString();
        for (int i = 1; i < parts.Length; i++)
        {
            var part = parts[i];
            if (part.StartsWith("WPOS:"))
            {
                ParseFloatsInto(part.Substring(5), Data.WPosition);
                // adjust wpos
            }
            else if (part.StartsWith("MPOS:"))
            {
                ParseFloatsInto(part.Substring(5), Data.MPosition);
                if (!HasCheckedforHome)
                {
                    CheckIfHomedOnRestart();
                }
                // adjust mpos
            }
            else if (part.StartsWith("PRB:"))
            {
                var floatsBool = part.Substring(4).Split(':');
                ParseFloatsInto(floatsBool[0], Data.ProbePosition);
                HasValidProbeHit = floatsBool[1] == "0" ? MachineSettings.InverProbePin : !MachineSettings.InverProbePin;
                result = "PRB_INFO";
            }
            else if (part.StartsWith("WCO:"))
            {
                ParseFloatsInto(part.Substring(4), WorkCoordinateOffests);
            }
            else if (part.StartsWith("BF:"))
            {
                var fsVals = ParseFloats(part.Substring(3));
                BlockBuffersFree = (int)fsVals[0];
                RxCharsFree = (int)fsVals[1];
            }
            else if (part.StartsWith("FS:"))
            {
                var fsVals = ParseFloats(part.Substring(3));
                FeedRate = fsVals[0];
                ProgrammedRPM = fsVals[1];
            }
            else if (part.StartsWith("LN:"))
            {
                var fsVals = ParseFloats(part.Substring(3));
                LineNumber = (int)fsVals[0];
            }
            else if (part.StartsWith("OV:"))
            {
                ParseFloatsInto(part.Substring(3), Overrides);

            }
            else if (part.StartsWith("PN:"))
            {
                // X Y Z XYZ limit pins, respectively
                // P the probe pin.
                // D H R S the door, hold, soft-reset, and cycle-start pins, respectively.
                // A May be A limit pin, future.
            }
            else if (part.StartsWith("A:"))
            {
                // S indicates spindle is enabled in the CW direction.This does not appear with C.
                // C indicates spindle is enabled in the CCW direction.This does not appear with S.
                // F indicates flood coolant is enabled.
                // M indicates mist coolant is enabled.
            }
        }
        Settings.Default.mXPos = Data.MPosition[0];
        Settings.Default.mYPos = Data.MPosition[1];
        Settings.Default.mZPos = Data.MPosition[2];
        Settings.Default.Save();
        return result;
    }
    private string ParseMessage(string info) // PRB|MSG|G54 etc   [PRB:-163.330,-164.825,-100.175:1]
    {
        var result = "";
        if (info.StartsWith("G5")) // G54, G55... home positions
        {
            var name = info.Substring(0, 3);
            if (!HomePositions.ContainsKey(name))
            {
                HomePositions[name] = new float[3];
            }
            ParseFloatsInto(info.Substring(4), HomePositions[name]);
            result = name;
            Settings.Default.G54X = HomePositions["G54"][0];
            Settings.Default.G54Y = HomePositions["G54"][1];
            Settings.Default.G54Z = HomePositions["G54"][2];
            Settings.Default.Save();
        }
        else if (info.StartsWith("PRB:"))
        {
            var floatsBool = info.Substring(4).Split(':');
            ParseFloatsInto(floatsBool[0], Data.ProbePosition);
            HasValidProbeHit = floatsBool[1] == "0" ? MachineSettings.InverProbePin : !MachineSettings.InverProbePin;
            //Data.ProbePosition.CopyTo(LastProbe, 0);
            result = "PRB_MSG";
        }
        else if (info.StartsWith("G28:"))
        {
            ParseFloatsInto(info.Substring(4), ZeroReturnsA);
            result = "G28";
        }
        else if (info.StartsWith("G30:"))
        {
            ParseFloatsInto(info.Substring(4), ZeroReturnsB);
            result = "G30";
        }
        else if (info.StartsWith("TLO:"))
        {
            var fsVals = ParseFloats(info.Substring(4)); // may have more axis eventually
            ToolLengthOffset = fsVals[0];
            result = "TLO";
        }
        return result;
    }
    private string ParseMachineSetting(string gcode) // $$   $1=255
    {
        var result = "Setting";
        return result;
    }
    private float[] ParseFloats(string csf)
    {
        List<float> result = new List<float>();
        var floatStrings = csf.Split(',');
        for (int i = 0; i < floatStrings.Length; i++)
        {
            var success = float.TryParse(floatStrings[i], out float f);
            if (success)
            {
                result.Add(f);
            }
            else
            {
                throw new Exception("Error parsing floats");
            }
        }
        return result.ToArray();
    }
    private void ParseFloatsInto(string csf, float[] target)
    {
        var floatStrings = csf.Split(',');
        for (int i = 0; i < floatStrings.Length; i++)
        {
            if (i - 1 < target.Length)
            {
                float.TryParse(floatStrings[i], out target[i]);
            }
        }
    }
}
public class AlarmEventArgs : EventArgs
{
    public int Value { get; }

    public AlarmEventArgs(int value)
    {
        Value = value;
    }
}