using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Gojo.GearShapeLadder;

internal sealed class GearLadderSlider : Control
{
    private const float TrackPadding = 18f;
    private int maximum;
    private int value;

    public GearLadderSlider()
    {
        Focusable = true;
        Width = 64;
        Height = 230;
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

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.Custom(new GearLadderDrawOperation(new Rect(Bounds.Size), maximum, value));
    }

    protected override Size MeasureOverride(Size availableSize) => new(64, 230);

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        e.Pointer.Capture(this);
        SetValue(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (e.Pointer.Captured == this)
        {
            SetValue(e.GetPosition(this));
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.Pointer.Captured == this)
        {
            SetValue(e.GetPosition(this));
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

    private void SetValue(Point point)
    {
        var usable = Math.Max(1, Bounds.Height - TrackPadding * 2);
        var normalized = Math.Clamp((Bounds.Height - TrackPadding - point.Y) / usable, 0, 1);
        Value = maximum == 0 ? 0 : (int)Math.Round(normalized * maximum);
    }
}

internal sealed class GearLadderDrawOperation(Rect bounds, int maximum, int value) : ICustomDrawOperation
{
    public Rect Bounds { get; } = bounds;

    public void Dispose() { }

    public bool HitTest(Point p) => Bounds.Contains(p);

    public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);

    public void Render(ImmediateDrawingContext context)
    {
        if (context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) is not ISkiaSharpApiLeaseFeature feature)
        {
            return;
        }

        using var lease = feature.Lease();
        var canvas = lease.SkCanvas;
        var centerX = (float)Bounds.Width * 0.5f;
        var top = 18f;
        var bottom = (float)Bounds.Height - 18f;
        using var track = Stroke(new SKColor(129, 133, 137), 2f);
        using var tick = Stroke(new SKColor(129, 133, 137), 1.5f);
        using var active = Stroke(new SKColor(63, 94, 124), 3f);
        using var thumb = Fill(new SKColor(63, 94, 124));
        canvas.DrawLine(centerX, top, centerX, bottom, track);
        for (var index = 0; index <= maximum; index++)
        {
            var y = YFor(index, maximum, top, bottom);
            var length = index == value ? 13f : 8f;
            canvas.DrawLine(centerX - length, y, centerX + length, y, tick);
        }

        var selected = YFor(value, maximum, top, bottom);
        canvas.DrawLine(centerX, selected, centerX, bottom, active);
        canvas.DrawCircle(centerX, selected, 8f, thumb);
    }

    private static float YFor(int index, int maximum, float top, float bottom) =>
        maximum == 0 ? bottom : bottom - (bottom - top) * index / maximum;

    private static SKPaint Fill(SKColor color) => new() { IsAntialias = true, Color = color, Style = SKPaintStyle.Fill };

    private static SKPaint Stroke(SKColor color, float width) => new()
    {
        IsAntialias = true,
        Color = color,
        StrokeWidth = width,
        StrokeCap = SKStrokeCap.Round,
        Style = SKPaintStyle.Stroke
    };
}
