using System.Text.Json.Serialization;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>A bounded observer of exposed fragment ends. Compatible evidence
/// creates an open continuation request; it never welds endpoints or reads a
/// concealed source route. The straight and tangent tests are selected local
/// hypotheses, not an exhaustive interpolation or image-recognition law.</summary>
public static class WeaveContinuationStudy
{
    // port = visible section -> (selected site -> (actual position word -> tangent word))
    // query = (frame -> Closed(visible sections)) ->
    //         (source port -> (Closed(target ports) ->
    //          (Closed(opaque boundary sections) -> Closed(exposed return route))))
    // candidate = (query -> (source port -> target port)) ->
    //             ((boundary -> (position pair -> tangent pair)) -> Open(bridge request))
    // An opaque rectangular scope is an explicit observation premise. A port
    // exposes only its visible section; no source owner or hidden span exists
    // in this request. The return route, when offered, is independently visible.

    public readonly record struct Point(int X, int Y);
    public sealed record Fragment(int Index, Point[] Controls, int StartSite, int EndSite);
    public sealed record Candidate(int Target, Point From, Point To, bool StraightAligned,
        bool TangentCompatible, bool OccluderSupported, bool Eligible, bool NativeJoined,
        bool WouldClose, string Status, [property: JsonIgnore] Relation Surface);
    public sealed record Observation(bool Complete, Fragment[] Fragments, Point[] Occluder,
        int Source, int[] Targets, Candidate[] Candidates, int CompatibleCandidates,
        bool ActualClosure, bool ConditionalClosure, string Status, string Limit,
        [property: JsonIgnore] Relation Surface);
    public sealed record Case(string Id, string Name, string Explanation, Observation Read,
        [property: JsonIgnore] Curve.Frame Frame, [property: JsonIgnore] Relation Surface);

    private const string Limits = "Only exposed endpoints, tangents, a declared rectangular occluder and an offered visible return route are read. A compatible bridge stays hypothetical. No hidden curve, automatic weld or probability is supplied.";

    public static Relation Expose(Curve.Frame frame, Relation visibleSection, bool start)
    {
        var read = frame.Read(visibleSection);
        if (!read.Complete) return new(visibleSection, StructuralConstraint.Open());
        var words = E(read.ControlWords);
        return new(visibleSection, new Relation(start ? read.Start : read.End,
            new Relation(words[start ? 0 : ^1], start ? read.StartTangent : read.EndTangent)));
    }

    public static Relation Request(Curve.Frame frame, IReadOnlyList<Relation> visible,
        Relation source, IReadOnlyList<Relation> targets, IReadOnlyList<Relation> opaqueBoundary,
        IReadOnlyList<Relation>? returnRoute = null) =>
        new(new Relation(frame.Surface, S(visible.Cast<IElement>().ToArray())),
            new Relation(source, new Relation(S(targets.Cast<IElement>().ToArray()),
                new Relation(S(opaqueBoundary.Cast<IElement>().ToArray()), S((returnRoute ?? []).Cast<IElement>().ToArray())))));

