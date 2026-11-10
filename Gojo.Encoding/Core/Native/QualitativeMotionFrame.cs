using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Encoding.Core.Native;

/// <summary>
/// A declared unmeasured additive interpretation of motion on two independent
/// ordered axes. Screen direction names are presentation aliases. Motion history
/// remains ordered; only the selected displacement read permits exchange.
/// </summary>
public sealed class QualitativeMotionFrame
{
    // axis = negative-end -> positive-end; reverse = positive-end -> negative-end
    // move = direction -> shared-reach
    // cancellation = selected-reach -> (Closed(move, inverse-move) -> Closed())
    // exchange = (selected-slots -> direction-memberships) ->
    //            (Closed(first-move, second-move) -> Closed(second-move, first-move))
    // Surface = (origin -> (horizontal -> vertical)) ->
    //           (opposites -> (cancellation-laws -> exchange))
    // witness = (Surface -> (before -> incoming)) ->
    //           (unmatched-moves -> (cancellation-pairs -> unresolved))
    // Cancellation pairs retain the actual two move occurrences. Repetition in
    // a scope is repeated participation, never a numeric coefficient in disguise.
    public IElement Origin { get; }
    public Relation Horizontal { get; }
    public Relation Vertical { get; }
    public Relation Right { get; }
    public Relation Left { get; }
    public Relation Up { get; }
    public Relation Down { get; }
    public Relation Opposites { get; }
    public Relation CancellationLaws { get; }
    public Relation Exchange { get; }
    public Relation Surface { get; }

    public QualitativeMotionFrame()
    {
        Origin = new Placeholder();
        Horizontal = new(new Placeholder(), new Placeholder());
        Vertical = new(new Placeholder(), new Placeholder());
        Right = Horizontal; Left = Horizontal.Reverse();
        Up = Vertical; Down = Vertical.Reverse();
        Opposites = StructuralConstraint.Closed(new Relation(Right, Left), new Relation(Left, Right),
            new Relation(Up, Down), new Relation(Down, Up));
        var cancellations = new List<IElement>();
        foreach (var entry in FiniteLaw.Entries(Opposites).Cast<Relation>())
        {
            var reach = new Placeholder();
            cancellations.Add(new Relation(StructuralConstraint.Closed(reach),
                new Relation(StructuralConstraint.Closed(Move(entry.Left, reach), Move(entry.Right, reach)),
                    StructuralConstraint.Closed())));
        }
        CancellationLaws = StructuralConstraint.Closed(cancellations.ToArray());
        var directionA = new Placeholder(); var directionB = new Placeholder();
        var reachA = new Placeholder(); var reachB = new Placeholder();
        var directions = StructuralConstraint.Closed(Right, Left, Up, Down);
        var memberships = StructuralConstraint.Closed(
            StructuralConstraint.Request(directionA, StructuralConstraint.Closed(directionA), directions),
            StructuralConstraint.Request(directionB, StructuralConstraint.Closed(directionB), directions));
        var first = Move(directionA, reachA); var second = Move(directionB, reachB);
        Exchange = new Relation(new Relation(StructuralConstraint.Closed(directionA, directionB, reachA, reachB), memberships),
            new Relation(StructuralConstraint.Closed(first, second), StructuralConstraint.Closed(second, first)));
        Surface = new Relation(new Relation(Origin, new Relation(Horizontal, Vertical)),
            new Relation(Opposites, new Relation(CancellationLaws, Exchange)));
    }

    private QualitativeMotionFrame(Relation surface, IElement origin, Relation horizontal,
        Relation vertical, Relation left, Relation down, Relation opposites,
        Relation cancellationLaws, Relation exchange)
    {
        Surface = surface; Origin = origin; Horizontal = Right = horizontal; Vertical = Up = vertical;
        Left = left; Down = down; Opposites = opposites; CancellationLaws = cancellationLaws; Exchange = exchange;
    }

    /// <summary>Rebinds the exact declared two-axis grammar, retaining its native
    /// sites, direction identities and laws. It constructs no replacement basis.</summary>
    public static QualitativeMotionFrame FromSurface(Relation surface) =>
        TryFromSurface(surface, out var frame) ? frame :
            throw new ArgumentException("Expected an exact qualitative motion frame.", nameof(surface));

