using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using S = ChangeSubscriptions.NativeExperiment;

namespace ChangeSubscriptions;

internal static class Program
{
    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };
    public static int Main(string[] args)
    {
        try
        {
            if (args.Contains("--checks-only"))
            {
                var count = ChangeSurfaceChecks.Run() + ComparativeRetryChecks.Run() + IntegrationChecks.Run()
                    + WakeProbeChecks.Run() + WakeIntegrationChecks.Run();
                Console.WriteLine($"{count} focused check groups passed."); return 0;
            }
            var wakeCampaign = args.Contains("--wake-campaign");
            if (!args.Contains("--campaign") && !wakeCampaign)
            {
                Console.WriteLine("Use --checks-only, --campaign, or --wake-campaign --output <new directory>."); return 0;
            }
            var outputIndex = Array.IndexOf(args, "--output");
            if (outputIndex < 0 || outputIndex + 1 >= args.Length) throw new ArgumentException("Supply --output with a new directory.");
            var output = Path.GetFullPath(args[outputIndex + 1]);
            if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
                throw new IOException("Campaign output must be empty; preserved evidence will not be overwritten.");
            Directory.CreateDirectory(output);
            var repository = Repository();
            var policies = wakeCampaign ? new[] { "fixed2", "escalating32", "escalating32-wake2" }
                : new[] { "unthrottled", "fixed2", "escalating32" };
            var fixtures = new[] { "steady-reversal", "grain-gap", "applicability-return", "unpredictable-113", "unpredictable-271" }
                .Concat(wakeCampaign ? new[] { "wake-chatter" } : Array.Empty<string>()).ToArray();
            var manifest = new
            {
                Format = wakeCampaign ? "change-subscriptions-wake-v1" : "change-subscriptions-v1", SourceFingerprint = Fingerprint(repository),
                ProtocolFingerprint = Hash(File.ReadAllBytes(Path.Combine(repository,
                    wakeCampaign ? "experiments/ChangeSubscriptions/WAKE_PROTOCOL.md" : "experiments/ChangeSubscriptions/PROTOCOL.md"))),
                ConsumerOrder = new[] { "direction", "amount" }, ViewOrder = new[] { "direction", "amount", "coarse-amount" },
                Policies = policies, Fixtures = fixtures,
                Limit = "Authored local laws. Host schedules arrivals and observers. Modeled access costs, no autonomous asynchronous kernel scheduler."
            };
            File.WriteAllText(Path.Combine(output, "manifest.json"), JsonSerializer.Serialize(manifest, Pretty));
            var summaries = new List<object>();
            foreach (var fixture in fixtures)
            foreach (var policy in policies)
                summaries.Add(Run(output, fixture, policy));
            File.WriteAllText(Path.Combine(output, "comparison.json"), JsonSerializer.Serialize(summaries, Pretty));
            Console.WriteLine($"{summaries.Count} runs complete: {output}"); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static object Run(string output, string fixture, string policy)
    {
        var directory = Path.Combine(output, $"{fixture}-{policy}"); Directory.CreateDirectory(directory);
        var root = S.Create(policy != "unthrottled", policy == "fixed2" ? 1 : 5, wake: policy == "escalating32-wake2");
        var before = Report.State(root); var latest = before; string? phase = null;
        var phases = new List<PhaseRow>(); var graphs = new List<object>();
        long nativeBytes = 0, reportBytes = 0; double nativeMs = 0, reportMs = 0;
        using var arrivals = new StreamWriter(Path.Combine(directory, "arrivals.jsonl"), append: false, new UTF8Encoding(false));
        foreach (var sample in Fixtures.Trace(fixture))
        {
            if (phase != sample.Phase)
            {
                if (phase is not null) { phases.Add(Report.Phase(phase, before, latest)); graphs.Add(Graph(root)); }
                phase = sample.Phase; before = latest;
            }
            var allocated = GC.GetAllocatedBytesForCurrentThread(); var started = Stopwatch.GetTimestamp();
            var frame = S.Observe(ref root, sample.Level);
            nativeMs += Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            nativeBytes += GC.GetAllocatedBytesForCurrentThread() - allocated;
            allocated = GC.GetAllocatedBytesForCurrentThread(); started = Stopwatch.GetTimestamp();
            arrivals.WriteLine(JsonSerializer.Serialize(Report.Arrival(sample, root, frame)));
            latest = Report.State(root);
            reportMs += Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            reportBytes += GC.GetAllocatedBytesForCurrentThread() - allocated;
            root = S.Retain(root);
        }
        if (phase is not null) phases.Add(Report.Phase(phase, before, latest));
        graphs.Add(Graph(root));
        var bytes = GojoNumericFormat.Encode(root);
        using (var file = File.Create(Path.Combine(directory, "final.native.zip")))
        using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
        using (var entry = zip.CreateEntry("native.gojo", CompressionLevel.Optimal).Open()) entry.Write(bytes);
        // One still-pending paid promise may remain at the end; paid access and
        // resolved evidence are intentionally different totals.
        var summary = new
        {
            Fixture = fixture, Policy = policy, State = latest, Phases = phases, GraphSamples = graphs,
            PendingPaid = S.Parts((Relation)S.Parts(root)[1]).Cast<Relation>().Select(p => S.Parts(p)[2] is CarrierLocation ? 1 : 0).ToArray(),
            PendingEarly = S.Parts((Relation)S.Parts(root)[1]).Cast<Relation>().Select(p => S.Early(p) ? 1 : 0).ToArray(),
            NativeAllocationBytes = nativeBytes, ReportAllocationBytes = reportBytes,
            NativeMilliseconds = nativeMs, ReportMilliseconds = reportMs, NativeTransportBytes = bytes.Length,
            GraphSampleLimit = "Retained graph at phase boundaries and end; not peak transient memory. Allocations are cumulative."
        };
        File.WriteAllText(Path.Combine(directory, "summary.json"), JsonSerializer.Serialize(summary, Pretty));
        Console.WriteLine($"{fixture}/{policy}: {latest.Arrival} arrivals; direction benefit {latest.Consumers[0].Benefit}, amount benefit {latest.Consumers[1].Benefit}; paid {latest.Consumers.Sum(c => c.Paid)}.");
        return summary;
    }

    private static object Graph(Relation root)
    {
        var graph = GojoEncodedSnapshot.CaptureGraph(root);
        return new { Arrival = S.Arrival(root).Ticks, Carriers = graph.Carriers.Length, Relations = graph.Relations.Length,
            Locations = graph.CarrierLocations.Length, Ticks = graph.TickRefs.Length };
    }
    private static string Repository()
    {
        for (var path = new DirectoryInfo(Directory.GetCurrentDirectory()); path is not null; path = path.Parent)
            if (File.Exists(Path.Combine(path.FullName, "Gojo.slnx"))) return path.FullName;
        throw new IOException("Run from the Gojo checkout.");
    }
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static string Fingerprint(string repository)
    {
        var files = Directory.EnumerateFiles(Path.Combine(repository, "experiments/ChangeSubscriptions"), "*.cs")
            .Append(Path.Combine(repository, "experiments/ChangeSubscriptions/ChangeSubscriptions.csproj"))
            .Append(Path.Combine(repository, "experiments/ConnectionAssembly/NativeMath.cs"))
            .Concat(Directory.EnumerateFiles(Path.Combine(repository, "Gojo"), "*.cs", SearchOption.AllDirectories)
                .Where(p => !Path.GetRelativePath(repository, p).Split(Path.DirectorySeparatorChar).Any(x => x is "bin" or "obj")));
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var file in files.Order(StringComparer.Ordinal))
        { hash.AppendData(Encoding.UTF8.GetBytes(Path.GetRelativePath(repository, file))); hash.AppendData(File.ReadAllBytes(file)); }
        hash.AppendData(File.ReadAllBytes(typeof(Program).Assembly.Location));
        hash.AppendData(File.ReadAllBytes(typeof(Carrier).Assembly.Location));
        return Convert.ToHexString(hash.GetHashAndReset());
    }
}