    public static Observation Read(Curve.Frame frame, Relation query)
    {
        Observation Open() => new(false, [], [], -1, [], [], 0, false, false,
            "The exposed request is incomplete.", Limits, new Relation(query, StructuralConstraint.Open()));
        if (query is not { Left: Relation { Left: var basis, Right: Relation visibleScope },
            Right: Relation { Left: Relation sourcePort, Right: Relation { Left: Relation targetsScope,
                Right: Relation { Left: Relation boundaryScope, Right: Relation routeScope } } } } ||
            !Same(basis, frame.Surface) || !Entries(visibleScope, out var visible) ||
            visible.Length is < 1 or > 64 || visible.Any(x => x is not Relation) ||
            !Entries(targetsScope, out var targetItems) || targetItems.Length > 32 ||
            targetItems.Any(x => x is not Relation) || !Entries(boundaryScope, out var boundaryItems) ||
            boundaryItems.Any(x => x is not Relation) || !Entries(routeScope, out var routeItems) ||
            routeItems.Any(x => !visible.Any(v => Same(v, x)))) return Open();

        var sections = visible.Cast<Relation>().Select(frame.Read).ToArray();
        if (sections.Any(s => !s.Complete) || !Port(frame, sourcePort, visible, false, out var source)) return Open();
        if (routeItems.Length > 0 && !routeItems.Any(item => Same(item, visible[source.Index]))) return Open();
        var targets = new List<PortRead>();
        foreach (var target in targetItems.Cast<Relation>())
        {
            if (!Port(frame, target, visible, true, out var selected)) return Open();
            targets.Add(selected);
        }
        var route = routeItems.Cast<Relation>().Select(frame.Read).ToArray();
        bool routeConnected = route.Length > 0 && route.All(r => r.Complete) &&
            route.Zip(route.Skip(1)).All(p => Same(p.First.End, p.Second.Start) && p.First.Controls[^1] == p.Second.Controls[0]);
        bool closed = route.Length > 1 && routeConnected && Same(route[^1].End, route[0].Start) &&
            route[^1].Controls[^1] == route[0].Controls[0];
        bool rectangle = Rectangle(frame, boundaryItems, out var corners);
        if (boundaryItems.Length > 0 && !rectangle) return Open();
        var candidates = new List<Candidate>();
        foreach (var target in targets)
        {
            if (Same(source.Site, target.Site) && source.Position != target.Position) return Open();
            var delta = target.Position - source.Position;
            bool forward = !delta.Zero && Curve.Vector.Dot(source.Tangent, delta).Sign > 0 &&
                Curve.Vector.Dot(target.Tangent, delta).Sign > 0;
            bool tangent = forward && !source.Tangent.Zero && !target.Tangent.Zero;
            bool aligned = tangent && Curve.Vector.Cross(source.Tangent, delta).IsZero &&
                Curve.Vector.Cross(target.Tangent, delta).IsZero;
            // A convex rectangular obscuring scope contains the whole straight
            // chord when both ends lie inside it. For a curved hypothesis this
            // is only coverage of the chord, never coverage of a chosen curve.
            bool covered = rectangle && !delta.Zero && Inside(corners, source.Position) && Inside(corners, target.Position) &&
                Inside(corners, source.Position + target.Position, doubled: true, strict: true);
            bool eligible = tangent && covered;
            bool joined = Same(source.Site, target.Site);
            bool wouldClose = eligible && !closed && routeConnected &&
                Same(route[0].Start, target.Site) && Same(route[^1].End, source.Site) &&
                route[0].Controls[0] == target.Position && route[^1].Controls[^1] == source.Position;
            var bridge = new Relation(new Relation(source.Site, target.Site), StructuralConstraint.Open());
            var evidence = new Relation(boundaryScope, new Relation(new Relation(source.PositionWord, target.PositionWord),
                new Relation(source.TangentWord, target.TangentWord)));
            var witness = new Relation(new Relation(query, new Relation(sourcePort, target.Surface)),
                new Relation(evidence, StructuralConstraint.Open(bridge)));
            string status = joined ? "The exposed sites are already identical." : delta.Zero ?
                "Equal positions do not identify the sites." : !tangent ?
                "Outside this forward-continuation hypothesis." : !covered ?
                "Direction evidence exists, but the selected occluder does not cover the gap." : aligned ?
                "A straight continuation is compatible; the bridge remains open." :
                "Endpoint tangents permit a forward curved continuation; its shape remains open.";
            candidates.Add(new(target.Index, P(source.Position), P(target.Position), aligned, tangent, covered,
                eligible, joined, wouldClose, status, witness));
        }
        var ids = new List<IElement>();
        int Site(IElement site)
        {
            int index = ids.FindIndex(item => Same(item, site));
            if (index >= 0) return index;
            ids.Add(site); return ids.Count - 1;
        }
        var fragments = sections.Select((section, i) => new Fragment(i, section.Controls.Select(P).ToArray(), Site(section.Start), Site(section.End))).ToArray();
        int count = candidates.Count(c => c.Eligible);
        string conclusion = closed ? "The supplied route is closed by shared native sites." : count > 1 ?
            "Several exposed targets fit; no continuation is selected." : candidates.Any(c => c.WouldClose) ?
            "Accepting the proposed bridge would close the visible return route." : count == 1 ?
            "One target fits within the exposed comparison set; connection is still a hypothesis." :
            "No continuation is supported by this selected local rule.";
        return new(true, fragments, rectangle ? corners.Select(P).ToArray() : [], source.Index,
            targets.Select(t => t.Index).ToArray(), candidates.ToArray(), count, closed,
            candidates.Any(c => c.WouldClose), conclusion, Limits,
            new Relation(query, S(candidates.Select(c => (IElement)c.Surface).ToArray())));
    }

