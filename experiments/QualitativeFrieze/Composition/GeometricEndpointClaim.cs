using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>
/// Compares two advertised endpoint positions along one selected cardinal axis.
/// A connected response word says how one actuator tick moves one endpoint.
/// The packet exposes only decrease/hold/increase; transverse displacement and
/// coordinate coincidence remain separate private reads. No read welds sites.
/// This supplied response correspondence is a premise, not a discovered law.
/// </summary>
public sealed class GeometricEndpointClaim
{
    // surface = (frame -> Closed(first role, second role)) -> Closed(
    //   selected axis, extent port -> (moved role -> positive motion word),
    //   Closed(measure tick -> Closed(reach), actuator tick -> motion word),
    //   Closed(sensor, receiver, comparison basis, comparison domain),
    //   measured basis, polarity basis, finite response law, opaque identity)
    // endpoint port = actual site -> position word in the selected frame.
    // A private observation binds both ports, reduces second - first and keeps
    // its along/transverse words. Zero along that axis is not full coincidence,
    // and even full coordinate coincidence does not imply shared-site identity.
    // The selected response can have transverse effects; this claim promises
    // only a correction of its own projection, not simultaneous joint success.

    private readonly Curve.Frame frame;
    public Relation Surface { get; }

    public sealed record Observation(Relation Packet, byte Mask,
        long? AlongDifference, long? TransverseDifference,
        bool? AxisAgreement, bool? CoordinateCoincident,
        bool GeometryComplete, bool Complete, Relation TransverseResidue,
        Relation Witness, Relation LawRead);

    private sealed record Contract(IElement FirstRole, IElement SecondRole,
        Relation Axis, Relation ExtentPort, IElement MovedRole, Relation Motion,
        Relation Calibration, IElement Sensor, IElement Owner, Carrier OutputBasis,
        Relation OutputDomain, Carrier MeasuredBasis, Carrier PolarityBasis,
        Relation Law, IElement Identity, bool Calibrated);

    private GeometricEndpointClaim(Curve.Frame frame, Relation surface)
    { this.frame = frame; Surface = surface; }

    public static GeometricEndpointClaim Create(Curve.Frame frame,
        CoupledExtentMachine receiver, Relation selectedAxis,
        Relation positiveMotionWord, bool moveSecond = true)
    {
        if (!Port(receiver.ExtentPort, out var extent))
            throw new ArgumentException("The claim needs the actuator's actual advertised value port.", nameof(receiver));
        var first = new Placeholder(); var second = new Placeholder();
        var measured = new Carrier(); var polarity = new Carrier();
        var comparisons = new long[] { -1, 0, 1 }
            .Select(n => (IElement)new CarrierLocation(measured, n)).ToArray();
        var polarities = new IElement[] { new CarrierLocation(polarity, -1), new CarrierLocation(polarity, 1) };
        var outputs = E(receiver.ComparisonDomain); var rows = new List<Relation>();
        for (var i = 0; i < comparisons.Length; i++)
            foreach (CarrierLocation face in polarities)
                rows.Add(FiniteLaw.Row(comparisons[i], face,
                    S(outputs[checked((int)((i - 1) * face.Ticks + 1))])));
        var law = FiniteLaw.Create(S(comparisons), S(polarities), receiver.ComparisonDomain, rows.ToArray());
        var calibration = S(new Relation(Relation.Segment(measured, 0, 1), S(frame.Reach)),
            new Relation(Relation.Segment(extent, 0, 1), positiveMotionWord));
        var output = S(receiver.Sensor, receiver.Owner, receiver.ComparisonBasis, receiver.ComparisonDomain);
        var response = new Relation(receiver.ExtentPort,
            new Relation(moveSecond ? second : first, positiveMotionWord));
        return FromSurface(frame, new Relation(new Relation(frame.Surface, S(first, second)),
            S(selectedAxis, response, calibration, output, measured, polarity, law, new Placeholder())));
    }

    public static GeometricEndpointClaim FromSurface(Curve.Frame frame, Relation surface)
    {
        var claim = new GeometricEndpointClaim(frame, surface);
        if (!claim.TryContract(out _))
            throw new ArgumentException("Expected an exact endpoint-claim surface in this frame.", nameof(surface));
        return claim;
    }

