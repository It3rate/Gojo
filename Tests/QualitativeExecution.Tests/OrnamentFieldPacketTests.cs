using System.Text.Json.Nodes;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentFieldPacketTests
{
    [Fact]
    public void PacketReplaysBothProgramAndSeamLawInOneFreshIdentityWorld()
    {
        var f = new Fixture(); var json = f.Save();
        Assert.True(OrnamentFieldPacket.TryLoad(json, out var loaded, out var unresolved)); Assert.Empty(E(unresolved));
        Assert.NotSame(f.Program.Surface, loaded.Program.Surface);
        Assert.Same(loaded.Program.Surface, ((Relation)loaded.SeamContract.Left).Left);
        var run = loaded.Program.Generate(loaded.Bindings, loaded.Window);
        var field = new OrnamentProgramField(run, loaded.SeamContract);
        Assert.True(run.IsComplete); Assert.Empty(E(field.Blocked)); Assert.Empty(E(field.Unresolved));
        Assert.Equal(2, run.Units.Count); Assert.Single(E(field.Accepted)); Assert.Single(E(field.Frontier));
        Assert.Equal(3, E(field.Graph.Sites).Length); Assert.Equal(2, E(field.Graph.Strokes).Length);
        Assert.All(Reachable(loaded.Source), element => Assert.True(element is Relation or Placeholder));
        var originalSlots = Reachable(f.Program.Surface).OfType<Placeholder>().ToArray();
        Assert.All(Reachable(loaded.Program.Surface).OfType<Placeholder>(), actual =>
            Assert.DoesNotContain(originalSlots, original => original.SameSlotAs(actual)));
    }

    [Fact]
    public void ImportedSeamLawResamplesANewWindowWithoutTheAuthoringFactory()
    {
        var f = new Fixture(); Assert.True(OrnamentFieldPacket.TryLoad(f.Save(), out var loaded, out _));
        var start = (Relation)loaded.Window.Left;
        var window = OrnamentProgram.Window(start.Left, start.Right, 4, 1);
        var run = loaded.Program.Generate(loaded.Bindings, window);
        var field = new OrnamentProgramField(run, loaded.SeamContract);
        Assert.True(run.IsComplete); Assert.Equal(4, run.Units.Count);
        Assert.Equal(3, E(field.Accepted).Length); Assert.Single(E(field.Frontier));
        Assert.Empty(E(field.Blocked)); Assert.Empty(E(field.Unresolved));
        Assert.Equal(5, E(field.Graph.Sites).Length);
        Assert.Same(loaded.Program.Surface, ((Relation)loaded.SeamContract.Left).Left);
    }

    [Fact]
    public void AForeignSeamLawIsNotAcceptedBecauseItsLabelsOrGeometryLookTheSame()
    {
        var first = new Fixture(); var foreign = new Fixture();
        var json = OrnamentFieldPacket.Save(first.Program, first.Bindings, first.Window, foreign.Contract);
        Assert.True(OrnamentFieldPacket.TryLoad(json, out var loaded, out _));
        var run = loaded.Program.Generate(loaded.Bindings, loaded.Window);
        Assert.True(run.IsComplete);
        var field = new OrnamentProgramField(run, loaded.SeamContract);
        Assert.Empty(E(field.Accepted)); Assert.True(E(field.Blocked).Length + E(field.Unresolved).Length > 0);
        Assert.Equal(4, E(field.Graph.Sites).Length);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"version\":0}")]
    [InlineData("{\"version\":1,\"native\":null}")]
    [InlineData("not json")]
    public void IncompleteTransportDoesNotInventAProgram(string json) =>
        Assert.False(OrnamentFieldPacket.TryLoad(json, out _, out _));

    [Fact]
    public void UnversionedAndMeasuredNodesAreRejectedBeforeExecution()
    {
        var root = JsonNode.Parse(new Fixture().Save())!.AsObject();
        var unversioned = root.DeepClone().AsObject(); unversioned.Remove("version");
        Assert.False(OrnamentFieldPacket.TryLoad(unversioned.ToJsonString(), out _, out _));
        var changed = root.DeepClone().AsObject();
        changed["native"]!["nodes"]!.AsArray()[0]!["kind"] = "carrier";
        Assert.False(OrnamentFieldPacket.TryLoad(changed.ToJsonString(), out _, out _));
        var version = root.DeepClone().AsObject(); version["native"]!["version"] = 2;
        Assert.False(OrnamentFieldPacket.TryLoad(version.ToJsonString(), out _, out _));
    }

    [Fact]
    public void LegacyProgramLoadAlsoPreservesTheParsedDeclarationIdentity()
    {
        var f = new Fixture(); Assert.True(OrnamentProgram.TryLoad(f.Program.Save(f.Bindings, f.Window), out var loaded, out _));
        var reboundWitness = (Relation)loaded.Correspondence.Right;
        var target = (Relation)((Relation)reboundWitness.Right).Left;
        var root = Assert.IsType<Relation>(Assert.Single(E(target)));
        Assert.Same(loaded.Program.Surface, root.Left);
    }

    private sealed class Fixture
    {
        public OrnamentProgram Program { get; }
        public Relation Bindings { get; }
        public Relation Window { get; }
        public Relation Contract { get; }
        public Fixture()
        {
            var frame = new QualitativeMotionFrame(); IElement reach = new Placeholder();
            Relation W(IElement direction) => S(QualitativeMotionFrame.Move(direction, reach));
            var chord = W(frame.Right); var path = new Relation(S(), S(chord));
            IElement from = new Placeholder(), to = new Placeholder();
            var motif = new UnitArrangementProgram(frame, "one passage", S(path),
                S(new Relation(from, new Relation(path, chord)), new Relation(to, new Relation(path, path))), S());
            var transform = new QualitativePatternTransform(frame, frame,
                S(new Relation(QualitativeMotionFrame.Move(frame.Right, reach), W(frame.Right))));
            IElement u = new Placeholder(), v = new Placeholder(), motifSlot = new Placeholder(), transformSlot = new Placeholder(),
                stepU = new Placeholder(), stepV = new Placeholder();
            var parameters = S(new Relation(motifSlot, S(motif.Surface)), new Relation(transformSlot, S(transform.Surface)),
                new Relation(stepU, S(chord)), new Relation(stepV, S(W(frame.Up))));
            var layout = new Relation(new Relation(new Relation(S(u), S(new Relation(u, u))), new Relation(S(v), S(new Relation(v, v)))),
                new Relation(new Relation(stepU, stepV), S(new Relation(new Relation(u, v), new Relation(motifSlot, transformSlot)))));
            Program = new(frame, S(motif.Surface), S(transform.Surface), parameters, layout);
            Bindings = S(new Relation(motifSlot, motif.Surface), new Relation(transformSlot, transform.Surface),
                new Relation(stepU, chord), new Relation(stepV, E((Relation)((Relation)E(parameters)[3]).Right)[0]));
            Window = OrnamentProgram.Window(u, v, 2, 1);
            Contract = OrnamentProgramField.Contract(Program, OrnamentProgramField.Rule(motif.Surface, transform.Surface,
                motif.Surface, transform.Surface, OrnamentProgramField.Offset(Program, 1, 0), to, from));
        }
        public string Save() => OrnamentFieldPacket.Save(Program, Bindings, Window, Contract);
    }

    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var element))
        {
            if (!seen.Add(element)) continue; yield return element;
            if (element is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
    private static Relation S(params IElement[] elements) => StructuralConstraint.Closed(elements);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
