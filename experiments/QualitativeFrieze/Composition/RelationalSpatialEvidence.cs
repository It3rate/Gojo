using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Projection equalities and directed order facts, without an authored
/// path order. Ray pairing is a separate hypothesis. A finite local quotient
/// graph constructs ordinal layouts and retains every invented tie or order as
/// an assumption; it never consults source passages or position words.</summary>
public static class RelationalSpatialEvidence
{
    // spatial = (frame -> Closed(roles)) -> (horizontal -> vertical)
    // axis = selected projection -> (Closed(unordered equality groups) ->
    //                              Closed(directed before pairs))
    // Projection equality does not create a common physical site. Scope order
    // serializes facts only; it is not an additional path or ordering fact.
    public sealed class Read
    {
        public QualitativeMotionFrame Frame { get; }
        public Relation Roles { get; }
        public Relation Horizontal { get; }
        public Relation Vertical { get; }
        public Relation Surface { get; }
        internal Read(QualitativeMotionFrame frame, Relation roles, Relation h, Relation v, Relation? surface = null)
        { Frame = frame; Roles = roles; Horizontal = h; Vertical = v; Surface = surface ?? new(new Relation(frame.Surface, roles), new Relation(h, v)); }
    }
    public sealed record Inference(JointShapeDescription.Message? Layout, Relation Witness,
        Relation Contradictions, Relation Assumptions, bool Complete);
    public sealed record Assessment(Relation Supported, Relation Contradicted, Relation Open, Relation Surface);

    public static Read Analyze(JointShapeDescription.Message full) => new(full.Frame, full.Roles,
        FromBuckets(full.Horizontal), FromBuckets(full.Vertical));

    public static Read Select(Read full, JointShapeDescription.Message message, string profile)
    {
        if (!SameRoles(full.Roles, message.Roles) || !ReferenceEquals(full.Frame, message.Frame))
            throw new ArgumentException("Spatial publication must retain its exposed joint-role and frame correspondence.", nameof(message));
        if (profile is not ("observed" or "sparse" or "none")) throw new ArgumentException("Unknown spatial publication policy.", nameof(profile));
        Relation SelectAxis(Relation axis)
        {
            var body = (Relation)axis.Right;
            var equal = profile == "none" ? S() : (Relation)body.Left;
            var before = profile == "observed" ? (Relation)body.Right : profile == "none" ? S()
                : S(E((Relation)body.Right).Where((_, index) => index % 3 == 0).ToArray());
            return new(axis.Left, new Relation(equal, before));
        }
        return new(message.Frame, message.Roles, SelectAxis(full.Horizontal), SelectAxis(full.Vertical));
    }

    public static bool TryRead(Relation surface, JointShapeDescription.Message message, out Read read, out Relation witness)
    {
        read = null!; witness = new(surface, StructuralConstraint.Open());
        if (surface is not { Left: Relation { Left: Relation rawFrame, Right: Relation rawRoles } } ||
            !SameRoles(rawRoles, message.Roles) || !QualitativeMotionFrameRead.TryReadStandard(rawFrame, out _)) return false;
        var actual = surface;
        var correspondence = S();
        if (!ReferenceEquals(rawFrame, message.Frame.Surface) && !Reframe(surface, rawFrame, message.Frame.Surface, out actual, out correspondence)) return false;
        if (actual is not { Left: Relation { Left: Relation frame, Right: Relation roles },
            Right: Relation { Left: Relation h, Right: Relation v } } ||
            !frame.Equals(message.Frame.Surface) || !SameRoles(roles, message.Roles) ||
            !ReferenceEquals(h.Left, message.Frame.Horizontal) || !ReferenceEquals(v.Left, message.Frame.Vertical) ||
            !Axis(h, message.Roles) || !Axis(v, message.Roles)) return false;
        read = new(message.Frame, roles, h, v, actual); witness = new(new Relation(surface, read.Surface), correspondence); return true;
    }

