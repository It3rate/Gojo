using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using C = BreakDrivenPrediction.Continuation;
using N = BreakDrivenPrediction.NativeCircuit;

namespace BreakDrivenPrediction;

/// <summary>
/// A selected local event read over the producer's own observations. Its public
/// packet exposes a resolved bit or an open reading, never its private sensor,
/// retained differences, detector circuit or consumer-specific interpretation.
/// </summary>
public static class EventSource
{
    // Root: sensor, private arrival clock, previous-delta site, public port,
    // private-to-public clock maps, current packet, latest private read witness.
    // Port: unit event frame, source-support axis, publication-sequence axis.
    // Packet: port, sequence, source-support span, bit/open, exposed residue.
    public static Relation Create(long initial)
    {
        var sensor = new Carrier();
        var clock = new Carrier();
        var admitted = Carrier.ReceiveTogether(C.G(new CarrierLocation(sensor, initial)));
        var previousDelta = new CarrierLocation(sensor, 0);
        var eventAxis = new Carrier();
        var sourceAxis = new Carrier();
        var sequenceAxis = new Carrier();
        var port = C.G(Relation.Segment(eventAxis, 0, 1), sourceAxis, sequenceAxis);
        var maps = C.G(Equation.Affine(clock, 0, 1, sourceAxis, 0, 1),
            Equation.Affine(clock, 0, 1, sequenceAxis, 0, 1));
        var open = new Placeholder();
        var packet = C.G(port, new CarrierLocation(sequenceAxis, 0),
            Relation.Segment(sourceAxis, 0, 0), open, C.G(open));
        var initialRead = ReadWitness.Create(new Carrier(), new CarrierLocation(sensor, initial),
            maps, packet, open, admitted);
        return C.G(sensor, clock, previousDelta, port, maps, packet, initialRead);
    }

    public static Carrier Sensor(Relation root) => (Carrier)C.Parts(root)[0];
    public static Carrier Clock(Relation root) => (Carrier)C.Parts(root)[1];
    public static Relation Port(Relation root) => (Relation)C.Parts(root)[3];
    public static Relation Packet(Relation root) => (Relation)C.Parts(root)[5];

