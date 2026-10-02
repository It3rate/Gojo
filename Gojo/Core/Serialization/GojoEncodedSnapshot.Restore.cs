namespace Gojo.Core;

public sealed partial record GojoEncodedSnapshot
{
    /// <summary>Restores isolated element identities before wiring their shared
    /// structure. Use CaptureArchive when the result must resume live behavior.
    /// Numeric ids are session-local; reference identity and attachment order
    /// inside the restored world are preserved.</summary>
    public IElement[] RestoreRoots()
    {
        // Settlement uses creation order to break ties. Traversal order can be
        // different, so allocate identities in the original relative order.
        var carriers = new Carrier[Carriers.Length];
        foreach (var row in Carriers.OrderBy(row => row.Id))
            carriers[row.Index] = row.IsUnknown ? Carrier.Unknown : new Carrier(row.IsInverse);
        var ticks = TickRefs.Select(row => new TickRef(row.Ticks)).ToArray();
        var relations = new Relation[Relations.Length];
        foreach (var row in Relations.OrderBy(row => row.Id))
            relations[row.Index] = new Relation(Carrier.Unknown, Carrier.Unknown);
        var locations = CarrierLocations.Select(row => new CarrierLocation(carriers[row.CarrierIndex], ticks[row.TickRefIndex])).ToArray();
        var slots = Placeholders.Select(p => p.SlotIndex).Distinct().ToDictionary(index => index, _ => new Placeholder());
        var placeholders = Placeholders.Select(p => p.IsInverse ? slots[p.SlotIndex].Dual() : slots[p.SlotIndex]).ToArray();

        IElement Read(GojoEncodedElementRef reference) => reference.Kind switch
        {
            GojoEncodedElementKind.Carrier => carriers[reference.Index],
            GojoEncodedElementKind.TickRef => ticks[reference.Index],
            GojoEncodedElementKind.CarrierLocation => locations[reference.Index],
            GojoEncodedElementKind.Relation => relations[reference.Index],
            GojoEncodedElementKind.Placeholder => placeholders[reference.Index],
            _ => throw new InvalidOperationException("Archive contains an absent or unsupported element reference.")
        };

        foreach (var row in Relations)
            relations[row.Index].RestoreArchiveSides(Read(row.Left), Read(row.Right));

        var units = UnitShapes.Select(row => UnitShape.FromArchive(Read(row.Surface), row.Descriptors.Select(Read).ToArray())).ToArray();
        var stateTicks = Carriers.Where(row => !row.IsUnknown && row.ValueTickRefIndex >= 0)
            .Select(row => row.ValueTickRefIndex).ToHashSet();
        foreach (var row in Carriers)
        {
            if (row.IsUnknown) continue;
            var tick = row.ValueTickRefIndex >= 0 ? ticks[row.ValueTickRefIndex] : new TickRef(row.Value);
            if (tick.Ticks < 0 || (ulong)tick.Ticks != row.Value)
                throw new InvalidOperationException("Archive value slot contradicts its carrier accounting state.");
            // Older archives did not expose a live pressure coordinate. Their
            // existing tension locations remain historical observations.
            var tension = row.TensionTickRefIndex >= 0 ? ticks[row.TensionTickRefIndex] : new TickRef(row.Tension);
            if (row.TensionTickRefIndex >= 0 && !stateTicks.Add(row.TensionTickRefIndex))
                throw new InvalidOperationException("Archive tension slot aliases another accounting coordinate.");
            if (tension.Ticks != row.Tension)
                throw new InvalidOperationException("Archive tension slot contradicts its carrier accounting state.");
            if (row.InverseCarrierIndex >= 0)
            {
                var opposed = Carriers[row.InverseCarrierIndex];
                if (opposed.InverseCarrierIndex != row.Index || opposed.IsInverse == row.IsInverse)
                    throw new InvalidOperationException("Archive inverse pairing is not reciprocal.");
            }
            carriers[row.Index].RestoreArchiveState(
                row.UnitShapeIndex >= 0 ? units[row.UnitShapeIndex] : carriers[row.Index].UnitShape,
                tick, tension,
                row.InverseCarrierIndex >= 0 ? carriers[row.InverseCarrierIndex] : null,
                (row.AttachedRelationIndexes ?? []).Select(index => relations[index]));
        }
        return Roots.Select(Read).ToArray();
    }
}
