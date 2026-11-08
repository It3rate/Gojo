using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>
/// An explicit preference for section-gap evidence, with projected landmark
/// separation as a retained alternative. The alternative does not establish a
/// nonzero overlapping section, uniform clearance, closure or material joining.
/// A hypothetical target is constructed observationally, never moved into place.
/// </summary>
public sealed class GeometricGapAlternative
{
    // surface = frame -> Closed(primary section-gap claim,
    //   (desired port -> (normal -> desired-tick/reach calibration)) ->
    //     (hypothetical target role -> local construction-capacity span),
    //   endpoint claim, first-preference response law)
    // target application = (target construction -> first landmark) -> Closed(
    //   captured desired sample, repeated normal reaches, addition witness,
    //   hypothetical target port, or open remainder)
    // The selector chooses a response, not the truth of the primary claim.

    private readonly Curve.Frame frame;
    private readonly GeometricGapClaim primary;
    private readonly GeometricEndpointClaim alternative;
    private readonly ExtentResponseArbiter selector;
    private readonly Relation targetConstruction;
    private readonly Relation desiredPort;
    private readonly Relation normal;
    private readonly Relation calibration;
    private readonly Relation capacity;
    private readonly IElement targetRole;
    private readonly bool calibrated;

    public Relation Surface { get; }

    public sealed record Observation(Relation Packet, byte Mask, long? PrimaryGap,
        long? AlternativeGap, long? DesiredGap, long? SelectedGap,
        bool PrimaryComplete, bool AlternativeComplete, bool UsedAlternative,
        byte PrimaryMask, byte AlternativeMask, Relation PrimaryPacket,
        Relation AlternativePacket, Relation Witness, Relation Selection);

    private GeometricGapAlternative(Curve.Frame frame, Relation surface)
    {
        if (!ReferenceEquals(surface.Left, frame.Surface) || surface.Right is not Relation body ||
            !Closed(body, out var entries) || entries is not [Relation p, Relation target, Relation a, Relation s] ||
            target is not { Left: Relation { Left: Relation desired, Right: Relation { Left: Relation direction, Right: Relation map } },
                Right: Relation { Left: Placeholder role, Right: Relation bound } } ||
            !Port(desired, out var desiredCarrier, out _) || !Capacity(bound, out _) )
            throw new ArgumentException("Expected an exact section/landmark alternative surface.", nameof(surface));
        primary = GeometricGapClaim.FromSurface(frame, p);
        alternative = GeometricEndpointClaim.FromSurface(frame, a);
        selector = ExtentResponseArbiter.FromSurface(s);
        var pp = E((Relation)p.Right); var ap = E((Relation)a.Right);
        var pr = (Relation)pp[1]; var ar = (Relation)ap[1];
        var pc = E((Relation)pp[4]); var ac = E((Relation)ap[3]); var sc = E(s);
        if (!ReferenceEquals(pp[2], desired) || !ReferenceEquals(pp[0], direction) || !ReferenceEquals(ap[0], direction) ||
            !ReferenceEquals(pr.Left, ar.Left) || !ReferenceEquals(((Relation)pr.Right).Right, ((Relation)ar.Right).Right) ||
            MovedSecond(p, pr) != MovedSecond(a, ar) ||
            !pc.Zip(ac).All(pair => Same(pair.First, pair.Second)) ||
            !Same(pc[0], sc[1]) || !Same(pc[1], sc[2]) || !Same(pc[2], sc[3]) || !Same(pc[3], sc[4]) ||
            !Port((Relation)pr.Left, out var extent, out _))
            throw new ArgumentException("The alternatives must share goal, normal, response and receiver contracts.", nameof(surface));
        if (!FirstPreference(selector.Law))
            throw new ArgumentException("This reader requires the retained first-preference law.", nameof(surface));
        calibrated = map is { Left: var unit, Right: Relation reach } && UnitSpan(unit, desiredCarrier) &&
            Closed(reach, out var participants) && participants.Length == 1 && Same(participants[0], frame.Reach);
        if (!calibrated && (!StructuralConstraint.TryReadScope(map, out _, out var seam, out var tail) || seam.Equals(tail)))
            throw new ArgumentException("An unavailable target calibration must remain open.", nameof(surface));
        this.frame = frame; Surface = surface; targetConstruction = target;
        desiredPort = desired; normal = direction; calibration = map; capacity = bound; targetRole = role;
    }