    public Observation Observe(Relation firstEndpointPort, Relation secondEndpointPort,
        bool available = true, bool ambiguous = false, IElement? eventIdentity = null,
        bool compiled = false)
    {
        if (!TryContract(out var c)) throw new InvalidOperationException("The connected endpoint claim no longer reads.");
        var occurrence = eventIdentity ?? new Placeholder();
        if (occurrence is not Placeholder)
            throw new ArgumentException("A transmitted observation identity must remain opaque.", nameof(eventIdentity));
        var bindings = S(new Relation(c.FirstRole, firstEndpointPort), new Relation(c.SecondRole, secondEndpointPort));
        Relation measurement = StructuralConstraint.Open(bindings, c.Calibration);
        var transverse = StructuralConstraint.Open();
        long? alongValue = null; long? transverseValue = null;
        bool? axisAgreement = null; bool? coincident = null;
        var left = StructuralConstraint.Open(); var right = StructuralConstraint.Open();
        FiniteLaw.TryRead(c.Law, out var comparisons, out var polarities, out _, out _);
        var geometric = Position(firstEndpointPort, out var first, out var firstWord) &&
            Position(secondEndpointPort, out var second, out var secondWord) && Axis(c.Axis, out _, out _);
        var complete = false;
        if (geometric)
        {
            Position(firstEndpointPort, out first, out firstWord);
            Position(secondEndpointPort, out second, out secondWord);
            Axis(c.Axis, out var selectedAxis, out var axisVector);
            var difference = second - first;
            var along = Curve.Vector.Dot(difference, axisVector);
            var across = Curve.Vector.Cross(axisVector, difference);
            if (along < long.MinValue || along > long.MaxValue || across < long.MinValue || across > long.MaxValue)
                throw new OverflowException("The exact endpoint displacement exceeds the claim's stored coordinate range.");
            alongValue = (long)along; transverseValue = (long)across;
            axisAgreement = along.IsZero; coincident = difference.Zero;
            var reduction = Difference(firstWord, secondWord);
            if (!frame.Motion.TryReadResult(reduction, out var remaining, out _, out var unresolved) ||
                !Closed(remaining, out var moves) || !Closed(unresolved, out var pending) || pending.Length != 0)
                throw new InvalidOperationException("Readable shared-reach endpoint words did not reduce completely.");
            var alongMoves = new List<IElement>(); var acrossMoves = new List<IElement>();
            foreach (Relation move in moves)
            {
                frame.Motion.Direction(move, out var axis, out _, out _);
                (ReferenceEquals(axis, selectedAxis) ? alongMoves : acrossMoves).Add(move);
            }
            var projection = S(alongMoves.ToArray()); transverse = S(acrossMoves.ToArray());
            var projected = new Relation(c.Axis, new Relation(projection, transverse));
            var samples = S(new Relation(c.Axis, new CarrierLocation(c.MeasuredBasis, alongValue.Value)),
                new Relation(transverse, new CarrierLocation(c.MeasuredBasis, transverseValue.Value)));
            Relation effectRead = StructuralConstraint.Open(c.Motion, c.Calibration);
            if (c.Calibrated && frame.Vector(c.Motion, out var motion) &&
                !Curve.Vector.Dot(motion, axisVector).IsZero)
            {
                complete = true;
                var effect = Curve.Vector.Dot(motion, axisVector).Sign * (Same(c.MovedRole, c.SecondRole) ? 1 : -1);
                var polarity = E(polarities).Cast<CarrierLocation>().Single(at => at.Ticks == effect);
                left = !available ? StructuralConstraint.Open() : ambiguous ? comparisons : S(E(comparisons)[1 - along.Sign]);
                right = S(polarity);
                effectRead = new Relation(new Relation(c.MovedRole, c.Motion), polarity);
            }
            measurement = S(new Relation(firstEndpointPort, secondEndpointPort), reduction,
                projected, new Relation(c.Calibration, samples), effectRead);
        }
        var lawRead = compiled
            ? FiniteLawPlan.Capture(c.Law)!.Evaluate(left, right)
            : FiniteLaw.Evaluate(c.Law, left, right);
        byte mask = 0; var support = StructuralConstraint.Open();
        if (FiniteLaw.TryReadResult(lawRead, out var result, out var residue, out _) &&
            Closed(result, out var outcomes) && Closed(residue, out var rest) && rest.Length == 0)
        {
            var domain = E(c.OutputDomain);
            foreach (var outcome in outcomes)
            {
                var index = Array.FindIndex(domain, item => Same(item, outcome));
                if (index >= 0) mask |= (byte)(1 << index);
            }
            support = result;
        }
        var packet = QualitativePublication.Create(c.Sensor, c.Owner, occurrence,
            c.OutputBasis, c.OutputDomain, support, c.Identity);
        var witness = new Relation(new Relation(Surface, occurrence), S(bindings, measurement, lawRead, packet));
        return new(packet, mask, alongValue, transverseValue, axisAgreement, coincident,
            geometric, complete, transverse, witness, lawRead);
    }

