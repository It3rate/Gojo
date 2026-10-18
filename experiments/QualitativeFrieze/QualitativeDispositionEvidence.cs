using Gojo.Core;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Qualifies the local evidence retained by each disposition. Heading
/// admission, a connected run, a periodic run and an actual commuting circuit
/// remain separate claims. Their union is supported participation at these
/// selected local reads; it is never a verdict that the whole source is a grid.</summary>
public sealed class QualitativeDispositionEvidence
{
    // route proof = (actual channel -> actual retained route) ->
    //               (unique actual segments -> actual route-next links)
    // circuit proof = actual commuting witness ->
    //                 (Closed(four actual segments) -> Closed(four actual sites))
    // triangle proof = actual closed triangle -> Closed(three actual segments)
    // channel evidence = actual channel -> (route proofs -> (route support ->
    //    (admitted -> (admitted without route support -> (branching -> boundary)))))
    // candidate evidence = actual candidate ->
    //   (channel evidence -> (route proofs -> (route support ->
    //   (circuit proofs -> (circuit support -> (triangle proofs ->
    //   (triangle support -> (supported union -> (admitted only ->
    //   (outside -> (single-edge routes -> (blocked -> unresolved))))))))))))
    // The three disjoint participation scopes are supported, admitted-only,
    // and outside. Branches, mismatches and finite boundaries remain on the
    // actual candidate and channel captures instead of being folded into a score.
    public QualitativeDispositionInduction Induction { get; }
    public IReadOnlyList<QualitativeDispositionCandidateEvidence> Candidates { get; }
    public Relation Proofs { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }
    public bool IsComplete => N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0;

