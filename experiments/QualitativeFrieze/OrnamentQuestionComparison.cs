using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Finite, non-draining comparison of supplied questions. Training and
/// held-out memberships are inputs, not selected from a successful full-field
/// read. Each reader receives only its induced edge/occurrence scope and actual
/// bindings. Held-out recurrence is conditional deterministic evidence, not a
/// claim of independent samples, statistical generalisation, or beauty.</summary>
public sealed class OrnamentQuestionComparison
{
    // candidates = Closed(identity -> actual heading grammar / translation scope)
    // split = (actual universe -> training membership) ->
    //         (held-out membership -> unselected complement)
    // measure = reader capture -> ((recurrence proofs -> covered participation)
    //          -> (uncovered -> (branch/conflict evidence -> description occurrences)))
    // candidate = actual definition -> (training measure -> held-out measure)
    // cardinality witness = (preferred scope -> other scope) ->
    //                       (Closed(actual paired participants) ->
    //                        (preferred remainder -> other remainder))
    // dominance = (actual better candidate -> actual worse candidate) ->
    //             Closed(actual cardinality witnesses for declared criteria)
    // Preference basis: recurrence and coverage increase; branching/conflicts
    // and description participation decrease. There is no weighted scalar.
    public Relation Definition { get; }
    public Relation Split { get; }
    public Relation Carried { get; }
    public OrnamentQuestionCandidate[] Candidates { get; }
    public Relation TrainingPareto { get; }
    public Relation HeldOutPareto { get; }
    public Relation Pareto => HeldOutPareto;
    public Relation TrainingDominance { get; }
    public Relation HeldOutDominance { get; }
    public Relation Dominance { get; }
    public Relation Ties { get; }
    public Relation Incomparable { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }
    public bool IsComplete => N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0 &&
        Candidates.All(candidate => candidate.Training.IsComplete && candidate.HeldOut.IsComplete);

    private OrnamentQuestionComparison(Relation definition, Relation split, Relation carried,
        OrnamentQuestionCandidate[] candidates, List<IElement> blocked, List<IElement> pending)
    {
        Definition = definition; Split = split; Carried = carried; Candidates = candidates;
        var partitionReady = blocked.Count == 0 && pending.Count == 0;
        foreach (var candidate in candidates)
            foreach (var measure in new[] { candidate.Training, candidate.HeldOut })
            {
                if (N.E(measure.Blocked).Length != 0) blocked.Add(new Relation(candidate.Definition, new Relation(measure.Read, measure.Blocked)));
                if (N.E(measure.Unresolved).Length != 0) pending.Add(new Relation(candidate.Definition, new Relation(measure.Read, measure.Unresolved)));
            }
        Blocked = N.S(blocked.ToArray()); Unresolved = N.S(pending.ToArray());
        var training = Compare(candidates, false, partitionReady);
        var held = Compare(candidates, true, partitionReady);
        TrainingPareto = training.Pareto; HeldOutPareto = held.Pareto;
        TrainingDominance = training.Dominance; HeldOutDominance = held.Dominance;
        Dominance = new(TrainingDominance, HeldOutDominance);
        Ties = new(training.Ties, held.Ties); Incomparable = new(training.Incomparable, held.Incomparable);
        Surface = new(definition, new Relation(split, new Relation(N.S(candidates.Select(c => (IElement)c.Surface).ToArray()),
            new Relation(new Relation(TrainingPareto, HeldOutPareto), new Relation(Dominance,
                new Relation(Ties, new Relation(Incomparable, new Relation(Blocked, Unresolved))))))));
    }

