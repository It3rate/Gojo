using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using N = ConnectionAssembly.NativeMath;

namespace ChangeSubscriptions;

/// <summary>A supplied, host-scheduled observer over a native read. The reusable
/// surface publishes only selected-coordinate or validity changes. A revision
/// is publication history, not a count of validations or utility evidence.</summary>
public static class ChangeSurface
{
    // Read2 = G(native recipe, clamp span/open). Magnitude is signed amplitude;
    // an integer grain deliberately omits finer distinctions, retaining residue.
    public static Relation MagnitudeRead(CarrierLocation source, int grain = 1)
    {
        if (grain <= 0) throw new ArgumentOutOfRangeException(nameof(grain));
        var target = new Carrier(UnitShape.ResolutionOf(source.Carrier.UnitShape, grain));
        var recipe = G(Equation.Affine(source.Carrier, 0, grain, target, 0, 1), source, target, G());
        return G(recipe, new Placeholder());
    }

    public static Relation DirectionRead(CarrierLocation source)
    {
        var recipe = N.Reframe(source, new Carrier(source.Carrier.UnitShape));
        return G(recipe, Relation.Segment(N.Output(recipe), -1, 1));
    }

    public static Relation EvaluateRead(Relation selectedRead)
    {
        var p = Parts(selectedRead);
        if (p.Length != 2 || p[0] is not Relation recipe)
            return Relation.UnresolvedRead(selectedRead, selectedRead, Carrier.Unknown);
        var read = N.Read(recipe);
        if (!Scalar(read, out var value) || p[1] is Placeholder) return read;
        if (p[1] is not Relation span || !TimeStepSequence.TryReadSpan(span, out var low, out var high))
            return Relation.UnresolvedRead(read, selectedRead, N.Output(recipe));
        var lower = OperationClaim.AtLeast(new Carrier(), Copy(value), Copy(low), new Carrier());
        var upper = OperationClaim.AtMost(new Carrier(), Copy(value), Copy(high), new Carrier());
        if (!OperationClaim.TryReadResolvedViolation(lower, out var below) ||
            !OperationClaim.TryReadResolvedViolation(upper, out var above))
            return Relation.UnresolvedRead(read, G(lower, upper), N.Output(recipe));
        var selected = below.Ticks != 0 ? low : above.Ticks != 0 ? high : value;
        var omitted = N.Read(N.SumTo(value.Carrier, (value, 1), (selected, -1)));
        return new Relation(Copy(selected), G(selectedRead, read, lower, upper, omitted));
    }

    // Surface11 = G(source, source-validity, selected-read, held coordinate,
    // published-validity, revision carrier, current interval, last validation
    // opportunity, validation count, initialized carrier, latest event sites).
    // Interval is [first publication opportunity, latest validated opportunity].
    // LatestEvent10 = G(revision,time,before,after,valid-before,valid-after,
    // value-changed,validity-changed,old-interval,new-interval). Its sites are a
    // deliberately mutable one-event trace; returned witnesses copy them.
    public static Relation Create(CarrierLocation source, CarrierLocation validity,
        Relation selectedRead, Carrier clock)
    {
        var recipe = Parts(selectedRead).FirstOrDefault() as Relation;
        var target = recipe is null ? Carrier.Unknown : N.Output(recipe);
        var flags = new Carrier(); var revision = new Carrier();
        var interval = new Relation(L(clock, 0), L(clock, 0));
        var latest = G(L(revision, 0), L(clock, 0), L(target, 0), L(target, 0),
            L(flags, 0), L(flags, 0), L(flags, 0), L(flags, 0),
            new Relation(L(clock, 0), L(clock, 0)), new Relation(L(clock, 0), L(clock, 0)));
        return G(source, validity, selectedRead, L(target, 0), L(flags, 0), revision,
            interval, L(clock, 0), new Carrier(), new Carrier(), latest);
    }

    public static IElement[] Parts(Relation group) => N.Parts(group);
    public static CarrierLocation Source(Relation surface) => (CarrierLocation)Parts(surface)[0];
    public static CarrierLocation SourceValidity(Relation surface) => (CarrierLocation)Parts(surface)[1];
    public static Relation Read(Relation surface) => (Relation)Parts(surface)[2];
    public static CarrierLocation Current(Relation surface) => (CarrierLocation)Parts(surface)[3];
    public static CarrierLocation Validity(Relation surface) => (CarrierLocation)Parts(surface)[4];
    public static Carrier Revision(Relation surface) => (Carrier)Parts(surface)[5];
    public static Relation Interval(Relation surface) => (Relation)Parts(surface)[6];
    public static CarrierLocation LastOpportunity(Relation surface) => (CarrierLocation)Parts(surface)[7];
    public static Carrier ValidationCount(Relation surface) => (Carrier)Parts(surface)[8];
    private static Carrier Initialized(Relation surface) => (Carrier)Parts(surface)[9];
    public static Relation LastEvent(Relation surface) => (Relation)Parts(surface)[10];

