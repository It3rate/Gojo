using System.Globalization;
using System.Text.Json.Serialization;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Finite output-range observations of connected construction programs.
/// The supplied extent grid is an experiment domain, not the range of all possible
/// realizations. Coordinate atom keys only index candidates; actual native
/// boundary witnesses establish every reported correspondence.</summary>
public static class ConstructionRangeStudy
{
    public sealed record FamilyInfo(string Id, string Name, string Constraint, string Release)
    {
        [JsonIgnore] public ConstructionExchangeMachine Machine { get; init; } = null!;
        [JsonIgnore] public Relation? RepeatAction { get; init; }
        [JsonIgnore] public Relation InputDomain { get; init; } = null!;
        [JsonIgnore] public Relation Witness { get; init; } = null!;
    }

    public sealed record Sample(string Id, string Family, int Width, int Height, int Notch,
        bool Admissible, int Failed, int Unresolved, long[][][] Lines, int Sections, int ZeroSections)
    {
        [JsonIgnore] public Relation Publication { get; init; } = null!;
        [JsonIgnore] public ConstructionExchangeMachine.Execution Execution { get; init; } = null!;
        [JsonIgnore] public Relation Output { get; init; } = null!;
        [JsonIgnore] public Relation Witness { get; init; } = null!;
        [JsonIgnore] public Repetition? Repeat { get; init; }
        [JsonIgnore] public BoundaryExchangeRead.Read? AtomRead { get; init; }
    }

    public sealed record RangeSummary(string Family, int Attempts, int Admitted, int Rejected,
        int Unresolved, int UniqueBoundaries, int UniqueDirected)
    {
        [JsonIgnore] public Relation Witness { get; init; } = null!;
        [JsonIgnore] public IReadOnlyDictionary<string, Sample> Boundaries { get; init; } = null!;
        [JsonIgnore] public IReadOnlyDictionary<string, Sample> Directed { get; init; } = null!;
        [JsonIgnore] public BoundaryExchangeRead.Read[] DuplicateReads { get; init; } = [];
    }

    public sealed record Comparison(string First, string Second, bool Directed, int FirstCount,
        int SecondCount, int Intersection, int FirstOnly, int SecondOnly, bool FirstSubsetOfSecond,
        bool SecondSubsetOfFirst, string? FirstOnlySample, string? SecondOnlySample)
    {
        [JsonIgnore] public Relation Witness { get; init; } = null!;
        [JsonIgnore] public BoundaryExchangeRead.Read[] IntersectionReads { get; init; } = [];
        [JsonIgnore] public BoundaryExchangeRead.Read? RepresentativeMismatch { get; init; }
    }

    public sealed record Report(FamilyInfo[] Families, Sample[] Samples, RangeSummary[] Ranges,
        Comparison[] Comparisons, string Domain, string[] Limits)
    {
        [JsonIgnore] public Curve.Frame Frame { get; init; } = null!;
        [JsonIgnore] public Relation Witness { get; init; } = null!;
    }

    /// <summary>One actual transformed copy, plus explicitly declared endpoint
    /// joins in a derived view. Agreement of coordinates alone never joins sites.</summary>
    public sealed record Repetition(Relation Source, Relation Action, Relation Application,
        QualitativeGraphJoin Join, Relation Sections, Relation Correspondence, Relation Witness);

