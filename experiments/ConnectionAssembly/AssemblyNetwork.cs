using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using N = ConnectionAssembly.NativeMath;

namespace ConnectionAssembly;

/// <summary>
/// A supplied finite neighborhood of native recognition and help circuits.
/// Scheduling and local proposal traversal are experimental policy. All reads,
/// ports, numeric responses and actual dependencies remain connected structure.
/// </summary>
public static class AssemblyNetwork
{
    // World8: input carrier, admission cursor, nodes, links, policy, two-site
    // trace, copied admission version, latest frame witness/open.
    // Node8: exposed input/trace, constraints, raw, engagement, request, help,
    // pressure, accounted. Node identity is its raw carrier, not its wrapper.
    // Link6: producer raw, consumer request, support, gate, live product, receipt.
    // Policy7: engagement cap, support cap, recognition grant, utility grant,
    // proposal grant, fade span, phase carrier. Policy numbers are supplied.
    public static Relation Create()
    {
        var source = new Carrier();
        var cursor = new Carrier();
        var credit = new Carrier();
        var phase = new Carrier();
        var packet = new Carrier();
        var policy = G(Relation.Segment(credit, 0, 48), Relation.Segment(credit, 0, 48),
            new CarrierLocation(credit, 8), new CarrierLocation(credit, 6),
            new CarrierLocation(credit, 3), Relation.Segment(phase, 0, 4), phase);
        var trace = G(new Placeholder(), new Placeholder());
        var constraints = Constraints(new Placeholder(), new Placeholder(), new Placeholder());
        var nodes = new Relation[6];
        for (var i = 0; i < nodes.Length; i++)
        {
            var pressure = new Carrier(packet.UnitShape);
            // Reset every whole request packet to the origin while retaining it
            // as positive tension. A zero-to-zero boundary would be inert.
            pressure.Attach(new Relation(Relation.Point(pressure, 1), Relation.Point(pressure, 0)));
            nodes[i] = G(G(Relation.Segment(source, 0, 1), trace, new Placeholder()), constraints[i],
                new Carrier(packet.UnitShape), new Carrier(credit.UnitShape),
                new Carrier(packet.UnitShape), new Carrier(packet.UnitShape), pressure,
                new Carrier(packet.UnitShape));
        }
        var links = new List<Relation>();
        foreach (var consumer in nodes)
        foreach (var producer in nodes)
        {
            if (ReferenceEquals(consumer, producer)) continue;
            var support = new Carrier(credit.UnitShape);
            var gate = new Carrier(packet.UnitShape);
            var receipt = new Carrier(packet.UnitShape);
            var equation = Equation.Product(receipt, Equation.UnitPort(Raw(producer)),
                Equation.UnitPort(gate), Equation.UnitPort(Request(consumer)));
            var live = receipt.AttachLiveOutput(Input(Raw(producer).ValueSlot, gate.ValueSlot,
                Request(consumer).ValueSlot), equation);
            links.Add(G(Raw(producer), Request(consumer), support, gate, live, receipt));
        }
        foreach (var node in nodes)
        {
            var incoming = links.Where(link => ConsumerRequest(link).SameIdentityAs(Request(node))).ToArray();
            Help(node).AttachLiveOutput(Input(incoming.Select(link => (IElement)Receipt(link).ValueSlot).ToArray()),
                Equation.Sum(Help(node), incoming.Select(link => Equation.UnitPort(Receipt(link))).ToArray()));
        }
        return G(source, cursor, G(nodes), G(links.ToArray()), policy, trace,
            new CarrierLocation(cursor, 0), new Placeholder());
    }

    public static Relation[] Nodes(Relation root) => Parts((Relation)Parts(root)[2]).Cast<Relation>().ToArray();
    public static Relation[] Links(Relation root) => Parts((Relation)Parts(root)[3]).Cast<Relation>().ToArray();
    public static Carrier Raw(Relation node) => (Carrier)Parts(node)[2];
    public static Carrier Engagement(Relation node) => (Carrier)Parts(node)[3];
    public static Carrier Request(Relation node) => (Carrier)Parts(node)[4];
    public static Carrier Help(Relation node) => (Carrier)Parts(node)[5];
    public static Carrier Pressure(Relation node) => (Carrier)Parts(node)[6];
    public static bool Accounted(Relation node) => ((Carrier)Parts(node)[7]).Value != 0;
    public static Carrier Support(Relation link) => (Carrier)Parts(link)[2];
    public static Carrier Gate(Relation link) => (Carrier)Parts(link)[3];
    public static Carrier Receipt(Relation link) => (Carrier)Parts(link)[5];
    public static Carrier ProducerPort(Relation link) => (Carrier)Parts(link)[0];
    public static Carrier ConsumerRequest(Relation link) => (Carrier)Parts(link)[1];
    public static IElement[] Parts(Relation relation) => N.Parts(relation);
    public static bool Pass(Relation claim) =>
        OperationClaim.TryReadResolvedViolation(claim, out var violation) && violation.Ticks == 0;

