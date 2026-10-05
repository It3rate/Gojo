using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using N = ConnectionAssembly.NativeMath;

namespace FailureSubscriptions;

/// <summary>Authored local subscription policy. Forecasts, conditional relief,
/// prospect evidence, lease budgets, live publications and payment all remain
/// connected native structure. Host loops schedule four supplied neighbors.</summary>
public static class SubscriptionNetwork
{
    // World5: reusable operations, pending issue/open, four current contracts,
    // latest frame/open, captured version. No report rows feed this structure.
    // Op13: input sites, latest/previous differences, services, seeker, routes,
    // resources, policy, clock, successor, transfer calibrations, expenses,
    // cumulative lifecycle accounts, local neighbors.
    public static Relation Create(int horizon = 5, bool useProspects = true, int neighborRotation = 0)
    {
        if (horizon is not (0 or 1 or 5)) throw new ArgumentOutOfRangeException(nameof(horizon));
        if (neighborRotation is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(neighborRotation));
        var signal = new Carrier(); var credit = new Carrier(); var presence = new Carrier();
        var sites = Enumerable.Range(0, 3).Select(_ => new Relation(L(signal, 0), L(presence, 0))).ToArray();
        var admitted = sites.Select(s => N.Present((CarrierLocation)s.Left, (CarrierLocation)s.Right)).ToArray();
        var previous = N.SumTo(signal, (admitted[1], 1), (admitted[0], -1));
        var latest = N.SumTo(signal, (admitted[2], 1), (admitted[1], -1));
        var recipes = new[] { latest, N.SumTo(signal, (latest, -1)),
            N.SumTo(signal, (latest, 2), (previous, -1)), N.SumTo(signal) };
        var services = recipes.Select(recipe => BuildService(signal, credit, presence, recipe)).ToArray();
        var cursor = new Carrier(); cursor.Add((ulong)neighborRotation);
        // Seeker6: wallet, self recipe, live self output/presence, local cursor,
        // native cursor successor. Latest difference is the supplied self model.
        var seeker = G(new Carrier(credit.UnitShape), latest, L(signal, 0), L(presence, 0), cursor,
            N.SumTo(cursor, (cursor.ValueSlot, 1), (L(cursor, 1), 1)));
        var routes = services.Select(service => BuildRoute(service, signal, credit)).ToArray();
        var reservoir = new Carrier(credit.UnitShape); reservoir.Add(65536);
        var spent = new Carrier(credit.UnitShape);
        var owners = new[] { reservoir, spent, Wallet(seeker) }.Concat(services.Select(Wallet)).ToArray();
        var total = N.SumTo(credit, owners.SelectMany(c => new[]
            { ((IElement)c.ValueSlot, 1L), ((IElement)c.LiveTensionSlot, 1L) }).ToArray());
        var resources = G(reservoir, spent, credit, L(credit, 65536), total, G(owners.Cast<IElement>().ToArray()));
        var edges = new List<IElement>();
        foreach (var from in owners)
        foreach (var to in owners)
            if (!ReferenceEquals(from, to)) edges.Add(G(from, to,
                new Relation(Relation.Segment(from, 0, 1), Relation.Segment(to, 0, 1))));
        var policy = G(L(credit, horizon), L(credit, useProspects ? 1 : 0), L(credit, 2),
            L(credit, 2), L(credit, 4), L(credit, 8), L(credit, 128),
            L(credit, 1), L(credit, 1), L(credit, 1), L(credit, 1), L(credit, 1));
        var clock = L(new Carrier(), 0);
        var op = G(G(sites), G(latest, previous), G(services), seeker, G(routes), resources, policy, clock,
            N.SumTo(clock.Carrier, (clock, 1), (L(clock.Carrier, 1), 1)), G(edges.ToArray()),
            G(Enumerable.Range(0, 5).Select(_ => (IElement)new Carrier(credit.UnitShape)).ToArray()),
            G(Enumerable.Range(0, 5).Select(_ => (IElement)new Carrier()).ToArray()), G(services));
        var root = G(op, new Placeholder(), G(Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray()),
            new Placeholder(), Copy(clock));
        var evidence = new List<IElement>();
        Transfer(root, reservoir, Wallet(seeker), Cash(root, 128), evidence);
        foreach (var service in services) Transfer(root, reservoir, Wallet(service), Cash(root, 32), evidence);
        return root;
    }

