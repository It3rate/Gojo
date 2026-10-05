using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using N = ConnectionAssembly.NativeMath;

namespace NegotiatedPorts;

/// <summary>Supplied local negotiation policy. Native groups, equations, ports,
/// claims and drained transfers carry the current world; host loops schedule
/// the finite neighborhood. No prior observer rows enter the learner.</summary>
public static class MarketProbe
{
    // World4: operational graph, two pending issues, latest frame, version.
    // Operational10: input sites, source equations, providers, offers,
    // consumers, routes, resources, cursor, successor equation, transfer edges.
    public static Relation Create(int firstCursor = 0, int secondCursor = 0)
    {
        if (firstCursor is < 0 or > 3 || secondCursor is < 0 or > 3)
            throw new ArgumentOutOfRangeException(nameof(firstCursor));
        var source = new Carrier();
        var packet = new Carrier();
        var credit = new Carrier();
        var reservoir = new Carrier(credit.UnitShape);
        var spent = new Carrier(credit.UnitShape);
        reservoir.Add(4096);
        var sites = Enumerable.Range(0, 3).Select(_ => new Relation(
            new CarrierLocation(source, 0), new CarrierLocation(packet, 0))).ToArray();
        var present = sites.Select(s => N.Present((CarrierLocation)s.Left, (CarrierLocation)s.Right)).ToArray();
        var a = N.SumTo(source, (present[1], 1), (present[0], -1));
        var d = N.SumTo(source, (present[2], 1), (present[1], -1));
        var extrapolated = N.SumTo(source, (d, 2), (a, -1));
        // Provider3: wallet, occupied service slot, reserve cap.
        var providers = Enumerable.Range(0, 3).Select(_ => G(new Carrier(credit.UnitShape),
            new Carrier(), new CarrierLocation(credit, 32))).ToArray();
        var offers = new[]
        {
            BuildOffer(providers[0], d, source, credit, packet, 1, 3, true),
            BuildOffer(providers[1], N.SumTo(source, (present[2], 1), (present[1], -1)), source, credit, packet, 2, 5, true),
            BuildOffer(providers[2], d, source, credit, packet, 2, 5, false),
            BuildOffer(providers[2], extrapolated, source, credit, packet, 3, 7, false)
        };
        var consumers = new[] { BuildConsumer(source, credit, packet, 4, firstCursor, true),
            BuildConsumer(source, credit, packet, 8, secondCursor, false) };
        var routes = consumers.SelectMany(consumer => offers.Select(offer =>
        {
            var gate = new Carrier();
            var receipt = new Carrier(source.UnitShape);
            var output = (CarrierLocation)Parts(offer)[3];
            var proxy = (Relation)Parts(offer)[7];
            // The actual exposed site is an executable operand of the live
            // publication; retargeting that site reaches attached consumers.
            // This explicit affine proxy supplies the source-unit interpretation.
            var publication = Relation.LiveOutput(new Relation(output, gate.ValueSlot),
                Equation.Product(receipt, proxy, Equation.UnitPort(gate)));
            return G(consumer, offer, gate, receipt, publication, output,
                N.SumTo(source, (receipt.ValueSlot, 1), (receipt.LiveTensionSlot, 1)),
                G(output, proxy));
        })).ToArray();
        var currencyCarriers = new[] { reservoir, spent }.Concat(consumers.Select(Wallet)).Concat(providers.Select(Reserve)).ToArray();
        var total = N.SumTo(credit, currencyCarriers.SelectMany(c => new[]
            { (Value: (IElement)c.ValueSlot, Coefficient: 1L), (Value: (IElement)c.LiveTensionSlot, Coefficient: 1L) }).ToArray());
        var resources = G(reservoir, spent, credit, new CarrierLocation(credit, 4096), total,
            G(currencyCarriers.Cast<IElement>().ToArray()));
        var clock = new CarrierLocation(new Carrier(), 0);
        var edges = new List<IElement>();
        foreach (var from in currencyCarriers)
        foreach (var to in currencyCarriers)
            if (!ReferenceEquals(from, to)) edges.Add(G(from, to,
                new Relation(Relation.Segment(from, 0, 1), Relation.Segment(to, 0, 1))));
        var operational = G(G(sites), G(a, d, extrapolated), G(providers), G(offers), G(consumers), G(routes), resources,
            clock, N.SumTo(clock.Carrier, (clock, 1), (new CarrierLocation(clock.Carrier, 1), 1)), G(edges.ToArray()));
        var root = G(operational, G(new Placeholder(), new Placeholder()), new Placeholder(), Copy(clock));
        var evidence = new List<IElement>();
        foreach (var consumer in consumers) Transfer(root, reservoir, Wallet(consumer), Cash(root, 8), evidence);
        foreach (var provider in providers) Transfer(root, reservoir, Reserve(provider), Cash(root, 4), evidence);
        return G(operational, Parts(root)[1], G(evidence.ToArray()), Copy(clock));
    }

