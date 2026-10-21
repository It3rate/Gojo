using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using P = Gojo.Experiments.QualitativeFrieze.StructuralPotential;
using E = Gojo.Experiments.QualitativeFrieze.StructuralEvidenceRules;

namespace QualitativeExecution.Tests;

public sealed class StructuralPotentialTests
{
    [Fact]
    public void Known_unequal_gaps_refute_this_square_recipe_before_any_span_or_angle_query()
    {
        var frame = new P.Frame();
        var source = frame.Publish((frame.EqualSpacing, "No"));
        var read = E.Read(frame.Laws, source, frame.FiniteSquare);
        Assert.Equal("No", read.Answer);
        Assert.Equal(2, Entries(read.Visited).Length);
        Assert.Contains(Entries(read.Visited), predicate => ReferenceEquals(predicate, frame.EqualSpacing));
        Assert.DoesNotContain(Entries(read.Visited), predicate => ReferenceEquals(predicate, frame.RightAngle));
        Assert.All(frame.SpanCoverage, span => Assert.DoesNotContain(Entries(read.Visited), predicate => ReferenceEquals(predicate, span)));
        Assert.Contains(Entries(read.Unasked), predicate => ReferenceEquals(predicate, frame.FiniteRectangle));
        // The rejected candidate says nothing about differently selected supports.
        Assert.Equal("Open", E.Read(frame.Laws, source, frame.FiniteBoundary).Answer);
    }

    [Fact]
    public void An_empty_fact_inventory_keeps_construction_obligations_open_instead_of_becoming_a_closed_false_world()
    {
        var frame = new P.Frame(); var source = frame.Publish();
        foreach (var goal in frame.Goals)
        {
            var read = E.Read(frame.Laws, source, goal.Predicate);
            Assert.Equal("Open", read.Answer);
            Assert.NotEmpty(Entries(read.Missing));
            Assert.Empty(Entries(read.Refuted));
        }
        AssertOnlyNativeStructure(frame.Source);
    }

    [Fact]
    public void Distinct_material_identities_and_internal_repetition_do_not_supply_missing_straightness_or_transverse_separation()
    {
        var frame = new P.Frame();
        Assert.Equal(8, Entries(frame.SourceObservation.Sites).Length);
        Assert.Equal(4, Entries(frame.SourceObservation.Segments).Length);
        var notStraight = frame.Publish((frame.SeparateA, "Yes"), (frame.SeparateB, "Yes"), (frame.Independent, "Yes"),
            (frame.UniformA, "Yes"), (frame.UniformB, "Yes"));
        var read = E.Read(frame.Laws, notStraight, frame.PotentialBoundary);
        Assert.Equal("Open", read.Answer); Assert.True(Visited(read, frame.Straight));
        var notSeparated = frame.Publish((frame.Straight, "Yes"), (frame.Independent, "Yes"),
            (frame.UniformA, "Yes"), (frame.UniformB, "Yes"));
        var other = E.Read(frame.Laws, notSeparated, frame.PotentialBoundary);
        Assert.Equal("Open", other.Answer);
        Assert.True(Visited(other, frame.SeparateA)); Assert.True(Visited(other, frame.SeparateB));
    }

    [Fact]
    public void Independent_line_families_do_not_manufacture_a_right_angle_or_cross_family_spacing_equality()
    {
        var frame = new P.Frame(); var source = frame.Publish(Base(frame));
        Assert.Equal("Yes", E.Read(frame.Laws, source, frame.PotentialBoundary).Answer);
        var rectangle = E.Read(frame.Laws, source, frame.PotentialRectangle);
        Assert.Equal("Open", rectangle.Answer); Assert.True(Visited(rectangle, frame.RightAngle));
        var square = E.Read(frame.Laws, source, frame.PotentialSquare);
        Assert.Equal("Open", square.Answer);
        Assert.Empty(Entries(square.Refuted));
    }

    [Fact]
    public void Uniform_spacing_in_each_family_still_leaves_the_two_family_spacings_unrelated()
    {
        var frame = new P.Frame();
        var facts = Base(frame).Concat([(frame.RightAngle, "Yes"), (frame.UniformA, "Yes"), (frame.UniformB, "Yes")]).ToArray();
        var source = frame.Publish(facts);
        Assert.Equal("Yes", E.Read(frame.Laws, source, frame.PotentialRectangle).Answer);
        var openSquare = E.Read(frame.Laws, source, frame.PotentialSquare);
        Assert.Equal("Open", openSquare.Answer); Assert.True(Visited(openSquare, frame.EqualSpacing));
        var unequal = frame.Publish(facts.Append((frame.EqualSpacing, "No")).ToArray());
        Assert.Equal("No", E.Read(frame.Laws, unequal, frame.PotentialSquare).Answer);
        Assert.Equal("Yes", E.Read(frame.Laws, unequal, frame.PotentialRectangle).Answer);
    }

