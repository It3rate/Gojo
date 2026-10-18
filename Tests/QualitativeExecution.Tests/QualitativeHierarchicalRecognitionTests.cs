using Gojo.Core;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;

namespace QualitativeExecution.Tests;

public sealed class QualitativeHierarchicalRecognitionTests : IClassFixture<QualitativeHierarchicalRecognitionTests.Fixtures>
{
    private readonly Fixtures fixtures;
    public QualitativeHierarchicalRecognitionTests(Fixtures fixtures) => this.fixtures = fixtures;

    public sealed class Fixtures
    {
        public Lazy<QualitativeHierarchicalRecognitionSession> Second { get; } = new(() => Session(4, 6, 2, 4, 2));
        public Lazy<QualitativeHierarchicalRecognitionSession> Third { get; } = new(() => Session(6, 8, 3, 6, 2));
        public Lazy<QualitativeHierarchicalRecognitionSession> Fourth { get; } = new(() => Session(8, 6, 4, 4, 2));
        public Lazy<QualitativeHierarchicalRecognitionSession> Contrary { get; } = new(() => new("test", "contrary", "", new(4, 6,
            (x, y) => x == 1 && y == 4 ? 2 : (x + y) % 2), 4, [(1, 4), (2, 5)], 2));
        public Lazy<QualitativeHierarchicalRecognitionSession> Budget { get; }
        public Lazy<QualitativeLayeredPatternFixture> OverlapFixture { get; } = new(() => new(4, 6, (x, y) => (x + y) % 2, overlappingUnit: (1, 4)));
        public Lazy<QualitativeHierarchicalRecognitionSession> Broad { get; }
        public Lazy<QualitativeHierarchicalRecognitionSession> Narrow { get; }
        public Fixtures()
        {
            Budget = new(() => new("test", "budget", "", Fourth.Value.Fixture, 4, [(1, 4), (2, 5)], 1));
            Broad = new(() => new("test", "broad", "", OverlapFixture.Value, 4, [(1, 4)], 1));
            Narrow = new(() => new("test", "narrow", "", OverlapFixture.Value, 4, [(1, 4)], 1,
                S(OverlapFixture.Value.Motifs.Families.Where(family => family.Kind == "cycle" && family.SegmentCount == 4)
                    .Select(family => (IElement)family.Read).ToArray())));
        }
        private static QualitativeHierarchicalRecognitionSession Session(int columns, int rows, int period, int trainingRows, int budget) =>
            new("test", "phase", "", new(columns, rows, (x, y) => (x + (period == 4 ? 2 * y : y)) % period),
                trainingRows, [(1, trainingRows), (2, trainingRows + 1)], budget);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Native_local_periods_lift_and_recur_in_fresh_row_participants(int period)
    {
        var session = period == 2 ? fixtures.Second.Value : period == 3 ? fixtures.Third.Value : fixtures.Fourth.Value;
        Assert.Contains(session.CellGrammar.Grammars, candidate => candidate.Period == period);
        Assert.Contains(session.RowGrammar.Grammars, candidate => candidate.Period == (period == 4 ? 2 : period));
        Assert.NotEmpty(E(session.CellBefore.Unresolved)); Assert.NotEmpty(E(session.RowBefore.Unresolved));
        Assert.NotEmpty(E(session.CellAfter.Supported)); Assert.NotEmpty(E(session.RowAfter.Supported));
        Assert.Empty(E(session.CellAfter.Demands)); Assert.Empty(E(session.RowAfter.Demands));
        Assert.All(session.FinalRowReads, row => Assert.True(Closed(row.Symbols)));
        var lowerNodes = E(session.Arrangement.Nodes); var upperNodes = E(session.Ladder.Rows);
        Assert.DoesNotContain(lowerNodes, lower => upperNodes.Any(upper => ReferenceEquals(lower, upper)));
        Assert.Equal(session.RowPaths.Count - 1, E(session.Ladder.UpperLinks).Length);
    }

