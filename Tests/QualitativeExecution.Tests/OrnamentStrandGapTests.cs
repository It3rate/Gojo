using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentStrandGapTests
{
    [Fact]
    public void ActualDistinctStrandEndsRetainANonzeroGapWithoutAnImplicitJunction()
    {
        var f = new Fixture(); var gap = new OrnamentStrandGap(f.Program, f.End, f.Start);
        Assert.True(gap.IsComplete); Assert.True(OrnamentStrandGap.TryRead(f.Frame, gap.Surface, out var read));
        Assert.Same(gap.Surface, read.Surface); Assert.Same(f.Program.Surface, read.SourceSurface);
        Assert.Same(f.End, read.FromPort); Assert.Same(f.Start, read.ToPort);
        Assert.True(f.Frame.TryReadResult(read.Difference, out var displacement, out _, out var unresolved));
        Assert.Equal(2, E(displacement).Length); Assert.Empty(E(unresolved));
        Assert.All(E(displacement).Cast<Relation>(), move => Assert.Same(f.Frame.Right, move.Left));
        var node = new UnitArrangementNode("three independent traces", [f.Program], f.Program, S());
        var summary = node.Current.Join.Graph.Describe(node.Current.Join.Graph.Evaluate());
        Assert.Equal(3, summary.Strokes); Assert.Equal(6, summary.Endpoints); Assert.Equal(0, summary.X);
        Assert.Empty(E(node.Current.Join.Accepted));
        // The crossing strand passes through the apparent interruption in the
        // optional display. No native vertex or hidden connecting edge is added.
    }

    [Fact]
    public void ZeroGapSamePathForeignAndCopiedPortsCannotReceiveTheClaim()
    {
        var f = new Fixture(); var zero = new Fixture(-1);
        var copied = new Relation(f.End.Left, f.End.Right);
        var samePathStart = (Relation)E(f.Program.Ports)[0];
        foreach (var gap in new[] {
            new OrnamentStrandGap(zero.Program, zero.End, zero.Start),
            Gap(new Fixture(closedFirst: true)),
            Gap(new Fixture(closedSecond: true)),
            new OrnamentStrandGap(f.Program, f.End, samePathStart),
            new OrnamentStrandGap(f.Program, copied, f.Start),
            new OrnamentStrandGap(f.Program, f.End, new Fixture().Start),
            new OrnamentStrandGap(f.Program, samePathStart, f.Start)
        })
        {
            Assert.False(gap.IsComplete); Assert.NotEmpty(E(gap.Blocked));
            Assert.False(OrnamentStrandGap.TryRead(gap.Frame, gap.Surface, out _));
        }
        static OrnamentStrandGap Gap(Fixture fixture) => new(fixture.Program, fixture.End, fixture.Start);
    }

    [Fact]
    public void NativeWitnessesCannotBeReplacedByFavourableResultsOrUnrelatedSources()
    {
        var f = new Fixture(); var gap = new OrnamentStrandGap(f.Program, f.End, f.Start);
        var body = (Relation)gap.Surface.Right; var remainder = (Relation)body.Right;
        var forged = new Relation(gap.Surface.Left, new Relation(body.Left,
            new Relation(f.Frame.Reduce(S(), S()), remainder.Right)));
        Assert.False(OrnamentStrandGap.TryRead(f.Frame, forged, out _));
        var unrelated = new Fixture();
        var foreignSource = new Relation(new Relation(unrelated.Program.Surface, new Relation(f.End, f.Start)), gap.Surface.Right);
        Assert.False(OrnamentStrandGap.TryRead(f.Frame, foreignSource, out _));
        Assert.False(OrnamentStrandGap.TryRead(unrelated.Frame, gap.Surface, out _));
        Assert.True(OrnamentStrandGap.TryRead(f.Frame, gap.Surface, out _));
    }

    [Fact]
    public void ADecoratedProgramRetainsItsActualGapContractAcrossNativeArchiveImport()
    {
        var f = new Fixture(); var gap = new OrnamentStrandGap(f.Program, f.End, f.Start);
        var decorated = new UnitArrangementProgram(f.Frame, "actual interrupted-strand construction", f.Program.Paths,
            f.Program.Ports, f.Program.ClosedPaths, S(gap.Surface));
        var archive = GojoStructureCompactJson.FromElement("strand-gap-study", decorated.Surface);
        Assert.Empty(archive.Carriers); Assert.All(archive.Nodes, node => Assert.Contains(node.Kind, new[] { "relation", "placeholder" }));
        Assert.True(GojoStructureCompactJson.TryToElement(archive, out var loaded));
        var source = Assert.IsType<Relation>(loaded.Root);
        Assert.True(QualitativeMotionFrameRead.TryReadStandard((Relation)source.Left, out var frameRead));
        Assert.True(frameRead.TryRebind(source, out var rebound, out _));
        var actual = Assert.IsType<Relation>(rebound);
        var data = (Relation)actual.Right; var roles = (Relation)data.Left; var history = (Relation)data.Right;
        var evidence = (Relation)history.Right;
        Assert.True(OrnamentStrandGap.TryRead(frameRead.Target, Assert.IsType<Relation>(Assert.Single(E(evidence))), out var read));
        Assert.NotSame(f.Program.Surface, read.SourceSurface);
        Assert.Contains(E((Relation)roles.Right), port => ReferenceEquals(port, read.FromPort));
        Assert.Contains(E((Relation)roles.Right), port => ReferenceEquals(port, read.ToPort));
        // These actual paths and port rows are shared between the executed
        // declaration and its retained source; a factory or sidecar is absent.
        var retained = (Relation)((Relation)read.SourceSurface.Right).Left;
        Assert.Same(roles.Left, retained.Left); Assert.Same(roles.Right, retained.Right);
    }

    [Fact]
    public void ImportedGuillocheGapSelectsTheActualRenderedGuidesAndTheirPlacedPublications()
    {
        var study = Assert.Single(OrnamentDetailLibrary.Create(["D09"])); var design = study.Design;
        var source = design.Source;
        var installed = new OrnamentEnrichmentProgram(source.Program, source.Bindings, source.SeamContract, design.Enrichment);
        var start = (Relation)source.Window.Left;
        Assert.True(OrnamentFieldPacket.TryLoad(installed.Save(OrnamentProgram.Window(start.Left, start.Right, 1, 1)),
            out var packet, out var unresolved)); Assert.Empty(E(unresolved));
        var motif = Assert.IsType<Relation>(Assert.Single(E(packet.Program.Motifs)));
        Assert.True(OrnamentEnrichment.TryReadEnrichment(packet.Program.Frame, motif, out var enrichment));
        var gapSurface = Assert.IsType<Relation>(Assert.Single(E((Relation)enrichment.Source.Evidence)));
        Assert.True(OrnamentStrandGap.TryRead(packet.Program.Frame, gapSurface, out var gap));
        Assert.Contains(E(enrichment.Source.Ports), port => ReferenceEquals(port, gap.FromPort));
        Assert.Contains(E(enrichment.Source.Ports), port => ReferenceEquals(port, gap.ToPort));
        var firstPath = ((Relation)gap.FromPort.Right).Left; var secondPath = ((Relation)gap.ToPort.Right).Left;
        Assert.Contains(E(enrichment.Source.Paths), path => ReferenceEquals(path, firstPath));
        Assert.Contains(E(enrichment.Source.Paths), path => ReferenceEquals(path, secondPath));
        var run = packet.Program.Generate(packet.Bindings, packet.Window); Assert.True(run.IsComplete);
        var field = new OrnamentProgramField(run, packet.SeamContract); Assert.True(field.IsComplete);
        var unit = Assert.Single(run.Units);
        Assert.True(UnitArrangementConnection.TryReadPublication(unit, gap.FromPort.Left, out _, out var from));
        Assert.True(UnitArrangementConnection.TryReadPublication(unit, gap.ToPort.Left, out _, out var to));
        Assert.NotSame(from.Left, to.Left);
        var frame = packet.Program.Frame;
        var inverse = S(E((Relation)from.Right).Reverse().Cast<Relation>().Select(move =>
        {
            var direction = E(frame.Opposites).Cast<Relation>().Single(pair => ReferenceEquals(pair.Left, move.Left)).Right;
            return (IElement)QualitativeMotionFrame.Move(direction, move.Right);
        }).ToArray());
        var read = frame.Reduce((Relation)to.Right, inverse);
        Assert.True(frame.TryReadResult(read, out var gapWord, out _, out var pending)); Assert.Empty(E(pending));
        Assert.Equal(4, E(gapWord).Cast<Relation>().Count(move => ReferenceEquals(move.Left, frame.Right)));
        Assert.Equal(6, E(gapWord).Cast<Relation>().Count(move => ReferenceEquals(move.Left, frame.Up)));
        // The claim is over the actual source guides and their exact placed
        // counterparts. It is not an unattached prototype or screen sidecar.
    }

    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        private readonly IElement reach = new Placeholder();
        public UnitArrangementProgram Program { get; }
        public Relation End { get; }
        public Relation Start { get; }
        public Fixture(int secondStart = 1, bool closedFirst = false, bool closedSecond = false)
        {
            var firstWord = Word(2, 0); var lastWord = Word(2, 0); var crossWord = Word(0, 4);
            var first = new Relation(Word(-3, 0), closedFirst ? S(firstWord, Word(-2, 0)) : S(firstWord));
            var last = new Relation(Word(secondStart, 0), closedSecond ? S(lastWord, Word(-2, 0)) : S(lastWord));
            var cross = new Relation(Word(0, -2), S(crossWord));
            End = new(new Placeholder(), new Relation(first, first));
            Start = new(new Placeholder(), new Relation(last, lastWord));
            var firstStart = new Relation(new Placeholder(), new Relation(first, firstWord));
            Program = new(Frame, "native interrupted traces", S(first, last, cross), S(firstStart, End, Start),
                S((closedFirst ? new[] { first } : Array.Empty<Relation>()).Concat(
                    closedSecond ? new[] { last } : Array.Empty<Relation>()).Cast<IElement>().ToArray()));
        }
        private Relation Word(int x, int y) => S(Enumerable.Repeat<IElement>(QualitativeMotionFrame.Move(x < 0 ? Frame.Left : Frame.Right, reach), Math.Abs(x))
            .Concat(Enumerable.Repeat<IElement>(QualitativeMotionFrame.Move(y < 0 ? Frame.Down : Frame.Up, reach), Math.Abs(y))).ToArray());
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