    // Service8: wallet, recipe, actual output, presence, operating cost, delivery
    // price, two trigger-conditioned prospect states, receipt account.
    private static Relation BuildService(Carrier signal, Carrier credit, Carrier presence, Relation recipe)
    {
        var port = L(signal, 0);
        var contexts = G(ProspectiveOffer.Create(port, Relation.Segment(signal, -32, -1)),
            ProspectiveOffer.Create(port, Relation.Segment(signal, 1, 32)));
        return G(new Carrier(credit.UnitShape), recipe, port, L(presence, 0), L(credit, 1), L(credit, 2),
            contexts, new Carrier(credit.UnitShape));
    }

    // Route15: supplier, pulse gate, signed receipt, actual live publication,
    // current receipt read, remaining opportunities, term relief, latest own
    // relief, consecutive resolved nonpositive reads, progress, two contextual
    // affinities, last trigger, resolved term count, last resolved flag, epoch.
    private static Relation BuildRoute(Relation service, Carrier signal, Carrier credit)
    {
        var gate = new Carrier(); var receipt = new Carrier(signal.UnitShape);
        var publication = Relation.LiveOutput(new Relation(SourcePort(service), gate.ValueSlot),
            Equation.Product(receipt, Equation.Affine(signal, 0, 1, signal, 0, 1), Equation.UnitPort(gate)));
        return G(service, gate, receipt, publication, N.SumTo(signal, (receipt.ValueSlot, 1), (receipt.LiveTensionSlot, 1)),
            new Carrier(), new Carrier(signal.UnitShape), new Carrier(signal.UnitShape), new Carrier(), new Carrier(),
            G(new Carrier(), new Carrier()), L(signal, 0), new Carrier(), new Carrier(), new Carrier());
    }

    public static IElement[] Parts(Relation group) => N.Parts(group);
    public static Relation Operational(Relation root) => (Relation)Parts(root)[0];
    private static IElement[] Op(Relation root) => Parts(Operational(root));
    public static Relation[] Services(Relation root) => Parts((Relation)Op(root)[2]).Cast<Relation>().ToArray();
    public static Relation Seeker(Relation root) => (Relation)Op(root)[3];
    public static Relation[] Routes(Relation root) => Parts((Relation)Op(root)[4]).Cast<Relation>().ToArray();
    public static Relation Resources(Relation root) => (Relation)Op(root)[5];
    public static Relation Policy(Relation root) => (Relation)Op(root)[6];
    private static CarrierLocation PolicyAt(Relation root, int index) => (CarrierLocation)Parts(Policy(root))[index];
    public static Carrier Reservoir(Relation root) => (Carrier)Parts(Resources(root))[0];
    public static Carrier Spent(Relation root) => (Carrier)Parts(Resources(root))[1];
    private static Carrier Credit(Relation root) => (Carrier)Parts(Resources(root))[2];
    public static Carrier Wallet(Relation participant) => (Carrier)Parts(participant)[0];
    public static Relation Recipe(Relation service) => (Relation)Parts(service)[1];
    public static CarrierLocation SourcePort(Relation service) => (CarrierLocation)Parts(service)[2];
    public static CarrierLocation SourcePresence(Relation service) => (CarrierLocation)Parts(service)[3];
    public static Relation[] ProspectStates(Relation service) => Parts((Relation)Parts(service)[6]).Cast<Relation>().ToArray();
    public static Carrier FeesReceived(Relation service) => (Carrier)Parts(service)[7];
    public static Relation RouteService(Relation route) => (Relation)Parts(route)[0];
    public static Carrier Gate(Relation route) => (Carrier)Parts(route)[1];
    public static Carrier Receipt(Relation route) => (Carrier)Parts(route)[2];
    public static Relation Publication(Relation route) => (Relation)Parts(route)[3];
    public static Carrier Remaining(Relation route) => (Carrier)Parts(route)[5];
    public static Carrier TermRelief(Relation route) => (Carrier)Parts(route)[6];
    public static Carrier LastRelief(Relation route) => (Carrier)Parts(route)[7];
    public static Carrier MissCount(Relation route) => (Carrier)Parts(route)[8];
    public static Carrier Progress(Relation route) => (Carrier)Parts(route)[9];
    public static Carrier[] Affinities(Relation route) => Parts((Relation)Parts(route)[10]).Cast<Carrier>().ToArray();
    public static Carrier Affinity(Relation route) => Affinities(route)[((CarrierLocation)Parts(route)[11]).Ticks < 0 ? 0 : 1];
    public static Carrier ResolvedCount(Relation route) => (Carrier)Parts(route)[12];
    public static Carrier LastResolved(Relation route) => (Carrier)Parts(route)[13];
    public static Carrier Generation(Relation route) => (Carrier)Parts(route)[14];
    public static bool Attached(Relation route) => Receipt(route).IsAttached(Publication(route));
    public static IElement[] Contracts(Relation root) => Parts((Relation)Parts(root)[2]);
    public static Relation? Contract(Relation root, Relation route) => Contracts(root).OfType<Relation>().SingleOrDefault(c => ReferenceEquals(Parts(c)[0], route));
    public static Carrier InspectionExpense(Relation root) => Expense(root, 0);
    public static Carrier ActivationExpense(Relation root) => Expense(root, 1);
    public static Carrier CalculationExpense(Relation root) => Expense(root, 2);
    public static Carrier CancellationExpense(Relation root) => Expense(root, 3);
    public static Carrier SelfExpense(Relation root) => Expense(root, 4);
    private static Carrier Expense(Relation root, int index) => (Carrier)Parts((Relation)Op(root)[10])[index];
    public static Carrier Activations(Relation root) => Counter(root, 0);
    public static Carrier Renewals(Relation root) => Counter(root, 1);
    public static Carrier Cancellations(Relation root) => Counter(root, 2);
    public static Carrier Deliveries(Relation root) => Counter(root, 3);
    public static Carrier Expiries(Relation root) => Counter(root, 4);
    private static Carrier Counter(Relation root, int index) => (Carrier)Parts((Relation)Op(root)[11])[index];
    public static long Version(Relation root) => ((CarrierLocation)Op(root)[7]).Ticks;
    public static Relation Conservation(Relation root) => OperationClaim.Equality(new Carrier(),
        Copy(Value(N.Read((Relation)Parts(Resources(root))[4]))), Parts(Resources(root))[3], new Carrier());
    public static Relation RetainContinuation(Relation root)
    { var p = Parts(root); return G(p[0], p[1], p[2], new Placeholder(), p[4]); }
    private static Carrier Signal(Relation root) => SourcePort(Services(root)[0]).Carrier;

