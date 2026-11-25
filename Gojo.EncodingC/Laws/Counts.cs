namespace Gojo.EncodingC;

/// <summary>Counting as laws: the few arithmetic reads tension needs, each a
/// small transition table that reads unary counts and writes one result.
/// <para><b>Why laws.</b> What a sum, a shortfall or a threshold means is then
/// held in a table: saved with the machine, inspectable, and replaceable by
/// structure. A view that computed the same thing in host code on every read
/// could not be inspected or changed by anything in the structure.</para>
/// <para><b>Emulating numbers.</b> Pre-numeric Gojo has no magnitudes, so each
/// law enumerates its counts. These are the primitives numeric support will
/// replace with arithmetic on magnitudes, keeping the same roles: sum and
/// streak are accumulation, at-least is a threshold read with the threshold
/// held as data, tally counts bits, and shortfall is one face of a gap. Tables
/// grow with capacity, which bounds the counts they can read today.</para>
/// <para><b>How they run.</b> Each writes its result only when it changes, so a
/// structure settles quietly around them, and each result has one writer, so
/// they never contend. Their inputs are read and unchanged. Declare them before
/// what reads their results, so a step in order sees a fresh value.</para></summary>
public static class Counts
{
    private static readonly Dictionary<(int, int, int), TransitionLaw> Sums = [];
    private static readonly Dictionary<(int, int), TransitionLaw> Shortfalls = [];
    private static readonly Dictionary<int, TransitionLaw> Tallies = [];
    private static readonly Dictionary<(int, int), TransitionLaw> Thresholds = [];
    private static readonly Dictionary<(int, bool), TransitionLaw> Streaks = [];

    /// <summary>Accumulation: a + weight · b, as a count of capacity
    /// a + weight · b. Roles: a, b (read), total (written). A weight states a
    /// rate, such as items per case on order.</summary>
    public static TransitionLaw Sum(int capacityA, int capacityB, int weight = 1)
    {
        if (weight < 1) throw new ArgumentOutOfRangeException(nameof(weight));
        lock (Sums)
        {
            var key = (capacityA, capacityB, weight);
            if (Sums.TryGetValue(key, out var cached)) return cached;
            return Sums[key] = Reading([Shape.Unary(capacityA), Shape.Unary(capacityB)], Shape.Unary(capacityA + weight * capacityB),
                s => s[0] + weight * s[1]);
        }
    }

    /// <summary>One face of a gap: how far have is below want, never negative.
    /// Roles: have, want (read), shortfall (written). The other face, the
    /// excess, is the same law with have and want swapped.</summary>
    public static TransitionLaw Shortfall(int haveCapacity, int wantCapacity)
    {
        lock (Shortfalls)
        {
            var key = (haveCapacity, wantCapacity);
            if (Shortfalls.TryGetValue(key, out var cached)) return cached;
            return Shortfalls[key] = Reading([Shape.Unary(haveCapacity), Shape.Unary(wantCapacity)], Shape.Unary(wantCapacity),
                s => s[1] > s[0] ? s[1] - s[0] : 0);
        }
    }

    /// <summary>How many of <paramref name="bits"/> bits, one to seven, are set, as a count.
    /// Roles: the bits (read), the count (written). Over a value's support
    /// planes it is how many values are still possible: its thermometer bit 0
    /// is "any possible", bit 1 "more than one", so settled is bit 0 without
    /// bit 1 and contradicted is no bit 0.</summary>
    public static TransitionLaw Tally(int bits)
    {
        if (bits is < 1 or > 7) throw new ArgumentOutOfRangeException(nameof(bits), "A tally reads one to seven bits; eight would exceed Law.MaxBits.");
        lock (Tallies)
        {
            if (Tallies.TryGetValue(bits, out var cached)) return cached;
            return Tallies[bits] = Reading([.. Enumerable.Repeat(Shape.Bit, bits)], Shape.Unary(bits), s => s.Sum());
        }
    }

    /// <summary>A threshold read: 1 when a count is at least a threshold that
    /// is itself a count in the planes, so a law, a learner or a host can move
    /// it. Roles: count, threshold (read), result (written).</summary>
    public static TransitionLaw AtLeast(int countCapacity, int thresholdCapacity)
    {
        lock (Thresholds)
        {
            var key = (countCapacity, thresholdCapacity);
            if (Thresholds.TryGetValue(key, out var cached)) return cached;
            return Thresholds[key] = Reading([Shape.Unary(countCapacity), Shape.Unary(thresholdCapacity)], Shape.Bit,
                s => s[0] >= s[1] ? 1 : 0);
        }
    }

    /// <summary>Accumulation: one more each step a signal bit is 1, up to
    /// capacity, and otherwise held. A shop placing an order each step it is
    /// short of its reorder point is this. Roles: signal (read), count (written).</summary>
    public static TransitionLaw Accumulate(int capacity) => Counting(capacity, reset: false);

    /// <summary>Accumulation that resets: one more each step a signal bit is 1,
    /// up to capacity, and back to 0 the first step it is 0. Read through a
    /// threshold, it is "this has held for a while", the slow clock's trigger.
    /// Roles: signal (read), streak (written).</summary>
    public static TransitionLaw Streak(int capacity) => Counting(capacity, reset: true);

    private static TransitionLaw Counting(int capacity, bool reset)
    {
        lock (Streaks)
        {
            if (Streaks.TryGetValue((capacity, reset), out var cached)) return cached;
            return Streaks[(capacity, reset)] = TransitionLaw.FromFunction([Shape.Bit, Shape.Unary(capacity)],
                s => [s[0], s[0] == 1 ? (s[1] < capacity ? s[1] + 1 : capacity) : reset ? 0 : s[1]],
                new Accounting(true, new Unchanged(0)));
        }
    }

    /// <summary>Ranking, as a law (from tension_neon). Each tier of tension is
    /// compared before and after a step as two bits, dropped and rose, such as
    /// the two faces of a shortfall taken each way round. Of two tiers, the
    /// higher decides when it changed at all, and otherwise the lower does: no
    /// change in a lower tier outweighs any change in a higher one (strict
    /// domination). Roles: higher dropped, higher rose, lower dropped, lower
    /// rose (read), then dropped and rose overall (written). Chained, it ranks
    /// any number of tiers; a step is admissible when overall it did not rise.</summary>
    public static TransitionLaw Rank { get; } = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit],
        s => (s[0] | s[1]) != 0 ? [s[0], s[1], s[2], s[3], s[0], s[1]] : [s[0], s[1], s[2], s[3], s[2], s[3]],
        new Accounting(true, new Unchanged(0), new Unchanged(1), new Unchanged(2), new Unchanged(3)));

    /// <summary>A law that reads its inputs and writes one result computed from
    /// them, leaving the inputs unchanged.</summary>
    private static TransitionLaw Reading(Shape[] inputs, Shape result, Func<int[], int> read) =>
        TransitionLaw.FromFunction([.. inputs, result],
            s => [.. s[..inputs.Length], read(s[..inputs.Length])],
            new Accounting(true, [.. Enumerable.Range(0, inputs.Length).Select(r => (Invariant)new Unchanged(r))]));
}
