namespace Gojo.EncodingC;

/// <summary>What a structure exposes to others: an ordered list of its
/// positions (lane i is the i-th) and an ordered list of its planes. That is
/// all. A port carries no description of what its bits stand for, no unit and
/// no type, and it does not say whether its bits may be read or taken. A
/// receiver interprets them in its own terms, and whether a connection copies
/// or moves follows from the law the receiver wires to it.
/// <para>A connection holds lane numbers, never the owner's positions. The
/// owner can rebind a lane to another position and every connection follows.
/// Nothing on a port leads back to the owner's other positions.</para></summary>
public sealed class Port : Surface
{
    private readonly PackedInts lanes = new();
    private readonly int[] planes;

    internal Port(Structure owner, int[] positions, int[] planes)
    {
        Owner = owner;
        foreach (var position in positions) lanes.Add(position);
        this.planes = planes.ToArray();
    }

    public override int Count => lanes.Count;

    public override int PlaneCount => planes.Length;

    internal Structure Owner { get; }

    internal override bool ThroughPort => true;

    internal override (Structure Structure, int Plane, int Position) Resolve(int index, int plane)
    {
        if ((uint)plane >= (uint)planes.Length) throw new ArgumentOutOfRangeException(nameof(plane));
        return (Owner, planes[plane], lanes[index]);
    }

    internal override bool IsConstant(int plane) => Owner.IsConstantPlane(planes[plane]);

    internal void SetLane(int lane, int position) => lanes[lane] = position;

    internal int AddLane(int position)
    {
        lanes.Add(position);
        return lanes.Count - 1;
    }
}
