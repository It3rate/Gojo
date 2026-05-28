using SkiaSharp;

namespace GojoDraw;

/// <summary>
/// Temporary render-boundary paints until GojoCAD has style participation
/// surfaces for color, radius, and width.
/// </summary>
public sealed class GojoCadSkiaPaints : IDisposable
{
    public GojoCadSkiaPaints()
    {
        Dot = Fill(new SKColor(31, 45, 61));
        Segment = Stroke(new SKColor(42, 91, 143), 5f);
        DirectedSegment = Stroke(new SKColor(63, 113, 83), 5f);
        EndpointBar = Stroke(new SKColor(128, 132, 137), 3f);
        RadialInner = new SKColor(162, 64, 46, 110);
        RadialOuter = new SKColor(162, 64, 46, 0);
    }

    public SKPaint Dot { get; }

    public SKPaint Segment { get; }

    public SKPaint DirectedSegment { get; }

    public SKPaint EndpointBar { get; }

    public float DotRadius { get; init; } = 9f;

    public float RadialRadius { get; init; } = 58f;

    public SKColor RadialInner { get; init; }

    public SKColor RadialOuter { get; init; }

    public float EndpointBarLength { get; init; } = 17f;

    public float ArrowLength { get; init; } = 18f;

    public float ArrowWidth { get; init; } = 8f;

    public void Dispose()
    {
        Dot.Dispose();
        Segment.Dispose();
        DirectedSegment.Dispose();
        EndpointBar.Dispose();
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
        StrokeJoin = SKStrokeJoin.Round,
        Style = SKPaintStyle.Stroke
    };
}