    public static GeometricGapAlternative Create(Curve.Frame frame, Relation desiredGapPort,
        CoupledExtentMachine receiver, Relation normalDirection, Relation positiveMotionWord,
        bool moveSecond = true, int maxTargetReaches = 65536) =>
        Build(frame, desiredGapPort, receiver, normalDirection, positiveMotionWord, moveSecond, maxTargetReaches, false);

    /// <summary>Explicitly calibrates a desired tick to one geometric reach and
    /// the actuator tick to the supplied motion word; their units may differ.</summary>
    public static GeometricGapAlternative CreateCalibrated(Curve.Frame frame, Relation desiredGapPort,
        CoupledExtentMachine receiver, Relation normalDirection, Relation positiveMotionWord,
        bool moveSecond = true, int maxTargetReaches = 65536) =>
        Build(frame, desiredGapPort, receiver, normalDirection, positiveMotionWord, moveSecond, maxTargetReaches, true);

    private static GeometricGapAlternative Build(Curve.Frame frame, Relation desiredGapPort,
        CoupledExtentMachine receiver, Relation normalDirection, Relation positiveMotionWord,
        bool moveSecond, int maxTargetReaches, bool explicitCalibration)
    {
        if (maxTargetReaches is < 0 or > 65536) throw new ArgumentOutOfRangeException(nameof(maxTargetReaches));
        if (!Port(desiredGapPort, out var desired, out _))
            throw new ArgumentException("The desired gap needs its advertised value port.", nameof(desiredGapPort));
        var p = explicitCalibration
            ? GeometricGapClaim.CreateCalibrated(frame, desiredGapPort, receiver, normalDirection, positiveMotionWord, moveSecond)
            : GeometricGapClaim.Create(frame, desiredGapPort, receiver, normalDirection, positiveMotionWord, moveSecond);
        var a = GeometricEndpointClaim.Create(frame, receiver, normalDirection, positiveMotionWord, moveSecond);
        var map = explicitCalibration || desired.UnitShape.Equals(receiver.StepUnit.UnitShape)
            ? new Relation(Relation.Segment(desired, 0, 1), S(frame.Reach))
            : StructuralConstraint.Open(new Relation(desired.UnitShape.Surface, receiver.StepUnit.UnitShape.Surface));
        var target = new Relation(new Relation(desiredGapPort, new Relation(normalDirection, map)),
            new Relation(new Placeholder(), Relation.Segment(new Carrier(), 0, maxTargetReaches)));
        return FromSurface(frame, new Relation(frame.Surface, S(p.Surface, target, a.Surface,
            ExtentResponseArbiter.Create(receiver, "first").Surface)));
    }

    public static GeometricGapAlternative FromSurface(Curve.Frame frame, Relation surface) => new(frame, surface);

    public Observation Observe(Relation firstSection, Relation secondSection,
        Relation firstLandmarkPort, Relation secondLandmarkPort, bool available = true,
        bool ambiguous = false, IElement? eventIdentity = null, bool compiled = false)
    {
        var section = primary.Observe(firstSection, secondSection, available, ambiguous, compiled: compiled);
        var target = Target(firstLandmarkPort);
        var landmark = alternative.Observe(target.Port, secondLandmarkPort, available, ambiguous, compiled: compiled);
        var chosen = selector.Evaluate(section.Packet, landmark.Packet, compiled, eventIdentity);
        var alternativeGap = target.Desired.HasValue && landmark.AlongDifference.HasValue
            ? checked(target.Desired.Value + landmark.AlongDifference.Value) : (long?)null;
        var usedAlternative = !chosen.FirstEligible && chosen.SecondEligible && chosen.Resolved;
        var selectedGap = chosen.FirstEligible && chosen.Resolved ? section.Gap : usedAlternative ? alternativeGap : null;
        var witness = new Relation(Surface, S(section.Witness, target.Witness, landmark.Witness, chosen.Witness));
        return new(chosen.Packet, chosen.Mask, section.Gap, alternativeGap, target.Desired,
            selectedGap, section.Complete, landmark.Complete, usedAlternative, section.Mask,
            landmark.Mask, section.Packet, landmark.Packet, witness, chosen.Witness);
    }

