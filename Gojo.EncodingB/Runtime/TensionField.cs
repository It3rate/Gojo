namespace Gojo.EncodingB;

/// <summary>Tension on a tile, as three thermometer fields per site: magnitude M
/// (absorbed, visible), positive tension P and negative tension N. P and N are
/// never netted against each other directly: a site holding +3 and −3 is not a
/// site holding nothing. External change enters only as tension (Axiom 5);
/// absorption moves it into M within capacity, and unmet draw stays as N.</summary>
public sealed class TensionField
{
    private readonly string[] magnitude, positive, negative;
    private readonly int[] overflow;

    public TensionField(Tile tile, string magnitudeField = "M", string positiveField = "P", string negativeField = "N")
    {
        ArgumentNullException.ThrowIfNull(tile);
        Tile = tile;
        magnitude = tile.Grammar.PlanesOf(magnitudeField).ToArray();
        positive = tile.Grammar.PlanesOf(positiveField).ToArray();
        negative = tile.Grammar.PlanesOf(negativeField).ToArray();
        if (magnitude.Length != positive.Length || magnitude.Length != negative.Length)
            throw new ArgumentException("M, P and N need the same capacity.");
        overflow = new int[tile.SiteCount];
        PositiveField = positiveField;
    }

    public Tile Tile { get; }
    public int Capacity => magnitude.Length;

    /// <summary>The positive tension field: pending units that may leave through a material port.</summary>
    public string PositiveField { get; }

    /// <summary>A grammar with M, P and N thermometers of the given capacity.</summary>
    public static SiteGrammar Grammar(int capacity, Action<SiteGrammar.Builder>? extra = null)
    {
        var builder = SiteGrammar.Create($"tension-{capacity}")
            .Thermometer("M", "value", capacity, "absorbed magnitude", material: true)
            .Thermometer("P", "tension", capacity, "positive tension", material: true)
            .Thermometer("N", "tension", capacity, "negative tension (unmet draw)", material: true);
        extra?.Invoke(builder);
        return builder.Build();
    }

    public int Magnitude(int site) => Count(magnitude, site);
    public int Positive(int site) => Count(positive, site);
    public int Negative(int site) => Count(negative, site);

    /// <summary>Units that arrived when a site's tension planes were already full.
    /// Kept as residue, never destroyed.</summary>
    public int Overflow(int site) => overflow[site];

    public long TotalTension => Enumerable.Range(0, Tile.SiteCount).Sum(s => (long)Positive(s) + Negative(s) + overflow[s]);

    /// <summary>Introduces signed change at a site as tension, one unit at a time.</summary>
    public IngestRecord Ingest(int site, int delta)
    {
        Tile.CheckSite(site);
        var planes = Load(delta >= 0 ? positive : negative);
        var accepted = 0;
        for (var unit = 0; unit < Math.Abs(delta); unit++)
            if (Thermometer.AddOneAt(planes, Bits.Of(site)) == 0) accepted++;
        Store(delta >= 0 ? positive : negative, planes);
        var spilled = Math.Abs(delta) - accepted;
        overflow[site] += spilled;
        return new IngestRecord(site, delta, accepted, spilled);
    }

    /// <summary>Self-local absorption (C2), bit-sliced over all given sites at once:
    /// first negative tension draws magnitude down, then positive tension fills it
    /// up to capacity. The declared order means opposed packets meet through M,
    /// not by silent netting. Returns units drawn and filled.</summary>
    public (int Drawn, int Filled) Absorb(ulong sites)
    {
        var m = Load(magnitude);
        var p = Load(positive);
        var n = Load(negative);
        int drawn = 0, filled = 0;
        while ((sites & n[0] & m[0]) is var draw and not 0)
        {
            Thermometer.RemoveOneAt(n, draw);
            Thermometer.RemoveOneAt(m, draw);
            drawn += Bits.Count(draw);
        }
        while ((sites & p[0] & ~m[^1]) is var fill and not 0)
        {
            Thermometer.RemoveOneAt(p, fill);
            Thermometer.AddOneAt(m, fill);
            filled += Bits.Count(fill);
        }
        Store(magnitude, m);
        Store(positive, p);
        Store(negative, n);
        return (drawn, filled);
    }

    /// <summary>Publishes a material port over these sites. Pending positive tension
    /// may leave through it, one unit per lane per transfer. A reader sees only
    /// whether a unit is pending, through a rung map, never how many.</summary>
    public Port PublishMaterial(string name, int[] sites, string unit, PortAccounting accounting = PortAccounting.Material)
    {
        if (accounting == PortAccounting.Observation)
            throw new ArgumentException("Publish an observation port with Tile.Publish.", nameof(accounting));
        var units = new Domain("pending units", Enumerable.Range(0, Capacity + 1).Select(k => k.ToString()).ToArray());
        var rung = new RungMap($"a unit of {unit} is pending", units, Passage.Presence, pending => pending == 0 ? 0 : 1);
        return Tile.Publish(name, PositiveField, sites, new PortBasis(Passage.Presence, unit, rung), accounting, this);
    }

    /// <summary>Exposes these sites as a material inlet: an arriving unit of
    /// <paramref name="unit"/> enters as one unit of positive tension.</summary>
    public Inlet MaterialInlet(string name, int[] sites, string unit) =>
        Tile.AddInlet(name, PositiveField, sites, new PortBasis(Passage.Presence, unit), this);

    /// <summary>Removes one positive tension unit at every given site at once.
    /// Returns sites that had none.</summary>
    internal ulong Release(ulong sites)
    {
        var p = Load(positive);
        var underflow = Thermometer.RemoveOneAt(p, sites);
        Store(positive, p);
        return underflow;
    }

    /// <summary>Adds one positive tension unit at every given site at once.
    /// Returns sites whose tension planes were already full.</summary>
    internal ulong Receive(ulong sites)
    {
        var p = Load(positive);
        var full = Thermometer.AddOneAt(p, sites);
        Store(positive, p);
        return full;
    }

    /// <summary>Moves one positive tension unit from one site to another.</summary>
    internal bool MoveUnit(int from, int to)
    {
        var p = Load(positive);
        if (Thermometer.RemoveOneAt(p, Bits.Of(from)) != 0) return false;
        if (Thermometer.AddOneAt(p, Bits.Of(to)) != 0)
        {
            Thermometer.AddOneAt(p, Bits.Of(from));
            return false;
        }
        Store(positive, p);
        return true;
    }

    /// <summary>Free room at a site: capacity minus absorbed magnitude and pending tension.</summary>
    public int Room(int site) => Math.Max(0, Capacity - Magnitude(site) - Positive(site));

    internal int[] OverflowSnapshot() => overflow.ToArray();

    internal void RestoreOverflow(int[] values) => values.CopyTo(overflow, 0);

    private ulong[] Load(string[] planes) => planes.Select(Tile.Read).ToArray();

    private void Store(string[] planes, ulong[] values)
    {
        for (var k = 0; k < planes.Length; k++) Tile.WriteState(planes[k], values[k], Tile.Allocated);
    }

    private int Count(string[] planes, int site) =>
        Thermometer.Decode(Tile.Gather(Tile, planes, site)) ?? throw new InvalidOperationException($"Broken thermometer at site {site}.");
}

public sealed record IngestRecord(int Site, int Delta, int Accepted, int Overflow);
