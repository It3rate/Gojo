using System.Text.Json;
using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Experiments.OrnamentFlowPilot;

internal static class SymmetryStudy
{
    internal sealed record CapRead(int[] Outer, int[] Inner, int ScaleNumerator, int ScaleDenominator,
        PathSite[] RequiredInnerNumerators, bool Fits, bool CutsBothArms);

    // Select one witness from the exhaustive read for illustration. The scale
    // printed here goes from the smaller inner cap to the larger outer cap.
    internal static CapRead ReadCap(PathSite[] segments, FretSymmetryRead survey)
    {
        int[] outer = [15, 0, 1], inner = [1, 2, 3];
        var cut = survey.Matches.Single(m => m.Source == inner[0] && m.Target == outer[0] &&
            m.Frame == "half-turn" && !m.Reversed).Cut;
        int numerator = checked((int)cut.ScaleNumerator), denominator = checked((int)cut.ScaleDenominator);
        var required = outer.Select(i => new PathSite(-denominator * segments[i].X, -denominator * segments[i].Y)).ToArray();
        bool fits = cut.Run == 3 && Math.Abs(required[0].Y) <= cut.FirstLimitNumerator &&
            Math.Abs(required[2].Y) <= cut.LastLimitNumerator;
        bool bothArmsCut = new[] { 0, 2 }.All(i => Math.Abs(required[i].Y) < numerator * Math.Abs(segments[inner[i]].Y));
        return new(outer, inner, numerator, denominator, required, fits, fits && bothArmsCut);
    }

    internal static void Write(string output)
    {
        var independent = FretSymmetrySource.ReadDirect();
        var shared = FretSymmetrySource.ReadGlide();
        var segments = shared.Segments;
        if (!segments.SequenceEqual(independent.Segments)) throw new InvalidDataException("Nominal tracings differ.");
        var survey = FretSymmetry.Read(segments);
        var directChange = FretSymmetry.Read(FretSymmetrySource.Direct(2));
        var sharedChange = FretSymmetry.Read(FretSymmetrySource.Glide(2));
        var turns = segments.Select((from, i) =>
        {
            var to = segments[(i + 1) % segments.Length];
            long cross = (long)from.X * to.Y - (long)from.Y * to.X;
            return cross > 0 ? 'L' : cross < 0 ? 'R' : throw new InvalidDataException("Expected a corner.");
        });
        var scaledCuts = survey.Matches.Where(m => m.Cut.ScaleNumerator != m.Cut.ScaleDenominator).ToArray();
        var report = new
        {
            segments, turns = string.Concat(turns), independent, shared, cap = ReadCap(segments, survey),
            cuts = new { scaledAlignments = scaledCuts.Length, longestScaledWindow = scaledCuts.Select(m => m.Cut.Run).DefaultIfEmpty().Max() },
            coordinates = "Band offsets relative to the first upper-left vertex (0,0); the gallery places it at (0,4).",
            survey, changedReach = new { directChange.Pitch, independentBand = directChange.Band,
                sharedPitch = sharedChange.Pitch, sharedBand = sharedChange.Band }
        };
        var file = Path.GetFullPath(output);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true
        }));
        Console.WriteLine($"{survey.Period} strokes, pitch {survey.Pitch}, {survey.CandidatesChecked} alignments. " +
            $"{survey.Band.Length} band maps modulo pitch, {survey.Matches.Length} ordered direction matches of 3+ strokes, " +
            $"{survey.Matches.Count(m => m.EqualRun >= 3)} at equal size, " +
            $"{survey.Matches.Count(m => m.SimilarRun >= 3 && m.ScaleNumerator != m.ScaleDenominator)} at changed scale. " +
            $"Allowing end cuts gives {scaledCuts.Length} changed-scale alignments.");
    }
}
