using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using Phototaxis;
using static Phototaxis.Native;

namespace Phototaxis.Tests;

public class CompactExecutionTests
{
    private static void SameRead(IElement source)
    {
        var reference = ReferenceRead(source);
        using var compact = new NativeExecution(NativeExecutionBackend.Compact);
        ReadMany(source); // Exercise both the first-use reader and the promoted execution plan.
        var observed = ReadMany(source)[0];
        Assert.Equal(TryValue(reference, out var expected), TryValue(observed, out var actual));
        if (TryValue(reference, out _))
        {
            Assert.Equal(expected, actual);
            Assert.True(((CarrierLocation)reference.Left).Carrier.SameIdentityAs(((CarrierLocation)observed.Left).Carrier));
            Assert.Equal(Coordinates(reference.Right), Coordinates(observed.Right));
        }
    }

    // Compare ordered retained coordinates, including exact fractional source
    // residues. Disposable ordered-group anchors are not physical observations.
    private static (ulong Carrier, long Numerator, long Denominator)[] Coordinates(IElement residue)
    {
        var result = new List<(ulong, long, long)>();
        var active = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        void Walk(IElement value)
        {
            if (ProjectionResidue.TryRead(value, out var axis, out var n, out var d))
            { result.Add((axis.Id, n, d)); return; }
            if (value is not Relation r || !active.Add(r)) return;
            if (r.TryReadOrderedGroup(out var parts)) foreach (var part in parts) Walk(part);
            else { Walk(r.Left); Walk(r.Right); }
            active.Remove(r);
        }
        Walk(residue); return result.ToArray();
    }

    [Theory]
    [InlineData(7, 3, 2)]
    [InlineData(-7, 3, 2)]
    [InlineData(7, -3, 2)]
    [InlineData(7, 3, -2)]
    [InlineData(7, 0, 2)]
    public void SignedProjectionsRetainTheExactNativeSourceSubdivision(long value, long numerator, long denominator)
    {
        var source = new Carrier();
        SameRead(Scale(L(source, value), numerator, denominator));
    }

    [Fact]
    public void ArithmeticAndBooleanCompositionMatchNativeValuesAndOrderedResidue()
    {
        var u = new Units();
        foreach (var x in new long[] { -255, -7, 0, 1, 15, 255 })
        foreach (var y in new long[] { -17, -1, 0, 1, 15 })
        {
            var a = L(u.Feature, x); var b = L(u.Feature, y);
            SameRead(ExpressionGraph.Add(u, a, b));
            SameRead(ExpressionGraph.Subtract(u, a, b));
            SameRead(ExpressionGraph.Multiply(u, a, b));
            SameRead(ExpressionGraph.Quotient(u, a, b));
            var less = ExpressionGraph.Less(u, a, b); var equal = ExpressionGraph.Equal(u, a, b);
            SameRead(less); SameRead(equal);
            SameRead(ExpressionGraph.And(u, less, ExpressionGraph.Not(u, equal)));
            SameRead(ExpressionGraph.Choose(u, less, ExpressionGraph.Add(u, a, b), ExpressionGraph.Subtract(u, a, b)));
        }
    }

    [Fact]
    public void LazyChoiceKeepsItsNativeSelectionWitnessAndDoesNotReadAnOverflowingAlternative()
    {
        var u = new Units(); var bad = Product(L(u.Feature, long.MaxValue), L(u.Feature, long.MaxValue));
        var selected = L(u.Feature, 9);
        var root = ExpressionGraph.Choose(u, L(u.Feature, 15), selected, bad);
        using var compact = new NativeExecution(NativeExecutionBackend.Compact);
        ReadMany(root);
        var read = ReadMany(root)[0];
        Assert.True(TryValue(read, out var value)); Assert.Equal(9, value); Assert.Equal(0, compact.Fallbacks);
        var retained = Parts((Relation)read.Right);
        Assert.True(ReadWitness.TryRead((Relation)retained[0], out var source, out var scope, out var offer, out _, out _));
        Assert.Same(root, source);
        Assert.True(ScopedChoice.TryRead(root, out _, out var nativeScope, out var offers, out _));
        Assert.Same(nativeScope, scope); Assert.Same(offers[1], offer);
        SameRead(root);
    }

    [Fact]
    public void OpenUnchosenBranchesRemainLazyAndTheChosenOpenBranchRemainsUnresolved()
    {
        var u = new Units(); var selector = L(u.Feature, 15);
        var root = ExpressionGraph.Choose(u, selector, L(u.Feature, 9), new Placeholder());
        SameRead(root);
        using var compact = new NativeExecution(NativeExecutionBackend.Compact);
        Assert.True(TryValue(ReadMany(root)[0], out var value)); Assert.Equal(9, value);
        selector.Tick.Set(0);
        Assert.False(TryValue(ReadMany(root)[0], out _)); Assert.True(compact.Fallbacks > 0);
    }

