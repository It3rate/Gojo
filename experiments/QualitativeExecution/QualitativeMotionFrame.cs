using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

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
    public IElement Origin { get; } = new Placeholder();
    public Relation Horizontal { get; } = new(new Placeholder(), new Placeholder());
    public Relation Vertical { get; } = new(new Placeholder(), new Placeholder());
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
        Append(before); Append(incoming);
        // Known unequal extents do not make the formal displacement incomplete.
        // They prevent a resolved net-direction/return claim at this resolution.
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
                var inverse = residual.FindIndex(prior => prior is Relation other &&
                    Direction(other, out _, out _, out var otherReach) &&
                    FiniteLaw.SameParticipant(reach, otherReach) && Opposed(other, move));
                if (inverse < 0) residual.Add(move);
                else
                {
                    cancellations.Add(new Relation(residual[inverse], move));
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

    private bool Accounted(Relation before, Relation incoming, IElement[] survivors, IElement[] pairs,
        IElement[] pending, bool closed)
    {
        var participation = new List<IElement>(); var incomplete = false;
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
        if (participation.Count != 0) return false;
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
                participation.Add(entry);
                if (entry is not Relation move || !Direction(move, out _, out _, out _))
                { incomplete = true; expectedCauses.Add(new Relation(entry, Surface)); }
            }
        }

        bool Consume(IElement entry)
        {
            var at = participation.FindIndex(candidate => ReferenceEquals(candidate, entry));
            if (at < 0) return false;
            participation.RemoveAt(at);
            return true;
        }
    }
}
