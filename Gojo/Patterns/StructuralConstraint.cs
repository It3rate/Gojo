using Gojo.Core;

namespace Gojo.Patterns;

/// <summary>
/// A local, finite identity/shape read over exposed relation structure. Selected
/// slots, scope closure, correspondences and unresolved alternatives are all
/// ordinary elements. This does not change Bind/Advance or carrier settlement.
/// </summary>
public static class StructuralConstraint
{
    // scope = seam -> (entry -> (... -> tail)); tail aliases seam iff closed.
    // request = ((pattern -> selected slots) -> initial bindings) -> support.
    // read = (request -> selected slots) ->
    //        (candidates -> (blocked comparisons -> open evidence)).
    // candidate = offered participant -> (pattern instance -> binding scope).

    public static Relation Closed(params IElement[] entries) => Scope(entries, open: false);
    public static Relation Open(params IElement[] entries) => Scope(entries, open: true);

    private static Relation Scope(IElement[] entries, bool open)
    {
        var seam = new Placeholder();
        IElement spine = open ? new Placeholder() : seam;
        for (var i = entries.Length - 1; i >= 0; i--) spine = new Relation(entries[i], spine);
        return new Relation(seam, spine);
    }

    /// <summary>Reads only the exposed spine, never nested entries. Shared seam
    /// identity closes it; an independent terminal preserves an open remainder.</summary>
    public static bool TryReadScope(Relation scope, out IElement[] entries,
        out Placeholder seam, out Placeholder tail)
    {
        entries = []; seam = tail = null!;
        if (scope.Left is not Placeholder { IsInverse: false } start) return false;
        var values = new List<IElement>();
        var seen = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        IElement cursor = scope.Right;
        while (cursor is Relation cell)
        {
            if (!seen.Add(cell)) return false;
            values.Add(cell.Left);
            cursor = cell.Right;
        }
        if (cursor is not Placeholder { IsInverse: false } end) return false;
        entries = [.. values]; seam = start; tail = end;
        return true;
    }

    public static Relation Request(IElement pattern, Relation variables, Relation support,
        params Relation[] initialBindings) =>
        new(new Relation(new Relation(pattern, variables), Closed(initialBindings)), support);

    public static Relation Match(IElement pattern, Relation variables, Relation support,
        params Relation[] initialBindings) => Read(Request(pattern, variables, support, initialBindings));

    /// <summary>Each offered alternative gets its own binding frame. Only
    /// relation nodes containing selected variables decompose as a pattern;
    /// other participants retain their identity. Absence in an open scope
    /// cannot reject a query, and multiple matches never select a first winner.</summary>
    public static Relation Read(Relation request)
    {
        if (!Definition(request, out var pattern, out var variables, out var initial, out var support) ||
            !Selected(variables, out var slots) || !Bindings(initial, slots, out var supplied) ||
            !TryReadScope(support, out var offers, out var seam, out var tail) ||
            !Acyclic(pattern, new(ReferenceEqualityComparer.Instance))) return Unresolved(request);

        var candidates = new List<IElement>();
        var blocked = new List<IElement>();
        var unresolved = new List<IElement>();
        foreach (var offered in offers)
        {
            var bindings = new Dictionary<Placeholder, IElement>(supplied);
            var comparison = new Relation(pattern, offered);
            // A supplier cannot discharge a selected variable by offering that
            // same unresolved variable (possibly nested) back to this query.
            if (ContainsSelected(offered, slots, new(ReferenceEqualityComparer.Instance)))
            { unresolved.Add(comparison); continue; }
            if (!MatchCore(pattern, offered, slots, bindings, new(ReferenceEqualityComparer.Instance)))
            { blocked.Add(comparison); continue; }
            var frame = bindings.Select(pair => StructuralPattern.Bind(pair.Key, pair.Value)).ToArray();
            var instance = Instantiate(pattern, bindings, new(ReferenceEqualityComparer.Instance));
            candidates.Add(new Relation(offered, new Relation(instance, Closed(frame))));
        }
        if (!seam.Equals(tail)) unresolved.Add(new Relation(support, tail));
        if (candidates.Count == 0 && unresolved.Count == 0) blocked.Add(request);
        return Result(request, candidates, blocked, unresolved, slots);
    }