    [Fact]
    public void AmbiguousSupportAndGapsHaveNoInventedNumericAnswer()
    {
        var axis = new Carrier(); var output = new Carrier(); var sample = L(axis, 5);
        Relation Offer(long a, long b, long value) => ScopedChoice.Offer(Relation.Segment(axis, a, b), L(axis, value),
            Equation.Affine(axis, 0, 1, output, 0, 1));
        var overlap = ScopedChoice.Create(sample, Relation.Segment(axis, 0, 16), output, Offer(0, 10, 3), Offer(5, 16, 9));
        var gap = ScopedChoice.Create(sample, Relation.Segment(axis, 0, 16), output, Offer(0, 4, 3), Offer(6, 16, 9));
        using var compact = new NativeExecution(NativeExecutionBackend.Compact);
        ReadMany(overlap); ReadMany(gap);
        Assert.False(TryValue(ReadMany(overlap)[0], out _)); Assert.False(TryValue(ReadMany(gap)[0], out _));
        Assert.Equal(2, compact.Fallbacks);
    }

    [Fact]
    public void SharedBindingConflictsAndWrongCarrierIdentityRemainFailedReads()
    {
        var axis = new Carrier(); var output = new Carrier(); var tick = new TickRef(0);
        var a = new CarrierLocation(axis, tick); var b = new CarrierLocation(axis, tick);
        var equation = Equation.Sum(output, Equation.UnitPort(axis), Equation.UnitPort(axis));
        var root = G(equation, new Relation(a, b), output, G(new Relation(a, L(axis, 2)), new Relation(b, L(axis, 3))));
        using var compact = new NativeExecution(NativeExecutionBackend.Compact);
        Assert.False(TryValue(ReadMany(root)[0], out _)); Assert.False(TryValue(ReferenceRead(root), out _));
        var mismatch = G(Equation.Affine(axis, 0, 1, output, 0, 1), a, output, G(new Relation(a, L(new Carrier(), 2))));
        Assert.False(TryValue(ReadMany(mismatch)[0], out _));
    }

    [Fact]
    public void RetargetedBasesAreLiveAndEarlierReadsAndCallerEditsCannotPoisonThePlan()
    {
        var axis = new Carrier(); var root = Scale(L(axis, 7), 3, 2);
        using var compact = new NativeExecution(NativeExecutionBackend.Compact);
        var first = ReadMany(root)[0]; var residue = Coordinates(first.Right);
        var equation = (Relation)Parts(root)[0];
        var sourceFrame = (Relation)equation.Left;
        var endpoint = (CarrierLocation)((Relation)sourceFrame.Right).Left;
        endpoint.Tick.Set(7);
        Assert.Equal(3, ((CarrierLocation)ReadMany(root)[0].Left).Ticks);
        Assert.Equal(10, ((CarrierLocation)first.Left).Ticks); Assert.Equal(residue, Coordinates(first.Right));
        ((CarrierLocation)first.Left).Tick.Set(1234);
        Assert.Equal(3, ((CarrierLocation)ReadMany(root)[0].Left).Ticks);
        endpoint.Tick.Set(0); Assert.False(TryValue(ReadMany(root)[0], out _));
        endpoint.Tick.Set(2); Assert.Equal(10, ((CarrierLocation)ReadMany(root)[0].Left).Ticks);
        Assert.Equal(1, compact.PlanBuilds); Assert.True(compact.CacheHits >= 3);
    }

    [Fact]
    public void WiderExactIntermediateArithmeticFallsBackWithoutChangingTheResult()
    {
        var axis = new Carrier(); var output = new Carrier();
        var inputs = Enumerable.Range(0, 4).Select(_ => L(axis, 0)).ToArray();
        IElement tree = inputs[0];
        for (var i = 1; i < inputs.Length; i++) tree = new Relation(tree, inputs[i]);
        var equation = Equation.Product(output, inputs.Select(_ => Equation.UnitPort(axis)).ToArray());
        var root = G(equation, tree, output, G(inputs.Select((input, i) => (IElement)new Relation(input,
            L(axis, i == 3 ? 0 : long.MaxValue))).ToArray()));
        using var compact = new NativeExecution(NativeExecutionBackend.Compact);
        ReadMany(root);
        Assert.True(TryValue(ReadMany(root)[0], out var value)); Assert.Equal(0, value);
        Assert.True(compact.Fallbacks > 0); SameRead(root);
        Assert.Throws<OverflowException>(() => ReadMany(Product(L(axis, long.MaxValue), L(axis, long.MaxValue))));
    }

