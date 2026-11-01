using Gojo.Core;
using Scalar = Gojo.Core.ExactScalar;

namespace Gojo.Structures;

/// <summary>
/// Selected unital bilinear coefficient algebra over two calibrated roles.
/// The connected four-state action supplies J(1)=i and J(i)=-1. Opposed faces
/// and geometric quarters can ground that same law without becoming the same
/// primitive operation. All durable values and evidence are ordinary relations.
/// </summary>
public static partial class ComplexFrame
{
    /// <summary>Builds a selected signed phase cycle on a carrier and its opposed face.</summary>
    public static Relation Opposed(Carrier real)
    {
        ArgumentNullException.ThrowIfNull(real);
        if (real.IsUnknown || real.IsInverse) throw new ArgumentException("Select a concrete forward carrier.", nameof(real));
        var coordinates = Group(Relation.Segment(real, 0, 1), Relation.Segment(real.Inverse(), 0, 1));
        Coordinates(coordinates, out var frame);
        return Basis(coordinates, frame, new Relation(real, real.Inverse()));
    }

    /// <summary>Checks the exact ordered basis, unit states, shared cycle and coefficient calibration.</summary>
    public static bool TryReadBasis(IElement basis) => ReadBasis(basis, out _);

    /// <summary>Reads group(basis, left, right), with exact or open coefficient slots.</summary>
    public static bool TryAdd(Relation request, out Relation witness) =>
        AddOrSubtract(request, false, out witness);

    /// <summary>Subtracts in the selected coefficient basis; repeated identical slots may cancel.</summary>
    public static bool TrySubtract(Relation request, out Relation witness) =>
        AddOrSubtract(request, true, out witness);

    private static bool AddOrSubtract(Relation request, bool subtract, out Relation witness)
    {
        var captured = Capture(request);
        if (!Inputs(captured, out var f, out var basis, out var a, out var b)) return Unresolved(request, captured, out witness);
        Fields(f[1], 2, out var left); Fields(f[2], 2, out var right);
        var value = new Scalar?[2];
        for (var i = 0; i < 2; i++)
            value[i] = subtract && (ReferenceEquals(f[1], f[2]) || left.Length == 2 && right.Length == 2 && ReferenceEquals(left[i], right[i]))
                ? Scalar.Zero : a[i] is { } x && b[i] is { } y ? subtract ? x - y : x + y : null;
        // Keep the signed coordinate calibrations visible, including when
        // the two outputs happen to coincide or an input is still open.
        Relation Calibration(long sign) => Group(basis.Axes.Select(axis => (IElement)new Relation(
            Relation.Segment(axis, 0, 1), Relation.Segment(axis, 0, sign))).ToArray());
        return Finish(request, captured, basis, value,
            Group(new Relation(f[1], Calibration(1)), new Relation(f[2], Calibration(subtract ? -1 : 1))), out witness);
    }

    /// <summary>Distributes group(basis,left,right) into four witnessed coefficient products.</summary>
    public static bool TryMultiply(Relation request, out Relation witness)
    {
        var captured = Capture(request);
        if (!Inputs(captured, out var f, out var basis, out var a, out var b)) return Unresolved(request, captured, out witness);
        try
        {
            var result = Product(basis, a, b);
            if (!Fields(f[1], 2, out var left) || !Fields(f[2], 2, out var right))
                return Finish(request, captured, basis, result, Group(f[1], f[2], basis.Action), out witness);
            var terms = new List<IElement>();
            for (var i = 0; i < 2; i++)
                for (var j = 0; j < 2; j++)
                {
                    Scalar?[] x = [Scalar.Zero, Scalar.Zero], y = [Scalar.Zero, Scalar.Zero];
                    x[i] = a[i]; y[j] = b[j];
                    terms.Add(new Relation(new Relation(left[i], right[j]), Value(basis, Product(basis, x, y))));
                }
            return Finish(request, captured, basis, result, Group([.. terms]), out witness);
        }
        catch (OverflowException) { return Unresolved(request, captured, out witness); }
    }

    /// <summary>Reads group(basis,value); advances the connected signed phase action.</summary>
    public static bool TryApplyGenerator(Relation request, out Relation witness)
    {
        var captured = Capture(request);
        if (!Fields(captured, 2, out var f) || !ReadBasis(f[0], out var basis) || !ReadValue(f[1], basis, out var a))
            return Unresolved(request, captured, out witness);
        return Finish(request, captured, basis, Apply(basis, a), basis.Action, out witness);
    }

