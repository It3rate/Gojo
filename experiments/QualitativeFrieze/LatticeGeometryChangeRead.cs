using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Non-draining common-reach support comparison. Actual old/new
/// chord occurrences, prefix reductions and overlap correspondences remain
/// native participants. Host lattice coordinates only select this diagnostic
/// projection; they never create shared sites or material connections.</summary>
public sealed class LatticeGeometryChangeRead
{
    public Relation Source { get; }
    public Relation Basis { get; }
    public Relation RemovedSpans { get; }
    public Relation IntroducedSpans { get; }
    public Relation RetainedCorrespondence { get; }
    public Relation IntroducedBends { get; }
    public Relation RemovedBends { get; }
    public Relation Open { get; }
    public Relation Surface { get; }
    public LatticeGeometryChangeSummary Summary { get; }

    public static LatticeGeometryChangeRead Read(UnitArrangementProgram source, UnitArrangementProgram result) => new(source, result);

    private LatticeGeometryChangeRead(UnitArrangementProgram source, UnitArrangementProgram result)
    {
        Source = new(source.Surface, result.Surface);
        var open = new List<IElement>();
        var reaches = Words(source).Concat(Words(result)).SelectMany(N.E).OfType<Relation>()
            .Select(move => move.Right).Distinct(N.Participants.Instance).ToArray();
        var common = reaches is [var reach] ? reach : null;
        Basis = new(new Relation(source.Frame.Surface, result.Frame.Surface), common ?? N.S(reaches));
        if (!ReferenceEquals(source.Frame, result.Frame) || common is null || !Native(common)) open.Add(Basis);
        if (!source.Complete()) open.Add(source.Surface);
        if (!result.Complete()) open.Add(result.Surface);
        var before = new List<Span>(); var after = new List<Span>();
        var oldBends = new Dictionary<Bend, List<Relation>>(); var newBends = new Dictionary<Bend, List<Relation>>();
        if (open.Count == 0) { Read(source, before, oldBends); Read(result, after, newBends); }
        var oldSupport = new Dictionary<Atom, List<Span>>(); var newSupport = new Dictionary<Atom, List<Span>>();
        foreach (var span in before) Add(span, oldSupport);
        foreach (var span in after) Add(span, newSupport);
        var removed = before.Where(span => span.Atoms.Any(atom => !newSupport.ContainsKey(atom))).Select(span => (IElement)span.Native).ToArray();
        var added = after.Where(span => span.Atoms.Any(atom => !oldSupport.ContainsKey(atom))).Select(span => (IElement)span.Native).ToArray();
        var retained = new HashSet<(Relation Before, Relation After)>(NativePairs.Instance);
        foreach (var atom in oldSupport.Keys.Where(newSupport.ContainsKey))
            foreach (var a in oldSupport[atom]) foreach (var b in newSupport[atom]) retained.Add((a.Native, b.Native));
        RemovedSpans = N.S(removed); IntroducedSpans = N.S(added);
        RetainedCorrespondence = N.S(retained.Select(pair => (IElement)new Relation(pair.Before, pair.After)).ToArray());
        IntroducedBends = N.S(newBends.Where(pair => !oldBends.ContainsKey(pair.Key)).SelectMany(pair => pair.Value).Cast<IElement>().ToArray());
        RemovedBends = N.S(oldBends.Where(pair => !newBends.ContainsKey(pair.Key)).SelectMany(pair => pair.Value).Cast<IElement>().ToArray());
        Open = N.S(open.ToArray());
        long Weight(IEnumerable<Atom> atoms) => atoms.Sum(atom => Math.Abs(atom.To.X - atom.From.X) + Math.Abs(atom.To.Y - atom.From.Y));
        var sourceWeight = Weight(oldSupport.Keys); var resultWeight = Weight(newSupport.Keys);
        var sharedWeight = Weight(oldSupport.Keys.Where(newSupport.ContainsKey));
        var lostWeight = sourceWeight - sharedWeight; var addedWeight = resultWeight - sharedWeight;
        var unionWeight = checked(sourceWeight + resultWeight - sharedWeight);
        Summary = new(open.Count == 0, before.Count, after.Count, sourceWeight, resultWeight, sharedWeight,
            lostWeight, addedWeight, unionWeight == 0 ? 0 : (double)(lostWeight + addedWeight) / unionWeight,
            N.E(IntroducedBends).Length, N.E(RemovedBends).Length);
        // Native scope inventories explain each derived diagnostic count. A
        // retained overlap is a projection correspondence, never a new join.
        Surface = new(new Relation(Source, Basis), new Relation(RemovedSpans, new Relation(IntroducedSpans,
            new Relation(RetainedCorrespondence, new Relation(IntroducedBends, new Relation(RemovedBends, Open))))));

        void Read(UnitArrangementProgram unit, List<Span> spans, Dictionary<Bend, List<Relation>> bends)
        {
            foreach (var path in N.E(unit.Paths).Cast<Relation>())
            {
                if (path.Left is not Relation start || path.Right is not Relation words || !PointOf(unit.Frame, start, out var at))
                { open.Add(path); continue; }
                var local = new List<Span>(); var atWord = start;
                foreach (var word in N.E(words).Cast<Relation>())
                {
                    if (!PointOf(unit.Frame, word, out var step)) { open.Add(word); continue; }
                    var prefix = unit.Frame.Reduce(atWord, word);
                    if (!unit.Frame.TryReadResult(prefix, out var nextWord, out _, out var unresolved) || N.E(unresolved).Length != 0)
                    { open.Add(prefix); continue; }
                    var to = at + step;
                    var native = new Relation(new Relation(path, word), new Relation(atWord, prefix));
                    var span = new Span(native, at, to, Atoms(at, to)); spans.Add(span); local.Add(span);
                    at = to; atWord = nextWord;
                }
                for (var index = 1; index < local.Count; index++) BendAt(local[index - 1], local[index]);
                if (local.Count > 1 && N.E(unit.ClosedPaths).Any(closed => ReferenceEquals(closed, path)))
                    BendAt(local[^1], local[0]);
                void BendAt(Span a, Span b)
                {
                    var first = a.From - a.To; var second = b.To - b.From;
                    if (a.To != b.From || Cross(first, second) == 0) return;
                    first = Direction(first); second = Direction(second);
                    if (Compare(first, second) > 0) (first, second) = (second, first);
                    var key = new Bend(a.To, first, second);
                    if (!bends.TryGetValue(key, out var rows)) bends.Add(key, rows = []);
                    rows.Add(new Relation(path, new Relation(a.Native, b.Native)));
                }
            }
        }
        bool PointOf(QualitativeMotionFrame frame, Relation word, out Point point)
        {
            point = default;
            if (!N.Read(word, out var terms)) return false;
            foreach (var term in terms)
            {
                if (term is not Relation move || !frame.Direction(move, out var axis, out var forward, out var actualReach) || !N.Same(actualReach, common!)) return false;
                var sign = forward ? 1 : -1;
                point = ReferenceEquals(axis, frame.Horizontal) ? point with { X = checked(point.X + sign) } : point with { Y = checked(point.Y + sign) };
            }
            return true;
        }
    }

