using Gojo.Core;

namespace Gojo.GearShapeLadder;

/// <summary>
/// Exact local composition reads for the demo. These helpers package ordinary
/// Gojo relations; the gear, stage, and developed profile remain traversable
/// without depending on the read records.
/// </summary>
public static class GearShapeLadderSurface
{
    public static Relation SizedCircle(
        Carrier root,
        Carrier perimeter,
        Relation circle,
        Relation calibration) =>
        Relation.OrderedGroup(root, perimeter, circle, calibration);

    public static bool TryReadSizedCircle(IElement element, out SizedCircleRead read)
    {
        if (TryReadElements(element, 3, out var elements) &&
            elements[0] is Carrier perimeter &&
            elements[1] is Relation circle &&
            elements[2] is Relation calibration)
        {
            read = new SizedCircleRead(perimeter, circle, calibration);
            return true;
        }

        read = default!;
        return false;
    }

    public static Relation Gear(
        Carrier root,
        Carrier perimeter,
        Relation pitchCircle,
        Relation contour,
        Relation boreCircle,
        Relation phaseMarker,
        Relation calibration) =>
        Relation.OrderedGroup(
            root,
            perimeter,
            pitchCircle,
            contour,
            boreCircle,
            phaseMarker,
            calibration);

    public static bool TryReadGear(IElement element, out GearShapeRead read)
    {
        if (TryReadElements(element, 6, out var elements) &&
            elements[0] is Carrier perimeter &&
            elements[1] is Relation pitchCircle &&
            elements[2] is Relation contour &&
            elements[3] is Relation boreCircle &&
            elements[4] is Relation phaseMarker &&
            elements[5] is Relation calibration)
        {
            read = new GearShapeRead(
                perimeter,
                pitchCircle,
                contour,
                boreCircle,
                phaseMarker,
                calibration);
            return true;
        }

        read = default!;
        return false;
    }

    public static Relation DevelopedProfile(
        Carrier root,
        Carrier line,
        Relation profile,
        Relation baseline,
        Relation calibration) =>
        Relation.OrderedGroup(root, line, profile, baseline, calibration);

    public static bool TryReadDevelopedProfile(IElement element, out DevelopedProfileRead read)
    {
        if (TryReadElements(element, 4, out var elements) &&
            elements[0] is Carrier line &&
            elements[1] is Relation profile &&
            elements[2] is Relation baseline &&
            elements[3] is Relation calibration)
        {
            read = new DevelopedProfileRead(line, profile, baseline, calibration);
            return true;
        }

        read = default!;
        return false;
    }

    public static Relation Stage(
        Carrier root,
        IElement driver,
        IElement driven,
        IElement developedProfile,
        IElement mesh) =>
        Relation.OrderedGroup(root, driver, driven, developedProfile, mesh);

    public static bool TryReadStage(IElement element, out GearShapeStageRead read)
    {
        if (TryReadElements(element, 4, out var elements) &&
            TryReadCircularShape(elements[0], out var driverRead) &&
            TryReadOptionalCircularShape(elements[1], out var drivenRead) &&
            TryReadOptionalDevelopedProfile(elements[2], out var developedRead) &&
            TryReadOptionalRelation(elements[3], out var meshRead))
        {
            read = new GearShapeStageRead(
                elements[0],
                elements[1],
                elements[2],
                elements[3],
                driverRead,
                drivenRead,
                developedRead,
                meshRead);
            return true;
        }

        read = default!;
        return false;
    }

    private static bool TryReadCircularShape(IElement element, out CircularShapeRead read)
    {
        if (TryReadGear(element, out var gear))
        {
            read = new CircularShapeRead(
                gear.Perimeter,
                gear.PitchCircle,
                gear.Contour,
                gear.BoreCircle,
                gear.PhaseMarker,
                gear.Calibration);
            return true;
        }

        if (TryReadSizedCircle(element, out var circle))
        {
            read = new CircularShapeRead(
                circle.Perimeter,
                circle.Circle,
                circle.Circle,
                null,
                null,
                circle.Calibration);
            return true;
        }

        read = default!;
        return false;
    }

