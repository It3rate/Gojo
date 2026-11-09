using System.Text.Json.Serialization;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>
/// A chronological construction read, distinct from the final joint geometry
/// and from a later traffic law. Earlier strokes expose landmarks; later
/// strokes can return to them, leave a side branch, or arrive and stop there.
/// No glyph name, screen hit or future stroke supplies an earlier landmark.
/// </summary>
public static class GlyphConstructionHistory
{
    // placement = ((target stroke -> earlier source stroke) ->
    //              (earlier landmark site -> target start site)) -> offset word
    // read input = glyph surface -> Closed(placement rows)
    // read = input -> (Closed(chronological rows) -> Closed(unresolved))
    // chronological row = (actual stroke -> (start site -> end site)) ->
    //   (Closed(earlier strokes) -> (Closed(earlier sections) ->
    //    (Closed(event rows) -> (Closed(accepted placements) -> unresolved))))
    // event = Closed(site, earlier incident sections, earlier strokes at site,
    //                current incoming visits, current outgoing visits,
    //                already completed visits within this stroke)
    // The event's actual visit references retain direction and section identity.
    // Words such as "branch" or "stop" below are derived display summaries.
    public sealed record EventRead(int Site, string Kind, string Description,
        int BeforeDegree, int[] SourceStrokes);
    public sealed record StrokeRead(int Index, int StartSite, int EndSite,
        string StartKind, string EndKind, string StartDescription, string EndDescription,
        int[] StartSourceStrokes, int[] EndSourceStrokes, int? PlacementSourceStroke, int? PlacementSourceSite,
        long[] PlacementOffset, bool? ParallelToSource, EventRead[] Events);
    public sealed record SiteRead(int Site, string[] Descriptions, string[] Kinds);
    public sealed record Report(StrokeRead[] Strokes, SiteRead[] Sites, bool Complete)
    {
        [JsonIgnore] public Relation Surface { get; init; } = null!;
    }

    public static Relation Placement(Relation targetStroke, Relation sourceStroke,
        IElement sourceLandmark, IElement targetStart, Relation offsetWord) =>
        new(new Relation(new Relation(targetStroke, sourceStroke), new Relation(sourceLandmark, targetStart)), offsetWord);

    private sealed record Visit(Relation Native, Relation Section, IElement Start, IElement End,
        Curve.Vector StartVector, Curve.Vector EndVector, Curve.Vector StartPoint, Curve.Vector EndPoint, bool Straight);

