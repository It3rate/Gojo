namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>One selected occurrence's current read. Tip is an ordinal landmark
/// on the field's own rail, not a measured length. Reversed exchanges ordered
/// endpoints for this use; it does not flip the stored ODV direction bit.</summary>
public readonly record struct VariationRead(int Occurrence, int Link, int Field,
    bool Reversed, int From, int To, int Tip, byte CoarseWord);

/// <summary>Retained field/use references connected to the existing local
/// adjustment mechanism. Shared uses observe one dial; independent fields own
/// different rails. No shape, symmetry or confidence label governs response.
/// Opposed uses are a narrow precursor to mirror correspondence, not a claim
/// of geometric reflection, parallelism or equal independently measured spans.
/// </summary>
public sealed class SharedVariation
{
    public FamilyTable Table { get; }
    public FamilyEntry Entry { get; }
    public FamilyUseTable Uses { get; }
    public FamilyAdjustment Adjustment { get; }
    public int UseCount => Uses.UseCount;
    public int BindingBits { get; }

    public SharedVariation(FamilyTable table, FamilyEntry entry, FamilyUseTable uses,
        int? selectedField = 0, IReadOnlyCollection<int>? pinnedFields = null)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(uses);
        if (!ReferenceEquals(table.Template, uses.Template))
            throw new ArgumentException("Uses must belong to the supplied family.", nameof(uses));
        if (uses.UseCount == 0)
            throw new ArgumentException("Supply at least one observable occurrence.", nameof(uses));
        Table = table;
        Entry = entry;
        Uses = uses;
        BindingBits = OrderedBoundary.Bits(Math.Max(uses.UseCount,
            Math.Max(2 * table.Template.LinkCount, table.Template.FieldCount)));
        Adjustment = new(table, entry, uses, selectedField, pinnedFields);
    }

    /// <summary>A closed occurrence order with either one reused reach or one
    /// independent reach per link. The four-step default supplies no geometric
    /// square, equal-angle or area claim. Participation is known in all fields.
    /// </summary>
    public static SharedVariation Reaches(bool shared, int count = 4, bool pinned = false)
    {
        if (count is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(count));
        var fields = Enumerable.Repeat(new FamilyField(0, 0), shared ? 1 : count).ToArray();
        var links = Enumerable.Range(0, count)
            .Select(i => new FamilyLink(i, (i + 1) % count, shared ? 0 : i)).ToArray();
        return Loaded(fields, links, Enumerable.Range(0, count).Select(i => 2 * i).ToArray(), pinned);
    }

    /// <summary>One link used in both orders, or two independently valued
    /// lookalike links used in opposite orders. Their initial occurrence reads
    /// agree, but only the first construction retains a shared link and reach.
    /// This is ordered correspondence, not a geometric reflection engine.
    /// </summary>
    public static SharedVariation OpposedReads(bool shared, bool pinned = false) => shared
        ? Loaded([new(0, 0)], [new(0, 1, 0)], [0, 1], pinned)
        : Loaded([new(0, 0), new(0, 0)], [new(0, 1, 0), new(0, 1, 1)], [0, 3], pinned);

    private static SharedVariation Loaded(FamilyField[] fields, FamilyLink[] links, int[] uses, bool pinned)
    {
        var source = new FamilyTable(new(fields, links));
        var entry = source.Append(1, Enumerable.Repeat(1UL, fields.Length).ToArray());
        var selected = new FamilyUseTable(source.Template, uses);
        // Keep only independently decoded retained structure. There is no live
        // generator or presentation coordinate behind a subsequent response.
        var retained = FamilyUseCodec.Decode(FamilyUseCodec.Encode(source, entry, selected));
        return new(retained.Table, retained.Entry, retained.Uses, 0, pinned ? [0] : null);
    }

    public VariationRead Read(int occurrence, bool reverse = false)
    {
        if ((uint)occurrence >= (uint)UseCount) throw new ArgumentOutOfRangeException(nameof(occurrence));
        var signed = Uses.SignedUse(occurrence) ^ (reverse ? 1 : 0);
        var link = signed >> 1;
        var source = Table.Template.Link(link);
        var reversed = (signed & 1) != 0;
        return new(occurrence, link, source.Field, reversed,
            reversed ? source.End : source.Start, reversed ? source.Start : source.End,
            Adjustment.Tip(source.Field), Table.ReadCoarse(Entry, source.Field));
    }

    public VariationRead[] Reads() => Enumerable.Range(0, UseCount).Select(i => Read(i)).ToArray();

    /// <summary>Exact same source field in this owner/entry. Different sources
    /// do not imply either unequal extents or statistical independence.</summary>
    public bool SharesSource(int first, int second) => Read(first).Field == Read(second).Field;

    /// <summary>One source rail read under the same local order. This is not a
    /// claim that the two associated geometric links occupy the same place.</summary>
    public bool SameDirectedSource(int first, int second)
    {
        var a = Read(first); var b = Read(second);
        return a.Field == b.Field && a.Reversed == b.Reversed;
    }

    /// <summary>One source rail observed through opposed ordered uses. Stored
    /// O/D/V remains unchanged and no second material parcel is created.</summary>
    public bool OpposedSource(int first, int second)
    {
        var a = Read(first); var b = Read(second);
        return a.Field == b.Field && a.Reversed != b.Reversed;
    }

    public FamilyAdjustmentPrediction Predict(bool concealControl = false) => Adjustment.Predict(concealControl);
    public FamilyAdjustmentStep Push(bool concealControl = false) => Adjustment.Push(concealControl);
    public int[] ChangedUses()
    {
        var changed = Adjustment.AffectedFields().ToHashSet();
        return Enumerable.Range(0, UseCount).Where(i => changed.Contains(Read(i).Field)).ToArray();
    }

    /// <summary>Derived inspection rows [occurrence, signed use, source field].
    /// The canonical connections remain the family's link and use arrays;
    /// this chart is not another binding store or operational dispatcher.</summary>
    public int[][] BindingRows() => Enumerable.Range(0, UseCount)
        .Select(i => new[] { i, Uses.SignedUse(i), Table.Template.Link(Uses.SignedUse(i) >> 1).Field }).ToArray();
    public byte[] BindingBytes()
    {
        var rows = new EndpointRows();
        foreach (var row in BindingRows()) rows.Append(BindingBits, row);
        return rows.PackedBytes(3, BindingBits);
    }

    /// <summary>Retained family artifact only; runtime request, dials and
    /// transition history are deliberately not a checkpoint in this transport.</summary>
    public byte[] SourceBytes() => FamilyUseCodec.Encode(Table, Entry, Uses);
    public byte[] StoreBytes() => Adjustment.StoreBytes();
}
