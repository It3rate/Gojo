using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class JointEquivalenceEvidenceTests
{
    [Fact]
    public void A_square_publishes_one_shared_extent_group_without_revealing_opposite_endpoints()
    {
        var analysis = JointShapeDescription.Analyze(Polygon([(0, 0), (4, 0), (4, 4), (0, 4)]));
        var stream = JointEquivalenceEvidence.Analyze(analysis);
        Assert.Single(E(stream.ExactLengths));
        var members = JointEquivalenceEvidence.Members((Relation)E(stream.ExactLengths)[0]);
        Assert.Equal(4, members.Length);
        Assert.Equal(8, analysis.Message.JointReads.Sum(joint => E(joint.Rays).Length));
        Assert.Single(E(stream.ExactAngles));
        Assert.Equal(4, JointEquivalenceEvidence.Members((Relation)E(stream.ExactAngles)[0]).Length);
        Assert.True(JointEquivalenceEvidence.TryRead(stream.Surface, analysis.Message, out var read, out _));
        Assert.Single(E(read.ExactLengths));
    }

    [Fact]
    public void A_rectangle_retains_two_length_groups_but_one_magnitude_independent_right_angle_group()
    {
        var analysis = JointShapeDescription.Analyze(Polygon([(0, 0), (8, 0), (8, 3), (0, 3)]));
        var stream = JointEquivalenceEvidence.Analyze(analysis);
        Assert.Equal(2, E(stream.ExactLengths).Length);
        Assert.All(E(stream.ExactLengths).Cast<Relation>(), group => Assert.Equal(2, JointEquivalenceEvidence.Members(group).Length));
        Assert.Single(E(stream.ExactAngles));
        Assert.Equal(4, JointEquivalenceEvidence.Members((Relation)E(stream.ExactAngles)[0]).Length);
    }

    [Fact]
    public void Native_repeated_products_compare_a_three_four_chord_with_five_axis_reaches()
    {
        var source = Passages([((0, 0), (5, 0)), ((0, 2), (3, 6))]);
        var stream = JointEquivalenceEvidence.Analyze(JointShapeDescription.Analyze(source));
        Assert.Single(E(stream.ExactLengths));
        Assert.Equal(2, JointEquivalenceEvidence.Members((Relation)E(stream.ExactLengths)[0]).Length);
        Assert.Empty(E(stream.ExactAngles));
    }

    [Fact]
    public void A_single_physical_passage_does_not_supply_two_independent_length_observations()
    {
        var stream = JointEquivalenceEvidence.Analyze(JointShapeDescription.Analyze(Passages([((0, 0), (3, 0))])));
        Assert.Empty(E(stream.ExactLengths));
    }

    [Fact]
    public void Receiver_surface_forgets_source_strokes_reaches_words_and_proof_ancestry()
    {
        var source = Polygon([(0, 0), (4, 0), (4, 4), (0, 4)]);
        var analysis = JointShapeDescription.Analyze(source); var stream = JointEquivalenceEvidence.Analyze(analysis);
        var participants = Reachable(stream.Surface).ToHashSet(ReferenceEqualityComparer.Instance);
        var excluded = new IElement[] { source.Graph.Surface, source.Graph.Frame.Surface, source.Bindings, analysis.Audit }
            .Concat(E(source.Graph.Strokes)).Concat(E(source.Graph.Sites)).Concat(E(source.Graph.Strokes).Cast<Relation>().SelectMany(stroke => {
                QualitativeStrokeGraph.TryReadStroke(stroke, out _, out _, out _, out var word);
                return new IElement[] { word }.Concat(E(word).Cast<Relation>().Select(move => move.Right));
            }));
        foreach (var item in excluded) Assert.False(participants.Contains(item));
        Assert.False(participants.Any(item => item is Carrier or CarrierLocation));
        Assert.True(Reachable(stream.Audit).Any(item => ReferenceEquals(item, source.Graph.Surface)));
    }

    [Fact]
    public void Imported_message_can_read_the_same_supplement_without_scope_object_identity()
    {
        var analysis = JointShapeDescription.Analyze(Polygon([(0, 0), (2, 0), (2, 2), (0, 2)]));
        var stream = JointEquivalenceEvidence.Analyze(analysis);
        Assert.True(JointShapeDescription.TryRead(analysis.Message.Surface, out var imported, out _));
        Assert.False(ReferenceEquals(imported.Roles, analysis.Message.Roles));
        Assert.True(JointEquivalenceEvidence.TryRead(stream.Surface, imported, out var read, out _));
        Assert.Single(E(read.ExactLengths));
    }

    [Fact]
    public void Missing_straight_interpolation_leaves_comparisons_open()
    {
        var source = Polygon([(0, 0), (2, 0), (2, 2), (0, 2)]);
        var stream = JointEquivalenceEvidence.Analyze(JointShapeDescription.Analyze(source with { StraightPassages = S() }));
        Assert.Empty(E(stream.ExactLengths)); Assert.Empty(E(stream.ExactAngles));
        Assert.Equal(4, E(stream.UnresolvedLengths).Length);
        Assert.Equal(4, E(stream.UnresolvedAngles).Length);
    }

    [Fact]
    public void Without_equal_turn_basis_different_axes_do_not_acquire_equal_length_by_name()
    {
        var source = Polygon([(0, 0), (2, 0), (2, 2), (0, 2)]) with { TurnModel = false };
        var stream = JointEquivalenceEvidence.Analyze(JointShapeDescription.Analyze(source));
        Assert.Equal(2, E(stream.ExactLengths).Length);
        Assert.All(E(stream.ExactLengths).Cast<Relation>(), group => Assert.Equal(2, JointEquivalenceEvidence.Members(group).Length));
    }

    [Fact]
    public void The_critic_rejects_unequal_candidate_lengths_from_a_shared_extent_promise()
    {
        var analysis = JointShapeDescription.Analyze(Polygon([(0, 0), (2, 0), (2, 2), (0, 2)]));
        var stream = JointEquivalenceEvidence.Analyze(analysis);
        var balanced = JointShapeReconstruction.Propose(analysis.Message, 0);
        var stretched = JointShapeReconstruction.Propose(analysis.Message, 1);
        var good = JointEquivalenceEvidence.Assess(stream, balanced);
        var bad = JointEquivalenceEvidence.Assess(stream, stretched);
        Assert.True(good.Claims.Any(claim => claim.Kind == "lengthEquivalence" && claim.State == "Supported"));
        Assert.False(good.Claims.Any(claim => claim.State == "Contradicted"));
        Assert.True(bad.Claims.Any(claim => claim.Kind == "lengthEquivalence" && claim.State == "Contradicted"));
        Assert.True(bad.Claims.Any(claim => claim.Kind == "angleEquivalence" && claim.State == "Supported"));
        Assert.True(Reachable(bad.Surface).Any(item => ReferenceEquals(item, stretched.Surface)));
    }

    [Fact]
    public void Coarse_diagonal_heading_similarity_does_not_manufacture_exact_angle_equivalence()
    {
        var source = Passages([((0, 0), (4, 0)), ((0, 0), (1, 2)), ((8, 0), (12, 0)), ((8, 0), (10, 1))], joinStarts: true);
        var stream = JointEquivalenceEvidence.Analyze(JointShapeDescription.Analyze(source));
        Assert.Empty(E(stream.ExactAngles));
        Assert.Single(E(stream.SimilarAngles));
        Assert.Equal(2, JointEquivalenceEvidence.Members((Relation)E(stream.SimilarAngles)[0]).Length);
    }

    [Fact]
    public void Reader_rejects_an_unknown_ray_slot_and_a_pair_from_different_joints()
    {
        var analysis = JointShapeDescription.Analyze(Polygon([(0, 0), (2, 0), (2, 2), (0, 2)]));
        var stream = JointEquivalenceEvidence.Analyze(analysis); var group = (Relation)E(stream.ExactLengths)[0];
        var row = (Relation)E((Relation)group.Right)[0]; var invented = new Relation(new Placeholder(), row.Right);
        Assert.False(JointEquivalenceEvidence.TryRead(Replace(stream.Surface, row, invented), analysis.Message, out _, out _));
        var turnGroup = (Relation)E(stream.ExactAngles)[0]; var turnRow = (Relation)E((Relation)turnGroup.Right)[0];
        var slots = analysis.Message.JointReads.Select(joint => E(joint.Rays).Cast<Relation>().First().Left).ToArray();
        var badPair = new Relation(slots[0], slots[1]);
        Assert.False(JointEquivalenceEvidence.TryRead(Replace(stream.Surface, turnRow, new Relation(badPair, turnRow.Right)), analysis.Message, out _, out _));
    }

    private static NativeLatticeSources.SourceCase Polygon((int X, int Y)[] points)
    {
        var author = new LatticeLadderAuthor(); var sites = points.Select(_ => (IElement)new Placeholder()).ToArray();
        var strokes = points.Select((point, index) => {
            var next = points[(index + 1) % points.Length];
            return (IElement)QualitativeStrokeGraph.Stroke(new Placeholder(), sites[index], sites[(index + 1) % points.Length], author.Word(next.X - point.X, next.Y - point.Y));
        }).ToArray();
        var graph = new QualitativeStrokeGraph(author.Frame, S(sites), S(strokes));
        return new("anonymous", "anonymous", "", graph, S(points.Select((point, index) => (IElement)new Relation(sites[index], author.Word(point.X, point.Y))).ToArray()), graph.Strokes, S(), true);
    }
    private static NativeLatticeSources.SourceCase Passages(((int X, int Y) A, (int X, int Y) B)[] passages, bool joinStarts = false)
    {
        var author = new LatticeLadderAuthor(); var sites = new List<IElement>(); var words = new List<Relation>(); var edges = new List<IElement>();
        var starts = new Dictionary<(int, int), IElement>();
        foreach (var (a, b) in passages)
        {
            var from = joinStarts && starts.TryGetValue(a, out var old) ? old : new Placeholder(); var to = new Placeholder();
            if (!sites.Contains(from, ReferenceEqualityComparer.Instance)) { sites.Add(from); words.Add(new(from, author.Word(a.X, a.Y))); starts[a] = from; }
            sites.Add(to); words.Add(new(to, author.Word(b.X, b.Y)));
            edges.Add(QualitativeStrokeGraph.Stroke(new Placeholder(), from, to, author.Word(b.X - a.X, b.Y - a.Y)));
        }
        var graph = new QualitativeStrokeGraph(author.Frame, S(sites.ToArray()), S(edges.ToArray()));
        return new("anonymous", "anonymous", "", graph, S(words.Cast<IElement>().ToArray()), graph.Strokes, S(), true);
    }
    private static Relation Replace(Relation root, IElement target, IElement replacement)
    {
        var memo = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        IElement Copy(IElement element)
        {
            if (ReferenceEquals(element, target)) return replacement;
            if (memo.TryGetValue(element, out var actual)) return actual;
            actual = element is Relation relation ? new Relation(Copy(relation.Left), Copy(relation.Right)) : element;
            memo.Add(element, actual); return actual;
        }
        return (Relation)Copy(root);
    }
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item)) { if (!seen.Add(item)) continue; yield return item; if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); } }
    }
    private static IElement[] E(Relation scope) => StructuralPotential.Items(scope);
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
}
