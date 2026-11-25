using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class OperationPairingTests
{
    private const int LeftCount = 3;
    private const int RightCount = 4;
    private const int Present = 0;
    private const int Before = 1;
    private const int LeftArrived = 1;
    private const int RightArrived = 2;
    private const int BothArrived = 3;

    private sealed record Pairing(Engine Engine, Store Left, Store Right, Store Pairs, Store Totals, Field Total)
    {
        public int? Product => Total.Read(Totals, 0);

        public (int Left, int Right, int Both) Formation =>
            (Pairs.Read(LeftArrived).Indexes().Count(),
                Pairs.Read(RightArrived).Indexes().Count(),
                Pairs.Read(BothArrived).Indexes().Count());

        public void Arrive(int? left, int? right)
        {
            if (left is int l) Left.Write(Present, l, true);
            if (right is int r) Right.Write(Present, r, true);
            Engine.Step();
        }
    }

    private static Pairing Arrange(bool recordFormation)
    {
        var left = new Store(recordFormation ? 2 : 1, LeftCount);
        var right = new Store(recordFormation ? 2 : 1, RightCount);
        var pairs = new Store(recordFormation ? 4 : 1, LeftCount * RightCount);
        var totals = new Store(0, LeftCount);
        var engine = new Engine();

        var occupancy = new Wiring(Settling.Justify(2),
            new Binding(left, Present), new Binding(right, Present), new Binding(pairs, Present));
        for (var l = 0; l < LeftCount; l++)
            for (var r = 0; r < RightCount; r++)
                occupancy.Add(l, r, l * RightCount + r);
        engine.Add(occupancy);

        if (recordFormation)
        {
            var formation = new Wiring(PairFormation.Record,
                new Binding(left, Before), new Binding(right, Before),
                new Binding(left, Present), new Binding(right, Present),
                new Binding(pairs, LeftArrived), new Binding(pairs, RightArrived), new Binding(pairs, BothArrived));
            for (var l = 0; l < LeftCount; l++)
                for (var r = 0; r < RightCount; r++)
                {
                    var pair = l * RightCount + r;
                    formation.Add(l, r, l, r, pair, pair, pair);
                }
            engine.Add(formation);

            // Capture each source after every pair has read the same before-state.
            foreach (var factor in new[] { left, right })
            {
                var snapshot = new Wiring(Settling.Justify(1), new Binding(factor, Present), new Binding(factor, Before));
                for (var i = 0; i < factor.Count; i++)
                    snapshot.Add(i, i);
                engine.Add(snapshot);
            }
        }

        Field Count(int capacity) => new(Shape.Unary(capacity),
            [.. Enumerable.Range(0, capacity).Select(_ => totals.AddPlane())]);

        var rowTotal = Count(RightCount);
        var rowTally = new Wiring(Counts.Tally(RightCount),
            [.. Enumerable.Range(0, RightCount).Select(_ => new Binding(pairs, Present)), rowTotal.On(totals)]);
        for (var l = 0; l < LeftCount; l++)
            rowTally.Add([.. Enumerable.Range(l * RightCount, RightCount), l]);

        var firstTwo = Count(2 * RightCount);
        var sumFirstTwo = new Wiring(Counts.Sum(RightCount, RightCount),
            rowTotal.On(totals), rowTotal.On(totals), firstTwo.On(totals));
        sumFirstTwo.Add(0, 1, 0);

        var total = Count(LeftCount * RightCount);
        var fold = new Wiring(Counts.Sum(2 * RightCount, RightCount),
            firstTwo.On(totals), rowTotal.On(totals), total.On(totals));
        fold.Add(0, 2, 0);
        engine.Add(rowTally).Add(sumFirstTwo).Add(fold);
        return new Pairing(engine, left, right, pairs, totals, total);
    }

    [Fact]
    public void EqualProductsCanRetainDifferentHistoriesOfPairFormation()
    {
        var sequential = Arrange(recordFormation: true);
        for (var l = 0; l < LeftCount; l++) sequential.Arrive(l, null);
        for (var r = 0; r < RightCount; r++) sequential.Arrive(null, r);

        var simultaneous = Arrange(recordFormation: true);
        for (var i = 0; i < LeftCount; i++) simultaneous.Arrive(i, i);
        simultaneous.Arrive(null, 3);

        var leftThenRight = Arrange(recordFormation: true);
        for (var i = 0; i < LeftCount; i++)
        {
            leftThenRight.Arrive(i, null);
            leftThenRight.Arrive(null, i);
        }
        leftThenRight.Arrive(null, 3);

        Assert.All(new[] { sequential, simultaneous, leftThenRight }, pairing =>
        {
            Assert.Equal(12, pairing.Product);
            Assert.Equal(Plane.Ones(12), pairing.Pairs.Read(Present));
        });
        Assert.Equal((0, 12, 0), sequential.Formation);
        Assert.Equal((3, 6, 3), simultaneous.Formation);
        Assert.Equal((3, 9, 0), leftThenRight.Formation);
        Assert.True(simultaneous.Pairs.Read(LeftArrived, RightCount)); // Left 1 with right 0.
        Assert.True(simultaneous.Pairs.Read(RightArrived, 1)); // Left 0 with right 1.
        Assert.True(simultaneous.Pairs.Read(BothArrived, RightCount + 1)); // Left 1 with right 1.
    }

    [Fact]
    public void AProductWithoutHistoryStillTracksWhichSitesArePaired()
    {
        var pairing = Arrange(recordFormation: false);
        pairing.Arrive(0, 1);
        pairing.Arrive(2, 3);
        Assert.Equal(1, pairing.Pairs.PlaneCount);
        Assert.Equal(4, pairing.Product);
        Assert.Equal(Plane.Of(12, 1, 3, 9, 11), pairing.Pairs.Read(Present));

        pairing.Left.Write(Present, 0, false);
        pairing.Engine.Step();
        Assert.Equal(2, pairing.Product);
        Assert.Equal(Plane.Of(12, 9, 11), pairing.Pairs.Read(Present));
        Assert.Equal(Plane.Of(RightCount, 1, 3), pairing.Right.Read(Present));
    }

    [Fact]
    public void ATransportedProductKeepsFormationEvidenceAndContinuesWithTheSameBindings()
    {
        var original = Arrange(recordFormation: true);
        original.Arrive(0, 0);
        original.Arrive(1, 1);
        Assert.Equal((1, 1, 2), original.Formation);
        Assert.Equal(4, original.Product);

        var saved = Transport.Save(original.Engine, original.Left, original.Right, original.Pairs, original.Totals);
        var (engine, stores) = Transport.Load(saved);
        var restored = new Pairing(engine, stores[0], stores[1], stores[2], stores[3], original.Total);
        Assert.Equal(original.Formation, restored.Formation);
        Assert.Equal(original.Product, restored.Product);

        restored.Arrive(2, 2);
        restored.Arrive(null, 3);
        Assert.Equal(12, restored.Product);
        Assert.Equal((3, 6, 3), restored.Formation);
        Assert.Equal(4, original.Product);
    }

    [Fact]
    public void ReformationCanReplaceTheRecordWhileRemovalLeavesItReadable()
    {
        var pairing = Arrange(recordFormation: true);
        pairing.Arrive(0, 0);
        pairing.Left.Write(Present, 0, false);
        pairing.Engine.Step();
        Assert.Equal(0, pairing.Product);
        Assert.Equal((0, 0, 1), pairing.Formation);

        pairing.Arrive(0, null);
        pairing.Engine.Step();
        Assert.Equal(1, pairing.Product);
        Assert.Equal((1, 0, 0), pairing.Formation);
    }
}
