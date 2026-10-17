using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class FriezeRealizationTests
{
    [Fact]
    public void AllAuthoredVariantsAdmitPositiveEmbeddingsWithoutNumericSourceState()
    {
        FriezeFixture[] fixtures = [FriezeFixture.SquareWave(), FriezeFixture.SquareWave(false), FriezeFixture.Meander(),
            FriezeFixture.SlowFastDrift(), FriezeFixture.SlowFastDrift(true), FriezeFixture.PausesAndDrift(), FriezeFixture.VariableSteps()];
        foreach (var fixture in fixtures)
        {
            var trace = fixture.Run(12);
            var calibration = FriezeRealization.CreateCalibration(fixture, FriezeRealization.DefaultLengths(fixture));
            Assert.True(FriezeRealization.TryRealize(fixture, trace, calibration, out var measured));
            Assert.True(FriezeRealization.TryRead(measured, out var original, out var basis, out var points, out var pending));
            Assert.Same(trace, original); Assert.Same(calibration, basis); Assert.Empty(E(pending)); Assert.NotEmpty(E(points));
            Assert.DoesNotContain(Traverse(new Relation(fixture.Surface, trace)), element => element is Carrier or CarrierLocation);
            Assert.All(Traverse(calibration).OfType<Carrier>(), carrier => { Assert.Equal(0UL, carrier.Value); Assert.Equal(0L, carrier.Tension); });
            Assert.True(Contains(measured, trace));
        }
    }

    [Fact]
    public void SharedVerticalReachAndItsInverseGiveTheExpectedMeasuredReturn()
    {
        var fixture = FriezeFixture.SquareWave(false);
        var trace = fixture.Run(4);
        var points = Realize(fixture, trace, 12, 20);
        Assert.Equal(new[] { (0L, 20L), (12L, 20L), (12L, 0L), (24L, 0L) }, points);
        Assert.True(fixture.TryReadTrace(trace, out _, out _, out _, out var applications, out _, out _));
        var vertical = E(applications).Cast<Relation>().Select(app => (Relation)app.Right)
            .Where(move => fixture.Frame.Direction(move, out var axis, out _, out _) && ReferenceEquals(axis, fixture.Frame.Vertical)).ToArray();
        Assert.Equal(2, vertical.Length); Assert.Same(vertical[0].Right, vertical[1].Right);
        Assert.NotSame(vertical[0].Left, vertical[1].Left);
    }

    [Fact]
    public void TwoAspectRatiosRefineTheSameSamplesAndKeepSourceIdentity()
    {
        var fixture = FriezeFixture.Meander(); var trace = fixture.Run(9);
        var first = Realize(fixture, trace, 12, 20); var second = Realize(fixture, trace, 24, 10);
        Assert.Equal(first.Length, second.Length);
        Assert.All(first.Zip(second), pair =>
        { Assert.Equal(pair.First.Item1 * 2, pair.Second.Item1); Assert.Equal(pair.First.Item2, pair.Second.Item2 * 2); });
        Assert.NotEqual(first, second);
        Assert.DoesNotContain(Traverse(trace), element => element is Carrier or CarrierLocation);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonpositiveReachCalibrationIsRefusedWithItsNativeEvidence(long invalid)
    {
        var fixture = FriezeFixture.SquareWave(false); var trace = fixture.Run(4);
        var lengths = Lengths(fixture); lengths[fixture.Reaches[0]] = invalid;
        var calibration = FriezeRealization.CreateCalibration(fixture, lengths);
        Assert.False(FriezeRealization.TryRealize(fixture, trace, calibration, out var read));
        Assert.True(FriezeRealization.TryRead(read, out _, out _, out var points, out var unresolved));
        Assert.Empty(E(points)); Assert.NotEmpty(E(unresolved)); Assert.True(Contains(read, calibration));
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(20, 10)]
    public void PositiveLengthsStillHaveToRespectDeclaredStrictReachOrder(long lower, long upper)
    {
        var fixture = FriezeFixture.VariableSteps(); var trace = fixture.Run(4);
        var order = Assert.IsType<Relation>(Assert.Single(E(fixture.ReachOrder)));
        var lengths = Lengths(fixture); lengths[order.Left] = lower; lengths[order.Right] = upper;
        var calibration = FriezeRealization.CreateCalibration(fixture, lengths);
        Assert.False(FriezeRealization.TryRealize(fixture, trace, calibration, out var read));
        Assert.True(Contains(read, order));
    }

    [Fact]
    public void ForeignOrMissingReachBindingsDoNotSupplyAnImplicitZero()
    {
        var fixture = FriezeFixture.SquareWave(); var trace = fixture.Run(4);
        var missing = Lengths(fixture); missing.Remove(fixture.Reaches[0]);
        var extra = Lengths(fixture); var foreign = new Placeholder(); extra.Add(foreign, 12);
        foreach (var lengths in new[] { missing, extra })
        {
            var calibration = FriezeRealization.CreateCalibration(fixture, lengths);
            Assert.False(FriezeRealization.TryRealize(fixture, trace, calibration, out var read));
            Assert.True(FriezeRealization.TryRead(read, out _, out _, out var points, out var unresolved));
            Assert.Empty(E(points)); Assert.NotEmpty(E(unresolved));
        }
    }

    [Fact]
    public void LookalikeAxisAndForeignNumericBasisCannotReplaceTheSelectedCalibration()
    {
        var fixture = FriezeFixture.SquareWave(false); var trace = fixture.Run(4);
        var calibration = FriezeRealization.CreateCalibration(fixture, Lengths(fixture));
        var rows = E((Relation)calibration.Right).Cast<Relation>().ToArray();
        var role = (Relation)rows[0].Left; var axis = (Relation)role.Left;
        var lookalike = new Relation(axis.Left, axis.Right);
        var wrongAxis = new Relation(new Relation(lookalike, role.Right), rows[0].Right);
        var wrongBasis = new Relation(rows[0].Left, Relation.Segment(new Carrier(), 0, 12));
        foreach (var replacement in new[] { wrongAxis, wrongBasis })
        {
            var changed = new Relation(calibration.Left, S(replacement, rows[1]));
            Assert.False(FriezeRealization.TryRealize(fixture, trace, changed, out var read));
            Assert.True(Contains(read, replacement));
        }
    }

    [Fact]
    public void DifferentCausalNetworksCanHaveTheSameDrawnPathWithoutLosingEitherHistory()
    {
        var frame = new Gojo.Experiments.QualitativeExecution.QualitativeMotionFrame();
        var h = new Placeholder(); var v = new Placeholder();
        var coupled = FriezeFixture.SquareWave(true, frame, h, v); var direct = FriezeFixture.SquareWave(false, frame, h, v);
        var a = coupled.Run(8); var b = direct.Run(8);
        Assert.Equal(Realize(coupled, a, 12, 20), Realize(direct, b, 12, 20));
        Assert.NotSame(coupled.Network.Surface, direct.Network.Surface);
        Assert.NotEqual(coupled.Components.Count, direct.Components.Count);
        Assert.NotSame(a, b);
        Assert.True(Contains(a, coupled.Network.Surface)); Assert.True(Contains(b, direct.Network.Surface));
    }

    [Fact]
    public void SamplingEachFiringChangesCommittedPathWithoutLosingTheUnderlyingMotionCount()
    {
        var grouped = FriezeFixture.SlowFastDrift();
        var detailed = grouped.WithSampling(S(E(grouped.Network.Ports).Where(port => !ReferenceEquals(port, grouped.RootPort)).ToArray()));
        Assert.Same(grouped.Network, detailed.Network); Assert.Same(grouped.OutputLaw, detailed.OutputLaw);
        var a = grouped.Run(12); var b = detailed.Run(12);
        var first = Realize(grouped, a, 12, 20); var second = Realize(detailed, b, 12, 20);
        Assert.True(grouped.TryReadTrace(a, out _, out _, out _, out var aMoves, out _, out _));
        Assert.True(detailed.TryReadTrace(b, out _, out _, out _, out var bMoves, out _, out _));
        Assert.Equal(E(aMoves).Length, E(bMoves).Length);
        Assert.True(second.Length > first.Length); Assert.Equal(first[^1], second[^1]);
    }

    [Fact]
    public void MissingMotionOutputKeepsPositionOpenAndCannotBeRenderedAsZero()
    {
        var fixture = FriezeFixture.SquareWave(false);
        Assert.True(FiniteLaw.TryRead(fixture.OutputLaw, out var phases, out var ports, out var words, out _));
        var missing = fixture.WithOutputs(FiniteLaw.Create(phases, ports, words));
        var trace = missing.Run(2);
        var calibration = FriezeRealization.CreateCalibration(missing, FriezeRealization.DefaultLengths(missing));
        Assert.False(FriezeRealization.TryRealize(missing, trace, calibration, out var read));
        Assert.True(FriezeRealization.TryRead(read, out _, out _, out var points, out var pending));
        Assert.Empty(E(points)); Assert.NotEmpty(E(pending)); Assert.True(Contains(read, trace));
    }

    private static (long, long)[] Realize(FriezeFixture fixture, Relation trace, long horizontal, long vertical)
    {
        var calibration = FriezeRealization.CreateCalibration(fixture, FriezeRealization.DefaultLengths(fixture, horizontal, vertical));
        Assert.True(FriezeRealization.TryRealize(fixture, trace, calibration, out var read));
        Assert.True(FriezeRealization.TryRead(read, out _, out _, out var points, out var pending));
        Assert.Empty(E(pending));
        return E(points).Cast<Relation>().Select(clause =>
        {
            Assert.True(FriezeRealization.TryReadSamplePoints(clause, out _, out _, out var after));
            Assert.True(FriezeRealization.TryReadPoint(after, out var x, out var y)); return (x, y);
        }).ToArray();
    }
    private static Dictionary<IElement, long> Lengths(FriezeFixture fixture) =>
        new(FriezeRealization.DefaultLengths(fixture), ReferenceEqualityComparer.Instance);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Contains(IElement root, IElement target) => Traverse(root).Any(value => ReferenceEquals(value, target));
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var value))
        {
            if (!seen.Add(value)) continue; yield return value;
            if (value is Relation relation) { stack.Push(relation.Right); stack.Push(relation.Left); }
        }
    }
}
