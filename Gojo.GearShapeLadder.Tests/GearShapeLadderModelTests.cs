using Gojo.Core;
using Gojo.Structures;
using GojoCAD;

namespace Gojo.GearShapeLadder.Tests;

public sealed class GearShapeLadderModelTests
{
    [Fact]
    public void BuildsSixReadableTimedRefinementRungs()
    {
        var model = new GearShapeLadderModel();

        Assert.True(StructuralLadderSurface.TryReadRungPath(model.Ladder, out var path));
        Assert.Equal(6, path.Length);
        Assert.All(path, rung => Assert.True(StructuralLadderSurface.TryReadRung(
            rung, out _, out _, out var result, out _, out _, out _, out _, out _) &&
            GearShapeLadderSurface.TryReadStage(result, out _)));

        Assert.True(StructuralLadderSurface.TryReadTimeline(model.Ladder, out var timeline));
        Assert.True(TimeStepSequence.TryRead(timeline, out _, out _, out var steps));
        Assert.Equal(5, steps.Length);
        Assert.All(steps, step => Assert.True(StructuralLadderSurface.TryReadTransformStep(
            step, out _, out _, out _, out _, out _, out _, out _)));
    }

    [Fact]
    public void EarlyRungsIntroduceSecondCircleAndThenMechanicalCoupling()
    {
        var model = new GearShapeLadderModel(24, 16);
        var oneCircle = model.ReadStage(GearShapeGeometry.SingleCircleStage);
        var twoCircles = model.ReadStage(GearShapeGeometry.TwoCircleStage);
        var coupled = model.ReadStage(GearShapeGeometry.CoupledCircleStage);

        Assert.True(GearShapeLadderSurface.TryReadSizedCircle(oneCircle.DriverElement, out var first));
        Assert.True(GearShapeLadderSurface.TryReadSizedCircleCalibration(first.Calibration, out var firstCalibration));
        Assert.False(GearShapeLadderSurface.TryReadCalibration(first.Calibration, out _));
        Assert.Equal(24, firstCalibration.SizeStep);
        Assert.Null(oneCircle.Driven);
        Assert.Null(oneCircle.DevelopedProfile);
        Assert.Null(oneCircle.Mesh);

        Assert.NotNull(twoCircles.Driven);
        Assert.True(GearShapeLadderSurface.TryReadSizedCircle(twoCircles.DrivenElement, out var second));
        Assert.True(GearShapeLadderSurface.TryReadSizedCircleCalibration(second.Calibration, out var secondCalibration));
        Assert.Equal(16, secondCalibration.SizeStep);
        Assert.Null(twoCircles.DevelopedProfile);
        Assert.Null(twoCircles.Mesh);

        Assert.True(GearShapeLadderSurface.TryReadGear(coupled.DriverElement, out _));
        Assert.True(GearShapeLadderSurface.TryReadGear(coupled.DrivenElement, out _));
        Assert.False(GearShapeLadderSurface.TryReadSizedCircle(coupled.DriverElement, out _));
        Assert.NotNull(coupled.DevelopedProfile);
        Assert.NotNull(coupled.Mesh);
    }

    [Theory]
    [InlineData(GearShapeGeometry.SingleCircleStage)]
    [InlineData(GearShapeGeometry.TwoCircleStage)]
    [InlineData(GearShapeGeometry.CoupledCircleStage)]
    public void BuildsOnlyTheVisibleLadderPrefixWhenRequested(int maximumStage)
    {
        var model = new GearShapeLadderModel(maximumStage: maximumStage);

        Assert.Equal(maximumStage, model.MaximumStage);
        Assert.Equal(maximumStage + 1, model.Rungs.Count);
        Assert.True(StructuralLadderSurface.TryReadRungPath(model.Ladder, out var path));
        Assert.Equal(maximumStage + 1, path.Length);
        Assert.NotNull(model.ReadStage(maximumStage));

        var rebuilt = model.WithTeeth(25, 17);
        Assert.Equal(maximumStage, rebuilt.MaximumStage);
        Assert.Equal(maximumStage + 1, rebuilt.Rungs.Count);
    }