    private (Relation Port, long? Desired, Relation Witness) Target(Relation first)
    {
        var site = new Placeholder();
        var declared = Port(desiredPort, out var desiredCarrier, out var desired) ? (long?)desired : null;
        var sample = declared.HasValue ? (IElement)new CarrierLocation(desiredCarrier, declared.Value) : StructuralConstraint.Open(desiredPort);
        var binding = new Relation(targetRole, new Relation(first, site));
        Relation word = StructuralConstraint.Open(binding, calibration);
        Relation expansion = StructuralConstraint.Open(targetConstruction, sample);
        if (calibrated && declared.HasValue && Capacity(capacity, out var limit) &&
            desired >= 0 && desired <= limit && first.Right is Relation position && frame.Vector(position, out _) &&
            frame.Motion.Direction(QualitativeMotionFrame.Move(normal, frame.Reach), out _, out _, out _))
        {
            var moves = S(Enumerable.Range(0, checked((int)desired))
                .Select(_ => (IElement)QualitativeMotionFrame.Move(normal, frame.Reach)).ToArray());
            var sum = frame.Motion.Reduce(position, moves);
            if (frame.Motion.TryReadResult(sum, out var reduced, out _, out var unresolved) &&
                Closed(unresolved, out var pending) && pending.Length == 0)
                word = reduced;
            expansion = S(new Relation(desiredPort, sample), new Relation(calibration, moves),
                new Relation(capacity, moves), sum);
        }
        var port = new Relation(site, word);
        var witness = new Relation(new Relation(targetConstruction, first), S(binding, expansion, port));
        return (port, declared, witness);
    }

    private static bool MovedSecond(Relation claim, Relation response) =>
        Same(((Relation)response.Right).Left, E((Relation)((Relation)claim.Left).Right)[1]);
    private static bool FirstPreference(Relation law)
    {
        if (!FiniteLaw.TryRead(law, out var left, out var right, out var output, out var rows) ||
            !ReferenceEquals(left, right) || rows.Length != 16) return false;
        var input = E(left); var result = E(output);
        if (input.Length != 4 || result.Length != 4) return false;
        for (var a = 0; a < 4; a++)
            for (var b = 0; b < 4; b++)
            {
                var matching = rows.Where(row => row.Left is Relation pair &&
                    Same(pair.Left, input[a]) && Same(pair.Right, input[b])).ToArray();
                if (matching.Length != 1 || matching[0].Right is not Relation support ||
                    !Closed(support, out var values) || values.Length != 1 ||
                    !Same(values[0], result[a != 3 ? a : b])) return false;
            }
        return true;
    }
    private static bool UnitSpan(IElement span, Carrier carrier) => span is Relation
        { Left: Relation { Left: CarrierLocation a, Right: Carrier ca }, Right: Relation { Left: CarrierLocation b, Right: Carrier cb } } &&
        ReferenceEquals(ca, carrier) && ReferenceEquals(cb, carrier) && ReferenceEquals(a.Carrier, carrier) &&
        ReferenceEquals(b.Carrier, carrier) && a.Ticks == 0 && b.Ticks == 1;
    private static bool Capacity(Relation span, out long count)
    {
        count = 0;
        if (span is not { Left: Relation { Left: CarrierLocation a, Right: Carrier ca },
                Right: Relation { Left: CarrierLocation b, Right: Carrier cb } } ||
            !ReferenceEquals(ca, cb) || !ReferenceEquals(a.Carrier, ca) || !ReferenceEquals(b.Carrier, ca) ||
            a.Ticks != 0 || b.Ticks is < 0 or > 65536) return false;
        count = b.Ticks; return true;
    }
    private static bool Port(Relation port, out Carrier carrier, out long value)
    {
        carrier = null!; value = 0;
        if (port is not { Left: CarrierLocation location, Right: Carrier owner } || owner.IsUnknown ||
            !ReferenceEquals(location.Carrier, owner) || !location.SameTickRefAs(owner.ValueSlot)) return false;
        carrier = owner; value = location.Ticks; return true;
    }
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) ||
        a is Placeholder x && b is Placeholder y && x.Equals(y) ||
        a is CarrierLocation u && b is CarrierLocation v && ReferenceEquals(u.Carrier, v.Carrier) && u.SameTickRefAs(v);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope, out IElement[] entries) =>
        StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
}