    // Offer9: provider, projection recipe, native clamp constraints, live result,
    // presence, ask, operating cost, explicit proxy-to-source calibration,
    // original recipe. Direction proxy != exact change; correspondence is public.
    private static Relation BuildOffer(Relation provider, Relation recipe, Carrier source, Carrier credit,
        Carrier packet, long cost, long ask, bool direction)
    {
        var basis = direction ? new Carrier() : source;
        var output = new CarrierLocation(basis, 0);
        var price = new Carrier(credit.UnitShape);
        price.Add((ulong)ask); // Authored informational price, not resource currency.
        var limits = direction ? G(Relation.Segment(basis, -1, 1)) : G();
        return G(provider, N.Reframe(recipe, basis), limits, output, new CarrierLocation(packet, 0), price,
            new CarrierLocation(credit, cost), Equation.Affine(basis, 0, 1, source, 0, 1), recipe);
    }

    // Consumer10: wallet, wallet cap, read basis+constraints, exploratory bid,
    // learned willingness, local offer cursor, completed-service counter,
    // exploration cadence, current external demand, selected input/basis frame.
    private static Relation BuildConsumer(Carrier source, Carrier credit, Carrier packet, long bid, int cursor, bool direction)
    {
        var basis = direction ? new Carrier() : source;
        var willingness = new Carrier(credit.UnitShape);
        willingness.Add((ulong)bid);
        var position = new Carrier();
        position.Add((ulong)cursor);
        return G(new Carrier(credit.UnitShape), new CarrierLocation(credit, 16),
            G(Equation.Affine(source, 0, 1, basis, 0, 1), direction ? G(Relation.Segment(basis, -1, 1)) : G(), basis),
            new CarrierLocation(credit, bid), willingness, position, new Carrier(),
            new CarrierLocation(credit, 4), new CarrierLocation(packet, 0), Relation.Segment(source, 0, 1));
    }