    public static (Relation Next, Relation Frame) Observe(Relation root, long? input)
    {
        if (!TryObserve(root, input, out var next, out var frame)) throw new InvalidOperationException("An obsolete world envelope cannot admit another observation.");
        return (next, frame);
    }

    // Frame8: time, observation/open, prior outcome, inspected advertisements,
    // lease transitions, new issue, native calculation/accounting witnesses,
    // conservation. The latest frame may be released after observation.
    public static bool TryObserve(Relation root, long? input, out Relation next, out Relation frame)
    {
        next = root; frame = Relation.UnresolvedRead(root, root, Carrier.Unknown);
        var clock = (CarrierLocation)Op(root)[7];
        if (!Pass(OperationClaim.Equality(new Carrier(), Parts(root)[4], Copy(clock), new Carrier()))) return false;
        var evidence = new List<IElement>(); var transitions = new List<IElement>();
        var time = Copy(Value(N.Read((Relation)Op(root)[8])));
        foreach (var route in Routes(root)) ClosePulse(root, route, evidence);
        foreach (var service in Services(root)) SourcePresence(service).Tick.Set(0);
        ((CarrierLocation)Parts(Seeker(root))[3]).Tick.Set(0);
        var outcome = Assess(root, Parts(root)[1] as Relation, input, evidence);
        Admit(root, input); clock.Tick.Set(time.Ticks);
        var funding = Min(root, PolicyAt(root, 5), Sum(root, evidence, (PolicyAt(root, 6), 1), (Wallet(Seeker(root)).ValueSlot, -1)), evidence);
        funding = Min(root, funding, AsCash(root, Reservoir(root).ValueSlot, evidence), evidence);
        Transfer(root, Reservoir(root), Wallet(Seeker(root)), funding, evidence);
        var self = ReadSelf(root, evidence);
        var contracts = Contracts(root);
        var routes = Routes(root);
        for (var i = 0; i < routes.Length; i++)
        {
            var route = routes[i];
            if (contracts[i] is not Relation contract) continue;
            var remaining = Sum(root, evidence, (Remaining(route).ValueSlot, 1), (Cash(root, 1), -1));
            Set(root, Remaining(route), Max(root, Cash(root, 0), remaining, evidence), evidence);
            Add(root, Progress(route), Cash(root, 1), evidence);
            var expired = AtMost(root, AsCash(root, Remaining(route).ValueSlot, evidence), Cash(root, 0), evidence);
            var cancel = !expired && AtLeast(root, AsCash(root, MissCount(route).ValueSlot, evidence), PolicyAt(root, 3), evidence);
            if (!expired && !cancel) continue;
            // Failure to fund a requested withdrawal is a failed local claim,
            // not permission to obtain a free cancellation. Calendar expiry
            // still ends the term without an early-withdrawal charge.
            if (cancel && !Affordable(root, Wallet(Seeker(root)), PolicyAt(root, 9), evidence)) continue;
            var resolved = AtLeast(root, AsCash(root, ResolvedCount(route).ValueSlot, evidence), Cash(root, 1), evidence);
            var good = Positive(root, SignedValue(TermRelief(route)), evidence);
            if (resolved) Add(root, Affinity(route), Cash(root, good ? 1 : -1), evidence);
            var cancellation = Cash(root, 0);
            if (cancel)
            { Charge(root, Wallet(Seeker(root)), PolicyAt(root, 9), CancellationExpense(root), evidence); cancellation = Copy(PolicyAt(root, 9)); Add(root, Cancellations(root), Cash(root, 1), evidence); }
            if (expired) Add(root, Expiries(root), Cash(root, 1), evidence);
            Disconnect(root, route, evidence); contracts[i] = new Placeholder();
            Relation? renewed = null;
            if (expired && self is CarrierLocation && good && AtLeast(root, AsCash(root, SignedValue(LastRelief(route)), evidence), PolicyAt(root, 11), evidence) &&
                Affordable(root, Wallet(Seeker(root)), PolicyAt(root, 8), evidence) && CanIntake(root, evidence))
            {
                var trigger = (CarrierLocation)Parts(contract)[3];
                renewed = Activate(root, route, trigger, time, evidence);
                contracts[i] = renewed;
                Add(root, Renewals(root), Cash(root, 1), evidence);
            }
            transitions.Add(G(route, contract, renewed is null ? new Placeholder() : renewed, renewed is null ? Cash(root, 0) : Copy(PolicyAt(root, 8)),
                cancellation, Cash(root, renewed is null ? 0 : 1), Cash(root, expired ? 1 : 0)));
        }
        var discovered = new List<IElement>();
        var o = Parts(outcome);
        var horizon = (CarrierLocation)Parts((Relation)Op(root)[6])[0];
        if (self is CarrierLocation && o[7] is CarrierLocation evaluated && evaluated.Ticks == 1 &&
            o[3] is CarrierLocation actualError && Positive(root, actualError, evidence) && Positive(root, horizon, evidence))
            Discover(root, contracts, (CarrierLocation)o[5], time, discovered, transitions, evidence);
        root = G(Operational(root), Parts(root)[1], G(contracts), Parts(root)[3], Parts(root)[4]);
        var issue = Issue(root, input, self, time, evidence);
        frame = G(Copy(time), input.HasValue ? L(Signal(root), input.Value) : new Placeholder(), outcome,
            G(discovered.ToArray()), G(transitions.ToArray()), issue, G(evidence.ToArray()), Conservation(root));
        next = G(Operational(root), issue, G(contracts), frame, Copy(time));
        return true;
    }

