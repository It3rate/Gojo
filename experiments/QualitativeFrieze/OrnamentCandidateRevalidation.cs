using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Reapplies retained occurrence obligations to an exposed new state.
/// A counterexample refutes this application, never its historical validity.
/// Unrelated open bindings do not invalidate a locally complete occurrence.
/// Route support promises the ordered walk and phase grammar, not continued
/// maximality, uniqueness, or absence of newly introduced branches.</summary>
public sealed class OrnamentCandidateRevalidation
{
    // witness = actual earlier occurrence ->
    //   (Closed(consumed dependencies / calculations) ->
    //    (Closed(counterexamples) -> Closed(open obligations)))
    // read = ((earlier surface -> actual candidate) ->
    //         ((new topology -> new bindings) -> (question -> change))) ->
    //        (supported -> (refuted -> unresolved))
    public IElement Previous { get; }
    public IElement Candidate { get; }
    public Relation Occurrences { get; }
    public Relation Topology { get; }
    public Relation Bindings { get; }
    public IElement Question { get; }
    public Relation Definition { get; }
    public Relation Supported { get; }
    public Relation Refuted { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }
    public bool IsComplete => N.E(Unresolved).Length == 0;

    private OrnamentCandidateRevalidation(IElement previous, IElement candidate, Relation occurrences,
        Relation topology, Relation bindings, IElement question, IElement? change,
        Action<IElement, Check> evaluate)
    {
        Previous = previous; Candidate = candidate; Occurrences = occurrences;
        Topology = topology; Bindings = bindings; Question = question;
        Definition = new(new Relation(previous, candidate),
            new Relation(new Relation(topology, bindings), new Relation(question, change ?? N.S())));
        var supported = new List<IElement>(); var refuted = new List<IElement>(); var pending = new List<IElement>();
        foreach (var occurrence in N.E(occurrences))
        {
            var check = new Check(); evaluate(occurrence, check);
            var witness = new Relation(occurrence, new Relation(N.S(check.Proofs.ToArray()),
                new Relation(N.S(check.Failures.ToArray()), N.S(check.Unknown.ToArray()))));
            if (check.Failures.Count != 0) refuted.Add(witness);
            else if (check.Unknown.Count != 0) pending.Add(witness);
            else supported.Add(witness);
        }
        if (!N.Read(occurrences, out _)) pending.Add(new Relation(occurrences,
            new Relation(N.S(), new Relation(N.S(), N.S(occurrences)))));
        Supported = N.S(supported.ToArray()); Refuted = N.S(refuted.ToArray()); Unresolved = N.S(pending.ToArray());
        Surface = new(Definition, new Relation(Supported, new Relation(Refuted, Unresolved)));
    }

    public static OrnamentCandidateRevalidation Cells(OrnamentCellDiscovery previous, Relation actualFamily,
        Relation topology, Relation bindings, IElement question, IElement? change = null)
    {
        var valid = previous.TryReadFamily(actualFamily, out var family);
        var occurrences = valid ? family.Occurrences : N.S(actualFamily);
        var state = new State(topology, bindings);
        return new(previous.Surface, actualFamily, occurrences, topology, bindings, question, change, (item, check) =>
        {
            if (!valid || item is not Relation occurrence ||
                !previous.TryReadOccurrence(actualFamily, occurrence, out var faceIdentity, out var boundary, out var correspondence))
            { check.Unknown.Add(new Relation(item, previous.Families)); return; }
            check.Proofs.Add(correspondence);
            if (!state.Ready) { check.Unknown.Add(topology); return; }
            var faces = state.Faces.Match(faceIdentity, check);
            if (faces is not Relation { Right: Relation currentBoundary }) return;
            if (!Closed(currentBoundary, check, out var currentParts)) return;
            if (!Closed(boundary, check, out var oldParts)) return;
            if (!SameBoundary(oldParts, currentParts)) check.Failures.Add(new Relation(boundary, currentBoundary));
            foreach (var itemPart in oldParts)
            {
                if (itemPart is not Relation { Right: Relation ends } dart)
                { check.Unknown.Add(itemPart); continue; }
                state.Edge(dart.Left, ends, check);
            }
            // Preserve the exact ordered cycle claim rather than guessing a new
            // chamber when a seam disappears or an endpoint is detached.
            if (oldParts.Length < 3 || !N.Distinct(oldParts.OfType<Relation>().Select(dart => dart.Left).ToArray()) ||
                oldParts.Where((part, i) => part is not Relation { Right: Relation ends } ||
                    oldParts[(i + 1) % oldParts.Length] is not Relation { Right: Relation next } ||
                    !N.Same(ends.Right, next.Left)).Any()) check.Failures.Add(boundary);
            check.Proofs.Add(new Relation((Relation)occurrence.Left, faces));
        });
    }

