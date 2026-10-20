using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;
using C = Gojo.Experiments.QualitativeFrieze.OccurrenceComposition;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>An explicitly selected, certified occurrence assignment retained
/// for another read of the same observation event. It is observational memory,
/// not a lifetime rule, an implicit first match or a correspondence across events.</summary>
public sealed class RetainedMatch
{
    // memory = (actual assessment -> actual supported witness) ->
    //   (Closed(selected actual role slots) ->
    //     (actual observation event -> Closed(pins)))
    // pin = slot -> (source-free pattern ->
    //     ((Closed(actual sites) -> Closed(actual segment rows)) ->
    //       Closed(pattern site -> actual site)))
    // Pins are checked against the original native correspondence and actual
    // observation, including all pattern sites, rather than only the selected
    // landmarks. The provenance retains the corresponding actual edge map.
    public Relation Surface { get; }
    public Relation SourceAssessment { get; private set; } = null!;
    public Relation Witness { get; private set; } = null!;
    public Relation Observation { get; private set; } = null!;
    public Relation SelectedRoles { get; private set; } = N.S();
    public Relation Pins { get; private set; } = N.S();
    public Relation Blocked { get; }
    public Relation Open { get; }
    public bool IsValid => N.E(Blocked).Length == 0 && N.E(Open).Length == 0;

    public static RetainedMatch Capture(C.Assessment assessment, C.Witness witness, Relation selectedRoleSlots)
    {
        ArgumentNullException.ThrowIfNull(assessment); ArgumentNullException.ThrowIfNull(witness);
        ArgumentNullException.ThrowIfNull(selectedRoleSlots);
        var basis = new Relation(assessment.Surface, witness.Surface);
        var rows = ReadPins(assessment.Surface, witness.Surface, selectedRoleSlots, out var observation);
        return new(new Relation(basis, new Relation(selectedRoleSlots,
            new Relation(observation ?? N.S(), N.S(rows ?? [])))));
    }

    /// <summary>Reads the exact native memory, including certification of its
    /// selected witness and the preserved full site correspondence.</summary>
    public RetainedMatch(Relation surface)
    {
        Surface = surface;
        var blocked = new List<IElement>(); var pending = new List<IElement>();
        if (surface is not { Left: Relation { Left: Relation assessment, Right: Relation witness },
                Right: Relation { Left: Relation selected, Right: Relation { Left: Relation observation, Right: Relation pins } } })
            blocked.Add(surface);
        else
        {
            SourceAssessment = assessment; Witness = witness; Observation = observation;
            SelectedRoles = selected; Pins = pins;
            var rows = ReadPins(assessment, witness, selected, out var source);
            if (!StructuralConstraint.TryReadScope(selected, out _, out var seam, out var tail)) blocked.Add(selected);
            else if (!seam.Equals(tail)) pending.Add(selected);
            if (rows is null || !ReferenceEquals(source, observation) || !N.Read(pins, out var supplied) ||
                supplied.Length != rows.Length || rows.Where((row, index) => !SamePin(row, supplied[index])).Any())
                blocked.Add(new Relation(surface, selected));
        }
        Blocked = N.S(blocked.ToArray()); Open = N.S(pending.ToArray());
    }

    internal Relation? Pin(C.Role role) => N.E(Pins).OfType<Relation>()
        .SingleOrDefault(row => N.Same(row.Left, role.Slot));

    internal bool Accepts(C.Role role, Relation footprint, Relation siteMap)
    {
        var pin = Pin(role);
        return pin is null || pin.Right is Relation { Left: var pattern, Right: Relation { Left: Relation fixedFootprint,
            Right: Relation fixedMap } } && ReferenceEquals(pattern, role.PatternSurface) &&
            SameFootprint(fixedFootprint, footprint) && SameMap(fixedMap, siteMap);
    }

