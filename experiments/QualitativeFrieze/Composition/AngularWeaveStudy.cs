using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Parts = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentBandComposition;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>An authored four-course angular interlace. Native section identity,
/// repeated word applications, material selection and local crossing order are
/// separate. Coordinates below select a common-reach realization of the supplied
/// reference; neither a photograph reconstruction nor learned weaving is claimed.</summary>
public static class AngularWeaveStudy
{
    // root = Closed(frame, pitch word, geometry phase controller,
    //               Closed(strands), Closed(crossing rows), Closed(generation receipts))
    // strand = Closed(Closed(ordered sections), Closed(material sections),
    //                 phase seed, Closed(encounter predictions))
    // crossing row = crossing -> (order -> (local depth axis -> Closed(samples)))
    // depth sample = actual source section -> actual endpoint of local depth axis
    // encounter prediction = (crossing -> actual section) -> (phase -> exchange read)
    // generation = (actual controller occurrence -> reusable local words) ->
    //              (Closed(substitution/sum receipts) -> Closed(actual sections))
    // The local two-ended depth axis has no metric height. +/-1 is a display read.

    public readonly record struct Pt(int X, int Y);
    public sealed record Segment(int Id, int Strand, int Slot, Pt From, Pt To, bool Present, int FromSite, int ToSite);
    public sealed record Crossing(int Id, Pt At, int FirstSegment, int SecondSegment,
        int? FrontSegment, int? BackSegment, int? FirstZ, int? SecondZ, bool Projected, bool Material,
        bool OrderKnown, bool DepthAgrees);
    public sealed record Visit(int Crossing, int Segment, bool? Over, int? Z, bool? ExpectedOver);
    public sealed record Strand(int Id, bool Continuous, bool Closed, bool RouteContinuous, bool RouteClosed,
        Visit[] Visits, int Alternations, int AdjacentVisits, int PredictionMatches, int PredictionChecks);
    public sealed record Check(string Key, string Claim, int Met, int Checked, string? Limit = null);
    public sealed record Summary(int Strands, int MaterialComponents, int MaterialEnds, int Crossings,
        int KnownOrders, int DepthAgreements, int Alternations, int AdjacentVisits,
        int ClosedStrands, int PresentSegments, int SegmentSlots, int ProjectedEndpointContacts);
    public sealed record Case(string Id, string Name, string Note, Segment[] Segments, Crossing[] Crossings,
        Strand[] Strands, Summary Summary, Check[] Checks);
    public sealed record Fixture(Curve.Frame Frame, Relation Surface);
    private sealed record NativeSection(int Strand, int Slot, Relation Section, Curve.Read Read);
    private sealed record Encounter(int First, int Second, Pt At, Relation Surface);
    private sealed record VisitSite(int Crossing, int Segment, BigInteger Progress);