    /// <summary>Reads group(basis,value); conjugation fixes the unit and opposes the generator coefficient.</summary>
    public static bool TryConjugate(Relation request, out Relation witness)
    {
        var captured = Capture(request);
        if (!Fields(captured, 2, out var f) || !ReadBasis(f[0], out var basis) || !ReadValue(f[1], basis, out var a))
            return Unresolved(request, captured, out witness);
        return Finish(request, captured, basis, [a[0], a[1] is { } y ? -y : null],
            Group(new Relation(basis.States[0], basis.States[0]), new Relation(basis.States[1], basis.States[3])), out witness);
    }

    /// <summary>Reads group(basis,numerator,denominator). A zero or unknown divisor retains the domain obligation.</summary>
    public static bool TryDivide(Relation request, out Relation witness)
    {
        var captured = Capture(request);
        if (!Inputs(captured, out var f, out var basis, out var a, out var b) || b.Any(v => v is null))
            return Unresolved(request, captured, out witness);
        var norm = b[0]!.Value * b[0]!.Value + b[1]!.Value * b[1]!.Value;
        if (norm.IsZero) return Unresolved(request, captured, out witness);
        try
        {
            Scalar?[] conjugate = [b[0], -b[1]!.Value];
            var numerator = Product(basis, a, conjugate);
            var result = numerator.Select(v => v is { } x ? (Scalar?)(x / norm) : null).ToArray();
            return Finish(request, captured, basis, result,
                Group(new Relation(f[2], Value(basis, conjugate)), norm.Encode(basis.Axes[0]), Value(basis, numerator)), out witness);
        }
        catch (OverflowException) { return Unresolved(request, captured, out witness); }
    }

    /// <summary>
    /// Reads group(source basis,target basis,group(source one -> target one,
    /// source generator -> target generator),value). Explicit coefficient
    /// correspondence is required even when the visible numbers agree.
    /// </summary>
    public static bool TryReexpress(Relation request, out Relation witness)
    {
        var captured = Capture(request);
        if (!Fields(captured, 4, out var f) || !ReadBasis(f[0], out var source) || !ReadBasis(f[1], out var target) ||
            !Fields(f[2], 2, out var maps) || !ReadValue(f[3], source, out var value)) return Unresolved(request, captured, out witness);
        for (var i = 0; i < 2; i++)
            if (maps[i] is not Relation map || !ReferenceEquals(map.Left, source.States[i]) ||
                !ReferenceEquals(map.Right, target.States[i])) return Unresolved(request, captured, out witness);
        return Finish(request, captured, target, value, f[2], out witness);
    }

    private static bool Inputs(IElement request, out IElement[] fields, out Frame basis, out Scalar?[] a, out Scalar?[] b)
    {
        basis = null!; a = b = [];
        return Fields(request, 3, out fields) && ReadBasis(fields[0], out basis) &&
            ReadValue(fields[1], basis, out a) && ReadValue(fields[2], basis, out b);
    }

    // The backend applies columns read from connected transitions. It does not
    // identify generic face multiplication or geometric duality with this law.
    private static Scalar?[] Apply(Frame basis, Scalar?[] value) =>
        [Sum(Times(value[0], basis.Columns[0][0]), Times(value[1], basis.Columns[1][0])),
         Sum(Times(value[0], basis.Columns[0][1]), Times(value[1], basis.Columns[1][1]))];
    private static Scalar?[] Product(Frame basis, Scalar?[] a, Scalar?[] b)
    {
        var turned = Apply(basis, a);
        return [Sum(Times(a[0], b[0]), Times(turned[0], b[1])), Sum(Times(a[1], b[0]), Times(turned[1], b[1]))];
    }
    private static Scalar? Times(Scalar? a, Scalar? b) =>
        a is { IsZero: true } || b is { IsZero: true } ? Scalar.Zero : a is { } x && b is { } y ? x * y : null;
    private static Scalar? Sum(Scalar? a, Scalar? b) => a is { } x && b is { } y ? x + y : null;

    private static Relation Basis(Relation coordinates, Frame frame, IElement evidence)
    {
        Scalar[][] values = [[Scalar.One, Scalar.Zero], [Scalar.Zero, Scalar.One],
            [new Scalar(-1), Scalar.Zero], [Scalar.Zero, new Scalar(-1)]];
        var states = values.Select(v => (IElement)Value(frame, [v[0], v[1]])).ToArray();
        return Group(coordinates, Group(Enumerable.Range(0, 4)
            .Select(i => (IElement)new Relation(states[i], states[(i + 1) % 4])).ToArray()), evidence);
    }

