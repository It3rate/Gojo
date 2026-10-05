using Gojo.Core;
using Gojo.Structures;
using C = ChangeSubscriptions.ChangeSurface;
using R = ChangeSubscriptions.ComparativeRetry;
using N = ConnectionAssembly.NativeMath;

namespace ChangeSubscriptions;

/// <summary>A bounded exploratory allowance during an existing local retry
/// delay. A physically attached cheap read can notice and confirm change; it
/// cannot revise the paid-outcome history, rating or retry deadline. The host
/// schedules observations and owns proof that Consume follows a paid issue.</summary>
public static class WakeProbe
{
    // State11 = G(read surface,retry state,live validation opportunity,cheap
    // subscriber,policy,anchor,candidate,epoch,tokens,admission,statistics).
    // Policy2 = G(required confirmations,maximum probes), both native sites.
    // Anchor2 = G(coarse value,validity). This is the CURRENT withdrawal read,
    // not an inferred copy of the last paid forecast that failed.
    // Candidate4 = G(value,armed count,first arrival,first validation opportunity).
    // Epoch4 = G(completed blocks,deadline,initialized,grant already used).
    // Admission4 = G(last arrival,initialized,last consumed arrival,consumed).
    // Statistics7 = G(notifications,initial synchronizations,confirmation reads,
    // grants,probes issued,suppressed changes,revision polls). No monetary units
    // are invented; routine notification checks remain explicitly accounted.
    public static Relation Create(Relation readSurface, Relation retryState, Carrier arrivalClock,
        CarrierLocation opportunities, int maxProbe = 2, int requiredConfirmations = 2)
    {
        if (maxProbe is < 1 or > 2) throw new ArgumentOutOfRangeException(nameof(maxProbe));
        if (requiredConfirmations != 2) throw new ArgumentOutOfRangeException(nameof(requiredConfirmations));
        var count = new Carrier(); var selected = C.Current(readSurface); var flags = new Carrier();
        var completed = R.CompletedBlocks(retryState); var deadline = R.RetryAfter(retryState);
        return G(readSurface, retryState, opportunities, C.Subscribe(readSurface),
            G(L(count, requiredConfirmations), L(count, maxProbe)),
            G(L(selected.Carrier, 0), L(flags, 0)),
            G(L(selected.Carrier, 0), new Carrier(count.UnitShape), L(arrivalClock, 0), L(opportunities.Carrier, 0)),
            G(L(completed, 0), L(deadline.Carrier, 0), new Carrier(), new Carrier()),
            new Carrier(count.UnitShape),
            G(L(arrivalClock, 0), new Carrier(), L(arrivalClock, 0), new Carrier()),
            G(Enumerable.Range(0, 7).Select(_ => (IElement)new Carrier(count.UnitShape)).ToArray()));
    }

    public static IElement[] Parts(Relation relation) => N.Parts(relation);
    public static Relation Surface(Relation state) => (Relation)Parts(state)[0];
    public static Relation Retry(Relation state) => (Relation)Parts(state)[1];
    public static CarrierLocation Opportunity(Relation state) => (CarrierLocation)Parts(state)[2];
    public static Relation Watcher(Relation state) => (Relation)Parts(state)[3];
    public static CarrierLocation RequiredCount(Relation state) => (CarrierLocation)Parts((Relation)Parts(state)[4])[0];
    public static CarrierLocation MaximumProbe(Relation state) => (CarrierLocation)Parts((Relation)Parts(state)[4])[1];
    public static Relation Anchor(Relation state) => (Relation)Parts(state)[5];
    public static Relation Candidate(Relation state) => (Relation)Parts(state)[6];
    public static Relation Epoch(Relation state) => (Relation)Parts(state)[7];
    public static Carrier Tokens(Relation state) => (Carrier)Parts(state)[8];
    private static Relation Admission(Relation state) => (Relation)Parts(state)[9];
    private static Carrier Statistic(Relation state, int index) => (Carrier)Parts((Relation)Parts(state)[10])[index];
    public static Carrier Notifications(Relation state) => Statistic(state, 0);
    public static Carrier InitialSyncs(Relation state) => Statistic(state, 1);
    public static Carrier ConfirmationReads(Relation state) => Statistic(state, 2);
    public static Carrier Grants(Relation state) => Statistic(state, 3);
    public static Carrier ProbesIssued(Relation state) => Statistic(state, 4);
    public static Carrier SuppressedChanges(Relation state) => Statistic(state, 5);
    public static Carrier RevisionPolls(Relation state) => Statistic(state, 6);
    public static CarrierLocation AnchorValue(Relation state) => (CarrierLocation)Parts(Anchor(state))[0];
    public static CarrierLocation AnchorValidity(Relation state) => (CarrierLocation)Parts(Anchor(state))[1];
    public static CarrierLocation CandidateValue(Relation state) => (CarrierLocation)Parts(Candidate(state))[0];
    public static Carrier CandidateCount(Relation state) => (Carrier)Parts(Candidate(state))[1];
    public static CarrierLocation CandidateArrival(Relation state) => (CarrierLocation)Parts(Candidate(state))[2];
    public static CarrierLocation CandidateOpportunity(Relation state) => (CarrierLocation)Parts(Candidate(state))[3];
    public static Carrier GrantUsed(Relation state) => (Carrier)Parts(Epoch(state))[3];
    public static CarrierLocation LastArrival(Relation state) => (CarrierLocation)Parts(Admission(state))[0];
    private static Carrier AdmissionInitialized(Relation state) => (Carrier)Parts(Admission(state))[1];
    private static CarrierLocation LastConsumed(Relation state) => (CarrierLocation)Parts(Admission(state))[2];
    private static Carrier ConsumedInitialized(Relation state) => (Carrier)Parts(Admission(state))[3];

