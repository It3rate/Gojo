using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Pairs anonymous local ray slots through advertised ordinal lanes.
/// A bounded authored policy proposes connections; a separate read assesses
/// each proposal from the message alone. Neither receives teacher geometry.</summary>
public static class JointShapeReconstruction
{
    // attempt = (message -> (policy native choices -> previous feedback)) ->
    //   ((ray pairings -> assumed bridges) ->
    //    ((joint-role -> fresh output-site) -> (native output -> unresolved)))
    // feedback = (message -> output) ->
    //   (Closed(matched claim witnesses) -> (Closed(violations) -> Closed(open)))
    // Ordinary edge pairing and a default-induced bridge are different scopes.
    public sealed record Ray(int Index, IElement Slot, IElement Joint, Relation Heading);
    public sealed record Claim(string Kind, string State, Relation Witness);
    public sealed class Feedback
    {
        public IReadOnlyList<Claim> Claims { get; }
        public Relation Matched { get; }
        public Relation Violations { get; }
        public Relation Open { get; }
        public Relation Surface { get; }
        public Candidate Output { get; }
        public int Quality => Claims.Where(claim => claim.State == "Supported").Sum(claim => claim.Kind == "closed" ? 4 : 1)
            - Claims.Where(claim => claim.State == "Violated").Sum(claim => claim.Kind is "crossings" or "overlaps" or "closed" ? 4 : 2);
        internal Feedback(JointShapeDescription.Message message, Candidate candidate, IReadOnlyList<Claim> claims)
        {
            Claims = claims;
            Output = candidate;
            Matched = S(claims.Where(claim => claim.State == "Supported").Select(claim => (IElement)claim.Witness).ToArray());
            Violations = S(claims.Where(claim => claim.State == "Violated").Select(claim => (IElement)claim.Witness).ToArray());
            Open = S(claims.Where(claim => claim.State == "Open").Select(claim => (IElement)claim.Witness).ToArray());
            Surface = new(new Relation(message.Surface, candidate.Surface), new Relation(Matched, new Relation(Violations, Open)));
        }
    }

    public sealed class Candidate
    {
        public NativeLatticeSources.SourceCase Source { get; }
        public Relation RoleMap { get; }
        public Relation Pairings { get; }
        public Relation AssumedStrokes { get; }
        public Relation Unresolved { get; }
        public Relation Surface { get; }
        public IReadOnlyDictionary<IElement, IElement> Sites { get; }
        public IReadOnlyDictionary<IElement, Relation> Positions { get; }
        public IReadOnlyDictionary<IElement, Relation> SlotEdges { get; }
        public Feedback Assessment { get; internal set; } = null!;
        public string Policy { get; }
        internal Candidate(NativeLatticeSources.SourceCase source, Relation roleMap, Relation pairings, Relation assumed,
            Relation open, Relation policy, Relation previous, IReadOnlyDictionary<IElement, IElement> sites,
            IReadOnlyDictionary<IElement, Relation> positions, IReadOnlyDictionary<IElement, Relation> slotEdges, string name)
        {
            Source = source; RoleMap = roleMap; Pairings = pairings; AssumedStrokes = assumed; Unresolved = open;
            Sites = sites; Positions = positions; SlotEdges = slotEdges; Policy = name;
            Surface = new(new Relation(policy, previous), new Relation(new Relation(pairings, assumed),
                new Relation(roleMap, new Relation(new Relation(source.Graph.Surface, source.Bindings), open))));
        }
    }

    public static IReadOnlyList<Candidate> ThreeAttempts(JointShapeDescription.Message message)
    {
        var result = new List<Candidate>();
        for (var mode = 0; mode < 3; mode++)
        {
            var next = Propose(message, mode, result.LastOrDefault()?.Assessment);
            next.Assessment = Assess(message, next); result.Add(next);
        }
        return result;
    }

    public static Candidate Best(IReadOnlyList<Candidate> attempts) => attempts.OrderByDescending(attempt => attempt.Assessment.Quality)
        .ThenBy(attempt => E(attempt.AssumedStrokes).Length).ThenBy(attempt => E(attempt.Unresolved).Length).First();

