using System.Text.Json.Nodes;
using Gojo.Core;
using Gojo.Encoding;

namespace Gojo.Encoding.Tests;

public sealed class IntervalBasisReadSurfaceTests
{
    [Fact]
    public void IntervalReaderUsesStructureRatherThanBandNames()
    {
        var axis = new Carrier();
        var definitions = new List<Relation>();

        Relation Add(
            string name,
            IElement body,
            IReadOnlyList<IElement>? dependencies = null)
        {
            var definition = ConceptDefinitionSurface.Create(
                new Carrier(),
                name,
                body,
                dependencies);
            definitions.Add(definition);
            return definition;
        }

        var interval = Add("useful span", Relation.Segment(axis, -2, 10));
        var start = Add("alpha marker", Relation.Point(axis, -2), [interval]);
        var center = Add("balance marker", Relation.Point(axis, 4), [interval]);
        var end = Add("omega marker", Relation.Point(axis, 10), [interval]);
        Add("entry window", Relation.Segment(axis, -2, 1), [interval, start]);
        Add("balance window", Relation.Segment(axis, 3, 5), [interval, center]);
        Add("exit window", Relation.Segment(axis, 7, 10), [interval, end]);
        Add("unrelated rail", Relation.Segment(new Carrier(), 0, 3));

        var ledger = ConceptDefinitionSurface.Ledger(new Carrier(), definitions);

        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(ledger, out var graph));
        Assert.True(IntervalBasisReadSurface.TryRead(graph, out var intervals));

        var read = Assert.Single(intervals);
        Assert.Equal("useful span", read.DefinitionName);
        Assert.Equal(-2, read.StartTicks);
        Assert.Equal(4, read.CenterTicks);
        Assert.Equal(10, read.EndTicks);
        Assert.Equal("alpha marker", read.StartLandmark);
        Assert.Equal("balance marker", read.CenterLandmark);
        Assert.Equal("omega marker", read.EndLandmark);

        Assert.Contains(read.Bands, band =>
            band.DefinitionName == "entry window" &&
            band.Role == "start");
        Assert.Contains(read.Bands, band =>
            band.DefinitionName == "balance window" &&
            band.Role == "center");
        Assert.Contains(read.Bands, band =>
            band.DefinitionName == "exit window" &&
            band.Role == "end");

        Assert.True(IntervalBasisReadSurface.TryLocate(graph, axis, 0, out var startLocation));
        Assert.Equal("entry window", startLocation.BandName);
        Assert.Equal("start", startLocation.Role);

        Assert.True(IntervalBasisReadSurface.TryLocate(graph, axis, 4, out var centerLocation));
        Assert.Equal("balance window", centerLocation.BandName);
        Assert.Equal("center", centerLocation.Role);

