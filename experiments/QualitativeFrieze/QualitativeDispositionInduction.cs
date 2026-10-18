using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Bounded induction of qualitative flow channels and their local
/// arrangements from a history-blind segment observation. Heading agreement
/// does not claim a metric straight line. Actual site identity, rather than a
/// projected location, closes a route or commuting circuit.</summary>
public sealed class QualitativeDispositionInduction
{
    // use = actual observed segment -> ((actual from -> actual to) -> heading)
    // family = heading -> opposite heading
    // strand = ((start -> end) -> Closed(actual uses)) -> Closed(next-use links)
    // rhythm = ((definition -> Closed(headings)) -> actual phase cycle) ->
    //          (routes -> (continuations -> (competing ->
    //          (selected segments -> (residue -> boundary)))))
    // candidate = (definition -> Closed(actual channels)) ->
    //   (selected segments -> (channel strands -> (commutations ->
    //   (closed triangles -> (unexpected triangles -> (boundary ->
    //   (competing -> (residue -> unsettled obligations)))))))))
    // A commutation retains two actual two-step paths from the same role.
    // An absent continuation is an open finite-boundary obligation; it is not
    // evidence that an unobserved outside segment should exist.
    // Alternatives do not consume the observed material. Residue is a read
    // remainder, not invented physical carrier tension.
    public QualitativeRecognitionObservation Observation { get; }
    public Relation Budget { get; }
    public IElement Question { get; }
    public Relation Definition { get; }
    public Relation DirectionFamilies { get; }
    public Relation StrandFamilies { get; }
    public Relation TriangleCircuits { get; }
    public Relation Rhythms { get; }
    public Relation Candidates { get; }
    public Relation Spent { get; }
    public Relation Unsearched { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }
    public bool IsComplete => N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0 && N.E(Unsearched).Length == 0;

    public QualitativeDispositionInduction(QualitativeRecognitionObservation observation, int capacity = 128)
        : this(observation, BudgetSlots(capacity), new Placeholder()) { }

