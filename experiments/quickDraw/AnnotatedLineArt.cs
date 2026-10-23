using System.Numerics;
using System.Text.Json;
using Gojo.Core;
using Gojo.Equations;
using Gojo.Patterns;
using static Gojo.Experiments.QuickDraw.SequentialEvidence;

namespace Gojo.Experiments.QuickDraw;

/// <summary>An authored, measured line-art source and its explicit annotation
/// surfaces. Membership is declared; incidence, closure, tangents and transform
/// agreement are reads. No label, coordinate equality or drawing prompt welds
/// sites, establishes an object, supplies fill, or proves physical attachment.</summary>
public static class AnnotatedLineArt
{
    // scene = (author event -> measured frame) -> Closed(view, sites, edges,
    //   strokes, objects, (figure regions -> (void regions -> unselected regions)),
    //   (core-focus groups -> detail groups), transforms, crossings, contacts)
    // row = (fresh event -> L(order,index)) -> body
    // site body = coordinate pair; edge body = finite interpolation curve
    // membership body = Closed(actual edge rows) -> author event
    // region body = Closed(edge row -> oriented site pair) -> (parent scope -> author)
    // transform body = Closed((source edge -> target edge) -> target orientation)
    //                  -> (native affine equations -> (basis descriptor -> author))
    // crossing body = (edge pair -> coordinate pair) -> (over scope -> author)
    // contact body = (actual site row -> actual site row) -> author
    // Geometry sites are introduced before curves; stroke lists are an authored
    // traversal, not claimed human acquisition history. Parent regions must
    // already exist. No arbitrary string tags occur in the native structure.

    public sealed record Site(int Index, Placeholder Event, Point Position, Relation Surface);
    public sealed record Edge(int Index, Placeholder Event, NativeReconstruction.Curve Curve, Relation Surface);
    public sealed record Membership(int Index, Placeholder Event, Edge[] Edges, Relation Surface);
    public sealed record Walk(Edge Edge, bool Forward, Placeholder From, Placeholder To, Relation Surface);
    public sealed record Region(int Index, Placeholder Event, Walk[] Boundary, int? Figure, Region? Parent, Relation Surface)
    {
        public bool Closed => Boundary.Length > 0 && Boundary.Where((step, i) =>
            !ReferenceEquals(step.To, Boundary[(i + 1) % Boundary.Length].From)).Any() == false;
    }
    public sealed record Group(Membership Membership, bool Detail);
    public sealed record Match(Edge Source, Edge Target, bool Reversed, Relation Surface);
    public sealed record Correspondence(int Index, Match[] Matches, string Kind, Point[] Descriptor,
        bool GeometryMatches, bool IdentityMatches, Relation Surface)
    { public bool Verified => Matches.Length > 0 && GeometryMatches && IdentityMatches; }
    public sealed record Crossing(int Index, Edge First, Edge Second, Point At, Edge? Over, Relation Surface);
    public sealed record Contact(int Index, Site First, Site Second, Relation Surface);
    public sealed record Read(Placeholder Author, Frame Frame, long[] ViewBox, Site[] Sites, Edge[] Edges,
        Membership[] Strokes, Membership[] Objects, Region[] Regions, Group[] Groups,
        Correspondence[] Correspondences, Crossing[] Crossings, Contact[] Contacts, Relation Surface);