    public static bool TryRead(Relation read, out Relation request, out Relation candidates,
        out Relation blocked, out Relation unresolved)
    {
        request = candidates = blocked = unresolved = null!;
        if (read.Left is not Relation { Left: Relation source, Right: Relation variables } ||
            !Selected(variables, out _) ||
            read.Right is not Relation { Left: Relation accepted, Right: Relation remainder } ||
            remainder.Left is not Relation rejected || remainder.Right is not Relation open ||
            !ClosedEntries(accepted, out _) || !ClosedEntries(rejected, out _) || !ClosedEntries(open, out _)) return false;
        request = source; candidates = accepted; blocked = rejected; unresolved = open;
        return true;
    }

    public static bool TryReadCandidate(Relation candidate, out IElement offered,
        out IElement instance, out Relation bindings)
    {
        offered = instance = null!; bindings = null!;
        if (candidate.Right is not Relation { Right: Relation frame } body ||
            !ClosedEntries(frame, out var entries) || entries.Any(entry => entry is not Relation { Left: Placeholder })) return false;
        offered = candidate.Left; instance = body.Left; bindings = frame;
        return true;
    }

    /// <summary>Projects every supported binding separately. This is an explicit
    /// consumption of the read; its original candidate remains provenance.</summary>
    public static Relation Project(Relation read, IElement target)
    {
        var request = new Relation(read, target);
        if (!TryRead(read, out _, out var candidates, out var blocked, out var unresolved) ||
            !SelectedInRead(read, out var selected) ||
            !Acyclic(target, new(ReferenceEqualityComparer.Instance))) return Unresolved(request);
        ClosedEntries(candidates, out var offers);
        ClosedEntries(blocked, out var rejected);
        ClosedEntries(unresolved, out var pending);
        var results = new List<IElement>();
        var open = new List<IElement>(pending);
        foreach (var entry in offers)
        {
            if (entry is not Relation candidate ||
                !TryReadCandidate(candidate, out _, out var previousInstance, out var bindings) ||
                !ClosedEntries(bindings, out _) ||
                ContainsSelected(previousInstance, selected, new(ReferenceEqualityComparer.Instance)))
            { open.Add(new Relation(entry, target)); continue; }
            if (!Bindings(bindings, selected, out var supplied))
            { open.Add(new Relation(candidate, target)); continue; }
            var instance = Instantiate(target, supplied, new(ReferenceEqualityComparer.Instance));
            if (ContainsSelected(instance, selected, new(ReferenceEqualityComparer.Instance)))
            { open.Add(new Relation(candidate, target)); continue; }
            results.Add(new Relation(candidate, new Relation(instance, bindings)));
        }
        return Result(request, results, rejected, open, selected);
    }

