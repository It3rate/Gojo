using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A captured quarter-turn observation of active motif ink and
/// undirected required-seam obligations. Local graph proofs compose through
/// explicit node, port and seam correspondences. Directed material routing,
/// policy state and Euclidean metric symmetry are separate questions.</summary>
public sealed class AdaptiveWallpaperSymmetry
{
    // Surface = ((machine -> revision snapshot) -> (field -> transform)) ->
    //           ((origin -> role-turn) -> captured-seams)
    // read = Surface -> ((node-map -> (component-proofs -> seam-map)) -> (blocked -> unknown))
    // component = actual node-map row -> ((anchor-proof -> port-proofs) -> local-graph-proof)
    // seam = (actual source seam -> actual target seam) ->
    //        ((actual source link -> actual target link) ->
    //         ((from-node-map -> to-node-map) -> ((from-role-map -> to-role-map) ->
    //          (ordered target endpoints -> (source equality -> target equality)))))
    // Reversed ordered target endpoints retain undirected seam reversal;
    // they do not establish equivalence of directed packet routes.
    public AdaptiveWallpaperMachine Machine { get; }
    public QualitativeGraphJoin Field { get; }
    public QualitativeStrokeGraph Graph => Field.Graph;
    public Relation Bindings => Field.Bindings;
    public QualitativePatternTransform Transform { get; }
    public Relation Snapshot { get; }
    public Relation CapturedSeams { get; }
    public Relation RoleTurn { get; }
    public Relation Surface { get; }
    private readonly Dictionary<AdaptiveMotifNode, MotifRevision> revisions;
    private readonly Dictionary<Relation, AdaptiveWallpaperComponentSymmetry> components = new(ReferenceEqualityComparer.Instance);
    private Relation? evaluated;
    private AdaptiveMotifGrammar Grammar => Machine.Grammar;

    public AdaptiveWallpaperSymmetry(AdaptiveWallpaperMachine machine)
    {
        Machine = machine; Snapshot = machine.Snapshot(); CapturedSeams = machine.CurrentSeams; Field = machine.Field();
        revisions = machine.Nodes.ToDictionary(node => node, node => node.Current);
        Transform = Grammar.TurnTransform;
        RoleTurn = S(new Relation(Grammar.East, Grammar.North), new Relation(Grammar.North, Grammar.West),
            new Relation(Grammar.West, Grammar.South), new Relation(Grammar.South, Grammar.East));
        Surface = new(new Relation(new Relation(machine.Surface, Snapshot), new Relation(Field.Surface, Transform.Surface)),
            new Relation(new Relation(Grammar.Frame.Origin, RoleTurn), CapturedSeams));
    }

