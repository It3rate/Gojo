namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Occupied points and open gaps on one ordered rail. Even bit 2i
/// names point i in storage order; bit 2i+1 names the whole open gap after it.
/// No bit represents a distance. Boolean results use the same rail.</summary>
public sealed class OrdinalInk
{
    public LandmarkRun Rail { get; }
    public ulong Bits { get; }
    private ulong Domain => (1UL << (2*Rail.Count-1)) - 1;
    public bool ContainsInterval => (Bits & 0xAAAAAAAAAAAAAAAAUL) != 0;
    private OrdinalInk(LandmarkRun rail, ulong bits) { Rail = rail; Bits = bits; }

    public static OrdinalInk Empty(LandmarkRun rail)
    {
        if (!rail.IsChain || rail.Count > 32) throw new ArgumentException("Supply an ordered rail of up to 32 stops.");
        return new(rail, 0);
    }

    public static OrdinalInk Closed(LandmarkRun rail, int first, int last)
    {
        _ = Empty(rail);
        var stops = rail.Sites();
        int a = Array.IndexOf(stops,first), b = Array.IndexOf(stops,last);
        if (a < 0 || b < 0) throw new ArgumentException("A span needs two exposed stops on its rail.");
        int lo = 2*Math.Min(a,b), hi = 2*Math.Max(a,b);
        return new(rail, ((1UL << (hi-lo+1))-1) << lo);
    }

    // Bit a+2b in the truth table selects each atomic point/gap. A foreign
    // rail has no selected result until its landmarks have a correspondence.
    public OrdinalInk? Combine(OrdinalInk other, byte truth)
    {
        if (truth > 15) throw new ArgumentOutOfRangeException(nameof(truth));
        if (!ReferenceEquals(Rail,other.Rail)) return null;
        ulong bits = 0;
        for (int pair = 0; pair < 4; pair++)
            if ((truth & (1<<pair)) != 0)
                bits |= ((pair & 1) == 0 ? ~Bits : Bits) & ((pair & 2) == 0 ? ~other.Bits : other.Bits);
        return new(Rail, bits & Domain);
    }
}

/// <summary>A separate reader of ink projected onto the forward H rail.
/// Every advancing beat exposes its next landmark. A vertical stroke covers
/// just its current point in this projection. Unseen ink can be present or absent.</summary>
public sealed class BorderCoverage : IPrimitiveReadPort
{
    public OrdinalInk Certain { get; }
    public OrdinalInk Possible { get; }
    public OrdinalInk Required { get; }
    public OrdinalInk DefiniteWhite => Required.Combine(Possible,2)!;
    public OrdinalInk PossibleWhite => Required.Combine(Certain,2)!;

    public BorderCoverage(BorderEmission[][] beats, uint ink, uint visible)
    {
        if (beats.Length is < 1 or > 31) throw new ArgumentException("Supply 1..31 beats.");
        uint valid = (1u << beats.Length)-1;
        if (((ink|visible) & ~valid) != 0) throw new ArgumentException("Ink masks name beats in this window.");
        var advances = new bool[beats.Length];
        for (int i = 0; i < beats.Length; i++)
        {
            var horizontal = beats[i].Where(e => e.Move.Axis == 0);
            if (horizontal.Any(e => e.Move.Sense < 0)) throw new ArgumentException("This rail requires forward H travel.");
            advances[i] = horizontal.Any(e => e.Move.Sense > 0);
        }
        int end = advances.Count(step => step);
        if (end == 0) throw new ArgumentException("No positive H span was supplied.");
        var rail = new LandmarkRun(OrderedBoundary.Bits(end+1), Enumerable.Range(0,end+1).ToArray());
        Required = OrdinalInk.Closed(rail,0,end);
        Certain = Possible = OrdinalInk.Empty(rail);
        int start = 0;
        for (int i = 0; i < beats.Length; i++)
        {
            int next = start + (advances[i] ? 1 : 0);
            if (beats[i].Any(e => e.Move.Sense != 0))
            {
                var span = OrdinalInk.Closed(rail,start,next);
                uint mask = 1u << i;
                if ((visible & ink & mask) != 0) Certain = Certain.Combine(span,14)!;
                if ((visible & mask) == 0 || (ink & mask) != 0) Possible = Possible.Combine(span,14)!;
            }
            start = next;
        }
    }

    public byte ReadSupport() => DefiniteWhite.ContainsInterval ? (byte)2 : PossibleWhite.ContainsInterval ? (byte)3 : (byte)1;
}
