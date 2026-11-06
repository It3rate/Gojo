namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>The same order read over named landmarks or witnessed event sites.
/// The three low support bits admit first-before-second, second-before-first,
/// and one shared event respectively. Unplaced sites admit all three.</summary>
public sealed class RaceOrderRead(LandmarkRun rail, int first, int second) : IPrimitiveReadPort
{
    public byte ReadSupport() => rail.Compare(first, second) switch
    {
        LandmarkRead.Before => 1,
        LandmarkRead.After => 2,
        LandmarkRead.SameSite => 4,
        LandmarkRead.Unresolved => 7,
        _ => 0
    };
}

// These are actual exposed ports, including when two connections were
// constructed separately to the same endpoint. Their bits need not differ.
public sealed record RaceFrame(PortConnection Contest, PortConnection First, PortConnection Second);
public sealed record RaceOrderOffer(RaceFrame Frame, IPrimitiveReadPort Order);

/// <summary>One level up: only the two public ranks cross the boundary.
/// Binding selects a correspondence through references to the common event
/// and entrants. The packed law then evaluates outright victory; a shared
/// finish is a definite false for that question.</summary>
public sealed class RaceDecision : IPrimitiveReadPort
{
    private static readonly BitRelation Direct = Compile(false), Reversed = Compile(true);
    private static readonly BitRelation Outside = BitRelation.FromTuples(3, []);
    private readonly CorrespondenceProbe probe;
    public int HostReads { get; private set; }
    public bool Compatible { get; }

    public RaceDecision(RaceOrderOffer favorite, RaceOrderOffer finish)
    {
        ArgumentNullException.ThrowIfNull(favorite);
        ArgumentNullException.ThrowIfNull(finish);
        var a = favorite.Frame;
        var b = finish.Frame;
        bool common = a.Contest.SamePortAs(b.Contest) && !a.First.SamePortAs(a.Second);
        bool direct = common && a.First.SamePortAs(b.First) && a.Second.SamePortAs(b.Second);
        bool reversed = common && a.First.SamePortAs(b.Second) && a.Second.SamePortAs(b.First);
        Compatible = direct || reversed;
        var prediction = new Meter(this, favorite.Order);
        bool samePort = ReferenceEquals(favorite.Order, finish.Order) ||
            (favorite.Order is PortConnection first && finish.Order is PortConnection second && first.SamePortAs(second));
        probe = new(direct ? Direct : reversed ? Reversed : Outside, prediction,
            samePort ? prediction : new Meter(this, finish.Order));
    }

    public byte ReadSupport() => Compatible ? probe.ReadSupport() : (byte)0;
    public byte[] LawBytes() => probe.LawBytes();
    public byte[] RowBytes() => probe.RowBytes();

    private sealed class Meter(RaceDecision owner, IPrimitiveReadPort source) : IPrimitiveReadPort
    {
        public byte ReadSupport()
        {
            owner.HostReads++;
            return source.ReadSupport();
        }
    }

    private static BitRelation Compile(bool reversed)
    {
        var tuples = new List<byte[]>();
        // Only a strict, single favorite is defined in this two-entrant fixture.
        for (byte forecast = 0; forecast < 2; forecast++)
        for (byte actual = 0; actual < 3; actual++)
            tuples.Add([forecast, actual, (byte)(actual == (reversed ? 1 - forecast : forecast) ? 1 : 0)]);
        return BitRelation.FromTuples(3, tuples);
    }
}
