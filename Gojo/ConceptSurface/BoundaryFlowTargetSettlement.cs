using Gojo.Core;
using Gojo.Structures;

namespace Gojo.ConceptSurface;

/// <summary>
/// Applies an accepted transfer to an existing target value and records the
/// transition as a time-step witness. This is still a concept-surface
/// settlement prototype; it does not mutate carrier state.
/// </summary>
public static class BoundaryFlowTargetSettlement
{
    public static Relation Create(
        Carrier root,
        Relation targetBefore,
        Relation transferSurface,
        Relation targetAfter,
        IElement residue,
        Relation timeline)
    {
        var before = targetBefore ?? ConceptSurface.UnknownPoint();
        var transfer = transferSurface ?? ConceptSurface.UnknownPoint();
        var after = targetAfter ?? ConceptSurface.UnknownPoint();
        var residueOrTension = residue ?? ConceptSurface.UnknownPoint();
        var time = timeline ?? ConceptSurface.UnknownPoint();
        var witness = ReadWitness.Create(
            new Carrier(),
            before,
            transfer,
            after,
            residueOrTension,
            time);

        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            before,
            transfer,
            after,
            residueOrTension,
            time,
            witness);
    }

    public static bool TrySettle(
        Relation targetBefore,
        Relation transferSurface,
        out Relation settlementSurface)
    {
        if (!BoundaryFlowValueSurface.TryReadValueWithBasis(
                targetBefore,
                out var beforeRead) ||
            !BoundaryFlowValueTransfer.TryReadSummary(transferSurface, out var transferRead))
        {
            settlementSurface = null!;
            return false;
        }

        var residue = transferRead.Blocked.Magnitude.Ticks == 0
            ? transferRead.Accepted.ResidueOrTension
            : BlockedResidue(transferSurface);
        var targetAfter = targetBefore;

        if (transferRead.HasAccepted &&
            transferRead.Accepted.Magnitude.Ticks != 0)
        {
            if (!CanApplyAcceptedToTarget(targetBefore, transferRead.Accepted, out var deltaTicks) ||
                !BoundaryFlowValueSettlement.TryApplySignedDelta(
                    targetBefore,
                    beforeRead.LaneBasis,
                    deltaTicks,
                    out targetAfter))
            {
                targetAfter = targetBefore;
                residue = transferRead.Accepted.ResidueOrTension;
            }
        }

        var time = new Carrier();
        var step = TimeStepSequence.Step(
            new Carrier(),
            time,
            0,
            1,
            targetBefore,
            transferSurface,
            targetAfter,
            residue);
        var timeline = TimeStepSequence.Create(new Carrier(), time, targetBefore, step);

        settlementSurface = Create(
            new Carrier(),
            targetBefore,
            transferSurface,
            targetAfter,
            residue,
            timeline);
        return true;
    }

    public static bool TryRead(
        Relation settlementSurface,
        out Relation targetBefore,
        out Relation transferSurface,
        out Relation targetAfter,
        out IElement residue,
        out Relation timeline)
    {
        if (ConceptSurfaceReader.TryReadRootElements(settlementSurface, out var elements) &&
            elements.Length >= 5 &&
            elements[0] is Relation before &&
            elements[1] is Relation transfer &&
            elements[2] is Relation after &&
            elements[4] is Relation time)
        {
            targetBefore = before;
            transferSurface = transfer;
            targetAfter = after;
            residue = elements[3];
            timeline = time;
            return true;
        }

        targetBefore = null!;
        transferSurface = null!;
        targetAfter = null!;
        residue = default!;
        timeline = null!;
        return false;
    }

    public static bool TryReadAfter(
        Relation settlementSurface,
        out BoundaryFlowValueRead afterRead)
    {
        if (TryRead(settlementSurface, out _, out _, out var targetAfter, out _, out _))
        {
            return BoundaryFlowValueSurface.TryReadValue(targetAfter, out afterRead);
        }

        afterRead = default;
        return false;
    }

    public static bool TryReadTimeline(Relation settlementSurface, out Relation timeline)
    {
        if (TryRead(settlementSurface, out _, out _, out _, out _, out var candidate) &&
            TimeStepSequence.TryRead(candidate, out _, out _, out _))
        {
            timeline = candidate;
            return true;
        }

        timeline = null!;
        return false;
    }

    public static bool TryReadWitness(
        Relation settlementSurface,
        Carrier root,
        out Relation witness)
    {
        if (ReadWitness.TryReadEmbedded(settlementSurface, index: 5, out witness))
        {
            return true;
        }

        if (TryRead(
                settlementSurface,
                out var targetBefore,
                out var transferSurface,
                out var targetAfter,
                out var residue,
                out var timeline))
        {
            witness = ReadWitness.Create(
                ConceptSurface.RootOrNew(root),
                targetBefore,
                transferSurface,
                targetAfter,
                residue,
                timeline);
            return true;
        }

        witness = null!;
        return false;
    }

    private static bool CanApplyAcceptedToTarget(
        Relation targetBefore,
        BoundaryFlowValueRead accepted,
        out long deltaTicks)
    {
        if (!BoundaryFlowValueSurface.TryReadValue(targetBefore, out var before) ||
            !before.IsLegal ||
            !accepted.IsLegal ||
            !before.IsSignedBoundary ||
            !accepted.IsSignedBoundary ||
            before.Lane.Flow != accepted.Lane.Flow)
        {
            deltaTicks = 0;
            return false;
        }

        deltaTicks = accepted.SignedTicks;
        return true;
    }

    private static IElement BlockedResidue(Relation transferSurface)
    {
        if (BoundaryFlowValueTransfer.TryRead(
                transferSurface,
                out _,
                out _,
                out var blocked,
                out _))
        {
            return blocked;
        }

        return ConceptSurface.UnknownPoint();
    }
}

