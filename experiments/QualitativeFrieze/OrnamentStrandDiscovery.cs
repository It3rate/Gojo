using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A bounded observational route read over exposed incidence and
/// published formal motion words. The question supplies a cyclic heading
/// grammar, not locations, edge identities, seeds, or a preferred route.
/// Reach and multiplicity are deliberately forgotten by the heading read.
/// Unique local continuations grow strands; forks remain explicit alternatives.
/// This never identifies sites merely because their projected words agree.</summary>
public sealed class OrnamentStrandDiscovery
{
    // pattern = Closed(Closed(actual frame directions), ...)
    // phase = fresh occurrence -> actual pattern signature
    // phase-next = actual phase -> next actual phase (including last -> first)
    // edge-use = actual edge identity -> (actual from role -> actual to role)
    // edge-read = edge-use -> ((actual from binding -> actual to binding) ->
    //                         (actual reduction witness -> qualitative heading))
    // state = edge-use -> actual phase
    // accepted continuation = actual state -> actual next state
    // route = ((actual start role -> actual end role) -> Closed(edge-uses)) ->
    //         (Closed(actual states) -> Closed(actual accepted continuations))
    // stop = actual state -> (Closed(actual predecessors) -> Closed(actual successors))
    // capture = definition -> ((phase cycle -> edge reads) ->
    //           ((routes -> closed cycles) ->
    //            (continuations -> (competing -> (boundary -> (blocked -> unresolved))))))
    public QualitativeMotionFrame Frame { get; }
    public IElement Source { get; }
    public Relation SourceTopology { get; }
    public Relation SourceBindings { get; }
    public Relation Pattern { get; }
    public IElement Question { get; }
    public Relation Definition { get; }
    public Relation PhaseCycle { get; }
    public Relation EdgeReads { get; }
    public Relation Routes { get; }
    public Relation ClosedCycles { get; }
    public Relation Continuations { get; }
    public Relation Competing { get; }
    public Relation Boundary { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }
    public bool IsComplete => E(Blocked).Length == 0 && E(Unresolved).Length == 0;

