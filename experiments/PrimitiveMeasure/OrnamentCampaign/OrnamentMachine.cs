using System.Numerics;
using System.Runtime.CompilerServices;

namespace Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;

public readonly record struct RailInput(int Width, ulong Seed, ulong Initial, ulong Protected, int TargetGap,
    byte GoalBits, int Stock, int EditBudget, int Beats, ulong Hidden = 0, int InterventionBeat = -1,
    ulong InterventionXor = 0, int Start = 0);

// These modes are interventions on the public channel by the experimental
// apparatus. They are not instructions encoded in the candidate program.
public enum WireMode : byte { Live, Cut, Frozen, Inverted, Clock, Unknown }

public readonly record struct RailResult(ulong Ink, int Stock, int Edits, int Attempts, int Blocked,
    int Unresolved, int ExternalDelta)
{
    public int Missing { get; init; }
    public int BudgetRefusals { get; init; }
    public ulong TraceHash { get; init; }
}

public readonly record struct RailFrame(int Beat, int Site, byte WireSupport, int WriterRow, byte GuardSupport,
    bool Charged, bool Moved, ulong Before, ulong After, int RemainingStock);

/// <summary>Compiled execution over a shared unit-step ring. The sensor owner
/// imports only its local rail observations. Its last gate publishes one
/// positive Boolean support. The writer imports that port, a prior sample,
/// its own resources, and its own local state. Its ordered passages transfer
/// whole material between stock and rail sites. One funded request per beat.
/// The independent reference path computes reads through BitRelation and
/// PassageLaw directly and scans sites to obtain its distance bounds.</summary>
public sealed class OrnamentMachine
{
    private readonly SensorRow[] sensor;
    private readonly WriterRow[] writer;
    private readonly bool reversed, delayed, readsSpans;

    public OrnamentMachine(OrnamentProgram program)
    {
        sensor = program.Sensor.ToArray();
        writer = program.Writer.ToArray();
        reversed = (program.Flags & 1) != 0;
        delayed = (program.Flags & 2) != 0;
        readsSpans = sensor.Any(row => row.A is >= 5 and <= 8 || row.B is >= 5 and <= 8);
    }

    public RailResult Run(RailInput input, WireMode mode = WireMode.Live)
        => Execute(input, mode, false, null);
    public RailResult RunReference(RailInput input, WireMode mode = WireMode.Live)
        => Execute(input, mode, true, null);
    public RailFrame[] Replay(RailInput input, WireMode mode = WireMode.Live)
    {
        var frames = new List<RailFrame>();
        Execute(input, mode, false, frames);
        return frames.ToArray();
    }

    private static void Validate(RailInput input, WireMode mode)
    {
        if (input.Width is < 3 or > 63 || input.Start < 0 || input.Start >= input.Width ||
            input.TargetGap < 1 || input.TargetGap > input.Width || input.GoalBits > 15 ||
            input.Stock is < 0 or > 63 || input.EditBudget is < 0 or > 127 || input.Beats < 0 ||
            input.InterventionBeat < -1 || input.InterventionBeat >= input.Beats ||
            (input.InterventionBeat == -1 && input.InterventionXor != 0) || (byte)mode > (byte)WireMode.Unknown)
            throw new ArgumentOutOfRangeException(nameof(input));
        ulong occupied = input.Seed | input.Initial | input.Protected | input.Hidden | input.InterventionXor;
        if ((occupied >> input.Width) != 0) throw new ArgumentException("Site outside the ring.");
    }

