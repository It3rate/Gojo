namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Public tip landmarks on the same outward basis. Unknown is an
/// unavailable tip observation. Local dial addresses never cross this port.</summary>
public readonly record struct BowlTips(bool HasLower, int? Upper, int? Lower);

/// <summary>stem -> pinned half-span; return -> (dial, resting tip, farther tip);
/// reserve -> waiting -> dial. A shared dial receives a single request.</summary>
public sealed class BowlReach
{
    // Positions along the outward ray supply order. Their local names do not.
    public static LandmarkRun Outward { get; } = new(3, [0, 3, 1, 4, 2]);
    private readonly PortStructure owner = new();
    private readonly EndpointRows tips = new(), pins = new();
    private readonly PassageConnections offer, settle;
    private readonly ConnectionRow offerRow;
    private ConnectionRow? settleRow;
    private readonly int reserve, waiting, free, locked, halfSpan;
    public CurveInk Ink { get; }
    public BowlTips Before { get; private set; }
    public bool HasLower => tips.Count(4,3) > 1;
    public bool Pushed => owner.ReadPort(reserve) == 0;
    public bool Waiting => owner.ReadPort(waiting) != 0;
    public int SiteCount { get; private set; }

    private BowlReach(CurveInk ink)
    {
        Ink = ink;
        reserve = Site(true); waiting = Site(false);
        free = Site(false); locked = Site(true); halfSpan = Site(false);
        offer = new(owner, PassageLaw.Unconditional());
        settle = new(owner, PassageLaw.Controlled(whenPresent:false));
        offerRow = offer.Connect(3,reserve,free,waiting);
        // Each straight stem span pins its half-height adjustment. Both B
        // spans in this hand have equal height; only their widths differ.
        foreach (var row in ink.DriveLinks().Where(r => r[1] == 0))
            pins.Append(3,row[0],halfSpan,locked);
    }

    public static BowlReach AdjustableD()
    {
        var body = new BowlReach(CurvedCapitals.Ink("D"));
        body.BindTip(body.Ink.Probe[0],body.Site(false),3,1);
        return body.Ready();
    }

    public static BowlReach RadialD()
    {
        var body = new BowlReach(CurvedCapitals.Ink("D · half of O"));
        // The return is one of two equal arcs. Its transverse radius is
        // the same length role as half the pinned stem diameter.
        body.BindTip(body.Ink.Probe[0],body.halfSpan,3,1);
        return body.Ready();
    }

    public static BowlReach EndsOnlyD() => new BowlReach(CurvedCapitals.Ink("D · ends only")).Ready();

    public static BowlReach B(bool shared)
    {
        var body = new BowlReach(CurvedCapitals.Ink("B"));
        int upper = body.Site(false), first = body.Ink.Probe[0];
        body.BindTip(first,upper,3,1);
        body.BindTip(first+1,shared ? upper : body.Site(false),4,2);
        return body.Ready();
    }

    private int Site(bool present)
    {
        SiteCount++;
        return owner.Publish(owner.Create(0,0,1,present ? 1UL : 0UL));
    }

    private void BindTip(int edge, int dial, int rest, int farther)
    {
        if (Outward.Compare(rest,farther) != LandmarkRead.Before)
            throw new ArgumentException("The second tip must lie farther along the outward ray.");
        tips.Append(3,edge,dial,rest,farther);
    }

    private BowlReach Ready()
    {
        var rows = TipRows();
        if (rows.Length > 0)
        {
            int dial = rows[0][1]; // the offered action is on the upper bowl
            var pin = PinRows().FirstOrDefault(row => row[1] == dial);
            settleRow = settle.Connect(3,waiting,pin == null ? free : pin[2],dial);
        }
        Before = ReadTips();
        return this;
    }

    public void Push()
    {
        if (Pushed) return; // this trial supplied one request
        Step(offer,offerRow);
        if (settleRow is { } route) Step(settle,route);
    }

    private static void Step(PassageConnections table, ConnectionRow row)
    {
        if (!table.TryStep(row,table.Observe(row),out _))
            throw new InvalidOperationException("The local request passage did not resolve.");
    }

    public BowlTips ReadTips()
    {
        var rows = TipRows();
        int? Tip(int i) => i < rows.Length ? rows[i][2 + owner.ReadPort(rows[i][1])] : null;
        return new(HasLower,Tip(0),Tip(1));
    }

    public int[][] TipRows() => Rows(tips,4);
    public int[][] PinRows() => Rows(pins,3);
    private static int[][] Rows(EndpointRows table,int arity) =>
        Enumerable.Range(0,table.Count(arity,3)).Select(i=>table.Read(new(arity,3,i))).ToArray();
    public byte[] TipBytes() => tips.PackedBytes(4,3);
    public byte[] PinBytes() => pins.PackedBytes(3,3);
    public byte[] StoreBytes() => owner.PackedBytes(0,0,1);
    public int[] OfferRow() => offer.Wiring(offerRow);
    public int[][] SettleRows() => settleRow is { } row ? [settle.Wiring(row)] : [];
    public byte[] SettleBytes() => settle.PackedRows(3);
    public byte[] SettleLaw() => settle.LawBytes();
}

/// <summary>A second reader has only tip publications from supplied reference
/// trials. Mask bit m admits a joint movement pattern (upper=1, lower=2).</summary>
public static class BowlMotionReader
{
    private static readonly (BowlTips Before, byte Support)[] Trials =
        new[]{BowlReach.AdjustableD(),BowlReach.RadialD(),BowlReach.B(false),BowlReach.B(true)}
        .Select(reference =>
        {
            reference.Push();
            return (reference.Before,Compare(reference.Before,reference.ReadTips()));
        }).ToArray();

    public static byte Predict(BowlTips before)
    {
        byte support = 0;
        foreach (var reference in Trials)
            if (before.HasLower == reference.Before.HasLower &&
                (before.Upper == null || before.Upper == reference.Before.Upper) &&
                (before.Lower == null || before.Lower == reference.Before.Lower))
                support |= reference.Support;
        return support;
    }

    public static byte Compare(BowlTips before, BowlTips after)
    {
        if (before.HasLower != after.HasLower) return 0;
        static byte Movement(int? from,int? to) => (from,to) switch
        {
            (int a,int b) => BowlReach.Outward.Compare(a,b) switch
            {
                LandmarkRead.SameSite => 1, LandmarkRead.Before => 2, _ => 0
            },
            _ => 3
        };
        var upper = Movement(before.Upper,after.Upper);
        if (!before.HasLower) return upper;
        var lower = Movement(before.Lower,after.Lower);
        return (byte)(((lower & 1) != 0 ? upper : 0) | ((lower & 2) != 0 ? upper << 2 : 0));
    }
}