    public QualitativeDispositionInduction(QualitativeRecognitionObservation observation, Relation budget, IElement question)
    {
        Observation = observation; Budget = budget; Question = question;
        Definition = new(new Relation(observation.Surface, question), budget);
        var blocked = new List<IElement>(); var pending = new List<IElement>();
        pending.AddRange(N.E(observation.Unresolved));
        var spent = new List<IElement>(); var unsearched = new List<IElement>();
        var candidates = new List<IElement>(); var rhythms = new List<IElement>();
        var families = new List<Family>(); var uses = new List<Use>();
        var slots = Array.Empty<IElement>();
        if (!N.Read(budget, out slots) || !N.Distinct(slots) || slots.Any(slot => slot is not Placeholder))
        { blocked.Add(budget); slots = []; }
        if (!N.Read(observation.Sites, out _) || !N.Read(observation.Segments, out _))
            pending.Add(new Relation(observation.Sites, observation.Segments));
        var duplicateSites = N.E(observation.Sites).GroupBy(site => site, N.Participants.Instance)
            .Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        foreach (var site in duplicateSites) blocked.Add(new Relation(site, observation.Sites));
        foreach (var item in N.E(observation.Segments))
        {
            if (item is not Relation row || !observation.TryReadSegment(row, out _, out var from, out var to, out var heading))
            { blocked.Add(item); continue; }
            if (duplicateSites.Any(site => N.Same(site, from) || N.Same(site, to)))
            { blocked.Add(row); continue; }
            if (!N.Read(heading, out var directions) || directions.Length == 0)
            { pending.Add(new Relation(row, heading)); continue; }
            var inverse = S(directions.Select(Opposite).ToArray());
            var family = families.FirstOrDefault(other => SameHeading(other.Forward, heading) || SameHeading(other.Reverse, heading));
            if (family is null) { family = new(heading, inverse); families.Add(family); }
            uses.Add(new(row, from, to, heading, family));
            uses.Add(new(row, to, from, inverse, family));
        }
        // Duplicate source identities cannot become independent support.
        var duplicateRows = uses.Where((use, index) => index % 2 == 0)
            .GroupBy(use => use.Row.Left, N.Participants.Instance).Where(group => group.Count() > 1)
            .SelectMany(group => group).Select(use => use.Row).ToArray();
        if (duplicateRows.Length != 0)
        {
            foreach (var row in duplicateRows) blocked.Add(row);
            uses.RemoveAll(use => duplicateRows.Any(row => ReferenceEquals(row, use.Row)));
        }
        families.RemoveAll(family => !uses.Any(use => ReferenceEquals(use.Family, family)));
        DirectionFamilies = S(families.Select(family => (IElement)family.Read).ToArray());
        var familyPaths = new Dictionary<Family, Relation[]>(ReferenceEqualityComparer.Instance);
        var allStrands = new List<IElement>();
        foreach (var family in families)
        {
            var states = uses.Where(use => SameHeading(use.Heading, family.Forward)).Select(use => new State(use, 0)).ToArray();
            var walk = Walk(states, _ => 0, 1);
            familyPaths.Add(family, walk.Routes);
            allStrands.Add(new Relation(family.Read, new Relation(S(walk.Routes),
                new Relation(S(walk.Links), new Relation(S(walk.Competing), S(walk.Boundary))))));
        }
        StrandFamilies = S(allStrands.ToArray());
        var triangles = Triangles(uses);
        TriangleCircuits = S(triangles.Select(triangle => (IElement)triangle.Read).ToArray());

        // Propose a cyclic two-heading grammar only after witnessing its local
        // transition. Both cyclic phases belong to the same grammar.
        var signatures = new List<Relation>();
        foreach (var use in uses) if (!signatures.Any(signature => SameHeading(signature, use.Heading))) signatures.Add(use.Heading);
        var rhythmChannels = new List<Channel>();
        for (var a = 0; a < signatures.Count; a++)
            for (var b = a + 1; b < signatures.Count; b++)
            {
                var first = signatures[a]; var second = signatures[b];
                if (SameHeading(first, OppositeHeading(second)) ||
                    !uses.Any(x => SameHeading(x.Heading, first) && uses.Any(y =>
                        !ReferenceEquals(x.Row, y.Row) && N.Same(x.To, y.From) && SameHeading(y.Heading, second)))) continue;
                var grammar = S(first, second); var proposal = new Relation(Definition, grammar);
                if (!Take(proposal, out var slot)) continue;
                var states = uses.Where(use => SameHeading(use.Heading, first) || SameHeading(use.Heading, second))
                    .Select(use => new State(use, SameHeading(use.Heading, first) ? 0 : 1)).ToArray();
                var walk = Walk(states, state => 1 - state.Phase, 4);
                var phaseA = new Relation(new Placeholder(), first); var phaseB = new Relation(new Placeholder(), second);
                var phaseCycle = new Relation(S(new Relation(phaseA, phaseB), new Relation(phaseB, phaseA)),
                    S(states.Select(state => (IElement)new Relation(state.Read, state.Phase == 0 ? phaseA : phaseB)).ToArray()));
                var selected = DistinctRows(walk.Routes.SelectMany(RouteUses).Select(read => (Relation)read.Left));
                var residue = N.E(observation.Segments).Where(row => !selected.Any(chosen => ReferenceEquals(row, chosen))).ToArray();
                var result = new Relation(new Relation(proposal, phaseCycle), new Relation(S(walk.Routes),
                    new Relation(S(walk.Links), new Relation(S(walk.Competing), new Relation(S(selected),
                        new Relation(S(residue), S(walk.Boundary)))))));
                spent.Add(new Relation(slot, result));
                if (walk.Routes.Length == 0) continue;
                rhythms.Add(result);
                var participating = families.Where(family => SameHeading(family.Forward, first) || SameHeading(family.Reverse, first) ||
                    SameHeading(family.Forward, second) || SameHeading(family.Reverse, second)).ToArray();
                rhythmChannels.Add(new(result, [first, second], participating, walk.Routes));
            }
        Rhythms = S(rhythms.ToArray());

        // Every two-family and three-family basis remains available. There is
        // no hard-coded noun winner and unused directions stay as actual rows.
        for (var size = 2; size <= Math.Min(3, families.Count); size++)
            foreach (var subset in Combinations(families, size))
                Propose(subset.Select(family => new Channel(family.Read, [family.Forward], [family], familyPaths[family])).ToArray());
        // A periodic channel can account for alternating row transport without
        // pretending its two headings are one exact straight direction.
        foreach (var rhythm in rhythmChannels)
            foreach (var family in families.Where(family => !rhythm.Families.Contains(family)))
                Propose([new(family.Read, [family.Forward], [family], familyPaths[family]), rhythm]);

        Candidates = S(candidates.ToArray()); Spent = S(spent.ToArray()); Unsearched = S(unsearched.ToArray());
        Blocked = S(blocked.Distinct(N.Participants.Instance).ToArray()); Unresolved = S(pending.Distinct(N.Participants.Instance).ToArray());
        Surface = new(Definition, new Relation(DirectionFamilies, new Relation(StrandFamilies,
            new Relation(TriangleCircuits, new Relation(Rhythms, new Relation(Candidates,
                new Relation(Spent, new Relation(Unsearched, new Relation(Blocked, Unresolved)))))))));

        bool Take(Relation proposal, out IElement slot)
        {
            if (spent.Count >= slots.Length)
            { slot = null!; unsearched.Add(new Relation(proposal, new Placeholder())); return false; }
            slot = slots[spent.Count]; return true;
        }

        void Propose(Channel[] channels)
        {
            var basis = S(channels.Select(channel => channel.Read).ToArray());
            var proposal = new Relation(Definition, basis);
            if (!Take(proposal, out var slot)) return;
            var admitted = channels.SelectMany(channel => channel.Families).Distinct().ToArray();
            var selected = DistinctRows(uses.Where(use => admitted.Contains(use.Family)).Select(use => use.Row));
            var residue = N.E(observation.Segments).Where(row => !selected.Any(chosen => ReferenceEquals(row, chosen))).ToArray();
            var commutations = new List<IElement>(); var boundary = new List<IElement>();
            var competing = new List<IElement>(); var unsettled = new List<IElement>();
            for (var a = 0; a < channels.Length; a++)
                for (var b = a + 1; b < channels.Length; b++)
                    foreach (var site in N.E(observation.Sites))
                    {
                        var aa = Out(site, channels[a]); var bb = Out(site, channels[b]);
                        if (aa.Length > 1 || bb.Length > 1)
                        { competing.Add(new Relation(site, new Relation(S(aa.Select(use => (IElement)use.Read).ToArray()), S(bb.Select(use => (IElement)use.Read).ToArray())))); continue; }
                        if (aa is not [var x] || bb is not [var y]) continue;
                        var afterX = Out(x.To, channels[b]).Where(use => SameHeading(use.Heading, y.Heading)).ToArray();
                        var afterY = Out(y.To, channels[a]).Where(use => SameHeading(use.Heading, x.Heading)).ToArray();
                        var obligation = new Relation(new Relation(x.Read, y.Read),
                            new Relation(S(afterX.Select(use => (IElement)use.Read).ToArray()), S(afterY.Select(use => (IElement)use.Read).ToArray())));
                        if (afterX.Length > 1 || afterY.Length > 1) { competing.Add(obligation); continue; }
                        if (afterX is not [var xx] || afterY is not [var yy])
                        { boundary.Add(new Relation(obligation, new Placeholder())); continue; }
                        var proof = new Relation(site, new Relation(S(x.Read, xx.Read), S(y.Read, yy.Read)));
                        if (N.Same(xx.To, yy.To)) commutations.Add(proof);
                        else unsettled.Add(new Relation(proof, new Relation(xx.To, yy.To)));
                    }
            var closed = triangles.Where(triangle => triangle.Uses.All(use => admitted.Contains(use.Family))).Select(triangle => (IElement)triangle.Read).ToArray();
            var unexpected = triangles.Where(triangle => triangle.Uses.Any(use => admitted.Contains(use.Family)) &&
                triangle.Uses.Any(use => !admitted.Contains(use.Family))).Select(triangle => (IElement)triangle.Read).ToArray();
            var row = new Relation(proposal, new Relation(S(selected), new Relation(
                S(channels.Select(channel => (IElement)new Relation(channel.Read, S(channel.Paths))).ToArray()),
                new Relation(S(commutations.ToArray()), new Relation(S(closed), new Relation(S(unexpected),
                    new Relation(S(boundary.ToArray()), new Relation(S(competing.ToArray()),
                        new Relation(S(residue), S(unsettled.ToArray()))))))))));
            candidates.Add(row); spent.Add(new Relation(slot, row));
        }

        Use[] Out(IElement site, Channel channel) => uses.Where(use => N.Same(use.From, site) &&
            channel.Headings.Any(heading => SameHeading(heading, use.Heading))).ToArray();
        IElement Opposite(IElement direction) => N.E(observation.Frame.Opposites).Cast<Relation>()
            .Single(pair => ReferenceEquals(pair.Left, direction)).Right;
        Relation OppositeHeading(Relation heading) => S(N.E(heading).Select(Opposite).ToArray());
    }