    private static IElement[]? ReadPins(Relation assessment, Relation witness, Relation selected, out Relation? observation)
    {
        observation = null;
        RetainedMatch? previousMemory = null;
        if (assessment.Left is Relation prior && assessment.Right is Relation { Left: Relation memory, Right: Relation stale } &&
            prior.Left is Relation { Left: Relation { Left: Relation } } && N.Read(stale, out var staleRows) && staleRows.Length == 0 &&
            memory.Left is Relation { Left: Relation, Right: Relation } && memory.Right is Relation { Left: Relation, Right: Relation })
        {
            // This is the one exact continuation envelope emitted by the local
            // assessment. Earlier captures remain provenance inside memory;
            // they are never searched recursively for a convenient certificate.
            previousMemory = new RetainedMatch(memory);
            if (!previousMemory.IsValid) return null;
            assessment = prior;
        }
        if (assessment is not { Left: Relation { Left: Relation { Left: Relation output, Right: Relation questionSurface } },
                Right: Relation result } || output.Left is not Relation { Left: Relation observed } ||
            !StructuralConstraint.TryReadScope(selected, out var slots, out var seam, out var tail) ||
            slots.Length == 0 && seam.Equals(tail) || !N.Distinct(slots)) return null;
        observation = observed;
        if (previousMemory is not null && !ReferenceEquals(previousMemory.Observation, observation)) return null;
        if (observation is not { Left: Relation { Left: Relation observedFrame, Right: Relation },
                Right: Relation { Left: Relation, Right: Relation { Left: Relation, Right: Relation } } }) return null;
        // Exact assessment result: inventory, consumed, remaining, rejected,
        // supported, refuted, open, blocked, correspondence accounting.
        for (var index = 0; index < 4; index++)
        { if (result.Right is not Relation next) return null; result = next; }
        if (result.Left is not Relation supported || !N.Read(supported, out var supports) ||
            !supports.OfType<Relation>().Any(row => ReferenceEquals(row.Left, questionSurface) && ReferenceEquals(row.Right, witness)))
            return null;
        if (result.Right is not Relation { Left: Relation refuted, Right: Relation { Left: Relation open,
                Right: Relation { Left: Relation blocked } } } ||
            !N.Read(refuted, out var refutations) || refutations.Length != 0 ||
            !N.Read(open, out var unresolved) || unresolved.Length != 0 ||
            !N.Read(blocked, out var failures) || failures.Length != 0) return null;
        var question = new C.Question("retained source question", questionSurface);
        if (!question.IsValid || witness is not { Left: Relation bindings,
                Right: Relation { Left: Relation footprints, Right: Relation { Left: Relation correspondences,
                    Right: Relation { Left: Relation checks, Right: Relation selectedSites } } } } ||
            !N.Read(bindings, out var bindingRows) || !N.Read(footprints, out var footprintRows) ||
            !N.Read(correspondences, out var correspondenceRows) || !N.Read(checks, out _) || !N.Read(selectedSites, out _) ||
            bindingRows.Length != question.Roles.Count || footprintRows.Length != question.Roles.Count ||
            correspondenceRows.Length != question.Roles.Count) return null;
        if (slots.Any(slot => !question.Roles.Any(role => N.Same(role.Slot, slot)))) return null;
        var pins = new List<IElement>(); var participation = new List<C.Participation>();
        // A capture preserves a selected subset, but its source certificate is
        // a joint claim about every role. Valid isolated pieces cannot certify
        // false contact, distinction or selected-site constraints between them.
        foreach (var role in question.Roles)
        {
            var slot = role.Slot;
            var binding = Single(bindingRows, slot); var footprint = Single(footprintRows, slot);
            var correspondence = Single(correspondenceRows, slot);
            if (binding?.Right is not Relation occurrence || footprint?.Right is not Relation physical ||
                correspondence?.Right is not Relation proof || !CommunicationShape.TryRead(role.PatternSurface, out var pattern)) return null;
            Relation? explicitActual = null;
            if (proof.Left is Relation candidate && candidate.Left is Relation pair && ReferenceEquals(pair.Left, role.PatternSurface))
            { explicitActual = proof.Right as Relation; proof = candidate; }
            if (proof is not { Left: Relation { Left: var patternSurface, Right: var actualOccurrence },
                    Right: Relation { Left: Relation frames, Right: Relation { Left: Relation directions,
                        Right: Relation { Left: Relation siteMap, Right: Relation edgeMap } } } } ||
                !ReferenceEquals(patternSurface, role.PatternSurface) || !ReferenceEquals(actualOccurrence, occurrence) ||
                occurrence is not { Left: Relation { Left: Relation definition, Right: var eventSurface },
                    Right: Relation { Left: Relation siteBindings, Right: Relation { Left: Relation edgeBindings, Right: Relation route } } } ||
                !ReferenceEquals(eventSurface, observation) || !CommunicationShape.TryRead(new(frames.Right, definition), out var target) ||
                pattern.Sites.Length != target.Sites.Length || pattern.Edges.Length != target.Edges.Length ||
                pattern.Centres.Length != target.Centres.Length || pattern.IsCycle != target.IsCycle ||
                !ReferenceEquals(frames.Left, pattern.Frame) || !ReferenceEquals(target.Frame, observedFrame) ||
                !N.Read(directions, out var directionRows) || directionRows.Length != pattern.Directions.Length ||
                pattern.Directions.Where((direction, i) => !directionRows.OfType<Relation>().Any(row =>
                    ReferenceEquals(row.Left, direction) && ReferenceEquals(row.Right, target.Directions[i]))).Any() ||
                !N.Read(siteMap, out var sites) || !N.Read(edgeMap, out var edges) ||
                !N.Read(siteBindings, out var actualSites) || !N.Read(edgeBindings, out var actualEdges) || !N.Read(route, out var uses) ||
                sites.Length != pattern.Sites.Length || edges.Length != pattern.Edges.Length ||
                actualSites.Length != target.Sites.Length || actualEdges.Length != target.Edges.Length || uses.Length != target.Edges.Length)
                return null;
            var actualMap = new List<IElement>(); var actualEdgeMap = new List<IElement>();
            var targetSiteMap = new List<Relation>(); var targetEdgeMap = new List<Relation>();
            foreach (var site in target.Sites)
            {
                var bindingRow = Single(actualSites, site);
                if (bindingRow is null) return null;
                targetSiteMap.Add(bindingRow);
            }
            for (var index = 0; index < target.Edges.Length; index++)
            {
                var bindingRow = Single(actualEdges, target.Edges[index].Left);
                var from = targetSiteMap.Single(row => N.Same(row.Left, target.Ends[index].Left)).Right;
                var to = targetSiteMap.Single(row => N.Same(row.Left, target.Ends[index].Right)).Right;
                if (bindingRow?.Right is not Relation actualSegment ||
                    uses[index] is not Relation { Left: var actualUse, Right: Relation ends } ||
                    !ReferenceEquals(actualUse, actualSegment) || !N.Same(ends.Left, from) || !N.Same(ends.Right, to)) return null;
                targetEdgeMap.Add(bindingRow);
            }
            if (!Certifies(target, observation, physical, targetSiteMap.ToArray(), targetEdgeMap.ToArray())) return null;
            foreach (var site in pattern.Sites)
            {
                var mapped = Single(sites, site); var actual = mapped is null ? null : Single(actualSites, mapped.Right);
                if (actual is null) return null;
                actualMap.Add(new Relation(site, actual.Right));
            }
            foreach (var edge in pattern.Edges)
            {
                var mapped = Single(edges, edge.Left); var actual = mapped is null ? null : Single(actualEdges, mapped.Right);
                if (actual is null) return null;
                actualEdgeMap.Add(new Relation(edge.Left, actual.Right));
            }
            var map = N.S(actualMap.ToArray());
            if (explicitActual is not null && !SameMap(map, explicitActual) ||
                !Certifies(pattern, observation, physical, actualMap.Cast<Relation>().ToArray(), actualEdgeMap.Cast<Relation>().ToArray())) return null;
            if (previousMemory is not null && !previousMemory.Accepts(role, physical, map)) return null;
            pins.Add(new Relation(slot, new Relation(role.PatternSurface, new Relation(physical, map))));
            participation.Add(new(physical, map));
        }
        if (previousMemory is not null && N.E(previousMemory.Pins).Cast<Relation>().Any(pin =>
            !question.Roles.Any(role => N.Same(role.Slot, pin.Left)))) return null;
        if (!C.CheckAssignments(question, participation, out _, out _)) return null;
        return slots.Select(slot => pins.Single(row => row is Relation pin && N.Same(pin.Left, slot))).ToArray();
    }

