using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using N = ConnectionAssembly.NativeMath;

namespace LocalServiceGrowth;

/// <summary>
/// Supplied make-or-buy policy over connected, reusable Gojo equations. The
/// host schedules this small authored neighborhood; native reads, claims,
/// live publications and drained transfers carry its decisions and accounts.
/// </summary>
public static class ServiceNetwork
{
    // World6: operational graph, pending consumer promises, pending candidate
    // promise, candidate-template binding/open, latest frame, captured version.
    // Op16: input sites, delta recipe, providers, services, consumers, routes,
    // upstream purchase route, advertisements, local neighborhoods, resources,
    // candidate state, clock, successor, supplied policy, transfer calibrations,
    // expense channel accounts. Routes are grouped by their local client.
    public static Relation Create(bool learning = true, int supportWeight = 1, bool directAccess = false)
    {
        if (supportWeight < 0) throw new ArgumentOutOfRangeException(nameof(supportWeight));
        var source = new Carrier();
        var credit = new Carrier();
        var packet = new Carrier();
        var reservoir = new Carrier(credit.UnitShape); reservoir.Add(65536);
        var spent = new Carrier(credit.UnitShape);
        var sites = Enumerable.Range(0, 2).Select(_ => new Relation(L(source, 0), L(packet, 0))).ToArray();
        var delta = N.SumTo(source,
            (N.Present((CarrierLocation)sites[1].Left, (CarrierLocation)sites[1].Right), 1),
            (N.Present((CarrierLocation)sites[0].Left, (CarrierLocation)sites[0].Right), -1));
        // Provider3: spendable wallet, cumulative sale receipts, incurred cost.
        // The latter two are informational accounts, not currency owners.
        var providers = Enumerable.Range(0, 2).Select(_ => G(new Carrier(credit.UnitShape),
            new Carrier(credit.UnitShape), new Carrier(credit.UnitShape))).ToArray();
        var candidate = G(new Placeholder(), new Carrier(), new Carrier(), new Carrier(),
            L(source, 0), L(packet, 0), L(credit, 1), L(credit, 12), L(credit, 4), Relation.Segment(source, -8, 8));
        var fine = BuildService(providers[0], source, credit, packet, 4, 2, 0, delta);
        var fineAdvert = LocalDiscovery.Advertise(fine);
        var upstream = BuildRoute(providers[1], fine, fineAdvert, source, credit);
        var intermediary = BuildService(providers[1], source, credit, packet, 6, 1, 7,
            G(upstream, Relation.Segment(source, -1, 1), candidate));
        var services = new[] { fine, intermediary };
        var adverts = new[] { fineAdvert, LocalDiscovery.Advertise(intermediary) };
        var consumers = new[] { BuildConsumer(source, credit), BuildConsumer(source, credit) };
        var routes = consumers.SelectMany(c => services.Select((s, i) => BuildRoute(c, s, adverts[i], source, credit))).ToArray();
        var neighborhoods = new[]
        {
            LocalDiscovery.Neighborhood(consumers[0], directAccess ? [adverts[1], adverts[0]] : [adverts[1]]),
            LocalDiscovery.Neighborhood(consumers[1], adverts[1], adverts[0])
        };
        var owners = new[] { reservoir, spent }.Concat(providers.Select(Wallet)).Concat(consumers.Select(Wallet)).ToArray();
        var total = N.SumTo(credit, owners.SelectMany(c => new[]
            { ((IElement)c.ValueSlot, 1L), ((IElement)c.LiveTensionSlot, 1L) }).ToArray());
        var resources = G(reservoir, spent, credit, L(credit, 65536), total, G(owners.Cast<IElement>().ToArray()));
        var edges = new List<IElement>();
        foreach (var from in owners)
        foreach (var to in owners)
            if (!ReferenceEquals(from, to)) edges.Add(G(from, to,
                new Relation(Relation.Segment(from, 0, 1), Relation.Segment(to, 0, 1))));
        var clock = L(new Carrier(), 0);
        var policy = G(L(credit, learning ? 1 : 0), L(credit, supportWeight), L(credit, 4),
            Relation.Segment(source, -8, 8), L(credit, 8), L(credit, 8), L(credit, 64));
        var expenseChannels = G(new Carrier(credit.UnitShape), new Carrier(credit.UnitShape),
            new Carrier(credit.UnitShape), new Carrier(credit.UnitShape));
        var operational = G(G(sites), delta, G(providers), G(services), G(consumers), G(G(routes.Take(2).ToArray()), G(routes.Skip(2).ToArray())), upstream,
            G(adverts), G(neighborhoods), resources, candidate, clock,
            N.SumTo(clock.Carrier, (clock, 1), (L(clock.Carrier, 1), 1)), policy, G(edges.ToArray()), expenseChannels);
        var root = G(operational, G(new Placeholder(), new Placeholder()), new Placeholder(),
            new Placeholder(), new Placeholder(), Copy(clock));
        var evidence = new List<IElement>();
        foreach (var provider in providers) Transfer(root, reservoir, Wallet(provider), Cash(root, 32), evidence);
        foreach (var consumer in consumers) Transfer(root, reservoir, Wallet(consumer), Cash(root, 32), evidence);
        return root;
    }

    // Service10: provider, actual exposed value, presence, ask, operating cost,
    // maximum omitted source ticks, shared service capacity, occupied slots,
    // production recipe/connected inputs, refreshed-this-arrival presence.
    private static Relation BuildService(Relation provider, Carrier source, Carrier credit, Carrier packet,
        long ask, long cost, long omission, Relation recipe)
    {
        var price = new Carrier(credit.UnitShape); price.Add((ulong)ask);
        return G(provider, L(source, 0), L(packet, 0), price, L(credit, cost), L(source, omission),
            L(credit, 2), new Carrier(), recipe, L(packet, 0));
    }

