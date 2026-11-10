using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Ports, reading, moving, branching, merging and calibrators.</summary>
public sealed class FlowTests
{
    /// <summary>Reads a unary count through a port and sets the reader's own
    /// bit when it is not empty. The count is never written: a reference.</summary>
    private static readonly TransitionLaw SeesAny = TransitionLaw.FromFunction(
        [Shape.Unary(3), Shape.Bit],
        s => [s[0], s[0] > 0 ? 1 : 0],
        new Accounting(true, new Unchanged(0)));

    [Fact]
    public void APortIsLanesAndPlanesAndItsOwnerCanMoveWhatALaneShows()
    {
        var (sender, count) = Stores.Store(4, 3);
        var (receiver, received) = Stores.Store(1, 3);
        count.Write(sender, 2, 3);
        var port = sender.Publish([2], [0, 1, 2]);

        Assert.Equal(1, port.Count);
        Assert.Equal(3, port.PlaneCount);

        var move = Stores.Wire(Flows.Move(3, 3), port, receiver);
        move.Apply();
        Assert.Equal((2, 1), (count.Read(sender, 2), received.Read(receiver, 0)));

        count.Write(sender, 3, 1);
        sender.Rebind(port, 0, 3);
        move.Apply();
        Assert.Equal((2, 0, 2), (count.Read(sender, 2), count.Read(sender, 3), received.Read(receiver, 0)));
    }

    [Fact]
    public void ReadingThroughAPortLeavesTheSenderAsItIsAndAnyNumberCanRead()
    {
        var (sender, count) = Stores.Store(1, 3);
        count.Write(sender, 0, 2);
        var port = sender.Publish([0], [0, 1, 2]);
        var readers = Enumerable.Range(0, 3).Select(_ => new Structure(1, 1)).ToArray();
        var engine = new Engine();
        foreach (var reader in readers)
        {
            var wiring = new Wiring(SeesAny, new Binding(port, 0, 1, 2), new Binding(reader, 0));
            wiring.Add(0, 0);
            engine.Add(wiring);
        }

        var step = engine.Step();

        Assert.All(readers, reader => Assert.True(reader.Read(0, 0)));
        Assert.Equal(2, count.Read(sender, 0));
        Assert.Single(step.Waves);
    }

    [Fact]
    public void WritingThroughAPortIsAllowedOnlyAsAMove()
    {
        var (sender, _) = Stores.Store(1, 3);
        var (receiver, _) = Stores.Store(1, 3);
        var port = sender.Publish([0], [0, 1, 2]);
        var destroys = TransitionLaw.FromFunction(
            [Shape.Unary(3), Shape.Unary(3)],
            s => s[0] > 0 ? [s[0] - 1, s[1]] : s,
            new Accounting(true, new NeverIncreases(0), new Unchanged(1)));

        Assert.Throws<ArgumentException>(() =>
            new Wiring(destroys, new Binding(port, 0, 1, 2), new Binding(receiver, 0, 1, 2)));

        // On its own positions a structure may do as it likes.
        _ = new Wiring(destroys, new Binding(sender, 0, 1, 2), new Binding(receiver, 0, 1, 2));
    }

    /// <summary>A move between two lanes of one port, then the owner points the
    /// second lane at the first lane's position. The law would see one count as
    /// two and add a unit to it. The row is reported as aliased and does not
    /// run; it runs again once the lane moves away.</summary>
    [Fact]
    public void ALaneMovedOntoAnotherRolesBitStopsTheRowInsteadOfMakingAUnit()
    {
        var (owner, count) = Stores.Store(3, 3);
        count.Write(owner, 0, 1);
        var port = owner.Publish([0, 1], [0, 1, 2]);
        var move = new Wiring(Flows.Move(3, 3), new Binding(port, 0, 1, 2), new Binding(port, 0, 1, 2));
        move.Add(0, 1);

        owner.Rebind(port, 1, 0);
        var stopped = move.Apply();

        Assert.Equal(Plane.Of(1, 0), stopped.Aliased);
        Assert.True(stopped.Changed.None);
        Assert.Equal(1, count.Read(owner, 0));

        owner.Rebind(port, 1, 2);
        var runs = move.Apply();

        Assert.True(runs.Aliased.None);
        Assert.Equal((0, 1), (count.Read(owner, 0), count.Read(owner, 2)));
    }