    public Relation Evaluate() => Surface;

    public bool TryReadCandidate(Relation actual, out QualitativeDispositionCandidate candidate)
    {
        candidate = null!;
        if (!N.E(Candidates).Any(row => ReferenceEquals(row, actual)) ||
            actual is not { Left: Relation { Left: var definition, Right: Relation basis },
                Right: Relation { Left: Relation selected, Right: Relation { Left: Relation strands,
                Right: Relation { Left: Relation commutations, Right: Relation { Left: Relation closures,
                Right: Relation { Left: Relation unexpected, Right: Relation { Left: Relation boundary,
                Right: Relation { Left: Relation competing, Right: Relation { Left: Relation residue,
                Right: Relation unsettled } } } } } } } } } || !ReferenceEquals(definition, Definition)) return false;
        candidate = new(actual, basis, selected, strands, commutations, closures, unexpected, boundary, competing, residue, unsettled);
        return true;
    }

    public bool TryReadRhythm(Relation actual, out QualitativeDispositionRhythm rhythm)
    {
        rhythm = null!;
        if (!N.E(Rhythms).Any(row => ReferenceEquals(row, actual)) ||
            actual is not { Left: Relation { Left: Relation { Left: var definition, Right: Relation headings },
                Right: Relation phases }, Right: Relation { Left: Relation routes,
                Right: Relation { Left: Relation continuations, Right: Relation { Left: Relation competing,
                Right: Relation { Left: Relation selected, Right: Relation { Left: Relation residue,
                Right: Relation boundary } } } } } } || !ReferenceEquals(definition, Definition)) return false;
        rhythm = new(actual, headings, phases, routes, continuations, competing, selected, residue, boundary); return true;
    }