    // Advance8 = G(state,arrival,validation opportunity,ordinary readiness,
    // notification/initial sync/open,granted-now flag,remaining tokens,evidence).
    // Same-arrival rereads are rejected before they can synchronize or confirm.
    public static Relation Advance(Relation state, CarrierLocation arrival)
    {
        var evidence = new List<IElement>();
        var countBasis = RequiredCount(state).Carrier;
        var initial = Zero(AdmissionInitialized(state), evidence);
        var aligned = Equal(L(arrival.Carrier, 0), L(LastArrival(state).Carrier, 0), evidence);
        var next = Calculate(arrival.Carrier, evidence, (LastArrival(state), 1), (L(arrival.Carrier, 1), 1));
        var later = AtLeast(Copy(arrival), next, evidence);
        var ready = R.Ready(Retry(state), Copy(Opportunity(state))); evidence.Add(ready);
        IElement notice = new Placeholder(); var granted = false;
        Relation Finish() => G(state, Copy(arrival), Copy(Opportunity(state)), ready, notice,
            L(countBasis, granted ? 1 : 0), C.Signed(Tokens(state)), G(evidence.ToArray()));
        if (!aligned || (!initial && !later)) return Finish();
        if (!initial && !Equal(Copy(arrival), next, evidence))
        {
            // Current-state notices may coalesce an intervening invalid read.
            // An unobserved interval therefore withdraws an allowance without
            // claiming to reconstruct the missing transitions or refund a grant.
            evidence.Add(Relation.UnresolvedRead(new Relation(Copy(LastArrival(state)), Copy(arrival)),
                C.LastEvent(Surface(state)), C.Current(Surface(state)).Carrier));
            Cancel(state, evidence);
        }
        LastArrival(state).Tick.Set(arrival.Ticks);
        Set(AdmissionInitialized(state), L(AdmissionInitialized(state), 1), evidence);

        if (!OperationClaim.TryReadResolvedViolation(ready, out var notReady) || notReady.Ticks == 0)
        {
            if (C.Attached(Watcher(state))) evidence.Add(C.Detach(Watcher(state)));
            Cancel(state, evidence);
            return Finish();
        }
        var fresh = Equal(Copy(C.LastOpportunity(Surface(state))), Copy(arrival), evidence);
        if (!fresh)
        {
            Cancel(state, evidence);
            evidence.Add(Relation.UnresolvedRead(arrival, Surface(state), C.Current(Surface(state)).Carrier));
            return Finish();
        }

        var sameEpoch = EpochMatches(state, evidence);
        if (!sameEpoch)
        {
            var epoch = Parts(Epoch(state));
            ((CarrierLocation)epoch[0]).Tick.Set(C.Signed(R.CompletedBlocks(Retry(state))).Ticks);
            ((CarrierLocation)epoch[1]).Tick.Set(R.RetryAfter(Retry(state)).Ticks);
            Set((Carrier)epoch[2], L((Carrier)epoch[2], 1), evidence);
            Set(GrantUsed(state), L(GrantUsed(state), 0), evidence);
            Cancel(state, evidence);
            if (C.Attached(Watcher(state))) evidence.Add(C.Detach(Watcher(state)));
            notice = C.Attach(Watcher(state)); Add(InitialSyncs(state), 1, evidence);
            var anchorRead = (Relation)C.Parts((Relation)notice)[4]; evidence.Add(anchorRead);
            var available = C.Scalar(anchorRead, out var current);
            AnchorValue(state).Tick.Set(available ? current.Ticks : C.Current(Surface(state)).Ticks);
            AnchorValidity(state).Tick.Set(available ? 1 : 0);
            return Finish();
        }
        if (!C.Attached(Watcher(state)))
        {
            notice = C.Attach(Watcher(state)); Add(InitialSyncs(state), 1, evidence);
            // Reconnection within an existing epoch acknowledges current state
            // but cannot manufacture a change or another grant.
            Cancel(state, evidence);
            return Finish();
        }

        notice = C.Notify(Watcher(state), includeCurrent: false); Add(RevisionPolls(state), 1, evidence);
        var noticeFlag = (CarrierLocation)C.Parts((Relation)notice)[2];
        var notified = Equal(Copy(noticeFlag), L(noticeFlag.Carrier, 1), evidence);
        if (notified) Add(Notifications(state), 1, evidence);
        var armed = Positive(CandidateCount(state), evidence);
        if (armed) Add(ConfirmationReads(state), 1, evidence);
        // With no incoming change and no armed follow-up, the watcher does not
        // repeatedly inspect the selected value. A permit still validates its
        // current premises before any paid calculation can consume a token.
        if (!notified && !armed) return Finish();
        var valid = C.TryReceived(Watcher(state), out var observed, out var observedRead); evidence.Add(observedRead);
        var acceptedChange = false;
        if (!valid)
        {
            Cancel(state, evidence);
        }
        else if (!Equal(Copy(AnchorValidity(state)), L(AnchorValidity(state).Carrier, 1), evidence))
        {
            // An invalid anchor first needs an actual baseline. Mere recovery
            // of presence is not evidence that a previously bad cell changed.
            AnchorValue(state).Tick.Set(observed.Ticks); AnchorValidity(state).Tick.Set(1);
            Cancel(state, evidence);
        }
        else if (Equal(Copy(observed), Copy(AnchorValue(state)), evidence))
        {
            Cancel(state, evidence);
        }
        else if (!Zero(GrantUsed(state), evidence))
        {
            Set(CandidateCount(state), L(CandidateCount(state), 0), evidence);
        }
        else
        {
            if (armed)
            {
                var nextArrival = Calculate(arrival.Carrier, evidence, (CandidateArrival(state), 1), (L(arrival.Carrier, 1), 1));
                var distinctOpportunity = Calculate(Opportunity(state).Carrier, evidence,
                    (CandidateOpportunity(state), 1), (L(Opportunity(state).Carrier, 1), 1));
                var sameCell = Equal(Copy(observed), Copy(CandidateValue(state)), evidence);
                var subsequent = Equal(Copy(arrival), nextArrival, evidence) &&
                    AtLeast(Copy(Opportunity(state)), distinctOpportunity, evidence);
                if (sameCell && subsequent)
                {
                    Add(CandidateCount(state), 1, evidence);
                    var confirmations = Calculate(countBasis, evidence, (CandidateCount(state).ValueSlot, 1));
                    if (AtLeast(confirmations, Copy(RequiredCount(state)), evidence))
                    {
                        var maximum = Copy(MaximumProbe(state));
                        var capped = AtMost(maximum, L(countBasis, 2), evidence) ? maximum : L(countBasis, 2);
                        if (AtLeast(capped, L(countBasis, 1), evidence))
                        {
                            Set(Tokens(state), capped, evidence); Set(GrantUsed(state), L(GrantUsed(state), 1), evidence);
                            Add(Grants(state), 1, evidence); granted = true; acceptedChange = true;
                        }
                    }
                }
                Set(CandidateCount(state), L(CandidateCount(state), 0), evidence);
            }
            if (!granted && notified)
            {
                var changed = (CarrierLocation)C.Parts(C.LastEvent(Surface(state)))[6];
                if (Equal(Copy(changed), L(changed.Carrier, 1), evidence))
                {
                    CandidateValue(state).Tick.Set(observed.Ticks);
                    CandidateArrival(state).Tick.Set(arrival.Ticks);
                    CandidateOpportunity(state).Tick.Set(Opportunity(state).Ticks);
                    Set(CandidateCount(state), L(CandidateCount(state), 1), evidence);
                    acceptedChange = true;
                }
            }
        }
        if (notified && !acceptedChange) Add(SuppressedChanges(state), 1, evidence);
        return Finish();
    }

