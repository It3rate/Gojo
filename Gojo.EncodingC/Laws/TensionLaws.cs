namespace Gojo.EncodingC;

/// <summary>Small stock tables for reading and responding to tension inside a
/// running machine. Every member returns an ordinary <see cref="Law"/> table
/// with checked accounting; nothing outside knows these laws by name.
/// <para>All tables follow canonical prompt §9.7: 1 is presence, 0 is absence.
/// Folding laws (<see cref="SupportFold"/>, <see cref="MismatchFold"/>) work
/// bit by bit so a structure scales across up to 64 values without building an
/// exponential truth table.</para></summary>
public static class TensionLaws
{
    private static readonly Dictionary<int, RelationLaw> Gaps = [];
    private static readonly Dictionary<(int, bool), TransitionLaw> Streaks = [];
    private static readonly Dictionary<(int, int), TransitionLaw> Recruits = [];
    private static readonly Dictionary<(int, int), TransitionLaw> Retires = [];
    private static readonly Dictionary<int, TransitionLaw> Firmnesses = [];

    /// <summary>Resets two scratch bits to zero before a fold pass. Roles:
    /// bit 0, bit 1.</summary>
    public static TransitionLaw Clear2 { get; } = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit],
        _ => [0, 0],
        new Accounting(true));

    /// <summary>Folds one support plane (<c>1</c> = value still possible) into
    /// two presence planes <c>(seen1, seen2)</c>:
    /// <list type="bullet">
    /// <item><c>(0, 0)</c>: no value possible (contradiction);</item>
    /// <item><c>(1, 0)</c>: exactly one value possible (settled);</item>
    /// <item><c>(1, 1)</c>: at least two values possible (open).</item>
    /// </list>
    /// Roles: possible bit (read), seen1 (written), seen2 (written). Works for
    /// any support width without an exponential table.</summary>
    public static TransitionLaw SupportFold { get; } = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit, Shape.Bit],
        s => [s[0], s[1] | s[0], s[2] | (s[1] & s[0])],
        new Accounting(true, new Unchanged(0), new NeverDecreases(1), new NeverDecreases(2)));

    /// <summary>Turns <c>(demand, seen1, seen2)</c> into demanded information
    /// tension: <c>deficit = demand &amp; seen2</c> (open where settledness is
    /// demanded) and <c>excess = demand &amp; ~seen1</c> (contradicted where
    /// demanded). When <c>demand = 0</c>, both outputs are 0: openness without
    /// demand creates no tension. Roles: demand, seen1, seen2, deficit, excess.</summary>
    public static TransitionLaw DemandedStatus { get; } = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit],
        s => [s[0], s[1], s[2], s[0] & s[2], s[0] & (1 - s[1])],
        new Accounting(true, new Unchanged(0), new Unchanged(1), new Unchanged(2)));

    /// <summary>Folds one bit pair of two unary counts (<c>have</c> and
    /// <c>want</c>) into two directional presence bits:
    /// <list type="bullet">
    /// <item><c>(0, 0)</c>: matched so far;</item>
    /// <item><c>(1, 0)</c>: <c>have &lt; want</c> (need more);</item>
    /// <item><c>(0, 1)</c>: <c>have &gt; want</c> (have extra);</item>
    /// <item><c>(1, 1)</c>: broken unary code (both higher and lower bits disagree).</item>
    /// </list>
    /// Roles: have bit, want bit, needMore, haveExtra. Scales to any unary
    /// capacity up to 63.</summary>
    public static TransitionLaw MismatchFold { get; } = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit],
        s => [s[0], s[1], s[2] | (s[1] & (1 - s[0])), s[3] | (s[0] & (1 - s[1]))],
        new Accounting(true, new Unchanged(0), new Unchanged(1), new NeverDecreases(2), new NeverDecreases(3)));

    /// <summary>Two-faced unary gap relation over <c>(have, want, deficit, excess)</c>:
    /// <c>deficit = max(0, want - have)</c> and <c>excess = max(0, have - want)</c>.
    /// Both faces are valid unary prefixes and never both nonzero. Because it is
    /// a relation, it also narrows open supports in either direction.</summary>
    public static RelationLaw Gap(int capacity)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        var size = (long)(capacity + 1);
        if (size * size * size * size > Law.MaxBits)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Two-faced gap table exceeds MaxBits; use MismatchFold for wider counts.");
        lock (Gaps)
        {
            if (Gaps.TryGetValue(capacity, out var cached)) return cached;
            var unary = Shape.Unary(capacity);
            return Gaps[capacity] = RelationLaw.FromPredicate(
                [unary, unary, unary, unary],
                t => t[2] == (t[1] > t[0] ? t[1] - t[0] : 0) && t[3] == (t[0] > t[1] ? t[0] - t[1] : 0));
        }
    }

    /// <summary>Flips a state bit whenever a trigger bit is set, such as
    /// flipping a turn-taking gate on a contest or after a move. Roles:
    /// trigger (read), state (written).</summary>
    public static TransitionLaw ToggleWhen { get; } = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit],
        s => [s[0], s[0] == 1 ? 1 - s[1] : s[1]],
        new Accounting(true, new Unchanged(0)));

    /// <summary>Yields a turn-taking gate when the current holder did not move
    /// (<c>activeChanged = 0</c>) while the waiting side has unmet demand
    /// (<c>waitingNeed = 1</c>). Prevents starvation without a global scheduler.
    /// Roles: activeChanged, waitingNeed, turn.</summary>
    public static TransitionLaw YieldTurn { get; } = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit, Shape.Bit],
        s => [s[0], s[1], s[0] == 0 && s[1] == 1 ? 1 - s[2] : s[2]],
        new Accounting(true, new Unchanged(0), new Unchanged(1)));

    /// <summary>Breaks a contested tie between two open gates and alternates
    /// turns while work remains under <see cref="Engine.StepTogether"/>:
    /// when <c>contested = 1</c> and both gates are open <c>(1, 1)</c>, closes
    /// gate B to <c>(1, 0)</c>; when <c>active = 1</c> and gates hold one turn
    /// <c>(1, 0)</c> or <c>(0, 1)</c>, swaps them so each taker receives one
    /// unit per turn without double-firing; when <c>active = 0</c>, holds still
    /// so the machine settles quietly. Roles: contested, active, gateA, gateB.</summary>
    public static TransitionLaw TurnStep { get; } = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit],
        s =>
        {
            if (s[0] == 1 && s[2] == 1 && s[3] == 1) return [s[0], s[1], 1, 0];
            if (s[1] == 1 && s[2] != s[3]) return [s[0], s[1], s[3], s[2]];
            return [s[0], s[1], s[2], s[3]];
        },
        new Accounting(true, new Unchanged(0), new Unchanged(1)));

    /// <summary>Counts consecutive steps where a signal matches
    /// <paramref name="countWhenSet"/>, up to <paramref name="capacity"/>, and
    /// resets to 0 otherwise. With <c>countWhenSet = true</c>, tracks persistent
    /// tension; with <c>countWhenSet = false</c> on <c>Evidence.Changed</c>,
    /// tracks idle rows. Roles: signal (read), unary streak (written).</summary>
    public static TransitionLaw Streak(int capacity, bool countWhenSet = true)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        lock (Streaks)
        {
            var key = (capacity, countWhenSet);
            if (Streaks.TryGetValue(key, out var cached)) return cached;
            var match = countWhenSet ? 1 : 0;
            return Streaks[key] = TransitionLaw.FromFunction(
                [Shape.Bit, Shape.Unary(capacity)],
                s => [s[0], s[0] == match ? (s[1] < capacity ? s[1] + 1 : capacity) : 0],
                new Accounting(true, new Unchanged(0)));
        }
    }

    /// <summary>Sets a life or gate bit once a unary counter reaches
    /// <paramref name="threshold"/>. Used on the slow clock to recruit a spare
    /// route when persistent tension saturates. Roles: counter (read), life (written).</summary>
    public static TransitionLaw RecruitAt(int capacity, int threshold)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (threshold < 1 || threshold > capacity) throw new ArgumentOutOfRangeException(nameof(threshold));
        lock (Recruits)
        {
            var key = (capacity, threshold);
            if (Recruits.TryGetValue(key, out var cached)) return cached;
            return Recruits[key] = TransitionLaw.FromFunction(
                [Shape.Unary(capacity), Shape.Bit],
                s => [s[0], s[0] >= threshold ? 1 : s[1]],
                new Accounting(true, new Unchanged(0), new NeverDecreases(1)));
        }
    }

    /// <summary>Clears a life or gate bit once a unary counter reaches
    /// <paramref name="threshold"/>. Used on the slow clock to retire an idle
    /// row or low-trust route. Roles: counter (read), life (written).</summary>
    public static TransitionLaw RetireAt(int capacity, int threshold)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (threshold < 1 || threshold > capacity) throw new ArgumentOutOfRangeException(nameof(threshold));
        lock (Retires)
        {
            var key = (capacity, threshold);
            if (Retires.TryGetValue(key, out var cached)) return cached;
            return Retires[key] = TransitionLaw.FromFunction(
                [Shape.Unary(capacity), Shape.Bit],
                s => [s[0], s[0] >= threshold ? 0 : s[1]],
                new Accounting(true, new Unchanged(0), new NeverIncreases(1)));
        }
    }

    /// <summary>Local conflict resolution by firmness (AGM entrenchment). When
    /// <c>contradiction = 1</c> and <c>myFirmness &lt; otherFirmness</c>, clears
    /// <c>active</c> (retiring or opening the weaker participant). Equal
    /// firmness leaves <c>active</c> untouched so genuine ties stay visible.
    /// Roles: contradiction, myFirmness, otherFirmness, active.</summary>
    public static TransitionLaw YieldByFirmness(int firmnessCapacity = 3)
    {
        if (firmnessCapacity is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(firmnessCapacity));
        lock (Firmnesses)
        {
            if (Firmnesses.TryGetValue(firmnessCapacity, out var cached)) return cached;
            var firmness = Shape.Unary(firmnessCapacity);
            return Firmnesses[firmnessCapacity] = TransitionLaw.FromFunction(
                [Shape.Bit, firmness, firmness, Shape.Bit],
                s => [s[0], s[1], s[2], s[0] == 1 && s[1] < s[2] ? 0 : s[3]],
                new Accounting(true, new Unchanged(0), new Unchanged(1), new Unchanged(2), new NeverIncreases(3)));
        }
    }
}