    private static IEnumerable<Relation> Words(UnitArrangementProgram unit) => N.E(unit.Paths).OfType<Relation>()
        .SelectMany(path => (path.Left is Relation start ? new[] { start } : [])
            .Concat(path.Right is Relation words ? N.E(words).OfType<Relation>() : []));
    private static bool Native(IElement root)
    {
        var seen = new HashSet<IElement>(N.Participants.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item)) if (seen.Add(item))
        { if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); } else if (item is not Placeholder) return false; }
        return true;
    }
    private static void Add(Span span, Dictionary<Atom, List<Span>> support)
    {
        foreach (var atom in span.Atoms)
        { if (!support.TryGetValue(atom, out var rows)) support.Add(atom, rows = []); rows.Add(span); }
    }
    private static Atom[] Atoms(Point from, Point to)
    {
        var delta = to - from; var count = Gcd(Math.Abs(delta.X), Math.Abs(delta.Y));
        if (count == 0) return [];
        var direction = new Point(delta.X / count, delta.Y / count); var atoms = new List<Atom>();
        for (long index = 0; index < count; index++)
        {
            var next = from + direction;
            atoms.Add(Compare(from, next) <= 0 ? new(from, next) : new(next, from)); from = next;
        }
        return atoms.ToArray();
    }
    private static Point Direction(Point point) { var gcd = Gcd(Math.Abs(point.X), Math.Abs(point.Y)); return new(point.X / gcd, point.Y / gcd); }
    private static long Gcd(long a, long b) { while (b != 0) (a, b) = (b, a % b); return a; }
    private static long Cross(Point a, Point b) => checked(a.X * b.Y - a.Y * b.X);
    private static int Compare(Point a, Point b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y);
    private readonly record struct Point(long X, long Y)
    {
        public static Point operator +(Point a, Point b) => new(checked(a.X + b.X), checked(a.Y + b.Y));
        public static Point operator -(Point a, Point b) => new(checked(a.X - b.X), checked(a.Y - b.Y));
    }
    private readonly record struct Atom(Point From, Point To);
    private readonly record struct Bend(Point Site, Point FirstRay, Point SecondRay);
    private sealed record Span(Relation Native, Point From, Point To, Atom[] Atoms);
    private sealed class NativePairs : IEqualityComparer<(Relation Before, Relation After)>
    {
        public static NativePairs Instance { get; } = new();
        public bool Equals((Relation Before, Relation After) a, (Relation Before, Relation After) b) =>
            ReferenceEquals(a.Before, b.Before) && ReferenceEquals(a.After, b.After);
        public int GetHashCode((Relation Before, Relation After) pair) => HashCode.Combine(
            System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(pair.Before),
            System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(pair.After));
    }
}

/// <summary>Support uses unique primitive lattice spans weighted by |dx|+|dy|
/// in the selected shared-reach basis. This is not Euclidean length, occupied
/// area, affine-normalized novelty or a beauty score. Added bends concern
/// authored paths; regrouping independent lines as a box may add authored
/// corners while leaving support unchanged.</summary>
public sealed record LatticeGeometryChangeSummary(bool IsComparable, int SourceSpans, int ResultSpans,
    long SourceFormalSupport, long ResultFormalSupport, long RetainedFormalSupport,
    long RemovedFormalSupport, long AddedFormalSupport, double SymmetricDifferenceRatio,
    int AddedAuthoredBends, int RemovedAuthoredBends)
{
    public bool HasSupportChange => IsComparable && (RemovedFormalSupport != 0 || AddedFormalSupport != 0);
}
