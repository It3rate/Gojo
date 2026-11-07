using Gojo.Experiments.OrnamentFlowPilot;
using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Tests.OrnamentTension;

public class JointSignatureTests
{
    [Fact]
    public void ColoredCornersRotateAroundTheirSharedTipAndReverseTheTrace()
    {
        var source = FretSymmetrySource.ReadDirect();
        var read = JointSignature.Read(source.Segments, source.StoredWords, source.EmittedWords);
        var red = read.Corners.Single(c => c.Vertex == 6);
        var blue = read.Corners.Single(c => c.Vertex == 4);
        Assert.Equal(("left", (byte?)0x19, (byte?)0x13), (red.Turn, red.RawIn, red.RawOut));
        Assert.Equal(("right", (byte?)0x19, (byte?)0x21), (blue.Turn, blue.RawIn, blue.RawOut));
        var pair = read.Matches.Where(m => m.Source == 6 && m.Target == 4).ToArray();
        var rotation = Assert.Single(pair, m => m.Reversed);
        Assert.Equal("quarter-left", rotation.Frame);
        Assert.Equal(new JointPivot(12, -4), rotation.Pivot);
        Assert.Equal((1L, 1L, false, (bool?)false), (rotation.FirstCut, rotation.LastCut, rotation.EqualArms, rotation.SameRawOdv));
        var mirror = Assert.Single(pair, m => !m.Reversed);
        Assert.Equal(("vertical-mirror", 11L, -1L), (mirror.Frame, mirror.OffsetX, mirror.OffsetY));
        Assert.Null(mirror.Pivot);
        var tip = Assert.Single(read.QuarterTips, t => t.Source == 6 && t.Target == 4);
        Assert.True(tip.SamePathSite && tip.IncomingArm);
        Assert.Equal(rotation.Pivot, tip.Pivot);
    }

    [Fact]
    public void ReusedMotionBytesCanEmitDifferentPhysicalSigns()
    {
        var source = FretSymmetrySource.ReadGlide();
        var read = JointSignature.Read(source.Segments, source.StoredWords, source.EmittedWords);
        var mirror = Assert.Single(read.Matches, m => m.Source == 5 && m.Target == 13 && !m.Reversed);
        Assert.Equal("horizontal-mirror", mirror.Frame);
        Assert.True(mirror.SameRawBytes);
        Assert.False(mirror.SameEmittedOdv);
        Assert.Throws<ArgumentException>(() => JointSignature.Read(source.Segments, source.StoredWords, source.StoredWords));
        Assert.Null(JointSignature.Read(source.Segments).Matches[0].SameRawOdv);
    }

    [Fact]
    public void NativeQuarterTurnTipsAndGlobalHalfTurnCentersHaveDifferentSpacings()
    {
        var read = JointSignature.Read(FretSymmetrySource.Direct());
        Assert.Equal(4096L, read.CandidatesChecked);
        Assert.Equal(512, read.Matches.Length);
        Assert.Equal(80, read.Matches.Count(m => m.EqualArms));
        var local = Assert.Single(read.SharedEndQuarterRows);
        Assert.Equal(-4L, local.Y2);
        Assert.Equal(new long[] { 2, 4, 10, 12 }, local.X2);
        Assert.Equal(new long[] { 2, 6, 2, 6 }, local.Gaps2);
        var global = Assert.Single(read.WholeBandHalfTurnRows);
        Assert.Equal(new long[] { 3, 11 }, global.X2);
        Assert.Equal(new long[] { 8, 8 }, global.Gaps2);
    }

    [Fact]
    public void RevisitedCoordinatesDoNotSupplyTheSameNativeTip()
    {
        PathSite[] path = [new(1, 0), new(0, 1), new(-1, 0), new(0, -1),
            new(1, 0), new(0, 1), new(1, 0), new(0, -1)];
        var read = JointSignature.Read(path);
        Assert.Contains(read.QuarterTips, t => !t.SamePathSite);
        Assert.Contains(read.QuarterTips, t => t.TargetBay != 0 && t.SamePathSite);
    }

}
