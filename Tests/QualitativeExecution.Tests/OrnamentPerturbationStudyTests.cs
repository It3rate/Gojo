using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentPerturbationStudyTests
{
    private static readonly Lazy<OrnamentPerturbationStudy> Shared = new(() => new OrnamentPerturbationStudy());
    private static OrnamentPerturbationStudy Study => Shared.Value;
    private static OrnamentPerturbationStudy.State Baseline => State("P01", "original");

    [Fact]
    public void EarlierCandidatesAndOriginalExposedStateRemainAvailableUnchanged()
    {
        var original = Study.Field.Levels[0];
        Assert.Same(original.Read.Topology, Baseline.Topology);
        Assert.Same(original.Bindings, Baseline.Bindings);
        Assert.Contains(E(original.Bindings), row => ReferenceEquals(row, Study.TargetBinding));
        foreach (var state in States())
        {
            Assert.Same(Study.Forward.Occurrences, state.Forward.Occurrences);
            Assert.Same(Study.Forward.Strands!.Surface, state.Forward.Previous);
            Assert.Same(Study.Reverse.Occurrences, state.Reverse.Occurrences);
            foreach (var check in state.Cells)
                Assert.Contains(E(Study.Diamonds.Cells!.Families), family => ReferenceEquals(family, check.Candidate));
            if (state.Patch is not { } patch) continue;
            Assert.Same(original.Read.Topology, patch.BeforeTopology);
            Assert.Same(original.Bindings, patch.BeforeBindings);
            Assert.Same(state.Topology, patch.Topology);
            Assert.Same(state.Bindings, patch.Bindings);
        }
        Assert.Equal(39, Baseline.Cells.Sum(read => E(read.Supported).Length));
        Assert.Equal(4, E(Baseline.Forward.Supported).Length);
        Assert.Equal(4, E(Baseline.Reverse.Supported).Length);
    }

    [Fact]
    public void TargetIsAnActualSelectivePassageBetweenTwoIncidenceFamilies()
    {
        var topology = Study.Field.Levels[0].Read.Topology;
        var edgeRows = E((Relation)((Relation)topology.Right).Left);
        var faces = E((Relation)((Relation)topology.Right).Right).Cast<Relation>().ToArray();
        Assert.Contains(edgeRows, edge => ReferenceEquals(edge, Study.TargetEdge));
        var uses = faces.Where(face => E((Relation)face.Right).Cast<Relation>()
            .Any(dart => ReferenceEquals(dart.Left, Study.TargetEdge.Left))).ToArray();
        Assert.Equal(2, uses.Length);
        Assert.Equal(new[] { 4, 8 }, uses.Select(face => E((Relation)face.Right).Length).Order().ToArray());
        Assert.Single(Routes(Study.Forward), route => route.Uses.Any(use => ReferenceEquals(use.Left, Study.TargetEdge.Left)));
        Assert.DoesNotContain(Routes(Study.Reverse), route => route.Uses.Any(use => ReferenceEquals(use.Left, Study.TargetEdge.Left)));
        Assert.Contains(Study.TargetRole, new[] { ((Relation)Study.TargetEdge.Right).Left, ((Relation)Study.TargetEdge.Right).Right });
    }

    [Theory]
    [InlineData("right-1", 4, 0)]
    [InlineData("right-2", 3, 1)]
    [InlineData("right-3", 3, 1)]
    [InlineData("right-4", 3, 1)]
    public void WordPerturbationPreservesCyclesButCrossesAHeadingBoundary(string id, int supported, int refuted)
    {
        var state = State("P01", id);
        Assert.Same(Baseline.Topology, state.Topology);
        Assert.Equal(39, state.Cells.Sum(read => E(read.Supported).Length));
        Assert.All(state.Cells, read => { Assert.Empty(E(read.Refuted)); Assert.Empty(E(read.Unresolved)); });
        Assert.Equal(supported, E(state.Forward.Supported).Length);
        Assert.Equal(refuted, E(state.Forward.Refuted).Length);
        Assert.Empty(E(state.Forward.Unresolved));
        Assert.Equal(4, E(state.Reverse.Supported).Length);
        Assert.Empty(E(state.Reverse.Refuted));
        Assert.Empty(E(state.Reverse.Unresolved));
        Assert.Same(Study.TargetRole, E(state.Bindings).Cast<Relation>().Single(row => ReferenceEquals(row.Left, Study.TargetRole)).Left);
    }

    [Fact]
    public void WithdrawingOnePassageRefutesOnlyDependentOriginalCellsAndLongRoute()
    {
        var state = State("P02", "withdraw");
        var patch = Assert.IsType<OrnamentStructurePerturbation>(state.Patch);
        Assert.Equal(2, E(patch.WithdrawnFaces).Length);
        Assert.DoesNotContain(E((Relation)((Relation)state.Topology.Right).Left), edge =>
            edge is Relation row && ReferenceEquals(row.Left, Study.TargetEdge.Left));
        Assert.Equal(37, state.Cells.Sum(read => E(read.Supported).Length));
        Assert.Equal(2, state.Cells.Sum(read => E(read.Refuted).Length));
        Assert.All(state.Cells, read => Assert.Empty(E(read.Unresolved)));
        Assert.Equal(3, E(state.Forward.Supported).Length);
        Assert.Single(E(state.Forward.Refuted));
        Assert.Empty(E(state.Forward.Unresolved));
        Assert.Equal(4, E(state.Reverse.Supported).Length);
        Assert.Empty(E(state.Reverse.Refuted));
        foreach (var oldFace in E(patch.WithdrawnFaces))
            Assert.DoesNotContain(E((Relation)((Relation)state.Topology.Right).Right), current => ReferenceEquals(current, oldFace));
    }

    [Fact]
    public void DetachmentCanKeepTheSameWordWhileInvalidatingActualContinuity()
    {
        var state = State("P02", "detach");
        var active = E((Relation)((Relation)state.Topology.Right).Left).Cast<Relation>()
            .Single(edge => ReferenceEquals(edge.Left, Study.TargetEdge.Left));
        var ends = Assert.IsType<Relation>(active.Right);
        var originalEnds = Assert.IsType<Relation>(Study.TargetEdge.Right);
        var fresh = ReferenceEquals(originalEnds.Left, Study.TargetRole) ? ends.Left : ends.Right;
        Assert.NotSame(Study.TargetRole, fresh);
        var binding = E(state.Bindings).Cast<Relation>().Single(row => ReferenceEquals(row.Left, fresh));
        Assert.Same(Study.TargetBinding.Right, binding.Right);
        Assert.Equal(3, E(state.Forward.Supported).Length);
        Assert.Single(E(state.Forward.Refuted));
        Assert.Empty(E(state.Forward.Unresolved));
        Assert.Equal(2, state.Cells.Sum(read => E(read.Refuted).Length));
        Assert.Equal(4, E(state.Reverse.Supported).Length);
    }

    [Theory]
    [InlineData("open-word")]
    [InlineData("foreign-reach")]
    public void UnavailableWordDetailRemainsLocalInsteadOfRefutingRemoteCandidates(string id)
    {
        var state = State("P03", id);
        Assert.Equal(39, state.Cells.Sum(read => E(read.Supported).Length));
        Assert.Equal(3, E(state.Forward.Supported).Length);
        Assert.Empty(E(state.Forward.Refuted));
        Assert.Single(E(state.Forward.Unresolved));
        Assert.Equal(4, E(state.Reverse.Supported).Length);
        Assert.Empty(E(state.Reverse.Unresolved));
        Assert.Empty(E(state.Fresh.Routes));
        Assert.NotEmpty(E(state.Fresh.Unresolved));
        var witness = Assert.IsType<Relation>(E(state.Forward.Unresolved).Single());
        Assert.True(state.Forward.TryReadWitness(witness, out var occurrence, out _, out var failures, out var open));
        Assert.Contains(E(Study.Forward.Occurrences), old => ReferenceEquals(old, occurrence));
        Assert.Empty(E(failures)); Assert.NotEmpty(E(open));
    }

    [Fact]
    public void EqualShareLandmarksExposeResolutionRemaindersBeforeTranslationFailure()
    {
        var frame = Study.Field.Source.Frame;
        foreach (var actual in E(Baseline.LandmarkBindings).Cast<Relation>())
        {
            var original = E(Study.Diamonds.Rhythm!.Bindings).Cast<Relation>().Single(row => ReferenceEquals(row.Left, actual.Left));
            var expected = E((Relation)original.Right).Cast<Relation>().Reverse().Select(move => (IElement)
                QualitativeMotionFrame.Move(E(frame.Opposites).Cast<Relation>().Single(law => ReferenceEquals(law.Left, move.Left)).Right, move.Right)).ToArray();
            var difference = frame.Reduce((Relation)actual.Right, StructuralConstraint.Closed(expected));
            Assert.True(frame.TryReadResult(difference, out var remaining, out _, out var unknown));
            Assert.Empty(E(remaining)); Assert.Empty(E(unknown));
        }
        Assert.Equal(E(Study.Diamonds.Rhythm!.Successors).Length, E(Baseline.Rhythm.Supported).Length);
        var partial = State("P01", "right-1");
        Assert.Contains(E(partial.LandmarkBindings).Cast<Relation>(), binding => !Closed((Relation)binding.Right));
        Assert.NotEmpty(E(partial.Rhythm.Unresolved)); Assert.Empty(E(partial.Rhythm.Refuted));
        var complete = State("P01", "right-4");
        Assert.All(E(complete.LandmarkBindings).Cast<Relation>(), binding => Assert.True(Closed((Relation)binding.Right)));
        Assert.NotEmpty(E(complete.Rhythm.Refuted)); Assert.Empty(E(complete.Rhythm.Unresolved));
    }

    [Fact]
    public void ComparisonConsumesActualDisjointPartitionsAndRetainsEveryCandidateRead()
    {
        var universe = E((Relation)((Relation)Baseline.Topology.Right).Left).Cast<Relation>().ToArray();
        var training = E(Study.TrainingEdges).Cast<Relation>().ToArray();
        var held = E(Study.HeldOutEdges).Cast<Relation>().ToArray();
        Assert.NotEmpty(training); Assert.NotEmpty(held);
        Assert.All(training.Concat(held), edge => Assert.Contains(universe, actual => ReferenceEquals(actual, edge)));
        Assert.DoesNotContain(training, edge => held.Any(other => ReferenceEquals(edge.Left, other.Left)));
        var trainingOccurrences = Set(E(Study.TrainingOccurrences));
        Assert.False(trainingOccurrences.Overlaps(E(Study.HeldOutOccurrences)));
        trainingOccurrences.UnionWith(E(Study.HeldOutOccurrences));
        Assert.True(trainingOccurrences.SetEquals(E(Study.Diamonds.Rhythm!.Occurrences)));
        Assert.Equal(5, Baseline.HeadingComparison.Candidates.Length);
        Assert.Equal(4, Baseline.TranslationComparison.Candidates.Length);
        foreach (var candidate in Baseline.HeadingComparison.Candidates)
        {
            var train = Assert.IsType<OrnamentStrandDiscovery>(candidate.Training.Strands);
            var test = Assert.IsType<OrnamentStrandDiscovery>(candidate.HeldOut.Strands);
            Assert.True(Set(E((Relation)((Relation)train.SourceTopology.Right).Left)).SetEquals(training));
            Assert.True(Set(E((Relation)((Relation)test.SourceTopology.Right).Left)).SetEquals(held));
        }
    }

    [Fact]
#pragma warning disable xUnit2017
    public void CaptureRetainsOriginalCandidatesReapplicationsAndComparisonWitnessesWithoutCarriers()
    {
        var seen = Set([]); var todo = new Stack<IElement>(); todo.Push(Study.Surface);
        while (todo.TryPop(out var item))
        {
            if (!seen.Add(item)) continue;
            Assert.False(item is Carrier or CarrierLocation);
            if (item is Relation relation) { todo.Push(relation.Left); todo.Push(relation.Right); }
        }
        Assert.True(seen.Contains(Study.Original.Surface));
        Assert.True(seen.Contains(Study.TargetSelection));
        Assert.True(seen.Contains(Study.PartitionEvidence));
        foreach (var state in States())
        {
            Assert.True(seen.Contains(state.Surface));
            foreach (var check in state.Cells.Concat([state.Forward, state.Reverse, state.Rhythm]))
            {
                Assert.True(seen.Contains(check.Surface)); Assert.True(seen.Contains(check.Previous));
                Assert.True(seen.Contains(check.Supported)); Assert.True(seen.Contains(check.Refuted)); Assert.True(seen.Contains(check.Unresolved));
            }
            Assert.True(seen.Contains(state.LandmarkReads));
            Assert.True(seen.Contains(state.HeadingComparison.Surface));
            Assert.True(seen.Contains(state.TranslationComparison.Surface));
            if (state.Patch is { } patch) Assert.True(seen.Contains(patch.Timeline));
        }
    }
#pragma warning restore xUnit2017

    private static OrnamentDiscoveredStrand[] Routes(OrnamentDiscoveryStudy.Reading reading) => E(reading.Occurrences).Cast<Relation>().Select(route =>
    { Assert.True(reading.Strands!.TryReadRoute(route, out var parts)); return parts; }).ToArray();
    private static OrnamentPerturbationStudy.State State(string series, string id) => Study.Experiments.Single(item => item.Id == series).States.Single(item => item.Id == id);
    private static OrnamentPerturbationStudy.State[] States() => Study.Experiments.SelectMany(series => series.States).Distinct(ReferenceEqualityComparer.Instance).Cast<OrnamentPerturbationStudy.State>().ToArray();
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static HashSet<IElement> Set(IEnumerable<IElement> source) => new(source, ReferenceEqualityComparer.Instance);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
