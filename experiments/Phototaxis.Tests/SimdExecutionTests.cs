using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using Phototaxis;
using static Phototaxis.Native;

namespace Phototaxis.Tests;

public class SimdExecutionTests
{
    private static Relation Pair(long x, long y, long coefficient = 3, long offset = 0, long denominator = 1)
    {
        var axis = new Carrier(); var output = new Carrier();
        var a = L(axis, 0); var b = L(axis, 0);
        var equation = Equation.Sum(output, Equation.WeightedPort(axis, 0, denominator, offset, offset + coefficient),
            Equation.WeightedPort(axis, 0, 1, 0, -2));
        return G(equation, new Relation(a, b), output,
            G(new Relation(a, L(axis, x)), new Relation(b, L(axis, y))));
    }

    private static (ulong Carrier, long Numerator, long Denominator)[] Coordinates(IElement residue)
    {
        var values = new List<(ulong, long, long)>();
        var active = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        void Walk(IElement element)
        {
            if (ProjectionResidue.TryRead(element, out var axis, out var n, out var d))
            { values.Add((axis.Id, n, d)); return; }
            if (element is not Relation r || !active.Add(r)) return;
            if (r.TryReadOrderedGroup(out var parts)) foreach (var part in parts) Walk(part);
            else { Walk(r.Left); Walk(r.Right); }
            active.Remove(r);
        }
        Walk(residue); return values.ToArray();
    }

    private static Relation SameRead(Relation root)
    {
        var expected = ReferenceRead(root);
        ReadMany(root); // Promote this stable root before the measured read.
        var actual = ReadMany(root)[0];
        Assert.Equal(TryValue(expected, out var a), TryValue(actual, out var b));
        Assert.Equal(a, b);
        Assert.True(((CarrierLocation)expected.Left).Carrier.SameIdentityAs(((CarrierLocation)actual.Left).Carrier));
        Assert.Equal(Coordinates(expected.Right), Coordinates(actual.Right));
        return actual;
    }

    [Theory]
    [InlineData(-255, 17, 3, 0)]
    [InlineData(255, -17, -3, 0)]
    [InlineData(67108864, -67108864, 1048576, 70368744177664)]
    [InlineData(-67108864, 67108864, -1048576, -70368744177664)]
    public void IntegralLaneArithmeticIsExactThroughTheDocumentedBounds(long x, long y, long coefficient, long offset)
    {
        using var run = new NativeExecution(NativeExecutionBackend.CompactSimd);
        SameRead(Pair(x, y, coefficient, offset));
        Assert.Equal(NativeExecution.SimdSupported ? 1 : 0, run.SimdBatches);
        Assert.Equal(run.SimdBatches * 2, run.SimdLanes);
    }

    [Theory]
    [InlineData(7, 3, 0, 2)] // Fractional projection retains its source subdivision.
    [InlineData(67108865, 3, 0, 1)] // Wider input.
    [InlineData(7, 1048577, 0, 1)] // Wider coefficient.
    [InlineData(7, 3, 70368744177665, 1)] // Wider target offset.
    [InlineData(7, 0, 0, 1)] // Constant target keeps the whole input as residue.
    public void NoneligibleLanesRetainScalarResultsAndResidue(long x, long coefficient, long offset, long denominator)
    {
        using var run = new NativeExecution(NativeExecutionBackend.CompactSimd);
        SameRead(Pair(x, -17, coefficient, offset, denominator));
        Assert.Equal(0, run.SimdBatches);
    }

    [Fact]
    public void ReversedIntegralBasesAndRetargetedCoefficientsStayExactAndWitnessesStayFrozen()
    {
        var root = Pair(-17, 9, -6, 11, -2);
        using var run = new NativeExecution(NativeExecutionBackend.CompactSimd);
        var first = SameRead(root); var retained = Coordinates(first.Right);
        var equation = (Relation)Parts(root)[0];
        var ports = (Relation)equation.Left;
        var firstPort = (Relation)ports.Left;
        var target = (Relation)firstPort.Right;
        var end = (CarrierLocation)((Relation)target.Right).Left;
        end.Tick.Set(15); // Extent 4, source extent -2: still an exact integral map.
        SameRead(root);
        Assert.Equal(retained, Coordinates(first.Right));
        Assert.Equal(-58, ((CarrierLocation)first.Left).Ticks);
        Assert.True(run.SimdBatches > 0 || !NativeExecution.SimdSupported);
    }

