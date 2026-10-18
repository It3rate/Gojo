using Gojo.Core;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;

namespace QualitativeExecution.Tests;

public sealed class QualitativeRecognitionStudyTests : IClassFixture<QualitativeRecognitionStudyTests.StudyFixture>
{
    private readonly QualitativeRecognitionStudy study;
    public QualitativeRecognitionStudyTests(StudyFixture fixture) => study = fixture.Value;
    public sealed class StudyFixture { public QualitativeRecognitionStudy Value { get; } = new(); }

    [Fact]
    public void Every_fixture_publishes_complete_native_segments_with_external_only_presentation_positions()
    {
        Assert.Equal(10, study.Cases.Count);
        foreach (var item in study.Cases)
        {
            Assert.True(item.Publisher.IsComplete, item.Id); Assert.True(item.Observation.IsComplete, item.Id);
            var observation = Reachable(item.Observation.Surface);
            Assert.DoesNotContain(item.SourceFrame.Surface, observation);
            Assert.DoesNotContain(item.SourceTopology, observation); Assert.DoesNotContain(item.SourceBindings, observation);
            Assert.All(Entries((Relation)item.SourceTopology.Left), sourceSite => Assert.DoesNotContain(sourceSite, observation));
            var source = Reachable(new Relation(item.SourceFrame.Surface, new Relation(item.SourceTopology, item.SourceBindings)));
            Assert.False(source.Overlaps(observation), item.Id + " exposed a source identity through its observation");
            var recognition = Reachable(new Relation(item.Motifs.Surface, new Relation(item.Disposition.Surface, item.Readings.Surface)));
            Assert.False(source.Overlaps(recognition), item.Id + " exposed a source identity through a derived recognition witness");
            Assert.All(item.Positions.Keys, observedSite => Assert.Contains(observedSite, observation));
            Assert.DoesNotContain(observation, element => element is Carrier or CarrierLocation);
        }
    }

    [Fact]
    public void Plain_triangular_and_hexagonal_sources_offer_different_positive_cycle_evidence()
    {
        var square = study.Cases.Single(item => item.Id == "R01");
        var triangles = study.Cases.Single(item => item.Id == "R04");
        var hexagons = study.Cases.Single(item => item.Id == "R07");
        Assert.Contains(square.Motifs.Families, family => family.Kind == "cycle" && family.SegmentCount == 4 && family.Occurrences.Count > 1);
        Assert.Contains(triangles.Motifs.Families, family => family.Kind == "cycle" && family.SegmentCount == 3 && family.Occurrences.Count > 1);
        Assert.Contains(hexagons.Motifs.Families, family => family.Kind == "cycle" && family.SegmentCount == 6 && family.Occurrences.Count > 1);
    }

    [Fact]
    public void Separate_zigzag_strips_repeat_without_manufacturing_closed_cells()
    {
        var zigzag = study.Cases.Single(item => item.Id == "R06");
        Assert.DoesNotContain(zigzag.Motifs.Families, family => family.Kind == "cycle");
        Assert.Contains(zigzag.Motifs.Families, family => family.Kind == "walk" && family.Occurrences.Count > 1);
        Assert.NotEmpty(Entries(zigzag.Disposition.Rhythms));
    }

    [Fact]
    public void Stress_case_preserves_supported_matches_and_an_explicit_search_cut()
    {
        var dense = study.Cases.Single(item => item.Id == "R10");
        Assert.False(dense.Motifs.IsComplete); Assert.NotEmpty(Entries(dense.Motifs.Unsearched));
        Assert.NotEmpty(dense.Motifs.Families); Assert.NotEmpty(Entries(dense.Motifs.Covered));
        Assert.Empty(Entries(dense.Motifs.Unresolved));
    }