    /// <param name="trainingEdges">Closed actual source edge rows, chosen before candidate evaluation.</param>
    /// <param name="heldOutEdges">Closed actual source edge rows disjoint in edge identity from training.</param>
    public static OrnamentQuestionComparison Heading(QualitativeMotionFrame frame, IElement source,
        Relation topology, Relation bindings, Relation trainingEdges, Relation heldOutEdges,
        Relation candidates, IElement context)
    {
        var blocked = new List<IElement>(); var pending = new List<IElement>();
        var universe = topology.Right is Relation { Left: Relation edges } ? edges : N.S();
        if (topology.Left is not Relation sites || !N.Read(sites, out _) ||
            topology.Right is not Relation { Left: Relation, Right: Relation faces } || !N.Read(faces, out _))
            pending.Add(topology);
        else
        {
            var roles = N.E(sites);
            if (!N.Distinct(roles)) blocked.Add(sites);
            foreach (var edge in N.E(universe))
                if (edge is not Relation { Right: Relation ends } ||
                    !roles.Any(role => N.Same(role, ends.Left)) || !roles.Any(role => N.Same(role, ends.Right)))
                    blocked.Add(new Relation(edge, topology));
        }
        var partition = Partition(universe, trainingEdges, heldOutEdges, true, blocked, pending);
        var definitions = CandidateDefinitions(candidates, blocked, pending);
        if (!N.Read(bindings, out var offers)) { pending.Add(bindings); offers = []; }
        var result = new List<OrnamentQuestionCandidate>();
        if (blocked.Count == 0 && pending.Count == 0)
            foreach (var candidate in definitions)
            {
                var pattern = (Relation)candidate.Right;
                var description = HeadingDescription(pattern);
                var a = Read(partition.Training, candidate, description); var b = Read(partition.HeldOut, candidate, description);
                result.Add(new(candidate, a, b));
            }
        var definition = new Relation(new Relation(source, context), new Relation(frame.Surface,
            new Relation(topology, new Relation(bindings, candidates))));
        return new(definition, partition.Split, partition.Carried, result.ToArray(), blocked, pending);

        OrnamentQuestionMeasure Read(IElement[] selected, Relation candidate, Relation description)
        {
            var roles = selected.Cast<Relation>().SelectMany(edge => new[] { ((Relation)edge.Right).Left, ((Relation)edge.Right).Right })
                .Distinct(N.Participants.Instance).ToArray();
            var localBindings = N.S(offers.OfType<Relation>().Where(row => roles.Any(role => N.Same(role, row.Left))).Cast<IElement>().ToArray());
            var localTopology = new Relation(N.S(roles), new Relation(N.S(selected), N.S()));
            // Reader capture retains the actual induced input, not a filtered
            // result from a reader that could traverse the other partition.
            var reader = new OrnamentStrandDiscovery(frame, source, localTopology, localBindings,
                (Relation)candidate.Right, candidate.Left);
            var recurrences = new List<IElement>(); var covered = new HashSet<IElement>(N.Participants.Instance);
            var footprints = new List<(Relation[] Uses, Relation Proof)>(); var recurrenceCorrespondence = new List<IElement>();
            var phases = N.E((Relation)candidate.Right).Length;
            foreach (var route in N.E(reader.Routes).Cast<Relation>())
            {
                if (!reader.TryReadRoute(route, out var parts) || phases == 0 || parts.Uses.Length < phases * 2) continue;
                // Repeating an identical phase in the question may reproduce
                // the very same ordered material route. Its extra state labels
                // do not become extra recurrence evidence.
                var prior = footprints.FirstOrDefault(footprint => footprint.Uses.Length == parts.Uses.Length && footprint.Uses.Zip(parts.Uses).All(pair =>
                    N.Same(pair.First.Left, pair.Second.Left) &&
                    N.Same(((Relation)pair.First.Right).Left, ((Relation)pair.Second.Right).Left) &&
                    N.Same(((Relation)pair.First.Right).Right, ((Relation)pair.Second.Right).Right)));
                if (prior.Proof is not null)
                { recurrenceCorrespondence.Add(new Relation(route, prior.Proof)); continue; }
                var states = N.E(parts.States);
                var proof = new Relation(route, new Relation(N.S(states.Take(phases).ToArray()), N.S(states.Skip(phases).Take(phases).ToArray())));
                recurrences.Add(proof); footprints.Add((parts.Uses, proof)); recurrenceCorrespondence.Add(new Relation(route, proof));
                foreach (var use in parts.Uses)
                    foreach (var edge in selected.Cast<Relation>().Where(edge => N.Same(edge.Left, use.Left))) covered.Add(edge);
            }
            return new(reader.Surface, N.S(recurrences.ToArray()), N.S(selected.Where(covered.Contains).ToArray()),
                N.S(selected.Where(edge => !covered.Contains(edge)).ToArray()), reader.Competing,
                description, reader.Blocked, reader.Unresolved,
                reader.IsComplete && selected.Length != 0, reader, null, N.S(recurrenceCorrespondence.ToArray()));
        }
    }

