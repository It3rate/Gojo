using Gojo.Core;
using Gojo.Structures;
using SkiaSharp;

namespace GojoCAD.SketchViewer;

internal static class SketchSamples
{
    public static IReadOnlyList<SketchScene> CreateInitialScenes()
    {
        var seed = Random.Shared.Next();
        return
        [
            CreateComplexHouseSketch(seed)
        ];
    }

    private static SketchScene CreateComplexHouseSketch(int seed)
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();

        var lowerLeft = Point(frame, x, y, -52, -30);
        var lowerRight = Point(frame, x, y, 52, -30);
        var upperRight = Point(frame, x, y, 52, 8);
        var roofApex = Point(frame, x, y, 0, 40);
        var roofAntennaJoint = Point(frame, x, y, -24, 25);
        var upperLeft = Point(frame, x, y, -52, 8);

        var doorLeftBottom = Point(frame, x, y, -14, -30);
        var doorLeftTop = Point(frame, x, y, -14, -8);
        var doorLeftControl = Point(frame, x, y, -14, 8);
        var doorRightControl = Point(frame, x, y, 14, 8);
        var doorRightTop = Point(frame, x, y, 14, -8);
        var doorRightBottom = Point(frame, x, y, 14, -30);

        var windowLowerLeft = Point(frame, x, y, 22, -14);
        var windowLowerRight = Point(frame, x, y, 42, -14);
        var windowUpperRight = Point(frame, x, y, 42, 6);
        var windowUpperLeft = Point(frame, x, y, 22, 6);
        var windowMidLeft = Point(frame, x, y, 22, -4);
        var windowMidRight = Point(frame, x, y, 42, -4);
        var windowMidTop = Point(frame, x, y, 32, 6);
        var windowMidBottom = Point(frame, x, y, 32, -14);
        var windowCenter = Point(frame, x, y, 32, -4);

        var windowRoundTopLeft = Point(frame, x, y, 26, 6);
        var windowRoundTopRight = Point(frame, x, y, 38, 6);
        var windowRoundRightTop = Point(frame, x, y, 42, 2);
        var windowRoundRightBottom = Point(frame, x, y, 42, -10);
        var windowRoundBottomRight = Point(frame, x, y, 38, -14);
        var windowRoundBottomLeft = Point(frame, x, y, 26, -14);
        var windowRoundLeftBottom = Point(frame, x, y, 22, -10);
        var windowRoundLeftTop = Point(frame, x, y, 22, 2);

        var windowTopRightControlA = Point(frame, x, y, 40, 6);
        var windowTopRightControlB = Point(frame, x, y, 42, 4);
        var windowBottomRightControlA = Point(frame, x, y, 42, -12);
        var windowBottomRightControlB = Point(frame, x, y, 40, -14);
        var windowBottomLeftControlA = Point(frame, x, y, 24, -14);
        var windowBottomLeftControlB = Point(frame, x, y, 22, -12);
        var windowTopLeftControlA = Point(frame, x, y, 22, 4);
        var windowTopLeftControlB = Point(frame, x, y, 24, 6);

        var windowRectangle = Chain(
            GraphicStructure.FrameSegment(windowLowerLeft, windowLowerRight),
            GraphicStructure.FrameSegment(windowLowerRight, windowUpperRight),
            GraphicStructure.FrameSegment(windowUpperRight, windowUpperLeft),
            GraphicStructure.FrameSegment(windowUpperLeft, windowLowerLeft));
        var windowCross = Chain(
            GraphicStructure.FrameSegment(windowMidLeft, windowCenter),
            GraphicStructure.FrameSegment(windowCenter, windowMidRight),
            GraphicStructure.FrameSegment(windowMidTop, windowCenter),
            GraphicStructure.FrameSegment(windowCenter, windowMidBottom));
        var roundedWindow = Chain(
            GraphicStructure.FrameSegment(windowRoundTopLeft, windowRoundTopRight),
            GraphicStructure.FrameCubicCurve(windowRoundTopRight, windowTopRightControlA, windowTopRightControlB, windowRoundRightTop),
            GraphicStructure.FrameSegment(windowRoundRightTop, windowRoundRightBottom),
            GraphicStructure.FrameCubicCurve(windowRoundRightBottom, windowBottomRightControlA, windowBottomRightControlB, windowRoundBottomRight),
            GraphicStructure.FrameSegment(windowRoundBottomRight, windowRoundBottomLeft),
            GraphicStructure.FrameCubicCurve(windowRoundBottomLeft, windowBottomLeftControlA, windowBottomLeftControlB, windowRoundLeftBottom),
            GraphicStructure.FrameSegment(windowRoundLeftBottom, windowRoundLeftTop),
            GraphicStructure.FrameCubicCurve(windowRoundLeftTop, windowTopLeftControlA, windowTopLeftControlB, windowRoundTopLeft));
        var roundedWindowPlus = Chain(roundedWindow, windowCross);

