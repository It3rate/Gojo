using Gojo.Experiments.OrnamentFlowPilot;
using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Tests.OrnamentTension;

public class ReferencePatternTests
{
    [Fact]
    public void SelectedSourcesExecuteTheDeclaredThreeCourses()
    {
        var sources = ReferencePatterns.Read().ToDictionary(p => p.Id);
        Assert.Equal(new PathSite[] { new(3, 0), new(0, 4), new(3, 0), new(0, -2), new(-1, 0), new(0, -2) },
            sources["running-key"].Encoding.Segments);
        Assert.Equal(new PathSite[] { new(3, 0), new(0, -1), new(-1, 0), new(0, -1), new(3, 0), new(0, 1), new(-1, 0), new(0, 1) },
            sources["opposed-hook"].Encoding.Segments);
        Assert.Equal(new PathSite[] { new(1, 0), new(0, 1), new(1, 0), new(0, -1) },
            sources["battlement"].Encoding.Segments);
        foreach (var source in sources.Values)
        {
            var finite = PatternScorecard.ReadRepeatedCourse(source.Encoding.Segments);
            Assert.Equal(1, finite.NativeComponents);
            Assert.Equal(0, finite.Handoffs.Misses);
            Assert.Equal(0, finite.CollinearOverlaps);
            Assert.Equal(0, finite.UnjoinedContacts);
            var gaps = GapSignature.Read(source.Encoding.Segments);
            Assert.Equal(2, gaps.Regions.Length);
            Assert.True(gaps.AlternatingMouths);
        }
    }

    [Fact]
    public void SharedMotifDoesNotEraseRailsCrossingsOrDeferredInterpretations()
    {
        var selections = ReferencePatterns.Selections();
        Assert.Equal(28, selections.Length);
        Assert.Contains(selections, s => s.Source == "Attachment 2, row 1" && s.Status == "deferred");
        Assert.Contains(selections, s => s.Source == "Attachment 1, row 4" && s.Status == "deferred");
        Assert.Equal(3, selections.Count(s => s.Status == "selected"));
        Assert.Contains(selections, s => s.PatternId == "interlock");
    }
}
