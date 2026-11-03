namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Each local arm port binds to [run, span, reverse on arrival].
/// The same arm read backwards is an exit. Passage rows use those ports.</summary>
public sealed class GlyphJunction
{
    private readonly EndpointRows arms = new();
    public StrokeGraph Ink { get; }
    public PassageFixture Flow { get; }
    public byte Before { get; }
    public int Centre { get; }
    private GlyphJunction(StrokeGraph ink, byte initial)
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
        Flow = new PassageFixture(4,initial);
        Before = Flow.Stored;
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
