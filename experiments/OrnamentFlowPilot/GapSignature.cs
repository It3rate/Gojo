using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Experiments.OrnamentFlowPilot;

internal sealed record GapCell(long X0, long X1, long Y0, long Y1);
internal sealed record GapRegion(int Id, GapCell[] Cells, decimal Area, bool Top, bool Bottom);
internal sealed record GapMouth(string Side, long Start, long End, int Region);
internal sealed record GapSpan(string Axis, long AcrossStart, long AcrossEnd, long Lower, long Upper,
    int[] LowerStrokes, int[] UpperStrokes, int Region)
{
    public long Width => Upper - Lower;
}
internal sealed record GapWidth(string Axis, long Width, long Coverage);
internal sealed record NearestGap(int Stroke, long? Width, int[] SpanIndices);
internal sealed record GapRead(long Pitch, long Bottom, long Top, GapRegion[] Regions, GapMouth[] Mouths,
    bool AlternatingMouths, GapSpan[] Spans, GapWidth[] Widths, NearestGap[] Nearest);

// The tracing supplies zero-width walls on a cylinder of one pitch, bounded at its
// lowest and highest vertices. Open rectangular cells connect across unobstructed
// sides. Cuts at the corners of the actual strokes suffice at any integer scale.
internal static class GapSignature
{
    private sealed record Stroke(long X0, long Y0, long X1, long Y1);
    private static long Phase(long x, long pitch) => ((x % pitch) + pitch) % pitch;

