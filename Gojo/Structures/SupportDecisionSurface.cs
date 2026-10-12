using Gojo.ConceptSurface;
using Gojo.Core;

namespace Gojo.Structures;

/// <summary>
/// Ordered local rules over advertised support. Conditions, bindings, priority,
/// selected responses and failed demands remain native relation structure.
/// Source ancestry is never consulted to recover unpublished precision.
/// </summary>
public static class SupportDecisionSurface
{
    // condition = G(role, closed required frame)
    // rule = G(G(conditions), response); policy = G(ordered rules)
    // binding = G(role, publication)
    // rule read = witness(rule, conditions, response OR open,
    //   G(rejected reads, unresolved reads),
    //   G(admitted bindings, admitted reads, rejected reads, unresolved reads))
    // selection = witness(bindings, policy, response OR open, earlier reads,
    //   G(selected rule OR open, admitted bindings, earlier reads, selected read OR open))

    public static Relation Condition(Carrier root, IElement role, Relation closedRequiredFrame) =>
        Relation.OrderedGroup(root, role, closedRequiredFrame);

    public static Relation Rule(Carrier root, IElement response, params Relation[] conditions) =>
        Relation.OrderedGroup(root, G(conditions), response);

    public static Relation Create(Carrier root, params Relation[] orderedRules) =>
        Relation.OrderedGroup(root, orderedRules);

    public static Relation Binding(Carrier root, IElement role, Relation publication) =>
        Relation.OrderedGroup(root, role, publication);

    /// <summary>
    /// Selects the first rule whose every condition admits the whole published
    /// support. A later unconditional rule is an explicit fallback: earlier
    /// ambiguity is retained, not interpreted as a false condition.
    /// </summary>
    public static Relation Select(Carrier root, Relation rules, params Relation[] bindings)
    {
        var supplied = G(bindings);
        var earlier = new List<IElement>();
        if (!rules.TryReadOrderedGroup(out var ordered)) return Finish(new Placeholder(), new Placeholder(), G(), new Placeholder());
        foreach (var entry in ordered)
        {
            if (entry is not Relation rule)
            {
                earlier.Add(Open(entry, rules));
                return Finish(new Placeholder(), new Placeholder(), G(), new Placeholder());
            }
            var read = EvaluateRule(rule, bindings, out var admitted, out var response);
            if (response is not null) return Finish(rule, response, admitted, read);
            earlier.Add(read);
        }
        return Finish(new Placeholder(), new Placeholder(), G(), new Placeholder());

        Relation Finish(IElement selected, IElement response, Relation admitted, IElement selectedRead)
        {
            var failed = G(earlier.ToArray());
            return ReadWitness.Create(root, supplied, rules, response, failed, G(selected, admitted, failed, selectedRead));
        }
    }

    public static bool TryReadSelection(Relation selection, out IElement selectedRule, out IElement response,
        out Relation admittedBindings, out Relation earlierReads)
    {
        selectedRule = response = null!; admittedBindings = earlierReads = null!;
        if (!Witness(selection, out _, out _, out var result, out var residue, out var provenance) ||
            provenance is not Relation group || !group.TryReadOrderedGroup(out var fields) || fields.Length != 4 ||
            fields[0] is not (Relation or Placeholder) || fields[1] is not Relation admitted ||
            !admitted.TryReadOrderedGroup(out _) || fields[2] is not Relation earlier ||
            !earlier.TryReadOrderedGroup(out _) || !ReferenceEquals(residue, earlier) ||
            fields[3] is not (Relation or Placeholder)) return false;
        selectedRule = fields[0]; response = result; admittedBindings = admitted; earlierReads = earlier; return true;
    }

