using Gojo.Core;
using Gojo.Patterns;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class JointShapeReconstructionTests
{
    [Fact]
    public void Transported_anonymous_message_reconstructs_without_source_graph_or_factory_access()
    {
        var source = Source([(0, 0), (4, 0), (4, 2), (0, 2), (0, 0)]);
        var analysis = JointShapeDescription.Analyze(source);
        var message = Import(RoundTrip(analysis.Message.Surface));
        var output = JointShapeReconstruction.ThreeAttempts(message)[0];
        Assert.Equal(ExpectedPairs(analysis, E(source.Graph.Sites)), ActualPairs(analysis, message, output, E(source.Graph.Sites)));
        var participants = Reachable(output.Surface).ToHashSet(ReferenceEqualityComparer.Instance);
        Assert.False(participants.Contains(source.Graph.Surface), "A receiver output must not retain its teacher graph.");
        Assert.False(participants.Contains(source.Bindings), "Native teacher positions are not decoder inputs.");
        Assert.False(E(source.Graph.Sites).Any(participants.Contains));
        Assert.False(E(source.Graph.Strokes).Any(participants.Contains));
        Assert.False(participants.Any(item => item is Carrier or CarrierLocation));
        Assert.True(output.Assessment.Claims.Any(claim => claim.Kind == "closed" && claim.State == "Supported"));
    }

    [Fact]
    public void Closing_a_source_gap_adds_a_separate_assumed_bridge_without_revising_observed_rays()
    {
        var source = Source([(0, 0), (4, 0), (4, 3), (3, 3)], [(1, 3), (0, 3), (0, 0)]);
        var originalEdges = E(source.Graph.Strokes); var analysis = JointShapeDescription.Analyze(source);
        var message = Import(analysis.Message.Surface);
        var endpointCount = message.JointReads.Count(joint => E(joint.Rays).Length == 1);
        var output = JointShapeReconstruction.ThreeAttempts(message)[0];
        Assert.Equal(5, originalEdges.Length);
        Assert.Equal(2, endpointCount);
        Assert.Equal(5, E(output.Pairings).Length);
        Assert.Equal(1, E(output.AssumedStrokes).Length);
        Assert.Equal(6, E(output.Source.Graph.Strokes).Length);
        var bridge = E(output.AssumedStrokes)[0];
        Assert.False(output.SlotEdges.Values.Any(edge => ReferenceEquals(edge, bridge)), "A default-induced bridge is not an observed ray pairing.");
        Assert.True(output.Assessment.Claims.Any(claim => claim.Kind == "assumptions" && claim.State == "Open"));
        Assert.Equal(ExpectedPairs(analysis, E(source.Graph.Sites)), ActualPairs(analysis, message, output, E(source.Graph.Sites), observedOnly: true));
        Assert.True(originalEdges.SequenceEqual(E(source.Graph.Strokes), ReferenceEqualityComparer.Instance));
        Assert.Equal(endpointCount, message.JointReads.Count(joint => E(joint.Rays).Length == 1));
    }

    [Fact]
    public void An_open_axis_order_prevents_invented_positions_and_passages()
    {
        var analysis = JointShapeDescription.Analyze(Source([(0, 0), (2, 0), (2, 2), (0, 2), (0, 0)]));
        var original = analysis.Message;
        var buckets = (Relation)original.Horizontal.Right;
        var port = Replace(original.Surface, new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance)
            { [buckets] = StructuralConstraint.Open(E(buckets)) });
        var message = Import(port);
        var output = JointShapeReconstruction.Propose(message, 0);
        var assessment = JointShapeReconstruction.Assess(message, output);
        Assert.False(Closed((Relation)message.Horizontal.Right));
        Assert.Equal(0, output.Positions.Count);
        Assert.Equal(0, E(output.Pairings).Length);
        Assert.Equal(0, E(output.Source.Graph.Strokes).Length);
        Assert.True(E(output.Unresolved).Length != 0);
        Assert.True(assessment.Claims.Any(claim => claim.State == "Open"));
    }

    [Fact]
    public void An_unrecognized_closed_position_term_does_not_project_to_a_concrete_zero()
    {
        var source = Source([(0, 0), (2, 0), (2, 2), (0, 2), (0, 0)]);
        var site = E(source.Graph.Sites)[1];
        var bad = source with { Bindings = S(E(source.Bindings).Cast<Relation>().Select(row => (IElement)
            (ReferenceEquals(row.Left, site) ? new Relation(site, S(new Placeholder())) : row)).ToArray()) };
        var message = Import(JointShapeDescription.Analyze(bad).Message.Surface);
        Assert.True(!Closed((Relation)message.Horizontal.Right) || !Closed((Relation)message.Vertical.Right));
        var output = JointShapeReconstruction.Propose(message, 0);
        Assert.Equal(0, output.Positions.Count);
        Assert.Equal(0, E(output.Source.Graph.Strokes).Length);
        Assert.True(E(output.Unresolved).Length != 0);
    }

    [Fact]
    public void Later_policy_attempts_preserve_earlier_candidates_and_their_native_feedback()
    {
        var source = Source([(0, 0), (2, 0), (2, 2), (0, 2), (0, 0)]);
        var message = Import(JointShapeDescription.Analyze(source).Message.Surface);
        var attempts = JointShapeReconstruction.ThreeAttempts(message);
        Assert.Equal(3, attempts.Count);
        var first = attempts[0]; var originalGraph = first.Source.Graph.Surface; var originalPairings = first.Pairings;
        Assert.True(Reachable(attempts[1].Surface).Any(item => ReferenceEquals(item, first.Assessment.Surface)));
        Assert.True(Reachable(attempts[2].Surface).Any(item => ReferenceEquals(item, attempts[1].Assessment.Surface)));
        Assert.True(ReferenceEquals(originalGraph, first.Source.Graph.Surface));
        Assert.True(ReferenceEquals(originalPairings, first.Pairings));
        Assert.False(ReferenceEquals(first.Source.Graph.Surface, attempts[1].Source.Graph.Surface));
        Assert.True(ReferenceEquals(first.Assessment.Output, first));
        Assert.True(attempts.All(attempt => Reachable(attempt.Assessment.Surface).Any(item => ReferenceEquals(item, attempt.Surface))));
    }

    [Fact]
    public void Native_quarter_turn_evidence_rejects_a_stretched_sample_even_when_incidence_is_preserved()
    {
        var source = Source([(0, 0), (2, 0), (2, 2), (0, 2), (0, 0)]);
        var analysis = JointShapeDescription.Analyze(source); var message = Import(analysis.Message.Surface);
        var attempts = JointShapeReconstruction.ThreeAttempts(message);
        Assert.Equal(ActualPairs(analysis, message, attempts[0], E(source.Graph.Sites)), ActualPairs(analysis, message, attempts[1], E(source.Graph.Sites)));
        Assert.True(attempts[0].Assessment.Claims.Any(claim => claim.Kind == "symmetry" && claim.State == "Supported"));
        Assert.True(attempts[1].Assessment.Claims.Any(claim => claim.Kind == "symmetry" && claim.State == "Violated"));
        Assert.False(ReferenceEquals(JointShapeReconstruction.Best(attempts), attempts[1]));
    }

    [Fact]
    public void Reordering_graph_sites_strokes_joints_and_rays_does_not_supply_hidden_edge_pairing_hints()
    {
        var source = Source([(0, 0), (4, 0), (4, 3), (3, 3), (3, 1), (1, 1), (1, 3), (0, 3), (0, 0)]);
        var actualSites = E(source.Graph.Sites); var firstAnalysis = JointShapeDescription.Analyze(source);
        var firstMessage = Import(firstAnalysis.Message.Surface);
        var first = JointShapeReconstruction.ThreeAttempts(firstMessage)[0];
        var reordered = source with { Graph = new(source.Graph.Frame, S(actualSites.Reverse().ToArray()), S(E(source.Graph.Strokes).Reverse().ToArray())),
            Bindings = S(E(source.Bindings).Reverse().ToArray()) };
        var otherAnalysis = JointShapeDescription.Analyze(reordered);
        var replacements = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance)
            { [otherAnalysis.Message.Joints] = S(E(otherAnalysis.Message.Joints).Reverse().ToArray()) };
        // Reverse the ray scopes before replacing the outer joint inventory;
        // the replacement itself must retain these modified native rows.
        var rows = otherAnalysis.Message.JointReads.Reverse().Select(joint => (IElement)new Relation(joint.Role,
            new Relation(Closed(joint.Rays) ? S(E(joint.Rays).Reverse().ToArray()) : StructuralConstraint.Open(E(joint.Rays).Reverse().ToArray()),
                new Relation(joint.Opposed, joint.Perpendicular)))).ToArray();
        replacements[otherAnalysis.Message.Joints] = S(rows);
        var otherMessage = Import(Replace(otherAnalysis.Message.Surface, replacements));
        var other = JointShapeReconstruction.ThreeAttempts(otherMessage)[0];
        var expected = ExpectedPairs(firstAnalysis, actualSites);
        Assert.Equal(expected, ActualPairs(firstAnalysis, firstMessage, first, actualSites));
        Assert.Equal(expected, ActualPairs(otherAnalysis, otherMessage, other, actualSites));
        Assert.Equal(first.Assessment.Claims.Count(claim => claim.State == "Violated"), other.Assessment.Claims.Count(claim => claim.State == "Violated"));
    }

    [Fact]
    public void Separate_component_hints_survive_transport_and_do_not_become_a_bridge_between_closed_contours()
    {
        var source = TwoSquares(); var analysis = JointShapeDescription.Analyze(source); var message = Import(analysis.Message.Surface);
        var output = JointShapeReconstruction.ThreeAttempts(message)[0];
        Assert.Equal(2, E(message.Components).Length);
        Assert.Equal(8, E(output.Source.Graph.Strokes).Length);
        Assert.Equal(0, E(output.AssumedStrokes).Length);
        var componentClaims = output.Assessment.Claims.Where(claim => claim.Kind == "components").ToArray();
        Assert.Equal(2, componentClaims.Length);
        Assert.True(componentClaims.All(claim => claim.State == "Supported"));
        Assert.Equal(ExpectedPairs(analysis, E(source.Graph.Sites)), ActualPairs(analysis, message, output, E(source.Graph.Sites)));
    }

    [Fact]
    public void A_physical_three_arm_joint_reconstructs_as_one_component_without_inventing_a_loop()
    {
        var source = Source([(-2, 0), (0, 0), (2, 0)], [(0, 0), (0, -2)]);
        var analysis = JointShapeDescription.Analyze(source); var message = Import(analysis.Message.Surface);
        var output = JointShapeReconstruction.ThreeAttempts(message)[0];
        Assert.Equal(1, E(message.Components).Length);
        Assert.Equal(1, message.JointReads.Count(joint => E(joint.Rays).Length == 3));
        Assert.Equal(3, E(output.Source.Graph.Strokes).Length);
        Assert.Equal(0, E(output.AssumedStrokes).Length);
        Assert.Equal(ExpectedPairs(analysis, E(source.Graph.Sites)), ActualPairs(analysis, message, output, E(source.Graph.Sites)));
    }

    [Fact]
    public void One_interior_context_cannot_borrow_compatible_turns_from_two_different_output_faces()
    {
        var description = JointShapeDescription.Analyze(TwoSquares()).Message;
        var turns = E(description.InteriorTurns).Cast<Relation>().ToArray();
        var contexts = turns.Select(turn => ((Relation)turn.Left).Right).Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray();
        Assert.Equal(2, contexts.Length);
        var shared = new Placeholder(); var replacements = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        foreach (var context in contexts) replacements.Add(context, shared);
        var message = Import(Replace(description.Surface, replacements));
        var output = JointShapeReconstruction.ThreeAttempts(message)[0];
        var refinements = output.Assessment.Claims.Where(claim => claim.Kind == "refinement").ToArray();
        Assert.True(refinements.Length != 0);
        Assert.True(refinements.Any(claim => claim.State == "Violated"));
        Assert.False(refinements.Any(claim => claim.State == "Supported"), "A coherent area context needs one output boundary containing its required turns.");
        Assert.True(output.Assessment.Claims.Where(claim => claim.Kind == "components").All(claim => claim.State == "Supported"));
    }

    [Fact]
    public void Withheld_global_hints_allow_different_provisional_pairings_while_uncertain_diagonal_encounters_remain_open()
    {
        var source = Source([(0, 0), (8, 1)], [(1, 0), (5, 2)], [(2, 0), (6, 3)]);
        var analysis = JointShapeDescription.Analyze(source); var original = analysis.Message;
        var replacements = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance)
            { [original.Components] = StructuralConstraint.Open(), [original.Symmetries] = StructuralConstraint.Open(), [original.InteriorTurns] = S() };
        var message = Import(Replace(original.Surface, replacements));
        var attempts = JointShapeReconstruction.ThreeAttempts(message);
        var signatures = attempts.Select(attempt => string.Join(',', RolePairs(message, attempt))).ToArray();
        Assert.True(signatures.Distinct().Count() > 1, "Coarse oblique endpoint clues can admit more than one pairing policy.");
        Assert.True(attempts.All(attempt => E(attempt.Pairings).Length == 3));
        Assert.True(attempts.All(attempt => attempt.Assessment.Claims.Where(claim => claim.Kind == "jointTypes").All(claim => claim.State != "Violated")));
        Assert.True(attempts.Any(attempt => attempt.Assessment.Claims.Any(claim => claim.State == "Open")));
    }

    private static NativeLatticeSources.SourceCase TwoSquares() => Source([(0, 0), (2, 0), (2, 2), (0, 2), (0, 0)],
        [(6, 6), (8, 6), (8, 8), (6, 8), (6, 6)]);

    [Fact]
    public void Complete_joint_opposition_evidence_rejects_flattened_star_corners_and_guides_gap_repair()
    {
        var source = Source([(0, -4), (1, -1), (4, 0), (1, 1), (0, 4), (-1, 1), (-4, 0), (-1, -1), (0, -4)]);
        var message = Import(JointShapeDescription.Analyze(source).Message.Surface);
        var attempts = JointShapeReconstruction.ThreeAttempts(message);
        Assert.True(attempts[0].Assessment.Claims.Any(claim => claim.Kind == "jointTypes" && claim.State == "Violated"));
        Assert.False(attempts[2].Assessment.Claims.Any(claim => claim.State == "Violated"));
        Assert.True(ReferenceEquals(attempts[2], JointShapeReconstruction.Best(attempts)));
        Assert.True(Reachable(attempts[2].Surface).Any(item => ReferenceEquals(item, attempts[1].Assessment.Surface)));
    }

    [Fact]
    public void An_open_opposed_pair_inventory_cannot_exclude_unadvertised_oppositions()
    {
        var source = Source([(0, -4), (1, -1), (4, 0), (1, 1), (0, 4), (-1, 1), (-4, 0), (-1, -1), (0, -4)]);
        var description = JointShapeDescription.Analyze(source).Message;
        var replacements = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        foreach (var joint in description.JointReads) replacements.Add(joint.Opposed, StructuralConstraint.Open(E(joint.Opposed)));
        var message = Import(Replace(description.Surface, replacements));
        var output = JointShapeReconstruction.ThreeAttempts(message)[0];
        Assert.False(output.Assessment.Claims.Any(claim => claim.Kind == "jointTypes" && claim.State == "Violated"));
        Assert.True(message.JointReads.All(joint => !Closed(joint.Opposed)));
    }

    private static NativeLatticeSources.SourceCase Source(params (int X, int Y)[][] paths)
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
                edges.Add(QualitativeStrokeGraph.Stroke(new Placeholder(), Site(a), Site(b), author.Word(b.X - a.X, b.Y - a.Y)));
            }
        var graph = new QualitativeStrokeGraph(author.Frame, S(sites.Values.ToArray()), S(edges.ToArray()));
        return new("anonymous", "No recognition label", "Finite native authoring repetitions only.", graph, S(bindings.ToArray()), graph.Strokes, S(), true);
    }

    private static string[] ExpectedPairs(JointShapeDescription.Analysis analysis, IElement[] originalSites)
    {
        var indices = originalSites.Select((site, index) => (site, index)).ToDictionary(pair => pair.site, pair => pair.index, ReferenceEqualityComparer.Instance);
        return E(analysis.Source.Graph.Strokes).Cast<Relation>().Select(edge =>
        {
            QualitativeStrokeGraph.TryReadStroke(edge, out _, out var a, out var b, out _); return Pair(indices[a], indices[b]);
        }).Order(StringComparer.Ordinal).ToArray();
    }
    private static string[] ActualPairs(JointShapeDescription.Analysis analysis, JointShapeDescription.Message message,
        JointShapeReconstruction.Candidate output, IElement[] originalSites, bool observedOnly = false)
    {
        var oldRoles = E(analysis.Message.Roles); var newRoles = E(message.Roles);
        var sourceMap = E(analysis.SourceMap).Cast<Relation>().ToDictionary(row => row.Right, row => row.Left, ReferenceEqualityComparer.Instance);
        var originalIndices = originalSites.Select((site, index) => (site, index)).ToDictionary(pair => pair.site, pair => pair.index, ReferenceEqualityComparer.Instance);
        var outputToOriginal = new Dictionary<IElement, int>(ReferenceEqualityComparer.Instance);
        for (var index = 0; index < newRoles.Length; index++) outputToOriginal.Add(output.Sites[newRoles[index]], originalIndices[sourceMap[oldRoles[index]]]);
        var edges = observedOnly ? E(output.Pairings).Cast<Relation>().Select(pair => (Relation)pair.Right) : E(output.Source.Graph.Strokes).Cast<Relation>();
        return edges.Select(edge => { QualitativeStrokeGraph.TryReadStroke(edge, out _, out var a, out var b, out _); return Pair(outputToOriginal[a], outputToOriginal[b]); }).Order(StringComparer.Ordinal).ToArray();
    }
    private static string[] RolePairs(JointShapeDescription.Message message, JointShapeReconstruction.Candidate output)
    {
        var roles = E(message.Roles); var sites = roles.Select((role, index) => (Site: output.Sites[role], index))
            .ToDictionary(pair => pair.Site, pair => pair.index, ReferenceEqualityComparer.Instance);
        return E(output.Source.Graph.Strokes).Cast<Relation>().Select(edge =>
        { QualitativeStrokeGraph.TryReadStroke(edge, out _, out var a, out var b, out _); return Pair(sites[a], sites[b]); }).Order(StringComparer.Ordinal).ToArray();
    }
    private static string Pair(int a, int b) => Math.Min(a, b) + ":" + Math.Max(a, b);
    private static JointShapeDescription.Message Import(Relation port)
    {
        Assert.True(JointShapeDescription.TryRead(port, out var message, out _), "The native anonymous message must remain independently readable.");
        return message;
    }
    private static Relation RoundTrip(Relation source)
    {
        var packet = GojoStructureCompactJson.Deserialize(GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("joint-reconstruction-test", source)));
        Assert.True(GojoStructureCompactJson.TryToElement(packet, out var restored));
        return (Relation)restored.Root;
    }
    private static Relation Replace(Relation source, IReadOnlyDictionary<IElement, IElement> replacements)
    {
        var memo = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        IElement Copy(IElement item)
        {
            if (replacements.TryGetValue(item, out var replacement)) return replacement;
            if (memo.TryGetValue(item, out var prior)) return prior;
            if (item is not Relation relation) return item;
            var left = Copy(relation.Left); var right = Copy(relation.Right);
            var result = ReferenceEquals(left, relation.Left) && ReferenceEquals(right, relation.Right) ? relation : new Relation(left, right);
            memo.Add(item, result); return result;
        }
        return (Relation)Copy(source);
    }
    private static IEnumerable<IElement> Reachable(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(source);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; yield return item;
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}
