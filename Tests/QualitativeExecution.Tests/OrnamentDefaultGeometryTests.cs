using Gojo.Core;
using Gojo.Patterns;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using G = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultGeometry;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;

namespace QualitativeExecution.Tests;

public sealed class OrnamentDefaultGeometryTests
{
    [Fact]
    public void Explicit_metric_publication_remains_separate_from_history_blind_topology_and_program()
    {
        var sample = Sample("SQ-PATH"); var publication = G.Publication.FromSample(sample);
        Assert.DoesNotContain(publication.Port, R.Participants(sample.Observation.Surface));
        var publicWorld = R.Participants(publication.Port);
        Assert.DoesNotContain(sample.Program.Surface, publicWorld);
        Assert.DoesNotContain(sample.Recipe, publicWorld);
        Assert.DoesNotContain(sample.Execution.Current.Surface, publicWorld);
        Assert.Contains(sample.Observation.Surface, publicWorld);
        Assert.Equal(E(sample.Observation.Sites).Length, E(publication.Bindings).Length);
        Assert.All(E(publication.Bindings).Cast<Relation>(), row => Assert.Contains(row.Left, E(sample.Observation.Sites)));
    }

    [Fact]
    public void Shared_junction_declared_crossing_and_unexplained_crossing_are_different_encounters()
    {
        var standard = new G.Standard();
        Assert.Equal("Yes", Read("X-JOIN", standard.NoUnexpectedEncounters).Answer);
        Assert.Equal("Yes", Read("X-CROSS", standard.NoUnexpectedEncounters).Answer);
        var unexplained = Read("X-UNRELATED", standard.NoUnexpectedEncounters);
        Assert.Equal("No", unexplained.Answer);
        Assert.Single(E(unexplained.Refuted));
        Assert.Empty(E(unexplained.Open));
        // A geometric crossing is not a positive-length collinear overlap.
        Assert.Equal("Yes", Read("X-UNRELATED", standard.NoCollinearOverlap).Answer);
        var sameVisibleSites = Sample("SQ-OPEN");
        Assert.Equal("No", new G.Receipt(standard.NoUnexpectedEncounters, G.Publication.FromSample(sameVisibleSites)).Answer);
    }

    [Fact]
    public void Collinear_retracing_is_visible_even_when_the_endpoints_are_real_joins()
    {
        var original = G.Publication.FromSample(Sample("SQ-PATH"));
        var rows = E(original.Bindings).Cast<Relation>().ToArray();
        var reach = E((Relation)rows.First(row => E((Relation)row.Right).Length > 0).Right).Cast<Relation>().First().Right;
        var author = new LatticeLadderAuthor(original.Frame, reach);
        // All four sites now lie on one line. Distinct native segments occupy
        // positive shared intervals; identity-sharing does not excuse that.
        var words = new[] { author.Word(0, 0), author.Word(4, 0), author.Word(2, 0), author.Word(1, 0) };
        var publication = new G.Publication(original.Frame, original.Observation,
            S(rows.Select((row, index) => (IElement)new Relation(row.Left, words[index])).ToArray()), original.MechanicalGroups);
        var standard = new G.Standard();
        var read = new G.Receipt(standard.NoCollinearOverlap, publication);
        Assert.Equal("No", read.Answer);
        Assert.NotEmpty(E(read.Refuted));
        Assert.Equal("No", new G.Receipt(standard.NoUnexpectedEncounters, publication).Answer);
    }

    [Fact]
    public void Aspect_and_breadth_use_declared_relative_resolution_and_are_uniform_scale_invariant()
    {
        var standard = new G.Standard(aspectLimit: 4, thinLimit: 8);
        var square = G.Publication.FromSample(Sample("SQ-PATH"));
        var large = Scale(square, 10, 10); var narrow = Scale(square, 10, 1);
        foreach (var question in new[] { standard.ModerateAspect, standard.LegibleClosedGroups })
        {
            Assert.Equal("Yes", new G.Receipt(question, square).Answer);
            Assert.Equal("Yes", new G.Receipt(question, large).Answer);
            Assert.Equal("No", new G.Receipt(question, narrow).Answer);
        }
        // More resolving capability changes the optional legibility read,
        // without changing the underlying contour or aspect judgment.
        var finer = new G.Standard(aspectLimit: 4, thinLimit: 64);
        Assert.Equal("Yes", new G.Receipt(finer.LegibleClosedGroups, narrow).Answer);
        Assert.Equal("No", new G.Receipt(finer.ModerateAspect, narrow).Answer);
        Assert.Contains(standard.ResolutionThreshold, R.Participants(new G.Receipt(standard.LegibleClosedGroups, narrow).Surface));
    }

