using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeJunctionFlowTests
{
    [Fact]
    public void OneActualPacketCirculatesOneNeighbourPerRoundAndRevisitsAlternatingLocalPhases()
    {
        var substrate = Substrate(); var ring = Ring(substrate.Topology, substrate.Reach);
        var packet = new Placeholder(); ring.Flow.Seed(new Placeholder(), ring.Nodes[0].InputPort, S(packet));
        for (var round = 0; round < 8; round++)
        {
            ring.Flow.Round();
            Assert.Equal(round + 1, E(ring.Flow.Emissions).Length); Assert.Equal(round, E(ring.Flow.Deliveries).Length);
            for (var node = 0; node < ring.Nodes.Length; node++)
                Assert.Equal(Enumerable.Range(0, round + 1).Count(index => index % 4 == node), E(ring.Nodes[node].Activations).Length);
            Assert.Same(packet, Assert.Single(E(ring.Flow.Transport.ImportedPackets)));
            Assert.Same(Assert.IsType<Relation>(E(ring.Flow.Channels)[round % 4]).Right, Owner(ring.Flow, packet));
            AssertAccounted(ring.Flow, packet);
        }
        foreach (var node in ring.Nodes)
        {
            var strokes = E(node.Drawing.Strokes).Cast<Relation>().ToArray(); Assert.Equal(2, strokes.Length);
            Assert.True(QualitativeStrokeGraph.TryReadStroke(strokes[0], out _, out _, out _, out var first));
            Assert.True(QualitativeStrokeGraph.TryReadStroke(strokes[1], out _, out _, out _, out var second));
            Assert.Same(node.Drawing.Frame.Up, Assert.IsType<Relation>(E(first)[1]).Left);
            Assert.Same(node.Drawing.Frame.Down, Assert.IsType<Relation>(E(second)[1]).Left);
            AssertAcceptedActivations(ring.Flow, node);
        }
        Assert.Empty(E(ring.Flow.Pending)); Assert.Empty(E(ring.Flow.Blocked)); Assert.Empty(E(ring.Flow.Unresolved));
        Assert.DoesNotContain(Traverse(new Relation(ring.Flow.Surface, ring.Flow.Run(0))), item => item is Carrier or CarrierLocation);
    }

    [Fact]
    public void ReversingTheNodeEnumerationCannotDeliverANewEmissionInItsOwnRound()
    {
        var substrate = Substrate(); var forward = Ring(substrate.Topology, substrate.Reach); var reverse = Ring(substrate.Topology, substrate.Reach);
        var a = new Placeholder(); var b = new Placeholder();
        forward.Flow.Seed(new Placeholder(), forward.Nodes[0].InputPort, S(a)); reverse.Flow.Seed(new Placeholder(), reverse.Nodes[0].InputPort, S(b));
        var oppositeSchedule = S(reverse.Nodes.Reverse().Select(node => (IElement)node.Surface).ToArray());
        for (var round = 0; round < 5; round++)
        {
            forward.Flow.Round(); reverse.Flow.Round(oppositeSchedule);
            Assert.Equal(forward.Nodes.Select(node => E(node.Activations).Length), reverse.Nodes.Select(node => E(node.Activations).Length));
            Assert.Equal(E(forward.Flow.Deliveries).Length, E(reverse.Flow.Deliveries).Length);
            Assert.Equal(round + 1, E(reverse.Flow.Emissions).Length);
            AssertAccounted(forward.Flow, a); AssertAccounted(reverse.Flow, b);
        }
    }

    [Fact]
    public void CuttingAcceptedSeamsRemovesRemotePressureWithoutRemovingTheLocalResponse()
    {
        var substrate = Substrate(2, 1); var open = Pair(substrate.Topology, substrate.Reach);
        var cutField = substrate.Study.Capture(substrate.Example, S()); var cutTopology = new QualitativeJunctionTopology(cutField);
        Assert.True(cutTopology.IsValid); Assert.Empty(E(cutTopology.Edges));
        var cutNodes = Nodes(cutTopology, substrate.Reach); var cut = new QualitativeJunctionFlow("cut", cutTopology, cutNodes, S());
        var first = new Placeholder(); var second = new Placeholder();
        open.Flow.Seed(new Placeholder(), open.Nodes[0].InputPort, S(first)); cut.Seed(new Placeholder(), cutNodes[0].InputPort, S(second));
        open.Flow.Run(2); cut.Run(2);
        Assert.Single(E(open.Nodes[1].Activations)); Assert.Empty(E(cutNodes[1].Activations));
        Assert.Single(E(cutNodes[0].Activations)); Assert.Empty(E(cut.Deliveries)); Assert.Same(cutNodes[0].SinkPort, Owner(cut, second));
        AssertAccounted(open.Flow, first); AssertAccounted(cut, second);
        var borrowedEdge = Assert.IsType<Relation>(Assert.Single(E(open.Flow.Channels)));
        var invalid = new QualitativeJunctionFlow("cannot borrow cut seam", cutTopology, cutNodes, S(borrowedEdge));
        Assert.False(invalid.IsValid); Assert.NotEmpty(E(invalid.DefinitionProblems));
        invalid.Seed(new Placeholder(), cutNodes[0].InputPort, S(new Placeholder())); invalid.Round();
        Assert.Empty(E(invalid.Transport.ImportedPackets)); Assert.Empty(E(invalid.Emissions));
    }

    [Fact]
    public void CoincidenceNeedsTwoDistinctInputPassagesAndRetainsSameSideOverflow()
    {
        var substrate = Substrate(); var distinct = TwoSources(substrate.Topology, substrate.Reach, coincidence: true, sameSide: false);
        var sameSide = TwoSources(substrate.Topology, substrate.Reach, coincidence: true, sameSide: true);
        var a = new Placeholder(); var b = new Placeholder(); var c = new Placeholder(); var d = new Placeholder();
        SeedBoth(distinct, a, b); SeedBoth(sameSide, c, d);
        distinct.Flow.Run(2); sameSide.Flow.Run(2);
        Assert.Single(E(distinct.Target.Activations)); Assert.Empty(E(sameSide.Target.Activations));
        Assert.Empty(E(distinct.Flow.Transport.PortPackets(distinct.Target.InputPort)));
        Assert.Empty(E(distinct.Flow.Transport.PortPackets(distinct.Target.AuxiliaryPort)));
        Assert.Equal(2, E(distinct.Flow.Transport.PortPackets(distinct.Target.SinkPort)).Length);
        Assert.Single(E(sameSide.Flow.Transport.PortPackets(sameSide.Target.InputPort)));
        Assert.Empty(E(sameSide.Flow.Transport.PortPackets(sameSide.Target.AuxiliaryPort))); Assert.NotEmpty(E(sameSide.Flow.Blocked));
        Assert.Equal(1, new[] { c, d }.Count(packet => ReferenceEquals(Owner(sameSide.Flow, packet), sameSide.Target.InputPort)));
        AssertAccounted(distinct.Flow, a, b); AssertAccounted(sameSide.Flow, c, d);
        AssertAcceptedActivations(distinct.Flow, distinct.Target);
    }

    [Fact]
    public void ASeparateNeighbourInputChangesTheConditionalAndCuttingItsChannelRestoresTheOtherAction()
    {
        var substrate = Substrate(); var inhibited = TwoSources(substrate.Topology, substrate.Reach, coincidence: false, sameSide: false);
        var cut = TwoSources(substrate.Topology, substrate.Reach, coincidence: false, sameSide: false, cutSecond: true);
        var a = new Placeholder(); var b = new Placeholder(); var c = new Placeholder(); var d = new Placeholder();
        SeedBoth(inhibited, a, b); SeedBoth(cut, c, d); inhibited.Flow.Run(2); cut.Flow.Run(2);
        Assert.Single(E(inhibited.Target.Activations)); Assert.Single(E(cut.Target.Activations));
        Assert.Same(inhibited.Target.Drawing.Frame.Down, Vertical(inhibited.Target)); Assert.Same(cut.Target.Drawing.Frame.Up, Vertical(cut.Target));
        Assert.Same(cut.Second.SinkPort, Owner(cut.Flow, d)); Assert.Empty(E(cut.Flow.Transport.PortPackets(cut.Target.AuxiliaryPort)));
        AssertAccounted(inhibited.Flow, a, b); AssertAccounted(cut.Flow, c, d);
    }

    [Fact]
    public void PureAcceptedDrawingAndWrongAcknowledgmentsCannotReleasePressureOrAdvanceThePhase()
    {
        var substrate = Substrate(2, 1); var pair = Pair(substrate.Topology, substrate.Reach); var node = pair.Nodes[0];
        var packet = new Placeholder(); pair.Flow.Seed(new Placeholder(), node.InputPort, S(packet));
        var phase = node.ControllerState; var material = pair.Flow.Transport.State; var drawing = node.Drawing.State;
        var request = pair.Flow.Propose(node);
        Assert.True(pair.Flow.TryReadRequest(request, out var selected, out var action, out var before, out var offer)); Assert.Same(node, selected);
        var actual = node.Drawing.Step(before, offer); Assert.True(node.Drawing.TryReadAccepted(actual, out _, out _));
        Assert.Same(phase, node.ControllerState); Assert.Same(material, pair.Flow.Transport.State); Assert.Same(drawing, node.Drawing.State);
        Assert.Same(node.InputPort, Owner(pair.Flow, packet)); Assert.Empty(E(pair.Flow.Emissions));
        var copy = new Relation(request.Left, request.Right); pair.Flow.Acknowledge(copy, actual);
        Assert.True(node.Drawing.TryReadState(before, out var previous, out _, out _, out _, out _, out _, out _));
        var otherBranch = node.Drawing.Step(before, node.Drawing.Offer(node.Drawing.Source, new Placeholder(), previous, S(action), request));
        Assert.True(node.Drawing.TryReadAccepted(otherBranch, out _, out _)); pair.Flow.Acknowledge(request, otherBranch);
        var foreign = pair.Nodes[1].Drawing;
        var foreignEffect = foreign.Apply(new Placeholder(), S(pair.Nodes[1].Up), S()); pair.Flow.Acknowledge(request, foreignEffect);
        Assert.Same(request, Assert.Single(E(node.Pending))); Assert.Same(phase, node.ControllerState); Assert.Same(material, pair.Flow.Transport.State);
        Assert.Same(drawing, node.Drawing.State); Assert.Empty(E(pair.Flow.Emissions));
        pair.Flow.Acknowledge(request, actual);
        Assert.Single(E(pair.Flow.Emissions)); Assert.Empty(E(node.Pending)); Assert.NotSame(phase, node.ControllerState);
        Assert.Same(Assert.IsType<Relation>(Assert.Single(E(pair.Flow.Channels))).Right, Owner(pair.Flow, packet));
        var after = pair.Flow.Transport.State; var acceptedPhase = node.ControllerState;
        pair.Flow.Acknowledge(request, actual);
        Assert.Single(E(pair.Flow.Emissions)); Assert.Same(after, pair.Flow.Transport.State); Assert.Same(acceptedPhase, node.ControllerState);
        AssertAcceptedActivations(pair.Flow, node); AssertAccounted(pair.Flow, packet);
    }

    [Fact]
    public void AnAcceptedHistoricalDrawingReceiptCannotOverwriteANewerLiveHead()
    {
        var substrate = Substrate(2, 1); var pair = Pair(substrate.Topology, substrate.Reach); var node = pair.Nodes[0];
        var packet = new Placeholder(); pair.Flow.Seed(new Placeholder(), node.InputPort, S(packet));
        var request = pair.Flow.Propose(node); Assert.True(pair.Flow.TryReadRequest(request, out _, out var action, out var before, out var offer));
        var historical = node.Drawing.Step(before, offer); Assert.True(node.Drawing.TryReadAccepted(historical, out _, out _));
        node.Drawing.Apply(new Placeholder(), S(action), new Placeholder());
        var live = node.Drawing.State; var material = pair.Flow.Transport.State; var phase = node.ControllerState;
        pair.Flow.Acknowledge(request, historical);
        Assert.Same(live, node.Drawing.State); Assert.Same(material, pair.Flow.Transport.State); Assert.Same(phase, node.ControllerState);
        Assert.Same(request, Assert.Single(E(node.Pending))); Assert.Empty(E(pair.Flow.Emissions));
        Assert.Same(node.InputPort, Owner(pair.Flow, packet)); AssertAccounted(pair.Flow, packet);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingOrOpenPermissionRetainsThePacketWithoutInventingRestOrAnAction(bool open)
    {
        var substrate = Substrate(2, 1); var cell = E(substrate.Topology.Cells)[0]; var anchor = Anchor(substrate.Topology, cell);
        var node = new JunctionFlowNode("unresolved condition", cell, anchor, substrate.Topology.Field.SourceGraph.Frame, substrate.Reach,
            current => FiniteLaw.Create(S(current.Waiting, current.Ready), S(current.AuxiliaryWaiting, current.AuxiliaryReady), current.Actions, [
                FiniteLaw.Row(current.Waiting, current.AuxiliaryWaiting, S(current.Hold)),
                FiniteLaw.Row(current.Waiting, current.AuxiliaryReady, S(current.Hold)),
                FiniteLaw.Row(current.Ready, current.AuxiliaryReady, S(current.Up)),
                .. (open ? new[] { FiniteLaw.Row(current.Ready, current.AuxiliaryWaiting, StructuralConstraint.Open(current.Up)) } : Array.Empty<Relation>())]));
        var flow = new QualitativeJunctionFlow("unknown", substrate.Topology, new[] { node }, S()); Assert.True(flow.IsValid);
        var packet = new Placeholder(); flow.Seed(new Placeholder(), node.InputPort, S(packet));
        var phase = node.ControllerState; var drawing = node.Drawing.State; var material = flow.Transport.State;
        var question = flow.Propose(node);
        Assert.False(flow.TryReadRequest(question, out _, out _, out _, out _)); Assert.NotEmpty(E(flow.Unresolved));
        Assert.Same(phase, node.ControllerState); Assert.Same(drawing, node.Drawing.State); Assert.Same(material, flow.Transport.State);
        Assert.Same(node.InputPort, Owner(flow, packet)); Assert.Empty(E(flow.Emissions)); AssertAccounted(flow, packet);
    }

    [Fact]
    public void NoInputLeavesLocalControllersAndMaterialQuietWhileExplicitRoundsMayAdvance()
    {
        var substrate = Substrate(2, 1); var pair = Pair(substrate.Topology, substrate.Reach);
        var phases = pair.Nodes.Select(node => node.ControllerState).ToArray(); var material = pair.Flow.Transport.State;
        pair.Flow.Run(3);
        Assert.Same(material, pair.Flow.Transport.State); Assert.Empty(E(pair.Flow.Transport.ImportedPackets));
        Assert.Empty(E(pair.Flow.Emissions)); Assert.Empty(E(pair.Flow.Deliveries)); Assert.Empty(E(pair.Flow.Unresolved));
        for (var index = 0; index < pair.Nodes.Length; index++)
        { Assert.Same(phases[index], pair.Nodes[index].ControllerState); Assert.Empty(E(pair.Nodes[index].Drawing.Strokes)); }
        Assert.Equal(3, E(pair.Flow.Timeline).Length);
    }

    [Fact]
    public void OneMaterialInputCannotBeDuplicatedAcrossTwoOutgoingBranchesOrBorrowAForeignEdge()
    {
        var substrate = Substrate(); var nodes = Nodes(substrate.Topology, substrate.Reach);
        var first = Channel(substrate.Topology, nodes[0], nodes[1], nodes[0].InputPort, nodes[1].InputPort);
        var second = Channel(substrate.Topology, nodes[0], nodes[2], nodes[0].InputPort, nodes[2].InputPort);
        var fork = new QualitativeJunctionFlow("duplicate export", substrate.Topology, nodes, S(first, second));
        Assert.False(fork.IsValid); fork.Seed(new Placeholder(), nodes[0].InputPort, S(new Placeholder())); fork.Round();
        Assert.Empty(E(fork.Transport.ImportedPackets)); Assert.Empty(E(fork.Emissions)); Assert.NotEmpty(E(fork.DefinitionProblems));
        var context = Assert.IsType<Relation>(first.Left); var edge = Assert.IsType<Relation>(context.Left);
        var cloned = new Relation(new Relation(new Relation(edge.Left, edge.Right), context.Right), first.Right);
        var foreign = new QualitativeJunctionFlow("copied edge", substrate.Topology, nodes, S(cloned));
        Assert.False(foreign.IsValid); Assert.NotEmpty(E(foreign.DefinitionProblems));
    }

    [Fact]
    public void DuplicateCellDefinitionsRemainAnInvalidWorldEvenWhenAChannelUsesThatCell()
    {
        var substrate = Substrate(2, 1); var nodes = Nodes(substrate.Topology, substrate.Reach);
        var channel = Channel(substrate.Topology, nodes[0], nodes[1], nodes[0].InputPort, nodes[1].InputPort);
        var duplicate = JunctionFlowNode.Relay("another receiver at same cell", nodes[0].Cell, nodes[0].Anchor,
            substrate.Topology.Field.SourceGraph.Frame, substrate.Reach);
        foreach (var repeated in new[] { nodes[0], duplicate })
        {
            var flow = new QualitativeJunctionFlow("duplicate cell", substrate.Topology, new[] { nodes[0], repeated, nodes[1] }, S(channel));
            Assert.False(flow.IsValid); Assert.NotEmpty(E(flow.DefinitionProblems));
            var before = flow.Transport.State;
            flow.Seed(new Placeholder(), nodes[0].InputPort, S(new Placeholder())); flow.Round();
            Assert.Same(before, flow.Transport.State); Assert.Empty(E(flow.Emissions));
        }
    }

    [Fact]
    public void ANodeCannotProjectItsTraceIntoAForeignWorldFrameUsingAnActualCellAndAnchor()
    {
        var substrate = Substrate(2, 1); var nodes = Nodes(substrate.Topology, substrate.Reach);
        var wrong = JunctionFlowNode.Relay("foreign view", nodes[0].Cell, nodes[0].Anchor, new QualitativeMotionFrame(), substrate.Reach);
        var channel = Channel(substrate.Topology, wrong, nodes[1], wrong.InputPort, nodes[1].InputPort);
        var flow = new QualitativeJunctionFlow("wrong target basis", substrate.Topology, new[] { wrong, nodes[1] }, S(channel));
        Assert.NotSame(substrate.Topology.Field.SourceGraph.Frame, wrong.Projection.Target);
        Assert.False(flow.IsValid); Assert.NotEmpty(E(flow.DefinitionProblems));
        var before = flow.Transport.State; flow.Seed(new Placeholder(), wrong.InputPort, S(new Placeholder())); flow.Round();
        Assert.Same(before, flow.Transport.State); Assert.Empty(E(wrong.Activations));
    }

    [Fact]
    public void PublicQuestionsCannotReadForeignOrMissingCapacityAsKnownEmptyInputs()
    {
        var substrate = Substrate(2, 1); var pair = Pair(substrate.Topology, substrate.Reach); var node = pair.Nodes[0];
        var other = new QualitativePacketTransport(S(new Relation(new Placeholder(), S(new Placeholder()))), S());
        var onlyFirst = new QualitativePacketTransport(S(new Relation(node.InputPort, node.InputSites)), S());
        var lookalikePort = new Relation(node.InputPort.Left, node.InputPort.Right);
        var copied = new QualitativePacketTransport(S(new Relation(lookalikePort, node.InputSites), new Relation(node.AuxiliaryPort, node.AuxiliarySites)), S());
        var open = new QualitativePacketTransport(S(new Relation(node.InputPort, StructuralConstraint.Open(E(node.InputSites))),
            new Relation(node.AuxiliaryPort, node.AuxiliarySites)), S());
        var originalPhase = node.ControllerState; var originalDrawing = node.Drawing.State;
        var cases = new[] {
            (Transport: pair.Flow.Transport, Snapshot: other.State),
            (Transport: other, Snapshot: other.State),
            (Transport: onlyFirst, Snapshot: onlyFirst.State),
            (Transport: copied, Snapshot: copied.State),
            (Transport: open, Snapshot: open.State)
        };
        foreach (var sample in cases)
        {
            var before = sample.Transport.State;
            var question = node.Ask(sample.Transport, sample.Snapshot, new Placeholder());
            Assert.False(QualitativeQuestion.TryReadAnswer(question, out _));
            Assert.Same(before, sample.Transport.State); Assert.Same(originalPhase, node.ControllerState); Assert.Same(originalDrawing, node.Drawing.State);
        }
    }

    [Fact]
    public void ADelayedQueueCannotAliasEitherActualNodeInputPassage()
    {
        var substrate = Substrate(2, 1); var nodes = Nodes(substrate.Topology, substrate.Reach);
        var channel = Channel(substrate.Topology, nodes[0], nodes[1], nodes[0].InputPort, nodes[1].InputPort);
        foreach (var alias in new[] { nodes[0].InputPort, nodes[1].InputPort })
        {
            var flow = new QualitativeJunctionFlow("aliased queue", substrate.Topology, nodes, S(new Relation(channel.Left, alias)));
            Assert.False(flow.IsValid); Assert.NotEmpty(E(flow.DefinitionProblems));
            var before = flow.Transport.State; flow.Seed(new Placeholder(), nodes[0].InputPort, S(new Placeholder())); flow.Round();
            Assert.Same(before, flow.Transport.State); Assert.Empty(E(flow.Emissions));
        }
    }

    [Fact]
    public void MutatingTheSuppliedNodeArrayCannotReplaceTheCapturedNativeParticipants()
    {
        var substrate = Substrate(2, 1); var supplied = Nodes(substrate.Topology, substrate.Reach); var original = supplied[0]; var target = supplied[1];
        var channel = Channel(substrate.Topology, original, target, original.InputPort, target.InputPort);
        var flow = new QualitativeJunctionFlow("captured participants", substrate.Topology, supplied, S(channel)); Assert.True(flow.IsValid);
        supplied[0] = target;
        Assert.Same(original, flow.Nodes[0]); Assert.Same(target, flow.Nodes[1]);
        var nativeNodes = Assert.IsType<Relation>(Assert.IsType<Relation>(flow.Surface.Left).Right);
        Assert.Same(original.Surface, E(nativeNodes)[0]); Assert.Same(target.Surface, E(nativeNodes)[1]);
        var packet = new Placeholder(); flow.Seed(new Placeholder(), original.InputPort, S(packet)); flow.Round();
        Assert.Single(E(original.Activations)); Assert.Empty(E(target.Activations)); Assert.Empty(E(flow.Unresolved)); AssertAccounted(flow, packet);
    }

    [Fact]
    public void ExternalSeedsCannotBypassNodeInputsByEnteringAQueueOrSink()
    {
        var substrate = Substrate(2, 1); var pair = Pair(substrate.Topology, substrate.Reach);
        var queue = Assert.IsType<Relation>(Assert.IsType<Relation>(Assert.Single(E(pair.Flow.Channels))).Right);
        var before = pair.Flow.Transport.State;
        foreach (var port in new[] { queue, pair.Nodes[0].SinkPort, pair.Nodes[1].SinkPort })
        {
            pair.Flow.Seed(new Placeholder(), port, S(new Placeholder()));
            Assert.Same(before, pair.Flow.Transport.State); Assert.Empty(E(pair.Flow.Transport.ImportedPackets));
        }
        Assert.Empty(E(pair.Flow.Emissions)); Assert.Empty(E(pair.Flow.Deliveries)); Assert.NotEmpty(E(pair.Flow.Unresolved));
    }

    private static (ConnectedWallpaperStudy Study, ConnectedWallpaperStudy.Case Example, QualitativeJunctionTopology Topology, IElement Reach) Substrate(int columns = 2, int rows = 2)
    {
        var study = new ConnectedWallpaperStudy(columns, rows); var example = study.Cases[1]; example.Machine.Run(128);
        Assert.True(example.Machine.IsComplete); var topology = new QualitativeJunctionTopology(study.Capture(example)); Assert.True(topology.IsValid);
        return (study, example, topology, study.Basis.Basis.Reach);
    }
    private static JunctionFlowNode[] Nodes(QualitativeJunctionTopology topology, IElement reach) => E(topology.Cells)
        .Select((cell, index) => JunctionFlowNode.Relay("node " + index, cell, Anchor(topology, cell), topology.Field.SourceGraph.Frame, reach)).ToArray();
    private static Relation Anchor(QualitativeJunctionTopology topology, IElement cell) =>
        Assert.IsType<Relation>(Assert.Single(E(topology.Anchors).Cast<Relation>(), row => ReferenceEquals(row.Left, cell)).Right);
    private static Relation Channel(QualitativeJunctionTopology topology, JunctionFlowNode from, JunctionFlowNode to, Relation source, Relation target)
    {
        var edge = Assert.Single(E(topology.Edges).Cast<Relation>(), row => topology.TryReadEdge(row, out var a, out var b, out _) && ReferenceEquals(a, from.Cell) && ReferenceEquals(b, to.Cell));
        return new Relation(new Relation(edge, new Relation(source, target)), new Relation(edge, new Placeholder()));
    }
    private static (QualitativeJunctionFlow Flow, JunctionFlowNode[] Nodes) Ring(QualitativeJunctionTopology topology, IElement reach)
    {
        var grid = Nodes(topology, reach); var ring = new[] { grid[0], grid[1], grid[3], grid[2] };
        var channels = S(ring.Select((node, index) => (IElement)Channel(topology, node, ring[(index + 1) % ring.Length], node.InputPort, ring[(index + 1) % ring.Length].InputPort)).ToArray());
        var flow = new QualitativeJunctionFlow("ring", topology, ring, channels); Assert.True(flow.IsValid); return (flow, ring);
    }
    private static (QualitativeJunctionFlow Flow, JunctionFlowNode[] Nodes) Pair(QualitativeJunctionTopology topology, IElement reach)
    {
        var nodes = Nodes(topology, reach); var flow = new QualitativeJunctionFlow("pair", topology, nodes,
            S(Channel(topology, nodes[0], nodes[1], nodes[0].InputPort, nodes[1].InputPort))); Assert.True(flow.IsValid); return (flow, nodes);
    }
    private sealed record SourcePair(QualitativeJunctionFlow Flow, JunctionFlowNode First, JunctionFlowNode Second, JunctionFlowNode Target);
    private static SourcePair TwoSources(QualitativeJunctionTopology topology, IElement reach, bool coincidence, bool sameSide, bool cutSecond = false)
    {
        var nodes = Nodes(topology, reach); var cell = nodes[1].Cell;
        nodes[1] = coincidence ? JunctionFlowNode.Coincidence("coincidence", cell, Anchor(topology, cell), topology.Field.SourceGraph.Frame, reach)
            : JunctionFlowNode.Reflection("reflection", cell, Anchor(topology, cell), topology.Field.SourceGraph.Frame, reach);
        var channels = new List<IElement> { Channel(topology, nodes[0], nodes[1], nodes[0].InputPort, nodes[1].InputPort) };
        if (!cutSecond) channels.Add(Channel(topology, nodes[3], nodes[1], nodes[3].InputPort, sameSide ? nodes[1].InputPort : nodes[1].AuxiliaryPort));
        var flow = new QualitativeJunctionFlow("two sources", topology, nodes, S(channels.ToArray())); Assert.True(flow.IsValid);
        return new(flow, nodes[0], nodes[3], nodes[1]);
    }
    private static void SeedBoth(SourcePair pair, IElement first, IElement second)
    { pair.Flow.Seed(new Placeholder(), pair.First.InputPort, S(first)); pair.Flow.Seed(new Placeholder(), pair.Second.InputPort, S(second)); }
    private static IElement Vertical(JunctionFlowNode node)
    { Assert.True(QualitativeStrokeGraph.TryReadStroke(Assert.IsType<Relation>(Assert.Single(E(node.Drawing.Strokes))), out _, out _, out _, out var chord)); return Assert.IsType<Relation>(E(chord)[1]).Left; }
    private static IElement Owner(QualitativeJunctionFlow flow, IElement packet)
    {
        Assert.True(flow.Transport.TryReadState(flow.Transport.State, out _, out var placements, out _, out _));
        return Assert.IsType<Relation>(Assert.Single(E(placements).Cast<Relation>(), row => ReferenceEquals(row.Left, packet)).Right).Left;
    }
    private static void AssertAccounted(QualitativeJunctionFlow flow, params IElement[] expected)
    {
        Assert.True(flow.Transport.TryReadState(flow.Transport.State, out _, out var placements, out var imported, out _));
        Assert.Equal(expected.Length, E(imported).Length); Assert.Equal(expected.Length, E(placements).Length);
        foreach (var packet in expected)
        { Assert.Single(E(imported), item => ReferenceEquals(item, packet)); Assert.Single(E(placements).Cast<Relation>(), row => ReferenceEquals(row.Left, packet)); }
        var account = Assert.IsType<Relation>(flow.Transport.Account().Right); Assert.Empty(E(Assert.IsType<Relation>(account.Right)));
    }
    private static void AssertAcceptedActivations(QualitativeJunctionFlow flow, JunctionFlowNode node)
    {
        foreach (var activation in E(node.Activations).Cast<Relation>())
        {
            var premise = Assert.IsType<Relation>(activation.Left); var effect = Assert.IsType<Relation>(premise.Right);
            Assert.True(node.Drawing.TryReadAccepted(effect, out _, out _));
            var result = Assert.IsType<Relation>(activation.Right); Assert.True(node.Controller.TryReadAccepted(Assert.IsType<Relation>(result.Left), out _, out _, out _));
            var transfer = Assert.IsType<Relation>(result.Right); Assert.True(flow.Transport.TryReadAccepted(transfer, out _, out _));
            Assert.True(flow.Transport.TryReadAccounting(transfer, out var incoming, out var accepted, out var blocked, out var remainder));
            Assert.Equal(E(incoming).Length, E(accepted).Length); Assert.Empty(E(blocked)); Assert.Empty(E(remainder));
        }
    }
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var element))
        { if (!seen.Add(element)) continue; yield return element; if (element is Relation row) { stack.Push(row.Left); stack.Push(row.Right); } }
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