    public static Candidate Propose(JointShapeDescription.Message message, int mode, Feedback? previous = null)
    {
        if (mode is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(mode));
        var frame = message.Frame; var roles = E(message.Roles);
        var sites = roles.ToDictionary(role => role, _ => (IElement)new Placeholder(), FieldNative.Participants.Instance);
        var positions = new Dictionary<IElement, Relation>(FieldNative.Participants.Instance);
        var h = Membership((Relation)message.Horizontal.Right); var v = Membership((Relation)message.Vertical.Right);
        var rayList = message.JointReads.SelectMany(joint => E(joint.Rays).Cast<Relation>().Select(row => (joint, row)))
            // Compile tie order from advertised axis order and local headings,
            // rather than inherit the teacher's path/site serialization order.
            .OrderBy(pair => v.GetValueOrDefault(pair.joint.Role, int.MaxValue))
            .ThenBy(pair => h.GetValueOrDefault(pair.joint.Role, int.MaxValue))
            .ThenBy(pair => Sector(frame, (Relation)pair.row.Right))
            .Select((pair, index) => new Ray(index, pair.row.Left, pair.joint.Role, (Relation)pair.row.Right)).ToArray();
        var pending = new List<IElement>(); var paired = new List<IElement>(); var edges = new List<IElement>();
        var assumed = new List<IElement>(); var policy = new List<IElement>();
        var feedbackDemand = previous?.Claims.Where(claim => claim.State != "Supported").Select(claim => (IElement)claim.Witness).ToArray() ?? [];
        var repair = mode > 0 && feedbackDemand.Length != 0;
        var bendRepair = mode == 2 && previous is not null && E(previous.Violations).OfType<Relation>().Any(witness =>
            witness.Right is Relation evidence && message.JointReads.Any(joint => ReferenceEquals(evidence.Left, joint.Opposed)));
        var discouraged = new List<Relation>();
        if (previous is not null)
            foreach (var claim in previous.Claims.Where(claim => claim.State == "Violated"))
            {
                var evidence = claim.Witness.Right;
                var possibleEdges = evidence is Relation pair ? new[] { evidence, pair.Left, pair.Right } : new[] { evidence };
                foreach (var item in possibleEdges.OfType<Relation>())
                    if (QualitativeStrokeGraph.TryReadStroke(item, out _, out var a, out var b, out _))
                    {
                        var ra = previous.Output.Sites.FirstOrDefault(site => ReferenceEquals(site.Value, a)).Key;
                        var rb = previous.Output.Sites.FirstOrDefault(site => ReferenceEquals(site.Value, b)).Key;
                        if (ra is not null && rb is not null) discouraged.Add(new Relation(ra, rb));
                    }
            }
        if (previous is not null) policy.Add(new Relation(previous.Surface, repair ? S(feedbackDemand) : S()));
        var slotEdges = new Dictionary<IElement, Relation>(FieldNative.Participants.Instance);
        var reach = new Placeholder();
        var validOrder = Closed((Relation)message.Horizontal.Right) && Closed((Relation)message.Vertical.Right) &&
            roles.All(role => h.ContainsKey(role) && v.ContainsKey(role));
        if (validOrder)
            foreach (var role in roles)
                positions[role] = S(Gaps(frame.Right, h[role], E((Relation)message.Horizontal.Right).Length, mode == 1 ? 2 : 1)
                    .Concat(Gaps(frame.Up, v[role], E((Relation)message.Vertical.Right).Length, 1)).ToArray());
        else pending.Add(new Relation(message.Horizontal, message.Vertical));
        var used = new HashSet<int>();
        if (validOrder)
        {
            var eligible = rayList.ToDictionary(ray => ray.Index, ray => rayList.Where(other => other.Index != ray.Index &&
                !ReferenceEquals(ray.Joint, other.Joint) && CanPair(ray, other)).Select(other => other.Index).ToArray());
            while (used.Count < rayList.Length)
            {
                var first = rayList.Where(ray => !used.Contains(ray.Index)).OrderBy(ray => eligible[ray.Index].Count(index => !used.Contains(index))).ThenBy(ray => ray.Index).First();
                var choices = eligible[first.Index].Where(index => !used.Contains(index)).Select(index => rayList[index]).Where(other => CanPair(first, other)).ToArray();
                if (repair || mode == 2)
                    choices = choices.OrderBy(other => Penalty(first, other)).ThenBy(other => other.Index).ToArray();
                if (choices.Length == 0) { used.Add(first.Index); pending.Add(new Relation(first.Slot, first.Heading)); continue; }
                var second = choices[0]; used.Add(first.Index); used.Add(second.Index);
                var edge = MakeEdge(first.Joint, second.Joint); edges.Add(edge);
                slotEdges[first.Slot] = edge; slotEdges[second.Slot] = edge;
                var binding = new Relation(new Relation(first.Slot, second.Slot), edge); paired.Add(binding);
                policy.Add(new Relation(new Relation(first.Joint, second.Joint), new Relation(first.Heading, second.Heading)));
            }
            // Close only a two-ended observed component. The added passage is
            // a proposal in its own scope, never inserted into source evidence.
            foreach (var component in Components(roles, edges.Cast<Relation>().ToArray(), sites))
            {
                var ends = component.Where(role => Degree(role) == 1).ToArray();
                if (ends.Length == 2 && component.Count >= 3)
                {
                    var bridge = MakeEdge(ends[0], ends[1]); edges.Add(bridge); assumed.Add(bridge);
                    policy.Add(new Relation(message.ClosureDefault, bridge));
                }
            }
        }
        var graph = new QualitativeStrokeGraph(frame, S(roles.Select(role => sites[role]).ToArray()), S(edges.ToArray()));
        var bindings = S(positions.Select(pair => (IElement)new Relation(sites[pair.Key], pair.Value)).ToArray());
        var source = new NativeLatticeSources.SourceCase("sample", "Joint reconstruction", "A native sample of an anonymous structural message.",
            graph, bindings, graph.Strokes, S(), message.Basis.ReadRightAngle(S()).Answer == "Yes");
        var roleMap = S(roles.Select(role => (IElement)new Relation(role, sites[role])).ToArray());
        return new(source, roleMap, S(paired.ToArray()), S(assumed.ToArray()), S(pending.ToArray()),
            new Relation(message.Surface, S(policy.ToArray())), previous?.Surface ?? S(), sites, positions, slotEdges,
            mode == 0 ? "Local pairing + equal ordinal gaps" : mode == 1 ? "Feedback: short ordinal spans + wider horizontal gaps" :
                bendRepair ? "Feedback: restore bends with wider outer gaps" : "Feedback: symmetry-aware pairing + balanced gaps");

        IEnumerable<IElement> Gaps(IElement direction, int index, int buckets, int scale) => Enumerable.Range(0, index)
            .SelectMany(gap => Enumerable.Repeat((IElement)QualitativeMotionFrame.Move(direction, reach),
                scale * (bendRepair && (gap == 0 || gap == buckets - 2) ? 2 : 1)));

        Relation MakeEdge(IElement a, IElement b) => QualitativeStrokeGraph.Stroke(new Placeholder(), sites[a], sites[b], Difference(frame, positions[b], positions[a]));
        int Degree(IElement role) => edges.Cast<Relation>().Count(edge => { QualitativeStrokeGraph.TryReadStroke(edge, out _, out var a, out var b, out _); return ReferenceEquals(a, sites[role]) || ReferenceEquals(b, sites[role]); });
        bool CanPair(Ray a, Ray b)
        {
            if (!Closed(a.Heading) || !Closed(b.Heading)) return false;
            var actual = Heading(frame, Difference(frame, positions[b.Joint], positions[a.Joint]));
            var reverse = Heading(frame, Inverse(frame, Difference(frame, positions[b.Joint], positions[a.Joint])));
            if (!SameHeading(actual, a.Heading) || !SameHeading(reverse, b.Heading)) return false;
            return !edges.OfType<Relation>().Any(edge => SameEnds(edge, sites[a.Joint], sites[b.Joint]));
        }
        int Penalty(Ray a, Ray b)
        {
            var distance = Math.Abs(h[a.Joint] - h[b.Joint]) + Math.Abs(v[a.Joint] - v[b.Joint]);
            var penalty = distance;
            if (discouraged.Any(pair => ReferenceEquals(pair.Left, a.Joint) && ReferenceEquals(pair.Right, b.Joint) || ReferenceEquals(pair.Left, b.Joint) && ReferenceEquals(pair.Right, a.Joint))) penalty += 50;
            var edge = MakeEdge(a.Joint, b.Joint);
            foreach (var other in edges.Cast<Relation>()) if (AxisCrossing(frame, edge, other, bindings: positions, sites)) penalty += 100;
            if (mode == 2)
            {
                if (Closed(message.Components) && E(message.Components).OfType<Relation>().Any(component => E(component).Contains(a.Joint, ReferenceEqualityComparer.Instance) && !E(component).Contains(b.Joint, ReferenceEqualityComparer.Instance))) penalty += 1000;
                foreach (var clause in E(message.Symmetries).Cast<Relation>())
                {
                    var map = (Relation)((Relation)clause.Right).Left;
                    var targetA = E(map).OfType<Relation>().FirstOrDefault(row => ReferenceEquals(row.Left, a.Joint))?.Right;
                    var targetB = E(map).OfType<Relation>().FirstOrDefault(row => ReferenceEquals(row.Left, b.Joint))?.Right;
                    if (targetA is not null && targetB is not null && edges.OfType<Relation>().Any(other => SameEnds(other, sites[targetA], sites[targetB]))) penalty -= 3;
                }
            }
            return penalty;
        }
    }

