using Gojo.Core;
using Gojo.Equations;

namespace Gojo.Structures;

/// <summary>
/// Experimental finite region arithmetic. A publication exposes a native union
/// of admissible regions; bit masks are inspection/execution projections only.
/// Difference is a calibrated additive law, not arithmetic on region indices.
/// </summary>
public static class CoarseReadSurface
{
    // G(whole frame, G(closed region frames)). Integer regions must exhaust the
    // declared whole without gaps or overlap. The small range is a fixture law.
    public static Relation Partition(Carrier root, Carrier axis, params (int Low, int High)[] regions) =>
        Relation.OrderedGroup(root,
            Relation.Segment(axis, regions.Length == 0 ? 0 : regions[0].Low,
                regions.Length == 0 ? 0 : regions[^1].High),
            Group(regions.Select(region => (IElement)Relation.Segment(axis, region.Low, region.High)).ToArray()));

    // G(input partition, output partition, oriented unit frame, additive equation).
    // The orientation is a frame, separate from the signs in the result regions.
    public static Relation DifferenceLaw(Carrier root, Relation inputPartition,
        Relation outputPartition, int orientation = 1)
    {
        var axis = TryReadPartition(inputPartition, out var inputAxis, out _) ? inputAxis : Carrier.Unknown;
        return Relation.OrderedGroup(root, inputPartition, outputPartition,
            Relation.Segment(axis, 0, orientation), Equation.Sum(axis,
                Equation.WeightedPort(axis, 0, 1, 0, orientation),
                Equation.WeightedPort(axis, 0, 1, 0, -orientation)));
    }

    /// <summary>External instrument adapter. It retains only membership, never
    /// the fine input coordinate. Out-of-domain input remains an open read.</summary>
    public static Relation Measure(Carrier root, Relation partition, int value, IElement source)
    {
        if (!TryReadPartition(partition, out _, out var regions)) return Unknown(root, partition, source);
        for (var i = 0; i < regions.Length; i++)
            if (Span(regions[i], out _, out var low, out var high) && value >= low && value <= high)
                return Publish(root, partition, 1 << i, source);
        return Unknown(root, partition, source);
    }

    /// <summary>Publishes a native union. Mask zero is an empty admissible set,
    /// not a numeric zero. A numeric zero occupies its own declared region.</summary>
    public static Relation Publish(Carrier root, Relation partition, int mask, IElement source)
    {
        if (!TryReadPartition(partition, out _, out var regions) || mask < 0 || mask >= 1 << regions.Length)
            return Unknown(root, partition, source);
        return ReadWitness.Create(root, source, partition, Union(regions, mask), Group(), source);
    }

    public static Relation Unknown(Carrier root, Relation partition, IElement source) =>
        ReadWitness.Create(root, source, partition, new Placeholder(), new Relation(source, partition), source);

