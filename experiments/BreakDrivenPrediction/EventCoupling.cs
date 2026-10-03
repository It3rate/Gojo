using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using C = BreakDrivenPrediction.Continuation;
using N = BreakDrivenPrediction.NativeCircuit;

namespace BreakDrivenPrediction;

/// <summary>
/// Experimental local interpretation of an exposed event port. It has no access
/// to the supplier's private root. All retained state is ordinary Gojo structure.
/// </summary>
public static class EventCoupling
{
    // Root: continuation, subscription, conditional equation, credit, demand,
    // policy, proposed route, participating route, sequence watermark, last event.
    public static Relation Create(Relation continuation, Relation port)
    {
        var p = C.Parts(port);
        if (p.Length != 3 || p[0] is not Relation basis ||
            !TimeStepSequence.TryReadSpan(basis, out var start, out var end) ||
            start.Ticks != 0 || end.Ticks != 1 || p[1] is not Carrier sourceAxis ||
            p[2] is not Carrier sequenceAxis)
            return Relation.UnresolvedRead(port, continuation, Carrier.Unknown);
        var cue = new Carrier();
        var subscription = C.G(port,
            Equation.Affine(sourceAxis, 0, 1, C.Clock(continuation), 0, 1),
            Equation.Affine(start.Carrier, 0, 1, cue, 0, 1));
        // Replacement delta = primitive delta * (1 - 2 * event).
        // Event zero preserves the primitive; event one reverses it.
        var candidate = Equation.Product(C.Sensor(continuation),
            Equation.UnitPort(C.Sensor(continuation)), Equation.WeightedPort(cue, 0, 1, 1, -1));
        var credit = new Carrier();
        credit.Attach(new Relation(Relation.Point(credit, 4), Relation.Point(credit, 4)));
        var demand = new Carrier();
        Carrier.ReceiveTogether(C.G(new CarrierLocation(demand, 1)));
        var policy = C.G(new CarrierLocation(credit, 2), new CarrierLocation(credit, 4),
            new CarrierLocation(credit, 2), new CarrierLocation(credit, 1));
        return C.G(continuation, subscription, candidate, credit, demand, policy,
            new Placeholder(), new Placeholder(), new CarrierLocation(sequenceAxis, -1), new Placeholder());
    }

    public static Relation Base(Relation root) => (Relation)C.Parts(root)[0];
    public static Relation Subscription(Relation root) => (Relation)C.Parts(root)[1];
    public static Carrier Credit(Relation root) => (Carrier)C.Parts(root)[3];
    public static Carrier Demand(Relation root) => (Carrier)C.Parts(root)[4];
    public static bool Participates(Relation root) => C.Parts(root)[7] is Relation;
    public static bool HasProposal(Relation root) => C.Parts(root)[6] is Relation;

    // Issue: base issue, receipt, counterfactual, shadow/open, export,
    // shadow calculation/open, frozen demand, frozen credit, proposed route,
    // participating route. Read-only: no delivery is consumed until assessment.
    public static Relation Issue(Relation root, IElement packet)
    {
        var p = C.Parts(root);
        if (p.Length != 10) return Relation.UnresolvedRead(packet, root, Carrier.Unknown);
        var existing = C.Issue(Base(root));
        var f = C.Parts(existing);
        if (f.Length != 11) return existing;
        var receipt = ReadReceipt(root, packet);
        var r = C.Parts(receipt);
        var counterfactual = C.Copy((CarrierLocation)f[6]);
        var demand = C.Copy(Demand(root).ValueSlot);
        var credit = C.Copy(Credit(root).ValueSlot);
        IElement shadow = new Placeholder(), calculation = new Placeholder();
        var exported = counterfactual;
        if (p[6] is Relation && r[4] is CarrierLocation cue && IsPositive(cue))
        {
            var input = new Relation(C.Copy((CarrierLocation)f[5]), cue);
            var read = Equation.Predict((Relation)p[2], input, counterfactual.Carrier);
            calculation = new Relation(new Relation(input, p[2]), read);
            if (Equation.TryReadPrediction(read, out var candidate, out _))
            {
                shadow = C.Copy(candidate);
                if (p[7] is Relation && IsPositive(demand)) exported = C.Copy(candidate);
            }
        }
        return C.G(existing, receipt, counterfactual, shadow, exported, calculation,
            demand, credit, p[6], p[7]);
    }