    public OrnamentStrandDiscovery(QualitativeMotionFrame frame, IElement source,
        Relation topology, Relation bindings, Relation pattern, IElement question)
    {
        Frame = frame; Source = source; SourceTopology = topology;
        SourceBindings = bindings; Pattern = pattern; Question = question;
        Definition = new(new Relation(source, new Relation(topology, bindings)),
            new Relation(frame.Surface, new Relation(question, pattern)));
        var blocked = new List<IElement>(); var pending = new List<IElement>();
        var edgeReads = new List<IElement>(); var continuations = new List<IElement>();
        var competing = new List<IElement>(); var boundary = new List<IElement>();
        var routes = new List<IElement>(); var cycles = new List<IElement>();
        var phases = new List<Phase>();
        if (!Closed(pattern, out var signatures) || signatures.Length == 0)
            pending.Add(new Relation(question, pattern));
        else foreach (var entry in signatures)
        {
            if (entry is not Relation signature || !Closed(signature, out var directions) ||
                directions.Length == 0 || !N.Distinct(directions) || !ValidDirections(directions))
            { blocked.Add(new Relation(entry, frame.Surface)); continue; }
            phases.Add(new(new Relation(new Placeholder(), signature), directions));
        }
        PhaseCycle = S(phases.Select((phase, index) => (IElement)new Relation(phase.Read,
            phases[(index + 1) % phases.Count].Read)).ToArray());

        var sites = Array.Empty<IElement>(); var edges = Array.Empty<IElement>();
        var faces = Array.Empty<IElement>(); var bindingRows = Array.Empty<IElement>();
        var knownScope = topology.Left is Relation siteScope && Closed(siteScope, out sites) &&
            topology.Right is Relation { Left: Relation edgeScope, Right: Relation faceScope } &&
            Closed(edgeScope, out edges) && Closed(faceScope, out faces) && Closed(bindings, out bindingRows);
        if (!knownScope) pending.Add(new Relation(topology, bindings));
        else if (!N.Distinct(sites) || !N.Distinct(edges) || !N.Distinct(faces)) blocked.Add(topology);
        var words = new Dictionary<IElement, (Relation Binding, Relation Word)>(N.Participants.Instance);
        if (knownScope)
        {
            foreach (var site in sites)
            {
                var actual = bindingRows.OfType<Relation>().Where(row => N.Same(row.Left, site)).ToArray();
                if (actual is not [var binding] || binding.Right is not Relation word || !Closed(word, out var moves) ||
                    moves.Any(item => item is not Relation move || !frame.Direction(move, out _, out _, out _)))
                { pending.Add(new Relation(site, bindings)); continue; }
                // A blocked duplicate participant scope must stay inspectable,
                // including aliases of the same placeholder, rather than
                // escaping as a dictionary exception during this local read.
                words.TryAdd(site, (binding, word));
            }
            foreach (var row in bindingRows)
                if (row is not Relation binding || !sites.Any(site => N.Same(site, binding.Left)))
                    blocked.Add(new Relation(row, topology));
        }
        var orientations = new List<Orientation>(); var identities = new List<IElement>();
        foreach (var item in edges)
        {
            if (item is not Relation { Right: Relation ends } row ||
                !sites.Any(site => N.Same(site, ends.Left)) || !sites.Any(site => N.Same(site, ends.Right)) ||
                N.Same(ends.Left, ends.Right) || identities.Any(identity => N.Same(identity, row.Left)))
            { blocked.Add(new Relation(item, topology)); continue; }
            identities.Add(row.Left);
            ReadOrientation(row.Left, ends.Left, ends.Right);
            ReadOrientation(row.Left, ends.Right, ends.Left);
        }

        if (blocked.Count == 0 && pending.Count == 0)
        {
            var states = orientations.SelectMany(use => phases.Where(phase => SameHeading(use.Directions, phase.Directions))
                .Select(phase => new State(use, phase))).ToArray();
            foreach (var state in states)
            {
                var nextPhase = phases[(phases.IndexOf(state.Phase) + 1) % phases.Count];
                state.Next = states.Where(candidate => ReferenceEquals(candidate.Phase, nextPhase) &&
                    N.Same(state.Use.To, candidate.Use.From) && !N.Same(state.Use.Identity, candidate.Use.Identity)).ToArray();
            }
            foreach (var state in states)
            {
                state.Previous = states.Where(candidate => candidate.Next.Any(next => ReferenceEquals(next, state))).ToArray();
                foreach (var next in state.Next)
                    continuations.Add(new Relation(state.Read, next.Read));
                if (state.Next.Length > 1 || state.Previous.Length > 1)
                    competing.Add(Stop(state));
                if (state.Next.Length == 0 || state.Previous.Length == 0)
                    boundary.Add(Stop(state));
            }
            // Stop before an arbitrary branch choice. The remaining graph is a
            // union of chains and cycles; each state is emitted once, so the
            // retained read is bounded by edge uses times declared phases.
            var visited = new HashSet<State>(ReferenceEqualityComparer.Instance);
            foreach (var state in states.Where(state => state.Previous.Length != 1 || state.Previous[0].Next.Length != 1))
                Follow(state);
            foreach (var state in states) Follow(state);

            void Follow(State first)
            {
                if (visited.Contains(first)) return;
                var chain = new List<State>(); var at = first;
                while (visited.Add(at))
                {
                    chain.Add(at);
                    if (at.Next.Length != 1 || at.Next[0].Previous.Length != 1) break;
                    at = at.Next[0];
                }
                var links = chain.Zip(chain.Skip(1), (a, b) => (IElement)new Relation(a.Read, b.Read)).ToList();
                var closed = chain[^1].Next.Length == 1 && ReferenceEquals(chain[^1].Next[0], first) && first.Previous.Length == 1;
                if (closed) links.Add(new Relation(chain[^1].Read, first.Read));
                var route = new Relation(new Relation(new Relation(chain[0].Use.From, chain[^1].Use.To),
                    S(chain.Select(state => (IElement)state.Use.Read).ToArray())),
                    new Relation(S(chain.Select(state => (IElement)state.Read).ToArray()), S(links.ToArray())));
                routes.Add(route); if (closed) cycles.Add(route);
            }
        }
        EdgeReads = S(edgeReads.ToArray()); Routes = S(routes.ToArray()); ClosedCycles = S(cycles.ToArray());
        Continuations = S(continuations.ToArray()); Competing = S(competing.ToArray());
        Boundary = S(boundary.ToArray()); Blocked = S(blocked.ToArray()); Unresolved = S(pending.ToArray());
        Surface = new(Definition, new Relation(new Relation(PhaseCycle, EdgeReads),
            new Relation(new Relation(Routes, ClosedCycles), new Relation(Continuations,
                new Relation(Competing, new Relation(Boundary, new Relation(Blocked, Unresolved)))))));

        void ReadOrientation(IElement identity, IElement from, IElement to)
        {
            if (!words.TryGetValue(from, out var a) || !words.TryGetValue(to, out var b)) return;
            var use = new Relation(identity, new Relation(from, to));
            var reduced = frame.Reduce(b.Word, Inverse(frame, a.Word));
            if (!frame.TryReadResult(reduced, out var residual, out _, out var unknown) ||
                !Closed(residual, out var moves) || E(unknown).Length != 0)
            { pending.Add(new Relation(use, reduced)); return; }
            var heading = moves.Cast<Relation>().Select(move => move.Left).Distinct(N.Participants.Instance).ToArray();
            var read = new Relation(use, new Relation(new Relation(a.Binding, b.Binding), new Relation(reduced, S(heading))));
            edgeReads.Add(read); orientations.Add(new(identity, from, to, use, heading));
        }

        bool ValidDirections(IElement[] directions)
        {
            var axes = new List<Relation>();
            foreach (var direction in directions)
            {
                if (!frame.Direction(QualitativeMotionFrame.Move(direction, new Placeholder()), out var axis, out _, out _) ||
                    axes.Any(actual => ReferenceEquals(actual, axis))) return false;
                axes.Add(axis);
            }
            return true;
        }
    }

