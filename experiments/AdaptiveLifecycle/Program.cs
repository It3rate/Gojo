using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gojo.Core;
using CompositeEmergence;
using PrimitiveLearning;

namespace AdaptiveLifecycle;

public static class Program
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    public static int Main(string[] args)
    {
        string? output = null; var started = false;
        try
        {
            if (args.Contains("--help"))
            {
                Console.WriteLine("Adaptive lifecycle verification: --pilot or --campaign; --output DIRECTORY; --seeds N; --seed-start N. Source, consumer and manager budgets are fixed in the recorded settings.");
                return 0;
            }
            var campaign = args.Contains("--campaign"); var seeds = campaign ? 8 : 2; var seedStart = campaign ? 410 : 400;
            for (var index = 0; index < args.Length; index++)
            {
                var name = args[index]; if (name is "--pilot" or "--campaign") continue;
                if (++index >= args.Length) throw new ArgumentException($"Missing value for {name}.");
                var value = args[index];
                switch (name)
                {
                    case "--seeds": seeds = int.Parse(value); break;
                    case "--seed-start": seedStart = int.Parse(value); break;
                    case "--output": output = Path.GetFullPath(value); break;
                    default: throw new ArgumentException($"Unknown option {name}.");
                }
            }
            if (seeds is < 1 or > 64 || seedStart < 0) throw new ArgumentException("Seeds must be 1..64 and seed start nonnegative.");
            output ??= Path.GetFullPath(Path.Combine("experiments", "AdaptiveLifecycle", "results",
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + (campaign ? "-confirmation" : "-pilot")));
            if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
                throw new IOException($"Output already contains files: {output}. Use a new directory.");
            Directory.CreateDirectory(output); started = true;
            var settings = new AdaptationSettings(); var startedUtc = DateTime.UtcNow; var timer = Stopwatch.StartNew();
            Save(output, "manifest.json", new { startedUtc, seeds, seedStart, campaign, settings,
                runtime = RuntimeInformation.FrameworkDescription, sourceHashes = SourceHashes(), paidJevCalls = 0 });
            var lifecycle = LifecycleVerification.Run(output);
            Save(output, "lifecycle.json", lifecycle);
            for (var seed = seedStart; seed < checked(seedStart + seeds); seed++)
            {
                Console.WriteLine($"Seed {seed}: training and paired continuous/frozen trials.");
                var run = AdaptationTrial.Run(seed, settings); Save(output, $"seed-{seed}.json", run);
                if (seed == seedStart)
                {
                    var archiveChecks = new List<object>();
                    foreach (var (name, root) in run.NativeRoots)
                    {
                        var filename = $"{name.Replace('/', '-')}.gojo"; var path = Path.Combine(output, filename);
                        File.WriteAllBytes(path, GojoNumericFormat.Encode(root));
                        var restored = (Relation)GojoNumericFormat.Decode(File.ReadAllBytes(path))[0];
                        var signatureMatched = NativeInspection.Signature(root) == NativeInspection.Signature(restored);
                        var originalFields = NativeGraph.Parts(root, 5); var restoredFields = NativeGraph.Parts(restored, 5);
                        var behaviorMatched = ArchiveVerifier.VerifyPipeline(NativeGraph.G(originalFields.Take(4).ToArray()),
                            NativeGraph.G(restoredFields.Take(4).ToArray()));
                        if (!signatureMatched || !behaviorMatched) throw new InvalidOperationException($"Saved archive replay failed: {filename}.");
                        archiveChecks.Add(new { arm = name, filename, signatureMatched, behaviorMatched, bytes = new FileInfo(path).Length });
                    }
                    Save(output, "archive-check.json", archiveChecks);
                }
                Console.WriteLine($"Seed {seed}: saved; {timer.Elapsed.TotalSeconds:F1} seconds elapsed.");
            }
            Save(output, "run.json", new { status = "complete", startedUtc, finishedUtc = DateTime.UtcNow,
                elapsedSeconds = timer.Elapsed.TotalSeconds, seeds, seedStart, campaign, paidJevCalls = 0 });
            Console.WriteLine($"Results: {output}"); return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            if (started && output is not null) Save(output, "failure.json", new { error = exception.ToString(), utc = DateTime.UtcNow });
            return 1;
        }
    }

    private static void Save(string directory, string filename, object value) =>
        File.WriteAllText(Path.Combine(directory, filename), JsonSerializer.Serialize(value, Json));

    private static Dictionary<string, string> SourceHashes()
    {
        var candidates = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
        DirectoryInfo? repository = null;
        foreach (var candidate in candidates)
            for (var current = new DirectoryInfo(candidate); current is not null; current = current.Parent)
                if (File.Exists(Path.Combine(current.FullName, "Gojo.slnx"))) { repository = current; break; }
        if (repository is null) throw new DirectoryNotFoundException("Cannot locate Gojo source manifest.");
        var roots = new[] { "Gojo", "experiments/PrimitiveLearning", "experiments/CompositeEmergence", "experiments/AdaptiveLifecycle" };
        var files = roots.SelectMany(root => Directory.EnumerateFiles(Path.Combine(repository.FullName, root), "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"));
        return files.OrderBy(path => path, StringComparer.Ordinal).ToDictionary(path => Path.GetRelativePath(repository.FullName, path).Replace('\\', '/'),
            path => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant());
    }
}