/// <summary>
/// Chains several target settlements into one visible run. The helper builds a
/// fresh combined timeline from the same before/transfer/after/residue evidence
/// already present in each local settlement step.
/// </summary>

/// <summary>
/// Chains several target settlements into one visible run. The helper builds a
/// fresh combined timeline from the same before/transfer/after/residue evidence
/// already present in each local settlement step.
/// </summary>
public static class BoundaryFlowTargetSettlementRun
{
    public static Relation Create(
        Carrier root,
        Relation initialTarget,
        Relation finalTarget,
        Relation transfers,
        Relation residues,
        Relation timeline)
    {
        var initial = initialTarget ?? ConceptSurface.UnknownPoint();
        var final = finalTarget ?? ConceptSurface.UnknownPoint();
        var transferGroup = transfers ?? ConceptSurface.UnknownPoint();
        var residueGroup = residues ?? ConceptSurface.UnknownPoint();
        var time = timeline ?? ConceptSurface.UnknownPoint();
        var witness = ReadWitness.Create(
            new Carrier(),
            initial,
            transferGroup,
            final,
            residueGroup,
            time);

        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            initial,
            final,
            transferGroup,
            residueGroup,
            time,
            witness);
    }

    public static bool TrySettle(
        Relation initialTarget,
        IReadOnlyList<Relation> transferSurfaces,
        out Relation runSurface)
    {
        if (initialTarget is null)
        {
            runSurface = null!;
            return false;
        }

        var transfers = CleanRelations(transferSurfaces);
        var current = initialTarget;
        var time = new Carrier();
        var steps = new List<Relation>();
        var residues = new List<IElement>();

        for (var index = 0; index < transfers.Length; index++)
        {
            if (!BoundaryFlowTargetSettlement.TrySettle(current, transfers[index], out var settlement) ||
                !BoundaryFlowTargetSettlement.TryRead(
                    settlement,
                    out var before,
                    out var transition,
                    out var after,
                    out var residue,
                    out _))
            {
                runSurface = null!;
                return false;
            }

            steps.Add(TimeStepSequence.Step(
                new Carrier(),
                time,
                index,
                index + 1,
                before,
                transition,
                after,
                residue));
            residues.Add(residue);
            current = after;
        }

        var timeline = TimeStepSequence.Create(new Carrier(), time, initialTarget, [.. steps]);
        runSurface = Create(
            new Carrier(),
            initialTarget,
            current,
            Relation.OrderedGroup(new Carrier(), transfers),
            Relation.OrderedGroup(new Carrier(), [.. residues]),
            timeline);
        return true;
    }

    public static bool TrySettle(
        Relation initialTarget,
        out Relation runSurface,
        params Relation[] transferSurfaces) =>
        TrySettle(initialTarget, transferSurfaces, out runSurface);

    public static bool TryRead(
        Relation runSurface,
        out Relation initialTarget,
        out Relation finalTarget,
        out Relation transfers,
        out Relation residues,
        out Relation timeline)
    {
        if (ConceptSurfaceReader.TryReadRootElements(runSurface, out var elements) &&
            elements.Length >= 5 &&
            elements[0] is Relation initial &&
            elements[1] is Relation final &&
            elements[2] is Relation transferGroup &&
            elements[3] is Relation residueGroup &&
            elements[4] is Relation time)
        {
            initialTarget = initial;
            finalTarget = final;
            transfers = transferGroup;
            residues = residueGroup;
            timeline = time;
            return true;
        }

        initialTarget = null!;
        finalTarget = null!;
        transfers = null!;
        residues = null!;
        timeline = null!;
        return false;
    }

    public static bool TryReadFinal(
        Relation runSurface,
        out BoundaryFlowValueRead finalRead)
    {
        if (TryRead(runSurface, out _, out var final, out _, out _, out _))
        {
            return BoundaryFlowValueSurface.TryReadValue(final, out finalRead);
        }

        finalRead = default;
        return false;
    }

    public static bool TryReadTimeline(Relation runSurface, out Relation timeline)
    {
        if (TryRead(runSurface, out _, out _, out _, out _, out var candidate) &&
            TimeStepSequence.TryRead(candidate, out _, out _, out _))
        {
            timeline = candidate;
            return true;
        }

        timeline = null!;
        return false;
    }

    public static bool TryReadWitness(
        Relation runSurface,
        Carrier root,
        out Relation witness)
    {
        if (ReadWitness.TryReadEmbedded(runSurface, index: 5, out witness))
        {
            return true;
        }

        if (TryRead(
                runSurface,
                out var initialTarget,
                out var finalTarget,
                out var transfers,
                out var residues,
                out var timeline))
        {
            witness = ReadWitness.Create(
                ConceptSurface.RootOrNew(root),
                initialTarget,
                transfers,
                finalTarget,
                residues,
                timeline);
            return true;
        }

        witness = null!;
        return false;
    }

    public static bool TryReadTotals(
        Relation runSurface,
        out BoundaryFlowSettlementRunTotals totals)
    {
        if (!TryReadTimeline(runSurface, out var timeline) ||
            !TimeStepSequence.TryRead(timeline, out _, out _, out var steps))
        {
            totals = default;
            return false;
        }

        var accepted = 0L;
        var blocked = 0L;
        foreach (var step in steps)
        {
            if (!TimeStepSequence.TryReadStep(
                    step,
                    out _,
                    out _,
                    out _,
                    out _,
                    out var transition,
                    out _,
                    out _) ||
                transition is not Relation transfer ||
                !BoundaryFlowValueTransfer.TryReadSummary(transfer, out var transferRead))
            {
                totals = default;
                return false;
            }

            if (transferRead.HasAccepted)
            {
                accepted = checked(accepted + transferRead.Accepted.Magnitude.Ticks);
            }

            blocked = checked(blocked + transferRead.Blocked.Magnitude.Ticks);
        }

        totals = new BoundaryFlowSettlementRunTotals(accepted, blocked, steps.Length);
        return true;
    }

    private static Relation[] CleanRelations(IReadOnlyList<Relation> relations)
    {
        if (relations is null || relations.Count == 0)
        {
            return [];
        }

        return [.. relations.Where(relation => relation is not null)];
    }
}


public readonly record struct BoundaryFlowSettlementRunTotals(
    long AcceptedMagnitude,
    long BlockedMagnitude,
    int StepCount);

/// <summary>
/// Builds and reads boundary/domain exposure surfaces. A boundary surface
/// records whether left and right boundary sides are present, plus the
/// selected whole/axis being described.
/// </summary>