    public static Feedback Assess(JointShapeDescription.Message message, Candidate candidate)
    {
        var frame = message.Frame; var claims = new List<Claim>(); var graph = candidate.Source.Graph; var edges = E(graph.Strokes).Cast<Relation>().ToArray();
        void Add(string kind, string state, IElement evidence) => claims.Add(new(kind, state, new Relation(new Relation(message.Surface, candidate.Surface), evidence)));
        var roleForSite = candidate.Sites.ToDictionary(pair => pair.Value, pair => pair.Key, FieldNative.Participants.Instance);
        var rayWords = new Dictionary<IElement, Relation>(FieldNative.Participants.Instance);
        foreach (var joint in message.JointReads)
            foreach (var ray in E(joint.Rays).Cast<Relation>())
            {
                if (!candidate.SlotEdges.TryGetValue(ray.Left, out var edge)) { Add("jointTypes", "Open", ray); continue; }
                QualitativeStrokeGraph.TryReadStroke(edge, out _, out var a, out _, out var word);
                var outward = ReferenceEquals(a, candidate.Sites[joint.Role]) ? word : Inverse(frame, word); rayWords[ray.Left] = outward;
                Add("jointTypes", SameHeading(Heading(frame, outward), (Relation)ray.Right) ? "Supported" : "Violated", new Relation(ray, edge));
            }
        foreach (var joint in message.JointReads)
            foreach (var pair in E(joint.Opposed).Cast<Relation>())
            {
                var state = "Open";
                if (rayWords.TryGetValue(pair.Left, out var a) && rayWords.TryGetValue(pair.Right, out var b))
                {
                    var ha = Heading(frame, a); var hb = Heading(frame, b);
                    var opposite = (Sector(frame, ha) + 4) % 8 == Sector(frame, hb);
                    if (!opposite) state = "Violated";
                    else if (E(ha).Length == 1 || Zero(frame, frame.Reduce(a, b))) state = "Supported";
                }
                Add("jointTypes", state, pair);
            }
        foreach (var joint in message.JointReads)
            foreach (var pair in E(joint.Perpendicular).Cast<Relation>())
                Add("jointTypes", rayWords.TryGetValue(pair.Left, out var a) && rayWords.TryGetValue(pair.Right, out var b)
                    ? Perpendicular(frame, a, b) ? "Supported" : "Violated" : "Open", pair);
        // A complete native opposed-pair inventory can also exclude a newly
        // straightened corner. An open inventory supplies positive evidence
        // only; absence from it cannot reject a new opposition.
        graph.TryReadResult(graph.Evaluate(), out var outputJunctions, out _, out _, out _);
        foreach (var joint in message.JointReads.Where(joint => Closed(joint.Opposed)))
        {
            var actual = E(outputJunctions).Cast<Relation>().Single(row => ReferenceEquals(((Relation)row.Left).Right, candidate.Sites[joint.Role]));
            graph.TryReadJunction(actual, out _, out _, out var actualPairs, out _);
            foreach (var pair in E(actualPairs).Cast<Relation>())
            {
                var rays = (Relation)pair.Left;
                var aEdge = ((Relation)rays.Left).Left; var bEdge = ((Relation)rays.Right).Left;
                var a = E(joint.Rays).Cast<Relation>().FirstOrDefault(ray => candidate.SlotEdges.TryGetValue(ray.Left, out var edge) && ReferenceEquals(edge, aEdge));
                var b = E(joint.Rays).Cast<Relation>().FirstOrDefault(ray => candidate.SlotEdges.TryGetValue(ray.Left, out var edge) && ReferenceEquals(edge, bEdge));
                if (a is null || b is null) continue; // default bridge has no observed ray slot
                if (!E(joint.Opposed).Cast<Relation>().Any(expected => ReferenceEquals(expected.Left, a.Left) && ReferenceEquals(expected.Right, b.Left) ||
                    ReferenceEquals(expected.Left, b.Left) && ReferenceEquals(expected.Right, a.Left)))
                    Add("jointTypes", "Violated", new Relation(joint.Opposed, pair));
            }
        }
        foreach (var order in new[] { message.Horizontal, message.Vertical })
        {
            var axis = (Relation)order.Left; var buckets = E((Relation)order.Right).Cast<Relation>().ToArray();
            for (var i = 0; i < buckets.Length; i++) foreach (var first in E(buckets[i]))
                for (var j = i; j < buckets.Length; j++) foreach (var second in E(buckets[j]))
                {
                    if (ReferenceEquals(first, second)) continue;
                    var comparison = candidate.Positions.TryGetValue(first, out var a) && candidate.Positions.TryGetValue(second, out var b) ? Compare(frame, axis, a, b) : null;
                    Add("positions", comparison is null ? "Open" : comparison == (i == j ? 0 : -1) ? "Supported" : "Violated", new Relation(order, new Relation(first, second)));
                }
        }
        var actualComponents = Components(E(message.Roles), edges, candidate.Sites);
        foreach (var expected in E(message.Components).Cast<Relation>())
        {
            var members = E(expected); var actual = actualComponents.FirstOrDefault(component => members.Length != 0 && component.Contains(members[0], ReferenceEqualityComparer.Instance));
            var connected = actual is not null && members.All(role => actual.Contains(role, ReferenceEqualityComparer.Instance));
            var exact = connected && actual!.Count == members.Length;
            Add("components", Closed(message.Components) && Closed(expected) ? exact ? "Supported" : "Violated" : connected ? "Supported" : "Open", expected);
        }
        var topology = new NativeInteriorTopology(candidate.Source, 64);
        var publicSites = E(topology.Publication.SiteMap).Cast<Relation>().ToDictionary(row => row.Left, row => row.Right, FieldNative.Participants.Instance);
        foreach (var component in actualComponents)
        {
            var allBound = component.Count >= 3 && component.All(role => edges.Count(edge => { QualitativeStrokeGraph.TryReadStroke(edge, out _, out var a, out var b, out _); return ReferenceEquals(a, candidate.Sites[role]) || ReferenceEquals(b, candidate.Sites[role]); }) >= 2);
            var componentEdges = edges.Where(edge => { QualitativeStrokeGraph.TryReadStroke(edge, out _, out var a, out _, out _); return component.Contains(roleForSite[a], ReferenceEqualityComparer.Instance); }).ToArray();
            var closedRoutes = componentEdges.All(edge => {
                QualitativeStrokeGraph.TryReadStroke(edge, out _, out var a, out var b, out _);
                var alternatives = Components(component.ToArray(), componentEdges.Where(other => !ReferenceEquals(edge, other)).ToArray(), candidate.Sites);
                return alternatives.Any(route => route.Contains(roleForSite[a], ReferenceEqualityComparer.Instance) && route.Contains(roleForSite[b], ReferenceEqualityComparer.Instance));
            });
            var publicComponent = component.Select(role => publicSites[candidate.Sites[role]]).ToArray();
            var boundary = topology.Boundaries.FirstOrDefault(read => read.Darts.Any(dart => E(dart.From.SourceSites).Any(site => publicComponent.Contains(site, ReferenceEqualityComparer.Instance))));
            Add("closed", allBound && closedRoutes && boundary is not null ? "Supported" : "Open", new Relation(message.ClosureDefault,
                new Relation(new Relation(S(component.ToArray()), S(componentEdges)), boundary?.Surface ?? StructuralConstraint.Open())));
        }
        for (var i = 0; i < edges.Length; i++) for (var j = i + 1; j < edges.Length; j++)
        {
            if (AxisOverlap(frame, edges[i], edges[j], candidate.Positions, candidate.Sites)) Add("overlaps", "Violated", new Relation(edges[i], edges[j]));
            if (ShareEnd(edges[i], edges[j])) continue;
            if (AxisCrossing(frame, edges[i], edges[j], candidate.Positions, candidate.Sites)) Add("crossings", "Violated", new Relation(edges[i], edges[j]));
        }
        foreach (var clause in E(message.Symmetries).Cast<Relation>())
        {
            var action = E((Relation)clause.Left).Cast<Relation>().ToDictionary(row => row.Left, row => row.Right, FieldNative.Participants.Instance);
            var body = (Relation)clause.Right; var mapping = (Relation)body.Left; var remainder = (Relation)body.Right;
            var exact = remainder.Left is Relation unmatched && Closed(unmatched) && E(unmatched).Length == 0 &&
                remainder.Right is Relation { Left: Relation missing, Right: Relation open } && Closed(missing) && E(missing).Length == 0 && Closed(open) && E(open).Length == 0;
            var siteMap = E(mapping).Cast<Relation>().ToDictionary(row => row.Left, row => row.Right, FieldNative.Participants.Instance);
            Relation Transform(Relation word) => S(E(word).Cast<Relation>().Select(move => (IElement)QualitativeMotionFrame.Move(action[move.Left], move.Right)).ToArray());
            // One action clause has one translation, including when its scope
            // spans disconnected components. Independent component sliding
            // cannot masquerade as the same rigid action.
            Relation? translation = null;
            foreach (var correspondence in siteMap)
            {
                if (!candidate.Positions.TryGetValue(correspondence.Key, out var from) || !candidate.Positions.TryGetValue(correspondence.Value, out var to))
                { Add("symmetry", "Open", new Relation(clause, new Relation(correspondence.Key, correspondence.Value))); continue; }
                var offset = Difference(frame, to, Transform(from));
                if (translation is null) translation = offset;
                else Add("symmetry", Zero(frame, frame.Reduce(offset, Inverse(frame, translation))) ? "Supported" : exact ? "Violated" : "Open",
                    new Relation(clause, new Relation(translation, offset)));
            }
            foreach (var edge in edges)
            {
                QualitativeStrokeGraph.TryReadStroke(edge, out _, out var a, out var b, out var word);
                if (!roleForSite.TryGetValue(a, out var ra) || !roleForSite.TryGetValue(b, out var rb) || !siteMap.TryGetValue(ra, out var ma) || !siteMap.TryGetValue(rb, out var mb)) continue;
                var target = edges.FirstOrDefault(other => SameEnds(other, candidate.Sites[ma], candidate.Sites[mb]));
                var matched = false;
                if (target is not null)
                {
                    QualitativeStrokeGraph.TryReadStroke(target, out _, out var start, out _, out var targetWord);
                    if (!ReferenceEquals(start, candidate.Sites[ma])) targetWord = Inverse(frame, targetWord);
                    matched = Zero(frame, frame.Reduce(Transform(word), Inverse(frame, targetWord)));
                }
                Add("symmetry", matched ? "Supported" : exact ? "Violated" : "Open", new Relation(clause, edge));
            }
            if (!exact) Add("symmetry", "Open", clause);
        }
        foreach (var unresolved in E(topology.EmbeddingUnknown)) Add("crossings", "Open", unresolved);
        var sourceToRole = E(topology.Publication.SiteMap).Cast<Relation>().ToDictionary(row => row.Right, row => roleForSite[row.Left], FieldNative.Participants.Instance);
        var outputContexts = new List<Dictionary<IElement, List<int>>>();
        foreach (var boundary in topology.Boundaries)
        {
            var outputTurns = new Dictionary<IElement, List<int>>(FieldNative.Participants.Instance);
            outputContexts.Add(outputTurns);
            for (var i = 0; i < boundary.Darts.Count; i++)
                foreach (var site in E(boundary.Darts[i].From.SourceSites))
                    if (sourceToRole.TryGetValue(site, out var role))
                    {
                        var difference = (boundary.Darts[i].Sector - boundary.Darts[(i + boundary.Darts.Count - 1) % boundary.Darts.Count].Sector + 8) % 8;
                        var sign = difference is > 0 and < 4 ? 1 : difference is > 4 and < 8 ? -1 : 0;
                        if (!outputTurns.TryGetValue(role, out var list)) outputTurns[role] = list = [];
                        list.Add(sign);
                    }
        }
        foreach (var context in E(message.InteriorTurns).Cast<Relation>().GroupBy(row => ((Relation)row.Left).Right, FieldNative.Participants.Instance))
        {
            var expectedTurns = context.Select(row => {
                var turn = (Relation)row.Right;
                var difference = (Sector(frame, (Relation)turn.Right) - Sector(frame, (Relation)turn.Left) + 8) % 8;
                return (Row: row, Role: ((Relation)row.Left).Left, Sign: difference is > 0 and < 4 ? 1 : difference is > 4 and < 8 ? -1 : 0);
            }).ToArray();
            // All turns in one communicated area context need one compatible
            // output boundary; borrowing unrelated faces independently is not
            // a witness for that context.
            var known = expectedTurns.Where(turn => turn.Sign != 0).ToArray();
            var coherent = known.Length != 0 && outputContexts.Any(read => known.All(turn => read.TryGetValue(turn.Role, out var actual) && actual.Contains(turn.Sign)));
            foreach (var turn in expectedTurns)
                Add("refinement", turn.Sign == 0 || outputContexts.Count == 0 ? "Open" : coherent ? "Supported" : "Violated", turn.Row);
        }
        foreach (var edge in E(candidate.AssumedStrokes)) Add("assumptions", "Open", new Relation(message.ClosureDefault, edge));
        foreach (var unresolved in E(candidate.Unresolved)) Add("jointTypes", "Open", unresolved);
        return new(message, candidate, claims);
    }