    /// <summary>
    /// Complete means three consecutive delivered levels are present. Gaps and
    /// missing samples admit an explicitly incomplete frame without feedback.
    /// Repeated/backwards reports and stale roots are rejected before mutation.
    /// </summary>
    public static bool TryObserve(Relation root, long sequence, long? value,
        out Relation next, out Relation witness)
    {
        next = root;
        witness = Relation.UnresolvedRead(root, root, Carrier.Unknown);
        var p = Parts(root);
        if (p.Length != 8 || p[0] is not Carrier source || p[1] is not Carrier cursor ||
            p[4] is not Relation policy || p[5] is not Relation trace ||
            p[6] is not CarrierLocation version) return false;
        try
        {
            var nativeSequence = new CarrierLocation(cursor, sequence);
            var current = Copy(cursor.ValueSlot);
            var currentVersion = Equal(version, current);
            var successorCircuit = N.Reframe(N.Sum((current, 1), (new CarrierLocation(cursor, 1), 1)), cursor);
            var successorRead = N.Read(successorCircuit);
            if (!Scalar(successorRead, out var successor)) return false;
            var increasing = AtLeast(nativeSequence, successor);
            if (!Pass(currentVersion) || !Pass(increasing))
            {
                witness = G(currentVersion, increasing, nativeSequence);
                return false;
            }
            var adjacent = Equal(nativeSequence, successor);
            var oldTrace = Parts(trace);
            IElement older = Pass(adjacent) && oldTrace.Length == 2 ? oldTrace[0] : new Placeholder();
            IElement last = Pass(adjacent) && oldTrace.Length == 2 ? oldTrace[1] : new Placeholder();
            IElement input = value.HasValue ? new CarrierLocation(source, value.Value) : new Placeholder();
            var complete = input is CarrierLocation && older is CarrierLocation && last is CarrierLocation;
            var availability = G(currentVersion, increasing, adjacent, successorCircuit, successorRead,
                G(older, last, input), new CarrierLocation(cursor, complete ? 1 : 0));
            var oldNodes = Nodes(root);
            var constraints = Constraints(older, last, input);
            var nodes = oldNodes.Select((node, i) =>
            {
                var parts = Parts(node);
                return G(G(Relation.Segment(source, 0, 1), G(older, last), input), constraints[i],
                    parts[2], parts[3], parts[4], parts[5], parts[6], parts[7]);
            }).ToArray();
            var links = Links(root);
            var q = Parts(policy);
            var phase = (Carrier)q[6];
            var beforePhase = Copy(phase.ValueSlot);
            var constraintReads = new Relation[nodes.Length];
            var recognized = new bool[nodes.Length];
            var requested = new bool[nodes.Length];
            var engagementBefore = nodes.Select(node => Copy(Engagement(node).ValueSlot)).ToArray();
            var supportBefore = links.Select(link => Copy(Support(link).ValueSlot)).ToArray();
            var gateBefore = links.Select(link => Copy(Gate(link).ValueSlot)).ToArray();
            var allConstraints = constraints.SelectMany(Parts).Cast<Relation>().ToArray();
            var reads = complete ? N.ReadMany(allConstraints.Select(item => item.Left).ToArray()) : [];
            var readIndex = 0;
            for (var i = 0; i < nodes.Length; i++)
            {
                var claims = new List<IElement>();
                recognized[i] = complete;
                foreach (var constraint in Parts(constraints[i]).Cast<Relation>())
                {
                    if (!complete)
                    {
                        claims.Add(Relation.UnresolvedRead(input, constraint, Raw(nodes[i])));
                        continue;
                    }
                    var read = reads[readIndex++];
                    if (!Scalar(read, out var measured)) return false;
                    var claim = AtLeast(measured, (CarrierLocation)constraint.Right);
                    recognized[i] &= Pass(claim);
                    claims.Add(G(constraint, read, claim));
                }
                var engaged = AtLeast(engagementBefore[i], new CarrierLocation(engagementBefore[i].Carrier, 1));
                requested[i] = complete && !recognized[i] && Pass(engaged);
                constraintReads[i] = G(G(claims.ToArray()), engaged);
            }

            // Stage representable native deltas before opening this finite input
            // frame. A numeric report is an external perturbation of the source.
            var opening = new List<IElement>();
            var openingReads = new List<IElement>();
            AddTarget(opening, openingReads, cursor, nativeSequence);
            if (input is CarrierLocation supplied) AddTarget(opening, openingReads, source, supplied);
            for (var i = 0; i < nodes.Length; i++)
            {
                AddTarget(opening, openingReads, Raw(nodes[i]), new CarrierLocation(Raw(nodes[i]), recognized[i] ? 1 : 0));
                AddTarget(opening, openingReads, Request(nodes[i]), new CarrierLocation(Request(nodes[i]), requested[i] ? 1 : 0));
                AddTarget(opening, openingReads, Pressure(nodes[i]), new CarrierLocation(Pressure(nodes[i]), requested[i] ? 1 : 0));
            }
            var opened = Carrier.ReceiveTogether(G(opening.ToArray()));
            if (!SettlementObservation.ObservedQuiet(opened))
            {
                witness = G(availability, opened);
                return false;
            }

            var frozenRaw = nodes.Select(node => Copy(Raw(node).ValueSlot)).ToArray();
            var frozenRequest = nodes.Select(node => Copy(Request(node).ValueSlot)).ToArray();
            var frozenHelp = nodes.Select(node => Copy(Help(node).ValueSlot)).ToArray();
            var frozenReceipt = links.Select(link => Copy(Receipt(link).ValueSlot)).ToArray();
            var accounting = new Relation[nodes.Length];
            var unmetReads = new Relation[nodes.Length];
            var unmet = new CarrierLocation[nodes.Length];
            var accounted = new bool[nodes.Length];
            for (var i = 0; i < nodes.Length; i++)
            {
                accounting[i] = Account(frozenRaw[i], frozenHelp[i]);
                accounted[i] = complete && Pass((Relation)Parts(accounting[i])[2]);
                var helpInRequest = N.Reframe(frozenHelp[i], frozenRequest[i].Carrier);
                var helpRead = N.Read(helpInRequest);
                if (!Scalar(helpRead, out var help)) return false;
                var shortage = OperationClaim.AtMost(new Carrier(), frozenRequest[i], help, Pressure(nodes[i]));
                if (!OperationClaim.TryReadResolvedViolation(shortage, out unmet[i])) return false;
                unmetReads[i] = G(helpInRequest, helpRead, shortage);
            }
            var useful = new bool[links.Length];
            var proposed = new bool[links.Length];
            var ablations = new Relation[links.Length];
            for (var j = 0; j < links.Length; j++)
            {
                var i = Array.FindIndex(nodes, node => Request(node).SameIdentityAs(ConsumerRequest(links[j])));
                var withoutCircuit = N.Reframe(N.Sum((frozenHelp[i], 1), (frozenReceipt[j], -1)), frozenHelp[i].Carrier);
                var withoutRead = N.Read(withoutCircuit);
                if (!Scalar(withoutRead, out var without)) return false;
                var withoutAccount = Account(frozenRaw[i], without);
                var positiveReceipt = AtLeast(frozenReceipt[j], new CarrierLocation(frozenReceipt[j].Carrier, 1));
                useful[j] = complete && accounted[i] && Pass(positiveReceipt) &&
                    !Pass((Relation)Parts(withoutAccount)[2]);
                ablations[j] = G(accounting[i], withoutCircuit, withoutRead, withoutAccount, positiveReceipt);
            }
            // Local fixed-neighborhood traversal happens after all existing
            // receipts and their ablations are frozen. No discovery-frame credit.
            if (complete)
            for (var i = 0; i < nodes.Length; i++)
            {
                var unsatisfied = AtLeast(unmet[i], new CarrierLocation(unmet[i].Carrier, 1));
                if (!Pass(unsatisfied)) continue;
                for (var j = 0; j < links.Length; j++)
                {
                    if (!ConsumerRequest(links[j]).SameIdentityAs(Request(nodes[i]))) continue;
                    var producer = Array.FindIndex(nodes, node => Raw(node).SameIdentityAs(ProducerPort(links[j])));
                    var unused = Equal(supportBefore[j], new CarrierLocation(supportBefore[j].Carrier, 0));
                    var succeeds = AtLeast(frozenRaw[producer], new CarrierLocation(frozenRaw[producer].Carrier, 1));
                    if (!Pass(unused) || !Pass(succeeds)) continue;
                    proposed[j] = true;
                    ablations[j] = G(ablations[j], G(unsatisfied, unused, succeeds));
                    break;
                }
            }

            var phaseCircuit = N.Reframe(N.Sum((beforePhase, 1), (new CarrierLocation(phase, complete ? 1 : 0), 1)), phase);
            var phaseRead = N.Read(phaseCircuit);
            if (!Scalar(phaseRead, out var progressedPhase)) return false;
            var fadeSpan = (Relation)q[5];
            if (!TimeStepSequence.TryReadSpan(fadeSpan, out _, out var phaseLimit)) return false;
            var fadeClaim = AtLeast(progressedPhase, new CarrierLocation(phase, phaseLimit.Ticks));
            var fade = complete && Pass(fadeClaim);
            var phaseAfter = fade ? new CarrierLocation(phase, 0) : progressedPhase;
            var changes = new List<IElement>();
            var changeReads = new List<IElement>();
            var engagementAfter = new CarrierLocation[nodes.Length];
            var engagementReads = new Relation[nodes.Length];
            var supportAfter = new CarrierLocation[links.Length];
            var supportReads = new Relation[links.Length];
            var nextGate = new CarrierLocation[links.Length];
            var gateClaims = new Relation[links.Length];
            AddTarget(changes, changeReads, phase, phaseAfter);
            for (var i = 0; i < nodes.Length; i++)
            {
                var reward = complete && recognized[i] ? (CarrierLocation)q[2] : new CarrierLocation(((CarrierLocation)q[2]).Carrier, 0);
                engagementReads[i] = Bounded(engagementBefore[i], reward, (Relation)q[0], fade, out engagementAfter[i]);
                AddTarget(changes, changeReads, Engagement(nodes[i]), engagementAfter[i]);
                AddTarget(changes, changeReads, Pressure(nodes[i]), unmet[i]);
                var accountPort = (Carrier)Parts(nodes[i])[7];
                AddTarget(changes, changeReads, accountPort, new CarrierLocation(accountPort, accounted[i] ? 1 : 0));
            }
            for (var j = 0; j < links.Length; j++)
            {
                var reward = useful[j] ? (CarrierLocation)q[3] : proposed[j] ? (CarrierLocation)q[4] :
                    new CarrierLocation(((CarrierLocation)q[3]).Carrier, 0);
                supportReads[j] = Bounded(supportBefore[j], reward, (Relation)q[1], fade, out supportAfter[j]);
                AddTarget(changes, changeReads, Support(links[j]), supportAfter[j]);
                gateClaims[j] = AtLeast(supportAfter[j], new CarrierLocation(supportAfter[j].Carrier, 1));
                nextGate[j] = new CarrierLocation(Gate(links[j]), Pass(gateClaims[j]) ? 1 : 0);
            }
            var changed = Carrier.ReceiveTogether(G(changes.ToArray()));
            if (!SettlementObservation.ObservedQuiet(changed)) { witness = G(opened, changed); return false; }

            var nodeSamples = nodes.Select((node, i) => (IElement)G(node,
                G(frozenRaw[i], frozenRequest[i], frozenHelp[i], Copy(unmet[i]), Copy(engagementAfter[i]),
                    new CarrierLocation((Carrier)Parts(node)[7], accounted[i] ? 1 : 0)),
                constraintReads[i], accounting[i], unmetReads[i], engagementReads[i])).ToArray();
            var linkSamples = links.Select((link, j) => (IElement)G(link,
                G(Copy(supportAfter[j]), Copy(nextGate[j]), frozenReceipt[j],
                    new CarrierLocation(Gate(link), proposed[j] ? 1 : 0),
                    new CarrierLocation(Gate(link), useful[j] ? 1 : 0),
                    new CarrierLocation(Gate(link), gateBefore[j].Ticks != 0 && nextGate[j].Ticks == 0 ? 1 : 0)),
                G(supportBefore[j], gateBefore[j]), ablations[j], supportReads[j], gateClaims[j])).ToArray();

            // All recognition and request pulses close before any new gate opens.
            // The retained sample is the current frame; gate state belongs to the
            // next frame. No newly probed link can retroactively deliver help.
            var closing = new List<IElement>();
            var closingReads = new List<IElement>();
            foreach (var node in nodes)
            {
                AddTarget(closing, closingReads, Raw(node), new CarrierLocation(Raw(node), 0));
                AddTarget(closing, closingReads, Request(node), new CarrierLocation(Request(node), 0));
            }
            var closed = Carrier.ReceiveTogether(G(closing.ToArray()));
            var gates = new List<IElement>();
            var gateReads = new List<IElement>();
            for (var j = 0; j < links.Length; j++) AddTarget(gates, gateReads, Gate(links[j]), nextGate[j]);
            var installed = Carrier.ReceiveTogether(G(gates.ToArray()));
            if (!SettlementObservation.ObservedQuiet(closed) || !SettlementObservation.ObservedQuiet(installed))
            { witness = G(opened, changed, closed, installed); return false; }

            var nextTrace = input is CarrierLocation ? G(last, input) : G(new Placeholder(), new Placeholder());
            witness = G(G(nativeSequence, input), availability, G(nodeSamples), G(linkSamples),
                G(G(openingReads.ToArray()), opened, phaseCircuit, phaseRead, fadeClaim,
                    G(changeReads.ToArray()), changed, G(closingReads.ToArray()), closed,
                    G(gateReads.ToArray()), installed), nextTrace);
            next = G(source, cursor, G(nodes), G(links), policy, nextTrace, Copy(nativeSequence), witness);
            return true;
        }
        catch (OverflowException)
        {
            witness = Relation.UnresolvedRead(new CarrierLocation(cursor, sequence), root, source);
            return false;
        }
    }

