using System.Numerics;

namespace Gojo.EncodingC;

/// <summary>Hardware-accelerated SIMD (<see cref="Vector{T}"/>: ARM NEON 128-bit,
/// x86 AVX2 256-bit / AVX-512 512-bit, scalar tail) bit-sliced plane stepping.
/// Strictly derived from canonical <see cref="TransitionLaw"/> tables and
/// bit-for-bit identical to <see cref="TransitionLaw.Step"/>.</summary>
public static class CompiledSimd
{
    public static bool IsHardwareAccelerated => Vector.IsHardwareAccelerated;
    public static int LanesPerVector => Vector<ulong>.Count * 64;

    /// <summary>Vectorized bit-sliced step of a functional transition law across
    /// aligned <see cref="Plane"/> word buffers.</summary>
    public static TransitionStep StepTransition(TransitionLaw law, Plane[][] codes, Plane lanes)
    {
        ArgumentNullException.ThrowIfNull(law);
        ArgumentNullException.ThrowIfNull(codes);
        ArgumentNullException.ThrowIfNull(lanes);
        var roleCount = law.Roles.Count;
        if (codes.Length != roleCount) throw new ArgumentException("Supply code planes for every role.", nameof(codes));

        var wordCount = lanes.WordCount;
        var vecWidth = Vector<ulong>.Count;
        var next = new Plane[roleCount][];
        for (var r = 0; r < roleCount; r++)
        {
            if (codes[r].Length != law.Roles[r].Bits) throw new ArgumentException($"Role {r} needs {law.Roles[r].Bits} code planes.", nameof(codes));
            next[r] = new Plane[law.Roles[r].Bits];
            for (var b = 0; b < next[r].Length; b++) next[r][b] = new Plane(lanes.Length);
        }

        var covered = new Plane(lanes.Length);
        var readable = new Plane(lanes.Length);
        var ambiguous = new Plane(lanes.Length);
        var lanesWords = lanes.RawWords;
        var coveredWords = covered.RawWords;
        var readableWords = readable.RawWords;
        var ambiguousWords = ambiguous.RawWords;

        var patterns = new ulong[roleCount];
        var afterPatterns = new ulong[roleCount];

        for (var before = 0; before < law.StateCount; before++)
        {
            var tuple = law.Tuple(before);
            for (var r = 0; r < roleCount; r++) patterns[r] = law.Roles[r].Encode(tuple[r]);
            var candidates = law.Afters(before);
            var single = candidates.Count == 1;
            var multi = candidates.Count > 1;
            if (single)
            {
                var after = law.Tuple(candidates[0]);
                for (var r = 0; r < roleCount; r++) afterPatterns[r] = law.Roles[r].Encode(after[r]);
            }

            var w = 0;
            for (; w + vecWidth <= wordCount; w += vecWidth)
            {
                var m = new Vector<ulong>(lanesWords, w);
                for (var r = 0; r < roleCount && !Vector.EqualsAll(m, Vector<ulong>.Zero); r++)
                    for (var b = 0; b < codes[r].Length; b++)
                    {
                        var cv = new Vector<ulong>(codes[r][b].RawWords, w);
                        m = ((patterns[r] >> b) & 1) != 0 ? (m & cv) : Vector.AndNot(m, cv);
                    }
                if (Vector.EqualsAll(m, Vector<ulong>.Zero)) continue;

                (new Vector<ulong>(readableWords, w) | m).CopyTo(readableWords, w);
                if (!single)
                {
                    if (multi) (new Vector<ulong>(ambiguousWords, w) | m).CopyTo(ambiguousWords, w);
                    continue;
                }
                (new Vector<ulong>(coveredWords, w) | m).CopyTo(coveredWords, w);
                for (var r = 0; r < roleCount; r++)
                    for (var b = 0; b < next[r].Length; b++)
                        if (((afterPatterns[r] >> b) & 1) != 0)
                        {
                            var dst = next[r][b].RawWords;
                            (new Vector<ulong>(dst, w) | m).CopyTo(dst, w);
                        }
            }

            for (; w < wordCount; w++)
            {
                var m = lanesWords[w];
                for (var r = 0; r < roleCount && m != 0; r++)
                    for (var b = 0; b < codes[r].Length; b++)
                        m &= ((patterns[r] >> b) & 1) != 0 ? codes[r][b].RawWords[w] : ~codes[r][b].RawWords[w];
                if (m == 0) continue;
                readableWords[w] |= m;
                if (!single)
                {
                    if (multi) ambiguousWords[w] |= m;
                    continue;
                }
                coveredWords[w] |= m;
                for (var r = 0; r < roleCount; r++)
                    for (var b = 0; b < next[r].Length; b++)
                        if (((afterPatterns[r] >> b) & 1) != 0) next[r][b].RawWords[w] |= m;
            }
        }

        var unchanged = lanes.AndNot(covered);
        var unchangedWords = unchanged.RawWords;
        for (var r = 0; r < roleCount; r++)
            for (var b = 0; b < next[r].Length; b++)
            {
                var dst = next[r][b].RawWords;
                var src = codes[r][b].RawWords;
                var w = 0;
                for (; w + vecWidth <= wordCount; w += vecWidth)
                    (new Vector<ulong>(dst, w) | (new Vector<ulong>(src, w) & new Vector<ulong>(unchangedWords, w))).CopyTo(dst, w);
                for (; w < wordCount; w++)
                    dst[w] |= src[w] & unchangedWords[w];
                next[r][b].TrimTail();
            }

        covered.TrimTail();
        readable.TrimTail();
        ambiguous.TrimTail();
        return new TransitionStep(next, lanes.AndNot(readable), readable.AndNot(covered).AndNot(ambiguous), ambiguous);
    }
}

/// <summary>Integer-only Regime 2 finite-read contract for approximate
/// waveform lowering (zero floats in canonical EncodingC).</summary>
public sealed record FiniteReadContract(int MaxAbs, int Shift, int ToleranceTicks);

/// <summary>Outcome of one Regime 2 declared approximate waveform read.</summary>
public sealed record ApproximateWaveformRead(bool Accepted, bool Approximate, int MaxErrorTicks, int X, int V);

/// <summary>Outcome of one derived compiled step: reports whether the fast
/// path ran, how many lanes used the native path vs deopted to canonical, and
/// whether staleness forced full canonical fallback.</summary>
public sealed record CompiledStepReport(bool UsedFastPath, bool Stale, int FastLanes, int DeoptLanes);

/// <summary>Derived, epoch-guarded compiled execution plan for an
/// <see cref="Engine"/> (GojoOperations-2026-11.md §6, generalizing
/// <c>FiniteLawPlan.IsCurrent</c>). Never saved by <see cref="Transport"/>;
/// recognizes packet conversions, bounded lossless multipliers, radix-B ripple
/// adders, kick-drift oscillators, and SIMD bit-sliced laws strictly from table
/// bits, accounting, and topology — never from names or opcode enums.</summary>
public sealed class CompiledPlan
{
    private readonly Engine engine;
    private readonly bool snapshotTogether;
    private readonly WiringSnapshot[] wiringSnapshots;
    private readonly CompiledWiring[] compiled;
    private readonly RecognizedOscillator? oscillator;
    private readonly RecognizedLadderAdder? ladderAdder;

    private CompiledPlan(
        Engine engine,
        WiringSnapshot[] snapshots,
        CompiledWiring[] compiled,
        RecognizedOscillator? oscillator,
        RecognizedLadderAdder? ladderAdder)
    {
        this.engine = engine;
        snapshotTogether = engine.Together;
        wiringSnapshots = snapshots;
        this.compiled = compiled;
        this.oscillator = oscillator;
        this.ladderAdder = ladderAdder;
    }

    public Engine Source => engine;

    /// <summary>True iff a packet-exact Convert(take, give) law was recognized
    /// strictly from its table bits and PreservedSum accounting.</summary>
    public bool RecognizedPacketConvert => compiled.Any(c => c.Kind == KernelKind.PacketConvert);

    /// <summary>True iff a functional lossless bounded multiplier with explicit
    /// T+ overflow was recognized strictly from its table bits.</summary>
    public bool RecognizedBoundedMultiply => compiled.Any(c => c.Kind == KernelKind.BoundedMultiply);

    /// <summary>True iff a sealed multi-rung radix-B ripple adder was recognized
    /// strictly from verified DigitAddStep tables and carry wiring.</summary>
    public bool RecognizedRadixAdder => ladderAdder is not null;

    /// <summary>True iff a declared-order symplectic kick-drift oscillator was
    /// recognized strictly from its two signed-shear tables.</summary>
    public bool RecognizedKickDriftOscillator => oscillator is not null;

    /// <summary>False after any structural edit to the engine: adding a wiring,
    /// changing Together, adding/retiring rows, flipping Attention, turning on
    /// Record(), or unplugging/replugging a Socket.</summary>
    public bool IsCurrent
    {
        get
        {
            if (engine.Together != snapshotTogether || engine.Wirings.Count != wiringSnapshots.Length) return false;
            for (var i = 0; i < wiringSnapshots.Length; i++)
                if (!wiringSnapshots[i].Matches(engine.Wirings[i])) return false;
            return true;
        }
    }

