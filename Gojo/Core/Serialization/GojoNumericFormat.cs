namespace Gojo.Core;

/// <summary>
/// Versioned integer-table transport of complete Gojo graphs. Table selectors
/// describe storage, not operations. Loading builds an independent local world
/// with fresh runtime IDs, preserving source ID order and all graph references.
/// </summary>
public static partial class GojoNumericFormat
{
    private const uint Magic = 0x4F4A4F47; // GOJO, little endian.
    public const int Version = 1;

    public static byte[] Encode(params IElement[] roots) => Encode(GojoEncodedSnapshot.CaptureGraph(roots));

    public static byte[] Encode(GojoEncodedSnapshot graph)
    {
        Validate(graph);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(Magic); writer.Write(Version);
        foreach (var count in new[] { graph.Roots.Length, graph.Carriers.Length, graph.TickRefs.Length,
            graph.CarrierLocations.Length, graph.Relations.Length, graph.UnitShapes.Length, graph.Placeholders.Length })
            writer.Write(count);
        foreach (var root in graph.Roots) Ref(root);
        foreach (var c in graph.Carriers)
        {
            writer.Write(c.Id); writer.Write(c.IsInverse); writer.Write(c.IsUnknown);
            writer.Write(c.Value); writer.Write(c.Tension); writer.Write(c.UnitShapeIndex);
            writer.Write(c.InverseCarrierIndex); writer.Write(c.ValueTickRefIndex);
            writer.Write(c.AttachedRelationIndexes.Length);
            foreach (var index in c.AttachedRelationIndexes) writer.Write(index);
        }
        foreach (var t in graph.TickRefs) { writer.Write(t.Id); writer.Write(t.Ticks); }
        foreach (var l in graph.CarrierLocations) { writer.Write(l.CarrierIndex); writer.Write(l.TickRefIndex); }
        foreach (var r in graph.Relations) { writer.Write(r.Id); Ref(r.Left); Ref(r.Right); }
        foreach (var u in graph.UnitShapes)
        {
            writer.Write(u.Id); Ref(u.Surface); writer.Write(u.Descriptors.Length);
            foreach (var descriptor in u.Descriptors) Ref(descriptor);
        }
        foreach (var p in graph.Placeholders) { writer.Write(p.SlotIndex); writer.Write(p.IsInverse); }
        return stream.ToArray();

        void Ref(GojoEncodedElementRef reference) { writer.Write((byte)reference.Kind); writer.Write(reference.Index); }
    }

    public static IElement[] Decode(ReadOnlySpan<byte> bytes) => Restore(ReadSnapshot(bytes));

    /// <summary>Reads the transport tables without creating or settling a local world.</summary>
    public static GojoEncodedSnapshot ReadSnapshot(ReadOnlySpan<byte> bytes)
    {
        using var stream = new MemoryStream(bytes.ToArray(), writable: false);
        using var reader = new BinaryReader(stream);
        try
        {
            if (reader.ReadUInt32() != Magic || reader.ReadInt32() != Version)
                throw new InvalidDataException("Unsupported Gojo numeric header or version.");
            var counts = new int[7];
            for (var i = 0; i < counts.Length; i++) counts[i] = Count(1);
            var minimum = (long)counts[0] * 5 + (long)counts[1] * 42 + (long)counts[2] * 16 +
                (long)counts[3] * 8 + (long)counts[4] * 18 + (long)counts[5] * 17 + (long)counts[6] * 5;
            if (minimum > stream.Length - stream.Position) throw new InvalidDataException("Truncated Gojo numeric tables.");
            var roots = new GojoEncodedElementRef[counts[0]];
            for (var i = 0; i < roots.Length; i++) roots[i] = Ref();
            var carriers = new GojoEncodedCarrier[counts[1]];
            for (var i = 0; i < carriers.Length; i++)
            {
                var id = reader.ReadUInt64(); var inverse = Flag(); var unknown = Flag();
                var value = reader.ReadUInt64(); var tension = reader.ReadInt64();
                var unit = reader.ReadInt32(); var opposite = reader.ReadInt32(); var tick = reader.ReadInt32();
                var attached = new int[Count(4)];
                for (var j = 0; j < attached.Length; j++) attached[j] = reader.ReadInt32();
                carriers[i] = new(i, id, inverse, unknown, value, tension, unit, opposite)
                    { ValueTickRefIndex = tick, AttachedRelationIndexes = attached };
            }
            var ticks = new GojoEncodedTickRef[counts[2]];
            for (var i = 0; i < ticks.Length; i++) ticks[i] = new(i, reader.ReadUInt64(), reader.ReadInt64());
            var locations = new GojoEncodedCarrierLocation[counts[3]];
            for (var i = 0; i < locations.Length; i++) locations[i] = new(i, reader.ReadInt32(), reader.ReadInt32());
            var relations = new GojoEncodedRelation[counts[4]];
            for (var i = 0; i < relations.Length; i++) relations[i] = new(i, reader.ReadUInt64(), Ref(), Ref());
            var units = new GojoEncodedUnitShape[counts[5]];
            for (var i = 0; i < units.Length; i++)
            {
                var id = reader.ReadUInt64(); var surface = Ref(); var descriptors = new GojoEncodedElementRef[Count(5)];
                for (var j = 0; j < descriptors.Length; j++) descriptors[j] = Ref();
                units[i] = new(i, id, surface) { Descriptors = descriptors };
            }
            var slots = new GojoEncodedPlaceholder[counts[6]];
            for (var i = 0; i < slots.Length; i++) slots[i] = new(i, reader.ReadInt32(), Flag());
            if (stream.Position != stream.Length) throw new InvalidDataException("Trailing bytes after Gojo numeric graph.");
            var graph = new GojoEncodedSnapshot(roots, carriers, ticks, locations, relations, units)
                { Placeholders = slots, IsCompleteGraph = true };
            Validate(graph);
            return graph;
        }
        catch (EndOfStreamException error) { throw new InvalidDataException("Truncated Gojo numeric graph.", error); }

        int Count(int minimumBytes)
        {
            var count = reader.ReadInt32();
            if (count < 0 || count > (stream.Length - stream.Position) / minimumBytes)
                throw new InvalidDataException("Invalid Gojo numeric table length.");
            return count;
        }
        GojoEncodedElementRef Ref() => new((GojoEncodedElementKind)reader.ReadByte(), reader.ReadInt32());
        bool Flag() => reader.ReadByte() switch { 0 => false, 1 => true, _ => throw new InvalidDataException("Invalid numeric boolean.") };
    }