        var antennaTip = Point(frame, x, y, -29, 43);
        var antennaLeft = Point(frame, x, y, -38, 48);
        var antennaRight = Point(frame, x, y, -18, 46);

        var houseBoxTop = GraphicStructure.FrameSegment(upperRight, upperLeft);
        var houseBox = Chain(
            GraphicStructure.FrameSegment(lowerLeft, lowerRight),
            GraphicStructure.FrameSegment(lowerRight, upperRight),
            houseBoxTop,
            GraphicStructure.FrameSegment(upperLeft, lowerLeft));
        var houseRoof = Chain(
            GraphicStructure.FrameSegment(lowerLeft, lowerRight),
            GraphicStructure.FrameSegment(lowerRight, upperRight),
            GraphicStructure.FrameSegment(upperRight, roofApex),
            GraphicStructure.FrameSegment(roofApex, roofAntennaJoint),
            GraphicStructure.FrameSegment(roofAntennaJoint, upperLeft),
            GraphicStructure.FrameSegment(upperLeft, lowerLeft));
        var doorRectangle = Chain(
            GraphicStructure.FrameSegment(doorLeftBottom, doorRightBottom),
            GraphicStructure.FrameSegment(doorRightBottom, doorRightTop),
            GraphicStructure.FrameSegment(doorRightTop, doorLeftTop),
            GraphicStructure.FrameSegment(doorLeftTop, doorLeftBottom));
        var archedDoor = Chain(
            GraphicStructure.FrameSegment(doorLeftBottom, doorLeftTop),
            GraphicStructure.FrameCubicCurve(doorLeftTop, doorLeftControl, doorRightControl, doorRightTop),
            GraphicStructure.FrameSegment(doorRightTop, doorRightBottom),
            GraphicStructure.FrameSegment(doorRightBottom, doorLeftBottom));
        var antennaStem = GraphicStructure.FrameSegment(roofAntennaJoint, antennaTip);
        var refinedAntenna = Chain(
            antennaStem,
            GraphicStructure.FrameSegment(antennaTip, antennaLeft),
            GraphicStructure.FrameSegment(antennaTip, antennaRight));

        const long cloudAnchorX = -120;
        const long cloudAnchorY = 60;
        Relation CloudPoint(long xOffset, long yOffset) =>
            Point(frame, x, y, cloudAnchorX + xOffset, cloudAnchorY + yOffset);

        var cloudLeftCusp = CloudPoint(0, 0);
        var cloudLeftPeak = CloudPoint(10, 12);
        var cloudLeftValley = CloudPoint(19, 5);
        var cloudMiddlePeak = CloudPoint(30, 16);
        var cloudMiddleValley = CloudPoint(40, 6);
        var cloudRightPeak = CloudPoint(49, 11);
        var cloudRightCusp = CloudPoint(54, -1);
        var cloudLowerRight = CloudPoint(42, -9);
        var cloudLowerLeft = CloudPoint(16, -8);

        var cloudLeftCuspOut = CloudPoint(0, 7);
        var cloudLeftPeakIn = CloudPoint(7, 12);
        var cloudLeftPeakOut = CloudPoint(13, 12);
        var cloudLeftValleyIn = CloudPoint(18, 9);

        var cloudLeftValleyOut = CloudPoint(21, 12);
        var cloudMiddlePeakIn = CloudPoint(26, 16);
        var cloudMiddlePeakOut = CloudPoint(34, 16);
        var cloudMiddleValleyIn = CloudPoint(38, 11);
        var cloudMiddleValleyOut = CloudPoint(43, 11);
        var cloudRightPeakIn = CloudPoint(45, 11);
        var cloudRightPeakOut = CloudPoint(52, 11);
        var cloudRightCuspIn = CloudPoint(54, 5);

