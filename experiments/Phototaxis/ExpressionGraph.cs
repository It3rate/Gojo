using ConnectionAssembly;
using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using static Phototaxis.Native;

namespace Phototaxis;

/// <summary>Builders and local traversal of connected programs, with no symbolic AST or operation tags.</summary>
public static class ExpressionGraph
{
    public const int MaximumDepth = 6;
    public const int MaximumOperations = 24;
    public const long SelectionExtent = 1_048_576;

    public static Relation Add(Units units, IElement a, IElement b) => SumTo(new Carrier(units.Feature.UnitShape), (a, 1), (b, 1));
    public static Relation Subtract(Units units, IElement a, IElement b) => SumTo(new Carrier(units.Feature.UnitShape), (a, 1), (b, -1));
    public static Relation Multiply(Units units, IElement a, IElement b) => Scale(Product(a, b), 1, 15, new Carrier(units.Feature.UnitShape));
    public static Relation Quotient(Units units, IElement a, IElement b) => Divide(Scale(a, 15, 1), b, new Carrier(units.Feature.UnitShape));

    public static Relation Less(Units units, IElement a, IElement b) => Bands(units, Subtract(units, a, b),
        (-SelectionExtent, 0, L(units.Feature, 15)), (0, SelectionExtent, L(units.Feature, 0)));
    public static Relation Greater(Units units, IElement a, IElement b) => Less(units, b, a);
    public static Relation Equal(Units units, IElement a, IElement b) => Bands(units, Subtract(units, a, b),
        (-SelectionExtent, 0, L(units.Feature, 0)), (0, 1, L(units.Feature, 15)), (1, SelectionExtent, L(units.Feature, 0)));
    public static Relation Truth(Units units, IElement a) => Bands(units, a,
        (-SelectionExtent, 0, L(units.Feature, 15)), (0, 1, L(units.Feature, 0)), (1, SelectionExtent, L(units.Feature, 15)));
    public static Relation Choose(Units units, IElement condition, IElement whenTrue, IElement whenFalse) =>
        Bands(units, condition, (0, 1, whenFalse), (15, 16, whenTrue));
    public static Relation Not(Units units, IElement a) => Choose(units, a, L(units.Feature, 0), L(units.Feature, 15));
    private static Relation Boolean(Units units, IElement a) => Choose(units, a, L(units.Feature, 15), L(units.Feature, 0));
    public static Relation And(Units units, IElement a, IElement b) => Choose(units, a, Boolean(units, b), L(units.Feature, 0));
    public static Relation Or(Units units, IElement a, IElement b) => Choose(units, a, L(units.Feature, 15), Boolean(units, b));
    public static Relation Xor(Units units, IElement a, IElement b) => Choose(units, a, Not(units, b), Boolean(units, b));

    private static Relation Bands(Units units, IElement selector, params (long Start, long End, IElement Value)[] bands)
    {
        var axis = NativeMath.Output(selector); var output = new Carrier(units.Feature.UnitShape);
        var lower = bands.Min(b => b.Start); var upper = bands.Max(b => b.End);
        return ScopedChoice.Create(selector, Relation.Segment(axis, lower, upper), output,
            bands.Select(b => ScopedChoice.Offer(Relation.Segment(axis, b.Start, b.End), b.Value,
                Equation.Affine(NativeMath.Output(b.Value), 0, 1, output, 0, 1))).ToArray());
    }

    public static IElement[] Inputs(IElement expression)
    {
        if (expression is not Relation root) return [];
        if (ScopedChoice.TryRead(root, out var selector, out _, out var offers, out _))
            return new[] { selector }.Concat(offers.Select(o => ScopedChoice.TryReadOffer(o, out _, out var source, out _) ? source : new Placeholder())).ToArray();
        var fields = Parts(root);
        if (fields.Length != 4 || fields[0] is not Relation || fields[2] is not Carrier || fields[3] is not Relation bindings) return [];
        var parts = Parts(bindings);
        return parts.All(p => p is Relation { Left: CarrierLocation }) ? parts.Cast<Relation>().Select(p => p.Right).ToArray() : [];
    }

