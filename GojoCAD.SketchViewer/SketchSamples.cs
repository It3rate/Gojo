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

        Relation P(long xTicks, long yTicks) =>
            Point(frame, x, y, xTicks, yTicks);

        Relation Oval(long centerX, long centerY, long radiusX, long radiusY)
        {
            var left = P(centerX - radiusX, centerY);
            var top = P(centerX, centerY + radiusY);
            var right = P(centerX + radiusX, centerY);
            var bottom = P(centerX, centerY - radiusY);
            var controlX = Math.Max(2, radiusX / 2);
            var controlY = Math.Max(2, radiusY / 2);
            return Chain(
                GraphicStructure.FrameCubicCurve(left, P(centerX - radiusX, centerY + controlY), P(centerX - controlX, centerY + radiusY), top),
                GraphicStructure.FrameCubicCurve(top, P(centerX + controlX, centerY + radiusY), P(centerX + radiusX, centerY + controlY), right),
                GraphicStructure.FrameCubicCurve(right, P(centerX + radiusX, centerY - controlY), P(centerX + controlX, centerY - radiusY), bottom),
                GraphicStructure.FrameCubicCurve(bottom, P(centerX - controlX, centerY - radiusY), P(centerX - radiusX, centerY - controlY), left));
        }

        Relation Cloudlet(long anchorX, long anchorY)
        {
            var left = P(anchorX, anchorY);
            var leftPeak = P(anchorX + 10, anchorY + 12);
            var leftValley = P(anchorX + 19, anchorY + 5);
            var middlePeak = P(anchorX + 30, anchorY + 16);
            var middleValley = P(anchorX + 40, anchorY + 6);
            var rightPeak = P(anchorX + 49, anchorY + 11);
            var right = P(anchorX + 54, anchorY - 1);
            var lowerRight = P(anchorX + 42, anchorY - 9);
            var lowerLeft = P(anchorX + 16, anchorY - 8);
            return Chain(
                GraphicStructure.FrameCubicCurve(left, P(anchorX, anchorY + 7), P(anchorX + 7, anchorY + 12), leftPeak),
                GraphicStructure.FrameCubicCurve(leftPeak, P(anchorX + 13, anchorY + 12), P(anchorX + 18, anchorY + 9), leftValley),
                GraphicStructure.FrameCubicCurve(leftValley, P(anchorX + 21, anchorY + 12), P(anchorX + 26, anchorY + 16), middlePeak),
                GraphicStructure.FrameCubicCurve(middlePeak, P(anchorX + 34, anchorY + 16), P(anchorX + 38, anchorY + 11), middleValley),
                GraphicStructure.FrameCubicCurve(middleValley, P(anchorX + 43, anchorY + 11), P(anchorX + 45, anchorY + 11), rightPeak),
                GraphicStructure.FrameCubicCurve(rightPeak, P(anchorX + 52, anchorY + 11), P(anchorX + 54, anchorY + 5), right),
                GraphicStructure.FrameCubicCurve(right, P(anchorX + 54, anchorY - 6), P(anchorX + 48, anchorY - 9), lowerRight),
                GraphicStructure.FrameCubicCurve(lowerRight, P(anchorX + 36, anchorY - 9), P(anchorX + 22, anchorY - 8), lowerLeft),
                GraphicStructure.FrameCubicCurve(lowerLeft, P(anchorX + 6, anchorY - 8), P(anchorX, anchorY - 7), left));
        }

        var lowerLeft = P(-52, -30);
        var lowerRight = P(52, -30);
        var upperRight = P(52, 8);
        var upperLeft = P(-52, 8);
        var roofApex = P(0, 40);
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
            GraphicStructure.FrameSegment(roofApex, upperLeft),
            GraphicStructure.FrameSegment(upperLeft, lowerLeft));

        var doorLeftBottom = P(-14, -30);
        var doorRightBottom = P(14, -30);
        var doorRightTop = P(14, -8);
        var doorLeftTop = P(-14, -8);
        var doorRectangle = Chain(
            GraphicStructure.FrameSegment(doorLeftBottom, doorRightBottom),
            GraphicStructure.FrameSegment(doorRightBottom, doorRightTop),
            GraphicStructure.FrameSegment(doorRightTop, doorLeftTop),
            GraphicStructure.FrameSegment(doorLeftTop, doorLeftBottom));
        var archedDoor = Chain(
            GraphicStructure.FrameSegment(doorLeftBottom, doorLeftTop),
            GraphicStructure.FrameCubicCurve(doorLeftTop, P(-14, 8), P(14, 8), doorRightTop),
            GraphicStructure.FrameSegment(doorRightTop, doorRightBottom),
            GraphicStructure.FrameSegment(doorRightBottom, doorLeftBottom));

        var windowLowerLeft = P(22, -14);
        var windowLowerRight = P(42, -14);
        var windowUpperRight = P(42, 6);
        var windowUpperLeft = P(22, 6);
        var windowCenter = P(32, -4);
        var windowRectangle = Chain(
            GraphicStructure.FrameSegment(windowLowerLeft, windowLowerRight),
            GraphicStructure.FrameSegment(windowLowerRight, windowUpperRight),
            GraphicStructure.FrameSegment(windowUpperRight, windowUpperLeft),
            GraphicStructure.FrameSegment(windowUpperLeft, windowLowerLeft));
        var windowCross = Chain(
            GraphicStructure.FrameSegment(P(22, -4), windowCenter),
            GraphicStructure.FrameSegment(windowCenter, P(42, -4)),
            GraphicStructure.FrameSegment(P(32, 6), windowCenter),
            GraphicStructure.FrameSegment(windowCenter, P(32, -14)));
        var roundedWindow = Chain(
            GraphicStructure.FrameSegment(P(26, 6), P(38, 6)),
            GraphicStructure.FrameCubicCurve(P(38, 6), P(40, 6), P(42, 4), P(42, 2)),
            GraphicStructure.FrameSegment(P(42, 2), P(42, -10)),
            GraphicStructure.FrameCubicCurve(P(42, -10), P(42, -12), P(40, -14), P(38, -14)),
            GraphicStructure.FrameSegment(P(38, -14), P(26, -14)),
            GraphicStructure.FrameCubicCurve(P(26, -14), P(24, -14), P(22, -12), P(22, -10)),
            GraphicStructure.FrameSegment(P(22, -10), P(22, 2)),
            GraphicStructure.FrameCubicCurve(P(22, 2), P(22, 4), P(24, 6), P(26, 6)));
        var roundedWindowPlus = Chain(roundedWindow, windowCross);

        var chimneyLowerLeft = P(14, 31);
        var chimneyLowerRight = P(24, 25);
        var chimneyUpperRight = P(24, 42);
        var chimneyUpperLeft = P(14, 42);
        var chimneyBody = Chain(
            GraphicStructure.FrameSegment(chimneyLowerLeft, chimneyLowerRight),
            GraphicStructure.FrameSegment(chimneyLowerRight, chimneyUpperRight),
            GraphicStructure.FrameSegment(chimneyUpperRight, chimneyUpperLeft),
            GraphicStructure.FrameSegment(chimneyUpperLeft, chimneyLowerLeft));
        var chimneyWithCap = Chain(
            chimneyBody,
            GraphicStructure.FrameSegment(P(10, 42), P(28, 42)),
            GraphicStructure.FrameSegment(P(28, 42), P(19, 48)),
            GraphicStructure.FrameSegment(P(19, 48), P(10, 42)));

        var cloudOval = Oval(98, 48, 27, 12);
        var cloudOutline = Cloudlet(70, 48);

        var treeTrunkLine = GraphicStructure.FrameSegment(P(-94, -30), P(-94, 8));
        var treeBall = Oval(-94, 22, 17, 15);
        var treeLineBall = Chain(treeTrunkLine, treeBall);
        var treeBranches = Chain(
            treeTrunkLine,
            GraphicStructure.FrameSegment(P(-94, 4), P(-113, 20)),
            GraphicStructure.FrameSegment(P(-94, 4), P(-76, 22)),
            treeBall);
        var treeTrunkThick = Chain(
            GraphicStructure.FrameSegment(P(-99, -30), P(-90, -30)),
            GraphicStructure.FrameSegment(P(-90, -30), P(-90, 8)),
            GraphicStructure.FrameSegment(P(-90, 8), P(-99, 8)),
            GraphicStructure.FrameSegment(P(-99, 8), P(-99, -30)));
        var treeWithThickTrunk = Chain(
            treeTrunkThick,
            GraphicStructure.FrameSegment(P(-94, 4), P(-113, 20)),
            GraphicStructure.FrameSegment(P(-94, 4), P(-76, 22)),
            treeBall);
        var leftBranchThick = Chain(
            GraphicStructure.FrameSegment(P(-97, 6), P(-92, 2)),
            GraphicStructure.FrameSegment(P(-92, 2), P(-111, 18)),
            GraphicStructure.FrameSegment(P(-111, 18), P(-115, 22)),
            GraphicStructure.FrameSegment(P(-115, 22), P(-97, 6)));
        var rightBranchThick = Chain(
            GraphicStructure.FrameSegment(P(-96, 6), P(-91, 3)),
            GraphicStructure.FrameSegment(P(-91, 3), P(-75, 20)),
            GraphicStructure.FrameSegment(P(-75, 20), P(-78, 25)),
            GraphicStructure.FrameSegment(P(-78, 25), P(-96, 6)));
        var treeWoodThick = Chain(treeTrunkThick, leftBranchThick, rightBranchThick, treeBall);
        var treeLeafRegions = Chain(
            treeTrunkThick,
            leftBranchThick,
            rightBranchThick,
            Oval(-112, 28, 15, 13),
            Oval(-93, 35, 18, 16),
            Oval(-74, 28, 15, 13));
        var treeFluffy = Chain(
            treeTrunkThick,
            leftBranchThick,
            rightBranchThick,
            Cloudlet(-134, 26),
            Cloudlet(-121, 38),
            Cloudlet(-108, 28));

        var rough = Chain(houseBox, cloudOval, treeLineBall);
        var roofState = Chain(houseRoof, cloudOval, treeLineBall);
        var treeSplitState = Chain(houseRoof, cloudOval, treeBranches);
        var doorRectState = Chain(houseRoof, cloudOval, treeBranches, doorRectangle);
        var treeTrunkState = Chain(houseRoof, cloudOval, treeWithThickTrunk, doorRectangle);
        var windowRectState = Chain(houseRoof, cloudOval, treeWithThickTrunk, doorRectangle, windowRectangle);
        var treeBranchThickState = Chain(houseRoof, cloudOval, treeWoodThick, doorRectangle, windowRectangle);
        var doorArchState = Chain(houseRoof, cloudOval, treeWoodThick, archedDoor, windowRectangle);
        var windowRefinedState = Chain(houseRoof, cloudOval, treeWoodThick, archedDoor, roundedWindowPlus);
        var treeRegionsState = Chain(houseRoof, cloudOval, treeLeafRegions, archedDoor, roundedWindowPlus);
        var cloudRefinedState = Chain(houseRoof, cloudOutline, treeLeafRegions, archedDoor, roundedWindowPlus);
        var chimneyBodyState = Chain(houseRoof, cloudOutline, treeLeafRegions, archedDoor, roundedWindowPlus, chimneyBody);
        var treeFluffyState = Chain(houseRoof, cloudOutline, treeFluffy, archedDoor, roundedWindowPlus, chimneyBody);
        var finalSketch = Chain(houseRoof, cloudOutline, treeFluffy, archedDoor, roundedWindowPlus, chimneyWithCap);

        var baseRung = StructuralLadderSurface.BaseRung(new Carrier(), rough);
        var roofStep = RefinedRung(rough, houseBoxTop, houseBox, Chain(cloudOval, treeLineBall), houseRoof, roofState);
        var treeSplitStep = RefinedRung(roofState, P(-94, 4), treeLineBall, Chain(houseRoof, cloudOval), treeBranches, treeSplitState);
        var doorRectStep = RefinedRung(treeSplitState, doorLeftBottom, doorLeftBottom, treeSplitState, doorRectangle, doorRectState);
        var treeTrunkStep = RefinedRung(doorRectState, treeTrunkLine, treeBranches, Chain(houseRoof, cloudOval, doorRectangle), treeWithThickTrunk, treeTrunkState);
        var windowRectStep = RefinedRung(treeTrunkState, windowLowerLeft, windowLowerLeft, treeTrunkState, windowRectangle, windowRectState);
        var treeBranchThickStep = RefinedRung(windowRectState, P(-94, 4), treeWithThickTrunk, Chain(houseRoof, cloudOval, doorRectangle, windowRectangle), treeWoodThick, treeBranchThickState);
        var doorArchStep = RefinedRung(treeBranchThickState, doorRectangle, doorRectangle, Chain(houseRoof, cloudOval, treeWoodThick, windowRectangle), archedDoor, doorArchState);
        var windowRefineStep = RefinedRung(doorArchState, windowRectangle, windowRectangle, Chain(houseRoof, cloudOval, treeWoodThick, archedDoor), roundedWindowPlus, windowRefinedState);
        var treeRegionStep = RefinedRung(windowRefinedState, treeBall, treeWoodThick, Chain(houseRoof, cloudOval, archedDoor, roundedWindowPlus), treeLeafRegions, treeRegionsState);
        var cloudRefineStep = RefinedRung(treeRegionsState, cloudOval, cloudOval, Chain(houseRoof, treeLeafRegions, archedDoor, roundedWindowPlus), cloudOutline, cloudRefinedState);
        var chimneyBodyStep = RefinedRung(cloudRefinedState, P(19, 33), P(19, 33), cloudRefinedState, chimneyBody, chimneyBodyState);
        var treeFluffyStep = RefinedRung(chimneyBodyState, treeLeafRegions, treeLeafRegions, Chain(houseRoof, cloudOutline, archedDoor, roundedWindowPlus, chimneyBody), treeFluffy, treeFluffyState);
        var chimneyCapStep = RefinedRung(treeFluffyState, chimneyBody, chimneyBody, Chain(houseRoof, cloudOutline, treeFluffy, archedDoor, roundedWindowPlus), chimneyWithCap, finalSketch);
        var sketchRungs = new[]
        {
            baseRung,
            roofStep.Rung,
            treeSplitStep.Rung,
            doorRectStep.Rung,
            treeTrunkStep.Rung,
            windowRectStep.Rung,
            treeBranchThickStep.Rung,
            doorArchStep.Rung,
            windowRefineStep.Rung,
            treeRegionStep.Rung,
            cloudRefineStep.Rung,
            chimneyBodyStep.Rung,
            treeFluffyStep.Rung,
            chimneyCapStep.Rung
        };
        var sketchFocuses = new[]
        {
            roofStep.Focus,
            treeSplitStep.Focus,
            doorRectStep.Focus,
            treeTrunkStep.Focus,
            windowRectStep.Focus,
            treeBranchThickStep.Focus,
            doorArchStep.Focus,
            windowRefineStep.Focus,
            treeRegionStep.Focus,
            cloudRefineStep.Focus,
            chimneyBodyStep.Focus,
            treeFluffyStep.Focus,
            chimneyCapStep.Focus
        };
        var ladder = TimelineLadder(sketchRungs, sketchFocuses);
        var rungLabels = new[]
        {
            "Box + oval + tree ball",
            "Peak roof",
            "Tree branch split",
            "Door rectangle",
            "Tree trunk thickens",
            "Window rectangle",
            "Tree branches thicken",
            "Door arch",
            "Window rounded plus",
            "Tree leaf regions",
            "Puffy cloud",
            "Chimney body",
            "Tree fluffs out",
            "Pointy chimney cap"
        };

        return new SketchScene(
            "House Drawing Ladder",
            "Ordered rough-in ladder from box and oval to the finished sketch.",
            ladder,
            seed,
            new SKRect(-142, -84, 136, 42),
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
