using System.Numerics;
using Gojo.Core;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>
/// A small operational shell over a native interface. This class caches references;
/// its Surface contains the roles, boundary, current bindings, selected machine and
/// witnessed publications. Attachments consume the stable roles, not this object.
/// </summary>
public sealed class ShapeInterface
{
    public Relation Roles { get; }
    public Relation Boundary { get; }
    public Relation Bindings { get; private set; } = G();
    public Relation Timeline { get; private set; }
    public IElement Selected { get; private set; } = new Placeholder();
    public Relation Surface => G(Roles, Boundary, Bindings, Selected, Timeline);
    public Relation? LastAttempt { get; private set; }
    private readonly Carrier clock = new();
    private readonly List<Relation> steps = [];
    private readonly IElement initial;

    public ShapeInterface()
    {
        var roles = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray();
        Roles = G(roles);
        Boundary = G(Enumerable.Range(0, 4).Select(i => (IElement)new Relation(roles[i], roles[(i + 1) % 4])).ToArray());
        initial = G(Roles, Boundary, Bindings);
        Timeline = TimeStepSequence.Create(new Carrier(), clock, initial);
    }

    public Relation[] CurrentBindings => F(Bindings).Cast<Relation>().ToArray();
    public IElement Corner(int index) => F(Roles)[index];
    public Relation Side(int index) => (Relation)F(Boundary)[index];

    public bool Publish(Relation application)
    {
        var a = F(application); var outputs = F(a[2]); var roles = F(Roles);
        var capturedBefore = CaptureBindings();
        var bad = new List<IElement>(F(a[4]));
        if (!CoherentMetric(a[0], outputs)) bad.Add(a[2]);
        if (!ConnectedOutputs(a)) bad.Add(a[0]);
        if (outputs.Length is not (4 or 5)) bad.Add(a[2]);
        if (outputs.Length >= 4 && !Rectangle(outputs.Take(4).ToArray())) bad.Add(a[2]);
        if (outputs.Length == 5 && !EqualPoint(outputs[0], outputs[4])) bad.Add(new Relation(outputs[4], outputs[0]));
        // This public interface explicitly selects whole ticks. Refuse fractional
        // output atomically, retaining it for a future finer interface.
        var coordinates = new List<CarrierLocation>();
        foreach (var output in outputs.Take(4))
        {
            if (!MachineRunner.TryNumbers(output, out var values) || values.Any(v => v.D != 1 || v.N < long.MinValue || v.N > long.MaxValue))
            { bad.Add(output); continue; }
            foreach (var c in F(output))
            {
                ProjectionResidue.TryRead(c, out var axis, out var n, out _);
                coordinates.Add(new CarrierLocation(axis, (long)n));
            }
        }
        if (bad.Count > 0 || coordinates.Count != 8) return Record(false, G(bad.ToArray()), G());
        if (CurrentBindings.Length == 0)
        {
            Bindings = G(roles.Select((role, i) => (IElement)new Relation(role,
                Relation.OrderedGroup((Carrier)((Relation)outputs[i]).Right,
                    new CarrierLocation(coordinates[i * 2].Carrier, new TickRef(coordinates[i * 2].Ticks)),
                    new CarrierLocation(coordinates[i * 2 + 1].Carrier, new TickRef(coordinates[i * 2 + 1].Ticks))))).ToArray());
            return Record(true, G(), G(CurrentBindings));
        }
        var old = CurrentBindings.SelectMany(b => F(b.Right).Cast<CarrierLocation>()).ToArray();
        if (old.Where((c, i) => !c.Carrier.SameIdentityAs(coordinates[i].Carrier)).Any())
            return Record(false, G(a[2]), G());
        var sources = new List<IElement>(); var targets = new List<IElement>(); var incoming = new List<IElement>();
        for (var i = 0; i < old.Length; i++)
        {
            // One-tick calibration ending at the actual public TickRef. It works
            // at coordinate zero too; a zero-valued point port would have no scale.
            var low = checked(old[i].Ticks - 1);
            sources.Add(Relation.Segment(old[i].Carrier, 0, 1));
            targets.Add(new Relation(Relation.Point(old[i].Carrier, low), new Relation(old[i], old[i].Carrier)));
            incoming.Add(new CarrierLocation(old[i].Carrier, checked(coordinates[i].Ticks - low)));
        }
        var port = new Relation(Tree(sources), Tree(targets)); var supplied = Tree(incoming);
        var accepted = port.TrySetStructureFrom(supplied);
        return Record(accepted, accepted ? G() : G(application), G(port, supplied));

        bool Record(bool accepted, Relation remainder, Relation route)
        {
            if (accepted) Selected = a[0];
            var privatePorts = F(F(a[0])[2]);
            var targetRoles = outputs.Length == 5 ? G(roles.Concat([roles[0]]).ToArray()) : Roles;
            var portMap = privatePorts.Length == 1
                ? G(new Relation(privatePorts[0], targetRoles))
                : G(privatePorts.Zip(roles).Select(p => (IElement)new Relation(p.First, p.Second)).ToArray());
            var correspondence = G(portMap, G(CurrentBindings));
            var patch = TransformSurface.Patch(new Carrier(), capturedBefore, a[0], Roles, Boundary,
                G(), a[2], correspondence, CaptureBindings(), remainder, route);
            LastAttempt = ReadWitness.Create(new Carrier(), application, G(Roles, Boundary),
                accepted ? CaptureBindings() : new Placeholder(), remainder, patch);
            steps.Add(TimeStepSequence.Step(new Carrier(), clock, steps.Count, steps.Count + 1,
                capturedBefore, LastAttempt, CaptureBindings(), remainder));
            Timeline = TimeStepSequence.Create(new Carrier(), clock, initial, steps.ToArray());
            return accepted;
        }
    }

