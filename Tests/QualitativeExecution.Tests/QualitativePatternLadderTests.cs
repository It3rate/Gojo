using Gojo.Core;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;

namespace QualitativeExecution.Tests;

public sealed class QualitativePatternLadderTests
{
    [Fact]
    public void Complete_ordered_cell_correspondences_create_fresh_upper_nodes_links_and_ports()
    {
        var fixture = new Fixture(); var first = fixture.Row(8); var second = fixture.Row(8, offset: 1); var third = fixture.Row(8);
        fixture.Vertical(first, second); fixture.Vertical(second, third);
        var grammar = fixture.Grammar(first); var ladder = new QualitativePatternLadder(grammar, S(first, second, third), S(fixture.VerticalLinks.ToArray()));
        Assert.Equal(3, E(ladder.Rows).Length); Assert.Equal(3, E(ladder.Ports).Length); Assert.Equal(2, E(ladder.UpperLinks).Length);
        var path = Assert.IsType<Relation>(Assert.Single(E(ladder.UpperPaths))); Assert.Equal(3, E(path).Length);
        Assert.Same(ladder.NodeForPath(first), E(path)[0]); Assert.Same(ladder.NodeForPath(second), E(path)[1]);
        Assert.True(ladder.TryPathForNode(ladder.NodeForPath(third), out var actual)); Assert.Same(third, actual);
        foreach (var witness in E(ladder.AdjacencyWitnesses).Cast<Relation>())
        { Assert.Equal(8, E((Relation)witness.Right).Length); Assert.All(E((Relation)witness.Right).Cast<Relation>(), pair => Assert.Single(E((Relation)pair.Right))); }
    }

    [Fact]
    public void Seven_of_eight_vertical_proofs_cannot_create_a_row_connection()
    {
        var fixture = new Fixture(); var first = fixture.Row(8); var second = fixture.Row(8);
        fixture.Vertical(first, second, omitLast: true);
        var ladder = new QualitativePatternLadder(fixture.Grammar(first), S(first, second), S(fixture.VerticalLinks.ToArray()));
        Assert.Empty(E(ladder.UpperLinks)); Assert.NotEmpty(E(ladder.RejectedAdjacency));
        Assert.All(E(ladder.UpperPaths).Cast<Relation>(), path => Assert.Single(E(path)));
    }

    [Fact]
    public void Different_lengths_and_shared_actual_cells_cannot_become_parallel_row_transport()
    {
        var fixture = new Fixture(); var first = fixture.Row(8); var shortRow = fixture.Row(7);
        var overlapping = S(E(first).Reverse().ToArray()); fixture.Vertical(first, shortRow, omitLast: true);
        var ladder = new QualitativePatternLadder(fixture.Grammar(first), S(first, shortRow, overlapping), S(fixture.VerticalLinks.ToArray()));
        Assert.Empty(E(ladder.UpperLinks)); Assert.NotEmpty(E(ladder.RejectedAdjacency));
    }

    [Fact]
    public void Public_interface_and_row_reply_never_reach_lower_occurrences_or_private_matches()
    {
        var fixture = new Fixture(); var first = fixture.Row(8); var second = fixture.Row(8); fixture.Vertical(first, second);
        var grammar = fixture.Grammar(first); var ladder = new QualitativePatternLadder(grammar, S(first, second), S(fixture.VerticalLinks.ToArray()));
        var evaluation = grammar.Evaluate(fixture.Graph(), S(second));
        var reply = ladder.ReadRow(ladder.NodeForPath(second), grammar.Training, evaluation);
        var publicInterface = Reachable(ladder.Interface); var publicReply = Reachable(reply.Surface);
        foreach (var cell in fixture.Nodes)
        { Assert.DoesNotContain(cell, publicInterface); Assert.DoesNotContain(cell, publicReply); }
        Assert.DoesNotContain(first, publicInterface); Assert.DoesNotContain(second, publicReply);
        Assert.DoesNotContain(grammar.TrainingSurface, publicReply); Assert.DoesNotContain(evaluation.Surface, publicReply);
        Assert.Contains(second, Reachable(reply.AuditSurface)); Assert.Contains(evaluation.Surface, Reachable(reply.AuditSurface));
        Assert.Same(reply.Symbols, ((Relation)reply.Surface.Right).Left);
    }

