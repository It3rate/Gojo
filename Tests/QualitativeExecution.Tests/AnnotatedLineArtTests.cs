using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QuickDraw;
using static Gojo.Experiments.QuickDraw.SequentialEvidence;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class AnnotatedLineArtTests
{
    [Theory]
    [InlineData("L", "10,0;0,10")]
    [InlineData("I", "10,0;-10,0")]
    [InlineData("T", "10,0;-10,0;0,10")]
    [InlineData("Y", "10,0;-5,8;-5,-8")]
    [InlineData("X", "10,0;-10,0;0,10;0,-10")]
    [InlineData("K", "10,0;-10,0;5,8;5,-8")]
    [InlineData("unresolved", "10,0;20,0")]
    [InlineData("unresolved", "10,0;20,0;-10,0;-20,0")]
    public void JunctionLettersAreExactLocalRayReadsRatherThanInputLabels(string expected, string coordinates)
    {
        var positions = coordinates.Split(';').Select(pair => pair.Split(',').Select(int.Parse).ToArray()).ToArray();
        var sites = new List<object> { Site("c", 50, 50) }; var edges = new List<object>();
        for (var i = 0; i < positions.Length; i++)
        {
            var p = positions[i]; sites.Add(Site("s" + i, 50 + p[0], 50 + p[1]));
            edges.Add(Edge("e" + i, "c", "s" + i, [50, 50], [50 + p[0], 50 + p[1]]));
        }
        var read = Read(Scene(sites, edges)); Assert.Equal(expected, AnnotatedLineArt.JointKind(read, read.Sites[0]));
    }

    [Fact]
    public void SelfLoopUsesBothEndpointTangentsAndHasNoAutomaticOJunction()
    {
        var read = Read(Scene([Site("a", 50, 50)], [Edge("loop", "a", "a", [50, 50], [80, 50], [20, 50], [50, 50])]));
        Assert.Equal("I", AnnotatedLineArt.JointKind(read, Assert.Single(read.Sites)));
        Assert.Same(read.Edges[0].Curve.From, read.Edges[0].Curve.To); Assert.Empty(read.Regions);
    }

    [Fact]
    public void ExplicitContactHasProjectedTWhileActualSitesAndObjectMembershipStayDistinct()
    {
        var read = Read(Scene([Site("left", 10, 50), Site("branch", 50, 50), Site("right", 90, 50), Site("rope", 50, 50), Site("bottom", 50, 90)],
            [Edge("left", "left", "branch", [10, 50], [50, 50]), Edge("right", "branch", "right", [50, 50], [90, 50]), Edge("rope", "rope", "bottom", [50, 50], [50, 90])],
            objects: [new { id = "branch", edges = new[] { "left", "right" } }, new { id = "rope", edges = new[] { "rope" } }],
            contacts: [new { sites = new[] { "branch", "rope" } }]));
        Assert.Equal("I", AnnotatedLineArt.JointKind(read, read.Sites[1])); Assert.Equal("endpoint", AnnotatedLineArt.JointKind(read, read.Sites[3]));
        Assert.Equal("T", AnnotatedLineArt.ContactKind(read, Assert.Single(read.Contacts)));
        Assert.NotSame(read.Sites[1].Event, read.Sites[3].Event); Assert.Equal(read.Sites[1].Position, read.Sites[3].Position);
        Assert.DoesNotContain(read.Objects[1].Edges[0], read.Objects[0].Edges);
    }

    [Fact]
    public void CoincidenceWithoutAnExplicitContactDoesNotPublishAContactOrJoinedT()
    {
        var read = Read(Scene([Site("left", 10, 50), Site("a", 50, 50), Site("right", 90, 50), Site("b", 50, 50), Site("bottom", 50, 90)],
            [Edge("left", "left", "a", [10, 50], [50, 50]), Edge("right", "a", "right", [50, 50], [90, 50]), Edge("down", "b", "bottom", [50, 50], [50, 90])]));
        Assert.Empty(read.Contacts); Assert.DoesNotContain(read.Sites, site => AnnotatedLineArt.JointKind(read, site) == "T");
    }

    [Fact]
    public void NoncoincidentAuthoredContactRemainsAClaimWithAnUnresolvedProjectedJoint()
    {
        var read = Read(Scene([Site("a", 10, 10), Site("b", 11, 10)], [Edge("line", "a", "b", [10, 10], [11, 10])],
            contacts: [new { sites = new[] { "a", "b" } }]));
        Assert.Equal("unresolved", AnnotatedLineArt.ContactKind(read, Assert.Single(read.Contacts)));
    }

    [Fact]
    public void CrossingDoesNotInventASiteWhileASharedEndpointIsAnActualJoin()
    {
        var read = Read(Scene([Site("a", 10, 50), Site("b", 90, 50), Site("c", 50, 10), Site("d", 50, 90)],
            [Edge("h", "a", "b", [10, 50], [90, 50]), Edge("v", "c", "d", [50, 10], [50, 90])],
            crossings: [new { edges = new[] { "h", "v" }, site = new[] { 50, 50 } }]));
        var crossing = Assert.Single(read.Crossings); Assert.True(AnnotatedLineArt.Contains(crossing.First, crossing.At));
        Assert.True(AnnotatedLineArt.Contains(crossing.Second, crossing.At)); Assert.False(AnnotatedLineArt.JoinedAt(crossing));
        Assert.All(read.Sites, site => Assert.Equal("endpoint", AnnotatedLineArt.JointKind(read, site))); Assert.Null(crossing.Over);
        var joined = Read(Scene([Site("a", 10, 50), Site("c", 50, 50), Site("b", 90, 50)],
            [Edge("left", "a", "c", [10, 50], [50, 50]), Edge("right", "c", "b", [50, 50], [90, 50])],
            crossings: [new { edges = new[] { "left", "right" }, site = new[] { 50, 50 } }]));
        Assert.True(AnnotatedLineArt.JoinedAt(Assert.Single(joined.Crossings)));
    }

    [Fact]
    public void APointEdgeDoesNotContainEveryPointAndAnUnsolvedCurveHitStaysOpen()
    {
        var read = Read(Scene([Site("a", 10, 10), Site("b", 90, 10)],
            [Edge("point", "a", "a", [10, 10], [10, 10]), Edge("curve", "a", "b", [10, 10], [50, 90], [90, 10])]));
        Assert.False(AnnotatedLineArt.Contains(read.Edges[0], new(11, 10)));
        Assert.Null(AnnotatedLineArt.Contains(read.Edges[1], new(50, 50)));
        Assert.Equal("curved", AnnotatedLineArt.TraceKind(read.Edges[1]));
    }

    [Fact]
    public void RegionClosureRequiresAnActualIdentityCycleAndKeepsFigureSelectionSeparate()
    {
        var sites = new[] { Site("a", 10, 10), Site("b", 90, 10), Site("c", 50, 90), Site("lookalike", 10, 10) };
        var edges = new[] { Edge("ab", "a", "b", [10, 10], [90, 10]), Edge("bc", "b", "c", [90, 10], [50, 90]),
            Edge("ca", "c", "a", [50, 90], [10, 10]), Edge("fake", "c", "lookalike", [50, 90], [10, 10]) };
        var read = Read(Scene(sites, edges, regions: [Region("real", ["ab", "bc", "ca"], true), Region("open", ["ab", "bc", "fake"], null)]));
        Assert.True(read.Regions[0].Closed); Assert.False(read.Regions[1].Closed);
        Assert.Equal(1, read.Regions[0].Figure); Assert.Null(read.Regions[1].Figure);
        Assert.NotSame(read.Sites[0].Event, read.Sites[3].Event);
    }

    [Fact]
    public void ReverseBoundaryWalkUsesActualEdgeEndpointsAndParentsMustBeIntroducedEarlier()
    {
        var sites = new[] { Site("a", 10, 10), Site("b", 90, 10), Site("c", 50, 90) };
        var edges = new[] { Edge("ab", "a", "b", [10, 10], [90, 10]), Edge("bc", "b", "c", [90, 10], [50, 90]), Edge("ac", "a", "c", [10, 10], [50, 90]) };
        var boundary = new[] { new { edge = "ab", forward = true }, new { edge = "bc", forward = true }, new { edge = "ac", forward = false } };
        var read = Read(Scene(sites, edges, regions: [new { id = "outer", boundary, figure = true }, new { id = "inner", boundary, figure = false, parent = "outer" }]));
        Assert.True(read.Regions[0].Closed); Assert.Same(read.Regions[0], read.Regions[1].Parent);
        using var malformed = JsonDocument.Parse(JsonSerializer.Serialize(Scene(sites, edges,
            regions: [new { id = "inner", boundary, figure = false, parent = "later" }, new { id = "later", boundary, figure = true }])));
        Assert.Throws<KeyNotFoundException>(() => AnnotatedLineArt.Import(malformed.RootElement));
    }

    [Fact]
    public void ReflectionAndTranslationReadExactControlsAndKeepFailedCorrespondenceVisible()
    {
        var sites = new[] { Site("a", 10, 10), Site("b", 30, 30), Site("ma", 90, 10), Site("mb", 70, 30), Site("ta", 10, 50), Site("tb", 30, 70) };
        var edges = new[] { Edge("source", "a", "b", [10, 10], [10, 30], [30, 30]), Edge("mirror", "ma", "mb", [90, 10], [90, 30], [70, 30]),
            Edge("translated", "ta", "tb", [10, 50], [10, 70], [30, 70]) };
        var read = Read(Scene(sites, edges, correspondences: [
            new { kind = "mirror", source = new[] { "source" }, target = new[] { "mirror" }, axis = new[] { new[] { 50, 0 }, new[] { 50, 100 } } },
            new { kind = "repeat", source = new[] { "source" }, target = new[] { "translated" }, translation = new[] { 0, 40 } },
            new { kind = "repeat", source = new[] { "source" }, target = new[] { "translated" }, translation = new[] { 1, 40 } }]));
        Assert.True(read.Correspondences[0].Verified); Assert.True(read.Correspondences[1].Verified);
        Assert.False(read.Correspondences[2].Verified); Assert.False(read.Correspondences[2].GeometryMatches);
    }

    [Fact]
    public void CoordinateCorrectCopiesCannotFakeASharedSiteTransformation()
    {
        var read = Read(Scene([Site("a", 10, 10), Site("b", 30, 10), Site("c", 50, 10), Site("ta", 10, 50), Site("tb1", 30, 50), Site("tb2", 30, 50), Site("tc", 50, 50)],
            [Edge("ab", "a", "b", [10, 10], [30, 10]), Edge("bc", "b", "c", [30, 10], [50, 10]),
             Edge("tab", "ta", "tb1", [10, 50], [30, 50]), Edge("tbc", "tb2", "tc", [30, 50], [50, 50])],
            correspondences: [new { kind = "repeat", source = new[] { "ab", "bc" }, target = new[] { "tab", "tbc" }, translation = new[] { 0, 40 } }]));
        var map = Assert.Single(read.Correspondences); Assert.True(map.GeometryMatches); Assert.False(map.IdentityMatches); Assert.False(map.Verified);
    }

    [Fact]
    public void RestoredGeometryAndMembershipDoNotDependOnExternalDisplayNames()
    {
        var source = Scene([Site("a", 10, 10), Site("b", 90, 10)], [Edge("line", "a", "b", [10, 10], [90, 10])],
            objects: [new { id = "church", label = "Anything", edges = new[] { "line" } }],
            groups: [new { id = "body", role = "template", edges = new[] { "line" } }, new { id = "trim", role = "detail", edges = new[] { "line" } }]);
        var read = Read(source); Assert.Same(read.Edges[0], read.Objects[0].Edges[0]);
        Assert.False(read.Groups[0].Detail); Assert.True(read.Groups[1].Detail);
        Assert.Equal("straight", AnnotatedLineArt.TraceKind(read.Edges[0]));
        Assert.All(Reach(read.Surface).OfType<Carrier>(), carrier => { Assert.Equal(0UL, carrier.Value); Assert.Equal(0L, carrier.Tension); });
    }

    private static object Site(string id, int x, int y) => new { id, x, y };
    private static object Edge(string id, string from, string to, params int[][] controls) => new { id, from, to, controls };
    private static object Region(string id, string[] edges, bool? figure) => new { id, boundary = edges.Select(edge => new { edge, forward = true }).ToArray(), figure };
    private static object Scene(IEnumerable<object> sites, IEnumerable<object> edges, object[]? objects = null, object[]? regions = null,
        object[]? groups = null, object[]? correspondences = null, object[]? crossings = null, object[]? contacts = null) => new {
            id = "fixture", coordinateQuantum = 1, viewBox = new[] { 0, 0, 100, 100 }, sites = sites.ToArray(), edges = edges.ToArray(),
            strokes = Array.Empty<object>(), objects = objects ?? [], regions = regions ?? [], groups = groups ?? [], correspondences = correspondences ?? [], crossings = crossings ?? [], contacts = contacts ?? [] };
    private static AnnotatedLineArt.Read Read(object input)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(input)); var native = AnnotatedLineArt.Import(document.RootElement);
        var archive = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("anonymous", native));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(archive), out var restored));
        Assert.True(AnnotatedLineArt.TryRead(Assert.IsType<Relation>(restored.Root), out var read)); return read;
    }
    private static IElement[] Reach(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var element)) if (seen.Add(element) && element is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        return seen.ToArray();
    }
}