    [Fact]
    public void Potential_intersections_do_not_supply_unoffered_finite_span_coverage()
    {
        var frame = new P.Frame(); var source = frame.Publish(Base(frame));
        Assert.Equal("Yes", E.Read(frame.Laws, source, frame.PotentialBoundary).Answer);
        Assert.Equal("Open", E.Read(frame.Laws, source, frame.FiniteBoundary).Answer);
        var incomplete = frame.Publish(Base(frame).Concat(frame.SpanCoverage.Take(3).Select(predicate => (predicate, "Yes"))).ToArray());
        Assert.Equal("Open", E.Read(frame.Laws, incomplete, frame.FiniteBoundary).Answer);
        var covered = frame.Publish(Base(frame).Concat(frame.SpanCoverage.Select(predicate => (predicate, "Yes"))).ToArray());
        Assert.Equal("Yes", E.Read(frame.Laws, covered, frame.FiniteBoundary).Answer);
    }

    [Fact]
    public void A_rectangle_question_does_not_query_or_require_square_spacing_or_internal_uniformity()
    {
        var frame = new P.Frame(); var source = frame.Publish(FiniteRectangle(frame));
        var read = E.Read(frame.Laws, source, frame.FiniteRectangle);
        Assert.Equal("Yes", read.Answer);
        Assert.False(Visited(read, frame.EqualSpacing)); Assert.False(Visited(read, frame.UniformA)); Assert.False(Visited(read, frame.UniformB));
        var contradictoryIrrelevance = frame.Publish(FiniteRectangle(frame).Concat([
            (frame.EqualSpacing, "No"), (frame.UniformA, "No"), (frame.UniformB, "No")]).ToArray());
        Assert.Equal("Yes", E.Read(frame.Laws, contradictoryIrrelevance, frame.FiniteRectangle).Answer);
    }

    [Fact]
    public void A_definite_finite_span_failure_refutes_this_completion_without_erasing_the_more_general_potential()
    {
        var frame = new P.Frame();
        var source = frame.Publish(Base(frame).Concat([(frame.RightAngle, "Yes"), (frame.EqualSpacing, "Yes")])
            .Concat(frame.SpanCoverage.Select((predicate, i) => (predicate, i == 2 ? "No" : "Yes"))).ToArray());
        Assert.Equal("Yes", E.Read(frame.Laws, source, frame.PotentialSquare).Answer);
        Assert.Equal("No", E.Read(frame.Laws, source, frame.FiniteSquare).Answer);
        var completion = frame.Complete(source, frame.FiniteSquare, All(frame));
        Assert.Equal("No", completion.Answer); Assert.Null(completion.Sample);
        Assert.DoesNotContain(Entries(completion.AssumedPredicates), predicate => ReferenceEquals(predicate, frame.SpanCoverage[2]));
    }

    [Fact]
    public void Query_scoped_rectangle_completion_records_only_demanded_assumptions_and_does_not_promote_them_to_source_facts()
    {
        var frame = new P.Frame(); var source = frame.Publish(Base(frame)); var before = source.Surface;
        var completion = frame.Complete(source, frame.FiniteRectangle, All(frame));
        Assert.Equal("Open", completion.SourceRead.Answer); Assert.Equal("Yes", completion.ConditionalRead.Answer);
        Assert.Equal("Yes", completion.Answer); Assert.NotNull(completion.Sample);
        Assert.Contains(Entries(completion.AssumedPredicates), predicate => ReferenceEquals(predicate, frame.RightAngle));
        Assert.DoesNotContain(Entries(completion.AssumedPredicates), predicate => ReferenceEquals(predicate, frame.EqualSpacing));
        Assert.DoesNotContain(Entries(completion.AssumedPredicates), predicate => ReferenceEquals(predicate, frame.UniformA) || ReferenceEquals(predicate, frame.UniformB));
        Assert.NotEmpty(Entries(completion.Assumptions));
        Assert.Same(before, source.Surface);
        Assert.Equal("Open", E.Read(frame.Laws, source, frame.FiniteRectangle).Answer);
        Assert.Equal("Open", E.Read(frame.Laws, source, frame.PotentialSquare).Answer);
        AssertOnlyNativeStructure(completion.Surface);
    }

