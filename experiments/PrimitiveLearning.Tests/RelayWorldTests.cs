using Gojo.Core;
using Gojo.Structures;
using PrimitiveLearning;

namespace PrimitiveLearning.Tests;

public sealed class RelayWorldTests
{
    [Fact]
    public void TapeIsDeterministicAndIndependentOfPrivateEncodingAndQueryPrice()
    {
        var condition = new RelayCondition("reference", MissingEvery: 5, AbsentEvery: 7);
        var original = RelayWorld.Tape(73, 512, condition);
        var repeated = RelayWorld.Tape(73, 512, condition);
        var reencoded = RelayWorld.Tape(73, 512, condition with { Encoding = 2, QueryPrice = 6 });
        Assert.Equal(original, repeated);
        Assert.Equal(original, reencoded);
        Assert.Equal(RelayWorld.Digest(original), RelayWorld.Digest(repeated));
        Assert.NotEqual(RelayWorld.Digest(original), RelayWorld.Digest(RelayWorld.Tape(74, 512, condition)));

        var world = new RelayWorld(condition);
        foreach (var episode in original.Take(20))
        {
            world.Acquire(episode); world.QueryDirection(episode);
            world.Execute(episode, NativePublication.Point(world.ActionAxis, 0));
        }
        Assert.Equal(original, RelayWorld.Tape(73, 512, condition));
    }

