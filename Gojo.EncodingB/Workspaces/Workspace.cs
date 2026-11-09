namespace Gojo.EncodingB;

/// <summary>The two-bit provenance code of a workspace site (plane K0 low, K1 high).</summary>
public enum SiteProvenance
{
    /// <summary>00: reads the source as it is now.</summary>
    ReferencedLive = 0,

    /// <summary>01: a sample taken at the recorded source version.</summary>
    ReferencedCaptured = 1,

    /// <summary>10: created here, with no source counterpart. Never evidence about the source.</summary>
    Introduced = 2,

    /// <summary>11: was referenced, then changed here (copy on write); correspondence kept.</summary>
    Diverged = 3,
}

/// <summary>A row a workspace proposed, such as an edge closing an open stroke.</summary>
public sealed record IntroducedRow(string RowSet, int[] Sites, string Rule, string Workspace);

/// <summary>A workspace holds what is <em>noticed, supposed or attempted</em> about
/// a source tile: a selection, supplied evidence, supports, local introductions
/// and changes. It shares the source's index space: bit i is source site i, and
/// introduced sites take indexes above the source's sites. Nothing here changes
/// the source except an explicit, validated commit (see <see cref="TrialWorkspace"/>).</summary>
public abstract class Workspace : IPlaneStore
{
    private readonly ulong[] local;
    private readonly Dictionary<string, SupportPlanes> supports = new(StringComparer.Ordinal);
    private readonly List<Selection> retained = [];
    private readonly List<IntroducedRow> introducedRows = [];
    private ulong k0, k1, introduced, supplied;

    protected Workspace(string name, Tile source, Selection focus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(focus);
        if (!ReferenceEquals(focus.Scope.Tile, source) || focus.Scope.OverRows)
            throw new ArgumentException("The focus must select sites of the source tile.", nameof(focus));
        Name = name;
        Source = source;
        Focus = focus;
        local = new ulong[source.Grammar.Planes.Count];
        ReadEpoch = source.Epoch;
        ReadVersion = source.Version;
    }

    public string Name { get; }
    public Tile Source { get; }
    public SiteGrammar Grammar => Source.Grammar;

    /// <summary>The selection over source sites this workspace works on.</summary>
    public Selection Focus { get; }

    /// <summary>The source epoch and version this workspace's reads were taken at.</summary>
    public long ReadEpoch { get; protected set; }
    public long ReadVersion { get; protected set; }

    /// <summary>Selection plane: focused source sites plus introduced sites.</summary>
    public ulong Sel => Focus.Members | introduced;

    /// <summary>Supplied plane: evidence was supplied for this site.</summary>
    public ulong Sup => supplied;

    /// <summary>Provenance planes. Read together as <see cref="SiteProvenance"/>.</summary>
    public ulong K0 => k0;
    public ulong K1 => k1;

    public ulong Introduced => introduced;
    public IReadOnlyList<Selection> Selections => retained;
    public IReadOnlyList<IntroducedRow> IntroducedRows => introducedRows;

    public SiteProvenance ProvenanceOf(int site)
    {
        if (!Bits.Get(Sel, site)) throw new ArgumentOutOfRangeException(nameof(site), $"Site {site} is not selected: no claim.");
        return (SiteProvenance)((Bits.Get(k0, site) ? 1 : 0) | (Bits.Get(k1, site) ? 2 : 0));
    }

    /// <summary>A bit at a site, or null when the site is not selected: not
    /// selected makes no claim, and it never reads as zero.</summary>
    public bool? ReadBit(string plane, int site) => Bits.Get(Sel, site) ? Bits.Get(Read(plane), site) : null;

    /// <summary>The whole plane over selected sites. Unselected bits are 0 here
    /// only because a plane word must hold something; laws refuse to read them.</summary>
    public ulong Read(string plane)
    {
        var index = Grammar.IndexOf(plane);
        var live = Sel & ~(k0 | k1);
        if (Grammar.Planes[index].BasisSupplied) return Source.Read(plane) & Sel;
        return (Source.Read(plane) & live) | (local[index] & Sel & ~live);
    }

