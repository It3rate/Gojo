using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Gojo.Core;

namespace Gojo.Experiments.RefinementPerformance;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var options = Options.Parse(args);
            var measurements = new List<Measurement>();
            foreach (var size in options.Sizes)
            {
                var modes = new (string Name, Func<IPerformanceWorkload> Create)[]
                {
                    ("qualitative-native-witness", () => new QualitativeCircuitWorkload(size, CircuitBackend.NativeWitness)),
                    ("qualitative-compiled-witness", () => new QualitativeCircuitWorkload(size, CircuitBackend.CompiledWitness)),
                    ("qualitative-batched-masks", () => new QualitativeCircuitWorkload(size, CircuitBackend.BatchedMasks)),
                    ("numeric-native-witness", () => new NumericWorkload(size)),
                    ("numeric-compiled-compute", () => new NumericWorkload(size, compiled: true))
                }.Where(mode => options.Mode == "all" || options.Mode == "optimized" &&
                    (mode.Name.Contains("batched") || mode.Name == "numeric-compiled-compute") ||
                    mode.Name.Contains(options.Mode, StringComparison.Ordinal)).ToArray();
                foreach (var mode in modes)
                {
                    Console.WriteLine($"Warmup: {mode.Name}, {size} cells");
                    var warmup = mode.Create();
                    warmup.Run(Math.Clamp(512 / size, 2, 16));
                    GC.KeepAlive(warmup);
                }
                // Rotate order across samples; each sample begins with a fresh world.
                for (var sample = 0; sample < options.Samples; sample++)
                    for (var offset = 0; offset < modes.Length; offset++)
                    {
                        var mode = modes[(offset + sample) % modes.Length];
                        Console.WriteLine($"Measure: {mode.Name}, {size} cells x {options.Rounds} rounds, sample {sample + 1}");
                        var row = Measure("recurrent-ring", mode.Name, size, options.Rounds, sample + 1, mode.Create);
                        measurements.Add(row);
                        Console.WriteLine($"  {row.ElapsedMilliseconds:N2} ms; {row.AllocatedBytes:N0} B; checksum {row.Checksum}");
                    }
            }
            if (options.Coverage && options.Mode == "all")
            {
                foreach (var measured in new[] { false, true })
                    foreach (var compiled in new[] { false, true })
                    {
                        var name = $"{(measured ? "measured" : "qualitative")}-coverage-{(compiled ? "compiled-ordinal" : "native")}";
                        new StructuralCoverageWorkload(1, measured, compiled).Run(1);
                        for (var sample = 1; sample <= options.Samples; sample++)
                        {
                            Console.WriteLine($"Measure: {name}, {options.CoverageSize} cells x {options.CoverageRounds} episodes, sample {sample}");
                            var row = Measure("structural-coverage", name, options.CoverageSize, options.CoverageRounds,
                                sample, () => new StructuralCoverageWorkload(options.CoverageSize, measured, compiled));
                            measurements.Add(row);
                            Console.WriteLine($"  {row.ElapsedMilliseconds:N2} ms; {row.AllocatedBytes:N0} B; checksum {row.Checksum}");
                        }
                    }
            }
            ValidateChecksums(measurements);
            var report = new Report(DateTimeOffset.UtcNow, ReadRevision(), RuntimeInformation.FrameworkDescription,
                RuntimeInformation.OSDescription, RuntimeInformation.ProcessArchitecture.ToString(),
                Environment.ProcessorCount, System.Runtime.GCSettings.IsServerGC,
                Environment.GetEnvironmentVariable("DOTNET_TieredPGO"), BuildConfiguration,
                SourceFingerprint(), options, measurements.ToArray(),
                [
                    "One process, single-thread execution; build configuration recorded; construction/capture and execution measured separately; raw samples retained.",
                    "Identical qualitative circuit/checksums for native witnesses, compiled witnesses and batched masks. Different answers and detailed dynamics are expected in the numeric counterpart.",
                    "The qualitative ring executes ten native finite-law applications per cell-round. Compilation derives atomic tables, including nine stock-transition identities, from those same laws.",
                    "Numeric ring executes five equation reads, two numeric conditional selections, stock settlement, and explicit signal/previous/command delta imports. Persistent neighboring ports feed subsequent rounds. Its compiled lane derives arithmetic/interval plans from native equations/policies and still uses kernel tension settlement.",
                    "Batched masks omit per-event witness allocation/history and validate source dependencies once per stage batch. This is a different evidence cost, reported separately; it retains the source laws and supports selected witness reconstruction.",
                    "Numeric stock retains clamp pressure; qualitative stock rejects blocked pulses. This benchmark is a resource comparison of corresponding models, not a claim of behavioral equivalence or an isolated arithmetic-speed comparison.",
                    "Audit costs after execution are separate: qualitative decodes current state and one final law read; numeric decodes current state and reconstructs/verifies a final cell's native equation/policy evidence. Neither reconstructs an unretained full history.",
                    "Coverage repeats bounded witnessed episodes including patterns, resolution, refinement, laws and event schedules. Measured coverage adds calibrated numeric interpretation; only ordinal evaluation is compiled in that suite.",
                    "Allocated bytes are managed allocations on the executing thread, not retained memory or process peak. No learning, full creature physics, result-quality score or speed threshold is used."
                ]);
            var markdown = Render(report);
            var directory = Path.GetFullPath(options.Output);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "comparison.json"), JsonSerializer.Serialize(report,
                new JsonSerializerOptions { WriteIndented = true }));
            File.WriteAllText(Path.Combine(directory, "comparison.md"), markdown);
            Console.WriteLine(markdown);
            Console.WriteLine($"Saved {directory}");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static Measurement Measure(string family, string mode, int size, int rounds,
        int sample, Func<IPerformanceWorkload> create)
    {
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var setupBytes = GC.GetAllocatedBytesForCurrentThread();
        var setupStart = Stopwatch.GetTimestamp();
        var workload = create();
        var setupElapsed = Stopwatch.GetElapsedTime(setupStart).TotalMilliseconds;
        setupBytes = GC.GetAllocatedBytesForCurrentThread() - setupBytes;
        var graph = Describe(workload.Surface);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var gen0 = GC.CollectionCount(0); var gen1 = GC.CollectionCount(1); var gen2 = GC.CollectionCount(2);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var start = Stopwatch.GetTimestamp();
        var checksum = workload.Run(rounds);
        var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        var collections0 = GC.CollectionCount(0) - gen0; var collections1 = GC.CollectionCount(1) - gen1;
        var collections2 = GC.CollectionCount(2) - gen2;
        var auditBefore = GC.GetAllocatedBytesForCurrentThread();
        var auditStart = Stopwatch.GetTimestamp();
        IElement? audit = workload switch
        {
            QualitativeCircuitWorkload qualitative => qualitative.Audit(),
            NumericWorkload numeric => numeric.Snapshot(),
            StructuralCoverageWorkload coverage => coverage.LatestWitness,
            _ => null
        };
        var auditElapsed = Stopwatch.GetElapsedTime(auditStart).TotalMilliseconds;
        var auditBytes = GC.GetAllocatedBytesForCurrentThread() - auditBefore;
        GC.KeepAlive(audit);
        GC.KeepAlive(workload);
        return new Measurement(family, mode, size, rounds, sample, setupElapsed, setupBytes,
            elapsed, allocated, collections0, collections1, collections2,
            auditElapsed, auditBytes, checksum, graph);
    }

    private static void ValidateChecksums(List<Measurement> rows)
    {
        foreach (var group in rows.GroupBy(row => (row.Family, row.Size, row.Rounds)))
        {
            var comparable = group.Where(row => row.Family == "structural-coverage" || row.Mode.StartsWith("qualitative-"));
            if (comparable.Select(row => row.Checksum).Distinct().Count() > 1)
                throw new InvalidOperationException("Corresponding qualitative execution modes disagreed.");
        }
        foreach (var group in rows.GroupBy(row => (row.Family, row.Mode, row.Size, row.Rounds)))
            if (group.Select(row => row.Checksum).Distinct().Count() != 1)
                throw new InvalidOperationException("A fresh workload did not repeat deterministically.");
        foreach (var group in rows.Where(row => row.Family == "recurrent-ring" && row.Mode.StartsWith("numeric-"))
                     .GroupBy(row => (row.Size, row.Rounds)))
            if (group.Select(row => row.Checksum).Distinct().Count() > 1)
                throw new InvalidOperationException("Native and compiled numeric models disagreed.");
    }

    private static Graph Describe(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(root);
        var relations = 0; var placeholders = 0; var carriers = 0; var locations = 0;
        while (pending.TryPop(out var element))
        {
            if (!seen.Add(element)) continue;
            switch (element)
            {
                case Relation r: relations++; pending.Push(r.Left); pending.Push(r.Right); break;
                case Carrier: carriers++; break;
                case CarrierLocation: locations++; break;
                case Placeholder: placeholders++; break;
            }
        }
        return new Graph(relations, placeholders, carriers, locations);
    }

    private static string ReadRevision()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("git", "rev-parse HEAD")
                { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true });
            var revision = process!.StandardOutput.ReadToEnd().Trim(); process.WaitForExit();
            return revision;
        }
        catch { return "unavailable"; }
    }

    private static string SourceFingerprint()
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var directory in new[] { "Gojo", "experiments/QualitativeExecution", "experiments/RefinementPerformance" })
            foreach (var path in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
                         .Where(path => !path.Replace('\\', '/').Split('/').Any(part => part is "bin" or "obj")).Order(StringComparer.Ordinal))
            {
                hash.AppendData(Encoding.UTF8.GetBytes(path.Replace('\\', '/') + "\n"));
                hash.AppendData(Encoding.UTF8.GetBytes(File.ReadAllText(path).Replace("\r\n", "\n")));
            }
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static string BuildConfiguration =>
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    private static string Render(Report report)
    {
        var text = new StringBuilder();
        text.AppendLine("# Refinement compute comparison").AppendLine();
        text.AppendLine($"Recorded {report.RecordedAt:O}; source base {report.SourceRevision} plus current working tree.");
        text.AppendLine($"{report.BuildConfiguration}; normalized source SHA-256: {report.SourceFilesSha256}.");
        text.AppendLine($"{report.Runtime}; {report.OperatingSystem}; {report.Architecture}; {report.LogicalProcessors} logical processors; server GC={report.ServerGc}.");
        text.AppendLine($"Single thread; {report.Options.Samples} samples; medians below, raw samples in comparison.json.").AppendLine();
        text.AppendLine("| Workload | Mode | Cells × rounds | Execution ms median [min, max] | Cell-rounds/s | Bytes/cell-round | Setup ms median | Audit ms median |");
        text.AppendLine("| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |");
        foreach (var group in report.Measurements.GroupBy(row => (row.Family, row.Mode, row.Size, row.Rounds)))
        {
            var rows = group.ToArray(); var count = (long)group.Key.Size * group.Key.Rounds;
            var times = rows.Select(row => row.ElapsedMilliseconds).Order().ToArray();
            var milliseconds = Median(times);
            text.AppendLine($"| {group.Key.Family} | {group.Key.Mode} | {group.Key.Size} × {group.Key.Rounds} | " +
                $"{F(milliseconds)} [{F(times[0])}, {F(times[^1])}] | {F(count * 1000 / milliseconds)} | " +
                $"{F(Median(rows.Select(row => (double)row.AllocatedBytes).ToArray()) / count)} | " +
                $"{F(Median(rows.Select(row => row.SetupMilliseconds).ToArray()))} | {F(Median(rows.Select(row => row.AuditMilliseconds).ToArray()))} |");
        }
        text.AppendLine().AppendLine("Source graphs (relations / placeholders / carriers / boxed locations; reachable relation graph, not a heap census):").AppendLine();
        foreach (var group in report.Measurements.GroupBy(row => (row.Family, row.Mode, row.Size)))
        {
            var row = group.First(); var graph = row.SourceGraph;
            text.AppendLine($"- {row.Family}, {row.Mode}, {row.Size} cells: {graph.Relations} / {graph.Placeholders} / {graph.Carriers} / {graph.Locations}.");
        }
        text.AppendLine(); foreach (var qualification in report.Qualifications) text.AppendLine($"- {qualification}");
        return text.ToString();
    }
    private static double Median(double[] values) { Array.Sort(values); return values.Length % 2 == 1 ? values[values.Length / 2] : (values[values.Length / 2 - 1] + values[values.Length / 2]) / 2; }
    private static string F(double value) => value.ToString("N2", CultureInfo.InvariantCulture);
    private sealed record Graph(int Relations, int Placeholders, int Carriers, int Locations);
    private sealed record Measurement(string Family, string Mode, int Size, int Rounds, int Sample,
        double SetupMilliseconds, long SetupBytes, double ElapsedMilliseconds, long AllocatedBytes,
        int Gen0, int Gen1, int Gen2, double AuditMilliseconds, long AuditBytes, long Checksum, Graph SourceGraph);
    private sealed record Report(DateTimeOffset RecordedAt, string SourceRevision, string Runtime,
        string OperatingSystem, string Architecture, int LogicalProcessors, bool ServerGc,
        string? TieredPgoEnvironment, string BuildConfiguration, string SourceFilesSha256,
        Options Options, Measurement[] Measurements, string[] Qualifications);
    private sealed record Options(int[] Sizes, int Rounds, int Samples, bool Coverage,
        int CoverageSize, int CoverageRounds, string Mode, string Output)
    {
        public static Options Parse(string[] args)
        {
            int[] sizes = [32, 128, 512]; var rounds = 128; var samples = 3;
            var coverage = true; var coverageSize = 8; var coverageRounds = 8;
            var mode = "all"; var output = "experiments/RefinementPerformance/results/latest";
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "--no-coverage") { coverage = false; continue; }
                if (i + 1 == args.Length) throw new ArgumentException($"Missing value for {args[i]}.");
                var option = args[i]; var value = args[++i];
                switch (option)
                {
                    case "--sizes": sizes = value.Split(',').Select(v => int.Parse(v, CultureInfo.InvariantCulture)).ToArray(); break;
                    case "--rounds": rounds = int.Parse(value, CultureInfo.InvariantCulture); break;
                    case "--samples": samples = int.Parse(value, CultureInfo.InvariantCulture); break;
                    case "--coverage-size": coverageSize = int.Parse(value, CultureInfo.InvariantCulture); break;
                    case "--coverage-rounds": coverageRounds = int.Parse(value, CultureInfo.InvariantCulture); break;
                    case "--mode": mode = value; break;
                    case "--output": output = value; break;
                    default: throw new ArgumentException($"Unknown option {option}.");
                }
            }
            if (sizes.Length == 0 || sizes.Any(s => s < 2 || s > 16384) || rounds < 1 || rounds > 1000000 ||
                samples < 1 || samples > 9 || coverageSize < 1 || coverageRounds < 1 ||
                mode is not ("all" or "native" or "compiled" or "batched" or "qualitative" or "numeric" or "optimized"))
                throw new ArgumentException("Invalid benchmark bounds or mode.");
            return new Options(sizes, rounds, samples, coverage, coverageSize, coverageRounds, mode, output);
        }
    }
}
