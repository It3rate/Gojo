using Gojo.Core;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;

namespace QualitativeExecution.Tests;

public sealed class QualitativeRecognitionFirmnessStudyTests : IClassFixture<QualitativeRecognitionFirmnessStudyTests.StudyFixture>
{
    private readonly QualitativeRecognitionFirmnessStudy study;
    public QualitativeRecognitionFirmnessStudyTests(StudyFixture fixture) => study = fixture.Value;
    public sealed class StudyFixture { public QualitativeRecognitionFirmnessStudy Value { get; } = new(); }

    [Fact]
    public void Placement_queries_and_assignment_evidence_are_blind_to_original_and_changed_source_words()
    {
        var original = study.PlacementSource;
        var forbidden = Reachable(new Relation(original.SourceFrame.Surface,
            new Relation(original.SourceTopology, original.SourceBindings)));
        foreach (var source in study.Source.Cases)
            forbidden.UnionWith(Reachable(new Relation(source.SourceFrame.Surface,
                new Relation(source.SourceTopology, source.SourceBindings))));
        foreach (var probe in study.Placements)
        {
            // Source publication provenance deliberately remains outside the
            // recognizer. Include its actual changed raw word input, while
            // excluding the published observation it legitimately contains.
            var provenanceInput = (Relation)probe.ExternalProvenance.Left;
            var sourceAndWords = (Relation)provenanceInput.Right;
            forbidden.UnionWith(Reachable(sourceAndWords));
        }
        foreach (var probe in study.Placements)
        {
            Assert.False(Reachable(probe.Placement.Surface).Overlaps(forbidden), probe.Name);
            Assert.False(Reachable(probe.Surface).Overlaps(forbidden), probe.Name);
            Assert.False(Reachable(probe.Considered).Overlaps(forbidden), probe.Name);
            Assert.Same(original.Observation, probe.Placement.Observation);
            Assert.DoesNotContain(Reachable(probe.Surface), element => element is Carrier or CarrierLocation);
        }
    }

