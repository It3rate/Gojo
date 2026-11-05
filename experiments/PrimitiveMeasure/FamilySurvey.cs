namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>After-the-fact inspection/export. Labels and transport byte counts
/// are presentation, not inputs to the family reader.</summary>
public static class FamilySurvey
{
    public static void Export(string folder)
    {
        Directory.CreateDirectory(folder);
        using var rows = File.CreateText(Path.Combine(folder, "family-rows.csv"));
        using var sizes = File.CreateText(Path.Combine(folder, "family-sizes.csv"));
        rows.WriteLine("family,value_bits,entry,row_bits,raw_fields,coarse_words,closed_walk,reverse_closed_walk");
        sizes.WriteLine("family,field_bits,link_bits,payload_bits,template_bytes,payload_bytes,transport_bytes");
        for (var example = 0; example < FamilyExamples.Names.Count(); example++)
        {
            var table = FamilyExamples.Build(example);
            var encoded = FamilyCodec.Encode(table);
            var restored = FamilyCodec.Decode(encoded);
            if (!encoded.AsSpan().SequenceEqual(FamilyCodec.Encode(restored)))
                throw new InvalidOperationException("Family transport changed its own representation.");
            File.WriteAllBytes(Path.Combine(folder, $"family-{example + 1}.bin"), encoded);
            var shape = table.Template;
            var payloadBits = table.Widths.Sum(table.PayloadBitLength);
            var payloadBytes = table.Widths.Sum(w => table.PackedBytes(w).Length);
            var templateBytes = shape.FieldBytes().Length + shape.LinkBytes().Length;
            sizes.WriteLine($"{example + 1},{shape.FieldCount * 4},{shape.LinkCount * 3 * shape.ReferenceBits},{payloadBits},{templateBytes},{payloadBytes},{encoded.Length}");
            foreach (var width in restored.Widths)
                for (var i = 0; i < restored.Count(width); i++)
                {
                    var entry = new FamilyEntry(width, i);
                    var raw = string.Join(";", Enumerable.Range(0, shape.FieldCount).Select(f => restored.ReadRaw(entry, f)));
                    var coarse = string.Join(";", Enumerable.Range(0, shape.FieldCount).Select(f => Convert.ToString(restored.ReadCoarse(entry, f), 2).PadLeft(3, '0')));
                    rows.WriteLine($"{example + 1},{width},{i},{restored.RowBits(width)},{raw},{coarse},{restored.IsClosedWalk(entry)},{restored.IsClosedWalk(entry, true)}");
                }
            Console.WriteLine($"{example + 1}. {FamilyExamples.Names[example]}: {shape.FieldCount} value roles, {shape.LinkCount} links; " +
                $"{payloadBits} payload bits, {encoded.Length} transport bytes; restored without its builder.");
        }
    }
}