    [Fact]
    public void Ordinary_open_lines_have_no_loop_breadth_obligation()
    {
        var standard = new G.Standard();
        var line = G.Publication.FromSample(Sample("X-CROSS"));
        Assert.Equal("Yes", new G.Receipt(standard.LegibleClosedGroups, line).Answer);
        var missingGroups = new G.Publication(line.Frame, line.Observation, line.Bindings);
        Assert.Equal("Open", new G.Receipt(standard.LegibleClosedGroups, missingGroups).Answer);
    }

    [Fact]
    public void Missing_metric_basis_open_crossing_scope_and_missing_fill_stay_open()
    {
        var standard = new G.Standard(); var source = G.Publication.FromSample(Sample("X-CROSS"));
        var missing = new G.Publication(source.Frame, source.Observation, StructuralConstraint.Open(), source.MechanicalGroups);
        Assert.Equal("Open", new G.Receipt(standard.NoUnexpectedEncounters, missing).Answer);
        Assert.Equal("Open", new G.Receipt(standard.ModerateAspect, missing).Answer);
        var observation = new QualitativeRecognitionObservation(source.Observation.Frame,
            source.Observation.Sites, source.Observation.Segments, source.Observation.Continuations, StructuralConstraint.Open());
        var openCrossings = new G.Publication(source.Frame, observation, source.Bindings, source.MechanicalGroups);
        Assert.Equal("Open", new G.Receipt(standard.NoUnexpectedEncounters, openCrossings).Answer);
        Assert.Equal("Open", new G.Receipt(standard.DisjointFilledInteriors, source).Answer);
        Assert.Equal("Open", new G.Receipt(standard.WithinEnvelope, source).Answer);
    }

    [Fact]
    public void A_boundary_is_only_checked_when_explicitly_selected_and_retains_its_landmarks()
    {
        var publication = G.Publication.FromSample(Sample("SQ-PATH"));
        var exact = new G.Standard(envelopeHalfSpan: 2); var tooSmall = new G.Standard(envelopeHalfSpan: 1);
        Assert.Equal("Yes", new G.Receipt(exact.WithinEnvelope, publication).Answer);
        var failure = new G.Receipt(tooSmall.WithinEnvelope, publication);
        Assert.Equal("No", failure.Answer);
        Assert.NotEmpty(E(failure.Refuted));
        Assert.Contains(tooSmall.Envelope, R.Participants(failure.Surface));
    }

    [Fact]
    public void Native_question_and_publication_transport_needs_no_question_kind_or_case_metadata()
    {
        var standard = new G.Standard(aspectLimit: 5, thinLimit: 12, envelopeHalfSpan: 3);
        var source = G.Publication.FromSample(Sample("SQ-PATH"));
        foreach (var question in standard.Questions)
        {
            var world = Restore(new Relation(question.Surface, source.Port));
            Assert.True(G.Question.TryRead("anonymous question", Assert.IsType<Relation>(world.Left), out var importedQuestion, out var questionWitness));
            Assert.True(G.Publication.TryRead(Assert.IsType<Relation>(world.Right), out var importedSource, out var publicationWitness));
            var before = new G.Receipt(question, source); var after = new G.Receipt(importedQuestion, importedSource);
            Assert.Equal(before.Answer, after.Answer);
            Assert.NotSame(question.Surface, importedQuestion.Surface);
            Assert.Contains(world.Left, R.Participants(questionWitness));
            Assert.Contains(world.Right, R.Participants(publicationWitness));
            Assert.DoesNotContain(source.Port, R.Participants(after.Surface));
        }
        // Matching the record's broad outline is insufficient. The selected
        // ratio must share the actual standard reach, not an unrelated slot.
        var forgedStandard = new Relation(standard.Surface.Left, new Relation(
            S(QualitativeMotionFrame.Move(standard.Frame.Right, new Placeholder())),
            ((Relation)standard.Surface.Right).Right));
        Assert.False(G.Standard.TryRead(forgedStandard, out _, out _));
        Assert.False(G.Question.TryRead("unrecognized obligation", new Relation(standard.Surface, S(new Placeholder())), out _, out _));
    }