    public static Relation Import(JsonElement document)
    {
        var quantum = document.GetProperty("coordinateQuantum").GetInt64();
        SequentialEvidence.TryRead(Create(quantum, []), out var empty); var frame = empty.Frame;
        var author = new Placeholder(); var views = Longs(document.GetProperty("viewBox"));
        if (views.Length != 4 || views[2] <= 0 || views[3] <= 0) throw new InvalidDataException("A source needs a positive declared view box.");
        var view = new Relation(Position(frame.X, frame.Y, new(views[0], views[1])),
            Position(frame.X, frame.Y, new(checked(views[0] + views[2]), checked(views[1] + views[3]))));
        var sites = new Dictionary<string, Relation>(StringComparer.Ordinal); var edges = new Dictionary<string, Relation>(StringComparer.Ordinal);
        var regions = new Dictionary<string, Relation>(StringComparer.Ordinal);
        Relation Row(int index, IElement body) => new(new Relation(new Placeholder(), new CarrierLocation(frame.Order, index)), body);
        foreach (var item in Items(document, "sites"))
            sites.Add(Id(item), Row(sites.Count, Position(frame.X, frame.Y, new(item.GetProperty("x").GetInt64(), item.GetProperty("y").GetInt64()))));
        foreach (var item in Items(document, "edges"))
        {
            var a = sites[item.GetProperty("from").GetString()!]; var b = sites[item.GetProperty("to").GetString()!];
            var controls = item.GetProperty("controls").EnumerateArray().Select(PointOf).ToArray();
            TryPosition(a.Right, frame, out var start); TryPosition(b.Right, frame, out var end);
            if (controls.Length is < 2 or > 4 || controls[0] != start || controls[^1] != end)
                throw new InvalidDataException("Curve endpoints must use their declared site positions.");
            edges.Add(Id(item), Row(edges.Count, NativeReconstruction.BuildCurve(frame, Event(a), Event(b), controls)));
        }
        Relation Members(JsonElement item) => S(item.GetProperty("edges").EnumerateArray().Select(id => (IElement)edges[id.GetString()!]).ToArray());
        var strokes = Items(document, "strokes").Select((item, i) => (IElement)Row(i, new Relation(Members(item), author))).ToArray();
        var objects = Items(document, "objects").Select((item, i) => (IElement)Row(i, new Relation(Members(item), author))).ToArray();
        var figure = new List<IElement>(); var holes = new List<IElement>(); var unselected = new List<IElement>();
        foreach (var item in Items(document, "regions"))
        {
            var steps = item.GetProperty("boundary").EnumerateArray().Select(step => {
                var edge = edges[step.GetProperty("edge").GetString()!]; var curve = (Relation)edge.Right;
                var pair = (Relation)curve.Left;
                var forward = !step.TryGetProperty("forward", out var flag) || flag.GetBoolean();
                return (IElement)new Relation(edge, forward ? pair : pair.Reverse());
            }).ToArray();
            var parent = item.TryGetProperty("parent", out var parentId) && parentId.ValueKind == JsonValueKind.String
                ? S(regions[parentId.GetString()!]) : S();
            var region = Row(regions.Count, new Relation(S(steps), new Relation(parent, author))); regions.Add(Id(item), region);
            if (!item.TryGetProperty("figure", out var isFigure) || isFigure.ValueKind == JsonValueKind.Null) unselected.Add(region);
            else if (isFigure.GetBoolean()) figure.Add(region); else holes.Add(region);
        }
        var core = new List<IElement>(); var detail = new List<IElement>(); var groupIndex = 0;
        foreach (var item in Items(document, "groups"))
        {
            var row = Row(groupIndex++, new Relation(Members(item), author));
            switch (item.GetProperty("role").GetString())
            { case "template": core.Add(row); break; case "detail": detail.Add(row); break; default: throw new InvalidDataException("Unknown authored focus role."); }
        }
        var transforms = new List<IElement>();
        foreach (var item in Items(document, "correspondences"))
        {
            var source = item.GetProperty("source").EnumerateArray().Select(id => edges[id.GetString()!]).ToArray();
            var target = item.GetProperty("target").EnumerateArray().Select(id => edges[id.GetString()!]).ToArray();
            var reversed = item.TryGetProperty("reversed", out var flips) ? flips.EnumerateArray().Select(flag => flag.GetBoolean()).ToArray() : new bool[source.Length];
            if (source.Length == 0 || source.Length != target.Length || source.Length != reversed.Length)
                throw new InvalidDataException("A correspondence needs equally sized nonempty source, target and orientation lists.");
            Point[] descriptor = item.GetProperty("kind").GetString() switch {
                "mirror" => item.GetProperty("axis").EnumerateArray().Select(PointOf).ToArray(),
                "repeat" or "translation" => [PointOf(item.GetProperty("translation"))],
                _ => throw new InvalidDataException("Only authored reflection and translation correspondences are supported.") };
            var matrix = Matrix(descriptor);
            var pairs = source.Select((edge, i) => {
                var pair = (Relation)((Relation)target[i].Right).Left;
                return (IElement)new Relation(new Relation(edge, target[i]), reversed[i] ? pair.Reverse() : pair);
            }).ToArray();
            transforms.Add(Row(transforms.Count, new Relation(S(pairs), new Relation(Affine(frame, matrix),
                new Relation(S(descriptor.Select(point => (IElement)Position(frame.X, frame.Y, point)).ToArray()), author)))));
        }
        var crossings = Items(document, "crossings").Select((item, i) => {
            var pair = item.GetProperty("edges").EnumerateArray().Select(id => edges[id.GetString()!]).ToArray();
            if (pair.Length != 2) throw new InvalidDataException("A crossing declaration selects two edges.");
            var over = item.TryGetProperty("over", out var overId) && overId.ValueKind == JsonValueKind.String ? S(edges[overId.GetString()!]) : StructuralConstraint.Open();
            return (IElement)Row(i, new Relation(new Relation(new Relation(pair[0], pair[1]), Position(frame.X, frame.Y, PointOf(item.GetProperty("site")))), new Relation(over, author)));
        }).ToArray();
        var contacts = Items(document, "contacts").Select((item, i) => {
            var pair = item.GetProperty("sites").EnumerateArray().Select(id => sites[id.GetString()!]).ToArray();
            if (pair.Length != 2) throw new InvalidDataException("A contact declaration selects two sites.");
            return (IElement)Row(i, new Relation(new Relation(pair[0], pair[1]), author));
        }).ToArray();
        return new(new Relation(author, frame.Surface), S(view, S(sites.Values.Cast<IElement>().ToArray()), S(edges.Values.Cast<IElement>().ToArray()),
            S(strokes), S(objects), new Relation(S(figure.ToArray()), new Relation(S(holes.ToArray()), S(unselected.ToArray()))),
            new Relation(S(core.ToArray()), S(detail.ToArray())), S(transforms.ToArray()), S(crossings), S(contacts)));
    }

