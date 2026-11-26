using System.Numerics;

namespace Gojo.EncodingC;

/// <summary>Outcome of one derived compiled step or settle call: reports
/// whether any compiled kernel ran, whether staleness forced fallback to
/// <see cref="Engine"/>, and how many lanes took a compiled path versus
/// falling back.</summary>
public sealed record CompiledStepReport(bool UsedFastPath, bool Stale, int FastLanes, int DeoptLanes);

/// <summary>Optional, derived, epoch-guarded compiled execution backend for an
/// <see cref="Engine"/> (Gojo/docs/GojoOperationsSynthesis-2026-11.md, §7).
/// Never saved by <see cref="Transport"/> and adds no enums, tags, strings,
/// real numbers, or <c>System.Math</c> calls.
/// <para>Every compiled kernel qualifies strictly from table bits, accounting,
/// bindings, layout, and schedule — never from a name:
/// <list type="bullet">
/// <item><b>Relational column &amp; ladder adders:</b> Pass-1
/// <see cref="Ladder.Add"/> chains whose column tables match
/// <see cref="Numbers.Column"/> via <see cref="Law.SameTable"/>, running
/// 64-lane bit-sliced SWAR on settled words and exact 64-lane bitwise
/// factorized narrowing on open, backward, or contradictory words.</item>
/// <item><b>Concrete multi-rung ripple adders:</b> combined or split
/// <see cref="Numbers.DigitSum"/> transitions verified via
/// <see cref="Recognition.Compare"/> / <see cref="Law.SameTable"/>, running
/// 64-lane bit-sliced SWAR across arbitrary mixed partitions up to 16.</item>
/// <item><b>Packet conversions:</b> unary <see cref="Flows.Convert"/> laws
/// verified via <see cref="PreservedSum"/> accounting and
/// <see cref="Law.SameTable"/>, running AVX2/SIMD unary plane shifts and
/// exact bounded multi-step settling.</item>
/// <item><b>Bounded lossless multipliers:</b> functional
/// <c>a · b = val + ovf</c> laws verified via <see cref="Law.SameTable"/>.</item>
/// <item><b>General small-table transitions:</b> identity-mapped functional
/// transitions lowered from their own table bits to L1-resident SIMD
/// XOR-flip loops.</item>
/// </list>
/// Every ineligible case — open bindings with shared references, ports,
/// sockets, workspaces, recorded wirings, self-rewiring topology, broken codes,
/// multi-wiring simultaneous steps, or stale plans after edits — falls back
/// cleanly to <see cref="Engine"/> with exact parity.</para></summary>
public sealed class CompiledPlan
{
    private static readonly Dictionary<(int SrcCap, int DstCap, int Take, int Give), TransitionLaw> ConvertCache = [];
    private static readonly Dictionary<(int MaxA, int MaxB, int ProdCap, int OvfCap, bool AUnary, bool BUnary, bool VUnary, bool OUnary), TransitionLaw> BoundedMulCache = [];
    private static readonly Dictionary<(int Radix, bool Binary, bool SumBeforeCarry), TransitionLaw> DigitAddCache = [];
    private static readonly Dictionary<(int Radix, bool Binary), (TransitionLaw Sum, TransitionLaw Carry)> SplitDigitCache = [];
    private static readonly Dictionary<(int ASize, bool AUnary, int BSize, bool BUnary, int Radix, bool SUnary), RelationLaw> ColumnCache = [];

    private readonly Engine engine;
    private readonly bool dynamicTopology;
    private readonly WiringSnapshot[] snapshots;
    private readonly CompiledWiring[] compiled;
    private readonly FusedConcreteLadderAdder? concreteLadder;
    private readonly FusedRelationalLadderAdder? relationalLadder;
    private IReadOnlyList<int[]>? cachedEngineWaves;

    private CompiledPlan(
        Engine engine,
        bool dynamicTopology,
        WiringSnapshot[] snapshots,
        CompiledWiring[] compiled,
        FusedConcreteLadderAdder? concreteLadder,
        FusedRelationalLadderAdder? relationalLadder)
    {
        this.engine = engine;
        this.dynamicTopology = dynamicTopology;
        this.snapshots = snapshots;
        this.compiled = compiled;
        this.concreteLadder = concreteLadder;
        this.relationalLadder = relationalLadder;
    }

    public Engine Source => engine;

    /// <summary>True iff a packet-exact unary <see cref="Flows.Convert"/> law
    /// was qualified strictly from its table bits and accounting.</summary>
    internal bool RecognizedPacketConvert => compiled.Any(c => c.Kernel is PacketConvertKernel);

    /// <summary>True iff a functional lossless bounded multiplier was qualified
    /// strictly from its table bits and unchanged-input accounting.</summary>
    internal bool RecognizedBoundedMultiply => compiled.Any(c => c.Kernel is BoundedMultiplyKernel);

    /// <summary>True iff a general small-table functional transition was lowered
    /// to an L1-resident SIMD bit-sliced XOR-flip kernel.</summary>
    internal bool RecognizedSimdTransition => compiled.Any(c => c.Kernel is SimdTransitionKernel);

    /// <summary>True iff a relational column matching <see cref="Numbers.Column"/>
    /// was qualified strictly via <see cref="Law.SameTable"/>.</summary>
    internal bool RecognizedRelationalColumn => compiled.Any(c => c.Kernel is RelationalColumnKernel);

    /// <summary>True iff a multi-rung concrete ripple-carry ladder adder was
    /// qualified from verified <see cref="Numbers.DigitSum"/> tables.</summary>
    internal bool RecognizedConcreteLadderAdder => concreteLadder is not null;

    /// <summary>True iff a multi-rung Pass-1 relational <see cref="Ladder.Add"/>
    /// chain was qualified from verified <see cref="Numbers.Column"/> tables.</summary>
    internal bool RecognizedRelationalLadderAdder => relationalLadder is not null;

    /// <summary>True iff at least one compiled kernel is available on this plan.</summary>
    public bool HasAnyCompiledKernel =>
        !dynamicTopology && (concreteLadder is not null || relationalLadder is not null || compiled.Any(c => c.CanAccelerate));

    /// <summary>False after any structural edit to the engine: adding a wiring,
    /// adding or retiring rows, rewiring references, toggling attention, enabling
    /// recording, plugging or unplugging a socket, rebinding or extending a port
    /// lane, changing workspace candidates, or growing/recruiting/retiring a bound
    /// store.</summary>
    public bool IsCurrent
    {
        get
        {
            if (engine.Wirings.Count != snapshots.Length) return false;
            for (var i = 0; i < snapshots.Length; i++)
                if (!snapshots[i].Matches(engine.Wirings[i])) return false;
            return true;
        }
    }

    /// <summary>Builds a derived compiled plan for <paramref name="engine"/>
    /// strictly by inspecting its law tables, accounting, and bindings.</summary>
    public static CompiledPlan Compile(Engine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        var dynamicTopology = DetectsDynamicTopology(engine);
        var snapshots = new WiringSnapshot[engine.Wirings.Count];
        var compiled = new CompiledWiring[engine.Wirings.Count];
        for (var i = 0; i < engine.Wirings.Count; i++)
        {
            var w = engine.Wirings[i];
            snapshots[i] = WiringSnapshot.Capture(w);
            compiled[i] = dynamicTopology ? CompiledWiring.FallbackOnly(w) : CompileWiring(w);
        }

        var concrete = dynamicTopology ? null : TryRecognizeConcreteLadder(engine, compiled);
        var relational = dynamicTopology || concrete is not null ? null : TryRecognizeRelationalLadder(engine, compiled);
        return new CompiledPlan(engine, dynamicTopology, snapshots, compiled, concrete, relational);
    }