    internal static GapRead Read(IReadOnlyList<PathSite> bay)
    {
        ArgumentNullException.ThrowIfNull(bay);
        if (bay.Count is < 3 or > 16) throw new ArgumentException("Supply three through sixteen strokes.", nameof(bay));
        long x = 0, y = 0;
        var strokes = new List<Stroke>();
        foreach (var step in bay)
        {
            if ((step.X == 0) == (step.Y == 0)) throw new ArgumentException("Expected a nonzero cardinal reach.", nameof(bay));
            strokes.Add(new(x, y, x + step.X, y + step.Y));
            x += step.X;
            y += step.Y;
        }
        long pitch = x;
        if (pitch <= 0 || y != 0) throw new ArgumentException("A periodic bay must advance horizontally.", nameof(bay));
        long[] xs = strokes.Select(s => Phase(s.X0, pitch)).Append(pitch).Distinct().Order().ToArray();
        long[] ys = strokes.Select(s => s.Y0).Distinct().Order().ToArray();
        if (ys.Length == 1) return new(pitch, ys[0], ys[0], [], [], false, [], [],
            Enumerable.Range(0, bay.Count).Select(i => new NearestGap(i, null, [])).ToArray());
        int nx = xs.Length - 1, ny = ys.Length - 1;

        // Each side retains every incident source stroke, including collinear
        // retraces. A horizontal span crossing the seam can cover both end slabs.
        var vertical = new int[nx, ny];
        var horizontal = new int[nx, ny + 1];
        for (int k = 0; k < strokes.Count; k++)
        {
            var s = strokes[k];
            if (s.X0 == s.X1)
            {
                int col = Array.IndexOf(xs, Phase(s.X0, pitch));
                for (int j = 0; j < ny; j++)
                    if (Math.Min(s.Y0, s.Y1) <= ys[j] && ys[j] < Math.Max(s.Y0, s.Y1)) vertical[col, j] |= 1 << k;
            }
            else
            {
                int row = Array.IndexOf(ys, s.Y0);
                for (int i = 0; i < nx; i++)
                    if (Phase(xs[i] - Math.Min(s.X0, s.X1), pitch) < Math.Abs(s.X1 - s.X0)) horizontal[i, row] |= 1 << k;
            }
        }

        var labels = new int[nx, ny]; // Positive labels while flooding; exported ids are zero-based.
        var regions = new List<GapRegion>();
        for (int i = 0; i < nx; i++)
        for (int j = 0; j < ny; j++)
        {
            if (labels[i, j] != 0) continue;
            int id = regions.Count;
            var cells = new List<GapCell>();
            var pending = new Queue<(int Col, int Row)>();
            void Visit(int col, int row)
            {
                if (labels[col, row] != 0) return;
                labels[col, row] = id + 1;
                pending.Enqueue((col, row));
            }
            Visit(i, j);
            bool top = false, bottom = false;
            while (pending.TryDequeue(out var next))
            {
                var (col, row) = next;
                cells.Add(new(xs[col], xs[col + 1], ys[row], ys[row + 1]));
                int right = (col + 1) % nx;
                if (vertical[col, row] == 0) Visit((col + nx - 1) % nx, row);
                if (vertical[right, row] == 0) Visit(right, row);
                if (horizontal[col, row] == 0)
                {
                    if (row == 0) bottom = true;
                    else Visit(col, row - 1);
                }
                if (horizontal[col, row + 1] == 0)
                {
                    if (row + 1 == ny) top = true;
                    else Visit(col, row + 1);
                }
            }
            decimal area = cells.Sum(c => (decimal)(c.X1 - c.X0) * (c.Y1 - c.Y0));
            regions.Add(new(id, cells.ToArray(), area, top, bottom));
        }

        GapMouth[] mouths = [.. ReadMouths("bottom", 0, 0), .. ReadMouths("top", ny, ny - 1)];
        List<GapMouth> ReadMouths(string side, int bank, int adjacentRow)
        {
            var result = new List<GapMouth>();
            for (int i = 0; i < nx; i++)
            {
                if (horizontal[i, bank] != 0) continue;
                int id = labels[i, adjacentRow] - 1;
                if (result.Count > 0 && result[^1].End == xs[i] && result[^1].Region == id && vertical[i, adjacentRow] == 0)
                    result[^1] = result[^1] with { End = xs[i + 1] };
                else result.Add(new(side, xs[i], xs[i + 1], id));
            }
            if (result.Count > 1 && result[0].Start == 0 && result[^1].End == pitch &&
                result[0].Region == result[^1].Region && vertical[0, adjacentRow] == 0)
            {
                result[^1] = result[^1] with { End = pitch + result[0].End };
                result.RemoveAt(0);
            }
            return result;
        }

        var spans = new List<GapSpan>();
        int[] Refs(int mask) => Enumerable.Range(0, bay.Count).Where(k => (mask & (1 << k)) != 0).ToArray();
        for (int j = 0; j < ny; j++)
        {
            var walls = Enumerable.Range(0, nx).Where(i => vertical[i, j] != 0).ToArray();
            for (int k = 0; k < walls.Length; k++)
            {
                int left = walls[k], right = walls[(k + 1) % walls.Length];
                long end = right <= left ? xs[right] + pitch : xs[right];
                spans.Add(new("x", ys[j], ys[j + 1], xs[left], end, Refs(vertical[left, j]),
                    Refs(vertical[right, j]), labels[left, j] - 1));
            }
        }
        for (int i = 0; i < nx; i++)
        {
            var walls = Enumerable.Range(0, ny + 1).Where(j => horizontal[i, j] != 0).ToArray();
            for (int k = 1; k < walls.Length; k++)
            {
                int low = walls[k - 1], high = walls[k];
                spans.Add(new("y", xs[i], xs[i + 1], ys[low], ys[high], Refs(horizontal[i, low]),
                    Refs(horizontal[i, high]), labels[i, low] - 1));
            }
        }
        var widths = spans.GroupBy(s => (s.Axis, s.Width)).OrderBy(g => g.Key.Axis, StringComparer.Ordinal)
            .ThenBy(g => g.Key.Width).Select(g => new GapWidth(g.Key.Axis, g.Key.Width, g.Sum(s => s.AcrossEnd - s.AcrossStart))).ToArray();
        var nearest = Enumerable.Range(0, bay.Count).Select(stroke =>
        {
            int[] incident = Enumerable.Range(0, spans.Count)
                .Where(i => spans[i].LowerStrokes.Contains(stroke) || spans[i].UpperStrokes.Contains(stroke)).ToArray();
            long? width = incident.Select(i => (long?)spans[i].Width).Min();
            return new NearestGap(stroke, width, incident.Where(i => spans[i].Width == width).ToArray());
        }).ToArray();
        mouths = mouths.OrderBy(m => m.Start).ThenBy(m => m.End).ToArray();
        bool alternating = mouths.Length > 1;
        for (int k = 0; k < mouths.Length && alternating; k++)
        {
            var next = mouths[(k + 1) % mouths.Length];
            alternating = mouths[k].Side != next.Side && mouths[k].End <= next.Start + (k + 1 == mouths.Length ? pitch : 0);
        }
        return new(pitch, ys[0], ys[^1], regions.ToArray(), mouths, alternating, spans.ToArray(), widths, nearest);
    }
}