    // Consumer6: wallet, interpretation (affine, bounds, benefit calibration),
    // tolerated omitted source ticks, completed service count, search count,
    // current requested range. No classifier tag defines coarse/fine meaning.
    private static Relation BuildConsumer(Carrier source, Carrier credit) => G(new Carrier(credit.UnitShape),
        G(Equation.Affine(source, 0, 1, source, 0, 1),
            new Relation(L(source, -1), L(source, 1)), L(credit, 8)),
        L(source, 7), new Carrier(), new Carrier(), Relation.Segment(source, -8, 8));

    // Route11: client, service, pulse gate, signed receipt, actual publication,
    // receipt read, public usage edge, latest local gross evidence, completed
    // evidence count, proxy correspondence, last interpreted allowance.
    private static Relation BuildRoute(Relation client, Relation service, Relation advert, Carrier source, Carrier credit)
    {
        var gate = new Carrier();
        var receipt = new Carrier(source.UnitShape);
        var calibration = Equation.Affine(Port(service).Carrier, 0, 1, source, 0, 1);
        var publication = Relation.LiveOutput(new Relation(Port(service), gate.ValueSlot),
            Equation.Product(receipt, calibration, Equation.UnitPort(gate)));
        var gross = new Carrier(credit.UnitShape); gross.Add(8);
        return G(client, service, gate, receipt, publication,
            N.SumTo(source, (receipt.ValueSlot, 1), (receipt.LiveTensionSlot, 1)),
            LocalDiscovery.Register(advert, client), gross, new Carrier(), calibration, L(source, 7));
    }

    public static IElement[] Parts(Relation group) => N.Parts(group);
    private static IElement[] Op(Relation root) => Parts(Operational(root));
    public static Relation Operational(Relation root) => (Relation)Parts(root)[0];
    public static Relation[] Services(Relation root) => Parts((Relation)Op(root)[3]).Cast<Relation>().ToArray();
    public static Relation[] Providers(Relation root) => Parts((Relation)Op(root)[2]).Cast<Relation>().ToArray();
    public static Relation[] Consumers(Relation root) => Parts((Relation)Op(root)[4]).Cast<Relation>().ToArray();
    public static Relation[] Routes(Relation root) => Parts((Relation)Op(root)[5]).Cast<Relation>()
        .SelectMany(group => Parts(group).Cast<Relation>()).ToArray();
    private static Relation[] ClientRoutes(Relation root, int client) => Parts((Relation)Parts((Relation)Op(root)[5])[client]).Cast<Relation>().ToArray();
    public static Relation[] Advertisements(Relation root) => Parts((Relation)Op(root)[7]).Cast<Relation>().ToArray();
    public static Relation[] Neighborhoods(Relation root) => Parts((Relation)Op(root)[8]).Cast<Relation>().ToArray();
    public static Relation UpstreamRoute(Relation root) => (Relation)Op(root)[6];
    public static Relation Candidate(Relation root) => (Relation)Op(root)[10];
    public static Carrier Built(Relation root) => (Carrier)Parts(Candidate(root))[1];
    public static Carrier Promoted(Relation root) => (Carrier)Parts(Candidate(root))[2];
    public static Carrier Support(Relation root) => (Carrier)Parts(Candidate(root))[3];
    public static CarrierLocation CandidatePort(Relation root) => (CarrierLocation)Parts(Candidate(root))[4];
    public static CarrierLocation CandidatePresence(Relation root) => (CarrierLocation)Parts(Candidate(root))[5];
    public static Relation? CandidateRecipe(Relation root) => Parts(root)[3] is Relation binding ? binding.Right as Relation : null;
    public static Relation Resources(Relation root) => (Relation)Op(root)[9];
    public static Carrier Reservoir(Relation root) => (Carrier)Parts(Resources(root))[0];
    public static Carrier Spent(Relation root) => (Carrier)Parts(Resources(root))[1];
    public static Carrier OperatingExpense(Relation root) => (Carrier)Parts((Relation)Op(root)[15])[0];
    public static Carrier ContactExpense(Relation root) => (Carrier)Parts((Relation)Op(root)[15])[1];
    public static Carrier ConstructionExpense(Relation root) => (Carrier)Parts((Relation)Op(root)[15])[2];
    public static Carrier TrialExpense(Relation root) => (Carrier)Parts((Relation)Op(root)[15])[3];
    private static Carrier Credit(Relation root) => (Carrier)Parts(Resources(root))[2];
    private static Carrier Source(Relation root) => ((CarrierLocation)((Relation)Parts((Relation)Op(root)[0])[0]).Left).Carrier;
    public static Carrier Wallet(Relation owner) => (Carrier)Parts(owner)[0];
    public static Relation Provider(Relation service) => (Relation)Parts(service)[0];
    public static CarrierLocation Port(Relation service) => (CarrierLocation)Parts(service)[1];
    public static CarrierLocation Presence(Relation service) => (CarrierLocation)Parts(service)[2];
    public static Carrier Ask(Relation service) => (Carrier)Parts(service)[3];
    public static CarrierLocation OperatingCost(Relation service) => (CarrierLocation)Parts(service)[4];
    public static CarrierLocation Omission(Relation service) => (CarrierLocation)Parts(service)[5];
    public static Carrier Occupancy(Relation service) => (Carrier)Parts(service)[7];
    public static Carrier Receipts(Relation provider) => (Carrier)Parts(provider)[1];
    public static Carrier Expenses(Relation provider) => (Carrier)Parts(provider)[2];
    public static Relation RouteClient(Relation route) => (Relation)Parts(route)[0];
    public static Relation RouteService(Relation route) => (Relation)Parts(route)[1];
    public static Carrier Gate(Relation route) => (Carrier)Parts(route)[2];
    public static Carrier Receipt(Relation route) => (Carrier)Parts(route)[3];
    public static Relation Publication(Relation route) => (Relation)Parts(route)[4];
    public static Relation Usage(Relation route) => (Relation)Parts(route)[6];
    public static Carrier LocalGross(Relation route) => (Carrier)Parts(route)[7];
    public static Carrier EvaluatedCount(Relation route) => (Carrier)Parts(route)[8];
    public static bool Attached(Relation route) => Receipt(route).IsAttached(Publication(route));
    public static long Version(Relation root) => ((CarrierLocation)Op(root)[11]).Ticks;
    public static Relation? SelectedRoute(Relation root, Relation consumer) => Routes(root)
        .SingleOrDefault(r => ReferenceEquals(RouteClient(r), consumer) && Attached(r));
    public static Relation Conservation(Relation root) => OperationClaim.Equality(new Carrier(),
        Copy(Value(N.Read((Relation)Parts(Resources(root))[4]))), Parts(Resources(root))[3], new Carrier());
    public static Relation RetainContinuation(Relation root)
    {
        var p = Parts(root); return G(p[0], p[1], p[2], p[3], new Placeholder(), p[5]);
    }

