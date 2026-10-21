using Gojo.Core;
using Gojo.Patterns;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeFrieze.Learning;
using Gojo.Experiments.QualitativeExecution;
using M = Gojo.Experiments.QualitativeFrieze.Learning.OrnamentLearningMemory;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public class OrnamentLearningMemoryTests
{
    [Fact]
    public void TrainingExtractsNativeRoutesAndRecurrentFragmentsWithoutShapeNames()
    {
        var source = Source((3, 0), (0, 2), (-3, 0), (0, -2));
        var features = new NativeInteriorFeatures(new NativeInteriorTopology(source));
        var first = new Relation(new Placeholder(), S()); var second = new Relation(new Placeholder(), S());
        var model = M.Train([new(first, features), new(second, features)]);
        var prototype = Assert.Single(model.Prototypes);
        Assert.Equal(2, prototype.Support); Assert.Equal(2, model.TrainingEpisodes);
        Assert.NotEmpty(model.Fragments); Assert.All(model.Fragments, fragment => Assert.False(fragment.Pattern.IsClosed));
        Assert.Equal(4, prototype.Pattern.Words.Count);
        Assert.All(Items(prototype.Occurrences).Cast<Relation>(), occurrence =>
            Assert.True(ReferenceEquals(prototype.Pattern.Surface, occurrence.Right), "Each native occurrence refers to the actual learned pattern."));
        Assert.Empty(Elements(model.Surface).OfType<Carrier>());
    }

    [Fact]
    public void FrequencyChangesOrderingWithoutMakingAnUnsupportedRouteTrue()
    {
        var rectangle = Features(Source((2, 0), (0, 2), (-2, 0), (0, -2)));
        var triangle = Features(Source((4, 0), (-2, 3), (-2, -3)));
        var model = M.Train([new(Id(), rectangle), new(Id(), triangle), new(Id(), triangle)]);
        var empty = new M.Query([]);
        Assert.Equal(3, model.Rank(empty)[0].Prototype.Pattern.Words.Count);
        var rectangleQuery = M.FromSource(Source((2, 0), (0, 2), (-2, 0), (0, -2)));
        var predictions = model.Rank(rectangleQuery);
        Assert.True(predictions.Any(prediction => prediction.Prototype.Pattern.Words.Count == 4 && prediction.IsAdmissible), "The actual closed four-side native route remains admissible.");
        Assert.True(predictions.Any(prediction => prediction.Prototype.Pattern.Words.Count == 3 && !prediction.IsAdmissible), "A learned three-side route cannot satisfy the four-side closure.");
        Assert.Equal(1, predictions.Sum(prediction => prediction.Share), 10);
    }

    [Fact]
    public void TransportRecreatesVocabularyAndFrequencyWithoutTeacherFactories()
    {
        var source = Source((3, 0), (0, 2), (-3, 0), (0, -2));
        var model = M.Train([new(Id(), Features(source)), new(Id(), Features(Source((4, 0), (-2, 3), (-2, -3))))]);
        var restored = RoundTrip(model.Surface);
        Assert.True(M.Model.TryRead(restored, out var other));
        Assert.Equal(model.Prototypes.Select(item => (item.Key, item.Support)), other.Prototypes.Select(item => (item.Key, item.Support)));
        Assert.Equal(model.Fragments.Select(item => (item.Pattern.Key, item.Support)), other.Fragments.Select(item => (item.Pattern.Key, item.Support)));
        Assert.Equal(model.Rank(M.FromSource(source)).Select(item => (item.Prototype.Key, item.Score, item.IsAdmissible)),
            other.Rank(M.FromSource(source)).Select(item => (item.Prototype.Key, item.Score, item.IsAdmissible)));
        Assert.True(Items(other.TrainingWitnesses).Length == 0, "Compact transport does not restore discarded source audits.");
    }

    [Fact]
    public void RealizationExecutesStoredWordsAndIndependentReaderDiscoversClosure()
    {
        var model = M.Train([new(Id(), Features(Source((4, 0), (0, 2), (-4, 0), (0, -2))))]);
        var prototype = Assert.Single(model.Prototypes);
        var output = M.Realize(prototype);
        Assert.Same(output.Unit, output.Execution.Current.Program);
        Assert.NotEmpty(Items(output.Execution.Current.Graph.Strokes));
        var read = Features(output.Source);
        var boundary = Assert.Single(read.Reads);
        Assert.Equal("Yes", boundary.Find("Closed").Evidence.Answer);
        Assert.Equal(prototype.Pattern.ShapeKey, M.Pattern.FromBoundary(boundary, read.Topology.Source.Graph.Frame).ShapeKey);
        Assert.Equal(prototype.Pattern.Key, M.Pattern.FromBoundary(boundary, read.Topology.Source.Graph.Frame).Key);
    }

    [Fact]
    public void AClosedLabelCannotReplaceNativeSharedReachCancellation()
    {
        var frame = new QualitativeMotionFrame();
        var a = new Placeholder(); var b = new Placeholder();
        var first = S(QualitativeMotionFrame.Move(frame.Right, a));
        var second = S(QualitativeMotionFrame.Move(frame.Left, b));
        Assert.Throws<ArgumentException>(() => M.Pattern.FromWords(frame, [first, second], true));
    }

    [Fact]
    public void ConflictingLandmarkAccountsRemainVisibleEvenWithNoPassages()
    {
        var model = M.Train([new(Id(), Features(Source((2, 0), (0, 2), (-2, 0), (0, -2))))]);
        var frame = new QualitativeMotionFrame(); var site = new Placeholder(); var reach = new Placeholder();
        var graph = new QualitativeStrokeGraph(frame, S(site), S());
        var source = new NativeLatticeSources.SourceCase("diagnostic", "unused", "unused", graph,
            S(new Relation(site, S()), new Relation(site, S(QualitativeMotionFrame.Move(frame.Right, reach)))), S(), S(), true);
        var query = M.FromSource(source);
        Assert.NotEmpty(Items(query.Contradictions));
        Assert.All(model.Rank(query), prediction => Assert.False(prediction.IsAdmissible));
    }

    [Fact]
    public void RelatedStreamsMustBindTheSameActualSitesUnderOneOrientation()
    {
        var model = M.Train([new(Id(), Features(Source((2, 0), (0, 2), (-2, 0), (0, -2))))]);
        var frame = new QualitativeMotionFrame(); var a = new Placeholder(); var b = new Placeholder(); var reach = new Placeholder();
        NativeLatticeSources.SourceCase Edge(Relation direction)
        {
            var edge = QualitativeStrokeGraph.Stroke(new Placeholder(), a, b, S(QualitativeMotionFrame.Move(direction, reach)));
            return new("diagnostic", "unused", "unused", new(frame, S(a, b), S(edge)), S(), S(edge), S(), true);
        }
        var first = M.FromSource(Edge(frame.Right)); var second = M.FromSource(Edge(frame.Up));
        Assert.All(model.Rank(first), prediction => Assert.True(prediction.IsAdmissible));
        Assert.All(model.Rank(second), prediction => Assert.True(prediction.IsAdmissible));
        var combined = M.Combine(first.Streams.Concat(second.Streams));
        Assert.NotEmpty(Items(combined.Contradictions));
        Assert.All(model.Rank(combined), prediction => Assert.False(prediction.IsAdmissible));
    }

    [Fact]
    public void PartialStreamsEndingInsideAMergedLearnedSideRetainAnOpenCorrespondence()
    {
        var source = Source((2, 0), (2, 0), (0, 2), (-4, 0), (0, -2));
        var model = M.Train([new(Id(), Features(source))]);
        var strokes = Items(source.Graph.Strokes);
        NativeLatticeSources.SourceCase Partial(params int[] indices)
        {
            var selected = indices.Select(index => strokes[index]).ToArray();
            var graph = new QualitativeStrokeGraph(source.Graph.Frame, source.Graph.Sites, StructuralConstraint.Open(selected));
            return source with { Graph = graph, StraightPassages = S(selected), Provenance = S() };
        }
        var first = M.FromSource(Partial(0, 1, 2));
        var second = M.FromSource(Partial(1, 2, 3));
        var query = M.CombineQueries([first, second]);
        Assert.True(Items(query.Contradictions).Length == 0, "Overlapping route observations agree on their actual shared chords.");
        var prediction = Assert.Single(model.Rank(query));
        Assert.True(prediction.IsAdmissible, "A source landmark inside a merged learned side does not refute that side.");
        Assert.NotEmpty(Items(prediction.Open));
    }

    [Fact]
    public void TrainingDetailedAuditRetentionHasAnExplicitBound()
    {
        var features = Features(Source((2, 0), (0, 2), (-2, 0), (0, -2)));
        var model = M.Train(Enumerable.Range(0, 12).Select(_ => new M.TrainingItem(Id(), features)), maximumAuditWitnesses: 2);
        Assert.Equal(12, Assert.Single(model.Prototypes).Support);
        Assert.Equal(2, Items(model.TrainingWitnesses).Length);
        Assert.True(StructuralConstraint.TryReadScope(model.Loss, out _, out var seam, out var tail));
        Assert.False(seam.Equals(tail));
    }

    [Fact]
    public void CheckpointDoesNotSilentlyIgnoreAnExtraNativeVertex()
    {
        var model = M.Train([new(Id(), Features(Source((2, 0), (0, 2), (-2, 0), (0, -2))))], maxFragmentLength: 0);
        var prototype = Assert.Single(model.Prototypes); var pattern = prototype.Pattern;
        var enlarged = new Relation(new Relation(model.Frame.Surface, S(Items(pattern.Sites).Append(new Placeholder()).ToArray())),
            new Relation(pattern.Edges, pattern.Closure));
        var episode = new Relation(Id(), enlarged);
        var checkpoint = new Relation(new Relation(model.Frame.Surface, S(new Relation(enlarged, S(episode)))), new Relation(S(), model.Loss));
        Assert.False(M.Model.TryRead(checkpoint, out _));
    }

    [Fact]
    public void ExhaustedRoleBindingCapacityStaysOpenInsteadOfRefutingTheRoute()
    {
        var source = Source((2, 0), (0, 2), (-2, 0), (0, -2));
        var model = M.Train([new(Id(), Features(source))]);
        var prediction = Assert.Single(model.Rank(M.FromSource(source), bindingCapacity: 0));
        Assert.True(prediction.IsAdmissible);
        Assert.NotEmpty(Items(prediction.Open));
        Assert.True(Items(prediction.Contradicted).Length == 0, "A search-capacity cut is unresolved, not a contradiction.");
    }

    private static NativeInteriorFeatures Features(NativeLatticeSources.SourceCase source) => new(new NativeInteriorTopology(source));
    private static Relation Id() => new(new Placeholder(), S());
    private static NativeLatticeSources.SourceCase Source(params (int X, int Y)[] chords)
    {
        var author = new LatticeLadderAuthor();
        var path = LatticeLadderAuthor.Path(S(), chords.Select(chord => author.Word(chord.X, chord.Y)).ToArray());
        var unit = new UnitArrangementProgram(author.Frame, "diagnostic", S(path), LatticeLadderAuthor.Ports(path), S(path));
        var execution = new UnitArrangementNode("diagnostic", [unit], unit, S());
        var current = execution.Current;
        return new("diagnostic", "unused", "unused", current.Join.Graph, current.Join.Bindings, S(Items(current.Join.Graph.Strokes)), current.Surface, true);
    }
    private static Relation RoundTrip(Relation source)
    {
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("learned-test", source));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored));
        return Assert.IsType<Relation>(restored.Root);
    }
    private static IEnumerable<IElement> Elements(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var queue = new Queue<IElement>(); queue.Enqueue(root);
        while (queue.TryDequeue(out var item))
        { if (!seen.Add(item)) continue; yield return item; if (item is Relation relation) { queue.Enqueue(relation.Left); queue.Enqueue(relation.Right); } }
    }
    private static IElement[] Items(Relation scope) => StructuralConstraint.TryReadScope(scope, out var items, out _, out _) ? items : [];
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}
