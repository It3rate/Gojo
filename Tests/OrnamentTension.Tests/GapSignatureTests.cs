using Gojo.Experiments.OrnamentFlowPilot;
using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Tests.OrnamentTension;

public class GapSignatureTests
{
    [Fact]
    public void EachInterlockStrokeHasALocalOneTickPartnerButSomeCrossSectionsAreWider()
    {
        var read = GapSignature.Read(FretSymmetrySource.Direct());
        Assert.Equal((8L, -4L, 0L), (read.Pitch, read.Bottom, read.Top));
        Assert.Equal(new GapWidth[] {
            new("x", 1, 10), new("x", 2, 4), new("x", 7, 2), new("y", 1, 12), new("y", 3, 4)
        }, read.Widths);
        Assert.Equal(16, read.Nearest.Length);
        foreach (var hit in read.Nearest)
        {
            Assert.Equal(1L, hit.Width);
            Assert.NotEmpty(hit.SpanIndices);
            foreach (int i in hit.SpanIndices)
            {
                var span = read.Spans[i];
                Assert.True(span.AcrossEnd > span.AcrossStart);
                Assert.Contains(hit.Stroke, span.LowerStrokes.Concat(span.UpperStrokes));
                Assert.Equal(hit.Width, span.Width);
            }
        }
        Assert.Equal(2, read.Regions.Length);
        Assert.All(read.Regions, r => { Assert.Equal(16m, r.Area); Assert.NotEqual(r.Bottom, r.Top); });
        Assert.Collection(read.Mouths,
            m => Assert.Equal(("bottom", 3L, 4L), (m.Side, m.Start, m.End)),
            m => Assert.Equal(("top", 7L, 8L), (m.Side, m.Start, m.End)));
        Assert.True(read.AlternatingMouths);
    }

    [Fact]
    public void EnlargingOnlyOneInnerReachIntroducesSealedPockets()
    {
        var read = GapSignature.Read(FretSymmetrySource.Direct(innerReach: 2));
        Assert.Equal(9L, read.Pitch);
        Assert.Equal(new[] { 2m, 2m }, read.Regions.Where(r => !r.Top && !r.Bottom).Select(r => r.Area));
        Assert.Equal(new[] { 16m, 16m }, read.Regions.Where(r => r.Top || r.Bottom).Select(r => r.Area));
        Assert.All(read.Mouths, m => Assert.Equal(2L, m.End - m.Start));
        Assert.True(read.AlternatingMouths);
    }

    [Fact]
    public void SeamCrossingMouthKeepsOneRegionAndUnpairedRailsHaveNoWidth()
    {
        PathSite[] bay = [new(1, 0), new(0, -2), new(2, 0), new(0, 2), new(1, 0)];
        var read = GapSignature.Read(bay);
        Assert.Collection(read.Mouths,
            m => Assert.Equal(("top", 1L, 3L), (m.Side, m.Start, m.End)),
            m => Assert.Equal(("bottom", 3L, 5L), (m.Side, m.Start, m.End)));
        Assert.True(read.AlternatingMouths);
        var wrapped = read.Regions.Single(r => r.Id == read.Mouths[1].Region);
        Assert.True(wrapped.Bottom);
        Assert.Equal(4m, wrapped.Area);
        foreach (int stroke in new[] { 0, 2, 4 }) Assert.Null(read.Nearest[stroke].Width);
    }

    [Fact]
    public void CollinearRetracesAppearAsSeparateReferencesButOneWall()
    {
        PathSite[] bay = [new(2, 0), new(-2, 0), new(2, 0), new(0, 2), new(-2, 0), new(0, -2), new(4, 0)];
        var read = GapSignature.Read(bay);
        var span = Assert.Single(read.Spans, s => s.Axis == "y");
        Assert.Equal(new[] { 0, 1, 2, 6 }, span.LowerStrokes);
        Assert.Equal(new[] { 4 }, span.UpperStrokes);
        Assert.Equal(new GapWidth("y", 2, 2), read.Widths.Single(w => w.Axis == "y"));
        Assert.False(read.AlternatingMouths);
    }

    [Fact]
    public void OverlappingBankOpeningsDoNotClaimAlongBandAlternation()
    {
        PathSite[] bay = [new(2, 0), new(0, 1), new(3, 0), new(0, -1), new(-1, 0), new(0, -2), new(1, 0), new(0, 2)];
        var read = GapSignature.Read(bay);
        Assert.Contains(read.Mouths, m => m.Side == "top");
        Assert.Contains(read.Mouths, m => m.Side == "bottom");
        Assert.False(read.AlternatingMouths);
    }
}