    public static IElement[] Parts(Relation group) => N.Parts(group);
    private static IElement[] Op(Relation root) => Parts(Operational(root));
    public static Relation Operational(Relation root) => (Relation)Parts(root)[0];
    public static Relation InputSites(Relation root) => (Relation)Op(root)[0];
    public static Relation[] SourceEquations(Relation root) => Parts((Relation)Op(root)[1]).Cast<Relation>().ToArray();
    public static Relation[] Producers(Relation root) => Parts((Relation)Op(root)[2]).Cast<Relation>().ToArray();
    public static Relation[] Offers(Relation root) => Parts((Relation)Op(root)[3]).Cast<Relation>().ToArray();
    public static Relation[] Consumers(Relation root) => Parts((Relation)Op(root)[4]).Cast<Relation>().ToArray();
    public static Relation[] Routes(Relation root) => Parts((Relation)Op(root)[5]).Cast<Relation>().ToArray();
    public static Carrier Wallet(Relation consumer) => (Carrier)Parts(consumer)[0];
    public static Carrier Reserve(Relation provider) => (Carrier)Parts(provider)[0];
    public static Carrier Occupancy(Relation provider) => (Carrier)Parts(provider)[1];
    public static Carrier Ask(Relation offer) => (Carrier)Parts(offer)[5];
    public static CarrierLocation OfferPort(Relation offer) => (CarrierLocation)Parts(offer)[3];
    public static CarrierLocation OfferPresence(Relation offer) => (CarrierLocation)Parts(offer)[4];
    public static Relation Recipe(Relation offer) => (Relation)Parts(offer)[8];
    public static Relation Provider(Relation offer) => (Relation)Parts(offer)[0];
    public static Relation ConsumerRead(Relation consumer) => (Relation)Parts(consumer)[2];
    public static Carrier Willingness(Relation consumer) => (Carrier)Parts(consumer)[4];
    public static Carrier OfferCursor(Relation consumer) => (Carrier)Parts(consumer)[5];
    public static Carrier ServiceCount(Relation consumer) => (Carrier)Parts(consumer)[6];
    public static Carrier Gate(Relation route) => (Carrier)Parts(route)[2];
    public static Carrier Receipt(Relation route) => (Carrier)Parts(route)[3];
    public static Relation Publication(Relation route) => (Relation)Parts(route)[4];
    public static Relation RouteConsumer(Relation route) => (Relation)Parts(route)[0];
    public static Relation RouteOffer(Relation route) => (Relation)Parts(route)[1];
    public static bool Attached(Relation route) => Receipt(route).IsAttached(Publication(route));
    public static Relation? SelectedRoute(Relation root, Relation consumer) => Routes(root)
        .SingleOrDefault(route => ReferenceEquals(RouteConsumer(route), consumer) && Attached(route));
    public static Relation[] Pending(Relation root) => Parts((Relation)Parts(root)[1]).OfType<Relation>().ToArray();
    public static Carrier Reservoir(Relation root) => (Carrier)Parts((Relation)Op(root)[6])[0];
    public static Carrier Spent(Relation root) => (Carrier)Parts((Relation)Op(root)[6])[1];
    private static Carrier Credit(Relation root) => (Carrier)Parts((Relation)Op(root)[6])[2];
    public static Relation Conservation(Relation root)
    {
        var money = Parts((Relation)Op(root)[6]);
        return OperationClaim.Equality(new Carrier(), Value(N.Read((Relation)money[4])), money[3], new Carrier());
    }
    public static long Version(Relation root) => ((CarrierLocation)Op(root)[7]).Ticks;

    /// <summary>Pure diagnostic forecast. It does not set the exposed live port,
    /// spend resources, refresh ability or imply that computation was purchased.</summary>
    public static Relation Forecast(Relation offer)
    {
        var evidence = new List<IElement>();
        var read = N.Read((Relation)Parts(offer)[1]);
        if (!Scalar(read, out var value)) return read;
        var clamped = Clamp(value, (Relation)Parts(offer)[2], evidence);
        return new Relation(clamped, G(read, G(evidence.ToArray())));
    }