    /// <summary>A current local inspection of the inputs of a production
    /// structure. This does not charge, publish or refresh a paid output. In
    /// buy mode its actual upstream receipt must have a current paid pulse;
    /// in make mode it reads only the independently constructed candidate port.
    /// A missing pulse cannot turn yesterday's value into a current answer.</summary>
    public static Relation ReadProduction(Relation root, Relation service)
    {
        var evidence = new List<IElement>();
        if (EquationRecipe(service)) return N.Read(N.Present(Port(service), Presence(service)));
        var make = OperationClaim.AtLeast(new Carrier(), AsCash(root, Promoted(root).ValueSlot, evidence), Cash(root, 1), new Carrier());
        evidence.Add(make);
        Relation read;
        if (Pass(make)) read = N.Read(N.Present(CandidatePort(root), CandidatePresence(root)));
        else
        {
            var upstream = (Relation)Parts((Relation)Parts(service)[8])[0];
            var current = OperationClaim.Equality(new Carrier(), Copy(Gate(upstream).ValueSlot), L(Gate(upstream), 1), new Carrier());
            evidence.Add(current);
            if (!Attached(upstream) || !Pass(current)) return Relation.UnresolvedRead(upstream, service, Source(root));
            var receipt = Value(N.Read((Relation)Parts(upstream)[5]));
            read = N.Read(N.Present(receipt, Presence(RouteService(upstream))));
        }
        evidence.Add(read);
        if (!Scalar(read, out var value)) return Relation.UnresolvedRead(read, service, Source(root));
        var result = Clamp(value, (Relation)Parts((Relation)Parts(service)[8])[1], evidence);
        return new Relation(result, G(evidence.ToArray()));
    }

    /// <summary>Advances one externally supplied observation. Past promises
    /// are evaluated before current input is admitted or new predictions run.</summary>
    public static bool TryObserve(Relation root, long? input, bool fineSecond, out Relation next, out Relation frame)
    {
        next = root;
        frame = Relation.UnresolvedRead(root, root, Carrier.Unknown);
        var clock = (CarrierLocation)Op(root)[11];
        if (!Pass(OperationClaim.Equality(new Carrier(), Parts(root)[5], Copy(clock), new Carrier()))) return false;
        var evidence = new List<IElement>();
        var time = Copy(Value(N.Read((Relation)Op(root)[12])));
        var routes = Routes(root).Append(UpstreamRoute(root)).ToArray();
        foreach (var route in routes) ClosePulse(root, route, evidence);
        foreach (var service in Services(root))
        {
            Presence(service).Tick.Set(0); ((CarrierLocation)Parts(service)[9]).Tick.Set(0);
            Set(root, Occupancy(service), Cash(root, 0), evidence);
        }
        CandidatePresence(root).Tick.Set(0);
        var consumers = Consumers(root);
        var prior = Parts((Relation)Parts(root)[1]);
        var outcomes = consumers.Select((c, i) => Evaluate(root, c, prior[i] as Relation, input, evidence)).ToArray();
        var candidateOutcome = EvaluateCandidate(root, Parts(root)[2] as Relation, input, evidence);
        Admit(root, input);
        clock.Tick.Set(time.Ticks);
        SetConsumerScope(root, 0, false, evidence);
        SetConsumerScope(root, 1, fineSecond, evidence);
        foreach (var consumer in consumers)
        {
            var amount = Min(root, Cash(root, 8), Sum(root, evidence, (Parts((Relation)Op(root)[13])[6], 1), (Wallet(consumer).ValueSlot, -1)), evidence);
            amount = Min(root, amount, AsCash(root, Reservoir(root).ValueSlot, evidence), evidence);
            Transfer(root, Reservoir(root), Wallet(consumer), amount, evidence);
        }
        root = EnsureCandidate(root, evidence);
        var shadow = IssueCandidate(root, input, time, evidence);
        var issues = new Relation[2];
        var decisions = new Relation[2];
        foreach (var i in new[] { 0, 1 }) (issues[i], decisions[i]) = Issue(root, consumers[i], i, input, time, evidence);
        // An upstream dependency that was not needed this arrival is released;
        // retained money and yesterday's receipt do not imply current access.
        if (!Pass(OperationClaim.Equality(new Carrier(), AsCash(root, Gate(UpstreamRoute(root)).ValueSlot, evidence), Cash(root, 1), new Carrier())))
            Disconnect(root, UpstreamRoute(root), evidence);
        frame = G(Copy(time), input.HasValue ? L(Source(root), input.Value) : new Placeholder(),
            G(outcomes), G(decisions), candidateOutcome, G(evidence.ToArray()), Conservation(root));
        next = G(Operational(root), G(issues), shadow, Parts(root)[3], frame, Copy(time));
        return true;
    }

