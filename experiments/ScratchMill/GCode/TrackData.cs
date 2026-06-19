using ScratchMill.KiCad;
using ScratchMill.KiCad.Elements;
using ScratchMill.Utils;
using SkiaSharp;
using System.Diagnostics;
using System.Text;

namespace ScratchMill.GCode;

public interface IKcData
{
    public SKPath BasePath { get; }
    public string Svg { get; set; }
    public string GCode { get; set; }
    public int ContourIndex { get; set; }
    public PassKind PassKind { get; }
    public bool IsRecutPass { get; set; }
    public bool OverlapTail { get; set; }
}
public class TrackData : IKcData
{
    public KcNetTracks Track { get; }
    public List<SKPath> BasePaths { get; }
    public SKPath BasePath { get; private set; }
    public SKRect Bounds { get; private set; }
    private SKMatrix Transform { get; set; }
    public float ContourOffset { get; }
    public int ContourIndex { get; set; }
    public string Svg { get; set; }
    public string GCode { get; set; }
    public PassKind PassKind { get; } = PassKind.Track;
    public bool IsRecutPass { get; set; } = false;
    public bool OverlapTail { get; set; } = true;
    public SKPath OutlinePath { get; private set; }
    public List<SKPath> PathContours { get; } = new List<SKPath>();
    public SKPoint? TestPadCenter => Track.LargestPad?.Center;

