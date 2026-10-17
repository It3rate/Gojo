using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// A finite unmeasured cell with two explicitly glued opposite boundaries.
/// The motion frame supplies a declared independent additive basis; checked
/// seams and winding paths do not prove an infinite plane embedding or p4m.
/// </summary>
public sealed class QualitativePeriodicCell
{
    // path = Closed(ordered actual sites) -> Closed(ordered actual moves)
    // seam = (source-path -> target-path) -> Closed(actual source-site -> target-site)
    // gluing = Closed(actual boundary-site -> actual quotient-site)
    // Surface = (frame.Surface -> Closed(bottom,right,top,left)) -> ((seamX -> seamY) -> gluing)
    // generator = (actual path -> actual word) -> (actual axis -> additive-read)
    // inverse = (actual path -> reversed-path) -> inverse-cancellation-read
    // seam-read = (Surface -> actual seam) -> (actual map-rows -> segment-equality-reads)
    // corner = (Closed(actual X-then-Y map rows) -> Closed(actual Y-then-X map rows)) ->
    //          ((actual start -> actual common end) -> Closed(actual gluing rows))
    // winding = (actual path -> additive-read) -> Closed(actual endpoint-gluing rows)
    // read = Surface -> (generators -> (inverses -> (seams -> (corners ->
    //        (zero-returns -> (windings -> unresolved))))))
    // Top and bottom have the same forward order, as do left and right.
    // A zero perimeter and nonzero quotient winding are different witnesses.
    public QualitativeMotionFrame Frame { get; }
    public Relation Bottom { get; }
    public Relation RightBoundary { get; }
    public Relation Top { get; }
    public Relation LeftBoundary { get; }
    public Relation SeamX { get; }
    public Relation SeamY { get; }
    public Relation Gluing { get; }
    public Relation XWord { get; }
    public Relation YWord { get; }
    public Relation Surface { get; }

    public QualitativePeriodicCell()
    {
        Frame = new();
        var a = new Placeholder(); var b = new Placeholder(); var c = new Placeholder(); var d = new Placeholder();
        var quotientCorner = new Placeholder(); var sharedReach = new Placeholder();
        var x = QualitativeMotionFrame.Move(Frame.Right, sharedReach);
        var y = QualitativeMotionFrame.Move(Frame.Up, sharedReach);
        Bottom = Path(S(a, b), S(x)); RightBoundary = Path(S(b, c), S(y));
        Top = Path(S(d, c), S(x)); LeftBoundary = Path(S(a, d), S(y));
        SeamX = Seam(LeftBoundary, RightBoundary, S(new Relation(a, b), new Relation(d, c)));
        SeamY = Seam(Bottom, Top, S(new Relation(a, d), new Relation(b, c)));
        Gluing = Glue(new Relation(a, quotientCorner), new Relation(b, quotientCorner),
            new Relation(c, quotientCorner), new Relation(d, quotientCorner));
        XWord = (Relation)Bottom.Right; YWord = (Relation)LeftBoundary.Right; Surface = MakeSurface();
    }

    public QualitativePeriodicCell(QualitativeMotionFrame frame, Relation bottom, Relation right,
        Relation top, Relation left, Relation seamX, Relation seamY, Relation gluing)
    {
        Frame = frame; Bottom = bottom; RightBoundary = right; Top = top; LeftBoundary = left;
        SeamX = seamX; SeamY = seamY; Gluing = gluing;
        XWord = TryReadPath(bottom, out _, out var x) ? x : StructuralConstraint.Open(bottom);
        YWord = TryReadPath(left, out _, out var y) ? y : StructuralConstraint.Open(left);
        Surface = MakeSurface();
    }

    public static Relation Path(Relation orderedSites, Relation word) => new(orderedSites, word);
    public static Relation Seam(Relation source, Relation target, Relation siteMap) => new(new Relation(source, target), siteMap);
    public static Relation Glue(params Relation[] siteBindings) => S(siteBindings);

