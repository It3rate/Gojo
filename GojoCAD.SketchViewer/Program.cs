using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Gojo.ConceptSurface;
using Gojo.Core;
using Gojo.Structures;
using GojoCAD.GCode;
using GojoCAD.Structures;
using GojoDraw;
using SkiaSharp;

namespace GojoCAD.SketchViewer;

internal static class Program
{
    private static readonly SKColor RadialMergedFill = new(48, 88, 142, 158);
    private static readonly SKColor RadialMergedAntiAreaFill = new(246, 199, 199);

    private static readonly SKColor[] RadialPartitionPalette =
    [
        new(229, 95, 91),
        new(79, 142, 210),
        new(93, 166, 111),
        new(218, 163, 75),
        new(166, 111, 194),
        new(223, 127, 78),
        new(92, 178, 176),
        new(199, 89, 132),
        new(132, 165, 74),
        new(109, 119, 207),
        new(217, 115, 166),
        new(111, 184, 92),
        new(198, 139, 210),
        new(232, 139, 95),
        new(79, 163, 137),
        new(186, 104, 86),
        new(102, 151, 221),
        new(225, 181, 91),
        new(137, 112, 198),
        new(99, 181, 151),
        new(210, 102, 116),
        new(143, 174, 91),
        new(221, 127, 189),
        new(105, 132, 183),
        new(184, 147, 72),
        new(82, 170, 205),
        new(221, 101, 83),
        new(154, 126, 205),
        new(112, 174, 121),
        new(231, 155, 118),
        new(91, 154, 176),
        new(201, 118, 157),
        new(167, 164, 86)
    ];

    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Any(arg => string.Equals(arg, "--smoke-scenes", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var radialComparisonVariant in new[] { 0, 1, 2 })
            foreach (var scene in SketchSamples.CreateInitialScenes(radialComparisonVariant))
            {
                ValidateGeneratedScene(scene);
            }

            return;
        }

        if (args.Any(arg => string.Equals(arg, "--smoke-render-scenes", StringComparison.OrdinalIgnoreCase)))
        {
            SmokeRenderGeneratedScenes(args);
            return;
        }

