using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Local Release cost landmark, not a training-throughput benchmark.
/// One study includes four authored cases, ten stages and eight fresh output
/// executions. Complete native evidence capture is measured separately from
/// execution; no gallery rendering or per-iteration files enter either sample.</summary>
public static class EquivalenceCostProbe
{
    private sealed record Sample(double ElapsedMilliseconds, double CpuMilliseconds,
        long AllocatedBytes, long ManagedBytesAfter, int Gen0Collections, int Gen1Collections, int Gen2Collections);

    public static int Run(string[] args)
    {
        var repetitions = 7;
        var output = Path.Combine("artifacts", "equivalence-local-costs.json");
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else if (args[i] == "--repetitions" && i + 1 < args.Length && int.TryParse(args[++i], out var requested) && requested > 0)
                repetitions = requested;
            else throw new ArgumentException("Usage: --equivalence-costs [--output FILE] [--repetitions POSITIVE_INTEGER]");

        using var process = Process.GetCurrentProcess();
        long sampledWorking = 0, sampledPrivate = 0;
        using var stopSampling = new CancellationTokenSource();
        var sampler = Task.Run(async () =>
        {
            while (!stopSampling.IsCancellationRequested)
            {
                process.Refresh();
                sampledWorking = Math.Max(sampledWorking, process.WorkingSet64);
                sampledPrivate = Math.Max(sampledPrivate, process.PrivateMemorySize64);
                try { await Task.Delay(20, stopSampling.Token); }
                catch (OperationCanceledException) { break; }
            }
        });

        // This first construction is process-cold (JIT included), not an OS
        // cold-start measurement. SDK build and process launch are excluded.
        EquivalenceRevisionStudy? last = null;
        var first = Measure(() => last = EquivalenceRevisionStudy.Build());
        var samples = new List<Sample>();
        for (var i = 0; i < repetitions; i++)
            samples.Add(Measure(() => last = EquivalenceRevisionStudy.Build()));
        var study = last!;
        var participants = Count(study.Surface);
        var stages = study.Cases.Sum(item => item.Stages.Count);
        var outputs = study.Cases.Sum(item => item.Stages.Count(stage => stage.Output is not null));

        long jsonBytes = 0, gzipBytes = 0;
        void Capture()
        {
            var archive = GojoStructureCompactJson.Serialize(
                GojoStructureCompactJson.FromElement("equivalence-cost-probe-v1", study.Surface));
            jsonBytes = System.Text.Encoding.UTF8.GetByteCount(archive);
            if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(archive), out var restored) ||
                Count(restored.Root) != participants)
                throw new InvalidOperationException("The timed native archive must restore all reachable participants.");
            using var compressed = new MemoryStream();
            using (var zip = new GZipStream(compressed, CompressionLevel.SmallestSize, true))
            using (var writer = new StreamWriter(zip)) writer.Write(archive);
            gzipBytes = compressed.Length;
        }
        var captureFirst = Measure(Capture);
        var captures = Enumerable.Range(0, Math.Min(3, repetitions)).Select(_ => Measure(Capture)).ToArray();
        stopSampling.Cancel(); sampler.GetAwaiter().GetResult(); process.Refresh();
        var median = Median(samples.Select(sample => sample.ElapsedMilliseconds));
        var medianCapture = Median(captures.Select(sample => sample.ElapsedMilliseconds));
        var report = new
        {
            Schema = "equivalence-local-costs-v1", MeasuredAtUtc = DateTimeOffset.UtcNow,
            Runtime = RuntimeInformation.FrameworkDescription, RuntimeInformation.ProcessArchitecture,
            RuntimeInformation.OSDescription, LogicalProcessors = Environment.ProcessorCount,
#if DEBUG
            Configuration = "Debug",
#else
            Configuration = "Release",
#endif
            CountsPerStudy = new { Cases = study.Cases.Count, Stages = stages, OutputExecutions = outputs,
                AdditionalExemplarExecutions = 1, NativeParticipants = participants },
            FirstConstruction = first,
            WarmConstruction = new { Repetitions = samples.Count, MedianElapsedMilliseconds = median,
                MedianCpuMilliseconds = Median(samples.Select(sample => sample.CpuMilliseconds)),
                MedianAllocatedBytes = Median(samples.Select(sample => (double)sample.AllocatedBytes)), Samples = samples },
            FirstArchive = captureFirst,
            WarmArchive = new { Repetitions = captures.Length, MedianElapsedMilliseconds = medianCapture,
                MedianCpuMilliseconds = Median(captures.Select(sample => sample.CpuMilliseconds)),
                MedianAllocatedBytes = Median(captures.Select(sample => (double)sample.AllocatedBytes)),
                JsonUtf8Bytes = jsonBytes, GzipBytes = gzipBytes, RoundTrip = true, Samples = captures },
            Memory = new { SampleIntervalMilliseconds = 20, SampledMaximumWorkingSetBytes = sampledWorking,
                SampledMaximumPrivateBytes = sampledPrivate, ProcessLifetimePeakWorkingSetBytes = process.PeakWorkingSet64 },
            ConditionalProjection = new { TargetStages = 32000,
                ConstructionOnlyHours = median / stages * 32000 / 3600000,
                ConstructionAndFullArchiveHours = (median + medianCapture) / stages * 32000 / 3600000,
                FullArchiveBytes = gzipBytes * (32000L / stages) },
            Limits = new[] {
                "One sample rebuilds the entire four-case study, not one independent training episode. Per-stage projections are workload normalization only.",
                "Warm runs exclude SDK build, process launch, HTML/SVG generation, file I/O and statistical policy training. First construction includes JIT.",
                "Archive samples include capture, JSON serialization, parsing, full restoration, participant-count check and smallest-size gzip in memory.",
                "Allocation is cumulative managed allocation, not live RAM. Memory maxima cover this process and both phases; 20 ms sampling can miss brief peaks.",
                "Projection assumes exactly this small workload and sequential execution. Different structure size, search breadth and training can change costs substantially.",
                "No hidden-source training, GPU computation, remote calls or paid infrastructure is involved. Retaining every full native archive is optional."
            }
        };
        var absolute = Path.GetFullPath(output); Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
        File.WriteAllText(absolute, JsonSerializer.Serialize(report, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
        Console.WriteLine($"{samples.Count} warm studies: median {median:0.###} ms; {participants:N0} native participants / {stages} stages.");
        Console.WriteLine($"Full native archive + restore + gzip: median {medianCapture:0.###} ms; {gzipBytes:N0} compressed bytes.");
        Console.WriteLine(absolute); return 0;
    }

    private static Sample Measure(Action action)
    {
        using var process = Process.GetCurrentProcess();
        var cpu = process.TotalProcessorTime; var allocation = GC.GetTotalAllocatedBytes(true);
        var gen0 = GC.CollectionCount(0); var gen1 = GC.CollectionCount(1); var gen2 = GC.CollectionCount(2);
        var timer = Stopwatch.StartNew(); action(); timer.Stop();
        process.Refresh();
        return new(timer.Elapsed.TotalMilliseconds, (process.TotalProcessorTime - cpu).TotalMilliseconds,
            GC.GetTotalAllocatedBytes(true) - allocation, GC.GetTotalMemory(false),
            GC.CollectionCount(0) - gen0, GC.CollectionCount(1) - gen1, GC.CollectionCount(2) - gen2);
    }
    private static double Median(IEnumerable<double> values)
    {
        var ordered = values.Order().ToArray();
        return ordered.Length % 2 == 1 ? ordered[ordered.Length / 2]
            : (ordered[ordered.Length / 2 - 1] + ordered[ordered.Length / 2]) / 2;
    }
    private static int Count(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var current))
            if (seen.Add(current) && current is Relation relation)
            { pending.Push(relation.Left); pending.Push(relation.Right); }
        return seen.Count;
    }
}