    public static bool TryReadPath(Relation path, out Relation sites, out Relation word)
    {
        sites = word = null!;
        if (path.Left is not Relation points || path.Right is not Relation moves ||
            !Scope(points) || !Scope(moves)) return false;
        sites = points; word = moves; return true;
    }

    public Relation Evaluate()
    {
        var generators = new List<IElement>(); var inverses = new List<IElement>(); var seams = new List<IElement>();
        var corners = new List<IElement>(); var returns = new List<IElement>(); var windings = new List<IElement>();
        var pending = new List<IElement>();
        var paths = new[] { Bottom, RightBoundary, Top, LeftBoundary };
        var points = new IElement[4][]; var words = new Relation[4]; var valid = true;
        for (var i = 0; i < paths.Length; i++)
        {
            if (!TryReadPath(paths[i], out var sites, out var word) || !FiniteLaw.DomainEntries(sites, out points[i]) ||
                points[i].Length < 2 || !FiniteLaw.ClosedEntries(word, out var moves) || moves.Length != points[i].Length - 1)
            { pending.Add(paths[i]); valid = false; continue; }
            words[i] = word;
        }
        if (!valid) return Result();
        var a = points[0][0]; var b = points[0][^1]; var c = points[1][^1]; var d = points[3][^1];
        if (!Same(b, points[1][0]) || !Same(d, points[2][0]) || !Same(c, points[2][^1]) || !Same(a, points[3][0]) ||
            Distinct([a, b, c, d]).Length != 4) pending.Add(S(paths));
        // Each corner belongs to exactly its two joined boundaries; an interior
        // boundary site belongs to one. Additional material joins stay pending.
        var allSites = Distinct(points.SelectMany(p => p).ToArray());
        foreach (var site in allSites)
            if (points.Count(path => Member(path, site)) != (Member(new[] { a, b, c, d }, site) ? 2 : 1)) pending.Add(site);
        var xKnown = Generator(Bottom, words[0], out var xAxis);
        var yKnown = Generator(LeftBoundary, words[3], out var yAxis);
        if (!xKnown || !yKnown || ReferenceEquals(xAxis, yAxis)) pending.Add(new Relation(Bottom, LeftBoundary));
        var xMap = CheckSeam(SeamX, LeftBoundary, RightBoundary, points[3], points[1], words[3], words[1]);
        var yMap = CheckSeam(SeamY, Bottom, Top, points[0], points[2], words[0], words[2]);
        var glue = CheckGluing(allSites, xMap.Concat(yMap).ToArray());
        var perimeter = S([.. E(words[0]), .. E(words[1]), .. E(Inverse(words[2])), .. E(Inverse(words[3]))]);
        var perimeterRead = Frame.Reduce(S(), perimeter);
        if (Zero(perimeterRead)) returns.Add(new Relation(S(Bottom, RightBoundary, ReversePath(Top), ReversePath(LeftBoundary)), perimeterRead));
        else pending.Add(perimeterRead);
        if (pending.Count == 0)
        {
            var xa = Find(xMap, a); var yb = Find(yMap, b); var ya = Find(yMap, a); var xd = Find(xMap, d);
            if (xa is null || yb is null || ya is null || xd is null || !Same(xa.Right, yb.Left) ||
                !Same(ya.Right, xd.Left) || !Same(yb.Right, xd.Right)) pending.Add(new Relation(SeamX, SeamY));
            else
            {
                corners.Add(new Relation(new Relation(S(xa, yb), S(ya, xd)),
                    new Relation(new Relation(a, yb.Right), S(glue.Where(row => Member(new[] { a, b, c, d }, row.Left)).ToArray()))));
                foreach (var (path, word, start, end) in new[] { (Bottom, words[0], a, b), (LeftBoundary, words[3], a, d) })
                    windings.Add(new Relation(new Relation(path, Frame.Reduce(S(), word)), S(Find(glue, start)!, Find(glue, end)!)));
            }
        }
        return Result();

        bool Generator(Relation path, Relation word, out Relation axis)
        {
            axis = null!; bool? face = null;
            foreach (var entry in E(word))
            {
                if (entry is not Relation move || !Frame.Direction(move, out var current, out var forward, out _))
                { pending.Add(new Relation(path, entry)); return false; }
                if (axis is null) { axis = current; face = forward; }
                else if (!ReferenceEquals(axis, current) || face != forward) { pending.Add(path); return false; }
            }
            if (axis is null) { pending.Add(path); return false; }
            var read = Frame.Reduce(S(), word); var inverse = Inverse(word); var cancellation = Frame.Reduce(word, inverse);
            if (!Frame.TryReadResult(read, out var residual, out _, out var open) || !FiniteLaw.ClosedEntries(residual, out var terms) ||
                terms.Length == 0 || E(open).Length != 0 || !Zero(cancellation)) { pending.Add(read); return false; }
            generators.Add(new Relation(new Relation(path, word), new Relation(axis, read)));
            inverses.Add(new Relation(new Relation(path, Path(S(E((Relation)path.Left).Reverse().ToArray()), inverse)), cancellation));
            return true;
        }

        Relation[] CheckSeam(Relation seam, Relation from, Relation to, IElement[] source, IElement[] target, Relation first, Relation second)
        {
            var checks = new List<IElement>(); var rows = Array.Empty<Relation>();
            if (seam.Left is not Relation endpoints || !ReferenceEquals(endpoints.Left, from) || !ReferenceEquals(endpoints.Right, to) ||
                seam.Right is not Relation map || !FiniteLaw.ClosedEntries(map, out var entries) || entries.Any(e => e is not Relation))
            { pending.Add(seam); return rows; }
            rows = entries.Cast<Relation>().ToArray();
            if (source.Length != target.Length || rows.Length != source.Length ||
                rows.Where((row, i) => i >= source.Length || !Same(row.Left, source[i]) || !Same(row.Right, target[i])).Any())
            { pending.Add(seam); return []; }
            var firstMoves = E(first); var secondMoves = E(second);
            if (firstMoves.Length != secondMoves.Length) { pending.Add(seam); return []; }
            for (var i = 0; i < firstMoves.Length; i++)
            {
                var check = Frame.Reduce(S(firstMoves[i]), Inverse(S(secondMoves[i]))); checks.Add(check);
                if (!Zero(check)) pending.Add(new Relation(seam, check));
            }
            seams.Add(new Relation(new Relation(Surface, seam), new Relation(S(rows), S(checks.ToArray()))));
            return rows;
        }

        Relation[] CheckGluing(IElement[] sites, Relation[] identifications)
        {
            if (!FiniteLaw.ClosedEntries(Gluing, out var entries) || entries.Any(e => e is not Relation))
            { pending.Add(Gluing); return []; }
            var rows = entries.Cast<Relation>().ToArray();
            if (rows.Length != sites.Length || rows.Any(row => !Member(sites, row.Left)) ||
                Distinct(rows.Select(row => row.Left).ToArray()).Length != sites.Length)
            { pending.Add(Gluing); return []; }
            var components = Enumerable.Range(0, sites.Length).ToArray();
            foreach (var pair in identifications)
            {
                var from = Index(sites, pair.Left); var to = Index(sites, pair.Right);
                if (from < 0 || to < 0) { pending.Add(pair); continue; }
                var old = components[to]; var next = components[from];
                for (var i = 0; i < components.Length; i++) if (components[i] == old) components[i] = next;
            }
            for (var i = 0; i < sites.Length; i++)
                for (var j = i + 1; j < sites.Length; j++)
                    if ((components[i] == components[j]) != Same(Find(rows, sites[i])!.Right, Find(rows, sites[j])!.Right))
                        pending.Add(new Relation(Find(rows, sites[i])!, Find(rows, sites[j])!));
            return rows;
        }

        Relation Result() => new(Surface, new Relation(S(generators.ToArray()), new Relation(S(inverses.ToArray()),
            new Relation(S(seams.ToArray()), new Relation(S(corners.ToArray()), new Relation(S(returns.ToArray()),
                new Relation(S(windings.ToArray()), S(pending.ToArray()))))))));
    }