    public static (Relation Next, Relation Frame) Observe(Relation root, long? input, bool fineSecond = false)
    {
        if (!TryObserve(root, input, fineSecond, out var next, out var frame))
            throw new InvalidOperationException("The supplied observation targeted an obsolete native world envelope.");
        return (next, frame);
    }

    private static void Admit(Relation root, long? input)
    {
        var sites = Parts((Relation)Op(root)[0]).Cast<Relation>().ToArray();
        if (!input.HasValue) { foreach (var site in sites) ((CarrierLocation)site.Right).Tick.Set(0); return; }
        ((CarrierLocation)sites[0].Left).Tick.Set(((CarrierLocation)sites[1].Left).Ticks);
        ((CarrierLocation)sites[0].Right).Tick.Set(((CarrierLocation)sites[1].Right).Ticks);
        ((CarrierLocation)sites[1].Left).Tick.Set(input.Value);
        ((CarrierLocation)sites[1].Right).Tick.Set(1);
    }

    private static bool Ready(Relation root, List<IElement> evidence)
    {
        var read = N.Read((Relation)Op(root)[1]); evidence.Add(read);
        if (!Scalar(read, out var delta)) return false;
        var domain = OperationClaim.Between(new Carrier(), Copy(delta), L(Source(root), -8), L(Source(root), 8), new Carrier());
        evidence.Add(domain);
        return OperationClaim.TryReadResolvedBetweenViolation(domain, out var residual) && residual.Ticks == 0;
    }

    private static void SetConsumerScope(Relation root, int index, bool fine, List<IElement> evidence)
    {
        var consumer = Consumers(root)[index];
        var p = Parts(consumer);
        var read = Parts((Relation)p[1]);
        var bounds = (Relation)read[1];
        var newAllowance = fine ? 0 : 7;
        var changed = ((CarrierLocation)p[2]).Ticks != newAllowance;
        ((CarrierLocation)bounds.Left).Tick.Set(fine ? -8 : -1);
        ((CarrierLocation)bounds.Right).Tick.Set(fine ? 8 : 1);
        ((CarrierLocation)read[2]).Tick.Set(fine ? 4 : 8);
        ((CarrierLocation)p[2]).Tick.Set(newAllowance);
        if (!changed) return;
        foreach (var route in ClientRoutes(root, index))
        {
            Disconnect(root, route, evidence);
            Set(root, LocalGross(route), Cash(root, 8), evidence);
            Set(root, EvaluatedCount(route), Cash(root, 0), evidence);
        }
        Set(root, (Carrier)p[3], Cash(root, 0), evidence);
    }

