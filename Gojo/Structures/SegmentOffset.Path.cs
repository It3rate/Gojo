using Gojo.Core;
using static Gojo.Structures.PlanarMetric;
using Scalar = Gojo.Core.ExactScalar;

namespace Gojo.Structures;

public static partial class SegmentOffset
{
    /// <summary>
    /// Reads group(ordered preparation requests, ordered frame -> support maps).
    /// Composes the existing exact bounded miters into an open path, then admits
    /// each span only when its remaining traversal is positive in source order.
    /// Result entries are admitted edges or open edge slots. Per-edge witnesses
    /// retain candidate topology, shared open joints and unfinished preparations.
    /// This does not certify closed regions or non-neighbour intersections.
    /// </summary>
    public static bool TryConstructPath(Relation request, out Relation witness)
    {
        var captured = Capture(request);
        if (!Fields(captured, 2, out var f) || !GroupFields(f[0], out var preparations) ||
            preparations.Length < 2 || !GroupFields(f[1], out var supports) ||
            preparations.Length != supports.Length) return Unresolved(request, out witness);

        var count = preparations.Length;
        var offsets = new Relation[count]; var sources = new Relation[count];
        for (var i = 0; i < count; i++)
        {
            if (!Fields(preparations[i], 2, out var p) || p[0] is not Relation offset ||
                !Fields(offset, 4, out var o) || o[0] is not Relation source ||
                !Span(p[1], out _, out var lo, out var hi) || lo != 0 || hi != 1 ||
                supports[i] is not Relation support ||
                !Span(support.Left, out var axis, out var a, out var b) || a == b ||
                !Span(support.Right, out var allowed, out var c, out var d) || c > d ||
                !axis.SameIdentityAs(allowed)) return Unresolved(request, out witness);
            offsets[i] = offset; sources[i] = source;
        }
        if (ReferenceEquals(sources[0].Left, sources[^1].Right)) return Unresolved(request, out witness);

        var prepared = new IElement[count]; var exact = new IElement[count];
        var exactEdges = new Relation?[count]; var partialEdges = new Relation?[count];
        for (var i = 0; i < count; i++)
        {
            var preparedOk = TryPrepare((Relation)preparations[i], out var preparation);
            prepared[i] = preparation;
            if (preparedOk)
            {
                Fields(PathResult(preparation), 4, out var result);
                partialEdges[i] = (Relation)result[3];
            }
            if (TryConstruct(offsets[i], out var read)) exactEdges[i] = (Relation)PathResult(read);
            exact[i] = read;
        }

        var corners = new IElement[count - 1]; var sites = new IElement[count + 1];
        sites[0] = exactEdges[0]?.Left ?? partialEdges[0]?.Left ?? new Placeholder();
        sites[^1] = exactEdges[^1]?.Right ?? partialEdges[^1]?.Right ?? new Placeholder();
        for (var i = 0; i < corners.Length; i++)
        {
            TryJoin(Group(offsets[i], offsets[i + 1], Group(supports[i], supports[i + 1])), out var corner);
            corners[i] = corner;
            var result = PathResult(corner);
            // A refused corner's result is one slot used by both candidate edges.
            // A coordinate candidate in its provenance is not an accepted joint.
            if (Fields(result, 2, out var joined)) sites[i + 1] = ((Relation)joined[0]).Right;
            else sites[i + 1] = result;
        }

        var results = new IElement[count]; var admissions = new IElement[count];
        var correspondence = new IElement[count]; var remainder = new List<IElement>();
        for (var i = 0; i < count; i++)
        {
            var candidate = new Relation(sites[i], sites[i + 1]);
            IElement start = new Placeholder(), end = new Placeholder();
            var admitted = false;
            try
            {
                if (exactEdges[i] is Relation offset && Fields(offsets[i], 4, out var fields) &&
                    Metric(fields[1], out var metric) && Complete(offset.Left, metric, out var a) &&
                    Complete(offset.Right, metric, out var b))
                {
                    var direction = Subtract(b, a); var lane = direction[0].IsZero ? 1 : 0;
                    var first = Scalar.Zero; var last = Scalar.Zero;
                    var firstAllowed = false; var lastAllowed = false;
                    if (Complete(candidate.Left, metric, out var left))
                    {
                        first = (left[lane] - a[lane]) / direction[lane];
                        firstAllowed = Parameter(supports[i], first, out start);
                    }
                    if (Complete(candidate.Right, metric, out var right))
                    {
                        last = (right[lane] - a[lane]) / direction[lane];
                        lastAllowed = Parameter(supports[i], last, out end);
                    }
                    admitted = firstAllowed && lastAllowed && first < last;
                }
            }
            catch (OverflowException) { /* Keep the candidate and stored partial samples below. */ }
            var traversal = new Relation(new Relation(start, end), supports[i]);
            var incident = new List<IElement>();
            if (i > 0) incident.Add(corners[i - 1]);
            if (i < corners.Length) incident.Add(corners[i]);
            results[i] = admitted ? candidate : new Placeholder();
            var admission = Observe(preparations[i], Group(exact[i], Group(incident.ToArray()), supports[i]),
                results[i], admitted ? [] : [candidate, traversal], Group(candidate, traversal, prepared[i]));
            admissions[i] = admission;
            if (!admitted) remainder.Add(admission);
            correspondence[i] = new Relation(sources[i], results[i]);
        }
        witness = Observe(request, captured, Group(results), remainder.ToArray(),
            Group(Group(prepared), Group(exact), Group(corners), Group(admissions), Group(correspondence)));
        return remainder.Count == 0;
    }

    private static IElement PathResult(IElement witness)
    { Fields(witness, 5, out var fields); return fields[2]; }
}
