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

    [Fact]
    public void BitsThatAreNotACodeAreReportedNotGuessed()
    {
        var (source, _) = Stores.Store(1, 3);
        var (target, _) = Stores.Store(1, 3);
        source.Write(0, 0, true);
        source.Write(2, 0, true);
        var move = Stores.Wire(Flows.Move(3, 3), source, target);

        var step = move.Apply();

        Assert.True(step.Unadmitted[0]);
        Assert.True(step.Changed.None);
        Assert.True(source.Read(2, 0));
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
}