    public Relation CaptureBindings() => G(CurrentBindings.Select(binding => (IElement)new Relation(binding.Left,
        Relation.OrderedGroup((Carrier)((Relation)binding.Right).Right,
            F(binding.Right).Cast<CarrierLocation>().Select(c => (IElement)new CarrierLocation(c.Carrier, c.Ticks)).ToArray()))).ToArray());

    private static IElement Tree(IReadOnlyList<IElement> values) => values.Count == 1 ? values[0]
        : new Relation(values[0], Tree(values.Skip(1).ToArray()));

    private static bool ConnectedOutputs(IElement[] application)
    {
        if (application.Length != 6) return false;
        var ports = F(F(application[0])[2]); var values = F(application[2]); var bindings = F(application[5]);
        if (bindings.Length != ports.Length) return false;
        for (var i = 0; i < ports.Length; i++)
        {
            if (bindings[i] is not Relation binding || !ReferenceEquals(binding.Left, ports[i])) return false;
            if (ports.Length == values.Length)
            { if (!ReferenceEquals(binding.Right, values[i])) return false; }
            else if (ports.Length != 1 || !ReferenceEquals(binding.Right, application[2])) return false;
        }
        return true;
    }

    private static bool CoherentMetric(IElement machine, IElement[] outputs)
    {
        if (machine is not Relation m || !m.TryReadOrderedGroup(out var definition) || definition.Length != 5 ||
            definition[4] is not Relation provenance || !provenance.TryReadOrderedGroup(out var context) || context.Length < 2 ||
            context[0] is not Relation basis || !basis.TryReadOrderedGroup(out var spans) || spans.Length != 2 ||
            context[1] is not Relation metric || !metric.TryReadOrderedGroup(out var metricFields) || metricFields.Length != 2 ||
            !ReferenceEquals(metricFields[0], basis) || metricFields[1] is not Relation gram ||
            !gram.TryReadOrderedGroup(out var pairings) || pairings.Length != 3) return false;
        var axes = new Carrier[2];
        for (var i = 0; i < 2; i++)
        {
            if (spans[i] is not Relation span || !TimeStepSequence.TryReadSpan(span, out var start, out var end) ||
                start.Ticks != 0 || end.Ticks != 1 || start.Carrier.IsUnknown || start.Carrier.IsInverse) return false;
            axes[i] = start.Carrier;
        }
        if (axes[0].SameIdentityAs(axes[1]) || !axes[0].UnitShape.CompatibleWith(axes[1].UnitShape)) return false;
        Carrier? square = null;
        for (var i = 0; i < 3; i++)
        {
            if (pairings[i] is not Relation { Left: Relation pair, Right: var value } ||
                !ReferenceEquals(pair.Left, spans[i == 2 ? 1 : 0]) || !ReferenceEquals(pair.Right, spans[i == 0 ? 0 : 1]) ||
                !ProjectionResidue.TryRead(value, out var unit, out var n, out var d) || n != (i == 1 ? 0 : d) ||
                !unit.UnitShape.CompatibleWith(UnitShape.Product(axes[0].UnitShape, axes[0].UnitShape)) ||
                square is not null && !square.SameIdentityAs(unit)) return false;
            square = unit;
        }
        return outputs.All(output => output is Relation vector && ReferenceEquals(vector.Right, basis.Right) &&
            vector.TryReadOrderedGroup(out var coordinates) && coordinates.Length == 2 &&
            coordinates.Select((coordinate, i) => ProjectionResidue.TryRead(coordinate, out var axis, out _, out _) &&
                axis.SameIdentityAs(axes[i])).All(ok => ok));
    }
    private static bool EqualPoint(IElement a, IElement b) => MachineRunner.TryNumbers(a, out var x) &&
        MachineRunner.TryNumbers(b, out var y) && x.Zip(y).All(p => p.First.N * p.Second.D == p.Second.N * p.First.D);

