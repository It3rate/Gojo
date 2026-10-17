using CompositeEmergence;
using Gojo.Core;
using Gojo.Structures;
using PrimitiveLearning;

namespace UtilityEcology.Tests;

public sealed class EcologyWorldTests
{
    [Fact]
    public void AcquisitionPublishesPresenceButNoMagnitudeOrInitialInterpretation()
    {
        var world = new EcologyWorld(); var sample = world.Request(Trial(raw: 5));
        Assert.True(SamplingSurface.TryReadSample(sample, out var presence, out var magnitude));
        Assert.Same(world.PresenceAxes[0], presence.Carrier); Assert.Equal(1, presence.Ticks);
        Assert.IsType<Placeholder>(magnitude);
        var learner = new AdaptiveOrdinal(world.Sources[0], world.PresenceAxes[0], world.OrderAxes[0], new Carrier());
        Assert.False(AdaptiveOrdinal.TryReadPrediction(learner.Predict(sample, world.Compare), out _));
        Assert.Equal(0, learner.LandmarkCount);
        Assert.Equal(0, world.ComparisonQueries);
    }

    [Fact]
    public void EntirePublicSampleGraphContainsOnlyPresenceCoordinates()
    {
        var world = new EcologyWorld(2);
        foreach (var source in Enumerable.Range(0, 4))
        {
            var sample = world.Request(Trial(source, raw: 5, phase: 3));
            Assert.True(world.TrySource(sample, out var exposed)); Assert.Equal(source, exposed);
            var points = Reachable(sample).OfType<CarrierLocation>().ToArray();
            Assert.NotEmpty(points);
            Assert.All(points, point =>
            {
                Assert.InRange(point.Ticks, 0, 1);
                // Ordered-group holder sites have their own structural origin;
                // those zero coordinates are not a sampled magnitude.
                if (point.Ticks != 0) Assert.Same(world.PresenceAxes[source], point.Carrier);
            });
            Assert.DoesNotContain(Reachable(sample), element => ReferenceEquals(element, world.TimeAxis));
            Assert.DoesNotContain(Reachable(sample), element => ReferenceEquals(element, world.ActionAxis));
            Assert.DoesNotContain(Reachable(sample), element => ReferenceEquals(element, world.ResourceAxis));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void OwnAcquiredSamplesExposeOnlyOrderedComparison(int encoding)
    {
        var world = new EcologyWorld(encoding);
        var samples = Enumerable.Range(0, 6).Select(raw => world.Request(Trial(raw: raw))).ToArray();
        for (var a = 0; a < samples.Length; a++) for (var b = 0; b < samples.Length; b++)
        {
            var comparison = world.Compare(samples[a], samples[b]);
            Assert.True(SamplingSurface.TryReadComparison(comparison, samples[a], samples[b], world.OrderAxes[0], out var order));
            Assert.Equal(Math.Sign(a - b), order);
            Assert.All(Reachable(comparison).OfType<CarrierLocation>(), point => Assert.InRange(point.Ticks, -1, 1));
        }
        Assert.Equal(36, world.ComparisonQueries);
    }

    [Fact]
    public void CrossSourceForeignAndUnacquiredSamplesCannotBeCompared()
    {
        var world = new EcologyWorld(); var acquired = world.Request(Trial());
        var otherSource = world.Request(Trial(source: 1));
        var otherWorld = new EcologyWorld().Request(Trial());
        var forged = SamplingSurface.Sample(new Carrier(), world.Sources[0], world.PresenceAxes[0], true);
        var absent = SamplingSurface.Sample(new Carrier(), world.Sources[0], world.PresenceAxes[0], false);
        foreach (var anchor in new[] { otherSource, otherWorld, forged, absent })
        {
            var comparison = world.Compare(acquired, anchor);
            Assert.False(SamplingSurface.TryReadComparison(comparison, acquired, anchor, world.OrderAxes[0], out _));
            Assert.False(NativeGraph.TryValue(comparison, world.OrderAxes[0], out _));
            Assert.True(ReadWitness.TryRead(comparison, out _, out _, out var result, out _, out _));
            Assert.IsType<Placeholder>(result);
        }
        Assert.Equal(4, world.ComparisonQueries);
    }

    [Fact]
    public void TapeIsDeterministicAndHasFourEqualPhases()
    {
        var a = WorldTape.Create(139, 256); var b = WorldTape.Create(139, 256); var other = WorldTape.Create(140, 256);
        Assert.Equal(1024, a.Count); Assert.Equal(a.Digest(), b.Digest()); Assert.NotEqual(a.Digest(), other.Digest());
        for (var index = 0; index < a.Count; index++)
        {
            Assert.Equal(a[index], b[index]); Assert.Equal(index / 256, a[index].Phase);
            Assert.Equal(index, a[index].Index); Assert.InRange(a[index].RawLevel, 0, 5);
            Assert.Contains(a[index].DesiredAction, new[] { -1, 1 });
            Assert.Contains(a[index].ExploratoryAction, new[] { -1, 1 });
        }
        Assert.Contains(a.Trials, trial => trial.ExploratoryAction != trial.NoiseDirection);
        Assert.Contains(Enumerable.Range(0, a.Count), index => a[index].RawLevel != other[index].RawLevel);
    }

    [Fact]
    public void RareRequestsDisappearForTwoPhasesAndReturnWithoutPublicAnnouncements()
    {
        var tape = WorldTape.Create(71, 4096);
        Assert.Contains(tape.Trials, trial => trial.Phase == 0 && trial.Source == 1);
        Assert.DoesNotContain(tape.Trials, trial => trial.Phase is 1 or 2 && trial.Source == 1);
        Assert.Contains(tape.Trials, trial => trial.Phase == 3 && trial.Source == 1);
        var world = new EcologyWorld();
        foreach (var phase in new[] { 0, 3 })
        {
            var trial = tape.Trials.First(trial => trial.Source == 1 && trial.Phase == phase);
            var sample = world.Request(trial);
            Assert.True(SamplingSurface.TryReadSample(sample, out var presence, out _)); Assert.Equal(1, presence.Ticks);
            Assert.All(Reachable(sample).OfType<CarrierLocation>(), point =>
            {
                Assert.InRange(point.Ticks, 0, 1);
                if (point.Ticks != 0) Assert.Same(world.PresenceAxes[1], point.Carrier);
            });
        }
    }

    [Fact]
    public void ExactNoisyInputLawMakesConsequenceIndependentOfEverySampleClass()
    {
        // Enumerate the physical fair-input law. This establishes an analytic
        // independence property; a finite pseudorandom tape is not the proof.
        foreach (var phase in Enumerable.Range(0, 4)) foreach (var raw in Enumerable.Range(0, 6))
        {
            var possibilities = new[] { -1, 1 }.Select(noise => WorldTape.PhysicalDirection(2, phase, raw, noise)).ToArray();
            Assert.Equal(new[] { -1, 1 }, possibilities);
        }
        foreach (var phase in Enumerable.Range(0, 3)) foreach (var raw in Enumerable.Range(0, 6))
            Assert.Equal(new[] { -1, 1 }, new[] { -1, 1 }.Select(noise => WorldTape.PhysicalDirection(3, phase, raw, noise)).ToArray());
    }

    [Fact]
    public void CommonAndNewOpportunityChangePhysicalLawWithoutAddingQualityPorts()
    {
        foreach (var raw in Enumerable.Range(0, 6))
        {
            Assert.Equal(1, WorldTape.PhysicalDirection(0, 0, raw, -1));
            var ordinal = raw < 3 ? -1 : 1;
            foreach (var phase in new[] { 1, 2, 3 }) Assert.Equal(ordinal, WorldTape.PhysicalDirection(0, phase, raw, -1));
            foreach (var phase in new[] { 0, 1, 2, 3 }) Assert.Equal(ordinal, WorldTape.PhysicalDirection(1, phase, raw, -1));
            Assert.Equal(ordinal, WorldTape.PhysicalDirection(3, 3, raw, -1));
            Assert.Equal(ordinal, WorldTape.PhysicalDirection(3, 3, raw, 1));
        }
        var world = new EcologyWorld();
        var early = world.Request(Trial(source: 3, phase: 0, raw: 1));
        var late = world.Request(Trial(source: 3, phase: 3, raw: 1));
        Assert.True(SamplingSurface.TryReadComparison(world.Compare(early, late), early, late, world.OrderAxes[3], out var order));
        Assert.Equal(0, order);
    }

    [Theory]
    [InlineData(0, 8, -12)]
    [InlineData(1, 16, -24)]
    [InlineData(2, 8, -12)]
    [InlineData(3, 8, -12)]
    public void PhysicalResourceIsBoundableToActualCommandAndHoldIsDistinctFromOpen(int source, int good, int bad)
    {
        var world = new EcologyWorld(); var trial = Trial(source, desired: -1);
        foreach (var (action, expected) in new[] { (-1, good), (1, bad), (0, 0) })
        {
            var command = WorldPublication.Value(world.ActionAxis, action, new Carrier());
            var physical = world.Execute(trial, command);
            Assert.True(WorldPublication.TryValue(physical, world.ResourceAxis, out var observed)); Assert.Equal(expected, observed);
            var bound = NativeGraph.BindFeedback(command, physical, world.ResourceAxis);
            Assert.True(NativeGraph.IsBoundTo(bound, command, world.ResourceAxis));
            Assert.False(NativeGraph.IsBoundTo(bound, WorldPublication.Value(world.ActionAxis, action, new Carrier()), world.ResourceAxis));
        }
        foreach (var command in new[] { WorldPublication.Value(world.ActionAxis, 2, new Carrier()),
            WorldPublication.Open(world.ActionAxis, new Carrier()), WorldPublication.Value(new Carrier(), 1, new Carrier()) })
        {
            var physical = world.Execute(trial, command);
            Assert.False(WorldPublication.TryValue(physical, world.ResourceAxis, out _));
            Assert.True(ReadWitness.TryRead(physical, out _, out _, out var result, out _, out _)); Assert.IsType<Placeholder>(result);
        }
    }

    [Fact]
    public void MonotonePrivateEncodingsGiveSameComparisonsAndConsequencesOnSameTape()
    {
        var tape = WorldTape.Create(91, 128);
        var signatures = Enumerable.Range(0, 3).Select(encoding =>
        {
            var world = new EcologyWorld(encoding); var prior = new Relation?[4]; var signature = new List<long>();
            foreach (var trial in tape.Trials)
            {
                var sample = world.Request(trial);
                if (prior[trial.Source] is { } anchor)
                {
                    Assert.True(SamplingSurface.TryReadComparison(world.Compare(sample, anchor), sample, anchor, world.OrderAxes[trial.Source], out var order));
                    signature.Add(order);
                }
                prior[trial.Source] = sample;
                var actual = world.Execute(trial, WorldPublication.Value(world.ActionAxis, trial.ExploratoryAction, new Carrier()));
                Assert.True(WorldPublication.TryValue(actual, world.ResourceAxis, out var resource)); signature.Add(resource);
            }
            return signature.ToArray();
        }).ToArray();
        Assert.Equal(signatures[0], signatures[1]); Assert.Equal(signatures[0], signatures[2]);
    }

    [Fact]
    public void ResourcePublicationWithholdsPrivateCommandAncestryAndDoesNotWriteAnyCarriers()
    {
        var world = new EcologyWorld(); var privateCarrier = new Carrier(); privateCarrier.Add(23);
        var privateGraph = NativeGraph.G(privateCarrier, Relation.Point(new Carrier(), 918273));
        var command = WorldPublication.Value(world.ActionAxis, 1, privateGraph);
        var before = new[] { privateCarrier, world.ActionAxis, world.ResourceAxis }.Select(c => (c.Value, c.Tension)).ToArray();
        var resource = world.Execute(Trial(desired: 1), command);
        Assert.DoesNotContain(Reachable(resource), element => ReferenceEquals(element, command));
        Assert.DoesNotContain(Reachable(resource), element => ReferenceEquals(element, privateGraph));
        Assert.DoesNotContain(Reachable(resource), element => ReferenceEquals(element, privateCarrier));
        Assert.DoesNotContain(Reachable(resource).OfType<CarrierLocation>(), point => point.Ticks == 918273);
        Assert.Equal(before, new[] { privateCarrier, world.ActionAxis, world.ResourceAxis }.Select(c => (c.Value, c.Tension)).ToArray());
        Assert.True(WorldPublication.TryValue(resource, world.ResourceAxis, out var amount)); Assert.Equal(8, amount);
    }

    private static EcologyTrial Trial(int source = 0, int raw = 0, int phase = 0, int desired = 1) =>
        new(0, phase, source, raw, desired, -1);

    private static IReadOnlyList<IElement> Reachable(IElement root)
    {
        var result = new List<IElement>(); var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var element))
        {
            if (element is CarrierLocation point) { result.Add(point); continue; }
            if (!seen.Add(element)) continue;
            result.Add(element);
            if (element is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
        return result;
    }
}
