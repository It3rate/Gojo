using CompositeEmergence;
using Gojo.Core;
using Gojo.Equations;
using PrimitiveLearning;

namespace AdaptiveLifecycle;

public sealed record LifecycleEvent(string Stage, long Reserve, long Tension, bool Viable,
    bool Participating, long NurseryFunding, int ActiveParticipants);
public sealed record LifecycleResult(IReadOnlyList<LifecycleEvent> Events, bool DormantWithdraws,
    bool DebtPreserved, bool FreshIdentity, bool CapacityBlocksBirth, bool FundingBlocksBirth,
    bool FundedParticipantSurvives, bool TransportPreservesState, long BirthCredits,
    long RemainingNurseryFunding, long InitialNurseryFunding, long ExternalCredits,
    long MaintenanceRequested, long RetiredShortfall, long MaintenancePaid);

/// <summary>A deterministic diagnostic of physical reserves and construction.
/// It supplies no learned policy or claim of evolutionary improvement.</summary>
public static class LifecycleVerification
{
    public static LifecycleResult Run(string output)
    {
        var resource = new Carrier(); var time = new Carrier(); var input = new Carrier(); var result = new Carrier();
        var owner = new Carrier(); var receiver = new Carrier();
        var connection = new Relation(Relation.Point(owner, 4), Relation.Point(receiver, 0));
        owner.Attach(connection);
        var body = Relation.OrderedGroup(new Carrier(), Equation.Affine(input, 0, 1, result, 0, 1), receiver);
        var seed = LifecycleSeed.Create(body, owner, connection);
        var nursery = new BoundedNursery(resource, time, 40, 1, 16);
        var events = new List<LifecycleEvent>();
        Require(nursery.TryBirth(seed, out var first, out _) && first is not null, "First birth must be funded.");
        Record("first birth", first!);
        var capacityBlocks = !nursery.TryBirth(seed, out _, out _); Require(capacityBlocks, "Occupied capacity must block birth.");
        first!.Advance(17); Record("dormant after 17 ticks", first);
        var withdrawn = !first.Viable && !first.Participating;
        var debtPreserved = first.Reserve.Tension == -1;
        Require(withdrawn && debtPreserved, "Dormant upkeep must withdraw participation and retain unpaid draw.");
        nursery.Retire(first); Record("first retired", first);
        Require(nursery.TryBirth(seed, out var second, out _) && second is not null, "Retirement must make capacity available.");
        Record("replacement born", second!);
        var independent = !first.Reserve.SameIdentityAs(second!.Reserve) &&
            !first.ParticipationOwner.SameIdentityAs(second.ParticipationOwner);
        Require(independent, "Replacement must have independent mutable identity.");
        second.Credit(WorldPublication.Value(resource, 8, new Carrier(), -1024, 1024));
        second.Advance(12); Record("replacement funded and maintained", second);
        var survives = second.Viable && second.Participating && second.Reserve.Value == 12;
        Require(survives, "Useful resource must pay upkeep.");
        var liveBytes = GojoNumericFormat.Encode(nursery.Root);
        var restored = BoundedNursery.FromRoot((Relation)GojoNumericFormat.Decode(liveBytes)[0]);
        var transport = NativeInspection.Signature(nursery.Root) == NativeInspection.Signature(restored.Root) &&
            restored.ActiveCount == nursery.ActiveCount && restored.Funding.Value == nursery.Funding.Value;
        Require(transport, "Connected nursery transport must preserve state.");
        File.WriteAllBytes(Path.Combine(output, "nursery-live.gojo"), liveBytes);
        File.WriteAllBytes(Path.Combine(output, "retired-participant.gojo"), GojoNumericFormat.Encode(first.Root));
        second.Advance(13); nursery.Retire(second); Record("replacement retired", second);
        var fundingBlocks = !nursery.TryBirth(seed, out _, out _);
        Require(fundingBlocks, "Birth must remain blocked when funding is insufficient.");
        File.WriteAllBytes(Path.Combine(output, "nursery-final.gojo"), GojoNumericFormat.Encode(nursery.Root));
        Require(nursery.Funding.Value == 8, "Two 16-unit births from 40 units must leave 8 units.");
        Require(nursery.RetiredShortfall.Value == 2, "Both retired shortfalls must remain in bounded aggregate history.");
        var maintenanceRequested = checked((long)(first.Clock.Value + second.Clock.Value));
        var shortfall = checked((long)nursery.RetiredShortfall.Value);
        Require(40 + 8 == 8 + maintenanceRequested - shortfall, "Paid maintenance and unfulfilled demand must remain distinct.");
        return new(events, withdrawn, debtPreserved, independent, capacityBlocks, fundingBlocks, survives, transport,
            32, 8, 40, 8, maintenanceRequested, shortfall, maintenanceRequested - shortfall);

        void Record(string stage, LocalLifeBudget participant) => events.Add(new(stage,
            checked((long)participant.Reserve.Value), participant.Reserve.Tension, participant.Viable,
            participant.Participating, checked((long)nursery.Funding.Value), checked((int)nursery.ActiveCount)));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
