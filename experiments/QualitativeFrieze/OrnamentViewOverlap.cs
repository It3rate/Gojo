using Gojo.Core;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Many-to-many correspondence between two observation covers over
/// the same actual source. Shared source chambers, strokes and sites are
/// distinct grades of overlap. In particular shared strokes alone establish
/// no common chamber, shared sites alone establish no shared stroke, and
/// none of them makes a transitive equivalence relation between groups.</summary>
public sealed class OrnamentViewOverlap
{
    // Surface = (left.Surface -> right.Surface) -> comparison context
    // correspondence = (actual left group read -> actual right group read) ->
    //   ((shared faces -> (left-only faces -> right-only faces)) ->
    //    ((shared strokes -> (left-only strokes -> right-only strokes)) ->
    //     (shared sites -> (left-only sites -> right-only sites))))
    // read = Surface -> (correspondences -> (unrelated group pairs ->
    //             ((left complement -> right complement) -> (blocked -> unresolved))))
    public OrnamentView Left { get; }
    public OrnamentView Right { get; }
    public IElement Context { get; }
    public Relation Surface { get; }
    public Relation Correspondences { get; }
    public Relation Unrelated { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public bool IsComplete => N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0;
    private readonly Relation read;

    public OrnamentViewOverlap(OrnamentView left, OrnamentView right, IElement context)
    {
        Left = left; Right = right; Context = context; Surface = new(new Relation(left.Surface, right.Surface), context);
        var correspondences = new List<IElement>(); var unrelated = new List<IElement>();
        var blocked = new List<IElement>(); var pending = new List<IElement>();
        var compatible = ReferenceEquals(left.Source.Surface, right.Source.Surface);
        if (!compatible) blocked.Add(new Relation(left.Source.Surface, right.Source.Surface));
        if (!left.IsComplete) pending.Add(left.Evaluate());
        if (!right.IsComplete) pending.Add(right.Evaluate());
        if (compatible)
        foreach (var a in N.E(left.Groups).Cast<Relation>()) foreach (var b in N.E(right.Groups).Cast<Relation>())
        {
            if (!left.TryReadGroup(a, out var x) || !right.TryReadGroup(b, out var y))
            { pending.Add(new Relation(a, b)); continue; }
            var faces = Compare(x.Faces, y.Faces, out var anyFaces);
            var strokes = Compare(x.Strokes, y.Strokes, out var anyStrokes);
            var sites = Compare(x.Sites, y.Sites, out var anySites);
            var pair = new Relation(a, b);
            if (anyFaces || anyStrokes || anySites) correspondences.Add(new Relation(pair, new Relation(faces, new Relation(strokes, sites))));
            else unrelated.Add(pair);
        }
        Correspondences = N.S(correspondences.ToArray()); Unrelated = N.S(unrelated.ToArray());
        Blocked = N.S(blocked.ToArray()); Unresolved = N.S(pending.ToArray());
        read = new(Surface, new Relation(Correspondences, new Relation(Unrelated,
            new Relation(new Relation(left.Complement, right.Complement), new Relation(Blocked, Unresolved)))));
    }

    public Relation Evaluate() => read;
    public bool TryReadCorrespondence(Relation actualRead, out OrnamentViewCorrespondence parts)
    {
        parts = null!;
        if (!N.E(Correspondences).Any(actual => ReferenceEquals(actual, actualRead)) ||
            actualRead is not { Left: Relation { Left: Relation a, Right: Relation b },
                Right: Relation { Left: Relation { Left: Relation sharedFaces, Right: Relation { Left: Relation leftFaces, Right: Relation rightFaces } },
                    Right: Relation { Left: Relation { Left: Relation sharedStrokes, Right: Relation { Left: Relation leftStrokes, Right: Relation rightStrokes } },
                        Right: Relation { Left: Relation sharedSites, Right: Relation { Left: Relation leftSites, Right: Relation rightSites } } } } } ||
            !Left.TryReadGroup(a, out var left) || !Right.TryReadGroup(b, out var right)) return false;
        parts = new(actualRead, left, right, sharedFaces, leftFaces, rightFaces,
            sharedStrokes, leftStrokes, rightStrokes, sharedSites, leftSites, rightSites); return true;
    }

    private static Relation Compare(Relation left, Relation right, out bool anyShared)
    {
        var a = N.E(left); var b = N.E(right);
        var common = a.Where(item => OrnamentView.Contains(b, item)).ToArray(); anyShared = common.Length != 0;
        return new(N.S(common), new Relation(N.S(a.Where(item => !OrnamentView.Contains(b, item)).ToArray()),
            N.S(b.Where(item => !OrnamentView.Contains(a, item)).ToArray())));
    }
}

/// <summary>A decoded native pair witness, retaining both question contexts
/// through its actual source group reads.</summary>
public sealed class OrnamentViewCorrespondence(Relation read, OrnamentViewGroup left, OrnamentViewGroup right,
    Relation sharedFaces, Relation leftOnlyFaces, Relation rightOnlyFaces,
    Relation sharedStrokes, Relation leftOnlyStrokes, Relation rightOnlyStrokes,
    Relation sharedSites, Relation leftOnlySites, Relation rightOnlySites)
{
    public Relation Read { get; } = read;
    public OrnamentViewGroup Left { get; } = left;
    public OrnamentViewGroup Right { get; } = right;
    public Relation SharedFaces { get; } = sharedFaces;
    public Relation LeftOnlyFaces { get; } = leftOnlyFaces;
    public Relation RightOnlyFaces { get; } = rightOnlyFaces;
    public Relation SharedStrokes { get; } = sharedStrokes;
    public Relation LeftOnlyStrokes { get; } = leftOnlyStrokes;
    public Relation RightOnlyStrokes { get; } = rightOnlyStrokes;
    public Relation SharedSites { get; } = sharedSites;
    public Relation LeftOnlySites { get; } = leftOnlySites;
    public Relation RightOnlySites { get; } = rightOnlySites;
}
