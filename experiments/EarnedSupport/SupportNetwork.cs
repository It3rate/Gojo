using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using M = NegotiatedPorts.MarketProbe;
using N = ConnectionAssembly.NativeMath;

namespace EarnedSupport;

/// <summary>
/// An exploratory support recurrence connected to the unchanged negotiated
/// market. Its carriers are mutable physical traces, not stored verdicts.
/// Issuance is explicit external credit input; the issued carrier is an
/// accounting witness and is deliberately not a spendable resource owner.
/// </summary>
public static class SupportNetwork
{
    // World5: market, policy, consumer support states, ledger, latest witness.
    // Policy5: old weight 7, divisor 8, trace grain 64, gain numerator,
    // gain denominator. These are supplied exchange/response calibrations.
    // State6: consumer, signed trace, division remainder, payout remainder,
    // mutable observation sites, reusable equations.
    // Ledger5: original supply, issued credit, last settled market version,
    // expected-total equation, market's exposed expected-total coordinate.
    public static Relation Create(long gainNumerator = 1, long gainDenominator = 1)
    {
        if (gainNumerator < 0 || gainDenominator <= 0)
            throw new ArgumentOutOfRangeException(nameof(gainDenominator),
                "The authored exchange calibration needs a nonnegative numerator and positive denominator.");
        var market = M.Create();
        var resources = ResourceParts(market);
        var credit = (Carrier)resources[2];
        var policy = G(L(credit, 7), L(credit, 8), L(credit, 64),
            L(credit, gainNumerator), L(credit, gainDenominator));
        var states = M.Consumers(market).Select(consumer => BuildState(consumer, policy, credit)).ToArray();
        var issued = new Carrier(credit.UnitShape);
        var clock = ((CarrierLocation)M.Parts(M.Operational(market))[7]).Carrier;
        var lastSettled = new Carrier(clock.UnitShape);
        var initial = L(credit, 4096);
        var expected = N.SumTo(credit, (initial, 1), (issued.ValueSlot, 1), (issued.LiveTensionSlot, 1));
        var ledger = G(initial, issued, lastSettled, expected, resources[3]);
        return G(market, policy, G(states), ledger, new Placeholder());
    }

    private static Relation BuildState(Relation consumer, Relation policy, Carrier credit)
    {
        var p = M.Parts(policy);
        var trace = new Carrier(credit.UnitShape);
        var remainder = new Carrier(credit.UnitShape);
        var payoutRemainder = new Carrier(credit.UnitShape);
        var gross = L(credit, 0);
        var positive = L(credit, 0);
        var liveTrace = Signed(trace);
        var liveRemainder = Signed(remainder);
        var livePayoutRemainder = Signed(payoutRemainder);
        var traceNumerator = N.SumTo(credit, (N.Product(liveTrace, p[0]), 1),
            (N.Product(gross, p[2]), 1), (liveRemainder, 1));
        var traceQuotient = Divide(traceNumerator, p[1], credit);
        var traceRemainder = N.SumTo(credit, (traceNumerator, 1), (N.Product(traceQuotient, p[1]), -1));
        var payoutNumerator = N.SumTo(credit, (N.Product(positive, p[3]), 1), (livePayoutRemainder, 1));
        var payoutDenominator = N.Product(p[2], p[4]);
        var payoutQuotient = Divide(payoutNumerator, payoutDenominator, credit);
        var payoutRest = N.SumTo(credit, (payoutNumerator, 1), (N.Product(payoutQuotient, payoutDenominator), -1));
        return G(consumer, trace, remainder, payoutRemainder, G(gross, positive),
            G(traceNumerator, traceQuotient, traceRemainder, payoutNumerator,
                payoutDenominator, payoutQuotient, payoutRest));
    }

    public static Relation Market(Relation root) => (Relation)M.Parts(root)[0];
    public static Relation Policy(Relation root) => (Relation)M.Parts(root)[1];
    public static Relation[] States(Relation root) => M.Parts((Relation)M.Parts(root)[2]).Cast<Relation>().ToArray();
    public static Relation Ledger(Relation root) => (Relation)M.Parts(root)[3];
    public static Carrier Trace(Relation state) => (Carrier)M.Parts(state)[1];
    public static Carrier TraceRemainder(Relation state) => (Carrier)M.Parts(state)[2];
    public static Carrier PayoutRemainder(Relation state) => (Carrier)M.Parts(state)[3];
    public static Carrier Issued(Relation root) => (Carrier)M.Parts(Ledger(root))[1];
    public static Carrier LastSettledVersion(Relation root) => (Carrier)M.Parts(Ledger(root))[2];
    public static Relation Equations(Relation state) => (Relation)M.Parts(state)[5];
    public static CarrierLocation SignedValue(Carrier carrier) => Copy(M.Value(N.Read(Signed(carrier))));

