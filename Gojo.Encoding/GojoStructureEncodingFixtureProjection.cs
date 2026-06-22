using Gojo.Core;

namespace Gojo.Encoding;

/// <summary>
/// Builds small, human-facing JSON fixture entries from one live Gojo structure.
/// The fixture projection is intentionally lossy: it keeps only local reads
/// that help us understand what the structure claims to know. The full
/// diagnostic relation-shaped snapshot surface is still available from
/// DiagnosticEntrySurfaceFromStructure.
/// Each call reads exactly one source structure, not a glossary page or bundle
/// of sibling examples.
/// </summary>
public static class GojoStructureEncodingFixtureProjection
{
    public readonly record struct Source(
        string EntryId,
        string Represents,
        string Expression,
        string? EncodingNote,
        IElement Structure);

    public static GojoEncodingJsonFixtureEntry EntryFromStructure(Source source) =>
        new()
        {
            Represents = source.Represents,
            EntryId = source.EntryId,
            Expression = source.Expression,
            EncodingNote = string.IsNullOrEmpty(source.EncodingNote) ? null : source.EncodingNote,
            Gojo = SparseGojoProjection(source)
        };

    public static Relation DiagnosticEntrySurfaceFromStructure(Source source)
    {
        var raw = GojoStructureEncodingSurface.CreateDiagnosticSnapshot(new Carrier(), source.EntryId, source.Structure);
        if (!GojoStructureEncodingSurface.TryRead(raw, out var read))
        {
            throw new InvalidOperationException("Could not read generated Gojo diagnostic snapshot surface.");
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

    private static GojoEncodingJsonFixtureGojo SparseGojoProjection(Source source)
    {
        var read = SparseRead.From(source);
        return new GojoEncodingJsonFixtureGojo
        {
            Known = NullIfEmpty(read.Known),
            Paths = NullIfEmpty(read.Paths),
            Tension = NullIfEmpty(read.Tension)
        };
    }

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

    private static List<string>? NullIfEmpty(IReadOnlyCollection<string> values) =>
        values.Count == 0 ? null : [.. values];

    private sealed class SparseRead
    {
        private readonly HashSet<IElement> seen = new(ReferenceEqualityComparer.Instance);
        private readonly SortedSet<string> known = new(StringComparer.Ordinal);
        private readonly SortedSet<string> paths = new(StringComparer.Ordinal);
        private readonly SortedSet<string> tension = new(StringComparer.Ordinal);

        public IReadOnlyCollection<string> Known => known;

        public IReadOnlyCollection<string> Paths => paths;

        public IReadOnlyCollection<string> Tension => tension;

        public static SparseRead From(Source source)
        {
            var read = new SparseRead();
            read.Visit(source.Structure);
            read.ReadExpressionHints(source.Expression, source.EncodingNote);
            if (read.known.Count == 0)
            {
                read.known.Add("structure");
            }

            return read;
        }

        private void Visit(IElement element)
        {
            if (!seen.Add(element))
            {
                return;
            }

            if (TryReadGroup(element, out var participants))
            {
                foreach (var participant in participants)
                {
                    Visit(participant);
                }

                return;
            }

            if (UnitShape.TryReadOrthogonalSource(element, out var orthogonalSource))
            {
                AddOrthogonalPath(orthogonalSource);
                return;
            }

            if (TryReadFramePoint(element, out _))
            {
                known.Add("position");
                return;
            }

            if (TryReadPoint(element, out _))
            {
                known.Add("position");
                return;
            }

            if (TryReadSegment(element, out var start, out var end))
            {
                known.Add(start.Carrier.SameIdentityAs(end.Carrier) && start.Ticks == end.Ticks
                    ? "coincident-span"
                    : "span");
                return;
            }

            if (element is Relation relation)
            {
                Visit(relation.Left);
                Visit(relation.Right);
            }
        }

        private void AddOrthogonalPath(IElement source)
        {
            if (TryReadFramePoint(source, out _) || TryReadPoint(source, out _))
            {
                known.Add("position");
                paths.Add("position --open normal--> radial-flow");
                tension.Add("direction unspecified");
                return;
            }

            if (TryReadSegment(source, out var start, out var end))
            {
                if (start.Carrier.SameIdentityAs(end.Carrier) && start.Ticks == end.Ticks)
                {
                    known.Add("coincident-span");
                    paths.Add("coincident-span --open normal--> linear-potential");
                    tension.Add("length unresolved");
                    return;
                }

                known.Add("span");
                paths.Add("span --open normal--> boundary-support");
                return;
            }

            paths.Add("source --open normal--> orthogonal-potential");
            tension.Add("normal basis unresolved");
        }

        private void ReadExpressionHints(string expression, string? note)
        {
            if (expression.Contains("P(", StringComparison.Ordinal))
            {
                known.Add("position");
            }

            if (expression.Contains("S(", StringComparison.Ordinal))
            {
                known.Add("span");
            }

            if (expression.Contains("Circle", StringComparison.Ordinal))
            {
                known.Add("circle-boundary");
                paths.Add("position + radius-position --> circle-boundary");
            }

            if (expression.Contains("source", StringComparison.OrdinalIgnoreCase))
            {
                known.Add("source");
            }

            if (expression.Contains("flow_radial", StringComparison.Ordinal))
            {
                paths.Add("position --open normal--> radial-flow");
                tension.Add("direction unspecified");
            }

            if (expression.Contains("coincident", StringComparison.Ordinal))
            {
                known.Add("coincident-span");
                paths.Add("coincident-span --open normal--> linear-potential");
                tension.Add("length unresolved");
            }

            if (expression.Contains("Tension", StringComparison.Ordinal))
            {
                tension.Add("unresolved change remains inspectable");
            }

            if (expression.Contains("residue", StringComparison.Ordinal))
            {
                paths.Add("source --requested read--> result + residue");
                tension.Add("read residue preserved");
            }

            if (expression.Contains("coarse and fine resolution", StringComparison.Ordinal))
            {
                paths.Add("span --resolution read--> coarse + fine views");
                tension.Add("detail below selected resolution remains open");
            }

            if (expression.Contains("close(", StringComparison.Ordinal))
            {
                paths.Add("open path --closure read--> closure tension");
                tension.Add("closure unresolved");
            }

            if (expression.Contains("settled read", StringComparison.Ordinal))
            {
                paths.Add("source + basis + resolution --> settled read");
            }

            if (note?.Contains("preferred direction", StringComparison.OrdinalIgnoreCase) == true)
            {
                tension.Add("no preferred direction");
            }
        }

        private static bool TryReadGroup(IElement element, out IElement[] participants)
        {
            if (element is Relation relation &&
                relation.Right is Carrier holder &&
                relation.TryReadOrderedElementsOn(holder, out participants))
            {
                return true;
            }

            participants = [];
            return false;
        }

        private static bool TryReadFramePoint(IElement element, out CarrierLocation[] coordinates)
        {
            if (TryReadGroup(element, out var participants) &&
                participants.Length > 0 &&
                participants.All(participant => participant is CarrierLocation))
            {
                coordinates = [.. participants.Cast<CarrierLocation>()];
                return true;
            }

            coordinates = [];
            return false;
        }

        private static bool TryReadPoint(IElement element, out CarrierLocation location)
        {
            if (element is Relation point &&
                point.Left is CarrierLocation pointLocation &&
                point.Right is Carrier carrier &&
                pointLocation.Carrier.SameIdentityAs(carrier))
            {
                location = pointLocation;
                return true;
            }

            location = default;
            return false;
        }

        private static bool TryReadSegment(
            IElement element,
            out CarrierLocation start,
            out CarrierLocation end)
        {
            if (element is Relation segment &&
                TryReadPoint(segment.Left, out start) &&
                TryReadPoint(segment.Right, out end) &&
                start.Carrier.SameIdentityAs(end.Carrier))
            {
                return true;
            }

            start = default;
            end = default;
            return false;
        }
    }
}
