namespace Gojo.Encoding.Core.Packed;

/// <summary>One occurrence of a link. Position is its stored use position even
/// when a group is read backwards. Reversed describes the selected view, not D.</summary>
public readonly record struct FamilyOccurrence(int Position, int Link, bool Reversed, FamilySpan Span);

/// <summary>Immutable ordered uses over a family's actual links. Each use is
/// (link &lt;&lt; 1) | reverse; half-open ranges delimit groups, including empty ones.
/// Repeated link references share a seam; repeated field references share a
/// value source. Neither relation is inferred from equal endpoints or values.</summary>
public sealed class FamilyUseTable
{
    private readonly EndpointRows uses = new(), ranges = new();
    public FamilyTemplate Template { get; }
    public int UseCount { get; }
    public int GroupCount { get; }
    public int UseBits { get; }
    public int RangeBits { get; }

    public FamilyUseTable(FamilyTemplate template, params int[][] groups)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(groups);
        if (template.LinkCount > 32768 || groups.Length > 65536)
            throw new ArgumentOutOfRangeException(nameof(groups), "This adapter uses at most 16-bit references.");
        var total = 0;
        foreach (var group in groups)
        {
            ArgumentNullException.ThrowIfNull(group);
            if (group.Length > 65535 - total)
                throw new ArgumentOutOfRangeException(nameof(groups), "The exclusive group end must fit a 16-bit reference.");
            total += group.Length;
            foreach (var use in group)
                if (use < 0 || (use >> 1) >= template.LinkCount)
                    throw new ArgumentOutOfRangeException(nameof(groups), "A use must refer to an existing family link.");
        }
        Template = template;
        UseCount = total;
        GroupCount = groups.Length;
        UseBits = OrderedBoundary.Bits(Math.Max(2, template.LinkCount * 2));
        RangeBits = OrderedBoundary.Bits(total + 1);
        var first = 0;
        foreach (var group in groups)
        {
            foreach (var use in group) uses.Append(UseBits, use);
            ranges.Append(RangeBits, first, first + group.Length);
            first += group.Length;
        }
    }

    public int SignedUse(int occurrence) => uses.Read(new(1, UseBits, occurrence))[0];
    public int[] Range(int group) => ranges.Read(new(2, RangeBits, group));
    public byte[] UseBytes() => uses.PackedBytes(1, UseBits);
    public byte[] RangeBytes() => ranges.PackedBytes(2, RangeBits);

    /// <summary>Reads occurrence identity and actual ordered endpoints. Reading
    /// backwards reverses the selected sequence and each use, without changing
    /// owned O/D/V, aliasing different links, or asserting a physical flow law.</summary>
    public FamilyOccurrence[] ReadGroup(FamilyTable table, FamilyEntry entry, int group, bool reverse = false)
    {
        ArgumentNullException.ThrowIfNull(table);
        if (!ReferenceEquals(table.Template, Template))
            throw new ArgumentException("The table must use this exact family template.", nameof(table));
        table.ReadRaw(entry, 0); // Validate the selected live entry, including empty groups.
        var range = Range(group);
        var result = new FamilyOccurrence[range[1] - range[0]];
        for (var i = 0; i < result.Length; i++)
        {
            var position = reverse ? range[1] - 1 - i : range[0] + i;
            var signed = SignedUse(position) ^ (reverse ? 1 : 0);
            var link = Template.Link(signed >> 1);
            var reversed = (signed & 1) != 0;
            var span = new FamilySpan(new(table, entry, reversed ? link.End : link.Start),
                new(table, entry, reversed ? link.Start : link.End), new(table, entry, link.Field));
            result[i] = new(position, signed >> 1, reversed, span);
        }
        return result;
    }

    /// <summary>Empty groups provide no positive continuity evidence. These
    /// reads inspect endpoints only, not reach, angles, or geometric realization.</summary>
    public bool IsContinuous(FamilyTable table, FamilyEntry entry, int group, bool reverse = false) =>
        Continuous(ReadGroup(table, entry, group, reverse));

    public bool IsClosed(FamilyTable table, FamilyEntry entry, int group, bool reverse = false)
    {
        var read = ReadGroup(table, entry, group, reverse);
        return Continuous(read) && read[^1].Span.End == read[0].Span.Start;
    }

    private static bool Continuous(FamilyOccurrence[] read)
    {
        if (read.Length == 0) return false;
        for (var i = 1; i < read.Length; i++)
            if (read[i - 1].Span.End != read[i].Span.Start) return false;
        return true;
    }
}
