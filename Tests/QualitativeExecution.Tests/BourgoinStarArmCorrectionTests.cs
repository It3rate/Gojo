using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class BourgoinStarArmCorrectionTests
{
    private static readonly Lazy<BourgoinPatternLibrary> Library = new(() => new());
    private static readonly Lazy<OrnamentProgramField> StarField = new(() => Field(Recipe("B26"), 2, 2));

    [Fact]
    public void FlatStarRetainsSixCoarseLandmarksAndSixActualFourChordRefinements()
    {
        var recipe = Recipe("B26"); var read = Read(recipe);
        Assert.Equal(13, read.Images.Count); Assert.Equal(3, E(read.Prototypes).Length);
        Assert.Single(E(read.ClosedPaths)); Assert.Empty(E(read.Blocked));
        var refinement = Assert.Single(read.Refinements);
        Assert.Equal(6, E(refinement.Replacements).Length); Assert.Equal(6, E(refinement.Proofs).Length);
        var coarse = Program(recipe.Frame, refinement.Coarse); var fine = Program(recipe.Frame, refinement.Fine);
        var coarsePath = Assert.IsType<Relation>(Assert.Single(E(coarse.Paths)));
        var finePath = Assert.IsType<Relation>(Assert.Single(E(fine.Paths)));
        Assert.Equal(6, E((Relation)coarsePath.Right).Length); Assert.Equal(24, E((Relation)finePath.Right).Length);
        foreach (var row in E(refinement.Proofs).Cast<Relation>())
        {
            var proof = Assert.IsType<Relation>(row.Right);
            Assert.Equal(4, E((Relation)proof.Left).Length); AssertZero(recipe.Frame, (Relation)proof.Right);
        }
        foreach (var port in E(coarse.Ports).Cast<Relation>())
            Assert.Single(E(fine.Ports).Cast<Relation>(), current => ReferenceEquals(current.Left, port.Left));
        Assert.Single(read.Images, image => ReferenceEquals(image.SourcePath, finePath));
        var boundary = Assert.IsType<Relation>(Assert.Single(E(read.ClosedPaths)));
        var vectors = E((Relation)boundary.Right).Cast<Relation>().Select(word => Position(recipe.Frame, word)).ToArray();
        var turns = vectors.Select((vector, index) => Cross(vectors[(index + vectors.Length - 1) % vectors.Length], vector)).ToArray();
        Assert.Equal(6, turns.Count(turn => turn < 0)); // Inward corners.
        Assert.Equal(12, turns.Count(turn => turn > 0)); // Two ends of each flat tip.
        Assert.Equal(6, turns.Count(turn => turn == 0)); // Retained flat-tip midpoint roles.
    }

    [Fact]
    public void TwoSixfoldStemPrototypesProduceTwelveActualRadialParticipants()
    {
        var recipe = Recipe("B26"); var read = Read(recipe);
        var rays = read.Images.Where(image => E((Relation)image.SourcePath.Right).Length == 1).ToArray();
        Assert.Equal(12, rays.Length);
        var groups = rays.GroupBy(image => image.Prototype, ReferenceEqualityComparer.Instance).ToArray();
        Assert.Equal(2, groups.Length); Assert.All(groups, group => Assert.Equal(6, group.Count()));
        Assert.Equal(12, rays.Select(image => Position(recipe.Frame, (Relation)Assert.Single(E((Relation)image.OutputPath.Right)))).Distinct().Count());
        var field = Field(recipe, 1, 1);
        var centre = SiteAt(field, (0, 0));
        Assert.Equal(12, E(field.Graph.Strokes).Cast<Relation>().Count(edge =>
        {
            Assert.True(QualitativeStrokeGraph.TryReadStroke(edge, out _, out var from, out var to, out _));
            return Same(from, centre) || Same(to, centre);
        }));
    }

    [Fact]
    public void SmallHexagonalIntersticeIsAnActualSixEdgeRouteNotAnAddedHexagonMotif()
    {
        var field = StarField.Value; Assert.True(field.IsComplete);
        var points = new[] { (6, -2), (4, 0), (4, 2), (6, 2), (8, 0), (8, -2) };
        var sites = points.Select(point => SiteAt(field, point)).ToArray();
        var words = new List<IElement>();
        for (var index = 0; index < sites.Length; index++)
        {
            var from = sites[index]; var to = sites[(index + 1) % sites.Length];
            var actual = E(field.Graph.Strokes).Cast<Relation>().First(edge =>
            {
                QualitativeStrokeGraph.TryReadStroke(edge, out _, out var a, out var b, out _);
                return Same(a, from) && Same(b, to) || Same(a, to) && Same(b, from);
            });
            Assert.True(QualitativeStrokeGraph.TryReadStroke(actual, out _, out var start, out _, out var word));
            words.AddRange(E(Same(start, from) ? word : Inverse(field.Graph.Frame, word)));
        }
        AssertZero(field.Graph.Frame, field.Graph.Frame.Reduce(S(), S(words.ToArray())));
        var recipe = Recipe("B26");
        Assert.Single(E(Read(recipe).ClosedPaths));
        // The route is a boundary witness, not a certified occupied face.
    }

    [Fact]
    public void SlenderArmsHaveOnePrototypeConsistentHandednessAndOutwardTerminalHands()
    {
        var recipe = Recipe("B31"); var read = Read(recipe);
        Assert.Equal(5, read.Images.Count); Assert.Equal(2, E(read.Prototypes).Length);
        var arms = read.Images.Where(image => E((Relation)image.SourcePath.Right).Length == 3).ToArray();
        Assert.Equal(4, arms.Length); Assert.All(arms, image => Assert.Same(arms[0].Prototype, image.Prototype));
        Assert.Equal(4, arms.Select(image => image.Transform).Distinct(ReferenceEqualityComparer.Instance).Count());
        foreach (var arm in arms)
        {
            Assert.DoesNotContain(E(read.ClosedPaths), path => ReferenceEquals(path, arm.OutputPath));
            var vectors = E((Relation)arm.OutputPath.Right).Cast<Relation>().Select(word => Position(recipe.Frame, word)).ToArray();
            Assert.True(Cross(vectors[0], vectors[1]) < 0); Assert.True(Cross(vectors[1], vectors[2]) < 0);
            var wrist = Add(vectors[0], vectors[1]); var end = Add(wrist, vectors[2]);
            Assert.True(Norm(end) > Norm(wrist));
            Assert.True(end.X * vectors[2].X + end.Y * vectors[2].Y > 0);
        }
        var field = Field(recipe, 1, 1); var junction = SiteAt(field, (-12, 0));
        Assert.Equal(4, E(field.Graph.Strokes).Cast<Relation>().Count(edge =>
        {
            QualitativeStrokeGraph.TryReadStroke(edge, out _, out var from, out var to, out _);
            return Same(junction, from) || Same(junction, to);
        }));
    }

    [Fact]
    public void RoundMarkerIsAClosedNativeFacetProfileWithFourfoldCorrespondence()
    {
        var recipe = Recipe("B31"); var read = Read(recipe);
        var marker = Assert.Single(read.Images, image => E((Relation)image.SourcePath.Right).Length == 12);
        Assert.Same(marker.OutputPath, Assert.Single(E(read.ClosedPaths)));
        var points = Points(recipe.Frame, marker.OutputPath).SkipLast(1).ToArray();
        Assert.Equal(12, points.Length); Assert.Equal(12, points.Distinct().Count());
        Assert.All(points, point => Assert.InRange(Norm(point), 4, 5));
        Assert.All(points, point => Assert.Contains((-point.Y, point.X), points));
        AssertZero(recipe.Frame, recipe.Frame.Reduce(S(), S(E((Relation)marker.OutputPath.Right).Cast<Relation>().SelectMany(E).ToArray())));
        Assert.Contains(points, point => point.X != 0 && point.Y != 0);
        // Unequal squared radii deliberately rule out an exact analytic-circle
        // assertion; this is a small closed round approximation, not fill.
        Assert.Equal(2, points.Select(Norm).Distinct().Count());
    }

    [Theory]
    [InlineData("B26")]
    [InlineData("B31")]
    public void CorrectedDefinitionsReplayWithoutSourceFactoriesOrNumericElements(string id)
    {
        var recipe = Recipe(id); var start = (Relation)recipe.Window.Left;
        var window = OrnamentProgram.Window(start.Left, start.Right, 1, 1);
        var archive = OrnamentFieldPacket.Save(recipe.Program, recipe.Bindings, window, recipe.SeamContract);
        Assert.DoesNotContain(recipe.Name, archive, StringComparison.Ordinal);
        Assert.True(OrnamentFieldPacket.TryLoad(archive, out var loaded, out var pending)); Assert.Empty(E(pending));
        var run = loaded.Program.Generate(loaded.Bindings, loaded.Window); Assert.True(run.IsComplete);
        var field = new OrnamentProgramField(run, loaded.SeamContract); Assert.True(field.IsComplete);
        var read = new BourgoinConstructionRead(loaded.Program.Frame, (Relation)Assert.Single(E(loaded.Program.Motifs)));
        Assert.Empty(E(read.Blocked));
        Assert.All(Reachable(new Relation(loaded.Program.Surface, loaded.SeamContract)), item => Assert.True(item is Relation or Placeholder));
    }

    private static BourgoinPatternLibrary.Recipe Recipe(string id) => Library.Value.Recipes.Single(recipe => recipe.Id == id);
    private static BourgoinConstructionRead Read(BourgoinPatternLibrary.Recipe recipe) => new(recipe.Frame, (Relation)Assert.Single(E(recipe.Motifs)));
    private static OrnamentProgramField Field(BourgoinPatternLibrary.Recipe recipe, int columns, int rows)
    {
        var start = (Relation)recipe.Window.Left;
        var run = recipe.Program.Generate(recipe.Bindings, OrnamentProgram.Window(start.Left, start.Right, columns, rows));
        Assert.True(run.IsComplete); return new(run, recipe.SeamContract);
    }
    private static IElement SiteAt(OrnamentProgramField field, (int X, int Y) at) =>
        Assert.Single(E(field.Bindings).Cast<Relation>(), row => Position(field.Graph.Frame, (Relation)row.Right) == at).Left;
    private static UnitArrangementProgram Program(QualitativeMotionFrame frame, Relation surface)
    {
        Assert.Same(frame.Surface, surface.Left); var parts = (Relation)surface.Right;
        var geometry = (Relation)parts.Left; var rest = (Relation)parts.Right;
        return new(frame, "native inspection", (Relation)geometry.Left, (Relation)geometry.Right, (Relation)rest.Left, rest.Right);
    }
    private static IEnumerable<(int X, int Y)> Points(QualitativeMotionFrame frame, Relation path)
    {
        var current = Position(frame, (Relation)path.Left); yield return current;
        foreach (var word in E((Relation)path.Right).Cast<Relation>())
        { current = Add(current, Position(frame, word)); yield return current; }
    }
    private static (int X, int Y) Position(QualitativeMotionFrame frame, Relation word)
    {
        var result = frame.Reduce(S(), word); Assert.True(frame.TryReadResult(result, out var remaining, out _, out var pending)); Assert.Empty(E(pending));
        var x = 0; var y = 0;
        foreach (var move in E(remaining).Cast<Relation>())
        {
            Assert.True(frame.Direction(move, out var axis, out var forward, out _));
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
        }
        return (x, y);
    }
    private static (int X, int Y) Add((int X, int Y) a, (int X, int Y) b) => (a.X + b.X, a.Y + b.Y);
    private static int Cross((int X, int Y) a, (int X, int Y) b) => a.X * b.Y - a.Y * b.X;
    private static int Norm((int X, int Y) value) => value.X * value.X + value.Y * value.Y;
    private static Relation Inverse(QualitativeMotionFrame frame, Relation word) => S(E(word).Reverse().Cast<Relation>().Select(move =>
        (IElement)QualitativeMotionFrame.Move(E(frame.Opposites).Cast<Relation>().Single(row => Same(row.Left, move.Left)).Right, move.Right)).ToArray());
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static void AssertZero(QualitativeMotionFrame frame, Relation read)
    { Assert.True(frame.TryReadResult(read, out var remaining, out _, out var pending)); Assert.Empty(E(remaining)); Assert.Empty(E(pending)); }
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item))
        { if (!seen.Add(item)) continue; yield return item; if (item is Relation row) { pending.Push(row.Left); pending.Push(row.Right); } }
    }
    private static Relation S(params IElement[] elements) => StructuralConstraint.Closed(elements);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
