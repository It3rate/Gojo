using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using C = BreakDrivenPrediction.Continuation;
using N = BreakDrivenPrediction.NativeCircuit;

namespace BreakDrivenPrediction;

/// <summary>
/// Bounded local acquisition and reporting structure. A fine occurrence means
/// a strict adjacent-delta reversal detected at an admitted observation time;
/// it does not locate a physical inflection between samples. Sparse acquisition
/// keeps that local clue private and leaves its fine occurrence collection open.
/// </summary>
public static class TimingSource
{
    // Root: sensor, private timestamp, public port, report-span contract,
    // report start, last two (public time, private delta/open) pairs,
    // current-window occurrences, current-window residue, latest packet,
    // latest private witness, private-to-public clock/value maps.
    // Port: public clock, independent public value axis, acquisition span.
    // Packet: port, directed coverage (start,end], availability, occurrences/open,
    // copied public scalar, exposed residue. The half-open coverage convention
    // is the admission rule: start was already accounted for by the prior report.
    public static Relation Create(long initial, Carrier publicClock, long sampleStride,
        long reportSpan = 12)
    {
        ArgumentNullException.ThrowIfNull(publicClock);
        if (publicClock.IsUnknown || publicClock.IsInverse)
            throw new ArgumentException("A concrete forward public clock is required.", nameof(publicClock));
        if (sampleStride <= 0) throw new ArgumentOutOfRangeException(nameof(sampleStride));
        if (reportSpan <= 0 || reportSpan % sampleStride != 0)
            throw new ArgumentOutOfRangeException(nameof(reportSpan),
                "The positive report span must contain whole acquisition intervals.");

        var sensor = new Carrier();
        var clock = new Carrier();
        var valueAxis = new Carrier();
        var admitted = Carrier.ReceiveTogether(C.G(new CarrierLocation(sensor, initial)));
        var port = C.G(publicClock, valueAxis, Relation.Segment(publicClock, 0, sampleStride));
        var maps = C.G(Equation.Affine(clock, 0, 1, publicClock, 0, 1),
            Equation.Affine(sensor, 0, 1, valueAxis, 0, 1));
        var report = Relation.Segment(publicClock, 0, reportSpan);
        var start = new CarrierLocation(publicClock, 0);
        var trace = C.G(C.G(C.Copy(start), new Placeholder()));
        var open = new Placeholder();
        var packet = C.G(port, Relation.Segment(publicClock, 0, 0), C.Copy(start), open,
            new CarrierLocation(valueAxis, initial), C.G(open));
        var witness = ReadWitness.Create(new Carrier(), new CarrierLocation(sensor, initial),
            maps, packet, open, admitted);
        return C.G(sensor, clock, port, report, start, trace, C.G(), C.G(), packet, witness, maps);
    }

    public static Carrier Sensor(Relation root) => (Carrier)C.Parts(root)[0];
    public static Carrier Clock(Relation root) => (Carrier)C.Parts(root)[1];
    public static Relation Port(Relation root) => (Relation)C.Parts(root)[2];
    public static Relation Packet(Relation root) => (Relation)C.Parts(root)[8];

