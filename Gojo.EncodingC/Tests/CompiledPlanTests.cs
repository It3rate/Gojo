using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Phase 5 of GojoOperations-2026-11.md (X15): derived compiled
/// execution (SIMD bit-sliced planes, packet-exact Convert(3,2), widening
/// multi-rung radix-B ALU adder, Regime 1 &amp; Regime 2 oscillator lowering,
/// per-lane &amp; group contention guards, content recognition after Transport
/// restore, and IsCurrent invalidation).</summary>
public sealed class CompiledPlanTests
{
    private static readonly Plane Here = Plane.Of(1, 0);

    /// <summary>SIMD bit-sliced CompiledSimd.StepTransition produces bit-for-bit
    /// identical output and evidence across 256 randomized lanes (including
    /// malformed unary codes, retired lanes, and Attention=0 lanes) compared
    /// to canonical TransitionLaw.Step and Engine.Step.</summary>
    [Fact]
    public void SimdBitSlicedFastPathMatchesCanonicalBitForBitAcross256Lanes()
    {
        const int Lanes = 256;
        static (Engine Engine, Store Store, Field A, Field B) BuildPair(bool injectBroken)
        {
            var store = new Store(0, Lanes);
            var a = new Field(Shape.Unary(3), store.AddPlane(), store.AddPlane(), store.AddPlane());
            var b = new Field(Shape.Unary(3), store.AddPlane(), store.AddPlane(), store.AddPlane());
            // Deterministic pseudo-random states across 256 lanes (no System.Math!)
            uint rng = 0x9E3779B9u;
            for (var i = 0; i < Lanes; i++)
            {
                rng = rng * 1664525u + 1013904223u;
                a.Write(store, i, (int)(rng % 4));
                rng = rng * 1664525u + 1013904223u;
                b.Write(store, i, (int)(rng % 4));
            }
            if (injectBroken)
            {
                // Malformed unary prefix 010 at lane 17; retired lane 99.
                store.Write(a.Planes[0], 17, false);
                store.Write(a.Planes[1], 17, true);
                store.Retire(99);
            }
            var w = Tension.Wire(Counts.Shortfall(3, 3), [a.On(store), b.On(store), new Field(Shape.Unary(3), store.AddPlane(), store.AddPlane(), store.AddPlane()).On(store)], Plane.Ones(Lanes));
            if (injectBroken)
            {
                var att = w.Attend();
                w.Rows.Write(att, 42, false); // unattended lane
            }
            return (new Engine().Add(w), store, a, b);
        }

        // 1. Clean 256 lanes -> takes SIMD fast path!
        var canonClean = BuildPair(injectBroken: false);
        var fastClean = BuildPair(injectBroken: false);
        var planClean = CompiledPlan.Compile(fastClean.Engine);
        var reportClean = planClean.RunStep();
        canonClean.Engine.Step();

        Assert.True(reportClean.UsedFastPath);
        Assert.False(reportClean.Stale);
        Assert.Equal(Lanes, reportClean.FastLanes);
        Assert.Equal(0, reportClean.DeoptLanes);
        Assert.Equal(canonClean.Engine.Tick, fastClean.Engine.Tick);
        for (var p = 0; p < canonClean.Store.PlaneCount; p++)
            Assert.Equal(canonClean.Store.Read(p), fastClean.Store.Read(p));

        // 2. Dirty lanes (malformed unary 010 + retired lane + unattended lane) -> deopts cleanly with exact parity!
        var canonDirty = BuildPair(injectBroken: true);
        var fastDirty = BuildPair(injectBroken: true);
        var planDirty = CompiledPlan.Compile(fastDirty.Engine);
        var reportDirty = planDirty.RunStep();
        canonDirty.Engine.Step();

        Assert.False(reportDirty.UsedFastPath); // malformed lane triggered deopt guard!
        Assert.Equal(canonDirty.Engine.Tick, fastDirty.Engine.Tick);
        for (var p = 0; p < canonDirty.Store.PlaneCount; p++)
            Assert.Equal(canonDirty.Store.Read(p), fastDirty.Store.Read(p));
    }

