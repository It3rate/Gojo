using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class GuaranteeIntegrationTests
{
    [Fact]
    public void PublishedFourStepEncodingChartMatchesTheActualPackedFields()
    {
        var pairs = StructuralRoute.Pairs(5, [[0, 1], [1, 2], [2, 3], [3, 0]]);
        var chain = StructuralRoute.Chain(5, [0, 1, 2, 3, 0]);
        var cycle = StructuralRoute.Cycle(5, [0, 1, 2, 3]);
        Assert.Equal(new byte[] { 0x48, 0xA4, 0x0D }, pairs.TopologyBytes());
        Assert.Equal(new byte[] { 0x88, 0x06 }, chain.TopologyBytes());
        Assert.Equal(new byte[] { 0x88, 0x06 }, cycle.TopologyBytes());
        Assert.Equal(new long[] { 24, 15, 12 }, new[] { pairs.TopologyBits, chain.TopologyBits, cycle.TopologyBits });
        foreach (var route in new[] { pairs, chain, cycle }) Assert.Equal(new byte[] { 0x0F }, route.WordBytes());
        // Same raw topology bytes do not erase their different enclosing grammars.
        Assert.NotEqual(chain.Encode(), cycle.Encode());
    }

    [Fact]
    public void AConsumerUsesTheCurrentPublicClosureReadWithoutInspectingPrivateEndpoints()
    {
        var route = StructuralRoute.Cycle(5, [0, 1, 2, 3]);
        var circuit = new PulseCircuit(2);
        var source = circuit.State(true);
        var target = circuit.State();
        var guard = circuit.Guard();
        circuit.Bind(guard, route.Closure);
        circuit.Link(circuit.AddTable(PassageLaw.Controlled()), source, guard, target);

        var before = circuit.ValueBytes();
        Assert.False(circuit.Pulse().Committed); // The source has not published its proof.
        Assert.Equal(before, circuit.ValueBytes());
        route.Inspect(1);
        Assert.False(route.TryRetargetEndpoint(3, 1, 4).Applied);
        Assert.Equal((byte)2, route.Closure.ReadSupport());
        route.SetWord(0, 0);
        Assert.Equal((byte)2, route.Closure.ReadSupport()); // Topology does not depend on V.
        route.Unpack();
        Assert.False(circuit.Pulse().Committed);
        route.Inspect(4);
        route.TryRetargetEndpoint(3, 1, 4);
        Assert.False(circuit.Pulse().Committed); // Old positive answer was withdrawn.
        route.Inspect(4);
        var blocked = circuit.Pulse();
        Assert.True(blocked.Committed);
        Assert.False(blocked.Moved);
        Assert.Equal(source, circuit.Active);

        route.TryRetargetEndpoint(3, 1, 0);
        Assert.False(circuit.Pulse().Committed);
        route.Inspect(4);
        Assert.True(circuit.Pulse().Moved);
        Assert.Equal(target, circuit.Active);
    }

    [Fact]
    public void DeclaredFiniteSurveyAndLargerCostCasesConform()
    {
        var folder = Path.Combine(Path.GetTempPath(), "gojo-guarantees-" + Guid.NewGuid().ToString("N"));
        try
        {
            // Export performs independent endpoint and material-accounting checks.
            // Running it here keeps the declared bounded exhaustive audit in CI.
            GuaranteeSurvey.Export(folder);
            Assert.Equal(1111, File.ReadLines(Path.Combine(folder, "finite-routes.csv")).Count());
            Assert.Equal(13, File.ReadLines(Path.Combine(folder, "inspection-costs.csv")).Count());
            Assert.Equal(33, File.ReadLines(Path.Combine(folder, "shared-variation.csv")).Count());
            Assert.Equal(961, File.ReadLines(Path.Combine(folder, "gate-closure.csv")).Count());
        }
        finally
        {
            // The report creates only these files; cleanup is nonrecursive.
            foreach (var name in new[] { "finite-routes.csv", "inspection-costs.csv", "shared-variation.csv", "gate-closure.csv", "summary.txt" })
                File.Delete(Path.Combine(folder, name));
            if (Directory.Exists(folder)) Directory.Delete(folder);
        }
    }
}