    // Witness6: input, availability, sampled nodes, sampled links, transitions,
    // next trace. Samples preserve numeric outputs while their first participant
    // identifies the actual connected node/link; it is not a historical snapshot
    // of every mutable carrier reachable from that identity.
    public static bool Complete(Relation witness) => Parts(witness) is { Length: 6 } p &&
        p[1] is Relation availability && Parts(availability) is { Length: 7 } a &&
        a[6] is CarrierLocation complete && complete.Ticks == 1;
    public static Relation[] NodeSamples(Relation witness) => Parts((Relation)Parts(witness)[2]).Cast<Relation>().ToArray();
    public static Relation[] LinkSamples(Relation witness) => Parts((Relation)Parts(witness)[3]).Cast<Relation>().ToArray();
    public static Relation SampleNode(Relation sample) => (Relation)Parts(sample)[0];
    public static Relation SampleLink(Relation sample) => (Relation)Parts(sample)[0];
    private static long Sample(Relation sample, int index) => ((CarrierLocation)Parts((Relation)Parts(sample)[1])[index]).Ticks;
    public static long SampleRaw(Relation sample) => Sample(sample, 0);
    public static long SampleRequest(Relation sample) => Sample(sample, 1);
    public static long SampleHelp(Relation sample) => Sample(sample, 2);
    public static long SamplePressure(Relation sample) => Sample(sample, 3);
    public static long SampleEngagement(Relation sample) => Sample(sample, 4);
    public static bool SampleAccounted(Relation sample) => Sample(sample, 5) == 1;
    public static long SampleSupport(Relation sample) => Sample(sample, 0);
    public static long SampleGate(Relation sample) => Sample(sample, 1);
    public static long SampleReceipt(Relation sample) => Sample(sample, 2);
    public static bool WasProposed(Relation sample) => Sample(sample, 3) == 1;
    public static bool WasUseful(Relation sample) => Sample(sample, 4) == 1;
    public static bool WasReleased(Relation sample) => Sample(sample, 5) == 1;
    public static Carrier SampleProducer(Relation sample) => ProducerPort(SampleLink(sample));
    public static Carrier SampleConsumer(Relation sample) => ConsumerRequest(SampleLink(sample));

