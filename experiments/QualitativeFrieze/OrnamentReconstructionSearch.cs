using System.Numerics;
using Gojo.Core;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Evaluator-side finite message search. The goal mask belongs to the
/// judge, never the receiver. Exhaustive minimality is conditional on this pool,
/// native cost and advertised repertoire; it is not learned discovery.</summary>
public static class OrnamentReconstructionSearch
{
    public sealed record Option(int Mask, int Hints, int NativeCost, BigInteger Admitted, BigInteger Possible, bool Robust);
    public sealed record Result(IReadOnlyList<Option> Options, Option Best, int GreedyMask, int Enumerated,
        bool HasRobust, double Milliseconds);

    public static Result Run(IReadOnlyList<R.Claim> pool, IReadOnlyList<BigInteger> filters, IReadOnlyList<BigInteger> possibleFilters,
        BigInteger universe, BigInteger goal, IReadOnlyList<string> focusProfiles)
    {
        if (pool.Count > 12 || filters.Count != pool.Count || possibleFilters.Count != pool.Count)
            throw new ArgumentException("Finite exhaustive search admits at most twelve actual claims.");
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var count = 1 << pool.Count; var masks = new BigInteger[count]; var possible = new BigInteger[count]; var options = new List<Option>(count);
        masks[0] = universe; possible[0] = universe;
        // Query definitions are shared across subsets. Count their actual union,
        // then the closed-message spine. This is exactly Message.NativeCost;
        // constructing and parsing every candidate message would add no evidence.
        var participants = pool.Select(claim => OrnamentReconstructionPacket.Participants(claim.Surface)).ToArray();
        var identity = new Dictionary<IElement, int>(ReferenceEqualityComparer.Instance);
        foreach (var group in participants) foreach (var item in group)
            if (!identity.ContainsKey(item)) identity.Add(item, identity.Count);
        var membership = new int[identity.Count];
        for (var index = 0; index < participants.Length; index++) foreach (var item in participants[index])
            membership[identity[item]] |= 1 << index;
        var frequency = new int[count]; foreach (var bits in membership) frequency[bits]++;
        var subset = (int[])frequency.Clone();
        for (var bit = 0; bit < pool.Count; bit++) for (var mask = 0; mask < count; mask++)
            if ((mask & 1 << bit) != 0) subset[mask] += subset[mask ^ (1 << bit)];
        for (var mask = 0; mask < count; mask++)
        {
            if (mask != 0)
            {
                var lowest = mask & -mask; var index = BitOperations.TrailingZeroCount((uint)lowest);
                masks[mask] = masks[mask ^ lowest] & filters[index];
                possible[mask] = possible[mask ^ lowest] & possibleFilters[index];
            }
            var hints = BitOperations.PopCount((uint)mask);
            // Closed(empty) has one shared seam and one relation; each
            // entry adds one ordered relation. Claim/query internals are shared.
            var cost = identity.Count - subset[(count - 1) ^ mask] + hints + 2;
            var robust = masks[mask] != BigInteger.Zero && (possible[mask] & ~goal) == BigInteger.Zero;
            options.Add(new(mask, hints, cost, masks[mask], possible[mask], robust));
        }
        var robustOptions = options.Where(option => option.Robust).ToArray();
        var best = robustOptions.OrderBy(option => option.NativeCost).ThenBy(option => option.Hints).ThenBy(option => option.Mask).FirstOrDefault()
            ?? options.OrderBy(option => Count(option.Admitted & ~goal)).ThenByDescending(option => Count(option.Admitted & goal))
                .ThenBy(option => option.NativeCost).ThenBy(option => option.Mask).First();
        var greedy = 0; var current = universe;
        while (current != BigInteger.Zero && !Uniform(current, focusProfiles) && greedy != count - 1)
        {
            var next = Enumerable.Range(0, pool.Count).Where(index => (greedy & 1 << index) == 0)
                .Select(index => new { Index = index, Remaining = current & possibleFilters[index],
                    Gain = Count(current) - Count(current & possibleFilters[index]),
                    Cost = Math.Max(1, options[greedy | 1 << index].NativeCost - options[greedy].NativeCost) })
                .OrderByDescending(item => (double)item.Gain / item.Cost).ThenBy(item => item.Cost).ThenBy(item => item.Index).First();
            if (next.Gain == 0) break;
            greedy |= 1 << next.Index; current = next.Remaining;
        }
        watch.Stop();
        return new(options, best, greedy, count, robustOptions.Length != 0, watch.Elapsed.TotalMilliseconds);
    }

    public static bool Uniform(BigInteger mask, IReadOnlyList<string> profiles)
    {
        string? first = null;
        for (var index = 0; index < profiles.Count; index++) if ((mask & (BigInteger.One << index)) != 0)
        {
            var profile = profiles[index];
            if (profile.IndexOfAny(['?', '!']) >= 0) return false;
            first ??= profile; if (first != profile) return false;
        }
        return first is not null;
    }

    public static R.Message Message(IReadOnlyList<R.Claim> pool, int mask) =>
        new(pool.Where((_, index) => (mask & 1 << index) != 0).ToArray());

    public static int Count(BigInteger mask)
    {
        var result = 0; while (mask > BigInteger.Zero) { mask &= mask - BigInteger.One; result++; } return result;
    }
}
