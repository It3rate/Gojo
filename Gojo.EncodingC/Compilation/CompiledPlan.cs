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
            // SIMD main loop: Vector<ulong>.Count * 64 lanes per iteration (NEON 128b / AVX2 256b / AVX-512 512b)
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

            // Scalar tail for remaining < vecWidth words
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

        // Preserve unchanged lanes via SIMD AndNot/Or
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
/// recognizes packet conversions, radix-B ripple adders, kick-drift
/// oscillators, and SIMD bit-sliced laws strictly from table bits, accounting,
/// and topology — never from names or opcode enums.</summary>
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

    /// <summary>True iff a functional bounded multiplier with explicit T+
    /// overflow was recognized strictly from its table bits.</summary>
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
        // Exhaustively verify every single cell of the table against exact packet firing!
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
    /// functional bounded multiplier <c>(a [Unchanged], b [Unchanged], value,
    /// overflow/T+)</c> strictly from every cell of its table bits.</summary>
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
        for (var a = 0; a < aSize; a++)
            for (var b = 0; b < bSize; b++)
            {
                var prod = (ulong)a * (ulong)b;
                var expVal = prod <= (ulong)candProdCap ? (int)prod : candProdCap;
                var rem = prod > (ulong)candProdCap ? prod - (ulong)candProdCap : 0UL;
                var expOvf = rem <= (ulong)candOvfCap ? (int)rem : candOvfCap;
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
                    // Exhaustively check every prior (sum, cout) cell so the table is 100% verified.
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
    public CompiledStepReport RunStep()
    {
        if (!IsCurrent)
        {
            engine.Advance();
            return new CompiledStepReport(UsedFastPath: false, Stale: true, FastLanes: 0, DeoptLanes: TotalLiveRows());
        }

        // Simultaneous Together engines defer to canonical contention resolution.
        if (engine.Together)
        {
            engine.StepTogether();
            return new CompiledStepReport(UsedFastPath: false, Stale: false, FastLanes: 0, DeoptLanes: TotalLiveRows());
        }

        // Regime 1 fast path for recognized declared-order kick-drift oscillator!
        if (oscillator is { } osc && osc.CanRunFast())
        {
            var (fast, deopt) = osc.StepFast(engine);
            if (deopt == 0)
            {
                engine.AdvanceTick();
                return new CompiledStepReport(UsedFastPath: true, Stale: false, FastLanes: fast, DeoptLanes: 0);
            }
        }

        // Regime 1 widening ALU fast path for recognized multi-rung radix-B ripple adder!
        if (ladderAdder is { } adder && adder.CanRunFast())
        {
            var (fast, deopt) = adder.StepFast(engine);
            if (deopt == 0)
            {
                engine.AdvanceTick();
                return new CompiledStepReport(UsedFastPath: true, Stale: false, FastLanes: fast, DeoptLanes: 0);
            }
        }

        var fastTotal = 0;
        var deoptTotal = 0;
        var anyFast = false;
        for (var i = 0; i < compiled.Length; i++)
        {
            var cw = compiled[i];
            if (!cw.CanAccelerate)
            {
                cw.Wiring.Apply();
                deoptTotal += cw.Wiring.Live.Count;
                continue;
            }
            var (fast, deopt) = cw.StepGuarded();
            fastTotal += fast;
            deoptTotal += deopt;
            if (fast > 0) anyFast = true;
        }
        engine.AdvanceTick();
        return new CompiledStepReport(UsedFastPath: anyFast, Stale: false, FastLanes: fastTotal, DeoptLanes: deoptTotal);
    }

    /// <summary>Packet-exact bulk settle fast path for a single uncontested
    /// packet-conversion wiring: transfers exact whole packets
    /// <c>p = min(src/take, room/give)</c> (NEVER bulk <c>(src*give)/take</c>),
    /// falling back to canonical settle for anything else.</summary>
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
            var before = SnapshotStores();
            var rep = RunStep();
            totalFast += rep.FastLanes;
            totalDeopt += rep.DeoptLanes;
            if (rep.UsedFastPath) anyFast = true;
            if (StoresEqual(before)) break;
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

        // Compute canonical Regime 1 discrete shadow reference over 0..ticks on a clean scratch state.
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

    private Plane[][] SnapshotStores() =>
        [.. engine.Stores.Select(s => Enumerable.Range(0, s.PlaneCount).Select(p => s.Read(p).Clone()).ToArray())];

    private bool StoresEqual(Plane[][] before)
    {
        for (var s = 0; s < engine.Stores.Count; s++)
            for (var p = 0; p < engine.Stores[s].PlaneCount; p++)
                if (!engine.Stores[s].Read(p).Equals(before[s][p])) return false;
        return true;
    }

    private enum KernelKind { Canonical, SimdBitSliced, PacketConvert, BoundedMultiply }

    private sealed class CompiledWiring
    {
        public CompiledWiring(Wiring wiring, KernelKind kind, int param0, int param1)
        {
            Wiring = wiring;
            Kind = kind;
            Param0 = param0;
            Param1 = param1;
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

        public (int Fast, int Deopt) StepGuarded()
        {
            var law = (TransitionLaw)Wiring.Law;
            var runnable = Wiring.Runnable();
            if (runnable.None) return (0, 0);

            // Group contention guard: if any two runnable rows write the same bit
            // OR any runnable row aliases two roles onto the same bit, deopt the entire wiring
            // to canonical Wiring.Apply()!
            var writtenBits = new Dictionary<(Surface, int, int), int>();
            var contestedRows = new Plane(Wiring.Count);
            var aliasedRows = new Plane(Wiring.Count);
            foreach (var row in runnable.Indexes())
            {
                // Aliasing check within one row (any two roles sharing a bit)
                var rowBits = new HashSet<(Surface, int, int)>();
                var aliased = false;
                for (var r = 0; r < Wiring.Bindings.Count && !aliased; r++)
                {
                    var b = Wiring.Bindings[r];
                    var pos = Wiring.Reference(row, r);
                    for (var p = 0; p < b.Planes.Count; p++)
                        if (!rowBits.Add(b.Surface.Bit(pos, b.Plane(p)))) { aliased = true; break; }
                }
                if (aliased) { aliasedRows[row] = true; continue; }

                // Shared-bit group contention check across rows of this wiring
                for (var r = 0; r < Wiring.Bindings.Count; r++)
                {
                    if (!Wiring.Writes(r)) continue;
                    var b = Wiring.Bindings[r];
                    var pos = Wiring.Reference(row, r);
                    for (var p = 0; p < b.Planes.Count; p++)
                    {
                        var bit = b.Surface.Bit(pos, b.Plane(p));
                        if (writtenBits.TryGetValue(bit, out var first))
                        {
                            contestedRows[first] = true;
                            contestedRows[row] = true;
                        }
                        else writtenBits[bit] = row;
                    }
                }
            }

            // Also check valid codes per runnable row so malformed codes (e.g. unary 010) deopt cleanly.
            var invalidCodeRows = new Plane(Wiring.Count);
            foreach (var row in runnable.Indexes())
            {
                if (aliasedRows[row] || contestedRows[row]) continue;
                for (var r = 0; r < Wiring.Bindings.Count; r++)
                {
                    var b = Wiring.Bindings[r];
                    var store = (Store)b.Surface;
                    var pos = Wiring.Reference(row, r);
                    if (!store.IsLive(pos)) { invalidCodeRows[row] = true; break; }
                    ulong pat = 0;
                    for (var k = 0; k < b.Planes.Count; k++)
                        if (store.Read(b.Plane(k), pos)) pat |= 1UL << k;
                    if (law.Roles[r].Decode(pat) is null) { invalidCodeRows[row] = true; break; }
                }
            }

            var deoptMask = aliasedRows.Or(contestedRows).Or(invalidCodeRows);
            if (deoptMask.Any)
            {
                // Run canonical Apply() when contested/aliased/malformed lanes exist so sequential order & evidence stay exact.
                Wiring.Apply();
                return (0, runnable.Count);
            }

            var fastCount = runnable.Count;
            if (Kind == KernelKind.PacketConvert)
            {
                var srcBind = Wiring.Bindings[0];
                var dstBind = Wiring.Bindings[1];
                var srcStore = (Store)srcBind.Surface;
                var dstStore = (Store)dstBind.Surface;
                var srcField = new Field(law.Roles[0], [.. srcBind.Planes]);
                var dstField = new Field(law.Roles[1], [.. dstBind.Planes]);
                var dstCap = law.Roles[1].Size - 1;
                foreach (var row in runnable.Indexes())
                {
                    var sp = Wiring.Reference(row, 0);
                    var dp = Wiring.Reference(row, 1);
                    var s = srcField.Read(srcStore, sp)!.Value;
                    var d = dstField.Read(dstStore, dp)!.Value;
                    if (s >= Take && d + Give <= dstCap)
                    {
                        srcField.Write(srcStore, sp, s - Take);
                        dstField.Write(dstStore, dp, d + Give);
                    }
                }
                return (fastCount, 0);
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
                var ovfCap = (ulong)OverflowCap;
                foreach (var row in runnable.Indexes())
                {
                    var a = (ulong)aField.Read(aStore, Wiring.Reference(row, 0))!.Value;
                    var b = (ulong)bField.Read(bStore, Wiring.Reference(row, 1))!.Value;
                    // Native widening integer MUL (never silently wraps): excess lands directly in T+ overflow!
                    var prod = a * b;
                    var val = prod <= cap ? (int)prod : ProdCap;
                    var rem = prod > cap ? prod - cap : 0UL;
                    var ovf = rem <= ovfCap ? (int)rem : OverflowCap;
                    vField.Write(vStore, Wiring.Reference(row, 2), val);
                    oField.Write(oStore, Wiring.Reference(row, 3), ovf);
                }
                return (fastCount, 0);
            }

            // General SIMD bit-sliced fast path across aligned identity-mapped lanes, or packed lane buffers!
            var codes = new Plane[law.Roles.Count][];
            var identity = IsIdentityMapped(Wiring, runnable, out var sharedLen);
            if (identity)
            {
                for (var r = 0; r < law.Roles.Count; r++)
                {
                    var b = Wiring.Bindings[r];
                    var store = (Store)b.Surface;
                    codes[r] = [.. b.Planes.Select(p => store.Read(p))];
                }
                var laneMask = new Plane(sharedLen);
                foreach (var row in runnable.Indexes()) laneMask[row] = true;
                var step = CompiledSimd.StepTransition(law, codes, laneMask);
                for (var r = 0; r < law.Roles.Count; r++)
                {
                    if (!Wiring.Writes(r)) continue;
                    var b = Wiring.Bindings[r];
                    var store = (Store)b.Surface;
                    for (var k = 0; k < b.Planes.Count; k++)
                    {
                        var planeIdx = b.Plane(k);
                        var srcWords = step.After[r][k].RawWords;
                        foreach (var row in runnable.Indexes())
                            store.Write(planeIdx, row, ((srcWords[row >> 6] >> (row & 63)) & 1) != 0);
                    }
                }
                return (fastCount, 0);
            }

            // Packed SIMD buffer for non-identity row mappings: pack runnable rows into contiguous Planes -> SIMD -> unpack.
            var activeRows = runnable.Indexes().ToArray();
            var n = activeRows.Length;
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
                        store.Write(pIdx, Wiring.Reference(activeRows[idx], r), simdStep.After[r][k][idx]);
                }
            }
            return (fastCount, 0);
        }

        public (int Fast, int Deopt, int Ticks) SettlePacketConvertExact(int limit)
        {
            var law = (TransitionLaw)Wiring.Law;
            var runnable = Wiring.Runnable();
            if (runnable.None) return (0, 0, limit > 0 ? 1 : 0);

            var srcBind = Wiring.Bindings[0];
            var dstBind = Wiring.Bindings[1];
            var srcStore = (Store)srcBind.Surface;
            var dstStore = (Store)dstBind.Surface;
            var srcField = new Field(law.Roles[0], [.. srcBind.Planes]);
            var dstField = new Field(law.Roles[1], [.. dstBind.Planes]);

            // Guard against bit aliasing, cross-row group contention, retired lanes, or malformed codes.
            var seenBits = new HashSet<(Surface, int, int)>();
            foreach (var row in runnable.Indexes())
            {
                var sp = Wiring.Reference(row, 0);
                var dp = Wiring.Reference(row, 1);
                if (!srcStore.IsLive(sp) || !dstStore.IsLive(dp)) return (0, runnable.Count, 0);
                for (var k = 0; k < srcBind.Planes.Count; k++)
                    if (!seenBits.Add(srcBind.Surface.Bit(sp, srcBind.Plane(k)))) return (0, runnable.Count, 0);
                for (var k = 0; k < dstBind.Planes.Count; k++)
                    if (!seenBits.Add(dstBind.Surface.Bit(dp, dstBind.Plane(k)))) return (0, runnable.Count, 0);
                if (srcField.Read(srcStore, sp) is null || dstField.Read(dstStore, dp) is null) return (0, runnable.Count, 0);
            }

            var dstCap = law.Roles[1].Size - 1;
            var maxPackets = 0;
            foreach (var row in runnable.Indexes())
            {
                var sp = Wiring.Reference(row, 0);
                var dp = Wiring.Reference(row, 1);
                var s = srcField.Read(srcStore, sp)!.Value;
                var d = dstField.Read(dstStore, dp)!.Value;
                var room = dstCap - d;
                // Packet-exact: p = min(s / take, room / give, limit) — NEVER bulk (s * give) / take!
                var bySrc = s / Take;
                var byRoom = room / Give;
                var packets = bySrc < byRoom ? bySrc : byRoom;
                if (packets > limit) packets = limit;
                if (packets > maxPackets) maxPackets = packets;
                if (packets > 0)
                {
                    srcField.Write(srcStore, sp, s - packets * Take);
                    dstField.Write(dstStore, dp, d + packets * Give);
                }
            }
            var ticks = maxPackets < limit ? maxPackets + 1 : limit;
            return (runnable.Count, 0, ticks);
        }

        private static bool IsIdentityMapped(Wiring wiring, Plane runnable, out int sharedLength)
        {
            sharedLength = ((Store)wiring.Bindings[0].Surface).Count;
            for (var r = 1; r < wiring.Bindings.Count; r++)
                if (((Store)wiring.Bindings[r].Surface).Count != sharedLength) return false;
            if (wiring.Count > sharedLength) return false;
            foreach (var row in runnable.Indexes())
                for (var r = 0; r < wiring.Bindings.Count; r++)
                    if (wiring.Reference(row, r) != row) return false;
            return true;
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

        public (int Fast, int Deopt) StepFast(Engine engine)
        {
            var runnable = kickWiring.Runnable();
            // Verify every lane is unaliased, live, valid code, and stays in range (else deopt to canonical!).
            var seen = new HashSet<int>();
            foreach (var row in runnable.Indexes())
            {
                var px = kickWiring.Reference(row, 0);
                var pv = kickWiring.Reference(row, 1);
                if (px != driftWiring.Reference(row, 1) || pv != driftWiring.Reference(row, 0)) return (0, runnable.Count);
                if (!store.IsLive(px) || !store.IsLive(pv) || !seen.Add(px)) return (0, runnable.Count);
                var rx = fx.Read(store, px);
                var rv = fv.Read(store, pv);
                if (rx is null || rv is null) return (0, runnable.Count);
                var x = rx.Value - maxAbs;
                var v = rv.Value - maxAbs;
                var nv = v - feedbackSign * Dynamics.Coarse(x, shift);
                if (!clamps && (nv < -maxAbs || nv > maxAbs)) return (0, runnable.Count);
                var clampedV = nv < -maxAbs ? -maxAbs : nv > maxAbs ? maxAbs : nv;
                var nx = x + Dynamics.Coarse(clampedV, shift);
                if (!clamps && (nx < -maxAbs || nx > maxAbs)) return (0, runnable.Count);
            }

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
                fv.Write(store, pv, nv + maxAbs);
                fx.Write(store, px, nx + maxAbs);
            }
            return (runnable.Count, 0);
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

        public (int Fast, int Deopt) StepFast(Engine engine)
        {
            var runnable = wirings[0].Runnable();
            var k = wirings.Length;
            ulong modulus = 1;
            for (var i = 0; i < k; i++) modulus *= (ulong)radix;

            var seenPos = new HashSet<int>();
            foreach (var row in runnable.Indexes())
            {
                var pos = wirings[0].Reference(row, 0);
                if (!store.IsLive(pos) || !seenPos.Add(pos)) return (0, runnable.Count);
                for (var i = 0; i < k; i++)
                {
                    for (var r = 0; r < 5; r++)
                        if (wirings[i].Reference(row, r) != pos) return (0, runnable.Count);
                    if (a[i].Read(store, pos) is null || b[i].Read(store, pos) is null || sum[i].Read(store, pos) is null)
                        return (0, runnable.Count);
                }
            }

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
                // Native widening integer ADD (never wraps!): exact quotient -> cout, digits -> sum rungs & intermediate carries!
                var total = va + vb + cin;
                ulong carry = cin;
                for (var i = 0; i < k; i++)
                {
                    var da = (ulong)a[i].Read(store, pos)!.Value;
                    var db = (ulong)b[i].Read(store, pos)!.Value;
                    var rungTotal = da + db + carry;
                    sum[i].Write(store, pos, (int)(rungTotal % (ulong)radix));
                    carry = rungTotal / (ulong)radix;
                    if (i < carryPlanes.Length) store.Write(carryPlanes[i], pos, carry != 0);
                }
                store.Write(coutPlane, pos, total >= modulus);
            }
            return (runnable.Count, 0);
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
            // Verify cout of rung i-1 chains directly into cin of rung i
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

        // Guard against ulong overflow in va + vb + cin.
        ulong modulus = 1;
        for (var i = 0; i < k; i++)
        {
            if (modulus > (ulong.MaxValue / 2) / (ulong)radix) return null;
            modulus *= (ulong)radix;
        }

        // Guard bit-disjointness: all written planes (sum rungs, carries, cout) must be pairwise distinct
        // and disjoint from all read input planes (a, b, cin) so sequential writes never alias inputs.
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
            if (recorded != (other.Recorded is not null) || !live.Equals(other.Live)) return false;
            if ((attention is null) != (other.Attention is null)) return false;
            if (attention is not null && !attention.Equals(other.Rows.Read(other.Attention!.Value))) return false;
            for (var r = 0; r < bindings.Length; r++)
            {
                if (!ReferenceEquals(bindings[r].Surface, other.Bindings[r].Surface)) return false;
                if (bindings[r].Surface is Socket sock && !ReferenceEquals(bindings[r].Plugged, sock.Plugged)) return false;
                var rp = other.ReferencePlanes(r);
                if (rp.Count != refPlanes[r].Length) return false;
                for (var k = 0; k < rp.Count; k++)
                    if (!refPlanes[r][k].Equals(other.Rows.Read(rp[k]))) return false;
            }
            return true;
        }
    }
}
