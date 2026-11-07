using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Experiments.OrnamentFlowPilot;

// Source and Target are indices of the first participating stroke (also when
// the target is traversed backwards). Every prefix of either run is witnessed.
internal sealed record FretMatch(int Source, int Target, bool Reversed, string Frame,
    long ScaleNumerator, long ScaleDenominator, int DirectionRun, int SimilarRun, FretCut Cut)
{
    public int EqualRun => ScaleNumerator == ScaleDenominator ? SimilarRun : 0;
}

// The first and last arms can end anywhere from an arbitrarily small positive
// extent through their stated upper limit. Divide each limit numerator by
// ScaleNumerator for source units, or ScaleDenominator for target units.
internal sealed record FretCut(long ScaleNumerator, long ScaleDenominator, int Run,
    long FirstLimitNumerator, long LastLimitNumerator);

// The affine map is frame(point) + offset, in coordinates relative to
// the first vertex. OffsetX is a representative modulo horizontal pitch.
internal sealed record BandSymmetry(string Frame, long OffsetX, long OffsetY, bool Reversed, string Kind);
internal sealed record FretSymmetryRead(int Period, long Pitch, long CandidatesChecked,
    FretMatch[] Matches, BandSymmetry[] Band);

internal static class FretSymmetry
{
    private readonly record struct Frame(string Name, int XX, int XY, int YX, int YY)
    {
        public (long X, long Y) Apply(long x, long y) => (XX * x + XY * y, YX * x + YY * y);
    }

    private static readonly Frame[] Frames =
    [
        new("identity", 1, 0, 0, 1), new("quarter-left", 0, -1, 1, 0),
        new("half-turn", -1, 0, 0, -1), new("quarter-right", 0, 1, -1, 0),
        new("vertical-mirror", -1, 0, 0, 1), new("horizontal-mirror", 1, 0, 0, -1),
        new("rising-diagonal", 0, 1, 1, 0), new("falling-diagonal", 0, -1, -1, 0)
    ];

    private static int Phase(long index, int period) => (int)((index % period + period) % period);

    private static long Gcd(long a, long b)
    {
        while (b != 0) (a, b) = (b, a % b);
        return a;
    }

    // Every matched window of three or more strokes has at least one whole
    // interior reach. Its ratio fixes the scale, even if both outer arms can
    // end partway along their respective carriers. To extend by another
    // stroke, today's last arm has to become tomorrow's whole interior reach.
    private static FretCut ReadCut(long[] lengths, int source, int target, int sense, int directions)
    {
        long SourceLength(int j) => lengths[Phase((long)source + j, lengths.Length)];
        long TargetLength(int j) => lengths[Phase(target + (long)sense * j, lengths.Length)];
        long numerator = TargetLength(1), denominator = SourceLength(1);
        long divisor = Gcd(numerator, denominator);
        numerator /= divisor;
        denominator /= divisor;
        int run = 3;
        while (run < directions && SourceLength(run - 1) * numerator == TargetLength(run - 1) * denominator)
            run++;
        long Limit(int j) => Math.Min(SourceLength(j) * numerator, TargetLength(j) * denominator);
        return new(numerator, denominator, run, Limit(0), Limit(run - 1));
    }

    /// <summary>Read a periodic, ordered strip of whole positive-length
    /// cardinal strokes. A witness fixes one frame, one traversal, and one
    /// source-to-target scale across its entire window. DirectionRun drops
    /// only the extents. All windows can cross the chosen bay boundary.</summary>
    internal static FretSymmetryRead Read(IReadOnlyList<PathSite> bay)
    {
        ArgumentNullException.ThrowIfNull(bay);
        int n = bay.Count;
        if (n is < 3 or > 16)
            throw new ArgumentException("Supply one bay of three through sixteen strokes.", nameof(bay));
        var vertices = new (long X, long Y)[n + 1];
        var lengths = new long[n];
        for (int i = 0; i < n; i++)
        {
            var v = bay[i];
            if ((v.X == 0) == (v.Y == 0))
                throw new ArgumentException("Each stroke must use exactly one cardinal axis.", nameof(bay));
            lengths[i] = Math.Abs((long)v.X) + Math.Abs((long)v.Y);
            vertices[i + 1] = (vertices[i].X + v.X, vertices[i].Y + v.Y);
        }
        long pitch = vertices[n].X;
        if (pitch <= 0 || vertices[n].Y != 0)
            throw new ArgumentException("A bay must advance along the positive horizontal axis.", nameof(bay));

        var matches = new List<FretMatch>();
        var band = new HashSet<BandSymmetry>();
        foreach (var frame in Frames)
        {
            var headings = bay.Select(v => frame.Apply(Math.Sign(v.X), Math.Sign(v.Y))).ToArray();
            for (int source = 0; source < n; source++)
            for (int target = 0; target < n; target++)
            foreach (int sense in Traversals)
            {
                long numerator = lengths[target], denominator = lengths[source];
                int directions = 0, similar = 0;
                for (int j = 0; j < n; j++)
                {
                    int from = Phase((long)source + j, n);
                    int to = Phase(target + (long)sense * j, n);
                    var v = bay[to];
                    if (headings[from] != (sense * Math.Sign(v.X), sense * Math.Sign(v.Y))) break;
                    directions++;
                    if (similar == j && lengths[to] * denominator == lengths[from] * numerator)
                        similar++;
                }
                if (directions < 3) continue;
                var divisor = Gcd(numerator, denominator);
                matches.Add(new(source, target, sense < 0, frame.Name, numerator / divisor, denominator / divisor,
                    directions, similar, ReadCut(lengths, source, target, sense, directions)));

                // A full unit-scale period maps all subsequent and preceding
                // bays: summing these vectors also maps pitch to sense*pitch.
                if (similar != n || numerator != denominator) continue;
                var anchor = vertices[target + (sense < 0 ? 1 : 0)];
                var origin = frame.Apply(vertices[source].X, vertices[source].Y);
                long tx = (anchor.X - origin.X) % pitch;
                if (tx < 0) tx += pitch;
                long ty = anchor.Y - origin.Y;
                string kind = frame.Name switch
                {
                    "identity" => tx == 0 && ty == 0 ? "identity" : "translation",
                    "horizontal-mirror" => tx == 0 ? "reflection" : "glide reflection",
                    "vertical-mirror" => ty == 0 ? "reflection" : "glide reflection",
                    _ => "rotation"
                };
                band.Add(new(frame.Name, tx, ty, sense < 0, kind));
            }
        }
        return new(n, pitch, checked(16L * n * n), matches.ToArray(),
            band.OrderBy(b => b.Frame, StringComparer.Ordinal).ThenBy(b => b.OffsetX).ThenBy(b => b.OffsetY)
                .ThenBy(b => b.Reversed).ToArray());
    }

    private static readonly int[] Traversals = [1, -1];
}