    /// <summary>Packet-exact Convert(3, 2) lowering (ops_sol catch!):
    /// recognized purely from Transport-restored table bits + PreservedSum
    /// (zero builder names); x=8 with room=10 settles to (2, 4), NOT bulk 5;
    /// x=8 with room=3 settles to (5, 2); shared-target rows trigger the group
    /// contention guard and match canonical stepping bit-for-bit.</summary>
    [Fact]
    public void PacketConvertRecognizesRestoredTablePreservesPacketsAndGuardsGroupContention()
    {
        // Build Convert(3, 2), save via Transport (throwing away original builder object), and restore!
        var store = new Store(0, 2);
        var src = new Field(Shape.Unary(8), [.. Enumerable.Range(0, 8).Select(_ => store.AddPlane())]);
        var dst = new Field(Shape.Unary(10), [.. Enumerable.Range(0, 10).Select(_ => store.AddPlane())]);
        src.Write(store, 0, 8);
        dst.Write(store, 0, 0); // room = 10 -> 2 packets -> (src=2, dst=4), NEVER bulk 5!
        src.Write(store, 1, 8);
        dst.Write(store, 1, 7); // room = 3  -> 1 packet  -> (src=5, dst=9)!

        var engine = new Engine().Add(Tension.Route(Flows.Convert(3, 2, 8, 10), src.On(store), dst.On(store), Plane.Ones(2)));
        var bytes = Transport.Save(engine, store);
        var (restoredCanon, canonStores) = Transport.Load(bytes);
        var (restoredFast, fastStores) = Transport.Load(bytes);

        // Compile the RESTORED machine (fresh law instance, recognized strictly from table bits & Accounting!).
        var plan = CompiledPlan.Compile(restoredFast);
        Assert.True(plan.RecognizedPacketConvert);
        Assert.NotSame(Flows.Convert(3, 2, 8, 10), restoredFast.Wirings[0].Law);

        var rep = plan.RunSettle(16);
        restoredCanon.Settle(16);

        Assert.True(rep.UsedFastPath);
        Assert.Equal(2, rep.FastLanes);
        Assert.Equal(restoredCanon.Tick, restoredFast.Tick);
        // Verify exact packet accounting: (2, 4) on lane 0 (NOT 5!) and (5, 9) on lane 1.
        Assert.Equal((2, 4), (src.Read(fastStores[0], 0), dst.Read(fastStores[0], 0)));
        Assert.Equal((5, 9), (src.Read(fastStores[0], 1), dst.Read(fastStores[0], 1)));
        for (var p = 0; p < canonStores[0].PlaneCount; p++)
            Assert.Equal(canonStores[0].Read(p), fastStores[0].Read(p));

        // Group contention guard: two rows pouring from separate sources into ONE shared target position 0!
        var gStore = new Store(0, 2);
        var gSrc = new Field(Shape.Unary(6), [.. Enumerable.Range(0, 6).Select(_ => gStore.AddPlane())]);
        var gDst = new Field(Shape.Unary(4), [.. Enumerable.Range(0, 4).Select(_ => gStore.AddPlane())]);
        gSrc.Write(gStore, 0, 6);
        gSrc.Write(gStore, 1, 6);
        var gw = new Wiring(Flows.Convert(3, 2, 6, 4), gSrc.On(gStore), gDst.On(gStore));
        gw.Add(0, 0); // row 0 writes shared dst[0]
        gw.Add(1, 0); // row 1 ALSO writes shared dst[0] -> group contested!
        var gEngine = new Engine().Add(gw);
        var gBytes = Transport.Save(gEngine, gStore);
        var (gCanon, gCanonStores) = Transport.Load(gBytes);
        var (gFast, gFastStores) = Transport.Load(gBytes);

        var gPlan = CompiledPlan.Compile(gFast);
        var gRep = gPlan.RunStep();
        gCanon.Step();
        Assert.False(gRep.UsedFastPath); // group contention guard deopted cleanly!
        Assert.Equal(gCanon.Tick, gFast.Tick);
        for (var p = 0; p < gCanonStores[0].PlaneCount; p++)
            Assert.Equal(gCanonStores[0].Read(p), gFastStores[0].Read(p));
    }