    public Relation Evaluate()
    {
        if (evaluated is not null) return evaluated;
        var blocked = new List<IElement>(); var unknown = new List<IElement>();
        var nodeMap = new List<IElement>(); var componentReads = new List<IElement>(); var seamMap = new List<IElement>();
        if (!Machine.IsValid || revisions.Count == 0 || !Field.TryReadPassed(Field.Evaluate())) unknown.Add(Field.Surface);
        var anchors = new Dictionary<AdaptiveMotifNode, Position>();
        foreach (var node in revisions.Keys)
            if (TryPosition(node.AnchorWord, out var position)) anchors.Add(node, position);
            else unknown.Add(node.Surface);
        var mappedNodes = new Dictionary<AdaptiveMotifNode, (AdaptiveMotifNode Target, Relation Row)>();
        foreach (var source in revisions.Keys)
        {
            if (!anchors.TryGetValue(source, out var position)) continue;
            var candidates = anchors.Where(entry => entry.Value == position.Turn()).Select(entry => entry.Key).ToArray();
            if (candidates.Length == 0) { blocked.Add(new Relation(source.Surface, Transform.Apply(source.AnchorWord))); continue; }
            if (candidates is not [var target]) { unknown.Add(new Relation(source.Surface, S(candidates.Select(node => (IElement)node.Surface).ToArray()))); continue; }
            var row = new Relation(source.Cell, target.Cell); nodeMap.Add(row); mappedNodes.Add(source, (target, row));
            var component = new AdaptiveWallpaperComponentSymmetry(Grammar, revisions[source], revisions[target]);
            components.Add(row, component); var graphRead = component.Evaluate();
            component.TryReadResult(graphRead, out var localSites, out _, out _, out _, out _, out _, out var localBlocked, out var localUnknown);
            blocked.AddRange(E(localBlocked).Select(item => (IElement)new Relation(row, item)));
            unknown.AddRange(E(localUnknown).Select(item => (IElement)new Relation(row, item)));
            var anchor = Equality(source.AnchorWord, target.AnchorWord);
            var ports = new List<IElement>();
            foreach (var turn in E(RoleTurn).Cast<Relation>())
                if (revisions[source].TryReadPort(turn.Left, out var fromPort, out var fromBinding) &&
                    revisions[target].TryReadPort(turn.Right, out var toPort, out var toBinding) &&
                    PortCorrespondence(revisions[source], revisions[target], fromBinding, toBinding, localSites, out var correspondence))
                    ports.Add(new Relation(turn, new Relation(new Relation(fromPort, toPort),
                        new Relation(new Relation(fromBinding, toBinding), new Relation(correspondence, Equality((Relation)fromBinding.Right, (Relation)toBinding.Right))))));
                else unknown.Add(new Relation(row, turn));
            componentReads.Add(new Relation(row, new Relation(new Relation(anchor, S(ports.ToArray())), graphRead)));
        }
        var seams = E(CapturedSeams).OfType<Relation>().ToArray();
        foreach (var source in Machine.Links)
        {
            var sourceSeams = seams.Where(seam => HasLink(seam, source)).ToArray();
            if (sourceSeams is not [var sourceSeam] || !ReadSeam(sourceSeam, source, out var sourceEquality, out var accepted) ||
                !mappedNodes.TryGetValue(source.From, out var from) || !mappedNodes.TryGetValue(source.To, out var to))
            { unknown.Add(source.Surface); continue; }
            var fromRole = E(RoleTurn).Cast<Relation>().Single(turn => ReferenceEquals(turn.Left, source.FromRole));
            var toRole = E(RoleTurn).Cast<Relation>().Single(turn => ReferenceEquals(turn.Left, source.ToRole));
            var candidates = Machine.Links.Where(target => Matches(target, from.Target, to.Target, fromRole.Right, toRole.Right)).ToArray();
            if (candidates.Length == 0) { blocked.Add(new Relation(source.Surface, new Relation(from.Row, to.Row))); continue; }
            if (candidates is not [var target]) { unknown.Add(new Relation(source.Surface, S(candidates.Select(link => (IElement)link.Surface).ToArray()))); continue; }
            var targetSeams = seams.Where(seam => HasLink(seam, target)).ToArray();
            if (targetSeams is not [var targetSeam] || !ReadSeam(targetSeam, target, out var targetEquality, out var targetAccepted))
            { unknown.Add(target.Surface); continue; }
            if (accepted != targetAccepted) { blocked.Add(new Relation(sourceSeam, targetSeam)); continue; }
            var endpoints = ReferenceEquals(target.From, from.Target)
                ? new Relation(target.From.Cell, target.To.Cell) : new Relation(target.To.Cell, target.From.Cell);
            seamMap.Add(new Relation(new Relation(sourceSeam, targetSeam),
                new Relation(new Relation(source.Surface, target.Surface), new Relation(new Relation(from.Row, to.Row),
                    new Relation(new Relation(fromRole, toRole), new Relation(endpoints, new Relation(sourceEquality, targetEquality)))))));
        }
        return evaluated = new(Surface, new Relation(new Relation(S(nodeMap.ToArray()), new Relation(S(componentReads.ToArray()), S(seamMap.ToArray()))),
            new Relation(S(blocked.ToArray()), S(unknown.ToArray()))));
    }

    public bool TryReadResult(Relation read, out Relation nodeMap, out Relation componentProofs, out Relation seamMap, out Relation blocked, out Relation unknown)
    {
        nodeMap = componentProofs = seamMap = blocked = unknown = null!;
        if (!ReferenceEquals(read.Left, Surface) || read.Right is not Relation
            { Left: Relation { Left: Relation nodes, Right: Relation { Left: Relation parts, Right: Relation seams } },
                Right: Relation { Left: Relation refused, Right: Relation pending } } ||
            !new[] { nodes, parts, seams, refused, pending }.All(Closed)) return false;
        nodeMap = nodes; componentProofs = parts; seamMap = seams; blocked = refused; unknown = pending; return true;
    }

