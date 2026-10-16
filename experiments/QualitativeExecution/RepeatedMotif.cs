using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// Reusable native patterns unfolded into distinct ordered occurrences. A seam
/// connects exposed ports; it does not assert that its two sites are identical,
/// equally spaced, or metric translations of one another.
/// </summary>
public sealed class RepeatedMotif
{
    // template = ((variables -> pattern) -> (entry-slot -> exit-slot)) ->
    //            (stable-bindings -> identity-separation-obligations).
    // occurrence = ((identity -> template) -> (entry -> exit)) ->
    //              (instance -> (bindings -> generation-read)).
    // successor = previous-occurrence -> following-occurrence.
    // seam = successor -> (previous-exit -> following-entry).
    // strip = ordered-occurrence-scope -> (successor-scope -> seam-scope).
    // read = (strip -> obligations) -> (passed -> (blocked -> unresolved)).

    public Relation Surface { get; }

    public RepeatedMotif(Relation pattern, Relation variables, Placeholder entrySlot,
        Placeholder exitSlot, Relation? stableBindings = null, Relation? distinctRoles = null)
    {
        var roles = distinctRoles ?? StructuralConstraint.Closed(entrySlot, exitSlot);
        if (!FiniteLaw.ClosedEntries(roles, out var localRoles) ||
            !FiniteLaw.ClosedEntries(variables, out var selected) ||
            localRoles.Any(role => !selected.Any(slot => FiniteLaw.SameParticipant(role, slot))))
            throw new ArgumentException("Local role selection must contain selected slots in a closed scope.");
        var obligations = new List<IElement>();
        for (var i = 0; i < localRoles.Length; i++)
            for (var j = 0; j < i; j++)
                obligations.Add(new Relation(StructuralConstraint.Request(localRoles[i], StructuralConstraint.Closed(),
                    StructuralConstraint.Closed(localRoles[j])), StructuralConstraint.Closed()));
        Surface = new(new Relation(new Relation(variables, pattern), new Relation(entrySlot, exitSlot)),
            new Relation(stableBindings ?? StructuralConstraint.Closed(),
                StructuralConstraint.Closed(obligations.ToArray())));
        if (!Template(Surface, out _, out _, out _, out _, out _, out _))
            throw new ArgumentException("A motif needs selected port slots and closed native binding/role scopes.");
    }

    /// <summary>Freshens selected local roles unless a binding is supplied.
    /// Stable bindings remain the same participants across occurrences. Conflicts
    /// remain in the generation read rather than overwriting an earlier choice.</summary>
    public Relation Generate(IElement? occurrenceIdentity = null, params Relation[] bindings)
    {
        if (!Template(Surface, out var pattern, out var variables, out var entrySlot,
            out var exitSlot, out var stable, out _))
            return new(new Relation(occurrenceIdentity ?? new Placeholder(), Surface), StructuralConstraint.Open(bindings));
        var supplied = FiniteLaw.Entries(stable).Cast<Relation>().Concat(bindings).ToList();
        var slots = FiniteLaw.Entries(variables).Cast<Placeholder>().ToArray();
        foreach (var slot in slots)
            if (!supplied.Any(binding => FiniteLaw.SameParticipant(binding.Left, slot)))
                supplied.Add(new Relation(slot, new Placeholder()));
        var seed = supplied.First(binding => FiniteLaw.SameParticipant(binding.Left, slots[0])).Right;
        var generation = StructuralConstraint.Project(StructuralConstraint.Match(slots[0], variables,
            StructuralConstraint.Closed(seed), supplied.ToArray()), pattern);
        var complete = StructuralConstraint.TryReadUnique(generation, out var instance, out var frame);
        frame = complete ? frame : StructuralConstraint.Closed(supplied.Cast<IElement>().ToArray());
        instance = complete ? instance : new Placeholder();
        var entry = supplied.First(binding => FiniteLaw.SameParticipant(binding.Left, entrySlot)).Right;
        var exit = supplied.First(binding => FiniteLaw.SameParticipant(binding.Left, exitSlot)).Right;
        return new(new Relation(new Relation(occurrenceIdentity ?? new Placeholder(), Surface), new Relation(entry, exit)),
            new Relation(instance, new Relation(frame, generation)));
    }

