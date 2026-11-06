using System.Numerics;

namespace Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;

public readonly record struct CampaignEpisode(string Goal, string Family, string Group, int Variant, RailInput Input);
public readonly record struct CampaignScore(int Utility, int Error, int Violations, bool Exact);
public readonly record struct CampaignArm(string Method, WireMode Wire, string Treatment = "native");
public sealed record CampaignHypothesis(string Id, string Mechanism, string Goal, string Family,
    CampaignArm Left, CampaignArm Right, string Relation, int Margin, string Rationale);
public readonly record struct CampaignPair(string Group, int LeftUtility, int RightUtility, bool SameMaterial = true);
public sealed record CampaignVerdict(string Status, int Groups, int Positive, int Negative, int Within, double MeanDelta);

/// <summary>Offline construction and judging. The target metrics, recipe identifiers,
/// group labels and training outcomes are unavailable to the executing machine.</summary>
public static partial class CampaignScience
{
    public static readonly string[] Goals = ["balance", "infill", "mirror", "seam"];
    public static readonly string[] Families = ["comb", "doublet", "window", "reflected", "terrace", "interleave"];
    public static readonly string[] Splits = ["train", "validation", "test", "reserve"];

    public static CampaignScore Evaluate(CampaignEpisode episode, RailResult result, RailInput? executed = null)
    {
        var input = episode.Input;
        var physical = executed ?? input;
        var width = input.Width;
        var valid = (1UL << width) - 1;
        var ink = result.Ink & valid;
        var n = BitOperations.PopCount(ink);
        // A disturbance is a separately supplied physical event. All generated
        // interventions avoid protected sites, so their original state must hold.
        var violations = BitOperations.PopCount((result.Ink ^ input.Initial) & input.Protected)
            + BitOperations.PopCount(result.Ink & ~valid);
        var incoming = BitOperations.PopCount(physical.Initial) + physical.Stock + result.ExternalDelta;
        if (incoming != n + result.Stock) violations++;
        if (result.Stock < 0 || result.Edits < 0 || result.Attempts < result.Edits ||
            result.Attempts > physical.EditBudget || result.Attempts > physical.Beats) violations++;
        var error = episode.Goal switch
        {
            "balance" => BalanceError(ink, width, n, BitOperations.PopCount(input.Initial)),
            "infill" => InfillError(ink, width, n, input.TargetGap),
            "mirror" => MirrorError(ink, width, input.Initial),
            "seam" => SeamError(ink, width, n, input.TargetGap, input.Initial),
            _ => throw new ArgumentOutOfRangeException(nameof(episode))
        };
        var utility = violations > 0 ? 0 : Math.Clamp(1000 - error, 0, 1000);
        return new(utility, error, violations, violations == 0 && error == 0);
    }

    private static int BalanceError(ulong ink, int width, int count, int originalCount)
    {
        if (count < 2) return 1000;
        var total = GapDeviation(ink, width, count, width);
        int remainder = width % count;
        // This much deviation is unavoidable when width is indivisible by count.
        var unavoidable = 2 * remainder * (count - remainder);
        return 180 * Math.Abs(count - originalCount) + Scale(total - unavoidable, 2 * width * count);
    }

    private static int InfillError(ulong ink, int width, int count, int step)
    {
        if (count < 2) return 1000;
        int remainder = width % step;
        return Scale(GapDeviation(ink, width, 1, step) - Math.Min(remainder, step - remainder), width);
    }

    // Sum |scale * gap - target| over successive occupied sites, including wrap.
    private static int GapDeviation(ulong ink, int width, int scale, int target)
    {
        int first = BitOperations.TrailingZeroCount(ink);
        int last = first, total = 0;
        for (var remaining = ink & (ink - 1); remaining != 0; remaining &= remaining - 1)
        {
            int next = BitOperations.TrailingZeroCount(remaining);
            total += Math.Abs(scale * (next - last) - target);
            last = next;
        }
        return total + Math.Abs(scale * (width + first - last) - target);
    }

    private static int MirrorError(ulong ink, int width, ulong initial)
    {
        // Complete precisely the reflected source, keeping every original ink
        // site and its intervening ground. Ink outside this completion is an error.
        ulong permitted = initial | Reflect(initial, width);
        return Scale(BitOperations.PopCount(ink ^ permitted), Math.Max(1, BitOperations.PopCount(permitted)));
    }

    internal static ulong Reflect(ulong ink, int width)
    {
        ulong result = 0;
        for (var remaining = ink; remaining != 0; remaining &= remaining - 1)
            result |= 1UL << (width - 1 - BitOperations.TrailingZeroCount(remaining));
        return result;
    }

    private static int SeamError(ulong ink, int width, int count, int step, ulong initial)
    {
        if (count < 2) return 1000;
        int first = BitOperations.TrailingZeroCount(ink);
        int last = 63 - BitOperations.LeadingZeroCount(ink);
        int gap = width + first - last;
        int edge = Math.Min(step + 1, width / 2);
        ulong endSites = ((1UL << edge) - 1) | (((1UL << edge) - 1) << (width - edge));
        // Private detail away from the fixed repeat boundary is a preservation
        // obligation. A repair may edit either participating edge neighborhood.
        int interiorChanges = BitOperations.PopCount((initial ^ ink) & ~endSites);
        return Scale(Math.Abs(gap - step), Math.Max(step, width / 3)) + 100 * interiorChanges;
    }

    private static int Scale(int numerator, int denominator) =>
        (1000 * Math.Max(numerator, 0) + denominator / 2) / denominator;

    public static RailInput ArmInput(CampaignEpisode episode, CampaignArm arm) => arm.Treatment switch
    {
        "native" => episode.Input,
        "clear" => episode.Input with { Hidden = 0 },
        "hidden" => episode.Input with { Hidden = (1UL << episode.Input.Width) - 1 },
        "empty-stock" => episode.Input with { Stock = 0 },
        "no-edits" => episode.Input with { EditBudget = 0 },
        _ => throw new ArgumentException("Unknown treatment.", nameof(arm))
    };
}
