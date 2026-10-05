namespace LocalServiceGrowth;

// This supplied fixture is outside the interpreting network. Its phase,
// seed and future samples are never used by a native discovery or support read.
internal sealed class EnvironmentSource
{
    public long Seed { get; set; } = 11;
    public long Position { get; set; }
    public long Level { get; set; } = 100;
    public string Scenario { get; set; } = "changing-demand";

    public (long? Input, bool FineSecond, string Phase) Next()
    {
        var local = Position % 360;
        var offset = local % 120;
        var random = Mix(unchecked((ulong)Seed) ^ unchecked((ulong)Position));
        long delta;
        if (Scenario == "changing-demand")
            delta = offset is >= 72 and < 84 ? 0 :
                (Position / 18 % 2 == 0 ? 1 : -1) * (2 + (long)(random % 3));
        else if (Scenario == "unpredictable")
            delta = (random % 2 == 0 ? 1 : -1) * (2 + (long)((random >> 8) % 5));
        else throw new ArgumentException("Unknown supplied scenario: " + Scenario);
        Level = checked(Level + delta);
        Position++;
        var fine = local is >= 120 and < 240;
        return (local == 179 ? null : Level, fine,
            local < 120 ? "coarse" : local < 240 ? "fine-second" : "coarse-return");
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
