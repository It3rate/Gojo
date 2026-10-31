namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>One structure owns its packed arrays, local bindings, and port map.
/// Ports expose the same coarse ODV observation as their binding moves. These
/// Storage/migration operations change representation, not measurement or flow.
/// Explicit local passage steps additionally execute whole-handoff laws.</summary>
public sealed partial class PortStructure
{
    private readonly object gate = new();
    private readonly PositionalBuckets storage = new();
    private readonly List<SlotAddress> bindings = [];
    private readonly List<int> ports = [];

    /// <summary>Returns a local binding position. It is not stored on the value
    /// and has no meaning outside this owner. Unpublished values remain live.</summary>
    public int Create(int orientationGroup, int directionGroup, int width, ulong raw)
    {
        lock (gate)
        {
            bindings.EnsureCapacity(checked(bindings.Count + 1));
            var address = storage.Append(orientationGroup, directionGroup, width, raw);
            bindings.Add(address);
            return bindings.Count - 1;
        }
    }

    /// <summary>Several ports can refer to the same binding. The public promise
    /// is always an ODV read, including bits supplied by position.</summary>
    public int Publish(int binding)
    {
        lock (gate)
        {
            _ = bindings[binding];
            ports.Add(binding);
            return ports.Count - 1;
        }
    }

    public PortConnection Connect(int port)
    {
        lock (gate)
        {
            _ = ports[port];
            return new PortConnection(this, port);
        }
    }

    public byte ReadPort(int port)
    {
        lock (gate)
        {
            var address = bindings[ports[port]];
            return PositionalBuckets.CoarseRead(address, ReadLive(address));
        }
    }

    /// <summary>Stores a supplied encoding, like ContextTable.TryWrite. This
    /// does not model incoming tension, transfer, or settlement. Inherited O/D
    /// mismatches and codes too wide for the bucket are rejected before writing.</summary>
    public void StoreObservation(int binding, bool positiveLeft, bool negative, ulong valueCode)
    {
        lock (gate)
        {
            var address = bindings[binding];
            var raw = PositionalBuckets.Encode(address.OrientationGroup, address.DirectionGroup,
                address.Width, positiveLeft, negative, valueCode);
            if (!storage.TryWrite(address, raw)) throw new InvalidOperationException("Binding is not live.");
        }
    }

    /// <summary>Releases restrictions and/or adds storage capacity. It neither
    /// reorients a value nor discovers finer distinctions. Prepare, rebind,
    /// retire occur under the owner lock; no reader can observe two live owners.</summary>
    public bool TryMove(int binding, int orientationGroup, int directionGroup, int width)
    {
        var targetValueBits = PositionalBuckets.ValueBits(orientationGroup, directionGroup, width);
        lock (gate)
        {
            var source = bindings[binding];
            if (orientationGroup != 2 && source.OrientationGroup != orientationGroup) return false;
            var sourceOwnsDirection = source.OrientationGroup == 2 || source.DirectionGroup == 2;
            var targetOwnsDirection = orientationGroup == 2 || directionGroup == 2;
            if (!targetOwnsDirection && (sourceOwnsDirection || source.DirectionGroup != directionGroup)) return false;
            if (targetValueBits < PositionalBuckets.ValueBits(source.OrientationGroup, source.DirectionGroup, source.Width))
                return false;
            if ((source.OrientationGroup, source.DirectionGroup, source.Width) ==
                (orientationGroup, directionGroup, width)) return true;

            var (positiveLeft, negative, value) = PositionalBuckets.Decode(source, ReadLive(source));
            var raw = PositionalBuckets.Encode(orientationGroup, directionGroup, width, positiveLeft, negative, value);
            var target = storage.Append(orientationGroup, directionGroup, width, raw);
            bindings[binding] = target;
            storage.Retire(source);
            return true;
        }
    }

    // Owner-side diagnostics. External connections carry only a port position;
    // they never cache this mutable packed address. Returned bytes are copies.
    public SlotAddress Locate(int binding)
    {
        lock (gate) return bindings[binding];
    }

    public bool TryReadLocation(SlotAddress address, out ulong raw)
    {
        lock (gate) return storage.TryRead(address, out raw);
    }

    public byte[] PackedBytes(int orientationGroup, int directionGroup, int width)
    {
        lock (gate) return storage.PackedBytes(orientationGroup, directionGroup, width);
    }

    public int Count(int orientationGroup, int directionGroup, int width)
    {
        lock (gate) return storage.Count(orientationGroup, directionGroup, width);
    }

    public int LiveCount(int orientationGroup, int directionGroup, int width)
    {
        lock (gate) return storage.LiveCount(orientationGroup, directionGroup, width);
    }

    private ulong ReadLive(SlotAddress address) => storage.TryRead(address, out var raw)
        ? raw : throw new InvalidOperationException("Binding is not live.");

    internal bool HasBinding(int port, int binding)
    {
        lock (gate) return ports[port] == binding;
    }

    /// <summary>Explicitly retain a resolved coarse observation. Holding an old
    /// sample is allowed; this is neither material transfer nor a live output.</summary>
    internal bool TryCaptureObservation(int binding, byte support)
    {
        lock (gate)
        {
            var address = bindings[binding];
            if (!ReadSupport.TrySingle(support, out var word)) return false;
            // A held coarse sample uses one value bit. Do not overwrite a
            // separately interpreted wider field through this coarse interface.
            if (PositionalBuckets.ValueBits(address.OrientationGroup, address.DirectionGroup, address.Width) != 1)
                return false;
            var positiveLeft = (word & ThreeBitMeasure.Orientation) != 0;
            var negative = (word & ThreeBitMeasure.Direction) != 0;
            if (address.OrientationGroup != 2 && positiveLeft != (address.OrientationGroup == 1)) return false;
            if (address.OrientationGroup != 2 && address.DirectionGroup != 2 && negative != (address.DirectionGroup == 1))
                return false;
            var raw = PositionalBuckets.Encode(address.OrientationGroup, address.DirectionGroup,
                address.Width, positiveLeft, negative, (ulong)(word & ThreeBitMeasure.Value));
            return storage.TryWrite(address, raw);
        }
    }
}

/// <summary>A non-draining read attachment to an exposed port. This object does
/// not join values or own a copy of their flow. Its owner reference supplies scope.</summary>
public sealed class PortConnection : IPrimitiveReadPort
{
    private readonly PortStructure owner;
    public int PortIndex { get; }

    internal PortConnection(PortStructure owner, int port)
    {
        this.owner = owner;
        PortIndex = port;
    }

    public byte Read() => owner.ReadPort(PortIndex);
    public byte ReadSupport() => Gojo.Experiments.PrimitiveMeasure.ReadSupport.Of(Read());

    internal bool RefersTo(PortStructure expectedOwner, int binding) =>
        ReferenceEquals(owner, expectedOwner) && owner.HasBinding(PortIndex, binding);

    internal bool SamePortAs(PortConnection other) =>
        ReferenceEquals(owner, other.owner) && PortIndex == other.PortIndex;
}