    /// <summary>Builds a derived compiled plan from <paramref name="engine"/>
    /// strictly by inspecting its laws, accounting, and bindings.</summary>
    public static CompiledPlan Compile(Engine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        var snapshots = new WiringSnapshot[engine.Wirings.Count];
        var compiled = new CompiledWiring[engine.Wirings.Count];
        for (var i = 0; i < engine.Wirings.Count; i++)
        {
            var w = engine.Wirings[i];
            snapshots[i] = WiringSnapshot.Capture(w);
            compiled[i] = CompileWiring(w);
        }

        var osc = TryRecognizeKickDrift(engine);
        var adder = TryRecognizeLadderAdder(engine);
        return new CompiledPlan(engine, snapshots, compiled, osc, adder);
    }

    /// <summary>Mechanically verifies whether <paramref name="law"/> is an
    /// exact packet conversion <c>Convert(take, give)</c> strictly from its
    /// role shapes, <see cref="PreservedSum"/> weights, and table bits (works
    /// identically on restored or learned laws).</summary>
    public static bool TryRecognizePacketConvert(TransitionLaw law, out int take, out int give)
    {
        take = give = 0;
        if (law.Roles.Count != 2 || !law.Roles[0].IsUnary || !law.Roles[1].IsUnary || !law.IsFunctional) return false;
        var sum = law.Accounting.Invariants.OfType<PreservedSum>().FirstOrDefault(s => s.Conserves && s.Roles.Count == 2 && s.Roles[0] == 0 && s.Roles[1] == 1);
        if (sum is null) return false;
        var candGive = sum.Weights[0];
        var candTake = sum.Weights[1];
        if (candTake < 1 || candGive < 1) return false;

        var srcCap = law.Roles[0].Size - 1;
        var dstCap = law.Roles[1].Size - 1;
        for (var s = 0; s <= srcCap; s++)
            for (var d = 0; d <= dstCap; d++)
            {
                var after = law.Tuple(law.Afters(law.TupleIndex([s, d]))[0]);
                var canFire = s >= candTake && d + candGive <= dstCap;
                var expS = canFire ? s - candTake : s;
                var expD = canFire ? d + candGive : d;
                if (after[0] != expS || after[1] != expD) return false;
            }
        take = candTake;
        give = candGive;
        return true;
    }

    /// <summary>Mechanically verifies whether <paramref name="law"/> is a
    /// functional lossless bounded multiplier <c>(a [Unchanged], b [Unchanged],
    /// value, overflow/T+)</c> where <c>prodCap + overflowCap ≥ maxA · maxB</c>
    /// strictly from every cell of its table bits.</summary>
    public static bool TryRecognizeBoundedMultiply(TransitionLaw law, out int prodCap, out int overflowCap)
    {
        prodCap = overflowCap = 0;
        if (law.Roles.Count != 4 || !law.IsFunctional) return false;
        if (law.Writes(0) || law.Writes(1)) return false;
        var aSize = law.Roles[0].Size;
        var bSize = law.Roles[1].Size;
        var vSize = law.Roles[2].Size;
        var oSize = law.Roles[3].Size;
        if (aSize < 2 || bSize < 2 || vSize < 2 || oSize < 2) return false;
        var candProdCap = vSize - 1;
        var candOvfCap = oSize - 1;
        if ((ulong)candProdCap + (ulong)candOvfCap < (ulong)(aSize - 1) * (ulong)(bSize - 1)) return false;
        for (var a = 0; a < aSize; a++)
            for (var b = 0; b < bSize; b++)
            {
                var prod = (ulong)a * (ulong)b;
                var expVal = prod <= (ulong)candProdCap ? (int)prod : candProdCap;
                var expOvf = prod > (ulong)candProdCap ? (int)(prod - (ulong)candProdCap) : 0;
                for (var ov = 0; ov < vSize; ov++)
                    for (var oo = 0; oo < oSize; oo++)
                    {
                        var after = law.Tuple(law.Afters(law.TupleIndex([a, b, ov, oo]))[0]);
                        if (after[0] != a || after[1] != b || after[2] != expVal || after[3] != expOvf) return false;
                    }
            }
        prodCap = candProdCap;
        overflowCap = candOvfCap;
        return true;
    }

    /// <summary>Mechanically verifies whether <paramref name="law"/> is a
    /// justified functional radix-B ripple adder (a, b, cin [Unchanged] ->
    /// sum, cout) strictly from every cell of its table bits.</summary>
    public static bool TryRecognizeDigitAddStep(TransitionLaw law, out int radix)
    {
        radix = 0;
        if (law.Roles.Count != 5 || !law.IsFunctional) return false;
        var digit = law.Roles[0];
        var b = digit.Size;
        if (b < 2 || law.Roles[1] != digit || law.Roles[2] != Shape.Bit || law.Roles[3] != digit || law.Roles[4] != Shape.Bit)
            return false;
        if (law.Writes(0) || law.Writes(1) || law.Writes(2)) return false;
        for (var x = 0; x < b; x++)
            for (var y = 0; y < b; y++)
                for (var c = 0; c <= 1; c++)
                {
                    var total = x + y + c;
                    var expSum = total % b;
                    var expCout = total / b;
                    for (var os = 0; os < b; os++)
                        for (var oc = 0; oc <= 1; oc++)
                        {
                            var after = law.Tuple(law.Afters(law.TupleIndex([x, y, c, os, oc]))[0]);
                            if (after[0] != x || after[1] != y || after[2] != c || after[3] != expSum || after[4] != expCout) return false;
                        }
                }
        radix = b;
        return true;
    }

    /// <summary>Mechanically verifies whether <paramref name="law"/> is a signed
    /// shear <c>dst ← dst + sign · coarse(src, shift)</c> over offset-binary
    /// [-maxAbs, +maxAbs].</summary>
    public static bool TryRecognizeSignedShear(TransitionLaw law, out int maxAbs, out int shift, out int sign, out bool clamps)
    {
        maxAbs = shift = sign = 0;
        clamps = false;
        if (law.Roles.Count != 2 || law.Roles[0].IsUnary || law.Roles[0] != law.Roles[1]) return false;
        var size = law.Roles[0].Size;
        if (size < 3 || (size & 1) == 0 || law.Writes(0)) return false;
        var candMax = (size - 1) / 2;
        var candClamps = law.IsFunctional;

        for (var candShift = 0; candShift <= 6; candShift++)
            for (var candSign = -1; candSign <= 1; candSign++)
            {
                var ok = true;
                for (var s = 0; s < size && ok; s++)
                    for (var d = 0; d < size && ok; d++)
                    {
                        var src = s - candMax;
                        var dst = d - candMax;
                        var next = dst + candSign * Dynamics.Coarse(src, candShift);
                        var afters = law.Afters(law.TupleIndex([s, d]));
                        if (next < -candMax || next > candMax)
                        {
                            if (!candClamps) { if (afters.Count != 0) ok = false; }
                            else
                            {
                                var clamped = next < -candMax ? -candMax : candMax;
                                if (afters.Count != 1) ok = false;
                                else { var a = law.Tuple(afters[0]); if (a[0] != s || a[1] != clamped + candMax) ok = false; }
                            }
                        }
                        else
                        {
                            if (afters.Count != 1) ok = false;
                            else { var a = law.Tuple(afters[0]); if (a[0] != s || a[1] != next + candMax) ok = false; }
                        }
                    }
                if (ok)
                {
                    maxAbs = candMax;
                    shift = candShift;
                    sign = candSign;
                    clamps = candClamps;
                    return true;
                }
            }
        return false;
    }

    /// <summary>Executes one step: runs verified fast paths on guarded,
    /// non-aliased, non-contending lanes and falls back cleanly to canonical
    /// execution for stale plans, open/broken/aliased lanes, or contested
    /// shared-capacity groups.</summary>
    public CompiledStepReport RunStep() => RunStepCore(out _);

    private CompiledStepReport RunStepCore(out bool quiet)
    {
        if (!IsCurrent)
        {
            var step = engine.Advance();
            quiet = step.Steps.All(s => s.Quiet);
            return new CompiledStepReport(UsedFastPath: false, Stale: true, FastLanes: 0, DeoptLanes: TotalLiveRows());
        }

        if (engine.Together)
        {
            var step = engine.StepTogether();
            quiet = step.Steps.All(s => s.Quiet);
            return new CompiledStepReport(UsedFastPath: false, Stale: false, FastLanes: 0, DeoptLanes: TotalLiveRows());
        }

        if (oscillator is { } osc && osc.CanRunFast())
        {
            var (fast, deopt, changed) = osc.StepFast();
            if (deopt == 0)
            {
                engine.AdvanceTick();
                quiet = !changed;
                return new CompiledStepReport(UsedFastPath: true, Stale: false, FastLanes: fast, DeoptLanes: 0);
            }
        }

        if (ladderAdder is { } adder && adder.CanRunFast())
        {
            var (fast, deopt, changed) = adder.StepFast();
            if (deopt == 0)
            {
                engine.AdvanceTick();
                quiet = !changed;
                return new CompiledStepReport(UsedFastPath: true, Stale: false, FastLanes: fast, DeoptLanes: 0);
            }
        }

        var fastTotal = 0;
        var deoptTotal = 0;
        var anyFast = false;
        var allQuiet = true;
        for (var i = 0; i < compiled.Length; i++)
        {
            var cw = compiled[i];
            if (!cw.CanAccelerate)
            {
                var step = cw.Wiring.Apply();
                if (!step.Quiet) allQuiet = false;
                deoptTotal += cw.Wiring.Live.Count;
                continue;
            }
            var (fast, deopt, wQuiet) = cw.StepGuarded();
            if (!wQuiet) allQuiet = false;
            fastTotal += fast;
            deoptTotal += deopt;
            if (fast > 0) anyFast = true;
        }
        engine.AdvanceTick();
        quiet = allQuiet;
        return new CompiledStepReport(UsedFastPath: anyFast, Stale: false, FastLanes: fastTotal, DeoptLanes: deoptTotal);
    }