    /// <summary>A preserved sum is conservation only when it adds up positive
    /// amounts. With a zero weight, a role can change with nothing paying for
    /// it; with a negative one, two roles can grow together. Neither may write
    /// through a port, though both are still promises a law can keep.</summary>
    [Fact]
    public void OnlyASumOfPositiveAmountsLetsALawWriteThroughAPort()
    {
        var (sender, _) = Stores.Store(1, 1);
        var (receiver, _) = Stores.Store(1, 1);
        var port = sender.Publish([0], [0]);
        var fills = TransitionLaw.FromFunction(
            [Shape.Bit, Shape.Bit], s => [s[0], 1], new Accounting(true, new PreservedSum([1, 0], [0, 1])));
        var growTogether = TransitionLaw.FromFunction(
            [Shape.Bit, Shape.Bit], s => s is [0, 0] ? [1, 1] : s, new Accounting(true, new PreservedSum([0, 1], [1, -1])));

        Assert.False(fills.Accounting.Conserves(1));
        Assert.Throws<ArgumentException>(() => new Wiring(fills, new Binding(receiver, 0), new Binding(port, 0)));
        Assert.Throws<ArgumentException>(() => new Wiring(growTogether, new Binding(port, 0), new Binding(receiver, 0)));
        Assert.True(Flows.Convert(2, 1, 3, 3).Accounting.Conserves(0));
    }

    [Fact]
    public void UnitsMoveOneToOneUncalibratedAndWhatDoesNotFitStays()
    {
        var (light, emitted) = Stores.Store(1, 3);
        var (body, heat) = Stores.Store(1, 2);
        emitted.Write(light, 0, 3);
        var port = light.Publish([0], [0, 1, 2]);
        var engine = new Engine().Add(Stores.Wire(Flows.Move(3, 2), port, body));

        engine.Run(5);

        Assert.Equal(1, emitted.Read(light, 0));
        Assert.Equal(2, heat.Read(body, 0));
    }

    [Fact]
    public void TwoReceiversCanTakeTheSameFlowInTheirOwnTerms()
    {
        var (light, emitted) = Stores.Store(1, 3);
        emitted.Write(light, 0, 2);
        var port = light.Publish([0], [0, 1, 2]);
        var (body, heat) = Stores.Store(1, 3);
        var eye = new Structure(1, 1);
        var sees = new Wiring(SeesAny, new Binding(port, 0, 1, 2), new Binding(eye, 0));
        sees.Add(0, 0);
        var engine = new Engine().Add(sees).Add(Stores.Wire(Flows.Move(3, 3), port, body));

        var step = engine.Step();

        Assert.True(eye.Read(0, 0));
        Assert.Equal((1, 1), (emitted.Read(light, 0), heat.Read(body, 0)));
        Assert.Equal(2, step.Waves.Count);
    }

    [Fact]
    public void TwoTakersCannotBothHaveOneUnit()
    {
        var (sender, count) = Stores.Store(1, 1);
        count.Write(sender, 0, 1);
        var port = sender.Publish([0], [0]);
        var (first, a) = Stores.Store(1, 1);
        var (second, b) = Stores.Store(1, 1);
        var engine = new Engine()
            .Add(Stores.Wire(Flows.Move(1, 1), port, first))
            .Add(Stores.Wire(Flows.Move(1, 1), port, second));

        var step = engine.Step();

        Assert.Equal(2, step.Waves.Count);
        Assert.Equal((0, 1, 0), (count.Read(sender, 0), a.Read(first, 0), b.Read(second, 0)));
    }

    [Fact]
    public void BranchesAndMergesAreRowsOfOneWiringInDeclaredOrder()
    {
        // 0 source, 1 gate A, 2 gate B, 3 target A, 4 target B.
        var net = new Structure(2, 5);
        var count = new Field(Shape.Unary(2), 0, 1);
        count.Write(net, 0, 1);
        net.Write(0, 2, true);
        var branch = new Wiring(Flows.GatedMove(2, 2), new Binding(net, 0, 1), new Binding(net, 0), new Binding(net, 0, 1));
        branch.Add(0, 1, 3);
        branch.Add(0, 2, 4);

        var step = branch.Apply();

        Assert.Equal(2, step.Waves.Count);
        Assert.Equal((0, 0, 1), (count.Read(net, 0), count.Read(net, 3), count.Read(net, 4)));

        var pool = new Structure(2, 3);
        count.Write(pool, 0, 1);
        count.Write(pool, 1, 1);
        var merge = new Wiring(Flows.Move(2, 2), new Binding(pool, 0, 1), new Binding(pool, 0, 1));
        merge.Add(0, 2);
        merge.Add(1, 2);

        merge.Apply();

        Assert.Equal((0, 0, 2), (count.Read(pool, 0), count.Read(pool, 1), count.Read(pool, 2)));
    }