    [Fact]
    public void RefinementAddsVisibleVectorDetailToGearAndDevelopedProfile()
    {
        var model = new GearShapeLadderModel(24, 16);
        var stages = Enumerable.Range(0, GearShapeGeometry.StageCount)
            .Select(model.ReadStage)
            .ToArray();

        var driverSegments = stages.Select(stage => GearShapeGeometry.SegmentCount(stage.Driver.Contour)).ToArray();
        var developedSegments = stages
            .Skip(GearShapeGeometry.CoupledCircleStage)
            .Select(stage => GearShapeGeometry.SegmentCount(stage.DevelopedProfile!.Profile))
            .ToArray();

        Assert.Equal(0, driverSegments[0]);
        Assert.Equal(0, driverSegments[1]);
        Assert.Equal(0, driverSegments[2]);
        Assert.True(driverSegments[3] < driverSegments[4]);
        Assert.True(driverSegments[4] < driverSegments[5]);
        Assert.True(driverSegments[5] >= model.DriverTeeth * 40);
        Assert.True(developedSegments[0] < developedSegments[1]);
        Assert.True(developedSegments[1] < developedSegments[2]);
        Assert.True(developedSegments[2] < developedSegments[3]);
    }

    [Fact]
    public void RefinementPreservesPerimeterIdentityAndSharedPitchEvidence()
    {
        var model = new GearShapeLadderModel();
        var first = model.ReadStage(GearShapeGeometry.SingleCircleStage);
        var coupled = model.ReadStage(GearShapeGeometry.CoupledCircleStage);
        var last = model.ReadStage(GearShapeGeometry.InvoluteToothStage);

        Assert.Same(first.Driver.Perimeter, last.Driver.Perimeter);
        Assert.Same(coupled.Driven!.Perimeter, last.Driven!.Perimeter);
        Assert.Same(first.Driver.PitchCircle, last.Driver.PitchCircle);
        Assert.Same(coupled.DevelopedProfile!.Line, last.DevelopedProfile!.Line);
        Assert.NotSame(first.Driver.Contour, last.Driver.Contour);
        Assert.NotSame(coupled.DevelopedProfile.Profile, last.DevelopedProfile.Profile);
    }

    [Fact]
    public void MotionProjectsEqualOpposedPerimeterTicksWithoutMutatingDrivenCarrier()
    {
        var model = new GearShapeLadderModel(24, 16);

        model.Advance(24);
        var motion = model.ReadMotion();

        Assert.Equal(24, motion.SourceTicks);
        Assert.Equal(24, motion.DriverTicks);
        Assert.Equal(-24, motion.DrivenTicks);
        Assert.Equal(0UL, model.DriverPerimeter.Value);
        Assert.Equal(0UL, model.DrivenPerimeter.Value);
        Assert.Equal(1.5d, (Math.Abs(motion.DrivenTicks) / (16d * 8d)) / (motion.DriverTicks / (24d * 8d)), 10);
    }

    [Fact]
    public void ChangingToothCountsRebuildsStructureAndPreservesIncomingMotion()
    {
        var model = new GearShapeLadderModel();
        model.Advance(19);

        var rebuilt = model.WithTeeth(30, 12);

        Assert.Equal(30, rebuilt.DriverTeeth);
        Assert.Equal(12, rebuilt.DrivenTeeth);
        Assert.Equal(19, rebuilt.ReadMotion().SourceTicks);
        Assert.NotSame(model.Ladder, rebuilt.Ladder);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(16)]
    [InlineData(19)]
    [InlineData(25)]
    public void FinalDevelopedProfileKeepsAnExplicitBottomLandAtLowToothCounts(int teeth)
    {
        var profile = new GearShapeLadderModel(teeth, 24)
            .ReadStage(GearShapeGeometry.InvoluteToothStage)
            .DevelopedProfile!.Profile;
        var points = ReadPolylinePoints(profile);
        var baseline = checked((long)GearShapeGeometry.DevelopedBaselineY);
        var longestBottomLand = 0;
        var currentBottomLand = 0;

        foreach (var point in points)
        {
            if (point.Y == baseline)
            {
                currentBottomLand++;
                longestBottomLand = Math.Max(longestBottomLand, currentBottomLand);
            }
            else
            {
                currentBottomLand = 0;
            }
        }

        Assert.True(longestBottomLand >= 3,
            $"Expected a sampled bottom land rather than a single root vertex for {teeth} teeth.");
    }