    /// <summary>Packet-exact bulk settle fast path for a single uncontested
    /// packet-conversion wiring: transfers exact whole packets
    /// <c>p = min(src/take, room/give, limit)</c> (NEVER bulk
    /// <c>(src*give)/take</c>). Generic settling checks per-wiring quietness
    /// matching <see cref="Engine.Settle(int)"/> even when two ordered wirings
    /// undo each other's bit changes within one step.</summary>
    public CompiledStepReport RunSettle(int limit)
    {
        if (!IsCurrent)
        {
            engine.Settle(limit);
            return new CompiledStepReport(UsedFastPath: false, Stale: true, FastLanes: 0, DeoptLanes: TotalLiveRows());
        }

        if (limit <= 0)
            return new CompiledStepReport(UsedFastPath: false, Stale: false, FastLanes: 0, DeoptLanes: 0);

        if (!engine.Together && compiled.Length == 1 && compiled[0].Kind == KernelKind.PacketConvert && compiled[0].CanAccelerate)
        {
            var (fast, deopt, ticks) = compiled[0].SettlePacketConvertExact(limit);
            if (deopt == 0)
            {
                engine.AdvanceTick(ticks);
                return new CompiledStepReport(UsedFastPath: true, Stale: false, FastLanes: fast, DeoptLanes: 0);
            }
        }

        var totalFast = 0;
        var totalDeopt = 0;
        var anyFast = false;
        for (var i = 0; i < limit; i++)
        {
            var rep = RunStepCore(out var quiet);
            totalFast += rep.FastLanes;
            totalDeopt += rep.DeoptLanes;
            if (rep.UsedFastPath) anyFast = true;
            if (quiet) break;
        }
        return new CompiledStepReport(anyFast, Stale: false, totalFast, totalDeopt);
    }

    /// <summary>Regime 2 declared approximate waveform read for a recognized
    /// kick-drift oscillator. Refused unless an explicit integer-tick
    /// <see cref="FiniteReadContract"/> is provided and the external oracle
    /// stays within <see cref="FiniteReadContract.ToleranceTicks"/> against
    /// the canonical discrete oscillator over the requested horizon.</summary>
    public ApproximateWaveformRead TryReadApproximateOscillator(
        int ticks,
        FiniteReadContract? contract,
        Func<int, (int X, int V)> externalOracle)
    {
        ArgumentNullException.ThrowIfNull(externalOracle);
        if (!IsCurrent || oscillator is null || contract is null)
            return new ApproximateWaveformRead(Accepted: false, Approximate: false, MaxErrorTicks: -1, X: 0, V: 0);
        if (contract.MaxAbs != oscillator.MaxAbs || contract.Shift != oscillator.Shift || contract.ToleranceTicks < 0)
            return new ApproximateWaveformRead(Accepted: false, Approximate: false, MaxErrorTicks: -1, X: 0, V: 0);

        var (x, v) = oscillator.ReadLane0();
        var maxErr = 0;
        var lastOracle = (X: x, V: v);
        for (var t = 0; t <= ticks; t++)
        {
            var approx = externalOracle(t);
            lastOracle = approx;
            var ex = approx.X - x; if (ex < 0) ex = -ex;
            var ev = approx.V - v; if (ev < 0) ev = -ev;
            var err = ex > ev ? ex : ev;
            if (err > maxErr) maxErr = err;
            if (t < ticks)
            {
                v -= oscillator.FeedbackSign * Dynamics.Coarse(x, oscillator.Shift);
                x += Dynamics.Coarse(v, oscillator.Shift);
            }
        }
        var accepted = maxErr <= contract.ToleranceTicks;
        return new ApproximateWaveformRead(accepted, Approximate: accepted, maxErr, lastOracle.X, lastOracle.V);
    }

    private int TotalLiveRows() => engine.Wirings.Sum(w => w.Live.Count);

    private static bool LiveCovers(Plane live, Plane runnable)
    {
        if (live.Length < runnable.Length) return false;
        var lw = live.RawWords;
        var rw = runnable.RawWords;
        for (var w = 0; w < rw.Length; w++)
            if ((rw[w] & ~lw[w]) != 0) return false;
        return true;
    }

    /// <summary>Exact 64-lane bitwise check: returns a bitmask of lanes where
    /// the unsigned binary value across <paramref name="planes"/> is
    /// &gt;= <paramref name="threshold"/>.</summary>
    private static ulong WordBinaryGreaterOrEqual(ulong[]?[] planes, bool[] constVal, int wordIdx, int threshold)
    {
        ulong gt = 0, lt = 0;
        for (var k = planes.Length - 1; k >= 0; k--)
        {
            var bit = planes[k] is { } arr ? arr[wordIdx] : (constVal[k] ? ulong.MaxValue : 0UL);
            var undecided = ~(gt | lt);
            if (((threshold >> k) & 1) != 0)
                lt |= undecided & ~bit;
            else
                gt |= undecided & bit;
        }
        return ~lt;
    }

    private enum KernelKind { Canonical, SimdBitSliced, PacketConvert, BoundedMultiply }

    private readonly record struct ChangingState(ulong BeforeMask, int[] FlippedWriteIndices);

    private sealed class CompiledWiring
    {
        private readonly Plane staticRunnable;
        private readonly int staticRunnableCount;
        private readonly bool staticTopologyClean;
        private readonly bool identityMapped;
        private readonly Store[] distinctStores;
        private readonly int[] roleOffsets;
        private readonly int totalRoleBits;
        private readonly int[] writeFlatBits;
        private readonly (Store Store, int Plane)[] flatPlaneRefs;
        private readonly (Store Store, int Plane)[] writePlaneRefs;
        private readonly ChangingState[] changingStates;
        private readonly Vector<ulong>[] inVecScratch;
        private readonly Vector<ulong>[] flipVecScratch;
        private readonly ulong[] inScalarScratch;
        private readonly ulong[] flipScalarScratch;