    private static void Admit(Relation root, long? input)
    {
        var sites = Parts((Relation)Op(root)[0]).Cast<Relation>().ToArray();
        if (!input.HasValue) { foreach (var site in sites) ((CarrierLocation)site.Right).Tick.Set(0); return; }
        for (var i = 0; i < 2; i++)
        {
            ((CarrierLocation)sites[i].Left).Tick.Set(((CarrierLocation)sites[i + 1].Left).Ticks);
            ((CarrierLocation)sites[i].Right).Tick.Set(((CarrierLocation)sites[i + 1].Right).Ticks);
        }
        ((CarrierLocation)sites[2].Left).Tick.Set(input.Value); ((CarrierLocation)sites[2].Right).Tick.Set(1);
    }

    private static IElement ReadSelf(Relation root, List<IElement> evidence)
    {
        var seeker = Seeker(root); var p = Parts(seeker);
        var read = N.Read((Relation)p[1]); evidence.Add(read);
        if (!Scalar(read, out var value) || !Within(root, value, -8, 8, evidence) || !Affordable(root, Wallet(seeker), PolicyAt(root, 10), evidence))
            return new Placeholder();
        Charge(root, Wallet(seeker), PolicyAt(root, 10), SelfExpense(root), evidence);
        ((CarrierLocation)p[2]).Tick.Set(value.Ticks); ((CarrierLocation)p[3]).Tick.Set(1);
        return Copy(value);
    }

