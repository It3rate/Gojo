using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class RelationalShapeCorpusTests
{
    private static readonly Lazy<IReadOnlyList<RelationalShapeCorpus.Case>> Sources = new(RelationalShapeCorpus.Cases);

    [Fact]
    public void One_hundred_preexisting_native_sources_cover_twenty_structural_families()
    {
        var sources = Sources.Value;
        Assert.Equal(100, sources.Count);
        Assert.True(sources.Select(source => source.Id).SequenceEqual(Enumerable.Range(1, 100).Select(index => $"R{index:000}")));
        Assert.Equal(20, sources.Select(source => source.Control.Family).Distinct().Count());
        Assert.True(sources.GroupBy(source => source.Control.Family).All(family => family.Count() == 5));
        Assert.True(sources.All(source => E(source.Source.Graph.Sites).Length is >= 3 and <= 40));
        Assert.True(sources.All(source => E(source.Source.Graph.Strokes).Length is >= 2 and <= 60));
        Assert.True(sources.Select(source => E(source.Source.Graph.Sites).Length).Distinct().Count() >= 12);
    }

    [Fact]
    public void Every_published_passage_has_native_endpoints_and_complete_inventory_without_teacher_hints()
    {
        foreach (var item in Sources.Value)
        {
            var source = item.Source;
            Assert.Equal("public-source", source.Id); Assert.Equal("Native source", source.Name);
            Assert.Empty(E(source.Provenance));
            Assert.True(Closed(source.Graph.Sites)); Assert.True(Closed(source.Graph.Strokes));
            Assert.True(Closed(source.Bindings)); Assert.True(Closed(source.StraightPassages));
            Assert.False(OrnamentReconstruction.Participants(new Relation(source.Graph.Surface, source.Bindings))
                .Any(participant => participant is Carrier or CarrierLocation));
            foreach (var itemStroke in E(source.Graph.Strokes))
            {
                Assert.True(itemStroke is Relation);
                var stroke = (Relation)itemStroke;
                Assert.True(QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out var word));
                Assert.False(ReferenceEquals(from, to));
                Assert.True(E(source.Graph.Sites).Any(site => ReferenceEquals(site, from)));
                Assert.True(E(source.Graph.Sites).Any(site => ReferenceEquals(site, to)));
                Assert.True(E(source.StraightPassages).Any(passage => ReferenceEquals(passage, stroke)));
                var frame = source.Graph.Frame;
                Assert.True(frame.TryReadResult(frame.Reduce(Position(source, from), word), out var end, out _, out var pending));
                Assert.Empty(E(pending));
                Assert.True(frame.TryReadResult(frame.Reduce(end, Inverse(frame, Position(source, to))),
                    out var remainder, out _, out pending));
                Assert.Empty(E(pending)); Assert.Empty(E(remainder));
            }
        }
    }

    [Fact]
    public void Branch_incidence_is_distinct_from_unjoined_ink_crossings()
    {
        var joined = Case("R068").Source; var unjoined = Case("R071").Source;
        Assert.Equal(5, E(joined.Graph.Sites).Length); Assert.Equal(4, E(joined.Graph.Strokes).Length);
        Assert.Equal(1, Degrees(joined).Count(degree => degree == 4));
        Assert.Equal(4, E(unjoined.Graph.Sites).Length); Assert.Equal(2, E(unjoined.Graph.Strokes).Length);
        Assert.True(Degrees(unjoined).All(degree => degree == 1));
        Assert.Equal(1, Components(joined)); Assert.Equal(2, Components(unjoined));
        var overlap = Case("R074").Source;
        Assert.Equal(2, Components(overlap)); Assert.True(Degrees(overlap).All(degree => degree == 2));
    }

    [Fact]
    public void Damaged_templates_keep_the_observed_gap_and_an_independent_withheld_complete_control()
    {
        var damaged = Sources.Value.Where(source => source.Control.CompleteTarget is not null).ToArray();
        Assert.Equal(5, damaged.Length);
        Assert.True(damaged.Select(source => source.Id).SequenceEqual(new[] { "R081", "R082", "R083", "R084", "R085" }));
        foreach (var item in damaged)
        {
            Assert.Equal(2, Degrees(item.Source).Count(degree => degree == 1));
            Assert.True(Degrees(item.Control.CompleteTarget!).All(degree => degree == 2));
            var observed = OrnamentReconstruction.Participants(new Relation(item.Source.Graph.Surface, item.Source.Bindings))
                .ToHashSet(ReferenceEqualityComparer.Instance);
            Assert.False(observed.Contains(item.Control.CompleteTarget!.Graph.Surface));
            Assert.False(observed.Contains(item.Control.Surface));
            Assert.False(ReferenceEquals(item.Source.Graph.Frame, item.Control.CompleteTarget.Graph.Frame));
        }
    }

    [Fact]
    public void Repeated_reflected_and_turned_closed_units_remain_independent_components()
    {
        foreach (var item in Sources.Value.Skip(40).Take(15))
        {
            Assert.True(Degrees(item.Source).All(degree => degree == 2));
            Assert.True(Components(item.Source) is >= 2 and <= 4);
        }
        Assert.Equal(3, Components(Case("R043").Source));
        Assert.Equal(4, Components(Case("R051").Source));
        Assert.Equal(4, Components(Case("R054").Source));
        Assert.Equal(3, Components(Case("R058").Source));
    }

    [Fact]
    public void Closed_context_does_not_make_all_observed_sources_closed()
    {
        foreach (var item in Sources.Value.Skip(65).Take(15))
            if (item.Id != "R074") Assert.True(Degrees(item.Source).Any(degree => degree == 1));
        Assert.True(Degrees(Case("R099").Source).Any(degree => degree == 1));
        Assert.Equal(1, Components(Case("R100").Source));
        Assert.Equal(2, Degrees(Case("R100").Source).Count(degree => degree == 3));
        Assert.True(Degrees(Case("R100").Source).All(degree => degree >= 2));
    }

    private static RelationalShapeCorpus.Case Case(string id) => Sources.Value.Single(item => item.Id == id);
    private static Relation Position(NativeLatticeSources.SourceCase source, IElement site)
    {
        var bindings = E(source.Bindings).OfType<Relation>().Where(row => ReferenceEquals(row.Left, site)).ToArray();
        Assert.Single(bindings); Assert.True(bindings[0].Right is Relation); return (Relation)bindings[0].Right;
    }
    private static int[] Degrees(NativeLatticeSources.SourceCase source) => E(source.Graph.Sites).Select(site =>
        E(source.Graph.Strokes).OfType<Relation>().Count(stroke => QualitativeStrokeGraph.TryReadStroke(stroke,
            out _, out var from, out var to, out _) && (ReferenceEquals(from, site) || ReferenceEquals(to, site)))).ToArray();
    private static int Components(NativeLatticeSources.SourceCase source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var count = 0;
        foreach (var site in E(source.Graph.Sites))
        {
            if (!seen.Add(site)) continue;
            count++; var queue = new Queue<IElement>(); queue.Enqueue(site);
            while (queue.TryDequeue(out var at))
                foreach (var stroke in E(source.Graph.Strokes).OfType<Relation>())
                {
                    QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _);
                    var other = ReferenceEquals(at, from) ? to : ReferenceEquals(at, to) ? from : null;
                    if (other is not null && seen.Add(other)) queue.Enqueue(other);
                }
        }
        return count;
    }
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var rows, out _, out _) ? rows : [];
    private static Relation Inverse(Gojo.Experiments.QualitativeExecution.QualitativeMotionFrame frame, Relation word) =>
        StructuralConstraint.Closed(E(word).Reverse().Cast<Relation>().Select(move => (IElement)
            Gojo.Experiments.QualitativeExecution.QualitativeMotionFrame.Move(
                E(frame.Opposites).Cast<Relation>().Single(row => ReferenceEquals(row.Left, move.Left)).Right,
                move.Right)).ToArray());
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}
