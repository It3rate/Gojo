using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Encoding.Structures;

// Storage adapter only. Every reference restores a physical graph element;
// node kind selects a storage table, never a domain operation or concept.
internal static class GojoCompactArchive
{
    internal static GojoCompactStructureDocument Capture(string sourceId, IElement root,
        IReadOnlyDictionary<Carrier, string>? labels)
    {
        var graph = GojoEncodedSnapshot.CaptureArchive(root);
        var names = graph.Carriers.Select(c => $"c{c.Index}").ToArray();
        // Labels are display addresses. They never determine identity or reads.
        if (labels is not null)
        {
            var byId = labels.ToDictionary(pair => pair.Key.Id, pair => pair.Value);
            foreach (var row in graph.Carriers)
            {
                var requested = byId.TryGetValue(row.Id, out var label) ? $"c{row.Index}:{label}" : names[row.Index];
                names[row.Index] = requested;
            }
        }
        string Ref(GojoEncodedElementRef r) => r.Kind switch
        {
            GojoEncodedElementKind.Carrier => names[r.Index],
            GojoEncodedElementKind.TickRef => $"t{r.Index}",
            GojoEncodedElementKind.CarrierLocation => $"l{r.Index}",
            GojoEncodedElementKind.Relation => $"r{r.Index}",
            GojoEncodedElementKind.Placeholder => $"p{r.Index}",
            _ => throw new InvalidOperationException("Archive has no physical element for this reference.")
        };
        var nodes = new List<GojoCompactStructureNode>();
        nodes.AddRange(graph.Placeholders.Select(p => new GojoCompactStructureNode
        { Id = $"p{p.Index}", Kind = "placeholder", Ticks = p.SlotIndex, IsInverse = p.IsInverse }));
        nodes.AddRange(graph.TickRefs.Select(t => new GojoCompactStructureNode { Id = $"t{t.Index}", Kind = "tick", Ticks = t.Ticks }));
        nodes.AddRange(graph.CarrierLocations.Select(l => new GojoCompactStructureNode
        { Id = $"l{l.Index}", Kind = "location", Carrier = names[l.CarrierIndex], TickRef = $"t{l.TickRefIndex}" }));
        // Table order retains relative creation order, which settlement uses
        // for ties. Reference names still address their original graph rows.
        nodes.AddRange(graph.Relations.OrderBy(r => r.Id).Select(r => new GojoCompactStructureNode
        { Id = $"r{r.Index}", Kind = "relation", Left = Ref(r.Left), Right = Ref(r.Right) }));
        nodes.AddRange(graph.UnitShapes.Select(u => new GojoCompactStructureNode
        { Id = $"u{u.Index}", Kind = "unitShape", Surface = Ref(u.Surface), Descriptors = u.Descriptors.Select(Ref).ToList() }));
        return new GojoCompactStructureDocument
        {
            Version = 3, SourceId = sourceId, Root = Ref(graph.Root), Nodes = nodes,
            Carriers = graph.Carriers.OrderBy(c => c.Id).Select(c => new GojoCompactCarrierNode
            {
                Id = names[c.Index], IsInverse = c.IsInverse, IsUnknown = c.IsUnknown,
                Value = checked((long)c.Value), Tension = c.Tension,
                UnitShape = $"u{c.UnitShapeIndex}",
                Inverse = c.InverseCarrierIndex >= 0 ? names[c.InverseCarrierIndex] : null,
                ValueTick = c.ValueTickRefIndex >= 0 ? $"t{c.ValueTickRefIndex}" : null,
                TensionTick = c.TensionTickRefIndex >= 0 ? $"t{c.TensionTickRefIndex}" : null,
                Attachments = (c.AttachedRelationIndexes ?? []).Select(i => $"r{i}").ToList()
            }).ToList(),
            Counts = new GojoCompactStructureCounts
            { Carriers = graph.Carriers.Length, Nodes = nodes.Count, Relations = graph.Relations.Length, Locations = graph.CarrierLocations.Length }
        };
    }

    internal static RehydratedCompactStructure Restore(GojoCompactStructureDocument document)
    {
        TryRestore(document, out var restored);
        return restored;
    }

