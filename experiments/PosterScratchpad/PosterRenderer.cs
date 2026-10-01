using Gojo.Core;
using Gojo.Encoding;
using GojoCAD;
using SkiaSharp;
using static PosterScratchpad.PosterSurfaces;

namespace PosterScratchpad;

// All SK/double/pixel/font values below are derived at the render boundary.
// This adapter handles the experiment's local color/text surfaces; GojoDraw's
// generic paints do not yet have a structural typography/style reader.
internal static class PosterRenderer
{
    internal static void Render(Relation document, string path)
    {
        using var surface = SKSurface.Create(new SKImageInfo(1080, 1440));
        surface.Canvas.Clear(SKColors.Transparent);
        DrawGroup(surface.Canvas, (Relation)Parts(document)[1]);
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var output = File.Create(path);
        data.SaveTo(output);
    }

    internal static void DrawGroup(SKCanvas canvas, Relation group)
    {
        foreach (var element in Parts(group))
        {
            var layer = (Relation)element;
            if (layer.Right is Carrier)
            {
                DrawGroup(canvas, layer);
                continue;
            }
            var color = Parts((Relation)layer.Right).Cast<CarrierLocation>().Select(p => p.Ticks).ToArray();
            using var paint = new SKPaint { IsAntialias = true, Color = new SKColor((byte)color[0], (byte)color[1], (byte)color[2]) };
            Draw(canvas, (Relation)layer.Left, paint);
        }
    }

    private static SKPoint Project(SharedFramePointRead point) =>
        new(point.Coordinates[0].Ticks, point.Coordinates[1].Ticks);

    private static void Draw(SKCanvas canvas, Relation source, SKPaint paint)
    {
        if (source.Right is Carrier)
        {
            var elements = Parts(source);
            if (elements.Length == 5 && EncodedTextSurface.TryRead(elements[0], out var text))
            {
                EncodedTextSurface.TryRead(elements[3], out var family);
                using var font = SKTypeface.FromFamilyName(family,
                    new SKFontStyle((int)((CarrierLocation)elements[4]).Ticks, 5, SKFontStyleSlant.Upright));
                paint.Typeface = font;
                paint.TextSize = ((CarrierLocation)elements[2]).Ticks;
                var baseline = GraphicReadGraph.From(elements[1]).ReadsOf<SharedFrameSegmentRead>()
                    .Single(p => ReferenceEquals(p.Source, elements[1]));
                var origin = Project(baseline.Start);
                var end = Project(baseline.End);
                var measure = paint.MeasureText(text);
                if (measure > end.X - origin.X)
                    paint.TextSize *= (end.X - origin.X) / measure;
                canvas.DrawText(text, origin, paint);
                return;
            }
            using var path = new SKPath();
            var first = true;
            foreach (var element in elements)
            {
                var edge = GraphicReadGraph.From(element).ReadsOf<SharedFrameSegmentRead>()
                    .Single(p => ReferenceEquals(p.Source, element));
                if (first) { path.MoveTo(Project(edge.Start)); first = false; }
                path.LineTo(Project(edge.End));
            }
            path.Close();
            canvas.DrawPath(path, paint);
            return;
        }
        var circle = GraphicReadGraph.From(source).ReadsOf<SharedFrameCircleRead>()
            .Single(p => ReferenceEquals(p.Source, source));
        var center = Project(circle.Center);
        var radiusPoint = Project(circle.RadiusPoint);
        canvas.DrawCircle(center, radiusPoint.X - center.X, paint);
    }
}