    /// <summary>Pure native-premise read. Neither rereading nor a successful
    /// prediction adds tokens, clears prior failures or moves a retry date.</summary>
    public static bool TryPermit(Relation state, CarrierLocation arrival, out Relation witness)
    {
        var evidence = new List<IElement>();
        var ready = R.Ready(Retry(state), Copy(Opportunity(state))); evidence.Add(ready);
        var cooling = OperationClaim.TryReadResolvedViolation(ready, out var violation) && violation.Ticks != 0;
        var admitted = !Zero(AdmissionInitialized(state), evidence) && Equal(Copy(arrival), Copy(LastArrival(state)), evidence);
        var fresh = Equal(Copy(C.LastOpportunity(Surface(state))), Copy(arrival), evidence);
        var epoch = EpochMatches(state, evidence);
        var unconsumed = Zero(ConsumedInitialized(state), evidence) || !Equal(Copy(LastConsumed(state)), Copy(arrival), evidence);
        var eligible = cooling && admitted && fresh && epoch && unconsumed && Positive(Tokens(state), evidence);
        var changed = false;
        if (eligible)
        {
            var available = C.TryReceived(Watcher(state), out var value, out var read); evidence.Add(read);
            changed = available && Equal(Copy(AnchorValidity(state)), L(AnchorValidity(state).Carrier, 1), evidence) &&
                !Equal(Copy(value), Copy(AnchorValue(state)), evidence);
        }
        else evidence.Add(new Placeholder());
        var permit = eligible && changed;
        witness = G(state, Copy(arrival), Copy(Opportunity(state)), L(RequiredCount(state).Carrier, permit ? 1 : 0),
            C.Signed(Tokens(state)), G(evidence.ToArray()));
        return permit;
    }