    public static int Count(Relation catalog, params IElement[] expressions)
    {
        var seen = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        void Visit(IElement value)
        {
            if (CircuitInspection.SourceIndex(catalog, value) >= 0 || value is not Relation root || !seen.Add(root)) return;
            foreach (var input in Inputs(root)) Visit(input);
        }
        foreach (var expression in expressions) Visit(expression);
        return seen.Count;
    }

    public static int Depth(Relation catalog, IElement expression)
    {
        var active = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        int Visit(IElement value)
        {
            if (CircuitInspection.SourceIndex(catalog, value) >= 0 || value is not Relation root) return 0;
            if (!active.Add(root)) return MaximumDepth + 1;
            var inputs = Inputs(root);
            var depth = 1 + (inputs.Length == 0 ? 0 : inputs.Max(Visit)); active.Remove(root); return depth;
        }
        return Visit(expression);
    }

    /// <summary>Clone native recipes and rebind homologous exposed ports. Child sites have independent identity.</summary>
    public static IElement Rebind(IElement expression, IElement[] parentPorts, IElement[] childPorts)
    {
        var catalog = G(parentPorts);
        var memo = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        var carrierMap = new Dictionary<Carrier, Carrier>(ReferenceEqualityComparer.Instance);
        for (var i = 0; i < parentPorts.Length && i < childPorts.Length; i++)
            carrierMap[NativeMath.Output(parentPorts[i])] = NativeMath.Output(childPorts[i]);
        IElement Clone(IElement value)
        {
            var port = CircuitInspection.SourceIndex(catalog, value);
            if (port >= 0) return port < childPorts.Length ? childPorts[port] : new Placeholder();
            if (memo.TryGetValue(value, out var prior)) return prior;
            if (value is CarrierLocation constant)
            {
                if (!carrierMap.TryGetValue(constant.Carrier, out var carrier))
                    carrierMap.Add(constant.Carrier, carrier = new Carrier(constant.Carrier.UnitShape));
                return memo[value] = L(carrier, constant.Ticks);
            }
            if (value is not Relation root) return new Placeholder();
            // Prevent cyclic recipes from becoming an accidental recursive program.
            memo[value] = new Placeholder();
            if (ScopedChoice.TryRead(root, out var selector, out var scope, out var offers, out var choiceOutput))
            {
                var selected = Clone(selector); var axis = NativeMath.Output(selected);
                var output = new Carrier(choiceOutput.UnitShape);
                if (!ScopedChoice.TrySpan(scope, out _, out var lower, out var upper)) return memo[value];
                var rebound = new List<Relation>();
                foreach (var offer in offers)
                {
                    if (!ScopedChoice.TryReadOffer(offer, out var support, out var source, out var originalCalibration) ||
                        !ScopedChoice.TrySpan(support, out _, out var a, out var b)) return memo[value];
                    var input = Clone(source);
                    IElement CopyCalibration(IElement part) => part switch
                    {
                        CarrierLocation site => L(site.Carrier.SameIdentityAs(choiceOutput) ? output :
                            site.Carrier.SameIdentityAs(NativeMath.Output(source)) ? NativeMath.Output(input) : site.Carrier, site.Ticks),
                        Carrier c => c.SameIdentityAs(choiceOutput) ? output : c.SameIdentityAs(NativeMath.Output(source)) ? NativeMath.Output(input) : c,
                        Relation r => new Relation(CopyCalibration(r.Left), CopyCalibration(r.Right)),
                        _ => part
                    };
                    rebound.Add(ScopedChoice.Offer(Relation.Segment(axis, a, b), input, (Relation)CopyCalibration(originalCalibration)));
                }
                return memo[value] = ScopedChoice.Create(selected, Relation.Segment(axis, lower, upper), output, rebound.ToArray());
            }
            var fields = Parts(root);
            if (fields.Length != 4 || fields[0] is not Relation equation || fields[2] is not Carrier originalOutput ||
                fields[3] is not Relation bindings) return memo[value];
            var entries = Parts(bindings);
            if (entries.Any(e => e is not Relation { Left: CarrierLocation })) return memo[value];
            var targetOutput = new Carrier(originalOutput.UnitShape); carrierMap[originalOutput] = targetOutput;
            var ticks = new Dictionary<TickRef, CarrierLocation>(ReferenceEqualityComparer.Instance);
            var reboundBindings = new List<IElement>();
            foreach (var entry in entries.Cast<Relation>())
            {
                var original = (CarrierLocation)entry.Left; var input = Clone(entry.Right);
                var target = L(NativeMath.Output(input), original.Ticks); ticks[original.Tick] = target;
                carrierMap[original.Carrier] = target.Carrier; reboundBindings.Add(new Relation(target, input));
            }
            var copied = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
            IElement Copy(IElement part)
            {
                if (part is CarrierLocation site)
                {
                    if (ticks.TryGetValue(site.Tick, out var target)) return new CarrierLocation(target.Carrier, target.Tick);
                    return L(carrierMap.TryGetValue(site.Carrier, out var siteCarrier) ? siteCarrier : site.Carrier, site.Ticks);
                }
                if (part is Carrier carrier) return carrierMap.TryGetValue(carrier, out var replacement) ? replacement : carrier;
                if (part is not Relation relation) return part;
                if (!copied.TryGetValue(relation, out var copy)) copied.Add(relation, copy = new Relation(Copy(relation.Left), Copy(relation.Right)));
                return copy;
            }
            return memo[value] = G(Copy(equation), Copy(fields[1]), targetOutput, G(reboundBindings.ToArray()));
        }
        return Clone(expression);
    }