    [Fact]
    public void NovelLoadTapeChangesOnlyPrivateWithinBandCoordinates()
    {
        var condition = new RelayCondition("paired novelty", MissingEvery: 5, AbsentEvery: 7);
        var original = RelayWorld.Tape(73, 512, condition);
        var novel = RelayWorld.Tape(73, 512, condition with { NovelLoads = true });
        Assert.Equal(original.Length, novel.Length);
        for (var index = 0; index < original.Length; index++)
        {
            Assert.Equal(0, original[index].FineOffset);
            Assert.InRange(novel[index].FineOffset, 1, 15);
            Assert.Equal(original[index], novel[index] with { FineOffset = 0 });
        }
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void PersistenceIsActualRepeatProbabilityRatherThanPersistentMixture(double persistence, bool repeat)
    {
        var tape = RelayWorld.Tape(79, 2048, new RelayCondition("extreme", Persistence: persistence));
        var stable = tape.Where(episode => episode.Continuity == 0).ToArray();
        Assert.NotEmpty(stable);
        Assert.All(stable, episode => Assert.Equal(repeat ? episode.PreviousDirection : -episode.PreviousDirection,
            episode.ArrivingDirection));
        var unpredictable = tape.Where(episode => episode.Continuity == 1).ToArray();
        Assert.Contains(unpredictable, episode => episode.ArrivingDirection == episode.PreviousDirection);
        Assert.Contains(unpredictable, episode => episode.ArrivingDirection != episode.PreviousDirection);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(8)]
    [InlineData(13)]
    public void PrivateFitLawStaysConsistentAcrossThresholdConditions(int threshold)
    {
        var tape = RelayWorld.Tape(43, 512, new RelayCondition("threshold", Threshold: threshold));
        Assert.All(tape, episode =>
        {
            Assert.InRange(episode.Load, 0, 15);
            Assert.Equal(episode.Load < threshold, episode.Fits);
            Assert.Contains(episode.Continuity, new[] { 0, 1 });
            Assert.Contains(episode.ArrivingDirection, new[] { -1, 1 });
        });
        Assert.Contains(tape, episode => episode.Fits);
        Assert.Contains(tape, episode => !episode.Fits);
    }

    [Fact]
    public void MissingAndAbsentIntervalsDoNotRefreshTheLastDeliveredReference()
    {
        var condition = new RelayCondition("gaps", MissingEvery: 3, AbsentEvery: 5);
        var tape = RelayWorld.Tape(61, 128, condition);
        var lastDirection = tape[0].PreviousDirection;
        var lastTick = -1;
        Assert.Contains(tape, episode => episode.ReferenceAge > 1);
        foreach (var episode in tape)
        {
            Assert.Equal(lastDirection, episode.PreviousDirection);
            Assert.Equal(episode.Tick - lastTick, episode.ReferenceAge);
            if (!episode.Unavailable && !episode.Absent)
            {
                lastDirection = episode.ArrivingDirection;
                lastTick = episode.Tick;
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void AcquisitionPublishesOnlyPresenceAndOpenMagnitude(int encoding)
    {
        var world = new RelayWorld(new RelayCondition("opaque", Encoding: encoding));
        var sample = world.Acquire(Episode(load: 14, fits: false));
        Assert.True(SamplingSurface.TryReadSample(sample, out var presence, out var magnitude));
        Assert.Equal(1L, presence.Ticks);
        Assert.True(presence.Carrier.SameIdentityAs(world.PresenceAxis));
        Assert.IsType<Placeholder>(magnitude);
        Assert.True(ReadWitness.TryRead(sample, out var source, out _, out _, out _, out _));
        Assert.Same(world.LoadSource, source);
        Assert.All(Coordinates(sample), value => Assert.Contains(value, new[] { 0L, 1L }));
    }

    [Fact]
    public void AbsenceUnavailableAndEqualComparisonRemainDifferentReads()
    {
        var world = new RelayWorld(new RelayCondition("presence"));
        var absent = world.Acquire(Episode(absent: true));
        var missing = world.Acquire(Episode(unavailable: true));
        var present = world.Acquire(Episode());
        Assert.True(SamplingSurface.TryReadSample(absent, out var presence, out var magnitude));
        Assert.Equal(0L, presence.Ticks); Assert.IsType<Placeholder>(magnitude);
        Assert.False(SamplingSurface.TryReadSample(missing, out _, out _));
        Assert.Null(NativePublication.Read(world.Compare(present, absent)));
        Assert.Null(NativePublication.Read(world.Compare(present, missing)));
        var equal = world.Compare(present, present);
        Assert.True(SamplingSurface.TryReadComparison(equal, present, present, world.OrderAxis, out var order));
        Assert.Equal(0L, order);
        Assert.Null(NativePublication.Read(world.ProbeFit(Episode(absent: true), absent)));
        Assert.Null(NativePublication.Read(world.Arrival(Episode(unavailable: true))));
    }

    [Fact]
    public void PairOrderingIsInvariantUnderAllPrivateEncodingsAndRejectsForeignAcquisitions()
    {
        var worlds = Enumerable.Range(0, 3).Select(encoding => new RelayWorld(new RelayCondition("order", Encoding: encoding))).ToArray();
        var samples = worlds.Select(world => Enumerable.Range(0, 16).Select(load => world.Acquire(Episode(load: load, fits: load < 8))).ToArray()).ToArray();
        for (var a = 0; a < 16; a++)
        for (var b = 0; b < 16; b++)
        for (var encoding = 0; encoding < worlds.Length; encoding++)
        {
            var current = samples[encoding][a]; var anchor = samples[encoding][b];
            var comparison = worlds[encoding].Compare(current, anchor);
            Assert.True(SamplingSurface.TryReadComparison(comparison, current, anchor, worlds[encoding].OrderAxis, out var order));
            Assert.Equal((long)a.CompareTo(b), order);
        }
        Assert.Null(NativePublication.Read(worlds[0].Compare(samples[0][0], samples[1][0])));
        Assert.Equal(257, worlds[0].ComparisonQueries);
        Assert.Equal(256, worlds[1].ComparisonQueries);
    }

    [Fact]
    public void NovelWithinBandLoadsAreOrderedWithoutPublishingThePrivateSubcoordinate()
    {
        foreach (var encoding in Enumerable.Range(0, 3))
        {
            var world = new RelayWorld(new RelayCondition("novel", Encoding: encoding, NovelLoads: true));
            var oldEpisode = Episode(load: 8, fits: false);
            var novelEpisode = oldEpisode with { FineOffset = 7 };
            var higherEpisode = oldEpisode with { Load = 9 };
            var old = world.Acquire(oldEpisode); var novel = world.Acquire(novelEpisode); var higher = world.Acquire(higherEpisode);
            Assert.Equal(1L, NativePublication.Read(world.Compare(novel, old)));
            Assert.Equal(-1L, NativePublication.Read(world.Compare(novel, higher)));
            Assert.Equal(0L, NativePublication.Read(world.ProbeFit(oldEpisode, old)));
            Assert.Equal(0L, NativePublication.Read(world.ProbeFit(novelEpisode, novel)));
            Assert.True(SamplingSurface.TryReadSample(novel, out _, out var magnitude));
            Assert.IsType<Placeholder>(magnitude);
            Assert.All(Coordinates(novel), value => Assert.Contains(value, new[] { 0L, 1L }));
        }
    }

    [Fact]
    public void PublicServicesRetainTheirOwnAxesAndNarrowFrames()
    {
        var world = new RelayWorld(new RelayCondition("frames"));
        var episode = Episode(load: 5, fits: true, previous: -1, arrival: 1);
        var sample = world.Acquire(episode);
        AssertFrame(world.Context(episode), world.ContextAxis, 0, 1);
        AssertFrame(world.ProbeFit(episode, sample), world.FitAxis, 0, 1);
        AssertFrame(world.Previous(episode), world.DirectionAxis, -1, 1);
        AssertFrame(world.QueryDirection(episode), world.DirectionAxis, -1, 1);
        Assert.Equal(1, world.DirectionQueries); Assert.Equal(1, world.FitProbes);
        Assert.Null(NativePublication.Read(world.Previous(episode), world.ActionAxis));
    }

    [Fact]
    public void FitProbeRequiresTheExactAcquisitionAndEpisodeCorrespondence()
    {
        var world = new RelayWorld(new RelayCondition("fit receipts"));
        var episode = Episode(load: 5, fits: true);
        var sample = world.Acquire(episode);
        var anotherEvent = episode with { };
        var foreignWorld = new RelayWorld(world.Condition);
        var foreign = foreignWorld.Acquire(episode);
        var unregistered = SamplingSurface.Sample(new Carrier(), world.LoadSource, world.PresenceAxis, true);
        Assert.Equal(1L, NativePublication.Read(world.ProbeFit(episode, sample)));
        Assert.Null(NativePublication.Read(world.ProbeFit(anotherEvent, sample)));
        Assert.Null(NativePublication.Read(world.ProbeFit(episode with { Fits = false }, sample)));
        Assert.Null(NativePublication.Read(world.ProbeFit(episode, foreign)));
        Assert.Null(NativePublication.Read(world.ProbeFit(episode, unregistered)));
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(true, -1)]
    [InlineData(false, 1)]
    [InlineData(false, -1)]
    public void RoutingNeedsBothFitAndArrivingDirectionAndAccountsForActualFees(bool fits, int arriving)
    {
        var world = new RelayWorld(new RelayCondition("route"));
        var episode = Episode(fits: fits, arrival: arriving);
        var useful = fits ? arriving : -arriving;
        var command = NativePublication.Point(world.ActionAxis, useful);
        Assert.Equal(3L, NativePublication.Read(world.Execute(episode, command, paidFee: 1)));
        Assert.Equal(-5L, NativePublication.Read(world.Execute(episode, NativePublication.Point(world.ActionAxis, -useful), paidFee: 1)));
        Assert.Equal(-1L, NativePublication.Read(world.Execute(episode, NativePublication.Point(world.ActionAxis, 0), paidFee: 1)));
        Assert.True(ReadWitness.TryRead(world.Execute(episode, command), out var selected, out _, out _, out _, out _));
        Assert.Same(command, selected);
    }

    [Fact]
    public void MissingDirectionDoesNotEraseThePhysicalConsequenceOfAnExecutedAction()
    {
        var world = new RelayWorld(new RelayCondition("missing"));
        var episode = Episode(fits: true, arrival: 1, unavailable: true);
        Assert.Null(NativePublication.Read(world.QueryDirection(episode)));
        Assert.Equal(3L, NativePublication.Read(world.Execute(episode, NativePublication.Point(world.ActionAxis, 1), paidFee: 1)));
        Assert.Equal(-5L, NativePublication.Read(world.Execute(episode, NativePublication.Point(world.ActionAxis, -1), paidFee: 1)));
        Assert.Equal(-1L, NativePublication.Read(world.Execute(episode with { Absent = true }, NativePublication.Point(world.ActionAxis, 1), paidFee: 1)));
    }

    [Fact]
    public void ForeignOutOfDomainAndUnknownCommandsRemainOpen()
    {
        var world = new RelayWorld(new RelayCondition("invalid"));
        var episode = Episode();
        var commands = new[]
        {
            NativePublication.Point(new Carrier(), 1),
            NativePublication.Point(world.DirectionAxis, 1),
            NativePublication.Point(world.ActionAxis, 2),
            NativePublication.Point(world.ActionAxis, -2),
            NativePublication.Open(world.ActionAxis)
        };
        Assert.All(commands, command =>
        {
            var result = world.Execute(episode, command);
            Assert.Null(NativePublication.Read(result));
            Assert.True(ReadWitness.TryRead(result, out var selected, out _, out var output, out _, out _));
            Assert.Same(command, selected); Assert.IsType<Placeholder>(output);
        });
    }

    [Fact]
    public void NegativeFeeCannotManufactureResourceFeedback()
    {
        var world = new RelayWorld(new RelayCondition("cost"));
        var command = NativePublication.Point(world.ActionAxis, 1);
        var result = world.Execute(Episode(), command, paidFee: -1);
        Assert.Null(NativePublication.Read(result));
        Assert.True(ReadWitness.TryRead(result, out var selected, out _, out var output, out _, out _));
        Assert.Same(command, selected); Assert.IsType<Placeholder>(output);
    }

    [Fact]
    public void ShadowActionsAreCountedSeparatelyAndWorldPublicationsDoNotAssignNativeState()
    {
        var world = new RelayWorld(new RelayCondition("shadow"));
        var retained = new Carrier(); retained.Set(7);
        var episode = Episode(); var sample = world.Acquire(episode);
        var command = NativePublication.Point(world.ActionAxis, 1, retained);
        world.ProbeFit(episode, sample); world.Context(episode); world.Previous(episode); world.QueryDirection(episode);
        world.Execute(episode, command); world.Execute(episode, command, paidFee: 1, shadow: true);
        Assert.Equal(1, world.ShadowActions);
        Assert.Equal(7UL, retained.Value); Assert.Equal(0L, retained.Tension);
        Assert.All(new[] { world.LoadSource, world.PresenceAxis, world.OrderAxis, world.FitAxis, world.ContextAxis,
            world.DirectionAxis, world.ActionAxis, world.UtilityAxis, world.CostAxis, world.QueryBenefitAxis }, axis =>
        { Assert.Equal(0UL, axis.Value); Assert.Equal(0L, axis.Tension); });
    }

    private static RelayEpisode Episode(int load = 5, bool fits = true, int previous = 1, int arrival = 1,
        bool unavailable = false, bool absent = false) => new(0, load, fits, 0, previous, arrival, unavailable, absent);

    private static void AssertFrame(Relation publication, Carrier axis, long low, long high)
    {
        Assert.True(ReadWitness.TryRead(publication, out _, out var surface, out var result, out _, out _));
        var frame = Assert.IsType<Relation>(surface);
        var start = Assert.IsType<CarrierLocation>(Assert.IsType<Relation>(frame.Left).Left);
        var end = Assert.IsType<CarrierLocation>(Assert.IsType<Relation>(frame.Right).Left);
        Assert.True(start.Carrier.SameIdentityAs(axis)); Assert.True(end.Carrier.SameIdentityAs(axis));
        Assert.Equal(low, start.Ticks); Assert.Equal(high, end.Ticks);
        Assert.True(Assert.IsType<CarrierLocation>(result).Carrier.SameIdentityAs(axis));
    }

    private static IEnumerable<long> Coordinates(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.Count > 0)
        {
            var element = pending.Pop();
            if (!seen.Add(element)) continue;
            if (element is CarrierLocation point) yield return point.Ticks;
            if (element is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