    public QualitativeDispositionEvidence(QualitativeDispositionInduction induction)
    {
        Induction = induction;
        var candidates = new List<QualitativeDispositionCandidateEvidence>();
        var blocked = new List<IElement>(); var pending = new List<IElement>();
        pending.AddRange(N.E(induction.Unresolved));
        pending.AddRange(N.E(induction.Unsearched));
        blocked.AddRange(N.E(induction.Blocked));
        foreach (var row in N.E(induction.Candidates))
        {
            if (row is not Relation actual || !induction.TryReadCandidate(actual, out var candidate))
            { blocked.Add(row); continue; }
            var candidateBlocked = new List<IElement>(); var candidatePending = new List<IElement>();
            var channelEvidence = new List<IElement>(); var routeProofs = new List<IElement>();
            var routeSupport = new List<IElement>(); var singletons = new List<IElement>();
            var circuitProofs = new List<IElement>(); var circuitSupport = new List<IElement>();
            var triangleProofs = new List<IElement>(); var triangleSupport = new List<IElement>();
            var admitted = N.E(candidate.SelectedSegments);
            foreach (var item in N.E(candidate.Basis))
            {
                if (item is not Relation channel || !TryChannel(channel, out var description))
                { candidateBlocked.Add(item); continue; }
                var channelRows = N.E(candidate.Strands).OfType<Relation>().Where(strand => ReferenceEquals(strand.Left, channel)).ToArray();
                if (channelRows is not [var channelRow] || channelRow.Right is not Relation routes || !N.Read(routes, out var paths))
                { candidateBlocked.Add(new Relation(channel, candidate.Strands)); continue; }
                var qualified = new List<IElement>(); var covered = new List<IElement>();
                foreach (var entry in paths)
                {
                    if (entry is not Relation route || !description.Routes.Any(source => ReferenceEquals(source, route)) ||
                        !ValidateRoute(description, route, out var segments, out var links))
                    { candidateBlocked.Add(new Relation(channel, entry)); continue; }
                    if (segments.Length == 1) { singletons.Add(new Relation(channel, route)); continue; }
                    if (description.Periodic && segments.Length < 4) continue;
                    var proof = new Relation(new Relation(channel, route), new Relation(N.S(segments), links));
                    qualified.Add(proof); routeProofs.Add(proof); covered.AddRange(segments); routeSupport.AddRange(segments);
                }
                var admittedHere = admitted.Where(entry => entry is Relation segment &&
                    induction.Observation.TryReadSegment(segment, out _, out _, out _, out var heading) &&
                    description.Headings.Any(signature => SameHeading(heading, signature) || OppositeHeading(heading, signature))).ToArray();
                var actualCovered = Unique(covered);
                channelEvidence.Add(new Relation(channel, new Relation(N.S(qualified.ToArray()), new Relation(N.S(actualCovered),
                    new Relation(N.S(admittedHere), new Relation(N.S(admittedHere.Where(segment => !Contains(actualCovered, segment)).ToArray()),
                        new Relation(description.Competing, description.Boundary)))))));
            }
            foreach (var item in N.E(candidate.Commutations))
            {
                if (item is not Relation circuit || !ValidateCircuit(candidate, circuit, out var segments, out var sites))
                { candidateBlocked.Add(item); continue; }
                circuitProofs.Add(new Relation(circuit, new Relation(N.S(segments), N.S(sites))));
                circuitSupport.AddRange(segments);
            }
            foreach (var item in N.E(candidate.Closures))
            {
                if (item is not Relation triangle || !ValidateTriangle(candidate, triangle, out var segments))
                { candidateBlocked.Add(item); continue; }
                triangleProofs.Add(new Relation(triangle, N.S(segments))); triangleSupport.AddRange(segments);
            }
            var supported = Unique(routeSupport.Concat(circuitSupport).Concat(triangleSupport));
            if (supported.Any(segment => !Contains(admitted, segment))) candidateBlocked.Add(new Relation(N.S(supported), candidate.SelectedSegments));
            var routeScope = N.S(routeProofs.ToArray()); var routeCoverage = N.S(Unique(routeSupport));
            var circuitScope = N.S(circuitProofs.ToArray()); var circuitCoverage = N.S(Unique(circuitSupport));
            var triangleScope = N.S(triangleProofs.ToArray()); var triangleCoverage = N.S(Unique(triangleSupport));
            var channels = N.S(channelEvidence.ToArray()); var support = N.S(supported);
            var admittedOnly = N.S(admitted.Where(segment => !Contains(supported, segment)).ToArray());
            var singletonScope = N.S(singletons.ToArray());
            var rejected = N.S(candidateBlocked.ToArray()); var unresolved = N.S(candidatePending.ToArray());
            var capture = new Relation(candidate.Read, new Relation(channels, new Relation(routeScope, new Relation(routeCoverage,
                new Relation(circuitScope, new Relation(circuitCoverage, new Relation(triangleScope, new Relation(triangleCoverage,
                    new Relation(support, new Relation(admittedOnly, new Relation(candidate.Residue, new Relation(singletonScope,
                        new Relation(rejected, unresolved)))))))))))));
            candidates.Add(new(candidate, capture, channels, routeScope, routeCoverage, circuitScope, circuitCoverage,
                triangleScope, triangleCoverage, support, admittedOnly, candidate.Residue, singletonScope, rejected, unresolved));
            blocked.AddRange(candidateBlocked); pending.AddRange(candidatePending);
        }
        Candidates = candidates;
        Proofs = N.S(candidates.Select(candidate => (IElement)candidate.Surface).ToArray());
        Blocked = N.S(Unique(blocked)); Unresolved = N.S(Unique(pending));
        Surface = new(induction.Surface, new Relation(Proofs, new Relation(Blocked, Unresolved)));
    }

    public Relation Evaluate() => Surface;

    public bool TryReadCandidate(Relation actualCandidate, out QualitativeDispositionCandidateEvidence evidence)
    {
        var actual = Candidates.FirstOrDefault(candidate => ReferenceEquals(candidate.Candidate.Read, actualCandidate));
        evidence = actual!;
        return actual is not null && ReferenceEquals(actual.Surface.Left, actualCandidate) &&
            N.E(Proofs).Any(row => ReferenceEquals(row, actual.Surface));
    }