        var cloudRightCuspDown = CloudPoint(54, -6);
        var cloudLowerRightIn = CloudPoint(48, -9);
        var cloudLowerRightOut = CloudPoint(36, -9);
        var cloudLowerLeftIn = CloudPoint(22, -8);
        var cloudLowerLeftOut = CloudPoint(6, -8);
        var cloudLeftCuspReturn = CloudPoint(0, -7);
        var cloudOvalTop = CloudPoint(27, 13);
        var cloudOvalRight = CloudPoint(54, -1);
        var cloudOvalBottom = CloudPoint(27, -10);
        var cloudOvalLeftToTopA = CloudPoint(0, 7);
        var cloudOvalLeftToTopB = CloudPoint(12, 13);
        var cloudOvalTopToRightA = CloudPoint(42, 13);
        var cloudOvalTopToRightB = CloudPoint(54, 7);
        var cloudOvalRightToBottomA = CloudPoint(54, -7);
        var cloudOvalRightToBottomB = CloudPoint(42, -10);
        var cloudOvalBottomToLeftA = CloudPoint(12, -10);
        var cloudOvalBottomToLeftB = CloudPoint(0, -7);
        var cloudOval = Chain(
            GraphicStructure.FrameCubicCurve(cloudLeftCusp, cloudOvalLeftToTopA, cloudOvalLeftToTopB, cloudOvalTop),
            GraphicStructure.FrameCubicCurve(cloudOvalTop, cloudOvalTopToRightA, cloudOvalTopToRightB, cloudOvalRight),
            GraphicStructure.FrameCubicCurve(cloudOvalRight, cloudOvalRightToBottomA, cloudOvalRightToBottomB, cloudOvalBottom),
            GraphicStructure.FrameCubicCurve(cloudOvalBottom, cloudOvalBottomToLeftA, cloudOvalBottomToLeftB, cloudLeftCusp));
        var cloudOutline = Chain(
            // Puffy cloud: peaks and lower joins are tangential; valleys and tips are cusp-like direction changes.
            GraphicStructure.FrameCubicCurve(cloudLeftCusp, cloudLeftCuspOut, cloudLeftPeakIn, cloudLeftPeak),
            GraphicStructure.FrameCubicCurve(cloudLeftPeak, cloudLeftPeakOut, cloudLeftValleyIn, cloudLeftValley),
            GraphicStructure.FrameCubicCurve(cloudLeftValley, cloudLeftValleyOut, cloudMiddlePeakIn, cloudMiddlePeak),
            GraphicStructure.FrameCubicCurve(cloudMiddlePeak, cloudMiddlePeakOut, cloudMiddleValleyIn, cloudMiddleValley),
            GraphicStructure.FrameCubicCurve(cloudMiddleValley, cloudMiddleValleyOut, cloudRightPeakIn, cloudRightPeak),
            GraphicStructure.FrameCubicCurve(cloudRightPeak, cloudRightPeakOut, cloudRightCuspIn, cloudRightCusp),
            GraphicStructure.FrameCubicCurve(cloudRightCusp, cloudRightCuspDown, cloudLowerRightIn, cloudLowerRight),
            GraphicStructure.FrameCubicCurve(cloudLowerRight, cloudLowerRightOut, cloudLowerLeftIn, cloudLowerLeft),
            GraphicStructure.FrameCubicCurve(cloudLowerLeft, cloudLowerLeftOut, cloudLeftCuspReturn, cloudLeftCusp));

        var roughHouseAndCloud = Chain(houseBox, cloudOval);
        var peakedHouseAndOval = Chain(houseRoof, cloudOval);
        var rectFeatures = Chain(houseRoof, cloudOval, doorRectangle, windowRectangle);
        var refinedFeatures = Chain(houseRoof, cloudOval, archedDoor, roundedWindowPlus);
        var refinedCloud = Chain(houseRoof, cloudOutline, archedDoor, roundedWindowPlus);
        var antennaLine = Chain(houseRoof, cloudOutline, archedDoor, roundedWindowPlus, antennaStem);
        var finalSketch = Chain(houseRoof, cloudOutline, archedDoor, roundedWindowPlus, refinedAntenna);

