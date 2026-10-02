using Gojo.Core;
using Gojo.Structures;

namespace Gojo.GearShapeLadder;

/// <summary>
/// The inspectable Gojo structure behind the demo. Geometry refinement lives
/// in a structural ladder; motion enters a carrier as tension and is observed
/// through two explicit projections.
/// </summary>
public sealed class GearShapeLadderModel
{
    public const int MinimumTeeth = 8;
    public const int MaximumTeeth = 40;

    private readonly Relation[] rungs;
    private readonly Relation motionToDriver;
    private readonly Relation driverToDriven;

    public GearShapeLadderModel(
        int driverTeeth = 24,
        int drivenTeeth = 16,
        ulong initialMotionTicks = 0,
        int maximumStage = GearShapeGeometry.InvoluteToothStage)
    {
        DriverTeeth = Math.Clamp(driverTeeth, MinimumTeeth, MaximumTeeth);
        DrivenTeeth = Math.Clamp(drivenTeeth, MinimumTeeth, MaximumTeeth);
        MaximumStage = Math.Clamp(maximumStage, GearShapeGeometry.SingleCircleStage, GearShapeGeometry.InvoluteToothStage);

        var basis = new GearShapeGeometry.FrameBasis();
        var driverRadius = GearShapeGeometry.PitchRadius(DriverTeeth);
        var drivenRadius = GearShapeGeometry.PitchRadius(DrivenTeeth);
        DriverCenterX = -driverRadius;
        DrivenCenterX = drivenRadius;

        var driverCenter = new Carrier();
        var drivenCenter = new Carrier();
        DriverPerimeter = new Carrier(UnitShape.CircularFrom(Relation.Point(driverCenter, 0)));
        DrivenPerimeter = new Carrier(UnitShape.CircularFrom(Relation.Point(drivenCenter, 0)));
        Motion = new Carrier();

        motionToDriver = new Relation(
            Relation.Segment(Motion, 0, 1),
            Relation.Segment(DriverPerimeter, 0, 1));
        driverToDriven = new Relation(
            Relation.Segment(DriverPerimeter, 0, 1),
            Relation.Segment(DrivenPerimeter, 0, -1));

        var driverPitch = GearShapeGeometry.PitchCircle(
            basis,
            DriverCenterX,
            GearShapeGeometry.GearCenterY,
            driverRadius);
        var driverSizeCalibration = SizedCircleCalibration(DriverTeeth, driverRadius);
        var driverSizedCircle = GearShapeLadderSurface.SizedCircle(
            new Carrier(), DriverPerimeter, driverPitch, driverSizeCalibration);
        var drivenOpen = new Placeholder();
        var developedOpen = new Placeholder();
        var meshOpen = new Placeholder();

        var stages = new Relation[MaximumStage + 1];
        stages[GearShapeGeometry.SingleCircleStage] = GearShapeLadderSurface.Stage(
            new Carrier(), driverSizedCircle, drivenOpen, developedOpen, meshOpen);

        Relation drivenPitch = null!;
        if (MaximumStage >= GearShapeGeometry.TwoCircleStage)
        {
            drivenPitch = GearShapeGeometry.PitchCircle(
                basis,
                DrivenCenterX,
                GearShapeGeometry.GearCenterY,
                drivenRadius);
            var drivenSizeCalibration = SizedCircleCalibration(DrivenTeeth, drivenRadius);
            var drivenSizedCircle = GearShapeLadderSurface.SizedCircle(
                new Carrier(), DrivenPerimeter, drivenPitch, drivenSizeCalibration);
            stages[GearShapeGeometry.TwoCircleStage] = GearShapeLadderSurface.Stage(
                new Carrier(), driverSizedCircle, drivenSizedCircle, developedOpen, meshOpen);
        }

        Relation driverBore = null!;
        Relation drivenBore = null!;
        Relation driverMarker = null!;
        Relation drivenMarker = null!;
        Relation driverCalibration = null!;
        Relation drivenCalibration = null!;
        Relation developedBaseline = null!;
        Relation developedCalibration = null!;
        Carrier developedLine = null!;
        if (MaximumStage >= GearShapeGeometry.CoupledCircleStage)
        {
            driverBore = GearShapeGeometry.BoreCircle(
                basis, DriverCenterX, GearShapeGeometry.GearCenterY, driverRadius);
            drivenBore = GearShapeGeometry.BoreCircle(
                basis, DrivenCenterX, GearShapeGeometry.GearCenterY, drivenRadius);
            driverMarker = GearShapeGeometry.PhaseMarker(
                basis, DriverCenterX, GearShapeGeometry.GearCenterY, driverRadius);
            drivenMarker = GearShapeGeometry.PhaseMarker(
                basis, DrivenCenterX, GearShapeGeometry.GearCenterY, drivenRadius);
            driverCalibration = Calibration(DriverTeeth, driverRadius, meshPhaseTicks: 0);
            drivenCalibration = Calibration(
                DrivenTeeth,
                drivenRadius,
                GearShapeGeometry.DrivenMeshPhaseTicks(DrivenTeeth));
            developedLine = new Carrier();
            developedBaseline = GearShapeGeometry.DevelopedBaseline(basis);
            developedCalibration = Calibration(DriverTeeth, driverRadius, meshPhaseTicks: 0);

            var driverCircle = GearShapeLadderSurface.Gear(
                new Carrier(), DriverPerimeter, driverPitch, driverPitch, driverBore, driverMarker, driverCalibration);
            var drivenCircle = GearShapeLadderSurface.Gear(
                new Carrier(), DrivenPerimeter, drivenPitch, drivenPitch, drivenBore, drivenMarker, drivenCalibration);
            var developedPitchLine = GearShapeLadderSurface.DevelopedProfile(
                new Carrier(),
                developedLine,
                GearShapeGeometry.DevelopedProfile(basis, DriverTeeth, shapeDetail: 0),
                developedBaseline,
                developedCalibration);
            stages[GearShapeGeometry.CoupledCircleStage] = GearShapeLadderSurface.Stage(
                new Carrier(), driverCircle, drivenCircle, developedPitchLine, driverToDriven);
        }

        for (var stage = GearShapeGeometry.TriangleToothStage; stage < stages.Length; stage++)
        {
            var shapeDetail = GearShapeGeometry.ShapeDetailForStage(stage);
            var driverContour = GearShapeGeometry.GearContour(
                basis, DriverTeeth, DriverCenterX, GearShapeGeometry.GearCenterY, shapeDetail, driverPitch);
            var drivenContour = GearShapeGeometry.GearContour(
                basis, DrivenTeeth, DrivenCenterX, GearShapeGeometry.GearCenterY, shapeDetail, drivenPitch);
            var developedContour = GearShapeGeometry.DevelopedProfile(basis, DriverTeeth, shapeDetail);

            var driver = GearShapeLadderSurface.Gear(
                new Carrier(), DriverPerimeter, driverPitch, driverContour, driverBore, driverMarker, driverCalibration);
            var driven = GearShapeLadderSurface.Gear(
                new Carrier(), DrivenPerimeter, drivenPitch, drivenContour, drivenBore, drivenMarker, drivenCalibration);
            var developed = GearShapeLadderSurface.DevelopedProfile(
                new Carrier(), developedLine, developedContour, developedBaseline, developedCalibration);
            stages[stage] = GearShapeLadderSurface.Stage(new Carrier(), driver, driven, developed, driverToDriven);
        }

        rungs = new Relation[stages.Length];
        rungs[0] = StructuralLadderSurface.BaseRung(new Carrier(), stages[0]);
        var applications = new Relation[stages.Length - 1];
        for (var stage = 1; stage < stages.Length; stage++)
        {
            var previous = ReadStage(stages[stage - 1]);
            var next = ReadStage(stages[stage]);
            var (participation, complement, replacement) = RefinementParticipation(previous, next, stage);
            (rungs[stage], applications[stage - 1]) = RefinedRung(
                stages[stage - 1],
                participation,
                complement,
                replacement,
                stages[stage],
                Relation.Point(new Carrier(), stage));
        }

        Ladder = TimelineLadder(rungs, applications);
        if (initialMotionTicks > 0)
        {
            Motion.Add(initialMotionTicks);
        }
    }

