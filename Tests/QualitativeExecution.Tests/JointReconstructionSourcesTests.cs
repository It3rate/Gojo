using System.Text;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Gojo.Experiments.QualitativeFrieze.Learning;

namespace QualitativeExecution.Tests;

public sealed class JointReconstructionSourcesTests
{
    private static readonly Lazy<IReadOnlyList<JointReconstructionSources.Case>> Sources = new(JointReconstructionSources.Cases);

    [Fact]
    public void Every_fixture_is_an_executed_public_native_graph_with_consistent_endpoint_words()
    {
        Assert.Equal(16, Sources.Value.Count);
        Assert.Equal(16, Sources.Value.Select(item => item.Id).Distinct().Count());
        foreach (var item in Sources.Value)
        {
            var source = item.Source; var frame = source.Graph.Frame;
            Assert.True(Closed(source.Graph.Sites)); Assert.True(Closed(source.Graph.Strokes));
            Assert.True(Closed(source.Bindings)); Assert.True(Closed(source.StraightPassages));
            Assert.Empty(E(source.Provenance));
            Assert.False(OrnamentReconstruction.Participants(source.Graph.Surface).Any(participant => participant is Carrier or CarrierLocation));
            foreach (var entry in E(source.Graph.Strokes))
            {
                Assert.True(entry is Relation);
                var stroke = (Relation)entry;
                Assert.True(QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out var word));
                Assert.True(E(source.Graph.Sites).Any(site => ReferenceEquals(site, from)));
                Assert.True(E(source.Graph.Sites).Any(site => ReferenceEquals(site, to)));
                Assert.True(E(source.StraightPassages).Any(offered => ReferenceEquals(offered, stroke)));
                var start = Position(source, from); var end = Position(source, to);
                Assert.True(frame.TryReadResult(frame.Reduce(start, word), out var target, out _, out var pending));
                Assert.Empty(E(pending));
                Assert.True(frame.TryReadResult(frame.Reduce(target, Inverse(frame, end)), out var remaining, out _, out pending));
                Assert.Empty(E(remaining)); Assert.Empty(E(pending));
            }
        }
    }

    [Fact]
    public void Actual_shared_sites_distinguish_branching_sources_from_closed_outline_sources()
    {
        Assert.Equal(1, Degrees(Case("J13")).Count(degree => degree == 3));
        Assert.Equal(3, Degrees(Case("J13")).Count(degree => degree == 1));
        Assert.Equal(1, Degrees(Case("J14")).Count(degree => degree == 4));
        Assert.Equal(4, Degrees(Case("J14")).Count(degree => degree == 1));
        Assert.Equal(12, E(Case("J06").Graph.Strokes).Length);
        Assert.All(Degrees(Case("J06")), degree => Assert.Equal(2, degree));
    }

    [Fact]
    public void The_damaged_source_keeps_two_open_ends_without_an_authored_repair()
    {
        var source = Case("J08");
        Assert.Equal(5, E(source.Graph.Strokes).Length);
        Assert.Equal(2, Degrees(source).Count(degree => degree == 1));
        Assert.Equal(4, Degrees(source).Count(degree => degree == 2));
        Assert.Empty(E(source.Provenance));
    }

    [Fact]
    public void Repeated_and_turned_units_do_not_accidentally_join_disconnected_contours()
    {
        Assert.Equal(2, Components(Case("J10")));
        Assert.Equal(2, Components(Case("J11")));
        Assert.Equal(4, Components(Case("J12")));
        Assert.Equal(16, E(Case("J12").Graph.Strokes).Length);
        foreach (var id in new[] { "J10", "J11", "J12" })
            Assert.All(Degrees(Case(id)), degree => Assert.Equal(2, degree));
    }

    [Fact]
    public void Earlier_learning_targets_are_reused_at_their_actual_descriptors_without_label_hints()
    {
        var descriptors = OrnamentLearningCorpus.Describe(65);
        foreach (var (index, id) in new[] { (16, "J15"), (64, "J16") })
        {
            var earlier = OrnamentLearningCorpus.Materialize(descriptors[index], includePattern: false).Target;
            Assert.Equal(Signature(earlier), Signature(Case(id)));
            Assert.False(ReferenceEquals(earlier.Graph.Surface, Case(id).Graph.Surface));
        }
        Assert.Single(Sources.Value.Select(item => item.Source.Id).Distinct());
        Assert.Single(Sources.Value.Select(item => item.Source.Name).Distinct());
    }

    private static NativeLatticeSources.SourceCase Case(string id) => Sources.Value.Single(item => item.Id == id).Source;
    private static Relation Position(NativeLatticeSources.SourceCase source, IElement site)
    {
        var rows = E(source.Bindings).OfType<Relation>().Where(row => ReferenceEquals(row.Left, site)).ToArray();
        Assert.Equal(1, rows.Length); Assert.True(rows[0].Right is Relation);
        return (Relation)rows[0].Right;
    }
    private static int[] Degrees(NativeLatticeSources.SourceCase source) => E(source.Graph.Sites).Select(site =>
        E(source.Graph.Strokes).OfType<Relation>().Count(stroke => QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _) &&
            (ReferenceEquals(site, from) || ReferenceEquals(site, to)))).ToArray();
    private static int Components(NativeLatticeSources.SourceCase source)
    {
        var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var count = 0;
        foreach (var site in E(source.Graph.Sites))
        {
            if (!visited.Add(site)) continue;
            count++; var queue = new Queue<IElement>(); queue.Enqueue(site);
            while (queue.TryDequeue(out var next))
                foreach (var stroke in E(source.Graph.Strokes).OfType<Relation>())
                {
                    QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _);
                    var other = ReferenceEquals(next, from) ? to : ReferenceEquals(next, to) ? from : null;
                    if (other is not null && visited.Add(other)) queue.Enqueue(other);
                }
        }
        return count;
    }
    private static string Signature(NativeLatticeSources.SourceCase source)
    {
        var sites = E(source.Graph.Sites); var text = new StringBuilder();
        var reaches = new Dictionary<IElement, int>(ReferenceEqualityComparer.Instance);
        int Site(IElement participant) => Array.FindIndex(sites, site => ReferenceEquals(site, participant));
        string Word(Relation word) => string.Join(',', E(word).Cast<Relation>().Select(move =>
        {
            Assert.True(source.Graph.Frame.Direction(move, out var axis, out var forward, out var reach));
            if (!reaches.TryGetValue(reach, out var identity)) { identity = reaches.Count; reaches.Add(reach, identity); }
            return (ReferenceEquals(axis, source.Graph.Frame.Horizontal) ? "H" : "V") + (forward ? "+" : "-") + identity;
        }));
        foreach (var site in sites) text.Append(Site(site)).Append(':').Append(Word(Position(source, site))).Append(';');
        foreach (var stroke in E(source.Graph.Strokes).Cast<Relation>())
        {
            Assert.True(QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out var word));
            text.Append(Site(from)).Append('>').Append(Site(to)).Append(':').Append(Word(word)).Append(';');
        }
        return text.ToString();
    }
    private static Relation Inverse(QualitativeMotionFrame frame, Relation word) => StructuralConstraint.Closed(E(word).Reverse().Cast<Relation>()
        .Select(move => (IElement)QualitativeMotionFrame.Move(E(frame.Opposites).Cast<Relation>().Single(row => ReferenceEquals(row.Left, move.Left)).Right, move.Right)).ToArray());
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var rows, out _, out _) ? rows : [];
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}