    [Fact]
    public void VectorizingTheSelectedBranchNeverReadsTheOpenOrOverflowingAlternative()
    {
        var u = new Units(); var selector = L(u.Feature, 15);
        var good = SumTo(u.Feature, (L(u.Feature, 9), 3), (L(u.Feature, 1), -2));
        var bad = Product(L(u.Feature, long.MaxValue), L(u.Feature, long.MaxValue));
        var root = ExpressionGraph.Choose(u, selector, good, bad);
        using var run = new NativeExecution(NativeExecutionBackend.CompactSimd);
        var read = SameRead(root);
        Assert.Equal(25, ((CarrierLocation)read.Left).Ticks);
        Assert.Equal(0, run.Fallbacks);
        Assert.True(ReadWitness.TryRead((Relation)Parts((Relation)read.Right)[0], out var source, out _, out _, out _, out _));
        Assert.Same(root, source);
        Assert.True(run.SimdBatches > 0 || !NativeExecution.SimdSupported);
        var open = ExpressionGraph.Choose(u, selector, good, new Placeholder());
        SameRead(open);
        selector.Tick.Set(0);
        ReadMany(open);
        Assert.False(TryValue(ReadMany(open)[0], out _));
    }

    [Fact]
    public void TheOptionalBackendKeepsASeededWorldAndItsPredictionStateIdentical()
    {
        var compact = new Bath(7, 6, rocks: true, resources: MachineResources.Fixed(.9), pulsedLights: true,
            cognitiveEcology: true, ecology: EcologySettings.Roving);
        var simd = new Bath(7, 6, rocks: true, resources: MachineResources.Fixed(.9), pulsedLights: true,
            cognitiveEcology: true, ecology: EcologySettings.Roving);
        using var run = new NativeExecution(NativeExecutionBackend.CompactSimd);
        for (var tick = 0; tick < 200; tick++)
        {
            using (new NativeExecution(NativeExecutionBackend.Compact)) compact.Step();
            simd.Step();
            Assert.Equal(State(compact), State(simd));
        }
        Assert.True(run.SimdBatches > 0 || !NativeExecution.SimdSupported);
        compact.CheckConservation(); simd.CheckConservation();
    }

    private static string State(Bath bath)
    {
        static long[] HeldStates(params Carrier[] carriers) => carriers.SelectMany(c => new[] { checked((long)c.Value), c.Tension }).ToArray();
        return System.Text.Json.JsonSerializer.Serialize(new { bath.Tick, bath.TotalFood, bath.Births,
            bath.PredationDeaths, bath.StarvationDeaths, bath.SpentAttention.Value,
            bodies = bath.Creatures.Select(c => new { c.Slot, c.BornAt, x = c.X.Ticks, y = c.Y.Ticks, heading = c.Heading.Ticks,
                material = HeldStates(c.Energy, c.Eaten, c.Age, c.NextMating, c.Wallet, c.Computation.Runtime, c.Computation.Billed),
                ports = HeldStates(c.LeftEye.Pixels[0], c.RightEye.Pixels[0], c.LeftMuscle.Force, c.RightMuscle.Force),
                feedback = Parts(c.ControllerState).Cast<Relation>().Select(cell => HeldStates((Carrier)Parts(cell)[0], (Carrier)Parts(cell)[4])).ToArray(),
                memory = c.Cognition is { } b ? HeldStates(b.PreviousX, b.PreviousY, b.PreviousTime, b.PreviousEnergy,
                    b.PreviousIntake, b.Ready, b.SpeedSquared, b.Forecast, b.PersistenceForecast, b.ForecastTime, b.Pressure,
                    b.Error, b.Blocked, b.PainMemory, b.Samples, b.PredictionChecks, b.ErrorTotal) : [] }).ToArray() });
    }
}
