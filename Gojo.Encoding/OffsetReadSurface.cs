using Gojo.Core;

namespace Gojo.Encoding;

/// <summary>
/// Local offset discovery over a concept definition graph. It reads offset
/// structure from compound bodies and dependencies: source support, outlet,
/// facing/side, distance sign, resolution, boundary role, orientation, band
/// pairing, residue, and crystallized reuse.
/// </summary>
public static class OffsetReadSurface
{
    public readonly record struct FacingRead(
        ConceptDefinitionDiscoverySurface.DefinitionNode Definition,
        ConceptDefinitionDiscoverySurface.DefinitionNode Target,
        string Side);

    public readonly record struct OrientationRead(
        ConceptDefinitionDiscoverySurface.DefinitionNode Definition,
        ConceptDefinitionDiscoverySurface.DefinitionNode Target,
        string Direction);

    public readonly record struct OffsetRead(
        ConceptDefinitionDiscoverySurface.DefinitionNode Definition,
        string Kind,
        string SourceShape,
        ConceptDefinitionDiscoverySurface.DefinitionNode Source,
        ConceptDefinitionDiscoverySurface.DefinitionNode? Outlet,
        FacingRead? Facing,
        string Distance,
        ConceptDefinitionDiscoverySurface.DefinitionNode? Resolution,
        string ResidueOrTension);

    public readonly record struct OffsetBoundaryRead(
        ConceptDefinitionDiscoverySurface.DefinitionNode Definition,
        string Role,
        OffsetRead? Offset,
        OrientationRead? Orientation);

    public readonly record struct OffsetBandRead(
        ConceptDefinitionDiscoverySurface.DefinitionNode Definition,
        OffsetBoundaryRead First,
        OffsetBoundaryRead Second);

    public readonly record struct OffsetResidueRead(
        ConceptDefinitionDiscoverySurface.DefinitionNode Definition,
        OffsetRead Offset,
        string ResidueOrTension);

    public readonly record struct CrystallizedOffsetRead(
        ConceptDefinitionDiscoverySurface.DefinitionNode Definition,
        OffsetRead Offset,
        ConceptDefinitionDiscoverySurface.DefinitionNode? Context,
        string ResidueOrTension);

    public readonly record struct OffsetSurfaceRead(
        FacingRead[] Facings,
        OrientationRead[] Orientations,
        OffsetRead[] Offsets,
        OffsetBoundaryRead[] Boundaries,
        OffsetBandRead[] Bands,
        OffsetResidueRead[] Residues,
        CrystallizedOffsetRead[] CrystallizedOffsets);

    public static bool TryReadOffsets(IElement ledger, out OffsetSurfaceRead read)
    {
        if (!ConceptDefinitionDiscoverySurface.TryReadLedger(ledger, out var graph))
        {
            read = default;
            return false;
        }

        return TryReadOffsets(graph, out read);
    }

    public static bool TryReadOffsets(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        out OffsetSurfaceRead read)
    {
        var facings = Facings(graph);
        var facingByDefinition = new Dictionary<IElement, FacingRead>(ReferenceEqualityComparer.Instance);
        foreach (var facing in facings)
        {
            facingByDefinition[facing.Definition.Definition] = facing;
        }

        var orientations = Orientations(graph);
        var orientationByDefinition = new Dictionary<IElement, OrientationRead>(ReferenceEqualityComparer.Instance);
        foreach (var orientation in orientations)
        {
            orientationByDefinition[orientation.Definition.Definition] = orientation;
        }

        var orientationByTarget = new Dictionary<IElement, List<OrientationRead>>(ReferenceEqualityComparer.Instance);
        foreach (var orientation in orientations)
        {
            if (!orientationByTarget.TryGetValue(orientation.Target.Definition, out var targetOrientations))
            {
                targetOrientations = [];
                orientationByTarget[orientation.Target.Definition] = targetOrientations;
            }

            targetOrientations.Add(orientation);
        }
        var orientationArraysByTarget = new Dictionary<IElement, OrientationRead[]>(ReferenceEqualityComparer.Instance);
        foreach (var orientationGroup in orientationByTarget)
        {
            orientationArraysByTarget[orientationGroup.Key] = [.. orientationGroup.Value];
        }

        var offsets = Offsets(graph, facingByDefinition);
        var offsetByDefinition = new Dictionary<IElement, OffsetRead>(ReferenceEqualityComparer.Instance);
        foreach (var offset in offsets)
        {
            offsetByDefinition[offset.Definition.Definition] = offset;
        }

        var boundaries = Boundaries(graph, offsetByDefinition, orientationByDefinition, orientationArraysByTarget);
        var boundaryByDefinition = new Dictionary<IElement, OffsetBoundaryRead>(ReferenceEqualityComparer.Instance);
        foreach (var boundary in boundaries)
        {
            boundaryByDefinition[boundary.Definition.Definition] = boundary;
        }

        var bands = Bands(graph, boundaryByDefinition);
        var residues = Residues(graph, offsetByDefinition);
        var crystallized = CrystallizedOffsets(graph, offsetByDefinition);

        read = new OffsetSurfaceRead(facings, orientations, offsets, boundaries, bands, residues, crystallized);
        return offsets.Length > 0 ||
            boundaries.Length > 0 ||
            bands.Length > 0 ||
            residues.Length > 0 ||
            crystallized.Length > 0;
    }

