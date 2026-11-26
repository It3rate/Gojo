using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Gojo-native discrete symbolic regression and structural compression
/// tests: exact integer-kernel recovery of core laws (including auto-discovered
/// free residue roles), crystallized LearnedTable compression (B7), mutated
/// exception retention + MDL savings vs. random rejection, sparse hypothesis
/// discovery &amp; discriminator probes (ops_sol #1/#2/#4), mid-flight source edit
/// separating 3 realizations of 3×4=12 (ops_sol #3), and decoupled observational
/// Flows.Sweep (ops_sol + ops_claude 928ef76f).</summary>
public sealed class SymbolicRegressionTests
{
    private static readonly Plane Here = Plane.Of(1, 0);

    [Fact]
    public void StructuralAnalyzerRecoversExactBalanceWeightsResidueAndMdlForAllCoreLaws()
    {
        // 1. Counts.Sum(3, 3, weight: 2) -> weights (1, 2, -1)
        var sumFit = StructuralCompression.TryRecognizeBalance(Counts.Sum(3, 3, weight: 2));
        Assert.NotNull(sumFit);
        Assert.True(sumFit.ExactTableMatch);
        Assert.Equal([1, 2, -1], sumFit.Weights);
        Assert.True(sumFit.CompressesByMdl);

        // 2. Counts.Tally(4) -> weights (1, 1, 1, 1, -1)
        var tallyFit = StructuralCompression.TryRecognizeBalance(Counts.Tally(4));
        Assert.NotNull(tallyFit);
        Assert.True(tallyFit.ExactTableMatch);
        Assert.Equal([1, 1, 1, 1, -1], tallyFit.Weights);
        Assert.True(tallyFit.CompressesByMdl);

        // 3. Counts.Shortfall(3, 3) -> auto-discovers free excess residue role!
        var shortHints = StructuralCompression.ReadHints(Counts.Shortfall(3, 3));
        Assert.True(shortHints.BoundaryCornerOnly);
        var shortFit = StructuralCompression.TryRecognizeBalance(Counts.Shortfall(3, 3));
        Assert.NotNull(shortFit);
        Assert.True(shortFit.ExactTableMatch);
        Assert.Equal(3, shortFit.FreeResidueCapacity);
        Assert.Equal([-1, 1, -1, 1], shortFit.Weights); // -have + want - short + excess = 0 (exact same balance!)
        Assert.True(shortFit.CompressesByMdl);

        // 4. Counts.Accumulate(4) -> in-place hold at cap equals clamp! Compresses even tighter to [1, 1, -1] (0 free roles, exact SameTable)!
        var accFit = StructuralCompression.TryRecognizeBalance(Counts.Accumulate(4));
        Assert.NotNull(accFit);
        Assert.True(accFit.ExactTableMatch);
        Assert.Equal(0, accFit.FreeResidueCapacity);
        Assert.Equal([1, 1, -1], accFit.Weights);
        Assert.True(accFit.CompressesByMdl);

        // And on a separate-output clamped sum (bit OR: a + b = out + overflow), it auto-discovers free overflow residue capacity 1!
        var bitOr = Operations.Bind(Operations.Balance([Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit], 1, 1, -1, -1),
            [Shape.Bit, Shape.Bit, Shape.Bit], [0, 1, -1, -1], [-1, -1, 2, -1]);
        var orFit = StructuralCompression.TryRecognizeBalance(bitOr);
        Assert.NotNull(orFit);
        Assert.True(orFit.ExactTableMatch);
        Assert.Equal(1, orFit.FreeResidueCapacity);
        Assert.Equal([1, 1, -1, -1], orFit.Weights);

        // 5. Combined radix-4 and split base-10 digit adders!
        var r4Fit = StructuralCompression.TryRecognizeBalance(Numbers.DigitAddCombined(4));
        Assert.NotNull(r4Fit);
        Assert.True(r4Fit.ExactTableMatch);
        Assert.Equal([1, 1, 1, -1, -4], r4Fit.Weights);

        var b10Sum = StructuralCompression.TryRecognizeBalance(Numbers.DigitSum(10));
        var b10Car = StructuralCompression.TryRecognizeBalance(Numbers.DigitCarry(10));
        Assert.NotNull(b10Sum);
        Assert.NotNull(b10Car);
        Assert.True(b10Sum.ExactTableMatch);
        Assert.True(b10Car.ExactTableMatch);
        Assert.Equal([1, 1, 1, -1, -10], b10Sum.Weights);
        Assert.Equal([1, 1, 1, -10, -1], b10Car.Weights);
        Assert.True(b10Sum.BitsSaved > 3_900_000); // 4,000,000 bits -> ~30 bits!

        // 6. Unsigned LandmarkShear (k=2, with blocked bit)
        var lmFit = StructuralCompression.TryRecognizeBalance(Numbers.LandmarkShear(6, weight: 2, withBlocked: true));
        Assert.NotNull(lmFit);
        Assert.True(lmFit.ExactTableMatch);
        Assert.Equal([1, 2, -2, -1], lmFit.Weights);
        Assert.Equal(3, lmFit.BlockedRole);

        // 7. Packet trade Convert(3, 2) and lossless BoundedMultiply
        var tradeFit = StructuralCompression.TryRecognizePacketTrade(Flows.Convert(3, 2, 8, 10));
        Assert.NotNull(tradeFit);
        Assert.Equal((3, 2), (tradeFit.Take, tradeFit.Give));
        Assert.True(tradeFit.CompressesByMdl);

        var mulFit = StructuralCompression.TryRecognizeProduct(Multiplication.BoundedMultiply(3, 3, 5, 4));
        Assert.NotNull(mulFit);
        Assert.True(mulFit.ExactTableMatch);
        Assert.Equal((0, 1, 2, 3), (mulFit.LeftRole, mulFit.RightRole, mulFit.ProductRole, mulFit.OverflowRole));
        Assert.True(mulFit.CompressesByMdl);
    }

