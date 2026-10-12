using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Gojo.Core;
using PrimitiveLearning;

namespace UtilityEcology;

/// <summary>Separate, reversible study entry point. No legacy study settings
/// are changed when --seed-trial is absent.</summary>
public static class SeedTrialProgram
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    public static int Run(string[] args)
    {
        string? output = null; var started = false;
        try
        {
            if (args.Contains("--help"))
            { Console.WriteLine("Protected candidate trials: --seed-trial --pilot or --campaign --output DIRECTORY; optional --seeds N --seed-start N --phase-length N. Legacy runs omit --seed-trial."); return 0; }
            var campaign = args.Contains("--campaign"); var seeds = campaign ? 8 : 2; var seedStart = campaign ? 1000 : 900;
            var settings = new EcologySettings();
            for (var index = 0; index < args.Length; index++)
            {
                var name = args[index]; if (name is "--seed-trial" or "--pilot" or "--campaign") continue;
                if (++index >= args.Length) throw new ArgumentException($"Missing value for {name}.");
                var value = args[index];
                switch (name)
                {
                    case "--output": output = Path.GetFullPath(value); break;
                    case "--seeds": seeds = int.Parse(value); break;
                    case "--seed-start": seedStart = int.Parse(value); break;
                    case "--phase-length": settings = settings with { PhaseLength = int.Parse(value) }; break;
                    default: throw new ArgumentException($"Unknown seed-trial option {name}.");
                }
            }
            if (seeds is < 1 or > 64 || seedStart < 0 || settings.PhaseLength < 16) throw new ArgumentException("Bounded positive seed counts and phase lengths >=16 are required.");
            output ??= Path.GetFullPath(Path.Combine("experiments", "UtilityEcology", "results", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-seed-trial"));
            if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any()) throw new IOException("Choose an empty output directory.");
            Directory.CreateDirectory(output); started = true; var timer = Stopwatch.StartNew(); var startedUtc = DateTime.UtcNow;
            Save(output, "manifest.json", new { startedUtc, campaign, study = "protected-seed-trials", seeds, seedStart, settings,
                arms = SeedTrialRunner.Arms, trainingEvents = 16, validationEvents = 32, trialWorkCap = 320,
                minimumKnownValidation = 24, retryPeriod = 64, maximumAttempts = 8, paidJevCalls = 0, sourceHashes = SourceHashes() });
            var archives = new List<object>();
            for (var seed = seedStart; seed < checked(seedStart + seeds); seed++)
                foreach (var arm in SeedTrialRunner.Arms)
                {
                    Console.WriteLine($"Seed {seed}: {arm.Name}"); var run = SeedTrialRunner.Run(seed, arm, settings);
                    var stem = $"{seed}-{arm.Name.Replace('/', '-')}"; Save(output, stem + ".json", run);
                    if (seed == seedStart)
                    {
                        var path = Path.Combine(output, stem + ".gojo"); File.WriteAllBytes(path, GojoNumericFormat.Encode(run.NativeRoot));
                        var restored = (Relation)GojoNumericFormat.Decode(File.ReadAllBytes(path))[0];
                        var signature = NativeInspection.Signature(run.NativeRoot) == NativeInspection.Signature(restored);
                        var behavior = SeedTrialArchive.Check(run.NativeRoot, restored, arm.ProtectedTrials);
                        if (!signature || !behavior.Matched || !behavior.Conserved || !behavior.AliasesMatched) throw new InvalidOperationException("Saved seed-trial native graph failed scoped replay.");
                        archives.Add(new { seed, arm = arm.Name, filename = stem + ".gojo", signatureMatched = signature, behavior, bytes = new FileInfo(path).Length });
                        Save(output, "archive-check.json", archives);
                    }
                    Console.WriteLine($"  net {run.Ecology.NetPerStep:F3}, {timer.Elapsed.TotalSeconds:F1}s elapsed");
                }
            Save(output, "run.json", new { status = "complete", startedUtc, finishedUtc = DateTime.UtcNow, elapsedSeconds = timer.Elapsed.TotalSeconds,
                seeds, seedStart, arms = SeedTrialRunner.Arms.Length, trials = checked(seeds * SeedTrialRunner.Arms.Length * settings.PhaseLength * 4), paidJevCalls = 0 });
            Console.WriteLine($"Results: {output}"); return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error); if (started && output is not null) Save(output, "failure.json", new { error = error.ToString(), utc = DateTime.UtcNow }); return 1;
        }
    }
    private static void Save(string directory, string file, object value) => File.WriteAllText(Path.Combine(directory, file), JsonSerializer.Serialize(value, Json));
    private static Dictionary<string, string> SourceHashes()
    {
        var root = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Gojo.slnx"))) root = root.Parent;
        if (root is null) throw new DirectoryNotFoundException("Run within the Gojo repository.");
        var folders = new[] { "Gojo", "experiments/PrimitiveLearning", "experiments/CompositeEmergence", "experiments/AdaptiveLifecycle", "experiments/UtilityEcology" };
        return folders.SelectMany(folder => Directory.EnumerateFiles(Path.Combine(root.FullName, folder), "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj" or "backout"))
            .OrderBy(path => path, StringComparer.Ordinal).ToDictionary(path => Path.GetRelativePath(root.FullName, path).Replace('\\', '/'),
                path => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant());
    }
}