    private static FacingRead[] Facings(ConceptDefinitionDiscoverySurface.DefinitionGraph graph)
    {
        var facings = new List<FacingRead>();
        foreach (var node in graph.WithCompoundBody("Face"))
        {
            if (!graph.TryReadCompoundBody(node, out var body) ||
                body.PartNames.Length < 2)
            {
                continue;
            }

            var dependencies = graph.DependenciesOf(node);
            if (dependencies.Count == 0)
            {
                continue;
            }

            facings.Add(new FacingRead(node, dependencies[0], body.PartNames[1]));
        }

        return [.. facings];
    }

    private static OrientationRead[] Orientations(ConceptDefinitionDiscoverySurface.DefinitionGraph graph)
    {
        var orientations = new List<OrientationRead>();
        foreach (var node in graph.WithCompoundBody("Orient"))
        {
            if (!graph.TryReadCompoundBody(node, out var body) ||
                body.PartNames.Length < 2)
            {
                continue;
            }

            var dependencies = graph.DependenciesOf(node);
            if (dependencies.Count == 0)
            {
                continue;
            }

            orientations.Add(new OrientationRead(node, dependencies[0], body.PartNames[1]));
        }

        return [.. orientations];
    }

    private static OffsetRead[] Offsets(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        IReadOnlyDictionary<IElement, FacingRead> facingByDefinition)
    {
        var offsets = new List<OffsetRead>();
        foreach (var node in graph.Nodes)
        {
            if (!graph.TryReadCompoundBody(node, out var body) ||
                body.Lexical is not ("Off" or "OffOpen" or "OffClosed" or "Off?"))
            {
                continue;
            }

            var dependencies = graph.DependenciesOf(node);
            if (dependencies.Count == 0)
            {
                continue;
            }

            if (body.Lexical == "OffOpen")
            {
                var sourceOffset = dependencies
                    .Select(dependency => offsets.FirstOrDefault(offset => ReferenceEquals(offset.Definition.Definition, dependency.Definition)))
                    .FirstOrDefault(offset => offset.Definition.Definition is not null);
                if (sourceOffset.Definition.Definition is null)
                {
                    continue;
                }

                offsets.Add(sourceOffset with
                {
                    Definition = node,
                    Kind = "open",
                    ResidueOrTension = ResidueText(node.Read.ResidueOrTension)
                });
                continue;
            }

            var source = dependencies[0];
            var outlet = dependencies.Count > 1 ? dependencies[1] : (ConceptDefinitionDiscoverySurface.DefinitionNode?)null;
            var facing = dependencies
                .Select(dependency => facingByDefinition.TryGetValue(dependency.Definition, out var read) ? read : (FacingRead?)null)
                .FirstOrDefault(read => read.HasValue);
            var distance = dependencies
                .Select(AtomBodyOrEmpty(graph))
                .FirstOrDefault(value => value is "+d" or "-d" or "+r" or "-r") ?? "?";
            var resolution = dependencies
                .FirstOrDefault(dependency =>
                    graph.TryReadCompoundBody(dependency, out var dependencyBody) &&
                    dependencyBody.Lexical == "Res");

            offsets.Add(new OffsetRead(
                node,
                KindOf(body.Lexical),
                SourceShapeOf(graph, source),
                source,
                outlet,
                facing,
                distance,
                resolution.Definition is null ? null : resolution,
                ResidueText(node.Read.ResidueOrTension)));
        }

        return [.. offsets];
    }

    private static OffsetBoundaryRead[] Boundaries(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        IReadOnlyDictionary<IElement, OffsetRead> offsets,
        IReadOnlyDictionary<IElement, OrientationRead> orientations,
        IReadOnlyDictionary<IElement, OrientationRead[]> orientationByTarget)
    {
        var boundaries = new List<OffsetBoundaryRead>();
        foreach (var node in graph.Nodes)
        {
            if (!graph.TryReadCompoundBody(node, out var body) ||
                body.Lexical is not ("Bd0" or "Bd1" or "BdSelf"))
            {
                continue;
            }

            var dependencies = graph.DependenciesOf(node);
            var offset = dependencies
                .Select(dependency => offsets.TryGetValue(dependency.Definition, out var read) ? read : (OffsetRead?)null)
                .FirstOrDefault(read => read.HasValue);
            OrientationRead? orientation = null;
            if (orientationByTarget.TryGetValue(node.Definition, out var directOrientations) && directOrientations.Length > 0)
            {
                orientation = directOrientations[0];
            }
            else
            {
                orientation = dependencies
                    .Select(dependency =>
                        orientations.TryGetValue(dependency.Definition, out var dependencyOrientation)
                            ? dependencyOrientation
                            : orientationByTarget.TryGetValue(dependency.Definition, out var reads) && reads.Length > 0
                                ? reads[0]
                                : (OrientationRead?)null)
                    .FirstOrDefault(read => read.HasValue);
            }

            boundaries.Add(new OffsetBoundaryRead(
                node,
                BoundaryRole(body.Lexical, offset),
                offset,
                orientation));
        }

        return [.. boundaries];
    }

