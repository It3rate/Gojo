using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.OrnamentFlowPilot;

// Vertex numbers follow the authored walk, including its implicit previous bay.
// Words are optional source evidence. Geometry alone supplies no packed ODV word.
internal sealed record JointCorner(int Vertex, long X, long Y, PathSite Incoming, PathSite Outgoing,
    string Turn, long InLength, long OutLength, byte? RawIn, byte? RawOut, byte? EmittedIn, byte? EmittedOut);
internal readonly record struct JointPivot(long X2, long Y2);
internal sealed record JointMatch(int Source, int Target, string Frame, bool Reversed,
    long OffsetX, long OffsetY, JointPivot? Pivot, long FirstCut, long LastCut, bool EqualArms, bool ProportionalArms,
    bool? SameRawOdv, bool? SameRawBytes, bool? SameEmittedOdv, bool? SameEmittedBytes);
internal sealed record JointTip(int Source, int Target, string Frame, bool Reversed, bool IncomingArm,
    long Extent, long TargetBay, JointPivot Pivot, bool SamePathSite);
internal sealed record PivotRow(long Y2, long[] X2, long[] Gaps2);
internal sealed record JointSignatureRead(long Pitch, long CandidatesChecked, JointCorner[] Corners,
    JointMatch[] Matches, JointTip[] QuarterTips, PivotRow[] SharedEndQuarterRows, PivotRow[] WholeBandHalfTurnRows);

internal static class JointSignature
{
    private static long Length(PathSite v) => Math.Abs((long)v.X) + Math.Abs((long)v.Y);
    private static (long X, long Y) Heading(PathSite v) => (Math.Sign(v.X), Math.Sign(v.Y));

    // For a fixed point p of R*q + t, solve (I-R)p=t. Reflections have no
    // unique center. Coordinates are doubled to preserve half-tick centers.
    private static JointPivot? Center(string frame, long tx, long ty) => frame switch
    {
        "quarter-left" => new(tx - ty, tx + ty),
        "quarter-right" => new(tx + ty, ty - tx),
        "half-turn" => new(tx, ty),
        _ => null
    };

    private static bool? Literal(byte? a, byte? b, byte? c, byte? d, int mask)
    {
        if (a is null || b is null || c is null || d is null) return null;
        return (a & mask) == (c & mask) && (b & mask) == (d & mask);
    }

    private static PivotRow[] Rows(IEnumerable<JointPivot> pivots, long pitch)
    {
        long period = 2 * pitch;
        return pivots.GroupBy(p => p.Y2).OrderBy(group => group.Key).Select(group =>
        {
            long[] phases = group.Select(p => (p.X2 % period + period) % period).Distinct().Order().ToArray();
            long[] gaps = phases.Select((phase, i) => i + 1 == phases.Length
                ? phases[0] + period - phase : phases[i + 1] - phase).ToArray();
            return new PivotRow(group.Key, phases, gaps);
        }).ToArray();
    }

    // A common tip can lie on either arm, including a partial arm. To find
    // every target lift, solve a + u*r = b + (k*pitch,0) + u*R*r. The y
    // equation fixes u since r and R*r are perpendicular for quarter-turns.
    private static JointTip? CommonTip(JointCorner a, JointCorner b, JointMatch m,
        FretSymmetry.Frame frame, bool incoming, long pitch, int count)
    {
        var ray = Heading(incoming ? a.Incoming : a.Outgoing);
        if (incoming) ray = (-ray.X, -ray.Y);
        var other = frame.Apply(ray.X, ray.Y);
        long extent = (b.Y - a.Y) / (ray.Y - other.Y);
        if (extent <= 0 || extent > (incoming ? m.FirstCut : m.LastCut)) return null;
        long liftX = a.X - b.X + (ray.X - other.X) * extent;
        if (liftX % pitch != 0) return null;
        long targetBay = liftX / pitch;
        bool targetIncoming = incoming != m.Reversed;
        bool shared = extent == (incoming ? a.InLength : a.OutLength) &&
            extent == (targetIncoming ? b.InLength : b.OutLength) &&
            a.Vertex + (incoming ? -1 : 1) == b.Vertex + targetBay * count + (targetIncoming ? -1 : 1);
        var pivot = new JointPivot(2 * (a.X + ray.X * extent), 2 * (a.Y + ray.Y * extent));
        return new(a.Vertex, b.Vertex, frame.Name, m.Reversed, incoming, extent, targetBay, pivot, shared);
    }

    internal static JointSignatureRead Read(IReadOnlyList<PathSite> segments,
        IReadOnlyList<byte>? rawWords = null, IReadOnlyList<byte>? emittedWords = null) =>
        ReadWithWindows(segments, rawWords, emittedWords).Joints;

