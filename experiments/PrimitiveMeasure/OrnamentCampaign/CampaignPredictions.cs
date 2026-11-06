using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;

public static partial class CampaignScience
{
    public static CampaignHypothesis[] Hypotheses =>
        (from goal in Goals from family in Families from h in Predictions(goal, family) select h).ToArray();

    private static IEnumerable<CampaignHypothesis> Predictions(string goal, string family)
    {
        var live = new CampaignArm("evolve", WireMode.Live);
        bool regular = goal == "balance" && family == "comb";
        bool axes = goal == "mirror" && family == "reflected";
        CampaignHypothesis H(string mechanism, CampaignArm right, string relation, int margin, string why,
            CampaignArm? left = null) => new($"{goal}.{family}.{mechanism}", mechanism, goal, family, left ?? live,
            right, relation, margin, why);

        yield return H("structure", new("parameter", WireMode.Live), regular ? "lower" : axes ? "equivalent" : "higher",
            20, regular ? "A compact fixed wiring search can cover easy regular-spacing rules more densely." :
            axes ? "Mostly bilateral parents already supply much of the requested geometry." :
            "Changing structural connections should improve held-out utility over parameter-only variation.");
        yield return H("channel", new("open", WireMode.Live), regular || axes ? "equivalent" : "higher", 20,
            regular || axes ? "The common scaffold can make an open-loop pattern competitive." :
            "Inspecting local gaps should help on disturbed or uneven parents.");
        yield return H("selection", new("random", WireMode.Live), regular || axes ? "equivalent" : "higher", 15,
            regular || axes ? "An uncomplicated scaffold should narrow the gain over unguided full-grammar search." :
            "Selection among edits should improve held-out utility at a fixed proposal count.");
        yield return H("cut", new("evolve", WireMode.Cut), axes ? "equivalent" : "higher", 10,
            axes ? "A near-bilateral seed reduces the need for communication." :
            "The native local signal should outperform withholding the wire on the same program.");
        yield return H("freeze", new("evolve", WireMode.Frozen), goal == "seam" ? "equivalent" : "higher", 10,
            goal == "seam" ? "The small boundary neighborhood may be repaired from its initial sample." :
            "Sampling the changed rail should improve response over sampling the first rail repeatedly.");
        yield return H("flip", new("evolve", WireMode.Inverted), axes ? "equivalent" : "higher", 10,
            axes ? "A close initial match leaves little useful directional signal." :
            "The polarity learned on the native channel should matter.");
        bool clockCanFit = family == "comb" && (goal == "balance" || goal == "infill");
        yield return H("clock", new("evolve", WireMode.Clock), clockCanFit ? "equivalent" : "higher", 10,
            clockCanFit ? "A regular scaffold may already coincide with a rhythmic writer." :
            "A fixed clock should lose information about the current bound interval.");
        yield return H("hide", live with { Treatment = "hidden" }, axes ? "equivalent" : "higher", 10,
            axes ? "The reflection brief starts relatively close on this stratum." :
            "Useful local observations should improve the native settlement over concealing all sites.",
            live with { Treatment = "clear" });
        bool stockNeutral = goal == "balance" || axes || (goal == "seam" && family == "doublet");
        yield return H("stock", live with { Treatment = "empty-stock" }, regular ? "lower" :
            stockNeutral ? "equivalent" : "higher", stockNeutral ? 10 : 20,
            regular ? "Extra initial stock can enable unnecessary insertions under a count-preserving brief." :
            stockNeutral ? "Conservative relocation or the initial scaffold can suffice." :
            "Additional insertions should help satisfy the brief where an actual gap remains.");
        yield return H("zero-budget", new("evolve", WireMode.Inverted, "no-edits"), "identical", 0,
            "With no edit attempts both signal interpretations must leave the same material, including any external injection.",
            live with { Treatment = "no-edits" });
    }