    public static bool TryRead(Relation surface, out Read read)
    {
        read = null!;
        if (surface is not { Left: Relation { Left: Placeholder { IsInverse: false } author, Right: Relation basis }, Right: Relation body } ||
            !TryReadFrame(basis, out var frame) || !Entries(body, out var parts) || parts is not [Relation view, Relation siteScope, Relation edgeScope,
                Relation strokeScope, Relation objectScope, Relation regionContexts, Relation groupContexts, Relation transformScope, Relation crossingScope, Relation contactScope] ||
            !TryPosition(view.Left, frame, out var low) || !TryPosition(view.Right, frame, out var high) || high.X <= low.X || high.Y <= low.Y ||
            !Entries(siteScope, out var siteRows) || !Entries(edgeScope, out var edgeRows)) return false;
        var events = new HashSet<Placeholder> { author }; var sites = new List<Site>(); var edges = new List<Edge>();
        bool Row(IElement row, out int index, out Placeholder identity, out IElement contents)
        {
            index = -1; identity = null!; contents = null!;
            if (row is not Relation { Left: Relation { Left: Placeholder { IsInverse: false } ev, Right: CarrierLocation at }, Right: var content } ||
                !ReferenceEquals(at.Carrier, frame.Order) || at.Ticks < 0 || at.Ticks > int.MaxValue || !events.Add(ev)) return false;
            index = (int)at.Ticks; identity = ev; contents = content; return true;
        }
        foreach (var item in siteRows)
        {
            if (!Row(item, out var i, out var ev, out var pos) || i != sites.Count || !TryPosition(pos, frame, out var point)) return false;
            sites.Add(new(i, ev, point, (Relation)item));
        }
        var siteMap = sites.ToDictionary(site => site.Event); var edgeMap = new Dictionary<Relation, Edge>(ReferenceEqualityComparer.Instance);
        foreach (var item in edgeRows)
        {
            if (!Row(item, out var i, out var ev, out var source) || i != edges.Count || source is not Relation curve ||
                !NativeReconstruction.TryReadCurve(frame, curve, out var geometry) || !siteMap.TryGetValue(geometry.From, out var from) ||
                !siteMap.TryGetValue(geometry.To, out var to) || from.Position != geometry.Controls[0] || to.Position != geometry.Controls[^1]) return false;
            var edge = new Edge(i, ev, geometry, (Relation)item); edges.Add(edge); edgeMap.Add(edge.Surface, edge);
        }
        bool Members(Relation scope, bool contiguousIndexes, out Membership[] result)
        {
            result = []; if (!Entries(scope, out var rows)) return false; var found = new List<Membership>();
            foreach (var item in rows)
            {
                if (!Row(item, out var i, out var ev, out var content) || contiguousIndexes && i != found.Count ||
                    content is not Relation { Left: Relation memberScope, Right: var provenance } || !ReferenceEquals(provenance, author) || !Entries(memberScope, out var members) ||
                    members.Any(member => member is not Relation memberRow || !edgeMap.ContainsKey(memberRow))) return false;
                found.Add(new(i, ev, members.Cast<Relation>().Select(member => edgeMap[member]).ToArray(), (Relation)item));
            }
            result = found.ToArray(); return true;
        }
        if (!Members(strokeScope, true, out var strokes) || !Members(objectScope, true, out var objects) ||
            groupContexts is not { Left: Relation coreScope, Right: Relation detailScope } || !Members(coreScope, false, out var core) || !Members(detailScope, false, out var detail)) return false;
        var groups = core.Select(item => new Group(item, false)).Concat(detail.Select(item => new Group(item, true))).OrderBy(group => group.Membership.Index).ToArray();
        if (!groups.Select(group => group.Membership.Index).SequenceEqual(Enumerable.Range(0, groups.Length)) ||
            regionContexts is not { Left: Relation figures, Right: Relation { Left: Relation holes, Right: Relation unspecified } } ||
            !Entries(figures, out var f) || !Entries(holes, out var h) || !Entries(unspecified, out var u)) return false;
        var regionRows = f.Select(item => (Item: item, Role: (int?)1)).Concat(h.Select(item => (Item: item, Role: (int?)0)))
            .Concat(u.Select(item => (Item: item, Role: (int?)null))).OrderBy(row => Index(row.Item)).ToArray();
        var regions = new List<Region>(); var regionMap = new Dictionary<Relation, Region>(ReferenceEqualityComparer.Instance);
        foreach (var entry in regionRows)
        {
            if (!Row(entry.Item, out var i, out var ev, out var content) || i != regions.Count ||
                content is not Relation { Left: Relation boundaries, Right: Relation { Left: Relation parentScope, Right: var source } } ||
                !ReferenceEquals(source, author) || !Entries(boundaries, out var walks) || !Entries(parentScope, out var parents) || parents.Length > 1) return false;
            Region? parent = null;
            if (parents.Length == 1 && (parents[0] is not Relation previous || !regionMap.TryGetValue(previous, out parent))) return false;
            var boundary = new List<Walk>();
            foreach (var walk in walks)
            {
                if (walk is not Relation { Left: Relation edgeRow, Right: Relation { Left: Placeholder from, Right: Placeholder to } } step ||
                    !edgeMap.TryGetValue(edgeRow, out var edge) || !Orientation(edge, from, to, out var forward)) return false;
                boundary.Add(new(edge, forward, from, to, step));
            }
            var region = new Region(i, ev, boundary.ToArray(), entry.Role, parent, (Relation)entry.Item); regions.Add(region); regionMap.Add(region.Surface, region);
        }
        if (!Entries(transformScope, out var transformRows)) return false; var correspondences = new List<Correspondence>();
        foreach (var item in transformRows)
        {
            if (!Row(item, out var i, out _, out var content) || i != correspondences.Count ||
                content is not Relation { Left: Relation matchScope, Right: Relation { Left: Relation operation,
                    Right: Relation { Left: Relation descriptorScope, Right: var provenance } } } || !ReferenceEquals(provenance, author) ||
                !Entries(matchScope, out var mappings) || mappings.Length == 0 || !Entries(descriptorScope, out var descriptorRows) || descriptorRows.Length is < 1 or > 2) return false;
            var descriptor = new Point[descriptorRows.Length];
            for (var d = 0; d < descriptorRows.Length; d++) if (!TryPosition(descriptorRows[d], frame, out descriptor[d])) return false;
            long[] matrix;
            try { matrix = Matrix(descriptor); } catch (ArgumentException) { return false; } catch (OverflowException) { return false; }
            if (!ReadAffine(operation, frame, matrix)) return false;
            var matches = new List<Match>(); var geometry = true; var identity = true;
            var siteCorrespondence = new Dictionary<Placeholder, Placeholder>(); var usedTargets = new Dictionary<Placeholder, Placeholder>();
            var usedSourceEdges = new HashSet<Edge>(); var usedTargetEdges = new HashSet<Edge>();
            foreach (var mapping in mappings)
            {
                if (mapping is not Relation { Left: Relation { Left: Relation a, Right: Relation b },
                    Right: Relation { Left: Placeholder from, Right: Placeholder to } } map ||
                    !edgeMap.TryGetValue(a, out var first) || !edgeMap.TryGetValue(b, out var second) || !Orientation(second, from, to, out var forward)) return false;
                var target = forward ? second.Curve.Controls : second.Curve.Controls.Reverse().ToArray();
                geometry &= first.Curve.Controls.Length == target.Length && first.Curve.Controls.Where((point, j) =>
                    j >= target.Length || !TransformEqual(point, target[j], matrix)).Any() == false;
                identity &= usedSourceEdges.Add(first) && usedTargetEdges.Add(second);
                Bind(first.Curve.From, from); Bind(first.Curve.To, to);
                matches.Add(new(first, second, !forward, map));
                void Bind(Placeholder sourceSite, Placeholder targetSite)
                {
                    if (siteCorrespondence.TryGetValue(sourceSite, out var previous)) identity &= ReferenceEquals(previous, targetSite);
                    else siteCorrespondence.Add(sourceSite, targetSite);
                    if (usedTargets.TryGetValue(targetSite, out var previousSource)) identity &= ReferenceEquals(previousSource, sourceSite);
                    else usedTargets.Add(targetSite, sourceSite);
                }
            }
            correspondences.Add(new(i, matches.ToArray(), descriptor.Length == 2 ? "mirror" : "translation", descriptor, geometry, identity, (Relation)item));
        }
        if (!Entries(crossingScope, out var crossingRows) || !Entries(contactScope, out var contactRows)) return false;
        var crossings = new List<Crossing>(); var contacts = new List<Contact>();
        foreach (var item in crossingRows)
        {
            if (!Row(item, out var i, out _, out var content) || i != crossings.Count || content is not Relation {
                Left: Relation { Left: Relation { Left: Relation a, Right: Relation b }, Right: var position },
                Right: Relation { Left: Relation overScope, Right: var source } } || !ReferenceEquals(source, author) ||
                !edgeMap.TryGetValue(a, out var first) || !edgeMap.TryGetValue(b, out var second) || ReferenceEquals(first, second) ||
                !TryPosition(position, frame, out var at) || !StructuralConstraint.TryReadScope(overScope, out var overs, out var seam, out var tail) || overs.Length > 1) return false;
            Edge? over = null;
            if (overs.Length == 1 && (!seam.Equals(tail) || overs[0] is not Relation selected || !edgeMap.TryGetValue(selected, out over) ||
                !ReferenceEquals(over, first) && !ReferenceEquals(over, second))) return false;
            crossings.Add(new(i, first, second, at, over, (Relation)item));
        }
        var siteRowsByRef = new Dictionary<Relation, Site>(ReferenceEqualityComparer.Instance);
        foreach (var site in sites) siteRowsByRef.Add(site.Surface, site);
        foreach (var item in contactRows)
        {
            if (!Row(item, out var i, out _, out var content) || i != contacts.Count || content is not Relation {
                Left: Relation { Left: Relation a, Right: Relation b }, Right: var source } || !ReferenceEquals(source, author) ||
                !siteRowsByRef.TryGetValue(a, out var first) || !siteRowsByRef.TryGetValue(b, out var second) || ReferenceEquals(first, second)) return false;
            contacts.Add(new(i, first, second, (Relation)item));
        }
        read = new(author, frame, [low.X, low.Y, checked(high.X - low.X), checked(high.Y - low.Y)], sites.ToArray(), edges.ToArray(), strokes, objects,
            regions.ToArray(), groups, correspondences.ToArray(), crossings.ToArray(), contacts.ToArray(), surface); return true;
    }