    internal static (FretSymmetryRead Windows, JointSignatureRead Joints) ReadWithWindows(IReadOnlyList<PathSite> segments,
        IReadOnlyList<byte>? rawWords = null, IReadOnlyList<byte>? emittedWords = null)
    {
        // The same reader validates the periodic cardinal basis and supplies
        // the globally qualified affine transforms for a separate pivot read.
        var band = FretSymmetry.Read(segments);
        int n = segments.Count;
        if (rawWords is not null && rawWords.Count != n || emittedWords is not null && emittedWords.Count != n)
            throw new ArgumentException("Supply one bound motion byte per stroke, or omit the byte view.");
        var corners = new List<JointCorner>();
        long x = 0, y = 0;
        for (int i = 0; i < n; i++)
        {
            int prev = (i + n - 1) % n;
            var from = segments[prev];
            var to = segments[i];
            if (emittedWords is not null)
            {
                var word = new BorderMove(emittedWords[i]);
                if (word.Axis != (to.Y == 0 ? 0 : 1) || word.Sense != (word.Axis == 0 ? Math.Sign(to.X) : Math.Sign(to.Y)))
                    throw new ArgumentException("The emitted words must observe the supplied motion in the same basis.");
            }
            long turn = (long)from.X * to.Y - (long)from.Y * to.X;
            if (turn != 0)
                corners.Add(new(i, x, y, from, to, turn > 0 ? "left" : "right", Length(from), Length(to),
                    rawWords?[prev], rawWords?[i], emittedWords?[prev], emittedWords?[i]));
            x += to.X; y += to.Y;
        }

        var matches = new List<JointMatch>();
        var tips = new List<JointTip>();
        foreach (var source in corners)
        foreach (var target in corners)
        foreach (var frame in FretSymmetry.Frames)
        foreach (var reversed in Traversals)
        {
            var first = Heading(source.Incoming);
            var last = Heading(source.Outgoing);
            var otherFirst = Heading(reversed ? target.Outgoing : target.Incoming);
            var otherLast = Heading(reversed ? target.Incoming : target.Outgoing);
            int sign = reversed ? -1 : 1;
            if (frame.Apply(first.X, first.Y) != (sign * otherFirst.X, sign * otherFirst.Y) ||
                frame.Apply(last.X, last.Y) != (sign * otherLast.X, sign * otherLast.Y)) continue;
            long firstLength = reversed ? target.OutLength : target.InLength;
            long lastLength = reversed ? target.InLength : target.OutLength;
            var mapped = frame.Apply(source.X, source.Y);
            long tx = target.X - mapped.X, ty = target.Y - mapped.Y;
            byte? rawFirst = reversed ? target.RawOut : target.RawIn;
            byte? rawLast = reversed ? target.RawIn : target.RawOut;
            byte? emittedFirst = reversed ? target.EmittedOut : target.EmittedIn;
            byte? emittedLast = reversed ? target.EmittedIn : target.EmittedOut;
            // Compare the actual bytes in arm correspondence order. Reading the
            // target backwards does not rewrite the target owner's stored D bit.
            var match = new JointMatch(source.Vertex, target.Vertex, frame.Name, reversed, tx, ty,
                Center(frame.Name, tx, ty), Math.Min(source.InLength, firstLength), Math.Min(source.OutLength, lastLength),
                source.InLength == firstLength && source.OutLength == lastLength,
                source.InLength * lastLength == source.OutLength * firstLength,
                Literal(source.RawIn, source.RawOut, rawFirst, rawLast, 7),
                Literal(source.RawIn, source.RawOut, rawFirst, rawLast, 255),
                Literal(source.EmittedIn, source.EmittedOut, emittedFirst, emittedLast, 7),
                Literal(source.EmittedIn, source.EmittedOut, emittedFirst, emittedLast, 255));
            matches.Add(match);
            if (frame.Name is "quarter-left" or "quarter-right")
            {
                foreach (var incoming in Traversals)
                    if (CommonTip(source, target, match, frame, incoming, band.Pitch, n) is { } tip) tips.Add(tip);
            }
        }

        // A whole-band half-turn has center t/2. The next target lift adds
        // pitch/2 to its x coordinate, producing two centers per full pitch.
        var global = band.Band.Where(b => b.Frame == "half-turn").SelectMany(b => new[]
        {
            new JointPivot(b.OffsetX, b.OffsetY), new JointPivot(b.OffsetX + band.Pitch, b.OffsetY)
        });
        return (band, new(band.Pitch, 16L * corners.Count * corners.Count, corners.ToArray(), matches.ToArray(), tips.ToArray(),
            Rows(tips.Where(t => t.SamePathSite).Select(t => t.Pivot), band.Pitch), Rows(global, band.Pitch)));
    }

    private static readonly bool[] Traversals = [false, true];
}
