using Gojo.Core;
using Gojo.Equations;

namespace PredictorScratchpad;

/// <summary>
/// Construction and local evaluation of ordinary connected Gojo equations.
/// A node contains its equation, input coordinates, output carrier, and input
/// bindings. There is no operation tag or authoritative stored derived answer.
/// </summary>
internal static class NativeCircuit
{
    public static Relation Group(params IElement[] parts) => Relation.OrderedGroup(new Carrier(), parts);

    public static IElement[] Parts(Relation group) =>
        group.Right is Carrier holder && group.TryReadOrderedElementsOn(holder, out var parts) ? parts : [];

    public static Carrier Output(IElement element)
    {
        if (element is CarrierLocation location) return location.Carrier;
        if (element is Relation node && Parts(node) is { Length: 4 } parts && parts[2] is Carrier carrier)
            return carrier;
        return Carrier.Unknown;
    }

    public static CarrierLocation State(long initial = 0) => new(new Carrier(), initial);
    public static CarrierLocation Constant(long value) => new(new Carrier(), value);

    public static Relation Sum(params (IElement Value, long Coefficient)[] terms)
    {
        var output = new Carrier();
        if (terms.Length == 0)
            return Group(Equation.Constant(output, 0), new CarrierLocation(output, 0), output, Group());
        var inputs = terms.Select(term => new CarrierLocation(Output(term.Value), 0)).ToArray();
        var equation = Equation.Sum(output, terms.Select((term, index) =>
            Equation.WeightedPort(inputs[index].Carrier, 0, 1, 0, term.Coefficient)).ToArray());
        return Group(equation, InputTree(inputs), output,
            Group(terms.Select((term, index) => (IElement)new Relation(inputs[index], term.Value)).ToArray()));
    }

    public static Relation Product(IElement left, IElement right)
    {
        var output = new Carrier();
        var a = new CarrierLocation(Output(left), 0);
        var b = new CarrierLocation(Output(right), 0);
        return Group(Equation.Product(output, Equation.UnitPort(a.Carrier), Equation.UnitPort(b.Carrier)),
            new Relation(a, b), output, Group(new Relation(a, left), new Relation(b, right)));
    }

    public static Relation Reframe(IElement value, Carrier target)
    {
        var input = new CarrierLocation(Output(value), 0);
        return Group(Equation.Affine(input.Carrier, 0, 1, target, 0, 1), input, target,
            Group(new Relation(input, value)));
    }

    public static Relation Divide(IElement numerator, IElement denominator)
    {
        var basis = new Carrier();
        var output = new Carrier();
        var n = new CarrierLocation(basis, 0);
        var d = new CarrierLocation(basis, 1);
        // Both upstream quantities reach this ratio basis through explicit
        // unit bridges. The denominator is a shared source-frame endpoint.
        var equation = new Relation(new Relation(new CarrierLocation(basis, 0), d),
            Relation.Segment(output, 0, 1));
        return Group(equation, n, output,
            Group(new Relation(n, Reframe(numerator, basis)), new Relation(d, Reframe(denominator, basis))));
    }

    /// <summary>Copies a native result into an explicit temporal coordinate.
    /// Cross-carrier commits require a visible Reframe equation first.</summary>
    public static bool Assign(CarrierLocation state, CarrierLocation nativeResult)
    {
        if (state.Carrier is null || state.Carrier.IsUnknown || state.Tick is null ||
            nativeResult.Carrier is null || nativeResult.Tick is null ||
            !state.Carrier.SameIdentityAs(nativeResult.Carrier)) return false;
        state.Tick.Set(nativeResult.Ticks);
        return true;
    }

    public static Relation Read(IElement root) => ReadMany(root)[0];

    /// <summary>Evaluates one batch with shared scratch only for this call.
    /// Frozen leaf reads cannot change when the input coordinates move later.</summary>
    public static Relation[] ReadMany(params IElement[] roots)
    {
        var memo = new Dictionary<ulong, Relation>();
        var active = new HashSet<ulong>();
        Relation Evaluate(IElement value)
        {
            if (value is CarrierLocation leaf && leaf.Carrier is not null && !leaf.Carrier.IsUnknown && leaf.Tick is not null)
                return new Relation(new CarrierLocation(leaf.Carrier, leaf.Ticks), Relation.Point(leaf.Carrier, 0));
            if (value is not Relation node)
                return Relation.UnresolvedRead(value, value, Output(value));
            if (memo.TryGetValue(node.Id, out var prior)) return prior;
            if (!active.Add(node.Id)) return Relation.UnresolvedRead(node, node, Output(node));
            try
            {
                var parts = Parts(node);
                if (parts.Length != 4 || parts[0] is not Relation equation || parts[2] is not Carrier output || parts[3] is not Relation bindings)
                    return Relation.UnresolvedRead(node, node, Output(node));
                var upstream = new List<IElement>();
                var updates = new List<(CarrierLocation Target, CarrierLocation Value)>();
                foreach (var bindingElement in Parts(bindings))
                {
                    if (bindingElement is not Relation { Left: CarrierLocation target } binding)
                        return Unresolved(parts[1], equation, output, upstream);
                    var read = Evaluate(binding.Right);
                    upstream.Add(read);
                    if (!Equation.TryReadPrediction(read, out var result, out _) || result.Carrier.IsUnknown ||
                        !target.Carrier.SameIdentityAs(result.Carrier))
                        return Unresolved(parts[1], equation, output, upstream);
                    updates.Add((target, result));
                }
                foreach (var (target, result) in updates) target.Tick.Set(result.Ticks);
                // The dynamic ratio frame has no usable basis when its endpoints
                // coincide. Preserve the failed read, never an invented quotient.
                if (equation.Left is Relation { Left: CarrierLocation start, Right: CarrierLocation end } &&
                    start.Carrier.SameIdentityAs(end.Carrier) && start.Ticks == end.Ticks)
                    return memo[node.Id] = Unresolved(parts[1], equation, output, upstream);
                if (!Equation.TryPredict(equation, parts[1], output, out var prediction) ||
                    !Equation.TryReadPrediction(prediction, out var calculated, out var residue))
                    return memo[node.Id] = Unresolved(parts[1], equation, output, upstream);
                return memo[node.Id] = new Relation(new CarrierLocation(calculated.Carrier, calculated.Ticks),
                    Group(residue, Group(upstream.ToArray())));
            }
            finally { active.Remove(node.Id); }
        }
        return roots.Select(Evaluate).ToArray();
    }

    public static CarrierLocation Value(IElement element)
    {
        if (Equation.TryReadPrediction(Read(element), out var value, out _)) return value;
        throw new InvalidOperationException("The native circuit remains unresolved; inspect Read for its retained structure.");
    }

    private static IElement InputTree(IReadOnlyList<CarrierLocation> inputs)
    {
        IElement result = inputs[0];
        for (var index = 1; index < inputs.Count; index++) result = new Relation(result, inputs[index]);
        return result;
    }

    private static Relation Unresolved(IElement input, Relation equation, Carrier output, List<IElement> upstream)
    {
        var failed = Relation.UnresolvedRead(input, equation, output);
        return new Relation(failed.Left, Group(failed.Right, Group(upstream.ToArray())));
    }
}
