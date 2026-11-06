namespace Gojo.Experiments.OrnamentPathCampaign;

public static partial class PathMachine
{
    private static StrokeRow Step(int d, int e = 0, int src = 0, int dst = 0, int gate = 15,
        bool save = false, bool stop = false) => StrokeRow.Bind(d,e,src,dst,gate,save,stop);
    private static StrokeRow Rest(bool stop = false) => StrokeRow.Bind(0,pen:false,stop:stop);
    private static PathOwner Body(StrokeRow[] rows, byte frame = 8, byte control = 0, byte clock = 0x55)
        => new(frame,clock,control,rows);

    // Common starting bodies are search priors. Their names never enter execution.
    // Each is expressed solely as rows of the same motion, reference and bit-law grammar.
    public static byte[] Foundation(int family)
    {
        PathOwner[] owners = family switch
        {
            0 => [Body([Step(0),Step(2,1),Step(4,1),Step(2,1),Step(0),Step(6,1),Step(0,1),Step(6,1)]),
                  Body([Step(0,3)], 12)],
            1 => [Body([Step(1),Step(7)]), Body([Step(1),Step(7)],18)],
            2 => [Body([Step(1),Step(7),Rest()]),
                  Body([Step(7,src:3),Rest(),Step(1,dst:2)])],
            3 => [Body([Step(0),Step(0)]),
                  Body([Rest(),Step(1,1,3,gate:12),Step(0,1),Step(5,1),Rest(stop:true)])],
            4 => [Body([Step(2,1),Step(0),Step(6,1),Step(0)]),
                  Body([Step(0),Step(6,1),Step(4,1),Step(2,1)], 10)],
            5 => [Body([Step(0),Step(2,1),Step(4,1),Step(6,1),Step(0,2)]),
                  Body([Step(0),Step(2,1),Step(4,1),Step(6,1),Step(0,2)], 28)],
            _ => throw new ArgumentOutOfRangeException(nameof(family))
        };
        return Encode(owners);
    }

    public static byte[] Create(Random rng) => Create(rng,-1);

    public static byte[] Create(Random rng, int family)
    {
        ArgumentNullException.ThrowIfNull(rng);
        if (family == -1 && rng.Next(5) == 0) return CreateUnrestricted(rng);
        var bytes = Foundation(family == -1 ? rng.Next(6) : family);
        int mutations = rng.Next(1,7);
        for (int i = 0; i < mutations; i++) bytes = Mutate(bytes,rng);
        return bytes;
    }

    // A matched search can start from unconstrained, well-formed programs.
    public static byte[] CreateUnrestricted(Random rng)
    {
        int size = rng.Next(1,4);
        var owners = new PathOwner[size];
        for (int i = 0; i < size; i++) owners[i] = RandomOwner(rng);
        return Encode(owners);
    }

    private static StrokeRow RandomRow(Random rng) => StrokeRow.Bind(rng.Next(8),rng.Next(4),
        rng.Next(3) == 0 ? rng.Next(4) : 0, rng.Next(6) == 0 ? rng.Next(1,4) : 0,
        rng.Next(3) == 0 ? rng.Next(16) : 15, rng.Next(4) == 0, rng.Next(24) == 0, rng.Next(12) != 0);

    private static PathOwner RandomOwner(Random rng)
    {
        var rows = new StrokeRow[rng.Next(2,9)];
        for (int r = 0; r < rows.Length; r++) rows[r] = RandomRow(rng);
        return new((byte)rng.Next(64),(byte)rng.Next(256),
            (byte)(rng.Next(4) | (rng.Next(3) == 0 ? rng.Next(16) << 2 : 0)),rows);
    }

    public static byte[] Mutate(byte[] program, Random rng)
    {
        var parts = Decode(program).ToList();
        int i = rng.Next(parts.Count);
        var owner = parts[i];
        var rows = owner.Rows.ToList();
        int j = rng.Next(rows.Count);
        var row = rows[j];
        switch (rng.Next(15))
        {
            case 0:
                rows[j] = row with { Motion = (byte)((row.Motion & ~7) | ((row.Direction + (rng.Next(2)==0 ? 1 : 7)) & 7)) };
                break;
            case 1:
                rows[j] = row with { Motion = (byte)(row.Motion ^ (1 << rng.Next(3,5))) };
                break;
            case 2:
                rows[j] = row with { Motion = (byte)(row.Motion ^ (1 << rng.Next(5,7))) };
                break;
            case 3:
                rows[j] = row with { Gate = (byte)(row.Gate ^ (1 << rng.Next(4,6))) };
                break;
            case 4:
                rows[j] = row with { Gate = (byte)(row.Gate ^ (1 << rng.Next(4))) };
                break;
            case 5:
                owner = owner with { Frame = (byte)(owner.Frame ^ (1 << rng.Next(6))) };
                break;
            case 6:
                owner = owner with { Clock = (byte)(owner.Clock ^ (1 << rng.Next(8))) };
                break;
            case 7:
                owner = owner with { Control = (byte)(owner.Control ^ (1 << rng.Next(6))) };
                break;
            case 8:
                if (rows.Count < 8) rows.Insert(j,rng.Next(2) == 0 ? row : RandomRow(rng));
                else rows.RemoveAt(j);
                break;
            case 9:
                if (rows.Count > 1) rows.RemoveAt(j);
                else rows.Add(RandomRow(rng));
                break;
            case 10:
                int k = rng.Next(rows.Count);
                (rows[k], rows[j]) = (rows[j], rows[k]);
                break;
            case 11:
                rows[j] = row with { Gate = (byte)(row.Gate ^ (1 << rng.Next(6,8))) };
                break;
            case 12:
                rows[j] = row with { Motion = (byte)(row.Motion ^ 128) };
                break;
            case 13:
                if (parts.Count < 3 && (parts.Count == 1 || rng.Next(3) != 0))
                    parts.Insert(i+1,rng.Next(2)==0 ? owner with { Frame = (byte)(owner.Frame ^ 16) } : RandomOwner(rng));
                else if (parts.Count > 1) { parts.RemoveAt(i); return Encode(parts.ToArray()); }
                break;
            case 14:
                // Exchanging a binding across a whole body preserves repeated participation in a common extent.
                int oldSlot = row.Extent, newSlot = (oldSlot+rng.Next(1,4))&3;
                for (int at = 0; at < rows.Count; at++)
                    if (rows[at].Extent == oldSlot)
                        rows[at] = rows[at] with { Motion = (byte)((rows[at].Motion & ~24) | newSlot << 3) };
                break;
        }
        parts[i] = owner with { Rows = rows.ToArray() };
        return Encode(parts.ToArray());
    }

    public static byte[] Cross(byte[] first, byte[] second, Random rng)
    {
        var a = Decode(first);
        var b = Decode(second);
        int i = rng.Next(a.Length);
        var other = b[rng.Next(b.Length)];
        if (rng.Next(2) == 0) a[i] = other;
        else
        {
            var before = a[i].Rows;
            int prefix = rng.Next(before.Length+1);
            var body = before.Take(prefix).Concat(other.Rows.Skip(rng.Next(other.Rows.Length))).Take(8).ToArray();
            a[i] = a[i] with { Rows = body };
        }
        return Encode(a);
    }
}
