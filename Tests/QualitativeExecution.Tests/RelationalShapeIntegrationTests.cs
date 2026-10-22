using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Streams = Gojo.Experiments.QualitativeFrieze.Composition.StructuralEvidenceStreams;

namespace QualitativeExecution.Tests;

public sealed class RelationalShapeIntegrationTests
{
    [Fact]
    public void Standalone_packet_withholds_bucket_lists_and_teacher_mechanics_but_keeps_spatial_facts()
    {
        var privateCause = new Relation(new Placeholder(), new Placeholder());
        var source = Source(privateCause, [(0, 0), (8, 0), (8, 3), (0, 3), (0, 0)]);
        var analysis = JointShapeDescription.Analyze(source);
        var packet = RelationalShapeExperiment.Publish(analysis); var message = packet.Message;
        Assert.False(Closed((Relation)message.Horizontal.Right));
        Assert.False(Closed((Relation)message.Vertical.Right));
        Assert.Empty(E((Relation)message.Horizontal.Right));
        Assert.Empty(E((Relation)message.Vertical.Right));
        foreach (var axis in new[] { packet.Spatial.Horizontal, packet.Spatial.Vertical })
        {
            var facts = (Relation)axis.Right;
            Assert.Equal(2, E((Relation)facts.Left).Length);
            Assert.Single(E((Relation)facts.Right));
        }
        Assert.True(ReferenceEquals(packet.Spatial.Frame, message.Frame));
        Assert.True(ReferenceEquals(packet.Equivalence.Message, message));
        var participants = Reachable(packet.Surface).ToHashSet(ReferenceEqualityComparer.Instance);
        var privateParts = new IElement[] { privateCause, source.Graph.Surface, source.Graph.Frame.Surface, source.Bindings,
            source.Provenance, analysis.Audit, analysis.SourceMap, analysis.Message.Horizontal, analysis.Message.Vertical }
            .Concat(E(source.Graph.Sites)).Concat(E(source.Graph.Strokes))
            .Concat(E(source.Bindings).Cast<Relation>().Select(row => row.Right));
        Assert.False(privateParts.Any(participants.Contains), "The receiver packet must not retain teacher objects or analysis ancestry.");
        Assert.False(participants.Any(item => item is Carrier or CarrierLocation));
        var roles = E(message.Roles).ToHashSet(ReferenceEqualityComparer.Instance);
        Assert.False(participants.OfType<Relation>().Any(row => QualitativeStrokeGraph.TryReadStroke(row,
            out _, out var from, out var to, out var word) && roles.Contains(from) && roles.Contains(to) &&
            E(word).OfType<Relation>().Any(move => message.Frame.Direction(move, out _, out _, out _))),
            "An advertised stroke between joint roles would leak the edge pairing this experiment is meant to reconstruct.");
    }

    [Fact]
    public void A_companion_opposition_claim_keeps_exact_closure_identity_after_repeated_transport()
    {
        var first = Packet(Gap());
        var packet = RelationalShapeExperiment.Import(first.Surface); var message = packet.Message;
        Assert.False(ReferenceEquals(first.Message.ClosureDefault, message.ClosureDefault));
        var observation = packet.Streams.Read(packet.Streams.Observations).Single(stream =>
            ReferenceEquals(stream.Claim, message.ClosureDefault));
        var fallback = packet.Streams.Read(packet.Streams.Defaults).Single();
        Assert.True(ReferenceEquals(observation.Claim, message.ClosureDefault));
        Assert.True(ReferenceEquals(fallback.Claim, message.ClosureDefault));
        Assert.True(ReferenceEquals(observation.Opposition, packet.Streams.Ladder.Strong));
        var context = Streams.SelectClosure(message, packet.Streams);
        Assert.False(context.ShouldComplete); Assert.True(context.PreserveObservedGap);
        Assert.True(context.Read.Opposes); Assert.True(context.Read.Conflicted);
        Assert.True(Reachable(context.Surface).Any(item => ReferenceEquals(item, fallback.Surface)));
        Assert.True(packet.Spatial.Horizontal.Left is Relation && ReferenceEquals(packet.Spatial.Horizontal.Left, message.Frame.Horizontal));
    }

