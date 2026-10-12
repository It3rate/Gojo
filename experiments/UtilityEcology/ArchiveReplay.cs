using CompositeEmergence;
using Gojo.Core;
using Gojo.Structures;
using PrimitiveLearning;

namespace UtilityEcology;

public sealed record ArchiveBehaviorCheck(bool Matched, int SamplesReplayed,
    bool ContinuedMoneyConserved, long AttentionSpent, string Scope);

/// <summary>Verification harness only. The fixture reconnects retained acquired
/// anchors in their learned order, without reconstructing private world metrics.
/// It verifies known-anchor publications and a subsequent paid gate decision.
/// It does not supply new training labels or resume novel world sampling.</summary>
public static class ArchiveReplay
{
    public static ArchiveBehaviorCheck Check(Relation original, Relation restored)
    {
        var reference = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(original))[0];
        var left = Replay(reference); var right = Replay(restored);
        return new(left.Publications.SequenceEqual(right.Publications) &&
            left.Permissions.SequenceEqual(right.Permissions) && left.Spent == right.Spent &&
            left.Money == right.Money, left.Publications.Length, left.Conserved && right.Conserved,
            right.Spent, "Retained ordinal anchors or fresh presence-only requests; reconnected ordered fixture; paid gate continuation. No novel-sample world reconnection.");
    }

    private static (long?[] Publications, bool[] Permissions, long Spent, long Money, bool Conserved) Replay(Relation root)
    {
        var fields = NativeGraph.Parts(root, 9);
        var economy = ServiceEconomy.FromRoot((Relation)fields[0]);
        var providers = NativeGraph.Parts((Relation)fields[1], 4).Cast<Relation>().Select(InterpretationProvider.FromRoot).ToArray();
        var outputs = new List<long?>(); var permissions = new List<bool>(); var spent = 0L;
        foreach (var provider in providers)
        {
            var service = economy.Services.Single(service => InterpretationProvider.FromRoot(service.Body).Gate.Attention.SameIdentityAs(provider.Gate.Attention));
            if (provider.Gate.Attention.Value < 2)
            {
                if (service.Wallet.Value < 8) economy.Sponsor(service, 8);
                economy.PayWork(service, provider.Gate.Attention, 8, out _);
            }
            var before = checked((long)provider.Gate.Spent.Value);
            var decision = provider.Gate.Decide(
                WorldPublication.Value(provider.Gate.TrafficAxis, 1, new Carrier()),
                WorldPublication.Value(provider.Gate.DemandAxis, 1, new Carrier()));
            provider.Gate.TryPermission(decision, out var allowed); permissions.Add(allowed);
            spent += checked((long)provider.Gate.Spent.Value) - before;

            Relation[] anchors = [];
            if (provider.OrdinalModel is { } ordinal && AdaptiveOrdinal.TryReadModel(ordinal.Root, out _, out var landmarks, out _))
                anchors = NativeGraph.Parts(landmarks).Cast<Relation>().Select(landmark =>
                { AdaptiveOrdinal.TryReadLandmark(landmark, out var sample, out _, out _); return sample; }).ToArray();
            var samples = anchors.Length > 0 ? anchors : [SamplingSurface.Sample(new Carrier(), provider.Source, provider.PresenceAxis, true)];
            Relation Compare(Relation sample, Relation anchor)
            {
                var a = Array.IndexOf(anchors, sample); var b = Array.IndexOf(anchors, anchor);
                return a < 0 || b < 0 ? NativeGraph.Open(NativeGraph.G(sample, anchor), provider.OrderAxis) :
                    SamplingSurface.Comparison(new Carrier(), sample, anchor, provider.OrderAxis, a.CompareTo(b));
            }
            foreach (var sample in samples)
            {
                var read = provider.Interpret(sample, Compare, provider.LastSeen + 1);
                outputs.Add(WorldPublication.TryValue(read, provider.AdviceAxis, out var value) ? value : null);
            }
            economy.UpdateBody(service, provider.Root, decision);
        }
        return (outputs.ToArray(), permissions.ToArray(), spent, economy.AccountedMoney, economy.AccountedMoney == economy.ExternalNet);
    }
}
