using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentEnrichmentProgramTests
{
    [Fact]
    public void InstallationRebindsMotifDomainsBindingsAndBothDirectionsOfTheActualSeamCycles()
    {
        var f = new Fixture(); var adapter = new OrnamentEnrichmentProgram(f.Program, f.Bindings, f.Contract, f.Enrichment());
        Assert.Same(f.Program.Layout, adapter.Program.Layout); Assert.Same(f.Program.Transforms, adapter.Program.Transforms);
        var motif = Assert.IsType<Relation>(Assert.Single(E(adapter.Program.Motifs)));
        Assert.Same(adapter.Enrichment.ResultSurface, motif); Assert.Same(f.Motif.Surface, adapter.Enrichment.SourceSurface);
        var parameter = E(adapter.Program.Parameters).Cast<Relation>().Single(row => ReferenceEquals(row.Left, f.MotifSlot));
        var binding = E(adapter.Bindings).Cast<Relation>().Single(row => ReferenceEquals(row.Left, f.MotifSlot));
        Assert.Same(motif, Assert.Single(E((Relation)parameter.Right))); Assert.Same(motif, binding.Right);
        var rules = E((Relation)adapter.SeamContract.Right).Cast<Relation>().ToArray(); Assert.Equal(2, rules.Length);
        for (var index = 0; index < rules.Length; index++)
        {
            var selectors = (Relation)rules[index].Left;
            Assert.Same(motif, ((Relation)selectors.Left).Left); Assert.Same(motif, ((Relation)selectors.Right).Left);
            var step = Assert.IsType<Relation>(Assert.Single(E((Relation)((Relation)rules[index].Right).Left)));
            Assert.Same(adapter.Program.Surface, index == 0 ? step.Left : step.Right);
            Assert.Same(f.UCycle, index == 0 ? step.Right : step.Left);
        }
        var field = new OrnamentProgramField(adapter.Program.Generate(adapter.Bindings, f.Window()), adapter.SeamContract);
        Assert.True(field.IsComplete); Assert.Equal(10, E(field.Graph.Strokes).Length); Assert.Equal(2, E(field.Accepted).Length);
        Assert.Empty(E(field.Blocked)); Assert.Empty(E(field.Unresolved));
    }

    [Fact]
    public void ATransportedPacketReplaysLayersAndFocusWithoutTheAuthoringFactory()
    {
        var f = new Fixture(); var adapter = new OrnamentEnrichmentProgram(f.Program, f.Bindings, f.Contract, f.Enrichment());
        Assert.True(OrnamentFieldPacket.TryLoad(adapter.Save(f.Window()), out var packet, out var unresolved)); Assert.Empty(E(unresolved));
        var actualMotif = Assert.IsType<Relation>(Assert.Single(E(packet.Program.Motifs)));
        Assert.True(OrnamentEnrichment.TryReadEnrichment(packet.Program.Frame, actualMotif, out var decoded));
        Assert.Same(actualMotif, decoded.ResultSurface); Assert.NotSame(f.Motif.Surface, decoded.SourceSurface);
        Assert.Equal(2, E(decoded.Layers).Length); Assert.Equal(2, E(decoded.Result.Paths).Length); Assert.Single(E(decoded.Result.ClosedPaths));
        var layer = Assert.IsType<Relation>(E(decoded.Layers)[1]);
        Assert.True(decoded.TryReadLayer(layer, out var definition, out var selected, out _, out var added, out var anchors));
        Assert.Same(Assert.Single(E(decoded.Source.Paths)), Assert.Single(E(selected))); Assert.Single(E(added)); Assert.Single(E(anchors));
        var focus = decoded.Focus(definition.Left); Assert.True(focus.IsComplete); Assert.Same(layer, Assert.Single(E(focus.Selected))); Assert.Single(E(focus.Complement));
        var field = new OrnamentProgramField(packet.Program.Generate(packet.Bindings, packet.Window), packet.SeamContract);
        Assert.True(field.IsComplete); Assert.Equal(10, E(field.Graph.Strokes).Length); Assert.Equal(2, E(field.Accepted).Length);
    }

    [Fact]
    public void ParsingAWrapperKeepsTheActualSourceDeclarationInInstalledHistory()
    {
        var f = new Fixture();
        var wrapper = new UnitArrangementProgram(f.Frame, "reader wrapper", f.Motif.Paths, f.Motif.Ports, f.Motif.ClosedPaths, f.Motif.Evidence);
        Assert.NotSame(wrapper.Surface, f.Motif.Surface);
        var enrichment = f.Enrichment(wrapper);
        var adapter = new OrnamentEnrichmentProgram(f.Program, f.Bindings, f.Contract, enrichment);
        Assert.Same(f.Motif.Surface, adapter.Enrichment.SourceSurface); Assert.NotSame(enrichment.Surface, adapter.Enrichment.Surface);
        Assert.True(OrnamentEnrichment.TryReadEnrichment(f.Frame, adapter.Enrichment.ResultSurface, out var decoded));
        Assert.Same(f.Motif.Surface, decoded.SourceSurface);
    }

    [Fact]
    public void ForeignOffsetCannotBeSilentlyKeptInTheInstalledProgram()
    {
        var f = new Fixture(); var other = new Fixture();
        var foreign = OrnamentProgramField.Contract(f.Program, OrnamentProgramField.Rule(f.Motif.Surface, f.Identity.Surface,
            f.Motif.Surface, f.Identity.Surface, OrnamentProgramField.Offset(other.Program, 1, 0), f.End, f.Start));
        Assert.Throws<ArgumentException>(() => new OrnamentEnrichmentProgram(f.Program, f.Bindings, foreign, f.Enrichment()));
    }

    [Fact]
    public void RebindingPreservesRepeatedActualOffsetStepAndSharedSelectorIdentities()
    {
        var f = new Fixture(); var selector = new Relation(f.Motif.Surface, f.Identity.Surface);
        var offset = OrnamentProgramField.Offset(f.Program, 2, 0); Assert.Same(E(offset)[0], E(offset)[1]);
        var rule = new Relation(new Relation(selector, selector), new Relation(offset, new Relation(f.End, f.Start)));
        var adapter = new OrnamentEnrichmentProgram(f.Program, f.Bindings, OrnamentProgramField.Contract(f.Program, rule), f.Enrichment());
        var mapped = Assert.IsType<Relation>(Assert.Single(E((Relation)adapter.SeamContract.Right)));
        var selectors = (Relation)mapped.Left; Assert.Same(selectors.Left, selectors.Right);
        var steps = E((Relation)((Relation)mapped.Right).Left); Assert.Equal(2, steps.Length); Assert.Same(steps[0], steps[1]);
        Assert.NotSame(E(offset)[0], steps[0]); Assert.Same(adapter.Program.Surface, ((Relation)steps[0]).Left);
    }

    [Fact]
    public void ALookalikeSourceOrUnretainedLayerCannotAcquireAnEnrichmentRead()
    {
        var f = new Fixture(); var good = f.Enrichment();
        var plain = new UnitArrangementProgram(f.Frame, "ordinary source", good.Result.Paths, good.Result.Ports, good.Result.ClosedPaths);
        Assert.False(OrnamentEnrichment.TryReadEnrichment(f.Frame, plain.Surface, out _));
        var layer = (Relation)E(good.Layers)[1];
        Assert.True(good.TryReadLayer(layer, out var definition, out var selected, out _, out var added, out var anchorReads));
        var falseComplement = S(Assert.Single(E(f.Motif.Paths)));
        var forgedLayer = new Relation(definition, new Relation(selected, new Relation(falseComplement, new Relation(added, anchorReads))));
        var evidence = new Relation(good.SourceSurface, new Relation(good.Definitions, new Relation(S(E(good.Layers)[0], forgedLayer), new Relation(S(), S()))));
        var forged = new UnitArrangementProgram(f.Frame, "invalid carried complement", good.Result.Paths, good.Result.Ports, good.Result.ClosedPaths, evidence);
        Assert.False(OrnamentEnrichment.TryReadEnrichment(f.Frame, forged.Surface, out _));
    }

    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        private readonly IElement reach = new Placeholder();
        public IElement Start { get; } = new Placeholder();
        public IElement End { get; } = new Placeholder();
        public IElement MotifSlot { get; } = new Placeholder();
        private readonly IElement transformSlot = new Placeholder();
        private readonly IElement stepU = new Placeholder();
        private readonly IElement stepV = new Placeholder();
        private readonly IElement uphase = new Placeholder();
        private readonly IElement vphase = new Placeholder();
        public UnitArrangementProgram Motif { get; }
        public QualitativePatternTransform Identity { get; }
        public OrnamentProgram Program { get; }
        public Relation Bindings { get; }
        public Relation Contract { get; }
        public Relation UCycle { get; }
        public Fixture()
        {
            var right = Word(1, 0); var path = new Relation(S(), S(right));
            Motif = new(Frame, "line", S(path), S(new Relation(Start, new Relation(path, right)), new Relation(End, new Relation(path, path))));
            Identity = new(Frame, Frame, S(new[] { Frame.Right, Frame.Left, Frame.Up, Frame.Down }.Select(direction =>
            {
                var move = QualitativeMotionFrame.Move(direction, reach); return (IElement)new Relation(move, S(move));
            }).ToArray()));
            UCycle = new(S(uphase), S(new Relation(uphase, uphase))); var vcycle = new Relation(S(vphase), S(new Relation(vphase, vphase)));
            var vertical = Word(0, 3);
            var parameters = S(new Relation(MotifSlot, S(Motif.Surface)), new Relation(transformSlot, S(Identity.Surface)),
                new Relation(stepU, S(right)), new Relation(stepV, S(vertical)));
            var layout = new Relation(new Relation(UCycle, vcycle), new Relation(new Relation(stepU, stepV),
                S(new Relation(new Relation(uphase, vphase), new Relation(MotifSlot, transformSlot)))));
            Program = new(Frame, S(Motif.Surface), S(Identity.Surface), parameters, layout);
            Bindings = S(new Relation(MotifSlot, Motif.Surface), new Relation(transformSlot, Identity.Surface),
                new Relation(stepU, right), new Relation(stepV, vertical));
            Contract = OrnamentProgramField.Contract(Program,
                OrnamentProgramField.Rule(Motif.Surface, Identity.Surface, Motif.Surface, Identity.Surface, OrnamentProgramField.Offset(Program, 1, 0), End, Start),
                OrnamentProgramField.Rule(Motif.Surface, Identity.Surface, Motif.Surface, Identity.Surface, OrnamentProgramField.Offset(Program, -1, 0), Start, End));
        }
        public Relation Window() => OrnamentProgram.Window(uphase, vphase, 2, 1);
        public OrnamentEnrichment Enrichment(UnitArrangementProgram? source = null)
        {
            source ??= Motif;
            var outline = (Relation)Assert.Single(E(source.Paths)); var anchor = (Relation)E(source.Ports)[0];
            var smallSquare = new Relation(Word(0, 1), S(Word(1, 0), Word(0, 1), Word(-1, 0), Word(0, -1)));
            var detail = new UnitArrangementProgram(Frame, "native square detail", S(smallSquare), S(), S(smallSquare));
            return new(source, S(OrnamentEnrichment.Selection(new Placeholder(), outline),
                OrnamentEnrichment.Layer(new Placeholder(), S(outline), S(detail.Surface),
                    S(OrnamentEnrichment.Attach(anchor, Word(0, 1), smallSquare)))));
        }
        private Relation Word(int x, int y) => S(Enumerable.Repeat<IElement>(QualitativeMotionFrame.Move(x < 0 ? Frame.Left : Frame.Right, reach), Math.Abs(x))
            .Concat(Enumerable.Repeat<IElement>(QualitativeMotionFrame.Move(y < 0 ? Frame.Down : Frame.Up, reach), Math.Abs(y))).ToArray());
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