    // Promise9: time, observed level/open, client, route/open, copied predicted
    // source difference/open, frozen interpretation, frozen ask, paid flag,
    // the actual upstream dependency used for this promise, if any.
    private static (Relation Issue, Relation Decision) Issue(Relation root, Relation consumer, int index,
        long? input, CarrierLocation time, List<IElement> evidence)
    {
        var inspected = new List<IElement>();
        var selection = new List<IElement>();
        var localRoutes = ClientRoutes(root, index);
        var incumbent = localRoutes.SingleOrDefault(Attached);
        Relation? selected = null;
        var p = Parts(consumer);
        if (input.HasValue && Ready(root, evidence))
        {
            var challenge = incumbent is null || !Eligible(root, consumer, RouteService(incumbent), selection) ||
                AtLeast(root, AsCash(root, ((Carrier)p[3]).ValueSlot, selection), Cash(root, 4), selection);
            if (!challenge && incumbent is not null) selected = incumbent;
            else
            {
                var scan = LocalDiscovery.ReadNext(Neighborhoods(root)[index], 2);
                selection.Add(scan);
                var search = Sum(root, selection, (((Carrier)p[4]).ValueSlot, 1), (Cash(root, 1), 1));
                Set(root, (Carrier)p[4], search, evidence);
                var exploration = Equation.Affine(Credit(root), 0, 4, Credit(root), 0, 1);
                Equation.TryPredict(exploration, search, Credit(root), out var cadence);
                Equation.TryReadPrediction(cadence, out _, out var rest);
                selection.Add(cadence);
                var reserveTrial = rest is CarrierLocation r && r.Ticks == 0;
                CarrierLocation? best = null;
                Relation? newcomer = null;
                foreach (var advert in LocalDiscovery.Visited(scan))
                {
                    if (!Affordable(root, Wallet(consumer), Cash(root, 1), selection)) break;
                    Transfer(root, Wallet(consumer), Spent(root), Cash(root, 1), evidence);
                    Add(root, ContactExpense(root), Cash(root, 1), evidence);
                    var service = (Relation)LocalDiscovery.Service(advert);
                    var route = localRoutes.Single(r => ReferenceEquals(RouteService(r), service));
                    var eligible = Eligible(root, consumer, service, selection);
                    var privateMargin = Sum(root, selection, (Signed(LocalGross(route)), 1), (Ask(service).ValueSlot, -1));
                    var boundedSupport = Min(root, AsCash(root, LocalDiscovery.SuccessfulCount(advert).ValueSlot, selection), Cash(root, 2), selection);
                    var score = Sum(root, selection, (privateMargin, 1),
                        (N.Product(boundedSupport, Parts((Relation)Op(root)[13])[1]), 1));
                    var row = G(advert, Copy(score), Cash(root, eligible ? 1 : 0), Copy(Ask(service).ValueSlot),
                        Copy(LocalDiscovery.ActiveCount(advert).ValueSlot), Copy(LocalDiscovery.SuccessfulCount(advert).ValueSlot));
                    inspected.Add(row);
                    if (!eligible || !Affordable(root, Wallet(consumer), AsCash(root, Ask(service).ValueSlot, selection), selection)) continue;
                    if (newcomer is null && (EvaluatedCount(route).Value == 0 || (reserveTrial && !ReferenceEquals(route, incumbent)))) newcomer = route;
                    if (AtLeast(root, privateMargin, Cash(root, 1), selection) && AtLeast(root, score, Cash(root, 1), selection) &&
                        (best is null || AtLeast(root, score, Sum(root, selection, (best.Value, 1), (Cash(root, 1), 1)), selection)))
                    { selected = route; best = score; }
                }
                // One bounded newcomer/periodic trial is reserved before
                // popularity can settle the choice. It is still paid normally.
                if (newcomer is not null) selected = newcomer;
                Set(root, (Carrier)p[3], Cash(root, 0), evidence);
            }
        }
        if (incumbent is not null && !ReferenceEquals(incumbent, selected)) Disconnect(root, incumbent, evidence);
        IElement prediction = input.HasValue ? L(Source(root), 0) : new Placeholder();
        var price = Cash(root, 0);
        var paid = false;
        if (selected is not null)
        {
            var service = RouteService(selected);
            price = AsCash(root, Ask(service).ValueSlot, evidence);
            if (Affordable(root, Wallet(consumer), price, evidence) && Produce(root, service, evidence))
            {
                Pay(root, consumer, service, price, evidence);
                Connect(root, selected, evidence);
                prediction = Copy(Value(N.Read((Relation)Parts(selected)[5])));
                paid = true;
                Set(root, (Carrier)p[3], Sum(root, evidence, (((Carrier)p[3]).ValueSlot, 1), (Cash(root, 1), 1)), evidence);
            }
            else { Disconnect(root, selected, evidence); selected = null; price = Cash(root, 0); }
        }
        var issue = G(Copy(time), input.HasValue ? L(Source(root), input.Value) : new Placeholder(), consumer,
            selected is not null ? selected : new Placeholder(), prediction, CaptureRead((Relation)p[1]), Copy(price), Cash(root, paid ? 1 : 0),
            selected is not null && ReferenceEquals(RouteService(selected), Services(root)[1]) && Promoted(root).Value == 0
                ? UpstreamRoute(root) : new Placeholder());
        return (issue, G(consumer, selected is not null ? selected : new Placeholder(), G(inspected.ToArray()), issue, G(selection.ToArray())));
    }

    private static bool Eligible(Relation root, Relation consumer, Relation service, List<IElement> evidence)
    {
        var omission = OperationClaim.AtMost(new Carrier(), Copy(Omission(service)), Copy((CarrierLocation)Parts(consumer)[2]), new Carrier());
        evidence.Add(omission);
        var free = AtMost(root, AsCash(root, Occupancy(service).ValueSlot, evidence),
            Sum(root, evidence, (Parts(service)[6], 1), (Cash(root, 1), -1)), evidence);
        return Pass(omission) && free;
    }

    private static bool Produce(Relation root, Relation service, List<IElement> evidence)
    {
        var s = Parts(service);
        if (!AtMost(root, AsCash(root, Occupancy(service).ValueSlot, evidence),
                Sum(root, evidence, (s[6], 1), (Cash(root, 1), -1)), evidence)) return false;
        if (((CarrierLocation)s[9]).Ticks == 1) return true;
        var provider = Provider(service);
        CarrierLocation produced;
        if (EquationRecipe(service))
        {
            if (!Affordable(root, Wallet(provider), OperatingCost(service), evidence)) return false;
            var read = N.Read((Relation)s[8]); evidence.Add(read);
            if (!Scalar(read, out produced)) return false;
            Charge(root, provider, OperatingCost(service), OperatingExpense(root), evidence);
        }
        else
        {
            var production = Parts((Relation)s[8]);
            CarrierLocation imported;
            var make = OperationClaim.AtLeast(new Carrier(), AsCash(root, Promoted(root).ValueSlot, evidence), Cash(root, 1), new Carrier());
            evidence.Add(make);
            if (Pass(make))
            {
                if (!RunCandidate(root, evidence)) return false;
                imported = Copy(CandidatePort(root));
                Disconnect(root, UpstreamRoute(root), evidence);
            }
            else
            {
                var upstream = RouteService((Relation)production[0]);
                var total = Sum(root, evidence, (Ask(upstream).ValueSlot, 1), (OperatingCost(service), 1));
                if (!Affordable(root, Wallet(provider), total, evidence) || !Produce(root, upstream, evidence)) return false;
                Pay(root, provider, upstream, AsCash(root, Ask(upstream).ValueSlot, evidence), evidence);
                Connect(root, UpstreamRoute(root), evidence);
                imported = Copy(Value(N.Read((Relation)Parts(UpstreamRoute(root))[5])));
                Charge(root, provider, OperatingCost(service), OperatingExpense(root), evidence);
            }
            produced = Clamp(imported, (Relation)production[1], evidence);
            evidence.Add(G(service, make, Pass(make) ? CandidateRecipe(root)! : (IElement)UpstreamRoute(root), Copy(imported), Copy(produced)));
        }
        Port(service).Tick.Set(produced.Ticks); Presence(service).Tick.Set(1); ((CarrierLocation)s[9]).Tick.Set(1);
        evidence.Add(G(service, Copy(produced), Copy(Presence(service))));
        return true;
    }

