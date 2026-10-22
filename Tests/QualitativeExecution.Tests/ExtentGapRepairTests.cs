using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class ExtentGapRepairTests
{
    [Fact]
    public void Shared_extent_evidence_repairs_an_unequal_rectangle_without_a_teacher_measurement()
    {
        var analysis = JointShapeDescription.Analyze(Polygon([(0, 0), (4, 0), (4, 4), (0, 4)]));
        var message = analysis.Message; var eq = JointEquivalenceEvidence.Analyze(analysis);
        var before = Sample(message, [3], [1]);
        Assert.True(JointEquivalenceEvidence.Assess(eq, before).Claims.Any(claim => claim.Kind == "lengthEquivalence" && claim.State == "Contradicted"));
        Assert.True(ExtentGapRepair.TryRepair(eq, before, message, [3], [1], 0, out var h, out var v, out var witness));
        Assert.Equal(h[0], v[0]);
        var reach = SharedReach(before); var after = Sample(message, h, v, reach);
        Assert.False(JointEquivalenceEvidence.Assess(eq, after).Claims.Any(claim => claim.Kind == "lengthEquivalence" && claim.State == "Contradicted"));
        Assert.True(Reachable(witness).Any(item => ReferenceEquals(item, reach)));
        Assert.True(Reachable(witness).Any(item => ReferenceEquals(item, eq.ExactLengths)));
        Assert.True(h.SequenceEqual(new[] { 1 }) && v.SequenceEqual(new[] { 1 }));
        // The source was a four-reach square; the smaller valid receiver
        // sampling proves that repair did not restore its hidden dimensions.
        var excluded = new IElement[] { analysis.Source.Graph.Surface, analysis.Source.Bindings, analysis.Audit }
            .Concat(E(analysis.Source.Graph.Strokes)).Concat(E(analysis.Source.Graph.Sites));
        var participants = Reachable(witness).ToHashSet(ReferenceEqualityComparer.Instance);
        foreach (var item in excluded) Assert.False(participants.Contains(item));
        // A stale proposal cannot attribute a different gap sampling to this
        // candidate and then present its arithmetic as a native repair.
        Assert.False(ExtentGapRepair.TryRepair(eq, before, message, [4], [1], 0, out var staleH, out var staleV, out _));
        Assert.True(staleH.SequenceEqual(new[] { 4 }) && staleV.SequenceEqual(new[] { 1 }));
    }

    [Fact]
    public void Equal_passages_in_disconnected_translated_units_can_coordinate_their_gap_words()
    {
        var source = JointReconstructionSources.Cases().Single(item => item.Id == "J10").Source;
        var analysis = JointShapeDescription.Analyze(source); var eq = JointEquivalenceEvidence.Analyze(analysis);
        var h = Enumerable.Repeat(1, E((Relation)analysis.Message.Horizontal.Right).Length - 1).ToArray();
        var v = Enumerable.Repeat(1, E((Relation)analysis.Message.Vertical.Right).Length - 1).ToArray(); h[0] = 3;
        var sample = Sample(analysis.Message, h, v);
        var oldConflicts = JointEquivalenceEvidence.Assess(eq, sample).Claims.Count(claim => claim.Kind == "lengthEquivalence" && claim.State == "Contradicted");
        Assert.True(oldConflicts > 0);
        Assert.True(ExtentGapRepair.TryRepair(eq, sample, analysis.Message, h, v, 2, out var newH, out var newV, out var witness));
        var after = Sample(analysis.Message, newH, newV, SharedReach(sample));
        var newConflicts = JointEquivalenceEvidence.Assess(eq, after).Claims.Count(claim => claim.Kind == "lengthEquivalence" && claim.State == "Contradicted");
        Assert.True(newConflicts < oldConflicts);
        Assert.All(newH.Concat(newV), value => Assert.InRange(value, 1, 6));
        Assert.Equal(h[0], 3);
        Assert.True(Reachable(witness).Any(item => ReferenceEquals(item, sample.RoleMap)));
    }

    [Fact]
    public void Diagonal_groups_and_an_unavailable_metric_remain_open_without_coordinate_changes()
    {
        var diamond = JointShapeDescription.Analyze(Polygon([(0, -3), (3, 0), (0, 3), (-3, 0)]));
        var eq = JointEquivalenceEvidence.Analyze(diamond); int[] h = [2, 2], v = [1, 1];
        var sample = Sample(diamond.Message, h, v);
        Assert.False(ExtentGapRepair.TryRepair(eq, sample, diamond.Message, h, v, 0, out var afterH, out var afterV, out var witness));
        Assert.True(h.SequenceEqual(afterH)); Assert.True(v.SequenceEqual(afterV));
        Assert.True(Reachable(witness).Any(item => ReferenceEquals(item, eq.ExactLengths)));
        var source = Polygon([(0, 0), (4, 0), (4, 4), (0, 4)]) with { TurnModel = false };
        var noMetric = JointShapeDescription.Analyze(source); var noMetricEq = JointEquivalenceEvidence.Analyze(noMetric);
        var plain = Sample(noMetric.Message, [2], [1]);
        Assert.False(ExtentGapRepair.TryRepair(noMetricEq, plain, noMetric.Message, [2], [1], 0, out var unchangedH, out var unchangedV, out _));
        Assert.True(unchangedH.SequenceEqual(new[] { 2 })); Assert.True(unchangedV.SequenceEqual(new[] { 1 }));
    }

    private static JointShapeReconstruction.Candidate Sample(JointShapeDescription.Message message, int[] h, int[] v, IElement? selectedReach = null)
    {
        var author = new LatticeLadderAuthor(message.Frame, selectedReach ?? new Placeholder());
        Dictionary<IElement, int> Buckets(Relation axis) => E((Relation)axis.Right).Cast<Relation>().SelectMany((bucket, index) => E(bucket).Select(role => (role, index)))
            .ToDictionary(pair => pair.role, pair => pair.index, FieldComparer.Instance);
        var hs = Buckets(message.Horizontal); var vs = Buckets(message.Vertical);
        var positions = S(E(message.Roles).Select(role => (IElement)new Relation(role, author.Word(h.Take(hs[role]).Sum(), v.Take(vs[role]).Sum()))).ToArray());
        var construction = JointShapeReconstruction.Construction(message, positions, S(), S(), S());
        return JointShapeReconstruction.Propose(message, 0, construction: construction);
    }
    private static IElement SharedReach(JointShapeReconstruction.Candidate sample) => E(sample.Positions.Values.First(word => E(word).Length != 0)).Cast<Relation>().First().Right;
    private static NativeLatticeSources.SourceCase Polygon((int X, int Y)[] points)
    {
        var author = new LatticeLadderAuthor(); var sites = points.Select(_ => (IElement)new Placeholder()).ToArray();
        var edges = points.Select((point, index) => {
            var next = points[(index + 1) % points.Length];
            return (IElement)QualitativeStrokeGraph.Stroke(new Placeholder(), sites[index], sites[(index + 1) % points.Length], author.Word(next.X - point.X, next.Y - point.Y));
        }).ToArray();
        var graph = new QualitativeStrokeGraph(author.Frame, S(sites), S(edges));
        return new("anonymous", "anonymous", "", graph, S(points.Select((point, index) => (IElement)new Relation(sites[index], author.Word(point.X, point.Y))).ToArray()), graph.Strokes, S(), true);
    }
    private sealed class FieldComparer : IEqualityComparer<IElement>
    {
        public static FieldComparer Instance { get; } = new();
        public bool Equals(IElement? a, IElement? b) => ReferenceEquals(a, b);
        public int GetHashCode(IElement value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
    }
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item)) { if (!seen.Add(item)) continue; yield return item; if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); } }
    }
    private static IElement[] E(Relation scope) => StructuralPotential.Items(scope);
    private static Relation S(params IElement[] rows) => StructuralConstraint.Closed(rows);
}
