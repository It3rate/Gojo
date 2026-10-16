using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class ConnectedReflexTests
{
    [Fact]
    public void AMeasuredRefinementFeedsTheSameScheduledConditionalWithoutChangingTheOriginalState()
    {
        var h = new Harness(); var f = h.Reflex;
        var priorState = f.Stock.Initial(f.Agent, h.M0, f.Stock.StateDomain);
        var prior = Mode(h, priorState);
        var measured = new MeasuredStructuralSupplier(f.Stock, new StructuralLandmarkFixture(), new Carrier());
        var numeric = FiniteReadSurface.Publish(new Carrier(), measured.Capacity,
            new CarrierLocation(measured.Axis, 10), new Placeholder());
        var offered = measured.ObserveState(measured.Envelope(f.Agent, h.M0, numeric));
        Assert.True(QualitativePublication.TryRefine(prior, offered, out var refined, out var refinement));
        var modeState = h.Mode.Initial(h.R0); var lightState = h.Sensor.Initial(h.R0);
        var mode = Deliver(h.Mode, ref modeState, refined, h.M0, h.R1);
        var light = Deliver(h.Sensor, ref lightState, h.Light(h.S0, h.FineLow), h.S0, h.R1);
        AssertCommand(f, h, mode, light, f.MoveToB);
        var read = f.ReadConnected(h.Mode, mode, h.Sensor, light);
        Assert.True(Contains(read, measured.Calibration)); Assert.True(Contains(read, numeric));
        Assert.True(Contains(read, priorState)); Assert.True(Contains(refinement, offered));
        Assert.True(f.Stock.TryReadState(priorState, out _, out _, out var oldSupport));
        Assert.Same(f.Stock.StateDomain, oldSupport); Assert.Empty(priorState.Carriers());
        Assert.NotEmpty(read.Carriers());
    }

    [Fact]
    public void DifferentResolutionsAndIndependentSchedulesComposeWithoutInventingNewSamples()
    {
        var h = new Harness(); var f = h.Reflex;
        var initial = f.Stock.Initial(f.Agent, h.M0, StructuralConstraint.Closed(f.Stock.Empty));
        var half = Pulse(f, initial, h.Mid, f.Stock.Charge);
        var full = Pulse(f, half, h.M1, f.Stock.Charge);
        var memory0 = Mode(h, initial); var memory1 = Mode(h, full);
        var light0 = h.Light(h.S0, h.FineLow);
        var light1 = h.Light(h.S1, h.FineHigh);
        var modeState = h.Mode.Initial(h.R0); var lightState = h.Sensor.Initial(h.R0);
        var modeAt1 = Deliver(h.Mode, ref modeState, memory0, h.M0, h.R1);
        var lightAt1 = Deliver(h.Sensor, ref lightState, light0, h.S0, h.R1);
        AssertCommand(f, h, modeAt1, lightAt1, f.MoveToB);
        var modeAt2 = Deliver(h.Mode, ref modeState, memory0, h.M0, h.R2);
        var lightAt2 = Deliver(h.Sensor, ref lightState, light1, h.S1, h.R2);
        AssertCommand(f, h, modeAt2, lightAt2, f.MoveToA);
        var modeAt3 = Deliver(h.Mode, ref modeState, memory1, h.M1, h.R3);
        var lightAt3 = Deliver(h.Sensor, ref lightState, light1, h.S1, h.R3);
        AssertCommand(f, h, modeAt3, lightAt3, f.MoveToB);
        var read = f.ReadConnected(h.Mode, modeAt3, h.Sensor, lightAt3);
        Assert.True(Contains(read, initial)); Assert.True(Contains(read, full));
        Assert.True(Contains(read, h.Map.Surface)); Assert.True(Contains(read, light1));
        Assert.True(h.Sensor.TryReadAccepted(lightAt3, out var held, out var producer, out var receiver));
        Assert.Same(light1, held); Assert.Same(h.S1, producer); Assert.Same(h.R3, receiver);
        Assert.True(QualitativePublication.TryRead(light1, out _, out _, out var sample, out _, out _, out _, out _));
        Assert.Same(h.S1, sample); // The later receiver event did not become a new sample.
        Assert.Empty(read.Carriers()); Assert.Empty(read.CarrierLocations());
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)]
    public void ScheduleAuthorityCannotRepairAWrongPublicationContract(int changed)
    {
        var h = new Harness(); var f = h.Reflex;
        var state = f.Stock.Initial(f.Agent, h.M0, StructuralConstraint.Closed(f.Stock.Empty));
        var memory = Mode(h, state);
        var light = QualitativePublication.Create(changed == 0 ? new Placeholder() : f.LightSource,
            changed == 1 ? new Relation(f.EyeB, f.EyeA) : f.EyePair,
            changed == 2 ? new Placeholder() : h.S0,
            changed == 3 ? new Placeholder() : f.LightFrame.Surface,
            changed == 4 ? StructuralConstraint.Closed(f.LightFrame.Lower, f.LightFrame.Equal, f.LightFrame.Higher)
                : f.LightFrame.Domain,
            changed == 5 ? StructuralConstraint.Open(f.LightFrame.Lower) : StructuralConstraint.Closed(f.LightFrame.Lower));
        var modeState = h.Mode.Initial(h.R0); var lightState = h.Sensor.Initial(h.R0);
        var modes = Deliver(h.Mode, ref modeState, memory, h.M0, h.R1);
        var lights = Deliver(h.Sensor, ref lightState, light, h.S0, h.R1);
        if (changed == 6)
            lights = Deliver(h.Sensor, ref lightState, light, h.S0, h.R2);
        var read = f.ReadConnected(h.Mode, modes, h.Sensor, lights);
        Assert.False(f.TryReadCommand(read, out _));
        Assert.True(FiniteLaw.TryReadResult(read, out _, out var pending, out _));
        Assert.NotEmpty(FiniteLaw.Entries(pending)); Assert.True(Contains(read, light));
    }

    [Fact]
    public void AnExpiredObservationDoesNotBecomeFreshByMatchingItsCategory()
    {
        var h = new Harness(); var f = h.Reflex;
        var memory = Mode(h, f.Stock.Initial(f.Agent, h.M0, StructuralConstraint.Closed(f.Stock.Empty)));
        var light = h.Light(h.S0, h.FineLow);
        var modeState = h.Mode.Initial(h.R0); var lightState = h.Sensor.Initial(h.R0);
        Deliver(h.Mode, ref modeState, memory, h.M0, h.R1);
        Deliver(h.Sensor, ref lightState, light, h.S0, h.R1);
        Deliver(h.Mode, ref modeState, memory, h.M0, h.R2);
        Deliver(h.Sensor, ref lightState, light, h.S0, h.R2);
        var lateMode = h.Mode.Deliver(modeState, memory, h.M0, h.R3); // Its validity window ended.
        var lateLight = h.Sensor.Deliver(lightState, light, h.S0, h.R3);
        Assert.False(f.TryReadCommand(f.ReadConnected(h.Mode, lateMode, h.Sensor, lateLight), out _));
        Assert.True(h.Mode.TryReadDelivery(lateMode, out _, out var after, out _, out var blocked, out _));
        Assert.Same(modeState, after); Assert.NotEmpty(FiniteLaw.Entries(blocked));
    }

    [Fact]
    public void AForeignConnectionWithTheSameEventsCannotSupplyAnEndpoint()
    {
        var h = new Harness(); var f = h.Reflex;
        var memory = Mode(h, f.Stock.Initial(f.Agent, h.M0, StructuralConstraint.Closed(f.Stock.Empty)));
        var modeState = h.Mode.Initial(h.R0);
        var modes = Deliver(h.Mode, ref modeState, memory, h.M0, h.R1);
        var wrong = new EventScheduleConnection(f.LightSource, f.MotorPort,
            h.Sensor.ProducerSchedule, h.Sensor.ReceiverSchedule, h.Sensor.Windows);
        var delivered = wrong.Deliver(wrong.Initial(h.R0), h.Light(h.S0, h.FineLow), h.S0, h.R1);
        Assert.True(wrong.TryReadAccepted(delivered, out _, out _, out _));
        Assert.False(f.TryReadCommand(f.ReadConnected(h.Mode, modes, wrong, delivered), out _));
        Assert.False(f.TryReadCommand(f.ReadConnected(h.Mode, modes, h.Sensor, delivered), out _));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void ADisconnectedNativePortCannotKeepAuthorityThroughItsHelperReference(int selected)
    {
        var h = new Harness(); var f = h.Reflex;
        var memory = Mode(h, f.Stock.Initial(f.Agent, h.M0, StructuralConstraint.Closed(f.Stock.Empty)));
        var modeState = h.Mode.Initial(h.R0); var lightState = h.Sensor.Initial(h.R0);
        var modes = Deliver(h.Mode, ref modeState, memory, h.M0, h.R1);
        var lights = Deliver(h.Sensor, ref lightState, h.Light(h.S0, h.FineLow), h.S0, h.R1);
        var earlier = f.ReadConnected(h.Mode, modes, h.Sensor, lights);
        Assert.True(f.TryReadCommand(earlier, out _));
        // Inject an invalid native graph to exercise the reader boundary. Normal
        // public construction never mutates these relation endpoints directly.
        var port = new[] { f.ModePort, f.SensorPort, f.MotorPort, f.Surface }[selected];
        typeof(Relation).GetProperty(nameof(Relation.Right))!.SetValue(port, new Placeholder());
        Assert.False(f.TryReadCommand(f.ReadConnected(h.Mode, modes, h.Sensor, lights), out _));
        Assert.False(f.TryReadCommand(earlier, out _));
    }

    private static void AssertCommand(QualitativeReflexFixture f, Harness h,
        Relation mode, Relation light, Relation expected)
    {
        foreach (var compiled in new[] { false, true })
        {
            var read = f.ReadConnected(h.Mode, mode, h.Sensor, light, compiled);
            Assert.True(f.TryReadCommand(read, out var command)); Assert.Same(expected, command);
        }
    }

    private static Relation Deliver(EventScheduleConnection connection, ref Relation state,
        Relation publication, IElement producer, IElement receiver)
    {
        var receipt = connection.Deliver(state, publication, producer, receiver);
        Assert.True(connection.TryReadAccepted(receipt, out var accepted, out _, out _));
        Assert.Same(publication, accepted);
        Assert.True(connection.TryReadDelivery(receipt, out _, out var after, out _, out _, out _));
        state = after; return receipt;
    }

    private static Relation Mode(Harness h, Relation state)
    {
        Assert.True(h.Reflex.Stock.TryReadState(state, out var source, out var eventIdentity, out var support));
        return QualitativePublication.Create(source, h.Reflex.Stock.Receiver, eventIdentity,
            h.Reflex.Stock.Surface, h.Reflex.Stock.StateDomain, support, state);
    }

    private static Relation Pulse(QualitativeReflexFixture f, Relation state, IElement eventIdentity, IElement input)
    {
        Assert.True(f.Stock.TryReadState(state, out var source, out var previous, out _));
        var step = f.Stock.Step(state, f.Stock.Offer(source, eventIdentity, previous, StructuralConstraint.Closed(input)));
        Assert.True(f.Stock.TryReadStep(step, out _, out _, out var after, out _, out var pending));
        Assert.Empty(FiniteLaw.Entries(pending)); return after;
    }

    private sealed class Harness
    {
        public QualitativeReflexFixture Reflex { get; } = new();
        public IElement M0 { get; } = new Placeholder();
        public IElement Mid { get; } = new Placeholder();
        public IElement M1 { get; } = new Placeholder();
        public IElement S0 { get; } = new Placeholder();
        public IElement S1 { get; } = new Placeholder();
        public IElement R0 { get; } = new Placeholder();
        public IElement R1 { get; } = new Placeholder();
        public IElement R2 { get; } = new Placeholder();
        public IElement R3 { get; } = new Placeholder();
        public IElement FineLow { get; } = new Placeholder();
        public IElement FineHigh { get; } = new Placeholder();
        public IElement FineBasis { get; }
        public Relation FineDomain { get; }
        public ResolutionCorrespondence Map { get; }
        public EventScheduleConnection Mode { get; }
        public EventScheduleConnection Sensor { get; }
        public Harness()
        {
            var lowNear = new Placeholder(); var highNear = new Placeholder(); var equal = new Placeholder();
            FineBasis = new Relation(new Relation(FineLow, lowNear),
                new Relation(new Relation(lowNear, equal), new Relation(new Relation(equal, highNear),
                    new Relation(highNear, FineHigh))));
            FineDomain = StructuralConstraint.Closed(FineLow, lowNear, equal, highNear, FineHigh);
            Map = ResolutionCorrespondence.FromRows(FineBasis, Reflex.LightFrame.Surface,
                FineDomain, Reflex.LightFrame.Domain,
                new Relation(FineLow, Reflex.LightFrame.Lower), new Relation(lowNear, Reflex.LightFrame.Lower),
                new Relation(equal, Reflex.LightFrame.Equal), new Relation(highNear, Reflex.LightFrame.Higher),
                new Relation(FineHigh, Reflex.LightFrame.Higher));
            var receiving = StructuralConstraint.Closed(new Relation(R0, R1), new Relation(R1, R2), new Relation(R2, R3));
            Mode = new EventScheduleConnection(Reflex.Agent, Reflex.ModePort,
                StructuralConstraint.Closed(new Relation(M0, Mid), new Relation(Mid, M1)), receiving,
                StructuralConstraint.Closed(EventScheduleConnection.Window(M0, R1, R2),
                    EventScheduleConnection.Window(M1, R3)));
            Sensor = new EventScheduleConnection(Reflex.LightSource, Reflex.SensorPort,
                StructuralConstraint.Closed(new Relation(S0, S1)), receiving,
                StructuralConstraint.Closed(EventScheduleConnection.Window(S0, R1, R2),
                    EventScheduleConnection.Window(S1, R2, R3)));
        }
        public Relation Light(IElement eventIdentity, IElement reading)
        {
            var fine = QualitativePublication.Create(Reflex.LightSource, Reflex.EyePair, eventIdentity,
                FineBasis, FineDomain, StructuralConstraint.Closed(reading));
            Assert.True(QualitativePublication.TryForward(Map, fine, out var coarse, out _)); return coarse;
        }
    }

    private static bool Contains(IElement root, IElement target)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var e))
        {
            if (!seen.Add(e)) continue;
            if (ReferenceEquals(e, target)) return true;
            if (e is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
        return false;
    }
}