    /// <summary>Decode the actual ordered uses of a retained route.</summary>
    public static Relation[] RouteUses(Relation route) => route.Left is Relation { Right: Relation uses } ? N.E(uses).OfType<Relation>().ToArray() : [];
    private static Relation BudgetSlots(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        return S(Enumerable.Range(0, capacity).Select(_ => (IElement)new Placeholder()).ToArray());
    }
    private static bool SameHeading(Relation a, Relation b)
    {
        var aa = N.E(a); var bb = N.E(b);
        return aa.Length == bb.Length && aa.All(direction => bb.Any(other => ReferenceEquals(direction, other)));
    }
    private static Relation[] DistinctRows(IEnumerable<Relation> rows) => rows.Distinct(ReferenceEqualityComparer.Instance).Cast<Relation>().ToArray();
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);

    private static WalkResult Walk(State[] states, Func<State, int> nextPhase, int minimum)
    {
        foreach (var state in states)
            state.Next = states.Where(next => next.Phase == nextPhase(state) &&
                N.Same(state.Use.To, next.Use.From) && !ReferenceEquals(state.Use.Row, next.Use.Row)).ToArray();
        foreach (var state in states) state.Previous = states.Where(other => other.Next.Contains(state)).ToArray();
        var links = states.SelectMany(state => state.Next.Select(next => (IElement)new Relation(state.Read, next.Read))).ToArray();
        var competing = states.Where(state => state.Next.Length > 1 || state.Previous.Length > 1)
            .Select(state => (IElement)new Relation(state.Read, new Relation(S(state.Previous.Select(other => (IElement)other.Read).ToArray()),
                S(state.Next.Select(other => (IElement)other.Read).ToArray())))).ToArray();
        var boundary = states.Where(state => state.Previous.Length == 0 || state.Next.Length == 0)
            .Select(state => (IElement)new Relation(state.Read, new Relation(S(state.Previous.Select(other => (IElement)other.Read).ToArray()),
                S(state.Next.Select(other => (IElement)other.Read).ToArray())))).ToArray();
        var visited = new HashSet<State>(ReferenceEqualityComparer.Instance); var routes = new List<Relation>();
        foreach (var start in states.Where(state => state.Previous.Length != 1 || state.Previous[0].Next.Length != 1)
            .Concat(states))
        {
            if (visited.Contains(start)) continue;
            var walk = new List<State>(); var current = start;
            do
            {
                if (!visited.Add(current)) break;
                walk.Add(current);
                if (current.Next.Length != 1 || current.Next[0].Previous.Length != 1) break;
                current = current.Next[0];
            } while (!ReferenceEquals(current, start));
            if (walk.Count < minimum) continue;
            var last = walk[^1];
            var accepted = walk.Zip(walk.Skip(1)).Select(pair => (IElement)new Relation(pair.First.Read, pair.Second.Read)).ToList();
            if (last.Next.Contains(start)) accepted.Add(new Relation(last.Read, start.Read));
            routes.Add(new(new Relation(new Relation(start.Use.From, last.Use.To), S(walk.Select(state => (IElement)state.Use.Read).ToArray())),
                S(accepted.ToArray())));
        }
        return new(routes.ToArray(), links, competing, boundary);
    }

    private static Triangle[] Triangles(List<Use> uses)
    {
        var rows = DistinctRows(uses.Select(use => use.Row));
        var result = new List<Triangle>(); var keys = new HashSet<string>();
        foreach (var a in uses)
            foreach (var b in uses.Where(use => N.Same(a.To, use.From) && !ReferenceEquals(a.Row, use.Row)))
                foreach (var c in uses.Where(use => N.Same(b.To, use.From) && N.Same(use.To, a.From) &&
                    !ReferenceEquals(a.Row, use.Row) && !ReferenceEquals(b.Row, use.Row)))
                {
                    var indices = new[] { Array.IndexOf(rows, a.Row), Array.IndexOf(rows, b.Row), Array.IndexOf(rows, c.Row) };
                    Array.Sort(indices); if (!keys.Add(string.Join(",", indices))) continue;
                    result.Add(new(S(a.Read, b.Read, c.Read), [a, b, c]));
                }
        return result.ToArray();
    }

    private static IEnumerable<Family[]> Combinations(List<Family> items, int size, int start = 0)
    {
        if (size == 0) { yield return []; yield break; }
        for (var i = start; i <= items.Count - size; i++)
            foreach (var tail in Combinations(items, size - 1, i + 1)) yield return [items[i], .. tail];
    }
    private sealed class Family(Relation forward, Relation reverse)
    { public Relation Forward { get; } = forward; public Relation Reverse { get; } = reverse; public Relation Read { get; } = new(forward, reverse); }
    private sealed class Use(Relation row, IElement from, IElement to, Relation heading, Family family)
    {
        public Relation Row { get; } = row; public IElement From { get; } = from; public IElement To { get; } = to;
        public Relation Heading { get; } = heading; public Family Family { get; } = family;
        public Relation Read { get; } = new(row, new Relation(new Relation(from, to), heading));
    }
    private sealed class State(Use use, int phase)
    {
        public Use Use { get; } = use; public int Phase { get; } = phase;
        public Relation Read { get; } = new(use.Read, use.Heading);
        public State[] Next { get; set; } = []; public State[] Previous { get; set; } = [];
    }
    private sealed class Channel(Relation read, Relation[] headings, Family[] families, Relation[] paths)
    { public Relation Read { get; } = read; public Relation[] Headings { get; } = headings; public Family[] Families { get; } = families; public Relation[] Paths { get; } = paths; }
    private sealed class Triangle(Relation read, Use[] uses)
    { public Relation Read { get; } = read; public Use[] Uses { get; } = uses; }
    private sealed record WalkResult(Relation[] Routes, IElement[] Links, IElement[] Competing, IElement[] Boundary);
}

