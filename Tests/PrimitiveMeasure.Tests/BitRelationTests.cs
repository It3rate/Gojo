using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class BitRelationTests
{
    [Fact]
    public void OrderedTuplesOccupyExactBitsWithoutAHeader()
    {
        var relation = BitRelation.FromTuples(3, [[0, 0, 0], [1, 2, 3], [7, 7, 7]]);
        var expected = new byte[64];
        expected[0] = 1;
        expected[26] = 2; // Tuple (1,2,3): bit 209.
        expected[63] = 128;

        Assert.Equal(3, relation.Arity);
        Assert.Equal(expected, relation.PackedBytes());
        Assert.True(relation.Allows(1, 2, 3));
        Assert.False(relation.Allows(3, 2, 1));
        Assert.False(relation.Allows(1, 2, 2));
    }

    [Fact]
    public void OneGenericProjectionReadsDifferentExplicitRelations()
    {
        var first = BitRelation.FromTuples(3, [[2, 6, 1], [2, 6, 5], [6, 2, 7]]);
        var second = BitRelation.FromTuples(3, [[2, 6, 0], [6, 2, 3]]);

        Assert.Equal((byte)0b00100010, first.Predict([0b00000100, 0b01000000]));
        Assert.Equal((byte)0b10000000, first.Predict([0b01000000, 0b00000100]));
        Assert.Equal((byte)0b10100010, first.Predict([0b01000100, 0b01000100]));
        Assert.Equal((byte)0b00000001, second.Predict([0b00000100, 0b01000000]));
        Assert.Equal((byte)0b00001000, second.Predict([0b01000000, 0b00000100]));
    }

    [Fact]
    public void EveryTupleCanBePresentOrAbsentAtEachSupportedArity()
    {
        for (var arity = 1; arity <= 3; arity++)
        {
            var tupleCount = 1 << (3 * arity);
            var full = new BitRelation(arity, Enumerable.Repeat((byte)255, tupleCount / 8).ToArray());
            var empty = new BitRelation(arity, new byte[tupleCount / 8]);
            for (var index = 0; index < tupleCount; index++)
            {
                var tuple = Enumerable.Range(0, arity).Select(role => (byte)((index >> (3 * role)) & 7)).ToArray();
                Assert.True(full.Allows(tuple));
                Assert.False(empty.Allows(tuple));
            }
            var unconstrainedInputs = Enumerable.Repeat((byte)255, arity - 1).ToArray();
            Assert.Equal(byte.MaxValue, full.Predict(unconstrainedInputs));
            Assert.Equal((byte)0, empty.Predict(unconstrainedInputs));
        }
    }

    [Fact]
    public void MissingInputOrForbiddenTupleIsDifferentFromAnAdmittedZeroWord()
    {
        var relation = BitRelation.FromTuples(3, [[1, 3, 0], [1, 3, 2]]);

        Assert.Equal((byte)0b00000101, relation.Predict([0b00000010, 0b00001000]));
        Assert.Equal((byte)0, relation.Predict([0, 0b00001000]));
        Assert.Equal((byte)0, relation.Predict([0b00000010, 0]));
        Assert.Equal((byte)0, relation.Predict([0b00000100, 0b00001000]));
        var onlyZero = BitRelation.FromTuples(1, [[0]]);
        Assert.Equal((byte)1, onlyZero.Predict([]));
    }

    [Fact]
    public void SharedReferencesConstrainWordsButEqualMasksDoNotEstablishAliases()
    {
        var unequal = BitRelation.FromTuples(3, [[0, 0, 0], [0, 1, 1], [1, 0, 1], [1, 1, 0]]);

        Assert.Equal((byte)3, unequal.Predict([3, 3]));
        Assert.Equal((byte)3, unequal.Predict([3, 3], [4, 5]));
        Assert.Equal((byte)1, unequal.Predict([3, 3], [4, 4]));
        Assert.Equal((byte)1, unequal.Predict([3, 2], [4, 4]));
        // One shared input cannot simultaneously choose different words.
        Assert.Equal((byte)0, unequal.Predict([1, 2], [4, 4]));
        Assert.Equal((byte)2, unequal.Predict([1, 2], [4, 5]));
    }

    [Fact]
    public void ReferenceConstraintsValidateTheirLayoutIncludingEmptyUnaryInputs()
    {
        var unary = BitRelation.FromTuples(1, [[0], [7]]);
        Assert.Equal((byte)129, unary.Predict([], []));
        Assert.Equal(unary.Predict([]), unary.Predict([], []));
        Assert.Throws<ArgumentException>(() => unary.Predict([], [0]));

        var binary = BitRelation.FromTuples(2, [[0, 0]]);
        Assert.Throws<ArgumentException>(() => binary.Predict([1], []));
        Assert.Throws<ArgumentException>(() => binary.Predict([1], [0, 1]));
        Assert.Throws<ArgumentOutOfRangeException>(() => binary.Predict([1], [-1]));
        // An unavailable observation does not bypass validation of the layout.
        Assert.Throws<ArgumentOutOfRangeException>(() => binary.Predict([0], [-1]));
    }

    [Fact]
    public void CoarsePresenceRetainsAllFinePreimagesWithoutInventingASelection()
    {
        // Same fixed positive context: two distinguishable occupied parts and
        // their coarse presence. These are participation bits, not calibrated units.
        var relation = BitRelation.FromTuples(3, [[0, 0, 0], [0, 1, 1], [1, 0, 1], [1, 1, 1]]);
        var admittedFine = new List<(byte First, byte Second)>();
        for (byte first = 0; first <= 1; first++)
            for (byte second = 0; second <= 1; second++)
                if (relation.Allows(first, second, 1)) admittedFine.Add((first, second));

        Assert.Equal(new (byte, byte)[] { (0, 1), (1, 0), (1, 1) }, admittedFine);
        Assert.True(relation.Allows(0, 0, 0));
        Assert.False(relation.Allows(1, 0, 0));
        Assert.False(relation.Allows(0, 1, 0));
        Assert.False(relation.Allows(1, 1, 0));
        Assert.Equal((byte)0b00000011, relation.Predict([0b00000011, 0b00000011]));
    }

    [Fact]
    public void ConstructorAndDiagnosticsDoNotLeaveWritableAliases()
    {
        byte[] bitmap = [0b00000101];
        var relation = new BitRelation(1, bitmap);
        bitmap[0] = 0;
        var copy = relation.PackedBytes();
        copy[0] = 255;

        Assert.Equal(new byte[] { 5 }, relation.PackedBytes());
        Assert.Equal((byte)5, relation.Predict([]));
        var tuple = new byte[] { 2, 7 };
        var fromTuple = BitRelation.FromTuples(2, [tuple, tuple]);
        tuple[0] = 0;
        Assert.True(fromTuple.Allows(2, 7));
        Assert.False(fromTuple.Allows(0, 7));
    }

    [Fact]
    public void MalformedLayoutsAndWordsAreRejectedWithoutChangingExistingRelation()
    {
        var relation = BitRelation.FromTuples(2, [[1, 7]]);
        var before = relation.PackedBytes();
        Assert.Throws<ArgumentOutOfRangeException>(() => new BitRelation(0, []));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BitRelation(4, []));
        Assert.Throws<ArgumentException>(() => new BitRelation(1, []));
        Assert.Throws<ArgumentException>(() => new BitRelation(2, new byte[9]));
        Assert.Throws<ArgumentException>(() => relation.Allows(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => relation.Allows(8, 7));
        Assert.Throws<ArgumentException>(() => relation.Predict([]));
        Assert.Throws<ArgumentException>(() => relation.Predict([1, 1]));
        Assert.Throws<ArgumentException>(() => BitRelation.FromTuples(2, [[1, 7], [1]]));
        Assert.Throws<ArgumentOutOfRangeException>(() => BitRelation.FromTuples(2, [[1, 7], [8, 7]]));
        Assert.Equal(before, relation.PackedBytes());
    }
}
