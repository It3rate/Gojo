using System.Text;

namespace Gojo.EncodingC;

/// <summary>Names for people reading a machine. The encoding never stores a
/// lens and never reads one: remove every lens and every store, law, port,
/// wiring and workspace behaves exactly the same. Tests and diagnostics use a
/// lens to print bits in words. A name here is a label for a reader, not where
/// meaning lives; meaning is what the laws wired to the bits do.</summary>
public sealed class Lens
{
    private readonly Dictionary<object, string> names = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<(object Thing, int Part), string> parts = [];
    private readonly Dictionary<(object Thing, int Part), string[]> values = [];

    /// <summary>Names a store, port, wiring, law or field.</summary>
    public Lens Name(object thing, string name)
    {
        ArgumentNullException.ThrowIfNull(thing);
        names[thing] = name;
        return this;
    }

    /// <summary>Names a part: a plane of a store, or a role of a law.</summary>
    public Lens Name(object thing, int part, string name)
    {
        ArgumentNullException.ThrowIfNull(thing);
        parts[(thing, part)] = name;
        return this;
    }

    /// <summary>Names the values of a role of a law (or, with part 0, of a field).</summary>
    public Lens Values(object thing, int part, params string[] valueNames)
    {
        ArgumentNullException.ThrowIfNull(thing);
        values[(thing, part)] = valueNames.ToArray();
        return this;
    }

    public string NameOf(object thing) => names.TryGetValue(thing, out var name) ? name : thing.GetType().Name.ToLowerInvariant();

    public string NameOf(object thing, int part) => parts.TryGetValue((thing, part), out var name) ? name : part.ToString();

    public string ValueOf(object thing, int part, int? value) =>
        value is not int v ? "?"
        : values.TryGetValue((thing, part), out var named) && v < named.Length ? named[v]
        : v.ToString();

    /// <summary>"valve[2]: occupied=1 open=0" — every plane at one position.</summary>
    public string Describe(Store store, int position)
    {
        ArgumentNullException.ThrowIfNull(store);
        var text = new StringBuilder($"{NameOf(store)}[{position}]:");
        for (var p = 0; p < store.PlaneCount; p++)
            text.Append($" {NameOf(store, p)}={(store.Read(p, position) ? 1 : 0)}");
        if (store.IsSpare(position)) text.Append(" (spare)");
        else if (!store.IsLive(position)) text.Append(" (retired)");
        return text.ToString();
    }

    /// <summary>"[source=1, gate=open, target=0]" — a law tuple with role and value names.</summary>
    public string Describe(Law law, int tuple)
    {
        ArgumentNullException.ThrowIfNull(law);
        var t = law.Tuple(tuple);
        return "[" + string.Join(", ", t.Select((v, r) => $"{NameOf(law, r)}={ValueOf(law, r, v)}")) + "]";
    }

    /// <summary>"[…] -> […]" for one transition.</summary>
    public string Describe(TransitionLaw law, int before, int after) => $"{Describe((Law)law, before)} -> {Describe((Law)law, after)}";
}

/// <summary>Notation for ODV words, for people: R/L for the orientation, +/−
/// for the direction, ? when detectable and 0 when not. The words store only
/// their three bits.</summary>
public static class OdvNotation
{
    private static readonly string[] Names = ["R+0", "R+?", "R-0", "R-?", "L+0", "L+?", "L-0", "L-?"];

    public static string Name(OdvWord word) => Names[word.Code];

    public static OdvWord Parse(string name)
    {
        var code = Array.IndexOf(Names, name);
        return code >= 0 ? new OdvWord((byte)code) : throw new ArgumentException($"'{name}' is not an ODV word.", nameof(name));
    }

    /// <summary>"{R+0, R+?}" for a mask over the eight words.</summary>
    public static string Describe(ulong support) =>
        "{" + string.Join(", ", Enumerable.Range(0, 8).Where(c => ((support >> c) & 1) != 0).Select(c => Names[c])) + "}";
}
