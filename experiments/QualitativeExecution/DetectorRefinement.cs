using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// A finer categorical observation may narrow alternatives for the same event
/// and detector contract. It cannot retrieve a discarded signal or substitute
/// a later event. Admission evidence and failed claims stay connected.
/// </summary>
public static class DetectorRefinement
{
    // witness = (earlier -> offered) -> (accepted -> (blocked -> evidence)).
    public static bool TryAccept(ThresholdView view, Relation earlier, Relation offered,
        out Relation accepted, out Relation witness)
    {
        accepted = earlier;
        var checks = new List<IElement>();
        var compatible = view.TryReadObservation(earlier, out var source, out var eventIdentity,
                out var oldSides, out var oldTrends) &&
            view.TryReadObservation(offered, out var offeredSource, out var offeredEvent,
                out _, out _) &&
            FiniteLaw.SameParticipant(source, offeredSource) &&
            FiniteLaw.SameParticipant(eventIdentity, offeredEvent);
        // Read again only after contract checks, so unassigned outputs never
        // participate in membership claims.
        if (compatible)
        {
            view.TryReadObservation(offered, out _, out _, out var newSides, out var newTrends);
            compatible = Narrower(oldSides!, newSides, view.SideDomain, checks) &
                Narrower(oldTrends!, newTrends, view.TrendDomain, checks);
        }
        if (compatible)
        {
            DetectorObservation.TryRead(offered, out var s, out var e, out _, out var sides, out var trends, out _);
            accepted = view.Observe(s, e, sides, trends, new Relation(earlier, offered));
        }
        witness = new Relation(new Relation(earlier, offered),
            new Relation(accepted, new Relation(
                compatible ? StructuralConstraint.Closed() : StructuralConstraint.Closed(offered),
                StructuralConstraint.Closed(checks.ToArray()))));
        return compatible;
    }

    private static bool Narrower(Relation earlier, Relation offered, Relation domain, List<IElement> checks)
    {
        if (!FiniteLaw.ClosedEntries(offered, out var entries) || entries.Length == 0 ||
            !StructuralConstraint.TryReadScope(earlier, out var oldEntries, out var seam, out var tail)) return false;
        var compatible = true;
        foreach (var oldEntry in oldEntries)
            compatible &= Member(oldEntry, domain, checks);
        foreach (var entry in entries)
        {
            compatible &= Member(entry, domain, checks);
            if (!seam.Equals(tail)) continue;
            compatible &= Member(entry, earlier, checks);
        }
        return compatible;
    }

    private static bool Member(IElement participant, Relation support, List<IElement> checks)
    {
        var match = StructuralConstraint.Match(participant, StructuralConstraint.Closed(), support);
        checks.Add(match);
        return StructuralConstraint.TryRead(match, out _, out var candidates, out _, out var unresolved) &&
            FiniteLaw.Entries(candidates).Length > 0 && FiniteLaw.Entries(unresolved).Length == 0;
    }
}
