using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.OrnamentPathCampaign;

public readonly record struct PathInput(int A, int B, int Pitch, int Cells = 4, int Phase = 0, int SeedBit = 0);
public enum PathMode { Live, Cut, Frozen, Open }
public readonly record struct PathSite(int X, int Y);
public readonly record struct PathEdge(int From, int To, int Owner, int Beat, int Cell, int Row, int Direction, int Extent);
public readonly record struct PathEvent(int Owner, int Beat, int Cell, int Row, string Kind, int From, int To, int Support)
{
    public int BeforeFrame { get; init; }
    public int AfterFrame { get; init; }
}
public sealed record PathTrace(PathSite[] Sites, PathEdge[] Edges, PathEvent[] Events, int Pitch, int Cells, int Owners)
{
    public byte[] FinalProgram { get; init; } = [];
}

// All addresses are positions within one owner or its exposed incoming port.
// Geometry is a realization of the shared A, B, A+B and period slots.
public readonly record struct StrokeRow(byte Motion, byte Gate)
{
    public int Direction => Motion & 7;
    public int Extent => (Motion >> 3) & 3;
    public int Source => (Motion >> 5) & 3;
    public bool Participates => (Motion & 128) != 0;
    public int Law => Gate & 15;
    public int Target => (Gate >> 4) & 3;
    public bool Save => (Gate & 64) != 0;
    public bool Stop => (Gate & 128) != 0;

    public static StrokeRow Bind(int direction, int extent = 0, int source = 0, int target = 0,
        int law = 15, bool save = false, bool stop = false, bool pen = true)
    {
        if ((uint)direction > 7 || (uint)extent > 3 || (uint)source > 3 || (uint)target > 3 || (uint)law > 15)
            throw new ArgumentOutOfRangeException(nameof(direction), "Field exceeds its positional width.");
        return new((byte)(direction | extent << 3 | source << 5 | (pen ? 128 : 0)),
            (byte)(law | target << 4 | (save ? 64 : 0) | (stop ? 128 : 0)));
    }
}

public sealed record PathOwner(byte Frame, byte Clock, byte Control, StrokeRow[] Rows);

// A coupled pen can select a native directional/extent lane before the stroke.
// Bound targets and the existing bit gates remain enforced by the path owner.
public readonly record struct PenOffer(int Owner, int Beat, int Cell, int Row, int From, PathSite Start,
    int Direction, int Extent, int Length, int Target, PathSite? TargetPoint, int Anchor, PathSite AnchorPoint,
    int NextAnchor, PathSite NextAnchorPoint, bool Last, bool Stop,bool AutoNext);
public readonly record struct PenResponse(int Direction, int Extent, bool Admit, int BindTo = -1);
public interface IPathCoupling
{
    void BeginBeat(int beat,int cell);
    PenResponse Offer(PenOffer offer);
    void Drew(PathEdge edge, PathSite start, PathSite end);
    void EndCell(int cell,int beat,int owner,int cursor,int anchor,int nextAnchor,bool stopped);
}

public static partial class PathMachine
{
    private const byte Version = 1;
    private const byte VerticalFlip = 16;
    private static readonly int[] Dx = [1,1,0,-1,-1,-1,0,1];
    private static readonly int[] Dy = [0,1,1,1,0,-1,-1,-1];
    private static readonly byte[,,] Gates = CompileGates();

    // Precompile from the same 3-role ODV bit relations as the rail machine.
    // An input omitted by the Boolean law is not a structural dependency.
    private static byte[,,] CompileGates()
    {
        var result = new byte[16, 2, 4];
        for (int law = 0; law < 16; law++)
        {
            var relation = BitRelation.FromTuples(3, from a in Enumerable.Range(0, 2)
                from b in Enumerable.Range(0, 2)
                select new byte[] { (byte)a, (byte)b, (byte)((law >> (a + 2 * b)) & 1) });
            bool needsPeer = (law & 3) != (law >> 2);
            for (int a = 0; a < 2; a++)
                for (byte b = 0; b < 4; b++)
                    result[law,a,b] = relation.Predict([(byte)(1 << a), needsPeer ? b : (byte)1]);
        }
        return result;
    }

    public static byte[] Encode(params PathOwner[] owners)
    {
        ArgumentNullException.ThrowIfNull(owners);
        if (owners.Length is < 1 or > 3) throw new ArgumentException("One to three path owners are required.");
        var packed = new List<byte> { Version, (byte)owners.Length };
        foreach (var o in owners)
        {
            if (o.Rows.Length is < 1 or > 8 || (o.Frame & 0xc0) != 0 || (o.Control & 0xc0) != 0)
                throw new ArgumentException("Invalid path table width or reserved bits.");
            packed.Add((byte)o.Rows.Length);
            packed.Add(o.Frame);
            packed.Add(o.Clock);
            packed.Add(o.Control);
            foreach (var row in o.Rows) { packed.Add(row.Motion); packed.Add(row.Gate); }
        }
        return packed.ToArray();
    }