    private RailResult Execute(RailInput input, WireMode mode, bool reference, List<RailFrame>? frames)
    {
        Validate(input, mode);
        ulong ink = input.Initial;
        var stock = Prefix(input.Stock);
        var stockCapacity = Prefix(input.Width + input.Stock);
        var permits = Prefix(input.EditBudget);
        Span<byte> observations = stackalloc byte[20];
        Span<byte> controls = stackalloc byte[12];
        observations[0] = controls[0] = 1;
        observations[1] = controls[1] = 2;
        for (var i = 0; i < 4; i++) observations[12 + i] = Known((input.GoalBits & (1 << i)) != 0);
        byte previousWire = 1, previousExport = 1, previousCommit = 1;
        int site = input.Start, edits = 0, attempts = 0, blocked = 0, uncertain = 0, missing = 0, denied = 0;
        int externalDelta = 0;
        ulong history = 14695981039346656037UL;

        for (var beat = 0; beat < input.Beats; beat++)
        {
            if (beat == input.InterventionBeat)
            {
                externalDelta = BitOperations.PopCount(ink ^ input.InterventionXor) - BitOperations.PopCount(ink);
                ink ^= input.InterventionXor;
            }
            int left = site == 0 ? input.Width - 1 : site - 1;
            int right = site == input.Width - 1 ? 0 : site + 1;
            int previous = reversed ? right : left, next = reversed ? left : right;
            ulong publication = mode == WireMode.Frozen ? input.Initial : ink;
            ReadSensorPorts(observations, input, publication, site, previous, next, reference);
            for (var i = 0; i < sensor.Length; i++)
            {
                var row = sensor[i];
                observations[16 + i] = PositiveLaws.Eval(row.Truth, observations[row.A], observations[row.B],
                    row.A == row.B, reference);
            }
            byte exported = observations[15 + sensor.Length];
            byte sample = delayed ? previousExport : exported;
            byte phase = Known((beat & 1) != 0);
            byte wire = mode switch
            {
                WireMode.Cut => 0, WireMode.Unknown => 3, WireMode.Clock => phase,
                WireMode.Inverted => (byte)((sample >> 1) | ((sample & 1) << 1)), _ => sample
            };
            controls[2] = wire;
            controls[3] = previousWire;
            controls[4] = previousCommit;
            controls[5] = phase;
            controls[6] = Inspect(ink, input.Hidden, site);
            controls[7] = Known(stock != 0);
            previousWire = wire;
            previousExport = exported;

            var before = ink;
            bool moved = false, charged = false;
            int selected = -1;
            byte support = 1;
            for (var i = 0; i < writer.Length; i++)
            {
                var row = writer[i];
                var read = PositiveLaws.Eval(row.Truth, controls[row.A], controls[row.B], row.A == row.B, reference);
                controls[8 + i] = read;
                if (row.From == row.To || read == 1) continue;
                selected = i;
                support = read;
                // A higher-priority uncertain passage could consume the same
                // opportunity. Waiting preserves the unchosen alternative.
                if (read == 0) { missing++; break; }
                if (read == 3) { uncertain++; break; }
                if (permits == 0) { denied++; break; }
                permits >>= 1;
                attempts++;
                charged = true;
                int from = Site(row.From, site, previous, next);
                int to = Site(row.To, site, previous, next);
                if (Transfer(from, to, input.Protected, ref ink, ref stock, stockCapacity, reference))
                {
                    moved = true;
                    edits++;
                }
                else blocked++;
                break;
            }
            previousCommit = Known(moved);
            uint witness = (uint)site | ((uint)wire << 6) | ((uint)(selected + 1) << 8) |
                ((uint)support << 11) | (charged ? 1U << 13 : 0) | (moved ? 1U << 14 : 0);
            history = unchecked(((history ^ witness) * 1099511628211UL ^ ink) * 1099511628211UL);
            if (frames != null)
                frames.Add(new(beat, site, wire, selected, support, charged, moved, before, ink, Count(stock)));
            site = next;
        }
        return new(ink, Count(stock), edits, attempts, blocked, uncertain, externalDelta)
            { Missing = missing, BudgetRefusals = denied, TraceHash = history };
    }

    private static int Site(byte role, int site, int previous, int next) =>
        role switch { 0 => -1, 1 => site, 2 => previous, _ => next };

    private static bool Transfer(int source, int target, ulong protectedSites, ref ulong ink,
        ref UInt128 stock, UInt128 capacity, bool reference)
    {
        ulong from = source < 0 ? 0 : 1UL << source;
        ulong to = target < 0 ? 0 : 1UL << target;
        bool present = source < 0 ? stock != 0 : (ink & from) != 0;
        bool occupied = target < 0 ? stock == capacity : (ink & to) != 0;
        bool allowed = (protectedSites & (from | to)) == 0;
        byte before = PassageLaw.State(present, allowed, occupied);
        if (!PositiveLaws.Moves(before, reference)) return false;
        if (source < 0) stock >>= 1;
        else ink &= ~from;
        if (target < 0) stock = (stock << 1) | 1;
        else ink |= to;
        return true;
    }

