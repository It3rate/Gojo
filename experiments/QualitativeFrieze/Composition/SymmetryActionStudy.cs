using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Atlas = Gojo.Experiments.QualitativeFrieze.Composition.CentrelinePatternStudy;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Bounded searches over exposed centre-line sections. Host vectors
/// reject unsuitable proposals cheaply; every retained engine is executed and
/// matched through the native action grammar. No source caption selects a law.</summary>
public static class SymmetryActionStudy
{
    // focus = owner -> Closed(actual selected sections)
    // recurrence = (owner -> (seed -> action)) ->
    //              (controller -> (Closed(feedback applications) -> Closed(matches)))
    // controller = Closed(phases) -> Closed(phase -> next phase)
    // feedback = (phase -> next phase) -> (consumed scope -> application)
    // A generated scope is the next actual input. Target chunks are read-only
    // comparison surfaces; they cannot correct the generated trajectory.
    // The original course retains its handoffs. Fresh predicted copies are not
    // welded merely because their seam positions agree.

    public sealed record EngineRead(string Id, string Pattern, int PathIndex,
        int SeedSegments, int Occurrences, int Code, string Bits, int[] Offset,
        int[][] Source, int[][] Output, int[][] Target, bool Exact,
        bool SitePatternExact, int ExpandedSegments, int RetainedSeedSegments,
        int ActionRows, int ValidatedApplications, string Note);
    public sealed record Engine(Relation Surface, Relation Action, Relation Seed,
        Relation[] Applications, Relation[] Matches, EngineRead Read);
    public sealed record TRead(string Id, string Name, string Pattern,
        int[][] SourcePath, int[] SourceIndices, int[] TargetIndices,
        int[] SharedIndices, int[][] Source, int[][] Output, int[][] Target,
        int Code, string Bits, int[] Offset, bool Exact, bool SitePatternExact,
        int SharedCount, string Note, object[] StepRows);
    public sealed record TCase(Curve.Frame Frame, Relation Surface, TRead Read);
    public sealed record TemplateRead(string Id, string Name, int[][] Source, bool Closed);
    public sealed record OffsetRead(string Id, string Name, string Word, int[] Xy);
    public sealed record ActionRead(string TemplateId, int Code, string Bits,
        string OffsetId, int[][] Source, int[][] Output, bool Complete, bool Validated);
    public sealed record ControlRead(string Id, string Name, int[][] Source,
        int[][] Output, int[][] Target, bool Exact, bool SitePatternExact, string Note);
    public sealed record CorpusRead(string Id, string Name, int Paths, int Segments,
        int Spots, int ClosedPaths, int EngineCount, int PairMatches, int? ShortestSeed, string Note);
    public sealed record PairEvidence(string Pattern, int First, int Second, int Code,
        int[] Offset, Relation Action, Relation Application, Relation Witness);
    private sealed record Template(string Id, string Name, Curve.Frame Frame, Relation Sections);

    public static readonly string[] SourceIds =
        ["A1", "A2", "A3", "A4", "A5", "A6", "B1", "B3", "B4", "B5", "B6", "B8", "CL1", "CL2", "CL5", "CL6", "CR3", "CR4"];

