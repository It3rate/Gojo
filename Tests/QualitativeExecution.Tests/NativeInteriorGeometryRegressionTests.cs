using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace QualitativeExecution.Tests;

public sealed class NativeInteriorGeometryRegressionTests
{
    [Fact]
    public void A_cardinal_and_mixed_corner_stays_possible_when_the_angle_model_is_open()
    {
        // Equal-length basis traversals can have an obtuse angle. For example,
        // under a 120-degree interpretation, R + 2U is perpendicular to R.
        // That illustration is not input to the native reader: only these
        // ordered words and the absence of an equal-turn promise are supplied.
        var source = Fixture([(0, 0), (2, 0), (3, 2), (1, 2)], turnModel: false);
        var read = Assert.Single(new NativeInteriorFeatures(new NativeInteriorTopology(source)).Reads);
        Assert.Equal("Yes", read.Find("Four-sided").Evidence.Answer);
        Assert.Equal("Open", read.Find("Rectangle").Evidence.Answer);
        var selected = source with { TurnModel = true };
        var underQuarterTurns = Assert.Single(new NativeInteriorFeatures(new NativeInteriorTopology(selected)).Reads);
        Assert.Equal("No", underQuarterTurns.Find("Rectangle").Evidence.Answer);
    }

    [Fact]
    public void Unresolved_diagonal_coorientation_does_not_refute_a_possible_four_sided_read()
    {
        // The first two actual passages have words 2R+U and 4R+2U. A reader
        // without a general proportional-direction law may leave their common
        // heading open, but it must not turn that uncertainty into five known
        // geometric sides and refute the four-sided interpretation.
        var source = Fixture([(0, 0), (2, 1), (6, 3), (6, 6), (0, 6)]);
        var topology = new NativeInteriorTopology(source);
        var read = Assert.Single(new NativeInteriorFeatures(topology).Reads);
        Assert.NotEqual("No", read.Find("Four-sided").Evidence.Answer);
        Assert.NotEqual("Yes", read.Find("Hexagon").Evidence.Answer);
    }

    [Fact]
    public void Discovered_geometry_does_not_republish_private_execution_or_original_site_identities()
    {
        var privateDetail = new Relation(new Placeholder(), new Placeholder());
        var privateCause = new Relation(privateDetail, new Placeholder());
        var source = Fixture([(0, 0), (4, 0), (4, 4), (0, 4)], cause: privateCause);
        source = source with { Provenance = new Relation(source.Bindings, privateDetail) };
        var topology = new NativeInteriorTopology(source);
        var features = new NativeInteriorFeatures(topology);
        var publicParticipants = Reachable(features.Surface).ToHashSet(ReferenceEqualityComparer.Instance);
        Assert.DoesNotContain(privateDetail, publicParticipants);
        Assert.DoesNotContain(privateCause, publicParticipants);
        Assert.DoesNotContain(source.Graph.Surface, publicParticipants);
        Assert.DoesNotContain(source.Bindings, publicParticipants);
        Assert.DoesNotContain(source.Provenance, publicParticipants);
        Assert.All(E(source.Graph.Sites), site => Assert.DoesNotContain(site, publicParticipants));
        Assert.All(E(source.Graph.Strokes), stroke => Assert.DoesNotContain(stroke, publicParticipants));
        // The actual exposed frame is deliberately shared and remains public.
        Assert.Contains(source.Graph.Frame.Surface, publicParticipants);
        Assert.Single(features.Reads);
    }

    private static NativeLatticeSources.SourceCase Fixture((int X, int Y)[] vertices,
        bool turnModel = true, IElement? cause = null)
    {
        var author = new LatticeLadderAuthor();
        var sites = vertices.Select(_ => (IElement)new Placeholder()).ToArray();
        var passages = vertices.Select((from, index) =>
        {
            var to = vertices[(index + 1) % vertices.Length];
            return (IElement)QualitativeStrokeGraph.Stroke(cause ?? new Placeholder(), sites[index], sites[(index + 1) % sites.Length],
                author.Word(to.X - from.X, to.Y - from.Y));
        }).ToArray();
        var graph = new QualitativeStrokeGraph(author.Frame, S(sites), S(passages));
        return new("audit fixture", "audit fixture", "native geometry audit", graph,
            S(vertices.Select((point, index) => (IElement)new Relation(sites[index], author.Word(point.X, point.Y))).ToArray()),
            graph.Strokes, S(), turnModel);
    }
    private static Relation S(params IElement[] rows) => StructuralConstraint.Closed(rows);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var rows, out _, out _) ? rows : [];
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; yield return item;
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