    /// <summary>Sealed multi-rung radix-4 adder recognized purely from
    /// DigitAddStep table bits lowers to widening integer ALU addition with
    /// exact overflow routed to carry-out (never silent wraparound) and
    /// bit-for-bit parity across all 256 input pairs (0..15 + 0..15).</summary>
    [Fact]
    public void RecognizedRadixAdderLowersToWideningAluWithExactOverflowAndParity()
    {
        const int Radix = 4;
        const int Lanes = 256; // all 16×16 pairs of 2-rung radix-4 numbers!

        static (Engine Engine, Store Store, Ladder A, Ladder B, Ladder Sum, int Cin, int Cout) BuildAdder()
        {
            var store = new Store(0, Lanes);
            var a = new Ladder(store, Radix, 2, unary: false);
            var b = new Ladder(store, Radix, 2, unary: false);
            var sum = new Ladder(store, Radix, 2, unary: false);
            var cin = store.AddPlane();
            var cMid = store.AddPlane();
            var cout = store.AddPlane();
            for (var x = 0; x < 16; x++)
                for (var y = 0; y < 16; y++)
                {
                    var lane = x * 16 + y;
                    a.Write(lane, x);
                    b.Write(lane, y);
                }
            var law = Rungs.DigitAddStep(Radix, unary: false);
            var all = Plane.Ones(Lanes);
            var w0 = Tension.Wire(law, [a.Digits[0].On(store), b.Digits[0].On(store), new Binding(store, cin), sum.Digits[0].On(store), new Binding(store, cMid)], all);
            var w1 = Tension.Wire(law, [a.Digits[1].On(store), b.Digits[1].On(store), new Binding(store, cMid), sum.Digits[1].On(store), new Binding(store, cout)], all);
            return (new Engine().Add(w0).Add(w1), store, a, b, sum, cin, cout);
        }

        var canon = BuildAdder();
        var fast = BuildAdder();
        var plan = CompiledPlan.Compile(fast.Engine);
        Assert.True(plan.RecognizedRadixAdder);

        var rep = plan.RunStep();
        canon.Engine.Step();

        Assert.True(rep.UsedFastPath);
        Assert.Equal(Lanes, rep.FastLanes);
        Assert.Equal(canon.Engine.Tick, fast.Engine.Tick);
        for (var x = 0; x < 16; x++)
            for (var y = 0; y < 16; y++)
            {
                var lane = x * 16 + y;
                var total = x + y;
                Assert.Equal((long)(total % 16), fast.Sum.Read(fast.Store, lane));
                Assert.Equal(total >= 16, fast.Store.Read(fast.Cout, lane)); // overflow visible, never wrapped!
            }
        for (var p = 0; p < canon.Store.PlaneCount; p++)
            Assert.Equal(canon.Store.Read(p), fast.Store.Read(p));
    }

