using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using N = ConnectionAssembly.NativeMath;

namespace FailureSubscriptions;

/// <summary>
/// Bounded response evidence by future input opportunity, conditional on the
/// initiating error and the connected buyer baseline. State6 = G(source port,
/// context span, maximum extent, G(offset rows), G(minimum reads,retry delay),
/// baseline premise). Row6 = G(offset,helpful,evaluated,unknown,signed relief,
/// retry-after). These empirical reads do not establish a remaining lifetime.
/// </summary>
public static class DurationOffer
{
    public static Relation Create(IElement sourcePort, Relation contextSpan, int maximum = 8, IElement? premise = null)
    {
        if (maximum is < 1 or > 32)
            throw new ArgumentOutOfRangeException(nameof(maximum), "A duration profile contains one to thirty-two supplied rungs.");
        var opportunities = new Carrier();
        var clock = new Carrier();
        var benefitBasis = ReadSpan(contextSpan, out var lower, out _) ? lower.Carrier : new Carrier();
        var rows = Enumerable.Range(1, maximum).Select(offset => (IElement)G(
            L(opportunities, offset), new Carrier(opportunities.UnitShape), new Carrier(opportunities.UnitShape),
            new Carrier(opportunities.UnitShape), new Carrier(benefitBasis.UnitShape), new Carrier(clock.UnitShape))).ToArray();
        return G(sourcePort, contextSpan, L(opportunities, maximum), G(rows),
            G(L(opportunities, 2), L(clock, 8)), premise ?? new Placeholder());
    }

    public static IElement Source(Relation state) => Parts(state)[0];
    public static Relation Context(Relation state) => (Relation)Parts(state)[1];
    public static IElement Premise(Relation state) => Parts(state)[5];
    public static Relation[] Rows(Relation state) => Parts((Relation)Parts(state)[3]).Cast<Relation>().ToArray();
    public static Carrier Helpful(Relation row) => (Carrier)Parts(row)[1];
    public static Carrier Evaluated(Relation row) => (Carrier)Parts(row)[2];
    public static Carrier Unknown(Relation row) => (Carrier)Parts(row)[3];
    public static Carrier Benefit(Relation row) => (Carrier)Parts(row)[4];
    public static Carrier RetryAfter(Relation row) => (Carrier)Parts(row)[5];