    public static CampaignVerdict Classify(CampaignHypothesis h, IEnumerable<CampaignPair> pairs)
    {
        var observations = pairs.ToArray();
        var groupMeans = observations.GroupBy(x => x.Group)
            .Select(g => g.Average(x => x.LeftUtility - x.RightUtility)).ToArray();
        int n = groupMeans.Length;
        bool equivalence = h.Relation is "equivalent" or "identical";
        int positive = groupMeans.Count(d => equivalence ? d > h.Margin : d >= h.Margin);
        int negative = groupMeans.Count(d => equivalence ? d < -h.Margin : d <= -h.Margin);
        int within = n - positive - negative;
        bool adequate(int groups) => n >= 8 && groups * 3 >= n * 2;
        string answer = h.Relation switch
        {
            "identical" when observations.Any(x => !x.SameMaterial || x.LeftUtility != x.RightUtility) => "contrary",
            "identical" when n >= 8 => "supported",
            "equivalent" when adequate(within) => "supported",
            "equivalent" when adequate(positive) || adequate(negative) => "contrary",
            "higher" when adequate(positive) => "supported",
            "higher" when adequate(negative) => "contrary",
            "lower" when adequate(negative) => "supported",
            "lower" when adequate(positive) => "contrary",
            "higher" or "lower" or "equivalent" or "identical" => "inconclusive",
            _ => throw new ArgumentException("Unknown preregistered direction.", nameof(h))
        };
        return new(answer, n, positive, negative, within, n == 0 ? 0 : groupMeans.Average());
    }

    /// <summary>Creates deterministic UTF-8/LF ledger files, or verifies an
    /// identical existing ledger. A changed ledger requires a different folder.</summary>
    public static void WriteManifest(string folder)
    {
        Directory.CreateDirectory(folder);
        var hypotheses = new StringBuilder("id,mechanism,goal,family,left_method,left_wire,left_treatment,right_method,right_wire,right_treatment,relation,margin,rationale\n");
        foreach (var h in Hypotheses)
            Line(hypotheses, h.Id, h.Mechanism, h.Goal, h.Family, h.Left.Method, h.Left.Wire, h.Left.Treatment,
                h.Right.Method, h.Right.Wire, h.Right.Treatment, h.Relation, h.Margin, h.Rationale);
        var episodes = new StringBuilder("split,goal,family,parent,variant,width,seed_hex,initial_hex,protected_hex,target_gap,goal_bits,stock,edit_budget,beats,hidden_hex,intervention_beat,intervention_xor_hex,start\n");
        foreach (var split in Splits)
        foreach (var e in Episodes(split, "mixed"))
        {
            var i = e.Input;
            Line(episodes, split, e.Goal, e.Family, e.Group, e.Variant, i.Width, i.Seed.ToString("X16"),
                i.Initial.ToString("X16"), i.Protected.ToString("X16"), i.TargetGap, i.GoalBits, i.Stock,
                i.EditBudget, i.Beats, i.Hidden.ToString("X16"), i.InterventionBeat, i.InterventionXor.ToString("X16"), i.Start);
        }
        var hashes = new StringBuilder();
        foreach (var (name, contents) in new[] { ("hypotheses.csv", hypotheses.ToString()), ("episodes.csv", episodes.ToString()) })
        {
            byte[] bytes = Encoding.UTF8.GetBytes(contents);
            WriteOnce(Path.Combine(folder, name), bytes);
            hashes.Append(Convert.ToHexString(SHA256.HashData(bytes))).Append("  ").Append(name).Append('\n');
        }
        WriteOnce(Path.Combine(folder, "design.sha256"), Encoding.UTF8.GetBytes(hashes.ToString()));
    }

    private static void Line(StringBuilder text, params object[] fields)
    {
        text.AppendJoin(',', fields.Select(f => "\"" + (Convert.ToString(f, CultureInfo.InvariantCulture) ?? "")
            .Replace("\"", "\"\"") + "\"")).Append('\n');
    }

    private static void WriteOnce(string file, byte[] bytes)
    {
        if (File.Exists(file))
        {
            if (!File.ReadAllBytes(file).AsSpan().SequenceEqual(bytes))
                throw new InvalidOperationException($"Frozen design differs at {file}.");
            return;
        }
        File.WriteAllBytes(file, bytes);
    }
}