    public bool TryReadPassed(Relation read)
    {
        if (!TryReadResult(read, out var nodeMap, out var parts, out var seamMap, out var blocked, out var unknown) ||
            E(blocked).Length != 0 || E(unknown).Length != 0 || revisions.Count == 0 ||
            E(nodeMap).Length != revisions.Count || E(parts).Length != revisions.Count || E(seamMap).Length != Machine.Links.Count ||
            !Field.TryReadPassed(Field.Evaluate())) return false;
        var rows = E(nodeMap).OfType<Relation>().ToArray();
        foreach (var node in revisions.Keys)
            if (rows.Count(row => ReferenceEquals(row.Left, node.Cell)) != 1 || rows.Count(row => ReferenceEquals(row.Right, node.Cell)) != 1) return false;
        foreach (var row in rows)
        {
            var source = revisions.Keys.SingleOrDefault(node => ReferenceEquals(node.Cell, row.Left));
            var target = revisions.Keys.SingleOrDefault(node => ReferenceEquals(node.Cell, row.Right));
            var actual = E(parts).OfType<Relation>().Where(part => ReferenceEquals(part.Left, row)).ToArray();
            if (source is null || target is null || !components.TryGetValue(row, out var component) ||
                actual is not [var part] || part.Right is not Relation { Left: Relation { Left: Relation anchor, Right: Relation ports }, Right: Relation graphRead } ||
                !CheckEquality(anchor, source.AnchorWord, target.AnchorWord) || !component.TryReadPassed(graphRead) || E(ports).Length != E(RoleTurn).Length ||
                !component.TryReadResult(graphRead, out var localSites, out _, out _, out _, out _, out _, out _, out _)) return false;
            foreach (var turn in E(RoleTurn).Cast<Relation>())
            {
                var proofs = E(ports).OfType<Relation>().Where(proof => ReferenceEquals(proof.Left, turn)).ToArray();
                if (!revisions[source].TryReadPort(turn.Left, out var a, out var ab) || !revisions[target].TryReadPort(turn.Right, out var b, out var bb) ||
                    proofs is not [var proof] || proof.Right is not Relation { Left: Relation publications, Right: Relation { Left: Relation bindings,
                        Right: Relation { Left: Relation correspondence, Right: Relation equality } } } ||
                    !ReferenceEquals(publications.Left, a) || !ReferenceEquals(publications.Right, b) || !ReferenceEquals(bindings.Left, ab) || !ReferenceEquals(bindings.Right, bb) ||
                    !CheckPortCorrespondence(correspondence, revisions[source], revisions[target], ab, bb, localSites) ||
                    !CheckEquality(equality, (Relation)ab.Right, (Relation)bb.Right)) return false;
            }
        }
        var seenTargets = new HashSet<AdaptiveMotifLink>();
        foreach (var source in Machine.Links)
        {
            var matches = E(seamMap).OfType<Relation>().Where(row => row.Left is Relation pair && pair.Left is Relation seam && HasLink(seam, source)).ToArray();
            if (matches is not [var proof] || proof.Left is not Relation { Left: Relation a, Right: Relation b } ||
                proof.Right is not Relation { Left: Relation links, Right: Relation { Left: Relation nodeRows, Right: Relation { Left: Relation roles, Right: Relation { Left: Relation endpoints, Right: Relation equalities } } } } ||
                !ReferenceEquals(links.Left, source.Surface) || Machine.Links.SingleOrDefault(link => ReferenceEquals(link.Surface, links.Right)) is not { } target || !seenTargets.Add(target) ||
                !ReadSeam(a, source, out var sourceEquality, out var accepted) || !ReadSeam(b, target, out var targetEquality, out var targetAccepted) || accepted != targetAccepted ||
                !ReferenceEquals(equalities.Left, sourceEquality) || !ReferenceEquals(equalities.Right, targetEquality) ||
                nodeRows.Left is not Relation from || nodeRows.Right is not Relation to || !rows.Any(row => ReferenceEquals(row, from)) || !rows.Any(row => ReferenceEquals(row, to)) ||
                !ReferenceEquals(from.Left, source.From.Cell) || !ReferenceEquals(to.Left, source.To.Cell) ||
                roles.Left is not Relation fromRole || roles.Right is not Relation toRole || !E(RoleTurn).Any(row => ReferenceEquals(row, fromRole)) || !E(RoleTurn).Any(row => ReferenceEquals(row, toRole)) ||
                !ReferenceEquals(fromRole.Left, source.FromRole) || !ReferenceEquals(toRole.Left, source.ToRole) ||
                !ReferenceEquals(endpoints.Left, from.Right) || !ReferenceEquals(endpoints.Right, to.Right) ||
                !Matches(target, revisions.Keys.Single(node => ReferenceEquals(node.Cell, from.Right)), revisions.Keys.Single(node => ReferenceEquals(node.Cell, to.Right)), fromRole.Right, toRole.Right)) return false;
        }
        return seenTargets.Count == Machine.Links.Count;
    }

