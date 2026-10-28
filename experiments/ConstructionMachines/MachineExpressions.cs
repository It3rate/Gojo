using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>
/// A deliberately small interpreter over retained native requests. It selects
/// existing affine and contour laws; construction names never select behavior.
/// Evaluation observes the graph and does not move its carriers or public sites.
/// </summary>
public static class MachineExpressions
{
    public static Relation G(params IElement[] elements) =>
        Relation.OrderedGroup(new Carrier(), elements);

    public static IElement[] F(IElement element) =>
        element is Relation relation && relation.TryReadOrderedGroup(out var fields)
            ? fields
            : throw new ArgumentException("An ordered native group is required.", nameof(element));

    public static Relation Interpolate(IElement basis, IElement first, IElement second,
        IElement sample, long denominator, Carrier grade,
        long supportMin = -100000, long supportMax = 100000) =>
        new(G(first, second, G(sample, Relation.Segment(grade, 0, denominator),
            Relation.Segment(grade, supportMin, supportMax), basis)), new Placeholder());

    public static Relation Scale(IElement basis, IElement zero, IElement value,
        IElement sample, long denominator, Carrier grade) =>
        Interpolate(basis, zero, value, sample, denominator, grade);

    /// <summary>
    /// Sum in the supplied affine origin: extrapolate twice from zero toward the
    /// midpoint. Both operations and their parameter frames remain in the graph.
    /// </summary>
    public static Relation Add(IElement basis, IElement zero, IElement first,
        IElement second, Carrier grade)
    {
        var midpoint = Interpolate(basis, first, second, new CarrierLocation(grade, 1), 2, grade);
        return Interpolate(basis, zero, midpoint, new CarrierLocation(grade, 2), 1, grade);
    }

    public static Relation Turn(IElement traversal, IElement vector) => G(traversal, G(vector));

    public static Relation Project(IElement point, IElement calibrations, IElement basis) =>
        G(point, calibrations, basis);

    /// <summary>
    /// Resolve actual placeholder bindings and evaluate only the selected request
    /// shapes. Coordinate roles can be bound, but retained traversals and unrelated
    /// structures are opaque. Shared expressions are read once within this evaluation.
    /// Failed native reads keep their open result and complete witness.
    /// </summary>
    public static IElement Evaluate(IElement expression, IReadOnlyList<Relation> bindings,
        List<Relation> witnesses)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(witnesses);
        var memo = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        var active = new HashSet<IElement>(ReferenceEqualityComparer.Instance);

        IElement Visit(IElement current)
        {
            if (memo.TryGetValue(current, out var previous)) return previous;
            if (!active.Add(current))
            {
                // Cyclic bindings are unresolved participation, not a request to
                // recurse forever or to manufacture a concrete coordinate.
                var pending = new Placeholder();
                witnesses.Add(ReadWitness.Create(new Carrier(), current, G([.. bindings]),
                    pending, G(current), current));
                return pending;
            }

            IElement result = current;
            if (current is Placeholder)
            {
                var matching = bindings.Where(binding => ReferenceEquals(binding.Left, current)).ToArray();
                var alternatives = new List<IElement>();
                foreach (var binding in matching)
                    if (!alternatives.Any(candidate => SameParticipant(candidate, binding.Right)))
                        alternatives.Add(binding.Right);
                if (alternatives.Count == 1 && !ReferenceEquals(alternatives[0], current))
                    result = Visit(alternatives[0]);
                else if (alternatives.Count > 1)
                    witnesses.Add(ReadWitness.Create(new Carrier(), current, G(matching), current,
                        G(matching), current));
            }
            else if (current is Relation request &&
                Fields(request.Left, 3, out var inputs) && Fields(inputs[2], 4, out var grade))
            {
                var readGrade = SameGroup(inputs[2], grade,
                    [Visit(grade[0]), grade[1], grade[2], grade[3]]);
                var readInputs = SameGroup(request.Left, inputs,
                    [Visit(inputs[0]), Visit(inputs[1]), readGrade]);
                var applied = ReferenceEquals(readInputs, request.Left)
                    ? request : new Relation(readInputs, request.Right);
                AffineFrame.TryInterpolate(applied, out var witness);
                witnesses.Add(witness);
                result = F(witness)[2];
            }
            else if (current is Relation turn && Fields(turn, 2, out var fields) &&
                Fields(fields[1], 1, out var points))
            {
                // The turning law may itself be an open role. Resolving it must
                // supply a retained traversal, not replace the direction vector.
                // Native TryApply validates the resulting witness and keeps an
                // unresolved request if the law remains absent or incompatible.
                var traversal = Visit(fields[0]);
                var readPoints = SameGroup(fields[1], points, [Visit(points[0])]);
                var applied = (Relation)SameGroup(turn, fields, [traversal, readPoints]);
                ContourRotation.TryApply(applied, out var witness);
                witnesses.Add(witness);
                var output = F(witness)[2];
                result = Fields(output, 1, out var outputs) ? outputs[0] : output;
            }
            else if (current is Relation project && Fields(project, 3, out var projection) &&
                Calibrations(projection[1]) && Basis(projection[2]))
            {
                var applied = (Relation)SameGroup(project, projection,
                    [Visit(projection[0]), projection[1], projection[2]]);
                AffineFrame.TryProject(applied, out var witness);
                witnesses.Add(witness);
                result = F(witness)[2];
            }
            else if (current is Relation coordinates && coordinates.TryReadOrderedGroup(out var values) &&
                values.Length > 0 && values.All(IsCoordinate))
            {
                result = SameGroup(coordinates, values, values.Select(Visit).ToArray());
            }
            active.Remove(current);
            memo[current] = result;
            return result;
        }

        return Visit(expression);
    }

    private static bool Fields(IElement element, int count, out IElement[] fields)
    {
        if (element is Relation relation && relation.TryReadOrderedGroup(out fields) && fields.Length == count)
            return true;
        fields = [];
        return false;
    }

    private static bool IsCoordinate(IElement element) =>
        element is Placeholder || ProjectionResidue.TryRead(element, out _, out _, out _);

    private static bool SameParticipant(IElement first, IElement second) =>
        ReferenceEquals(first, second) || first is CarrierLocation a && second is CarrierLocation b &&
        a.Carrier.SameIdentityAs(b.Carrier) && a.SameTickRefAs(b);

    private static bool Calibrations(IElement element) =>
        element is Relation group && group.TryReadOrderedGroup(out var rows) && rows.Length > 0 &&
        rows.All(row => row is Relation map && Span(map.Left) && Span(map.Right));

    private static bool Basis(IElement element) =>
        element is Relation group && group.TryReadOrderedGroup(out var spans) && spans.Length > 0 &&
        spans.All(Span);

    private static bool Span(IElement element) => element is Relation span &&
        TimeStepSequence.TryReadSpan(span, out _, out _);

    private static IElement SameGroup(IElement original, IElement[] old, IElement[] updated)
    {
        if (old.Zip(updated).All(pair => ReferenceEquals(pair.First, pair.Second))) return original;
        return Relation.OrderedGroup((Carrier)((Relation)original).Right, updated);
    }
}
