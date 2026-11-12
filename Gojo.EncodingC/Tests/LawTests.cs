using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class LawTests
{
    [Fact]
    public void ThePassageLawIsEightBytesThatKeepTheirAccounting()
    {
        var passage = Flows.Passage;

        Assert.Equal(new byte[] { 0x01, 0x02, 0x04, 0x00, 0x10, 0x20, 0x48, 0x80 }, passage.Bytes());
        Assert.Equal(0x8048201000040201UL, passage.AsUInt64());
        Assert.True(passage.IsFunctional);
        Assert.True(passage.Writes(0));
        Assert.False(passage.Writes(1));
        Assert.True(passage.Writes(2));
    }

    [Fact]
    public void ALawThatBreaksItsAccountingIsNotMade()
    {
        Shape[] roles = [Shape.Bit, Shape.Bit];
        var keepsUnits = new Accounting(true, new PreservedSum([0, 1]));

        Assert.Throws<ArgumentException>(() =>
            TransitionLaw.FromFunction(roles, s => s is [1, 0] ? [1, 1] : s, keepsUnits));

        var law = TransitionLaw.TryFromPairs(roles, [(0, 0), (1, 3), (2, 2), (3, 3)], keepsUnits, out var violations);
        Assert.Null(law);
        var violation = Assert.Single(violations);
        Assert.Equal((1, 3, 0), (violation.Before, violation.After, violation.Invariant));
    }

    [Fact]
    public void ALawRunsAcrossAsManyLanesAsThereAreRows()
    {
        const int rows = 130;
        var store = new Store(2, rows * 3);
        var wiring = new Wiring(Flows.Passage, new Binding(store, 0), new Binding(store, 1), new Binding(store, 0));
        for (var i = 0; i < rows; i++)
        {
            wiring.Add(3 * i, 3 * i + 1, 3 * i + 2);
            store.Write(0, 3 * i, true);
            store.Write(1, 3 * i + 1, i % 2 == 0);
        }

        var step = wiring.Apply();

        Assert.Single(step.Waves);
        Assert.Equal(65, step.Changed.Count);
        for (var i = 0; i < rows; i++)
        {
            Assert.Equal(i % 2 == 0, store.Read(0, 3 * i + 2));
            Assert.Equal(i % 2 != 0, store.Read(0, 3 * i));
        }
    }

    [Fact]
    public void AUnaryMoveTakesOneUnitPerStepUntilNothingIsLeft()
    {
        var (source, count) = Make.Counts(1, 3);
        var (target, _) = Make.Counts(1, 3);
        count.Write(source, 0, 2);
        var move = Make.Wire(Flows.Move(3, 3), source, target);

        move.Apply();
        Assert.Equal((1, 1), (count.Read(source, 0), count.Read(target, 0)));
        move.Apply();
        Assert.Equal((0, 2), (count.Read(source, 0), count.Read(target, 0)));
        Assert.True(move.Apply().Changed.None);
    }

    /// <summary>Two different reasons a row does nothing. Bits that are not a
    /// code are noise in the state: the row is unread. A state the law says
    /// nothing about is a gap in the law: the row is unadmitted. Neither is
    /// guessed at, and they are reported apart.</summary>
    [Fact]
    public void BitsThatAreNotACodeAreReportedNotGuessed()
    {
        var (source, _) = Make.Counts(1, 3);
        var (target, _) = Make.Counts(1, 3);
        source.Write(0, 0, true);
        source.Write(2, 0, true);
        var move = Make.Wire(Flows.Move(3, 3), source, target);

        var step = move.Apply();

        Assert.True(step.Unread[0]);
        Assert.True(step.Unadmitted.None);
        Assert.True(step.Changed.None);
        Assert.True(source.Read(2, 0));

        var onlyFromZero = TransitionLaw.FromFunction([Shape.Bit], s => s[0] == 0 ? [1] : null, Accounting.None);
        var bit = new Store(1, 1);
        bit.Write(0, 0, true);
        var silent = new Wiring(onlyFromZero, new Binding(bit, 0));
        silent.Add(0);

        var stuck = silent.Apply();

        Assert.True(stuck.Unadmitted[0]);
        Assert.True(stuck.Unread.None);
    }

    [Fact]
    public void OnePositionUsedTwiceCannotBeWrittenAsTwo()
    {
        var store = new Store(2, 3);
        var wiring = new Wiring(Flows.Passage, new Binding(store, 0), new Binding(store, 1), new Binding(store, 0));

        Assert.Throws<ArgumentException>(() => wiring.Add(0, 1, 0));
    }

    [Fact]
    public void ACoarseReadIsARelationAndCannotRecoverWhatItDropped()
    {
        // A count of 0..4 read as "none" or "some". No special workspace type: a relation law.
        var presence = RelationLaw.FromPredicate([Shape.Unary(4), Shape.Bit], t => t[1] == (t[0] > 0 ? 1 : 0));

        Assert.Equal(0b10UL, presence.Project(1, [1UL << 3, 0b11]));
        Assert.Equal(0b11110UL, presence.Project(0, [0b11111, 0b10]));
    }

    [Fact]
    public void SameTableComparesTablesNotWhoMadeThem()
    {
        Assert.True(Odv.Addition(2).SameTable(Odv.Addition(5)));
        Assert.False(Odv.Addition(1).SameTable(Odv.Addition(2)));
    }

    [Fact]
    public void PredictionCollectsEveryStateALaneCouldReachWithoutRunningIt()
    {
        var joint = Enumerable.Range(0, 8).Select(_ => new Plane(2)).ToArray();
        joint[3][0] = true; // lane 0 may be S occupied, gate open, T empty
        joint[1][0] = true; // or S occupied with the gate closed
        joint[6][1] = true; // lane 1 has already handed off

        var next = Flows.Passage.PredictJoint(joint);

        Assert.True(next[6][0]);
        Assert.True(next[1][0]);
        Assert.False(next[3][0]);
        Assert.True(next[6][1]);
    }

    /// <summary>The table bits are the law. Lists a law or binding hands out are
    /// views or copies, so nothing outside can make execution disagree with
    /// the bits.</summary>
    [Fact]
    public void WhatALawHandsOutCannotChangeIt()
    {
        var afters = Flows.Passage.Afters(3);
        var binding = new Binding(new Store(1, 1), 0);

        Assert.IsNotType<int[]>(afters);
        Assert.Throws<NotSupportedException>(() => ((IList<int>)afters)[0] = 3);
        Assert.Throws<NotSupportedException>(() => ((IList<int>)Odv.Addition().AdmittedTuples()[0])[0] = 7);
        Assert.Throws<NotSupportedException>(() => ((IList<int>)binding.Planes)[0] = 0);
        Assert.Throws<NotSupportedException>(() => ((IList<Invariant>)Flows.Passage.Accounting.Invariants)[0] = new Unchanged(0));
        Assert.Equal(new[] { 6 }, Flows.Passage.Afters(3));
    }

    /// <summary>A primitive read needs only a few distinctions. One support or
    /// projection tells apart at most 64 values, and asking for more is
    /// refused rather than silently merging value 64 into value 0. A larger
    /// field is read through a coarser one first.</summary>
    [Fact]
    public void OnePrimitiveReadTellsApartAtMost64Values()
    {
        var store = new Store(7, 1);
        var sixtyFour = Supports.On(store, new Field(Shape.Binary(64), 0, 1, 2, 3, 4, 5));
        sixtyFour.Restrict(0, 1UL);

        Assert.Equal(Plane.Ones(1), sixtyFour.Settled);
        Assert.Throws<ArgumentException>(() => Supports.On(store, new Field(Shape.Binary(65), 0, 1, 2, 3, 4, 5, 6)));
        var wide = RelationLaw.FromPredicate([Shape.Binary(65)], t => t[0] == 64);
        Assert.Throws<ArgumentException>(() => wide.Project(0, [ulong.MaxValue]));
    }

    /// <summary>A Boolean operation is the smallest relation there is: eight
    /// bits over (a, b, out), one byte. Nothing else is needed to have the
    /// operation. The one table reads in every direction, and what is open
    /// passes through it: an output can settle its inputs, and one input can
    /// settle the output while the other is still open.</summary>
    [Fact]
    public void ABooleanOperationIsOneByteReadInEveryDirection()
    {
        Shape[] roles = [Shape.Bit, Shape.Bit, Shape.Bit];
        var and = RelationLaw.FromPredicate(roles, t => t[2] == (t[0] & t[1]));
        var or = RelationLaw.FromPredicate(roles, t => t[2] == (t[0] | t[1]));
        var xor = RelationLaw.FromPredicate(roles, t => t[2] == (t[0] ^ t[1]));

        Assert.Equal(new byte[] { 0x87 }, and.Bytes());
        Assert.Equal(new byte[] { 0xE1 }, or.Bytes());
        Assert.Equal(new byte[] { 0x69 }, xor.Bytes());

        const ulong zero = 0b01, one = 0b10, open = 0b11;
        Assert.Equal(one, and.Project(2, [one, one, open]));
        Assert.Equal(zero, and.Project(2, [zero, open, open]));
        Assert.Equal(one, and.Project(0, [open, open, one]));
        Assert.Equal(zero, and.Project(1, [one, open, zero]));
        Assert.Equal(open, and.Project(0, [open, open, zero]));
    }

    /// <summary>Adding two small counts is one relation over (a, b, sum). The
    /// same table subtracts, since a sum and one part settle the other part.
    /// What is open stays exactly as open as it was. The table has one bit per
    /// combination, so it grows with the product of the sizes: operands stay
    /// coarse, and a wider sum is the same small table at each rung of a
    /// ladder with a carry.</summary>
    [Fact]
    public void AddingTwoCountsIsOneRelationThatAlsoSubtracts()
    {
        var add = RelationLaw.FromPredicate([Shape.Unary(3), Shape.Unary(3), Shape.Unary(6)], t => t[2] == t[0] + t[1]);

        Assert.Equal(112, add.BitCount);
        Assert.Equal(16, add.AdmittedCount);
        Assert.Equal(1UL << 5, add.Project(2, [1UL << 2, 1UL << 3, 0x7F]));
        Assert.Equal(1UL << 3, add.Project(1, [1UL << 2, 0xF, 1UL << 5]));
        Assert.Equal(0b110000UL, add.Project(2, [0b0110, 1UL << 3, 0x7F]));
    }
}