    // Advance7 = G(surface,captured opportunity,admission claim,selected read,
    // changed flag,event snapshot/open,native evidence). Older/repeated or
    // unaligned opportunities cannot validate, mutate, or publish again.
    public static Relation Advance(Relation surface, CarrierLocation opportunity)
    {
        var evidence = new List<IElement>();
        var prior = Copy(LastOpportunity(surface));
        var first = OperationClaim.Equality(new Carrier(), Initialized(surface).ValueSlot,
            L(Initialized(surface), 0), new Carrier()); evidence.Add(first);
        var aligned = OperationClaim.Equality(new Carrier(), L(opportunity.Carrier, 0),
            L(prior.Carrier, 0), new Carrier()); evidence.Add(aligned);
        var minimum = Sum(prior.Carrier, evidence, (prior, 1), (L(prior.Carrier, 1), 1));
        var later = OperationClaim.AtLeast(new Carrier(), Copy(opportunity), minimum, new Carrier()); evidence.Add(later);
        var admission = G(aligned, first, later);
        if (!Pass(aligned) || (!Pass(first) && !Pass(later)))
            return G(surface, Copy(opportunity), admission, Relation.UnresolvedRead(opportunity, admission, Current(surface).Carrier),
                L(Validity(surface).Carrier, 0), new Placeholder(), G(evidence.ToArray()));

        var rawValidity = SourceValidity(surface);
        var admitted = OperationClaim.Equality(new Carrier(), Copy(rawValidity), L(rawValidity.Carrier, 1), new Carrier());
        evidence.Add(admitted);
        var selected = EvaluateRead(Read(surface)); evidence.Add(selected);
        var valid = Pass(admitted) && Scalar(selected, out _);
        var before = Copy(Current(surface)); var validBefore = Copy(Validity(surface));
        var after = valid ? Copy(Value(selected)) : before;
        var validAfter = L(validBefore.Carrier, valid ? 1 : 0);
        var sameValue = OperationClaim.Equality(new Carrier(), before, after, new Carrier());
        var sameValidity = OperationClaim.Equality(new Carrier(), validBefore, validAfter, new Carrier());
        evidence.Add(sameValue); evidence.Add(sameValidity);
        var valueChanged = !Pass(sameValue); var validityChanged = !Pass(sameValidity);
        var changed = valueChanged || validityChanged;
        var oldInterval = CaptureInterval(Interval(surface));
        if (valid) Current(surface).Tick.Set(after.Ticks);
        Validity(surface).Tick.Set(validAfter.Ticks);
        LastOpportunity(surface).Tick.Set(opportunity.Ticks);
        Add(ValidationCount(surface), L(ValidationCount(surface), 1), evidence);
        Set(Initialized(surface), L(Initialized(surface), 1), evidence);
        if (changed)
        {
            Add(Revision(surface), L(Revision(surface), 1), evidence);
            ((CarrierLocation)Interval(surface).Left).Tick.Set(opportunity.Ticks);
        }
        else if (Pass(first)) ((CarrierLocation)Interval(surface).Left).Tick.Set(opportunity.Ticks);
        ((CarrierLocation)Interval(surface).Right).Tick.Set(opportunity.Ticks);
        IElement change = new Placeholder();
        if (changed)
        {
            var latest = Parts(LastEvent(surface));
            var values = new[] { Signed(Revision(surface)), Copy(opportunity), before, after, validBefore, validAfter,
                L(validBefore.Carrier, valueChanged ? 1 : 0), L(validBefore.Carrier, validityChanged ? 1 : 0) };
            for (var i = 0; i < values.Length; i++) ((CarrierLocation)latest[i]).Tick.Set(values[i].Ticks);
            CopyIntervalInto(oldInterval, (Relation)latest[8]);
            CopyIntervalInto(Interval(surface), (Relation)latest[9]);
            change = G(values.Cast<IElement>().Concat(new IElement[] { oldInterval, CaptureInterval(Interval(surface)) }).ToArray());
        }
        // Attached publications are discovered by the kernel's local world;
        // there is no subscriber registry or a host list of notification targets.
        Current(surface).Carrier.Settle(); Revision(surface).Settle();
        return G(surface, Copy(opportunity), admission, selected, L(validBefore.Carrier, changed ? 1 : 0),
            change, G(evidence.ToArray()));
    }

