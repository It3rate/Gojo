namespace Gojo.EncodingB;

/// <summary>Explicit local topology: ordered rows of site references, stored as
/// offsets plus packed references (CSR). Order inside a row is meaningful. A
/// reference repeated in one row is one site used twice, not two equal sites.
/// Every row records where it came from. Editing rows is a structural edit.</summary>
public sealed class RowSet
{
    private readonly Tile owner;
    private readonly List<int> offsets = [0];
    private readonly List<int> references = [];
    private readonly List<string> provenance = [];

    internal RowSet(Tile owner, string name, string[] roles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        this.owner = owner;
        Name = name;
        Roles = roles;
    }

    public string Name { get; }

    /// <summary>Role names when every row has the same arity; empty for paths.</summary>
    public IReadOnlyList<string> Roles { get; }

    public int Count => provenance.Count;

    public int[] Row(int row)
    {
        CheckRow(row);
        return references.GetRange(offsets[row], offsets[row + 1] - offsets[row]).ToArray();
    }

    public string Provenance(int row)
    {
        CheckRow(row);
        return provenance[row];
    }

    /// <summary>Sites a row references, as a site mask.</summary>
    public ulong SitesOf(int row) => Bits.Of(Row(row));

    /// <summary>Rows as a 64-bit mask, for selections over this row set.</summary>
    public ulong All => Bits.Low(Math.Min(Count, Bits.Width));

    public int Add(IReadOnlyList<int> sites, string origin = "authored")
    {
        CheckSites(sites);
        if (Count == Bits.Width) throw new InvalidOperationException("A row set holds at most 64 rows.");
        references.AddRange(sites);
        offsets.Add(references.Count);
        provenance.Add(origin);
        owner.StructuralEdit();
        return Count - 1;
    }

    public int Add(params int[] sites) => Add(sites, "authored");

    /// <summary>Inserts a site into a row, for example a landmark on an edge.
    /// The row keeps its direction; every reader of the row sees the new site.</summary>
    public void Insert(int row, int position, int site)
    {
        CheckRow(row);
        if (Roles.Count > 0) throw new InvalidOperationException("Rows with declared roles have fixed arity.");
        owner.CheckSite(site);
        var length = offsets[row + 1] - offsets[row];
        if (position < 0 || position > length) throw new ArgumentOutOfRangeException(nameof(position));
        references.Insert(offsets[row] + position, site);
        for (var r = row + 1; r < offsets.Count; r++) offsets[r]++;
        owner.StructuralEdit();
    }

    /// <summary>Reads a row in its stored order or reversed. Reversal is a read;
    /// the stored row is not changed.</summary>
    public int[] Traverse(int row, bool reversed)
    {
        var sites = Row(row);
        if (reversed) Array.Reverse(sites);
        return sites;
    }

    internal RowSetSnapshot Snapshot() => new(Name, Roles.ToArray(), offsets.ToArray(), references.ToArray(), provenance.ToArray());

    internal void RestoreRows(RowSetSnapshot snapshot)
    {
        offsets.Clear();
        offsets.AddRange(snapshot.Offsets);
        references.Clear();
        references.AddRange(snapshot.References);
        provenance.Clear();
        provenance.AddRange(snapshot.Provenance);
    }

    private void CheckSites(IReadOnlyList<int> sites)
    {
        ArgumentNullException.ThrowIfNull(sites);
        if (sites.Count == 0) throw new ArgumentException("A row references at least one site.", nameof(sites));
        if (Roles.Count > 0 && sites.Count != Roles.Count)
            throw new ArgumentException($"Rows in '{Name}' have {Roles.Count} roles.", nameof(sites));
        foreach (var site in sites) owner.CheckSite(site);
    }

    private void CheckRow(int row)
    {
        if (row < 0 || row >= Count) throw new ArgumentOutOfRangeException(nameof(row));
    }
}

public sealed record RowSetSnapshot(string Name, string[] Roles, int[] Offsets, int[] References, string[] Provenance)
{
    public bool SameRows(RowSetSnapshot other) =>
        Name == other.Name && Offsets.SequenceEqual(other.Offsets) &&
        References.SequenceEqual(other.References) && Provenance.SequenceEqual(other.Provenance);
}