    void IPlaneStore.Write(string plane, ulong bits, ulong mask)
    {
        if ((mask & ~Sel) != 0)
            throw new InvalidOperationException($"Workspace {Name} cannot write unselected sites {Bits.Format(mask & ~Sel, Bits.Width).TrimEnd('0')}.");
        foreach (var site in Bits.Indexes(mask)) Diverge(site);
        var index = Grammar.IndexOf(plane);
        local[index] = (local[index] & ~mask) | (bits & mask);
    }

    /// <summary>Samples every live selected site at the current source version.</summary>
    public void Capture()
    {
        var live = Focus.Members & ~(k0 | k1);
        for (var p = 0; p < local.Length; p++)
            if (!Grammar.Planes[p].BasisSupplied)
                local[p] = (local[p] & ~live) | (Source.Read(Grammar.Planes[p].Name) & live);
        k0 |= live;
        ReadEpoch = Source.Epoch;
        ReadVersion = Source.Version;
    }

    /// <summary>Creates a site here with no source counterpart, at the next free
    /// index above the source's sites.</summary>
    public int Introduce(IReadOnlyDictionary<string, bool>? bits = null)
    {
        var free = Bits.Indexes(~(Source.Allocated | introduced)).FirstOrDefault(-1);
        if (free < 0) throw new InvalidOperationException("No free index for an introduced site.");
        introduced |= Bits.Of(free);
        k1 |= Bits.Of(free);
        k0 &= ~Bits.Of(free);
        foreach (var (plane, value) in bits ?? new Dictionary<string, bool>())
        {
            var index = Grammar.IndexOf(plane);
            local[index] = Bits.With(local[index], free, value);
        }
        return free;
    }

    /// <summary>Proposes a row here, for example a closing edge. The source's own
    /// rows are unchanged; the proposal is marked with the rule that made it.</summary>
    public IntroducedRow IntroduceRow(string rowSet, int[] sites, string rule)
    {
        ArgumentNullException.ThrowIfNull(sites);
        Source.Rows(rowSet);
        if (sites.Any(site => !Bits.Get(Sel, site)))
            throw new ArgumentException("An introduced row may only use selected or introduced sites.", nameof(sites));
        var row = new IntroducedRow(rowSet, sites.ToArray(), rule, Name);
        introducedRows.Add(row);
        return row;
    }

    /// <summary>This workspace's read of a row set: the source rows plus rows
    /// introduced here. The source's own read does not include the introductions.</summary>
    public IReadOnlyList<int[]> RowsOf(string rowSet)
    {
        var rows = Source.Rows(rowSet);
        return Enumerable.Range(0, rows.Count).Select(rows.Row)
            .Concat(introducedRows.Where(r => r.RowSet == rowSet).Select(r => r.Sites)).ToArray();
    }

    /// <summary>Keeps a selection as part of this workspace. A retained selection
    /// is ordinary structure: it can be reused, transformed or itself selected.</summary>
    public Selection Retain(Selection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        if (!ReferenceEquals(selection.Scope.Tile, Source)) throw new ArgumentException("Selections must range over the source tile.", nameof(selection));
        retained.Add(selection);
        return selection;
    }

    /// <summary>Reads a row in the direction this workspace traverses it. Null when
    /// no retained row selection includes the row: not selected, no claim.
    /// Reversal is a read; the stored row keeps its direction.</summary>
    public int[]? Traverse(string rowSet, int row, bool reversed)
    {
        var selected = retained.Any(s => s.Scope.RowSet == rowSet && Bits.Get(s.Members, row));
        return selected ? Source.Rows(rowSet).Traverse(row, reversed) : null;
    }

