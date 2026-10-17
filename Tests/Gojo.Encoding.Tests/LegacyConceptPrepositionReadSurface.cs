using Gojo.Core;

namespace Gojo.Encoding;

/// <summary>
/// Legacy inspection surface for the old preposition fixture. It remains in the
/// test project so the historical JSON fixture can round-trip, but it is not an
/// active grammar architecture.
/// </summary>
internal static class LegacyConceptPrepositionReadSurface
{
    public readonly record struct PrepositionRead(
        ConceptDefinitionDiscoverySurface.DefinitionNode Definition,
        string Kind,
        string Body,
        string[] Ingredients,
        string ResidueOrTension);

    public readonly record struct PrepositionSurfaceRead(PrepositionRead[] Reads)
    {
        public PrepositionRead[] Of(string kind) =>
            [.. Reads.Where(read => read.Kind == kind)];

        public bool Contains(string kind) =>
            Reads.Any(read => read.Kind == kind);
    }

    public static bool TryReadPrepositions(IElement ledger, out PrepositionSurfaceRead read)
    {
        if (!ConceptDefinitionDiscoverySurface.TryReadLedger(ledger, out var graph))
        {
            read = default;
            return false;
        }

        return TryReadPrepositions(graph, out read);
    }

    public static bool TryReadPrepositions(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        out PrepositionSurfaceRead read)
    {
        var reads = new List<PrepositionRead>();
        var readByDefinition = new Dictionary<IElement, PrepositionRead>(ReferenceEqualityComparer.Instance);

        void Add(PrepositionRead preposition)
        {
            reads.Add(preposition);
            readByDefinition[preposition.Definition.Definition] = preposition;
        }

        foreach (var node in graph.Nodes)
        {
            if (IsSlot(graph, node))
            {
                Add(Read(graph, node, "open-role-slot"));
            }
        }

        foreach (var node in graph.WithCompoundBody("RoleFrame"))
        {
            Add(Read(graph, node, "role-frame"));
        }

        foreach (var node in graph.Nodes)
        {
            if (ResolutionFacetStructureSurface.TryReadComparison(node.Read.Body, out _))
            {
                Add(Read(graph, node, HasRoleSlotDependency(graph, node) ? "comparison-template" : "bound-comparison"));
            }
        }

        foreach (var node in graph.WithCompoundBody("Scale"))
        {
            Add(Read(graph, node, "parameter-scale"));
        }

        foreach (var node in graph.WithCompoundBody("Zone"))
        {
            Add(Read(graph, node, "focus-zone"));
        }

        foreach (var node in graph.WithCompoundBody("Focus"))
        {
            Add(Read(graph, node, "focus-read"));
        }

        foreach (var node in graph.Nodes)
        {
            if (!graph.TryReadReferenceBody(node, out var reference))
            {
                continue;
            }

            if (!TryFindRead(graph, readByDefinition, reference.DefinitionOrAddress, graph.DependenciesOf(node), out var referenced) ||
                referenced.Kind != "focus-read")
            {
                continue;
            }

            Add(new PrepositionRead(
                node,
                "crystallized-focus-read",
                ConceptDefinitionInspection.ElementDebug(node.Read.Body),
                IngredientsForReference(graph, node, reference, referenced.Definition),
                ResidueText(reference.ResidueOrTension)));
        }

        foreach (var node in graph.WithCompoundBody("BindRoles"))
        {
            Add(Read(graph, node, "role-binding"));
        }

        foreach (var node in graph.WithCompoundBody("Apply"))
        {
            var dependencies = graph.DependenciesOf(node);
            var hasCrystallizedFocus = dependencies.Any(dependency =>
                readByDefinition.TryGetValue(dependency.Definition, out var dependencyRead) &&
                dependencyRead.Kind == "crystallized-focus-read");
            var hasBinding = dependencies.Any(dependency =>
                readByDefinition.TryGetValue(dependency.Definition, out var dependencyRead) &&
                dependencyRead.Kind == "role-binding");
            var hasComparison = dependencies.Any(dependency =>
                readByDefinition.TryGetValue(dependency.Definition, out var dependencyRead) &&
                dependencyRead.Kind == "bound-comparison");
            if (hasCrystallizedFocus && hasBinding && hasComparison)
            {
                Add(Read(graph, node, "applied-preposition-read"));
            }
        }

        read = new PrepositionSurfaceRead([.. reads]);
        return reads.Count > 0;
    }

    private static bool TryFindRead(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        IReadOnlyDictionary<IElement, PrepositionRead> readByDefinition,
        IElement definitionOrAddress,
        IReadOnlyList<ConceptDefinitionDiscoverySurface.DefinitionNode> dependencies,
        out PrepositionRead read)
    {
        if (readByDefinition.TryGetValue(definitionOrAddress, out read))
        {
            return true;
        }

        if (ConceptAddressSurface.TryReadAtom(definitionOrAddress, out var name, out _) &&
            graph.TryFindByName(name, out var named) &&
            readByDefinition.TryGetValue(named.Definition, out read))
        {
            return true;
        }

        foreach (var dependency in dependencies)
        {
            if (readByDefinition.TryGetValue(dependency.Definition, out read))
            {
                return true;
            }
        }

        read = default;
        return false;
    }

    private static string[] IngredientsForReference(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        ConceptDefinitionDiscoverySurface.DefinitionNode node,
        ConceptDefinitionSurface.ReferenceRead reference,
        ConceptDefinitionDiscoverySurface.DefinitionNode referenced)
    {
        var dependencies = graph.DependenciesOf(node);
        var context = graph.TryFind(reference.Context, out var contextNode)
            ? contextNode
            : dependencies.FirstOrDefault(dependency => !ReferenceEquals(dependency.Definition, referenced.Definition));

        return context.Definition is null
            ? [referenced.Name]
            : [referenced.Name, context.Name];
    }

    private static PrepositionRead Read(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        ConceptDefinitionDiscoverySurface.DefinitionNode node,
        string kind) =>
        new(
            node,
            kind,
            ConceptDefinitionInspection.ElementDebug(node.Read.Body),
            [.. graph.DependenciesOf(node).Select(dependency => dependency.Name)],
            ResidueText(node.Read.ResidueOrTension));

    private static bool HasRoleSlotDependency(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        ConceptDefinitionDiscoverySurface.DefinitionNode node) =>
        graph.DependenciesOf(node).Any(dependency => IsSlot(graph, dependency));

    private static bool IsSlot(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        ConceptDefinitionDiscoverySurface.DefinitionNode node) =>
        ResolutionFacetStructureSurface.TryReadSlot(node.Read.Body, out _);

    private static string ResidueText(IElement element) =>
        IsUnknownPoint(element)
            ? string.Empty
            : EncodedTextSurface.TryRead(element, out var text)
                ? text
                : ConceptDefinitionInspection.ElementDebug(element);

    private static bool IsUnknownPoint(IElement element) =>
        element is Relation relation &&
        relation.Left is CarrierLocation location &&
        relation.Right is Carrier carrier &&
        carrier.IsUnknown &&
        location.Carrier.IsUnknown &&
        location.Ticks == 0;
}
