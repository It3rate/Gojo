using Gojo.Core;

namespace Gojo.Structures;

/// <summary>
/// Static construction and inspection helpers for value ladders. These helpers
/// create and read ordinary Gojo carriers and relations; they do not own hidden
/// ladder meaning after construction.
/// </summary>
public static class ResolutionLadder
{
    /// <summary>Creates independent level carriers sharing the root unit shape.</summary>
    public static Carrier[] CreateLevels(Carrier root, int levelCount)
    {
        if (levelCount <= 0)
        {
            return [];
        }

        var holder = RootOrUnknown(root);
        var levels = new Carrier[levelCount];
        for (var index = 0; index < levelCount; index++)
        {
            levels[index] = new Carrier(holder.UnitShape);
        }

        return levels;
    }

    /// <summary>
    /// Creates repeated carry-route steps. A step of 10 creates
    /// Segment(level,0,10) -> Point(next,0), which current settlement reads
    /// as ten lower ticks carrying one tick into the next level.
    /// </summary>
    public static Relation[] CreateCarrySteps(IReadOnlyList<Carrier> levels, long stepTicks)
    {
        var stepCount = StepCount(levels);
        var steps = new Relation[stepCount];
        for (var index = 0; index < stepCount; index++)
        {
            steps[index] = CreateCarryStep(levels[index], levels[index + 1], stepTicks);
        }

        return steps;
    }

    /// <summary>Creates per-step carry-route relations between adjacent levels.</summary>
    public static Relation[] CreateCarrySteps(IReadOnlyList<Carrier> levels, IReadOnlyList<long> stepTicks)
    {
        var stepCount = StepCount(levels);
        var steps = new Relation[stepCount];
        for (var index = 0; index < stepCount; index++)
        {
            steps[index] = CreateCarryStep(levels[index], levels[index + 1], RepeatLast(stepTicks, index));
        }

        return steps;
    }

    /// <summary>
    /// Creates per-step ratio relations between adjacent levels. These are
    /// structural ratio/projection steps; only shapes compatible with current
    /// settlement rules will actively route tension today.
    /// </summary>
    public static Relation[] CreateRatioSteps(
        IReadOnlyList<Carrier> levels,
        IReadOnlyList<(long LowerTicks, long UpperTicks)> ratios)
    {
        var stepCount = StepCount(levels);
        var steps = new Relation[stepCount];
        for (var index = 0; index < stepCount; index++)
        {
            var (lowerTicks, upperTicks) = RepeatLast(ratios, index);
            steps[index] = new Relation(
                Relation.Point(RootOrUnknown(levels[index]), lowerTicks),
                Relation.Point(RootOrUnknown(levels[index + 1]), upperTicks));
        }

        return steps;
    }

    /// <summary>
    /// Attaches level and step participants to a root as one ordered structure,
    /// and attaches each step relation to the carriers it references.
    /// </summary>
    public static Relation Attach(
        Carrier root,
        IReadOnlyList<Carrier> levels,
        IReadOnlyList<Relation> steps)
    {
        var holder = RootOrUnknown(root);
        if (CountOf(levels) == 0)
        {
            var route = Relation.Point(holder, 0);
            holder.Attach(route);
            return route;
        }

        var participants = new List<IElement>();
        for (var index = 0; index < levels.Count; index++)
        {
            participants.Add(RootOrUnknown(levels[index]));
            if (index < CountOf(steps) && steps[index] is { } step)
            {
                participants.Add(step);
                var firstCarrier = step.Carriers()
                    .FirstOrDefault(carrier => !carrier.IsUnknown);
                firstCarrier?.Attach(step);
            }
        }

        var structure = Relation.OrderedGroup(holder, [.. participants]);
        holder.Attach(structure);
        return structure;
    }

    /// <summary>Creates level carriers, repeated carry steps, and root structure.</summary>
    public static Relation AttachCarry(
        Carrier root,
        int levelCount,
        long stepTicks,
        out Carrier[] levels,
        out Relation[] steps)
    {
        levels = CreateLevels(root, levelCount);
        steps = CreateCarrySteps(levels, stepTicks);
        return Attach(root, levels, steps);
    }

    /// <summary>Creates level carriers, per-step carry steps, and root structure.</summary>
    public static Relation AttachCarry(
        Carrier root,
        IReadOnlyList<long> stepTicks,
        out Carrier[] levels,
        out Relation[] steps)
    {
        levels = CreateLevels(root, CountOf(stepTicks) + 1);
        steps = CreateCarrySteps(levels, stepTicks);
        return Attach(root, levels, steps);
    }