    private static bool EquationRecipe(Relation service)
    {
        var recipe = Parts((Relation)Parts(service)[8]);
        return recipe.Length == 4 && recipe[0] is Relation && recipe[2] is Carrier && recipe[3] is Relation;
    }

    private static void Pay(Relation root, Relation client, Relation service, CarrierLocation price, List<IElement> evidence)
    {
        Transfer(root, Wallet(client), Wallet(Provider(service)), price, evidence);
        Add(root, Receipts(Provider(service)), price, evidence);
        if (ReferenceEquals(client, Providers(root)[1])) Add(root, Expenses(client), price, evidence);
        Add(root, Occupancy(service), Cash(root, 1), evidence);
    }

    private static void Charge(Relation root, Relation provider, CarrierLocation amount, Carrier category, List<IElement> evidence)
    {
        Transfer(root, Wallet(provider), Spent(root), amount, evidence);
        Add(root, Expenses(provider), amount, evidence);
        Add(root, category, amount, evidence);
    }

    // Outcome9: promise/open, actual/open, error response/open, gross/open,
    // net/open, evaluated flag, positive-use flag, baseline error, used error.
    private static Relation Evaluate(Relation root, Relation consumer, Relation? issue, long? input, List<IElement> evidence)
    {
        if (issue is null) return EmptyOutcome(root, new Placeholder());
        var p = Parts(issue);
        if (p[1] is not CarrierLocation previous || p[4] is not CarrierLocation forecast || !input.HasValue)
        {
            if (p[3] is Relation unresolvedRoute) Disconnect(root, unresolvedRoute, evidence);
            return EmptyOutcome(root, issue);
        }
        var actual = NativeSum(Source(root), evidence, (L(Source(root), input.Value), 1), (previous, -1));
        var domain = OperationClaim.Between(new Carrier(), Copy(actual), L(Source(root), -8), L(Source(root), 8), new Carrier());
        evidence.Add(domain);
        if (!OperationClaim.TryReadResolvedBetweenViolation(domain, out var outside) || outside.Ticks != 0)
        {
            if (p[3] is Relation outsideRoute) Disconnect(root, outsideRoute, evidence);
            return G(issue, Copy(actual), Relation.UnresolvedRead(actual, domain, Source(root)), new Placeholder(),
                new Placeholder(), Cash(root, 0), Cash(root, 0), new Placeholder(), new Placeholder());
        }
        var read = (Relation)p[5];
        var actualRead = Interpret(actual, read, evidence);
        var predictedRead = Interpret(forecast, read, evidence);
        var before = NativeSum(actualRead.Carrier, evidence, (actualRead, -1));
        var after = NativeSum(actualRead.Carrier, evidence, (predictedRead, 1), (actualRead, -1));
        var response = OperationClaim.IntervalResponse(new Carrier(), Relation.Segment(actualRead.Carrier, 0, 0), before, after,
            new Carrier(), G(issue, Copy(actual), Copy(predictedRead)));
        if (!OperationClaim.TryReadIntervalResponse(response, out _, out _, out _, out var baseline, out var delivered, out var relief))
            return EmptyOutcome(root, issue);
        var gross = NativeSum(Credit(root), evidence, (N.Product(relief, Parts(read)[2]), 1));
        var net = Sum(root, evidence, (gross, 1), (p[6], -1));
        var helpful = AtLeast(root, gross, Cash(root, 1), evidence);
        if (p[3] is Relation route && ((CarrierLocation)p[7]).Ticks == 1)
        {
            Set(root, LocalGross(route), gross, evidence);
            Add(root, EvaluatedCount(route), Cash(root, 1), evidence);
            evidence.Add(LocalDiscovery.Update(Usage(route), Attached(route), helpful));
            if (p.Length > 8 && p[8] is Relation upstream && Attached(upstream))
                evidence.Add(LocalDiscovery.Update(Usage(upstream), true, helpful));
            if (!AtLeast(root, net, Cash(root, 1), evidence)) Disconnect(root, route, evidence);
        }
        return G(issue, Copy(actual), response, Copy(gross), Copy(net), Cash(root, 1), Cash(root, helpful ? 1 : 0), Copy(baseline), Copy(delivered));
    }

    private static Relation EmptyOutcome(Relation root, IElement issue) => G(issue, new Placeholder(),
        Relation.UnresolvedRead(issue, issue, Source(root)), new Placeholder(), new Placeholder(), Cash(root, 0), Cash(root, 0),
        new Placeholder(), new Placeholder());