    public TrackData(KcNetTracks track, SKMatrix transform, PassKind passKind, bool overlapTail = true)
    {
        Track = track;
        Transform = transform;
        OverlapTail = overlapTail;
        PassKind = passKind;
        OutlinePath = new SKPath();
        BasePaths = ToSkia();
        BasePath = GetContourPath(BasePaths, Transform, 0);
    }
    public void Reset()
    {

    }
    public void GenerateContours(float[] countourOffsets)
    {
        PathContours.Clear();

        foreach (var contourOffset in countourOffsets)
        {
            var contourPath = GetContourPath(BasePaths, Transform, contourOffset);
            PathContours.Add(contourPath);

            if (Bounds.IsEmpty)
            {
                Bounds = contourPath.Bounds;
            }
            else
            {
                Bounds.Union(contourPath.Bounds);
            }
        }
    }
    public void GenerateFillPath(float contourOffset = 0)
    {
        var outlinePath = new SKPath();
        float absOffset = 0;
        foreach (var elementPath in BasePaths)
        {
            var shape = new SKPath();
            var pathAndOutline = new SKPath[2];
            shape.FillType = SKPathFillType.Winding;
            int contourCount = 0;
            _shapePaint.GetFillPath(elementPath, outlinePath);

            var rim = 0.2f * Transform.ScaleX; // offset for first round so it doesn't hit track
            absOffset = (Math.Abs(contourOffset) + rim) * Transform.ScaleX; // shrink slightly for paste pads
            _wirePaint.StrokeWidth = Math.Abs(contourOffset) < 1f ? rim : absOffset;// use only rim if a scribe bit

            _wirePaint.GetFillPath(elementPath, outlinePath);
            var clearingPass = elementPath.Op(outlinePath, SKPathOp.Difference);
            OutlinePath.AddPath(clearingPass);

            var lastValidPass = clearingPass;
            var minDim = Math.Min(clearingPass.Bounds.Width, clearingPass.Bounds.Height);
            while (contourCount == 0 || minDim > absOffset)
            {
                shape.AddPath(clearingPass);
                lastValidPass = clearingPass;
                var stepOver = 1.6f; //(minDim < absOffset * 2) ? 1.25f : 1.6f;
                absOffset = Math.Abs(contourOffset * stepOver) * Transform.ScaleX;
                _wirePaint.StrokeWidth = absOffset;
                outlinePath.Reset();
                _wirePaint.GetFillPath(clearingPass, outlinePath);
                clearingPass = clearingPass.Op(outlinePath, SKPathOp.Difference);
                contourCount++;
                minDim = Math.Min(clearingPass.Bounds.Width, clearingPass.Bounds.Height);
            }
            shape.AddPoly( // make sure the center is cleared.
                [
                new SKPoint(lastValidPass.Bounds.MidX, lastValidPass.Bounds.Top),
                new SKPoint(lastValidPass.Bounds.MidX, lastValidPass.Bounds.Bottom),
                new SKPoint(lastValidPass.Bounds.Left, lastValidPass.Bounds.MidY),
                new SKPoint(lastValidPass.Bounds.Right,lastValidPass.Bounds.MidY)
                ],
                false);
            var result = shape.EnforceMaxSegLength();
            PathContours.Add(result);
        }
    }
    private static SkiaToGCode _skiaToGCode = new SkiaToGCode();
    public float ManualZDepth { get; set; } = 0;
    public string ToGCode(SKMatrix transform, float zDepth, float nextCountourDiff = 0f)
    {
        var result = "";
        foreach (var path in PathContours)
        {
            var gcode = _skiaToGCode.ConvertToGCode(path, this, transform, zDepth);
            result += gcode;
            zDepth += nextCountourDiff;
        }
        return result;
    }
    #region Outlines
    public static SKPath GetContourPath(List<SKPath> basePaths, SKMatrix transform, float contourOffset = 0)
    {
        SKPath workingPath = new SKPath();
        var outlinePath = new SKPath();
        foreach (var elementPath in basePaths)
        {
            _shapePaint.GetFillPath(elementPath, outlinePath);
            workingPath = workingPath.Op(outlinePath, SKPathOp.Union);
            if (contourOffset != 0)
            {
                _wirePaint.StrokeWidth = Math.Abs(contourOffset) * transform.ScaleX;
                _wirePaint.GetFillPath(elementPath, outlinePath);
                workingPath = workingPath.Op(outlinePath, SKPathOp.Union);
            }
        }
        workingPath.Close();
        var result = workingPath.EnforceMaxSegLength();

        return result;
    }
    #endregion
    #region SVG
    public string ToSvg(bool writeToDisk = false, string? outputDirectory = null) // for debugging
    {
        SKRect svgBounds = GetContourPath(BasePaths, Transform).Bounds;

        float padding = Math.Max(svgBounds.Width, svgBounds.Height) * 0.1f;
        float viewBoxMinX = svgBounds.Left - padding;
        float viewBoxMinY = svgBounds.Top - padding;
        float viewBoxWidth = svgBounds.Width + 2 * padding;
        float viewBoxHeight = svgBounds.Height + 2 * padding;

        int svgWidth = 800;
        int svgHeight = (int)(svgWidth * (viewBoxHeight / viewBoxWidth));

        StringBuilder sb = new StringBuilder();

        sb.AppendLine($@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""{svgWidth}"" height=""{svgHeight}"" viewBox=""{viewBoxMinX} {viewBoxMinY} {viewBoxWidth} {viewBoxHeight}"" fill=""none"">");

        for (int i = 0; i < BasePaths.Count; i++)
        {
            float hue = (i * 360f / BasePaths.Count) % 360;
            var hsl = SKColor.FromHsl(hue, 1.0f * 100, 0.5f * 100);
            //var (r, g, b) = color.Red HslToRgb(hue, 1.0f, 0.5f);
            string color = $"rgba({hsl.Red}, {hsl.Green}, {hsl.Blue}, 0.5)";

            sb.AppendLine($@"<path d=""{BasePaths[i].ToSvgPathData()}"" fill=""none"" stroke=""{color}"" stroke-width=""0.1"" />");
        }

        sb.AppendLine("</svg>");

        if (writeToDisk && Track.Elements.Count > 0)
        {
            var netId = Track.Elements[0].NetId;
            var targetDirectory = outputDirectory ?? Path.Combine(AppContext.BaseDirectory, "SvgDebug");
            var path = Path.Combine(targetDirectory, $"track_{netId}.svg");
            try
            {
                Directory.CreateDirectory(targetDirectory);
                File.WriteAllText(path, sb.ToString());
            }
            catch
            {
                Trace.WriteLine($"Could not save {path}.");
            }
        }
        return sb.ToString();
    }
    #endregion
    private (float r, float g, float b) HslToRgb(float h, float s, float l)
    {
        float c = (1 - Math.Abs(2 * l - 1)) * s;
        float x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        float m = l - c / 2;
        float r = 0, g = 0, b = 0;

        if (h >= 0 && h < 60) { r = c; g = x; b = 0; }
        else if (h >= 60 && h < 120) { r = x; g = c; b = 0; }
        else if (h >= 120 && h < 180) { r = 0; g = c; b = x; }
        else if (h >= 180 && h < 240) { r = 0; g = x; b = c; }
        else if (h >= 240 && h < 300) { r = x; g = 0; b = c; }
        else if (h >= 300 && h < 360) { r = c; g = 0; b = x; }

        return (r + m, g + m, b + m);
    }

    private static SKPaint _shapePaint = new SKPaint
    {
        Style = SKPaintStyle.Fill,
    };
    private static SKPaint _wirePaint = new SKPaint
    {
        Style = SKPaintStyle.Stroke,
        StrokeCap = SKStrokeCap.Round,
        StrokeJoin = SKStrokeJoin.Round,
        StrokeWidth = 10, // to be set by wires
    };
    #region Base Path Generation
    private List<SKPath> ToSkia()
    {
        var result = new List<SKPath>();
        foreach (var shapeElements in Track.ClosedShapes)
        {
            if (shapeElements.Count > 0)
            {
                for (int i = 0; i < shapeElements.Count; i++)
                {
                    var element = shapeElements[i];
                    if (element is KcSegment seg)
                    {
                        result.Add(SegmentPath(seg));
                    }
                    else if (element is KcArc arc)
                    {
                        result.Add(ArcPath(arc));
                    }
                    else if (element is KcPad pad)
                    {
                        result.Add(pad.PadPath(Transform));
                    }
                    else if (element is KcVia via)
                    {
                        result.Add(ViaPath(via));
                    }
                    else if (element is KcZone zone)
                    {
                        if (zone.IsTeardrop)
                        {
                            result.Add(TeardropPath(zone));
                        }
                    }
                    else
                    {
                        var curPt = element.At.SKPointTransformed(Transform);
                        //result.Add(AddCircle(curPt.X, curPt.Y, 4));
                    }
                }
            }
        }
        return result;
    }
    private SKPath SegmentPath(KcSegment seg)
    {
        var result = new SKPath();
        using (var outlinePath = new SKPath())
        {
            outlinePath.MoveTo(seg.At.SKPointTransformed(Transform));
            outlinePath.LineTo(seg.End.SKPointTransformed(Transform));
            _wirePaint.StrokeWidth = (float)seg.Width * Transform.ScaleX;
            _wirePaint.GetFillPath(outlinePath, result);
        }
        return result;
    }
    private SKPath ArcPath(KcArc arc)
    {
        var result = new SKPath();
        var start = arc.At.SKPoint();
        var mid = arc.Mid.SKPoint();
        var end = arc.End.SKPoint();

        var center = start.CalculateCircleCenter(mid, end);
        var radius = CalculateArcRadius(start, mid, end);
        //var radius = (float)Math.Sqrt(Math.Pow(center.X - start.X, 2) + Math.Pow(center.Y - start.Y, 2));

        var startAngle = center.CalculateAngle(start);
        var endAngle = center.CalculateAngle(end);

        var sweepAngle = endAngle - startAngle;

        if (sweepAngle < 0) sweepAngle += 360;
        if (sweepAngle > 180)//&& midAngle != (startAngle + endAngle) / 2)
        {
            sweepAngle -= 360;
        }

        var tl = new SKPoint(center.X - radius, center.Y - radius).SKPointTransformed(Transform);
        var br = new SKPoint(center.X + radius, center.Y + radius).SKPointTransformed(Transform);
        var rect = new SKRect(tl.X, tl.Y, br.X, br.Y);

        using (var outlinePath = new SKPath())
        {
            outlinePath.MoveTo(arc.At.SKPointTransformed(Transform));
            outlinePath.AddArc(rect, startAngle, sweepAngle);
            _wirePaint.StrokeWidth = (float)arc.Width * Transform.ScaleX;
            _wirePaint.GetFillPath(outlinePath, result);
        }
        return result;
    }
    private float CalculateArcRadius(SKPoint start, SKPoint mid, SKPoint end)
    {
        float a = start.X * start.X + start.Y * start.Y;
        float b = mid.X * mid.X + mid.Y * mid.Y;
        float c = end.X * end.X + end.Y * end.Y;

        float det = start.X * (mid.Y - end.Y) + mid.X * (end.Y - start.Y) + end.X * (start.Y - mid.Y);
        float denom = 2 * det;

        float centerX = ((a * (mid.Y - end.Y)) + (b * (end.Y - start.Y)) + (c * (start.Y - mid.Y))) / denom;
        float centerY = ((a * (end.X - mid.X)) + (b * (start.X - end.X)) + (c * (mid.X - start.X))) / denom;

        // Compute the radius as the distance from the center to the start point
        float radius = (float)Math.Sqrt(Math.Pow(start.X - centerX, 2) + Math.Pow(start.Y - centerY, 2));

        return radius;
    }
    private SKPath ViaPath(KcVia via)
    {
        var result = new SKPath();
        var curPt = via.At.SKPointTransformed(Transform);
        result.MoveTo(curPt);
        result.AddCircle(curPt.X, curPt.Y, (float)via.Size.Width * Transform.ScaleX);
        return result;
    }
    private SKPath TeardropPath(KcZone teardrop)
    {
        var result = new SKPath();
        var pts = teardrop.Polygon.SKPointsTransformed(Transform);
        result.MoveTo(pts[0]);
        result.AddPoly(pts);
        return result;
    }
    #endregion

}