        public CompiledWiring(Wiring wiring, KernelKind kind, int param0, int param1)
        {
            Wiring = wiring;
            Kind = kind;
            Param0 = param0;
            Param1 = param1;
            staticRunnable = wiring.Runnable();
            staticRunnableCount = staticRunnable.Count;

            if (kind == KernelKind.Canonical || wiring.Law is not TransitionLaw law)
            {
                distinctStores = [];
                roleOffsets = [];
                writeFlatBits = [];
                flatPlaneRefs = [];
                writePlaneRefs = [];
                changingStates = [];
                inVecScratch = [];
                flipVecScratch = [];
                inScalarScratch = [];
                flipScalarScratch = [];
                return;
            }

            // Hoist static intra-row aliasing and cross-row write-contention checks to Compile()
            // (guarded at runtime by CompiledPlan.IsCurrent, which validates Live, Attention, and ReferencePlanes).
            var clean = true;
            var writtenBits = new HashSet<(Surface, int, int)>();
            var rowBits = new HashSet<(Surface, int, int)>();
            var stores = new HashSet<Store>(ReferenceEqualityComparer.Instance);
            for (var r = 0; r < wiring.Bindings.Count; r++)
                stores.Add((Store)wiring.Bindings[r].Surface);
            distinctStores = [.. stores];

            var ident = staticRunnable.Any;
            if (ident)
            {
                var sharedLen = ((Store)wiring.Bindings[0].Surface).Count;
                for (var r = 1; r < wiring.Bindings.Count && ident; r++)
                    if (((Store)wiring.Bindings[r].Surface).Count < wiring.Count) ident = false;
                if (sharedLen < wiring.Count) ident = false;
            }

            // Fast check for identity mapping: if all roles reference row == pos, intra-row aliasing
            // reduces to plane disjointness per store, and cross-row write contention is impossible!
            if (ident)
            {
                foreach (var row in staticRunnable.Indexes())
                {
                    for (var r = 0; r < wiring.Bindings.Count && ident; r++)
                        if (wiring.Reference(row, r) != row) ident = false;
                    if (!ident) break;
                }
            }

            if (ident)
            {
                // Identity-mapped: every row uses its own distinct position, so cross-row contention cannot happen.
                // Check intra-row bit aliasing once at position 0.
                for (var r = 0; r < wiring.Bindings.Count && clean; r++)
                {
                    var b = wiring.Bindings[r];
                    for (var p = 0; p < b.Planes.Count; p++)
                        if (!rowBits.Add((b.Surface, 0, b.Plane(p)))) { clean = false; break; }
                }
                // Written planes must never be constant planes.
                for (var r = 0; r < wiring.Bindings.Count && clean; r++)
                {
                    if (!wiring.Writes(r)) continue;
                    var st = (Store)wiring.Bindings[r].Surface;
                    for (var p = 0; p < wiring.Bindings[r].Planes.Count; p++)
                        if (st.IsConstantPlane(wiring.Bindings[r].Plane(p))) { clean = false; break; }
                }
            }
            else
            {
                foreach (var row in staticRunnable.Indexes())
                {
                    rowBits.Clear();
                    for (var r = 0; r < wiring.Bindings.Count && clean; r++)
                    {
                        var b = wiring.Bindings[r];
                        var pos = wiring.Reference(row, r);
                        for (var p = 0; p < b.Planes.Count; p++)
                            if (!rowBits.Add(b.Surface.Bit(pos, b.Plane(p)))) { clean = false; break; }
                    }
                    if (!clean) break;

                    for (var r = 0; r < wiring.Bindings.Count && clean; r++)
                    {
                        if (!wiring.Writes(r)) continue;
                        var b = wiring.Bindings[r];
                        var pos = wiring.Reference(row, r);
                        for (var p = 0; p < b.Planes.Count; p++)
                            if (!writtenBits.Add(b.Surface.Bit(pos, b.Plane(p)))) { clean = false; break; }
                    }
                    if (!clean) break;
                }
            }

            staticTopologyClean = clean;
            identityMapped = ident;

            // Flatten role bit indices and precompute changing-state entries once for L1-resident SIMD!
            roleOffsets = new int[law.Roles.Count];
            var flatRefs = new List<(Store, int)>();
            var writeFlatList = new List<int>();
            var writeRefList = new List<(Store, int)>();
            var writeSlotByFlat = new Dictionary<int, int>();
            for (var r = 0; r < law.Roles.Count; r++)
            {
                roleOffsets[r] = flatRefs.Count;
                var b = wiring.Bindings[r];
                var st = (Store)b.Surface;
                var writes = wiring.Writes(r);
                for (var k = 0; k < b.Planes.Count; k++)
                {
                    var flatIdx = flatRefs.Count;
                    flatRefs.Add((st, b.Plane(k)));
                    if (writes)
                    {
                        writeSlotByFlat[flatIdx] = writeFlatList.Count;
                        writeFlatList.Add(flatIdx);
                        writeRefList.Add((st, b.Plane(k)));
                    }
                }
            }

            totalRoleBits = flatRefs.Count;
            flatPlaneRefs = [.. flatRefs];
            writeFlatBits = [.. writeFlatList];
            writePlaneRefs = [.. writeRefList];
            inVecScratch = new Vector<ulong>[totalRoleBits];
            flipVecScratch = new Vector<ulong>[writeFlatBits.Length];
            inScalarScratch = new ulong[totalRoleBits];
            flipScalarScratch = new ulong[writeFlatBits.Length];

            if (kind == KernelKind.SimdBitSliced && totalRoleBits <= 64)
            {
                var entries = new List<ChangingState>();
                for (var before = 0; before < law.StateCount; before++)
                {
                    var afters = law.Afters(before);
                    if (afters.Count != 1 || afters[0] == before) continue;
                    var bt = law.Tuple(before);
                    var at = law.Tuple(afters[0]);
                    ulong bMask = 0;
                    var flips = new List<int>();
                    for (var r = 0; r < law.Roles.Count; r++)
                    {
                        var bp = law.Roles[r].Encode(bt[r]);
                        var ap = law.Roles[r].Encode(at[r]);
                        var diff = bp ^ ap;
                        var off = roleOffsets[r];
                        for (var k = 0; k < law.Roles[r].Bits; k++)
                        {
                            if (((bp >> k) & 1) != 0) bMask |= 1UL << (off + k);
                            if (((diff >> k) & 1) != 0 && writeSlotByFlat.TryGetValue(off + k, out var wSlot))
                                flips.Add(wSlot);
                        }
                    }
                    if (flips.Count > 0) entries.Add(new ChangingState(bMask, [.. flips]));
                }
                changingStates = [.. entries];
            }
            else
            {
                changingStates = [];
            }
        }

        public Wiring Wiring { get; }
        public KernelKind Kind { get; }
        public int Param0 { get; }
        public int Param1 { get; }
        public int Take => Param0;
        public int Give => Param1;
        public int ProdCap => Param0;
        public int OverflowCap => Param1;

        /// <summary>Accelerate only unrecorded functional TransitionLaws on
        /// direct Stores (never open Relations or foreign Ports).</summary>
        public bool CanAccelerate =>
            Kind != KernelKind.Canonical
            && Wiring.Recorded is null
            && Wiring.Law is TransitionLaw { IsFunctional: true }
            && Wiring.Bindings.All(b => !b.IsOpen && b.Surface is Store);

        public (int Fast, int Deopt, bool Quiet) StepGuarded()
        {
            if (staticRunnableCount == 0) return (0, 0, true);
            if (!staticTopologyClean)
            {
                var step = Wiring.Apply();
                return (0, staticRunnableCount, step.Quiet);
            }

            if (identityMapped)
            {
                if (!AllIdentityLanesValid())
                {
                    var step = Wiring.Apply();
                    return (0, staticRunnableCount, step.Quiet);
                }

                if (Kind == KernelKind.PacketConvert)
                {
                    var changed = StepPacketConvertIdentity();
                    return (staticRunnableCount, 0, !changed);
                }

                if (Kind == KernelKind.BoundedMultiply)
                {
                    var changed = StepBoundedMultiplyIdentity();
                    return (staticRunnableCount, 0, !changed);
                }

                if (totalRoleBits <= 64)
                {
                    var changed = StepSimdIdentity();
                    return (staticRunnableCount, 0, !changed);
                }
            }

            return StepNonIdentity();
        }

        private bool AllIdentityLanesValid()
        {
            for (var s = 0; s < distinctStores.Length; s++)
                if (!LiveCovers(distinctStores[s].RawLive, staticRunnable)) return false;

            var law = (TransitionLaw)Wiring.Law;
            var runWords = staticRunnable.RawWords;
            var wordCount = runWords.Length;
            for (var r = 0; r < law.Roles.Count; r++)
            {
                var shape = law.Roles[r];
                var b = Wiring.Bindings[r];
                var st = (Store)b.Surface;
                if (shape.IsUnary)
                {
                    if (b.Planes.Count <= 1) continue;
                    var prevPlane = st.RawPlane(b.Plane(0));
                    var prevConst = prevPlane is null && st.ConstantValue(b.Plane(0));
                    for (var k = 1; k < b.Planes.Count; k++)
                    {
                        var curPlane = st.RawPlane(b.Plane(k));
                        var curConst = curPlane is null && st.ConstantValue(b.Plane(k));
                        if (prevPlane is not null && curPlane is not null)
                        {
                            var pw = prevPlane.RawWords;
                            var cw = curPlane.RawWords;
                            for (var w = 0; w < wordCount; w++)
                                if ((cw[w] & ~pw[w] & runWords[w]) != 0) return false;
                        }
                        else
                        {
                            for (var w = 0; w < wordCount; w++)
                            {
                                var pv = prevPlane is not null ? prevPlane.RawWords[w] : (prevConst ? ulong.MaxValue : 0UL);
                                var cv = curPlane is not null ? curPlane.RawWords[w] : (curConst ? ulong.MaxValue : 0UL);
                                if ((cv & ~pv & runWords[w]) != 0) return false;
                            }
                        }
                        prevPlane = curPlane;
                        prevConst = curConst;
                    }
                }
                else
                {
                    var bits = shape.Bits;
                    if (bits < 31 && shape.Size == (1 << bits)) continue; // Every binary bit pattern is valid!
                    ulong[]?[] pWords = new ulong[bits][];
                    var pConst = new bool[bits];
                    for (var k = 0; k < bits; k++)
                    {
                        var pl = st.RawPlane(b.Plane(k));
                        pWords[k] = pl?.RawWords;
                        if (pl is null) pConst[k] = st.ConstantValue(b.Plane(k));
                    }
                    for (var w = 0; w < wordCount; w++)
                    {
                        if (runWords[w] == 0) continue;
                        if ((WordBinaryGreaterOrEqual(pWords, pConst, w, shape.Size) & runWords[w]) != 0) return false;
                    }
                }
            }
            return true;
        }