    /// <summary>Creates level carriers, per-step ratio relations, and root structure.</summary>
    public static Relation AttachRatios(
        Carrier root,
        IReadOnlyList<(long LowerTicks, long UpperTicks)> ratios,
        out Carrier[] levels,
        out Relation[] steps)
    {
        levels = CreateLevels(root, CountOf(ratios) + 1);
        steps = CreateRatioSteps(levels, ratios);
        return Attach(root, levels, steps);
    }

    /// <summary>Reads ordered root participants created by Attach.</summary>
    public static bool TryReadParticipants(Relation structure, Carrier root, out IElement[] participants)
    {
        if (structure is null)
        {
            participants = [];
            return false;
        }

        return structure.TryReadOrderedElementsOn(RootOrUnknown(root), out participants);
    }

    /// <summary>Reads carrier levels from a mixed level/step root structure.</summary>
    public static bool TryReadLevels(Relation structure, Carrier root, out Carrier[] levels)
    {
        if (!TryReadParticipants(structure, root, out var participants))
        {
            levels = [];
            return false;
        }

        levels = [.. participants.OfType<Carrier>()];
        return true;
    }

    /// <summary>Reads relation steps from a mixed level/step root structure.</summary>
    public static bool TryReadSteps(Relation structure, Carrier root, out Relation[] steps)
    {
        if (!TryReadParticipants(structure, root, out var participants))
        {
            steps = [];
            return false;
        }

        steps = [.. participants.OfType<Relation>()];
        return true;
    }

    /// <summary>Sets one level without interpreting the rest of the ladder.</summary>
    public static void SetLevel(IReadOnlyList<Carrier> levels, int levelIndex, long ticks)
    {
        if (TryReadLevel(levels, levelIndex, out var level))
        {
            level.Set(ticks);
        }
    }

    /// <summary>Adds ticks to one level and lets attached relations settle if they can.</summary>
    public static void AddToLevel(IReadOnlyList<Carrier> levels, int levelIndex, ulong ticks)
    {
        if (TryReadLevel(levels, levelIndex, out var level))
        {
            level.Add(Math.Min(ticks, (ulong)long.MaxValue));
        }
    }

    /// <summary>Clears level carriers through ordinary Set input.</summary>
    public static void Clear(IReadOnlyList<Carrier> levels)
    {
        for (var index = 0; index < CountOf(levels); index++)
        {
            SetLevel(levels, index, 0);
        }
    }

    /// <summary>Sets level digits in lowest-to-highest order.</summary>
    public static void SetDigitsLowToHigh(IReadOnlyList<Carrier> levels, params long[] digits)
    {
        Clear(levels);
        var count = Math.Min(CountOf(levels), CountOf(digits));
        for (var index = 0; index < count; index++)
        {
            SetLevel(levels, index, digits[index]);
        }
    }

    /// <summary>
    /// Sets digits starting at a chosen highest level and moving downward. For
    /// example, on a decimal ladder SetDigitsFrom(levels, 2, 3, 9, 6)
    /// represents 396.
    /// </summary>
    public static void SetDigitsFrom(
        IReadOnlyList<Carrier> levels,
        int highestLevelIndex,
        params long[] highToLowDigits)
    {
        if (CountOf(levels) == 0 || CountOf(highToLowDigits) == 0)
        {
            return;
        }

        Clear(levels);
        var targetIndex = Math.Clamp(highestLevelIndex, 0, levels.Count - 1);
        for (var digitIndex = 0; digitIndex < highToLowDigits.Length && targetIndex >= 0; digitIndex++, targetIndex--)
        {
            SetLevel(levels, targetIndex, highToLowDigits[digitIndex]);
        }
    }

    /// <summary>Reads a composite value using an explicit scale per level.</summary>
    public static long ReadComposite(IReadOnlyList<Carrier> levels, IReadOnlyList<long> levelScales)
    {
        var value = 0L;
        for (var index = 0; index < CountOf(levels); index++)
        {
            if (!TryReadLevel(levels, index, out var level))
            {
                continue;
            }

            value = SaturatingAdd(
                value,
                SaturatingMultiply(SaturatingToLong(level.Value), RepeatLast(levelScales, index)));
        }

        return value;
    }

    /// <summary>Reads a composite value from a repeated radix scale.</summary>
    public static long ReadCompositeFromRadix(IReadOnlyList<Carrier> levels, long radix) =>
        ReadComposite(levels, RepeatedRadixScales(CountOf(levels), radix));

    /// <summary>Reads a composite prefix from level zero through highestLevelIndex.</summary>
    public static long ReadCompositeFromRadix(
        IReadOnlyList<Carrier> levels,
        int highestLevelIndex,
        long radix)
    {
        if (CountOf(levels) == 0 || highestLevelIndex < 0)
        {
            return 0;
        }

        var count = Math.Min(levels.Count, highestLevelIndex + 1);
        return ReadCompositeFromRadix([.. levels.Take(count)], radix);
    }

