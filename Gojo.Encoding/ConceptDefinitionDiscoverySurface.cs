using Gojo.Core;

namespace Gojo.Encoding;

/// <summary>
/// Local read surface over a definition ledger. This is intentionally generic:
/// it reads definition names, dependency edges, compound bodies, references,
/// and ordered participant groups without deciding what the concepts mean.
/// </summary>
public static class ConceptDefinitionDiscoverySurface
{
    public readonly record struct DefinitionNode(
        int Index,
        string Name,
        Relation Definition,
        ConceptDefinitionSurface.DefinitionRead Read);

    public readonly record struct CompoundBodyRead(
        string Lexical,
        IElement[] Parts,
        string[] PartNames,
        IElement[] Contexts,
        IElement ResidueOrTension);

    public sealed class DefinitionGraph
    {
        private readonly Dictionary<IElement, DefinitionNode> byDefinition;
        private readonly Dictionary<string, List<DefinitionNode>> byName;

        internal DefinitionGraph(DefinitionNode[] nodes)
        {
            Nodes = nodes;
            byDefinition = new Dictionary<IElement, DefinitionNode>(ReferenceEqualityComparer.Instance);
            byName = new Dictionary<string, List<DefinitionNode>>(StringComparer.Ordinal);

            foreach (var node in nodes)
            {
                byDefinition[node.Definition] = node;
                if (!byName.TryGetValue(node.Name, out var named))
                {
                    named = [];
                    byName[node.Name] = named;
                }

                named.Add(node);
            }
        }

        public IReadOnlyList<DefinitionNode> Nodes { get; }

        public bool TryFind(IElement definition, out DefinitionNode node) =>
            byDefinition.TryGetValue(definition, out node);

        public bool TryFindByName(string name, out DefinitionNode node)
        {
            if (byName.TryGetValue(name, out var nodes) && nodes.Count > 0)
            {
                node = nodes[0];
                return true;
            }

            node = default;
            return false;
        }

        public IReadOnlyList<DefinitionNode> FindAllByName(string name) =>
            byName.TryGetValue(name, out var nodes)
                ? nodes
                : [];

        public IReadOnlyList<DefinitionNode> DependenciesOf(DefinitionNode node)
        {
            var dependencies = new List<DefinitionNode>();
            foreach (var dependency in node.Read.Dependencies)
            {
                if (TryFind(dependency, out var dependencyNode))
                {
                    dependencies.Add(dependencyNode);
                }
            }

            return dependencies;
        }

        public bool DependsOn(DefinitionNode node, DefinitionNode dependency) =>
            node.Read.Dependencies.Any(item => ReferenceEquals(item, dependency.Definition));

        public IEnumerable<DefinitionNode> WithCompoundBody(string lexical) =>
            Nodes.Where(node =>
                TryReadCompoundBody(node, out var body) &&
                body.Lexical == lexical);

        public bool TryReadCompoundBody(DefinitionNode node, out CompoundBodyRead body)
        {
            if (ConceptAddressSurface.TryReadCompound(
                    node.Read.Body,
                    out var lexical,
                    out var parts,
                    out var contexts,
                    out var residueOrTension))
            {
                body = new CompoundBodyRead(
                    lexical,
                    parts,
                    [.. parts.Select(PartName)],
                    contexts,
                    residueOrTension);
                return true;
            }

            body = default;
            return false;
        }

        public bool TryReadAtomBody(DefinitionNode node, out string name)
        {
            if (ConceptAddressSurface.TryReadAtom(node.Read.Body, out name, out _))
            {
                return true;
            }

            name = string.Empty;
            return false;
        }

        public bool TryReadReferenceBody(DefinitionNode node, out ConceptDefinitionSurface.ReferenceRead reference) =>
            ConceptDefinitionSurface.TryReadReference(node.Read.Body, out reference);

        public bool TryReadDefinitionGroupBody(DefinitionNode node, out DefinitionNode[] participants)
        {
            if (node.Read.Body is Relation body &&
                !ConceptAddressSurface.TryReadAtom(body, out _, out _) &&
                !ConceptAddressSurface.TryReadCompound(body, out _, out _, out _, out _) &&
                !ConceptDefinitionSurface.TryRead(body, out _) &&
                !ConceptDefinitionSurface.TryReadReference(body, out _) &&
                body.Right is Carrier holder &&
                body.TryReadOrderedElementsOn(holder, out var elements))
            {
                var participantNodes = new List<DefinitionNode>();
                foreach (var element in elements)
                {
                    if (!TryFind(element, out var participant))
                    {
                        participants = [];
                        return false;
                    }

                    participantNodes.Add(participant);
                }

                participants = [.. participantNodes];
                return true;
            }

            participants = [];
            return false;
        }
    }

    public static bool TryReadLedger(IElement ledger, out DefinitionGraph graph)
    {
        if (!ConceptDefinitionSurface.TryReadLedger(ledger, out var definitions))
        {
            graph = default!;
            return false;
        }

        var nodes = new List<DefinitionNode>();
        for (var index = 0; index < definitions.Length; index++)
        {
            var definition = definitions[index];
            if (!ConceptDefinitionSurface.TryRead(definition, out var read))
            {
                graph = default!;
                return false;
            }

            nodes.Add(new DefinitionNode(index, NameOf(read.Name), definition, read));
        }

        graph = new DefinitionGraph([.. nodes]);
        return true;
    }

    private static string NameOf(IElement element)
    {
        if (ConceptAddressSurface.TryReadAtom(element, out var atom, out _))
        {
            return atom;
        }

        if (ConceptAddressSurface.TryReadCompound(element, out var lexical, out _, out _, out _))
        {
            return lexical;
        }

        if (EncodedTextSurface.TryRead(element, out var text))
        {
            return text;
        }

        if (EncodedTextSurface.TryReadPoint(element, out var location))
        {
            return $"local-{location.Ticks}";
        }

        return "?";
    }

    private static string PartName(IElement element) =>
        ConceptAddressSurface.TryReadAtom(element, out var atom, out _)
            ? atom
            : ConceptAddressSurface.TryReadCompound(element, out var lexical, out _, out _, out _)
                ? lexical
                : ConceptDefinitionSurface.TryRead(element, out var definition)
                    ? NameOf(definition.Name)
                    : EncodedTextSurface.TryRead(element, out var text)
                        ? text
                        : EncodedTextSurface.TryReadPoint(element, out var location)
                            ? $"local-{location.Ticks}"
                            : "?";
}
