using Gojo.Core;

namespace Gojo.Structures;

/// <summary>
/// An exploratory local sampling surface. Presence, witnessed order, landmark
/// regions and optional metric publications coexist as ordinary relations.
/// Neither sample count nor landmark rank becomes the sampled magnitude.
/// </summary>
public static class SamplingSurface
{
    /// <summary>The policy connects a presence unit, an ordered comparison
    /// frame, and a finite landmark budget. History is retained for this small
    /// fixture; the landmark budget is not a bound on its whole graph.</summary>
    public static Relation Create(Carrier root, IElement source, Carrier presenceAxis,
        Carrier orderAxis, Carrier countAxis, int maxLandmarks = 8) =>
        State(root, source, Group(Relation.Point(presenceAxis, 1),
            Relation.Segment(orderAxis, -1, 1), Relation.Point(countAxis, maxLandmarks)),
            Group(), Group(), new Placeholder(), Open(source, source));

    /// <summary>One acquisition. Zero/one refers only to the exposed presence
    /// predicate. The magnitude slot is open even when a signal exists.</summary>
    public static Relation Sample(Carrier root, IElement source, Carrier presenceAxis, bool exists) =>
        ReadWitness.Create(root, source, Relation.Segment(presenceAxis, 0, 1),
            Group(new Placeholder(), new CarrierLocation(presenceAxis, exists ? 1 : 0)),
            new Placeholder(), source);

    /// <summary>Publication by an external comparable-property service. Order
    /// is candidate versus anchor: -1 below, 0 indistinguishable, +1 above.
    /// The selected pair and exposed frame carry those roles, not an enum.</summary>
    public static Relation Comparison(Carrier root, Relation candidate, Relation anchor, Carrier orderAxis, int order)
    {
        var pair = Group(candidate, anchor);
        return ReadWitness.Create(root, pair, new Relation(pair, Relation.Segment(orderAxis, -1, 1)),
            new CarrierLocation(orderAxis, order), new Placeholder(), pair);
    }

    /// <summary>Admits only presence and locally requested comparison witnesses.
    /// Distinct samples become landmarks while capacity remains. Otherwise they
    /// retain a region between landmarks, with open outer boundaries. No raw
    /// magnitude, automatic distance measurement, or private ancestry is read.</summary>
    public static Relation Observe(Carrier root, Relation state, Relation sample,
        Func<Relation, Relation, Relation> compare)
    {
        if (!TryReadState(state, out var source, out var policy, out var landmarks, out var visits,
                out var metric, out _) || !TryPolicy(policy, out var presenceAxis, out var orderAxis, out var budget) ||
            !landmarks.TryReadOrderedGroup(out var anchors) || !visits.TryReadOrderedGroup(out var history) ||
            anchors.Length > budget || anchors.Any(x => x is not Relation) || history.Any(x => x is not Relation))
            return Open(state, sample);

        var probes = new List<IElement>();
        Relation Failed() => Finish(landmarks, Open(sample, Group(policy, Group(probes.ToArray()))));
        if (!TryReadSample(sample, out var presence, out _) || !presence.Carrier.SameIdentityAs(presenceAxis) ||
            !ReadWitness.TryRead(sample, out var sampleSource, out _, out _, out _, out _) ||
            !ReferenceEquals(source, sampleSource)) return Failed();

        // No-signal is its own presence observation, not an amplitude-zero
        // landmark. In particular, never ask the ordering service to compare it.
        if (presence.Ticks == 0)
            return Finish(landmarks, Visit(sample, new Placeholder(), new Placeholder()));

        IElement direction = new Placeholder();
        foreach (var previous in history.Reverse())
        {
            if (previous is Relation prior && ReadWitness.TryRead(prior, out var selected, out _, out _, out _, out _) &&
                selected is Relation priorSample && TryReadSample(priorSample, out var priorPresence, out _) && priorPresence.Ticks == 1)
            {
                var contrast = Ask(priorSample);
                if (TryReadComparison(contrast, sample, priorSample, orderAxis, out _)) direction = contrast;
                // A failed direction query remains visible, while independently
                // readable membership may still be useful.
                else direction = Open(sample, contrast);
                break;
            }
        }

        var lower = 0; var upper = anchors.Length;
        Relation? equal = null;
        while (lower < upper)
        {
            var middle = lower + (upper - lower) / 2;
            var anchor = (Relation)anchors[middle];
            if (!TryReadComparison(Ask(anchor), sample, anchor, orderAxis, out var order)) return Failed();
            if (order == 0) { equal = anchor; lower = middle; break; }
            if (order < 0) upper = middle;
            else lower = middle + 1;
        }

        // Check the adjacent roles rather than treating a search index as a
        // measurement. The source promises a comparable ordering; this local
        // guard exposes contradicted boundary evidence without a global solver.
        var insertion = lower;
        if (insertion > 0 && (!TryReadComparison(Ask((Relation)anchors[insertion - 1]), sample,
                (Relation)anchors[insertion - 1], orderAxis, out var below) || below <= 0)) return Failed();
        var after = equal is null ? insertion : insertion + 1;
        if (after < anchors.Length && (!TryReadComparison(Ask((Relation)anchors[after]), sample,
                (Relation)anchors[after], orderAxis, out var above) || above >= 0)) return Failed();

        IElement region;
        var nextLandmarks = landmarks;
        if (equal is not null) region = new Relation(equal, equal);
        else if (anchors.Length < budget)
        {
            var ordered = anchors.ToList(); ordered.Insert(insertion, sample);
            nextLandmarks = Group(ordered.ToArray());
            region = new Relation(sample, sample);
        }
        else region = new Relation(insertion == 0 ? new Placeholder() : anchors[insertion - 1],
            insertion == anchors.Length ? new Placeholder() : anchors[insertion]);

        return Finish(nextLandmarks, Visit(sample, region, direction));

        Relation Ask(Relation anchor)
        {
            // Same pair is one acquisition during this visit. Avoid spending
            // another query just because direction and membership both need it.
            foreach (var earlier in probes)
                if (earlier is Relation query && TryReadComparison(query, sample, anchor, orderAxis, out _)) return query;
            var observation = compare(sample, anchor);
            probes.Add(observation);
            return observation;
        }
        Relation Visit(Relation selected, IElement regionRead, IElement directionRead) =>
            ReadWitness.Create(new Carrier(), selected, Group(policy, landmarks), Group(regionRead, directionRead),
                Group(), Group(probes.ToArray()));
        Relation Finish(Relation nextAnchors, Relation witness) =>
            State(root, source, policy, nextAnchors, Group(history.Append(witness).ToArray()), metric, witness);
    }

