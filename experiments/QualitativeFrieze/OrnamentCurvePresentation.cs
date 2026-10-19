using System.Globalization;
using System.Text;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Optional display fillets over acknowledged chord-guide points.
/// Quadratic controls stay at their original corners; all curves lie within
/// local guide triangles. Joins agree in tangent direction (G1), without a
/// native curvature, parametric C1, occupied-clearance or new-topology claim.</summary>
public static class OrnamentCurvePresentation
{
    public readonly record struct Point(double X, double Y);
    public sealed record Segment(Point Start, Point End, Point? Control = null)
    {
        public bool IsQuadratic => Control.HasValue;
        public Point At(double t)
        {
            if (t is < 0 or > 1 || !double.IsFinite(t)) throw new ArgumentOutOfRangeException(nameof(t));
            var s = 1 - t;
            return Control is { } q ? new(s * s * Start.X + 2 * s * t * q.X + t * t * End.X,
                s * s * Start.Y + 2 * s * t * q.Y + t * t * End.Y) :
                new(s * Start.X + t * End.X, s * Start.Y + t * End.Y);
        }
    }

    /// <summary>Preserves open ends, the closed-path start, supplied fixed
    /// landmarks, straight runs and reversal cusps. Each side of a rounded
    /// corner consumes at most .45 of its own guide edge, leaving a straight
    /// interval between adjacent fillets. No segment crosses between paths.</summary>
    public static IReadOnlyList<Segment> Build(IReadOnlyList<(double X, double Y)> guidePoints, bool closed,
        double rounding = .32, IReadOnlySet<int>? fixedLandmarks = null)
    {
        if (!double.IsFinite(rounding) || rounding is < 0 or > .45) throw new ArgumentOutOfRangeException(nameof(rounding));
        var points = guidePoints.Select(point => new Point(point.X, point.Y)).ToList();
        if (points.Any(point => !double.IsFinite(point.X) || !double.IsFinite(point.Y)))
            throw new ArgumentException("Display guides require finite coordinates.", nameof(guidePoints));
        if (closed && points.Count > 1 && points[^1] != points[0]) points.Add(points[0]);
        if (fixedLandmarks is not null && fixedLandmarks.Any(index => index < 0 || index >= points.Count))
            throw new ArgumentOutOfRangeException(nameof(fixedLandmarks));
        var segments = new List<Segment>();
        if (points.Count < 2) return segments;
        var current = points[0];
        for (var index = 1; index < points.Count - 1; index++)
        {
            var before = points[index - 1]; var corner = points[index]; var after = points[index + 1];
            var ax = corner.X - before.X; var ay = corner.Y - before.Y;
            var bx = after.X - corner.X; var by = after.Y - corner.Y;
            var a = Math.Sqrt(ax * ax + ay * ay); var b = Math.Sqrt(bx * bx + by * by);
            var cross = ax * by - ay * bx; var dot = ax * bx + ay * by;
            if (rounding == 0 || fixedLandmarks?.Contains(index) == true || a <= 1e-12 || b <= 1e-12 ||
                Math.Abs(cross) <= 1e-10 * a * b || dot <= -.985 * a * b)
            { Line(corner); continue; }
            var entry = new Point(corner.X + rounding * (before.X - corner.X), corner.Y + rounding * (before.Y - corner.Y));
            var exit = new Point(corner.X + rounding * (after.X - corner.X), corner.Y + rounding * (after.Y - corner.Y));
            Line(entry); segments.Add(new(entry, exit, corner)); current = exit;
        }
        Line(points[^1]); return segments;

        void Line(Point end) { segments.Add(new(current, end)); current = end; }
    }

    public static string PathData(IReadOnlyList<(double X, double Y)> guidePoints, bool closed,
        double rounding = .32, IReadOnlySet<int>? fixedLandmarks = null)
    {
        var segments = Build(guidePoints, closed, rounding, fixedLandmarks);
        if (guidePoints.Count == 0) return "";
        var start = segments.Count == 0 ? new Point(guidePoints[0].X, guidePoints[0].Y) : segments[0].Start;
        var data = new StringBuilder("M ").Append(F(start.X)).Append(' ').Append(F(start.Y));
        foreach (var segment in segments)
        {
            if (segment.Control is { } control)
                data.Append(" Q ").Append(F(control.X)).Append(' ').Append(F(control.Y)).Append(' ');
            else data.Append(" L ");
            data.Append(F(segment.End.X)).Append(' ').Append(F(segment.End.Y));
        }
        if (closed) data.Append(" Z");
        return data.ToString();
    }
    private static string F(double value) => value.ToString("0.########", CultureInfo.InvariantCulture);
}
