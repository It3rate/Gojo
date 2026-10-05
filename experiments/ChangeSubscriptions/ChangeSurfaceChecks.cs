using Gojo.Core;
using Gojo.Equations;
using N = ConnectionAssembly.NativeMath;
using S = ChangeSubscriptions.ChangeSurface;

namespace ChangeSubscriptions;

internal static class ChangeSurfaceChecks
{
    public static int Run()
    {
        SeparateReadsAndRepeatedOpportunity();
        ValidityIsAChangeWithoutInventingZero();
        AttachDetachAndLateJoin();
        CoarseReadRetainsItsBasis();
        CapturedChangesRemainCaptured();
        NativeContinuationKeepsPhysicalConnections();
        return 6;
    }

    private static void SeparateReadsAndRepeatedOpportunity()
    {
        var input = L(new Carrier(), 1); var valid = L(new Carrier(), 1); var clock = new Carrier();
        var direction = S.Create(input, valid, S.DirectionRead(input), clock);
        var magnitude = S.Create(input, valid, S.MagnitudeRead(input), clock);
        for (var step = 1; step <= 3; step++)
        {
            input.Tick.Set(step);
            S.Advance(direction, L(clock, step)); S.Advance(magnitude, L(clock, step));
        }
        Require(S.Revision(direction).Value == 1 && S.Revision(magnitude).Value == 3 &&
            S.Current(direction).Ticks == 1 && S.Current(magnitude).Ticks == 3 && S.ValidationCount(direction).Value == 3,
            "Different native selected reads expose different changes from one shared input.");
        var repeat = S.Advance(direction, L(clock, 3));
        var backward = S.Advance(direction, L(clock, 2));
        var foreign = S.Advance(direction, L(new Carrier(), 99));
        Require(Number(repeat, 4) == 0 && Number(backward, 4) == 0 && Number(foreign, 4) == 0 &&
            S.ValidationCount(direction).Value == 3 && S.Revision(direction).Value == 1 &&
            ((CarrierLocation)S.Interval(direction).Left).Ticks == 1 && ((CarrierLocation)S.Interval(direction).Right).Ticks == 3,
            "Repeated, backward and unaligned opportunities cannot create publications or validation credit.");
    }

    private static void ValidityIsAChangeWithoutInventingZero()
    {
        var input = L(new Carrier(), -3); var valid = L(new Carrier(), 1); var clock = new Carrier();
        var surface = S.Create(input, valid, S.MagnitudeRead(input), clock);
        S.Advance(surface, L(clock, 1));
        valid.Tick.Set(0); input.Tick.Set(900);
        var lost = S.Advance(surface, L(clock, 2));
        Require(!S.TryCurrent(surface, out _, out var unresolved) && !S.Scalar(unresolved, out _) &&
            S.Current(surface).Ticks == -3 && Number((Relation)S.Parts(lost)[5], 6) == 0 &&
            Number((Relation)S.Parts(lost)[5], 7) == 1,
            "Losing validity is an explicit event; held negative data remains inspectable and cannot read as current.");
        input.Tick.Set(-700); var stillMissing = S.Advance(surface, L(clock, 3));
        Require(Number(stillMissing, 4) == 0 && S.Current(surface).Ticks == -3 && S.Revision(surface).Value == 2,
            "Changing an invalid raw coordinate does not publish a fabricated answer.");
        input.Tick.Set(-3); valid.Tick.Set(1);
        var recovered = S.Advance(surface, L(clock, 4));
        Require(S.TryCurrent(surface, out var value, out _) && value.Ticks == -3 &&
            Number((Relation)S.Parts(recovered)[5], 6) == 0 && Number((Relation)S.Parts(recovered)[5], 7) == 1,
            "Revalidation is distinguishable from a changed selected value.");
    }

    private static void AttachDetachAndLateJoin()
    {
        var input = L(new Carrier(), 2); var valid = L(new Carrier(), 1); var clock = new Carrier();
        var surface = S.Create(input, valid, S.DirectionRead(input), clock);
        var subscriber = S.Subscribe(surface);
        Require(!S.Attached(subscriber) && !S.TryReceived(subscriber, out _, out _), "Constructing a potential subscriber does not attach it.");
        S.Advance(surface, L(clock, 1));
        var joined = S.Attach(subscriber); var physical = S.Publication(subscriber);
        Require(S.Attached(subscriber) && Number(joined, 1) == 1 && Number(joined, 2) == 0 &&
            S.TryReceived(subscriber, out var first, out _) && first.Ticks == 1 && S.NotificationCount(subscriber).Value == 0,
            "A late join obtains current flow through a physical output and an explicit initial synchronization.");
        input.Tick.Set(3); S.Advance(surface, L(clock, 2)); S.Notify(subscriber);
        Require(S.NotificationCount(subscriber).Value == 0, "Raw variation omitted by the selected read creates no notification.");
        input.Tick.Set(-3); S.Advance(surface, L(clock, 3));
        var changed = S.Notify(subscriber); S.Notify(subscriber);
        Require(Number(changed, 2) == 1 && S.NotificationCount(subscriber).Value == 1 &&
            S.TryReceived(subscriber, out var reversed, out _) && reversed.Ticks == -1,
            "A changed read is physically delivered and acknowledged once, retaining signed participation.");
        S.Detach(subscriber); input.Tick.Set(3); S.Advance(surface, L(clock, 4)); S.Notify(subscriber);
        Require(!S.Attached(subscriber) && !S.TryReceived(subscriber, out _, out _) && S.NotificationCount(subscriber).Value == 1,
            "A detached subscriber neither receives notifications nor treats retained receipt state as current.");
        var resumed = S.Attach(subscriber);
        Require(ReferenceEquals(physical, S.Publication(subscriber)) && S.Attached(subscriber) &&
            Number(resumed, 1) == 1 && S.NotificationCount(subscriber).Value == 1 && S.JoinCount(subscriber).Value == 2 &&
            S.TryReceived(subscriber, out var current, out _) && current.Ticks == 1,
            "Resumption uses the same exact relation and current synchronization, without replaying missed events as new success.");
    }