    public static Report Read(Relation glyphSurface, Relation? placements = null)
    {
        placements ??= S();
        var input = new Relation(glyphSurface, placements);
        var pending = new List<IElement>(); var rows = new List<IElement>();
        var strokeReads = new List<StrokeRead>();
        var allEvents = new List<EventRead>();
        Report Finish() => new(strokeReads.ToArray(), allEvents.GroupBy(e => e.Site)
            .Select(g => new SiteRead(g.Key, g.Select(e => e.Description).Distinct().ToArray(),
                g.Select(e => e.Kind).Distinct().ToArray())).ToArray(), pending.Count == 0)
        { Surface = new(input, new Relation(S(rows.ToArray()), S(pending.ToArray()))) };

        if (glyphSurface is not { Left: Relation { Left: Relation frameSurface, Right: Relation sections }, Right: Relation history } ||
            !Curve.Frame.TryFromSurface(frameSurface, out var frame) || !Closed(sections) || !Closed(history) || !Closed(placements))
        { pending.Add(input); return Finish(); }
        var sectionEntries = E(sections); var historyEntries = E(history);
        if (sectionEntries.Any(s => s is not Relation) || historyEntries.Any(s => s is not Relation) ||
            sectionEntries.Distinct(N.Participants.Instance).Count() != sectionEntries.Length)
        { pending.Add(glyphSurface); return Finish(); }
        var actualSections = sectionEntries.Cast<Relation>().ToArray();
        var actualStrokes = historyEntries.Cast<Relation>().ToArray();
        var siteIndices = new Dictionary<IElement, int>(N.Participants.Instance);
        var positions = new Dictionary<IElement, Curve.Vector>(N.Participants.Instance);
        var edgeEnds = new Dictionary<Relation, (IElement Start, IElement End)>(ReferenceEqualityComparer.Instance);
        foreach (var section in actualSections)
        {
            var read = frame.Read(section);
            if (!read.Complete) { pending.Add(section); continue; }
            edgeEnds.Add(section, (read.Start, read.End));
            Site(read.Start, read.Controls[0]); Site(read.End, read.Controls[^1]);
        }
        void Site(IElement site, Curve.Vector position)
        {
            if (!siteIndices.ContainsKey(site)) siteIndices.Add(site, siteIndices.Count);
            if (positions.TryGetValue(site, out var prior) && prior != position) pending.Add(new Relation(site, sections));
            else positions.TryAdd(site, position);
        }
        if (pending.Count != 0) return Finish();

        var previousSections = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        var previousStrokes = new List<Relation>();
        var visitsByStroke = new Dictionary<Relation, Visit[]>(ReferenceEqualityComparer.Instance);
        var touchedByStroke = new Dictionary<IElement, List<Relation>>(N.Participants.Instance);
        var consumedPlacements = new HashSet<IElement>(N.Participants.Instance);
        for (var strokeIndex = 0; strokeIndex < actualStrokes.Length; strokeIndex++)
        {
            var stroke = actualStrokes[strokeIndex]; var visits = new List<Visit>();
            // Reusing sections is supported. Reusing one stroke holder as two
            // timed occurrences needs a separate occurrence correspondence;
            // otherwise a placement cannot say which occurrence it targets.
            if (previousStrokes.Any(s => N.Same(s, stroke))) { pending.Add(stroke); break; }
            if (!Closed(stroke)) { pending.Add(stroke); break; }
            foreach (var entry in E(stroke))
            {
                if (entry is not Relation { Left: Relation section,
                        Right: Relation { Left: Relation ends, Right: Relation wordOrder } } visit ||
                    !edgeEnds.ContainsKey(section) || !Closed(wordOrder))
                { pending.Add(entry); break; }
                var read = frame.Read(section); var controlWords = E(read.ControlWords); var ordered = E(wordOrder);
                var forward = N.Same(ends.Left, read.Start) && N.Same(ends.Right, read.End) &&
                    controlWords.SequenceEqual(ordered, N.Participants.Instance);
                var reverse = N.Same(ends.Left, read.End) && N.Same(ends.Right, read.Start) &&
                    controlWords.Reverse().SequenceEqual(ordered, N.Participants.Instance);
                if ((!forward && !reverse) || visits.Count > 0 && !N.Same(visits[^1].End, ends.Left))
                { pending.Add(visit); break; }
                visits.Add(new(visit, section, ends.Left, ends.Right,
                    forward ? read.StartVector : -read.EndVector,
                    forward ? read.EndVector : -read.StartVector,
                    forward ? read.Controls[0] : read.Controls[^1],
                    forward ? read.Controls[^1] : read.Controls[0], read.Straight == true));
            }
            if (pending.Count != 0 || visits.Count == 0) { if (visits.Count == 0) pending.Add(stroke); break; }
            var first = visits[0]; var last = visits[^1];
            var start = first.Start; var end = last.End;
            var observedSections = new HashSet<Relation>(previousSections, ReferenceEqualityComparer.Instance);
            var prefix = new List<Visit>();
            var startDegree = Degree(start); var endPriorDegree = Degree(end);
            var startSources = Sources(start);
            var startKind = startDegree switch { 0 => "new-start", 1 => "return-endpoint", 2 => "return-interior", _ => "return-junction" };
            var startDescription = startDegree == 0 ? "Start at a new site; no earlier landmark is supplied." :
                $"Lift and return to an existing {Role(startDegree)} from {SourceNames(startSources)}.";
            int? placementSource = null; int? placementSourceSite = null; long[] placementOffset = []; bool? parallel = null;
            var acceptedPlans = new List<IElement>(); var localPending = new List<IElement>();
            var selectedPlans = E(placements).OfType<Relation>().Where(p =>
                p.Left is Relation { Left: Relation role } && N.Same(role.Left, stroke)).ToArray();
            if (selectedPlans.Length > 1) localPending.Add(S(selectedPlans));
            foreach (var plan in selectedPlans)
            {
                consumedPlacements.Add(plan);
                if (plan is not { Left: Relation { Left: Relation pairing, Right: Relation starts }, Right: Relation offsetWord } ||
                    pairing.Right is not Relation earlier || !previousStrokes.Any(s => N.Same(s, earlier)) ||
                    !visitsByStroke.TryGetValue(earlier, out var sourceVisits) ||
                    !sourceVisits.Any(v => N.Same(starts.Left, v.Start) || N.Same(starts.Left, v.End)) || !N.Same(starts.Right, start) ||
                    !frame.Vector(offsetWord, out var offset) || positions[starts.Left] + offset != first.StartPoint)
                { localPending.Add(plan); continue; }
                acceptedPlans.Add(plan); placementSource = Array.FindIndex(actualStrokes, s => N.Same(s, earlier));
                placementSourceSite = siteIndices[starts.Left];
                placementOffset = [checked((long)offset.X), checked((long)offset.Y)];
                parallel = Parallel(sourceVisits, visits.ToArray());
                if (startDegree == 0)
                {
                    startKind = "offset-start";
                    startDescription = $"Locate a new start by the supplied offset from landmark p{placementSourceSite} on stroke {placementSource + 1}" +
                        (parallel == true ? "; the new stroke runs parallel to it." : ".");
                }
            }
            if (localPending.Count > 0)
            {
                pending.AddRange(localPending); startKind = "unresolved-placement";
                startDescription = "The supplied landmark placement does not validate against earlier evidence.";
            }
            var events = new List<EventRead>(); var nativeEvents = new List<IElement>();
            var startRetrace = previousSections.Contains(first.Section);
            AddEvent(start, startRetrace ? "retrace-start" : startDegree switch
                { 0 => startKind, 1 => "endpoint-attachment", 2 => "outward-branch", _ => "junction-departure" },
                startRetrace ? "Return and retrace an existing section; this is not a new attachment." : startDegree switch
                {
                    0 => startDescription,
                    1 => "Construct an attachment by leaving an earlier endpoint after a pen lift.",
                    2 => "Construct a secondary route outward from an earlier interior landmark.",
                    _ => "Leave an existing multi-arm junction along the selected new section."
                }, [], [first.Native]);

            for (var i = 0; i < visits.Count - 1; i++)
            {
                var incoming = visits[i]; var outgoing = visits[i + 1]; var site = incoming.End;
                var beforeDegree = Degree(site);
                var kind = "continuous-passage";
                var description = "Continue through this site without lifting the pen.";
                if (beforeDegree > 0 && !observedSections.Contains(incoming.Section))
                {
                    var fromEarlierStroke = previousSections.Any(s => N.Same(edgeEnds[s].Start, site) || N.Same(edgeEnds[s].End, site));
                    kind = fromEarlierStroke ? "meet-landmark-and-continue" : "revisit-and-continue";
                    description = fromEarlierStroke ? $"Reach an earlier {Role(beforeDegree)}, then continue this stroke from it." :
                        "Revisit a landmark reached earlier in this same stroke, then continue.";
                }
                else if (incoming.EndVector.Zero || outgoing.StartVector.Zero)
                { kind = "unresolved-continuation"; description = "The continuous visit is known, but its tangent read is unresolved."; }
                else if (!Curve.Vector.Cross(incoming.EndVector, outgoing.StartVector).IsZero ||
                    Curve.Vector.Dot(incoming.EndVector, outgoing.StartVector).Sign <= 0)
                { kind = "continuous-turn"; description = "Change direction here during one continuous pen stroke."; }
                AddEvent(site, kind, description, [incoming.Native], [outgoing.Native]);
                // The incoming edge becomes earlier evidence only after its
                // arrival read. Otherwise every new corner would appear to be
                // a return to a pre-existing landmark.
                observedSections.Add(incoming.Section); prefix.Add(incoming);
            }
            var endDegree = Degree(end); var endSources = Sources(end);
            var endKind = N.Same(start, end) ? "closed-return" : endPriorDegree == 0 && endDegree > 0 ? "stop-at-earlier-landmark" : endDegree switch
                { 0 => "free-stop", 1 => "stop-at-endpoint", 2 => "stop-at-interior", _ => "stop-at-junction" };
            var endDescription = endKind == "closed-return" ? "Return to this stroke's actual starting site and stop." :
                endKind == "stop-at-earlier-landmark" ? "Stop at a landmark already reached earlier in this same stroke." :
                endDegree == 0 ? "Stop at a newly reached endpoint." : $"Arrive at the existing {Role(endDegree)} from {SourceNames(endSources)} and stop.";
            var finalKind = endKind == "closed-return" ? "closure-stop" : endKind == "stop-at-earlier-landmark" ? "earlier-landmark-stop" :
                endDegree == 0 ? "free-stop" : "incoming-stop";
            AddEvent(end, finalKind, endDescription, [last.Native], []);
            if (startDegree > 0 && endPriorDegree > 0 && !N.Same(start, end) && visits.Any(v => !previousSections.Contains(v.Section)))
            {
                endDescription += " This stroke joins two landmarks that already existed before it began.";
                AddEvent(end, "landmark-to-landmark-join", "Connect two previously available landmarks with this new stroke.", [last.Native], []);
            }
            var nativeRow = new Relation(new Relation(stroke, new Relation(start, end)),
                new Relation(S(previousStrokes.ToArray()), new Relation(S(previousSections.Cast<IElement>().ToArray()),
                    new Relation(S(nativeEvents.ToArray()), new Relation(S(acceptedPlans.ToArray()), S(localPending.ToArray()))))));
            rows.Add(nativeRow); allEvents.AddRange(events);
            strokeReads.Add(new(strokeIndex, siteIndices[start], siteIndices[end], startKind, endKind,
                startDescription, endDescription, startSources, endSources, placementSource, placementSourceSite, placementOffset, parallel, events.ToArray()));
            visitsByStroke.Add(stroke, visits.ToArray()); previousStrokes.Add(stroke);
            foreach (var visit in visits)
            {
                previousSections.Add(visit.Section);
                foreach (var site in new[] { visit.Start, visit.End })
                {
                    if (!touchedByStroke.TryGetValue(site, out var owners)) touchedByStroke.Add(site, owners = []);
                    if (!owners.Any(s => N.Same(s, stroke))) owners.Add(stroke);
                }
            }

            int Degree(IElement site) => observedSections.Sum(s =>
                (N.Same(edgeEnds[s].Start, site) ? 1 : 0) + (N.Same(edgeEnds[s].End, site) ? 1 : 0));
            int[] Sources(IElement site)
            {
                var sources = touchedByStroke.TryGetValue(site, out var owners) ?
                    owners.Select(owner => Array.FindIndex(actualStrokes, s => N.Same(s, owner))).ToList() : [];
                if (prefix.Any(v => N.Same(v.Start, site) || N.Same(v.End, site))) sources.Add(strokeIndex);
                return sources.ToArray();
            }
            void AddEvent(IElement site, string kind, string description, IElement[] incoming, IElement[] outgoing)
            {
                var incident = observedSections.Where(s => N.Same(edgeEnds[s].Start, site) || N.Same(edgeEnds[s].End, site)).ToArray();
                var sourceIndices = Sources(site);
                nativeEvents.Add(S(site, S(incident), S(sourceIndices.Select(i => (IElement)actualStrokes[i]).ToArray()), S(incoming), S(outgoing),
                    S(prefix.Select(v => (IElement)v.Native).ToArray())));
                events.Add(new(siteIndices[site], kind, description, Degree(site), sourceIndices));
            }
        }
        foreach (var unvisited in actualSections.Where(s => !previousSections.Contains(s))) pending.Add(unvisited);
        foreach (var unused in E(placements).Where(p => !consumedPlacements.Contains(p))) pending.Add(unused);
        return Finish();
    }

    private static bool? Parallel(Visit[] source, Visit[] target)
    {
        if (source.Any(v => !v.Straight) || target.Any(v => !v.Straight)) return null;
        var a = source.Select(v => v.EndPoint - v.StartPoint).Where(v => !v.Zero).ToArray();
        var b = target.Select(v => v.EndPoint - v.StartPoint).Where(v => !v.Zero).ToArray();
        if (a.Length == 0 || b.Length == 0) return null;
        if (a.Any(v => !Curve.Vector.Cross(v, a[0]).IsZero) || b.Any(v => !Curve.Vector.Cross(v, b[0]).IsZero)) return null;
        return Curve.Vector.Cross(a[0], b[0]).IsZero;
    }
    private static string Role(int degree) => degree == 1 ? "endpoint" : degree == 2 ? "interior landmark" : "junction";
    private static string SourceNames(int[] sources) => sources.Length == 1 ? $"stroke {sources[0] + 1}" :
        "strokes " + string.Join(", ", sources.Select(i => i + 1));
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}
