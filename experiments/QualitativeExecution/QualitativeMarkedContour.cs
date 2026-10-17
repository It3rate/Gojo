using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// A selected, unmeasured radial path and marked whole traversal. This is a
/// declared construction, not a law that carrier tension generates a circle.
/// Closure and common radial participation do not establish equal angles.
/// </summary>
public sealed class QualitativeMarkedContour
{
    // radius = (center -> actual-marker) -> shared-reach-support
    // whole = reference -> actual-ordered-traversal
    // equal-turn promise = actual-traversal -> Closed(actual-edge -> common-turn)
    // surface = ((center -> reach-support) -> radii) ->
    //           ((markers -> whole) -> (successors -> (whole -> equal-turn-promise)))
    public IElement Center { get; }
    public Relation ReachSupport { get; }
    public Relation Markers { get; }
    public Relation Traversal { get; }
    public Relation Successors => Traversal;
    public IElement Reference { get; }
    public Relation RadialAttachments { get; }
    public Relation WholeTraversal { get; }
    public Relation EqualTurnPromise { get; }
    public Relation Surface { get; }

    public QualitativeMarkedContour(IElement center, Relation reachSupport, Relation markers,
        Relation traversal, IElement reference, Relation? equalTurnPromise = null)
    {
        Center = center; ReachSupport = reachSupport; Markers = markers;
        Traversal = traversal; Reference = reference;
        var radii = E(markers).Select(marker => (IElement)new Relation(new Relation(center, marker), reachSupport)).ToArray();
        RadialAttachments = Closed(markers) ? S(radii) : StructuralConstraint.Open(radii);
        WholeTraversal = new Relation(reference, traversal);
        EqualTurnPromise = new Relation(WholeTraversal, equalTurnPromise ?? StructuralConstraint.Open());
        Surface = new Relation(new Relation(new Relation(center, reachSupport), RadialAttachments),
            new Relation(new Relation(markers, WholeTraversal), new Relation(Successors, EqualTurnPromise)));
    }

    /// <summary>Authors equal subdivision as repeated participation of one
    /// unmeasured turn. The promise is conditional geometry, not a deduction
    /// from the number of markers or an independently verified measurement.</summary>
    public static Relation EqualTurns(Relation traversal, IElement commonTurn) => new(traversal,
        Closed(traversal) ? S(E(traversal).Select(edge => (IElement)new Relation(edge, commonTurn)).ToArray())
            : StructuralConstraint.Open(E(traversal).Select(edge => (IElement)new Relation(edge, commonTurn)).ToArray()));

    public QualitativeMarkedContour WithEqualTurns(IElement commonTurn) =>
        new(Center, ReachSupport, Markers, Traversal, Reference, EqualTurns(Traversal, commonTurn));

    // read = actual-surface -> (admitted-turns -> (violations -> unresolved))
    public Relation Evaluate() => Inspect(out _, out _);

    /// <summary>Freshly checks the receipt's native evidence. Rebuilt evidence
    /// wrappers are allowed; omitted or substituted evidence is not.</summary>
    public bool TryReadResult(Relation read, out Relation admittedTurns,
        out Relation violations, out Relation unresolved)
    {
        admittedTurns = violations = unresolved = null!;
        if (!ReadScopes(read, Surface, out var turns, out var failed, out var pending)) return false;
        var fresh = Inspect(out _, out _);
        ReadScopes(fresh, Surface, out var actualTurns, out var actualFailed, out var actualPending);
        if (!SameEntries(turns, actualTurns, false) || !SameEntries(failed, actualFailed, true) || !SameEntries(pending, actualPending, true)) return false;
        admittedTurns = turns; violations = failed; unresolved = pending; return true;
    }

    /// <summary>A valid topological whole may coexist with an unresolved
    /// equal-turn obligation. The returned whole retains its nonempty walk.</summary>
    public bool TryReadClosedContour(Relation read, out Relation whole)
    {
        whole = null!;
        if (!TryReadResult(read, out _, out _, out _)) return false;
        Inspect(out var closed, out _);
        if (!closed) return false;
        whole = WholeTraversal; return true;
    }

    public bool TryReadEqualTurns(Relation read, out IElement commonTurn)
    {
        commonTurn = null!;
        if (!TryReadResult(read, out _, out _, out _)) return false;
        Inspect(out _, out var turn);
        if (turn is null) return false;
        commonTurn = turn; return true;
    }