    public static bool TryObserve(Relation root, long? input, bool demand0, bool demand1, long funding0, long funding1,
        out Relation next, out Relation frame)
    {
        next = root;
        frame = Relation.UnresolvedRead(root, root, Carrier.Unknown);
        if (funding0 < 0 || funding1 < 0 || !Pass(OperationClaim.Equality(new Carrier(), Parts(root)[3], Op(root)[7], new Carrier())))
            return false;
        var evidence = new List<IElement>();
        var nextTime = Value(N.Read((Relation)Op(root)[8]));
        foreach (var route in Routes(root)) ClosePulse(root, route, evidence);
        foreach (var offer in Offers(root)) OfferPresence(offer).Tick.Set(0);
        foreach (var provider in Producers(root)) Set(root, Occupancy(provider), Cash(root, 0), evidence);
        var consumers = Consumers(root);
        var previousIssues = Parts((Relation)Parts(root)[1]);
        var outcomes = consumers.Select((consumer, index) =>
            Evaluate(root, consumer, previousIssues[index] as Relation, input, nextTime, evidence)).ToArray();
        AdmitInput(root, input);
        ((CarrierLocation)Op(root)[7]).Tick.Set(nextTime.Ticks);
        var demands = new[] { demand0, demand1 };
        var funding = new[] { funding0, funding1 };
        for (var i = 0; i < consumers.Length; i++)
        {
            ((CarrierLocation)Parts(consumers[i])[8]).Tick.Set(demands[i] ? 1 : 0);
            var headroom = Sum(root, evidence, (Parts(consumers[i])[1], 1), (Wallet(consumers[i]).ValueSlot, -1));
            var available = Minimum(root, Cash(root, funding[i]), headroom, evidence);
            available = Minimum(root, available, AsCash(root, Reservoir(root).ValueSlot, evidence), evidence);
            Transfer(root, Reservoir(root), Wallet(consumers[i]), available, evidence);
        }
        var issues = new Relation[2];
        var decisions = new Relation[2];
        // Admission order is an exposed alternating clock perspective, not a
        // fitness ranking or classifier of the input signal.
        var parity = Equation.Affine(nextTime.Carrier, 0, 2, Credit(root), 0, 1);
        Equation.TryPredict(parity, nextTime, Credit(root), out var parityRead);
        Equation.TryReadPrediction(parityRead, out _, out var parityResidue);
        var odd = parityResidue is CarrierLocation remainder && remainder.Ticks != 0;
        foreach (var index in odd ? new[] { 0, 1 } : new[] { 1, 0 })
            (issues[index], decisions[index]) = Issue(root, consumers[index], input, nextTime, evidence);
        var conservation = Conservation(root);
        frame = G(Copy(nextTime), input.HasValue ? new CarrierLocation(Source(root), input.Value) : new Placeholder(),
            G(outcomes), G(decisions), G(evidence.ToArray()), conservation, parityRead);
        next = G(Operational(root), G(issues), frame, Copy(nextTime));
        return true;
    }

    private static void AdmitInput(Relation root, long? input)
    {
        var sites = Parts(InputSites(root)).Cast<Relation>().ToArray();
        if (!input.HasValue)
        { foreach (var site in sites) ((CarrierLocation)site.Right).Tick.Set(0); return; }
        for (var i = 0; i < 2; i++)
        {
            ((CarrierLocation)sites[i].Left).Tick.Set(((CarrierLocation)sites[i + 1].Left).Ticks);
            ((CarrierLocation)sites[i].Right).Tick.Set(((CarrierLocation)sites[i + 1].Right).Ticks);
        }
        ((CarrierLocation)sites[2].Left).Tick.Set(input.Value);
        ((CarrierLocation)sites[2].Right).Tick.Set(1);
    }

