using Gojo.Core;
using Gojo.Encoding;

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
            band.Role == "start" &&
            band.Qualifier == "near");
        Assert.Contains(read.Bands, band =>
            band.DefinitionName == "balance window" &&
            band.Role == "center" &&
            band.Qualifier == "middle");
        Assert.Contains(read.Bands, band =>
            band.DefinitionName == "exit window" &&
            band.Role == "end" &&
            band.Qualifier == "far");

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
}