    public static bool TryCurrent(Relation surface, out CarrierLocation value, out Relation witness)
    {
        var valid = OperationClaim.Equality(new Carrier(), Copy(Validity(surface)), L(Validity(surface).Carrier, 1), new Carrier());
        witness = Pass(valid)
            ? new Relation(Copy(Current(surface)), G(surface, valid, CaptureInterval(Interval(surface)), Signed(Revision(surface))))
            : Relation.UnresolvedRead(Current(surface), G(surface, valid), Current(surface).Carrier);
        return Scalar(witness, out value);
    }

    // Subscriber9 = G(surface, signed receipt, value publication, receipt read,
    // revision receipt, revision publication, acknowledged revision,
    // notification count, join count). Attach/detach edits the two actual
    // publications; no subscribed boolean substitutes for participation.
    public static Relation Subscribe(Relation surface)
    {
        var source = Current(surface); var valid = Validity(surface);
        var receipt = new Carrier(source.Carrier.UnitShape);
        var publication = Relation.LiveOutput(new Relation(source, valid),
            Equation.Product(receipt, Equation.Affine(source.Carrier, 0, 1, source.Carrier, 0, 1), Equation.UnitPort(valid.Carrier)));
        var revisionReceipt = new Carrier(Revision(surface).UnitShape);
        var revisionPublication = Relation.LiveOutput(Revision(surface).ValueSlot,
            Equation.Affine(Revision(surface), 0, 1, revisionReceipt, 0, 1));
        return G(surface, receipt, publication, N.SumTo(source.Carrier, (receipt.ValueSlot, 1), (receipt.LiveTensionSlot, 1)),
            revisionReceipt, revisionPublication, L(Revision(surface), 0), new Carrier(), new Carrier());
    }

    public static Relation Surface(Relation subscriber) => (Relation)Parts(subscriber)[0];
    public static Carrier Receipt(Relation subscriber) => (Carrier)Parts(subscriber)[1];
    public static Relation Publication(Relation subscriber) => (Relation)Parts(subscriber)[2];
    public static Carrier RevisionReceipt(Relation subscriber) => (Carrier)Parts(subscriber)[4];
    public static Relation RevisionPublication(Relation subscriber) => (Relation)Parts(subscriber)[5];
    public static CarrierLocation AcknowledgedRevision(Relation subscriber) => (CarrierLocation)Parts(subscriber)[6];
    public static Carrier NotificationCount(Relation subscriber) => (Carrier)Parts(subscriber)[7];
    public static Carrier JoinCount(Relation subscriber) => (Carrier)Parts(subscriber)[8];
    public static bool Attached(Relation subscriber) => Receipt(subscriber).IsAttached(Publication(subscriber)) &&
        RevisionReceipt(subscriber).IsAttached(RevisionPublication(subscriber));

    // Sync6 = G(subscriber,initial-sync flag,changed flag,revision,current/open,evidence).
    // Initial synchronization acknowledges current state without pretending
    // that a late join created a new publication or useful prediction.
    public static Relation Attach(Relation subscriber)
    {
        if (Attached(subscriber)) return Notify(subscriber);
        var evidence = new List<IElement>();
        Receipt(subscriber).Attach(Publication(subscriber));
        RevisionReceipt(subscriber).Attach(RevisionPublication(subscriber));
        Receipt(subscriber).Settle(); RevisionReceipt(subscriber).Settle();
        AcknowledgedRevision(subscriber).Tick.Set(Signed(Revision(Surface(subscriber))).Ticks);
        Add(JoinCount(subscriber), L(JoinCount(subscriber), 1), evidence);
        TryReceived(subscriber, out _, out var read);
        return G(subscriber, L(NotificationCount(subscriber), 1), L(NotificationCount(subscriber), 0),
            Copy(AcknowledgedRevision(subscriber)), read, G(evidence.ToArray()));
    }

