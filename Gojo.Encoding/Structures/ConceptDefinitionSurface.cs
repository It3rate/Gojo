using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Encoding.Structures;

/// <summary>
/// Relation-shaped definition and crystallization surface. A definition is an
/// ordered local read: name/address, body, provenance, dependency frame, and
/// residue/tension. The helper owns no concept meaning; it only makes the
/// connected structure easy to build and inspect.
/// </summary>
public static class ConceptDefinitionSurface
{
    public readonly record struct DefinitionRead(
        IElement Name,
        IElement Body,
        IElement Provenance,
        IElement[] Dependencies,
        IElement ResidueOrTension);

    public readonly record struct ReferenceRead(
        IElement DefinitionOrAddress,
        IElement Context,
        IElement ResidueOrTension);

    public static Relation Create(
        Carrier root,
        string name,
        IElement body,
        IReadOnlyList<IElement>? dependencies = null,
        IElement? provenance = null,
        IElement? residueOrTension = null) =>
        Create(
            root,
            ConceptAddressSurface.Atom(new Carrier(), name),
            body,
            dependencies,
            provenance,
            residueOrTension);

    public static Relation Create(
        Carrier root,
        IElement name,
        IElement body,
        IReadOnlyList<IElement>? dependencies = null,
        IElement? provenance = null,
        IElement? residueOrTension = null,
        Carrier? dependencyFrameRoot = null) =>
        Relation.OrderedGroup(
            EncodedTextSurface.RootOrNew(root),
            name ?? EncodedTextSurface.UnknownPoint(),
            body ?? EncodedTextSurface.UnknownPoint(),
            provenance ?? body ?? EncodedTextSurface.UnknownPoint(),
            DependencyFrame(dependencyFrameRoot ?? new Carrier(), dependencies ?? []),
            residueOrTension ?? EncodedTextSurface.UnknownPoint());

    public static Relation DependencyFrame(Carrier root, IReadOnlyList<IElement> dependencies) =>
        Relation.OrderedGroup(EncodedTextSurface.RootOrNew(root), [.. dependencies]);

    public static Relation Ledger(Carrier root, IReadOnlyList<Relation> definitions) =>
        Relation.OrderedGroup(EncodedTextSurface.RootOrNew(root), [.. definitions]);

    public static Relation Reference(
        Carrier root,
        IElement definitionOrAddress,
        IElement? context = null,
        IElement? residueOrTension = null) =>
        Relation.OrderedGroup(
            EncodedTextSurface.RootOrNew(root),
            definitionOrAddress ?? EncodedTextSurface.UnknownPoint(),
            context ?? EncodedTextSurface.UnknownPoint(),
            residueOrTension ?? EncodedTextSurface.UnknownPoint());

    public static bool TryRead(IElement element, out DefinitionRead read)
    {
        if (element is Relation definition &&
            EncodedTextSurface.TryReadRootElements(definition, out var elements) &&
            elements.Length == 5 &&
            IsDefinitionName(elements[0]) &&
            TryReadDependencyFrame(elements[3], out var dependencies))
        {
            read = new DefinitionRead(
                elements[0],
                elements[1],
                elements[2],
                dependencies,
                elements[4]);
            return true;
        }

        read = default;
        return false;
    }

    private static bool IsDefinitionName(IElement element) =>
        ConceptAddressSurface.TryReadAtom(element, out _, out _) ||
        ConceptAddressSurface.TryReadCompound(element, out _, out _, out _, out _) ||
        (EncodedTextSurface.TryRead(element, out var text) && text.Length > 0) ||
        EncodedTextSurface.TryReadPoint(element, out _);

    public static bool TryReadDependencyFrame(IElement element, out IElement[] dependencies)
    {
        if (element is Relation frame &&
            EncodedTextSurface.TryReadRootElements(frame, out dependencies))
        {
            return true;
        }

        dependencies = [];
        return false;
    }

    public static bool TryReadLedger(IElement element, out Relation[] definitions)
    {
        if (element is Relation ledger &&
            EncodedTextSurface.TryReadRootElements(ledger, out var elements) &&
            elements.All(item => item is Relation definition && TryRead(definition, out _)))
        {
            definitions = [.. elements.Cast<Relation>()];
            return true;
        }

        definitions = [];
        return false;
    }

    public static bool TryReadReference(IElement element, out ReferenceRead read)
    {
        if (element is Relation reference &&
            EncodedTextSurface.TryReadRootElements(reference, out var elements) &&
            elements.Length == 3)
        {
            read = new ReferenceRead(elements[0], elements[1], elements[2]);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryCreateWitness(Carrier root, Relation definition, out Relation witness)
    {
        if (TryRead(definition, out var read))
        {
            witness = ReadWitness.Create(
                EncodedTextSurface.RootOrNew(root),
                read.Body,
                definition,
                read.Name,
                read.ResidueOrTension,
                read.Provenance);
            return true;
        }

        witness = null!;
        return false;
    }
}