    public static object Report()
    {
        var corpus = new List<CorpusRead>(); var engines = new List<EngineRead>(); var pairReadings = new List<object>();
        var pairTotal = 0; var distinctPairTotal = 0;
        foreach (var id in SourceIds)
        {
            var fixture = Atlas.BuildSource(id); var atlas = Atlas.Read(fixture);
            var found = FindEngines(fixture, id); var pairs = FindPairs(fixture, id);
            engines.AddRange(found.Select(e => e.Read)); pairTotal += pairs.Length;
            var distinctPairs = pairs.Select(p => (p.First, p.Second)).Distinct().Count(); distinctPairTotal += distinctPairs;
            pairReadings.AddRange(pairs.Select(p => (object)new { p.Pattern, p.First, p.Second, p.Code, Bits = Bits(p.Code), p.Offset,
                Note = "Exact whole-route correspondence with a bijective site-alias pattern. Several actions for one pair are alternative explanations, not independent evidence." }));
            corpus.Add(new(id, SourceName(id), atlas.Metrics.Paths, atlas.Metrics.Segments,
                atlas.Metrics.Spots, atlas.Metrics.ClosedPaths, found.Length, distinctPairs,
                found.Length == 0 ? null : found.Min(e => e.Read.SeedSegments),
                "Candidates are exact in this authored common-reach realization. Search covers whole contiguous route chunks and whole route pairs with at least two sections; it does not search arbitrary multi-route motifs, cyclic phases, unequal scale, or erased construction history."));
        }
        var templates = Templates();
        var offsets = new[] { new OffsetRead("origin", "Keep the origin", "empty displacement", [0, 0]),
            new OffsetRead("advance", "Advance four shared reaches", "H+ H+ H+ H+", [4, 0]),
            new OffsetRead("diagonal", "Advance two; rise two", "H+ H+ V+ V+", [2, 2]) };
        var actions = new List<ActionRead>();
        foreach (var template in templates)
            foreach (var offset in offsets)
                for (byte code = 0; code < 16; code++)
                {
                    var action = CentrelineAction.FromBits(template.Frame, code, template.Frame.Word(offset.Xy[0], offset.Xy[1]));
                    var applied = CentrelineAction.Apply(template.Frame, action, template.Sections);
                    if (!CentrelineAction.TryReadApplication(template.Frame, applied, out var read) || !read.Complete)
                        throw new InvalidOperationException("A complete laboratory action did not validate.");
                    actions.Add(new(template.Id, code, Bits(code), offset.Id, Points(template.Frame, template.Sections),
                        Points(template.Frame, read.Output), read.Complete, true));
                }
        return new
        {
            Schema = "gojo.symmetry-actions.v1",
            Scope = "Reusable native substitutions, displacements and traversal correspondences, with bounded recurrence search over the 18 authored atlas readings. This is an executable symmetry study, not autonomous concept discovery, a global minimum-description search, material transfer or geometric settlement.",
            Counts = new { Patterns = corpus.Count, Engines = engines.Count, WholePathPairs = distinctPairTotal, PairActionMatches = pairTotal, Actions = actions.Count, Templates = templates.Length, TReadings = 2, Controls = 5 },
            BitGrammar = new[] {
                new { Bit = 0, Name = "Exchange axes", Meaning = "Choose V as the image of H, and H as the image of V." },
                new { Bit = 1, Name = "Oppose H image", Meaning = "Reverse the ordered target direction selected for input H." },
                new { Bit = 2, Name = "Oppose V image", Meaning = "Reverse the ordered target direction selected for input V." },
                new { Bit = 3, Name = "Reverse traversal", Meaning = "Reverse the section order and each section's endpoint order, independently of the basis action." } },
            Corpus = corpus, Engines = engines, Pairs = pairReadings, TReadings = TReadings(),
            Templates = templates.Select(t => new TemplateRead(t.Id, t.Name, Points(t.Frame, t.Sections), IsClosed(t.Frame, t.Sections))),
            Offsets = offsets, Actions = actions, Controls = Controls(),
            Findings = new[] {
                "A2 and B6 admit overlapping boundary readings produced by vertical reflection plus advance. Shared source sections are one piece of evidence read twice, not duplicate material.",
                "Vertical reflection changes vertical direction in the selected frame; it does not itself reverse horizontal motion. Reversing traversal changes both the order and each directed section.",
                "One small instruction can describe a witnessed correspondence and generate a fresh copy on a different input. The application and match still retain separate evidence.",
                "The smallest tested seed is conditional on this grammar and on the chosen realization. It is not an original-engine probability or a complete storage-cost estimate.",
                "Topology, ordered motion, equal reaches, geometric support and projected crossings are different invariants. A shared silhouette does not imply shared routes or physical junctions.",
                "A desired rectangle or hexagon can next be a connected target claim. Turning mismatch into change still needs explicit response routes, movable parameters, resistance and retained unresolved tension; this study does not add an unseen shape solver." }
        };
    }