    private bool ReadSeam(Relation seam, AdaptiveMotifLink link, out Relation equality, out bool accepted)
    {
        equality = null!; accepted = false;
        if (!E(CapturedSeams).Any(actual => ReferenceEquals(actual, seam)) || !HasLink(seam, link) ||
            seam.Left is not Relation { Right: Relation heads } || !ReferenceEquals(heads.Left, revisions[link.From].Surface) || !ReferenceEquals(heads.Right, revisions[link.To].Surface) ||
            !revisions[link.From].TryReadPort(link.FromRole, out var a, out var ab) || !revisions[link.To].TryReadPort(link.ToRole, out var b, out var bb) ||
            seam.Right is not Relation { Left: Relation publications, Right: Relation { Left: Relation bindings, Right: Relation read } } ||
            !ReferenceEquals(publications.Left, a) || !ReferenceEquals(publications.Right, b) || !ReferenceEquals(bindings.Left, ab) || !ReferenceEquals(bindings.Right, bb) ||
            read.Left is not Relation { Left: var frame, Right: Relation operands } || !ReferenceEquals(frame, Grammar.Frame.Surface) ||
            !ReferenceEquals(operands.Left, ab.Right) || operands.Right is not Relation inverse || !SameWord(inverse, Inverse((Relation)bb.Right)) ||
            !Grammar.Frame.TryReadResult(read, out var rest, out _, out var pending) || !Closed(rest) || E(pending).Length != 0) return false;
        equality = read; accepted = E(rest).Length == 0; return true;
    }
    private static bool PortCorrespondence(MotifRevision source, MotifRevision target, Relation a, Relation b, Relation sites, out Relation correspondence)
    {
        correspondence = null!;
        var from = E(source.Join.SiteMap).OfType<Relation>().Where(row => ReferenceEquals(row.Left, a.Left)).ToArray();
        var to = E(target.Join.SiteMap).OfType<Relation>().Where(row => ReferenceEquals(row.Left, b.Left)).ToArray();
        if (from is not [var sourceAlias] || to is not [var targetAlias]) return false;
        var maps = E(sites).OfType<Relation>().Where(row => ReferenceEquals(row.Left, sourceAlias.Right) && ReferenceEquals(row.Right, targetAlias.Right)).ToArray();
        if (maps is not [var map]) return false;
        correspondence = new(new Relation(sourceAlias, targetAlias), map); return true;
    }
    private static bool CheckPortCorrespondence(Relation correspondence, MotifRevision source, MotifRevision target, Relation a, Relation b, Relation sites) =>
        correspondence.Left is Relation { Left: Relation from, Right: Relation to } && correspondence.Right is Relation map &&
        E(source.Join.SiteMap).Any(row => ReferenceEquals(row, from)) && E(target.Join.SiteMap).Any(row => ReferenceEquals(row, to)) &&
        ReferenceEquals(from.Left, a.Left) && ReferenceEquals(to.Left, b.Left) && E(sites).Any(row => ReferenceEquals(row, map)) &&
        ReferenceEquals(map.Left, from.Right) && ReferenceEquals(map.Right, to.Right);
    private bool HasLink(Relation seam, AdaptiveMotifLink link) => seam.Left is Relation { Left: Relation context } &&
        ReferenceEquals(context.Left, Machine.Surface) && ReferenceEquals(context.Right, link.Surface);
    private static bool Matches(AdaptiveMotifLink link, AdaptiveMotifNode from, AdaptiveMotifNode to, IElement fromRole, IElement toRole) =>
        ReferenceEquals(link.From, from) && ReferenceEquals(link.To, to) && ReferenceEquals(link.FromRole, fromRole) && ReferenceEquals(link.ToRole, toRole) ||
        ReferenceEquals(link.From, to) && ReferenceEquals(link.To, from) && ReferenceEquals(link.FromRole, toRole) && ReferenceEquals(link.ToRole, fromRole);
    private Relation Equality(Relation source, Relation target)
    {
        var transformRead = Transform.Apply(source);
        return new(transformRead, Transform.TryReadComplete(transformRead, out var image)
            ? Grammar.Frame.Reduce(image, Inverse(target)) : StructuralConstraint.Open(target));
    }
    private bool CheckEquality(Relation proof, Relation source, Relation target)
    {
        if (proof.Left is not Relation transformed || transformed.Left is not Relation request || !ReferenceEquals(request.Right, source) ||
            !Transform.TryReadComplete(transformed, out var image) || proof.Right is not Relation equality ||
            equality.Left is not Relation { Left: var frame, Right: Relation operands } || !ReferenceEquals(frame, Grammar.Frame.Surface) ||
            !ReferenceEquals(operands.Left, image) || operands.Right is not Relation inverse || !SameWord(inverse, Inverse(target)) ||
            !Grammar.Frame.TryReadResult(equality, out var rest, out _, out var pending)) return false;
        return Closed(rest) && E(rest).Length == 0 && E(pending).Length == 0;
    }
    private bool TryPosition(Relation word, out Position position)
    {
        position = default; if (!Closed(word)) return false; long x = 0, y = 0;
        foreach (var item in E(word))
        {
            if (item is not Relation move || !Grammar.Frame.Direction(move, out var axis, out var forward, out var reach) || !Same(reach, Grammar.Reach)) return false;
            if (ReferenceEquals(axis, Grammar.Frame.Horizontal)) x = checked(x + (forward ? 1 : -1)); else y = checked(y + (forward ? 1 : -1));
        }
        position = new(x, y); return true;
    }
    private Relation Inverse(Relation word) => S(E(word).Reverse().Cast<Relation>().Select(move =>
        (IElement)QualitativeMotionFrame.Move(E(Grammar.Frame.Opposites).Cast<Relation>().Single(row => ReferenceEquals(row.Left, move.Left)).Right, move.Right)).ToArray());
    private readonly record struct Position(long X, long Y) { public Position Turn() => new(-Y, X); }
    private static bool SameWord(Relation a, Relation b) => Closed(a) && Closed(b) && E(a).Length == E(b).Length &&
        E(a).Zip(E(b)).All(pair => pair.First is Relation x && pair.Second is Relation y && ReferenceEquals(x.Left, y.Left) && Same(x.Right, y.Right));
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}

