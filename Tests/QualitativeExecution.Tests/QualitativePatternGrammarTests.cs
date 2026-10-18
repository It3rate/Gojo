using Gojo.Core;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;

namespace QualitativeExecution.Tests;

public sealed class QualitativePatternGrammarTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Two_actual_cycles_induce_native_phase_graphs_and_read_disjoint_recurrence(int period)
    {
        var symbols = Enumerable.Range(0, period).Select(_ => Symbol()).ToArray();
        var training = Chain(Enumerable.Range(0, 2 * period).Select(index => S(symbols[index % period])).ToArray());
        var grammar = new QualitativePatternGrammar(training.Graph, S(training.Path));
        var candidate = Assert.Single(grammar.Grammars, candidate => candidate.Period == period);
        Assert.True(QualitativePatternGrammar.TryReadPattern(candidate.Pattern, out var phases, out var successors));
        Assert.Equal(period, phases.Length); Assert.Equal(period, successors.Length);
        Assert.NotEmpty(E(candidate.TrainingWitnesses)); Assert.Equal(2 * period, E(candidate.Description).Length);
        var heldout = Chain(Enumerable.Range(0, 3 * period).Select(index => S(symbols[index % period])).ToArray());
        var read = grammar.Evaluate(heldout.Graph, S(heldout.Path));
        var match = Assert.Single(read.Matches, match => ReferenceEquals(match.Candidate, candidate) && match.IsSupported);
        Assert.Equal(3 * period, E(match.PhaseBindings).Length);
        Assert.Same(candidate.Pattern, match.Pattern); Assert.Equal(3 * period, match.Obligations.Count);
        Assert.All(match.Obligations, obligation => Assert.True(obligation.IsSupported));
        Assert.All(E(match.PhaseBindings).Cast<Relation>(), binding => Assert.Contains(phases, phase => ReferenceEquals(phase.Left, binding.Right)));
    }

    [Fact]
    public void Short_and_long_repeating_descriptions_remain_distinct_native_candidates()
    {
        var a = Symbol(); var b = Symbol(); var training = Chain(S(a), S(b), S(a), S(b), S(a), S(b), S(a), S(b));
        var grammar = new QualitativePatternGrammar(training.Graph, S(training.Path));
        Assert.Contains(grammar.Grammars, candidate => candidate.Period == 2);
        Assert.Contains(grammar.Grammars, candidate => candidate.Period == 4);
        Assert.All(grammar.Grammars, candidate => Assert.True(grammar.TryReadCandidate(candidate.Read, out _)));
    }

    [Fact]
    public void Cycle_rotations_share_a_pattern_while_start_phase_keeps_the_row_read_distinct()
    {
        var a = Symbol(); var b = Symbol(); var training = Chain(S(a), S(b), S(a), S(b), S(a), S(b));
        var grammar = new QualitativePatternGrammar(training.Graph, S(training.Path));
        var candidate = Assert.Single(grammar.Grammars, candidate => candidate.Period == 2);
        foreach (var witness in E(candidate.TrainingWitnesses).Cast<Relation>())
            foreach (var pair in E((Relation)((Relation)witness.Right).Right).Cast<Relation>())
            {
                var phase = (Relation)pair.Left; var actualRows = (Relation)pair.Right;
                Assert.Contains(E((Relation)((Relation)actualRows.Left).Right), symbol => ReferenceEquals(symbol, phase.Right));
                Assert.Contains(E((Relation)((Relation)actualRows.Right).Right), symbol => ReferenceEquals(symbol, phase.Right));
            }
        var first = Chain(S(a), S(b), S(a), S(b)); var second = Chain(S(b), S(a), S(b), S(a));
        var one = Assert.Single(grammar.Evaluate(first.Graph, S(first.Path)).Matches, match => match.IsSupported && ReferenceEquals(match.Candidate, candidate));
        var two = Assert.Single(grammar.Evaluate(second.Graph, S(second.Path)).Matches, match => match.IsSupported && ReferenceEquals(match.Candidate, candidate));
        Assert.Same(one.Pattern, two.Pattern); Assert.NotSame(one.StartPhase, two.StartPhase);
        Assert.NotSame(one.FramedSymbol, two.FramedSymbol); Assert.Same(one.Pattern, one.FramedSymbol.Left);
        Assert.Same(one.StartPhase, one.FramedSymbol.Right);
        var again = Assert.Single(grammar.Evaluate(first.Graph, S(first.Path)).Matches, match => match.IsSupported && ReferenceEquals(match.Candidate, candidate));
        Assert.Same(one.FramedSymbol, again.FramedSymbol);
    }

    [Fact]
    public void Frozen_candidates_and_seed_witnesses_never_read_heldout_symbols_or_nodes()
    {
        var a = Symbol(); var b = Symbol(); var foreign = Symbol(); var training = Chain(S(a), S(b), S(a), S(b));
        var grammar = new QualitativePatternGrammar(training.Graph, S(training.Path));
        var before = grammar.Grammars.Select(candidate => candidate.Read).ToArray();
        var heldout = Chain(S(foreign), S(foreign), S(foreign));
        var read = grammar.Evaluate(heldout.Graph, S(heldout.Path));
        Assert.Equal(before, grammar.Grammars.Select(candidate => candidate.Read).ToArray());
        foreach (var candidate in grammar.Grammars)
        {
            var graph = Reachable(candidate.Read); Assert.DoesNotContain(foreign, graph);
            Assert.All(heldout.Nodes, node => Assert.DoesNotContain(node, graph));
        }
        Assert.Empty(E(read.Supported)); Assert.NotEmpty(E(read.Refuted));
    }

    [Fact]
    public void Heldout_comparison_rejects_reuse_of_training_occurrence_identity()
    {
        var a = Symbol(); var training = Chain(S(a), S(a));
        var grammar = new QualitativePatternGrammar(training.Graph, S(training.Path));
        var read = grammar.Evaluate(training.Graph, S(training.Path));
        Assert.Empty(read.Matches); Assert.NotEmpty(E(read.Blocked)); Assert.False(read.IsComplete);
        Assert.NotEmpty(E(grammar.Training.Supported));
    }

    [Fact]
    public void One_open_token_creates_an_actual_downward_demand_in_the_plausible_phase()
    {
        var a = Symbol(); var b = Symbol(); var training = Chain(S(a), S(b), S(a), S(b));
        var grammar = new QualitativePatternGrammar(training.Graph, S(training.Path));
        var heldout = Chain(StructuralConstraint.Open(), S(b), S(a), S(b));
        var read = grammar.Evaluate(heldout.Graph, S(heldout.Path));
        var demand = Assert.IsType<Relation>(Assert.Single(E(read.Demands)));
        Assert.True(read.TryReadDemand(demand, out var obligation)); Assert.True(obligation.IsUnresolved);
        Assert.Same(heldout.Nodes[0], obligation.Node); Assert.Same(a, obligation.Expected);
        Assert.Same(obligation.Read, demand.Left);
        Assert.False(read.TryReadDemand(new Relation(demand.Left, demand.Right), out _));
        Assert.Single(read.Matches, match => match.IsUnresolved); Assert.Single(read.Matches, match => match.IsRefuted);
    }

    [Fact]
    public void Closed_multiple_candidates_preserve_possibility_and_do_not_promote_it_to_certainty()
    {
        var a = Symbol(); var b = Symbol(); var training = Chain(S(a), S(a));
        var grammar = new QualitativePatternGrammar(training.Graph, S(training.Path));
        var heldout = Chain(S(a, b), S(a)); var read = grammar.Evaluate(heldout.Graph, S(heldout.Path));
        Assert.Empty(E(read.Supported)); Assert.Empty(E(read.Refuted)); Assert.Single(E(read.Unresolved)); Assert.Single(E(read.Demands));
    }

    [Fact]
    public void Closed_exclusion_is_a_counterexample_while_an_open_alternative_prefix_is_not()
    {
        var a = Symbol(); var b = Symbol(); var training = Chain(S(a), S(a));
        var grammar = new QualitativePatternGrammar(training.Graph, S(training.Path));
        var closed = Chain(S(b)); var open = Chain(StructuralConstraint.Open(b));
        var no = grammar.Evaluate(closed.Graph, S(closed.Path)); var unknown = grammar.Evaluate(open.Graph, S(open.Path));
        Assert.Single(E(no.Refuted)); Assert.Empty(E(no.Demands));
        Assert.Empty(E(unknown.Refuted)); Assert.Single(E(unknown.Unresolved)); Assert.Single(E(unknown.Demands));
    }

    [Fact]
    public void A_closed_empty_symbol_inventory_refutes_instead_of_inventing_an_unknown_token()
    {
        var a = Symbol(); var training = Chain(S(a), S(a)); var grammar = new QualitativePatternGrammar(training.Graph, S(training.Path));
        var heldout = Chain(S()); var read = grammar.Evaluate(heldout.Graph, S(heldout.Path));
        Assert.Single(E(read.Refuted)); Assert.Empty(E(read.Demands));
    }

    [Fact]
    public void Token_refinement_reuses_frozen_candidates_and_preserves_the_previous_unknown_read()
    {
        var a = Symbol(); var b = Symbol(); var training = Chain(S(a), S(b), S(a), S(b));
        var grammar = new QualitativePatternGrammar(training.Graph, S(training.Path));
        var heldout = Chain(StructuralConstraint.Open(), S(b), S(a), S(b));
        var before = grammar.Evaluate(heldout.Graph, S(heldout.Path));
        var rows = E(heldout.Graph.Nodes).Cast<Relation>().Select(row => ReferenceEquals(row.Left, heldout.Nodes[0]) ? new Relation(row.Left, S(a)) : row).ToArray();
        var revised = new QualitativePatternTokenGraph(S(rows), heldout.Graph.Links);
        var after = grammar.Evaluate(revised, S(heldout.Path));
        Assert.Single(E(before.Unresolved)); Assert.NotEmpty(E(before.Demands)); Assert.Single(E(after.Supported)); Assert.Empty(E(after.Demands));
        Assert.All(after.Matches, match => Assert.Contains(grammar.Grammars, candidate => ReferenceEquals(candidate.Pattern, match.Pattern)));
        Assert.Same(heldout.Graph, before.Graph); Assert.Same(revised, after.Graph);
    }

    [Fact]
    public void Native_seed_and_retention_capacities_expose_the_unsearched_scope()
    {
        var a = Symbol(); var training = Chain(S(a), S(a), S(a), S(a));
        var seedCut = new QualitativePatternGrammar(training.Graph, S(training.Path), seedCapacity: 0);
        var retentionCut = new QualitativePatternGrammar(training.Graph, S(training.Path), retainedCapacity: 0);
        Assert.Empty(seedCut.Grammars); Assert.NotEmpty(E(seedCut.Unsearched)); Assert.False(seedCut.IsComplete);
        Assert.Empty(retentionCut.Grammars); Assert.NotEmpty(E(retentionCut.Unsearched)); Assert.NotEmpty(E(retentionCut.Spent));
    }

    [Fact]
    public void One_observed_period_is_insufficient_to_induce_its_cycle()
    {
        var a = Symbol(); var b = Symbol(); var training = Chain(S(a), S(b));
        var grammar = new QualitativePatternGrammar(training.Graph, S(training.Path));
        Assert.Empty(grammar.Grammars); Assert.Empty(E(grammar.Unsearched)); Assert.NotEmpty(E(grammar.RejectedSeeds));
    }

    [Fact]
    public void A_budget_cut_inside_the_last_ambiguous_seed_does_not_claim_exhaustive_proposal_search()
    {
        var a = Symbol(); var b = Symbol(); var training = Chain(S(a, b), S(a, b));
        var grammar = new QualitativePatternGrammar(training.Graph, S(training.Path), maxPeriod: 1, seedCapacity: 2);
        Assert.Single(grammar.Grammars); Assert.NotEmpty(E(grammar.Unsearched)); Assert.False(grammar.IsComplete);
        Assert.Contains(Reachable(grammar.Unsearched), element => ReferenceEquals(element, b));
    }

    [Fact]
    public void A_supplied_node_order_without_actual_link_proofs_cannot_create_training_recurrence()
    {
        var a = Symbol(); var training = Chain(S(a), S(a));
        var graph = new QualitativePatternTokenGraph(training.Graph.Nodes, S());
        var grammar = new QualitativePatternGrammar(graph, S(training.Path));
        Assert.Empty(grammar.Grammars); Assert.NotEmpty(E(grammar.Blocked));
    }

    [Fact]
    public void Duplicate_occurrence_roles_cannot_masquerade_as_two_training_cycles()
    {
        var a = Symbol(); var training = Chain(S(a), S(a));
        var grammar = new QualitativePatternGrammar(training.Graph, S(S(training.Nodes[0], training.Nodes[1], training.Nodes[0], training.Nodes[1])));
        Assert.Empty(grammar.Grammars); Assert.NotEmpty(E(grammar.Blocked));
    }

    [Fact]
    public void Graph_branching_is_retained_instead_of_being_hidden_by_a_selected_route()
    {
        var a = Symbol(); var training = Chain(S(a), S(a), S(a)); var extra = new Placeholder();
        var graph = new QualitativePatternTokenGraph(S(E(training.Graph.Nodes).Append(new Relation(extra, S(a))).ToArray()),
            S(E(training.Graph.Links).Append(new Relation(new Placeholder(), new Relation(training.Nodes[0], extra))).ToArray()));
        var grammar = new QualitativePatternGrammar(graph, S(training.Path));
        Assert.Single(E(graph.Branching)); Assert.Single(E(grammar.Branching)); Assert.NotEmpty(grammar.Grammars);
    }

    [Fact]
    public void Pattern_and_actual_read_parsers_do_not_accept_copied_candidate_or_demand_captures()
    {
        var a = Symbol(); var training = Chain(S(a), S(a)); var grammar = new QualitativePatternGrammar(training.Graph, S(training.Path));
        var candidate = Assert.Single(grammar.Grammars);
        Assert.False(grammar.TryReadCandidate(new Relation(candidate.Read.Left, candidate.Read.Right), out _));
        var heldout = Chain(S(a)); var read = grammar.Evaluate(heldout.Graph, S(heldout.Path)); var match = Assert.Single(read.Matches);
        Assert.True(read.TryReadMatch(match.Read, out _)); Assert.False(read.TryReadMatch(new Relation(match.Read.Left, match.Read.Right), out _));
    }

    [Fact]
    public void The_same_engine_reads_framed_lower_patterns_as_upper_tokens()
    {
        var a = Symbol(); var b = Symbol(); var lowerTrain = Chain(S(a), S(b), S(a), S(b));
        var lower = new QualitativePatternGrammar(lowerTrain.Graph, S(lowerTrain.Path));
        var frames = E(Assert.Single(lower.Grammars).FramedSymbols);
        var upperTrain = Chain(S(frames[0]), S(frames[1]), S(frames[0]), S(frames[1]));
        var upper = new QualitativePatternGrammar(upperTrain.Graph, S(upperTrain.Path));
        var heldout = Chain(S(frames[1]), S(frames[0]), S(frames[1]), S(frames[0]));
        var read = upper.Evaluate(heldout.Graph, S(heldout.Path)); Assert.Single(E(read.Supported));
        var match = Assert.Single(read.Matches, match => match.IsSupported);
        Assert.All(match.Obligations, obligation => Assert.Contains(frames, frame => ReferenceEquals(frame, obligation.Expected)));
    }

    private static Relation Symbol() => new(new Placeholder(), new Relation(new Placeholder(), new Placeholder()));
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var rows, out _, out _) ? rows : [];
    private static (QualitativePatternTokenGraph Graph, Relation Path, IElement[] Nodes) Chain(params Relation[] possibilities)
    {
        var nodes = possibilities.Select(_ => (IElement)new Placeholder()).ToArray();
        var rows = nodes.Select((node, i) => (IElement)new Relation(node, possibilities[i])).ToArray();
        var links = Enumerable.Range(1, nodes.Length - 1).Select(i => (IElement)new Relation(new Placeholder(), new Relation(nodes[i - 1], nodes[i]))).ToArray();
        return (new(S(rows), S(links)), S(nodes), nodes);
    }
    private static HashSet<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var item))
        { if (!seen.Add(item) || item is not Relation row) continue; stack.Push(row.Left); stack.Push(row.Right); }
        return seen;
    }
}
