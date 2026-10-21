using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace QualitativeExecution.Tests;

public sealed class NativeLineMalformedTests
{
    private static readonly Lazy<NativeLatticeSources.SourceCase> Grid = new(() =>
        NativeLatticeSources.Cases().Single(item => item.Id == "N02"));

    [Fact]
    public void Malformed_stroke_remains_unsupported_without_throwing_or_invalidating_known_local_supports()
    {
        var source = Grid.Value; var malformed = new Relation(new Placeholder(), new Placeholder());
        var graph = new QualitativeStrokeGraph(source.Graph.Frame, source.Graph.Sites,
            S(E(source.Graph.Strokes).Append(malformed).ToArray()));
        var supplier = new NativeLineSupplier(source with { Graph = graph });
        Assert.NotNull(supplier.Publication);
        Assert.True(supplier.Publication.TryMapStroke(malformed, out var exposed));
        Assert.NotSame(malformed, exposed);
        Assert.Contains(E(supplier.Unsupported), item => ReferenceEquals(item, exposed));
        Assert.Contains(E(supplier.Support.Graph.Strokes), item => ReferenceEquals(item, exposed));
        Assert.NotEmpty(E(supplier.Support.Unresolved));
        Assert.True(StructuralConstraint.TryReadScope(supplier.Observation.Segments, out var known, out var head, out var tail));
        Assert.False(head.Equals(tail)); Assert.Equal(6, known.Length);
        Assert.Equal(9, supplier.Candidates.Count);
        Assert.All(supplier.Candidates, candidate => Assert.Equal("Yes", candidate.Read(candidate.Frame.FiniteBoundary).Answer));
        // These are selected four-support proofs. Their success does not erase
        // the malformed remainder or assert a complete global observation.
        Assert.NotEmpty(E(supplier.Unsupported)); Assert.Equal(7, E(graph.Strokes).Length);
    }

    [Fact]
    public void Exposed_port_with_a_malformed_stroke_rebuilds_a_useful_partial_supplier()
    {
        var source = Grid.Value;
        var malformed = new Relation(new Placeholder(), new Placeholder());
        var graph = new QualitativeStrokeGraph(source.Graph.Frame, source.Graph.Sites,
            S(E(source.Graph.Strokes).Append(malformed).ToArray()));
        var supplier = new NativeLineSupplier(source with { Graph = graph });
        Assert.True(NativeLineSupplier.TryReadPort(supplier.Port, out var imported, out _));
        Assert.NotEmpty(E(imported.Unsupported)); Assert.Equal(9, imported.Candidates.Count);
        Assert.All(imported.Candidates, candidate => Assert.Equal("Yes", candidate.Read(candidate.Frame.FiniteBoundary).Answer));
        Assert.True(StructuralConstraint.TryReadScope(imported.Observation.Segments, out _, out var head, out var tail));
        Assert.False(head.Equals(tail));
    }

    [Fact]
    public void Zero_candidate_capacity_retains_all_known_pair_selections_without_constructing_candidate_frames()
    {
        var supplier = new NativeLineSupplier(Grid.Value, candidateCapacity: 0);
        Assert.Empty(supplier.Candidates); Assert.Equal(9, E(supplier.Unsearched).Length);
        Assert.Empty(E(supplier.Unsupported));
        foreach (var cut in E(supplier.Unsearched).Cast<Relation>())
        {
            var selected = E((Relation)cut.Left).Concat(E((Relation)cut.Right)).ToArray();
            Assert.Equal(4, selected.Length); Assert.Equal(4, selected.Distinct(ReferenceEqualityComparer.Instance).Count());
            Assert.All(selected, support => Assert.Contains(E(supplier.Support.Graph.Strokes), actual => ReferenceEquals(actual, support)));
        }
    }

    [Fact]
    public void Unsupported_diagonal_families_are_distinct_from_unsearched_supported_candidates()
    {
        var a = new LatticeLadderAuthor();
        IElement p = new Placeholder(), q = new Placeholder(), r = new Placeholder(), s = new Placeholder();
        var up = QualitativeStrokeGraph.Stroke(new Placeholder(), p, q, a.Word(1, 1));
        var down = QualitativeStrokeGraph.Stroke(new Placeholder(), r, s, a.Word(1, -1));
        var strokes = S(up, down);
        var graph = new QualitativeStrokeGraph(a.Frame, S(p, q, r, s), strokes);
        var words = S(new Relation(p, a.Word(0, 0)), new Relation(q, a.Word(1, 1)),
            new Relation(r, a.Word(0, 1)), new Relation(s, a.Word(1, 0)));
        var source = new NativeLatticeSources.SourceCase("diagonals", "Diagonal source", "Presentation only",
            graph, words, strokes, S(), false);
        var supplier = new NativeLineSupplier(source);
        Assert.Empty(supplier.Candidates); Assert.Empty(E(supplier.Unsearched)); Assert.Equal(2, E(supplier.Unsupported).Length);
        var retained = OrnamentReconstruction.Participants(supplier.Unsupported);
        Assert.NotNull(supplier.Publication);
        Assert.True(supplier.Publication.TryMapStroke(up, out var publishedUp));
        Assert.True(supplier.Publication.TryMapStroke(down, out var publishedDown));
        Assert.Contains(publishedUp, retained); Assert.Contains(publishedDown, retained);
        Assert.DoesNotContain(up, retained); Assert.DoesNotContain(down, retained);
        // The supplier has no applicable pure-axis candidate, rather than a
        // negative shape verdict or an exhausted candidate-search budget.
        Assert.Equal(2, E(supplier.Observation.Segments).Length);
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => StructuralPotential.Items(scope);
}