    /// <param name="trainingOccurrences">Closed actual occurrence identities selected before this read.</param>
    /// <param name="heldOutOccurrences">Closed disjoint actual occurrence identities.</param>
    public static OrnamentQuestionComparison Translation(QualitativeMotionFrame frame, IElement source,
        Relation occurrences, Relation bindings, Relation trainingOccurrences, Relation heldOutOccurrences,
        Relation candidates, Relation phases, IElement context)
    {
        var blocked = new List<IElement>(); var pending = new List<IElement>();
        var partition = Partition(occurrences, trainingOccurrences, heldOutOccurrences, false, blocked, pending);
        var definitions = CandidateDefinitions(candidates, blocked, pending);
        if (!N.Read(bindings, out var offers)) { pending.Add(bindings); offers = []; }
        var result = new List<OrnamentQuestionCandidate>();
        if (blocked.Count == 0 && pending.Count == 0)
            foreach (var candidate in definitions)
            {
                var description = TranslationDescription((Relation)candidate.Right);
                result.Add(new(candidate, Read(partition.Training, candidate, description), Read(partition.HeldOut, candidate, description)));
            }
        var definition = new Relation(new Relation(source, context), new Relation(frame.Surface,
            new Relation(occurrences, new Relation(bindings, new Relation(phases, candidates)))));
        return new(definition, partition.Split, partition.Carried, result.ToArray(), blocked, pending);

        OrnamentQuestionMeasure Read(IElement[] selected, Relation candidate, Relation description)
        {
            var localBindings = N.S(offers.OfType<Relation>().Where(row => selected.Any(item => N.Same(item, row.Left))).Cast<IElement>().ToArray());
            var reader = new OrnamentOccurrenceRhythm(frame, source, N.S(selected), localBindings, (Relation)candidate.Right, phases, candidate.Left);
            var successors = N.E(reader.Successors).Cast<Relation>().ToArray();
            var recurrences = new List<IElement>(); var covered = new HashSet<IElement>(N.Participants.Instance);
            foreach (var a in successors) foreach (var b in successors)
            {
                reader.TryReadSuccessor(a, out var from, out var middle, out var firstStep, out _);
                reader.TryReadSuccessor(b, out var next, out var to, out var secondStep, out _);
                if (!N.Same(middle, next) || !ReferenceEquals(firstStep, secondStep) || N.Same(from, to)) continue;
                recurrences.Add(new Relation(a, b)); covered.Add(from); covered.Add(middle); covered.Add(to);
            }
            var branch = new List<IElement>();
            foreach (var item in selected)
            {
                var next = successors.Where(edge => N.Same(((Relation)edge.Right).Left, item)).Cast<IElement>().ToArray();
                var previous = successors.Where(edge => N.Same(((Relation)edge.Right).Right, item)).Cast<IElement>().ToArray();
                if (next.Length > 1 || previous.Length > 1) branch.Add(new Relation(item, new Relation(N.S(previous), N.S(next))));
            }
            branch.AddRange(N.E(reader.Conflicts));
            return new(reader.Surface, N.S(recurrences.ToArray()), N.S(selected.Where(covered.Contains).ToArray()),
                N.S(selected.Where(item => !covered.Contains(item)).ToArray()), N.S(branch.ToArray()),
                description, N.S(), reader.Unresolved,
                N.E(reader.Unresolved).Length == 0 && selected.Length != 0, null, reader);
        }
    }

