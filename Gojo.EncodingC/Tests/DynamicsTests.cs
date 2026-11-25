using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Phase 4 of GojoOperations-2026-11.md (X8–X10): un-deferring
/// generator J via the rung-0 8-state Sides oscillator, symplectic step-order
/// proof (k=1 hexagon &amp; shift-2 Minsky orbit vs StepTogether spiral), the
/// unified 2×3 order-and-sign family (Napier log, sin/cos, coast multiply,
/// exp/cosh), backward phase inference via AsRelation(), and waveform
/// refinement by read coarseness.</summary>
public sealed class DynamicsTests
{
    private static readonly Plane Here = Plane.Of(1, 0);

    private static Field SignedField(Store store, int maxAbs)
    {
        var shape = Dynamics.SignedShape(maxAbs);
        return new Field(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]);
    }

    /// <summary>X8: Generator J has exact order 4 (J² = Negate, J⁴ = id), is
    /// reversible with exact Inverse(), and the rung-0 8-state Sides oscillator
    /// traces J's cycle across its four axis states while narrowing initial
    /// phase backwards via AsRelation().</summary>
    [Fact]
    public void GeneratorJAndRung0OscillatorCycleFourQuartersAndInferBackwards()
    {
        // 1. Generator J: (F,+) -> (I,+) -> (F,-) -> (I,-) -> (F,+)
        Assert.True(Dynamics.GeneratorJ.IsFunctional);
        Assert.True(Dynamics.GeneratorJ.IsReversible);
        var jStore = new Store(2, 1);
        var jEngine = new Engine().Add(Tension.Wire(Dynamics.GeneratorJ, [new Binding(jStore, 0), new Binding(jStore, 1)], Here));
        var jCycle = new List<(bool FaceI, bool Neg)>();
        for (var i = 0; i < 5; i++)
        {
            jCycle.Add((jStore.Read(0, 0), jStore.Read(1, 0)));
            jEngine.Advance();
        }
        Assert.Equal([(false, false), (true, false), (false, true), (true, true), (false, false)], jCycle);

        // Inverse J runs the conjugate direction: (F,+) -> (I,-) -> (F,-) -> (I,+) -> (F,+).
        var invJ = Dynamics.GeneratorJ.Inverse();
        Assert.True(invJ.IsFunctional && invJ.IsReversible);
        var jiStore = new Store(2, 1);
        var jiEngine = new Engine().Add(Tension.Wire(invJ, [new Binding(jiStore, 0), new Binding(jiStore, 1)], Here));
        var jiCycle = new List<(bool FaceI, bool Neg)>();
        for (var i = 0; i < 5; i++)
        {
            jiCycle.Add((jiStore.Read(0, 0), jiStore.Read(1, 0)));
            jiEngine.Advance();
        }
        Assert.Equal([(false, false), (true, true), (false, true), (true, false), (false, false)], jiCycle);

        // 2. Rung-0 Gray projection: 11 -> 10 -> 00 -> 01 -> 11.
        var gStore = new Store(2, 1);
        gStore.Write(0, 0, true);
        gStore.Write(1, 0, true);
        var gEngine = new Engine().Add(Tension.Wire(Dynamics.Rung0GrayCycle, [new Binding(gStore, 0), new Binding(gStore, 1)], Here));
        var gray = new List<(bool X, bool V)>();
        for (var i = 0; i < 5; i++)
        {
            gray.Add((gStore.Read(0, 0), gStore.Read(1, 0)));
            gEngine.Advance();
        }
        Assert.Equal([(true, true), (true, false), (false, false), (false, true), (true, true)], gray);

        // 3. Full 8-state rung-0 Sides oscillator: axis states match conjugate J cycle!
        var oStore = new Store(0, 1);
        var xSide = new Field(Shape.Binary(3), oStore.AddPlane(), oStore.AddPlane());
        var vSide = new Field(Shape.Binary(3), oStore.AddPlane(), oStore.AddPlane());
        xSide.Write(oStore, 0, 2); // +1
        vSide.Write(oStore, 0, 1); // 0
        var oEngine = new Engine().Add(Tension.Wire(Dynamics.Rung0Oscillator, [xSide.On(oStore), vSide.On(oStore)], Here));
        var states = new List<(int X, int V)>();
        for (var i = 0; i < 9; i++)
        {
            states.Add((xSide.Read(oStore, 0)!.Value - 1, vSide.Read(oStore, 0)!.Value - 1));
            oEngine.Advance();
        }
        Assert.Equal([(1, 0), (1, -1), (0, -1), (-1, -1), (-1, 0), (-1, 1), (0, 1), (1, 1), (1, 0)], states);

        // 4. Backward phase inference via AsRelation() across 3 moments:
        // Observing only moment 2 == (-1, 0) narrows moment 0 backwards to (0, -1)!
        var planStore = new Store(0, 3);
        var px = Supports.On(planStore, new Field(Shape.Binary(3), planStore.AddPlane(), planStore.AddPlane()));
        var pv = Supports.On(planStore, new Field(Shape.Binary(3), planStore.AddPlane(), planStore.AddPlane()));
        px.Restrict(2, 1UL << 0); // x2 = -1
        pv.Restrict(2, 1UL << 1); // v2 = 0
        var stepRel = Dynamics.Rung0Oscillator.AsRelation();
        var planW = new Wiring(stepRel, px.Variable, pv.Variable, px.Variable, pv.Variable);
        planW.Add(0, 0, 1, 1);
        planW.Add(1, 1, 2, 2);
        Assert.NotNull(new Engine().Add(planW).Settle(8));
        Assert.Equal((1UL << 1, 1UL << 0), (px.Values(0), pv.Values(0))); // recovered moment 0 = (0, -1)!
    }

    /// <summary>X9 (step order as saved structure + exact shadow energy):
    /// both the k=1 6-step hexagon and the shift-2 Minsky oscillator close
    /// periodic integer orbits under declared order (Together=false) and survive
    /// Transport.Save/Load, whereas Together=true (StepTogether) spirals out.</summary>
    [Fact]
    public void DeclaredOrderClosesSymplecticOrbitsAndSavesWhileStepTogetherSpirals()
    {
        // 1. Exact k=1 hexagon (ops_sol): (1,0)->(0,-1)->(-1,-1)->(-1,0)->(0,1)->(1,1)->(1,0),
        // preserving exact shadow energy H = v^2 + x^2 - x*v == 1 at every tick.
        static (Engine Engine, Store Store, Field X, Field V) BuildOsc(int maxAbs, int shift, int x0, int v0, bool together, int m = 1)
        {
            var store = new Store(0, 1);
            var fx = SignedField(store, maxAbs);
            var fv = SignedField(store, maxAbs);
            fx.Write(store, 0, Dynamics.EncodeSigned(x0, maxAbs));
            fv.Write(store, 0, Dynamics.EncodeSigned(v0, maxAbs));
            // Wave 1 (kick): v -= m * coarse(x, shift); Wave 2 (drift): x += coarse(v, shift)
            var kick = Dynamics.SignedShear(maxAbs, shift, -m, admitOutOfRange: false);
            var drift = Dynamics.SignedShear(maxAbs, shift, +1, admitOutOfRange: false);
            Assert.True(kick.IsReversible && kick.Accounting.Reversible);
            Assert.True(drift.IsReversible && drift.Accounting.Reversible);
            var engine = new Engine { Together = together }
                .Add(Tension.Wire(kick, [fx.On(store), fv.On(store)], Here))
                .Add(Tension.Wire(drift, [fv.On(store), fx.On(store)], Here));
            return (engine, store, fx, fv);
        }

        var hex = BuildOsc(maxAbs: 2, shift: 0, x0: 1, v0: 0, together: false);
        var hexSaved = Transport.Save(hex.Engine, hex.Store);
        var (hexRestored, hexCopies) = Transport.Load(hexSaved);
        Assert.False(hexRestored.Together);

        var hexOrbit = new List<(int X, int V)>();
        for (var i = 0; i < 7; i++)
        {
            var x = Dynamics.DecodeSigned(hex.X.Read(hexCopies[0], 0)!.Value, 2);
            var v = Dynamics.DecodeSigned(hex.V.Read(hexCopies[0], 0)!.Value, 2);
            hexOrbit.Add((x, v));
            Assert.Equal(1, v * v + x * x - x * v); // exact shadow energy H == 1!
            hexRestored.Advance();
        }
        Assert.Equal([(1, 0), (0, -1), (-1, -1), (-1, 0), (0, 1), (1, 1), (1, 0)], hexOrbit);

        // Same k=1 shears with Together=true spiral out immediately (hitting Unadmitted at step 3).
        var hexTog = BuildOsc(maxAbs: 2, shift: 0, x0: 1, v0: 0, together: true);
        hexTog.Engine.Advance(); // (1, -1)
        hexTog.Engine.Advance(); // (0, -2)
        var spiralHit = hexTog.Engine.Advance(); // tries (-2, -2) -> next hits out-of-range!
        Assert.Equal((-2, -2), (Dynamics.DecodeSigned(hexTog.X.Read(hexTog.Store, 0)!.Value, 2),
            Dynamics.DecodeSigned(hexTog.V.Read(hexTog.Store, 0)!.Value, 2)));
        Assert.Contains(hexTog.Engine.Advance().Steps, s => s.Unadmitted.Any); // spirals past ±2!

        // 2. Integer Minsky oscillator (x0 = 10, v0 = 0, shift = 2, maxAbs = 12):
        // Declared order closes exact period 30 (4 full cycles = 120 steps, max |x| == 10, zero drift)!
        var minsky = BuildOsc(maxAbs: 12, shift: 2, x0: 10, v0: 0, together: false);
        var (minskyRestored, mStores) = Transport.Load(Transport.Save(minsky.Engine, minsky.Store));
        var period = -1;
        var maxAbsX = 0;
        for (var t = 1; t <= 120; t++)
        {
            var step = minskyRestored.Advance();
            Assert.All(step.Steps, s => Assert.True(s.Unadmitted.None));
            var x = Dynamics.DecodeSigned(minsky.X.Read(mStores[0], 0)!.Value, 12);
            var v = Dynamics.DecodeSigned(minsky.V.Read(mStores[0], 0)!.Value, 12);
            var absX = x < 0 ? -x : x;
            if (absX > maxAbsX) maxAbsX = absX;
            if (period < 0 && x == 10 && v == 0) period = t;
        }
        Assert.Equal(30, period);
        Assert.Equal(10, maxAbsX);
        Assert.Equal((10, 0), (Dynamics.DecodeSigned(minsky.X.Read(mStores[0], 0)!.Value, 12),
            Dynamics.DecodeSigned(minsky.V.Read(mStores[0], 0)!.Value, 12)));

        // Together=true on the exact same Minsky machine spirals past ±12 at step 13!
        var minskyTog = BuildOsc(maxAbs: 12, shift: 2, x0: 10, v0: 0, together: true);
        var hitBoundaryAt = -1;
        for (var t = 1; t <= 30; t++)
        {
            if (minskyTog.Engine.Advance().Steps.Any(s => s.Unadmitted.Any)) { hitBoundaryAt = t; break; }
        }
        Assert.Equal(13, hitBoundaryAt);
    }

    /// <summary>X10 (2×3 order-and-sign family + waveform refinement):
    /// 1st-order m=+1 (Napier halvings at steps 6, 11, 17), m=0 (hold), m=-1
    /// (exponential growth); 2nd-order m=+1 (sin/cos), m=0 (linear coast
    /// multiplication), m=-1 (hyperbolic runaway); plus constant-sign parabolic
    /// arcs vs distance-proportional sine pull.</summary>
    [Fact]
    public void UnifiedOrderAndSignFamilyCoversNapierLogSinCosCoastExpAndParabolicRefinement()
    {
        // 1. First-order family over 0..256 (shift=3):
        int[] RunFirstOrder(int start, int m, int steps)
        {
            var store = new Store(0, 1);
            var f = new Field(Shape.Binary(257), [.. Enumerable.Range(0, 9).Select(_ => store.AddPlane())]);
            f.Write(store, 0, start);
            var engine = new Engine().Add(Tension.Wire(Dynamics.FirstOrderStep(256, shift: 3, feedbackSign: m), [f.On(store)], Here));
            var trace = new int[steps + 1];
            trace[0] = start;
            for (var i = 1; i <= steps; i++)
            {
                engine.Advance();
                trace[i] = f.Read(store, 0)!.Value;
            }
            return trace;
        }

        // m = +1: Napier relaxation — equal ratios (256 -> 128 -> 64 -> 32) take near-equal steps (6, 11, 17).
        var decay = RunFirstOrder(256, +1, 20);
        Assert.Equal(6, Array.FindIndex(decay, v => v <= 128));
        Assert.Equal(11, Array.FindIndex(decay, v => v <= 64));
        Assert.Equal(17, Array.FindIndex(decay, v => v <= 32));

        // m = 0: holds constant.
        Assert.All(RunFirstOrder(256, 0, 6), v => Assert.Equal(256, v));

        // m = -1: exponential compounding from 32.
        Assert.Equal([32, 36, 40, 45, 50, 56, 63, 70], RunFirstOrder(32, -1, 7));

        // 2. Second-order family (maxAbs = 18, shift = 1, x0 = 4, v0 = 4) switching solely on m in {+1, 0, -1}:
        int[] RunSecondOrder(int m, int steps)
        {
            const int MaxAbs = 18;
            var store = new Store(0, 1);
            var fx = SignedField(store, MaxAbs);
            var fv = SignedField(store, MaxAbs);
            fx.Write(store, 0, Dynamics.EncodeSigned(4, MaxAbs));
            fv.Write(store, 0, Dynamics.EncodeSigned(4, MaxAbs));
            var engine = new Engine()
                .Add(Tension.Wire(Dynamics.SignedShear(MaxAbs, shift: 1, sign: -m, admitOutOfRange: true), [fx.On(store), fv.On(store)], Here))
                .Add(Tension.Wire(Dynamics.SignedShear(MaxAbs, shift: 1, sign: +1, admitOutOfRange: true), [fv.On(store), fx.On(store)], Here));
            var trace = new int[steps + 1];
            trace[0] = 4;
            for (var i = 1; i <= steps; i++)
            {
                engine.Advance();
                trace[i] = Dynamics.DecodeSigned(fx.Read(store, 0)!.Value, MaxAbs);
            }
            return trace;
        }

        Assert.Equal([4, 5, 5, 4, 2, 0, -2], RunSecondOrder(+1, 6));  // circular (sin/cos)
        Assert.Equal([4, 6, 8, 10, 12, 14, 16], RunSecondOrder(0, 6)); // linear coasting (multiplication 4 + 2*t)
        Assert.Equal([4, 7, 11, 18], RunSecondOrder(-1, 3));           // hyperbolic runaway (cosh/sinh)

        // 3. Waveform refinement: constant-sign restoring pull gives parabolic x (linear triangle v).
        var pStore = new Store(0, 1);
        var px = SignedField(pStore, 10);
        var pv = SignedField(pStore, 10);
        px.Write(pStore, 0, Dynamics.EncodeSigned(6, 10));
        pv.Write(pStore, 0, Dynamics.EncodeSigned(0, 10));
        var pEngine = new Engine()
            .Add(Tension.Wire(Dynamics.ConstantSignKick(10, step: 1), [px.On(pStore), pv.On(pStore)], Here))
            .Add(Tension.Wire(Dynamics.SignedShear(10, shift: 0, sign: +1, admitOutOfRange: false), [pv.On(pStore), px.On(pStore)], Here));
        var pxTrace = new List<int> { 6 };
        var pvTrace = new List<int> { 0 };
        for (var i = 0; i < 4; i++)
        {
            pEngine.Advance();
            pxTrace.Add(Dynamics.DecodeSigned(px.Read(pStore, 0)!.Value, 10));
            pvTrace.Add(Dynamics.DecodeSigned(pv.Read(pStore, 0)!.Value, 10));
        }
        // Constant deceleration -1 while x > 0: v = 0, -1, -2, -3 (linear!) -> x = 6, 5, 3, 0 (parabolic: differences -1, -2, -3!).
        Assert.Equal([0, -1, -2, -3, -3], pvTrace);
        Assert.Equal([6, 5, 3, 0, -3], pxTrace);
    }
}
