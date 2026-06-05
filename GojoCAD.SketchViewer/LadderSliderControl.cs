using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace GojoCAD.SketchViewer;

internal sealed class LadderSliderControl : Control
{
    private const float TrackPadding = 18f;
    private int maximum;
    private int value;

    public LadderSliderControl()
    {
        Focusable = true;
        ClipToBounds = false;
        Width = 64;
        Height = 320;
    }

    public event Action<int>? ValueChanged;

    public int Maximum
    {
        get => maximum;
        set
        {
            maximum = Math.Max(0, value);
            Value = this.value;
            InvalidateVisual();
        }
    }

    public int Value
    {
        get => value;
        set
        {
            var next = Math.Clamp(value, 0, maximum);
            if (this.value == next)
            {
                return;
            }

            this.value = next;
            InvalidateVisual();
            ValueChanged?.Invoke(next);
        }
    }

    public void SetRange(int max, int selected)
    {
        maximum = Math.Max(0, max);
        value = Math.Clamp(selected, 0, maximum);
        InvalidateMeasure();
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.Custom(new LadderSliderDrawOperation(new Rect(Bounds.Size), maximum, value));
    }

    protected override Size MeasureOverride(Size availableSize) => new(64, 320);

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        e.Pointer.Capture(this);
        SetValueFromPoint(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (e.Pointer.Captured == this)
        {
            SetValueFromPoint(e.GetPosition(this));
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.Pointer.Captured == this)
        {
            SetValueFromPoint(e.GetPosition(this));
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key is Key.Up or Key.Right)
        {
            Value++;
            e.Handled = true;
        }
        else if (e.Key is Key.Down or Key.Left)
        {
            Value--;
            e.Handled = true;
        }
    }

    private void SetValueFromPoint(Point point)
    {
        if (maximum == 0)
        {
            Value = 0;
            return;
        }

        var height = Math.Max(1, Bounds.Height);
        var usable = Math.Max(1, height - TrackPadding * 2);
        var normalized = Math.Clamp((height - TrackPadding - point.Y) / usable, 0, 1);
        Value = (int)Math.Round(normalized * maximum);
    }
}

internal sealed class LadderSliderDrawOperation : ICustomDrawOperation
{
    private readonly int maximum;
    private readonly int value;

    public LadderSliderDrawOperation(Rect bounds, int maximum, int value)
    {
        Bounds = bounds;
        this.maximum = Math.Max(0, maximum);
        this.value = Math.Clamp(value, 0, this.maximum);
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
        var bounds = new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height);
        Draw(canvas, bounds);
    }

    private void Draw(SKCanvas canvas, SKRect bounds)
    {
        var centerX = bounds.Left + bounds.Width * 0.5f;
        var top = bounds.Top + 18f;
        var bottom = bounds.Bottom - 18f;
        var count = maximum + 1;

        using var track = Stroke(new SKColor(111, 118, 128, 150), 2f);
        using var tick = Stroke(new SKColor(111, 118, 128, 185), 1.4f);
        using var active = Stroke(new SKColor(36, 91, 148, 245), 3f);
        using var thumbFill = Fill(new SKColor(36, 91, 148, 250));
        using var thumbBorder = Stroke(new SKColor(255, 255, 255, 235), 2f);

        canvas.DrawLine(centerX, top, centerX, bottom, track);
        for (var index = 0; index < count; index++)
        {
            var y = YFor(index, top, bottom);
            var length = index == value ? 13f : 8f;
            canvas.DrawLine(centerX - length, y, centerX + length, y, tick);
        }

        var selectedY = YFor(value, top, bottom);
        canvas.DrawLine(centerX, selectedY, centerX, bottom, active);
        canvas.DrawCircle(centerX, selectedY, 8f, thumbFill);
        canvas.DrawCircle(centerX, selectedY, 8f, thumbBorder);
    }

    private float YFor(int index, float top, float bottom)
    {
        if (maximum == 0)
        {
            return bottom;
        }

        var normalized = index / (float)maximum;
        return bottom - (bottom - top) * normalized;
    }

    private static SKPaint Fill(SKColor color) => new()
    {
        IsAntialias = true,
        Color = color,
        Style = SKPaintStyle.Fill
    };

    private static SKPaint Stroke(SKColor color, float width) => new()
    {
        IsAntialias = true,
        Color = color,
        StrokeWidth = width,
        StrokeCap = SKStrokeCap.Round,
        Style = SKPaintStyle.Stroke
    };
}
