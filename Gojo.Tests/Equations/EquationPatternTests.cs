using Gojo.Core;
using Gojo.Equations;
using Gojo.Patterns;

namespace Gojo.Tests.Equations;

public sealed class EquationPatternTests
{
    [Fact]
    public void CompleteEquationPatternPredictsThroughBoundSlots()
    {
        var xPort = new Placeholder();
        var yPort = UnitShape.DescriptorSlot(0);
        var outputSlot = new Placeholder();
        var xInput = new Placeholder();
        var yInput = UnitShape.DescriptorSlot(1);
        var x = new Carrier();
        var y = new Carrier();
        var result = new Carrier();
        var equationPattern = new Relation(new Relation(xPort, yPort), outputSlot);
        var inputPattern = new Relation(xInput, yInput);
        x.Set(3);
        y.Set(4);

        var complete = EquationPattern.TryPredict(
            equationPattern,
            inputPattern,
            result,
            out var prediction,
            out var unbound,
            StructuralPattern.Bind(xPort, Equation.UnitPort(x)),
            StructuralPattern.BindDescriptor(0, Equation.UnitPort(y)),
            StructuralPattern.Bind(outputSlot, Relation.Point(result, 1)),
            StructuralPattern.Bind(xInput, x.ValueSlot),
            StructuralPattern.BindDescriptor(1, y.ValueSlot));

        Assert.True(complete);
        Assert.Empty(unbound);
        Assert.True(Equation.TryReadPrediction(prediction, out var predicted, out _));
        Assert.Equal(new CarrierLocation(result, 7), predicted);
    }

    [Fact]
    public void PartialEquationPatternReportsUnboundInputSlotAsResidue()
    {
        var xPort = new Placeholder();
        var yPort = UnitShape.DescriptorSlot(0);
        var outputSlot = new Placeholder();
        var xInput = new Placeholder();
        var yInput = UnitShape.DescriptorSlot(1);
        var x = new Carrier();
        var y = new Carrier();
        var result = new Carrier();
        var equationPattern = new Relation(new Relation(xPort, yPort), outputSlot);
        var inputPattern = new Relation(xInput, yInput);
        x.Set(3);
        y.Set(4);

        var complete = EquationPattern.TryPredict(
            equationPattern,
            inputPattern,
            result,
            out var prediction,
            out var unbound,
            StructuralPattern.Bind(xPort, Equation.UnitPort(x)),
            StructuralPattern.BindDescriptor(0, Equation.UnitPort(y)),
            StructuralPattern.Bind(outputSlot, Relation.Point(result, 1)),
            StructuralPattern.Bind(xInput, x.ValueSlot));

        Assert.False(complete);
        Assert.Equal([yInput], unbound);
        Assert.True(Equation.TryReadPrediction(prediction, out var predicted, out var residue));
        Assert.Equal(new CarrierLocation(result, 0), predicted);
        Assert.Contains(yInput, StructuralPattern.ReadSlots(residue));
    }

    [Fact]
    public void PatternMapRecordsMixedCompleteAndPartialBindingFrames()
    {
        var xPort = new Placeholder();
        var yPort = UnitShape.DescriptorSlot(0);
        var outputSlot = new Placeholder();
        var xInput = new Placeholder();
        var yInput = UnitShape.DescriptorSlot(1);
        var x = new Carrier();
        var y = new Carrier();
        var result = new Carrier();
        var equationPattern = new Relation(new Relation(xPort, yPort), outputSlot);
        var inputPattern = new Relation(xInput, yInput);
        x.Set(3);
        y.Set(4);
        IReadOnlyList<IReadOnlyList<Relation>> frames =
        [
            [
                StructuralPattern.Bind(xPort, Equation.UnitPort(x)),
                StructuralPattern.BindDescriptor(0, Equation.UnitPort(y)),
                StructuralPattern.Bind(outputSlot, Relation.Point(result, 1)),
                StructuralPattern.Bind(xInput, x.ValueSlot),
                StructuralPattern.BindDescriptor(1, y.ValueSlot)
            ],
            [
                StructuralPattern.Bind(xPort, Equation.UnitPort(x)),
                StructuralPattern.BindDescriptor(0, Equation.UnitPort(y)),
                StructuralPattern.Bind(outputSlot, Relation.Point(result, 1)),
                StructuralPattern.Bind(xInput, x.ValueSlot)
            ]
        ];

        var map = EquationPatternInspection.Map(
            new Carrier(),
            equationPattern,
            inputPattern,
            frames,
            result,
            out var samples);

        Assert.True(EquationPatternInspection.TryReadMapSamples(map, out var readSamples));
        Assert.Equal(samples, readSamples);

        Assert.True(EquationPatternInspection.TryReadPatternSample(
            samples[0],
            out var firstIndex,
            out var firstBindings,
            out var firstPredicted,
            out _,
            out var firstUnbound));
        Assert.Equal(0, firstIndex.Ticks);
        Assert.Equal(new CarrierLocation(result, 7), firstPredicted);
        Assert.Equal(5, firstBindings.Length);
        Assert.Empty(firstUnbound);

        Assert.True(EquationPatternInspection.TryReadPatternSample(
            samples[1],
            out var secondIndex,
            out var secondBindings,
            out var secondPredicted,
            out var secondResidue,
            out var secondUnbound));
        Assert.Equal(1, secondIndex.Ticks);
        Assert.Equal(new CarrierLocation(result, 0), secondPredicted);
        Assert.Equal(4, secondBindings.Length);
        Assert.Equal([yInput], secondUnbound);
        Assert.Contains(yInput, StructuralPattern.ReadSlots(secondResidue));
    }
}