    /// <summary>
    /// Quote13 = G(state, captured trigger, captured start, requested cap,
    /// captured profile, backed extent, offered extent, accepted extent,
    /// accepted future window, accepted tariff, eligibility/calculation evidence,
    /// G(captured per-output fee,activation fee), offered tariff).
    /// A supported prefix needs at least two resolved outcomes and positive
    /// total relief at every rung. One cold/underobserved frontier is a trial.
    /// A known nonpositive frontier is offered again only after its retry date.
    /// The requested cap can shorten an offer without rewriting provider evidence.
    /// </summary>
    public static Relation Quote(Relation state, CarrierLocation trigger, CarrierLocation startTime,
        int requestedCap, CarrierLocation deliveryFee, CarrierLocation activationFee)
    {
        var evidence = new List<IElement>();
        var maximum = (CarrierLocation)Parts(state)[2];
        var requested = L(maximum.Carrier, requestedCap);
        var requestClaim = OperationClaim.Between(new Carrier(), Copy(requested), L(maximum.Carrier, 0), Copy(maximum), new Carrier());
        evidence.Add(requestClaim);
        Relation contextClaim;
        if (ReadSpan(Context(state), out var low, out var high))
            contextClaim = OperationClaim.Between(new Carrier(), Copy(trigger), Copy(low), Copy(high), new Carrier());
        else contextClaim = Relation.UnresolvedRead(trigger, Context(state), trigger.Carrier);
        evidence.Add(contextClaim);
        var deliveryNonnegative = OperationClaim.AtLeast(new Carrier(), Copy(deliveryFee), L(deliveryFee.Carrier, 0), new Carrier());
        var activationNonnegative = OperationClaim.AtLeast(new Carrier(), Copy(activationFee), L(activationFee.Carrier, 0), new Carrier());
        evidence.Add(deliveryNonnegative); evidence.Add(activationNonnegative);
        var startRead = N.Read(Copy(startTime));
        evidence.Add(startRead);
        var readableStart = Equation.TryReadPrediction(startRead, out _, out _);

        var rows = Rows(state);
        var snapshots = new List<IElement>();
        foreach (var row in rows)
        {
            var amount = Calculate(Benefit(row), evidence, (Benefit(row).ValueSlot, 1), (Benefit(row).LiveTensionSlot, 1));
            var retry = Calculate(RetryAfter(row), evidence, (RetryAfter(row).ValueSlot, 1), (RetryAfter(row).LiveTensionSlot, 1));
            snapshots.Add(G(Copy((CarrierLocation)Parts(row)[0]), Copy(Helpful(row).ValueSlot),
                Copy(Evaluated(row).ValueSlot), Copy(Unknown(row).ValueSlot), amount, retry));
        }

        var backed = L(maximum.Carrier, 0);
        var offered = backed;
        var frontier = false;
        foreach (var snapshot in snapshots.Cast<Relation>())
        {
            if (frontier) break;
            var row = Parts(snapshot);
            var reads = As(maximum.Carrier, (CarrierLocation)row[2], evidence);
            var enough = OperationClaim.AtLeast(new Carrier(), reads, Parts((Relation)Parts(state)[4])[0], new Carrier());
            var positive = OperationClaim.AtLeast(new Carrier(), row[4], L(((CarrierLocation)row[4]).Carrier, 1), new Carrier());
            evidence.Add(enough); evidence.Add(positive);
            if (Pass(enough) && Pass(positive))
            {
                backed = Copy((CarrierLocation)row[0]);
                offered = backed;
                continue;
            }

            frontier = true;
            var observed = OperationClaim.AtLeast(new Carrier(), reads, L(maximum.Carrier, 1), new Carrier());
            evidence.Add(observed);
            var retry = (CarrierLocation)row[5];
            var retryReady = readableStart
                ? OperationClaim.AtLeast(new Carrier(), As(retry.Carrier, Copy(startTime), evidence), retry, new Carrier())
                : Relation.UnresolvedRead(startRead, retry, retry.Carrier);
            evidence.Add(retryReady);
            // No resolved outcome or positive but insufficient evidence may be
            // probed immediately. Known zero/harm must wait its native deadline.
            if (!Pass(observed) || Pass(positive) || Pass(retryReady))
                offered = Copy((CarrierLocation)row[0]);
        }

        var eligible = BetweenPass(requestClaim) && BetweenPass(contextClaim) &&
            Pass(deliveryNonnegative) && Pass(activationNonnegative) && readableStart;
        var withinOffer = OperationClaim.AtMost(new Carrier(), requested, offered, new Carrier());
        evidence.Add(withinOffer);
        var accepted = eligible ? Copy(Pass(withinOffer) ? requested : offered) : L(maximum.Carrier, 0);
        IElement end = readableStart
            ? Calculate(startTime.Carrier, evidence, (Copy(startTime), 1), (accepted, 1))
            : Relation.UnresolvedRead(startRead, accepted, startTime.Carrier);
        var total = Tariff(accepted, deliveryFee, activationFee, evidence);
        var offeredTotal = Tariff(eligible ? offered : L(maximum.Carrier, 0), deliveryFee, activationFee, evidence);
        return G(state, Copy(trigger), Copy(startTime), Copy(requested), G(snapshots.ToArray()),
            backed, offered, accepted, new Relation(Copy(startTime), end), total, G(evidence.ToArray()),
            G(Copy(deliveryFee), Copy(activationFee)), offeredTotal);
    }

    /// <summary>
    /// Update the one actually observed offset targetTime-start. The caller
    /// owns the immediate-next-observation promise and once-only gate. Missing
    /// outcomes update only that offset's unknown count. Unpurchased/cancelled
    /// tail offsets are untouched, never recorded as failures. Nonpositive
    /// resolved evidence sets this rung's retry date eight arrivals later.
    /// </summary>
    public static Relation Observe(Relation state, Relation quote, CarrierLocation targetTime,
        CarrierLocation? relief, IElement provenance)
    {
        var evidence = new List<IElement>();
        if (!ReferenceEquals(Parts(quote)[0], state) || !Matches(quote))
            return G(state, quote, provenance, Relation.UnresolvedRead(quote, state, Carrier.Unknown));
        var start = (CarrierLocation)Parts(quote)[2];
        var earliest = Calculate(start.Carrier, evidence, (Copy(start), 1), (L(start.Carrier, 1), 1));
        var end = (CarrierLocation)Future(quote).Right;
        var inside = OperationClaim.Between(new Carrier(), Copy(targetTime), earliest, Copy(end), new Carrier());
        evidence.Add(inside);
        if (!BetweenPass(inside)) return G(state, quote, provenance, G(evidence.ToArray()), Relation.UnresolvedRead(targetTime, Future(quote), start.Carrier));
        var axis = ((CarrierLocation)Parts(state)[2]).Carrier;
        var offset = Calculate(axis, evidence, (Copy(targetTime), 1), (Copy(start), -1));
        foreach (var row in Rows(state))
        {
            var selected = OperationClaim.Equality(new Carrier(), offset, Parts(row)[0], new Carrier());
            evidence.Add(selected);
            if (!Pass(selected)) continue;
            var arrivals = new List<IElement>();
            if (relief.HasValue)
            {
                var value = Copy(relief.Value);
                var benefitBasis = ReadSpan(Context(state), out var low, out _) ? low.Carrier : Carrier.Unknown;
                var positive = OperationClaim.AtLeast(new Carrier(), value, L(benefitBasis, 1), new Carrier());
                evidence.Add(positive);
                if (!OperationClaim.TryReadResolvedViolation(positive, out var violation))
                    return G(state, quote, provenance, G(evidence.ToArray()), Relation.UnresolvedRead(value, row, Benefit(row)));
                arrivals.Add(Calculate(Evaluated(row), evidence, (L(Evaluated(row), 1), 1)));
                arrivals.Add(Calculate(Helpful(row), evidence, (L(Helpful(row), violation.Ticks == 0 ? 1 : 0), 1)));
                arrivals.Add(Calculate(Benefit(row), evidence, (value, 1)));
                if (violation.Ticks != 0)
                {
                    var retry = Calculate(RetryAfter(row), evidence, (Copy(targetTime), 1), (Parts((Relation)Parts(state)[4])[1], 1));
                    arrivals.Add(Calculate(RetryAfter(row), evidence, (retry, 1),
                        (RetryAfter(row).ValueSlot, -1), (RetryAfter(row).LiveTensionSlot, -1)));
                }
            }
            else arrivals.Add(Calculate(Unknown(row), evidence, (L(Unknown(row), 1), 1)));
            var settlement = Carrier.ReceiveTogether(G(arrivals.ToArray()));
            return G(state, quote, provenance, offset, row, G(evidence.ToArray()), settlement);
        }
        return G(state, quote, provenance, G(evidence.ToArray()), Relation.UnresolvedRead(offset, state, axis));
    }

