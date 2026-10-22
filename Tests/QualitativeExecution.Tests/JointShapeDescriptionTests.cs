using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class JointShapeDescriptionTests
{
    private static readonly Lazy<IReadOnlyList<JointReconstructionSources.Case>> Sources = new(JointReconstructionSources.Cases);

    [Fact]
    public void Anonymous_message_forgets_source_sites_strokes_reaches_and_private_construction()
    {
        var cause = new Relation(new Placeholder(), new Placeholder());
        var source = Polygon([(0, 0), (4, 0), (4, 2), (0, 2)], cause);
        var privateTeacher = new Relation(source.Graph.Surface, cause);
        source = source with { Provenance = privateTeacher };
        var description = JointShapeDescription.Analyze(source);
        var participants = Reachable(description.Message.Surface).ToHashSet(ReferenceEqualityComparer.Instance);
        var excluded = new IElement[] { source.Graph.Surface, source.Bindings, source.Provenance, cause, source.Graph.Frame.Surface }
            .Concat(E(source.Graph.Sites)).Concat(E(source.Graph.Strokes))
            .Concat(E(source.Graph.Strokes).Cast<Relation>().SelectMany(stroke =>
            {
                QualitativeStrokeGraph.TryReadStroke(stroke, out _, out _, out _, out var word);
                return E(word).Cast<Relation>().Select(move => move.Right);
            }));
        foreach (var item in excluded) Assert.False(participants.Contains(item));
        Assert.False(participants.Any(item => item is Carrier or CarrierLocation));
        Assert.True(Reachable(description.Audit).Any(item => ReferenceEquals(item, source.Graph.Surface)));
        Assert.Equal(4, E(description.Message.Roles).Length);
    }

    [Fact]
    public void Port_only_roundtrip_preserves_joint_and_order_structure_without_source_access()
    {
        var message = JointShapeDescription.Analyze(Case("J05")).Message;
        Assert.True(JointShapeDescription.TryRead(message.Surface, out var imported, out _));
        Assert.Equal(message.JointReads.Count, imported.JointReads.Count);
        Assert.Equal(message.JointReads.Select(joint => joint.Kind), imported.JointReads.Select(joint => joint.Kind));
        Assert.Equal(BucketSizes(message.Horizontal), BucketSizes(imported.Horizontal));
        Assert.Equal(BucketSizes(message.Vertical), BucketSizes(imported.Vertical));
        Assert.Equal(E(message.Components).Length, E(imported.Components).Length);
        Assert.Equal(E(message.Symmetries).Length, E(imported.Symmetries).Length);
        Assert.All(imported.JointReads, joint => Assert.True(E(imported.Roles).Any(role => ReferenceEquals(role, joint.Role))));
        Assert.False(ReferenceEquals(message.Frame.Surface, imported.Frame.Surface));
    }

    [Fact]
    public void Relative_order_uses_ordinal_buckets_and_does_not_communicate_reach_multiplicity()
    {
        var compact = JointShapeDescription.Analyze(Polygon([(0, 0), (2, 0), (2, 1), (0, 1)])).Message;
        var stretched = JointShapeDescription.Analyze(Polygon([(0, 0), (9, 0), (9, 3), (0, 3)])).Message;
        Assert.Equal(new[] { 2, 2 }, BucketSizes(compact.Horizontal));
        Assert.Equal(new[] { 2, 2 }, BucketSizes(compact.Vertical));
        Assert.Equal(BucketSizes(compact.Horizontal), BucketSizes(stretched.Horizontal));
        Assert.Equal(BucketSizes(compact.Vertical), BucketSizes(stretched.Vertical));
        Assert.All(compact.JointReads, joint => Assert.Equal(2, E(joint.Rays).Length));
        Assert.Equal(compact.JointReads.Select(joint => E(joint.Perpendicular).Length),
            stretched.JointReads.Select(joint => E(joint.Perpendicular).Length));
        Assert.All(stretched.JointReads, joint => Assert.Single(E(joint.Perpendicular)));
    }

    [Fact]
    public void Reflex_and_convex_boundary_turns_can_both_have_perpendicular_local_rays()
    {
        var source = Polygon([(0, 0), (3, 0), (3, 1), (1, 1), (1, 3), (0, 3)]);
        var analysis = JointShapeDescription.Analyze(source);
        var sourceSites = E(source.Graph.Sites);
        var convex = Role(analysis, sourceSites[1]);
        var reflex = Role(analysis, sourceSites[3]);
        Assert.Single(E(analysis.Message.JointReads.Single(joint => ReferenceEquals(joint.Role, convex)).Perpendicular));
        Assert.Single(E(analysis.Message.JointReads.Single(joint => ReferenceEquals(joint.Role, reflex)).Perpendicular));
        var convexTurn = Turn(analysis.Message, convex);
        var reflexTurn = Turn(analysis.Message, reflex);
        Assert.True(E((Relation)convexTurn.Left).Any(direction => ReferenceEquals(direction, analysis.Message.Frame.Right)));
        Assert.True(E((Relation)convexTurn.Right).Any(direction => ReferenceEquals(direction, analysis.Message.Frame.Up)));
        Assert.True(E((Relation)reflexTurn.Left).Any(direction => ReferenceEquals(direction, analysis.Message.Frame.Left)));
        Assert.True(E((Relation)reflexTurn.Right).Any(direction => ReferenceEquals(direction, analysis.Message.Frame.Up)));
    }

    [Fact]
    public void Projected_crossing_does_not_become_a_shared_four_arm_joint()
    {
        var crossing = JunctionInterpretationStudy.Build().Cases.Single(item => item.Id == "J01");
        var source = new NativeLatticeSources.SourceCase("anonymous", "anonymous", "", crossing.Graph,
            crossing.Bindings, crossing.Graph.Strokes, S(), true);
        var separate = JointShapeDescription.Analyze(source).Message;
        var connected = JointShapeDescription.Analyze(Case("J14")).Message;
        Assert.Equal(2, E(separate.Components).Length);
        Assert.All(separate.JointReads, joint => Assert.Single(E(joint.Rays)));
        Assert.Single(E(connected.Components));
        Assert.Single(connected.JointReads.Where(joint => E(joint.Rays).Length == 4));
        Assert.Equal(4, separate.JointReads.Count);
        Assert.Equal(5, connected.JointReads.Count);
    }

    [Fact]
    public void Closure_default_preserves_the_observed_missing_passage_and_endpoint_obligations()
    {
        var source = Case("J08");
        var sites = E(source.Graph.Sites); var strokes = E(source.Graph.Strokes);
        var message = JointShapeDescription.Analyze(source).Message;
        Assert.Equal(2, message.JointReads.Count(joint => E(joint.Rays).Length == 1));
        Assert.Equal(5, strokes.Length);
        Assert.True(E(message.Obligations).Length >= 2);
        Assert.True(Reachable(message.ClosureDefault).Any());
        Assert.True(sites.SequenceEqual(E(source.Graph.Sites), ReferenceEqualityComparer.Instance));
        Assert.True(strokes.SequenceEqual(E(source.Graph.Strokes), ReferenceEqualityComparer.Instance));
    }

    [Fact]
    public void Site_enumeration_is_not_a_geometric_order_or_an_edge_pairing_hint()
    {
        var source = Polygon([(0, 0), (4, 0), (4, 2), (0, 2)]);
        var reordered = source with { Graph = new QualitativeStrokeGraph(source.Graph.Frame,
            S(E(source.Graph.Sites).Reverse().ToArray()), source.Graph.Strokes) };
        var original = JointShapeDescription.Analyze(source).Message;
        var alternate = JointShapeDescription.Analyze(reordered).Message;
        Assert.Equal(BucketSizes(original.Horizontal), BucketSizes(alternate.Horizontal));
        Assert.Equal(BucketSizes(original.Vertical), BucketSizes(alternate.Vertical));
        Assert.Equal(original.JointReads.Select(joint => joint.Kind).Order(), alternate.JointReads.Select(joint => joint.Kind).Order());
        Assert.Equal(E(original.Components).Select(component => E((Relation)component).Length).Order(),
            E(alternate.Components).Select(component => E((Relation)component).Length).Order());
    }

    [Fact]
    public void Unrelated_reaches_leave_relative_order_open_instead_of_using_enumeration_order()
    {
        var source = Polygon([(0, 0), (2, 0), (2, 1), (0, 1)]);
        var site = E(source.Graph.Sites)[1]; var unknownReach = new Placeholder();
        var changed = S(E(source.Bindings).Cast<Relation>().Select(row => (IElement)(ReferenceEquals(row.Left, site)
            ? new Relation(site, S(QualitativeMotionFrame.Move(source.Graph.Frame.Right, unknownReach))) : row)).ToArray());
        var message = JointShapeDescription.Analyze(source with { Bindings = changed }).Message;
        Assert.False(Closed((Relation)message.Horizontal.Right));
        Assert.True(BucketSizes(message.Horizontal).Sum() < E(message.Roles).Length);
        Assert.NotEmpty(E(message.Obligations));
        Assert.True(JointShapeDescription.TryRead(message.Surface, out _, out _));
    }

    [Fact]
    public void Port_reader_rejects_duplicate_joint_roles_and_unbound_ray_pair_claims()
    {
        var message = JointShapeDescription.Analyze(Case("J02")).Message;
        var joints = E(message.Joints);
        var duplicate = S(joints.Select((row, index) => index == 1 ? joints[0] : row).ToArray());
        Assert.False(JointShapeDescription.TryRead(Replace(message.Surface, message.Joints, duplicate), out _, out _));
        var first = message.JointReads[0];
        var invented = S(new Relation(new Placeholder(), new Placeholder()));
        Assert.False(JointShapeDescription.TryRead(Replace(message.Surface, first.Opposed, invented), out _, out _));
    }

    [Fact]
    public void Perpendicular_evidence_requires_the_selected_turn_model()
    {
        var source = Polygon([(0, 0), (4, 0), (4, 1), (0, 1)]);
        var supported = JointShapeDescription.Analyze(source).Message;
        var open = JointShapeDescription.Analyze(source with { TurnModel = false }).Message;
        Assert.All(supported.JointReads, joint => Assert.Single(E(joint.Perpendicular)));
        Assert.All(open.JointReads, joint => Assert.Empty(E(joint.Perpendicular)));
        Assert.Equal("Open", open.Basis.ReadRightAngle(S()).Answer);
        Assert.True(JointShapeDescription.TryRead(open.Surface, out _, out _));
    }

    [Fact]
    public void A_quarter_turn_claim_constrains_generated_chords_as_well_as_cycle_incidence()
    {
        var message = JointShapeDescription.Analyze(Polygon([(0, 0), (2, 0), (2, 2), (0, 2)])).Message;
        Assert.True(JointShapeDescription.TryRead(message.Surface, out var imported, out _));
        var attempts = JointShapeReconstruction.ThreeAttempts(imported);
        Assert.Equal(3, attempts.Count);
        Assert.Contains(attempts[0].Assessment.Claims, claim => claim.Kind == "symmetry" && claim.State == "Supported");
        Assert.Contains(attempts[1].Assessment.Claims, claim => claim.Kind == "symmetry" && claim.State == "Violated");
        Assert.False(ReferenceEquals(attempts[0].Source.Graph.Surface, attempts[1].Source.Graph.Surface));
        Assert.True(Reachable(attempts[1].Surface).Any(item => ReferenceEquals(item, attempts[0].Assessment.Surface)));
    }

    [Fact]
    public void Open_source_stroke_inventory_does_not_turn_observed_ray_counts_into_exact_degrees()
    {
        var source = Polygon([(0, 0), (2, 0), (2, 2), (0, 2)]);
        var graph = new QualitativeStrokeGraph(source.Graph.Frame, source.Graph.Sites, StructuralConstraint.Open(E(source.Graph.Strokes)));
        var message = JointShapeDescription.Analyze(source with { Graph = graph }).Message;
        Assert.All(message.JointReads, joint =>
        {
            Assert.Equal(2, E(joint.Rays).Length);
            Assert.False(Closed(joint.Rays));
        });
        Assert.NotEmpty(E(message.Obligations));
        Assert.True(JointShapeDescription.TryRead(message.Surface, out var imported, out _));
        Assert.All(imported.JointReads, joint => Assert.False(Closed(joint.Rays)));
    }

    private static NativeLatticeSources.SourceCase Case(string id) => Sources.Value.Single(item => item.Id == id).Source;

    [Fact]
    public void A_net_displacement_without_a_straight_passage_contract_does_not_supply_tangent_oppositions()
    {
        var source = Polygon([(0, 0), (2, 0), (2, 2), (0, 2)]);
        var message = JointShapeDescription.Analyze(source with { StraightPassages = S() }).Message;
        Assert.All(message.JointReads, joint => {
            Assert.False(Closed(joint.Opposed));
            Assert.Empty(E(joint.Opposed));
            Assert.All(E(joint.Rays).Cast<Relation>(), ray => Assert.False(Closed((Relation)ray.Right)));
        });
        Assert.True(JointShapeDescription.TryRead(message.Surface, out _, out _));
    }

    [Fact]
    public void Partial_symmetry_retains_known_defect_roles_without_their_source_words()
    {
        var analysis = JointShapeDescription.Analyze(Case("J09"));
        var roles = E(analysis.Message.Roles);
        var exceptions = E(analysis.Message.Symmetries).Cast<Relation>().SelectMany(clause => {
            var remainder = (Relation)((Relation)clause.Right).Right;
            return E((Relation)remainder.Left).Concat(E((Relation)((Relation)remainder.Right).Left));
        }).OfType<Relation>().ToArray();
        Assert.True(exceptions.Any(row => roles.Contains(row.Left, ReferenceEqualityComparer.Instance) && row.Right is Placeholder));
        Assert.True(JointShapeDescription.TryRead(analysis.Message.Surface, out _, out _));
    }
    private static IElement Role(JointShapeDescription.Analysis analysis, IElement actual) => E(analysis.SourceMap).Cast<Relation>()
        .Single(row => ReferenceEquals(row.Left, actual)).Right;
    private static Relation Turn(JointShapeDescription.Message message, IElement role) =>
        (Relation)E(message.InteriorTurns).Cast<Relation>().Single(row => row.Left is Relation context && ReferenceEquals(context.Left, role)).Right;
    private static int[] BucketSizes(Relation order) => E((Relation)order.Right).Cast<Relation>().Select(bucket => E(bucket).Length).ToArray();

    private static NativeLatticeSources.SourceCase Polygon((int X, int Y)[] points, IElement? cause = null)
    {
        // Integers author finite repetitions of uncalibrated native reaches.
        // No scalar positions are offered to the description reader.
        var author = new LatticeLadderAuthor();
        var sites = points.Select(_ => (IElement)new Placeholder()).ToArray();
        var strokes = points.Select((point, index) =>
        {
            var next = points[(index + 1) % points.Length];
            return (IElement)QualitativeStrokeGraph.Stroke(cause ?? new Placeholder(), sites[index], sites[(index + 1) % sites.Length],
                author.Word(next.X - point.X, next.Y - point.Y));
        }).ToArray();
        var graph = new QualitativeStrokeGraph(author.Frame, S(sites), S(strokes));
        return new("anonymous", "anonymous", "", graph,
            S(points.Select((point, index) => (IElement)new Relation(sites[index], author.Word(point.X, point.Y))).ToArray()), graph.Strokes, S(), true);
    }

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
    private static Relation Replace(Relation root, IElement target, IElement replacement)
    {
        var copies = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        IElement Copy(IElement item)
        {
            if (ReferenceEquals(item, target)) return replacement;
            if (copies.TryGetValue(item, out var known)) return known;
            var result = item is Relation relation ? new Relation(Copy(relation.Left), Copy(relation.Right)) : item;
            copies.Add(item, result); return result;
        }
        return (Relation)Copy(root);
    }
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var rows, out _, out _) ? rows : [];
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var a, out var b) && a.Equals(b);
}