    private static bool Certifies(CommunicationShape pattern, Relation observation, Relation footprint,
        Relation[] sites, Relation[] edges)
    {
        if (observation is not { Left: Relation { Right: Relation observedSites }, Right: Relation { Left: Relation observedEdges } } ||
            !N.Read(observedSites, out var siteRows) || !N.Read(observedEdges, out var edgeRows) ||
            footprint is not { Left: Relation physicalSites, Right: Relation physicalEdges } ||
            !N.Read(physicalSites, out var siteFootprint) || !N.Read(physicalEdges, out var edgeFootprint) ||
            !N.Distinct(sites.Select(row => row.Right).ToArray()) || !N.Distinct(edges.Select(row => row.Right).ToArray()) ||
            !SameSet(siteFootprint, sites.Select(row => row.Right).ToArray()) ||
            !SameSet(edgeFootprint, edges.Select(row => row.Right).ToArray()) ||
            sites.Any(row => !siteRows.Any(site => N.Same(site, row.Right)))) return false;
        var targetDirections = (Relation)((Relation)observation.Left).Left;
        if (targetDirections.Left is not Relation { Right: Relation { Left: Relation horizontal, Right: Relation vertical } } ||
            targetDirections.Right is not Relation { Left: Relation opposites }) return false;
        var directions = new IElement[] { horizontal, N.E(opposites).Cast<Relation>().Single(row => ReferenceEquals(row.Left, horizontal)).Right,
            vertical, N.E(opposites).Cast<Relation>().Single(row => ReferenceEquals(row.Left, vertical)).Right };
        for (var i = 0; i < pattern.Edges.Length; i++)
        {
            if (edges[i].Right is not Relation actual || !edgeRows.Any(row => ReferenceEquals(row, actual)) ||
                actual.Right is not Relation { Left: Relation pair, Right: Relation headings } || !N.Read(headings, out var headingRows)) return false;
            var from = sites.Single(row => N.Same(row.Left, pattern.Ends[i].Left)).Right;
            var to = sites.Single(row => N.Same(row.Left, pattern.Ends[i].Right)).Right;
            var forward = N.Same(from, pair.Left) && N.Same(to, pair.Right);
            var reverse = N.Same(from, pair.Right) && N.Same(to, pair.Left);
            if (!forward && !reverse) return false;
            var mask = 0;
            foreach (var heading in headingRows)
            {
                var index = Array.FindIndex(directions, direction => ReferenceEquals(direction, heading));
                if (index < 0 || (mask & 1 << index) != 0) return false;
                mask |= 1 << index;
            }
            if ((forward ? mask : Opposite(mask)) != pattern.Headings[i]) return false;
        }
        if (pattern.Centres is [var centre])
        {
            var actualCentre = sites.Single(row => N.Same(row.Left, centre)).Right;
            var incident = edgeRows.OfType<Relation>().Where(row => row.Right is Relation { Left: Relation pair } &&
                (N.Same(pair.Left, actualCentre) || N.Same(pair.Right, actualCentre))).ToArray();
            if (!SameSet(incident, edges.Select(row => row.Right).ToArray())) return false;
        }
        return true;
    }

