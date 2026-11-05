using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private FamilyTable? familyTable;
    private int familyExample = -1;

    private ExplorerPage FamilyPage()
    {
        var p = new ExplorerPage("One template, many instances",
            "Connections and constant faces are stored once. Each instance contains only the value bits it owns.", "JoinedFamilies.md")
        {
            DrawingGuide = ("Read one instance through its shared template",
                "Arrows show the selected order of endpoint roles, not moving material. Reverse the view without changing the stored bits. Equal spacing is only a drawing choice.")
        };
        Choose(p, "Family", FamilyExamples.Names.ToArray(), Mode, n => Mode = n, true);
        Choose(p, "Value width", ["1 bit", "5 bits"], Control, n => Control = n);
        Choose(p, "Instance", ["First", "Second", "Zero"], Variant, n => Variant = n);
        Choose(p, "View", ["Forward", "Reverse"], A, n => A = n);
        if (familyTable is null || familyExample != Mode)
        {
            familyTable = FamilyExamples.Build(Mode);
            familyExample = Mode;
        }
        var table = familyTable;
        var template = table.Template;
        var width = Control == 0 ? 1 : 5;
        var entry = new FamilyEntry(width, Variant);
        var reverse = A == 1;
        var spans = table.ReadLinks(entry, reverse);

        p.Rows("Constant / owned field positions", "Each row is [O group, D group]. O: 0 right, 1 left, 2 owned. Under fixed O, D: 0 positive, 1 negative, 2 owned.", 2,
            Enumerable.Range(0, template.FieldCount).Select(i => { var f = template.Field(i); return new[] { f.OrientationGroup, f.DirectionGroup }; }).ToArray(), "template",
            roleKeys: Enumerable.Range(0, template.FieldCount).Select(i => new[] { "field" + i, "field" + i }).ToArray(), actual: template.FieldBytes());
        p.Rows("Template connections", "[Start site, end site, value field]. A repeated site is one joint within an instance; a repeated field is one stored amount.", template.ReferenceBits,
            Enumerable.Range(0, template.LinkCount).Select(i => { var l = template.Link(i); return new[] { l.Start, l.End, l.Field }; }).ToArray(), "links",
            roleKeys: Enumerable.Range(0, template.LinkCount).Select(i => { var l = template.Link(i); return new[] { "site" + l.Start, "site" + l.End, "field" + l.Field }; }).ToArray(),
            actual: template.LinkBytes(), rowKeys: Enumerable.Range(0, template.LinkCount).Select(i => "link" + i).ToArray());
        foreach (var groupWidth in table.Widths)
        {
            var marks = new List<BitMark>();
            var offset = 0;
            for (var row = 0; row < table.Count(groupWidth); row++)
            for (var field = 0; field < template.FieldCount; field++)
            {
                var bits = groupWidth + template.Field(field).OwnedBits;
                var selected = groupWidth == width && row == Variant;
                var key = selected ? "field" + field : $"entry{groupWidth}.{row}";
                var keys = new List<string> { key };
                if (selected)
                    keys.AddRange(Enumerable.Range(0, template.LinkCount).Where(i => template.Link(i).Field == field).Select(i => "link" + i));
                marks.Add(new(offset, bits, keys.ToArray(), $"Instance {row}, field {field}: {groupWidth} value bits, then its owned D/O bits."));
                offset += bits;
            }
            p.Bits($"{groupWidth}-bit values · packed instances", $"{table.Count(groupWidth)} instances; {table.RowBits(groupWidth)} bits per instance. Zero slots are real entries; trailing padding is not another entry.",
                table.PackedBytes(groupWidth), checked((int)table.PayloadBitLength(groupWidth)), "payload" + groupWidth, marks.ToArray());
        }

        p.Label($"Instance {Variant} · {width}-bit value group · {(reverse ? "reverse" : "forward")} read", 350, 45);
        if (template.LinkCount == 0)
        {
            p.Node("field0", "One shared amount", "There are no endpoint rows. This value has participation and direction but does not describe a bounded link.", 350, 165, (table.ReadCoarse(entry, 0) & 1) != 0);
            var positive = table.ReadSide(entry, 0, false);
            var negative = table.ReadSide(entry, 0, true);
            p.Node("positive", "+ read · " + Word(positive), "Observation of the positive face of this one stored amount.", 190, 295, (positive & 1) != 0);
            p.Node("negative", "− read · " + Word(negative), "Observation of the negative face of this one stored amount.", 510, 295, (negative & 1) != 0);
            p.Bits("Positive-side observation", "A derived read, not a second owned amount.", [positive], 3, "positive", new BitMark(0, 3, ["positive", "field0"], "Positive side of field 0"));
            p.Bits("Negative-side observation", "At most one side participates. At zero both side reads are empty, even when D is retained.", [negative], 3, "negative", new BitMark(0, 3, ["negative", "field0"], "Negative side of field 0"));
            p.Label("No endpoint claim; reversal has no links to reverse.", 350, 375);
        }
        else
        {
            var points = FamilyPositions(Mode);
            for (var i = 0; i < spans.Length; i++)
            {
                var span = spans[i];
                var from = points[span.Start.Local]; var to = points[span.End.Local];
                var dx = to.X - from.X; var dy = to.Y - from.Y;
                var length = Math.Sqrt(dx * dx + dy * dy);
                var original = reverse ? spans.Length - 1 - i : i;
                p.Edge("link" + original, $"{span.Start.Local} → {span.End.Local} · f{span.Value.Field}",
                    $"Ordered endpoint roles {span.Start.Local} → {span.End.Local}; both directions use value field {span.Value.Field}. No distance or turn angle is supplied.",
                    from.X + dx * 16 / length, from.Y + dy * 16 / length,
                    to.X - dx * 16 / length, to.Y - dy * 16 / length, (table.ReadCoarse(entry, span.Value.Field) & 1) != 0);
            }
            for (var i = 0; i < points.Length; i++)
            {
                p.Circle("site" + i, $"Local site {i} of instance {Variant}. The same index in another instance is a different site.", points[i].X, points[i].Y, 10);
                p.Label("s" + i, points[i].X, points[i].Y - 26);
            }
            for (var field = 0; field < template.FieldCount; field++)
                p.Node("field" + field, $"f{field} · {Word(table.ReadCoarse(entry, field))}",
                    $"Stored value role {field}. Repeated connection references read this one amount. The displayed ODV word is a coarse observation.",
                    350 + (field - (template.FieldCount - 1) / 2.0) * 160, 410, (table.ReadCoarse(entry, field) & 1) != 0);
        }
        var closed = table.IsClosedWalk(entry, reverse);
        p.Result = template.LinkCount == 0 ? "One signed amount; no endpoints. The two side observations share one store." :
            $"{template.LinkCount} connections use {template.FieldCount} value {(template.FieldCount == 1 ? "field" : "fields")}. " +
            (closed ? "Ordered return is closed." : "This row sequence is not a closed walk.");
        p.Events.Add($"Both width groups share this template. The selected instance occupies {table.RowBits(width)} bits; the other group is separate storage, not an automatic refinement.");
        p.Events.Add("Reversing a read preserves all template and payload bytes. It changes endpoint order and row traversal, not sign or physical flow.");
        if (Mode == 4) p.Events.Add("All four fields may read nonzero, but they remain independent amounts. The equal-looking drawing does not establish equal lengths.");
        if (Mode == 5) p.Events.Add("All four connections refer to f0. That is shared state, not four equal-looking independent values or four copied reservoirs.");
        if (Variant == 2) p.Events.Add("Zero participation preserves the connection sites and any topological return.");
        p.Boundary = "Encoding and observation only. Shared tips do not establish a common whole; hubs do not establish equal angles; cycles do not establish squares. Calibration and material settlement need further connected laws.";
        return p;
    }

    private static (double X, double Y)[] FamilyPositions(int example) => example switch
    {
        1 => [(350, 220), (110, 220), (590, 220)],
        2 => [(110, 220), (590, 220), (350, 220)],
        3 => [(350, 225), (350, 100), (140, 320), (560, 320)],
        _ => [(160, 110), (540, 110), (540, 310), (160, 310)]
    };
}
