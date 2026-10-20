using System.Globalization;
using System.Net;
using System.Text;
using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

public static partial class StructuralEvidenceExperiment
{
    internal static string RenderSample(StructuralEvidenceCases.Sample sample, string title,
        Relation? selectedObservedSegments = null) => Svg(sample, title, selectedObservedSegments);

    private readonly record struct Point(double X, double Y);

    /// <summary>Illustration of an already executed sample. Coincident screen
    /// positions affect marker presentation only; they never create native joins.</summary>
    private static string Svg(StructuralEvidenceCases.Sample sample, string title, Relation? selectedObservedSegments = null)
    {
        var selected = selectedObservedSegments is null ? null : E(selectedObservedSegments)
            .Select(observed => E(sample.Publisher.SegmentMap).Cast<Relation>()
                .Single(map => ReferenceEquals(map.Right, observed)).Left)
            .ToHashSet((IEqualityComparer<IElement>)ReferenceEqualityComparer.Instance);
        var positions = E(sample.Bindings).Cast<Relation>().ToDictionary(row => row.Left,
            row => Position((Relation)row.Right), (IEqualityComparer<IElement>)ReferenceEqualityComparer.Instance);
        var minX = positions.Values.Min(p => p.X); var maxX = positions.Values.Max(p => p.X);
        var minY = positions.Values.Min(p => p.Y); var maxY = positions.Values.Max(p => p.Y);
        var scale = Math.Min(450 / Math.Max(1, maxX - minX), 180 / Math.Max(1, maxY - minY));
        Point P(Point p) => new(280 + (p.X - (minX + maxX) / 2) * scale, 125 - (p.Y - (minY + maxY) / 2) * scale);
        var strokes = E(sample.Graph.Strokes).Cast<Relation>().Select(stroke =>
        {
            if (!QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _))
                throw new InvalidOperationException("An illustration requires an actual executed stroke.");
            return (Stroke: stroke, From: from, To: to);
        }).ToArray();
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' width='560' height='250' viewBox='0 0 560 250' role='img' aria-label='{H(title)}'><title>{H(title)}</title><rect width='100%' height='100%' fill='#fbf8ef'/>");
        foreach (var stroke in strokes)
        {
            var a = P(positions[stroke.From]); var b = P(positions[stroke.To]);
            var retained = selected?.Contains(stroke.Stroke) == true;
            var colour = selected is null ? "#28616a" : retained ? "#315f90" : "#7b8588";
            svg.Append($"<line class='actual-edge{(retained ? " retained-edge" : "")}' x1='{F(a.X)}' y1='{F(a.Y)}' x2='{F(b.X)}' y2='{F(b.Y)}' stroke='{colour}' stroke-width='{(retained ? "7" : "5")}' stroke-linecap='round'/>");
        }
        foreach (var group in positions.GroupBy(row => row.Value))
        {
            var p = P(group.Key); var members = group.ToArray();
            if (members.Length > 1)
            {
                // Two actual endpoint identities can occupy one projected site.
                // The split marker makes this distinction visible without welding.
                svg.Append($"<g class='separate-sites'><title>{members.Length} separate actual sites at one displayed location; no shared joint</title><circle cx='{F(p.X)}' cy='{F(p.Y)}' r='9' fill='#fbf8ef'/><circle cx='{F(p.X - 3.5)}' cy='{F(p.Y)}' r='4.5' fill='#fbf8ef' stroke='#805b91' stroke-width='2'/><circle cx='{F(p.X + 3.5)}' cy='{F(p.Y)}' r='4.5' fill='#fbf8ef' stroke='#805b91' stroke-width='2'/></g>");
                continue;
            }
            var site = members[0].Key;
            var degree = strokes.Count(stroke => ReferenceEquals(stroke.From, site) || ReferenceEquals(stroke.To, site));
            svg.Append($"<circle class='actual-site' cx='{F(p.X)}' cy='{F(p.Y)}' r='{(degree > 1 ? "5.5" : "4")}' fill='{(degree > 1 ? "#b76b45" : "#fbf8ef")}' stroke='{(degree > 1 ? "#b76b45" : "#718780")}' stroke-width='2'><title>{degree} incident edges at this actual site</title></circle>");
        }
        foreach (var row in E(sample.Observation.Crossings).OfType<Relation>())
        {
            if (!sample.Observation.TryReadCrossing(row, out var first, out var second)) continue;
            var firstStroke = E(sample.Publisher.SegmentMap).Cast<Relation>().Single(map => ReferenceEquals(map.Right, first)).Left;
            var secondStroke = E(sample.Publisher.SegmentMap).Cast<Relation>().Single(map => ReferenceEquals(map.Right, second)).Left;
            var a = strokes.Single(stroke => ReferenceEquals(stroke.Stroke, firstStroke));
            var b = strokes.Single(stroke => ReferenceEquals(stroke.Stroke, secondStroke));
            var af = P(positions[a.From]); var at = P(positions[a.To]);
            var bf = P(positions[b.From]); var bt = P(positions[b.To]);
            var dx = at.X - af.X; var dy = at.Y - af.Y;
            var ex = bt.X - bf.X; var ey = bt.Y - bf.Y;
            var denominator = dx * ey - dy * ex;
            if (Math.Abs(denominator) < 1e-9) continue;
            var t = ((bf.X - af.X) * ey - (bf.Y - af.Y) * ex) / denominator;
            var centre = new Point(af.X + t * dx, af.Y + t * dy);
            svg.Append($"<circle class='declared-crossing' cx='{F(centre.X)}' cy='{F(centre.Y)}' r='11' fill='none' stroke='#805b91' stroke-width='2' stroke-dasharray='3 3'><title>Declared crossing without a shared site; over/under is unspecified</title></circle>");
        }
        return svg.Append("</svg>").ToString();

        Point Position(Relation word)
        {
            var x = 0d; var y = 0d;
            foreach (var move in E(word).Cast<Relation>())
            {
                if (!sample.Frame.Direction(move, out var axis, out var forward, out _))
                    throw new InvalidOperationException("Unknown illustrative direction.");
                if (ReferenceEquals(axis, sample.Frame.Horizontal)) x += forward ? 1 : -1;
                else if (ReferenceEquals(axis, sample.Frame.Vertical)) y += forward ? 1 : -1;
                else throw new InvalidOperationException("Uninterpreted illustrative axis.");
            }
            return new(x, y);
        }
    }
    private static string H(string value) => WebUtility.HtmlEncode(value);
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