    [Theory]
    [InlineData("R01")]
    [InlineData("R02")]
    [InlineData("R03")]
    public void Square_sheared_and_diagonal_grids_share_a_two_channel_commutation_read(string id)
    {
        var item = study.Cases.Single(item => item.Id == id);
        var candidate = Assert.Single(Candidates(item), candidate => Entries(candidate.Basis).Length == 2 &&
            Entries(candidate.Residue).Length == 0 && Entries(candidate.Commutations).Length > 0);
        Assert.Empty(Entries(candidate.Unsettled)); Assert.Empty(Entries(candidate.Closures));
        foreach (var row in Entries(candidate.Commutations).Cast<Relation>())
        {
            var paths = (Relation)row.Right;
            var first = Entries((Relation)paths.Left).Cast<Relation>().ToArray();
            var second = Entries((Relation)paths.Right).Cast<Relation>().ToArray();
            Assert.Same(End(first[^1]), End(second[^1]));
        }
    }

    [Fact]
    public void Triangular_field_preserves_partial_grid_and_more_complete_three_channel_candidates()
    {
        var item = study.Cases.Single(item => item.Id == "R04"); var candidates = Candidates(item);
        var coarse = candidates.First(candidate => Entries(candidate.Basis).Length == 2 &&
            Entries(candidate.Commutations).Length > 0 && Entries(candidate.Residue).Length > 0);
        Assert.NotEmpty(Entries(coarse.UnexpectedClosures));
        var detailed = Assert.Single(candidates, candidate => Entries(candidate.Basis).Length == 3);
        Assert.Empty(Entries(detailed.Residue)); Assert.NotEmpty(Entries(detailed.Closures));
        Assert.Empty(Entries(detailed.UnexpectedClosures));
        Assert.Contains(Entries(item.Disposition.Candidates), row => ReferenceEquals(row, coarse.Read));
    }

    [Fact]
    public void Alternating_offsets_offer_a_two_phase_transport_explanation_alongside_straight_family_reads()
    {
        var item = study.Cases.Single(item => item.Id == "R05");
        var rhythms = Entries(item.Disposition.Rhythms);
        Assert.NotEmpty(rhythms);
        Assert.Contains(Candidates(item), candidate => Entries(candidate.Basis).Length == 2 &&
            Entries(candidate.Residue).Length == 0 && Entries(candidate.Commutations).Length > 0 &&
            Entries(candidate.Basis).Any(channel => rhythms.Any(rhythm => ReferenceEquals(channel, rhythm))));
        Assert.Contains(Candidates(item), candidate => Entries(candidate.Residue).Length > 0);
    }

    [Fact]
    public void Zigzags_have_long_periodic_flows_and_hexagons_have_six_cycles_without_triangle_evidence()
    {
        var zigzag = study.Cases.Single(item => item.Id == "R06");
        Assert.All(Candidates(zigzag), candidate => Assert.Empty(Entries(candidate.Commutations)));
        Assert.Contains(Entries(zigzag.Disposition.Rhythms).Cast<Relation>(), row =>
        {
            if (!zigzag.Disposition.TryReadRhythm(row, out var rhythm)) return false;
            return Entries(rhythm.Residue).Length == 0 && Entries(rhythm.Routes).Cast<Relation>()
                .Any(route => QualitativeDispositionInduction.RouteUses(route).Length > 4);
        });
        var hexagons = study.Cases.Single(item => item.Id == "R07");
        Assert.Empty(Entries(hexagons.Disposition.TriangleCircuits));
        Assert.DoesNotContain(hexagons.Motifs.Families, family => family.Kind == "cycle" && family.SegmentCount == 3);
    }

    private static QualitativeDispositionCandidate[] Candidates(QualitativeRecognitionCase item) =>
        Entries(item.Disposition.Candidates).Cast<Relation>().Select(row =>
        { Assert.True(item.Disposition.TryReadCandidate(row, out var candidate)); return candidate; }).ToArray();
    private static IElement End(Relation use) => ((Relation)((Relation)use.Right).Left).Right;

    private static IElement[] Entries(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static HashSet<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var todo = new Stack<IElement>(); todo.Push(root);
        while (todo.TryPop(out var element))
        { if (!seen.Add(element) || element is not Relation relation) continue; todo.Push(relation.Left); todo.Push(relation.Right); }
        return seen;
    }
}
