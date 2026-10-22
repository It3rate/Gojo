using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class RelationalSpatialEvidenceTests
{
    [Fact]
    public void Projection_facts_serialize_groups_and_before_relations_without_path_order()
    {
        var message = Rectangle(); var read = RelationalSpatialEvidence.Analyze(message);
        Assert.True(RelationalSpatialEvidence.TryRead(read.Surface, message, out var restored, out _));
        var horizontal = (Relation)restored.Horizontal.Right;
        Assert.Equal(2, E((Relation)horizontal.Left).Length); Assert.Equal(1, E((Relation)horizontal.Right).Length);
        Assert.True(E((Relation)horizontal.Left).Cast<Relation>().All(group => E(group).Length == 2));
        Assert.False(Reachable(read.Surface).Any(participant => participant is Carrier or CarrierLocation));
    }

    [Fact]
    public void Full_positive_order_is_invariant_to_unordered_role_serialization()
    {
        var full = OrderedIsolated(6); var facts = RelationalSpatialEvidence.Analyze(full);
        var forward = Open(full); var reverse = Open(full, reverse: true);
        Assert.True(RelationalSpatialEvidence.TryRead(facts.Surface, forward, out var a, out _));
        Assert.True(RelationalSpatialEvidence.TryRead(facts.Surface, reverse, out var b, out _));
        var left = RelationalSpatialEvidence.Infer(forward, a, S(), 0);
        var right = RelationalSpatialEvidence.Infer(reverse, b, S(), 2);
        Assert.True(left.Complete); Assert.True(right.Complete);
        Assert.True(Buckets(left.Layout!.Horizontal).SelectMany(E).SequenceEqual(Buckets(right.Layout!.Horizontal).SelectMany(E), ReferenceEqualityComparer.Instance));
        Assert.Empty(E(left.Assumptions)); Assert.Empty(E(right.Assumptions));
    }

    [Fact]
    public void Ray_pair_hypotheses_supply_relative_projection_constraints_without_teacher_orders()
    {
        var full = Rectangle(); var receiver = Open(full);
        var empty = RelationalSpatialEvidence.Select(RelationalSpatialEvidence.Analyze(receiver), receiver, "none");
        var guessed = RectanglePairs(receiver);
        foreach (var variant in new[] { 0, 1, 2 })
        {
            var result = RelationalSpatialEvidence.Infer(receiver, empty, guessed, variant);
            Assert.True(result.Complete); Assert.Empty(E(result.Contradictions)); Assert.NotEmpty(E(result.Assumptions));
            Assert.Equal(new[] { 2, 2 }, Buckets(result.Layout!.Horizontal).Select(bucket => E(bucket).Length).ToArray());
            Assert.Equal(new[] { 2, 2 }, Buckets(result.Layout.Vertical).Select(bucket => E(bucket).Length).ToArray());
            var sourceRoles = E(receiver.Roles);
            Assert.True(E(Buckets(result.Layout.Horizontal)[0]).ToHashSet(ReferenceEqualityComparer.Instance).SetEquals(new[] { sourceRoles[0], sourceRoles[3] }));
        }
    }

    [Fact]
    public void Sparse_and_absent_publications_leave_guessed_extensions_explicit()
    {
        var full = OrderedIsolated(7); var facts = RelationalSpatialEvidence.Analyze(full);
        var sparse = RelationalSpatialEvidence.Select(facts, full, "sparse");
        var none = RelationalSpatialEvidence.Select(facts, full, "none");
        Assert.Equal(6, E((Relation)((Relation)facts.Horizontal.Right).Right).Length);
        Assert.Equal(2, E((Relation)((Relation)sparse.Horizontal.Right).Right).Length);
        Assert.Empty(E((Relation)((Relation)none.Horizontal.Right).Left));
        Assert.Empty(E((Relation)((Relation)none.Horizontal.Right).Right));
        var receiver = Open(full);
        Assert.True(RelationalSpatialEvidence.TryRead(sparse.Surface, receiver, out var imported, out _));
        var inferred = RelationalSpatialEvidence.Infer(receiver, imported, S(), 1);
        Assert.True(inferred.Complete); Assert.NotEmpty(E(inferred.Assumptions));
        Assert.Equal(7, Buckets(inferred.Layout!.Horizontal).Count);
    }

    [Fact]
    public void Positive_order_cycles_block_a_layout_instead_of_reversing_evidence()
    {
        var full = OrderedIsolated(3); var facts = RelationalSpatialEvidence.Analyze(full); var roles = E(full.Roles);
        var body = (Relation)facts.Horizontal.Right;
        var cycle = new Relation(full.Frame.Horizontal, new Relation(body.Left,
            S([.. E((Relation)body.Right), new Relation(roles[2], roles[0])])));
        var surface = new Relation(new Relation(full.Frame.Surface, full.Roles), new Relation(cycle, facts.Vertical));
        Assert.True(RelationalSpatialEvidence.TryRead(surface, full, out var read, out _));
        var result = RelationalSpatialEvidence.Infer(full, read, S(), 0);
        Assert.False(result.Complete); Assert.Null(result.Layout); Assert.NotEmpty(E(result.Contradictions));
    }

    [Fact]
    public void Contradictory_pair_direction_does_not_override_observed_projections()
    {
        var message = Rectangle(); var read = RelationalSpatialEvidence.Analyze(message); var roles = E(message.Roles);
        var pair = S(new Relation(Ray(message, roles[1], message.Frame.Right, fallback: message.Frame.Up),
            Ray(message, roles[0], message.Frame.Down, fallback: message.Frame.Right)));
        var result = RelationalSpatialEvidence.Infer(message, read, pair, 0);
        Assert.False(result.Complete); Assert.NotEmpty(E(result.Contradictions));
    }

    [Fact]
    public void Hypothetical_projection_equality_conflicting_with_a_known_before_fact_is_rejected()
    {
        var message = Rectangle(); var read = RelationalSpatialEvidence.Analyze(message); var roles = E(message.Roles);
        // These opposed horizontal rays could be paired without source edges,
        // but that would wrongly identify two known unequal vertical levels.
        var pair = S(new Relation(Ray(message, roles[0], message.Frame.Right), Ray(message, roles[2], message.Frame.Left)));
        var result = RelationalSpatialEvidence.Infer(message, read, pair, 1);
        Assert.False(result.Complete); Assert.Null(result.Layout); Assert.NotEmpty(E(result.Contradictions));
        Assert.NotEmpty(E(result.Assumptions));
    }

    [Fact]
    public void Spatial_critic_tests_only_published_facts_and_keeps_unavailable_positions_open()
    {
        var message = Rectangle(); var facts = RelationalSpatialEvidence.Analyze(message);
        var candidate = JointShapeReconstruction.Propose(message, 0);
        var assessment = RelationalSpatialEvidence.Assess(facts, candidate);
        Assert.NotEmpty(E(assessment.Supported)); Assert.Empty(E(assessment.Contradicted)); Assert.Empty(E(assessment.Open));
        var receiver = Open(message);
        Assert.True(RelationalSpatialEvidence.TryRead(facts.Surface, receiver, out var imported, out _));
        var unavailable = JointShapeReconstruction.Propose(receiver, 0);
        var unknown = RelationalSpatialEvidence.Assess(imported, unavailable);
        Assert.Empty(E(unknown.Supported)); Assert.Empty(E(unknown.Contradicted)); Assert.NotEmpty(E(unknown.Open));
        var none = RelationalSpatialEvidence.Select(imported, receiver, "none");
        var nothing = RelationalSpatialEvidence.Assess(none, candidate);
        Assert.Empty(E(nothing.Supported)); Assert.Empty(E(nothing.Contradicted)); Assert.Empty(E(nothing.Open));
    }

    [Fact]
    public void Exact_reader_rejects_foreign_roles_and_preserves_standalone_frame_rebinding()
    {
        var message = Rectangle(); var facts = RelationalSpatialEvidence.Analyze(message);
        var receiver = Open(message);
        Assert.False(ReferenceEquals(receiver.Frame, message.Frame));
        Assert.True(RelationalSpatialEvidence.TryRead(facts.Surface, receiver, out var imported, out _));
        Assert.True(ReferenceEquals(imported.Frame, receiver.Frame));
        var invalid = new Relation(new Relation(message.Frame.Surface, message.Roles), new Relation(
            new Relation(message.Frame.Horizontal, new Relation(S(S(new Placeholder())), S())), facts.Vertical));
        Assert.False(RelationalSpatialEvidence.TryRead(invalid, message, out _, out _));
    }

    private static JointShapeDescription.Message Rectangle()
    {
        var frame = new QualitativeMotionFrame(); var roles = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray();
        var directions = new IElement[][] { [frame.Right, frame.Up], [frame.Left, frame.Up], [frame.Left, frame.Down], [frame.Right, frame.Down] };
        var joints = S(roles.Select((role, index) => (IElement)new Relation(role, new Relation(
            S(directions[index].Select(direction => (IElement)new Relation(new Placeholder(), S(direction))).ToArray()), new Relation(S(), S())))).ToArray());
        return Message(frame, S(roles), joints,
            new Relation(frame.Horizontal, S(S(roles[0], roles[3]), S(roles[1], roles[2]))),
            new Relation(frame.Vertical, S(S(roles[0], roles[1]), S(roles[2], roles[3]))));
    }
    private static JointShapeDescription.Message OrderedIsolated(int count)
    {
        var frame = new QualitativeMotionFrame(); var roles = Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray();
        var joints = S(roles.Select(role => (IElement)new Relation(role, new Relation(S(), new Relation(S(), S())))).ToArray());
        return Message(frame, S(roles), joints,
            new Relation(frame.Horizontal, S(roles.Select(role => (IElement)S(role)).ToArray())), new Relation(frame.Vertical, S(S(roles))));
    }
    private static JointShapeDescription.Message Open(JointShapeDescription.Message full, bool reverse = false) =>
        Message(full.Frame, reverse ? S(E(full.Roles).Reverse().ToArray()) : full.Roles,
            reverse ? S(E(full.Joints).Reverse().ToArray()) : full.Joints,
            new Relation(full.Frame.Horizontal, StructuralConstraint.Open()), new Relation(full.Frame.Vertical, StructuralConstraint.Open()));
    private static JointShapeDescription.Message Message(QualitativeMotionFrame frame, Relation roles, Relation joints, Relation h, Relation v)
    {
        var start = new Placeholder(); var end = new Placeholder();
        var closure = new Relation(new Relation(new Placeholder(), new Relation(start, end)), new Relation(end, start));
        var port = new Relation(new Relation(new Relation(frame.Surface, new NativeLineBasis(frame, true).Surface), roles),
            new Relation(new Relation(joints, new Relation(h, v)), new Relation(new Relation(S(), S(roles)), new Relation(closure, new Relation(S(), S())))));
        Assert.True(JointShapeDescription.TryRead(port, out var message, out _)); return message;
    }
    private static Relation RectanglePairs(JointShapeDescription.Message message)
    {
        var r = E(message.Roles); var f = message.Frame;
        return S(new Relation(Ray(message, r[0], f.Right), Ray(message, r[1], f.Left)),
            new Relation(Ray(message, r[1], f.Up), Ray(message, r[2], f.Down)),
            new Relation(Ray(message, r[2], f.Left), Ray(message, r[3], f.Right)),
            new Relation(Ray(message, r[3], f.Down), Ray(message, r[0], f.Up)));
    }
    private static IElement Ray(JointShapeDescription.Message message, IElement role, IElement direction, IElement? fallback = null) =>
        E(message.JointReads.Single(joint => ReferenceEquals(joint.Role, role)).Rays).Cast<Relation>()
            .First(row => E((Relation)row.Right).Any(at => ReferenceEquals(at, direction)) || fallback is not null && E((Relation)row.Right).Any(at => ReferenceEquals(at, fallback))).Left;
    private static IReadOnlyList<Relation> Buckets(Relation axis) => E((Relation)axis.Right).Cast<Relation>().ToArray();
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => StructuralPotential.Items(scope);
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var todo = new Stack<IElement>(); todo.Push(root);
        while (todo.TryPop(out var at)) if (seen.Add(at)) { yield return at; if (at is Relation relation) { todo.Push(relation.Left); todo.Push(relation.Right); } }
    }
}