    public static Engine[] FindEngines(Atlas.Fixture f, string pattern)
    {
        var found = new List<Engine>(); var routes = Routes(f);
        for (var path = 0; path < routes.Length; path++)
        {
            var scope = (Relation)E(routes[path])[0]; var sections = E(scope).Cast<Relation>().ToArray();
            if (sections.Length < 2) continue;
            var points = Points(f.Frame, scope); var candidates = new List<(int Width, byte Code, int[] Shift)>();
            for (var width = 1; width <= sections.Length / 2; width++)
            {
                if (sections.Length % width != 0) continue;
                var seed = points.Take(width + 1).ToArray();
                for (byte code = 0; code < 16; code++)
                {
                    var first = MapPoints(seed, code, [0, 0]); var shift = Sub(points[width], first[0]);
                    var predicted = seed; var matches = true;
                    for (var i = width; i < sections.Length && matches; i += width)
                    {
                        predicted = MapPoints(predicted, code, shift);
                        matches = SamePoints(predicted, points.Skip(i).Take(width + 1).ToArray());
                    }
                    if (matches) candidates.Add((width, code, shift));
                }
            }
            // Geometry is only a prefilter. A shorter geometric match can have
            // the wrong alias pattern, so test longer widths until the first
            // native-valid seed for each action is found.
            foreach (var candidate in candidates.OrderBy(c => c.Width).ThenBy(c => c.Code))
            {
                if (found.Any(e => e.Read.PathIndex == path && e.Read.Code == candidate.Code)) continue;
                var width = candidate.Width; var action = CentrelineAction.FromBits(f.Frame, candidate.Code,
                    f.Frame.Word(candidate.Shift[0], candidate.Shift[1]));
                var seed = S(sections.Take(width).Cast<IElement>().ToArray()); var current = seed;
                var applications = new List<Relation>(); var matches = new List<Relation>();
                var feedback = new List<IElement>(); var count = sections.Length / width;
                var phases = Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray();
                var links = Enumerable.Range(0, count - 1).Select(i => new Relation(phases[i], phases[i + 1])).ToArray();
                var exact = true; var topology = true; Relation? firstOutput = null; Relation? firstTarget = null;
                for (var i = 1; i < count; i++)
                {
                    var application = CentrelineAction.Apply(f.Frame, action, current);
                    if (!CentrelineAction.TryReadApplication(f.Frame, application, out var read) || !read.Complete)
                        throw new InvalidOperationException("Native recurrence did not execute its complete proposal.");
                    var target = S(sections.Skip(i * width).Take(width).Cast<IElement>().ToArray());
                    var witness = CentrelineAction.Match(f.Frame, application, target);
                    if (!CentrelineAction.TryReadMatch(f.Frame, witness, out var match))
                        throw new InvalidOperationException("Unreadable native match.");
                    exact &= match.GeometryExact; topology &= match.SitePatternExact;
                    applications.Add(application); matches.Add(witness);
                    feedback.Add(new Relation(links[i - 1], new Relation(current, application)));
                    current = read.Output; firstOutput ??= current; firstTarget ??= target;
                }
                if (!exact || !topology) continue;
                var controller = new Relation(S(phases), S(links.Cast<IElement>().ToArray()));
                var surface = new Relation(new Relation(routes[path], new Relation(seed, action)),
                    new Relation(controller, new Relation(S(feedback.ToArray()), S(matches.Cast<IElement>().ToArray()))));
                var readout = new EngineRead($"{pattern}-p{path}-a{candidate.Code}", pattern, path, width, count,
                    candidate.Code, Bits(candidate.Code), candidate.Shift, Points(f.Frame, seed),
                    Points(f.Frame, firstOutput!), Points(f.Frame, firstTarget!), exact, topology,
                    sections.Length, width, 4, applications.Count,
                    "One seed and one constant action predict all subsequent chunks. Fresh copies preserve their own site pattern; the source's inter-chunk seam still needs an explicit connection in a generating machine. Counts omit frame, displacement, repeat-controller, correspondence and proof storage.");
                found.Add(new(surface, action, seed, applications.ToArray(), matches.ToArray(), readout));
            }
        }
        return found.ToArray();
    }

