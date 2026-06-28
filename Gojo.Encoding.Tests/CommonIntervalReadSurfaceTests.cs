using Gojo.Core;
using Gojo.Encoding;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class CommonIntervalReadSurfaceTests
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
        Assert.True(CommonIntervalReadSurface.TryRead(graph, out var intervals));

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

        Assert.True(CommonIntervalReadSurface.TryLocate(graph, axis, 0, out var startLocation));
        Assert.Equal("entry window", startLocation.BandName);
        Assert.Equal("start", startLocation.Role);

        Assert.True(CommonIntervalReadSurface.TryLocate(graph, axis, 4, out var centerLocation));
        Assert.Equal("balance window", centerLocation.BandName);
        Assert.Equal("center", centerLocation.Role);

        Assert.True(CommonIntervalReadSurface.TryLocate(graph, axis, 9, out var endLocation));
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
        AddProjection("spatial middle projection", middle, spatialContext, "middle");
        AddProjection("spatial exit projection", exit, spatialContext, "far");
        AddProjection("temporal entry projection", entry, temporalContext, "before");
        AddProjection("temporal middle projection", middle, temporalContext, "during");
        AddProjection("temporal exit projection", exit, temporalContext, "after");

        var ledger = ConceptDefinitionSurface.Ledger(new Carrier(), definitions);

        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(ledger, out var graph));
        Assert.True(ContextualIntervalProjectionReadSurface.TryProject(graph, spatialContext, axis, 1, out var spatialStart));
        Assert.True(ContextualIntervalProjectionReadSurface.TryProject(graph, temporalContext, axis, 1, out var temporalStart));
        Assert.True(ContextualIntervalProjectionReadSurface.TryProject(graph, spatialContext, axis, 10, out var spatialEnd));
        Assert.True(ContextualIntervalProjectionReadSurface.TryProject(graph, temporalContext, axis, 10, out var temporalEnd));

        Assert.Equal("start", spatialStart.Role);
        Assert.Equal(spatialStart.Role, temporalStart.Role);
        Assert.Equal("near", spatialStart.Projection);
        Assert.Equal("before", temporalStart.Projection);
        Assert.Equal("end", spatialEnd.Role);
        Assert.Equal(spatialEnd.Role, temporalEnd.Role);
        Assert.Equal("far", spatialEnd.Projection);
        Assert.Equal("after", temporalEnd.Projection);

        var unresolvedContext = Add("unresolved context", ConceptAddressSurface.Atom(new Carrier(), "unresolved"));
        var unresolvedLedger = ConceptDefinitionSurface.Ledger(new Carrier(), definitions);

        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(unresolvedLedger, out var unresolvedGraph));
        Assert.True(CommonIntervalReadSurface.TryLocate(unresolvedGraph, axis, 1, out var unresolvedLocation));
        Assert.Equal("start", unresolvedLocation.Role);
        Assert.False(ContextualIntervalProjectionReadSurface.TryProject(unresolvedGraph, unresolvedContext, axis, 1, out _));

        void AddProjection(string name, Relation band, Relation context, string projection)
        {
            Add(
                name,
                ReadWitness.Create(
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
