using Gojo.Core;

namespace Gojo.Structures;

public static partial class AffineFrame
{
    /// <summary>
    /// Reads group(motif, exposed points, ordered calibrations, target basis).
    /// Only explicitly exposed point references are projected. Relation order,
    /// repeated subgraphs, shared coordinate ticks and unselected structure survive.
    /// The witness provenance contains source-to-result clauses and per-point reads.
    /// Success covers the exposed points; it does not assert complete motif exposure.
    /// </summary>
    public static bool TryMapExposed(Relation request, out Relation witness)
    {
        if (!Fields(request, 4, out var fields) ||
            fields[1] is not Relation exposure || !exposure.TryReadOrderedGroup(out var points) ||
            points.Length == 0 || !TryBasis(fields[3], out _, out var axes))
            return Unresolved(request, out witness);

        var selected = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        Collect(fields[0], selected);
        // A coordinate value or a merely equal point cannot stand in for an
        // exposed point's identity. Disjoint exposures cannot be nested.
        if (points.Any(point => point is not Relation and not Placeholder || !selected.Contains(point)))
            return Unresolved(request, out witness);
        var unique = points.Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray();
        foreach (var point in unique)
        {
            if (point is not Relation relation) continue;
            var descendants = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
            Collect(relation.Left, descendants); Collect(relation.Right, descendants);
            if (unique.Any(other => descendants.Contains(other))) return Unresolved(request, out witness);
        }

        var replacements = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        var shared = axes.Select(_ => new Dictionary<object, IElement>(ReferenceEqualityComparer.Instance)).ToArray();
        var reads = new List<IElement>();
        var clauses = new List<IElement>();
        var remainder = new List<IElement>();
        foreach (var point in unique)
        {
            var complete = TryProject(Group(point, fields[2], fields[3]), shared, out var read);
            var result = ReadResult(read);
            // An invalid calibration/basis has no partial point result. Do not
            // replace source structure with a fabricated successful point.
            if (result is not Relation) return Unresolved(request, out witness);
            reads.Add(read);
            replacements.Add(point, result);
            clauses.Add(new Relation(point, result));
            if (!complete) remainder.Add(read);
        }
        var mapped = Replace(fields[0], replacements,
            new Dictionary<Relation, IElement>(ReferenceEqualityComparer.Instance));
        witness = Observe(request, Group(fields[1], fields[2], fields[3]), mapped, remainder,
            Group(Group([.. clauses]), Group([.. reads])));
        return remainder.Count == 0;
    }

    private static IElement ReadResult(Relation witness)
    {
        witness.TryReadOrderedGroup(out var fields);
        return fields[2];
    }

    private static void Collect(IElement element, HashSet<IElement> visited)
    {
        if (!visited.Add(element) || element is not Relation relation) return;
        Collect(relation.Left, visited); Collect(relation.Right, visited);
    }

    private static IElement Replace(IElement element, Dictionary<IElement, IElement> replacements,
        Dictionary<Relation, IElement> memo)
    {
        if (replacements.TryGetValue(element, out var result)) return result;
        if (element is not Relation relation) return element;
        if (memo.TryGetValue(relation, out result)) return result;
        var left = Replace(relation.Left, replacements, memo);
        var right = Replace(relation.Right, replacements, memo);
        result = ReferenceEquals(left, relation.Left) && ReferenceEquals(right, relation.Right)
            ? relation : new Relation(left, right);
        memo.Add(relation, result);
        return result;
    }
}