    // Contract9: route, start, end, frozen trigger, prospective offer, horizon,
    // delivery fee, activation cost, captured generation. End is start+horizon.
    private static Relation Activate(Relation root, Relation route, CarrierLocation trigger, CarrierLocation time, List<IElement> evidence)
    {
        var horizon = (CarrierLocation)Parts((Relation)Op(root)[6])[0];
        var quote = Quote(root, RouteService(route), trigger, time, checked((int)horizon.Ticks));
        Charge(root, Wallet(Seeker(root)), PolicyAt(root, 8), ActivationExpense(root), evidence);
        Add(root, Activations(root), Cash(root, 1), evidence);
        Add(root, Generation(route), Cash(root, 1), evidence);
        Set(root, Remaining(route), horizon, evidence);
        foreach (var state in new[] { TermRelief(route), MissCount(route), Progress(route), ResolvedCount(route), LastResolved(route) }) Set(root, state, Cash(root, 0), evidence);
        ((CarrierLocation)Parts(route)[11]).Tick.Set(trigger.Ticks);
        if (!Attached(route)) Receipt(route).Attach(Publication(route));
        var end = NativeSum(time.Carrier, evidence, (time, 1), (horizon, 1));
        return G(route, Copy(time), end, Copy(trigger), quote, Copy(horizon), Copy((CarrierLocation)Parts(RouteService(route))[5]),
            Copy(PolicyAt(root, 8)), SignedValue(Generation(route)));
    }

    private static Relation Quote(Relation root, Relation service, CarrierLocation trigger, CarrierLocation time, int horizon)
    {
        foreach (var state in ProspectStates(service))
        {
            var quote = ProspectiveOffer.Quote(state, Copy(trigger), Copy(time), horizon);
            if (ProspectiveOffer.Matches(quote)) return quote;
        }
        return ProspectiveOffer.Quote(ProspectStates(service)[0], Copy(trigger), Copy(time), horizon);
    }

    private static void Discover(Relation root, IElement[] contracts, CarrierLocation trigger, CarrierLocation time,
        List<IElement> discovered, List<IElement> transitions, List<IElement> evidence)
    {
        var routes = Routes(root); var seeker = Seeker(root); var horizon = (CarrierLocation)Parts((Relation)Op(root)[6])[0];
        var active = NativeSum(Credit(root), evidence, routes.Select(r => ((IElement)L(Credit(root), Attached(r) ? 1 : 0), 1L)).ToArray());
        var intakeThreshold = Sum(root, evidence, (Min(root, Cash(root, 2), PolicyAt(root, 2), evidence), 1), (Cash(root, 1), -1));
        if (!AtMost(root, active, intakeThreshold, evidence)) return;
        var candidates = new List<(int Index, Relation Quote, CarrierLocation Score, Relation Evidence)>();
        for (var scan = 0; scan < 2; scan++)
        {
            var cursor = (Carrier)Parts(seeker)[4];
            var index = checked((int)cursor.Value);
            var service = (Relation)Parts((Relation)Op(root)[12])[index];
            Advance(root, evidence);
            if (!Affordable(root, Wallet(seeker), PolicyAt(root, 7), evidence)) break;
            Charge(root, Wallet(seeker), PolicyAt(root, 7), InspectionExpense(root), evidence);
            var quote = Quote(root, service, trigger, time, checked((int)horizon.Ticks));
            var local = new List<IElement>();
            var slot = OperationClaim.AtMost(new Carrier(), Copy(active), Copy(intakeThreshold), new Carrier()); local.Add(slot);
            var score = Score(root, routes[index], quote, local);
            var eligible = contracts[index] is Placeholder && Pass(slot) && ProspectiveOffer.Matches(quote);
            candidates.Add((index, quote, score, G(local.ToArray())));
            discovered.Add(G(service, quote, slot, Copy(score), Cash(root, eligible ? 1 : 0), G(local.ToArray())));
        }
        // At most two native scores are compared. No private recipe is run by
        // discovery; equal or unresolved cold offers keep local traversal order.
        if (candidates.Count == 2 && AtLeast(root, candidates[1].Score,
                Sum(root, evidence, (candidates[0].Score, 1), (Cash(root, 1), 1)), evidence))
            (candidates[0], candidates[1]) = (candidates[1], candidates[0]);
        foreach (var candidate in candidates)
        {
            if (!AtMost(root, active, intakeThreshold, evidence) || contracts[candidate.Index] is not Placeholder ||
                !ProspectiveOffer.Matches(candidate.Quote) || !Affordable(root, Wallet(seeker), PolicyAt(root, 8), evidence)) continue;
            var route = routes[candidate.Index];
            var contract = Activate(root, route, trigger, time, evidence);
            contracts[candidate.Index] = contract;
            active = Sum(root, evidence, (active, 1), (Cash(root, 1), 1));
            transitions.Add(G(route, new Placeholder(), contract, Copy(PolicyAt(root, 8)), Cash(root, 0), Cash(root, 0), Cash(root, 0)));
        }
    }

    private static bool CanIntake(Relation root, List<IElement> evidence)
    {
        var active = NativeSum(Credit(root), evidence, Routes(root).Select(r => ((IElement)Cash(root, Attached(r) ? 1 : 0), 1L)).ToArray());
        var limit = Sum(root, evidence, (Min(root, Cash(root, 2), PolicyAt(root, 2), evidence), 1), (Cash(root, 1), -1));
        return AtMost(root, active, limit, evidence);
    }

