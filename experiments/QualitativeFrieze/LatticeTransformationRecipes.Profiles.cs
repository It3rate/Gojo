namespace Gojo.Experiments.QualitativeFrieze;

public sealed partial class LatticeTransformationRecipes
{
    private static V[] Profile(int operation, int length, int depth)
    {
        var quarter = Math.Max(1, length / 4); var fifth = Math.Max(1, length / 5);
        var sixth = Math.Max(1, length / 6);
        V[] points = operation switch
        {
            0 => [new(0, 0), new(quarter, 0), new(quarter, depth), new(length - quarter, depth), new(length - quarter, 0), new(length, 0)],
            1 => [new(0, 0), new(quarter, 0), new(length / 2, depth), new(length - quarter, 0), new(length, 0)],
            2 => [new(0, 0), new(sixth, 0), new(2 * sixth, depth), new(3 * sixth, 0), new(4 * sixth, depth), new(5 * sixth, 0), new(length, 0)],
            3 => [new(0, 0), new(quarter, depth), new(length / 2, -depth), new(length - quarter, depth), new(length, 0)],
            4 => [new(0, 0), new(fifth, 0), new(fifth, depth), new(2 * fifth, depth), new(2 * fifth, 2 * depth),
                new(length - 2 * fifth, 2 * depth), new(length - 2 * fifth, depth), new(length - fifth, depth), new(length - fifth, 0), new(length, 0)],
            5 => [new(0, 0), new(quarter, 0), new(quarter, depth), new(quarter - 1, depth), new(quarter - 1, 2 * depth),
                new(length - quarter + 1, 2 * depth), new(length - quarter + 1, depth), new(length - quarter, depth), new(length - quarter, 0), new(length, 0)],
            6 => [new(0, 0), new(quarter, depth), new(length - quarter, depth), new(length, 0)],
            7 => [new(0, 0), new(quarter, 0), new(quarter, -depth), new(length - quarter, -depth), new(length - quarter, 0), new(length, 0)],
            8 => [new(0, 0), new(fifth, 0), new(fifth, -depth), new(2 * fifth, -depth), new(2 * fifth, depth),
                new(length - fifth, depth), new(length - fifth, 0), new(length, 0)],
            9 => [new(0, 0), new(quarter, depth), new(length / 2, depth), new(length / 2, -depth), new(length - quarter, -depth), new(length, 0)],
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
        return points.Where((point, index) => index == 0 || point != points[index - 1]).ToArray();
    }

    private static (Segment First, Segment Second, V Start, V End)? FindFusionPair(Segment[] segments)
    {
        (Segment First, Segment Second, V Start, V End)? selected = null;
        var best = double.NegativeInfinity;
        for (var firstIndex = 0; firstIndex < segments.Length; firstIndex++)
        for (var secondIndex = firstIndex + 1; secondIndex < segments.Length; secondIndex++)
        {
            var first = segments[firstIndex]; var second = segments[secondIndex];
            if (ReferenceEquals(first.Path, second.Path)) continue;
            var fv = first.Vector; var sv = second.Vector;
            if (fv == default || sv == default || Cross(fv, sv) != 0 || Cross(fv, second.Start - first.Start) == 0) continue;
            var count = Gcd(fv.X, fv.Y); var canonical = Canonical(fv); var tangent = new V(canonical.X / count, canonical.Y / count);
            // This authoring search selects exact integral candidate landmarks.
            // More general rational intersections remain outside this fixture.
            if (Math.Abs(tangent.X) > 1 || Math.Abs(tangent.Y) > 1) continue;
            var xAxis = tangent.X != 0;
            int At(V point) => xAxis ? point.X : point.Y;
            var a = At(first.Start) < At(first.End) ? first.Start : first.End;
            var b = At(first.Start) < At(first.End) ? first.End : first.Start;
            var c = At(second.Start) < At(second.End) ? second.Start : second.End;
            var d = At(second.Start) < At(second.End) ? second.End : second.Start;
            var lower = Math.Max(At(a), At(c)); var upper = Math.Min(At(b), At(d));
            var span = upper - lower; if (span < 4) continue;
            var inset = Math.Max(1, span / 4); var startAt = lower + inset; var endAt = upper - inset;
            V On(V start, V end, int position)
            {
                var total = At(end) - At(start); var steps = position - At(start);
                return new(start.X + (end.X - start.X) * steps / total, start.Y + (end.Y - start.Y) * steps / total);
            }
            var startSum = On(a, b, startAt) + On(c, d, startAt); var endSum = On(a, b, endAt) + On(c, d, endAt);
            if ((startSum.X & 1) != 0 || (startSum.Y & 1) != 0 || (endSum.X & 1) != 0 || (endSum.Y & 1) != 0) continue;
            var start = new V(startSum.X / 2, startSum.Y / 2); var end = new V(endSum.X / 2, endSum.Y / 2);
            if (start == end) continue;
            if (WouldRetrace(first, start, end) || WouldRetrace(second, start, end)) continue;
            var separation = Math.Abs((long)Cross(tangent, c - a));
            var score = span - separation * .5;
            if (score <= best) continue;
            best = score; selected = (first, second, start, end);
        }
        return selected;

        bool WouldRetrace(Segment selectedChord, V sharedStart, V sharedEnd)
        {
            var forward = Canonical(selectedChord.Vector) == selectedChord.Vector;
            var orderedStart = forward ? sharedStart : sharedEnd; var orderedEnd = forward ? sharedEnd : sharedStart;
            var replacement = new[] { (A: selectedChord.Start, B: orderedStart), (A: orderedStart, B: orderedEnd), (A: orderedEnd, B: selectedChord.End) }
                .Where(edge => edge.A != edge.B).ToArray();
            var retained = segments.Where(segment => ReferenceEquals(segment.Path, selectedChord.Path) && !ReferenceEquals(segment.Chord, selectedChord.Chord))
                .Select(segment => (A: segment.Start, B: segment.End)).ToArray();
            foreach (var edge in replacement) foreach (var old in retained) if (Overlaps(edge.A, edge.B, old.A, old.B)) return true;
            for (var first = 0; first < replacement.Length; first++) for (var second = first + 1; second < replacement.Length; second++)
                if (Overlaps(replacement[first].A, replacement[first].B, replacement[second].A, replacement[second].B)) return true;
            return false;
        }
    }

    private static long Cross(V first, V second) => (long)first.X * second.Y - (long)first.Y * second.X;
    private static bool Overlaps(V a, V b, V c, V d)
    {
        if (a == b || c == d || Cross(b - a, d - c) != 0 || Cross(b - a, c - a) != 0) return false;
        return a.X != b.X ? Math.Max(Math.Min(a.X, b.X), Math.Min(c.X, d.X)) < Math.Min(Math.Max(a.X, b.X), Math.Max(c.X, d.X))
            : Math.Max(Math.Min(a.Y, b.Y), Math.Min(c.Y, d.Y)) < Math.Min(Math.Max(a.Y, b.Y), Math.Max(c.Y, d.Y));
    }

    private static Gojo.Core.Relation? PathOverlap(UnitArrangementProgram unit, LatticeLadderAuthor author)
    {
        foreach (var path in Segments(unit, author).GroupBy(segment => segment.Path, FieldNative.Participants.Instance))
        {
            var segments = path.ToArray();
            for (var first = 0; first < segments.Length; first++) for (var second = first + 1; second < segments.Length; second++)
                if (Overlaps(segments[first].Start, segments[first].End, segments[second].Start, segments[second].End))
                    return new(path.Key, new Gojo.Core.Relation(segments[first].Chord, segments[second].Chord));
        }
        return null;
    }
}
