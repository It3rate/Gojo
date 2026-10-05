using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using N = ConnectionAssembly.NativeMath;

namespace FailureSubscriptions;

/// <summary>
/// A supplied conditional empirical prospect, not a confidence or a promise of
/// consecutive correct answers. State7 = G(source port, signed trigger span,
/// maximum horizon, helpful count, evaluated count, unknown count, signed relief).
/// Counts remain unreduced and the signed relief retains harmful outcomes.
/// </summary>
public static class ProspectiveOffer
{
    public static Relation Create(IElement sourcePort, Relation contextSpan, int horizon = 5)
    {
        if (horizon <= 0) throw new ArgumentOutOfRangeException(nameof(horizon));
        var observations = new Carrier();
        var benefit = ReadSpan(contextSpan, out var lower, out _)
            ? new Carrier(lower.Carrier.UnitShape) : new Carrier();
        return G(sourcePort, contextSpan, new CarrierLocation(observations, horizon),
            new Carrier(observations.UnitShape), new Carrier(observations.UnitShape),
            new Carrier(observations.UnitShape), benefit);
    }

    public static IElement Source(Relation state) => Parts(state)[0];
    public static Relation Context(Relation state) => (Relation)Parts(state)[1];
    public static Carrier Helpful(Relation state) => (Carrier)Parts(state)[3];
    public static Carrier Evaluated(Relation state) => (Carrier)Parts(state)[4];
    public static Carrier Unknown(Relation state) => (Carrier)Parts(state)[5];
    public static Carrier Benefit(Relation state) => (Carrier)Parts(state)[6];

    /// <summary>
    /// Quote8 = G(state, captured trigger, captured start, (start,end] window,
    /// captured G(helpful,evaluated,unknown,signed relief), eligibility claims,
    /// native aggregate prospect, requested horizon). The caller supplies the
    /// arrival clock. Missing arrivals consume this interval as well.
    /// All quote values are captured before later observations can change state.
    /// The aggregate extrapolates the mean of resolved historical outcomes; it
    /// does not predict availability. Unknown coverage remains separate in the
    /// captured counts rather than being treated as either success or failure.
    /// </summary>
    public static Relation Quote(Relation state, CarrierLocation trigger, CarrierLocation startTime, int requestedHorizon = 5)
    {
        var evidence = new List<IElement>();
        var maximum = (CarrierLocation)Parts(state)[2];
        var requested = new CarrierLocation(maximum.Carrier, requestedHorizon);
        var requestBounds = OperationClaim.Between(new Carrier(), requested,
            new CarrierLocation(maximum.Carrier, 1), Copy(maximum), new Carrier());
        evidence.Add(requestBounds);
        Relation contextClaim;
        if (ReadSpan(Context(state), out var lower, out var upper))
            contextClaim = OperationClaim.Between(new Carrier(), Copy(trigger), Copy(lower), Copy(upper), new Carrier());
        else contextClaim = Relation.UnresolvedRead(trigger, Context(state), trigger.Carrier);
        evidence.Add(contextClaim);

        var helpful = Copy(Helpful(state).ValueSlot);
        var evaluated = Copy(Evaluated(state).ValueSlot);
        var unknown = Copy(Unknown(state).ValueSlot);
        var signedRelief = Calculate(Benefit(state), evidence,
            (Benefit(state).ValueSlot, 1), (Benefit(state).LiveTensionSlot, 1));
        var counts = G(helpful, evaluated, unknown, signedRelief);
        var end = Calculate(startTime.Carrier, evidence, (Copy(startTime), 1),
            (new CarrierLocation(startTime.Carrier, requestedHorizon), 1));
        var future = new Relation(Copy(startTime), end);
        Relation expected;
        var observed = OperationClaim.AtLeast(new Carrier(), evaluated,
            new CarrierLocation(evaluated.Carrier, 1), new Carrier());
        evidence.Add(observed);
        if (BetweenPass(requestBounds) && BetweenPass(contextClaim) && Pass(observed))
        {
            // This explicit calibration extrapolates pooled benefit per past
            // resolved delivery into the requested future number of arrivals.
            // It does not infer a hazard model or independent Bernoulli trials.
            var projection = Equation.Affine(signedRelief.Carrier, 0, evaluated.Ticks,
                signedRelief.Carrier, 0, requestedHorizon);
            evidence.Add(projection);
            if (!Equation.TryPredict(projection, signedRelief, signedRelief.Carrier, out expected))
                expected = Relation.UnresolvedRead(signedRelief, projection, signedRelief.Carrier);
        }
        else expected = Relation.UnresolvedRead(counts, G(contextClaim, requestBounds, observed), Benefit(state));
        return G(state, Copy(trigger), Copy(startTime), future, counts, G(evidence.ToArray()), expected, requested);
    }