    [Fact]
    public void Separate_spatial_facts_can_derive_a_layout_and_reconstruct_without_importing_source_edges()
    {
        var source = Source(new Placeholder(), [(0, 0), (8, 0), (8, 3), (0, 3), (0, 0)]);
        var packet = Packet(source); var message = packet.Message;
        var inference = RelationalSpatialEvidence.Infer(message, packet.Spatial, S(), 0);
        Assert.True(inference.Complete); Assert.NotNull(inference.Layout);
        Assert.Empty(E(inference.Contradictions)); Assert.Empty(E(inference.Assumptions));
        var layout = inference.Layout!; var positions = RelationalShapeSearch.Placement(layout, [2], [1]);
        var construction = JointShapeReconstruction.Construction(layout, positions, S(), S(), inference.Witness);
        var candidate = JointShapeReconstruction.Propose(layout, 0, construction: construction);
        Assert.Equal(new[] { "0:1", "0:3", "1:2", "2:3" }, Edges(message, candidate));
        Assert.Equal(8, candidate.SlotEdges.Count);
        Assert.Empty(E(candidate.AssumedStrokes));
        var spatial = RelationalSpatialEvidence.Assess(packet.Spatial, candidate);
        Assert.NotEmpty(E(spatial.Supported)); Assert.Empty(E(spatial.Contradicted)); Assert.Empty(E(spatial.Open));
        Assert.False(Closed((Relation)message.Horizontal.Right), "A derived layout must not rewrite the original open evidence stream.");
        Assert.True(Reachable(candidate.Surface).Any(item => ReferenceEquals(item, inference.Witness)));
        Assert.False(Reachable(candidate.Surface).Any(item => ReferenceEquals(item, source.Graph.Surface)));
    }

    [Fact]
    public void Explicit_repair_permission_changes_output_without_rewriting_observed_rays_or_default_evidence()
    {
        var source = Gap(); var originalStrokes = E(source.Graph.Strokes); var packet = Packet(source); var message = packet.Message;
        var defaultContext = Streams.SelectClosure(message, packet.Streams); var ladder = packet.Streams.Ladder;
        var goal = new Streams.Stream(message.ClosureDefault, ladder.Strong, ladder.None,
            message.Roles, S(message.ClosureDefault), message.ClosureDefault);
        var requested = new Streams.Bundle(ladder, packet.Streams.Observations, packet.Streams.Hypotheses,
            packet.Streams.Defaults, S(goal.Surface));
        var repairContext = Streams.SelectClosure(message, requested);
        var inference = RelationalSpatialEvidence.Infer(message, packet.Spatial, S(), 0); Assert.True(inference.Complete);
        var layout = inference.Layout!;
        var positions = RelationalShapeSearch.Placement(layout,
            Enumerable.Repeat(1, E((Relation)layout.Horizontal.Right).Length - 1).ToArray(),
            Enumerable.Repeat(1, E((Relation)layout.Vertical.Right).Length - 1).ToArray());
        JointShapeReconstruction.Candidate Propose(Streams.ClosureSelection context) =>
            JointShapeReconstruction.Propose(layout, 0, construction: JointShapeReconstruction.Construction(layout,
                positions, S(), context.ShouldComplete ? S(message.ClosureDefault) : S(), context.Surface));
        var open = Propose(defaultContext); var repaired = Propose(repairContext);
        Assert.Empty(E(open.AssumedStrokes)); Assert.Single(E(repaired.AssumedStrokes));
        Assert.Equal(5, E(open.Pairings).Length); Assert.Equal(5, E(repaired.Pairings).Length);
        Assert.Equal(open.SlotEdges.Count, repaired.SlotEdges.Count);
        var bridge = E(repaired.AssumedStrokes)[0];
        Assert.False(repaired.SlotEdges.Values.Any(edge => ReferenceEquals(edge, bridge)));
        Assert.True(repairContext.Read.Opposes); Assert.True(repairContext.PreserveObservedGap);
        Assert.Equal(2, message.JointReads.Count(joint => E(joint.Rays).Length == 1));
        Assert.True(originalStrokes.SequenceEqual(E(source.Graph.Strokes), ReferenceEqualityComparer.Instance));
        Assert.True(Reachable(repaired.Surface).Any(item => ReferenceEquals(item, goal.Surface)));
    }