    private static Relation[] Constraints(IElement older, IElement last, IElement current)
    {
        var d = N.Sum((current, 1), (last, -1));
        var prior = N.Sum((last, 1), (older, -1));
        var oppositeD = N.Sum((d, -1));
        var oppositePrior = N.Sum((prior, -1));
        var product = N.Product(d, prior);
        var reverse = N.Sum((product, -1));
        var magnitudeSquared = N.Product(d, d);
        var difference = N.Sum((d, 1), (prior, -1));
        var differenceSquared = N.Product(difference, difference);
        Relation Floor(IElement value, long bound) => new(value, new CarrierLocation(N.Output(value), bound));
        return
        [
            G(Floor(d, 1), Floor(oppositeD, -3), Floor(prior, 1), Floor(oppositePrior, -3)),
            G(Floor(oppositeD, 1), Floor(d, -3), Floor(oppositePrior, 1), Floor(prior, -3)),
            G(Floor(d, 0), Floor(oppositeD, 0)),
            G(Floor(magnitudeSquared, 16)),
            G(Floor(reverse, 1)),
            G(Floor(product, 1), Floor(differenceSquared, 1))
        ];
    }

    private static Relation Account(CarrierLocation raw, CarrierLocation help)
    {
        var circuit = N.Reframe(N.Sum((raw, 1), (help, 1)), raw.Carrier);
        var read = N.Read(circuit);
        if (!Scalar(read, out var total)) return G(circuit, read, Relation.UnresolvedRead(raw, help, raw.Carrier));
        return G(circuit, read, AtLeast(total, new CarrierLocation(raw.Carrier, 1)));
    }