    [Fact]
    public void Shorter_description_dominates_only_the_same_expected_sequence_and_keeps_both_private_reads()
    {
        var fixture = new Fixture(); var first = fixture.Row(8); var second = fixture.Row(8);
        var grammar = fixture.Grammar(first); var ladder = new QualitativePatternLadder(grammar, S(first, second), S());
        var evaluation = grammar.Evaluate(fixture.Graph(), S(second)); var reply = ladder.ReadRow(ladder.NodeForPath(second), evaluation);
        Assert.NotEmpty(E(reply.Dominance)); Assert.True(Closed(reply.Symbols)); Assert.Single(E(reply.Symbols));
        Assert.True(E(reply.PrivateMatches).Length > E(reply.Preferred).Length);
        var preferred = Assert.Single(E(reply.Preferred));
        var actual = Assert.Single(evaluation.Matches, match => ReferenceEquals(match.Read, preferred)); Assert.Equal(2, actual.Candidate.Period);
        Assert.Same(actual.FramedSymbol, E(reply.Symbols)[0]);
        foreach (var witness in E(reply.Dominance).Cast<Relation>())
        {
            var pair = (Relation)witness.Left;
            var small = evaluation.Matches.Single(match => ReferenceEquals(match.Read, pair.Left));
            var large = evaluation.Matches.Single(match => ReferenceEquals(match.Read, pair.Right));
            Assert.True(small.Candidate.Period < large.Candidate.Period);
            Assert.All(small.Obligations.Zip(large.Obligations), peers => { Assert.Same(peers.First.Expected, peers.Second.Expected); Assert.Same(peers.First.NodeRow, peers.Second.NodeRow); });
        }
    }

    [Fact]
    public void Compatible_but_unresolved_lower_matches_publish_open_row_symbols()
    {
        var fixture = new Fixture(); var first = fixture.Row(8); var second = fixture.Row(8, unknownFirst: true);
        var grammar = fixture.Grammar(first); var ladder = new QualitativePatternLadder(grammar, S(first, second), S());
        var reply = ladder.ReadRow(ladder.NodeForPath(second), grammar.Evaluate(fixture.Graph(), S(second)));
        Assert.False(Closed(reply.Symbols)); Assert.NotEmpty(E(reply.Symbols)); Assert.NotEmpty(E(reply.Preferred));
    }

    [Fact]
    public void A_short_exact_prefix_does_not_become_a_two_cycle_row_definition()
    {
        var fixture = new Fixture(); var first = fixture.Row(8); var shortRow = fixture.Row(3);
        var grammar = fixture.Grammar(first); var ladder = new QualitativePatternLadder(grammar, S(first, shortRow), S());
        var evaluation = grammar.Evaluate(fixture.Graph(), S(shortRow));
        Assert.Contains(evaluation.Matches, match => match.IsSupported);
        var reply = ladder.ReadRow(ladder.NodeForPath(shortRow), evaluation);
        Assert.False(Closed(reply.Symbols)); Assert.NotEmpty(E(reply.Symbols));
        Assert.Contains(E(reply.Coverage).Cast<Relation>(), row => E((Relation)((Relation)row.Right).Right).Length != 0);
    }

    [Fact]
    public void Rejected_known_rows_publish_empty_only_relative_to_the_frozen_program_scope()
    {
        var fixture = new Fixture(); var first = fixture.Row(8); var second = fixture.Row(8, foreignFirst: true);
        var grammar = fixture.Grammar(first); var ladder = new QualitativePatternLadder(grammar, S(first, second), S());
        var reply = ladder.ReadRow(ladder.NodeForPath(second), grammar.Evaluate(fixture.Graph(), S(second)));
        Assert.True(Closed(reply.Symbols)); Assert.Empty(E(reply.Symbols)); Assert.Empty(E(reply.Preferred)); Assert.NotEmpty(E(reply.PrivateMatches));
        Assert.Equal(grammar.Grammars.Sum(candidate => E(candidate.FramedSymbols).Length), E((Relation)ladder.Question.Left).Length);
    }

    [Fact]
    public void An_unqueried_or_copied_path_is_unavailable_instead_of_a_negative_row_answer()
    {
        var fixture = new Fixture(); var first = fixture.Row(8); var second = fixture.Row(8);
        var copy = S(E(second)); var grammar = fixture.Grammar(first); var ladder = new QualitativePatternLadder(grammar, S(first, second, copy), S());
        var evaluation = grammar.Evaluate(fixture.Graph(), S(second));
        Assert.False(Closed(ladder.ReadRow(ladder.NodeForPath(second)).Symbols));
        var reply = ladder.ReadRow(ladder.NodeForPath(copy), evaluation);
        Assert.False(Closed(reply.Symbols)); Assert.Empty(E(reply.Symbols));
    }

    [Fact]
    public void Different_unknown_expected_sequences_are_not_dominated_merely_by_lower_description_cost()
    {
        var fixture = new Fixture(); var constant = fixture.Row(8, constant: true); var alternating = fixture.Row(8); var unknown = fixture.Row(8, allUnknown: true);
        var graph = fixture.Graph(); var grammar = new QualitativePatternGrammar(graph, S(constant, alternating));
        var ladder = new QualitativePatternLadder(grammar, S(constant, alternating, unknown), S());
        var evaluation = grammar.Evaluate(graph, S(unknown)); var reply = ladder.ReadRow(ladder.NodeForPath(unknown), evaluation);
        var preferred = evaluation.Matches.Where(match => E(reply.Preferred).Any(row => ReferenceEquals(row, match.Read))).ToArray();
        Assert.Contains(preferred, match => match.Candidate.Period == 1); Assert.Contains(preferred, match => match.Candidate.Period == 2);
        Assert.False(Closed(reply.Symbols));
    }

