namespace Gojo.EncodingC;

/// <summary>Positions and the planes over them. A store has as many
/// positions as it needs and grows when it needs more. Each plane holds one bit
/// per position, or is <em>constant</em>: one bit for every position, stored
/// once, such as an orientation the whole store shares. A constant is
/// positional, as in the core's buckets: belonging to this store is what
/// gives a position the bit, so everything here turns together, and turning
/// one word means moving it into a store that faces the other way. Planes
/// and positions are numbers. Nothing is named; what a bit means is what the
/// laws wired to it do with it.
/// <para><b>A store is state, not a structure.</b> It holds bits and does
/// nothing. A local structure is the group of stores and wirings that forms
/// one local intelligence. Inside it, laws connect to any of its stores
/// freely and everything settles together. Between local structures the only
/// connection is a port, and each settles independently. Nothing here records
/// which local structure a store belongs to yet, so that boundary is kept by
/// whoever wires, and is not checked.</para>
/// <para><b>Spare, in use, retired.</b> A position is in one of three states,
/// and only moves forward through them. A spare position is room: allocated,
/// not yet in use, and rows that touch it do not run. It can be recruited
/// once. A retired position stays allocated and is never reused, so a
/// reference to a position never silently changes target. Whether a position
/// is in use is state like any other: <see cref="Life"/> shows it as a
/// surface, so a law grows a store by recruiting a spare position and
/// prunes it by retiring one.</para></summary>
public sealed class Store : Surface
{
    private readonly List<Plane?> planes = [];
    private readonly List<Port> ports = [];
    private Plane constant = new(0);
    private Plane constantValue = new(0);
    private Plane live = new(0);
    private Plane used = new(0);

    public Store(int planes, int positions = 0)
    {
        if (planes < 0) throw new ArgumentOutOfRangeException(nameof(planes));
        Life = new Life(this);
        for (var p = 0; p < planes; p++) AddPlane();
        if (positions > 0) Add(positions);
    }

    /// <summary>Positions ever added: spare, in use and retired.</summary>
    public override int Count => live.Length;

    public override int PlaneCount => planes.Count;

    /// <summary>Positions in use.</summary>
    public Plane Live => live.Clone();

    /// <summary>Positions that are room, not yet in use.</summary>
    public Plane Spare => used.Not();

    /// <summary>Whether each position is in use, as a surface with one plane.
    /// A law bound to it recruits a spare position by setting its bit and
    /// retires one in use by clearing it.</summary>
    public Life Life { get; }

    public IReadOnlyList<Port> Ports => ports.AsReadOnly();

    internal override bool ThroughPort => false;

    /// <summary>Adds a plane with one bit per position. Returns its number.</summary>
    public int AddPlane()
    {
        planes.Add(new Plane(Count));
        GrowPlaneFlags();
        return planes.Count - 1;
    }

    /// <summary>Adds a constant plane: one bit for every position, stored once.</summary>
    public int AddConstantPlane(bool value)
    {
        planes.Add(null);
        GrowPlaneFlags();
        constant[planes.Count - 1] = true;
        constantValue[planes.Count - 1] = value;
        return planes.Count - 1;
    }

    /// <summary>Adds positions in use. Returns the first new one.</summary>
    public int Add(int count = 1) => Allocate(count, inUse: true);

    /// <summary>Adds spare positions: room that nothing uses yet. Their bits
    /// can be written, so a dormant part can be prepared before it is
    /// recruited. Returns the first new one.</summary>
    public int AddSpare(int count = 1) => Allocate(count, inUse: false);

    /// <summary>Brings a spare position into use. A retired position is never
    /// recruited again.</summary>
    public void Recruit(int position)
    {
        if (!IsSpare(position)) throw new ArgumentOutOfRangeException(nameof(position), $"Position {position} is not spare.");
        used[position] = true;
        live[position] = true;
    }

    /// <summary>Takes a position out of use. Its number is never reused; laws
    /// skip rows that touch it.</summary>
    public void Retire(int position)
    {
        CheckLive(position);
        live[position] = false;
    }

    public override bool IsLive(int position) => (uint)position < (uint)Count && live[position];

    public bool IsSpare(int position) => (uint)position < (uint)Count && !used[position];

    /// <summary>In use once, and no longer.</summary>
    public bool IsRetired(int position) => (uint)position < (uint)Count && used[position] && !live[position];

    public bool IsConstantPlane(int plane)
    {
        CheckPlane(plane);
        return constant[plane];
    }

    /// <summary>The bit of a constant plane.</summary>
    public bool ConstantValue(int plane)
    {
        if (!IsConstantPlane(plane)) throw new ArgumentException($"Plane {plane} holds one bit per position.", nameof(plane));
        return constantValue[plane];
    }

    public override bool Read(int plane, int position)
    {
        CheckPlane(plane);
        if ((uint)position >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(position));
        return constant[plane] ? constantValue[plane] : planes[plane]![position];
    }