    /// <summary>Support for a field over focused source sites. Introduced sites
    /// are never part of it: they are not evidence about the source.</summary>
    public SupportPlanes Support(string field)
    {
        if (!supports.TryGetValue(field, out var support))
        {
            var planes = Grammar.PlanesOf(field);
            var domain = Grammar.TryField(field, out var descriptor) && descriptor is CodeField code
                ? code.Domain
                : Domain.Binary(field, Grammar.Plane(planes[0]).WhenClear, Grammar.Plane(planes[0]).WhenSet);
            support = new SupportPlanes(field, domain, Focus.Members);
            supports.Add(field, support);
        }
        return support;
    }

    /// <summary>Supplies evidence from the source into this workspace's support.
    /// Refused (false) for introduced or unselected sites: the self-confirmation
    /// guard at bit level.</summary>
    public bool Observe(string field, int site)
    {
        if (!Bits.Get(Focus.Members, site) || Bits.Get(introduced, site)) return false;
        Support(field).Supply(site, Tile.ReadCode(this, field, site));
        supplied |= Bits.Of(site);
        return true;
    }

    /// <summary>Narrows this workspace's supports through relation laws. Rows must
    /// lie inside the focus: an unselected site has no support here, and reading
    /// it as empty would manufacture a contradiction.</summary>
    public PropagationResult Propagate(IReadOnlyList<LawRows> laws)
    {
        ArgumentNullException.ThrowIfNull(laws);
        foreach (var rows in laws)
        {
            var outside = rows.SitesOf(rows.AllRows) & ~Focus.Members;
            if (outside != 0)
                throw new InvalidOperationException($"{rows.Name} reads unselected sites {string.Join(",", Bits.Indexes(outside))}; select them first.");
        }
        return Propagation.Narrow(laws, Support);
    }

    /// <summary>Runs a transition law on this workspace's own view. The source is
    /// untouched; changed sites diverge. Rows must lie inside the selection,
    /// because reading an unselected site would manufacture a zero.</summary>
    public TransitionWitness ApplyLocal(LawRows rows, ulong rowMask)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var outside = rows.SitesOf(rowMask) & ~Sel;
        if (outside != 0)
            throw new InvalidOperationException($"Rows read unselected sites {string.Join(",", Bits.Indexes(outside))}; select them first.");
        return rows.Apply(this, rowMask);
    }

    /// <summary>The site's provenance, value bits and support, in words.</summary>
    public string Describe(int site)
    {
        if (!Bits.Get(Sel, site)) return $"{Name}[{site}]: not selected (no claim)";
        var planes = Grammar.Planes.Select(p => $"{p.Name}={(Bits.Get(Read(p.Name), site) ? 1 : 0)}");
        return $"{Name}[{site}]: {ProvenanceOf(site)}; {string.Join(" ", planes)}";
    }

    /// <summary>After this workspace's own commit the source holds the result:
    /// those sites read live again. The read version advances only when it was
    /// current before the commit, so an unrelated newer change stays visible as staleness.</summary>
    protected void Relink(ulong sites, bool versionWasCurrent)
    {
        sites &= ~introduced;
        k0 &= ~sites;
        k1 &= ~sites;
        ReadEpoch = Source.Epoch;
        if (versionWasCurrent) ReadVersion = Source.Version;
    }

    protected void Forget(IntroducedRow row) => introducedRows.Remove(row);

    private void Diverge(int site)
    {
        var provenance = (SiteProvenance)((Bits.Get(k0, site) ? 1 : 0) | (Bits.Get(k1, site) ? 2 : 0));
        if (provenance is SiteProvenance.Introduced or SiteProvenance.Diverged) return;
        if (provenance == SiteProvenance.ReferencedLive)
            for (var p = 0; p < local.Length; p++)
                if (!Grammar.Planes[p].BasisSupplied)
                    local[p] = Bits.With(local[p], site, Source.Bit(Grammar.Planes[p].Name, site));
        k0 |= Bits.Of(site);
        k1 |= Bits.Of(site);
    }
}