        /// <summary>Zero-allocation, L1-resident SIMD bit-sliced step directly
        /// on Store planes using XOR flip masks for changing states only.</summary>
        private bool StepSimdIdentity()
        {
            if (changingStates.Length == 0) return false;
            var runWords = staticRunnable.RawWords;
            var wordCount = runWords.Length;
            var vecWidth = Vector<ulong>.Count;

            ulong[]?[] flatWords = new ulong[totalRoleBits][];
            var flatConst = new ulong[totalRoleBits];
            for (var f = 0; f < totalRoleBits; f++)
            {
                var (st, pIdx) = flatPlaneRefs[f];
                var pl = st.RawPlane(pIdx);
                flatWords[f] = pl?.RawWords;
                if (pl is null) flatConst[f] = st.ConstantValue(pIdx) ? ulong.MaxValue : 0UL;
            }
            var writeTargets = new Plane[writePlaneRefs.Length];
            var writeWords = new ulong[writePlaneRefs.Length][];
            for (var wi = 0; wi < writePlaneRefs.Length; wi++)
            {
                var (st, pIdx) = writePlaneRefs[wi];
                writeTargets[wi] = st.RawPlane(pIdx)!;
                writeWords[wi] = writeTargets[wi].RawWords;
            }

            var changed = false;
            var w = 0;
            for (; w + vecWidth <= wordCount; w += vecWidth)
            {
                var mask = new Vector<ulong>(runWords, w);
                if (Vector.EqualsAll(mask, Vector<ulong>.Zero)) continue;

                for (var f = 0; f < totalRoleBits; f++)
                    inVecScratch[f] = flatWords[f] is { } arr ? new Vector<ulong>(arr, w) : new Vector<ulong>(flatConst[f]);
                for (var wi = 0; wi < flipVecScratch.Length; wi++)
                    flipVecScratch[wi] = Vector<ulong>.Zero;

                for (var s = 0; s < changingStates.Length; s++)
                {
                    var entry = changingStates[s];
                    var bMask = entry.BeforeMask;
                    var m = mask;
                    for (var f = 0; f < totalRoleBits; f++)
                    {
                        m = ((bMask >> f) & 1) != 0 ? (m & inVecScratch[f]) : Vector.AndNot(m, inVecScratch[f]);
                        if (Vector.EqualsAll(m, Vector<ulong>.Zero)) break;
                    }
                    if (Vector.EqualsAll(m, Vector<ulong>.Zero)) continue;
                    var flips = entry.FlippedWriteIndices;
                    for (var k = 0; k < flips.Length; k++)
                        flipVecScratch[flips[k]] |= m;
                }

                for (var wi = 0; wi < writeFlatBits.Length; wi++)
                {
                    var flip = flipVecScratch[wi];
                    if (Vector.EqualsAll(flip, Vector<ulong>.Zero)) continue;
                    changed = true;
                    (inVecScratch[writeFlatBits[wi]] ^ flip).CopyTo(writeWords[wi], w);
                }
            }

            for (; w < wordCount; w++)
            {
                var mask = runWords[w];
                if (mask == 0) continue;

                for (var f = 0; f < totalRoleBits; f++)
                    inScalarScratch[f] = flatWords[f] is { } arr ? arr[w] : flatConst[f];
                for (var wi = 0; wi < flipScalarScratch.Length; wi++)
                    flipScalarScratch[wi] = 0UL;

                for (var s = 0; s < changingStates.Length; s++)
                {
                    var entry = changingStates[s];
                    var bMask = entry.BeforeMask;
                    var m = mask;
                    for (var f = 0; f < totalRoleBits && m != 0; f++)
                        m &= ((bMask >> f) & 1) != 0 ? inScalarScratch[f] : ~inScalarScratch[f];
                    if (m == 0) continue;
                    var flips = entry.FlippedWriteIndices;
                    for (var k = 0; k < flips.Length; k++)
                        flipScalarScratch[flips[k]] |= m;
                }

                for (var wi = 0; wi < writeFlatBits.Length; wi++)
                {
                    var flip = flipScalarScratch[wi];
                    if (flip == 0UL) continue;
                    changed = true;
                    writeWords[wi][w] = inScalarScratch[writeFlatBits[wi]] ^ flip;
                }
            }

            if (changed)
                for (var wi = 0; wi < writeTargets.Length; wi++)
                    writeTargets[wi].TrimTail();
            return changed;
        }

        /// <summary>Pure bitwise SIMD unary plane-shift kernel for Convert(take, give):
        /// zero integer decoding, zero encoding, 256 lanes per AVX2 instruction.</summary>
        private bool StepPacketConvertIdentity()
        {
            var srcBind = Wiring.Bindings[0];
            var dstBind = Wiring.Bindings[1];
            var srcCap = srcBind.Planes.Count;
            var dstCap = dstBind.Planes.Count;
            if (Take > srcCap || Give > dstCap) return false;

            var srcStore = (Store)srcBind.Surface;
            var dstStore = (Store)dstBind.Surface;
            var srcPlanes = new Plane[srcCap];
            var srcWords = new ulong[srcCap][];
            for (var k = 0; k < srcCap; k++) { srcPlanes[k] = srcStore.RawPlane(srcBind.Plane(k))!; srcWords[k] = srcPlanes[k].RawWords; }
            var dstPlanes = new Plane[dstCap];
            var dstWords = new ulong[dstCap][];
            for (var k = 0; k < dstCap; k++) { dstPlanes[k] = dstStore.RawPlane(dstBind.Plane(k))!; dstWords[k] = dstPlanes[k].RawWords; }

            var runWords = staticRunnable.RawWords;
            var wordCount = runWords.Length;
            var vecWidth = Vector<ulong>.Count;
            var srcCheckIdx = Take - 1;
            var dstRoomIdx = dstCap - Give;
            var allOnes = Vector<ulong>.AllBitsSet;
            var changed = false;

            var w = 0;
            for (; w + vecWidth <= wordCount; w += vecWidth)
            {
                var fire = Vector.AndNot(new Vector<ulong>(runWords, w) & new Vector<ulong>(srcWords[srcCheckIdx], w),
                    new Vector<ulong>(dstWords[dstRoomIdx], w));
                if (Vector.EqualsAll(fire, Vector<ulong>.Zero)) continue;
                changed = true;

                // Shift src unary prefix down by Take (increasing j so src[j + Take] is still unread).
                for (var j = 0; j < srcCap; j++)
                {
                    var cur = new Vector<ulong>(srcWords[j], w);
                    var next = j + Take < srcCap ? new Vector<ulong>(srcWords[j + Take], w) : Vector<ulong>.Zero;
                    (Vector.AndNot(cur, fire) | (next & fire)).CopyTo(srcWords[j], w);
                }
                // Shift dst unary prefix up by Give (decreasing j so dst[j - Give] is still unread).
                for (var j = dstCap - 1; j >= 0; j--)
                {
                    var cur = new Vector<ulong>(dstWords[j], w);
                    var next = j >= Give ? new Vector<ulong>(dstWords[j - Give], w) : allOnes;
                    (Vector.AndNot(cur, fire) | (next & fire)).CopyTo(dstWords[j], w);
                }
            }

            for (; w < wordCount; w++)
            {
                var fire = runWords[w] & srcWords[srcCheckIdx][w] & ~dstWords[dstRoomIdx][w];
                if (fire == 0UL) continue;
                changed = true;

                for (var j = 0; j < srcCap; j++)
                {
                    var next = j + Take < srcCap ? srcWords[j + Take][w] : 0UL;
                    srcWords[j][w] = (srcWords[j][w] & ~fire) | (next & fire);
                }
                for (var j = dstCap - 1; j >= 0; j--)
                {
                    var next = j >= Give ? dstWords[j - Give][w] : ulong.MaxValue;
                    dstWords[j][w] = (dstWords[j][w] & ~fire) | (next & fire);
                }
            }

            if (changed)
            {
                for (var k = 0; k < srcCap; k++) srcPlanes[k].TrimTail();
                for (var k = 0; k < dstCap; k++) dstPlanes[k].TrimTail();
            }
            return changed;
        }