/// <summary>A historical, uncalibrated quarter-turn read of actual active ink.
/// Formal position equality proposes a correspondence; it never joins sites.
/// Graph incidence and every transformed position/chord are checked separately.
/// This is not a Euclidean isometry, flow-state symmetry or wallpaper classification.</summary>
internal sealed class AdaptiveWallpaperComponentSymmetry
{
    private static readonly IEqualityComparer<IElement> Identity = ReferenceEqualityComparer.Instance;
    // Surface = (machine -> actual revision snapshot) ->
    //           ((field -> (frame origin -> transform)) -> (action algebra -> exact-order read))
    // read = (Surface -> (actual field read -> actual graph read)) ->
    //        ((site-map -> (stroke-map -> chord-map)) ->
    //         ((position-proofs -> (chord-proofs -> graph-map-read)) -> (blocked -> unknown)))
    // position-proof = actual site-map row ->
    //                  ((actual source binding -> actual target binding) -> (transform-read -> equality-read))
    // chord-proof = actual chord-map row -> (transform-read -> equality-read)
    // The integer index below is derived search acceleration only. Native
    // transformed-word and cancellation witnesses carry each admitted equality.
    public AdaptiveMotifGrammar Grammar { get; }
    public QualitativeGraphJoin Field { get; }
    public QualitativeGraphJoin TargetField { get; }
    public QualitativeStrokeGraph Graph => Field.Graph;
    public QualitativeStrokeGraph TargetGraph => TargetField.Graph;
    public Relation Bindings => Field.Bindings;
    public Relation TargetBindings => TargetField.Bindings;
    public QualitativePatternTransform Transform { get; }
    public Relation Snapshot { get; }
    public QualitativeActionAlgebra ActionAlgebra { get; }
    public Relation Order { get; }
    public Relation Surface { get; }
    private Relation? evaluated;
    private QualitativeMotionFrame Frame => Graph.Frame;

    public AdaptiveWallpaperComponentSymmetry(AdaptiveMotifGrammar grammar, MotifRevision source, MotifRevision target)
    {
        Grammar = grammar; Snapshot = new(source.Surface, target.Surface); Field = source.Join; TargetField = target.Join;
        Transform = grammar.TurnTransform;
        ActionAlgebra = new(S(Frame.Right, Frame.Up, Frame.Left, Frame.Down));
        Order = ActionAlgebra.CheckOrder(grammar.Turn,
            S(grammar.Turn, grammar.Turn, grammar.Turn, grammar.Turn));
        Surface = new(new Relation(Snapshot, TargetField.Surface),
            new Relation(new Relation(Field.Surface, new Relation(Frame.Origin, Transform.Surface)),
                new Relation(ActionAlgebra.Surface, Order)));
    }

