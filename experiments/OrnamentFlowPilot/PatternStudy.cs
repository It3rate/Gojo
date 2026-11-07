using System.Text.Json;
using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.OrnamentPlateStudy;
using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.OrnamentFlowPilot;

internal sealed record SignatureFeature(string Key, string Family, string Label, string Value);
internal sealed record StudyPattern(string Id, string Name, string Source, string Scope, PathSite[] Segments,
    CourseSignature Course, FretSymmetryRead Windows, JointSignatureRead Joints, GapRead Gaps, GapMap[] GapMaps,
    PatternScorecardRead Score, PatternScorecardRead RepeatScore, SignatureFeature[] Features, string[] Dominant,
    FretSourceRead? SourceEncoding);
internal sealed record StudyContrast(string Id, string Name, string Parent, string Change, PatternScorecardRead Score,
    PatternScorecardRead RepeatScore, PatternCheck[] Checks, string Interpretation);
internal sealed record SignatureOverlap(string First, string Second, string[] Shared, string[] Different);

internal static class PatternStudy
{
    private static string Yes(bool value) => value ? "yes" : "no";

    private static SignatureFeature[] Features(PatternSignature p, PatternScorecardRead material)
    {
        var gaps = p.Gaps;
        var sides = gaps.Regions.Count(r => r.Top || r.Bottom);
        long? nearest = gaps.Nearest.FirstOrDefault()?.Width;
        bool sameNearest = nearest.HasValue && gaps.Nearest.All(r => r.Width == nearest);
        return
        [
            new("connected", "material", "One native material course", Yes(material.NativeComponents == 1)),
            new("handoffs", "material", "All adjacent strokes share a site", Yes(material.Handoffs.Misses == 0)),
            new("overdraw", "material", "Positive-length collinear overlaps", material.CollinearOverlaps.ToString()),
            new("contacts", "material", "Contacts without a native join", material.UnjoinedContacts.ToString()),
            new("glide", "symmetry", "Whole-band glide", Yes(p.Windows.Band.Any(b => b.Kind == "glide reflection"))),
            new("mirror", "symmetry", "Vertical mirror", Yes(p.Windows.Band.Any(b => b.Frame == "vertical-mirror"))),
            new("half", "symmetry", "Half-turn", Yes(p.Windows.Band.Any(b => b.Frame == "half-turn"))),
            new("hand-runs", "sequence", "Same-hand run lengths", string.Join(" / ", p.Course.TurnRuns.Select(r => r.Turns).Order())),
            new("shrink", "sequence", "Strict shrinking turns", p.Course.LongestContraction.ToString()),
            new("length-reuse", "sequence", "Reach word repeats within the period", Yes(p.Course.LengthPeriod < p.Segments.Length)),
            new("pivot-rows", "joints", "Shared quarter-tip rows", p.Joints.SharedEndQuarterRows.Length.ToString()),
            new("paired-pivots", "joints", "Four pivots with alternating unequal gaps", Yes(p.Joints.SharedEndQuarterRows.Any(r =>
                r.Gaps2 is [var a, var b, var c, var d] && a != b && a == c && b == d))),
            new("regions", "space", "Open white regions", sides.ToString()),
            new("sealed", "space", "Enclosed white regions", gaps.Regions.Count(r => !r.Top && !r.Bottom).ToString()),
            new("mouths", "space", "Top / bottom mouths alternate", Yes(gaps.AlternatingMouths)),
            new("nearest", "space", "Every stroke has the same nearest gap", Yes(sameNearest)),
            new("mouth-neighbor", "space", "Every mouth equals that nearest gap", Yes(sameNearest && gaps.Mouths.Length > 0 &&
                gaps.Mouths.All(m => m.End - m.Start == nearest))),
            new("area", "space", "Open regions have equal area", Yes(sides > 1 &&
                gaps.Regions.Where(r => r.Top || r.Bottom).Select(r => r.Area).Distinct().Count() == 1))
        ];
    }

    private static StudyPattern Read(string id, string name, string source, string scope, PathSite[] segments,
        FretSourceRead? encoding = null)
    {
        var p = SignatureStudy.Read(id, name, source, segments, encoding?.StoredWords, encoding?.EmittedWords);
        var score = PatternScorecard.ReadCourse(segments);
        var repeated = PatternScorecard.ReadRepeatedCourse(segments);
        var dominant = new List<string>();
        var glide = p.Windows.Band.FirstOrDefault(b => b.Frame == "horizontal-mirror" && b.Kind == "glide reflection");
        if (glide is not null)
            dominant.Add($"The full course follows its top-to-bottom reflection after {glide.OffsetX} ticks across.");
        int turns = p.Course.TurnRuns.Max(r => r.Turns);
        dominant.Add($"The longest turn run has {turns} consecutive turns toward the same side.");
        if (p.Course.LongestContraction > 0)
            dominant.Add($"A reach sequence contracts through {p.Course.LongestContraction} same-hand turns.");
        if (p.Course.LengthPeriod < segments.Length)
            dominant.Add($"The lengths repeat after {p.Course.LengthPeriod} strokes inside a {segments.Length}-stroke directed period.");
        if (p.Gaps.AlternatingMouths)
            dominant.Add($"The {p.Gaps.Mouths.Length} openings alternate between top and bottom.");
        return new(id, name, source, scope, segments, p.Course, p.Windows, p.Joints, p.Gaps, p.GapMaps,
            score, repeated, Features(p, repeated), dominant.ToArray(), encoding);
    }

    private static StudyPattern FromReference(ReferencePattern p) =>
        Read(p.Id, p.Name, p.Source, p.Scope, p.Encoding.Segments, p.Encoding);

