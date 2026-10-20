using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;

namespace QualitativeExecution.Tests;

public sealed class OrnamentReconstructionAuditTests
{
    private static readonly Lazy<OrnamentReconstructionCases.Sample> Square = new(() => Sample("SQ-PATH"));
    private static readonly Lazy<OrnamentReconstructionCases.Sample> Sides = new(() => Sample("SQ-SIDES"));
    private static readonly Lazy<OrnamentReconstructionCases.Sample> Cross = new(() => Sample("X-CROSS"));
    private static readonly Lazy<CommunicationPattern> SquarePattern = new(() => Pattern(Square.Value, "cycle", 4));
    private static readonly Lazy<CommunicationPattern> Horizontal = new(() => Single(Cross.Value, horizontal: true));
    private static readonly Lazy<CommunicationPattern> Vertical = new(() => Single(Cross.Value, horizontal: false));

    [Fact]
    public void Answer_packets_and_public_inputs_do_not_reveal_private_programs_observations_or_proof_history()
    {
        var source = Square.Value; var sourceInput = Input(source);
        var visible = R.Query.Motif("visible contour", SquarePattern.Value);
        var mechanical = R.Query.WholePath("one author path", SquarePattern.Value);
        var sender = new R.Bank([sourceInput], [visible, mechanical]);
        var answers = new[] { visible, mechanical }.Select(query => new R.Claim(query, sender.Read(sourceInput, query).Answer)).ToArray();
        var message = new R.Message(answers);
        Assert.True(message.IsValid);
        var packet = R.Participants(message.Surface);
        foreach (var privateRoot in new IElement[] { source.Surface, source.Program.Surface, source.Execution.Surface,
            source.Execution.Current.Surface, source.Publisher.Surface, source.Observation.Surface, source.Observation.Segments,
            source.Bindings, source.MechanicalEvidence, sender.Surface }) Assert.DoesNotContain(privateRoot, packet);
        foreach (var path in E(source.Program.Paths).Cast<Relation>())
        {
            Assert.DoesNotContain(path, packet); Assert.DoesNotContain(path.Left, packet);
            Assert.DoesNotContain(path, R.Participants(sourceInput.Surface));
            Assert.DoesNotContain(path.Left, R.Participants(sourceInput.Surface));
        }
        Assert.DoesNotContain(source.MechanicalEvidence, R.Participants(sourceInput.Surface));
        Assert.DoesNotContain(R.Participants(message.Surface), element => element is Carrier or CarrierLocation);
    }

    [Fact]
    public void Equal_visible_square_incidence_does_not_establish_one_mechanical_path()
    {
        var first = Input(Square.Value); var second = Input(Sides.Value);
        var visible = R.Query.Motif("closed contour", SquarePattern.Value);
        var path = R.Query.WholePath("one closed path", SquarePattern.Value);
        var bank = new R.Bank([first, second], [visible, path]);
        Assert.Equal("Yes", bank.Read(first, visible).Answer); Assert.Equal("Yes", bank.Read(second, visible).Answer);
        Assert.Equal("Yes", bank.Read(first, path).Answer); Assert.Equal("No", bank.Read(second, path).Answer);
        var hiddenMechanics = new R.Input("unpublished grouping", first.Observation, StructuralConstraint.Open());
        var hiddenBank = new R.Bank([hiddenMechanics], [visible, path]);
        Assert.Equal("Yes", hiddenBank.Read(hiddenMechanics, visible).Answer);
        Assert.Equal("Open", hiddenBank.Read(hiddenMechanics, path).Answer);
    }