    public static Fixture Build(int bays = 3, bool mirror = false, int? flipCrossing = null,
        bool invertDepth = false, int? missingSegment = null, int shiftInner = 0, int? unknownCrossing = null)
    {
        if (bays is < 1 or > 6 || shiftInner is < -2 or > 2) throw new ArgumentOutOfRangeException(nameof(bays));
        var frame = new Curve.Frame(new QualitativeMotionFrame());
        var phasePair = new Relation(new Placeholder(), new Placeholder());
        var exchange = QualitativeWeave.CreateExchangeLaw(phasePair);
        var controller = new Relation(phasePair, exchange);
        var identity = Parts.Identity(frame);
        var reflection = Parts.Map(frame, frame.Word(1, 0), frame.Word(-1, 0), frame.Word(0, -1), frame.Word(0, 1));
        var phaseMaps = S(new Relation(phasePair.Left, identity.Surface), new Relation(phasePair.Right, reflection.Surface));
        var generations = new List<IElement>();
        var native = new List<NativeSection>();
        var strandSections = new List<Relation[]>();
        var seedWords = new[]
        {
            new[] { new Pt(0, 0), new Pt(16, 0), new Pt(28, 12) },
            new[] { new Pt(2, 4), new Pt(6, 4), new Pt(10, 8), new Pt(14, 8), new Pt(22, 0), new Pt(30, 8) }
        }.Select(points => S(points.Select(p => (IElement)frame.Word(p.X, p.Y)).ToArray())).ToArray();
        for (var strand = 0; strand < 4; strand++)
        {
            var sections = new List<Relation>();
            IElement phase = (strand % 2 == 0) != mirror ? phasePair.Left : phasePair.Right;
            IElement cursor = new Placeholder();
            var previousWord = (Relation?)null;
            var local = seedWords[strand / 2];
            for (var bay = 0; bay < bays; bay++)
            {
                var transform = Same(phase, phasePair.Left) ? identity : reflection;
                var displacement = frame.Word(bay * 28 + (strand == 2 ? shiftInner : 0), Same(phase, phasePair.Left) ? 0 : 12);
                var receipts = new List<IElement>();
                var positions = E(local).Cast<Relation>().Select(word =>
                {
                    var mapped = transform.Apply(word);
                    if (!transform.TryReadComplete(mapped, out var image)) throw new InvalidOperationException("Authored word substitution failed.");
                    var sum = frame.Motion.Reduce(displacement, image);
                    if (!frame.Motion.TryReadResult(sum, out var result, out _, out _)) throw new InvalidOperationException("Authored translated word failed.");
                    receipts.Add(new Relation(mapped, sum)); return result;
                }).ToArray();
                if (previousWord is not null && (!frame.Vector(previousWord, out var oldEnd) || !frame.Vector(positions[0], out var newStart) || oldEnd != newStart))
                    throw new InvalidOperationException("The authored repeat cannot share a site at different positions.");
                var actual = new List<IElement>();
                for (var i = 1; i < positions.Length; i++)
                {
                    var next = new Placeholder();
                    var section = frame.Line(cursor, next, i == 1 && previousWord is not null ? previousWord : positions[i - 1], positions[i]);
                    var read = frame.Read(section);
                    native.Add(new(strand, sections.Count, section, read));
                    sections.Add(section); actual.Add(section); cursor = next;
                }
                previousWord = positions[^1];
                var step = FiniteLaw.Evaluate(exchange, S(phase), S(phasePair));
                var nextPhase = Result(step);
                var occurrence = new Relation(new Relation(controller, phaseMaps), new Relation(phase, new Relation(nextPhase, step)));
                generations.Add(new Relation(new Relation(occurrence, local), new Relation(S(receipts.ToArray()), S(actual.ToArray()))));
                phase = nextPhase;
            }
            strandSections.Add(sections.ToArray());
        }

        // Interior geometric encounter is an observation. It creates no shared
        // route endpoint and never changes material identity.
        var encounters = new List<Encounter>();
        for (var a = 0; a < native.Count; a++)
        for (var b = a + 1; b < native.Count; b++)
        {
            var first = native[a]; var second = native[b];
            if (first.Strand == second.Strand) continue;
            if (!Intersect(first.Read.Controls[0], first.Read.Controls[^1], second.Read.Controls[0], second.Read.Controls[^1], out var at)) continue;
            var crossing = Band.Crossing(frame, first.Section, second.Section, frame.Word(at.X, at.Y));
            if (crossing.Crosses != true) throw new InvalidOperationException("An authored strict encounter failed its native reader.");
            encounters.Add(new(a, b, at, crossing.Surface));
        }
        encounters = encounters.OrderBy(c => c.At.X).ThenBy(c => c.At.Y).ThenBy(c => c.First).ToList();
        if (flipCrossing is { } flip && (uint)flip >= encounters.Count || unknownCrossing is { } unknown && (uint)unknown >= encounters.Count ||
            missingSegment is { } absent && (uint)absent >= native.Count) throw new ArgumentOutOfRangeException(nameof(flipCrossing));

        // Independent encounter sequences use the same visible exchange law.
        // These authored seeds make the reference realization alternating. If
        // a perturbation produces equal offers, the order stays open.
        bool[] seeds = [false, true, true, false];
        var predictions = Enumerable.Range(0, 4).Select(_ => new List<IElement>()).ToArray();
        var offers = new Dictionary<(int Crossing, int Segment), bool>();
        for (var strand = 0; strand < 4; strand++)
        {
            IElement phase = seeds[strand] ? phasePair.Right : phasePair.Left;
            foreach (var visit in OrderedVisits(native, encounters, strand))
            {
                var crossing = encounters[visit.Crossing];
                var step = FiniteLaw.Evaluate(exchange, S(phase), S(phasePair));
                predictions[strand].Add(new Relation(new Relation(crossing.Surface, native[visit.Segment].Section), new Relation(phase, step)));
                offers.Add((visit.Crossing, visit.Segment), Same(phase, phasePair.Right));
                phase = Result(step);
            }
        }
        var crossRows = new List<IElement>();
        for (var i = 0; i < encounters.Count; i++)
        {
            var c = encounters[i]; var first = native[c.First].Section; var second = native[c.Second].Section;
            bool a = offers[(i, c.First)], b = offers[(i, c.Second)];
            Relation order;
            if (a == b || unknownCrossing == i) order = new(c.Surface, StructuralConstraint.Open(first, second));
            else
            {
                if (invertDepth ^ (flipCrossing == i)) a = !a;
                var read = Band.ReadCrossing(frame, c.Surface);
                order = Band.Over(read, a ? first : second, a ? second : first);
            }
            var depthAxis = new Relation(new Placeholder(), new Placeholder());
            var readOrder = Band.ReadOrder(frame, order);
            var samples = readOrder.Complete ? S(new Relation(readOrder.Front!, depthAxis.Right), new Relation(readOrder.Back!, depthAxis.Left)) : StructuralConstraint.Open(first, second);
            crossRows.Add(new Relation(c.Surface, new Relation(order, new Relation(depthAxis, samples))));
        }
        var roots = strandSections.Select((sections, strand) => (IElement)S(S(sections.Cast<IElement>().ToArray()),
            S(sections.Where(section => native.FindIndex(s => Same(s.Section, section)) != missingSegment).Cast<IElement>().ToArray()),
            seeds[strand] ? phasePair.Right : phasePair.Left, S(predictions[strand].ToArray()))).ToArray();
        return new(frame, S(frame.Surface, frame.Word(28, 0), controller, S(roots), S(crossRows.ToArray()), S(generations.ToArray())));
    }

