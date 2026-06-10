using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.VisualTree;
using Gojo.Core;
using Gojo.Structures;
using GojoCAD.Structures;
using GojoDraw;
using SkiaSharp;

namespace GojoCAD.SketchViewer;

internal sealed class SketchCanvas : Control
{
    private SketchDocument? document;

    public SketchCanvas()
    {
        ClipToBounds = true;
    }

    public SketchDocument? Document
    {
        get => document;
        set
        {
            document = value;
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Document is { } current)
        {
            context.Custom(new SketchDrawOperation(
                new Rect(Bounds.Size),
                current.SelectedScene,
                current.CurrentRungIndex,
                current.ShowConstructionPoints,
                current.ShowPreviousRungs));
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        InvalidateVisual();
    }
}

internal sealed class SketchDrawOperation : ICustomDrawOperation
{
    private static readonly GojoCadSkiaSceneRenderer Renderer = new();
    private static readonly GojoCadSkiaOffsetStudyRenderer OffsetRenderer = new();
    private static readonly SKColor CanvasBackground = new(249, 248, 244);
    private static readonly SKColor RadialMergedFill = new(48, 88, 142, 158);
    private static readonly SKColor RadialAntiAreaFill = new(226, 50, 55, 58);
    private static readonly SKColor RadialMergedAntiAreaFill = new(246, 199, 199);
    private static readonly SKColor RadialPartitionEmptyFill = new(34, 215, 230);
    private static readonly SKColor RadialCuspMarker = new(226, 18, 34, 245);
    private static readonly SKColor RadialPositiveSide = new(78, 158, 210, 228);
    private static readonly SKColor RadialInverseSide = new(226, 92, 96, 222);
    private static readonly SKColor[] RadialSeparateFillPalette =
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
    private readonly SketchScene scene;
    private readonly int selectedRungIndex;
    private readonly bool showConstructionPoints;
    private readonly bool showPreviousRungs;

    public SketchDrawOperation(Rect bounds, SketchScene scene, int selectedRungIndex, bool showConstructionPoints, bool showPreviousRungs)
    {
        Bounds = bounds;
        this.scene = scene;
        this.selectedRungIndex = selectedRungIndex;
        this.showConstructionPoints = showConstructionPoints;
        this.showPreviousRungs = showPreviousRungs;
    }

    public Rect Bounds { get; }

    public void Dispose()
    {
    }

    public bool HitTest(Point p) => Bounds.Contains(p);

    public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);

    public void Render(ImmediateDrawingContext context)
    {
        if (context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) is not ISkiaSharpApiLeaseFeature leaseFeature)
        {
            return;
        }