    /// <summary>Checks a supplied bijection's commuting successor squares.
    /// This certifies the selected discrete action, not Euclidean angles.</summary>
    public Relation CompareAction(Relation directionDomain, Relation directionSuccessors, Relation markerToDirection)
    {
        var request = new Relation(Surface, new Relation(directionDomain, new Relation(directionSuccessors, markerToDirection)));
        var matched = new List<IElement>(); var failed = new List<IElement>(); var pending = new List<IElement>();
        Inspect(out var contourClosed, out _);
        if (!contourClosed) pending.Add(WholeTraversal);
        if (!FiniteLaw.DomainEntries(directionDomain, out var directions) || !Closed(directionSuccessors) || !Closed(markerToDirection))
            pending.Add(new Relation(directionDomain, new Relation(directionSuccessors, markerToDirection)));
        else
        {
            var markers = E(Markers); var maps = E(markerToDirection).OfType<Relation>().ToArray();
            var edges = E(directionSuccessors).OfType<Relation>().ToArray();
            if (maps.Length != E(markerToDirection).Length || edges.Length != E(directionSuccessors).Length ||
                directions.Length != markers.Length || maps.Length != markers.Length || edges.Length != directions.Length ||
                !Unique(maps.Select(row => row.Left)) || !Unique(maps.Select(row => row.Right)) ||
                !Unique(edges.Select(row => row.Left)) || !Unique(edges.Select(row => row.Right)) ||
                maps.Any(row => !Member(markers, row.Left) || !Member(directions, row.Right)) ||
                edges.Any(row => !Member(directions, row.Left) || !Member(directions, row.Right)))
                failed.Add(new Relation(directionSuccessors, markerToDirection));
            else foreach (var entry in E(Traversal))
            {
                if (entry is not Relation edge) { failed.Add(entry); continue; }
                var from = maps.SingleOrDefault(row => Same(row.Left, edge.Left));
                var to = maps.SingleOrDefault(row => Same(row.Left, edge.Right));
                var target = from is null || to is null ? null : edges.SingleOrDefault(row => Same(row.Left, from.Right) && Same(row.Right, to.Right));
                if (from is null || to is null || target is null) failed.Add(new Relation(edge, markerToDirection));
                else matched.Add(new Relation(edge, new Relation(from, new Relation(to, target))));
            }
        }
        return Receipt(request, matched, failed, pending);
    }

    public bool TryReadActionResult(Relation read, out Relation matched, out Relation violations, out Relation unresolved)
    {
        matched = violations = unresolved = null!;
        if (read.Left is not Relation { Right: Relation inputs } request || !ReferenceEquals(request.Left, Surface) ||
            inputs.Left is not Relation domain || inputs.Right is not Relation { Left: Relation successors, Right: Relation map } ||
            !ReadScopes(read, request, out var yes, out var no, out var pending)) return false;
        var fresh = CompareAction(domain, successors, map);
        ReadScopes(fresh, fresh.Left, out var actualYes, out var actualNo, out var actualPending);
        if (!SameEntries(yes, actualYes, true, request) || !SameEntries(no, actualNo, true, request) || !SameEntries(pending, actualPending, true, request)) return false;
        matched = yes; violations = no; unresolved = pending; return true;
    }

    private Relation Inspect(out bool contourClosed, out IElement? commonTurn)
    {
        var turns = new List<IElement>(); var failed = new List<IElement>(); var pending = new List<IElement>();
        var markers = E(Markers); var walk = E(Traversal);
        if (!Connected()) pending.Add(Surface);
        if (!Closed(Markers)) pending.Add(Markers);
        if (!Unique(markers) || markers.Length < 2 || Member(markers, Center)) failed.Add(new Relation(Center, Markers));
        if (!Member(markers, Reference)) (Closed(Markers) ? failed : pending).Add(new Relation(Reference, Markers));
        if (!FiniteLaw.ClosedEntries(ReachSupport, out var reaches) || reaches.Length != 1) pending.Add(ReachSupport);
        if (Measured(Surface)) pending.Add(new Relation(ReachSupport, Surface));
        var radii = E(RadialAttachments).OfType<Relation>().ToArray();
        if (!Closed(RadialAttachments) || radii.Length != E(RadialAttachments).Length || radii.Length != markers.Length || radii.Any(row => row.Left is not Relation ends ||
                !Same(ends.Left, Center) || !Member(markers, ends.Right) || !ReferenceEquals(row.Right, ReachSupport)) ||
            !Unique(radii.Select(row => ((Relation)row.Left).Right))) pending.Add(RadialAttachments);
        if (!Closed(Traversal)) pending.Add(Traversal);
        IElement expected = Reference;
        foreach (var entry in walk)
        {
            if (entry is not Relation edge) { failed.Add(entry); continue; }
            if (!Member(markers, edge.Left) || !Member(markers, edge.Right))
                (Closed(Markers) ? failed : pending).Add(new Relation(edge, Markers));
            else if (!Same(expected, edge.Left)) failed.Add(new Relation(expected, edge));
            else turns.Add(edge);
            expected = edge.Right;
        }
        if (walk.Length == 0 || !Same(expected, Reference)) pending.Add(new Relation(WholeTraversal, new Relation(expected, Reference)));
        if (Closed(Traversal) && (walk.Length != markers.Length || !Unique(walk.OfType<Relation>().Select(edge => edge.Left))))
            failed.Add(new Relation(Markers, Traversal));
        contourClosed = failed.Count == 0 && pending.Count == 0;
        commonTurn = null;
        if (EqualTurnPromise.Right is not Relation { Right: Relation bindings } promise || !ReferenceEquals(promise.Left, Traversal) || !Closed(bindings))
            pending.Add(EqualTurnPromise);
        else
        {
            var rows = E(bindings).OfType<Relation>().ToArray();
            if (rows.Length != walk.Length || rows.Length == 0 || rows.Length != E(bindings).Length ||
                !Unique(rows.Select(row => row.Left)) || rows.Any(row => !walk.Any(edge => ReferenceEquals(edge, row.Left))))
                failed.Add(EqualTurnPromise);
            else if (!rows.All(row => Same(row.Right, rows[0].Right))) failed.Add(EqualTurnPromise);
            else if (contourClosed) commonTurn = rows[0].Right;
        }
        return Receipt(Surface, turns, failed, pending);
    }

