using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class OperationsEvidenceConformanceTests
{
    private static readonly Plane Here = Plane.Ones(1);

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void ARepeatedPairKeepsTheLatestNamedOccurrenceAcrossTransport(ExecutionConformancePath path)
    {
        var first = PairWorld(path, firstLeft: 0);
        var swapped = PairWorld(path, firstLeft: 1);
        Assert.Equal(Plane.Ones(2), first.Pairs.Read(first.Occupied));
        Assert.Equal(first.Pairs.Read(first.Occupied), swapped.Pairs.Read(swapped.Occupied));
        Assert.Equal(new int?[] { 2, 3 }, Enumerable.Range(0, 2).Select(row => first.Formed.Value(row, 4)));
        Assert.Equal(new int?[] { 3, 2 }, Enumerable.Range(0, 2).Select(row => swapped.Formed.Value(row, 4)));
        Assert.True(first.Left.SamePosition(0, first.Formed.Bindings[2].Surface, first.Formed.Row(0)[2]));
        Assert.False(first.Left.SamePosition(0, first.Formed.Bindings[2].Surface, first.Formed.Row(1)[2]));

        var saved = Transport.Save(first.Engine);
        var (loaded, _) = Transport.Load(saved);
        var loadedFormation = loaded.Wirings[1];
        var loadedLeft = Assert.IsType<Store>(loadedFormation.Bindings[2].Surface);
        Assert.Equal(saved, Transport.Save(loaded));

        foreach (var present in new[] { false, true })
        {
            first.Left.Write(first.Present, 0, present);
            loadedLeft.Write(first.Present, 0, present);
            // The receipt reads the preceding capture, then the capture advances.
            path.Step(first.Engine);
            path.Step(loaded);
            Assert.Equal(present, first.Pairs.Read(first.Occupied, 0));
            Assert.Equal(present ? 1 : 2, first.Formed.Value(0, 4));
            Assert.Equal(3, first.Formed.Value(1, 4));
            Assert.Equal(Transport.Save(first.Engine), Transport.Save(loaded));
        }
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void HoldsExposeWhichOfFourDifferentInputsHasToChange(ExecutionConformancePath path)
    {
        const int closed = 0, empty = 1, full = 2, noBooking = 3;
        var all = Plane.Ones(4);
        var sites = new Store(0, 4);
        var stock = FieldOn(sites, Shape.Unary(2));
        var dock = FieldOn(sites, Shape.Unary(2));
        var booking = new Tension(sites, dock, 1);
        var opportunity = FieldOn(sites, Shape.Bit);
        var outward = FieldOn(sites, Shape.Unary(2));
        var incoming = FieldOn(sites, Shape.Unary(2));
        var returnedRoom = FieldOn(sites, Shape.Bit);
        for (var row = 0; row < sites.Count; row++)
        {
            stock.Write(sites, row, row == empty ? 0 : 2);
            opportunity.Write(sites, row, row == closed ? 0 : 1);
            booking.Minus.Write(sites, row, row == noBooking ? 0 : 1);
            dock.Write(sites, row, row == full ? 2 : 0);
        }
        var refill = Tension.Wire(Flows.Convert(2, 2, 2, 2), [incoming.On(sites), stock.On(sites)], all);
        var freeRoom = Tension.Wire(Flows.Convert(2, 2, 2, 2),
            [dock.On(sites), outward.On(sites)], all, returnedRoom.On(sites));
        var delivery = Tension.Wire(Flows.Fulfill(2, 1, 2, take: 2, give: 2),
            [stock.On(sites), booking.Minus.On(sites), dock.On(sites)], all, opportunity.On(sites));
        var receiver = new Engine().Add(refill).Add(freeRoom).Add(delivery);

        Assert.True(path.Step(receiver).Steps[2].Quiet);
        Assert.Equal((2, 1, 0, 0), Inputs(closed));
        Assert.Equal((0, 1, 0, 1), Inputs(empty));
        Assert.Equal((2, 1, 2, 1), Inputs(full));
        Assert.Equal((2, 0, 0, 1), Inputs(noBooking));

        opportunity.Write(sites, closed, 1);
        Assert.Equal(Plane.Of(4, closed), path.Step(receiver).Steps[2].Changed);
        incoming.Write(sites, empty, 2);
        Assert.Equal(Plane.Of(4, empty), path.Step(receiver).Steps[2].Changed);
        returnedRoom.Write(sites, full, 1);
        Assert.Equal(Plane.Of(4, full), path.Step(receiver).Steps[2].Changed);
        returnedRoom.Write(sites, full, 0);
        Assert.Equal(2, outward.Read(sites, full));
        Assert.Equal(Plane.Of(4, noBooking), booking.Demand(Plane.Of(4, noBooking)).Taken);
        Assert.Equal(Plane.Of(4, noBooking), path.Step(receiver).Steps[2].Changed);
        for (var row = 0; row < sites.Count; row++) Assert.Equal((0, 0, 2, 1), Inputs(row));

        (int?, int?, int?, int?) Inputs(int row) =>
            (delivery.Value(row, 0), delivery.Value(row, 1), delivery.Value(row, 2), delivery.Value(row, 3));
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void AQuietFactoredProductDoesNotQualifyForTheExactInverseRead(ExecutionConformancePath path)
    {
        foreach (var together in new[] { false, true })
        {
            foreach (var (target, expected) in new[] { (1, 1UL << 1), (9, 1UL << 3) })
            {
                var wide = DenseInverse(4, target, sameOperand: false);
                Assert.NotNull(path.Settle(wide.Engine, 8, together));
                Assert.Equal(expected, wide.Left.Values(0));
                Assert.Equal(expected, wide.Right.Values(0));

                var lattice = FactoredRungs(target);
                Assert.NotNull(path.Settle(lattice.Engine, 24, together));
                Assert.True((lattice.Left.Values(0) & expected) != 0);
                Assert.Equal(lattice.Left.Values(0), lattice.Right.Values(0));
            }

            var same = DenseInverse(3, 6, sameOperand: true);
            Assert.True(Assert.Single(path.Step(same.Engine, together).Steps).Unadmitted[0]);
            var distinct = DenseInverse(3, 6, sameOperand: false);
            Assert.NotNull(path.Settle(distinct.Engine, 8, together));
            Assert.Equal((1UL << 2) | (1UL << 3), distinct.Left.Values(0));
            Assert.Equal(distinct.Left.Values(0), distinct.Right.Values(0));

            var impossible = DenseInverse(3, 5, sameOperand: false);
            Assert.True(Assert.Single(path.Step(impossible.Engine, together).Steps).Unadmitted[0]);
            Assert.True(impossible.Left.Empty[0]);
            var factored = FactoredInverse(target: 5);
            Assert.NotNull(path.Settle(factored.Engine, 10, together));
            Assert.All(path.Step(factored.Engine, together).Steps, step =>
            {
                Assert.True(step.Quiet);
                Assert.True(step.Unadmitted.None);
            });
            Assert.True(factored.LeftHigh.Open[0]);
            Assert.True(factored.RightHigh.Open[0]);
        }
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void EachTickCanDeliverOnlyOnePacketAndTransportKeepsTheRemainingBook(ExecutionConformancePath path)
    {
        foreach (var together in new[] { false, true })
        {
            var (stockStore, stock) = Make.Counts(1, 8);
            stock.Write(stockStore, 0, 8);
            var supplier = new Engine().Hold(stockStore);
            var supply = stockStore.Publish([0], [.. stock.Planes]);
            var dockStore = new Store(0, 1);
            var debt = FieldOn(dockStore, Shape.Unary(4));
            var dock = FieldOn(dockStore, Shape.Unary(6));
            debt.Write(dockStore, 0, 4);
            var delivery = new Wiring(Flows.Fulfill(8, 4, 6, take: 2, give: 2),
                ReadPort(supply), debt.On(dockStore), dock.On(dockStore));
            delivery.Add(0, 0, 0);
            delivery.Record();
            var receiver = new Engine().Add(delivery);

            var first = path.Step(receiver, together);
            Assert.Equal(1L, first.Tick);
            Assert.Equal((6, 3, 2), PacketState(delivery));
            var captured = Transport.SaveLocals(supplier, receiver);
            var restored = Transport.LoadLocals(captured).Select(local => local.Engine).ToArray();
            var restoredDelivery = Assert.Single(restored[1].Wirings);
            Assert.Equal(captured, Transport.SaveLocals(restored));
            Assert.Equal(PacketState(delivery), PacketState(restoredDelivery));

            (int, int, int)[] outcomes = [(4, 2, 4), (2, 1, 6), (2, 1, 6), (2, 1, 6)];
            for (var remaining = 0; remaining < outcomes.Length; remaining++)
            {
                var next = path.Step(receiver, together);
                var loaded = path.Step(restored[1], together);
                Assert.Equal(next.Tick - 1, loaded.Tick);
                Assert.Equal(outcomes[remaining], PacketState(delivery));
                Assert.Equal(PacketState(delivery), PacketState(restoredDelivery));
                Assert.Equal(remaining == outcomes.Length - 1, Assert.Single(next.Steps).Quiet);
                Assert.Equal(Assert.Single(next.Steps).Quiet, Assert.Single(loaded.Steps).Quiet);
                Assert.Equal(Transport.SaveLocals(supplier, receiver), Transport.SaveLocals(restored));
            }
        }
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void OverlappingPatchesCheckTheirCalibrationAndCommitTheFreshGroupOnce(ExecutionConformancePath path)
    {
        const int datum = 0, joint = 1, tip = 2, carried = 3;
        var source = new Store(0, 4);
        var gain = FieldOn(source, Shape.Unary(3));
        var coordinate = FieldOn(source, Shape.Unary(9));
        var selected = Plane.Of(4, joint, tip);
        var basis = Plane.Of(4, datum);
        gain.Write(source, datum, 2);
        coordinate.Write(source, joint, 2);
        coordinate.Write(source, tip, 3);
        coordinate.Write(source, carried, 8);
        var scale = TransitionLaw.FromFunction([gain.Shape, coordinate.Shape, coordinate.Shape], state =>
        {
            var atJoint = 1 + state[0] * (state[1] - 1);
            var atTip = 1 + state[0] * (state[2] - 1);
            return atJoint is >= 0 and <= 9 && atTip is >= 0 and <= 9 ? [state[0], atJoint, atTip] : null;
        }, new Accounting(false, new Unchanged(0)));

        var staleCandidate = Preview();
        Assert.Equal((3, 5, 8), Positions(staleCandidate));
        gain.Write(source, datum, 3);
        var freshCandidate = Preview();
        Assert.Equal((4, 7, 8), Positions(freshCandidate));
        Assert.Equal((2, 3, 8), Positions(source));

        var refused = staleCandidate.Commit();
        Assert.False(refused.Committed);
        Assert.Equal(basis, refused.Stale);
        Assert.True(refused.Written.None);
        Assert.Equal((2, 3, 8), Positions(source));

        var split = freshCandidate.Commit(Plane.Of(4, joint));
        Assert.False(split.Committed);
        Assert.Equal(Plane.Of(4, tip), split.Split);
        Assert.Equal((2, 3, 8), Positions(source));
        Assert.Equal(selected, freshCandidate.Commit().Written);
        Assert.Equal((4, 7, 8), Positions(source));
        Assert.True(freshCandidate.Commit().Written.None);

        Workspace Preview()
        {
            var workspace = new Workspace(source).Select(selected);
            foreach (var plane in gain.Planes) workspace.Fix(plane, basis);
            var row = new Wiring(scale, gain.On(workspace), coordinate.On(workspace), coordinate.On(workspace));
            row.Add(datum, joint, tip);
            var preview = path.Preview(workspace, row);
            Assert.True(preview.Step.Unadmitted.None);
            Assert.True(preview.Beyond.None);
            return workspace;
        }

        (int?, int?, int?) Positions(Surface surface) =>
            (coordinate.Read(surface, joint), coordinate.Read(surface, tip), coordinate.Read(surface, carried));
    }

    private static (int?, int?, int?) PacketState(Wiring delivery) =>
        (delivery.Value(0, 0), delivery.Value(0, 1), delivery.Value(0, 2));

    private static (Engine Engine, Store Left, int Present, Store Pairs, int Occupied, Wiring Formed) PairWorld(
        ExecutionConformancePath path, int firstLeft)
    {
        const int present = 0, before = 1;
        var left = new Store(2, 2);
        var right = new Store(2, 1);
        var pairs = new Store(1, 2);
        var receipt = FieldOn(pairs, Shape.Binary(4));
        var occupancy = new Wiring(Settling.Justify(2), new Binding(left, present), new Binding(right, present), new Binding(pairs, present));
        var formation = TransitionLaw.FromFunction([Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit, receipt.Shape], state =>
            state[2] == 1 && state[3] == 1 && (state[0] == 0 || state[1] == 0)
                ? [.. state[..4], (1 - state[0]) + 2 * (1 - state[1])] : state,
            new Accounting(true, new Unchanged(0), new Unchanged(1), new Unchanged(2), new Unchanged(3)));
        var record = new Wiring(formation, new Binding(left, before), new Binding(right, before),
            new Binding(left, present), new Binding(right, present), receipt.On(pairs));
        for (var member = 0; member < left.Count; member++)
        {
            occupancy.Add(member, 0, member);
            record.Add(member, 0, member, 0, member);
        }
        var engine = new Engine().Add(occupancy).Add(record);
        foreach (var source in new[] { left, right })
        {
            var capture = new Wiring(Settling.Justify(1), new Binding(source, present), new Binding(source, before));
            for (var position = 0; position < source.Count; position++) capture.Add(position, position);
            engine.Add(capture);
        }
        left.Write(present, firstLeft, true);
        path.Step(engine);
        left.Write(present, 1 - firstLeft, true);
        right.Write(present, 0, true);
        path.Step(engine);
        return (engine, left, present, pairs, present, record);
    }

    private static (Engine Engine, Supports Left, Supports Right) DenseInverse(int capacity, int target, bool sameOperand)
    {
        var world = new Store(0, 1);
        var product = Operations.Product(capacity, capacity);
        var left = Supports.On(world, FieldOn(world, product.Roles[0]));
        var right = sameOperand ? left : Supports.On(world, FieldOn(world, product.Roles[1]));
        var result = Supports.On(world, FieldOn(world, product.Roles[2]));
        result.Restrict(0, 1UL << target);
        return (new Engine().Add(Constraint(product, left, right, result)), left, right);
    }

    private static (Engine Engine, Supports LeftHigh, Supports RightHigh) FactoredInverse(int target)
    {
        var world = new Store(0, 1);
        Supports Bit() => Supports.On(world, FieldOn(world, Shape.Bit));
        var leftLow = Bit(); var leftHigh = Bit(); var rightLow = Bit(); var rightHigh = Bit();
        var low = Bit(); var middle = Bit(); var high = Bit(); var top = Bit();
        var lowHigh = Bit(); var highLow = Bit(); var highHigh = Bit(); var carry = Bit();
        Supports[] output = [low, middle, high, top];
        for (var bit = 0; bit < output.Length; bit++) output[bit].Restrict(0, 1UL << ((target >> bit) & 1));

        var product = Operations.Product(1, 1);
        var add = Operations.Balance([Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit], 1, 1, -1, -2);
        var engine = new Engine()
            .Add(Constraint(product, leftLow, rightLow, low))
            .Add(Constraint(product, leftLow, rightHigh, lowHigh))
            .Add(Constraint(product, leftHigh, rightLow, highLow))
            .Add(Constraint(product, leftHigh, rightHigh, highHigh))
            .Add(Constraint(add, lowHigh, highLow, middle, carry))
            .Add(Constraint(add, highHigh, carry, high, top));
        return (engine, leftHigh, rightHigh);
    }

    private static (Engine Engine, Supports Left, Supports Right) FactoredRungs(int target)
    {
        var world = new Store(0, 1);
        Ladder left = new(world, 2, 3), right = new(world, 2, 3), product = new(world, 2, 6);
        var domain = Shape.Binary(5);
        var word = Operations.Balance([Shape.Unary(1), Shape.Unary(1), Shape.Unary(1), domain], 1, 2, 4, -1);
        var leftWord = Supports.On(world, FieldOn(world, domain));
        var rightWord = Supports.On(world, FieldOn(world, domain));
        var engine = new Engine()
            .Add(Constraint(word, [.. left.Possible, leftWord]))
            .Add(Constraint(word, [.. right.Possible, rightWord]));
        foreach (var column in Ladder.Multiply(left, right, product, Here)) engine.Add(column);
        product.Restrict(0, target);
        return (engine, leftWord, rightWord);
    }

    private static Wiring Constraint(RelationLaw law, params Supports[] support)
    {
        var row = new Wiring(law, [.. support.Select(value => value.Variable)]);
        row.Add(new int[support.Length]);
        return row;
    }

    private static Field FieldOn(Store store, Shape shape) =>
        new(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]);

    private static Binding ReadPort(Port port) => new(port, [.. Enumerable.Range(0, port.PlaneCount)]);
}