    /// <summary>A captured native accounting claim: original supply plus
    /// explicitly issued credit equals the actual market resource owners.</summary>
    public static Relation Conservation(Relation root)
    {
        var resources = ResourceParts(Market(root));
        var ledger = M.Parts(Ledger(root));
        return OperationClaim.Equality(new Carrier(), Copy(M.Value(N.Read((Relation)resources[4]))),
            Copy(M.Value(N.Read((Relation)ledger[3]))), new Carrier());
    }

    public static bool TryObserve(Relation root, long? input, out Relation next,
        out Relation marketFrame, out Relation supportFrame) =>
        TryObserve(root, input, true, true, 5, 8, out next, out marketFrame, out supportFrame);

    public static bool TryObserve(Relation root, long? input, bool demand0, bool demand1,
        long funding0, long funding1, out Relation next, out Relation marketFrame, out Relation supportFrame)
    {
        next = root;
        supportFrame = Relation.UnresolvedRead(root, root, Carrier.Unknown);
        if (!M.TryObserve(Market(root), input, demand0, demand1, funding0, funding1,
                out var marketNext, out var observed))
        {
            marketFrame = observed;
            return false;
        }
        // M's fixed-supply assertion shares the resource expected-total site.
        // Capture that assertion before issuance retargets the live site, so
        // the returned pre-issuance frame does not silently change its meaning.
        var frameParts = M.Parts(observed);
        var resources = ResourceParts(marketNext);
        frameParts[5] = OperationClaim.Equality(new Carrier(),
            Copy(M.Value(N.Read((Relation)resources[4]))), Copy((CarrierLocation)resources[3]), new Carrier());
        marketFrame = G(frameParts);
        var m = M.Parts(marketNext);
        marketNext = G(m[0], m[1], marketFrame, m[3]);
        var current = M.Parts(root);
        var advanced = G(marketNext, current[1], current[2], current[3], current[4]);
        return TrySettle(advanced, marketFrame, out next, out supportFrame);
    }

