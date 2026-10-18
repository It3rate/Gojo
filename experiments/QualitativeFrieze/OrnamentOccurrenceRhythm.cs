using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A non-draining repeat read over actual occurrence identities and
/// exposed word bindings. Declared translations supply witnessed successor
/// relations; a selected two-phase cycle supplies alternating participation.
/// Equal positions never identify occurrences. A conflicted component retains
/// its proposed assignments as evidence and publishes no complete labels.</summary>
public sealed class OrnamentOccurrenceRhythm
{
    // comparison = ((actual from-binding -> actual to-binding) -> actual step)
    //              -> ((source inversion -> difference-read) ->
    //                  (step inversion -> difference-minus-step-read))
    // successor = actual comparison -> (actual from-occurrence -> actual to-occurrence)
    // assignment = actual occurrence -> selected phase
    // assignment-proof = actual assignment -> (prior assignment ->
    //                        (actual successor -> actual phase transition))
    // component = (root assignment -> Closed(actual occurrence identities)) ->
    //             (Closed(assignment proofs) -> Closed(actual successor relations))
    // conflict = actual successor -> (actual from-assignment -> actual to-assignment)
    public QualitativeMotionFrame Frame { get; }
    public IElement Source { get; }
    public Relation Occurrences { get; }
    public Relation Bindings { get; }
    public Relation Translations { get; }
    public Relation Phases { get; }
    public IElement Question { get; }
    public Relation PhaseTransitions { get; }
    public Relation Definition { get; }
    public Relation Comparisons { get; }
    public Relation Successors { get; }
    public Relation Roots { get; }
    public Relation Components { get; }
    public Relation Assignments { get; }
    public Relation Conflicts { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }
    public bool IsComplete => N.E(Conflicts).Length == 0 && N.E(Unresolved).Length == 0;

