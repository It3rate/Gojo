using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentPerspectiveStudyTests
{
    private static readonly Lazy<OrnamentPerspectiveStudy> Shared = new(() => new OrnamentPerspectiveStudy());

    [Fact]
    public void DifferentLozengeQuestionsShareExactSourceTrianglesAndKeepTheirExclusiveParts()
    {
        var patch = Patch("V01-T"); var a = View(patch, "T-A"); var b = View(patch, "T-B");
        var comparison = Comparison(patch, a, b); Assert.True(comparison.IsComplete);
        var first = patch.Source.FaceRecords.Single(face => face.CellX == 2 && face.CellY == 2 && face.Sector == 0).Face;
        var aGroup = Groups(a).Single(group => E(group.Faces).Any(face => ReferenceEquals(face, first)));
        var bGroup = Groups(b).Single(group => E(group.Faces).Any(face => ReferenceEquals(face, first)));
        var match = Matches(comparison).Single(pair => ReferenceEquals(pair.Left.Read, aGroup.Read) && ReferenceEquals(pair.Right.Read, bGroup.Read));
        Assert.Same(first, Assert.Single(E(match.SharedFaces)));
        Assert.Single(E(match.LeftOnlyFaces)); Assert.Single(E(match.RightOnlyFaces));
        Assert.Equal(3, E(match.SharedStrokes).Length); Assert.Equal(3, E(match.SharedSites).Length);
        Assert.NotSame(aGroup.Identity, bGroup.Identity); Assert.Same(patch.Source.Scaffold, a.Source); Assert.Same(a.Source, b.Source);
        Assert.Equal(30, E(a.Groups).Length); Assert.Equal(24, E(b.Groups).Length); Assert.Equal(12, E(b.Complement).Length);
        foreach (var pair in Matches(comparison))
        {
            var expected = E(pair.Left.Faces).Where(face => E(pair.Right.Faces).Any(other => ReferenceEquals(face, other))).ToArray();
            Assert.Equal(expected.Length, E(pair.SharedFaces).Length);
            foreach (var face in expected) Assert.Contains(E(pair.SharedFaces), actual => ReferenceEquals(actual, face));
            foreach (var face in E(pair.LeftOnlyFaces)) Assert.DoesNotContain(E(pair.Right.Faces), actual => ReferenceEquals(actual, face));
        }
    }

    [Fact]
    public void VertexDiamondReadOverlapsWithItselfWithoutCreatingMaterialOwnershipOrTransitiveGroups()
    {
        var patch = Patch("V01-F"); var view = View(patch, "F-B"); var groups = Groups(view);
        Assert.True(view.IsComplete); Assert.Equal(15, groups.Length); Assert.All(groups, group => Assert.Equal(8, E(group.Faces).Length));
        Assert.Equal(120, groups.Sum(group => E(group.Faces).Length)); Assert.Equal(76, E(view.SelectedFaces).Length);
        Assert.Equal(20, E(view.Complement).Length); Assert.Equal(22, E(view.Overlaps).Length);
        var self = Comparison(patch, view, view);
        var proper = Matches(self).Where(pair => !ReferenceEquals(pair.Left.Read, pair.Right.Read) && E(pair.SharedFaces).Length != 0).ToArray();
        Assert.Equal(44, proper.Length); Assert.All(proper, pair => Assert.Equal(2, E(pair.SharedFaces).Length));
        Assert.Equal(15, E(view.Groups).Length); Assert.Equal(96, E(patch.Source.Scaffold.Faces).Length);
        Assert.Equal(154, E(patch.Source.Scaffold.Graph.Strokes).Length);
        Assert.All(E(view.SelectedFaces), face => Assert.Contains(E(patch.Source.Scaffold.Faces), actual => ReferenceEquals(actual, face)));
    }

    [Fact]
    public void CourtsSupportCrossingParentOrganizationsAndAThirdLevelThroughActualChildReads()
    {
        var patch = Patch("V01-F"); Assert.Equal(3, patch.Compositions.Length);
        Assert.Equal(24, E(View(patch, "F-A").Groups).Length); Assert.Equal(6, E(View(patch, "F-C").Groups).Length);
        Assert.Equal(4, E(View(patch, "F-D").Groups).Length); Assert.Single(E(View(patch, "F-E").Groups));
        foreach (var composition in patch.Compositions)
        {
            Assert.True(composition.IsComplete); Assert.Same(patch.Source.Scaffold, composition.View.Source);
            foreach (var definition in E(composition.Definitions).Cast<Relation>())
                foreach (var child in E((Relation)definition.Right))
                    Assert.Contains(E(composition.Lower.Groups), actual => ReferenceEquals(actual, child));
            foreach (var row in E(composition.Correspondence).Cast<Relation>())
            {
                var definitions = (Relation)row.Left; var childMaps = (Relation)((Relation)row.Right).Left;
                Assert.Contains(E(composition.Definitions), actual => ReferenceEquals(actual, definitions.Left));
                Assert.Contains(E(composition.View.Definitions), actual => ReferenceEquals(actual, definitions.Right));
                foreach (var map in E(childMaps).Cast<Relation>())
                {
                    Assert.True(composition.Lower.TryReadGroup((Relation)map.Left, out var child)); Assert.Same(child.Faces, map.Right);
                }
            }
        }
        Assert.Same(patch.Compositions[0].View, patch.Compositions[2].Lower);
        Assert.Equal(96, E(patch.Compositions[2].View.SelectedFaces).Length);
        var blockRows = Comparison(patch, View(patch, "F-C"), View(patch, "F-D"));
        var shared = Matches(blockRows).Where(pair => E(pair.SharedFaces).Length != 0).ToArray();
        Assert.Equal(12, shared.Length); Assert.All(shared, pair => Assert.Equal(8, E(pair.SharedFaces).Length));
    }

    [Fact]
    public void ThreeDerivedLevelsRetainActualCornerProofsAndFiniteBoundaryComplement()
    {
        foreach (var patch in Shared.Value.Patches)
        {
            Assert.Equal(3, patch.Levels.Length);
            foreach (var level in patch.Levels)
            {
                var read = level.Read; Assert.True(read.IsComplete); Assert.NotEmpty(E(read.Boundary));
                var sourceSites = E((Relation)read.SourceTopology.Left);
                var sourceFaces = E((Relation)((Relation)read.SourceTopology.Right).Right).Cast<Relation>().ToArray();
                foreach (var cell in E(read.Faces).Cast<Relation>())
                {
                    Assert.True(read.TryReadCell(cell, out var sourceVertex, out var boundary, out var corners));
                    Assert.Contains(sourceSites, actual => ReferenceEquals(actual, sourceVertex));
                    Assert.Equal(E(corners).Length, E(boundary).Length); Assert.True(E(boundary).Length >= 3);
                    foreach (var corner in E(corners).Cast<Relation>())
                    {
                        var sourceFace = sourceFaces.Single(face => ReferenceEquals(face.Left, corner.Left));
                        var parts = E((Relation)sourceFace.Right); var pair = (Relation)corner.Right;
                        Assert.Contains(parts, actual => ReferenceEquals(actual, pair.Left)); Assert.Contains(parts, actual => ReferenceEquals(actual, pair.Right));
                    }
                    foreach (var part in E(boundary).Cast<Relation>())
                        Assert.Contains(E(read.Edges), actual => ReferenceEquals(actual, part.Left));
                }
            }
        }
        // G07 correspondence is topological four/eight boundary arity and
        // interior 4-8-8 incidence. It does not assert regular metric octagons.
        var fan = Patch("V01-F").Levels[0].Read; var cells = E(fan.Faces).Cast<Relation>().ToArray();
        Assert.Equal(24, cells.Count(cell => E((Relation)cell.Right).Length == 4));
        Assert.Equal(15, cells.Count(cell => E((Relation)cell.Right).Length == 8));
        var interiors = E(fan.Sites).Select(site => cells.Where(cell => E((Relation)cell.Right).Cast<Relation>().Any(part =>
            part.Right is Relation ends && (ReferenceEquals(ends.Left, site) || ReferenceEquals(ends.Right, site)))).ToArray()).Where(incident => incident.Length == 3).ToArray();
        Assert.NotEmpty(interiors);
        foreach (var incident in interiors) Assert.Equal(new[] { 4, 8, 8 }, incident.Select(cell => E((Relation)cell.Right).Length).Order().ToArray());
    }

    [Fact]
    public void DifferentViewNetworksCarryContactAndOverlapWitnessesIntoDifferentConstructionTopologies()
    {
        foreach (var patch in Shared.Value.Patches)
        foreach (var network in patch.Networks)
        {
            Assert.Same(network.View.View, network.Read.Source); Assert.True(network.Read.IsComplete);
            Assert.Equal(E(network.View.View.Groups).Length, E(network.Read.Sites).Length);
            var topology = (Relation)network.Read.Topology.Right; Assert.Empty(E((Relation)topology.Right));
            Assert.Same(network.Read.Topology, network.Execution.SourceTopology);
            foreach (var row in E((Relation)topology.Left).Cast<Relation>())
            {
                var contact = (Relation)row.Left; Assert.Same(contact.Right, row.Right);
                Assert.True(network.Read.TryReadContact(contact, out var witness, out _, out _));
                Assert.Contains(E(network.View.View.Adjacency), actual => ReferenceEquals(actual, witness));
            }
            foreach (var overlap in E(network.Read.Overlaps).Cast<Relation>())
            {
                Assert.True(network.Read.TryReadOverlap(overlap, out var witness, out _, out _));
                Assert.Contains(E(network.View.View.Overlaps), actual => ReferenceEquals(actual, witness));
                Assert.DoesNotContain(E(network.Execution.Commands).Cast<Relation>(), command => ReferenceEquals(command.Left, overlap));
            }
        }
        var fans = Patch("V01-F").Networks;
        Assert.Equal(38, E(fans[0].Read.Contacts).Length); Assert.Empty(E(fans[0].Read.Overlaps));
        Assert.Equal(16, E(fans[1].Read.Contacts).Length); Assert.Equal(22, E(fans[1].Read.Overlaps).Length);
    }

    [Fact]
    public void NewDrawingConsumesPublishedLandmarksAndAccountsForEveryImportedPacketWithoutNumericCarriers()
    {
        foreach (var patch in Shared.Value.Patches)
        {
            foreach (var (binding, decoration) in E(patch.Levels[1].Bindings).Cast<Relation>().Zip(patch.Decorations))
                Assert.Same(binding.Right, decoration.PlacementWord);
            foreach (var network in patch.Networks)
            foreach (var row in E(network.LandmarkReads).Cast<Relation>())
            {
                var representative = (Relation)row.Left; var calculation = (Relation)row.Right;
                Assert.Contains(E(network.Read.Representatives), actual => ReferenceEquals(actual, representative));
                Assert.True(OrnamentWordLandmarks.TryReadComplete(patch.Source.Frame, calculation, out var word));
                var binding = E(network.Bindings).Cast<Relation>().Single(actual => ReferenceEquals(actual.Left, representative.Right));
                Assert.Same(word, binding.Right);
                var inputs = (Relation)((Relation)calculation.Left).Left;
                foreach (var input in E((Relation)inputs.Right)) Assert.Contains(E(patch.Source.Bindings), actual => ReferenceEquals(actual, input));
            }
            foreach (var execution in patch.Executions.Concat(patch.Networks.Select(network => network.Execution)))
            {
                Assert.True(execution.IsComplete); Assert.Empty(E(execution.Unresolved));
                Assert.Equal(E(execution.Commands).Length, execution.Units.Length);
                Assert.Equal(E(execution.IsolatedCommands).Length, execution.IsolatedUnits.Length);
                var imported = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var released = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
                foreach (var unit in execution.Units)
                    foreach (var leaf in new[] { unit.PlacementExecution, unit.Current.Execution }) AssertAccounted(leaf, imported, released);
                foreach (var leaf in execution.IsolatedUnits) AssertAccounted(leaf, imported, released);
                Assert.Equal(E(execution.Commands).Length * 5 + E(execution.IsolatedCommands).Length * 2, imported.Count);
                Assert.True(imported.SetEquals(released));
            }
            var sourceAndMarkersIn = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var sourceAndMarkersOut = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
            foreach (var unit in patch.Source.Units.Concat(patch.Decorations))
                foreach (var leaf in new[] { unit.PlacementExecution, unit.Current.Execution }) AssertAccounted(leaf, sourceAndMarkersIn, sourceAndMarkersOut);
            Assert.True(sourceAndMarkersIn.SetEquals(sourceAndMarkersOut));
        }
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var todo = new Stack<IElement>(); todo.Push(Shared.Value.Surface);
        while (todo.TryPop(out var element))
        {
            if (!seen.Add(element)) continue; Assert.False(element is Carrier or CarrierLocation);
            if (element is Relation relation) { todo.Push(relation.Left); todo.Push(relation.Right); }
        }
        foreach (var patch in Shared.Value.Patches)
        {
            foreach (var view in patch.Views) Assert.True(seen.Contains(view.View.Evaluate()));
            foreach (var composition in patch.Compositions) Assert.True(seen.Contains(composition.Evaluate()));
            foreach (var comparison in patch.Comparisons) Assert.True(seen.Contains(comparison.Evaluate()));
        }
    }

    private static OrnamentPerspectiveStudy.Patch Patch(string id) => Shared.Value.Patches.Single(patch => patch.Id == id);
    private static void AssertAccounted(PrimitiveFlowLeaf leaf, HashSet<IElement> imported, HashSet<IElement> released)
    {
        Assert.True(leaf.IsAtBoundary); Assert.Single(E(leaf.CompletionEvents)); Assert.Empty(E(leaf.Pending)); Assert.Empty(E(leaf.Unresolved));
        var account = leaf.Account(); var incoming = E((Relation)((Relation)account.Left).Right);
        var active = (Relation)account.Right; var retained = (Relation)active.Right; var exits = (Relation)retained.Right;
        Assert.Empty(E((Relation)active.Left)); Assert.Empty(E((Relation)retained.Left)); Assert.Empty(E((Relation)exits.Right));
        Assert.Equal(E(leaf.Sequence.Actions).Length, incoming.Length);
        foreach (var packet in incoming) Assert.True(imported.Add(packet));
        foreach (var receipt in E((Relation)exits.Left).Cast<Relation>()) Assert.True(released.Add(receipt.Left));
    }
    private static OrnamentView View(OrnamentPerspectiveStudy.Patch patch, string id) => patch.Views.Single(view => view.Id == id).View;
    private static OrnamentViewOverlap Comparison(OrnamentPerspectiveStudy.Patch patch, OrnamentView left, OrnamentView right) =>
        patch.Comparisons.Single(comparison => ReferenceEquals(comparison.Left, left) && ReferenceEquals(comparison.Right, right));
    private static OrnamentViewGroup[] Groups(OrnamentView view) => E(view.Groups).Cast<Relation>().Select(group =>
    { Assert.True(view.TryReadGroup(group, out var parts)); return parts; }).ToArray();
    private static OrnamentViewCorrespondence[] Matches(OrnamentViewOverlap comparison) => E(comparison.Correspondences).Cast<Relation>().Select(row =>
    { Assert.True(comparison.TryReadCorrespondence(row, out var parts)); return parts; }).ToArray();
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
}
