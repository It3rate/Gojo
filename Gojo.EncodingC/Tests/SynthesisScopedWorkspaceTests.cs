using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class SynthesisScopedWorkspaceTests
{
    private const int Datum = 0;
    private const int Joint = 1;
    private const int Tip = 2;
    private const int Carried = 3;
    private const int SiteCount = 4;
    private const int CoordinateCapacity = 10;

    private static readonly Plane Selected = Plane.Of(SiteCount, Joint, Tip);

    // The two endpoints are written by the same row. If either computed
    // endpoint falls outside its shape, the row admits no change.
    private static readonly TransitionLaw ScaleAboutDatum = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Unary(2), Shape.Unary(CoordinateCapacity), Shape.Unary(CoordinateCapacity)],
        before =>
        {
            var (datum, gain) = (before[0], before[1]);
            var joint = datum + gain * (before[2] - datum);
            var tip = datum + gain * (before[3] - datum);
            return joint is >= 0 and <= CoordinateCapacity && tip is >= 0 and <= CoordinateCapacity
                ? [datum, gain, joint, tip]
                : null;
        },
        new Accounting(false, new Unchanged(0), new Unchanged(1)));

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void AScopedChangeKeepsTheSharedJointAndCommitsBothEndpoints(ExecutionConformancePath path)
    {
        var span = Arrange();
        var (candidate, scale) = Preview(span.Source, span.Origin, span.Gain, span.Coordinate);
        Assert.True(path.Preview(candidate, scale).Step.Unadmitted.None);
        Assert.Equal((3, 5, 5), Sites(candidate, span.Coordinate));
        Assert.False(candidate.SamePosition(Tip, candidate, Carried));
        Assert.Equal((2, 3, 5), Sites(span.Source, span.Coordinate));

        var split = candidate.Commit(Plane.Of(SiteCount, Joint));
        Assert.False(split.Committed);
        Assert.Equal(Plane.Of(SiteCount, Tip), split.Split);
        Assert.Equal((2, 3, 5), Sites(span.Source, span.Coordinate));

        var applied = candidate.Commit();
        Assert.True(applied.Committed);
        Assert.Equal(Selected, applied.Written);
        Assert.Equal((3, 5, 5), Sites(span.Source, span.Coordinate));
        span.Ladder.Observe(Joint);
        span.Ladder.Observe(Tip);
        Assert.Equal(3, span.Ladder.Read(Joint));
        Assert.Equal(5, span.Ladder.Read(Tip));

        var (next, another) = Preview(span.Source, span.Origin, span.Gain, span.Coordinate);
        path.Preview(next, another);
        Assert.Equal((5, 9, 5), Sites(next, span.Coordinate));
        Assert.True(next.Commit().Committed);
        Assert.Equal((5, 9, 5), Sites(span.Source, span.Coordinate));
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void AChangedCalibrationLeavesTheWholePatchStale(ExecutionConformancePath path)
    {
        var span = Arrange();
        var (candidate, scale) = Preview(span.Source, span.Origin, span.Gain, span.Coordinate);
        path.Preview(candidate, scale);
        span.Gain.Write(span.Source, Datum, 1);
        var commit = candidate.Commit();

        Assert.False(commit.Committed);
        Assert.Equal(Plane.Of(SiteCount, Datum), commit.Stale);
        Assert.True(commit.Written.None);
        Assert.Equal((2, 3, 5), Sites(span.Source, span.Coordinate));
        Assert.Equal((3, 5, 5), Sites(candidate, span.Coordinate));
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void AGroupedChangeSkipsAnExcludedEndpointAndRefusesAnOutOfRangeEndpoint(ExecutionConformancePath path)
    {
        var span = Arrange(tip: 7);
        var (outside, excluded) = Preview(span.Source, span.Origin, span.Gain, span.Coordinate);
        outside.Ignore(Plane.Of(SiteCount, Tip));
        var attempt = path.Preview(outside, excluded);
        Assert.Equal(Plane.Ones(1), attempt.Beyond);
        Assert.True(attempt.Step.Changed.None);
        Assert.True(outside.Changes().None);

        var (inside, requiresRoom) = Preview(span.Source, span.Origin, span.Gain, span.Coordinate);
        var blocked = path.Preview(inside, requiresRoom);
        Assert.Equal(Plane.Ones(1), blocked.Step.Unadmitted);
        Assert.True(blocked.Step.Changed.None);
        Assert.Equal((2, 7, 5), Sites(inside, span.Coordinate));
        Assert.Equal((2, 7, 5), Sites(span.Source, span.Coordinate));
    }

    private static (Store Source, Field Origin, Field Gain, Field Coordinate, Ladder Ladder) Arrange(int tip = 3)
    {
        var source = new Store(0, SiteCount);
        var origin = Field.Bit(source.AddPlane());
        var gain = new Field(Shape.Unary(2), source.AddPlane(), source.AddPlane());
        var ladder = new Ladder(source, [CoordinateCapacity + 1]);
        var coordinate = ladder.Digits[0];
        origin.Write(source, Datum, 1);
        gain.Write(source, Datum, 2);
        coordinate.Write(source, Joint, 2);
        coordinate.Write(source, Tip, tip);
        coordinate.Write(source, Carried, 5);
        return (source, origin, gain, coordinate, ladder);
    }

    private static (Workspace Candidate, Wiring Scale) Preview(Store source, Field origin, Field gain, Field coordinate)
    {
        var workspace = new Workspace(source).Select(Selected);
        var calibration = Plane.Of(SiteCount, Datum);
        foreach (var plane in origin.Planes.Concat(gain.Planes)) workspace.Fix(plane, calibration);
        var scale = new Wiring(ScaleAboutDatum,
            origin.On(workspace), gain.On(workspace), coordinate.On(workspace), coordinate.On(workspace));
        scale.Add(Datum, Datum, Joint, Tip);
        return (workspace, scale);
    }

    private static (int? Joint, int? Tip, int? Carried) Sites(Surface surface, Field coordinate) =>
        (coordinate.Read(surface, Joint), coordinate.Read(surface, Tip), coordinate.Read(surface, Carried));
}
