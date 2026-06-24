using Gojo.Core;

namespace Gojo.Encoding;

/// <summary>
/// Local boundary/area discovery over a concept definition graph. It consumes
/// generic definition reads and emits a compact interpretation of boundary
/// pairs, lane evidence, settled area, anti-area, and unresolved mixed lanes.
/// </summary>
public static class BoundaryAreaReadSurface
{
    public readonly record struct BoundaryRead(
        ConceptDefinitionDiscoverySurface.DefinitionNode Definition,
        string Role,
        string Extent);

    public readonly record struct BoundaryPairRead(
        ConceptDefinitionDiscoverySurface.DefinitionNode Definition,
        BoundaryRead Source,
        BoundaryRead Terminal);

    public readonly record struct LaneRead(
        ConceptDefinitionDiscoverySurface.DefinitionNode Definition,
        ConceptDefinitionDiscoverySurface.DefinitionNode Target,
        string Mark);

    public readonly record struct LaneEvidenceRead(
        ConceptDefinitionDiscoverySurface.DefinitionNode Definition,
        LaneRead[] Lanes);

    public readonly record struct AreaRead(
        ConceptDefinitionDiscoverySurface.DefinitionNode Definition,
        string Settlement,
        BoundaryPairRead BoundaryPair,
        LaneEvidenceRead LaneEvidence,
        string ResidueOrTension);

    public readonly record struct ResidueRead(
        ConceptDefinitionDiscoverySurface.DefinitionNode Definition,
        AreaRead Source,
        string ResidueOrTension);

    public readonly record struct BandAreaRead(
        BoundaryRead[] Boundaries,
        BoundaryPairRead[] BoundaryPairs,
        LaneEvidenceRead[] LaneEvidence,
        AreaRead[] Areas,
        ResidueRead[] Residues);

    public static bool TryReadBandArea(IElement ledger, out BandAreaRead read)
    {
        if (!ConceptDefinitionDiscoverySurface.TryReadLedger(ledger, out var graph))
        {
            read = default;
            return false;
        }

        return TryReadBandArea(graph, out read);
    }

    public static bool TryReadBandArea(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        out BandAreaRead read)
    {
        var sourceBoundaries = SourceBoundaries(graph);
        var terminalBoundaries = TerminalBoundaries(graph);
        var boundaries = sourceBoundaries.Concat(terminalBoundaries).ToArray();
        var boundaryByDefinition = new Dictionary<IElement, BoundaryRead>(ReferenceEqualityComparer.Instance);
        foreach (var boundary in boundaries)
        {
            boundaryByDefinition[boundary.Definition.Definition] = boundary;
        }

        var boundaryPairs = BoundaryPairs(graph, boundaryByDefinition);
        var pairByDefinition = new Dictionary<IElement, BoundaryPairRead>(ReferenceEqualityComparer.Instance);
        foreach (var pair in boundaryPairs)
        {
            pairByDefinition[pair.Definition.Definition] = pair;
        }

        var lanes = Lanes(graph);
        var laneByDefinition = new Dictionary<IElement, LaneRead>(ReferenceEqualityComparer.Instance);
        foreach (var lane in lanes)
        {
            laneByDefinition[lane.Definition.Definition] = lane;
        }

        var laneEvidence = LaneEvidences(graph, laneByDefinition);
        var evidenceByDefinition = new Dictionary<IElement, LaneEvidenceRead>(ReferenceEqualityComparer.Instance);
        foreach (var evidence in laneEvidence)
        {
            evidenceByDefinition[evidence.Definition.Definition] = evidence;
        }

        var areas = Areas(graph, pairByDefinition, evidenceByDefinition);
        var areaByDefinition = new Dictionary<IElement, AreaRead>(ReferenceEqualityComparer.Instance);
        foreach (var area in areas)
        {
            areaByDefinition[area.Definition.Definition] = area;
        }

        var residues = Residues(graph, areaByDefinition);
        read = new BandAreaRead(boundaries, boundaryPairs, laneEvidence, areas, residues);
        return read.Areas.Length > 0 || read.Residues.Length > 0;
    }

    private static BoundaryRead[] SourceBoundaries(ConceptDefinitionDiscoverySurface.DefinitionGraph graph) =>
        [.. graph.WithCompoundBody("Bd0")
            .Select(node => new BoundaryRead(node, "source", "source"))];

    private static BoundaryRead[] TerminalBoundaries(ConceptDefinitionDiscoverySurface.DefinitionGraph graph) =>
        [.. graph.WithCompoundBody("Bd1")
            .Select(node =>
            {
                var extent = TerminalBoundaryExtent(graph, node);
                return new BoundaryRead(node, "terminal", extent);
            })];