    public static OrnamentCandidateRevalidation Strands(OrnamentStrandDiscovery previous, Relation actualRoutes,
        Relation topology, Relation bindings, IElement question, IElement? change = null)
    {
        var state = new State(topology, bindings);
        return new(previous.Surface, actualRoutes, actualRoutes, topology, bindings, question, change, (item, check) =>
        {
            if (item is not Relation route || !previous.TryReadRoute(route, out var strand))
            { check.Unknown.Add(new Relation(item, previous.Routes)); return; }
            if (!state.Ready) { check.Unknown.Add(topology); return; }
            var states = N.E(strand.States);
            if (!N.Read(strand.States, out _) || states.Length != strand.Uses.Length)
            { check.Unknown.Add(strand.States); return; }
            for (var i = 0; i < strand.Uses.Length; i++)
            {
                var use = strand.Uses[i];
                if (use.Right is not Relation ends || states[i] is not Relation { Right: Relation phase } actualState ||
                    !ReferenceEquals(actualState.Left, use) || phase.Right is not Relation expectedHeading)
                { check.Unknown.Add(use); continue; }
                state.Edge(use.Left, ends, check);
                var from = state.Word(ends.Left, previous.Frame, check);
                var to = state.Word(ends.Right, previous.Frame, check);
                if (from is not null && to is not null)
                {
                    var inverse = Inverse(previous.Frame, (Relation)from.Right);
                    var reduction = previous.Frame.Reduce((Relation)to.Right, inverse);
                    check.Proofs.Add(new Relation(use, new Relation(new Relation(from, to), reduction)));
                    if (!previous.Frame.TryReadResult(reduction, out var remaining, out _, out var unknown) ||
                        !N.Read(remaining, out var terms) || N.E(unknown).Length != 0)
                        check.Unknown.Add(new Relation(use, reduction));
                    else
                    {
                        var heading = terms.Cast<Relation>().Select(term => term.Left).Distinct(N.Participants.Instance).ToArray();
                        if (!N.Read(expectedHeading, out var directions)) check.Unknown.Add(phase);
                        else if (heading.Length != directions.Length ||
                            !heading.All(direction => directions.Any(expected => N.Same(direction, expected))))
                            check.Failures.Add(new Relation(new Relation(use, phase), N.S(heading)));
                    }
                }
                if (i + 1 < strand.Uses.Length && strand.Uses[i + 1].Right is Relation next &&
                    !N.Same(ends.Right, next.Left)) check.Failures.Add(new Relation(use, strand.Uses[i + 1]));
            }
            var links = N.E(strand.Continuations);
            var expectedLinkCount = strand.Uses.Length - 1 + (strand.IsClosed ? 1 : 0);
            if (!N.Read(strand.Continuations, out _) || links.Length != expectedLinkCount)
                check.Unknown.Add(strand.Continuations);
            foreach (var link in links)
            {
                if (link is not Relation { Left: Relation from, Right: Relation to } ||
                    !N.E(previous.PhaseCycle).OfType<Relation>().Any(law =>
                        ReferenceEquals(law.Left, from.Right) && ReferenceEquals(law.Right, to.Right)))
                    check.Failures.Add(new Relation(link, previous.PhaseCycle));
                else check.Proofs.Add(new Relation(link, previous.PhaseCycle));
            }
        });
    }

    /// <summary>Rechecks each previously witnessed translation separately. This
    /// says nothing about a complete new phase colouring or new neighbours.</summary>
    public static OrnamentCandidateRevalidation Rhythm(OrnamentOccurrenceRhythm previous,
        Relation bindings, IElement question, IElement? change = null)
    {
        var topology = new Relation(previous.Occurrences, new Relation(N.S(), N.S()));
        var state = new State(topology, bindings);
        return new(previous.Surface, previous.Successors, previous.Successors, topology, bindings, question, change, (item, check) =>
        {
            if (item is not Relation actual || !previous.TryReadSuccessor(actual, out var fromRole,
                out var toRole, out var translation, out var previousProof))
            { check.Unknown.Add(new Relation(item, previous.Successors)); return; }
            check.Proofs.Add(previousProof);
            var from = state.Word(fromRole, previous.Frame, check);
            var to = state.Word(toRole, previous.Frame, check);
            if (from is null || to is null) return;
            var difference = previous.Frame.Reduce((Relation)to.Right, Inverse(previous.Frame, (Relation)from.Right));
            check.Proofs.Add(new Relation(new Relation(from, to), difference));
            if (!previous.Frame.TryReadResult(difference, out var remaining, out _, out var unknown) ||
                !N.Read(remaining, out _) || N.E(unknown).Length != 0)
            { check.Unknown.Add(difference); return; }
            var match = previous.Frame.Reduce(remaining, Inverse(previous.Frame, translation));
            check.Proofs.Add(new Relation(translation, match));
            if (!previous.Frame.TryReadResult(match, out var residual, out _, out var unresolved) ||
                !N.Read(residual, out var terms) || N.E(unresolved).Length != 0)
                check.Unknown.Add(match);
            else if (terms.Length != 0) check.Failures.Add(new Relation(actual, new Relation(match, residual)));
        });
    }

