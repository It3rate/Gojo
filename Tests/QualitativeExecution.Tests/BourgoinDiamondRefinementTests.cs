using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class BourgoinDiamondRefinementTests : IClassFixture<BourgoinDiamondRefinementTests.Catalog>
{
    public sealed class Catalog
    {
        public BourgoinPatternLibrary Library { get; } = new();
    }

    private readonly Catalog catalog;
    public BourgoinDiamondRefinementTests(Catalog catalog) => this.catalog = catalog;

    [Theory]
    [InlineData("B10", 2)]
    [InlineData("B11", 4)]
    public void FineContoursRetainActualCoarsePathsReplacementChainsAndEndpointProofs(string id, int contours)
    {
        var recipe = Recipe(id);
        var motif = ReadProgram(recipe.Frame, Assert.IsType<Relation>(Assert.Single(E(recipe.Motifs))));
        var refinements = Refinements(recipe.Frame, motif).ToArray();
        Assert.Equal(contours, refinements.Length);
        Assert.Equal(contours, E(motif.Paths).Length);

        foreach (var fine in refinements)
        {
            var provenance = Assert.IsType<Relation>(fine.Evidence);
            var coarse = ReadProgram(recipe.Frame, Assert.IsType<Relation>(provenance.Left));
            var retained = Assert.IsType<Relation>(provenance.Right);
            var replacements = Assert.IsType<Relation>(retained.Left);
            var proofs = E(Assert.IsType<Relation>(retained.Right)).Cast<Relation>().ToArray();
            var sourcePath = Assert.IsType<Relation>(Assert.Single(E(coarse.Paths)));
            var finePath = Assert.IsType<Relation>(Assert.Single(E(fine.Paths)));
            var sourceWords = E(Assert.IsType<Relation>(sourcePath.Right));
            var fineWords = E(Assert.IsType<Relation>(finePath.Right));
            Assert.Equal(4, sourceWords.Length);
            Assert.Equal(16, fineWords.Length);
            Assert.Same(sourcePath.Left, finePath.Left);
            Assert.Contains(E(motif.Paths), path => ReferenceEquals(path, finePath));
            Assert.Equal(sourceWords.Length, E(replacements).Length);
            Assert.Equal(sourceWords.Length, proofs.Length);

            var flattened = new List<IElement>();
            foreach (var sourceWord in sourceWords)
            {
                var replacement = Assert.Single(E(replacements).Cast<Relation>(), row => ReferenceEquals(row.Left, sourceWord));
                var chain = Assert.IsType<Relation>(replacement.Right);
                var proof = Assert.Single(proofs, row => ReferenceEquals(row.Left, sourceWord));
                var result = Assert.IsType<Relation>(proof.Right);
                Assert.Same(chain, result.Left);
                AssertZero(recipe.Frame, Assert.IsType<Relation>(result.Right));
                flattened.AddRange(E(chain));
            }
            Assert.Equal(fineWords.Length, flattened.Count);
            for (var i = 0; i < fineWords.Length; i++) Assert.Same(flattened[i], fineWords[i]);

            foreach (var sourcePort in E(coarse.Ports).Cast<Relation>())
            {
                var oldPick = Assert.IsType<Relation>(sourcePort.Right);
                var actual = Assert.Single(E(fine.Ports).Cast<Relation>(), port => ReferenceEquals(port.Left, sourcePort.Left));
                Assert.Contains(E(motif.Ports), port => ReferenceEquals(port, actual));
                var newPick = Assert.IsType<Relation>(actual.Right);
                Assert.Same(finePath, newPick.Left);
                if (ReferenceEquals(oldPick.Right, sourcePath)) Assert.Same(finePath, newPick.Right);
                else
                {
                    var replacement = Assert.Single(E(replacements).Cast<Relation>(), row => ReferenceEquals(row.Left, oldPick.Right));
                    Assert.Same(E(Assert.IsType<Relation>(replacement.Right))[0], newPick.Right);
                }
            }
        }
    }

    [Theory]
    [InlineData("B10")]
    [InlineData("B11")]
    public void EveryCoarseDiagonalBecomesAPointedFourChordDetour(string id)
    {
        var recipe = Recipe(id);
        var motif = ReadProgram(recipe.Frame, Assert.IsType<Relation>(Assert.Single(E(recipe.Motifs))));
        foreach (var fine in Refinements(recipe.Frame, motif))
        {
            var retained = Assert.IsType<Relation>(Assert.IsType<Relation>(fine.Evidence).Right);
            foreach (var replacement in E(Assert.IsType<Relation>(retained.Left)).Cast<Relation>())
            {
                Assert.Equal(2, Axes(recipe.Frame, Assert.IsType<Relation>(replacement.Left)).Count);
                var chords = E(Assert.IsType<Relation>(replacement.Right)).Cast<Relation>().ToArray();
                Assert.Equal(4, chords.Length);
                Assert.Equal(2, E(chords[0]).Length);
                Assert.Equal(2, E(chords[3]).Length);
                Assert.Equal(2, Axes(recipe.Frame, chords[0]).Count);
                Assert.Equal(2, Axes(recipe.Frame, chords[3]).Count);
                Assert.Single(Axes(recipe.Frame, chords[1]));
                Assert.Single(Axes(recipe.Frame, chords[2]));
                Assert.NotSame(Axes(recipe.Frame, chords[1]).Single(), Axes(recipe.Frame, chords[2]).Single());
            }
        }
    }

    [Theory]
    [InlineData("B10")]
    [InlineData("B11")]
    public void EveryExecutedCompartmentIsAnExplicitClosedNativePathWithEveryCornerPublished(string id)
    {
        var recipe = Recipe(id);
        var motif = ReadProgram(recipe.Frame, Assert.IsType<Relation>(Assert.Single(E(recipe.Motifs))));
        Assert.Equal(E(motif.Paths).Length, E(motif.ClosedPaths).Length);
        foreach (var path in E(motif.Paths).Cast<Relation>())
        {
            Assert.Contains(E(motif.ClosedPaths), actual => ReferenceEquals(actual, path));
            var chords = E(Assert.IsType<Relation>(path.Right)).Cast<Relation>().ToArray();
            AssertZero(recipe.Frame, recipe.Frame.Reduce(S(), S(chords.SelectMany(E).ToArray())));
            var ports = E(motif.Ports).Cast<Relation>().Where(port => ReferenceEquals(((Relation)port.Right).Left, path)).ToArray();
            Assert.Equal(chords.Length + 1, ports.Length);
            foreach (var chord in chords)
                Assert.Single(ports, port => ReferenceEquals(((Relation)port.Right).Right, chord));
            Assert.Single(ports, port => ReferenceEquals(((Relation)port.Right).Right, path));
        }
    }

    [Theory]
    [InlineData("B10")]
    [InlineData("B11")]
    public void CoincidentIndependentContoursNeedDeclaredSeamsToShareSites(string id)
    {
        var recipe = Recipe(id);
        var origin = Assert.IsType<Relation>(recipe.Window.Left);
        var run = recipe.Program.Generate(recipe.Bindings, OrnamentProgram.Window(origin.Left, origin.Right, 1, 1));
        Assert.True(run.IsComplete);
        var separate = new OrnamentProgramField(run, OrnamentProgramField.Contract(recipe.Program));
        Assert.True(separate.IsComplete);
        Assert.Empty(E(separate.Accepted));
        Assert.Equal(E(separate.SourceGraph.Sites).Length, E(separate.Graph.Sites).Length);

        var bindings = E(separate.SourceBindings).Cast<Relation>().ToArray();
        Assert.Contains(bindings, a => bindings.Any(b => !ReferenceEquals(a.Left, b.Left) &&
            Zero(recipe.Frame, recipe.Frame.Reduce(Assert.IsType<Relation>(a.Right), Inverse(recipe.Frame, Assert.IsType<Relation>(b.Right))))));

        var connected = new OrnamentProgramField(run, recipe.SeamContract);
        Assert.True(connected.IsComplete);
        Assert.NotEmpty(E(connected.Accepted));
        Assert.True(E(connected.Graph.Sites).Length < E(separate.Graph.Sites).Length);
        Assert.Equal(E(separate.Graph.Strokes).Length, E(connected.Graph.Strokes).Length);
    }

    private BourgoinPatternLibrary.Recipe Recipe(string id) => catalog.Library.Recipes.Single(recipe => recipe.Id == id);

    private sealed record NativeProgram(Relation Surface, Relation Paths, Relation Ports, Relation ClosedPaths, IElement Evidence);

    private static NativeProgram ReadProgram(QualitativeMotionFrame frame, Relation surface)
    {
        Assert.Same(frame.Surface, surface.Left);
        var definition = Assert.IsType<Relation>(surface.Right);
        var drawing = Assert.IsType<Relation>(definition.Left);
        var closure = Assert.IsType<Relation>(definition.Right);
        return new(surface, Assert.IsType<Relation>(drawing.Left), Assert.IsType<Relation>(drawing.Right),
            Assert.IsType<Relation>(closure.Left), closure.Right);
    }

    private static IEnumerable<NativeProgram> Refinements(QualitativeMotionFrame frame, NativeProgram motif) =>
        E(Assert.IsType<Relation>(motif.Evidence)).OfType<Relation>()
            .Where(entry => ReferenceEquals(entry.Left, frame.Surface))
            .Select(entry => ReadProgram(frame, entry));

    private static HashSet<Relation> Axes(QualitativeMotionFrame frame, Relation word)
    {
        var axes = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        foreach (var move in E(word).Cast<Relation>())
        {
            Assert.True(frame.Direction(move, out var axis, out _, out _));
            axes.Add(axis);
        }
        return axes;
    }

    private static Relation Inverse(QualitativeMotionFrame frame, Relation word) => S(E(word).Reverse().Cast<Relation>().Select(move =>
        (IElement)QualitativeMotionFrame.Move(E(frame.Opposites).Cast<Relation>().Single(row => ReferenceEquals(row.Left, move.Left)).Right, move.Right)).ToArray());

    private static bool Zero(QualitativeMotionFrame frame, Relation read) =>
        frame.TryReadResult(read, out var remainder, out _, out var pending) &&
        StructuralConstraint.TryReadScope(remainder, out var terms, out var seam, out var tail) && seam.Equals(tail) &&
        terms.Length == 0 && E(pending).Length == 0;
    private static void AssertZero(QualitativeMotionFrame frame, Relation read) => Assert.True(Zero(frame, read));
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
