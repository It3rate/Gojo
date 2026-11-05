using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class FamilyWalkTests
{
    [Fact]
    public void InputAndEveryRouteAreActualPassagesWithInspectableCorrespondence()
    {
        var (table, entry, uses) = Square();
        var walk = new FamilyWalk(table, entry, uses);
        Assert.Null(walk.ActiveFamilySite);
        Assert.Equal(5, walk.StateCount);
        Assert.Equal(1, walk.GuardCount);
        Assert.Equal(1, walk.OccupiedStateCount);
        Assert.Equal((byte)2, walk.ReadStateSupport(walk.InputState));
        Assert.Equal(new[] { 0, 1, 2, 0, 0 }, walk.InputRow());
        Assert.Equal(1, walk.TableCount);
        Assert.Equal(PassageLaw.Unconditional().PackedBytes(), walk.LawBytes(0));
        for (var site = 0; site < 4; site++) Assert.Equal(new[] { site, site + 2 }, walk.SiteRow(site));
        for (var route = 0; route < 4; route++)
            Assert.Equal(new[] { route * 2, route + 2, 1, (route + 1) % 4 + 2, 0, route + 1 }, walk.RouteRow(route));
        var before = walk.ValueBytes();
        var prediction = walk.Predict();
        Assert.Equal(new[] { walk.StateForSite(0) }, prediction.Targets);
        Assert.Equal(before, walk.ValueBytes());
        Assert.Empty(walk.Trace);
        Assert.True(walk.Pulse().Moved);
        Assert.Equal(0, walk.ActiveFamilySite);
        Assert.Equal((byte)1, walk.ReadStateSupport(walk.InputState));
        Assert.True(walk.Trace[0].InputTransfer);
        Assert.Null(walk.Trace[0].FromSite);
        Assert.Equal(0, walk.Trace[0].ToSite);
        Assert.Empty(walk.Trace[0].CompatibleUses);
        Assert.Equal(1, walk.OccupiedStateCount);
    }

    [Fact]
    public void APathCycleKeepsOneRequestAndDoesNotInventAStoppingRule()
    {
        var (table, entry, uses) = Square();
        var bytes = FamilyUseCodec.Encode(table, entry, uses);
        var walk = new FamilyWalk(table, entry, uses, startSite: 2);
        for (var pulse = 0; pulse < 13; pulse++)
        {
            Assert.True(walk.Pulse().Moved);
            Assert.Equal((2 + pulse) % 4, walk.ActiveFamilySite);
            Assert.Equal(1, walk.OccupiedStateCount);
            if (pulse > 0) Assert.Equal(new[] { ((2 + pulse - 1) % 4) * 2 }, walk.Trace[pulse].CompatibleUses);
        }
        Assert.True(walk.Predict().Resolved);
        Assert.Equal(new[] { walk.StateForSite(3) }, walk.Predict().Targets);
        Assert.Equal(bytes, FamilyUseCodec.Encode(table, entry, uses));
    }

    [Fact]
    public void AlignedRailsExposeCompetingRoutesInsteadOfDuplicatingTheRequest()
    {
        var (table, entry, uses) = Square(rails: true);
        var walk = new FamilyWalk(table, entry, uses);
        walk.Pulse();
        var before = walk.ValueBytes();
        var prediction = walk.Predict();
        Assert.True(prediction.Conflict);
        Assert.True(prediction.Resolved);
        Assert.False(prediction.Missing);
        var attempted = walk.Pulse();
        Assert.False(attempted.Committed);
        Assert.False(attempted.Moved);
        Assert.Equal(0, walk.ActiveFamilySite);
        Assert.Equal(before, walk.ValueBytes());
        Assert.Equal(1, walk.OccupiedStateCount);
        Assert.Empty(walk.Trace[^1].CompatibleUses);
    }

    [Theory]
    [InlineData(1, 3, 4)]
    [InlineData(2, 2, 2)]
    public void ComplementaryLawBitsSelectOneYArmFromOneSharedGuard(int support, int target, int signedUse)
    {
        var selector = new Observation((byte)support);
        var walk = Y(selector);
        Assert.Equal(3, walk.TableCount);
        Assert.Equal(walk.GuardState, walk.RouteRow(1)[2]);
        Assert.Equal(walk.GuardState, walk.RouteRow(2)[2]);
        Assert.Equal(PassageLaw.Controlled().PackedBytes(), walk.LawBytes(walk.RouteRow(1)[4]));
        Assert.Equal(PassageLaw.Controlled(false).PackedBytes(), walk.LawBytes(walk.RouteRow(2)[4]));
        walk.Pulse(); // Explicit input passage does not need the selector.
        walk.Pulse(); // Shared stem is unconditional.
        Assert.Equal(0, selector.Reads);
        Assert.Equal(1, walk.ActiveFamilySite);
        Assert.True(walk.Pulse().Moved);
        Assert.Equal(1, selector.Reads); // Both outgoing rows sample the same guard once.
        Assert.Equal(target, walk.ActiveFamilySite);
        Assert.Equal(new[] { signedUse }, walk.Trace[^1].CompatibleUses);
        Assert.Equal(1, walk.OccupiedStateCount);
        var final = walk.Pulse();
        Assert.True(final.Committed);
        Assert.False(final.Moved);
        Assert.Equal(target, walk.ActiveFamilySite);
        Assert.Equal(1, walk.OccupiedStateCount);
    }

    [Fact]
    public void UnknownOrMissingSelectorRetainsTheRequestAndResolutionCanArriveLater()
    {
        var selector = new Observation(3);
        var walk = Y(selector);
        walk.Pulse(); walk.Pulse();
        var before = walk.ValueBytes();
        var uncertain = walk.Pulse();
        Assert.False(uncertain.Committed);
        Assert.False(uncertain.Prediction.Resolved);
        Assert.False(uncertain.Prediction.Conflict);
        Assert.Equal(new[] { walk.StateForSite(2), walk.StateForSite(3) }, uncertain.Prediction.Targets);
        Assert.Equal(before, walk.ValueBytes());
        Assert.Equal(1, walk.ActiveFamilySite);
        Assert.Equal(1, walk.OccupiedStateCount);
        selector.Support = 0;
        var missing = walk.Pulse();
        Assert.True(missing.Prediction.Missing);
        Assert.False(missing.Committed);
        Assert.Equal(before, walk.ValueBytes());
        selector.Support = 2;
        Assert.True(walk.Pulse().Moved);
        Assert.Equal(2, walk.ActiveFamilySite);
        Assert.Equal(1, walk.OccupiedStateCount);
        Assert.False(walk.Trace[2].Step.Committed);
        Assert.True(walk.Trace[3].Step.Prediction.Missing);
    }

    [Fact]
    public void RepeatedUsesOfOneSeamCompileOnceButSeparateParallelLinksStillCompete()
    {
        var table = new FamilyTable(new([new(0, 0)], [new(0, 1, 0)]));
        var entry = table.Append(1, 1);
        var repeated = new FamilyWalk(table, entry, new FamilyUseTable(table.Template, [0], [0], [0]));
        Assert.Equal(1, repeated.RouteCount);
        repeated.Pulse();
        Assert.True(repeated.Pulse().Moved);
        Assert.Equal(1, repeated.ActiveFamilySite);
        Assert.Equal(1, repeated.OccupiedStateCount);

        var copy = new FamilyTable(new([new(0, 0)], [new(0, 1, 0), new(0, 1, 0)]));
        var copiedEntry = copy.Append(1, 1);
        var competing = new FamilyWalk(copy, copiedEntry, new FamilyUseTable(copy.Template, [0], [2]));
        Assert.Equal(2, competing.RouteCount);
        competing.Pulse();
        Assert.True(competing.Predict().Conflict);
        Assert.False(competing.Pulse().Moved);
        Assert.Equal(0, competing.ActiveFamilySite);
        Assert.Equal(1, competing.OccupiedStateCount);
    }

    [Fact]
    public void ParallelMovementWitnessDoesNotInventAnExactChosenLink()
    {
        var table = new FamilyTable(new([new(0, 0)], [new(0, 1, 0), new(0, 1, 0)]));
        var entry = table.Append(1, 1);
        var uses = new FamilyUseTable(table.Template, [0], [2]);
        var laws = new Dictionary<int, PassageLaw> { [0] = PassageLaw.Controlled(), [2] = PassageLaw.Controlled(false) };
        var walk = new FamilyWalk(table, entry, uses, laws: laws, control: new Observation(2));
        walk.Pulse();
        Assert.True(walk.Pulse().Moved);
        Assert.Equal(1, walk.ActiveFamilySite);
        Assert.Equal(new[] { 0, 2 }, walk.Trace[^1].CompatibleUses);
        Assert.Equal(1, walk.OccupiedStateCount);
    }

    [Fact]
    public void ReadReversalDoesNotRewriteOperationalRoutesButAnExplicitNewViewCan()
    {
        var (table, entry, uses) = Square();
        var forward = new FamilyWalk(table, entry, uses);
        var before = forward.RouteBytes();
        var read = uses.ReadGroup(table, entry, 0, reverse: true);
        Assert.Equal(0, read[0].Span.Start.Local);
        Assert.Equal(3, read[0].Span.End.Local);
        Assert.Equal(before, forward.RouteBytes());
        forward.Pulse(); forward.Pulse();
        Assert.Equal(1, forward.ActiveFamilySite);
        var reversedUses = new FamilyUseTable(table.Template,
            read.Select(use => (use.Link << 1) | (use.Reversed ? 1 : 0)).ToArray());
        var reverse = new FamilyWalk(table, entry, reversedUses);
        reverse.Pulse(); reverse.Pulse();
        Assert.Equal(3, reverse.ActiveFamilySite);
        Assert.Equal(new[] { 7 }, reverse.Trace[^1].CompatibleUses);
        Assert.Equal(1, reverse.OccupiedStateCount);
        Assert.Equal(uses.UseBytes(), new FamilyUseTable(table.Template, [0, 2, 4, 6]).UseBytes());
    }

    [Fact]
    public void IndependentlyDecodedPathAndRadialRemaindersHaveTheSameBehavior()
    {
        var path = ConstructionAtlas.All.Single(e => e.Pattern == "Square" && e.Method == "Repeated directed path").Surface;
        var radial = ConstructionAtlas.All.Single(e => e.Pattern == "Square" && e.Method == "Radial parts and chords").Surface;
        var pathBytes = FamilyUseCodec.Encode(path.Table, path.Entry, path.Uses);
        var radialBytes = FamilyUseCodec.Encode(radial.Table, radial.Entry, radial.Uses);
        Assert.Equal(pathBytes, radialBytes);
        var a = FamilyUseCodec.Decode(pathBytes);
        var b = FamilyUseCodec.Decode(radialBytes);
        var first = new FamilyWalk(a.Table, a.Entry, a.Uses);
        var second = new FamilyWalk(b.Table, b.Entry, b.Uses);
        Assert.NotSame(first.Family, second.Family);
        Assert.Equal(first.RouteBytes(), second.RouteBytes());
        Assert.Equal(first.InputBytes(), second.InputBytes());
        for (var pulse = 0; pulse < 10; pulse++)
        {
            var left = first.Pulse(); var right = second.Pulse();
            Assert.Equal(left.Moved, right.Moved);
            Assert.Equal(left.Before, right.Before);
            Assert.Equal(left.After, right.After);
            Assert.Equal(left.Prediction.Targets, right.Prediction.Targets);
            Assert.Equal(first.ActiveFamilySite, second.ActiveFamilySite);
            Assert.Equal(first.ValueBytes(), second.ValueBytes());
            Assert.Equal(1, first.OccupiedStateCount);
            Assert.Equal(1, second.OccupiedStateCount);
        }
        Assert.Equal(pathBytes, FamilyUseCodec.Encode(a.Table, a.Entry, a.Uses));
        Assert.Equal(radialBytes, FamilyUseCodec.Encode(b.Table, b.Entry, b.Uses));
    }

    [Fact]
    public void EmptySelectedRoutesAndUnconditionalGuardsHaveNoHiddenFallbacks()
    {
        var table = new FamilyTable(new([new(0, 0)], [new(0, 1, 0)]));
        var entry = table.Append(1, 1);
        var walk = new FamilyWalk(table, entry, new FamilyUseTable(table.Template, []), control: new Unreadable());
        Assert.Equal(0, walk.RouteCount);
        Assert.True(walk.Pulse().Moved);
        var before = walk.ValueBytes();
        var held = walk.Pulse();
        Assert.True(held.Committed);
        Assert.False(held.Moved);
        Assert.Equal(0, walk.ActiveFamilySite);
        Assert.Equal(new[] { walk.StateForSite(0) }, held.Prediction.Targets);
        Assert.Equal(before, walk.ValueBytes());
        Assert.Equal(1, walk.OccupiedStateCount);

        var unconditional = new FamilyWalk(table, entry, new FamilyUseTable(table.Template, [0]), control: new Unreadable());
        Assert.True(unconditional.Pulse().Moved);
        Assert.True(unconditional.Pulse().Moved);
        Assert.Equal(1, unconditional.ActiveFamilySite);
    }

    [Fact]
    public void TraceAndDiagnosticsAreDetachedObservationsAndCannotDriveOrRewriteHistory()
    {
        var (table, entry, uses) = Square();
        var walk = new FamilyWalk(table, entry, uses);
        var first = walk.Pulse();
        first.Prediction.Targets[0] = 999;
        walk.Pulse();
        var trace = walk.Trace;
        trace[0].Step.Prediction.Targets[0] = 998;
        trace[1].CompatibleUses[0] = 997;
        Array.Fill(walk.RouteBytes(), (byte)0);
        Array.Fill(walk.InputBytes(), (byte)0);
        Array.Fill(walk.SiteBytes(), (byte)0);
        Array.Fill(walk.ValueBytes(), (byte)0);
        Array.Fill(walk.PackedRows(0), (byte)0);
        Array.Fill(walk.LawBytes(0), (byte)0);
        Array.Fill(walk.RouteRow(0), 0);
        Assert.Equal(new[] { walk.StateForSite(0) }, walk.Trace[0].Step.Prediction.Targets);
        Assert.Equal(new[] { 0 }, walk.Trace[1].CompatibleUses);
        Assert.Equal(2, walk.Trace.Count);
        Assert.Equal(1, walk.ActiveFamilySite);
        Assert.Equal(1, walk.OccupiedStateCount);
        Assert.True(walk.Pulse().Moved);
        Assert.Equal(2, walk.ActiveFamilySite);
        Assert.Equal(2, trace.Count);
        Assert.Equal(3, walk.Trace.Count);
    }

    [Fact]
    public void UnsupportedSelfLoopsAndInvalidBindingsAreRejectedWithoutEditingTheSource()
    {
        var table = new FamilyTable(new([new(0, 0)], [new(0, 0, 0)]));
        var entry = table.Append(1, 1);
        var uses = new FamilyUseTable(table.Template, [0]);
        var before = FamilyUseCodec.Encode(table, entry, uses);
        Assert.Throws<ArgumentException>(() => new FamilyWalk(table, entry, uses));
        Assert.Equal(before, FamilyUseCodec.Encode(table, entry, uses));
        var square = Square();
        Assert.Throws<ArgumentOutOfRangeException>(() => new FamilyWalk(square.Table, square.Entry, square.Uses, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FamilyWalk(square.Table, square.Entry, square.Uses, 4));
        Assert.Throws<ArgumentException>(() => new FamilyWalk(square.Table, square.Entry, square.Uses,
            laws: new Dictionary<int, PassageLaw> { [1] = PassageLaw.Unconditional() }));
        var foreign = new FamilyUseTable(new([new(0, 0)], [new(0, 1, 0)]), [0]);
        Assert.Throws<ArgumentException>(() => new FamilyWalk(square.Table, square.Entry, foreign));
        var bare = new FamilyTable(new([new(0, 0)], []));
        var bareEntry = bare.Append(1, 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => new FamilyWalk(bare, bareEntry, new FamilyUseTable(bare.Template)));
    }

    private static (FamilyTable Table, FamilyEntry Entry, FamilyUseTable Uses) Square(bool rails = false)
    {
        var table = new FamilyTable(new([new(0, 0)],
            [new(0, 1, 0), new(1, 2, 0), new(2, 3, 0), new(3, 0, 0)]));
        var entry = table.Append(1, 1);
        var uses = rails ? new FamilyUseTable(table.Template, [0], [2], [5], [7])
            : new FamilyUseTable(table.Template, [0, 2, 4, 6]);
        return (table, entry, uses);
    }

    private static FamilyWalk Y(IPrimitiveReadPort selector)
    {
        var table = new FamilyTable(new([new(0, 0), new(0, 0), new(0, 0)],
            [new(0, 1, 0), new(1, 2, 1), new(1, 3, 2)]));
        var entry = table.Append(1, 1, 1, 1);
        var uses = new FamilyUseTable(table.Template, [0, 2], [0, 4]);
        return new(table, entry, uses, laws: new Dictionary<int, PassageLaw>
        {
            [2] = PassageLaw.Controlled(), [4] = PassageLaw.Controlled(false)
        }, control: selector);
    }

    private sealed class Observation(byte support) : IPrimitiveReadPort
    {
        public byte Support { get; set; } = support;
        public int Reads { get; private set; }
        public byte ReadSupport() { Reads++; return Support; }
    }

    private sealed class Unreadable : IPrimitiveReadPort
    {
        public byte ReadSupport() => throw new InvalidOperationException("An unconditional route must not query this input.");
    }
}