    private sealed record PortRead(int Index, IElement Site, Relation PositionWord, Relation TangentWord,
        Curve.Vector Position, Curve.Vector Tangent, Relation Surface);

    private static bool Port(Curve.Frame frame, Relation surface, IElement[] visible, bool start, out PortRead port)
    {
        port = null!;
        if (surface is not { Left: Relation section, Right: Relation { Left: var site,
            Right: Relation { Left: Relation position, Right: Relation tangent } } }) return false;
        int index = Array.FindIndex(visible, v => Same(v, section));
        if (index < 0) return false;
        var read = frame.Read(section); var words = E(read.ControlWords);
        if (!read.Complete || !Same(site, start ? read.Start : read.End) ||
            !Same(position, words[start ? 0 : ^1]) || !frame.Vector(position, out var at) ||
            !frame.Vector(tangent, out var direction) || direction != (start ? read.StartVector : read.EndVector)) return false;
        port = new(index, site, position, tangent, at, direction, surface); return true;
    }

    private static bool Rectangle(Curve.Frame frame, IElement[] items, out Curve.Vector[] corners)
    {
        corners = [];
        if (items.Length != 4) return false;
        var read = items.Cast<Relation>().Select(frame.Read).ToArray();
        if (read.Any(r => !r.Complete || r.Controls.Length != 2)) return false;
        for (int i = 0; i < 4; i++)
        {
            var a = read[i]; var b = read[(i + 1) % 4]; var d = a.Controls[1] - a.Controls[0];
            if (d.Zero || (!d.X.IsZero && !d.Y.IsZero) || !Same(a.End, b.Start) || a.Controls[1] != b.Controls[0]) return false;
        }
        var points = read.Select(r => r.Controls[0]).ToArray();
        var xs = points.Select(p => p.X).Distinct().ToArray(); var ys = points.Select(p => p.Y).Distinct().ToArray();
        if (xs.Length != 2 || ys.Length != 2 || points.Distinct().Count() != 4) return false;
        corners = points; return true;
    }

    private static bool Inside(Curve.Vector[] box, Curve.Vector point, bool doubled = false, bool strict = false)
    {
        int factor = doubled ? 2 : 1;
        var minX = box.Min(p => p.X) * factor; var maxX = box.Max(p => p.X) * factor;
        var minY = box.Min(p => p.Y) * factor; var maxY = box.Max(p => p.Y) * factor;
        return strict ? point.X > minX && point.X < maxX && point.Y > minY && point.Y < maxY :
            point.X >= minX && point.X <= maxX && point.Y >= minY && point.Y <= maxY;
    }