    private static string TerminalBoundaryExtent(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        ConceptDefinitionDiscoverySurface.DefinitionNode terminalBoundary)
    {
        foreach (var offset in graph.DependenciesOf(terminalBoundary))
        {
            if (!graph.TryReadCompoundBody(offset, out var offsetBody) ||
                offsetBody.Lexical != "Off")
            {
                continue;
            }

            foreach (var dependency in graph.DependenciesOf(offset))
            {
                if (graph.TryReadAtomBody(dependency, out var extent) &&
                    (extent == "+d" || extent == "-d"))
                {
                    return extent;
                }
            }
        }

        return "?";
    }

    private static BoundaryPairRead[] BoundaryPairs(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        IReadOnlyDictionary<IElement, BoundaryRead> boundaries)
    {
        var pairs = new List<BoundaryPairRead>();
        foreach (var node in graph.Nodes)
        {
            if (!graph.TryReadDefinitionGroupBody(node, out var participants) ||
                participants.Length != 2 ||
                !boundaries.TryGetValue(participants[0].Definition, out var source) ||
                !boundaries.TryGetValue(participants[1].Definition, out var terminal) ||
                source.Role != "source" ||
                terminal.Role != "terminal")
            {
                continue;
            }

            pairs.Add(new BoundaryPairRead(node, source, terminal));
        }

        return [.. pairs];
    }

    private static LaneRead[] Lanes(ConceptDefinitionDiscoverySurface.DefinitionGraph graph)
    {
        var lanes = new List<LaneRead>();
        foreach (var node in graph.WithCompoundBody("Dir"))
        {
            var dependencies = graph.DependenciesOf(node);
            if (dependencies.Count == 0 ||
                !graph.TryReadCompoundBody(node, out var body) ||
                body.PartNames.Length < 2)
            {
                continue;
            }

            lanes.Add(new LaneRead(node, dependencies[0], body.PartNames[1]));
        }

        return [.. lanes];
    }

    private static LaneEvidenceRead[] LaneEvidences(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        IReadOnlyDictionary<IElement, LaneRead> lanes)
    {
        var evidences = new List<LaneEvidenceRead>();
        foreach (var node in graph.Nodes)
        {
            if (!graph.TryReadDefinitionGroupBody(node, out var participants))
            {
                continue;
            }

            var laneReads = new List<LaneRead>();
            foreach (var participant in participants)
            {
                if (!lanes.TryGetValue(participant.Definition, out var lane))
                {
                    laneReads.Clear();
                    break;
                }

                laneReads.Add(lane);
            }

            if (laneReads.Count > 0)
            {
                evidences.Add(new LaneEvidenceRead(node, [.. laneReads]));
            }
        }

        return [.. evidences];
    }

    private static AreaRead[] Areas(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        IReadOnlyDictionary<IElement, BoundaryPairRead> boundaryPairs,
        IReadOnlyDictionary<IElement, LaneEvidenceRead> laneEvidence)
    {
        var areas = new List<AreaRead>();
        foreach (var node in graph.Nodes)
        {
            if (!graph.TryReadCompoundBody(node, out var body) ||
                body.Lexical is not ("Area+" or "Area-" or "Area?"))
            {
                continue;
            }

            var dependencies = graph.DependenciesOf(node);
            var pair = dependencies
                .Select(dependency => boundaryPairs.TryGetValue(dependency.Definition, out var read) ? read : (BoundaryPairRead?)null)
                .FirstOrDefault(item => item.HasValue);
            var evidence = dependencies
                .Select(dependency => laneEvidence.TryGetValue(dependency.Definition, out var read) ? read : (LaneEvidenceRead?)null)
                .FirstOrDefault(item => item.HasValue);

            if (!pair.HasValue || !evidence.HasValue)
            {
                continue;
            }

            areas.Add(new AreaRead(
                node,
                SettlementOf(body.Lexical),
                pair.Value,
                evidence.Value,
                ResidueText(node.Read.ResidueOrTension)));
        }

        return [.. areas];
    }

    private static ResidueRead[] Residues(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        IReadOnlyDictionary<IElement, AreaRead> areas)
    {
        var residues = new List<ResidueRead>();
        foreach (var node in graph.WithCompoundBody("Residue"))
        {
            foreach (var dependency in graph.DependenciesOf(node))
            {
                if (areas.TryGetValue(dependency.Definition, out var area))
                {
                    residues.Add(new ResidueRead(node, area, ResidueText(node.Read.ResidueOrTension)));
                }
            }
        }

        return [.. residues];
    }

    private static string SettlementOf(string lexical) =>
        lexical switch
        {
            "Area+" => "positive",
            "Area-" => "anti",
            "Area?" => "unsettled",
            _ => "unknown"
        };

    private static string ResidueText(IElement element) =>
        EncodedTextSurface.IsUnknownPoint(element)
            ? string.Empty
            : EncodedTextSurface.TryRead(element, out var text)
                ? text
                : ConceptDefinitionInspection.ElementDebug(element);
}