    public static Report Explore(int maxExtent = 4)
    {
        if (maxExtent is < 1 or > 6) throw new ArgumentOutOfRangeException(nameof(maxExtent));
        var frame = new Curve.Frame(new QualitativeMotionFrame());
        var free = ConstructionExchangeMachine.Create(frame, 0);
        var halfTurn = CentrelineAction.FromBits(frame, 6, frame.Word(0, 0));
        // These authoring choices supply actual connected programs and finite
        // publications. The execution loop below does not inspect family names.
        var families = new[]
        {
            Family("tied-square", "Walk with shared width and height", free.WithEqualExtents(), false, null,
                "W and H are joined by an actual equality obligation.",
                "Removing that obligation admits independent widths and heights."),
            Family("free-walk", "Four-step independent rectangle walk", free, false, null,
                "Opposite reaches share roles; W and H are independent; return is checked.",
                "Already permits rectangular aspect changes. A new corner factorization alone adds no range."),
            Family("repeated-corner", "Two-step corner repeated by half-turn", ConstructionExchangeMachine.Create(frame, 8), false, halfTurn,
                "One horizontal/vertical corner, one half-turn copy, and two declared endpoint joins.",
                "Same sampled rectangle range as the free four-step walk; different reusable construction."),
            Family("rails", "Two pairs of parallel rails", ConstructionExchangeMachine.Create(frame, 1), false, null,
                "Both horizontal rails point right; both vertical rails point up; intersections are shared sites.",
                "Aspect changes fit. A concave notch requires additional structure."),
            Family("caps", "Two routes sharing their endpoints", ConstructionExchangeMachine.Create(frame, 4), false, null,
                "Upper and lower three-step caps share start and finish, retaining opposed route flows.",
                "Same rectangle ink range, but a different directed boundary range."),
            Family("radial-equal", "Four equal-turn radial markers", ConstructionExchangeMachine.Create(frame, 2), false, null,
                "Common contour, cyclic adjacency and equal radial turns; selected calibration requires W = H.",
                "Releasing equal turns allows the selected rectangular calibration to stretch."),
            Family("radial-open", "Radial markers with angular spacing open", ConstructionExchangeMachine.Create(frame, 3), false, null,
                "Common contour and cyclic adjacency remain; this probe supplies a centered rectangular realization.",
                "Matches rectangle ranges here; this is not an enumeration of all angular placements."),
            Family("latent-notch", "Six phases with a zero-read notch", ConstructionExchangeMachine.Create(frame, 5), false, null,
                "Six ordered phases remain; the supplied N binding is zero at this resolution.",
                "Two participating zero-length phases can expose a notch when the offered N domain expands."),
            Family("notch-range", "Six phases with notch optional", ConstructionExchangeMachine.Create(frame, 5), true, null,
                "Same six-phase construction; both N = 0 and one shared-reach N are offered.",
                "Includes the sampled rectangles and additional concave outlines without deleting the latent phases."),
            Family("six-equal-steps", "Six equal steps around a rectangle", ConstructionExchangeMachine.Create(frame, 9), false, null,
                "Right, right, up, left, left, down all reuse one extent role. Every phase uses W twice; H is offered but unused.",
                "Total width is 4W and height is 2W. Varying the unused H cannot broaden this fixed-aspect family; independent corner extents can.")
        };

        var samples = new List<Sample>();
        foreach (var family in families)
        {
            foreach (var publication in E(family.InputDomain).Cast<Relation>())
            {
                var machine = family.Machine;
                int Count(IElement role) => E((Relation)E(publication).Cast<Relation>()
                    .Single(row => ReferenceEquals(row.Left, role)).Right).Length;
                var w = Count(machine.WidthRole); var h = Count(machine.HeightRole); var n = Count(machine.NotchRole);
                var execution = machine.Execute(publication);
                var repeat = family.RepeatAction is Relation action && execution.Admissible
                    ? RepeatAndJoin(frame, execution.Sections, action) : null;
                var output = repeat?.Sections ?? execution.Sections;
                var views = E(output).Cast<Relation>().Select(frame.Read).ToArray();
                var nativeRead = execution.Admissible ? BoundaryExchangeRead.Evaluate(frame, output, output) : null;
                var pending = E(execution.Unresolved).Length + (nativeRead is null ? 0 : E(nativeRead.Unresolved).Length);
                var admissible = execution.Admissible && pending == 0;
                var evidence = S(execution.Surface, repeat?.Witness ?? S(), nativeRead?.Surface ?? S());
                samples.Add(new($"{family.Id}-{w}-{h}-{n}", family.Id, w, h, n, admissible,
                    E(execution.Failed).Length, pending,
                    views.Select(v => v.Controls.Select(p => new[] { checked((long)p.X), checked((long)p.Y) }).ToArray()).ToArray(),
                    views.Length, views.Count(v => v.Controls[0] == v.Controls[^1]))
                {
                    Publication = publication, Execution = execution, Output = output,
                    Witness = evidence, Repeat = repeat, AtomRead = nativeRead
                });
            }
        }

        var ranges = families.Select(family => Summarize(frame, family, samples.Where(s => s.Family == family.Id).ToArray())).ToArray();
        var comparisons = new List<Comparison>();
        for (var i = 0; i < ranges.Length; i++)
            for (var j = i + 1; j < ranges.Length; j++)
                foreach (var directed in new[] { false, true }) comparisons.Add(Compare(frame, ranges[i], ranges[j], directed));
        return new(families, samples.ToArray(), ranges, comparisons.ToArray(),
            $"W and H each use 1 through {maxExtent} repeated shared reaches (normally half-extents). Six-equal-steps uses W twice per equal phase, giving total width 4W and height 2W; H is unused there. N is a zero-at-resolution publication, or zero/one in notch-range. One common origin, frame and resolution; no translation or rotation quotient.",
            ["Range inclusion and equality refer only to these explicitly enumerated publications, not all possible machine outputs.",
             "Undirected ink ignores traversal and subdivision; directed ink retains segment direction but still ignores execution order. Both preserve overlapping occurrence multiplicity.",
             "Boundary agreement does not establish identical topology, machine, potential, construction history, area prediction or equal probability of outputs.",
             "The radial-open family samples one supplied rectangular realization. Open angular spacing has many other possible realizations that are not sampled.",
             "Six-equal-steps has only one used extent role. Its unused H publications duplicate outputs; finite range overlap is also limited by the offered scale range.",
             "Zero-at-resolution notch phases remain in structure and evidence. Their lack of visible length is not removal of participation."])
        {
            Frame = frame,
            Witness = S(S(families.Select(f => (IElement)f.Witness).ToArray()),
                S(ranges.Select(r => (IElement)r.Witness).ToArray()), S(comparisons.Select(c => (IElement)c.Witness).ToArray()))
        };

        FamilyInfo Family(string id, string name, ConstructionExchangeMachine machine, bool varyNotch,
            Relation? action, string constraint, string release)
        {
            var publications = new List<IElement>();
            for (var w = 1; w <= maxExtent; w++)
                for (var h = 1; h <= maxExtent; h++)
                    for (var n = 0; n <= (varyNotch ? 1 : 0); n++) publications.Add(machine.Publish(w, h, n));
            var domain = S(publications.ToArray());
            return new(id, name, constraint, release)
            {
                Machine = machine, RepeatAction = action, InputDomain = domain,
                Witness = new Relation(new Relation(machine.Surface, action is null ? S() : S(action)), domain)
            };
        }
    }