    private static Relation EnsureCandidate(Relation root, List<IElement> evidence)
    {
        if (!AtLeast(root, (CarrierLocation)Parts((Relation)Op(root)[13])[0], Cash(root, 1), evidence) ||
            Built(root).Value != 0 || !Ready(root, evidence)) return root;
        var provider = Providers(root)[1];
        var price = (CarrierLocation)Parts(Candidate(root))[7];
        if (!Affordable(root, Wallet(provider), price, evidence)) return root;
        Charge(root, provider, price, ConstructionExpense(root), evidence);
        // Construction occurs only after the explicit investment. It is a
        // supplied independent two-sample recipe, not copied cached outputs.
        var sites = Parts((Relation)Op(root)[0]).Cast<Relation>().ToArray();
        var recipe = N.SumTo(Source(root),
            (N.Present((CarrierLocation)sites[1].Left, (CarrierLocation)sites[1].Right), 1),
            (N.Present((CarrierLocation)sites[0].Left, (CarrierLocation)sites[0].Right), -1));
        var binding = new Relation(Parts(Candidate(root))[0], recipe);
        evidence.Add(G(Candidate(root), Copy(price), binding));
        Set(root, Built(root), Cash(root, 1), evidence);
        var r = Parts(root); return G(r[0], r[1], r[2], binding, r[4], r[5]);
    }

    private static bool RunCandidate(Relation root, List<IElement> evidence)
    {
        if (CandidatePresence(root).Ticks == 1) return true;
        var recipe = CandidateRecipe(root);
        if (recipe is null) return false;
        var cost = (CarrierLocation)Parts(Candidate(root))[6];
        if (!Affordable(root, Wallet(Providers(root)[1]), cost, evidence)) return false;
        var read = N.Read(recipe); evidence.Add(read);
        if (!Scalar(read, out var value)) return false;
        Charge(root, Providers(root)[1], cost, Promoted(root).Value == 0 ? TrialExpense(root) : OperatingExpense(root), evidence);
        var clamped = Clamp(value, Relation.Segment(Source(root), -1, 1), evidence);
        CandidatePort(root).Tick.Set(clamped.Ticks); CandidatePresence(root).Tick.Set(1);
        evidence.Add(G(recipe, read, Copy(clamped), Copy(CandidatePresence(root))));
        return true;
    }

    // Shadow promise uses the same causal shape as a consumer promise, but is
    // funded by A and has no paid/public route or published success count.
    private static IElement IssueCandidate(Relation root, long? input, CarrierLocation time, List<IElement> evidence)
    {
        if (Promoted(root).Value != 0 || CandidateRecipe(root) is null || !input.HasValue || !Ready(root, evidence) ||
            !RunCandidate(root, evidence)) return new Placeholder();
        return G(Copy(time), L(Source(root), input.Value), Candidate(root), new Placeholder(), Copy(CandidatePort(root)),
            CaptureRead((Relation)Parts(Consumers(root)[0])[1]), Copy((CarrierLocation)Parts(Candidate(root))[6]), Cash(root, 1));
    }

    // Candidate result4: delayed outcome, support before, support after,
    // promotion witness/open. Support changes only on an observed opportunity.
    private static Relation EvaluateCandidate(Relation root, Relation? issue, long? input, List<IElement> evidence)
    {
        var before = SignedValue(Support(root));
        if (issue is null || Promoted(root).Value != 0) return G(EmptyOutcome(root, issue ?? (IElement)new Placeholder()), before, before, new Placeholder());
        var outcome = Evaluate(root, Consumers(root)[0], issue, input, evidence);
        var o = Parts(outcome);
        IElement promotion = new Placeholder();
        if (((CarrierLocation)o[5]).Ticks == 1 && o[3] is CarrierLocation gross)
        {
            var useful = AtLeast(root, gross, Cash(root, 1), evidence);
            var change = Cash(root, useful ? 1 : -1);
            var proposed = Sum(root, evidence, (Signed(Support(root)), 1), (change, 1));
            proposed = Max(root, Cash(root, 0), Min(root, (CarrierLocation)Parts(Candidate(root))[8], proposed, evidence), evidence);
            Set(root, Support(root), proposed, evidence);
            var sufficientlyRepeated = AtLeast(root, AsCash(root, Support(root).ValueSlot, evidence),
                (CarrierLocation)Parts(Candidate(root))[8], evidence);
            var saving = Sum(root, evidence, (Ask(Services(root)[0]).ValueSlot, 1), (Parts(Candidate(root))[6], -1));
            if (useful && sufficientlyRepeated && AtLeast(root, saving, Cash(root, 1), evidence))
            {
                Set(root, Promoted(root), Cash(root, 1), evidence);
                Set(root, Ask(Services(root)[1]), Cash(root, 2), evidence);
                Disconnect(root, UpstreamRoute(root), evidence);
                promotion = G(Candidate(root), issue, outcome, Copy(proposed), Copy(saving), CandidateRecipe(root)!);
            }
        }
        return G(outcome, before, SignedValue(Support(root)), promotion);
    }

    private static void Connect(Relation root, Relation route, List<IElement> evidence)
    {
        if (!Attached(route)) Receipt(route).Attach(Publication(route));
        Set(root, Gate(route), Cash(root, 1), evidence);
        Receipt(route).Settle();
        evidence.Add(LocalDiscovery.Update(Usage(route), true, null));
    }

    private static void Disconnect(Relation root, Relation route, List<IElement> evidence)
    {
        ClosePulse(root, route, evidence);
        if (Attached(route)) Receipt(route).Detach(Publication(route));
        evidence.Add(LocalDiscovery.Update(Usage(route), false, null));
    }

    private static void ClosePulse(Relation root, Relation route, List<IElement> evidence)
    {
        Set(root, Gate(route), Cash(root, 0), evidence);
        if (Attached(route)) Receipt(route).Settle();
    }

    private static Relation CaptureRead(Relation read)
    {
        var p = Parts(read); var bounds = (Relation)p[1];
        return G(p[0], new Relation(Copy((CarrierLocation)bounds.Left), Copy((CarrierLocation)bounds.Right)), Copy((CarrierLocation)p[2]));
    }