        if (args.Any(arg => string.Equals(arg, "--inspect-radial-partitions", StringComparison.OrdinalIgnoreCase)))
        {
            InspectRadialPartitions(args);
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect();

    private static void SmokeRenderGeneratedScenes(string[] args)
    {
        var renderer = new GojoCadSkiaSceneRenderer();
        using var bitmap = new SKBitmap(1200, 900);
        using var canvas = new SKCanvas(bitmap);
        var viewport = new SKRect(42f, 78f, 1158f, 852f);
        var rendered = 0;
        var requestedVariant = ReadIntArg(args, "--variant=");
        var requestedRung = ReadIntArg(args, "--rung=");
        var requestedScene = ReadStringArg(args, "--scene=");
        var savePath = ReadStringArg(args, "--save=");
        var verbosePartitions = args.Any(arg => string.Equals(arg, "--verbose", StringComparison.OrdinalIgnoreCase));
        int[] variants = requestedVariant is { } variant ? [variant] : [0, 1, 2];

        foreach (var radialComparisonVariant in variants)
        foreach (var scene in SketchSamples.CreateInitialScenes(radialComparisonVariant))
        {
            if (scene.OffsetStudy is not null)
            {
                continue;
            }

            if (requestedScene is { Length: > 0 } sceneFilter &&
                !scene.Label.Contains(sceneFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var rungs = scene.ReadRungs();
            if (rungs.Count == 0)
            {
                canvas.Clear(SKColors.White);
                var stopwatch = Stopwatch.StartNew();
                renderer.Draw(canvas, scene.Structure, viewport, scene.ProjectionSeed, scene.ModelBounds);
                stopwatch.Stop();
                Console.WriteLine($"{scene.Label} rendered in {stopwatch.ElapsedMilliseconds} ms.");
                rendered++;
                continue;
            }

            for (var index = 0; index < rungs.Count; index++)
            {
                if (requestedRung is { } rungFilter &&
                    rungFilter != index)
                {
                    continue;
                }

                if (!StructuralLadderSurface.TryReadRung(
                        rungs[index],
                        out _,
                        out _,
                        out var result,
                        out _,
                        out _,
                        out _,
                        out _,
                        out _))
                {
                    continue;
                }

                using var paints = new GojoCadSkiaPaints
                {
                    AreaFillPalette = scene.Label == "Radial Shape Ladder" && index == 3
                        ? RadialPartitionPalette
                        : null,
                    HideAreaBoundary = scene.Label == "Radial Shape Ladder" && (index == 3 || index == 4),
                    HideSharedFrameGeometry = scene.Label == "Radial Shape Ladder" && (index == 3 || index == 4),
                    DrawSharedFrameAreaPartitions = scene.Label == "Radial Shape Ladder" && index == 3,
                    DrawAreaUnionFromSharedFrameAreaPartitions = scene.Label == "Radial Shape Ladder" && index == 4,
                    DrawAreaUnionAntiAreas = scene.Label == "Radial Shape Ladder" && index >= 4 && index < 10,
                    DrawAreaUnionSettlementPartitions = scene.Label == "Radial Shape Ladder" && index >= 4 && index < 10
                };
                if (scene.Label == "Radial Shape Ladder" && index == 3)
                {
                    paints.AntiAreaFill.Color = new SKColor(34, 215, 230);
                }
                else if (scene.Label == "Radial Shape Ladder" && index == 4)
                {
                    paints.AreaFill.Color = RadialMergedFill;
                    paints.AntiAreaFill.Color = RadialMergedAntiAreaFill;
                }

                canvas.Clear(SKColors.White);
                var stopwatch = Stopwatch.StartNew();
                var hitAreas = new List<GojoCadSkiaHitArea>();
                var drawn = renderer.Draw(canvas, result, viewport, scene.ProjectionSeed, scene.ModelBounds, paints, hitAreas);
                stopwatch.Stop();
                var partitionReport = scene.Label == "Radial Shape Ladder" && index == 3
                    ? RadialPartitionReport(hitAreas, viewport, scene.ModelBounds, verbosePartitions)
                    : string.Empty;
                Console.WriteLine($"{scene.Label} variant {radialComparisonVariant} rung {index} rendered {drawn} elements{partitionReport} in {stopwatch.ElapsedMilliseconds} ms.");
                if (savePath is { Length: > 0 })
                {
                    SaveSmokeBitmap(bitmap, savePath);
                }

                rendered++;
            }
        }

        Console.WriteLine($"Rendered {rendered} generated scene/rung surfaces.");
    }

    private static void SaveSmokeBitmap(SKBitmap bitmap, string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write);
        data.SaveTo(stream);
    }

    private static string RadialPartitionReport(
        IReadOnlyCollection<GojoCadSkiaHitArea> hitAreas,
        SKRect viewport,
        SKRect? modelBounds,
        bool verbose)
    {
        var filled = hitAreas.Where(hit => hit.Text == "frame area partition").ToArray();
        var empty = hitAreas.Where(hit => hit.Text == "frame empty partition").ToArray();
        var splitX = modelBounds is { } bounds
            ? ProjectModelX(170f, bounds, viewport)
            : viewport.MidX;
        if (verbose &&
            modelBounds is { } verboseBounds)
        {
            Console.WriteLine("  filled:");
            foreach (var hit in filled.OrderBy(hit => UnprojectModelMidX(hit.Bounds, verboseBounds, viewport)).ThenBy(hit => hit.Bounds.MidY))
            {
                Console.WriteLine($"    {PartitionBoundsLabel(hit.Bounds, verboseBounds, viewport)}");
            }

            Console.WriteLine("  empty:");
            foreach (var hit in empty.OrderBy(hit => UnprojectModelMidX(hit.Bounds, verboseBounds, viewport)).ThenBy(hit => hit.Bounds.MidY))
            {
                Console.WriteLine($"    {PartitionBoundsLabel(hit.Bounds, verboseBounds, viewport)}");
            }
        }

        return
            $" partitions +{filled.Length} empty {empty.Length}" +
            $" left +{filled.Count(hit => hit.Bounds.MidX < splitX)} empty {empty.Count(hit => hit.Bounds.MidX < splitX)}" +
            $" right +{filled.Count(hit => hit.Bounds.MidX >= splitX)} empty {empty.Count(hit => hit.Bounds.MidX >= splitX)}";
    }

    private static string PartitionBoundsLabel(SKRect screenBounds, SKRect modelBounds, SKRect viewport)
    {
        var transform = ReadSmokeTransform(modelBounds, viewport);
        var left = (screenBounds.Left - transform.Offset.X) / transform.Scale;
        var right = (screenBounds.Right - transform.Offset.X) / transform.Scale;
        var top = (screenBounds.Top - transform.Offset.Y) / transform.Scale;
        var bottom = (screenBounds.Bottom - transform.Offset.Y) / transform.Scale;
        return $"x={left,7:0.0}:{right,7:0.0} y={top,7:0.0}:{bottom,7:0.0}";
    }

    private static float UnprojectModelMidX(SKRect screenBounds, SKRect modelBounds, SKRect viewport)
    {
        var transform = ReadSmokeTransform(modelBounds, viewport);
        return (screenBounds.MidX - transform.Offset.X) / transform.Scale;
    }

    private static float ProjectModelX(float modelX, SKRect modelBounds, SKRect viewport)
    {
        var transform = ReadSmokeTransform(modelBounds, viewport);
        return transform.Offset.X + modelX * transform.Scale;
    }

    private static (float Scale, SKPoint Offset) ReadSmokeTransform(SKRect modelBounds, SKRect viewport)
    {
        var margin = MathF.Min(36f, MathF.Min(viewport.Width, viewport.Height) * 0.12f);
        var innerWidth = Math.Max(1f, viewport.Width - margin * 2f);
        var innerHeight = Math.Max(1f, viewport.Height - margin * 2f);
        var scale = MathF.Min(innerWidth / modelBounds.Width, innerHeight / modelBounds.Height);
        var modelCenterX = modelBounds.Left + modelBounds.Width * 0.5f;
        var modelCenterY = modelBounds.Top + modelBounds.Height * 0.5f;
        var viewportCenterX = viewport.Left + viewport.Width * 0.5f;
        var viewportCenterY = viewport.Top + viewport.Height * 0.5f;
        return (scale, new SKPoint(
            viewportCenterX - modelCenterX * scale,
            viewportCenterY - modelCenterY * scale));
    }

    private static void InspectRadialPartitions(string[] args)
    {
        var requestedVariant = ReadIntArg(args, "--variant=") ?? 0;
        var requestedRung = ReadIntArg(args, "--rung=") ?? 4;
        var verbose = args.Any(arg => string.Equals(arg, "--verbose", StringComparison.OrdinalIgnoreCase));
        var scene = SketchSamples.CreateInitialScenes(requestedVariant)
            .First(scene => scene.Label == "Radial Shape Ladder");
        var rungs = scene.ReadRungs();
        if (!StructuralLadderSurface.TryReadRung(
                rungs[requestedRung],
                out _,
                out _,
                out var result,
                out _,
                out _,
                out _,
                out _,
                out _))
        {
            throw new InvalidOperationException($"Radial rung {requestedRung} did not expose a result.");
        }

        var rungAreas = GraphicReadGraph.From(result)
            .ReadsOf<SharedFrameAreaRead>()
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        Console.WriteLine(
            $"variant={requestedVariant} rung={requestedRung} areas +{rungAreas.Count(area => !area.IsAntiArea)} empty {rungAreas.Count(area => area.IsAntiArea)}");

        var unionReads = WalkRelations(result)
            .Where(relation => GraphicAreaParticipationSurface.TryReadAreaUnion(relation, out _, out _, out _, out _))
            .DistinctBy(ReadSourceId)
            .ToArray();
        Console.WriteLine($"variant={requestedVariant} rung={requestedRung} unions={unionReads.Length}");
        for (var unionIndex = 0; unionIndex < unionReads.Length; unionIndex++)
        {
            var union = unionReads[unionIndex];
            GraphicAreaParticipationSurface.TryReadAreaUnion(union, out var areaFrame, out var outline, out var settlement, out _);
            var partitionLayer = settlement;
            if (settlement is Relation settlementRelation &&
                ParticipationSettlementSurface.TryRead(settlementRelation, out var settlementRead))
            {
                partitionLayer = settlementRead.Partitions;
                if (partitionLayer is Relation partitionFrame &&
                    partitionFrame.Right is Carrier partitionHolder &&
                    partitionFrame.TryReadOrderedElementsOn(partitionHolder, out var layers) &&
                    layers.Length > 0)
                {
                    partitionLayer = layers[0];
                }
            }

            var directElements = partitionLayer is Relation directFrame &&
                directFrame.Right is Carrier directHolder &&
                directFrame.TryReadOrderedElementsOn(directHolder, out var ordered)
                    ? ordered
                    : [partitionLayer];
            var directCircleCount = directElements.Count(element => GraphicReadGraph.From(element).ReadsOf<SharedFrameCircleRead>().Any());
            var directArcSectionCount = directElements.Count(element => GraphicReadGraph.From(element).ReadsOf<SharedFrameArcSectionRead>().Any());
            var directArcCount = directElements.Count(element => GraphicReadGraph.From(element).ReadsOf<SharedFrameArcRead>().Any());
            var directSegmentCount = directElements.Count(element => GraphicReadGraph.From(element).ReadsOf<SharedFrameSegmentRead>().Any());
            var graph = GraphicReadGraph.From(partitionLayer);
            var circles = graph.ReadsOf<SharedFrameCircleRead>()
                .DistinctBy(circle => ReadSourceId(circle.Source))
                .ToArray();
            var sections = graph.ReadsOf<SharedFrameArcSectionRead>()
                .DistinctBy(section => ReadSourceId(section.Source))
                .ToArray();
            var arcs = graph.ReadsOf<SharedFrameArcRead>()
                .DistinctBy(arc => ReadSourceId(arc.Source))
                .ToArray();
            var segments = graph.ReadsOf<SharedFrameSegmentRead>()
                .DistinctBy(segment => ReadSourceId(segment.Source))
                .ToArray();
            var points = graph.ReadsOf<SharedFramePointRead>()
                .Select(PointSignature)
                .Distinct()
                .Count();
            var areas = GraphicReadGraph.From(areaFrame)
                .ReadsOf<SharedFrameAreaRead>()
                .DistinctBy(area => ReadSourceId(area.Source))
                .ToArray();
            var outlines = GraphicReadGraph.From(outline)
                .ReadsOf<ClosedSharedFrameBoundaryRead>()
                .DistinctBy(boundary => ReadSourceId(boundary.Source))
                .ToArray();
            Console.WriteLine(
                $"  union[{unionIndex}] partitions circles={circles.Length} arcSections={sections.Length} arcs={arcs.Length} segments={segments.Length} points={points}; " +
                $"direct={directElements.Length} c={directCircleCount} as={directArcSectionCount} a={directArcCount} s={directSegmentCount}; " +
                $"areas +{areas.Count(area => !area.IsAntiArea)} -{areas.Count(area => area.IsAntiArea)} outlines={outlines.Length}");
            if (verbose)
            {
                foreach (var antiArea in areas.Where(area => area.IsAntiArea))
                {
                    if (TryReadSharedFrameBoundaryBounds(antiArea.Boundary, out var minX, out var maxX, out var minY, out var maxY))
                    {
                        Console.WriteLine($"    anti {minX}:{maxX}:{minY}:{maxY}");
                    }
                }

                for (var elementIndex = 0; elementIndex < directElements.Length; elementIndex++)
                {
                    var elementGraph = GraphicReadGraph.From(directElements[elementIndex]);
                    var label = elementGraph.ReadsOf<SharedFrameCircleRead>().Any()
                        ? "circle"
                        : elementGraph.ReadsOf<SharedFrameArcSectionRead>().Any()
                            ? "arc-section"
                            : "segment";
                    var pointList = elementGraph.ReadsOf<SharedFramePointRead>()
                        .Select(PointSignature)
                        .Distinct()
                        .ToArray();
                    Console.WriteLine($"    {elementIndex:00} {label} {string.Join(" ", pointList)}");
                }
            }
        }
    }

    private static int? ReadIntArg(IEnumerable<string> args, string prefix) =>
        args.Select(arg => arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? arg[prefix.Length..]
                : null)
            .FirstOrDefault(value => value is not null && int.TryParse(value, out _)) is { } text
            ? int.Parse(text)
            : null;

    private static string? ReadStringArg(IEnumerable<string> args, string prefix) =>
        args.Select(arg => arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? arg[prefix.Length..]
                : null)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

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
        var comparisonPairMode = !scene.Caption.Contains("H/square", StringComparison.OrdinalIgnoreCase);
        var expectedUnionLanes = comparisonPairMode ? 1 : 2;
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
        if (baseOpenSegments < 2 ||
            baseQuadraticCurves != 0 ||
            baseCubicCurves != 0)
        {
            throw new InvalidOperationException(
                $"Sketch scene '{scene.Label}' base rung should expose centerline seeds with terminal structure, not unsupported Bezier curve reads. " +
                $"Open={baseOpenSegments}, quadratic={baseQuadraticCurves}, cubic={baseCubicCurves}, transitions={baseTransitions}.");
        }

        var unionSurfaces = WalkRelations(ladder)
            .Count(relation => GraphicAreaParticipationSurface.TryReadAreaUnion(relation, out _, out _, out _, out _));
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
                rungs[2],
                out _,
                out _,
                out var offsetResult,
                out _,
                out _,
                out _,
                out _,
                out _))
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose the offset boundary rung.");
        }

        if (scene.Caption.Contains("bug", StringComparison.OrdinalIgnoreCase))
        {
            var offsetGraph = GraphicReadGraph.From(offsetResult);
            var closedArcSources = offsetGraph.ReadsOf<ClosedSharedFrameBoundaryRead>()
                .SelectMany(boundary => boundary.Arcs ?? [])
                .Select(arc => ReadSourceId(arc.Source))
                .ToHashSet();
            var openOffsetArcs = offsetGraph.ReadsOf<SharedFrameArcRead>()
                .Count(arc => !closedArcSources.Contains(ReadSourceId(arc.Source)));
            if (openOffsetArcs < 4)
            {
                throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not keep curved-arm offset arcs in the comparison lane.");
            }
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

        var fillAreas = GraphicReadGraph.From(fillResult)
            .ReadsOf<SharedFrameAreaRead>()
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();

        if (fillAreas.Count(area => !area.IsAntiArea) < expectedUnionLanes)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose separate positive offset fill loops.");
        }

        var curvedFillReads = fillAreas
            .Count(area =>
                !area.IsAntiArea &&
                (area.Boundary.Arcs is { Count: > 0 } ||
                    area.Boundary.Circles is { Count: > 0 }));
        if (curvedFillReads < expectedUnionLanes)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not preserve circular fills as curved boundaries.");
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
                var read = GraphicAreaParticipationSurface.TryReadAreaUnion(relation, out var areaFrame, out _, out _, out _);
                return (read, areaFrame);
            })
            .Where(union => union.read)
            .DistinctBy(union => ReadSourceId(union.areaFrame))
            .ToArray();
        if (mergeAreaFrames.Length < expectedUnionLanes)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose the expected merged fill lane area unions.");
        }

        var mergeParticipantElements = new List<IElement>();
        var mergeAreas = new List<SharedFrameAreaRead>();
        foreach (var (_, mergeAreaFrame) in mergeAreaFrames)
        {
            if (mergeAreaFrame is not Relation mergeAreaFrameRelation ||
                !GraphicAreaParticipationSurface.TryReadAreaFrame(mergeAreaFrameRelation, out var frameParticipants))
            {
                continue;
            }

            mergeParticipantElements.AddRange(frameParticipants);
            mergeAreas.AddRange(GraphicReadGraph.From(mergeAreaFrameRelation).ReadsOf<SharedFrameAreaRead>());
        }

        var mergeOutlineBoundaries = WalkRelations(mergeResult)
            .Where(relation => GraphicAreaParticipationSurface.TryReadAreaUnion(relation, out _, out _, out _, out _))
            .SelectMany(relation =>
            {
                GraphicAreaParticipationSurface.TryReadAreaUnion(relation, out _, out var outlineWitness, out _, out _);
                return GraphicReadGraph.From(outlineWitness).ReadsOf<ClosedSharedFrameBoundaryRead>();
            })
            .ToArray();

        var positiveMergeAreas = mergeAreas.Count(area => !area.IsAntiArea);
        var antiMergeAreas = mergeAreas.Count(area => area.IsAntiArea);
        if (positiveMergeAreas < expectedUnionLanes)
        {
            var positiveBounds = mergeAreas
                .Where(area => !area.IsAntiArea &&
                    TryReadSharedFrameBoundaryBounds(area.Boundary, out _, out _, out _, out _))
                .Select(area =>
                {
                    TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY);
                    return $"{minX}:{maxX}:{minY}:{maxY}";
                })
                .Distinct()
                .ToArray();
            var antiBounds = mergeAreas
                .Where(area => area.IsAntiArea &&
                    TryReadSharedFrameBoundaryBounds(area.Boundary, out _, out _, out _, out _))
                .Select(area =>
                {
                    TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY);
                    return $"{minX}:{maxX}:{minY}:{maxY}";
                })
                .Distinct()
                .ToArray();
            throw new InvalidOperationException(
                $"Sketch scene '{scene.Label}' did not expose the merged fill cuts as settled area-frame structure. " +
                $"Positive={positiveMergeAreas} [{string.Join(", ", positiveBounds)}], " +
                $"anti={antiMergeAreas} [{string.Join(", ", antiBounds)}], comparison={comparisonPairMode}.");
        }

        if (scene.Caption.Contains("YT", StringComparison.OrdinalIgnoreCase))
        {
            var yMergedPositive = mergeAreas.Any(area =>
                !area.IsAntiArea &&
                TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
                minX <= 140 &&
                maxX >= 390 &&
                minY <= -100 &&
                maxY >= 120);
            if (!yMergedPositive)
            {
                throw new InvalidOperationException(
                    $"Sketch scene '{scene.Label}' did not settle the YT fills into a merged positive component.");
            }
        }

        if (scene.Caption.Contains("bug", StringComparison.OrdinalIgnoreCase))
        {
            var curvedMergedPositive = mergeAreas.Any(area =>
                !area.IsAntiArea &&
                TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
                minX <= -170 &&
                maxX >= 30 &&
                minY <= -110 &&
                maxY >= 110);
            var curvedResiduePositive =
                mergeAreas.Any(area =>
                    !area.IsAntiArea &&
                    TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
                    minX <= -150 &&
                    maxX >= 10 &&
                    minY >= 80 &&
                    maxY >= 90) &&
                mergeAreas.Any(area =>
                    !area.IsAntiArea &&
                    TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
                    minX <= -180 &&
                    maxX >= 40 &&
                    minY <= -78 &&
                    maxY <= -70) &&
                mergeAreas.Any(area =>
                    !area.IsAntiArea &&
                    TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
                    minX <= -105 &&
                    maxX >= -35 &&
                    minY <= -70 &&
                    maxY >= 45);
            var curvedSideBandsPositive =
                mergeAreas.Count(area =>
                    !area.IsAntiArea &&
                    TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
                    minX >= -115 &&
                    maxX <= -25 &&
                    minY >= -80 &&
                    maxY <= 60) >= 4;
            var curvedAntiOpenings = mergeOutlineBoundaries
                .Where(boundary =>
                    TryReadSharedFrameBoundaryBounds(boundary, out var minX, out var maxX, out var minY, out var maxY) &&
                    minX >= -105 &&
                    maxX <= -35 &&
                    minY >= -70 &&
                    maxY <= 40)
                .ToArray();
            var curvedAntiOpening = curvedAntiOpenings.Any(area =>
            {
                TryReadSharedFrameBoundaryBounds(area, out _, out _, out var minY, out var maxY);
                return minY <= -48 && maxY >= 28;
            }) ||
                curvedAntiOpenings.Length >= 2 &&
                curvedAntiOpenings.Any(area =>
                {
                    TryReadSharedFrameBoundaryBounds(area, out _, out _, out var minY, out _);
                    return minY < -20;
                }) &&
                curvedAntiOpenings.Any(area =>
                {
                    TryReadSharedFrameBoundaryBounds(area, out _, out _, out var minY, out _);
                    return minY >= 0;
                });
            var curvedPositive = curvedMergedPositive || curvedResiduePositive || curvedSideBandsPositive;
            if (!curvedPositive ||
                !curvedAntiOpening)
            {
                var positiveBounds = mergeAreas
                    .Where(area => !area.IsAntiArea &&
                        TryReadSharedFrameBoundaryBounds(area.Boundary, out _, out _, out _, out _))
                    .Select(area =>
                    {
                        TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY);
                        return $"{minX}:{maxX}:{minY}:{maxY}";
                    })
                    .Distinct()
                    .ToArray();
                var antiBounds = mergeAreas
                    .Where(area => area.IsAntiArea &&
                        TryReadSharedFrameBoundaryBounds(area.Boundary, out _, out _, out _, out _))
                    .Select(area =>
                    {
                        TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY);
                        return $"{minX}:{maxX}:{minY}:{maxY}";
                    })
                    .Distinct()
                    .ToArray();
                throw new InvalidOperationException(
                    $"Sketch scene '{scene.Label}' did not preserve the curved-arm rectangle as positive frame plus anti opening. " +
                    $"Curved positive={curvedPositive}, merged={curvedMergedPositive}, residue={curvedResiduePositive}, sideBands={curvedSideBandsPositive}, opening={curvedAntiOpening}. " +
                    $"Positive=[{string.Join(", ", positiveBounds)}], anti=[{string.Join(", ", antiBounds)}].");
            }

            var localOpeningParticipants = mergeParticipantElements
                .Select(participant =>
                {
                    var areas = GraphicReadGraph.From(participant)
                        .ReadsOf<SharedFrameAreaRead>()
                        .DistinctBy(area => ReadSourceId(area.Source))
                        .ToArray();
                    return (
                        Participant: participant,
                        Positive: areas.Where(area => !area.IsAntiArea).ToArray(),
                        Anti: areas.Where(area => area.IsAntiArea).ToArray());
                })
                .Where(participant => participant.Positive.Length > 0 && participant.Anti.Length > 0)
                .ToArray();
            var curvedCenterFrameIsLocal = localOpeningParticipants.Any(participant =>
                participant.Positive.Any(area =>
                    area.Boundary.Arcs is not { Count: > 0 } &&
                    TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
                    minX <= -105 &&
                    maxX >= -35 &&
                    minY <= -70 &&
                    maxY >= 45) &&
                participant.Anti.Any(area =>
                    area.Boundary.Arcs is not { Count: > 0 } &&
                    TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
                    minX >= -95 &&
                    maxX <= -45 &&
                    minY <= -55 &&
                    maxY >= 30));
            var curvedCenterOpeningPreserved = curvedCenterFrameIsLocal || curvedAntiOpening;
            var yCenterCutsAreSettled = mergeOutlineBoundaries.Count(boundary =>
                TryReadSharedFrameBoundaryBounds(boundary, out var minX, out var maxX, out var minY, out var maxY) &&
                minX >= 210 &&
                maxX <= 320 &&
                minY >= -5 &&
                maxY <= 105) >= 3;
            var yCenterNoFillRingBands = fillAreas.Count(area =>
                !area.IsAntiArea &&
                area.Boundary.Arcs is { Count: > 0 } &&
                TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
                minX >= 190 &&
                maxX <= 340 &&
                minY >= -25 &&
                maxY <= 125) >= 4;
            if (!curvedCenterOpeningPreserved ||
                !(yCenterCutsAreSettled || yCenterNoFillRingBands))
            {
                var yAntiBounds = mergeOutlineBoundaries
                    .Where(boundary =>
                        TryReadSharedFrameBoundaryBounds(boundary, out var minX, out var maxX, out var minY, out var maxY) &&
                        minX >= 190 &&
                        maxX <= 340 &&
                        minY >= -55 &&
                        maxY <= 95)
                    .Select(boundary =>
                    {
                        TryReadSharedFrameBoundaryBounds(boundary, out var minX, out var maxX, out var minY, out var maxY);
                        return $"{minX}:{maxX}:{minY}:{maxY}";
                    })
                    .Distinct()
                    .ToArray();
                var yFillRingBounds = fillAreas
                    .Where(area => !area.IsAntiArea &&
                        area.Boundary.Arcs is { Count: > 0 } &&
                        TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
                        minX >= 190 &&
                        maxX <= 340 &&
                        minY >= -55 &&
                        maxY <= 95)
                    .Select(area =>
                    {
                        TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY);
                        return $"{minX}:{maxX}:{minY}:{maxY}";
                    })
                    .Distinct()
                    .ToArray();
                throw new InvalidOperationException(
                    $"Sketch scene '{scene.Label}' lifted a comparison centre opening out of its local frame participant before merge. " +
                    $"Curved local={curvedCenterFrameIsLocal}, curved preserved={curvedCenterOpeningPreserved}, " +
                    $"Y settled cuts={yCenterCutsAreSettled} [{string.Join(", ", yAntiBounds)}], " +
                    $"Y ring bands={yCenterNoFillRingBands} [{string.Join(", ", yFillRingBounds)}].");
            }
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
            .Where(relation => GraphicAreaParticipationSurface.TryReadAreaUnion(relation, out _, out _, out _, out _))
            .SelectMany(relation =>
            {
                GraphicAreaParticipationSurface.TryReadAreaUnion(relation, out _, out var outlineWitness, out _, out _);
                return GraphicReadGraph.From(outlineWitness).ReadsOf<ClosedSharedFrameBoundaryRead>();
            })
            .Count();
        if (openingOutlines < 8)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose the eight interior union openings.");
        }

        if (scene.Caption.Contains("crescent", StringComparison.OrdinalIgnoreCase))
        {
            var crescentLocalCenterOpenings = mergeParticipantElements
                .Select(participant =>
                {
                    var areas = GraphicReadGraph.From(participant)
                        .ReadsOf<SharedFrameAreaRead>()
                        .DistinctBy(area => ReadSourceId(area.Source))
                        .ToArray();
                    return (
                        Positive: areas.Where(area => !area.IsAntiArea).ToArray(),
                        Anti: areas.Where(area => area.IsAntiArea).ToArray());
                })
                .Where(participant => participant.Positive.Length > 0 && participant.Anti.Length > 0)
                .Count(participant =>
                    participant.Positive.Any(area =>
                        area.Boundary.Arcs is { Count: >= 4 } &&
                        TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
                        minX <= -145 &&
                        maxX >= 5 &&
                        minY <= -80 &&
                        maxY >= 80) &&
                    participant.Anti.Any(area =>
                        area.Boundary.Arcs is { Count: >= 4 } &&
                        TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
                        minX >= -160 &&
                        maxX <= 25 &&
                        minY <= -40 &&
                        maxY >= 40));
            var crescentOutlineOpenings = WalkRelations(outlineResult)
                .Where(relation => GraphicAreaParticipationSurface.TryReadAreaUnion(relation, out _, out _, out _, out _))
                .SelectMany(relation =>
                {
                    GraphicAreaParticipationSurface.TryReadAreaUnion(relation, out _, out var outlineWitness, out _, out _);
                    return GraphicReadGraph.From(outlineWitness).ReadsOf<ClosedSharedFrameBoundaryRead>();
                })
                .Where(boundary =>
                    TryReadSharedFrameBoundaryBounds(boundary, out var minX, out var maxX, out var minY, out var maxY) &&
                    minX >= -155 &&
                    maxX <= 10 &&
                    minY <= -60 &&
                    maxY >= 60 &&
                    boundary.Arcs is { Count: >= 4 })
                .Select(boundary =>
                {
                    TryReadSharedFrameBoundaryBounds(boundary, out var minX, out var maxX, out var minY, out var maxY);
                    return $"{minX}:{maxX}:{minY}:{maxY}:arcs={boundary.Arcs?.Count ?? 0}";
                })
                .Distinct()
                .ToArray();
            if (crescentOutlineOpenings.Length == 0)
            {
                var outlineBounds = WalkRelations(outlineResult)
                    .Where(relation => GraphicAreaParticipationSurface.TryReadAreaUnion(relation, out _, out _, out _, out _))
                    .SelectMany(relation =>
                    {
                        GraphicAreaParticipationSurface.TryReadAreaUnion(relation, out _, out var outlineWitness, out _, out _);
                        return GraphicReadGraph.From(outlineWitness).ReadsOf<ClosedSharedFrameBoundaryRead>();
                    })
                    .Select(boundary =>
                    {
                        TryReadSharedFrameBoundaryBounds(boundary, out var minX, out var maxX, out var minY, out var maxY);
                        return $"{minX}:{maxX}:{minY}:{maxY}:arcs={boundary.Arcs?.Count ?? 0}";
                    })
                    .Distinct()
                    .ToArray();
                var localAntiBounds = mergeAreas
                    .Where(area => area.IsAntiArea &&
                        TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
                        minX >= -170 &&
                        maxX <= 40 &&
                        minY <= -30 &&
                        maxY >= 30)
                    .Select(area =>
                    {
                        TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY);
                        return $"{minX}:{maxX}:{minY}:{maxY}:arcs={area.Boundary.Arcs?.Count ?? 0}";
                    })
                    .Distinct()
                    .ToArray();
                throw new InvalidOperationException(
                    $"Sketch scene '{scene.Label}' did not preserve the crescent center opening as full settled outline evidence. " +
                    $"Local openings={crescentLocalCenterOpenings}, LocalAnti=[{string.Join(", ", localAntiBounds)}], outlines=[{string.Join(", ", outlineBounds)}].");
            }
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
        var expectedInteriorCusps = comparisonPairMode ? 4 : 8;
        if (cuspMarkers < expectedInteriorCusps)
        {
            throw new InvalidOperationException(
                $"Sketch scene '{scene.Label}' did not mark the expected interior non-tangent joins. " +
                $"Markers={cuspMarkers}, expected={expectedInteriorCusps}.");
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
            .Where(relation => GraphicAreaParticipationSurface.TryReadAreaUnion(relation, out _, out _, out _, out _))
            .SelectMany(relation =>
            {
                GraphicAreaParticipationSurface.TryReadAreaUnion(relation, out _, out var roundedOutlineWitness, out _, out _);
                return GraphicReadGraph.From(roundedOutlineWitness).ReadsOf<ClosedSharedFrameBoundaryRead>();
            })
            .Count(boundary => boundary.Arcs is { Count: >= 4 });
        var expectedRoundedCenterOpenings = comparisonPairMode ? 1 : 4;
        if (roundedCenterOpenings < expectedRoundedCenterOpenings)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not replace the interior corner joins with rounded arc outlines.");
        }

        var comparisonRoundedAntiOpenings = WalkRelations(roundedResult)
            .Where(relation => GraphicAreaParticipationSurface.TryReadAreaUnion(relation, out _, out _, out _, out _))
            .SelectMany(relation =>
            {
                GraphicAreaParticipationSurface.TryReadAreaUnion(relation, out _, out var roundedOutlineWitness, out _, out _);
                return GraphicReadGraph.From(roundedOutlineWitness).ReadsOf<ClosedSharedFrameBoundaryRead>();
            })
            .Where(boundary => boundary.Arcs is { Count: >= 3 } &&
                TryReadSharedFrameBoundaryBounds(boundary, out var minX, out var maxX, out var minY, out var maxY) &&
                minX >= 145 &&
                maxX <= 390 &&
                maxY - minY >= 8)
            .Select(boundary =>
            {
                TryReadSharedFrameBoundaryBounds(boundary, out var minX, out var maxX, out var minY, out var maxY);
                return $"{minX}:{maxX}:{minY}:{maxY}";
            })
            .Distinct()
            .ToArray();
        if (comparisonRoundedAntiOpenings.Length < 1)
        {
            var comparisonAntiBounds = WalkRelations(roundedResult)
                .Where(relation => GraphicAreaParticipationSurface.TryReadAreaUnion(relation, out _, out _, out _, out _))
                .SelectMany(relation =>
                {
                    GraphicAreaParticipationSurface.TryReadAreaUnion(relation, out _, out var roundedOutlineWitness, out _, out _);
                    return GraphicReadGraph.From(roundedOutlineWitness).ReadsOf<ClosedSharedFrameBoundaryRead>();
                })
                .Select(boundary =>
                {
                    TryReadSharedFrameBoundaryBounds(boundary, out var minX, out var maxX, out var minY, out var maxY);
                    return $"{minX}:{maxX}:{minY}:{maxY}:arcs={boundary.Arcs?.Count ?? 0}";
                })
                .Distinct()
                .ToArray();
            throw new InvalidOperationException(
                $"Sketch scene '{scene.Label}' did not preserve rounded comparison-lane anti-area openings. " +
                $"Anti=[{string.Join(", ", comparisonAntiBounds)}].");
        }

        var roundedAreas = WalkRelations(roundedResult)
            .Where(relation => GraphicAreaParticipationSurface.TryReadAreaUnion(relation, out _, out _, out _, out _))
            .SelectMany(relation =>
            {
                GraphicAreaParticipationSurface.TryReadAreaUnion(relation, out var roundedAreaFrame, out _, out _, out _);
                return GraphicReadGraph.From(roundedAreaFrame).ReadsOf<SharedFrameAreaRead>();
            })
            .ToArray();
        var roundedPositiveBounds = roundedAreas
            .Where(area => !area.IsAntiArea)
            .Select(area =>
            {
                TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY);
                return $"{minX}:{maxX}:{minY}:{maxY}";
            })
            .Distinct()
            .ToArray();
        if (roundedPositiveBounds.Length < expectedUnionLanes)
        {
            throw new InvalidOperationException(
                $"Sketch scene '{scene.Label}' did not preserve rounded positive area lanes. " +
                $"Positive={roundedPositiveBounds.Length} [{string.Join(", ", roundedPositiveBounds)}].");
        }

        if (!comparisonPairMode &&
            !roundedAreas.Any(area =>
                !area.IsAntiArea &&
                TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out _, out _) &&
                minX >= 120 &&
                maxX <= 420))
        {
            throw new InvalidOperationException(
                $"Sketch scene '{scene.Label}' did not preserve the rounded H positive area lane. " +
                $"Positive={roundedPositiveBounds.Length} [{string.Join(", ", roundedPositiveBounds)}].");
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
        var expectedExteriorCusps = comparisonPairMode ? 8 : 16;
        if (exteriorCuspMarkers < expectedExteriorCusps)
        {
            throw new InvalidOperationException(
                $"Sketch scene '{scene.Label}' did not mark the expected exterior non-tangent joins. " +
                $"Read {exteriorCuspMarkers}, expected {expectedExteriorCusps}.");
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
                var read = GraphicAreaParticipationSurface.TryReadAreaUnion(relation, out var areaFrame, out var outlineWitness, out _, out _);
                return (read, areaFrame, outlineWitness);
            })
            .Where(union => union.read)
            .DistinctBy(union => ReadSourceId(union.areaFrame))
            .ToArray();
        if (exteriorRoundedUnions.Length < expectedUnionLanes)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose the expected exterior-rounded area union lanes.");
        }

        var exteriorRoundedParticipantCount = 0;
        var exteriorRoundedAreas = new List<SharedFrameAreaRead>();
        foreach (var (_, exteriorRoundedAreaFrame, _) in exteriorRoundedUnions)
        {
            if (exteriorRoundedAreaFrame is Relation exteriorRoundedAreaFrameRelation &&
                GraphicAreaParticipationSurface.TryReadAreaFrame(exteriorRoundedAreaFrameRelation, out var exteriorRoundedParticipants))
            {
                exteriorRoundedParticipantCount += exteriorRoundedParticipants.Length;
                exteriorRoundedAreas.AddRange(GraphicReadGraph.From(exteriorRoundedAreaFrameRelation).ReadsOf<SharedFrameAreaRead>());
            }
        }

        var exteriorRoundedOutlines = exteriorRoundedUnions
            .SelectMany(union => GraphicReadGraph.From(union.outlineWitness).ReadsOf<ClosedSharedFrameBoundaryRead>())
            .ToArray();
        var exteriorRoundedPositiveBounds = exteriorRoundedAreas
            .Where(area => !area.IsAntiArea)
            .Select(area =>
            {
                TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY);
                return $"{minX}:{maxX}:{minY}:{maxY}";
            })
            .Distinct()
            .ToArray();
        var exteriorRoundedHoleOutlineBounds = exteriorRoundedOutlines
            .Select(boundary =>
            {
                TryReadSharedFrameBoundaryBounds(boundary, out var minX, out var maxX, out var minY, out var maxY);
                return $"{minX}:{maxX}:{minY}:{maxY}";
            })
            .Where(bounds => !exteriorRoundedPositiveBounds.Contains(bounds))
            .Distinct()
            .ToArray();
        var exteriorRoundedExteriorOutlineBounds = exteriorRoundedOutlines
            .Select(boundary =>
            {
                TryReadSharedFrameBoundaryBounds(boundary, out var minX, out var maxX, out var minY, out var maxY);
                return $"{minX}:{maxX}:{minY}:{maxY}";
            })
            .Where(exteriorRoundedPositiveBounds.Contains)
            .Distinct()
            .ToArray();
        if (exteriorRoundedAreas.Count(area => !area.IsAntiArea) < 1 ||
            exteriorRoundedAreas.Any(area => area.IsAntiArea) ||
            exteriorRoundedExteriorOutlineBounds.Length < expectedUnionLanes ||
            exteriorRoundedHoleOutlineBounds.Length < 8)
        {
            var outlineBounds = exteriorRoundedUnions
                .SelectMany(union => GraphicReadGraph.From(union.outlineWitness).ReadsOf<ClosedSharedFrameBoundaryRead>())
                .Select(boundary =>
                {
                    TryReadSharedFrameBoundaryBounds(boundary, out var minX, out var maxX, out var minY, out var maxY);
                    return $"{minX}:{maxX}:{minY}:{maxY}";
                })
                .Distinct()
                .ToArray();
            var antiBounds = exteriorRoundedAreas
                .Where(area => area.IsAntiArea)
                .Select(area =>
                {
                    TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY);
                    return $"{minX}:{maxX}:{minY}:{maxY}";
                })
                .Distinct()
                .ToArray();
            throw new InvalidOperationException(
                $"Sketch scene '{scene.Label}' did not preserve the exterior-rounded positive fill and hole cuts. " +
                $"Positive={exteriorRoundedPositiveBounds.Length} [{string.Join(", ", exteriorRoundedPositiveBounds)}], " +
                $"anti={antiBounds.Length} [{string.Join(", ", antiBounds)}], " +
                $"exteriorOutlines={exteriorRoundedExteriorOutlineBounds.Length} [{string.Join(", ", exteriorRoundedExteriorOutlineBounds)}], " +
                $"holes={exteriorRoundedHoleOutlineBounds.Length} [{string.Join(", ", exteriorRoundedHoleOutlineBounds)}], " +
                $"outline={outlineBounds.Length} [{string.Join(", ", outlineBounds)}].");
        }

        var exteriorRoundedBoundaryEvidence = exteriorRoundedOutlines
            .Concat(exteriorRoundedAreas
                .Where(area => !area.IsAntiArea)
                .Select(area => area.Boundary))
            .ToArray();
        var exteriorRoundedArcCount = exteriorRoundedBoundaryEvidence.Sum(boundary => boundary.Arcs?.Count ?? 0);
        if (comparisonPairMode)
        {
            if (exteriorRoundedArcCount < 12)
            {
                throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not replace exterior cusp joins with tangent arc outline evidence.");
            }
        }
        else if (!exteriorRoundedBoundaryEvidence.Any(boundary => boundary.Arcs is { Count: >= 24 }))
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not replace exterior cusp joins with tangent arc outline evidence.");
        }

        var hasComparisonRoundedOutlineEvidence = exteriorRoundedBoundaryEvidence.Any(boundary =>
            boundary.Arcs is { Count: >= 3 } &&
            TryReadSharedFrameBoundaryBounds(boundary, out var minX, out var maxX, out _, out _) &&
            minX >= 140 &&
            maxX <= 390);
        var hasOriginalRoundedOutlineEvidence = exteriorRoundedBoundaryEvidence.Any(boundary =>
            boundary.Arcs is { Count: >= 4 } &&
            TryReadSharedFrameBoundaryBounds(boundary, out var minX, out var maxX, out _, out _) &&
            minX >= 150 &&
            maxX <= 370);
        if (comparisonPairMode
                ? !hasComparisonRoundedOutlineEvidence
                : !hasOriginalRoundedOutlineEvidence)
        {
            var roundedOutlineBounds = exteriorRoundedOutlines
                .Select(boundary =>
                {
                    TryReadSharedFrameBoundaryBounds(boundary, out var minX, out var maxX, out var minY, out var maxY);
                    return $"{minX}:{maxX}:{minY}:{maxY}:arcs={boundary.Arcs?.Count ?? 0}";
                })
                .Distinct()
                .ToArray();
            throw new InvalidOperationException(
                $"Sketch scene '{scene.Label}' did not apply selected exterior cusp rounding to the comparison outline. " +
                $"Outlines=[{string.Join(", ", roundedOutlineBounds)}].");
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
                var read = GraphicAreaParticipationSurface.TryReadAreaUnion(relation, out var areaFrame, out var outlineWitness, out _, out _);
                return (read, areaFrame, outlineWitness);
            })
            .Where(union => union.read)
            .DistinctBy(union => ReadSourceId(union.areaFrame))
            .ToArray();
        var finalColoredParticipantCount = 0;
        var finalColoredAreas = new List<SharedFrameAreaRead>();
        foreach (var (_, finalColoredAreaFrame, _) in finalColoredUnions)
        {
            if (finalColoredAreaFrame is Relation finalColoredAreaFrameRelation &&
                GraphicAreaParticipationSurface.TryReadAreaFrame(finalColoredAreaFrameRelation, out var finalColoredParticipants))
            {
                finalColoredParticipantCount += finalColoredParticipants.Length;
                finalColoredAreas.AddRange(GraphicReadGraph.From(finalColoredAreaFrameRelation).ReadsOf<SharedFrameAreaRead>());
            }
        }

        var finalColoredOutlines = finalColoredUnions
            .SelectMany(union => GraphicReadGraph.From(union.outlineWitness).ReadsOf<ClosedSharedFrameBoundaryRead>())
            .ToArray();
        var finalColoredPositiveBounds = finalColoredAreas
            .Where(area => !area.IsAntiArea)
            .Select(area =>
            {
                TryReadSharedFrameBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY);
                return $"{minX}:{maxX}:{minY}:{maxY}";
            })
            .Distinct()
            .ToArray();
        var finalColoredHoleOutlineBounds = finalColoredOutlines
            .Select(boundary =>
            {
                TryReadSharedFrameBoundaryBounds(boundary, out var minX, out var maxX, out var minY, out var maxY);
                return $"{minX}:{maxX}:{minY}:{maxY}";
            })
            .Where(bounds => !finalColoredPositiveBounds.Contains(bounds))
            .Distinct()
            .ToArray();
        var finalColoredExteriorOutlineBounds = finalColoredOutlines
            .Select(boundary =>
            {
                TryReadSharedFrameBoundaryBounds(boundary, out var minX, out var maxX, out var minY, out var maxY);
                return $"{minX}:{maxX}:{minY}:{maxY}";
            })
            .Where(finalColoredPositiveBounds.Contains)
            .Distinct()
            .ToArray();
        if (finalColoredUnions.Length < expectedUnionLanes ||
            finalColoredAreas.Count(area => !area.IsAntiArea) < expectedUnionLanes ||
            finalColoredAreas.Any(area => area.IsAntiArea) ||
            finalColoredExteriorOutlineBounds.Length < expectedUnionLanes ||
            finalColoredHoleOutlineBounds.Length < 8)
        {
            throw new InvalidOperationException(
                $"Sketch scene '{scene.Label}' ({scene.Caption}) did not keep the final holes as unfilled area cuts. " +
                $"unions={finalColoredUnions.Length}/{expectedUnionLanes}, " +
                $"positive={finalColoredAreas.Count(area => !area.IsAntiArea)}/{expectedUnionLanes}, " +
                $"anti={finalColoredAreas.Count(area => area.IsAntiArea)}, " +
                $"exteriorOutlines={finalColoredExteriorOutlineBounds.Length}/{expectedUnionLanes}, " +
                $"holes={finalColoredHoleOutlineBounds.Length}/8, " +
                $"outlines={finalColoredOutlines.Length}.");
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

    private static string PointSignature(SharedFramePointRead point) =>
        point.Coordinates.Count >= 2
            ? $"{point.Coordinates[0].Ticks}:{point.Coordinates[1].Ticks}"
            : ReadSourceId(point.Source).ToString();

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

        void Include(long x, long y)
        {
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

        void IncludePoint(SharedFramePointRead point)
        {
            if (point.Coordinates.Count < 2)
            {
                return;
            }

            Include(point.Coordinates[0].Ticks, point.Coordinates[1].Ticks);
        }

        foreach (var segment in boundary.Segments)
        {
            IncludePoint(segment.Start);
            IncludePoint(segment.End);
        }

        foreach (var arc in boundary.Arcs ?? [])
        {
            IncludeArc(arc);
        }

        foreach (var circle in boundary.Circles ?? [])
        {
            IncludeCircle(circle);
        }

        minX = readMinX;
        maxX = readMaxX;
        minY = readMinY;
        maxY = readMaxY;
        return sawPoint;

        void IncludeArc(SharedFrameArcRead arc)
        {
            if (arc.Center.Coordinates.Count < 2 ||
                arc.Start.Coordinates.Count < 2 ||
                arc.End.Coordinates.Count < 2)
            {
                return;
            }

            var centerX = arc.Center.Coordinates[0].Ticks;
            var centerY = arc.Center.Coordinates[1].Ticks;
            var startX = arc.Start.Coordinates[0].Ticks;
            var startY = arc.Start.Coordinates[1].Ticks;
            var endX = arc.End.Coordinates[0].Ticks;
            var endY = arc.End.Coordinates[1].Ticks;
            var radius = (Math.Sqrt(Math.Pow(startX - centerX, 2) + Math.Pow(startY - centerY, 2)) +
                Math.Sqrt(Math.Pow(endX - centerX, 2) + Math.Pow(endY - centerY, 2))) * 0.5d;
            var startAngle = Math.Atan2(startY - centerY, startX - centerX);
            var sweep = ShortestSweep(startAngle, Math.Atan2(endY - centerY, endX - centerX));
            for (var index = 0; index <= 32; index++)
            {
                var angle = startAngle + sweep * (index / 32d);
                Include(
                    (long)Math.Round(centerX + Math.Cos(angle) * radius),
                    (long)Math.Round(centerY + Math.Sin(angle) * radius));
            }
        }

        void IncludeCircle(SharedFrameCircleRead circle)
        {
            if (circle.Center.Coordinates.Count < 2 ||
                circle.RadiusPoint.Coordinates.Count < 2)
            {
                return;
            }

            var centerX = circle.Center.Coordinates[0].Ticks;
            var centerY = circle.Center.Coordinates[1].Ticks;
            var radiusX = circle.RadiusPoint.Coordinates[0].Ticks;
            var radiusY = circle.RadiusPoint.Coordinates[1].Ticks;
            var radius = Math.Sqrt(Math.Pow(radiusX - centerX, 2) + Math.Pow(radiusY - centerY, 2));
            Include((long)Math.Round(centerX - radius), (long)Math.Round(centerY - radius));
            Include((long)Math.Round(centerX + radius), (long)Math.Round(centerY + radius));
        }

        static double ShortestSweep(double startAngle, double endAngle)
        {
            var sweep = endAngle - startAngle;
            while (sweep <= -Math.PI)
            {
                sweep += Math.PI * 2d;
            }

            while (sweep > Math.PI)
            {
                sweep -= Math.PI * 2d;
            }

            return sweep;
        }
    }

    private static bool TryReadSharedFrameCircleBoundary(
        ClosedSharedFrameBoundaryRead boundary,
        out long centerX,
        out long centerY,
        out long radius)
    {
        if (boundary.Circles is { Count: > 0 } circles &&
            circles[0].Center.Coordinates.Count >= 2 &&
            circles[0].RadiusPoint.Coordinates.Count >= 2)
        {
            centerX = circles[0].Center.Coordinates[0].Ticks;
            centerY = circles[0].Center.Coordinates[1].Ticks;
            var radiusX = circles[0].RadiusPoint.Coordinates[0].Ticks;
            var radiusY = circles[0].RadiusPoint.Coordinates[1].Ticks;
            radius = (long)Math.Round(Math.Sqrt(
                Math.Pow(radiusX - centerX, 2) +
                Math.Pow(radiusY - centerY, 2)));
            return radius > 0;
        }

        var arcs = boundary.Arcs ?? [];
        if (arcs.Count < 4 ||
            arcs[0].Center.Coordinates.Count < 2 ||
            arcs[0].Start.Coordinates.Count < 2)
        {
            centerX = 0;
            centerY = 0;
            radius = 0;
            return false;
        }

        centerX = arcs[0].Center.Coordinates[0].Ticks;
        centerY = arcs[0].Center.Coordinates[1].Ticks;
        var startX = arcs[0].Start.Coordinates[0].Ticks;
        var startY = arcs[0].Start.Coordinates[1].Ticks;
        foreach (var arc in arcs)
        {
            if (arc.Center.Coordinates.Count < 2 ||
                arc.Center.Coordinates[0].Ticks != centerX ||
                arc.Center.Coordinates[1].Ticks != centerY)
            {
                radius = 0;
                return false;
            }
        }

        radius = (long)Math.Round(Math.Sqrt(
            Math.Pow(startX - centerX, 2) +
            Math.Pow(startY - centerY, 2)));
        return radius > 0;
    }

    private static IEnumerable<(long MinX, long MaxX, long MinY, long MaxY)> ReadSharedFrameLineSegmentBounds(IElement element)
    {
        var graph = GraphicReadGraph.From(element);
        var arcSpanIds = graph.ReadsOf<SharedFrameArcRead>()
            .Select(arc => ReadSourceId(arc.Span.Source))
            .ToHashSet();
        var circleRadiusSpanIds = graph.ReadsOf<SharedFrameCircleRead>()
            .Select(circle => ReadSourceId(circle.RadiusSpan.Source))
            .ToHashSet();
        foreach (var segment in graph.ReadsOf<SharedFrameSegmentRead>())
        {
            if (arcSpanIds.Contains(ReadSourceId(segment.Source)) ||
                circleRadiusSpanIds.Contains(ReadSourceId(segment.Source)) ||
                segment.Start.Coordinates.Count < 2 ||
                segment.End.Coordinates.Count < 2)
            {
                continue;
            }

            var startX = segment.Start.Coordinates[0].Ticks;
            var startY = segment.Start.Coordinates[1].Ticks;
            var endX = segment.End.Coordinates[0].Ticks;
            var endY = segment.End.Coordinates[1].Ticks;
            yield return (
                Math.Min(startX, endX),
                Math.Max(startX, endX),
                Math.Min(startY, endY),
                Math.Max(startY, endY));
        }
    }

    private static IEnumerable<(long MinX, long MaxX, long MinY, long MaxY)> ReadSharedFrameArcBounds(IElement element)
    {
        foreach (var arc in GraphicReadGraph.From(element).ReadsOf<SharedFrameArcRead>())
        {
            if (arc.Start.Coordinates.Count < 2 ||
                arc.End.Coordinates.Count < 2)
            {
                continue;
            }

            var startX = arc.Start.Coordinates[0].Ticks;
            var startY = arc.Start.Coordinates[1].Ticks;
            var endX = arc.End.Coordinates[0].Ticks;
            var endY = arc.End.Coordinates[1].Ticks;
            yield return (
                Math.Min(startX, endX),
                Math.Max(startX, endX),
                Math.Min(startY, endY),
                Math.Max(startY, endY));
        }
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
            canvas.InvalidateVisual();
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