    /// <summary>
    /// Settle exactly the latest attached market observation once. A frame is
    /// not transferable evidence merely because its version has the same
    /// number: it must be the frame connected to this market root. Native
    /// version claims then reject stale roots and repeat settlement.
    /// </summary>
    public static bool TrySettle(Relation root, Relation marketFrame, out Relation next, out Relation witness)
    {
        next = root;
        witness = Relation.UnresolvedRead(marketFrame, root, Carrier.Unknown);
        var market = Market(root);
        var m = M.Parts(market);
        var frame = M.Parts(marketFrame);
        if (frame.Length != 7 || frame[0] is not CarrierLocation time ||
            m[3] is not CarrierLocation capturedVersion || !ReferenceEquals(m[2], marketFrame)) return false;

        var evidence = new List<IElement>();
        var clock = (CarrierLocation)M.Parts(M.Operational(market))[7];
        var currentClaim = OperationClaim.Equality(new Carrier(), Copy(capturedVersion), Copy(clock), new Carrier());
        var frameClaim = OperationClaim.Equality(new Carrier(), Copy(time), Copy(capturedVersion), new Carrier());
        var laterEquation = N.SumTo(time.Carrier, (LastSettledVersion(root).ValueSlot, 1),
            (LastSettledVersion(root).LiveTensionSlot, 1), (L(time.Carrier, 1), 1));
        var laterRead = N.Read(laterEquation);
        var laterClaim = OperationClaim.AtLeast(new Carrier(), Copy(time), Copy(M.Value(laterRead)), new Carrier());
        var beforeConservation = Conservation(root);
        evidence.AddRange([currentClaim, frameClaim, G(laterEquation, laterRead), laterClaim, beforeConservation]);
        if (!M.Pass(currentClaim) || !M.Pass(frameClaim) || !M.Pass(laterClaim) || !M.Pass(beforeConservation))
        {
            witness = G(marketFrame, G(evidence.ToArray()), Relation.UnresolvedRead(time, root, time.Carrier));
            return false;
        }

        var outcomes = M.Parts((Relation)frame[2]);
        var states = States(root);
        if (outcomes.Length != states.Length) return false;
        var rows = new List<IElement>();
        var arrivals = new List<IElement>();
        var payments = new List<IElement>();
        var beforeIssued = SignedValue(Issued(root));
        var beforeReservoir = SignedValue(M.Reservoir(market));
        var beforeVersion = SignedValue(LastSettledVersion(root));
        for (var index = 0; index < states.Length; index++)
        {
            var state = states[index];
            var stateParts = M.Parts(state);
            var before = G(SignedValue(Trace(state)), SignedValue(TraceRemainder(state)), SignedValue(PayoutRemainder(state)));
            var outcome = outcomes[index] as Relation;
            var o = outcome is not null ? M.Parts(outcome) : [];
            if (o.Length != 10 || o[0] is not Relation issue || M.Parts(issue) is not { Length: 11 } issueParts ||
                !ReferenceEquals(issueParts[2], stateParts[0]) || o[8] is not CarrierLocation evaluated ||
                issueParts[8] is not CarrierLocation demand)
            {
                rows.Add(G(state, outcomes[index], before, new Placeholder(), before, new Placeholder()));
                continue;
            }
            var evaluatedClaim = OperationClaim.Equality(new Carrier(), Copy(evaluated), L(evaluated.Carrier, 1), new Carrier());
            var demandClaim = OperationClaim.Equality(new Carrier(), Copy(demand), L(demand.Carrier, 1), new Carrier());
            if (!M.Pass(evaluatedClaim) || !M.Pass(demandClaim) || o[3] is not CarrierLocation gross)
            {
                rows.Add(G(state, outcome!, before, G(evaluatedClaim, demandClaim), before, new Placeholder()));
                continue;
            }

            var sites = M.Parts((Relation)stateParts[4]);
            var equations = M.Parts((Relation)stateParts[5]).Cast<Relation>().ToArray();
            ((CarrierLocation)sites[0]).Tick.Set(gross.Ticks);
            var traceReads = N.ReadMany(equations[0], equations[1], equations[2]);
            if (!M.Scalar(traceReads[1], out var newTrace) || !M.Scalar(traceReads[2], out var newTraceRemainder))
            {
                witness = G(marketFrame, state, G(traceReads), Relation.UnresolvedRead(gross, state, gross.Carrier));
                return false;
            }
            var positiveClaim = OperationClaim.AtLeast(new Carrier(), Copy(newTrace), L(newTrace.Carrier, 0), new Carrier());
            var positive = M.Pass(positiveClaim) ? Copy(newTrace) : L(newTrace.Carrier, 0);
            ((CarrierLocation)sites[1]).Tick.Set(positive.Ticks);
            var payoutReads = N.ReadMany(equations[3], equations[4], equations[5], equations[6]);
            if (!M.Scalar(payoutReads[2], out var payout) || !M.Scalar(payoutReads[3], out var payoutRest))
            {
                witness = G(marketFrame, state, G(payoutReads), Relation.UnresolvedRead(positive, state, positive.Carrier));
                return false;
            }
            var local = G(evaluatedClaim, demandClaim, Copy(gross), G(traceReads), positiveClaim,
                Copy(positive), G(payoutReads));
            AddSet(arrivals, evidence, Trace(state), newTrace);
            AddSet(arrivals, evidence, TraceRemainder(state), newTraceRemainder);
            AddSet(arrivals, evidence, PayoutRemainder(state), payoutRest);
            payments.Add(Copy(payout));
            // Row6: state, outcome, captured before, current calculation
            // witness, captured intended after, credit earned this observation.
            rows.Add(G(state, outcome!, before, local,
                G(Copy(newTrace), Copy(newTraceRemainder), Copy(payoutRest)), Copy(payout)));
        }

        var credit = (Carrier)ResourceParts(market)[2];
        var totalEquation = N.SumTo(credit, payments.Select(p => (p, 1L)).ToArray());
        var totalRead = N.Read(totalEquation);
        if (!M.Scalar(totalRead, out var earned)) return false;
        var nonnegative = OperationClaim.AtLeast(new Carrier(), Copy(earned), L(credit, 0), new Carrier());
        evidence.AddRange([G(totalEquation, totalRead), nonnegative]);
        if (!M.Pass(nonnegative)) return false;
        // Two visible arrivals: externally issued spendable credit and its
        // non-spendable cumulative accounting mirror. No supplier-chain mint.
        var reservoirInput = N.Reframe(totalEquation, M.Reservoir(market));
        var issuedInput = N.Reframe(totalEquation, Issued(root));
        var issueReads = N.ReadMany(reservoirInput, issuedInput);
        if (!M.Scalar(issueReads[0], out var reservoirArrival) || !M.Scalar(issueReads[1], out var ledgerArrival))
        {
            witness = G(marketFrame, reservoirInput, issuedInput, G(issueReads));
            return false;
        }
        evidence.Add(G(reservoirInput, issuedInput, G(issueReads)));
        arrivals.Add(Copy(reservoirArrival));
        arrivals.Add(Copy(ledgerArrival));
        AddSet(arrivals, evidence, LastSettledVersion(root), time);
        var settlement = Carrier.ReceiveTogether(G(arrivals.ToArray()));
        var ledger = M.Parts(Ledger(root));
        var expectedRead = N.Read((Relation)ledger[3]);
        if (!M.Scalar(expectedRead, out var expected))
            throw new InvalidOperationException("The supplied issuance ledger lost its readable total after settlement.");
        ((CarrierLocation)ledger[4]).Tick.Set(expected.Ticks);
        var afterConservation = Conservation(root);
        var after = G(SignedValue(Issued(root)), SignedValue(M.Reservoir(market)));
        var timeline = TimeStepSequence.Step(new Carrier(), time.Carrier, beforeVersion.Ticks, time.Ticks,
            G(beforeIssued, beforeReservoir), G(G(rows.ToArray()), G(evidence.ToArray())),
            G(after, Copy(earned)), settlement);
        // SupportFrame8: market frame, rows, earned, before balances,
        // after balances, settlement/calculation witness, accounting, timeline.
        witness = G(marketFrame, G(rows.ToArray()), Copy(earned), G(beforeIssued, beforeReservoir),
            after, G(G(evidence.ToArray()), settlement, expectedRead), afterConservation, timeline);
        var r = M.Parts(root);
        next = G(market, r[1], r[2], r[3], witness);
        return true;
    }