    private static OffsetBandRead[] Bands(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        IReadOnlyDictionary<IElement, OffsetBoundaryRead> boundaries)
    {
        var bands = new List<OffsetBandRead>();
        foreach (var node in graph.WithCompoundBody("Band"))
        {
            var dependencies = graph.DependenciesOf(node);
            if (dependencies.Count < 2 ||
                !boundaries.TryGetValue(dependencies[0].Definition, out var first) ||
                !boundaries.TryGetValue(dependencies[1].Definition, out var second))
            {
                continue;
            }

            bands.Add(new OffsetBandRead(node, first, second));
        }

        return [.. bands];
    }

    private static OffsetResidueRead[] Residues(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        IReadOnlyDictionary<IElement, OffsetRead> offsets)
    {
        var residues = new List<OffsetResidueRead>();
        foreach (var node in graph.WithCompoundBody("Residue"))
        {
            foreach (var dependency in graph.DependenciesOf(node))
            {
                if (offsets.TryGetValue(dependency.Definition, out var offset))
                {
                    residues.Add(new OffsetResidueRead(node, offset, ResidueText(node.Read.ResidueOrTension)));
                }
            }
        }

        return [.. residues];
    }

    private static CrystallizedOffsetRead[] CrystallizedOffsets(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        IReadOnlyDictionary<IElement, OffsetRead> offsets)
    {
        var reads = new List<CrystallizedOffsetRead>();
        foreach (var node in graph.Nodes)
        {
            if (!graph.TryReadReferenceBody(node, out var reference))
            {
                continue;
            }

            var dependencies = graph.DependenciesOf(node);
            var offset = dependencies
                .Select(dependency => offsets.TryGetValue(dependency.Definition, out var read) ? read : (OffsetRead?)null)
                .FirstOrDefault(read => read.HasValue);
            if (!offset.HasValue)
            {
                continue;
            }

            var context = dependencies.FirstOrDefault(dependency =>
                !ReferenceEquals(dependency.Definition, offset.Value.Definition.Definition));
            reads.Add(new CrystallizedOffsetRead(
                node,
                offset.Value,
                context.Definition is null ? null : context,
                ResidueText(reference.ResidueOrTension)));
        }

        return [.. reads];
    }

    private static Func<ConceptDefinitionDiscoverySurface.DefinitionNode, string> AtomBodyOrEmpty(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph) =>
        node => graph.TryReadAtomBody(node, out var atom) ? atom : string.Empty;

    private static string KindOf(string lexical) =>
        lexical switch
        {
            "OffOpen" => "open",
            "OffClosed" => "closed",
            "Off?" => "unresolved",
            _ => "offset"
        };

    private static string BoundaryRole(string lexical, OffsetRead? offset) =>
        lexical switch
        {
            "Bd0" => "source",
            "Bd1" => "terminal",
            "BdSelf" when offset.HasValue => "self-terminal",
            "BdSelf" => "self-source",
            _ => "unknown"
        };

    private static string SourceShapeOf(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        ConceptDefinitionDiscoverySurface.DefinitionNode source)
    {
        if (graph.TryReadCompoundBody(source, out var sourceBody))
        {
            if (sourceBody.Lexical == "Circle")
            {
                return "circle";
            }

            if (sourceBody.Lexical == "BdSelf")
            {
                var dependencies = graph.DependenciesOf(source);
                return dependencies.Any(dependency =>
                    graph.TryReadCompoundBody(dependency, out var dependencyBody) &&
                    dependencyBody.Lexical == "Circle")
                    ? "circumference"
                    : "self-boundary";
            }

            return sourceBody.Lexical;
        }

        var debug = ConceptDefinitionInspection.ElementDebug(source.Read.Body);
        return debug.StartsWith("S(", StringComparison.Ordinal) ? "segment" : debug;
    }

    private static string ResidueText(IElement element) =>
        EncodedTextSurface.IsUnknownPoint(element)
            ? string.Empty
            : EncodedTextSurface.TryRead(element, out var text)
                ? text
                : ConceptDefinitionInspection.ElementDebug(element);
}
