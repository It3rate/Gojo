using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeRecognitionReadingsTests
{
    [Fact]
    public void EveryMotifOccurrenceIsPartitionedAgainstEveryDispositionWithoutOwnership()
    {
        var observation = TriangleField(); var motifs = new QualitativeMotifInduction(observation, maxCycleEdges: 4);
        var dispositions = new QualitativeDispositionInduction(observation); var readings = new QualitativeRecognitionReadings(motifs, dispositions);
        Assert.True(readings.IsComplete); Assert.Equal(E(dispositions.Candidates).Length, readings.Readings.Count);
        var totalOccurrences = motifs.Families.Sum(family => family.Occurrences.Count);
        foreach (var reading in readings.Readings)
        {
            Assert.Equal(totalOccurrences, E(reading.Contained).Length + E(reading.Straddling).Length + E(reading.Outside).Length);
            Assert.Equal(totalOccurrences, E(reading.Partitions).Length);
            Assert.Same(reading.Candidate.Read, ((Relation)reading.Surface.Left).Left);
            Assert.Same(motifs.Surface, ((Relation)reading.Surface.Left).Right);
            Assert.Same(reading.Contained, ((Relation)reading.Surface.Right).Left);
            foreach (var item in E(reading.Partitions))
            {
                var partition = Assert.IsType<Relation>(item); var use = Assert.IsType<Relation>(partition.Left);
                var actualFamily = motifs.Families.Single(family => ReferenceEquals(family.Read, use.Left));
                var occurrence = actualFamily.Occurrences.Single(occurrence => ReferenceEquals(occurrence.Read, use.Right));
                var edges = Assert.IsType<Relation>(partition.Right);
                var inside = E((Relation)edges.Left); var outside = E((Relation)edges.Right);
                Assert.Equal(E(occurrence.Segments).Length, inside.Length + outside.Length);
                Assert.All(inside, edge => Assert.Contains(E(reading.Candidate.SelectedSegments), selected => ReferenceEquals(edge, selected)));
                Assert.All(outside, edge => Assert.DoesNotContain(E(reading.Candidate.SelectedSegments), selected => ReferenceEquals(edge, selected)));
                Assert.Equal(inside.Length + outside.Length, inside.Concat(outside).Distinct(ReferenceEqualityComparer.Instance).Count());
            }
        }
        var coarse = readings.Readings.First(reading => E(reading.Candidate.Residue).Length != 0);
        Assert.NotEmpty(E(coarse.Contained)); Assert.NotEmpty(E(coarse.Straddling));
        var full = Assert.Single(readings.Readings, reading => E(reading.Candidate.Basis).Length == 3);
        Assert.Empty(E(full.Straddling)); Assert.Empty(E(full.Outside));
    }

    [Fact]
    public void UniqueCoverageNeverDuplicatesAnEdgeThatParticipatesInSeveralMotifs()
    {
        var observation = TriangleField(); var motifs = new QualitativeMotifInduction(observation, maxCycleEdges: 4);
        var readings = new QualitativeRecognitionReadings(motifs, new(observation));
        foreach (var reading in readings.Readings)
        {
            var covered = E(reading.Covered); var remainder = E(reading.Remainder);
            Assert.Equal(covered.Length, covered.Distinct(ReferenceEqualityComparer.Instance).Count());
            Assert.Equal(E(reading.Candidate.SelectedSegments).Length, covered.Length + remainder.Length);
            Assert.All(covered, edge => Assert.Contains(E(reading.Candidate.SelectedSegments), selected => ReferenceEquals(edge, selected)));
            Assert.All(remainder, edge => Assert.DoesNotContain(covered, selected => ReferenceEquals(edge, selected)));
        }
    }

    [Fact]
    public void RicherSupportRetainsBothReadingsAndActualInclusionCorrespondence()
    {
        var observation = TriangleField(); var readings = new QualitativeRecognitionReadings(new(observation, maxCycleEdges: 4), new(observation));
        Assert.NotEmpty(E(readings.Refinements));
        foreach (var item in E(readings.Refinements))
        {
            var extension = Assert.IsType<Relation>(item); var participants = Assert.IsType<Relation>(extension.Left);
            var lower = readings.Readings.Single(reading => ReferenceEquals(reading.Surface, participants.Left));
            var richer = readings.Readings.Single(reading => ReferenceEquals(reading.Surface, participants.Right));
            Assert.True(E(lower.Candidate.SelectedSegments).Length < E(richer.Candidate.SelectedSegments).Length);
            var proof = Assert.IsType<Relation>(extension.Right); var correspondence = E((Relation)proof.Left);
            Assert.Equal(E(lower.Candidate.SelectedSegments).Length, correspondence.Length);
            Assert.All(correspondence.Cast<Relation>(), pair => Assert.Same(pair.Left, pair.Right));
            var introduced = E((Relation)((Relation)proof.Right).Left);
            Assert.Equal(E(richer.Candidate.SelectedSegments).Length - E(lower.Candidate.SelectedSegments).Length, introduced.Length);
            Assert.Empty(E(richer.Candidate.Unsettled)); Assert.Empty(E(richer.Candidate.Competing));
        }
    }

    [Fact]
    public void ACompleteMismatchCannotBeAdvertisedAsRicherSettledSupport()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site(); var d = f.Site(); var e = f.Site();
        f.Edge(a, b, f.Frame.Right); f.Edge(a, c, f.Frame.Up); f.Edge(b, d, f.Frame.Up); f.Edge(c, e, f.Frame.Right);
        f.Edge(a, d, f.Frame.Right, f.Frame.Up);
        var observation = f.Observation(); var readings = new QualitativeRecognitionReadings(new(observation, maxCycleEdges: 4), new(observation));
        var full = Assert.Single(readings.Readings, reading => E(reading.Candidate.Basis).Length == 3);
        Assert.NotEmpty(E(full.Candidate.Unsettled));
        Assert.DoesNotContain(E(readings.Refinements).Cast<Relation>(), row => ReferenceEquals(((Relation)row.Left).Right, full.Surface));
    }

    [Fact]
    public void ExhaustedMotifSearchKeepsSelectedUnexplainedParticipationAndSearchRemainder()
    {
        var observation = TriangleField(); var motifs = new QualitativeMotifInduction(observation, proposalCapacity: 0);
        var readings = new QualitativeRecognitionReadings(motifs, new(observation));
        Assert.False(readings.IsComplete); Assert.NotEmpty(E(readings.Unresolved));
        foreach (var reading in readings.Readings)
        {
            Assert.Empty(E(reading.Covered)); Assert.Empty(E(reading.Contained));
            Assert.Equal(E(reading.Candidate.SelectedSegments).Length, E(reading.Remainder).Length);
        }
    }

    [Fact]
    public void SeparateLookalikeObservationCannotBeSilentlyJoined()
    {
        var motifs = new QualitativeMotifInduction(TriangleField()); var dispositions = new QualitativeDispositionInduction(TriangleField());
        var readings = new QualitativeRecognitionReadings(motifs, dispositions);
        Assert.False(readings.IsComplete); Assert.NotEmpty(E(readings.Blocked));
        Assert.Empty(readings.Readings); Assert.Empty(E(readings.Refinements));
    }

    private static QualitativeRecognitionObservation TriangleField()
    {
        var f = new Fixture(); var sites = new IElement[3, 3];
        for (var y = 0; y < 3; y++) for (var x = 0; x < 3; x++) sites[x, y] = f.Site();
        for (var y = 0; y < 3; y++) for (var x = 0; x < 2; x++) f.Edge(sites[x, y], sites[x + 1, y], f.Frame.Right);
        for (var y = 0; y < 2; y++) for (var x = 0; x < 3; x++) f.Edge(sites[x, y], sites[x, y + 1], f.Frame.Up);
        for (var y = 0; y < 2; y++) for (var x = 0; x < 2; x++) f.Edge(sites[x, y], sites[x + 1, y + 1], f.Frame.Right, f.Frame.Up);
        return f.Observation();
    }
    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        public List<IElement> Sites { get; } = [];
        public List<IElement> Edges { get; } = [];
        public IElement Site() { var site = new Placeholder(); Sites.Add(site); return site; }
        public void Edge(IElement from, IElement to, params IElement[] directions) =>
            Edges.Add(new Relation(new Placeholder(), new Relation(new Relation(from, to), S(directions))));
        public QualitativeRecognitionObservation Observation() => new(Frame, S(Sites.ToArray()), S(Edges.ToArray()));
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
