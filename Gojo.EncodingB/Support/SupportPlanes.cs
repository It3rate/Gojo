namespace Gojo.EncodingB;

/// <summary>Marginal support (U0) for one field over a set of sites: plane v
/// has bit i set while value v is still possible at site i. One set bit at a site
/// means settled, several mean competing candidates, none means contradiction.
/// None of these is a probability. Supports live in the workspace that reads,
/// never in the source tile.</summary>
public sealed class SupportPlanes
{
    private readonly ulong[] planes;
    private ulong narrowedThisEpoch;

    /// <summary>Starts open: every value possible at every given site. Open is
    /// not zero: a site nobody has supplied is not a site that reads zero.</summary>
    public SupportPlanes(string field, Domain domain, ulong sites)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);
        ArgumentNullException.ThrowIfNull(domain);
        Field = field;
        Domain = domain;
        Sites = sites;
        planes = Enumerable.Repeat(sites, domain.Size).ToArray();
    }

    private SupportPlanes(SupportPlanes other)
    {
        Field = other.Field;
        Domain = other.Domain;
        Sites = other.Sites;
        Epoch = other.Epoch;
        planes = other.planes.ToArray();
        narrowedThisEpoch = other.narrowedThisEpoch;
    }

    public string Field { get; }
    public Domain Domain { get; }
    public ulong Sites { get; }

    /// <summary>Within an epoch supports only narrow. A changed source starts a
    /// new epoch in which a site may be reopened.</summary>
    public int Epoch { get; private set; }

    public ulong Plane(int value) => planes[value];

    /// <summary>The value mask still possible at a site.</summary>
    public ulong Values(int site)
    {
        ulong values = 0;
        for (var v = 0; v < planes.Length; v++)
            if (Bits.Get(planes[v], site)) values |= 1UL << v;
        return values;
    }

    public int? SettledValue(int site)
    {
        var values = Values(site);
        return Bits.Count(values) == 1 ? Bits.Indexes(values).First() : null;
    }

    /// <summary>Sites with no possible value: contradictions.</summary>
    public ulong Empty => Sites & ~planes.Aggregate(0UL, (any, plane) => any | plane);

    /// <summary>Sites with exactly one possible value.</summary>
    public ulong Settled => Bits.Indexes(Sites).Where(site => Bits.Count(Values(site)) == 1).Aggregate(0UL, (m, s) => m | Bits.Of(s));

    /// <summary>Sites with more than one possible value: open questions.</summary>
    public ulong Open => Bits.Indexes(Sites).Where(site => Bits.Count(Values(site)) > 1).Aggregate(0UL, (m, s) => m | Bits.Of(s));

    /// <summary>Intersects a site's support with <paramref name="allowed"/>. Never widens.</summary>
    public bool Restrict(int site, ulong allowed)
    {
        CheckSite(site);
        var changed = false;
        for (var v = 0; v < planes.Length; v++)
            if (!Bits.Get(allowed, v) && Bits.Get(planes[v], site))
            {
                planes[v] &= ~Bits.Of(site);
                changed = true;
            }
        if (changed) narrowedThisEpoch |= Bits.Of(site);
        return changed;
    }

    /// <summary>Supplied evidence: the site holds this value at the read's resolution.</summary>
    public bool Supply(int site, int code) => Restrict(site, Bits.Of(code));

    public void BeginEpoch()
    {
        Epoch++;
        narrowedThisEpoch = 0;
    }

    /// <summary>Reopens a site after its source changed. Refused if the site was
    /// already narrowed in this epoch: within an epoch supports only narrow.</summary>
    public void Reopen(int site)
    {
        CheckSite(site);
        if (Bits.Get(narrowedThisEpoch, site))
            throw new InvalidOperationException($"Site {site} was narrowed in epoch {Epoch}; begin a new epoch before widening it.");
        for (var v = 0; v < planes.Length; v++) planes[v] |= Bits.Of(site);
    }

    public string Describe(int site) => Domain.Describe(Values(site));

    public SupportPlanes Clone() => new(this);

    private void CheckSite(int site)
    {
        if (!Bits.Get(Sites, site)) throw new ArgumentOutOfRangeException(nameof(site), $"Site {site} is not in this support's scope.");
    }
}
