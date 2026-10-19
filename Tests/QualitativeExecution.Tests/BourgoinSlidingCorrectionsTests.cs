using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class BourgoinSlidingCorrectionsTests
{
    private static readonly Lazy<IReadOnlyList<BourgoinPatternLibrary.Recipe>> Triangles = new(() => BourgoinPatternLibrary.CreateTriangleSlides(3, 3, 2));
    private static readonly Lazy<IReadOnlyList<BourgoinPatternLibrary.Recipe>> Hexagons = new(() => BourgoinPatternLibrary.CreateHexagonSlides(4, 0, -1, 1));

    [Theory]
    [InlineData(true, 0, 2, 6)]
    [InlineData(true, 1, 2, 12)]
    [InlineData(false, 0, 1, 6)]
    [InlineData(false, 1, 1, 12)]
    [InlineData(false, 2, 1, 12)]
    public void ArchivedSamplesContainOnlyTheOriginalPolygonImagesAndExplicitContactLandmarks(bool triangle, int index, int paths, int chords)
    {
        var recipe = Sample(triangle, index); var phases = (Relation)recipe.Window.Left;
        var archive = OrnamentFieldPacket.Save(recipe.Program, recipe.Bindings,
            OrnamentProgram.Window(phases.Left, phases.Right, 2, 2), recipe.SeamContract);
        Assert.True(OrnamentFieldPacket.TryLoad(archive, out var packet, out var pending)); Assert.Empty(E(pending));
        Assert.All(Reachable(new Relation(packet.Program.Surface, packet.SeamContract)), item => Assert.True(item is Relation or Placeholder));
        var motif = Motif(packet.Program); var read = new BourgoinConstructionRead(packet.Program.Frame, motif);
        Assert.True(read.IsReadable); Assert.Empty(E(read.Blocked)); Assert.Empty(E(read.Unresolved));
        Assert.Equal(paths, read.PathCount); Assert.Equal(paths, read.ClosedPathCount); Assert.Equal(chords, read.ChordCount);
        Assert.Equal(paths, read.Images.Count); Assert.Single(E(read.Prototypes)); Assert.Single(read.Refinements);
        var field = new OrnamentProgramField(packet.Program.Generate(packet.Bindings, packet.Window), packet.SeamContract);
        Assert.True(field.IsComplete); Assert.NotEmpty(E(field.Accepted)); Assert.NotEmpty(E(field.Frontier));
        Assert.Empty(E(field.Blocked)); Assert.Empty(E(field.Unresolved));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void VariantsKeepOneActualCoarsePrototypeAndAccountForContactPlusExposedSide(bool triangle)
    {
        var family = triangle ? Triangles.Value : Hexagons.Value; Relation? coarse = null;
        foreach (var recipe in family)
        {
            var read = Read(recipe); var refinement = Assert.Single(read.Refinements);
            if (coarse is null) coarse = refinement.Coarse; else Assert.Same(coarse, refinement.Coarse);
            Assert.Same(family[0].Frame, recipe.Frame);
            var partitions = Partitions(recipe); Assert.Equal(triangle ? 3 : 6, partitions.Length);
            foreach (var part in partitions)
            {
                var fragments = E(part.Pieces).Cast<Relation>().ToArray();
                Assert.All(fragments, word => Assert.NotEmpty(E(word)));
                if (E(part.Contact).Length != 0) Assert.Contains(fragments, fragment => ReferenceEquals(fragment, part.Contact));
                if (E(part.Exposed).Length != 0) Assert.Contains(fragments, fragment => ReferenceEquals(fragment, part.Exposed));
                var all = fragments.SelectMany(E).ToArray();
                Assert.True(part.Sum.Left is Relation request && request.Right is Relation inputs &&
                    ReferenceEquals(request.Left, recipe.Frame.Surface) && inputs.Right is Relation actualWords &&
                    E(actualWords).SequenceEqual(all, ReferenceEqualityComparer.Instance));
                Assert.True(recipe.Frame.TryReadResult(part.Sum, out var sum, out _, out var unknown)); Assert.Empty(E(unknown));
                Assert.Equal(Profile(recipe.Frame, part.Whole), Profile(recipe.Frame, sum));
                Assert.True(part.Account.Left is Relation accounting && accounting.Right is Relation operands && ReferenceEquals(operands.Left, part.Whole));
                AssertZero(recipe.Frame, part.Account);
            }
        }
    }

    [Fact]
    public void TriangleSideSlideReplacesTipOnlyContactsWithPartialOverlapsAndShorterHexagonalRoutes()
    {
        var baseline = Field(Triangles.Value[0]);
        // This handed disposition's six-side witness uses three neighbouring
        // column slots. A 2x2 publication is valid but does not enclose it.
        var slidRecipe = Triangles.Value[1]; var phases = (Relation)slidRecipe.Window.Left;
        var slid = new OrnamentProgramField(slidRecipe.Program.Generate(slidRecipe.Bindings,
            OrnamentProgram.Window(phases.Left, phases.Right, 3, 2)), slidRecipe.SeamContract);
        Assert.True(baseline.IsComplete); Assert.True(slid.IsComplete);
        Assert.Equal(0, DuplicatePairs(baseline.Graph)); Assert.True(DuplicatePairs(slid.Graph) > 0);
        Assert.True(HasCircuit(slid.Graph, 6, 2));
        Assert.All(Partitions(Triangles.Value[0]), part => Assert.Empty(E(part.Contact)));
        Assert.All(Partitions(Triangles.Value[1]), part =>
        {
            Assert.Equal(3, SideSteps(Triangles.Value[1].Frame, part.Whole));
            Assert.Equal(1, SideSteps(Triangles.Value[1].Frame, part.Contact));
            Assert.Equal(2, SideSteps(Triangles.Value[1].Frame, part.Exposed));
        });
        Assert.Equal((5, -1), Profile(Triangles.Value[1].Frame, Triangles.Value[1].ColumnPitch));
        Assert.Equal((1, 4), Profile(Triangles.Value[1].Frame, Triangles.Value[1].RowPitch));
        // A native closed route is not a certified empty or filled face.
    }

    [Fact]
    public void SlidingHexagonsProduceSmallTriangleRoutesWithoutAuthoredTrianglePaths()
    {
        var aligned = Field(Hexagons.Value[0]); Assert.True(aligned.IsComplete); Assert.False(HasCircuit(aligned.Graph, 3, 1));
        foreach (var recipe in Hexagons.Value.Skip(1))
        {
            var read = Read(recipe); Assert.Single(E(read.Paths)); Assert.Single(E(read.ClosedPaths));
            var field = Field(recipe); Assert.True(field.IsComplete); Assert.True(HasCircuit(field.Graph, 3, 1));
            Assert.True(DuplicatePairs(field.Graph) > 0);
            Assert.All(Partitions(recipe), part =>
            {
                Assert.Equal(4, SideSteps(recipe.Frame, part.Whole)); Assert.Equal(3, SideSteps(recipe.Frame, part.Contact));
                Assert.Equal(1, SideSteps(recipe.Frame, part.Exposed));
            });
        }
    }

    [Fact]
    public void OppositeTangentialWordsReverseTheDispositionWhileTheActualHexagonPrototypeStaysShared()
    {
        var aligned = Hexagons.Value[0]; var negative = Hexagons.Value[1]; var positive = Hexagons.Value[2];
        Assert.Same(Assert.Single(Read(negative).Refinements).Coarse, Assert.Single(Read(positive).Refinements).Coarse);
        var basis = Profile(aligned.Frame, aligned.ColumnPitch); var vertical = Profile(aligned.Frame, aligned.RowPitch);
        Assert.Equal((0, -1), Difference(Profile(negative.Frame, negative.ColumnPitch), basis));
        Assert.Equal((0, 1), Difference(Profile(positive.Frame, positive.ColumnPitch), basis));
        Assert.Equal((1, -1), Difference(Profile(negative.Frame, negative.RowPitch), vertical));
        Assert.Equal((-1, 1), Difference(Profile(positive.Frame, positive.RowPitch), vertical));
        Assert.All(Partitions(negative), part => Assert.Same(part.Contact, E(part.Pieces)[0]));
        Assert.All(Partitions(positive), part => Assert.Same(part.Exposed, E(part.Pieces)[0]));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MissingSeamLawLeavesTheCoincidentContactSubspansAsSeparateParticipations(bool triangle)
    {
        var recipe = Sample(triangle, 1); var run = Run(recipe);
        var joined = new OrnamentProgramField(run, recipe.SeamContract);
        var separate = new OrnamentProgramField(run, OrnamentProgramField.Contract(recipe.Program));
        Assert.True(joined.IsComplete); Assert.True(separate.IsComplete); Assert.Empty(E(separate.Accepted));
        Assert.Equal(E(joined.Graph.Strokes).Length, E(separate.Graph.Strokes).Length);
        Assert.True(E(joined.Graph.Sites).Length < E(separate.Graph.Sites).Length);
        Assert.Equal(0, DuplicatePairs(separate.Graph));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RemovingTheDeclaredNeighbourMoveBlocksOtherwiseAuthenticatedContactRoles(bool triangle)
    {
        var recipe = Sample(triangle, 1); var run = Run(recipe);
        var rules = E((Relation)recipe.SeamContract.Right).Cast<Relation>().Select(rule =>
        {
            var details = (Relation)rule.Right;
            return E((Relation)details.Left).Length == 0 ? rule : new Relation(rule.Left,
                new Relation(OrnamentProgramField.Offset(recipe.Program, 0, 0), details.Right));
        }).ToArray();
        var wrong = new OrnamentProgramField(run, OrnamentProgramField.Contract(recipe.Program, rules));
        Assert.False(wrong.IsComplete); Assert.NotEmpty(E(wrong.Blocked));
        Assert.Equal(E(new OrnamentProgramField(run, recipe.SeamContract).SourceGraph.Strokes).Length, E(wrong.SourceGraph.Strokes).Length);
    }

    private sealed record Partition(Relation Whole, Relation Contact, Relation Exposed, Relation Pieces, Relation Sum, Relation Account);
    private static Partition[] Partitions(BourgoinPatternLibrary.Recipe recipe)
    {
        var motif = Motif(recipe.Program); var read = Read(recipe); var refinement = Assert.Single(read.Refinements);
        var body = (Relation)motif.Right; var tail = (Relation)body.Right;
        return E((Relation)tail.Right).OfType<Relation>().Where(row => row.Left is Relation owner && ReferenceEquals(owner.Left, refinement.Surface))
            .Select(row =>
            {
                var selector = (Relation)((Relation)row.Left).Right; var partsAndReads = (Relation)row.Right;
                var parts = (Relation)partsAndReads.Left; var ordered = (Relation)partsAndReads.Right; var proofs = (Relation)ordered.Right;
                return new Partition((Relation)selector.Right, (Relation)parts.Left, (Relation)parts.Right,
                    (Relation)ordered.Left, (Relation)proofs.Left, (Relation)proofs.Right);
            }).ToArray();
    }
    private static BourgoinPatternLibrary.Recipe Sample(bool triangle, int index) => (triangle ? Triangles.Value : Hexagons.Value)[index];
    private static Relation Motif(OrnamentProgram program) => (Relation)Assert.Single(E(program.Motifs));
    private static BourgoinConstructionRead Read(BourgoinPatternLibrary.Recipe recipe) => new(recipe.Frame, Motif(recipe.Program));
    private static OrnamentProgramRun Run(BourgoinPatternLibrary.Recipe recipe)
    { var phases = (Relation)recipe.Window.Left; return recipe.Program.Generate(recipe.Bindings, OrnamentProgram.Window(phases.Left, phases.Right, 2, 2)); }
    private static OrnamentProgramField Field(BourgoinPatternLibrary.Recipe recipe) => new(Run(recipe), recipe.SeamContract);
    private static (int X, int Y) Difference((int X, int Y) a, (int X, int Y) b) => (a.X - b.X, a.Y - b.Y);
    private static int SideSteps(QualitativeMotionFrame frame, Relation word)
    { var p = Profile(frame, word); Assert.True(p.X == 0 || p.Y == 0 || p.X == -p.Y); return Math.Max(Math.Abs(p.X), Math.Abs(p.Y)); }
    private static (int X, int Y) Profile(QualitativeMotionFrame frame, Relation word)
    {
        var read = frame.Reduce(S(), word); Assert.True(frame.TryReadResult(read, out var remaining, out _, out var pending)); Assert.Empty(E(pending));
        var x = 0; var y = 0; IElement? commonReach = null;
        foreach (var item in E(remaining).Cast<Relation>())
        {
            Assert.True(frame.Direction(item, out var axis, out var forward, out var reach));
            if (commonReach is null) commonReach = reach; else Assert.Same(commonReach, reach);
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
        }
        return (x, y);
    }
    private static int DuplicatePairs(QualitativeStrokeGraph graph)
    {
        var index = new Dictionary<IElement, int>(ReferenceEqualityComparer.Instance);
        foreach (var site in E(graph.Sites)) index.Add(site, index.Count);
        var pairs = new HashSet<(int, int)>(); var duplicate = 0;
        foreach (var row in E(graph.Strokes).Cast<Relation>())
        {
            Assert.True(QualitativeStrokeGraph.TryReadStroke(row, out _, out var from, out var to, out _)); var a = index[from]; var b = index[to];
            if (!pairs.Add(a < b ? (a, b) : (b, a))) duplicate++;
        }
        return duplicate;
    }
    private static bool HasCircuit(QualitativeStrokeGraph graph, int sides, int stepCount)
    {
        var adjacency = new Dictionary<IElement, List<(Relation Edge, IElement To, Relation Word)>>(ReferenceEqualityComparer.Instance);
        foreach (var row in E(graph.Strokes).Cast<Relation>())
        {
            Assert.True(QualitativeStrokeGraph.TryReadStroke(row, out _, out var from, out var to, out var word));
            if (SideSteps(graph.Frame, word) != stepCount) continue;
            Add(from, to, word); Add(to, from, Inverse(graph.Frame, word));
            void Add(IElement a, IElement b, Relation w)
            { if (!adjacency.TryGetValue(a, out var edges)) adjacency.Add(a, edges = []); edges.Add((row, b, w)); }
        }
        foreach (var root in adjacency.Keys)
        {
            var sites = new HashSet<IElement>(ReferenceEqualityComparer.Instance) { root };
            var edges = new HashSet<Relation>(ReferenceEqualityComparer.Instance); var words = new List<Relation>();
            if (Visit(root)) return true;
            bool Visit(IElement current)
            {
                foreach (var edge in adjacency[current])
                {
                    if (edges.Contains(edge.Edge)) continue;
                    if (words.Count == sides - 1)
                    {
                        if (!ReferenceEquals(edge.To, root)) continue;
                        var zero = graph.Frame.Reduce(S(), S(words.Append(edge.Word).SelectMany(E).ToArray()));
                        if (graph.Frame.TryReadResult(zero, out var remainder, out _, out var unknown) && E(remainder).Length == 0 && E(unknown).Length == 0) return true;
                        continue;
                    }
                    if (!sites.Add(edge.To)) continue;
                    edges.Add(edge.Edge); words.Add(edge.Word); if (Visit(edge.To)) return true;
                    words.RemoveAt(words.Count - 1); edges.Remove(edge.Edge); sites.Remove(edge.To);
                }
                return false;
            }
        }
        return false;
    }
    private static Relation Inverse(QualitativeMotionFrame frame, Relation word) => S(E(word).Reverse().Cast<Relation>().Select(move =>
        (IElement)QualitativeMotionFrame.Move(E(frame.Opposites).Cast<Relation>().Single(pair => ReferenceEquals(pair.Left, move.Left)).Right, move.Right)).ToArray());
    private static void AssertZero(QualitativeMotionFrame frame, Relation read)
    { Assert.True(frame.TryReadResult(read, out var result, out _, out var pending)); Assert.Empty(E(result)); Assert.Empty(E(pending)); }
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item))
        { if (!seen.Add(item)) continue; yield return item; if (item is Relation row) { pending.Push(row.Left); pending.Push(row.Right); } }
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