    /// <summary>Generic two-copy composition. The action is a native transform,
    /// not a shape name. Input order must be an actual connected path. The last
    /// source site is declared equal to the first copy site, and vice versa;
    /// failed seam bindings are rejected, never repaired by moving an endpoint.</summary>
    public static Repetition RepeatAndJoin(Curve.Frame frame, Relation source, Relation action)
    {
        if (!Closed(source) || E(source).Length == 0) throw new ArgumentException("A closed nonempty path scope is required.", nameof(source));
        var original = E(source).Cast<Relation>().Select(frame.Read).ToArray();
        if (original.Any(r => !r.Complete || E(r.ControlWords).Length != 2) ||
            original.Skip(1).Where((r, i) => !ReferenceEquals(original[i].End, r.Start)).Any())
            throw new ArgumentException("Each successive line must share its actual handoff site.", nameof(source));
        var application = CentrelineAction.Apply(frame, action, source);
        if (!CentrelineAction.TryReadApplication(frame, application, out var applied) || !applied.Complete)
            throw new ArgumentException("The transform left unresolved participation.", nameof(action));
        var copy = E(applied.Output).Cast<Relation>().Select(frame.Read).ToArray();
        var views = original.Concat(copy).ToArray();
        var bindings = new Dictionary<IElement, Relation>(ReferenceEqualityComparer.Instance);
        var strokes = new List<IElement>();
        foreach (var view in views)
        {
            var controls = E(view.ControlWords).Cast<Relation>().ToArray();
            bindings.TryAdd(view.Start, controls[0]); bindings.TryAdd(view.End, controls[^1]);
            var delta = frame.Motion.Reduce(controls[^1], Oppose(frame, controls[0]));
            if (!frame.Motion.TryReadResult(delta, out var word, out _, out var unresolved) || E(unresolved).Length != 0)
                throw new InvalidOperationException("A repeated line has an unresolved chord.");
            strokes.Add(QualitativeStrokeGraph.Stroke(view.Section, view.Start, view.End, word));
        }
        var graph = new QualitativeStrokeGraph(frame.Motion, S(bindings.Keys.ToArray()), S(strokes.ToArray()));
        var declarations = S(
            new Relation(new Relation(original[^1].End, copy[0].Start), application),
            new Relation(new Relation(copy[^1].End, original[0].Start), application));
        var join = new QualitativeGraphJoin(graph,
            S(bindings.Select(b => (IElement)new Relation(b.Key, b.Value)).ToArray()), declarations);
        if (!join.TryReadPassed(join.Evaluate())) throw new ArgumentException("The requested repeat does not meet both declared seams.", nameof(action));
        var joinedBindings = E(join.Bindings).Cast<Relation>().ToArray();
        var sections = new List<IElement>(); var correspondence = new List<IElement>();
        foreach (var item in E(join.Graph.Strokes).Cast<Relation>())
        {
            if (!QualitativeStrokeGraph.TryReadStroke(item, out _, out var from, out var to, out _))
                throw new InvalidOperationException("Unusable joined stroke.");
            var section = frame.Line(from, to,
                (Relation)joinedBindings.Single(b => ReferenceEquals(b.Left, from)).Right,
                (Relation)joinedBindings.Single(b => ReferenceEquals(b.Left, to)).Right);
            sections.Add(section); correspondence.Add(new Relation(item, section));
        }
        var output = S(sections.ToArray()); var map = S(correspondence.ToArray());
        return new(source, action, application, join, output, map,
            new Relation(new Relation(source, action), S(application, join.Surface, join.Evaluate(), map, output)));
    }