    public bool TryReadRouteProof(Relation actualCandidate, Relation actualProof, out Relation channel, out Relation route, out Relation segments)
    {
        channel = route = segments = null!;
        if (!TryReadCandidate(actualCandidate, out var evidence) || !N.E(evidence.RouteProofs).Any(row => ReferenceEquals(row, actualProof)) ||
            actualProof is not { Left: Relation { Left: Relation selectedChannel, Right: Relation selectedRoute },
                Right: Relation { Left: Relation participation, Right: Relation links } } ||
            !TryChannel(selectedChannel, out var description) || !ValidateRoute(description, selectedRoute, out var actualSegments, out var actualLinks) ||
            !ReferenceEquals(links, actualLinks) || !SameScope(participation, actualSegments)) return false;
        channel = selectedChannel; route = selectedRoute; segments = participation; return true;
    }

    public bool TryReadCircuitProof(Relation actualCandidate, Relation actualProof, out Relation circuit, out Relation segments, out Relation sites)
    {
        circuit = segments = sites = null!;
        if (!TryReadCandidate(actualCandidate, out var evidence) || !N.E(evidence.CircuitProofs).Any(row => ReferenceEquals(row, actualProof)) ||
            actualProof is not { Left: Relation selectedCircuit, Right: Relation { Left: Relation participation, Right: Relation actualSites } } ||
            !ValidateCircuit(evidence.Candidate, selectedCircuit, out var expectedSegments, out var expectedSites) ||
            !SameScope(participation, expectedSegments) || !SameScope(actualSites, expectedSites)) return false;
        circuit = selectedCircuit; segments = participation; sites = actualSites; return true;
    }

    public bool TryReadTriangleProof(Relation actualCandidate, Relation actualProof, out Relation triangle, out Relation segments)
    {
        triangle = segments = null!;
        if (!TryReadCandidate(actualCandidate, out var evidence) || !N.E(evidence.TriangleProofs).Any(row => ReferenceEquals(row, actualProof)) ||
            actualProof is not { Left: Relation selectedTriangle, Right: Relation participation } ||
            !ValidateTriangle(evidence.Candidate, selectedTriangle, out var expected) || !SameScope(participation, expected)) return false;
        triangle = selectedTriangle; segments = participation; return true;
    }

    private bool TryChannel(Relation channel, out Channel description)
    {
        description = null!;
        if (N.E(Induction.DirectionFamilies).Any(actual => ReferenceEquals(actual, channel)))
        {
            if (channel.Left is not Relation forward || channel.Right is not Relation reverse || !OppositeHeading(forward, reverse)) return false;
            var rows = N.E(Induction.StrandFamilies).OfType<Relation>().Where(row => ReferenceEquals(row.Left, channel)).ToArray();
            if (rows is not [Relation { Right: Relation { Left: Relation routes, Right: Relation { Right: Relation { Left: Relation competing, Right: Relation boundary } } } }]) return false;
            description = new([forward], N.E(routes).OfType<Relation>().ToArray(), competing, boundary, false, null); return true;
        }
        if (!Induction.TryReadRhythm(channel, out var rhythm) || !N.Read(rhythm.Headings, out var headings) || headings is not [Relation a, Relation b]) return false;
        description = new([a, b], N.E(rhythm.Routes).OfType<Relation>().ToArray(), rhythm.Competing, rhythm.Boundary, true, rhythm.Phases); return true;
    }

    private bool ValidateRoute(Channel channel, Relation route, out IElement[] segments, out Relation links)
    {
        segments = []; links = null!;
        if (route is not { Left: Relation { Left: Relation ends, Right: Relation usesScope }, Right: Relation nextScope } ||
            !N.Read(usesScope, out var entries) || entries.Length == 0 || !N.Read(nextScope, out var next)) return false;
        var uses = new List<Use>();
        foreach (var entry in entries)
        {
            if (entry is not Relation use || !TryUse(use, out var read) || !channel.Headings.Any(heading => SameHeading(heading, read.Heading))) return false;
            uses.Add(read);
        }
        if (!N.Distinct(uses.Select(use => (IElement)use.Segment).ToArray()) ||
            !N.Same(ends.Left, uses[0].From) || !N.Same(ends.Right, uses[^1].To) ||
            uses.Where((use, index) => index != 0 && !N.Same(uses[index - 1].To, use.From)).Any()) return false;
        if (channel.Periodic && uses.Where((use, index) => index != 0 && SameHeading(use.Heading, uses[index - 1].Heading)).Any()) return false;
        var closed = N.Same(uses[0].From, uses[^1].To);
        if (next.Length != uses.Count - 1 + (closed ? 1 : 0)) return false;
        for (var i = 0; i < next.Length; i++)
        {
            if (next[i] is not Relation { Left: Relation first, Right: Relation second } ||
                !ReferenceEquals(first.Left, uses[i].Read) || !ReferenceEquals(second.Left, uses[(i + 1) % uses.Count].Read) ||
                first.Right is not Relation firstHeading || second.Right is not Relation secondHeading ||
                !SameHeading(firstHeading, uses[i].Heading) || !SameHeading(secondHeading, uses[(i + 1) % uses.Count].Heading)) return false;
            if (channel.Periodic && (!Phase(channel.Phases!, first, out var phaseA) || !Phase(channel.Phases!, second, out var phaseB) ||
                !N.E((Relation)channel.Phases!.Left).OfType<Relation>().Any(pair => ReferenceEquals(pair.Left, phaseA) && ReferenceEquals(pair.Right, phaseB)))) return false;
        }
        segments = uses.Select(use => (IElement)use.Segment).ToArray(); links = nextScope; return true;
    }

