using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeExecution;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;

namespace QualitativeExecution.Tests;

public sealed class OrnamentReconstructionTests
{
    [Fact]
    public void Identical_observed_square_can_retain_one_path_or_four_separate_mechanical_groups()
    {
        var (whole, sides, pattern) = Squares();
        var visible = R.Query.Motif("closed contour", pattern); var mechanical = R.Query.WholePath("one turning contour", pattern);
        var bank = new R.Bank([whole, sides], [visible, mechanical]);
        Assert.Equal("Yes", bank.Read(whole, visible).Answer); Assert.Equal("Yes", bank.Read(sides, visible).Answer);
        Assert.Equal("Yes", bank.Read(whole, mechanical).Answer); Assert.Equal("No", bank.Read(sides, mechanical).Answer);
        Assert.Same(whole.Observation, sides.Observation);
        Assert.DoesNotContain(R.Participants(whole.Surface), participant => participant is Carrier or CarrierLocation);
    }

    [Fact]
    public void Read_focus_preserves_complement_while_changing_the_obligations()
    {
        var (whole, sides, pattern) = Squares();
        var visible = R.Query.Motif("closed contour", pattern); var mechanical = R.Query.WholePath("one turning contour", pattern);
        var bank = new R.Bank([sides, whole], [visible, mechanical]); var context = new R.Context([sides, whole], [visible, mechanical]);
        var message = new R.Message([new(visible, true), new(mechanical, true)]);
        var geometry = new R.Focus([visible, mechanical], [visible]);
        var broad = bank.Evaluate(message, context, geometry); var full = bank.Evaluate(message, context);
        Assert.Equal(2, broad.Admissible.Count); Assert.Same(sides, broad.Selected);
        Assert.Single(full.Admissible); Assert.Same(whole, full.Selected);
        Assert.Same(mechanical.Surface, Assert.Single(FiniteLaw.Entries(geometry.Complement)));
        Assert.Same(message.Surface, broad.Message.Surface);
    }

    [Fact]
    public void Missing_capability_is_open_and_cannot_admit_an_unknown_claim()
    {
        var (whole, _, pattern) = Squares(); var mechanical = R.Query.WholePath("one contour", pattern);
        var bank = new R.Bank([whole], [mechanical]); var message = new R.Message([new(mechanical, false)]);
        var selection = bank.Evaluate(message, new R.Context([whole], []));
        Assert.Empty(selection.Admissible); Assert.NotEmpty(FiniteLaw.Entries(selection.Candidates[0].Open));
        Assert.Empty(FiniteLaw.Entries(selection.Candidates[0].Refuted));
    }

    [Fact]
    public void Crossing_evidence_concerns_the_matched_lines_and_does_not_create_a_join()
    {
        var frame = new QualitativeMotionFrame(); var sites = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray();
        var h = Edge(sites[0], sites[1], frame.Right); var v = Edge(sites[2], sites[3], frame.Up);
        var observed = new QualitativeRecognitionObservation(frame, S(sites), S(h, v), crossings: S(new Relation(h, v)));
        var unrelated = new QualitativeRecognitionObservation(frame, S(sites), S(h, v));
        var a = new R.Input("declared crossing", observed, S()); var b = new R.Input("unrelated", unrelated, S());
        var query = R.Query.Crossing("horizontal over vertical", Single(frame, frame.Right), Single(frame, frame.Up));
        var bank = new R.Bank([a, b], [query]);
        Assert.Equal("Yes", bank.Read(a, query).Answer); Assert.Equal("No", bank.Read(b, query).Answer);
        Assert.Equal(4, FiniteLaw.Entries(a.Observation.Sites).Length);
    }

    [Fact]
    public void Cached_proof_is_reused_only_for_the_actual_input_and_query_and_fresh_read_agrees()
    {
        var (whole, _, pattern) = Squares(); var query = R.Query.Motif("contour", pattern);
        var bank = new R.Bank([whole], [query]); var first = bank.Read(whole, query); var repeated = bank.Read(whole, query);
        Assert.Same(first, repeated); Assert.Equal(1, bank.CachedReads); Assert.Equal(1, bank.EvaluatedReads);
        var fresh = bank.Read(whole, query, useCache: false);
        Assert.Equal(first.Answer, fresh.Answer); Assert.NotSame(first.Surface, fresh.Surface); Assert.Equal(2, bank.Inductions);
        var (other, _, _) = Squares();
        Assert.Equal("Blocked", bank.Read(other, query).Answer);
        Assert.Equal("Blocked", bank.Read(whole, R.Query.Motif("lookalike", pattern)).Answer);
    }

    [Fact]
    public void Native_query_message_and_focus_reconstruction_preserve_actual_aliases()
    {
        var (whole, _, pattern) = Squares(); var query = R.Query.WholePath("contour", pattern);
        var raw = new R.Query("different presentation label", query.Surface); Assert.True(raw.IsValid);
        var message = new R.Message([new(query, true)]); var reconstructed = new R.Message(message.Surface);
        Assert.Same(query.Surface, reconstructed.Claims[0].Query.Surface);
        var bank = new R.Bank([whole], [query]);
        Assert.Single(bank.Evaluate(reconstructed, new R.Context([whole], [query])).Admissible);
        var invalidFocus = new R.Focus(new Relation(S(query.Surface), new Relation(S(query.Surface), S(query.Surface))));
        Assert.False(invalidFocus.IsValid);
    }

