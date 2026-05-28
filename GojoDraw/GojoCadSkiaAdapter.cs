using Gojo.Core;
using SkiaSharp;

namespace GojoDraw;

/// <summary>
/// Minimal Skia adapter that traverses GojoCAD relation structure and renders
/// low-level physical forms it recognizes.
/// </summary>
public sealed class GojoCadSkiaAdapter
{
    public int DrawElement(
        SKCanvas canvas,
        IElement element,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(paints);

        return DrawElementCore(canvas, element, projection, paints);
    }

    private static int DrawElementCore(
        SKCanvas canvas,
        IElement element,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints)
    {
        if (element is not Relation relation)
        {
            return 0;
        }

        if (TryReadRadialFlow(relation, out var radialPoint) &&
            DrawRadialFlow(canvas, radialPoint, projection, paints))
        {
            return 1 + DrawElementCore(canvas, radialPoint, projection, paints);
        }

        if (TryReadDirectedSegment(relation, out var inverseStart, out var positiveEnd) &&
            DrawDirectedSegment(canvas, inverseStart, positiveEnd, projection, paints))
        {
            return 1;
        }

        if (TryReadPositiveSegment(relation, out var start, out var end) &&
            DrawPositiveSegment(canvas, start, end, projection, paints))
        {
            return 1;
        }

        if (TryReadPoint(relation, out var point) &&
            DrawDot(canvas, point, projection, paints))
        {
            return 1;
        }

        return
            DrawElementCore(canvas, relation.Left, projection, paints) +
            DrawElementCore(canvas, relation.Right, projection, paints);
    }

    private static bool TryReadPoint(Relation relation, out CarrierLocation location)
    {
        if (relation.Left is CarrierLocation candidate &&
            relation.Right is Carrier carrier &&
            candidate.Carrier.SameIdentityAs(carrier) &&
            !candidate.Carrier.IsUnknown)
        {
            location = candidate;
            return true;
        }

        location = default;
        return false;
    }

    private static bool TryReadPositiveSegment(
        Relation relation,
        out CarrierLocation start,
        out CarrierLocation end)
    {
        if (relation.Left is Relation startPoint &&
            relation.Right is Relation endPoint &&
            TryReadPoint(startPoint, out start) &&
            TryReadPoint(endPoint, out end) &&
            start.Carrier.SameIdentityAs(end.Carrier) &&
            !start.Carrier.IsInverse &&
            !end.Carrier.IsInverse)
        {
            return true;
        }

        start = default;
        end = default;
        return false;
    }

    private static bool TryReadDirectedSegment(
        Relation relation,
        out CarrierLocation inverseStart,
        out CarrierLocation positiveEnd)
    {
        if (relation.Left is Relation startPoint &&
            relation.Right is Relation endPoint &&
            TryReadPointOnAnyCarrier(startPoint, out inverseStart) &&
            TryReadPointOnAnyCarrier(endPoint, out positiveEnd) &&
            inverseStart.Carrier.IsInverse &&
            !positiveEnd.Carrier.IsInverse &&
            inverseStart.Carrier.Inverse().SameIdentityAs(positiveEnd.Carrier))
        {
            return true;
        }

        inverseStart = default;
        positiveEnd = default;
        return false;
    }

    private static bool TryReadRadialFlow(Relation relation, out Relation point)
    {
        if (relation.Left is Relation sourcePoint &&
            TryReadPoint(sourcePoint, out _) &&
            relation.Right is Relation outlet &&
            TryReadUnknownZeroSegment(outlet))
        {
            point = sourcePoint;
            return true;
        }

        point = null!;
        return false;
    }

    private static bool TryReadUnknownZeroSegment(Relation relation)
    {
        return
            relation.Left is Relation startPoint &&
            relation.Right is Relation endPoint &&
            TryReadPointOnAnyCarrier(startPoint, out var start) &&
            TryReadPointOnAnyCarrier(endPoint, out var end) &&
            start.Carrier.IsUnknown &&
            end.Carrier.IsUnknown &&
            start.Ticks == 0 &&
            end.Ticks == 0;
    }

