using Gojo.Core;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>Builders only: every operative dependency is retained in the returned graph.</summary>
public sealed class ConstructionRecipes
{
    public Carrier X { get; } = new();
    public Carrier Y { get; }
    public Carrier Grade { get; } = new();
    public Relation Basis { get; }
    public Relation Metric { get; }
    public Relation QuarterTurn { get; }
    public Relation Circle { get; }
    public Relation Zero => Point(0, 0);

    public ConstructionRecipes()
    {
        Y = new Carrier(X.UnitShape);
        Basis = G(Relation.Segment(X, 0, 1), Relation.Segment(Y, 0, 1));
        var axes = F(Basis); var square = new Carrier(UnitShape.Product(X.UnitShape, X.UnitShape));
        Metric = G(Basis, G(new Relation(new Relation(axes[0], axes[0]), new CarrierLocation(square, 1)),
            new Relation(new Relation(axes[0], axes[1]), new CarrierLocation(square, 0)),
            new Relation(new Relation(axes[1], axes[1]), new CarrierLocation(square, 1))));
        var source = Relation.Point(new Carrier(), 0);
        var outlet = Relation.OrthogonalOutlet(source);
        if (!outlet.TryCrystallize(out var contour) ||
            !Relation.OrthogonalOutlet(new Relation(source, source)).TryCrystallize(out var ray))
            throw new InvalidOperationException("The point-derived contour basis is unavailable.");
        var radius = Relation.Segment(ray, 0, 1);
        var start = Relation.Point(contour, 0); var end = Relation.Point(contour, 1);
        var whole = new Relation(start, end);
        Circle = G(new Relation(outlet, new Relation(radius, contour)),
            G(new Relation(source, radius.Left), radius, new Relation(radius.Right, start)),
            whole, new Relation(end, start));
        var fine = new Carrier(UnitShape.SubdivisionOf(contour.UnitShape, 4));
        var partition = G(Circle, new Relation(Relation.Segment(fine, 0, 4), whole));
        var centre = Zero;
        if (!ContourConstruction.TryCalibrateQuarters(G(partition, Metric, new Relation(source, centre),
            new Relation(radius, Relation.Segment(X, 0, 1)), new Relation(centre, Point(1, 0)),
            new Relation(centre, Point(0, 1))), out var calibration))
            throw new InvalidOperationException("The explicit unit metric could not calibrate the contour.");
        var surface = F(calibration)[2]; var marker = ((Relation)F(F(surface)[3])[0]).Left;
        if (!ContourRotation.TryTraverse(G(surface, marker, Relation.Segment(new Carrier(), 0, 1)), out var turn))
            throw new InvalidOperationException("The constructed contour could not expose a quarter traversal.");
        QuarterTurn = turn;
    }

    public Relation Point(long x, long y) => Relation.OrderedGroup((Carrier)Basis.Right,
        new CarrierLocation(X, x), new CarrierLocation(Y, y));
    public Relation Plus(IElement a, IElement b) => Add(Basis, Zero, a, b, Grade);
    public Relation Times(IElement point, IElement sample, Carrier grade, long denominator = 1) =>
        Scale(Basis, Zero, point, sample, denominator, grade);
    public Relation Times(IElement point, long n, long d = 1) => Times(point, new CarrierLocation(Grade, n), Grade, d);
    public Relation Between(IElement a, IElement b, long n, long d) =>
        Interpolate(Basis, a, b, new CarrierLocation(Grade, n), d, Grade);

