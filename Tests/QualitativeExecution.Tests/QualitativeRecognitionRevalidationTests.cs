using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;

namespace QualitativeExecution.Tests;

public sealed class QualitativeRecognitionRevalidationTests
{
    [Fact]
    public void Unchanged_observation_revalidates_every_retained_binding_without_new_induction()
    {
        var fixture = new Fixture(); fixture.Square(); fixture.Square(); var prior = fixture.Induce();
        var read = new QualitativeRecognitionRevalidation(prior, prior.Observation);
        Assert.True(read.IsComplete); Assert.Empty(E(read.Refuted)); Assert.Empty(E(read.Unresolved));
        Assert.Equal(prior.Families.Sum(family => family.Occurrences.Count), E(read.Supported).Length);
        Assert.Same(prior.Surface, ((Relation)read.Definition.Left).Left);
        Assert.All(read.Checks, check => Assert.True(read.TryReadCheck(check.Read, out _)));
    }

    [Fact]
    public void An_explicit_heading_replacement_refutes_only_dependent_occurrences_and_retains_original()
    {
        var fixture = new Fixture(); var edited = fixture.Square()[0]; fixture.Square(); var prior = fixture.Induce();
        var replacement = fixture.Replacement(edited, heading: S(fixture.Frame.Up));
        var current = fixture.Current(edited, replacement);
        var map = new Relation(edited, replacement); var read = new QualitativeRecognitionRevalidation(prior, current, S(map));
        Assert.True(read.IsComplete); Assert.NotEmpty(E(read.Refuted)); Assert.NotEmpty(E(read.Supported));
        Assert.Empty(E(read.Unresolved));
        foreach (var check in read.Checks)
        {
            var dependent = E(check.PriorOccurrence.Segments).Any(row => ReferenceEquals(row, edited));
            Assert.Equal(dependent ? "refuted" : "supported", check.Status);
            Assert.Contains(E(check.Dependencies), item => ReferenceEquals(item, check.PriorOccurrence.Read));
            if (dependent) Assert.Contains(E(check.Dependencies), item => ReferenceEquals(item, map));
        }
        Assert.Same(edited, E(prior.Observation.Segments)[0]);
    }

    [Fact]
    public void Same_identity_foreign_copy_is_not_an_authorized_replacement_correspondence()
    {
        var fixture = new Fixture(); var edited = fixture.Square()[0]; var prior = fixture.Induce();
        var replacement = fixture.Replacement(edited); var current = fixture.Current(edited, replacement);
        var read = new QualitativeRecognitionRevalidation(prior, current);
        Assert.False(read.IsComplete); Assert.NotEmpty(E(read.Unresolved)); Assert.Empty(E(read.Refuted));
        Assert.All(read.Checks.Where(check => E(check.PriorOccurrence.Segments).Any(row => ReferenceEquals(row, edited))),
            check => Assert.Equal("unresolved", check.Status));
    }

    [Fact]
    public void A_correspondence_to_a_row_not_actually_published_stays_unresolved()
    {
        var fixture = new Fixture(); var edited = fixture.Square()[0]; var prior = fixture.Induce();
        var actual = fixture.Replacement(edited); var foreign = new Relation(actual.Left, actual.Right);
        var read = new QualitativeRecognitionRevalidation(prior, fixture.Current(edited, actual), S(new Relation(edited, foreign)));
        Assert.False(read.IsComplete); Assert.NotEmpty(E(read.InputUnresolved)); Assert.NotEmpty(E(read.Unresolved));
        Assert.Empty(E(read.Refuted));
    }

    [Fact]
    public void Explicit_reversed_row_and_opposed_heading_preserve_the_selected_walk_read()
    {
        var fixture = new Fixture(); var edited = fixture.Square()[0]; var prior = fixture.Induce();
        var ends = (Relation)((Relation)edited.Right).Left;
        var replacement = new Relation(edited.Left, new Relation(new Relation(ends.Right, ends.Left), S(fixture.Frame.Left)));
        var mapping = new Relation(edited, replacement);
        var read = new QualitativeRecognitionRevalidation(prior, fixture.Current(edited, replacement), S(mapping));
        Assert.True(read.IsComplete); Assert.Empty(E(read.Refuted)); Assert.Empty(E(read.Unresolved));
        Assert.Equal(read.Checks.Count, E(read.Supported).Length);
        Assert.Contains(read.Checks.SelectMany(check => E(check.Dependencies)), item => ReferenceEquals(item, mapping));
    }