    private static bool ReadBasis(IElement element, out Frame basis)
    {
        basis = null!;
        if (!Fields(element, 3, out var f) || !Coordinates(f[0], out var frame) ||
            f[1] is not Relation action || !Fields(action, 4, out var edges)) return false;
        var states = new IElement[4]; var values = new Scalar?[4][];
        for (var i = 0; i < 4; i++)
        {
            if (edges[i] is not Relation edge || edges[(i + 1) % 4] is not Relation next ||
                !ReferenceEquals(edge.Right, next.Left) || !ReadValue(edge.Left, frame, out values[i]) ||
                values[i].Any(v => v is null)) return false;
            states[i] = edge.Left;
        }
        if (values[0][0] != Scalar.One || values[0][1] != Scalar.Zero ||
            values[1][0] != Scalar.Zero || values[1][1] != Scalar.One) return false;
        for (var i = 0; i < 2; i++)
            for (var c = 0; c < 2; c++)
                if (values[i + 2][c] != -values[i][c]!.Value) return false;
        basis = frame with { Surface = (Relation)element, Action = action, States = states,
            Columns = [values[1].Select(v => v!.Value).ToArray(), values[2].Select(v => v!.Value).ToArray()] };
        return true;
    }

    private static bool Coordinates(IElement element, out Frame frame)
    {
        frame = null!;
        if (element is not Relation { Right: Carrier holder } || holder.IsUnknown || holder.IsInverse ||
            !Fields(element, 2, out var units)) return false;
        var axes = new Carrier[2];
        for (var i = 0; i < 2; i++)
        {
            if (units[i] is not Relation span || !Point(span.Left, out var a) || !Point(span.Right, out var b) ||
                !a.Carrier.SameIdentityAs(b.Carrier) || a.Ticks != 0 || b.Ticks != 1) return false;
            axes[i] = a.Carrier;
        }
        if (axes[0].SameIdentityAs(axes[1])) return false;
        frame = new Frame((Relation)element, holder, axes, null!, [], []); return true;
    }

    private static bool ReadValue(IElement element, Frame basis, out Scalar?[] values)
    {
        values = [null, null];
        if (element is Placeholder { IsInverse: false }) return true;
        if (element is not Relation relation || !ReferenceEquals(relation.Right, basis.Holder) || !Fields(element, 2, out var f)) return false;
        for (var i = 0; i < 2; i++)
        {
            if (f[i] is Placeholder { IsInverse: false }) continue;
            var e = Point(f[i], out var p) ? p : f[i];
            if (!ProjectionResidue.TryRead(e, out var axis, out var n, out var d) || !axis.SameIdentityAs(basis.Axes[i])) return false;
            values[i] = new Scalar(n, d);
        }
        return true;
    }

    private static bool Point(IElement element, out CarrierLocation point)
    {
        if (element is Relation { Left: CarrierLocation p, Right: Carrier c } && !c.IsUnknown && p.Carrier.SameIdentityAs(c))
        { point = p; return true; }
        point = default; return false;
    }
    private static Relation Value(Frame frame, Scalar?[] values) => Relation.OrderedGroup(frame.Holder,
        values.Select((v, i) => v is { } x ? x.Encode(frame.Axes[i]) : new Placeholder()).ToArray());
    private static bool Finish(Relation request, IElement captured, Frame basis, Scalar?[] values, IElement proof, out Relation witness)
    {
        try
        {
            var result = Value(basis, values);
            var complete = values.All(v => v is not null);
            witness = Observe(request, basis.Surface, result, complete ? [] : [captured], Group(captured, proof));
            return complete;
        }
        catch (OverflowException) { return Unresolved(request, captured, out witness); }
    }
    private static bool Fields(IElement e, int count, out IElement[] fields)
    {
        if (e is Relation r && r.TryReadOrderedGroup(out fields) && fields.Length == count) return true;
        fields = []; return false;
    }
    private static Relation Group(params IElement[] fields) => Relation.OrderedGroup(new Carrier(), fields);
    private static Relation Capture(Relation source) => (Relation)new ExposedStructureSnapshot().Copy(source);
    private static Relation Observe(IElement source, IElement basis, IElement result, IElement[] residue, IElement proof) =>
        ReadWitness.Create(new Carrier(), source, basis, result, Group(residue), proof);
    private static bool Unresolved(Relation source, IElement captured, out Relation witness)
    { witness = Observe(source, captured, new Placeholder(), [captured], captured); return false; }
    private sealed record Frame(Relation Surface, Carrier Holder, Carrier[] Axes, Relation Action, IElement[] States, Scalar[][] Columns);
}
