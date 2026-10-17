using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class ConnectedWallpaperExperimentTests
{
    [Fact]
    public void SixTurnStarUsesInwardLandmarksAndExplicitlyClosesTwelveActualSides()
    {
        var study = new ConnectedWallpaperStudy(1, 1); var basis = study.Basis.Basis;
        var words = new QualitativeSixfoldWords(basis.Frame, basis.Reach);
        var example = ConnectedWallpaperExperiment.SixfoldStars(study, words, 1, 1);
        example.Machine.Run(); var field = study.Capture(example);
        Assert.True(example.Machine.IsComplete); Assert.Equal(12, E(field.SourceGraph.Strokes).Length);
        Assert.Equal(13, E(field.SourceGraph.Sites).Length); Assert.Equal(12, E(field.Graph.Sites).Length);
        Assert.Single(E(field.AcceptedSeams)); Assert.True(field.Join.TryReadPassed(field.Join.Evaluate()));
        Assert.Equal(1, field.Graph.Describe(field.Graph.Evaluate()).Contours);
        Assert.Empty(E(field.Unresolved)); Assert.Empty(E(field.BlockedSeams));
        var firstStroke = (Relation)E(field.SourceGraph.Strokes)[0];
        QualitativeStrokeGraph.TryReadStroke(firstStroke, out _, out _, out var inward, out _);
        var inwardBinding = Assert.Single(E(field.SourceBindings).Cast<Relation>(), row => ReferenceEquals(row.Left, inward));
        Assert.True(Equal(basis, (Relation)inwardBinding.Right, S([.. E(words.U), .. E(words.V)])));
        Assert.DoesNotContain(Traverse(field.Surface), item => item is Carrier or CarrierLocation);
    }

    [Fact]
    public void ExecutedSixfoldPatchJoinsItsActualAxialAndDiagonalPortsWithoutRewritingSourceSites()
    {
        var study = new ConnectedWallpaperStudy(2, 2); var basis = study.Basis.Basis;
        var words = new QualitativeSixfoldWords(basis.Frame, basis.Reach);
        var example = ConnectedWallpaperExperiment.SixfoldRings(study, words, 2, 2);
        var machine = example.Machine; machine.Run(128);
        Assert.True(machine.IsComplete); Assert.Equal(4, E(machine.CompletedMotifs).Length);
        Assert.Equal(3, E(machine.AcceptedPassages).Length); Assert.Equal(24, E(machine.Drawing.Strokes).Length);
        Assert.Empty(E(machine.Pending)); Assert.Empty(E(machine.Unresolved));
        var sourceState = machine.Drawing.State; var sourceSites = machine.Drawing.SiteBindings;
        var sourceStrokes = machine.Drawing.Strokes;
        var field = study.Capture(example);
        Assert.Empty(E(field.Unresolved)); Assert.Empty(E(field.BlockedSeams));
        Assert.True(field.Join.TryReadPassed(field.Join.Evaluate()));
        Assert.Same(sourceState, machine.Drawing.State); Assert.Same(sourceSites, machine.Drawing.SiteBindings);
        Assert.Same(sourceStrokes, machine.Drawing.Strokes); Assert.Same(sourceStrokes, field.SourceGraph.Strokes);
        Assert.Equal(28, E(field.SourceGraph.Sites).Length); Assert.Equal(19, E(field.Graph.Sites).Length);
        Assert.Equal(24, E(field.Graph.Strokes).Length); Assert.Equal(9, E(field.AcceptedSeams).Length);
        Assert.DoesNotContain(Traverse(new Relation(words.Surface, field.Surface)), item => item is Carrier or CarrierLocation);

        var diagonal = S([.. E(basis.Inverse(example.ColumnPitch)), .. E(example.RowPitch)]);
        var counts = new int[4]; Relation? actualDiagonal = null;
        foreach (var accepted in E(field.AcceptedSeams).Cast<Relation>())
        {
            var context = Assert.IsType<Relation>(Assert.IsType<Relation>(accepted.Right).Left);
            var connection = Assert.IsType<Relation>(context.Left);
            var period = Assert.IsType<Relation>(Assert.IsType<Relation>(connection.Left).Right);
            if (Equal(basis, period, S())) counts[0]++;
            else if (Equal(basis, period, example.ColumnPitch)) counts[1]++;
            else if (Equal(basis, period, example.RowPitch)) counts[2]++;
            else { Assert.True(Equal(basis, period, diagonal)); counts[3]++; actualDiagonal = connection; }
            var witness = Assert.IsType<Relation>(Assert.IsType<Relation>(accepted.Right).Right);
            var comparisons = Assert.IsType<Relation>(witness.Right);
            AssertZero(basis.Frame, Assert.IsType<Relation>(comparisons.Left));
            AssertZero(basis.Frame, Assert.IsType<Relation>(comparisons.Right));
        }
        Assert.Equal(new[] { 4, 2, 2, 1 }, counts);
        Assert.NotNull(actualDiagonal);
        var diagonalCells = Assert.IsType<Relation>(Assert.IsType<Relation>(actualDiagonal!.Left).Left);
        Assert.Same(E(Assert.IsType<Relation>(E(example.Cells)[0]))[1], diagonalCells.Left);
        Assert.Same(E(Assert.IsType<Relation>(E(example.Cells)[1]))[0], diagonalCells.Right);

        // The seven published occurrences are six formal positions plus a
        // separately identified return. This is read from actual receipts.
        var firstCell = E(Assert.IsType<Relation>(E(example.Cells)[0]))[0];
        var definitions = E(example.PortDefinitions).Cast<Relation>().ToArray();
        Assert.Equal(7, definitions.Length); var firstBindings = new List<Relation>();
        for (var index = 0; index < definitions.Length; index++)
        {
            var publication = Assert.Single(E(field.PortPublications).Cast<Relation>(), item =>
                item.Left is Relation owner && ReferenceEquals(owner.Left, firstCell) && ReferenceEquals(owner.Right, definitions[index]));
            var binding = PublicationBinding(publication); firstBindings.Add(binding);
            var direction = Assert.IsType<Relation>(E(words.Orbit)[index % 6]);
            Assert.True(Equal(basis, Assert.IsType<Relation>(binding.Right), S([.. E(direction), .. E(direction)])));
        }
        Assert.NotSame(firstBindings[0].Left, firstBindings[^1].Left);
        Assert.Same(Quotient(field, firstBindings[0].Left), Quotient(field, firstBindings[^1].Left));

        // Incidence is determined by the declared equivalence classes and
        // copied source edges, independent of any Euclidean display basis.
        var sites = E(field.Graph.Sites); var edges = E(field.Graph.Strokes).Cast<Relation>().ToArray();
        var degrees = sites.Select(site => edges.Sum(edge =>
        {
            Assert.True(QualitativeStrokeGraph.TryReadStroke(edge, out _, out var from, out var to, out _));
            return (ReferenceEquals(site, from) ? 1 : 0) + (ReferenceEquals(site, to) ? 1 : 0);
        })).ToArray();
        Assert.Equal(5, degrees.Count(degree => degree == 4)); Assert.All(degrees, degree => Assert.True(degree is 2 or 4));
        foreach (var row in E(field.Join.StrokeMap).Cast<Relation>())
        {
            var original = Assert.IsType<Relation>(row.Left); var copied = Assert.IsType<Relation>(row.Right);
            Assert.True(QualitativeStrokeGraph.TryReadStroke(original, out _, out var from, out var to, out var chord));
            Assert.True(QualitativeStrokeGraph.TryReadStroke(copied, out var cause, out var mappedFrom, out var mappedTo, out var mappedChord));
            Assert.Same(original, cause); Assert.Same(chord, mappedChord);
            Assert.Same(Quotient(field, from), mappedFrom); Assert.Same(Quotient(field, to), mappedTo);
        }
        ClassesHaveOnlyEqualBindings(field, basis);

        // Keeping the diagonal neighbour while swapping its destination role
        // must expose a failed seam rather than manufacture the same junction.
        var pair = Assert.IsType<Relation>(Assert.Single(E(Assert.IsType<Relation>(actualDiagonal.Right))));
        var wrongDiagonal = new Relation(actualDiagonal.Left, S(new Relation(pair.Left, pair.Left)));
        var wrongConnections = S(E(example.ConnectionDefinitions).Select(connection =>
            ReferenceEquals(connection, actualDiagonal) ? wrongDiagonal : connection).ToArray());
        var rejected = study.Capture(example, wrongConnections);
        Assert.Single(E(rejected.BlockedSeams)); Assert.Equal(8, E(rejected.AcceptedSeams).Length);
        Assert.Same(sourceState, machine.Drawing.State); Assert.Same(sourceStrokes, machine.Drawing.Strokes);
    }

    [Fact]
    public void AReturnedRingVertexNeedsItsExplicitLocalJoinEvenWhenItsFormalPositionAgrees()
    {
        var study = new ConnectedWallpaperStudy(1, 1); var basis = study.Basis.Basis;
        var words = new QualitativeSixfoldWords(basis.Frame, basis.Reach);
        var example = ConnectedWallpaperExperiment.SixfoldRings(study, words, 1, 1);
        example.Machine.Run(32); Assert.True(example.Machine.IsComplete);
        var declared = study.Capture(example); var unjoined = study.Capture(example, S());
        Assert.Equal(7, E(unjoined.SourceGraph.Sites).Length); Assert.Equal(7, E(unjoined.Graph.Sites).Length);
        Assert.Equal(6, E(declared.Graph.Sites).Length); Assert.Single(E(declared.AcceptedSeams));
        var rawRead = unjoined.Graph.Evaluate(); var closedRead = declared.Graph.Evaluate();
        Assert.True(unjoined.Graph.TryReadResult(rawRead, out _, out var rawCycles, out _, out _)); Assert.Empty(E(rawCycles));
        Assert.True(declared.Graph.TryReadResult(closedRead, out _, out var closedCycles, out _, out _)); Assert.Single(E(closedCycles));
        var ports = E(declared.PortPublications).Cast<Relation>().ToArray();
        var first = PublicationBinding(ports[0]); var last = PublicationBinding(ports[^1]);
        Assert.NotSame(first.Left, last.Left); Assert.True(Equal(basis, Assert.IsType<Relation>(first.Right), Assert.IsType<Relation>(last.Right)));
        Assert.NotSame(Quotient(unjoined, first.Left), Quotient(unjoined, last.Left));
        Assert.Same(Quotient(declared, first.Left), Quotient(declared, last.Left));
        Assert.Empty(E(unjoined.Unresolved)); Assert.Empty(E(unjoined.BlockedSeams));
        Assert.NotEmpty(E(declared.Frontier)); // finite boundary remains explicit
    }

    [Fact]
    public void UnexecutedNeighbourMotifsRemainUnresolvedInsteadOfPublishingPredictedPorts()
    {
        var study = new ConnectedWallpaperStudy(2, 2); var basis = study.Basis.Basis;
        var words = new QualitativeSixfoldWords(basis.Frame, basis.Reach);
        var example = ConnectedWallpaperExperiment.SixfoldRings(study, words, 2, 2);
        example.Machine.Run(8); Assert.Single(E(example.Machine.CompletedMotifs)); Assert.False(example.Machine.IsComplete);
        var field = study.Capture(example); Assert.NotEmpty(E(field.Unresolved));
        Assert.Equal(7, E(field.PortPublications).Length); Assert.Single(E(field.AcceptedSeams));
        var firstCell = E(Assert.IsType<Relation>(E(example.Cells)[0]))[0];
        Assert.All(E(field.PortPublications).Cast<Relation>(), publication => Assert.Same(firstCell, Assert.IsType<Relation>(publication.Left).Left));
    }

    [Fact]
    public void MeasuredGramReadDistinguishesTriangularAndSquareViewsOfTheActualWordAction()
    {
        var words = new QualitativeSixfoldWords(new QualitativeMotionFrame(), new Placeholder());
        var triangular = ConnectedWallpaperExperiment.ReadSixfoldMetric(words, true);
        var square = ConnectedWallpaperExperiment.ReadSixfoldMetric(words, false);
        Assert.True(triangular.PreservesSixfoldAction); Assert.False(square.PreservesSixfoldAction);
        Assert.True(triangular.Determinant > 0); Assert.True(square.Determinant > 0);
        Assert.Equal(triangular.XX, triangular.YY); Assert.Equal(triangular.XX, 2 * triangular.XY);
        Assert.Equal(0, square.XY);
        // The geometric distinction does not change the same native closure
        // and half-turn cancellation certificates.
        AssertZero(words.Frame, words.HalfTurnRead); AssertZero(words.Frame, words.FullTurnRead);
        Assert.DoesNotContain(Traverse(words.Surface), item => item is Carrier or CarrierLocation);
    }

    private static Relation PublicationBinding(Relation publication) =>
        Assert.IsType<Relation>(Assert.IsType<Relation>(Assert.IsType<Relation>(publication.Right).Right).Right);
    private static IElement Quotient(ConnectedWallpaperStudy.Field field, IElement site) =>
        Assert.Single(E(field.Join.SiteMap).Cast<Relation>(), row => ReferenceEquals(row.Left, site)).Right;
    private static bool Equal(QualitativeSquareStudy basis, Relation first, Relation second) =>
        basis.Frame.TryReadResult(basis.Frame.Reduce(first, basis.Inverse(second)), out var remaining, out _, out var pending) &&
        E(remaining).Length == 0 && E(pending).Length == 0;
    private static void AssertZero(QualitativeMotionFrame frame, Relation read)
    { Assert.True(frame.TryReadResult(read, out var remaining, out _, out var pending)); Assert.Empty(E(remaining)); Assert.Empty(E(pending)); }
    private static void ClassesHaveOnlyEqualBindings(ConnectedWallpaperStudy.Field field, QualitativeSquareStudy basis)
    {
        foreach (var item in E(field.Join.Classes).Cast<Relation>())
        {
            var partition = Assert.IsType<Relation>(item.Left); var members = E(Assert.IsType<Relation>(partition.Left));
            var evidence = Assert.IsType<Relation>(item.Right);
            if (members.Length > 1) Assert.NotEmpty(E(Assert.IsType<Relation>(evidence.Left)));
            var output = Assert.Single(E(field.Bindings).Cast<Relation>(), row => ReferenceEquals(row.Left, partition.Right));
            foreach (var member in members)
            {
                var original = Assert.Single(E(field.SourceBindings).Cast<Relation>(), row => ReferenceEquals(row.Left, member));
                Assert.True(Equal(basis, Assert.IsType<Relation>(output.Right), Assert.IsType<Relation>(original.Right)));
            }
        }
    }
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var element))
        { if (!seen.Add(element)) continue; yield return element; if (element is Relation row) { stack.Push(row.Left); stack.Push(row.Right); } }
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
