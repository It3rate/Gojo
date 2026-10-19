using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A selected, non-draining publication of the current stroke graph.
/// Accepted endpoint seams are already represented by the graph's shared sites.
/// Neither coordinate coincidence nor construction ancestry proposes a join.
/// Recognition receives only fresh site and segment identities and headings.
/// Parallel stroke occurrences remain separate evidence at this resolution.</summary>
public sealed class LatticeLadderObservation
{
    // topology = actual sites -> (Closed(actual stroke -> actual ends) -> Closed())
    // stroke map = Closed(actual graph stroke -> actual topology edge)
    // source = current graph surface -> current site bindings
    // publication = ((source -> topology) -> stroke map) -> publisher surface
    // surface = publication -> (induction surface -> (blocked -> unresolved))
    private readonly Relation sourceStrokes;
    public Relation Source { get; }
    public Relation Topology { get; }
    public Relation StrokeMap { get; }
    public QualitativeRecognitionPublisher Publisher { get; }
    public QualitativeRecognitionObservation Observation { get; }
    public QualitativeRecognitionObservation IncidenceObservation { get; }
    /// <summary>Actual graph stroke to selected observed support segment.
    /// In a support question several source strokes may share one target.</summary>
    public Relation SourceToSupport { get; }
    /// <summary>Each source observation, its support segment, and both ordered
    /// endpoint/heading frames. Reversal remains visible in this witness.</summary>
    public Relation SegmentCorrespondence { get; }
    /// <summary>Each incidence segment to its selected straight-strand segment.</summary>
    public Relation StrandMap { get; }
    /// <summary>Closed ordered oriented incidence uses to their coarse segment.</summary>
    public Relation StrandCorrespondence { get; }
    public Relation CarriedSites { get; }
    public Relation StrandRemainder { get; }
    public Relation Publication { get; }
    public int SourceStrokeCount => N.E(sourceStrokes).Length;
    public int IncidenceStrokeCount => N.E(IncidenceObservation.Segments).Length;
    public int ObservedStrokeCount => N.E(Observation.Segments).Length;
    public QualitativeDispositionInduction Induction { get; }
    public IReadOnlyList<QualitativeDispositionCandidate> Candidates { get; }
    public IReadOnlyList<LatticeLadderCandidateSummary> Summaries { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public Relation Unsearched => Induction.Unsearched;
    public Relation Surface { get; }
    public bool IsComplete => N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0 && Induction.IsComplete;

    /// <summary>Observes the field's actual seam quotient, retaining any failed
    /// or stale field evidence beside the useful published subset. Capacity
    /// bounds disposition proposals; it is not a whole-graph time budget.</summary>
    public static LatticeLadderObservation Read(OrnamentProgramRun run, OrnamentProgramField field,
        int capacity = 128, Relation? declaredCrossings = null) =>
        ReadField(run, field, capacity, declaredCrossings, false, false);

    /// <summary>Asks for incidence support at a coarser resolution. Only an
    /// actual shared endpoint pair and compatible headings coalesce. Every
    /// original stroke and its orientation correspondence remains carried.</summary>
    public static LatticeLadderObservation ReadIncidenceSupport(OrnamentProgramRun run, OrnamentProgramField field,
        int capacity = 128, Relation? declaredCrossings = null) =>
        ReadField(run, field, capacity, declaredCrossings, true, false);

    /// <summary>Continues the incidence question by suppressing only actual
    /// degree-two landmarks with a published opposite-ray continuation.
    /// Ordered paths and suppressed sites remain inspectable beside the read.</summary>
    public static LatticeLadderObservation ReadStrandSupport(OrnamentProgramRun run, OrnamentProgramField field,
        int capacity = 128, Relation? declaredCrossings = null) =>
        ReadField(run, field, capacity, declaredCrossings, true, true);

    private static LatticeLadderObservation ReadField(OrnamentProgramRun run, OrnamentProgramField field,
        int capacity, Relation? declaredCrossings, bool incidenceSupport, bool strandSupport)
    {
        var blocked = N.E(field.Blocked).ToList();
        var unresolved = N.E(field.Unresolved).ToList();
        if (!ReferenceEquals(run, field.Run)) blocked.Add(new Relation(run.Surface, field.Run.Surface));
        if (!run.IsComplete) unresolved.Add(new Relation(run.Blocked, run.Unresolved));
        if (!field.Join.TryReadPassed(field.Join.Evaluate())) unresolved.Add(field.Join.Evaluate());
        return new(field.Graph, field.Bindings, capacity, declaredCrossings, blocked, unresolved, incidenceSupport, strandSupport);
    }

    /// <summary>Reads a standalone current graph without requiring its earlier
    /// rungs or recipe. Crossing declarations name actual current graph strokes;
    /// their absence never creates a crossing or a material connection.</summary>
    public static LatticeLadderObservation Read(QualitativeStrokeGraph graph, Relation bindings,
        int capacity = 128, Relation? declaredCrossings = null) =>
        new(graph, bindings, capacity, declaredCrossings, [], [], false, false);

    public static LatticeLadderObservation ReadIncidenceSupport(QualitativeStrokeGraph graph, Relation bindings,
        int capacity = 128, Relation? declaredCrossings = null) =>
        new(graph, bindings, capacity, declaredCrossings, [], [], true, false);

    public static LatticeLadderObservation ReadStrandSupport(QualitativeStrokeGraph graph, Relation bindings,
        int capacity = 128, Relation? declaredCrossings = null) =>
        new(graph, bindings, capacity, declaredCrossings, [], [], true, true);

    private LatticeLadderObservation(QualitativeStrokeGraph graph, Relation bindings, int capacity,
        Relation? declaredCrossings, List<IElement> blocked, List<IElement> unresolved, bool incidenceSupport, bool strandSupport)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        Source = new(graph.Surface, bindings);
        sourceStrokes = graph.Strokes;
        var edges = new List<IElement>(); var correspondence = new List<IElement>();
        foreach (var item in N.E(graph.Strokes))
        {
            if (item is Relation stroke && QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _))
            {
                // The actual stroke occurrence supplies identity. A stroke's
                // cause may be shared with another occurrence without making
                // their observed participation identical.
                var edge = new Relation(stroke, new Relation(from, to));
                edges.Add(edge); correspondence.Add(new Relation(stroke, edge));
            }
            else
            {
                // Keep malformed participation visible instead of silently
                // filtering it into a complete segment inventory.
                edges.Add(new Relation(item, new Placeholder()));
                unresolved.Add(new Relation(item, graph.Strokes));
            }
        }
        var edgeScope = N.Read(graph.Strokes, out _) ? N.S(edges.ToArray()) : StructuralConstraint.Open(edges.ToArray());
        Topology = new(graph.Sites, new Relation(edgeScope, N.S()));
        StrokeMap = N.S(correspondence.ToArray());
        Publisher = new(graph.Frame, Topology, bindings, declaredCrossings);
        var published = Publisher.Observation;
        StrandMap = StrandCorrespondence = CarriedSites = StrandRemainder = N.S();
        if (incidenceSupport)
        {
            var support = Support(published);
            IncidenceObservation = support.Observation; Observation = IncidenceObservation;
            SegmentCorrespondence = support.Correspondence;
            if (strandSupport)
            {
                var strands = Strands(IncidenceObservation);
                Observation = strands.Observation; StrandMap = strands.Map; StrandCorrespondence = strands.Correspondence;
                CarriedSites = strands.Carried; StrandRemainder = strands.Remainder;
            }
            var mapping = N.E(SegmentCorrespondence).Cast<Relation>().ToArray();
            var strandMapping = N.E(StrandMap).Cast<Relation>().ToArray();
            SourceToSupport = N.S(N.E(Publisher.SegmentMap).Cast<Relation>().Select(row =>
            {
                var incidence = ((Relation)mapping.Single(map => ReferenceEquals(map.Left, row.Right)).Right).Left;
                var target = strandSupport ? strandMapping.Single(map => ReferenceEquals(map.Left, incidence)).Right : incidence;
                return (IElement)new Relation(row.Left, target);
            }).ToArray());
            Publication = new(new Relation(new Relation(new Relation(Source, Topology), StrokeMap), Publisher.Surface),
                new Relation(new Relation(SourceToSupport, SegmentCorrespondence), new Relation(IncidenceObservation.Surface,
                    new Relation(support.Remainder, new Relation(new Relation(StrandMap, StrandCorrespondence),
                        new Relation(CarriedSites, new Relation(Observation.Surface, StrandRemainder)))))));
        }
        else
        {
            Observation = IncidenceObservation = published;
            SourceToSupport = Publisher.SegmentMap;
            SegmentCorrespondence = N.S(N.E(published.Segments).OfType<Relation>().Select(segment =>
                (IElement)new Relation(segment, new Relation(segment, new Relation(segment.Right, segment.Right)))).ToArray());
            Publication = new(new Relation(new Relation(Source, Topology), StrokeMap),
                new Relation(Publisher.Surface, new Relation(SourceToSupport, SegmentCorrespondence)));
        }
        Induction = new(Observation, capacity);
        var candidates = new List<QualitativeDispositionCandidate>();
        foreach (var item in N.E(Induction.Candidates))
        {
            if (item is Relation row && Induction.TryReadCandidate(row, out var candidate)) candidates.Add(candidate);
            else blocked.Add(item);
        }
        Candidates = candidates.AsReadOnly();
        Summaries = candidates.Select(candidate => new LatticeLadderCandidateSummary(candidate)).ToArray();
        blocked.AddRange(N.E(Publisher.Blocked)); blocked.AddRange(N.E(Induction.Blocked));
        unresolved.AddRange(N.E(Publisher.Unresolved)); unresolved.AddRange(N.E(Induction.Unresolved));
        Blocked = N.S(blocked.Distinct(N.Participants.Instance).ToArray());
        Unresolved = N.S(unresolved.Distinct(N.Participants.Instance).ToArray());
        Surface = new(Publication, new Relation(Induction.Surface, new Relation(Blocked, Unresolved)));
    }

    private static (QualitativeRecognitionObservation Observation, Relation Correspondence, Relation Remainder)
        Support(QualitativeRecognitionObservation source)
    {
        var segments = new List<IElement>(); var correspondence = new List<IElement>(); var remainder = new List<IElement>();
        var targets = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
        var groups = new List<(Relation Segment, IElement From, IElement To, Relation Heading)>();
        foreach (var item in N.E(source.Segments))
        {
            if (item is not Relation segment || !source.TryReadSegment(segment, out _, out var from, out var to, out var heading) ||
                !N.Read(heading, out var directions) || directions.Length == 0)
            {
                // Open or malformed participation is not evidence for merging.
                segments.Add(item); remainder.Add(item);
                if (item is Relation unknown)
                {
                    targets.Add(unknown, unknown);
                    correspondence.Add(new Relation(unknown, new Relation(unknown, new Relation(unknown.Right, unknown.Right))));
                }
                continue;
            }
            var opposite = N.S(directions.Select(direction => N.E(source.Frame.Opposites).Cast<Relation>()
                .Single(pair => ReferenceEquals(pair.Left, direction)).Right).ToArray());
            var group = groups.FirstOrDefault(group =>
                N.Same(group.From, from) && N.Same(group.To, to) && SameHeading(group.Heading, heading) ||
                N.Same(group.From, to) && N.Same(group.To, from) && SameHeading(group.Heading, opposite));
            Relation target;
            if (group.Segment is null)
            {
                target = new(new Placeholder(), new Relation(new Relation(from, to), heading));
                segments.Add(target); groups.Add((target, from, to, heading));
            }
            else target = group.Segment;
            targets.Add(segment, target);
            correspondence.Add(new Relation(segment, new Relation(target, new Relation(segment.Right, target.Right))));
        }
        var continuations = new List<IElement>(); var crossings = new List<IElement>();
        foreach (var item in N.E(source.Continuations))
        {
            if (item is Relation row && source.TryReadContinuation(row, out var site, out var first, out var second) &&
                targets.TryGetValue(first, out var a) && targets.TryGetValue(second, out var b) && !ReferenceEquals(a, b))
                continuations.Add(new Relation(site, new Relation(a, b)));
            else remainder.Add(item);
        }
        foreach (var item in N.E(source.Crossings))
        {
            if (item is Relation row && source.TryReadCrossing(row, out var first, out var second) &&
                targets.TryGetValue(first, out var a) && targets.TryGetValue(second, out var b) && !ReferenceEquals(a, b))
                crossings.Add(new Relation(a, b));
            else remainder.Add(item);
        }
        Relation Scope(Relation original, IElement[] entries) => N.Read(original, out _) ? N.S(entries) : StructuralConstraint.Open(entries);
        var observation = new QualitativeRecognitionObservation(source.Frame, source.Sites, Scope(source.Segments, segments.ToArray()),
            Scope(source.Continuations, continuations.ToArray()), Scope(source.Crossings, crossings.ToArray()));
        return (observation, N.S(correspondence.ToArray()), N.S(remainder.ToArray()));
    }

    private static (QualitativeRecognitionObservation Observation, Relation Map, Relation Correspondence, Relation Carried, Relation Remainder)
        Strands(QualitativeRecognitionObservation source)
    {
        var rows = N.E(source.Segments).OfType<Relation>().ToArray();
        var valid = rows.Where(row => source.TryReadSegment(row, out _, out _, out _, out var heading) &&
            N.Read(heading, out var directions) && directions.Length > 0).ToArray();
        var eligible = new HashSet<IElement>(N.Participants.Instance);
        foreach (var site in N.Read(source.Segments, out _) && N.Read(source.Sites, out _) ? N.E(source.Sites) : [])
        {
            var incident = valid.Where(row => Touches(row, site)).ToArray();
            // All observed incidences count, including open headings. A known
            // straight pair cannot hide a third, incompletely read branch.
            if (rows.Count(row => Touches(row, site)) != 2 || incident is not [var a, var b]) continue;
            var continuation = N.E(source.Continuations).OfType<Relation>().Any(row =>
                source.TryReadContinuation(row, out var actualSite, out var first, out var second) && N.Same(site, actualSite) &&
                (ReferenceEquals(first, a) && ReferenceEquals(second, b) || ReferenceEquals(first, b) && ReferenceEquals(second, a)));
            if (continuation && SameHeading(Outward(a, site), Opposite(Outward(b, site)))) eligible.Add(site);
        }
        var visited = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        var targets = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
        var segments = new List<IElement>(); var correspondence = new List<IElement>();
        var carried = new HashSet<IElement>(N.Participants.Instance); var remainder = new List<IElement>();
        foreach (var first in valid)
        {
            if (visited.Contains(first)) continue;
            source.TryReadSegment(first, out _, out var a, out var b, out _);
            if (eligible.Contains(a) && eligible.Contains(b)) continue;
            var start = eligible.Contains(a) ? b : a;
            var at = start; var current = first; var uses = new List<IElement>(); var members = new List<Relation>();
            var intermediate = new List<IElement>(); var local = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
            while (local.Add(current))
            {
                source.TryReadSegment(current, out _, out var from, out var to, out _);
                var next = N.Same(at, from) ? to : from;
                uses.Add(new Relation(current, new Relation(at, next))); members.Add(current);
                at = next;
                if (!eligible.Contains(at)) break;
                intermediate.Add(at);
                current = valid.Single(row => !ReferenceEquals(row, current) && Touches(row, at));
            }
            // A closed all-straight component has no justified open span.
            // Preserve it at the original resolution rather than making a
            // forbidden zero-ended segment or choosing an arbitrary cut.
            if (N.Same(start, at)) continue;
            var target = members.Count == 1 && N.Same(start, a) ? first : new Relation(new Placeholder(),
                new Relation(new Relation(start, at), Outward(first, start)));
            segments.Add(target); correspondence.Add(new Relation(N.S(uses.ToArray()), target));
            foreach (var member in members) { visited.Add(member); targets.Add(member, target); }
            foreach (var site in intermediate) carried.Add(site);
        }
        foreach (var item in N.E(source.Segments))
        {
            if (item is Relation row && visited.Contains(row)) continue;
            segments.Add(item);
            if (item is Relation segment)
            {
                targets.Add(segment, segment);
                correspondence.Add(new Relation(N.S(new Relation(segment,
                    segment.Right is Relation frame ? frame.Left : segment)), segment));
            }
        }
        var continuations = new List<IElement>(); var crossings = new List<IElement>();
        foreach (var item in N.E(source.Continuations))
        {
            if (item is Relation row && source.TryReadContinuation(row, out var site, out var first, out var second) &&
                !carried.Contains(site) && targets.TryGetValue(first, out var a) && targets.TryGetValue(second, out var b) &&
                !ReferenceEquals(a, b)) continuations.Add(new Relation(site, new Relation(a, b)));
            else remainder.Add(item);
        }
        foreach (var item in N.E(source.Crossings))
        {
            if (item is Relation row && source.TryReadCrossing(row, out var first, out var second) &&
                targets.TryGetValue(first, out var a) && targets.TryGetValue(second, out var b) && !ReferenceEquals(a, b))
                crossings.Add(new Relation(a, b));
            else remainder.Add(item);
        }
        Relation Scope(Relation original, IElement[] entries) => N.Read(original, out _) ? N.S(entries) : StructuralConstraint.Open(entries);
        var sites = Scope(source.Sites, N.E(source.Sites).Where(site => !carried.Contains(site)).ToArray());
        var observation = new QualitativeRecognitionObservation(source.Frame, sites, Scope(source.Segments, segments.ToArray()),
            Scope(source.Continuations, continuations.ToArray()), Scope(source.Crossings, crossings.ToArray()));
        return (observation, N.S(targets.Select(pair => (IElement)new Relation(pair.Key, pair.Value)).ToArray()),
            N.S(correspondence.ToArray()), N.S(carried.ToArray()), N.S(remainder.ToArray()));

        bool Touches(Relation row, IElement site) => row.Right is Relation { Left: Relation ends } &&
            (N.Same(ends.Left, site) || N.Same(ends.Right, site));
        Relation Outward(Relation row, IElement site)
        {
            source.TryReadSegment(row, out _, out var from, out _, out var heading);
            return N.Same(from, site) ? heading : Opposite(heading);
        }
        Relation Opposite(Relation heading) => N.S(N.E(heading).Select(direction => N.E(source.Frame.Opposites).Cast<Relation>()
            .Single(pair => ReferenceEquals(pair.Left, direction)).Right).ToArray());
    }

    private static bool SameHeading(Relation first, Relation second)
    {
        var a = N.E(first); var b = N.E(second);
        return a.Length == b.Length && a.All(direction => b.Any(other => ReferenceEquals(direction, other)));
    }
}

/// <summary>Calculated presentation counts over one exact retained candidate.
/// Positive grid commutations or triangle circuits support local readings;
/// no count chooses a winner, supplies a metric, or erases a remainder.</summary>
public sealed class LatticeLadderCandidateSummary(QualitativeDispositionCandidate candidate)
{
    public QualitativeDispositionCandidate Candidate { get; } = candidate;
    public Relation Surface => Candidate.Read;
    public int Channels => N.E(Candidate.Basis).Length;
    public int Selected => N.E(Candidate.SelectedSegments).Length;
    public int Commutations => N.E(Candidate.Commutations).Length;
    public int Closures => N.E(Candidate.Closures).Length;
    public int UnexpectedClosures => N.E(Candidate.UnexpectedClosures).Length;
    public int Boundary => N.E(Candidate.Boundary).Length;
    public int Competing => N.E(Candidate.Competing).Length;
    public int Residue => N.E(Candidate.Residue).Length;
    public int Unsettled => N.E(Candidate.Unsettled).Length;
}