    /// <summary>All semantic reads below come from the connected root. The
    /// fixture wrapper supplies only its explicit frame reader.</summary>
    public static Case Read(Fixture fixture, string id = "baseline", string name = "Angular interlace", string note = "")
    {
        var frame = fixture.Frame; var fields = E(fixture.Surface);
        if (fields.Length != 6 || !Same(fields[0], frame.Surface) || fields[2] is not Relation { Left: Relation phasePair })
            throw new ArgumentException("Expected the bounded angular-weave root grammar.", nameof(fixture));
        var rawStrands = E((Relation)fields[3]); var rawCrosses = E((Relation)fields[4]);
        var native = new List<NativeSection>(); var material = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        var strandFields = rawStrands.Select(s => E((Relation)s)).ToArray();
        for (var strand = 0; strand < rawStrands.Length; strand++)
        {
            var parts = strandFields[strand];
            if (parts.Length != 4) throw new ArgumentException("A route scope is incomplete.");
            var slot = 0;
            foreach (var section in E((Relation)parts[0]).Cast<Relation>()) native.Add(new(strand, slot++, section, frame.Read(section)));
            foreach (var section in E((Relation)parts[1]).Cast<Relation>())
            {
                if (!native.Any(s => s.Strand == strand && Same(s.Section, section))) throw new ArgumentException("Material must refer to this route's sections.");
                material.Add(section);
            }
        }
        var sites = new List<IElement>();
        int Site(IElement site) { var i = sites.FindIndex(s => Same(s, site)); if (i >= 0) return i; sites.Add(site); return sites.Count - 1; }
        var segments = native.Select((s, i) => new Segment(i, s.Strand, s.Slot, Point(s.Read.Controls[0]), Point(s.Read.Controls[^1]),
            material.Contains(s.Section), Site(s.Read.Start), Site(s.Read.End))).ToArray();
        int Section(IElement section) => native.FindIndex(s => Same(s.Section, section));
        var crossings = new List<Crossing>(); var nativeCrosses = new List<Encounter>();
        foreach (var item in rawCrosses)
        {
            if (item is not Relation { Left: Relation cross, Right: Relation { Left: Relation order,
                Right: Relation { Left: Relation axis, Right: Relation samples } } }) throw new ArgumentException("Incomplete crossing evidence.");
            var c = Band.ReadCrossing(frame, cross); var o = Band.ReadOrder(frame, order);
            var first = Section(c.First); var second = Section(c.Second);
            if (first < 0 || second < 0 || !frame.Vector(c.Locus, out var at)) throw new ArgumentException("Crossing references foreign sections.");
            var orderKnown = c.Crosses == true && o.Complete && Same(order.Left, cross) &&
                (Same(o.Front!, c.First) && Same(o.Back!, c.Second) || Same(o.Front!, c.Second) && Same(o.Back!, c.First));
            int? front = orderKnown ? Section(o.Front!) : null, back = orderKnown ? Section(o.Back!) : null;
            int? Depth(Relation section)
            {
                if (Same(axis.Left, axis.Right) || !StructuralConstraint.TryReadScope(samples, out var rows, out var seam, out var tail) || !seam.Equals(tail)) return null;
                var matches = rows.OfType<Relation>().Where(r => Same(r.Left, section)).ToArray();
                return matches.Length == 1 ? Same(matches[0].Right, axis.Right) ? 1 : Same(matches[0].Right, axis.Left) ? -1 : null : null;
            }
            var az = Depth(c.First); var bz = Depth(c.Second);
            var isMaterial = material.Contains(c.First) && material.Contains(c.Second);
            crossings.Add(new(crossings.Count, Point(at), first, second, front, back, az, bz, c.Crosses == true, isMaterial,
                orderKnown, orderKnown && az.HasValue && bz.HasValue && (front == first ? az > bz : bz > az)));
            nativeCrosses.Add(new(first, second, Point(at), cross));
        }
        var strands = new List<Strand>();
        for (var strand = 0; strand < rawStrands.Length; strand++)
        {
            var route = native.Where(s => s.Strand == strand).ToArray();
            var selected = route.Where(s => material.Contains(s.Section)).ToArray();
            var predictions = E((Relation)strandFields[strand][3]);
            var visits = OrderedVisits(native, nativeCrosses, strand).Select(v =>
            {
                var c = crossings[v.Crossing];
                var prediction = predictions.OfType<Relation>().FirstOrDefault(p => p.Left is Relation pair && Same(pair.Left, nativeCrosses[v.Crossing].Surface) && Same(pair.Right, native[v.Segment].Section));
                bool? expected = prediction?.Right is Relation phase ? Same(phase.Left, phasePair.Right) ? true : Same(phase.Left, phasePair.Left) ? false : null : null;
                bool? over = c.Material && c.OrderKnown ? c.FrontSegment == v.Segment : null;
                int? z = c.Material ? c.FirstSegment == v.Segment ? c.FirstZ : c.SecondZ : null;
                return new Visit(c.Id, v.Segment, over, z, expected);
            }).ToArray();
            var pairs = visits.Zip(visits.Skip(1)).Where(p => p.First.Over.HasValue && p.Second.Over.HasValue).ToArray();
            bool Joined(NativeSection[] path) => path.Length > 0 && path.All(s => s.Read.Complete) && path.Zip(path.Skip(1)).All(p =>
                Same(p.First.Read.End, p.Second.Read.Start) && p.First.Read.Controls[^1] == p.Second.Read.Controls[0]);
            bool Closed(NativeSection[] path) => Joined(path) && Same(path[0].Read.Start, path[^1].Read.End) && path[0].Read.Controls[0] == path[^1].Read.Controls[^1];
            strands.Add(new(strand, Joined(selected), Closed(selected), Joined(route), Closed(route), visits,
                pairs.Count(p => p.First.Over != p.Second.Over), pairs.Length,
                visits.Count(v => v.Over.HasValue && v.ExpectedOver.HasValue && v.Over == v.ExpectedOver), visits.Count(v => v.Over.HasValue && v.ExpectedOver.HasValue)));
        }
        var parent = Enumerable.Range(0, sites.Count).ToArray(); var degree = new int[sites.Count];
        int Root(int i) { while (parent[i] != i) i = parent[i]; return i; }
        foreach (var s in segments.Where(s => s.Present)) { parent[Root(s.ToSite)] = Root(s.FromSite); degree[s.FromSite]++; degree[s.ToSite]++; }
        int contacts = 0;
        for (var i = 0; i < segments.Length; i++) for (var j = i + 1; j < segments.Length; j++)
        {
            var a = segments[i]; var b = segments[j]; if (!a.Present || !b.Present) continue;
            if (a.FromSite != b.FromSite && a.From == b.From || a.FromSite != b.ToSite && a.From == b.To ||
                a.ToSite != b.FromSite && a.To == b.From || a.ToSite != b.ToSite && a.To == b.To) contacts++;
        }
        var summary = new Summary(strands.Count, Enumerable.Range(0, sites.Count).Where(i => degree[i] > 0).Select(Root).Distinct().Count(),
            degree.Count(d => d == 1), crossings.Count, crossings.Count(c => c.Material && c.OrderKnown), crossings.Count(c => c.Material && c.DepthAgrees),
            strands.Sum(s => s.Alternations), strands.Sum(s => s.AdjacentVisits), strands.Count(s => s.Closed), segments.Count(s => s.Present), segments.Length, contacts);
        Check[] checks = [
            new("continuity", "Each material strand keeps its ordered handoffs", strands.Count(s => s.Continuous), strands.Count),
            new("crossing-order", "Every material crossing has a known local order", summary.KnownOrders, crossings.Count(c => c.Material)),
            new("alternation", "Consecutive readable encounters alternate over and under", summary.Alternations, summary.AdjacentVisits,
                crossings.Any(c => !c.Material || !c.OrderKnown) ? "Pairs touching an absent or unknown encounter are unscored; no pair is bridged across it." : null),
            new("depth", "The local depth samples agree with the crossing order", summary.DepthAgreements, crossings.Count(c => c.Material)),
            new("closure", "Actual strand endpoints share identity", summary.ClosedStrands, strands.Count, "A repeated bay or closed-looking window does not establish strand closure.")
        ];
        return new(id, name, note, segments, crossings.ToArray(), strands.ToArray(), summary, checks);
    }

