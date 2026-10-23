using System.Numerics;
using System.Text.Json;
using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using static Gojo.Experiments.QuickDraw.SequentialEvidence;

namespace Gojo.Experiments.QuickDraw;

/// <summary>An executable finite drawing construction. Strings stay at the import
/// boundary: sites refer to actual earlier site rows, affine grades retain their
/// parameter carrier, and curves refer to actual control sites. Replaying a restored
/// program calculates every derived coordinate; no final geometry oracle is stored
/// in the program. The selected finite law truncates each affine displacement and
/// retains Equation's exact source-basis residue.</summary>
public static class ConstructionLadder
{
    // program = group(measured basis, view, parameters, site rows, edge rows,
    //                 rung plans, containment requests, repeat requests, circle requests)
    // row = (identity -> L(order,earliest rung)) -> body
    // site body = x expression -> y expression
    // root coordinate = L(axis,n)
    // affine expression = group(origin, from, to) -> (sample -> S(grade,0,d))
    // sample is a literal location or an actual parameter ValueSlot.
    // edge body = ordered actual control-site rows
    // rung plan = group(visible edges, guides, selected earlier edges, introduced edges)
    public sealed record Site(int Index, int At, Placeholder Event, Point Position,
        int[] Dependencies, Relation Surface, Relation Witness);
    public sealed record Edge(int Index, int At, Placeholder Event, Site[] Controls,
        NativeReconstruction.Curve Curve, Relation Surface);
    public sealed record Constraint(int Index, int At, Site[] Container, Edge[] Edges,
        bool? Satisfied, Relation Witness);
    public sealed record Rung(int Index, Edge[] Visible, Edge[] Guides, Edge[] Selected,
        Edge[] Introduced, Relation Surface);
    public sealed record Execution(Frame Frame, long[] ViewBox, Carrier[] Parameters,
        Site[] Sites, Edge[] Edges, Rung[] Rungs, Constraint[] Constraints,
        Relation[] Repeats, Relation[] Circular, Relation Ladder, Relation Surface);

