using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Encoding.Structures;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;
using Art = Gojo.Experiments.QualitativeFrieze.Composition.BandEvidenceGallery;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Reads the same native paths through width rungs and selected views.
/// SVG commands, paint and visibility masks are downstream presentation only.</summary>
public static class BandEvidenceExperiment
{
    public sealed record ThreadRead(Relation Source, IReadOnlyList<Band.EnvelopeRead> Envelopes, bool JoinedSides, bool SideCycles, Relation Surface);
    public sealed record Read(IReadOnlyList<ThreadRead> Threads, IReadOnlyList<Band.CrossingRead> Crossings,
        IReadOnlyList<Band.OrderRead> Orders, Relation Remainder, Relation Surface);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly Art.ViewBox View = new(0, 0, 42, 30);
    private static readonly string[] Colours = ["#286672", "#b45b40", "#536b42", "#806078"];

    public static Read Inspect(BandEvidenceCorpus.Layer layer)
    {
        // Read exact public layer scope. Host collections are not the authority.
        var threads = new List<ThreadRead>(); var crosses = new List<Band.CrossingRead>(); var orders = new List<Band.OrderRead>(); var pending = new List<IElement>();
        if (layer.Surface is not { Left: Relation { Left: var frame, Right: Relation group },
            Right: Relation { Left: Relation { Left: Relation crossings, Right: Relation precedence } } } ||
            !ReferenceEquals(frame, layer.Frame.Surface) || !Closed(group) || !Closed(crossings) || !Closed(precedence))
            return new([], [], [], StructuralConstraint.Open(layer.Surface), new(layer.Surface, StructuralConstraint.Open(layer.Surface)));
        foreach (var entry in E(group))
        {
            if (entry is not Relation { Left: Relation source, Right: Relation envs } thread || !Closed(envs))
            { pending.Add(entry); threads.Add(new(StructuralConstraint.Open(entry), [], false, false, new(entry, StructuralConstraint.Open(entry)))); continue; }
            var read = E(envs).Select(e => Band.ReadEnvelope(layer.Frame, e as Relation ?? StructuralConstraint.Open(e))).ToArray();
            var joined = read.Length > 0 && read.All(e => e.Sweep.Complete) && read.Zip(read.Skip(1)).All(pair =>
                SideConnection(pair.First.Sweep, pair.Second.Sweep));
            var cyclic = joined && SideConnection(read[^1].Sweep, read[0].Sweep);
            foreach (var unresolved in read.Where(e => !e.Sweep.Complete)) pending.Add(unresolved.Surface);
            var connections = new List<IElement>();
            void Compare(Band.SweepRead a, Band.SweepRead b)
            {
                var endpointPairs = new Relation(new Relation(a.SideA.End, b.SideA.Start), new Relation(a.SideB.End, b.SideB.Start));
                var witness = new Relation(new Relation(a.Surface, b.Surface), SideConnection(a, b) ? S(endpointPairs) : StructuralConstraint.Open(endpointPairs));
                connections.Add(witness); if (!SideConnection(a, b)) pending.Add(witness);
            }
            foreach (var pair in read.Zip(read.Skip(1))) Compare(pair.First.Sweep, pair.Second.Sweep);
            if (read.Length > 0 && read[0].Sweep.Spine.Complete && read[^1].Sweep.Spine.Complete &&
                Same(read[^1].Sweep.Spine.End, read[0].Sweep.Spine.Start)) Compare(read[^1].Sweep, read[0].Sweep);
            threads.Add(new(source, read, joined, cyclic, new(thread, new Relation(S(read.Select(e => (IElement)e.Surface).ToArray()), S(connections.ToArray())))));
        }
        var exposed = threads.SelectMany(t => t.Envelopes).Select(e => e.Sweep.Spine.Section).ToArray();
        bool Member(Relation section) => exposed.Count(s => ReferenceEquals(s, section)) == 1;
        foreach (var entry in E(crossings))
        {
            var read = Band.ReadCrossing(layer.Frame, entry as Relation ?? StructuralConstraint.Open(entry));
            if (!Member(read.First) || !Member(read.Second)) read = read with { Crosses = null, Surface = new(read.Surface, StructuralConstraint.Open(entry)) };
            if (read.Crosses is null) pending.Add(read.Surface);
            crosses.Add(read);
        }
        foreach (var entry in E(precedence))
        {
            var read = Band.ReadOrder(layer.Frame, entry as Relation ?? StructuralConstraint.Open(entry));
            if (entry is not Relation order || !E(crossings).Any(c => ReferenceEquals(c, order.Left)) ||
                read.Front is null || read.Back is null || !Member(read.Front) || !Member(read.Back))
                read = read with { Complete = false, Surface = new(read.Surface, StructuralConstraint.Open(entry)) };
            if (!read.Complete) pending.Add(read.Surface);
            orders.Add(read);
        }
        var remainder = S(pending.ToArray());
        return new(threads, crosses, orders, remainder, new(layer.Surface, new Relation(S(threads.Select(t => (IElement)t.Surface).ToArray()),
            new Relation(new Relation(S(crosses.Select(c => (IElement)c.Surface).ToArray()), S(orders.Select(c => (IElement)c.Surface).ToArray())), remainder))));
    }