    [Fact]
    public void Closed_complete_inventory_can_refute_a_withdrawn_edge_at_its_retained_binding()
    {
        var fixture = new Fixture(); var withdrawn = fixture.Square()[0]; fixture.Square(); var prior = fixture.Induce();
        var read = new QualitativeRecognitionRevalidation(prior, fixture.Current(withdrawn, null));
        Assert.True(read.IsComplete); Assert.NotEmpty(E(read.Refuted)); Assert.Empty(E(read.Unresolved));
        Assert.All(read.Checks.Where(check => E(check.PriorOccurrence.Segments).Any(row => ReferenceEquals(row, withdrawn))),
            check => Assert.Equal("refuted", check.Status));
        Assert.NotEmpty(E(read.Supported));
    }

    [Fact]
    public void Open_inventory_cannot_turn_an_unobserved_edge_into_a_definite_absence()
    {
        var fixture = new Fixture(); var withdrawn = fixture.Square()[0]; fixture.Square(); var prior = fixture.Induce();
        var read = new QualitativeRecognitionRevalidation(prior, fixture.Current(withdrawn, null, open: true));
        Assert.False(read.IsComplete); Assert.Empty(E(read.Refuted)); Assert.NotEmpty(E(read.Unresolved));
        Assert.Contains(read.Checks, check => check.PriorFamily.Kind == "cycle" && check.IsSupported);
        Assert.All(read.Checks.Where(check => E(check.PriorOccurrence.Segments).Any(row => ReferenceEquals(row, withdrawn))),
            check => Assert.Equal("unresolved", check.Status));
    }

    [Fact]
    public void A_detached_endpoint_is_a_topological_mismatch_even_with_the_same_heading()
    {
        var fixture = new Fixture(); var edited = fixture.Square()[0]; fixture.Square(); var prior = fixture.Induce();
        var fresh = new Placeholder(); fixture.Sites.Add(fresh);
        var replacement = fixture.Replacement(edited, from: fresh);
        var read = new QualitativeRecognitionRevalidation(prior, fixture.Current(edited, replacement), S(new Relation(edited, replacement)));
        Assert.NotEmpty(E(read.Refuted)); Assert.NotEmpty(E(read.Supported));
        Assert.All(read.Checks.Where(check => E(check.PriorOccurrence.Segments).Any(row => ReferenceEquals(row, edited))),
            check => Assert.Equal("refuted", check.Status));
    }

    [Fact]
    public void An_open_heading_changes_exact_matches_to_unresolved_without_erasing_remote_support()
    {
        var fixture = new Fixture(); var edited = fixture.Square()[0]; fixture.Square(); var prior = fixture.Induce();
        var replacement = fixture.Replacement(edited, heading: StructuralConstraint.Open(fixture.Frame.Right));
        var read = new QualitativeRecognitionRevalidation(prior, fixture.Current(edited, replacement), S(new Relation(edited, replacement)));
        Assert.False(read.IsComplete); Assert.Empty(E(read.Refuted)); Assert.NotEmpty(E(read.Unresolved)); Assert.NotEmpty(E(read.Supported));
        Assert.All(read.Checks.Where(check => E(check.PriorOccurrence.Segments).Any(row => ReferenceEquals(row, edited))),
            check => Assert.Equal("unresolved", check.Status));
    }

