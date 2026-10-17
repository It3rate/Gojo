using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class PrimitiveSamplingStudyTests
{
    [Fact]
    public void SamplingPerspectiveSharesEverySourceLawAndChangesOnlyTheExposedCommitRoutes()
    {
        var study = new PrimitiveSamplingStudy();
        var a = study.Combined; var b = study.Separated;
        Assert.Same(a.Network, b.Network); Assert.Same(a.OutputLaw, b.OutputLaw);
        Assert.Same(a.Starts, b.Starts); Assert.Same(a.RootPort, b.RootPort);
        Assert.Same(a.Frame, b.Frame); Assert.Same(a.Reaches, b.Reaches);
        Assert.Same(a.ReachOrder, b.ReachOrder); Assert.NotSame(a.Sampling, b.Sampling);
        var jointRoute = Assert.IsType<Relation>(Assert.Single(E(a.Sampling)));
        Assert.Same(a.Network.Surface, jointRoute.Left);
        Assert.Equal(2, E(b.Sampling).Length);
        foreach (var route in E(b.Sampling).Cast<Relation>())
        {
            Assert.Contains(route.Left, E(a.Network.Ports)); Assert.NotSame(a.RootPort, route.Left);
            Assert.Same(jointRoute.Right, route.Right);
        }
        Assert.Contains(a.Surface, Traverse(study.Surface)); Assert.Contains(b.Surface, Traverse(study.Surface));
        AssertNoNumbers(study.Surface);
    }

    [Fact]
    public void SameDriveApplicationsProduceZigzagChordsOrSquareWaveCornersWithoutNumericGeometry()
    {
        var study = new PrimitiveSamplingStudy(); var capture = study.Capture(4);
        var a = capture.Combined; var b = capture.Separated; var frame = a.Fixture.Frame;
        Assert.Empty(E(a.Unresolved)); Assert.Empty(E(b.Unresolved));
        Assert.Equal(4, E(a.SampleWords).Length); Assert.Equal(8, E(b.SampleWords).Length);
        Assert.Equal(new[] { new[] { frame.Right, frame.Up }, new[] { frame.Right, frame.Down },
            new[] { frame.Right, frame.Up }, new[] { frame.Right, frame.Down } },
            E(a.SampleWords).Cast<Relation>().Select(word => E(word).Cast<Relation>().Select(move => move.Left).ToArray()));
        Assert.Equal(new IElement[] { frame.Right, frame.Up, frame.Right, frame.Down,
            frame.Right, frame.Up, frame.Right, frame.Down },
            E(b.SampleWords).Cast<Relation>().Select(word => Assert.IsType<Relation>(Assert.Single(E(word))).Left));
        Assert.True(a.Fixture.TryReadTrace(a.Trace, out _, out _, out _, out var aApps, out var aSamples, out _));
        Assert.True(b.Fixture.TryReadTrace(b.Trace, out _, out _, out _, out var bApps, out var bSamples, out _));
        foreach (var (first, second) in E(aApps).Cast<Relation>().Zip(E(bApps).Cast<Relation>()))
        {
            Assert.Same(first.Right, second.Right); Assert.NotSame(first.Left, second.Left);
        }
        // Each completed root ends at the same formal displacement. The proof
        // uses native shared-reach cancellation, without calibrating any length.
        for (var i = 0; i < E(aSamples).Length; i++)
        {
            Assert.True(FriezeFixture.TryReadSample((Relation)E(aSamples)[i], out _, out _, out _, out var joint, out _, out _));
            Assert.True(FriezeFixture.TryReadSample((Relation)E(bSamples)[i * 2 + 1], out _, out _, out _, out var separate, out _, out _));
            var inverse = S(E(separate).Cast<Relation>().Select(move =>
                (IElement)new Relation(E(frame.Opposites).Cast<Relation>()
                    .Single(pair => ReferenceEquals(pair.Left, move.Left)).Right, move.Right)).ToArray());
            var comparison = frame.Reduce(joint, inverse);
            Assert.True(frame.TryReadResult(comparison, out var difference, out var canceled, out var unknown));
            Assert.Empty(E(difference)); Assert.NotEmpty(E(canceled)); Assert.Empty(E(unknown));
            AssertNoNumbers(comparison);
        }
        AssertNoNumbers(capture.NativeSource);
    }

    [Fact]
    public void EveryPublishedWordRetainsItsActualSampleAndDeltaWitness()
    {
        var capture = new PrimitiveSamplingStudy().Capture(4);
        foreach (var run in new[] { capture.Combined, capture.Separated, capture.Slower })
        {
            Assert.Equal(E(run.SampleWords).Length, E(run.WordReads).Length);
            Assert.True(run.Fixture.TryReadTrace(run.Trace, out _, out _, out _, out _, out var samples, out _));
            foreach (var ((word, read), sample) in E(run.SampleWords).Zip(E(run.WordReads)).Zip(E(samples)))
            {
                var correspondence = Assert.IsType<Relation>(read); var context = Assert.IsType<Relation>(correspondence.Left);
                Assert.Same(word, correspondence.Right); Assert.Same(sample, context.Left);
                Assert.True(FriezeFixture.TryReadSampleDelta((Relation)sample, out var delta)); Assert.Same(delta, context.Right);
                Assert.True(run.Fixture.Frame.TryReadResult(delta, out var actual, out _, out _)); Assert.Same(word, actual);
            }
            Assert.Contains(run.Trace, Traverse(run.NativeSource)); Assert.Contains(run.Analysis, Traverse(run.NativeSource));
            Assert.Contains(run.Unresolved, Traverse(run.NativeSource));
        }
    }

    [Fact]
    public void SlowerVerticalRecurrenceHasActualAlternatingRootRoutesAndHorizontalTerraces()
    {
        var study = new PrimitiveSamplingStudy(); var slower = study.Slower;
        Assert.Same(study.Combined.Frame, slower.Frame);
        foreach (var (a, b) in study.Combined.Reaches.Zip(slower.Reaches)) Assert.Same(a, b);
        Assert.NotSame(study.Combined.Network, slower.Network);
        var root = slower.Components.Single(component => ReferenceEquals(component.Surface, slower.RootPort.Right));
        Assert.Equal(2, E(root.Domain).Length);
        var run = study.Capture(4).Slower; var frame = slower.Frame;
        Assert.Equal(new[] { 2, 1, 2, 1 }, E(run.SampleWords).Cast<Relation>().Select(word => E(word).Length));
        Assert.Equal(new IElement[][] { [frame.Right, frame.Up], [frame.Right], [frame.Right, frame.Down], [frame.Right] },
            E(run.SampleWords).Cast<Relation>().Select(word => E(word).Cast<Relation>().Select(move => move.Left).ToArray()));
        Assert.Empty(E(run.Unresolved)); AssertNoNumbers(run.NativeSource);
    }

    [Fact]
    public void AbsentSamplingRouteLeavesActualApplicationsPendingInsteadOfInventingAStroke()
    {
        var fixture = new PrimitiveSamplingStudy().Combined.WithSampling(S());
        var run = PrimitiveSamplingStudy.Observe(fixture, 2);
        Assert.Empty(E(run.SampleWords)); Assert.Empty(E(run.WordReads)); Assert.NotEmpty(E(run.Unresolved));
        Assert.True(fixture.TryReadTrace(run.Trace, out _, out _, out _, out var applications, out var samples, out _));
        Assert.Equal(4, E(applications).Length); Assert.Empty(E(samples));
        Assert.Contains(Assert.IsType<Relation>(E(applications)[0]), Traverse(run.NativeSource));
        AssertNoNumbers(run.NativeSource);
    }

    [Fact]
    public void MissingEmissionRemainsAnOpenSampleWordRatherThanAClaimedHorizontalOnlyMotion()
    {
        var original = new PrimitiveSamplingStudy().Combined;
        Assert.True(FiniteLaw.TryRead(original.OutputLaw, out var phases, out var ports, out var words, out var rows));
        var up = rows.Single(row => row.Right is Relation support && E(support) is [Relation word] &&
            E(word) is [Relation move] && ReferenceEquals(move.Left, original.Frame.Up));
        var fixture = original.WithOutputs(FiniteLaw.Create(phases, ports, words,
            rows.Where(row => !ReferenceEquals(row, up)).ToArray()));
        var run = PrimitiveSamplingStudy.Observe(fixture, 1);
        var openWord = Assert.IsType<Relation>(Assert.Single(E(run.SampleWords)));
        Assert.False(Closed(openWord)); Assert.Contains(E(openWord), item => item is Placeholder);
        Assert.NotEmpty(E(run.Unresolved)); AssertNoNumbers(run.NativeSource);
    }

    [Fact]
    public void UnfinishedCascadeRetainsItsQueueAndCannotPretendACompleteCombinedSample()
    {
        var run = new PrimitiveSamplingStudy().Capture(1, eventBudget: 2).Combined;
        Assert.Empty(E(run.SampleWords)); Assert.NotEmpty(E(run.Unresolved));
        Assert.True(run.Fixture.TryReadTrace(run.Trace, out _, out var final, out _, out var applications, out _, out _));
        Assert.Single(E(applications));
        Assert.True(run.Fixture.Network.TryReadState(final, out _, out _, out _, out var queue)); Assert.NotEmpty(E(queue));
        Assert.Contains(queue, Traverse(run.NativeSource)); AssertNoNumbers(run.NativeSource);
    }

    [Fact]
    public void EmptyObservationAddsNoEventsOrStrokesToAnyPerspective()
    {
        var capture = new PrimitiveSamplingStudy().Capture(0);
        foreach (var run in new[] { capture.Combined, capture.Separated, capture.Slower })
        {
            Assert.Empty(E(run.SampleWords)); Assert.Empty(E(run.Unresolved));
            Assert.True(run.Fixture.TryReadTrace(run.Trace, out var before, out var after, out var steps, out var apps, out _, out _));
            Assert.Same(before, after); Assert.Empty(E(steps)); Assert.Empty(E(apps));
        }
        AssertNoNumbers(capture.Surface);
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static void AssertNoNumbers(IElement source) => Assert.DoesNotContain(Traverse(source), item => item is Carrier or CarrierLocation);
    private static IEnumerable<IElement> Traverse(IElement source)
    {
        var pending = new Stack<IElement>(); var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance); pending.Push(source);
        while (pending.Count != 0)
        {
            var item = pending.Pop(); if (!visited.Add(item)) continue; yield return item;
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
