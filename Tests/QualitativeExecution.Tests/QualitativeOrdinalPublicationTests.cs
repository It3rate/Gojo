using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeOrdinalPublicationTests
{
    [Fact]
    public void Shared_reach_order_keeps_equality_and_signed_order_with_linear_comparison_evidence()
    {
        var frame = new QualitativeMotionFrame(); var reach = new Placeholder();
        var sites = Enumerable.Range(0, 20).Select(_ => (IElement)new Placeholder()).ToArray();
        var bindings = S(sites.Select((site, index) => (IElement)new Relation(site, S(
            Enumerable.Repeat(index < 10 ? frame.Left : frame.Right, Math.Abs(index - 10)).Select(direction =>
                (IElement)QualitativeMotionFrame.Move(direction, reach)).Concat(
                Enumerable.Repeat(frame.Up, index % 3).Select(direction => (IElement)QualitativeMotionFrame.Move(direction, reach)))))));
        var topology = new Relation(S(sites), new Relation(S(), S()));
        var publication = new QualitativeRecognitionPublisher(frame, topology, bindings);
        var ordinal = new QualitativeRecognitionPlacementPublisher(frame, bindings, publication);
        Assert.True(ordinal.Placement.IsComplete);
        // Each axis retains an actual chain of equality/adjacent-order proofs,
        // rather than every pair of sites. The public order settles all pairs.
        var axisReads = E(ordinal.Reads).Cast<Relation>().ToArray(); Assert.Equal(2, axisReads.Length);
        Assert.All(axisReads, row => Assert.Equal(sites.Length - 1, E((Relation)row.Right).Length));
        for (var first = 0; first < sites.Length; first++)
            for (var second = 0; second < sites.Length; second++)
            {
                Assert.True(publication.TryMapSite(sites[first], out var a)); Assert.True(publication.TryMapSite(sites[second], out var b));
                var read = ordinal.Placement.Compare(ordinal.Placement.Observation.Frame.Horizontal, a, b);
                Assert.True(ordinal.Placement.TryReadComparison(read, out var before, out var equal, out var after, out var unknown));
                Assert.Empty(E(unknown));
                Assert.Equal(first < second ? 1 : 0, E(before).Length);
                Assert.Equal(first == second ? 1 : 0, E(equal).Length);
                Assert.Equal(first > second ? 1 : 0, E(after).Length);
            }
    }

    [Fact]
    public void Distinct_uncalibrated_reaches_do_not_acquire_an_order_from_an_index()
    {
        var frame = new QualitativeMotionFrame(); var a = new Placeholder(); var b = new Placeholder();
        var bindings = S(new Relation(a, S(QualitativeMotionFrame.Move(frame.Right, new Placeholder()))),
            new Relation(b, S(QualitativeMotionFrame.Move(frame.Right, new Placeholder()))));
        var publication = new QualitativeRecognitionPublisher(frame, new Relation(S(a, b), new Relation(S(), S())), bindings);
        var ordinal = new QualitativeRecognitionPlacementPublisher(frame, bindings, publication);
        Assert.False(ordinal.Placement.IsComplete);
        publication.TryMapSite(a, out var aa); publication.TryMapSite(b, out var bb);
        var read = ordinal.Placement.Compare(ordinal.Placement.Observation.Frame.Horizontal, aa, bb);
        Assert.True(ordinal.Placement.TryReadComparison(read, out var before, out var equal, out var after, out var unknown));
        Assert.Empty(E(before)); Assert.Empty(E(equal)); Assert.Empty(E(after)); Assert.NotEmpty(E(unknown));
    }

    [Fact]
    public void General_reader_retains_equal_mixed_reach_participation()
    {
        var frame = new QualitativeMotionFrame(); var a = new Placeholder(); var b = new Placeholder();
        var u = new Placeholder(); var v = new Placeholder();
        var bindings = S(new Relation(a, S(QualitativeMotionFrame.Move(frame.Right, u), QualitativeMotionFrame.Move(frame.Right, v))),
            new Relation(b, S(QualitativeMotionFrame.Move(frame.Right, v), QualitativeMotionFrame.Move(frame.Right, u))));
        var publication = new QualitativeRecognitionPublisher(frame, new Relation(S(a, b), new Relation(S(), S())), bindings);
        var ordinal = new QualitativeRecognitionPlacementPublisher(frame, bindings, publication);
        Assert.True(ordinal.Placement.IsComplete);
        publication.TryMapSite(a, out var aa); publication.TryMapSite(b, out var bb);
        var read = ordinal.Placement.Compare(ordinal.Placement.Observation.Frame.Horizontal, aa, bb);
        Assert.True(ordinal.Placement.TryReadComparison(read, out var before, out var equal, out var after, out var unknown));
        Assert.Empty(E(before)); Assert.Single(E(equal)); Assert.Empty(E(after)); Assert.Empty(E(unknown));
    }

    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static Relation S(IEnumerable<IElement> entries) => S(entries.ToArray());
}