    /// <summary>Recomputes the finite input obligations and compares the native
    /// proof grammar. Reallocated wrappers may survive; actual source rows,
    /// paths, words, sites, seams and gluing participants retain identity.</summary>
    public bool TryReadResult(Relation read, out Relation generators, out Relation inverses, out Relation seams,
        out Relation corners, out Relation zeroReturns, out Relation windings, out Relation unresolved)
    {
        generators = inverses = seams = corners = zeroReturns = windings = unresolved = null!;
        if (!ReferenceEquals(read.Left, Surface) || read.Right is not Relation { Left: Relation g, Right: Relation r1 } ||
            r1.Left is not Relation i || r1.Right is not Relation { Left: Relation s, Right: Relation r2 } ||
            r2.Left is not Relation c || r2.Right is not Relation { Left: Relation z, Right: Relation r3 } ||
            r3.Left is not Relation w || r3.Right is not Relation u ||
            new[] { g, i, s, c, z, w, u }.Any(scope => !FiniteLaw.ClosedEntries(scope, out _))) return false;
        var anchors = new HashSet<IElement>(ReferenceEqualityComparer.Instance); Anchor(Surface);
        if (!Equivalent(Evaluate().Right, read.Right)) return false;
        generators = g; inverses = i; seams = s; corners = c; zeroReturns = z; windings = w; unresolved = u; return true;

        void Anchor(IElement item) { if (!anchors.Add(item)) return; if (item is Relation row) { Anchor(row.Left); Anchor(row.Right); } }
        bool Equivalent(IElement expected, IElement actual)
        {
            if (Same(expected, actual)) return true;
            if (anchors.Contains(expected) || anchors.Contains(actual)) return false;
            if (expected is not Relation a || actual is not Relation b) return false;
            if (StructuralConstraint.TryReadScope(a, out var ae, out var aseam, out var atail) &&
                StructuralConstraint.TryReadScope(b, out var be, out var bseam, out var btail))
                return aseam.Equals(atail) == bseam.Equals(btail) && ae.Length == be.Length && ae.Zip(be).All(pair => Equivalent(pair.First, pair.Second));
            return Equivalent(a.Left, b.Left) && Equivalent(a.Right, b.Right);
        }
    }

