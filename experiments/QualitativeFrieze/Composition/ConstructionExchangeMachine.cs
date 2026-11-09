using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>
/// Small authored construction programs. All dependencies, moves, extent roles,
/// drawing connections and equalities are native relations. Names belong only
/// to the study. Execution observes a supplied binding; it never changes an
/// observed source or selects a new machine. No shape-name execution dispatch.
/// </summary>
public sealed class ConstructionExchangeMachine
{
    // surface = frame -> Closed(roles, seeds, advances, drawing edges, equalities, retained basis)
    // seed = actual site -> Closed(direction -> extent role, ...)
    // advance = (source site -> target site) -> Closed(direction -> extent role, ...)
    // equality = extent role -> extent role
    // publication = Closed(role -> Closed(shared reach, ...), ...)
    // execution = (surface -> publication) -> Closed(positions, steps, sections,
    //              matched obligations, failed obligations, unresolved obligations)
    // A repeated target is checked against its existing position, never overwritten.
    public Curve.Frame Frame { get; }
    public Relation Surface { get; }
    public IElement WidthRole { get; }
    public IElement HeightRole { get; }
    public IElement NotchRole { get; }
    public QualitativeMarkedContour? Contour { get; }

    public sealed record Execution(Relation Surface, Relation Sections, Relation Positions,
        Relation Steps, Relation Matched, Relation Failed, Relation Unresolved)
    {
        public bool Admissible => E(Failed).Length == 0 && E(Unresolved).Length == 0;
    }

    private ConstructionExchangeMachine(Curve.Frame frame, IElement width, IElement height,
        IElement notch, Relation seeds, Relation advances, Relation edges, Relation equalities,
        Relation retained, QualitativeMarkedContour? contour = null)
    {
        Frame = frame; WidthRole = width; HeightRole = height; NotchRole = notch; Contour = contour;
        Surface = new(frame.Surface, S(S(width, height, notch), seeds, advances, edges, equalities, retained));
    }

    public Relation Publish(int width, int height, int notch = 0)
    {
        if (width is < 0 or > 12 || height is < 0 or > 12 || notch is < 0 or > 12)
            throw new ArgumentOutOfRangeException(nameof(width));
        Relation Row(IElement role, int count) => new(role, S(Enumerable.Repeat(Frame.Reach, count).ToArray()));
        return S(Row(WidthRole, width), Row(HeightRole, height), Row(NotchRole, notch));
    }

    /// <summary>A new candidate with an actual shared-extent obligation. The
    /// source program, sites and publications remain unchanged; execution checks
    /// the connected equality rather than a shape label or a filtered sample.</summary>
    public ConstructionExchangeMachine WithEqualExtents()
    {
        var scopes = E((Relation)Surface.Right).Cast<Relation>().ToArray();
        return new(Frame, WidthRole, HeightRole, NotchRole, scopes[1], scopes[2], scopes[3],
            S(E(scopes[4]).Append(new Relation(WidthRole, HeightRole)).ToArray()), scopes[5], Contour);
    }