    private static bool TryReadPointOnAnyCarrier(Relation relation, out CarrierLocation location)
    {
        if (relation.Left is CarrierLocation candidate &&
            relation.Right is Carrier carrier &&
            candidate.Carrier.SameIdentityAs(carrier))
        {
            location = candidate;
            return true;
        }

        location = default;
        return false;
    }

    private static bool DrawDot(
        SKCanvas canvas,
        CarrierLocation location,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints)
    {
        if (!projection.TryProject(location, out var center))
        {
            return false;
        }

        canvas.DrawCircle(center, paints.DotRadius, paints.Dot);
        return true;
    }

    private static bool DrawRadialFlow(
        SKCanvas canvas,
        Relation point,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints)
    {
        if (!TryReadPoint(point, out var location) ||
            !projection.TryProject(location, out var center))
        {
            return false;
        }

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(
                center,
                paints.RadialRadius,
                [paints.RadialInner, paints.RadialOuter],
                [0f, 1f],
                SKShaderTileMode.Clamp),
            Style = SKPaintStyle.Fill
        };
        canvas.DrawCircle(center, paints.RadialRadius, paint);
        return true;
    }

    private static bool DrawPositiveSegment(
        SKCanvas canvas,
        CarrierLocation start,
        CarrierLocation end,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints)
    {
        if (!projection.TryProject(start, out var startPoint) ||
            !projection.TryProject(end, out var endPoint))
        {
            return false;
        }

        canvas.DrawLine(startPoint, endPoint, paints.Segment);
        DrawEndpointBar(canvas, startPoint, endPoint, paints);
        DrawEndpointBar(canvas, endPoint, startPoint, paints);
        return true;
    }

    private static bool DrawDirectedSegment(
        SKCanvas canvas,
        CarrierLocation inverseStart,
        CarrierLocation positiveEnd,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints)
    {
        if (!projection.TryProject(inverseStart, out var startPoint) ||
            !projection.TryProject(positiveEnd, out var endPoint))
        {
            return false;
        }

        canvas.DrawLine(startPoint, endPoint, paints.DirectedSegment);
        DrawArrowTip(canvas, startPoint, endPoint, paints);
        return true;
    }

    private static void DrawEndpointBar(
        SKCanvas canvas,
        SKPoint point,
        SKPoint toward,
        GojoCadSkiaPaints paints)
    {
        var perpendicular = PerpendicularUnit(point, toward);
        var half = paints.EndpointBarLength * 0.5f;
        canvas.DrawLine(
            new SKPoint(point.X - perpendicular.X * half, point.Y - perpendicular.Y * half),
            new SKPoint(point.X + perpendicular.X * half, point.Y + perpendicular.Y * half),
            paints.EndpointBar);
    }

    private static void DrawArrowTip(
        SKCanvas canvas,
        SKPoint start,
        SKPoint end,
        GojoCadSkiaPaints paints)
    {
        var direction = Unit(start, end);
        var perpendicular = new SKPoint(-direction.Y, direction.X);
        var back = new SKPoint(
            end.X - direction.X * paints.ArrowLength,
            end.Y - direction.Y * paints.ArrowLength);
        var left = new SKPoint(
            back.X + perpendicular.X * paints.ArrowWidth,
            back.Y + perpendicular.Y * paints.ArrowWidth);
        var right = new SKPoint(
            back.X - perpendicular.X * paints.ArrowWidth,
            back.Y - perpendicular.Y * paints.ArrowWidth);

        canvas.DrawLine(end, left, paints.DirectedSegment);
        canvas.DrawLine(end, right, paints.DirectedSegment);
    }

    private static SKPoint PerpendicularUnit(SKPoint start, SKPoint end)
    {
        var unit = Unit(start, end);
        return new SKPoint(-unit.Y, unit.X);
    }

    private static SKPoint Unit(SKPoint start, SKPoint end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = MathF.Sqrt(dx * dx + dy * dy);
        return length <= float.Epsilon
            ? new SKPoint(1, 0)
            : new SKPoint(dx / length, dy / length);
    }
}
