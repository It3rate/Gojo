namespace ChangeSubscriptions;

// Fixture-only external observations. Names, seeds, phase boundaries and future
// values never enter the native actor graph.
internal sealed record InputEvent(string Phase, long? Level);
internal static class Fixtures
{
    public static IEnumerable<InputEvent> Trace(string name)
    {
        long level = 10000;
        yield return new("initial", level);
        IEnumerable<InputEvent> Steps(string phase, int count, params long[] increments)
        {
            for (var i = 0; i < count; i++)
            { level += increments[i % increments.Length]; yield return new(phase, level); }
        }
        if (name == "steady-reversal")
        {
            foreach (var e in Steps("increase", 96, 1)) yield return e;
            foreach (var e in Steps("decrease", 96, -1)) yield return e;
            foreach (var e in Steps("flat", 64, 0)) yield return e;
            foreach (var e in Steps("increase-return", 96, 1)) yield return e;
        }
        else if (name == "grain-gap")
        {
            foreach (var e in Steps("fine-variation", 96, 3, 4, 3, 5)) yield return e;
            foreach (var e in Steps("coarse-crossing", 48, 6, 7)) yield return e;
            for (var i = 0; i < 6; i++) yield return new("unavailable", null);
            yield return new("resume", level);
            foreach (var e in Steps("variation-return", 64, 3, 4)) yield return e;
        }
        else if (name == "applicability-return")
        {
            foreach (var e in Steps("small-increase", 96, 1)) yield return e;
            foreach (var e in Steps("large-increase", 160, 4)) yield return e;
            foreach (var e in Steps("small-decrease", 96, -1)) yield return e;
            foreach (var e in Steps("large-decrease", 160, -4)) yield return e;
        }
        else if (name == "wake-chatter")
        {
            foreach (var e in Steps("small-increase", 96, 1)) yield return e;
            foreach (var e in Steps("boundary-chatter", 64, 1, 2)) yield return e;
            foreach (var e in Steps("isolated-changes", 64, 4, 1)) yield return e;
            foreach (var e in Steps("paired-reversals", 96, 4, 4, -4, -4)) yield return e;
            foreach (var e in Steps("useful-return", 128, 4)) yield return e;
        }
        else if (name is "unpredictable-113" or "unpredictable-271")
        {
            uint state = name.EndsWith("113", StringComparison.Ordinal) ? 113U : 271U;
            for (var i = 0; i < 256; i++)
            {
                state ^= state << 13; state ^= state >> 17; state ^= state << 5;
                var magnitude = (state & 2) == 0 ? 1 : 4;
                level += (state & 1) == 0 ? magnitude : -magnitude;
                yield return new("unpredictable", level);
            }
        }
        else throw new ArgumentException("Unknown fixture.", nameof(name));
    }
}