    private static Relation? Single(IElement[] rows, IElement key)
    {
        var selected = rows.OfType<Relation>().Where(row => N.Same(row.Left, key)).ToArray();
        return selected.Length == 1 ? selected[0] : null;
    }
    private static bool SamePin(IElement expected, IElement supplied) => expected is Relation a && supplied is Relation b &&
        N.Same(a.Left, b.Left) && a.Right is Relation ar && b.Right is Relation br && ReferenceEquals(ar.Left, br.Left) &&
        ar.Right is Relation { Left: Relation af, Right: Relation am } && br.Right is Relation { Left: Relation bf, Right: Relation bm } &&
        SameFootprint(af, bf) && SameMap(am, bm);
    private static bool SameFootprint(Relation first, Relation second) =>
        first.Left is Relation aSites && first.Right is Relation aEdges && second.Left is Relation bSites && second.Right is Relation bEdges &&
        N.Read(aSites, out var left) && N.Read(bSites, out var right) && SameSet(left, right) &&
        N.Read(aEdges, out left) && N.Read(bEdges, out right) && SameSet(left, right);
    private static bool SameMap(Relation first, Relation second) => N.Read(first, out var a) && N.Read(second, out var b) &&
        a.Length == b.Length && a.All(item => item is Relation row && b.OfType<Relation>().Any(other => N.Same(row.Left, other.Left) && N.Same(row.Right, other.Right)));
    private static bool SameSet(IElement[] first, IElement[] second) => first.Length == second.Length &&
        first.All(item => second.Any(other => N.Same(item, other)));
    private static int Opposite(int mask) => ((mask & 1) << 1) | ((mask & 2) >> 1) | ((mask & 4) << 1) | ((mask & 8) >> 1);
}
