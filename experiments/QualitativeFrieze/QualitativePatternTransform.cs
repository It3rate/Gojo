using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Explicit local move-word substitution. A shared unmeasured source
/// step may become several target participations, as in a shear. This retains
/// a structural correspondence, not a claim of Euclidean length preservation.</summary>
public sealed class QualitativePatternTransform
{
    // Surface = (source-frame -> target-frame) -> (substitution-rows -> evidence)
    // read = (Surface -> actual-input-word) ->
    //        (mapped-word -> (blocked -> (unresolved -> consumed-rows)))
    // consumed = actual input move occurrence -> actual substitution row
    public QualitativeMotionFrame Source { get; }
    public QualitativeMotionFrame Target { get; }
    public Relation Rows { get; }
    public Relation Surface { get; }

    public QualitativePatternTransform(QualitativeMotionFrame source, QualitativeMotionFrame target, Relation rows, IElement? evidence = null)
    { Source = source; Target = target; Rows = rows; Surface = new(new Relation(source.Surface, target.Surface), new Relation(rows, evidence ?? S())); }

    public Relation Apply(Relation word)
    {
        var terms = new List<IElement>(); var blocked = new List<IElement>(); var pending = new List<IElement>(); var proofs = new List<IElement>();
        if (!Scope(word, out var input, out var inputClosed)) { pending.Add(word); return Receipt(); }
        if (!inputClosed) pending.Add(word);
        if (!Scope(Rows, out var rows, out var rowsClosed)) { pending.Add(Rows); return Receipt(); }
        if (!rowsClosed) pending.Add(Rows);
        foreach (var term in input)
        {
            if (term is not Relation move || !Source.Direction(move, out _, out _, out _))
            { blocked.Add(term); continue; }
            var matches = rows.OfType<Relation>().Where(row => row.Left is Relation pattern && Same(pattern.Left, move.Left) && Same(pattern.Right, move.Right)).ToArray();
            if (matches.Length != 1) { pending.Add(term); continue; }
            var selected = matches[0];
            if (selected.Right is not Relation result || !Scope(result, out var images, out var complete))
            { pending.Add(selected); continue; }
            if (!complete) pending.Add(selected);
            if (images.Any(image => image is not Relation item || !Target.Direction(item, out _, out _, out _)))
            { blocked.Add(selected); continue; }
            terms.AddRange(images); proofs.Add(new Relation(term, selected));
        }
        foreach (var row in rows)
            if (row is not Relation { Left: Relation, Right: Relation }) pending.Add(row);
        return Receipt();
        Relation Receipt() => new(new Relation(Surface, word), new Relation(
            pending.Count == 0 && blocked.Count == 0 ? S(terms.ToArray()) : StructuralConstraint.Open(terms.ToArray()),
            new Relation(S(blocked.ToArray()), new Relation(S(pending.ToArray()), S(proofs.ToArray())))));
    }

    /// <summary>Rechecks the actual substitution rather than accepting copied
    /// favourable output fields. This read observes; it does not mutate a path.</summary>
    public bool TryReadResult(Relation read, out Relation output, out Relation blocked, out Relation unresolved, out Relation provenance)
    {
        output = blocked = unresolved = provenance = null!;
        if (read.Left is not Relation request || !ReferenceEquals(request.Left, Surface) || request.Right is not Relation word ||
            !Parts(read, out var result, out var refused, out var pending, out var proofs)) return false;
        var fresh = Apply(word);
        Parts(fresh, out var expected, out var expectedBlocked, out var expectedPending, out var expectedProofs);
        if (!SameScope(result, expected) || !SameScope(refused, expectedBlocked) || !SameScope(pending, expectedPending)) return false;
        var actualRows = E(proofs); var wantedRows = E(expectedProofs);
        if (actualRows.Length != wantedRows.Length) return false;
        for (var i = 0; i < actualRows.Length; i++)
            if (actualRows[i] is not Relation a || wantedRows[i] is not Relation b || !ReferenceEquals(a.Left, b.Left) || !ReferenceEquals(a.Right, b.Right)) return false;
        output = result; blocked = refused; unresolved = pending; provenance = proofs; return true;
    }

    public bool TryReadComplete(Relation read, out Relation output)
    {
        output = null!;
        if (!TryReadResult(read, out var result, out var blocked, out var pending, out _) || !Scope(result, out _, out var closed) ||
            !closed || E(blocked).Length != 0 || E(pending).Length != 0) return false;
        output = result; return true;
    }

    private static bool Parts(Relation read, out Relation output, out Relation blocked, out Relation unresolved, out Relation proofs)
    {
        output = blocked = unresolved = proofs = null!;
        if (read.Right is not Relation { Left: Relation result, Right: Relation rest } || rest.Left is not Relation denied ||
            rest.Right is not Relation { Left: Relation pending, Right: Relation evidence } || !Scope(result, out _, out _) ||
            !Scope(denied, out _, out var noClosed) || !noClosed || !Scope(pending, out _, out var pendingClosed) || !pendingClosed ||
            !Scope(evidence, out _, out var proofsClosed) || !proofsClosed) return false;
        output = result; blocked = denied; unresolved = pending; proofs = evidence; return true;
    }
    private static bool SameScope(Relation a, Relation b) => Scope(a, out var aa, out var ac) && Scope(b, out var bb, out var bc) &&
        ac == bc && aa.Length == bb.Length && aa.Zip(bb).All(pair => ReferenceEquals(pair.First, pair.Second));
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static bool Scope(Relation scope, out IElement[] entries, out bool closed)
    {
        closed = false;
        if (!StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail)) return false;
        closed = seam.Equals(tail); return true;
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