    /// <summary>
    /// A phrase of move/turn beats with live length sources. State is position and
    /// heading; feedback connects both output roles to the next input roles.
    /// </summary>
    public Relation Path(IElement origin, IElement heading, Carrier[] lengths, int repeats,
        IElement? turn = null)
    {
        var position = new Placeholder(); var direction = new Placeholder(); var length = new Placeholder();
        var outPosition = new Placeholder(); var outDirection = new Placeholder();
        // Each length is explicitly calibrated from its own carrier into this
        // transition's parameter basis by the per-beat binding below.
        var stepGrade = new Carrier();
        var moved = Plus(position, Times(direction, length, stepGrade));
        IElement turned = Turn(turn ?? QuarterTurn, direction);
        var transition = TransformSurface.Create(new Carrier(), G(position, direction, length),
            G(moved, turned), G(outPosition, outDirection), G(), G(Basis, turn ?? QuarterTurn));
        var beats = lengths.Select(c => (IElement)new Relation(c.ValueSlot,
            new Relation(Relation.Segment(c, 0, 1), Relation.Segment(stepGrade, 0, 1)))).ToArray();
        var feedback = G(G(new Relation(position, origin), new Relation(direction, heading)),
            G(new Relation(outPosition, position), new Relation(outDirection, direction)));
        var repeat = RepeatSurface.Create(new Carrier(), G(beats), Relation.Segment(new Carrier(), 0, repeats),
            transition, feedback, new Relation(outPosition, origin), new Placeholder());
        return TransformSurface.Create(new Carrier(), G(origin, heading, G(lengths)), repeat,
            G(new Placeholder()), new Relation(outPosition, origin), G(Basis, Metric));
    }

    /// <summary>
    /// Two independent ordered spans. Opposite edges consume the same translated
    /// displacement. Rectangle status comes from the selected metric axis pairing.
    /// </summary>
    public Relation Offsets(IElement origin, Carrier width, Carrier height)
        => Offsets(origin, width.ValueSlot, width, height.ValueSlot, height);

    /// <summary>Same machine with unresolved samples, before metric amounts are supplied.</summary>
    public Relation Offsets(IElement origin, IElement widthSample, Carrier widthBasis,
        IElement heightSample, Carrier heightBasis)
    {
        var u = Times(Point(1, 0), widthSample, widthBasis);
        var v = Times(Point(0, 1), heightSample, heightBasis);
        var a = origin; var b = Plus(a, u); var d = Plus(a, v); var c = Plus(b, v);
        var families = G(G(new Relation(a, b), new Relation(d, c), v),
            G(new Relation(a, d), new Relation(b, c), u));
        return Direct(G(a, b, c, d), G(origin, widthSample, heightSample), families);
    }

    /// <summary>
    /// Centre and two antipodal pairs. Reflection in the two explicitly selected
    /// perpendicular axes gives equal squared radial reach, without pretending that
    /// rectangular corners are equally spaced around a circle.
    /// </summary>
    public Relation OppositePairs(IElement centre, Carrier halfWidth, Carrier halfHeight)
    {
        var dx = Times(Point(1, 0), halfWidth.ValueSlot, halfWidth);
        var dy = Times(Point(0, 1), halfHeight.ValueSlot, halfHeight);
        var r0 = Plus(Times(dx, -1), Times(dy, -1));
        var r1 = Plus(dx, Times(dy, -1));
        var r2 = Times(r0, -1); var r3 = Times(r1, -1);
        IElement[] points = [Plus(centre, r0), Plus(centre, r1), Plus(centre, r2), Plus(centre, r3)];
        var radials = G(points.Select(p => (IElement)new Relation(centre, p)).ToArray());
        var diameters = G(new Relation(points[0], points[2]), new Relation(points[1], points[3]));
        return Direct(G(points), G(centre, halfWidth, halfHeight), G(radials, diameters, Metric));
    }

    private Relation Direct(Relation outputs, IElement inputs, IElement construction) =>
        TransformSurface.Create(new Carrier(), inputs, outputs,
            G(F(outputs).Select(_ => (IElement)new Placeholder()).ToArray()), G(), G(Basis, Metric, construction));
}

