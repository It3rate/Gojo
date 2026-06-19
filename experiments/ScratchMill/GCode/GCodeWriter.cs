using ScratchMill.Machine;
using ScratchMill.Utils;
using SkiaSharp;
using System.Text;

namespace ScratchMill.GCode;

public class GCodeWriter
{
    public static int ContinuityProbeRate = 400;
    public static int FastProbeRate = 100;
    public static int SlowProbeRate = 45;
    public static int ZSafeHeight = 5;
    public static int PreProbeSafeHeight = 15;

    public static int HorzCutSpeed = 300;
    public static int SpindleScratchSpeed = 0;
    public static int SpindleCutSpeed = 10000;
    public static int SpindleDrillSpeed = 10000;
    public static int SpindleDelay = 2;

    public static string _resolution = "0.###";

    private readonly SkiaToGCode skiaToGCode = new();

    public ScratchJobs Jobs { get; }

    public string GCode { get; private set; } = "";

    public GCodeWriter(ScratchJobs? jobs = null)
    {
        Jobs = jobs ?? new ScratchJobs();
    }

    public void Reset()
    {
        skiaToGCode.Reset();
        GCode = "";
    }

    public string WriteAll()
    {
        Reset();

        var sb = new StringBuilder();
        AddHeader(sb);
        AddSide(BoardSide.Front, sb);
        AddSide(BoardSide.Back, sb);
        AddDrills(Jobs.DrillJob, Jobs.FrontTransform, sb);
        AddFooter(sb);

        GCode = sb.ToString();
        return GCode;
    }

    public string WriteTracks(IEnumerable<TrackData> tracks, SKMatrix transform)
    {
        var sb = new StringBuilder();
        TracksToGCode(tracks, transform, sb);
        return sb.ToString();
    }

    public string WriteGraphics(IEnumerable<GraphicData> graphics, SKMatrix transform)
    {
        var sb = new StringBuilder();
        AddGraphics(graphics, transform, sb, "graphics");
        return sb.ToString();
    }

    private void AddSide(BoardSide side, StringBuilder sb)
    {
        var transform = Jobs.GetTransformForSide(side);
        var sideName = side.IsBottom() ? "bottom" : "top";

        AddTrackPass(Jobs.GetTrackContoursForSide(side), transform, $"{sideName} track contours", sb);
        AddTrackPass(Jobs.GetCleanupForSide(side), transform, $"{sideName} cleanup contours", sb);
        AddTrackList(Jobs.GetMasksForSide(side), transform, $"{sideName} mask removal", sb);
        AddGraphics(Jobs.GetGraphicsForSide(side), transform, sb, $"{sideName} graphics");
        AddGraphics(Jobs.GetEdgeScratchesForSide(side), transform, sb, $"{sideName} edge cuts");
    }

    private void AddTrackPass(IEnumerable<List<TrackData>> batches, SKMatrix transform, string label, StringBuilder sb)
    {
        var any = false;
        foreach (var batch in batches)
        {
            if (!any)
            {
                sb.AppendLine();
                sb.AppendLine($"; Start {label}");
                any = true;
            }

            TracksToGCode(batch, transform, sb);
        }

        if (any)
        {
            sb.AppendLine($"; End {label}");
        }
    }