    private static Relation Bounded(CarrierLocation before, CarrierLocation grant, Relation cap,
        bool fade, out CarrierLocation after)
    {
        if (!TimeStepSequence.TryReadSpan(cap, out _, out var upper)) throw new InvalidOperationException("Missing native cap span.");
        var increase = N.Reframe(N.Sum((before, 1), (grant, 1)), before.Carrier);
        var increaseRead = N.Read(increase);
        if (!Scalar(increaseRead, out var proposed)) throw new InvalidOperationException("Unresolved native increase.");
        var ceilingBridge = N.Reframe(upper, before.Carrier);
        var ceilingRead = N.Read(ceilingBridge);
        if (!Scalar(ceilingRead, out var ceiling)) throw new InvalidOperationException("Unresolved native cap calibration.");
        var upperClaim = OperationClaim.AtMost(new Carrier(), proposed, ceiling, new Carrier());
        var capped = Pass(upperClaim) ? proposed : ceiling;
        var decline = N.Reframe(N.Sum((capped, 1), (new CarrierLocation(before.Carrier, fade ? 1 : 0), -1)), before.Carrier);
        var declineRead = N.Read(decline);
        if (!Scalar(declineRead, out var declining)) throw new InvalidOperationException("Unresolved native fade.");
        var lower = new CarrierLocation(before.Carrier, 0);
        var lowerClaim = AtLeast(declining, lower);
        after = Pass(lowerClaim) ? declining : lower;
        return G(before, grant, cap, increase, increaseRead, ceilingBridge, ceilingRead, upperClaim, capped,
            decline, declineRead, lowerClaim, after);
    }

