using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Gojo.Core;
using Gojo.Structures;
using M = NegotiatedPorts.MarketProbe;
using N = ConnectionAssembly.NativeMath;

namespace MarketResourceAudit;

// This executable is an observer and workload harness. It does not change the
// frozen market policy, supply learner classifications, or feed metrics back.
internal static class Program
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private const int Repeats = 3;

    private static void Main(string[] args)
    {
        var repository = FindRepository();
        var output = Path.GetFullPath(args.FirstOrDefault() ??
            Path.Combine(repository, "experiments/MarketResourceAudit/results/resource-audit.json"));
        if (File.Exists(output)) throw new InvalidOperationException("Choose a new output file; existing findings are retained.");
        var saved = Path.Combine(repository, "experiments/NegotiatedPorts/results");
        var trials = NegotiatedPorts.TrialInputs.All().ToArray();
        var graphs = new List<object>();
        foreach (var trial in trials)
        {
            var packet = File.ReadAllBytes(Path.Combine(saved, trial.Name + "-final.gojo"));
            var world = (Relation)GojoNumericFormat.Decode(packet)[0];
            var all = GojoEncodedSnapshot.CaptureGraph(world);
            var continuation = GojoEncodedSnapshot.CaptureGraph(ContinuationRoots(world));
            var operational = GojoEncodedSnapshot.CaptureGraph(M.Operational(world));
            var frame = GojoEncodedSnapshot.CaptureGraph(M.Parts(world)[2]);
            var trimmed = ForgetLatestFrame(world);
            var trimmedGraph = GojoEncodedSnapshot.CaptureGraph(trimmed);
            var paired = CheckContinuation(packet);
            graphs.Add(new
            {
                trial = trial.Name, full = Count(all), operationalOnly = Count(operational),
                continuationRoots = Count(continuation), latestFrameRoot = Count(frame),
                frameForgottenEnvelope = Count(trimmedGraph), paired,
                fullNativeBytes = GojoNumericFormat.Encode(all).Length,
                continuationNativeBytes = GojoNumericFormat.Encode(continuation).Length,
                frameForgottenNativeBytes = GojoNumericFormat.Encode(trimmedGraph).Length,
                caveat = "Frame reachability overlaps operational and pending structure. Do not sum these counts. Omitting retained frame does not avoid its creation inside TryObserve."
            });
        }

        // Warm every workload and serializer before recording. Setup, collection
        // and graph inspection sit outside the measured stages.
        foreach (var trial in trials) Run(trial.Name, trial.InitialCursors, trial.Samples, -1);
        var runs = new List<object>();
        for (var repetition = 0; repetition < Repeats; repetition++)
        foreach (var trial in repetition % 2 == 0 ? trials : trials.Reverse().ToArray())
        {
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            var measured = Run(trial.Name, trial.InitialCursors, trial.Samples, repetition);
            runs.Add(measured);
            Console.WriteLine($"{trial.Name} repetition {repetition + 1}: {measured.Admission.Milliseconds:F1} ms, {measured.Admission.AllocatedBytes:N0} admission bytes");
        }
        var findings = new
        {
            measuredAtUtc = DateTimeOffset.UtcNow,
            runtime = RuntimeInformation.FrameworkDescription,
            os = RuntimeInformation.OSDescription,
            processorCount = Environment.ProcessorCount,
            protocol = "caf8cae; unchanged supplied policy",
            methodology = new
            {
                warmupsPerTrial = 1, repetitionsPerTrial = Repeats, arrivalsPerTrial = trials[0].Samples.Length,
                allocation = "GC.GetAllocatedBytesForCurrentThread around each synchronous stage; cumulative allocation, not retained heap or process RSS.",
                time = "Stopwatch.GetTimestamp sums around each stage; Release, warmed, local diagnostic run, not a controlled benchmark.",
                admission = "Existing TryObserve, including construction of the latest native evidence frame, native policy arithmetic, conservation claim, transfer and settlement. Excludes market construction, inspector and output serialization.",
                localRead = "Compact observer: money state, asks, port values, selected routes, pending promises, prior outcomes and existing conservation claim. Includes native live-receipt reads. Not the full existing viewer observer.",
                json = "One compact observer row serialized to UTF-8 per arrival and immediately discarded. No growing replay list and no timed disk writes.",
                checkpoint = "One full native checkpoint encoding after each trial; separately one continuation-roots encoding. Includes graph capture and encoding. No timed disk writes.",
                resultUse = "No measured record or graph snapshot enters the native learner.",
                caution = "Retained graph reduction is not a proof of a fundamental minimum, nor of avoiding allocations that have already happened inside admission. Single-machine timings can vary with contention, GC and JIT."
            },
            graphs, runs
        };
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        File.WriteAllText(output, JsonSerializer.Serialize(findings, Json) + Environment.NewLine);
        Console.WriteLine("Findings: " + output);
    }

    private sealed record Stage(double Milliseconds, long AllocatedBytes);
    private sealed record RunResult(string Trial, int Repetition, int Arrivals, Stage Admission,
        Stage LocalRead, Stage CompactJson, Stage FullNativeCheckpoint, Stage ContinuationCheckpoint,
        int FullNativeBytes, int ContinuationNativeBytes, long CompactJsonBytes, bool ReadWasPure,
        string ContinuationSignature);
    private sealed class Meter
    {
        private long ticks, bytes;
        public T Measure<T>(Func<T> operation)
        {
            var beforeBytes = GC.GetAllocatedBytesForCurrentThread();
            var beforeTicks = Stopwatch.GetTimestamp();
            var result = operation();
            ticks += Stopwatch.GetTimestamp() - beforeTicks;
            bytes += GC.GetAllocatedBytesForCurrentThread() - beforeBytes;
            return result;
        }
        public Stage Result() => new(ticks * 1000d / Stopwatch.Frequency, bytes);
    }

    private static RunResult Run(string name, int[] cursors,
        (string Phase, long? Value, bool Demand0, bool Demand1, long Funding0, long Funding1)[] samples, int repetition)
    {
        var root = M.Create(cursors[0], cursors[1]);
        var admissions = new Meter(); var reads = new Meter(); var json = new Meter();
        var checkpoint = new Meter(); var continuation = new Meter();
        long jsonBytes = 0;
        foreach (var sample in samples)
        {
            var admitted = admissions.Measure(() =>
            {
                if (!M.TryObserve(root, sample.Value, sample.Demand0, sample.Demand1,
                    sample.Funding0, sample.Funding1, out var next, out var frame))
                    throw new InvalidOperationException("Frozen input admission failed.");
                return (next, frame);
            });
            root = admitted.next;
            var metrics = reads.Measure(() => Inspect(root, admitted.frame));
            if (!metrics.Conserved) throw new InvalidOperationException("Resource conservation failed.");
            jsonBytes += json.Measure(() => JsonSerializer.SerializeToUtf8Bytes(metrics)).Length;
        }
        var signature = Signature(ContinuationRoots(root));
        _ = Inspect(root, (Relation)M.Parts(root)[2]);
        var pure = signature == Signature(ContinuationRoots(root));
        if (!pure) throw new InvalidOperationException("Observer changed continuation state.");
        var full = checkpoint.Measure(() => GojoNumericFormat.Encode(root));
        var reduced = continuation.Measure(() => GojoNumericFormat.Encode(ContinuationRoots(root)));
        return new(name, repetition, samples.Length, admissions.Result(), reads.Result(), json.Result(),
            checkpoint.Result(), continuation.Result(), full.Length, reduced.Length, jsonBytes, pure, signature);
    }

    // Boundary-only records: never canonical state, cached beliefs or feedback.
    private sealed record Money(ulong Value, long Tension);
    private sealed record Offer(long? Published, long Presence, Money Ask);
    private sealed record Consumer(Money Wallet, Money Bid, Money Cursor, Money Completed,
        int? Selected, Money? Gate, long? Receipt, long? Prediction, long? Price, long? Contact, long? Paid);
    private sealed record Prior(long? Evaluated, long? Actual, long? Gross, long? OngoingNet, long? TotalNet, long? Retained);
    private sealed record Metrics(long Version, bool Conserved, Money Reservoir, Money Spent,
        Money[] Reserves, Money[] Occupancy, Offer[] Offers, Consumer[] Consumers, Prior[] Previous);
    private static Metrics Inspect(Relation root, Relation frame)
    {
        var offers = M.Offers(root); var providers = M.Producers(root); var consumers = M.Consumers(root);
        var issues = M.Parts((Relation)M.Parts(root)[1]);
        var outcomes = M.Parts((Relation)M.Parts(frame)[2]);
        return new(M.Version(root), M.Pass((Relation)M.Parts(frame)[5]), Cash(M.Reservoir(root)), Cash(M.Spent(root)),
            providers.Select(p => Cash(M.Reserve(p))).ToArray(), providers.Select(p => Cash(M.Occupancy(p))).ToArray(),
            offers.Select(o => new Offer(M.OfferPresence(o).Ticks == 1 ? M.OfferPort(o).Ticks : null,
                M.OfferPresence(o).Ticks, Cash(M.Ask(o)))).ToArray(),
            consumers.Select((consumer, i) =>
            {
                var selected = M.SelectedRoute(root, consumer); var issue = M.Parts((Relation)issues[i]);
                return new Consumer(Cash(M.Wallet(consumer)), Cash(M.Willingness(consumer)), Cash(M.OfferCursor(consumer)),
                    Cash(M.ServiceCount(consumer)), selected is null ? null : Array.IndexOf(offers, M.RouteOffer(selected)),
                    selected is null ? null : Cash(M.Gate(selected)),
                    selected is null || M.Gate(selected).Value == 0 ? null : M.Value(N.Read((Relation)M.Parts(selected)[6])).Ticks,
                    Number(issue[4]), Number(issue[6]), Number(issue[7]), Number(issue[10]));
            }).ToArray(), outcomes.Select(o =>
            {
                var parts = M.Parts((Relation)o);
                return new Prior(Number(parts[8]), Number(parts[1]), Number(parts[3]), Number(parts[4]), Number(parts[5]), Number(parts[9]));
            }).ToArray());
        static Money Cash(Carrier c) => new(c.Value, c.Tension);
        static long? Number(IElement e) => e is CarrierLocation l ? l.Ticks : null;
    }

    private static IElement[] ContinuationRoots(Relation root)
    {
        var parts = M.Parts(root);
        return [parts[0], parts[1], parts[3]];
    }

    private static Relation ForgetLatestFrame(Relation root)
    {
        var parts = M.Parts(root);
        // The omitted diagnostic slot stays explicitly open. Live structure,
        // pending promises, and current version remain physically connected.
        return Relation.OrderedGroup(new Carrier(), parts[0], parts[1], new Placeholder(), parts[3]);
    }

    private static object CheckContinuation(byte[] original)
    {
        var complete = (Relation)GojoNumericFormat.Decode(original)[0];
        var forgotten = ForgetLatestFrame((Relation)GojoNumericFormat.Decode(original)[0]);
        var sampleSite = (Relation)M.Parts(M.InputSites(complete))[2];
        var level = ((CarrierLocation)sampleSite.Left).Ticks;
        var pattern = new long?[] { 4, 4, 3, 4, 5, 4, 0, 0, 0, -3, -3, -4, null, 2, 2, 8, 2, -2, 0, 3, 3, 3, 4, 4 };
        RequireSame(complete, forgotten, "before continuation");
        for (var index = 0; index < pattern.Length; index++)
        {
            if (pattern[index].HasValue) level += pattern[index]!.Value;
            var input = pattern[index].HasValue ? (long?)level : null;
            var demand1 = index is not (>= 6 and <= 9);
            var scarce = index is >= 14 and <= 18;
            if (!M.TryObserve(complete, input, true, demand1, scarce ? 1 : 5, scarce ? 2 : 8,
                    out var nextComplete, out var frameComplete) ||
                !M.TryObserve(forgotten, input, true, demand1, scarce ? 1 : 5, scarce ? 2 : 8,
                    out var nextForgotten, out var frameForgotten))
                throw new InvalidOperationException("Paired continuation admission failed.");
            complete = nextComplete; forgotten = ForgetLatestFrame(nextForgotten);
            RequireSame(complete, forgotten, "after continuation " + index);
            if (JsonSerializer.Serialize(Inspect(complete, frameComplete)) != JsonSerializer.Serialize(Inspect(forgotten, frameForgotten)))
                throw new InvalidOperationException("Paired financial/prediction reads differ.");
        }
        return new { continuationArrivals = pattern.Length, equivalentEveryArrival = true,
            check = "Normalized complete native continuation graphs plus financial and prediction reads; latest frame forgotten after every admission.",
            coverage = "Changing increment, flat, falling, missing, jump, withdrawal and scarce funding; bounded fixture, not proof for all future inputs." };
        static void RequireSame(Relation a, Relation b, string stage)
        {
            if (Signature(ContinuationRoots(a)) != Signature(ContinuationRoots(b)))
                throw new InvalidOperationException("Native continuation graph differs " + stage);
        }
    }

    private static object Count(GojoEncodedSnapshot graph) => new
    {
        carriers = graph.Carriers.Length, relations = graph.Relations.Length, ticks = graph.TickRefs.Length,
        locations = graph.CarrierLocations.Length, units = graph.UnitShapes.Length, placeholders = graph.Placeholders.Length,
        attachmentMemberships = graph.Carriers.Sum(c => c.AttachedRelationIndexes!.Length),
        distinctAttachedRelations = graph.Carriers.SelectMany(c => c.AttachedRelationIndexes!).Distinct().Count()
    };

    private static string Signature(params IElement[] roots)
    {
        var graph = GojoEncodedSnapshot.CaptureGraph(roots);
        // Runtime identities differ on restore. Table indexes preserve aliases,
        // directions, state, unit promises, attachments, and ordered roots.
        var normalized = graph with
        {
            Carriers = graph.Carriers.Select(c => c with { Id = 0 }).ToArray(),
            TickRefs = graph.TickRefs.Select(t => t with { Id = 0 }).ToArray(),
            Relations = graph.Relations.Select(r => r with { Id = 0 }).ToArray(),
            UnitShapes = graph.UnitShapes.Select(u => u with { Id = 0 }).ToArray()
        };
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(normalized)));
    }

    private static string FindRepository()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Gojo.slnx"))) return directory.FullName;
        throw new DirectoryNotFoundException("Run the audit from its repository build.");
    }
}