    // Presentation only: the compact description is derived from exact local recipes.
    public static string Describe(Relation catalog, IElement source, int depth = 0)
    {
        if (depth > 32) return "deeper expression";
        string[] labels = ["15", "left eye", "right eye", "hunger", "fullness", "L×hunger", "R×hunger", "L×full", "R×full"];
        var index = CircuitInspection.SourceIndex(catalog, source);
        if (index >= 0) return index < labels.Length ? labels[index] : $"input {index}";
        if (source is CarrierLocation constant) return constant.Ticks.ToString();
        if (source is not Relation root) return "?";
        var inputs = Inputs(root);
        if (ScopedChoice.TryRead(root, out _, out var choiceScope, out var offers, out _))
        {
            var bands = offers.Select(o =>
            {
                if (!ScopedChoice.TryReadOffer(o, out var band, out var offered, out _) ||
                    !ScopedChoice.TrySpan(band, out _, out var start, out var end)) return (start: 0L, end: 0L, offered: (IElement)new Placeholder());
                return (start, end, offered);
            }).ToArray();
            if (bands.Length == 2 && bands[0].start == 0 && bands[0].end == 1 && bands[1].start == 15 && bands[1].end == 16)
                return $"choose({Describe(catalog, inputs[0], depth + 1)}, {Describe(catalog, inputs[2], depth + 1)}, {Describe(catalog, inputs[1], depth + 1)})";
            return $"bands({Describe(catalog, inputs[0], depth + 1)}; " + string.Join("; ", bands.Select(b =>
                $"[{b.start},{b.end}): {Describe(catalog, b.offered, depth + 1)}")) + ")";
        }
        var fields = Parts(root);
        if (fields.Length != 4 || fields[0] is not Relation equation) return "?";
        if (inputs.Length == 2 && equation.Right is Relation { Right: Carrier })
        {
            var ports = equation.Left is Relation pair ? new[] { pair.Left, pair.Right } : [];
            var negative = ports.Length == 2 && ports[1] is Relation p && p.Right is Relation frame &&
                frame.Right is Relation { Left: CarrierLocation end } && end.Ticks < 0;
            return $"({Describe(catalog, inputs[0], depth + 1)} {(negative ? "−" : "+")} {Describe(catalog, inputs[1], depth + 1)})";
        }
        if (inputs.Length == 2 && equation.Right is Relation { Left: Relation, Right: Relation })
            return $"({Describe(catalog, inputs[0], depth + 1)} × {Describe(catalog, inputs[1], depth + 1)})";
        if (inputs.Length == 1) return $"scale({Describe(catalog, inputs[0], depth + 1)})";
        return inputs.Length == 2 ? $"({Describe(catalog, inputs[0], depth + 1)} ÷ {Describe(catalog, inputs[1], depth + 1)})" : "equation";
    }
}