    private static RangeSummary Summarize(Curve.Frame frame, FamilyInfo family, Sample[] samples)
    {
        var boundary = new Dictionary<string, Sample>(StringComparer.Ordinal);
        var directed = new Dictionary<string, Sample>(StringComparer.Ordinal);
        var duplicates = new List<BoundaryExchangeRead.Read>();
        foreach (var sample in samples.Where(s => s.Admissible))
            foreach (var mode in new[] { false, true })
            {
                var key = AtomKey(frame, sample.AtomRead!.SourceAtoms, mode); var index = mode ? directed : boundary;
                if (index.TryGetValue(key, out var prior))
                {
                    var proof = BoundaryExchangeRead.Evaluate(frame, prior.Output, sample.Output, mode);
                    if (!proof.Equivalent) throw new InvalidOperationException("An atom index collision did not pass native comparison.");
                    duplicates.Add(proof);
                }
                else index.Add(key, sample);
            }
        return new(family.Id, samples.Length, samples.Count(s => s.Admissible), samples.Count(s => s.Failed > 0),
            samples.Count(s => s.Unresolved > 0), boundary.Count, directed.Count)
        {
            Boundaries = boundary, Directed = directed, DuplicateReads = duplicates.ToArray(),
            Witness = new Relation(family.Witness, S(S(samples.Select(s => (IElement)s.Witness).ToArray()),
                S(boundary.Values.Select(s => (IElement)s.Witness).ToArray()),
                S(directed.Values.Select(s => (IElement)s.Witness).ToArray()),
                S(duplicates.Select(r => (IElement)r.Surface).ToArray())))
        };
    }