    public static bool TryFromSurface(Relation surface, out QualitativeMotionFrame frame)
    {
        frame = null!;
        if (surface is not { Left: Relation { Left: var origin, Right: Relation axes },
                Right: Relation { Left: Relation opposites, Right: Relation laws } } ||
            axes.Left is not Relation horizontal || axes.Right is not Relation vertical ||
            laws.Left is not Relation cancellations || laws.Right is not Relation exchange ||
            !FiniteLaw.ClosedEntries(opposites, out var pairs) ||
            pairs is not [Relation first, Relation second, Relation third, Relation fourth] ||
            first.Right is not Relation left || third.Right is not Relation down ||
            !Pair(first, horizontal, left) || !Pair(second, left, horizontal) ||
            !Pair(third, vertical, down) || !Pair(fourth, down, vertical) ||
            !Pair(left, horizontal.Right, horizontal.Left) || !Pair(down, vertical.Right, vertical.Left) ||
            ReferenceEquals(horizontal, left) || ReferenceEquals(vertical, down)) return false;

        // These are independently declared roles, not coincident coordinates or
        // aliases of one another. No recursive search can supply a missing role.
        var roles = new HashSet<Placeholder>();
        bool Role(IElement element) => element is Placeholder { IsInverse: false } slot && roles.Add(slot);
        if (!Role(origin) || !Role(horizontal.Left) || !Role(horizontal.Right) ||
            !Role(vertical.Left) || !Role(vertical.Right) ||
            !FiniteLaw.ClosedEntries(cancellations, out var rows) || rows.Length != pairs.Length) return false;
        for (var i = 0; i < rows.Length; i++)
        {
            if (rows[i] is not Relation { Left: Relation selected, Right: Relation reduction } ||
                !FiniteLaw.ClosedEntries(selected, out var slots) || slots is not [var reach] || !Role(reach) ||
                reduction.Left is not Relation before || !FiniteLaw.ClosedEntries(before, out var moves) ||
                moves is not [Relation a, Relation b] || !ClosedMatches(reduction.Right) ||
                !Pair(a, ((Relation)pairs[i]).Left, reach) || !Pair(b, ((Relation)pairs[i]).Right, reach)) return false;
        }

        if (exchange is not { Left: Relation { Left: Relation selectedRoles, Right: Relation memberships },
                Right: Relation { Left: Relation beforeExchange, Right: Relation afterExchange } } ||
            !FiniteLaw.ClosedEntries(selectedRoles, out var exchangeRoles) ||
            exchangeRoles is not [var directionA, var directionB, var reachA, var reachB] ||
            !exchangeRoles.All(Role) || !FiniteLaw.ClosedEntries(memberships, out var requests) ||
            requests is not [Relation requestA, Relation requestB] ||
            !Membership(requestA, directionA, out var directions) ||
            !Membership(requestB, directionB, out var otherDirections) || !ReferenceEquals(directions, otherDirections) ||
            !ClosedMatches(directions, horizontal, left, vertical, down) ||
            !FiniteLaw.ClosedEntries(beforeExchange, out var ordered) || ordered is not [Relation moveA, Relation moveB] ||
            !Pair(moveA, directionA, reachA) || !Pair(moveB, directionB, reachB) ||
            !ClosedMatches(afterExchange, moveB, moveA)) return false;

        frame = new(surface, origin, horizontal, vertical, left, down, opposites, cancellations, exchange);
        return true;
    }

    private static bool Pair(Relation relation, IElement left, IElement right) =>
        FiniteLaw.SameParticipant(relation.Left, left) && FiniteLaw.SameParticipant(relation.Right, right);

    private static bool ClosedMatches(IElement surface, params IElement[] expected) =>
        surface is Relation scope && FiniteLaw.ClosedEntries(scope, out var entries) &&
        entries.Length == expected.Length && entries.Zip(expected).All(pair => FiniteLaw.SameParticipant(pair.First, pair.Second));

    private static bool Membership(Relation request, IElement direction, out Relation directions)
    {
        directions = null!;
        if (request is not { Left: Relation { Left: Relation pattern, Right: var initial }, Right: Relation support } ||
            !FiniteLaw.SameParticipant(pattern.Left, direction) || !ClosedMatches(pattern.Right, direction) ||
            !ClosedMatches(initial)) return false;
        directions = support; return true;
    }