    [Fact]
    public void New_information_is_requested_through_two_actual_causal_levels_only_for_hidden_cells()
    {
        var session = fixtures.Second.Value;
        Assert.Equal(E(session.HiddenNodes).Length, session.NewCellQueries);
        Assert.Equal(session.InitiallyCollectedCount + session.NewCellQueries, session.Arrangement.ActualQueryCount);
        Assert.Equal(E(session.HiddenNodes).Length, session.LowerRounds.Sum(round => E(round.NewFacts).Length));
        Assert.NotEmpty(session.LowerRounds);
        foreach (var lower in session.LowerRounds)
        {
            Assert.Contains(session.UpperRound.RequestReads, upper => ReferenceEquals(upper.Read, lower.ParentCause));
            Assert.Same(lower.ParentCause, lower.CausalBridge.Left); Assert.Same(lower.Before.Surface, lower.CausalBridge.Right);
            Assert.Contains(E(session.CausalRounds).Cast<Relation>(), row => ReferenceEquals(row.Left, lower.ParentCause) && ReferenceEquals(row.Right, lower.Surface));
            foreach (var request in lower.RequestReads)
            {
                Assert.Same(lower.CausalBridge, request.Cause);
                Assert.Contains(E(session.HiddenNodes), hidden => ReferenceEquals(hidden, request.Node));
                Assert.True(lower.Before.TryReadDemand(request.Obligation, out var actual)); Assert.Same(actual.Node, request.Node);
            }
        }
        Assert.NotEmpty(E(session.UpperRound.Reused));
    }

    [Fact]
    public void The_upper_grammar_and_service_answers_cannot_traverse_into_actual_lower_cell_identities()
    {
        var session = fixtures.Second.Value; var lower = E(session.Arrangement.Nodes);
        foreach (var root in new IElement[] { session.Ladder.Interface, session.InitialRows.Surface,
            session.RowGrammar.Surface, session.RowBefore.Surface, session.RowAfter.Surface, session.UpperRound.Surface })
        {
            var exposed = Reachable(root);
            Assert.All(lower, cell => Assert.DoesNotContain(cell, exposed));
            Assert.DoesNotContain(session.CellGrammar.TrainingSurface, exposed);
            Assert.DoesNotContain(session.Fixture.Observation.Surface, exposed);
        }
        Assert.All(session.InitialRowReads.Concat(session.FinalRowReads), row =>
            Assert.All(lower, cell => Assert.DoesNotContain(cell, Reachable(row.Surface))));
        Assert.Contains(session.LowerRounds[0].Surface, Reachable(session.AuditSurface));
    }

    [Fact]
    public void Both_frozen_program_scopes_remain_identical_across_refinement_and_exclude_heldout_nodes()
    {
        var session = fixtures.Third.Value;
        Assert.Same(session.CellGrammar, session.CellBefore.Grammar); Assert.Same(session.CellGrammar, session.CellAfter.Grammar);
        Assert.Same(session.RowGrammar, session.RowBefore.Grammar); Assert.Same(session.RowGrammar, session.RowAfter.Grammar);
        var heldCells = E(session.CellHeldoutPaths).Cast<Relation>().SelectMany(E).ToArray();
        var heldRows = E(session.RowHeldoutPaths).Cast<Relation>().SelectMany(E).ToArray();
        foreach (var candidate in session.CellGrammar.Grammars)
            Assert.All(heldCells, node => Assert.DoesNotContain(node, Reachable(candidate.Read)));
        foreach (var candidate in session.RowGrammar.Grammars)
            Assert.All(heldRows, node => Assert.DoesNotContain(node, Reachable(candidate.Read)));
        Assert.NotEmpty(E(session.CellBefore.Unresolved)); Assert.NotEmpty(E(session.RowBefore.Unresolved));
    }

