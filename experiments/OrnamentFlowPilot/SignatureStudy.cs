using System.Text.Json;
using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.OrnamentPlateStudy;

namespace Gojo.Experiments.OrnamentFlowPilot;

internal sealed record GapMap(string Frame, long OffsetX, long OffsetY, int?[] TargetRegions, decimal[][] AreaOverlaps)
{
    public bool CarriesRegions => TargetRegions.All(target => target.HasValue) &&
        TargetRegions.Distinct().Count() == TargetRegions.Length;
}
internal sealed record PatternSignature(string Id, string Name, string Source, PathSite[] Segments, CourseSignature Course,
    FretSymmetryRead Windows, JointSignatureRead Joints, GapRead Gaps, GapMap[] GapMaps, GapMap UnshiftedBankFlip);
internal sealed record HookResult(int Across, int Inset, int FirstDrop, int LastDrop, int SecondAcross,
    bool Glide, bool AlternatingMouths, bool UnitCorridors, bool ThreeShrinkingTurns, bool PairedPivots);

internal static class SignatureStudy
{
    // Map positive areas in the periodic window. A translated cell can cross
    // the chosen seam; aggregate overlaps on both sides before comparing regions.
    internal static GapMap MapGaps(GapRead gaps, BandSymmetry symmetry)
    {
        var frame = FretSymmetry.Frames.Single(f => f.Name == symmetry.Frame);
        var targets = new List<int?>();
        var overlaps = new List<decimal[]>();
        foreach (var source in gaps.Regions)
        {
            var areas = new decimal[gaps.Regions.Length];
            foreach (var cell in source.Cells)
            {
                var a = frame.Apply(cell.X0, cell.Y0);
                var b = frame.Apply(cell.X1, cell.Y1);
                long left = Math.Min(a.X, b.X) + symmetry.OffsetX, width = Math.Abs(a.X - b.X);
                long low = Math.Min(a.Y, b.Y) + symmetry.OffsetY, high = Math.Max(a.Y, b.Y) + symmetry.OffsetY;
                long phase = ((left % gaps.Pitch) + gaps.Pitch) % gaps.Pitch;
                foreach (var region in gaps.Regions)
                foreach (var target in region.Cells)
                {
                    long dy = Math.Max(0, Math.Min(high, target.Y1) - Math.Max(low, target.Y0));
                    if (dy == 0) continue;
                    long dx = Overlap(phase, phase + width, target.X0, target.X1) +
                        Overlap(phase - gaps.Pitch, phase + width - gaps.Pitch, target.X0, target.X1);
                    areas[region.Id] += (decimal)dx * dy;
                }
            }
            int id = Array.FindIndex(areas, area => area == source.Area);
            bool flipsBanks = frame.YY < 0;
            var partner = id < 0 ? null : gaps.Regions[id];
            bool corresponds = partner is not null && partner.Area == source.Area &&
                source.Top == (flipsBanks ? partner.Bottom : partner.Top) &&
                source.Bottom == (flipsBanks ? partner.Top : partner.Bottom);
            targets.Add(corresponds ? id : null);
            overlaps.Add(areas);
        }
        return new(symmetry.Frame, symmetry.OffsetX, symmetry.OffsetY, targets.ToArray(), overlaps.ToArray());
    }

    private static long Overlap(long a, long b, long c, long d) => Math.Max(0, Math.Min(b, d) - Math.Max(a, c));

    internal static PatternSignature Read(string id, string name, string source, PathSite[] segments,
        byte[]? stored = null, byte[]? emitted = null)
    {
        var (windows, joints) = JointSignature.ReadWithWindows(segments, stored, emitted);
        var gaps = GapSignature.Read(segments);
        var gapMaps = windows.Band.Select(b => MapGaps(gaps, b)).ToArray();
        if (gapMaps.Any(m => !m.CarriesRegions))
            throw new InvalidDataException("An ink symmetry failed its whole-region correspondence check.");
        var flip = new BandSymmetry("horizontal-mirror", 0, gaps.Top + gaps.Bottom, false, "reflection");
        return new(id, name, source, segments, CourseSignature.Read(segments), windows,
            joints, gaps, gapMaps, MapGaps(gaps, flip));
    }

