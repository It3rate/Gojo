namespace FailureSubscriptions;

// Host-only fixture. Its labels, seed and future increments never enter Gojo.
internal sealed class EnvironmentSource
{
    public long Seed { get; set; } = 11;
    public long Position { get; set; }
    public long Level { get; set; } = 100;
    public long Difference { get; set; }
    public string Scenario { get; set; } = "mixed";

    public (long? Input, string Phase) Next()
    {
        var t = Position % 241;
        var hash = Mix(unchecked((ulong)Seed ^ ((ulong)Position * 0x9E3779B97F4A7C15UL)));
        string phase;
        if (Scenario == "unpredictable")
        {
            Difference = ((hash & 1) == 0 ? 1 : -1) * (1 + (long)((hash >> 1) % 4));
            phase = "unpredictable";
        }
        else if (Scenario == "wandering")
        {
            Difference = Math.Clamp(Difference + ((hash & 1) == 0 ? 1 : -1), -6, 6);
            phase = "wandering";
        }
        else if (t < 36) { Difference = 2 + (hash % 7 == 0 ? 1 : 0); phase = "increasing"; }
        else if (t < 77 || t is >= 143 and < 184)
        {
            Difference = (t % 2 == 0 ? 1 : -1) * (2 + (hash % 7 == 0 ? 1 : 0));
            phase = t < 77 ? "alternating" : "alternating-return";
        }
        else if (t < 113)
        {
            var offset = (t - 77) % 12;
            Difference = offset < 6 ? offset - 3 : 9 - offset;
            phase = "changing-slope";
        }
        else if (t < 143) { Difference = 0; phase = "flat"; }
        else { Difference = ((t - 184) / 13 % 2 == 0 ? 1 : -1) * (2 + (hash % 7 == 0 ? 1 : 0)); phase = "zigzag"; }
        Level += Difference;
        Position++;
        return (t is 89 or 178 ? null : Level, phase);
    }

    private static ulong Mix(ulong value)
    {
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }
}
