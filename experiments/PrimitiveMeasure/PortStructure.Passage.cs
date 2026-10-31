namespace Gojo.Experiments.PrimitiveMeasure;

public sealed partial class PortStructure
{
    internal void RequirePassagePorts(ReadOnlySpan<int> positions)
    {
        lock (gate)
            foreach (var position in positions) _ = ports[position];
    }

    /// <summary>Observe a selected subset of local V sites. An omitted bit is
    /// not read and is left as both alternatives in the joint result. This mask
    /// describes the reader's perspective, not another field on a stored value.</summary>
    internal uint ObservePassageSites(ReadOnlySpan<int> positions, int visibleSites)
    {
        if (positions.Length is < 3 or > 5) throw new ArgumentOutOfRangeException(nameof(positions));
        if (visibleSites < 0 || visibleSites >= (1 << positions.Length))
            throw new ArgumentOutOfRangeException(nameof(visibleSites));
        lock (gate)
        {
            if (!TryPassageAddresses(positions, out var addresses)) return 0;
            var known = 0;
            for (var site = 0; site < positions.Length; site++)
                if ((visibleSites & (1 << site)) != 0)
                    known |= (int)ReadLive(addresses[site]) << site;
            uint support = 0;
            for (var state = 0; state < (1 << positions.Length); state++)
                if (((state ^ known) & visibleSites) == 0) support |= 1u << state;
            return support;
        }
    }

    /// <summary>Execute one resolved whole-handoff transition under this owner's
    /// lock. Both material sites change together; control is only observed.
    /// Unknown/absent input is refused before consulting concealed value bits.
    /// Success includes a resolved unchanged step; it does not imply movement.</summary>
    internal bool TryStepPassage(ReadOnlySpan<int> positions, PassageLaw law,
        byte observedBefore, out byte afterState)
    {
        afterState = 0;
        if (positions.Length != 3) throw new ArgumentException("Expected [source, control, target].", nameof(positions));
        if (!ReadSupport.TrySingle(observedBefore, out var before)) return false;
        if (!ReadSupport.TrySingle(law.Predict(observedBefore), out var after)) return false;
        lock (gate)
        {
            if (!TryPassageAddresses(positions, out var addresses)) return false;
            var actual = (byte)(ReadLive(addresses[0]) | (ReadLive(addresses[1]) << 1) | (ReadLive(addresses[2]) << 2));
            if (actual != before) return false;

            // Validation above resolves both live one-bit targets before either
            // write. No allocation, reinterpretation, or user callback occurs
            // between these writes, and every owner read uses the same lock.
            storage.TryWrite(addresses[0], (ulong)(after & 1));
            storage.TryWrite(addresses[2], (ulong)((after >> 2) & 1));
            afterState = after;
            return true;
        }
    }

    /// <summary>Resolve an ordered sequence on one complete scope candidate,
    /// then commit its final differences under the same owner lock. Binding
    /// aliases and migrated contexts are checked here before any value read;
    /// the supplied laws are the only way the planned state can change.</summary>
    internal bool TrySequencePassages(ReadOnlySpan<int> positions, uint observedBefore,
        ReadOnlySpan<(PassageLaw Law, int[] Roles)> steps, out byte afterState)
    {
        afterState = 0;
        if (observedBefore == 0 || (observedBefore & (observedBefore - 1)) != 0) return false;
        var before = System.Numerics.BitOperations.TrailingZeroCount(observedBefore);
        lock (gate)
        {
            if (!TryPassageAddresses(positions, out var addresses)) return false;
            var actual = 0;
            for (var site = 0; site < addresses.Length; site++)
                actual |= (int)ReadLive(addresses[site]) << site;
            if (actual != before) return false;

            var candidate = observedBefore;
            foreach (var (law, roles) in steps)
            {
                candidate = law.PredictScope(candidate, roles, positions.Length);
                if (candidate == 0 || (candidate & (candidate - 1)) != 0) return false;
            }

            var after = System.Numerics.BitOperations.TrailingZeroCount(candidate);
            // No writes occur until the complete sequence has resolved. All
            // addresses are still live and fixed-width under this owner lock.
            for (var site = 0; site < addresses.Length; site++)
                if (((before ^ after) & (1 << site)) != 0)
                    storage.TryWrite(addresses[site], (ulong)((after >> site) & 1));
            afterState = (byte)after;
            return true;
        }
    }

    private bool TryPassageAddresses(ReadOnlySpan<int> positions, out SlotAddress[] addresses)
    {
        addresses = new SlotAddress[positions.Length];
        for (var site = 0; site < positions.Length; site++)
        {
            var binding = ports[positions[site]];
            for (var earlier = 0; earlier < site; earlier++)
                if (binding == ports[positions[earlier]]) return false;
            var address = bindings[binding];
            // This first transition grammar requires inherited O/D and exactly
            // one owned V bit. A later move can invalidate a formerly legal row.
            if (address.OrientationGroup == 2 || address.DirectionGroup == 2 || address.Width != 1) return false;
            if (site > 0 && (address.OrientationGroup != addresses[0].OrientationGroup ||
                address.DirectionGroup != addresses[0].DirectionGroup)) return false;
            addresses[site] = address;
        }
        return true;
    }
}