    [Fact]
    public void Selected_word_frame_symmetries_are_optional_segment_correspondences_not_topological_truth()
    {
        var standard = new G.Standard(); var square = G.Publication.FromSample(Sample("SQ-PATH"));
        var rectangle = Scale(square, 3, 1);
        Assert.Equal("Yes", new G.Receipt(standard.HorizontalMirror, square).Answer);
        Assert.Equal("Yes", new G.Receipt(standard.QuarterTurn, square).Answer);
        Assert.Equal("Yes", new G.Receipt(standard.HorizontalMirror, rectangle).Answer);
        Assert.Equal("No", new G.Receipt(standard.QuarterTurn, rectangle).Answer);
        Assert.Equal("Yes", new G.Receipt(standard.QuarterTurn, Scale(square, 11, 11)).Answer);
        var open = G.Publication.FromSample(Sample("SQ-OPEN"));
        Assert.Equal("Yes", new G.Receipt(standard.QuarterTurn, open).Answer);
        Assert.Equal("No", new G.Receipt(standard.NoUnexpectedEncounters, open).Answer);
        var read = new G.Receipt(standard.QuarterTurn, square);
        Assert.NotEmpty(E(read.Supported));
        Assert.Empty(E(read.Refuted));
    }

    [Fact]
    public void Unequal_uninterpreted_reaches_cannot_be_silently_counted_as_one_metric()
    {
        var source = G.Publication.FromSample(Sample("SQ-PATH"));
        var rows = E(source.Bindings).Cast<Relation>().ToArray();
        var row = rows.First(item => E((Relation)item.Right).Length > 0);
        var terms = E((Relation)row.Right).ToArray(); var first = (Relation)terms[0];
        terms[0] = new Relation(first.Left, new Placeholder());
        var bindings = S(rows.Select(item => (IElement)(ReferenceEquals(item, row) ? new Relation(item.Left, S(terms)) : item)).ToArray());
        var publication = new G.Publication(source.Frame, source.Observation, bindings, source.MechanicalGroups);
        var standard = new G.Standard();
        Assert.Equal("Open", new G.Receipt(standard.ModerateAspect, publication).Answer);
        Assert.Equal("Open", new G.Receipt(standard.NoUnexpectedEncounters, publication).Answer);
    }

    private static OrnamentReconstructionCases.Sample Sample(string family) => OrnamentReconstructionCases.Specs
        .Single(spec => spec.Family == family && spec.Transform == 0 && spec.ReachRecipe == 0 && spec.Repeats == 1).Create();
    private static G.Receipt Read(string family, G.Question question) => new(question, G.Publication.FromSample(Sample(family)));
    private static G.Publication Scale(G.Publication source, int horizontal, int vertical)
    {
        var rows = E(source.Bindings).Cast<Relation>().ToArray();
        var reach = rows.SelectMany(row => E((Relation)row.Right)).Cast<Relation>().First().Right;
        var author = new LatticeLadderAuthor(source.Frame, reach);
        var scaled = rows.Select(row =>
        {
            var x = 0; var y = 0;
            foreach (var move in E((Relation)row.Right).Cast<Relation>())
            {
                Assert.True(source.Frame.Direction(move, out var axis, out var forward, out _));
                if (ReferenceEquals(axis, source.Frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
            }
            return (IElement)new Relation(row.Left, author.Word(x * horizontal, y * vertical));
        }).ToArray();
        return new(source.Frame, source.Observation, S(scaled), source.MechanicalGroups);
    }
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static Relation Restore(Relation source)
    {
        var text = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("native cue world", source));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(text), out var world));
        return Assert.IsType<Relation>(world.Root);
    }
}
