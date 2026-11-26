using System.Numerics;

namespace Gojo.EncodingC;

/// <summary>Outcome of one derived compiled step or settle call: reports
/// whether any compiled kernel ran, whether staleness forced fallback to
/// <see cref="Engine"/>, and how many lanes took a compiled path versus
/// falling back.</summary>
public sealed record CompiledStepReport(bool UsedFastPath, bool Stale, int FastLanes, int DeoptLanes);

/// <summary>Optional, derived, snapshot-guarded compiled execution backend for
/// an <see cref="Engine"/> (Gojo/docs/GojoOperationsSynthesis-2026-11.md, §12).
/// Never saved by <see cref="Transport"/> and adds no enums, tags, strings,
/// real numbers, or <c>System.Math</c> calls.
/// <para>Only two family-independent table kernels are built, each lowered
/// strictly from a law's own table bits, bindings, and schedule — never from
/// a law name or family:
/// <list type="bullet">
/// <item><b>General small-table transition kernel:</b> identity-mapped
/// functional <see cref="TransitionLaw"/>s (<c>totalRoleBits ≤ 64</c>,
/// <c>StateCount ≤ 512</c>) lowered to L1-resident portable
/// <see cref="Vector{T}"/> SIMD XOR-flip loops.</item>
/// <item><b>General bit-sliced relation kernel:</b> identity-mapped
/// <see cref="RelationLaw"/>s (up to 8,192 admitted tuples, role sizes ≤ 64)
/// lowered from their admitted tuples grouped by role-0 value, running
/// word-outer with <see cref="Vector{T}"/> directly over
/// <see cref="Store"/> planes — covering ladders, <see cref="Portions"/>,
/// products, ODV, sides, value gaps, and shared-site diagonals through one
/// mechanism.</item>
/// </list>
/// Every ineligible case — non-identity rows, oversized tables, recorded
/// wirings, ports, sockets, workspaces, flipped views, dynamic-topology
/// graphs (<c>Rows</c> writes through any surface or <c>Store.Life</c> writes),
/// broken codes, multi-wiring simultaneous steps, or stale snapshots — falls
/// back cleanly to <see cref="Engine"/> with exact state and step-record
/// parity.</para></summary>
public sealed class CompiledPlan
{
    internal const int MaxTransitionStates = 512;
    internal const int MaxRelationAdmittedTuples = 8192;

    private readonly Engine engine;
    private readonly bool dynamicTopology;
    private readonly WiringSnapshot[] snapshots;
    private readonly CompiledWiring[] compiled;
    private IReadOnlyList<int[]>? cachedEngineWaves;

    private CompiledPlan(
        Engine engine,
        bool dynamicTopology,
        WiringSnapshot[] snapshots,
        CompiledWiring[] compiled)
    {
        this.engine = engine;
        this.dynamicTopology = dynamicTopology;
        this.snapshots = snapshots;
        this.compiled = compiled;
    }

    public Engine Source => engine;

    /// <summary>True iff at least one general transition kernel qualified.</summary>
    internal bool HasTransitionKernel => !dynamicTopology && compiled.Any(c => c.CanAccelerate && c.IsTransitionKernel);

    /// <summary>True iff at least one general relation kernel qualified.</summary>
    internal bool HasRelationKernel => !dynamicTopology && compiled.Any(c => c.CanAccelerate && c.IsRelationKernel);

    /// <summary>True iff at least one compiled kernel is available on this plan.</summary>
    public bool HasAnyCompiledKernel => !dynamicTopology && compiled.Any(c => c.CanAccelerate);

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
    /// strictly by inspecting its law tables and bindings.</summary>
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
        return new CompiledPlan(engine, dynamicTopology, snapshots, compiled);
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

    // ---- Fast 64-lane bitwise identity-mapping & validity helpers ----

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