        /// <summary>Zero-allocation register-level widening integer MUL -> T+
        /// directly over Store plane words.</summary>
        private bool StepBoundedMultiplyIdentity()
        {
            var law = (TransitionLaw)Wiring.Law;
            var aBind = Wiring.Bindings[0];
            var bBind = Wiring.Bindings[1];
            var vBind = Wiring.Bindings[2];
            var oBind = Wiring.Bindings[3];
            var aStore = (Store)aBind.Surface;
            var bStore = (Store)bBind.Surface;
            var vStore = (Store)vBind.Surface;
            var oStore = (Store)oBind.Surface;

            var aUnary = law.Roles[0].IsUnary;
            var bUnary = law.Roles[1].IsUnary;
            var vUnary = law.Roles[2].IsUnary;
            var oUnary = law.Roles[3].IsUnary;

            var aBits = aBind.Planes.Count;
            var bBits = bBind.Planes.Count;
            var vBits = vBind.Planes.Count;
            var oBits = oBind.Planes.Count;

            ulong[]?[] aWords = new ulong[aBits][];
            var aConst = new ulong[aBits];
            for (var k = 0; k < aBits; k++) { var pl = aStore.RawPlane(aBind.Plane(k)); aWords[k] = pl?.RawWords; if (pl is null) aConst[k] = aStore.ConstantValue(aBind.Plane(k)) ? ulong.MaxValue : 0UL; }
            ulong[]?[] bWords = new ulong[bBits][];
            var bConst = new ulong[bBits];
            for (var k = 0; k < bBits; k++) { var pl = bStore.RawPlane(bBind.Plane(k)); bWords[k] = pl?.RawWords; if (pl is null) bConst[k] = bStore.ConstantValue(bBind.Plane(k)) ? ulong.MaxValue : 0UL; }

            var vPlanes = new Plane[vBits];
            var vWords = new ulong[vBits][];
            for (var k = 0; k < vBits; k++) { vPlanes[k] = vStore.RawPlane(vBind.Plane(k))!; vWords[k] = vPlanes[k].RawWords; }
            var oPlanes = new Plane[oBits];
            var oWords = new ulong[oBits][];
            for (var k = 0; k < oBits; k++) { oPlanes[k] = oStore.RawPlane(oBind.Plane(k))!; oWords[k] = oPlanes[k].RawWords; }

            var aCur = new ulong[aBits];
            var bCur = new ulong[bBits];
            var vNext = new ulong[vBits];
            var oNext = new ulong[oBits];

            var runWords = staticRunnable.RawWords;
            var wordCount = runWords.Length;
            var cap = (ulong)ProdCap;
            var changed = false;

            for (var w = 0; w < wordCount; w++)
            {
                var mask = runWords[w];
                if (mask == 0UL) continue;

                for (var k = 0; k < aBits; k++) aCur[k] = aWords[k] is { } arr ? arr[w] : aConst[k];
                for (var k = 0; k < bBits; k++) bCur[k] = bWords[k] is { } arr ? arr[w] : bConst[k];
                for (var k = 0; k < vBits; k++) vNext[k] = vWords[k][w] & ~mask;
                for (var k = 0; k < oBits; k++) oNext[k] = oWords[k][w] & ~mask;

                var rem = mask;
                while (rem != 0UL)
                {
                    var bit = BitOperations.TrailingZeroCount(rem);
                    var bitMask = 1UL << bit;
                    rem &= rem - 1;

                    ulong a = 0, b = 0;
                    if (aUnary) { for (var k = 0; k < aBits; k++) a += (aCur[k] >> bit) & 1UL; }
                    else { for (var k = 0; k < aBits; k++) a |= ((aCur[k] >> bit) & 1UL) << k; }

                    if (bUnary) { for (var k = 0; k < bBits; k++) b += (bCur[k] >> bit) & 1UL; }
                    else { for (var k = 0; k < bBits; k++) b |= ((bCur[k] >> bit) & 1UL) << k; }

                    var prod = a * b;
                    var val = prod <= cap ? (int)prod : ProdCap;
                    var ovf = prod > cap ? (int)(prod - cap) : 0;

                    if (vUnary) { for (var k = 0; k < val; k++) vNext[k] |= bitMask; }
                    else { for (var k = 0; k < vBits; k++) if (((val >> k) & 1) != 0) vNext[k] |= bitMask; }

                    if (oUnary) { for (var k = 0; k < ovf; k++) oNext[k] |= bitMask; }
                    else { for (var k = 0; k < oBits; k++) if (((ovf >> k) & 1) != 0) oNext[k] |= bitMask; }
                }

                for (var k = 0; k < vBits; k++)
                    if (vWords[k][w] != vNext[k]) { vWords[k][w] = vNext[k]; changed = true; }
                for (var k = 0; k < oBits; k++)
                    if (oWords[k][w] != oNext[k]) { oWords[k][w] = oNext[k]; changed = true; }
            }

            if (changed)
            {
                for (var k = 0; k < vBits; k++) vPlanes[k].TrimTail();
                for (var k = 0; k < oBits; k++) oPlanes[k].TrimTail();
            }
            return changed;
        }

        private (int Fast, int Deopt, bool Quiet) StepNonIdentity()
        {
            var law = (TransitionLaw)Wiring.Law;
            foreach (var row in staticRunnable.Indexes())
            {
                for (var r = 0; r < Wiring.Bindings.Count; r++)
                {
                    var b = Wiring.Bindings[r];
                    var store = (Store)b.Surface;
                    var pos = Wiring.Reference(row, r);
                    if (!store.IsLive(pos))
                    {
                        var step = Wiring.Apply();
                        return (0, staticRunnableCount, step.Quiet);
                    }
                    ulong pat = 0;
                    for (var k = 0; k < b.Planes.Count; k++)
                        if (store.Read(b.Plane(k), pos)) pat |= 1UL << k;
                    if (law.Roles[r].Decode(pat) is null)
                    {
                        var step = Wiring.Apply();
                        return (0, staticRunnableCount, step.Quiet);
                    }
                }
            }

            var changed = false;
            if (Kind == KernelKind.PacketConvert)
            {
                var srcBind = Wiring.Bindings[0];
                var dstBind = Wiring.Bindings[1];
                var srcStore = (Store)srcBind.Surface;
                var dstStore = (Store)dstBind.Surface;
                var srcField = new Field(law.Roles[0], [.. srcBind.Planes]);
                var dstField = new Field(law.Roles[1], [.. dstBind.Planes]);
                var dstCap = law.Roles[1].Size - 1;
                foreach (var row in staticRunnable.Indexes())
                {
                    var sp = Wiring.Reference(row, 0);
                    var dp = Wiring.Reference(row, 1);
                    var s = srcField.Read(srcStore, sp)!.Value;
                    var d = dstField.Read(dstStore, dp)!.Value;
                    if (s >= Take && d + Give <= dstCap)
                    {
                        srcField.Write(srcStore, sp, s - Take);
                        dstField.Write(dstStore, dp, d + Give);
                        changed = true;
                    }
                }
                return (staticRunnableCount, 0, !changed);
            }

            if (Kind == KernelKind.BoundedMultiply)
            {
                var aBind = Wiring.Bindings[0];
                var bBind = Wiring.Bindings[1];
                var vBind = Wiring.Bindings[2];
                var oBind = Wiring.Bindings[3];
                var aStore = (Store)aBind.Surface;
                var bStore = (Store)bBind.Surface;
                var vStore = (Store)vBind.Surface;
                var oStore = (Store)oBind.Surface;
                var aField = new Field(law.Roles[0], [.. aBind.Planes]);
                var bField = new Field(law.Roles[1], [.. bBind.Planes]);
                var vField = new Field(law.Roles[2], [.. vBind.Planes]);
                var oField = new Field(law.Roles[3], [.. oBind.Planes]);
                var cap = (ulong)ProdCap;
                foreach (var row in staticRunnable.Indexes())
                {
                    var a = (ulong)aField.Read(aStore, Wiring.Reference(row, 0))!.Value;
                    var b = (ulong)bField.Read(bStore, Wiring.Reference(row, 1))!.Value;
                    var vp = Wiring.Reference(row, 2);
                    var op = Wiring.Reference(row, 3);
                    var prod = a * b;
                    var val = prod <= cap ? (int)prod : ProdCap;
                    var ovf = prod > cap ? (int)(prod - cap) : 0;
                    if (vField.Read(vStore, vp)!.Value != val) { vField.Write(vStore, vp, val); changed = true; }
                    if (oField.Read(oStore, op)!.Value != ovf) { oField.Write(oStore, op, ovf); changed = true; }
                }
                return (staticRunnableCount, 0, !changed);
            }

            // Packed SIMD buffer for non-identity row mappings: pack runnable rows -> SIMD -> unpack.
            var activeRows = staticRunnable.Indexes().ToArray();
            var n = activeRows.Length;
            var codes = new Plane[law.Roles.Count][];
            for (var r = 0; r < law.Roles.Count; r++)
            {
                var b = Wiring.Bindings[r];
                var store = (Store)b.Surface;
                codes[r] = new Plane[b.Planes.Count];
                for (var k = 0; k < b.Planes.Count; k++)
                {
                    var buf = new Plane(n);
                    var pIdx = b.Plane(k);
                    for (var idx = 0; idx < n; idx++) buf[idx] = store.Read(pIdx, Wiring.Reference(activeRows[idx], r));
                    codes[r][k] = buf;
                }
            }
            var simdStep = CompiledSimd.StepTransition(law, codes, Plane.Ones(n));
            for (var r = 0; r < law.Roles.Count; r++)
            {
                if (!Wiring.Writes(r)) continue;
                var b = Wiring.Bindings[r];
                var store = (Store)b.Surface;
                for (var k = 0; k < b.Planes.Count; k++)
                {
                    var pIdx = b.Plane(k);
                    for (var idx = 0; idx < n; idx++)
                    {
                        var pos = Wiring.Reference(activeRows[idx], r);
                        var nv = simdStep.After[r][k][idx];
                        if (store.Read(pIdx, pos) != nv) { store.Write(pIdx, pos, nv); changed = true; }
                    }
                }
            }
            return (staticRunnableCount, 0, !changed);
        }