    [Fact]
    public void The_baseline_places_twelve_distinct_motifs_into_twelve_native_chamber_requirements()
    {
        var probe = study.Placements[0];
        Assert.Equal(12, E(probe.Requirements).Length); Assert.Equal(12, E(probe.Accepted).Length);
        Assert.Empty(E(probe.Empty)); Assert.Empty(E(probe.Competing)); Assert.Empty(E(probe.Unresolved)); Assert.Empty(E(probe.Unplaced));
        var assignedUnits = E(probe.Accepted).Cast<Relation>().Select(binding => ((Relation)binding.Right).Left).ToArray();
        Assert.Equal(assignedUnits.Length, assignedUnits.Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.All(probe.Queries.Where(query => query.IsStrictlyInside), query => Assert.NotEmpty(E(query.Inside)));
    }

    [Fact]
    public void A_whole_chamber_shift_exposes_an_empty_requirement_and_two_candidates_for_its_neighbour()
    {
        var probe = study.Placements[1];
        Assert.Equal(12, E(probe.Accepted).Length); Assert.Single(E(probe.Empty)); Assert.Single(E(probe.Competing));
        Assert.Empty(E(probe.Unresolved)); Assert.Empty(E(probe.Unplaced));
        var competition = Assert.IsType<Relation>(E(probe.Competing)[0]);
        var assignments = E((Relation)competition.Right).Cast<Relation>().ToArray(); Assert.Equal(2, assignments.Length);
        Assert.All(assignments, assignment => Assert.Same(competition.Left, assignment.Left));
        Assert.NotSame(((Relation)assignments[0].Right).Left, ((Relation)assignments[1].Right).Left);
        Assert.Same(study.PlacementSource.Observation, probe.Placement.Observation);
    }

    [Fact]
    public void A_half_chamber_shift_leaves_the_same_motif_unplaced_without_changing_its_segment_observation()
    {
        var probe = study.Placements[2];
        Assert.Equal(11, E(probe.Accepted).Length); Assert.Single(E(probe.Empty));
        Assert.Same(study.Units.Occurrences[0].Read, Assert.Single(E(probe.Unplaced)));
        Assert.Empty(E(probe.Competing)); Assert.Empty(E(probe.Unresolved));
        Assert.Same(study.PlacementSource.Observation, probe.Placement.Observation);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public void Missing_projection_or_passage_envelopes_stay_unresolved_instead_of_empty(int index)
    {
        var probe = study.Placements[index];
        Assert.Empty(E(probe.Accepted)); Assert.Empty(E(probe.Empty)); Assert.Empty(E(probe.Competing)); Assert.Empty(E(probe.Unplaced));
        Assert.Equal(E(probe.Requirements).Length, E(probe.Unresolved).Length);
        Assert.All(probe.Queries, query => Assert.False(query.IsStrictlyInside));
        Assert.All(probe.Queries, query => Assert.NotEmpty(E(query.Unresolved)));
    }

    [Fact]
    public void Every_requirement_retains_all_positive_negative_and_unresolved_queries_against_the_same_units()
    {
        var sourceUnits = study.Units.Occurrences.Select(unit => unit.Read).ToArray();
        foreach (var probe in study.Placements)
        {
            var requirements = E(probe.Requirements).Cast<Relation>().ToArray();
            Assert.Equal(study.Chambers.Count, requirements.Length);
            Assert.Equal(requirements.Length * sourceUnits.Length, probe.Queries.Count);
            foreach (var requirement in requirements)
            {
                var promise = Assert.IsType<Relation>(requirement.Right); Assert.IsType<Placeholder>(promise.Left);
                Assert.Same(study.Units.Definition, promise.Right);
                Assert.Contains(study.Chambers, chamber => ReferenceEquals(chamber.Read, requirement.Left));
                var considered = Assert.Single(E(probe.Considered).Cast<Relation>(), row => ReferenceEquals(row.Left, requirement));
                var basisAndQueries = Assert.IsType<Relation>(considered.Right); Assert.Same(study.Units.Read, basisAndQueries.Left);
                var allQueries = E((Relation)basisAndQueries.Right).Cast<Relation>().ToArray();
                Assert.Equal(sourceUnits.Length, allQueries.Length);
                foreach (var unit in sourceUnits)
                {
                    var retained = Assert.Single(allQueries, row => ReferenceEquals(row.Left, unit));
                    Assert.Contains(probe.Queries, query => ReferenceEquals(query.Surface, retained.Right));
                }
            }
            Assert.All(probe.Queries, query => Assert.Contains(E(probe.Considered).Cast<Relation>()
                .SelectMany(row => E((Relation)((Relation)row.Right).Right)).Cast<Relation>(),
                retained => ReferenceEquals(retained.Right, query.Surface)));
        }
        var baseline = study.Placements[0];
        Assert.True(baseline.Queries.Count(query => !query.IsStrictlyInside) > baseline.Queries.Count(query => query.IsStrictlyInside));
    }

    [Fact]
    public void Projected_grid_without_shared_endpoints_has_admission_but_no_route_or_circuit_support()
    {
        var field = study.Fields.Single(field => field.Observation.Segments is Relation segments &&
            E(field.Observation.Sites).Length == 2 * E(segments).Length);
        Assert.NotEmpty(field.Evidence.Candidates);
        foreach (var evidence in field.Evidence.Candidates)
        {
            Assert.Empty(E(evidence.Supported)); Assert.Empty(E(evidence.RouteProofs)); Assert.Empty(E(evidence.CircuitProofs));
            Assert.Empty(E(evidence.Candidate.Commutations)); Assert.Equal(E(field.Observation.Segments).Length, E(evidence.AdmittedOnly).Length);
        }
    }

    [Fact]
    public void An_isolated_admitted_diagonal_is_not_promoted_to_periodic_or_circuit_support()
    {
        var candidate = study.Fields.SelectMany(field => field.Evidence.Candidates.Select(evidence => (field, evidence)))
            .First(pair => E(pair.evidence.Supported).Length == 40 && E(pair.evidence.AdmittedOnly).Length == 1 &&
                E(pair.evidence.Candidate.SelectedSegments).Length == 41 && E(pair.evidence.Outside).Length == 0 &&
                E(pair.evidence.Candidate.Basis).Any(channel => E(pair.field.Induction.Rhythms).Any(rhythm => ReferenceEquals(rhythm, channel))));
        var isolated = Assert.Single(E(candidate.evidence.AdmittedOnly));
        Assert.DoesNotContain(E(candidate.evidence.Supported), row => ReferenceEquals(row, isolated));
        Assert.DoesNotContain(E(candidate.evidence.RouteSupport), row => ReferenceEquals(row, isolated));
        Assert.DoesNotContain(E(candidate.evidence.CircuitSupport), row => ReferenceEquals(row, isolated));
        Assert.Contains(E(candidate.evidence.Candidate.Basis), channel => E(candidate.field.Induction.Rhythms).Any(rhythm => ReferenceEquals(rhythm, channel)));
    }

    [Fact]
    public void Retained_diamond_checks_change_locally_while_eleven_remote_occurrences_stay_supported()
    {
        var probes = study.Revalidations;
        var baseline = UnitChecks(probes[0]); Assert.Equal(12, baseline.Length); Assert.All(baseline, AssertSupported);
        var changed = UnitChecks(probes[1]); Assert.Single(changed, check => E(check.RefutedObligations).Length != 0);
        Assert.Equal(11, changed.Count(check => check.IsSupported));
        var unavailable = UnitChecks(probes[2]); Assert.Single(unavailable, check => E(check.UnresolvedObligations).Length != 0);
        Assert.Empty(unavailable.SelectMany(check => E(check.RefutedObligations)));
        Assert.Equal(11, unavailable.Count(check => check.IsSupported));
        foreach (var probe in probes)
        {
            Assert.Same(study.PlacementSource.Motifs, probe.Read.Prior);
            Assert.Same(probe.Observation, probe.Read.Current);
            var local = UnitChecks(probe).Single(check => ReferenceEquals(check.PriorOccurrence.Read, study.Units.Occurrences[0].Read));
            if (!ReferenceEquals(probe, probes[0])) Assert.False(local.IsSupported);
            Assert.All(UnitChecks(probe).Where(check => !ReferenceEquals(check.PriorOccurrence.Read, local.PriorOccurrence.Read)), AssertSupported);
        }
    }

    [Fact]
    public void Closed_and_open_withdrawal_revalidate_the_same_prior_diamond_differently()
    {
        var closed = UnitChecks(study.Revalidations[3]); var open = UnitChecks(study.Revalidations[4]);
        var refuted = Assert.Single(closed, check => E(check.RefutedObligations).Length != 0);
        var unresolved = Assert.Single(open, check => E(check.UnresolvedObligations).Length != 0);
        Assert.Same(refuted.PriorOccurrence.Read, unresolved.PriorOccurrence.Read);
        Assert.Empty(E(refuted.UnresolvedObligations)); Assert.Empty(E(unresolved.RefutedObligations));
        Assert.True(study.Revalidations[3].Read.IsComplete); Assert.False(study.Revalidations[4].Read.IsComplete);
    }

    private QualitativeRecognitionCheck[] UnitChecks(QualitativeRevalidationProbe probe) => probe.Read.Checks
        .Where(check => ReferenceEquals(check.PriorFamily.Read, study.Units.Read)).ToArray();
    private static void AssertSupported(QualitativeRecognitionCheck check)
    { Assert.True(check.IsSupported); Assert.Empty(E(check.RefutedObligations)); Assert.Empty(E(check.UnresolvedObligations)); Assert.NotEmpty(E(check.SupportedObligations)); }
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static HashSet<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var work = new Stack<IElement>(); work.Push(root);
        while (work.TryPop(out var element))
        { if (!seen.Add(element) || element is not Relation relation) continue; work.Push(relation.Left); work.Push(relation.Right); }
        return seen;
    }
}