    [Fact]
    public void Sparse_search_keeps_guessed_orders_out_of_the_original_core_observations()
    {
        var packet = Packet(Source(new Placeholder(),
            [(0, 0), (6, 0), (6, 2), (4, 2), (4, 4), (2, 4), (2, 6), (0, 6), (0, 0)]));
        var message = packet.Message;
        var sparse = RelationalSpatialEvidence.Select(packet.Spatial, message, "sparse");
        var inference = RelationalSpatialEvidence.Infer(message, sparse, S(), 1);
        Assert.True(inference.Complete); Assert.NotEmpty(E(inference.Assumptions));
        var run = RelationalShapeSearch.Run(message,
            new("sparse", "Sparse evidence", sparse, null, Streams.SelectClosure(message, packet.Streams)), 6);
        Assert.True(run.Evaluated > 0); Assert.Equal(3, run.Retained.Count);
        Assert.True(run.Retained.Contains(run.Best));
        foreach (var trial in run.Retained)
        {
            Assert.False(trial.Core.Claims.Any(claim => claim.Kind == "positions"),
                "The core critic must not promote the inferred complete bucket list into an original observation.");
            Assert.Empty(E(trial.Spatial.Contradicted));
            Assert.NotEmpty(E(trial.Spatial.Supported));
        }
        Assert.Empty(E((Relation)message.Horizontal.Right)); Assert.Empty(E((Relation)message.Vertical.Right));
        Assert.True(Reachable(run.Surface).Any(item => ReferenceEquals(item, sparse.Surface)));
        Assert.True(Reachable(inference.Witness).Any(item => ReferenceEquals(item, inference.Assumptions)));
    }

    [Fact]
    public void Shared_axis_projection_does_not_merge_joint_identity_or_add_a_path_order()
    {
        var packet = Packet(Source(new Placeholder(), [(0, 0), (8, 0), (8, 3), (0, 3), (0, 0)]));
        var message = packet.Message; var facts = (Relation)packet.Spatial.Horizontal.Right;
        var aligned = E((Relation)facts.Left).Cast<Relation>().First(); var members = E(aligned);
        Assert.Equal(2, members.Length); Assert.False(ReferenceEquals(members[0], members[1]));
        Assert.True(message.JointReads.Any(joint => ReferenceEquals(joint.Role, members[0])));
        Assert.True(message.JointReads.Any(joint => ReferenceEquals(joint.Role, members[1])));
        var inferred = RelationalSpatialEvidence.Infer(message, packet.Spatial, S(), 0);
        var candidate = JointShapeReconstruction.Propose(inferred.Layout!, 0,
            construction: JointShapeReconstruction.Construction(inferred.Layout!, RelationalShapeSearch.Placement(inferred.Layout!, [1], [1]), S(), S(), inferred.Witness));
        Assert.False(ReferenceEquals(candidate.Sites[members[0]], candidate.Sites[members[1]]));
        Assert.Equal(4, E(candidate.Source.Graph.Sites).Length);
        Assert.True(candidate.Sites.Values.Distinct(ReferenceEqualityComparer.Instance).Count() == 4);
        Assert.Empty(E((Relation)message.Horizontal.Right));
    }

    [Fact]
    public void Retained_extent_feedback_changes_a_realization_without_changing_the_observed_connections()
    {
        var packet = Packet(RelationalShapeCorpus.Cases().Single(item => item.Id == "R022").Source);
        var message = packet.Message;
        var focus = new RelationalShapeSearch.Focus("evidence", "Evidence", packet.Spatial,
            packet.Equivalence, Streams.SelectClosure(message, packet.Streams));
        var baseline = RelationalShapeSearch.Run(message, focus, 8);
        var revised = RelationalShapeSearch.Run(message, focus, 8, extentRepair: true);
        Assert.True(baseline.Best.HardConflicts > revised.Best.HardConflicts);
        Assert.Equal(0, revised.Best.HardConflicts);
        Assert.Equal(Edges(message, baseline.Best.Candidate), Edges(message, revised.Best.Candidate));
        Assert.Equal(0, revised.Best.UnpairedRays);
        Assert.False(revised.Best.Equivalence!.Claims.Any(claim => claim.State == "Contradicted"));
        Assert.Empty(E((Relation)message.Horizontal.Right));
        Assert.Empty(E((Relation)message.Vertical.Right));
    }