    /// <summary>Passes every candidate frame into a second local constraint.
    /// No alternatives are collapsed into a first-binding-wins frame. An open
    /// upstream remainder stays open; every second-stage comparison retains
    /// its originating branch as provenance.</summary>
    public static Relation Join(Relation read, IElement pattern, Relation variables,
        Relation support, params Relation[] initialBindings)
    {
        var request = new Relation(read, Request(pattern, variables, support, initialBindings));
        if (!TryRead(read, out _, out var candidates, out var blocked, out var unresolved) ||
            !SelectedInRead(read, out var selected) || !Selected(variables, out var additional)) return Unresolved(request);
        selected.UnionWith(additional);
        var jointVariables = Closed(selected.Cast<IElement>().ToArray());
        ClosedEntries(candidates, out var offers);
        ClosedEntries(blocked, out var rejected);
        ClosedEntries(unresolved, out var pending);
        var results = new List<IElement>();
        var denied = new List<IElement>(rejected);
        var open = new List<IElement>(pending);
        foreach (var entry in offers)
        {
            if (entry is not Relation parent || !TryReadCandidate(parent, out _, out var previousInstance, out var bindings) ||
                !ClosedEntries(bindings, out var frame) || !Bindings(bindings, selected, out _) ||
                ContainsSelected(previousInstance, selected, new(ReferenceEqualityComparer.Instance))) { open.Add(entry); continue; }
            var child = Match(pattern, jointVariables, support, [.. frame.Cast<Relation>(), .. initialBindings]);
            TryRead(child, out _, out var accepted, out var refused, out var incomplete);
            ClosedEntries(accepted, out var matches);
            ClosedEntries(refused, out var failures);
            ClosedEntries(incomplete, out var unknown);
            foreach (var match in matches)
            {
                var candidate = (Relation)match;
                TryReadCandidate(candidate, out _, out var instance, out var bound);
                results.Add(new Relation(new Relation(parent, candidate), new Relation(instance, bound)));
            }
            denied.AddRange(failures.Select(failure => (IElement)new Relation(parent, failure)));
            open.AddRange(unknown.Select(remaining => (IElement)new Relation(parent, remaining)));
        }
        return Result(request, results, denied, open, selected);
    }

    /// <summary>A closed read with one supported route may supply one result.
    /// Equal-looking routes are retained separately; this reader does not merge
    /// their identity or invent a preferred proof.</summary>
    public static bool TryReadUnique(Relation read, out IElement instance, out Relation bindings)
    {
        instance = null!; bindings = null!;
        if (TryRead(read, out _, out var candidates, out _, out var unresolved) &&
            SelectedInRead(read, out var selected) &&
            ClosedEntries(unresolved, out var pending) && pending.Length == 0 &&
            ClosedEntries(candidates, out var entries) && entries.Length == 1 &&
            entries[0] is Relation candidate && TryReadCandidate(candidate, out _, out var result, out var frame) &&
            Bindings(frame, selected, out _) &&
            !ContainsSelected(result, selected, new(ReferenceEqualityComparer.Instance)))
        { instance = result; bindings = frame; return true; }
        return false;
    }

    /// <summary>Guarded structural application at this explicit passage. Blocked,
    /// open or competing proposals return the original target unchanged. The
    /// supplied read is evidence for its exposed premises, not an auto-refreshing
    /// authority over later worlds or direct StructuralPattern substitutions.</summary>
    public static bool ApplyUnique(Relation read, IElement target, out IElement applied)
    {
        applied = target;
        if (!TryReadUnique(read, out _, out _) || !TryReadUnique(Project(read, target), out var result, out _)) return false;
        applied = result;
        return true;
    }

    private static bool Definition(Relation request, out IElement pattern, out Relation variables,
        out Relation initial, out Relation support)
    {
        pattern = null!; variables = initial = support = null!;
        if (request.Left is not Relation { Left: Relation definition, Right: Relation frame } ||
            definition.Right is not Relation selected || request.Right is not Relation scope) return false;
        pattern = definition.Left; variables = selected; initial = frame; support = scope;
        return true;
    }

    private static bool Selected(Relation variables, out HashSet<Placeholder> slots)
    {
        slots = [];
        if (!ClosedEntries(variables, out var entries)) return false;
        foreach (var entry in entries)
            if (entry is not Placeholder { IsInverse: false } slot || !slots.Add(slot)) return false;
        return true;
    }

    private static bool Bindings(Relation frame, HashSet<Placeholder> slots,
        out Dictionary<Placeholder, IElement> bindings)
    {
        bindings = [];
        if (!ClosedEntries(frame, out var entries)) return false;
        foreach (var entry in entries)
        {
            if (entry is not Relation { Left: Placeholder slot } binding || !slots.Contains(slot) ||
                ContainsSelected(binding.Right, slots, new(ReferenceEqualityComparer.Instance)) ||
                bindings.TryGetValue(slot, out var prior) && !SameParticipant(prior, binding.Right)) return false;
            bindings[slot] = binding.Right;
        }
        return true;
    }