    public static bool TryReadRuleRead(Relation read, out Relation rule, out Relation admittedBindings,
        out Relation rejectedConditions, out Relation unresolvedConditions)
    {
        rule = admittedBindings = rejectedConditions = unresolvedConditions = null!;
        if (!Witness(read, out var selected, out _, out _, out _, out var provenance) || selected is not Relation source ||
            provenance is not Relation group || !group.TryReadOrderedGroup(out var fields) || fields.Length != 4 ||
            fields[0] is not Relation admitted || fields[1] is not Relation accepted ||
            fields[2] is not Relation rejected || fields[3] is not Relation unresolved ||
            !admitted.TryReadOrderedGroup(out _) || !accepted.TryReadOrderedGroup(out _) ||
            !rejected.TryReadOrderedGroup(out _) || !unresolved.TryReadOrderedGroup(out _)) return false;
        rule = source; admittedBindings = admitted; rejectedConditions = rejected; unresolvedConditions = unresolved; return true;
    }

    private static Relation EvaluateRule(Relation rule, Relation[] bindings, out Relation admittedBindings, out IElement? response)
    {
        var admitted = new List<IElement>(); var accepted = new List<IElement>();
        var rejected = new List<IElement>(); var unresolved = new List<IElement>();
        response = null;
        IElement conditions = rule;
        if (!rule.TryReadOrderedGroup(out var fields) || fields.Length != 2 || fields[0] is not Relation group ||
            !group.TryReadOrderedGroup(out var checks)) unresolved.Add(Open(rule, rule));
        else
        {
            conditions = group;
            foreach (var check in checks)
            {
                var read = ReadCondition(check, bindings, out var binding, out var admission);
                if (admission > 0) { accepted.Add(read); admitted.Add(binding!); }
                else if (admission < 0) rejected.Add(read);
                else unresolved.Add(read);
            }
            if (rejected.Count == 0 && unresolved.Count == 0) response = fields[1];
        }
        admittedBindings = G(admitted.ToArray());
        var denied = G(rejected.ToArray()); var open = G(unresolved.ToArray());
        return ReadWitness.Create(new Carrier(), rule, conditions, response ?? new Placeholder(), G(denied, open),
            G(admittedBindings, G(accepted.ToArray()), denied, open));
    }

    private static Relation ReadCondition(IElement condition, Relation[] bindings, out Relation? binding, out int admission)
    {
        binding = null; admission = 0;
        if (condition is not Relation check || !check.TryReadOrderedGroup(out var fields) || fields.Length != 2 ||
            fields[1] is not Relation required || !Frame(required, out var axis, out var minimum, out var maximum))
            return Open(condition, condition);
        var matches = bindings.Where(candidate => candidate.TryReadOrderedGroup(out var pair) && pair.Length == 2 &&
            Equals(pair[0], fields[0])).ToArray();
        if (matches.Length != 1) return Open(G(matches), condition);
        binding = matches[0]; binding.TryReadOrderedGroup(out var parts);
        if (parts[1] is not Relation publication || !Support(publication, out var supportAxis, out var support) ||
            !axis.SameIdentityAs(supportAxis)) return Open(binding, condition);
        var captured = G(support.Select(range => (IElement)new Relation(
            range.Low is long low ? new CarrierLocation(axis, low) : new Placeholder(),
            range.High is long high ? new CarrierLocation(axis, high) : new Placeholder())).ToArray());
        var allInside = support.All(range => range.Low is long low && range.High is long high && low >= minimum && high <= maximum);
        var allOutside = support.All(range => range.Low is long low && range.High is long high && (high < minimum || low > maximum));
        admission = allInside ? 1 : allOutside ? -1 : 0;
        var frozenFrame = Relation.Segment(axis, minimum, maximum);
        return ReadWitness.Create(new Carrier(), binding, new Relation(condition, frozenFrame), captured,
            admission == 0 ? new Relation(captured, frozenFrame) : G(), new Relation(publication, captured));
    }

