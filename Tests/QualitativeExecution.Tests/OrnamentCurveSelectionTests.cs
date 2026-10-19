using System.Text.Json;
using System.Text.Json.Nodes;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentCurveSelectionTests
{
    [Fact]
    public void NativeSelectionRetainsActualPathsComplementAndFixedPublicLandmarks()
    {
        var f = new Fixture(); var selection = new OrnamentCurveSelection(f.Unit, S(f.First), S(f.Corner));
        Assert.True(selection.IsComplete); Assert.Same(f.First, Assert.Single(E(selection.Selected)));
        Assert.Same(f.Second, Assert.Single(E(selection.Complement))); Assert.Same(f.Corner, Assert.Single(E(selection.FixedPorts)));
        Assert.Equal(new[] { 1 }, selection.FixedLandmarks(f.First));
        Assert.True(OrnamentCurveSelection.TryRead(f.Frame, f.Unit.Surface, selection.Surface, out var read));
        Assert.Same(selection.Surface, read.Surface); Assert.True(read.Contains(f.First)); Assert.False(read.Contains(f.Second));
        Assert.False(OrnamentCurveSelection.TryRead(f.Frame, new Relation(f.Unit.Surface.Left, f.Unit.Surface.Right), selection.Surface, out _));
    }

    [Fact]
    public void ForeignCopiedDuplicateAndOpenSelectionsCannotBorrowActualGuideIdentity()
    {
        var f = new Fixture();
        foreach (var selection in new[] {
            new OrnamentCurveSelection(f.Unit, S(new Relation(f.First.Left, f.First.Right))),
            new OrnamentCurveSelection(f.Unit, S(f.First, f.First)),
            new OrnamentCurveSelection(f.Unit, S(f.First), S(new Relation(f.Corner.Left, f.Corner.Right))),
            new OrnamentCurveSelection(f.Unit, S(f.Second), S(f.Corner)),
            new OrnamentCurveSelection(f.Unit, StructuralConstraint.Open(f.First))
        })
        {
            Assert.False(selection.IsComplete);
            Assert.False(OrnamentCurveSelection.TryRead(f.Frame, f.Unit.Surface, selection.Surface, out _));
        }
        var good = new OrnamentCurveSelection(f.Unit, S(f.First), S(f.Corner)); var body = (Relation)good.Surface.Right;
        var rest = (Relation)body.Right;
        var forged = new Relation(good.Surface.Left, new Relation(body.Left, new Relation(S(), rest.Right)));
        Assert.False(OrnamentCurveSelection.TryRead(f.Frame, f.Unit.Surface, forged, out _));
    }

    [Fact]
    public void VersionTwoPacketImportsHintsAndProgramThroughTheSameIdentityGraph()
    {
        var f = new Fixture(); var selection = new OrnamentCurveSelection(f.Unit, S(f.First), S(f.Corner));
        var json = OrnamentFieldPacket.Save(f.Program, f.Bindings, f.Window, f.Contract, S(selection.Surface));
        using var document = JsonDocument.Parse(json); Assert.Equal(2, document.RootElement.GetProperty("version").GetInt32());
        Assert.Equal(0, document.RootElement.GetProperty("native").GetProperty("carriers").GetArrayLength());
        Assert.True(OrnamentFieldPacket.TryLoad(json, out var packet, out var pending)); Assert.Empty(E(pending));
        var actual = Assert.IsType<Relation>(Assert.Single(E(packet.Program.Motifs)));
        var hint = Assert.IsType<Relation>(Assert.Single(E(Assert.IsType<Relation>(packet.PresentationHints))));
        Assert.True(OrnamentCurveSelection.TryRead(packet.Program.Frame, actual, hint, out var imported));
        Assert.Same(actual, imported.SourceSurface); Assert.NotSame(f.Unit.Surface, imported.SourceSurface);
        var declarations = (Relation)((Relation)actual.Right).Left;
        Assert.Same(E((Relation)declarations.Left)[0], Assert.Single(E(imported.Selected)));
        Assert.Same(E((Relation)declarations.Right)[1], Assert.Single(E(imported.FixedPorts)));
        Assert.Equal(new[] { 1 }, imported.FixedLandmarks(Assert.IsType<Relation>(Assert.Single(E(imported.Selected)))));
        var field = new OrnamentProgramField(packet.Program.Generate(packet.Bindings, packet.Window), packet.SeamContract);
        Assert.True(field.IsComplete); Assert.Equal(6, E(field.Graph.Strokes).Length);
    }

    [Fact]
    public void OriginalPacketsRemainVersionOneAndDoNotInventPresentationHints()
    {
        var f = new Fixture(); var json = OrnamentFieldPacket.Save(f.Program, f.Bindings, f.Window, f.Contract);
        using var document = JsonDocument.Parse(json); Assert.Equal(1, document.RootElement.GetProperty("version").GetInt32());
        Assert.True(OrnamentFieldPacket.TryLoad(json, out var packet, out _)); Assert.Null(packet.PresentationHints);
        var malformed = JsonNode.Parse(json)!; malformed["version"] = 2;
        Assert.False(OrnamentFieldPacket.TryLoad(malformed.ToJsonString(), out _, out _));
    }

    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        private readonly IElement reach = new Placeholder();
        public Relation First { get; }
        public Relation Second { get; }
        public Relation Corner { get; }
        public UnitArrangementProgram Unit { get; }
        public OrnamentProgram Program { get; }
        public Relation Bindings { get; }
        public Relation Window { get; }
        public Relation Contract { get; }
        public Fixture()
        {
            var right = Word(2, 0); var up = Word(0, 2);
            First = new(S(), S(right, up));
            Second = new(Word(4, 0), S(Word(2, 0), Word(0, 2), Word(-2, 0), Word(0, -2)));
            Corner = new(new Placeholder(), new Relation(First, up));
            Unit = new(Frame, "actual guides", S(First, Second), S(new Relation(new Placeholder(), new Relation(First, right)), Corner), S(Second));
            var identity = new QualitativePatternTransform(Frame, Frame, S(new[] { Frame.Right, Frame.Left, Frame.Up, Frame.Down }.Select(direction =>
            { var move = QualitativeMotionFrame.Move(direction, reach); return (IElement)new Relation(move, S(move)); }).ToArray()));
            IElement m = new Placeholder(), t = new Placeholder(), cu = new Placeholder(), rv = new Placeholder(), u = new Placeholder(), v = new Placeholder();
            var column = Word(8, 0); var row = Word(0, 8); var motifs = S(Unit.Surface); var transforms = S(identity.Surface);
            var uc = new Relation(S(u), S(new Relation(u, u))); var vc = new Relation(S(v), S(new Relation(v, v)));
            Program = new(Frame, motifs, transforms, S(new Relation(m, motifs), new Relation(t, transforms),
                new Relation(cu, S(column)), new Relation(rv, S(row))),
                new Relation(new Relation(uc, vc), new Relation(new Relation(cu, rv), S(new Relation(new Relation(u, v), new Relation(m, t))))));
            Bindings = S(new Relation(m, Unit.Surface), new Relation(t, identity.Surface), new Relation(cu, column), new Relation(rv, row));
            Window = OrnamentProgram.Window(u, v, 1, 1); Contract = OrnamentProgramField.Contract(Program);
        }
        private Relation Word(int x, int y) => S(Enumerable.Repeat<IElement>(QualitativeMotionFrame.Move(x < 0 ? Frame.Left : Frame.Right, reach), Math.Abs(x))
            .Concat(Enumerable.Repeat<IElement>(QualitativeMotionFrame.Move(y < 0 ? Frame.Down : Frame.Up, reach), Math.Abs(y))).ToArray());
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
