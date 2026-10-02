using Gojo.Core;

namespace Gojo.Encoding.Structures;

/// <summary>
/// Small relation-shaped bundle for diagnostic snapshots plus observer paths.
/// It is a packaging surface for inspection; sparse Gojo structure, residue,
/// and locally readable paths remain the conceptual source of meaning.
/// </summary>
public static class GojoStructureEncodingBundleSurface
{
    public readonly record struct BundleRead(
        string SourceId,
        IElement[] Entries,
        IElement ResidueOrTension);

    public readonly record struct EntryRead(
        string EntryId,
        string Expression,
        string EncodingNote,
        IElement StructureEncoding,
        Relation[] ObserverPaths,
        IElement ResidueOrTension);

    public static Relation Create(
        Carrier root,
        string sourceId,
        IReadOnlyList<IElement> entries,
        IElement? residueOrTension = null) =>
        Relation.OrderedGroup(
            EncodedTextSurface.RootOrNew(root),
            EncodedTextSurface.Create(new Carrier(), sourceId),
            Frame(new Carrier(), entries ?? []),
            residueOrTension ?? EncodedTextSurface.UnknownPoint());

    public static Relation Entry(
        Carrier root,
        string entryId,
        string expression,
        string? encodingNote,
        IElement structureEncoding,
        IReadOnlyList<Relation> observerPaths,
        IElement? residueOrTension = null) =>
        Relation.OrderedGroup(
            EncodedTextSurface.RootOrNew(root),
            EncodedTextSurface.Create(new Carrier(), entryId),
            EncodedTextSurface.Create(new Carrier(), expression),
            EncodedTextSurface.Create(new Carrier(), encodingNote ?? string.Empty),
            structureEncoding ?? EncodedTextSurface.UnknownPoint(),
            PathFrame(new Carrier(), observerPaths ?? []),
            residueOrTension ?? EncodedTextSurface.UnknownPoint());

    public static Relation Frame(Carrier root, IReadOnlyList<IElement> elements) =>
        Relation.OrderedGroup(EncodedTextSurface.RootOrNew(root), [.. elements]);

    public static Relation PathFrame(Carrier root, IReadOnlyList<Relation> paths) =>
        Relation.OrderedGroup(EncodedTextSurface.RootOrNew(root), [.. paths]);

    public static bool TryRead(IElement element, out BundleRead read)
    {
        if (element is Relation bundle &&
            EncodedTextSurface.TryReadRootElements(bundle, out var elements) &&
            elements.Length >= 3 &&
            EncodedTextSurface.TryRead(elements[0], out var sourceId) &&
            TryReadFrame(elements[1], out var entries))
        {
            read = new BundleRead(sourceId, entries, elements[2]);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadEntry(IElement element, out EntryRead read)
    {
        if (element is Relation entry &&
            EncodedTextSurface.TryReadRootElements(entry, out var elements) &&
            elements.Length >= 6 &&
            EncodedTextSurface.TryRead(elements[0], out var entryId) &&
            EncodedTextSurface.TryRead(elements[1], out var expression) &&
            EncodedTextSurface.TryRead(elements[2], out var encodingNote) &&
            TryReadPathFrame(elements[4], out var observerPaths))
        {
            read = new EntryRead(
                entryId,
                expression,
                encodingNote,
                elements[3],
                observerPaths,
                elements[5]);
            return true;
        }

        read = default;
        return false;
    }

    private static bool TryReadFrame(IElement element, out IElement[] elements)
    {
        if (element is Relation frame &&
            EncodedTextSurface.TryReadRootElements(frame, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    private static bool TryReadPathFrame(IElement element, out Relation[] paths)
    {
        if (TryReadFrame(element, out var elements) &&
            elements.All(item => item is Relation path && GojoObserverPathSurface.TryRead(path, out _)))
        {
            paths = [.. elements.Cast<Relation>()];
            return true;
        }

        paths = [];
        return false;
    }
}