    private static bool Support(Relation publication, out Carrier axis, out (long? Low, long? High)[] support)
    {
        axis = null!; support = [];
        if (!Witness(publication, out _, out var surface, out var result, out _, out _)) return false;
        // A qualified observation must honor its advertised variation. Invalid
        // or open finite support cannot become a point through nominal fallback.
        if (QualifiedReadSurface.TryReadPublished(publication, out _, out _, out _, out _, out _, out var scope, out _, out _))
        {
            if (!FiniteReadSurface.TryReadValue(publication, out var nominal) ||
                !FiniteReadSurface.TryReadSupport(publication, out var low, out var high) || scope is not Relation capacity ||
                !BoundedWholeSurface.TryRead(capacity, out _, out var left, out var right, out _) ||
                !Point(left, out var minimum) || !Point(right, out var maximum) ||
                low is CarrierLocation lower && (lower.Ticks < minimum.Ticks || lower.Ticks > maximum.Ticks) ||
                high is CarrierLocation upper && (upper.Ticks < minimum.Ticks || upper.Ticks > maximum.Ticks)) return false;
            axis = nominal.Carrier; support = [(low?.Ticks, high?.Ticks)]; return true;
        }
        if (surface is Relation partition && CoarseReadSurface.TryReadPartition(partition, out var coarseAxis, out var regions))
        {
            if (!CoarseReadSurface.TryReadMask(publication, partition, out var mask) || mask == 0) return false;
            var ranges = new List<(long?, long?)>();
            for (var i = 0; i < regions.Length; i++)
                if ((mask & 1 << i) != 0)
                {
                    Frame(regions[i], out _, out var low, out var high); ranges.Add((low, high));
                }
            axis = coarseAxis; support = ranges.ToArray(); return true;
        }
        if (!Point(result, out var point) || point.Carrier.IsUnknown) return false;
        if (surface is Relation frame)
        {
            if (Span(frame, out var frameAxis, out _, out _) && !frameAxis.SameIdentityAs(point.Carrier)) return false;
            // An ordinary affine projection can publish a point through its
            // explicit target frame. A qualified shape with absent/invalid
            // variation must not fall through to that point contract.
            if (frame.Left is Relation basis && Span(basis, out _, out _, out _) &&
                (frame.Right is not Relation target || !Span(target, out var targetAxis, out _, out _) ||
                 !targetAxis.SameIdentityAs(point.Carrier))) return false;
        }
        axis = point.Carrier; support = [(point.Ticks, point.Ticks)]; return true;
    }

    private static bool Witness(Relation witness, out IElement selected, out IElement surface, out IElement result,
        out IElement residue, out IElement provenance)
    {
        selected = surface = result = residue = provenance = null!;
        return witness.TryReadOrderedGroup(out var fields) && fields.Length == 5 &&
            ReadWitness.TryRead(witness, out selected, out surface, out result, out residue, out provenance);
    }
    private static bool Frame(Relation frame, out Carrier axis, out long low, out long high)
    {
        if (Span(frame, out axis, out low, out high) && low <= high) return true;
        axis = null!; low = high = 0; return false;
    }
    private static bool Span(Relation frame, out Carrier axis, out long start, out long end)
    {
        if (Point(frame.Left, out var left) && Point(frame.Right, out var right) && !left.Carrier.IsUnknown &&
            left.Carrier.SameIdentityAs(right.Carrier))
        { axis = left.Carrier; start = left.Ticks; end = right.Ticks; return true; }
        axis = null!; start = end = 0; return false;
    }
    private static bool Point(IElement element, out CarrierLocation location)
    {
        if (element is CarrierLocation value && value.Carrier is not null && value.Tick is not null)
        { location = value; return true; }
        if (element is Relation { Left: CarrierLocation point, Right: Carrier carrier } && point.Carrier is not null &&
            point.Tick is not null && point.Carrier.SameIdentityAs(carrier)) { location = point; return true; }
        location = default; return false;
    }
    private static Relation Open(IElement selected, IElement attempted) =>
        ReadWitness.Create(new Carrier(), selected, attempted, new Placeholder(), new Relation(selected, attempted), attempted);
    private static Relation G(params IElement[] fields) => Relation.OrderedGroup(new Carrier(), fields);
}
