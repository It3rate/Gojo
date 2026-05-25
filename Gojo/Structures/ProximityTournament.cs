using Gojo.Core;

namespace Gojo.Structures;

/// <summary>
/// Chooses values by proximity to a target. Each candidate keeps the equality
/// claim, optional boundary claim, and selection measure as Gojo structure; the
/// final choice is still an ordinary minimum tournament over that measure.
/// </summary>
public static class ProximityTournament
{
    /// <summary>Chooses the candidate with the smallest equality-claim residual magnitude.</summary>
    public static Relation Nearest(
        Carrier root,
        IReadOnlyList<IElement> values,
        IElement target,
        Carrier residual,
        Carrier measure,
        out Relation[] choices,
        out Relation[] rounds) =>
        Create(root, values, target, residual, measure, boundaryClaim: null, out choices, out rounds);

    /// <summary>Chooses the nearest candidate that satisfies value &gt;= target when possible.</summary>
    public static Relation NearestAtLeast(
        Carrier root,
        IReadOnlyList<IElement> values,
        IElement target,
        Carrier residual,
        Carrier measure,
        out Relation[] choices,
        out Relation[] rounds) =>
        Create(root, values, target, residual, measure, OperationClaim.AtLeast, out choices, out rounds);

    /// <summary>Chooses the nearest candidate that satisfies value &lt;= target when possible.</summary>
    public static Relation NearestAtMost(
        Carrier root,
        IReadOnlyList<IElement> values,
        IElement target,
        Carrier residual,
        Carrier measure,
        out Relation[] choices,
        out Relation[] rounds) =>
        Create(root, values, target, residual, measure, OperationClaim.AtMost, out choices, out rounds);

    /// <summary>Chooses the nearest candidate that satisfies lower &lt;= value &lt;= upper when possible.</summary>
    public static Relation NearestWithin(
        Carrier root,
        IReadOnlyList<IElement> values,
        IElement target,
        IElement lower,
        IElement upper,
        Carrier residual,
        Carrier measure,
        out Relation[] choices,
        out Relation[] rounds)
    {
        var lowerElement = lower ?? Relation.Point(Carrier.Unknown, 0);
        var upperElement = upper ?? Relation.Point(Carrier.Unknown, 0);
        return Create(
            root,
            values,
            target,
            residual,
            measure,
            (claimRoot, value, _, claimResidual) => OperationClaim.Between(
                claimRoot,
                value,
                lowerElement,
                upperElement,
                claimResidual),
            out choices,
            out rounds);
    }

    /// <summary>Reads the winning choice from a proximity tournament.</summary>
    public static bool TryReadWinner(Relation proximity, out Relation choice)
    {
        if (TryReadTournament(proximity, out var tournament) &&
            ExtremaTournament.TryReadWinner(tournament, out var winner) &&
            ExtremaTournament.TryReadEntry(winner, out var value, out _) &&
            value is Relation winnerChoice &&
            TryReadChoice(winnerChoice, out _, out _, out _, out _, out _, out _, out _))
        {
            choice = winnerChoice;
            return true;
        }

        choice = null!;
        return false;
    }

    /// <summary>Reads the underlying minimum tournament.</summary>
    public static bool TryReadTournament(Relation proximity, out Relation tournament)
    {
        if (TryReadRootElements(proximity, out var elements) &&
            elements.Length >= 2 &&
            elements[1] is Relation candidateTournament &&
            ExtremaTournament.TryReadWinner(candidateTournament, out _))
        {
            tournament = candidateTournament;
            return true;
        }

        tournament = null!;
        return false;
    }

    /// <summary>Reads the generic time-step timeline from the underlying choice tournament.</summary>
    public static bool TryReadTimeline(Relation proximity, out Relation timeline)
    {
        if (TryReadTournament(proximity, out var tournament) &&
            ExtremaTournament.TryReadTimeline(tournament, out var tournamentTimeline))
        {
            timeline = tournamentTimeline;
            return true;
        }

        timeline = null!;
        return false;
    }

    /// <summary>Reads timed pairwise choice rounds from the underlying tournament.</summary>
    public static bool TryReadRounds(Relation proximity, out Relation[] rounds)
    {
        if (TryReadTournament(proximity, out var tournament) &&
            ExtremaTournament.TryReadRounds(tournament, out var tournamentRounds))
        {
            rounds = tournamentRounds;
            return true;
        }

        rounds = [];
        return false;
    }

    /// <summary>Reads candidate choices from a proximity tournament.</summary>
    public static bool TryReadChoices(Relation proximity, out Relation[] choices)
    {
        if (TryReadRootElements(proximity, out var elements) &&
            elements.Length >= 2)
        {
            choices = [.. elements.Skip(2).OfType<Relation>().Where(choice =>
                TryReadChoice(choice, out _, out _, out _, out _, out _, out _, out _))];
            return true;
        }

        choices = [];
        return false;
    }

