using System.Globalization;
using System.Text;

namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>An after-the-fact observation, not stored on the construction.
/// Source and candidate positions index the supplied inspection catalogue.</summary>
public sealed record ConstructionObservation(int Source, ConstructionVisibility Visibility,
    bool Reverse, ConstructionClues Clues, int[] Candidates)
{
    public int KnownCount => Clues.Values.Count(value => value.HasValue);
    public bool RetainsSource => Candidates.Contains(Source);
}

/// <summary>Observation/export apparatus. ConstructionEvidence receives only
/// source surfaces and published clues. Captions and drawing points are used
/// after matching, for the reports and illustrative gallery.</summary>
public static class ConstructionSurvey
{
    public static ConstructionObservation[] Run(IReadOnlyList<ConstructionExample>? examples = null)
    {
        examples ??= ConstructionAtlas.All;
        var observations = new List<ConstructionObservation>();
        for (var source = 0; source < examples.Count; source++)
            foreach (var reverse in new[] { false, true })
            {
                var full = ConstructionEvidence.Read(examples[source].Surface, reverse);
                foreach (var visibility in Enum.GetValues<ConstructionVisibility>())
                {
                    var published = full.Publish(visibility);
                    observations.Add(new(source, visibility, reverse, published,
                        ConstructionEvidence.Match(published, examples, reverse)));
                }
            }
        return observations.ToArray();
    }

    public static void Export(string folder)
    {
        Directory.CreateDirectory(folder);
        var examples = ConstructionAtlas.All;
        var observations = Run(examples);
        using (var output = File.CreateText(Path.Combine(folder, "observations.csv")))
        {
            output.WriteLine("source,pattern,construction,visibility,view,known_clues,candidate_count,candidate_pattern_count,self_retained,candidate_ids,candidate_constructions,candidate_patterns");
            foreach (var row in observations)
            {
                var source = examples[row.Source];
                var patterns = row.Candidates.Select(i => examples[i].Pattern).Distinct().ToArray();
                output.WriteLine(Csv(row.Source + 1, source.Pattern, source.Method, row.Visibility,
                    row.Reverse ? "reverse" : "forward", row.KnownCount, row.Candidates.Length, patterns.Length,
                    row.RetainsSource, string.Join(";", row.Candidates.Select(i => i + 1)),
                    string.Join(";", row.Candidates.Select(i => examples[i].Pattern + " / " + examples[i].Method)),
                    string.Join(";", patterns)));
            }
        }
        using (var output = File.CreateText(Path.Combine(folder, "clues.csv")))
        {
            output.WriteLine(Csv(["source", "visibility", "view", "value_bits", "known_mask_hex", "payload_hex", .. ConstructionEvidence.Labels]));
            foreach (var row in observations)
                output.WriteLine(Csv([row.Source + 1, row.Visibility, row.Reverse ? "reverse" : "forward",
                    row.Clues.ValueBits, Convert.ToHexString(row.Clues.KnownBytes()), Convert.ToHexString(row.Clues.PackedBytes()),
                    .. row.Clues.Values.Select(value => (object?)value)]));
        }

        var artifactFolder = Path.Combine(folder, "artifacts");
        Directory.CreateDirectory(artifactFolder);
        var transportBytes = 0;
        using (var output = File.CreateText(Path.Combine(folder, "transport.csv")))
        {
            output.WriteLine("source,pattern,construction,artifact,bytes,uses,groups,forward_clues_preserved,reverse_clues_preserved,basis_transported");
            for (var i = 0; i < examples.Count; i++)
            {
                var example = examples[i]; var surface = example.Surface;
                var bytes = FamilyUseCodec.Encode(surface.Table, surface.Entry, surface.Uses);
                var restored = FamilyUseCodec.Decode(bytes);
                if (!bytes.AsSpan().SequenceEqual(FamilyUseCodec.Encode(restored.Table, restored.Entry, restored.Uses)))
                    throw new InvalidOperationException("Grouped-use transport changed its representation.");
                var decoded = new ConstructionSurface(restored.Table, restored.Entry, restored.Uses);
                foreach (var reverse in new[] { false, true })
                    if (!ConstructionEvidence.Read(surface with { Basis = null }, reverse).Values
                        .SequenceEqual(ConstructionEvidence.Read(decoded, reverse).Values))
                        throw new InvalidOperationException("Transport changed an observable clue of the family and its grouped uses.");
                var artifact = $"construction-{i + 1:00}.bin";
                File.WriteAllBytes(Path.Combine(artifactFolder, artifact), bytes);
                transportBytes += bytes.Length;
                output.WriteLine(Csv(i + 1, example.Pattern, example.Method, "artifacts/" + artifact, bytes.Length,
                    surface.Uses.UseCount, surface.Uses.GroupCount, true, true, false));
            }
        }
        File.WriteAllText(Path.Combine(folder, "gallery.svg"), Gallery(examples));
        var summary = Summary(examples, observations, transportBytes);
        File.WriteAllText(Path.Combine(folder, "summary.txt"), summary);
        Console.Write(summary);
    }

