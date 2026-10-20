using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class ConstructionViewStudyTests
{
    private static readonly Lazy<ConstructionViewStudy> Study = new(() => new());

    [Fact]
    public void DifferentContextsConsumeOneActualSourceAndProduceDifferentAcknowledgedNetworks()
    {
        var study = Study.Value;
        Assert.Equal(48, E(study.Source.Scaffold.Faces).Length);
        Assert.Equal(new[] { 12, 6, 3 }, study.Variants.Select(variant => E(variant.Network.Sites).Length));
        Assert.Equal(new[] { 17, 4, 2 }, study.Variants.Select(variant => E(variant.Network.Contacts).Length));
        foreach (var variant in study.Variants)
        {
            Assert.Same(study.Source.Scaffold, variant.View.Source);
            Assert.Same(variant.View, variant.Network.Source);
            Assert.Same(variant.Network.Topology, variant.Execution.SourceTopology);
            Assert.Same(variant.Bindings, variant.Execution.SourceBindings);
            Assert.Same(variant.Network.Surface, variant.Execution.Cause);
            Assert.True(variant.Execution.IsComplete);
            Assert.Equal(E(variant.Network.Contacts).Length, E(variant.Execution.Commands).Length);
            Assert.Equal(E(variant.Network.Contacts).Length, E(variant.Execution.Join!.Graph.Strokes).Length);
            Assert.Equal(E(variant.Network.Sites).Length, E(variant.Execution.OutputBindings).Length);
            Assert.All(variant.Execution.Units, unit => Assert.True(unit.Current.Join.TryReadPassed(unit.Current.Join.Evaluate())));
            foreach (var landmark in E(variant.LandmarkReads).Cast<Relation>())
            {
                Assert.Contains(E(variant.Network.Representatives), actual => ReferenceEquals(actual, landmark.Left));
                Assert.True(OrnamentWordLandmarks.TryReadComplete(study.Source.Frame, (Relation)landmark.Right, out _));
            }
        }
    }

    [Fact]
    public void OverlappingDiamondObservationsRetainComplementWithoutDuplicatingPhysicalOwnership()
    {
        var study = Study.Value;
        var view = study.Variants[1].View;
        var groups = E(view.Groups).Cast<Relation>().Select(read =>
        {
            Assert.True(view.TryReadGroup(read, out var group));
            return group;
        }).ToArray();
        Assert.Equal(48, groups.Sum(group => E(group.Faces).Length));
        Assert.True(E(view.SelectedFaces).Length < groups.Sum(group => E(group.Faces).Length));
        Assert.NotEmpty(E(view.Complement));
        Assert.NotEmpty(E(view.Overlaps));
        Assert.NotEmpty(E(study.Variants[1].Network.Overlaps));
        foreach (var group in groups)
        foreach (var face in E(group.Faces))
            Assert.Contains(E(study.Source.Scaffold.Faces), actual => ReferenceEquals(actual, face));
        Assert.Equal(48, E(study.Source.Scaffold.Faces).Length);
        foreach (var overlap in E(study.Variants[1].Network.Overlaps))
            Assert.DoesNotContain(E(study.Variants[1].Execution.Commands).Cast<Relation>(), command => ReferenceEquals(command.Left, overlap));
        Assert.All(study.Comparisons, comparison => Assert.True(comparison.IsComplete));
        Assert.Contains(E(study.Comparisons[0].Correspondences).Cast<Relation>(), read =>
            study.Comparisons[0].TryReadCorrespondence(read, out var pair) && E(pair.SharedFaces).Length != 0);
    }

    [Fact]
    public void RowParentsConsumeActualChildReadsAndDoNotAcquireThemByLookalikeGeometry()
    {
        var study = Study.Value;
        var row = study.Variants[2];
        var composition = Assert.IsType<OrnamentViewComposition>(row.Composition);
        Assert.Same(study.Variants[0].View, composition.Lower);
        Assert.Same(composition.View, row.View);
        foreach (var definition in E(composition.Definitions).Cast<Relation>())
        foreach (var child in E((Relation)definition.Right))
            Assert.Contains(E(study.Variants[0].View.Groups), actual => ReferenceEquals(actual, child));
        var actual = (Relation)E(study.Variants[0].View.Groups)[0];
        var copied = new Relation(actual.Left, actual.Right);
        var rejected = new OrnamentViewComposition(study.Variants[0].View,
            S(new Relation(new Placeholder(), S(copied))), new Placeholder());
        Assert.False(rejected.IsComplete);
        Assert.NotEmpty(E(rejected.Blocked));
    }

    [Fact]
    public void ForeignFaceAndOpenMembershipDoNotYieldACompleteConstruction()
    {
        var study = Study.Value;
        var face = (Relation)E(study.Source.Scaffold.Faces)[0];
        var copy = new Relation(face.Left, face.Right);
        var foreign = new OrnamentView(study.Source.Scaffold,
            S(OrnamentView.Group(new Placeholder(), copy)), new Placeholder());
        Assert.False(foreign.IsComplete);
        Assert.NotEmpty(E(foreign.Blocked));
        var blockedNetwork = new OrnamentViewNetwork(foreign, new Placeholder());
        Assert.False(blockedNetwork.IsComplete);
        Assert.Empty(E(blockedNetwork.Sites));
        var open = new OrnamentView(study.Source.Scaffold,
            StructuralConstraint.Open(OrnamentView.Group(new Placeholder(), face)), new Placeholder());
        Assert.False(open.IsComplete);
        Assert.NotEmpty(E(open.Unresolved));
        Assert.False(new OrnamentViewNetwork(open, new Placeholder()).IsComplete);
    }

    [Fact]
    public void EveryConstructionRetainsOnlyNativePreNumericParticipants()
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(Study.Value.Surface);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue;
            Assert.True(item is Relation or Placeholder);
            if (item is Relation row) { pending.Push(row.Left); pending.Push(row.Right); }
        }
    }

    [Fact]
    public void OneSharedMotifConsumesEachActualSelectedLandmarkThroughAcknowledgedPlacement()
    {
        var study = Study.Value;
        Assert.Equal(8, E(study.MotifProgram.Paths).Cast<Relation>().Sum(path => E((Relation)path.Right).Length));
        Assert.Equal(new[] { 12, 6, 3 }, study.Variants.Select(variant => variant.Decorations.Length));
        foreach (var variant in study.Variants)
        {
            Assert.Equal(variant.Decorations.Length, E(variant.DecorationInputs).Length);
            foreach (var pair in E(variant.Bindings).Cast<Relation>().Zip(variant.Decorations))
            {
                var binding = pair.First;
                var node = pair.Second;
                Assert.Same(study.MotifProgram, node.Current.Program);
                Assert.Same(study.MotifProgram, Assert.Single(node.Programs));
                Assert.Same(binding.Right, node.PlacementWord);
                Assert.Single(E(node.PlacementExecution.CompletionEvents));
                Assert.True(node.Current.Join.TryReadPassed(node.Current.Join.Evaluate()));
                Assert.Equal(8, E(node.Current.Graph.Strokes).Length);
                var receipt = Assert.IsType<Relation>(Assert.Single(E(variant.DecorationInputs), item =>
                    item is Relation row && ReferenceEquals(row.Left, binding)));
                var route = Assert.IsType<Relation>(receipt.Right);
                Assert.Same(node.PlacementExecution.InputPort, route.Left);
                var accepted = Assert.IsType<Relation>(route.Right);
                Assert.Same(node.Current.Surface, accepted.Right);
                Assert.IsType<Relation>(accepted.Left);
            }
        }
    }

    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
