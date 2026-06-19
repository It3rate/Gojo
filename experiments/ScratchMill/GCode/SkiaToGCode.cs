using ScratchMill.Utils;
using SkiaSharp;
using System.Diagnostics;
using System.Text;

namespace ScratchMill.GCode;

public interface GCodeConverter
{
    public void Reset();
    //string ConvertToGCode(SKPath path, IKcData elementData, SKMatrix transform);
}
public class SkiaToGCode : GCodeConverter
{
    protected string _resolution => GCodeWriter._resolution;
    private SKRect _bounds = SKRect.Empty;
    public SkiaToGCode()
    {
    }
    public void Reset()
    {
        _bounds = SKRect.Empty;
    }
    public string ConvertToGCode(SKPath path, IKcData elementData, SKMatrix cncTransform, float defaultZMillDepth = -0.04f)
    {
        bool addTailOverlap = elementData.OverlapTail;
        bool isClearingOperation = elementData.PassKind == PassKind.SolderMask;


        string millDepth;
        if (elementData != null && elementData.PassKind != PassKind.TestPattern)
        {
            var passKind = elementData.IsRecutPass ? (char)PassKind.Recut : (char)elementData.PassKind;
            millDepth = "(depthIndex" + passKind + elementData.ContourIndex + ")";
        }
        else
        {
            millDepth = $"(millDepth {defaultZMillDepth.ToString(_resolution)})";
        }

        var sb = new StringBuilder();
        SKPoint endPoint = SKPoint.Empty;
        SKPoint lastPoint = SKPoint.Empty;
        var segLen = 0f;
        var lineLen = 0f;
        string strX = "";
        string strY = "";
        var curAction = "";
        var prevAction = "";
        var overlaps = new List<string>();
        bool doneFirstMove = false;


        var points = new SKPoint[4];
        using (var iterator = path.CreateIterator(false)) // false = don't force close the path
        {
            SKPathVerb verb;
            do
            {
                var firstPoint = SKPoint.Empty;
                verb = iterator.Next(points);
                var p0 = points[0].SKPointTransformed(cncTransform);
                strX = (p0.X).ToString(_resolution);
                strY = (p0.Y).ToString(_resolution);
                // calc bounds here if needed
                // _bounds = SKRect.Union(_bounds, new SKRect((float)endX, (float)endY, (float)endX, (float)endY));

                switch (verb)
                {
                    case SKPathVerb.Move:
                        segLen = 0;
                        if (isClearingOperation && doneFirstMove) // for clearing, don't lift the z every move.
                        {
                            curAction = $"G01 X{strX} Y{strY}";
                        }
                        else
                        {
                            firstPoint = p0;
                            sb.AppendLine(@$"
(SafeZMove)
G00 X{strX} Y{strY}
(ZSpeed)
{millDepth}
;(Pause1)
(HorzScratchSpeed)");
                        }
                        doneFirstMove = true;
                        break;

                    case SKPathVerb.Line:
                        curAction = $"G01 X{strX} Y{strY}";
                        endPoint = p0;
                        break;

                    // already converted to polyline, so should be no curves.
                    case SKPathVerb.Quad:
                        endPoint = points[2];
                        Trace.WriteLine($"QuadTo: Control=({points[1].X}, {points[1].Y}), End=({points[2].X}, {points[2].Y})");
                        break;

                    case SKPathVerb.Cubic:
                        endPoint = points[3];
                        Trace.WriteLine($"CubicTo: Control1=({points[1].X}, {points[1].Y}), Control2=({points[2].X}, {points[2].Y}), End=({points[3].X}, {points[3].Y})");
                        break;

                    case SKPathVerb.Conic:
                        endPoint = points[2];
                        Trace.WriteLine($"ConicTo: Control=({points[1].X}, {points[1].Y}), End=({points[2].X}, {points[2].Y}), Weight={iterator.ConicWeight}");
                        break;

                    case SKPathVerb.Close:
                        curAction = $"G01 X{strX} Y{strY}"; // line to first point tp close
                        sb.AppendLine(curAction);

                        // not sure if this is at every close, or only at done. Are there multiple tracks in one path? Do they always close?
                        foreach (var overlap in overlaps)
                        {
                            sb.AppendLine(overlap);
                        }
                        curAction = "";
                        prevAction = "";
                        overlaps.Clear();
                        lineLen = 0;
                        segLen = 0;
                        break;

                    case SKPathVerb.Done:
                        break;

                    default:
                        Trace.WriteLine($"Unknown verb: {verb}");
                        break;
                }
                if (curAction != "" && curAction != prevAction)
                {
                    sb.AppendLine(curAction);
                    if (doneFirstMove)
                    {
                        segLen = lastPoint.DistanceTo(endPoint);
                        lastPoint = endPoint;

                        if (addTailOverlap && lineLen < 3) // make overlap distance based eventually
                        {
                            overlaps.Add(curAction);
                        }
                        lineLen += segLen;
                    }
                }
                prevAction = curAction;
                curAction = "";
            } while (verb != SKPathVerb.Done);
        }
        var result = sb.ToString();
        if (elementData != null)
        {
            elementData.GCode = result;
        }
        return result;
    }
}