    public static Relation Import(JsonElement document)
    {
        SequentialEvidence.TryRead(Create(1, []), out var empty); var frame = empty.Frame;
        var grade = new Carrier(); var parameters = new Dictionary<string, Carrier>(StringComparer.Ordinal);
        foreach (var p in Items(document, "parameters"))
        {
            var value = p.GetProperty("value").GetInt64();
            if (value < 0) throw new InvalidDataException("Parameter state is nonnegative; use a signed literal or reversed frame for negative participation.");
            var carrier = new Carrier(grade.UnitShape); carrier.Set(value); parameters.Add(Id(p), carrier);
        }
        var sites = new Dictionary<string, Relation>(StringComparer.Ordinal);
        var edges = new Dictionary<string, Relation>(StringComparer.Ordinal);
        var rungRows = Items(document, "rungs").ToArray();
        if (rungRows.Length == 0) throw new InvalidDataException("A construction needs at least one rung.");
        int At(JsonElement row)
        {
            var at = row.GetProperty("at").GetInt32();
            if (at < 0 || at >= rungRows.Length) throw new InvalidDataException("A row lies outside its ladder.");
            return at;
        }
        Relation Row(int at, IElement body) => new(new Relation(new Placeholder(), new CarrierLocation(frame.Order, at)), body);
        IElement Expression(JsonElement value, Carrier axis, int at)
        {
            if (value.ValueKind == JsonValueKind.Number) return new CarrierLocation(axis, value.GetInt64());
            var first = sites[value.GetProperty("from").GetString()!];
            var second = sites[value.GetProperty("to").GetString()!];
            var origin = value.TryGetProperty("origin", out var o) ? sites[o.GetString()!] : first;
            if (RowAt(first) > at || RowAt(second) > at || RowAt(origin) > at) throw new InvalidDataException("A coordinate cannot consume a future site.");
            var denominator = value.GetProperty("denominator").GetInt64();
            if (denominator <= 0) throw new InvalidDataException("An affine grade requires a positive denominator.");
            var numerator = value.GetProperty("numerator");
            var sample = numerator.ValueKind == JsonValueKind.String ? parameters[numerator.GetString()!].ValueSlot
                : new CarrierLocation(grade, numerator.GetInt64());
            return new Relation(G(origin, first, second), new Relation(sample, Relation.Segment(sample.Carrier, 0, denominator)));
        }
        foreach (var row in Items(document, "sites"))
        {
            var at = At(row);
            sites.Add(Id(row), Row(at, new Relation(Expression(row.GetProperty("x"), frame.X, at), Expression(row.GetProperty("y"), frame.Y, at))));
        }
        foreach (var row in Items(document, "edges"))
        {
            var at = At(row); var controls = Names(row, "controls").Select(id => sites[id]).ToArray();
            if (controls.Length is < 2 or > 4 || controls.Any(site => RowAt(site) > at))
                throw new InvalidDataException("A line or finite curve needs two to four available control sites.");
            edges.Add(Id(row), Row(at, G(controls)));
        }
        Relation EdgeScope(JsonElement row, string property) => G(Names(row, property).Select(id => (IElement)edges[id]).ToArray());
        var plans = rungRows.Select(row => (IElement)G(EdgeScope(row, "visible"), EdgeScope(row, "guides"),
            EdgeScope(row, "selected"), EdgeScope(row, "introduced"))).ToArray();
        var constraints = Items(document, "constraints").Select(row => (IElement)Row(At(row),
            new Relation(G(Names(row, "container").Select(id => (IElement)sites[id]).ToArray()), EdgeScope(row, "edges")))).ToArray();
        var repeats = Items(document, "repeats").Select(row => (IElement)Row(At(row), new Relation(EdgeScope(row, "source"),
            G(row.GetProperty("instances").EnumerateArray().Select(instance => (IElement)G(instance.EnumerateArray()
                .Select(id => (IElement)edges[id.GetString()!]).ToArray())).ToArray())))).ToArray();
        var circular = Items(document, "circular").Select(row => (IElement)Row(At(row), new Relation(sites[row.GetProperty("center").GetString()!],
            G(Names(row, "ring").Select(id => (IElement)sites[id]).ToArray())))).ToArray();
        var box = document.GetProperty("viewBox").EnumerateArray().Select(v => v.GetInt64()).ToArray();
        if (box.Length != 4 || box[2] <= 0 || box[3] <= 0) throw new InvalidDataException("The drawing frame needs a positive view box.");
        var program = G(frame.Surface, new Relation(Position(frame.X, frame.Y, new(box[0], box[1])),
            Position(frame.X, frame.Y, new(checked(box[0] + box[2]), checked(box[1] + box[3])))),
            G(parameters.Values.Cast<IElement>().ToArray()), G(sites.Values.Cast<IElement>().ToArray()),
            G(edges.Values.Cast<IElement>().ToArray()), G(plans), G(constraints), G(repeats), G(circular));
        if (!TryExecute(program, out _)) throw new InvalidDataException("The construction violates native dependency or rung accounting requirements.");
        return program;
    }

