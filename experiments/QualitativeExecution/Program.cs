using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var options = Options.Parse(args);
            var fixture = new ThresholdFixture();
            var view = fixture.Forward;
            var plan = FiniteLawPlan.Capture(view.Law) ?? throw new InvalidOperationException("The fixture law did not compile.");
            var cases = BuildCases(view, plan);
            var graph = DescribeGraph(view.Law);

            long Native(int index)
            {
                var item = cases[index % cases.Length];
                return ReadMask(plan, FiniteLaw.Evaluate(view.Law, item.Left, item.Right));
            }
            long CompiledWitness(int index)
            {
                var item = cases[index % cases.Length];
                return ReadMask(plan, plan.Evaluate(item.Left, item.Right));
            }
            long ComputeOnly(int index)
            {
                var item = cases[index % cases.Length];
                if (!plan.TryEvaluateMasks(item.LeftMask, item.RightMask, out var result))
                    throw new InvalidOperationException("A current prepared mask was refused.");
                return result;
            }
            long Capture(int _)
            {
                var captured = FiniteLawPlan.Capture(view.Law) ?? throw new InvalidOperationException("Law capture failed.");
                if (!captured.TryEvaluateMasks(4, 1, out var result)) throw new InvalidOperationException("Captured law was unusable.");
                GC.KeepAlive(captured);
                return result;
            }

            // Timings include current-plan validation on every compiled call.
            // Inputs and scopes are prepared before timing for all three modes.
            var native = Measure("native interpretation + witness + result encoding", options.Iterations, Native, true);
            var compiled = Measure("compiled execution + witness + result encoding", options.Iterations, CompiledWitness, true);
            var computeOnly = Measure("compiled masks + current-law validation", options.Iterations, ComputeOnly, false);
            var capture = Measure("capture native law into derived table", Math.Min(options.Iterations, 25), Capture, false, 2);
            if (native.Checksum != compiled.Checksum || native.Checksum != computeOnly.Checksum)
                throw new InvalidOperationException("Execution modes disagreed on the same prepared support sequence.");

            var report = new BenchmarkReport(DateTimeOffset.UtcNow, options.Iterations, cases.Length,
                RuntimeInformation.FrameworkDescription, RuntimeInformation.OSDescription,
                RuntimeInformation.ProcessArchitecture.ToString(), graph,
                [native, compiled, computeOnly, capture],
                [
                    "The source fixture uses relation/placeholder identities and supplied detector categories, with no measured numeric operands.",
                    "All 64 closed support pairs are reused in each execution mode; prepared inputs and capture are outside repeated execution timing.",
                    "Native and compiled witness modes materialize read/result/residue/provenance graphs and encode the resulting support for the checksum.",
                    "Compute-only masks omit new witness graphs, but validate the captured law on every call and retain SourceLaw for interpretation.",
                    "Capture is reported separately and interprets the native atomic rows to derive its table.",
                    "Thread allocation counts are managed allocations on the calling thread; these are one-process diagnostic timings, not stable performance guarantees.",
                    "There is no eager numeric baseline: the detector fixture does not expose numeric samples, and this run does not claim a numerical-arithmetic speedup."
                ]);
            var markdown = Render(report);
            Console.WriteLine(markdown);
            if (options.Output is not null)
            {
                var output = Path.GetFullPath(options.Output);
                Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, "benchmark.json"), JsonSerializer.Serialize(report,
                    new JsonSerializerOptions { WriteIndented = true }));
                File.WriteAllText(Path.Combine(output, "benchmark.md"), markdown);
                Console.WriteLine($"Saved diagnostics to {output}");
            }
            GC.KeepAlive(fixture);
            GC.KeepAlive(plan);
            return 0;
        }
        catch (ArgumentException error)
        {
            Console.Error.WriteLine(error.Message);
            Console.Error.WriteLine("Usage: dotnet run --project experiments/QualitativeExecution -- [--iterations 1..5000] [--output DIRECTORY]");
            return 2;
        }
    }

    private static InputCase[] BuildCases(ThresholdView view, FiniteLawPlan plan)
    {
        var result = new List<InputCase>();
        for (var leftMask = 0; leftMask < 8; leftMask++)
            for (var rightMask = 0; rightMask < 8; rightMask++)
            {
                var left = Support(leftMask, view.Below, view.WithinBand, view.Above);
                var right = Support(rightMask, view.Falling, view.Steady, view.Rising);
                if (!plan.TryEncodeLeft(left, out var a) || !plan.TryEncodeRight(right, out var b))
                    throw new InvalidOperationException("A fixture support was refused.");
                result.Add(new InputCase(left, right, a, b));
            }
        return result.ToArray();
    }

    private static Relation Support(int mask, params IElement[] participants) =>
        StructuralConstraint.Closed(participants.Where((_, index) => (mask & (1 << index)) != 0).ToArray());

    private static byte ReadMask(FiniteLawPlan plan, Relation read)
    {
        if (!FiniteLaw.TryReadResult(read, out var support, out var residue, out _) ||
            FiniteLaw.Entries(residue).Length != 0 || !plan.TryEncodeOutput(support, out var mask))
            throw new InvalidOperationException("An expected closed fixture answer was unresolved.");
        return mask;
    }

    private static BenchmarkMeasurement Measure(string name, int iterations,
        Func<int, long> operation, bool producesWitness, int warmup = 16)
    {
        for (var index = 0; index < warmup; index++) operation(index);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var checksum = 17L;
        var before = GC.GetAllocatedBytesForCurrentThread();
        var start = Stopwatch.GetTimestamp();
        for (var index = 0; index < iterations; index++)
            checksum = unchecked((checksum * 397) ^ operation(index));
        var elapsed = Stopwatch.GetElapsedTime(start);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        return new BenchmarkMeasurement(name, iterations, elapsed.TotalMilliseconds,
            elapsed.TotalNanoseconds / iterations, allocated, (double)allocated / iterations,
            checksum, producesWitness);
    }

    private static GraphDescription DescribeGraph(IElement root)
    {
        var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>();
        pending.Push(root);
        var relations = 0;
        var placeholders = 0;
        var carriers = 0;
        var locations = 0;
        while (pending.TryPop(out var element))
        {
            if (!visited.Add(element)) continue;
            switch (element)
            {
                case Relation relation:
                    relations++;
                    pending.Push(relation.Left);
                    pending.Push(relation.Right);
                    break;
                case Placeholder:
                    placeholders++;
                    break;
                case Carrier:
                    carriers++;
                    break;
                case CarrierLocation:
                    locations++;
                    break;
            }
        }
        return new GraphDescription(relations, placeholders, carriers, locations);
    }

    private static string Render(BenchmarkReport report)
    {
        var lines = new List<string>
        {
            "# Qualitative execution diagnostics",
            "",
            $"Run: {report.RecordedAt:O}; {report.Runtime}; {report.Architecture}.",
            $"Inputs: {report.PreparedSupportPairs} prepared closed qualitative support pairs; {report.RequestedIterations} repeated operations per execution mode.",
            $"Native source law: {report.SourceGraph.Relations} relation nodes, {report.SourceGraph.Placeholders} placeholder instances, {report.SourceGraph.Carriers} carriers, {report.SourceGraph.CarrierLocations} carrier locations.",
            "",
            "| Mode | Operations | ns/op | Allocated bytes/op | Checksum | New witness per read |",
            "|---|---:|---:|---:|---:|---|"
        };
        foreach (var measurement in report.Measurements)
            lines.Add($"| {measurement.Mode} | {measurement.Iterations} | {F(measurement.NanosecondsPerOperation)} | {F(measurement.AllocatedBytesPerOperation)} | {measurement.Checksum} | {(measurement.ProducesWitness ? "yes" : "no")} |");
        lines.Add("");
        lines.AddRange(report.Qualifications.Select(item => $"- {item}"));
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string F(double value) => value.ToString("N1", CultureInfo.InvariantCulture);

    private sealed record InputCase(Relation Left, Relation Right, byte LeftMask, byte RightMask);
    private sealed record GraphDescription(int Relations, int Placeholders, int Carriers, int CarrierLocations);
    private sealed record BenchmarkMeasurement(string Mode, int Iterations, double ElapsedMilliseconds,
        double NanosecondsPerOperation, long AllocatedBytes, double AllocatedBytesPerOperation,
        long Checksum, bool ProducesWitness);
    private sealed record BenchmarkReport(DateTimeOffset RecordedAt, int RequestedIterations,
        int PreparedSupportPairs, string Runtime, string OperatingSystem, string Architecture,
        GraphDescription SourceGraph, BenchmarkMeasurement[] Measurements, string[] Qualifications);

    private sealed record Options(int Iterations, string? Output)
    {
        public static Options Parse(string[] args)
        {
            var iterations = 512;
            string? output = null;
            for (var index = 0; index < args.Length; index++)
                switch (args[index])
                {
                    case "--iterations" when index + 1 < args.Length &&
                        int.TryParse(args[++index], NumberStyles.None, CultureInfo.InvariantCulture, out var requested) &&
                        requested is >= 1 and <= 5000:
                        iterations = requested;
                        break;
                    case "--output" when index + 1 < args.Length:
                        output = args[++index];
                        if (string.IsNullOrWhiteSpace(output)) throw new ArgumentException("Output directory must be nonempty.");
                        break;
                    default:
                        throw new ArgumentException("Expected --iterations 1..5000 or --output DIRECTORY.");
                }
            return new Options(iterations, output);
        }
    }
}