        Assert.True(IntervalBasisReadSurface.TryLocate(graph, axis, 9, out var endLocation));
        Assert.Equal("exit window", endLocation.BandName);
        Assert.Equal("end", endLocation.Role);
    }

    [Fact]
    public void ContextProjectionMapsSameIntervalRolesDifferently()
    {
        var sample = CreateContextualIntervalProjectionSample();
        var axis = sample.Axis;
        var spatialContext = sample.SpatialContext;
        var temporalContext = sample.TemporalContext;
        var unresolvedContext = sample.UnresolvedContext;
        var ledger = sample.Ledger;

        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(ledger, out var graph));
        Assert.True(ContextualProjectionReadSurface.TryReadAt(graph, spatialContext, axis, 1, out var spatialStart));
        Assert.True(ContextualProjectionReadSurface.TryReadAt(graph, temporalContext, axis, 1, out var temporalStart));
        Assert.True(ContextualProjectionReadSurface.TryReadAt(graph, spatialContext, axis, 10, out var spatialEnd));
        Assert.True(ContextualProjectionReadSurface.TryReadAt(graph, temporalContext, axis, 10, out var temporalEnd));

        Assert.Equal("start", spatialStart.Location.Role);
        Assert.Equal(spatialStart.Location.Role, temporalStart.Location.Role);
        Assert.Equal("near", sample.LabeledLedger.ProjectionResultLabel(spatialStart.Projection));
        Assert.Equal("before", sample.LabeledLedger.ProjectionResultLabel(temporalStart.Projection));
        Assert.Equal("end", spatialEnd.Location.Role);
        Assert.Equal(spatialEnd.Location.Role, temporalEnd.Location.Role);
        Assert.Equal("far", sample.LabeledLedger.ProjectionResultLabel(spatialEnd.Projection));
        Assert.Equal("after", sample.LabeledLedger.ProjectionResultLabel(temporalEnd.Projection));
        Assert.True(ContextualProjectionReadSurface.TryReadAtAll(graph, spatialContext, axis, 1, out var spatialStartReads));
        Assert.Equal(
            ["near", "close-ish"],
            spatialStartReads.Select(item => sample.LabeledLedger.ProjectionResultLabel(item.Projection)).ToArray());

        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(ledger, out var unresolvedGraph));
        Assert.True(IntervalBasisReadSurface.TryLocate(unresolvedGraph, axis, 1, out var unresolvedLocation));
        Assert.Equal("start", unresolvedLocation.Role);
        Assert.False(ContextualProjectionReadSurface.TryReadAt(unresolvedGraph, unresolvedContext, axis, 1, out _));

        var document = CompactStructureDocumentFrom(sample);
        var json = GojoStructureCompactJson.Serialize(document);
        var deserialized = GojoStructureCompactJson.Deserialize(json);
        var rewritten = GojoStructureCompactJson.Serialize(deserialized);

        Assert.Equal(json, rewritten);
        Assert.Equal("grammar/contextual-interval-projection", deserialized.SourceId);
        Assert.NotEmpty(deserialized.Nodes);
        AssertReloadedSpatialStartAlternatives(deserialized);
    }

    [Fact]
    public void ContextProjectionDiagnosticJsonFixtureReloadsBackIntoReadableObjects()
    {
        var sample = CreateContextualIntervalProjectionSample();
        var generated = GojoStructureCompactJson.Serialize(CompactStructureDocumentFrom(sample));
        var fixture = File.ReadAllText(ContextProjectionFixturePath());
        var reread = GojoStructureCompactJson.Deserialize(fixture);
        var rewritten = GojoStructureCompactJson.Serialize(reread);

        // The checked-in fixture exercises the legacy folded format. New
        // archives retain graph identities that format could not represent.
        AssertJsonEquivalent(fixture, rewritten);
        var archive = GojoStructureCompactJson.Deserialize(generated);
        Assert.Equal(3, archive.Version);
        AssertReloadedSpatialStartAlternatives(archive);
        Assert.InRange(reread.Counts.Carriers, 1, 11);
        Assert.InRange(reread.Counts.Nodes, 1, 90);
        Assert.Equal(39, reread.Counts.Groups);
        Assert.Equal(4, reread.Counts.Segments);
        Assert.Equal(0, reread.Counts.Relations);
        AssertReloadedSpatialStartAlternatives(reread);
    }

    [Fact]
    public void ContextProjectionCompactInterchangeJsonReloadsBackIntoReadableObjects()
    {
        var generated = ConceptDefinitionInterchangeJson.Serialize(CompactDocument());
        var fixture = File.ReadAllText(ContextProjectionCompactFixturePath());
        var reread = ConceptDefinitionInterchangeJson.Deserialize(fixture);
        var rewritten = ConceptDefinitionInterchangeJson.Serialize(reread);

        AssertJsonEquivalent(fixture, generated);
        AssertJsonEquivalent(generated, rewritten);
        Assert.Equal(
            "witness",
            Assert.Single(reread.Definitions, definition => definition.Id == "start read").Body.Kind);
        AssertCompactReloadedSpatialStartAlternatives(reread);
    }

    private static GojoCompactStructureDocument CompactStructureDocumentFrom(ContextualIntervalProjectionSample sample) =>
        GojoStructureCompactJson.FromElement(
            "grammar/contextual-interval-projection",
            sample.Ledger,
            sample.LabeledLedger.CarrierLabels);

    private static ContextualIntervalProjectionSample CreateContextualIntervalProjectionSample()
    {
        var loaded = ConceptDefinitionInterchangeJson.ToLabeledLedger(CompactDocument());
        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(loaded.Ledger, out var graph));
        Assert.True(IntervalBasisReadSurface.TryRead(graph, out var intervals));

        return new ContextualIntervalProjectionSample(
            loaded.Ledger,
            Assert.Single(intervals).Carrier,
            loaded.SingleDefinition("spatial context"),
            loaded.SingleDefinition("temporal context"),
            loaded.SingleDefinition("unresolved context"),
            loaded);
    }

    private static ConceptDefinitionInterchangeDocument CompactDocument() =>
        new()
        {
            SourceId = "grammar/contextual-interval-projection",
            Carriers = ["interval-axis", "context-axis", "interval-role-axis"],
            Definitions =
            [
                new()
                {
                    Id = "shared span",
                    Body = new() { Kind = "segment", Carrier = "interval-axis", Start = 0, End = 12 },
                    DependsOn = []
                },
                new()
                {
                    Id = "spatial context",
                    Body = new() { Kind = "point", Carrier = "context-axis", Ticks = 1 },
                    DependsOn = []
                },
                new()
                {
                    Id = "temporal context",
                    Body = new() { Kind = "point", Carrier = "context-axis", Ticks = 2 },
                    DependsOn = []
                },
                new()
                {
                    Id = "unresolved context",
                    Body = new() { Kind = "point", Carrier = "context-axis", Ticks = 3 },
                    DependsOn = []
                },
                IntervalRoleRead("start read", roleTicks: 1, start: 0, end: 2),
                IntervalRoleRead("center read", roleTicks: 2, start: 5, end: 7),
                IntervalRoleRead("end read", roleTicks: 3, start: 9, end: 12),
                Projection("spatial entry projection", "start read", "spatial context", "near"),
                Projection("spatial entry alternate projection", "start read", "spatial context", "close-ish"),
                Projection("spatial middle projection", "center read", "spatial context", "middle"),
                Projection("spatial exit projection", "end read", "spatial context", "far"),
                Projection("temporal entry projection", "start read", "temporal context", "before"),
                Projection("temporal middle projection", "center read", "temporal context", "during"),
                Projection("temporal exit projection", "end read", "temporal context", "after")
            ]
        };

    private static ConceptDefinitionInterchangeDefinition IntervalRoleRead(
        string id,
        long roleTicks,
        long start,
        long end) =>
        new()
        {
            Id = id,
            Body = new()
            {
                Kind = "witness",
                Selected = "shared span",
                ReadSurface = new() { Kind = "point", Carrier = "interval-role-axis", Ticks = roleTicks },
                Result = new() { Kind = "segment", Carrier = "interval-axis", Start = start, End = end },
                ResidueOrTension = "interval role read",
                Provenance = "shared span"
            },
            DependsOn = ["shared span"],
            ResidueOrTension = "derived interval role"
        };

    private static ConceptDefinitionInterchangeDefinition Projection(
        string id,
        string selected,
        string context,
        string result) =>
        new()
        {
            Id = id,
            Body = new()
            {
                Kind = "projection",
                Selected = selected,
                Context = context,
                ResultText = result,
                ResidueOrTension = "context projection",
                Provenance = context
            },
            DependsOn = [selected, context],
            ResidueOrTension = "local projection"
        };

    private static void AssertReloadedSpatialStartAlternatives(GojoCompactStructureDocument document)
    {
        var rehydrated = GojoStructureCompactJson.ToElement(document);
        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(rehydrated.Root, out var graph));
        Assert.True(TryFindPointBody(graph, ticks: 1, dependencyCount: 0, out var spatialContext));
        Assert.True(IntervalBasisReadSurface.TryRead(graph, out var intervals));

        var interval = Assert.Single(intervals);
        Assert.True(ContextualProjectionReadSurface.TryReadAtAll(
            graph,
            spatialContext.Definition,
            interval.Carrier,
            1,
            out var spatialStartReads));
        Assert.Equal(
            [1, 2],
            spatialStartReads.Select(item => PointTicks(item.Projection.Result)).ToArray());
    }

    private static bool TryFindPointBody(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        long ticks,
        int dependencyCount,
        out ConceptDefinitionDiscoverySurface.DefinitionNode node)
    {
        foreach (var candidate in graph.Nodes)
        {
            if (candidate.Read.Dependencies.Length == dependencyCount &&
                TryReadPoint(candidate.Read.Body, out var point) &&
                point.Ticks == ticks)
            {
                node = candidate;
                return true;
            }
        }

        node = default;
        return false;
    }

    private static long PointTicks(IElement element)
    {
        Assert.True(TryReadPoint(element, out var point));
        return point.Ticks;
    }

    private static bool TryReadPoint(IElement element, out CarrierLocation location)
    {
        if (element is Relation point &&
            point.Left is CarrierLocation pointLocation &&
            point.Right is Carrier pointCarrier &&
            pointLocation.Carrier.SameIdentityAs(pointCarrier))
        {
            location = pointLocation;
            return true;
        }

        location = default;
        return false;
    }

    private static void AssertCompactReloadedSpatialStartAlternatives(
        ConceptDefinitionInterchangeDocument document)
    {
        var loaded = ConceptDefinitionInterchangeJson.ToLabeledLedger(document);
        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(loaded.Ledger, out var graph));
        var spatialContext = loaded.SingleDefinition("spatial context");
        Assert.True(IntervalBasisReadSurface.TryRead(graph, out var intervals));

        var interval = Assert.Single(intervals);
        Assert.True(ContextualProjectionReadSurface.TryReadAtAll(
            graph,
            spatialContext,
            interval.Carrier,
            1,
            out var spatialStartReads));
        Assert.Equal(
            ["near", "close-ish"],
            spatialStartReads.Select(item => loaded.ProjectionResultLabel(item.Projection)).ToArray());

        AssertContainsNoEncodedLabelWords(loaded.Ledger, document);
    }

    private static void AssertContainsNoEncodedLabelWords(
        IElement element,
        ConceptDefinitionInterchangeDocument document)
    {
        var labels = document.Definitions
            .SelectMany(definition => new[]
            {
                definition.Id,
                definition.ResidueOrTension,
                definition.Body.Text,
                definition.Body.Selected,
                definition.Body.Context,
                definition.Body.ResultText,
                definition.Body.ResidueOrTension,
                definition.Body.Provenance
            })
            .Where(label => !string.IsNullOrEmpty(label))
            .ToHashSet(StringComparer.Ordinal);
        var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        Visit(element);

        void Visit(IElement item)
        {
            if (!visited.Add(item))
            {
                return;
            }

            if (EncodedTextSurface.TryRead(item, out var text) && labels.Contains(text))
            {
                Assert.Fail($"Compact reload should keep label word '{text}' outside Gojo structure.");
            }

            if (item is Relation relation)
            {
                Visit(relation.Left);
                Visit(relation.Right);
            }
        }
    }

    private static string ContextProjectionFixturePath() =>
        Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "EncoderJson",
            "grammar",
            "contextual-interval-projection.json");

    private static string ContextProjectionCompactFixturePath() =>
        Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "EncoderJson",
            "grammar",
            "contextual-interval-projection.compact.json");

    private static void AssertJsonEquivalent(string expected, string actual)
    {
        var expectedNode = JsonNode.Parse(expected);
        var actualNode = JsonNode.Parse(actual);
        Assert.True(JsonNode.DeepEquals(expectedNode, actualNode), actual);
    }

    private readonly record struct ContextualIntervalProjectionSample(
        Relation Ledger,
        Carrier Axis,
        Relation SpatialContext,
        Relation TemporalContext,
        Relation UnresolvedContext,
        ConceptDefinitionInterchangeLedger LabeledLedger);
}