    public static Relation Move(IElement direction, IElement reach) => new(direction, reach);

    /// <summary>Reads this frame's declared axis and traversal face. The bool is
    /// a presentation read of native order, not a direction flag in the graph.</summary>
    public bool Direction(Relation move, out Relation axis, out bool forward, out IElement reach)
    {
        axis = null!; forward = false; reach = null!;
        if (ReferenceEquals(move.Left, Right)) { axis = Horizontal; forward = true; }
        else if (ReferenceEquals(move.Left, Left)) axis = Horizontal;
        else if (ReferenceEquals(move.Left, Up)) { axis = Vertical; forward = true; }
        else if (ReferenceEquals(move.Left, Down)) axis = Vertical;
        else return false;
        reach = move.Right;
        return true;
    }

    /// <summary>Projects ordered motion into formal additive displacement.
    /// Opposite directions cancel only a shared extent identity. Distinct
    /// extents remain complete known terms with unresolved net-comparison
    /// evidence. Unknown participation instead keeps the result scope open.</summary>
    public Relation Reduce(Relation before, Relation incoming)
    {
        var residual = new List<IElement>(); var cancellations = new List<IElement>();
        var unresolved = new List<IElement>(); var incomplete = false;
        // An ephemeral index of actual native direction participation. It only
        // rules out absent opposite candidates; it does not replace reach
        // identity, ordered cancellation or retained uncertainty evidence.
        var directionCounts = new int[4];
        Append(before); Append(incoming);
        // Known unequal extents do not make the formal displacement incomplete.
        // They prevent a resolved net-direction/return claim at this resolution.
        if (directionCounts[0] != 0 && directionCounts[1] != 0 || directionCounts[2] != 0 && directionCounts[3] != 0)
            for (var i = 0; i < residual.Count; i++)
                for (var j = i + 1; j < residual.Count; j++)
                    if (residual[i] is Relation a && residual[j] is Relation b &&
                        Direction(a, out var axisA, out var faceA, out _) &&
                        Direction(b, out var axisB, out var faceB, out _) &&
                        ReferenceEquals(axisA, axisB) && faceA != faceB)
                        unresolved.Add(new Relation(a, b));
        var result = incomplete ? StructuralConstraint.Open(residual.ToArray()) : StructuralConstraint.Closed(residual.ToArray());
        return new Relation(new Relation(Surface, new Relation(before, incoming)),
            new Relation(result, new Relation(StructuralConstraint.Closed(cancellations.ToArray()),
                StructuralConstraint.Closed(unresolved.ToArray()))));

        void Append(Relation scope)
        {
            if (!StructuralConstraint.TryReadScope(scope, out var entries, out var seam, out var tail))
            { incomplete = true; unresolved.Add(new Relation(scope, Surface)); return; }
            if (!seam.Equals(tail))
            { incomplete = true; unresolved.Add(new Relation(scope, tail)); }
            foreach (var entry in entries)
            {
                if (entry is not Relation move || !Direction(move, out _, out _, out var reach))
                {
                    incomplete = true; residual.Add(entry); unresolved.Add(new Relation(entry, Surface));
                    continue;
                }
                var directionIndex = DirectionIndex(move);
                var inverse = directionCounts[directionIndex ^ 1] == 0 ? -1 : residual.FindIndex(prior => prior is Relation other &&
                    Direction(other, out _, out _, out var otherReach) &&
                    FiniteLaw.SameParticipant(reach, otherReach) && Opposed(other, move));
                if (inverse < 0) { residual.Add(move); directionCounts[directionIndex]++; }
                else
                {
                    cancellations.Add(new Relation(residual[inverse], move));
                    directionCounts[DirectionIndex((Relation)residual[inverse])]--;
                    residual.RemoveAt(inverse);
                }
            }
        }
    }