    /// <summary>Restores validated graph state before exposing roots; this never calls Attach or Set.</summary>
    public static IElement[] Restore(GojoEncodedSnapshot graph)
    {
        Validate(graph);
        var carriers = new Carrier[graph.Carriers.Length];
        var ticks = new TickRef[graph.TickRefs.Length];
        var relations = new Relation[graph.Relations.Length];
        var units = new UnitShape[graph.UnitShapes.Length];
        var slots = new Placeholder[graph.Placeholders.Length];
        var unknown = graph.Carriers.Where(c => c.IsUnknown).ToArray();
        var unknownTick = unknown.Length == 0 ? -1 : unknown[0].ValueTickRefIndex;
        var unknownUnit = unknown.Length == 0 ? -1 : unknown[0].UnitShapeIndex;
        // Relative allocation order matters to today's deterministic local
        // settlement tie-breaks. Never reuse process-global IDs from a packet.
        foreach (var c in graph.Carriers.OrderBy(c => c.Id))
            carriers[c.Index] = c.IsUnknown ? Carrier.Unknown : Carrier.CreateRestorationShell(c.IsInverse);
        foreach (var t in graph.TickRefs.OrderBy(t => t.Id))
            ticks[t.Index] = t.Index == unknownTick ? Carrier.Unknown.ValueSlot.Tick : new TickRef(t.Ticks);
        foreach (var r in graph.Relations.OrderBy(r => r.Id)) relations[r.Index] = Relation.CreateRestorationShell();
        foreach (var u in graph.UnitShapes.OrderBy(u => u.Id))
            units[u.Index] = u.Index == unknownUnit ? Carrier.Unknown.UnitShape : UnitShape.CreateRestorationShell();
        var slotBases = new Dictionary<int, Placeholder>();
        foreach (var p in graph.Placeholders)
        {
            if (!slotBases.TryGetValue(p.SlotIndex, out var basis)) slotBases.Add(p.SlotIndex, basis = new Placeholder());
            slots[p.Index] = p.IsInverse ? basis.Dual() : basis;
        }
        foreach (var r in graph.Relations) relations[r.Index].CompleteRestoration(Element(r.Left), Element(r.Right));
        foreach (var u in graph.UnitShapes)
            if (u.Index != unknownUnit) units[u.Index].CompleteRestoration(Element(u.Surface), [.. u.Descriptors.Select(Element)]);
        foreach (var c in graph.Carriers)
            if (!c.IsUnknown) carriers[c.Index].CompleteRestoration(units[c.UnitShapeIndex], ticks[c.ValueTickRefIndex], c.Tension,
                c.InverseCarrierIndex < 0 ? null : carriers[c.InverseCarrierIndex],
                [.. c.AttachedRelationIndexes.Select(index => relations[index])]);
        return [.. graph.Roots.Select(Element)];

        IElement Element(GojoEncodedElementRef reference) => reference.Kind switch
        {
            GojoEncodedElementKind.Carrier => carriers[reference.Index],
            GojoEncodedElementKind.TickRef => ticks[reference.Index],
            GojoEncodedElementKind.CarrierLocation => new CarrierLocation(
                carriers[graph.CarrierLocations[reference.Index].CarrierIndex], ticks[graph.CarrierLocations[reference.Index].TickRefIndex]),
            GojoEncodedElementKind.Relation => relations[reference.Index],
            GojoEncodedElementKind.Placeholder => slots[reference.Index],
            _ => throw new InvalidDataException("Unsupported element table in graph.")
        };
    }
}