/// <summary>Projection over one native disposition candidate. Counts and names
/// are deliberately absent; evidence, basis and remaining participation are the
/// definition.</summary>
public sealed class QualitativeDispositionCandidate(Relation read, Relation basis, Relation selectedSegments,
    Relation strands, Relation commutations, Relation closures, Relation unexpectedClosures,
    Relation boundary, Relation competing, Relation residue, Relation unsettled)
{
    public Relation Read { get; } = read;
    public Relation Basis { get; } = basis;
    public Relation SelectedSegments { get; } = selectedSegments;
    public Relation Strands { get; } = strands;
    public Relation Commutations { get; } = commutations;
    public Relation Closures { get; } = closures;
    public Relation UnexpectedClosures { get; } = unexpectedClosures;
    public Relation Boundary { get; } = boundary;
    public Relation Competing { get; } = competing;
    public Relation Residue { get; } = residue;
    public Relation Unsettled { get; } = unsettled;
}

public sealed class QualitativeDispositionRhythm(Relation read, Relation headings, Relation phases,
    Relation routes, Relation continuations, Relation competing, Relation selectedSegments, Relation residue, Relation boundary)
{
    public Relation Read { get; } = read;
    public Relation Headings { get; } = headings;
    public Relation Phases { get; } = phases;
    public Relation Routes { get; } = routes;
    public Relation Continuations { get; } = continuations;
    public Relation Competing { get; } = competing;
    public Relation SelectedSegments { get; } = selectedSegments;
    public Relation Residue { get; } = residue;
    public Relation Boundary { get; } = boundary;
}