    public int DriverTeeth { get; }

    public int DrivenTeeth { get; }

    public int MaximumStage { get; }

    public double DriverCenterX { get; }

    public double DrivenCenterX { get; }

    public Carrier Motion { get; }

    public Carrier DriverPerimeter { get; }

    public Carrier DrivenPerimeter { get; }

    public Relation Ladder { get; }

    public IReadOnlyList<Relation> Rungs => rungs;

    public void Advance(ulong ticks) => Motion.Add(ticks);

    public void ResetMotion() => Motion.Set(0);

    public GearMotionRead ReadMotion()
    {
        var driverTicks = ProjectTicks(motionToDriver, Motion.ValueSlot, DriverPerimeter);
        var drivenTicks = ProjectTicks(driverToDriven, new CarrierLocation(DriverPerimeter, driverTicks), DrivenPerimeter);
        return new GearMotionRead(checked((long)Motion.Value), driverTicks, drivenTicks);
    }

    public GearShapeStageRead ReadStage(int stage)
    {
        var index = Math.Clamp(stage, 0, rungs.Length - 1);
        if (!StructuralLadderSurface.TryReadRung(
                rungs[index], out _, out _, out var result, out _, out _, out _, out _, out _) ||
            !GearShapeLadderSurface.TryReadStage(result, out var read))
        {
            throw new InvalidOperationException($"Gear ladder rung {index} is not structurally readable.");
        }

        return read;
    }

