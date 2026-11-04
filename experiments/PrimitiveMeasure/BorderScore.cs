namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>In a supplied straight two-axis frame each byte is one emission.
/// Low to high: V,D,O,axis, four-bit reach reference. Reach names are local
/// to this score and axis. Repeated uses of one name share a positive length.</summary>
public readonly record struct BorderMove(byte Bits)
{
    public int Axis => (Bits >> 3) & 1;
    public int Reach => Bits >> 4;
    public int Sense => (Bits & 1) == 0 ? 0 : (((Bits >> 1) ^ (Bits >> 2)) & 1) == 0 ? 1 : -1;
    public BorderMove Backward() => new((byte)((Bits & ~7) | ThreeBitMeasure.ReverseDirection((byte)(Bits & 7))));
}

/// <summary>Counts signed uses of unmeasured positive reaches. A mixed row
/// admits every sign. The counts are multiplicities, never measured lengths.</summary>
public sealed class ReachBalance
{
    private readonly int[,] uses = new int[2,16];
    internal void Include(BorderMove move, int count = 1)
    {
        checked { uses[move.Axis,move.Reach] += move.Sense * count; }
    }
    public int Uses(int axis, int reach) => uses[axis,reach];

    public ReachBalance AssumingEqualReaches()
    {
        var view = new ReachBalance();
        for (int axis = 0; axis < 2; axis++)
            for (int r = 0; r < 16; r++) checked { view.uses[axis,0] += uses[axis,r]; }
        return view;
    }

    // Support bits in order: negative, zero, positive.
    public byte SignSupport(int axis)
    {
        bool positive = false, negative = false;
        for (int r = 0; r < 16; r++)
        {
            positive |= uses[axis,r] > 0;
            negative |= uses[axis,r] < 0;
        }
        if (positive && negative) return 7;
        if (positive) return 4;
        return negative ? (byte)1 : (byte)2;
    }
}

public readonly record struct BorderEmission(int Voice, int Phase, BorderMove Move);
public readonly record struct BorderPhase(int Clock, int First, int Second);
public sealed record BorderPeriod(int Beats, int FirstLaps, int SecondLaps, ReachBalance End);

/// <summary>Two cyclic motion streams and one cyclic clock strip. Each
/// two-bit clock field selects which stream heads emit and advance together.
/// Row order and wrap supply successors. A zero mask is a real waiting beat.</summary>
public sealed class BorderScore
{
    private readonly byte[][] voices;
    private readonly EndpointRows clock = new();
    public int ClockLength => clock.Count(1,2);
    public int Mask(int phase) => clock.Read(new(1,2,phase))[0];
    public byte[] ClockBytes() => clock.PackedBytes(1,2);
    public byte[] VoiceBytes(int voice) => (byte[])voices[voice].Clone();
    public int Length(int voice) => voices[voice].Length;
    public BorderMove Move(int voice, int phase) => new(voices[voice][phase]);

    public BorderScore(byte[] first, byte[] second, params byte[] masks)
    {
        if (first.Length is < 1 or > 16 || second.Length is < 1 or > 16 || masks.Length is < 1 or > 16)
            throw new ArgumentException("Each strip in this experiment has one to sixteen positions.");
        voices = [(byte[])first.Clone(), (byte[])second.Clone()];
        foreach (byte mask in masks) clock.Append(2,mask);
    }

    // Both roles are in this score's basis. A negative lap compares by
    // subtracting the second displacement while retaining its reach names.
    public ReachBalance ReadLaps(int first, int second)
    {
        var end = new ReachBalance();
        foreach (byte cell in voices[0]) end.Include(new(cell), first);
        foreach (byte cell in voices[1]) end.Include(new(cell), second);
        return end;
    }

    // Count the firings in one clock lap, then find the first common phase
    // return. The endpoint proof never runs a pen or inspects screen samples.
    public BorderPeriod Predict()
    {
        int[] firings = new int[2];
        for (int t = 0; t < ClockLength; t++)
        {
            int gates = Mask(t);
            firings[0] += gates & 1;
            firings[1] += gates >> 1;
        }
        static int Gcd(int a, int b)
        {
            while (b != 0) (a,b) = (b,a%b);
            return a;
        }
        int first = Length(0) / Gcd(Length(0), firings[0]);
        int second = Length(1) / Gcd(Length(1), firings[1]);
        int laps = first / Gcd(first,second) * second;
        int firstLaps = laps * firings[0] / Length(0), secondLaps = laps * firings[1] / Length(1);
        return new(laps * ClockLength, firstLaps, secondLaps, ReadLaps(firstLaps,secondLaps));
    }

    // A reverse read unwinds the clock field that led here. Reverse emission
    // order also inverts the fine, individual-firing view of a grouped beat.
    public BorderEmission[] Step(ref BorderPhase phase, bool backwards = false)
    {
        if ((uint)phase.Clock >= ClockLength || (uint)phase.First >= Length(0) || (uint)phase.Second >= Length(1))
            throw new ArgumentOutOfRangeException(nameof(phase));
        int clockPhase = backwards ? (phase.Clock + ClockLength-1) % ClockLength : phase.Clock;
        int mask = Mask(clockPhase);
        int[] heads = [phase.First, phase.Second];
        var beat = new List<BorderEmission>(2);
        for (int i = 0; i < 2; i++)
        {
            int voice = backwards ? 1-i : i;
            if ((mask & (1 << voice)) == 0) continue;
            int length = Length(voice);
            int at = backwards ? (heads[voice]+length-1) % length : heads[voice];
            var move = Move(voice, at);
            beat.Add(new(voice, at, backwards ? move.Backward() : move));
            heads[voice] = backwards ? at : (at+1) % length;
        }
        phase = new(backwards ? clockPhase : (clockPhase+1) % ClockLength, heads[0],heads[1]);
        return beat.ToArray();
    }
}