    public static Case[] Cases()
    {
        var result = new List<Case>();
        foreach (var mode in new[] { "aligned", "misaligned", "ambiguous", "unoccluded", "tangent" })
        {
            var f = new Curve.Frame(new QualitativeMotionFrame());
            var source = Line(f, (-4, 0), (-1, 0));
            var target = mode == "tangent" ? Line(f, (1, 1), (4, 1)) :
                mode == "misaligned" ? Line(f, (1, 0), (1, 3)) : Line(f, (1, 0), (4, 0));
            var visible = new List<Relation> { source, target }; var targets = new List<Relation> { Expose(f, target, true) };
            if (mode == "ambiguous")
            {
                // Coincident exposed starts have distinct identities and the
                // two visible continuations diverge beyond that start.
                var other = f.Cubic(new Placeholder(), new Placeholder(), f.Word(1, 0), f.Word(2, 0), f.Word(3, 2), f.Word(4, 2));
                visible.Add(other); targets.Add(Expose(f, other, true));
            }
            var boundary = mode == "unoccluded" ? [] : Box(f, -1, -1, 1, 2);
            var request = Request(f, visible, Expose(f, source, false), targets, boundary);
            var (name, explanation) = mode switch
            {
                "aligned" => ("Aligned ends behind a crossing", "The visible ends and their directions agree with a straight hidden continuation. The read retains an open bridge instead of asserting one."),
                "misaligned" => ("A conflicting exit direction", "The offered exit points sideways to the chord. This rejects the selected forward bridge rule; a more elaborate hidden route remains outside the test."),
                "ambiguous" => ("Two compatible identities", "Two different exposed fragments begin at the same position and tangent. Geometry cannot choose their identity."),
                "unoccluded" => ("An uncovered gap", "Alignment alone cannot explain a missing visible stroke. There is no offered opaque scope covering this gap."),
                _ => ("Tangency without a unique curve", "Both ends point forward but lie on different parallel lines. Many curved bridges can share these tangents; no interpolation is selected.")
            };
            result.Add(new(mode, name, explanation, Read(f, request), f, request));
        }
        foreach (bool closed in new[] { false, true })
        {
            var f = new Curve.Frame(new QualitativeMotionFrame());
            var sites = Enumerable.Range(0, 6).Select(_ => (IElement)new Placeholder()).ToArray();
            (int X, int Y)[] points = [(-4, 0), (-1, 0), (1, 0), (4, 0), (4, -4), (-4, -4)];
            var source = Line(f, points[0], points[1], sites[0], sites[1]);
            var target = Line(f, points[2], points[3], sites[2], sites[3]);
            var tail = Enumerable.Range(3, 3).Select(i => Line(f, points[i], points[(i + 1) % 6], sites[i], sites[(i + 1) % 6])).ToArray();
            var visible = new List<Relation> { source, target }; visible.AddRange(tail);
            Relation[] route = [target, .. tail, source];
            if (closed)
            {
                var bridge = Line(f, points[1], points[2], sites[1], sites[2]); visible.Add(bridge);
                route = [source, bridge, target, .. tail];
            }
            var request = Request(f, visible, Expose(f, source, false), closed ? [] : [Expose(f, target, true)],
                closed ? [] : Box(f, -1, -1, 1, 1), route);
            result.Add(new(closed ? "authored-closure" : "conditional-closure", closed ? "Known closed route" : "Closure conditional on a bridge",
                closed ? "The missing section is now explicitly supplied with the actual endpoint identities. Closure follows from the connected cycle." :
                "The rest of the route is visibly connected. One proposed bridge would close it, but the current route remains open.", Read(f, request), f, request));
        }
        {
            var f = new Curve.Frame(new QualitativeMotionFrame());
            var sites = Enumerable.Range(0, 5).Select(_ => (IElement)new Placeholder()).ToArray();
            (int X, int Y)[] points = [(0, 0), (3, 0), (3, 3), (0, 3), (0, 0)];
            var route = Enumerable.Range(0, 4).Select(i => Line(f, points[i], points[i + 1], sites[i], sites[i + 1])).ToArray();
            var request = Request(f, route, Expose(f, route[^1], false), [Expose(f, route[0], true)], [], route);
            result.Add(new("coincident-open", "Equal position, separate sites", "The endpoint coordinates agree, but their identities differ. The outline alone does not establish a connected loop.", Read(f, request), f, request));
        }
        return result.ToArray();
    }

    private static Relation Line(Curve.Frame frame, (int X, int Y) a, (int X, int Y) b, IElement? from = null, IElement? to = null) =>
        frame.Line(from ?? new Placeholder(), to ?? new Placeholder(), frame.Word(a.X, a.Y), frame.Word(b.X, b.Y));
    private static Relation[] Box(Curve.Frame frame, int x0, int y0, int x1, int y1)
    {
        var sites = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray();
        (int X, int Y)[] points = [(x0, y0), (x1, y0), (x1, y1), (x0, y1)];
        return Enumerable.Range(0, 4).Select(i => Line(frame, points[i], points[(i + 1) % 4], sites[i], sites[(i + 1) % 4])).ToArray();
    }
    private static Point P(Curve.Vector v) => new(checked((int)v.X), checked((int)v.Y));
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static bool Entries(Relation scope, out IElement[] entries) => StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
    private static IElement[] E(Relation scope) => Entries(scope, out var entries) ? entries : [];
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}
