namespace ConnectionAssembly;

// Environment and report labels only. No label, phase or generator is passed
// into AssemblyNetwork; it receives only sequence and observed integer input.
internal static class TrialInputs
{
    private static readonly long[] Pulse =
        [0, 0, 0, 0, 0, 0, 1, 3, 12, 0, -4, 0, 0, 0, 1, 6, 0, 0, 0, 0, 0, 0, 0, 0];

    public static IEnumerable<(string Name, (string Phase, long? Value)[] Samples)> All()
    {
        yield return ("changing-patterns", Sequence(
            ("Zigzag", Zigzag(2)), ("Square", Square()),
            ("Heartbeat", Heartbeat()), ("Zigzag returns", Zigzag(2))));
        yield return ("reordered-and-flat", Sequence(
            ("Square first", Square()), ("Mirrored zigzag", Zigzag(-2)),
            ("Heartbeat", Heartbeat()), ("Flat", Enumerable.Repeat(0L, 240).ToArray())));
        var shuffled = Heartbeat();
        yield return ("ordered-pulse-control", Sequence(("Ordered pulse increments", Heartbeat())));
        var random = new Random(603);
        for (var i = shuffled.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }
        yield return ("shuffled-pulse-control", Sequence(("Shuffled pulse increments", shuffled)));
        yield return ("large-zigzag-control", Sequence(("Larger zigzag: step 5", Zigzag(5))));
    }

    private static long[] Zigzag(int step) => Enumerable.Range(0, 240)
        .Select(i => (long)(i % 16 < 8 ? step : -step)).ToArray();

    private static long[] Square() => Enumerable.Range(0, 240)
        .Select(i => i > 0 && i % 12 == 0 ? (i / 12 % 2 == 1 ? 12L : -12L) : 0L).ToArray();

    private static long[] Heartbeat() => Enumerable.Range(0, 240)
        .Select(i => Pulse[i % Pulse.Length] - Pulse[(i + Pulse.Length - 1) % Pulse.Length]).ToArray();

    private static (string Phase, long? Value)[] Sequence(params (string Phase, long[] Deltas)[] blocks)
    {
        var samples = new List<(string, long?)>();
        long value = 0;
        foreach (var (phase, deltas) in blocks)
            foreach (var delta in deltas)
            {
                value = checked(value + delta);
                samples.Add((phase, value));
            }
        return samples.ToArray();
    }
}