    /// <summary>
    /// Admit exactly the next acquisition time, up to the current report deadline.
    /// Use the returned root for the next call. Input identities and every native
    /// calculation are checked before the two free private carriers receive change.
    /// This fixture owns those carriers; attaching external settlement feedback is
    /// outside its contract. On failure no new report or observation is claimed:
    /// next remains root, whose contract can be inspected alongside the rejected input.
    /// </summary>
    public static bool TryObserve(Relation root, CarrierLocation timeOnPublicClock,
        CarrierLocation levelOnSensor, out Relation next)
    {
        next = root;
        if (!TryRoot(root, out var p, out var port, out var trace, out var maps) ||
            timeOnPublicClock.Tick is null || levelOnSensor.Tick is null ||
            !ReferenceEquals(timeOnPublicClock.Carrier, port[0]) ||
            !ReferenceEquals(levelOnSensor.Carrier, p[0])) return false;
        try
        {
            var sensor = (Carrier)p[0];
            var clock = (Carrier)p[1];
            var publicClock = (Carrier)port[0];
            var acquired = C.Copy(timeOnPublicClock);
            var observed = C.Copy(levelOnSensor);
            var priorPair = C.Parts((Relation)trace[^1]);
            var priorTime = C.Copy((CarrierLocation)priorPair[0]);
            var reportStart = C.Copy((CarrierLocation)p[4]);
            TimeStepSequence.TryReadSpan((Relation)port[2], out _, out var stride);
            TimeStepSequence.TryReadSpan((Relation)p[3], out _, out var reportSpan);

            var dueCircuit = N.Reframe(N.Sum((priorTime, 1), (stride, 1)), publicClock);
            var deadlineCircuit = N.Reframe(N.Sum((reportStart, 1), (reportSpan, 1)), publicClock);
            if (!TryCalculate(dueCircuit, out var due, out var dueRead) ||
                !TryCalculate(deadlineCircuit, out var deadline, out var deadlineRead) ||
                !TryMap((Relation)maps[0], C.Copy(clock.ValueSlot), publicClock,
                    out var heldTime, out var heldTimeRead)) return false;
            var currentClaim = OperationClaim.Equality(new Carrier(), heldTime, priorTime, new Carrier());
            var sequentialClaim = OperationClaim.Equality(new Carrier(), acquired, due, new Carrier());
            var deadlineClaim = OperationClaim.AtMost(new Carrier(), acquired, deadline, new Carrier());
            if (!C.Pass(currentClaim) || !C.Pass(sequentialClaim) || !C.Pass(deadlineClaim)) return false;

            // Read signed participation, including a free source's retained draw.
            // Historical operands have independent coordinates before any admission.
            var levelCircuit = N.Reframe(N.Sum((C.Copy(sensor.ValueSlot), 1),
                (C.Copy(sensor.LiveTensionSlot), 1)), sensor);
            if (!TryCalculate(levelCircuit, out var priorLevel, out var levelRead)) return false;
            var deltaCircuit = N.Reframe(N.Sum((observed, 1), (priorLevel, -1)), sensor);
            var clockStepCircuit = N.Reframe(stride, clock);
            var clockDueCircuit = N.Reframe(N.Sum((C.Copy(clock.ValueSlot), 1),
                (clockStepCircuit, 1)), clock);
            if (!TryCalculate(deltaCircuit, out var delta, out var deltaRead) ||
                !TryCalculate(clockStepCircuit, out var clockStep, out var clockStepRead) ||
                !TryCalculate(clockDueCircuit, out _, out var clockDueRead)) return false;

            IElement priorDelta = priorPair[1] is CarrierLocation previous ? C.Copy(previous) : new Placeholder();
            var productCircuit = N.Product(priorDelta, delta);
            var productRead = N.Read(productCircuit);
            IElement reversal = new Placeholder();
            var detected = false;
            if (priorDelta is CarrierLocation)
            {
                if (!Equation.TryReadPrediction(productRead, out var product, out _)) return false;
                var claim = OperationClaim.AtMost(new Carrier(), product,
                    new CarrierLocation(product.Carrier, -1), new Carrier());
                if (!OperationClaim.TryReadResolvedViolation(claim, out var violation)) return false;
                reversal = claim;
                detected = violation.Ticks == 0;
            }

            var supportStart = (CarrierLocation)C.Parts((Relation)trace[0])[0];
            var support = Relation.Segment(publicClock, supportStart.Ticks, acquired.Ticks);
            var fineClaim = OperationClaim.Equality(new Carrier(), stride,
                new CarrierLocation(publicClock, 1), new Carrier());
            if (!OperationClaim.TryReadResolvedViolation(fineClaim, out var coarse)) return false;
            var occurrences = C.Parts((Relation)p[6]).ToList();
            if (coarse.Ticks == 0 && detected)
                occurrences.Add(Relation.Segment(publicClock, acquired.Ticks, acquired.Ticks));
            var pending = C.G(occurrences.ToArray());
            var windowResidue = C.Parts((Relation)p[7]).ToList();
            if (priorDelta is Placeholder)
                windowResidue.Add(C.G(Relation.Segment(publicClock, priorTime.Ticks, acquired.Ticks), new Placeholder()));
            var retainedResidue = C.G(windowResidue.ToArray());
            var nextTrace = C.G(C.G(C.Copy(priorTime), priorDelta), C.G(C.Copy(acquired), C.Copy(delta)));
            var reads = C.G(currentClaim, sequentialClaim, deadlineClaim, fineClaim,
                new Relation(dueCircuit, dueRead), new Relation(deadlineCircuit, deadlineRead),
                new Relation(maps[0], heldTimeRead), new Relation(levelCircuit, levelRead),
                new Relation(deltaCircuit, deltaRead), new Relation(clockStepCircuit, clockStepRead),
                new Relation(clockDueCircuit, clockDueRead), new Relation(productCircuit, productRead), reversal);

            // Whole-arrival preflight also checks both primitive state ranges before
            // mutation. A failed product or time read never partially admits input.
            var admitted = Carrier.ReceiveTogether(C.G(delta, clockStep));
            if (!SettlementObservation.ObservedQuiet(admitted)) return false;
            var witness = ReadWitness.Create(new Carrier(), C.G(C.Copy(acquired), observed, priorLevel, priorDelta),
                reads, C.G(support, reversal), priorDelta is Placeholder ? new Placeholder() : C.G(), admitted);
            next = C.G(sensor, clock, p[2], p[3], p[4], nextTrace, pending,
                retainedResidue, p[8], witness, p[10]);
            return true;
        }
        catch (OverflowException) { return false; }
    }

