using System.Numerics;
using Gojo.Core;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

public sealed partial class InteriorCellPartition
{
    /// <summary>Experimental descriptive threshold, separate from partition
    /// validity: a tooth's returning crosspiece is below a quarter of 4A/P.</summary>
    public const double FineReturnScaleFraction = .25;
    private static (InteriorCellShapeSummary Summary, Relation Witness) ShapeOf(InteriorCellGroupRead group,
        FormalPoint[] vertices, Dictionary<IElement, FormalPoint> positions, Dictionary<IElement, Relation> bindings)
    {
        var corners = new List<int>(); var straight = new List<IElement>();
        for (var i = 0; i < vertices.Length; i++)
        {
            var before = vertices[(i + vertices.Length - 1) % vertices.Length]; var here = vertices[i]; var after = vertices[(i + 1) % vertices.Length];
            if (Cross(before, here, after) == 0 && Dot(here.X - before.X, here.Y - before.Y, after.X - here.X, after.Y - here.Y) > 0)
                straight.Add(new Relation(group.BoundaryDarts[(i + vertices.Length - 1) % vertices.Length], group.BoundaryDarts[i]));
            else corners.Add(i);
        }
        var points = corners.Select(i => vertices[i]).ToArray(); var sides = points.Length;
        var area2 = Area2(points); var area = (double)BigInteger.Abs(area2) / 2;
        var lengths = Enumerable.Range(0, sides).Select(i => Distance(points[i], points[(i + 1) % sides])).ToArray();
        var perimeter = lengths.Sum(); var scale = perimeter == 0 ? 0 : 4 * area / perimeter;
        var convex = new List<IElement>(); var reflex = new List<IElement>(); var returns = new List<IElement>();
        var turnSigns = new int[sides]; var headings = new (long X, long Y)[sides]; var headingWeights = new Dictionary<(long, long), double>();
        for (var i = 0; i < sides; i++)
        {
            var previous = (i + sides - 1) % sides; var next = (i + 1) % sides;
            turnSigns[i] = Cross(points[previous], points[i], points[next]).Sign;
            var cornerWitness = new Relation(group.BoundaryDarts[(corners[i] + vertices.Length - 1) % vertices.Length], group.BoundaryDarts[corners[i]]);
            if (turnSigns[i] == area2.Sign) convex.Add(cornerWitness); else reflex.Add(cornerWitness);
            var dx = points[next].X - points[i].X; var dy = points[next].Y - points[i].Y; var gcd = Gcd(Math.Abs(dx), Math.Abs(dy));
            headings[i] = (dx / gcd, dy / gcd);
            headingWeights[headings[i]] = headingWeights.GetValueOrDefault(headings[i]) + lengths[i];
        }
        for (var i = 0; i < sides; i++)
        {
            var next = (i + 1) % sides; var after = (i + 2) % sides;
            // A small return along one persistent heading is evidence for an
            // edge tooth. Rotating star arms are not automatically equivalent:
            // their successive outer headings change around the interior.
            if (headings[i] == headings[after] && turnSigns[next] == -turnSigns[after] &&
                turnSigns[next] != 0 && lengths[next] < scale * FineReturnScaleFraction)
                returns.Add(new Relation(group.BoundaryDarts[corners[i]], new Relation(group.BoundaryDarts[corners[next]], group.BoundaryDarts[corners[after]])));
        }
        var alternations = Enumerable.Range(0, sides).Count(i => turnSigns[i] * turnSigns[(i + 1) % sides] < 0);
        var undirected = headings.Select(h => h.X < 0 || h.X == 0 && h.Y < 0 ? (-h.X, -h.Y) : h).Distinct().Count();
        var minX = points.Min(p => p.X); var maxX = points.Max(p => p.X); var minY = points.Min(p => p.Y); var maxY = points.Max(p => p.Y);
        var spanX = maxX - minX; var spanY = maxY - minY; var largest = Math.Max(spanX, spanY); var smallest = Math.Min(spanX, spanY);
        var sortedLengths = lengths.Order().ToArray(); var median = sortedLengths.Length % 2 == 1 ? sortedLengths[sortedLengths.Length / 2] :
            (sortedLengths[sortedLengths.Length / 2 - 1] + sortedLengths[sortedLengths.Length / 2]) / 2;
        var summary = new InteriorCellShapeSummary(group.Atoms.Count, vertices.Length, sides, convex.Count, reflex.Count,
            checked((long)BigInteger.Abs(area2)), area, perimeter, scale, 4 * Math.PI * area / (perimeter * perimeter),
            smallest == 0 ? double.PositiveInfinity : (double)largest / smallest,
            lengths.Min() / scale, median / scale, lengths.Max() / lengths.Min(),
            returns.Count, (double)returns.Count / sides, (double)alternations / sides,
            undirected, headingWeights.Values.Max() / perimeter);
        var positionEvidence = N.S(group.BoundaryDarts.Select(d => (IElement)bindings[((Relation)d.Right).Left]).ToArray());
        // Every count is a derived projection over these actual inventories.
        // Euclidean ratios select the displayed formal common-reach embedding;
        // they do not change the topology gate or declare a universal aesthetic.
        var witness = new Relation(new Relation(group.Boundary, positionEvidence),
            new Relation(N.S(straight.ToArray()), new Relation(N.S(convex.ToArray()), new Relation(N.S(reflex.ToArray()), N.S(returns.ToArray())))));
        return (summary, witness);
    }
    private static BigInteger Dot(long ax, long ay, long bx, long by) => (BigInteger)ax * bx + (BigInteger)ay * by;
    private static double Distance(FormalPoint a, FormalPoint b) => Math.Sqrt(Math.Pow((double)b.X - a.X, 2) + Math.Pow((double)b.Y - a.Y, 2));
    private static long Gcd(long a, long b) { while (b != 0) (a, b) = (b, a % b); return a; }
}

/// <summary>
/// Descriptive preferences in one supplied formal embedding, never hard laws
/// of beauty. Atomic ownership, exact area accounting and simple boundaries are
/// checked separately. Formal distance units are uncalibrated common reaches.
/// </summary>
public sealed record InteriorCellShapeSummary(int AtomicFaces, int AuthoredBoundaryDarts, int Sides,
    int ConvexCorners, int ReflexCorners, long FormalDoubleArea, double Area, double Perimeter,
    double InteriorScale, double Compactness, double AspectRatio, double MinimumSideToInteriorScale,
    double MedianSideToInteriorScale, double BoundaryScaleVariation, int FineToothReturns,
    double FineToothRatio, double TurnAlternationRatio, int DirectionFamilies, double DominantHeadingFraction);
