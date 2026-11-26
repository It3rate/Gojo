using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class SynthesisScopeTests
{
    private const ulong No = 0b01;
    private const ulong Yes = 0b10;
    private const ulong Pending = No | Yes;

    [Fact]
    public void ScopeIsReadFromCurrentPortPositions()
    {
        var owner = new Store(1, 3);
        var first = owner.Publish([0, 1], [0]);
        var second = owner.Publish([2], [0]);
        var receiver = Socket.For(second);
        Assert.Null(first.DisjointPositions(receiver));
        receiver.Plug(second);
        Assert.Equal(true, first.DisjointPositions(receiver));

        owner.Rebind(second, 0, 1);
        Assert.Equal(false, first.DisjointPositions(receiver));
        owner.Retire(1);
        Assert.Null(first.DisjointPositions(receiver));
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void TwoPublishedScopesPoolAndOneScopePublishedTwiceDoesNot(ExecutionConformancePath path)
    {
        var left = Publish(Yes, Yes, Yes, No, No);
        var right = Publish(Yes, Yes, No, No, No);
        var (observer, read, total, second) = Pool(left.Members, right.Members);

        var republished = ((Store)left.Votes.Surface).Publish([4, 3, 2, 1, 0], [.. left.Votes.Planes]);
        var repeatedLane = ((Store)left.Votes.Surface).Publish([0, 0, 1, 2, 3], [.. left.Votes.Planes]);
        Assert.NotSame(left.Members, republished);
        Assert.True(left.Members.SamePosition(0, republished, 4));
        Assert.Equal(false, left.Members.DisjointPositions(republished));
        Assert.Equal(false, repeatedLane.DisjointPositions(right.Members));
        Assert.Null(left.Members.DisjointPositions(second));
        Assert.Equal(true, left.Members.DisjointPositions(right.Members));

        Assert.False(SelectDisjoint(left.Members, republished, second));
        Assert.True(Assert.Single(path.Step(observer).Steps).Skipped[0]);
        Assert.Null(total.Read(0));
        Assert.Equal((0L, 10L), total.Interval(0));

        Assert.True(SelectDisjoint(left.Members, right.Members, second));
        var step = Assert.Single(path.Step(observer).Steps);
        Assert.True(step.Skipped.None);
        Assert.True(step.Unadmitted.None);
        Assert.Equal((5L, 10L), (total.Read(0), total.Capacity));
        Assert.Equal(5, read.Value(0, read.Bindings.Count - 1));
        Assert.Equal(1, path.Settle(observer, 2));
        Assert.False(left.Members.IsTaken);
        Assert.False(right.Members.IsTaken);
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void PendingScopesKeepFiveSixOrSevenThroughSaveAndLoad(ExecutionConformancePath path)
    {
        var left = Publish(Yes, Yes, Yes, No, Pending);
        var right = Publish(Yes, Yes, No, No, Pending);
        var (observer, read, total, second) = Pool(left.Members, right.Members);
        Assert.True(SelectDisjoint(left.Members, right.Members, second));
        Assert.True(Assert.Single(path.Step(observer).Steps).Ambiguous[0]);
        ulong possible = (1UL << 5) | (1UL << 6) | (1UL << 7);
        Assert.Equal(possible, total.Possible[0].Values(0));

        var original = new[] { left.Owner, right.Owner, observer };
        var saved = Transport.SaveLocals(original);
        var restored = Transport.LoadLocals(saved).Select(local => local.Engine).ToArray();
        var copy = Assert.Single(restored[2].Wirings);
        var restoredLeft = copy.Bindings[0].Surface;
        var restoredRight = copy.Bindings[left.Members.Count].Surface;
        var restoredTotal = total.Possible[0].In(copy.Bindings[^1].Surface);
        Assert.Equal(saved, Transport.SaveLocals(restored));
        Assert.Equal(true, restoredLeft.DisjointPositions(restoredRight));
        Assert.Equal(possible, restoredTotal.Values(0));
        Assert.Equal(11, copy.Law.Roles[^1].Size);
        Assert.Null(copy.Value(0, copy.Bindings.Count - 1));

        left.Votes.Restrict(4, No);
        right.Votes.Restrict(4, Yes);
        left.Votes.In(Assert.Single(restored[0].Stores)).Restrict(4, No);
        right.Votes.In(Assert.Single(restored[1].Stores)).Restrict(4, Yes);
        Assert.True(Assert.Single(path.Step(observer).Steps).Unadmitted.None);
        Assert.True(Assert.Single(path.Step(restored[2]).Steps).Unadmitted.None);
        Assert.Equal(6L, total.Read(0));
        Assert.Equal(6, copy.Value(0, read.Bindings.Count - 1));
        Assert.Equal(1UL << 6, restoredTotal.Values(0));
        Assert.Equal(1, path.Settle(observer, 2));
        Assert.Equal(1, path.Settle(restored[2], 2));
        Assert.Equal(Transport.SaveLocals(original), Transport.SaveLocals(restored));
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void MissingScopeObservedZeroAndContradictionHaveDifferentEvidence(ExecutionConformancePath path)
    {
        var left = Publish(Yes, Yes, Yes, No, No);
        var zero = Publish(No, No, No, No, No);
        var (observer, _, total, socket) = Pool(left.Members, zero.Members);
        var missing = Assert.Single(path.Step(observer).Steps);
        Assert.True(missing.Skipped[0]);
        Assert.True(missing.Unadmitted.None);
        Assert.True(total.Possible[0].Open[0]);

        Assert.True(SelectDisjoint(left.Members, zero.Members, socket));
        var nominal = Assert.Single(path.Step(observer).Steps);
        Assert.True(nominal.Skipped.None);
        Assert.True(nominal.Unadmitted.None);
        Assert.Equal(3, total.Read(0));
        Assert.All(Enumerable.Range(0, zero.Members.Count), lane => Assert.Equal(No, zero.Votes.Values(lane)));

        var impossible = Publish(No, No, No, No, 0);
        var (other, _, contrary, otherSocket) = Pool(left.Members, impossible.Members);
        Assert.True(SelectDisjoint(left.Members, impossible.Members, otherSocket));
        var contradiction = Assert.Single(path.Step(other).Steps);
        Assert.True(contradiction.Skipped.None);
        Assert.True(contradiction.Unadmitted[0]);
        Assert.True(contrary.Possible[0].Empty[0]);
        Assert.Equal(3, total.Read(0));
    }

    private static (Engine Owner, Supports Votes, Port Members) Publish(params ulong[] observations)
    {
        var store = new Store(0, observations.Length);
        var votes = Supports.On(store, Field.Bit(store.AddPlane()));
        for (var lane = 0; lane < observations.Length; lane++)
            votes.Restrict(lane, observations[lane]);
        return (new Engine().Hold(store), votes,
            store.Publish([.. Enumerable.Range(0, observations.Length)], [.. votes.Planes]));
    }

    private static (Engine Owner, Wiring Read, Ladder Total, Socket Second) Pool(Port first, Port second)
    {
        var socket = Socket.For(second);
        var total = new Ladder(new Store(0, 1), [first.Count + socket.Count + 1]);
        Binding left = Binding.Open(first, 0, 1), right = Binding.Open(socket, 0, 1);
        var members = Enumerable.Range(0, first.Count).Concat(Enumerable.Range(0, socket.Count)).ToArray();
        var tally = RelationLaw.FromPredicate(
            [.. members.Select(_ => Shape.Bit), total.Digits[0].Shape],
            values => values.Take(members.Length).Sum() == values[^1]);
        var read = new Wiring(tally,
            [.. Enumerable.Repeat(left, first.Count), .. Enumerable.Repeat(right, socket.Count), total.Possible[0].Variable]);
        read.Add([.. members, 0]);
        return (new Engine().Add(read), read, total, socket);
    }

    private static bool SelectDisjoint(Port first, Port candidate, Socket second)
    {
        if (second.Count != candidate.Count || first.DisjointPositions(candidate) != true) return false;
        second.Plug(candidate);
        return true;
    }
}