        public (int Fast, int Deopt, int Ticks) SettlePacketConvertExact(int limit)
        {
            if (staticRunnableCount == 0) return (0, 0, limit > 0 ? 1 : 0);
            if (!staticTopologyClean) return (0, staticRunnableCount, 0);

            if (identityMapped)
            {
                if (!AllIdentityLanesValid()) return (0, staticRunnableCount, 0);
                // Direct word-level unary packet-exact bulk settle (p = min(s/Take, room/Give, limit)):
                var srcBind = Wiring.Bindings[0];
                var dstBind = Wiring.Bindings[1];
                var srcCap = srcBind.Planes.Count;
                var dstCap = dstBind.Planes.Count;
                var srcStore = (Store)srcBind.Surface;
                var dstStore = (Store)dstBind.Surface;
                var srcPlanes = new Plane[srcCap];
                var srcWords = new ulong[srcCap][];
                for (var k = 0; k < srcCap; k++) { srcPlanes[k] = srcStore.RawPlane(srcBind.Plane(k))!; srcWords[k] = srcPlanes[k].RawWords; }
                var dstPlanes = new Plane[dstCap];
                var dstWords = new ulong[dstCap][];
                for (var k = 0; k < dstCap; k++) { dstPlanes[k] = dstStore.RawPlane(dstBind.Plane(k))!; dstWords[k] = dstPlanes[k].RawWords; }

                var runWords = staticRunnable.RawWords;
                var wordCount = runWords.Length;
                var maxPackets = 0;
                var changed = false;

                for (var w = 0; w < wordCount; w++)
                {
                    var rem = runWords[w];
                    if (rem == 0UL) continue;
                    // Quick word skip: if no lane in this 64-lane word can fire even 1 packet, skip entire word!
                    if (Take > srcCap || Give > dstCap) continue;
                    var canAny = rem & srcWords[Take - 1][w] & ~dstWords[dstCap - Give][w];
                    if (canAny == 0UL) continue;

                    while (canAny != 0UL)
                    {
                        var bit = BitOperations.TrailingZeroCount(canAny);
                        var bitMask = 1UL << bit;
                        canAny &= canAny - 1;

                        var s = 0;
                        for (var k = 0; k < srcCap; k++) s += (int)((srcWords[k][w] >> bit) & 1UL);
                        var d = 0;
                        for (var k = 0; k < dstCap; k++) d += (int)((dstWords[k][w] >> bit) & 1UL);
                        var bySrc = s / Take;
                        var byRoom = (dstCap - d) / Give;
                        var packets = bySrc < byRoom ? bySrc : byRoom;
                        if (packets > limit) packets = limit;
                        if (packets > maxPackets) maxPackets = packets;
                        if (packets > 0)
                        {
                            changed = true;
                            var ns = s - packets * Take;
                            var nd = d + packets * Give;
                            for (var k = ns; k < s; k++) srcWords[k][w] &= ~bitMask;
                            for (var k = d; k < nd; k++) dstWords[k][w] |= bitMask;
                        }
                    }
                }

                if (changed)
                {
                    for (var k = 0; k < srcCap; k++) srcPlanes[k].TrimTail();
                    for (var k = 0; k < dstCap; k++) dstPlanes[k].TrimTail();
                }
                var ticks = maxPackets < limit ? maxPackets + 1 : limit;
                return (staticRunnableCount, 0, ticks);
            }

            var law = (TransitionLaw)Wiring.Law;
            var srcB = Wiring.Bindings[0];
            var dstB = Wiring.Bindings[1];
            var srcSt = (Store)srcB.Surface;
            var dstSt = (Store)dstB.Surface;
            var srcF = new Field(law.Roles[0], [.. srcB.Planes]);
            var dstF = new Field(law.Roles[1], [.. dstB.Planes]);
            foreach (var row in staticRunnable.Indexes())
            {
                var sp = Wiring.Reference(row, 0);
                var dp = Wiring.Reference(row, 1);
                if (!srcSt.IsLive(sp) || !dstSt.IsLive(dp) || srcF.Read(srcSt, sp) is null || dstF.Read(dstSt, dp) is null)
                    return (0, staticRunnableCount, 0);
            }

            var dCap = law.Roles[1].Size - 1;
            var maxP = 0;
            foreach (var row in staticRunnable.Indexes())
            {
                var sp = Wiring.Reference(row, 0);
                var dp = Wiring.Reference(row, 1);
                var s = srcF.Read(srcSt, sp)!.Value;
                var d = dstF.Read(dstSt, dp)!.Value;
                var bySrc = s / Take;
                var byRoom = (dCap - d) / Give;
                var packets = bySrc < byRoom ? bySrc : byRoom;
                if (packets > limit) packets = limit;
                if (packets > maxP) maxP = packets;
                if (packets > 0)
                {
                    srcF.Write(srcSt, sp, s - packets * Take);
                    dstF.Write(dstSt, dp, d + packets * Give);
                }
            }
            var t = maxP < limit ? maxP + 1 : limit;
            return (staticRunnableCount, 0, t);
        }
    }

    private static CompiledWiring CompileWiring(Wiring w)
    {
        if (w.Law is TransitionLaw t && t.IsFunctional && w.Recorded is null && w.Bindings.All(b => !b.IsOpen && b.Surface is Store))
        {
            if (TryRecognizePacketConvert(t, out var take, out var give))
                return new CompiledWiring(w, KernelKind.PacketConvert, take, give);
            if (TryRecognizeBoundedMultiply(t, out var prodCap, out var ovfCap))
                return new CompiledWiring(w, KernelKind.BoundedMultiply, prodCap, ovfCap);
            return new CompiledWiring(w, KernelKind.SimdBitSliced, 0, 0);
        }
        return new CompiledWiring(w, KernelKind.Canonical, 0, 0);
    }

    private sealed class RecognizedOscillator(
        Wiring kickWiring,
        Wiring driftWiring,
        Store store,
        Field fx,
        Field fv,
        int maxAbs,
        int shift,
        int feedbackSign,
        bool clamps)
    {
        public int MaxAbs => maxAbs;
        public int Shift => shift;
        public int FeedbackSign => feedbackSign;

        public bool CanRunFast() =>
            kickWiring.Recorded is null && driftWiring.Recorded is null
            && kickWiring.Runnable().Equals(driftWiring.Runnable())
            && kickWiring.Runnable().Any;

        public (int X, int V) ReadLane0() =>
            (Dynamics.DecodeSigned(fx.Read(store, kickWiring.Reference(0, 0))!.Value, maxAbs),
             Dynamics.DecodeSigned(fv.Read(store, kickWiring.Reference(0, 1))!.Value, maxAbs));

        public (int Fast, int Deopt, bool Changed) StepFast()
        {
            var runnable = kickWiring.Runnable();
            var seen = new HashSet<int>();
            foreach (var row in runnable.Indexes())
            {
                var px = kickWiring.Reference(row, 0);
                var pv = kickWiring.Reference(row, 1);
                if (px != driftWiring.Reference(row, 1) || pv != driftWiring.Reference(row, 0)) return (0, runnable.Count, false);
                if (!store.IsLive(px) || !store.IsLive(pv) || !seen.Add(px)) return (0, runnable.Count, false);
                var rx = fx.Read(store, px);
                var rv = fv.Read(store, pv);
                if (rx is null || rv is null) return (0, runnable.Count, false);
                var x = rx.Value - maxAbs;
                var v = rv.Value - maxAbs;
                var nv = v - feedbackSign * Dynamics.Coarse(x, shift);
                if (!clamps && (nv < -maxAbs || nv > maxAbs)) return (0, runnable.Count, false);
                var clampedV = nv < -maxAbs ? -maxAbs : nv > maxAbs ? maxAbs : nv;
                var nx = x + Dynamics.Coarse(clampedV, shift);
                if (!clamps && (nx < -maxAbs || nx > maxAbs)) return (0, runnable.Count, false);
            }

            var changed = false;
            foreach (var row in runnable.Indexes())
            {
                var px = kickWiring.Reference(row, 0);
                var pv = kickWiring.Reference(row, 1);
                var x = fx.Read(store, px)!.Value - maxAbs;
                var v = fv.Read(store, pv)!.Value - maxAbs;
                var nv = v - feedbackSign * Dynamics.Coarse(x, shift);
                if (nv < -maxAbs) nv = -maxAbs; else if (nv > maxAbs) nv = maxAbs;
                var nx = x + Dynamics.Coarse(nv, shift);
                if (nx < -maxAbs) nx = -maxAbs; else if (nx > maxAbs) nx = maxAbs;
                if (nv != v) { fv.Write(store, pv, nv + maxAbs); changed = true; }
                if (nx != x) { fx.Write(store, px, nx + maxAbs); changed = true; }
            }
            return (runnable.Count, 0, changed);
        }
    }

