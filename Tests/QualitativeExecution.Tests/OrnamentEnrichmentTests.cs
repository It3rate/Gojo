using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentEnrichmentTests
{
    [Fact]
    public void FocusCarriesSiblingsAndOverlappingSourceMembershipWithoutConsumingThePattern()
    {
        var f = new Fixture(); var overall = new Placeholder(); var middle = new Placeholder(); var detail = new Placeholder();
        var enrichment = new OrnamentEnrichment(f.Source, S(
            OrnamentEnrichment.Selection(overall, f.Square, f.Stem),
            OrnamentEnrichment.Selection(middle, f.Square),
            OrnamentEnrichment.Selection(detail, f.Stem)));
        Assert.True(enrichment.IsComplete); Assert.Equal(3, E(enrichment.Layers).Length);
        Assert.Same(f.Square, E(enrichment.Result.Paths)[0]); Assert.Same(f.Stem, E(enrichment.Result.Paths)[1]);
        var first = enrichment.Focus(overall); var second = enrichment.Focus(detail);
        Assert.True(first.IsComplete); Assert.True(second.IsComplete);
        Assert.Single(E(first.Selected)); Assert.Equal(2, E(first.Complement).Length);
        Assert.Same(enrichment.Surface, ((Relation)first.Surface.Left).Left);
        Assert.Same(enrichment.Surface, ((Relation)second.Surface.Left).Left);
        Assert.True(enrichment.TryReadLayer((Relation)E(first.Selected)[0], out var definition, out var selected, out var complement, out var additions, out var anchors));
        Assert.Same(overall, definition.Left); Assert.Equal(2, E(selected).Length); Assert.Empty(E(complement)); Assert.Empty(E(additions)); Assert.Empty(E(anchors));
        Assert.True(enrichment.TryReadLayer((Relation)E(second.Selected)[0], out _, out selected, out complement, out _, out _));
        Assert.Same(f.Stem, Assert.Single(E(selected))); Assert.Same(f.Square, Assert.Single(E(complement)));
        Assert.Equal(2, E(enrichment.Result.Paths).Length); Assert.Single(E(enrichment.Result.ClosedPaths));
    }

    [Fact]
    public void DecorationKeepsActualPublishedAnchorOffsetAndNativeDisplacementWitness()
    {
        var f = new Fixture();
        Assert.True(OrnamentEnrichment.TryAnchorWord(f.Source, f.CornerPort, out var corner, out var prefixRead));
        AssertZero(f.Frame, f.Frame.Reduce(corner, f.Word(-4, -4)));
        Assert.Same(f.Frame.Surface, ((Relation)prefixRead.Left).Left);
        var offset = f.Word(1, -1); var start = f.Word(5, 3);
        var polygon = f.Path(start, f.Word(1, 0), f.Word(0, 1), f.Word(-1, 0), f.Word(0, -1));
        var decoration = new UnitArrangementProgram(f.Frame, "small native contour", S(polygon), S(), S(polygon));
        var attachment = OrnamentEnrichment.Attach(f.CornerPort, offset, polygon);
        var identity = new Placeholder();
        var enrichment = new OrnamentEnrichment(f.Source, S(OrnamentEnrichment.Layer(identity, S(f.Square), S(decoration.Surface), S(attachment))));
        Assert.True(enrichment.IsComplete); Assert.Equal(3, E(enrichment.Result.Paths).Length); Assert.Equal(2, E(enrichment.Result.ClosedPaths).Length);
        Assert.Same(polygon, E(enrichment.Result.Paths)[2]); Assert.Same(f.Source.Surface, ((Relation)enrichment.Result.Evidence).Left);
        var layer = Assert.IsType<Relation>(Assert.Single(E(enrichment.Layers)));
        Assert.True(enrichment.TryReadLayer(layer, out _, out _, out _, out var added, out var witnesses));
        Assert.Same(polygon, Assert.Single(E(added)));
        var witness = Assert.IsType<Relation>(Assert.Single(E(witnesses)));
        Assert.Same(attachment, witness.Left);
        AssertZero(f.Frame, (Relation)((Relation)witness.Right).Right);
        // Native closure declarations survive. Neither closure nor the anchor
        // equality claims an occupied area or a physical site join.
        var unit = new UnitArrangementNode("enriched", [enrichment.Result], enrichment.Result, S());
        Assert.Equal(9, E(unit.Current.Graph.Strokes).Length);
        AssertNoNumeric(enrichment.Surface);
    }

    [Fact]
    public void ALookalikePortOrUnanchoredAdditionCannotAcquirePublishedParticipation()
    {
        var f = new Fixture(); var path = f.Path(f.Word(4, 4), f.Word(1, 0));
        var program = new UnitArrangementProgram(f.Frame, "unanchored", S(path), S());
        var copiedPort = new Relation(f.CornerPort.Left, f.CornerPort.Right);
        Assert.False(OrnamentEnrichment.TryAnchorWord(f.Source, copiedPort, out _, out _));
        var missing = new OrnamentEnrichment(f.Source, S(OrnamentEnrichment.Layer(new Placeholder(), S(f.Square), S(program.Surface), S())));
        var lookalike = new OrnamentEnrichment(f.Source, S(OrnamentEnrichment.Layer(new Placeholder(), S(f.Square), S(program.Surface),
            S(OrnamentEnrichment.Attach(copiedPort, S(), path)))));
        foreach (var result in new[] { missing, lookalike })
        {
            Assert.False(result.IsComplete); Assert.Single(E(result.Blocked)); Assert.Empty(E(result.Layers));
            Assert.Equal(2, E(result.Result.Paths).Length); Assert.Same(f.Square, E(result.Result.Paths)[0]);
        }
    }

    [Fact]
    public void MismatchedOffsetBlocksTheWholeAdditionAndKeepsTheSourceUnchanged()
    {
        var f = new Fixture(); var path = f.Path(f.Word(5, 4), f.Word(1, 0));
        var program = new UnitArrangementProgram(f.Frame, "misplaced", S(path), S());
        var enrichment = new OrnamentEnrichment(f.Source, S(OrnamentEnrichment.Layer(new Placeholder(), S(f.Square), S(program.Surface),
            S(OrnamentEnrichment.Attach(f.CornerPort, S(), path)))));
        Assert.False(enrichment.IsComplete); Assert.Single(E(enrichment.Blocked)); Assert.Empty(E(enrichment.Layers));
        Assert.Equal(2, E(enrichment.Result.Paths).Length); Assert.Equal(2, E(f.Source.Paths).Length);
    }

    [Fact]
    public void CopiedPathCannotEnterSelectionAndUnknownFocusRemainsOpen()
    {
        var f = new Fixture(); var copied = new Relation(f.Square.Left, f.Square.Right);
        var invalid = new OrnamentEnrichment(f.Source, S(OrnamentEnrichment.Selection(new Placeholder(), copied)));
        Assert.Single(E(invalid.Blocked)); Assert.Empty(E(invalid.Layers));
        var enrichment = new OrnamentEnrichment(f.Source, S(OrnamentEnrichment.Selection(new Placeholder(), f.Square)));
        var open = enrichment.Focus(new Placeholder());
        Assert.False(open.IsComplete); Assert.Empty(E(open.Selected)); Assert.Single(E(open.Complement)); Assert.Single(E(open.Unresolved));
        Assert.False(enrichment.TryReadLayer(new Relation(E(enrichment.Layers)[0], S()), out _, out _, out _, out _, out _));
    }

    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        private readonly IElement reach = new Placeholder();
        public Relation Square { get; }
        public Relation Stem { get; }
        public Relation CornerPort { get; }
        public UnitArrangementProgram Source { get; }
        public Fixture()
        {
            var right = Word(4, 0); var up = Word(0, 4); var left = Word(-4, 0); var down = Word(0, -4);
            Square = Path(S(), right, up, left, down); Stem = Path(Word(2, 2), Word(0, 1));
            CornerPort = new(new Placeholder(), new Relation(Square, left));
            Source = new(Frame, "two actual source paths", S(Square, Stem), S(CornerPort), S(Square));
        }
        public Relation Path(Relation start, params Relation[] chords) => new(start, S(chords));
        public Relation Word(int x, int y) => S(Enumerable.Repeat<IElement>(QualitativeMotionFrame.Move(x < 0 ? Frame.Left : Frame.Right, reach), Math.Abs(x))
            .Concat(Enumerable.Repeat<IElement>(QualitativeMotionFrame.Move(y < 0 ? Frame.Down : Frame.Up, reach), Math.Abs(y))).ToArray());
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static void AssertZero(QualitativeMotionFrame frame, Relation read)
    {
        Assert.True(frame.TryReadResult(read, out var remainder, out _, out var unresolved)); Assert.Empty(E(remainder)); Assert.Empty(E(unresolved));
    }
    private static void AssertNoNumeric(IElement source)
    {
        var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(source);
        while (pending.TryPop(out var element))
        {
            if (!visited.Add(element)) continue;
            Assert.False(element is Carrier or CarrierLocation);
            if (element is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