    public static bool TryReadPartition(Relation partition, out Carrier axis, out Relation[] regions)
    {
        axis = null!; regions = [];
        if (!partition.TryReadOrderedGroup(out var fields) || fields.Length != 2 ||
            fields[0] is not Relation whole || !Span(whole, out var basis, out var minimum, out var maximum) ||
            basis.IsUnknown || minimum > maximum || minimum < -4096 || maximum > 4096 ||
            fields[1] is not Relation group || !group.TryReadOrderedGroup(out var parts) || parts.Length is < 1 or > 8)
            return false;
        var next = minimum;
        var ordered = new Relation[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i] is not Relation region || !Span(region, out var regionAxis, out var low, out var high) ||
                !regionAxis.SameIdentityAs(basis) || low != next || high < low || high > maximum) return false;
            ordered[i] = region; next = high + 1;
        }
        if (next != maximum + 1) return false;
        axis = basis; regions = ordered; return true;
    }

    public static bool TryReadLaw(Relation law, out Relation input, out Relation output, out int orientation)
    {
        input = output = null!; orientation = 0;
        if (!law.TryReadOrderedGroup(out var fields) || fields.Length != 4 ||
            fields[0] is not Relation from || fields[1] is not Relation to ||
            !TryReadPartition(from, out var axis, out var inputRegions) ||
            !TryReadPartition(to, out var outputAxis, out var outputRegions) || !axis.SameIdentityAs(outputAxis) ||
            fields[2] is not Relation frame || !Span(frame, out var orientedAxis, out var origin, out var forward) ||
            !axis.SameIdentityAs(orientedAxis) || origin != 0 || forward is not (-1 or 1) ||
            fields[3] is not Relation equation || !DifferenceEquation(equation, axis, forward)) return false;
        Span(inputRegions[0], out _, out var inputLow, out _);
        Span(inputRegions[^1], out _, out _, out var inputHigh);
        Span(outputRegions[0], out _, out var outputLow, out _);
        Span(outputRegions[^1], out _, out _, out var outputHigh);
        var width = inputHigh - inputLow;
        if (outputLow > -width || outputHigh < width) return false;
        input = from; output = to; orientation = forward; return true;
    }

    public static bool TryReadMask(Relation publication, Relation partition, out int mask)
    {
        mask = 0;
        if (!TryReadPartition(partition, out _, out var regions) ||
            !ReadWitness.TryRead(publication, out _, out var surface, out var result, out _, out _) ||
            surface is not Relation basis || !basis.Equals(partition) ||
            result is not Relation union || !ReadUnion(union, regions, out var selected)) return false;
        mask = selected; return true;
    }

    /// <summary>Computes the conservative image of each selected region pair.
    /// Bounds are at most 4096 ticks, so all finite subtraction stays in int.
    /// Disconnected alternatives are kept separate; no convex hull is imposed.</summary>
    public static Relation DifferenceComputed(Carrier root, Relation law, Relation left, Relation right)
    {
        var selected = Group(left, right);
        if (!TryReadLaw(law, out var input, out var output, out var orientation) ||
            !TryReadPartition(input, out _, out var inputs) || !TryReadPartition(output, out _, out var outputs) ||
            !TryReadMask(left, input, out var a) || !TryReadMask(right, input, out var b) || a == 0 || b == 0)
            return Open(root, selected, law);
        var evidence = new List<IElement>();
        var result = 0;
        for (var i = 0; i < inputs.Length; i++)
            if ((a & 1 << i) != 0)
                for (var j = 0; j < inputs.Length; j++)
                    if ((b & 1 << j) != 0)
                    {
                        var pairResult = PairMask(inputs[i], inputs[j], outputs, orientation);
                        result |= pairResult;
                        evidence.Add(new Relation(Group(inputs[i], inputs[j]), Union(outputs, pairResult)));
                    }
        return ReadWitness.Create(root, selected, output, Union(outputs, result), Group(), Group(law, Group(evidence.ToArray())));
    }

    /// <summary>Captures the finite law and materializes native region-pair
    /// correspondences once. Lookup rejects later drift in its source law.</summary>
    public static Relation CompileDifference(Carrier root, Relation law)
    {
        if (!TryReadLaw(law, out _, out _, out _)) return Open(root, law, law);
        var snapshot = (Relation)new ExposedStructureSnapshot().Copy(law);
        TryReadLaw(snapshot, out var input, out var output, out var orientation);
        TryReadPartition(input, out _, out var inputs); TryReadPartition(output, out _, out var outputs);
        var rows = new List<IElement>();
        foreach (var a in inputs)
            foreach (var b in inputs)
                rows.Add(new Relation(Group(a, b), Union(outputs, PairMask(a, b, outputs, orientation))));
        return Relation.OrderedGroup(root, law, snapshot, Group(rows.ToArray()));
    }

    public static bool TryReadTable(Relation table, out Relation sourceLaw, out Relation capturedLaw, out Relation[] rows)
    {
        sourceLaw = capturedLaw = null!; rows = [];
        if (!table.TryReadOrderedGroup(out var fields) || fields.Length != 3 ||
            fields[0] is not Relation source || fields[1] is not Relation captured ||
            !source.Equals(captured) || !TryReadLaw(captured, out var input, out var output, out var orientation) ||
            !TryReadPartition(input, out _, out var inputs) || !TryReadPartition(output, out _, out var outputs) ||
            fields[2] is not Relation group || !group.TryReadOrderedGroup(out var entries) || entries.Length != inputs.Length * inputs.Length)
            return false;
        var ordered = new Relation[entries.Length];
        for (var n = 0; n < entries.Length; n++)
            if (entries[n] is not Relation row || row.Left is not Relation pair ||
                !pair.TryReadOrderedGroup(out var operands) || operands.Length != 2 ||
                !inputs[n / inputs.Length].Equals(operands[0]) || !inputs[n % inputs.Length].Equals(operands[1]) ||
                row.Right is not Relation union || !ReadUnion(union, outputs, out var mask) || mask == 0 ||
                mask != PairMask(inputs[n / inputs.Length], inputs[n % inputs.Length], outputs, orientation))
                return false;
            else ordered[n] = row;
        sourceLaw = source; capturedLaw = captured; rows = ordered; return true;
    }

    /// <summary>Validates and combines precomputed native rows. No private source
    /// magnitude is queried. The selected rows remain in provenance.</summary>
    public static Relation DifferenceLookup(Carrier root, Relation table, Relation left, Relation right)
    {
        var selected = Group(left, right);
        if (!TryReadTable(table, out _, out var law, out var rows) ||
            !TryReadLaw(law, out var input, out var output, out _) ||
            !TryReadPartition(input, out _, out var inputs) || !TryReadPartition(output, out _, out var outputs) ||
            !TryReadMask(left, input, out var a) || !TryReadMask(right, input, out var b) || a == 0 || b == 0)
            return Open(root, selected, table);
        var used = new List<IElement>();
        var result = 0;
        for (var i = 0; i < inputs.Length; i++)
            if ((a & 1 << i) != 0)
                for (var j = 0; j < inputs.Length; j++)
                    if ((b & 1 << j) != 0)
                    {
                        var row = rows[i * inputs.Length + j];
                        ReadUnion((Relation)row.Right, outputs, out var mask);
                        result |= mask; used.Add(row);
                    }
        return ReadWitness.Create(root, selected, output, Union(outputs, result), Group(), Group(table, Group(used.ToArray())));
    }

    private static int PairMask(Relation a, Relation b, Relation[] output, int orientation)
    {
        Span(a, out _, out var aLow, out var aHigh); Span(b, out _, out var bLow, out var bHigh);
        var low = orientation == 1 ? aLow - bHigh : bLow - aHigh;
        var high = orientation == 1 ? aHigh - bLow : bHigh - aLow;
        var result = 0;
        for (var i = 0; i < output.Length; i++)
        {
            Span(output[i], out _, out var from, out var to);
            if (low <= to && high >= from) result |= 1 << i;
        }
        return result;
    }

    private static bool ReadUnion(Relation union, Relation[] regions, out int mask)
    {
        mask = 0;
        if (!union.TryReadOrderedGroup(out var parts)) return false;
        var previous = -1;
        foreach (var part in parts)
        {
            var index = Array.FindIndex(regions, region => region.Equals(part));
            if (index <= previous) return false;
            previous = index; mask |= 1 << index;
        }
        return true;
    }

    private static bool DifferenceEquation(Relation equation, Carrier axis, int orientation) =>
        Point(equation.Right, out var result) && result.Carrier.SameIdentityAs(axis) && result.Ticks == 1 &&
        equation.Left is Relation operands && Weighted(operands.Left, axis, orientation) && Weighted(operands.Right, axis, -orientation);

    private static bool Weighted(IElement element, Carrier expectedAxis, int coefficient)
    {
        if (element is not Relation port || port.Left is not Relation input || port.Right is not Relation output ||
            !Span(input, out var axis, out var zero, out var one) || axis.IsUnknown || !axis.SameIdentityAs(expectedAxis) || zero != 0 || one != 1 ||
            !Span(output, out var target, out var start, out var end) || !target.IsUnknown || start != 0 || end != coefficient)
            return false;
        return true;
    }

    private static bool Span(Relation span, out Carrier axis, out int start, out int end)
    {
        if (Point(span.Left, out var a) && Point(span.Right, out var b) && a.Carrier.SameIdentityAs(b.Carrier) &&
            a.Ticks is >= -4096 and <= 4096 && b.Ticks is >= -4096 and <= 4096)
        { axis = a.Carrier; start = (int)a.Ticks; end = (int)b.Ticks; return true; }
        axis = null!; start = end = 0; return false;
    }

    private static bool Point(IElement element, out CarrierLocation location)
    {
        if (element is CarrierLocation direct && direct.Carrier is not null && direct.Tick is not null)
        { location = direct; return true; }
        if (element is Relation { Left: CarrierLocation point, Right: Carrier carrier } &&
            point.Carrier is not null && point.Tick is not null && point.Carrier.SameIdentityAs(carrier))
        { location = point; return true; }
        location = default; return false;
    }

    private static Relation Union(Relation[] regions, int mask) =>
        Group(regions.Where((_, index) => (mask & 1 << index) != 0).Cast<IElement>().ToArray());
    private static Relation Open(Carrier root, IElement selected, IElement attempted) =>
        ReadWitness.Create(root, selected, attempted, new Placeholder(), new Relation(selected, attempted), attempted);
    private static Relation Group(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);
}