    private static PatternSignature FromVoices(string id, string name, FretSourceRead voice) =>
        Read(id, name, "Packed Gojo horizontal and vertical motion strips", voice.Segments, voice.StoredWords, voice.EmittedWords);

    private static PatternSignature Meyer(int figure)
    {
        var program = PlateReads.Author(figure);
        var run = PathMachine.Run(program, new PathInput(4, 2, PlateReads.Period[figure], 2));
        if (!PlateReads.Measure(run, figure).ExactInk) throw new InvalidDataException("Historical control did not reproduce its reference.");
        var segments = run.Edges.Where(e => e.Cell == 0).Select(edge =>
        {
            var start = run.Sites[edge.From];
            var end = run.Sites[edge.To];
            return new PathSite(end.X - start.X, end.Y - start.Y);
        }).ToArray();
        return Read($"meyer{figure+1}", $"Meyer figure {figure+1}", $"Plate 81 path machine {Convert.ToHexString(program)}", segments);
    }

    private static HookResult[] Hunt()
    {
        var rows = new List<HookResult>();
        for (int inset = 1; inset <= 2; inset++)
        for (int clearance = 1; clearance <= 2; clearance++)
        for (int first = 1; first <= 3; first++)
        for (int last = 1; last <= 3; last++)
        for (int delta = 0; delta <= 1; delta++)
        {
            int across = 2 * inset + clearance;
            var bay = delta == 0 ? FretSymmetrySource.ReadHook(across, inset, first, last).Segments :
                FretSymmetrySource.ReadIndependentHook(across, inset, first, last, across + delta).Segments;
            var gaps = GapSignature.Read(bay);
            var (windows, joints) = JointSignature.ReadWithWindows(bay);
            bool glide = windows.Band.Any(s => s.Kind == "glide reflection");
            bool unit = gaps.Regions.Length == 2 && gaps.Mouths.Length == 2 &&
                gaps.Mouths.All(m => m.End - m.Start == 1) && gaps.Nearest.All(s => s.Width == 1);
            var pivots = joints.SharedEndQuarterRows;
            bool paired = pivots.Any(row => row.Gaps2 is [var a, var b, var c, var d] &&
                a != b && a == c && b == d);
            rows.Add(new(across, inset, first, last, across + delta, glide, gaps.AlternatingMouths, unit,
                CourseSignature.Read(bay).LongestContraction >= 3, paired));
        }
        return rows.ToArray();
    }

    internal static void Write(string output)
    {
        var patterns = new[]
        {
            FromVoices("interlock", "Interlocking fret", FretSymmetrySource.ReadGlide()),
            Meyer(0), Meyer(1),
            FromVoices("hook", "Invented hook pair", FretSymmetrySource.ReadHook(3, 1, 2, 2)),
            FromVoices("changed", "One independently stretched reach", FretSymmetrySource.ReadDirect(2)),
            FromVoices("hook-neighbor", "Hook with one-tick neighbors", FretSymmetrySource.ReadHook(5, 2, 2, 1)),
            FromVoices("hook-pivots", "Hook with paired pivots", FretSymmetrySource.ReadHook(5, 2, 2, 2))
        };
        var hunt = Hunt();
        var file = Path.GetFullPath(output);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, JsonSerializer.Serialize(new { patterns, hunt }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true
        }));
        Console.WriteLine($"Read {patterns.Length} signatures and {hunt.Length} invented settings. " +
            $"{hunt.Count(h => h.Glide && h.AlternatingMouths)} have glide and alternating mouths; " +
            $"{hunt.Count(h => h.Glide && h.AlternatingMouths && h.UnitCorridors)} also meet the unit-neighbor and mouth conditions.");
    }
}