    public static Inference Infer(JointShapeDescription.Message receiverWithOpenOrders, Read spatial,
        Relation proposedRayPairs, int variant)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(variant);
        var message = receiverWithOpenOrders;
        var assumptions = new List<IElement>(); var contradictions = new List<IElement>();
        if (!SameRoles(message.Roles, spatial.Roles) || !ReferenceEquals(message.Frame, spatial.Frame))
            return Finish(null, [new Relation(message.Roles, spatial.Roles)], []);
        var roles = E(message.Roles); var rays = new Dictionary<IElement, (IElement Role, Relation Heading)>(FieldNative.Participants.Instance);
        foreach (var joint in message.JointReads)
            foreach (var row in E(joint.Rays).Cast<Relation>()) rays.Add(row.Left, (joint.Role, (Relation)row.Right));
        var hEqual = new List<Relation>(); var vEqual = new List<Relation>();
        var hBefore = new List<Relation>(); var vBefore = new List<Relation>();
        if (!Closed(proposedRayPairs)) assumptions.Add(proposedRayPairs);
        foreach (var entry in E(proposedRayPairs))
        {
            if (entry is not Relation pair || !rays.TryGetValue(pair.Left, out var a) || !rays.TryGetValue(pair.Right, out var b) ||
                ReferenceEquals(a.Role, b.Role)) { contradictions.Add(new Relation(proposedRayPairs, entry)); continue; }
            var sa = Sector(message.Frame, a.Heading); var sb = Sector(message.Frame, b.Heading);
            if (!Closed(a.Heading) || !Closed(b.Heading) || sa < 0 || sb < 0)
            { assumptions.Add(new Relation(pair, StructuralConstraint.Open())); continue; }
            if ((sa + 4) % 8 != sb)
            { contradictions.Add(new Relation(pair, new Relation(a.Heading, b.Heading))); continue; }
            // The actual pair is a proposed mechanical connection. Its implied
            // relative projections are useful constraints, never observations.
            var h = Constrain(message.Frame.Horizontal, message.Frame.Right, message.Frame.Left, hEqual, hBefore);
            var v = Constrain(message.Frame.Vertical, message.Frame.Up, message.Frame.Down, vEqual, vBefore);
            assumptions.Add(new Relation(pair, new Relation(h, v)));
            Relation Constrain(Relation axis, IElement positive, IElement negative, List<Relation> ties, List<Relation> before)
            {
                var directions = E(a.Heading);
                Relation constraint;
                if (directions.Any(direction => ReferenceEquals(direction, positive))) { constraint = new(a.Role, b.Role); before.Add(constraint); }
                else if (directions.Any(direction => ReferenceEquals(direction, negative))) { constraint = new(b.Role, a.Role); before.Add(constraint); }
                else { constraint = S(a.Role, b.Role); ties.Add(constraint); }
                return new(axis, constraint);
            }
        }
        if (contradictions.Count != 0) return Finish(null, contradictions, assumptions);
        var horizontal = Solve(spatial.Horizontal, hEqual, hBefore);
        var vertical = Solve(spatial.Vertical, vEqual, vBefore);
        if (horizontal is null || vertical is null) return Finish(null, contradictions, assumptions);
        var layout = new JointShapeDescription.Message(message.Frame, message.Basis, message.Roles, message.Joints,
            horizontal, vertical, message.Symmetries, message.Components, message.ClosureDefault, message.InteriorTurns, message.Obligations);
        return Finish(layout, contradictions, assumptions);

        Inference Finish(JointShapeDescription.Message? layout, IEnumerable<IElement> falseRows, IEnumerable<IElement> guessed)
        {
            var conflict = S(falseRows.ToArray()); var assumed = S(guessed.ToArray());
            var witness = new Relation(new Relation(message.Surface, spatial.Surface), new Relation(proposedRayPairs,
                new Relation(layout?.Surface ?? StructuralConstraint.Open(), new Relation(conflict, assumed))));
            return new(layout, witness, conflict, assumed, layout is not null && E(conflict).Length == 0);
        }

