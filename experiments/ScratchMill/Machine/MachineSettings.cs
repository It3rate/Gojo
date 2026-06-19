using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;

namespace ScratchMill.Machine
{
    public class MachineSettings
    {
        // Stepper motor settings
        public int StepPulseTime { get; set; } // $0 (microseconds)
        public int StepIdleDelay { get; set; } // $1 (milliseconds)
        public int StepPulseInvertMask { get; set; } // $2 (bitmask: 0=normal, 1=invert for each axis)
        public int DirectionPortInvertMask { get; set; } // $3 (bitmask: 0=normal, 1=invert for each axis)

        // Axis calibration
        public float[] StepsPerMm { get; set; } = new float[3]; // $100 (X), $101 (Y), $102 (Z) (steps/mm)
        public float[] MaxRate { get; set; } = new float[3]; // $110 (X), $111 (Y), $112 (Z) (mm/min)
        public float[] Acceleration { get; set; } = new float[3]; // $120 (X), $121 (Y), $122 (Z) (mm/sec^2)
        public float[] MaxTravel { get; set; } = new float[3]; // $130 (X), $131 (Y), $132 (Z) (mm)

        // Machine settings
        public float JunctionDeviation { get; set; } // $11 (mm)
        public float ArcTolerance { get; set; } // $12 (mm)
        public bool ReportInches { get; set; } // $13 (0=mm, 1=inches)

        // Control settings
        public bool InvertStepEnablePin { get; set; } // $4 (0=disabled, 1=enabled)
        public bool InvertLimitPins { get; set; } // $5 (0=disabled, 1=enabled)
        public bool InverProbePin { get; set; } // $6 (0=disabled, 1=enabled)
        public bool SoftLimitsEnabled { get; set; } // $20 (0=disabled, 1=enabled)
        public bool HardLimitsEnabled { get; set; } // $21 (0=disabled, 1=enabled)
        public bool HomingCycleEnabled { get; set; } // $22 (0=disabled, 1=enabled)
        public int HomingDirectionInvertMask { get; set; } // $23 (bitmask: 0=normal, 1=invert for each axis)
        public float HomingFeedRate { get; set; } // $24 (mm/min)
        public float HomingSeekRate { get; set; } // $25 (mm/min)
        public int HomingDebounceDelay { get; set; } // $26 (milliseconds)
        public float HomingPullOff { get; set; } // $27 (mm)

        // Spindle settings
        public int MaxSpindleSpeed { get; set; } // $30 (RPM)
        public int MinSpindleSpeed { get; set; } // $31 (RPM)
        public bool LaserModeEnabled { get; set; } // $32 (0=disabled, 1=enabled)

        // Status reporting
        public int StatusReportMask { get; set; } // $10 (bitmask: 0=MPos, 1=WPos, etc.)