    public static PairEvidence[] FindPairs(Atlas.Fixture f, string pattern)
    {
        var found = new List<PairEvidence>(); var routes = Routes(f);
        for (var a = 0; a < routes.Length; a++) for (var b = a + 1; b < routes.Length; b++)
        {
            var sa = (Relation)E(routes[a])[0]; var sb = (Relation)E(routes[b])[0];
            if (E(sa).Length < 2 || E(sa).Length != E(sb).Length) continue;
            var ap = Points(f.Frame, sa); var bp = Points(f.Frame, sb);
            for (byte code = 0; code < 16; code++)
            {
                var proposal = MapPoints(ap, code, [0, 0]); var shift = Sub(bp[0], proposal[0]);
                if (!SamePoints(MapPoints(ap, code, shift), bp)) continue;
                var action = CentrelineAction.FromBits(f.Frame, code, f.Frame.Word(shift[0], shift[1]));
                var application = CentrelineAction.Apply(f.Frame, action, sa);
                var witness = CentrelineAction.Match(f.Frame, application, sb);
                if (!CentrelineAction.TryReadMatch(f.Frame, witness, out var match) || !match.GeometryExact || !match.SitePatternExact) continue;
                found.Add(new(pattern, a, b, code, shift, action, application, witness));
            }
        }
        return found.ToArray();
    }

    public static TRead[] TReadings() => [BuildT("A2").Read, BuildT("B6").Read];
    public static TCase BuildT(string source) => source switch {
        "A2" => T("A2", 1, 5, 7, [2, 2]), "B6" => T("B6", 1, 9, 15, [4, -4]),
        _ => throw new ArgumentException("No authored overlapping T selection for this source.", nameof(source)) };

    private static TCase T(string id, int start, int next, int count, int[] shift)
    {
        var f = Atlas.BuildSource(id); var route = Routes(f)[0]; var all = (Relation)E(route)[0]; var entries = E(all);
        var a = S(entries.Skip(start).Take(count).ToArray()); var b = S(entries.Skip(next).Take(count).ToArray());
        var action = CentrelineAction.FromBits(f.Frame, 4, f.Frame.Word(shift[0], shift[1]));
        var application = CentrelineAction.Apply(f.Frame, action, a);
        if (!CentrelineAction.TryReadApplication(f.Frame, application, out var applied) || !applied.Complete)
            throw new InvalidOperationException("T action failed.");
        var witness = CentrelineAction.Match(f.Frame, application, b);
        if (!CentrelineAction.TryReadMatch(f.Frame, witness, out var match)) throw new InvalidOperationException("T match failed.");
        var source = Points(f.Frame, a); var output = Points(f.Frame, applied.Output);
        var shared = Enumerable.Range(start, count).Intersect(Enumerable.Range(next, count)).ToArray();
        var read = new TRead(id + "-overlapping-boundaries", id == "A2" ? "Two open T-shaped boundaries" : "Two decorated T-shaped boundaries",
            id, Points(f.Frame, all), Enumerable.Range(start, count).ToArray(), Enumerable.Range(next, count).ToArray(), shared,
            source, output, Points(f.Frame, b), 4, Bits(4), shift, match.GeometryExact, match.SitePatternExact, shared.Length,
            id == "A2" ? "Seven-edge windows share three actual source sections. The transformed copy predicts the second window; the original overlapping reads reference one path. These are open T-shaped outlines, not three-way native junctions."
            : "Fifteen-edge windows share seven actual source sections. The curled flanges belong to a decorated T/corridor read. Literal T contacts against the framing rail are a separate projected read; their native course and rail are not welded.",
            Enumerable.Range(0, count).Select(i => (object)new { Index = i, SourceStep = Sub(source[i + 1], source[i]), OutputStep = Sub(output[i + 1], output[i]) }).ToArray());
        // Both readings select actual owner sections. Shared participation is
        // connected as references, not reconstructed from coincident pixels.
        var surface = new Relation(new Relation(route, S(a, b)),
            new Relation(action, new Relation(application, new Relation(witness, S(shared.Select(i => entries[i]).ToArray())))));
        return new(f.Frame, surface, read);
    }