    public static bool TryExecute(Relation program, out Execution read)
    {
        read = null!;
        if (!Fields(program, 9, out var p) || p[0] is not Relation frameSurface || !TryReadFrame(frameSurface, out var frame) ||
            p[1] is not Relation view || !TryPosition(view.Left, frame, out var low) || !TryPosition(view.Right, frame, out var high) ||
            !Elements(p[2], out var parameterRows) || parameterRows.Any(e => e is not Carrier) ||
            !Elements(p[3], out var siteRows) || !Elements(p[4], out var edgeRows) || !Elements(p[5], out var plans) || plans.Length == 0 ||
            !Elements(p[6], out var constraintRows) || !Elements(p[7], out var repeatRows) || !Elements(p[8], out var circularRows) ||
            high.X <= low.X || high.Y <= low.Y) return false;
        var parameters = parameterRows.Cast<Carrier>().ToArray();
        if (parameters.Any(c => c.IsUnknown || c.IsInverse || c.Tension != 0)) return false;
        var siteMap = new Dictionary<Relation, Site>(ReferenceEqualityComparer.Instance);
        var edgeMap = new Dictionary<Relation, Edge>(ReferenceEqualityComparer.Instance);
        var identities = new HashSet<Placeholder>(); var coordinateWitnesses = new List<IElement>();
        bool Row(IElement element, out Relation row, out Placeholder identity, out int at, out IElement body)
        {
            row = null!; identity = null!; at = -1; body = null!;
            if (element is not Relation { Left: Relation { Left: Placeholder { IsInverse: false } ev, Right: CarrierLocation ordinal }, Right: var b } r ||
                !ReferenceEquals(ordinal.Carrier, frame.Order) || ordinal.Ticks < 0 || ordinal.Ticks >= plans.Length || !identities.Add(ev)) return false;
            row = r; identity = ev; at = (int)ordinal.Ticks; body = b; return true;
        }
        bool Coordinate(IElement expression, Carrier axis, int at, bool x, out long value, out Relation witness, out Site[] inputs)
        {
            value = 0; witness = null!; inputs = [];
            if (expression is CarrierLocation literal && ReferenceEquals(literal.Carrier, axis))
            {
                value = literal.Ticks; witness = ReadWitness.Create(new Carrier(), literal, Relation.Segment(axis, 0, 1), literal, G(), literal); return true;
            }
            if (expression is not Relation { Left: var inputScope,
                Right: Relation { Left: CarrierLocation sample, Right: Relation span } } || !Fields(inputScope, 3, out var points) ||
                points[0] is not Relation o || points[1] is not Relation a || points[2] is not Relation b ||
                !siteMap.TryGetValue(o, out var origin) || origin.At > at ||
                !siteMap.TryGetValue(a, out var first) || !siteMap.TryGetValue(b, out var second) || first.At > at || second.At > at ||
                !Span(span, out var start, out var end) || !ReferenceEquals(start.Carrier, sample.Carrier) ||
                !ReferenceEquals(end.Carrier, sample.Carrier) || start.Ticks != 0 || end.Ticks <= 0 ||
                sample.Carrier.IsUnknown || sample.Carrier.IsInverse) return false;
            // A parameter sample must retain the live value tick, not a numeric copy.
            if (parameters.Contains(sample.Carrier) && !sample.SameTickRefAs(sample.Carrier.ValueSlot)) return false;
            var originValue = x ? origin.Position.X : origin.Position.Y;
            var displacement = checked((x ? second.Position.X : second.Position.Y) - (x ? first.Position.X : first.Position.Y));
            var equation = Equation.Affine(span, Relation.Segment(axis, originValue, checked(originValue + displacement)));
            var capturedSample = new CarrierLocation(sample.Carrier, sample.Ticks);
            if (!Equation.TryPredict(equation, capturedSample, axis, out var prediction) ||
                !Equation.TryReadPrediction(prediction, out var output, out var remainder)) return false;
            value = output.Ticks; inputs = [origin, first, second];
            witness = ReadWitness.Create(new Carrier(), expression, equation, output, remainder,
                G(origin.Witness, first.Witness, second.Witness, capturedSample, prediction)); return true;
        }
        foreach (var element in siteRows)
        {
            if (!Row(element, out var row, out var identity, out var at, out var body) || body is not Relation xy ||
                !Coordinate(xy.Left, frame.X, at, true, out var x, out var xw, out var xd) ||
                !Coordinate(xy.Right, frame.Y, at, false, out var y, out var yw, out var yd)) return false;
            var witness = ReadWitness.Create(new Carrier(), row, G(xw, yw), Position(frame.X, frame.Y, new(x, y)),
                G(Residue(xw), Residue(yw)), G(xw, yw));
            var site = new Site(siteMap.Count, at, identity, new(x, y), xd.Concat(yd).Select(s => s.Index).Distinct().ToArray(), row, witness);
            siteMap.Add(row, site); coordinateWitnesses.Add(witness);
        }
        foreach (var element in edgeRows)
        {
            if (!Row(element, out var row, out var identity, out var at, out var body) || !Elements(body, out var controls) || controls.Length is < 2 or > 4 ||
                controls.Any(c => c is not Relation r || !siteMap.ContainsKey(r))) return false;
            var sites = controls.Cast<Relation>().Select(r => siteMap[r]).ToArray(); if (sites.Any(s => s.At > at)) return false;
            var curve = NativeReconstruction.BuildCurve(frame, sites[0].Event, sites[^1].Event, sites.Select(s => s.Position).ToArray());
            if (!NativeReconstruction.TryReadCurve(frame, curve, out var geometry)) return false;
            edgeMap.Add(row, new Edge(edgeMap.Count, at, identity, sites, geometry, row));
        }
        bool ReadEdges(IElement element, out Edge[] edges)
        {
            edges = []; if (!Elements(element, out var entries) || entries.Any(e => e is not Relation r || !edgeMap.ContainsKey(r))) return false;
            edges = entries.Cast<Relation>().Select(r => edgeMap[r]).ToArray(); return edges.Distinct(ReferenceEqualityComparer.Instance).Count() == edges.Length;
        }
        var rungs = new List<Rung>(); var steps = new List<Relation>(); var clock = new Carrier(); var seen = new HashSet<Edge>(ReferenceEqualityComparer.Instance);
        var results = new List<Relation>(); var applications = new List<Relation>();
        for (var i = 0; i < plans.Length; i++)
        {
            if (!Fields(plans[i], 4, out var plan) || !ReadEdges(plan[0], out var visible) || !ReadEdges(plan[1], out var guides) ||
                !ReadEdges(plan[2], out var selected) || !ReadEdges(plan[3], out var introduced) || visible.Intersect<Edge>(guides, ReferenceEqualityComparer.Instance).Any() ||
                visible.Concat(guides).Any(e => e.At > i) || selected.Any(e => !seen.Contains(e)) ||
                introduced.Any(e => e.At != i || seen.Contains(e)) ||
                !introduced.ToHashSet<Edge>(ReferenceEqualityComparer.Instance).SetEquals(edgeMap.Values.Where(e => e.At == i))) return false;
            foreach (var edge in introduced) seen.Add(edge);
            if (visible.Concat(guides).Any(e => !seen.Contains(e))) return false;
            if (!selected.ToHashSet<Edge>(ReferenceEqualityComparer.Instance).SetEquals(i == 0 ? [] : rungs[^1].Visible.Except<Edge>(visible, ReferenceEqualityComparer.Instance))) return false;
            var result = G(visible.Select(e => (IElement)e.Curve.Surface).ToArray());
            var guideContext = G(guides.Select(e => (IElement)e.Curve.Surface).ToArray());
            Relation rung;
            if (i == 0) rung = StructuralLadderSurface.BaseRung(new Carrier(), result);
            else
            {
                var prior = rungs[^1]; var source = results[^1];
                var carried = prior.Visible.Intersect<Edge>(visible, ReferenceEqualityComparer.Instance).Select(e => (IElement)e.Curve.Surface).ToArray();
                var inputs = introduced.SelectMany(e => e.Controls).SelectMany(s => s.Dependencies.Concat(s.At < i ? [s.Index] : []))
                    .Distinct().Select(s => siteMap.Values.ElementAt(s)).ToArray();
                var maps = G(inputs.Select(s => (IElement)new Relation(s.Surface, s.Witness)).Concat(
                    introduced.Select(e => (IElement)new Relation(e.Surface, e.Curve.Surface))).ToArray());
                var focus = FocusSurface.Create(new Carrier(), source, G(guideContext, G(inputs.Select(s => (IElement)s.Surface).ToArray())),
                    G(selected.Select(e => (IElement)e.Curve.Surface).ToArray()), G(carried),
                    G(introduced.Select(e => (IElement)e.Curve.Surface).ToArray()), maps, G(), plans[i]);
                var transform = TransformSurface.Create(new Carrier(), TransformSurface.Ports(new Carrier(), inputs.Select(s => (IElement)s.Surface).ToArray()),
                    maps, TransformSurface.Ports(new Carrier(), introduced.Select(e => (IElement)e.Surface).ToArray()), G(), plans[i]);
                if (!TransformSurface.TryCreateApplication(new Carrier(), transform, focus, maps, result, G(), plans[i], out var application) ||
                    !StructuralLadderSurface.TryCreateRungFromTransformApplication(new Carrier(), application, out rung)) return false;
                applications.Add(application);
                steps.Add(StructuralLadderSurface.TransformStep(new Carrier(), clock, i - 1, i, prior.Surface, application, rung, G()));
            }
            results.Add(result); rungs.Add(new(i, visible, guides, selected, introduced, rung));
        }
        var constraints = new List<Constraint>();
        foreach (var element in constraintRows)
        {
            if (!Row(element, out var row, out _, out var at, out var body) || body is not Relation request ||
                !Elements(request.Left, out var containerRows) || containerRows.Length < 3 ||
                containerRows.Any(e => e is not Relation r || !siteMap.ContainsKey(r)) || !ReadEdges(request.Right, out var constrained)) return false;
            var container = containerRows.Cast<Relation>().Select(r => siteMap[r]).ToArray();
            if (container.Any(s => s.At > at) || constrained.Any(e => e.At > at)) return false;
            var satisfied = ConvexContains(container.Select(s => s.Position).ToArray(), constrained.SelectMany(e => e.Controls).Select(s => s.Position).ToArray());
            IElement result = satisfied.HasValue ? new CarrierLocation(new Carrier(), satisfied.Value ? 1 : 0) : new Placeholder();
            var witness = ReadWitness.Create(new Carrier(), row, request, result,
                satisfied == true ? G() : row, G(container.Select(s => (IElement)s.Witness).ToArray()));
            constraints.Add(new(constraints.Count, at, container, constrained, satisfied, witness));
        }
        var repeats = new List<Relation>();
        foreach (var element in repeatRows)
        {
            if (!Row(element, out var row, out _, out var at, out var body) || body is not Relation request ||
                !ReadEdges(request.Left, out var source) || source.Length == 0 || source.Any(e => e.At > at) || !Elements(request.Right, out var instances) || instances.Length == 0) return false;
            var bindings = new List<IElement>(); var repeatSteps = new List<Relation>(); var repeatClock = new Carrier();
            for (var i = 0; i < instances.Length; i++)
            {
                if (!ReadEdges(instances[i], out var target) || target.Length != source.Length || target.Any(e => e.At > at) ||
                    target.Where((e, j) => e.Controls.Length != source[j].Controls.Length).Any()) return false;
                if (i > 0 && !TranslatedTemplate(source, target)) return false;
                var map = G(source.Select((edge, j) => (IElement)new Relation(edge.Surface, target[j].Surface)).ToArray());
                bindings.Add(map); repeatSteps.Add(TimeStepSequence.Step(new Carrier(), repeatClock, i, i + 1, request.Left, map, instances[i], G()));
            }
            if (!ReadEdges(instances[0], out var first) || !source.SequenceEqual(first)) return false;
            repeats.Add(RepeatSurface.Create(new Carrier(), request.Left, RepeatSurface.Count(new Carrier(), instances.Length),
                G(bindings.ToArray()), G(), G(), TimeStepSequence.Create(new Carrier(), repeatClock, request.Left, repeatSteps.ToArray())));
        }
        var circular = new List<Relation>();
        foreach (var element in circularRows)
        {
            if (!Row(element, out var row, out _, out var at, out var body) || body is not Relation { Left: Relation centerRow, Right: var ringScope } ||
                !siteMap.TryGetValue(centerRow, out var center) || center.At > at || !Elements(ringScope, out var ringRows) || ringRows.Length < 3 ||
                ringRows.Any(e => e is not Relation r || !siteMap.TryGetValue(r, out var s) || s.At > at)) return false;
            circular.Add(Circle(row, center, ringRows.Cast<Relation>().Select(r => siteMap[r]).ToArray()));
        }
        var timeline = StructuralLadderSurface.Timeline(new Carrier(), clock, rungs[0].Surface, steps.ToArray());
        var ladder = StructuralLadderSurface.Create(new Carrier(), rungs[0].Surface,
            StructuralLadderSurface.RungFrame(new Carrier(), rungs.Skip(1).Select(r => r.Surface).ToArray()), timeline, G(), program);
        var execution = G(program, ladder, G(coordinateWitnesses.ToArray()), G(edgeMap.Values.Select(e => (IElement)e.Curve.Surface).ToArray()),
            G(constraints.Select(c => (IElement)c.Witness).ToArray()), G(repeats.Cast<IElement>().ToArray()), G(circular.Cast<IElement>().ToArray()));
        read = new(frame, [low.X, low.Y, checked(high.X - low.X), checked(high.Y - low.Y)], parameters,
            siteMap.Values.ToArray(), edgeMap.Values.ToArray(), rungs.ToArray(), constraints.ToArray(), repeats.ToArray(), circular.ToArray(), ladder, execution);
        return true;
    }