    /// <summary>Workspace preview always falls back cleanly to
    /// <see cref="Workspace.Apply"/>, keeping copy-on-write, selection layers,
    /// and calibration checks intact.</summary>
    public static WorkspaceStep Preview(Workspace workspace, Wiring wiring)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(wiring);
        return workspace.Apply(wiring);
    }

    // ---- Table-level qualification helpers (strictly SameTable / Recognition.Compare) ----

    /// <summary>Qualifies a unary <see cref="Flows.Convert"/> law from its
    /// <see cref="PreservedSum"/> accounting and exact <see cref="Law.SameTable"/>
    /// equality against <see cref="Flows.Convert"/>.</summary>
    public static bool TryRecognizePacketConvert(TransitionLaw law, out int take, out int give)
    {
        ArgumentNullException.ThrowIfNull(law);
        take = give = 0;
        if (law.Roles.Count != 2 || !law.Roles[0].IsUnary || !law.Roles[1].IsUnary || !law.IsFunctional) return false;
        int srcCap = law.Roles[0].Size - 1, dstCap = law.Roles[1].Size - 1;
        if (srcCap < 1 || dstCap < 1) return false;

        PreservedSum? conserved = null;
        foreach (var inv in law.Accounting.Invariants)
            if (inv is PreservedSum ps && ps.Roles.Count == 2)
            {
                var r0 = ps.Roles[0];
                var r1 = ps.Roles[1];
                if ((r0 == 0 && r1 == 1) || (r0 == 1 && r1 == 0)) { conserved = ps; break; }
            }
        if (conserved is null) return false;

        var probeAfter = law.Tuple(law.Afters(law.TupleIndex([srcCap, 0]))[0]);
        var t = srcCap - probeAfter[0];
        var g = probeAfter[1];
        if (t < 1 || g < 1) return false;

        var wSrc = conserved.Roles[0] == 0 ? conserved.Weights[0] : conserved.Weights[1];
        var wDst = conserved.Roles[0] == 1 ? conserved.Weights[0] : conserved.Weights[1];
        if (wSrc <= 0 || wDst <= 0 || (long)wSrc * t != (long)wDst * g) return false;

        var reference = CachedConvert(srcCap, dstCap, t, g);
        if (!law.SameTable(reference)) return false;
        take = t;
        give = g;
        return true;
    }

    /// <summary>Qualifies a functional lossless bounded multiplier
    /// <c>(a, b, value, overflow)</c> strictly via <see cref="Law.SameTable"/>
    /// and unchanged-input verification.</summary>
    public static bool TryRecognizeBoundedMultiply(TransitionLaw law, out int prodCap, out int overflowCap)
    {
        ArgumentNullException.ThrowIfNull(law);
        prodCap = overflowCap = 0;
        if (law.Roles.Count != 4 || !law.IsFunctional || law.Writes(0) || law.Writes(1)) return false;
        int maxA = law.Roles[0].Size - 1, maxB = law.Roles[1].Size - 1;
        int cap = law.Roles[2].Size - 1, ovfCap = law.Roles[3].Size - 1;
        if (cap < 1 || ovfCap < 1 || (long)cap + ovfCap < (long)maxA * maxB) return false;

        var reference = CachedBoundedMultiply(law.Roles[0], law.Roles[1], law.Roles[2], law.Roles[3]);
        if (!law.SameTable(reference)) return false;
        prodCap = cap;
        overflowCap = ovfCap;
        return true;
    }

    /// <summary>Qualifies a 5-role functional digit-add transition against
    /// <see cref="Numbers.DigitSum"/> via <see cref="Recognition.Compare"/> /
    /// <see cref="Law.SameTable"/> (accepting either <c>(a, b, cin, sum, cout)</c>
    /// or <c>(a, b, cin, cout, sum)</c> role order).</summary>
    public static bool TryRecognizeDigitAddStep(TransitionLaw law, out int radix) =>
        TryRecognizeDigitAddStep(law, out radix, out _, out _);

    internal static bool TryRecognizeDigitAddStep(TransitionLaw law, out int radix, out int sumRole, out int coutRole)
    {
        ArgumentNullException.ThrowIfNull(law);
        radix = sumRole = coutRole = 0;
        if (law.Roles.Count != 5 || !law.IsFunctional) return false;
        var d = law.Roles[0];
        var p = d.Size;
        if (p is < 2 or > 16 || law.Roles[1] != d || law.Roles[2].Size != 2) return false;
        if (law.Writes(0) || law.Writes(1) || law.Writes(2)) return false;

        if (law.Roles[3] == d && law.Roles[4].Size == 2)
        {
            var reference = CachedDigitAdd(p, !d.IsUnary, sumBeforeCarry: true, law.Roles[2], law.Roles[4]);
            if (law.SameTable(reference)) { radix = p; sumRole = 3; coutRole = 4; return true; }
        }
        if (law.Roles[4] == d && law.Roles[3].Size == 2)
        {
            var reference = CachedDigitAdd(p, !d.IsUnary, sumBeforeCarry: false, law.Roles[2], law.Roles[3]);
            if (law.SameTable(reference)) { radix = p; sumRole = 4; coutRole = 3; return true; }
        }
        return false;
    }

    /// <summary>Qualifies a split <c>(DigitSum, DigitCarry)</c> transition pair
    /// against <see cref="Numbers.DigitSum"/> via <see cref="Operations.Bind"/>
    /// and <see cref="Law.SameTable"/>.</summary>
    public static bool TryRecognizeSplitDigitPair(TransitionLaw sumLaw, TransitionLaw carryLaw, out int radix)
    {
        ArgumentNullException.ThrowIfNull(sumLaw);
        ArgumentNullException.ThrowIfNull(carryLaw);
        radix = 0;
        if (sumLaw.Roles.Count != 4 || carryLaw.Roles.Count != 4 || !sumLaw.IsFunctional || !carryLaw.IsFunctional) return false;
        var d = sumLaw.Roles[0];
        var p = d.Size;
        if (p is < 2 or > 16) return false;
        var c = sumLaw.Roles[2];
        if (c.Size != 2 || sumLaw.Roles[1] != d || sumLaw.Roles[3] != d) return false;
        if (carryLaw.Roles[0] != d || carryLaw.Roles[1] != d || carryLaw.Roles[2] != c || carryLaw.Roles[3] != c) return false;

        var (refSum, refCarry) = CachedSplitDigit(p, !d.IsUnary, c);
        if (!sumLaw.SameTable(refSum) || !carryLaw.SameTable(refCarry)) return false;
        radix = p;
        return true;
    }

    /// <summary>Qualifies a 5-role relational sum column strictly via
    /// <see cref="Law.SameTable"/> against <see cref="Numbers.Column"/>.</summary>
    public static bool TryRecognizeRelationalColumn(RelationLaw law, out int radix)
    {
        ArgumentNullException.ThrowIfNull(law);
        radix = 0;
        if (law.Roles.Count != 5) return false;
        if (law.Roles[2] != Numbers.Carry || law.Roles[3] != Numbers.Carry) return false;
        var sa = law.Roles[0];
        var sb = law.Roles[1];
        var ss = law.Roles[4];
        if (sa.Size > 64 || sb.Size > 64 || ss.Size is < 2 or > 64) return false;

        var reference = CachedColumn(sa, sb, ss.Size, !ss.IsUnary);
        if (!law.SameTable(reference)) return false;
        radix = ss.Size;
        return true;
    }

    // ---- Execution entry points ----

    /// <summary>Runs one step matching <see cref="Engine.Step"/> or
    /// <see cref="Engine.StepTogether"/> and returns a full
    /// <see cref="EngineStep"/>.</summary>
    public EngineStep Step(bool together = false) => Step(out _, together);

    /// <summary>Runs one step matching <see cref="Engine.Step"/> or
    /// <see cref="Engine.StepTogether"/>, returning both the exact
    /// <see cref="EngineStep"/> and execution telemetry.</summary>
    public EngineStep Step(out CompiledStepReport report, bool together = false)
    {
        if (!IsCurrent)
        {
            report = new CompiledStepReport(UsedFastPath: false, Stale: true, FastLanes: 0, DeoptLanes: TotalLiveRows());
            return together ? engine.StepTogether() : engine.Step();
        }
        if (dynamicTopology)
        {
            report = new CompiledStepReport(UsedFastPath: false, Stale: false, FastLanes: 0, DeoptLanes: TotalLiveRows());
            return together ? engine.StepTogether() : engine.Step();
        }
        if (together)
        {
            if (compiled.Length == 1 && compiled[0].CanAccelerateTogether)
            {
                var (wStep, fast, deopt) = compiled[0].StepWithResult(forTogether: true);
                if (deopt == 0)
                {
                    engine.AdvanceTick(1);
                    report = new CompiledStepReport(UsedFastPath: fast > 0, Stale: false, FastLanes: fast, DeoptLanes: 0);
                    return new EngineStep(engine.Tick, [wStep], [[0]]);
                }
                // Deopted before mutating state: let canonical Engine.StepTogether run cleanly.
                report = new CompiledStepReport(UsedFastPath: false, Stale: false, FastLanes: 0, DeoptLanes: deopt);
                return engine.StepTogether();
            }
            report = new CompiledStepReport(UsedFastPath: false, Stale: false, FastLanes: 0, DeoptLanes: TotalLiveRows());
            return engine.StepTogether();
        }

        var waves = cachedEngineWaves ??= engine.Waves();

        if (relationalLadder is not null)
        {
            var (steps, fast, deopt) = relationalLadder.StepWithResults();
            if (deopt == 0)
            {
                engine.AdvanceTick(1);
                report = new CompiledStepReport(UsedFastPath: fast > 0, Stale: false, FastLanes: fast, DeoptLanes: 0);
                return new EngineStep(engine.Tick, steps, waves);
            }
            report = new CompiledStepReport(UsedFastPath: false, Stale: false, FastLanes: 0, DeoptLanes: deopt);
            return engine.Step();
        }

        if (concreteLadder is not null && concreteLadder.TryCaptureOldOutputs(out var oldSums, out var oldCarries))
        {
            var (fast, deopt, changedPerWiring) = concreteLadder.StepWithChangePlanes();
            if (deopt == 0)
            {
                var steps = concreteLadder.BuildWiringSteps(oldSums!, oldCarries!, changedPerWiring!);
                engine.AdvanceTick(1);
                report = new CompiledStepReport(UsedFastPath: fast > 0, Stale: false, FastLanes: fast, DeoptLanes: 0);
                return new EngineStep(engine.Tick, steps, waves);
            }
            report = new CompiledStepReport(UsedFastPath: false, Stale: false, FastLanes: 0, DeoptLanes: deopt);
            return engine.Step();
        }

        var wiringSteps = new WiringStep[compiled.Length];
        var totalFast = 0;
        var totalDeopt = 0;
        for (var i = 0; i < compiled.Length; i++)
        {
            var c = compiled[i];
            if (c.CanAccelerate)
            {
                var (wStep, fast, deopt) = c.StepWithResult(forTogether: false);
                if (deopt == 0)
                {
                    wiringSteps[i] = wStep;
                    totalFast += fast;
                    continue;
                }
                wiringSteps[i] = c.Wiring.Apply();
                totalDeopt += deopt;
                continue;
            }
            wiringSteps[i] = c.Wiring.Apply();
            totalDeopt += c.Wiring.Live.Count;
        }
        engine.AdvanceTick(1);
        report = new CompiledStepReport(UsedFastPath: totalFast > 0, Stale: false, FastLanes: totalFast, DeoptLanes: totalDeopt);
        return new EngineStep(engine.Tick, wiringSteps, waves);
    }

    /// <summary>Settles matching <see cref="Engine.Settle"/> without allocating
    /// per-step <see cref="EngineStep"/> snapshots on compiled paths.</summary>
    public int? Settle(int limit, bool together = false) => Settle(limit, out _, together);

    /// <summary>Settles matching <see cref="Engine.Settle"/> and reports
    /// compiled versus fallback lane counts.</summary>
    public int? Settle(int limit, out CompiledStepReport report, bool together = false)
    {
        if (!IsCurrent)
        {
            var settled = engine.Settle(limit, together);
            report = new CompiledStepReport(UsedFastPath: false, Stale: true, FastLanes: 0, DeoptLanes: TotalLiveRows());
            return settled;
        }
        if (limit <= 0)
        {
            report = new CompiledStepReport(UsedFastPath: false, Stale: false, FastLanes: 0, DeoptLanes: 0);
            return null;
        }
        if (dynamicTopology)
        {
            var settled = engine.Settle(limit, together);
            report = new CompiledStepReport(UsedFastPath: false, Stale: false, FastLanes: 0, DeoptLanes: TotalLiveRows());
            return settled;
        }

        if (!together && compiled.Length == 1 && compiled[0].Kernel is PacketConvertKernel && compiled[0].CanAccelerate)
        {
            var (fast, deopt, ticks, quiet) = compiled[0].SettlePacketConvertExact(limit);
            if (deopt == 0)
            {
                engine.AdvanceTick(ticks);
                report = new CompiledStepReport(UsedFastPath: fast > 0, Stale: false, FastLanes: fast, DeoptLanes: 0);
                return quiet ? ticks : null;
            }
        }

        var totalFast = 0;
        var totalDeopt = 0;
        var anyFast = false;
        for (var tick = 1; tick <= limit; tick++)
        {
            var stepRep = RunStepCore(together, out var quiet);
            totalFast += stepRep.FastLanes;
            totalDeopt += stepRep.DeoptLanes;
            if (stepRep.UsedFastPath) anyFast = true;
            if (quiet)
            {
                report = new CompiledStepReport(anyFast, Stale: false, totalFast, totalDeopt);
                return tick;
            }
        }
        report = new CompiledStepReport(anyFast, Stale: false, totalFast, totalDeopt);
        return null;
    }

    /// <summary>Zero-allocation single step returning telemetry only.</summary>
    public CompiledStepReport RunStep(bool together = false)
    {
        if (!IsCurrent)
        {
            if (together) engine.StepTogether(); else engine.Step();
            return new CompiledStepReport(UsedFastPath: false, Stale: true, FastLanes: 0, DeoptLanes: TotalLiveRows());
        }
        if (dynamicTopology)
        {
            if (together) engine.StepTogether(); else engine.Step();
            return new CompiledStepReport(UsedFastPath: false, Stale: false, FastLanes: 0, DeoptLanes: TotalLiveRows());
        }
        return RunStepCore(together, out _);
    }

    /// <summary>Zero-allocation settle returning telemetry and settled step count.</summary>
    public CompiledStepReport RunSettle(int limit, out int? settledSteps, bool together = false)
    {
        settledSteps = Settle(limit, out var report, together);
        return report;
    }

    private CompiledStepReport RunStepCore(bool together, out bool allQuiet)
    {
        if (together)
        {
            if (compiled.Length == 1 && compiled[0].CanAccelerateTogether)
            {
                var (fast, deopt, quiet) = compiled[0].StepFastOnly();
                if (deopt == 0)
                {
                    engine.AdvanceTick(1);
                    allQuiet = quiet;
                    return new CompiledStepReport(UsedFastPath: fast > 0, Stale: false, FastLanes: fast, DeoptLanes: 0);
                }
            }
            var fallback = engine.StepTogether();
            allQuiet = fallback.Steps.All(s => s.Quiet);
            return new CompiledStepReport(UsedFastPath: false, Stale: false, FastLanes: 0, DeoptLanes: TotalLiveRows());
        }

        if (relationalLadder is not null)
        {
            var (fast, deopt, changed) = relationalLadder.StepFastOnly();
            if (deopt == 0)
            {
                engine.AdvanceTick(1);
                allQuiet = !changed;
                return new CompiledStepReport(UsedFastPath: fast > 0, Stale: false, FastLanes: fast, DeoptLanes: 0);
            }
            var fallback = engine.Step();
            allQuiet = fallback.Steps.All(s => s.Quiet);
            return new CompiledStepReport(UsedFastPath: false, Stale: false, FastLanes: 0, DeoptLanes: deopt);
        }

        if (concreteLadder is not null)
        {
            var (fast, deopt, changed) = concreteLadder.StepFastOnly();
            if (deopt == 0)
            {
                engine.AdvanceTick(1);
                allQuiet = !changed;
                return new CompiledStepReport(UsedFastPath: fast > 0, Stale: false, FastLanes: fast, DeoptLanes: 0);
            }
            var fallback = engine.Step();
            allQuiet = fallback.Steps.All(s => s.Quiet);
            return new CompiledStepReport(UsedFastPath: false, Stale: false, FastLanes: 0, DeoptLanes: deopt);
        }

        var totalFast = 0;
        var totalDeopt = 0;
        allQuiet = true;
        for (var i = 0; i < compiled.Length; i++)
        {
            var c = compiled[i];
            if (c.CanAccelerate)
            {
                var (fast, deopt, quiet) = c.StepFastOnly();
                if (deopt == 0)
                {
                    totalFast += fast;
                    if (!quiet) allQuiet = false;
                    continue;
                }
                var applied = c.Wiring.Apply();
                if (!applied.Quiet) allQuiet = false;
                totalDeopt += deopt;
                continue;
            }
            var step = c.Wiring.Apply();
            if (!step.Quiet) allQuiet = false;
            totalDeopt += c.Wiring.Live.Count;
        }
        engine.AdvanceTick(1);
        return new CompiledStepReport(UsedFastPath: totalFast > 0, Stale: false, FastLanes: totalFast, DeoptLanes: totalDeopt);
    }

    private int TotalLiveRows() => engine.Wirings.Sum(w => w.Live.Count);

    private static bool DetectsDynamicTopology(Engine engine)
    {
        if (engine.Wirings.Count == 0) return false;
        var rowStores = new HashSet<Store>(ReferenceEqualityComparer.Instance);
        for (var i = 0; i < engine.Wirings.Count; i++) rowStores.Add(engine.Wirings[i].Rows);
        for (var i = 0; i < engine.Wirings.Count; i++)
        {
            var w = engine.Wirings[i];
            for (var r = 0; r < w.Bindings.Count; r++)
            {
                if (!w.Writes(r)) continue;
                var (target, writesLife) = WriteTarget(w.Bindings[r].Surface);
                if (writesLife || (target is not null && rowStores.Contains(target)))
                    return true;
            }
        }
        return false;
    }

    private static (Store? Store, bool WritesLife) WriteTarget(Surface surface) => surface.Underlying switch
    {
        Store st => (st, false),
        Life life => (life.Of, true),
        Port port => (port.Owner, false),
        Socket sock => (sock.Plugged?.Owner, false),
        _ => (null, false),
    };

    // ---- Reference-table caches for O(1) SameTable qualification ----

    private static TransitionLaw CachedConvert(int srcCap, int dstCap, int take, int give)
    {
        var key = (srcCap, dstCap, take, give);
        lock (ConvertCache)
        {
            if (ConvertCache.TryGetValue(key, out var cached)) return cached;
            return ConvertCache[key] = Flows.Convert(take, give, srcCap, dstCap);
        }
    }

    private static TransitionLaw CachedBoundedMultiply(Shape a, Shape b, Shape val, Shape ovf)
    {
        var key = (a.Size - 1, b.Size - 1, val.Size - 1, ovf.Size - 1, a.IsUnary, b.IsUnary, val.IsUnary, ovf.IsUnary);
        lock (BoundedMulCache)
        {
            if (BoundedMulCache.TryGetValue(key, out var cached)) return cached;
            var cap = val.Size - 1;
            return BoundedMulCache[key] = TransitionLaw.FromFunction(
                [a, b, val, ovf],
                s =>
                {
                    var p = s[0] * s[1];
                    return [s[0], s[1], p <= cap ? p : cap, p <= cap ? 0 : p - cap];
                },
                new Accounting(true, new Unchanged(0), new Unchanged(1)));
        }
    }

    private static TransitionLaw CachedDigitAdd(int radix, bool binary, bool sumBeforeCarry, Shape cinShape, Shape coutShape)
    {
        var d = Numbers.Digit(radix, binary);
        if (cinShape != Numbers.Carry || coutShape != Numbers.Carry)
        {
            var col = Polynomial.Balance([d, d, cinShape, coutShape, d], 1, 1, 1, -radix, -1);
            return sumBeforeCarry
                ? Operations.Bind(col.Relation, [d, d, cinShape, d, coutShape], [0, 1, 2, -1, -1], [-1, -1, -1, 4, 3])
                : Operations.Bind(col.Relation, [d, d, cinShape, coutShape, d], [0, 1, 2, -1, -1], [-1, -1, -1, 3, 4]);
        }
        var key = (radix, binary, sumBeforeCarry);
        lock (DigitAddCache)
        {
            if (DigitAddCache.TryGetValue(key, out var cached)) return cached;
            var col = Numbers.DigitSum(radix, binary);
            return DigitAddCache[key] = sumBeforeCarry
                ? Operations.Bind(col.Relation, [d, d, Numbers.Carry, d, Numbers.Carry], [0, 1, 2, -1, -1], [-1, -1, -1, 4, 3])
                : Operations.Bind(col.Relation, [d, d, Numbers.Carry, Numbers.Carry, d], [0, 1, 2, -1, -1], [-1, -1, -1, 3, 4]);
        }
    }

    private static (TransitionLaw Sum, TransitionLaw Carry) CachedSplitDigit(int radix, bool binary, Shape carryShape)
    {
        var d = Numbers.Digit(radix, binary);
        if (carryShape != Numbers.Carry)
        {
            var col = Polynomial.Balance([d, d, carryShape, carryShape, d], 1, 1, 1, -radix, -1);
            return (
                Operations.Bind(col.Relation, [d, d, carryShape, d], [0, 1, 2, -1, -1], [-1, -1, -1, -1, 3]),
                Operations.Bind(col.Relation, [d, d, carryShape, carryShape], [0, 1, 2, -1, -1], [-1, -1, -1, 3, -1]));
        }
        var key = (radix, binary);
        lock (SplitDigitCache)
        {
            if (SplitDigitCache.TryGetValue(key, out var cached)) return cached;
            var col = Numbers.DigitSum(radix, binary);
            return SplitDigitCache[key] = (
                Operations.Bind(col.Relation, [d, d, Numbers.Carry, d], [0, 1, 2, -1, -1], [-1, -1, -1, -1, 3]),
                Operations.Bind(col.Relation, [d, d, Numbers.Carry, Numbers.Carry], [0, 1, 2, -1, -1], [-1, -1, -1, 3, -1]));
        }
    }

    private static RelationLaw CachedColumn(Shape a, Shape b, int radix, bool binary)
    {
        var key = (a.Size, a.IsUnary, b.Size, b.IsUnary, radix, !binary);
        lock (ColumnCache)
        {
            if (ColumnCache.TryGetValue(key, out var cached)) return cached;
            return ColumnCache[key] = Numbers.Column(a, b, radix, binary).Relation;
        }
    }

    // ---- Fast 64-lane bitwise identity-mapping detector ----

    private static readonly ulong[] LowBitPatterns =
    [
        0xAAAA_AAAA_AAAA_AAAAUL,
        0xCCCC_CCCC_CCCC_CCCCUL,
        0xF0F0_F0F0_F0F0_F0F0UL,
        0xFF00_FF00_FF00_FF00UL,
        0xFFFF_0000_FFFF_0000UL,
        0xFFFF_FFFF_0000_0000UL,
    ];

    private static bool CheckIdentityReferences(Wiring wiring, Plane activeMask)
    {
        if (activeMask.None) return false;
        var rowCount = wiring.Count;
        for (var r = 0; r < wiring.Bindings.Count; r++)
            if (wiring.Bindings[r].Surface is not Store st || st.Count < rowCount) return false;

        var activeWords = activeMask.RawWords;
        var wordCount = activeWords.Length;
        for (var r = 0; r < wiring.Bindings.Count; r++)
        {
            var refPlanes = wiring.ReferencePlanes(r);
            for (var k = 0; k < refPlanes.Count; k++)
            {
                var raw = wiring.Rows.RawPlane(refPlanes[k]);
                if (raw is null) return false;
                var words = raw.RawWords;
                if (k < 6)
                {
                    var expected = LowBitPatterns[k];
                    for (var w = 0; w < wordCount; w++)
                        if (((words[w] ^ expected) & activeWords[w]) != 0) return false;
                }
                else
                {
                    var shift = k - 6;
                    for (var w = 0; w < wordCount; w++)
                    {
                        var expected = ((w >> shift) & 1) != 0 ? ulong.MaxValue : 0UL;
                        if (((words[w] ^ expected) & activeWords[w]) != 0) return false;
                    }
                }
            }
        }
        return true;
    }

    private static Plane IntersectStoreLives(Plane activeMask, Store[] stores)
    {
        var result = activeMask.Clone();
        var rw = result.RawWords;
        for (var s = 0; s < stores.Length; s++)
        {
            var lw = stores[s].RawLive.RawWords;
            for (var w = 0; w < rw.Length; w++) rw[w] &= lw[w];
        }
        result.TrimTail();
        return result;
    }

    private static bool LiveCovers(Plane live, Plane runnable)
    {
        if (live.Length < runnable.Length) return false;
        var lw = live.RawWords;
        var rw = runnable.RawWords;
        for (var w = 0; w < rw.Length; w++)
            if ((rw[w] & ~lw[w]) != 0) return false;
        return true;
    }

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

    // ---- Per-wiring qualification (zero enums: polymorphic sealed kernels) ----

    private abstract class WiringKernel { }

    private sealed class PacketConvertKernel(int take, int give) : WiringKernel
    {
        public int Take { get; } = take;
        public int Give { get; } = give;
    }

    private sealed class BoundedMultiplyKernel(int prodCap, int overflowCap) : WiringKernel
    {
        public int ProdCap { get; } = prodCap;
        public int OverflowCap { get; } = overflowCap;
    }

    private sealed class SimdTransitionKernel : WiringKernel { }

    private sealed class RelationalColumnKernel(int radix) : WiringKernel
    {
        public int Radix { get; } = radix;
    }

    private static CompiledWiring CompileWiring(Wiring wiring)
    {
        if (wiring.Recorded is not null) return CompiledWiring.FallbackOnly(wiring);
        if (wiring.Bindings.Any(b => b.Surface is not Store)) return CompiledWiring.FallbackOnly(wiring);

        if (wiring.Law is TransitionLaw transition)
        {
            if (!transition.IsFunctional || wiring.Bindings.Any(b => b.IsOpen))
                return CompiledWiring.FallbackOnly(wiring);

            if (TryRecognizePacketConvert(transition, out var take, out var give))
                return new CompiledWiring(wiring, new PacketConvertKernel(take, give));

            if (TryRecognizeBoundedMultiply(transition, out var prodCap, out var overflowCap))
                return new CompiledWiring(wiring, new BoundedMultiplyKernel(prodCap, overflowCap));

            var totalBits = 0;
            for (var r = 0; r < wiring.Bindings.Count; r++) totalBits += wiring.Bindings[r].Planes.Count;
            if (totalBits <= 64 && transition.StateCount <= 512)
                return new CompiledWiring(wiring, new SimdTransitionKernel());

            return CompiledWiring.FallbackOnly(wiring);
        }

        if (wiring.Law is RelationLaw relation && TryRecognizeRelationalColumn(relation, out var radix))
            return new CompiledWiring(wiring, new RelationalColumnKernel(radix));

        return CompiledWiring.FallbackOnly(wiring);
    }

    private readonly record struct ChangingState(ulong BeforeMask, int[] FlippedWriteIndices);

    private sealed class CompiledWiring
    {
        private readonly Plane activeMask;
        private readonly Plane staticRunnable;
        private readonly Plane staticSkipped;
        private readonly int staticRunnableCount;
        private readonly bool staticTopologyClean;
        private readonly bool identityMapped;
        private readonly Store[] distinctStores;

        // Transition SIMD layout (only populated for SimdTransitionKernel)
        private readonly int totalRoleBits;
        private readonly int[] writeFlatBits;
        private readonly (Store Store, int Plane)[] flatPlaneRefs;
        private readonly (Store Store, int Plane)[] writePlaneRefs;
        private readonly ChangingState[] changingStates;
        private readonly Vector<ulong>[] inVecScratch;
        private readonly Vector<ulong>[] flipVecScratch;
        private readonly ulong[] inScalarScratch;
        private readonly ulong[] flipScalarScratch;

        // Relational column layout (only populated for RelationalColumnKernel)
        private readonly RelationalColumnPlan? columnPlan;

        private CompiledWiring(Wiring wiring)
        {
            Wiring = wiring;
            Kernel = null;
            activeMask = new Plane(0);
            staticRunnable = new Plane(0);
            staticSkipped = new Plane(0);
            distinctStores = [];
            writeFlatBits = [];
            flatPlaneRefs = [];
            writePlaneRefs = [];
            changingStates = [];
            inVecScratch = [];
            flipVecScratch = [];
            inScalarScratch = [];
            flipScalarScratch = [];
        }

        public static CompiledWiring FallbackOnly(Wiring wiring) => new(wiring);

        public CompiledWiring(Wiring wiring, WiringKernel kernel)
        {
            Wiring = wiring;
            Kernel = kernel;
            activeMask = wiring.Attended();
            distinctStores = [.. new HashSet<Store>(wiring.Bindings.Select(b => (Store)b.Surface), ReferenceEqualityComparer.Instance)];

            var ident = CheckIdentityReferences(wiring, activeMask);
            identityMapped = ident;

            if (kernel is RelationalColumnKernel relKernel)
            {
                writeFlatBits = [];
                flatPlaneRefs = [];
                writePlaneRefs = [];
                changingStates = [];
                inVecScratch = [];
                flipVecScratch = [];
                inScalarScratch = [];
                flipScalarScratch = [];

                if (!ident)
                {
                    staticRunnable = new Plane(0);
                    staticSkipped = new Plane(0);
                    staticTopologyClean = false;
                    return;
                }

                staticRunnable = IntersectStoreLives(activeMask, distinctStores);
                staticSkipped = activeMask.AndNot(staticRunnable);
                staticRunnableCount = staticRunnable.Count;
                columnPlan = RelationalColumnPlan.TryBuild(wiring, relKernel.Radix, staticRunnable, staticSkipped);
                staticTopologyClean = columnPlan is not null;
                return;
            }

            var transition = (TransitionLaw)wiring.Law;
            var clean = true;
            if (ident)
            {
                // Identity-mapped: intra-row bit aliasing reduces to checking position 0 once;
                // cross-row write contention is impossible because row i touches only position i.
                var rowBits = new HashSet<(Surface, int)>();
                for (var r = 0; r < wiring.Bindings.Count && clean; r++)
                {
                    var b = wiring.Bindings[r];
                    var st = (Store)b.Surface;
                    var writes = wiring.Writes(r);
                    for (var p = 0; p < b.Planes.Count; p++)
                    {
                        var plane = b.Plane(p);
                        if (writes && st.IsConstantPlane(plane)) { clean = false; break; }
                        if (!rowBits.Add((b.Surface, plane))) { clean = false; break; }
                    }
                }
                staticRunnable = clean ? IntersectStoreLives(activeMask, distinctStores) : new Plane(wiring.Count);
                staticSkipped = activeMask.AndNot(staticRunnable);
            }
            else
            {
                var runnableAll = wiring.Runnable();
                var aliasedAll = wiring.Aliased();
                staticRunnable = activeMask.And(runnableAll);
                staticSkipped = activeMask.AndNot(runnableAll).AndNot(aliasedAll);
                if (!activeMask.And(aliasedAll).None)
                {
                    clean = false;
                }
                else if (staticRunnable.Any && wiring.Waves(staticRunnable).Count > 1)
                {
                    clean = false;
                }
                else
                {
                    var writtenBits = new HashSet<(Surface, int, int)>();
                    var rowBits = new HashSet<(Surface, int, int)>();
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
            }

            staticRunnableCount = staticRunnable.Count;
            staticTopologyClean = clean;

            if (kernel is SimdTransitionKernel && ident && clean)
            {
                var roleOffsets = new int[transition.Roles.Count];
                var flatRefs = new List<(Store, int)>();
                var writeFlatList = new List<int>();
                var writeRefList = new List<(Store, int)>();
                var writeSlotByFlat = new Dictionary<int, int>();
                for (var r = 0; r < transition.Roles.Count; r++)
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

                var entries = new List<ChangingState>();
                for (var before = 0; before < transition.StateCount; before++)
                {
                    var afters = transition.Afters(before);
                    if (afters.Count != 1 || afters[0] == before) continue;
                    var bt = transition.Tuple(before);
                    var at = transition.Tuple(afters[0]);
                    ulong bMask = 0;
                    var flips = new List<int>();
                    for (var r = 0; r < transition.Roles.Count; r++)
                    {
                        var bp = transition.Roles[r].Encode(bt[r]);
                        var ap = transition.Roles[r].Encode(at[r]);
                        var diff = bp ^ ap;
                        var off = roleOffsets[r];
                        for (var k = 0; k < transition.Roles[r].Bits; k++)
                        {
                            if (((bp >> k) & 1) != 0) bMask |= 1UL << (off + k);
                            if (((diff >> k) & 1) != 0 && writeSlotByFlat.TryGetValue(off + k, out var wSlot))
                                flips.Add(wSlot);
                        }
                    }
                    if (flips.Count > 0) entries.Add(new ChangingState(bMask, [.. flips]));
                }

                if (entries.Count <= 256)
                {
                    totalRoleBits = flatRefs.Count;
                    flatPlaneRefs = [.. flatRefs];
                    writeFlatBits = [.. writeFlatList];
                    writePlaneRefs = [.. writeRefList];
                    changingStates = [.. entries];
                    inVecScratch = new Vector<ulong>[totalRoleBits];
                    flipVecScratch = new Vector<ulong>[writeFlatBits.Length];
                    inScalarScratch = new ulong[totalRoleBits];
                    flipScalarScratch = new ulong[writeFlatBits.Length];
                    return;
                }

                staticTopologyClean = false;
            }

            writeFlatBits = [];
            flatPlaneRefs = [];
            writePlaneRefs = [];
            changingStates = [];
            inVecScratch = [];
            flipVecScratch = [];
            inScalarScratch = [];
            flipScalarScratch = [];
        }

        public Wiring Wiring { get; }
        public WiringKernel? Kernel { get; }
        public Plane StaticRunnable => staticRunnable;
        public Plane StaticSkipped => staticSkipped;
        public bool IdentityMapped => identityMapped;
        public bool StaticTopologyClean => staticTopologyClean;
        public RelationalColumnPlan? ColumnPlan => columnPlan;

        public bool CanAccelerate =>
            Kernel is not null
            && staticTopologyClean
            && (Kernel is not SimdTransitionKernel || identityMapped)
            && (Kernel is not BoundedMultiplyKernel || identityMapped)
            && Wiring.Recorded is null;

        public bool CanAccelerateTogether => CanAccelerate && identityMapped;

        public (int Fast, int Deopt, bool Quiet) StepFastOnly()
        {
            if (!CanAccelerate) return (0, staticRunnableCount, false);
            if (staticRunnableCount == 0) return (0, 0, true);

            if (columnPlan is not null)
            {
                if (!AllStoresLive() || !columnPlan.TryStepFastOnly(out var relChanged)) return (0, staticRunnableCount, false);
                return (staticRunnableCount, 0, !relChanged);
            }

            if (identityMapped)
            {
                if (!AllIdentityLanesValid()) return (0, staticRunnableCount, false);
                var changed = Kernel switch
                {
                    PacketConvertKernel pc => StepPacketConvertIdentity(pc.Take, pc.Give, changedPlane: null),
                    BoundedMultiplyKernel bm => StepBoundedMultiplyIdentity(bm.ProdCap, changedPlane: null),
                    SimdTransitionKernel => StepSimdIdentity(changedPlane: null),
                    _ => false,
                };
                return (staticRunnableCount, 0, !changed);
            }

            if (Kernel is PacketConvertKernel nonIdentPc)
            {
                if (!AllNonIdentityRowsValid()) return (0, staticRunnableCount, false);
                var changed = StepPacketConvertNonIdentity(nonIdentPc.Take, nonIdentPc.Give, changedPlane: null);
                return (staticRunnableCount, 0, !changed);
            }

            return (0, staticRunnableCount, false);
        }

        public (WiringStep Step, int Fast, int Deopt) StepWithResult(bool forTogether)
        {
            if (!CanAccelerate) return (null!, 0, staticRunnableCount);
            if (staticRunnableCount == 0)
            {
                var empty = new Plane(Wiring.Count);
                var none = Enumerable.Repeat(-1, Wiring.Count).ToArray();
                var step = new WiringStep(empty, empty.Clone(), empty.Clone(), empty.Clone(),
                    staticSkipped.Clone(), empty.Clone(), none, (int[])none.Clone(), []);
                if (forTogether) step = step with { Contested = empty.Clone() };
                return (step, 0, 0);
            }

            if (columnPlan is not null)
            {
                if (!AllStoresLive() || !columnPlan.TryStepWithResult(forTogether, out var relStep))
                    return (null!, 0, staticRunnableCount);
                return (relStep!, staticRunnableCount, 0);
            }

            if (identityMapped)
            {
                if (!AllIdentityLanesValid()) return (null!, 0, staticRunnableCount);
            }
            else if (Kernel is PacketConvertKernel)
            {
                if (!AllNonIdentityRowsValid()) return (null!, 0, staticRunnableCount);
            }
            else
            {
                return (null!, 0, staticRunnableCount);
            }

            var before = Enumerable.Repeat(-1, Wiring.Count).ToArray();
            var after = Enumerable.Repeat(-1, Wiring.Count).ToArray();
            foreach (var row in staticRunnable.Indexes()) before[row] = Wiring.TupleOf(row) ?? -1;

            var changedPlane = new Plane(Wiring.Count);
            if (identityMapped)
            {
                switch (Kernel)
                {
                    case PacketConvertKernel pc: StepPacketConvertIdentity(pc.Take, pc.Give, changedPlane); break;
                    case BoundedMultiplyKernel bm: StepBoundedMultiplyIdentity(bm.ProdCap, changedPlane); break;
                    case SimdTransitionKernel: StepSimdIdentity(changedPlane); break;
                }
            }
            else if (Kernel is PacketConvertKernel nonIdentPc)
            {
                StepPacketConvertNonIdentity(nonIdentPc.Take, nonIdentPc.Give, changedPlane);
            }

            foreach (var row in staticRunnable.Indexes())
                after[row] = changedPlane[row] ? (Wiring.TupleOf(row) ?? -1) : before[row];

            var emptyPlane = new Plane(Wiring.Count);
            var result = new WiringStep(
                changedPlane,
                emptyPlane,
                emptyPlane.Clone(),
                emptyPlane.Clone(),
                staticSkipped.Clone(),
                emptyPlane.Clone(),
                before,
                after,
                [staticRunnable.Clone()]);
            if (forTogether) result = result with { Contested = new Plane(Wiring.Count) };
            return (result, staticRunnableCount, 0);
        }

        public (int Fast, int Deopt, int Ticks, bool Quiet) SettlePacketConvertExact(int limit)
        {
            if (Kernel is not PacketConvertKernel pc || !identityMapped || !staticTopologyClean || staticRunnableCount == 0)
            {
                if (staticRunnableCount == 0) return (0, 0, 1, true);
                return (0, staticRunnableCount, 0, false);
            }
            if (!AllIdentityLanesValid()) return (0, staticRunnableCount, 0, false);

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
            int take = pc.Take, give = pc.Give;
            var maxPackets = 0;
            var hitLimit = false;

            for (var w = 0; w < wordCount; w++)
            {
                var mask = runWords[w];
                if (mask == 0UL) continue;

                // Quick word-level skip if no lane in this word can fire even once.
                if (take > srcCap || give > dstCap) continue;
                var canFire = mask & srcWords[take - 1][w] & ~dstWords[dstCap - give][w];
                if (canFire == 0UL) continue;

                var rem = canFire;
                while (rem != 0UL)
                {
                    var bit = BitOperations.TrailingZeroCount(rem);
                    var bitMask = 1UL << bit;
                    rem &= rem - 1;

                    var s = 0;
                    for (var k = 0; k < srcCap; k++)
                        if ((srcWords[k][w] & bitMask) != 0) s++; else break;
                    var d = 0;
                    for (var k = 0; k < dstCap; k++)
                        if ((dstWords[k][w] & bitMask) != 0) d++; else break;

                    var bySrc = s / take;
                    var byRoom = (dstCap - d) / give;
                    var needed = bySrc < byRoom ? bySrc : byRoom;
                    if (needed >= limit)
                    {
                        needed = limit;
                        hitLimit = true;
                    }
                    if (needed > maxPackets) maxPackets = needed;

                    var newS = s - needed * take;
                    var newD = d + needed * give;
                    for (var k = newS; k < s; k++) srcWords[k][w] &= ~bitMask;
                    for (var k = d; k < newD; k++) dstWords[k][w] |= bitMask;
                }
            }

            if (maxPackets > 0)
            {
                for (var k = 0; k < srcCap; k++) srcPlanes[k].TrimTail();
                for (var k = 0; k < dstCap; k++) dstPlanes[k].TrimTail();
            }

            var ticks = hitLimit ? limit : maxPackets + 1;
            return (staticRunnableCount, 0, ticks, !hitLimit);
        }

        private bool AllStoresLive()
        {
            for (var s = 0; s < distinctStores.Length; s++)
                if (!LiveCovers(distinctStores[s].RawLive, staticRunnable)) return false;
            return true;
        }

        private bool AllIdentityLanesValid()
        {
            if (!AllStoresLive()) return false;
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
                    if (bits < 31 && shape.Size == (1 << bits)) continue;
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

        private bool AllNonIdentityRowsValid()
        {
            var law = (TransitionLaw)Wiring.Law;
            foreach (var row in staticRunnable.Indexes())
            {
                for (var r = 0; r < Wiring.Bindings.Count; r++)
                {
                    var b = Wiring.Bindings[r];
                    var store = (Store)b.Surface;
                    var pos = Wiring.Reference(row, r);
                    if (!store.IsLive(pos)) return false;
                    ulong pat = 0;
                    for (var k = 0; k < b.Planes.Count; k++)
                        if (store.Read(b.Plane(k), pos)) pat |= 1UL << k;
                    if (law.Roles[r].Decode(pat) is null) return false;
                }
            }
            return true;
        }

        private bool StepPacketConvertIdentity(int take, int give, Plane? changedPlane)
        {
            var srcBind = Wiring.Bindings[0];
            var dstBind = Wiring.Bindings[1];
            var srcCap = srcBind.Planes.Count;
            var dstCap = dstBind.Planes.Count;
            if (take > srcCap || give > dstCap) return false;

            var srcStore = (Store)srcBind.Surface;
            var dstStore = (Store)dstBind.Surface;
            var srcPlanes = new Plane[srcCap];
            var srcWords = new ulong[srcCap][];
            for (var k = 0; k < srcCap; k++) { srcPlanes[k] = srcStore.RawPlane(srcBind.Plane(k))!; srcWords[k] = srcPlanes[k].RawWords; }
            var dstPlanes = new Plane[dstCap];
            var dstWords = new ulong[dstCap][];
            for (var k = 0; k < dstCap; k++) { dstPlanes[k] = dstStore.RawPlane(dstBind.Plane(k))!; dstWords[k] = dstPlanes[k].RawWords; }

            var runWords = staticRunnable.RawWords;
            var changedWords = changedPlane?.RawWords;
            var wordCount = runWords.Length;
            var vecWidth = Vector<ulong>.Count;
            var srcCheckIdx = take - 1;
            var dstRoomIdx = dstCap - give;
            var allOnes = Vector<ulong>.AllBitsSet;
            var changed = false;

            var w = 0;
            for (; w + vecWidth <= wordCount; w += vecWidth)
            {
                var fire = Vector.AndNot(
                    new Vector<ulong>(runWords, w) & new Vector<ulong>(srcWords[srcCheckIdx], w),
                    new Vector<ulong>(dstWords[dstRoomIdx], w));
                if (Vector.EqualsAll(fire, Vector<ulong>.Zero)) continue;
                changed = true;
                if (changedWords is not null) fire.CopyTo(changedWords, w);

                for (var j = 0; j < srcCap; j++)
                {
                    var cur = new Vector<ulong>(srcWords[j], w);
                    var next = j + take < srcCap ? new Vector<ulong>(srcWords[j + take], w) : Vector<ulong>.Zero;
                    (Vector.AndNot(cur, fire) | (next & fire)).CopyTo(srcWords[j], w);
                }
                for (var j = dstCap - 1; j >= 0; j--)
                {
                    var cur = new Vector<ulong>(dstWords[j], w);
                    var next = j >= give ? new Vector<ulong>(dstWords[j - give], w) : allOnes;
                    (Vector.AndNot(cur, fire) | (next & fire)).CopyTo(dstWords[j], w);
                }
            }

            for (; w < wordCount; w++)
            {
                var fire = runWords[w] & srcWords[srcCheckIdx][w] & ~dstWords[dstRoomIdx][w];
                if (fire == 0UL) continue;
                changed = true;
                if (changedWords is not null) changedWords[w] = fire;

                for (var j = 0; j < srcCap; j++)
                {
                    var next = j + take < srcCap ? srcWords[j + take][w] : 0UL;
                    srcWords[j][w] = (srcWords[j][w] & ~fire) | (next & fire);
                }
                for (var j = dstCap - 1; j >= 0; j--)
                {
                    var next = j >= give ? dstWords[j - give][w] : ulong.MaxValue;
                    dstWords[j][w] = (dstWords[j][w] & ~fire) | (next & fire);
                }
            }

            if (changed)
            {
                for (var k = 0; k < srcCap; k++) srcPlanes[k].TrimTail();
                for (var k = 0; k < dstCap; k++) dstPlanes[k].TrimTail();
                changedPlane?.TrimTail();
            }
            return changed;
        }

        private bool StepPacketConvertNonIdentity(int take, int give, Plane? changedPlane)
        {
            var law = (TransitionLaw)Wiring.Law;
            var srcBind = Wiring.Bindings[0];
            var dstBind = Wiring.Bindings[1];
            var srcStore = (Store)srcBind.Surface;
            var dstStore = (Store)dstBind.Surface;
            var srcField = new Field(law.Roles[0], [.. srcBind.Planes]);
            var dstField = new Field(law.Roles[1], [.. dstBind.Planes]);
            var dstCap = law.Roles[1].Size - 1;
            var changed = false;

            foreach (var row in staticRunnable.Indexes())
            {
                var sp = Wiring.Reference(row, 0);
                var dp = Wiring.Reference(row, 1);
                var s = srcField.Read(srcStore, sp)!.Value;
                var d = dstField.Read(dstStore, dp)!.Value;
                if (s >= take && d + give <= dstCap)
                {
                    srcField.Write(srcStore, sp, s - take);
                    dstField.Write(dstStore, dp, d + give);
                    if (changedPlane is not null) changedPlane[row] = true;
                    changed = true;
                }
            }
            return changed;
        }

        private bool StepBoundedMultiplyIdentity(int prodCap, Plane? changedPlane)
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

            bool aUnary = law.Roles[0].IsUnary, bUnary = law.Roles[1].IsUnary;
            bool vUnary = law.Roles[2].IsUnary, oUnary = law.Roles[3].IsUnary;
            int aBits = aBind.Planes.Count, bBits = bBind.Planes.Count;
            int vBits = vBind.Planes.Count, oBits = oBind.Planes.Count;

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
            var changedWords = changedPlane?.RawWords;
            var wordCount = runWords.Length;
            var cap = (ulong)prodCap;
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
                    var val = prod <= cap ? (int)prod : prodCap;
                    var ovf = prod > cap ? (int)(prod - cap) : 0;

                    if (vUnary) { for (var k = 0; k < val; k++) vNext[k] |= bitMask; }
                    else { for (var k = 0; k < vBits; k++) if (((val >> k) & 1) != 0) vNext[k] |= bitMask; }

                    if (oUnary) { for (var k = 0; k < ovf; k++) oNext[k] |= bitMask; }
                    else { for (var k = 0; k < oBits; k++) if (((ovf >> k) & 1) != 0) oNext[k] |= bitMask; }
                }

                ulong diff = 0;
                for (var k = 0; k < vBits; k++)
                {
                    var d = vWords[k][w] ^ vNext[k];
                    if (d != 0) { vWords[k][w] = vNext[k]; diff |= d; }
                }
                for (var k = 0; k < oBits; k++)
                {
                    var d = oWords[k][w] ^ oNext[k];
                    if (d != 0) { oWords[k][w] = oNext[k]; diff |= d; }
                }
                if (diff != 0)
                {
                    changed = true;
                    if (changedWords is not null) changedWords[w] = diff;
                }
            }

            if (changed)
            {
                for (var k = 0; k < vBits; k++) vPlanes[k].TrimTail();
                for (var k = 0; k < oBits; k++) oPlanes[k].TrimTail();
                changedPlane?.TrimTail();
            }
            return changed;
        }

        private bool StepSimdIdentity(Plane? changedPlane)
        {
            if (changingStates.Length == 0) return false;
            var runWords = staticRunnable.RawWords;
            var changedWords = changedPlane?.RawWords;
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

                var anyFlip = Vector<ulong>.Zero;
                for (var wi = 0; wi < writeFlatBits.Length; wi++)
                {
                    var flip = flipVecScratch[wi];
                    if (Vector.EqualsAll(flip, Vector<ulong>.Zero)) continue;
                    changed = true;
                    anyFlip |= flip;
                    (inVecScratch[writeFlatBits[wi]] ^ flip).CopyTo(writeWords[wi], w);
                }
                if (changedWords is not null && !Vector.EqualsAll(anyFlip, Vector<ulong>.Zero))
                    anyFlip.CopyTo(changedWords, w);
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

                ulong anyFlip = 0UL;
                for (var wi = 0; wi < writeFlatBits.Length; wi++)
                {
                    var flip = flipScalarScratch[wi];
                    if (flip == 0UL) continue;
                    changed = true;
                    anyFlip |= flip;
                    writeWords[wi][w] = inScalarScratch[writeFlatBits[wi]] ^ flip;
                }
                if (changedWords is not null && anyFlip != 0UL)
                    changedWords[w] = anyFlip;
            }

            if (changed)
            {
                for (var wi = 0; wi < writeTargets.Length; wi++) writeTargets[wi].TrimTail();
                changedPlane?.TrimTail();
            }
            return changed;
        }
    }

    // ---- Relational column kernel (Pass-1 Ladder.Add single & multi-rung SWAR + exact factorized narrowing) ----

    private sealed class RolePlanes
    {
        public RolePlanes(Store store, Binding binding, Shape shape, bool writes, int[] planes, bool constantPlain, int plainConstValue)
        {
            Store = store;
            IsOpen = binding.IsOpen;
            Shape = shape;
            Size = shape.Size;
            Writes = writes;
            Planes = planes;
            ConstantPlain = constantPlain;
            PlainConstValue = plainConstValue;
        }

        public Store Store { get; }
        public bool IsOpen { get; }
        public Shape Shape { get; }
        public int Size { get; }
        public bool Writes { get; }
        public int[] Planes { get; }
        public bool ConstantPlain { get; }
        public int PlainConstValue { get; }

        public ulong[]?[] ResolveRawWords()
        {
            var raw = new ulong[]?[Planes.Length];
            for (var i = 0; i < Planes.Length; i++) raw[i] = Store.RawPlane(Planes[i])?.RawWords;
            return raw;
        }

        public Plane?[] ResolvePlanes()
        {
            var pls = new Plane?[Planes.Length];
            for (var i = 0; i < Planes.Length; i++) pls[i] = Store.RawPlane(Planes[i]);
            return pls;
        }

        public int CurrentPlainConstant()
        {
            ulong pat = 0;
            for (var k = 0; k < Planes.Length; k++)
                if (Store.ConstantValue(Planes[k])) pat |= 1UL << k;
            return Shape.Decode(pat) ?? -1;
        }
    }

    private sealed class RelationalColumnPlan
    {
        private readonly Wiring wiring;
        private readonly Plane staticRunnable;
        private readonly Plane staticSkipped;
        private readonly RolePlanes a, b, cin, cout, sum;
        private readonly int radix;
        private readonly int maxT;
        private readonly bool canSwar4;

        private RelationalColumnPlan(
            Wiring wiring,
            int radix,
            Plane staticRunnable,
            Plane staticSkipped,
            RolePlanes a,
            RolePlanes b,
            RolePlanes cin,
            RolePlanes cout,
            RolePlanes sum)
        {
            this.wiring = wiring;
            this.radix = radix;
            this.staticRunnable = staticRunnable;
            this.staticSkipped = staticSkipped;
            this.a = a;
            this.b = b;
            this.cin = cin;
            this.cout = cout;
            this.sum = sum;
            var lhsMax = a.Size + b.Size - 1;
            var rhsMax = 2 * radix - 1;
            maxT = lhsMax < rhsMax ? lhsMax : rhsMax;
            canSwar4 = a.Size <= 16 && b.Size <= 16 && radix <= 16;
        }

        public Wiring Wiring => wiring;
        public Store PrimaryStore => a.Store;
        public Plane StaticRunnable => staticRunnable;
        public Plane StaticSkipped => staticSkipped;
        public RolePlanes A => a;
        public RolePlanes B => b;
        public RolePlanes Cin => cin;
        public RolePlanes Cout => cout;
        public RolePlanes Sum => sum;
        public int Radix => radix;
        public bool CanSwar4 => canSwar4;
        public bool AllSameStore =>
            ReferenceEquals(a.Store, b.Store) && ReferenceEquals(a.Store, cin.Store)
            && ReferenceEquals(a.Store, cout.Store) && ReferenceEquals(a.Store, sum.Store);

        public static RelationalColumnPlan? TryBuild(Wiring wiring, int radix, Plane staticRunnable, Plane staticSkipped)
        {
            if (wiring.Bindings.Count != 5) return null;
            var law = (RelationLaw)wiring.Law;

            // Every role must have pairwise-disjoint (Store, Plane) pairs so SameReference is provably empty.
            var seen = new HashSet<(Surface, int)>();
            var roles = new RolePlanes[5];
            for (var r = 0; r < 5; r++)
            {
                var bind = wiring.Bindings[r];
                var st = (Store)bind.Surface;
                var writes = wiring.Writes(r);
                var planes = bind.Planes.ToArray();
                for (var p = 0; p < planes.Length; p++)
                {
                    if (!seen.Add((st, planes[p]))) return null;
                    if (writes && st.IsConstantPlane(planes[p])) return null;
                }

                if (bind.IsOpen)
                {
                    roles[r] = new RolePlanes(st, bind, law.Roles[r], writes, planes, constantPlain: false, plainConstValue: -1);
                }
                else
                {
                    // Plain binding: allow all-constant code (such as constant carry 0) or single-bit Carry.
                    var allConst = planes.All(st.IsConstantPlane);
                    if (allConst)
                    {
                        ulong pat = 0;
                        for (var k = 0; k < planes.Length; k++)
                            if (st.ConstantValue(planes[k])) pat |= 1UL << k;
                        if (law.Roles[r].Decode(pat) is not int v) return null;
                        roles[r] = new RolePlanes(st, bind, law.Roles[r], writes: false, planes, constantPlain: true, plainConstValue: v);
                    }
                    else if (law.Roles[r].Size == 2 && planes.Length == 1)
                    {
                        roles[r] = new RolePlanes(st, bind, law.Roles[r], writes: false, planes, constantPlain: false, plainConstValue: -1);
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            return new RelationalColumnPlan(wiring, radix, staticRunnable, staticSkipped,
                roles[0], roles[1], roles[2], roles[3], roles[4]);
        }

        public bool TryStepFastOnly(out bool changed)
        {
            changed = false;
            var resolved = ResolveAll();
            if (!HasValidConstants(resolved)) return false;

            var runWords = staticRunnable.RawWords;
            var wordCount = runWords.Length;
            var buf = new ColumnWordScratch(a.Size, b.Size, radix, maxT);

            for (var w = 0; w < wordCount; w++)
            {
                var active = runWords[w];
                if (active == 0UL) continue;
                var ch = NarrowWord(w, active, resolved, ref buf, out _, out _);
                if (ch != 0UL) changed = true;
            }
            if (changed) TrimWritable(resolved);
            return true;
        }

        public bool TryStepWithResult(bool forTogether, out WiringStep? step)
        {
            step = null;
            var resolved = ResolveAll();
            if (!HasValidConstants(resolved)) return false;

            var count = wiring.Count;
            var changedPlane = new Plane(count);
            var unadmittedPlane = new Plane(count);
            var ambiguousPlane = new Plane(count);
            var runWords = staticRunnable.RawWords;
            var chWords = changedPlane.RawWords;
            var unWords = unadmittedPlane.RawWords;
            var amWords = ambiguousPlane.RawWords;
            var wordCount = runWords.Length;
            var buf = new ColumnWordScratch(a.Size, b.Size, radix, maxT);
            var anyChanged = false;

            for (var w = 0; w < wordCount; w++)
            {
                var active = runWords[w];
                if (active == 0UL) continue;
                var ch = NarrowWord(w, active, resolved, ref buf, out var un, out var am);
                chWords[w] = ch;
                unWords[w] = un;
                amWords[w] = am;
                if (ch != 0UL) anyChanged = true;
            }
            if (anyChanged) TrimWritable(resolved);
            changedPlane.TrimTail();
            unadmittedPlane.TrimTail();
            ambiguousPlane.TrimTail();

            var empty = new Plane(count);
            var none = Enumerable.Repeat(-1, count).ToArray();
            var result = new WiringStep(
                changedPlane,
                empty,
                unadmittedPlane,
                ambiguousPlane,
                staticSkipped.Clone(),
                empty.Clone(),
                none,
                (int[])none.Clone(),
                staticRunnable.Any ? [staticRunnable.Clone()] : []);
            if (forTogether) result = result with { Contested = new Plane(count) };
            step = result;
            return true;
        }

        internal bool HasValidConstants(in ResolvedColumn r) =>
            (!a.ConstantPlain || r.AConst >= 0)
            && (!b.ConstantPlain || r.BConst >= 0)
            && (!cin.ConstantPlain || r.CinConst >= 0)
            && (!cout.ConstantPlain || r.CoutConst >= 0)
            && (!sum.ConstantPlain || r.SumConst >= 0);

        internal ResolvedColumn ResolveAll() => new(
            a.ResolveRawWords(), a.ResolvePlanes(), a.ConstantPlain ? a.CurrentPlainConstant() : -1,
            b.ResolveRawWords(), b.ResolvePlanes(), b.ConstantPlain ? b.CurrentPlainConstant() : -1,
            cin.ResolveRawWords(), cin.ResolvePlanes(), cin.ConstantPlain ? cin.CurrentPlainConstant() : -1,
            cout.ResolveRawWords(), cout.ResolvePlanes(), cout.ConstantPlain ? cout.CurrentPlainConstant() : -1,
            sum.ResolveRawWords(), sum.ResolvePlanes(), sum.ConstantPlain ? sum.CurrentPlainConstant() : -1);

        internal void TrimWritable(in ResolvedColumn r)
        {
            if (a.Writes) TrimArray(r.APlanes);
            if (b.Writes) TrimArray(r.BPlanes);
            if (cin.Writes) TrimArray(r.CinPlanes);
            if (cout.Writes) TrimArray(r.CoutPlanes);
            if (sum.Writes) TrimArray(r.SumPlanes);
        }

        private static void TrimArray(Plane?[] planes)
        {
            for (var i = 0; i < planes.Length; i++) planes[i]?.TrimTail();
        }

        internal ulong NarrowWord(int w, ulong active, in ResolvedColumn r, ref ColumnWordScratch buf, out ulong unadmitted, out ulong ambiguous)
        {
            // Load role value masks for word w.
            LoadRole(a, r.AWords, r.AConst, w, active, buf.A);
            LoadRole(b, r.BWords, r.BConst, w, active, buf.B);
            LoadRole(cin, r.CinWords, r.CinConst, w, active, buf.Cin);
            LoadRole(cout, r.CoutWords, r.CoutConst, w, active, buf.Cout);
            LoadRole(sum, r.SumWords, r.SumConst, w, active, buf.S);

            // Fast 64-lane SWAR singleton path when inputs (a, b, cin) are all settled in word w.
            if (canSwar4 && TrySingleton4(buf.A, a.Size, active, out var a0, out var a1, out var a2, out var a3)
                && TrySingleton4(buf.B, b.Size, active, out var b0, out var b1, out var b2, out var b3)
                && TrySingleton2(buf.Cin, active, out var ciBit))
            {
                AddSwar4(a0, a1, a2, a3, b0, b1, b2, b3, ciBit, radix,
                    checkOvf: (a.Size - 1) + (b.Size - 1) + 1 >= 2 * radix,
                    out var r0, out var r1, out var r2, out var r3, out var coBit, out var ovfMask);
                if ((ovfMask & active) == 0UL)
                {
                    Decode4(r0, r1, r2, r3, radix, buf.NewS);
                    ulong validS = 0;
                    for (var v = 0; v < radix; v++) validS |= buf.NewS[v] & buf.S[v];
                    ulong validCo = (~coBit & buf.Cout[0]) | (coBit & buf.Cout[1]);
                    if ((validS & validCo & active) == active)
                    {
                        var ch = 0UL;
                        if (sum.Writes)
                            for (var v = 0; v < radix; v++)
                                ch |= NarrowPlaneWord(r.SumWords[v]!, w, buf.NewS[v], active);
                        if (cout.Writes)
                        {
                            ch |= NarrowPlaneWord(r.CoutWords[0]!, w, ~coBit, active);
                            ch |= NarrowPlaneWord(r.CoutWords[1]!, w, coBit, active);
                        }
                        unadmitted = 0UL;
                        ambiguous = 0UL;
                        return ch;
                    }
                }
            }

            // General exact 64-lane bitwise factorized narrowing: AB[a+b] -> ValidT[a+b+cin == p*cout+s].
            var ab = buf.AB;
            var validT = buf.ValidT;
            var needAB = buf.NeedAB;
            for (var u = 0; u <= maxT; u++) ab[u] = 0UL;

            var na = a.Size;
            var nb = b.Size;
            for (var av = 0; av < na; av++)
            {
                var am = buf.A[av];
                if (am == 0UL) continue;
                var limB = maxT - av;
                if (limB >= nb) limB = nb - 1;
                for (var bv = 0; bv <= limB; bv++)
                {
                    var bm = buf.B[bv];
                    if (bm != 0UL) ab[av + bv] |= am & bm;
                }
            }

            ulong c0 = buf.Cin[0], c1 = buf.Cin[1];
            ulong co0 = buf.Cout[0], co1 = buf.Cout[1];
            ulong anyValid = 0UL;
            for (var t = 0; t <= maxT; t++)
            {
                var fromLeft = (ab[t] & c0) | (t > 0 ? (ab[t - 1] & c1) : 0UL);
                if (fromLeft == 0UL) { validT[t] = 0UL; continue; }
                var fromRight = t < radix ? (buf.S[t] & co0) : (buf.S[t - radix] & co1);
                var vt = fromLeft & fromRight;
                validT[t] = vt;
                anyValid |= vt;
            }

            // Project back to Sum and Cout.
            ulong newCo0 = 0UL, newCo1 = 0UL;
            ulong sSeen = 0UL, sDup = 0UL;
            for (var sv = 0; sv < radix; sv++)
            {
                var v0 = sv <= maxT ? validT[sv] : 0UL;
                var v1 = sv + radix <= maxT ? validT[sv + radix] : 0UL;
                var ns = v0 | v1;
                buf.NewS[sv] = ns;
                sDup |= sSeen & ns;
                sSeen |= ns;
                newCo0 |= v0;
                newCo1 |= v1;
            }

            // Project back to Cin.
            ulong newCi0 = 0UL, newCi1 = 0UL;
            for (var t = 0; t <= maxT; t++)
            {
                var vt = validT[t];
                if (vt == 0UL) continue;
                newCi0 |= vt & ab[t] & c0;
                if (t > 0) newCi1 |= vt & ab[t - 1] & c1;
            }

            // Project back to A and B via NeedAB.
            for (var u = 0; u <= maxT; u++)
                needAB[u] = (validT[u] & c0) | (u + 1 <= maxT ? (validT[u + 1] & c1) : 0UL);

            for (var av = 0; av < na; av++) buf.NewA[av] = 0UL;
            for (var bv = 0; bv < nb; bv++) buf.NewB[bv] = 0UL;
            for (var av = 0; av < na; av++)
            {
                var am = buf.A[av];
                if (am == 0UL) continue;
                var limB = maxT - av;
                if (limB >= nb) limB = nb - 1;
                ulong accA = 0UL;
                for (var bv = 0; bv <= limB; bv++)
                {
                    var m = am & buf.B[bv] & needAB[av + bv];
                    if (m == 0UL) continue;
                    accA |= m;
                    buf.NewB[bv] |= m;
                }
                buf.NewA[av] = accA;
            }

            // Write back only writable open roles and track changed / unadmitted / ambiguous masks.
            ulong changed = 0UL;
            ulong open = 0UL;

            if (a.Writes)
                for (var av = 0; av < na; av++) changed |= NarrowPlaneWord(r.AWords[av]!, w, buf.NewA[av], active);
            if (a.IsOpen) open |= MultiBitMask(buf.NewA, na);

            if (b.Writes)
                for (var bv = 0; bv < nb; bv++) changed |= NarrowPlaneWord(r.BWords[bv]!, w, buf.NewB[bv], active);
            if (b.IsOpen) open |= MultiBitMask(buf.NewB, nb);

            if (cin.Writes)
            {
                changed |= NarrowPlaneWord(r.CinWords[0]!, w, newCi0, active);
                changed |= NarrowPlaneWord(r.CinWords[1]!, w, newCi1, active);
            }
            if (cin.IsOpen) open |= newCi0 & newCi1;

            if (cout.Writes)
            {
                changed |= NarrowPlaneWord(r.CoutWords[0]!, w, newCo0, active);
                changed |= NarrowPlaneWord(r.CoutWords[1]!, w, newCo1, active);
            }
            if (cout.IsOpen) open |= newCo0 & newCo1;

            if (sum.Writes)
                for (var sv = 0; sv < radix; sv++) changed |= NarrowPlaneWord(r.SumWords[sv]!, w, buf.NewS[sv], active);
            if (sum.IsOpen) open |= sDup;

            unadmitted = active & ~anyValid;
            ambiguous = active & anyValid & open;
            return changed;
        }

        internal static void LoadRole(RolePlanes role, ulong[]?[] words, int constVal, int w, ulong active, ulong[] dst)
        {
            if (role.IsOpen)
            {
                for (var v = 0; v < role.Size; v++) dst[v] = words[v]![w] & active;
                return;
            }
            if (role.ConstantPlain)
            {
                for (var v = 0; v < role.Size; v++) dst[v] = v == constVal ? active : 0UL;
                return;
            }
            // Single-bit plain Carry binding.
            var bit = words[0]![w];
            dst[0] = ~bit & active;
            dst[1] = bit & active;
        }

        internal static ulong NarrowPlaneWord(ulong[] dst, int w, ulong keep, ulong active)
        {
            var old = dst[w];
            var next = old & (~active | keep);
            var diff = old ^ next;
            if (diff != 0UL) dst[w] = next;
            return diff;
        }

        internal static bool TrySingleton4(ulong[] masks, int size, ulong active,
            out ulong b0, out ulong b1, out ulong b2, out ulong b3)
        {
            ulong seen = 0, dup = 0;
            b0 = b1 = b2 = b3 = 0;
            for (var v = 0; v < size; v++)
            {
                var m = masks[v];
                if (m == 0UL) continue;
                dup |= seen & m;
                seen |= m;
                if ((v & 1) != 0) b0 |= m;
                if ((v & 2) != 0) b1 |= m;
                if ((v & 4) != 0) b2 |= m;
                if ((v & 8) != 0) b3 |= m;
            }
            return ((seen & ~dup) & active) == active;
        }

        internal static bool TrySingleton2(ulong[] masks, ulong active, out ulong bit1)
        {
            ulong m0 = masks[0], m1 = masks[1];
            bit1 = m1;
            return (((m0 ^ m1) & active) == active);
        }

        internal static void AddSwar4(
            ulong a0, ulong a1, ulong a2, ulong a3,
            ulong b0, ulong b1, ulong b2, ulong b3,
            ulong carry, int p, bool checkOvf,
            out ulong r0, out ulong r1, out ulong r2, out ulong r3,
            out ulong cout, out ulong ovfMask)
        {
            ulong s0 = a0 ^ b0 ^ carry;
            ulong c0 = (a0 & b0) | (carry & (a0 ^ b0));
            ulong s1 = a1 ^ b1 ^ c0;
            ulong c1 = (a1 & b1) | (c0 & (a1 ^ b1));
            ulong s2 = a2 ^ b2 ^ c1;
            ulong c2 = (a2 & b2) | (c1 & (a2 ^ b2));
            ulong s3 = a3 ^ b3 ^ c2;
            ulong s4 = (a3 & b3) | (c2 & (a3 ^ b3));

            ovfMask = checkOvf ? Ge5(s0, s1, s2, s3, s4, 2 * p) : 0UL;

            if (p == 16)
            {
                cout = s4;
                r0 = s0; r1 = s1; r2 = s2; r3 = s3;
                return;
            }
            if (p == 8)
            {
                cout = s3;
                r0 = s0; r1 = s1; r2 = s2; r3 = 0UL;
                return;
            }
            if (p == 4)
            {
                cout = s2;
                r0 = s0; r1 = s1; r2 = 0UL; r3 = 0UL;
                return;
            }
            if (p == 2)
            {
                cout = s1;
                r0 = s0; r1 = 0UL; r2 = 0UL; r3 = 0UL;
                return;
            }

            // General non-power-of-two partition p in {3, 5, 6, 7, 9, 10, 11, 12, 13, 14, 15}:
            // t = s + (32 - p); carry out of bit 4 is 1 iff s >= p, and lower 4 bits equal s - p.
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

        internal static void Decode4(ulong r0, ulong r1, ulong r2, ulong r3, int p, ulong[] dst)
        {
            ulong nr0 = ~r0, nr1 = ~r1;
            ulong q0 = nr0 & nr1, q1 = r0 & nr1, q2 = nr0 & r1, q3 = r0 & r1;
            if (p <= 4)
            {
                dst[0] = q0;
                if (p > 1) dst[1] = q1;
                if (p > 2) dst[2] = q2;
                if (p > 3) dst[3] = q3;
                return;
            }
            ulong nr2 = ~r2, nr3 = ~r3;
            ulong h0 = nr2 & nr3, h1 = r2 & nr3, h2 = nr2 & r3, h3 = r2 & r3;
            for (var v = 0; v < p; v++)
            {
                var q = (v & 3) switch { 0 => q0, 1 => q1, 2 => q2, _ => q3 };
                var h = (v >> 2) switch { 0 => h0, 1 => h1, 2 => h2, _ => h3 };
                dst[v] = q & h;
            }
        }

        private static ulong Ge5(ulong s0, ulong s1, ulong s2, ulong s3, ulong s4, int threshold)
        {
            if (threshold >= 32) return 0UL;
            var k = 32 - threshold;
            ulong k0 = (k & 1) != 0 ? ~0UL : 0UL;
            ulong k1 = (k & 2) != 0 ? ~0UL : 0UL;
            ulong k2 = (k & 4) != 0 ? ~0UL : 0UL;
            ulong k3 = (k & 8) != 0 ? ~0UL : 0UL;
            ulong k4 = (k & 16) != 0 ? ~0UL : 0UL;
            ulong c0 = s0 & k0;
            ulong c1 = (s1 & k1) | (c0 & (s1 ^ k1));
            ulong c2 = (s2 & k2) | (c1 & (s2 ^ k2));
            ulong c3 = (s3 & k3) | (c2 & (s3 ^ k3));
            return (s4 & k4) | (c3 & (s4 ^ k4));
        }

        private static ulong MultiBitMask(ulong[] masks, int size)
        {
            ulong seen = 0, dup = 0;
            for (var v = 0; v < size; v++)
            {
                var m = masks[v];
                dup |= seen & m;
                seen |= m;
            }
            return dup;
        }
    }

    private readonly record struct ResolvedColumn(
        ulong[]?[] AWords, Plane?[] APlanes, int AConst,
        ulong[]?[] BWords, Plane?[] BPlanes, int BConst,
        ulong[]?[] CinWords, Plane?[] CinPlanes, int CinConst,
        ulong[]?[] CoutWords, Plane?[] CoutPlanes, int CoutConst,
        ulong[]?[] SumWords, Plane?[] SumPlanes, int SumConst);

    private struct ColumnWordScratch
    {
        public readonly ulong[] A;
        public readonly ulong[] B;
        public readonly ulong[] Cin;
        public readonly ulong[] Cout;
        public readonly ulong[] S;
        public readonly ulong[] NewA;
        public readonly ulong[] NewB;
        public readonly ulong[] NewS;
        public readonly ulong[] AB;
        public readonly ulong[] ValidT;
        public readonly ulong[] NeedAB;

        public ColumnWordScratch(int na, int nb, int radix, int maxT)
        {
            A = new ulong[na];
            B = new ulong[nb];
            Cin = new ulong[2];
            Cout = new ulong[2];
            S = new ulong[radix];
            NewA = new ulong[na];
            NewB = new ulong[nb];
            NewS = new ulong[radix];
            AB = new ulong[maxT + 1];
            ValidT = new ulong[maxT + 1];
            NeedAB = new ulong[maxT + 1];
        }
    }

    // ---- Fused Pass-1 relational Ladder.Add chain (64-lane SWAR ripple + exact per-column fallback) ----

    private sealed class FusedRelationalLadderAdder
    {
        private readonly Store store;
        private readonly RelationalColumnPlan[] rungs;
        private readonly Plane staticRunnable;
        private readonly int staticRunnableCount;
        private readonly bool allSwar4;

        public FusedRelationalLadderAdder(Store store, RelationalColumnPlan[] rungs, Plane staticRunnable)
        {
            this.store = store;
            this.rungs = rungs;
            this.staticRunnable = staticRunnable;
            staticRunnableCount = staticRunnable.Count;
            allSwar4 = rungs.All(r => r.CanSwar4);
        }

        public (int Fast, int Deopt, bool Changed) StepFastOnly()
        {
            if (staticRunnableCount == 0) return (0, 0, false);
            if (!LiveCovers(store.RawLive, staticRunnable)) return (0, staticRunnableCount, false);

            var k = rungs.Length;
            var resolved = new ResolvedColumn[k];
            var scratch = new ColumnWordScratch[k];
            for (var i = 0; i < k; i++)
            {
                resolved[i] = rungs[i].ResolveAll();
                if (!rungs[i].HasValidConstants(resolved[i])) return (0, staticRunnableCount, false);
                var na = rungs[i].A.Size;
                var nb = rungs[i].B.Size;
                var p = rungs[i].Radix;
                var maxT = (na + nb - 1) < (2 * p - 1) ? (na + nb - 1) : (2 * p - 1);
                scratch[i] = new ColumnWordScratch(na, nb, p, maxT);
            }

            var runWords = staticRunnable.RawWords;
            var wordCount = runWords.Length;
            var specSum = allSwar4 ? new ulong[k][] : [];
            var specCarry = allSwar4 ? new ulong[k] : [];
            if (allSwar4)
                for (var i = 0; i < k; i++) specSum[i] = new ulong[rungs[i].Radix];

            var changed = false;
            for (var w = 0; w < wordCount; w++)
            {
                var active = runWords[w];
                if (active == 0UL) continue;

                if (allSwar4 && TryRunFusedWord(w, active, resolved, scratch, specSum, specCarry, changedPlanes: null, out var wordChanged))
                {
                    if (wordChanged) changed = true;
                    continue;
                }

                for (var i = 0; i < k; i++)
                {
                    var ch = rungs[i].NarrowWord(w, active, resolved[i], ref scratch[i], out _, out _);
                    if (ch != 0UL) changed = true;
                }
            }

            if (changed)
                for (var i = 0; i < k; i++) rungs[i].TrimWritable(resolved[i]);

            return (staticRunnableCount, 0, changed);
        }

        public (WiringStep[] Steps, int Fast, int Deopt) StepWithResults()
        {
            var k = rungs.Length;
            if (staticRunnableCount == 0)
            {
                var emptySteps = new WiringStep[k];
                for (var i = 0; i < k; i++)
                {
                    if (!rungs[i].TryStepWithResult(forTogether: false, out var emptyStep))
                        return ([], 0, 1);
                    emptySteps[i] = emptyStep!;
                }
                return (emptySteps, 0, 0);
            }
            if (!LiveCovers(store.RawLive, staticRunnable)) return ([], 0, staticRunnableCount);

            var resolved = new ResolvedColumn[k];
            var scratch = new ColumnWordScratch[k];
            var changedPlanes = new Plane[k];
            var unadmittedPlanes = new Plane[k];
            var ambiguousPlanes = new Plane[k];
            for (var i = 0; i < k; i++)
            {
                resolved[i] = rungs[i].ResolveAll();
                if (!rungs[i].HasValidConstants(resolved[i])) return ([], 0, staticRunnableCount);
                var na = rungs[i].A.Size;
                var nb = rungs[i].B.Size;
                var p = rungs[i].Radix;
                var maxT = (na + nb - 1) < (2 * p - 1) ? (na + nb - 1) : (2 * p - 1);
                scratch[i] = new ColumnWordScratch(na, nb, p, maxT);
                var count = rungs[i].Wiring.Count;
                changedPlanes[i] = new Plane(count);
                unadmittedPlanes[i] = new Plane(count);
                ambiguousPlanes[i] = new Plane(count);
            }

            var runWords = staticRunnable.RawWords;
            var wordCount = runWords.Length;
            var specSum = allSwar4 ? new ulong[k][] : [];
            var specCarry = allSwar4 ? new ulong[k] : [];
            if (allSwar4)
                for (var i = 0; i < k; i++) specSum[i] = new ulong[rungs[i].Radix];

            var anyChanged = false;
            for (var w = 0; w < wordCount; w++)
            {
                var active = runWords[w];
                if (active == 0UL) continue;

                if (allSwar4 && TryRunFusedWord(w, active, resolved, scratch, specSum, specCarry, changedPlanes, out var wordChanged))
                {
                    if (wordChanged) anyChanged = true;
                    continue;
                }

                for (var i = 0; i < k; i++)
                {
                    var ch = rungs[i].NarrowWord(w, active, resolved[i], ref scratch[i], out var un, out var am);
                    changedPlanes[i].RawWords[w] = ch;
                    unadmittedPlanes[i].RawWords[w] = un;
                    ambiguousPlanes[i].RawWords[w] = am;
                    if (ch != 0UL) anyChanged = true;
                }
            }

            if (anyChanged)
                for (var i = 0; i < k; i++) rungs[i].TrimWritable(resolved[i]);

            var steps = new WiringStep[k];
            for (var i = 0; i < k; i++)
            {
                changedPlanes[i].TrimTail();
                unadmittedPlanes[i].TrimTail();
                ambiguousPlanes[i].TrimTail();
                var count = rungs[i].Wiring.Count;
                var empty = new Plane(count);
                var none = Enumerable.Repeat(-1, count).ToArray();
                steps[i] = new WiringStep(
                    changedPlanes[i],
                    empty,
                    unadmittedPlanes[i],
                    ambiguousPlanes[i],
                    rungs[i].StaticSkipped.Clone(),
                    empty.Clone(),
                    none,
                    (int[])none.Clone(),
                    [rungs[i].StaticRunnable.Clone()]);
            }
            return (steps, staticRunnableCount, 0);
        }

        private bool TryRunFusedWord(
            int w,
            ulong active,
            ResolvedColumn[] resolved,
            ColumnWordScratch[] scratch,
            ulong[][] specSum,
            ulong[] specCarry,
            Plane[]? changedPlanes,
            out bool wordChanged)
        {
            wordChanged = false;
            var k = rungs.Length;

            // Check initial carry-in on rung 0.
            RelationalColumnPlan.LoadRole(rungs[0].Cin, resolved[0].CinWords, resolved[0].CinConst, w, active, scratch[0].Cin);
            if (!RelationalColumnPlan.TrySingleton2(scratch[0].Cin, active, out var carry)) return false;

            // Speculatively ripple across all k rungs in 64-lane SWAR registers, checking compatibility with S and Cout at each rung.
            for (var i = 0; i < k; i++)
            {
                var rung = rungs[i];
                var r = resolved[i];
                var sc = scratch[i];

                RelationalColumnPlan.LoadRole(rung.A, r.AWords, r.AConst, w, active, sc.A);
                if (!RelationalColumnPlan.TrySingleton4(sc.A, rung.A.Size, active, out var a0, out var a1, out var a2, out var a3))
                    return false;

                RelationalColumnPlan.LoadRole(rung.B, r.BWords, r.BConst, w, active, sc.B);
                if (!RelationalColumnPlan.TrySingleton4(sc.B, rung.B.Size, active, out var b0, out var b1, out var b2, out var b3))
                    return false;

                RelationalColumnPlan.LoadRole(rung.Sum, r.SumWords, r.SumConst, w, active, sc.S);
                RelationalColumnPlan.LoadRole(rung.Cout, r.CoutWords, r.CoutConst, w, active, sc.Cout);

                var p = rung.Radix;
                RelationalColumnPlan.AddSwar4(a0, a1, a2, a3, b0, b1, b2, b3, carry, p,
                    checkOvf: (rung.A.Size - 1) + (rung.B.Size - 1) + 1 >= 2 * p,
                    out var r0, out var r1, out var r2, out var r3, out var cout, out var ovfMask);
                if ((ovfMask & active) != 0UL) return false;

                var decoded = specSum[i];
                RelationalColumnPlan.Decode4(r0, r1, r2, r3, p, decoded);
                ulong validS = 0UL;
                for (var v = 0; v < p; v++) validS |= decoded[v] & sc.S[v];
                ulong validCo = (~cout & sc.Cout[0]) | (cout & sc.Cout[1]);
                if ((validS & validCo & active) != active) return false;

                specCarry[i] = cout;
                carry = cout;
            }

            // All k rungs verified compatible on every active lane in word w: commit narrowed supports.
            for (var i = 0; i < k; i++)
            {
                var rung = rungs[i];
                var r = resolved[i];
                var p = rung.Radix;
                var decoded = specSum[i];
                var cout = specCarry[i];
                ulong ch = 0UL;

                if (rung.Sum.Writes)
                    for (var v = 0; v < p; v++)
                        ch |= RelationalColumnPlan.NarrowPlaneWord(r.SumWords[v]!, w, decoded[v], active);
                if (rung.Cout.Writes)
                {
                    ch |= RelationalColumnPlan.NarrowPlaneWord(r.CoutWords[0]!, w, ~cout, active);
                    ch |= RelationalColumnPlan.NarrowPlaneWord(r.CoutWords[1]!, w, cout, active);
                }
                if (changedPlanes is not null) changedPlanes[i].RawWords[w] = ch;
                if (ch != 0UL) wordChanged = true;
            }
            return true;
        }
    }

    private static FusedRelationalLadderAdder? TryRecognizeRelationalLadder(Engine engine, CompiledWiring[] compiled)
    {
        if (compiled.Length < 2) return null;
        var rungs = new RelationalColumnPlan[compiled.Length];
        Store? store = null;
        Plane? sharedRunnable = null;
        var readPlanes = new HashSet<int>();
        var writtenPlanes = new HashSet<int>();

        for (var i = 0; i < compiled.Length; i++)
        {
            var col = compiled[i].ColumnPlan;
            if (col is null || !compiled[i].CanAccelerate || !col.AllSameStore) return null;
            if (store is null) store = col.PrimaryStore;
            else if (!ReferenceEquals(store, col.PrimaryStore)) return null;

            if (sharedRunnable is null) sharedRunnable = col.StaticRunnable;
            else if (!sharedRunnable.Equals(col.StaticRunnable)) return null;

            if (i == 0)
            {
                foreach (var p in col.Cin.Planes) readPlanes.Add(p);
            }
            else
            {
                var prevCout = rungs[i - 1].Cout;
                if (!col.Cin.IsOpen || !prevCout.IsOpen || !col.Cin.Planes.SequenceEqual(prevCout.Planes))
                    return null;
            }

            foreach (var p in col.A.Planes) readPlanes.Add(p);
            foreach (var p in col.B.Planes) readPlanes.Add(p);

            foreach (var p in col.Sum.Planes)
                if (!writtenPlanes.Add(p)) return null;
            if (col.Cout.Writes)
                foreach (var p in col.Cout.Planes)
                    if (!writtenPlanes.Add(p)) return null;

            rungs[i] = col;
        }

        // Outputs and carries must never alias any operand or initial carry-in plane.
        foreach (var p in writtenPlanes)
            if (readPlanes.Contains(p)) return null;

        return new FusedRelationalLadderAdder(store!, rungs, sharedRunnable!);
    }

    // ---- Fused concrete TransitionLaw mixed-partition ripple adder (64-lane SWAR) ----

    private sealed class FusedConcreteLadderAdder
    {
        private readonly Wiring[] wirings;
        private readonly Store store;
        private readonly int[] partitions;
        private readonly Field[] a;
        private readonly Field[] b;
        private readonly Field[] sum;
        private readonly int cinPlane;
        private readonly int coutPlane;
        private readonly int[] carries;
        private readonly Plane staticRunnable;
        private readonly Plane[] staticSkipped;
        private readonly bool[] rungSplit;
        private readonly bool[] rungSumFirst;
        private readonly int[] rungWiringOffset;
        private readonly int staticRunnableCount;
        private readonly bool isUnary;
        private readonly int[] rungBits;
        private readonly int[] rungOffset;
        private readonly int totalDigitBits;

        public FusedConcreteLadderAdder(
            Wiring[] wirings,
            Store store,
            int[] partitions,
            Field[] a,
            Field[] b,
            Field[] sum,
            int cinPlane,
            int coutPlane,
            int[] carries,
            Plane staticRunnable,
            Plane[] staticSkipped,
            bool[] rungSplit,
            bool[] rungSumFirst)
        {
            this.wirings = wirings;
            this.store = store;
            this.partitions = partitions;
            this.a = a;
            this.b = b;
            this.sum = sum;
            this.cinPlane = cinPlane;
            this.coutPlane = coutPlane;
            this.carries = carries;
            this.staticRunnable = staticRunnable;
            this.staticSkipped = staticSkipped;
            this.rungSplit = rungSplit;
            this.rungSumFirst = rungSumFirst;
            staticRunnableCount = staticRunnable.Count;
            isUnary = a[0].Shape.IsUnary;

            var k = a.Length;
            rungBits = new int[k];
            rungOffset = new int[k];
            rungWiringOffset = new int[k];
            var off = 0;
            var wOff = 0;
            for (var i = 0; i < k; i++)
            {
                rungOffset[i] = off;
                rungBits[i] = a[i].Planes.Count;
                off += rungBits[i];
                rungWiringOffset[i] = wOff;
                wOff += rungSplit[i] ? 2 : 1;
            }
            totalDigitBits = off;
        }

        public bool TryCaptureOldOutputs(out int[][]? oldSums, out bool[][]? oldCarries)
        {
            oldSums = null;
            oldCarries = null;
            if (!LiveCovers(store.RawLive, staticRunnable)) return false;
            var k = a.Length;
            var len = staticRunnable.Length;
            oldSums = new int[k][];
            oldCarries = new bool[k][];
            for (var i = 0; i < k; i++)
            {
                oldSums[i] = new int[len];
                oldCarries[i] = new bool[len];
                var cPlane = i < carries.Length ? carries[i] : coutPlane;
                foreach (var pos in staticRunnable.Indexes())
                {
                    if (sum[i].Read(store, pos) is not int sv) return false;
                    oldSums[i][pos] = sv;
                    oldCarries[i][pos] = store.Read(cPlane, pos);
                }
            }
            return true;
        }

        public WiringStep[] BuildWiringSteps(int[][] oldSums, bool[][] oldCarries, Plane[] changedPlanes)
        {
            var steps = new WiringStep[wirings.Length];
            var k = a.Length;
            for (var i = 0; i < k; i++)
            {
                var wIdx = rungWiringOffset[i];
                var cinPl = i == 0 ? cinPlane : carries[i - 1];
                if (rungSplit[i])
                {
                    var ws = wirings[wIdx];
                    var wc = wirings[wIdx + 1];
                    var bSum = Enumerable.Repeat(-1, ws.Count).ToArray();
                    var aSum = Enumerable.Repeat(-1, ws.Count).ToArray();
                    var bCar = Enumerable.Repeat(-1, wc.Count).ToArray();
                    var aCar = Enumerable.Repeat(-1, wc.Count).ToArray();
                    foreach (var pos in staticRunnable.Indexes())
                    {
                        var av = a[i].Read(store, pos)!.Value;
                        var bv = b[i].Read(store, pos)!.Value;
                        var cv = store.Read(cinPl, pos) ? 1 : 0;
                        bSum[pos] = ws.Law.TupleIndex([av, bv, cv, oldSums[i][pos]]);
                        aSum[pos] = ws.TupleOf(pos) ?? -1;
                        bCar[pos] = wc.Law.TupleIndex([av, bv, cv, oldCarries[i][pos] ? 1 : 0]);
                        aCar[pos] = wc.TupleOf(pos) ?? -1;
                    }
                    changedPlanes[wIdx].TrimTail();
                    changedPlanes[wIdx + 1].TrimTail();
                    var es = new Plane(ws.Count);
                    var ec = new Plane(wc.Count);
                    var runWaves = staticRunnableCount > 0 ? new[] { staticRunnable.Clone() } : Array.Empty<Plane>();
                    steps[wIdx] = new WiringStep(changedPlanes[wIdx], es, es.Clone(), es.Clone(),
                        staticSkipped[wIdx].Clone(), es.Clone(), bSum, aSum, runWaves);
                    steps[wIdx + 1] = new WiringStep(changedPlanes[wIdx + 1], ec, ec.Clone(), ec.Clone(),
                        staticSkipped[wIdx + 1].Clone(), ec.Clone(), bCar, aCar,
                        staticRunnableCount > 0 ? [staticRunnable.Clone()] : []);
                }
                else
                {
                    var w = wirings[wIdx];
                    var before = Enumerable.Repeat(-1, w.Count).ToArray();
                    var after = Enumerable.Repeat(-1, w.Count).ToArray();
                    var sumFirst = rungSumFirst[i];
                    foreach (var pos in staticRunnable.Indexes())
                    {
                        var av = a[i].Read(store, pos)!.Value;
                        var bv = b[i].Read(store, pos)!.Value;
                        var cv = store.Read(cinPl, pos) ? 1 : 0;
                        var oc = oldCarries[i][pos] ? 1 : 0;
                        before[pos] = w.Law.TupleIndex(sumFirst
                            ? [av, bv, cv, oldSums[i][pos], oc]
                            : [av, bv, cv, oc, oldSums[i][pos]]);
                        after[pos] = w.TupleOf(pos) ?? -1;
                    }
                    changedPlanes[wIdx].TrimTail();
                    var empty = new Plane(w.Count);
                    steps[wIdx] = new WiringStep(changedPlanes[wIdx], empty, empty.Clone(), empty.Clone(),
                        staticSkipped[wIdx].Clone(), empty.Clone(), before, after,
                        staticRunnableCount > 0 ? [staticRunnable.Clone()] : []);
                }
            }
            return steps;
        }

        public (int Fast, int Deopt, bool Changed) StepFastOnly()
        {
            var (fast, deopt, changed, _) = StepCore(trackPerWiringChanges: false);
            return (fast, deopt, changed);
        }

        public (int Fast, int Deopt, Plane[]? ChangedPerWiring) StepWithChangePlanes()
        {
            var (fast, deopt, _, planes) = StepCore(trackPerWiringChanges: true);
            return (fast, deopt, planes);
        }

        private (int Fast, int Deopt, bool Changed, Plane[]? ChangedPlanes) StepCore(bool trackPerWiringChanges)
        {
            if (staticRunnableCount == 0)
            {
                var empty = trackPerWiringChanges ? wirings.Select(w => new Plane(w.Count)).ToArray() : null;
                return (0, 0, false, empty);
            }
            if (!LiveCovers(store.RawLive, staticRunnable)) return (0, staticRunnableCount, false, null);

            if (!isUnary && partitions.All(p => p <= 16))
                return StepBitSlicedBinary(trackPerWiringChanges);

            // Unary or wide partition fallback: guarded scalar per-lane ripple.
            var k = a.Length;
            foreach (var pos in staticRunnable.Indexes())
                for (var i = 0; i < k; i++)
                    if (a[i].Read(store, pos) is null || b[i].Read(store, pos) is null || sum[i].Read(store, pos) is null)
                        return (0, staticRunnableCount, false, null);

            var changedPlanes = trackPerWiringChanges ? wirings.Select(w => new Plane(w.Count)).ToArray() : null;
            var changed = false;
            foreach (var pos in staticRunnable.Indexes())
            {
                var carry = store.Read(cinPlane, pos) ? 1 : 0;
                for (var i = 0; i < k; i++)
                {
                    var total = a[i].Read(store, pos)!.Value + b[i].Read(store, pos)!.Value + carry;
                    var p = partitions[i];
                    var digit = total >= p ? total - p : total;
                    var nextCarry = total >= p ? 1 : 0;
                    var sumChanged = sum[i].Read(store, pos)!.Value != digit;
                    if (sumChanged) { sum[i].Write(store, pos, digit); changed = true; }
                    var cPlane = i < carries.Length ? carries[i] : coutPlane;
                    var carryChanged = store.Read(cPlane, pos) != (nextCarry != 0);
                    if (carryChanged) { store.Write(cPlane, pos, nextCarry != 0); changed = true; }
                    if (changedPlanes is not null)
                    {
                        var wIdx = rungWiringOffset[i];
                        if (rungSplit[i])
                        {
                            if (sumChanged) changedPlanes[wIdx][pos] = true;
                            if (carryChanged) changedPlanes[wIdx + 1][pos] = true;
                        }
                        else if (sumChanged || carryChanged)
                        {
                            changedPlanes[wIdx][pos] = true;
                        }
                    }
                    carry = nextCarry;
                }
            }
            return (staticRunnableCount, 0, changed, changedPlanes);
        }

        private (int Fast, int Deopt, bool Changed, Plane[]? ChangedPlanes) StepBitSlicedBinary(bool trackPerWiringChanges)
        {
            var k = a.Length;
            var runWords = staticRunnable.RawWords;
            var wordCount = runWords.Length;

            ulong[]?[] aRaw = new ulong[totalDigitBits][];
            bool[] aConst = new bool[totalDigitBits];
            ulong[]?[] bRaw = new ulong[totalDigitBits][];
            bool[] bConst = new bool[totalDigitBits];
            Plane[] sumPlanes = new Plane[totalDigitBits];
            ulong[][] sumRaw = new ulong[totalDigitBits][];

            for (var i = 0; i < k; i++)
            {
                var off = rungOffset[i];
                var bits = rungBits[i];
                for (var bit = 0; bit < bits; bit++)
                {
                    var ap = a[i].Planes[bit];
                    var apl = store.RawPlane(ap);
                    aRaw[off + bit] = apl?.RawWords;
                    aConst[off + bit] = apl is null && store.ConstantValue(ap);

                    var bp = b[i].Planes[bit];
                    var bpl = store.RawPlane(bp);
                    bRaw[off + bit] = bpl?.RawWords;
                    bConst[off + bit] = bpl is null && store.ConstantValue(bp);

                    var sp = sum[i].Planes[bit];
                    var spl = store.RawPlane(sp)!;
                    sumPlanes[off + bit] = spl;
                    sumRaw[off + bit] = spl.RawWords;
                }
            }

            var cinPl = store.RawPlane(cinPlane);
            var cinRaw = cinPl?.RawWords;
            var cinConst = cinPl is null && store.ConstantValue(cinPlane);

            var carryPlanes = new Plane[carries.Length];
            var carryRaw = new ulong[carries.Length][];
            for (var i = 0; i < carries.Length; i++)
            {
                carryPlanes[i] = store.RawPlane(carries[i])!;
                carryRaw[i] = carryPlanes[i].RawWords;
            }
            var coutPl = store.RawPlane(coutPlane)!;
            var coutRaw = coutPl.RawWords;

            // Pass 1: exact 64-lane bitwise validation before mutating any state.
            for (var w = 0; w < wordCount; w++)
            {
                var active = runWords[w];
                if (active == 0UL) continue;
                for (var i = 0; i < k; i++)
                {
                    var p = partitions[i];
                    var bits = rungBits[i];
                    if (p == (1 << bits)) continue;
                    var off = rungOffset[i];

                    ulong a0 = ReadWord(aRaw, aConst, off, w), a1 = bits > 1 ? ReadWord(aRaw, aConst, off + 1, w) : 0UL;
                    ulong a2 = bits > 2 ? ReadWord(aRaw, aConst, off + 2, w) : 0UL, a3 = bits > 3 ? ReadWord(aRaw, aConst, off + 3, w) : 0UL;
                    if ((Ge4(a0, a1, a2, a3, p) & active) != 0UL) return (0, staticRunnableCount, false, null);

                    ulong b0 = ReadWord(bRaw, bConst, off, w), b1 = bits > 1 ? ReadWord(bRaw, bConst, off + 1, w) : 0UL;
                    ulong b2 = bits > 2 ? ReadWord(bRaw, bConst, off + 2, w) : 0UL, b3 = bits > 3 ? ReadWord(bRaw, bConst, off + 3, w) : 0UL;
                    if ((Ge4(b0, b1, b2, b3, p) & active) != 0UL) return (0, staticRunnableCount, false, null);

                    ulong s0 = sumRaw[off][w], s1 = bits > 1 ? sumRaw[off + 1][w] : 0UL;
                    ulong s2 = bits > 2 ? sumRaw[off + 2][w] : 0UL, s3 = bits > 3 ? sumRaw[off + 3][w] : 0UL;
                    if ((Ge4(s0, s1, s2, s3, p) & active) != 0UL) return (0, staticRunnableCount, false, null);
                }
            }

            // Pass 2: 64-lane bit-sliced SWAR ripple addition directly across plane words.
            var changedPlanes = trackPerWiringChanges ? wirings.Select(w => new Plane(w.Count)).ToArray() : null;
            var changed = false;
            var cinFallback = cinConst ? ~0UL : 0UL;

            for (var w = 0; w < wordCount; w++)
            {
                var active = runWords[w];
                if (active == 0UL) continue;
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

                    RelationalColumnPlan.AddSwar4(a0, a1, a2, a3, b0, b1, b2, b3, carry, p, checkOvf: false,
                        out var r0, out var r1, out var r2, out var r3, out var cout, out _);

                    ulong sumDiff = WriteWordDiff(sumRaw[off], w, r0, active, invActive);
                    if (bits > 1) sumDiff |= WriteWordDiff(sumRaw[off + 1], w, r1, active, invActive);
                    if (bits > 2) sumDiff |= WriteWordDiff(sumRaw[off + 2], w, r2, active, invActive);
                    if (bits > 3) sumDiff |= WriteWordDiff(sumRaw[off + 3], w, r3, active, invActive);

                    var carryTarget = i < carryRaw.Length ? carryRaw[i] : coutRaw;
                    ulong carryDiff = WriteWordDiff(carryTarget, w, cout, active, invActive);

                    if ((sumDiff | carryDiff) != 0UL) changed = true;
                    if (changedPlanes is not null)
                    {
                        var wIdx = rungWiringOffset[i];
                        if (rungSplit[i])
                        {
                            changedPlanes[wIdx].RawWords[w] = sumDiff;
                            changedPlanes[wIdx + 1].RawWords[w] = carryDiff;
                        }
                        else
                        {
                            changedPlanes[wIdx].RawWords[w] = sumDiff | carryDiff;
                        }
                    }
                    carry = cout;
                }
            }

            if (changed)
            {
                for (var i = 0; i < totalDigitBits; i++) sumPlanes[i].TrimTail();
                for (var i = 0; i < carryPlanes.Length; i++) carryPlanes[i].TrimTail();
                coutPl.TrimTail();
            }
            return (staticRunnableCount, 0, changed, changedPlanes);
        }

        private static ulong ReadWord(ulong[]?[] raw, bool[] constVal, int idx, int w) =>
            raw[idx] is { } arr ? arr[w] : (constVal[idx] ? ~0UL : 0UL);

        private static ulong WriteWordDiff(ulong[] dst, int w, ulong value, ulong active, ulong invActive)
        {
            var old = dst[w];
            var next = (old & invActive) | (value & active);
            var diff = old ^ next;
            if (diff != 0UL) dst[w] = next;
            return diff;
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

    private static FusedConcreteLadderAdder? TryRecognizeConcreteLadder(Engine engine, CompiledWiring[] compiled)
    {
        if (engine.Wirings.Count < 2) return null;

        var wirings = new List<Wiring>(engine.Wirings.Count);
        var skipped = new List<Plane>(engine.Wirings.Count);
        var partitions = new List<int>();
        var aList = new List<Field>();
        var bList = new List<Field>();
        var sumList = new List<Field>();
        var carries = new List<int>();
        var rungSplit = new List<bool>();
        var rungSumFirst = new List<bool>();
        Store? store = null;
        Plane? sharedRunnable = null;
        var cinPlane = -1;
        var lastCoutPlane = -1;

        var idx = 0;
        while (idx < engine.Wirings.Count)
        {
            var w0 = engine.Wirings[idx];
            if (w0.Recorded is not null || w0.Law is not TransitionLaw t0 || w0.Bindings.Any(bind => bind.IsOpen || bind.Surface is not Store))
                return null;
            var s0 = (Store)w0.Bindings[0].Surface;
            if (store is null) store = s0; else if (!ReferenceEquals(store, s0)) return null;
            if (w0.Bindings.Any(bind => !ReferenceEquals(bind.Surface, store))) return null;

            var active0 = w0.Attended();
            if (!CheckIdentityReferences(w0, active0)) return null;
            var run0 = IntersectStoreLives(active0, [store]);
            var skip0 = active0.AndNot(run0);
            if (sharedRunnable is null) sharedRunnable = run0;
            else if (!sharedRunnable.Equals(run0)) return null;

            int p, rungCin, rungCout;
            Field fa, fb, fs;

            if (TryRecognizeDigitAddStep(t0, out var combP, out var sumRole, out var coutRole))
            {
                p = combP;
                rungCin = w0.Bindings[2].Plane(0);
                rungCout = w0.Bindings[coutRole].Plane(0);
                fa = new Field(t0.Roles[0], [.. w0.Bindings[0].Planes]);
                fb = new Field(t0.Roles[1], [.. w0.Bindings[1].Planes]);
                fs = new Field(t0.Roles[sumRole], [.. w0.Bindings[sumRole].Planes]);
                wirings.Add(w0);
                skipped.Add(skip0);
                rungSplit.Add(false);
                rungSumFirst.Add(sumRole == 3);
                idx++;
            }
            else if (idx + 1 < engine.Wirings.Count
                && engine.Wirings[idx + 1] is var w1
                && w1.Recorded is null
                && w1.Law is TransitionLaw t1
                && t0.Roles.Count == 4 && t1.Roles.Count == 4
                && !w1.Bindings.Any(bind => bind.IsOpen || !ReferenceEquals(bind.Surface, store))
                && TryRecognizeSplitDigitPair(t0, t1, out var splitP))
            {
                var active1 = w1.Attended();
                if (!CheckIdentityReferences(w1, active1)) return null;
                var run1 = IntersectStoreLives(active1, [store]);
                if (!sharedRunnable.Equals(run1)) return null;

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
                skipped.Add(skip0);
                skipped.Add(active1.AndNot(run1));
                rungSplit.Add(true);
                rungSumFirst.Add(false);
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
        if (aList.Any(f => f.Shape.IsUnary != aList[0].Shape.IsUnary)) return null;

        // Verify read/write plane disjointness and writable non-constant output/carry planes.
        var readPlanes = new HashSet<int> { cinPlane };
        for (var i = 0; i < aList.Count; i++)
        {
            foreach (var pl in aList[i].Planes) readPlanes.Add(pl);
            foreach (var pl in bList[i].Planes) readPlanes.Add(pl);
        }
        var writtenPlanes = new HashSet<int>();
        for (var i = 0; i < sumList.Count; i++)
            foreach (var pl in sumList[i].Planes)
                if (store!.IsConstantPlane(pl) || !writtenPlanes.Add(pl) || readPlanes.Contains(pl)) return null;
        foreach (var pl in carries)
            if (store!.IsConstantPlane(pl) || !writtenPlanes.Add(pl) || readPlanes.Contains(pl)) return null;
        if (store!.IsConstantPlane(lastCoutPlane) || !writtenPlanes.Add(lastCoutPlane) || readPlanes.Contains(lastCoutPlane)) return null;

        return new FusedConcreteLadderAdder(
            [.. wirings], store!, [.. partitions], [.. aList], [.. bList], [.. sumList],
            cinPlane, lastCoutPlane, [.. carries], sharedRunnable!, [.. skipped],
            [.. rungSplit], [.. rungSumFirst]);
    }

    // ---- Structural staleness snapshot ----

    private sealed class WiringSnapshot
    {
        private readonly record struct BindingSnapshot(
            Surface Surface,
            Port? Plugged,
            int[]? PortLanes,
            Store? TargetStore,
            Plane? TargetLive,
            int WorkspaceCount,
            Plane? WorkspaceIntroduced);

        private readonly Wiring wiring;
        private readonly Law law;
        private readonly int count;
        private readonly Plane live;
        private readonly Plane? attention;
        private readonly bool recorded;
        private readonly BindingSnapshot[] bindings;
        private readonly Plane[][] refPlanes;

        private WiringSnapshot(Wiring wiring)
        {
            this.wiring = wiring;
            law = wiring.Law;
            count = wiring.Count;
            live = wiring.Live.Clone();
            attention = wiring.Attention is int att ? wiring.Rows.Read(att).Clone() : null;
            recorded = wiring.Recorded is not null;
            bindings = [.. wiring.Bindings.Select(CaptureBinding)];
            refPlanes = [.. Enumerable.Range(0, wiring.Bindings.Count)
                .Select(r => wiring.ReferencePlanes(r).Select(p => wiring.Rows.Read(p).Clone()).ToArray())];
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
                var snap = bindings[r];
                var curSurface = other.Bindings[r].Surface;
                if (!ReferenceEquals(snap.Surface, curSurface)) return false;
                var underlying = curSurface.Underlying;
                if (underlying is Socket sock && !ReferenceEquals(snap.Plugged, sock.Plugged)) return false;
                var port = underlying switch { Port p => p, Socket s => s.Plugged, _ => null };
                if (port is not null)
                {
                    if (snap.PortLanes is null || snap.PortLanes.Length != port.Count) return false;
                    for (var lane = 0; lane < port.Count; lane++)
                        if (snap.PortLanes[lane] != port.Lane(lane)) return false;
                }
                else if (snap.PortLanes is not null)
                {
                    return false;
                }
                if (snap.TargetStore is not null && !snap.TargetLive!.Equals(snap.TargetStore.RawLive)) return false;
                if (underlying is Workspace ws && (ws.Count != snap.WorkspaceCount || !snap.WorkspaceIntroduced!.Equals(ws.Introduced)))
                    return false;
                var rp = other.ReferencePlanes(r);
                if (rp.Count != refPlanes[r].Length) return false;
                for (var k = 0; k < rp.Count; k++)
                    if (!PlaneMatchesStore(refPlanes[r][k], other.Rows, rp[k])) return false;
            }
            return true;
        }

        private static BindingSnapshot CaptureBinding(Binding binding)
        {
            var underlying = binding.Surface.Underlying;
            var plugged = underlying is Socket s ? s.Plugged : null;
            var port = underlying switch { Port p => p, Socket sock => sock.Plugged, _ => null };
            int[]? portLanes = null;
            if (port is not null)
            {
                portLanes = new int[port.Count];
                for (var lane = 0; lane < port.Count; lane++) portLanes[lane] = port.Lane(lane);
            }
            var targetStore = underlying switch
            {
                Store st => st,
                Life life => life.Of,
                Port p => p.Owner,
                Socket sock => sock.Plugged?.Owner,
                Workspace ws => ws.Home,
                _ => null,
            };
            var targetLive = targetStore?.RawLive.Clone();
            var wsCount = underlying is Workspace w ? w.Count : -1;
            var wsIntroduced = underlying is Workspace w2 ? w2.Introduced : null;
            return new BindingSnapshot(binding.Surface, plugged, portLanes, targetStore, targetLive, wsCount, wsIntroduced);
        }

        private static bool PlaneMatchesStore(Plane snapshot, Store store, int planeIndex)
        {
            if (store.RawPlane(planeIndex) is { } raw) return snapshot.Equals(raw);
            return snapshot.Equals(store.Read(planeIndex));
        }
    }
}