    /// <summary>Inspects a witness in this exact frame and verifies its known
    /// participation accounting. Empty displacement never asserts that two
    /// independently identified physical sites have become one site.</summary>
    public bool TryReadResult(Relation witness, out Relation residual,
        out Relation cancellations, out Relation unresolved)
    {
        residual = cancellations = unresolved = null!;
        if (witness.Left is not Relation { Right: Relation sources } selected ||
            !ReferenceEquals(selected.Left, Surface) || sources.Left is not Relation before || sources.Right is not Relation incoming ||
            witness.Right is not Relation { Left: Relation remaining, Right: Relation evidence } ||
            evidence.Left is not Relation paired || evidence.Right is not Relation pending ||
            !StructuralConstraint.TryReadScope(remaining, out var survivors, out var seam, out var tail) ||
            !FiniteLaw.ClosedEntries(paired, out var pairs) || !FiniteLaw.ClosedEntries(pending, out var causes) ||
            !Accounted(before, incoming, survivors, pairs, causes, seam.Equals(tail))) return false;
        residual = remaining; cancellations = paired; unresolved = pending;
        return true;
    }

    private bool Opposed(Relation a, Relation b) => FiniteLaw.Entries(Opposites).Any(entry =>
        entry is Relation pair && ReferenceEquals(pair.Left, a.Left) && ReferenceEquals(pair.Right, b.Left));

    private int DirectionIndex(Relation move) => ReferenceEquals(move.Left, Right) ? 0 :
        ReferenceEquals(move.Left, Left) ? 1 : ReferenceEquals(move.Left, Up) ? 2 :
        ReferenceEquals(move.Left, Down) ? 3 : -1;

    private bool Accounted(Relation before, Relation incoming, IElement[] survivors, IElement[] pairs,
        IElement[] pending, bool closed)
    {
        // The contract consumes actual occurrence identities as a multiset.
        // This per-read index preserves repeated aliases without repeatedly
        // searching and shifting a long list of source occurrences.
        var participation = new Dictionary<IElement, int>(ReferenceEqualityComparer.Instance);
        var participationCount = 0; var incomplete = false;
        var expectedCauses = new List<Relation>();
        Collect(before); Collect(incoming);
        if (closed && incomplete) return false;
        foreach (var entry in pairs)
        {
            if (entry is not Relation { Left: Relation a, Right: Relation b } ||
                !Direction(a, out _, out _, out var reachA) || !Direction(b, out _, out _, out var reachB) ||
                !Opposed(a, b) || !FiniteLaw.SameParticipant(reachA, reachB) || !Consume(a) || !Consume(b)) return false;
        }
        foreach (var survivor in survivors) if (!Consume(survivor)) return false;
        if (participationCount != 0) return false;
        var directions = 0;
        foreach (var survivor in survivors)
        {
            if (survivor is not Relation move) continue;
            var index = DirectionIndex(move);
            if (index >= 0) directions |= 1 << index;
        }
        if ((directions & 3) == 3 || (directions & 12) == 12)
            for (var i = 0; i < survivors.Length; i++)
                for (var j = i + 1; j < survivors.Length; j++)
                    if (survivors[i] is Relation a && survivors[j] is Relation b &&
                        Direction(a, out var axisA, out var faceA, out _) &&
                        Direction(b, out var axisB, out var faceB, out _) &&
                        ReferenceEquals(axisA, axisB) && faceA != faceB)
                        expectedCauses.Add(new Relation(a, b));
        foreach (var entry in pending)
        {
            if (entry is not Relation cause) return false;
            var at = expectedCauses.FindIndex(expected =>
                FiniteLaw.SameParticipant(cause.Left, expected.Left) && FiniteLaw.SameParticipant(cause.Right, expected.Right));
            if (at < 0) return false;
            expectedCauses.RemoveAt(at);
        }
        return expectedCauses.Count == 0;

        void Collect(Relation scope)
        {
            if (!StructuralConstraint.TryReadScope(scope, out var entries, out var seam, out var tail))
            { incomplete = true; expectedCauses.Add(new Relation(scope, Surface)); return; }
            if (!seam.Equals(tail))
            { incomplete = true; expectedCauses.Add(new Relation(scope, tail)); }
            foreach (var entry in entries)
            {
                participation.TryGetValue(entry, out var occurrences);
                participation[entry] = checked(occurrences + 1); participationCount = checked(participationCount + 1);
                if (entry is not Relation move || !Direction(move, out _, out _, out _))
                { incomplete = true; expectedCauses.Add(new Relation(entry, Surface)); }
            }
        }

        bool Consume(IElement entry)
        {
            if (!participation.TryGetValue(entry, out var occurrences)) return false;
            if (occurrences == 1) participation.Remove(entry);
            else participation[entry] = occurrences - 1;
            participationCount--;
            return true;
        }
    }
}
