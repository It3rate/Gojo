using System.Numerics;
using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class SaturationTests
{
    [Fact]
    public void OverflowAllStatesConserveContentsAndUseForwardBeforeSide()
    {
        byte[] expected = [0, 2, 2, 6, 4, 6, 6, 7];
        for (byte before = 0; before < 8; before++)
        {
            var circuit = SaturationExamples.Overflow(before);
            Assert.Equal(1u << expected[before], circuit.Predict(circuit.Observe()));
            Assert.True(circuit.TryStep(circuit.Observe(), out var after));
            Assert.Equal(expected[before], after);
            Assert.Equal(after, circuit.Stored);
            Assert.Equal(BitOperations.PopCount((uint)before), BitOperations.PopCount((uint)after));
        }
    }

    [Fact]
    public void OverflowMeaningIsInTheTwoRowsAndExistingLaws()
    {
        var circuit = SaturationExamples.Overflow(3);
        Assert.Equal(new byte[] { 0x18 }, circuit.Forward.PackedRows(2));
        Assert.Equal(new byte[] { 0x24 }, circuit.Present.PackedRows(2));
        Assert.Equal(PassageLaw.Unconditional().PackedBytes(), circuit.Forward.LawBytes());
        Assert.Equal(PassageLaw.Controlled().PackedBytes(), circuit.Present.LawBytes());
        Assert.Equal(3, circuit.Owner.LiveCount(0, 0, 1));
    }

    [Fact]
    public void EveryUncertainOverflowPredictionIsExactlyTheUnionOfItsResolvedCases()
    {
        byte[] outcomes = [0, 2, 2, 6, 4, 6, 6, 7];
        var circuit = SaturationExamples.Overflow(3);
        for (uint support = 0; support < 256; support++)
        {
            uint expected = 0;
            for (var before = 0; before < 8; before++)
                if ((support & (1u << before)) != 0) expected |= 1u << outcomes[before];
            Assert.Equal(expected, circuit.Predict(support));
            if (BitOperations.PopCount(support) != 1)
            {
                Assert.False(circuit.TryStep(support, out _));
                Assert.Equal(3, circuit.Stored);
            }
        }
    }

    [Fact]
    public void UnknownForwardOccupancyPreservesLinkedAlternativesRatherThanDuplicatingSource()
    {
        var circuit = SaturationExamples.Overflow(3);
        Assert.Equal(0x0Au, circuit.Observe(5));
        Assert.Equal(0x44u, circuit.Predict(circuit.Observe(5)));
        Assert.False(circuit.TryStep(circuit.Observe(5), out _));
        Assert.Equal(3, circuit.Stored);
        Assert.True(circuit.TryStep(circuit.Observe(), out var after));
        Assert.Equal(6, after);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryForkStateChoosesFirstAvailableOutletWithoutCopying(bool rightFirst)
    {
        for (byte before = 0; before < 16; before++)
        {
            var circuit = SaturationExamples.Fork(before, rightFirst);
            var expected = before;
            int[] priority = rightFirst ? [3, 2] : [2, 3];
            if ((before & 1) != 0)
                foreach (var target in priority)
                    if ((before & (1 << target)) == 0)
                    {
                        expected = (byte)((before & ~1) | (1 << target));
                        break;
                    }
            Assert.True(circuit.TryStep(circuit.Observe(), out var after));
            Assert.Equal(expected, after);
            Assert.Equal(before & 2, after & 2);
            Assert.Equal(BitOperations.PopCount((uint)before), BitOperations.PopCount((uint)after));
        }
    }

    [Fact]
    public void ChangingOrderChangesRouteSoPriorityCannotBePresentedAsSymmetry()
    {
        var left = SaturationExamples.Fork(1, false);
        var right = SaturationExamples.Fork(1, true);
        Assert.True(left.TryStep(left.Observe(), out var l));
        Assert.True(right.TryStep(right.Observe(), out var r));
        Assert.Equal(4, l);
        Assert.Equal(8, r);
        Assert.NotEqual(left.Forward.PackedRows(2), right.Forward.PackedRows(2));
    }

    [Fact]
    public void NoEscapeRetainsPendingContentsAndCreatesNoNewRoute()
    {
        var circuit = new PassageFixture(3, 3);
        circuit.Add(circuit.Forward, 0, 2, 1);
        var rows = circuit.Forward.PackedRows(2);
        for (var attempt = 0; attempt < 10; attempt++)
        {
            Assert.True(circuit.TryStep(circuit.Observe(), out var after));
            Assert.Equal(3, after);
        }
        Assert.Equal(rows, circuit.Forward.PackedRows(2));
        Assert.Empty(circuit.Present.PackedRows(2));
        // An explicit structural attachment enables escape; repeated failed
        // attempts did not silently create it or accumulate unrepresented input.
        circuit.Add(circuit.Present, 0, 1, 2);
        Assert.True(circuit.TryStep(circuit.Observe(), out var released));
        Assert.Equal(6, released);
    }

    [Fact]
    public void AThresholdReadCanAllowOvershootRatherThanImplySidewaysFlow()
    {
        var circuit = new PassageFixture(3, 3);
        circuit.Add(circuit.Present, 0, 1, 2);
        Assert.True(circuit.TryStep(circuit.Observe(), out var after));
        Assert.Equal(6, after);
        // This same encoded passage could be drawn straight or bent. Its
        // references alone establish no angle or length.
        Assert.Equal(new byte[] { 0x24 }, circuit.Present.PackedRows(2));
    }
}
