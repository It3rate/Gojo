namespace Gojo.EncodingC;

/// <summary>Runs wirings, one step at a time, in their declared order. A
/// wiring that touches a position another writes runs in a later wave and
/// reads what the earlier one left; the rest could run at the same moment on a
/// parallel substrate and give the same result. Running them one after
/// another in declared order is therefore exactly the waved run. The order is
/// structure: it is the schedule, and two takers of one unit cannot both have
/// it.</summary>
public sealed class Engine
{
    private readonly List<Wiring> wirings = [];

    public IReadOnlyList<Wiring> Wirings => wirings.AsReadOnly();

    /// <summary>Steps run so far.</summary>
    public long Tick { get; private set; }

    public Engine Add(Wiring wiring)
    {
        ArgumentNullException.ThrowIfNull(wiring);
        if (wiring.Law is not TransitionLaw) throw new ArgumentException("The engine runs transition laws; relations are read.", nameof(wiring));
        wirings.Add(wiring);
        return this;
    }

    /// <summary>Wirings grouped into waves, by index.</summary>
    public IReadOnlyList<int[]> Waves()
    {
        var lastWrite = new Dictionary<(Surface, int), int>();
        var lastTouch = new Dictionary<(Surface, int), int>();
        var waves = new List<List<int>>();
        for (var i = 0; i < wirings.Count; i++)
        {
            var rows = wirings[i].Runnable().Indexes().ToArray();
            var touched = rows.SelectMany(wirings[i].TouchedKeys).Distinct().ToArray();
            var written = rows.SelectMany(wirings[i].WrittenKeys).Distinct().ToArray();
            var level = 0;
            foreach (var site in touched)
                if (lastWrite.TryGetValue(site, out var w)) level = Math.Max(level, w + 1);
            foreach (var site in written)
                if (lastTouch.TryGetValue(site, out var t)) level = Math.Max(level, t + 1);
            foreach (var site in touched) lastTouch[site] = Math.Max(lastTouch.GetValueOrDefault(site, -1), level);
            foreach (var site in written) lastWrite[site] = Math.Max(lastWrite.GetValueOrDefault(site, -1), level);
            while (waves.Count <= level) waves.Add([]);
            waves[level].Add(i);
        }
        return waves.Select(wave => wave.ToArray()).ToArray();
    }

    public EngineStep Step()
    {
        var waves = Waves();
        var steps = wirings.Select(wiring => wiring.Apply()).ToArray();
        Tick++;
        return new EngineStep(Tick, steps, waves);
    }

    public IReadOnlyList<EngineStep> Run(int steps) => Enumerable.Range(0, steps).Select(_ => Step()).ToArray();
}

/// <summary>One engine step: what each wiring did, and which wirings could have
/// run together.</summary>
public sealed record EngineStep(long Tick, IReadOnlyList<WiringStep> Steps, IReadOnlyList<int[]> Waves);
