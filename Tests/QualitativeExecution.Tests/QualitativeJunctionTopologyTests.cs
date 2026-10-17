using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeJunctionTopologyTests
{
    [Fact]
    public void ActualKeySeamsExposeBothDirectionsAndCapturedAnchors()
    {
        var fixture = Fixture(); var topology = new QualitativeJunctionTopology(fixture.Field);
        Assert.True(topology.IsValid); Assert.Empty(E(topology.Unresolved));
        Assert.Equal(4, E(topology.Cells).Length); Assert.Equal(4, E(topology.Anchors).Length);
        Assert.Equal(8, E(topology.Edges).Length);
        foreach (var edge in E(topology.Edges).Cast<Relation>())
        {
            Assert.True(topology.TryReadEdge(edge, out var from, out var to, out var seam));
            Assert.NotSame(from, to);
            Assert.Contains(E(fixture.Field.AcceptedSeams), actual => ReferenceEquals(actual, seam));
            Assert.Contains(E(fixture.Field.Join.Accepted), actual => ReferenceEquals(actual, seam));
            Assert.Contains(E(topology.Edges).Cast<Relation>(), reverse =>
                reverse.Left is Relation ends && ReferenceEquals(ends.Left, to) && ReferenceEquals(ends.Right, from) &&
                reverse.Right is Relation proof && ReferenceEquals(proof.Left, seam));
        }
        foreach (var anchor in E(topology.Anchors).Cast<Relation>())
            Assert.Contains(E(topology.Cells), actual => ReferenceEquals(actual, anchor.Left));
        Assert.DoesNotContain(Traverse(topology.Surface), element => element is Carrier or CarrierLocation);
    }

    [Fact]
    public void LocalDiamondClosuresDoNotBecomeCellFeedbackEdges()
    {
        var fixture = Fixture(1); var topology = new QualitativeJunctionTopology(fixture.Field);
        Assert.True(topology.IsValid); Assert.Equal(8, E(fixture.Field.AcceptedSeams).Length);
        Assert.Equal(8, E(topology.Edges).Length); // four neighbour joins, in both directions
        Assert.All(E(topology.Edges).Cast<Relation>(), edge =>
        {
            Assert.True(topology.TryReadEdge(edge, out var from, out var to, out _)); Assert.NotSame(from, to);
        });
    }

    [Fact]
    public void CuttingAllDeclaredSeamsLeavesEqualPositionsWithoutAnEdge()
    {
        var fixture = Fixture(1); var cut = fixture.Study.Capture(fixture.Example, S());
        var topology = new QualitativeJunctionTopology(cut);
        Assert.True(topology.IsValid); Assert.Equal(4, E(topology.Anchors).Length);
        Assert.Empty(E(topology.Edges)); Assert.Empty(E(topology.Unresolved));
        Assert.Same(fixture.Field.SourceGraph.Strokes, cut.SourceGraph.Strokes);
        var ordinary = new QualitativeJunctionTopology(fixture.Field);
        Assert.False(topology.TryReadEdge(Assert.IsType<Relation>(E(ordinary.Edges)[0]), out _, out _, out _));
    }

    [Fact]
    public void CuttingOneSeamRemovesExactlyItsTwoDirectedPassages()
    {
        var fixture = Fixture();
        var selected = S(E(fixture.Example.ConnectionDefinitions).Skip(1).ToArray());
        var cut = fixture.Study.Capture(fixture.Example, selected);
        var topology = new QualitativeJunctionTopology(cut);
        Assert.True(topology.IsValid); Assert.Equal(6, E(topology.Edges).Length);
        Assert.Equal(3, E(cut.AcceptedSeams).Length);
    }

    [Fact]
    public void SwappingAnyPublishedSourcePartCannotBorrowTheOriginalCapture()
    {
        var fixture = Fixture(); var otherExample = fixture.Study.Cases[1]; otherExample.Machine.Run();
        var other = fixture.Study.Capture(otherExample); var original = fixture.Field;
        var altered = new[] {
            original with { SourceGraph = other.SourceGraph },
            original with { SourceBindings = other.SourceBindings },
            original with { Join = other.Join },
            original with { SeamRead = other.SeamRead },
            original with { PortPublications = other.PortPublications },
            original with { Definition = other.Definition },
            original with { Surface = other.Surface }
        };
        foreach (var field in altered)
        {
            var topology = new QualitativeJunctionTopology(field);
            Assert.False(topology.IsValid); Assert.Empty(E(topology.Edges)); Assert.NotEmpty(E(topology.Unresolved));
        }
    }

    [Fact]
    public void NativeGraphAndCoincidentSitesCannotReplaceCompletedPortEvidence()
    {
        var fixture = Fixture(); var field = fixture.Field;
        var bareGraph = new QualitativeStrokeGraph(field.SourceGraph.Frame, field.SourceGraph.Sites, field.SourceGraph.Strokes);
        var naked = field with { SourceGraph = bareGraph };
        var topology = new QualitativeJunctionTopology(naked);
        Assert.False(topology.IsValid); Assert.Empty(E(topology.Edges));
        var copiedBindings = S(E(field.SourceBindings).Cast<Relation>().Select(binding =>
            (IElement)new Relation(binding.Left, binding.Right)).ToArray());
        topology = new QualitativeJunctionTopology(field with { SourceBindings = copiedBindings });
        Assert.False(topology.IsValid); Assert.Empty(E(topology.Edges));
    }

    [Fact]
    public void NakedAndCopiedEdgeRowsCannotAcquireAPassageContract()
    {
        var fixture = Fixture(); var topology = new QualitativeJunctionTopology(fixture.Field);
        var actual = Assert.IsType<Relation>(E(topology.Edges)[0]);
        Assert.True(topology.TryReadEdge(actual, out var from, out var to, out var seam));
        Assert.False(topology.TryReadEdge(new Relation(actual.Left, actual.Right), out _, out _, out _));
        Assert.False(topology.TryReadEdge(new Relation(new Relation(from, to), seam), out _, out _, out _));
        Assert.False(topology.TryReadEdge(new Relation(from, to), out _, out _, out _));
    }

    [Fact]
    public void CaptureRemainsValidAfterTheProducerReceivesMoreDrawingActions()
    {
        var fixture = Fixture(); var field = fixture.Field; var drawing = fixture.Example.Machine.Drawing;
        var oldStrokes = field.SourceGraph.Strokes;
        var before = new QualitativeJunctionTopology(field); Assert.True(before.IsValid);
        drawing.Apply(new Placeholder(), S(drawing.Draw(fixture.Study.Word(drawing.Frame.Right))), new Placeholder());
        Assert.NotSame(oldStrokes, drawing.Strokes); Assert.Same(oldStrokes, field.SourceGraph.Strokes);
        var historical = new QualitativeJunctionTopology(field);
        Assert.True(historical.IsValid); Assert.Equal(8, E(historical.Edges).Length); Assert.Empty(E(historical.Unresolved));
        Assert.All(E(historical.Edges).Cast<Relation>(), edge => Assert.True(historical.TryReadEdge(edge, out _, out _, out _)));
    }

    [Fact]
    public void IncompleteOneCellCaptureDoesNotPublishAUsableAnchorTopology()
    {
        var study = new ConnectedWallpaperStudy(1, 1); var example = study.Cases[0]; example.Machine.Run(1);
        var field = study.Capture(example); var topology = new QualitativeJunctionTopology(field);
        Assert.False(topology.IsValid); Assert.Empty(E(topology.Edges)); Assert.Empty(E(topology.Anchors));
        Assert.NotEmpty(E(topology.Unresolved));
    }

    private sealed record FixtureWorld(ConnectedWallpaperStudy Study, ConnectedWallpaperStudy.Case Example, ConnectedWallpaperStudy.Field Field);
    private static FixtureWorld Fixture(int family = 0)
    {
        var study = new ConnectedWallpaperStudy(2, 2); var example = study.Cases[family]; example.Machine.Run();
        return new(study, example, study.Capture(example));
    }
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; yield return item;
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