    private static CarrierLocation Interpret(CarrierLocation value, Relation read, List<IElement> evidence)
    {
        var p = Parts(read);
        Equation.TryPredict((Relation)p[0], Copy(value), value.Carrier, out var projected);
        evidence.Add(projected);
        return Clamp(Value(projected), (Relation)p[1], evidence);
    }

    private static CarrierLocation Clamp(CarrierLocation value, Relation span, List<IElement> evidence)
    {
        CarrierLocation lower;
        CarrierLocation upper;
        if (span.Left is CarrierLocation first && span.Right is CarrierLocation last && first.Carrier.SameIdentityAs(last.Carrier))
        { lower = first; upper = last; }
        else if (!TimeStepSequence.TryReadSpan(span, out lower, out upper))
            throw new InvalidOperationException("An authored local bound lost its ordered native span.");
        var low = OperationClaim.AtLeast(new Carrier(), Copy(value), Copy(lower), new Carrier());
        var high = OperationClaim.AtMost(new Carrier(), Copy(value), Copy(upper), new Carrier());
        var selected = !Pass(low) ? lower : !Pass(high) ? upper : value;
        var residue = NativeSum(value.Carrier, evidence, (value, 1), (selected, -1));
        evidence.Add(G(span, Copy(value), low, high, Copy(selected), residue));
        return Copy(selected);
    }

    private static Relation Signed(Carrier carrier) => N.SumTo(carrier, (carrier.ValueSlot, 1), (carrier.LiveTensionSlot, 1));
    public static CarrierLocation SignedValue(Carrier carrier) => Copy(Value(N.Read(Signed(carrier))));
    private static CarrierLocation NativeSum(Carrier basis, List<IElement> evidence, params (IElement Value, long Coefficient)[] terms)
    {
        var equation = N.SumTo(basis, terms); var read = N.Read(equation);
        evidence.Add(G(equation, read)); return Copy(Value(read));
    }
    private static CarrierLocation Sum(Relation root, List<IElement> evidence, params (IElement Value, long Coefficient)[] terms) => NativeSum(Credit(root), evidence, terms);
    private static CarrierLocation AsCash(Relation root, IElement value, List<IElement> evidence) => Sum(root, evidence, (value, 1));
    private static CarrierLocation Cash(Relation root, long ticks) => L(Credit(root), ticks);
    private static CarrierLocation Min(Relation root, CarrierLocation a, CarrierLocation b, List<IElement> evidence) => AtMost(root, a, b, evidence) ? a : b;
    private static CarrierLocation Max(Relation root, CarrierLocation a, CarrierLocation b, List<IElement> evidence) => AtLeast(root, a, b, evidence) ? a : b;
    private static bool AtLeast(Relation root, CarrierLocation a, CarrierLocation b, List<IElement> evidence)
    { var claim = OperationClaim.AtLeast(new Carrier(), a, b, new Carrier()); evidence.Add(claim); return Pass(claim); }
    private static bool AtMost(Relation root, CarrierLocation a, CarrierLocation b, List<IElement> evidence)
    { var claim = OperationClaim.AtMost(new Carrier(), a, b, new Carrier()); evidence.Add(claim); return Pass(claim); }
    private static bool Affordable(Relation root, Carrier wallet, CarrierLocation cost, List<IElement> evidence) => AtLeast(root, AsCash(root, wallet.ValueSlot, evidence), cost, evidence);
    private static void Add(Relation root, Carrier carrier, CarrierLocation amount, List<IElement> evidence) =>
        Set(root, carrier, NativeSum(carrier, evidence, (Signed(carrier), 1), (amount, 1)), evidence);
    private static void Set(Relation root, Carrier carrier, CarrierLocation target, List<IElement> evidence)
    {
        var delta = NativeSum(carrier, evidence, (target, 1), (carrier.ValueSlot, -1), (carrier.LiveTensionSlot, -1));
        evidence.Add(Carrier.ReceiveTogether(G(delta)));
    }
    private static void Transfer(Relation root, Carrier from, Carrier to, CarrierLocation amount, List<IElement> evidence)
    {
        if (!AtLeast(root, amount, Cash(root, 0), evidence) || !Affordable(root, from, amount, evidence))
            throw new InvalidOperationException("A scheduled full-fee transfer did not satisfy its native funds claim.");
        var edge = Parts((Relation)Op(root)[14]).Cast<Relation>().Single(r => ReferenceEquals(Parts(r)[0], from) && ReferenceEquals(Parts(r)[1], to));
        var request = RefinementTransfer.Create(new Carrier(), (Relation)Parts(edge)[2], L(from, amount.Ticks));
        if (!RefinementTransfer.TryApply(request, out var witness)) throw new InvalidOperationException("The declared currency transfer calibration failed.");
        evidence.Add(G(from, to, Copy(amount), witness));
    }
    public static bool Pass(Relation claim) => OperationClaim.TryReadResolvedViolation(claim, out var violation) && violation.Ticks == 0;
    public static bool Scalar(Relation read, out CarrierLocation value) => Equation.TryReadPrediction(read, out value, out _);
    public static CarrierLocation Value(Relation read) => Scalar(read, out var result) ? result : throw new InvalidOperationException("An authored arithmetic surface was unresolved.");
    private static CarrierLocation Copy(CarrierLocation value) => L(value.Carrier, value.Ticks);
    private static CarrierLocation L(Carrier basis, long ticks) => new(basis, ticks);
    private static Relation G(params IElement[] parts) => N.Group(parts);
}
