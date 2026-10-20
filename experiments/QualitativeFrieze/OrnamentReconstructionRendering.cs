using System.Globalization;
using System.Net;
using System.Text;
using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Optional screen interpretation of executed native words. Neither
/// these positions nor intersection calculations enter recognition or messages.</summary>
public static class OrnamentReconstructionRendering
{
    private readonly record struct P(double X, double Y);
    private static readonly string[] Colours = ["#22677d", "#ad634b", "#778144", "#81547e", "#b68b38", "#3d8171", "#506398", "#9c5971"];
    public static void Save(OrnamentReconstructionCases.Sample sample, string path, bool groups)
    {
        var positions = E(sample.Bindings).Cast<Relation>().ToDictionary(row => row.Left, row => Position((Relation)row.Right),
            (IEqualityComparer<IElement>)ReferenceEqualityComparer.Instance);
        var minX = positions.Values.Min(p => p.X); var minY = positions.Values.Min(p => p.Y);
        var maxX = positions.Values.Max(p => p.X); var maxY = positions.Values.Max(p => p.Y);
        var scale = Math.Min(650 / Math.Max(1, maxX - minX), 300 / Math.Max(1, maxY - minY));
        var dx = (720 - (maxX - minX) * scale) / 2; var dy = (360 - (maxY - minY) * scale) / 2;
        P Screen(P p) => new(dx + (p.X - minX) * scale, 360 - dy - (p.Y - minY) * scale);
        var groupRows = E(sample.MechanicalGroups).Cast<Relation>().ToArray();
        var painted = new Dictionary<IElement, (P A, P B, string Colour)>(ReferenceEqualityComparer.Instance);
        var svg = new StringBuilder("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 720 360' role='img'>");
        svg.Append("<title>").Append(WebUtility.HtmlEncode(sample.Spec.Name + (groups ? " · actual drawing groups" : " · published structure")))
            .Append("</title><rect width='720' height='360' fill='#faf8ee'/>");
        foreach (var item in E(sample.Graph.Strokes).Cast<Relation>())
        {
            QualitativeStrokeGraph.TryReadStroke(item, out _, out var from, out var to, out _);
            var first = Screen(positions[from]); var second = Screen(positions[to]);
            if (!sample.Publisher.TryMapSegment(item, out var observed)) throw new InvalidOperationException("Render needs the explicit publisher correspondence.");
            var group = Array.FindIndex(groupRows, row => E((Relation)((Relation)row.Right).Left).Any(edge => ReferenceEquals(edge, observed)));
            var colour = groups && group >= 0 ? Colours[group % Colours.Length] : "#325660";
            painted[observed] = (first, second, colour); Line(first, second, colour, 4.5);
        }
        foreach (var row in E(sample.Observation.Crossings).Cast<Relation>())
        {
            var a = painted[row.Left]; var b = painted[row.Right];
            var ax = a.B.X - a.A.X; var ay = a.B.Y - a.A.Y; var bx = b.B.X - b.A.X; var by = b.B.Y - b.A.Y;
            var cross = ax * by - ay * bx; if (Math.Abs(cross) < 1e-9) continue;
            var t = ((b.A.X - a.A.X) * by - (b.A.Y - a.A.Y) * bx) / cross;
            var p = new P(a.A.X + t * ax, a.A.Y + t * ay);
            svg.Append($"<circle class='declared-crossing' cx='{F(p.X)}' cy='{F(p.Y)}' r='7' fill='#faf8ee'/>");
            var length = Math.Sqrt(bx * bx + by * by);
            Line(new(p.X - bx / length * 9, p.Y - by / length * 9), new(p.X + bx / length * 9, p.Y + by / length * 9), b.Colour, 4.5, true);
        }
        foreach (var coincident in positions.GroupBy(row => row.Value))
        {
            var p = Screen(coincident.Key);
            if (coincident.Count() > 1)
                svg.Append($"<circle class='unjoined-position' cx='{F(p.X)}' cy='{F(p.Y)}' r='5.5' fill='#faf8ee' stroke='#94729d' stroke-width='1.5'/>");
            else
            {
                var site = coincident.First().Key;
                var degree = E(sample.Graph.Strokes).Cast<Relation>().Count(stroke =>
                {
                    QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _);
                    return ReferenceEquals(from, site) || ReferenceEquals(to, site);
                });
                if (degree > 2) svg.Append($"<circle class='shared-site' cx='{F(p.X)}' cy='{F(p.Y)}' r='3.5' fill='#b37748'/>");
            }
        }
        svg.Append("</svg>"); File.WriteAllText(path, svg.ToString());

        void Line(P a, P b, string colour, double width, bool overlay = false) => svg.Append($"<line class='{(overlay ? "relation-overlay" : "actual-segment")}' x1='{F(a.X)}' y1='{F(a.Y)}' x2='{F(b.X)}' y2='{F(b.Y)}' stroke='{colour}' stroke-width='{F(width)}' stroke-linejoin='round' stroke-linecap='round'/>");
        P Position(Relation word)
        {
            var x = 0d; var y = 0d;
            foreach (var move in E(word).Cast<Relation>())
            {
                if (ReferenceEquals(move.Left, sample.Frame.Right)) x++;
                else if (ReferenceEquals(move.Left, sample.Frame.Left)) x--;
                else if (ReferenceEquals(move.Left, sample.Frame.Up)) y++;
                else if (ReferenceEquals(move.Left, sample.Frame.Down)) y--;
            }
            return new(x, y);
        }
    }
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