    public static PathOwner[] Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 2 || bytes[0] != Version || bytes[1] is < 1 or > 3)
            throw new FormatException("Invalid path program header.");
        var owners = new PathOwner[bytes[1]];
        int at = 2;
        for (int i = 0; i < owners.Length; i++)
        {
            if (at + 4 > bytes.Length) throw new FormatException("Truncated owner header.");
            int count = bytes[at++];
            byte frame = bytes[at++], clock = bytes[at++], control = bytes[at++];
            if (count is < 1 or > 8 || at + 2*count > bytes.Length || (frame & 0xc0) != 0 || (control & 0xc0) != 0)
                throw new FormatException("Invalid owner table.");
            var rows = new StrokeRow[count];
            for (int j = 0; j < count; j++) rows[j] = new(bytes[at++], bytes[at++]);
            owners[i] = new(frame, clock, control, rows);
        }
        if (at != bytes.Length) throw new FormatException("Unexpected trailing path bits.");
        return owners;
    }

    private static int Lane(int frame, PathInput input) => (frame & 7) switch
    {
        1 => input.B, 2 => -input.B, 3 => 2*input.B, 4 => -2*input.B,
        5 => input.A, 6 => -input.A, 7 => input.A+input.B, _ => 0
    };

    public static PathTrace Run(byte[] program, PathInput input, PathMode mode = PathMode.Live)
        => Run(program,input,mode,null);

    public static PathTrace Run(byte[] program, PathInput input, PathMode mode, IPathCoupling? coupling)
    {
        if (input.A is < 1 or > 8192 || input.B is < 1 or > 8192 || input.Pitch is < 1 or > 8192 ||
            input.Cells is < 1 or > 64 || input.SeedBit is < -1 or > 1 || (uint)mode > (uint)PathMode.Open)
            throw new ArgumentOutOfRangeException(nameof(input));
        var owners = Decode(program);
        int n = owners.Length, body = owners.Max(o => o.Rows.Length);
        int visits = n * body * input.Cells;
        var sites = new List<PathSite>(visits + n * (input.Cells+1));
        var edges = new List<PathEdge>(visits);
        var events = new List<PathEvent>(visits * 2);
        var root = new int[n,input.Cells+1];
        var frame = new byte[n];
        var cursor = new int[n];
        var saved = new int[n];
        var halted = new bool[n];
        var publishedSite = new int[n];
        var publishedPulse = new byte[n];
        var nextPulse = new byte[n];
        int[] extents = [input.A, input.B, input.A + input.B, input.Pitch];
        for (int o = 0; o < n; o++)
        {
            frame[o] = owners[o].Frame;
            cursor[o] = -1;
            for (int c = 0; c <= input.Cells; c++)
            {
                root[o,c] = sites.Count;
                sites.Add(new(c * input.Pitch, Lane(frame[o],input)));
            }
        }

        for (int c = 0; c < input.Cells; c++)
        {
            for (int o = 0; o < n; o++)
            {
                int anchor = root[o,c];
                events.Add(new(o,c*body,c,-1,cursor[o] == anchor ? "continue" : "start",cursor[o],anchor,2));
                cursor[o] = saved[o] = publishedSite[o] = anchor;
                halted[o] = false;
                if (c == 0) publishedPulse[o] = input.SeedBit < 0 ? (byte)3 : (byte)(1 << input.SeedBit);
            }
            for (int r = 0; r < body; r++)
            {
                int beat = c*body+r;
                coupling?.BeginBeat(beat,c);
                Array.Fill(nextPulse, (byte)1);
                for (int o = 0; o < n; o++)
                {
                    var owner = owners[o];
                    if (halted[o] || r >= owner.Rows.Length) continue;
                    var row = owner.Rows[r];
                    int role = owner.Control & 3;
                    int peer = role switch { 0 => (o+n-1)%n, 1 => (o+1)%n, _ => o };
                    bool detached = mode == PathMode.Cut && role < 2 || role == 3;
                    byte pulse = role == 3 ? (byte)1 : detached ? (byte)0 : publishedPulse[peer];
                    int exposed = detached ? -1 : publishedSite[peer];
                    int clock = (owner.Clock >> ((input.Phase+beat)&7)) & 1;
                    byte support = mode == PathMode.Open ? (byte)2 : Gates[row.Law,clock,pulse];
                    int before = cursor[o];
                    if (support != 2)
                    {
                        events.Add(new(o,beat,c,r,support == 1 ? "closed" : "unresolved",before,before,support));
                        nextPulse[o] = support;
                    }
                    else
                    {
                        if (!row.Participates)
                        {
                            events.Add(new(o,beat,c,r,"rest",before,before,2));
                        }
                        else
                        {
                            int from = row.Source switch { 0 => before, 1 => root[o,c], 2 => saved[o], _ => exposed };
                            int target = row.Target switch { 0 => -1, 1 => saved[o], 2 => exposed, _ => root[o,c+1] };
                            int dir = row.Direction;
                            int extent = row.Extent;
                            if ((frame[o] & 16) != 0) dir = (8-dir)&7;
                            if ((frame[o] & 32) != 0) dir = (4-dir)&7;
                            bool admitted = from >= 0 && (row.Target == 0 || target >= 0);
                            if (admitted && coupling != null)
                            {
                                var response=coupling.Offer(new(o,beat,c,r,from,sites[from],dir,extent,extents[extent],
                                    target,target<0 ? null : sites[target],root[o,c],sites[root[o,c]],root[o,c+1],sites[root[o,c+1]],
                                    r==owner.Rows.Length-1,row.Stop,(frame[o]&8)!=0));
                                if ((uint)response.Direction>=8 || (uint)response.Extent>=4 ||
                                    response.BindTo != -1 && response.BindTo != root[o,c] && response.BindTo != root[o,c+1])
                                    throw new InvalidOperationException("Illegal pen response.");
                                admitted=response.Admit && (target<0 || response.BindTo<0 || response.BindTo==target);
                                if (target<0 && response.BindTo>=0) target=response.BindTo;
                                dir=response.Direction;
                                extent=response.Extent;
                            }
                            PathSite end = default;
                            if (admitted)
                            {
                                var p = sites[from];
                                end = new(p.X + Dx[dir]*extents[extent], p.Y + Dy[dir]*extents[extent]);
                                if (target >= 0 && sites[target] != end) admitted = false;
                                // A stored seam attachment can bind only to the owner's next actual root.
                                if (target < 0 && (frame[o] & 8) != 0 && end == sites[root[o,c+1]])
                                    target = root[o,c+1];
                            }
                            if (admitted)
                            {
                                bool joined = target >= 0;
                                if (!joined) { target = sites.Count; sites.Add(end); }
                                var edge=new PathEdge(from,target,o,beat,c,r,dir,extent);
                                edges.Add(edge);
                                coupling?.Drew(edge,sites[from],end);
                                if (from != before) events.Add(new(o,beat,c,r,"branch",from,target,2));
                                if (joined) events.Add(new(o,beat,c,r,"merge",from,target,2));
                                events.Add(new(o,beat,c,r,"draw",from,target,2));
                                cursor[o] = target;
                                if (row.Save) saved[o] = target;
                                nextPulse[o] = 2;
                            }
                            else
                            {
                                events.Add(new(o,beat,c,r,"blocked",from,target,support));
                            }
                        }
                        if (row.Stop) { halted[o] = true; events.Add(new(o,beat,c,r,"stop",cursor[o],cursor[o],2)); }
                    }
                    // The same prior-beat port and the local clock can write a field of this owner's
                    // own frame. Its changed bits act on subsequent rows and survive the cell seam.
                    if (mode != PathMode.Frozen && Gates[owner.Control >> 2,clock,pulse] == 2)
                    {
                        byte old = frame[o];
                        frame[o] ^= VerticalFlip;
                        events.Add(new PathEvent(o,beat,c,r,"switch",cursor[o],cursor[o],2)
                            { BeforeFrame = old, AfterFrame = frame[o] });
                    }
                }
                Array.Copy(nextPulse, publishedPulse, n);
                Array.Copy(cursor, publishedSite, n);
            }
            if (coupling != null)
                for(int o=0;o<n;o++) coupling.EndCell(c,(c+1)*body-1,o,cursor[o],root[o,c],root[o,c+1],halted[o]);
        }
        var updated = (byte[])program.Clone();
        for (int o = 0, i = 2; o < n; o++)
        {
            updated[i+1] = frame[o];
            i += 4+2*owners[o].Rows.Length;
        }
        return new(sites.ToArray(), edges.ToArray(), events.ToArray(),input.Pitch,input.Cells,n) { FinalProgram = updated };
    }
}