    /// <summary>Recognizes occurrences of this exact reusable template. Local
    /// sites and occurrence identity vary; the actual generation remains evidence.</summary>
    public Relation Recognize(Relation support)
    {
        Placeholder identity = new(), entry = new(), exit = new(), instance = new(), bindings = new(), generation = new();
        var variables = StructuralConstraint.Closed(identity, entry, exit, instance, bindings, generation);
        var pattern = new Relation(new Relation(new Relation(identity, Surface), new Relation(entry, exit)),
            new Relation(instance, new Relation(bindings, generation)));
        var read = StructuralConstraint.Match(pattern, variables, support);
        StructuralConstraint.TryRead(read, out _, out var candidates, out var blocked, out var unresolved);
        var accepted = new List<IElement>();
        var denied = FiniteLaw.Entries(blocked).ToList();
        var pending = FiniteLaw.Entries(unresolved).ToList();
        foreach (var item in FiniteLaw.Entries(candidates))
        {
            var candidate = (Relation)item;
            StructuralConstraint.TryReadCandidate(candidate, out var offered, out _, out _);
            var good = Validate(offered, out var proof, out var open);
            var qualified = new Relation(new Relation(candidate, proof), candidate.Right);
            if (open) pending.Add(qualified);
            else if (good) accepted.Add(qualified);
            else denied.Add(qualified);
        }
        return new(read.Left, new Relation(StructuralConstraint.Closed(accepted.ToArray()),
            new Relation(StructuralConstraint.Closed(denied.ToArray()), StructuralConstraint.Closed(pending.ToArray()))));
    }

    public static bool TryReadOccurrence(Relation occurrence, out IElement identity,
        out Relation template, out IElement instance, out IElement entry, out IElement exit)
    {
        identity = instance = entry = exit = null!; template = null!;
        return Occurrence(occurrence, out identity, out template, out instance, out entry, out exit,
            out _, out _) && Validate(occurrence, out _, out _);
    }

    /// <summary>Builds explicit succession without manufacturing seam contact.
    /// Open occurrence support remains an open strip prefix.</summary>
    public static Relation Sequence(Relation occurrences, Relation? seams = null)
    {
        if (!StructuralConstraint.TryReadScope(occurrences, out var entries, out var start, out var end))
            return new(occurrences, new Relation(StructuralConstraint.Open(), seams ?? StructuralConstraint.Open()));
        var edges = new List<IElement>();
        for (var i = 1; i < entries.Length; i++) edges.Add(new Relation(entries[i - 1], entries[i]));
        var successors = start.Equals(end) ? StructuralConstraint.Closed(edges.ToArray()) : StructuralConstraint.Open(edges.ToArray());
        return new(occurrences, new Relation(successors, seams ?? StructuralConstraint.Closed()));
    }

    /// <summary>Connects ports through the actual successor relation. Copying an
    /// equal-looking successor does not connect the original one.</summary>
    public static Relation Join(Relation successor)
    {
        if (successor.Left is Relation previous && successor.Right is Relation following &&
            TryReadOccurrence(previous, out _, out _, out _, out _, out var exit) &&
            TryReadOccurrence(following, out _, out _, out _, out var entry, out _))
            return new(successor, new Relation(exit, entry));
        return new(successor, new Placeholder());
    }

