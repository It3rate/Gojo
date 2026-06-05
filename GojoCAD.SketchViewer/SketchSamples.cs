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

        Relation Circle(long centerX, long centerY, long radius)
        {
            var center = P(centerX, centerY);
            var right = P(centerX + radius, centerY);
            var top = P(centerX, centerY + radius);
            var left = P(centerX - radius, centerY);
            var bottom = P(centerX, centerY - radius);
            return Chain(
                GraphicStructure.FrameArc(center, right, top),
                GraphicStructure.FrameArc(center, top, left),
                GraphicStructure.FrameArc(center, left, bottom),
                GraphicStructure.FrameArc(center, bottom, right));
        }

        Relation AreaPolygon(params Relation[] points)
        {
            if (points.Length < 3)
            {
                return UnknownPoint();
            }

            var edges = new IElement[points.Length];
            for (var index = 0; index < points.Length; index++)
            {
                edges[index] = GraphicStructure.FrameSegment(points[index], points[(index + 1) % points.Length]);
            }

            return GraphicStructure.AreaFill(edges);
        }

        Relation FilledCircle(long centerX, long centerY, long radius)
        {
            return Chain(
                AreaPolygon(
                    P(centerX + radius, centerY),
                    P(centerX + radius * 7 / 10, centerY + radius * 7 / 10),
                    P(centerX, centerY + radius),
                    P(centerX - radius * 7 / 10, centerY + radius * 7 / 10),
                    P(centerX - radius, centerY),
                    P(centerX - radius * 7 / 10, centerY - radius * 7 / 10),
                    P(centerX, centerY - radius),
                    P(centerX + radius * 7 / 10, centerY - radius * 7 / 10)),
                Circle(centerX, centerY, radius));
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

        Relation FilledLeafBlob(long centerX, long centerY, long radiusX, long radiusY)
        {
            return AreaPolygon(
                P(centerX - radiusX, centerY),
                P(centerX - radiusX * 5 / 6, centerY + radiusY / 2),
                P(centerX - radiusX / 2, centerY + radiusY),
                P(centerX, centerY + radiusY + 2),
                P(centerX + radiusX / 2, centerY + radiusY),
                P(centerX + radiusX, centerY + radiusY / 3),
                P(centerX + radiusX, centerY - radiusY / 3),
                P(centerX + radiusX / 2, centerY - radiusY),
                P(centerX, centerY - radiusY - 1),
                P(centerX - radiusX * 2 / 3, centerY - radiusY * 2 / 3));
        }

        var horizon = GraphicStructure.FrameSegment(P(-148, -30), P(134, -30));
        var lowerLeft = P(-52, -30);
        var lowerRight = P(52, -30);
        var upperRight = P(52, 8);
        var upperLeft = P(-52, 8);
        var roofApex = P(0, 40);
        var roofOverhangLeft = P(-65, 8);
        var roofOverhangApex = P(0, 48);
        var roofOverhangRight = P(65, 8);
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
        var roofOverhang = Chain(
            GraphicStructure.FrameSegment(roofOverhangLeft, roofOverhangApex),
            GraphicStructure.FrameSegment(roofOverhangApex, roofOverhangRight));
        var houseWithOverhang = Chain(houseRoof, roofOverhang);

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
        var windowPlus = Chain(windowRectangle, windowCross);
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

        var chimneyLowerLeft = P(14, 39);
        var chimneyLowerRight = P(26, 32);
        var chimneyUpperRight = P(26, 51);
        var chimneyUpperLeft = P(14, 51);
        var chimneyBody = Chain(
            GraphicStructure.FrameSegment(chimneyLowerLeft, chimneyLowerRight),
            GraphicStructure.FrameSegment(chimneyLowerRight, chimneyUpperRight),
            GraphicStructure.FrameSegment(chimneyUpperRight, chimneyUpperLeft),
            GraphicStructure.FrameSegment(chimneyUpperLeft, chimneyLowerLeft));
        var chimneyWithCap = Chain(
            chimneyBody,
            GraphicStructure.FrameSegment(P(10, 51), P(30, 51)),
            GraphicStructure.FrameSegment(P(30, 51), P(20, 58)),
            GraphicStructure.FrameSegment(P(20, 58), P(10, 51)));
        var doorKnob = FilledCircle(7, -15, 2);

        var cloudOval = Oval(84, 48, 24, 11);
        var cloudOutline = Cloudlet(58, 48);

        var treeRoot = P(-108, -30);
        var treeTop = P(-108, 8);
        var treeJunction = P(-108, 4);
        var treeLeftTip = P(-128, 22);
        var treeRightTip = P(-88, 23);
        var treeTrunkLine = GraphicStructure.FrameSegment(treeRoot, treeTop);
        var treeBall = Circle(-108, 24, 17);
        var treeLineBall = Chain(treeTrunkLine, treeBall);
        var treeBranches = Chain(
            treeTrunkLine,
            GraphicStructure.FrameSegment(treeJunction, treeLeftTip),
            GraphicStructure.FrameSegment(treeJunction, treeRightTip),
            treeBall);
        var treeLeftCurve = GraphicStructure.FrameCubicCurve(treeJunction, P(-112, 11), P(-121, 18), treeLeftTip);
        var treeRightCurve = GraphicStructure.FrameCubicCurve(treeJunction, P(-103, 12), P(-95, 20), treeRightTip);
        var treeCurvedBranches = Chain(treeTrunkLine, treeLeftCurve, treeRightCurve, treeBall);
        var treeTrunkThick = Chain(
            GraphicStructure.FrameSegment(P(-113, -30), P(-103, -30)),
            GraphicStructure.FrameSegment(P(-103, -30), P(-103, 8)),
            GraphicStructure.FrameSegment(P(-103, 8), P(-113, 8)),
            GraphicStructure.FrameSegment(P(-113, 8), P(-113, -30)));
        var treeWithThickTrunk = Chain(
            treeTrunkThick,
            treeLeftCurve,
            treeRightCurve,
            treeBall);
        var leftBranchThick = AreaPolygon(
            P(-111, 2),
            P(-121, 11),
            P(-132, 20),
            P(-135, 24),
            P(-130, 28),
            P(-119, 19),
            P(-106, 8),
            P(-105, 4));
        var rightBranchThick = AreaPolygon(
            P(-111, 6),
            P(-100, 16),
            P(-90, 26),
            P(-84, 26),
            P(-85, 20),
            P(-96, 13),
            P(-106, 2),
            P(-111, 3));
        var treeWoodThick = Chain(treeTrunkThick, leftBranchThick, rightBranchThick, treeBall);
        var treeWoodMerged = AreaPolygon(
            P(-113, -30),
            P(-103, -30),
            P(-103, 2),
            P(-98, 7),
            P(-90, 14),
            P(-82, 22),
            P(-82, 27),
            P(-88, 28),
            P(-98, 20),
            P(-106, 12),
            P(-110, 12),
            P(-119, 20),
            P(-130, 28),
            P(-136, 25),
            P(-132, 20),
            P(-121, 11),
            P(-113, 5));
        var treeLeafRegions = Chain(
            treeWoodMerged,
            Circle(-128, 29, 15),
            Circle(-108, 39, 17),
            Circle(-88, 30, 15));
        var treeFluffy = Chain(
            treeWoodMerged,
            FilledLeafBlob(-130, 29, 17, 15),
            FilledLeafBlob(-108, 41, 19, 17),
            FilledLeafBlob(-87, 31, 17, 15));

        var horizonState = horizon;
        var houseBoxState = Chain(horizon, houseBox);
        var roofState = Chain(horizon, houseRoof);
        var treeSeedState = Chain(horizon, houseRoof, treeLineBall);
        var cloudSeedState = Chain(horizon, houseRoof, treeLineBall, cloudOval);
        var roofOverhangState = Chain(horizon, houseWithOverhang, treeLineBall, cloudOval);
        var treeSplitState = Chain(horizon, houseWithOverhang, treeBranches, cloudOval);
        var doorRectState = Chain(horizon, houseWithOverhang, treeBranches, cloudOval, doorRectangle);
        var treeCurvedState = Chain(horizon, houseWithOverhang, treeCurvedBranches, cloudOval, doorRectangle);
        var windowRectState = Chain(horizon, houseWithOverhang, treeCurvedBranches, cloudOval, doorRectangle, windowRectangle);
        var treeTrunkState = Chain(horizon, houseWithOverhang, treeWithThickTrunk, cloudOval, doorRectangle, windowRectangle);
        var doorArchState = Chain(horizon, houseWithOverhang, treeWithThickTrunk, cloudOval, archedDoor, windowRectangle);
        var treeBranchThickState = Chain(horizon, houseWithOverhang, treeWoodThick, cloudOval, archedDoor, windowRectangle);
        var treeWoodMergedState = Chain(horizon, houseWithOverhang, treeWoodMerged, cloudOval, archedDoor, windowRectangle);
        var windowPlusState = Chain(horizon, houseWithOverhang, treeWoodMerged, cloudOval, archedDoor, windowPlus);
        var windowRefinedState = Chain(horizon, houseWithOverhang, treeWoodMerged, cloudOval, archedDoor, roundedWindowPlus);
        var treeRegionsState = Chain(horizon, houseWithOverhang, treeLeafRegions, cloudOval, archedDoor, roundedWindowPlus);
        var cloudRefinedState = Chain(horizon, houseWithOverhang, treeLeafRegions, cloudOutline, archedDoor, roundedWindowPlus);
        var chimneyBodyState = Chain(horizon, houseWithOverhang, treeLeafRegions, cloudOutline, archedDoor, roundedWindowPlus, chimneyBody);
        var treeFluffyState = Chain(horizon, houseWithOverhang, treeFluffy, cloudOutline, archedDoor, roundedWindowPlus, chimneyBody);
        var chimneyCapState = Chain(horizon, houseWithOverhang, treeFluffy, cloudOutline, archedDoor, roundedWindowPlus, chimneyWithCap);
        var finalSketch = Chain(chimneyCapState, doorKnob);

        var baseRung = StructuralLadderSurface.BaseRung(new Carrier(), horizonState);
        var houseBoxStep = RefinedRung(horizonState, houseBox, horizon, horizonState, houseBox, houseBoxState);
        var roofStep = RefinedRung(houseBoxState, houseBoxTop, houseBox, horizon, houseRoof, roofState);
        var treeSeedStep = RefinedRung(roofState, treeRoot, treeRoot, roofState, treeLineBall, treeSeedState);
        var cloudSeedStep = RefinedRung(treeSeedState, cloudOval, cloudOval, treeSeedState, cloudOval, cloudSeedState);
        var roofOverhangStep = RefinedRung(cloudSeedState, roofOverhang, roofOverhang, cloudSeedState, roofOverhang, roofOverhangState);
        var treeSplitStep = RefinedRung(roofOverhangState, treeJunction, treeLineBall, Chain(horizon, houseWithOverhang, cloudOval), treeBranches, treeSplitState);
        var doorRectStep = RefinedRung(treeSplitState, doorLeftBottom, doorLeftBottom, treeSplitState, doorRectangle, doorRectState);
        var treeCurveStep = RefinedRung(doorRectState, treeJunction, treeBranches, Chain(horizon, houseWithOverhang, cloudOval, doorRectangle), treeCurvedBranches, treeCurvedState);
        var windowRectStep = RefinedRung(treeCurvedState, windowLowerLeft, windowLowerLeft, treeCurvedState, windowRectangle, windowRectState);
        var treeTrunkStep = RefinedRung(windowRectState, treeTrunkLine, treeCurvedBranches, Chain(horizon, houseWithOverhang, cloudOval, doorRectangle, windowRectangle), treeWithThickTrunk, treeTrunkState);
        var doorArchStep = RefinedRung(treeTrunkState, doorRectangle, doorRectangle, Chain(horizon, houseWithOverhang, cloudOval, treeWithThickTrunk, windowRectangle), archedDoor, doorArchState);
        var treeBranchThickStep = RefinedRung(doorArchState, treeJunction, treeWithThickTrunk, Chain(horizon, houseWithOverhang, cloudOval, archedDoor, windowRectangle), treeWoodThick, treeBranchThickState);
        var treeWoodMergeStep = RefinedRung(treeBranchThickState, treeWoodThick, treeWoodThick, Chain(horizon, houseWithOverhang, cloudOval, archedDoor, windowRectangle), treeWoodMerged, treeWoodMergedState);
        var windowPlusStep = RefinedRung(treeWoodMergedState, windowRectangle, windowRectangle, Chain(horizon, houseWithOverhang, cloudOval, treeWoodMerged, archedDoor), windowPlus, windowPlusState);
        var windowRefineStep = RefinedRung(windowPlusState, windowRectangle, windowPlus, Chain(horizon, houseWithOverhang, cloudOval, treeWoodMerged, archedDoor), roundedWindowPlus, windowRefinedState);
        var treeRegionStep = RefinedRung(windowRefinedState, treeBall, treeWoodMerged, Chain(horizon, houseWithOverhang, cloudOval, archedDoor, roundedWindowPlus), treeLeafRegions, treeRegionsState);
        var cloudRefineStep = RefinedRung(treeRegionsState, cloudOval, cloudOval, Chain(horizon, houseWithOverhang, treeLeafRegions, archedDoor, roundedWindowPlus), cloudOutline, cloudRefinedState);
        var chimneyBodyStep = RefinedRung(cloudRefinedState, roofOverhang, roofOverhang, cloudRefinedState, chimneyBody, chimneyBodyState);
        var treeFluffyStep = RefinedRung(chimneyBodyState, treeLeafRegions, treeLeafRegions, Chain(horizon, houseWithOverhang, cloudOutline, archedDoor, roundedWindowPlus, chimneyBody), treeFluffy, treeFluffyState);
        var chimneyCapStep = RefinedRung(treeFluffyState, chimneyBody, chimneyBody, Chain(horizon, houseWithOverhang, cloudOutline, treeFluffy, archedDoor, roundedWindowPlus), chimneyWithCap, chimneyCapState);
        var doorKnobStep = RefinedRung(chimneyCapState, doorKnob, doorKnob, chimneyCapState, doorKnob, finalSketch);
        var sketchRungs = new[]
        {
            baseRung,
            houseBoxStep.Rung,
            roofStep.Rung,
            treeSeedStep.Rung,
            cloudSeedStep.Rung,
            roofOverhangStep.Rung,
            treeSplitStep.Rung,
            doorRectStep.Rung,
            treeCurveStep.Rung,
            windowRectStep.Rung,
            treeTrunkStep.Rung,
            doorArchStep.Rung,
            treeBranchThickStep.Rung,
            treeWoodMergeStep.Rung,
            windowPlusStep.Rung,
            windowRefineStep.Rung,
            treeRegionStep.Rung,
            cloudRefineStep.Rung,
            chimneyBodyStep.Rung,
            treeFluffyStep.Rung,
            chimneyCapStep.Rung,
            doorKnobStep.Rung
        };
        var sketchApplications = new[]
        {
            houseBoxStep.Application,
            roofStep.Application,
            treeSeedStep.Application,
            cloudSeedStep.Application,
            roofOverhangStep.Application,
            treeSplitStep.Application,
            doorRectStep.Application,
            treeCurveStep.Application,
            windowRectStep.Application,
            treeTrunkStep.Application,
            doorArchStep.Application,
            treeBranchThickStep.Application,
            treeWoodMergeStep.Application,
            windowPlusStep.Application,
            windowRefineStep.Application,
            treeRegionStep.Application,
            cloudRefineStep.Application,
            chimneyBodyStep.Application,
            treeFluffyStep.Application,
            chimneyCapStep.Application,
            doorKnobStep.Application
        };
        var ladder = TimelineLadder(sketchRungs, sketchApplications);
        var rungLabels = new[]
        {
            "Horizon",
            "House box",
            "Peak roof",
            "Tree line + circular crown",
            "Cloud oval",
            "Roof overhang",
            "Tree branch split",
            "Door rectangle",
            "Tree split becomes curves",
            "Window rectangle",
            "Tree trunk thickens",
            "Door arch",
            "Tree branches thicken",
            "Tree wood merges",
            "Window plus",
            "Window rounded rectangle",
            "Tree leaf circles",
            "Puffy cloud",
            "Chimney body",
            "Tree fluffs out",
            "Pointy chimney cap",
            "Door knob"
        };

        return new SketchScene(
            "House Drawing Ladder",
            "Ordered rough-in ladder from box and oval to the finished sketch.",
            ladder,
            seed,
            new SKRect(-156, -84, 138, 64),
            rungLabels);
    }

    private static (Relation Rung, Relation Focus, Relation Application, Relation Transform) RefinedRung(
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

        var selectedPort = new Placeholder();
        var carryPort = new Placeholder();
        var replacementPort = new Placeholder();
        var resultPort = new Placeholder();
        var selectedCarryReplacementPorts = TransformSurface.Ports(new Carrier(), selectedPort, carryPort, replacementPort);
        var operation = TransformSurface.Fold(selectedCarryReplacementPorts, resultPort);
        var operationProvenance = TransformSurface.Frame(
            new Carrier(),
            TransformSurface.Remove(selectedPort),
            TransformSurface.Carry(carryPort, carryPort),
            TransformSurface.Map(selectedPort, replacementPort),
            operation);
        var transform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), selectedPort, carryPort, replacementPort),
            operation,
            TransformSurface.Ports(new Carrier(), resultPort),
            UnknownPoint(),
            operationProvenance);
        var bindings = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(selectedPort, participation),
            FocusSurface.Bind(carryPort, complement),
            FocusSurface.Bind(replacementPort, replacement),
            FocusSurface.Bind(resultPort, result),
            FocusSurface.Bind(participation, replacement));

        if (!TransformSurface.TryCreateApplication(
                new Carrier(),
                transform,
                focus,
                bindings,
                result,
                UnknownPoint(),
                UnknownPoint(),
                out var application))
        {
            throw new InvalidOperationException("Generated sketch refinement did not create a transform application.");
        }

        if (!StructuralLadderSurface.TryCreateRungFromTransformApplication(
                new Carrier(),
                application,
                out var rung))
        {
            throw new InvalidOperationException("Generated sketch transform application did not create a ladder rung.");
        }

        return (rung, focus, application, transform);
    }

    private static Relation TimelineLadder(IReadOnlyList<Relation> rungs, IReadOnlyList<Relation> applications)
    {
        if (rungs.Count == 0)
        {
            return StructuralLadderSurface.BaseRung(new Carrier(), UnknownPoint());
        }

        var clock = TimeStepSequence.CreateClock(new Carrier());
        var stepCount = Math.Min(Math.Max(0, rungs.Count - 1), applications.Count);
        var steps = new Relation[stepCount];
        for (var index = 0; index < stepCount; index++)
        {
            steps[index] = StructuralLadderSurface.TransformStep(
                new Carrier(),
                clock,
                index,
                index + 1,
                rungs[index],
                applications[index],
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