    public Relation Evaluate()
    {
        if (evaluated is not null) return evaluated;
        var blocked = new List<IElement>(); var unknown = new List<IElement>();
        var sourceRead = Graph.Evaluate(); var fieldRead = Field.Evaluate();
        if (!Field.TryReadPassed(fieldRead) || !TargetField.TryReadPassed(TargetField.Evaluate()) ||
            !Graph.TryReadResult(sourceRead, out _, out _, out var cuts, out var pending) ||
            E(cuts).Length != 0 || E(pending).Length != 0 ||
            !TargetGraph.TryReadResult(TargetGraph.Evaluate(), out _, out _, out var targetCuts, out var targetPending) ||
            E(targetCuts).Length != 0 || E(targetPending).Length != 0 ||
            !ActionAlgebra.TryReadOrder(Order, out var orderFour) || !orderFour)
            unknown.Add(new Relation(fieldRead, sourceRead));
        var sites = E(Graph.Sites); var targetSites = E(TargetGraph.Sites);
        var allSites = sites.Concat(targetSites).Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray();
        var rawBindings = E(Bindings).Concat(E(TargetBindings)).Distinct<IElement>(ReferenceEqualityComparer.Instance).OfType<Relation>().ToArray();
        var positions = new Dictionary<IElement, Position>(ReferenceEqualityComparer.Instance);
        var actualBindings = new Dictionary<IElement, Relation>(ReferenceEqualityComparer.Instance);
        var atPosition = new Dictionary<Position, List<IElement>>();
        if (!Closed(Graph.Sites) || !Closed(Bindings) || !Closed(TargetGraph.Sites) || !Closed(TargetBindings) || sites.Length == 0) unknown.Add(Graph.Surface);
        foreach (var site in allSites)
        {
            var found = rawBindings.Where(row => ReferenceEquals(row.Left, site)).ToArray();
            if (found is not [var binding] || binding.Right is not Relation word || !TryPosition(word, out var position))
            { unknown.Add(new Relation(site, Bindings)); continue; }
            actualBindings.Add(site, binding); positions.Add(site, position);
            if (targetSites.Any(target => ReferenceEquals(target, site)))
            {
                if (!atPosition.TryGetValue(position, out var members)) atPosition.Add(position, members = []);
                members.Add(site);
            }
        }
        var edges = new List<Edge>();
        var targetEdges = new List<Edge>();
        foreach (var item in E(Graph.Strokes).Concat(E(TargetGraph.Strokes)).Distinct<IElement>(ReferenceEqualityComparer.Instance))
            if (item is Relation row && QualitativeStrokeGraph.TryReadStroke(row, out _, out var from, out var to, out var word) &&
                positions.ContainsKey(from) && positions.ContainsKey(to) && TryPosition(word, out var delta))
            {
                var edge = new Edge(row, from, to, word, delta);
                if (E(Graph.Strokes).Any(actual => ReferenceEquals(actual, row))) edges.Add(edge);
                if (E(TargetGraph.Strokes).Any(actual => ReferenceEquals(actual, row))) targetEdges.Add(edge);
            }
            else unknown.Add(item);
        var incidence = allSites.ToDictionary<IElement, IElement, Incidence[]>(site => site,
            site => edges.Concat(targetEdges).DistinctBy(edge => edge.Row, ReferenceEqualityComparer.Instance).Where(edge => ReferenceEquals(edge.From, site) || ReferenceEquals(edge.To, site))
                .SelectMany(edge => (ReferenceEquals(edge.From, site)
                        ? new[] { new Incidence(true, edge.Delta, positions[edge.To]) } : [])
                    .Concat(ReferenceEquals(edge.To, site)
                        ? new[] { new Incidence(false, edge.Delta, positions[edge.From]) } : [])).ToArray(),
            ReferenceEqualityComparer.Instance);
        var siteRows = new List<IElement>(); var positionProofs = new List<IElement>();
        var selectedSites = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        foreach (var site in sites)
        {
            if (!positions.TryGetValue(site, out var position)) continue;
            var sourceBinding = actualBindings[site];
            var transformed = Transform.Apply((Relation)sourceBinding.Right);
            if (!Transform.TryReadComplete(transformed, out _)) { unknown.Add(transformed); continue; }
            var candidates = atPosition.TryGetValue(position.Turn(), out var found) ? found.ToArray() : [];
            if (candidates.Length > 1)
                candidates = candidates.Where(target => SameIncidence(incidence[site].Select(part => part.Turn()).ToArray(), incidence[target])).ToArray();
            if (candidates.Length == 0) { blocked.Add(new Relation(sourceBinding, transformed)); continue; }
            if (candidates is not [var target]) { unknown.Add(new Relation(sourceBinding, S(candidates))); continue; }
            var row = new Relation(site, target); siteRows.Add(row); selectedSites.Add(site, target);
            var targetBinding = actualBindings[target];
            positionProofs.Add(new Relation(row, new Relation(new Relation(sourceBinding, targetBinding),
                Equality(transformed, (Relation)targetBinding.Right))));
        }
        var strokeRows = new List<IElement>(); var wordRows = new List<IElement>(); var chordProofs = new List<IElement>();
        var selectedWords = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
        var byStart = targetEdges.GroupBy(edge => edge.From, Identity)
            .ToDictionary(group => group.Key, group => group.ToArray(), Identity);
        foreach (var edge in edges)
        {
            if (!selectedSites.TryGetValue(edge.From, out var from) || !selectedSites.TryGetValue(edge.To, out var to)) continue;
            var candidates = byStart.TryGetValue(from, out var available)
                ? available.Where(target => ReferenceEquals(target.To, to) && target.Delta == edge.Delta.Turn()).ToArray() : [];
            if (candidates.Length == 0) { blocked.Add(new Relation(edge.Row, new Relation(from, to))); continue; }
            if (candidates is not [var target]) { unknown.Add(new Relation(edge.Row, S(candidates.Select(item => (IElement)item.Row).ToArray()))); continue; }
            strokeRows.Add(new Relation(edge.Row, target.Row));
            if (selectedWords.TryGetValue(edge.Word, out var existing))
            { if (!ReferenceEquals(existing.Right, target.Word)) unknown.Add(new Relation(existing, target.Word)); continue; }
            var wordRow = new Relation(edge.Word, target.Word); selectedWords.Add(edge.Word, wordRow); wordRows.Add(wordRow);
            chordProofs.Add(new Relation(wordRow, Equality(Transform.Apply(edge.Word), target.Word)));
        }
        var siteMap = S(siteRows.ToArray()); var strokeMap = S(strokeRows.ToArray()); var chordMap = S(wordRows.ToArray());
        var mapRead = blocked.Count == 0 && unknown.Count == 0
            ? Graph.EvaluateMap(TargetGraph, siteMap, strokeMap, chordMap)
            : StructuralConstraint.Open(new Relation(Graph.Surface, new Relation(siteMap, new Relation(strokeMap, chordMap))));
        if (blocked.Count == 0 && unknown.Count == 0 && !Graph.TryReadMapPassed(TargetGraph, mapRead)) unknown.Add(mapRead);
        return evaluated = new(new Relation(Surface, new Relation(fieldRead, sourceRead)),
            new Relation(new Relation(siteMap, new Relation(strokeMap, chordMap)),
                new Relation(new Relation(S(positionProofs.ToArray()), new Relation(S(chordProofs.ToArray()), mapRead)),
                    new Relation(S(blocked.ToArray()), S(unknown.ToArray())))));
    }

