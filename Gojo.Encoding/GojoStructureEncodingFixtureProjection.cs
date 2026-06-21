using Gojo.Core;

namespace Gojo.Encoding;

/// <summary>
/// Builds compact, human-facing JSON fixture entries from one live Gojo structure.
/// The fixture projection is intentionally lossy: it keeps full counts and a
/// small number of row examples, while the relation-shaped encoding surface is
/// still produced from the actual source element. Each call encodes exactly one
/// source structure, not a glossary page or bundle of sibling examples.
/// </summary>
public static class GojoStructureEncodingFixtureProjection
{
    public readonly record struct Source(
        string EntryId,
        string Represents,
        string Expression,
        string? EncodingNote,
        IElement Structure);

    public static GojoEncodingJsonBundleEntry EntryFromStructure(Source source, int rowLimit = 1)
    {
        var surfaceEntry = EntrySurfaceFromStructure(source);
        var entry = GojoStructureEncodingJson.EntryFromSurface(surfaceEntry);
        return Compact(entry with { Represents = source.Represents }, rowLimit);
    }

    public static Relation EntrySurfaceFromStructure(Source source)
    {
        var raw = GojoStructureEncodingSurface.Create(new Carrier(), source.EntryId, source.Structure);
        if (!GojoStructureEncodingSurface.TryRead(raw, out var read))
        {
            throw new InvalidOperationException("Could not read generated Gojo structure encoding surface.");
        }

        return GojoStructureEncodingBundleSurface.Entry(
            new Carrier(),
            source.EntryId,
            source.Expression,
            source.EncodingNote,
            raw,
            [
                OpenedStructurePath(source, raw, read),
                FirstRelationRowPath(source, raw, read)
            ]);
    }

    private static GojoEncodingJsonBundleEntry Compact(
        GojoEncodingJsonBundleEntry entry,
        int rowLimit) =>
        entry with
        {
            Structure = Compact(entry.Structure, Math.Max(0, rowLimit))
        };

    private static GojoEncodingJsonStructure Compact(
        GojoEncodingJsonStructure structure,
        int rowLimit) =>
        structure with
        {
            Roots = Take(structure.Roots, rowLimit),
            Carriers = Take(structure.Carriers, rowLimit),
            TickRefs = Take(structure.TickRefs, rowLimit),
            CarrierLocations = Take(structure.CarrierLocations, rowLimit),
            Relations = Take(structure.Relations, rowLimit),
            UnitShapes = Take(structure.UnitShapes, rowLimit)
        };

    private static List<T> Take<T>(IEnumerable<T> values, int count) =>
        count == 0 ? [] : [.. values.Take(count)];

    private static Relation OpenedStructurePath(
        Source source,
        Relation rawEncoding,
        GojoStructureEncodingSurface.StructureRead read) =>
        GojoObserverPathSurface.Create(
            new Carrier(),
            rawEncoding,
            GojoObserverPathSurface.ObserverSurface(
                new Carrier(),
                rawEncoding,
                Label("opened glossary block")),
            GojoObserverPathSurface.StepFrame(
                new Carrier(),
                GojoObserverPathSurface.Step(
                    new Carrier(),
                    rawEncoding,
                    Label("opened structure"),
                    read.OpenedStructure,
                    source.Structure)),
            read.OpenedStructure,
            provenance: Label(source.Expression));

    private static Relation FirstRelationRowPath(
        Source source,
        Relation rawEncoding,
        GojoStructureEncodingSurface.StructureRead read)
    {
        var row = read.Relations.Length == 0
            ? read.OpenedStructure
            : read.Relations[0];

        return GojoObserverPathSurface.Create(
            new Carrier(),
            rawEncoding,
            GojoObserverPathSurface.ObserverSurface(
                new Carrier(),
                rawEncoding,
                Label("relation table scan")),
            GojoObserverPathSurface.StepFrame(
                new Carrier(),
                GojoObserverPathSurface.Step(
                    new Carrier(),
                    rawEncoding,
                    Label("first relation row"),
                    row,
                    read.OpenedStructure)),
            row,
            provenance: Label(source.EntryId));
    }

    private static Relation Label(string text) =>
        EncodedTextSurface.Create(new Carrier(), text);
}
