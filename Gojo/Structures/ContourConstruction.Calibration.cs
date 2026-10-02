using Gojo.Core;
using static Gojo.Structures.PlanarMetric;
using Scalar = Gojo.Core.ExactScalar;

namespace Gojo.Structures;

public static partial class ContourConstruction
{
    /// <summary>
    /// Reads group(partition, metric, source -> centre, radius -> length span,
    /// centre -> direction point, centre -> side point). Selects the homogeneous
    /// flat metric-circle interpretation of the native whole. Constructs its
    /// four quarter landmarks, keeping their native subdivision and seam sites.
    /// No output rim coordinates, angle constants or renderer axes are supplied.
    /// Other fractions and irrational coordinates remain a later read contract.
    /// </summary>
    public static bool TryCalibrateQuarters(Relation request, out Relation witness)
    {
        var captured = Capture(request);
        if (!Fields(captured, 6, out var f) || !Partition(f[0], out var circle, out var fine, out var parts) ||
            parts % 4 != 0 || !Metric(f[1], out var metric) ||
            f[2] is not Relation anchor || !SameSite(anchor.Left, circle.Source) ||
            !Complete(anchor.Right, metric, out var centre) ||
            !Distance(f[3], metric, out var radius, out var length) ||
            !ReferenceEquals(radius, circle.Radius) || length <= Scalar.Zero ||
            f[4] is not Relation aim || !ReferenceEquals(aim.Left, anchor.Right) ||
            !Complete(aim.Right, metric, out var aimPoint) ||
            f[5] is not Relation side || !ReferenceEquals(side.Left, anchor.Right) ||
            !Complete(side.Right, metric, out var sidePoint)) return Unresolved(request, out witness);

        var direction = Subtract(aimPoint, centre);
        var reachSquared = Dot(direction, direction, metric);
        if (reachSquared <= Scalar.Zero ||
            !(length * length / reachSquared).TrySquareRoot(out var scale)) return Unresolved(request, out witness);
        var first = Scale(direction, scale);
        if (!Normal(first, Subtract(sidePoint, centre), length, metric, out var next))
            return Unresolved(request, out witness);
        Scalar[][] rays = [first, next, Scale(first, new Scalar(-1)), Scale(next, new Scalar(-1))];
        var markers = new IElement[4]; var bindings = new IElement[4]; var native = new IElement[4];
        var correspondences = new IElement[4];
        try
        {
            for (var i = 0; i < 4; i++)
            {
                var location = Group(f[0], Relation.Point(fine, i * (parts / 4)));
                LocateCaptured(location, location, out var read);
                Fields(read, 5, out var readFields); Fields(readFields[2], 4, out var result);
                markers[i] = result[0]; native[i] = read;
                var radial = new Relation(anchor.Right, Vector(Add(centre, rays[i]), metric));
                bindings[i] = new Relation(markers[i], radial);
                // Native radial attachment -> its calculated metric observation.
                correspondences[i] = new Relation(result[1], radial);
            }
            var edges = Enumerable.Range(0, 4).Select(i => (IElement)new Relation(markers[i], markers[(i + 1) % 4])).ToArray();
            var surface = Group(anchor, f[0], f[1], Group(bindings), Group(edges));
            witness = Observe(request, captured, surface, [], Group(Group(native), Group(correspondences),
                (length * length).Encode(metric.Square)));
            return true;
        }
        catch (OverflowException) { return Unresolved(request, out witness); }
    }
}