    public static object Report()
    {
        var baseline = Build();
        Case[] cases = [
            Read(baseline, "baseline", "Four continuous strands", "A declared centerline idealization of the supplied angular interlace. Three bays retain four separate continuous strands and local alternating crossing orders."),
            Read(Build(flipCrossing: 3), "flip-crossing", "Reverse one crossing", "One local order is reversed. The same route and projected centerline remain, while the encounter rhythm changes."),
            Read(Build(invertDepth: true), "depth-inversion", "Exchange front and back everywhere", "Every local order is inverted. Alternation and the planar route remain; individual depth readings reverse."),
            Read(Build(mirror: true), "mirror", "Reflect the planar frame", "The horizontal-axis reflection changes the planar route while preserving each strand's local over/under order."),
            Read(Build(missingSegment: 14), "missing-section", "Remove a section between crossings", "The construction route remains available, but the material selection omits one inner horizontal section. Its unchanged crossing rhythm cannot establish material continuity."),
            Read(Build(unknownCrossing: 3), "unknown-order", "Leave one order open", "The crossing and both strands remain. Its local front/back and depth are unresolved; neighbouring readable encounters do not bridge this unknown visit."),
            Read(Build(shiftInner: 2), "phase-shift", "Shift one inner course", "One inner route is displaced by two shared-reach participations. New encounter positions and phase compatibility are read again. Conflicting offers leave crossing order open."),
            Read(Build(missingSegment: 15), "missing-crossing-section", "Remove a section through crossings", "The omitted inner section passed through two projected crossings. Its route is retained, while its material and the affected material encounters are absent. The observer leaves those visits unreadable.")
        ];
        return new
        {
            Title = "Following four strands through an angular interlace",
            Scope = "An authored common-reach construction and bounded observers. Shared words, direction substitutions, ordered sections and local crossing relations carry the construction. No photograph parser, learned generator, physical ribbon solver, automatic hidden-path completion or general knot classifier is claimed.",
            BayPitch = 28, Bays = 3, LocalDepth = "Each encounter owns a separate two-ended depth axis. The display reads its ordered endpoints as -1 and +1. Height, clearance and interpolation between encounters remain unspecified.",
            Cases = cases,
            PeriodReads = new[] { PeriodRead(cases[0], 1), PeriodRead(cases[0], 2) },
            Comparisons = new[]
            {
                Compare(cases[0], cases[1], "Same centerlines, one changed crossing", false, false),
                Compare(cases[0], cases[2], "Same centerlines, inverted depth", false, true),
                Compare(cases[0], cases[3], "Reflected geometry with retained depth", true, false),
                Compare(cases[0], cases[4], "Same route, different material", false, false),
                Compare(cases[0], cases[5], "Same route, one unresolved order", false, false),
                Compare(cases[0], cases[6], "Shifted inner route, retained alternation", false, false),
                Compare(cases[0], cases[7], "Same route, two absent material encounters", false, false)
            },
            Encoding = new[]
            {
                "Four ordered section scopes retain independent endpoint identities.",
                "A two-phase exchange controller selects identity or reflected common-reach words for each bay.",
                "Substitution and additive placement receipts retain each generated section and the reusable local word it consumed.",
                "Each crossing references two actual sections and a projected position word; it creates no shared endpoint.",
                "A local order references the actual front and back section, followed by a local two-ended depth relation.",
                "Material selection refers to actual sections. Occlusion leaves this selection intact.",
                "Encounter predictions retain their strand, crossing, phase and finite-law exchange result. Observed order may disagree or stay open."
            }
        };
    }