    internal static bool TryRestore(GojoCompactStructureDocument document, out RehydratedCompactStructure restored)
    {
        var missing = ReadArchiveGaps(document);
        if (missing.Count == 0)
        {
            try
            {
                restored = RestoreComplete(document);
                return true;
            }
            catch (Exception error) when (error is InvalidOperationException or ArgumentException or OverflowException or IndexOutOfRangeException)
            {
                // Storage contradictions are a failed import. Never publish a
                // partly restored world that can settle with invented state.
                missing.Add(error.Message);
            }
        }
        var source = EncodedTextSurface.Create(new Carrier(), GojoStructureCompactJson.Serialize(document));
        var gaps = Relation.OrderedGroup(new Carrier(), missing.Select(message =>
            (IElement)new Relation(source, EncodedTextSurface.Create(new Carrier(), message))).ToArray());
        var pending = ReadWitness.Create(new Carrier(), source, gaps, new Placeholder(), gaps, source);
        restored = new RehydratedCompactStructure(document.SourceId, pending) { ResidueOrTension = gaps };
        return false;
    }

    // These are transport requirements, not Gojo concept classifiers. An
    // absent required field is different from an explicitly recorded zero or
    // empty attachment list. Keep the supplied document intact on failure.
    private static List<string> ReadArchiveGaps(GojoCompactStructureDocument document)
    {
        var gaps = new List<string>();
        if (document.Carriers is null || document.Nodes is null)
        { gaps.Add("Archive tables are missing."); return gaps; }
        if (document.Carriers.Any(c => c is null) || document.Nodes.Any(n => n is null))
        { gaps.Add("Archive contains an absent table row."); return gaps; }
        var kinds = new Dictionary<string, string>(StringComparer.Ordinal);
        void Add(string id, string kind)
        {
            if (string.IsNullOrEmpty(id) || !kinds.TryAdd(id, kind)) gaps.Add($"Missing or duplicate identity '{id}'.");
        }
        foreach (var carrier in document.Carriers) Add(carrier.Id, "carrier");
        foreach (var node in document.Nodes) Add(node.Id, node.Kind);
        void Reference(string? id, string? kind, string field)
        {
            if (id is null || !kinds.TryGetValue(id, out var actual) || (kind is not null && actual != kind))
                gaps.Add($"Missing or incompatible {field} reference '{id}'.");
        }
        Reference(document.Root, null, "root");
        var accountingOwners = new Dictionary<string, string>(StringComparer.Ordinal);
        void AccountingReference(string? id, string field, bool isUnknown)
        {
            Reference(id, "tick", field);
            if (id is null) return;
            if (!accountingOwners.TryAdd(id, field))
                gaps.Add($"Accounting coordinate {field} aliases {accountingOwners[id]}.");
            if (isUnknown && document.Nodes.FirstOrDefault(node => node.Id == id)?.Ticks != 0)
                gaps.Add($"Wildcard accounting coordinate {field} is not zero.");
        }
        if (document.Carriers.Count(carrier => carrier.IsUnknown) > 1)
            gaps.Add("Archive contains more than one wildcard carrier identity.");
        foreach (var carrier in document.Carriers)
        {
            if (!carrier.Value.HasValue) gaps.Add($"Missing {carrier.Id}.value.");
            if (!carrier.Tension.HasValue) gaps.Add($"Missing {carrier.Id}.tension.");
            if (carrier.IsUnknown && (carrier.Value != 0 || carrier.Tension != 0))
                gaps.Add($"Wildcard {carrier.Id} cannot carry recorded accounting state.");
            Reference(carrier.UnitShape, "unitShape", $"{carrier.Id}.unitShape");
            // Complete captures include the wildcard's canonical zero slots.
            // Earlier archives omitted them, so absence there remains valid;
            // an explicit reference must preserve its distinct zero identity.
            if (!carrier.IsUnknown || carrier.ValueTick is not null)
                AccountingReference(carrier.ValueTick, $"{carrier.Id}.valueTick", carrier.IsUnknown);
            // Version 2 never asserted live pressure identity. Version 3 must
            // preserve it explicitly rather than guess after a missing field.
            if (document.Version >= 3 && (!carrier.IsUnknown || carrier.TensionTick is not null))
                AccountingReference(carrier.TensionTick, $"{carrier.Id}.tensionTick", carrier.IsUnknown);
            else if (document.Version == 2 && carrier.TensionTick is not null)
                gaps.Add($"Version 2 cannot assert {carrier.Id}.tensionTick; live pressure requires version 3.");
            if (carrier.Inverse is not null) Reference(carrier.Inverse, "carrier", $"{carrier.Id}.inverse");
            if (carrier.Attachments is null) gaps.Add($"Missing {carrier.Id}.attachments.");
            else foreach (var id in carrier.Attachments) Reference(id, "relation", $"{carrier.Id}.attachments");
        }
        foreach (var node in document.Nodes)
        {
            switch (node.Kind)
            {
                case "tick":
                case "placeholder":
                    if (!node.Ticks.HasValue) gaps.Add($"Missing {node.Id}.ticks.");
                    if (node.Kind == "placeholder" && node.Ticks is < 0 or > int.MaxValue)
                        gaps.Add($"Invalid {node.Id}.ticks slot identity.");
                    break;
                case "location":
                    Reference(node.Carrier, "carrier", $"{node.Id}.carrier");
                    Reference(node.TickRef, "tick", $"{node.Id}.tickRef");
                    break;
                case "relation":
                    Reference(node.Left, null, $"{node.Id}.left");
                    Reference(node.Right, null, $"{node.Id}.right");
                    break;
                case "unitShape":
                    Reference(node.Surface, null, $"{node.Id}.surface");
                    if (node.Descriptors is null) gaps.Add($"Missing {node.Id}.descriptors.");
                    else foreach (var id in node.Descriptors) Reference(id, null, $"{node.Id}.descriptors");
                    break;
                default: gaps.Add($"Unsupported storage node '{node.Kind}'."); break;
            }
        }
        return gaps;
    }

