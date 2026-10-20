using System.Numerics;
using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;

namespace QualitativeExecution.Tests;

public sealed class OrnamentReconstructionSearchTests
{
    [Fact]
    public void Exhaustive_search_reports_actual_native_cost_for_every_subset_and_chooses_the_conditional_minimum()
    {
        var pool = Pool();
        BigInteger[] filters = [7, 3, 5, 25, 17, 9, 15, 21];
        BigInteger universe = 31; BigInteger goal = 1;
        var search = OrnamentReconstructionSearch.Run(pool, filters, filters, universe, goal, ["a", "b", "c", "d", "e"]);

        Assert.Equal(256, search.Enumerated); Assert.Equal(256, search.Options.Count);
        foreach (var option in search.Options)
        {
            var actual = OrnamentReconstructionSearch.Message(pool, option.Mask);
            Assert.True(actual.IsValid);
            Assert.Equal(actual.NativeCost, option.NativeCost);
            Assert.Equal(actual.HintCount, option.Hints);
            var admitted = universe;
            for (var i = 0; i < pool.Count; i++) if ((option.Mask & 1 << i) != 0) admitted &= filters[i];
            Assert.Equal(admitted, option.Admitted); Assert.Equal(admitted, option.Possible);
            Assert.Equal(admitted != 0 && (admitted & ~goal) == 0, option.Robust);
        }
        var expected = search.Options.Where(option => option.Robust).OrderBy(option =>
            OrnamentReconstructionSearch.Message(pool, option.Mask).NativeCost).ThenBy(option => option.Hints)
            .ThenBy(option => option.Mask).First();
        Assert.True(search.HasRobust); Assert.Equal(expected, search.Best);
    }

    [Fact]
    public void Greedy_selection_depends_on_public_focus_profiles_and_claim_masks_without_hidden_goal_answers()
    {
        var pool = Pool(); BigInteger[] filters = [7, 3, 5, 25, 17, 9, 15, 21];
        var profiles = new[] { "ab", "bc", "cd", "de", "ef" };
        var first = OrnamentReconstructionSearch.Run(pool, filters, filters, 31, 1, profiles);
        var differentHiddenGoal = OrnamentReconstructionSearch.Run(pool, filters, filters, 31, 16, profiles);

        Assert.Equal(first.GreedyMask, differentHiddenGoal.GreedyMask);
        Assert.Equal(first.Options.Select(option => option.NativeCost), differentHiddenGoal.Options.Select(option => option.NativeCost));
        Assert.Equal(first.Options.Select(option => option.Admitted), differentHiddenGoal.Options.Select(option => option.Admitted));
        Assert.NotEqual(first.Options.Select(option => option.Robust), differentHiddenGoal.Options.Select(option => option.Robust));
    }

    [Fact]
    public void An_unfinished_unsafe_alternative_prevents_a_false_sufficiency_guarantee()
    {
        var pool = Pool().Take(1).ToArray();
        var uncertain = OrnamentReconstructionSearch.Run(pool, [BigInteger.One], [new BigInteger(3)], 3, 1, ["safe", "?"]);
        var completed = OrnamentReconstructionSearch.Run(pool, [BigInteger.One], [BigInteger.One], 3, 1, ["safe", "unsafe"]);

        Assert.Equal(1, uncertain.Options[1].Admitted); Assert.Equal(3, uncertain.Options[1].Possible);
        Assert.False(uncertain.Options[1].Robust); Assert.False(uncertain.HasRobust);
        Assert.True(completed.Options[1].Robust); Assert.True(completed.HasRobust);
        Assert.False(OrnamentReconstructionSearch.Uniform(3, ["safe", "?"]));
    }

    [Fact]
    public void Eliminating_every_candidate_never_counts_as_a_robust_encoding()
    {
        var search = OrnamentReconstructionSearch.Run(Pool().Take(1).ToArray(), [BigInteger.Zero], [BigInteger.Zero], 3, 0,
            ["first", "second"]);
        Assert.Equal(0, search.Options[1].Admitted); Assert.Equal(0, search.Options[1].Possible);
        Assert.False(search.Options[1].Robust); Assert.False(search.HasRobust);
    }

    [Fact]
    public void Native_unknown_candidates_survive_possible_masks_and_prevent_oracle_guarantees()
    {
        var sample = Sample("SQ-PATH"); var hidden = Sample("SQ-SIDES");
        var first = new R.Input("published path", sample.Observation, sample.MechanicalGroups);
        var second = new R.Input("unpublished path grouping", hidden.Observation, StructuralConstraint.Open());
        Assert.True(R.TryDescribeGroup(first, (Relation)E(first.MechanicalGroups)[0], out var pattern, out _));
        var query = R.Query.WholePath("one closed mechanical path", pattern); var claim = new R.Claim(query, true);
        var bank = new R.Bank([first, second], [query]); var context = new R.Context([first, second], [query]);
        var projection = new OrnamentReconstructionProjection(bank, context, [query]);
        var search = OrnamentReconstructionSearch.Run([claim], [projection.Mask(claim)], [projection.PossibleMask(claim)],
            projection.Universe, 1, ["+", "?"]);

        Assert.Equal("Yes", projection.ReadAnswer(0, query)); Assert.Equal("Open", projection.ReadAnswer(1, query));
        Assert.Single(bank.Evaluate(new([claim]), context).Admissible);
        Assert.Equal(3, projection.Possible([claim])); Assert.False(search.HasRobust);
    }

    private static IReadOnlyList<R.Claim> Pool()
    {
        var square = Sample("SQ-PATH"); var diamond = Sample("DIAMOND"); var meander = Sample("MEANDER");
        var crossing = Sample("X-CROSS");
        CommunicationPattern Shape(OrnamentReconstructionCases.Sample sample)
        {
            var input = new R.Input("independent exemplar", sample.Observation, sample.MechanicalGroups);
            Assert.True(R.TryDescribeGroup(input, (Relation)E(input.MechanicalGroups)[0], out var pattern, out _)); return pattern;
        }
        var a = Shape(square); var b = Shape(diamond); var c = Shape(meander);
        var crossInput = new R.Input("encounter exemplar", crossing.Observation, crossing.MechanicalGroups);
        var edges = E(crossing.Observation.Segments).Cast<Relation>().ToArray();
        Assert.True(R.TryDescribeEdge(crossInput, edges[0], out var horizontal, out _));
        Assert.True(R.TryDescribeEdge(crossInput, edges[1], out var vertical, out _));
        return [new(R.Query.Motif("one local square", a), true), new(R.Query.WholePath("one square path", a), true),
            new(R.Query.Motif("one local diamond", b), true), new(R.Query.WholePath("one diamond path", b), false),
            new(R.Query.WholePath("one returning meander", c), true), new(R.Query.Motif("long visible meander", c), false),
            new(R.Query.Crossing("one declared encounter", horizontal, vertical), true),
            new(R.Query.Continuation("one declared continuation", horizontal, vertical), false)];
    }
    private static OrnamentReconstructionCases.Sample Sample(string family) => OrnamentReconstructionCases.Specs.Single(spec =>
        spec.Family == family && spec.Transform == 0 && spec.ReachRecipe == 0 && spec.Repeats == 1).Create();
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
