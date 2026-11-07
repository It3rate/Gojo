using System.Text.Json.Serialization;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>The same projected X support with different native incidence.
/// A shared four-arm site supplies topology, without an automatic route choice
/// or material transfer law. Crossing geometry alone never supplies that site.</summary>
public static class WeaveJunctionStudy
{
    // surface = frame -> (Closed(actual sections) -> Closed(projected crossings))
    // Every endpoint site belongs to its actual section. No diagnostic join bit
    // is available to the reader; component and degree counts follow aliases.
    public readonly record struct Point(int X, int Y);
    public sealed record Segment(int Id, Point From, Point To, int FromSite, int ToSite);
    public sealed record Site(int Id, Point At, int Degree);
    public sealed record Fixture(Curve.Frame Frame, Relation Surface);
    public sealed record Case(string Id, string Name, string Explanation, Segment[] Segments,
        Site[] Sites, int MaterialComponents, int Ends, int BranchSites, int ProjectedCrossings,
        string Routing, [property: JsonIgnore] Relation Surface);

    public static Fixture Build(bool joined)
    {
        var frame = new Curve.Frame(new QualitativeMotionFrame());
        IElement a = new Placeholder(), b = new Placeholder(), c = new Placeholder(), d = new Placeholder();
        var aw = frame.Word(-3, -3); var bw = frame.Word(3, 3);
        var cw = frame.Word(-3, 3); var dw = frame.Word(3, -3);
        Relation[] sections; Relation[] crossings;
        if (joined)
        {
            var center = new Placeholder(); var at = frame.Word(0, 0);
            sections = [frame.Line(a, center, aw, at), frame.Line(center, b, at, bw),
                frame.Line(c, center, cw, at), frame.Line(center, d, at, dw)];
            crossings = [];
        }
        else
        {
            sections = [frame.Line(a, b, aw, bw), frame.Line(c, d, cw, dw)];
            crossings = [Band.Crossing(frame, sections[0], sections[1], frame.Word(0, 0)).Surface];
        }
        return new(frame, new Relation(frame.Surface,
            new Relation(S(sections.Cast<IElement>().ToArray()), S(crossings.Cast<IElement>().ToArray()))));
    }

    public static Case Read(Fixture fixture, string id = "", string name = "", string explanation = "")
    {
        var frame = fixture.Frame;
        if (fixture.Surface is not { Left: var selected, Right: Relation { Left: Relation sectionScope, Right: Relation crossScope } } ||
            !Same(selected, frame.Surface) || !Entries(sectionScope, out var source) || source.Length is < 1 or > 16 ||
            source.Any(x => x is not Relation) || !Entries(crossScope, out var rawCrossings) || rawCrossings.Any(x => x is not Relation))
            throw new ArgumentException("A finite exposed section and crossing scope is required.", nameof(fixture));
        var read = source.Cast<Relation>().Select(frame.Read).ToArray();
        if (read.Any(r => !r.Complete || r.Controls.Length != 2 || r.Controls[0] == r.Controls[1]))
            throw new ArgumentException("This small comparison reads complete nonzero straight sections.", nameof(fixture));
        var identities = new List<IElement>(); var positions = new List<Curve.Vector>(); var degrees = new List<int>();
        int SiteIndex(IElement site, Curve.Vector position)
        {
            int index = identities.FindIndex(value => Same(value, site));
            if (index >= 0)
            {
                if (positions[index] != position) throw new ArgumentException("A shared site has contradictory exposed positions.", nameof(fixture));
                degrees[index]++; return index;
            }
            identities.Add(site); positions.Add(position); degrees.Add(1); return identities.Count - 1;
        }
        var segments = read.Select((r, i) => new Segment(i, P(r.Controls[0]), P(r.Controls[^1]),
            SiteIndex(r.Start, r.Controls[0]), SiteIndex(r.End, r.Controls[^1]))).ToArray();
        int crossings = 0;
        foreach (var raw in rawCrossings.Cast<Relation>())
        {
            var c = Band.ReadCrossing(frame, raw);
            if (!source.Any(section => Same(section, c.First)) || !source.Any(section => Same(section, c.Second)) || c.Crosses != true)
                throw new ArgumentException("A crossing must refer to two actual exposed sections and a justified interior locus.", nameof(fixture));
            crossings++;
        }
        var parent = Enumerable.Range(0, identities.Count).ToArray();
        int Root(int n) { while (parent[n] != n) n = parent[n]; return n; }
        foreach (var segment in segments) parent[Root(segment.ToSite)] = Root(segment.FromSite);
        int components = Enumerable.Range(0, parent.Length).Select(Root).Distinct().Count();
        var sites = Enumerable.Range(0, parent.Length).Select(i => new Site(i, P(positions[i]), degrees[i])).ToArray();
        int branches = sites.Count(s => s.Degree > 2);
        string routing = branches == 0 ?
            "Each full section has its own through-route. Projected crossing supplies neither a join nor a front/back order." :
            "The shared four-arm site permits a junction interpretation. No switch, branch allocation or transfer law is selected.";
        return new(id, name, explanation, segments, sites, components, sites.Count(s => s.Degree == 1),
            branches, crossings, routing, fixture.Surface);
    }

    public static Case[] Cases() =>
    [
        Read(Build(false), "independent-crossing", "Crossing without joining",
            "Two continuous diagonal sections cross in the selected view. Their native endpoint identities remain separate, and this fixture leaves depth order open."),
        Read(Build(true), "shared-junction", "The same ink with a shared junction",
            "Four arms refer to one actual central site. Projected support is the same X, but the connected topology has changed.")
    ];

    private static Point P(Curve.Vector point) => new(checked((int)point.X), checked((int)point.Y));
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static bool Entries(Relation scope, out IElement[] entries) => StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}
