namespace Gojo.EncodingC;

/// <summary>Another surface with some of its planes shown inverted. Reading
/// a flipped plane reads the inverted bit; writing one writes the inverted
/// bit back. Nothing is copied, so the view is live, and a law runs on it
/// unchanged. It reaches the same bits as its source: a view and its source
/// are one position, not two, and belong to the same local structure.
/// <para>Reorienting an ODV store is this view over its D and O planes
/// (<see cref="Odv.Reoriented"/>): the reader turns around and nothing in the
/// store moves. Values held as one plane per possibility are not codes and
/// are not reordered by flipping planes; a view is for planes read as a
/// code.</para></summary>
public sealed class Flipped : Surface
{
    private readonly Surface source;
    private readonly Plane flipped;

    public Flipped(Surface source, params int[] planes)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(planes);
        this.source = source;
        flipped = new Plane(source.PlaneCount);
        foreach (var plane in planes)
        {
            if ((uint)plane >= (uint)source.PlaneCount) throw new ArgumentOutOfRangeException(nameof(planes));
            flipped[plane] = true;
        }
    }

    /// <summary>The surface this view shows.</summary>
    public Surface Source => source;

    /// <summary>The planes shown inverted, in order.</summary>
    public IReadOnlyList<int> Planes => [.. flipped.Indexes()];

    public override int Count => source.Count;

    public override int PlaneCount => source.PlaneCount;

    public override bool IsLive(int index) => source.IsLive(index);

    public override bool Read(int plane, int index) => source.Read(plane, index) ^ flipped[plane];

    internal override bool ThroughPort => source.ThroughPort;

    internal override bool ReadOnly => source.ReadOnly;

    internal override Store? Home => source.Home;

    internal override bool IsConstant(int plane) => source.IsConstant(plane);

    internal override bool Filled(int plane) => source.Filled(plane) ^ flipped[plane];

    internal override void Set(int plane, int index, bool value) => source.Set(plane, index, value ^ flipped[plane]);

    internal override (Surface Home, int Index, int Plane) Bit(int index, int plane) => source.Bit(index, plane);

    internal override void ReadKeys(int index, List<(Surface, int)> keys) => source.ReadKeys(index, keys);

    internal override (Surface, int) WriteKey(int index) => source.WriteKey(index);

    internal override void ChangedTogether(IReadOnlyList<int> indexes) => source.ChangedTogether(indexes);

    internal override bool TakenByOther(Wiring taker) => source.TakenByOther(taker);

    internal override void Take(Wiring taker) => source.Take(taker);
}