    [Fact]
    public void Declared_crossing_is_distinct_from_unrelated_lines_and_from_a_joined_cross()
    {
        var crossed = Input(Cross.Value); var unrelated = Input(Sample("X-UNRELATED")); var joined = Input(Sample("X-JOIN"));
        var query = R.Query.Crossing("encounter", Horizontal.Value, Vertical.Value);
        var swap = R.Query.Crossing("same unordered encounter", Vertical.Value, Horizontal.Value);
        var bank = new R.Bank([crossed, unrelated, joined], [query, swap]);
        Assert.Equal("Yes", bank.Read(crossed, query).Answer);
        Assert.Equal("Yes", bank.Read(crossed, swap).Answer);
        Assert.Equal("No", bank.Read(unrelated, query).Answer);
        Assert.Equal("No", bank.Read(joined, query).Answer);
        var original = crossed.Observation; var row = Assert.IsType<Relation>(Assert.Single(E(original.Crossings)));
        var reversedObservation = new QualitativeRecognitionObservation(original.Frame, original.Sites, original.Segments,
            original.Continuations, S(new Relation(row.Right, row.Left)));
        var reversed = new R.Input("opposite publication convention", reversedObservation, crossed.MechanicalGroups);
        var reversedBank = new R.Bank([reversed], [query]);
        Assert.Equal("Yes", reversedBank.Read(reversed, query).Answer);
        Assert.Equal("No", new OrnamentReconstructionGraphRead(original, reversedObservation).Answer);
    }

    [Fact]
    public void Focus_selects_a_question_context_and_carries_the_other_commitments_without_asserting_them()
    {
        var first = Input(Square.Value); var second = Input(Sides.Value);
        var visible = R.Query.Motif("closed contour", SquarePattern.Value); var path = R.Query.WholePath("one path", SquarePattern.Value);
        var queries = new[] { visible, path }; var bank = new R.Bank([first, second], queries);
        var message = new R.Message([new(visible, true), new(path, true)]);
        var context = new R.Context([second, first], queries);
        var visibleFocus = new R.Focus(queries, [visible]);
        var visibleRead = bank.Evaluate(message, context, visibleFocus);
        var bothRead = bank.Evaluate(message, context, new(queries, queries));

        Assert.Equal(2, visibleRead.Admissible.Count); Assert.Same(second, visibleRead.Selected);
        Assert.Same(first, Assert.Single(bothRead.Admissible).Input);
        Assert.Single(E(visibleFocus.Complement)); Assert.Same(path.Surface, E(visibleFocus.Complement)[0]);
        Assert.All(visibleRead.Candidates, candidate => Assert.Single(candidate.Receipts));
        Assert.All(bothRead.Candidates, candidate => Assert.Equal(2, candidate.Receipts.Count));
        Assert.Equal(2, message.Claims.Count);
    }

    [Fact]
    public void Contradictory_and_duplicate_claims_are_explicitly_blocked_before_candidate_selection()
    {
        var input = Input(Square.Value); var query = R.Query.Motif("closure", SquarePattern.Value);
        var bank = new R.Bank([input], [query]); var context = new R.Context([input], [query]);
        foreach (var message in new[] { new R.Message([new(query, true), new(query, false)]),
                     new R.Message([new(query, true), new(query, true)]) })
        {
            Assert.False(message.IsValid); Assert.NotEmpty(E(message.Blocked));
            var selection = bank.Evaluate(message, context);
            Assert.Empty(selection.Admissible); Assert.Null(selection.Selected);
            Assert.NotEmpty(E(Assert.Single(selection.Candidates).Blocked));
        }
    }

    [Fact]
    public void Capability_omission_remains_open_instead_of_becoming_a_negative_fact()
    {
        var input = Input(Square.Value); var query = R.Query.Motif("closure", SquarePattern.Value);
        var bank = new R.Bank([input], [query]); var context = new R.Context([input], []);
        foreach (var expected in new[] { true, false })
        {
            var selection = bank.Evaluate(new([new(query, expected)]), context);
            var candidate = Assert.Single(selection.Candidates);
            Assert.False(candidate.IsAdmissible); Assert.Empty(E(candidate.Refuted)); Assert.NotEmpty(E(candidate.Open));
            Assert.Empty(candidate.Receipts);
        }
        var foreign = R.Query.Motif("unadvertised actual port", SquarePattern.Value);
        Assert.Equal("Blocked", bank.Read(input, foreign).Answer);
    }