    private static Dictionary<IElement, int> Membership(Relation buckets) => E(buckets).Cast<Relation>()
        .SelectMany((bucket, index) => E(bucket).Select(role => (role, index))).ToDictionary(pair => pair.role, pair => pair.index, FieldNative.Participants.Instance);
    private static bool SameHeading(Relation a, Relation b) => Closed(a) && Closed(b) && E(a).Length == E(b).Length && E(a).All(direction => E(b).Contains(direction, ReferenceEqualityComparer.Instance));
    private static bool SameEnds(Relation edge, IElement a, IElement b) => QualitativeStrokeGraph.TryReadStroke(edge, out _, out var x, out var y, out _) &&
        (ReferenceEquals(a, x) && ReferenceEquals(b, y) || ReferenceEquals(a, y) && ReferenceEquals(b, x));
    private static bool ShareEnd(Relation first, Relation second)
    {
        QualitativeStrokeGraph.TryReadStroke(first, out _, out var a, out var b, out _); QualitativeStrokeGraph.TryReadStroke(second, out _, out var c, out var d, out _);
        return ReferenceEquals(a, c) || ReferenceEquals(a, d) || ReferenceEquals(b, c) || ReferenceEquals(b, d);
    }
    private static List<List<IElement>> Components(IElement[] roles, Relation[] edges, IReadOnlyDictionary<IElement, IElement> sites)
    {
        var result = new List<List<IElement>>(); var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        foreach (var role in roles)
        {
            if (!seen.Add(role)) continue;
            var component = new List<IElement> { role }; var queue = new Queue<IElement>(); queue.Enqueue(role);
            while (queue.TryDequeue(out var at))
                foreach (var next in roles.Where(other => !seen.Contains(other) && edges.Any(edge => SameEnds(edge, sites[at], sites[other]))))
                { seen.Add(next); component.Add(next); queue.Enqueue(next); }
            result.Add(component);
        }
        return result;
    }
    private static bool AxisCrossing(QualitativeMotionFrame frame, Relation first, Relation second,
        IReadOnlyDictionary<IElement, Relation> bindings, IReadOnlyDictionary<IElement, IElement> sites)
    {
        if (ShareEnd(first, second)) return false;
        QualitativeStrokeGraph.TryReadStroke(first, out _, out var a, out var b, out var aw); QualitativeStrokeGraph.TryReadStroke(second, out _, out var c, out var d, out var bw);
        var lookup = sites.ToDictionary(pair => pair.Value, pair => pair.Key, FieldNative.Participants.Instance);
        var sa = Sector(frame, Heading(frame, aw)); var sb = Sector(frame, Heading(frame, bw));
        if (sa is < 0 || sb is < 0 || sa % 2 != 0 || sb % 2 != 0 || sa % 4 == sb % 4) return false;
        var ah = sa % 4 == 0; var h0 = bindings[lookup[ah ? a : c]]; var h1 = bindings[lookup[ah ? b : d]];
        var v0 = bindings[lookup[ah ? c : a]]; var v1 = bindings[lookup[ah ? d : b]];
        bool Between(Relation axis, Relation point, Relation x, Relation y) => Compare(frame, axis, point, x) is int p && Compare(frame, axis, point, y) is int q && p * q < 0;
        return Between(frame.Horizontal, v0, h0, h1) && Between(frame.Vertical, h0, v0, v1);
    }