        var baseRung = StructuralLadderSurface.BaseRung(new Carrier(), roughHouseAndCloud);
        var roofStep = RefinedRung(
            roughHouseAndCloud,
            houseBoxTop,
            houseBox,
            cloudOval,
            houseRoof,
            peakedHouseAndOval);
        var featureRectStep = RefinedRung(
            peakedHouseAndOval,
            houseRoof,
            peakedHouseAndOval,
            peakedHouseAndOval,
            Chain(doorRectangle, windowRectangle),
            rectFeatures);
        var featureRefineStep = RefinedRung(
            rectFeatures,
            Chain(doorRectangle, windowRectangle),
            Chain(doorRectangle, windowRectangle),
            Chain(houseRoof, cloudOval),
            Chain(archedDoor, roundedWindowPlus),
            refinedFeatures);
        var cloudRefineStep = RefinedRung(
            refinedFeatures,
            cloudLeftCusp,
            cloudOval,
            Chain(houseRoof, archedDoor, roundedWindowPlus),
            cloudOutline,
            refinedCloud);
        var antennaLineStep = RefinedRung(
            refinedCloud,
            roofAntennaJoint,
            roofAntennaJoint,
            refinedCloud,
            antennaStem,
            antennaLine);
        var antennaRefineStep = RefinedRung(
            antennaLine,
            antennaTip,
            antennaStem,
            refinedCloud,
            refinedAntenna,
            finalSketch);
        var sketchRungs = new[]
        {
            baseRung,
            roofStep.Rung,
            featureRectStep.Rung,
            featureRefineStep.Rung,
            cloudRefineStep.Rung,
            antennaLineStep.Rung,
            antennaRefineStep.Rung
        };
        var sketchFocuses = new[]
        {
            roofStep.Focus,
            featureRectStep.Focus,
            featureRefineStep.Focus,
            cloudRefineStep.Focus,
            antennaLineStep.Focus,
            antennaRefineStep.Focus
        };
        var ladder = TimelineLadder(sketchRungs, sketchFocuses);
        var rungLabels = new[]
        {
            "Box + oval",
            "Peak roof",
            "Door/window rectangles",
            "Arch door + window",
            "Puffy cloud",
            "Antenna line",
            "Antenna refined"
        };

        return new SketchScene(
            "House Drawing Ladder",
            "Ordered rough-in ladder from box and oval to the finished sketch.",
            ladder,
            seed,
            new SKRect(-148, -84, 58, 38),
            rungLabels);
    }

    private static (Relation Rung, Relation Focus) RefinedRung(
        IElement source,
        IElement focusBasis,
        IElement participation,
        IElement complement,
        IElement replacement,
        IElement result)
    {
        var correspondence = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(participation, replacement));
        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            focusBasis,
            participation,
            complement,
            replacement,
            correspondence,
            UnknownPoint(),
            UnknownPoint());
        var rung = StructuralLadderSurface.Rung(
            new Carrier(),
            source,
            focus,
            result,
            correspondence,
            UnknownPoint(),
            UnknownPoint());
        return (rung, focus);
    }

    private static Relation TimelineLadder(IReadOnlyList<Relation> rungs, IReadOnlyList<Relation> focuses)
    {
        if (rungs.Count == 0)
        {
            return StructuralLadderSurface.BaseRung(new Carrier(), UnknownPoint());
        }

        var clock = TimeStepSequence.CreateClock(new Carrier());
        var stepCount = Math.Min(Math.Max(0, rungs.Count - 1), focuses.Count);
        var steps = new Relation[stepCount];
        for (var index = 0; index < stepCount; index++)
        {
            steps[index] = StructuralLadderSurface.Step(
                new Carrier(),
                clock,
                index,
                index + 1,
                rungs[index],
                focuses[index],
                rungs[index + 1],
                UnknownPoint());
        }

        return StructuralLadderSurface.Create(
            new Carrier(),
            rungs[0],
            StructuralLadderSurface.RungFrame(new Carrier(), [.. rungs]),
            StructuralLadderSurface.Timeline(new Carrier(), clock, rungs[0], steps),
            UnknownPoint(),
            UnknownPoint());
    }

    private static Relation Point(Carrier frame, Carrier x, Carrier y, long xTicks, long yTicks) =>
        GraphicStructure.FramePoint(frame, new CarrierLocation(x, xTicks), new CarrierLocation(y, yTicks));

    private static Relation UnknownPoint() =>
        Relation.Point(Carrier.Unknown, 0);

    private static Relation Chain(params IElement[] elements)
    {
        if (elements.Length == 0)
        {
            return new Relation(Carrier.Unknown, Carrier.Unknown);
        }

        IElement structure = elements[^1];
        for (var index = elements.Length - 2; index >= 0; index--)
        {
            structure = new Relation(elements[index], structure);
        }

        return structure as Relation ?? new Relation(structure, structure);
    }
}