    public bool TryReadResult(Relation read, out Relation siteMap, out Relation strokeMap, out Relation chordMap,
        out Relation positionProofs, out Relation chordProofs, out Relation graphMap, out Relation blocked, out Relation unknown)
    {
        siteMap = strokeMap = chordMap = positionProofs = chordProofs = graphMap = blocked = unknown = null!;
        if (read.Left is not Relation { Left: var owner, Right: Relation inputs } || !ReferenceEquals(owner, Surface) ||
            !ReferenceEquals(inputs.Left, Field.Evaluate()) || !ReferenceEquals(inputs.Right, Graph.Evaluate()) ||
            read.Right is not Relation { Left: Relation { Left: Relation sites, Right: Relation { Left: Relation strokes, Right: Relation words } },
                Right: Relation { Left: Relation { Left: Relation positions, Right: Relation { Left: Relation chords, Right: Relation map } },
                    Right: Relation { Left: Relation refused, Right: Relation pending } } } ||
            !new[] { sites, strokes, words, positions, chords, refused, pending }.All(Closed)) return false;
        siteMap = sites; strokeMap = strokes; chordMap = words; positionProofs = positions; chordProofs = chords;
        graphMap = map; blocked = refused; unknown = pending; return true;
    }

    /// <summary>Checks actual captured graph, input bindings, correspondence
    /// rows, transformed words and cancellation proofs. No favourable flag or
    /// cached diagnostic count can stand in for this evidence.</summary>
    public bool TryReadPassed(Relation read)
    {
        if (!TryReadResult(read, out var sites, out var strokes, out var words, out var positions, out var chords, out var graphMap, out var blocked, out var unknown) ||
            E(blocked).Length != 0 || E(unknown).Length != 0 || E(Graph.Sites).Length == 0 ||
            !Field.TryReadPassed(Field.Evaluate()) || !TargetField.TryReadPassed(TargetField.Evaluate()) || !ActionAlgebra.TryReadOrder(Order, out var orderFour) || !orderFour ||
            !Graph.TryReadResult(Graph.Evaluate(), out _, out _, out var cuts, out var pending) || E(cuts).Length != 0 || E(pending).Length != 0 ||
            graphMap.Left is not Relation { Right: Relation actualMaps } || !ReferenceEquals(actualMaps.Left, sites) ||
            actualMaps.Right is not Relation actualRest || !ReferenceEquals(actualRest.Left, strokes) || !ReferenceEquals(actualRest.Right, words) ||
            !Graph.TryReadMapPassed(TargetGraph, graphMap) || E(positions).Length != E(sites).Length || E(chords).Length != E(words).Length) return false;
        foreach (var item in E(sites))
        {
            if (item is not Relation row) return false;
            var proofs = E(positions).OfType<Relation>().Where(proof => ReferenceEquals(proof.Left, row)).ToArray();
            if (proofs is not [var proof] || proof.Right is not Relation { Left: Relation context, Right: Relation equality } ||
                context.Left is not Relation a || context.Right is not Relation b ||
                !E(Bindings).Any(binding => ReferenceEquals(binding, a)) || !E(TargetBindings).Any(binding => ReferenceEquals(binding, b)) ||
                !ReferenceEquals(a.Left, row.Left) || !ReferenceEquals(b.Left, row.Right) ||
                a.Right is not Relation before || b.Right is not Relation after || !CheckEquality(equality, before, after)) return false;
        }
        foreach (var item in E(words))
        {
            if (item is not Relation row || row.Left is not Relation before || row.Right is not Relation after) return false;
            var proofs = E(chords).OfType<Relation>().Where(proof => ReferenceEquals(proof.Left, row)).ToArray();
            if (proofs is not [var proof] || proof.Right is not Relation equality || !CheckEquality(equality, before, after)) return false;
        }
        return true;
    }

