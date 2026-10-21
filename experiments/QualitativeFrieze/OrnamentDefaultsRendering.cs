using System.Globalization;
using System.Net;
using System.Text;
using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Optional display of executed native position words and the same
/// explicit envelope offered to the defaults reader. Drawing never supplies
/// policy truth. Outside participation remains in view rather than clipped.</summary>
public static class OrnamentDefaultsRendering
{
    private readonly record struct P(double X, double Y);
    private static readonly string[] Colours = ["#22677d", "#ad634b", "#778144", "#81547e", "#b68b38", "#3d8171", "#506398", "#9c5971"];

    public static void Save(OrnamentReconstructionCases.Sample sample, string path, bool groups, int envelopeHalfSpan = 8, bool showEnvelope = true)
    {
        if (envelopeHalfSpan < 1) throw new ArgumentOutOfRangeException(nameof(envelopeHalfSpan));
        var positions = E(sample.Bindings).Cast<Relation>().ToDictionary(row => row.Left,
            row => Position((Relation)row.Right), N.Participants.Instance);
        var minX = positions.Values.Min(p => p.X); var minY = positions.Values.Min(p => p.Y);
        var maxX = positions.Values.Max(p => p.X); var maxY = positions.Values.Max(p => p.Y);
        if (showEnvelope)
        {
            minX = Math.Min(-envelopeHalfSpan, minX); minY = Math.Min(-envelopeHalfSpan, minY);
            maxX = Math.Max(envelopeHalfSpan, maxX); maxY = Math.Max(envelopeHalfSpan, maxY);
        }
        else
        {
            var horizontalMargin = Math.Max(1, maxX - minX) * .1;
            var verticalMargin = Math.Max(1, maxY - minY) * .1;
            minX -= horizontalMargin; maxX += horizontalMargin; minY -= verticalMargin; maxY += verticalMargin;
        }
        const double width = 720, height = 420;
        var scale = Math.Min((width - 64) / (maxX - minX), (height - 88) / (maxY - minY));
        var dx = (width - (maxX - minX) * scale) / 2;
        var dy = 45 + (height - 88 - (maxY - minY) * scale) / 2;
        P Screen(P point) => new(dx + (point.X - minX) * scale, height - dy - (point.Y - minY) * scale);
        var groupRows = E(sample.MechanicalGroups).Cast<Relation>().ToArray();
        var strokes = E(sample.Graph.Strokes).Cast<Relation>().ToArray();
        var painted = new Dictionary<IElement, (P A, P B, string Colour)>(N.Participants.Instance);
        var svg = new StringBuilder("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 720 420' role='img'>");
        svg.Append("<title>").Append(WebUtility.HtmlEncode(sample.Spec.Name + (showEnvelope ? " · explicit native-word envelope" : " · executed native structure")))
            .Append("</title><rect width='720' height='420' fill='#faf8ee'/>");
        if (showEnvelope)
        {
            var topLeft = Screen(new(-envelopeHalfSpan, envelopeHalfSpan));
            var bottomRight = Screen(new(envelopeHalfSpan, -envelopeHalfSpan));
            svg.Append($"<rect class='selected-envelope' x='{F(topLeft.X)}' y='{F(topLeft.Y)}' width='{F(bottomRight.X - topLeft.X)}' height='{F(bottomRight.Y - topLeft.Y)}' fill='#e8eee8' fill-opacity='.28' stroke='#9d9986' stroke-width='1.4' stroke-dasharray='6 5'/>");
            var origin = Screen(new(0, 0));
            svg.Append($"<path class='word-origin' d='M{F(origin.X - 4)},{F(origin.Y)}h8 M{F(origin.X)},{F(origin.Y - 4)}v8' fill='none' stroke='#b4b1a3' stroke-width='1'/>");
            svg.Append($"<text x='18' y='23' font-size='12' font-family='system-ui,sans-serif' fill='#716f62'>Chosen envelope: −{envelopeHalfSpan} to +{envelopeHalfSpan} common-reach words on each axis</text>");
        }
        else svg.Append("<text x='18' y='23' font-size='12' font-family='system-ui,sans-serif' fill='#716f62'>Executed native structure · fitted view, no scene boundary shown</text>");

        foreach (var stroke in strokes)
        {
            if (!QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _) ||
                !sample.Publisher.TryMapSegment(stroke, out var observed))
                throw new InvalidOperationException("Rendering requires the actual executed stroke and published correspondence.");
            var first = Screen(positions[from]); var second = Screen(positions[to]);
            var group = Array.FindIndex(groupRows, row => E((Relation)((Relation)row.Right).Left).Any(edge => ReferenceEquals(edge, observed)));
            var colour = groups && group >= 0 ? Colours[group % Colours.Length] : "#325660";
            painted.Add(observed, (first, second, colour));
            svg.Append($"<line class='actual-segment' x1='{F(first.X)}' y1='{F(first.Y)}' x2='{F(second.X)}' y2='{F(second.Y)}' stroke='{colour}' stroke-width='3.6' stroke-linejoin='round' stroke-linecap='round'/>");
        }
        // A declaration supplies an encounter, not an over/under order. Its
        // ring is an annotation rather than an invented bridge or joined site.
        foreach (var row in E(sample.Observation.Crossings).OfType<Relation>())
        {
            var a = painted[row.Left]; var b = painted[row.Right];
            var ax = a.B.X - a.A.X; var ay = a.B.Y - a.A.Y; var bx = b.B.X - b.A.X; var by = b.B.Y - b.A.Y;
            var denominator = ax * by - ay * bx;
            if (Math.Abs(denominator) < 1e-9) continue;
            var t = ((b.A.X - a.A.X) * by - (b.A.Y - a.A.Y) * bx) / denominator;
            var u = ((b.A.X - a.A.X) * ay - (b.A.Y - a.A.Y) * ax) / denominator;
            if (t is < 0 or > 1 || u is < 0 or > 1) continue;
            var crossing = new P(a.A.X + t * ax, a.A.Y + t * ay);
            svg.Append($"<circle class='declared-crossing' cx='{F(crossing.X)}' cy='{F(crossing.Y)}' r='6' fill='none' stroke='#8c6596' stroke-width='1.7'/>");
        }
        foreach (var coincident in positions.GroupBy(row => row.Value))
        {
            var point = Screen(coincident.Key);
            if (coincident.Count() > 1)
                svg.Append($"<circle class='unjoined-position' cx='{F(point.X)}' cy='{F(point.Y)}' r='4.6' fill='#faf8ee' stroke='#94729d' stroke-width='1.5'/>");
            else
            {
                var site = coincident.First().Key;
                var degree = strokes.Count(stroke =>
                {
                    QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _);
                    return N.Same(from, site) || N.Same(to, site);
                });
                if (degree > 2) svg.Append($"<circle class='shared-site' cx='{F(point.X)}' cy='{F(point.Y)}' r='3.3' fill='#ae784e'/>");
            }
        }
        svg.Append("<text x='18' y='403' font-size='11' font-family='system-ui,sans-serif' fill='#716f62'>Copper dots: shared branches · violet rings: declared crossing or unjoined coincident sites</text></svg>");
        File.WriteAllText(path, svg.ToString());

        P Position(Relation word)
        {
            var x = 0d; var y = 0d;
            foreach (var item in E(word))
            {
                if (item is not Relation move || !sample.Frame.Direction(move, out var axis, out var forward, out _))
                    throw new InvalidOperationException("This optional display requires complete native position words.");
                if (ReferenceEquals(axis, sample.Frame.Horizontal)) x += forward ? 1 : -1;
                else y += forward ? 1 : -1;
            }
            return new(x, y);
        }
    }

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