    [Fact]
    public void AStructureSwitchesItsOwnFlowWithItsOwnBits()
    {
        // 0 source, 1 gate A, 2 gate B, 3 target A, 4 target B. The gates toggle every step.
        var net = new Structure(2, 5);
        var count = new Field(Shape.Unary(2), 0, 1);
        count.Write(net, 0, 2);
        net.Write(0, 1, true);
        var gates = new Wiring(Flows.Toggle, new Binding(net, 0));
        gates.Add(1);
        gates.Add(2);
        var branch = new Wiring(Flows.GatedMove(2, 2), new Binding(net, 0, 1), new Binding(net, 0), new Binding(net, 0, 1));
        branch.Add(0, 1, 3);
        branch.Add(0, 2, 4);
        var engine = new Engine().Add(gates).Add(branch);

        engine.Step();
        Assert.Equal((0, 1), (count.Read(net, 3), count.Read(net, 4)));
        engine.Step();
        Assert.Equal((1, 1), (count.Read(net, 3), count.Read(net, 4)));
        Assert.Equal(0, count.Read(net, 0));
    }

    [Fact]
    public void ACalibratorIsJustASmallStructureWithAnInputAndAnOutput()
    {
        var oscillator = new Structure(1, 1);
        var clock = new Wiring(Flows.Toggle, new Binding(oscillator, 0));
        clock.Add(0);
        var signal = oscillator.Publish([0], [0]);

        // Reads the signal by reference, keeps the last input, and counts rising edges.
        var edges = TransitionLaw.FromFunction(
            [Shape.Bit, Shape.Bit, Shape.Unary(8)],
            s => [s[0], s[0], s[0] == 1 && s[1] == 0 && s[2] < 8 ? s[2] + 1 : s[2]],
            new Accounting(true, new Unchanged(0), new NeverDecreases(2)));
        var calibrator = new Structure(9, 1);
        var counter = new Wiring(edges, new Binding(signal, 0), new Binding(calibrator, 0), new Binding(calibrator, Enumerable.Range(1, 8).ToArray()));
        counter.Add(0, 0, 0);

        // Another reader takes the same signal as it is: no calibration needed.
        var raw = new Structure(1, 1);
        var copy = TransitionLaw.FromFunction([Shape.Bit, Shape.Bit], s => [s[0], s[0]], new Accounting(true, new Unchanged(0)));
        var mirror = new Wiring(copy, new Binding(signal, 0), new Binding(raw, 0));
        mirror.Add(0, 0);

        var engine = new Engine().Add(clock).Add(counter).Add(mirror);
        var waves = engine.Waves();
        engine.Run(8);

        var frequency = new Field(Shape.Unary(8), Enumerable.Range(1, 8).ToArray());
        Assert.Equal(4, frequency.Read(calibrator, 0));
        Assert.False(raw.Read(0, 0));
        Assert.Equal(new[] { new[] { 0 }, new[] { 1, 2 } }, waves);
    }

    [Fact]
    public void ACalibrationCanBeWiredButIsNeverRequired()
    {
        var (light, emitted) = Stores.Store(1, 6);
        var (body, heat) = Stores.Store(1, 4);
        emitted.Write(light, 0, 5);
        var port = light.Publish([0], [0, 1, 2, 3, 4, 5]);
        var engine = new Engine().Add(Stores.Wire(Flows.Convert(2, 1, 6, 4), port, body));

        engine.Run(4);

        Assert.Equal(1, emitted.Read(light, 0));
        Assert.Equal(2, heat.Read(body, 0));
    }

    [Fact]
    public void ARetiredPositionStopsTheRowsThatTouchIt()
    {
        var (source, count) = Stores.Store(1, 2);
        var (target, _) = Stores.Store(1, 2);
        count.Write(source, 0, 2);
        var move = Stores.Wire(Flows.Move(2, 2), source, target);
        target.Retire(0);

        var step = move.Apply();

        Assert.True(step.Skipped[0]);
        Assert.Equal(2, count.Read(source, 0));
    }
}
