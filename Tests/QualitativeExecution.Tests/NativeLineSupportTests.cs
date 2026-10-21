using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace QualitativeExecution.Tests;

public sealed class NativeLineSupportTests
{
    [Fact]
    public void Shared_grid_sites_allow_unique_axis_continuation_through_cross_junctions()
    {
        var a = new LatticeLadderAuthor();
        var sites = Enumerable.Range(0, 9).Select(_ => (IElement)new Placeholder()).ToArray();
        IElement At(int x, int y) => sites[3 * y + x];
        var strokes = new List<IElement>();
        for (var y = 0; y < 3; y++) for (var x = 0; x < 2; x++)
            strokes.Add(Stroke(At(x, y), At(x + 1, y), a.Word(1, 0)));
        for (var x = 0; x < 3; x++) for (var y = 0; y < 2; y++)
            strokes.Add(Stroke(At(x, y), At(x, y + 1), a.Word(0, 1)));
        var graph = new QualitativeStrokeGraph(a.Frame, S(sites), S(strokes.ToArray()));
        var bindings = S(Enumerable.Range(0, 9).Select(index => (IElement)new Relation(sites[index], a.Word(index % 3, index / 3))).ToArray());
        var read = new NativeLineSupport(graph, bindings);
        Assert.Equal(6, E(read.Graph.Strokes).Length); Assert.Equal(8, E(read.Graph.Sites).Length);
        Assert.Empty(E(read.Unresolved)); Assert.Equal(12, E(graph.Strokes).Length); Assert.Equal(9, E(graph.Sites).Length);
        Assert.All(E(read.Correspondence).Cast<Relation>(), row => Assert.Equal(2, E((Relation)row.Right).Length));
        Assert.DoesNotContain(E(read.Graph.Sites), site => ReferenceEquals(site, At(1, 1)));
        Assert.All(E(read.Graph.Sites), site => Assert.Contains(sites, source => ReferenceEquals(site, source)));
        Assert.All(E(read.Bindings), row => Assert.Contains(E(bindings), original => ReferenceEquals(row, original)));
        Assert.Equal(12, AllUses(read).Length);
    }

    [Fact]
    public void Equal_endpoint_words_never_supply_a_missing_site_join()
    {
        var a = new LatticeLadderAuthor(); IElement start = new Placeholder(), leftEnd = new Placeholder(), rightStart = new Placeholder(), end = new Placeholder();
        var first = Stroke(start, leftEnd, a.Word(1, 0)); var second = Stroke(rightStart, end, a.Word(1, 0));
        var graph = new QualitativeStrokeGraph(a.Frame, S(start, leftEnd, rightStart, end), S(first, second));
        var bindings = S(new Relation(start, a.Word(0, 0)), new Relation(leftEnd, a.Word(1, 0)),
            new Relation(rightStart, a.Word(1, 0)), new Relation(end, a.Word(2, 0)));
        var read = new NativeLineSupport(graph, bindings);
        Assert.Equal(2, E(read.Graph.Strokes).Length); Assert.Equal(4, E(read.Graph.Sites).Length);
        Assert.All(E(read.Correspondence).Cast<Relation>(), row => Assert.Single(E((Relation)row.Right)));
        Assert.Empty(E(read.Unresolved));
    }

    [Fact]
    public void A_bent_route_stays_two_supports_even_when_it_has_one_path_history()
    {
        var a = new LatticeLadderAuthor(); IElement start = new Placeholder(), corner = new Placeholder(), end = new Placeholder();
        var history = new Placeholder();
        var first = QualitativeStrokeGraph.Stroke(history, start, corner, a.Word(1, 0));
        var second = QualitativeStrokeGraph.Stroke(history, corner, end, a.Word(0, 1));
        var graph = new QualitativeStrokeGraph(a.Frame, S(start, corner, end), S(first, second));
        var read = new NativeLineSupport(graph, S());
        Assert.Equal(2, E(read.Graph.Strokes).Length); Assert.Equal(3, E(read.Graph.Sites).Length);
        Assert.Empty(E(read.Unresolved)); Assert.Equal(2, AllUses(read).Length);
    }

    [Fact]
    public void Ambiguous_same_axis_branch_stops_compression_at_the_shared_site()
    {
        var a = new LatticeLadderAuthor(); IElement start = new Placeholder(), branch = new Placeholder(), end = new Placeholder(), alternative = new Placeholder();
        var graph = new QualitativeStrokeGraph(a.Frame, S(start, branch, end, alternative), S(
            Stroke(start, branch, a.Word(1, 0)), Stroke(branch, end, a.Word(1, 0)), Stroke(branch, alternative, a.Word(2, 0))));
        var read = new NativeLineSupport(graph, S());
        Assert.Equal(3, E(read.Graph.Strokes).Length); Assert.Contains(E(read.Graph.Sites), site => ReferenceEquals(site, branch));
        Assert.All(E(read.Correspondence).Cast<Relation>(), row => Assert.Single(E((Relation)row.Right)));
    }