    public static string TraceKind(Edge edge)
    {
        var p = edge.Curve.Controls; var direction = p.Skip(1).Select(point => Difference(point, p[0])).FirstOrDefault(v => v != (BigInteger.Zero, BigInteger.Zero));
        if (direction == (BigInteger.Zero, BigInteger.Zero)) return "degenerate";
        return p.All(point => Cross(direction, Difference(point, p[0])).IsZero) ? "straight" : "curved";
    }
    public static (BigInteger X, BigInteger Y) Tangent(Edge edge, Placeholder site)
    {
        return Tangent(edge, ReferenceEquals(site, edge.Curve.From));
    }
    private static (BigInteger X, BigInteger Y) Tangent(Edge edge, bool atStart)
    {
        var points = atStart ? edge.Curve.Controls : edge.Curve.Controls.Reverse().ToArray();
        return points.Skip(1).Select(point => Difference(point, points[0])).FirstOrDefault(v => v != (BigInteger.Zero, BigInteger.Zero));
    }
    public static string JointKind(Read scene, Site site)
    {
        return RayKind(Rays(scene, [site.Event]));
    }
    /// <summary>A projected ray read through an explicit contact claim. It does
    /// not weld the two sites or identify their owning objects.</summary>
    public static string ContactKind(Read scene, Contact contact) => contact.First.Position == contact.Second.Position
        ? RayKind(Rays(scene, [contact.First.Event, contact.Second.Event])) : "unresolved";
    private static (BigInteger X, BigInteger Y)[] Rays(Read scene, Placeholder[] sites) => scene.Edges.SelectMany(edge =>
        new[] { (Site: edge.Curve.From, Start: true), (Site: edge.Curve.To, Start: false) }
            .Where(endpoint => sites.Any(site => ReferenceEquals(endpoint.Site, site)))
            .Select(endpoint => Tangent(edge, endpoint.Start))).ToArray();
    private static string RayKind((BigInteger X, BigInteger Y)[] rays)
    {
        if (rays.Length == 0) return "isolated"; if (rays.Length == 1) return "endpoint";
        if (rays.Any(ray => ray == (BigInteger.Zero, BigInteger.Zero))) return "unresolved";
        var opposed = 0;
        for (var i = 0; i < rays.Length; i++) for (var j = i + 1; j < rays.Length; j++)
            if (Cross(rays[i], rays[j]).IsZero)
            {
                var dot = Dot(rays[i], rays[j]).Sign;
                if (dot > 0) return "unresolved"; // Coincident rays do not manufacture another directional arm.
                if (dot < 0) opposed++;
            }
        return (rays.Length, opposed) switch { (2, 1) => "I", (2, 0) => "L", (3, 1) => "T", (3, 0) => "Y", (4, 2) => "X", (4, 1) => "K", _ => "unresolved" };
    }
    public static bool? Contains(Edge edge, Point point)
    {
        if (edge.Curve.Controls[0] == point || edge.Curve.Controls[^1] == point) return true;
        if (edge.Curve.Controls.Length != 2) return null; // No sampled curve hit promoted to exact incidence.
        var a = edge.Curve.Controls[0]; var b = edge.Curve.Controls[^1]; var v = Difference(b, a); var w = Difference(point, a);
        if (v == (BigInteger.Zero, BigInteger.Zero)) return false;
        return Cross(v, w).IsZero && Dot(w, v).Sign >= 0 && Dot(w, v) <= Dot(v, v);
    }
    public static bool JoinedAt(Crossing crossing) => new[] { crossing.First.Curve.From, crossing.First.Curve.To }
        .Any(site => (ReferenceEquals(site, crossing.First.Curve.From) ? crossing.First.Curve.Controls[0] : crossing.First.Curve.Controls[^1]) == crossing.At &&
            (ReferenceEquals(site, crossing.Second.Curve.From) || ReferenceEquals(site, crossing.Second.Curve.To)));