    public bool TryReadPassed(Relation read) => TryReadResult(read, out _, out _, out _, out _, out _, out _, out var pending) && E(pending).Length == 0;

    private Relation MakeSurface() => new(new Relation(Frame.Surface, S(Bottom, RightBoundary, Top, LeftBoundary)), new Relation(new Relation(SeamX, SeamY), Gluing));
    private Relation ReversePath(Relation path) => Path(S(E((Relation)path.Left).Reverse().ToArray()), Inverse((Relation)path.Right));
    private Relation Inverse(Relation word)
    {
        var moves = new List<IElement>();
        foreach (var entry in E(word).Reverse())
        {
            if (entry is Relation move && E(Frame.Opposites).OfType<Relation>().FirstOrDefault(row => ReferenceEquals(row.Left, move.Left)) is Relation opposite)
                moves.Add(QualitativeMotionFrame.Move(opposite.Right, move.Right));
            else moves.Add(new Relation(entry, StructuralConstraint.Open()));
        }
        return S(moves.ToArray());
    }
    private bool Zero(Relation read) => Frame.TryReadResult(read, out var residual, out _, out var pending) &&
        FiniteLaw.ClosedEntries(residual, out var terms) && terms.Length == 0 && E(pending).Length == 0;
    private static Relation? Find(Relation[] rows, IElement source) => rows.FirstOrDefault(row => Same(row.Left, source));
    private static IElement[] Distinct(IElement[] values) => values.Where((value, index) => !values.Take(index).Any(prior => Same(prior, value))).ToArray();
    private static int Index(IElement[] values, IElement item) => Array.FindIndex(values, value => Same(value, item));
    private static bool Member(IElement[] values, IElement item) => Index(values, item) >= 0;
    private static bool Same(IElement a, IElement b) => FiniteLaw.SameParticipant(a, b);
    private static bool Scope(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out _, out _);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
}
