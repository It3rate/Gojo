using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.VisualTree;
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
            context.Custom(new SketchDrawOperation(new Rect(Bounds.Size), current.SelectedScene));
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
    private readonly SketchScene scene;

    public SketchDrawOperation(Rect bounds, SketchScene scene)
    {
        Bounds = bounds;
        this.scene = scene;
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
        canvas.Clear(new SKColor(249, 248, 244));
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
        using var paints = CreateSketchPaints();
        Renderer.Draw(canvas, scene.Structure, viewport, scene.ProjectionSeed, scene.ModelBounds, paints);
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
        DrawTextLine(canvas, scene.Caption, viewport.Left, 56f, viewport.Width, caption);
    }

    private static GojoCadSkiaPaints CreateSketchPaints()
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
            SheetSeedWidth = 12f
        };
        paints.Segment.StrokeWidth = 2.1f;
        paints.SharedFrameSegment.StrokeWidth = 2.25f;
        paints.SharedFrameCurve.StrokeWidth = 2.7f;
        paints.SharedFramePoint.Color = new SKColor(70, 76, 84, 145);
        return paints;
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