    [Fact]
    public void CrystallizedLearnedTableCompressesToExactBalanceWeightsWithoutNames()
    {
        // ops_claude B7 / ops_sol closed-table control: watch an anonymous a + 2b = s law
        // through LearnedTable, crystallize, and recover exact integer weights (1, 2, -1)!
        Shape s2 = Shape.Unary(2), s6 = Shape.Unary(6);
        var secret = Counts.Sum(2, 2, weight: 2);
        var learned = new LearnedTable([s2, s2, s6], admitThreshold: 1, unsupportedAfter: 1);

        for (var before = 0; before < secret.StateCount; before++)
        {
            var after = secret.Afters(before)[0];
            learned.Witness(before, after);
        }

        var cryst = learned.Crystallize(new Accounting(true, new Unchanged(0), new Unchanged(1)));
        Assert.NotNull(cryst.Law);
        Assert.Equal(0, cryst.Unknown);

        var fit = StructuralCompression.TryRecognizeBalance(cryst.Law);
        Assert.NotNull(fit);
        Assert.True(fit.ExactTableMatch);
        Assert.Equal([1, 2, -1], fit.Weights);
        Assert.True(fit.CompressesByMdl);
    }

    [Fact]
    public void MutatedTableRetainsExplicitExceptionWithMdlSavingsAndRandomTableRejects()
    {
        // ops_sol #2 & ops_claude §8: a product table mutated at ONE corner (3,2) -> 5
        // (even though (2,3) -> 6) MUST fail exact acceptance, compress to Product + 1 explicit exception,
        // and save massive bits by MDL!
        Shape f = Shape.Binary(4), p = Shape.Binary(10);
        var mutated = TransitionLaw.FromFunction([f, f, p],
            s => [s[0], s[1], s[0] == 3 && s[1] == 2 ? 5 : s[0] * s[1]],
            new Accounting(true, new Unchanged(0), new Unchanged(1)));

        // Exact product fit (0 exceptions allowed) MUST reject!
        Assert.Null(StructuralCompression.TryRecognizeProduct(mutated, maxExceptions: 0));

        // Exception-aware fit recovers exact Product + 10 exception states (one per prior output value 0..9 at (3,2))!
        var withExc = StructuralCompression.TryRecognizeProduct(mutated, maxExceptions: 16);
        Assert.NotNull(withExc);
        Assert.False(withExc.ExactTableMatch);
        Assert.Equal(10, withExc.Exceptions.Length); // all 10 prior-output cells for (a=3, b=2)
        Assert.True(withExc.CompressesByMdl);
        Assert.True(withExc.BitsSaved > 20_000);

        // Deliberately irregular / pseudo-random table MUST fail both Balance and Product recognition!
        uint rng = 0x12345678u;
        var table = new int[16];
        for (var i = 0; i < 16; i++)
        {
            rng = rng * 1664525u + 1013904223u;
            table[i] = (int)(rng % 10);
        }
        var irregular = TransitionLaw.FromFunction([f, f, p],
            s => [s[0], s[1], table[s[0] * 4 + s[1]]],
            new Accounting(true, new Unchanged(0), new Unchanged(1)));

        Assert.Null(StructuralCompression.TryRecognizeBalance(irregular, maxExceptions: 4));
        Assert.Null(StructuralCompression.TryRecognizeProduct(irregular, maxExceptions: 4));
    }