    /// <summary>Crystallized bounded multiply law recognized strictly from
    /// Transport-restored table bits lowers to widening integer MUL with exact
    /// overflow routed into T+ (never silent wraparound), preserves Tick
    /// parity, and invalidates immediately on structural edit.</summary>
    [Fact]
    public void RecognizedBoundedMultiplyLowersToWideningMulWithOverflowToTPlusAndInvalidatesOnEdit()
    {
        const int MaxA = 3;
        const int MaxB = 3;
        const int MaxVal = 5;
        const int MaxOvf = 4; // 5 + 4 = 9 = 3 * 3: exact overflow accounting!
        const int Lanes = (MaxA + 1) * (MaxB + 1);

        var store = new Store(0, Lanes);
        var law = Multiplication.BoundedMultiply(MaxA, MaxB, MaxVal, MaxOvf);
        var fa = new Field(law.Roles[0], [.. Enumerable.Range(0, law.Roles[0].Bits).Select(_ => store.AddPlane())]);
        var fb = new Field(law.Roles[1], [.. Enumerable.Range(0, law.Roles[1].Bits).Select(_ => store.AddPlane())]);
        var fv = new Field(law.Roles[2], [.. Enumerable.Range(0, law.Roles[2].Bits).Select(_ => store.AddPlane())]);
        var fo = new Field(law.Roles[3], [.. Enumerable.Range(0, law.Roles[3].Bits).Select(_ => store.AddPlane())]);
        for (var a = 0; a <= MaxA; a++)
            for (var b = 0; b <= MaxB; b++)
            {
                var lane = a * (MaxB + 1) + b;
                fa.Write(store, lane, a);
                fb.Write(store, lane, b);
            }

        var w = Tension.Wire(law, [fa.On(store), fb.On(store), fv.On(store), fo.On(store)], Plane.Ones(Lanes));
        var bytes = Transport.Save(new Engine().Add(w), store);
        var (canon, canonStores) = Transport.Load(bytes);
        var (fast, fastStores) = Transport.Load(bytes);

        var plan = CompiledPlan.Compile(fast);
        Assert.True(plan.RecognizedBoundedMultiply);
        Assert.NotSame(law, fast.Wirings[0].Law);

        var rep = plan.RunStep();
        canon.Step();

        Assert.True(rep.UsedFastPath);
        Assert.Equal(Lanes, rep.FastLanes);
        Assert.Equal(0, rep.DeoptLanes);
        Assert.Equal(canon.Tick, fast.Tick);

        for (var a = 0; a <= MaxA; a++)
            for (var b = 0; b <= MaxB; b++)
            {
                var lane = a * (MaxB + 1) + b;
                var val = fv.Read(fastStores[0], lane)!.Value;
                var ovf = fo.Read(fastStores[0], lane)!.Value;
                Assert.Equal(a * b, val + ovf); // exact conservation into value + T+, zero silent wrap!
                Assert.True(val <= MaxVal);
            }
        for (var p = 0; p < canonStores[0].PlaneCount; p++)
            Assert.Equal(canonStores[0].Read(p), fastStores[0].Read(p));

        // Structural edit invalidates immediately and falls back safely.
        fast.Wirings[0].Retire(0);
        Assert.False(plan.IsCurrent);
        var staleRep = plan.RunStep();
        Assert.True(staleRep.Stale);
        Assert.False(staleRep.UsedFastPath);
    }

    /// <summary>IsCurrent invalidation: any structural edit (adding a wiring,
    /// retiring a row, flipping Attention, changing Together, or unplugging a
    /// Socket) immediately invalidates CompiledPlan and falls back safely to
    /// canonical execution.</summary>
    [Fact]
    public void StructuralEditsInvalidateCompiledPlanImmediatelyAndFallBack()
    {
        var store = new Store(0, 2);
        var src = new Field(Shape.Unary(4), [.. Enumerable.Range(0, 4).Select(_ => store.AddPlane())]);
        var dst = new Field(Shape.Unary(4), [.. Enumerable.Range(0, 4).Select(_ => store.AddPlane())]);
        src.Write(store, 0, 3);
        src.Write(store, 1, 2);
        var w = Tension.Route(Flows.Move(4, 4), src.On(store), dst.On(store), Plane.Ones(2));
        var engine = new Engine().Add(w);
        var plan = CompiledPlan.Compile(engine);
        Assert.True(plan.IsCurrent);

        // 1. Retiring a row invalidates immediately.
        w.Retire(1);
        Assert.False(plan.IsCurrent);
        var staleRep = plan.RunStep();
        Assert.True(staleRep.Stale);
        Assert.False(staleRep.UsedFastPath);
        // Canonical fallback still moved lane 0 cleanly and skipped retired lane 1!
        Assert.Equal((2, 1), (src.Read(store, 0), dst.Read(store, 0)));
        Assert.Equal((2, 0), (src.Read(store, 1), dst.Read(store, 1)));

        // 2. Recompiling restores IsCurrent; toggling Together invalidates again.
        var plan2 = CompiledPlan.Compile(engine);
        Assert.True(plan2.IsCurrent);
        engine.Together = true;
        Assert.False(plan2.IsCurrent);

        // 3. Unplugging a Socket invalidates a plan reading through that Socket.
        var peer = new Store(1, 1);
        peer.Write(0, 0, true);
        var port = peer.Publish([0], [0]);
        var sock = Socket.For(port);
        sock.Plug(port);
        var local = new Store(1, 1);
        var sw = Tension.Wire(Counts.Accumulate(1), [new Binding(sock, 0), new Binding(local, 0)], Here);
        var sockEngine = new Engine().Add(sw);
        var sockPlan = CompiledPlan.Compile(sockEngine);
        Assert.True(sockPlan.IsCurrent);
        sock.Unplug();
        Assert.False(sockPlan.IsCurrent);
    }

