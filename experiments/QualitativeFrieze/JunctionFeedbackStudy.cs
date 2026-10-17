using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Paired live feedback worlds over one actually generated diamond
/// substrate. Seeds describe independent material instances related by seed
/// roles; sharing a specification does not duplicate one conserved packet.
/// Construction generates the fixed substrate but does not run any flow.</summary>
public sealed class JunctionFeedbackStudy
{
    // seed = actual exposed intake port -> Closed(actual fresh packet identities)
    // seed correspondence = role ->
    //                       (Closed(first-world packets) -> Closed(control-world packets))
    // A closed empty side declares an intentionally withheld seed. It is not
    // an unresolved quantity or a shared packet imported into two worlds.
    // case support = (flow.Surface -> captured field.Surface) ->
    //                (actual seeds -> (paired correspondence -> retained control evidence))
    public ConnectedWallpaperStudy SourceStudy { get; } = new(2, 2);
    public ConnectedWallpaperStudy.Field SourceField { get; }
    public WallpaperFlowStudy Basis => SourceStudy.Basis;
    public Case[] Cases { get; }
    public Relation Surface { get; }

    public sealed record Case(string Name, string Description,
        QualitativeJunctionFlow Flow, ConnectedWallpaperStudy.Field Field,
        Relation Seeds, int Rounds, Relation SeedCorrespondence, Relation Support);

