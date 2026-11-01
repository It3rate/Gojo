using Gojo.Core;
using Scalar = Gojo.Core.ExactScalar;

namespace Gojo.Structures;

public static partial class ComplexFrame
{
    /// <summary>
    /// Reads group(constructed contour surface, coefficient coordinates).
    /// Four equal, orthogonal, opposed radii justify the generator's action.
    /// Coordinates measure coefficients along the first two rays, not screen
    /// x/y. The metric and native contour construction remain attached evidence.
    /// </summary>
    public static bool TryFromContour(Relation request, out Relation witness)
    {
        var captured = Capture(request);
        if (!Fields(captured, 2, out var f) || !Fields(f[0], 5, out var surface) ||
            !Coordinates(f[1], out var frame) || !Fields(surface[3], 4, out var bindings) ||
            bindings[0] is not Relation first || !PlanarMetric.Metric(surface[2], out var metric) ||
            !ContourRotation.TryTraverse(Group(f[0], first.Left, Relation.Segment(new Carrier(), 0, 1)), out var walk) ||
            !ContourRotation.TryTurn(walk, out var turn)) return Unresolved(request, captured, out witness);
        var rays = new Scalar[4][];
        for (var i = 0; i < 4; i++)
        {
            if (bindings[i] is not Relation { Right: Relation radial } ||
                !PlanarMetric.Complete(radial.Left, metric, out var centre) ||
                !PlanarMetric.Complete(radial.Right, metric, out var rim)) return Unresolved(request, captured, out witness);
            rays[i] = PlanarMetric.Subtract(rim, centre);
        }
        var reach = PlanarMetric.Dot(rays[0], rays[0], metric);
        if (reach <= Scalar.Zero || !PlanarMetric.Dot(rays[0], rays[1], metric).IsZero ||
            Enumerable.Range(0, 2).Any(i => Enumerable.Range(0, 2).Any(c => rays[i + 2][c] != -rays[i][c])))
            return Unresolved(request, captured, out witness);
        // Read the actual normalized coefficients of all rays. Equal reach and
        // perpendicularity above justify this calibration without angle fields.
        var states = rays.Select(ray => (IElement)Value(frame,
            [PlanarMetric.Dot(ray, rays[0], metric) / reach, PlanarMetric.Dot(ray, rays[1], metric) / reach])).ToArray();
        var maps = Group(Enumerable.Range(0, 4).Select(i => (IElement)new Relation(((Relation)bindings[i]).Left, states[i])).ToArray());
        var cycle = Group(Enumerable.Range(0, 4).Select(i => (IElement)new Relation(states[i], states[(i + 1) % 4])).ToArray());
        var basis = Group(f[1], cycle, Group(f[0], maps, walk, turn));
        if (!ReadBasis(basis, out _)) return Unresolved(request, captured, out witness);
        witness = Observe(request, captured, basis, [], maps); return true;
    }
}
