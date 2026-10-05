using Gojo.Core;
using Gojo.Structures;
using N = ConnectionAssembly.NativeMath;
using C = ChangeSubscriptions.ChangeSurface;
using R = ChangeSubscriptions.ComparativeRetry;

namespace ChangeSubscriptions;

/// <summary>Authored local scheduling over one reusable native source and two
/// independent uses. No fixture phase, expected answer or report feeds this world.</summary>
internal static class NativeExperiment
{
    // World4 = G(operational structure, two captured pending promises, latest frame,
    // captured arrival). The captured coordinate rejects a stale shared envelope.
    // The outer envelope changes; source equations, sites, reviews, subscriptions
    // and cost counters persist. Retain releases optional latest-frame evidence.
    // Op12 = G(signal,arrival clock,validation clock,input sites,delta recipe,
    // source-direction read,source recipe,source sites,views,consumers,policy,
    // actual-use reads). Sites5 = G(delta,direction,prediction,presence,cost basis).
    public static Relation Create(bool throttle = true, int maximumRung = 5)
    {
        var signal = new Carrier(); var clock = new Carrier(); var progress = new Carrier();
        var flags = new Carrier(); var cost = new Carrier();
        var input = G(L(signal, 0), L(flags, 0), L(signal, 0), L(flags, 0));
        var ip = Parts(input);
        var deltaRecipe = N.SumTo(signal,
            (N.Present((CarrierLocation)ip[2], (CarrierLocation)ip[3]), 1),
            (N.Present((CarrierLocation)ip[0], (CarrierLocation)ip[1]), -1));
        var delta = L(signal, 0);
        var sourceDirection = C.DirectionRead(delta);
        var direction = L(N.Output(Parts(sourceDirection)[0]), 0);
        var prediction = L(signal, 0); var presence = L(flags, 0);
        var sourceRecipe = N.SumTo(signal, (delta, 1), (direction, 2));
        var sourceSites = G(delta, direction, prediction, presence, cost);
        var views = G(C.Create(prediction, presence, C.DirectionRead(prediction), clock),
            C.Create(prediction, presence, C.MagnitudeRead(prediction), clock),
            C.Create(prediction, presence, C.MagnitudeRead(prediction, 4), clock));
        var consumers = G(Parts(views).Take(2).Cast<Relation>().Select(view =>
        {
            var subscriber = C.Subscribe(view);
            var retry = R.Create(view, sourceRecipe, C.Current(view).Carrier, progress,
                maximumRung: maximumRung);
            // Consumer4 = G(surface,subscriber,retry,counters).
            // Counters6 = G(paid units,actual error,reference error,joins,leaves,skips).
            return (IElement)G(view, subscriber, retry,
                G(new Carrier(cost.UnitShape), new Carrier(), new Carrier(), new Carrier(), new Carrier(), new Carrier()));
        }).ToArray());
        var coarseSubscriber = C.Subscribe((Relation)Parts(views)[2]);
        var policy = G(L(flags, throttle ? 1 : 0), L(cost, 1), coarseSubscriber);
        var actualReads = G(C.DirectionRead(delta), C.MagnitudeRead(delta));
        var op = G(signal, L(clock, 0), L(progress, 0), input, deltaRecipe, sourceDirection,
            sourceRecipe, sourceSites, views, consumers, policy, actualReads);
        return G(op, G(new Placeholder(), new Placeholder()), new Placeholder(), L(clock, 0));
    }

    public static IElement[] Parts(Relation group) => N.Parts(group);
    public static IElement[] Op(Relation root) => Parts((Relation)Parts(root)[0]);
    public static Relation[] Consumers(Relation root) => Parts((Relation)Op(root)[9]).Cast<Relation>().ToArray();
    public static Relation[] Views(Relation root) => Parts((Relation)Op(root)[8]).Cast<Relation>().ToArray();
    public static Relation Surface(Relation consumer) => (Relation)Parts(consumer)[0];
    public static Relation Subscriber(Relation consumer) => (Relation)Parts(consumer)[1];
    public static Relation Retry(Relation consumer) => (Relation)Parts(consumer)[2];
    public static Carrier Counter(Relation consumer, int index) => (Carrier)Parts((Relation)Parts(consumer)[3])[index];
    public static CarrierLocation Arrival(Relation root) => (CarrierLocation)Op(root)[1];
    public static CarrierLocation Opportunity(Relation root) => (CarrierLocation)Op(root)[2];
    public static CarrierLocation Source(Relation root) => (CarrierLocation)Parts((Relation)Op(root)[7])[2];
    public static CarrierLocation SourcePresence(Relation root) => (CarrierLocation)Parts((Relation)Op(root)[7])[3];
    public static Relation CoarseSubscriber(Relation root) => (Relation)Parts((Relation)Op(root)[10])[2];
    public static Relation Retain(Relation root) => G(Parts(root)[0], Parts(root)[1], new Placeholder(), Parts(root)[3]);

