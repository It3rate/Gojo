namespace Gojo.EncodingC;

/// <summary>What a law's role can be bound to: a structure's own positions, or
/// the lanes of a port another structure exposes. A role on a structure works
/// inside it. A role on a port works across a boundary, and a law may write
/// there only by moving what it takes (see <see cref="Wiring"/>).</summary>
public abstract class Surface
{
    private protected Surface()
    {
    }

    /// <summary>Positions of a structure, or lanes of a port.</summary>
    public abstract int Count { get; }

    public abstract int PlaneCount { get; }

    internal abstract bool ThroughPort { get; }

    /// <summary>The structure, plane and position behind a surface index and plane.</summary>
    internal abstract (Structure Structure, int Plane, int Position) Resolve(int index, int plane);

    internal abstract bool IsConstant(int plane);
}

/// <summary>Positions and the planes over them. A structure has as many
/// positions as it needs and grows when it needs more. Each plane holds one bit
/// per position, or is <em>constant</em>: one bit for every position, stored
/// once, such as an orientation the whole structure shares. Planes and
/// positions are numbers. Nothing is named; what a bit means is what the laws
/// wired to it do with it.
/// <para>Retired positions stay allocated and are never reused, so a reference
/// to a position never silently changes target. A structure counts every state
/// change (<see cref="Version"/>) and every structural change
/// (<see cref="Epoch"/>), so a copy can tell whether what it read is still
/// current.</para></summary>
public sealed class Structure : Surface
{
    private readonly List<Plane?> planes = [];
    private readonly List<Port> ports = [];
    private Plane constant = new(0);
    private Plane constantValue = new(0);
    private Plane live = new(0);

    public Structure(int planes, int positions = 0)
    {
        if (planes < 0) throw new ArgumentOutOfRangeException(nameof(planes));
        for (var p = 0; p < planes; p++) AddPlane();
        if (positions > 0) Add(positions);
        Epoch = 0;
        Version = 0;
    }

    /// <summary>Positions ever added, retired ones included.</summary>
    public override int Count => live.Length;

    public override int PlaneCount => planes.Count;

    /// <summary>Positions in use.</summary>
    public Plane Live => live.Clone();

    public long Version { get; private set; }

    public long Epoch { get; private set; }

    public IReadOnlyList<Port> Ports => ports;

    internal override bool ThroughPort => false;

    /// <summary>Adds a plane with one bit per position. Returns its number.</summary>
    public int AddPlane()
    {
        planes.Add(new Plane(Count));
        GrowPlaneFlags();
        StructuralEdit();
        return planes.Count - 1;
    }

    /// <summary>Adds a constant plane: one bit for every position, stored once.</summary>
    public int AddConstantPlane(bool value)
    {
        planes.Add(null);
        GrowPlaneFlags();
        constant[planes.Count - 1] = true;
        constantValue[planes.Count - 1] = value;
        StructuralEdit();
        return planes.Count - 1;
    }

    /// <summary>Adds positions. Returns the first new one.</summary>
    public int Add(int count = 1)
    {
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
        var first = Count;
        live.Grow(first + count);
        for (var i = first; i < first + count; i++) live[i] = true;
        foreach (var plane in planes) plane?.Grow(first + count);
        StructuralEdit();
        return first;
    }

    /// <summary>Takes a position out of use. Its number is never reused; laws
    /// skip rows that touch it.</summary>
    public void Retire(int position)
    {
        CheckLive(position);
        live[position] = false;
        StructuralEdit();
    }

    public bool IsLive(int position) => (uint)position < (uint)Count && live[position];

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

    public bool Read(int plane, int position)
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

    /// <summary>Sets one bit. This is for building a structure and for supplying
    /// observations; change that arrives from outside as material enters
    /// through <see cref="Unary.Add"/> as units.</summary>
    public void Write(int plane, int position, bool value)
    {
        CheckWritable(plane);
        CheckLive(position);
        var stored = planes[plane]!;
        if (stored[position] == value) return;
        stored[position] = value;
        Version++;
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
        Version++;
    }

    /// <summary>Changes a constant plane: one bit edit that applies to every position.</summary>
    public void SetConstant(int plane, bool value)
    {
        ConstantValue(plane);
        if (constantValue[plane] == value) return;
        constantValue[plane] = value;
        StructuralEdit();
    }

    /// <summary>Exposes positions and planes to other structures. Lane i is the
    /// i-th position given. The port says nothing about what its bits mean.</summary>
    public Port Publish(int[] positions, int[] planes)
    {
        ArgumentNullException.ThrowIfNull(positions);
        ArgumentNullException.ThrowIfNull(planes);
        if (planes.Length == 0) throw new ArgumentException("A port exposes at least one plane.", nameof(planes));
        foreach (var plane in planes) CheckPlane(plane);
        foreach (var position in positions) CheckLive(position);
        var port = new Port(this, positions, planes);
        ports.Add(port);
        StructuralEdit();
        return port;
    }

    /// <summary>Points a lane of one of this structure's ports at another of its
    /// positions. Connections hold lane numbers, so they follow the move.</summary>
    public void Rebind(Port port, int lane, int position)
    {
        ArgumentNullException.ThrowIfNull(port);
        if (!ReferenceEquals(port.Owner, this)) throw new ArgumentException("Only the owner rebinds its port.", nameof(port));
        CheckLive(position);
        port.SetLane(lane, position);
        StructuralEdit();
    }

    /// <summary>Adds a lane to one of this structure's ports. Returns the lane.</summary>
    public int Extend(Port port, int position)
    {
        ArgumentNullException.ThrowIfNull(port);
        if (!ReferenceEquals(port.Owner, this)) throw new ArgumentException("Only the owner extends its port.", nameof(port));
        CheckLive(position);
        var lane = port.AddLane(position);
        StructuralEdit();
        return lane;
    }

    internal override (Structure Structure, int Plane, int Position) Resolve(int index, int plane) => (this, plane, index);

    internal override bool IsConstant(int plane) => IsConstantPlane(plane);

    internal void CheckLive(int position)
    {
        if (!IsLive(position)) throw new ArgumentOutOfRangeException(nameof(position), $"Position {position} is not live.");
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

    private void StructuralEdit()
    {
        Epoch++;
        Version++;
    }
}
