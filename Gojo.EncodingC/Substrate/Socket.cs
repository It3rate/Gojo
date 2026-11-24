namespace Gojo.EncodingC;

/// <summary>The receiving end of a connection: the shape a local structure
/// expects from another's port, and which port fills it now. Lane i of the
/// socket is lane i of the port, one to one and uncalibrated. The shape is
/// how many lanes the receiver addresses, how many planes, and which of those
/// are constant; it says nothing about what the bits stand for.
/// <para>A local structure's wirings bind a socket where they would bind a
/// foreign port, so the structure can be built, saved and run before or
/// without its peer. Nothing plugged in, or a lane the port does not have,
/// reaches nowhere: rows through it do not run and are reported skipped,
/// which is how an absent peer shows. Plugging a port in is a structural edit
/// of the receiver; it reaches nothing behind the port.</para></summary>
public sealed class Socket : Surface
{
    private readonly int lanes;
    private readonly Plane constant;
    private Port? port;
    private Wiring? taker;

    /// <summary>A socket of <paramref name="lanes"/> lanes over
    /// <paramref name="planes"/> planes, none of them constant.</summary>
    public Socket(int lanes, int planes)
        : this(lanes, new Plane(planes))
    {
    }

    private Socket(int lanes, Plane constant)
    {
        if (lanes < 0) throw new ArgumentOutOfRangeException(nameof(lanes));
        if (constant.Length < 1) throw new ArgumentOutOfRangeException(nameof(constant), "A socket takes at least one plane.");
        this.lanes = lanes;
        this.constant = constant;
    }

    /// <summary>A socket shaped like a port as it is now, not yet plugged in.</summary>
    public static Socket For(Port port)
    {
        ArgumentNullException.ThrowIfNull(port);
        return Shaped(port.Count, port.PlaneCount, Enumerable.Range(0, port.PlaneCount).Where(port.IsConstant));
    }

    internal static Socket Shaped(int lanes, int planes, IEnumerable<int> constants) => new(lanes, Plane.Of(planes, constants));

    public override int Count => lanes;

    public override int PlaneCount => constant.Length;

    /// <summary>The port filling this socket, or null.</summary>
    public Port? Plugged => port;

    /// <summary>Whether a port has this socket's shape: as many planes, constant
    /// where the socket's are. Lane counts may differ; lanes the port lacks
    /// reach nowhere.</summary>
    public bool Fits(Port port)
    {
        ArgumentNullException.ThrowIfNull(port);
        return port.PlaneCount == PlaneCount && Enumerable.Range(0, PlaneCount).All(p => port.IsConstant(p) == constant[p]);
    }

    /// <summary>Plugs a port in. A socket that something takes through is
    /// that port's one taker, so a port another connection takes through is
    /// refused.</summary>
    public void Plug(Port port)
    {
        if (!Fits(port)) throw new ArgumentException("The port's planes do not have this socket's shape.", nameof(port));
        if (taker is not null) port.Claim(this);
        if (!ReferenceEquals(this.port, port)) Unplug();
        this.port = port;
    }

    public void Unplug()
    {
        port?.Release(this);
        port = null;
    }

    /// <summary>A socket that something takes through has one taker, as a port
    /// does: a second connection taking through it, from any structure, would
    /// make step order decide who gets a unit.</summary>
    internal override bool TakenByOther(Wiring taker) =>
        (this.taker is not null && !ReferenceEquals(this.taker, taker)) || (port is not null && !port.Holds(this) && port.IsTaken);

    internal override void Take(Wiring taker)
    {
        port?.Claim(this);
        this.taker = taker;
    }

    public override bool IsLive(int lane) => Reaches(lane) && port!.IsLive(lane);

    /// <summary>One bit through a lane; a lane that reaches nowhere reads clear.</summary>
    public override bool Read(int plane, int lane)
    {
        CheckPlane(plane);
        return Reaches(lane) && port!.Read(plane, lane);
    }

    internal override bool ThroughPort => true;

    internal override bool IsConstant(int plane)
    {
        CheckPlane(plane);
        return constant[plane];
    }

    internal override void Set(int plane, int index, bool value)
    {
        if (!Reaches(index)) throw new InvalidOperationException($"Lane {index} reaches nowhere.");
        port!.Set(plane, index, value);
    }

    internal override (Surface Home, int Index, int Plane) Bit(int index, int plane) =>
        Reaches(index) ? port!.Bit(index, plane) : (this, index, plane);

    internal override void ReadKeys(int index, List<(Surface, int)> keys)
    {
        if (Reaches(index)) port!.ReadKeys(index, keys);
        else keys.Add((this, index));
    }

    internal override (Surface, int) WriteKey(int index) => Reaches(index) ? port!.WriteKey(index) : (this, index);

    private bool Reaches(int lane) => port is not null && (uint)lane < (uint)lanes && lane < port.Count;

    private void CheckPlane(int plane)
    {
        if ((uint)plane >= (uint)PlaneCount) throw new ArgumentOutOfRangeException(nameof(plane));
    }
}