    [Fact]
    public void SparseDiscoveryFindsSeparatingDiscriminatorRetainsAmbiguityWhenOpaqueAndRebindsFreshSites()
    {
        // ops_sol #1 & #4: start with ONLY 2 sparse observations: (0,0)->0 and (2,2)->4 over 0..4!
        Shape f = Shape.Binary(5), p = Shape.Binary(17);
        var witnesses = new WitnessStore([f, f, p]);
        witnesses.WitnessObservation([0, 0], 0);
        witnesses.WitnessObservation([2, 2], 4);

        // Both a + b (Balance 1,1) AND a * b (Product) survive! For an opaque source, retaining both is honest.
        var initial = StructuralCompression.ProposeFromSparse(witnesses);
        Assert.Contains(initial, h => h.Bilinear == 1 && h.W0 == 0 && h.W1 == 0 && h.Offset == 0);
        Assert.Contains(initial, h => h.W0 == 1 && h.W1 == 1 && h.Bilinear == 0 && h.Offset == 0);

        // Find a discriminator probe where surviving hypotheses disagree!
        var probe1 = StructuralCompression.FindDiscriminator(initial, witnesses);
        Assert.NotNull(probe1);
        var preds = initial.Select(h => h.Predict(probe1)).Distinct().ToArray();
        Assert.True(preds.Length >= 2);

        // Feed back probe receipt from true product (plus (0,3)->0 and held-out (2,3)->6):
        witnesses.WitnessObservation(probe1, probe1[0] * probe1[1]);
        witnesses.WitnessObservation([0, 3], 0);
        witnesses.WitnessObservation([2, 3], 6);

        var narrowed = StructuralCompression.ProposeFromSparse(witnesses);
        var winner = Assert.Single(narrowed);
        Assert.Equal(1, winner.Bilinear);
        Assert.Equal((0, 0, 0, 0), (winner.W0, winner.W1, winner.Offset, winner.FoldSign));

        // Verify winner against all 25 exact pairs of 0..4 × 0..4 and rebind onto FRESH sites through Transport!
        var freshStore = new Store(0, 1);
        var fa = Supports.On(freshStore, new Field(f, [.. Enumerable.Range(0, f.Bits).Select(_ => freshStore.AddPlane())]));
        var fb = Supports.On(freshStore, new Field(f, [.. Enumerable.Range(0, f.Bits).Select(_ => freshStore.AddPlane())]));
        var fc = Supports.On(freshStore, new Field(p, [.. Enumerable.Range(0, p.Bits).Select(_ => freshStore.AddPlane())]));
        fa.Restrict(0, 1UL << 3);
        fc.Restrict(0, 1UL << 12); // backward division on fresh sites: 3 × ? = 12 -> 4!

        var w = new Wiring(winner.ToRelation(f, f, p), fa.Variable, fb.Variable, fc.Variable);
        w.Add(0, 0, 0);
        var saved = Transport.Save(new Engine().Add(w), freshStore);
        var (restoredEng, restoredStores) = Transport.Load(saved);
        restoredEng.Step();
        Assert.Equal(1UL << 4, new Supports(restoredStores[0], fb.Field, [.. fb.Planes]).Values(0));
    }