    private static StudyPattern Meyer(int figure)
    {
        var program = PlateReads.Author(figure);
        var trace = PathMachine.Run(program, new PathInput(4, 2, PlateReads.Period[figure], 2));
        if (!PlateReads.Measure(trace, figure).ExactInk) throw new InvalidDataException("Historical control changed.");
        var vectors = trace.Edges.Where(e => e.Cell == 0).Select(e => new PathSite(
            trace.Sites[e.To].X - trace.Sites[e.From].X, trace.Sites[e.To].Y - trace.Sites[e.From].Y)).ToArray();
        return Read($"meyer{figure + 1}", $"Meyer figure {figure + 1}", "Earlier study of Meyer, plate 81",
            "A supplied historical centerline control. Extents use its printed grid; both printed banks and framing rules are outside this one-pen read.", vectors);
    }

    private static StudyContrast Contrast(string id, string name, StudyPattern parent, string change,
        PathSite[] vectors, byte[]? mask, string interpretation, params string[] goals)
    {
        var selected = mask is null ? null : new ParticipationStrip(mask);
        var repeatedMask = mask is null ? null : new ParticipationStrip([.. mask, .. mask]);
        var score = PatternScorecard.ReadCourse(vectors, selected);
        var repeated = PatternScorecard.ReadRepeatedCourse(vectors, deposit: repeatedMask);
        var checks = goals.SelectMany(goal => PatternScorecard.Evaluate(
            goal is "no-overdraw" or "handoffs" ? repeated : score, goal)).ToArray();
        return new(id, name, parent.Id, change, score, repeated, checks, interpretation);
    }

    private static StudyContrast[] Contrasts(StudyPattern interlock, StudyPattern key)
    {
        var middle = Enumerable.Repeat((byte)1, interlock.Segments.Length).ToArray();
        middle[5] = 0;
        var seam = Enumerable.Repeat((byte)1, interlock.Segments.Length).ToArray();
        seam[^1] = 0;
        var retraced = key.Segments.ToList();
        var mark = retraced[2];
        retraced.InsertRange(3, [new PathSite(-mark.X, -mark.Y), mark]);
        return
        [
            Contrast("middle-gap", "One missing deposit", interlock, "Switch off deposition at the sixth stroke, preserving the movement sites and later marks.",
                interlock.Segments, middle, "A broken material path needs a gap-aware completion read. Whole-course descriptors remain unavailable.",
                "participation", "handoffs", "seam"),
            Contrast("seam-gap", "One missing repeat lead", interlock, "Switch off the final stroke in each repeat.",
                interlock.Segments, seam, "The intended connection to the next repeat now has missing ink.",
                "participation", "handoffs", "seam"),
            Contrast("overdraw", "A traced-back stroke", key, "After the third reach, return along it and trace it forward again before continuing.",
                retraced.ToArray(), null, "The visible support is unchanged in this centerline read while the same length has been deposited three times.",
                "participation", "handoffs", "seam", "no-overdraw"),
            Contrast("changed-reach", "One independently stretched reach", interlock, "Increase only one inner horizontal reach from one tick to two.",
                FretSymmetrySource.Direct(2), null, "The turn word survives. The former gap promises now fail, and new white pockets are sealed.",
                "seam", "no-overdraw", "common-nearest-gap", "alternating-mouths", "no-sealed-regions", "glide"),
            Contrast("scale-control", "The whole fret doubled", interlock, "Multiply every bound reach by two.",
                interlock.Segments.Select(s => new PathSite(2*s.X, 2*s.Y)).ToArray(), null,
                "A positive control. The realized size changes while its relative construction promises survive.",
                "seam", "no-overdraw", "common-nearest-gap", "alternating-mouths", "no-sealed-regions", "glide"),
            Contrast("mirror-control", "The whole fret reflected", interlock, "Exchange the vertical frame for its opposite.",
                interlock.Segments.Select(s => new PathSite(s.X, -s.Y)).ToArray(), null,
                "A positive control. Left and right exchange while the paired structure and white passages survive.",
                "seam", "no-overdraw", "common-nearest-gap", "alternating-mouths", "no-sealed-regions", "glide")
        ];
    }

    internal static void Write(string output)
    {
        var interlock = FromReference(new("interlock", "Interlocking fret", "Attachment 3, right row 3",
            "The existing 16-reach single-pen tracing. Same idealized family as the earlier annotated corner example.",
            FretSymmetrySource.ReadGlide()));
        StudyPattern[] patterns = [interlock, .. ReferencePatterns.Read().Select(FromReference), Meyer(0), Meyer(1)];
        var contrasts = Contrasts(interlock, patterns.Single(p => p.Id == "running-key"));
        var overlap = new List<SignatureOverlap>();
        for (int i = 0; i < patterns.Length; i++)
        for (int j = i + 1; j < patterns.Length; j++)
        {
            var other = patterns[j].Features.ToDictionary(f => f.Key);
            overlap.Add(new(patterns[i].Id, patterns[j].Id,
                patterns[i].Features.Where(f => f.Value == other[f.Key].Value).Select(f => f.Key).ToArray(),
                patterns[i].Features.Where(f => f.Value != other[f.Key].Value).Select(f => f.Key).ToArray()));
        }
        var file = Path.GetFullPath(output);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, JsonSerializer.Serialize(new
        {
            patterns, contrasts, selection = ReferencePatterns.Selections(), overlaps = overlap,
            scope = "Idealized cardinal courses with connected Gojo move sources. Complete descriptors use one periodic bay; material continuity and collisions use two repeats. Scorecard features overlap and are not independent votes for beauty."
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true }));
        Console.WriteLine($"Recorded {patterns.Length} references, {contrasts.Length} controlled changes and {overlap.Count} pairwise signature comparisons.");
    }
}