    private static void CoarseReadRetainsItsBasis()
    {
        var input = L(new Carrier(), 5); var valid = L(new Carrier(), 1); var clock = new Carrier();
        var coarse = S.MagnitudeRead(input, 4); var surface = S.Create(input, valid, coarse, clock);
        var read = S.EvaluateRead(coarse);
        Require(Equation.TryReadPrediction(read, out var value, out var residue) && value.Ticks == 1 &&
            ProjectionResidue.TryRead(residue, out var remainderBasis, out var numerator, out var denominator) &&
            remainderBasis.SameIdentityAs(input.Carrier) && numerator == 1 && denominator == 1,
            "Five source ticks project to one coarse tick plus exactly one retained source tick.");
        Require(value.Carrier.UnitShape.TryReadResolution(out var fineUnit, out var sourceTicksPerTick) &&
            fineUnit.CompatibleWith(input.Carrier.UnitShape) && sourceTicksPerTick == 4 &&
            !value.Carrier.UnitShape.CompatibleWith(input.Carrier.UnitShape),
            "The output tick promises four source ticks; the explicit affine frame performs that conversion exactly once.");
        S.Advance(surface, L(clock, 1)); input.Tick.Set(7);
        var seven = S.EvaluateRead(coarse);
        Require(Equation.TryReadPrediction(seven, out var sevenValue, out var sevenResidue) && sevenValue.Ticks == 1 &&
            ProjectionResidue.TryRead(sevenResidue, out var sevenBasis, out var sevenNumerator, out var sevenDenominator) &&
            sevenBasis.SameIdentityAs(input.Carrier) && sevenNumerator == 3 && sevenDenominator == 1,
            "Seven source ticks keep the same coarse coordinate with exactly three source ticks of residue.");
        S.Advance(surface, L(clock, 2));
        Require(S.Revision(surface).Value == 1 && S.ValidationCount(surface).Value == 2 && S.Current(surface).Ticks == 1,
            "Changes inside a coarse selected cell are validations rather than publications.");
        input.Tick.Set(8); S.Advance(surface, L(clock, 3));
        Require(S.Revision(surface).Value == 2 && S.Current(surface).Ticks == 2, "Crossing that cell produces a selected-value event.");
    }

    private static void CapturedChangesRemainCaptured()
    {
        var input = L(new Carrier(), 1); var valid = L(new Carrier(), 1); var clock = new Carrier();
        var surface = S.Create(input, valid, S.MagnitudeRead(input), clock);
        var old = S.Advance(surface, L(clock, 1)); var snapshot = (Relation)S.Parts(old)[5];
        input.Tick.Set(4); S.Advance(surface, L(clock, 2));
        Require(Number(snapshot, 0) == 1 && Number(snapshot, 1) == 1 && Number(snapshot, 3) == 1 &&
            Number(S.LastEvent(surface), 0) == 2 && Number(S.LastEvent(surface), 3) == 4,
            "The finite current event can move while a caller-retained historical witness stays frozen.");
    }

    private static void NativeContinuationKeepsPhysicalConnections()
    {
        var input = L(new Carrier(), -2); var valid = L(new Carrier(), 1); var clock = new Carrier();
        var surface = S.Create(input, valid, S.DirectionRead(input), clock); var subscriber = S.Subscribe(surface);
        S.Advance(surface, L(clock, 1)); S.Attach(subscriber);
        var world = N.Group(surface, subscriber);
        var clone = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(world)).Single();
        var cloneSurface = (Relation)S.Parts(clone)[0]; var cloneSubscriber = (Relation)S.Parts(clone)[1];
        Require(S.Attached(cloneSubscriber) && ReferenceEquals(S.Surface(cloneSubscriber), cloneSurface) &&
            S.TryReceived(cloneSubscriber, out var current, out _) && current.Ticks == -1,
            "Native reload preserves aliases, exact live publications and signed current flow.");
        S.Source(cloneSurface).Tick.Set(2);
        S.Advance(cloneSurface, L(S.LastOpportunity(cloneSurface).Carrier, 2)); S.Notify(cloneSubscriber);
        Require(S.Revision(cloneSurface).Value == 2 && S.NotificationCount(cloneSubscriber).Value == 1 &&
            S.TryReceived(cloneSubscriber, out var changed, out _) && changed.Ticks == 1 && S.Current(surface).Ticks == -1,
            "A restored world can continue independently through the existing physical read and revision routes.");
    }

    private static long Number(Relation row, int index) => ((CarrierLocation)S.Parts(row)[index]).Ticks;
    private static CarrierLocation L(Carrier basis, long ticks) => new(basis, ticks);
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