    private static RehydratedCompactStructure RestoreComplete(GojoCompactStructureDocument document)
    {
        var ticks = document.Nodes.Where(n => n.Kind == "tick").ToArray();
        var locations = document.Nodes.Where(n => n.Kind == "location").ToArray();
        var relations = document.Nodes.Where(n => n.Kind == "relation").ToArray();
        var units = document.Nodes.Where(n => n.Kind == "unitShape").ToArray();
        var placeholders = document.Nodes.Where(n => n.Kind == "placeholder").ToArray();
        if (ticks.Length + locations.Length + relations.Length + units.Length + placeholders.Length != document.Nodes.Count)
            throw new InvalidOperationException("Archive contains an unsupported storage node.");
        var refs = new Dictionary<string, GojoEncodedElementRef>(StringComparer.Ordinal);
        void Add(IEnumerable<string> ids, GojoEncodedElementKind kind)
        { var i = 0; foreach (var id in ids) refs.Add(id, new(kind, i++)); }
        Add(document.Carriers.Select(c => c.Id), GojoEncodedElementKind.Carrier);
        Add(ticks.Select(n => n.Id), GojoEncodedElementKind.TickRef);
        Add(locations.Select(n => n.Id), GojoEncodedElementKind.CarrierLocation);
        Add(relations.Select(n => n.Id), GojoEncodedElementKind.Relation);
        Add(units.Select(n => n.Id), GojoEncodedElementKind.UnitShape);
        Add(placeholders.Select(n => n.Id), GojoEncodedElementKind.Placeholder);
        GojoEncodedElementRef Ref(string? id) => id is not null && refs.TryGetValue(id, out var r)
            ? r : throw new InvalidOperationException($"Archive reference '{id}' is missing.");
        int Index(string? id, GojoEncodedElementKind kind)
        {
            var r = Ref(id);
            return r.Kind == kind ? r.Index : throw new InvalidOperationException("Archive reference uses the wrong storage table.");
        }
        var graph = new GojoEncodedSnapshot([Ref(document.Root)],
            document.Carriers.Select((c, i) => new GojoEncodedCarrier(i, (ulong)i, c.IsInverse, c.IsUnknown,
                checked((ulong)c.Value!.Value), c.Tension!.Value, Index(c.UnitShape, GojoEncodedElementKind.UnitShape),
                c.Inverse is null ? -1 : Index(c.Inverse, GojoEncodedElementKind.Carrier))
            {
                ValueTickRefIndex = c.ValueTick is null ? -1 : Index(c.ValueTick, GojoEncodedElementKind.TickRef),
                TensionTickRefIndex = c.TensionTick is null ? -1 : Index(c.TensionTick, GojoEncodedElementKind.TickRef),
                AttachedRelationIndexes = c.Attachments!.Select(id => Index(id, GojoEncodedElementKind.Relation)).ToArray()
            }).ToArray(),
            ticks.Select((t, i) => new GojoEncodedTickRef(i, 0, t.Ticks!.Value)).ToArray(),
            locations.Select((l, i) => new GojoEncodedCarrierLocation(i, Index(l.Carrier, GojoEncodedElementKind.Carrier), Index(l.TickRef, GojoEncodedElementKind.TickRef))).ToArray(),
            relations.Select((r, i) => new GojoEncodedRelation(i, (ulong)i, Ref(r.Left), Ref(r.Right))).ToArray(),
            units.Select((u, i) => new GojoEncodedUnitShape(i, 0, Ref(u.Surface)) { Descriptors = u.Descriptors!.Select(Ref).ToArray() }).ToArray())
        { Placeholders = placeholders.Select((p, i) => new GojoEncodedPlaceholder(i, checked((int)p.Ticks!.Value), p.IsInverse)).ToArray() };
        return new RehydratedCompactStructure(document.SourceId, graph.RestoreRoots()[0]);
    }
}
