using Gojo.Core;

namespace Gojo.Structures;

/// <summary>
/// Local inspection of a bounded execution witness. Source, scope, pass basis,
/// last-pass active relations, continuation and admitted input remain ordinary
/// connected structure. There is no stored status or authoritative cached value.
/// </summary>
public static class SettlementObservation
{
    internal static Relation Create(IElement selected, IElement scope, Relation budget,
        CarrierLocation used, IElement activity, IElement continuation, IElement admitted) =>
        ReadWitness.Create(new Carrier(), selected, new Relation(scope, budget),
            new Relation(used, activity), continuation, admitted);

    /// <summary>Reads the pass basis and last observed activity from this witness.</summary>
    public static bool TryRead(Relation witness, out Relation budget,
        out CarrierLocation used, out IElement activity, out IElement continuation)
    {
        if (ReadWitness.TryRead(witness, out _, out var surface, out var result,
                out continuation, out _) && surface is Relation { Right: Relation span } &&
            TimeStepSequence.TryReadSpan(span, out var start, out var end) &&
            result is Relation { Left: CarrierLocation count } execution &&
            count.Carrier.SameIdentityAs(start.Carrier) && start.Ticks == 0 &&
            count.Ticks >= 0 && count.Ticks <= end.Ticks)
        {
            budget = span;
            used = count;
            activity = execution.Right;
            return true;
        }
        budget = null!;
        used = default;
        activity = continuation = null!;
        return false;
    }

    /// <summary>
    /// True only when an executed pass observed no active relation or route.
    /// Retained unresolved pressure can coexist with this local quietness.
    /// This is an observation of that execution, not a promise about later input.
    /// </summary>
    public static bool ObservedQuiet(Relation witness) =>
        TryRead(witness, out _, out var used, out var activity, out var continuation) &&
        used.Ticks > 0 && IsEmptyGroup(activity) && IsEmptyGroup(continuation);

    private static bool IsEmptyGroup(IElement element) =>
        element is Relation { Right: Carrier holder } group &&
        group.TryReadOrderedElementsOn(holder, out var items) && items.Length == 0;
}