    [Fact]
    public void InverseFlowAndUnfamiliarEquationFamiliesKeepTheNativeReader()
    {
        var axis = new Carrier(); var inverse = axis.Inverse();
        using var compact = new NativeExecution(NativeExecutionBackend.Compact);
        SameRead(Product(L(inverse, 7), L(axis, 3)));
        var root = Product(L(inverse, 7), L(axis, 3));
        ReadMany(root);
        var read = ReadMany(root)[0];
        Assert.True(TryValue(read, out var value)); Assert.Equal(21, value);
        Assert.True(((CarrierLocation)read.Left).Carrier.IsInverse); Assert.True(compact.Fallbacks > 0);
        Assert.False(TryValue(ReadMany(new Placeholder())[0], out _));
        var middle = new Carrier(); var output = new Carrier(); var input = L(axis, 7);
        var sequence = EquationSequence.Create(new Carrier(), Equation.Affine(axis, 0, 2, middle, 0, 3),
            Equation.Affine(middle, 0, 1, output, 0, -1));
        var serial = G(sequence, input, output, G());
        SameRead(serial); ReadMany(serial);
        Assert.Equal(-10, ((CarrierLocation)ReadMany(serial)[0].Left).Ticks);
    }

    [Fact]
    public void OneShotProgramsStayNativeAndReusedProgramsPromoteWithoutChangingTheirMeaning()
    {
        using var compact = new NativeExecution(NativeExecutionBackend.Compact);
        var roots = Enumerable.Range(0, 3).Select(i => Scale(L(new Carrier(), 7 + i), 3, 2)).ToArray();
        foreach (var root in roots) ReadMany(root);
        Assert.Equal(0, compact.PlanBuilds); Assert.Equal(3, compact.DeferredBatches);
        SameRead(roots[0]);
        Assert.Equal(10, ((CarrierLocation)ReadMany(roots[0])[0].Left).Ticks);
        Assert.True(compact.NumericSteps > 0);
    }

    [Fact]
    public void NativeTransportAndExecutionCorrespondenceRemainIndependentOfTheCache()
    {
        var u = new Units(); var root = ExpressionGraph.Choose(u, L(u.Feature, 15), Scale(L(u.Feature, 7), 3, 2), new Placeholder());
        var map = ExecutionCorrespondence(root);
        Assert.Same(root, Parts(map)[0]);
        Assert.Contains(Parts((Relation)Parts(map)[1]), row => row is Relation r && ReferenceEquals(r.Right, root));
        var restored = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(root)).Single();
        SameRead(restored);
        using var compact = new NativeExecution(NativeExecutionBackend.Compact);
        Assert.Equal(10, ((CarrierLocation)ReadMany(restored)[0].Left).Ticks);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(19)]
    public void MatchedWorldsKeepIdenticalPhysicalAndFeedbackStateAcrossTime(int seed)
    {
        var planned = new Bath(seed, 6, rocks: true, resources: MachineResources.Fixed(.9), pulsedLights: true,
            cognitiveEcology: true, ecology: EcologySettings.Roving);
        var compact = new Bath(seed, 6, rocks: true, resources: MachineResources.Fixed(.9), pulsedLights: true,
            cognitiveEcology: true, ecology: EcologySettings.Roving);
        for (var tick = 0; tick < 600; tick++)
        {
            using (new NativeExecution(NativeExecutionBackend.Planned)) planned.Step();
            using (new NativeExecution(NativeExecutionBackend.Compact)) compact.Step();
            if (tick % 20 != 0) continue;
            Assert.Equal(State(planned), State(compact));
        }
        Assert.Equal(State(planned), State(compact));
        planned.CheckConservation(); compact.CheckConservation();
    }

    private static string State(Bath bath)
    {
        static long[] HeldStates(params Carrier[] carriers) => carriers.SelectMany(c => new[] { checked((long)c.Value), c.Tension }).ToArray();
        return System.Text.Json.JsonSerializer.Serialize(new { bath.Tick, bath.TotalFood, bath.Births,
            bath.PredationDeaths, bath.StarvationDeaths, credits = bath.SpentAttention.Value,
            bodies = bath.Creatures.Select(c => new { c.Slot, c.BornAt, x = c.X.Ticks, y = c.Y.Ticks, angle = c.Heading.Ticks,
                material = HeldStates(c.Energy, c.Eaten, c.Age, c.NextMating, c.Wallet, c.Computation.Runtime, c.Computation.Billed),
                ports = HeldStates(c.LeftEye.Pixels[0], c.RightEye.Pixels[0], c.LeftEye.SampleTime, c.RightEye.SampleTime,
                    c.LeftMuscle.Force, c.RightMuscle.Force, c.LeftMuscle.NextTime, c.RightMuscle.NextTime),
                feedback = Parts(c.ControllerState).Cast<Relation>().Select(cell => HeldStates((Carrier)Parts(cell)[0], (Carrier)Parts(cell)[4])).ToArray(),
                memories = c.Cognition is { } b ? HeldStates(b.PreviousX, b.PreviousY, b.PreviousTime, b.PreviousEnergy,
                    b.PreviousIntake, b.Ready, b.SpeedSquared, b.Forecast, b.PersistenceForecast, b.ForecastTime, b.Pressure,
                    b.Error, b.Blocked, b.PainMemory, b.Samples, b.PredictionChecks, b.ErrorTotal) : [] }).ToArray() });
    }
}
