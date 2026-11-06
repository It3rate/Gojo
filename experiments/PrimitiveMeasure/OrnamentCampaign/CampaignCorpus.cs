using System.Numerics;

namespace Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;

public static partial class CampaignScience
{
    private static readonly Lazy<CampaignEpisode[]> Corpus = new(BuildCorpus);

    public static CampaignEpisode[] Episodes(string split, string goal)
    {
        if (!Splits.Contains(split)) throw new ArgumentException("Unknown partition.", nameof(split));
        if (goal != "mixed" && !Goals.Contains(goal)) throw new ArgumentException("Unknown brief.", nameof(goal));
        return Corpus.Value.Where(x => x.Group.StartsWith(split + "-", StringComparison.Ordinal)
            && (goal == "mixed" || x.Goal == goal)).ToArray();
    }

    private static CampaignEpisode[] BuildCorpus()
    {
        var episodes = new List<CampaignEpisode>();
        var distinct = new HashSet<(int Width, ulong Shape)>();
        int[] parentCounts = [16, 8, 12, 8];
        for (int split = 0; split < Splits.Length; split++)
        for (int family = 0; family < Families.Length; family++)
        for (int group = 0; group < parentCounts[split]; group++)
        {
            var random = new RecipeRandom((ulong)(1 + split * 10000 + family * 100 + group));
            int width, target;
            ulong seed;
            int attempts = 0;
            do
            {
                if (++attempts > 4096) throw new InvalidOperationException("Recipe space exhausted.");
                width = 22 + random.Next(15);
                target = 2 + random.Next(4);
                seed = FamilyRail(family, width, target, ref random);
                seed = ModifyByPartition(seed, width, target, split, ref random);
            } while (BitOperations.PopCount(seed) < 4 || BitOperations.PopCount(seed) > width - 5 ||
                !distinct.Add((width, Canonical(seed, width))));

            ulong rail = (1UL << width) - 1;
            ulong protect = group % 4 == 0 ? 0 : Pick(seed, ref random);
            if (group % 3 == 0)
            {
                ulong white = Pick(rail & ~seed, ref random);
                protect |= white | (Rotate(white, width, 1) & ~seed);
            }
            ulong hidden = group % 4 == 2 ? Pick(rail, ref random) : 0;
            int start = random.Next(width);
            ulong disturbance = Pick(rail & ~protect, ref random);
            int stock = 2 + random.Next(4);
            int budget = 4 + width / 4;
            string id = $"{Splits[split]}-{Families[family]}-{group:D2}";
            for (int variant = 0; variant < 2; variant++)
            {
                bool mirrored = variant == 1;
                ulong ink = mirrored ? Reflect(seed, width) : seed;
                // The same physical parent serves all briefs. A goal-specific
                // target artwork is never present in Seed, Initial or Protected.
                for (int g = 0; g < Goals.Length; g++)
                {
                    var input = new RailInput(width, ink, ink, mirrored ? Reflect(protect, width) : protect,
                        target, (byte)(1 << g), stock, budget, 2 * width + 3,
                        Hidden: mirrored ? Reflect(hidden, width) : hidden,
                        InterventionBeat: mirrored ? width : -1,
                        InterventionXor: mirrored ? Reflect(disturbance, width) : 0,
                        Start: mirrored ? width - 1 - start : start);
                    episodes.Add(new(Goals[g], Families[family], id, variant, input));
                }
            }
        }
        return episodes.ToArray();
    }

    private static ulong FamilyRail(int family, int width, int target, ref RecipeRandom random)
    {
        int phase = random.Next(width);
        int period = target + random.Next(3), otherPeriod = 3 + random.Next(5);
        int offset = random.Next(otherPeriod);
        ulong seed = 0;
        if (family == 4)
        {
            int x = 0, stage = random.Next(4);
            while (x < width)
            {
                seed |= 1UL << x;
                x += 1 + (stage++ % 4);
            }
        }
        else
        {
            for (int i = 0; i < width; i++)
            {
                bool mark = family switch
                {
                    0 => i % period == 0,
                    1 => i % (period + 2) is 0 or 1,
                    2 => (i < width / 3 || i >= 2 * width / 3) && i % Math.Max(2, period - 2) == 0,
                    3 => i < (width + 1) / 2 && ((i + offset) % period == 0 || i % otherPeriod == 1),
                    5 => i % period == 0 || (i + offset) % otherPeriod == 0,
                    _ => throw new InvalidOperationException("Unknown rail family.")
                };
                if (mark) seed |= 1UL << i;
            }
            if (family == 3) seed |= Reflect(seed, width);
        }
        return family == 3 ? seed : Rotate(seed, width, phase);
    }

    // Distinct recipe combinations belong to distinct partitions. The stored
    // seed is the construction *after* these changes, not its pristine parent.
    private static ulong ModifyByPartition(ulong seed, int width, int target, int split, ref RecipeRandom random)
    {
        ulong rail = (1UL << width) - 1;
        ulong ink = Pick(seed, ref random), empty = Pick(rail & ~seed, ref random);
        return split switch
        {
            0 => random.Next(2) == 0 ? (seed & ~ink) | Rotate(ink, width, 1) : seed | empty,
            1 => (seed & ~ink) | Rotate(ink, width, 2) | empty,
            2 => (seed & ~ink) | empty | Rotate(empty, width, target),
            3 => (seed & ~ink & ~Rotate(ink, width, target)) | empty | Rotate(empty, width, 1),
            _ => throw new InvalidOperationException("Unknown modification recipe.")
        };
    }

    private static ulong Pick(ulong mask, ref RecipeRandom random)
    {
        if (mask == 0) return 0;
        int choice = random.Next(BitOperations.PopCount(mask));
        for (int i = 0; i < choice; i++) mask &= mask - 1;
        return mask & unchecked(0UL - mask);
    }

    private static ulong Rotate(ulong bits, int width, int shift)
    {
        shift %= width;
        return ((bits << shift) | (bits >> (width - shift))) & ((1UL << width) - 1);
    }

    // Reject rotational and reflected copies even when recipe IDs differ.
    private static ulong Canonical(ulong bits, int width)
    {
        ulong reversed = Reflect(bits, width), minimum = bits;
        for (int i = 0; i < width; i++)
            minimum = Math.Min(minimum, Math.Min(Rotate(bits, width, i), Rotate(reversed, width, i)));
        return minimum;
    }

    private struct RecipeRandom(ulong state)
    {
        public int Next(int bound)
        {
            ulong z = unchecked(state += 0x9E3779B97F4A7C15UL);
            z = unchecked((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL);
            z = unchecked((z ^ (z >> 27)) * 0x94D049BB133111EBUL);
            return (int)((z ^ (z >> 31)) % (uint)bound);
        }
    }
}