    public static ControlRead[] Controls()
    {
        var f = new Curve.Frame(new QualitativeMotionFrame());
        var a = Route(f, [[0, 0], [2, 0], [2, 1], [4, 1]]);
        var reflected = Route(f, [[0, 0], [2, 0], [2, -1], [4, -1]]);
        var changed = Route(f, [[0, 0], [2, 0], [2, -2], [4, -2]]);
        var disconnected = Route(f, [[0, 0], [2, 0], [2, -1], [4, -1]], disconnect: true);
        var backwards = S(E(reflected).Cast<Relation>().Reverse().Select(s => (IElement)f.Reverse(s)).ToArray());
        return [Control("exact", "A reflection predicts the directed path", 4, reflected,
                "Horizontal steps are unchanged; vertical steps reverse. Source and copy have independent site identities with the same alias pattern."),
            Control("reach-change", "Same turns; different reach", 4, changed,
                "The reflected action misses one changed reach. Similar turn order is weaker than exact correspondence; no settlement silently repairs it."),
            Control("unjoined", "Same coordinates; broken handoffs", 4, disconnected,
                "The support matches, but each section has independent endpoints. Coordinate equality does not prove path continuity."),
            Control("reverse-missing", "Same support; reversed traversal", 4, backwards,
                "Reflection alone cannot explain the target's order. A support-only read would lose this distinction."),
            Control("reverse-explicit", "Reflection and reversal together", 12, backwards,
                "Adding the separate traversal correspondence accounts for the reversed target without changing the basis law.")];

        ControlRead Control(string id, string name, byte code, Relation target, string note)
        {
            var action = CentrelineAction.FromBits(f, code, f.Word(0, 0)); var applied = CentrelineAction.Apply(f, action, a);
            if (!CentrelineAction.TryReadApplication(f, applied, out var read)) throw new InvalidOperationException();
            var witness = CentrelineAction.Match(f, applied, target);
            if (!CentrelineAction.TryReadMatch(f, witness, out var match)) throw new InvalidOperationException();
            return new(id, name, Points(f, a), Points(f, read.Output), Points(f, target), match.GeometryExact, match.SitePatternExact, note);
        }
    }

    private static Template[] Templates()
    {
        var a = Atlas.BuildSource("A2"); var b = Atlas.BuildSource("B6"); var w = Atlas.BuildSource("CR4");
        var simple = new Curve.Frame(new QualitativeMotionFrame());
        return [new("a2", "A2 open T boundary", a.Frame, S(E((Relation)E(Routes(a)[0])[0]).Skip(1).Take(7).ToArray())),
            new("b6", "B6 decorated boundary", b.Frame, S(E((Relation)E(Routes(b)[0])[0]).Skip(1).Take(15).ToArray())),
            new("weave", "Weave outer course", w.Frame, (Relation)E(Routes(w)[0])[0]),
            new("square", "A closed square", simple, Route(simple, [[0, 0], [3, 0], [3, 3], [0, 3], [0, 0]], closed: true)),
            new("asymmetric", "A new asymmetric step", simple, Route(simple, [[0, 0], [1, 0], [1, 3], [3, 3], [3, 2]]))];
    }