    public static bool? ConvexContains(Point[] boundary, Point[] points)
    {
        if (boundary.Length < 3 || points.Length == 0) return null;
        BigInteger Cross(Point a, Point b, Point c) => ((BigInteger)b.X - a.X) * ((BigInteger)c.Y - a.Y) - ((BigInteger)b.Y - a.Y) * ((BigInteger)c.X - a.X);
        if (boundary.Distinct().Count() != boundary.Length) return null;
        var winding = boundary.Select((p, i) => Cross(p, boundary[(i + 1) % boundary.Length], boundary[(i + 2) % boundary.Length]).Sign).ToArray();
        if (winding.Any(s => s == 0) || winding.Distinct().Count() != 1 ||
            boundary.Where((p, i) => boundary.Where((_, j) => j != i && j != (i + 1) % boundary.Length)
                .Any(q => Cross(p, boundary[(i + 1) % boundary.Length], q).Sign != winding[0])).Any()) return null;
        return points.All(point => boundary.Select((p, i) => Cross(p, boundary[(i + 1) % boundary.Length], point).Sign)
            .All(sign => sign == 0 || sign == winding[0]));
    }

    private static bool TranslatedTemplate(Edge[] source, Edge[] target)
    {
        var correspondence = new Dictionary<Site, Site>(ReferenceEqualityComparer.Instance);
        var inverse = new Dictionary<Site, Site>(ReferenceEqualityComparer.Instance);
        (BigInteger X, BigInteger Y)? displacement = null;
        for (var e = 0; e < source.Length; e++)
        for (var c = 0; c < source[e].Controls.Length; c++)
        {
            var a = source[e].Controls[c]; var b = target[e].Controls[c];
            if (b.Surface.Right is not Relation xy || !Origin(xy.Left, a.Surface, true) || !Origin(xy.Right, a.Surface, false) ||
                correspondence.TryGetValue(a, out var old) && !ReferenceEquals(old, b) ||
                inverse.TryGetValue(b, out var prior) && !ReferenceEquals(prior, a)) return false;
            correspondence[a] = b; inverse[b] = a;
            var delta = ((BigInteger)b.Position.X - a.Position.X, (BigInteger)b.Position.Y - a.Position.Y);
            if (displacement.HasValue && displacement.Value != delta) return false;
            displacement = delta;
        }
        return true;
        static bool Origin(IElement expression, Relation source, bool x)
        {
            var seen = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
            while (expression is Relation locator && Fields(locator.Left, 3, out var fields) && fields[0] is Relation origin && seen.Add(origin))
            {
                if (ReferenceEquals(origin, source)) return true;
                if (origin.Right is not Relation coordinates) return false;
                expression = x ? coordinates.Left : coordinates.Right;
            }
            return false;
        }
    }