    private static Relation Affine(Frame frame, long[] m)
    {
        var constant = new Carrier(frame.X.UnitShape); var rows = new List<IElement>();
        for (var i = 0; i < 2; i++)
        {
            var accumulator = new Carrier(frame.X.UnitShape); var output = new Carrier(frame.X.UnitShape);
            rows.Add(new Relation(Equation.Sum(accumulator, Equation.WeightedPort(frame.X, 0, 1, 0, m[i * 3]),
                Equation.WeightedPort(frame.Y, 0, 1, 0, m[i * 3 + 1]), Equation.WeightedPort(constant, 0, 1, 0, m[i * 3 + 2])),
                Equation.Affine(accumulator, 0, m[6], output, 0, 1)));
        }
        return new(new Relation(frame.Surface, new CarrierLocation(constant, 1)), S(rows.ToArray()));
    }
    private static bool ReadAffine(Relation operation, Frame frame, long[] matrix)
    {
        if (operation is not { Left: Relation { Left: var basis, Right: CarrierLocation constant }, Right: Relation scope } ||
            !ReferenceEquals(basis, frame.Surface) || constant.Ticks != 1 || constant.Carrier.IsUnknown || constant.Carrier.IsInverse ||
            !constant.Carrier.UnitShape.CompatibleWith(frame.X.UnitShape) || !Entries(scope, out var rows) || rows.Length != 2) return false;
        var carriersSeen = new HashSet<Carrier>(ReferenceEqualityComparer.Instance) { frame.X, frame.Y, frame.Order };
        if (!carriersSeen.Add(constant.Carrier)) return false;
        for (var i = 0; i < 2; i++)
        {
            if (rows[i] is not Relation { Left: Relation sum, Right: Relation projection } || sum.Left is not Relation { Left: Relation pair, Right: var third } ||
                sum.Right is not Relation { Left: CarrierLocation at, Right: Carrier accumulator } || !ReferenceEquals(at.Carrier, accumulator) || at.Ticks != 1 ||
                !Axis(projection.Left, out var c, out var denominator) || !ReferenceEquals(c, accumulator) || denominator != matrix[6] ||
                !Axis(projection.Right, out var output, out var one) || one != 1 || !carriersSeen.Add(accumulator) || !carriersSeen.Add(output) ||
                !accumulator.UnitShape.CompatibleWith(frame.X.UnitShape) || !output.UnitShape.CompatibleWith(frame.X.UnitShape)) return false;
            var ports = new[] { pair.Left, pair.Right, third }; var carriers = new[] { frame.X, frame.Y, constant.Carrier };
            for (var j = 0; j < 3; j++)
                if (ports[j] is not Relation { Left: var input, Right: Relation {
                    Left: Relation { Left: CarrierLocation lo, Right: Carrier lc }, Right: Relation { Left: CarrierLocation hi, Right: Carrier hc } } } ||
                    !Axis(input, out var axis, out var extent) || !ReferenceEquals(axis, carriers[j]) || extent != 1 || lo.Ticks != 0 || hi.Ticks != matrix[i * 3 + j] ||
                    !ReferenceEquals(lo.Carrier, Carrier.Unknown) || !ReferenceEquals(hi.Carrier, Carrier.Unknown) ||
                    !ReferenceEquals(lc, Carrier.Unknown) || !ReferenceEquals(hc, Carrier.Unknown)) return false;
        }
        return true;
    }
    private static long[] Matrix(Point[] descriptor)
    {
        if (descriptor.Length == 1) return [1, 0, descriptor[0].X, 0, 1, descriptor[0].Y, 1];
        if (descriptor.Length != 2 || descriptor[0] == descriptor[1]) throw new ArgumentException("A reflection axis requires two distinct positions.");
        var a = descriptor[0]; var d = Difference(descriptor[1], a); var xx = d.X * d.X; var yy = d.Y * d.Y; var xy = d.X * d.Y;
        return new[] { xx - yy, 2 * xy, 2 * (yy * a.X - xy * a.Y), 2 * xy, yy - xx, 2 * (xx * a.Y - xy * a.X), xx + yy }
            .Select(value => checked((long)value)).ToArray();
    }
    private static bool TransformEqual(Point source, Point target, long[] m) =>
        (BigInteger)m[0] * source.X + (BigInteger)m[1] * source.Y + m[2] == (BigInteger)m[6] * target.X &&
        (BigInteger)m[3] * source.X + (BigInteger)m[4] * source.Y + m[5] == (BigInteger)m[6] * target.Y;
    private static bool Orientation(Edge edge, Placeholder from, Placeholder to, out bool forward)
    {
        forward = ReferenceEquals(from, edge.Curve.From) && ReferenceEquals(to, edge.Curve.To);
        return forward || ReferenceEquals(from, edge.Curve.To) && ReferenceEquals(to, edge.Curve.From);
    }
    private static (BigInteger X, BigInteger Y) Difference(Point a, Point b) => ((BigInteger)a.X - b.X, (BigInteger)a.Y - b.Y);
    private static BigInteger Cross((BigInteger X, BigInteger Y) a, (BigInteger X, BigInteger Y) b) => a.X * b.Y - a.Y * b.X;
    private static BigInteger Dot((BigInteger X, BigInteger Y) a, (BigInteger X, BigInteger Y) b) => a.X * b.X + a.Y * b.Y;
    private static int Index(IElement row) => row is Relation { Left: Relation { Right: CarrierLocation at } } && at.Ticks is >= 0 and <= int.MaxValue ? (int)at.Ticks : -1;
    private static Placeholder Event(Relation row) => (Placeholder)((Relation)row.Left).Left;
    private static JsonElement[] Items(JsonElement source, string name) => source.TryGetProperty(name, out var value) ? value.EnumerateArray().ToArray() : [];
    private static string Id(JsonElement item) => item.GetProperty("id").GetString() ?? throw new InvalidDataException("An input identifier is required.");
    private static long[] Longs(JsonElement value) => value.EnumerateArray().Select(item => item.GetInt64()).ToArray();
    private static Point PointOf(JsonElement value)
    { var pair = Longs(value); return pair.Length == 2 ? new(pair[0], pair[1]) : throw new InvalidDataException("A position requires two integer coordinates."); }
}