    public static Relation Route(Curve.Frame f, int[][] points, bool closed = false, bool disconnect = false)
    {
        var sections = new List<IElement>(); IElement origin = new Placeholder(); IElement cursor = origin;
        for (var i = 1; i < points.Length; i++)
        {
            if (disconnect) cursor = new Placeholder();
            IElement end = closed && i == points.Length - 1 ? origin : new Placeholder();
            sections.Add(f.Line(cursor, end, f.Word(points[i - 1][0], points[i - 1][1]), f.Word(points[i][0], points[i][1])));
            cursor = end;
        }
        return S(sections.ToArray());
    }

    public static int[][] Points(Curve.Frame frame, Relation scope)
    {
        var sections = E(scope).Cast<Relation>().Select(frame.Read).ToArray();
        if (sections.Length == 0) return [];
        if (sections.Any(r => !r.Complete || r.Controls.Length != 2)) throw new ArgumentException("Expected a complete line-section scope.");
        return new[] { sections[0].Controls[0] }.Concat(sections.Select(r => r.Controls[^1]))
            .Select(p => new[] { checked((int)p.X), checked((int)p.Y) }).ToArray();
    }

    private static bool IsClosed(Curve.Frame f, Relation scope)
    { var parts = E(scope).Cast<Relation>().Select(f.Read).ToArray(); return parts.Length != 0 && ReferenceEquals(parts[0].Start, parts[^1].End); }
    private static Relation[] Routes(Atlas.Fixture f) => E((Relation)E(f.Surface)[2]).Cast<Relation>().ToArray();
    private static int[][] MapPoints(int[][] source, int code, int[] shift)
    {
        var points = (code & 8) == 0 ? source : source.Reverse();
        return points.Select(p => { var h = (code & 2) == 0 ? p[0] : -p[0]; var v = (code & 4) == 0 ? p[1] : -p[1];
            return (code & 1) == 0 ? new[] { checked(h + shift[0]), checked(v + shift[1]) } : new[] { checked(v + shift[0]), checked(h + shift[1]) }; }).ToArray();
    }
    private static bool SamePoints(int[][] a, int[][] b) => a.Length == b.Length && a.Zip(b).All(p => p.First.SequenceEqual(p.Second));
    private static int[] Sub(int[] a, int[] b) => [checked(a[0] - b[0]), checked(a[1] - b[1])];
    private static string Bits(int code) => Convert.ToString(code, 2).PadLeft(4, '0');
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out var seam, out var tail) && seam.Equals(tail)
        ? entries : throw new ArgumentException("Expected a closed scope.");
    private static string SourceName(string id) => id switch {
        "A1" => "Running key", "A2" => "Opposed hooks", "A3" => "Key with four-window inserts", "A4" => "Offset angular interlock",
        "A5" => "Continuous inward–outward winding", "A6" => "Nested squares and turned hooks", "B1" => "Hooks against rails",
        "B3" => "Stepped interlocking courses", "B4" => "Stacked crosses", "B5" => "Battlement", "B6" => "Framed deep interlock",
        "B8" => "Offset battlements and spots", "CL1" => "Narrow framed interlock", "CL2" => "Alternating finite coils",
        "CL5" => "Diagonal zigzag", "CL6" => "Clipped diagonal lattice", "CR3" => "Standalone deep interlock", "CR4" => "Four-strand weave", _ => id };

    public static int Run(string[] args)
    {
        var output = Path.Combine("artifacts", "symmetry-actions");
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else throw new ArgumentException("Usage: --symmetry-actions [--output DIRECTORY]");
        Directory.CreateDirectory(output); var file = Path.Combine(output, "study-data.json");
        File.WriteAllText(file, JsonSerializer.Serialize(Report(), new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
        Console.WriteLine("Symmetry action study recorded at " + Path.GetFullPath(file)); return 0;
    }
}