    // Issue11: time, current level/open, consumer, supplied route/open,
    // delivered source-basis forecast/open, frozen consumer interpretation,
    // service ask, contact expense, frozen demand, recipe/correspondence, paid.
    private static (Relation Issue, Relation Decision) Issue(Relation root, Relation consumer, long? input,
        CarrierLocation time, List<IElement> evidence)
    {
        var contacts = new List<IElement>();
        var incumbent = SelectedRoute(root, consumer);
        var fee = Cash(root, 0);
        var price = Cash(root, 0);
        Relation? selected = null;
        var demanded = ((CarrierLocation)Parts(consumer)[8]).Ticks == 1;
        var ready = input.HasValue && Scalar(N.Read(SourceEquations(root)[1]), out _);
        var challenge = incumbent is null || AtLeast(root, AsCash(root, ServiceCount(consumer).ValueSlot, evidence),
            (CarrierLocation)Parts(consumer)[7], evidence);
        if (demanded && ready && challenge)
        {
            var offers = Offers(root);
            var offer = offers[checked((int)OfferCursor(consumer).Value)];
            if (incumbent is not null && ReferenceEquals(RouteOffer(incumbent), offer))
            {
                Advance(root, consumer, evidence);
                offer = offers[checked((int)OfferCursor(consumer).Value)];
            }
            var bid = (CarrierLocation)Parts(consumer)[3];
            var contactCost = Cash(root, 1);
            var paidContact = Affordable(root, Wallet(consumer), contactCost, evidence);
            if (paidContact)
            {
                Transfer(root, Wallet(consumer), Spent(root), contactCost, evidence);
                fee = contactCost;
                var ask = AsCash(root, Ask(offer).ValueSlot, evidence);
                var fits = AtMost(root, ask, bid, evidence);
                if (!fits) LowerAsk(root, offer, evidence);
                var free = ProviderFree(Provider(offer), evidence);
                if (fits && free && Affordable(root, Wallet(consumer), ask, evidence) &&
                    Scalar(Forecast(offer), out _)) selected = FindRoute(root, consumer, offer);
                contacts.Add(G(offer, Copy(bid), Copy(ask), contactCost,
                    new CarrierLocation(Credit(root), fits ? 1 : 0),
                    new CarrierLocation(Credit(root), free ? 1 : 0),
                    new CarrierLocation(Credit(root), selected is null ? 0 : 1)));
                Advance(root, consumer, evidence);
                Set(root, ServiceCount(consumer), Cash(root, 0), evidence);
            }
            else contacts.Add(G(offer, bid, Copy(AsCash(root, Ask(offer).ValueSlot, evidence)), contactCost,
                new Placeholder(), new Placeholder(), new CarrierLocation(Credit(root), 0)));
        }
        if (demanded && ready && selected is null && incumbent is not null)
        {
            var offer = RouteOffer(incumbent);
            var ask = AsCash(root, Ask(offer).ValueSlot, evidence);
            if (AtMost(root, ask, AsCash(root, Willingness(consumer).ValueSlot, evidence), evidence) &&
                Affordable(root, Wallet(consumer), ask, evidence) && ProviderFree(Provider(offer), evidence) &&
                Scalar(Forecast(offer), out _)) selected = incumbent;
        }
        if (!ReferenceEquals(selected, incumbent) && incumbent is not null)
        {
            Receipt(incumbent).Detach(Publication(incumbent));
            Set(root, ServiceCount(consumer), Cash(root, 0), evidence);
        }
        IElement forecast = input.HasValue ? new CarrierLocation(Source(root), 0) : new Placeholder();
        IElement recipe = new Placeholder();
        if (selected is not null)
        {
            var offer = RouteOffer(selected);
            price = AsCash(root, Ask(offer).ValueSlot, evidence);
            var cost = (CarrierLocation)Parts(offer)[6];
            var provider = Provider(offer);
            var balanceAfter = Sum(root, evidence, (Reserve(provider).ValueSlot, 1), (price, 1), (cost, -1));
            var surplus = Sum(root, evidence, (balanceAfter, 1), (Parts(provider)[2], -1));
            surplus = Maximum(root, Cash(root, 0), surplus, evidence);
            Transfer(root, Reserve(provider), Reservoir(root), surplus, evidence);
            Transfer(root, Wallet(consumer), Reserve(provider), price, evidence);
            Transfer(root, Reserve(provider), Spent(root), cost, evidence);
            // Only selected paid computation updates a live exposed result.
            var computed = Forecast(offer);
            OfferPort(offer).Tick.Set(Value(computed).Ticks);
            OfferPresence(offer).Tick.Set(1);
            Equation.TryPredict((Relation)Parts(offer)[7], Copy(OfferPort(offer)), Source(root), out var proxy);
            if (!Attached(selected)) Receipt(selected).Attach(Publication(selected));
            Set(root, Gate(selected), Cash(root, 1), evidence);
            Set(root, Occupancy(provider), Cash(root, 1), evidence);
            forecast = Copy(Value(N.Read((Relation)Parts(selected)[6])));
            recipe = G(offer, computed, proxy, (Relation)Parts(selected)[7]);
        }
        var issue = G(Copy(time), input.HasValue ? new CarrierLocation(Source(root), input.Value) : new Placeholder(),
            consumer, selected is not null ? selected : new Placeholder(), forecast,
            CaptureInterpretation(ConsumerRead(consumer)), Copy(price), Copy(fee),
            Copy((CarrierLocation)Parts(consumer)[8]), recipe,
            new CarrierLocation(Credit(root), selected is null ? 0 : 1));
        return (issue, G(consumer, incumbent is null ? new Placeholder() : incumbent,
            selected is null ? new Placeholder() : selected, G(contacts.ToArray()), issue));
    }