    private static (IElement[] Training, IElement[] HeldOut, Relation Split, Relation Carried) Partition(
        Relation universe, Relation training, Relation heldOut, bool edges,
        List<IElement> blocked, List<IElement> pending)
    {
        var closed = N.Read(universe, out var all) & N.Read(training, out var a) & N.Read(heldOut, out var b);
        if (!closed) pending.Add(new Relation(universe, new Relation(training, heldOut)));
        if (a.Length == 0) pending.Add(training); if (b.Length == 0) pending.Add(heldOut);
        if (!N.Distinct(all) || !N.Distinct(a) || !N.Distinct(b)) blocked.Add(new Relation(universe, new Relation(training, heldOut)));
        foreach (var item in a.Concat(b))
            if (!all.Any(actual => N.Same(actual, item))) blocked.Add(new Relation(item, universe));
        IElement Identity(IElement row) => edges && row is Relation relation ? relation.Left : row;
        if (edges)
        {
            foreach (var item in all)
                if (item is not Relation { Right: Relation }) blocked.Add(new Relation(item, universe));
            if (!N.Distinct(all.Select(Identity).ToArray())) blocked.Add(universe);
        }
        foreach (var item in a)
            foreach (var other in b)
                if (N.Same(Identity(item), Identity(other))) blocked.Add(new Relation(item, other));
        var remainder = N.S(all.Where(item => !a.Concat(b).Any(chosen => N.Same(chosen, item))).ToArray());
        return (a, b, new Relation(new Relation(universe, training), new Relation(heldOut, remainder)), remainder);
    }

    private static Relation[] CandidateDefinitions(Relation candidates, List<IElement> blocked, List<IElement> pending)
    {
        if (!N.Read(candidates, out var entries) || entries.Length == 0) pending.Add(candidates);
        foreach (var entry in entries)
            if (entry is not Relation { Right: Relation }) blocked.Add(new Relation(entry, candidates));
        var rows = entries.OfType<Relation>().Where(row => row.Right is Relation).ToArray();
        if (!N.Distinct(rows.Select(row => row.Left).ToArray())) blocked.Add(candidates);
        return rows;
    }

    private static Relation HeadingDescription(Relation pattern)
    {
        var signatures = N.E(pattern); var rows = new List<IElement>();
        foreach (var signature in signatures.OfType<Relation>())
            foreach (var direction in N.E(signature)) rows.Add(new Relation(new Placeholder(), new Relation(signature, direction)));
        for (var i = 0; i < signatures.Length; i++) rows.Add(new Relation(signatures[i], signatures[(i + 1) % signatures.Length]));
        return N.S(rows.ToArray());
    }

    private static Relation TranslationDescription(Relation translations) => N.S(N.E(translations).OfType<Relation>()
        .SelectMany(word => N.E(word).Select(move => (IElement)new Relation(new Placeholder(), new Relation(word, move)))).ToArray());

