using Gojo.Core;
using Gojo.Equations;

namespace ConnectionAssembly;

/// <summary>
/// Construction and local evaluation of ordinary connected Gojo equations.
/// A node contains its equation, input coordinates, output carrier, and input
/// bindings. There is no operation tag or authoritative stored derived answer.
/// </summary>
internal static class NativeMath
{
    public static Relation Group(params IElement[] parts) => Relation.OrderedGroup(new Carrier(), parts);

    public static IElement[] Parts(Relation group) =>
        group.TryReadOrderedGroup(out var parts) ? parts : [];

    public static Carrier Output(IElement element)
    {
        if (element is CarrierLocation location) return location.Carrier;
        if (element is Relation node && Parts(node) is { Length: 4 } parts && parts[2] is Carrier carrier)
            return carrier;
        return Carrier.Unknown;
    }

    public static Relation Sum(params (IElement Value, long Coefficient)[] terms)
        => SumTo(new Carrier(), terms);

    public static Relation SumTo(Carrier output, params (IElement Value, long Coefficient)[] terms)
    {
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

    /// <summary>A live coordinate is readable only through an admitted unit
    /// span. Collapsing that span leaves an unresolved native projection; it
    /// never turns a retained/default coordinate into a current zero sample.</summary>
    public static Relation Present(CarrierLocation value, CarrierLocation presence)
    {
        var sourceFrame = new Relation(new CarrierLocation(value.Carrier, 0),
            new CarrierLocation(value.Carrier, presence.Tick));
        var targetFrame = new Relation(new CarrierLocation(value.Carrier, 0), new CarrierLocation(value.Carrier, 1));
        return Group(Equation.Affine(sourceFrame, targetFrame), value, value.Carrier, Group());
    }

    public static Relation Read(IElement root) => ReadMany(root)[0];

    /// <summary>
    /// Evaluates a batch using per-call coordinates and substitution scratch.
    /// Unchanged instruction relations are reused; local input coordinates and
    /// results are captured. The result preserves residue, not an exhaustive
    /// historical copy of the operational recipe. Persistent scratch slots are
    /// never retargeted by a read. Concurrent writers still need an admission
    /// boundary when several physical sites must be observed as one frame.
    /// </summary>
    public static Relation[] ReadMany(params IElement[] roots)
    {
        var memo = new Dictionary<ulong, Relation>();
        var active = new HashSet<ulong>();
        var observedTicks = new Dictionary<TickRef, TickRef>(ReferenceEqualityComparer.Instance);

        TickRef Observe(TickRef tick)
        {
            if (!observedTicks.TryGetValue(tick, out var observed))
                observedTicks.Add(tick, observed = new TickRef(tick.Ticks));
            return observed;
        }

        Relation Evaluate(IElement value)
        {
            if (value is CarrierLocation leaf && leaf.Carrier is not null && !leaf.Carrier.IsUnknown && leaf.Tick is not null)
                return new Relation(new CarrierLocation(leaf.Carrier, Observe(leaf.Tick)), Relation.Point(leaf.Carrier, 0));
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
                var substitutions = new Dictionary<TickRef, TickRef>(ReferenceEqualityComparer.Instance);
                var copiedRelations = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
                var capturedRelations = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
                var copyingRelations = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
                var incompleteCopy = false;

                IElement CopyBound(IElement element, bool capture = false)
                {
                    TickRef CopyTick(TickRef tick) =>
                        substitutions.TryGetValue(tick, out var replacement) ? replacement : capture ? Observe(tick) : tick;
                    if (element is CarrierLocation location && location.Carrier is not null && location.Tick is not null)
                    {
                        var copiedTick = CopyTick(location.Tick);
                        return ReferenceEquals(copiedTick, location.Tick) ? element : new CarrierLocation(location.Carrier, copiedTick);
                    }
                    if (element is TickRef tick) return CopyTick(tick);
                    if (element is not Relation relation) return element;
                    var copies = capture ? capturedRelations : copiedRelations;
                    if (copies.TryGetValue(relation, out var copied)) return copied;
                    if (!copyingRelations.Add(relation))
                    {
                        incompleteCopy = true;
                        return new Placeholder();
                    }
                    var left = CopyBound(relation.Left, capture);
                    var right = CopyBound(relation.Right, capture);
                    copied = ReferenceEquals(left, relation.Left) && ReferenceEquals(right, relation.Right)
                        ? relation : new Relation(left, right);
                    copyingRelations.Remove(relation);
                    copies.Add(relation, copied);
                    return copied;
                }

                Relation Failed() => Unresolved(CopyBound(parts[1]), (Relation)CopyBound(equation), output, upstream);

                foreach (var bindingElement in Parts(bindings))
                {
                    if (bindingElement is not Relation { Left: CarrierLocation target } binding ||
                        target.Carrier is null || target.Carrier.IsUnknown || target.Tick is null)
                        return Failed();
                    var read = Evaluate(binding.Right);
                    upstream.Add(read);
                    if (!Equation.TryReadPrediction(read, out var result, out _) || result.Carrier.IsUnknown ||
                        !target.Carrier.SameIdentityAs(result.Carrier))
                        return Failed();
                    // One shared site cannot silently take conflicting bindings.
                    if (substitutions.TryGetValue(target.Tick, out var existing))
                    {
                        if (existing.Ticks != result.Ticks) return Failed();
                    }
                    else substitutions.Add(target.Tick, new TickRef(result.Ticks));
                }

                var localEquation = (Relation)CopyBound(equation);
                var localInput = CopyBound(parts[1], capture: true);
                if (incompleteCopy)
                    return memo[node.Id] = Unresolved(localInput, localEquation, output, upstream);
                // The dynamic ratio frame has no usable basis when its endpoints
                // coincide. Preserve the failed read, never an invented quotient.
                if (localEquation.Left is Relation { Left: CarrierLocation start, Right: CarrierLocation end } &&
                    start.Carrier.SameIdentityAs(end.Carrier) && start.Ticks == end.Ticks)
                    return memo[node.Id] = Unresolved(localInput, localEquation, output, upstream);
                if (!Equation.TryPredict(localEquation, localInput, output, out var prediction) ||
                    !Equation.TryReadPrediction(prediction, out var calculated, out var residue))
                    return memo[node.Id] = Unresolved(localInput, localEquation, output, upstream);
                // This is a current numeric read, not an exhaustive derivation
                // archive. Keep every upstream residue but do not repeatedly
                // copy all prior predictions or add group-anchor carriers.
                IElement retainedResidue = residue;
                foreach (var read in upstream)
                    retainedResidue = new Relation(retainedResidue, ((Relation)read).Right);
                return memo[node.Id] = new Relation(new CarrierLocation(calculated.Carrier, calculated.Ticks), retainedResidue);
            }
            finally { active.Remove(node.Id); }
        }
        return roots.Select(Evaluate).ToArray();
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
