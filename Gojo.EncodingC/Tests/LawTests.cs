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
        var structure = new Structure(2, rows * 3);
        var wiring = new Wiring(Flows.Passage, new Binding(structure, 0), new Binding(structure, 1), new Binding(structure, 0));
        for (var i = 0; i < rows; i++)
        {
            wiring.Add(3 * i, 3 * i + 1, 3 * i + 2);
            structure.Write(0, 3 * i, true);
            structure.Write(1, 3 * i + 1, i % 2 == 0);
        }

        var step = wiring.Apply();

        Assert.Single(step.Waves);
        Assert.Equal(65, step.Changed.Count);
        for (var i = 0; i < rows; i++)
        {
            Assert.Equal(i % 2 == 0, structure.Read(0, 3 * i + 2));
            Assert.Equal(i % 2 != 0, structure.Read(0, 3 * i));
        }
    }

    [Fact]
    public void AUnaryMoveTakesOneUnitPerStepUntilNothingIsLeft()
    {
        var (source, count) = Stores.Store(1, 3);
        var (target, _) = Stores.Store(1, 3);
        count.Write(source, 0, 2);
        var move = Stores.Wire(Flows.Move(3, 3), source, target);

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
        var (source, _) = Stores.Store(1, 3);
        var (target, _) = Stores.Store(1, 3);
        source.Write(0, 0, true);
        source.Write(2, 0, true);
        var move = Stores.Wire(Flows.Move(3, 3), source, target);

        var step = move.Apply();

        Assert.True(step.Unread[0]);
        Assert.True(step.Unadmitted.None);
        Assert.True(step.Changed.None);
        Assert.True(source.Read(2, 0));

        var onlyFromZero = TransitionLaw.FromFunction([Shape.Bit], s => s[0] == 0 ? [1] : null, Accounting.None);
        var bit = new Structure(1, 1);
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
        var structure = new Structure(2, 3);
        var wiring = new Wiring(Flows.Passage, new Binding(structure, 0), new Binding(structure, 1), new Binding(structure, 0));

        Assert.Throws<ArgumentException>(() => wiring.Add(0, 1, 0));
    }

    [Fact]
    public void ACoarseReadIsARelationAndCannotRecoverWhatItDropped()
    {
        // A count of 0..4 read as "none" or "some". No special rung type: a relation law.
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
        var binding = new Binding(new Structure(1, 1), 0);

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
        var structure = new Structure(7, 1);
        var sixtyFour = Supports.On(structure, new Field(Shape.Binary(64), 0, 1, 2, 3, 4, 5));
        sixtyFour.Restrict(0, 1UL);

        Assert.Equal(Plane.Ones(1), sixtyFour.Settled);
        Assert.Throws<ArgumentException>(() => Supports.On(structure, new Field(Shape.Binary(65), 0, 1, 2, 3, 4, 5, 6)));
        var wide = RelationLaw.FromPredicate([Shape.Binary(65)], t => t[0] == 64);
        Assert.Throws<ArgumentException>(() => wide.Project(0, [ulong.MaxValue]));
    }
}