    public static CarrierLocation Backed(Relation quote) => (CarrierLocation)Parts(quote)[5];
    public static CarrierLocation Offered(Relation quote) => (CarrierLocation)Parts(quote)[6];
    public static CarrierLocation Accepted(Relation quote) => (CarrierLocation)Parts(quote)[7];
    public static Relation Future(Relation quote) => (Relation)Parts(quote)[8];
    public static CarrierLocation TotalFee(Relation quote) => (CarrierLocation)Parts(quote)[9];
    public static Relation Profile(Relation quote) => (Relation)Parts(quote)[4];
    public static CarrierLocation OfferedFee(Relation quote) => (CarrierLocation)Parts(quote)[12];
    public static bool Matches(Relation quote)
    {
        var evidence = Parts((Relation)Parts(quote)[10]);
        return evidence.Length >= 5 && BetweenPass((Relation)evidence[0]) && BetweenPass((Relation)evidence[1]) &&
            Pass((Relation)evidence[2]) && Pass((Relation)evidence[3]) &&
            Equation.TryReadPrediction((Relation)evidence[4], out _, out _);
    }

    private static CarrierLocation Tariff(CarrierLocation extent, CarrierLocation fee, CarrierLocation activation, List<IElement> evidence)
    {
        var positive = OperationClaim.AtLeast(new Carrier(), extent, L(extent.Carrier, 1), new Carrier());
        evidence.Add(positive);
        return Pass(positive) ? Calculate(fee.Carrier, evidence, (N.Product(extent, Copy(fee)), 1), (Copy(activation), 1)) : L(fee.Carrier, 0);
    }
    private static CarrierLocation As(Carrier target, CarrierLocation value, List<IElement> evidence) => Calculate(target, evidence, (value, 1));
    private static CarrierLocation Calculate(Carrier target, List<IElement> evidence, params (IElement Value, long Coefficient)[] terms)
    {
        var equation = N.SumTo(target, terms); var read = N.Read(equation);
        evidence.Add(G(equation, read));
        if (!Equation.TryReadPrediction(read, out var value, out _)) throw new InvalidOperationException("An authored duration arithmetic basis was unresolved.");
        return Copy(value);
    }
    private static bool ReadSpan(Relation span, out CarrierLocation lower, out CarrierLocation upper)
    {
        if (span.Left is CarrierLocation a && span.Right is CarrierLocation b && a.Carrier.SameIdentityAs(b.Carrier))
        { lower = a; upper = b; return true; }
        return TimeStepSequence.TryReadSpan(span, out lower, out upper);
    }
    private static bool BetweenPass(Relation claim) => OperationClaim.TryReadResolvedBetweenViolation(claim, out var violation) && violation.Ticks == 0;
    private static bool Pass(Relation claim) => OperationClaim.TryReadResolvedViolation(claim, out var violation) && violation.Ticks == 0;
    private static CarrierLocation Copy(CarrierLocation value) => L(value.Carrier, value.Ticks);
    private static CarrierLocation L(Carrier carrier, long ticks) => new(carrier, ticks);
    private static IElement[] Parts(Relation group) => N.Parts(group);
    private static Relation G(params IElement[] parts) => N.Group(parts);
}