    private static void Advance(Relation root, List<IElement> evidence)
    {
        var seeker = Parts(Seeker(root)); var cursor = (Carrier)seeker[4];
        var read = N.Read((Relation)seeker[5]); evidence.Add(read);
        var value = AsCash(root, Value(read), evidence);
        Set(root, cursor, AtLeast(root, value, Cash(root, 4), evidence) ? Cash(root, 0) : value, evidence);
    }

    private static CarrierLocation Score(Relation root, Relation route, Relation quote, List<IElement> evidence)
    {
        var recent = Max(root, Cash(root, 0), AsCash(root, SignedValue(LastRelief(route)), evidence), evidence);
        var score = Sum(root, evidence, (N.Product(recent, PolicyAt(root, 4)), 1));
        var use = (CarrierLocation)Parts((Relation)Op(root)[6])[1];
        if (Positive(root, use, evidence) && ProspectiveOffer.TryExpected(quote, out var expected))
            score = Sum(root, evidence, (score, 1),
                (N.Product(N.Product(Max(root, Cash(root, 0), AsCash(root, expected, evidence), evidence), PolicyAt(root, 4)), use), 1));
        return score;
    }

    // Issue7: time, observed level/open, own forecast/open, actual chosen
    // export/open, chosen route/open, four source promises, execution evidence.
    // SourcePromise5: route, copied forecast/open, contract, delivered flag,
    // selection score. Even missing opportunities keep their lease context.
    private static Relation Issue(Relation root, long? input, IElement self, CarrierLocation time, List<IElement> evidence)
    {
        var local = new List<IElement>(); var promises = new IElement[4]; var routes = Routes(root); var contracts = Contracts(root);
        IElement exported = self; IElement chosen = new Placeholder(); CarrierLocation? best = null;
        for (var i = 0; i < routes.Length; i++)
        {
            var route = routes[i];
            if (contracts[i] is not Relation contract) { promises[i] = new Placeholder(); continue; }
            var service = RouteService(route); IElement forecast = new Placeholder(); var delivered = false;
            var score = Score(root, route, (Relation)Parts(contract)[4], local);
            var fundedCost = Sum(root, local, (Wallet(service).ValueSlot, 1), (Parts(service)[5], 1));
            if (self is CarrierLocation && Affordable(root, Wallet(Seeker(root)), (CarrierLocation)Parts(service)[5], local) &&
                AtLeast(root, fundedCost, (CarrierLocation)Parts(service)[4], local))
            {
                var read = N.Read(Recipe(service)); local.Add(read);
                if (Scalar(read, out var value) && Within(root, value, -24, 24, local))
                {
                    var fee = (CarrierLocation)Parts(service)[5];
                    Transfer(root, Wallet(Seeker(root)), Wallet(service), fee, evidence); Add(root, FeesReceived(service), fee, evidence);
                    Charge(root, Wallet(service), (CarrierLocation)Parts(service)[4], CalculationExpense(root), evidence);
                    SourcePort(service).Tick.Set(value.Ticks); SourcePresence(service).Tick.Set(1);
                    Set(root, Gate(route), Cash(root, 1), evidence); Receipt(route).Settle();
                    forecast = Copy(Value(N.Read((Relation)Parts(route)[4]))); delivered = true;
                    Add(root, Deliveries(root), Cash(root, 1), evidence);
                    if (best is null || AtLeast(root, score, Sum(root, local, (best.Value, 1), (Cash(root, 1), 1)), local))
                    { exported = forecast; chosen = route; best = score; }
                }
            }
            promises[i] = G(route, forecast, contract, Cash(root, delivered ? 1 : 0), Copy(score));
        }
        var issue = G(Copy(time), input.HasValue ? L(Signal(root), input.Value) : new Placeholder(),
            self, exported, chosen, G(promises), G(local.ToArray()));
        evidence.Add(G(issue, G(local.ToArray())));
        return issue;
    }