    [Fact]
    public void Coincident_actual_links_keep_all_oriented_occurrences_without_counting_their_extent_twice()
    {
        var a = new LatticeLadderAuthor(); IElement start = new Placeholder(), middle = new Placeholder(), end = new Placeholder();
        var forward = Stroke(start, middle, a.Word(1, 0)); var inverse = Stroke(middle, start, a.Word(-1, 0));
        var last = Stroke(middle, end, a.Word(1, 0));
        var graph = new QualitativeStrokeGraph(a.Frame, S(start, middle, end), S(forward, inverse, last));
        var read = new NativeLineSupport(graph, S());
        var support = Assert.IsType<Relation>(Assert.Single(E(read.Graph.Strokes)));
        Assert.True(QualitativeStrokeGraph.TryReadStroke(support, out _, out var from, out var to, out var word));
        Assert.Same(start, from); Assert.Same(end, to); Assert.Equal(2, E(word).Length);
        var uses = AllUses(read); Assert.Equal(3, uses.Length);
        Assert.Contains(uses, use => ReferenceEquals(use.Left, forward)); Assert.Contains(uses, use => ReferenceEquals(use.Left, last));
        var reversedUse = Assert.Single(uses, use => ReferenceEquals(use.Left, inverse));
        var ends = Assert.IsType<Relation>(reversedUse.Right); Assert.Same(start, ends.Left); Assert.Same(middle, ends.Right);
        Assert.Equal(3, E(graph.Strokes).Length); Assert.Empty(E(read.Unresolved));
    }

    [Fact]
    public void Mixed_or_zero_displacement_is_carried_with_its_unresolved_support_read()
    {
        var a = new LatticeLadderAuthor(); IElement start = new Placeholder(), bendEnd = new Placeholder(), zeroEnd = new Placeholder();
        var mixed = Stroke(start, bendEnd, a.Word(1, 1));
        var zero = Stroke(start, zeroEnd, LatticeLadderAuthor.JoinWords(a.Word(1, 0), a.Word(-1, 0)));
        var graph = new QualitativeStrokeGraph(a.Frame, S(start, bendEnd, zeroEnd), S(mixed, zero));
        var read = new NativeLineSupport(graph, S());
        Assert.Equal(2, E(read.Graph.Strokes).Length); Assert.Contains(E(read.Graph.Strokes), row => ReferenceEquals(row, mixed));
        Assert.Contains(E(read.Graph.Strokes), row => ReferenceEquals(row, zero)); Assert.Equal(2, E(read.Unresolved).Length);
        Assert.Equal(2, E(read.Reductions).Length); Assert.Equal(2, AllUses(read).Length);
    }

    [Fact]
    public void Pure_axis_support_does_not_require_private_endpoint_positions()
    {
        var a = new LatticeLadderAuthor(); IElement first = new Placeholder(), middle = new Placeholder(), end = new Placeholder();
        var graph = new QualitativeStrokeGraph(a.Frame, S(first, middle, end), S(
            Stroke(middle, first, a.Word(0, -1)), Stroke(middle, end, a.Word(0, 1))));
        var open = StructuralConstraint.Open(new Relation(first, a.Word(0, 0)));
        var read = new NativeLineSupport(graph, open);
        Assert.Single(E(read.Graph.Strokes)); Assert.Equal(2, E(read.Graph.Sites).Length);
        Assert.Contains(E(read.Unresolved), row => ReferenceEquals(row, open));
        Assert.True(StructuralConstraint.TryReadScope(read.Bindings, out _, out var head, out var tail)); Assert.False(head.Equals(tail));
    }

    [Fact]
    public void Executed_source_controls_preserve_independent_endpoints_and_share_only_the_intended_model_comparisons()
    {
        var cases = NativeLatticeSources.Cases(); Assert.Equal(13, cases.Count);
        var plain = cases.Single(item => item.Id == "N01"); var modeled = cases.Single(item => item.Id == "N02");
        Assert.Same(plain.Graph, modeled.Graph); Assert.Same(plain.Bindings, modeled.Bindings);
        Assert.False(plain.TurnModel); Assert.True(modeled.TurnModel);
        foreach (var source in cases.Take(7))
        {
            Assert.Equal(6, E(source.Graph.Strokes).Length); Assert.Equal(12, E(source.Graph.Sites).Length);
            Assert.Equal(6, E(new NativeLineSupport(source.Graph, source.Bindings).Graph.Strokes).Length);
        }
        var bands = cases.Single(item => item.Id == "N09"); var turnedBands = cases.Single(item => item.Id == "N13");
        Assert.Same(bands.Graph, turnedBands.Graph); Assert.False(bands.TurnModel); Assert.True(turnedBands.TurnModel);
        Assert.All(cases.Skip(7).Take(4), source => Assert.NotEmpty(E(source.Graph.Strokes)));
        var withheld = cases.Single(item => item.Id == "N07");
        Assert.True(StructuralConstraint.TryReadScope(withheld.StraightPassages, out _, out var head, out var tail));
        Assert.False(head.Equals(tail));
    }

    private static Relation Stroke(IElement from, IElement to, Relation word) => QualitativeStrokeGraph.Stroke(new Placeholder(), from, to, word);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => StructuralPotential.Items(scope);
    private static Relation[] AllUses(NativeLineSupport read) => E(read.Correspondence).Cast<Relation>().SelectMany(row => E((Relation)row.Right)).Cast<Relation>().ToArray();
}
