using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeExecution;

namespace QualitativeExecution.Tests;

public sealed class NativeInteriorSourcesTests
{
    private static readonly Lazy<IReadOnlyList<NativeLatticeSources.SourceCase>> Cases = new(NativeInteriorSources.Cases);

    [Fact]
    public void All_source_passages_agree_with_their_exposed_native_endpoint_words()
    {
        Assert.Equal(20, Cases.Value.Count);
        Assert.Equal(Cases.Value.Count, Cases.Value.Select(source => source.Id).Distinct().Count());
        foreach (var source in Cases.Value)
        {
            var author = new LatticeLadderAuthor(source.Graph.Frame, new Placeholder());
            Assert.InRange(E(source.Graph.Strokes).Length, 1, 160);
            var sites = E(source.Graph.Sites);
            var bindings = E(source.Bindings).Cast<Relation>().ToArray();
            Assert.Equal(E(source.Graph.Strokes).Length, E(source.StraightPassages).Length);
            foreach (var item in E(source.Graph.Strokes))
            {
                var stroke = Assert.IsType<Relation>(item);
                Assert.True(QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out var word), source.Id);
                Assert.Contains(sites, site => ReferenceEquals(site, from));
                Assert.Contains(sites, site => ReferenceEquals(site, to));
                var first = Assert.IsType<Relation>(bindings.First(row => ReferenceEquals(row.Left, from)).Right);
                var last = Assert.IsType<Relation>(bindings.First(row => ReferenceEquals(row.Left, to)).Right);
                var reduced = source.Graph.Frame.Reduce(first,
                    LatticeLadderAuthor.JoinWords(word, author.Inverse(last)));
                Assert.True(source.Graph.Frame.TryReadResult(reduced, out var residual, out _, out var pending), source.Id);
                Assert.Empty(E(residual)); Assert.Empty(E(pending));
                Assert.Contains(E(source.StraightPassages), row => ReferenceEquals(row, stroke));
            }
            var published = new NativeLinePublication(source).Published;
            Assert.All(Reachable(new Relation(published.Graph.Surface,
                new Relation(published.Bindings, published.StraightPassages))), item => Assert.True(item is Placeholder or Relation));
            Assert.Empty(E(published.Provenance));
        }
    }

    [Fact]
    public void Long_crossing_passages_do_not_supply_material_junctions_or_authored_cycles()
    {
        foreach (var source in Cases.Value.Take(2))
        {
            Assert.Equal(6, E(source.Graph.Strokes).Length);
            var summary = source.Graph.Describe(source.Graph.Evaluate());
            Assert.Equal(12, summary.Endpoints);
            Assert.Equal(0, summary.Branches);
            Assert.Equal(0, summary.Contours);
        }
    }

    [Fact]
    public void Open_outline_and_bisected_outline_preserve_their_different_incidence()
    {
        var open = Source("I15"); var bisected = Source("I16");
        var partial = open.Graph.Describe(open.Graph.Evaluate());
        Assert.Equal(3, partial.Strokes); Assert.Equal(2, partial.Endpoints); Assert.Equal(0, partial.Contours);
        var divided = bisected.Graph.Describe(bisected.Graph.Evaluate());
        Assert.Equal(7, divided.Strokes); Assert.Equal(2, divided.T); Assert.Equal(0, divided.Endpoints);
        Assert.Equal(2, divided.Contours);
    }

    [Theory]
    [InlineData("I17", 8, 1)]
    [InlineData("I18", 9, 1)]
    [InlineData("I19", 7, 2)]
    [InlineData("I20", 8, 1)]
    public void Corner_controls_expose_actual_closed_connected_routes(string id, int passages, int loops)
    {
        var source = Source(id); var summary = source.Graph.Describe(source.Graph.Evaluate());
        Assert.Equal(passages, summary.Strokes); Assert.Equal(loops, summary.Contours);
        Assert.Equal(0, summary.Endpoints); Assert.Equal(0, summary.Unresolved);
    }

    [Fact]
    public void Triangular_display_sources_do_not_silently_acquire_a_quarter_turn_model()
    {
        Assert.False(Source("I05").TurnModel); Assert.False(Source("I06").TurnModel);
        Assert.True(Source("I01").TurnModel);
        Assert.True(Source("I07").TurnModel);
    }

    private static NativeLatticeSources.SourceCase Source(string id) => Cases.Value.Single(source => source.Id == id);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; yield return item;
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