    [Fact]
    public void A_known_incompatible_direction_already_refutes_an_open_heading()
    {
        var fixture = new Fixture(); var edited = fixture.Square()[0]; fixture.Square(); var prior = fixture.Induce();
        var replacement = fixture.Replacement(edited, heading: StructuralConstraint.Open(fixture.Frame.Up));
        var read = new QualitativeRecognitionRevalidation(prior, fixture.Current(edited, replacement), S(new Relation(edited, replacement)));
        Assert.NotEmpty(E(read.InputUnresolved));
        Assert.All(read.Checks.Where(check => E(check.PriorOccurrence.Segments).Any(row => ReferenceEquals(row, edited))),
            check => Assert.NotEmpty(E(check.RefutedObligations)));
        Assert.Contains(read.Checks, check => check.IsSupported);
    }

    [Fact]
    public void A_known_subset_of_a_diagonal_heading_stays_unresolved()
    {
        var fixture = new Fixture(); var from = new Placeholder(); var to = new Placeholder(); fixture.Sites.Add(from); fixture.Sites.Add(to);
        var edited = new Relation(new Placeholder(), new Relation(new Relation(from, to), S(fixture.Frame.Right, fixture.Frame.Up))); fixture.Edges.Add(edited);
        var prior = fixture.Induce(); var replacement = fixture.Replacement(edited, heading: StructuralConstraint.Open(fixture.Frame.Right));
        var read = new QualitativeRecognitionRevalidation(prior, fixture.Current(edited, replacement), S(new Relation(edited, replacement)));
        Assert.Empty(E(read.Refuted)); Assert.Empty(E(read.Supported)); Assert.NotEmpty(E(read.Unresolved));
    }

    [Fact]
    public void Reversing_an_open_heading_maps_its_known_prefix_and_preserves_its_missing_tail()
    {
        var fixture = new Fixture(); var edited = fixture.Square()[0]; var prior = fixture.Induce();
        var ends = (Relation)((Relation)edited.Right).Left;
        var compatible = new Relation(edited.Left, new Relation(new Relation(ends.Right, ends.Left), StructuralConstraint.Open(fixture.Frame.Left)));
        var compatibleRead = new QualitativeRecognitionRevalidation(prior, fixture.Current(edited, compatible), S(new Relation(edited, compatible)));
        Assert.Empty(E(compatibleRead.Refuted));
        Assert.All(compatibleRead.Checks.Where(check => E(check.PriorOccurrence.Segments).Any(row => ReferenceEquals(row, edited))),
            check => Assert.NotEmpty(E(check.UnresolvedObligations)));
        var incompatible = new Relation(edited.Left, new Relation(new Relation(ends.Right, ends.Left), StructuralConstraint.Open(fixture.Frame.Down)));
        var incompatibleRead = new QualitativeRecognitionRevalidation(prior, fixture.Current(edited, incompatible), S(new Relation(edited, incompatible)));
        Assert.All(incompatibleRead.Checks.Where(check => E(check.PriorOccurrence.Segments).Any(row => ReferenceEquals(row, edited))),
            check => Assert.NotEmpty(E(check.RefutedObligations)));
    }

    [Fact]
    public void An_extra_incident_edge_refutes_exhaustive_stars_but_not_existing_cycles()
    {
        var fixture = new Fixture(); var square = fixture.Square(); var prior = fixture.Induce();
        var centre = ((Relation)((Relation)square[0].Right).Left).Left; var fresh = new Placeholder(); fixture.Sites.Add(fresh);
        fixture.Edge(centre, fresh, fixture.Frame.Up);
        var read = new QualitativeRecognitionRevalidation(prior, fixture.Observation());
        Assert.Contains(read.Checks, check => check.PriorFamily.Kind == "junction" && check.Status == "refuted");
        Assert.All(read.Checks.Where(check => check.PriorFamily.Kind == "cycle"), check => Assert.True(check.IsSupported));
    }

    [Fact]
    public void An_open_correspondence_cannot_prove_absence_while_a_replacement_binding_is_unavailable()
    {
        var fixture = new Fixture(); var withdrawn = fixture.Square()[0]; var prior = fixture.Induce();
        var read = new QualitativeRecognitionRevalidation(prior, fixture.Current(withdrawn, null), StructuralConstraint.Open());
        Assert.Empty(E(read.Refuted)); Assert.NotEmpty(E(read.Unresolved)); Assert.False(read.IsComplete);
    }