    private static bool MatchCore(IElement pattern, IElement offered, HashSet<Placeholder> slots,
        Dictionary<Placeholder, IElement> bindings, Dictionary<Relation, Relation> correspondence)
    {
        if (pattern is Placeholder slot && slots.Contains(slot))
        {
            if (bindings.TryGetValue(slot, out var prior)) return SameParticipant(prior, offered);
            bindings.Add(slot, offered);
            return true;
        }
        if (!ContainsSelected(pattern, slots, new(ReferenceEqualityComparer.Instance))) return SameParticipant(pattern, offered);
        if (pattern is not Relation shape || offered is not Relation value) return false;
        if (correspondence.TryGetValue(shape, out var priorShape)) return ReferenceEquals(priorShape, value);
        correspondence.Add(shape, value);
        return MatchCore(shape.Left, value.Left, slots, bindings, correspondence) &&
            MatchCore(shape.Right, value.Right, slots, bindings, correspondence);
    }

    private static bool ContainsSelected(IElement element, HashSet<Placeholder> selected, HashSet<Relation> active)
    {
        if (element is Placeholder slot) return selected.Contains(slot);
        if (element is not Relation relation) return false;
        // Unsupported cyclic patterns remain open, never an exact false read.
        if (!active.Add(relation)) return true;
        try { return ContainsSelected(relation.Left, selected, active) || ContainsSelected(relation.Right, selected, active); }
        finally { active.Remove(relation); }
    }

    private static bool SameParticipant(IElement left, IElement right) =>
        ReferenceEquals(left, right) || left is Placeholder a && right is Placeholder b && a.Equals(b) ||
        left is CarrierLocation x && right is CarrierLocation y &&
        x.Carrier.SameIdentityAs(y.Carrier) && x.SameTickRefAs(y);

    private static bool SelectedInRead(Relation read, out HashSet<Placeholder> selected)
    {
        selected = [];
        return read.Left is Relation { Right: Relation variables } && Selected(variables, out selected);
    }

    // Exact selected faces only. StructuralPattern's dual-binding convenience
    // is intentionally not used: an unselected opposed slot remains rigid.
    private static IElement Instantiate(IElement element, Dictionary<Placeholder, IElement> bindings,
        Dictionary<Relation, IElement> memo)
    {
        if (element is Placeholder slot && bindings.TryGetValue(slot, out var value)) return value;
        if (element is not Relation relation) return element;
        if (memo.TryGetValue(relation, out var known)) return known;
        var left = Instantiate(relation.Left, bindings, memo);
        var right = Instantiate(relation.Right, bindings, memo);
        var result = ReferenceEquals(left, relation.Left) && ReferenceEquals(right, relation.Right)
            ? relation : new Relation(left, right);
        memo.Add(relation, result);
        return result;
    }

    private static bool Acyclic(IElement element, HashSet<Relation> active)
    {
        if (element is not Relation relation) return true;
        if (!active.Add(relation)) return false;
        try { return Acyclic(relation.Left, active) && Acyclic(relation.Right, active); }
        finally { active.Remove(relation); }
    }

    private static bool ClosedEntries(Relation scope, out IElement[] entries) =>
        TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);

    private static Relation Unresolved(Relation request) => Result(request, [], [], [request]);
    private static Relation Result(Relation request, IEnumerable<IElement> candidates,
        IEnumerable<IElement> blocked, IEnumerable<IElement> unresolved, IEnumerable<Placeholder>? selected = null) =>
        new(new Relation(request, Closed((selected ?? []).Cast<IElement>().ToArray())), new Relation(Closed(candidates.ToArray()),
            new Relation(Closed(blocked.ToArray()), Closed(unresolved.ToArray()))));
}
