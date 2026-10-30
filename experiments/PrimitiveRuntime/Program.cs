using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.PrimitiveRuntime;

internal static class Program
{
    public static int Main(string[] args)
    {
        string? Option(string name) => args.FirstOrDefault(value => value.StartsWith(name + "=", StringComparison.Ordinal))?[(name.Length + 1)..];
        var steps = int.TryParse(Option("--steps"), out var requested) ? requested : 1_000_000;
        if (steps < 1) throw new ArgumentOutOfRangeException(nameof(steps));
        var output = Option("--output");
        var motion = new PrimitiveMotion();
        for (var warm = 0; warm < 20_000; warm++) motion.Step();
        motion.Reset();
        var creation = Measure(10, () => new MotionDefinition());
        var capture = Measure(10, () => CompactTransitionPlan.Capture(motion.Definition.Law, motion.Definition.Surface));
        var validation = Measure(10_000, () => motion.Plan.ValidateSource());
        var initial = motion.Occurrence.State;
        var occurrences = new CompactOccurrence[10_000];
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < occurrences.Length; index++) occurrences[index] = motion.Plan.CreateOccurrence(initial);
        var occurrenceAllocation = (GC.GetAllocatedBytesForCurrentThread() - allocated) / (double)occurrences.Length;
        GC.KeepAlive(occurrences);

        ulong directChecksum = 0;
        var directStart = Stopwatch.GetTimestamp(); allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < steps; index++) { motion.Step(); directChecksum += motion.Occurrence.State.Known; }
        var directElapsed = Stopwatch.GetElapsedTime(directStart).TotalNanoseconds;
        var directAllocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        var direct = new Measurement(steps, directElapsed / steps, directAllocated / (double)steps);

        var referenceSteps = Math.Min(steps, 2_048);
        var definition = motion.Definition;
        var pulse = StructuralConstraint.Closed(definition.Pulse);
        var referenceGuard = new TransitionApplicationGuard(definition.Law);
        Relation nativeState = StructuralConstraint.Closed(definition.States[2]);
        // Warm reference interpretation outside timing; it intentionally creates
        // a native read witness while the direct path retains a compact receipt.
        for (var warm = 0; warm < 20; warm++) referenceGuard.TryApply(nativeState, pulse, out _, out _);
        ulong nativeChecksum = 0;
        allocated = GC.GetAllocatedBytesForCurrentThread(); var nativeStart = Stopwatch.GetTimestamp();
        for (var index = 0; index < referenceSteps; index++)
        {
            if (!referenceGuard.TryApply(nativeState, pulse, out var applied, out _)) throw new InvalidOperationException();
            nativeState = StructuralConstraint.Closed(applied);
            nativeChecksum += motion.Plan.StateMask(applied);
        }
        var nativeElapsed = Stopwatch.GetElapsedTime(nativeStart).TotalNanoseconds;
        var nativeAllocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        var native = new Measurement(referenceSteps, nativeElapsed / referenceSteps, nativeAllocated / (double)referenceSteps);
        var matching = motion.Plan.CreateOccurrence(initial); ulong comparisonChecksum = 0;
        var command = CompactSupport.Closed(motion.Plan.CommandMask(definition.Pulse));
        for (var index = 0; index < referenceSteps; index++) comparisonChecksum += matching.Step(command).After.Known;
        if (comparisonChecksum != nativeChecksum) throw new InvalidOperationException("Native/direct checksums differ.");
        var inspection = Measure(100, () => motion.Observe());
        var metricInspection = Measure(100, () => motion.Observe(false, 4));
        var nativeInspection = Measure(20, () => motion.Inspect());
        var retainedDefinitionBytes = RetainedDefinitions(64);
        var report = new
        {
            DateUtc = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            Architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            Scope = "Carrier-free finite transition execution; optional numeric projection is separately observed by existing native kernel.",
            NativeConstruction = creation, Capture = capture, ExplicitValidation = validation,
            CompactUpdates = direct, NativeInterpretationWithWitness = native,
            Inspection = inspection, CalibratedInspection = metricInspection, NativeInspection = nativeInspection,
            ArenaRecords = motion.Plan.Arena.Count, ArenaBytes = motion.Plan.Arena.PayloadBytes,
            CompiledTablesBytes = motion.Plan.TablePayloadBytes,
            DefinitionPayloadBytes = motion.Plan.DefinitionPayloadBytes,
            NativeCorrespondenceReferencesBytes = motion.Plan.Arena.NativeReferenceBytes,
            OccurrencePayloadBytes = CompactOccurrence.MutablePayloadBytes,
            EstimatedOccurrenceObjectBytes = CompactOccurrence.EstimatedObjectBytes,
            MeasuredOccurrenceAllocationBytes = occurrenceAllocation,
            ApproximateRetainedDefinitionBytes = retainedDefinitionBytes,
            DirectChecksum = directChecksum, NativeChecksum = nativeChecksum,
            SameLengthDirectChecksum = comparisonChecksum,
            SharedDefinitionCostPerOccurrenceAt10000 = motion.Plan.DefinitionPayloadBytes / 10_000d,
            EstimatedTotalBytesPerOccurrenceAt10000 = occurrenceAllocation + IntPtr.Size + retainedDefinitionBytes / 10_000d,
            Exclusions = "Definition payload excludes native source graph, address dictionaries, CLR headers and allocator overhead. Approximate retained definition bytes uses full-GC heap delta across 64 retained plans including native sources, maps and array headers; it is a process diagnostic, not an exact object-layout claim. Total amortized occurrence cost includes one owner-array reference. Construction/capture allocation includes temporary compiler objects. No serialization, metric projection, rendering, or native witness construction occurs during compact updates."
        };
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        if (output is not null)
        {
            var path = Path.GetFullPath(output); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json + Environment.NewLine);
            Console.WriteLine($"Measurements: {path}");
        }
        Console.WriteLine(json);
        return 0;
    }

    private static Measurement Measure<T>(int count, Func<T> action)
    {
        action();
        var allocation = GC.GetAllocatedBytesForCurrentThread(); var start = Stopwatch.GetTimestamp();
        T? last = default;
        for (var index = 0; index < count; index++) last = action();
        var elapsed = Stopwatch.GetElapsedTime(start).TotalNanoseconds;
        var bytes = GC.GetAllocatedBytesForCurrentThread() - allocation;
        GC.KeepAlive(last);
        return new(count, elapsed / count, bytes / (double)count);
    }

    private static double RetainedDefinitions(int count)
    {
        var retained = new CompactTransitionPlan[count];
        var before = GC.GetTotalMemory(forceFullCollection: true);
        for (var index = 0; index < count; index++)
        {
            var definition = new MotionDefinition();
            retained[index] = CompactTransitionPlan.Capture(definition.Law, definition.Surface);
        }
        var after = GC.GetTotalMemory(forceFullCollection: true);
        GC.KeepAlive(retained);
        return (after - before) / (double)count;
    }

    private sealed record Measurement(int Operations, double NanosecondsPerOperation, double AllocatedBytesPerOperation);
}