    /// <summary>Reads one level as a root-grounded contribution.</summary>
    public static CarrierLocation ReadLevelOnRoot(
        Carrier root,
        IReadOnlyList<Carrier> levels,
        IReadOnlyList<long> levelScales,
        int levelIndex)
    {
        var holder = RootOrUnknown(root);
        if (!TryReadLevel(levels, levelIndex, out var level))
        {
            return new CarrierLocation(holder, 0);
        }

        return new CarrierLocation(
            holder,
            SaturatingMultiply(SaturatingToLong(level.Value), RepeatLast(levelScales, levelIndex)));
    }

    /// <summary>Reads a repeated-radix level as a root-grounded contribution.</summary>
    public static CarrierLocation ReadLevelOnRoot(
        Carrier root,
        IReadOnlyList<Carrier> levels,
        long radix,
        int levelIndex) =>
        ReadLevelOnRoot(root, levels, RepeatedRadixScales(CountOf(levels), radix), levelIndex);

    /// <summary>Reads a composite prefix as a root-grounded location.</summary>
    public static CarrierLocation ReadCompositeOnRoot(
        Carrier root,
        IReadOnlyList<Carrier> levels,
        int highestLevelIndex,
        long radix) =>
        new(RootOrUnknown(root), ReadCompositeFromRadix(levels, highestLevelIndex, radix));

    /// <summary>Returns repeated radix scales for a level count.</summary>
    public static long[] RepeatedRadixScales(int levelCount, long radix)
    {
        if (levelCount <= 0)
        {
            return [];
        }

        var scales = new long[levelCount];
        var scale = 1L;
        for (var index = 0; index < levelCount; index++)
        {
            scales[index] = scale;
            scale = SaturatingMultiply(scale, radix);
        }

        return scales;
    }

    /// <summary>Reads the exclusive span represented by repeated radix levels.</summary>
    public static long SpanFromRadix(int levelCount, long radix)
    {
        if (levelCount <= 0)
        {
            return 0;
        }

        var span = 1L;
        for (var index = 0; index < levelCount; index++)
        {
            span = SaturatingMultiply(span, radix);
        }

        return span;
    }

    /// <summary>
    /// Projects a repeated-radix composite prefix into a target frame such as
    /// 0..100 percentage ticks.
    /// </summary>
    public static Relation ProjectCompositeTo(
        Carrier root,
        IReadOnlyList<Carrier> levels,
        int highestLevelIndex,
        long radix,
        Carrier target,
        long targetStart,
        long targetEnd)
    {
        var holder = RootOrUnknown(root);
        var targetCarrier = RootOrUnknown(target);
        var levelCount = highestLevelIndex < 0
            ? 0
            : Math.Min(CountOf(levels), highestLevelIndex + 1);
        var span = SpanFromRadix(levelCount, radix);
        var frame = new Relation(
            Relation.Segment(holder, 0, span),
            Relation.Segment(targetCarrier, targetStart, targetEnd));
        return frame.Project(ReadCompositeOnRoot(holder, levels, highestLevelIndex, radix), targetCarrier);
    }

    private static Relation CreateCarryStep(Carrier lower, Carrier upper, long stepTicks) =>
        new(
            Relation.Segment(RootOrUnknown(lower), 0, stepTicks),
            Relation.Point(RootOrUnknown(upper), 0));

    private static int StepCount(IReadOnlyList<Carrier> levels) =>
        Math.Max(0, CountOf(levels) - 1);

    private static T RepeatLast<T>(IReadOnlyList<T> values, int index)
    {
        if (CountOf(values) == 0)
        {
            return default!;
        }

        return values[Math.Min(index, values.Count - 1)];
    }

    private static bool TryReadLevel(IReadOnlyList<Carrier> levels, int levelIndex, out Carrier level)
    {
        if (levelIndex >= 0 && levelIndex < CountOf(levels))
        {
            level = RootOrUnknown(levels[levelIndex]);
            return true;
        }

        level = default!;
        return false;
    }

    private static int CountOf<T>(IReadOnlyList<T> values) =>
        values?.Count ?? 0;

    private static Carrier RootOrUnknown(Carrier carrier) =>
        carrier ?? Carrier.Unknown;

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

    private static long SaturatingToLong(ulong value) =>
        value > long.MaxValue ? long.MaxValue : (long)value;

    private static long ClampToLong(Int128 value)
    {
        if (value > long.MaxValue)
        {
            return long.MaxValue;
        }

        if (value < long.MinValue)
        {
            return long.MinValue;
        }

        return (long)value;
    }
}