    [Fact]
    public void Missing_search_capacity_never_turns_absence_of_a_match_into_a_false_answer()
    {
        var input = Input(Square.Value); var query = R.Query.Motif("closure", SquarePattern.Value);
        var bank = new R.Bank([input], [query], new(ProposalCapacity: 0, RetainedCapacity: 0));
        var receipt = bank.Read(input, query);
        Assert.Equal("Open", receipt.Answer); Assert.Empty(E(receipt.Refuted));
        var selection = bank.Evaluate(new([new(query, false)]), new([input], [query]));
        Assert.Empty(selection.Admissible); Assert.NotEmpty(E(Assert.Single(selection.Candidates).Open));
    }

    [Fact]
    public void Cache_is_scoped_to_actual_input_and_query_and_matches_fresh_reads()
    {
        var first = Input(Square.Value); var second = Input(Sample("SQ-PATH"));
        var visible = R.Query.Motif("contour", SquarePattern.Value); var path = R.Query.WholePath("path", SquarePattern.Value);
        var bank = new R.Bank([first, second], [visible, path]);
        foreach (var input in new[] { first, second }) foreach (var query in new[] { visible, path })
        {
            var cached = bank.Read(input, query); Assert.Same(cached, bank.Read(input, query));
            var fresh = bank.Read(input, query, useCache: false);
            Assert.NotSame(cached, fresh); Assert.Equal(cached.Answer, fresh.Answer);
            Assert.Equal(E(cached.Supported).Length, E(fresh.Supported).Length);
            Assert.Equal(E(cached.Refuted).Length, E(fresh.Refuted).Length);
            Assert.Equal(E(cached.Open).Length, E(fresh.Open).Length);
            Assert.Same(input.Observation, fresh.Input.Observation);
        }
        Assert.Equal(4, bank.CachedReads);
        Assert.NotSame(bank.Read(first, visible), bank.Read(second, visible));
        var unauthorized = new R.Input("new wrapper", first.Observation, first.MechanicalGroups);
        Assert.Equal("Blocked", bank.Read(unauthorized, visible).Answer);
    }

    [Fact]
    public void Foreign_segment_membership_and_false_closure_cannot_certify_a_mechanical_path()
    {
        var source = Square.Value; var query = R.Query.WholePath("closed path", SquarePattern.Value);
        var group = Assert.IsType<Relation>(Assert.Single(E(source.MechanicalGroups)));
        var body = Assert.IsType<Relation>(group.Right); var edges = Assert.IsType<Relation>(body.Left);
        var foreign = Assert.IsType<Relation>(E(Sides.Value.Observation.Segments)[0]);
        var wrongMembership = S(new Relation(new Placeholder(), new Relation(S([foreign, .. E(edges).Skip(1)]), body.Right)));
        var sites = E(source.Observation.Sites);
        var falseClosure = S(new Relation(new Placeholder(), new Relation(edges, S(new Relation(sites[0], sites[1])))));
        foreach (var malformed in new[] { wrongMembership, falseClosure })
        {
            var input = new R.Input("malformed public mechanical claim", source.Observation, malformed);
            var read = new R.Bank([input], [query]).Read(input, query);
            Assert.Equal("Blocked", read.Answer); Assert.Empty(E(read.Supported));
        }
    }

    [Fact]
    public void Invalid_focus_partitions_and_foreign_context_membership_do_not_bypass_admission()
    {
        var input = Input(Square.Value); var query = R.Query.Motif("closure", SquarePattern.Value);
        var bank = new R.Bank([input], [query]); var message = new R.Message([new(query, true)]);
        var context = new R.Context([input], [query]);
        foreach (var focus in new[] { new R.Focus(new(S(query.Surface), new Relation(S(query.Surface), S(query.Surface)))),
                     new R.Focus(new(S(query.Surface), new Relation(S(), S()))) })
        {
            Assert.False(focus.IsValid);
            var selection = bank.Evaluate(message, context, focus);
            Assert.Empty(selection.Admissible); Assert.NotEmpty(E(Assert.Single(selection.Candidates).Blocked));
        }
        var foreign = R.Query.Motif("lookalike undeclared query", SquarePattern.Value);
        var invalid = new R.Context(new(S(input.Surface), S(foreign.Surface)), [input], [query]);
        Assert.False(invalid.IsValid);
        Assert.Empty(bank.Evaluate(message, invalid).Admissible);
    }