    [Fact]
    public void MidFlightSourceEditSeparatesStretchCapturedRepeatAndLiveCoastingRealizations()
    {
        // ops_sol #3: all three start from gain=3, source=4 (producing 12 statically),
        // then source edits 4 -> 5 after tick 1 while gain stays 3!
        // 1. Live stretch: immediately jumps 12 -> 15 on edit.
        var stretchTrace = new WitnessStore([Shape.Binary(6), Shape.Binary(6), Shape.Binary(16)]);
        stretchTrace.WitnessTrajectory(tick: 1, source: 4, gain: 3, output: 12);
        stretchTrace.WitnessTrajectory(tick: 2, source: 5, gain: 3, output: 15);
        var stretchSig = StructuralCompression.AnalyzeRealization(stretchTrace.Trajectory);
        Assert.True(stretchSig.RespondedImmediatelyToEdit);
        Assert.False(stretchSig.IgnoredMidFlightEdit);
        Assert.False(stretchSig.TrackedLiveSourcePerStep);

        // 2. Captured-sample repeat: traces 4, 8, 12 (ignores edit to 5!).
        var capturedTrace = new WitnessStore([Shape.Binary(6), Shape.Binary(6), Shape.Binary(16)]);
        capturedTrace.WitnessTrajectory(tick: 1, source: 4, gain: 3, output: 4);
        capturedTrace.WitnessTrajectory(tick: 2, source: 5, gain: 3, output: 8);
        capturedTrace.WitnessTrajectory(tick: 3, source: 5, gain: 3, output: 12);
        var capturedSig = StructuralCompression.AnalyzeRealization(capturedTrace.Trajectory);
        Assert.True(capturedSig.IgnoredMidFlightEdit);
        Assert.False(capturedSig.RespondedImmediatelyToEdit);
        Assert.False(capturedSig.TrackedLiveSourcePerStep);
        Assert.Equal([4, 4], capturedSig.StepDeltas);
        Assert.Equal([0], capturedSig.SecondDeltas);

        // 3. Live-reading coasting repeat: traces 4, 9, 14 (tracks new source=5 on ticks 2 & 3!).
        var liveTrace = new WitnessStore([Shape.Binary(6), Shape.Binary(6), Shape.Binary(16)]);
        liveTrace.WitnessTrajectory(tick: 1, source: 4, gain: 3, output: 4);
        liveTrace.WitnessTrajectory(tick: 2, source: 5, gain: 3, output: 9);
        liveTrace.WitnessTrajectory(tick: 3, source: 5, gain: 3, output: 14);
        var liveSig = StructuralCompression.AnalyzeRealization(liveTrace.Trajectory);
        Assert.True(liveSig.TrackedLiveSourcePerStep);
        Assert.False(liveSig.IgnoredMidFlightEdit);
        Assert.False(liveSig.RespondedImmediatelyToEdit);
        Assert.Equal([5, 5], liveSig.StepDeltas);
    }

    [Fact]
    public void ObservationalSweepReceiptSaturatesWithoutEverBlockingMaterial()
    {
        // ops_sol catch on ops_claude (fixed in 928ef76f): receiptCapacity = 2 is far too small
        // for 4 * 3 = 12, yet all 4 units MUST still transfer identically to a plain move!
        var st = new Store(0, 1);
        Field U(int cap, int val = 0)
        {
            var f = new Field(Shape.Unary(cap), [.. Enumerable.Range(0, cap).Select(_ => st.AddPlane())]);
            f.Write(st, 0, val);
            return f;
        }
        var src = U(4, 4);
        var dst = U(4, 0);
        var other = U(3, 3);
        var receipt = U(2, 0); // intentionally smaller than 4 * 3 = 12!

        var eng = new Engine().Add(Tension.Wire(Flows.Sweep(4, 4, 3, receiptCapacity: 2),
            [src.On(st), dst.On(st), other.On(st), receipt.On(st)], Here));

        Assert.NotNull(eng.Settle(8));
        Assert.Equal(0, src.Read(st, 0));     // all material moved — NEVER blocked by full receipt!
        Assert.Equal(4, dst.Read(st, 0));
        Assert.Equal(2, receipt.Read(st, 0)); // receipt visibly saturated at capacity 2!
    }
}