    private bool ValidateCircuit(QualitativeDispositionCandidate candidate, Relation circuit, out IElement[] segments, out IElement[] sites)
    {
        segments = sites = [];
        if (!N.E(candidate.Commutations).Any(actual => ReferenceEquals(actual, circuit)) ||
            circuit is not { Right: Relation { Left: Relation firstScope, Right: Relation secondScope } } ||
            !N.Read(firstScope, out var first) || !N.Read(secondScope, out var second) ||
            first is not [Relation aa, Relation ab] || second is not [Relation ba, Relation bb] ||
            !TryUse(aa, out var x) || !TryUse(ab, out var xx) || !TryUse(ba, out var y) || !TryUse(bb, out var yy) ||
            !N.Same(circuit.Left, x.From) || !N.Same(circuit.Left, y.From) || !N.Same(x.To, xx.From) || !N.Same(y.To, yy.From) ||
            !N.Same(xx.To, yy.To) || !SameHeading(x.Heading, yy.Heading) || !SameHeading(y.Heading, xx.Heading)) return false;
        segments = [x.Segment, xx.Segment, y.Segment, yy.Segment]; sites = [x.From, x.To, xx.To, y.To];
        if (!N.Distinct(segments) || !N.Distinct(sites) || segments.Any(segment => !Contains(N.E(candidate.SelectedSegments), segment))) return false;
        var channels = N.E(candidate.Basis).OfType<Relation>().Select(channel => TryChannel(channel, out var description) ? description : null)
            .Where(channel => channel is not null).Cast<Channel>().ToArray();
        return channels.Any(a => a.Headings.Any(heading => SameHeading(heading, x.Heading)) && channels.Any(b => !ReferenceEquals(a, b) &&
            b.Headings.Any(heading => SameHeading(heading, y.Heading))));
    }

    private bool ValidateTriangle(QualitativeDispositionCandidate candidate, Relation triangle, out IElement[] segments)
    {
        segments = [];
        if (!N.E(candidate.Closures).Any(actual => ReferenceEquals(actual, triangle)) || !N.Read(triangle, out var entries) ||
            entries is not [Relation a, Relation b, Relation c] || !TryUse(a, out var aa) || !TryUse(b, out var bb) || !TryUse(c, out var cc) ||
            !N.Same(aa.To, bb.From) || !N.Same(bb.To, cc.From) || !N.Same(cc.To, aa.From) ||
            !N.Distinct([aa.From, bb.From, cc.From])) return false;
        segments = [aa.Segment, bb.Segment, cc.Segment];
        return N.Distinct(segments) && segments.All(segment => Contains(N.E(candidate.SelectedSegments), segment));
    }

