using Gojo.Core;

namespace Gojo.Structures;

/// <summary>
/// A derived execution projection of one captured, validated native table.
/// NativeTable owns its meaning and witnesses retain the rows they consume.
/// Re-capture to execute an edited law; this is not a live table subscription.
/// </summary>
public sealed class CoarseDifferencePlan
{
    private readonly Relation input;
    private readonly Relation output;
    private readonly Relation[] regions;
    private readonly Relation[] outputRegions;
    private readonly Relation[] rows;
    private readonly int[] masks;
    private readonly Relation nativeTable;
    private readonly (Relation Site, IElement Left, IElement Right)[] relationSites;
    private readonly (TickRef Site, long Value)[] tickSites;

    /// <summary>A defensive inspection copy of the captured native service.</summary>
    public Relation NativeTable => (Relation)new ExposedStructureSnapshot().Copy(nativeTable);

    private CoarseDifferencePlan(Relation table, Relation input, Relation output,
        Relation[] regions, Relation[] outputRegions, Relation[] rows, int[] masks)
    {
        nativeTable = table; this.input = input; this.output = output;
        this.regions = regions; this.outputRegions = outputRegions;
        this.rows = rows; this.masks = masks;
        var relationGuard = new List<(Relation, IElement, IElement)>();
        var tickGuard = new List<(TickRef, long)>();
        var seenRelations = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        var seenTicks = new HashSet<TickRef>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(table);
        while (pending.TryPop(out var element))
            if (element is Relation relation && seenRelations.Add(relation))
            {
                relationGuard.Add((relation, relation.Left, relation.Right));
                pending.Push(relation.Left); pending.Push(relation.Right);
            }
            else if (element is CarrierLocation location && seenTicks.Add(location.Tick))
                tickGuard.Add((location.Tick, location.Ticks));
        relationSites = relationGuard.ToArray(); tickSites = tickGuard.ToArray();
    }

    public static CoarseDifferencePlan? Capture(Relation table)
    {
        if (!CoarseReadSurface.TryReadTable(table, out _, out _, out _)) return null;
        var frozen = (Relation)new ExposedStructureSnapshot().Copy(table);
        if (!CoarseReadSurface.TryReadTable(frozen, out _, out var law, out var rows) ||
            !CoarseReadSurface.TryReadLaw(law, out var input, out var output, out _) ||
            !CoarseReadSurface.TryReadPartition(input, out _, out var inputs) ||
            !CoarseReadSurface.TryReadPartition(output, out _, out var outputs)) return null;
        var masks = new int[rows.Length];
        for (var i = 0; i < rows.Length; i++)
        {
            // This is decoding the table's admitted region union, not creating
            // another arithmetic interpretation or evaluating private operands.
            var witness = ReadWitness.Create(new Carrier(), rows[i], output, rows[i].Right,
                Group(), rows[i]);
            if (!CoarseReadSurface.TryReadMask(witness, output, out masks[i])) return null;
        }
        return new CoarseDifferencePlan(frozen, input, output, inputs, outputs, rows, masks);
    }

    /// <summary>Reads incoming native region unions, looks up admitted rows and
    /// publishes their union. Arithmetic and table validation happened at capture.
    /// The captured coordinates are a stable service contract for this plan.</summary>
    public Relation Evaluate(Carrier root, Relation left, Relation right)
    {
        var selected = Group(left, right);
        if (!Intact() || !CoarseReadSurface.TryReadMask(left, input, out var a) ||
            !CoarseReadSurface.TryReadMask(right, input, out var b) || a == 0 || b == 0)
            return ReadWitness.Create(root, selected, nativeTable, new Placeholder(),
                new Relation(selected, nativeTable), nativeTable);
        var used = new List<IElement>();
        var result = 0;
        for (var i = 0; i < regions.Length; i++)
            if ((a & 1 << i) != 0)
                for (var j = 0; j < regions.Length; j++)
                    if ((b & 1 << j) != 0)
                    {
                        var index = i * regions.Length + j;
                        result |= masks[index]; used.Add(rows[index]);
                    }
        var union = Group(outputRegions.Where((_, index) => (result & 1 << index) != 0).Cast<IElement>().ToArray());
        return ReadWitness.Create(root, selected, output, union, Group(), Group(nativeTable, Group(used.ToArray())));
    }

    // This guards a derived cache against native edits, including edits reached
    // through a returned witness. It introduces no alternate arithmetic law.
    private bool Intact()
    {
        foreach (var site in relationSites)
            if (!ReferenceEquals(site.Site.Left, site.Left) || !ReferenceEquals(site.Site.Right, site.Right)) return false;
        foreach (var site in tickSites)
            if (site.Site.Ticks != site.Value) return false;
        return true;
    }

    private static Relation Group(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);
}