    [Theory]
    [InlineData(24, 16)]
    [InlineData(25, 27)]
    [InlineData(24, 27)]
    [InlineData(25, 16)]
    [InlineData(8, 9)]
    [InlineData(39, 40)]
    public void StructuralMeshPhaseKeepsDrivenSpacesOppositeDriverTeeth(
        int driverTeeth,
        int drivenTeeth)
    {
        var stage = new GearShapeLadderModel(driverTeeth, drivenTeeth)
            .ReadStage(GearShapeGeometry.InvoluteToothStage);
        Assert.True(GearShapeLadderSurface.TryReadCalibration(stage.Driver.Calibration, out var driver));
        Assert.True(GearShapeLadderSurface.TryReadCalibration(stage.Driven!.Calibration, out var driven));
        Assert.Equal(0, driver.MeshPhaseTicks);
        Assert.Equal(drivenTeeth % 2 == 0 ? GearShapeGeometry.TicksPerTooth / 2 : 0, driven.MeshPhaseTicks);
        Assert.Equal(GearShapeGeometry.CoordinateResolution, driver.CoordinateResolution);
        Assert.Equal(GearShapeGeometry.CoordinateResolution, driven.CoordinateResolution);

        for (var motionTick = 0; motionTick < GearShapeGeometry.TicksPerTooth; motionTick++)
        {
            var driverRotation = motionTick * Math.PI * 2d /
                (driver.ToothCount * driver.TicksPerTooth);
            var drivenRotation = -motionTick * Math.PI * 2d /
                (driven.ToothCount * driven.TicksPerTooth);
            var drivenPhase = driven.MeshPhaseTicks * Math.PI * 2d /
                (driven.ToothCount * driven.TicksPerTooth);
            var driverContactTicks = NormalizeToothTicks(
                -driverRotation * driver.ToothCount / (Math.PI * 2d) * driver.TicksPerTooth,
                driver.TicksPerTooth);
            var drivenContactTicks = NormalizeToothTicks(
                (Math.PI - drivenRotation - drivenPhase) * driven.ToothCount /
                    (Math.PI * 2d) * driven.TicksPerTooth,
                driven.TicksPerTooth);

            Assert.Equal(
                driver.TicksPerTooth / 2d,
                NormalizeToothTicks(driverContactTicks + drivenContactTicks, driver.TicksPerTooth),
                9);
        }
    }

    private static IReadOnlyList<TestPoint> ReadPolylinePoints(Relation relation)
    {
        Assert.IsType<Carrier>(relation.Right);
        Assert.True(relation.TryReadOrderedElementsOn((Carrier)relation.Right, out var elements));
        var points = new List<TestPoint>();
        foreach (var segmentRelation in elements.OfType<Relation>())
        {
            var segment = GraphicReadGraph.From(segmentRelation)
                .ReadsOf<SharedFrameSegmentRead>()
                .First(read => ReferenceEquals(read.Source, segmentRelation));
            Append(points, segment.Start);
            Append(points, segment.End);
        }

        return points;
    }

    private static void Append(List<TestPoint> points, SharedFramePointRead point)
    {
        var next = new TestPoint(point.Coordinates[0].Ticks, point.Coordinates[1].Ticks);
        if (points.Count == 0 || points[^1] != next)
        {
            points.Add(next);
        }
    }

    private static double NormalizeToothTicks(double ticks, int ticksPerTooth)
    {
        var normalized = ticks % ticksPerTooth;
        return normalized < 0 ? normalized + ticksPerTooth : normalized;
    }

    private readonly record struct TestPoint(long X, long Y);
}
