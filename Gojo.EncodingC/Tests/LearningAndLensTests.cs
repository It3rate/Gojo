using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class LearningAndLensTests
{
    [Fact]
    public void AStructureLearnsThePassageLawFromWhatHappened()
    {
        var structure = new Structure(1, 24);
        var rows = new Wiring(Flows.Passage, new Binding(structure, 0), new Binding(structure, 0), new Binding(structure, 0));
        for (var state = 0; state < 8; state++) rows.Add(3 * state, 3 * state + 1, 3 * state + 2);
        var table = new LearnedTable(Flows.Passage.Roles);

        for (var tick = 0; tick < 4; tick++)
        {
            for (var state = 0; state < 8; state++)
                for (var bit = 0; bit < 3; bit++)
                    structure.Write(0, 3 * state + bit, ((state >> bit) & 1) != 0);
            var step = rows.Apply();
            table.Witness(step, rows.Live, tick);
        }

        var (law, unknown, violations) = table.Crystallize(Flows.Passage.Accounting);

        Assert.Equal(0, unknown);
        Assert.Empty(violations);
        Assert.True(law!.SameTable(Flows.Passage));
        Assert.Equal(new long[] { 0, 1, 2, 3 }, table.WitnessesOf(3, 6));
    }

    [Fact]
    public void ALearnedTableThatWouldMakeUnitsIsNotAdopted()
    {
        Shape[] roles = [Shape.Bit, Shape.Bit];
        var table = new LearnedTable(roles);
        for (var tick = 0; tick < 4; tick++)
        {
            table.Witness(0, 0, tick);
            table.Witness(1, 3, tick);
            table.Witness(2, 2, tick);
            table.Witness(3, 3, tick);
        }

        var (law, _, violations) = table.Crystallize(new Accounting(true, new PreservedSum([0, 1])));

        Assert.Null(law);
        Assert.Contains(new Violation(1, 3, 0), violations);
    }

    [Fact]
    public void UnseenPairsStayUnknownRatherThanGuessed()
    {
        var table = new LearnedTable([Shape.Bit]);
        table.Witness(0, 1, 0);

        var (admitted, open) = table.Predict(0);

        Assert.Equal(new[] { 1 }, admitted);
        Assert.True(open);
        Assert.Equal(LearnedRead.Unknown, table.Read(0, 0));
        Assert.True(table.Crystallize(Accounting.None).Unknown > 0);
    }

    [Fact]
    public void ALensNamesBitsForPeopleAndTheEncodingNeverReadsIt()
    {
        var valve = new Structure(2, 3);
        valve.Write(0, 0, true);
        var lens = new Lens()
            .Name(valve, "valve").Name(valve, 0, "occupied").Name(valve, 1, "open")
            .Name(Flows.Passage, 0, "S").Name(Flows.Passage, 1, "C").Name(Flows.Passage, 2, "T")
            .Values(Flows.Passage, 1, "closed", "open");

        Assert.Equal("valve[0]: occupied=1 open=0", lens.Describe(valve, 0));
        Assert.Equal("[S=1, C=open, T=0] -> [S=0, C=open, T=1]", lens.Describe(Flows.Passage, 3, 6));

        var unnamed = new Structure(2, 3);
        unnamed.Write(0, 0, true);
        foreach (var structure in new[] { valve, unnamed })
        {
            structure.Write(1, 1, true);
            var rows = new Wiring(Flows.Passage, new Binding(structure, 0), new Binding(structure, 1), new Binding(structure, 0));
            rows.Add(0, 1, 2);
            rows.Apply();
        }
        Assert.Equal(unnamed.Read(0), valve.Read(0));
    }
}