    // Outcome8: promise/open, actual change/open, self absolute error/open,
    // actual export absolute error/open, signed export relief/open, signed
    // trigger actual-export/open, four source outcomes, evaluated flag.
    // SourceOutcome6: promise, response/open, signed conditional relief/open,
    // evaluated flag, helpful flag, prospective update/open.
    private static Relation Assess(Relation root, Relation? issue, long? input, List<IElement> evidence)
    {
        var emptyRows = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray();
        if (issue is null) return G(new Placeholder(), new Placeholder(), new Placeholder(), new Placeholder(), new Placeholder(), new Placeholder(), G(emptyRows), Cash(root, 0));
        var p = Parts(issue); CarrierLocation actual = default; var resolved = false;
        if (input.HasValue && p[1] is CarrierLocation prior)
        {
            actual = NativeSum(Signal(root), evidence, (L(Signal(root), input.Value), 1), (prior, -1));
            resolved = p[2] is CarrierLocation && p[3] is CarrierLocation && Within(root, actual, -8, 8, evidence);
        }
        Relation? exportResponse = null; CarrierLocation baseline = default; CarrierLocation exportedError = default; CarrierLocation relief = default;
        IElement trigger = new Placeholder();
        if (resolved)
        {
            exportResponse = Response(root, (CarrierLocation)p[2], (CarrierLocation)p[3], actual, issue, evidence);
            resolved = OperationClaim.TryReadIntervalResponse(exportResponse, out _, out _, out _, out baseline, out exportedError, out relief);
            trigger = NativeSum(Signal(root), evidence, (actual, 1), (p[3], -1));
        }
        var promises = Parts((Relation)p[5]); var rows = new IElement[4];
        for (var i = 0; i < promises.Length; i++)
        {
            if (promises[i] is not Relation promise) { rows[i] = new Placeholder(); continue; }
            var s = Parts(promise); var route = (Relation)s[0]; var contract = (Relation)s[2]; var quote = (Relation)Parts(contract)[4];
            var state = (Relation)Parts(quote)[0];
            Relation response; CarrierLocation? conditional = null;
            if (resolved && s[1] is CarrierLocation prediction && ((CarrierLocation)s[3]).Ticks == 1)
            {
                response = Response(root, (CarrierLocation)p[2], prediction, actual, promise, evidence);
                if (OperationClaim.TryReadIntervalResponse(response, out _, out _, out _, out _, out _, out var found)) conditional = found;
            }
            else response = Relation.UnresolvedRead(promise, issue, Signal(root));
            var update = ProspectiveOffer.Observe(state, quote, conditional, G(issue, promise, response));
            if (conditional.HasValue)
            {
                Set(root, LastRelief(route), conditional.Value, evidence); Add(root, TermRelief(route), conditional.Value, evidence);
                Add(root, ResolvedCount(route), Cash(root, 1), evidence); Set(root, LastResolved(route), Cash(root, 1), evidence);
                if (Positive(root, conditional.Value, evidence)) Set(root, MissCount(route), Cash(root, 0), evidence);
                else Add(root, MissCount(route), Cash(root, 1), evidence);
            }
            else Set(root, LastResolved(route), Cash(root, 0), evidence);
            rows[i] = G(promise, response, conditional.HasValue ? Copy(conditional.Value) : new Placeholder(),
                Cash(root, conditional.HasValue ? 1 : 0), Cash(root, conditional.HasValue && Positive(root, conditional.Value, evidence) ? 1 : 0), update);
        }
        if (!resolved) return G(issue, input.HasValue && p[1] is CarrierLocation ? Copy(actual) : new Placeholder(),
            new Placeholder(), new Placeholder(), new Placeholder(), new Placeholder(), G(rows), Cash(root, 0));
        evidence.Add(exportResponse!);
        return G(issue, Copy(actual), Copy(baseline), Copy(exportedError), Copy(relief), trigger, G(rows), Cash(root, 1));
    }

    private static Relation Response(Relation root, CarrierLocation own, CarrierLocation supplied, CarrierLocation actual,
        IElement provenance, List<IElement> evidence)
    {
        var before = NativeSum(Signal(root), evidence, (own, 1), (actual, -1));
        var after = NativeSum(Signal(root), evidence, (supplied, 1), (actual, -1));
        return OperationClaim.IntervalResponse(new Carrier(), Relation.Segment(Signal(root), 0, 0), before, after,
            new Carrier(Signal(root).UnitShape), provenance);
    }

