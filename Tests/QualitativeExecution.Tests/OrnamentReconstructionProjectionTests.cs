using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;
using Xunit.Abstractions;

namespace QualitativeExecution.Tests;

public sealed class OrnamentReconstructionProjectionTests(ITestOutputHelper output)
{
    [Fact]
    public void Shared_codebook_increment_counts_only_new_claim_and_message_participation()
    {
        var queries = OrnamentReconstructionVocabulary.All.Select(entry => entry.Query).ToArray();
        var known = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        foreach (var query in queries) known.UnionWith(R.Participants(query.Surface));
        var codebook = StructuralConstraint.Closed(queries.Select(query => (IElement)query.Surface).ToArray());
        var totalCodebook = R.Participants(codebook).Count;
        Assert.Equal(known.Count + queries.Length + 2, totalCodebook);
        var pool = queries.Take(5).Select((query, index) => new R.Claim(query, index % 2 == 0)).ToArray();
        for (var mask = 0; mask < 32; mask++)
        {
            var claims = pool.Where((_, index) => (mask & 1 << index) != 0).ToArray();
            var message = new R.Message(claims);
            var increment = R.Participants(message.Surface).Count(participant => !known.Contains(participant));
            Assert.Equal(8 * claims.Length + 2, increment);
            Assert.True(message.NativeCost >= increment);
        }
        output.WriteLine($"Queries: {queries.Length}; query graph union: {known.Count}; closed codebook: {totalCodebook}; incremental message: 8 * hints + 2.");
    }

    [Fact]
    public void Every_subset_of_five_native_claims_matches_the_prepared_backend()
    {
        var sampleSpecs = new[] { "SQ-PATH", "SQ-SIDES", "SQ-OPEN", "MEANDER", "X-CROSS", "X-UNRELATED" }
            .Select(family => OrnamentReconstructionCases.Specs.First(spec => spec.Family == family && spec.Transform == 0 && spec.ReachRecipe == 0 && spec.Repeats == 1)).ToArray();
        var samples = sampleSpecs.Select(spec => spec.Create()).ToArray();
        var inputs = samples.Select(sample => new R.Input(sample.Spec.Name, sample.Observation, sample.MechanicalGroups)).ToArray();
        Assert.True(R.TryDescribeGroup(inputs[0], (Relation)FiniteLaw.Entries(inputs[0].MechanicalGroups)[0], out var square, out _));
        Assert.True(R.TryDescribeGroup(inputs[3], (Relation)FiniteLaw.Entries(inputs[3].MechanicalGroups)[0], out var meander, out _));
        var crossingEdges = FiniteLaw.Entries(inputs[4].Observation.Segments).Cast<Relation>().ToArray();
        Assert.True(R.TryDescribeEdge(inputs[4], crossingEdges[0], out var first, out _));
        Assert.True(R.TryDescribeEdge(inputs[4], crossingEdges[1], out var second, out _));
        var queries = new[] { R.Query.Motif("square", square), R.Query.WholePath("one square path", square),
            R.Query.Motif("long meander outside visible grammar", meander), R.Query.Crossing("declared encounter", first, second),
            R.Query.Continuation("continuation", first, second) };
        var claims = queries.Select((query, index) => new R.Claim(query, index is 0 or 1 or 3)).ToArray();
        var bank = new R.Bank(inputs, queries); var context = new R.Context(inputs, queries);
        var prepared = new OrnamentReconstructionProjection(bank, context, queries);
        for (var subset = 0; subset < 32; subset++)
        {
            var selected = claims.Where((_, index) => (subset & 1 << index) != 0).ToArray();
            var native = bank.Evaluate(new(selected), context);
            var expected = native.Admissible.Select(candidate => candidate.Input).ToArray();
            var actual = prepared.Indices(prepared.Admitted(selected)).Select(index => inputs[index]).ToArray();
            Assert.Equal(expected, actual);
            var possible = native.Candidates.Where(candidate => FiniteLaw.Entries(candidate.Refuted).Length == 0 &&
                FiniteLaw.Entries(candidate.Blocked).Length == 0).Select(candidate => candidate.Input).ToArray();
            Assert.Equal(possible, prepared.Indices(prepared.Possible(selected)).Select(index => inputs[index]).ToArray());
        }
        Assert.Equal("Open", prepared.ReadAnswer(0, queries[2]));
        Assert.Equal("Yes", prepared.ReadAnswer(4, queries[3])); Assert.Equal("No", prepared.ReadAnswer(5, queries[3]));
        var alias = new R.Query("new wrapper, same actual query", queries[3].Surface);
        Assert.Equal(prepared.Mask(new(queries[3], true)), prepared.Mask(new(alias, true)));
        Assert.Equal(0, prepared.Admitted([new(queries[0], true), new(queries[0], false)]));
        Assert.Equal(0, prepared.Possible([new(queries[0], true), new(queries[0], false)]));
    }

    [Fact]
    public void Missing_capability_cannot_supply_a_negative_mask_or_a_hidden_read()
    {
        var sample = OrnamentReconstructionCases.Specs[0].Create();
        var input = new R.Input("one panel", sample.Observation, sample.MechanicalGroups);
        Assert.True(R.TryDescribeGroup(input, (Relation)FiniteLaw.Entries(input.MechanicalGroups)[0], out var shape, out _));
        var query = R.Query.WholePath("one contour", shape); var bank = new R.Bank([input], [query]);
        var context = new R.Context([input], []); var prepared = new OrnamentReconstructionProjection(bank, context, [query]);
        Assert.Equal(0, bank.EvaluatedReads); Assert.Equal("Open", prepared.ReadAnswer(0, query));
        foreach (var expected in new[] { true, false })
        {
            Assert.Equal(0, prepared.Mask(new(query, expected)));
            Assert.Equal(prepared.Universe, prepared.PossibleMask(new(query, expected)));
            Assert.Equal(prepared.Universe, prepared.Possible([new(query, expected)]));
            Assert.Empty(bank.Evaluate(new([new(query, expected)]), context).Admissible);
        }
    }

    [Fact]
    public void An_open_native_query_scope_stays_possible_without_becoming_supported()
    {
        var sample = OrnamentReconstructionCases.Specs[0].Create();
        var input = new R.Input("one panel", sample.Observation, sample.MechanicalGroups);
        Assert.True(R.TryDescribeGroup(input, (Relation)FiniteLaw.Entries(input.MechanicalGroups)[0], out var shape, out _));
        var complete = R.Query.Motif("one contour", shape);
        var tail = (Relation)complete.Surface.Right;
        var query = new R.Query("unfinished query publication", new(complete.Surface.Left, new Relation(StructuralConstraint.Open(), tail.Right)));
        Assert.Empty(FiniteLaw.Entries(query.Blocked)); Assert.NotEmpty(FiniteLaw.Entries(query.Open));
        var bank = new R.Bank([input], [query]); var context = new R.Context([input], [query]);
        var prepared = new OrnamentReconstructionProjection(bank, context, [query]); var claim = new R.Claim(query, true);
        var native = bank.Evaluate(new([claim]), context);
        Assert.Empty(native.Admissible); Assert.Empty(FiniteLaw.Entries(native.Candidates[0].Blocked));
        Assert.Empty(FiniteLaw.Entries(native.Candidates[0].Refuted));
        Assert.Equal(0, prepared.Mask(claim)); Assert.Equal(prepared.Universe, prepared.PossibleMask(claim));
    }
}
