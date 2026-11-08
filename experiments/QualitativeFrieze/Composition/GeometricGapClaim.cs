using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>
/// A selected gap claim between two advertised straight sections. It reads
/// native control words in one shared cardinal frame, then publishes only a
/// coarse correction. It is not general closest-distance geometry, collision
/// avoidance, or a claim that the supplied response direction was discovered.
/// </summary>
public sealed class GeometricGapClaim
{
    // surface = (frame -> Closed(first role, second role)) -> Closed(
    //   normal, extent port -> (moved role -> positive motion word),
    //   desired-gap port, Closed(desired tick -> reach, actuator tick -> reach),
    //   Closed(sensor, receiver, comparison basis, comparison domain),
    //   measured basis, polarity basis, finite response law, opaque identity)
    // observation binds actual sections to the two roles; the private witness
    // retains their curve reads, shared-coordinate displacement, calibrated gap,
    // desired sample and law read. The transmitted packet has no edge to these.
    // Increasing the extent moves the selected role by the positive-motion word.
    // This response correspondence is supplied, not inferred from a picture.

    private readonly Curve.Frame frame;
    public Relation Surface { get; }

    public sealed record Observation(Relation Packet, byte Mask, long? Gap,
        long? DesiredGap, bool Complete, Relation Witness, Relation LawRead);

    private sealed record Contract(IElement FirstRole, IElement SecondRole,
        Relation Normal, Relation ExtentPort, IElement MovedRole, Relation Motion,
        Relation DesiredPort, Relation Calibration, IElement Sensor, IElement Owner,
        Carrier OutputBasis, Relation OutputDomain, Carrier MeasuredBasis,
        Carrier PolarityBasis, Relation Law, IElement Identity, bool Calibrated);

    private GeometricGapClaim(Curve.Frame frame, Relation surface)
    { this.frame = frame; Surface = surface; }

    public static GeometricGapClaim Create(Curve.Frame frame, Relation desiredGapPort,
        CoupledExtentMachine receiver, Relation normalDirection, Relation positiveMotionWord,
        bool moveSecond = true) => CreateCore(frame, desiredGapPort, receiver, normalDirection, positiveMotionWord, moveSecond, false);

    /// <summary>Explicit calibration: one desired tick reads as one frame reach;
    /// one actuator tick moves the selected role by the full connected word.
    /// These independent correspondences allow different stable units. They are
    /// supplied premises, not a conversion inferred from similar scalar values.</summary>
    public static GeometricGapClaim CreateCalibrated(Curve.Frame frame, Relation desiredGapPort,
        CoupledExtentMachine receiver, Relation normalDirection, Relation positiveMotionWord,
        bool moveSecond = true) => CreateCore(frame, desiredGapPort, receiver, normalDirection, positiveMotionWord, moveSecond, true);

    private static GeometricGapClaim CreateCore(Curve.Frame frame, Relation desiredGapPort,
        CoupledExtentMachine receiver, Relation normalDirection, Relation positiveMotionWord,
        bool moveSecond, bool declaredMotion)
    {
        if (!Port(desiredGapPort, out var desired, out _) ||
            !Port(receiver.ExtentPort, out var extent, out _))
            throw new ArgumentException("The claim needs actual advertised desired-gap and actuator value ports.");
        var first = new Placeholder(); var second = new Placeholder();
        var measured = new Carrier(desired.UnitShape); var polarity = new Carrier();
        var comparisons = new long[] { -1, 0, 1 }.Select(n => (IElement)new CarrierLocation(measured, n)).ToArray();
        var polarities = new IElement[] { new CarrierLocation(polarity, -1), new CarrierLocation(polarity, 1) };
        var outputs = E(receiver.ComparisonDomain);
        var rows = new List<Relation>();
        for (var i = 0; i < comparisons.Length; i++)
            foreach (CarrierLocation face in polarities)
                rows.Add(FiniteLaw.Row(comparisons[i], face,
                    S(outputs[checked((int)((i - 1) * face.Ticks + 1))])));
        var law = FiniteLaw.Create(S(comparisons), S(polarities), receiver.ComparisonDomain, rows.ToArray());
        var calibration = declaredMotion
            ? S(new Relation(Relation.Segment(desired, 0, 1), S(frame.Reach)),
                new Relation(Relation.Segment(extent, 0, 1), positiveMotionWord))
            : desired.UnitShape.Equals(extent.UnitShape)
            ? S(new Relation(Relation.Segment(desired, 0, 1), S(frame.Reach)),
                new Relation(Relation.Segment(extent, 0, 1), S(frame.Reach)))
            : StructuralConstraint.Open(new Relation(desired.UnitShape.Surface, extent.UnitShape.Surface));
        var output = S(receiver.Sensor, receiver.Owner, receiver.ComparisonBasis, receiver.ComparisonDomain);
        var response = new Relation(receiver.ExtentPort,
            new Relation(moveSecond ? second : first, positiveMotionWord));
        var surface = new Relation(new Relation(frame.Surface, S(first, second)),
            S(normalDirection, response, desiredGapPort, calibration, output, measured, polarity, law, new Placeholder()));
        return FromSurface(frame, surface);
    }

