namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Four observed slots in a known beat frame. Each five-bit field
/// admits blank, right, up, left, down, in that order. Blank is observed ink
/// absence; a covered observation admits all five alternatives.</summary>
public readonly record struct BorderEvidence
{
    public uint Bits { get; }
    private BorderEvidence(uint bits) => Bits = bits;
    public int Field(int slot) => (uint)slot >= 4 ? throw new ArgumentOutOfRangeException(nameof(slot)) :
        (int)((Bits >> (5*slot)) & 31);

    // Explicit correspondence: both inputs describe the same four slots in
    // the same phase and frame. A conflicting field stays empty.
    public BorderEvidence BindSameSlots(BorderEvidence other) => new(Bits & other.Bits);

    public static BorderEvidence Observe(ushort actual, int visible)
    {
        if ((uint)visible > 15) throw new ArgumentOutOfRangeException(nameof(visible));
        uint bits = 0;
        for (int i = 0; i < 4; i++)
        {
            int mark = (actual >> (4*i)) & 15;
            if (mark > 4) throw new ArgumentOutOfRangeException(nameof(actual));
            int field = (visible & (1<<i)) == 0 ? 31 : 1 << mark;
            bits |= (uint)field << (5*i);
        }
        return new(bits);
    }

    public bool Admits(ushort candidate)
    {
        for (int i = 0; i < 4; i++)
            if ((Field(i) & (1 << ((candidate >> (i*4)) & 15))) == 0) return false;
        return true;
    }

    public int CompletionCount()
    {
        int count = 1;
        for (int i = 0; i < 4; i++) count *= System.Numerics.BitOperations.PopCount((uint)Field(i));
        return count;
    }
}

public readonly record struct BorderHypothesis(ushort Word, int Phase, int QuarterTurns, bool Mirror, bool Backwards)
{
    public int PredictedMark(int beat) => (Word >> (4*(beat & 3))) & 15;
}

public static class BorderStudy
{
    // +H0, -H0, +V0, -V0. The two axes have separately scoped reaches.
    public const byte Right = 1, Left = 3, Up = 9, Down = 11;
    public static BorderScore Direct() => new([Up,Right,Down,Right], [0], 1);
    public static BorderScore Alternating() => new([Up,Down], [Right], 1,2);
    public static BorderScore Together() => new([Up,Down], [Right], 3);
    public static BorderScore OpposedSiblings(bool together) =>
        new([Up,Down], [Right,Left], together ? [(byte)3] : [(byte)1,(byte)2]);
    public static BorderScore LoopWithDrift(bool drift) =>
        new([Up,Right,Down,Left], [Right], 1,1,1, (byte)(drift ? 3 : 1));
    public static BorderScore SeparateFall() => new([Up,Right,Down | 16,Right], [0], 1);

    // Normal to the two supplied parallel lanes, the ordered stops are
    // origin, allowed spacing and farther. Only the first span follows the
    // spacing dial. Both spans are in this gauge's own local ordinal basis.
    public static PairedReach LaneSpacing(IPrimitiveReadPort? farther) =>
        new(new LandmarkRun(2,[2,0,3]), PresencePairPort.Bind(15,farther), (0,3), (0,0));

    public static BorderEmission[][] Trace(BorderScore score, int beats, bool backwards = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(beats);
        var result = new BorderEmission[beats][];
        BorderPhase phase = default;
        for (int i = 0; i < beats; i++) result[i] = score.Step(ref phase, backwards);
        return result;
    }

    // Signed sweep from the ordered sampled segments, in half-products H*V.
    // Only publish it for a projected return in a common positive reach on
    // each axis. A zero result can also arise from retracing or cancellation.
    public static long? TwiceClosedSweep(BorderEmission[][] beats, bool assumeEqual = false)
    {
        int?[] reach = new int?[2];
        long x = 0, y = 0, twiceArea = 0;
        checked
        {
            foreach (var beat in beats)
            {
                int dx = 0, dy = 0;
                foreach (var emission in beat)
                {
                    var m = emission.Move;
                    if (m.Sense == 0) continue;
                    reach[m.Axis] ??= m.Reach;
                    if (!assumeEqual && reach[m.Axis] != m.Reach) return null;
                    if (m.Axis == 0) dx += m.Sense;
                    else dy += m.Sense;
                }
                twiceArea += x*dy - y*dx;
                x += dx;
                y += dy;
            }
        }
        return x == 0 && y == 0 ? twiceArea : null;
    }

    // Deliberately narrow observer: individual cardinal strokes in four slots.
    public static ushort InkWord(BorderEmission[][] beats, int inkMask)
    {
        if (beats.Length != 4 || (uint)inkMask > 15) throw new ArgumentException("Supply four ink slots.");
        int word = 0;
        for (int i = 0; i < 4; i++)
        {
            if (beats[i].Length != 1) throw new ArgumentException("This observer requires one firing per beat.");
            var move = beats[i][0].Move;
            if ((inkMask & (1<<i)) != 0 && move.Sense != 0)
                word |= (1 + move.Axis + (move.Sense < 0 ? 2 : 0)) << (4*i);
        }
        return (ushort)word;
    }

    public static readonly IReadOnlyList<BorderHypothesis> Family = Array.AsReadOnly(Orbit(InkWord(Trace(Direct(),4),15)).ToArray());

    // Compare whole words so that the same transform governs all hidden slots.
    private static IEnumerable<BorderHypothesis> Orbit(ushort word)
    {
        for (int phase = 0; phase < 4; phase++)
            for (int turn = 0; turn < 4; turn++)
                foreach (bool mirror in new[]{false,true})
                    foreach (bool backwards in new[]{false,true})
                    {
                        int transformed = 0;
                        for (int i = 0; i < 4; i++)
                        {
                            int slot = (phase + (backwards ? -i : i)) & 3;
                            int mark = (word >> (4*slot)) & 15;
                            if (mark == 0) continue;
                            int heading = mark - 1;
                            if (mirror) heading = -heading; // reflect across H
                            heading = (heading + turn + (backwards ? 2 : 0)) & 3;
                            transformed |= (heading+1) << (4*i);
                        }
                        yield return new((ushort)transformed, phase, turn, mirror, backwards);
                    }
    }

    public static BorderHypothesis[] Predict(BorderEvidence evidence) =>
        Family.Where(h => evidence.Admits(h.Word)).ToArray();
}