    public GearShapeLadderModel WithTeeth(int driverTeeth, int drivenTeeth) =>
        new(driverTeeth, drivenTeeth, Motion.Value, MaximumStage);

    public GearShapeLadderModel WithMaximumStage(int maximumStage) =>
        new(DriverTeeth, DrivenTeeth, Motion.Value, maximumStage);

    private static long ProjectTicks(Relation projection, CarrierLocation source, Carrier target)
    {
        var read = projection.Project(source, target);
        if (!read.TryGetCarrierLocations(out var projected, out _))
        {
            throw new InvalidOperationException("Gear motion projection did not produce a carrier-location read.");
        }

        return projected.Ticks;
    }

    private static GearShapeStageRead ReadStage(Relation stage)
    {
        if (!GearShapeLadderSurface.TryReadStage(stage, out var read))
        {
            throw new InvalidOperationException("Generated gear stage is not structurally readable.");
        }

        return read;
    }

    private static Relation ShapeParticipation(GearShapeStageRead stage) =>
        Relation.OrderedGroup(
            new Carrier(),
            stage.Driver.Contour,
            (stage.Driven ?? throw new InvalidOperationException("A tooth rung requires a driven gear.")).Contour,
            (stage.DevelopedProfile ?? throw new InvalidOperationException("A tooth rung requires a developed profile.")).Profile);

    private static Relation SharedParticipation(GearShapeStageRead stage) =>
        Relation.OrderedGroup(
            new Carrier(),
            stage.Driver.Perimeter,
            stage.Driver.PitchCircle,
            stage.Driver.BoreCircle ?? throw new InvalidOperationException("A tooth rung requires a driver bore."),
            (stage.Driven ?? throw new InvalidOperationException("A tooth rung requires a driven gear.")).Perimeter,
            stage.Driven.PitchCircle,
            stage.Driven.BoreCircle ?? throw new InvalidOperationException("A tooth rung requires a driven bore."),
            (stage.DevelopedProfile ?? throw new InvalidOperationException("A tooth rung requires a developed profile.")).Line,
            stage.DevelopedProfile.Baseline,
            stage.Mesh ?? throw new InvalidOperationException("A tooth rung requires a mesh relation."));

    private static (IElement Participation, IElement Complement, IElement Replacement) RefinementParticipation(
        GearShapeStageRead previous,
        GearShapeStageRead next,
        int introducedStage)
    {
        if (introducedStage == GearShapeGeometry.TwoCircleStage)
        {
            return (
                previous.DrivenElement,
                Relation.OrderedGroup(
                    new Carrier(),
                    previous.DriverElement,
                    previous.DevelopedProfileElement,
                    previous.MeshElement),
                next.DrivenElement);
        }

        if (introducedStage == GearShapeGeometry.CoupledCircleStage)
        {
            return (
                Relation.OrderedGroup(
                    new Carrier(),
                    previous.DriverElement,
                    previous.DrivenElement,
                    previous.DevelopedProfileElement,
                    previous.MeshElement),
                Relation.OrderedGroup(
                    new Carrier(),
                    previous.Driver.Perimeter,
                    previous.Driver.PitchCircle,
                    (previous.Driven ?? throw new InvalidOperationException("The two-circle rung requires a second circle.")).Perimeter,
                    previous.Driven.PitchCircle),
                Relation.OrderedGroup(
                    new Carrier(),
                    next.DriverElement,
                    next.DrivenElement,
                    next.DevelopedProfileElement,
                    next.MeshElement));
        }

        return (
            ShapeParticipation(previous),
            SharedParticipation(previous),
            ShapeParticipation(next));
    }