    // Outcome10: issue/open, actual source change/open, paired consumer
    // response/open, gross calibrated benefit/open, ongoing net/open, total
    // net/open, willingness after, native evaluation/timeline, evaluated, retained.
    private static Relation Evaluate(Relation root, Relation consumer, Relation? issue, long? input,
        CarrierLocation time, List<IElement> evidence)
    {
        var zero = Cash(root, 0);
        if (issue is null) return G(new Placeholder(), new Placeholder(), new Placeholder(), new Placeholder(),
            new Placeholder(), new Placeholder(), Copy(Willingness(consumer).ValueSlot), new Placeholder(), zero, zero);
        var p = Parts(issue);
        var route = p[3] as Relation;
        if (p[1] is not CarrierLocation last || p[4] is not CarrierLocation prediction || !input.HasValue)
        {
            if (route is not null && Attached(route)) Receipt(route).Detach(Publication(route));
            var unresolved = Relation.UnresolvedRead(input.HasValue ? new CarrierLocation(Source(root), input.Value) : new Placeholder(),
                issue, Source(root));
            var timeline = TimeStepSequence.Step(new Carrier(), time.Carrier, ((CarrierLocation)p[0]).Ticks,
                time.Ticks, issue, p[5], unresolved, unresolved);
            return G(issue, new Placeholder(), unresolved, new Placeholder(), new Placeholder(), new Placeholder(),
                Copy(Willingness(consumer).ValueSlot), timeline, zero, zero);
        }
        var local = new List<IElement>();
        var actualEquation = N.SumTo(Source(root), (new CarrierLocation(Source(root), input.Value), 1), (last, -1));
        var actualRead = N.Read(actualEquation);
        var actual = Value(actualRead);
        var interpretation = (Relation)p[5];
        var projectedActual = Interpret(actual, interpretation, local);
        var projectedPrediction = Interpret(prediction, interpretation, local);
        var baselineError = NativeSum(projectedActual.Carrier, local, (projectedActual, -1));
        var deliveredError = NativeSum(projectedActual.Carrier, local, (projectedPrediction, 1), (projectedActual, -1));
        var response = OperationClaim.IntervalResponse(new Carrier(), Relation.Segment(projectedActual.Carrier, 0, 0),
            baselineError, deliveredError, new Carrier(), G(actualEquation, prediction, projectedPrediction, p[9]));
        if (!OperationClaim.TryReadIntervalResponse(response, out _, out _, out _, out _, out _, out var reduction))
            throw new InvalidOperationException("The finite experimental response did not resolve.");
        var gross = NativeSum(Credit(root), local, (reduction, 4));
        var ongoingNet = Sum(root, local, (gross, 1), (p[6], -1));
        var totalNet = Sum(root, local, (ongoingNet, 1), (p[7], -1));
        var paid = ((CarrierLocation)p[10]).Ticks == 1;
        var retained = false;
        if (paid && route is not null)
        {
            var bid = Maximum(root, zero, Minimum(root, Cash(root, 10), gross, local), local);
            Set(root, Willingness(consumer), bid, evidence);
            retained = AtLeast(root, ongoingNet, Cash(root, 1), local);
            var count = Sum(root, local, (ServiceCount(consumer).ValueSlot, 1), (Cash(root, 1), 1));
            Set(root, ServiceCount(consumer), count, evidence);
            if (!retained)
            {
                if (Attached(route)) Receipt(route).Detach(Publication(route));
                Set(root, ServiceCount(consumer), zero, evidence);
                if (AtMost(root, gross, Cash(root, -1), local)) LowerAsk(root, RouteOffer(route), evidence);
            }
        }
        var step = TimeStepSequence.Step(new Carrier(), time.Carrier, ((CarrierLocation)p[0]).Ticks, time.Ticks,
            issue, p[9], G(actualRead, response, gross, ongoingNet, totalNet), response);
        return G(issue, Copy(actual), response, Copy(gross), Copy(ongoingNet), Copy(totalNet),
            Copy(Willingness(consumer).ValueSlot), G(G(local.ToArray()), step),
            Cash(root, 1), Cash(root, retained ? 1 : 0));
    }