    [Fact]
    public void Proposed_ray_partners_are_accepted_only_when_their_projection_constraints_agree_with_evidence()
    {
        var packet = Packet(Source(new Placeholder(), [(0, 0), (8, 0), (8, 3), (0, 3), (0, 0)]));
        var message = packet.Message; var roles = E(message.Roles); var frame = message.Frame;
        var legal = S(new Relation(Slot(message, roles[0], frame.Right), Slot(message, roles[1], frame.Left)));
        var inferred = RelationalSpatialEvidence.Infer(message, packet.Spatial, legal, 0);
        Assert.True(inferred.Complete); Assert.Empty(E(inferred.Contradictions)); Assert.NotEmpty(E(inferred.Assumptions));
        var layout = inferred.Layout!;
        var construction = JointShapeReconstruction.Construction(layout, RelationalShapeSearch.Placement(layout, [2], [1]), legal, S(), inferred.Witness);
        var candidate = JointShapeReconstruction.Propose(layout, 0, construction: construction);
        var pair = (Relation)E(legal)[0];
        Assert.True(ReferenceEquals(candidate.SlotEdges[pair.Left], candidate.SlotEdges[pair.Right]));
        Assert.True(Reachable(candidate.Surface).Any(item => ReferenceEquals(item, legal)));
        // Opposite horizontal arms on different known vertical levels would
        // require an invalid equality. Their rejection retains the proposed
        // relation and the actual contradiction instead of moving an endpoint.
        var wrong = S(new Relation(Slot(message, roles[0], frame.Right), Slot(message, roles[2], frame.Left)));
        var blocked = RelationalSpatialEvidence.Infer(message, packet.Spatial, wrong, 0);
        Assert.False(blocked.Complete); Assert.Null(blocked.Layout); Assert.NotEmpty(E(blocked.Contradictions));
        Assert.True(Reachable(blocked.Witness).Any(item => ReferenceEquals(item, wrong)));
        Assert.True(Reachable(blocked.Witness).Any(item => ReferenceEquals(item, blocked.Contradictions)));
    }

    private static RelationalShapeExperiment.Packet Packet(NativeLatticeSources.SourceCase source) =>
        RelationalShapeExperiment.Publish(JointShapeDescription.Analyze(source));
    private static NativeLatticeSources.SourceCase Gap() => Source(new Placeholder(),
        [(0, 0), (4, 0), (4, 3), (3, 3)], [(1, 3), (0, 3), (0, 0)]);
    private static IElement Slot(JointShapeDescription.Message message, IElement role, IElement direction) =>
        E(message.JointReads.Single(joint => ReferenceEquals(joint.Role, role)).Rays).Cast<Relation>()
            .Single(row => E((Relation)row.Right).Length == 1 && ReferenceEquals(E((Relation)row.Right)[0], direction)).Left;
    private static string[] Edges(JointShapeDescription.Message message, JointShapeReconstruction.Candidate candidate)
    {
        var roles = E(message.Roles); var indices = roles.Select((role, index) => (Site: candidate.Sites[role], index))
            .ToDictionary(row => row.Site, row => row.index, ReferenceEqualityComparer.Instance);
        return E(candidate.Source.Graph.Strokes).Cast<Relation>().Select(edge =>
        {
            QualitativeStrokeGraph.TryReadStroke(edge, out _, out var a, out var b, out _);
            return Math.Min(indices[a], indices[b]) + ":" + Math.Max(indices[a], indices[b]);
        }).Order(StringComparer.Ordinal).ToArray();
    }
    private static NativeLatticeSources.SourceCase Source(IElement privateCause, params (int X, int Y)[][] paths)
    {
        var author = new LatticeLadderAuthor(); var sites = new Dictionary<(int, int), IElement>();
        var bindings = new List<IElement>(); var edges = new List<IElement>();
        IElement Site((int X, int Y) point)
        {
            if (sites.TryGetValue(point, out var prior)) return prior;
            var site = new Placeholder(); sites.Add(point, site); bindings.Add(new Relation(site, author.Word(point.X, point.Y))); return site;
        }
        foreach (var path in paths)
            for (var index = 0; index < path.Length - 1; index++)
            {
                var a = path[index]; var b = path[index + 1];
                edges.Add(QualitativeStrokeGraph.Stroke(privateCause, Site(a), Site(b), author.Word(b.X - a.X, b.Y - a.Y)));
            }
        var graph = new QualitativeStrokeGraph(author.Frame, S(sites.Values.ToArray()), S(edges.ToArray()));
        return new("anonymous", "Anonymous target", "An independently executed target without a receiver family label.",
            graph, S(bindings.ToArray()), graph.Strokes, new Relation(privateCause, graph.Surface), true);
    }
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var start, out var end) && start.Equals(end);
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item))
        {
            if (!visited.Add(item)) continue;
            yield return item;
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
