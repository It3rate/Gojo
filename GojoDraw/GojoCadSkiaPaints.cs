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
        Circumference = Stroke(new SKColor(95, 103, 116, 160), 1.5f);
        CircumferenceSpan = Stroke(new SKColor(42, 91, 143, 150), 2.25f);
        EndpointBar = Stroke(new SKColor(128, 132, 137, 185), 2f);
        Partition = Stroke(new SKColor(82, 88, 96, 190), 1.75f);
        Basis = Stroke(new SKColor(70, 76, 84, 190), 1.5f);
        HoverBackground = Fill(new SKColor(255, 255, 255, 235));
        HoverBorder = Stroke(new SKColor(80, 86, 96, 210), 1f);
        HoverText = Fill(new SKColor(31, 37, 45));
        RadialInner = new SKColor(162, 64, 46, 110);
        RadialOuter = new SKColor(162, 64, 46, 0);
    }

    public SKPaint Dot { get; }

    public SKPaint Segment { get; }

    public SKPaint DirectedSegment { get; }

    public SKPaint Circumference { get; }

    public SKPaint CircumferenceSpan { get; }

    public SKPaint EndpointBar { get; }

    public SKPaint Partition { get; }

    public SKPaint Basis { get; }

    public SKPaint HoverBackground { get; }

    public SKPaint HoverBorder { get; }

    public SKPaint HoverText { get; }

    public float DotRadius { get; init; } = 9f;

    public float RadialRadius { get; init; } = 58f;

    public SKColor RadialInner { get; init; }

    public SKColor RadialOuter { get; init; }

    public float EndpointBarLength { get; init; } = 17f;

    public float PartitionTickLength { get; init; } = 15f;

    public float BasisSize { get; init; } = 10f;

    public float ArrowLength { get; init; } = 18f;

    public float ArrowWidth { get; init; } = 8f;

    public void Dispose()
    {
        Dot.Dispose();
        Segment.Dispose();
        DirectedSegment.Dispose();
        Circumference.Dispose();
        CircumferenceSpan.Dispose();
        EndpointBar.Dispose();
        Partition.Dispose();
        Basis.Dispose();
        HoverBackground.Dispose();
        HoverBorder.Dispose();
        HoverText.Dispose();
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