public static class MachineRunner
{
    // Application = G(definition, external bindings, outputs, trace, remaining demands, output bindings).
    // The interpreter selects the retained expression/recurrence surfaces, never a
    // construction-name enum. Native readers perform the numeric observations.
    public static Relation Run(Relation machine, params Relation[] bindings)
    {
        var definition = F(machine);
        if (definition.Length != 5) throw new ArgumentException("An exact transform surface is required.");
        var witnesses = new List<Relation>(); var outputs = new List<IElement>();
        var remainder = new List<IElement>(); var operation = definition[1];
        var parts = F(operation);
        var repeated = parts.Length == 6 && RepeatSurface.TryReadSpan(parts[1], out _, out _);
        if (repeated)
        {
            RepeatSurface.TryReadSpan(parts[1], out var start, out var end);
            var count = checked(end.Ticks - start.Ticks);
            if (count < 0 || count > 64) throw new ArgumentOutOfRangeException(nameof(machine), "The selected repeat budget is 0..64.");
            var transition = F(parts[2]); var inputRoles = F(transition[0]); var outputRoles = F(transition[2]);
            var expressions = F(transition[1]); var feedback = F(parts[3]);
            var state = F(feedback[0]).Cast<Relation>().ToArray(); var feeds = F(feedback[1]).Cast<Relation>().ToArray();
            var current = state.Select(b => new Relation(b.Left, Evaluate(b.Right, bindings, witnesses))).ToArray();
            var initial = current[0].Right; var clock = new Carrier(); var steps = new List<Relation>();
            outputs.Add(initial); long step = 0;
            for (long repetition = 0; repetition < count; repetition++)
                foreach (var selected in F(parts[0]))
                {
                    var beat = (Relation)selected;
                    if (beat.Left is not CarrierLocation sample || beat.Right is not Relation calibration ||
                        calibration.Right is not Relation target || !TimeStepSequence.TryReadSpan(target, out var targetStart, out _) ||
                        !calibration.TryProject(sample, targetStart.Carrier, out var projection) ||
                        !projection.TryReadProjection(out var projected, out var residue) ||
                        !ProjectionResidue.TryRead(residue, out _, out var residual, out _) || residual != 0)
                    { remainder.Add(selected); continue; }
                    witnesses.Add(projection);
                    var frame = bindings.Concat(current).Append(new Relation(inputRoles[2], projected)).ToArray();
                    var next = expressions.Select(e => Evaluate(e, frame, witnesses)).ToArray();
                    var resultBindings = outputRoles.Zip(next).Select(p => new Relation(p.First, p.Second)).ToArray();
                    var nextState = feeds.Select(feed => new Relation(feed.Right,
                        resultBindings.Single(b => ReferenceEquals(b.Left, feed.Left)).Right)).ToArray();
                    steps.Add(TimeStepSequence.Step(new Carrier(), clock, step, ++step, G(current),
                        G(parts[2], selected), G(nextState), G()));
                    current = nextState; outputs.Add(next[0]);
                }
            var result = outputs[^1];
            var basis = F(definition[4])[0];
            AffineFrame.TryDifference(G(initial, result, basis), out var closure);
            witnesses.Add(closure);
            if (!IsZeroVector(F(closure)[2])) remainder.Add(closure);
            var timeline = TimeStepSequence.Create(new Carrier(), clock, G(state), steps.ToArray());
            witnesses.Add(RepeatSurface.Create(new Carrier(), parts[0], parts[1], parts[2], parts[3], G(remainder.ToArray()), timeline));
        }
        else outputs.AddRange(parts.Select(e => Evaluate(e, bindings, witnesses)));
        foreach (var output in outputs)
            if (!TryNumbers(output, out _)) remainder.Add(output);
        var ports = F(definition[2]);
        var outputFrame = G(outputs.ToArray());
        var outputBindings = repeated && ports.Length == 1
            ? G(new Relation(ports[0], outputFrame))
            : ports.Length == outputs.Count
                ? G(ports.Zip(outputs).Select(pair => (IElement)new Relation(pair.First, pair.Second)).ToArray())
                : G();
        if (F(outputBindings).Length != ports.Length) remainder.Add(definition[2]);
        return G(machine, G(bindings), outputFrame, G(witnesses.ToArray()), G(remainder.ToArray()), outputBindings);
    }

    public static bool TryNumbers(IElement vector, out (System.Numerics.BigInteger N, System.Numerics.BigInteger D)[] values)
    {
        values = [];
        if (vector is not Relation group || !group.TryReadOrderedGroup(out var coordinates) || coordinates.Length != 2) return false;
        var result = new List<(System.Numerics.BigInteger, System.Numerics.BigInteger)>();
        foreach (var coordinate in coordinates)
        {
            if (!ProjectionResidue.TryRead(coordinate, out _, out var n, out var d)) return false;
            result.Add((n, d));
        }
        values = result.ToArray(); return true;
    }
    public static bool IsZeroVector(IElement vector) => TryNumbers(vector, out var n) && n.All(v => v.N.IsZero);
}
