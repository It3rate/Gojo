using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.VisualTree;
using Gojo.Core;
using Gojo.Structures;
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
                current.ShowConstructionPoints));
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
    private static readonly SKColor RadialMergedFill = new(58, 128, 142, 118);
    private static readonly SKColor[] RadialSeparateFillPalette =
    [
        new(63, 126, 96, 118),
        new(48, 118, 154, 112),
        new(179, 113, 58, 104),
        new(142, 84, 148, 102),
        new(198, 74, 75, 96),
        new(89, 135, 188, 104),
        new(109, 148, 68, 104)
    ];
    private readonly SketchScene scene;
    private readonly int selectedRungIndex;
    private readonly bool showConstructionPoints;

    public SketchDrawOperation(Rect bounds, SketchScene scene, int selectedRungIndex, bool showConstructionPoints)
    {
        Bounds = bounds;
        this.scene = scene;
        this.selectedRungIndex = selectedRungIndex;
        this.showConstructionPoints = showConstructionPoints;
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

        for (var index = 0; index < boundedRung; index++)
        {
            var distance = boundedRung - index;
            var alpha = (byte)Math.Clamp(122 - distance * 18, 30, 104);
            DrawRung(canvas, rungs[index], index, viewport, Gray(alpha), 1.75f, hidePoints: !showConstructionPoints, useSpecialAreaPaints: false);
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

        ReadAreaPaints(rungIndex, useSpecialAreaPaints, out var areaPalette, out var areaFillColor, out var antiAreaFillColor);
        using var paints = CreateSketchPaints(color, strokeWidth, hidePoints, areaPalette, areaFillColor, antiAreaFillColor);
        Renderer.Draw(canvas, result, viewport, scene.ProjectionSeed, scene.ModelBounds, paints);
    }

    private void ReadAreaPaints(
        int rungIndex,
        bool useSpecialAreaPaints,
        out IReadOnlyList<SKColor>? areaPalette,
        out SKColor? areaFillColor,
        out SKColor? antiAreaFillColor)
    {
        areaPalette = null;
        areaFillColor = null;
        antiAreaFillColor = null;
        if (!useSpecialAreaPaints ||
            scene.Label != "Radial Shape Ladder")
        {
            return;
        }

        if (rungIndex == 3)
        {
            areaPalette = RadialSeparateFillPalette;
            antiAreaFillColor = CanvasBackground;
        }
        else if (rungIndex == 4)
        {
            areaFillColor = RadialMergedFill;
            antiAreaFillColor = CanvasBackground;
        }
    }

    private static GojoCadSkiaPaints CreateSketchPaints(
        SKColor color,
        float strokeWidth,
        bool hidePoints = false,
        IReadOnlyList<SKColor>? areaFillPalette = null,
        SKColor? areaFillColor = null,
        SKColor? antiAreaFillColor = null)
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
            AreaFillPalette = areaFillPalette
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
}
