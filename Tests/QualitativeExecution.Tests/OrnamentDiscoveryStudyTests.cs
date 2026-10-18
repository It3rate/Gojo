using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentDiscoveryStudyTests
{
    private static readonly Lazy<OrnamentDiscoveryStudy> Shared = new(() => new OrnamentDiscoveryStudy());

    [Fact]
    public void EveryEmphasisPreservesTheWholeFieldAsAnExactSelectionAndComplement()
    {
        Assert.Equal(7, Shared.Value.Readings.Length);
        foreach (var reading in Shared.Value.Readings)
        {
            var whole = Set(E(reading.Whole)); var selected = Set(E(reading.Selected)); var complement = Set(E(reading.Complement));
            Assert.Equal(E(reading.Whole).Length, whole.Count); Assert.Equal(E(reading.Selected).Length, selected.Count);
            Assert.Equal(E(reading.Complement).Length, complement.Count); Assert.NotEmpty(selected); Assert.NotEmpty(complement);
            Assert.False(selected.Overlaps(complement)); Assert.True(selected.IsSubsetOf(whole)); Assert.True(complement.IsSubsetOf(whole));
            selected.UnionWith(complement); Assert.True(selected.SetEquals(whole));
            var actual = reading.Patch.Executions.SelectMany(execution => E(execution.Join!.Graph.Strokes))
                .Concat(reading.Patch.Decorations.SelectMany(node => E(node.Current.Join.Graph.Strokes))).ToArray();
            Assert.True(whole.SetEquals(actual));
        }
    }

    [Fact]
    public void RepeatedCellFamiliesRetainActualBoundariesAtSeveralResolutions()
    {
        foreach (var (id, arity, count) in new[] { ("D02", 4, 24), ("D03", 8, 15), ("D04", 3, 44), ("D05", 6, 20) })
        {
            var reading = Reading(id); var discovery = Assert.IsType<OrnamentCellDiscovery>(reading.Cells);
            Assert.True(discovery.IsComplete); Assert.True(discovery.TryReadFamily(reading.Candidate, out var family));
            Assert.Equal(arity, family.BoundaryArity); Assert.Equal(count, E(family.Occurrences).Length);
            Assert.Same(family.Occurrences, reading.Occurrences);
            var sourceFaces = E((Relation)((Relation)discovery.SourceTopology.Right).Right);
            foreach (var occurrence in E(reading.Occurrences).Cast<Relation>())
            {
                Assert.Contains(sourceFaces, face => ReferenceEquals(face, occurrence.Left));
                Assert.True(discovery.TryReadOccurrence(family.Read, occurrence, out _, out var boundary, out var correspondence));
                Assert.Equal(arity, E(boundary).Length); Assert.Equal(arity, E(correspondence).Length);
                foreach (var row in E(correspondence).Cast<Relation>())
                    Assert.Contains(E(boundary), actual => ReferenceEquals(actual, row.Right));
            }
        }
    }

    [Fact]
    public void AlternatingGardensFollowWitnessedTwoDimensionalSuccessorsRatherThanListParity()
    {
        var reading = Reading("D02"); var rhythm = Assert.IsType<OrnamentOccurrenceRhythm>(reading.Rhythm);
        Assert.True(rhythm.IsComplete); Assert.Empty(E(rhythm.Conflicts)); Assert.Empty(E(rhythm.Unresolved));
        var phases = E(rhythm.Phases); var items = E(rhythm.Occurrences);
        Assert.Equal(24, E(rhythm.Assignments).Length); Assert.Equal(2, phases.Length);
        foreach (var phase in phases) Assert.Equal(12, E(rhythm.Assignments).Cast<Relation>().Count(row => ReferenceEquals(row.Right, phase)));
        var evenIndexSuccessor = false; var usedTranslations = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        foreach (var successor in E(rhythm.Successors).Cast<Relation>())
        {
            Assert.True(rhythm.TryReadSuccessor(successor, out var from, out var to, out var step, out var proof));
            Assert.Contains(E(rhythm.Comparisons), actual => ReferenceEquals(actual, proof));
            Assert.True(rhythm.TryReadAssignment(from, out var fromPhase)); Assert.True(rhythm.TryReadAssignment(to, out var toPhase));
            Assert.NotSame(fromPhase, toPhase); usedTranslations.Add(step);
            var a = Array.FindIndex(items, item => ReferenceEquals(item, from));
            var b = Array.FindIndex(items, item => ReferenceEquals(item, to));
            Assert.True(a >= 0 && b >= 0);
            evenIndexSuccessor |= Math.Abs(a - b) % 2 == 0;
        }
        Assert.Equal(2, usedTranslations.Count); Assert.True(evenIndexSuccessor);
    }

    [Fact]
    public void GrowingVinesKeepActualEndpointContinuityAndCompletedConstructionCorrespondence()
    {
        foreach (var id in new[] { "D06", "D07" })
        {
            var reading = Reading(id); var discovery = Assert.IsType<OrnamentStrandDiscovery>(reading.Strands);
            Assert.True(discovery.IsComplete); Assert.Empty(E(discovery.Competing));
            var nativeEdges = E(reading.Patch.Levels[0].Read.Edges); var routeEdges = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
            var routes = E(reading.Occurrences).Cast<Relation>().ToArray();
            Assert.Equal(4, routes.Length);
            foreach (var route in routes)
            {
                Assert.True(discovery.TryReadRoute(route, out var strand)); Assert.False(strand.IsClosed);
                Assert.Equal(17, strand.Uses.Length); Assert.True(strand.Uses.Length >= E(discovery.Pattern).Length * 2);
                Assert.Same(strand.Start, ((Relation)strand.Uses[0].Right).Left);
                Assert.Same(strand.End, ((Relation)strand.Uses[^1].Right).Right);
                for (var index = 0; index < strand.Uses.Length; index++)
                {
                    var use = strand.Uses[index]; routeEdges.Add(use.Left);
                    Assert.Contains(nativeEdges, edge => ReferenceEquals(edge, use.Left));
                    if (index != 0) Assert.Same(((Relation)strand.Uses[index - 1].Right).Right, ((Relation)use.Right).Left);
                }
            }
            var execution = reading.Patch.Executions[0]; var projection = Assert.IsType<Relation>(reading.Correspondence.Right);
            var projected = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
            foreach (var evidence in E(projection).Cast<Relation>())
            {
                var command = Assert.IsType<Relation>(evidence.Left);
                var pair = E(execution.Commands).Cast<Relation>().Zip(execution.Units).Single(pair => ReferenceEquals(pair.First, command));
                Assert.Contains(routeEdges, actual => ReferenceEquals(actual, command.Left));
                var payload = Assert.IsType<Relation>(evidence.Right); Assert.Same(pair.Second.Current.Surface, payload.Left);
                var map = Assert.IsType<Relation>(payload.Right);
                Assert.Contains(E(execution.Join!.StrokeMap), actual => ReferenceEquals(actual, map));
                Assert.Contains(E(pair.Second.Current.Graph.Strokes), actual => ReferenceEquals(actual, map.Left));
                Assert.True(pair.Second.Current.Execution.IsAtBoundary); projected.Add(map.Right);
            }
            Assert.Equal(routeEdges.Count, E(projection).Length); Assert.True(projected.SetEquals(E(reading.Selected)));
        }
    }

    [Fact]
    public void HistoryRecoversSharedProgramsWhileCellDiscoveryUsesExposedTopology()
    {
        var history = Reading("D01"); var nodes = history.Patch.Decorations;
        var program = nodes[0].Current.Program; Assert.Equal(nodes.Length, E(history.Occurrences).Length);
        foreach (var occurrence in E(history.Occurrences).Cast<Relation>())
        {
            var node = nodes.Single(node => ReferenceEquals(node.Current.Surface, occurrence.Left));
            var evidence = Assert.IsType<Relation>(occurrence.Right);
            Assert.Same(program, node.Current.Program); Assert.Same(program.Surface, evidence.Left);
            Assert.True(node.Current.Execution.IsAtBoundary); Assert.Single(E(node.Current.Execution.CompletionEvents));
        }
        var cells = Reading("D02"); Assert.Null(cells.Strands); Assert.NotSame(history.Candidate, cells.Candidate);
        var discovery = Assert.IsType<OrnamentCellDiscovery>(cells.Cells);
        Assert.Same(cells.Patch.Levels[cells.Level].Read.Topology, discovery.SourceTopology);
        Assert.True(discovery.TryReadFamily(cells.Candidate, out var family));
        Assert.Equal(24, E(family.Occurrences).Length);
    }

    [Fact]
    // Preserve the reference-identity HashSet lookup: generic structural
    // equality can recursively traverse millions of retained participants.
#pragma warning disable xUnit2017
    public void NativeCaptureReachesTheSelectedQuestionResultsWithoutNumericCarriers()
    {
        var seen = Set([]); var todo = new Stack<IElement>(); todo.Push(Shared.Value.Surface);
        while (todo.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; Assert.False(item is Carrier or CarrierLocation);
            if (item is Relation relation) { todo.Push(relation.Left); todo.Push(relation.Right); }
        }
        foreach (var reading in Shared.Value.Readings)
        {
            Assert.True(seen.Contains(reading.Surface)); Assert.True(seen.Contains(reading.Candidate));
            Assert.True(seen.Contains(reading.Selected)); Assert.True(seen.Contains(reading.Complement));
            Assert.True(seen.Contains(reading.Occurrences)); Assert.True(seen.Contains(reading.Correspondence));
            if (reading.Cells is not null) Assert.True(seen.Contains(reading.Cells.Families));
            if (reading.Rhythm is not null) Assert.True(seen.Contains(reading.Rhythm.Assignments));
            if (reading.Strands is not null) Assert.True(seen.Contains(reading.Strands.Routes));
        }
    }
#pragma warning restore xUnit2017

    private static OrnamentDiscoveryStudy.Reading Reading(string id) => Shared.Value.Readings.Single(reading => reading.Id == id);
    private static HashSet<IElement> Set(IEnumerable<IElement> items) => new(items, ReferenceEqualityComparer.Instance);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