    private static Relation CaptureInterpretation(Relation interpretation)
    {
        var p = Parts(interpretation);
        var basis = (Carrier)p[2];
        var equation = (Relation)p[0];
        if (!TimeStepSequence.TryReadSpan((Relation)equation.Left, out var a, out var b) ||
            !TimeStepSequence.TryReadSpan((Relation)equation.Right, out var c, out var d))
            throw new InvalidOperationException("Consumer interpretation has no native affine basis.");
        var bounds = Parts((Relation)p[1]).Cast<Relation>().Select(bound =>
        {
            TimeStepSequence.TryReadSpan(bound, out var lower, out var upper);
            return (IElement)Relation.Segment(lower.Carrier, lower.Ticks, upper.Ticks);
        }).ToArray();
        return G(Equation.Affine(a.Carrier, a.Ticks, b.Ticks, c.Carrier, c.Ticks, d.Ticks), G(bounds), basis);
    }

    private static CarrierLocation Interpret(CarrierLocation value, Relation interpretation, List<IElement> evidence)
    {
        var p = Parts(interpretation);
        if (!Equation.TryPredict((Relation)p[0], value, (Carrier)p[2], out var read))
            throw new InvalidOperationException("Supplied consumer basis is unavailable.");
        evidence.Add(G(Copy(value), p[0], read));
        return Clamp(Value(read), (Relation)p[1], evidence);
    }

    private static CarrierLocation Clamp(CarrierLocation value, Relation bounds, List<IElement> evidence)
    {
        foreach (var span in Parts(bounds).Cast<Relation>())
        {
            if (!TimeStepSequence.TryReadSpan(span, out var lower, out var upper))
                throw new InvalidOperationException("Malformed native finite interval.");
            var low = OperationClaim.AtLeast(new Carrier(), Copy(value), lower, new Carrier());
            var high = OperationClaim.AtMost(new Carrier(), Copy(value), upper, new Carrier());
            var selected = !Pass(low) ? lower : !Pass(high) ? upper : value;
            var residue = NativeSum(value.Carrier, evidence, (value, 1), (selected, -1));
            evidence.Add(G(span, Copy(value), low, high, Copy(selected), residue));
            value = Copy(selected);
        }
        return value;
    }

    private static void ClosePulse(Relation root, Relation route, List<IElement> evidence)
    {
        if (Gate(route).Value != 0 || Gate(route).Tension != 0) Set(root, Gate(route), Cash(root, 0), evidence);
        if (Attached(route)) Receipt(route).Settle();
    }

