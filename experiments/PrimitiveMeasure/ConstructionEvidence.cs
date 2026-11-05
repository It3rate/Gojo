namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Publication choices for the observer, not edits to source structure.</summary>
public enum ConstructionVisibility { All, DirectionsHidden, GroupingHidden, GeometryHidden, ConnectionsOnly }

/// <summary>A derived small observation stream. Known zero differs from an
/// unpublished or unproved slot. Counts are reference occurrences, not lengths,
/// charge, confidence or probabilities. The mask is observation availability.</summary>
public sealed class ConstructionClues
{
    private readonly int?[] values;
    public IReadOnlyList<int?> Values { get; }
    public int ValueBits => OrderedBoundary.Bits(values.Where(v => v.HasValue).Select(v => v!.Value).DefaultIfEmpty(0).Max() + 1);
    public ConstructionClues(IEnumerable<int?> values)
    {
        this.values = values.ToArray();
        if (this.values.Length != ConstructionEvidence.Labels.Length || this.values.Any(v => v is < 0 or > 65535))
            throw new ArgumentException("Supply one local count or open slot per observation role.", nameof(values));
        Values = Array.AsReadOnly(this.values);
    }

    public ConstructionClues Publish(ConstructionVisibility visibility)
    {
        var copy = values.ToArray();
        int[] hidden = visibility switch
        {
            ConstructionVisibility.All => [],
            ConstructionVisibility.DirectionsHidden => [6, 7, 9, 10, 11],
            ConstructionVisibility.GroupingHidden => [5, 6, 7, 8, 9],
            ConstructionVisibility.GeometryHidden => [14, 15, 16, 17, 18],
            ConstructionVisibility.ConnectionsOnly => Enumerable.Range(5, 15).ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(visibility))
        };
        foreach (var slot in hidden) copy[slot] = null;
        return new(copy);
    }

    public byte[] KnownBytes()
    {
        var bytes = new byte[(values.Length + 7) / 8];
        for (var i = 0; i < values.Length; i++) if (values[i].HasValue) bytes[i / 8] |= (byte)(1 << (i % 8));
        return bytes;
    }

    public byte[] PackedBytes()
    {
        var rows = new EndpointRows();
        foreach (var value in values) rows.Append(ValueBits, value ?? 0);
        return rows.PackedBytes(1, ValueBits);
    }

    /// <summary>Only two known incompatible answers exclude a candidate. An
    /// unproved candidate claim is not silently interpreted as false.</summary>
    public bool CompatibleWith(ConstructionClues candidate) =>
        values.Zip(candidate.values).All(pair => pair.First is null || pair.Second is null || pair.First == pair.Second);

    public int[] ConflictsWith(ConstructionClues candidate) => Enumerable.Range(0, values.Length)
        .Where(i => values[i].HasValue && candidate.values[i].HasValue && values[i] != candidate.values[i]).ToArray();
}

/// <summary>One read grammar for every supplied construction. Names and pixels
/// are absent from Read. Candidate comparison is an observer experiment over
/// published clues, not a learned classifier or a construction-history oracle.</summary>
public static class ConstructionEvidence
{
    public static readonly string[] Labels = [
        "Sites", "Links", "Components", "Independent cycles", "Branch sites",
        "Use groups", "Continuous groups", "Closed groups", "Links used by several groups",
        "Links used oppositely across groups", "Sites with several departing links", "Sites with several arriving links",
        "Used value roles", "Value roles shared by several links", "Complete rail correspondence",
        "Complete radial correspondence", "Complete quarter-lane correspondence", "Nondegenerate rectangle proved",
        "Nondegenerate square proved", "Participating links"
    ];

    public static ConstructionClues Read(ConstructionSurface surface, bool reverse = false)
    {
        var table = surface.Table; var template = table.Template; var uses = surface.Uses;
        if (!ReferenceEquals(uses.Template, template))
            throw new ArgumentException("The selected uses must refer to this exact family template.", nameof(surface));
        table.ReadRaw(surface.Entry, 0); // Empty views still require a live, owner-local instance.
        var allLinks = Enumerable.Range(0, template.LinkCount).Select(template.Link).ToArray();
        var sites = allLinks.SelectMany(link => new[] { link.Start, link.End }).Distinct().ToArray();
        var neighbors = sites.ToDictionary(s => s, _ => new List<int>());
        var degree = sites.ToDictionary(s => s, _ => 0);
        foreach (var link in allLinks)
        {
            neighbors[link.Start].Add(link.End); neighbors[link.End].Add(link.Start);
            degree[link.Start]++; degree[link.End]++;
        }
        var unseen = sites.ToHashSet(); var components = 0;
        while (unseen.Count > 0)
        {
            var pending = new Queue<int>(); var first = unseen.First(); unseen.Remove(first); pending.Enqueue(first); components++;
            while (pending.TryDequeue(out var at))
                foreach (var next in neighbors[at]) if (unseen.Remove(next)) pending.Enqueue(next);
        }
        var groups = Enumerable.Range(0, uses.GroupCount)
            .Select(g => uses.ReadGroup(table, surface.Entry, g, reverse)).ToArray();
        var occurrences = groups.SelectMany((group, g) => group.Select(use => (Group: g, Use: use))).ToArray();
        var reused = occurrences.GroupBy(x => x.Use.Link).Where(g => g.Select(x => x.Group).Distinct().Count() > 1).ToArray();
        var opposed = reused.Count(g => g.Any(a => g.Any(b => a.Group != b.Group && a.Use.Reversed != b.Use.Reversed)));
        var departing = occurrences.GroupBy(x => x.Use.Span.Start.Local).Count(g => g.Select(x => x.Use.Link).Distinct().Count() > 1);
        var arriving = occurrences.GroupBy(x => x.Use.Span.End.Local).Count(g => g.Select(x => x.Use.Link).Distinct().Count() > 1);
        var fields = allLinks.GroupBy(link => link.Field).ToArray();
        var basis = surface.Basis;
        var corresponds = basis?.Corresponds(surface) == true;
        var geometry = basis?.Read(surface) ?? ((byte)3, (byte)3);
        static int? Proof(byte support) => support == 2 ? 1 : support == 1 ? 0 : null;
        return new ConstructionClues([
            sites.Length, allLinks.Length, components, allLinks.Length - sites.Length + components,
            degree.Count(pair => pair.Value >= 3), uses.GroupCount,
            Enumerable.Range(0, uses.GroupCount).Count(g => uses.IsContinuous(table, surface.Entry, g, reverse)),
            Enumerable.Range(0, uses.GroupCount).Count(g => uses.IsClosed(table, surface.Entry, g, reverse)),
            reused.Length, opposed, departing, arriving, fields.Length, fields.Count(g => g.Count() > 1),
            corresponds && basis!.RailSource is not null ? 1 : 0,
            corresponds && basis!.RadialSource is not null ? 1 : 0,
            corresponds && basis!.PathSource is not null ? 1 : 0,
            Proof(geometry.Item1), Proof(geometry.Item2),
            allLinks.Count(link => (table.ReadCoarse(surface.Entry, link.Field) & 1) != 0)
        ]);
    }

    public static int[] Match(ConstructionClues observation, IReadOnlyList<ConstructionExample> candidates, bool reverse = false) =>
        Enumerable.Range(0, candidates.Count).Where(i => observation.CompatibleWith(Read(candidates[i].Surface, reverse))).ToArray();
}