    private static void ClosePulse(Relation root, Relation route, List<IElement> evidence)
    { Set(root, Gate(route), Cash(root, 0), evidence); if (Attached(route)) Receipt(route).Settle(); }
    private static void Disconnect(Relation root, Relation route, List<IElement> evidence)
    { ClosePulse(root, route, evidence); if (Attached(route)) Receipt(route).Detach(Publication(route)); Set(root, Remaining(route), Cash(root, 0), evidence); }
    private static bool Within(Relation root, CarrierLocation value, long lower, long upper, List<IElement> evidence)
    {
        var claim = OperationClaim.Between(new Carrier(), Copy(value), L(value.Carrier, lower), L(value.Carrier, upper), new Carrier());
        evidence.Add(claim); return OperationClaim.TryReadResolvedBetweenViolation(claim, out var remainder) && remainder.Ticks == 0;
    }
    private static Relation Signed(Carrier carrier) => N.SumTo(carrier, (carrier.ValueSlot, 1), (carrier.LiveTensionSlot, 1));
    public static CarrierLocation SignedValue(Carrier carrier) => Copy(Value(N.Read(Signed(carrier))));
    private static CarrierLocation NativeSum(Carrier basis, List<IElement> evidence, params (IElement Value, long Coefficient)[] terms)
    { var equation = N.SumTo(basis, terms); var read = N.Read(equation); evidence.Add(G(equation, read)); return Copy(Value(read)); }
    private static CarrierLocation Sum(Relation root, List<IElement> evidence, params (IElement Value, long Coefficient)[] terms) => NativeSum(Credit(root), evidence, terms);
    private static CarrierLocation AsCash(Relation root, IElement value, List<IElement> evidence) => Sum(root, evidence, (value, 1));
    private static CarrierLocation Cash(Relation root, long ticks) => L(Credit(root), ticks);
    private static CarrierLocation Min(Relation root, CarrierLocation a, CarrierLocation b, List<IElement> evidence) => AtMost(root, a, b, evidence) ? a : b;
    private static CarrierLocation Max(Relation root, CarrierLocation a, CarrierLocation b, List<IElement> evidence) => AtLeast(root, a, b, evidence) ? a : b;
    private static bool AtLeast(Relation root, CarrierLocation a, CarrierLocation b, List<IElement> evidence)
    { var claim = OperationClaim.AtLeast(new Carrier(), a, b, new Carrier()); evidence.Add(claim); return Pass(claim); }
    private static bool AtMost(Relation root, CarrierLocation a, CarrierLocation b, List<IElement> evidence)
    { var claim = OperationClaim.AtMost(new Carrier(), a, b, new Carrier()); evidence.Add(claim); return Pass(claim); }
    private static bool Positive(Relation root, CarrierLocation value, List<IElement> evidence) => AtLeast(root, AsCash(root, value, evidence), Cash(root, 1), evidence);
    private static bool Affordable(Relation root, Carrier wallet, CarrierLocation amount, List<IElement> evidence) => AtLeast(root, AsCash(root, wallet.ValueSlot, evidence), amount, evidence);
    private static void Add(Relation root, Carrier carrier, CarrierLocation amount, List<IElement> evidence) => Set(root, carrier, NativeSum(carrier, evidence, (Signed(carrier), 1), (amount, 1)), evidence);
    private static void Set(Relation root, Carrier carrier, CarrierLocation target, List<IElement> evidence)
    { var delta = NativeSum(carrier, evidence, (target, 1), (carrier.ValueSlot, -1), (carrier.LiveTensionSlot, -1)); evidence.Add(Carrier.ReceiveTogether(G(delta))); }
    private static void Charge(Relation root, Carrier wallet, CarrierLocation amount, Carrier category, List<IElement> evidence)
    { Transfer(root, wallet, Spent(root), amount, evidence); Add(root, category, amount, evidence); }
    private static void Transfer(Relation root, Carrier from, Carrier to, CarrierLocation amount, List<IElement> evidence)
    {
        if (!AtLeast(root, amount, Cash(root, 0), evidence) || !Affordable(root, from, amount, evidence))
            throw new InvalidOperationException("A scheduled full transfer lacks its native funds claim.");
        var edge = Parts((Relation)Op(root)[9]).Cast<Relation>().Single(r => ReferenceEquals(Parts(r)[0], from) && ReferenceEquals(Parts(r)[1], to));
        var request = RefinementTransfer.Create(new Carrier(), (Relation)Parts(edge)[2], L(from, amount.Ticks));
        if (!RefinementTransfer.TryApply(request, out var witness)) throw new InvalidOperationException("The declared payment calibration could not transfer.");
        evidence.Add(G(from, to, Copy(amount), witness));
    }
    public static bool Pass(Relation claim) => OperationClaim.TryReadResolvedViolation(claim, out var violation) && violation.Ticks == 0;
    public static bool Scalar(Relation read, out CarrierLocation value) => Equation.TryReadPrediction(read, out value, out _);
    public static CarrierLocation Value(Relation read) => Scalar(read, out var value) ? value : throw new InvalidOperationException("An authored native arithmetic read is unresolved.");
    private static CarrierLocation Copy(CarrierLocation value) => L(value.Carrier, value.Ticks);
    private static CarrierLocation L(Carrier basis, long ticks) => new(basis, ticks);
    private static Relation G(params IElement[] parts) => N.Group(parts);
}