    [Fact]
    public void A_contrary_local_motif_is_accepted_and_refutes_the_larger_description()
    {
        var session = fixtures.Contrary.Value;
        Assert.Equal(2, session.NewCellQueries); Assert.NotEmpty(E(session.CellAfter.Refuted)); Assert.NotEmpty(E(session.RowAfter.Refuted));
        Assert.Empty(E(session.RowAfter.Supported)); Assert.Empty(E(session.RowAfter.Demands));
        Assert.Contains(session.LowerRounds.SelectMany(round => E(round.NewFacts)).Cast<Relation>(), fact =>
            E((Relation)fact.Right).OfType<Relation>().Any(symbol => !session.CellGrammar.Grammars.Any(candidate =>
                E(candidate.Phases).Cast<Relation>().Any(phase => ReferenceEquals(phase.Right, symbol)))));
        Assert.Contains(session.FinalRowReads.Skip(session.TrainingRowCount), row => Closed(row.Symbols) && E(row.Symbols).Length == 0);
        Assert.Empty(E(session.UpperRound.Rejected)); Assert.All(session.LowerRounds, round => Assert.Empty(E(round.Rejected)));
    }

    [Fact]
    public void An_unpaid_query_stays_blocked_and_the_upper_description_stays_open()
    {
        var session = fixtures.Budget.Value;
        Assert.Equal(1, session.NewCellQueries); Assert.Empty(E(session.CellService.Available));
        Assert.Contains(session.LowerRounds, round => E(round.Blocked).Length != 0);
        Assert.NotEmpty(E(session.CellAfter.Unresolved)); Assert.NotEmpty(E(session.RowAfter.Unresolved)); Assert.Empty(E(session.RowAfter.Supported));
        Assert.Contains(session.FinalRowReads.Skip(session.TrainingRowCount), row => !Closed(row.Symbols));
        Assert.NotEmpty(E(session.UpperRound.Remaining));
    }

    [Fact]
    public void A_complete_local_alternative_inventory_does_not_become_a_certain_row_identity()
    {
        var session = fixtures.Broad.Value;
        var hidden = Assert.Single(E(session.HiddenNodes)); Assert.True(session.FinalCells.TryFindNode(hidden, out _, out var symbols));
        Assert.True(Closed(symbols)); Assert.Equal(2, E(symbols).Length);
        Assert.Equal(1, session.NewCellQueries); Assert.NotEmpty(E(session.CellAfter.Unresolved)); Assert.NotEmpty(E(session.RowAfter.Unresolved));
        Assert.Empty(E(session.RowAfter.Supported)); Assert.Contains(session.FinalRowReads, row => !Closed(row.Symbols));
        Assert.NotEmpty(E(session.UpperRound.Remaining));
    }

    [Fact]
    public void Changing_the_declared_question_can_resolve_a_read_without_changing_the_observed_field()
    {
        var broad = fixtures.Broad.Value; var narrow = fixtures.Narrow.Value;
        Assert.Same(broad.Fixture, narrow.Fixture); Assert.Same(broad.Fixture.Observation, narrow.Fixture.Observation);
        Assert.NotSame(broad.Arrangement.Question, narrow.Arrangement.Question);
        Assert.True(E(broad.Arrangement.PrototypeScope).Length > E(narrow.Arrangement.PrototypeScope).Length);
        Assert.Empty(E(broad.RowAfter.Supported)); Assert.NotEmpty(E(narrow.RowAfter.Supported)); Assert.Empty(E(narrow.RowAfter.Demands));
        var hidden = Assert.Single(E(narrow.HiddenNodes)); Assert.True(narrow.FinalCells.TryFindNode(hidden, out _, out var symbols));
        Assert.True(Closed(symbols)); Assert.Single(E(symbols)); Assert.Equal(1, narrow.NewCellQueries);
        var triangle = Assert.Single(narrow.Fixture.Motifs.Families, family => family.Kind == "cycle" && family.SegmentCount == 3);
        Assert.Contains(triangle.Definition, Reachable(narrow.Fixture.AuditSurface));
        Assert.DoesNotContain(E(narrow.Arrangement.PrototypeScope), definition => ReferenceEquals(definition, triangle.Definition));
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