    [Fact]
    public void Square_completion_can_add_the_one_missing_cross_family_equality_without_claiming_it_was_observed()
    {
        var frame = new P.Frame(); var source = frame.Publish(FiniteRectangle(frame));
        var square = frame.Complete(source, frame.FiniteSquare, All(frame));
        Assert.Equal("Open", square.SourceRead.Answer); Assert.Equal("Yes", square.ConditionalRead.Answer);
        Assert.Equal("Yes", square.Answer); Assert.NotNull(square.Sample);
        Assert.Same(frame.EqualSpacing, Assert.Single(Entries(square.AssumedPredicates)));
        Assert.Equal("Open", E.Read(frame.Laws, source, frame.FiniteSquare).Answer);
        var rectangle = frame.Complete(source, frame.FiniteRectangle, All(frame));
        Assert.Equal("Yes", rectangle.SourceRead.Answer); Assert.Empty(Entries(rectangle.Assumptions));
        Assert.Empty(Entries(rectangle.AssumedPredicates));
    }

    [Fact]
    public void Completion_permissions_do_not_turn_unavailable_or_contradictory_source_claims_into_facts()
    {
        var frame = new P.Frame();
        var missing = frame.Complete(frame.Publish(Base(frame)), frame.FiniteRectangle, Scope());
        Assert.Equal("Open", missing.Answer); Assert.Null(missing.Sample); Assert.Empty(Entries(missing.Assumptions));
        var no = frame.Publish(Base(frame).Where(fact => !ReferenceEquals(fact.Predicate, frame.Straight)).Append((frame.Straight, "No")).ToArray());
        var refuted = frame.Complete(no, frame.FiniteRectangle, All(frame));
        Assert.Equal("No", refuted.Answer); Assert.Null(refuted.Sample);
        Assert.Empty(Entries(refuted.Assumptions));
        var conflict = frame.Publish(FiniteRectangle(frame).Append((frame.RightAngle, "No")).ToArray());
        var inconsistent = frame.Complete(conflict, frame.FiniteRectangle, All(frame));
        Assert.Equal("Conflict", inconsistent.Answer); Assert.Null(inconsistent.Sample);
        Assert.Empty(Entries(inconsistent.Assumptions));
    }

    [Fact]
    public void Computational_capacity_cuts_cannot_be_filled_by_geometric_assumption_permission()
    {
        var frame = new P.Frame(); var source = frame.Publish(Base(frame));
        var completion = frame.Complete(source, frame.FiniteSquare, All(frame), queryCapacity: 0);
        Assert.Equal("Open", completion.Answer); Assert.Null(completion.Sample);
        Assert.Empty(Entries(completion.Assumptions)); Assert.Empty(Entries(completion.AssumedPredicates));
        Assert.Empty(Entries(completion.SourceRead.Visited));
        Assert.NotEmpty(Entries(completion.SourceRead.Unasked));
    }

    [Fact]
    public void Later_source_evidence_can_reject_a_new_completion_without_overwriting_the_earlier_imagination()
    {
        var frame = new P.Frame();
        var original = frame.Publish(Base(frame).Append((frame.RightAngle, "Yes")).ToArray());
        var originalSurface = original.Surface;
        var imagined = frame.Complete(original, frame.FiniteSquare, All(frame));
        Assert.Equal("Open", imagined.SourceRead.Answer); Assert.Equal("Yes", imagined.Answer);
        Assert.NotNull(imagined.Sample);
        var retained = imagined.Sample; var retainedSurface = retained.Surface;
        var retainedObservation = retained.Observation;
        Assert.Contains(Entries(imagined.AssumedPredicates), predicate => ReferenceEquals(predicate, frame.EqualSpacing));
        Assert.All(frame.SpanCoverage, coverage => Assert.Contains(Entries(imagined.AssumedPredicates),
            predicate => ReferenceEquals(predicate, coverage)));

        // The new supplier fact extends the actual publication, never its imagined completion.
        var contrary = OrnamentDefaultsPolicy.Evidence.FromAnswer(frame.EqualSpacing, "No", new Relation(frame.Source, frame.EqualSpacing));
        var revised = new E.Publication(original.Source, original.Evidence.Append(contrary).ToArray());
        var rejected = frame.Complete(revised, frame.FiniteSquare, All(frame));
        Assert.Equal("No", rejected.SourceRead.Answer); Assert.Equal("No", rejected.Answer);
        Assert.Null(rejected.Sample); Assert.Empty(Entries(rejected.Assumptions));
        Assert.Empty(Entries(rejected.AssumedPredicates));
        Assert.Same(retained, imagined.Sample); Assert.Same(retainedSurface, imagined.Sample.Surface);
        Assert.Same(retainedObservation, imagined.Sample.Observation);
        Assert.Same(originalSurface, original.Surface);
        Assert.Equal("Open", E.Read(frame.Laws, original, frame.FiniteSquare).Answer);
        Assert.Equal("Yes", E.Read(frame.Laws, revised, frame.PotentialRectangle).Answer);
    }