    private Relation Equality(Relation transformRead, Relation target)
    {
        if (!Transform.TryReadComplete(transformRead, out var image)) return new(transformRead, StructuralConstraint.Open(target));
        return new(transformRead, Frame.Reduce(image, Inverse(target)));
    }
    private bool CheckEquality(Relation proof, Relation source, Relation target)
    {
        if (proof.Left is not Relation transformRead || transformRead.Left is not Relation request || !ReferenceEquals(request.Right, source) ||
            !Transform.TryReadComplete(transformRead, out var image) || proof.Right is not Relation equality ||
            equality.Left is not Relation { Left: var frame, Right: Relation operands } || !ReferenceEquals(frame, Frame.Surface) ||
            !ReferenceEquals(operands.Left, image) || operands.Right is not Relation inverse || !SameWord(inverse, Inverse(target)) ||
            !Frame.TryReadResult(equality, out var rest, out _, out var pending)) return false;
        return Closed(rest) && E(rest).Length == 0 && E(pending).Length == 0;
    }
    private Relation Inverse(Relation word) => S(E(word).Reverse().Cast<Relation>().Select(move =>
        (IElement)QualitativeMotionFrame.Move(E(Frame.Opposites).Cast<Relation>().Single(row => ReferenceEquals(row.Left, move.Left)).Right, move.Right)).ToArray());
    private bool TryPosition(Relation word, out Position position)
    {
        position = default; if (!Closed(word)) return false;
        long x = 0, y = 0;
        foreach (var item in E(word))
        {
            if (item is not Relation move || !Frame.Direction(move, out var axis, out var forward, out var reach) ||
                !Same(reach, Grammar.Reach)) return false;
            if (ReferenceEquals(axis, Frame.Horizontal)) x = checked(x + (forward ? 1 : -1));
            else y = checked(y + (forward ? 1 : -1));
        }
        position = new(x, y); return true;
    }
    private static bool SameIncidence(Incidence[] a, Incidence[] b)
    {
        if (a.Length != b.Length) return false;
        var remaining = b.ToList();
        foreach (var item in a) { var index = remaining.IndexOf(item); if (index < 0) return false; remaining.RemoveAt(index); }
        return remaining.Count == 0;
    }
    private readonly record struct Position(long X, long Y) { public Position Turn() => new(-Y, X); }
    private readonly record struct Incidence(bool Outward, Position Delta, Position Other)
    { public Incidence Turn() => new(Outward, Delta.Turn(), Other.Turn()); }
    private sealed record Edge(Relation Row, IElement From, IElement To, Relation Word, Position Delta);
    private static bool SameWord(Relation a, Relation b) => Closed(a) && Closed(b) && E(a).Length == E(b).Length &&
        E(a).Zip(E(b)).All(pair => pair.First is Relation x && pair.Second is Relation y && ReferenceEquals(x.Left, y.Left) && Same(x.Right, y.Right));
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