    private static Relation Circle(Relation request, Site center, Site[] ring)
    {
        var origin = Relation.Point(new Carrier(), 0);
        var outward = Relation.OrthogonalOutlet(origin);
        var contour = new Carrier(UnitShape.CircularFrom(origin));
        var ray = new Carrier(UnitShape.LinearFrom(new Relation(origin, origin)));
        var a = Relation.Point(ray, 0); var b = Relation.Point(ray, 1); var radius = new Relation(a, b);
        var start = Relation.Point(contour, 0); var end = Relation.Point(contour, 1); var whole = new Relation(start, end);
        var circle = G(new Relation(outward, new Relation(radius, contour)), G(new Relation(origin, a), radius, new Relation(b, start)), whole, new Relation(end, start));
        var parts = new Carrier(UnitShape.SubdivisionOf(contour.UnitShape, ring.Length));
        var partition = G(circle, new Relation(Relation.Segment(parts, 0, ring.Length), whole));
        if (!ContourConstruction.TryReadPartition(partition, out var partitionWitness)) throw new InvalidDataException("Circle partition construction is unreadable.");
        var correspondences = new List<IElement>();
        for (var i = 0; i < ring.Length; i++)
        {
            if (!ContourConstruction.TryLocate(G(partition, Relation.Point(parts, i)), out var marker)) throw new InvalidDataException("Contour marker is unreadable.");
            // The finite sampled XY association is authored calibration. It is
            // explicitly open as an exact arbitrary-angle geometric equivalence.
            correspondences.Add(ReadWitness.Create(new Carrier(), marker, new Relation(center.Surface, ring[i].Surface), ring[i].Witness, new Placeholder(), request));
        }
        return ReadWitness.Create(new Carrier(), request, partitionWitness, G(correspondences.ToArray()), new Placeholder(), new Relation(origin, center.Surface));
    }