    /// <summary>Reads finite sequence, occurrence identity and seam obligations.
    /// Optional template order is a question about this strip, not a classifier.
    /// Closed missing joins are failures; an open prefix stays unresolved.</summary>
    public static Relation Read(Relation strip, Relation? templateOrder = null)
    {
        var obligations = new List<IElement>();
        var passed = new List<IElement>();
        var blocked = new List<IElement>();
        var unresolved = new List<IElement>();
        if (strip.Left is not Relation occurrences || strip.Right is not Relation { Left: Relation successors, Right: Relation seams } ||
            !Scope(occurrences, out var entries, unresolved) || !Scope(successors, out var edges, unresolved) ||
            !Scope(seams, out var joins, unresolved))
        { unresolved.Add(strip); return Result(); }

        var valid = new bool[entries.Length];
        for (var i = 0; i < entries.Length; i++)
        {
            valid[i] = Validate(entries[i], out var proof, out var open);
            obligations.Add(proof);
            (open ? unresolved : valid[i] ? passed : blocked).Add(proof);
            if (!valid[i]) continue;
            Occurrence(entries[i], out var identity, out _, out _, out _, out _, out _, out _);
            for (var j = 0; j < i; j++)
            {
                if (!valid[j]) continue;
                Occurrence(entries[j], out var previousIdentity, out _, out _, out _, out _, out _, out _);
                Separate(entries[j], entries[i]);
                Separate(previousIdentity, identity);
            }
        }

        // Admission of a known edge does not require claiming that an open
        // support is complete. Its query below still retains that uncertainty.
        var requiredEdges = edges.OfType<Relation>().Where(edge =>
            Enumerable.Range(1, Math.Max(0, entries.Length - 1)).Any(i =>
                FiniteLaw.SameParticipant(edge.Left, entries[i - 1]) &&
                FiniteLaw.SameParticipant(edge.Right, entries[i]))).ToList();
        for (var i = 1; i < entries.Length; i++)
        {
            Placeholder first = new(), second = new();
            var query = StructuralConstraint.Match(new Relation(first, second), StructuralConstraint.Closed(first, second),
                successors, new Relation(first, entries[i - 1]), new Relation(second, entries[i]));
            obligations.Add(query);
            if (!StructuralConstraint.TryReadUnique(query, out _, out _))
            { AddFailed(query, successors); continue; }
            StructuralConstraint.TryRead(query, out _, out var candidates, out _, out _);
            StructuralConstraint.TryReadCandidate((Relation)FiniteLaw.Entries(candidates)[0], out var actual, out _, out _);
            var edge = (Relation)actual;
            passed.Add(query);
            if (!valid[i - 1] || !valid[i]) { unresolved.Add(new Relation(edge, strip)); continue; }
            Occurrence(entries[i - 1], out _, out _, out _, out _, out var exit, out _, out _);
            Occurrence(entries[i], out _, out _, out _, out var entry, out _, out _, out _);
            Placeholder join = new(), port = new();
            var seamQuery = StructuralConstraint.Match(new Relation(edge, new Relation(join, port)),
                StructuralConstraint.Closed(join, port), seams, new Relation(join, exit), new Relation(port, entry));
            obligations.Add(seamQuery);
            if (StructuralConstraint.TryReadUnique(seamQuery, out _, out _)) passed.Add(seamQuery);
            else AddFailed(seamQuery, seams);
        }
        foreach (var edge in edges)
            if (!requiredEdges.Any(required => ReferenceEquals(required, edge)))
                (edge is Relation possible && CanAdmitEdge(possible) ? unresolved : blocked).Add(new Relation(edge, successors));
        foreach (var join in joins)
        {
            if (join is not Relation { Left: Relation edge, Right: Relation ports })
            { blocked.Add(new Relation(join, seams)); continue; }
            var admittedEdge = requiredEdges.Any(required => ReferenceEquals(required, edge));
            var publishedEdge = edges.Any(published => ReferenceEquals(published, edge));
            var duplicate = edges.OfType<Relation>().Any(published =>
                FiniteLaw.SameParticipant(published.Left, edge.Left) && FiniteLaw.SameParticipant(published.Right, edge.Right));
            // A seam may precede publication of its edge. It can settle later
            // only if that edge is still admissible and would not duplicate an
            // already published route for this exact neighboring pair.
            if (!admittedEdge && (!CanAdmitEdge(edge) ||
                (!publishedEdge && (FiniteLaw.ClosedEntries(successors, out _) || duplicate))))
            { blocked.Add(new Relation(join, seams)); continue; }
            if (edge.Left is not Relation left || edge.Right is not Relation right ||
                !TryReadOccurrence(left, out _, out _, out _, out _, out var exit) ||
                !TryReadOccurrence(right, out _, out _, out _, out var entry, out _))
            {
                Validate(edge.Left, out _, out var leftOpen);
                Validate(edge.Right, out _, out var rightOpen);
                (leftOpen || rightOpen ? unresolved : blocked).Add(new Relation(join, seams));
            }
            else if (!FiniteLaw.SameParticipant(ports.Left, exit) || !FiniteLaw.SameParticipant(ports.Right, entry))
                blocked.Add(new Relation(join, seams));
            else if (!admittedEdge) unresolved.Add(new Relation(join, seams));
        }

        if (templateOrder is not null)
        {
            if (!Scope(templateOrder, out var expected, unresolved)) unresolved.Add(templateOrder);
            else
            {
                for (var i = 0; i < Math.Min(expected.Length, entries.Length); i++)
                {
                    if (!Occurrence(entries[i], out _, out var actual, out _, out _, out _, out _, out _)) continue;
                    var comparison = Compare(expected[i], actual);
                    obligations.Add(comparison);
                    (FiniteLaw.SameParticipant(expected[i], actual) ? passed : blocked).Add(comparison);
                }
                if (expected.Length != entries.Length)
                    AddFailed(new Relation(templateOrder, occurrences), expected.Length < entries.Length ? templateOrder : occurrences);
            }
        }
        return Result();

        void Separate(IElement first, IElement second)
        {
            var comparison = Compare(first, second);
            obligations.Add(comparison);
            (FiniteLaw.SameParticipant(first, second) ? blocked : passed).Add(comparison);
        }
        void AddFailed(IElement evidence, Relation scope)
        {
            var open = !StructuralConstraint.TryReadScope(scope, out _, out var start, out var end) || !start.Equals(end);
            (open ? unresolved : blocked).Add(evidence);
        }
        bool CanAdmitEdge(Relation edge)
        {
            var left = FiniteLaw.IndexOf(entries, edge.Left);
            var right = FiniteLaw.IndexOf(entries, edge.Right);
            if (left >= 0 && right == left + 1) return true;
            if (FiniteLaw.ClosedEntries(occurrences, out _) || right >= 0 ||
                left >= 0 && left != entries.Length - 1 ||
                FiniteLaw.SameParticipant(edge.Left, edge.Right)) return false;
            if (edges.OfType<Relation>().Any(other => !ReferenceEquals(other, edge) &&
                FiniteLaw.SameParticipant(other.Left, edge.Left) && FiniteLaw.SameParticipant(other.Right, edge.Right))) return false;
            if (!FreshTailParticipant(edge.Right) || left < 0 && !FreshTailParticipant(edge.Left)) return false;
            return !Occurrence(edge.Left, out var a, out _, out _, out _, out _, out _, out _) ||
                !Occurrence(edge.Right, out var b, out _, out _, out _, out _, out _, out _) ||
                !FiniteLaw.SameParticipant(a, b);
        }
        bool FreshTailParticipant(IElement participant)
        {
            if (!Occurrence(participant, out var identity, out _, out _, out _, out _, out _, out _)) return true;
            return !entries.Any(existing => Occurrence(existing, out var known, out _, out _, out _, out _, out _, out _) &&
                FiniteLaw.SameParticipant(identity, known));
        }
        Relation Result() => new(new Relation(strip, StructuralConstraint.Closed(obligations.ToArray())),
            new Relation(StructuralConstraint.Closed(passed.ToArray()),
                new Relation(StructuralConstraint.Closed(blocked.ToArray()), StructuralConstraint.Closed(unresolved.ToArray()))));
    }

