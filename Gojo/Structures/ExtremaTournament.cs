using Gojo.Core;

namespace Gojo.Structures;

/// <summary>
/// Pairwise min/max tournament helpers over relation-shaped entries. Each entry
/// carries a value element and a measure location; each bout preserves the
/// tested one-sided claim, winner, remainder, and margin as ordinary Gojo
/// structure.
/// </summary>
public static class ExtremaTournament
{
    /// <summary>Creates a tournament entry from a value element and a measure location.</summary>
    public static Relation Entry(IElement value, CarrierLocation measure) =>
        Relation.OrderedGroup(
            new Carrier(),
            value ?? Relation.Point(Carrier.Unknown, 0),
            measure);

    /// <summary>Builds a pairwise tournament where lower measure wins.</summary>
    public static Relation Minimum(Carrier root, IReadOnlyList<Relation> entries, out Relation[] rounds) =>
        Create(root, entries, lowerWins: true, out rounds);

    /// <summary>Builds a pairwise tournament where higher measure wins.</summary>
    public static Relation Maximum(Carrier root, IReadOnlyList<Relation> entries, out Relation[] rounds) =>
        Create(root, entries, lowerWins: false, out rounds);

    /// <summary>Reads the winning entry from a tournament.</summary>
    public static bool TryReadWinner(Relation tournament, out Relation winner)
    {
        if (TryReadRootElements(tournament, out var elements) &&
            elements.Length >= 1 &&
            elements[0] is Relation entry &&
            TryReadEntry(entry, out _, out _))
        {
            winner = entry;
            return true;
        }

        winner = null!;
        return false;
    }

    /// <summary>Reads tournament rounds.</summary>
    public static bool TryReadRounds(Relation tournament, out Relation[] rounds)
    {
        if (TryReadTimeline(tournament, out var timeline) &&
            TimeStepSequence.TryRead(timeline, out _, out _, out var timedRounds))
        {
            rounds = timedRounds;
            return true;
        }

        if (TryReadRootElements(tournament, out var elements) && elements.Length >= 1)
        {
            rounds = [.. elements.Skip(1).OfType<Relation>()];
            return true;
        }

        rounds = [];
        return false;
    }

    /// <summary>Reads the generic time-step timeline for tournament rounds.</summary>
    public static bool TryReadTimeline(Relation tournament, out Relation timeline)
    {
        if (TryReadRootElements(tournament, out var elements) &&
            elements.Length >= 2 &&
            elements[1] is Relation candidateTimeline &&
            TimeStepSequence.TryRead(candidateTimeline, out _, out _, out _))
        {
            timeline = candidateTimeline;
            return true;
        }

        timeline = null!;
        return false;
    }

    /// <summary>Reads one tournament round and its pairwise bouts.</summary>
    public static bool TryReadRound(Relation round, out CarrierLocation roundIndex, out Relation[] bouts)
    {
        if (TimeStepSequence.TryReadStep(
                round,
                out _,
                out var timeStart,
                out _,
                out _,
                out var transition,
                out _,
                out _) &&
            transition is Relation roundEvidence &&
            TryReadRoundEvidence(roundEvidence, out _, out var timedBouts))
        {
            roundIndex = timeStart;
            bouts = timedBouts;
            return true;
        }

        if (TryReadRoundEvidence(round, out roundIndex, out bouts))
        {
            return true;
        }

        roundIndex = default;
        bouts = [];
        return false;
    }

    /// <summary>Reads one pairwise bout from a tournament round.</summary>
    public static bool TryReadBout(
        Relation bout,
        out CarrierLocation boutIndex,
        out Relation winner,
        out Relation remainder,
        out CarrierLocation margin)
    {
        if (TryReadRootElements(bout, out var elements) &&
            elements.Length >= 4 &&
            TryReadPoint(elements[0], out boutIndex) &&
            elements[1] is Relation winnerEntry &&
            elements[2] is Relation remainderEntry &&
            elements[3] is CarrierLocation marginLocation &&
            TryReadEntry(winnerEntry, out _, out _) &&
            TryReadEntry(remainderEntry, out _, out _))
        {
            winner = winnerEntry;
            remainder = remainderEntry;
            margin = marginLocation;
            return true;
        }

        boutIndex = default;
        winner = null!;
        remainder = null!;
        margin = default;
        return false;
    }

    /// <summary>Reads the one-sided claim tested by a pairwise bout.</summary>
    public static bool TryReadBoutClaim(Relation bout, out Relation claim)
    {
        if (TryReadRootElements(bout, out var elements) &&
            elements.Length >= 5 &&
            elements[4] is Relation boutClaim &&
            OperationClaim.TryRead(boutClaim, out _, out _, out _, out _, out _, out _))
        {
            claim = boutClaim;
            return true;
        }

        claim = null!;
        return false;
    }

    /// <summary>Reads a tournament entry into its value and measure elements.</summary>
    public static bool TryReadEntry(Relation entry, out IElement value, out CarrierLocation measure)
    {
        if (TryReadRootElements(entry, out var elements) &&
            elements.Length == 2 &&
            elements[1] is CarrierLocation measureLocation)
        {
            value = elements[0];
            measure = measureLocation;
            return true;
        }

        value = default!;
        measure = default;
        return false;
    }