    public Execution Execute(Relation publication)
    {
        var matched = new List<IElement>(); var failed = new List<IElement>(); var pending = new List<IElement>();
        var positions = new List<Relation>(); var steps = new List<IElement>(); var sections = new List<IElement>();
        if (!ReferenceEquals(Surface.Left, Frame.Surface) || Surface.Right is not Relation body ||
            !Closed(body, out var parts) || parts.Length != 6 || parts.Any(p => p is not Relation))
        { pending.Add(Surface); return Finish(); }
        var scopes = parts.Cast<Relation>().ToArray();
        if (scopes.Any(scope => !Closed(scope, out _)) ||
            !Closed(publication, out var supplied))
        { pending.Add(publication); return Finish(); }
        var roles = E(scopes[0]);
        if (supplied.Length != roles.Length || supplied.Any(row => row is not Relation))
        { pending.Add(publication); return Finish(); }
        var bindings = supplied.Cast<Relation>().ToArray();
        foreach (var role in roles)
        {
            var rows = bindings.Where(row => ReferenceEquals(row.Left, role)).ToArray();
            if (rows.Length != 1 || rows[0].Right is not Relation extent ||
                !Closed(extent, out var reaches) || reaches.Any(r => !ReferenceEquals(r, Frame.Reach)))
                pending.Add(new Relation(role, publication));
        }
        if (pending.Count != 0) return Finish();

        Relation? Expand(IElement expression)
        {
            if (expression is not Relation scope || !Closed(scope, out var uses))
            { pending.Add(expression); return null; }
            var moves = new List<IElement>();
            foreach (var item in uses)
            {
                if (item is not Relation use ||
                    !Frame.Motion.Direction(QualitativeMotionFrame.Move(use.Left, Frame.Reach), out _, out _, out _))
                { pending.Add(item); return null; }
                var binding = bindings.SingleOrDefault(row => ReferenceEquals(row.Left, use.Right));
                if (binding?.Right is not Relation extent) { pending.Add(item); return null; }
                moves.AddRange(E(extent).Select(reach => (IElement)QualitativeMotionFrame.Move(use.Left, reach)));
            }
            return S(moves.ToArray());
        }

        void Place(IElement site, Relation before, Relation incoming, IElement cause)
        {
            var reduction = Frame.Motion.Reduce(before, incoming);
            if (!Frame.Motion.TryReadResult(reduction, out var result, out _, out var open) || E(open).Length != 0)
            { pending.Add(reduction); return; }
            var existing = positions.SingleOrDefault(row => ReferenceEquals(row.Left, site));
            if (existing is null) positions.Add(new Relation(site, result));
            else
            {
                var inverse = Oppose((Relation)existing.Right);
                var comparison = Frame.Motion.Reduce(inverse, result);
                if (!Frame.Motion.TryReadResult(comparison, out var remainder, out _, out var unresolved) || E(unresolved).Length != 0)
                    pending.Add(comparison);
                else (E(remainder).Length == 0 ? matched : failed).Add(new Relation(cause, comparison));
            }
            steps.Add(new Relation(new Relation(cause, publication), reduction));
        }

        foreach (var item in E(scopes[1]))
            if (item is Relation seed && Expand(seed.Right) is Relation incoming) Place(seed.Left, S(), incoming, seed);
            else pending.Add(item);
        foreach (var item in E(scopes[2]))
        {
            if (item is not Relation { Left: Relation ends } advance) { pending.Add(item); continue; }
            var source = positions.SingleOrDefault(row => ReferenceEquals(row.Left, ends.Left));
            if (source?.Right is not Relation before || Expand(advance.Right) is not Relation incoming)
            { pending.Add(advance); continue; }
            Place(ends.Right, before, incoming, advance);
        }
        foreach (var item in E(scopes[4]))
        {
            if (item is not Relation equality) { pending.Add(item); continue; }
            var a = bindings.SingleOrDefault(row => ReferenceEquals(row.Left, equality.Left));
            var b = bindings.SingleOrDefault(row => ReferenceEquals(row.Left, equality.Right));
            if (a?.Right is not Relation first || b?.Right is not Relation second) { pending.Add(item); continue; }
            // Comparing repeated shared reaches is a finite participation read.
            var witness = new Relation(equality, new Relation(a, b));
            (E(first).Length == E(second).Length ? matched : failed).Add(witness);
        }
        if (Contour is not null)
        {
            // Validate the retained contour and its explicitly supplied local
            // realization. This is a conditional sign-reflection calibration,
            // not discovery of circularity from coordinates or marker count.
            var contourRead = Contour.Evaluate();
            var bases = E(scopes[5]);
            if (bases.Length != 2 || bases[0] is not Relation { Right: Relation calibration } basis ||
                !ReferenceEquals(basis.Left, Contour.Surface) ||
                !Closed(calibration, out var calibrationRows) || calibrationRows.Length != 3 ||
                calibrationRows[0] is not Relation center || !ReferenceEquals(center.Left, Contour.Center) ||
                center.Right is not Relation centerWord || !Frame.Vector(centerWord, out var origin) || !origin.Zero ||
                calibrationRows[1] is not Relation seedScope || !Closed(seedScope, out var selectedSeeds) ||
                !selectedSeeds.SequenceEqual(E(scopes[1])) ||
                calibrationRows[2] is not Relation actions || !Closed(actions, out var actionRows) ||
                !Contour.TryReadClosedContour(contourRead, out _)) pending.Add(scopes[5]);
            else
            {
                var radialWords = E(Contour.Markers).Select(marker => positions.SingleOrDefault(row => ReferenceEquals(row.Left, marker))).ToArray();
                if (radialWords.Length == 0 || radialWords.Any(row => row is null) || actionRows.Length != radialWords.Length ||
                    actionRows.Any(row => row is not Relation)) pending.Add(scopes[5]);
                else
                {
                    var vectors = radialWords.Select(row => { Frame.Vector((Relation)row!.Right, out var v); return v; }).ToArray();
                    // Distinct contour markers cannot be established by a
                    // collapsed projection. Retain the unresolved calibration.
                    if (vectors.Distinct().Count() != vectors.Length || vectors.Any(v => v.Zero))
                        pending.Add(new Relation(basis, S(radialWords.Select(r => (IElement)r!).ToArray())));
                    var reference = radialWords[0]!;
                    var referenceRadius = S(Frame.Line(Contour.Center, reference.Left, centerWord, (Relation)reference.Right));
                    foreach (var target in radialWords)
                    {
                        var maps = actionRows.Cast<Relation>().Where(row => ReferenceEquals(row.Left, target!.Left)).ToArray();
                        if (maps.Length != 1 || maps[0].Right is not Relation action ||
                            !CentrelineAction.TryBits(Frame, action, out var bits) || bits > 7)
                        { pending.Add(new Relation(target!, actions)); continue; }
                        var application = CentrelineAction.Apply(Frame, action, referenceRadius);
                        var targetRadius = S(Frame.Line(Contour.Center, target!.Left, centerWord, (Relation)target.Right));
                        var comparison = CentrelineAction.Match(Frame, application, targetRadius);
                        var evidence = new Relation(new Relation(basis, maps[0]), comparison);
                        if (!CentrelineAction.TryReadMatch(Frame, comparison, out var read) || !read.Complete)
                            pending.Add(evidence);
                        else (read.GeometryExact ? matched : failed).Add(evidence);
                    }
                    matched.Add(new Relation(basis, contourRead));
                }
            }
        }
        foreach (var item in E(scopes[3]))
        {
            if (item is not Relation edge) { pending.Add(item); continue; }
            var a = positions.SingleOrDefault(row => ReferenceEquals(row.Left, edge.Left));
            var b = positions.SingleOrDefault(row => ReferenceEquals(row.Left, edge.Right));
            if (a?.Right is not Relation first || b?.Right is not Relation second) { pending.Add(edge); continue; }
            sections.Add(Frame.Line(edge.Left, edge.Right, first, second));
        }
        return Finish();

        Execution Finish()
        {
            var p = S(positions.ToArray()); var t = S(steps.ToArray()); var d = S(sections.ToArray());
            var yes = S(matched.ToArray()); var no = S(failed.ToArray()); var open = S(pending.ToArray());
            return new(new Relation(new Relation(Surface, publication), S(p, t, d, yes, no, open)), d, p, t, yes, no, open);
        }
    }