    private static void LowerAsk(Relation root, Relation offer, List<IElement> evidence)
    {
        var proposed = Sum(root, evidence, (Ask(offer).ValueSlot, 1), (Cash(root, 1), -1));
        Set(root, Ask(offer), Maximum(root, (CarrierLocation)Parts(offer)[6], proposed, evidence), evidence);
    }
    private static void Advance(Relation root, Relation consumer, List<IElement> evidence)
    {
        var next = Sum(root, evidence, (OfferCursor(consumer).ValueSlot, 1), (Cash(root, 1), 1));
        Set(root, OfferCursor(consumer), AtMost(root, next, Cash(root, 3), evidence) ? next : Cash(root, 0), evidence);
    }
    private static Relation FindRoute(Relation root, Relation consumer, Relation offer) => Routes(root).Single(route =>
        ReferenceEquals(RouteConsumer(route), consumer) && ReferenceEquals(RouteOffer(route), offer));
    private static Carrier Source(Relation root) => ((CarrierLocation)((Relation)Parts(InputSites(root))[0]).Left).Carrier;
    private static CarrierLocation Cash(Relation root, long ticks) => new(Credit(root), ticks);
    private static CarrierLocation AsCash(Relation root, IElement value, List<IElement> evidence) =>
        NativeSum(Credit(root), evidence, (value, 1));
    private static CarrierLocation Sum(Relation root, List<IElement> evidence, params (IElement Value, long Coefficient)[] terms) =>
        NativeSum(Credit(root), evidence, terms);
    private static CarrierLocation NativeSum(Carrier output, List<IElement> evidence, params (IElement Value, long Coefficient)[] terms)
    {
        var equation = N.SumTo(output, terms);
        var read = N.Read(equation);
        evidence.Add(G(equation, read));
        return Copy(Value(read));
    }
    private static CarrierLocation Minimum(Relation root, CarrierLocation a, CarrierLocation b, List<IElement> evidence) =>
        AtMost(root, a, b, evidence) ? a : b;
    private static CarrierLocation Maximum(Relation root, CarrierLocation a, CarrierLocation b, List<IElement> evidence) =>
        AtLeast(root, a, b, evidence) ? a : b;
    private static bool AtMost(Relation root, CarrierLocation a, CarrierLocation b, List<IElement> evidence)
    {
        var claim = OperationClaim.AtMost(new Carrier(), a, b, new Carrier());
        evidence.Add(claim); return Pass(claim);
    }
    private static bool AtLeast(Relation root, CarrierLocation a, CarrierLocation b, List<IElement> evidence)
    {
        var claim = OperationClaim.AtLeast(new Carrier(), a, b, new Carrier());
        evidence.Add(claim); return Pass(claim);
    }
    private static bool Affordable(Relation root, Carrier wallet, CarrierLocation cost, List<IElement> evidence) =>
        AtLeast(root, AsCash(root, wallet.ValueSlot, evidence), cost, evidence);
    private static bool ProviderFree(Relation provider, List<IElement> evidence)
    {
        var claim = OperationClaim.Equality(new Carrier(), Copy(Occupancy(provider).ValueSlot),
            new CarrierLocation(Occupancy(provider), 0), new Carrier());
        evidence.Add(claim);
        return Pass(claim);
    }
    private static void Set(Relation root, Carrier carrier, CarrierLocation desired, List<IElement> evidence)
    {
        var delta = NativeSum(carrier, evidence, (desired, 1), (carrier.ValueSlot, -1), (carrier.LiveTensionSlot, -1));
        evidence.Add(Carrier.ReceiveTogether(G(delta)));
    }
    private static void Transfer(Relation root, Carrier from, Carrier to, CarrierLocation amount, List<IElement> evidence)
    {
        var positive = OperationClaim.AtLeast(new Carrier(), amount, new CarrierLocation(amount.Carrier, 0), new Carrier());
        evidence.Add(positive);
        if (!Pass(positive) || !Affordable(root, from, AsCash(root, amount, evidence), evidence))
            throw new InvalidOperationException("A supplied full-fee transaction was not affordable.");
        var edge = Parts((Relation)Op(root)[9]).Cast<Relation>().Single(row =>
            ReferenceEquals(Parts(row)[0], from) && ReferenceEquals(Parts(row)[1], to));
        var request = RefinementTransfer.Create(new Carrier(), (Relation)Parts(edge)[2], new CarrierLocation(from, amount.Ticks));
        if (!RefinementTransfer.TryApply(request, out var witness))
            throw new InvalidOperationException("The explicit currency calibration did not transfer.");
        evidence.Add(witness);
    }
    public static bool Pass(Relation claim) => OperationClaim.TryReadResolvedViolation(claim, out var violation) && violation.Ticks == 0;
    public static bool Scalar(Relation read, out CarrierLocation value) => Equation.TryReadPrediction(read, out value, out _);
    public static CarrierLocation Value(Relation read) => Scalar(read, out var value) ? value :
        throw new InvalidOperationException("A supplied native arithmetic read was unresolved.");
    private static CarrierLocation Copy(CarrierLocation location) => new(location.Carrier, location.Ticks);
    private static Relation G(params IElement[] elements) => N.Group(elements);
}