    public sealed record PeriodComparison(int Bays, int Translation, int[] StrandMap, int SegmentsMatched, int SegmentsChecked,
        int CrossingsMatched, int CrossingsChecked, int OrdersMatched, bool ClosesStrands, string Limit);

    /// <summary>Checks actual section and crossing correspondence inside the
    /// captured finite run. A translation correspondence never welds endpoints.</summary>
    public static PeriodComparison PeriodRead(Case source, int bays)
    {
        if (bays is < 1 or > 2) throw new ArgumentOutOfRangeException(nameof(bays));
        int dx = 28 * bays;
        var map = Enumerable.Range(0, 4).Select(s => bays % 2 == 0 ? s : s ^ 1).ToArray();
        var pairs = new Dictionary<int, int>(); var checkedSegments = 0; var matchedSegments = 0;
        foreach (var first in source.Segments)
        {
            var slot = first.Slot + bays * (first.Strand < 2 ? 2 : 5);
            var second = source.Segments.FirstOrDefault(s => s.Strand == map[first.Strand] && s.Slot == slot);
            if (second is null) continue;
            checkedSegments++;
            if (new Pt(first.From.X + dx, first.From.Y) != second.From || new Pt(first.To.X + dx, first.To.Y) != second.To) continue;
            matchedSegments++; pairs.Add(first.Id, second.Id);
        }
        var checkedCrosses = 0; var matchedCrosses = 0; var matchedOrders = 0;
        foreach (var c in source.Crossings)
        {
            if (!pairs.TryGetValue(c.FirstSegment, out var first) || !pairs.TryGetValue(c.SecondSegment, out var second)) continue;
            checkedCrosses++;
            var target = source.Crossings.FirstOrDefault(t => t.At == new Pt(c.At.X + dx, c.At.Y) &&
                (t.FirstSegment == first && t.SecondSegment == second || t.FirstSegment == second && t.SecondSegment == first));
            if (target is null) continue;
            matchedCrosses++;
            if (c.FrontSegment is { } front && target.FrontSegment == pairs[front]) matchedOrders++;
        }
        return new(bays, dx, map, matchedSegments, checkedSegments, matchedCrosses, checkedCrosses, matchedOrders,
            source.Strands.Any(s => s.Closed), "Only correspondences whose source and translated destination both occur in this finite capture are checked. No endpoint is identified by this observation.");
    }