    private Relation Oppose(Relation word) => S(E(word).Cast<Relation>().Select(move =>
    {
        Frame.Motion.Direction(move, out var axis, out var forward, out var reach);
        return (IElement)QualitativeMotionFrame.Move(ReferenceEquals(axis, Frame.Motion.Horizontal)
            ? forward ? Frame.Motion.Left : Frame.Motion.Right
            : forward ? Frame.Motion.Down : Frame.Motion.Up, reach);
    }).ToArray());

    /// <summary>Authoring apparatus only. Integer option selects which connected
    /// program to supply; Execute never sees it. 0 walk, 1 rails, 2 equal-turn
    /// radial quadrilateral, 3 radial quadrilateral with open angular spacing,
    /// 4 paired caps, 5 notched walk, 6 open walk, 7 detached lookalike rails,
    /// 8 two-step open corner (a repeat needs its own transform and seam law),
    /// 9 six equal advances around a 2:1 rectangle (H remains an unused offer).</summary>
    public static ConstructionExchangeMachine Create(Curve.Frame frame, int option)
    {
        var w = new Placeholder(); var h = new Placeholder(); var n = new Placeholder();
        var sites = Enumerable.Range(0, 8).Select(_ => (IElement)new Placeholder()).ToArray();
        var seeds = new List<IElement>(); var advances = new List<IElement>(); var edges = new List<IElement>();
        var equalities = new List<IElement>(); var retained = new List<IElement>();
        QualitativeMarkedContour? contour = null;
        Relation R(IElement role) => new(frame.Motion.Right, role);
        Relation L(IElement role) => new(frame.Motion.Left, role);
        Relation U(IElement role) => new(frame.Motion.Up, role);
        Relation D(IElement role) => new(frame.Motion.Down, role);
        void Seed(int site, params IElement[] terms) => seeds.Add(new Relation(sites[site], S(terms)));
        void Advance(int from, int to, params IElement[] terms)
        { advances.Add(new Relation(new Relation(sites[from], sites[to]), S(terms))); edges.Add(new Relation(sites[from], sites[to])); }
        if (option is 0 or 5 or 6 or 8)
        {
            Seed(0, L(w), D(h));
            Advance(0, 1, R(w), R(w));
            if (option == 5)
            {
                Advance(1, 2, U(h), U(h), D(n)); Advance(2, 3, L(n));
                Advance(3, 4, U(n)); Advance(4, 5, L(w), L(w), R(n)); Advance(5, 0, D(h), D(h));
            }
            else
            {
                Advance(1, 2, U(h), U(h));
                if (option != 8) Advance(2, 3, L(w), L(w));
                if (option == 0) Advance(3, 0, D(h), D(h));
            }
        }
        else if (option == 9)
        {
            // Six actual phases share one extent role. Two uses per phase keep
            // the centered seed in the same integral shared-reach frame; no
            // fractional origin or scalar aspect-ratio tag is introduced.
            Seed(0, L(w), L(w), D(w));
            Advance(0, 1, R(w), R(w)); Advance(1, 2, R(w), R(w));
            Advance(2, 3, U(w), U(w)); Advance(3, 4, L(w), L(w));
            Advance(4, 5, L(w), L(w)); Advance(5, 0, D(w), D(w));
        }
        else if (option == 4)
        {
            Seed(0, L(w));
            Advance(0, 1, U(h)); Advance(1, 2, R(w), R(w)); Advance(2, 3, D(h));
            Advance(0, 4, D(h)); Advance(4, 5, R(w), R(w)); Advance(5, 3, U(h));
        }
        else if (option is 1 or 2 or 3 or 7)
        {
            Seed(0, L(w), D(h)); Seed(1, R(w), D(h)); Seed(2, R(w), U(h)); Seed(3, L(w), U(h));
            var walk = S(new Relation(sites[0], sites[1]), new Relation(sites[1], sites[2]),
                new Relation(sites[2], sites[3]), new Relation(sites[3], sites[0]));
            if (option is 2 or 3)
            {
                contour = new QualitativeMarkedContour(new Placeholder(), S(new Placeholder()), S(sites.Take(4).ToArray()), walk, sites[0]);
                if (option == 2) { contour = contour.WithEqualTurns(new Placeholder()); equalities.Add(new Relation(w, h)); }
                // This selected centered rectangular realization provides four
                // common-radius corners; equality of component reaches is the
                // additional quarter-turn calibration obligation. It is supplied,
                // not inferred from the count of markers or a drawn picture.
                byte[] reflections = [0, 2, 6, 4];
                var maps = S(sites.Take(4).Select((site, i) => (IElement)new Relation(site,
                    CentrelineAction.FromBits(frame, reflections[i], frame.Word(0, 0)))).ToArray());
                retained.Add(new Relation(contour.Surface, S(new Relation(contour.Center, frame.Word(0, 0)), S(seeds.ToArray()), maps)));
                retained.Add(contour.Evaluate()); edges.AddRange(E(walk));
            }
            else
            {
                edges.AddRange(new IElement[] { new Relation(sites[0], sites[1]), new Relation(sites[3], sites[2]),
                    new Relation(sites[0], sites[3]), new Relation(sites[1], sites[2]) });
                if (option == 7)
                {
                    // Four independently authored lines at matching positions.
                    // Duplicate seed expressions do not create shared sites.
                    var original = seeds.Cast<Relation>().ToArray(); seeds.Clear();
                    for (var i = 0; i < edges.Count; i++)
                    {
                        var edge = (Relation)edges[i]; var a = new Placeholder(); var b = new Placeholder();
                        seeds.Add(new Relation(a, original.Single(s => ReferenceEquals(s.Left, edge.Left)).Right));
                        seeds.Add(new Relation(b, original.Single(s => ReferenceEquals(s.Left, edge.Right)).Right));
                        edges[i] = new Relation(a, b);
                    }
                }
            }
        }
        else throw new ArgumentOutOfRangeException(nameof(option));
        return new(frame, w, h, n, S(seeds.ToArray()), S(advances.ToArray()), S(edges.ToArray()),
            S(equalities.ToArray()), S(retained.ToArray()), contour);
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope, out IElement[] entries) =>
        StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
}