        Relation? Solve(Relation axis, IReadOnlyList<Relation> hypotheticalEqual, IReadOnlyList<Relation> hypotheticalBefore)
        {
            if (!Axis(axis, message.Roles)) { contradictions.Add(axis); return null; }
            var body = (Relation)axis.Right;
            var knownEqual = E((Relation)body.Left).Cast<Relation>().ToArray();
            var knownBefore = E((Relation)body.Right).Cast<Relation>().ToArray();
            var parent = roles.ToDictionary(role => role, role => role, FieldNative.Participants.Instance);
            IElement Find(IElement role)
            {
                var at = role;
                while (!ReferenceEquals(parent[at], at)) at = parent[at];
                while (!ReferenceEquals(parent[role], role)) { var next = parent[role]; parent[role] = at; role = next; }
                return at;
            }
            void Union(Relation group)
            {
                var members = E(group); if (members.Length == 0) return;
                var root = Find(members[0]); foreach (var member in members.Skip(1)) parent[Find(member)] = root;
            }
            foreach (var group in knownEqual) Union(group);
            foreach (var group in hypotheticalEqual) Union(group);
            var classes = roles.GroupBy(Find, FieldNative.Participants.Instance)
                .Select(group => (Root: group.Key, Members: group.ToArray())).ToArray();
            var index = classes.Select((item, i) => (item.Root, i)).ToDictionary(pair => pair.Root, pair => pair.i, FieldNative.Participants.Instance);
            var outgoing = Enumerable.Range(0, classes.Length).Select(_ => new HashSet<int>()).ToArray();
            var incoming = new int[classes.Length];
            var edges = knownBefore.Concat(hypotheticalBefore).ToArray();
            foreach (var edge in edges)
            {
                var a = index[Find(edge.Left)]; var b = index[Find(edge.Right)];
                if (a == b) { contradictions.Add(new Relation(axis.Left, new Relation(edge, S(classes[a].Members)))); continue; }
                if (outgoing[a].Add(b)) incoming[b]++;
            }
            if (contradictions.Count != 0) return null;
            var ready = Enumerable.Range(0, classes.Length).Where(i => incoming[i] == 0).ToList();
            var ordered = new List<int>(); var levels = new int[classes.Length];
            while (ready.Count != 0)
            {
                // Choice among incomparable classes is explicitly a sampling
                // policy. Complete positive order leaves no such choice.
                var pick = variant % 3 == 2 ? ready.Max() : ready.Min(); ready.Remove(pick); ordered.Add(pick);
                foreach (var next in outgoing[pick])
                { levels[next] = Math.Max(levels[next], levels[pick] + 1); if (--incoming[next] == 0) ready.Add(next); }
            }
            if (ordered.Count != classes.Length)
            {
                var blocked = Enumerable.Range(0, classes.Length).Where(i => incoming[i] > 0).ToHashSet();
                contradictions.Add(new Relation(axis.Left, S(edges.Where(edge => blocked.Contains(index[Find(edge.Left)]) &&
                    blocked.Contains(index[Find(edge.Right)])).Cast<IElement>().ToArray()))); return null;
            }
            bool Reachable(int a, int b)
            {
                var visited = new HashSet<int>(); var pending = new Stack<int>(); pending.Push(a);
                while (pending.TryPop(out var at))
                    if (visited.Add(at)) { if (at == b) return true; foreach (var next in outgoing[at]) pending.Push(next); }
                return false;
            }
            var buckets = variant % 3 == 0
                ? ordered.GroupBy(i => levels[i]).OrderBy(group => group.Key).Select(group => group.ToArray()).ToArray()
                : ordered.Select(i => new[] { i }).ToArray();
            for (var i = 0; i < buckets.Length; i++)
            {
                // Several unrelated quotient classes may share a longest-path
                // level. Their newly equal projection is a default, not proof.
                if (buckets[i].Length > 1) assumptions.Add(new Relation(axis.Left,
                    S(buckets[i].SelectMany(at => classes[at].Members).ToArray())));
                if (i == 0) continue;
                foreach (var before in buckets[i - 1]) foreach (var after in buckets[i])
                    if (!Reachable(before, after)) assumptions.Add(new Relation(axis.Left,
                        new Relation(classes[before].Root, classes[after].Root)));
            }
            return new(axis.Left, S(buckets.Select(bucket => (IElement)S(bucket.SelectMany(at => classes[at].Members).ToArray())).ToArray()));
        }
    }

    public static Assessment Assess(Read spatial, JointShapeReconstruction.Candidate candidate)
    {
        var supported = new List<IElement>(); var contradicted = new List<IElement>(); var open = new List<IElement>();
        var frame = candidate.Source.Graph.Frame;
        foreach (var axis in new[] { spatial.Horizontal, spatial.Vertical })
        {
            var selected = ReferenceEquals(axis.Left, spatial.Frame.Horizontal) ? frame.Horizontal : frame.Vertical;
            var body = (Relation)axis.Right;
            foreach (var group in E((Relation)body.Left).Cast<Relation>())
            {
                var members = E(group);
                for (var index = 1; index < members.Length; index++) Check(members[0], members[index], expected: 0, group);
            }
            foreach (var before in E((Relation)body.Right).Cast<Relation>()) Check(before.Left, before.Right, expected: -1, before);
            void Check(IElement a, IElement b, int expected, Relation fact)
            {
                if (!candidate.Positions.TryGetValue(a, out var pa) || !candidate.Positions.TryGetValue(b, out var pb))
                { open.Add(new Relation(new Relation(axis.Left, fact), candidate.RoleMap)); return; }
                var witness = new Relation(new Relation(axis.Left, fact), new Relation(new Relation(a, pa), new Relation(b, pb)));
                var comparison = Compare(frame, selected, pa, pb);
                if (comparison is null) open.Add(witness);
                else if (comparison == expected) supported.Add(witness);
                else contradicted.Add(witness);
            }
        }
        var yes = S(supported.ToArray()); var no = S(contradicted.ToArray()); var pending = S(open.ToArray());
        return new(yes, no, pending, new Relation(new Relation(spatial.Surface, candidate.Surface), new Relation(yes, new Relation(no, pending))));
    }

    private static Relation FromBuckets(Relation order)
    {
        var buckets = E((Relation)order.Right).Cast<Relation>().ToArray();
        var before = new List<IElement>();
        for (var index = 1; index < buckets.Length; index++)
            if (E(buckets[index - 1]).Length != 0 && E(buckets[index]).Length != 0)
                before.Add(new Relation(E(buckets[index - 1])[0], E(buckets[index])[0]));
        return new(order.Left, new Relation(S(buckets.Cast<IElement>().ToArray()), S(before.ToArray())));
    }
    private static bool Axis(Relation axis, Relation roles)
    {
        if (axis.Right is not Relation { Left: Relation equal, Right: Relation before } || !Closed(equal) || !Closed(before)) return false;
        var members = E(roles).ToHashSet(FieldNative.Participants.Instance);
        return E(equal).All(row => row is Relation group && Closed(group) && E(group).Length > 0 && E(group).All(members.Contains)) &&
            E(before).All(row => row is Relation pair && members.Contains(pair.Left) && members.Contains(pair.Right));
    }
    private static bool SameRoles(Relation a, Relation b) => Closed(a) && Closed(b) && E(a).Length == E(b).Length &&
        E(a).ToHashSet(FieldNative.Participants.Instance).SetEquals(E(b));

    private static bool Reframe(Relation source, Relation oldFrame, Relation targetFrame, out Relation output, out Relation correspondence)
    {
        output = null!; correspondence = S();
        var mapped = new Dictionary<IElement, IElement>(FieldNative.Participants.Instance);
        var pending = new Stack<(IElement Source, IElement Target)>(); pending.Push((oldFrame, targetFrame));
        while (pending.TryPop(out var pair))
        {
            if (mapped.TryGetValue(pair.Source, out var before)) { if (!ReferenceEquals(before, pair.Target)) return false; continue; }
            if (pair.Source is Placeholder a && pair.Target is Placeholder b && a.IsInverse == b.IsInverse) { mapped.Add(a, b); continue; }
            if (pair.Source is not Relation from || pair.Target is not Relation to) return false;
            mapped.Add(from, to); pending.Push((from.Left, to.Left)); pending.Push((from.Right, to.Right));
        }
        var active = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        var work = new Stack<(IElement Item, bool Finish)>(); work.Push((source, false));
        while (work.TryPop(out var at))
        {
            if (mapped.ContainsKey(at.Item)) continue;
            if (at.Item is Placeholder slot) { mapped.Add(slot, slot); continue; }
            if (at.Item is not Relation relation) return false;
            if (!at.Finish)
            {
                if (!active.Add(relation)) return false;
                work.Push((relation, true)); work.Push((relation.Right, false)); work.Push((relation.Left, false));
            }
            else { mapped.Add(relation, new Relation(mapped[relation.Left], mapped[relation.Right])); active.Remove(relation); }
        }
        output = (Relation)mapped[source];
        correspondence = S(mapped.Select(pair => (IElement)new Relation(pair.Key, pair.Value)).ToArray()); return true;
    }
}
