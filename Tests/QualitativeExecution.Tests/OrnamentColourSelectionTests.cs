using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentColourSelectionTests
{
    [Fact]
    public void FourOrderedGroupsRetainActualPathsAndCarryTheUnselectedComplement()
    {
        var f = new Fixture(); var selection = new OrnamentColourSelection(f.Unit, f.Groups);
        Assert.True(selection.IsComplete); Assert.Equal(4, E(selection.Groups).Length);
        for (var index = 0; index < 4; index++) Assert.Equal(index, selection.GroupIndex(f.Paths[index]));
        Assert.Equal(-1, selection.GroupIndex(f.Paths[4]));
        Assert.Equal(-1, selection.GroupIndex(new Relation(f.Paths[0].Left, f.Paths[0].Right)));
        Assert.Same(f.Paths[4], Assert.Single(E(selection.Complement)));
        Assert.True(OrnamentColourSelection.TryRead(f.Frame, f.Unit.Surface, selection.Surface, out var read));
        Assert.Same(selection.Surface, read.Surface); Assert.Same(f.Unit.Surface, read.SourceSurface);
        Assert.False(OrnamentColourSelection.TryRead(f.Frame, new Relation(f.Unit.Surface.Left, f.Unit.Surface.Right), selection.Surface, out _));
        var partial = new OrnamentColourSelection(f.Unit, S(S(f.Paths[0]), S(), S(f.Paths[2]), S()));
        Assert.True(partial.IsComplete); Assert.Equal(new[] { f.Paths[1], f.Paths[3], f.Paths[4] }, E(partial.Complement));
    }

    [Fact]
    public void OverlapCopiedForeignAndOpenGroupsRemainBlockedOrUnresolved()
    {
        var f = new Fixture();
        foreach (var requested in new[] {
            S(S(f.Paths[0]), S(f.Paths[0]), S(), S()),
            S(S(f.Paths[0], f.Paths[0]), S(), S(), S()),
            S(S(new Relation(f.Paths[0].Left, f.Paths[0].Right)), S(), S(), S()),
            S(S(new Placeholder()), S(), S(), S()),
            S(S(f.Paths[0]), S(), S()),
            S(StructuralConstraint.Open(f.Paths[0]), S(), S(), S()),
            StructuralConstraint.Open(S(f.Paths[0]), S(), S(), S())
        })
        {
            var selection = new OrnamentColourSelection(f.Unit, requested);
            Assert.False(selection.IsComplete); Assert.Empty(E(selection.Groups));
            Assert.False(OrnamentColourSelection.TryRead(f.Frame, f.Unit.Surface, selection.Surface, out _));
        }
    }

    [Fact]
    public void ExactReaderRefusesForgedOutputGroupsAndMissingComplement()
    {
        var f = new Fixture(); var good = new OrnamentColourSelection(f.Unit, f.Groups);
        var body = (Relation)good.Surface.Right; var rest = (Relation)body.Right;
        var swapped = new Relation(good.Surface.Left, new Relation(S(S(f.Paths[1]), S(f.Paths[0]), S(f.Paths[2]), S(f.Paths[3])), rest));
        var missing = new Relation(good.Surface.Left, new Relation(body.Left, new Relation(S(), rest.Right)));
        Assert.False(OrnamentColourSelection.TryRead(f.Frame, f.Unit.Surface, swapped, out _));
        Assert.False(OrnamentColourSelection.TryRead(f.Frame, f.Unit.Surface, missing, out _));
        var curve = new OrnamentCurveSelection(f.Unit, S(f.Paths[0]), S(f.Corner));
        Assert.False(OrnamentColourSelection.TryRead(f.Frame, f.Unit.Surface, curve.Surface, out _));
        Assert.False(OrnamentCurveSelection.TryRead(f.Frame, f.Unit.Surface, good.Surface, out _));
    }

    [Fact]
    public void PacketImportsColourAndCurveSelectionsThroughTheActualProgramIdentityGraph()
    {
        var f = new Fixture(); var colour = new OrnamentColourSelection(f.Unit, f.Groups);
        var curve = new OrnamentCurveSelection(f.Unit, S(f.Paths[0], f.Paths[1]), S(f.Corner));
        var json = OrnamentFieldPacket.Save(f.Program, f.Bindings, f.Window, f.Contract, S(curve.Surface, colour.Surface));
        using var document = JsonDocument.Parse(json);
        Assert.Equal(2, document.RootElement.GetProperty("version").GetInt32());
        Assert.Equal(0, document.RootElement.GetProperty("native").GetProperty("carriers").GetArrayLength());
        Assert.True(OrnamentFieldPacket.TryLoad(json, out var packet, out var pending)); Assert.Empty(E(pending));
        var actual = Assert.IsType<Relation>(Assert.Single(E(packet.Program.Motifs)));
        var hints = E(Assert.IsType<Relation>(packet.PresentationHints)); Assert.Equal(2, hints.Length);
        Assert.True(OrnamentCurveSelection.TryRead(packet.Program.Frame, actual, Assert.IsType<Relation>(hints[0]), out var importedCurve));
        Assert.True(OrnamentColourSelection.TryRead(packet.Program.Frame, actual, Assert.IsType<Relation>(hints[1]), out var importedColour));
        Assert.NotSame(f.Unit.Surface, importedColour.SourceSurface); Assert.Same(actual, importedColour.SourceSurface);
        var declarations = (Relation)((Relation)actual.Right).Left; var importedPaths = E((Relation)declarations.Left).Cast<Relation>().ToArray();
        for (var index = 0; index < 4; index++) Assert.Equal(index, importedColour.GroupIndex(importedPaths[index]));
        Assert.Same(importedPaths[4], Assert.Single(E(importedColour.Complement)));
        Assert.True(importedCurve.Contains(importedPaths[0])); Assert.Equal(new[] { 1 }, importedCurve.FixedLandmarks(importedPaths[0]));
        Assert.Equal(-1, importedColour.GroupIndex(f.Paths[0]));
        var field = new OrnamentProgramField(packet.Program.Generate(packet.Bindings, packet.Window), packet.SeamContract);
        Assert.True(field.IsComplete); Assert.Equal(10, E(field.Graph.Strokes).Length);
    }

    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        private readonly IElement reach = new Placeholder();
        public Relation[] Paths { get; }
        public Relation Corner { get; }
        public UnitArrangementProgram Unit { get; }
        public Relation Groups { get; }
        public OrnamentProgram Program { get; }
        public Relation Bindings { get; }
        public Relation Window { get; }
        public Relation Contract { get; }
        public Fixture()
        {
            Paths = Enumerable.Range(0, 5).Select(index => new Relation(Word(index * 4, 0), S(Word(2, 0), Word(0, 2)))).ToArray();
            Corner = new(new Placeholder(), new Relation(Paths[0], E((Relation)Paths[0].Right)[1]));
            Unit = new(Frame, "actual palette guides", S(Paths), S(Corner));
            Groups = S(Paths.Take(4).Select(path => (IElement)S(path)).ToArray());
            var identity = new QualitativePatternTransform(Frame, Frame, S(new[] { Frame.Right, Frame.Left, Frame.Up, Frame.Down }.Select(direction =>
            { var move = QualitativeMotionFrame.Move(direction, reach); return (IElement)new Relation(move, S(move)); }).ToArray()));
            IElement m = new Placeholder(), t = new Placeholder(), cu = new Placeholder(), rv = new Placeholder(), u = new Placeholder(), v = new Placeholder();
            var column = Word(24, 0); var row = Word(0, 8); var motifs = S(Unit.Surface); var transforms = S(identity.Surface);
            var uc = new Relation(S(u), S(new Relation(u, u))); var vc = new Relation(S(v), S(new Relation(v, v)));
            Program = new(Frame, motifs, transforms, S(new Relation(m, motifs), new Relation(t, transforms), new Relation(cu, S(column)), new Relation(rv, S(row))),
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