    private static object Compare(Case source, Case target, string label, bool reflected, bool depthInverted)
    {
        Pt Map(Pt p) => reflected ? new(p.X, 12 - p.Y) : p;
        bool geometry = source.Segments.Length == target.Segments.Length && source.Segments.Zip(target.Segments).All(p => Map(p.First.From) == p.Second.From && Map(p.First.To) == p.Second.To);
        var matched = source.Crossings.Select(c => (Source: c, Target: target.Crossings.FirstOrDefault(t => t.FirstSegment == c.FirstSegment && t.SecondSegment == c.SecondSegment && t.At == Map(c.At)))).ToArray();
        int orders = matched.Count(p => p.Target is { } t && p.Source.OrderKnown && t.OrderKnown && (depthInverted ? p.Source.FrontSegment == t.BackSegment : p.Source.FrontSegment == t.FrontSegment));
        return new { Source = source.Id, Target = target.Id, Label = label, GeometryMatches = geometry,
            TopologyMatches = source.Segments.Zip(target.Segments).All(p => p.First.FromSite == p.Second.FromSite && p.First.ToSite == p.Second.ToSite),
            MaterialMatches = source.Segments.Zip(target.Segments).All(p => p.First.Present == p.Second.Present),
            OrdersMatch = orders, OrdersChecked = source.Crossings.Length, DepthInverted = depthInverted, PlanarReflection = reflected };
    }

