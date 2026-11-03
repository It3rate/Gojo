namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Each local arm port binds to [run, span, reverse on arrival].
/// The same arm read backwards is an exit. Passage rows use those ports.</summary>
public sealed class GlyphJunction
{
    private readonly EndpointRows arms = new();
    private readonly EndpointRows outlet = new();
    private CalculatedPort? collected;
    public StrokeGraph Ink { get; }
    public PassageFixture Flow { get; }
    public IPrimitiveReadPort Stem => Surface(2);
    public ReadConnections? Collection { get; private set; }
    public int[]? OutletBinding => outlet.Count(2,2)==0 ? null : outlet.Read(new(2,2,0));
    public byte[] OutletBytes() => outlet.PackedBytes(2,2);
    public byte Before { get; }
    public int Centre { get; }
    private GlyphJunction(StrokeGraph ink, byte initial, int siteCount = 4)
    {
        Ink = ink;
        Centre = new StrokeFeatures(ink).Joints.Single(j => j.Arms == 3).Site;
        for (var run = 0; run < ink.RunCount; run++)
        {
            var sites = ink.Run(run).Sites();
            for (var slot = 0; slot < sites.Length - 1; slot++)
            {
                if (sites[slot] != Centre && sites[slot+1] != Centre)
                    throw new ArgumentException("Each span must reach the local joint.");
                arms.Append(2,run,slot,sites[slot] == Centre ? 1 : 0);
            }
        }
        Flow = new PassageFixture(siteCount,initial);
        Before = Flow.Stored;
    }

    public IPrimitiveReadPort Surface(int arm)
    {
        if ((uint)arm>=3) throw new ArgumentOutOfRangeException(nameof(arm));
        if (collected is {} port && OutletBinding is {} binding && binding[0]==arm && binding[1]==port.Endpoint)
            return port;
        return Flow.Owner.Connect(Flow.Ports[arm]);
    }

    public int[] Arm(int port) => arms.Read(new(3,2,port));
    public byte[] ArmBytes() => arms.PackedBytes(3,2);
    public int[] Arrival(int port)
    {
        var row = Arm(port);
        var sites = Ink.Run(row[0]).Sites();
        return [sites[row[1] + row[2]], Centre];
    }
    private void IntoStem()
    {
        Flow.Add(Flow.Forward,0,3,2);
        Flow.Add(Flow.Forward,1,3,2);
    }
    private void Choice(int from, int ifClear, int ifSet)
    {
        Flow.Add(Flow.Absent,from,3,ifClear);
        Flow.Add(Flow.Present,from,3,ifSet);
    }

    public static GlyphJunction YMerge(byte initial)
    {
        var joint = new GlyphJunction(StraightCapitals.Build("Y"),initial);
        joint.IntoStem();
        return joint;
    }
    public static GlyphJunction YBranch(byte initial)
    {
        var joint = new GlyphJunction(StraightCapitals.Build("Y"),initial);
        joint.Choice(2,0,1);
        return joint;
    }
    public static GlyphJunction YSelectInlet(byte initial)
    {
        var joint = new GlyphJunction(StraightCapitals.Build("Y"),initial);
        joint.Flow.Add(joint.Flow.Absent,0,3,2);
        joint.Flow.Add(joint.Flow.Present,1,3,2);
        return joint;
    }
    public static GlyphJunction YCollect(byte initial)
    {
        var joint = new GlyphJunction(StraightCapitals.Build("Y"),initial,5);
        var flow = joint.Flow;
        flow.Add(flow.Forward,0,3,2);
        flow.Add(flow.Forward,1,3,4);
        var reads = new ReadConnections();
        var first = reads.Import(flow.Owner.Connect(flow.Ports[2]));
        var second = reads.Import(flow.Owner.Connect(flow.Ports[4]));
        var presence = reads.Connect(reads.AddTable(ConnectionLaws.Presence()),2,first,second);
        joint.outlet.Append(2,2,presence.Endpoint);
        joint.collected = presence;
        joint.Collection = reads;
        return joint;
    }
    public static GlyphJunction TCorners(byte initial)
    {
        var joint = new GlyphJunction(StraightCapitals.Build("T"),initial);
        joint.IntoStem();
        return joint;
    }
    public static GlyphJunction TBar(byte initial, bool optionalTurn)
    {
        var joint = new GlyphJunction(StraightCapitals.Build("T"),initial);
        if (optionalTurn) joint.Choice(0,1,2);
        else joint.Flow.Add(joint.Flow.Forward,0,3,1);
        return joint;
    }
    public byte Advance() => Flow.TryStep(Flow.Observe(),out var after)
        ? after : throw new InvalidOperationException("The supplied junction did not settle.");
}