    /// <summary>Reuses an exact claim with the reader for its own native frame.
    /// A fresh unrelated frame is not a replacement for restored correspondence.</summary>
    public static GeometricGapClaim FromSurface(Curve.Frame frame, Relation surface)
    {
        var claim = new GeometricGapClaim(frame, surface);
        if (!claim.TryContract(out _)) throw new ArgumentException("Expected an exact gap-claim surface in this frame.", nameof(surface));
        return claim;
    }

    public Observation Observe(Relation firstSection, Relation secondSection,
        bool available = true, bool ambiguous = false, IElement? eventIdentity = null,
        bool compiled = false)
    {
        if (!TryContract(out var c)) throw new InvalidOperationException("The connected gap claim no longer reads.");
        var occurrence = eventIdentity ?? new Placeholder();
        if (occurrence is not Placeholder)
            throw new ArgumentException("A transmitted observation identity must remain opaque.", nameof(eventIdentity));
        var first = frame.Read(firstSection); var second = frame.Read(secondSection);
        var bindings = S(new Relation(c.FirstRole, firstSection), new Relation(c.SecondRole, secondSection));
        long? gap = null;
        var desired = Port(c.DesiredPort, out _, out var target) ? (long?)target : null;
        var complete = c.Calibrated && desired.HasValue &&
            Measure(first, second, c.Normal, out var measuredGap, out var normal) &&
            frame.Vector(c.Motion, out var motion) &&
            (Curve.Vector.Cross(motion, normal).IsZero || HasDeclaredMotion(c)) &&
            !Curve.Vector.Dot(motion, normal).IsZero;
        Relation measurement = StructuralConstraint.Open(bindings, c.Calibration);
        var left = StructuralConstraint.Open(); var right = StructuralConstraint.Open();
        FiniteLaw.TryRead(c.Law, out var comparisonDomain, out var polarityDomain, out _, out _);
        if (complete)
        {
            // Re-read after the predicate to retain definite assigned values.
            Measure(first, second, c.Normal, out measuredGap, out normal);
            frame.Vector(c.Motion, out motion);
            gap = measuredGap;
            var sign = target.CompareTo(measuredGap);
            var effect = Curve.Vector.Dot(motion, normal).Sign *
                (ReferenceEquals(c.MovedRole, c.SecondRole) ? 1 : -1);
            var polarityState = E(polarityDomain).Cast<CarrierLocation>().Single(at => at.Ticks == effect);
            left = !available ? StructuralConstraint.Open() : ambiguous ? comparisonDomain : S(E(comparisonDomain)[sign + 1]);
            right = S(polarityState);
            var from = (Relation)E(first.ControlWords)[0]; var to = (Relation)E(second.ControlWords)[0];
            var displacement = Difference(from, to);
            measurement = S(first.Surface, second.Surface, displacement,
                new Relation(c.Calibration, new CarrierLocation(c.MeasuredBasis, measuredGap)),
                new Relation(c.DesiredPort, new CarrierLocation(c.MeasuredBasis, target)),
                new Relation(new Relation(c.MovedRole, c.Motion), polarityState));
        }
        var lawRead = compiled
            ? FiniteLawPlan.Capture(c.Law)!.Evaluate(left, right)
            : FiniteLaw.Evaluate(c.Law, left, right);
        var mask = (byte)0;
        var outputSupport = StructuralConstraint.Open();
        if (FiniteLaw.TryReadResult(lawRead, out var result, out var residue, out _) &&
            Closed(result, out var outcomes) && Closed(residue, out var pending) && pending.Length == 0)
        {
            var domain = E(c.OutputDomain);
            foreach (var outcome in outcomes)
            {
                var index = Array.FindIndex(domain, item => Same(item, outcome));
                if (index >= 0) mask |= (byte)(1 << index);
            }
            outputSupport = result;
        }
        var packet = QualitativePublication.Create(c.Sensor, c.Owner, occurrence,
            c.OutputBasis, c.OutputDomain, outputSupport, c.Identity);
        var witness = new Relation(new Relation(Surface, occurrence), S(bindings, measurement, lawRead, packet));
        return new(packet, mask, gap, desired, complete, witness, lawRead);
    }

