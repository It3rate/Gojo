using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class OperationPartitionPoolingTests
{
    private const int Parts = 5;
    private const ulong No = 0b01;
    private const ulong Yes = 0b10;
    private const ulong Pending = No | Yes;

    private static readonly RelationLaw Pool = PartitionReads.PoolDisjoint(Parts, Parts);
    private static readonly RelationLaw CountMembers = RelationLaw.FromPredicate(
        [.. Enumerable.Repeat(Shape.Bit, Parts), Pool.Roles[0]],
        values => values.Take(Parts).Sum() == values[Parts]);

    [Fact]
    public void DisjointPublishedPoolsRetainBothSetsOfOpportunities()
    {
        var left = PublishPool(Yes, Yes, Yes, No, No);
        var right = PublishPool(Yes, Yes, No, No, No);
        Assert.True(Disjoint(left.Members, right.Members));
        var observer = Join(left.Count, right.Count);

        var step = Assert.Single(observer.Engine.Step().Steps);

        Assert.True(step.Unadmitted.None);
        Assert.Equal((3, 2, 5, 10), CountsIn(observer.Read));
        Assert.False(left.Count.IsTaken);
        Assert.False(right.Count.IsTaken);
    }

    [Fact]
    public void PendingMembersKeepTheWholeKnownAndTheCountOpenThroughTransport()
    {
        var left = PublishPool(Yes, Yes, Yes, No, Pending);
        var right = PublishPool(Yes, Yes, No, No, Pending);
        Assert.True(Disjoint(left.Members, right.Members));
        var observer = Join(left.Count, right.Count);

        var initial = Assert.Single(observer.Engine.Step().Steps);

        Assert.True(initial.Ambiguous[0]);
        Assert.Equal((1UL << 5) | (1UL << 6) | (1UL << 7), observer.Total.Values(0));
        Assert.Equal(10, observer.Read.Value(0, 3));
        Assert.Equal(Pending, left.Votes.Values(4));
        Assert.Equal(Pending, right.Votes.Values(4));

        var original = new[] { left.Owner, right.Owner, observer.Engine };
        var saved = Transport.SaveLocals(original);
        var restored = Transport.LoadLocals(saved).Select(local => local.Engine).ToArray();
        var restoredRead = Assert.Single(restored[2].Wirings);
        Assert.Equal(saved, Transport.SaveLocals(restored));
        Assert.NotSame(left.Count, restoredRead.Bindings[0].Surface);
        Assert.Null(restoredRead.Value(0, 2));
        Assert.Equal(10, restoredRead.Value(0, 3));

        left.Votes.Restrict(4, No);
        right.Votes.Restrict(4, Yes);
        left.Votes.In(Assert.Single(restored[0].Wirings).Bindings[0].Surface).Restrict(4, No);
        right.Votes.In(Assert.Single(restored[1].Wirings).Bindings[0].Surface).Restrict(4, Yes);
        foreach (var engine in original) engine.Step();
        foreach (var engine in restored) engine.Step();

        Assert.Equal((3, 3, 6, 10), CountsIn(observer.Read));
        Assert.Equal(CountsIn(observer.Read), CountsIn(restoredRead));
        Assert.Equal(Transport.SaveLocals(original), Transport.SaveLocals(restored));
    }

    [Fact]
    public void AnUnboundScopeAnObservedZeroAndEmptyEvidenceReadDifferently()
    {
        var left = PublishPool(Yes, Yes, Yes, No, No);
        var right = PublishPool(No, No, No, No, No);
        var selectedRight = Socket.For(right.Count);
        var observer = Join(left.Count, selectedRight);

        // No second scope was selected. The input is deliberately unbound.
        var missing = Assert.Single(observer.Engine.Step().Steps);
        Assert.True(missing.Skipped[0]);
        Assert.True(missing.Unadmitted.None);
        Assert.True(observer.Total.Open[0]);
        Assert.True(observer.Whole.Open[0]);

        Assert.True(Disjoint(left.Members, right.Members));
        selectedRight.Plug(right.Count);
        var known = Assert.Single(observer.Engine.Step().Steps);
        Assert.True(known.Skipped.None);
        Assert.Equal((3, 0, 3, 10), CountsIn(observer.Read));

        var incompatible = PublishPool(No, No, No, No, 0);
        var contrary = Join(left.Count, incompatible.Count);
        var contradicted = Assert.Single(contrary.Engine.Step().Steps);
        Assert.True(contradicted.Skipped.None);
        Assert.True(contradicted.Unadmitted[0]);
        Assert.True(contrary.Total.Empty[0]);
        Assert.True(contrary.Whole.Empty[0]);
        Assert.Equal((3, 0, 3, 10), CountsIn(observer.Read));
    }

    [Fact]
    public void RepublishingTheSameMembersDoesNotQualifyAsASecondPool()
    {
        var left = PublishPool(Yes, Yes, Yes, No, No);
        var republished = left.Members.Owner.Publish([4, 3, 2, 1, 0], [.. left.Votes.Planes]);
        var countPort = left.Count.Owner.Publish([0], [.. left.Count.OwnerPlanes]);
        var second = Socket.For(countPort);
        var observer = Join(left.Count, second);

        // The selection policy checks members, even when given different ports.
        if (Disjoint(left.Members, republished)) second.Plug(countPort);

        Assert.NotSame(left.Members, republished);
        Assert.True(left.Members.SamePosition(0, republished, 4));
        Assert.Null(second.Plugged);
        Assert.True(Assert.Single(observer.Engine.Step().Steps).Skipped[0]);
        Assert.True(observer.Total.Open[0]);
        Assert.True(observer.Whole.Open[0]);
    }

    [Theory]
    [InlineData(3, 2, 5)]
    [InlineData(5, 5, 10)]
    public void PortionsOnOneAuthoredWholeKeepThatWholeWhenAdded(int left, int right, int sum)
    {
        var law = PartitionReads.SumOnCommonWhole(Parts);
        var commonScope = new Store(0, 2);
        var portions = Allocate(commonScope, law.Roles[0]);
        portions.Write(commonScope, 0, left);
        portions.Write(commonScope, 1, right);
        var whole = Allocate(commonScope, law.Roles[3]);
        whole.Write(commonScope, 0, Parts);
        var total = Supports.On(commonScope, Allocate(commonScope, law.Roles[2]));
        var read = new Wiring(law, portions.On(commonScope), portions.On(commonScope), total.Variable, whole.On(commonScope));
        read.Add(0, 1, 0, 0);

        Assert.True(read.Apply().Unadmitted.None);
        Assert.Equal((left, right, sum, Parts), CountsIn(read));
        Assert.Equal(Parts, whole.Read(commonScope, 0));
    }

    private static (Engine Owner, Supports Votes, Port Members, Port Count) PublishPool(params ulong[] observations)
    {
        var members = new Store(0, observations.Length);
        var votes = Supports.On(members, Field.Bit(members.AddPlane()));
        for (var member = 0; member < observations.Length; member++)
            votes.Restrict(member, observations[member]);

        var total = new Store(0, 1);
        var possible = Supports.On(total, Allocate(total, Pool.Roles[0]));
        var selected = Enumerable.Range(0, observations.Length).ToArray();
        var tally = new Wiring(CountMembers, [.. selected.Select(_ => votes.Variable), possible.Variable]);
        tally.Add([.. selected, 0]);
        var owner = new Engine().Add(tally);
        owner.Step();
        return (owner, votes, members.Publish(selected, [.. votes.Planes]), total.Publish([0], [.. possible.Planes]));
    }

    private static (Engine Engine, Wiring Read, Supports Total, Supports Whole) Join(Surface left, Surface right)
    {
        var receiver = new Store(0, 1);
        var total = Supports.On(receiver, Allocate(receiver, Pool.Roles[2]));
        var whole = Supports.On(receiver, Allocate(receiver, Pool.Roles[3]));
        var read = new Wiring(Pool, Published(left), Published(right), total.Variable, whole.Variable);
        read.Add(0, 0, 0, 0);
        return (new Engine().Add(read), read, total, whole);
    }

    private static bool Disjoint(Port left, Port right)
    {
        for (var a = 0; a < left.Count; a++)
            for (var b = 0; b < right.Count; b++)
                if (left.SamePosition(a, right, b)) return false;
        return true;
    }

    private static Binding Published(Surface surface) =>
        Binding.Open(surface, [.. Enumerable.Range(0, surface.PlaneCount)]);

    private static Field Allocate(Store store, Shape shape) =>
        new(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]);

    private static (int?, int?, int?, int?) CountsIn(Wiring read) =>
        (read.Value(0, 0), read.Value(0, 1), read.Value(0, 2), read.Value(0, 3));
}
