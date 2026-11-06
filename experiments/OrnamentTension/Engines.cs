using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Experiments.OrnamentTension;

public sealed record EngineEntry(string Key, string Name, string Continuity, string Description);

public static class EngineLibrary
{
    public static readonly EngineEntry[] Entries = [
        new("running-key","Running key","through","One pen turns back inside the bay and exits at the next anchor."),
        new("inward-spiral","Inward square spiral","terminal","A rectangular course winds inward and stops on the inside."),
        new("zigzag","Through zigzag","through","A rising and falling diagonal share one material tip."),
        new("opposed-chevron","Opposed chevrons","through","Two mirrored courses frame repeating lozenges."),
        new("crossing-ribbons","Crossing ribbons","through","Two offset diagonal courses cross freely inside each bay."),
        new("branching-spray","Branching spray","through","Two subordinate pens attach opposite limbs to a continuing stem."),
        new("alternating-pinline","Alternating pinline","through","A writable frame reflects the angular pulse in alternate bays over a narrow rule."),
        new("counterstep","Counterstep","through","Two separated angular registers ascend and descend on opposite sides.")
    ];
    public static readonly string[] Names = Entries.Select(e=>e.Key).ToArray();

    private static StrokeRow Step(int dir,int extent=0,int source=0,int target=0,int law=15,bool stop=false) =>
        StrokeRow.Bind(dir,extent,source,target,law,stop:stop);
    private static StrokeRow Wait() => StrokeRow.Bind(0,pen:false);
    private static PathOwner Owner(StrokeRow[] rows,int frame=8,int control=0,int clock=85) =>
        new((byte)frame,(byte)clock,(byte)control,rows);

    public static byte[] Create(int family)
    {
        // References 0..3 choose A, B, A+B, pitch. Equal references participate
        // in the same calibrated extent. The training label is absent from the bytes.
        PathOwner[] pens=family switch
        {
            0 => [Owner([Step(0,1),Step(2),Step(4,1),Step(2,1),Step(0,2),Step(6,2),Step(0,1)])],
            1 => [Owner([Step(0,2),Step(2,2),Step(4,2),Step(6),Step(0),Step(2,1),Step(4,1,stop:true)],0)],
            2 => [Owner([Step(1),Step(7)])],
            3 => [Owner([Step(1),Step(7)]),Owner([Step(1),Step(7)],24)],
            4 => [Owner([Step(1),Step(7)]),Owner([Step(7),Step(1)],13)],
            5 => [Owner([Step(0),Step(0)]),
                  Owner([Wait(),Step(1,1,source:3,law:12),Step(0,1),Step(3,1,stop:true)]),
                  Owner([Wait(),Step(1,1,source:3,law:12),Step(0,1),Step(3,1,stop:true)],24,1)],
            // The five-row body has odd length. A constant truth row flips the
            // frame on each beat. The authored directions compensate within one
            // body and reverse their y interpretation on the next body.
            6 => [Owner([Step(0),Step(7,1),Step(0),Step(1,1),Step(0,1)],8,63),
                  Owner([Step(0,3)],12)],
            7 => [Owner([Step(2),Step(0,1),Step(2,1),Step(0),Step(6,2),Step(0,1)],9),
                  Owner([Step(2),Step(0,1),Step(2,1),Step(0),Step(6,2),Step(0,1)],28)],
            _ => throw new ArgumentOutOfRangeException(nameof(family))
        };
        return PathMachine.Encode(pens);
    }

    public static byte[] Create(Random rng,int family,int edits=1)
    {
        ArgumentNullException.ThrowIfNull(rng);
        ArgumentOutOfRangeException.ThrowIfNegative(edits);
        var bytes=Create(family);
        for(int i=0;i<edits;i++) bytes=Mutate(bytes,rng);
        return bytes;
    }

    public static byte[] Mutate(byte[] bytes, Random rng) => PathMachine.Mutate(bytes,rng);
    public static byte[] Cross(byte[] first,byte[] second,Random rng) => PathMachine.Cross(first,second,rng);
}