    private static bool AllPlainCodesValid(Store store, Shape shape, int[] planes, Plane runnable)
    {
        var runWords = runnable.RawWords;
        var wordCount = runWords.Length;
        if (shape.IsUnary)
        {
            if (planes.Length <= 1) return true;
            var prevPlane = store.RawPlane(planes[0]);
            var prevConst = prevPlane is null && store.ConstantValue(planes[0]);
            for (var k = 1; k < planes.Length; k++)
            {
                var curPlane = store.RawPlane(planes[k]);
                var curConst = curPlane is null && store.ConstantValue(planes[k]);
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
            return true;
        }

        var bits = shape.Bits;
        if (bits < 31 && shape.Size == (1 << bits)) return true;
        var pWords = new ulong[]?[bits];
        var pConst = new bool[bits];
        for (var k = 0; k < bits; k++)
        {
            var pl = store.RawPlane(planes[k]);
            pWords[k] = pl?.RawWords;
            if (pl is null) pConst[k] = store.ConstantValue(planes[k]);
        }
        for (var w = 0; w < wordCount; w++)
        {
            if (runWords[w] == 0) continue;
            if ((WordBinaryGreaterOrEqual(pWords, pConst, w, shape.Size) & runWords[w]) != 0) return false;
        }
        return true;
    }

    // ---- Per-wiring qualification (two general table kernels, zero enums) ----

    private static CompiledWiring CompileWiring(Wiring wiring)
    {
        if (wiring.Recorded is not null) return CompiledWiring.FallbackOnly(wiring);
        if (wiring.Bindings.Any(b => b.Surface is not Store)) return CompiledWiring.FallbackOnly(wiring);

        var activeMask = wiring.Attended();
        if (!CheckIdentityReferences(wiring, activeMask)) return CompiledWiring.FallbackOnly(wiring);

        Store[] distinctStores = [.. new HashSet<Store>(wiring.Bindings.Select(b => (Store)b.Surface), ReferenceEqualityComparer.Instance)];
        var staticRunnable = IntersectStoreLives(activeMask, distinctStores);
        var staticSkipped = activeMask.AndNot(staticRunnable);

        if (wiring.Law is TransitionLaw transition)
        {
            if (!transition.IsFunctional || wiring.Bindings.Any(b => b.IsOpen))
                return CompiledWiring.FallbackOnly(wiring);

            var totalBits = 0;
            for (var r = 0; r < wiring.Bindings.Count; r++) totalBits += wiring.Bindings[r].Planes.Count;
            if (totalBits > 64 || transition.StateCount > MaxTransitionStates)
                return CompiledWiring.FallbackOnly(wiring);

            var kernel = SimdTransitionPlan.TryBuild(wiring, transition, distinctStores, staticRunnable, staticSkipped);
            return kernel is not null ? CompiledWiring.FromTransition(wiring, kernel) : CompiledWiring.FallbackOnly(wiring);
        }

        if (wiring.Law is RelationLaw relation)
        {
            var kernel = SimdRelationPlan.TryBuild(wiring, relation, distinctStores, staticRunnable, staticSkipped);
            return kernel is not null ? CompiledWiring.FromRelation(wiring, kernel) : CompiledWiring.FallbackOnly(wiring);
        }

        return CompiledWiring.FallbackOnly(wiring);
    }

    private sealed class CompiledWiring
    {
        private readonly SimdTransitionPlan? transitionPlan;
        private readonly SimdRelationPlan? relationPlan;

        private CompiledWiring(Wiring wiring, SimdTransitionPlan? transitionPlan, SimdRelationPlan? relationPlan)
        {
            Wiring = wiring;
            this.transitionPlan = transitionPlan;
            this.relationPlan = relationPlan;
        }

        public static CompiledWiring FallbackOnly(Wiring wiring) => new(wiring, null, null);
        public static CompiledWiring FromTransition(Wiring wiring, SimdTransitionPlan plan) => new(wiring, plan, null);
        public static CompiledWiring FromRelation(Wiring wiring, SimdRelationPlan plan) => new(wiring, null, plan);

        public Wiring Wiring { get; }
        public bool IsTransitionKernel => transitionPlan is not null;
        public bool IsRelationKernel => relationPlan is not null;
        public bool CanAccelerate => (transitionPlan is not null || relationPlan is not null) && Wiring.Recorded is null;
        public bool CanAccelerateTogether => CanAccelerate;

        public (int Fast, int Deopt, bool Quiet) StepFastOnly()
        {
            if (!CanAccelerate) return (0, Wiring.Live.Count, false);
            if (transitionPlan is not null) return transitionPlan.StepFastOnly();
            return relationPlan!.StepFastOnly();
        }

        public (WiringStep Step, int Fast, int Deopt) StepWithResult(bool forTogether)
        {
            if (!CanAccelerate) return (null!, 0, Wiring.Live.Count);
            if (transitionPlan is not null) return transitionPlan.StepWithResult(forTogether);
            return relationPlan!.StepWithResult(forTogether);
        }
    }

    // ---- General small-table functional TransitionLaw kernel (portable Vector<ulong> SIMD) ----

    private readonly record struct ChangingState(ulong BeforeMask, int[] FlippedWriteIndices);

    private sealed class SimdTransitionPlan
    {
        private readonly Wiring wiring;
        private readonly Store[] distinctStores;
        private readonly Plane staticRunnable;
        private readonly Plane staticSkipped;
        private readonly int staticRunnableCount;
        private readonly int totalRoleBits;
        private readonly int[] writeFlatBits;
        private readonly (Store Store, int Plane)[] flatPlaneRefs;
        private readonly (Store Store, int Plane)[] writePlaneRefs;
        private readonly (Store Store, Shape Shape, int[] Planes)[] roleChecks;
        private readonly ChangingState[] changingStates;
        private readonly Vector<ulong>[] inVecScratch;
        private readonly Vector<ulong>[] flipVecScratch;
        private readonly ulong[] inScalarScratch;
        private readonly ulong[] flipScalarScratch;

        private SimdTransitionPlan(
            Wiring wiring,
            Store[] distinctStores,
            Plane staticRunnable,
            Plane staticSkipped,
            int totalRoleBits,
            int[] writeFlatBits,
            (Store Store, int Plane)[] flatPlaneRefs,
            (Store Store, int Plane)[] writePlaneRefs,
            (Store Store, Shape Shape, int[] Planes)[] roleChecks,
            ChangingState[] changingStates)
        {
            this.wiring = wiring;
            this.distinctStores = distinctStores;
            this.staticRunnable = staticRunnable;
            this.staticSkipped = staticSkipped;
            staticRunnableCount = staticRunnable.Count;
            this.totalRoleBits = totalRoleBits;
            this.writeFlatBits = writeFlatBits;
            this.flatPlaneRefs = flatPlaneRefs;
            this.writePlaneRefs = writePlaneRefs;
            this.roleChecks = roleChecks;
            this.changingStates = changingStates;
            inVecScratch = new Vector<ulong>[totalRoleBits];
            flipVecScratch = new Vector<ulong>[writeFlatBits.Length];
            inScalarScratch = new ulong[totalRoleBits];
            flipScalarScratch = new ulong[writeFlatBits.Length];
        }

        public static SimdTransitionPlan? TryBuild(
            Wiring wiring,
            TransitionLaw transition,
            Store[] distinctStores,
            Plane staticRunnable,
            Plane staticSkipped)
        {
            // Identity-mapped: check pairwise-disjoint (Store, plane) pairs and non-constant write planes.
            var rowBits = new HashSet<(Surface, int)>();
            var roleOffsets = new int[transition.Roles.Count];
            var flatRefs = new List<(Store, int)>();
            var writeFlatList = new List<int>();
            var writeRefList = new List<(Store, int)>();
            var writeSlotByFlat = new Dictionary<int, int>();
            var checks = new (Store, Shape, int[])[transition.Roles.Count];

            for (var r = 0; r < transition.Roles.Count; r++)
            {
                roleOffsets[r] = flatRefs.Count;
                var b = wiring.Bindings[r];
                var st = (Store)b.Surface;
                var writes = wiring.Writes(r);
                var planes = new int[b.Planes.Count];
                for (var k = 0; k < b.Planes.Count; k++)
                {
                    var plane = b.Plane(k);
                    planes[k] = plane;
                    if (writes && st.IsConstantPlane(plane)) return null;
                    if (!rowBits.Add((st, plane))) return null;
                    var flatIdx = flatRefs.Count;
                    flatRefs.Add((st, plane));
                    if (writes)
                    {
                        writeSlotByFlat[flatIdx] = writeFlatList.Count;
                        writeFlatList.Add(flatIdx);
                        writeRefList.Add((st, plane));
                    }
                }
                checks[r] = (st, transition.Roles[r], planes);
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

            return new SimdTransitionPlan(
                wiring, distinctStores, staticRunnable, staticSkipped,
                flatRefs.Count, [.. writeFlatList], [.. flatRefs], [.. writeRefList], checks, [.. entries]);
        }

        public (int Fast, int Deopt, bool Quiet) StepFastOnly()
        {
            if (staticRunnableCount == 0) return (0, 0, true);
            if (!AllLanesValid()) return (0, staticRunnableCount, false);
            var changed = StepCore(changedPlane: null);
            return (staticRunnableCount, 0, !changed);
        }

        public (WiringStep Step, int Fast, int Deopt) StepWithResult(bool forTogether)
        {
            if (staticRunnableCount == 0)
            {
                var empty = new Plane(wiring.Count);
                var none = Enumerable.Repeat(-1, wiring.Count).ToArray();
                var step = new WiringStep(empty, empty.Clone(), empty.Clone(), empty.Clone(),
                    staticSkipped.Clone(), empty.Clone(), none, (int[])none.Clone(), []);
                if (forTogether) step = step with { Contested = empty.Clone() };
                return (step, 0, 0);
            }
            if (!AllLanesValid()) return (null!, 0, staticRunnableCount);

            var before = Enumerable.Repeat(-1, wiring.Count).ToArray();
            var after = Enumerable.Repeat(-1, wiring.Count).ToArray();
            foreach (var row in staticRunnable.Indexes()) before[row] = wiring.TupleOf(row) ?? -1;

            var changedPlane = new Plane(wiring.Count);
            StepCore(changedPlane);

            foreach (var row in staticRunnable.Indexes())
                after[row] = changedPlane[row] ? (wiring.TupleOf(row) ?? -1) : before[row];

            var emptyPlane = new Plane(wiring.Count);
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
            if (forTogether) result = result with { Contested = new Plane(wiring.Count) };
            return (result, staticRunnableCount, 0);
        }

        private bool AllLanesValid()
        {
            for (var s = 0; s < distinctStores.Length; s++)
                if (!LiveCovers(distinctStores[s].RawLive, staticRunnable)) return false;
            for (var r = 0; r < roleChecks.Length; r++)
            {
                var (st, shape, planes) = roleChecks[r];
                if (!AllPlainCodesValid(st, shape, planes, staticRunnable)) return false;
            }
            return true;
        }

        private bool StepCore(Plane? changedPlane)
        {
            if (changingStates.Length == 0) return false;
            var runWords = staticRunnable.RawWords;
            var changedWords = changedPlane?.RawWords;
            var wordCount = runWords.Length;
            var vecWidth = Vector<ulong>.Count;

            var flatWords = new ulong[]?[totalRoleBits];
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

    // ---- General word-outer bit-sliced RelationLaw kernel (grouped by role 0, Vector<ulong> SIMD) ----

    private sealed class SimdRelationPlan
    {
        private readonly record struct RoleSpec(
            Store Store,
            Shape Shape,
            bool IsOpen,
            bool Writes,
            bool AllConstant,
            int[] Planes,
            int ValueOffset,
            int CanonicalRole);

        private readonly Wiring wiring;
        private readonly Store[] distinctStores;
        private readonly Plane staticRunnable;
        private readonly Plane staticSkipped;
        private readonly int staticRunnableCount;
        private readonly RoleSpec[] roles;
        private readonly int[] groupStart;
        private readonly byte[] flatOthers;
        private readonly int otherStride;
        private readonly Vector<ulong>[] inVec;
        private readonly Vector<ulong>[] outVec;
        private readonly ulong[] inScalar;
        private readonly ulong[] outScalar;

        private SimdRelationPlan(
            Wiring wiring,
            Store[] distinctStores,
            Plane staticRunnable,
            Plane staticSkipped,
            RoleSpec[] roles,
            int[] groupStart,
            byte[] flatOthers,
            int totalValues)
        {
            this.wiring = wiring;
            this.distinctStores = distinctStores;
            this.staticRunnable = staticRunnable;
            this.staticSkipped = staticSkipped;
            staticRunnableCount = staticRunnable.Count;
            this.roles = roles;
            this.groupStart = groupStart;
            this.flatOthers = flatOthers;
            otherStride = roles.Length - 1;
            inVec = new Vector<ulong>[totalValues];
            outVec = new Vector<ulong>[totalValues];
            inScalar = new ulong[totalValues];
            outScalar = new ulong[totalValues];
        }

        public static SimdRelationPlan? TryBuild(
            Wiring wiring,
            RelationLaw relation,
            Store[] distinctStores,
            Plane staticRunnable,
            Plane staticSkipped)
        {
            var roleCount = relation.Roles.Count;
            if (roleCount == 0) return null;

            // Validate role shapes, constant planes, and open-role plane sharing.
            // Two open roles sharing the exact same (Store, Planes) sequence form a SameReference diagonal;
            // any partial or read/write plane overlap falls back to Engine.
            var canonical = new int[roleCount];
            var openPlaneOwner = new Dictionary<(Surface, int), int>();
            var plainPlanes = new HashSet<(Surface, int)>();
            var roleSpecs = new RoleSpec[roleCount];
            var valueOffset = 0;

            for (var r = 0; r < roleCount; r++)
            {
                var shape = relation.Roles[r];
                if (shape.Size > 64) return null;
                var b = wiring.Bindings[r];
                var st = (Store)b.Surface;
                var writes = wiring.Writes(r);
                var planes = b.Planes.ToArray();
                var allConstant = true;

                if (b.IsOpen)
                {
                    var localPlanes = new HashSet<int>();
                    for (var p = 0; p < planes.Length; p++)
                    {
                        if (!localPlanes.Add(planes[p])) return null;
                        if (writes && st.IsConstantPlane(planes[p])) return null;
                        if (!st.IsConstantPlane(planes[p])) allConstant = false;
                    }

                    // Check whether this open role is full-plane identical to an earlier open role.
                    var match = -1;
                    for (var prev = 0; prev < r; prev++)
                    {
                        var pb = wiring.Bindings[prev];
                        if (!pb.IsOpen || !ReferenceEquals(pb.Surface, st) || pb.Planes.Count != planes.Length) continue;
                        var sameAll = true;
                        for (var p = 0; p < planes.Length; p++)
                            if (pb.Plane(p) != planes[p]) { sameAll = false; break; }
                        if (sameAll) { match = prev; break; }
                    }

                    if (match >= 0)
                    {
                        canonical[r] = canonical[match];
                    }
                    else
                    {
                        canonical[r] = r;
                        for (var p = 0; p < planes.Length; p++)
                        {
                            var key = ((Surface)st, planes[p]);
                            if (!openPlaneOwner.TryAdd(key, r)) return null;
                            if (plainPlanes.Contains(key)) return null;
                        }
                    }
                }
                else
                {
                    canonical[r] = r;
                    for (var p = 0; p < planes.Length; p++)
                    {
                        if (!st.IsConstantPlane(planes[p])) allConstant = false;
                        var key = ((Surface)st, planes[p]);
                        if (openPlaneOwner.ContainsKey(key)) return null;
                        plainPlanes.Add(key);
                    }
                }

                roleSpecs[r] = new RoleSpec(st, shape, b.IsOpen, writes, allConstant, planes, valueOffset, canonical[r]);
                valueOffset += shape.Size;
            }

            // Collect admitted tuples directly from the relation's raw table bytes,
            // filtering out tuples that violate any SameReference diagonal and aborting
            // immediately if the admitted-tuple ceiling is exceeded.
            var samePairs = new List<(int First, int Second)>();
            for (var r = 0; r < roleCount; r++)
                if (canonical[r] != r) samePairs.Add((canonical[r], r));

            var s0 = relation.Roles[0].Size;
            var buckets = new List<byte[]>[s0];
            for (var v = 0; v < s0; v++) buckets[v] = [];

            var bytes = relation.Bytes();
            var tupleCount = relation.TupleCount;
            var admittedCount = 0;
            var otherStride = roleCount - 1;
            var decoded = new int[roleCount];

            for (var byteIdx = 0; byteIdx < bytes.Length; byteIdx++)
            {
                var bits = (uint)bytes[byteIdx];
                while (bits != 0)
                {
                    var bit = BitOperations.TrailingZeroCount(bits);
                    bits &= bits - 1;
                    var tupleIndex = (byteIdx << 3) + bit;
                    if (tupleIndex >= tupleCount) break;

                    var rem = tupleIndex;
                    for (var r = 0; r < roleCount; r++)
                    {
                        var size = relation.Roles[r].Size;
                        decoded[r] = rem % size;
                        rem /= size;
                    }

                    var ok = true;
                    for (var p = 0; p < samePairs.Count; p++)
                    {
                        var (a, b) = samePairs[p];
                        if (decoded[a] != decoded[b]) { ok = false; break; }
                    }
                    if (!ok) continue;

                    if (++admittedCount > MaxRelationAdmittedTuples) return null;

                    if (otherStride == 0)
                    {
                        buckets[decoded[0]].Add([]);
                    }
                    else
                    {
                        var others = new byte[otherStride];
                        for (var r = 1; r < roleCount; r++) others[r - 1] = (byte)decoded[r];
                        buckets[decoded[0]].Add(others);
                    }
                }
            }

            // Pack buckets into a flat L1-friendly CSR table grouped by role-0 value.
            var groupStart = new int[s0 + 1];
            var flatOthers = new byte[admittedCount * otherStride];
            var cursor = 0;
            for (var v0 = 0; v0 < s0; v0++)
            {
                groupStart[v0] = cursor;
                var list = buckets[v0];
                for (var i = 0; i < list.Count; i++)
                {
                    if (otherStride > 0)
                        Buffer.BlockCopy(list[i], 0, flatOthers, cursor * otherStride, otherStride);
                    cursor++;
                }
            }
            groupStart[s0] = cursor;

            return new SimdRelationPlan(wiring, distinctStores, staticRunnable, staticSkipped, roleSpecs, groupStart, flatOthers, valueOffset);
        }

        public (int Fast, int Deopt, bool Quiet) StepFastOnly()
        {
            if (staticRunnableCount == 0) return (0, 0, true);
            if (!TryResolveAndValidate(out var resolved)) return (0, staticRunnableCount, false);
            var changed = RunCore(resolved, changedPlane: null, unadmittedPlane: null, ambiguousPlane: null);
            return (staticRunnableCount, 0, !changed);
        }

        public (WiringStep Step, int Fast, int Deopt) StepWithResult(bool forTogether)
        {
            if (staticRunnableCount == 0)
            {
                var empty = new Plane(wiring.Count);
                var none = Enumerable.Repeat(-1, wiring.Count).ToArray();
                var step = new WiringStep(empty, empty.Clone(), empty.Clone(), empty.Clone(),
                    staticSkipped.Clone(), empty.Clone(), none, (int[])none.Clone(), []);
                if (forTogether) step = step with { Contested = empty.Clone() };
                return (step, 0, 0);
            }
            if (!TryResolveAndValidate(out var resolved)) return (null!, 0, staticRunnableCount);

            var count = wiring.Count;
            var changedPlane = new Plane(count);
            var unadmittedPlane = new Plane(count);
            var ambiguousPlane = new Plane(count);
            RunCore(resolved, changedPlane, unadmittedPlane, ambiguousPlane);

            var emptyPlane = new Plane(count);
            var noneArr = Enumerable.Repeat(-1, count).ToArray();
            var result = new WiringStep(
                changedPlane,
                emptyPlane,
                unadmittedPlane,
                ambiguousPlane,
                staticSkipped.Clone(),
                emptyPlane.Clone(),
                noneArr,
                (int[])noneArr.Clone(),
                [staticRunnable.Clone()]);
            if (forTogether) result = result with { Contested = new Plane(count) };
            return (result, staticRunnableCount, 0);
        }

        private readonly record struct ResolvedRole(
            Plane?[] Planes,
            ulong[]?[] Words,
            ulong[] ConstMask,
            int PlainConstValue);

        private bool TryResolveAndValidate(out ResolvedRole[] resolved)
        {
            resolved = [];
            for (var s = 0; s < distinctStores.Length; s++)
                if (!LiveCovers(distinctStores[s].RawLive, staticRunnable)) return false;

            var res = new ResolvedRole[roles.Length];
            for (var r = 0; r < roles.Length; r++)
            {
                var spec = roles[r];
                var st = spec.Store;
                var n = spec.Planes.Length;

                if (!spec.IsOpen)
                {
                    if (spec.AllConstant)
                    {
                        ulong pat = 0;
                        for (var k = 0; k < n; k++)
                            if (st.ConstantValue(spec.Planes[k])) pat |= 1UL << k;
                        if (spec.Shape.Decode(pat) is not int decoded) return false;
                        res[r] = new ResolvedRole([], [], [], decoded);
                        continue;
                    }
                    if (!AllPlainCodesValid(st, spec.Shape, spec.Planes, staticRunnable)) return false;
                }

                var pls = new Plane?[n];
                var words = new ulong[]?[n];
                var cMask = new ulong[n];
                for (var k = 0; k < n; k++)
                {
                    var pl = st.RawPlane(spec.Planes[k]);
                    pls[k] = pl;
                    words[k] = pl?.RawWords;
                    if (pl is null) cMask[k] = st.ConstantValue(spec.Planes[k]) ? ulong.MaxValue : 0UL;
                }
                res[r] = new ResolvedRole(pls, words, cMask, -1);
            }

            resolved = res;
            return true;
        }

        private bool RunCore(
            ResolvedRole[] resolved,
            Plane? changedPlane,
            Plane? unadmittedPlane,
            Plane? ambiguousPlane)
        {
            var runWords = staticRunnable.RawWords;
            var wordCount = runWords.Length;
            var vecWidth = Vector<ulong>.Count;
            var roleCount = roles.Length;
            var s0 = roles[0].Shape.Size;
            var r0Off = roles[0].ValueOffset;
            var needEvidence = changedPlane is not null;
            var chWords = changedPlane?.RawWords;
            var unWords = unadmittedPlane?.RawWords;
            var amWords = ambiguousPlane?.RawWords;
            var anyChanged = false;
            var zeroVec = Vector<ulong>.Zero;

            var w = 0;
            for (; w + vecWidth <= wordCount; w += vecWidth)
            {
                var active = new Vector<ulong>(runWords, w);
                if (Vector.EqualsAll(active, zeroVec)) continue;

                // Load inputs for every role and clear output accumulators.
                for (var r = 0; r < roleCount; r++)
                    LoadRoleVec(roles[r], resolved[r], w, active);
                Array.Clear(outVec, 0, outVec.Length);

                // Word-outer role-0 grouped scan over admitted tuples.
                var anyValid = zeroVec;
                for (var v0 = 0; v0 < s0; v0++)
                {
                    var m0 = inVec[r0Off + v0];
                    if (Vector.EqualsAll(m0, zeroVec)) continue;
                    var start = groupStart[v0];
                    var end = groupStart[v0 + 1];
                    if (start == end) continue;
                    if (otherStride == 0)
                    {
                        outVec[r0Off + v0] = m0;
                        anyValid |= m0;
                        continue;
                    }
                    var out0 = zeroVec;
                    for (var idx = start; idx < end; idx++)
                    {
                        var baseOff = idx * otherStride;
                        var m = m0;
                        for (var r = 1; r < roleCount; r++)
                        {
                            m &= inVec[roles[r].ValueOffset + flatOthers[baseOff + r - 1]];
                            if (Vector.EqualsAll(m, zeroVec)) break;
                        }
                        if (Vector.EqualsAll(m, zeroVec)) continue;
                        out0 |= m;
                        for (var r = 1; r < roleCount; r++)
                            outVec[roles[r].ValueOffset + flatOthers[baseOff + r - 1]] |= m;
                    }
                    outVec[r0Off + v0] = out0;
                    anyValid |= out0;
                }

                // Write back writable canonical open roles and accumulate evidence.
                var ch = zeroVec;
                var open = zeroVec;
                for (var r = 0; r < roleCount; r++)
                {
                    var spec = roles[r];
                    var off = spec.ValueOffset;
                    var size = spec.Shape.Size;
                    if (spec.Writes && spec.CanonicalRole == r)
                    {
                        var words = resolved[r].Words;
                        for (var v = 0; v < size; v++)
                        {
                            var dst = words[v]!;
                            var old = new Vector<ulong>(dst, w);
                            var next = Vector.AndNot(old, active) | (old & outVec[off + v]);
                            var diff = old ^ next;
                            if (!Vector.EqualsAll(diff, zeroVec))
                            {
                                next.CopyTo(dst, w);
                                ch |= diff;
                            }
                        }
                    }
                    if (needEvidence && spec.IsOpen && spec.CanonicalRole == r)
                    {
                        var seen = zeroVec;
                        var dup = zeroVec;
                        for (var v = 0; v < size; v++)
                        {
                            var ov = outVec[off + v];
                            dup |= seen & ov;
                            seen |= ov;
                        }
                        open |= dup;
                    }
                }

                if (!Vector.EqualsAll(ch, zeroVec))
                {
                    anyChanged = true;
                    if (chWords is not null) ch.CopyTo(chWords, w);
                }
                if (needEvidence)
                {
                    Vector.AndNot(active, anyValid).CopyTo(unWords!, w);
                    (active & anyValid & open).CopyTo(amWords!, w);
                }
            }

            // Scalar 64-lane tail.
            for (; w < wordCount; w++)
            {
                var active = runWords[w];
                if (active == 0UL) continue;

                for (var r = 0; r < roleCount; r++)
                    LoadRoleScalar(roles[r], resolved[r], w, active);
                Array.Clear(outScalar, 0, outScalar.Length);

                ulong anyValid = 0UL;
                for (var v0 = 0; v0 < s0; v0++)
                {
                    var m0 = inScalar[r0Off + v0];
                    if (m0 == 0UL) continue;
                    var start = groupStart[v0];
                    var end = groupStart[v0 + 1];
                    if (start == end) continue;
                    if (otherStride == 0)
                    {
                        outScalar[r0Off + v0] = m0;
                        anyValid |= m0;
                        continue;
                    }
                    ulong out0 = 0UL;
                    for (var idx = start; idx < end; idx++)
                    {
                        var baseOff = idx * otherStride;
                        var m = m0;
                        for (var r = 1; r < roleCount && m != 0UL; r++)
                            m &= inScalar[roles[r].ValueOffset + flatOthers[baseOff + r - 1]];
                        if (m == 0UL) continue;
                        out0 |= m;
                        for (var r = 1; r < roleCount; r++)
                            outScalar[roles[r].ValueOffset + flatOthers[baseOff + r - 1]] |= m;
                    }
                    outScalar[r0Off + v0] = out0;
                    anyValid |= out0;
                }

                ulong ch = 0UL;
                ulong open = 0UL;
                for (var r = 0; r < roleCount; r++)
                {
                    var spec = roles[r];
                    var off = spec.ValueOffset;
                    var size = spec.Shape.Size;
                    if (spec.Writes && spec.CanonicalRole == r)
                    {
                        var words = resolved[r].Words;
                        for (var v = 0; v < size; v++)
                        {
                            var dst = words[v]!;
                            var old = dst[w];
                            var next = old & (~active | outScalar[off + v]);
                            var diff = old ^ next;
                            if (diff != 0UL)
                            {
                                dst[w] = next;
                                ch |= diff;
                            }
                        }
                    }
                    if (needEvidence && spec.IsOpen && spec.CanonicalRole == r)
                    {
                        ulong seen = 0UL, dup = 0UL;
                        for (var v = 0; v < size; v++)
                        {
                            var ov = outScalar[off + v];
                            dup |= seen & ov;
                            seen |= ov;
                        }
                        open |= dup;
                    }
                }

                if (ch != 0UL)
                {
                    anyChanged = true;
                    if (chWords is not null) chWords[w] = ch;
                }
                if (needEvidence)
                {
                    unWords![w] = active & ~anyValid;
                    amWords![w] = active & anyValid & open;
                }
            }

            if (anyChanged)
            {
                for (var r = 0; r < roleCount; r++)
                {
                    if (!roles[r].Writes || roles[r].CanonicalRole != r) continue;
                    var pls = resolved[r].Planes;
                    for (var v = 0; v < pls.Length; v++) pls[v]?.TrimTail();
                }
            }
            if (needEvidence)
            {
                changedPlane!.TrimTail();
                unadmittedPlane!.TrimTail();
                ambiguousPlane!.TrimTail();
            }
            return anyChanged;
        }

        private void LoadRoleVec(in RoleSpec spec, in ResolvedRole res, int w, Vector<ulong> active)
        {
            var off = spec.ValueOffset;
            var size = spec.Shape.Size;
            if (spec.IsOpen)
            {
                for (var v = 0; v < size; v++)
                {
                    var word = res.Words[v] is { } arr ? new Vector<ulong>(arr, w) : new Vector<ulong>(res.ConstMask[v]);
                    inVec[off + v] = word & active;
                }
                return;
            }
            if (spec.AllConstant)
            {
                for (var v = 0; v < size; v++)
                    inVec[off + v] = v == res.PlainConstValue ? active : Vector<ulong>.Zero;
                return;
            }
            if (spec.Shape.IsUnary)
            {
                var bits = spec.Planes.Length;
                var prev = ReadVec(res, 0, w);
                inVec[off] = Vector.AndNot(active, prev);
                for (var v = 1; v < bits; v++)
                {
                    var cur = ReadVec(res, v, w);
                    inVec[off + v] = Vector.AndNot(prev, cur) & active;
                    prev = cur;
                }
                inVec[off + bits] = prev & active;
                return;
            }

            // Binary plain role: iterative bit-doubling decode over valid codes < size.
            inVec[off] = active;
            var nBits = spec.Planes.Length;
            for (var k = 0; k < nBits; k++)
            {
                var pk = ReadVec(res, k, w);
                var step = 1 << k;
                var top = step < size ? step : size;
                for (var j = top - 1; j >= 0; j--)
                {
                    var cur = inVec[off + j];
                    if (j + step < size) inVec[off + j + step] = cur & pk;
                    inVec[off + j] = Vector.AndNot(cur, pk);
                }
            }
        }

        private void LoadRoleScalar(in RoleSpec spec, in ResolvedRole res, int w, ulong active)
        {
            var off = spec.ValueOffset;
            var size = spec.Shape.Size;
            if (spec.IsOpen)
            {
                for (var v = 0; v < size; v++)
                {
                    var word = res.Words[v] is { } arr ? arr[w] : res.ConstMask[v];
                    inScalar[off + v] = word & active;
                }
                return;
            }
            if (spec.AllConstant)
            {
                for (var v = 0; v < size; v++)
                    inScalar[off + v] = v == res.PlainConstValue ? active : 0UL;
                return;
            }
            if (spec.Shape.IsUnary)
            {
                var bits = spec.Planes.Length;
                var prev = ReadScalar(res, 0, w);
                inScalar[off] = active & ~prev;
                for (var v = 1; v < bits; v++)
                {
                    var cur = ReadScalar(res, v, w);
                    inScalar[off + v] = prev & ~cur & active;
                    prev = cur;
                }
                inScalar[off + bits] = prev & active;
                return;
            }

            inScalar[off] = active;
            var nBits = spec.Planes.Length;
            for (var k = 0; k < nBits; k++)
            {
                var pk = ReadScalar(res, k, w);
                var step = 1 << k;
                var top = step < size ? step : size;
                for (var j = top - 1; j >= 0; j--)
                {
                    var cur = inScalar[off + j];
                    if (j + step < size) inScalar[off + j + step] = cur & pk;
                    inScalar[off + j] = cur & ~pk;
                }
            }
        }

        private static Vector<ulong> ReadVec(in ResolvedRole res, int planeIdx, int w) =>
            res.Words[planeIdx] is { } arr ? new Vector<ulong>(arr, w) : new Vector<ulong>(res.ConstMask[planeIdx]);

        private static ulong ReadScalar(in ResolvedRole res, int planeIdx, int w) =>
            res.Words[planeIdx] is { } arr ? arr[w] : res.ConstMask[planeIdx];
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
