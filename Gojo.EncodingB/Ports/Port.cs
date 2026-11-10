namespace Gojo.EncodingB;

/// <summary>An exposed relation surface of a tile (topology grade 3). Lane i is
/// the i-th published site, read through one field and, when the basis carries a
/// rung map, coarsened by it. A port is the whole of what a reader can reach:
/// its contract and its publication, never the owner's other planes, sites or
/// finer detail. Publishing one is a structural edit of the owner.</summary>
public sealed class Port
{
    private readonly Tile owner;
    private readonly int[] sites;
    private readonly List<PortConnection> connections = [];

    internal Port(Tile owner, string name, string field, int[] sites, PortBasis basis, PortAccounting accounting, TensionField? tension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        this.owner = owner;
        this.sites = sites;
        Name = name;
        Field = field;
        Basis = basis;
        Accounting = accounting;
        Tension = tension;
    }

    public string Name { get; }

    /// <summary>The owner's name, for reading witnesses. The owner itself is not exposed.</summary>
    public string OwnerName => owner.Name;

    /// <summary>The published field, named in the owner's grammar.</summary>
    public string Field { get; }

    public int LaneCount => sites.Length;

    /// <summary>Every lane of the port as a 64-bit lane plane.</summary>
    public ulong Lanes => Bits.Low(LaneCount);

    public PortBasis Basis { get; }
    public PortAccounting Accounting { get; }
    public IReadOnlyList<PortConnection> Connections => connections;

    internal Tile Owner => owner;
    internal TensionField? Tension { get; }

    /// <summary>The publication: each lane's value in the port basis, read from
    /// the owner's committed state. Reading never changes the owner.</summary>
    public PortPublication Read()
    {
        var values = new int?[LaneCount];
        for (var lane = 0; lane < LaneCount; lane++)
            values[lane] = Tile.ReadValue(owner, Field, sites[lane]) is int fine ? Basis.Rung?.Abstract(fine) ?? fine : null;
        return new PortPublication(Name, owner.Version, owner.Epoch, values, Basis);
    }

    /// <summary>Connects this port to an inlet. With no correspondence the
    /// connection is an open slot: reads stay open and keep the publication as
    /// residue, and no material moves. A correspondence that does not read from
    /// this port's basis into the inlet's, or that breaks the accounting class, is
    /// refused and nothing is connected.</summary>
    public ConnectResult Connect(Inlet inlet, Correspondence? correspondence = null)
    {
        ArgumentNullException.ThrowIfNull(inlet);
        if (inlet.LaneCount != LaneCount)
            throw new ArgumentException($"Port {Name} has {LaneCount} lanes and inlet {inlet.Name} has {inlet.LaneCount}; a grouped surface connects whole.", nameof(inlet));
        if (correspondence is not null && PortConnection.Refusal(this, inlet, correspondence) is { } refusal)
            return new ConnectResult(null, refusal);
        var connection = new PortConnection(this, inlet, correspondence);
        connections.Add(connection);
        inlet.Attach(connection);
        return new ConnectResult(connection, connection.IsOpen
            ? $"open: no declared correspondence from basis {Basis.Fingerprint} to {inlet.Basis.Fingerprint}; reads stay open and keep the publication as residue"
            : $"connected through {correspondence!.Law.Name} ({correspondence.Law.ContentHash})");
    }

    public string Describe() =>
        $"port {OwnerName}.{Name}: {LaneCount} lane(s) of '{Field}', {Accounting.ToString().ToLowerInvariant()}; {Basis.Describe()}";

    internal int SiteOf(int lane) => sites[lane];

    internal ulong SitesOf(ulong lanes) => Bits.Indexes(lanes).Aggregate(0UL, (mask, lane) => mask | Bits.Of(sites[lane]));

    public override string ToString() => Describe();
}

/// <summary>An exposed receiving surface: an ordered subset of a tile's sites
/// and the field there that a connection may inform (observation) or fill
/// (material). Its basis is the receiver's own. A connection reaches it only
/// through a correspondence declared from the port's basis to this one.</summary>
public sealed class Inlet
{
    private readonly Tile owner;
    private readonly int[] sites;
    private readonly List<PortConnection> connections = [];

    internal Inlet(Tile owner, string name, string field, int[] sites, PortBasis basis, TensionField? tension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(basis);
        this.owner = owner;
        this.sites = sites;
        Name = name;
        Field = field;
        Basis = basis;
        Tension = tension;
    }

    public string Name { get; }
    public string OwnerName => owner.Name;
    public string Field { get; }
    public int LaneCount => sites.Length;
    public PortBasis Basis { get; }
    public IReadOnlyList<PortConnection> Connections => connections;

    internal Tile Owner => owner;
    internal TensionField? Tension { get; }

    public string Describe() =>
        $"inlet {OwnerName}.{Name}: {LaneCount} lane(s) into '{Field}'{(Tension is null ? "" : " as tension")}; {Basis.Describe()}";

    internal int SiteOf(int lane) => sites[lane];

    internal ulong SitesOf(ulong lanes) => Bits.Indexes(lanes).Aggregate(0UL, (mask, lane) => mask | Bits.Of(sites[lane]));

    internal void Attach(PortConnection connection) => connections.Add(connection);

    public override string ToString() => Describe();
}

/// <summary>What a port published at one owner version: one value per lane in
/// the port basis. A null value is a lane whose source bits do not decode (a
/// broken thermometer or an out-of-domain code); it is reported, not repaired.</summary>
public sealed record PortPublication(string Port, long OwnerVersion, long OwnerEpoch, IReadOnlyList<int?> Values, PortBasis Basis)
{
    /// <summary>Lanes whose published value decodes.</summary>
    public ulong Readable => Enumerable.Range(0, Values.Count).Where(lane => Values[lane] is not null).Aggregate(0UL, (m, lane) => m | Bits.Of(lane));

    /// <summary>Lanes publishing <paramref name="value"/>, as a lane plane.</summary>
    public ulong LanesReading(int value) =>
        Enumerable.Range(0, Values.Count).Where(lane => Values[lane] == value).Aggregate(0UL, (m, lane) => m | Bits.Of(lane));

    public string NameOf(int lane) => Values[lane] is int value ? Basis.Domain.NameOf(value) : "(does not decode)";
}