    private static Relation Create(
        Carrier root,
        IReadOnlyList<Relation> entries,
        bool lowerWins,
        out Relation[] rounds)
    {
        var current = NormalizeEntries(entries);
        var roundIndex = new Carrier();
        if (current.Length == 0)
        {
            rounds = [];
            var fallback = Entry(Relation.Point(Carrier.Unknown, 0), new CarrierLocation(Carrier.Unknown, 0));
            var timeline = TimeStepSequence.Create(
                new Carrier(),
                roundIndex,
                Relation.OrderedGroup(new Carrier()),
                rounds);
            return Relation.OrderedGroup(RootOrNew(root), fallback, timeline);
        }

        var builtRounds = new List<Relation>();
        var boutIndex = new Carrier();
        var initialField = EntryGroup(current);
        for (var round = 0; current.Length > 1; round++)
        {
            var next = new List<Relation>();
            var bouts = new List<IElement>();
            var remainders = new List<IElement>();
            var beforeField = EntryGroup(current);
            for (var index = 0; index < current.Length; index += 2)
            {
                if (index + 1 >= current.Length)
                {
                    next.Add(current[index]);
                    continue;
                }

                var bout = Bout(
                    boutIndex,
                    bouts.Count,
                    current[index],
                    current[index + 1],
                    lowerWins,
                    out var winner,
                    out var remainder);
                bouts.Add(bout);
                next.Add(winner);
                remainders.Add(remainder);
            }

            var roundEvidence = Relation.OrderedGroup(
                new Carrier(),
                [Relation.Point(roundIndex, round), .. bouts]);
            builtRounds.Add(TimeStepSequence.Step(
                new Carrier(),
                roundIndex,
                round,
                round + 1,
                beforeField,
                roundEvidence,
                EntryGroup(next),
                Relation.OrderedGroup(new Carrier(), [.. remainders])));
            current = [.. next];
        }

        rounds = [.. builtRounds];
        var roundTimeline = TimeStepSequence.Create(new Carrier(), roundIndex, initialField, rounds);
        return Relation.OrderedGroup(RootOrNew(root), current[0], roundTimeline);
    }

    private static Relation Bout(
        Carrier boutIndex,
        int index,
        Relation left,
        Relation right,
        bool lowerWins,
        out Relation winner,
        out Relation remainder)
    {
        var hasLeftMeasure = TryReadEntry(left, out _, out var leftMeasure);
        var hasRightMeasure = TryReadEntry(right, out _, out var rightMeasure);
        if (!hasLeftMeasure ||
            !hasRightMeasure ||
            !leftMeasure.Carrier.UnitShape.CompatibleWith(rightMeasure.Carrier.UnitShape))
        {
            var fallbackCarrier = leftMeasure.Carrier ?? rightMeasure.Carrier ?? Carrier.Unknown;
            var safeLeftMeasure = hasLeftMeasure ? leftMeasure : new CarrierLocation(fallbackCarrier, 0);
            var safeRightMeasure = hasRightMeasure ? rightMeasure : new CarrierLocation(fallbackCarrier, 0);
            var unresolvedClaim = lowerWins
                ? OperationClaim.AtLeast(new Carrier(), safeRightMeasure, safeLeftMeasure, fallbackCarrier)
                : OperationClaim.AtMost(new Carrier(), safeRightMeasure, safeLeftMeasure, fallbackCarrier);
            winner = left;
            remainder = right;
            return Relation.OrderedGroup(
                new Carrier(),
                Relation.Point(boutIndex, index),
                left,
                right,
                new CarrierLocation(fallbackCarrier, 0),
                unresolvedClaim);
        }

        var marginCarrier = leftMeasure.Carrier.IsUnknown
            ? rightMeasure.Carrier
            : leftMeasure.Carrier;
        var claim = lowerWins
            ? OperationClaim.AtLeast(new Carrier(), rightMeasure, leftMeasure, marginCarrier)
            : OperationClaim.AtMost(new Carrier(), rightMeasure, leftMeasure, marginCarrier);
        OperationClaim.TryRead(
            claim,
            out _,
            out _,
            out _,
            out var margin,
            out var violation,
            out _);
        var rightViolatesCurrent = violation.Ticks > 0;
        winner = rightViolatesCurrent ? right : left;
        remainder = rightViolatesCurrent ? left : right;

        return Relation.OrderedGroup(
            new Carrier(),
            Relation.Point(boutIndex, index),
            winner,
            remainder,
            margin,
            claim);
    }

    private static Relation[] NormalizeEntries(IReadOnlyList<Relation> entries) =>
        entries is null ? [] : [.. entries.Where(entry => TryReadEntry(entry, out _, out _))];

    private static Relation EntryGroup(IReadOnlyList<Relation> entries) =>
        Relation.OrderedGroup(new Carrier(), [.. entries.Cast<IElement>()]);

    private static bool TryReadRoundEvidence(Relation round, out CarrierLocation roundIndex, out Relation[] bouts)
    {
        if (TryReadRootElements(round, out var elements) &&
            elements.Length >= 1 &&
            TryReadPoint(elements[0], out roundIndex))
        {
            bouts = [.. elements.Skip(1).OfType<Relation>()];
            return true;
        }

        roundIndex = default;
        bouts = [];
        return false;
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

    private static Carrier RootOrNew(Carrier carrier) =>
        carrier ?? new Carrier();
}
