namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Each public stop denotes one distinct projected position on this
/// owner's straight rail. The owner answers ordinal queries over exported stops.</summary>
public sealed class RailOrderPort
{
    private readonly LandmarkRun local;
    private readonly EndpointRows exports = new();
    public int Count => exports.Count(1, local.Bits);

    public RailOrderPort(LandmarkRun local, params int[] stops)
    {
        if (stops.Length == 0 || stops.Distinct().Count() != stops.Length || stops.Except(local.Sites()).Any())
            throw new ArgumentException("Publish distinct landmarks belonging to the local rail.", nameof(stops));
        this.local = local;
        foreach (int stop in stops) exports.Append(local.Bits, stop);
    }

    public LandmarkRead Compare(int a, int b)
    {
        if ((uint)a >= Count || (uint)b >= Count) return LandmarkRead.Outside;
        return local.Compare(exports.Read(new(1, local.Bits, a))[0], exports.Read(new(1, local.Bits, b))[0]);
    }

    internal bool SharesRail(RailOrderPort other) => ReferenceEquals(local, other.local);
}

/// <summary>A docking surface for two straight rails on a common line. Two
/// seated halves in one socket equate the projected positions of their stops.</summary>
public sealed class RailContacts
{
    private readonly RailOrderPort left, right;
    private EndpointRows sockets = new();
    public int Bits { get; }
    public int SeatBits { get; }
    public int SocketCount => sockets.Count(2, SeatBits);
    public int Count => Enumerable.Range(0, SocketCount).Count(i => SeatCodes(i).All(code => code != 0));
    public int[] SeatCodes(int i) => sockets.Read(new(2, SeatBits, i));
    public byte[] Bytes() => sockets.PackedBytes(2, SeatBits);

    public RailContacts(RailOrderPort left, RailOrderPort right)
    {
        if (left.SharesRail(right)) throw new ArgumentException("Dock two independently owned rails.");
        this.left = left;
        this.right = right;
        int count = Math.Max(left.Count, right.Count);
        Bits = OrderedBoundary.Bits(count);
        SeatBits = OrderedBoundary.Bits(count + 1);
        if (SeatBits > 16) throw new ArgumentException("The packed seat needs one code for an open half.");
    }

    private void Check(int a, int b)
    {
        if ((uint)a >= left.Count || (uint)b >= right.Count)
            throw new ArgumentOutOfRangeException(nameof(a), "Select a published stop on each side.");
    }

    public void Touch(int a, int b)
    {
        Check(a, b);
        sockets.Append(SeatBits, a+1, b+1);
    }

    public int OpenSocket() => sockets.Append(SeatBits, 0, 0).Index;
    public void SeatLeft(int socket, int? port) => Seat(socket, 0, port, left.Count);
    public void SeatRight(int socket, int? port) => Seat(socket, 1, port, right.Count);
    public void Release(int socket) => SetSeats(socket, [0, 0]);

    private void Seat(int socket, int side, int? port, int count)
    {
        if (port is int p && (uint)p >= count) throw new ArgumentOutOfRangeException(nameof(port));
        var codes = SeatCodes(socket);
        codes[side] = (port + 1) ?? 0;
        SetSeats(socket, codes);
    }

    private void SetSeats(int socket, int[] codes)
    {
        if ((uint)socket >= SocketCount) throw new ArgumentOutOfRangeException(nameof(socket));
        var changed = new EndpointRows();
        for (int i = 0; i < SocketCount; i++)
            changed.Append(SeatBits, i == socket ? codes : SeatCodes(i));
        sockets = changed;
    }

    public SpanComparison Compare((int Start, int Tip) a, (int Start, int Tip) b)
    {
        Check(a.Start, b.Start);
        Check(a.Tip, b.Tip);
        return new(this, a, b);
    }

    internal SpanPlacement[] Placements(int[] a, int[] b)
    {
        // Each node is a projected position. Source sites remain with their
        // respective owners. Only contact and query ports enter this read.
        var nodes = new List<(int? Left, int? Right)>();
        var onLeft = new Dictionary<int, int>();
        var onRight = new Dictionary<int, int>();
        for (int i = 0; i < SocketCount; i++)
        {
            var pair = SeatCodes(i);
            if (pair[0] == 0 || pair[1] == 0) continue;
            int aPort = pair[0]-1, bPort = pair[1]-1;
            var hasLeft = onLeft.TryGetValue(aPort, out int l);
            var hasRight = onRight.TryGetValue(bPort, out int r);
            if (hasLeft || hasRight)
            {
                if (!hasLeft || !hasRight || l != r) return [];
                continue; // another witness of the same contact
            }
            onLeft.Add(aPort, nodes.Count);
            onRight.Add(bPort, nodes.Count);
            nodes.Add((aPort, bPort));
        }
        if (nodes.Count == 0) return [];

        int Node(int port, bool leftSide)
        {
            var names = leftSide ? onLeft : onRight;
            if (names.TryGetValue(port, out int found)) return found;
            names.Add(port, nodes.Count);
            nodes.Add(leftSide ? (port, null) : (null, port));
            return nodes.Count - 1;
        }
        int[] roles = [Node(a[0], true), Node(a[1], true), Node(b[0], false), Node(b[1], false)];
        var placements = new List<SpanPlacement>();
        foreach (bool reverse in new[] { false, true })
        {
            var before = new bool[nodes.Count, nodes.Count];
            void Link(int i, int j, LandmarkRead order)
            {
                if (order == LandmarkRead.Before) before[i,j] = true;
                if (order == LandmarkRead.After) before[j,i] = true;
                if (order == LandmarkRead.Conflict) before[i,i] = true;
            }
            for (int i = 0; i < nodes.Count; i++)
                for (int j = i; j < nodes.Count; j++)
                {
                    if (nodes[i].Left is int li && nodes[j].Left is int lj) Link(i, j, left.Compare(li, lj));
                    if (nodes[i].Right is int ri && nodes[j].Right is int rj)
                        Link(i, j, reverse ? right.Compare(rj, ri) : right.Compare(ri, rj));
                }
            LandmarkRun.CloseOrder(before);
            if (Enumerable.Range(0, nodes.Count).Any(i => before[i,i])) continue;

            // Project the contact order onto just four roles. Distinct stops
            // of one owner exclude equality even when their order is open.
            var allowed = new byte[4,4];
            for (int i = 0; i < 4; i++)
                for (int j = i+1; j < 4; j++)
                {
                    int u = roles[i], v = roles[j];
                    bool sameRail = (nodes[u].Left.HasValue && nodes[v].Left.HasValue) ||
                                    (nodes[u].Right.HasValue && nodes[v].Right.HasValue);
                    allowed[i,j] = u == v ? (byte)8 : before[u,v] ? (byte)4 : before[v,u] ? (byte)2 :
                        sameRail ? (byte)6 : (byte)14;
                }
            foreach (byte ranks in SpanPlacement.Orders)
            {
                var candidate = new SpanPlacement(reverse, ranks);
                if (candidate.Fits(allowed)) placements.Add(candidate);
            }
        }
        return placements.ToArray();
    }
}
