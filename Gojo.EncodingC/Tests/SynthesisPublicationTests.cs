using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class SynthesisPublicationTests
{
    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void AReceiverRefinesOnlyThroughThePublishedRungOfTheSameObservation(ExecutionConformancePath path)
    {
        var supplier = new Store(0, 3);
        var acquisition = new Ladder(supplier, [4, 4]);
        acquisition.Restrict(0, 9);
        acquisition.Restrict(1, 11);
        acquisition.Measure(2, 2, rung: 1);
        var coarse = supplier.Publish([0, 1, 2], [.. acquisition.Possible[1].Planes]);
        var fine = supplier.Publish([0, 1, 2], [.. acquisition.Possible[0].Planes]);
        var wrongOrder = supplier.Publish([1, 0, 2], [.. acquisition.Possible[0].Planes]);
        var source = new Engine().Hold(supplier);

        var receiver = new Store(0, 3);
        var display = new Ladder(receiver, [4, 4]);
        var optional = Socket.For(fine);
        var readCoarse = ReadPort(coarse, display.Possible[1]);
        var readFine = ReadPort(optional, display.Possible[0]);
        var receipt = readFine.Record();
        var answer = Supports.On(receiver, Field.Bit(receiver.AddPlane()));
        var meetsRequest = RelationLaw.FromPredicate([display.Digits[0].Shape, display.Digits[1].Shape, Shape.Bit],
            roles => roles[2] == (roles[0] + 4 * roles[1] >= 10 ? 1 : 0));
        var compare = new Wiring(meetsRequest, display.Possible[0].Variable, display.Possible[1].Variable, answer.Variable);
        for (var lane = 0; lane < receiver.Count; lane++) compare.Add(lane, lane, lane);
        var observer = new Engine().Add(readCoarse).Add(readFine).Add(compare);

        path.Step(observer);
        for (var lane = 0; lane < receiver.Count; lane++)
        {
            Assert.True(readFine.Rows.Read(receipt.Skipped, lane));
            Assert.Equal((8L, 11L), display.Interval(lane));
            Assert.Equal(Mask(0, 1), answer.Values(lane));
        }
        Assert.False(coarse.SamePosition(0, wrongOrder, 0));
        Assert.All(Enumerable.Range(0, coarse.Count), lane => Assert.True(coarse.SamePosition(lane, fine, lane)));

        optional.Plug(fine);
        path.Step(observer);
        Assert.True(readFine.Rows.Read(receipt.Skipped).None);
        Assert.Equal(9, display.Read(0));
        Assert.Equal(11, display.Read(1));
        Assert.Equal(Mask(0), answer.Values(0));
        Assert.Equal(Mask(1), answer.Values(1));
        Assert.Equal((8L, 11L), display.Interval(2));
        Assert.Equal(Mask(0, 1), answer.Values(2));
        Assert.Equal((8L, 11L), acquisition.Interval(2));
        Assert.False(coarse.IsTaken);
        Assert.False(fine.IsTaken);

        var saved = Transport.SaveLocals(source, observer);
        var restored = Transport.LoadLocals(saved).Select(local => local.Engine).ToArray();
        var copiedCoarse = restored[1].Wirings[0].Bindings[0].Surface;
        var copiedFine = restored[1].Wirings[1].Bindings[0].Surface;
        Assert.All(Enumerable.Range(0, coarse.Count), lane => Assert.True(copiedCoarse.SamePosition(lane, copiedFine, lane)));
        Assert.Equal(saved, Transport.SaveLocals(restored));
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void ReframingKeepsTheSourceBasisAndCoupledLossAcrossSaveAndLoad(ExecutionConformancePath path)
    {
        var acquired = new Ladder(new Store(0, 1), [6]);
        acquired.Restrict(0, 0, Mask(3, 4));
        var source = acquired.Store.Publish([0], [.. acquired.Possible[0].Planes]);
        var supplier = new Engine().Hold(acquired.Store);
        var receiver = new Store(0, 1);
        var copy = new Ladder(receiver, [6]);
        var display = new Ladder(receiver, [9]);
        var loss = new Ladder(receiver, [5]);
        var observation = ReadPort(source, copy.Possible[0]);
        var rescale = Numbers.Rescale(5, 8);
        var projection = new Wiring(rescale.Relation, copy.Possible[0].Variable, display.Possible[0].Variable, loss.Possible[0].Variable);
        projection.Add(0, 0, 0);
        var observer = new Engine().Add(observation).Add(projection);

        Assert.True(path.Step(observer).Steps[1].Ambiguous[0]);
        Assert.Equal(Mask(4, 6), display.Possible[0].Values(0));
        Assert.Equal(Mask(2, 4), loss.Possible[0].Values(0));

        var saved = Transport.SaveLocals(supplier, observer);
        var restored = Transport.LoadLocals(saved).Select(local => local.Engine).ToArray();
        var read = restored[1].Wirings[1];
        var restoredLoss = loss.Possible[0].In(read.Bindings[2].Surface);
        var restoredDisplay = display.Possible[0].In(read.Bindings[1].Surface);
        Assert.Equal(saved, Transport.SaveLocals(restored));
        Assert.Equal(Mask(4, 6), restoredDisplay.Values(0));
        Assert.Equal(Mask(2, 4), restoredLoss.Values(0));
        restoredDisplay.Restrict(0, Mask(4));
        Assert.True(path.Step(restored[1]).Steps[1].Unadmitted.None);
        var numerator = Assert.IsType<int>(read.Value(0, 0));
        var displayed = Assert.IsType<int>(read.Value(0, 1));
        var remainder = Assert.IsType<int>(read.Value(0, 2));
        Assert.Equal((3, 4, 4), (numerator, displayed, remainder));
        Assert.Equal(numerator * 8, displayed * 5 + remainder); // 4/8 source ticks, or 4/40 of the whole.
        var retained = acquired.Possible[0].In(Assert.Single(restored[0].Stores));
        Assert.Equal(Mask(3, 4), retained.Values(0));
        Assert.Equal(Mask(3, 4), acquired.Possible[0].Values(0));

        var mismatch = Transport.LoadLocals(saved)[1].Engine;
        var mismatchedRead = mismatch.Wirings[1];
        display.Possible[0].In(mismatchedRead.Bindings[1].Surface).Restrict(0, Mask(4));
        var wrongLoss = loss.Possible[0].In(mismatchedRead.Bindings[2].Surface);
        wrongLoss.Restrict(0, Mask(2));
        Assert.True(path.Step(mismatch).Steps[1].Unadmitted[0]);
        Assert.True(wrongLoss.Empty[0]);
    }

    private static Wiring ReadPort(Surface source, Supports target)
    {
        var read = new Wiring(Settling.Same(target.Field.Shape),
            Binding.Open(source, [.. Enumerable.Range(0, source.PlaneCount)]), target.Variable);
        for (var lane = 0; lane < source.Count; lane++) read.Add(lane, lane);
        return read;
    }

    private static ulong Mask(params int[] values) => values.Aggregate(0UL, (mask, value) => mask | 1UL << value);
}