    public static Relation Observe(ref Relation root, long? input)
    {
        var op = Op(root); var evidence = new List<IElement>();
        var admission = OperationClaim.Equality(new Carrier(), Parts(root)[3], Copy(Arrival(root)), new Carrier());
        if (!C.Pass(admission)) return Relation.UnresolvedRead(root, admission, Arrival(root).Carrier);
        evidence.Add(admission);
        var arrival = Sum(Arrival(root).Carrier, evidence, (Arrival(root), 1), (L(Arrival(root).Carrier, 1), 1));
        Arrival(root).Tick.Set(arrival.Ticks);
        var sites = Parts((Relation)op[3]);
        // These are explicitly retargetable input coordinates, not direct writes
        // to carrier value. The fixture supplies only the new observation.
        ((CarrierLocation)sites[0]).Tick.Set(((CarrierLocation)sites[2]).Ticks);
        ((CarrierLocation)sites[1]).Tick.Set(((CarrierLocation)sites[3]).Ticks);
        if (input.HasValue) ((CarrierLocation)sites[2]).Tick.Set(input.Value);
        ((CarrierLocation)sites[3]).Tick.Set(input.HasValue ? 1 : 0);
        var actualRead = N.Read((Relation)op[4]); evidence.Add(actualRead);
        var resolved = C.Scalar(actualRead, out var actual);
        var sourceSites = Parts((Relation)op[7]);
        if (resolved)
        {
            ((CarrierLocation)sourceSites[0]).Tick.Set(actual.Ticks);
            var next = Sum(Opportunity(root).Carrier, evidence,
                (Opportunity(root), 1), (L(Opportunity(root).Carrier, 1), 1));
            Opportunity(root).Tick.Set(next.Ticks);
        }
        var consumers = Consumers(root); var previous = Parts((Relation)Parts(root)[1]);
        var outcomes = new IElement[consumers.Length];
        for (var i = 0; i < consumers.Length; i++)
            outcomes[i] = Assess(root, consumers[i], previous[i], (Relation)Parts((Relation)op[11])[i],
                resolved, evidence);

        // Only after assessing frozen earlier promises may this arrival change
        // the source's prediction of the NEXT delta.
        IElement sourceRead = Relation.UnresolvedRead(actualRead, (Relation)op[6], (Carrier)op[0]);
        if (resolved)
        {
            var direction = C.EvaluateRead((Relation)op[5]); evidence.Add(direction);
            if (C.Scalar(direction, out var sign))
            {
                ((CarrierLocation)sourceSites[1]).Tick.Set(sign.Ticks);
                sourceRead = N.Read((Relation)op[6]); evidence.Add(sourceRead);
                if (C.Scalar((Relation)sourceRead, out var predicted)) Source(root).Tick.Set(predicted.Ticks);
                else resolved = false;
            }
            else resolved = false;
        }
        SourcePresence(root).Tick.Set(resolved ? 1 : 0);
        var advances = Views(root).Select(view => (IElement)C.Advance(view, Copy(arrival))).ToArray();
        var coarse = CoarseSubscriber(root);
        var coarseSync = C.Attached(coarse) ? C.Notify(coarse) : C.Attach(coarse);
        var connections = new IElement[consumers.Length]; var promises = new IElement[consumers.Length];
        var policy = Parts((Relation)op[10]);
        var throttle = OperationClaim.Equality(new Carrier(), policy[0],
            L(((CarrierLocation)policy[0]).Carrier, 1), new Carrier()); evidence.Add(throttle);
        for (var i = 0; i < consumers.Length; i++)
        {
            var consumer = consumers[i]; var subscription = Subscriber(consumer);
            var local = new List<IElement>();
            local.Add(C.Notify(subscription));
            var ready = R.Ready(Retry(consumer), Copy(Opportunity(root))); local.Add(ready);
            var eligible = !C.Pass(throttle) || C.Pass(ready);
            if (eligible && !C.Attached(subscription))
            { local.Add(C.Attach(subscription)); Add(Counter(consumer, 3), 1, evidence); }
            else if (!eligible && C.Attached(subscription))
            { local.Add(C.Detach(subscription)); Add(Counter(consumer, 4), 1, evidence); }
            var baseline = L(C.Current(Surface(consumer)).Carrier, 0);
            if (C.TryReceived(subscription, out var supplied, out var received))
            {
                var bill = Copy((CarrierLocation)policy[1]);
                Add(Counter(consumer, 0), bill, evidence);
                // Promise6 = G(issued arrival,baseline,supplied,bill,received witness,use).
                promises[i] = G(Copy(arrival), baseline, Copy(supplied), bill, received, consumer);
            }
            else
            {
                if (resolved && !eligible) Add(Counter(consumer, 5), 1, evidence);
                promises[i] = G(Copy(arrival), baseline, new Placeholder(), new Placeholder(), received, consumer);
            }
            connections[i] = G(consumer, ready, G(local.ToArray()));
        }
        // Frame9: arrival,validation-opportunity,actual read,earlier outcomes,
        // next source read,publication advances,connections,next promises,evidence.
        var frame = G(Copy(arrival), Copy(Opportunity(root)), actualRead, G(outcomes), sourceRead,
            G(advances), G(connections), G(promises), G(G(evidence.ToArray()), coarseSync));
        root = G(Parts(root)[0], G(promises), frame, Copy(arrival));
        return frame;
    }