    private bool Position(Relation port, out Curve.Vector at, out Relation word)
    {
        at = default; word = null!;
        if (port.Right is not Relation position || !frame.Vector(position, out at)) return false;
        word = position; return true;
    }

    private bool Axis(Relation direction, out Relation axis, out Curve.Vector vector)
    {
        vector = default;
        if (!frame.Motion.Direction(QualitativeMotionFrame.Move(direction, frame.Reach), out axis, out var forward, out _)) return false;
        vector = ReferenceEquals(axis, frame.Motion.Horizontal)
            ? new Curve.Vector(forward ? 1 : -1, 0) : new Curve.Vector(0, forward ? 1 : -1);
        return true;
    }

    private Relation Difference(Relation from, Relation to)
    {
        var inverse = E(from).Cast<Relation>().Select(move =>
        {
            frame.Motion.Direction(move, out var axis, out var forward, out var reach);
            return (IElement)QualitativeMotionFrame.Move(ReferenceEquals(axis, frame.Motion.Horizontal)
                ? forward ? frame.Motion.Left : frame.Motion.Right
                : forward ? frame.Motion.Down : frame.Motion.Up, reach);
        }).ToArray();
        return frame.Motion.Reduce(S(inverse), to);
    }

    private bool TryContract(out Contract c)
    {
        c = null!;
        if (Surface is not { Left: Relation { Left: var basis, Right: Relation roles }, Right: Relation body } ||
            !ReferenceEquals(basis, frame.Surface) || !Closed(roles, out var roleItems) ||
            roleItems is not [Placeholder first, Placeholder second] || Same(first, second) ||
            !Closed(body, out var fields) || fields is not [Relation axis,
                Relation { Left: Relation extentPort, Right: Relation { Left: var moved, Right: Relation motion } },
                Relation calibration, Relation output, Carrier measured, Carrier polarity, Relation law, Placeholder identity] ||
            !Port(extentPort, out var extent) || !Same(moved, first) && !Same(moved, second) ||
            !Closed(output, out var contract) || contract is not [Placeholder sensor, Placeholder owner, Carrier outputBasis, Relation domain] ||
            !Coordinates(domain, outputBasis, [-1, 0, 1]) ||
            !FiniteLaw.TryRead(law, out var left, out var right, out var outputDomain, out _) ||
            !Coordinates(left, measured, [-1, 0, 1]) || !Coordinates(right, polarity, [-1, 1]) ||
            !ReferenceEquals(outputDomain, domain)) return false;
        var calibrated = Closed(calibration, out var maps) && maps is [Relation measure, Relation actuator] &&
            UnitSpan(measure.Left, measured) && OneReach(measure.Right) &&
            UnitSpan(actuator.Left, extent) && ReferenceEquals(actuator.Right, motion);
        if (!calibrated && (!StructuralConstraint.TryReadScope(calibration, out _, out var seam, out var tail) || seam.Equals(tail))) return false;
        c = new(first, second, axis, extentPort, moved, motion, calibration, sensor, owner,
            outputBasis, domain, measured, polarity, law, identity, calibrated);
        return true;
    }

    private bool OneReach(IElement scope) => scope is Relation r && Closed(r, out var entries) &&
        entries.Length == 1 && Same(entries[0], frame.Reach);
    private static bool UnitSpan(IElement span, Carrier carrier) => span is Relation
        { Left: Relation { Left: CarrierLocation a, Right: Carrier ca }, Right: Relation { Left: CarrierLocation b, Right: Carrier cb } } &&
        ReferenceEquals(ca, carrier) && ReferenceEquals(cb, carrier) && ReferenceEquals(a.Carrier, carrier) &&
        ReferenceEquals(b.Carrier, carrier) && a.Ticks == 0 && b.Ticks == 1;
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) ||
        a is Placeholder x && b is Placeholder y && x.Equals(y) ||
        a is CarrierLocation ca && b is CarrierLocation cb && ReferenceEquals(ca.Carrier, cb.Carrier) && ca.SameTickRefAs(cb);
    private static bool Coordinates(Relation domain, Carrier carrier, long[] coordinates) =>
        Closed(domain, out var entries) && entries.Length == coordinates.Length &&
        entries.Select((item, i) => item is CarrierLocation at && ReferenceEquals(at.Carrier, carrier) && at.Ticks == coordinates[i]).All(x => x);
    private static bool Port(Relation port, out Carrier carrier)
    {
        carrier = null!;
        if (port is not { Left: CarrierLocation location, Right: Carrier owner } || owner.IsUnknown ||
            !ReferenceEquals(location.Carrier, owner) || !location.SameTickRefAs(owner.ValueSlot)) return false;
        carrier = owner; return true;
    }
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope, out IElement[] entries) =>
        StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
}
