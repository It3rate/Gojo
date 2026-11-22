namespace Gojo.EncodingC;

/// <summary>What a local structure exposes to others, from one of its
/// stores: an ordered list of that store's positions (lane i is the i-th) and
/// an ordered list of its planes. That is
/// all. A port carries no description of what its bits stand for, no unit and
/// no type, and it does not say whether its bits may be read or taken. A
/// receiver interprets them in its own terms, and whether a connection copies
/// or moves follows from the law the receiver wires to it.
/// <para>A connection holds lane numbers, never the owner's positions. The
/// owner can rebind a lane to another position and every connection follows.
/// Nothing on a port leads back to the owner's other positions.</para>
/// <para><b>One taker.</b> Any number of connections can read a port, and at
/// most one can take through it: one wiring bound to it directly, or one
/// socket plugged into it. Two takers would race for one unit whenever their
/// structures step at the same moment, so the contest is never made. An owner
/// that wants to serve two takers splits what it has with its own law and
/// publishes a port for each; inside the taking structure, a socket can feed
/// as many of its wirings as it likes. Two taken ports of one owner never
/// show the same position, so one position has at most one taker however the
/// owner lays out its ports.</para></summary>
public sealed class Port : Surface
{
    private readonly PackedInts lanes = new();
    private readonly int[] planes;
    private object? taker;

    internal Port(Store owner, int[] positions, int[] planes)
    {
        Owner = owner;
        foreach (var position in positions) lanes.Add(position);
        this.planes = planes.ToArray();
    }

    public override int Count => lanes.Count;

    public override int PlaneCount => planes.Length;

    internal Store Owner { get; }

    internal override bool ThroughPort => true;

    public override bool IsLive(int lane) => (uint)lane < (uint)Count && Owner.IsLive(lanes[lane]);

    /// <summary>Reads one bit through a lane: what the owner exposes there now.</summary>
    public override bool Read(int plane, int lane) => Owner.Read(OwnerPlane(plane), lanes[lane]);

    internal override bool IsConstant(int plane) => Owner.IsConstantPlane(OwnerPlane(plane));

    internal override void Set(int plane, int index, bool value) => Owner.Write(OwnerPlane(plane), lanes[index], value);

    internal override (Surface Home, int Index, int Plane) Bit(int index, int plane) => Owner.Bit(lanes[index], OwnerPlane(plane));

    internal override void ReadKeys(int index, List<(Surface, int)> keys) => Owner.ReadKeys(lanes[index], keys);

    internal override (Surface, int) WriteKey(int index) => Owner.WriteKey(lanes[index]);

    private int OwnerPlane(int plane)
    {
        if ((uint)plane >= (uint)planes.Length) throw new ArgumentOutOfRangeException(nameof(plane));
        return planes[plane];
    }

    /// <summary>Whether a connection takes through this port.</summary>
    public bool IsTaken => taker is not null;

    internal override bool TakenByOther(Wiring taker) => !Holds(taker) && IsTaken;

    internal override void Take(Wiring taker) => Claim(taker);

    internal bool Holds(object by) => ReferenceEquals(taker, by);

    internal void Claim(object by)
    {
        if (IsTaken && !Holds(by))
            throw new ArgumentException("A port has one taker. Split what it carries with the owner's own law and publish a port for each taker.");
        if (!IsTaken && Enumerable.Range(0, Count).Any(lane => Owner.TakenElsewhere(this, lanes[lane])))
            throw new ArgumentException("Another taken port shows a position of this one, so two takers would race for it.");
        taker = by;
    }

    internal void Release(object by)
    {
        if (Holds(by)) taker = null;
    }

    internal int Lane(int lane) => lanes[lane];

    internal IReadOnlyList<int> OwnerPlanes => Array.AsReadOnly(planes);

    internal void SetLane(int lane, int position) => lanes[lane] = position;

    internal int AddLane(int position)
    {
        lanes.Add(position);
        return lanes.Count - 1;
    }
}
