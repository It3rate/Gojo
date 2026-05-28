using GojoCAD;
using SkiaSharp;

namespace GojoDraw;

/// <summary>
/// Minimal Skia adapter for GojoCAD graphic primitives. Style, projection, and
/// pixel values enter only at the render boundary.
/// </summary>
public sealed class GojoCadSkiaAdapter
{
    public bool DrawDot(
        SKCanvas canvas,
        Gojo.Core.Relation point,
        IGojoCadSkiaProjection projection,
        float radius,
        SKPaint paint)
    {
        if (!GraphicStructure.TryReadPoint(point, out var location) ||
            !projection.TryProject(location, out var center))
        {
            return false;
        }

        canvas.DrawCircle(center, radius, paint);
        return true;
    }

    public bool DrawRadialFlow(
        SKCanvas canvas,
        Gojo.Core.Relation radialFlow,
        IGojoCadSkiaProjection projection,
        float radius,
        SKColor inner,
        SKColor outer)
    {
        if (!GraphicStructure.TryReadRadialFlow(radialFlow, out var point) ||
            !GraphicStructure.TryReadPoint(point, out var location) ||
            !projection.TryProject(location, out var center))
        {
            return false;
        }

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(
                center,
                radius,
                [inner, outer],
                [0f, 1f],
                SKShaderTileMode.Clamp),
            Style = SKPaintStyle.Fill
        };
        canvas.DrawCircle(center, radius, paint);
        return true;
    }

    public bool DrawSegment(
        SKCanvas canvas,
        Gojo.Core.Relation segment,
        IGojoCadSkiaProjection projection,
        SKPaint paint)
    {
        if (!GraphicStructure.TryReadSegment(segment, out var start, out var end) ||
            !projection.TryProject(start, out var startPoint) ||
            !projection.TryProject(end, out var endPoint))
        {
            return false;
        }

        canvas.DrawLine(startPoint, endPoint, paint);
        return true;
    }

    public bool DrawDirectedSegment(
        SKCanvas canvas,
        Gojo.Core.Relation directedSegment,
        IGojoCadSkiaProjection projection,
        SKPaint paint)
    {
        if (!GraphicStructure.TryReadDirectedSegment(directedSegment, out var inverseStart, out var positiveEnd) ||
            !projection.TryProject(inverseStart, out var startPoint) ||
            !projection.TryProject(positiveEnd, out var endPoint))
        {
            return false;
        }

        canvas.DrawLine(startPoint, endPoint, paint);
        return true;
    }
}