    /// <summary>
    /// Admits one whole observation after all native reads are representable.
    /// Two differences are required for a resolved event read. Zero differences
    /// are known samples and do not satisfy this strict adjacent-reversal read.
    /// Preflight failure leaves the producer unchanged and returns its previous
    /// packet; it is not a new emission. This fixture owns free private input
    /// carriers; externally attaching settlement feedback is outside its contract.
    /// Calls are sequential admissions, not rereads.
    /// </summary>
    public static bool TryAdvance(Relation root, CarrierLocation input,
        out Relation next, out Relation packet)
    {
        next = root;
        packet = C.Parts(root) is { Length: 7 } fields && fields[5] is Relation previous
            ? previous : C.G();
        if (!TryRoot(root, out var p, out var portFields, out var maps) ||
            input.Tick is null || !ReferenceEquals(input.Carrier, p[0])) return false;

        try
        {
            var sensor = (Carrier)p[0];
            var clock = (Carrier)p[1];
            var previousDelta = (CarrierLocation)p[2];
            var port = (Relation)p[3];
            var sourceAxis = (Carrier)portFields[1];
            var sequenceAxis = (Carrier)portFields[2];
            var observed = C.Copy(input);
            var earlierDelta = C.Copy(previousDelta);
            var now = C.Copy(clock.ValueSlot);

            // Every operand retained as historical evidence is copied before
            // admission; these circuits do not read a later sensor value.
            var levelCircuit = N.Reframe(N.Sum((C.Copy(sensor.ValueSlot), 1),
                (C.Copy(sensor.LiveTensionSlot), 1)), sensor);
            if (!TryCalculate(levelCircuit, out var priorLevel, out var levelRead)) return false;
            var deltaCircuit = N.Reframe(N.Sum((observed, 1), (priorLevel, -1)), sensor);
            if (!TryCalculate(deltaCircuit, out var delta, out var deltaRead)) return false;
            var nextClockCircuit = N.Reframe(N.Sum((now, 1), (N.Constant(1), 1)), clock);
            if (!TryCalculate(nextClockCircuit, out var due, out var clockRead)) return false;

            var warmup = OperationClaim.AtLeast(new Carrier(), due,
                new CarrierLocation(clock, 2), new Carrier());
            if (!OperationClaim.TryReadResolvedViolation(warmup, out var stillNeeded)) return false;
            var ready = stillNeeded.Ticks == 0;
            IElement priorOperand = ready ? earlierDelta : new Placeholder();
            var productCircuit = N.Product(priorOperand, delta);
            var productRead = N.Read(productCircuit);
            IElement reversal = new Placeholder();
            IElement reading = new Placeholder();
            if (ready)
            {
                if (!Equation.TryReadPrediction(productRead, out var product, out _)) return false;
                var claim = OperationClaim.AtMost(new Carrier(), product,
                    new CarrierLocation(product.Carrier, -1), new Carrier());
                if (!OperationClaim.TryReadResolvedViolation(claim, out var violation)) return false;
                reversal = claim;
                // The supplied binary exposure selects a native endpoint of
                // the public unit frame after its private claim resolves.
                var eventFrame = (Relation)portFields[0];
                var endpoint = (Relation)(violation.Ticks == 0 ? eventFrame.Right : eventFrame.Left);
                reading = C.Copy((CarrierLocation)endpoint.Left);
            }

            var supportStartCircuit = ready
                ? N.Reframe(N.Sum((due, 1), (N.Constant(2), -1)), clock)
                : N.Reframe(new CarrierLocation(clock, 0), clock);
            if (!TryCalculate(supportStartCircuit, out var supportStart, out var supportRead) ||
                !TryMap((Relation)maps[0], supportStart, sourceAxis, out var start, out var startRead) ||
                !TryMap((Relation)maps[0], due, sourceAxis, out var end, out var endRead) ||
                !TryMap((Relation)maps[1], due, sequenceAxis, out var sequence, out var sequenceRead))
                return false;

            // Prepare the structural-memory response before any physical input.
            // Its target is the one exposed retained coordinate, not carrier value.
            var memoryMap = N.Reframe(delta, previousDelta.Carrier);
            if (!TryCalculate(memoryMap, out var memoryValue, out var memoryRead) ||
                !ReferenceEquals(memoryValue.Carrier, previousDelta.Carrier)) return false;
            var memoryResponse = C.G(earlierDelta, new Relation(memoryMap, previousDelta),
                memoryRead, C.Copy(memoryValue));

            var support = new Relation(new Relation(C.Copy(start), sourceAxis),
                new Relation(C.Copy(end), sourceAxis));
            // No private read/circuit goes into the public residue. The open
            // reading is itself the only missing participation exposed here.
            var exposedResidue = ready ? C.G() : C.G(reading);
            var emitted = C.G(port, C.Copy(sequence), support, reading, exposedResidue);
            var reads = C.G(new Relation(levelCircuit, levelRead),
                new Relation(deltaCircuit, deltaRead), new Relation(nextClockCircuit, clockRead),
                warmup, new Relation(productCircuit, productRead), reversal,
                new Relation(supportStartCircuit, supportRead),
                new Relation(maps[0], C.G(startRead, endRead)), new Relation(maps[1], sequenceRead));

            // The private state is a free local source. ReceiveTogether checks
            // all primitive ranges before either sensor or clock can change.
            var admitted = Carrier.ReceiveTogether(C.G(delta, new CarrierLocation(clock, 1)));
            if (!SettlementObservation.ObservedQuiet(admitted)) return false;
            N.Assign(previousDelta, memoryValue);
            var witness = ReadWitness.Create(new Carrier(), C.G(observed, priorLevel, priorOperand),
                reads, emitted, exposedResidue, C.G(admitted, memoryResponse));
            next = C.G(sensor, clock, previousDelta, port, p[4], emitted, witness);
            packet = emitted;
            return true;
        }
        catch (OverflowException)
        {
            // Native preflight owns all numeric calculation. No unrepresentable
            // reading is converted into a quiet event or partially admitted.
            return false;
        }
    }

    private static bool TryCalculate(IElement circuit, out CarrierLocation value, out Relation read)
    {
        read = N.Read(circuit);
        return Equation.TryReadPrediction(read, out value, out _) &&
            value.Carrier is not null && !value.Carrier.IsUnknown && value.Tick is not null;
    }

    private static bool TryMap(Relation map, CarrierLocation input, Carrier output,
        out CarrierLocation value, out Relation read)
    {
        value = default;
        return Equation.TryPredict(map, input, output, out read) &&
            Equation.TryReadPrediction(read, out value, out var residue) &&
            ReferenceEquals(value.Carrier, output) &&
            ProjectionResidue.TryRead(residue, out _, out var remainder, out _) && remainder == 0;
    }

    private static bool TryRoot(Relation root, out IElement[] p,
        out IElement[] port, out IElement[] maps)
    {
        p = C.Parts(root);
        port = maps = [];
        if (p.Length != 7 || p[0] is not Carrier { IsUnknown: false, IsInverse: false } sensor ||
            p[1] is not Carrier { IsUnknown: false, IsInverse: false } clock ||
            p[2] is not CarrierLocation previous || previous.Tick is null ||
            !ReferenceEquals(previous.Carrier, sensor) || p[3] is not Relation publicPort ||
            p[4] is not Relation clockMaps || p[5] is not Relation)
            return false;
        port = C.Parts(publicPort);
        maps = C.Parts(clockMaps);
        return port.Length == 3 && port[0] is Relation frame &&
            TimeStepSequence.TryReadSpan(frame, out var start, out var end) &&
            start.Ticks == 0 && end.Ticks == 1 && !start.Carrier.IsUnknown &&
            port[1] is Carrier { IsUnknown: false } source &&
            port[2] is Carrier { IsUnknown: false } sequence &&
            !ReferenceEquals(source, sequence) && !ReferenceEquals(start.Carrier, source) &&
            !ReferenceEquals(start.Carrier, sequence) && maps.Length == 2 &&
            maps[0] is Relation && maps[1] is Relation;
    }
}