    private static bool Rectangle(IElement[] points)
    {
        if (points.Length != 4 || points.Any(p => !MachineRunner.TryNumbers(p, out _))) return false;
        // Rational cross multiplication keeps validation exact and independent of pixels.
        var p = points.Select(v => { MachineRunner.TryNumbers(v, out var n); return n; }).ToArray();
        var denominator = p.SelectMany(v => v).Aggregate(BigInteger.One, (d, v) => d * v.D);
        var xy = p.Select(v => v.Select(n => n.N * (denominator / n.D)).ToArray()).ToArray();
        var edges = Enumerable.Range(0, 4).Select(i => new[] { xy[(i + 1) % 4][0] - xy[i][0], xy[(i + 1) % 4][1] - xy[i][1] }).ToArray();
        return edges.All(e => !e.All(n => n.IsZero)) &&
            Enumerable.Range(0, 4).All(i => edges[i][0] * edges[(i + 1) % 4][0] + edges[i][1] * edges[(i + 1) % 4][1] == 0) &&
            edges[0][0] + edges[2][0] == 0 && edges[0][1] + edges[2][1] == 0;
    }
}

/// <summary>Consumers see only exposed roles/sides. Replaying them never inspects a maker.</summary>
public static class Attachments
{
    public static Relation Roof(ConstructionRecipes c, Relation top, Carrier rise, long ridgeN = 1, long ridgeD = 2)
    {
        var ridge = c.Plus(c.Between(top.Left, top.Right, ridgeN, ridgeD), c.Times(c.Point(0, 1), rise.ValueSlot, rise));
        return G(new Relation(top.Left, ridge), new Relation(ridge, top.Right), top);
    }

    // G(complete side, selected opening, carried complement, doorway outline).
    // Opening fractions are independent of the underlying rectangle construction.
    public static Relation Door(ConstructionRecipes c, Relation floor, Carrier height,
        long start = 3, long end = 5, long parts = 8, bool arch = false)
    {
        var left = c.Between(floor.Left, floor.Right, start, parts);
        var right = c.Between(floor.Left, floor.Right, end, parts);
        var rise = c.Times(c.Point(0, 1), height.ValueSlot, height);
        var a = c.Plus(left, rise); var b = c.Plus(right, rise);
        var cap = arch ? QuadraticCap(c, a, c.Plus(c.Between(a, b, 1, 2), c.Times(rise, 1, 2)), b) : G(a, b);
        return G(floor, new Relation(left, right), G(new Relation(floor.Left, left), new Relation(right, floor.Right)),
            G(G(left, a), cap, G(b, right)));
    }

    /// <summary>
    /// A retained de Casteljau machine. Its sample role remains open until a caller
    /// supplies a value in the exposed grade. Curvedness belongs to the three
    /// connected interpolations, not to a renderer interpreting a list of controls.
    /// Provenance retains basis, metric, endpoint surface, controls and sample grade.
    /// </summary>
    public static Relation QuadraticCap(ConstructionRecipes c, IElement first, IElement control, IElement last)
    {
        var sample = new Placeholder(); var grade = new Carrier();
        var left = Interpolate(c.Basis, first, control, sample, 1, grade, 0, 1);
        var right = Interpolate(c.Basis, control, last, sample, 1, grade, 0, 1);
        var point = Interpolate(c.Basis, left, right, sample, 1, grade, 0, 1);
        return TransformSurface.Create(new Carrier(), G(sample), G(point), G(new Placeholder()), G(),
            G(c.Basis, c.Metric, new Relation(first, last), G(first, control, last), grade));
    }

    // A hinge is a shared public endpoint; changing the retained traversal changes
    // the leaf while the wall opening and hinge remain available to other consumers.
    public static Relation WindowLeaf(ConstructionRecipes c, IElement hinge, IElement reach, Relation traversal) =>
        G(hinge, c.Plus(hinge, Turn(traversal, reach)), G(hinge, reach, traversal));

    public static Relation Read(Relation attachment, IReadOnlyList<Relation> bindings)
    {
        var witnesses = new List<Relation>();
        var memo = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        IElement Visit(IElement e)
        {
            if (memo.TryGetValue(e, out var prior)) return prior;
            if (e is Placeholder) return memo[e] = Evaluate(e, bindings, witnesses);
            if (e is not Relation r) return e;
            // Preserve executable nested templates. Bind only the advertised
            // external roles; their own sample/output slots remain open.
            if (r.TryReadOrderedGroup(out var machine) && machine.Length == 5 &&
                machine[0] is Relation inputs && inputs.TryReadOrderedGroup(out var inputRoles) &&
                inputRoles.Length > 0 && inputRoles.All(role => role is Placeholder) &&
                machine[1] is Relation operation && operation.TryReadOrderedGroup(out _) &&
                machine[2] is Relation outputs && outputs.TryReadOrderedGroup(out var outputRoles) &&
                outputRoles.Length > 0 && outputRoles.All(role => role is Placeholder))
                return memo[e] = Gojo.Patterns.StructuralPattern.Refine(r, bindings);
            var read = Evaluate(r, bindings, witnesses);
            if (!ReferenceEquals(read, r)) return memo[e] = read;
            if (r.TryReadOrderedGroup(out var f)) return memo[e] = Relation.OrderedGroup((Carrier)r.Right, f.Select(Visit).ToArray());
            return memo[e] = new Relation(Visit(r.Left), Visit(r.Right));
        }
        return ReadWitness.Create(new Carrier(), attachment, G(bindings.ToArray()), Visit(attachment), G(), G(witnesses.ToArray()));
    }
}
