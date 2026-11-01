using Gojo.Core;

namespace Gojo.Structures;

public static partial class TransformSurface
{
    /// <summary>
    /// Continues a partial replay in an explicit dependency-affected slice.
    /// The slice retains the full selected dependency graph, including consumers
    /// beyond the current stale frontier. Replays must replace pending consumers
    /// whose affected producers have already been replayed. This records supplied
    /// observations; it neither executes their math nor validates their truth.
    /// </summary>
    public static bool TryResumeDependencyReplay(
        Carrier root,
        Relation affectedSlice,
        Relation partialReplay,
        IReadOnlyList<Relation> replays,
        IElement residueOrTension,
        IElement provenance,
        out Relation resumedReplay)
    {
        resumedReplay = null!;
        if (!TryReadDependencyAffectedSlice(affectedSlice, out var scopePrevious, out _,
                out var dependencyFrame, out var scopeCarried, out var affectedFrame, out _, out _) ||
            !TryReadPartialReplay(partialReplay, out var previous, out var carried, out var replayFrame,
                out _, out var partialResidue, out _) ||
            !ReferenceEquals(scopePrevious, previous) || !ReferenceEquals(scopeCarried, carried) ||
            !TryReadApplicationDependencyFrame(dependencyFrame, out var dependencies) ||
            !TryReadApplicationFrame(affectedFrame, out var affected) ||
            !TryReadReplayFrame(replayFrame, out var existingReplays) ||
            !TryReadStaleDependencyResidue(partialResidue, out var staleDependencies) ||
            replays is null || replays.Count == 0 ||
            !TryReadRemainingStaleDependencies(staleDependencies, replays, out _)) return false;

        var combined = existingReplays.ToList();
        foreach (var replay in replays)
        {
            if (!TryReadReplay(replay, out var previousApplication, out _, out _, out _, out _, out _) ||
                !ContainsRelation(affected, previousApplication) ||
                TryFindReplayForPrevious(combined, previousApplication, out _)) return false;
            foreach (var dependency in dependencies)
            {
                if (!TryReadApplicationDependency(dependency, out var producer, out var consumer, out _, out _, out _)) return false;
                if (Equals(consumer, previousApplication) && ContainsRelation(affected, producer) &&
                    !TryFindReplayForPrevious(combined, producer, out _)) return false;
            }
            combined.Add(replay);
        }

        TryCreateStaleDependencyResidue(dependencies, affected, combined, UnknownPoint(), provenance,
            out var nextStale);
        return TryCreatePartialReplay(root, previous, carried, combined,
            CombineEvidence(WithoutStaleDependencies(partialResidue), residueOrTension, nextStale),
            Frame(new Carrier(), affectedSlice, partialReplay, provenance), out resumedReplay);
    }

    // Remove only interpreted stale-dependency records. Other residue is still
    // an outstanding obligation, even when every application has been replayed.
    private static IElement WithoutStaleDependencies(IElement residue)
    {
        if (residue is not Relation relation || IsUnknownPoint(relation)) return residue;
        if (TryReadStaleDependency(relation, out _, out _, out _, out _, out _)) return UnknownPoint();
        if (!relation.TryReadOrderedGroup(out var fields) || fields.Length == 0) return residue;
        var retained = fields.Select(WithoutStaleDependencies).ToArray();
        if (fields.Zip(retained).All(pair => ReferenceEquals(pair.First, pair.Second))) return residue;
        return CombineEvidence(retained);
    }
}
