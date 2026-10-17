using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeSquareStudyTests
{
    [Fact]
    public void SquareDefinitionConnectsSharedReachesContourAndSeamsWithoutNumericValues()
    {
        var study = new QualitativeSquareStudy();
        Assert.DoesNotContain(Traverse(study.Surface), item => item is Carrier or CarrierLocation);
        Assert.Equal(25, E(study.SiteBindings).Length); Assert.Same(study.Frame.Origin, study.Contour.Center);
        Assert.True(study.Cell.TryReadPassed(study.CellRead));
        Assert.True(study.Contour.TryReadEqualTurns(study.Contour.Evaluate(), out _));
        Assert.True(study.Contour.TryReadActionResult(study.ContourAgreement, out var matched, out var failed, out var unknown));
        Assert.Equal(4, E(matched).Length); Assert.Empty(E(failed)); Assert.Empty(E(unknown));
        Assert.Equal(new bool?[] { true, true, true, false, true, true, false, true, true }, study.Laws.Select(law => law.Passed));
    }

    [Fact]
    public void DirectionAndSitePermutationsMustAgreeOnEveryLandmark()
    {
        var study = new QualitativeSquareStudy();
        foreach (var read in E(study.SiteTransport).Cast<Relation>())
        {
            var result = Assert.IsType<Relation>(read.Right); Assert.Equal(25, E(Assert.IsType<Relation>(result.Left)).Length);
            Assert.Empty(E(Assert.IsType<Relation>(result.Right)));
        }
        var wrong = study.RelateActions(study.DirectionTurn, study.SiteMirror);
        Assert.NotEmpty(E(Assert.IsType<Relation>(Assert.IsType<Relation>(wrong.Right).Right)));
    }

    [Fact]
    public void EqualTurnPromiseIsSeparateFromClosedTraversalAndSuccessorCorrespondence()
    {
        var study = new QualitativeSquareStudy(); var raw = study.UnequalOrUnknownTurns; var read = raw.Evaluate();
        Assert.True(raw.TryReadClosedContour(read, out var whole)); Assert.Same(raw.WholeTraversal, whole);
        Assert.False(raw.TryReadEqualTurns(read, out _));
        var map = raw.CompareAction(study.DirectionActions.Domain, study.DirectionTurn, study.ContourCorrespondence);
        Assert.True(raw.TryReadActionResult(map, out var matched, out var failed, out var unknown));
        Assert.Equal(4, E(matched).Length); Assert.Empty(E(failed)); Assert.Empty(E(unknown));
    }

    [Fact]
    public void PeriodActionsRotateAndReflectThroughActualSharedReachWords()
    {
        var study = new QualitativeSquareStudy(); Assert.Equal(4, E(study.ActionTransport).Length);
        foreach (var witness in E(study.ActionTransport).Cast<Relation>())
        {
            var check = Assert.IsType<Relation>(Assert.IsType<Relation>(witness.Right).Right);
            Assert.True(study.Frame.TryReadResult(check, out var rest, out _, out var unknown)); Assert.Empty(E(rest)); Assert.Empty(E(unknown));
        }
    }

    [Fact]
    public void GeneratedOrbitsRetainEveryActualActionAndSourceStroke()
    {
        var study = new QualitativeSquareStudy(); var r = study.DirectionTurn; var m = study.DirectionMirror;
        var cyclic = study.Generate("cyclic", S(), S(r), S(r, r), S(r, r, r));
        var mirrored = study.Generate("mirrored", S(), S(r), S(r, r), S(r, r, r), S(m), S(r, m), S(r, r, m), S(r, r, r, m));
        Assert.Equal(8, E(cyclic.Graph.Strokes).Length); Assert.Equal(16, E(mirrored.Graph.Strokes).Length);
        Assert.Equal(4, E(cyclic.Actions).Length); Assert.Equal(8, E(mirrored.Actions).Length);
        Assert.All(E(mirrored.Graph.Strokes).Cast<Relation>(), stroke =>
        { Assert.True(QualitativeStrokeGraph.TryReadStroke(stroke, out var cause, out _, out _, out _)); Assert.IsType<Relation>(cause); });
        Assert.DoesNotContain(Traverse(mirrored.Surface), item => item is Carrier or CarrierLocation);
    }

    [Fact]
    public void TwoDirectionPatchJoinsSeamsByIdentityBeforeAnyCoordinateRead()
    {
        var study = new QualitativeSquareStudy(); var r = study.DirectionTurn;
        var pattern = study.Generate("hook", S(), S(r), S(r, r), S(r, r, r));
        var patch = new QualitativeTilePatch(study, pattern, 2, 2);
        Assert.Equal(4, E(patch.Placements).Length); Assert.Equal(2, E(patch.HorizontalSteps).Length); Assert.Equal(2, E(patch.VerticalSteps).Length);
        Assert.Single(E(patch.CommutingCorners)); Assert.Equal(81, E(patch.Pattern.Graph.Sites).Length);
        Assert.Equal(32, E(patch.Pattern.Graph.Strokes).Length); Assert.NotEmpty(E(patch.BindingChecks));
        foreach (var check in E(patch.BindingChecks).Cast<Relation>())
        { Assert.True(study.Frame.TryReadResult(Assert.IsType<Relation>(check.Right), out var rest, out _, out var unknown)); Assert.Empty(E(rest)); Assert.Empty(E(unknown)); }
        Assert.DoesNotContain(Traverse(patch.Surface), item => item is Carrier or CarrierLocation);
        var aliases = E(patch.SiteCorrespondence).Cast<Relation>().GroupBy(row => row.Right, ReferenceEqualityComparer.Instance);
        Assert.Contains(aliases, group => group.Count() == 4); // the four tile corners are actually joined
    }

    [Fact]
    public void BoundedPatchCannotTurnOpenSourceMembershipOrUnboundEndpointsIntoAClosedPatch()
    {
        var study = new QualitativeSquareStudy(); var source = study.Generate("hook", S());
        var openSites = new QualitativeStrokeGraph(study.Frame, StructuralConstraint.Open(E(source.Graph.Sites)), source.Graph.Strokes);
        var openStrokes = new QualitativeStrokeGraph(study.Frame, source.Graph.Sites, StructuralConstraint.Open(E(source.Graph.Strokes)));
        Assert.Throws<ArgumentException>(() => new QualitativeTilePatch(study, source with { Graph = openSites }, 2, 2));
        Assert.Throws<ArgumentException>(() => new QualitativeTilePatch(study, source with { Graph = openStrokes }, 2, 2));
        var foreignStroke = QualitativeStrokeGraph.Stroke(new Placeholder(), new Placeholder(), study.Frame.Origin, S());
        var unbound = new QualitativeStrokeGraph(study.Frame, source.Graph.Sites, S(foreignStroke));
        Assert.Throws<ArgumentException>(() => new QualitativeTilePatch(study, source with { Graph = unbound }, 2, 2));
    }

    [Fact]
    public void ADeclaredLeanKeepsLandmarkIdentityAndTopologyWithoutKnowingItsMagnitude()
    {
        var study = new QualitativeSquareStudy(); var r = study.DirectionTurn; var pattern = study.Generate("hook", S(), S(r), S(r, r), S(r, r, r));
        var target = new QualitativeMotionFrame(); var q = new Placeholder();
        var transform = Shear(study, target, q); var result = study.Deform(pattern, transform);
        Assert.Empty(E(result.Unresolved)); Assert.Equal(25, E(result.SiteMap).Length); Assert.Equal(8, E(result.EndpointChecks).Length);
        Assert.True(pattern.Graph.TryReadMapPassed(result.Graph, result.MapRead));
        Assert.Contains(Traverse(result.Surface), item => ReferenceEquals(item, q));
        Assert.DoesNotContain(Traverse(result.Surface), item => item is Carrier or CarrierLocation);
        Assert.Equal(25, E(result.Bindings).Length); Assert.Equal(8, E(result.Graph.Strokes).Length);
    }

    [Fact]
    public void AnInconsistentSubstitutionCannotCertifyAConsistentGeometricEmbedding()
    {
        var study = new QualitativeSquareStudy(); var r = study.DirectionTurn; var pattern = study.Generate("hook", S(), S(r), S(r, r), S(r, r, r));
        var frame = study.Frame;
        var bad = new QualitativePatternTransform(frame, frame, S(
            new Relation(QualitativeMotionFrame.Move(frame.Right, study.Reach), S(QualitativeMotionFrame.Move(frame.Right, study.Reach))),
            new Relation(QualitativeMotionFrame.Move(frame.Left, study.Reach), S(QualitativeMotionFrame.Move(frame.Right, study.Reach))),
            new Relation(QualitativeMotionFrame.Move(frame.Up, study.Reach), S(QualitativeMotionFrame.Move(frame.Up, study.Reach))),
            new Relation(QualitativeMotionFrame.Move(frame.Down, study.Reach), S(QualitativeMotionFrame.Move(frame.Down, study.Reach)))));
        var result = study.Deform(pattern, bad); Assert.NotEmpty(E(result.Unresolved));
        Assert.False(pattern.Graph.TryReadMapPassed(result.Graph, result.MapRead));
        Assert.True(StructuralConstraint.TryReadScope(result.Graph.Strokes, out _, out var seam, out var tail)); Assert.False(seam.Equals(tail));
    }

    [Fact]
    public void OpenActionAndOpenInverseCannotAcquireClosedParticipation()
    {
        var study = new QualitativeSquareStudy(); var open = StructuralConstraint.Open(E(study.DirectionTurn));
        var transform = study.AxisTransform(open); var read = transform.Apply(S(QualitativeMotionFrame.Move(study.Frame.Right, study.Reach)));
        Assert.False(transform.TryReadComplete(read, out _));
        var inverse = study.Inverse(StructuralConstraint.Open(QualitativeMotionFrame.Move(study.Frame.Right, study.Reach)));
        Assert.True(StructuralConstraint.TryReadScope(inverse, out _, out var seam, out var tail)); Assert.False(seam.Equals(tail));
    }

    [Fact]
    public void TransformReaderRejectsBorrowedOutputAndPreservesAmbiguousOrForeignInputs()
    {
        var study = new QualitativeSquareStudy(); var transform = study.AxisTransform(study.DirectionTurn);
        var right = QualitativeMotionFrame.Move(study.Frame.Right, study.Reach); var read = transform.Apply(S(right));
        Assert.True(transform.TryReadComplete(read, out var up));
        var forged = new Relation(read.Left, new Relation(S(right), Assert.IsType<Relation>(read.Right).Right));
        Assert.False(transform.TryReadComplete(forged, out _)); Assert.Same(study.Frame.Up, Assert.IsType<Relation>(Assert.Single(E(up))).Left);
        Assert.False(transform.TryReadComplete(transform.Apply(S(QualitativeMotionFrame.Move(new QualitativeMotionFrame().Right, study.Reach))), out _));
        var duplicate = new QualitativePatternTransform(study.Frame, study.Frame, S([.. E(transform.Rows), E(transform.Rows)[0]]));
        Assert.False(duplicate.TryReadComplete(duplicate.Apply(S(right)), out _));
    }

    [Fact]
    public void FrameTurnAndActualMotifInvarianceAreSeparateQuestions()
    {
        var study = new QualitativeSquareStudy(); var r = study.DirectionTurn; var m = study.DirectionMirror;
        var cyclic = study.Generate("turns", S(), S(r), S(r, r), S(r, r, r));
        var flips = study.Generate("flips", S(), S(m), S(r, r, m), S(m, r, r, m));
        var all = study.Generate("both", S(), S(r), S(r, r), S(r, r, r), S(m), S(r, m), S(r, r, m), S(r, r, r, m));
        Assert.True(study.ReadSymmetry(cyclic, r, study.SiteTurn).Preserves);
        Assert.False(study.ReadSymmetry(cyclic, m, study.SiteMirror).Preserves);
        Assert.False(study.ReadSymmetry(flips, r, study.SiteTurn).Preserves);
        Assert.True(study.ReadSymmetry(flips, m, study.SiteMirror).Preserves);
        Assert.True(study.ReadSymmetry(all, r, study.SiteTurn).Preserves);
        Assert.True(study.ReadSymmetry(all, m, study.SiteMirror).Preserves);
    }

    [Fact]
    public void OptionalMetricDistinguishesSquareStretchAndLeanWithoutChangingNativePromises()
    {
        var examples = SquareCellExperiment.Create(); var metrics = examples.Cases.Select(item => SquareCellExperiment.ObserveMetric(examples.Study, item)).ToArray();
        Assert.All(metrics.Take(3), metric => Assert.True(metric.QuarterTurnIsEuclideanIsometry));
        Assert.All(metrics.Skip(3), metric => Assert.False(metric.QuarterTurnIsEuclideanIsometry));
        Assert.All(metrics, metric => Assert.True(metric.Determinant > 0));
        Assert.Same(examples.Cases[4].Deformation, examples.Cases[5].Deformation);
        Assert.NotEqual(metrics[4], metrics[5]); Assert.Equal(metrics[4].Determinant, metrics[5].Determinant);
        Assert.All(examples.Cases, item => Assert.Empty(E(item.Deformation.Unresolved)));
    }

    [Fact]
    public void DifferentPeriodicChannelsRetainTheirOwnActualPortAndBoundaryWitnesses()
    {
        var study = new QualitativeSquareStudy(); var pattern = study.Generate("one hook", S()); var patch = new QualitativeTilePatch(study, pattern, 2, 2);
        foreach (var row in E(patch.HorizontalSteps).Cast<Relation>()) Assert.Same(study.Cell.SeamX, row.Right);
        foreach (var row in E(patch.VerticalSteps).Cast<Relation>()) Assert.Same(study.Cell.SeamY, row.Right);
        Assert.NotSame(study.Cell.SeamX, study.Cell.SeamY); Assert.NotSame(study.Cell.XWord, study.Cell.YWord);
        Assert.All(E(patch.SeamApplications).Cast<Relation>(), row => Assert.Contains(E(patch.HorizontalSteps).Concat(E(patch.VerticalSteps)), edge => ReferenceEquals(edge, row.Left)));
    }

    [Fact]
    public void IncompleteDeformationRetainsOpenLandmarksAndStrokeObligations()
    {
        var study = new QualitativeSquareStudy(); var r = study.DirectionTurn; var pattern = study.Generate("hook", S(), S(r), S(r, r), S(r, r, r));
        var transform = study.AxisTransform(StructuralConstraint.Open(E(r))); var result = study.Deform(pattern, transform);
        Assert.NotEmpty(E(result.Unresolved)); Assert.False(pattern.Graph.TryReadMapPassed(result.Graph, result.MapRead));
        Assert.Contains(E(result.Bindings).Cast<Relation>(), binding => StructuralConstraint.TryReadScope(Assert.IsType<Relation>(binding.Right), out _, out var seam, out var tail) && !seam.Equals(tail));
    }

    private static QualitativePatternTransform Shear(QualitativeSquareStudy study, QualitativeMotionFrame target, IElement q) => new(study.Frame, target, S(
        new Relation(QualitativeMotionFrame.Move(study.Frame.Right, study.Reach), S(QualitativeMotionFrame.Move(target.Right, study.Reach))),
        new Relation(QualitativeMotionFrame.Move(study.Frame.Left, study.Reach), S(QualitativeMotionFrame.Move(target.Left, study.Reach))),
        new Relation(QualitativeMotionFrame.Move(study.Frame.Up, study.Reach), S(QualitativeMotionFrame.Move(target.Right, q), QualitativeMotionFrame.Move(target.Up, study.Reach))),
        new Relation(QualitativeMotionFrame.Move(study.Frame.Down, study.Reach), S(QualitativeMotionFrame.Move(target.Left, q), QualitativeMotionFrame.Move(target.Down, study.Reach)))));
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var item)) { if (!seen.Add(item)) continue; yield return item; if (item is Relation row) { stack.Push(row.Left); stack.Push(row.Right); } }
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