    /// <summary>Admits an explicitly exposed common metric basis and
    /// sample-to-qualified-publication bindings. Earlier samples and states keep
    /// their open magnitude slots. Partition rank does not supply calibration.</summary>
    public static Relation WithMetric(Carrier root, Relation state, Relation basis, Relation observations)
    {
        if (!TryReadState(state, out var source, out var policy, out var landmarks, out var visits, out var metric, out _) ||
            !visits.TryReadOrderedGroup(out var history)) return Open(state, basis);
        var attempted = Group(basis, observations);
        if (!Span(basis, out var axis, out var start, out var end) || axis.IsUnknown || start != 0 || end != 1 ||
            !observations.TryReadOrderedGroup(out var bindings) || bindings.Length == 0 ||
            bindings.Any(binding => !ValidBinding(binding)))
            return State(root, source, policy, landmarks, visits, metric, Open(state, attempted));
        var snapshot = new ExposedStructureSnapshot();
        // Freeze exposed metric coordinates, preserving the existing acquisition
        // endpoints of each binding. A copied sample wrapper is not a new event.
        var captured = Group(snapshot.Copy(basis), Group(bindings.Select(binding =>
        {
            var connection = (Relation)binding;
            return (IElement)new Relation(connection.Left, snapshot.Copy(connection.Right));
        }).ToArray()));
        var witness = ReadWitness.Create(new Carrier(), state, basis, captured, Group(), attempted);
        return State(root, source, policy, landmarks, visits, captured, witness);

        bool ValidBinding(IElement binding)
        {
            if (binding is not Relation { Left: Relation sample, Right: Relation publication } ||
                !history.Any(visit => visit is Relation v && ReadWitness.TryRead(v, out var selected, out _, out _, out _, out _) &&
                    ReferenceEquals(selected, sample)) ||
                !QualifiedReadSurface.TryReadPublished(publication, out _, out var publishedBasis, out var result,
                    out _, out _, out _, out _, out _) || !publishedBasis.Equals(basis)) return false;
            // A materialized qualified read can expose a bound with an open
            // nominal. It must not be mistaken for a pending supplier equation.
            return result is Placeholder || result is CarrierLocation value && value.Carrier is not null &&
                value.Tick is not null && value.Carrier.SameIdentityAs(axis);
        }
    }

    public static bool TryReadState(Relation state, out IElement source, out Relation policy, out Relation landmarks,
        out Relation visits, out IElement metric, out Relation last)
    {
        if (state.TryReadOrderedGroup(out var fields) && fields.Length == 6 && fields[1] is Relation p &&
            fields[2] is Relation a && fields[3] is Relation v && fields[5] is Relation read)
        { source = fields[0]; policy = p; landmarks = a; visits = v; metric = fields[4]; last = read; return true; }
        source = metric = null!; policy = landmarks = visits = last = null!; return false;
    }

