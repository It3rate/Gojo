using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class FriezeCoordinationFeaturesTests
{
    [Fact]
    public void EqualAggregatesHideDifferentDirectionConditionedCoordination()
    {
        var library = new RhythmPatternLibrary();
        foreach (var entry in library.Entries.Take(2))
        {
            var feature = new FriezeCoordinationFeatures(entry.Fixture, entry.Fixture.Run(entry.PeriodRounds));
            Assert.Equal(new FriezeCoordinationFeatures.Summary(4, 4, 0), feature.Describe());
            Assert.True(feature.TryReadResult(feature.Evaluate(), out var same, out var opposed, out var pending));
            Assert.Equal(4, E(same).Length); Assert.Equal(4, E(opposed).Length); Assert.Empty(E(pending));
            Assert.Equal(2, E(feature.Conditioned).Length);
            foreach (var row in E(feature.Conditioned).Cast<Relation>())
            {
                Assert.True(feature.TryReadConditioned(row, out var axis, out var direction, out var a, out var b, out var aligned, out var inverse));
                Assert.Same(library.Frame.Vertical, axis); Assert.Same(entry.Components[1].Port, a); Assert.Same(entry.Components[2].Port, b);
                var firstIsUp = ReferenceEquals(direction, library.Frame.Up);
                var alignedAtThisFace = ReferenceEquals(entry, library.Entries[0]) == firstIsUp;
                Assert.Equal(alignedAtThisFace ? 4 : 0, E(aligned).Length);
                Assert.Equal(alignedAtThisFace ? 0 : 4, E(inverse).Length);
            }
            var pairs = E(same).Concat(E(opposed)).Cast<Relation>().ToArray();
            var actualPairs = new List<(Relation First, Relation Second)>();
            foreach (var pair in pairs)
            {
                Assert.True(feature.TryReadPair(pair, out var sample, out var axis, out var a, out var b, out var firstPort, out var secondPort, out var premise));
                Assert.DoesNotContain(actualPairs, actual => ReferenceEquals(actual.First, a) && ReferenceEquals(actual.Second, b));
                actualPairs.Add((a, b)); Assert.NotSame(firstPort, secondPort); Assert.Same(library.Frame.Vertical, axis);
                Assert.True(FriezeFixture.TryReadSample(sample, out _, out _, out _, out _, out var apps, out _));
                Assert.True(Array.FindIndex(E(apps), app => ReferenceEquals(app, a)) < Array.FindIndex(E(apps), app => ReferenceEquals(app, b)));
                if (E(same).Any(witness => ReferenceEquals(witness, pair))) Assert.Same(premise.Left, premise.Right);
                else Assert.Contains(premise, E(library.Frame.Opposites));
            }
            AssertNoNumbers(feature.Evaluate());
        }
    }

    [Fact]
    public void ConditioningRetainsTheActualOrderedPortPairWithThreeVerticalContributors()
    {
        var entry = new RhythmPatternLibrary().Entries[2];
        var feature = new FriezeCoordinationFeatures(entry.Fixture, entry.Fixture.Run(entry.PeriodRounds));
        Assert.Equal(0, feature.Describe().Unresolved);
        var portPairs = new HashSet<(Relation, Relation)>(); var proof = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        foreach (var row in E(feature.Conditioned).Cast<Relation>())
        {
            Assert.True(feature.TryReadConditioned(row, out _, out _, out var a, out var b, out var same, out var opposed));
            portPairs.Add((a, b));
            foreach (var pair in E(same).Concat(E(opposed))) Assert.True(proof.Add(pair));
        }
        Assert.Equal(3, portPairs.Count); Assert.Equal(6, E(feature.Conditioned).Length);
        Assert.Equal(E(feature.SameFace).Length + E(feature.OpposedFace).Length, proof.Count);
        Assert.True(feature.TryReadResult(feature.Evaluate(), out _, out _, out _));
    }

    [Fact]
    public void ASelectedSettledWindowDoesNotPretendToBeAWholeControlPeriod()
    {
        var entry = new RhythmPatternLibrary().Entries[0];
        var feature = new FriezeCoordinationFeatures(entry.Fixture, entry.Fixture.Run(1));
        Assert.Equal(new FriezeCoordinationFeatures.Summary(4, 0, 0), feature.Describe());
        Assert.True(feature.TryReadResult(feature.Evaluate(), out _, out _, out _));
        Assert.True(entry.PeriodRounds > 1);
    }

    [Fact]
    public void KnownZeroPairsAreLegitimateWhenOnlyOnePortContributesToTheAxis()
    {
        var entry = new RhythmPatternLibrary().Entries[3];
        var feature = new FriezeCoordinationFeatures(entry.Fixture, entry.Fixture.Run(entry.PeriodRounds));
        Assert.Equal(new FriezeCoordinationFeatures.Summary(0, 0, 0), feature.Describe());
        Assert.Empty(E(feature.SameFace)); Assert.Empty(E(feature.OpposedFace)); Assert.Empty(E(feature.Conditioned));
    }

    [Fact]
    public void UnequalExtentNetUncertaintyDoesNotHideAResolvedFaceComparison()
    {
        var frame = new QualitativeMotionFrame(); var h = new Placeholder(); var a = new Placeholder(); var b = new Placeholder();
        var fixture = FriezeFixture.LayeredCycles(frame,
            S(S(M(frame.Right, h), M(frame.Up, a))), S(S(S(M(frame.Down, b)))));
        var trace = fixture.Run(1); var feature = new FriezeCoordinationFeatures(fixture, trace);
        Assert.True(FriezeDriveAnalysis.TryReadResult(fixture, feature.DriveRead, out _, out var netPending)); Assert.NotEmpty(E(netPending));
        Assert.Equal(new FriezeCoordinationFeatures.Summary(0, 1, 0), feature.Describe());
        var pair = (Relation)Assert.Single(E(feature.OpposedFace));
        Assert.True(feature.TryReadPair(pair, out _, out var axis, out var first, out var second, out _, out _, out var premise));
        Assert.Same(frame.Vertical, axis); Assert.Same(a, ((Relation)first.Right).Right); Assert.Same(b, ((Relation)second.Right).Right);
        Assert.NotSame(a, b); Assert.Contains(premise, E(frame.Opposites)); AssertNoNumbers(feature.Evaluate());
    }

    [Fact]
    public void MissingMotionRetainsKnownPairsButMakesAggregateCountsIncomplete()
    {
        var frame = new QualitativeMotionFrame(); var h = new Placeholder(); var v = new Placeholder();
        var fixture = FriezeFixture.LayeredCycles(frame, S(S(M(frame.Right, h), M(frame.Up, v))),
            S(S(S(M(frame.Up, v))), S(StructuralConstraint.Open(M(frame.Down, v)))));
        var feature = new FriezeCoordinationFeatures(fixture, fixture.Run(1));
        var summary = feature.Describe(); Assert.Null(summary.SameFacePairs); Assert.Null(summary.OpposedFacePairs); Assert.True(summary.Unresolved > 0);
        Assert.Single(E(feature.SameFace)); Assert.Empty(E(feature.OpposedFace));
        Assert.True(feature.TryReadResult(feature.Evaluate(), out _, out _, out var pending)); Assert.NotEmpty(E(pending));
        AssertNoNumbers(feature.Evaluate());
    }

    [Fact]
    public void UnfinishedCausalQueueAndMalformedSourceCannotYieldFakeZeroCounts()
    {
        var fixture = FriezeFixture.CombinedDriveZigzag();
        foreach (var trace in new[] { fixture.Run(1, eventBudget: 2), new Relation(new Placeholder(), new Placeholder()),
            new Relation(M(fixture.Frame.Up, new Placeholder()), M(fixture.Frame.Down, new Placeholder())) })
        {
            var feature = new FriezeCoordinationFeatures(fixture, trace); var summary = feature.Describe();
            Assert.Null(summary.SameFacePairs); Assert.Null(summary.OpposedFacePairs); Assert.True(summary.Unresolved > 0);
            Assert.True(feature.TryReadResult(feature.Evaluate(), out _, out _, out var pending)); Assert.NotEmpty(E(pending));
        }
    }

    [Fact]
    public void ForeignOrOmittedNativeEvidenceCannotCreateACoordinationRead()
    {
        var entry = new RhythmPatternLibrary().Entries[0];
        var feature = new FriezeCoordinationFeatures(entry.Fixture, entry.Fixture.Run(entry.PeriodRounds));
        var other = new FriezeCoordinationFeatures(entry.Fixture, entry.Fixture.Run(entry.PeriodRounds));
        Assert.False(feature.TryReadResult(other.Evaluate(), out _, out _, out _));
        var witness = (Relation)E(feature.SameFace)[0];
        Assert.False(feature.TryReadPair(new Relation(witness.Left, witness.Right), out _, out _, out _, out _, out _, out _, out _));
        Assert.False(feature.TryReadPair((Relation)E(other.SameFace)[0], out _, out _, out _, out _, out _, out _, out _));
        var group = (Relation)E(feature.Conditioned)[0];
        Assert.False(feature.TryReadConditioned(new Relation(group.Left, group.Right), out _, out _, out _, out _, out _, out _));
        var omitted = new Relation(feature.Surface, new Relation(feature.DriveRead,
            new Relation(S(), new Relation(feature.OpposedFace, new Relation(feature.Conditioned, feature.Unresolved)))));
        Assert.False(feature.TryReadResult(omitted, out _, out _, out _));
        var omittedContext = new Relation(feature.Surface, new Relation(feature.DriveRead,
            new Relation(feature.SameFace, new Relation(feature.OpposedFace, new Relation(S(), feature.Unresolved)))));
        Assert.False(feature.TryReadResult(omittedContext, out _, out _, out _));
    }

    private static Relation M(IElement direction, IElement reach) => QualitativeMotionFrame.Move(direction, reach);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static void AssertNoNumbers(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(source);
        while (pending.Count != 0)
        {
            var item = pending.Pop(); if (!seen.Add(item)) continue; Assert.False(item is Carrier or CarrierLocation);
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