    private static RecognizedOscillator? TryRecognizeKickDrift(Engine engine)
    {
        if (engine.Together || engine.Wirings.Count != 2) return null;
        var (w0, w1) = (engine.Wirings[0], engine.Wirings[1]);
        if (w0.Law is not TransitionLaw t0 || w1.Law is not TransitionLaw t1) return null;
        if (!TryRecognizeSignedShear(t0, out var max0, out var sh0, out var sgn0, out var cl0)) return null;
        if (!TryRecognizeSignedShear(t1, out var max1, out var sh1, out var sgn1, out var cl1)) return null;
        if (max0 != max1 || sh0 != sh1 || cl0 != cl1 || sgn1 != +1) return null;
        if (w0.Bindings[0].Surface is not Store st || !ReferenceEquals(w0.Bindings[1].Surface, st)
            || !ReferenceEquals(w1.Bindings[0].Surface, st) || !ReferenceEquals(w1.Bindings[1].Surface, st)) return null;
        if (!w0.Bindings[0].Planes.SequenceEqual(w1.Bindings[1].Planes)
            || !w0.Bindings[1].Planes.SequenceEqual(w1.Bindings[0].Planes)) return null;
        if (w0.Bindings[0].Planes.Intersect(w0.Bindings[1].Planes).Any()) return null;
        var shape = t0.Roles[0];
        var fx = new Field(shape, [.. w0.Bindings[0].Planes]);
        var fv = new Field(shape, [.. w0.Bindings[1].Planes]);
        return new RecognizedOscillator(w0, w1, st, fx, fv, max0, sh0, -sgn0, cl0);
    }

    /// <summary>Recognized sealed multi-rung radix-B ripple-carry adder chain
    /// (N wirings of DigitAddStep chaining cout[i] -> cin[i+1]) lowered to
    /// native widening ulong integer addition with exact carry-out.</summary>
    private sealed class RecognizedLadderAdder(
        Wiring[] wirings,
        Store store,
        int radix,
        Field[] a,
        Field[] b,
        Field[] sum,
        int cinPlane,
        int coutPlane,
        int[] carryPlanes)
    {
        public bool CanRunFast()
        {
            var r0 = wirings[0].Runnable();
            if (r0.None) return false;
            for (var i = 0; i < wirings.Length; i++)
                if (wirings[i].Recorded is not null || !wirings[i].Runnable().Equals(r0)) return false;
            return true;
        }

        public (int Fast, int Deopt, bool Changed) StepFast()
        {
            var runnable = wirings[0].Runnable();
            var k = wirings.Length;
            ulong modulus = 1;
            for (var i = 0; i < k; i++) modulus *= (ulong)radix;

            var seenPos = new HashSet<int>();
            foreach (var row in runnable.Indexes())
            {
                var pos = wirings[0].Reference(row, 0);
                if (!store.IsLive(pos) || !seenPos.Add(pos)) return (0, runnable.Count, false);
                for (var i = 0; i < k; i++)
                {
                    for (var r = 0; r < 5; r++)
                        if (wirings[i].Reference(row, r) != pos) return (0, runnable.Count, false);
                    if (a[i].Read(store, pos) is null || b[i].Read(store, pos) is null || sum[i].Read(store, pos) is null)
                        return (0, runnable.Count, false);
                }
            }

            var changed = false;
            foreach (var row in runnable.Indexes())
            {
                var pos = wirings[0].Reference(row, 0);
                ulong va = 0, vb = 0, w = 1;
                for (var i = 0; i < k; i++)
                {
                    va += (ulong)a[i].Read(store, pos)!.Value * w;
                    vb += (ulong)b[i].Read(store, pos)!.Value * w;
                    w *= (ulong)radix;
                }
                ulong cin = store.Read(cinPlane, pos) ? 1UL : 0UL;
                var total = va + vb + cin;
                ulong carry = cin;
                for (var i = 0; i < k; i++)
                {
                    var da = (ulong)a[i].Read(store, pos)!.Value;
                    var db = (ulong)b[i].Read(store, pos)!.Value;
                    var rungTotal = da + db + carry;
                    var nextDigit = (int)(rungTotal % (ulong)radix);
                    if (sum[i].Read(store, pos)!.Value != nextDigit) { sum[i].Write(store, pos, nextDigit); changed = true; }
                    carry = rungTotal / (ulong)radix;
                    if (i < carryPlanes.Length)
                    {
                        var nextCarry = carry != 0;
                        if (store.Read(carryPlanes[i], pos) != nextCarry) { store.Write(carryPlanes[i], pos, nextCarry); changed = true; }
                    }
                }
                var nextCout = total >= modulus;
                if (store.Read(coutPlane, pos) != nextCout) { store.Write(coutPlane, pos, nextCout); changed = true; }
            }
            return (runnable.Count, 0, changed);
        }
    }

    private static RecognizedLadderAdder? TryRecognizeLadderAdder(Engine engine)
    {
        if (engine.Together || engine.Wirings.Count < 2) return null;
        var k = engine.Wirings.Count;
        var wirings = new Wiring[k];
        int radix = 0;
        Store? store = null;
        var a = new Field[k];
        var b = new Field[k];
        var sum = new Field[k];
        var carries = new int[k - 1];
        for (var i = 0; i < k; i++)
        {
            var w = engine.Wirings[i];
            if (w.Law is not TransitionLaw t || !TryRecognizeDigitAddStep(t, out var r)) return null;
            if (i == 0) radix = r; else if (r != radix) return null;
            if (w.Bindings.Any(bind => bind.IsOpen || bind.Surface is not Store)) return null;
            var s0 = (Store)w.Bindings[0].Surface;
            if (store is null) store = s0; else if (!ReferenceEquals(store, s0)) return null;
            if (w.Bindings.Any(bind => !ReferenceEquals(bind.Surface, store))) return null;
            if (i > 0)
            {
                var prevCout = wirings[i - 1].Bindings[4].Plane(0);
                if (w.Bindings[2].Plane(0) != prevCout) return null;
                carries[i - 1] = prevCout;
            }
            wirings[i] = w;
            a[i] = new Field(t.Roles[0], [.. w.Bindings[0].Planes]);
            b[i] = new Field(t.Roles[1], [.. w.Bindings[1].Planes]);
            sum[i] = new Field(t.Roles[3], [.. w.Bindings[3].Planes]);
        }

        ulong modulus = 1;
        for (var i = 0; i < k; i++)
        {
            if (modulus > (ulong.MaxValue / 2) / (ulong)radix) return null;
            modulus *= (ulong)radix;
        }

        var cinPlane = wirings[0].Bindings[2].Plane(0);
        var coutPlane = wirings[k - 1].Bindings[4].Plane(0);
        var readPlanes = new HashSet<int> { cinPlane };
        for (var i = 0; i < k; i++)
        {
            foreach (var p in a[i].Planes) readPlanes.Add(p);
            foreach (var p in b[i].Planes) readPlanes.Add(p);
        }
        var writtenPlanes = new HashSet<int>();
        for (var i = 0; i < k; i++)
            foreach (var p in sum[i].Planes)
                if (!writtenPlanes.Add(p) || readPlanes.Contains(p)) return null;
        foreach (var p in carries)
            if (!writtenPlanes.Add(p) || readPlanes.Contains(p)) return null;
        if (!writtenPlanes.Add(coutPlane) || readPlanes.Contains(coutPlane)) return null;

        return new RecognizedLadderAdder(wirings, store!, radix, a, b, sum, cinPlane, coutPlane, carries);
    }

    private sealed class WiringSnapshot
    {
        private readonly Wiring wiring;
        private readonly Law law;
        private readonly int count;
        private readonly Plane live;
        private readonly Plane? attention;
        private readonly bool recorded;
        private readonly (Surface Surface, int[] Planes, Port? Plugged)[] bindings;
        private readonly Plane[][] refPlanes;

        private WiringSnapshot(Wiring wiring)
        {
            this.wiring = wiring;
            law = wiring.Law;
            count = wiring.Count;
            live = wiring.Live.Clone();
            attention = wiring.Attention is int att ? wiring.Rows.Read(att).Clone() : null;
            recorded = wiring.Recorded is not null;
            bindings = [.. wiring.Bindings.Select(b => (b.Surface, b.Planes.ToArray(), (b.Surface as Socket)?.Plugged))];
            refPlanes = [.. Enumerable.Range(0, wiring.Bindings.Count).Select(r => wiring.ReferencePlanes(r).Select(p => wiring.Rows.Read(p).Clone()).ToArray())];
        }

        public static WiringSnapshot Capture(Wiring wiring) => new(wiring);

        public bool Matches(Wiring other)
        {
            if (!ReferenceEquals(wiring, other) || !ReferenceEquals(law, other.Law) || count != other.Count) return false;
            if (recorded != (other.Recorded is not null) || !live.Equals(other.Rows.RawLive)) return false;
            if ((attention is null) != (other.Attention is null)) return false;
            if (attention is not null && !PlaneMatchesStore(attention, other.Rows, other.Attention!.Value)) return false;
            for (var r = 0; r < bindings.Length; r++)
            {
                if (!ReferenceEquals(bindings[r].Surface, other.Bindings[r].Surface)) return false;
                if (bindings[r].Surface is Socket sock && !ReferenceEquals(bindings[r].Plugged, sock.Plugged)) return false;
                var rp = other.ReferencePlanes(r);
                if (rp.Count != refPlanes[r].Length) return false;
                for (var k = 0; k < rp.Count; k++)
                    if (!PlaneMatchesStore(refPlanes[r][k], other.Rows, rp[k])) return false;
            }
            return true;
        }

        private static bool PlaneMatchesStore(Plane snapshot, Store store, int planeIndex)
        {
            if (store.RawPlane(planeIndex) is { } raw) return snapshot.Equals(raw);
            return snapshot.Equals(store.Read(planeIndex));
        }
    }
}