    private static string Summary(IReadOnlyList<ConstructionExample> examples,
        IReadOnlyList<ConstructionObservation> observations, int transportBytes)
    {
        var text = new StringBuilder();
        text.AppendLine($"{examples.Count} authored constructions across {examples.Select(e => e.Pattern).Distinct().Count()} pattern captions.");
        text.AppendLine($"{observations.Count} observations: five visibility selections, forward and reverse views; candidates use the same selected view.");
        text.AppendLine($"Self retained: {observations.Count(row => row.RetainsSource)}/{observations.Count}. Labels and drawing coordinates are not matching inputs.");
        foreach (var visibility in Enum.GetValues<ConstructionVisibility>())
        {
            var rows = observations.Where(row => row.Visibility == visibility).ToArray();
            var unique = rows.Count(row => row.Candidates.Length == 1);
            var samePattern = rows.Count(row => row.Candidates.All(i => examples[i].Pattern == examples[row.Source].Pattern));
            text.AppendLine($"{visibility}: {rows.Length} observations; candidates min/mean/max " +
                $"{rows.Min(row => row.Candidates.Length)}/{rows.Average(row => row.Candidates.Length).ToString("0.00", CultureInfo.InvariantCulture)}/{rows.Max(row => row.Candidates.Length)}; " +
                $"unique construction {unique}; candidates stay within the source pattern caption {samePattern}.");
        }
        text.AppendLine($"Transport: {examples.Count} grouped-use artifacts, {transportBytes} bytes total; decoded forward/reverse clues preserved in all {examples.Count * 2} checks.");
        text.AppendLine("The geometric basis is NOT transported. Transport comparisons explicitly remove Basis from the original and leave it absent in the decoded surface.");
        text.AppendLine("A candidate with an open claim remains compatible; these counts are neither recognition accuracy nor learned confidence. A shared outline is not a shared construction.");
        text.AppendLine("observations.csv contains candidates; clues.csv contains the actual published slots, availability masks and packed values (blank slots are unknown, not zero).");
        text.AppendLine("transport.csv references reproducible artifacts/ files. gallery.svg is an illustrative contact sheet; its positions and arrows add no proof.");
        return text.ToString();
    }

    private static string Csv(params object?[] fields) => string.Join(",", fields.Select(field =>
    {
        var value = Convert.ToString(field, CultureInfo.InvariantCulture) ?? "";
        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? '"' + value.Replace("\"", "\"\"") + '"' : value;
    }));

    private static string Gallery(IReadOnlyList<ConstructionExample> examples)
    {
        const int columns = 4, cellWidth = 330, cellHeight = 245;
        var height = (examples.Count + columns - 1) / columns * cellHeight + 45;
        var svg = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {columns * cellWidth} {height}\"><rect width=\"100%\" height=\"100%\" fill=\"#f7f5ee\"/><defs><marker id=\"arrow\" markerWidth=\"5\" markerHeight=\"5\" refX=\"4\" refY=\"2.5\" orient=\"auto\"><path d=\"M0,0 L5,2.5 L0,5\" fill=\"none\" stroke=\"context-stroke\"/></marker></defs>");
        string[] colors = ["#167c6c", "#a54c2d", "#514fa1", "#8a6720"];
        static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
        void Text(string content, double x, double y, int size = 12) => svg.Append($"<text x=\"{F(x)}\" y=\"{F(y)}\" font-family=\"sans-serif\" font-size=\"{size}\" fill=\"#213b3a\">{System.Security.SecurityElement.Escape(content)}</text>");
        Text("Illustrative layouts: arrows are grouped reads; positions and spacing are not measured evidence.", 16, 25, 15);
        for (var i = 0; i < examples.Count; i++)
        {
            var example = examples[i]; var s = example.Surface;
            var x = i % columns * cellWidth; var y = i / columns * cellHeight + 45;
            Text($"{i + 1}. {example.Pattern}", x + 14, y + 20, 15);
            Text(example.Method, x + 14, y + 39);
            var minX = example.Points.Min(p => p.X); var maxX = example.Points.Max(p => p.X);
            var minY = example.Points.Min(p => p.Y); var maxY = example.Points.Max(p => p.Y);
            var scale = Math.Min(230 / Math.Max(maxX - minX, 1), 140 / Math.Max(maxY - minY, 1));
            var points = example.Points.Select(p => new PenPoint(x + 165 + (p.X - (minX + maxX) / 2) * scale,
                y + 126 - (p.Y - (minY + maxY) / 2) * scale)).ToArray();
            foreach (var span in s.Table.ReadLinks(s.Entry))
            {
                var a = points[span.Start.Local]; var b = points[span.End.Local];
                svg.Append($"<path d=\"M{F(a.X)} {F(a.Y)} L{F(b.X)} {F(b.Y)}\" fill=\"none\" stroke=\"#c7d1ca\" stroke-width=\"3\"/>");
            }
            for (var group = 0; group < s.Uses.GroupCount; group++)
                foreach (var use in s.Uses.ReadGroup(s.Table, s.Entry, group))
                {
                    var a = points[use.Span.Start.Local]; var b = points[use.Span.End.Local];
                    var dx = b.X - a.X; var dy = b.Y - a.Y; var length = Math.Sqrt(dx * dx + dy * dy);
                    if (length == 0) continue;
                    var offset = (group - (s.Uses.GroupCount - 1) / 2.0) * 1.7;
                    svg.Append($"<path d=\"M{F(a.X + dx * 7 / length - dy * offset / length)} {F(a.Y + dy * 7 / length + dx * offset / length)} " +
                        $"L{F(b.X - dx * 7 / length - dy * offset / length)} {F(b.Y - dy * 7 / length + dx * offset / length)}\" fill=\"none\" stroke=\"{colors[group % colors.Length]}\" stroke-width=\"1.4\" marker-end=\"url(#arrow)\"/>");
                }
            foreach (var point in points) svg.Append($"<circle cx=\"{F(point.X)}\" cy=\"{F(point.Y)}\" r=\"2.8\" fill=\"#213b3a\"/>");
            Text($"{s.Uses.GroupCount} groups; {s.Table.Template.LinkCount} links; {(s.Basis is null ? "topology only" : "explicit construction basis")}", x + 14, y + 224, 11);
        }
        return svg.Append("</svg>").ToString();
    }
}
