using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Encoding;

/// <summary>
/// Tiny settlement surface for an interval focus. It reads a focus point
/// against a derived interval-role read and proposes the nearest point inside
/// that role band. The correction is kept as structure: ordered current point
/// then proposed point.
/// </summary>
public static class IntervalFocusSettlementSurface
{
    public readonly record struct SettlementRead(
        Relation FocusDefinition,
        Relation TargetReadDefinition,
        Relation IntervalDefinition,
        Carrier Carrier,
        long CurrentTicks,
        long DesiredStartTicks,
        long DesiredEndTicks,
        long ProposedTicks,
        Relation Correction);

    public static bool TryCreateSettlement(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        Relation focusDefinition,
        Relation targetReadDefinition,
        Carrier root,
        out Relation settlement)
    {
        if (!TryPlan(graph, focusDefinition, targetReadDefinition, out var read))
        {
            settlement = null!;
            return false;
        }

        settlement = ReadWitness.Create(
            root ?? new Carrier(),
            focusDefinition,
            targetReadDefinition,
            Relation.Point(read.Carrier, read.ProposedTicks),
            read.Correction,
            read.IntervalDefinition);
        return true;
    }

    public static bool TryRead(Relation settlement, out SettlementRead read)
    {
        if (ReadWitness.TryRead(
                settlement,
                out var selected,
                out var readSurface,
                out var result,
                out var residueOrTension,
                out var provenance) &&
            selected is Relation focusDefinition &&
            readSurface is Relation targetReadDefinition &&
            provenance is Relation intervalDefinition &&
            TryReadPoint(result, out var proposed) &&
            TryReadCorrection(
                residueOrTension,
                out var carrier,
                out var currentTicks,
                out var proposedTicks,
                out var correction) &&
            carrier.SameIdentityAs(proposed.Carrier) &&
            proposedTicks == proposed.Ticks &&
            TryReadTargetBand(
                targetReadDefinition,
                intervalDefinition,
                out _,
                out var desiredStart,
                out var desiredEnd))
        {
            read = new SettlementRead(
                focusDefinition,
                targetReadDefinition,
                intervalDefinition,
                carrier,
                currentTicks,
                desiredStart,
                desiredEnd,
                proposedTicks,
                correction);
            return true;
        }

        read = default;
        return false;
    }

    private static bool TryPlan(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        Relation focusDefinition,
        Relation targetReadDefinition,
        out SettlementRead read)
    {
        if (!graph.TryFind(focusDefinition, out var focusNode) ||
            !TryReadPoint(focusNode.Read.Body, out var focus) ||
            !graph.TryFind(targetReadDefinition, out var targetNode) ||
            !TryReadTargetBand(
                targetNode.Definition,
                expectedIntervalDefinition: null,
                out var intervalDefinition,
                out var desiredStart,
                out var desiredEnd) ||
            !focus.Carrier.SameIdentityAs(intervalDefinition.Carrier))
        {
            read = default;
            return false;
        }

        var proposedTicks = ClosestTickInside(focus.Ticks, desiredStart, desiredEnd);
        var correction = Correction(new Carrier(), focus.Carrier, focus.Ticks, proposedTicks);
        read = new SettlementRead(
            focusDefinition,
            targetReadDefinition,
            intervalDefinition.Definition,
            focus.Carrier,
            focus.Ticks,
            desiredStart,
            desiredEnd,
            proposedTicks,
            correction);
        return true;
    }

    private static long ClosestTickInside(long ticks, long desiredStart, long desiredEnd)
    {
        if (ticks < desiredStart)
        {
            return desiredStart;
        }

        if (ticks > desiredEnd)
        {
            return desiredEnd;
        }

        return ticks;
    }

    private static Relation Correction(Carrier root, Carrier carrier, long currentTicks, long proposedTicks) =>
        Relation.OrderedGroup(
            root,
            Relation.Point(carrier, currentTicks),
            Relation.Point(carrier, proposedTicks));

    private static bool TryReadTargetBand(
        Relation targetReadDefinition,
        Relation? expectedIntervalDefinition,
        out TargetBandRead read,
        out long desiredStart,
        out long desiredEnd)
    {
        if (ConceptDefinitionSurface.TryRead(targetReadDefinition, out var definitionRead) &&
            definitionRead.Body is Relation witness &&
            ReadWitness.TryRead(
                witness,
                out var selected,
                out _,
                out var result,
                out _,
                out _) &&
            selected is Relation intervalDefinition &&
            (expectedIntervalDefinition is null || ReferenceEquals(intervalDefinition, expectedIntervalDefinition)) &&
            TryReadSegment(result, out var carrier, out desiredStart, out desiredEnd))
        {
            read = new TargetBandRead(intervalDefinition, carrier);
            return true;
        }

        read = default;
        desiredStart = 0;
        desiredEnd = 0;
        return false;
    }

    private static bool TryReadCorrection(
        IElement element,
        out Carrier carrier,
        out long currentTicks,
        out long proposedTicks,
        out Relation correction)
    {
        if (element is Relation relation &&
            EncodedTextSurface.TryReadRootElements(relation, out var elements) &&
            elements.Length == 2 &&
            TryReadPoint(elements[0], out var current) &&
            TryReadPoint(elements[1], out var proposed) &&
            current.Carrier.SameIdentityAs(proposed.Carrier))
        {
            carrier = current.Carrier;
            currentTicks = current.Ticks;
            proposedTicks = proposed.Ticks;
            correction = relation;
            return true;
        }

        carrier = Carrier.Unknown;
        currentTicks = 0;
        proposedTicks = 0;
        correction = null!;
        return false;
    }

    private static bool TryReadPoint(IElement element, out CarrierLocation location)
    {
        if (element is Relation point &&
            point.Left is CarrierLocation pointLocation &&
            point.Right is Carrier pointCarrier &&
            pointLocation.Carrier.SameIdentityAs(pointCarrier))
        {
            location = pointLocation;
            return true;
        }

        location = default;
        return false;
    }

    private static bool TryReadSegment(IElement element, out Carrier carrier, out long start, out long end)
    {
        if (element is Relation segment &&
            TryReadPoint(segment.Left, out var left) &&
            TryReadPoint(segment.Right, out var right) &&
            left.Carrier.SameIdentityAs(right.Carrier))
        {
            carrier = left.Carrier;
            start = Math.Min(left.Ticks, right.Ticks);
            end = Math.Max(left.Ticks, right.Ticks);
            return true;
        }

        carrier = Carrier.Unknown;
        start = 0;
        end = 0;
        return false;
    }

    private readonly record struct TargetBandRead(Relation Definition, Carrier Carrier);
}