    private static bool AxisOverlap(QualitativeMotionFrame frame, Relation first, Relation second,
        IReadOnlyDictionary<IElement, Relation> bindings, IReadOnlyDictionary<IElement, IElement> sites)
    {
        QualitativeStrokeGraph.TryReadStroke(first, out _, out var a, out var b, out var aw);
        QualitativeStrokeGraph.TryReadStroke(second, out _, out var c, out var d, out var bw);
        var sa = Sector(frame, Heading(frame, aw)); var sb = Sector(frame, Heading(frame, bw));
        if (sa < 0 || sb < 0 || sa % 2 != 0 || sb % 2 != 0 || sa % 4 != sb % 4) return false;
        var lookup = sites.ToDictionary(pair => pair.Value, pair => pair.Key, FieldNative.Participants.Instance);
        var p = bindings[lookup[a]]; var q = bindings[lookup[b]]; var r = bindings[lookup[c]]; var s = bindings[lookup[d]];
        var axis = sa % 4 == 0 ? frame.Horizontal : frame.Vertical;
        var other = sa % 4 == 0 ? frame.Vertical : frame.Horizontal;
        if (Compare(frame, other, p, r) != 0) return false;
        bool Inside(Relation point, Relation x, Relation y) => Compare(frame, axis, point, x) is int left && Compare(frame, axis, point, y) is int right && left * right < 0;
        return Inside(p, r, s) || Inside(q, r, s) || Inside(r, p, q) || Inside(s, p, q) ||
            Compare(frame, axis, p, r) == 0 && Compare(frame, axis, q, s) == 0 || Compare(frame, axis, p, s) == 0 && Compare(frame, axis, q, r) == 0;
    }
}