    /// <summary>Regime 1 (Bit-Exact) vs Regime 2 (Declared Finite-Read) on the
    /// integer Minsky oscillator: Regime 1 matches canonical bit-for-bit across
    /// 120 steps (4 exact cycles of period 30); Regime 2 requires an explicit
    /// integer-tick FiniteReadContract and rejects oracles exceeding tolerance
    /// or calls without a contract.</summary>
    [Fact]
    public void OscillatorCompilationSeparatesRegime1BitExactFromRegime2DeclaredFiniteRead()
    {
        const int MaxAbs = 12;
        const int Shift = 2;

        static (Engine Engine, Store Store, Field X, Field V) BuildMinsky()
        {
            var store = new Store(0, 1);
            var shape = Dynamics.SignedShape(MaxAbs);
            var fx = new Field(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]);
            var fv = new Field(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]);
            fx.Write(store, 0, Dynamics.EncodeSigned(10, MaxAbs));
            fv.Write(store, 0, Dynamics.EncodeSigned(0, MaxAbs));
            var engine = new Engine()
                .Add(Tension.Wire(Dynamics.SignedShear(MaxAbs, Shift, -1, admitOutOfRange: false), [fx.On(store), fv.On(store)], Here))
                .Add(Tension.Wire(Dynamics.SignedShear(MaxAbs, Shift, +1, admitOutOfRange: false), [fv.On(store), fx.On(store)], Here));
            return (engine, store, fx, fv);
        }

        var canon = BuildMinsky();
        var fast = BuildMinsky();
        var plan = CompiledPlan.Compile(fast.Engine);
        Assert.True(plan.RecognizedKickDriftOscillator);

        // Regime 1: 120 steps (4 full cycles of period 30) — 100% bit-for-bit identical!
        var reference = new (int X, int V)[31];
        reference[0] = (10, 0);
        for (var t = 1; t <= 120; t++)
        {
            var rep = plan.RunStep();
            canon.Engine.Step();
            Assert.True(rep.UsedFastPath);
            Assert.Equal(canon.Engine.Tick, fast.Engine.Tick);
            var cx = Dynamics.DecodeSigned(canon.X.Read(canon.Store, 0)!.Value, MaxAbs);
            var cv = Dynamics.DecodeSigned(canon.V.Read(canon.Store, 0)!.Value, MaxAbs);
            var fx = Dynamics.DecodeSigned(fast.X.Read(fast.Store, 0)!.Value, MaxAbs);
            var fv = Dynamics.DecodeSigned(fast.V.Read(fast.Store, 0)!.Value, MaxAbs);
            Assert.Equal((cx, cv), (fx, fv));
            if (t <= 30) reference[t] = (cx, cv);
        }
        Assert.Equal((10, 0), reference[30]);

        // Regime 2: no contract -> refused (never silently substitutes approximate waveform!).
        var refusedNoContract = plan.TryReadApproximateOscillator(30, contract: null, t => reference[t]);
        Assert.False(refusedNoContract.Accepted);

        // Regime 2: explicit contract (±1 tick tolerance) accepts a quantized oracle off by at most 1 tick and marks Approximate=true.
        var contract = new FiniteReadContract(MaxAbs, Shift, ToleranceTicks: 1);
        var okRead = plan.TryReadApproximateOscillator(30, contract, t => (reference[t].X + (t % 2 == 1 ? 1 : 0), reference[t].V));
        Assert.True(okRead.Accepted);
        Assert.True(okRead.Approximate);
        Assert.Equal(1, okRead.MaxErrorTicks);

        // Regime 2: oracle drifting by 2 ticks (> ToleranceTicks = 1) is rejected!
        var tooNoisy = plan.TryReadApproximateOscillator(30, contract, t => (reference[t].X + (t == 15 ? 2 : 0), reference[t].V));
        Assert.False(tooNoisy.Accepted);
        Assert.Equal(2, tooNoisy.MaxErrorTicks);
    }
}