    /// <summary>The caller invokes this only after forming an actual paid
    /// forecast, retaining that promise as provenance. At most one token can
    /// be consumed for an arrival; missing later outcomes never refund it.</summary>
    public static Relation Consume(Relation state, CarrierLocation arrival, IElement paidPromise)
    {
        var permitted = TryPermit(state, arrival, out var permit);
        var evidence = new List<IElement> { permit };
        if (permitted)
        {
            var left = Calculate(Tokens(state), evidence, (Tokens(state).ValueSlot, 1),
                (Tokens(state).LiveTensionSlot, 1), (L(Tokens(state), 1), -1));
            Set(Tokens(state), left, evidence); Add(ProbesIssued(state), 1, evidence);
            LastConsumed(state).Tick.Set(arrival.Ticks);
            Set(ConsumedInitialized(state), L(ConsumedInitialized(state), 1), evidence);
        }
        return G(state, Copy(arrival), paidPromise, L(RequiredCount(state).Carrier, permitted ? 1 : 0),
            C.Signed(Tokens(state)), G(evidence.ToArray()));
    }

    private static bool EpochMatches(Relation state, List<IElement> evidence)
    {
        var p = Parts(Epoch(state));
        return !Zero((Carrier)p[2], evidence) && Equal(Copy((CarrierLocation)p[0]), C.Signed(R.CompletedBlocks(Retry(state))), evidence) &&
            Equal(Copy((CarrierLocation)p[1]), Copy(R.RetryAfter(Retry(state))), evidence);
    }
    private static void Cancel(Relation state, List<IElement> evidence)
    { Set(CandidateCount(state), L(CandidateCount(state), 0), evidence); Set(Tokens(state), L(Tokens(state), 0), evidence); }
    private static bool Zero(Carrier carrier, List<IElement> evidence) => Equal(C.Signed(carrier), L(carrier, 0), evidence);
    private static bool Positive(Carrier carrier, List<IElement> evidence) => AtLeast(C.Signed(carrier), L(carrier, 1), evidence);
    private static bool Equal(CarrierLocation a, CarrierLocation b, List<IElement> evidence)
    { var claim = OperationClaim.Equality(new Carrier(), a, b, new Carrier()); evidence.Add(claim); return C.Pass(claim); }
    private static bool AtLeast(CarrierLocation a, CarrierLocation b, List<IElement> evidence)
    { var claim = OperationClaim.AtLeast(new Carrier(), a, b, new Carrier()); evidence.Add(claim); return C.Pass(claim); }
    private static bool AtMost(CarrierLocation a, CarrierLocation b, List<IElement> evidence)
    { var claim = OperationClaim.AtMost(new Carrier(), a, b, new Carrier()); evidence.Add(claim); return C.Pass(claim); }
    private static CarrierLocation Calculate(Carrier basis, List<IElement> evidence, params (IElement Value, long Coefficient)[] terms)
    { var recipe = N.SumTo(basis, terms); var read = N.Read(recipe); evidence.Add(G(recipe, read)); return Copy(C.Value(read)); }
    private static void Add(Carrier carrier, long increment, List<IElement> evidence) => Set(carrier,
        Calculate(carrier, evidence, (carrier.ValueSlot, 1), (carrier.LiveTensionSlot, 1), (L(carrier, increment), 1)), evidence);
    private static void Set(Carrier carrier, CarrierLocation value, List<IElement> evidence)
    { var delta = Calculate(carrier, evidence, (value, 1), (carrier.ValueSlot, -1), (carrier.LiveTensionSlot, -1)); evidence.Add(Carrier.ReceiveTogether(G(delta))); }
    private static CarrierLocation Copy(CarrierLocation value) => L(value.Carrier, value.Ticks);
    private static CarrierLocation L(Carrier basis, long ticks) => new(basis, ticks);
    private static Relation G(params IElement[] items) => N.Group(items);
}
