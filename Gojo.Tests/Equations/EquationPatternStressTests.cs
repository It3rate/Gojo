using Gojo.Core;
using Gojo.Equations;
using Gojo.Patterns;

namespace Gojo.Tests.Equations;

public sealed class EquationPatternStressTests
{
    [Fact]
    public void PatternMapCanCarryFibonacciLikeRecurrenceFrames()
    {
        var leftPort = new Placeholder();
        var rightPort = UnitShape.DescriptorSlot(0);
        var outputSlot = new Placeholder();
        var leftInput = new Placeholder();
        var rightInput = UnitShape.DescriptorSlot(1);
        var left = new Carrier();
        var right = new Carrier();
        var next = new Carrier();
        var equationPattern = new Relation(new Relation(leftPort, rightPort), outputSlot);
        var inputPattern = new Relation(leftInput, rightInput);
        var frames = new List<IReadOnlyList<Relation>>();
        var expected = new List<long>();
        var a = 1L;
        var b = 1L;

        for (var index = 0; index < 12; index++)
        {
            var sum = a + b;
            expected.Add(sum);
            frames.Add(
            [
                StructuralPattern.Bind(leftPort, Equation.UnitPort(left)),
                StructuralPattern.BindDescriptor(0, Equation.UnitPort(right)),
                StructuralPattern.Bind(outputSlot, Relation.Point(next, 1)),
                StructuralPattern.Bind(leftInput, new CarrierLocation(left, a)),
                StructuralPattern.BindDescriptor(1, new CarrierLocation(right, b))
            ]);
            a = b;
            b = sum;
        }

        EquationPatternInspection.Map(
            new Carrier(),
            equationPattern,
            inputPattern,
            frames,
            next,
            out var samples);

        Assert.Equal(expected.Count, samples.Length);
        for (var index = 0; index < samples.Length; index++)
        {
            Assert.True(EquationPatternInspection.TryReadPatternSample(
                samples[index],
                out var sampleIndex,
                out var bindings,
                out var predicted,
                out _,
                out var unbound));
            Assert.Equal(index, sampleIndex.Ticks);
            Assert.Equal(5, bindings.Length);
            Assert.Empty(unbound);
            Assert.Equal(new CarrierLocation(next, expected[index]), predicted);
        }
    }

    [Fact]
    public void PatternMapHandlesRotatingPartialFramesAcrossGeneratedStream()
    {
        var xPort = new Placeholder();
        var yPort = UnitShape.DescriptorSlot(0);
        var biasPort = new Placeholder();
        var outputSlot = new Placeholder();
        var xInput = new Placeholder();
        var yInput = UnitShape.DescriptorSlot(1);
        var biasInput = new Placeholder();
        var x = new Carrier();
        var y = new Carrier();
        var bias = new Carrier();
        var score = new Carrier();
        var equationPattern = new Relation(new Relation(new Relation(xPort, yPort), biasPort), outputSlot);
        var inputPattern = new Relation(new Relation(xInput, yInput), biasInput);
        var frames = new List<IReadOnlyList<Relation>>();
        const int frameCount = 256;

        for (var index = 0; index < frameCount; index++)
        {
            frames.Add(BuildFrame(index));
        }

        EquationPatternInspection.Map(
            new Carrier(),
            equationPattern,
            inputPattern,
            frames,
            score,
            out var samples);

        Assert.Equal(frameCount, samples.Length);
        for (var index = 0; index < frameCount; index++)
        {
            Assert.True(EquationPatternInspection.TryReadPatternSample(
                samples[index],
                out var sampleIndex,
                out var bindings,
                out var predicted,
                out var residue,
                out var unbound));
            Assert.Equal(index, sampleIndex.Ticks);

            switch (index % 4)
            {
                case 0:
                    Assert.Equal(7, bindings.Length);
                    Assert.Empty(unbound);
                    Assert.Equal(new CarrierLocation(score, ExpectedScore(index)), predicted);
                    break;
                case 1:
                    Assert.Equal(6, bindings.Length);
                    Assert.Equal([yInput], unbound);
                    Assert.Equal(new CarrierLocation(score, 0), predicted);
                    Assert.Contains(yInput, StructuralPattern.ReadSlots(residue));
                    break;
                case 2:
                    Assert.Equal(6, bindings.Length);
                    Assert.Equal([yPort], unbound);
                    Assert.Equal(new CarrierLocation(score, 0), predicted);
                    break;
                case 3:
                    Assert.Equal(6, bindings.Length);
                    Assert.Equal([outputSlot], unbound);
                    Assert.Equal(new CarrierLocation(score, 0), predicted);
                    break;
            }
        }

        Relation[] BuildFrame(int index)
        {
            var bindings = new List<Relation>
            {
                StructuralPattern.Bind(xPort, Equation.WeightedPort(x, 0, 1, 0, 2)),
                StructuralPattern.BindDescriptor(0, Equation.WeightedPort(y, 0, 1, 0, 3)),
                StructuralPattern.Bind(biasPort, Equation.WeightedPort(bias, 0, 1, 0, 5)),
                StructuralPattern.Bind(outputSlot, Relation.Point(score, 1)),
                StructuralPattern.Bind(xInput, new CarrierLocation(x, XValue(index))),
                StructuralPattern.BindDescriptor(1, new CarrierLocation(y, YValue(index))),
                StructuralPattern.Bind(biasInput, new CarrierLocation(bias, 1))
            };

            return (index % 4) switch
            {
                1 => [.. bindings.Where(binding => !Equals(binding.Left, yInput))],
                2 => [.. bindings.Where(binding => !Equals(binding.Left, yPort))],
                3 => [.. bindings.Where(binding => !Equals(binding.Left, outputSlot))],
                _ => [.. bindings]
            };
        }

        static long XValue(int index) => index % 17;

        static long YValue(int index) => (index * 3) % 19;

        static long ExpectedScore(int index) =>
            (2 * XValue(index)) + (3 * YValue(index)) + 5;
    }
}