    [Fact]
    public void Different_observation_revisions_do_not_supply_identical_premises_for_dominance()
    {
        var fixture = new Fixture(); var first = fixture.Row(8); var second = fixture.Row(8, unknownFirst: true);
        var grammar = fixture.Grammar(first); var ladder = new QualitativePatternLadder(grammar, S(first, second), S());
        var oldGraph = fixture.Graph(); var old = grammar.Evaluate(oldGraph, S(second));
        var firstCell = E(second)[0]; var newRows = E(oldGraph.Nodes).Cast<Relation>().Select(row => ReferenceEquals(row.Left, firstCell) ? new Relation(row.Left, S(fixture.A)) : row).ToArray();
        var revised = new QualitativePatternTokenGraph(S(newRows), oldGraph.Links); var current = grammar.Evaluate(revised, S(second));
        var reply = ladder.ReadRow(ladder.NodeForPath(second), old, current);
        Assert.False(Closed(reply.Symbols));
        foreach (var dominance in E(reply.Dominance).Cast<Relation>())
        {
            var pair = (Relation)dominance.Left;
            Assert.True(old.Matches.Any(match => ReferenceEquals(match.Read, pair.Left)) == old.Matches.Any(match => ReferenceEquals(match.Read, pair.Right)));
        }
    }

    [Fact]
    public void A_row_reply_refines_with_the_new_evaluation_while_the_previous_reply_remains_open()
    {
        var fixture = new Fixture(); var first = fixture.Row(8); var second = fixture.Row(8, unknownFirst: true);
        var grammar = fixture.Grammar(first); var ladder = new QualitativePatternLadder(grammar, S(first, second), S());
        var oldGraph = fixture.Graph(); var old = grammar.Evaluate(oldGraph, S(second)); var node = ladder.NodeForPath(second);
        var before = ladder.ReadRow(node, old); var firstCell = E(second)[0];
        var newRows = E(oldGraph.Nodes).Cast<Relation>().Select(row => ReferenceEquals(row.Left, firstCell) ? new Relation(row.Left, S(fixture.A)) : row).ToArray();
        var current = grammar.Evaluate(new(S(newRows), oldGraph.Links), S(second)); var after = ladder.ReadRow(node, current);
        Assert.False(Closed(before.Symbols)); Assert.True(Closed(after.Symbols)); Assert.Single(E(after.Symbols));
        Assert.NotSame(before.Revision, after.Revision); Assert.Same(before.Node, after.Node);
    }

    private sealed class Fixture
    {
        public Relation A { get; } = new(new Placeholder(), new Relation(new Placeholder(), new Placeholder()));
        public Relation B { get; } = new(new Placeholder(), new Relation(new Placeholder(), new Placeholder()));
        public Relation C { get; } = new(new Placeholder(), new Relation(new Placeholder(), new Placeholder()));
        public List<IElement> Nodes { get; } = [];
        public List<IElement> NodeRows { get; } = [];
        public List<IElement> Links { get; } = [];
        public List<IElement> VerticalLinks { get; } = [];
        public Relation Row(int count, int offset = 0, bool unknownFirst = false, bool foreignFirst = false, bool constant = false, bool allUnknown = false)
        {
            var nodes = Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray(); Nodes.AddRange(nodes);
            for (var i = 0; i < count; i++)
            {
                var symbols = allUnknown || i == 0 && unknownFirst ? StructuralConstraint.Open() : S(i == 0 && foreignFirst ? C : constant || (i + offset) % 2 == 0 ? A : B);
                NodeRows.Add(new Relation(nodes[i], symbols));
                if (i != 0) Links.Add(new Relation(new Placeholder(), new Relation(nodes[i - 1], nodes[i])));
            }
            return S(nodes);
        }
        public void Vertical(Relation from, Relation to, bool omitLast = false)
        {
            var a = E(from); var b = E(to);
            for (var i = 0; i < Math.Min(a.Length, b.Length) - (omitLast ? 1 : 0); i++)
                VerticalLinks.Add(new Relation(new Placeholder(), new Relation(a[i], b[i])));
        }
        public QualitativePatternTokenGraph Graph() => new(S(NodeRows.ToArray()), S(Links.ToArray()));
        public QualitativePatternGrammar Grammar(Relation training) => new(Graph(), S(training), seedCapacity: 2000, retainedCapacity: 64);
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var rows, out _, out _) ? rows : [];
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static HashSet<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var item))
        { if (!seen.Add(item) || item is not Relation row) continue; stack.Push(row.Left); stack.Push(row.Right); }
        return seen;
    }
}