    [Fact]
    public void Ambiguous_duplicate_extra_rows_do_not_supply_definite_new_branch_evidence()
    {
        var fixture = new Fixture(); var square = fixture.Square(); var prior = fixture.Induce();
        var centre = ((Relation)((Relation)square[0].Right).Left).Left; var first = new Placeholder(); var second = new Placeholder();
        fixture.Sites.Add(first); fixture.Sites.Add(second);
        var extra = fixture.Edge(centre, first, fixture.Frame.Up);
        fixture.Edges.Add(new(extra.Left, new Relation(new Relation(centre, second), S(fixture.Frame.Right))));
        var read = new QualitativeRecognitionRevalidation(prior, fixture.Observation());
        Assert.Empty(E(read.Refuted)); Assert.NotEmpty(E(read.Unresolved));
        Assert.All(read.Checks.Where(check => check.PriorFamily.Kind == "cycle"), check => Assert.True(check.IsSupported));
    }

    [Fact]
    public void An_unknown_frame_correspondence_never_reuses_direction_names_as_equality()
    {
        var fixture = new Fixture(); fixture.Square(); var prior = fixture.Induce();
        var current = new QualitativeRecognitionObservation(new QualitativeMotionFrame(), S(fixture.Sites.ToArray()), S(fixture.Edges.ToArray()));
        var read = new QualitativeRecognitionRevalidation(prior, current);
        Assert.False(read.IsComplete); Assert.Empty(E(read.Supported)); Assert.NotEmpty(E(read.Unresolved));
    }

    [Fact]
    public void Revalidation_does_not_create_occurrences_for_new_material()
    {
        var fixture = new Fixture(); fixture.Square(); var prior = fixture.Induce(); var before = prior.Families.Sum(family => family.Occurrences.Count);
        fixture.Square(); var read = new QualitativeRecognitionRevalidation(prior, fixture.Observation());
        Assert.Equal(before, read.Checks.Count); Assert.Equal(before, E(read.Supported).Length);
    }

    [Fact]
    public void Native_check_reader_requires_the_retained_actual_check()
    {
        var fixture = new Fixture(); fixture.Square(); var prior = fixture.Induce();
        var read = new QualitativeRecognitionRevalidation(prior, prior.Observation); var actual = read.Checks[0];
        Assert.True(read.TryReadCheck(actual.Read, out var check)); Assert.Same(actual, check);
        Assert.False(read.TryReadCheck(new Relation(actual.Read.Left, actual.Read.Right), out _));
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var rows, out _, out _) ? rows : [];
    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        public List<IElement> Sites { get; } = [];
        public List<Relation> Edges { get; } = [];
        public Relation Edge(IElement from, IElement to, IElement heading)
        { var row = new Relation(new Placeholder(), new Relation(new Relation(from, to), S(heading))); Edges.Add(row); return row; }
        public Relation[] Square()
        {
            var sites = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray(); Sites.AddRange(sites);
            return [Edge(sites[0], sites[1], Frame.Right), Edge(sites[1], sites[2], Frame.Up),
                Edge(sites[2], sites[3], Frame.Left), Edge(sites[3], sites[0], Frame.Down)];
        }
        public QualitativeRecognitionObservation Observation() => new(Frame, S(Sites.ToArray()), S(Edges.ToArray()));
        public QualitativeMotifInduction Induce() => new(Observation());
        public Relation Replacement(Relation original, IElement? from = null, Relation? heading = null)
        {
            var data = (Relation)original.Right; var ends = (Relation)data.Left;
            return new(original.Left, new Relation(new Relation(from ?? ends.Left, ends.Right), heading ?? data.Right));
        }
        public QualitativeRecognitionObservation Current(Relation removed, Relation? replacement, bool open = false)
        {
            var current = Edges.Where(row => !ReferenceEquals(row, removed)).Cast<IElement>().ToList();
            if (replacement is not null) current.Add(replacement);
            return new(Frame, S(Sites.ToArray()), open ? StructuralConstraint.Open(current.ToArray()) : S(current.ToArray()));
        }
    }
}