    private bool Connected() => Surface.Left is Relation { Left: Relation radial, Right: var radii } &&
        ReferenceEquals(radial.Left, Center) && ReferenceEquals(radial.Right, ReachSupport) && ReferenceEquals(radii, RadialAttachments) &&
        Surface.Right is Relation { Left: Relation whole, Right: Relation law } && ReferenceEquals(whole.Left, Markers) &&
        ReferenceEquals(whole.Right, WholeTraversal) && ReferenceEquals(WholeTraversal.Left, Reference) && ReferenceEquals(WholeTraversal.Right, Traversal) &&
        ReferenceEquals(law.Left, Successors) && ReferenceEquals(law.Right, EqualTurnPromise) && ReferenceEquals(EqualTurnPromise.Left, WholeTraversal);

    private static Relation Receipt(IElement request, IEnumerable<IElement> yes, IEnumerable<IElement> no, IEnumerable<IElement> pending) =>
        new(request, new Relation(S(yes.ToArray()), new Relation(S(no.ToArray()), S(pending.ToArray()))));
    private static bool ReadScopes(Relation read, IElement request, out Relation yes, out Relation no, out Relation pending)
    {
        yes = no = pending = null!;
        if (!ReferenceEquals(read.Left, request) || read.Right is not Relation { Left: Relation a, Right: Relation { Left: Relation b, Right: Relation c } } ||
            !Closed(a) || !Closed(b) || !Closed(c)) return false;
        yes = a; no = b; pending = c; return true;
    }
    private bool SameEntries(Relation a, Relation b, bool wrappers, IElement? premise = null)
    {
        var anchors = Participants(premise ?? Surface);
        return E(a).Length == E(b).Length && E(a).Zip(E(b)).All(pair => wrappers
            ? SameEvidence(pair.First, pair.Second, anchors, new()) : Same(pair.First, pair.Second));
    }
    private static bool SameEvidence(IElement a, IElement b, HashSet<IElement> anchors, HashSet<(IElement, IElement)> visited)
    {
        if (Same(a, b)) return true;
        if (anchors.Contains(a) || anchors.Contains(b) || a is not Relation x || b is not Relation y) return false;
        if (!visited.Add((a, b))) return true;
        return SameEvidence(x.Left, y.Left, anchors, visited) && SameEvidence(x.Right, y.Right, anchors, visited);
    }
    private static bool Measured(IElement root) => Participants(root).Any(item => item is Carrier or CarrierLocation);
    private static HashSet<IElement> Participants(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var item))
        {
            if (!seen.Add(item)) continue;
            if (item is Relation pair) { stack.Push(pair.Left); stack.Push(pair.Right); }
        }
        return seen;
    }
    private static bool Unique(IEnumerable<IElement> items)
    {
        var seen = new List<IElement>();
        foreach (var item in items) { if (Member(seen, item)) return false; seen.Add(item); }
        return true;
    }
    private static bool Member(IEnumerable<IElement> items, IElement item) => items.Any(other => Same(item, other));
    private static bool Same(IElement a, IElement b) => FiniteLaw.SameParticipant(a, b);
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static bool Closed(Relation scope) => FiniteLaw.ClosedEntries(scope, out _);
}
