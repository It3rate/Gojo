using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class RetainedPassagePatternTests
{
    [Fact]
    public void TheRetainedPatternContainsActualOrderedLawsAndRoleRowsButNoLiveValues()
    {
        var empty = RetainedPassagePattern.Capture(GlyphJunction.YBranch(0).Flow);
        var occupied = RetainedPassagePattern.Capture(GlyphJunction.YBranch(12).Flow);
        Assert.Equal(empty.Encode(), occupied.Encode());
        Assert.Equal(4, occupied.RoleCount);
        Assert.Equal(2, occupied.StepCount);
        Assert.Equal(new[] { 2, 3, 0 }, occupied.Wiring(0));
        Assert.Equal(new[] { 2, 3, 1 }, occupied.Wiring(1));
        Assert.Equal(PassageLaw.Controlled(false).PackedBytes(), occupied.LawBytes(0));
        Assert.Equal(PassageLaw.Controlled(true).PackedBytes(), occupied.LawBytes(1));
        Assert.Equal(new byte[] { 0x8E, 0x07 }, occupied.RowBytes());
        var bytes = occupied.Encode();
        Assert.Equal(20, bytes.Length);
        Assert.Equal(new byte[] { 1, 4 }, bytes[..2]);
        Assert.Equal((byte)0x0E, bytes[10]);
        Assert.Equal((byte)0x1E, bytes[19]);
        Assert.Equal(bytes, RetainedPassagePattern.Decode(bytes).Encode());
    }

    [Fact]
    public void ADecodedPatternReusesItsWholeArrangementAtDifferentlyOrderedTargetPorts()
    {
        // The original source is local to this call and is not returned.
        static byte[] Retain() => RetainedPassagePattern.Capture(GlyphJunction.YBranch(12).Flow).Encode();
        var pattern = RetainedPassagePattern.Decode(Retain());
        for (byte state = 0; state < 16; state++)
        {
            var target = new PassageFixture(4, YToT(state));
            Assert.Empty(target.Steps);
            var application = pattern.Bind(target.Owner, [target.Ports[1], target.Ports[2], target.Ports[0], target.Ports[3]]);
            Assert.Same(pattern, application.Pattern);
            Assert.Equal(new[] { target.Ports[0], target.Ports[3], target.Ports[1] }, application.Wiring(0));
            Assert.Equal(new[] { target.Ports[0], target.Ports[3], target.Ports[2] }, application.Wiring(1));
            Assert.Equal(1u << state, application.Observe());
            var expected = GlyphJunction.YBranch(state).Advance();
            Assert.Equal(1u << expected, application.Predict(application.Observe()));
            Assert.True(application.TryStep(application.Observe(), out var after));
            Assert.Equal(expected, after); // Pattern role order.
            Assert.Equal(YToT(expected), target.Stored); // Owner's actual order.
            Assert.Equal(PopCount(state & 7), PopCount(target.Stored & 7));
            Assert.Equal(state & 8, target.Stored & 8);
        }
    }

    [Theory]
    [InlineData(4)]
    [InlineData(12)]
    public void HiddenControlHoldsTheSameMaterialUntilItsActualReadIsExposed(byte sourceState)
    {
        var pattern = RetainedPassagePattern.Decode(RetainedPassagePattern.Capture(GlyphJunction.YBranch(0).Flow).Encode());
        var target = new PassageFixture(4, YToT(sourceState));
        var bound = pattern.Bind(target.Owner, [target.Ports[1], target.Ports[2], target.Ports[0], target.Ports[3]]);
        var hidden = bound.Observe(7);
        Assert.Equal((1u << 4) | (1u << 12), hidden);
        Assert.Equal((1u << 1) | (1u << 10), bound.Predict(hidden));
        Assert.False(bound.TryStep(hidden, out _));
        Assert.Equal(YToT(sourceState), target.Stored);
        Assert.True(bound.TryStep(bound.Observe(), out _));
        Assert.Equal(1, PopCount(target.Stored & 7));
        Assert.Equal(YToT(GlyphJunction.YBranch(sourceState).Advance()), target.Stored);
    }

    [Fact]
    public void OrderedRecordsPreserveMergePriorityEvenWhenTheirLawsAreIdentical()
    {
        var pattern = RetainedPassagePattern.Capture(GlyphJunction.YMerge(0).Flow);
        Assert.Equal(pattern.LawBytes(0), pattern.LawBytes(1));
        var first = new PassageFixture(4, 3);
        var bound = RetainedPassagePattern.Decode(pattern.Encode()).Bind(first.Owner, first.Ports);
        Assert.True(bound.TryStep(bound.Observe(), out var normal));
        Assert.Equal((byte)6, normal); // First contributor accepted, second retained.

        var bytes = pattern.Encode();
        var reversed = bytes.ToArray();
        bytes.AsSpan(2, 9).CopyTo(reversed.AsSpan(11, 9));
        bytes.AsSpan(11, 9).CopyTo(reversed.AsSpan(2, 9));
        var second = new PassageFixture(4, 3);
        var changed = RetainedPassagePattern.Decode(reversed).Bind(second.Owner, second.Ports);
        Assert.True(changed.TryStep(changed.Observe(), out var other));
        Assert.Equal((byte)5, other); // Second contributor accepted, first retained.
        Assert.Equal(2, PopCount(normal & 7));
        Assert.Equal(2, PopCount(other & 7));
        Assert.Equal(normal & 4, other & 4); // Same coarse outlet, distinct history.
    }

    [Fact]
    public void FiveRoleCollectionRetainsTwoMaterialSitesBehindOnePossiblePresenceRead()
    {
        var pattern = RetainedPassagePattern.Decode(RetainedPassagePattern.Capture(GlyphJunction.YCollect(0).Flow).Encode());
        Assert.Equal(5, pattern.RoleCount);
        Assert.Equal(3, pattern.ReferenceBits);
        for (byte state = 0; state < 32; state++)
        {
            var target = new PassageFixture(5, state);
            var bound = pattern.Bind(target.Owner, target.Ports);
            var expected = GlyphJunction.YCollect(state).Advance();
            Assert.True(bound.TryStep(bound.Observe(), out var after));
            Assert.Equal(expected, after);
            Assert.Equal(PopCount(state & 23), PopCount(after & 23));
            Assert.Equal(state & 8, after & 8);
        }
    }

    [Fact]
    public void AThreeRolePatternCanBindBeyondTheFirstFewLocalPortPositions()
    {
        var source = new PassageFixture(3, 0);
        source.Add(source.Forward, 0, 1, 2);
        var pattern = RetainedPassagePattern.Decode(RetainedPassagePattern.Capture(source).Encode());
        var owner = new PortStructure();
        var ports = Enumerable.Range(0, 13).Select(i => owner.Publish(owner.Create(0, 0, 1, i == 9 ? 1UL : 0UL))).ToArray();
        var bound = pattern.Bind(owner, [ports[9], ports[11], ports[12]]);
        Assert.Equal(4, bound.ReferenceBits);
        Assert.Equal(new[] { 9, 11, 12 }, bound.Wiring(0));
        Assert.True(bound.TryStep(bound.Observe(), out var after));
        Assert.Equal((byte)4, after);
        Assert.Equal((byte)0, owner.ReadPort(9));
        Assert.Equal((byte)1, owner.ReadPort(12));
    }

    [Fact]
    public void SourceEditsAndCallerOwnedArraysCannotRewriteACapturedPatternOrBinding()
    {
        var source = GlyphJunction.YBranch(12).Flow;
        var pattern = RetainedPassagePattern.Capture(source);
        var original = pattern.Encode();
        source.Add(source.Forward, 0, 3, 2);
        source.Owner.StoreObservation(0, false, false, 1);
        Assert.Equal(original, pattern.Encode());

        Array.Fill(pattern.LawBytes(0), (byte)0);
        Array.Fill(pattern.RowBytes(), (byte)0);
        Array.Fill(pattern.Wiring(0), 0);
        var transport = pattern.Encode();
        var decoded = RetainedPassagePattern.Decode(transport);
        Array.Fill(transport, (byte)0);
        Assert.Equal(original, decoded.Encode());

        var target = new PassageFixture(4, 4);
        var mappings = target.Ports.ToArray();
        var bound = decoded.Bind(target.Owner, mappings);
        Array.Fill(mappings, 0);
        Array.Fill(bound.RolePorts(), 0);
        Array.Fill(bound.Wiring(0), 0);
        Array.Fill(bound.RowBytes(0), (byte)0);
        Array.Fill(bound.LawBytes(0), (byte)0);
        Assert.True(bound.TryStep(bound.Observe(), out var after));
        Assert.Equal((byte)1, after);
        Assert.Equal(original, decoded.Encode());
    }

    [Fact]
    public void IndependentApplicationsShareThePatternButNotValuesOrMaterial()
    {
        var pattern = RetainedPassagePattern.Capture(GlyphJunction.YBranch(0).Flow);
        var first = new PassageFixture(4, 4);
        var second = new PassageFixture(4, 12);
        var a = pattern.Bind(first.Owner, first.Ports);
        var b = pattern.Bind(second.Owner, second.Ports);
        Assert.Same(a.Pattern, b.Pattern);
        Assert.True(a.TryStep(a.Observe(), out _));
        Assert.Equal((byte)1, first.Stored);
        Assert.Equal((byte)12, second.Stored);
        Assert.True(b.TryStep(b.Observe(), out _));
        Assert.Equal((byte)10, second.Stored);
        Assert.Equal((byte)1, first.Stored);
    }

    [Fact]
    public void StaleObservationsAliasesAndChangedContextCannotForceAPassage()
    {
        var pattern = RetainedPassagePattern.Capture(GlyphJunction.YBranch(0).Flow);
        var target = new PassageFixture(4, 4);
        var bound = pattern.Bind(target.Owner, target.Ports);
        var old = bound.Observe();
        target.Owner.StoreObservation(3, false, false, 1);
        Assert.False(bound.TryStep(old, out _));
        Assert.Equal((byte)12, target.Stored);

        var aliased = pattern.Bind(target.Owner, [0, 1, 2, 2]);
        Assert.Equal(0u, aliased.Observe());
        Assert.Equal(0u, aliased.Predict(old));
        Assert.False(aliased.TryStep(old, out _));

        Assert.True(target.Owner.TryMove(0, 0, 2, 2));
        Assert.Equal(0u, bound.Observe());
        Assert.False(bound.TryStep(old, out _));
    }

    [Fact]
    public void MalformedTransportCannotSmugglePaddingMissingRolesOrAnIllegalFlowLaw()
    {
        var valid = RetainedPassagePattern.Capture(GlyphJunction.YBranch(0).Flow).Encode();
        var badVersion = valid.ToArray(); badVersion[0] = 2;
        var badCount = valid.ToArray(); badCount[1] = 6;
        var absentRole = valid.ToArray(); absentRole[1] = 3; // Row still references role 3.
        var padding = valid.ToArray(); padding[10] |= 128;
        var duplicateRole = valid.ToArray(); duplicateRole[10] = 10; // [2,2,0].
        var materialCreation = valid.ToArray(); materialCreation[3] |= 1; // Before 0 -> after 1.
        foreach (var bytes in new byte[][] { [], [1], [1, 4], badVersion, badCount, absentRole,
                     padding, duplicateRole, materialCreation, valid[..^1], [.. valid, 0] })
            Assert.Throws<FormatException>(() => RetainedPassagePattern.Decode(bytes));

        Assert.Throws<ArgumentException>(() => RetainedPassagePattern.Capture(new PassageFixture(3, 0)));
        Assert.Throws<ArgumentException>(() => RetainedPassagePattern.Decode(valid).Bind(new PortStructure(), [0, 1, 2]));
    }

    private static byte YToT(byte state) => (byte)((state & 8) | ((state << 1) & 7) | ((state >> 2) & 1));
    private static int PopCount(int state) => System.Numerics.BitOperations.PopCount((uint)state);
}