    /// <summary>Keep live state and unsettled next-use promises, release the
    /// completed display/accounting frame. There is no retained outcome log.</summary>
    public static Relation ReleaseFrame(Relation root)
    {
        var r = M.Parts(root);
        var m = M.Parts((Relation)r[0]);
        return G(G(m[0], m[1], new Placeholder(), m[3]), r[1], r[2], r[3], new Placeholder());
    }

    private static Relation Signed(Carrier carrier) => N.SumTo(carrier,
        (carrier.ValueSlot, 1), (carrier.LiveTensionSlot, 1));

    private static Relation Divide(IElement numerator, IElement denominator, Carrier output)
    {
        var basis = N.Output(numerator);
        var input = L(basis, 0);
        var divisor = L(basis, 1);
        var equation = Equation.Affine(new Relation(L(basis, 0), divisor),
            new Relation(L(output, 0), L(output, 1)));
        return G(equation, input, output,
            G(new Relation(input, numerator), new Relation(divisor, N.Reframe(denominator, basis))));
    }

    private static void AddSet(List<IElement> arrivals, List<IElement> evidence, Carrier carrier, CarrierLocation desired)
    {
        var equation = N.SumTo(carrier, (desired, 1), (carrier.ValueSlot, -1), (carrier.LiveTensionSlot, -1));
        var read = N.Read(equation);
        evidence.Add(G(equation, read));
        arrivals.Add(Copy(M.Value(read)));
    }

    private static IElement[] ResourceParts(Relation market) =>
        M.Parts((Relation)M.Parts(M.Operational(market))[6]);
    private static CarrierLocation L(Carrier carrier, long ticks) => new(carrier, ticks);
    private static CarrierLocation Copy(CarrierLocation location) => L(location.Carrier, location.Ticks);
    private static Relation G(params IElement[] parts) => N.Group(parts);
}