    private void ReadSensorPorts(Span<byte> ports, RailInput input, ulong publication, int site,
        int previous, int next, bool reference)
    {
        ports[2] = Inspect(publication, input.Hidden, site);
        ports[3] = Inspect(publication, input.Hidden, previous);
        ports[4] = Inspect(publication, input.Hidden, next);
        ports[9] = Inspect(publication, input.Hidden, input.Width - site - 1);
        ports[10] = Inspect(input.Seed, input.Hidden, site);
        ports[11] = Known((input.Protected & (1UL << site)) != 0);
        if (!readsSpans) return;

        // The difference is on an explicit shared, unmeasured unit-step rail.
        // Concealed participation provides intervals of possible spans. The
        // interval comparison deliberately need not reconstruct correlated
        // hidden completions. No private concealed occupancy sets the answer.
        ulong certain = publication & ~input.Hidden;
        ulong possible = publication | input.Hidden;
        var longest = Distances(certain, site, input.Width, reference);
        var shortest = input.Hidden == 0 ? longest : Distances(possible, site, input.Width, reference);
        var (loBack, loAhead) = reversed ? (shortest.Right, shortest.Left) : shortest;
        var (hiBack, hiAhead) = reversed ? (longest.Right, longest.Left) : longest;
        ports[5] = AtLeast(loBack, hiBack, input.TargetGap);
        ports[6] = AtLeast(loAhead, hiAhead, input.TargetGap);
        ports[7] = possible == 0 ? (byte)0 : Greater(loBack, hiBack, loAhead, hiAhead);
        ports[8] = possible == 0 ? (byte)0 : Greater(loAhead, hiAhead, loBack, hiBack);
    }

    private static (int Left, int Right) Distances(ulong bits, int site, int width, bool reference)
    {
        if (bits == 0) return (width + 1, width + 1);
        if (reference)
        {
            int left = site, right = site, dl = 0, dr = 0;
            do { left = left == 0 ? width - 1 : left - 1; dl++; } while ((bits & (1UL << left)) == 0);
            do { right = right == width - 1 ? 0 : right + 1; dr++; } while ((bits & (1UL << right)) == 0);
            return (dl, dr);
        }
        ulong below = bits & ((1UL << site) - 1);
        ulong above = bits >> (site + 1);
        int l = below == 0 ? width + site - BitOperations.Log2(bits) : site - BitOperations.Log2(below);
        int r = above == 0 ? width - site + BitOperations.TrailingZeroCount(bits) : 1 + BitOperations.TrailingZeroCount(above);
        return (l, r);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Inspect(ulong ink, ulong hidden, int site)
    {
        ulong bit = 1UL << site;
        return (hidden & bit) != 0 ? (byte)3 : Known((ink & bit) != 0);
    }
    private static byte AtLeast(int min, int max, int mark) => min >= mark ? (byte)2 : max < mark ? (byte)1 : (byte)3;
    private static byte Greater(int min, int max, int otherMin, int otherMax) =>
        min > otherMax ? (byte)2 : max <= otherMin ? (byte)1 : (byte)3;
    private static byte Known(bool value) => value ? (byte)2 : (byte)1;
    private static UInt128 Prefix(int count) => ((UInt128)1 << count) - 1;
    private static int Count(UInt128 bits) => BitOperations.PopCount((ulong)bits) + BitOperations.PopCount((ulong)(bits >> 64));
}

internal static class PositiveLaws
{
    private static readonly BitRelation[] Laws = Enumerable.Range(0, 16).Select(mask =>
        BitRelation.FromTuples(3, from a in Enumerable.Range(0, 2) from b in Enumerable.Range(0, 2)
            select new byte[] { (byte)a, (byte)b, (byte)((mask >> (a + 2 * b)) & 1) })).ToArray();
    private static readonly PassageLaw Move = PassageLaw.Controlled();
    private static readonly bool[] MovesByState = Enumerable.Range(0, 8).Select(i => DirectMove((byte)i)).ToArray();
    private static readonly byte[] Supports = Compile();

    private static byte[] Compile()
    {
        var result = new byte[512];
        for (byte law = 0; law < 16; law++)
        for (byte left = 0; left < 4; left++)
        for (byte right = 0; right < 4; right++)
        for (int same = 0; same < 2; same++)
            result[32 * law + 16 * same + 4 * left + right] = Direct(law, left, right, same != 0);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte Eval(byte law, byte left, byte right, bool same, bool reference) => reference
        ? Direct(law, left, right, same) : Supports[(law << 5) | (same ? 16 : 0) | (left << 2) | right];

    private static byte Direct(byte law, byte left, byte right, bool same) => Laws[law].Predict(
        [left, right], same ? [0, 0] : [0, 1]);
    private static bool DirectMove(byte before) => Move.Predict((byte)(1 << before)) == (1 << (before ^ 5));
    public static bool Moves(byte state, bool reference) => reference ? DirectMove(state) : MovesByState[state];
}