    private static bool SideConnection(Band.SweepRead a, Band.SweepRead b) => a.Complete && b.Complete &&
        Same(a.SideA.End, b.SideA.Start) && Same(a.SideB.End, b.SideB.Start) &&
        a.SideA.Controls[^1] == b.SideA.Controls[0] && a.SideB.Controls[^1] == b.SideB.Controls[0];

    public static IReadOnlyList<IReadOnlyList<Band.SweepRead>> ConnectedRuns(IEnumerable<Band.EnvelopeRead> envelopes, bool occupiedOnly = false)
    {
        var runs = new List<IReadOnlyList<Band.SweepRead>>(); var run = new List<Band.SweepRead>();
        foreach (var e in envelopes)
        {
            var selected = e.Sweep.Complete && (!occupiedOnly || e.Occupied == true);
            if (!selected || run.Count > 0 && !SideConnection(run[^1], e.Sweep))
            { if (run.Count > 0) runs.Add(run.ToArray()); run.Clear(); }
            if (selected) run.Add(e.Sweep);
        }
        if (run.Count > 0) runs.Add(run.ToArray()); return runs;
    }

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "path-bands");
        var limit = int.MaxValue;
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else if (args[i] == "--limit" && i + 1 < args.Length && int.TryParse(args[++i], out var n) && n > 0) limit = n;
            else throw new ArgumentException("Usage: --path-bands [--output DIRECTORY] [--limit COUNT]");
        Directory.CreateDirectory(output); Directory.CreateDirectory(Path.Combine(output, "native"));
        var watch = Stopwatch.StartNew(); var examples = new List<Art.Example>(); var cases = BandEvidenceCorpus.Cases().Take(limit).ToArray();
        var results = new List<object>(); var complete = 0; var open = 0; var collapsed = 0; var crossingCount = 0; var orderedCount = 0;
        foreach (var sample in cases)
        {
            var narrow = Inspect(sample.Narrow); var broad = Inspect(sample.Broad);
            var all = broad.Threads.SelectMany(t => t.Envelopes).ToArray();
            var paths = all.Select(e => e.Sweep.Spine).Where(p => p.Complete).ToArray();
            var sharedSites = paths.SelectMany(p => new[] { p.Start, p.End }).Distinct(FieldNative.Participants.Instance)
                .Count(site => paths.Sum(p => (Same(p.Start, site) ? 1 : 0) + (Same(p.End, site) ? 1 : 0)) > 2);
            var pairJoins = paths.SelectMany((a, i) => paths.Skip(i + 1).Select(b => Curve.Join(a, b))).ToArray();
            var sourceCaps = all.Sum(e => (e.Start.Capped == true ? 1 : 0) + (e.End.Capped == true ? 1 : 0));
            var continuations = all.Sum(e => (e.Start.Continuing == true ? 1 : 0) + (e.End.Continuing == true ? 1 : 0));
            complete += all.Count(e => e.Sweep.Complete); open += all.Count(e => !e.Sweep.Complete);
            collapsed += all.Count(e => e.Sweep.Collapsed == true); crossingCount += broad.Crossings.Count(c => c.Crosses == true);
            orderedCount += broad.Orders.Count(o => o.Complete);
            var panels = CreatePanels(sample, narrow, broad);
            var evidence = new List<Art.Evidence> {
                new("Path ↔ sides", $"{all.Count(e => e.Sweep.Complete)} complete · {all.Count(e => !e.Sweep.Complete)} open sweeps",
                    "Actual control-word sums and shared interpolation parameters", "Changing width retains the exact source spine. Open width is never replaced by a zero displacement."),
                new("Transverse occupancy", $"{all.Count(e => e.Occupied == true)} selected · {all.Count(e => e.Sweep.Collapsed == true)} collapsed",
                    "An independent selection of bounded ruled support", "Selected occupancy does not certify positive area, absence of folds, or a simple global region."),
                new("Ends", $"{sourceCaps} visible caps · {continuations} continuations",
                    "Connected cross-section end conditions", "The drawing clip is an observation. Filled SVG paths may close outside it without creating a native cap."),
                new("Contact, joining and depth", $"{sharedSites} branch sites · {broad.Crossings.Count(c => c.Crosses == true)} crossings · {broad.Orders.Count(o => o.Complete)} ordered crossings",
                    "Site identity / strict interior straight crossing / separate precedence surface", "Crossings do not weld paths. Joined branches need no depth order. Unspecified order stays open."),
                new("Construction context", sample.Origin, "Retained source plus narrow/broad correspondence", "This round constructs and reads native bands; it is not a blind outline-to-skeleton parser or a trained recognizer.") };
            var audit = new { ThreadCount = broad.Threads.Count, Sections = all.Length, Complete = all.Count(e => e.Sweep.Complete),
                Open = all.Count(e => !e.Sweep.Complete), Collapsed = all.Count(e => e.Sweep.Collapsed == true),
                Occupied = all.Count(e => e.Occupied == true), VisibleCaps = sourceCaps, Continuations = continuations,
                SharedBranchSites = sharedSites, CertifiedCrossings = broad.Crossings.Count(c => c.Crosses == true),
                OrderedCrossings = broad.Orders.Count(o => o.Complete), ConnectedSideThreads = broad.Threads.Count(t => t.JoinedSides),
                PairedSideCycles = broad.Threads.Count(t => t.SideCycles), OpenReadItems = E(broad.Remainder).Length,
                SameSpinesAcrossWidthRungs = SameSpines(narrow, broad),
                WidthUnits = "Repeated participation in one uncalibrated reach; no measured radii or pixel widths",
                Limits = "No exact normal-distance offset, general curved crossing solver, global union/overlap certificate or automatic medial-axis inference" };
            // Each view selects actual exposed participants while carrying the
            // complete source. Paint and clipping remain outside this grammar.
            Relation Focus(IEnumerable<IElement> selected) => new(sample.Surface, new Relation(S(selected.ToArray()), sample.Source));
            var selections = S(Focus(paths.Select(p => (IElement)p.Section)),
                Focus(all.Select(e => (IElement)e.Sweep.Surface)),
                Focus(all.Where(e => e.Sweep.Complete).SelectMany(e => new IElement[] { e.Sweep.SideA.Section, e.Sweep.SideB.Section })),
                Focus(all.Select(e => (IElement)e.Sweep.Support)));
            var root = new Relation(sample.Surface, new Relation(new Relation(narrow.Surface, broad.Surface),
                new Relation(selections, S(pairJoins.Select(j => (IElement)j.Surface).ToArray()))));
            var nativeName = "native/" + sample.Id.ToLowerInvariant() + ".gojo.json.gz";
            Save(Path.Combine(output, nativeName), root);
            examples.Add(new(sample.Id, sample.Name, sample.Note, panels, evidence, audit, nativeName));
            results.Add(new { sample.Id, sample.Name, Audit = audit });
        }
        var summary = new { Cases = cases.Length, Views = examples.Sum(e => e.Panels.Count), CompleteSweeps = complete,
            OpenSweeps = open, CollapsedSweeps = collapsed, CertifiedCrossings = crossingCount, OrderedCrossings = orderedCount,
            Seconds = Math.Round(watch.Elapsed.TotalSeconds, 3),
            Scope = "Native path / paired boundary / ruled occupancy / end / crossing-order construction and local reads. No statistical training or blind parsing.",
            Results = results };
        File.WriteAllText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(summary, Json));
        Art.Write(output, summary, examples);
        Console.WriteLine($"Path bands: {cases.Length} cases, {complete} complete and {open} open sweeps, {orderedCount} ordered crossings. {Path.GetFullPath(output)}");
        return 0;
    }

    public static bool SameSpines(Read a, Read b) => a.Threads.Count == b.Threads.Count && a.Threads.Zip(b.Threads).All(pair =>
        pair.First.Envelopes.Count == pair.Second.Envelopes.Count && pair.First.Envelopes.Zip(pair.Second.Envelopes).All(e =>
            ReferenceEquals(e.First.Sweep.Spine.Section, e.Second.Sweep.Spine.Section)));

    public static IReadOnlyList<Art.Panel> CreatePanels(BandEvidenceCorpus.Case source) =>
        CreatePanels(source, Inspect(source.Narrow), Inspect(source.Broad));
    private static IReadOnlyList<Art.Panel> CreatePanels(BandEvidenceCorpus.Case source, Read narrow, Read broad) => [
        Render(source, broad, 0, "Centre-path read"), Render(source, narrow, 1, "Narrow occupied bands"),
        Render(source, broad, 2, "Paired boundaries"), Render(source, broad, 3,
            source.ReverseFigure ? "Reversed contrast" : source.Decorate ? "Decorated band read" : "Broad occupied bands") ];

    private static Art.Panel Render(BandEvidenceCorpus.Case source, Read read, int mode, string name)
    {
        var ink = new List<Art.InkPath>(); var supportInk = new List<Art.InkPath>();
        var negative = mode == 3 && source.ReverseFigure;
        var background = negative ? "#285762" : "#fbf8ee";
        var openOrder = read.Crossings.Any(c => c.Crosses != false && !read.Orders.Any(o => o.Complete &&
            (ReferenceEquals(o.Front, c.First) || ReferenceEquals(o.Front, c.Second)) &&
            (ReferenceEquals(o.Back, c.First) || ReferenceEquals(o.Back, c.Second))));
        for (var i = 0; i < read.Threads.Count; i++)
        {
            var thread = read.Threads[i]; var envs = thread.Envelopes;
            var good = envs.Where(e => e.Sweep.Complete).ToArray();
            var spines = envs.Select(e => e.Sweep.Spine).Where(s => s.Complete).ToArray();
            var colour = negative ? "#fbf8ee" : source.Joined ? Colours[0] : Colours[i % Colours.Length];
            var cuts = new List<Art.MaskCut>();
            foreach (var order in read.Orders.Where(o => o.Complete && spines.Any(s => ReferenceEquals(s.Section, o.Back))))
            {
                var frontEnvelope = read.Threads.SelectMany(t => t.Envelopes).Single(e => ReferenceEquals(e.Sweep.Spine.Section, order.Front));
                var front = frontEnvelope.Sweep;
                // View policy: a small gap survives when side detail is below
                // the chosen line-drawing resolution. It never cuts the spine.
                if (mode == 0) cuts.Add(new(CurvePath(front.Spine), .8));
                else if (front.Complete && frontEnvelope.Occupied == true) {
                    cuts.Add(new(FillPath([front]), 0));
                    cuts.Add(new(CurvePath(front.SideA), mode == 1 ? .22 : .35));
                    cuts.Add(new(CurvePath(front.SideB), mode == 1 ? .22 : .35));
                }
            }
            if (mode == 0 || good.Length == 0 || good.All(e => e.Sweep.Collapsed == true))
            {
                foreach (var p in spines) ink.Add(new(CurvePath(p), Stroke: good.Length == 0 ? "#a47537" : colour,
                    StrokeWidth: .18, Note: good.Length == 0 ? "Band unresolved; known spine retained" : "Selected centre-path projection",
                    Role: "spine", Dash: good.Length == 0 ? ".45 .25" : null, Cuts: cuts));
            }
            else
            {
                var fill = string.Join(" ", ConnectedRuns(envs, occupiedOnly: true).Select(FillPath));
                var boundaryCuts = new List<Art.MaskCut>(cuts);
                if (source.Joined && mode is 1 or 3)
                    boundaryCuts.AddRange(read.Threads.Where(other => !ReferenceEquals(other, thread))
                        .SelectMany(other => other.Envelopes).Where(e => e.Sweep.Complete && e.Occupied == true)
                        .Select(e => new Art.MaskCut(FillPath([e.Sweep]))));
                // No synthetic internal caps: draw a continuous filled support
                // separately from the actual visible boundary selection.
                if (fill.Length > 0) supportInk.Add(new(fill, Fill: mode == 2 ? "#e5d9ba" : openOrder ? "#738c8e" : colour, Stroke: "none", StrokeWidth: 0,
                    Role: "occupied-support", Opacity: mode == 2 ? .28 : openOrder ? .3 : 1, Cuts: cuts));
                foreach (var e in good)
                {
                    ink.Add(new(CurvePath(e.Sweep.SideA), Stroke: mode == 2 ? "#286672" : openOrder ? colour : negative ? "#e6b96a" : "#213e43", StrokeWidth: .14, Role: "side-a", Cuts: boundaryCuts));
                    ink.Add(new(CurvePath(e.Sweep.SideB), Stroke: mode == 2 ? "#b45b40" : openOrder ? colour : negative ? "#e6b96a" : "#213e43", StrokeWidth: .14, Role: "side-b", Cuts: boundaryCuts));
                    if (e.Start.Capped == true) ink.Add(new(CapPath(e.Sweep, true), Stroke: "#213e43", StrokeWidth: .14, Role: "visible-cap", Cuts: boundaryCuts));
                    if (e.End.Capped == true) ink.Add(new(CapPath(e.Sweep, false), Stroke: "#213e43", StrokeWidth: .14, Role: "visible-cap", Cuts: boundaryCuts));
                }
                if (mode == 3 && source.Decorate)
                    foreach (var p in spines) ink.Add(new(CurvePath(p), Stroke: "#f2d693", StrokeWidth: .2,
                        Note: "Accent follows the retained spine", Role: "spine-accent", Cuts: cuts));
            }
        }
        if (mode == 0)
        {
            // A joint marker is a projection of actual shared endpoint identity.
            var paths = read.Threads.SelectMany(t => t.Envelopes).Select(e => e.Sweep.Spine).Where(s => s.Complete).ToArray();
            foreach (var site in paths.SelectMany(p => new[] { p.Start, p.End }).Distinct(FieldNative.Participants.Instance)
                .Where(site => paths.Sum(p => (Same(p.Start, site) ? 1 : 0) + (Same(p.End, site) ? 1 : 0)) > 2))
            {
                var p = paths.First(p => Same(p.Start, site) || Same(p.End, site));
                var point = Same(p.Start, site) ? p.Controls[0] : p.Controls[^1];
                ink.Add(new($"M {Point(point)} l .01 0", Stroke: "#b45b40", StrokeWidth: .75, Role: "shared-site"));
            }
        }
        return new(name, supportInk.Concat(ink).ToArray(), mode switch {
            0 => "Read the retained paths. Ordered underpasses show a small visibility gap; no path is deleted.",
            1 => "Native narrow side profiles and selected occupancy. Width is not a screen-space stroke property.",
            2 => "Side A is blue; side B is rust. Only actual end caps are drawn. Pale support is a selected ruled construction.",
            _ => openOrder ? "Depth order is unresolved: neutral translucent support and uninterrupted coloured side boundaries show superposition without choosing a foreground." :
                negative ? "Only contrast polarity changes. The same native support and path identities remain available." :
                "Filled supports with visible side edges. Accents follow the spine; depth masks preserve hidden passage structure." }, View, background);
    }

    private static string FillPath(IReadOnlyList<Band.SweepRead> sweeps)
    {
        if (sweeps.Count == 0) return "";
        // Transported side-site joins have already been checked by Inspect.
        // Separate loop closures remain subpaths under the even/nonzero rules.
        var a = string.Join(" ", sweeps.Select((s, i) => CurvePath(s.SideA, move: i == 0)));
        var reversed = sweeps.Reverse().ToArray();
        var b = string.Join(" ", reversed.Select((s, i) => CurvePath(s.SideB, reverse: true, move: false)));
        return a + $" L {Point(reversed[0].SideB.Controls[^1])} " + b + " Z";
    }
    private static string CapPath(Band.SweepRead s, bool start) => "M " + Point(start ? s.SideA.Controls[0] : s.SideA.Controls[^1]) +
        " L " + Point(start ? s.SideB.Controls[0] : s.SideB.Controls[^1]);
    private static string CurvePath(Curve.Read read, bool reverse = false, bool move = true)
    {
        if (!read.Complete) return "";
        var c = reverse ? read.Controls.Reverse().ToArray() : read.Controls;
        return (move ? "M " + Point(c[0]) + " " : "") + (c.Length == 2 ? "L " + Point(c[1]) :
            c.Length == 3 ? "Q " + Point(c[1]) + " " + Point(c[2]) : "C " + Point(c[1]) + " " + Point(c[2]) + " " + Point(c[3]));
    }
    private static string Point(Curve.Vector v) => F(v.X) + " " + F(v.Y);
    private static string F(object v) => Convert.ToString(v, CultureInfo.InvariantCulture)!;
    private static bool Same(IElement a, IElement b) => FieldNative.Same(a, b);
    private static void Save(string path, Relation root)
    {
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("qualitative-path-band-v1", root));
        using var file = File.Create(path); using var gzip = new GZipStream(file, CompressionLevel.SmallestSize);
        using var writer = new StreamWriter(gzip); writer.Write(json);
    }
}
