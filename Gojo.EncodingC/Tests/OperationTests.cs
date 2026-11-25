using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Operations as relations run by binding (ops_claude/GojoOperations-2026-11.md,
/// §1.3 and §5): the counting laws are bindings of a balance, and one
/// product relation answers in every role, as a question narrowed or as a
/// run that settles, stays open or finds nothing.</summary>
public sealed class OperationTests
{
    private static int[] Next(TransitionLaw law, params int[] state) =>
        law.Tuple(Assert.Single(law.Afters(law.TupleIndex(state))));

    private static ulong Values(params int[] values) => values.Aggregate(0UL, (m, v) => m | 1UL << v);

    private static ulong Any(Shape shape) => shape.Size == 64 ? ulong.MaxValue : (1UL << shape.Size) - 1;

    /// <summary>The counting laws that are sums and gaps are bindings of a
    /// balance, and they keep the exact bits of the step functions they
    /// replace: sum forward into a new count, tally as a heap of bits,
    /// shortfall with the excess dropped as residue, and accumulate as heap
    /// written over the count it read, the overflow dropped.</summary>
    [Fact]
    public void CountingLawsAreBindingsOfABalanceAndKeepTheirBits()
    {
        static TransitionLaw Reference(Shape[] roles, Func<int[], int[]> step, params int[] unchanged) =>
            TransitionLaw.FromFunction(roles, step, new Accounting(true, [.. unchanged.Select(r => (Invariant)new Unchanged(r))]));

        foreach (var (a, b, w) in new[] { (3, 3, 1), (4, 2, 1), (2, 3, 2), (1, 1, 3) })
            Assert.True(Counts.Sum(a, b, w).SameTable(Reference(
                [Shape.Unary(a), Shape.Unary(b), Shape.Unary(a + w * b)], s => [s[0], s[1], s[0] + w * s[1]], 0, 1)));

        foreach (var (have, want) in new[] { (3, 3), (4, 2), (2, 5) })
            Assert.True(Counts.Shortfall(have, want).SameTable(Reference(
                [Shape.Unary(have), Shape.Unary(want), Shape.Unary(want)], s => [s[0], s[1], s[1] > s[0] ? s[1] - s[0] : 0], 0, 1)));

        foreach (var bits in new[] { 1, 3, 5 })
            Assert.True(Counts.Tally(bits).SameTable(Reference(
                [.. Enumerable.Repeat(Shape.Bit, bits), Shape.Unary(bits)], s => [.. s[..bits], s[..bits].Sum()], [.. Enumerable.Range(0, bits)])));

        foreach (var capacity in new[] { 1, 3, 6 })
            Assert.True(Counts.Accumulate(capacity).SameTable(Reference(
                [Shape.Bit, Shape.Unary(capacity)], s => [s[0], s[0] == 1 ? (s[1] < capacity ? s[1] + 1 : capacity) : s[1]], 0)));

        Assert.All(new[] { Counts.Sum(3, 3), Counts.Shortfall(3, 3), Counts.Tally(3), Counts.Accumulate(3) },
            law => Assert.True(law.Accounting.Functional));
    }

    /// <summary>The 3 × 4 comparison as a relation (from ops_sol): over
    /// a, b ∈ 0..4 and c ∈ 0..16 the product has 425 bits and admits 25
    /// tuples. It answers whichever roles are given: the product, a quotient,
    /// both ordered factors of 12 while still excluding (3, 3) and (4, 4), all
    /// of b when a and c are 0, nothing for a = 2 and c = 5. Bound to one
    /// sample, both inputs pair only with themselves, so only squares remain
    /// and 12 contradicts.</summary>
    [Fact]
    public void TheProductAnswersInEveryRole()
    {
        var times = Operations.Product(4, 4);
        Assert.Equal(425, times.BitCount);
        Assert.Equal(25, times.AdmittedCount);

        ulong a = Any(Shape.Unary(4)), b = a, c = Any(Shape.Unary(16));
        Assert.Equal(Values(12), times.Project(2, [Values(3), Values(4), c]));
        Assert.Equal(Values(4), times.Project(1, [Values(3), b, Values(12)]));
        Assert.Equal(Values(3, 4), times.Project(0, [a, b, Values(12)]));
        Assert.Equal(Values(3, 4), times.Project(1, [a, b, Values(12)]));
        Assert.False(times.Admits(3, 3, 12));
        Assert.False(times.Admits(4, 4, 12));
        Assert.Equal(b, times.Project(1, [Values(0), b, Values(0)]));
        Assert.Equal(0UL, times.Project(1, [Values(2), b, Values(5)]));

        Assert.Equal(Values(0, 1, 4, 9, 16), times.Project(2, [a, b, c], [0, 0, 1]));
        Assert.Equal(0UL, times.Project(0, [a, b, Values(12)], [0, 0, 1]));
    }

    /// <summary>Run instead of narrowed, the product read backward is a
    /// division that a structure has to finish: one answer for 12 over 3, and
    /// every b for 0 over 0, so the law alone cannot run there. For 5 over 2
    /// nothing fits: the step holds and the relation stays unmet. Bound to one
    /// count for both inputs, the product squares it.</summary>
    [Fact]
    public void RunBackwardTheProductDividesOrLeavesTheChoiceOpen()
    {
        var times = Operations.Product(4, 4);
        var divide = Operations.Bind(times, times.Roles, reads: [0, -1, 2], writes: [-1, 1, -1]);
        Assert.False(divide.Accounting.Functional);
        Assert.Equal([3, 4, 12], Next(divide, 3, 0, 12));
        Assert.Equal(5, divide.Afters(divide.TupleIndex([0, 2, 0])).Count);
        Assert.Equal([2, 0, 5], Next(divide, 2, 0, 5));
        Assert.False(times.Admits(2, 0, 5));

        Shape side = Shape.Unary(4), area = Shape.Unary(16);
        var square = Operations.Bind(times, [side, area], reads: [0, 0, -1], writes: [-1, -1, 1]);
        Assert.Equal([0, 1, 4, 9, 16], Enumerable.Range(0, 5).Select(x => Next(square, x, 0)[1]));
    }

    /// <summary>Heap read backward is subtraction. The gap is a balance with a
    /// face on each pan, what is short against what is over. Writing both faces
    /// leaves it open, since any amount can sit on both; with the other face
    /// free as residue, kept as small as it can be, one face settles, and the
    /// other face is the same binding the other way round.</summary>
    [Fact]
    public void HeapNarrowedBackwardSubtractsAndTheGapKeepsOneFace()
    {
        Shape three = Shape.Unary(3), six = Shape.Unary(6);
        var heap = Operations.Balance([three, three, six], 1, 1, -1);
        Assert.Equal(Values(2), heap.Project(1, [Values(3), Any(three), Values(5)]));
        Assert.Equal(0UL, heap.Project(1, [Values(3), Any(three), Values(2)]));

        var gap = Operations.Balance([three, three, three, three], 1, -1, 1, -1);
        var faces = Operations.Bind(gap, [three, three, three, three], reads: [0, 1, -1, -1], writes: [-1, -1, 2, 3]);
        Assert.False(faces.Accounting.Functional);
        var settled = Operations.Bind(gap, [three, three, three], reads: [0, 1, -1, -1], writes: [-1, -1, 2, -1]);
        Assert.True(settled.Accounting.Functional);
        Assert.Equal([1, 3, 2], Next(settled, 1, 3, 0));
        Assert.Equal([3, 1, 0], Next(settled, 3, 1, 0));
    }

    [Fact]
    public void ABindingNamesEachRoleOnceAndMatchesShapes()
    {
        var times = Operations.Product(2, 2);
        Assert.Throws<ArgumentException>(() => Operations.Bind(times, times.Roles, [0, 1, 2], [-1, -1, 2]));
        Assert.Throws<ArgumentException>(() => Operations.Bind(times, [Shape.Unary(3), Shape.Unary(2), Shape.Unary(4)], [0, 1, -1], [-1, -1, 2]));
        Assert.Throws<ArgumentException>(() => Operations.Bind(times, times.Roles, [-1, -1, -1], [2, -1, 2]));
        Assert.Throws<ArgumentException>(() => Operations.Balance([Shape.Bit, Shape.Bit], 1, 0));
    }
}
