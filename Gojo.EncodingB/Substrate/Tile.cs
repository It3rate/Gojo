namespace Gojo.EncodingB;

/// <summary>Something that holds planes under a site grammar: a source tile, or
/// a workspace's view of one. Laws read and write through this surface.</summary>
public interface IPlaneStore
{
    SiteGrammar Grammar { get; }

    /// <summary>The whole plane. A basis-supplied plane reads as all allocated
    /// sites set or none.</summary>
    ulong Read(string plane);

    /// <summary>Replaces the plane's bits under <paramref name="mask"/>.</summary>
    void Write(string plane, ulong bits, ulong mask);
}

/// <summary>A fixed-capacity block of up to 64 sites with one owner. Its planes
/// are bit-sliced: plane p is one 64-bit word and bit i belongs to site i.
/// A tile holds what <em>is</em>: structure, value at its resolution and
/// tension. Selection, supplied evidence, support and prediction live in
/// workspaces, never here.</summary>
public sealed class Tile : IPlaneStore
{
    private readonly ulong[] stored;
    private readonly bool[] basis;
    private readonly string[] siteNames;
    private readonly Dictionary<string, RowSet> rowSets = new(StringComparer.Ordinal);

    public Tile(string name, SiteGrammar grammar, int siteCount, IReadOnlyList<string>? siteNames = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(grammar);
        if (siteCount is < 1 or > Bits.Width) throw new ArgumentOutOfRangeException(nameof(siteCount));
        if (siteNames is not null && siteNames.Count != siteCount)
            throw new ArgumentException("Supply one name per site.", nameof(siteNames));
        Name = name;
        Grammar = grammar;
        SiteCount = siteCount;
        this.siteNames = siteNames?.ToArray() ?? Enumerable.Range(0, siteCount).Select(i => i.ToString()).ToArray();
        stored = new ulong[grammar.Planes.Count];
        basis = new bool[grammar.Planes.Count];
    }

    public string Name { get; }
    public SiteGrammar Grammar { get; }
    public int SiteCount { get; }

    /// <summary>The allocated sites. Bits above them in every plane are declared padding.</summary>
    public ulong Allocated => Bits.Low(SiteCount);

    public IReadOnlyList<string> SiteNames => siteNames;

    /// <summary>Increments on every structural or basis edit: rows, basis bits.</summary>
    public long Epoch { get; private set; }

    /// <summary>Increments on every state change, structural or not.</summary>
    public long Version { get; private set; }

    public IEnumerable<RowSet> RowSets => rowSets.Values;

    public int SiteOf(string siteName)
    {
        var index = Array.IndexOf(siteNames, siteName);
        return index >= 0 ? index : throw new ArgumentException($"Tile {Name} has no site '{siteName}'.", nameof(siteName));
    }

    public ulong Read(string plane)
    {
        var index = Grammar.IndexOf(plane);
        return Grammar.Planes[index].BasisSupplied ? (basis[index] ? Allocated : 0) : stored[index];
    }

    public bool Bit(string plane, int site)
    {
        CheckSite(site);
        return Bits.Get(Read(plane), site);
    }

    /// <summary>The basis bit of a basis-supplied plane.</summary>
    public bool Basis(string plane)
    {
        var index = Grammar.IndexOf(plane);
        if (!Grammar.Planes[index].BasisSupplied) throw new ArgumentException($"Plane '{plane}' is stored, not basis-supplied.");
        return basis[index];
    }

    /// <summary>The binary code a <see cref="CodeField"/> (or single plane) holds at a site.</summary>
    public int Code(string planeOrField, int site) => ReadCode(this, planeOrField, site);

    /// <summary>A thermometer field's magnitude, or null when the bits are not a
    /// prefix (a broken code is reported, never silently repaired).</summary>
    public int? Magnitude(string thermometerField, int site)
    {
        CheckSite(site);
        return Thermometer.Decode(Gather(this, Grammar.PlanesOf(thermometerField), site));
    }

    void IPlaneStore.Write(string plane, ulong bits, ulong mask) => WriteState(plane, bits, mask);

    /// <summary>Construction-time supply of any stored plane. After construction,
    /// material planes should change only through laws and tension.</summary>
    public void Seed(string plane, ulong bits) => WriteState(plane, bits, ulong.MaxValue);

    public void Seed(string plane, string sites) => Seed(plane, Bits.Parse(sites));

    /// <summary>Construction-time supply of a code at one site.</summary>
    public void SeedCode(string planeOrField, int site, int code)
    {
        CheckSite(site);
        var planes = Grammar.PlanesOf(planeOrField);
        if (code < 0 || code >= 1 << planes.Count) throw new ArgumentOutOfRangeException(nameof(code));
        for (var bit = 0; bit < planes.Count; bit++)
        {
            var set = ((code >> bit) & 1) != 0;
            if (Grammar.Plane(planes[bit]).BasisSupplied)
            {
                if (set != Basis(planes[bit]))
                    throw new ArgumentException($"Plane '{planes[bit]}' is supplied by the tile basis; this code needs the other basis.", nameof(code));
                continue;
            }
            WriteState(planes[bit], set ? Bits.Of(site) : 0, Bits.Of(site));
        }
    }

    /// <summary>Construction-time supply of a thermometer magnitude at one site.</summary>
    public void SeedMagnitude(string thermometerField, int site, int magnitude)
    {
        CheckSite(site);
        var planes = Grammar.PlanesOf(thermometerField);
        var code = Thermometer.Encode(magnitude, planes.Count);
        for (var k = 0; k < planes.Count; k++)
            WriteState(planes[k], Bits.Get(code, k) ? Bits.Of(site) : 0, Bits.Of(site));
    }