        public int XMDir => (StepPulseInvertMask & 0x01) == 0 ? 1 : -1;
        public int YMDir => (StepPulseInvertMask & 0x02) == 0 ? 1 : -1;
        public int ZMDir => (StepPulseInvertMask & 0x04) == 0 ? 1 : -1;
        public string ParseLine(string line)
        {
            int settingNumber = -1;
            var match = Regex.Match(line, @"^\$(\d+)=([\d.]+|true|false)");
            if (match.Success)
            {
                settingNumber = int.Parse(match.Groups[1].Value);
                string value = match.Groups[2].Value;

                switch (settingNumber)
                {
                    case 0: StepPulseTime = int.Parse(value); break;
                    case 1: StepIdleDelay = int.Parse(value); break;
                    case 2: StepPulseInvertMask = int.Parse(value); break;
                    case 3: DirectionPortInvertMask = int.Parse(value); break;
                    case 4: InvertStepEnablePin = value == "1" || value.ToLowerInvariant() == "true"; break;
                    case 5: InvertLimitPins = value == "1" || value.ToLowerInvariant() == "true"; break;
                    case 6: InverProbePin = value == "1" || value.ToLowerInvariant() == "true"; break;
                    case 10: StatusReportMask = int.Parse(value); break;
                    case 11: JunctionDeviation = float.Parse(value); break;
                    case 12: ArcTolerance = float.Parse(value); break;
                    case 13: ReportInches = value == "1" || value.ToLowerInvariant() == "true"; break;
                    case 20: SoftLimitsEnabled = value == "1" || value.ToLowerInvariant() == "true"; break;
                    case 21: HardLimitsEnabled = value == "1" || value.ToLowerInvariant() == "true"; break;
                    case 22: HomingCycleEnabled = value == "1" || value.ToLowerInvariant() == "true"; break;
                    case 23: HomingDirectionInvertMask = int.Parse(value); break;
                    case 24: HomingFeedRate = float.Parse(value); break;
                    case 25: HomingSeekRate = float.Parse(value); break;
                    case 26: HomingDebounceDelay = int.Parse(value); break;
                    case 27: HomingPullOff = float.Parse(value); break;
                    case 30: MaxSpindleSpeed = int.Parse(value); break;
                    case 31: MinSpindleSpeed = int.Parse(value); break;
                    case 32: LaserModeEnabled = value == "1" || value.ToLowerInvariant() == "true"; break;
                    case 100: StepsPerMm[0] = float.Parse(value); break;
                    case 101: StepsPerMm[1] = float.Parse(value); break;
                    case 102: StepsPerMm[2] = float.Parse(value); break;
                    case 110: MaxRate[0] = float.Parse(value); break;
                    case 111: MaxRate[1] = float.Parse(value); break;
                    case 112: MaxRate[2] = float.Parse(value); break;
                    case 120: Acceleration[0] = float.Parse(value); break;
                    case 121: Acceleration[1] = float.Parse(value); break;
                    case 122: Acceleration[2] = float.Parse(value); break;
                    case 130: MaxTravel[0] = float.Parse(value); break;
                    case 131: MaxTravel[1] = float.Parse(value); break;
                    case 132: MaxTravel[2] = float.Parse(value); break;
                }
            }
            return "SETTING" + settingNumber;
        }
        public string SetCommand(int settingNumber, object value)
        {
            string command = $"${settingNumber}={value}";
            return command;
        }
        public string WriteAllSettings(SerialPort serialPort)
        {
            var sb = new StringBuilder();
            sb.AppendLine(SetCommand(0, StepPulseTime));
            sb.AppendLine(SetCommand(1, StepIdleDelay));
            sb.AppendLine(SetCommand(2, StepPulseInvertMask));
            sb.AppendLine(SetCommand(3, DirectionPortInvertMask));
            sb.AppendLine(SetCommand(10, StatusReportMask));
            sb.AppendLine(SetCommand(11, JunctionDeviation));
            sb.AppendLine(SetCommand(12, ArcTolerance));
            sb.AppendLine(SetCommand(13, ReportInches ? 1 : 0));
            sb.AppendLine(SetCommand(20, SoftLimitsEnabled ? 1 : 0));
            sb.AppendLine(SetCommand(21, HardLimitsEnabled ? 1 : 0));
            sb.AppendLine(SetCommand(22, HomingCycleEnabled ? 1 : 0));
            sb.AppendLine(SetCommand(23, HomingDirectionInvertMask));
            sb.AppendLine(SetCommand(24, HomingFeedRate));
            sb.AppendLine(SetCommand(25, HomingSeekRate));
            sb.AppendLine(SetCommand(26, HomingDebounceDelay));
            sb.AppendLine(SetCommand(27, HomingPullOff));
            sb.AppendLine(SetCommand(30, MaxSpindleSpeed));
            sb.AppendLine(SetCommand(31, MinSpindleSpeed));
            sb.AppendLine(SetCommand(32, LaserModeEnabled ? 1 : 0));
            sb.AppendLine(SetCommand(100, StepsPerMm[0]));
            sb.AppendLine(SetCommand(101, StepsPerMm[1]));
            sb.AppendLine(SetCommand(102, StepsPerMm[2]));
            sb.AppendLine(SetCommand(110, MaxRate[0]));
            sb.AppendLine(SetCommand(111, MaxRate[1]));
            sb.AppendLine(SetCommand(112, MaxRate[2]));
            sb.AppendLine(SetCommand(120, Acceleration[0]));
            sb.AppendLine(SetCommand(121, Acceleration[1]));
            sb.AppendLine(SetCommand(122, Acceleration[2]));
            sb.AppendLine(SetCommand(130, MaxTravel[0]));
            sb.AppendLine(SetCommand(131, MaxTravel[1]));
            sb.AppendLine(SetCommand(132, MaxTravel[2]));
            return sb.ToString();
        }
    }
}