    private static Relation Assess(Relation root, Relation consumer, IElement prior,
        Relation actualUseRead, bool resolved, List<IElement> evidence)
    {
        var basis = C.Current(Surface(consumer)).Carrier;
        if (prior is not Relation promise)
            return G(prior, new Placeholder(), new Placeholder(), new Placeholder(), new Placeholder(),
                new Placeholder(), L(basis, 0), L(basis, 0));
        var p = Parts(promise);
        var selected = resolved ? C.EvaluateRead(actualUseRead)
            : Relation.UnresolvedRead(actualUseRead, promise, basis);
        evidence.Add(selected);
        if (!C.Scalar(selected, out var actual))
        {
            IElement unknown = p[2] is CarrierLocation
                ? R.Observe(Retry(consumer), Copy(Opportunity(root)), null, null, (CarrierLocation)p[3], G(promise, selected))
                : new Placeholder();
            return G(promise, new Placeholder(), unknown, selected, new Placeholder(), new Placeholder(),
                L(basis, p[2] is CarrierLocation ? 1 : 0), L(basis, 0));
        }
        var aligned = Sum(basis, evidence, (actual, 1));
        var baselineError = Sum(basis, evidence, (p[1], 1), (aligned, -1));
        var suppliedError = p[2] is CarrierLocation supplied
            ? Sum(basis, evidence, (supplied, 1), (aligned, -1)) : baselineError;
        var response = OperationClaim.IntervalResponse(new Carrier(), Relation.Segment(basis, 0, 0),
            baselineError, suppliedError, new Carrier(basis.UnitShape), G(promise, selected));
        if (!OperationClaim.TryReadIntervalResponse(response, out _, out _, out _,
            out var baselineLoss, out var suppliedLoss, out _))
            throw new InvalidOperationException("An aligned authored loss failed to read.");
        Add(Counter(consumer, 1), suppliedLoss, evidence);
        Add(Counter(consumer, 2), baselineLoss, evidence);
        IElement review = new Placeholder();
        if (p[2] is CarrierLocation)
        {
            var before = Sum(basis, evidence, (baselineLoss, 1));
            var after = Sum(basis, evidence, (suppliedLoss, 1));
            review = R.Observe(Retry(consumer), Copy(Opportunity(root)), before, after,
                (CarrierLocation)p[3], G(promise, response));
        }
        // Outcome8 = G(promise,response,review,actual-use read,chosen loss,
        // reference loss,subscribed flag,evaluated flag).
        return G(promise, response, review, selected, Copy(suppliedLoss), Copy(baselineLoss),
            L(basis, p[2] is CarrierLocation ? 1 : 0), L(basis, 1));
    }

    private static void Add(Carrier carrier, long amount, List<IElement> evidence) => Add(carrier, L(carrier, amount), evidence);
    private static void Add(Carrier carrier, CarrierLocation amount, List<IElement> evidence)
    {
        var delta = Sum(carrier, evidence, (amount, 1));
        evidence.Add(Carrier.ReceiveTogether(G(delta)));
    }
    public static CarrierLocation Sum(Carrier target, List<IElement> evidence, params (IElement Value, long Coefficient)[] terms)
    {
        var recipe = N.SumTo(target, terms); var read = N.Read(recipe); evidence.Add(G(recipe, read));
        return Copy(C.Value(read));
    }
    public static CarrierLocation Copy(CarrierLocation value) => L(value.Carrier, value.Ticks);
    private static CarrierLocation L(Carrier carrier, long ticks) => new(carrier, ticks);
    private static Relation G(params IElement[] parts) => N.Group(parts);
}
