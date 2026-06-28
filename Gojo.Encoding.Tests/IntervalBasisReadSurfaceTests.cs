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
        var axis = new Carrier();
        var definitions = new List<Relation>();

        Relation Add(
            string name,
            IElement body,
            IReadOnlyList<IElement>? dependencies = null,
            IElement? residueOrTension = null)
        {
            var definition = ConceptDefinitionSurface.Create(
                new Carrier(),
                name,
                body,
                dependencies,
                residueOrTension: residueOrTension);
            definitions.Add(definition);
            return definition;
        }

        var interval = Add("shared span", Relation.Segment(axis, 0, 12));
        var start = Add("alpha", Relation.Point(axis, 0), [interval]);
        var center = Add("balance", Relation.Point(axis, 6), [interval]);
        var end = Add("omega", Relation.Point(axis, 12), [interval]);
        var entry = Add("entry", Relation.Segment(axis, 0, 2), [interval, start]);
        var middle = Add("middle", Relation.Segment(axis, 5, 7), [interval, center]);
        var exit = Add("exit", Relation.Segment(axis, 9, 12), [interval, end]);
        var spatialContext = Add("spatial context", ConceptAddressSurface.Atom(new Carrier(), "spatial"));
        var temporalContext = Add("temporal context", ConceptAddressSurface.Atom(new Carrier(), "temporal"));

        AddProjection("spatial entry projection", entry, spatialContext, "near");
        AddProjection("spatial entry alternate projection", entry, spatialContext, "close-ish");
        AddProjection("spatial middle projection", middle, spatialContext, "middle");
        AddProjection("spatial exit projection", exit, spatialContext, "far");
        AddProjection("temporal entry projection", entry, temporalContext, "before");
        AddProjection("temporal middle projection", middle, temporalContext, "during");
        AddProjection("temporal exit projection", exit, temporalContext, "after");

        var ledger = ConceptDefinitionSurface.Ledger(new Carrier(), definitions);

        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(ledger, out var graph));
        Assert.True(ContextualProjectionReadSurface.TryReadAt(graph, spatialContext, axis, 1, out var spatialStart));
        Assert.True(ContextualProjectionReadSurface.TryReadAt(graph, temporalContext, axis, 1, out var temporalStart));
        Assert.True(ContextualProjectionReadSurface.TryReadAt(graph, spatialContext, axis, 10, out var spatialEnd));
        Assert.True(ContextualProjectionReadSurface.TryReadAt(graph, temporalContext, axis, 10, out var temporalEnd));

        Assert.Equal("start", spatialStart.Location.Role);
        Assert.Equal(spatialStart.Location.Role, temporalStart.Location.Role);
        Assert.Equal("near", spatialStart.Projection.ResultDebug);
        Assert.Equal("before", temporalStart.Projection.ResultDebug);
        Assert.Equal("end", spatialEnd.Location.Role);
        Assert.Equal(spatialEnd.Location.Role, temporalEnd.Location.Role);
        Assert.Equal("far", spatialEnd.Projection.ResultDebug);
        Assert.Equal("after", temporalEnd.Projection.ResultDebug);
        Assert.True(ContextualProjectionReadSurface.TryReadAtAll(graph, spatialContext, axis, 1, out var spatialStartReads));
        Assert.Equal(["near", "close-ish"], spatialStartReads.Select(item => item.Projection.ResultDebug).ToArray());

        var unresolvedContext = Add("unresolved context", ConceptAddressSurface.Atom(new Carrier(), "unresolved"));
        var unresolvedLedger = ConceptDefinitionSurface.Ledger(new Carrier(), definitions);

        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(unresolvedLedger, out var unresolvedGraph));
        Assert.True(IntervalBasisReadSurface.TryLocate(unresolvedGraph, axis, 1, out var unresolvedLocation));
        Assert.Equal("start", unresolvedLocation.Role);
        Assert.False(ContextualProjectionReadSurface.TryReadAt(unresolvedGraph, unresolvedContext, axis, 1, out _));

        var snapshot = GojoStructureEncodingSurface.CreateDiagnosticSnapshot(
            new Carrier(),
            "grammar/contextual-interval-projection",
            unresolvedLedger);
        var document = GojoStructureEncodingJson.StructureFromSurface(snapshot);
        var json = GojoStructureEncodingJson.SerializeStructure(document);
        var deserialized = GojoStructureEncodingJson.DeserializeStructure(json);
        var rewritten = GojoStructureEncodingJson.SerializeStructure(deserialized);

        Assert.Equal(json, rewritten);
        Assert.Equal("grammar/contextual-interval-projection", deserialized.SourceId);
        Assert.NotEmpty(deserialized.Relations);
        Assert.NotEmpty(deserialized.CarrierLocations);

        void AddProjection(string name, Relation band, Relation context, string projection)
        {
            Add(
                name,
                ContextualProjectionReadSurface.Create(
                    new Carrier(),
                    band,
                    context,
                    EncodedTextSurface.Create(new Carrier(), projection),
                    EncodedTextSurface.Create(new Carrier(), "context projection, not interval structure"),
                    context),
                [band, context],
                EncodedTextSurface.Create(new Carrier(), "projection remains local to this context"));
        }
    }
}
