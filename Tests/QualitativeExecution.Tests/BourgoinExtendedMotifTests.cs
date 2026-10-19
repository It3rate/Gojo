using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class BourgoinExtendedMotifTests
{
    private static readonly Lazy<BourgoinPatternLibrary> Library = new(() => new());

    [Fact]
    public void TenAdditionalMotifsReplayTheirActualNativeProgramsAndContracts()
    {
        var recipes = Enumerable.Range(25, 10).Select(index => Recipe($"B{index}")).ToArray();
        Assert.Equal(10, recipes.Length);
        foreach (var recipe in recipes)
        {
            var start = Assert.IsType<Relation>(recipe.Window.Left);
            var window = OrnamentProgram.Window(start.Left, start.Right, 2, 2);
            var archive = OrnamentFieldPacket.Save(recipe.Program, recipe.Bindings, window, recipe.SeamContract);
            Assert.DoesNotContain(recipe.Name, archive, StringComparison.Ordinal);
            Assert.True(OrnamentFieldPacket.TryLoad(archive, out var loaded, out var unknown), recipe.Id);
            Assert.Empty(E(unknown));
            var run = loaded.Program.Generate(loaded.Bindings, loaded.Window);
            Assert.True(run.IsComplete, recipe.Id);
            Assert.Equal(4, run.Units.Count);
            var field = new OrnamentProgramField(run, loaded.SeamContract);
            Assert.True(field.IsComplete, recipe.Id);
            Assert.True(field.Join.TryReadPassed(field.Join.Evaluate()), recipe.Id);
            Assert.NotEmpty(E(field.Graph.Strokes));
            Assert.All(Reachable(new Relation(loaded.Program.Surface, loaded.SeamContract)), element =>
                Assert.True(element is Relation or Placeholder, recipe.Id));
        }
    }

    [Fact]
    public void FloretStemsReuseOneActualPrototypeThroughSixActualSubstitutions()
    {
        var recipe = Recipe("B26");
        var read = Read(recipe);
        Assert.True(read.IsReadable);
        Assert.Empty(E(read.Blocked));
        Assert.Equal(7, read.Images.Count);
        var stems = read.Images.Where(image => E((Relation)image.SourcePath.Right).Length == 1).ToArray();
        Assert.Equal(6, stems.Length);
        Assert.All(stems, image => Assert.Same(stems[0].Prototype, image.Prototype));
        Assert.Equal(6, stems.Select(image => Position(recipe.Frame, Assert.IsType<Relation>(Assert.Single(E((Relation)image.OutputPath.Right))))).Distinct().Count());
        var selected = stems[1];
        var transformParts = Assert.IsType<Relation>(selected.Transform.Right);
        var incompleteRows = S(E(Assert.IsType<Relation>(transformParts.Left)).Skip(1).ToArray());
        var incomplete = new QualitativePatternTransform(recipe.Frame, recipe.Frame, incompleteRows, transformParts.Right);
        var sourceWord = Assert.IsType<Relation>(Assert.Single(E((Relation)selected.SourcePath.Right)));
        Assert.False(incomplete.TryReadComplete(incomplete.Apply(sourceWord), out _));
    }

    [Fact]
    public void FacetedArchRetainsItsCoarseChordAndActualPrototypeReusedByAllImages()
    {
        var recipe = Recipe("B30");
        var surface = Assert.IsType<Relation>(Assert.Single(E(recipe.Motifs)));
        var evidence = MotifEvidence(surface);
        var fineSurface = Assert.Single(E(evidence).OfType<Relation>(), entry => ReferenceEquals(entry.Left, recipe.Frame.Surface));
        var fine = Program(recipe.Frame, fineSurface);
        var finePath = Assert.IsType<Relation>(Assert.Single(E(fine.Paths)));
        Assert.Equal(20, E((Relation)finePath.Right).Length);
        var provenance = Assert.IsType<Relation>(fine.Evidence);
        var coarse = Program(recipe.Frame, Assert.IsType<Relation>(provenance.Left));
        var sourcePath = Assert.IsType<Relation>(Assert.Single(E(coarse.Paths)));
        Assert.Single(E((Relation)sourcePath.Right));
        var result = Assert.IsType<Relation>(provenance.Right);
        var replacements = Assert.IsType<Relation>(result.Left);
        var replacement = Assert.IsType<Relation>(Assert.Single(E(replacements)));
        Assert.Same(E((Relation)sourcePath.Right)[0], replacement.Left);
        var proof = Assert.IsType<Relation>(Assert.Single(E(Assert.IsType<Relation>(result.Right))));
        Assert.Same(replacement.Left, proof.Left);
        var proofResult = Assert.IsType<Relation>(proof.Right);
        Assert.Same(replacement.Right, proofResult.Left);
        AssertZero(recipe.Frame, Assert.IsType<Relation>(proofResult.Right));
        var read = Read(recipe);
        Assert.Equal(4, read.Images.Count);
        Assert.All(read.Images, image => Assert.Same(finePath, image.SourcePath));
        Assert.Equal(4, read.Images.Select(image => Position(recipe.Frame, image.Shift)).Distinct().Count());

        // A finer profile is not entitled to move the coarse endpoint.
        var words = E(Assert.IsType<Relation>(replacement.Right)).ToArray();
        var first = Assert.IsType<Relation>(words[0]);
        var reach = Assert.IsType<Relation>(E(first)[0]).Right;
        words[0] = S(E(first).Append(QualitativeMotionFrame.Move(recipe.Frame.Up, reach)).ToArray());
        Assert.Throws<ArgumentException>(() => coarse.Refine("wrong endpoint", S(new Relation(replacement.Left, S(words)))));
    }

    [Fact]
    public void CloudProfilePublishesTheCentralTopWhereTheStaggeredCourseActuallyStarts()
    {
        var recipe = Recipe("B30");
        var read = Read(recipe);
        var first = Assert.Single(read.Images, image => Position(recipe.Frame, image.Shift) == (0, 0));
        var next = Assert.Single(read.Images, image => Position(recipe.Frame, image.Shift) == (12, 8));
        var words = E((Relation)first.OutputPath.Right).Cast<Relation>().ToArray();
        Assert.Equal((12, 8), Position(recipe.Frame, S(words.Take(10).SelectMany(E).ToArray())));
        var top = Assert.Single(E(read.Ports).Cast<Relation>(), port => port.Right is Relation selector &&
            ReferenceEquals(selector.Left, first.OutputPath) && ReferenceEquals(selector.Right, words[10])).Left;
        var startWord = E((Relation)next.OutputPath.Right)[0];
        var start = Assert.Single(E(read.Ports).Cast<Relation>(), port => port.Right is Relation selector &&
            ReferenceEquals(selector.Left, next.OutputPath) && ReferenceEquals(selector.Right, startWord)).Left;
        Assert.Contains(E((Relation)recipe.SeamContract.Right).Cast<Relation>(), rule =>
        {
            var connection = (Relation)rule.Right;
            var roles = (Relation)connection.Right;
            return E((Relation)connection.Left).Length == 0 &&
                (ReferenceEquals(roles.Left, top) && ReferenceEquals(roles.Right, start) ||
                 ReferenceEquals(roles.Left, start) && ReferenceEquals(roles.Right, top));
        });
    }

    [Fact]
    public void FacetedLoopsCrossThroughInteriorsWithoutManufacturingFourJunctionSites()
    {
        var recipe = Recipe("B34");
        var native = Read(recipe);
        Assert.Equal(2, native.ClosedPathCount);
        var loops = native.Images.Where(image => E((Relation)image.SourcePath.Right).Length == 8).ToArray();
        Assert.Equal(2, loops.Length);
        Assert.Same(loops[0].Prototype, loops[1].Prototype);
        Assert.NotSame(loops[0].Transform, loops[1].Transform);
        var start = Assert.IsType<Relation>(recipe.Window.Left);
        var run = recipe.Program.Generate(recipe.Bindings, OrnamentProgram.Window(start.Left, start.Right, 1, 1));
        var field = new OrnamentProgramField(run, recipe.SeamContract);
        Assert.True(field.IsComplete);
        Assert.NotEmpty(E(field.Accepted)); // Actual outer cap/passage joins.
        var positions = E(field.Bindings).Cast<Relation>().Select(binding => Position(recipe.Frame, Assert.IsType<Relation>(binding.Right))).ToArray();
        foreach (var crossing in new[] { (-2, -2), (-2, 2), (2, -2), (2, 2) }) Assert.DoesNotContain(crossing, positions);
        Assert.Equal(0, field.Graph.Describe(field.Graph.Evaluate()).X);
    }

    [Fact]
    public void ALoopImageWithALookalikePrototypeWordNeedsNewActualApplicationReceipts()
    {
        var recipe = Recipe("B34");
        var surface = Assert.IsType<Relation>(Assert.Single(E(recipe.Motifs)));
        var program = Program(recipe.Frame, surface);
        var image = Read(recipe).Images[0];
        var sourceWords = E((Relation)image.SourcePath.Right).ToArray();
        sourceWords[0] = S(E(Assert.IsType<Relation>(sourceWords[0])));
        var clone = new Relation(image.SourcePath.Left, S(sourceWords));
        var prototype = new Relation(clone, S(clone));
        var counterfeit = new Relation(new Relation(prototype, image.OutputPath), image.Evidence.Right);
        var changedEvidence = S(E(Assert.IsType<Relation>(program.Evidence)).Select(entry =>
            ReferenceEquals(entry, image.Evidence) ? counterfeit : entry).ToArray());
        var changed = new UnitArrangementProgram(recipe.Frame, "same visible words", program.Paths, program.Ports, program.ClosedPaths, changedEvidence);
        var read = new BourgoinConstructionRead(recipe.Frame, changed.Surface);
        Assert.NotEmpty(E(read.Blocked));
        Assert.DoesNotContain(read.Images, item => ReferenceEquals(item.Evidence, counterfeit));
    }

    private static BourgoinPatternLibrary.Recipe Recipe(string id) => Library.Value.Recipes.Single(recipe => recipe.Id == id);
    private static BourgoinConstructionRead Read(BourgoinPatternLibrary.Recipe recipe) => new(recipe.Frame, (Relation)Assert.Single(E(recipe.Motifs)));
    private static Relation MotifEvidence(Relation surface) => (Relation)((Relation)((Relation)surface.Right).Right).Right;
    private static UnitArrangementProgram Program(QualitativeMotionFrame frame, Relation surface)
    {
        Assert.Same(frame.Surface, surface.Left);
        var body = Assert.IsType<Relation>(surface.Right); var drawing = Assert.IsType<Relation>(body.Left); var remainder = Assert.IsType<Relation>(body.Right);
        return new(frame, "native inspection", Assert.IsType<Relation>(drawing.Left), Assert.IsType<Relation>(drawing.Right),
            Assert.IsType<Relation>(remainder.Left), remainder.Right);
    }
    private static (int X, int Y) Position(QualitativeMotionFrame frame, Relation word)
    {
        var read = frame.Reduce(S(), word);
        Assert.True(frame.TryReadResult(read, out var remaining, out _, out var unknown)); Assert.Empty(E(unknown));
        var x = 0; var y = 0;
        foreach (var move in E(remaining).Cast<Relation>())
        {
            Assert.True(frame.Direction(move, out var axis, out var forward, out _));
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
        }
        return (x, y);
    }
    private static void AssertZero(QualitativeMotionFrame frame, Relation read)
    { Assert.True(frame.TryReadResult(read, out var remaining, out _, out var unknown)); Assert.Empty(E(remaining)); Assert.Empty(E(unknown)); }
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue;
            yield return item;
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
