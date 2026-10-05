using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Structures;
using M = NegotiatedPorts.MarketProbe;

namespace NegotiatedPorts;

internal static class Program
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private static int Main(string[] args)
    {
        var checks = Checks.Run();
        Console.WriteLine($"Negotiated ports checks: {checks.Length} passed.");
        if (args.Contains("--checks-only")) return 0;
        var directory = Path.GetFullPath(args.FirstOrDefault(a => !a.StartsWith("--")) ?? "experiments/NegotiatedPorts/results");
        if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
            throw new InvalidOperationException("Results already exist; choose a new output directory to preserve the first fixed run.");
        Directory.CreateDirectory(directory);
        var summaries = new List<object>();
        var selected = new HashSet<string>();
        foreach (var (name, cursors, samples) in TrialInputs.All())
        {
            var root = M.Create(cursors[0], cursors[1]);
            var operational = M.Operational(root);
            var sourceEquations = M.SourceEquations(root);
            var frames = new List<Frame>();
            long allocated = 0;
            foreach (var sample in samples)
            {
                var start = GC.GetTotalAllocatedBytes();
                if (!M.TryObserve(root, sample.Value, sample.Demand0, sample.Demand1, sample.Funding0, sample.Funding1,
                    out var next, out var native)) throw new InvalidOperationException("Fixed admission rejected.");
                allocated += GC.GetTotalAllocatedBytes() - start;
                root = next;
                var frame = Read(root, native, sample.Phase, sample.Value);
                frames.Add(frame);
                if (!frame.Conserved) throw new InvalidOperationException("Native resource conservation did not resolve.");
                Capture("paid-live-service", frame.Consumers.Any(c => c.Paid));
                Capture("paid-harm", frame.Consumers.Any(c => c.Previous.Paid && c.Previous.Gross < 0));
                Capture("counteroffer", frame.Contacts.Any(c => c.Fits == false));
                Capture("shared-capacity-block", frame.Contacts.Any(c => c.Free == false));
                void Capture(string key, bool qualifies)
                {
                    if (!qualifies || !selected.Add(key)) return;
                    File.WriteAllBytes(Path.Combine(directory, key + ".gojo"), GojoNumericFormat.Encode(root, native));
                    File.WriteAllText(Path.Combine(directory, key + ".json"), JsonSerializer.Serialize(new { trial = name, frame }, Json));
                }
            }
            var graph = GojoEncodedSnapshot.CaptureGraph(root);
            var allConsumers = frames.SelectMany(f => f.Consumers).ToArray();
            var outcomes = allConsumers.Select(c => c.Previous).Where(p => p.Paid && p.Evaluated).ToArray();
            var final = frames[^1];
            var summary = new
            {
                name, arrivals = frames.Count, paidServicesIssued = allConsumers.Count(c => c.Paid),
                evaluatedPaid = outcomes.Length,
                unresolvedPaid = allConsumers.Count(c => c.Previous.Paid && !c.Previous.Evaluated),
                evaluatedNoService = allConsumers.Count(c => !c.Previous.Paid && c.Previous.Evaluated),
                contacts = frames.Sum(f => f.Contacts.Count(c => c.Paid)),
                counteroffers = frames.Sum(f => f.Contacts.Count(c => c.Paid && c.Fits == false)),
                capacityBlockedContacts = frames.Sum(f => f.Contacts.Count(c => c.Paid && c.Free == false)),
                consumerErrors = new[] { 0, 1 }.Select(index => new
                {
                    consumer = index,
                    basis = index == 0 ? "direction ticks" : "source-change ticks",
                    baselineDemand = frames.Select(f => f.Consumers[index].Previous).Where(p => p.Paid && p.Evaluated).Sum(p => p.BaselineDemand),
                    deliveredDemand = frames.Select(f => f.Consumers[index].Previous).Where(p => p.Paid && p.Evaluated).Sum(p => p.DeliveredDemand),
                    relief = frames.Select(f => f.Consumers[index].Previous).Where(p => p.Paid && p.Evaluated).Sum(p => p.Relief)
                }).ToArray(),
                grossCredits = outcomes.Sum(p => p.Gross), ongoingNet = outcomes.Sum(p => p.OngoingNet),
                evaluatedPaidNetIncludingOwnContact = outcomes.Sum(p => p.TotalNet),
                totalConsumerOutlay = frames.SelectMany(f => f.Payments).Where(p => p.From.StartsWith("consumer-")).Sum(p => p.Amount),
                allContactFees = frames.SelectMany(f => f.Payments).Where(p => p.From.StartsWith("consumer-") && p.To == "spent").Sum(p => p.Amount),
                operatingCosts = frames.SelectMany(f => f.Payments).Where(p => p.From.StartsWith("provider-") && p.To == "spent").Sum(p => p.Amount),
                recycledReserves = frames.SelectMany(f => f.Payments).Where(p => p.From.StartsWith("provider-") && p.To == "reservoir").Sum(p => p.Amount),
                finalOutstandingPaid = final.Consumers.Count(c => c.Paid),
                phaseAccounting = frames.GroupBy(f => f.Phase).Select(group => new
                {
                    phase = group.Key, arrivals = group.Count(), paidIssued = group.Sum(f => f.Consumers.Count(c => c.Paid)),
                    consumerOutlay = group.SelectMany(f => f.Payments).Where(p => p.From.StartsWith("consumer-")).Sum(p => p.Amount),
                    contactFees = group.SelectMany(f => f.Payments).Where(p => p.From.StartsWith("consumer-") && p.To == "spent").Sum(p => p.Amount),
                    operatingCosts = group.SelectMany(f => f.Payments).Where(p => p.From.StartsWith("provider-") && p.To == "spent").Sum(p => p.Amount),
                    grossCreditsForEvaluatedPaidIssuedInPhase = outcomes.Where(p => p.IssueStep.HasValue &&
                        frames[checked((int)p.IssueStep.Value - 1)].Phase == group.Key).Sum(p => p.Gross)
                }).ToArray(),
                positivePaid = outcomes.Count(p => p.Gross > 0), zeroPaid = outcomes.Count(p => p.Gross == 0),
                harmfulPaid = outcomes.Count(p => p.Gross < 0),
                conservedEveryFrame = frames.All(f => f.Conserved),
                finalAsks = final.Offers.Select(o => o.Ask).ToArray(), finalWallets = final.Consumers.Select(c => c.Wallet).ToArray(),
                finalReserves = final.Producers.Select(p => p.Reserve).ToArray(), final.Reservoir, final.Spent,
                finalCarriers = graph.Carriers.Length, finalRelations = graph.Relations.Length,
                persistentEquations = ReferenceEquals(operational, M.Operational(root)) && sourceEquations.Zip(M.SourceEquations(root))
                    .All(pair => ReferenceEquals(pair.First, pair.Second)), cumulativeAdmissionAllocation = allocated
            };
            summaries.Add(summary);
            File.WriteAllText(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(new
            { protocol = "caf8cae", title = name, offerNames = new[] { "Cheap direction", "Direction duplicate", "Intermediate change", "Extrapolated change" }, frames }, Json));
            File.WriteAllBytes(Path.Combine(directory, name + "-final.gojo"), GojoNumericFormat.Encode(root));
            Console.WriteLine($"{name}: issued {allConsumers.Count(c => c.Paid)}, evaluated {outcomes.Length}; " +
                $"calibrated gross {outcomes.Sum(p => p.Gross)}; " +
                $"spent {final.Spent}, reservoir {final.Reservoir}; graph {graph.Carriers.Length}/{graph.Relations.Length}.");
        }
        File.WriteAllText(Path.Combine(directory, "summary.json"), JsonSerializer.Serialize(new { protocol = "caf8cae", checks, runs = summaries }, Json));
        var report = new StringBuilder("# Negotiated live ports: first fixed results\n\n")
            .AppendLine("Protocol `caf8cae` preceded these results. No price, cost, admission or feedback parameter was tuned after seeing trial behavior. Native structures calculate forecasts, interpretation, error, signed service value, comparisons and drained resource transfers. The host supplies wiring, contact/renewal policy, admission order and report aggregation.\n")
            .AppendLine($"Focused checks: {checks.Length} passed. Detailed observer replays are in the four trial JSON files; only selected full native witnesses and final worlds were saved.\n")
            .AppendLine("The resource prices are supplied accounting costs, not measured execution time. Operational source equations and physical publication relations persist, but current claims, transfer witnesses and financial arithmetic evidence are constructed on admissions. Cumulative allocation is not retained memory.\n")
            .AppendLine("Pure inspectors may calculate a diagnostic forecast without spending or updating its live port. Only paid service updates a current exposed live port. The fee accounts for this supplied admission policy, not every CPU instruction.\n")
            .AppendLine("A price fit or paid contact is not a delivered forecast. An evaluated zero/no-service baseline is not an evaluated paid service. Frame records separate current issues from prior issue outcomes and preserve missing outcomes as unresolved. Generated displays must not infer successful service from attachment alone; inspect gate and paid/current port presence.\n")
            .AppendLine("Population, exposed recipe family, cyclic neighbor traversal and four-service exploration cadence are supplied. There is no autonomous port invention, reproduction, general coarsening, strategic price increase or global optimality claim. No per-arrival rent is charged to dormant recipes; fixed occupancy is the present retention bound.\n")
            .AppendLine("The summary JSON contains the exact run counters, native conservation results, retained graph sizes and allocation. Direction-to-fine service uses the exposed one-direction-packet to one-source-tick proxy. It does not claim a direction packet establishes exact source magnitude.\n");
        File.WriteAllText(Path.Combine(directory, "first-run.md"), report.ToString().TrimEnd() + Environment.NewLine);
        Console.WriteLine("Results: " + directory);
        return 0;
    }

    // Observer-only transport. These records never enter MarketProbe.
    private sealed record Prior(bool Issued, bool Paid, bool Evaluated, long? IssueStep, int? Offer,
        long? Forecast, long? Actual, long? BaselineDemand, long? DeliveredDemand, long? Relief,
        long? Gross, long? OngoingNet, long? TotalNet, long? Price, long? ContactFee, bool? Demand, bool Retained);
    private sealed record Consumer(int Id, long Wallet, long Bid, int Cursor, long Completed,
        bool Demand, int? SelectedOffer, long Gate, long? Receipt, long? Forecast, long Price,
        long ContactFee, bool Paid, Prior Previous);
    private sealed record Offer(int Id, int Provider, long Ask, long Cost, long? LiveValue);
    private sealed record Producer(int Id, long Reserve, long Occupied);
    private sealed record Contact(int Consumer, int Offer, long Bid, long Ask, long Fee, bool Paid,
        bool? Fits, bool? Free, bool Agreed);
    private sealed record Payment(string From, string To, long Amount);
    private sealed record Frame(long Step, string Phase, long? Input, long Reservoir, long Spent,
        bool Conserved, Offer[] Offers, Producer[] Producers, Consumer[] Consumers, Contact[] Contacts, Payment[] Payments);

    private static Frame Read(Relation root, Relation frame, string phase, long? input)
    {
        var p = M.Parts(frame);
        var offers = M.Offers(root);
        var providers = M.Producers(root);
        var consumers = M.Consumers(root);
        var issues = M.Parts((Relation)M.Parts(root)[1]).Cast<Relation>().ToArray();
        var outcomes = M.Parts((Relation)p[2]).Cast<Relation>().ToArray();
        var decisions = M.Parts((Relation)p[3]).Cast<Relation>().ToArray();
        var consumerReads = consumers.Select((consumer, index) =>
        {
            var issue = M.Parts(issues[index]);
            var selected = M.SelectedRoute(root, consumer);
            var gate = selected is null ? 0 : (long)M.Gate(selected).Value;
            return new Consumer(index, (long)M.Wallet(consumer).Value, (long)M.Willingness(consumer).Value,
                (int)M.OfferCursor(consumer).Value, (long)M.ServiceCount(consumer).Value, Number(issue[8]) == 1,
                selected is null ? null : OfferIndex(M.RouteOffer(selected)), gate,
                selected is null || gate == 0 ? null : M.Value(ConnectionAssembly.NativeMath.Read((Relation)M.Parts(selected)[6])).Ticks,
                issue[4] is CarrierLocation predicted ? predicted.Ticks : null, Number(issue[6]), Number(issue[7]),
                Number(issue[10]) == 1, ReadPrior(outcomes[index]));
        }).ToArray();
        var contacts = decisions.SelectMany((decision, index) =>
            M.Parts((Relation)M.Parts(decision)[3]).Cast<Relation>().Select(contact =>
            {
                var c = M.Parts(contact);
                var paid = c[4] is CarrierLocation;
                return new Contact(index, OfferIndex((Relation)c[0]), Number(c[1]), Number(c[2]), Number(c[3]), paid,
                    c[4] is CarrierLocation fit ? fit.Ticks == 1 : null,
                    c[5] is CarrierLocation free ? free.Ticks == 1 : null, Number(c[6]) == 1);
            })).ToArray();
        var payments = new List<Payment>();
        foreach (var item in M.Parts((Relation)p[4]).OfType<Relation>())
        {
            if (!ReadWitness.TryRead(item, out _, out _, out var result, out _, out _) ||
                result is not Relation { Left: CarrierLocation from, Right: CarrierLocation to } || to.Ticks <= 0) continue;
            var fromName = Name(from.Carrier); var toName = Name(to.Carrier);
            if (fromName is not null && toName is not null) payments.Add(new Payment(fromName, toName, to.Ticks));
        }
        return new(M.Version(root), phase, input, (long)M.Reservoir(root).Value, (long)M.Spent(root).Value,
            M.Pass((Relation)p[5]), offers.Select((offer, index) => new Offer(index,
                Array.FindIndex(providers, provider => ReferenceEquals(provider, M.Provider(offer))),
                (long)M.Ask(offer).Value, Number(M.Parts(offer)[6]),
                M.OfferPresence(offer).Ticks == 1 ? M.OfferPort(offer).Ticks : null)).ToArray(),
            providers.Select((provider, index) => new Producer(index, (long)M.Reserve(provider).Value,
                (long)M.Occupancy(provider).Value)).ToArray(), consumerReads, contacts, payments.ToArray());

        int OfferIndex(Relation offer) => Array.FindIndex(offers, item => ReferenceEquals(item, offer));
        string? Name(Carrier carrier)
        {
            if (ReferenceEquals(carrier, M.Reservoir(root))) return "reservoir";
            if (ReferenceEquals(carrier, M.Spent(root))) return "spent";
            for (var index = 0; index < consumers.Length; index++) if (ReferenceEquals(carrier, M.Wallet(consumers[index]))) return "consumer-" + index;
            for (var index = 0; index < providers.Length; index++) if (ReferenceEquals(carrier, M.Reserve(providers[index]))) return "provider-" + index;
            return null;
        }
        Prior ReadPrior(Relation outcome)
        {
            var o = M.Parts(outcome);
            if (o[0] is not Relation issued) return new(false, false, false, null, null, null, null,
                null, null, null, null, null, null, null, null, null, false);
            var i = M.Parts(issued);
            var evaluated = Number(o[8]) == 1;
            long? baseline = null, delivered = null, relief = null;
            if (evaluated && OperationClaim.TryReadIntervalResponse((Relation)o[2], out _, out _, out _, out var a, out var b, out var c))
            { baseline = a.Ticks; delivered = b.Ticks; relief = c.Ticks; }
            return new(true, Number(i[10]) == 1, evaluated, Number(i[0]),
                i[3] is Relation route ? OfferIndex(M.RouteOffer(route)) : null,
                i[4] is CarrierLocation f ? f.Ticks : null, o[1] is CarrierLocation actual ? actual.Ticks : null,
                baseline, delivered, relief, o[3] is CarrierLocation gross ? gross.Ticks : null,
                o[4] is CarrierLocation ongoing ? ongoing.Ticks : null, o[5] is CarrierLocation total ? total.Ticks : null,
                Number(i[6]), Number(i[7]), Number(i[8]) == 1, Number(o[9]) == 1);
        }
    }
    private static long Number(IElement element) => ((CarrierLocation)element).Ticks;
}