    /// <summary>The whole plane. A constant plane reads as every live position or none.</summary>
    public Plane Read(int plane)
    {
        CheckPlane(plane);
        if (constant[plane]) return constantValue[plane] ? live.Clone() : new Plane(Count);
        return planes[plane]!.Clone();
    }

    /// <summary>Sets one bit. This is for building a store and for supplying
    /// observations; change that arrives from outside as material enters
    /// through <see cref="Unary.Add"/> as units.</summary>
    public void Write(int plane, int position, bool value)
    {
        CheckWritable(plane);
        if (!IsLive(position) && !IsSpare(position)) throw new ArgumentOutOfRangeException(nameof(position), $"Position {position} is retired or does not exist.");
        var stored = planes[plane]!;
        if (stored[position] == value) return;
        stored[position] = value;
    }

    /// <summary>Replaces the plane's bits under <paramref name="mask"/>.</summary>
    public void Write(int plane, Plane bits, Plane mask)
    {
        CheckWritable(plane);
        ArgumentNullException.ThrowIfNull(bits);
        ArgumentNullException.ThrowIfNull(mask);
        if (!live.Covers(mask)) throw new ArgumentException("Only live positions can be written.", nameof(mask));
        var next = planes[plane]!.AndNot(mask).Or(bits.And(mask));
        if (next.Equals(planes[plane])) return;
        planes[plane] = next;
    }

    /// <summary>Changes a constant plane: one bit edit that applies to every
    /// position, because they all share it. Only the store itself does
    /// this; a workspace over it cannot.</summary>
    public void SetConstant(int plane, bool value)
    {
        ConstantValue(plane);
        if (constantValue[plane] == value) return;
        constantValue[plane] = value;
    }

    /// <summary>Exposes positions and planes to other local structures. Lane i is the
    /// i-th position given. The port says nothing about what its bits mean.</summary>
    public Port Publish(int[] positions, int[] planes)
    {
        ArgumentNullException.ThrowIfNull(positions);
        ArgumentNullException.ThrowIfNull(planes);
        if (planes.Length == 0) throw new ArgumentException("A port exposes at least one plane.", nameof(planes));
        foreach (var plane in planes) CheckPlane(plane);
        if (planes.Distinct().Count() != planes.Length) throw new ArgumentException("A port exposes each plane once; one bit shown twice would be counted twice.", nameof(planes));
        foreach (var position in positions) CheckLive(position);
        var port = new Port(this, positions, planes);
        ports.Add(port);
        return port;
    }

    /// <summary>Points a lane of one of this store's ports at another of its
    /// positions. Connections hold lane numbers, so they follow the move.</summary>
    public void Rebind(Port port, int lane, int position)
    {
        ArgumentNullException.ThrowIfNull(port);
        if (!ReferenceEquals(port.Owner, this)) throw new ArgumentException("Only the owner rebinds its port.", nameof(port));
        CheckLive(position);
        port.SetLane(lane, position);
    }

    /// <summary>Adds a lane to one of this store's ports. Returns the lane.</summary>
    public int Extend(Port port, int position)
    {
        ArgumentNullException.ThrowIfNull(port);
        if (!ReferenceEquals(port.Owner, this)) throw new ArgumentException("Only the owner extends its port.", nameof(port));
        CheckLive(position);
        var lane = port.AddLane(position);
        return lane;
    }

    internal override bool IsConstant(int plane) => IsConstantPlane(plane);

    internal override void Set(int plane, int index, bool value) => Write(plane, index, value);

    internal override (Surface Home, int Index, int Plane) Bit(int index, int plane) => (this, index, plane);

    internal override void ReadKeys(int index, List<(Surface, int)> keys) => keys.Add((this, index));

    internal override (Surface, int) WriteKey(int index) => (this, index);

    internal void CheckLive(int position)
    {
        if (!IsLive(position)) throw new ArgumentOutOfRangeException(nameof(position), $"Position {position} is not live.");
    }

    private int Allocate(int count, bool inUse)
    {
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
        var first = Count;
        live.Grow(first + count);
        used.Grow(first + count);
        for (var i = first; inUse && i < first + count; i++) live[i] = used[i] = true;
        foreach (var plane in planes) plane?.Grow(first + count);
        return first;
    }

    private void CheckWritable(int plane)
    {
        CheckPlane(plane);
        if (constant[plane]) throw new InvalidOperationException($"Plane {plane} is constant: one bit for every position. Change it with SetConstant.");
    }

    private void CheckPlane(int plane)
    {
        if ((uint)plane >= (uint)planes.Count) throw new ArgumentOutOfRangeException(nameof(plane));
    }

    private void GrowPlaneFlags()
    {
        constant.Grow(planes.Count);
        constantValue.Grow(planes.Count);
    }
}
