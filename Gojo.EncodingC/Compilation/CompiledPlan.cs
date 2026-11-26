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
    private readonly RecognizedLandmarkOscillator? landmarkOscillator;
    private readonly RecognizedLadderAdder? ladderAdder;

    private CompiledPlan(
        Engine engine,
        WiringSnapshot[] snapshots,
        CompiledWiring[] compiled,
        RecognizedOscillator? oscillator,
        RecognizedLandmarkOscillator? landmarkOscillator,
        RecognizedLadderAdder? ladderAdder)
    {
        this.engine = engine;
        snapshotTogether = engine.Together;
        wiringSnapshots = snapshots;
        this.compiled = compiled;
        this.oscillator = oscillator;
        this.landmarkOscillator = landmarkOscillator;
        this.ladderAdder = ladderAdder;
    }

    public Engine Source => engine;

    /// <summary>True iff a packet-exact Convert(take, give) law was recognized
    /// strictly from its table bits and PreservedSum accounting.</summary>
    public bool RecognizedPacketConvert => compiled.Any(c => c.Kind == KernelKind.PacketConvert);

    /// <summary>True iff a functional lossless bounded multiplier with explicit
    /// T+ overflow was recognized strictly from its table bits.</summary>
    public bool RecognizedBoundedMultiply => compiled.Any(c => c.Kind == KernelKind.BoundedMultiply);

    /// <summary>True iff a sealed multi-rung radix-B ripple adder (combined or
    /// split DigitSum+DigitCarry up to base 10) was recognized strictly from
    /// verified balance tables and carry wiring.</summary>
    public bool RecognizedRadixAdder => ladderAdder is not null;

    /// <summary>True iff a declared-order symplectic kick-drift oscillator was
    /// recognized strictly from its two signed-shear tables.</summary>
    public bool RecognizedKickDriftOscillator => oscillator is not null;

    /// <summary>True iff an unsigned live-landmark kick-drift oscillator was
    /// recognized strictly from its two balance tables via
    /// <see cref="Recognition.DiscoverBoundBalance"/>.</summary>
    public bool RecognizedUnsignedLandmarkOscillator => landmarkOscillator is not null;

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
        var lmOsc = osc is null ? TryRecognizeLandmarkOscillator(engine) : null;
        var adder = TryRecognizeLadderAdder(engine);
        return new CompiledPlan(engine, snapshots, compiled, osc, lmOsc, adder);
    }

    /// <summary>Mechanically verifies whether <paramref name="law"/> is an
    /// exact packet conversion <c>Convert(take, give)</c> via
    /// <see cref="Recognition.ReadHints"/> and table verification.</summary>
    public static bool TryRecognizePacketConvert(TransitionLaw law, out int take, out int give)
    {
        take = give = 0;
        if (law.Roles.Count != 2 || !law.Roles[0].IsUnary || !law.Roles[1].IsUnary || !law.IsFunctional) return false;
        var hints = Recognition.ReadHints(law);
        if (hints.Trades.Length != 1) return false;
        var (from, to, t, g) = hints.Trades[0];
        if (from != 0 || to != 1 || t < 1 || g < 1) return false;
        var srcCap = law.Roles[0].Size - 1;
        var dstCap = law.Roles[1].Size - 1;
        for (var s = 0; s <= srcCap; s++)
            for (var d = 0; d <= dstCap; d++)
            {
                var can = s >= t && d + g <= dstCap;
                var after = law.Tuple(law.Afters(law.TupleIndex([s, d]))[0]);
                if (after[0] != (can ? s - t : s) || after[1] != (can ? d + g : d)) return false;
            }
        take = t;
        give = g;
        return true;
    }

    /// <summary>Mechanically verifies whether <paramref name="law"/> is a
    /// functional lossless bounded multiplier <c>(a [Unchanged], b [Unchanged],
    /// value, overflow/T+)</c> where <c>prodCap + overflowCap ≥ maxA · maxB</c>.</summary>
    public static bool TryRecognizeBoundedMultiply(TransitionLaw law, out int prodCap, out int overflowCap)
    {
        prodCap = overflowCap = 0;
        if (law.Roles.Count != 4 || !law.IsFunctional || law.Writes(0) || law.Writes(1)) return false;
        int maxA = law.Roles[0].Size - 1, maxB = law.Roles[1].Size - 1;
        int cap = law.Roles[2].Size - 1, ovfCap = law.Roles[3].Size - 1;
        if (cap < 1 || ovfCap < 1 || (long)cap + ovfCap < (long)maxA * maxB) return false;
        for (var state = 0; state < law.StateCount; state++)
        {
            var t = law.Tuple(state);
            var p = t[0] * t[1];
            var expVal = p <= cap ? p : cap;
            var expOvf = p <= cap ? 0 : p - cap;
            var after = law.Tuple(law.Afters(state)[0]);
            if (after[0] != t[0] || after[1] != t[1] || after[2] != expVal || after[3] != expOvf) return false;
        }
        prodCap = cap;
        overflowCap = ovfCap;
        return true;
    }

    /// <summary>Mechanically verifies whether <paramref name="law"/> is a
    /// justified functional partition-p ripple adder (a, b, cin [Unchanged] ->
    /// sum, cout) strictly from every cell of its table bits.</summary>
    public static bool TryRecognizeDigitAddStep(TransitionLaw law, out int radix)
    {
        radix = 0;
        if (law.Roles.Count != 5 || !law.IsFunctional) return false;
        var digit = law.Roles[0];
        var b = digit.Size;
        if (b < 2 || law.Roles[1] != digit || law.Roles[2].Size != 2 || law.Roles[3] != digit || law.Roles[4].Size != 2)
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

        if (landmarkOscillator is { } lmOsc && lmOsc.CanRunFast())
        {
            var (fast, deopt, changed) = lmOsc.StepFast();
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

    /// <summary>Recognized unsigned live-landmark kick-drift oscillator
    /// (ops_claude's <c>LandmarkShear</c> balances: <c>v' = v + k·(L − x)</c>
    /// then <c>x' = x + (v − R)</c>, holding when out of track range, with
    /// optional blocked bits) lowered directly to integer register math.</summary>
    private sealed class RecognizedLandmarkOscillator(
        Wiring kickWiring,
        Wiring driftWiring,
        Store store,
        Field fx,
        Field fv,
        Field fLandmark,
        Field fRest,
        int trackCap,
        int kickWeight,
        int kickBlockedPlane,
        int driftBlockedPlane)
    {
        public bool CanRunFast() =>
            kickWiring.Recorded is null && driftWiring.Recorded is null
            && kickWiring.Runnable().Equals(driftWiring.Runnable())
            && kickWiring.Runnable().Any;

        public (int Fast, int Deopt, bool Changed) StepFast()
        {
            var runnable = kickWiring.Runnable();
            var seen = new HashSet<int>();
            foreach (var row in runnable.Indexes())
            {
                var pos = kickWiring.Reference(row, 0);
                if (!store.IsLive(pos) || !seen.Add(pos)) return (0, runnable.Count, false);
                for (var r = 0; r < kickWiring.Bindings.Count; r++)
                    if (kickWiring.Reference(row, r) != pos) return (0, runnable.Count, false);
                for (var r = 0; r < driftWiring.Bindings.Count; r++)
                    if (driftWiring.Reference(row, r) != pos) return (0, runnable.Count, false);
                if (fx.Read(store, pos) is null || fv.Read(store, pos) is null
                    || fLandmark.Read(store, pos) is null || fRest.Read(store, pos) is null)
                    return (0, runnable.Count, false);
            }

            var changed = false;
            foreach (var row in runnable.Indexes())
            {
                var pos = kickWiring.Reference(row, 0);
                var x = fx.Read(store, pos)!.Value;
                var v = fv.Read(store, pos)!.Value;
                var lm = fLandmark.Read(store, pos)!.Value;
                var rest = fRest.Read(store, pos)!.Value;

                var candV = v + kickWeight * (lm - x);
                var kickBlocked = candV < 0 || candV > trackCap;
                var nextV = kickBlocked ? v : candV;
                if (nextV != v) { fv.Write(store, pos, nextV); changed = true; }
                if (kickBlockedPlane >= 0 && store.Read(kickBlockedPlane, pos) != kickBlocked)
                {
                    store.Write(kickBlockedPlane, pos, kickBlocked);
                    changed = true;
                }

                var candX = x + nextV - rest;
                var driftBlocked = candX < 0 || candX > trackCap;
                var nextX = driftBlocked ? x : candX;
                if (nextX != x) { fx.Write(store, pos, nextX); changed = true; }
                if (driftBlockedPlane >= 0 && store.Read(driftBlockedPlane, pos) != driftBlocked)
                {
                    store.Write(driftBlockedPlane, pos, driftBlocked);
                    changed = true;
                }
            }
            return (runnable.Count, 0, changed);
        }
    }

    private static RecognizedLandmarkOscillator? TryRecognizeLandmarkOscillator(Engine engine)
    {
        if (engine.Together || engine.Wirings.Count != 2) return null;
        var (w0, w1) = (engine.Wirings[0], engine.Wirings[1]);
        if (w0.Law is not TransitionLaw t0 || w1.Law is not TransitionLaw t1) return null;
        if (t0.Roles.Count is not (3 or 4) || t1.Roles.Count is not (3 or 4)) return null;
        if (w0.Bindings.Any(b => b.IsOpen || b.Surface is not Store) || w1.Bindings.Any(b => b.IsOpen || b.Surface is not Store))
            return null;
        var st = (Store)w0.Bindings[0].Surface;
        if (w0.Bindings.Any(b => !ReferenceEquals(b.Surface, st)) || w1.Bindings.Any(b => !ReferenceEquals(b.Surface, st)))
            return null;

        var shape = t0.Roles[0];
        if (t0.Roles[1] != shape || t0.Roles[2] != shape || t1.Roles[0] != shape || t1.Roles[1] != shape || t1.Roles[2] != shape)
            return null;

        // Recognize both shear balances strictly from table bits via Recognition.DiscoverBoundBalance!
        var fit0 = Recognition.DiscoverBoundBalance(t0, maxWeight: 6, maxExceptions: 0);
        var fit1 = Recognition.DiscoverBoundBalance(t1, maxWeight: 6, maxExceptions: 0);
        if (fit0 is null || fit1 is null || !fit0.ExactTableMatch || !fit1.ExactTableMatch) return null;
        if (fit0.FreeRole >= 0 || fit1.FreeRole >= 0) return null;
        if (!TryExtractLinearWeights(fit0.Equation, out var wts0) || !TryExtractLinearWeights(fit1.Equation, out var wts1)) return null;
        if (wts0.Length != 4 || wts1.Length != 4) return null;
        if (wts0[3] > 0) for (var i = 0; i < 4; i++) wts0[i] = -wts0[i];
        if (wts1[3] > 0) for (var i = 0; i < 4; i++) wts1[i] = -wts1[i];
        // Expected kick: weights (1, k, -k, -1), reads (0, 1, 2, -1), writes (-1, -1, -1, 0)
        // Expected drift: weights (1, 1, -1, -1), reads (0, 1, 2, -1), writes (-1, -1, -1, 0)
        if (wts0[0] != 1 || wts0[3] != -1 || wts0[1] < 1 || wts0[2] != -wts0[1]) return null;
        if (wts1[0] != 1 || wts1[1] != 1 || wts1[2] != -1 || wts1[3] != -1) return null;
        if (!fit0.Reads.SequenceEqual([0, 1, 2, -1]) || !fit0.Writes.SequenceEqual([-1, -1, -1, 0])) return null;
        if (!fit1.Reads.SequenceEqual([0, 1, 2, -1]) || !fit1.Writes.SequenceEqual([-1, -1, -1, 0])) return null;

        // Wiring coupling: w0=(v, L, x[, b0]), w1=(x, v, R[, b1])
        if (!w0.Bindings[0].Planes.SequenceEqual(w1.Bindings[1].Planes)
            || !w0.Bindings[2].Planes.SequenceEqual(w1.Bindings[0].Planes)) return null;

        // Disjointness of written planes (v, x, optional blocked bits) from each other and from L, R.
        var fvPlanes = w0.Bindings[0].Planes;
        var flPlanes = w0.Bindings[1].Planes;
        var fxPlanes = w0.Bindings[2].Planes;
        var frPlanes = w1.Bindings[2].Planes;
        var kbPlane = fit0.BlockedRole >= 0 ? w0.Bindings[fit0.BlockedRole].Plane(0) : -1;
        var dbPlane = fit1.BlockedRole >= 0 ? w1.Bindings[fit1.BlockedRole].Plane(0) : -1;
        var readSet = new HashSet<int>(flPlanes.Concat(frPlanes));
        var writeSet = new HashSet<int>();
        foreach (var p in fvPlanes.Concat(fxPlanes))
            if (!writeSet.Add(p) || readSet.Contains(p)) return null;
        if (kbPlane >= 0 && (!writeSet.Add(kbPlane) || readSet.Contains(kbPlane))) return null;
        if (dbPlane >= 0 && (!writeSet.Add(dbPlane) || readSet.Contains(dbPlane))) return null;

        return new RecognizedLandmarkOscillator(
            w0, w1, st,
            new Field(shape, [.. fxPlanes]),
            new Field(shape, [.. fvPlanes]),
            new Field(shape, [.. flPlanes]),
            new Field(shape, [.. frPlanes]),
            shape.Size - 1,
            wts0[1],
            kbPlane,
            dbPlane);
    }

    private static bool TryExtractLinearWeights(Polynomial poly, out int[] weights)
    {
        weights = new int[poly.Roles.Count];
        if (poly.Degree != 1) return false;
        foreach (var (weight, exp) in poly.Terms)
        {
            var deg = 0;
            var role = -1;
            for (var r = 0; r < exp.Count; r++)
            {
                deg += exp[r];
                if (exp[r] == 1) role = r;
            }
            if (deg != 1 || role < 0) return false;
            weights[role] = weight;
        }
        return true;
    }

    /// <summary>Recognized sealed multi-rung mixed-partition ripple-carry adder
    /// chain (any combination of combined <c>p ≤ 4</c> or split <c>p ∈ 5..10</c>
    /// rungs verified against <see cref="Numbers.DigitSum"/>). Identity-mapped
    /// binary-packed ladders execute 64 lanes simultaneously via pure bit-sliced
    /// SWAR directly over <see cref="Store.RawPlane"/> words.</summary>
    private sealed class RecognizedLadderAdder
    {
        private readonly Wiring[] wirings;
        private readonly Store store;
        private readonly int[] partitions;
        private readonly Field[] a;
        private readonly Field[] b;
        private readonly Field[] sum;
        private readonly int cinPlane;
        private readonly int coutPlane;
        private readonly int[] carryPlanes;
        private readonly Plane staticRunnable;
        private readonly bool canBitSliceSwar;
        private readonly ulong[]?[] aRaw;
        private readonly bool[] aConst;
        private readonly ulong[]?[] bRaw;
        private readonly bool[] bConst;
        private readonly ulong[]?[] sumRaw;
        private readonly Plane[] sumPlanes;
        private readonly ulong[]? cinRaw;
        private readonly bool cinConst;
        private readonly ulong[]? coutRaw;
        private readonly Plane? coutPlaneObj;
        private readonly ulong[]?[] carryRaw;
        private readonly Plane?[] carryPlaneObjs;
        private readonly int[] rungBits;
        private readonly int[] rungOffset;

        public RecognizedLadderAdder(
            Wiring[] wirings,
            Store store,
            int[] partitions,
            Field[] a,
            Field[] b,
            Field[] sum,
            int cinPlane,
            int coutPlane,
            int[] carryPlanes)
        {
            this.wirings = wirings;
            this.store = store;
            this.partitions = partitions;
            this.a = a;
            this.b = b;
            this.sum = sum;
            this.cinPlane = cinPlane;
            this.coutPlane = coutPlane;
            this.carryPlanes = carryPlanes;

            staticRunnable = wirings[0].Runnable();
            var k = a.Length;
            rungBits = new int[k];
            rungOffset = new int[k];
            var totalBits = 0;
            for (var i = 0; i < k; i++)
            {
                rungOffset[i] = totalBits;
                rungBits[i] = a[i].Planes.Count;
                totalBits += rungBits[i];
            }

            aRaw = new ulong[]?[totalBits];
            aConst = new bool[totalBits];
            bRaw = new ulong[]?[totalBits];
            bConst = new bool[totalBits];
            sumRaw = new ulong[]?[totalBits];
            sumPlanes = new Plane[totalBits];

            var ident = staticRunnable.Any
                && store.Count >= wirings[0].Count
                && !a[0].Shape.IsUnary
                && partitions.All(p => p <= 16);
            if (ident)
            {
                for (var wi = 0; wi < wirings.Length && ident; wi++)
                    if (wirings[wi].Recorded is not null || !wirings[wi].Runnable().Equals(staticRunnable)) ident = false;
            }
            if (ident)
            {
                foreach (var row in staticRunnable.Indexes())
                {
                    for (var wi = 0; wi < wirings.Length && ident; wi++)
                        for (var r = 0; r < wirings[wi].Bindings.Count && ident; r++)
                            if (wirings[wi].Reference(row, r) != row) ident = false;
                    if (!ident) break;
                }
            }

            // Capture raw plane arrays for zero-copy 64-lane SWAR (written planes must never be constant).
            if (ident)
            {
                for (var i = 0; i < k && ident; i++)
                {
                    var off = rungOffset[i];
                    for (var bit = 0; bit < rungBits[i]; bit++)
                    {
                        var ap = a[i].Planes[bit];
                        aRaw[off + bit] = store.RawPlane(ap)?.RawWords;
                        aConst[off + bit] = store.IsConstantPlane(ap) && store.Read(ap, 0);
                        var bp = b[i].Planes[bit];
                        bRaw[off + bit] = store.RawPlane(bp)?.RawWords;
                        bConst[off + bit] = store.IsConstantPlane(bp) && store.Read(bp, 0);
                        var sp = sum[i].Planes[bit];
                        if (store.RawPlane(sp) is not { } spObj) { ident = false; break; }
                        sumPlanes[off + bit] = spObj;
                        sumRaw[off + bit] = spObj.RawWords;
                    }
                }
            }

            cinRaw = store.RawPlane(cinPlane)?.RawWords;
            cinConst = store.IsConstantPlane(cinPlane) && store.Read(cinPlane, 0);
            coutPlaneObj = store.RawPlane(coutPlane);
            coutRaw = coutPlaneObj?.RawWords;
            carryPlaneObjs = new Plane?[carryPlanes.Length];
            carryRaw = new ulong[]?[carryPlanes.Length];
            for (var i = 0; i < carryPlanes.Length; i++)
            {
                carryPlaneObjs[i] = store.RawPlane(carryPlanes[i]);
                carryRaw[i] = carryPlaneObjs[i]?.RawWords;
                if (carryRaw[i] is null) ident = false;
            }
            if (coutRaw is null) ident = false;
            canBitSliceSwar = ident;
        }

        public bool CanRunFast()
        {
            if (canBitSliceSwar) return true;
            var r0 = wirings[0].Runnable();
            if (r0.None) return false;
            for (var i = 0; i < wirings.Length; i++)
                if (wirings[i].Recorded is not null || !wirings[i].Runnable().Equals(r0)) return false;
            return true;
        }

        public (int Fast, int Deopt, bool Changed) StepFast()
        {
            if (canBitSliceSwar) return StepBitSlicedSwar();

            var runnable = wirings[0].Runnable();
            var k = a.Length;
            var seenPos = new HashSet<int>();
            foreach (var row in runnable.Indexes())
            {
                var pos = wirings[0].Reference(row, 0);
                if (!store.IsLive(pos) || !seenPos.Add(pos)) return (0, runnable.Count, false);
                for (var wi = 0; wi < wirings.Length; wi++)
                    for (var r = 0; r < wirings[wi].Bindings.Count; r++)
                        if (wirings[wi].Reference(row, r) != pos) return (0, runnable.Count, false);
                for (var i = 0; i < k; i++)
                    if (a[i].Read(store, pos) is null || b[i].Read(store, pos) is null || sum[i].Read(store, pos) is null)
                        return (0, runnable.Count, false);
            }

            var changed = false;
            foreach (var row in runnable.Indexes())
            {
                var pos = wirings[0].Reference(row, 0);
                var carry = store.Read(cinPlane, pos) ? 1 : 0;
                for (var i = 0; i < k; i++)
                {
                    var p = partitions[i];
                    var rungTotal = a[i].Read(store, pos)!.Value + b[i].Read(store, pos)!.Value + carry;
                    var nextCarry = rungTotal >= p ? 1 : 0;
                    var nextDigit = nextCarry != 0 ? rungTotal - p : rungTotal;
                    if (sum[i].Read(store, pos)!.Value != nextDigit) { sum[i].Write(store, pos, nextDigit); changed = true; }
                    carry = nextCarry;
                    if (i < carryPlanes.Length)
                    {
                        var cb = carry != 0;
                        if (store.Read(carryPlanes[i], pos) != cb) { store.Write(carryPlanes[i], pos, cb); changed = true; }
                    }
                }
                var nextCout = carry != 0;
                if (store.Read(coutPlane, pos) != nextCout) { store.Write(coutPlane, pos, nextCout); changed = true; }
            }
            return (runnable.Count, 0, changed);
        }

        private (int Fast, int Deopt, bool Changed) StepBitSlicedSwar()
        {
            var runWords = staticRunnable.RawWords;
            var liveWords = store.RawLive.RawWords;
            var wordCount = runWords.Length;
            var k = partitions.Length;

            // Pass 1: validate live coverage and binary digit code validity across all 64-lane words before writing.
            for (var w = 0; w < wordCount; w++)
            {
                var active = runWords[w];
                if (active == 0) continue;
                if ((active & ~liveWords[w]) != 0) return (0, staticRunnable.Count, false);

                for (var i = 0; i < k; i++)
                {
                    var p = partitions[i];
                    var bits = rungBits[i];
                    if (p == (1 << bits)) continue; // power-of-two partition: every bit pattern is a valid code
                    var off = rungOffset[i];
                    ulong a0 = ReadWord(aRaw, aConst, off, w), a1 = bits > 1 ? ReadWord(aRaw, aConst, off + 1, w) : 0UL;
                    ulong a2 = bits > 2 ? ReadWord(aRaw, aConst, off + 2, w) : 0UL, a3 = bits > 3 ? ReadWord(aRaw, aConst, off + 3, w) : 0UL;
                    if ((Ge4(a0, a1, a2, a3, p) & active) != 0) return (0, staticRunnable.Count, false);

                    ulong b0 = ReadWord(bRaw, bConst, off, w), b1 = bits > 1 ? ReadWord(bRaw, bConst, off + 1, w) : 0UL;
                    ulong b2 = bits > 2 ? ReadWord(bRaw, bConst, off + 2, w) : 0UL, b3 = bits > 3 ? ReadWord(bRaw, bConst, off + 3, w) : 0UL;
                    if ((Ge4(b0, b1, b2, b3, p) & active) != 0) return (0, staticRunnable.Count, false);

                    ulong s0 = sumRaw[off]![w], s1 = bits > 1 ? sumRaw[off + 1]![w] : 0UL;
                    ulong s2 = bits > 2 ? sumRaw[off + 2]![w] : 0UL, s3 = bits > 3 ? sumRaw[off + 3]![w] : 0UL;
                    if ((Ge4(s0, s1, s2, s3, p) & active) != 0) return (0, staticRunnable.Count, false);
                }
            }

            // Pass 2: 64-lane bit-sliced mixed-partition ripple addition directly over plane words.
            var changed = false;
            var cinFallback = cinConst ? ~0UL : 0UL;
            for (var w = 0; w < wordCount; w++)
            {
                var active = runWords[w];
                if (active == 0) continue;
                var invActive = ~active;
                var carry = cinRaw is not null ? cinRaw[w] : cinFallback;

                for (var i = 0; i < k; i++)
                {
                    var p = partitions[i];
                    var bits = rungBits[i];
                    var off = rungOffset[i];

                    ulong a0 = ReadWord(aRaw, aConst, off, w);
                    ulong a1 = bits > 1 ? ReadWord(aRaw, aConst, off + 1, w) : 0UL;
                    ulong a2 = bits > 2 ? ReadWord(aRaw, aConst, off + 2, w) : 0UL;
                    ulong a3 = bits > 3 ? ReadWord(aRaw, aConst, off + 3, w) : 0UL;

                    ulong b0 = ReadWord(bRaw, bConst, off, w);
                    ulong b1 = bits > 1 ? ReadWord(bRaw, bConst, off + 1, w) : 0UL;
                    ulong b2 = bits > 2 ? ReadWord(bRaw, bConst, off + 2, w) : 0UL;
                    ulong b3 = bits > 3 ? ReadWord(bRaw, bConst, off + 3, w) : 0UL;

                    // 5-bit bit-sliced binary sum s = a + b + carry (s in 0..2p-1 <= 31).
                    ulong s0 = a0 ^ b0 ^ carry;
                    ulong c0 = (a0 & b0) | (carry & (a0 ^ b0));
                    ulong s1 = a1 ^ b1 ^ c0;
                    ulong c1 = (a1 & b1) | (c0 & (a1 ^ b1));
                    ulong s2 = a2 ^ b2 ^ c1;
                    ulong c2 = (a2 & b2) | (c1 & (a2 ^ b2));
                    ulong s3 = a3 ^ b3 ^ c2;
                    ulong s4 = (a3 & b3) | (c2 & (a3 ^ b3));

                    ulong cout;
                    ulong r0, r1, r2, r3;
                    if (p == (1 << bits))
                    {
                        cout = bits == 1 ? c0 : bits == 2 ? c1 : bits == 3 ? c2 : s4;
                        r0 = s0; r1 = s1; r2 = s2; r3 = s3;
                    }
                    else
                    {
                        // Exact 5-bit bit-sliced threshold + subtraction: t = s + (32 - p).
                        // Carry out of bit 4 is 1 iff s >= p, and lower 4 bits of t equal s - p!
                        var addK = 32 - p;
                        ulong k0 = (addK & 1) != 0 ? ~0UL : 0UL;
                        ulong k1 = (addK & 2) != 0 ? ~0UL : 0UL;
                        ulong k2 = (addK & 4) != 0 ? ~0UL : 0UL;
                        ulong k3 = (addK & 8) != 0 ? ~0UL : 0UL;
                        ulong k4 = (addK & 16) != 0 ? ~0UL : 0UL;

                        ulong t0 = s0 ^ k0;
                        ulong tc0 = s0 & k0;
                        ulong t1 = s1 ^ k1 ^ tc0;
                        ulong tc1 = (s1 & k1) | (tc0 & (s1 ^ k1));
                        ulong t2 = s2 ^ k2 ^ tc1;
                        ulong tc2 = (s2 & k2) | (tc1 & (s2 ^ k2));
                        ulong t3 = s3 ^ k3 ^ tc2;
                        ulong tc3 = (s3 & k3) | (tc2 & (s3 ^ k3));
                        cout = (s4 & k4) | (tc3 & (s4 ^ k4));

                        var invCout = ~cout;
                        r0 = (cout & t0) | (invCout & s0);
                        r1 = (cout & t1) | (invCout & s1);
                        r2 = (cout & t2) | (invCout & s2);
                        r3 = (cout & t3) | (invCout & s3);
                    }

                    // Write sum digit planes (only touching active lanes, tracking exact changed flag).
                    WriteWord(sumRaw[off]!, w, r0, active, invActive, ref changed);
                    if (bits > 1) WriteWord(sumRaw[off + 1]!, w, r1, active, invActive, ref changed);
                    if (bits > 2) WriteWord(sumRaw[off + 2]!, w, r2, active, invActive, ref changed);
                    if (bits > 3) WriteWord(sumRaw[off + 3]!, w, r3, active, invActive, ref changed);

                    carry = cout;
                    if (i < carryRaw.Length) WriteWord(carryRaw[i]!, w, carry, active, invActive, ref changed);
                }
                WriteWord(coutRaw!, w, carry, active, invActive, ref changed);
            }
            return (staticRunnable.Count, 0, changed);
        }

        private static ulong ReadWord(ulong[]?[] raw, bool[] constVal, int idx, int w) =>
            raw[idx] is { } arr ? arr[w] : (constVal[idx] ? ~0UL : 0UL);

        private static void WriteWord(ulong[] dst, int w, ulong value, ulong active, ulong invActive, ref bool changed)
        {
            var old = dst[w];
            var next = (old & invActive) | (value & active);
            if (next != old) { dst[w] = next; changed = true; }
        }

        private static ulong Ge4(ulong v0, ulong v1, ulong v2, ulong v3, int p)
        {
            var k = 16 - p;
            ulong k0 = (k & 1) != 0 ? ~0UL : 0UL;
            ulong k1 = (k & 2) != 0 ? ~0UL : 0UL;
            ulong k2 = (k & 4) != 0 ? ~0UL : 0UL;
            ulong k3 = (k & 8) != 0 ? ~0UL : 0UL;
            ulong c0 = v0 & k0;
            ulong c1 = (v1 & k1) | (c0 & (v1 ^ k1));
            ulong c2 = (v2 & k2) | (c1 & (v2 ^ k2));
            return (v3 & k3) | (c2 & (v3 ^ k3));
        }
    }

    private static RecognizedLadderAdder? TryRecognizeLadderAdder(Engine engine)
    {
        if (engine.Together || engine.Wirings.Count < 2) return null;

        var wirings = new List<Wiring>(engine.Wirings.Count);
        var partitions = new List<int>();
        var aList = new List<Field>();
        var bList = new List<Field>();
        var sumList = new List<Field>();
        var carries = new List<int>();
        Store? store = null;
        var cinPlane = -1;
        var lastCoutPlane = -1;

        var idx = 0;
        while (idx < engine.Wirings.Count)
        {
            var w0 = engine.Wirings[idx];
            if (w0.Law is not TransitionLaw t0 || w0.Bindings.Any(bind => bind.IsOpen || bind.Surface is not Store))
                return null;
            var s0 = (Store)w0.Bindings[0].Surface;
            if (store is null) store = s0; else if (!ReferenceEquals(store, s0)) return null;
            if (w0.Bindings.Any(bind => !ReferenceEquals(bind.Surface, store))) return null;

            int p, rungCin, rungCout;
            Field fa, fb, fs;

            if (TryRecognizeDigitAddStep(t0, out var combP))
            {
                p = combP;
                rungCin = w0.Bindings[2].Plane(0);
                rungCout = w0.Bindings[4].Plane(0);
                fa = new Field(t0.Roles[0], [.. w0.Bindings[0].Planes]);
                fb = new Field(t0.Roles[1], [.. w0.Bindings[1].Planes]);
                fs = new Field(t0.Roles[3], [.. w0.Bindings[3].Planes]);
                wirings.Add(w0);
                idx++;
            }
            else if (idx + 1 < engine.Wirings.Count
                && engine.Wirings[idx + 1] is var w1
                && w1.Law is TransitionLaw t1
                && t0.Roles.Count == 4 && t1.Roles.Count == 4
                && !w1.Bindings.Any(bind => bind.IsOpen || !ReferenceEquals(bind.Surface, store))
                && TryRecognizeSplitDigitPair(t0, t1, out var splitP))
            {
                if (!w0.Bindings[0].Planes.SequenceEqual(w1.Bindings[0].Planes)
                    || !w0.Bindings[1].Planes.SequenceEqual(w1.Bindings[1].Planes)
                    || w0.Bindings[2].Plane(0) != w1.Bindings[2].Plane(0))
                    return null;
                p = splitP;
                rungCin = w0.Bindings[2].Plane(0);
                rungCout = w1.Bindings[3].Plane(0);
                fa = new Field(t0.Roles[0], [.. w0.Bindings[0].Planes]);
                fb = new Field(t0.Roles[1], [.. w0.Bindings[1].Planes]);
                fs = new Field(t0.Roles[3], [.. w0.Bindings[3].Planes]);
                wirings.Add(w0);
                wirings.Add(w1);
                idx += 2;
            }
            else
            {
                return null;
            }

            if (partitions.Count == 0) cinPlane = rungCin;
            else
            {
                if (rungCin != lastCoutPlane) return null;
                carries.Add(rungCin);
            }
            lastCoutPlane = rungCout;
            partitions.Add(p);
            aList.Add(fa);
            bList.Add(fb);
            sumList.Add(fs);
        }

        if (partitions.Count < 2) return null;
        // All rungs must share unary vs binary digit shape for clean uniform handling.
        if (aList.Any(f => f.Shape.IsUnary != aList[0].Shape.IsUnary)) return null;

        return BuildVerifiedLadderAdder(
            [.. wirings], store!, [.. partitions], [.. aList], [.. bList], [.. sumList],
            cinPlane, lastCoutPlane, [.. carries]);
    }

    /// <summary>Verifies a split (DigitSumStep, DigitCarryStep) pair directly
    /// against the canonical <see cref="Numbers.DigitSum"/> polynomial via
    /// <see cref="Recognition.Compare"/>.</summary>
    private static bool TryRecognizeSplitDigitPair(TransitionLaw tSum, TransitionLaw tCar, out int partition)
    {
        partition = 0;
        var d = tSum.Roles[0];
        var p = d.Size;
        if (p is < 2 or > 10 || !tSum.IsFunctional || !tCar.IsFunctional) return false;
        var c = tSum.Roles[2];
        if (c.Size != 2 || tSum.Roles[1] != d || tSum.Roles[3] != d) return false;
        if (tCar.Roles[0] != d || tCar.Roles[1] != d || tCar.Roles[2] != c || tCar.Roles[3] != c) return false;
        var col = Polynomial.Balance([d, d, c, c, d], 1, 1, 1, -p, -1);
        if (!Recognition.Compare(tSum, col, [0, 1, 2, -1, -1], [-1, -1, -1, -1, 3]).Same) return false;
        if (!Recognition.Compare(tCar, col, [0, 1, 2, -1, -1], [-1, -1, -1, 3, -1]).Same) return false;
        partition = p;
        return true;
    }

    private static RecognizedLadderAdder? BuildVerifiedLadderAdder(
        Wiring[] wirings,
        Store store,
        int[] partitions,
        Field[] a,
        Field[] b,
        Field[] sum,
        int cinPlane,
        int coutPlane,
        int[] carries)
    {
        var k = a.Length;
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

        return new RecognizedLadderAdder(wirings, store, partitions, a, b, sum, cinPlane, coutPlane, carries);
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
