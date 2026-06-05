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
        var windowCrossedRectangle = Chain(windowRectangle, windowCross);
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
        var windowCrossCorrespondence = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(windowRectangle, windowCrossedRectangle),
            FocusSurface.Bind(windowCenter, windowCross));
        var windowCrossFocus = FocusSurface.Create(
            new Carrier(),
            windowRectangle,
            windowCenter,
            windowRectangle,
            windowRectangle,
            windowCross,
            windowCrossCorrespondence,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var windowBaseRung = StructuralLadderSurface.BaseRung(new Carrier(), windowRectangle);
        var windowCrossRung = StructuralLadderSurface.Rung(
            new Carrier(),
            windowRectangle,
            windowCrossFocus,
            windowCrossedRectangle,
            windowCrossCorrespondence,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var windowRoundCorrespondence = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(windowRectangle, roundedWindow),
            FocusSurface.Bind(windowCross, windowCross));
        var windowRoundFocus = FocusSurface.Create(
            new Carrier(),
            windowCrossedRectangle,
            windowRectangle,
            windowRectangle,
            windowCross,
            roundedWindow,
            windowRoundCorrespondence,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var windowRoundedRung = StructuralLadderSurface.Rung(
            new Carrier(),
            windowCrossedRectangle,
            windowRoundFocus,
            roundedWindowPlus,
            windowRoundCorrespondence,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var windowLadder = StructuralLadderSurface.CreateWithRungs(
            new Carrier(),
            windowBaseRung,
            windowCrossRung,
            windowRoundedRung);

        var antennaTip = Point(frame, x, y, -29, 43);
        var antennaLeft = Point(frame, x, y, -38, 48);
        var antennaRight = Point(frame, x, y, -18, 46);

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
        var cloudPlacement = new Relation(upperLeft, new Relation(cloudLeftCusp, Carrier.Unknown));
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
        var cloudCorrespondence = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(cloudLeftCusp, cloudOutline));
        var cloudFocus = FocusSurface.Create(
            new Carrier(),
            cloudPlacement,
            upperLeft,
            cloudLeftCusp,
            cloudPlacement,
            cloudOutline,
            cloudCorrespondence,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var cloudBaseRung = StructuralLadderSurface.BaseRung(new Carrier(), cloudPlacement);
        var cloudRefinedRung = StructuralLadderSurface.Rung(
            new Carrier(),
            cloudPlacement,
            cloudFocus,
            cloudOutline,
            cloudCorrespondence,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var cloudLadder = StructuralLadderSurface.CreateWithRungs(new Carrier(), cloudBaseRung, cloudRefinedRung);

        var elements = new IElement[]
        {
            // Outer closed house outline.
            GraphicStructure.FrameSegment(lowerLeft, lowerRight),
            GraphicStructure.FrameSegment(lowerRight, upperRight),
            GraphicStructure.FrameSegment(upperRight, roofApex),
            GraphicStructure.FrameSegment(roofApex, roofAntennaJoint),
            GraphicStructure.FrameSegment(roofAntennaJoint, upperLeft),
            GraphicStructure.FrameSegment(upperLeft, lowerLeft),

            // Curved closed door loop. Cubic controls make the arch tangent to the side lines.
            GraphicStructure.FrameSegment(doorLeftBottom, doorLeftTop),
            GraphicStructure.FrameCubicCurve(doorLeftTop, doorLeftControl, doorRightControl, doorRightTop),
            GraphicStructure.FrameSegment(doorRightTop, doorRightBottom),
            GraphicStructure.FrameSegment(doorRightBottom, doorLeftBottom),

            // Window ladder: rectangle -> rectangle with joined plus -> rounded rectangle with joined plus.
            windowLadder,

            // Roof antenna: the branch shares an actual split point on the roof segment.
            GraphicStructure.FrameSegment(roofAntennaJoint, antennaTip),
            GraphicStructure.FrameSegment(antennaTip, antennaLeft),
            GraphicStructure.FrameSegment(antennaTip, antennaRight),

            // Cloud ladder: coarse placement anchor refines into the local offset outline.
            cloudLadder
        };

        return new SketchScene(
            "House Sketch With Roof Antenna",
            "Straight outline, tangent door arch, closed window, roof branch joint, and a cusped puffy cloud.",
            Chain(elements),
            seed,
            new SKRect(-148, -80, 48, 38));
    }

    private static Relation Point(Carrier frame, Carrier x, Carrier y, long xTicks, long yTicks) =>
        GraphicStructure.FramePoint(frame, new CarrierLocation(x, xTicks), new CarrierLocation(y, yTicks));

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
