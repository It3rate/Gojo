using System.Runtime.InteropServices;
using Gojo.Core;

namespace Gojo.Experiments.PrimitiveRuntime;

/// <summary>Physical storage selectors, not domain operations.</summary>
public enum CompactStorageKind : uint { Placeholder = 1, Relation = 2 }

/// <summary>Four 32-bit words. Relations address ordered children; placeholders
/// store arena-local slot identity and face. Address zero is reserved.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct CompactRecord(CompactStorageKind Kind, uint Left, uint Right, uint Extension);

/// <summary>An immutable physical snapshot of a carrier-free native graph.
/// Native references are retained separately only for correspondence and explicit
/// source validation. Execution traverses records, never text or native objects.</summary>
public sealed class CompactArena
{
    private readonly CompactRecord[] records;
    private readonly IElement[] sources;
    private readonly Dictionary<IElement, uint> addresses;
    private readonly Dictionary<Placeholder, uint> placeholderAddresses;

    private CompactArena(CompactRecord[] records, IElement[] sources,
        Dictionary<IElement, uint> addresses, Dictionary<Placeholder, uint> placeholders)
    { this.records = records; this.sources = sources; this.addresses = addresses; placeholderAddresses = placeholders; }

    public int Count => records.Length - 1;
    public int PayloadBytes => checked(records.Length * 16);
    public int NativeReferenceBytes => checked(sources.Length * IntPtr.Size);
    public int AddressLookupCount => addresses.Count;
    public int AddressLookupCapacity => addresses.EnsureCapacity(0);
    public int PlaceholderLookupCount => placeholderAddresses.Count;
    public int PlaceholderLookupCapacity => placeholderAddresses.EnsureCapacity(0);
    public int CorrespondenceReferenceSlots => sources.Length + addresses.Count + placeholderAddresses.Count;
    public CompactRecord this[uint address] => address != 0 && address < records.Length
        ? records[address] : throw new ArgumentOutOfRangeException(nameof(address));
    public bool Contains(uint address) => address != 0 && address < records.Length;
    public bool IsPlaceholder(uint address) => Contains(address) && records[address].Kind == CompactStorageKind.Placeholder;
    public bool IsOpenTail(uint address) => IsPlaceholder(address) && records[address].Right == 0;

    /// <summary>Reconstructs an independent native inspection only when requested.
    /// Alias and placeholder-face correspondence are shared throughout this one
    /// reconstruction. Original mutable objects are never reused.</summary>
    public CompactNativeSnapshot Materialize()
    {
        var elements = new IElement[records.Length];
        var slots = new Dictionary<uint, Placeholder>();
        var active = new bool[records.Length];
        for (uint address = 1; address < records.Length; address++) Restore(address);
        return new(elements);

        IElement Restore(uint address)
        {
            if (elements[address] is { } existing) return existing;
            if (active[address]) throw new NotSupportedException("Native inspection of cyclic relation objects is outside this bounded arena reader.");
            active[address] = true;
            var record = records[address];
            if (record.Kind == CompactStorageKind.Placeholder)
            {
                if (!slots.TryGetValue(record.Left, out var slot))
                { slot = new Placeholder(); slots.Add(record.Left, slot); }
                elements[address] = record.Right == 0 ? slot : slot.Dual();
            }
            else elements[address] = new Relation(Restore(record.Left), Restore(record.Right));
            active[address] = false;
            return elements[address];
        }
    }

    public uint AddressOf(IElement participant)
    {
        if (participant is Placeholder slot && placeholderAddresses.TryGetValue(slot, out var existing)) return existing;
        return addresses.TryGetValue(participant, out var address) ? address
            : throw new ArgumentException("Participant does not belong to this captured arena.", nameof(participant));
    }

    public static CompactArena Capture(params IElement[] roots)
    {
        var sources = new List<IElement> { null! };
        var addresses = new Dictionary<IElement, uint>(ReferenceEqualityComparer.Instance);
        var placeholders = new Dictionary<Placeholder, uint>();
        var slots = new Dictionary<ulong, uint>();
        var records = new List<CompactRecord> { default };
        foreach (var root in roots) Add(root);
        for (var index = 1; index < sources.Count; index++)
        {
            var element = sources[index];
            if (element is Relation relation)
                records[index] = new(CompactStorageKind.Relation, Add(relation.Left), Add(relation.Right), 0);
            else if (element is Placeholder placeholder)
            {
                if (!slots.TryGetValue(placeholder.Id, out var slot))
                { slot = checked((uint)slots.Count + 1); slots.Add(placeholder.Id, slot); }
                records[index] = new(CompactStorageKind.Placeholder, slot, placeholder.IsInverse ? 1u : 0u, 0);
            }
        }
        return new(records.ToArray(), sources.ToArray(), addresses, placeholders);

        uint Add(IElement participant)
        {
            ArgumentNullException.ThrowIfNull(participant);
            if (participant is not Relation && participant is not Placeholder)
                throw new ArgumentException("This bounded arena captures only relations and placeholders.", nameof(roots));
            if (participant is Placeholder slot && placeholders.TryGetValue(slot, out var existing)) return existing;
            if (addresses.TryGetValue(participant, out var address)) return address;
            address = checked((uint)sources.Count); addresses.Add(participant, address);
            if (participant is Placeholder placeholder) placeholders.Add(placeholder, address);
            sources.Add(participant); records.Add(default); return address;
        }
    }

    /// <summary>Explicit synchronization boundary. Captured records never drift
    /// when their original native source is edited.</summary>
    public bool IsSourceCurrent()
    {
        for (var index = 1; index < sources.Length; index++)
        {
            if (sources[index] is not Relation relation) continue;
            var record = records[index];
            if (!TryAddress(relation.Left, out var left) || !TryAddress(relation.Right, out var right) ||
                left != record.Left || right != record.Right) return false;
        }
        return true;
    }

    private bool TryAddress(IElement participant, out uint address) => participant is Placeholder placeholder
        ? placeholderAddresses.TryGetValue(placeholder, out address) : addresses.TryGetValue(participant, out address);
}

public sealed class CompactNativeSnapshot
{
    private readonly IElement[] elements;
    internal CompactNativeSnapshot(IElement[] elements) => this.elements = elements;
    public IElement this[uint address] => address != 0 && address < elements.Length
        ? elements[address] : throw new ArgumentOutOfRangeException(nameof(address));
}