    public JunctionFeedbackStudy()
    {
        var source = SourceStudy.Cases[1];
        source.Machine.Run(); SourceField = SourceStudy.Capture(source);
        var topology = new QualitativeJunctionTopology(SourceField);
        var rowScopes = E(source.Cells).Cast<Relation>().ToArray();
        var bottom = E(rowScopes[0]); var top = E(rowScopes[1]);
        var bottomLeft = bottom[0]; var bottomRight = bottom[1];
        var topLeft = top[0]; var topRight = top[1];

        var firstCut = Cut(bottomLeft, bottomRight);
        var firstCutTopology = new QualitativeJunctionTopology(firstCut);
        var circulating = Ring("Circulating relay / connected", topology);
        var stopped = Ring("Circulating relay / first seam cut", firstCutTopology);
        var a = new Placeholder(); var b = new Placeholder(); var relayRole = new Placeholder();
        var relaySeeds = S(new Relation(Node(circulating, bottomLeft).InputPort, S(a)));
        var stoppedSeeds = S(new Relation(Node(stopped, bottomLeft).InputPort, S(b)));
        var relayCorrespondence = S(new Relation(relayRole, new Relation(S(a), S(b))));

        var coincidence = TwoInputs("Two-neighbour coincidence / both inputs", topology, false);
        var oneSided = TwoInputs("Two-neighbour coincidence / one input", topology, false);
        var left = new Placeholder(); var below = new Placeholder(); var onlyLeft = new Placeholder();
        var leftRole = new Placeholder(); var belowRole = new Placeholder();
        var coincidenceSeeds = S(new Relation(Node(coincidence, topLeft).InputPort, S(left)),
            new Relation(Node(coincidence, bottomRight).InputPort, S(below)));
        var oneSeeds = S(new Relation(Node(oneSided, topLeft).InputPort, S(onlyLeft)));
        var coincidenceCorrespondence = S(new Relation(leftRole, new Relation(S(left), S(onlyLeft))),
            new Relation(belowRole, new Relation(S(below), S())));

        var inhibitionCut = Cut(topLeft, topRight);
        var inhibitionCutTopology = new QualitativeJunctionTopology(inhibitionCut);
        var reflecting = TwoInputs("Neighbour-controlled reflection / both seams", topology, true);
        var uninhibited = TwoInputs("Neighbour-controlled reflection / inhibitory seam cut", inhibitionCutTopology, true);
        var excitation = new Placeholder(); var inhibition = new Placeholder();
        var controlExcitation = new Placeholder(); var controlInhibition = new Placeholder();
        var excitationRole = new Placeholder(); var inhibitionRole = new Placeholder();
        var reflectionSeeds = S(new Relation(Node(reflecting, bottomRight).InputPort, S(excitation)),
            new Relation(Node(reflecting, topLeft).InputPort, S(inhibition)));
        var uninhibitedSeeds = S(new Relation(Node(uninhibited, bottomRight).InputPort, S(controlExcitation)),
            new Relation(Node(uninhibited, topLeft).InputPort, S(controlInhibition)));
        var reflectionCorrespondence = S(new Relation(excitationRole, new Relation(S(excitation), S(controlExcitation))),
            new Relation(inhibitionRole, new Relation(S(inhibition), S(controlInhibition))));

        Cases = [
            Example(circulating, SourceField, relaySeeds, 12, relayCorrespondence,
                "One packet crosses actual accepted junctions around four nodes. Each visit draws the local phase's next diagonal.", S()),
            Example(stopped, firstCut, stoppedSeeds, 12, relayCorrespondence,
                "The same seed role meets a cut first seam. Its local diagonal is drawn, then the actual packet enters the declared local sink.", SourceField.SeamRead),
            Example(coincidence, SourceField, coincidenceSeeds, 4, coincidenceCorrespondence,
                "Separate left and lower neighbours fill separate intake sites. Their coincident readiness permits the target's live diagonal once.", S()),
            Example(oneSided, SourceField, oneSeeds, 4, coincidenceCorrespondence,
                "The lower seed is deliberately withheld. The left arrival remains at its own intake; the target's coincidence action stays waiting.", S()),
            Example(reflecting, SourceField, reflectionSeeds, 4, reflectionCorrespondence,
                "Excitation and the neighbouring inhibitory arrival select a downward diagonal. The accepted action then latches the target refractory.", S()),
            Example(uninhibited, inhibitionCut, uninhibitedSeeds, 4, reflectionCorrespondence,
                "Both seed roles are retained, but the inhibitory seam is absent. The target receives excitation alone and draws an upward diagonal.", SourceField.SeamRead)
        ];
        Surface = new Relation(SourceField.Surface,
            new Relation(S(relayCorrespondence, coincidenceCorrespondence, reflectionCorrespondence),
                S(Cases.Select(example => (IElement)example.Support).ToArray())));

        ConnectedWallpaperStudy.Field Cut(IElement from, IElement to)
        {
            var selected = S(E(source.ConnectionDefinitions).Where(item =>
                item is not Relation { Left: Relation { Left: Relation cells } } ||
                !ReferenceEquals(cells.Left, from) || !ReferenceEquals(cells.Right, to)).ToArray());
            return SourceStudy.Capture(source, selected);
        }

        QualitativeJunctionFlow Ring(string name, QualitativeJunctionTopology selected)
        {
            var nodes = RelayNodes(selected); var channels = new List<IElement>();
            Link(bottomLeft, bottomRight); Link(bottomRight, topRight);
            Link(topRight, topLeft); Link(topLeft, bottomLeft);
            return new(name, selected, nodes, S(channels.ToArray()));

            void Link(IElement from, IElement to)
            {
                var edge = Edge(selected, from, to);
                if (edge is not null) channels.Add(Channel(edge, nodes.Single(node => ReferenceEquals(node.Cell, from)).InputPort,
                    nodes.Single(node => ReferenceEquals(node.Cell, to)).InputPort));
            }
        }

        QualitativeJunctionFlow TwoInputs(string name, QualitativeJunctionTopology selected, bool reflect)
        {
            var nodes = E(selected.Cells).Select(cell =>
            {
                var anchor = Anchor(selected, cell);
                if (!ReferenceEquals(cell, topRight))
                    return JunctionFlowNode.Relay("source or quiet node", cell, anchor, Basis.Basis.Frame, Basis.Basis.Reach);
                return reflect
                    ? JunctionFlowNode.Reflection("reflected refractory target", cell, anchor, Basis.Basis.Frame, Basis.Basis.Reach)
                    : JunctionFlowNode.Coincidence("two-sided refractory target", cell, anchor, Basis.Basis.Frame, Basis.Basis.Reach);
            }).ToArray();
            var target = nodes.Single(node => ReferenceEquals(node.Cell, topRight));
            var channels = new List<IElement>();
            var leftEdge = Edge(selected, topLeft, topRight);
            var lowerEdge = Edge(selected, bottomRight, topRight);
            if (leftEdge is not null) channels.Add(Channel(leftEdge, nodes.Single(node => ReferenceEquals(node.Cell, topLeft)).InputPort,
                reflect ? target.AuxiliaryPort : target.InputPort));
            if (lowerEdge is not null) channels.Add(Channel(lowerEdge, nodes.Single(node => ReferenceEquals(node.Cell, bottomRight)).InputPort,
                reflect ? target.InputPort : target.AuxiliaryPort));
            return new(name, selected, nodes, S(channels.ToArray()));
        }

        JunctionFlowNode[] RelayNodes(QualitativeJunctionTopology selected) => E(selected.Cells)
            .Select(cell => JunctionFlowNode.Relay("circulating diagonal node", cell, Anchor(selected, cell), Basis.Basis.Frame, Basis.Basis.Reach)).ToArray();

        Case Example(QualitativeJunctionFlow flow, ConnectedWallpaperStudy.Field field, Relation seeds,
            int rounds, Relation correspondence, string description, Relation controlEvidence)
        {
            var support = new Relation(new Relation(flow.Surface, field.Surface),
                new Relation(seeds, new Relation(correspondence, controlEvidence)));
            return new(flow.Name, description, flow, field, seeds, rounds, correspondence, support);
        }
    }

    private static JunctionFlowNode Node(QualitativeJunctionFlow flow, IElement cell) => flow.Nodes.Single(node => ReferenceEquals(node.Cell, cell));
    private static Relation Anchor(QualitativeJunctionTopology topology, IElement cell) =>
        (Relation)E(topology.Anchors).Cast<Relation>().Single(row => ReferenceEquals(row.Left, cell)).Right;
    private static Relation? Edge(QualitativeJunctionTopology topology, IElement from, IElement to) =>
        E(topology.Edges).Cast<Relation>().SingleOrDefault(edge => edge.Left is Relation cells &&
            ReferenceEquals(cells.Left, from) && ReferenceEquals(cells.Right, to));
    private static Relation Channel(Relation edge, Relation source, Relation target)
    {
        var ports = new Relation(source, target); var queue = new Relation(edge, ports);
        return new(new Relation(edge, ports), queue);
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
