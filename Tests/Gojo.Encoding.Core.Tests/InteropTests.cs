using Gojo.Core;
using Gojo.Encoding.Core.Native;
using Gojo.Encoding.Core.Packed;
using Gojo.Patterns;

namespace Gojo.Encoding.Core.Tests;

/// <summary>A bounded adapter fixture, not a conversion between all native and
/// packed structures. Codes 0/1 select two declared truth participants; they do
/// not assign measured magnitudes, selection, or material inventory.</summary>
public sealed class InteropTests
{
    [Fact]
    public void AllBooleanTablesAgreeAcrossAllClosedIndependentSupportPairs()
    {
        var comparisons = 0;
        for (var truthTable = 0; truthTable < 16; truthTable++)
        {
            var fixture = Build(truthTable);
            var nativeBefore = GojoNumericFormat.Encode(fixture.Native, fixture.States[0], fixture.States[1]);
            var packedBefore = fixture.Packed.PackedBytes();
            for (byte left = 0; left < 4; left++)
                for (byte right = 0; right < 4; right++)
                {
                    AssertRead(fixture, truthTable, left, right);
                    comparisons++;
                }
            Assert.Equal(nativeBefore, GojoNumericFormat.Encode(fixture.Native, fixture.States[0], fixture.States[1]));
            Assert.Equal(packedBefore, fixture.Packed.PackedBytes());
        }
        Assert.Equal(256, comparisons);
    }

    [Theory]
    [InlineData(0)]  // Constant false.
    [InlineData(6)]  // Exclusive or.
    [InlineData(8)]  // And.
    [InlineData(14)] // Or.
    [InlineData(15)] // Constant true.
    public void RestoredNativeLawKeepsItsDeclaredParticipantsAndReadBehavior(int truthTable)
    {
        var original = Build(truthTable);
        // The local mapping is transported explicitly alongside the law. A new
        // world is never expected to recover our code assignments from names.
        var roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(
            original.Native, original.States[0], original.States[1]));
        var restored = new Fixture(original.Packed, Assert.IsType<Relation>(roots[0]), [roots[1], roots[2]]);
        Assert.NotSame(original.Native, restored.Native);
        Assert.True(FiniteLaw.TryRead(restored.Native, out var leftDomain, out var rightDomain,
            out var outputDomain, out _));
        foreach (var domain in new[] { leftDomain, rightDomain, outputDomain })
        {
            var entries = FiniteLaw.Entries(domain);
            Assert.Same(restored.States[0], entries[0]);
            Assert.Same(restored.States[1], entries[1]);
        }
        for (byte left = 0; left < 4; left++)
            for (byte right = 0; right < 4; right++)
                AssertRead(restored, truthTable, left, right);
    }

    [Fact]
    public void OpenOrForeignNativeSupportCannotSilentlyBecomeAClosedMask()
    {
        var fixture = Build(14);
        var open = StructuralConstraint.Open(fixture.States[0]);
        Assert.False(TryPackClosedSupport(open, fixture.States, out _));
        Assert.False(TryPackClosedSupport(StructuralConstraint.Closed(new Placeholder()), fixture.States, out _));
        var read = FiniteLaw.Evaluate(fixture.Native, open, Select(fixture.States, 1));
        Assert.True(FiniteLaw.TryReadResult(read, out var result, out var residue, out _));
        Assert.NotEmpty(FiniteLaw.Entries(residue));
        Assert.False(TryPackClosedSupport(result, fixture.States, out _));
        Assert.True(TryPackClosedSupport(StructuralConstraint.Closed(), fixture.States, out var empty));
        Assert.Equal((byte)0, empty);
    }

    [Fact]
    public void SharedInputCorrelationIsAnAdditionalContractNotImpliedByEqualMasks()
    {
        var xor = Build(6);
        var independent = xor.Packed.Predict([3, 3]);
        var shared = xor.Packed.Predict([3, 3], [0, 0]);
        Assert.Equal((byte)3, independent); // Either false or true.
        Assert.Equal((byte)1, shared);      // One shared truth state XOR itself is false.
        var nativeRead = FiniteLaw.Evaluate(xor.Native, Select(xor.States, 3), Select(xor.States, 3));
        Assert.True(FiniteLaw.TryReadResult(nativeRead, out var support, out _, out _));
        Assert.True(TryPackClosedSupport(support, xor.States, out var native));
        Assert.Equal(independent, native);
        Assert.NotEqual(shared, native);
    }

    private static Fixture Build(int truthTable)
    {
        IElement[] states = [new Placeholder(), new Placeholder()];
        var domain = StructuralConstraint.Closed(states);
        var tuples = new List<byte[]>();
        var rows = new List<Relation>();
        for (byte left = 0; left < 2; left++)
            for (byte right = 0; right < 2; right++)
            {
                var result = (byte)((truthTable >> (left + 2 * right)) & 1);
                tuples.Add([left, right, result]);
                rows.Add(FiniteLaw.Row(states[left], states[right], StructuralConstraint.Closed(states[result])));
            }
        return new(BitRelation.FromTuples(3, tuples), FiniteLaw.Create(domain, domain, domain, rows.ToArray()), states);
    }

    private static void AssertRead(Fixture fixture, int truthTable, byte left, byte right)
    {
        var leftScope = Select(fixture.States, left);
        var rightScope = Select(fixture.States, right);
        var leftBefore = FiniteLaw.Entries(leftScope);
        var rightBefore = FiniteLaw.Entries(rightScope);
        var read = FiniteLaw.Evaluate(fixture.Native, leftScope, rightScope);
        Assert.True(FiniteLaw.TryReadResult(read, out var support, out var residue, out _));
        Assert.Empty(FiniteLaw.Entries(residue));
        Assert.True(TryPackClosedSupport(support, fixture.States, out var native));
        var packed = fixture.Packed.Predict([left, right]);
        var expected = Expected(truthTable, left, right);
        Assert.Equal(expected, packed);
        Assert.Equal(expected, native);
        Assert.Equal(leftBefore, FiniteLaw.Entries(leftScope));
        Assert.Equal(rightBefore, FiniteLaw.Entries(rightScope));
    }

    private static byte Expected(int truthTable, byte leftSupport, byte rightSupport)
    {
        byte result = 0;
        for (var left = 0; left < 2; left++)
            for (var right = 0; right < 2; right++)
                if ((leftSupport & (1 << left)) != 0 && (rightSupport & (1 << right)) != 0)
                    result |= (byte)(1 << ((truthTable >> (left + 2 * right)) & 1));
        return result;
    }

    private static Relation Select(IElement[] states, byte mask) =>
        StructuralConstraint.Closed(states.Where((_, index) => (mask & (1 << index)) != 0).ToArray());

    private static bool TryPackClosedSupport(Relation scope, IElement[] states, out byte mask)
    {
        mask = 0;
        if (!StructuralConstraint.TryReadScope(scope, out var entries, out var seam, out var tail) || !seam.Equals(tail))
            return false;
        byte proposed = 0;
        foreach (var entry in entries)
        {
            var index = Array.FindIndex(states, state => ReferenceEquals(state, entry));
            if (index < 0) return false;
            proposed |= (byte)(1 << index);
        }
        mask = proposed;
        return true;
    }

    private sealed record Fixture(BitRelation Packed, Relation Native, IElement[] States);
}