    [Fact]
    public void A_foreign_source_cannot_borrow_this_frames_predicates_to_obtain_a_completion()
    {
        var frame = new P.Frame(); var genuine = frame.Publish(FiniteRectangle(frame));
        var foreign = new E.Publication(new Relation(new Placeholder(), new Placeholder()), genuine.Evidence);
        var completion = frame.Complete(foreign, frame.FiniteRectangle, All(frame));
        Assert.Equal("Blocked", completion.Answer); Assert.Null(completion.Sample);
        Assert.NotEmpty(Entries(completion.Blocked));
        Assert.Empty(Entries(completion.Assumptions)); Assert.Empty(Entries(completion.AssumedPredicates));
        Assert.Equal("Yes", frame.Complete(genuine, frame.FiniteRectangle, All(frame)).Answer);
    }

    [Fact]
    public void A_completed_read_cycle_preserves_all_original_disconnected_mechanical_supports()
    {
        var frame = new P.Frame(); var original = frame.SourceObservation;
        var sites = Entries(original.Sites); var segments = Entries(original.Segments);
        Assert.Equal(8, sites.Length); Assert.Equal(4, segments.Length);
        Assert.All(sites, site => Assert.Equal(1, Degree(original, site)));
        var completion = frame.Complete(frame.Publish(FiniteRectangle(frame)), frame.FiniteRectangle, Scope());
        Assert.Equal("Yes", completion.Answer); Assert.NotNull(completion.Sample);
        var sample = completion.Sample;
        Assert.Equal(4, Entries(sample.Observation.Sites).Length); Assert.Equal(4, Entries(sample.Observation.Segments).Length);
        Assert.All(Entries(sample.Observation.Sites), site => Assert.Equal(2, Degree(sample.Observation, site)));
        Assert.Same(original, frame.SourceObservation);
        Assert.All(sites, site => Assert.Contains(Entries(original.Sites), actual => ReferenceEquals(actual, site)));
        Assert.All(segments, segment => Assert.Contains(Entries(original.Segments), actual => ReferenceEquals(actual, segment)));
        Assert.All(sites, site => Assert.Equal(1, Degree(original, site)));
        var correspondence = OrnamentReconstruction.Participants(sample.Correspondence);
        Assert.All(segments, segment => Assert.True(correspondence.Contains(segment), "A projected subspan must retain the actual supplying support."));
        Assert.True(OrnamentReconstruction.Participants(sample.Incidences).Count > 4);
        Assert.True(OrnamentReconstruction.Participants(sample.CycleWitness).Count > 4);
        var induction = new QualitativeMotifInduction(sample.Observation, maxCycleEdges: 4, proposalCapacity: 4096, maxWalkEdges: 3, retainedCapacity: 512);
        Assert.Contains(induction.Families, family => new CommunicationPattern(sample.Observation.Frame, family.Definition).Kind == "cycle");
        AssertOnlyNativeStructure(sample.Surface);
    }

    private static (Relation Predicate, string Answer)[] Base(P.Frame frame) => [
        (frame.Straight, "Yes"), (frame.SeparateA, "Yes"), (frame.SeparateB, "Yes"), (frame.Independent, "Yes")];
    private static (Relation Predicate, string Answer)[] FiniteRectangle(P.Frame frame) => Base(frame)
        .Append((frame.RightAngle, "Yes")).Concat(frame.SpanCoverage.Select(predicate => (predicate, "Yes"))).ToArray();
    private static Relation All(P.Frame frame) => Scope(frame.Leaves.Select(item => (IElement)item.Predicate).ToArray());
    private static bool Visited(E.Receipt read, Relation predicate) => Entries(read.Visited).Any(actual => ReferenceEquals(actual, predicate));
    private static int Degree(QualitativeRecognitionObservation observation, IElement site) => Entries(observation.Segments).Cast<Relation>().Count(edge =>
        observation.TryReadSegment(edge, out _, out var from, out var to, out _) && (ReferenceEquals(from, site) || ReferenceEquals(to, site)));
    private static void AssertOnlyNativeStructure(IElement surface) => Assert.All(OrnamentReconstruction.Participants(surface),
        element => Assert.True(element is Relation or Placeholder, "The pre-numeric potential must not introduce a measured carrier or location."));
    private static Relation Scope(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] Entries(Relation scope) => FiniteLaw.Entries(scope);
}
