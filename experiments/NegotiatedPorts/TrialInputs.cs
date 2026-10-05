namespace NegotiatedPorts;

/// <summary>Supplied environment and requests. Phase labels and future samples
/// are observer data, never an input to the native market structure.</summary>
public static class TrialInputs
{
    public static IEnumerable<(string Name, int[] InitialCursors,
        (string Phase, long? Value, bool Demand0, bool Demand1, long Funding0, long Funding1)[] Samples)> All()
    {
        yield return ("changing-input", [0, 0], Samples(false, false));
        yield return ("demand-withdrawn", [0, 0], Samples(true, false));
        yield return ("scarce-funding", [0, 0], Samples(false, true));
        yield return ("different-encounters", [1, 2], Samples(false, false));
    }

    private static (string Phase, long? Value, bool Demand0, bool Demand1, long Funding0, long Funding1)[] Samples(
        bool withdrawFine, bool scarce)
    {
        var result = new List<(string, long?, bool, bool, long, long)>();
        long level = 100;
        result.Add(("Warmup", level, true, true, 5, 8));
        Add("Steady rise", Enumerable.Repeat(3L, 20), false);
        Add("Changing rise", Enumerable.Range(0, 24).Select(index => 2L + index % 6), true);
        result.Add(("Missing observation", null, true, !withdrawFine, scarce ? 1 : 5, scarce ? 2 : 8));
        Add("Steady fall", Enumerable.Repeat(-5L, 24), true);
        Add("Flat", Enumerable.Repeat(0L, 16), false);
        Add("Returning rise", Enumerable.Repeat(4L, 24), false);
        return result.ToArray();

        void Add(string phase, IEnumerable<long> increments, bool changedBlock)
        {
            foreach (var increment in increments)
            {
                level += increment;
                result.Add((phase, level, true, !(withdrawFine && changedBlock),
                    scarce && changedBlock ? 1 : 5, scarce && changedBlock ? 2 : 8));
            }
        }
    }
}
