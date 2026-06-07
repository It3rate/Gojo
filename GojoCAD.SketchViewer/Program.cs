using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Gojo.Core;
using Gojo.Structures;
using GojoCAD.GCode;
using GojoCAD.Structures;

namespace GojoCAD.SketchViewer;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Any(arg => string.Equals(arg, "--smoke-scenes", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var scene in SketchSamples.CreateInitialScenes())
            {
                ValidateGeneratedScene(scene);
            }

            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect();

    private static void ValidateGeneratedScene(SketchScene scene)
    {
        if (scene.OffsetStudy is { } offsetStudy)
        {
            ValidateOffsetStudyScene(scene, offsetStudy);
            return;
        }

        if (scene.Structure is Relation program &&
            GCodeStructure.TryReadProgram(program, out var basis, out var programElements) &&
            GCodeStructure.TryReadBasis(basis, out _))
        {
            ValidateGCodeScene(scene, programElements);
            return;
        }

        if (scene.Structure is not Relation ladder ||
            !StructuralLadderSurface.TryReadRungPath(ladder, out var rungs) ||
            rungs.Length == 0)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not read as a structural ladder.");
        }

        if (!StructuralLadderSurface.TryReadTimeline(ladder, out var timeline) ||
            !TimeStepSequence.TryRead(timeline, out _, out _, out var steps))
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose a readable timeline.");
        }

        if (steps.Length != rungs.Length - 1)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' has a timeline/rung count mismatch.");
        }

        if (scene.Label == "Radial Shape Ladder")
        {
            ValidateRadialShapeLadder(scene, ladder, rungs);
        }

        foreach (var step in steps)
        {
            if (!StructuralLadderSurface.TryReadTransformStep(
                    step,
                    out _,
                    out _,
                    out _,
                    out _,
                    out var application,
                    out _,
                    out _) ||
                !TransformSurface.TryReadApplication(
                    application,
                    out _,
                    out _,
                    out _,
                    out _,
                    out var patch,
                    out _,
                    out _,
                    out _))
            {
                throw new InvalidOperationException($"Sketch scene '{scene.Label}' has a non-transform ladder step.");
            }

            if (!TransformSurface.TryReadPatch(
                    patch,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _))
            {
                throw new InvalidOperationException($"Sketch scene '{scene.Label}' has a transform step without a readable patch.");
            }

            if (!TransformSurface.TryCreateOperationOutputRead(
                    new Carrier(),
                    application,
                    Relation.Point(Carrier.Unknown, 0),
                    Relation.Point(Carrier.Unknown, 0),
                    out var operationOutput) ||
                !TransformSurface.TryReadOperationOutput(
                    operationOutput,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _))
            {
                throw new InvalidOperationException($"Sketch scene '{scene.Label}' has a transform step without a readable operation output.");
            }
        }

        var normalizedParameterReads = 0;
        var templateInstances = 0;
        foreach (var relation in WalkRelations(ladder))
        {
            if (GraphicParameterSurface.TryReadNormalizedValue(
                    relation,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _))
            {
                normalizedParameterReads++;
            }

            if (GraphicParameterSurface.TryReadTemplateInstance(
                    relation,
                    out _,
                    out _,
                    out _,
                    out _))
            {
                templateInstances++;
            }
        }

        if (scene.Label == "House Drawing Ladder" &&
            (normalizedParameterReads < 39 || templateInstances < 20))
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not preserve parameterized template evidence.");
        }
    }

    private static void ValidateRadialShapeLadder(SketchScene scene, Relation ladder, IReadOnlyList<Relation> rungs)
    {
        if (rungs.Count != 11)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' should expose the eleven requested shape-ladder steps.");
        }

        if (!StructuralLadderSurface.TryReadRung(
                rungs[0],
                out _,
                out _,
                out var baseResult,
                out _,
                out _,
                out _,
                out _,
                out _))
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose the base rung.");
        }

        var baseGraph = GraphicReadGraph.From(baseResult);
        var baseArcSpanSources = baseGraph.ReadsOf<SharedFrameArcRead>()
            .Select(arc => ReadSourceId(arc.Span.Source))
            .ToHashSet();
        var baseClosedBoundarySegmentSources = baseGraph.ReadsOf<ClosedSharedFrameBoundaryRead>()
            .SelectMany(boundary => boundary.Segments)
            .Select(segment => ReadSourceId(segment.Source))
            .ToHashSet();
        var baseOpenSegments = baseGraph.ReadsOf<PathGraphRead>()
            .SelectMany(graph => graph.Edges)
            .Count(edge =>
                edge.IsSharedFrame &&
                edge.Segment is SharedFrameSegmentRead &&
                !baseArcSpanSources.Contains(ReadSourceId(edge.Segment.Source)) &&
                !baseClosedBoundarySegmentSources.Contains(ReadSourceId(edge.Segment.Source)));
        var baseQuadraticCurves = baseGraph.ReadsOf<SharedFrameQuadraticCurveRead>().Count();
        var baseCubicCurves = baseGraph.ReadsOf<SharedFrameCubicCurveRead>().Count();
        var baseTransitions = baseGraph.ReadsOf<SharedFrameCurveTransitionRead>().Count();
        if (baseOpenSegments < 7 ||
            baseQuadraticCurves != 0 ||
            baseCubicCurves != 0)
        {
            throw new InvalidOperationException(
                $"Sketch scene '{scene.Label}' base rung should expose the plus/circle and H/square centerline seeds, not a curve read. " +
                $"Open={baseOpenSegments}, quadratic={baseQuadraticCurves}, cubic={baseCubicCurves}, transitions={baseTransitions}.");
        }

        var unionSurfaces = WalkRelations(ladder)
            .Count(relation => GraphicBooleanSurface.TryReadAreaUnion(relation, out _, out _, out _, out _));
        if (unionSurfaces < 2)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not preserve fill union evidence.");
        }

        var outlineDerivations = WalkRelations(ladder)
            .Count(relation => GraphicOutlineSurface.TryReadFromArea(relation, out _, out _, out _, out _));
        if (outlineDerivations < 2)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not preserve area-to-outline derivation evidence.");
        }

        var terminalCapActions = WalkRelations(ladder)
            .Where(relation => GraphicBoundaryConceptSurface.TryReadCircleCapsFromLineTipSelection(
                relation,
                out _,
                out _,
                out _,
                out _,
                out _))
            .DistinctBy(relation => relation.Id)
            .ToArray();
        if (terminalCapActions.Length < 1)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not create endpoint circles from terminal-tip cap evidence.");
        }

        var terminalCapCircles = terminalCapActions
            .SelectMany(action =>
            {
                GraphicBoundaryConceptSurface.TryReadCircleCapsFromLineTipSelection(
                    action,
                    out _,
                    out _,
                    out _,
                    out var circleFrame,
                    out _);
                return GraphicReadGraph.From(circleFrame).ReadsOf<ClosedSharedFrameBoundaryRead>();
            })
            .Count();
        if (terminalCapCircles < 8)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' should create endpoint cap circles from both open-line seed shapes.");
        }

        if (!StructuralLadderSurface.TryReadRung(
                rungs[3],
                out _,
                out _,
                out var fillResult,
                out _,
                out _,
                out _,
                out _,
                out _))
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose the separate fill rung.");
        }

        var fillReads = GraphicReadGraph.From(fillResult).ReadsOf<SharedFrameAreaRead>().Count();
        if (fillReads < 7)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose separate offset fill reads.");
        }

        var arcFillReads = GraphicReadGraph.From(fillResult)
            .ReadsOf<SharedFrameAreaRead>()
            .Count(area => area.Boundary.Arcs is { Count: > 0 });
        if (arcFillReads < 5)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not preserve circular fills as arc boundaries.");
        }

        if (!StructuralLadderSurface.TryReadRung(
                rungs[4],
                out _,
                out _,
                out var mergeResult,
                out _,
                out _,
                out _,
                out _,
                out _))
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose the merged fill as an area union.");
        }

        var mergeAreaFrames = WalkRelations(mergeResult)
            .Select(relation =>
            {
                var read = GraphicBooleanSurface.TryReadAreaUnion(relation, out var areaFrame, out _, out _, out _);
                return (read, areaFrame);
            })
            .Where(union => union.read)
            .DistinctBy(union => ReadSourceId(union.areaFrame))
            .ToArray();
        if (mergeAreaFrames.Length < 2)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose both merged fill lanes as area unions.");
        }

        var mergeParticipantCount = 0;
        var mergeAreas = new List<SharedFrameAreaRead>();
        foreach (var (_, mergeAreaFrame) in mergeAreaFrames)
        {
            if (mergeAreaFrame is not Relation mergeAreaFrameRelation ||
                !GraphicBooleanSurface.TryReadAreaFrame(mergeAreaFrameRelation, out var mergeParticipants))
            {
                continue;
            }

            mergeParticipantCount += mergeParticipants.Length;
            mergeAreas.AddRange(GraphicReadGraph.From(mergeAreaFrameRelation).ReadsOf<SharedFrameAreaRead>());
        }

        var positiveMergeAreas = mergeAreas.Count(area => !area.IsAntiArea);
        var antiMergeAreas = mergeAreas.Count(area => area.IsAntiArea);
        if (mergeParticipantCount < 10 ||
            positiveMergeAreas < 2 ||
            antiMergeAreas < 8)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose the merged fill cuts as settled area-frame structure.");
        }

        if (!StructuralLadderSurface.TryReadRung(
                rungs[5],
                out _,
                out _,
                out var outlineResult,
                out _,
                out _,
                out _,
                out _,
                out _))
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not keep the composite outline tied to the merged area union.");
        }

        var openingOutlines = WalkRelations(outlineResult)
            .Where(relation => GraphicBooleanSurface.TryReadAreaUnion(relation, out _, out _, out _, out _))
            .SelectMany(relation =>
            {
                GraphicBooleanSurface.TryReadAreaUnion(relation, out _, out var outlineWitness, out _, out _);
                return GraphicReadGraph.From(outlineWitness).ReadsOf<ClosedSharedFrameBoundaryRead>();
            })
            .Count();
        if (openingOutlines < 8)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose the eight interior union openings.");
        }

        if (!StructuralLadderSurface.TryReadRung(
                rungs[6],
                out _,
                out _,
                out var cuspResult,
                out _,
                out _,
                out _,
                out _,
                out _))
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose the selected interior cusps.");
        }

        var cuspMarkers = CountSelectionMarkerBoundaries(cuspResult);
        if (cuspMarkers < 12)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not mark the twelve interior non-tangent joins.");
        }

        if (!StructuralLadderSurface.TryReadRung(
                rungs[7],
                out _,
                out _,
                out var roundedResult,
                out _,
                out _,
                out _,
                out _,
                out _))
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose the rounded interior-corner result.");
        }

        var roundedCenterOpenings = WalkRelations(roundedResult)
            .Where(relation => GraphicBooleanSurface.TryReadAreaUnion(relation, out _, out _, out _, out _))
            .SelectMany(relation =>
            {
                GraphicBooleanSurface.TryReadAreaUnion(relation, out _, out var roundedOutlineWitness, out _, out _);
                return GraphicReadGraph.From(roundedOutlineWitness).ReadsOf<ClosedSharedFrameBoundaryRead>();
            })
            .Count(boundary => boundary.Arcs is { Count: >= 4 });
        if (roundedCenterOpenings < 4)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not replace the interior corner joins with rounded arc outlines.");
        }

        var hRoundedSplitAntiBoxes = WalkRelations(roundedResult)
            .Where(relation => GraphicBooleanSurface.TryReadAreaUnion(relation, out _, out _, out _, out _))
            .SelectMany(relation =>
            {
                GraphicBooleanSurface.TryReadAreaUnion(relation, out var roundedAreaFrame, out _, out _, out _);
                return GraphicReadGraph.From(roundedAreaFrame).ReadsOf<SharedFrameAreaRead>();
            })
            .Where(area => area.IsAntiArea &&
                area.Boundary.Arcs is { Count: >= 4 } &&
                TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
                minX >= 235 &&
                maxX <= 295 &&
                Math.Abs(minY + maxY) >= 40)
            .Select(area =>
            {
                TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY);
                return $"{minX}:{maxX}:{Math.Sign(minY + maxY)}";
            })
            .Distinct()
            .ToArray();
        if (hRoundedSplitAntiBoxes.Length < 2)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not preserve both rounded H split anti-area openings.");
        }

        if (!StructuralLadderSurface.TryReadRung(
                rungs[8],
                out _,
                out _,
                out var exteriorCuspResult,
                out _,
                out _,
                out _,
                out _,
                out _))
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose the selected exterior cusps.");
        }

        var exteriorCuspMarkers = CountSelectionMarkerBoundaries(exteriorCuspResult);
        if (exteriorCuspMarkers < 16)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not mark the sixteen exterior non-tangent joins.");
        }

        if (!StructuralLadderSurface.TryReadRung(
                rungs[9],
                out _,
                out _,
                out var exteriorRoundedResult,
                out _,
                out _,
                out _,
                out _,
                out _))
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose the exterior-rounded area union.");
        }

        var exteriorRoundedUnions = WalkRelations(exteriorRoundedResult)
            .Select(relation =>
            {
                var read = GraphicBooleanSurface.TryReadAreaUnion(relation, out var areaFrame, out var outlineWitness, out _, out _);
                return (read, areaFrame, outlineWitness);
            })
            .Where(union => union.read)
            .DistinctBy(union => ReadSourceId(union.areaFrame))
            .ToArray();
        if (exteriorRoundedUnions.Length < 2)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose both exterior-rounded lanes.");
        }

        var exteriorRoundedParticipantCount = 0;
        var exteriorRoundedAreas = new List<SharedFrameAreaRead>();
        foreach (var (_, exteriorRoundedAreaFrame, _) in exteriorRoundedUnions)
        {
            if (exteriorRoundedAreaFrame is Relation exteriorRoundedAreaFrameRelation &&
                GraphicBooleanSurface.TryReadAreaFrame(exteriorRoundedAreaFrameRelation, out var exteriorRoundedParticipants))
            {
                exteriorRoundedParticipantCount += exteriorRoundedParticipants.Length;
                exteriorRoundedAreas.AddRange(GraphicReadGraph.From(exteriorRoundedAreaFrameRelation).ReadsOf<SharedFrameAreaRead>());
            }
        }

        if (exteriorRoundedParticipantCount < 16 ||
            exteriorRoundedAreas.Count(area => !area.IsAntiArea) < 2 ||
            exteriorRoundedAreas.Count(area => area.IsAntiArea) < 8)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not preserve the exterior-rounded positive fill and hole cuts.");
        }

        var exteriorRoundedOutlines = exteriorRoundedUnions
            .SelectMany(union => GraphicReadGraph.From(union.outlineWitness).ReadsOf<ClosedSharedFrameBoundaryRead>())
            .ToArray();
        if (!exteriorRoundedOutlines.Any(boundary => boundary.Arcs is { Count: >= 24 }))
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not replace exterior cusp joins with tangent arc outline evidence.");
        }

        if (!exteriorRoundedOutlines.Any(boundary =>
                boundary.Arcs is { Count: >= 20 } &&
                TryReadSharedFrameBoundaryBounds(boundary, out var minX, out var maxX, out _, out _) &&
                minX >= 150 &&
                maxX <= 370))
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not apply selected exterior cusp rounding to the H outline.");
        }

        if (!StructuralLadderSurface.TryReadRung(
                rungs[10],
                out _,
                out _,
                out var finalColoredResult,
                out _,
                out _,
                out _,
                out _,
                out _))
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose the final colored area union.");
        }

        var finalColoredUnions = WalkRelations(finalColoredResult)
            .Select(relation =>
            {
                var read = GraphicBooleanSurface.TryReadAreaUnion(relation, out var areaFrame, out _, out _, out _);
                return (read, areaFrame);
            })
            .Where(union => union.read)
            .DistinctBy(union => ReadSourceId(union.areaFrame))
            .ToArray();
        var finalColoredParticipantCount = 0;
        var finalColoredAreas = new List<SharedFrameAreaRead>();
        foreach (var (_, finalColoredAreaFrame) in finalColoredUnions)
        {
            if (finalColoredAreaFrame is Relation finalColoredAreaFrameRelation &&
                GraphicBooleanSurface.TryReadAreaFrame(finalColoredAreaFrameRelation, out var finalColoredParticipants))
            {
                finalColoredParticipantCount += finalColoredParticipants.Length;
                finalColoredAreas.AddRange(GraphicReadGraph.From(finalColoredAreaFrameRelation).ReadsOf<SharedFrameAreaRead>());
            }
        }

        if (finalColoredUnions.Length < 2 ||
            finalColoredParticipantCount < 16 ||
            finalColoredAreas.Count(area => !area.IsAntiArea) < 2 ||
            finalColoredAreas.Count(area => area.IsAntiArea) < 8)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not keep the final holes as unfilled area cuts.");
        }
    }

    private static void ValidateGCodeScene(SketchScene scene, IReadOnlyList<IElement> programElements)
    {
        var motions = programElements.OfType<Relation>().ToArray();
        var lineCount = motions.Count(motion => GCodeStructure.TryReadLineMotion(
            motion,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _));
        var graph = GraphicReadGraph.From(scene.Structure);
        if (lineCount < 1 ||
            !graph.ReadsOf<SharedFrameSegmentRead>().Any())
        {
            throw new InvalidOperationException($"G-code scene '{scene.Label}' did not expose renderable imported motion.");
        }
    }

    private static void ValidateOffsetStudyScene(SketchScene scene, GojoDraw.GojoCadOffsetStudy offsetStudy)
    {
        if (offsetStudy.Samples.Count < 9)
        {
            throw new InvalidOperationException($"Offset scene '{scene.Label}' does not cover enough curve families.");
        }

        var requestReads = WalkRelations(scene.Structure)
            .Count(relation => GraphicOffsetSurface.TryReadRequest(relation, out _, out _, out _));
        if (requestReads < offsetStudy.Samples.Count)
        {
            throw new InvalidOperationException($"Offset scene '{scene.Label}' does not preserve offset request evidence.");
        }

        foreach (var sample in offsetStudy.Samples)
        {
            if (sample.Original.Count < 2 ||
                sample.Interior.Count != sample.Original.Count ||
                sample.Exterior.Count != sample.Original.Count)
            {
                throw new InvalidOperationException($"Offset scene '{scene.Label}' has an incomplete sample '{sample.Label}'.");
            }
        }
    }

    private static IEnumerable<Relation> WalkRelations(IElement element)
    {
        var seen = new HashSet<ulong>();
        foreach (var relation in WalkRelations(element, seen))
        {
            yield return relation;
        }
    }

    private static IEnumerable<Relation> WalkRelations(IElement element, ISet<ulong> seen)
    {
        if (element is not Relation relation)
        {
            yield break;
        }

        if (!seen.Add(relation.Id))
        {
            yield break;
        }

        yield return relation;

        foreach (var child in WalkRelations(relation.Left, seen))
        {
            yield return child;
        }

        foreach (var child in WalkRelations(relation.Right, seen))
        {
            yield return child;
        }
    }

    private static ulong ReadSourceId(IElement source) =>
        source is Relation relation ? relation.Id : 0UL;

    private static bool TryReadSharedFrameBoundaryBounds(
        ClosedSharedFrameBoundaryRead boundary,
        out long minX,
        out long maxX,
        out long minY,
        out long maxY)
    {
        var readMinX = 0L;
        var readMaxX = 0L;
        var readMinY = 0L;
        var readMaxY = 0L;
        var sawPoint = false;

        void Include(SharedFramePointRead point)
        {
            if (point.Coordinates.Count < 2)
            {
                return;
            }

            var x = point.Coordinates[0].Ticks;
            var y = point.Coordinates[1].Ticks;
            if (!sawPoint)
            {
                readMinX = readMaxX = x;
                readMinY = readMaxY = y;
                sawPoint = true;
                return;
            }

            readMinX = Math.Min(readMinX, x);
            readMaxX = Math.Max(readMaxX, x);
            readMinY = Math.Min(readMinY, y);
            readMaxY = Math.Max(readMaxY, y);
        }

        foreach (var segment in boundary.Segments)
        {
            Include(segment.Start);
            Include(segment.End);
        }

        foreach (var arc in boundary.Arcs ?? [])
        {
            Include(arc.Start);
            Include(arc.End);
        }

        minX = readMinX;
        maxX = readMaxX;
        minY = readMinY;
        maxY = readMaxY;
        return sawPoint;
    }

    private static bool TryReadOrderedFrame(IElement element, out IElement[] elements)
    {
        if (element is Relation relation &&
            relation.Right is Carrier holder &&
            relation.TryReadOrderedElementsOn(holder, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    private static bool TryReadRadialSelectionWitness(IElement element, out IElement markerWitness)
    {
        if (element is Relation selection &&
            GraphicSelectionSurface.TryReadSelection(
                selection,
                out _,
                out _,
                out _,
                out var matches,
                out _) &&
            matches is Relation matchFrame &&
            GraphicSelectionSurface.TryReadMatchFrame(matchFrame, out var matchElements))
        {
            var markerWitnesses = new List<IElement>();
            foreach (var matchElement in matchElements)
            {
                if (matchElement is Relation match &&
                    GraphicSelectionSurface.TryReadMatch(
                        match,
                        out _,
                        out _,
                        out _,
                        out var evidence,
                        out _))
                {
                    markerWitnesses.Add(ReadRadialMarkerWitness(evidence));
                }
            }

            if (markerWitnesses.Count > 0)
            {
                markerWitness = GraphicSelectionSurface.Frame(new Carrier(), [.. markerWitnesses]);
                return true;
            }
        }

        if (TryReadOrderedFrame(element, out var frameElements) &&
            frameElements.Length >= 2)
        {
            markerWitness = frameElements[1];
            return true;
        }

        markerWitness = default!;
        return false;
    }

    private static int CountSelectionMarkerBoundaries(IElement element)
    {
        var markerWitnesses = new List<IElement>();
        foreach (var selection in WalkRelations(element)
            .Where(relation => GraphicSelectionSurface.TryReadSelection(relation, out _, out _, out _, out _, out _))
            .DistinctBy(relation => relation.Id))
        {
            GraphicSelectionSurface.TryReadSelection(
                selection,
                out _,
                out _,
                out _,
                out var matches,
                out _);
            if (matches is not Relation matchFrame ||
                !GraphicSelectionSurface.TryReadMatchFrame(matchFrame, out var matchElements))
            {
                continue;
            }

            foreach (var matchElement in matchElements)
            {
                if (matchElement is Relation match &&
                    GraphicSelectionSurface.TryReadMatch(
                        match,
                        out _,
                        out _,
                        out _,
                        out var evidence,
                        out _))
                {
                    markerWitnesses.Add(ReadRadialMarkerWitness(evidence));
                }
            }
        }

        return markerWitnesses
            .SelectMany(witness => GraphicReadGraph.From(witness).ReadsOf<ClosedSharedFrameBoundaryRead>())
            .Count();
    }

    private static IElement ReadRadialMarkerWitness(IElement evidence)
    {
        if (evidence is Relation patternEvidence &&
            GraphicSelectionSurface.TryReadPatternEvidence(
                patternEvidence,
                out _,
                out var appliedPattern,
                out _,
                out _) &&
            TryReadOrderedFrame(appliedPattern, out var appliedElements) &&
            appliedElements.Length >= 2)
        {
            return appliedElements[1];
        }

        return evidence;
    }
}

internal sealed class App : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

internal sealed class MainWindow : Window
{
    public MainWindow()
    {
        Title = "GojoCAD SketchViewer";
        Width = 1180;
        Height = 760;
        MinWidth = 860;
        MinHeight = 560;

        var document = SketchDocument.CreateInitial();
        var canvas = new SketchCanvas
        {
            Document = document
        };
        var sceneList = new ListBox
        {
            ItemsSource = document.Scenes.Select(scene => scene.Label).ToArray(),
            SelectedIndex = document.SelectedSceneIndex,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0)
        };
        var nextButton = CreateButton("Next");
        var refreshButton = CreateButton("Refresh");
        var pointsButton = CreateButton(document.ShowConstructionPoints ? "Points On" : "Points Off");
        var ghostButton = CreateButton(document.ShowPreviousRungs ? "Ghost On" : "Ghost Off");
        var rungLabel = new TextBlock
        {
            FontSize = 12.5,
            Foreground = new SolidColorBrush(Color.FromRgb(57, 64, 74)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 8)
        };
        var rungSlider = new LadderSliderControl
        {
            Maximum = Math.Max(0, document.SelectedScene.RungCount - 1),
            Value = document.CurrentRungIndex,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(6, 0, 0, 0)
        };

        var isUpdatingSceneList = false;
        var isUpdatingRungSlider = false;
        void UpdateRungControls(SketchDocument next)
        {
            isUpdatingRungSlider = true;
            try
            {
                rungSlider.SetRange(Math.Max(0, next.SelectedScene.RungCount - 1), next.CurrentRungIndex);
                rungLabel.Text = $"{next.CurrentRungIndex + 1}/{next.SelectedScene.RungCount}: {next.SelectedScene.ReadRungLabel(next.CurrentRungIndex)}";
            }
            finally
            {
                isUpdatingRungSlider = false;
            }
        }

        void SetDocument(SketchDocument next)
        {
            document = next;
            canvas.Document = next;
            pointsButton.Content = next.ShowConstructionPoints ? "Points On" : "Points Off";
            ghostButton.Content = next.ShowPreviousRungs ? "Ghost On" : "Ghost Off";
            UpdateRungControls(next);
            isUpdatingSceneList = true;
            try
            {
                sceneList.ItemsSource = next.Scenes.Select(scene => scene.Label).ToArray();
                sceneList.SelectedIndex = next.SelectedSceneIndex;
            }
            finally
            {
                isUpdatingSceneList = false;
            }
        }

        sceneList.SelectionChanged += (_, _) =>
        {
            if (isUpdatingSceneList)
            {
                return;
            }

            if (sceneList.SelectedIndex >= 0 && sceneList.SelectedIndex < document.Scenes.Count)
            {
                SetDocument(document.Select(sceneList.SelectedIndex));
            }
        };
        nextButton.Click += (_, _) => SetDocument(document.Select((document.SelectedSceneIndex + 1) % document.Scenes.Count));
        refreshButton.Click += (_, _) => SetDocument(document.Resample());
        pointsButton.Click += (_, _) => SetDocument(document.ToggleConstructionPoints());
        ghostButton.Click += (_, _) => SetDocument(document.TogglePreviousRungs());
        rungSlider.ValueChanged += nextRung =>
        {
            if (!isUpdatingRungSlider)
            {
                SetDocument(document.SelectRung(nextRung));
            }
        };

        var buttonRowA = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };
        buttonRowA.Children.Add(nextButton);
        buttonRowA.Children.Add(refreshButton);
        var buttonRowB = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };
        buttonRowB.Children.Add(pointsButton);
        buttonRowB.Children.Add(ghostButton);
        var buttonRows = new StackPanel
        {
            Spacing = 6,
            Margin = new Thickness(0, 14, 0, 0)
        };
        buttonRows.Children.Add(buttonRowA);
        buttonRows.Children.Add(buttonRowB);
        var rungPanel = new StackPanel
        {
            Margin = new Thickness(0, 20, 0, 14)
        };
        rungPanel.Children.Add(new TextBlock
        {
            Text = "Drawing Ladder",
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(31, 37, 45))
        });
        rungPanel.Children.Add(rungLabel);
        rungPanel.Children.Add(rungSlider);
        UpdateRungControls(document);

        var header = new TextBlock
        {
            Text = "Sketch Scenes",
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(31, 37, 45)),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var sideLayout = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };
        Grid.SetRow(header, 0);
        Grid.SetRow(rungPanel, 1);
        Grid.SetRow(sceneList, 2);
        Grid.SetRow(buttonRows, 3);
        sideLayout.Children.Add(header);
        sideLayout.Children.Add(rungPanel);
        sideLayout.Children.Add(sceneList);
        sideLayout.Children.Add(buttonRows);

        var side = new Border
        {
            Width = 260,
            Background = new SolidColorBrush(Color.FromRgb(246, 245, 240)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(65, 91, 97, 107)),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Padding = new Thickness(16, 16, 12, 16),
            Child = sideLayout
        };
        var root = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            }
        };
        root.Children.Add(side);
        Grid.SetColumn(canvas, 1);
        root.Children.Add(canvas);
        Content = root;
    }

    private static Button CreateButton(string text) =>
        new()
        {
            Content = text,
            Padding = new Thickness(8, 3),
            MinWidth = 66,
            MinHeight = 26,
            FontSize = 12,
            Background = new SolidColorBrush(Color.FromArgb(235, 255, 254, 250)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(120, 82, 88, 96)),
            BorderThickness = new Thickness(1)
        };
}