    private static VisitSite[] OrderedVisits(IReadOnlyList<NativeSection> native, IReadOnlyList<Encounter> encounters, int strand) =>
        encounters.SelectMany((c, i) => new[] { c.First, c.Second }.Where(s => native[s].Strand == strand).Select(s =>
        {
            var read = native[s].Read; var direction = read.Controls[^1] - read.Controls[0];
            return new VisitSite(i, s, Curve.Vector.Dot(new Curve.Vector(c.At.X, c.At.Y) - read.Controls[0], direction));
        })).OrderBy(v => native[v.Segment].Slot).ThenBy(v => v.Progress).ToArray();

    private static bool Intersect(Curve.Vector p, Curve.Vector q, Curve.Vector r, Curve.Vector s, out Pt at)
    {
        at = default; var a = q - p; var b = s - r; var den = Curve.Vector.Cross(a, b);
        if (den.IsZero) return false;
        var t = Curve.Vector.Cross(r - p, b); var u = Curve.Vector.Cross(r - p, a);
        if (den.Sign < 0) { den = -den; t = -t; u = -u; }
        if (t <= 0 || t >= den || u <= 0 || u >= den) return false;
        var xn = p.X * den + a.X * t; var yn = p.Y * den + a.Y * t;
        if (xn % den != 0 || yn % den != 0) throw new InvalidOperationException("This bounded fixture needs an explicitly refined crossing word.");
        at = new((int)(xn / den), (int)(yn / den)); return true;
    }
    private static IElement Result(Relation read)
    {
        if (!FiniteLaw.TryReadResult(read, out var result, out var open, out _) || E(result).Length != 1 || E(open).Length != 0)
            throw new InvalidOperationException("The authored phase has no unique exchange result.");
        return E(result)[0];
    }
    private static Pt Point(Curve.Vector p) => new((int)p.X, (int)p.Y);
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope)
    {
        if (!StructuralConstraint.TryReadScope(scope, out var entries, out var seam, out var tail) || !seam.Equals(tail)) throw new ArgumentException("Expected a finite closed scope.");
        return entries;
    }
}