    private static Comparison Compare(Curve.Frame frame, RangeSummary first, RangeSummary second, bool directed)
    {
        var a = directed ? first.Directed : first.Boundaries; var b = directed ? second.Directed : second.Boundaries;
        var reads = new List<BoundaryExchangeRead.Read>(); var paired = new List<IElement>();
        var leftOnly = new List<Sample>(); var rightOnly = new List<Sample>();
        foreach (var (key, source) in a)
        {
            if (!b.TryGetValue(key, out var target)) { leftOnly.Add(source); continue; }
            var read = BoundaryExchangeRead.Evaluate(frame, source.Output, target.Output, directed);
            if (!read.Equivalent) throw new InvalidOperationException("A range intersection lacks a valid boundary witness.");
            reads.Add(read); paired.Add(new Relation(new Relation(source.Witness, target.Witness), read.Surface));
        }
        foreach (var (key, target) in b) if (!a.ContainsKey(key)) rightOnly.Add(target);
        BoundaryExchangeRead.Read? mismatch = null;
        if (leftOnly.Count > 0 && b.Count > 0)
            mismatch = BoundaryExchangeRead.Evaluate(frame, leftOnly[0].Output, b.Values.First().Output, directed);
        else if (rightOnly.Count > 0 && a.Count > 0)
            mismatch = BoundaryExchangeRead.Evaluate(frame, a.Values.First().Output, rightOnly[0].Output, directed);
        return new(first.Family, second.Family, directed, a.Count, b.Count, reads.Count,
            leftOnly.Count, rightOnly.Count, leftOnly.Count == 0, rightOnly.Count == 0,
            leftOnly.FirstOrDefault()?.Id, rightOnly.FirstOrDefault()?.Id)
        {
            IntersectionReads = reads.ToArray(), RepresentativeMismatch = mismatch,
            Witness = new Relation(new Relation(first.Witness, second.Witness), S(S(paired.ToArray()),
                S(leftOnly.Select(s => (IElement)s.Witness).ToArray()), S(rightOnly.Select(s => (IElement)s.Witness).ToArray()),
                mismatch?.Surface ?? S()))
        };
    }

    private static string AtomKey(Curve.Frame frame, Relation atoms, bool directed)
    {
        // Presentation/index cache only. No identity, seam, probability, or
        // universal equivalence is inferred from the coordinate serialization.
        static int Compare(Curve.Vector a, Curve.Vector b) => a.X == b.X ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X);
        static string Point(Curve.Vector p) => p.X.ToString(CultureInfo.InvariantCulture) + "," + p.Y.ToString(CultureInfo.InvariantCulture);
        return string.Join(";", E(atoms).Cast<Relation>().Select(atom =>
        {
            var ends = (Relation)atom.Right;
            if (!frame.Vector((Relation)ends.Left, out var start) || !frame.Vector((Relation)ends.Right, out var end))
                throw new InvalidOperationException("A native boundary atom is unreadable.");
            if (!directed && Compare(start, end) > 0) (start, end) = (end, start);
            return Point(start) + ">" + Point(end);
        }).Order(StringComparer.Ordinal));
    }

    private static Relation Oppose(Curve.Frame frame, Relation word) => S(E(word).Cast<Relation>().Select(move =>
    {
        if (!frame.Motion.Direction(move, out var axis, out var forward, out var reach)) throw new ArgumentException("Unreadable motion.", nameof(word));
        return (IElement)QualitativeMotionFrame.Move(ReferenceEquals(axis, frame.Motion.Horizontal)
            ? forward ? frame.Motion.Left : frame.Motion.Right : forward ? frame.Motion.Down : frame.Motion.Up, reach);
    }).ToArray());
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}