    public static bool TryReadResult(Relation read, out Relation passed, out Relation blocked, out Relation unresolved)
    {
        passed = blocked = unresolved = null!;
        // Read can preserve an incomplete request as a native pending witness.
        // This exact reader requires the three exposed strip scopes rather
        // than treating an unrelated common read-witness shape as a strip.
        if (read.Left is not Relation { Left: Relation strip, Right: Relation obligations } ||
            strip.Left is not Relation occurrences ||
            strip.Right is not Relation { Left: Relation successors, Right: Relation seams } ||
            !StructuralConstraint.TryReadScope(occurrences, out _, out _, out _) ||
            !StructuralConstraint.TryReadScope(successors, out _, out _, out _) ||
            !StructuralConstraint.TryReadScope(seams, out _, out _, out _) ||
            !FiniteLaw.ClosedEntries(obligations, out _) ||
            read.Right is not Relation { Left: Relation yes, Right: Relation { Left: Relation no, Right: Relation open } } ||
            !FiniteLaw.ClosedEntries(yes, out _) || !FiniteLaw.ClosedEntries(no, out _) || !FiniteLaw.ClosedEntries(open, out _)) return false;
        passed = yes; blocked = no; unresolved = open; return true;
    }

    private static bool Validate(IElement value, out Relation proof, out bool open)
    {
        var checks = new List<IElement>();
        proof = new(value, StructuralConstraint.Closed()); open = false;
        if (!Occurrence(value, out _, out var surface, out var instance, out var entry, out var exit,
                out var bindings, out var generation) ||
            !Template(surface, out var pattern, out var variables, out var entrySlot, out var exitSlot,
                out var stable, out var distinct))
        { open = true; return false; }
        checks.Add(generation);
        if (!StructuralConstraint.TryReadUnique(generation, out var generated, out var frame))
        { open = true; proof = new(value, StructuralConstraint.Closed(checks.ToArray())); return false; }
        var good = Check(instance, generated) & Check(bindings, frame);
        good &= generation.Left is Relation { Left: Relation application } && Check(application.Right, pattern);
        var roles = FiniteLaw.Entries(bindings).Cast<Relation>().ToArray();
        var recognition = StructuralConstraint.Match(pattern, variables, StructuralConstraint.Closed(instance),
            [.. roles, .. FiniteLaw.Entries(stable).Cast<Relation>()]);
        checks.Add(recognition);
        good &= StructuralConstraint.TryReadUnique(recognition, out _, out _);
        good &= Bound(entrySlot, out var boundEntry) && Check(entry, boundEntry);
        good &= Bound(exitSlot, out var boundExit) && Check(exit, boundExit);
        foreach (var obligation in FiniteLaw.Entries(distinct))
        {
            if (!Separation(obligation, out var first, out var second) ||
                !Bound(first, out var a) || !Bound(second, out var b)) { open = true; continue; }
            checks.Add(new Relation(obligation, Compare(a, b)));
            good &= !FiniteLaw.SameParticipant(a, b);
        }
        proof = new(value, StructuralConstraint.Closed(checks.ToArray()));
        return good && !open;

        bool Check(IElement first, IElement second)
        { checks.Add(Compare(first, second)); return FiniteLaw.SameParticipant(first, second); }
        bool Bound(IElement slot, out IElement participant)
        {
            var binding = roles.FirstOrDefault(candidate => FiniteLaw.SameParticipant(candidate.Left, slot));
            participant = binding?.Right!;
            return binding is not null;
        }
    }

