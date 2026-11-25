using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class OperationScopeTests
{
    private const int Datum = 0;
    private const int Joint = 1;
    private const int Tip = 2;
    private const int Carried = 3;
    private const int SiteCount = 4;
    private const int CoordinateCapacity = 10;

    private static readonly Plane Target = Plane.Of(SiteCount, Joint, Tip);

    // The two positions are one change. An unrepresentable result leaves the
    // whole candidate row unadmitted so there is no half-scaled span to commit.
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

    private sealed record SpanSites(Store Source, Field Origin, Field Gain, Field Coordinate)
    {
        public Workspace Preview()
        {
            var workspace = new Workspace(Source).Select(Target);
            // Keep the read-only calibration in this candidate's commit basis.
            var datum = Plane.Of(SiteCount, Datum);
            foreach (var plane in Origin.Planes.Concat(Gain.Planes))
                workspace.Fix(plane, datum);
            return workspace;
        }

        public WorkspaceStep Apply(Workspace workspace)
        {
            var scale = new Wiring(ScaleAboutDatum,
                Origin.On(workspace), Gain.On(workspace), Coordinate.On(workspace), Coordinate.On(workspace));
            scale.Add(Datum, Datum, Joint, Tip);
            return workspace.Apply(scale);
        }

        public (int? Joint, int? Tip, int? Carried) Sites(Surface surface) =>
            (Coordinate.Read(surface, Joint), Coordinate.Read(surface, Tip), Coordinate.Read(surface, Carried));

        public (int? First, int? Second) Lengths(Surface surface)
        {
            var (result, length) = Make.Counts(2, CoordinateCapacity);
            var first = new Wiring(Counts.Shortfall(1, CoordinateCapacity),
                Origin.On(surface), Coordinate.On(surface), length.On(result));
            first.Add(Datum, Joint, 0);

            var second = new Wiring(Counts.Shortfall(CoordinateCapacity, CoordinateCapacity),
                Coordinate.On(surface), Coordinate.On(surface), length.On(result));
            second.Add(Joint, Tip, 1);
            first.Apply();
            second.Apply();
            return (length.Read(result, 0), length.Read(result, 1));
        }
    }

    private static SpanSites Arrange(int tip = 3)
    {
        var source = new Store(0, SiteCount);
        var origin = Field.Bit(source.AddPlane());
        var gain = new Field(Shape.Unary(2), source.AddPlane(), source.AddPlane());
        var coordinate = new Field(Shape.Unary(CoordinateCapacity),
            [.. Enumerable.Range(0, CoordinateCapacity).Select(_ => source.AddPlane())]);
        origin.Write(source, Datum, 1);
        gain.Write(source, Datum, 2);
        coordinate.Write(source, Joint, 2);
        coordinate.Write(source, Tip, tip);
        coordinate.Write(source, Carried, 5);
        return new SpanSites(source, origin, gain, coordinate);
    }

    [Fact]
    public void ScopedGainPreservesASharedJointAndCommitsBothChangesTogether()
    {
        var spans = Arrange();
        var candidate = spans.Preview();
        Assert.Equal((1, 1), spans.Lengths(spans.Source));
        Assert.True(spans.Apply(candidate).Step.Unadmitted.None);
        Assert.Equal((3, 5, 5), spans.Sites(candidate));
        Assert.Equal((2, 2), spans.Lengths(candidate));
        Assert.False(candidate.SamePosition(Tip, candidate, Carried));
        Assert.Equal((2, 3, 5), spans.Sites(spans.Source));

        var split = candidate.Commit(Plane.Of(SiteCount, Joint));
        Assert.False(split.Committed);
        Assert.Equal(Plane.Of(SiteCount, Tip), split.Split);
        Assert.Equal((2, 3, 5), spans.Sites(spans.Source));

        var applied = candidate.Commit();
        Assert.True(applied.Committed);
        Assert.Equal(Target, applied.Written);
        Assert.Equal((3, 5, 5), spans.Sites(spans.Source));
        Assert.Equal((2, 2), spans.Lengths(spans.Source));

        var next = spans.Preview();
        spans.Apply(next);
        Assert.Equal((5, 9, 5), spans.Sites(next));
        Assert.True(next.Commit().Committed);
        Assert.Equal((5, 9, 5), spans.Sites(spans.Source));
    }

    [Fact]
    public void ChangingTheBoundGainMakesTheWholePreparedPatchStale()
    {
        var spans = Arrange();
        var candidate = spans.Preview();
        spans.Apply(candidate);
        spans.Gain.Write(spans.Source, Datum, 1);

        var commit = candidate.Commit();
        Assert.False(commit.Committed);
        Assert.Equal(Plane.Of(SiteCount, Datum), commit.Stale);
        Assert.True(commit.Written.None);
        Assert.Equal((2, 3, 5), spans.Sites(spans.Source));
        Assert.Equal((3, 5, 5), spans.Sites(candidate));
    }

    [Fact]
    public void AnExcludedEndpointLeavesTheGroupedOperationOutsideTheScope()
    {
        var spans = Arrange();
        var candidate = spans.Preview().Ignore(Plane.Of(SiteCount, Tip));

        var attempt = spans.Apply(candidate);
        Assert.Equal(Plane.Ones(1), attempt.Beyond);
        Assert.True(attempt.Step.Changed.None);
        Assert.True(candidate.Changes().None);
    }

    [Fact]
    public void AResultOutsideTheCoordinateDomainRefusesBothChanges()
    {
        var spans = Arrange(tip: 7);
        var candidate = spans.Preview();

        var attempt = spans.Apply(candidate);
        Assert.Equal(Plane.Ones(1), attempt.Step.Unadmitted);
        Assert.True(attempt.Step.Changed.None);
        Assert.Equal((2, 7, 5), spans.Sites(candidate));
        Assert.Equal((2, 7, 5), spans.Sites(spans.Source));
    }
}