    public static Relation Detach(Relation subscriber)
    {
        var before = G(Signed(Receipt(subscriber)), Signed(RevisionReceipt(subscriber)), Copy(AcknowledgedRevision(subscriber)));
        if (Receipt(subscriber).IsAttached(Publication(subscriber))) Receipt(subscriber).Detach(Publication(subscriber));
        if (RevisionReceipt(subscriber).IsAttached(RevisionPublication(subscriber))) RevisionReceipt(subscriber).Detach(RevisionPublication(subscriber));
        return G(subscriber, before, Publication(subscriber), RevisionPublication(subscriber));
    }

    public static Relation Notify(Relation subscriber)
    {
        var evidence = new List<IElement>(); var surface = Surface(subscriber);
        var revision = Sum(Revision(surface), evidence, (RevisionReceipt(subscriber).ValueSlot, 1), (RevisionReceipt(subscriber).LiveTensionSlot, 1));
        var same = OperationClaim.Equality(new Carrier(), Copy(AcknowledgedRevision(subscriber)), revision, new Carrier()); evidence.Add(same);
        var changed = Attached(subscriber) && !Pass(same);
        if (changed)
        {
            Add(NotificationCount(subscriber), L(NotificationCount(subscriber), 1), evidence);
            AcknowledgedRevision(subscriber).Tick.Set(revision.Ticks);
        }
        TryReceived(subscriber, out _, out var read);
        return G(subscriber, L(NotificationCount(subscriber), 0), L(NotificationCount(subscriber), changed ? 1 : 0),
            revision, read, G(evidence.ToArray()));
    }

    public static bool TryReceived(Relation subscriber, out CarrierLocation value, out Relation witness)
    {
        var surface = Surface(subscriber);
        var evidence = new List<IElement>();
        var revision = Sum(Revision(surface), evidence, (RevisionReceipt(subscriber).ValueSlot, 1), (RevisionReceipt(subscriber).LiveTensionSlot, 1));
        var currentRevision = OperationClaim.Equality(new Carrier(), revision, Signed(Revision(surface)), new Carrier()); evidence.Add(currentRevision);
        var available = TryCurrent(surface, out _, out var current);
        var read = N.Read((Relation)Parts(subscriber)[3]); evidence.Add(read);
        witness = Attached(subscriber) && available && Pass(currentRevision) && Scalar(read, out _)
            ? new Relation(Copy(Value(read)), G(subscriber, current, G(evidence.ToArray())))
            : Relation.UnresolvedRead(subscriber, G(current, G(evidence.ToArray())), Current(surface).Carrier);
        return Scalar(witness, out value);
    }

    private static Relation CaptureInterval(Relation interval) => new(Copy((CarrierLocation)interval.Left), Copy((CarrierLocation)interval.Right));
    private static void CopyIntervalInto(Relation source, Relation target)
    { ((CarrierLocation)target.Left).Tick.Set(((CarrierLocation)source.Left).Ticks); ((CarrierLocation)target.Right).Tick.Set(((CarrierLocation)source.Right).Ticks); }
    private static CarrierLocation Sum(Carrier target, List<IElement> evidence, params (IElement Value, long Coefficient)[] terms)
    { var recipe = N.SumTo(target, terms); var read = N.Read(recipe); evidence.Add(G(recipe, read)); return Copy(Value(read)); }
    private static void Add(Carrier site, CarrierLocation amount, List<IElement> evidence) => Set(site, Sum(site, evidence,
        (site.ValueSlot, 1), (site.LiveTensionSlot, 1), (amount, 1)), evidence);
    private static void Set(Carrier site, CarrierLocation target, List<IElement> evidence)
    { var delta = Sum(site, evidence, (target, 1), (site.ValueSlot, -1), (site.LiveTensionSlot, -1)); evidence.Add(Carrier.ReceiveTogether(G(delta))); }
    public static CarrierLocation Signed(Carrier carrier) => Copy(Value(N.Read(N.SumTo(carrier, (carrier.ValueSlot, 1), (carrier.LiveTensionSlot, 1)))));
    public static bool Pass(Relation claim) => OperationClaim.TryReadResolvedViolation(claim, out var violation) && violation.Ticks == 0;
    public static bool Scalar(Relation read, out CarrierLocation value) => Equation.TryReadPrediction(read, out value, out _);
    public static CarrierLocation Value(Relation read) => Scalar(read, out var value) ? value : throw new InvalidOperationException("A supplied native read is unresolved.");
    private static CarrierLocation Copy(CarrierLocation value) => L(value.Carrier, value.Ticks);
    private static CarrierLocation L(Carrier basis, long ticks) => new(basis, ticks);
    private static Relation G(params IElement[] elements) => N.Group(elements);
}