    private bool Measure(Curve.Read a, Curve.Read b, Relation selectedNormal,
        out long gap, out Curve.Vector normal)
    {
        gap = 0; normal = default;
        if (!frame.Motion.Direction(QualitativeMotionFrame.Move(selectedNormal, frame.Reach), out var axis, out var forward, out _) ||
            !a.Complete || !b.Complete || a.Controls.Length != 2 || b.Controls.Length != 2) return false;
        normal = ReferenceEquals(axis, frame.Motion.Horizontal)
            ? new Curve.Vector(forward ? 1 : -1, 0) : new Curve.Vector(0, forward ? 1 : -1);
        var da = a.Controls[1] - a.Controls[0]; var db = b.Controls[1] - b.Controls[0];
        if (da.Zero || db.Zero || !Curve.Vector.Dot(da, normal).IsZero ||
            !Curve.Vector.Dot(db, normal).IsZero) return false;
        // Cardinal normal + perpendicular straight sections makes the gap
        // constant on their shared positive-length longitudinal interval.
        var horizontal = normal.X.IsZero;
        BigInteger Along(Curve.Vector at) => horizontal ? at.X : at.Y;
        var a0 = Along(a.Controls[0]); var a1 = Along(a.Controls[1]);
        var b0 = Along(b.Controls[0]); var b1 = Along(b.Controls[1]);
        if (BigInteger.Min(BigInteger.Max(a0, a1), BigInteger.Max(b0, b1)) <=
            BigInteger.Max(BigInteger.Min(a0, a1), BigInteger.Min(b0, b1))) return false;
        var signedGap = Curve.Vector.Dot(b.Controls[0] - a.Controls[0], normal);
        if (signedGap < 0 || signedGap > long.MaxValue) return false;
        gap = (long)signedGap; return true;
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
            roleItems is not [Placeholder first, Placeholder second] || first.Equals(second) ||
            !Closed(body, out var fields) || fields is not [Relation normal,
                Relation { Left: Relation extentPort, Right: Relation { Left: var moved, Right: Relation motion } },
                Relation desiredPort, Relation calibration, Relation output, Carrier measured,
                Carrier polarity, Relation law, Placeholder identity] ||
            !Port(extentPort, out var extent, out _) || !Port(desiredPort, out var desired, out _) ||
            !ReferenceEquals(moved, first) && !ReferenceEquals(moved, second) ||
            !Closed(output, out var contract) || contract is not [Placeholder sensor, Placeholder owner, Carrier outputBasis, Relation domain] ||
            !Coordinates(domain, outputBasis, [-1, 0, 1]) ||
            !FiniteLaw.TryRead(law, out var left, out var right, out var outputDomain, out _) ||
            !Coordinates(left, measured, [-1, 0, 1]) || !Coordinates(right, polarity, [-1, 1]) ||
            !ReferenceEquals(outputDomain, domain) || !measured.UnitShape.Equals(desired.UnitShape)) return false;
        var calibrated = Closed(calibration, out var maps) &&
            maps is [Relation d, Relation a] && UnitSpan(d.Left, desired) &&
            UnitSpan(a.Left, extent) && OneReach(d.Right) &&
            (ReferenceEquals(a.Right, motion) || desired.UnitShape.Equals(extent.UnitShape) && OneReach(a.Right));
        if (!calibrated && (!StructuralConstraint.TryReadScope(calibration, out _, out var seam, out var tail) || seam.Equals(tail))) return false;
        c = new(first, second, normal, extentPort, moved, motion, desiredPort, calibration,
            sensor, owner, outputBasis, domain, measured, polarity, law, identity, calibrated);
        return true;
    }

    private bool OneReach(IElement scope) => scope is Relation r && Closed(r, out var entries) &&
        entries.Length == 1 && ReferenceEquals(entries[0], frame.Reach);
    // A full motion correspondence permits a selected-normal read even when
    // movement also has a transverse component. The whole word stays in the
    // witness; future loss of overlap is not ruled out by this local correction.
    private static bool HasDeclaredMotion(Contract c) => Closed(c.Calibration, out var maps) &&
        maps is [_, Relation actuator] && ReferenceEquals(actuator.Right, c.Motion);
    private static bool UnitSpan(IElement span, Carrier carrier) => span is Relation
        { Left: Relation { Left: CarrierLocation a, Right: Carrier ca }, Right: Relation { Left: CarrierLocation b, Right: Carrier cb } } &&
        ReferenceEquals(ca, carrier) && ReferenceEquals(cb, carrier) && ReferenceEquals(a.Carrier, carrier) &&
        ReferenceEquals(b.Carrier, carrier) && a.Ticks == 0 && b.Ticks == 1;
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) ||
        a is CarrierLocation x && b is CarrierLocation y && x.Equals(y);
    private static bool Coordinates(Relation domain, Carrier carrier, long[] coordinates) =>
        Closed(domain, out var entries) && entries.Length == coordinates.Length &&
        entries.Select((item, i) => item is CarrierLocation at && ReferenceEquals(at.Carrier, carrier) && at.Ticks == coordinates[i]).All(x => x);
    private static bool Port(Relation port, out Carrier carrier, out long value)
    {
        carrier = null!; value = 0;
        if (port is not { Left: CarrierLocation location, Right: Carrier owner } || owner.IsUnknown ||
            !ReferenceEquals(location.Carrier, owner) || !location.SameTickRefAs(owner.ValueSlot)) return false;
        carrier = owner; value = location.Ticks; return true;
    }
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope, out IElement[] entries) =>
        StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
}