    private static bool Occurrence(IElement value, out IElement identity, out Relation template,
        out IElement instance, out IElement entry, out IElement exit, out Relation bindings, out Relation generation)
    {
        identity = instance = entry = exit = null!; template = bindings = generation = null!;
        if (value is not Relation { Left: Relation { Left: Relation owner, Right: Relation ports },
                Right: Relation { Right: Relation { Left: Relation frame, Right: Relation read } } body } ||
            owner.Right is not Relation surface || !FiniteLaw.ClosedEntries(frame, out var supplied) ||
            supplied.Any(item => item is not Relation { Left: Placeholder })) return false;
        identity = owner.Left; template = surface; entry = ports.Left; exit = ports.Right;
        instance = body.Left; bindings = frame; generation = read; return true;
    }

    private static bool Template(Relation surface, out Relation pattern, out Relation variables,
        out Placeholder entry, out Placeholder exit, out Relation stable, out Relation distinct)
    {
        pattern = variables = stable = distinct = null!; entry = exit = null!;
        if (surface.Left is not Relation { Left: Relation { Left: Relation selected, Right: Relation shape },
                Right: Relation { Left: Placeholder first, Right: Placeholder last } } ||
            surface.Right is not Relation { Left: Relation bindings, Right: Relation roles } ||
            !FiniteLaw.ClosedEntries(selected, out var slots) || slots.Length == 0 ||
            slots.Any(slot => slot is not Placeholder { IsInverse: false }) ||
            slots.Distinct().Count() != slots.Length || !slots.Contains(first) || !slots.Contains(last) ||
            !FiniteLaw.ClosedEntries(bindings, out var fixedBindings) ||
            fixedBindings.Any(binding => binding is not Relation { Left: Placeholder slot } || !slots.Contains(slot)) ||
            !FiniteLaw.ClosedEntries(roles, out var obligations) || obligations.Any(obligation =>
                !Separation(obligation, out var a, out var b) || !slots.Contains(a) || !slots.Contains(b))) return false;
        pattern = shape; variables = selected; entry = first; exit = last; stable = bindings; distinct = roles; return true;
    }

    private static bool Scope(Relation scope, out IElement[] entries, List<IElement> unresolved)
    {
        if (!StructuralConstraint.TryReadScope(scope, out entries, out var start, out var end)) return false;
        if (!start.Equals(end)) unresolved.Add(new Relation(scope, end));
        return true;
    }

    private static Relation Compare(IElement first, IElement second) =>
        StructuralConstraint.Match(first, StructuralConstraint.Closed(), StructuralConstraint.Closed(second));

    private static bool Separation(IElement value, out IElement first, out IElement second)
    {
        first = second = null!;
        if (value is not Relation { Left: Relation query, Right: Relation expected } ||
            query.Left is not Relation { Left: Relation { Right: Relation variables } definition, Right: Relation initial } ||
            query.Right is not Relation support || !FiniteLaw.ClosedEntries(variables, out var slots) || slots.Length != 0 ||
            !FiniteLaw.ClosedEntries(initial, out var bindings) || bindings.Length != 0 ||
            !FiniteLaw.ClosedEntries(expected, out var candidates) || candidates.Length != 0 ||
            !FiniteLaw.ClosedEntries(support, out var offers) || offers.Length != 1) return false;
        first = definition.Left; second = offers[0]; return true;
    }
}
