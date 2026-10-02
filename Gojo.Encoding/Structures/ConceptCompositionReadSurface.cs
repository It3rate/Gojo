using Gojo.Core;

namespace Gojo.Encoding.Structures;

/// <summary>
/// Small local reader for useful noun compositions. This is an inspection
/// surface over definition graphs: it recognizes composed shapes from
/// dependency structure such as Band, Area+/Area-, Support, Clearance, Residue,
/// and crystallized references. It is not the canonical source of meaning.
/// </summary>
public static class ConceptCompositionReadSurface
{
    public readonly record struct CompositionRead(
        ConceptDefinitionDiscoverySurface.DefinitionNode Definition,
        string Kind,
        string Body,
        string[] Ingredients,
        string ResidueOrTension);

    public readonly record struct CompositionSurfaceRead(CompositionRead[] Reads)
    {
        public CompositionRead[] Of(string kind) =>
            [.. Reads.Where(read => read.Kind == kind)];

        public bool Contains(string kind) =>
            Reads.Any(read => read.Kind == kind);
    }

    public static bool TryReadCompositions(IElement ledger, out CompositionSurfaceRead read)
    {
        if (!ConceptDefinitionDiscoverySurface.TryReadLedger(ledger, out var graph))
        {
            read = default;
            return false;
        }

        return TryReadCompositions(graph, out read);
    }

    public static bool TryReadCompositions(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        out CompositionSurfaceRead read)
    {
        var reads = new List<CompositionRead>();
        var readByDefinition = new Dictionary<IElement, CompositionRead>(ReferenceEqualityComparer.Instance);

        void Add(CompositionRead composition)
        {
            reads.Add(composition);
            readByDefinition[composition.Definition.Definition] = composition;
        }

        var bandNodes = graph.Nodes.Where(node => HasBody(graph, node, "Band")).ToArray();
        foreach (var band in bandNodes)
        {
            Add(Read(graph, band, "offset-band"));
        }

        foreach (var node in graph.Nodes.Where(node => HasBody(graph, node, "Area+") || HasBody(graph, node, "Area-")))
        {
            var dependencies = graph.DependenciesOf(node);
            if (dependencies.Any(dependency => bandNodes.Any(band => ReferenceEquals(band.Definition, dependency.Definition))))
            {
                Add(Read(graph, node, HasBody(graph, node, "Area+") ? "band-area" : "keepout-area"));
            }
            else if (HasBody(graph, node, "Area-"))
            {
                Add(Read(graph, node, "hole-area"));
            }
        }

        foreach (var node in graph.Nodes.Where(node => HasBody(graph, node, "Support")))
        {
            var dependencies = graph.DependenciesOf(node);
            var hasPositive = dependencies.Any(dependency => HasBody(graph, dependency, "Area+"));
            var hasAnti = dependencies.Any(dependency => HasBody(graph, dependency, "Area-"));
            if (hasPositive && hasAnti)
            {
                Add(Read(graph, node, "support-with-hole"));
            }
        }

        foreach (var node in graph.Nodes.Where(node => HasBody(graph, node, "Clearance")))
        {
            Add(Read(graph, node, "clearance-rule"));
        }

        foreach (var node in graph.Nodes)
        {
            if (!graph.TryReadReferenceBody(node, out var reference))
            {
                continue;
            }

            var dependencies = graph.DependenciesOf(node);
            if (!TryFindReferencedComposition(
                    graph,
                    readByDefinition,
                    reference.DefinitionOrAddress,
                    dependencies,
                    out var referencedComposition))
            {
                continue;
            }

            var context = ContextOf(graph, reference.Context, dependencies, referencedComposition.Definition);
            var ingredients = context.Definition is null
                ? [referencedComposition.Definition.Name]
                : new[] { referencedComposition.Definition.Name, context.Name };
            Add(new CompositionRead(
                node,
                $"crystallized-{referencedComposition.Kind}",
                ConceptDefinitionInspection.ElementDebug(node.Read.Body),
                ingredients,
                ResidueText(reference.ResidueOrTension)));
        }

        read = new CompositionSurfaceRead([.. reads]);
        return reads.Count > 0;
    }

    private static bool TryFindReferencedComposition(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        IReadOnlyDictionary<IElement, CompositionRead> readByDefinition,
        IElement definitionOrAddress,
        IReadOnlyList<ConceptDefinitionDiscoverySurface.DefinitionNode> dependencies,
        out CompositionRead composition)
    {
        if (readByDefinition.TryGetValue(definitionOrAddress, out composition))
        {
            return true;
        }

        if (ConceptAddressSurface.TryReadAtom(definitionOrAddress, out var name, out _) &&
            graph.TryFindByName(name, out var named) &&
            readByDefinition.TryGetValue(named.Definition, out composition))
        {
            return true;
        }

        foreach (var dependency in dependencies)
        {
            if (readByDefinition.TryGetValue(dependency.Definition, out composition))
            {
                return true;
            }
        }

        composition = default;
        return false;
    }

    private static ConceptDefinitionDiscoverySurface.DefinitionNode ContextOf(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        IElement context,
        IReadOnlyList<ConceptDefinitionDiscoverySurface.DefinitionNode> dependencies,
        ConceptDefinitionDiscoverySurface.DefinitionNode referencedComposition)
    {
        if (graph.TryFind(context, out var contextNode))
        {
            return contextNode;
        }

        if (ConceptAddressSurface.TryReadAtom(context, out var contextName, out _) &&
            graph.TryFindByName(contextName, out contextNode))
        {
            return contextNode;
        }

        return dependencies.FirstOrDefault(dependency =>
            !ReferenceEquals(dependency.Definition, referencedComposition.Definition));
    }

    private static CompositionRead Read(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        ConceptDefinitionDiscoverySurface.DefinitionNode node,
        string kind) =>
        new(
            node,
            kind,
            ConceptDefinitionInspection.ElementDebug(node.Read.Body),
            [.. graph.DependenciesOf(node).Select(dependency => dependency.Name)],
            ResidueText(node.Read.ResidueOrTension));

    private static bool HasBody(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        ConceptDefinitionDiscoverySurface.DefinitionNode node,
        string lexical) =>
        graph.TryReadCompoundBody(node, out var body) &&
        body.Lexical == lexical;

    private static string ResidueText(IElement element) =>
        EncodedTextSurface.IsUnknownPoint(element)
            ? string.Empty
            : EncodedTextSurface.TryRead(element, out var text)
                ? text
                : ConceptDefinitionInspection.ElementDebug(element);
}
