using System.Security.Cryptography;
using System.Text;

namespace Gojo.EncodingB;

/// <summary>What a port promises about how its lanes may be used. The owner
/// declares it, and every connection's law is checked against it, so it is a
/// published contract rather than a hidden drain flag.</summary>
public enum PortAccounting
{
    /// <summary>Readers observe; nothing leaves the owner. Connections use relation
    /// laws, which cannot write, so any number of readers may connect.</summary>
    Observation = 1,

    /// <summary>Units leave the owner through the port. Connections use transition
    /// laws whose signature preserves a sum spanning the port lane and the
    /// arrival. All material connections together must still preserve that sum,
    /// so a material port cannot fan out.</summary>
    Material = 2,

    /// <summary>Both: one material transfer and any number of observation reads.</summary>
    Coupled = 3,
}

/// <summary>A self-describing port basis: the values a lane can read, the unit
/// promise of those values, the rung map that coarsened the owner's field (if
/// any), and the content hashes of the laws the codes obey. Two bases with the
/// same value names but a different unit, rung or law are different bases:
/// equal shape is not equal meaning, and nothing connects them silently.</summary>
public sealed class PortBasis
{
    public PortBasis(Domain domain, string unit, RungMap? rung = null, params LawTable[] laws)
    {
        ArgumentNullException.ThrowIfNull(domain);
        ArgumentException.ThrowIfNullOrWhiteSpace(unit);
        ArgumentNullException.ThrowIfNull(laws);
        if (rung is not null && !rung.Coarse.Values.SequenceEqual(domain.Values))
            throw new ArgumentException($"Rung map {rung.Name} reads into {rung.Coarse}, not into the basis values.", nameof(rung));
        Domain = domain;
        Unit = unit;
        Rung = rung;
        LawHashes = laws.Select(law => law.ContentHash).Distinct().Order(StringComparer.Ordinal).ToArray();
        var separator = "\u001f";
        var contract = string.Join(separator,
            "values", string.Join(separator, domain.Values),
            "unit", unit,
            "rung", rung?.ContentHash ?? "-",
            "laws", string.Join(",", LawHashes));
        Fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(contract)))[..16].ToLowerInvariant();
    }

    /// <summary>The values one lane can read, in code order.</summary>
    public Domain Domain { get; }

    /// <summary>What one value is a value of, for example "one unit of water".</summary>
    public string Unit { get; }

    /// <summary>The map that coarsened the owner's field, if any. Its γ tells a
    /// reader which fine values a published value admits; it cannot recover them.</summary>
    public RungMap? Rung { get; }

    /// <summary>Content hashes of the laws the codes obey, such as the zero
    /// contract of an addition law.</summary>
    public IReadOnlyList<string> LawHashes { get; }

    /// <summary>Identifies the contract: values, unit, rung table and law tables.</summary>
    public string Fingerprint { get; }

    public bool SameAs(PortBasis other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Fingerprint == other.Fingerprint;
    }

    public string Describe()
    {
        var rung = Rung is null ? "" : $", coarsened by {Rung.Name} ({Rung.ContentHash})";
        var laws = LawHashes.Count == 0 ? "" : $", codes obey {string.Join(", ", LawHashes)}";
        return $"{Domain.Describe(Domain.Full)} of {Unit}{rung}{laws} [basis {Fingerprint}]";
    }

    public override string ToString() => Describe();
}