    public bool TryReadRoute(Relation actual, out OrnamentDiscoveredStrand strand)
    {
        strand = null!;
        if (!E(Routes).Any(row => ReferenceEquals(row, actual)) ||
            actual.Left is not Relation { Left: Relation ends, Right: Relation uses } ||
            actual.Right is not Relation { Left: Relation states, Right: Relation links } || !Closed(uses, out var entries)) return false;
        strand = new(actual, ends.Left, ends.Right, entries.Cast<Relation>().ToArray(), states, links,
            E(ClosedCycles).Any(row => ReferenceEquals(row, actual)));
        return true;
    }

    private static Relation Stop(State state) => new(state.Read,
        new Relation(S(state.Previous.Select(prior => (IElement)prior.Read).ToArray()),
            S(state.Next.Select(next => (IElement)next.Read).ToArray())));
    private static bool SameHeading(IElement[] a, IElement[] b) => a.Length == b.Length &&
        a.All(item => b.Any(other => N.Same(item, other)));
    private static Relation Inverse(QualitativeMotionFrame frame, Relation word) => S(E(word).Reverse().Cast<Relation>().Select(move =>
        (IElement)QualitativeMotionFrame.Move(E(frame.Opposites).Cast<Relation>().Single(row => ReferenceEquals(row.Left, move.Left)).Right, move.Right)).ToArray());
    private static bool Closed(Relation scope, out IElement[] entries) => StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private sealed class Phase(Relation read, IElement[] directions)
    {
        public Relation Read { get; } = read;
        public IElement[] Directions { get; } = directions;
    }
    private sealed record Orientation(IElement Identity, IElement From, IElement To, Relation Read, IElement[] Directions);
    private sealed class State(Orientation use, Phase phase)
    {
        public Orientation Use { get; } = use;
        public Phase Phase { get; } = phase;
        public Relation Read { get; } = new(use.Read, phase.Read);
        public State[] Next { get; set; } = [];
        public State[] Previous { get; set; } = [];
    }
}

/// <summary>A decoded native route; its ordered uses remain actual source
/// edge identities with actual oriented endpoints, never renderer routing.</summary>
public sealed class OrnamentDiscoveredStrand(Relation read, IElement start, IElement end,
    Relation[] uses, Relation states, Relation continuations, bool isClosed)
{
    public Relation Read { get; } = read;
    public IElement Start { get; } = start;
    public IElement End { get; } = end;
    public Relation[] Uses { get; } = uses;
    public Relation States { get; } = states;
    public Relation Continuations { get; } = continuations;
    public bool IsClosed { get; } = isClosed;
}