    public Relation Evaluate() => Surface;

    /// <summary>Reads only an actual result row, never a lookalike wrapper.</summary>
    public bool TryReadWitness(Relation actual, out IElement occurrence, out Relation proofs,
        out Relation counterexamples, out Relation openObligations)
    {
        occurrence = null!; proofs = counterexamples = openObligations = null!;
        if (!N.E(Supported).Concat(N.E(Refuted)).Concat(N.E(Unresolved)).Any(row => ReferenceEquals(row, actual)) ||
            actual is not { Right: Relation { Left: Relation p, Right: Relation { Left: Relation c, Right: Relation u } } }) return false;
        occurrence = actual.Left; proofs = p; counterexamples = c; openObligations = u; return true;
    }

    private static Relation Inverse(QualitativeMotionFrame frame, Relation word) => N.S(N.E(word).Reverse().Cast<Relation>()
        .Select(term => (IElement)QualitativeMotionFrame.Move(N.E(frame.Opposites).Cast<Relation>()
            .Single(law => ReferenceEquals(law.Left, term.Left)).Right, term.Right)).ToArray());
    private static bool SameBoundary(IElement[] a, IElement[] b) => a.Length == b.Length && a.Zip(b).All(pair =>
        pair.First is Relation { Right: Relation x } first && pair.Second is Relation { Right: Relation y } second &&
        N.Same(first.Left, second.Left) && N.Same(x.Left, y.Left) && N.Same(x.Right, y.Right));
    private static bool Closed(Relation scope, Check check, out IElement[] rows)
    {
        if (N.Read(scope, out rows)) return true;
        check.Unknown.Add(scope); return false;
    }
    private sealed class Check
    {
        internal List<IElement> Proofs { get; } = [];
        internal List<IElement> Failures { get; } = [];
        internal List<IElement> Unknown { get; } = [];
    }
    private sealed class Scope(Relation surface)
    {
        internal Relation Surface { get; } = surface;
        internal IElement[] Entries { get; } = N.E(surface);
        internal bool IsClosed { get; } = N.Read(surface, out _);
        internal Relation? Match(IElement identity, Check check)
        {
            var matches = Entries.OfType<Relation>().Where(row => N.Same(row.Left, identity)).ToArray();
            if (matches.Length > 1) { check.Failures.Add(N.S(matches)); return null; }
            if (!IsClosed) check.Unknown.Add(new Relation(identity, Surface));
            if (matches is [var actual]) { check.Proofs.Add(actual); return actual; }
            if (IsClosed) check.Failures.Add(new Relation(identity, Surface));
            return null;
        }
    }
    private sealed class State
    {
        internal bool Ready { get; }
        internal Scope Sites { get; }
        internal Scope Edges { get; }
        internal Scope Faces { get; }
        internal Scope Bindings { get; }
        internal State(Relation topology, Relation bindings)
        {
            if (topology is { Left: Relation sites, Right: Relation { Left: Relation edges, Right: Relation faces } })
            { Ready = true; Sites = new(sites); Edges = new(edges); Faces = new(faces); }
            else { Sites = Edges = Faces = new(N.S()); }
            Bindings = new(bindings);
        }
        internal void Edge(IElement identity, Relation expectedEnds, Check check)
        {
            var active = Edges.Match(identity, check);
            if (active is null) return;
            if (active.Right is not Relation ends)
            { check.Unknown.Add(active); return; }
            if (!(N.Same(ends.Left, expectedEnds.Left) && N.Same(ends.Right, expectedEnds.Right) ||
                N.Same(ends.Left, expectedEnds.Right) && N.Same(ends.Right, expectedEnds.Left)))
                check.Failures.Add(new Relation(new Relation(identity, expectedEnds), active));
            foreach (var role in new[] { expectedEnds.Left, expectedEnds.Right })
            {
                var matches = Sites.Entries.Where(site => N.Same(site, role)).ToArray();
                if (matches.Length > 1 || matches.Length == 0 && Sites.IsClosed)
                    check.Failures.Add(new Relation(role, Sites.Surface));
                else if (!Sites.IsClosed) check.Unknown.Add(new Relation(role, Sites.Surface));
                else check.Proofs.Add(new Relation(role, Sites.Surface));
            }
        }
        internal Relation? Word(IElement role, QualitativeMotionFrame frame, Check check)
        {
            // Missing binding is uncertainty, not a false geometric heading.
            var matches = Bindings.Entries.OfType<Relation>().Where(row => N.Same(row.Left, role)).ToArray();
            if (!Bindings.IsClosed || matches is not [var binding] || binding.Right is not Relation word ||
                !N.Read(word, out var terms) || terms.Any(term => term is not Relation move ||
                    !frame.Direction(move, out _, out _, out _)))
            { check.Unknown.Add(new Relation(role, Bindings.Surface)); return null; }
            check.Proofs.Add(binding); return binding;
        }
    }
}
