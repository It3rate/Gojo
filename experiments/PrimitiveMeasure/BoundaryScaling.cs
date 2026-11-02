using System.Diagnostics;

namespace Gojo.Experiments.PrimitiveMeasure;

public sealed record BoundaryMeasurement(int Sites, int Budget, int PayloadBytes, double BuildMilliseconds,
    double MedianReadMilliseconds, long MedianReadAllocatedBytes, BoundaryRead Read);

/// <summary>Explicit host apparatus. Timing and allocations describe this implementation,
/// not Gojo magnitudes, intelligence, or a universal maximum structure size.</summary>
public static class BoundaryScaling
{
    public static OrderedBoundary Ring(int sites) => new(sites,
        Enumerable.Range(0, sites).Select(i => new[] { i, (i + 1) % sites }).ToArray(),
        Enumerable.Range(0, sites).Select(i => i << 1).ToArray());

    public static BoundaryMeasurement Measure(int sites, int budget)
    {
        var clock = Stopwatch.StartNew(); var ring = Ring(sites); clock.Stop();
        var build = clock.Elapsed.TotalMilliseconds;
        ring.Read(budget); // Warm the selected reader before collecting samples.
        var times = new double[9]; var allocations = new long[9]; BoundaryRead? read = null;
        for (var i = 0; i < times.Length; i++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread(); clock.Restart();
            read = ring.Read(budget); clock.Stop();
            allocations[i] = GC.GetAllocatedBytesForCurrentThread() - before;
            times[i] = clock.Elapsed.TotalMilliseconds;
        }
        Array.Sort(times); Array.Sort(allocations);
        return new(sites, budget, ring.PayloadBytes, build, times[4], allocations[4], read!);
    }

    public static void Run()
    {
        Console.WriteLine("sites,budget,payload_bytes,build_ms,read_median_ms,read_allocated_bytes,visited,closed");
        foreach (var size in new[] { 16, 256, 4096, 16384 })
            foreach (var budget in new[] { size, Math.Max(1, size / 4) })
            {
                var m = Measure(size, budget);
                Console.WriteLine(FormattableString.Invariant($"{size},{budget},{m.PayloadBytes},{m.BuildMilliseconds:F4},{m.MedianReadMilliseconds:F4},{m.MedianReadAllocatedBytes},{m.Read.Visited},{m.Read.OrderedClosed?.ToString() ?? "unresolved"}"));
            }
    }
}