    public OrnamentOccurrenceRhythm(QualitativeMotionFrame frame, IElement source,
        Relation occurrences, Relation bindings, Relation translations, Relation phases, IElement question)
    {
        Frame = frame; Source = source; Occurrences = occurrences; Bindings = bindings;
        Translations = translations; Phases = phases; Question = question;
        var pending = new List<IElement>(); var comparisons = new List<IElement>();
        var successors = new List<IElement>(); var roots = new List<IElement>();
        var components = new List<IElement>(); var assignments = new List<IElement>();
        var conflicts = new List<IElement>();
        var occurrenceClosed = N.Read(occurrences, out var items);
        var bindingClosed = N.Read(bindings, out var bindingItems);
        var translationClosed = N.Read(translations, out var stepItems);
        var phaseClosed = N.Read(phases, out var phaseItems);
        var occurrenceReady = occurrenceClosed && N.Distinct(items);
        var bindingReady = bindingClosed;
        var translationReady = translationClosed && stepItems.Length != 0 && N.Distinct(stepItems);
        var phaseReady = phaseClosed && phaseItems.Length == 2 && N.Distinct(phaseItems) &&
            phaseItems.All(item => item is Placeholder { IsInverse: false });
        if (!occurrenceReady) pending.Add(occurrences);
        if (!bindingReady) pending.Add(bindings);
        if (!translationReady) pending.Add(translations);
        if (!phaseReady) pending.Add(phases);
        PhaseTransitions = phaseReady
            ? N.S(new Relation(phaseItems[0], phaseItems[1]), new Relation(phaseItems[1], phaseItems[0])) : N.S();
        Definition = new(new Relation(source, question), new Relation(frame.Surface,
            new Relation(occurrences, new Relation(bindings,
                new Relation(translations, new Relation(phases, PhaseTransitions))))));

        var actualBindings = new Dictionary<IElement, Relation>(N.Participants.Instance);
        foreach (var item in items)
        {
            var offers = bindingItems.OfType<Relation>().Where(binding => N.Same(binding.Left, item)).ToArray();
            if (offers is not [var binding] || binding.Right is not Relation word || !Word(word))
            { pending.Add(new Relation(item, bindings)); bindingReady = false; continue; }
            if (!actualBindings.TryAdd(item, binding)) bindingReady = false;
        }
        var steps = new List<(Relation Word, Relation Inversion)>();
        foreach (var item in stepItems)
        {
            if (item is not Relation step || !Word(step))
            { pending.Add(new Relation(item, translations)); translationReady = false; continue; }
            var read = frame.Reduce(N.S(), step);
            if (!frame.TryReadResult(read, out var remaining, out _, out var unknown) ||
                !N.Read(remaining, out var terms) || terms.Length == 0 || N.E(unknown).Length != 0)
            { pending.Add(new Relation(step, read)); translationReady = false; continue; }
            steps.Add((step, Inverse(step)));
        }
        if (occurrenceReady && bindingReady && translationReady && phaseReady)
        {
            foreach (var from in items) foreach (var to in items)
            {
                var first = actualBindings[from]; var second = actualBindings[to];
                var inverse = Inverse((Relation)first.Right);
                var difference = frame.Reduce((Relation)second.Right, (Relation)((Relation)inverse.Left).Right);
                var differenceKnown = frame.TryReadResult(difference, out var displacement, out _, out var unknown) &&
                    N.Read(displacement, out _) && N.E(unknown).Length == 0;
                foreach (var step in steps)
                {
                    var check = frame.Reduce(displacement, (Relation)((Relation)step.Inversion.Left).Right);
                    var comparison = new Relation(new Relation(new Relation(first, second), step.Word),
                        new Relation(new Relation(inverse, difference), new Relation(step.Inversion, check)));
                    comparisons.Add(comparison);
                    if (!differenceKnown || !frame.TryReadResult(check, out var residual, out _, out var unresolved) ||
                        !N.Read(residual, out var terms) || N.E(unresolved).Length != 0)
                    { pending.Add(comparison); continue; }
                    if (terms.Length == 0) successors.Add(new Relation(comparison, new Relation(from, to)));
                }
            }

            var visited = new HashSet<IElement>(N.Participants.Instance);
            foreach (var root in items)
            {
                if (!visited.Add(root)) continue;
                var local = new Dictionary<IElement, Relation>(N.Participants.Instance);
                var proofs = new List<IElement>(); var members = new List<IElement>(); var localEdges = new List<IElement>();
                var rootAssignment = new Relation(root, phaseItems[0]); roots.Add(rootAssignment);
                local.Add(root, rootAssignment); proofs.Add(new Relation(rootAssignment, new Relation(Question, Phases)));
                var queue = new Queue<IElement>(); queue.Enqueue(root);
                while (queue.TryDequeue(out var at))
                {
                    members.Add(at); var prior = local[at];
                    foreach (var edge in successors.Cast<Relation>())
                    {
                        var ends = (Relation)edge.Right;
                        var outgoing = N.Same(ends.Left, at); var incoming = N.Same(ends.Right, at);
                        if (!outgoing && !incoming) continue;
                        if (!localEdges.Any(actual => ReferenceEquals(actual, edge))) localEdges.Add(edge);
                        var next = outgoing ? ends.Right : ends.Left;
                        if (local.ContainsKey(next)) continue;
                        var transition = N.E(PhaseTransitions).Cast<Relation>().Single(row => N.Same(row.Left, prior.Right));
                        var assignment = new Relation(next, transition.Right); local.Add(next, assignment);
                        proofs.Add(new Relation(assignment, new Relation(prior, new Relation(edge, transition))));
                        visited.Add(next); queue.Enqueue(next);
                    }
                }
                var componentConflicts = new List<IElement>();
                foreach (var edge in localEdges.Cast<Relation>())
                {
                    var ends = (Relation)edge.Right; var a = local[ends.Left]; var b = local[ends.Right];
                    if (N.Same(a.Right, b.Right)) componentConflicts.Add(new Relation(edge, new Relation(a, b)));
                }
                conflicts.AddRange(componentConflicts);
                components.Add(new Relation(new Relation(rootAssignment, N.S(members.ToArray())),
                    new Relation(N.S(proofs.ToArray()), N.S(localEdges.ToArray()))));
                // An unknown comparison may hide another repeat constraint. A
                // provisional colouring must not then become a complete label.
                if (componentConflicts.Count == 0 && pending.Count == 0) assignments.AddRange(local.Values);
            }
        }
        Comparisons = N.S(comparisons.ToArray()); Successors = N.S(successors.ToArray());
        Roots = N.S(roots.ToArray()); Components = N.S(components.ToArray()); Assignments = N.S(assignments.ToArray());
        Conflicts = N.S(conflicts.ToArray()); Unresolved = N.S(pending.ToArray());
        Surface = new(Definition, new Relation(Comparisons, new Relation(Successors,
            new Relation(Roots, new Relation(Components,
                new Relation(Assignments, new Relation(Conflicts, Unresolved)))))));

        bool Word(Relation word) => N.Read(word, out var moves) &&
            moves.All(move => move is Relation term && frame.Direction(term, out _, out _, out _));

        Relation Inverse(Relation word)
        {
            var output = new List<IElement>(); var proof = new List<IElement>();
            foreach (var move in N.E(word).Cast<Relation>().Reverse())
            {
                var law = N.E(frame.Opposites).Cast<Relation>().Single(row => ReferenceEquals(row.Left, move.Left));
                var inverse = QualitativeMotionFrame.Move(law.Right, move.Right); output.Add(inverse);
                proof.Add(new Relation(new Relation(move, law), inverse));
            }
            return new(new Relation(word, N.S(output.ToArray())), N.S(proof.ToArray()));
        }
    }

    public Relation Evaluate() => Surface;

    public bool TryReadAssignment(IElement occurrence, out IElement phase)
    {
        phase = null!;
        var matches = N.E(Assignments).OfType<Relation>().Where(row => N.Same(row.Left, occurrence)).ToArray();
        if (matches is not [var actual]) return false;
        phase = actual.Right; return true;
    }

    public bool TryReadSuccessor(Relation actualSuccessor, out IElement from, out IElement to,
        out Relation translation, out Relation proof)
    {
        from = to = null!; translation = proof = null!;
        if (!N.E(Successors).Any(row => ReferenceEquals(row, actualSuccessor)) ||
            actualSuccessor is not { Left: Relation { Left: Relation { Right: Relation step } } comparison,
                Right: Relation ends }) return false;
        from = ends.Left; to = ends.Right; translation = step; proof = comparison; return true;
    }
}