    private static bool TryReadOptionalCircularShape(IElement element, out CircularShapeRead? read)
    {
        if (element is Placeholder)
        {
            read = null;
            return true;
        }

        if (TryReadCircularShape(element, out var gear))
        {
            read = gear;
            return true;
        }

        read = null;
        return false;
    }

    private static bool TryReadOptionalDevelopedProfile(
        IElement element,
        out DevelopedProfileRead? read)
    {
        if (element is Placeholder)
        {
            read = null;
            return true;
        }

        if (TryReadDevelopedProfile(element, out var developed))
        {
            read = developed;
            return true;
        }

        read = null;
        return false;
    }

    private static bool TryReadOptionalRelation(IElement element, out Relation? read)
    {
        if (element is Placeholder)
        {
            read = null;
            return true;
        }

        read = element as Relation;
        return read is not null;
    }

    public static bool TryReadCalibration(IElement element, out GearCalibrationRead read)
    {
        if (TryReadElements(element, 5, out var elements) &&
            TryReadPointTicks(elements[0], out var toothCount) &&
            TryReadPointTicks(elements[1], out var ticksPerTooth) &&
            TryReadPointTicks(elements[2], out var pitchRadius) &&
            TryReadPointTicks(elements[3], out var meshPhaseTicks) &&
            TryReadPointTicks(elements[4], out var coordinateResolution))
        {
            read = new GearCalibrationRead(
                checked((int)toothCount),
                checked((int)ticksPerTooth),
                pitchRadius,
                checked((int)meshPhaseTicks),
                checked((int)coordinateResolution));
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadSizedCircleCalibration(IElement element, out SizedCircleCalibrationRead read)
    {
        if (TryReadElements(element, 3, out var elements) &&
            TryReadPointTicks(elements[0], out var sizeStep) &&
            TryReadPointTicks(elements[1], out var radius) &&
            TryReadPointTicks(elements[2], out var coordinateResolution))
        {
            read = new SizedCircleCalibrationRead(
                checked((int)sizeStep),
                radius,
                checked((int)coordinateResolution));
            return true;
        }

        read = default;
        return false;
    }

    private static bool TryReadElements(IElement element, int expectedCount, out IElement[] elements)
    {
        if (element is Relation relation &&
            relation.Right is Carrier holder &&
            relation.TryReadOrderedElementsOn(holder, out elements) &&
            elements.Length == expectedCount)
        {
            return true;
        }

        elements = [];
        return false;
    }

    private static bool TryReadPointTicks(IElement element, out long ticks)
    {
        if (element is Relation relation &&
            relation.Left is CarrierLocation location &&
            relation.Right is Carrier carrier &&
            location.Carrier.SameIdentityAs(carrier))
        {
            ticks = location.Ticks;
            return true;
        }

        ticks = 0;
        return false;
    }
}

public readonly record struct GearCalibrationRead(
    int ToothCount,
    int TicksPerTooth,
    long PitchRadius,
    int MeshPhaseTicks,
    int CoordinateResolution);

public readonly record struct SizedCircleCalibrationRead(
    int SizeStep,
    long Radius,
    int CoordinateResolution);

public sealed record SizedCircleRead(
    Carrier Perimeter,
    Relation Circle,
    Relation Calibration);

public sealed record GearShapeRead(
    Carrier Perimeter,
    Relation PitchCircle,
    Relation Contour,
    Relation BoreCircle,
    Relation PhaseMarker,
    Relation Calibration);

public sealed record CircularShapeRead(
    Carrier Perimeter,
    Relation PitchCircle,
    Relation Contour,
    Relation? BoreCircle,
    Relation? PhaseMarker,
    Relation Calibration);

public sealed record DevelopedProfileRead(
    Carrier Line,
    Relation Profile,
    Relation Baseline,
    Relation Calibration);

public sealed record GearShapeStageRead(
    IElement DriverElement,
    IElement DrivenElement,
    IElement DevelopedProfileElement,
    IElement MeshElement,
    CircularShapeRead Driver,
    CircularShapeRead? Driven,
    DevelopedProfileRead? DevelopedProfile,
    Relation? Mesh);
