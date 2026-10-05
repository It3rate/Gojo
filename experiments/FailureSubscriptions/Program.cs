using System.Diagnostics;
using System.Text.Json;
using Gojo.Core;
using S = FailureSubscriptions.SubscriptionNetwork;

namespace FailureSubscriptions;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.SequenceEqual(new[] { "--checks-only" }))
            {
                Console.WriteLine($"{ProspectiveOfferChecks.Run()} offer, {Checks.Run()} subscription, " +
                    $"{DurationOfferChecks.Run()} duration and {AttentionChecks.Run()} attention check groups passed.");
                return 0;
            }
            var options = Options.Parse(args);
            if (!options.Campaign && !options.LongCampaign) { Run(options); return 0; }
            RequireEmpty(options.Directory);
            if (options.LongCampaign)
            {
                File.WriteAllText(Path.Combine(options.Directory, "campaign.json"), JsonSerializer.Serialize(new
                {
                    Format = "local-reviews-duration-v2", options.Steps, Runs = 24,
                    options.ResourceBudget, options.WindowEvery, options.CheckpointEvery, options.Profile,
                    Seeds = new long[] { 101, 307 }, Rotation = 0,
                    Limit = "Declared comparison; longer repetitions are separate evidence. No outcome is required to win."
                }, RunStore.Json));
                foreach (var seed in new long[] { 101, 307 })
                foreach (var scenario in new[] { "recurring", "wandering", "unpredictable" })
                foreach (var (name, horizon, attention, duration) in new[] {
                    ("alone", 0, false, false), ("original5", 5, false, false),
                    ("guarded5", 5, true, false), ("adaptive8", 8, true, true) })
                    if (!Run(options with { LongCampaign = false, Seed = seed, Rotation = 0,
                        Scenario = scenario, Horizon = horizon, Prospects = true,
                        AdaptiveAttention = attention, AdaptiveHorizon = duration,
                        Directory = Path.Combine(options.Directory, $"{scenario}-{seed}-{name}") })) return 0;
                return 0;
            }
            foreach (var (seed, rotation) in new[] { (11L, 0), (47L, 1) })
            foreach (var scenario in new[] { "mixed", "wandering", "unpredictable" })
            foreach (var (horizon, prospects) in new[] { (0, false), (1, false), (5, false), (5, true) })
                if (!Run(options with { Campaign = false, Seed = seed, Rotation = rotation,
                    Scenario = scenario, Horizon = horizon, Prospects = prospects,
                    AdaptiveAttention = false, AdaptiveHorizon = false,
                    Directory = Path.Combine(options.Directory, $"{scenario}-{seed}-h{horizon}-p{(prospects ? 1 : 0)}") })) return 0;
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static bool Run(Options options)
    {
        if (!options.Resume) RequireEmpty(options.Directory);
        Directory.CreateDirectory(options.Directory);
        using var writerLock = new FileStream(Path.Combine(options.Directory, ".writer.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var repository = RunStore.Repository();
        var sourceHash = RunStore.Fingerprint(repository);
        var protocolHash = RunStore.Hash(File.ReadAllBytes(Path.Combine(repository, "experiments/FailureSubscriptions/ADAPTIVE_PROTOCOL.md")));
        var manifestPath = Path.Combine(options.Directory, "manifest.json");
        string manifestHash;
        Relation root;
        EnvironmentSource source;
        ReportTotals totals;
        long offset = 0;
        if (options.Resume)
        {
            var bytes = File.ReadAllBytes(manifestPath);
            manifestHash = RunStore.Hash(bytes);
            var manifest = JsonSerializer.Deserialize<Manifest>(bytes, RunStore.Json)!;
            if (manifest.Format != "failure-subscriptions-v2" || manifest.SourceFingerprint != sourceHash || manifest.ProtocolFingerprint != protocolHash)
                throw new InvalidDataException("Resume needs the original source, build and protocol.");
            options = options with { Seed = manifest.Seed, Scenario = manifest.Scenario, Horizon = manifest.Horizon,
                Prospects = manifest.Prospects, Rotation = manifest.Rotation, AdaptiveAttention = manifest.AdaptiveAttention,
                AdaptiveHorizon = manifest.AdaptiveHorizon, ResourceBudget = manifest.ResourceBudget,
                WindowEvery = manifest.WindowEvery, CheckpointEvery = manifest.CheckpointEvery, Profile = manifest.Profile };
            var restored = RunStore.Load(options.Directory, manifestHash);
            root = restored.Root; source = restored.State.Environment; totals = restored.State.Totals; offset = restored.State.WindowBytes;
            if (S.Version(root) != source.Position || ((CarrierLocation)S.Parts(root)[4]).Ticks != source.Position ||
                source.Seed != manifest.Seed || source.Scenario != manifest.Scenario || !S.Pass(S.Conservation(root)))
                throw new InvalidDataException("Restored native world and run state disagree.");
        }
        else
        {
            var manifest = new Manifest("failure-subscriptions-v2", options.Seed, options.Scenario,
                options.Horizon, options.Prospects, options.Rotation, options.AdaptiveAttention, options.AdaptiveHorizon,
                options.ResourceBudget, options.WindowEvery, options.CheckpointEvery, options.Profile, sourceHash, protocolHash);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(manifest, RunStore.Json);
            File.WriteAllBytes(manifestPath, bytes); manifestHash = RunStore.Hash(bytes);
            root = S.RetainContinuation(S.Create(options.Horizon, options.Prospects, options.Rotation,
                options.AdaptiveAttention, options.AdaptiveHorizon, options.ResourceBudget));
            source = new() { Seed = options.Seed, Scenario = options.Scenario };
            totals = new();
        }
        using var windows = new FileStream(Path.Combine(options.Directory, "windows.jsonl"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        if (windows.Length < offset) throw new InvalidDataException("Report is shorter than its checkpoint; refusing to pad it.");
        windows.SetLength(offset); windows.Position = offset;
        var window = new ReportTotals();
        string? lastPhase = null;
        var stop = false;
        ConsoleCancelEventHandler cancel = (_, args) => { args.Cancel = true; stop = true; };
        Console.CancelKeyPress += cancel;
        try
        {
            for (var step = 0; step < options.Steps && !stop; step++)
            {
                var sample = source.Next();
                if (options.WindowEvery > 0 && lastPhase is not null && lastPhase != sample.Phase) Flush("phase-change");
                lastPhase = sample.Phase;
                var allocated = options.Profile ? GC.GetTotalAllocatedBytes(true) : 0;
                var started = options.Profile ? Stopwatch.GetTimestamp() : 0;
                if (!S.TryObserve(root, sample.Input, out var next, out var frame))
                    throw new InvalidDataException("Native observation rejected a current supplied sample.");
                root = next;
                var nativeTime = options.Profile ? Stopwatch.GetElapsedTime(started).TotalMilliseconds : 0;
                var nativeAllocation = options.Profile ? GC.GetTotalAllocatedBytes(true) - allocated : 0;
                allocated = options.Profile ? GC.GetTotalAllocatedBytes(true) : 0;
                started = options.Profile ? Stopwatch.GetTimestamp() : 0;
                if (options.WindowEvery > 0) Report.Add(window, root, frame, sample.Phase);
                Report.Add(totals, root, frame, sample.Phase);
                root = S.RetainContinuation(root);
                var reportTime = options.Profile ? Stopwatch.GetElapsedTime(started).TotalMilliseconds : 0;
                var reportAllocation = options.Profile ? GC.GetTotalAllocatedBytes(true) - allocated : 0;
                foreach (var aggregate in options.WindowEvery > 0 ? new[] { window, totals } : new[] { totals })
                {
                    aggregate.NativeWallMilliseconds += nativeTime; aggregate.NativeAllocationBytes += nativeAllocation;
                    aggregate.ReportWallMilliseconds += reportTime; aggregate.ReportAllocationBytes += reportAllocation;
                }
                if (!S.Pass(S.Conservation(root))) throw new InvalidDataException("Native currency ledger failed.");
                if (options.WindowEvery > 0 && source.Position % options.WindowEvery == 0) Flush("window");
                if (source.Position % options.CheckpointEvery == 0)
                {
                    Flush("checkpoint"); Save();
                    Console.WriteLine($"{Path.GetFileName(options.Directory)}: checkpoint {source.Position}; error {totals.DeliveredError}; spent {S.Spent(root).Value}; pool {S.Reservoir(root).Value}.");
                }
            }
            Flush(stop ? "interrupted" : "requested-end"); Save();
            File.WriteAllText(Path.Combine(options.Directory, "summary.json"), JsonSerializer.Serialize(new
            {
                ManifestHash = manifestHash, Reason = stop ? "interrupted" : "requested-end", source.Position,
                Totals = totals, Current = Report.State(root), ProfilingMeasured = options.Profile,
                Limit = "Supplied recipes, context partitions, costs and attention laws; future estimates are conditional empirical reads, not guaranteed correctness."
            }, RunStore.Json));
            Console.WriteLine($"{Path.GetFileName(options.Directory)}: {source.Position} observations completed.");
        }
        finally { Console.CancelKeyPress -= cancel; }
        return !stop;

        void Flush(string reason)
        {
            if (window.Admissions == 0) return;
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new { Reason = reason, Window = window, Current = Report.State(root) });
            windows.Write(bytes); windows.WriteByte((byte)'\n'); windows.Flush(true); window = new();
        }
        void Save()
        {
            var allocated = GC.GetTotalAllocatedBytes(true); var started = Stopwatch.GetTimestamp();
            var graph = GojoEncodedSnapshot.CaptureGraph(root);
            totals.CheckpointInspections++;
            totals.PeakRetainedCarriers = Math.Max(totals.PeakRetainedCarriers, graph.Carriers.Length);
            totals.PeakRetainedRelations = Math.Max(totals.PeakRetainedRelations, graph.Relations.Length);
            RunStore.Save(options.Directory, root, new(manifestHash, source, windows.Position, totals));
            File.WriteAllText(Path.Combine(options.Directory, "latest-checkpoint-inspection.json"), JsonSerializer.Serialize(new
            {
                source.Position, Carriers = graph.Carriers.Length, Relations = graph.Relations.Length,
                Bytes = new FileInfo(Path.Combine(options.Directory, "latest.checkpoint.zip")).Length,
                WallMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                AllocationBytes = GC.GetTotalAllocatedBytes(true) - allocated
            }, RunStore.Json));
        }
    }

    private static void RequireEmpty(string directory)
    {
        if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
            throw new InvalidOperationException("Output must be empty; select a new directory or --resume.");
        Directory.CreateDirectory(directory);
    }

    private sealed record Options(string Directory, bool Resume, bool Campaign, bool LongCampaign, long Seed, string Scenario,
        int Horizon, bool Prospects, int Rotation, int Steps, bool AdaptiveAttention, bool AdaptiveHorizon,
        long ResourceBudget, int WindowEvery, int CheckpointEvery, bool Profile)
    {
        public static Options Parse(string[] args)
        {
            string? output = null, resume = null;
            long seed = 11, resourceBudget = 1_048_576;
            string scenario = "recurring"; bool prospects = true, campaign = false, longCampaign = false;
            bool attention = true, duration = true, profile = true;
            int horizon = 8, rotation = 0, steps = 241, windowEvery = 128, checkpointEvery = 512;
            var supplied = new HashSet<string>();
            for (var i = 0; i < args.Length; i++)
            {
                var key = args[i];
                if (!supplied.Add(key)) throw new ArgumentException("Duplicate option: " + key);
                if (key == "--campaign") { campaign = true; continue; }
                if (key == "--long-campaign") { longCampaign = true; continue; }
                if (++i >= args.Length) throw new ArgumentException("Missing value for " + key);
                var value = args[i];
                switch (key)
                {
                    case "--output": output = value; break;
                    case "--resume": resume = value; break;
                    case "--seed": seed = long.Parse(value); break;
                    case "--scenario": scenario = value; break;
                    case "--horizon": horizon = int.Parse(value); break;
                    case "--prospects": prospects = value switch { "0" => false, "1" => true, _ => throw new ArgumentException("Use 0 or 1 for prospects.") }; break;
                    case "--rotation": rotation = int.Parse(value); break;
                    case "--steps": steps = int.Parse(value); break;
                    case "--attention": attention = Binary(value); break;
                    case "--adaptive-horizon": duration = Binary(value); break;
                    case "--resource-budget": resourceBudget = long.Parse(value); break;
                    case "--window": windowEvery = int.Parse(value); break;
                    case "--checkpoint": checkpointEvery = int.Parse(value); break;
                    case "--profile": profile = Binary(value); break;
                    default: throw new ArgumentException("Unknown option: " + key);
                }
            }
            if (output is not null && resume is not null) throw new ArgumentException("Choose output or resume.");
            if (campaign && longCampaign) throw new ArgumentException("Choose one campaign.");
            if (campaign && supplied.Any(k => k is not "--campaign" and not "--output"))
                throw new ArgumentException("Frozen campaign accepts only --campaign and --output.");
            if (longCampaign && supplied.Any(k => k is not "--long-campaign" and not "--output" and not "--steps"
                and not "--window" and not "--checkpoint" and not "--profile" and not "--resource-budget"))
                throw new ArgumentException("Long campaign keeps paired policies and streams; only run length, funding and reporting can change.");
            if (resume is not null && supplied.Any(k => k is not "--resume" and not "--steps"))
                throw new ArgumentException("Resume keeps frozen settings; only --steps can change.");
            if (longCampaign && !supplied.Contains("--steps")) steps = 4097;
            if (campaign) { resourceBudget = 65536; windowEvery = 48; checkpointEvery = 80; }
            if (scenario is not "mixed" and not "recurring" and not "wandering" and not "unpredictable" || horizon is < 0 or > 8 ||
                rotation is < 0 or > 3 || steps is <= 0 or > 1_000_000 || resourceBudget is < 256 or > 1_000_000_000_000 ||
                windowEvery is < 0 or > 1_000_000 || checkpointEvery is <= 0 or > 1_000_000)
                throw new ArgumentException("Invalid scenario, horizon, rotation or step count.");
            return new(Path.GetFullPath(resume ?? output ?? "experiments/FailureSubscriptions/obj/local-run"),
                resume is not null, campaign, longCampaign, seed, scenario, horizon, prospects, rotation, steps,
                attention, duration, resourceBudget, windowEvery, checkpointEvery, profile);
        }

        private static bool Binary(string value) => value switch { "0" => false, "1" => true,
            _ => throw new ArgumentException("Use 0 or 1 for policy/profiling switches.") };
    }
}
