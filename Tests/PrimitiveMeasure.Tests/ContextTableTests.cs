using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class ContextTableTests
{
    [Fact]
    public void FixedContextIsStoredOnceAndInstancesNeedOneBit()
    {
        // Low-to-high positions: O=0,D=1,V0=1,V1=0,V2=1 -> 00010110.
        var packed = ContextTable.Pack(1, 0b010, [0b011, 0b010, 0b011]);
        Assert.Equal(new byte[] { 0x16 }, packed);
        Assert.Equal(5, ContextTable.BitLength(1, 3));
        Assert.Equal(new byte[] { 3, 2, 3 }, ReadAll(packed, 1, 3));
        Assert.Equal(0, ContextTable.BitPosition(1, 3, 0, 2));
        Assert.Equal(0, ContextTable.BitPosition(1, 3, 2, 2));
        Assert.Equal(1, ContextTable.BitPosition(1, 3, 0, 1));
        Assert.Equal(1, ContextTable.BitPosition(1, 3, 2, 1));
        Assert.Equal(2, ContextTable.BitPosition(1, 3, 0, 0));
        Assert.Equal(4, ContextTable.BitPosition(1, 3, 2, 0));
    }

    [Fact]
    public void SignedAndFreeLayoutsHaveExactDistinctBytes()
    {
        // O=1 | (V,D)=(1,0),(0,1),(1,1) -> 01110011.
        var signed = ContextTable.Pack(2, 0b100, [0b101, 0b110, 0b111]);
        Assert.Equal(new byte[] { 0x73 }, signed);
        Assert.Equal(7, ContextTable.BitLength(2, 3));
        Assert.Equal(new byte[] { 5, 6, 7 }, ReadAll(signed, 2, 3));
        Assert.Equal(0, ContextTable.BitPosition(2, 3, 2, 2));
        Assert.Equal(6, ContextTable.BitPosition(2, 3, 2, 1));

        byte[] words = [0, 1, 2, 3, 4, 5, 6, 7];
        var free = ContextTable.Pack(3, 0, words);
        Assert.Equal(new byte[] { 0x88, 0xC6, 0xFA }, free);
        Assert.Equal(24, ContextTable.BitLength(3, 8));
        Assert.Equal(words, ReadAll(free, 3, 8));
        Assert.Equal(23, ContextTable.BitPosition(3, 8, 7, 2));
    }

    [Fact]
    public void InstanceUpdatesCannotFlipInheritedBitsEvenAtZero()
    {
        var fixedNegative = ContextTable.Pack(1, 0b010, [0b011, 0b010]);
        var before = fixedNegative.ToArray();
        Assert.False(ContextTable.TryWrite(fixedNegative, 1, 2, 0, 0b001));
        Assert.False(ContextTable.TryWrite(fixedNegative, 1, 2, 1, 0b000));
        Assert.False(ContextTable.TryWrite(fixedNegative, 1, 2, 1, 0b110));
        Assert.Equal(before, fixedNegative);
        Assert.True(ContextTable.TryWrite(fixedNegative, 1, 2, 0, 0b010));
        Assert.Equal(new byte[] { 2, 2 }, ReadAll(fixedNegative, 1, 2));

        var signed = ContextTable.Pack(2, 0, [0b001]);
        Assert.True(ContextTable.TryWrite(signed, 2, 1, 0, 0b011));
        Assert.False(ContextTable.TryWrite(signed, 2, 1, 0, 0b111));
        Assert.Equal((byte)3, ContextTable.Read(signed, 2, 1, 0));
    }

    [Fact]
    public void ReleasingSignPreservesWordsButMakesDirectionIndependentlyWritable()
    {
        var constrained = ContextTable.Pack(1, 0b010, [0b011, 0b010, 0b011]);
        var released = ContextTable.Release(constrained, 1, 3, 2);
        // O=0 | (V,D)=(1,1),(0,1),(1,1) -> 01110110.
        Assert.Equal(new byte[] { 0x76 }, released);
        Assert.Equal(ReadAll(constrained, 1, 3), ReadAll(released, 2, 3));
        Assert.Equal(new byte[] { 0x16 }, constrained);
        Assert.Equal(1, ContextTable.BitPosition(1, 3, 2, 1));
        Assert.Equal(6, ContextTable.BitPosition(2, 3, 2, 1));
        Assert.True(ContextTable.TryWrite(released, 2, 3, 0, 0b001));
        Assert.Equal(new byte[] { 1, 2, 3 }, ReadAll(released, 2, 3));
        Assert.False(ContextTable.TryWrite(released, 2, 3, 0, 0b101));
        Assert.Throws<ArgumentException>(() => ContextTable.Release(released, 2, 3, 1));
    }

    [Fact]
    public void SharedContextAgreementDoesNotChooseAnArithmeticMergeOrReorientation()
    {
        var sameSign = ContextTable.Pack(1, 0, [1, 1]);
        Assert.Equal(new byte[] { 1, 1 }, ReadAll(sameSign, 1, 2));
        Assert.Equal(new byte[] { 1 }, ThreeBitMeasure.Add(1, 1));
        Assert.Throws<ArgumentException>(() => ContextTable.Pack(1, 0, [1, 3]));
        var opposed = ContextTable.Pack(2, 0, [1, 3]);
        Assert.Equal(new byte[] { 1, 3 }, ReadAll(opposed, 2, 2));
        Assert.Equal(new byte[] { 0, 1, 2, 3 }, ThreeBitMeasure.Add(1, 3));

        Assert.Throws<ArgumentException>(() => ContextTable.Pack(2, 0, [1, 5]));
        var independentOrientations = ContextTable.Pack(3, 0, [1, 5]);
        Assert.Empty(ThreeBitMeasure.Add(ContextTable.Read(independentOrientations, 3, 2, 0),
            ContextTable.Read(independentOrientations, 3, 2, 1)));
    }

    [Fact]
    public void EveryContextAndPayloadWidthSurvivesByteBoundariesAndRelease()
    {
        for (var width = 1; width <= 3; width++)
            for (byte context = 0; context < 8; context++)
            {
                var mask = (1 << width) - 1;
                if ((context & mask) != 0) continue;
                for (var count = 0; count <= 17; count++)
                {
                    var words = Enumerable.Range(0, count).Select(i => (byte)(context | ((i * 5) & mask))).ToArray();
                    var packed = ContextTable.Pack(width, context, words);
                    Assert.Equal(words, ReadAll(packed, width, count));
                    for (var wider = width; wider <= 3; wider++)
                    {
                        var released = ContextTable.Release(packed, width, count, wider);
                        Assert.Equal(words, ReadAll(released, wider, count));
                        Assert.Equal((ContextTable.BitLength(wider, count) + 7) / 8, released.Length);
                    }
                }
            }
    }

    [Fact]
    public void PaddingIsNotAnotherEntryAndMalformedRequestsCannotPartiallyWrite()
    {
        var packed = ContextTable.Pack(1, 0, [1]);
        var before = packed.ToArray();
        Assert.Throws<ArgumentOutOfRangeException>(() => ContextTable.Read(packed, 1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ContextTable.TryWrite(packed, 1, 1, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ContextTable.TryWrite(packed, 1, 1, 0, 8));
        Assert.Throws<ArgumentException>(() => ContextTable.Read([], 1, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ContextTable.BitLength(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ContextTable.BitLength(2, -1));
        Assert.Throws<ArgumentException>(() => ContextTable.Pack(2, 0b010, [0b010]));
        Assert.Equal(before, packed);
    }

    private static byte[] ReadAll(byte[] packed, int width, int count) =>
        Enumerable.Range(0, count).Select(index => ContextTable.Read(packed, width, count, index)).ToArray();
}