    /// <summary>Supplies an observation to a non-material plane, such as a gate
    /// reading. Material planes refuse: supplying evidence is not moving material.</summary>
    public void Supply(string plane, int site, bool value)
    {
        CheckSite(site);
        if (Grammar.Plane(plane).Material)
            throw new InvalidOperationException($"Plane '{plane}' is material; it changes only through laws or tension.");
        WriteState(plane, value ? Bits.Of(site) : 0, Bits.Of(site));
    }

    /// <summary>Flips a basis-supplied plane for the whole tile. A basis edit, so
    /// the epoch advances.</summary>
    public void FlipBasis(string plane)
    {
        var index = Grammar.IndexOf(plane);
        if (!Grammar.Planes[index].BasisSupplied) throw new ArgumentException($"Plane '{plane}' is stored, not basis-supplied.");
        basis[index] = !basis[index];
        Epoch++;
        Version++;
    }

    public void SetBasis(string plane, bool value)
    {
        if (Basis(plane) != value) FlipBasis(plane);
    }

    public RowSet AddRowSet(string name, params string[] roles)
    {
        var set = new RowSet(this, name, roles);
        if (!rowSets.TryAdd(name, set)) throw new ArgumentException($"Row set '{name}' already exists.", nameof(name));
        StructuralEdit();
        return set;
    }

    public RowSet Rows(string name) =>
        rowSets.TryGetValue(name, out var set) ? set : throw new ArgumentException($"Tile {Name} has no row set '{name}'.", nameof(name));

    public bool HasRows(string name) => rowSets.ContainsKey(name);

    /// <summary>Problems a decoder must report: nonzero padding and broken
    /// thermometer prefixes. An empty list means every bit decodes cleanly.</summary>
    public IReadOnlyList<string> Validate()
    {
        var issues = new List<string>();
        for (var p = 0; p < stored.Length; p++)
            if (!Grammar.Planes[p].BasisSupplied && (stored[p] & ~Allocated) != 0)
                issues.Add($"plane {Grammar.Planes[p].Name}: padding bits set above site {SiteCount - 1}");
        foreach (var field in Grammar.Fields.OfType<ThermometerField>())
            for (var site = 0; site < SiteCount; site++)
                if (Magnitude(field.Name, site) is null)
                    issues.Add($"field {field.Name} at site {siteNames[site]}: bits are not a thermometer prefix");
        return issues;
    }

    public TileSnapshot Snapshot() => new(
        stored.ToArray(),
        basis.ToArray(),
        Epoch,
        Version,
        rowSets.Values.Select(set => set.Snapshot()).ToArray());

    /// <summary>Restores a snapshot taken from a tile with the same grammar and size.</summary>
    public void Restore(TileSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Stored.Length != stored.Length) throw new ArgumentException("Snapshot grammar does not match.");
        snapshot.Stored.CopyTo(stored, 0);
        snapshot.Basis.CopyTo(basis, 0);
        Epoch = snapshot.Epoch;
        Version = snapshot.Version;
        rowSets.Clear();
        foreach (var rows in snapshot.RowSets)
        {
            var set = new RowSet(this, rows.Name, rows.Roles.ToArray());
            set.RestoreRows(rows);
            rowSets.Add(rows.Name, set);
        }
    }

    /// <summary>Same planes, basis and rows. Epoch and version are history, not state.</summary>
    public bool SameState(Tile other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var a = Snapshot();
        var b = other.Snapshot();
        return a.Stored.SequenceEqual(b.Stored) && a.Basis.SequenceEqual(b.Basis) &&
            a.RowSets.Length == b.RowSets.Length &&
            a.RowSets.Zip(b.RowSets).All(pair => pair.First.SameRows(pair.Second));
    }

    internal void WriteState(string plane, ulong bits, ulong mask)
    {
        var index = Grammar.IndexOf(plane);
        if (Grammar.Planes[index].BasisSupplied)
            throw new InvalidOperationException($"Plane '{plane}' is supplied by the tile basis; flip the basis instead.");
        mask &= Allocated;
        var next = (stored[index] & ~mask) | (bits & mask);
        if (next == stored[index]) return;
        stored[index] = next;
        Version++;
    }

    internal void StructuralEdit()
    {
        Epoch++;
        Version++;
    }

    internal void CheckSite(int site)
    {
        if (site < 0 || site >= SiteCount) throw new ArgumentOutOfRangeException(nameof(site), $"Tile {Name} has {SiteCount} sites.");
    }

    internal static int ReadCode(IPlaneStore store, string planeOrField, int site)
    {
        var planes = store.Grammar.PlanesOf(planeOrField);
        var code = 0;
        for (var bit = 0; bit < planes.Count; bit++)
            if (Bits.Get(store.Read(planes[bit]), site)) code |= 1 << bit;
        return code;
    }

    internal static ulong Gather(IPlaneStore store, IReadOnlyList<string> planes, int site)
    {
        ulong word = 0;
        for (var k = 0; k < planes.Count; k++)
            if (Bits.Get(store.Read(planes[k]), site)) word |= 1UL << k;
        return word;
    }

    public override string ToString() => $"tile {Name} ({Grammar.Name}, {SiteCount} sites, epoch {Epoch})";
}

public sealed record TileSnapshot(ulong[] Stored, bool[] Basis, long Epoch, long Version, RowSetSnapshot[] RowSets);