    public static bool TryReadSample(Relation sample, out CarrierLocation presence, out Placeholder magnitude)
    {
        if (ReadWitness.TryRead(sample, out _, out var surface, out var result, out _, out _) &&
            surface is Relation frame && Span(frame, out var axis, out var start, out var end) && !axis.IsUnknown &&
            start == 0 && end == 1 && result is Relation fields && fields.TryReadOrderedGroup(out var parts) &&
            parts.Length == 2 && parts[0] is Placeholder slot && parts[1] is CarrierLocation value &&
            value.Carrier is not null && value.Tick is not null && value.Carrier.SameIdentityAs(axis) && value.Ticks is 0 or 1)
        { presence = value; magnitude = slot; return true; }
        presence = default; magnitude = null!; return false;
    }

    public static bool TryReadComparison(Relation witness, Relation candidate, Relation anchor, Carrier orderAxis, out long order)
    {
        if (TryReadSample(candidate, out var candidatePresence, out _) && candidatePresence.Ticks == 1 &&
            TryReadSample(anchor, out var anchorPresence, out _) && anchorPresence.Ticks == 1 &&
            candidatePresence.Carrier.SameIdentityAs(anchorPresence.Carrier) && !orderAxis.IsUnknown &&
            ReadWitness.TryRead(candidate, out var candidateSource, out _, out _, out _, out _) &&
            ReadWitness.TryRead(anchor, out var anchorSource, out _, out _, out _, out _) &&
            ReferenceEquals(candidateSource, anchorSource) &&
            ReadWitness.TryRead(witness, out var selected, out var surface, out var result, out _, out _) &&
            selected is Relation pair && pair.TryReadOrderedGroup(out var samples) && samples.Length == 2 &&
            ReferenceEquals(samples[0], candidate) && ReferenceEquals(samples[1], anchor) &&
            surface is Relation context && ReferenceEquals(context.Left, pair) && context.Right is Relation frame &&
            Span(frame, out var axis, out var start, out var end) && axis.SameIdentityAs(orderAxis) && start == -1 && end == 1 &&
            result is CarrierLocation answer && answer.Carrier is not null && answer.Tick is not null &&
            answer.Carrier.SameIdentityAs(orderAxis) && answer.Ticks is >= -1 and <= 1)
        { order = answer.Ticks; return true; }
        order = default; return false;
    }

    public static bool TryReadVisit(Relation witness, out Relation sample, out IElement region,
        out IElement direction, out Relation probes)
    {
        if (ReadWitness.TryRead(witness, out var selected, out _, out var result, out _, out var provenance) &&
            selected is Relation s && result is Relation group && group.TryReadOrderedGroup(out var parts) && parts.Length == 2 &&
            provenance is Relation queries && queries.TryReadOrderedGroup(out _))
        { sample = s; region = parts[0]; direction = parts[1]; probes = queries; return true; }
        sample = probes = null!; region = direction = null!; return false;
    }

    private static bool TryPolicy(Relation policy, out Carrier presence, out Carrier order, out int budget)
    {
        if (policy.TryReadOrderedGroup(out var fields) && fields.Length == 3 && Point(fields[0], out var p) &&
            p.Ticks == 1 && !p.Carrier.IsUnknown && fields[1] is Relation frame &&
            Span(frame, out var axis, out var start, out var end) && !axis.IsUnknown && start == -1 && end == 1 &&
            Point(fields[2], out var capacity) && !capacity.Carrier.IsUnknown && capacity.Ticks is >= 1 and <= 240)
        { presence = p.Carrier; order = axis; budget = (int)capacity.Ticks; return true; }
        presence = order = null!; budget = 0; return false;
    }

    private static bool Point(IElement element, out CarrierLocation point)
    {
        if (element is CarrierLocation value && value.Carrier is not null && value.Tick is not null)
        { point = value; return true; }
        if (element is Relation { Left: CarrierLocation location, Right: Carrier carrier } &&
            location.Carrier is not null && location.Tick is not null && carrier.SameIdentityAs(location.Carrier))
        { point = location; return true; }
        point = default; return false;
    }
    private static bool Span(Relation frame, out Carrier axis, out long start, out long end)
    {
        if (Point(frame.Left, out var a) && Point(frame.Right, out var b) && a.Carrier.SameIdentityAs(b.Carrier))
        { axis = a.Carrier; start = a.Ticks; end = b.Ticks; return true; }
        axis = null!; start = end = 0; return false;
    }
    private static Relation State(Carrier root, IElement source, Relation policy, Relation anchors,
        Relation visits, IElement metric, Relation last) => Relation.OrderedGroup(root, source, policy, anchors, visits, metric, last);
    private static Relation Open(IElement selected, IElement attempted) =>
        ReadWitness.Create(new Carrier(), selected, attempted, new Placeholder(), new Relation(selected, attempted), attempted);
    private static Relation Group(params IElement[] fields) => Relation.OrderedGroup(new Carrier(), fields);
}
