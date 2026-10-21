using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeFrieze.Learning;
using M = Gojo.Experiments.QualitativeFrieze.Learning.OrnamentLearningMemory;

namespace QualitativeExecution.Tests;

public sealed class OrnamentLearningAuditTests
{
    [Fact]
    public void Repeating_the_same_advertised_source_does_not_increase_its_ranking_weight()
    {
        var (model, source) = Fixture();
        var query = M.FromSource(source);
        var once = model.Rank(query);
        var repeated = model.Rank(new M.Query(Enumerable.Repeat(query.Streams.Single(), 6), query.Unresolved));
        Assert.NotEmpty(once);
        Assert.Equal(once.Select(row => row.Prototype.Key), repeated.Select(row => row.Prototype.Key));
        Assert.Equal(once.Select(row => row.Score), repeated.Select(row => row.Score));
        Assert.Equal(once.Select(row => row.Share), repeated.Select(row => row.Share));
    }

    [Fact]
    public void Subdividing_a_straight_native_passage_preserves_the_boundary_route_read()
    {
        var (model, source) = Fixture();
        var frame = source.Graph.Frame;
        var originalStrokes = E(source.Graph.Strokes).Cast<Relation>().ToArray();
        var first = originalStrokes[0];
        Assert.True(QualitativeStrokeGraph.TryReadStroke(first, out _, out var from, out var to, out var word));
        var terms = E(word);
        Assert.True(terms.Length >= 2);
        var before = S(terms.Take(terms.Length / 2).ToArray());
        var after = S(terms.Skip(terms.Length / 2).ToArray());
        IElement between = new Placeholder();
        var fromWord = (Relation)E(source.Bindings).Cast<Relation>().Single(row => ReferenceEquals(row.Left, from)).Right;
        var read = frame.Reduce(fromWord, before);
        Assert.True(frame.TryReadResult(read, out var position, out _, out _));
        var strokes = originalStrokes.Skip(1).Cast<IElement>().Concat([
            QualitativeStrokeGraph.Stroke(new Placeholder(), from, between, before),
            QualitativeStrokeGraph.Stroke(new Placeholder(), between, to, after)]).ToArray();
        var graph = new QualitativeStrokeGraph(frame, S([.. E(source.Graph.Sites), between]), S(strokes));
        var split = source with { Graph = graph, StraightPassages = graph.Strokes,
            Bindings = S([.. E(source.Bindings), new Relation(between, position)]) };

        var originalQuery = M.FromSource(source);
        var splitQuery = M.FromSource(split);
        Assert.Equal(Assert.Single(originalQuery.Streams).Routes.Single().ShapeKey,
            Assert.Single(splitQuery.Streams).Routes.Single().ShapeKey);
        Assert.Equal(model.Rank(originalQuery).Select(row => row.Score), model.Rank(splitQuery).Select(row => row.Score));
        Assert.Equal(4, E(source.Graph.Strokes).Length);
        Assert.Equal(5, E(split.Graph.Strokes).Length);
    }

    [Fact]
    public void Reversing_storage_orientation_and_scope_order_preserves_the_undirected_route_read()
    {
        var (model, source) = Fixture();
        var frame = source.Graph.Frame;
        var strokes = E(source.Graph.Strokes).Cast<Relation>().Select((stroke, index) =>
        {
            Assert.True(QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out var word));
            return index % 2 == 0 ? QualitativeStrokeGraph.Stroke(new Placeholder(), to, from,
                Inverse(frame, word)) : stroke;
        }).Reverse().Cast<IElement>().ToArray();
        var graph = new QualitativeStrokeGraph(frame, source.Graph.Sites, S(strokes));
        var reordered = source with { Graph = graph, StraightPassages = graph.Strokes };
        var expected = model.Rank(M.FromSource(source));
        var actual = model.Rank(M.FromSource(reordered));
        Assert.Equal(expected.Select(row => row.Prototype.Key), actual.Select(row => row.Prototype.Key));
        Assert.Equal(expected.Select(row => row.Score), actual.Select(row => row.Score));
    }

    [Fact]
    public void An_asymmetric_open_route_has_the_same_native_key_when_traversed_from_the_other_end()
    {
        var frame = new QualitativeMotionFrame();
        IElement reach = new Placeholder();
        var words = new[] { frame.Right, frame.Up, frame.Right, frame.Down }
            .Select(direction => S(QualitativeMotionFrame.Move(direction, reach))).ToArray();
        var forward = M.Pattern.FromWords(frame, words);
        var backward = M.Pattern.FromWords(frame, words.Reverse().Select(word => Inverse(frame, word)).ToArray());
        Assert.False(forward.IsClosed);
        Assert.Equal(forward.Key, backward.Key);
        Assert.Equal(forward.ShapeKey, backward.ShapeKey);
    }

    [Fact]
    public void Withholding_an_inventory_seam_does_not_make_the_query_complete()
    {
        var (_, source) = Fixture();
        var graph = new QualitativeStrokeGraph(source.Graph.Frame, source.Graph.Sites,
            StructuralConstraint.Open(E(source.Graph.Strokes)));
        var open = source with { Graph = graph, StraightPassages = graph.Strokes };
        var query = M.FromSource(open);
        Assert.NotEmpty(E(query.Unresolved));
        Assert.True(OrnamentReconstruction.Participants(query.Surface).Any(item => ReferenceEquals(item, graph.Strokes)));
    }

    [Fact]
    public void Different_uncalibrated_reaches_do_not_prove_incompatible_landmark_positions()
    {
        var frame = new QualitativeMotionFrame();
        IElement site = new Placeholder();
        var first = S(QualitativeMotionFrame.Move(frame.Right, new Placeholder()));
        var second = S(QualitativeMotionFrame.Move(frame.Right, new Placeholder()));
        var stream = new M.ClueStream(new Relation(new Placeholder(), new Placeholder()), [], S())
        { Frame = frame, Bindings = S(new Relation(site, first), new Relation(site, second)) };
        var query = new M.Query([stream]);
        Assert.Empty(E(query.Contradictions));
        Assert.NotEmpty(E(query.Unresolved));
    }

    private static (M.Model Model, NativeLatticeSources.SourceCase Source) Fixture()
    {
        var samples = new[] { 0, 2, 6 }.Select(family => OrnamentLearningCorpus.Materialize(
            new(0, "audit", "train", family, 0, 0))).ToArray();
        var items = samples.Select(sample => new M.TrainingItem(new Relation(new Placeholder(), new Placeholder()),
            new NativeInteriorFeatures(new(sample.Target))));
        return (M.Train(items), samples[0].Target);
    }

    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var rows, out _, out _) ? rows : [];
    private static Relation Inverse(QualitativeMotionFrame frame, Relation word) => S(E(word).Reverse().Cast<Relation>()
        .Select(move => (IElement)QualitativeMotionFrame.Move(E(frame.Opposites).Cast<Relation>()
            .Single(pair => ReferenceEquals(pair.Left, move.Left)).Right, move.Right)).ToArray());
    private static Relation S(params IElement[] rows) => StructuralConstraint.Closed(rows);
}
