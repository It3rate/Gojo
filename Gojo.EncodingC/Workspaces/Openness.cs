namespace Gojo.EncodingC;

/// <summary>Information tension at each position of a value's supports, read
/// live (docs/GojoTension-2026-11.md, §3.2). Let n be the number of values
/// still possible. Its two faces:
/// <list type="bullet">
/// <item><see cref="Deficit"/>: n − 1 alternatives left to rule out, as a unary
/// count. Bit k is set when more than k + 1 values remain.</item>
/// <item><see cref="Excess"/>: one bit, set when no value remains, a
/// contradiction.</item>
/// </list>
/// Settled is both clear. A law reads it as gates: the deficit's lowest bit is
/// "still open", the excess bit is "contradicted", and through a flipped view
/// each becomes its absence, so a move waiting on "settled" waits on two
/// flipped gates. An open value is not tension by itself; it becomes tension
/// where something reads this view as a demand.
/// <para>Never stored, never written. When the supports are a supposition, the
/// view is too.</para>
/// <para><b>A placeholder for a numeric read.</b> What this view computes is
/// worked out in host code on every read, not held in planes or a law table,
/// so the structure cannot inspect or change how it is computed. Pre-numeric
/// Gojo has no magnitude to compare, so each thermometer bit is rebuilt from
/// other counts. With numeric magnitudes each bit is one threshold read,
/// "magnitude at least threshold", with the threshold stored as data. Laws
/// bind only this view's planes, so the inside can be replaced without
/// changing them. See docs/GojoTension-2026-11.md, §11.</para></summary>
public sealed class Openness : Surface
{
    private readonly Surface surface;
    private readonly int[] planes;

    public Openness(Supports supports)
        : this(supports?.Surface!, [.. supports!.Planes])
    {
    }

    /// <summary>Openness of the support planes <paramref name="planes"/>, one
    /// per value, on a surface.</summary>
    public Openness(Surface surface, IReadOnlyList<int> planes)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(planes);
        if (planes.Count < 2) throw new ArgumentException("A value with one possibility is never open.", nameof(planes));
        if (planes.Any(p => (uint)p >= (uint)surface.PlaneCount)) throw new ArgumentOutOfRangeException(nameof(planes));
        this.surface = surface;
        this.planes = [.. planes];
        Deficit = new Field(Shape.Unary(planes.Count - 1), [.. Enumerable.Range(0, planes.Count - 1)]);
        Excess = Field.Bit(planes.Count - 1);
    }

    /// <summary>The surface whose supports this view reads.</summary>
    public Surface Source => surface;

    /// <summary>The support planes, one per value.</summary>
    public IReadOnlyList<int> Planes => Array.AsReadOnly(planes);

    /// <summary>Alternatives left to rule out: values possible, less one.</summary>
    public Field Deficit { get; }

    /// <summary>No value possible: the premises disagree.</summary>
    public Field Excess { get; }

    public override int Count => surface.Count;

    public override int PlaneCount => planes.Length;

    public override bool IsLive(int index) => surface.IsLive(index);

    public override bool Read(int plane, int index)
    {
        if ((uint)plane >= (uint)PlaneCount) throw new ArgumentOutOfRangeException(nameof(plane));
        var possible = planes.Count(p => surface.Read(p, index));
        return plane == PlaneCount - 1 ? possible == 0 : possible > plane + 1;
    }

    internal override bool ThroughPort => false;

    internal override Store? Home => surface.Home;

    internal override Surface Underlying => surface.Underlying is Workspace workspace ? workspace : this;

    internal override bool ReadOnly => true;

    internal override bool IsConstant(int plane) => false;

    internal override void Set(int plane, int index, bool value) =>
        throw new InvalidOperationException("Openness is a read of supports. Narrow or forget the supports.");

    internal override (Surface Home, int Index, int Plane) Bit(int index, int plane) => (this, index, plane);

    internal override void ReadKeys(int index, List<(Surface, int)> keys) => surface.ReadKeys(index, keys);

    internal override (Surface, int) WriteKey(int index) => (this, index);
}