    private bool TryUse(Relation actual, out Use use)
    {
        use = null!;
        if (actual is not { Left: Relation segment, Right: Relation { Left: Relation ends, Right: Relation heading } } ||
            !Induction.Observation.TryReadSegment(segment, out _, out var from, out var to, out var original) ||
            !N.Read(heading, out _) || !N.Read(original, out _) ||
            !(N.Same(ends.Left, from) && N.Same(ends.Right, to) && SameHeading(heading, original) ||
              N.Same(ends.Left, to) && N.Same(ends.Right, from) && OppositeHeading(heading, original))) return false;
        use = new(actual, segment, ends.Left, ends.Right, heading); return true;
    }
    private static bool Phase(Relation phases, Relation state, out Relation phase)
    {
        phase = null!;
        if (phases.Right is not Relation bindings || !N.Read(bindings, out var entries)) return false;
        var matches = entries.OfType<Relation>().Where(binding => ReferenceEquals(binding.Left, state)).ToArray();
        if (matches is not [Relation { Right: Relation selected }] || selected.Right is not Relation heading ||
            state.Right is not Relation actualHeading || !SameHeading(heading, actualHeading)) return false;
        phase = selected; return true;
    }
    private bool OppositeHeading(Relation a, Relation b) => N.Read(a, out var aa) && N.Read(b, out var bb) && aa.Length == bb.Length &&
        aa.All(direction => N.E(Induction.Observation.Frame.Opposites).OfType<Relation>()
            .Any(pair => ReferenceEquals(pair.Left, direction) && bb.Any(other => ReferenceEquals(other, pair.Right))));
    private static bool SameHeading(Relation a, Relation b) => N.Read(a, out var aa) && N.Read(b, out var bb) && aa.Length != 0 &&
        aa.Length == bb.Length && aa.All(direction => bb.Any(other => ReferenceEquals(other, direction)));
    private static bool SameScope(Relation actual, IElement[] expected) => N.Read(actual, out var entries) && entries.Length == expected.Length &&
        entries.Select((entry, index) => N.Same(entry, expected[index])).All(same => same);
    private static bool Contains(IElement[] scope, IElement item) => scope.Any(actual => N.Same(actual, item));
    private static IElement[] Unique(IEnumerable<IElement> entries) => entries.Distinct(N.Participants.Instance).ToArray();
    private sealed class Channel(Relation[] headings, Relation[] routes, Relation competing, Relation boundary, bool periodic, Relation? phases)
    {
        internal Relation[] Headings { get; } = headings; internal Relation[] Routes { get; } = routes;
        internal Relation Competing { get; } = competing; internal Relation Boundary { get; } = boundary;
        internal bool Periodic { get; } = periodic; internal Relation? Phases { get; } = phases;
    }
    private sealed class Use(Relation read, Relation segment, IElement from, IElement to, Relation heading)
    {
        internal Relation Read { get; } = read; internal Relation Segment { get; } = segment;
        internal IElement From { get; } = from; internal IElement To { get; } = to; internal Relation Heading { get; } = heading;
    }
}

public sealed class QualitativeDispositionCandidateEvidence(QualitativeDispositionCandidate candidate, Relation surface,
    Relation channelEvidence, Relation routeProofs, Relation routeSupport, Relation circuitProofs, Relation circuitSupport,
    Relation triangleProofs, Relation triangleSupport, Relation supported, Relation admittedOnly, Relation outside,
    Relation singletons, Relation blocked, Relation unresolved)
{
    public QualitativeDispositionCandidate Candidate { get; } = candidate;
    public Relation Surface { get; } = surface;
    public Relation ChannelEvidence { get; } = channelEvidence;
    public Relation Admitted => Candidate.SelectedSegments;
    public Relation RouteProofs { get; } = routeProofs;
    public Relation RouteSupport { get; } = routeSupport;
    public Relation CircuitProofs { get; } = circuitProofs;
    public Relation CircuitSupport { get; } = circuitSupport;
    public Relation TriangleProofs { get; } = triangleProofs;
    public Relation TriangleSupport { get; } = triangleSupport;
    public Relation Supported { get; } = supported;
    public Relation AdmittedOnly { get; } = admittedOnly;
    public Relation Outside { get; } = outside;
    public Relation Singletons { get; } = singletons;
    public Relation Competing => Candidate.Competing;
    public Relation Unsettled => Candidate.Unsettled;
    public Relation Boundary => Candidate.Boundary;
    public Relation Blocked { get; } = blocked;
    public Relation Unresolved { get; } = unresolved;
}