    /// <summary>
    /// Accumulates one later, once-only outcome admitted by the subscription.
    /// That subscription owns the delivery promise, next-arrival timing check,
    /// and settlement gate. This helper does not independently deduplicate a
    /// replayed call. A missing outcome increments only the unknown count.
    /// Provenance connects the externally admitted outcome to these deltas.
    /// </summary>
    public static Relation Observe(Relation state, Relation quote, CarrierLocation? relief, IElement provenance)
    {
        if (!ReferenceEquals(Parts(quote)[0], state) || !Matches(quote))
            return G(state, quote, provenance, Relation.UnresolvedRead(quote, state, Benefit(state)));
        var evidence = new List<IElement>();
        var arrivals = new List<IElement>();
        if (relief.HasValue)
        {
            var value = Copy(relief.Value);
            var helpful = OperationClaim.AtLeast(new Carrier(), value,
                new CarrierLocation(value.Carrier, 1), new Carrier());
            evidence.Add(helpful);
            if (!OperationClaim.TryReadResolvedViolation(helpful, out var violation))
                return G(state, quote, provenance, helpful, Relation.UnresolvedRead(value, state, Benefit(state)));
            arrivals.Add(Calculate(Evaluated(state), evidence, (new CarrierLocation(Evaluated(state), 1), 1)));
            arrivals.Add(Calculate(Helpful(state), evidence,
                (new CarrierLocation(Helpful(state), violation.Ticks == 0 ? 1 : 0), 1)));
            arrivals.Add(Calculate(Benefit(state), evidence, (value, 1)));
        }
        else arrivals.Add(Calculate(Unknown(state), evidence, (new CarrierLocation(Unknown(state), 1), 1)));
        var settlement = Carrier.ReceiveTogether(G(arrivals.ToArray()));
        return G(state, quote, provenance, G(evidence.ToArray()), settlement);
    }

    public static Relation Expected(Relation quote) => (Relation)Parts(quote)[6];
    public static Relation Counts(Relation quote) => (Relation)Parts(quote)[4];
    public static Relation Future(Relation quote) => (Relation)Parts(quote)[3];
    public static bool TryExpected(Relation quote, out CarrierLocation value) =>
        Equation.TryReadPrediction(Expected(quote), out value, out _);

    public static bool Matches(Relation quote)
    {
        var evidence = Parts((Relation)Parts(quote)[5]);
        return evidence.Length >= 2 && evidence[0] is Relation requested && evidence[1] is Relation context &&
            BetweenPass(requested) && BetweenPass(context);
    }

    private static CarrierLocation Calculate(Carrier target, List<IElement> evidence,
        params (IElement Value, long Coefficient)[] terms)
    {
        var equation = N.SumTo(target, terms);
        var read = N.Read(equation);
        evidence.Add(G(equation, read));
        if (!Equation.TryReadPrediction(read, out var value, out _))
            throw new InvalidOperationException("A supplied prospective-offer arithmetic basis was unresolved.");
        return Copy(value);
    }

    private static bool ReadSpan(Relation span, out CarrierLocation lower, out CarrierLocation upper)
    {
        if (span.Left is CarrierLocation a && span.Right is CarrierLocation b && a.Carrier.SameIdentityAs(b.Carrier))
        { lower = a; upper = b; return true; }
        return TimeStepSequence.TryReadSpan(span, out lower, out upper);
    }

    private static bool BetweenPass(Relation claim) =>
        OperationClaim.TryReadResolvedBetweenViolation(claim, out var violation) && violation.Ticks == 0;
    private static bool Pass(Relation claim) =>
        OperationClaim.TryReadResolvedViolation(claim, out var violation) && violation.Ticks == 0;
    private static CarrierLocation Copy(CarrierLocation value) => new(value.Carrier, value.Ticks);
    private static IElement[] Parts(Relation group) => N.Parts(group);
    private static Relation G(params IElement[] parts) => N.Group(parts);
}
