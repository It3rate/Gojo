using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class LearningAndLensTests
{
    [Fact]
    public void AStructureLearnsThePassageLawFromWhatHappened()
    {
        var store = new Store(1, 24);
        var rows = new Wiring(Flows.Passage, new Binding(store, 0), new Binding(store, 0), new Binding(store, 0));
        for (var state = 0; state < 8; state++) rows.Add(3 * state, 3 * state + 1, 3 * state + 2);
        var table = new LearnedTable(Flows.Passage.Roles);

        for (var tick = 0; tick < 4; tick++)
        {
            for (var state = 0; state < 8; state++)
                for (var bit = 0; bit < 3; bit++)
                    store.Write(0, 3 * state + bit, ((state >> bit) & 1) != 0);
            var step = rows.Apply();
            table.Witness(step, rows.Live);
        }

        var (law, unknown, violations, _) = table.Crystallize(Flows.Passage.Accounting);

        Assert.Equal(0, unknown);
        Assert.Empty(violations);
        Assert.True(law!.SameTable(Flows.Passage));
        Assert.Equal(4, table.Count(3, 6));
        Assert.Equal(Plane.Of(64, Enumerable.Range(0, 64).Where(Flows.Passage.BitAt)), table.Admitted);

        // What was learned is a store of small counts, so it is saved like any other.
        var (_, copies) = Transport.Load(Transport.Save(new Engine(), table.Counts));
        var restored = new LearnedTable(Flows.Passage.Roles, copies[0]);

        Assert.True(restored.Crystallize(Flows.Passage.Accounting).Law!.SameTable(Flows.Passage));
        Assert.Equal(40, Transport.Measure(new Engine(), table.Counts).Bytes);
    }

    [Fact]
    public void ALearnedTableThatWouldMakeUnitsIsNotAdopted()
    {
        Shape[] roles = [Shape.Bit, Shape.Bit];
        var table = new LearnedTable(roles);
        for (var tick = 0; tick < 4; tick++)
        {
            table.Witness(0, 0);
            table.Witness(1, 3);
            table.Witness(2, 2);
            table.Witness(3, 3);
        }

        var (law, _, violations, _) = table.Crystallize(new Accounting(true, new PreservedSum([0, 1])));

        Assert.Null(law);
        Assert.Contains(new Violation(1, 3, 0), violations);
    }

    /// <summary>A unit runs down a chain in one step: row (0, 1, 2) hands it to
    /// position 2, and row (2, 3, 4) runs in the next wave and hands it on.
    /// Each row's evidence is read around its own wave, so both rows did the
    /// same thing, a handoff. Read around the whole step instead, the first row
    /// would seem to destroy a unit and the second to make one, and a structure
    /// learning from that would learn something that never happened.</summary>
    [Fact]
    public void EachRowsEvidenceIsReadAroundItsOwnWave()
    {
        var chain = new Store(2, 5);
        var rows = new Wiring(Flows.Passage, new Binding(chain, 0), new Binding(chain, 1), new Binding(chain, 0));
        rows.Add(0, 1, 2);
        rows.Add(2, 3, 4);
        chain.Write(0, 0, true);
        chain.Write(1, 1, true);
        chain.Write(1, 3, true);
        var table = new LearnedTable(Flows.Passage.Roles);

        var step = rows.Apply();
        table.Witness(step, rows.Live);

        Assert.Equal(2, step.Waves.Count);
        Assert.True(chain.Read(0, 4));
        Assert.Equal(new[] { 3, 3 }, step.Before);
        Assert.Equal(new[] { 6, 6 }, step.After);
        Assert.Equal(LearnedRead.Admitted, table.Read(3, 6));
        Assert.Equal(2, table.Count(3, 6));
    }

    /// <summary>A learner is shown three rows. One holds bits that are not a
    /// code and one touches a retired position, so only one can be learned
    /// from. The table counts the two it could not read, and keeps the counts
    /// behind what it did learn: how sure it can be, not only what it says.</summary>
    [Fact]
    public void ALearnerKnowsHowMuchItCouldNotRead()
    {
        var (store, count) = Make.Counts(6, 2);
        count.Write(store, 0, 1);
        store.Write(1, 2, true);
        var move = new Wiring(Flows.Move(2, 2), new Binding(store, 0, 1), new Binding(store, 0, 1));
        move.Add(0, 1);
        move.Add(2, 3);
        move.Add(4, 5);
        store.Retire(5);
        var table = new LearnedTable(move.Law.Roles);

        table.Witness(move.Apply(), move.Live);

        Assert.Equal(2, table.Unread);
        Assert.Equal((1, 1), (table.Count(1, 3), table.Seen(1)));
    }

    /// <summary>A pair never seen after its before-state was seen four times is
    /// left out of the crystallized law, and the law says it was left out for
    /// that reason only. The table keeps counting: one later witness admits it.
    /// A count stops at four, which is all "often enough" needs; no history of
    /// when things happened is kept.</summary>
    [Fact]
    public void APairLeftOutForNotBeingSeenIsARuleOfThumbThatALaterWitnessUndoes()
    {
        var table = new LearnedTable([Shape.Bit]);
        for (var tick = 0; tick < 6; tick++)
        {
            table.Witness(0, 0);
            table.Witness(1, 1);
        }

        var first = table.Crystallize(Accounting.None);

        Assert.False(first.Law!.Admits(0, 1));
        Assert.Equal(Plane.Of(4, 1, 2), first.Unsupported);
        Assert.Equal(4, table.Count(0, 0));

        table.Witness(0, 1);
        var second = table.Crystallize(Accounting.None);

        Assert.True(second.Law!.Admits(0, 1));
        Assert.Equal(Plane.Of(4, 1), second.Unsupported);
    }

    [Fact]
    public void UnseenPairsStayUnknownRatherThanGuessed()
    {
        var table = new LearnedTable([Shape.Bit]);
        table.Witness(0, 1);

        var (admitted, open) = table.Predict(0);

        Assert.Equal(new[] { 1 }, admitted);
        Assert.True(open);
        Assert.Equal(LearnedRead.Unknown, table.Read(0, 0));
        Assert.True(table.Crystallize(Accounting.None).Unknown > 0);
    }

    [Fact]
    public void ALensNamesBitsForPeopleAndTheEncodingNeverReadsIt()
    {
        var valve = new Store(2, 3);
        valve.Write(0, 0, true);
        var lens = new Lens()
            .Name(valve, "valve").Name(valve, 0, "occupied").Name(valve, 1, "open")
            .Name(Flows.Passage, 0, "S").Name(Flows.Passage, 1, "C").Name(Flows.Passage, 2, "T")
            .Values(Flows.Passage, 1, "closed", "open");

        Assert.Equal("valve[0]: occupied=1 open=0", lens.Describe(valve, 0));
        Assert.Equal("[S=1, C=open, T=0] -> [S=0, C=open, T=1]", lens.Describe(Flows.Passage, 3, 6));

        var unnamed = new Store(2, 3);
        unnamed.Write(0, 0, true);
        foreach (var store in new[] { valve, unnamed })
        {
            store.Write(1, 1, true);
            var rows = new Wiring(Flows.Passage, new Binding(store, 0), new Binding(store, 1), new Binding(store, 0));
            rows.Add(0, 1, 2);
            rows.Apply();
        }
        Assert.Equal(unnamed.Read(0), valve.Read(0));
    }
}
