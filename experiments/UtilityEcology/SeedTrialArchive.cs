using CompositeEmergence;
using Gojo.Core;
using Gojo.Structures;
using PrimitiveLearning;

namespace UtilityEcology;

public sealed record SeedArchiveCheck(bool Matched, bool Conserved, bool AliasesMatched,
    int SamplesReplayed, long PaidGateSpent, string Scope, int TrialDecisionsReplayed = 0);

/// <summary>Exact transport and a deliberately narrow retained-anchor fixture.
/// This cannot reconnect new opaque acquisitions to a private instrument.</summary>
public static class SeedTrialArchive
{
    public static SeedArchiveCheck Check(Relation original, Relation restored, bool protectedTrials)
    {
        if (!protectedTrials)
        {
            var old = ArchiveReplay.Check(original, restored);
            return new(old.Matched, old.ContinuedMoneyConserved, true, old.SamplesReplayed, old.AttentionSpent, old.Scope);
        }
        var reference = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(original))[0];
        var left = Replay(reference); var right = Replay(restored);
        return new(left.Admissions && right.Admissions && left.Outputs.SequenceEqual(right.Outputs) && left.Permissions.SequenceEqual(right.Permissions) &&
            left.Spent == right.Spent && left.Money == right.Money, left.Conserved && right.Conserved,
            left.Aliases && right.Aliases, right.Outputs.Length, right.Spent,
            "Saved seed purses and provider/service aliases; completed native admission decisions replayed; retained current/candidate ordinal anchors through an ordered fixture; paid base-gate continuation. No novel acquisition or whole-world resume.", right.Decisions);
    }

    private static (long?[] Outputs, bool[] Permissions, long Spent, long Money, bool Conserved, bool Aliases, bool Admissions, int Decisions) Replay(Relation root)
    {
        var fields = NativeGraph.Parts(root, 9); var economy = ServiceEconomy.FromRoot((Relation)fields[0]);
        var providers = NativeGraph.Parts((Relation)fields[1], 4).Cast<Relation>().Select(SeededProvider.FromRoot).ToArray();
        var outputs = new List<long?>(); var permissions = new List<bool>(); var spent = 0L;
        var aliases = true; var admissions = true; var decisions = 0;
        foreach (var provider in providers)
        {
            var service = economy.Services.Single(service => SeededProvider.FromRoot(service.Body).Base.Gate.Attention.SameIdentityAs(provider.Base.Gate.Attention));
            aliases &= SeededProvider.FromRoot(service.Body).SeedEnergy.SameIdentityAs(provider.SeedEnergy);
            foreach (var trial in provider.TrialHistory.Where(trial => trial.Closed))
            {
                var selection = trial.ReplayAdmission();
                admissions &= SupportDecisionSurface.TryReadSelection(selection, out _, out var response, out _, out _) &&
                    response is CarrierLocation value && value.Ticks == (trial.Accepted ? 1 : 0);
                decisions++;
            }
            if (provider.Base.Gate.Attention.Value < 2 && service.Wallet.Value >= 8) economy.PayWork(service, provider.Base.Gate.Attention, 8, out _);
            var before = checked((long)provider.Base.Gate.Spent.Value);
            var decision = provider.Base.Gate.Decide(WorldPublication.Value(provider.Base.Gate.TrafficAxis, 1, new Carrier()),
                WorldPublication.Value(provider.Base.Gate.DemandAxis, 1, new Carrier()));
            provider.Base.Gate.TryPermission(decision, out var permission); permissions.Add(permission);
            spent += checked((long)provider.Base.Gate.Spent.Value) - before;
            foreach (var model in new[] { provider.Current, provider.Candidate }.Where(model => model is not null))
            {
                AdaptiveOrdinal.TryReadModel(model!.Root, out _, out var landmarks, out _);
                var anchors = NativeGraph.Parts(landmarks).Cast<Relation>().Select(row =>
                { AdaptiveOrdinal.TryReadLandmark(row, out var sample, out _, out _); return sample; }).ToArray();
                Relation Compare(Relation sample, Relation anchor)
                {
                    var a = Array.IndexOf(anchors, sample); var b = Array.IndexOf(anchors, anchor);
                    return a < 0 || b < 0 ? NativeGraph.Open(NativeGraph.G(sample, anchor), provider.Base.OrderAxis) :
                        SamplingSurface.Comparison(new Carrier(), sample, anchor, provider.Base.OrderAxis, a.CompareTo(b));
                }
                foreach (var sample in anchors)
                {
                    var prediction = model.Predict(sample, Compare);
                    outputs.Add(AdaptiveOrdinal.TryReadPrediction(prediction, out var label) ? label.Ticks : null);
                }
            }
            economy.UpdateBody(service, provider.Root, decision);
        }
        return (outputs.ToArray(), permissions.ToArray(), spent, economy.AccountedMoney,
            economy.AccountedMoney == economy.ExternalNet, aliases, admissions, decisions);
    }
}
