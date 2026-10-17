using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class ConnectedWallpaperStudyTests
{
    [Fact]
    public void KeyMeshPublishesActualCompletedCyclesAndJoinsBothPeriods()
    {
        var study = new ConnectedWallpaperStudy(2, 2); var example = study.Cases[0];
        example.Machine.Run(); var field = study.Capture(example);
        Assert.True(example.Machine.IsComplete);
        Assert.Equal(4, E(example.Machine.CompletedMotifs).Length);
        Assert.Equal(36, E(field.SourceGraph.Strokes).Length);
        Assert.Equal(16, E(field.PortPublications).Length);
        Assert.Equal(4, E(field.AcceptedSeams).Length);
        Assert.Empty(E(field.BlockedSeams)); Assert.Empty(E(field.Unresolved));
        Assert.NotEmpty(E(field.Frontier)); Assert.True(field.Join.TryReadPassed(field.Join.Evaluate()));
        Assert.Equal(E(field.SourceGraph.Sites).Length - 4, E(field.Graph.Sites).Length);
        foreach (var publication in E(field.PortPublications).Cast<Relation>())
        {
            var owner = Assert.IsType<Relation>(publication.Left);
            var proof = Assert.IsType<Relation>(publication.Right);
            Assert.Contains(E(example.Machine.CompletedMotifs), actual => ReferenceEquals(actual, proof.Left));
            var effectAndBinding = Assert.IsType<Relation>(proof.Right);
            var receipt = Assert.IsType<Relation>(effectAndBinding.Left);
            Assert.True(example.Machine.Drawing.TryReadAccepted(receipt, out _, out _));
            Assert.Contains(E(field.SourceBindings), actual => ReferenceEquals(actual, effectAndBinding.Right));
            Assert.Contains(E(example.PortDefinitions), actual => ReferenceEquals(actual, owner.Right));
        }
    }

    [Fact]
    public void DiamondClosuresAndSeamsHaveExplicitAliasesWithoutRewritingHistory()
    {
        var study = new ConnectedWallpaperStudy(2, 2); var example = study.Cases[1];
        example.Machine.Run(); var originalStrokes = example.Machine.Drawing.Strokes;
        var field = study.Capture(example);
        Assert.Equal(20, E(field.SourceGraph.Sites).Length);
        Assert.Equal(12, E(field.Graph.Sites).Length);
        Assert.Equal(8, E(field.AcceptedSeams).Length);
        Assert.Same(originalStrokes, example.Machine.Drawing.Strokes);
        Assert.Equal(16, E(field.SourceGraph.Strokes).Length);
        Assert.Equal(16, E(field.Graph.Strokes).Length);
        Assert.Empty(E(field.BlockedSeams)); Assert.Empty(E(field.Unresolved));
        var first = Assert.IsType<Relation>(E(field.SourceGraph.Strokes)[0]);
        var last = Assert.IsType<Relation>(E(field.SourceGraph.Strokes)[3]);
        Assert.True(QualitativeStrokeGraph.TryReadStroke(first, out _, out var start, out _, out _));
        Assert.True(QualitativeStrokeGraph.TryReadStroke(last, out _, out _, out var returned, out _));
        Assert.NotSame(start, returned);
        Assert.Same(Target(field.Join.SiteMap, start), Target(field.Join.SiteMap, returned));
        Assert.True(field.Join.TryReadPassed(field.Join.Evaluate()));
    }

    [Fact]
    public void EqualPositionsAloneCannotWeldEvenTheDiamondsReturn()
    {
        var study = new ConnectedWallpaperStudy(2, 2); var example = study.Cases[1]; example.Machine.Run();
        var field = study.Capture(example, S());
        Assert.Empty(E(field.AcceptedSeams)); Assert.Empty(E(field.Join.Accepted));
        Assert.Equal(20, E(field.SourceGraph.Sites).Length);
        Assert.Equal(20, E(field.Graph.Sites).Length);
        Assert.All(E(field.Join.SiteMap).Cast<Relation>(), row => Assert.Same(row.Left, row.Right));
    }

    [Fact]
    public void WrongPeriodBlocksAPositionMatchingPortPair()
    {
        var study = new ConnectedWallpaperStudy(2, 2); var example = study.Cases[0]; example.Machine.Run();
        var connection = Assert.IsType<Relation>(E(example.ConnectionDefinitions)[0]);
        var context = Assert.IsType<Relation>(connection.Left);
        var wrongPeriod = S([.. E(Assert.IsType<Relation>(context.Right)),
            .. E(study.Word(study.Basis.Basis.Frame.Right))]);
        var wrong = new Relation(new Relation(context.Left, wrongPeriod), connection.Right);
        var field = study.Capture(example, S(wrong));
        Assert.Empty(E(field.AcceptedSeams)); Assert.Single(E(field.BlockedSeams));
        Assert.Empty(E(field.Join.Accepted)); Assert.Empty(E(field.Unresolved));
    }

    [Fact]
    public void IncompleteMotifCannotPublishItsAlreadyDrawnPort()
    {
        var study = new ConnectedWallpaperStudy(2, 1); var example = study.Cases[0];
        example.Machine.Run(1); var field = study.Capture(example);
        Assert.Single(E(field.SourceGraph.Strokes)); Assert.Empty(E(example.Machine.CompletedMotifs));
        Assert.Empty(E(field.PortPublications)); Assert.Empty(E(field.AcceptedSeams));
        Assert.NotEmpty(E(field.Unresolved)); Assert.NotEmpty(E(field.Frontier));
        Assert.Empty(E(field.BlockedSeams));
    }

    [Fact]
    public void AForeignPortOrActionCannotSupplyTheSeam()
    {
        var study = new ConnectedWallpaperStudy(2, 1); var example = study.Cases[0]; example.Machine.Run();
        var connection = Assert.IsType<Relation>(E(example.ConnectionDefinitions)[0]);
        var actualPairs = Assert.IsType<Relation>(connection.Right);
        var actualPair = Assert.IsType<Relation>(Assert.Single(E(actualPairs)));
        var foreign = new Relation(connection.Left, S(new Relation(new Placeholder(), actualPair.Right)));
        var field = study.Capture(example, S(foreign));
        Assert.Empty(E(field.AcceptedSeams)); Assert.Single(E(field.BlockedSeams));
        var originalPort = Assert.IsType<Relation>(E(example.PortDefinitions)[0]);
        var foreignAction = study.Cases[1].Machine.Drawing.Draw(study.Word(study.Basis.Basis.Frame.Right));
        var badPort = new Relation(originalPort.Left, new Relation(example.Machine.Drawing.CursorPort, foreignAction));
        var altered = example with { PortDefinitions = S([badPort, .. E(example.PortDefinitions).Skip(1)]) };
        var rejected = study.Capture(altered);
        Assert.DoesNotContain(E(rejected.PortPublications).Cast<Relation>(), publication =>
            Assert.IsType<Relation>(publication.Left).Right is Relation port && ReferenceEquals(port, badPort));
        Assert.NotEmpty(E(rejected.BlockedSeams));
    }

    [Fact]
    public void ForeignMachineReceiptsCannotPublishTheOriginalMotifsPorts()
    {
        var study = new ConnectedWallpaperStudy(1, 1); var original = study.Cases[0]; var foreign = study.Cases[1];
        foreign.Machine.Run();
        var altered = original with { Machine = foreign.Machine };
        var field = study.Capture(altered);
        Assert.Empty(E(field.PortPublications)); Assert.Empty(E(field.AcceptedSeams));
        Assert.NotEmpty(E(field.Unresolved));
    }

    [Fact]
    public void CrossingAndExplicitJunctionCoverTheSameDiagonalsWithDifferentTopology()
    {
        var study = new ConnectedWallpaperStudy(1, 1); var crossing = study.Cases[2]; var joined = study.Cases[3];
        crossing.Machine.Run(); joined.Machine.Run();
        var a = study.Capture(crossing); var b = study.Capture(joined);
        Assert.Equal(2, E(a.Graph.Strokes).Length); Assert.Equal(4, E(b.Graph.Strokes).Length);
        Assert.Empty(E(a.AcceptedSeams)); Assert.Single(E(b.AcceptedSeams));
        Assert.Equal(0, a.Graph.Describe(a.Graph.Evaluate()).X);
        Assert.Equal(1, b.Graph.Describe(b.Graph.Evaluate()).X);
        Assert.Empty(E(a.Unresolved)); Assert.Empty(E(b.Unresolved));
        for (var diagonal = 0; diagonal < 2; diagonal++)
        {
            QualitativeStrokeGraph.TryReadStroke((Relation)E(a.SourceGraph.Strokes)[diagonal], out _, out _, out _, out var whole);
            QualitativeStrokeGraph.TryReadStroke((Relation)E(b.SourceGraph.Strokes)[diagonal * 2], out _, out _, out _, out var first);
            QualitativeStrokeGraph.TryReadStroke((Relation)E(b.SourceGraph.Strokes)[diagonal * 2 + 1], out _, out _, out _, out var second);
            var comparison = study.Basis.Basis.Frame.Reduce(whole, study.Basis.Basis.Inverse(S([.. E(first), .. E(second)])));
            Assert.True(study.Basis.Basis.Frame.TryReadResult(comparison, out var remaining, out _, out var pending));
            Assert.Empty(E(remaining)); Assert.Empty(E(pending));
        }
        Assert.All(E(b.Join.StrokeMap).Cast<Relation>(), row =>
        {
            QualitativeStrokeGraph.TryReadStroke((Relation)row.Left, out _, out _, out _, out var sourceWord);
            QualitativeStrokeGraph.TryReadStroke((Relation)row.Right, out var cause, out _, out _, out var targetWord);
            Assert.Same(row.Left, cause); Assert.Same(sourceWord, targetWord);
        });
    }

    [Fact]
    public void FiniteOneCellFrontierIsNotAMissingInteriorPromise()
    {
        var study = new ConnectedWallpaperStudy(1, 1); var example = study.Cases[0]; example.Machine.Run();
        var field = study.Capture(example);
        Assert.Empty(E(example.ConnectionDefinitions)); Assert.Empty(E(field.Unresolved));
        Assert.Equal(4, E(field.Frontier).Length); Assert.Empty(E(field.BlockedSeams));
        Assert.True(field.Join.TryReadPassed(field.Join.Evaluate()));
    }

    private static IElement Target(Relation map, IElement site) => Assert.Single(E(map).Cast<Relation>(), row => ReferenceEquals(row.Left, site)).Right;
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