    [Fact]
    public void Joint_question_reuses_native_role_constraints_without_sender_witnesses()
    {
        var (whole, _, pattern) = Squares(); var role = new OccurrenceComposition.Role(pattern);
        var query = R.Query.Joint("there is a closed contour", new("contour", [role]));
        var bank = new R.Bank([whole], [query]);
        Assert.Equal("Yes", bank.Read(whole, query).Answer);
        Assert.Empty(R.Participants(query.Surface).Intersect(FiniteLaw.Entries(whole.Observation.Sites), ReferenceEqualityComparer.Instance));
    }

    [Fact]
    public void Published_long_path_can_be_read_without_enumerating_the_visible_walk_grammar()
    {
        var frame = new QualitativeMotionFrame();
        var sites = Enumerable.Range(0, 7).Select(_ => (IElement)new Placeholder()).ToArray();
        var edges = Enumerable.Range(0, 6).Select(index => Edge(sites[index], sites[index + 1],
            index % 2 == 0 ? frame.Right : frame.Up)).ToArray();
        var observation = new QualitativeRecognitionObservation(frame, S(sites), S(edges));
        var group = new Relation(new Placeholder(), new Relation(S(edges), S()));
        var input = new R.Input("long stair", observation, S(group));
        Assert.True(R.TryDescribeGroup(input, group, out var pattern, out var correspondence));
        Assert.Empty(R.Participants(pattern.Surface).Intersect(sites, ReferenceEqualityComparer.Instance));
        Assert.NotEmpty(R.Participants(correspondence).Intersect(sites, ReferenceEqualityComparer.Instance));
        var whole = R.Query.WholePath("one six-edge stair", pattern); var visible = R.Query.Motif("six-edge stair", pattern);
        var bank = new R.Bank([input], [whole, visible], new(ProposalCapacity: 0));
        Assert.Equal("Yes", bank.Read(input, whole).Answer); Assert.Equal(0, bank.Inductions);
        Assert.Equal("Open", bank.Read(input, visible).Answer); Assert.Equal(1, bank.Inductions);
    }

    [Fact]
    public void Supplied_pattern_parsing_accepts_all_three_kinds_without_allocating_discovery_budgets()
    {
        var (whole, _, cycle) = Squares(); Assert.True(cycle.IsValid); Assert.Equal("cycle", cycle.Kind);
        var frame = whole.Observation.Frame; var centre = new Placeholder();
        var leaves = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray();
        var directions = new IElement[] { frame.Right, frame.Up, frame.Left, frame.Down };
        var rows = leaves.Select((leaf, index) => (IElement)new Relation(new Placeholder(),
            new Relation(new Relation(centre, leaf), S(directions[index])))).ToArray();
        var star = new CommunicationPattern(frame, new(new Relation(frame.Surface, new Relation(S([centre, .. leaves]), S(rows))), new Relation(S(centre), S())));
        Assert.True(star.IsValid); Assert.Equal("junction", star.Kind);
        var sites = Enumerable.Range(0, 11).Select(_ => (IElement)new Placeholder()).ToArray();
        var edges = Enumerable.Range(0, 10).Select(index => Edge(sites[index], sites[index + 1], directions[index % 2])).ToArray();
        var input = new R.Input("ten-edge supplied path", new(frame, S(sites), S(edges)),
            S(new Relation(new Placeholder(), new Relation(S(edges), S()))));
        Assert.True(R.TryDescribeGroup(input, (Relation)FiniteLaw.Entries(input.MechanicalGroups)[0], out var walk, out _));
        Assert.Equal("walk", walk.Kind); Assert.True(walk.IsValid);
        var foreignFrame = new CommunicationPattern(new(), walk.Definition);
        Assert.False(foreignFrame.IsValid); Assert.NotEmpty(FiniteLaw.Entries(foreignFrame.Blocked));
    }

    private static (R.Input Whole, R.Input Sides, CommunicationPattern Pattern) Squares()
    {
        var frame = new QualitativeMotionFrame(); var sites = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray();
        var edges = new[] { Edge(sites[0], sites[1], frame.Right), Edge(sites[1], sites[2], frame.Up),
            Edge(sites[2], sites[3], frame.Left), Edge(sites[3], sites[0], frame.Down) };
        var observation = new QualitativeRecognitionObservation(frame, S(sites), S(edges));
        var induction = new QualitativeMotifInduction(observation, 4, 128, 2, 128);
        var family = induction.Families.Single(family => family.Kind == "cycle" && family.SegmentCount == 4);
        var pattern = new CommunicationPattern(frame, family.Definition);
        var whole = new R.Input("one path", observation, S(new Relation(new Placeholder(), new Relation(S(edges), S(new Relation(sites[0], sites[0]))))));
        var sides = new R.Input("four sides", observation, S(edges.Select(edge => (IElement)new Relation(new Placeholder(), new Relation(S(edge), S()))).ToArray()));
        return (whole, sides, pattern);
    }

    private static CommunicationPattern Single(QualitativeMotionFrame frame, IElement direction)
    {
        var a = new Placeholder(); var b = new Placeholder(); var edge = new Placeholder(); var ends = new Relation(a, b);
        var definition = new Relation(new Relation(frame.Surface, new Relation(S(a, b), S(new Relation(edge, new Relation(ends, S(direction)))))),
            new Relation(S(a), S()));
        return new(frame, definition);
    }
    private static Relation Edge(IElement from, IElement to, IElement direction) => new(new Placeholder(), new Relation(new Relation(from, to), S(direction)));
    private static Relation S(params IElement[] elements) => StructuralConstraint.Closed(elements);
}