    private static Relation G(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);
    private static bool Span(Relation relation, out CarrierLocation start, out CarrierLocation end)
    {
        start = default; end = default;
        if (relation is not { Left: Relation { Left: CarrierLocation a, Right: Carrier ca }, Right: Relation { Left: CarrierLocation b, Right: Carrier cb } } ||
            !ReferenceEquals(a.Carrier, ca) || !ReferenceEquals(b.Carrier, cb) || !ReferenceEquals(ca, cb)) return false;
        start = a; end = b; return true;
    }
    private static bool Elements(IElement element, out IElement[] fields)
    { if (element is Relation r && r.TryReadOrderedGroup(out fields)) return true; fields = []; return false; }
    private static bool Fields(IElement element, int count, out IElement[] fields) => Elements(element, out fields) && fields.Length == count;
    private static IElement Residue(Relation witness) { ReadWitness.TryRead(witness, out _, out _, out _, out var residue, out _); return residue; }
    private static int RowAt(Relation row) => checked((int)((CarrierLocation)((Relation)row.Left).Right).Ticks);
    private static string Id(JsonElement element) => element.GetProperty("id").GetString() ?? throw new InvalidDataException("A source row needs an id.");
    private static IEnumerable<JsonElement> Items(JsonElement element, string name) => element.TryGetProperty(name, out var items) ? items.EnumerateArray() : [];
    private static IEnumerable<string> Names(JsonElement element, string name) => Items(element, name).Select(e => e.GetString()!);
}