        using var lease = leaseFeature.Lease();
        var canvas = lease.SkCanvas;
        canvas.Save();
        canvas.Clear(CanvasBackground);
        canvas.ClipRect(new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height));
        DrawScene(canvas);
        canvas.Restore();
    }

    private void DrawScene(SKCanvas canvas)
    {
        var viewport = new SKRect(
            42f,
            78f,
            Math.Max(43f, (float)Bounds.Width - 42f),
            Math.Max(79f, (float)Bounds.Height - 48f));

        DrawHeader(canvas, viewport);
        if (scene.OffsetStudy is { } offsetStudy)
        {
            OffsetRenderer.Draw(
                canvas,
                offsetStudy,
                new SKRect(viewport.Left, viewport.Top + 16f, viewport.Right, viewport.Bottom));
            return;
        }

        var rungs = scene.ReadRungs();
        var boundedRung = Math.Clamp(selectedRungIndex, 0, Math.Max(0, rungs.Count - 1));
        if (rungs.Count == 0)
        {
            using var paints = CreateSketchPaints(Blue(255), 3.4f, hidePoints: !showConstructionPoints);
            Renderer.Draw(canvas, scene.Structure, viewport, scene.ProjectionSeed, scene.ModelBounds, paints);
            return;
        }

        if (boundedRung == rungs.Count - 1)
        {
            DrawRung(canvas, rungs[boundedRung], boundedRung, viewport, Blue(255), 3.4f, hidePoints: true);
            return;
        }

        if (showPreviousRungs)
        {
            for (var index = 0; index < boundedRung; index++)
            {
                var distance = boundedRung - index;
                var alpha = (byte)Math.Clamp(122 - distance * 18, 30, 104);
                DrawRung(canvas, rungs[index], index, viewport, Gray(alpha), 1.75f, hidePoints: !showConstructionPoints, useSpecialAreaPaints: false);
            }
        }

        DrawRung(canvas, rungs[boundedRung], boundedRung, viewport, CurrentGray(235), 3.2f, hidePoints: !showConstructionPoints);
    }

    private void DrawHeader(SKCanvas canvas, SKRect viewport)
    {
        using var title = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(31, 37, 45),
            TextSize = 20f,
            Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold),
            Style = SKPaintStyle.Fill
        };
        using var caption = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(57, 64, 74, 205),
            TextSize = 12.5f,
            Typeface = SKTypeface.FromFamilyName("Segoe UI"),
            Style = SKPaintStyle.Fill
        };

        canvas.DrawText(scene.Label, viewport.Left, 34f, title);
        var rungText = $"{selectedRungIndex + 1}/{scene.RungCount}: {scene.ReadRungLabel(selectedRungIndex)}";
        DrawTextLine(canvas, $"{scene.Caption}  {rungText}", viewport.Left, 56f, viewport.Width, caption);
    }

    private void DrawRung(
        SKCanvas canvas,
        Relation rung,
        int rungIndex,
        SKRect viewport,
        SKColor color,
        float strokeWidth,
        bool hidePoints = false,
        bool useSpecialAreaPaints = true)
    {
        if (!StructuralLadderSurface.TryReadRung(
                rung,
                out _,
                out _,
                out var result,
                out _,
                out _,
                out _,
                out _,
                out _))
        {
            return;
        }

        if (TryDrawRadialOffsetSideWitness(canvas, result, rungIndex, viewport, strokeWidth))
        {
            return;
        }

        if (TryDrawRadialCuspSelection(canvas, result, rungIndex, viewport, strokeWidth, hidePoints))
        {
            return;
        }

        ReadAreaPaints(
            rungIndex,
            useSpecialAreaPaints,
            out var areaPalette,
            out var areaFillColor,
            out var antiAreaFillColor,
            out var hideAreaBoundary,
            out var hideSharedFrameGeometry,
            out var drawSharedFrameAreaPartitions,
            out var drawAreaUnionOutline,
            out var drawAreaUnionAsOutline,
            out var drawAreaUnionFromSharedFrameAreaPartitions,
            out var drawAreaUnionAntiAreas,
            out var drawAreaUnionSettlementPartitions,
            out var eraseAreaUnionOutline);
        using var paints = CreateSketchPaints(
            color,
            strokeWidth,
            hidePoints,
            areaPalette,
            areaFillColor,
            antiAreaFillColor,
            hideAreaBoundary,
            hideSharedFrameGeometry,
            drawSharedFrameAreaPartitions,
            drawAreaUnionOutline,
            drawAreaUnionAsOutline,
            drawAreaUnionFromSharedFrameAreaPartitions,
            drawAreaUnionAntiAreas,
            drawAreaUnionSettlementPartitions,
            eraseAreaUnionOutline);
        Renderer.Draw(canvas, result, viewport, scene.ProjectionSeed, scene.ModelBounds, paints);
    }

    private bool TryDrawRadialOffsetSideWitness(
        SKCanvas canvas,
        IElement result,
        int rungIndex,
        SKRect viewport,
        float strokeWidth)
    {
        if (scene.Label != "Radial Shape Ladder" ||
            rungIndex != 2)
        {
            return false;
        }

        var witnesses = ReadOffsetSideWitnesses(result).ToArray();
        if (witnesses.Length == 0)
        {
            return false;
        }

        using var sourcePaints = CreateSketchPaints(
            Gray(52),
            Math.Max(1.2f, strokeWidth * 0.55f),
            hidePoints: true);
        using var boundaryPaints = CreateSketchPaints(
            CurrentGray(228),
            strokeWidth,
            hidePoints: true);
        using var positivePaints = CreateSketchPaints(
            RadialPositiveSide,
            Math.Max(2.1f, strokeWidth * 0.78f),
            hidePoints: true);
        using var inversePaints = CreateSketchPaints(
            RadialInverseSide,
            Math.Max(2.1f, strokeWidth * 0.78f),
            hidePoints: true);

        foreach (var witness in witnesses)
        {
            Renderer.Draw(canvas, witness.Source, viewport, scene.ProjectionSeed, scene.ModelBounds, sourcePaints);
        }

        foreach (var witness in witnesses)
        {
            Renderer.Draw(canvas, witness.Boundary, viewport, scene.ProjectionSeed, scene.ModelBounds, boundaryPaints);
        }

        foreach (var witness in witnesses)
        {
            Renderer.Draw(canvas, witness.PositiveSide, viewport, scene.ProjectionSeed, scene.ModelBounds, positivePaints);
            Renderer.Draw(canvas, witness.InverseSide, viewport, scene.ProjectionSeed, scene.ModelBounds, inversePaints);
        }

        return true;
    }

    private static IEnumerable<OffsetSideWitnessRead> ReadOffsetSideWitnesses(IElement element)
    {
        if (element is Relation relation &&
            GraphicOffsetSurface.TryReadBoundarySideWitness(
                relation,
                out var source,
                out var boundary,
                out var sidePair,
                out _) &&
            GraphicOffsetSurface.TryReadOrthogonalSidePair(
                sidePair,
                out var positiveSide,
                out var inverseSide,
                out _))
        {
            yield return new OffsetSideWitnessRead(source, boundary, positiveSide, inverseSide);
            yield break;
        }

        if (TryReadOrderedFrame(element, out var elements))
        {
            foreach (var child in elements)
            foreach (var witness in ReadOffsetSideWitnesses(child))
            {
                yield return witness;
            }
        }
    }

    private bool TryDrawRadialCuspSelection(
        SKCanvas canvas,
        IElement result,
        int rungIndex,
        SKRect viewport,
        float strokeWidth,
        bool hidePoints)
    {
        if (scene.Label != "Radial Shape Ladder" ||
            (rungIndex != 6 && rungIndex != 8) ||
            !TryReadRadialSelectionWitness(result, out var outline, out var markerEvidence))
        {
            return false;
        }

        using var outlinePaints = CreateSketchPaints(
            CurrentGray(235),
            strokeWidth,
            hidePoints,
            antiAreaFillColor: RadialAntiAreaFill,
            drawAreaUnionOutline: false,
            drawAreaUnionAsOutline: true,
            drawAreaUnionFromSharedFrameAreaPartitions: rungIndex == 6,
            drawAreaUnionAntiAreas: false);
        Renderer.Draw(canvas, outline, viewport, scene.ProjectionSeed, scene.ModelBounds, outlinePaints);

        using var markerPaints = CreateSketchPaints(
            RadialCuspMarker,
            Math.Max(2.4f, strokeWidth * 0.78f),
            hidePoints: true,
            hideAreaBoundary: true);
        Renderer.Draw(canvas, markerEvidence, viewport, scene.ProjectionSeed, scene.ModelBounds, markerPaints);
        return true;
    }

    private static bool TryReadRadialSelectionWitness(
        IElement result,
        out IElement outline,
        out IElement markerEvidence)
    {
        if (TryReadOrderedFrame(result, out var framedSelections) &&
            framedSelections.Length > 0 &&
            framedSelections.All(element => element is Relation selection &&
                GraphicSelectionSurface.TryReadSelection(selection, out _, out _, out _, out _, out _)))
        {
            var outlines = new List<IElement>();
            var markerWitnesses = new List<IElement>();
            foreach (var framedSelection in framedSelections.OfType<Relation>())
            {
                if (TryReadSingleSelectionWitness(framedSelection, out var selectionSource, out var selectionMarkers))
                {
                    outlines.Add(selectionSource);
                    markerWitnesses.AddRange(selectionMarkers);
                }
            }

            if (outlines.Count > 0)
            {
                outline = GraphicSelectionSurface.Frame(new Carrier(), [.. outlines]);
                markerEvidence = GraphicSelectionSurface.Frame(new Carrier(), [.. markerWitnesses]);
                return true;
            }
        }

        if (result is Relation selection &&
            TryReadSingleSelectionWitness(selection, out outline, out var singleMarkerWitnesses))
        {
            markerEvidence = GraphicSelectionSurface.Frame(new Carrier(), [.. singleMarkerWitnesses]);
            return true;
        }

        outline = default!;
        markerEvidence = default!;
        return false;
    }

    private static bool TryReadSingleSelectionWitness(
        Relation selection,
        out IElement outline,
        out List<IElement> markerWitnesses)
    {
        if (GraphicSelectionSurface.TryReadSelection(
                selection,
                out var source,
                out _,
                out _,
                out var matches,
                out _) &&
            matches is Relation matchFrame &&
            GraphicSelectionSurface.TryReadMatchFrame(matchFrame, out var matchElements))
        {
            markerWitnesses = [];
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

            outline = source;
            return true;
        }

        outline = default!;
        markerWitnesses = [];
        return false;
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

    private void ReadAreaPaints(
        int rungIndex,
        bool useSpecialAreaPaints,
        out IReadOnlyList<SKColor>? areaPalette,
        out SKColor? areaFillColor,
        out SKColor? antiAreaFillColor,
        out bool hideAreaBoundary,
        out bool hideSharedFrameGeometry,
        out bool drawSharedFrameAreaPartitions,
        out bool drawAreaUnionOutline,
        out bool drawAreaUnionAsOutline,
        out bool drawAreaUnionFromSharedFrameAreaPartitions,
        out bool drawAreaUnionAntiAreas,
        out bool drawAreaUnionSettlementPartitions,
        out bool eraseAreaUnionOutline)
    {
        areaPalette = null;
        areaFillColor = null;
        antiAreaFillColor = null;
        hideAreaBoundary = false;
        hideSharedFrameGeometry = false;
        drawSharedFrameAreaPartitions = false;
        drawAreaUnionOutline = false;
        drawAreaUnionAsOutline = false;
        drawAreaUnionFromSharedFrameAreaPartitions = false;
        drawAreaUnionAntiAreas = false;
        drawAreaUnionSettlementPartitions = false;
        eraseAreaUnionOutline = false;
        if (!useSpecialAreaPaints ||
            scene.Label != "Radial Shape Ladder")
        {
            return;
        }

        antiAreaFillColor = RadialAntiAreaFill;
        drawAreaUnionAntiAreas = rungIndex >= 4 && rungIndex < 10 && rungIndex != 5 && rungIndex != 7 && rungIndex != 9;
        drawAreaUnionSettlementPartitions = rungIndex >= 4 && rungIndex < 10 && rungIndex != 5 && rungIndex != 7 && rungIndex != 9;
        if (rungIndex == 3)
        {
            areaPalette = RadialSeparateFillPalette;
            antiAreaFillColor = RadialPartitionEmptyFill;
            hideAreaBoundary = true;
            hideSharedFrameGeometry = true;
            drawSharedFrameAreaPartitions = true;
        }
        else if (rungIndex == 4)
        {
            areaFillColor = RadialMergedFill;
            antiAreaFillColor = RadialMergedAntiAreaFill;
            hideAreaBoundary = true;
            hideSharedFrameGeometry = true;
            drawAreaUnionFromSharedFrameAreaPartitions = true;
        }
        else if (rungIndex == 5)
        {
            drawAreaUnionFromSharedFrameAreaPartitions = true;
            drawAreaUnionAsOutline = true;
        }
        else if (rungIndex == 7 ||
            rungIndex == 9)
        {
            drawAreaUnionAsOutline = true;
        }
        else if (rungIndex == 10)
        {
            areaFillColor = new SKColor(RadialMergedFill.Red, RadialMergedFill.Green, RadialMergedFill.Blue, 146);
            antiAreaFillColor = CanvasBackground;
            drawAreaUnionAntiAreas = true;
            drawAreaUnionOutline = true;
            hideAreaBoundary = true;
            hideSharedFrameGeometry = true;
        }
    }

    private static GojoCadSkiaPaints CreateSketchPaints(
        SKColor color,
        float strokeWidth,
        bool hidePoints = false,
        IReadOnlyList<SKColor>? areaFillPalette = null,
        SKColor? areaFillColor = null,
        SKColor? antiAreaFillColor = null,
        bool hideAreaBoundary = false,
        bool hideSharedFrameGeometry = false,
        bool drawSharedFrameAreaPartitions = false,
        bool drawAreaUnionOutline = false,
        bool drawAreaUnionAsOutline = false,
        bool drawAreaUnionFromSharedFrameAreaPartitions = false,
        bool drawAreaUnionAntiAreas = false,
        bool drawAreaUnionSettlementPartitions = false,
        bool eraseAreaUnionOutline = false)
    {
        var paints = new GojoCadSkiaPaints
        {
            DotRadius = 4.7f,
            RadialRadius = 24f,
            EndpointBarLength = 8f,
            PartitionTickLength = 7f,
            BasisSize = 5f,
            ArrowLength = 8f,
            ArrowWidth = 4f,
            LineSeedLength = 30f,
            SheetSeedWidth = 12f,
            AreaFillPalette = areaFillPalette,
            HideAreaBoundary = hideAreaBoundary,
            HideSharedFrameGeometry = hideSharedFrameGeometry,
            DrawSharedFrameAreaPartitions = drawSharedFrameAreaPartitions,
            DrawAreaUnionOutline = drawAreaUnionOutline,
            DrawAreaUnionAsOutline = drawAreaUnionAsOutline,
            DrawAreaUnionFromSharedFrameAreaPartitions = drawAreaUnionFromSharedFrameAreaPartitions,
            DrawAreaUnionAntiAreas = drawAreaUnionAntiAreas,
            DrawAreaUnionSettlementPartitions = drawAreaUnionSettlementPartitions,
            EraseAreaUnionOutline = eraseAreaUnionOutline
        };
        ApplyStroke(paints.Segment, color, strokeWidth * 0.9f);
        ApplyStroke(paints.SegmentTension, color, strokeWidth * 0.82f);
        ApplyStroke(paints.AngleControlledSegment, color, strokeWidth);
        ApplyStroke(paints.AngleControlledSegmentTension, color, strokeWidth * 0.9f);
        ApplyStroke(paints.SharedFrameSegment, color, strokeWidth);
        ApplyStroke(paints.SharedFrameCurve, color, strokeWidth);
        ApplyStroke(paints.SharedFrameCurveTension, color, strokeWidth * 0.9f);
        ApplyStroke(paints.DirectedSegment, color, strokeWidth);
        ApplyStroke(paints.DirectedSegmentTension, color, strokeWidth * 0.9f);
        ApplyStroke(paints.Circumference, color, Math.Max(1.1f, strokeWidth * 0.45f));
        ApplyStroke(paints.CircumferenceTension, color, Math.Max(1.1f, strokeWidth * 0.45f));
        ApplyStroke(paints.CircumferenceSpan, color, Math.Max(1.2f, strokeWidth * 0.55f));
        ApplyStroke(paints.CircumferenceSpanTension, color, Math.Max(1.2f, strokeWidth * 0.55f));
        ApplyStroke(paints.EndpointBar, color, Math.Max(1f, strokeWidth * 0.45f));
        ApplyStroke(paints.Partition, color, Math.Max(1f, strokeWidth * 0.45f));
        ApplyStroke(paints.LineSeed, color, Math.Max(1f, strokeWidth * 0.5f));
        ApplyStroke(paints.LineSeedTension, color, Math.Max(1f, strokeWidth * 0.5f));
        ApplyStroke(paints.SheetSeedEdge, color, Math.Max(1f, strokeWidth * 0.45f));
        ApplyStroke(paints.SheetSeedEdgeTension, color, Math.Max(1f, strokeWidth * 0.45f));
        paints.AreaFill.Color = areaFillColor ?? new SKColor(255, 255, 255, 244);
        if (antiAreaFillColor is { } antiFill)
        {
            paints.AntiAreaFill.Color = antiFill;
        }
        ApplyStroke(paints.AreaBoundary, color, Math.Max(1f, strokeWidth * 0.82f));
        var pointColor = hidePoints ? new SKColor(color.Red, color.Green, color.Blue, 0) : color;
        var pointBorderColor = hidePoints ? new SKColor(color.Red, color.Green, color.Blue, 0) : paints.TopologyNodeBorder.Color;
        paints.Dot.Color = pointColor;
        paints.SharedFramePoint.Color = pointColor;
        paints.BranchNode.Color = pointColor;
        paints.MergeNode.Color = pointColor;
        paints.PathGraphTerminalNode.Color = pointColor;
        paints.PathGraphBranchNode.Color = pointColor;
        if (hidePoints)
        {
            paints.Basis.Color = pointColor;
            paints.TopologyNodeBorder.Color = pointBorderColor;
        }

        return paints;
    }

    private static SKColor Blue(byte alpha) => new(36, 91, 148, alpha);

    private static SKColor Gray(byte alpha) => new(84, 90, 98, alpha);

    private static SKColor CurrentGray(byte alpha) => new(48, 58, 68, alpha);

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

    private static void ApplyStroke(SKPaint paint, SKColor color, float strokeWidth)
    {
        paint.Color = color;
        paint.StrokeWidth = strokeWidth;
    }

    private static void DrawTextLine(SKCanvas canvas, string text, float x, float baseline, float maxWidth, SKPaint paint)
    {
        if (paint.MeasureText(text) <= maxWidth)
        {
            canvas.DrawText(text, x, baseline, paint);
            return;
        }

        var ellipsis = "...";
        var trimmed = text;
        while (trimmed.Length > 0 && paint.MeasureText(trimmed + ellipsis) > maxWidth)
        {
            trimmed = trimmed[..^1];
        }

        canvas.DrawText(trimmed + ellipsis, x, baseline, paint);
    }

    private readonly record struct OffsetSideWitnessRead(
        IElement Source,
        IElement Boundary,
        IElement PositiveSide,
        IElement InverseSide);
}
