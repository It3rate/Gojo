namespace MarketTraining;

/// <summary>External fixture state, never part of the interpreter. A fixed
/// integer hash makes seed/position reproducible without a runtime Random ABI.</summary>
internal sealed class EnvironmentSource
{
    public const string Version = "six-block-jitter-v1";
    public long Seed { get; set; } = 1;
    public long Position { get; set; }
    public long Level { get; set; } = 100;

    public (long? Input, string Phase) Next()
    {
        var within = Position % 240;
        var block = (int)(within / 40);
        var jitter = (long)(Mix(unchecked((ulong)Seed) ^ unchecked((ulong)Position)) % 3) - 1;
        var delta = block switch { 0 or 1 or 5 => 3 + jitter, 2 => -5 + jitter, 3 => 0, _ => 6 + jitter };
        Level = checked(Level + delta);
        Position = checked(Position + 1);
        return (within == 119 ? null : Level, block switch
        {
            0 => "rise-jitter", 1 => "rise-new-permutations", 2 => "fall-jitter",
            3 => "flat", 4 => "stronger-rise", _ => "return-rise"
        });
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
