using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class JointSymmetryEvidenceTests
{
    [Fact]
    public void A_native_closed_square_has_bijective_quarter_turn_and_mirror_correspondences()
    {
        var source = Source([(0, 0, 2, 0), (2, 0, 2, 2), (2, 2, 0, 2), (0, 2, 0, 0)]);
        var read = JointSymmetryEvidence.Analyze(source);
        Assert.Equal(1, read.Components.Count);
        var quarter = read.Operations.Single(operation => operation.Name == "quarter-turn");
        Assert.True(quarter.Exact, "An explicitly interpreted quarter turn maps every native site and stroke bijectively.");
        Assert.Equal(4, quarter.MatchedEdges); Assert.Equal(4, quarter.MatchedSites);
        Assert.True(read.Operations.Any(operation => operation.Name.StartsWith("mirror") && operation.Exact));
        Assert.True(E(quarter.Matched).Length >= 8);
    }

    [Fact]
    public void An_unequal_rectangle_keeps_half_turn_but_not_quarter_turn()
    {
        var source = Source([(0, 0, 4, 0), (4, 0, 4, 2), (4, 2, 0, 2), (0, 2, 0, 0)]);
        var read = JointSymmetryEvidence.Analyze(source);
        Assert.True(read.Operations.Any(operation => operation.Name == "half-turn" && operation.Exact));
        Assert.False(read.Operations.Any(operation => operation.Name == "quarter-turn" && operation.Exact));
    }

    [Fact]
    public void A_word_quarter_turn_does_not_manufacture_a_geometric_equal_turn_promise()
    {
        var source = Source([(0, 0, 2, 0), (2, 0, 2, 2), (2, 2, 0, 2), (0, 2, 0, 0)], turnModel: false);
        var read = JointSymmetryEvidence.Analyze(source);
        var quarter = read.Operations.Single(operation => operation.Name == "quarter-turn");
        Assert.Equal(4, quarter.MatchedEdges);
        Assert.False(quarter.Exact);
        Assert.True(E(quarter.Open).Length != 0);
        Assert.True(read.Operations.Any(operation => operation.Name == "half-turn" && operation.Exact));
    }

    [Fact]
    public void A_missing_counterpart_retains_partial_support_and_both_sides_of_the_remainder()
    {
        var source = Source([(0, 0, 2, 0), (2, 0, 2, 2), (0, 2, 0, 0)]);
        var read = JointSymmetryEvidence.Analyze(source);
        var half = read.Operations.Single(operation => operation.Name == "half-turn");
        Assert.False(half.Exact);
        Assert.True(half.MatchedEdges >= 2);
        Assert.True(E(half.Unmatched).Length != 0);
        Assert.True(E(half.Missing).Length != 0);
    }

    [Fact]
    public void Translated_disconnected_components_match_without_claiming_an_exact_finite_field_period()
    {
        var source = Source([(0, 0, 2, 0), (2, 0, 2, 1), (6, 0, 8, 0), (8, 0, 8, 1)]);
        var read = JointSymmetryEvidence.Analyze(source, operationCapacity: 80, candidateCapacity: 1024);
        Assert.Equal(2, read.Components.Count);
        Assert.True(read.Operations.Any(operation => operation.Name == "translation" && operation.Exact &&
            operation.SourceScope.Index != operation.TargetScope.Index));
        Assert.True(read.Operations.Any(operation => operation.Name == "translation" && operation.SourceScope.Index == -1 &&
            operation.NonTrivial && !operation.Exact && operation.MatchedEdges >= 2));
    }

    [Fact]
    public void Component_correspondences_can_require_a_mirror_instead_of_translation()
    {
        var source = Source([(0, 0, 2, 0), (2, 0, 2, 2), (8, 0, 6, 0), (6, 0, 6, 2)]);
        var read = JointSymmetryEvidence.Analyze(source, operationCapacity: 80, candidateCapacity: 1024);
        Assert.True(read.Operations.Any(operation => operation.Name.StartsWith("mirror") && operation.Exact &&
            operation.SourceScope.Index >= 0 && operation.SourceScope.Index != operation.TargetScope.Index));
        Assert.False(read.Operations.Any(operation => operation.Name == "translation" && operation.Exact &&
            operation.SourceScope.Index != operation.TargetScope.Index));
    }

    [Fact]
    public void An_unjoined_cross_remains_two_components_even_when_the_whole_ink_has_quarter_turn_symmetry()
    {
        var source = Source([(-2, 0, 2, 0), (0, -2, 0, 2)]);
        var read = JointSymmetryEvidence.Analyze(source, operationCapacity: 80, candidateCapacity: 1024);
        Assert.Equal(2, read.Components.Count);
        Assert.True(read.Operations.Any(operation => operation.Name == "quarter-turn" && operation.SourceScope.Index == -1 && operation.Exact));
        Assert.Equal(4, E(source.Graph.Sites).Length);
    }

    [Fact]
    public void Inconsistent_endpoint_and_stroke_words_cannot_certify_an_exact_geometric_action()
    {
        var original = Source([(0, 0, 2, 0)]);
        var old = (Relation)E(original.Graph.Strokes).Single();
        QualitativeStrokeGraph.TryReadStroke(old, out _, out var from, out var to, out _);
        var reach = ((Relation)E((Relation)((Relation)old.Right).Right)[0]).Right;
        var wrong = QualitativeStrokeGraph.Stroke(new Placeholder(), from, to, S(QualitativeMotionFrame.Move(original.Graph.Frame.Right, reach)));
        var source = original with { Graph = new(original.Graph.Frame, original.Graph.Sites, S(wrong)), StraightPassages = S(wrong) };
        var read = JointSymmetryEvidence.Analyze(source);
        Assert.True(E(read.Unresolved).Length != 0);
        Assert.False(read.Operations.Any(operation => operation.Exact));
    }

    [Fact]
    public void An_open_inventory_or_zero_search_budget_cannot_be_reported_as_exact_symmetry()
    {
        var original = Source([(0, 0, 2, 0), (2, 0, 2, 2), (2, 2, 0, 2), (0, 2, 0, 0)]);
        var source = original with { Graph = new(original.Graph.Frame, original.Graph.Sites, StructuralConstraint.Open(E(original.Graph.Strokes))) };
        var open = JointSymmetryEvidence.Analyze(source);
        Assert.True(E(open.Unresolved).Length != 0);
        Assert.False(open.Operations.Any(operation => operation.Exact));
        var cut = JointSymmetryEvidence.Analyze(original, candidateCapacity: 0);
        Assert.Empty(cut.Operations);
        Assert.True(E(cut.Unsearched).Length != 0);
    }

    [Fact]
    public void An_unknown_motion_word_does_not_erase_known_physical_incidence()
    {
        var original = Source([(0, 0, 2, 0), (2, 0, 2, 2)]);
        var rows = E(original.Graph.Strokes).Cast<Relation>().ToArray();
        QualitativeStrokeGraph.TryReadStroke(rows[1], out _, out var from, out var to, out _);
        var unknown = QualitativeStrokeGraph.Stroke(new Placeholder(), from, to, StructuralConstraint.Open());
        var source = original with { Graph = new(original.Graph.Frame, original.Graph.Sites, S(rows[0], unknown)), StraightPassages = S(rows[0], unknown) };
        var read = JointSymmetryEvidence.Analyze(source);
        Assert.Equal(1, read.Components.Count);
        Assert.Equal(2, E(read.Components[0].Strokes).Length);
        Assert.True(E(read.Unresolved).Length != 0);
        Assert.False(read.Operations.Any(operation => operation.Exact));
    }

    private static NativeLatticeSources.SourceCase Source((int X, int Y, int U, int V)[] segments, bool turnModel = true)
    {
        var author = new LatticeLadderAuthor(); var sites = new Dictionary<(int, int), IElement>(); var bindings = new List<IElement>();
        IElement Site(int x, int y)
        {
            if (sites.TryGetValue((x, y), out var existing)) return existing;
            var site = new Placeholder(); sites.Add((x, y), site); bindings.Add(new Relation(site, author.Word(x, y))); return site;
        }
        var strokes = segments.Select(segment => (IElement)QualitativeStrokeGraph.Stroke(new Placeholder(),
            Site(segment.X, segment.Y), Site(segment.U, segment.V), author.Word(segment.U - segment.X, segment.V - segment.Y))).ToArray();
        var graph = new QualitativeStrokeGraph(author.Frame, S(sites.Values.ToArray()), S(strokes));
        return new("diagnostic", "No semantic name", "Authoring coordinates select native repeated participation only.", graph, S(bindings.ToArray()), S(strokes), S(), turnModel);
    }
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var rows, out _, out _) ? rows : [];
    private static Relation S(params IElement[] rows) => StructuralConstraint.Closed(rows);
}
