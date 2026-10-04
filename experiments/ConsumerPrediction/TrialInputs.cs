namespace ConsumerPrediction;

/// <summary>External stimulus and consumer requests only. Phase names and
/// future values never enter the native predictor. Arithmetic here generates
/// the environment; prediction and usefulness calculations belong to Gojo.</summary>
public static class TrialInputs
{
    public static IEnumerable<(string Name, long Tolerance,
        (string Phase, long? Value, bool Demand)[] Samples)> All()
    {
        var increments = Enumerable.Range(0, 16)
            .SelectMany(_ => new long[] { 2, 2, 2, 2, -2, -2, -2, -2 }).ToArray();
        yield return ("ordinary-zigzag", 0, Integrate(increments, "Ordinary zigzag"));

        var shuffled = increments.ToArray();
        var random = new Random(42016);
        for (var index = shuffled.Length - 1; index > 0; index--)
        {
            var other = random.Next(index + 1);
            (shuffled[index], shuffled[other]) = (shuffled[other], shuffled[index]);
        }
        yield return ("shuffled-increments", 0, Integrate(shuffled, "Shuffled increments"));
        yield return ("mixed-episodes", 0, Episodes(quiet: 0, withdrawDemand: false));
        yield return ("mixed-episodes-long-quiet", 0, Episodes(quiet: 24, withdrawDemand: false));
        yield return ("mixed-episodes-tolerance-four", 4, Episodes(quiet: 0, withdrawDemand: false));
        yield return ("mixed-episodes-demand-withdrawn", 0, Episodes(quiet: 0, withdrawDemand: true));
    }

    private static (string Phase, long? Value, bool Demand)[] Integrate(long[] increments, string phase)
    {
        var samples = new List<(string, long?, bool)> { (phase, 0, true) };
        long level = 0;
        foreach (var increment in increments)
        {
            level += increment;
            samples.Add((phase, level, true));
        }
        return samples.ToArray();
    }

    private static (string Phase, long? Value, bool Demand)[] Episodes(int quiet, bool withdrawDemand)
    {
        var samples = new List<(string, long?, bool)>();
        for (var block = 0; block < 3; block++)
        {
            var phase = block switch
            {
                0 => "Transitional turn",
                1 => "New slope persists",
                _ => "Transitional turn returns"
            };
            long[] levels = block == 1 ? [0, 2, 4, -2, -8, -14] : [0, 2, 4, -2, -4, -6];
            var demand = !withdrawDemand || block != 1;
            for (var episode = 0; episode < 8; episode++)
            {
                var mirror = episode % 2 == 0 ? 1 : -1;
                foreach (var level in levels) samples.Add((phase, mirror * level, demand));
                // Every forecast arising at the turn already has its next
                // observed outcome before this padding starts. These zero
                // changes are real observations, not paused time or missing data.
                for (var index = 0; index < quiet; index++)
                    samples.Add((phase, mirror * levels[^1], demand));
                // Explicit external segmentation, not a learned boundary.
                samples.Add((phase, null, demand));
            }
        }
        return samples.ToArray();
    }
}