    /// <summary>Reads one candidate choice and its claim evidence.</summary>
    public static bool TryReadChoice(
        Relation choice,
        out IElement value,
        out IElement target,
        out Relation equalityClaim,
        out IElement boundaryClaim,
        out CarrierLocation distance,
        out CarrierLocation boundaryViolation,
        out CarrierLocation selectionMeasure)
    {
        if (TryReadRootElements(choice, out var elements) &&
            elements.Length == 7 &&
            elements[2] is Relation equality &&
            elements[4] is CarrierLocation distanceLocation &&
            elements[5] is CarrierLocation boundaryViolationLocation &&
            elements[6] is CarrierLocation selectionMeasureLocation &&
            OperationClaim.TryRead(equality, out _, out _, out _, out _, out _, out _))
        {
            value = elements[0];
            target = elements[1];
            equalityClaim = equality;
            boundaryClaim = elements[3];
            distance = distanceLocation;
            boundaryViolation = boundaryViolationLocation;
            selectionMeasure = selectionMeasureLocation;
            return true;
        }

        value = default!;
        target = default!;
        equalityClaim = null!;
        boundaryClaim = default!;
        distance = default;
        boundaryViolation = default;
        selectionMeasure = default;
        return false;
    }

    private static Relation Create(
        Carrier root,
        IReadOnlyList<IElement> values,
        IElement target,
        Carrier residual,
        Carrier measure,
        Func<Carrier, IElement, IElement, Carrier, Relation>? boundaryClaim,
        out Relation[] choices,
        out Relation[] rounds)
    {
        var holder = root ?? new Carrier();
        var residualCarrier = residual ?? Carrier.Unknown;
        var measureCarrier = measure ?? residualCarrier;
        var targetElement = target ?? Relation.Point(Carrier.Unknown, 0);
        var candidates = values ?? [];
        var count = candidates.Count;
        choices = new Relation[count];
        var reads = new ChoiceRead[count];
        var maxDistance = 0L;

        for (var index = 0; index < count; index++)
        {
            var value = candidates[index] ?? Relation.Point(Carrier.Unknown, 0);
            var equality = OperationClaim.Equality(new Carrier(), value, targetElement, residualCarrier);
            OperationClaim.TryRead(
                equality,
                out _,
                out _,
                out _,
                out var distance,
                out _,
                out _);

            var boundary = boundaryClaim is null
                ? (IElement)new CarrierLocation(residualCarrier, 0)
                : boundaryClaim(new Carrier(), value, targetElement, residualCarrier);
            var boundaryViolation = ReadBoundaryViolation(boundary, residualCarrier);

            reads[index] = new ChoiceRead(value, equality, boundary, distance, boundaryViolation);
            maxDistance = Math.Max(maxDistance, distance.Ticks);
        }

        var penaltyStep = SaturatingAdd(maxDistance, 1);
        var entries = new Relation[count];
        for (var index = 0; index < count; index++)
        {
            var read = reads[index];
            var selectionTicks = SaturatingAdd(
                read.Distance.Ticks,
                SaturatingMultiply(read.BoundaryViolation.Ticks, penaltyStep));
            var selectionMeasure = new CarrierLocation(measureCarrier, selectionTicks);
            choices[index] = Choice(
                read.Value,
                targetElement,
                read.EqualityClaim,
                read.BoundaryClaim,
                read.Distance,
                read.BoundaryViolation,
                selectionMeasure);
            entries[index] = ExtremaTournament.Entry(choices[index], selectionMeasure);
        }

        var tournament = ExtremaTournament.Minimum(new Carrier(), entries, out rounds);
        return Relation.OrderedGroup(holder, [targetElement, tournament, .. choices]);
    }

    private static Relation Choice(
        IElement value,
        IElement target,
        Relation equalityClaim,
        IElement boundaryClaim,
        CarrierLocation distance,
        CarrierLocation boundaryViolation,
        CarrierLocation selectionMeasure) =>
        Relation.OrderedGroup(
            new Carrier(),
            value,
            target,
            equalityClaim,
            boundaryClaim,
            distance,
            boundaryViolation,
            selectionMeasure);

    private static CarrierLocation ReadBoundaryViolation(IElement boundary, Carrier residual)
    {
        if (boundary is Relation boundaryRelation &&
            OperationClaim.TryReadBetween(
                boundaryRelation,
                out _,
                out _,
                out _,
                out _,
                out _,
                out var totalViolation,
                out _))
        {
            return totalViolation;
        }

        if (boundary is Relation singleClaim &&
            OperationClaim.TryRead(
                singleClaim,
                out _,
                out _,
                out _,
                out _,
                out var violationMagnitude,
                out _))
        {
            return violationMagnitude;
        }

        return new CarrierLocation(residual, 0);
    }

    private static bool TryReadRootElements(Relation structure, out IElement[] elements)
    {
        if (structure?.Right is Carrier holder &&
            structure.TryReadOrderedElementsOn(holder, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    private static long SaturatingAdd(long left, long right)
    {
        var sum = (Int128)left + right;
        return ClampToLong(sum);
    }

    private static long SaturatingMultiply(long left, long right)
    {
        var product = (Int128)left * right;
        return ClampToLong(product);
    }

    private static long ClampToLong(Int128 value)
    {
        if (value > long.MaxValue)
        {
            return long.MaxValue;
        }

        return value < long.MinValue ? long.MinValue : (long)value;
    }

    private readonly record struct ChoiceRead(
        IElement Value,
        Relation EqualityClaim,
        IElement BoundaryClaim,
        CarrierLocation Distance,
        CarrierLocation BoundaryViolation);
}