    private static void AddTarget(List<IElement> arrivals, List<IElement> witnesses,
        Carrier carrier, CarrierLocation target)
    {
        var total = N.Sum((Copy(carrier.ValueSlot), 1), (Copy(carrier.LiveTensionSlot), 1));
        var delta = N.Reframe(N.Sum((target, 1), (total, -1)), carrier);
        var read = N.Read(delta);
        if (!Scalar(read, out var change)) throw new InvalidOperationException("Unresolved native state response.");
        arrivals.Add(change);
        witnesses.Add(G(target, total, delta, read));
    }

    private static Relation AtLeast(CarrierLocation actual, CarrierLocation floor) =>
        OperationClaim.AtLeast(new Carrier(), actual, floor, new Carrier());
    private static Relation Equal(CarrierLocation actual, CarrierLocation expected) =>
        OperationClaim.Equality(new Carrier(), actual, expected, new Carrier());
    private static CarrierLocation Copy(CarrierLocation value) => new(value.Carrier, value.Ticks);
    private static bool Scalar(Relation read, out CarrierLocation value) => Equation.TryReadPrediction(read, out value, out _);
    private static Relation G(params IElement[] values) => N.Group(values);
    private static IElement Input(params IElement[] values)
    {
        IElement input = values[0];
        for (var i = 1; i < values.Length; i++) input = new Relation(input, values[i]);
        return input;
    }
}