    [Fact]
    public void Names_do_not_change_native_query_decoding_or_candidate_agreement()
    {
        var input = Input(Square.Value); var original = R.Query.Motif("closed contour", SquarePattern.Value);
        var renamed = new R.Query("completely misleading unrelated caption", original.Surface);
        var first = new R.Input("square caption", input.Observation, input.MechanicalGroups);
        var second = new R.Input("a different caption", input.Observation, input.MechanicalGroups);
        var bank = new R.Bank([first, second], [original]);
        Assert.True(renamed.IsValid);
        Assert.Equal(bank.Read(first, original, useCache: false).Answer, bank.Read(second, renamed, useCache: false).Answer);
        var read = bank.Evaluate(new([new(renamed, true)]), new([first, second], [original]));
        Assert.Equal(2, read.Admissible.Count);
    }

    [Fact]
    public void Native_message_transport_preserves_focus_aliases_without_requiring_sender_history()
    {
        var visible = R.Query.Motif("visible", SquarePattern.Value); var path = R.Query.WholePath("path", SquarePattern.Value);
        var message = new R.Message([new(visible, true), new(path, false)]);
        var focus = new R.Focus([visible, path], [visible]);
        var root = new Relation(message.Surface, focus.Surface);
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("reconstruction packet", root));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored));
        var native = Assert.IsType<Relation>(restored.Root);
        var imported = new R.Message(Assert.IsType<Relation>(native.Left));
        var importedFocus = new R.Focus(Assert.IsType<Relation>(native.Right));
        Assert.True(imported.IsValid); Assert.True(importedFocus.IsValid);
        Assert.Equal(new[] { "Yes", "No" }, imported.Claims.Select(claim => claim.Answer));
        Assert.Same(imported.Claims[0].Query.Surface, Assert.Single(E(importedFocus.Selected)));
        Assert.Same(imported.Claims[1].Query.Surface, Assert.Single(E(importedFocus.Complement)));
        Assert.DoesNotContain(Square.Value.Observation.Surface, R.Participants(imported.Surface));
    }

    private static OrnamentReconstructionCases.Sample Sample(string family) => OrnamentReconstructionCases.Specs.Single(spec =>
        spec.Family == family && spec.Transform == 0 && spec.ReachRecipe == 0 && spec.Repeats == 1).Create();
    private static R.Input Input(OrnamentReconstructionCases.Sample sample) => new(sample.Spec.Name, sample.Observation, sample.MechanicalGroups);
    private static CommunicationPattern Pattern(OrnamentReconstructionCases.Sample sample, string kind, int edges)
    {
        var read = new QualitativeMotifInduction(sample.Observation, maxCycleEdges: 4, maxWalkEdges: 3,
            proposalCapacity: 4096, retainedCapacity: 1024);
        return new(sample.Observation.Frame, read.Families.First(family => family.Kind == kind && family.SegmentCount == edges).Definition);
    }
    private static CommunicationPattern Single(OrnamentReconstructionCases.Sample sample, bool horizontal)
    {
        var read = new QualitativeMotifInduction(sample.Observation, maxCycleEdges: 0, maxWalkEdges: 0,
            proposalCapacity: 128, retainedCapacity: 128);
        var family = read.Families.First(family => family.Kind == "junction" && family.SegmentCount == 1 && Axis(family.Definition));
        return new(sample.Observation.Frame, family.Definition);
        bool Axis(Relation definition)
        {
            var graph = Assert.IsType<Relation>(Assert.IsType<Relation>(definition.Left).Right);
            var edge = Assert.IsType<Relation>(Assert.Single(E(Assert.IsType<Relation>(graph.Right))));
            var heading = Assert.IsType<Relation>(Assert.IsType<Relation>(edge.Right).Right);
            var direction = Assert.Single(E(heading)); var frame = sample.Observation.Frame;
            return horizontal ? ReferenceEquals(direction, frame.Right) || ReferenceEquals(direction, frame.Left) :
                ReferenceEquals(direction, frame.Up) || ReferenceEquals(direction, frame.Down);
        }
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