    private void AddTrackList(IEnumerable<TrackData> tracks, SKMatrix transform, string label, StringBuilder sb)
    {
        var list = tracks.ToList();
        if (list.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine($"; Start {label}");
        TracksToGCode(list, transform, sb);
        sb.AppendLine($"; End {label}");
    }

    private void TracksToGCode(IEnumerable<TrackData> tracks, SKMatrix transform, StringBuilder sb)
    {
        foreach (var track in tracks)
        {
            foreach (var path in track.PathContours)
            {
                sb.Append(skiaToGCode.ConvertToGCode(path, track, transform));
            }
        }
    }

    private void AddGraphics(IEnumerable<GraphicData> graphics, SKMatrix transform, StringBuilder sb, string label)
    {
        var list = graphics.ToList();
        if (list.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine($"; Start {label}");
        foreach (var graphic in list)
        {
            if (graphic.BasePath.IsEmpty)
            {
                graphic.GeneratePath();
            }

            sb.Append(skiaToGCode.ConvertToGCode(graphic.BasePath, graphic, transform));
        }

        sb.AppendLine($"; End {label}");
    }

    private void AddDrills(IReadOnlyList<DrillData> drills, SKMatrix transform, StringBuilder sb)
    {
        if (drills.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine("; Start drills");
        SetSpindleToDrill(sb);

        foreach (var drill in drills)
        {
            var transformedLoc = drill.HoleLocation.SKPointTransformed(transform);
            var strX = transformedLoc.X.ToString(_resolution);
            var strY = transformedLoc.Y.ToString(_resolution);
            var drillDepth = (-(Jobs.CarveSettings.PcbHeight + 0.2f)).ToString(_resolution);
            var padOrVia = drill.PassKind == PassKind.Via ? "Via" : "Pad";
            sb.AppendLine($@"
; Drill {padOrVia} Hole
G00 X{strX} Y{strY}
G01 Z{drillDepth}
G01 Z0
G00 Z2");
        }

        AddPostDrill(sb);
        sb.AppendLine("; End drills");
    }

    public void AddHeader(StringBuilder sb, bool probeFirst = true, bool autolevelFirst = true)
    {
        sb.AppendLine(GetStartScratchBlock);
    }

    public void AddFooter(StringBuilder sb, string manualHeader = "")
    {
        sb.AppendLine(GetEndScratchBlock);
    }

    private void SetSpindleToDrill(StringBuilder sb)
    {
        sb.AppendLine(GetPreDrill);
    }

    private void AddPostDrill(StringBuilder sb)
    {
        sb.AppendLine(GetPostDrill);
    }

    public static string GetStartScratchBlock = @$"
G21
G90
G94
G01 F1000 ; default scratch speed for UGS preview
(HorzScratchSpeed)
M5 ; motor off, so no speed control S{SpindleScratchSpeed} ";

    public static string GetEndScratchBlock = @$"
G00 Z2.0000
M5
G00 Z20.00
M30
%";

    public static string GetStartCutBlock = @$"
%
G21
G90
G94
G01 F{HorzCutSpeed}
S{SpindleCutSpeed} M3
G4 P {SpindleDelay}; ramp up spindle speed";

    public static string GetEndCutBlock = @$"
G00 Z2.0000
M5
G00 Z20.00
M30
%";

    public static string GetPreDrill = @$"
; Turn on spindle and warmup
G01 F300.00
G00 Z10.00
S{SpindleDrillSpeed} M03
G4 P{SpindleDelay} ; ramp up spindle speed";

    public static string GetPostDrill = @$"
M05
G00 Z2.00";

    public static string GetToolChange(string msg, SKPoint probeLocation)
    {
        return $@"
; Start Tool Change
(DialogSafe {msg})
G49
G90
G0 X{probeLocation.X.ToString(_resolution)} Y{probeLocation.Y.ToString(_resolution)}
G0 Z{PreProbeSafeHeight.ToString(_resolution)}
(ProbeAndZeroZ)
; End Tool Change
";
    }

    public static string GetProbe(float xPos, float yPos, bool appendRetract)
    {
        return $@"
; Start Probe
M5 G91
{FastProbe()}
{SlowProbe()}
{Dwell()}
{ZeroOutAxis(false, false, true)}
; End Probe
";
    }

    public static string RetractZ()
    {
        return $@"
G91 G0 Z{ZSafeHeight.ToString(_resolution)} F500
G90";
    }

    public static string FastProbe(float minDepth = -15)
    {
        return $@"
G38.2 Z{minDepth.ToString(_resolution)} F{FastProbeRate.ToString(_resolution)}";
    }

    public static string SlowProbe(float minDepth = -5)
    {
        return $@"
G38.2 Z{minDepth.ToString(_resolution)} F{SlowProbeRate.ToString(_resolution)}";
    }

    public static string ContinuityProbe(float minDepth = -15)
    {
        return $@"
G38.2 Z{minDepth.ToString(_resolution)} F{ContinuityProbeRate.ToString(_resolution)}";
    }

    public static string Dwell(float seconds = 1f)
    {
        return $@"G4 P{seconds.ToString(_resolution)}; Dwell time";
    }

    public static string ZeroOutAxis(bool zeroX, bool zeroY, bool zeroZ)
    {
        var x = zeroX ? "X0" : "";
        var y = zeroY ? "Y0" : "";
        var z = zeroZ ? "Z0" : "";
        return "G90 G54 G10 L20 P0 " + x + y + z + "; zero axis";
    }
}