    /// <summary>
    /// Publish only at the latest admitted timestamp after the bounded window is
    /// complete. Sparse acquisition exposes unknown fine occurrences even when a
    /// local reversal clue was observed. Copies expose no private sensor, clock,
    /// delta coordinate, prior root, circuit or read ancestry.
    /// </summary>
    public static bool Publish(Relation root, CarrierLocation availableAt,
        out Relation next, out Relation packet)
    {
        next = root;
        packet = C.Parts(root) is { Length: 11 } fields && fields[8] is Relation previous ? previous : C.G();
        if (!TryRoot(root, out var p, out var port, out var trace, out var maps) ||
            availableAt.Tick is null || !ReferenceEquals(availableAt.Carrier, port[0])) return false;
        try
        {
            var publicClock = (Carrier)port[0];
            var available = C.Copy(availableAt);
            var latest = (CarrierLocation)C.Parts((Relation)trace[^1])[0];
            var start = C.Copy((CarrierLocation)p[4]);
            TimeStepSequence.TryReadSpan((Relation)p[3], out _, out var reportSpan);
            TimeStepSequence.TryReadSpan((Relation)port[2], out _, out var stride);
            var elapsedCircuit = N.Reframe(N.Sum((available, 1), (start, -1)), publicClock);
            if (!TryCalculate(elapsedCircuit, out var elapsed, out var elapsedRead) ||
                !TryMap((Relation)maps[0], C.Copy(((Carrier)p[1]).ValueSlot), publicClock,
                    out var heldTime, out var heldTimeRead)) return false;
            var latestClaim = OperationClaim.Equality(new Carrier(), available, C.Copy(latest), new Carrier());
            var currentClaim = OperationClaim.Equality(new Carrier(), heldTime, C.Copy(latest), new Carrier());
            var completeClaim = OperationClaim.AtLeast(new Carrier(), elapsed, reportSpan, new Carrier());
            var boundedClaim = OperationClaim.AtMost(new Carrier(), elapsed, reportSpan, new Carrier());
            if (!C.Pass(latestClaim) || !C.Pass(currentClaim) || !C.Pass(completeClaim) || !C.Pass(boundedClaim)) return false;

            var sensor = (Carrier)p[0];
            var levelCircuit = N.Reframe(N.Sum((C.Copy(sensor.ValueSlot), 1),
                (C.Copy(sensor.LiveTensionSlot), 1)), sensor);
            if (!TryCalculate(levelCircuit, out var level, out var levelRead) ||
                !TryMap((Relation)maps[1], level, (Carrier)port[1], out var value, out var valueRead)) return false;
            var fineClaim = OperationClaim.Equality(new Carrier(), stride,
                new CarrierLocation(publicClock, 1), new Carrier());
            if (!OperationClaim.TryReadResolvedViolation(fineClaim, out var coarse)) return false;
            IElement occurrences = coarse.Ticks == 0 ? p[6] : new Placeholder();
            IElement residue = coarse.Ticks == 0 ? p[7] : C.G(port[2], occurrences);
            var emitted = C.G(p[2], Relation.Segment(publicClock, start.Ticks, available.Ticks),
                C.Copy(available), occurrences, C.Copy(value), residue);
            var witness = ReadWitness.Create(new Carrier(), C.G(start, C.Copy(available), level),
                C.G(latestClaim, currentClaim, completeClaim, boundedClaim, fineClaim,
                    new Relation(elapsedCircuit, elapsedRead), new Relation(maps[0], heldTimeRead),
                    new Relation(levelCircuit, levelRead), new Relation(maps[1], valueRead)),
                // One latest detector witness survives the boundary, including
                // a sparse reversal's support interval. It has no predecessor
                // root/witness link; the next admission replaces it outright.
                emitted, residue, p[9]);
            next = C.G(p[0], p[1], p[2], p[3], C.Copy(available), p[5], C.G(), C.G(), emitted, witness, p[10]);
            packet = emitted;
            return true;
        }
        catch (OverflowException) { return false; }
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

    private static bool TryRoot(Relation root, out IElement[] p, out IElement[] port,
        out IElement[] trace, out IElement[] maps)
    {
        p = C.Parts(root);
        port = trace = maps = [];
        if (p.Length != 11 || p[0] is not Carrier { IsUnknown: false, IsInverse: false } sensor ||
            p[1] is not Carrier { IsUnknown: false, IsInverse: false } clock || clock.Tension != 0 ||
            p[2] is not Relation publicPort || p[3] is not Relation report ||
            p[4] is not CarrierLocation start || start.Tick is null ||
            p[5] is not Relation retained || p[6] is not Relation pending ||
            p[7] is not Relation residue || p[8] is not Relation || p[9] is not Relation ||
            p[10] is not Relation bridges) return false;
        port = C.Parts(publicPort);
        if (port.Length != 3 || port[0] is not Carrier { IsUnknown: false, IsInverse: false } publicClock ||
            port[1] is not Carrier { IsUnknown: false, IsInverse: false } valueAxis ||
            new[] { sensor, clock, publicClock, valueAxis }.Distinct(ReferenceEqualityComparer.Instance).Count() != 4 ||
            !ReferenceEquals(start.Carrier, publicClock) || start.Ticks < 0 ||
            port[2] is not Relation acquisition ||
            !TimeStepSequence.TryReadSpan(acquisition, out var a, out var b) ||
            !TimeStepSequence.TryReadSpan(report, out var c, out var d) ||
            !ReferenceEquals(a.Carrier, publicClock) || !ReferenceEquals(b.Carrier, publicClock) ||
            !ReferenceEquals(c.Carrier, publicClock) || !ReferenceEquals(d.Carrier, publicClock) ||
            a.Ticks != 0 || b.Ticks <= 0 || c.Ticks != 0 || d.Ticks <= 0 || d.Ticks % b.Ticks != 0 ||
            !retained.TryReadOrderedGroup(out trace) || trace.Length is < 1 or > 2 ||
            !pending.TryReadOrderedGroup(out var events) || !residue.TryReadOrderedGroup(out _) ||
            !bridges.TryReadOrderedGroup(out maps) || maps.Length != 2 || maps.Any(map => map is not Relation)) return false;
        foreach (var entry in trace)
        {
            if (entry is not Relation pair || C.Parts(pair) is not { Length: 2 } parts ||
                parts[0] is not CarrierLocation time || time.Tick is null ||
                !ReferenceEquals(time.Carrier, publicClock) || time.Ticks < 0 ||
                (parts[1] is not Placeholder && (parts[1] is not CarrierLocation delta ||
                    delta.Tick is null || !ReferenceEquals(delta.Carrier, sensor)))) return false;
        }
        foreach (var occurrence in events)
            if (occurrence is not Relation span || !TimeStepSequence.TryReadSpan(span, out var from, out var to) ||
                !ReferenceEquals(from.Carrier, publicClock) || !ReferenceEquals(to.Carrier, publicClock) ||
                from.Ticks != to.Ticks || from.Ticks <= start.Ticks) return false;
        return true;
    }
}