    // Receipt: public packet, actual availability, subscription, validation
    // evidence, projected reading/open, accepted sequence/open. Open payloads
    // can still advance a valid publication watermark; absence cannot.
    public static Relation ReadReceipt(Relation root, IElement packet)
    {
        var receiver = Base(root);
        var subscription = Subscription(root);
        var s = C.Parts(subscription);
        var availability = C.Copy(C.Clock(receiver).ValueSlot);
        Relation Unavailable(IElement evidence) => C.G(packet, availability, subscription,
            evidence, Relation.UnresolvedRead(packet, subscription, C.Sensor(receiver)), new Placeholder());
        if (packet is not Relation message || C.Parts(message) is not { Length: 5 } m ||
            !ReferenceEquals(m[0], s[0]) || m[1] is not CarrierLocation sequence ||
            m[2] is not Relation span || !TimeStepSequence.TryReadSpan(span, out var start, out var end))
            return Unavailable(new Placeholder());
        var port = C.Parts((Relation)s[0]);
        if (!ReferenceEquals(sequence.Carrier, port[2]) || !ReferenceEquals(start.Carrier, port[1]) ||
            !ReferenceEquals(end.Carrier, port[1])) return Unavailable(span);
        try
        {
            var mappedTime = Equation.Predict((Relation)s[1], end, C.Clock(receiver));
            if (!ExactProjection(mappedTime, out var localTime)) return Unavailable(mappedTime);
            var mappedStart = Equation.Predict((Relation)s[1], start, C.Clock(receiver));
            if (!ExactProjection(mappedStart, out var localStart)) return Unavailable(mappedStart);
            var aligned = OperationClaim.Equality(new Carrier(), localTime, availability, new Carrier());
            var availableSupport = OperationClaim.AtMost(new Carrier(), localStart, availability, new Carrier());
            var cursor = (CarrierLocation)C.Parts(root)[8];
            // Strict order without manufacturing an unrepresentable cursor + 1.
            var ordered = OperationClaim.AtLeast(new Carrier(), sequence, cursor, new Carrier());
            var sameSequence = OperationClaim.Equality(new Carrier(), sequence, cursor, new Carrier());
            var evidence = C.G(mappedTime, mappedStart, aligned, availableSupport, ordered, sameSequence);
            if (!C.Pass(aligned) || !C.Pass(availableSupport) || !C.Pass(ordered) ||
                !OperationClaim.TryReadResolvedViolation(sameSequence, out var separation) ||
                !IsPositive(separation)) return Unavailable(evidence);
            if (m[3] is Placeholder)
                return C.G(packet, availability, subscription, evidence, new Placeholder(), C.Copy(sequence));
            if (m[3] is not CarrierLocation value ||
                !TimeStepSequence.TryReadSpan((Relation)port[0], out var basis, out _) ||
                !ReferenceEquals(value.Carrier, basis.Carrier)) return Unavailable(evidence);
            var inRange = C.G(OperationClaim.AtLeast(new Carrier(), value,
                    new CarrierLocation(value.Carrier, 0), new Carrier()),
                OperationClaim.AtMost(new Carrier(), value, new CarrierLocation(value.Carrier, 1), new Carrier()));
            if (!C.Parts(inRange).Cast<Relation>().All(C.Pass)) return Unavailable(C.G(evidence, inRange));
            var targetBasis = ((Relation)s[2]).Right;
            if (targetBasis is not Relation frame || !TimeStepSequence.TryReadSpan(frame, out var target, out _))
                return Unavailable(evidence);
            var mapped = Equation.Predict((Relation)s[2], C.Copy(value), target.Carrier);
            if (!ExactProjection(mapped, out var reading)) return Unavailable(C.G(evidence, mapped));
            return C.G(packet, availability, subscription, C.G(evidence, inRange, mapped),
                C.Copy(reading), C.Copy(sequence));
        }
        catch (OverflowException) { return Unavailable(span); }
    }

    public static bool TryObserve(Relation root, Relation issue, CarrierLocation observation,
        out Relation next, out Relation assessment)
    {
        next = root;
        assessment = Relation.UnresolvedRead(observation, issue, C.Sensor(Base(root)));
        try { return ObserveCore(root, issue, observation, out next, out assessment); }
        catch (OverflowException) { return false; }
    }