    private static Relation Calibration(int teeth, double pitchRadius, int meshPhaseTicks)
    {
        var toothCount = new Carrier();
        var ticksPerTooth = new Carrier();
        var radius = new Carrier();
        var phase = new Carrier();
        var coordinateResolution = new Carrier();
        return Relation.OrderedGroup(
            new Carrier(),
            Relation.Point(toothCount, teeth),
            Relation.Point(ticksPerTooth, GearShapeGeometry.TicksPerTooth),
            Relation.Point(radius, checked((long)Math.Round(pitchRadius))),
            Relation.Point(phase, meshPhaseTicks),
            Relation.Point(coordinateResolution, GearShapeGeometry.CoordinateResolution));
    }

    private static Relation SizedCircleCalibration(int sizeStep, double radius)
    {
        var size = new Carrier();
        var radiusCarrier = new Carrier();
        var coordinateResolution = new Carrier();
        return Relation.OrderedGroup(
            new Carrier(),
            Relation.Point(size, sizeStep),
            Relation.Point(radiusCarrier, checked((long)Math.Round(radius))),
            Relation.Point(coordinateResolution, GearShapeGeometry.CoordinateResolution));
    }

    private static (Relation Rung, Relation Application) RefinedRung(
        IElement source,
        IElement participation,
        IElement complement,
        IElement replacement,
        IElement result,
        IElement parameterEvidence)
    {
        var correspondence = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(participation, replacement));
        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            participation,
            participation,
            complement,
            replacement,
            correspondence,
            UnknownPoint(),
            UnknownPoint());

        var selectedPort = new Placeholder();
        var carryPort = new Placeholder();
        var replacementPort = new Placeholder();
        var resultPort = new Placeholder();
        var operationInputs = TransformSurface.Ports(new Carrier(), selectedPort, carryPort, replacementPort);
        var operation = TransformSurface.Fold(operationInputs, resultPort);
        var transform = TransformSurface.Create(
            new Carrier(),
            operationInputs,
            operation,
            TransformSurface.Ports(new Carrier(), resultPort),
            UnknownPoint(),
            TransformSurface.Frame(
                new Carrier(),
                TransformSurface.Remove(selectedPort),
                TransformSurface.Carry(carryPort, carryPort),
                TransformSurface.Map(selectedPort, replacementPort),
                operation,
                parameterEvidence));
        var bindings = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(selectedPort, participation),
            FocusSurface.Bind(carryPort, complement),
            FocusSurface.Bind(replacementPort, replacement),
            FocusSurface.Bind(resultPort, result),
            FocusSurface.Bind(participation, replacement));

        if (!TransformSurface.TryCreateApplication(
                new Carrier(), transform, focus, bindings, result, UnknownPoint(), parameterEvidence, out var application) ||
            !StructuralLadderSurface.TryCreateRungFromTransformApplication(new Carrier(), application, out var rung))
        {
            throw new InvalidOperationException("Generated gear refinement did not create a ladder rung.");
        }

        return (rung, application);
    }

    private static Relation TimelineLadder(IReadOnlyList<Relation> ladderRungs, IReadOnlyList<Relation> applications)
    {
        var clock = TimeStepSequence.CreateClock(new Carrier());
        var steps = new Relation[applications.Count];
        for (var index = 0; index < steps.Length; index++)
        {
            steps[index] = StructuralLadderSurface.TransformStep(
                new Carrier(), clock, index, index + 1, ladderRungs[index], applications[index], ladderRungs[index + 1], UnknownPoint());
        }

        return StructuralLadderSurface.Create(
            new Carrier(),
            ladderRungs[0],
            StructuralLadderSurface.RungFrame(new Carrier(), [.. ladderRungs]),
            StructuralLadderSurface.Timeline(new Carrier(), clock, ladderRungs[0], steps),
            UnknownPoint(),
            UnknownPoint());
    }

    private static Relation UnknownPoint() => Relation.Point(Carrier.Unknown, 0);
}

public readonly record struct GearMotionRead(long SourceTicks, long DriverTicks, long DrivenTicks);
