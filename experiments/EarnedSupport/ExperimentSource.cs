using MarketTraining;

namespace EarnedSupport;

// External fixtures only. Phase names, future samples, and the seed are never
// passed into the native market or its support equations.
internal sealed class ExperimentSource
{
    public string Scenario { get; set; } = "periodic";
    public EnvironmentSource Source { get; set; } = new();

    public (long? Input, string Phase) Next()
    {
        if (Scenario == "periodic") return Source.Next();
        var position = Source.Position;
        var random = Mix(unchecked((ulong)Source.Seed) ^ unchecked((ulong)position));
        long delta;
        string phase;
        if (Scenario == "flat-return")
        {
            delta = position is >= 240 and < 1200 ? 0 : 3 + (long)(random % 3) - 1;
            phase = position < 240 ? "rise" : position < 1200 ? "long-flat" : "return-rise";
        }
        else if (Scenario == "random-sign")
        {
            delta = random % 2 == 0 ? -3 : 3;
            phase = "unpredictable-sign";
        }
        else throw new ArgumentException("Unknown external scenario: " + Scenario);
        Source.Level = checked(Source.Level + delta);
        Source.Position = checked(position + 1);
        return (Source.Level, phase);
    }

    private static ulong Mix(ulong value)
    {
        unchecked
        {
            value += 0x9E3779B97F4A7C15UL;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}