    private static bool ObserveCore(Relation root, Relation issue, CarrierLocation observation,
        out Relation next, out Relation assessment)
    {
        next = root;
        var receiver = Base(root);
        assessment = Relation.UnresolvedRead(observation, issue, C.Sensor(receiver));
        var p = C.Parts(root);
        var f = C.Parts(issue);
        if (p.Length != 10 || f.Length != 10 || f[0] is not Relation baseIssue ||
            f[1] is not Relation receipt || f[2] is not CarrierLocation counterfactual ||
            f[4] is not CarrierLocation exported || f[6] is not CarrierLocation demand ||
            !ReferenceEquals(f[8], p[6]) || !ReferenceEquals(f[9], p[7]) ||
            !ReferenceEquals(counterfactual.Carrier, C.Sensor(receiver)) ||
            !ReferenceEquals(exported.Carrier, C.Sensor(receiver)) ||
            !ReferenceEquals(observation.Carrier, C.Sensor(receiver)) ||
            !ReferenceEquals(demand.Carrier, Demand(root))) return false;
        var b = C.Parts(baseIssue);
        var r = C.Parts(receipt);
        if (b.Length != 11 || r.Length != 6 || !ReferenceEquals(r[2], p[1]) ||
            b[3] is not CarrierLocation before || b[6] is not CarrierLocation priorExport ||
            !C.Pass(OperationClaim.Equality(new Carrier(), counterfactual, priorExport, new Carrier()))) return false;

        var actual = C.Difference(C.Copy(observation), before, C.Sensor(receiver));
        var baseClaim = C.Agreement(counterfactual, actual);
        var exportClaim = C.Agreement(exported, actual);
        IElement shadowClaim = new Placeholder();
        var shadowReady = f[3] is CarrierLocation candidate && ReferenceEquals(candidate.Carrier, actual.Carrier);
        if (f[3] is CarrierLocation && !shadowReady) return false;
        if (shadowReady) shadowClaim = C.Agreement((CarrierLocation)f[3], actual);
        var improved = shadowClaim is Relation c && C.Pass(c) && !C.Pass(baseClaim);
        var policy = C.Parts((Relation)p[5]);
        var credit = Credit(root);
        var creditBefore = C.Copy(credit.ValueSlot);
        IElement response = new Placeholder();
        CarrierLocation? admitted = null;
        if (shadowReady)
        {
            var requested = N.Value(N.Reframe(IsPositive(demand) && improved
                ? N.Sum((policy[2], 1), (policy[3], -1)) : N.Sum((policy[3], -1)), credit));
            var target = N.Value(N.Reframe(N.Sum((creditBefore, 1), (requested, 1)), credit));
            var lower = OperationClaim.AtLeast(new Carrier(), target, new CarrierLocation(credit, 0), new Carrier());
            var upper = OperationClaim.AtMost(new Carrier(), target, policy[1], new Carrier());
            response = C.G(requested, lower, upper);
            if (C.Pass(lower) && C.Pass(upper)) admitted = requested;
        }
        // Grade all frozen alternatives first. Only the base's own input is
        // admitted to its sensor; a supplier publication is an observation.
        if (!C.TryObserve(receiver, baseIssue, observation, out var nextBase, out var baseAssessment)) return false;
        if (admitted is CarrierLocation change)
            response = C.G(response, Carrier.ReceiveTogether(C.G(change)));
        if (r[5] is CarrierLocation consumed)
            N.Assign((CarrierLocation)p[8], consumed);
        IElement proposal = p[6];
        if (proposal is Placeholder && !C.Pass(baseClaim) && IsPositive(demand))
            proposal = new Relation(p[1], p[2]);
        var qualifies = OperationClaim.AtLeast(new Carrier(), C.Copy(credit.ValueSlot), policy[0], new Carrier());
        IElement participating = new Placeholder();
        if (proposal is Relation && C.Pass(qualifies))
            participating = p[7] is Relation existing ? existing : new Relation(proposal, Relation.Point(C.Sensor(receiver), 1));
        var magnitudeClaim = OperationClaim.Equality(new Carrier(), C.Magnitude(exported), C.Magnitude(actual), new Carrier());
        // Assessment: base result, receipt, baseline verdict, shadow verdict,
        // export verdict, magnitude, response, before/after support, demand,
        // actual, frozen counterfactual/shadow/export. No predecessor root.
        assessment = C.G(baseAssessment, receipt, baseClaim, shadowClaim, exportClaim,
            magnitudeClaim, response, creditBefore, C.Copy(credit.ValueSlot), demand,
            actual, counterfactual, f[3], exported);
        var last = shadowReady || !C.Pass(baseClaim)
            ? TimeStepSequence.Step(new Carrier(), C.Clock(receiver), ((CarrierLocation)b[1]).Ticks,
                ((CarrierLocation)b[2]).Ticks, C.G(counterfactual, f[3], exported),
                C.G(receipt, baseClaim, shadowClaim, exportClaim, response),
                C.G(creditBefore, C.Copy(credit.ValueSlot), demand), magnitudeClaim)
            : p[9];
        next = C.G(nextBase, p[1], p[2], credit, p[4], p[5], proposal, participating, p[8], last);
        return true;
    }

    public static bool IsPositive(CarrierLocation value) => C.Pass(OperationClaim.AtLeast(
        new Carrier(), value, new CarrierLocation(value.Carrier, 1), new Carrier()));

    private static bool ExactProjection(Relation read, out CarrierLocation value)
    {
        if (Equation.TryReadPrediction(read, out value, out var residue) &&
            ProjectionResidue.TryRead(residue, out _, out var numerator, out _) && numerator == 0) return true;
        value = default;
        return false;
    }
}