    private static (Relation Pareto, Relation Dominance, Relation Ties, Relation Incomparable) Compare(
        OrnamentQuestionCandidate[] candidates, bool held, bool permitted)
    {
        var dominance = new List<IElement>(); var ties = new List<IElement>(); var incomparable = new List<IElement>();
        var eligible = permitted ? candidates.Where(c => (held ? c.HeldOut : c.Training).IsComplete).ToArray() : [];
        var beaten = new HashSet<OrnamentQuestionCandidate>(ReferenceEqualityComparer.Instance);
        for (var i = 0; i < eligible.Length; i++) for (var j = i + 1; j < eligible.Length; j++)
        {
            var a = held ? eligible[i].HeldOut : eligible[i].Training;
            var b = held ? eligible[j].HeldOut : eligible[j].Training;
            var aa = new[] { a.Recurrences, a.Covered, b.Branching, b.Description };
            var bb = new[] { b.Recurrences, b.Covered, a.Branching, a.Description };
            var witnesses = aa.Zip(bb, Cardinality).ToArray();
            var signs = aa.Zip(bb, (x, y) => N.E(x).Length.CompareTo(N.E(y).Length)).ToArray();
            var pair = new Relation(eligible[i].Surface, eligible[j].Surface);
            var proof = new Relation(pair, N.S(witnesses.Cast<IElement>().ToArray()));
            if (signs.All(sign => sign == 0)) ties.Add(proof);
            else if (signs.All(sign => sign >= 0)) { dominance.Add(proof); beaten.Add(eligible[j]); }
            else if (signs.All(sign => sign <= 0))
            {
                dominance.Add(new Relation(new Relation(eligible[j].Surface, eligible[i].Surface),
                    N.S(bb.Zip(aa, Cardinality).Cast<IElement>().ToArray()))); beaten.Add(eligible[i]);
            }
            else incomparable.Add(proof);
        }
        return (N.S(eligible.Where(c => !beaten.Contains(c)).Select(c => (IElement)c.Surface).ToArray()),
            N.S(dominance.ToArray()), N.S(ties.ToArray()), N.S(incomparable.ToArray()));
    }

    private static Relation Cardinality(Relation preferred, Relation other)
    {
        var a = N.E(preferred); var b = N.E(other); var paired = Math.Min(a.Length, b.Length);
        return new(new Relation(preferred, other), new Relation(N.S(a.Take(paired).Zip(b, (x, y) => (IElement)new Relation(x, y)).ToArray()),
            new Relation(N.S(a.Skip(paired).ToArray()), N.S(b.Skip(paired).ToArray()))));
    }
}

/// <summary>A decoded native candidate; names, percentages and rankings are
/// presentation concerns. The two evidence measures remain distinct.</summary>
public sealed class OrnamentQuestionCandidate(Relation definition, OrnamentQuestionMeasure training, OrnamentQuestionMeasure heldOut)
{
    public Relation Definition { get; } = definition;
    public IElement Identity => Definition.Left;
    public OrnamentQuestionMeasure Training { get; } = training;
    public OrnamentQuestionMeasure HeldOut { get; } = heldOut;
    public Relation Surface { get; } = new(definition, new Relation(training.Surface, heldOut.Surface));
}

public sealed class OrnamentQuestionMeasure(Relation read, Relation recurrences, Relation covered, Relation uncovered,
    Relation branching, Relation description, Relation blocked, Relation unresolved, bool complete,
    OrnamentStrandDiscovery? strands, OrnamentOccurrenceRhythm? rhythm, Relation? recurrenceCorrespondence = null)
{
    public Relation Read { get; } = read;
    public Relation Recurrences { get; } = recurrences;
    public Relation RecurrenceCorrespondence { get; } = recurrenceCorrespondence ?? N.S();
    public Relation Covered { get; } = covered;
    public Relation Uncovered { get; } = uncovered;
    public Relation Branching { get; } = branching;
    public Relation Description { get; } = description;
    public Relation Blocked { get; } = blocked;
    public Relation Unresolved { get; } = unresolved;
    /// <summary>The comparison evidence is complete. Known branch alternatives
    /// or phase conflicts may remain as explicit penalties; this is not a
    /// promise of an unambiguous route or a valid alternating labelling.</summary>
    public bool IsComplete { get; } = complete;
    public OrnamentStrandDiscovery? Strands { get; } = strands;
    public OrnamentOccurrenceRhythm? Rhythm { get; } = rhythm;
    public int RecurrenceCount => N.E(Recurrences).Length;
    public int CoverageCount => N.E(Covered).Length;
    public int UncoveredCount => N.E(Uncovered).Length;
    public int BranchingCount => N.E(Branching).Length;
    public int DescriptionCount => N.E(Description).Length;
    public Relation Surface { get; } = new(read, new Relation(new Relation(recurrences, new Relation(recurrenceCorrespondence ?? N.S(), covered)),
        new Relation(uncovered, new Relation(branching, new Relation(description, new Relation(blocked, unresolved))))));
}
