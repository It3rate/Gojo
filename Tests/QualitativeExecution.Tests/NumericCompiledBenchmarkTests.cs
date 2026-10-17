using Gojo.Core;
using Gojo.Equations;
using Gojo.Experiments.RefinementPerformance;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class NumericCompiledBenchmarkTests
{
    [Theory]
    [InlineData(2, 1)]
    [InlineData(3, 7)]
    [InlineData(17, 71)]
    public void LoweredNumericExecutionAndReconstructedEvidenceAgreeWithNative(int size, int rounds)
    {
        var native = new NumericWorkload(size);
        var compiled = new NumericWorkload(size, compiled: true);

        Assert.Equal(native.Run(rounds), compiled.Run(rounds));
        Assert.Null(compiled.LatestWitness);
        Assert.NotNull(compiled.Snapshot());
        Assert.NotNull(compiled.LatestWitness);
        var nativeReads = Reads(native.LatestWitness!);
        var compiledReads = Reads(compiled.LatestWitness!);
        for (var index = 0; index < 5; index++)
        {
            Assert.True(Equation.TryReadPrediction((Relation)nativeReads[index], out var a, out _));
            Assert.True(Equation.TryReadPrediction((Relation)compiledReads[index], out var b, out _));
            Assert.Equal(a.Ticks, b.Ticks);
        }
        Assert.True(SupportDecisionSurface.TryReadSelection((Relation)nativeReads[7], out _, out var nativeCommand, out _, out _));
        Assert.True(SupportDecisionSurface.TryReadSelection((Relation)compiledReads[7], out _, out var compiledCommand, out _, out _));
        Assert.Equal(((CarrierLocation)nativeCommand).Ticks, ((CarrierLocation)compiledCommand).Ticks);
    }

    [Theory]
    [InlineData(0, 3, 0, 2, 2)]
    [InlineData(3, 0, -2, 2, 1)]
    [InlineData(-3, 4, 5, -2, -2)]
    public void DerivedAffineReadMatchesSignedTruncationAndLeavesNativeResidueRecoverable(
        long sourceStart, long sourceEnd, long targetStart, long targetEnd, long sample)
    {
        var input = new Carrier(); var output = new Carrier();
        var port = Equation.WeightedPort(input, sourceStart, sourceEnd, targetStart, targetEnd);
        var equation = Equation.Sum(output, port);
        var plan = Assert.IsType<NumericEquationPlan>(NumericEquationPlan.Capture(equation, output));
        var retained = new CarrierLocation(input, sample);

        var ticks = plan.Evaluate([sample]);
        var native = Equation.Predict(equation, retained, output);
        Assert.True(Equation.TryReadPrediction(native, out var value, out var residue));
        Assert.Equal(value.Ticks, ticks);
        Assert.True(ProjectionResidue.TryRead(residue, out var residueAxis, out var numerator, out var denominator));
        Assert.Same(input, residueAxis);
        Assert.Equal((decimal)sample - sourceStart,
            (decimal)(ticks - targetStart) * (sourceEnd - sourceStart) / (targetEnd - targetStart) + (decimal)numerator / denominator);
        if (sourceStart == 0 && sourceEnd == 3) { Assert.Equal(1, numerator); Assert.Equal(2, denominator); }
    }

    [Fact]
    public void ZeroResultDoesNotHideAnEditedCoordinateAndOpposedAxesStayNative()
    {
        var input = new Carrier(); var output = new Carrier();
        var port = Equation.WeightedPort(input, 0, 3, 0, 2);
        var equation = Equation.Sum(output, port);
        var plan = Assert.IsType<NumericEquationPlan>(NumericEquationPlan.Capture(equation, output));
        Assert.Equal(0, plan.Evaluate([0]));
        Assert.True(plan.IsCurrent);
        var sourceFrame = Assert.IsType<Relation>(port.Left);
        var origin = Assert.IsType<Relation>(sourceFrame.Left);
        var location = Assert.IsType<CarrierLocation>(origin.Left);
        location.Tick.Set(1);
        Assert.False(plan.IsCurrent);
        Assert.Null(NumericEquationPlan.Capture(Equation.Product(output,
            Equation.UnitPort(input.Inverse()), Equation.UnitPort(input)), output));
    }

    [Fact]
    public void ConditionalPlanRetainsTheSelectedNativeRuleAndRefusesDuplicateBindings()
    {
        var axis = new Carrier(); var role = new Placeholder(); var root = new Carrier();
        var response = new CarrierLocation(axis, 7);
        var rule = SupportDecisionSurface.Rule(root, response,
            SupportDecisionSurface.Condition(root, role, Relation.Segment(axis, -2, 2)));
        var policy = SupportDecisionSurface.Create(root, rule);
        var plan = Assert.IsType<NumericIntervalPolicyPlan>(NumericIntervalPolicyPlan.Capture(policy));
        Assert.True(plan.TrySelect(role, axis, 0, null, null, 0, out var selected, out var selectedRule));
        Assert.Equal(response, selected); Assert.Same(rule, selectedRule);
        Assert.False(plan.TrySelect(role, axis, 0, role, axis, 0, out _, out _));
        response.Tick.Set(8);
        Assert.False(plan.IsCurrent);
    }

    private static IElement[] Reads(Relation witness)
    {
        var envelope = Assert.IsType<Relation>(witness.Right);
        var step = Assert.IsType<Relation>(envelope.Right);
        Assert.True(StructuralConstraint.TryReadScope((Relation)step.Right, out var entries, out _, out _));
        Assert.Equal(8, entries.Length);
        return entries;
    }
}
