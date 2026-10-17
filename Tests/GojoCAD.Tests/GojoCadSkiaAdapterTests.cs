using Gojo.Core;
using GojoCAD.Structures;
using GojoDraw;
using SkiaSharp;

namespace GojoCAD.Tests;

public sealed class GojoCadSkiaAdapterTests
{
    [Fact]
    public void AreaUnionCutsEveryBoundaryCarriedByOneAntiAreaParticipant()
    {
        var basis = new TestBasis();
        var exterior = Rectangle(basis, 10, 230, 10, 230);
        var openings = Relation.OrderedGroup(
            new Carrier(),
            Circle(basis, 55, 55, 18),
            Circle(basis, 185, 55, 18),
            Circle(basis, 55, 185, 18),
            Circle(basis, 185, 185, 18));
        var areaFrame = GraphicAreaParticipationSurface.AreaFrame(
            new Carrier(),
            GraphicStructure.AreaFill(exterior),
            GraphicStructure.AntiAreaFill(openings));
        var union = GraphicAreaParticipationSurface.AreaUnion(
            new Carrier(),
            areaFrame,
            Relation.OrderedGroup(new Carrier(), exterior, openings));

        using var bitmap = new SKBitmap(240, 240);
        using var canvas = new SKCanvas(bitmap);
        using var paints = new GojoCadSkiaPaints
        {
            HideAreaBoundary = true,
            HideSharedFrameGeometry = true
        };
        paints.AreaFill.Color = SKColors.SteelBlue;
        paints.AntiAreaFill.Color = SKColors.White;
        canvas.Clear(SKColors.White);

        var drawn = new GojoCadSkiaAdapter().DrawElement(
            canvas,
            union,
            new DirectProjection(),
            paints);

        Assert.True(drawn > 0);
        Assert.Equal(SKColors.SteelBlue, bitmap.GetPixel(120, 120));
        Assert.Equal(SKColors.White, bitmap.GetPixel(55, 55));
        Assert.Equal(SKColors.White, bitmap.GetPixel(185, 55));
        Assert.Equal(SKColors.White, bitmap.GetPixel(55, 185));
        Assert.Equal(SKColors.White, bitmap.GetPixel(185, 185));
    }

    private static Relation Rectangle(TestBasis basis, long minX, long maxX, long minY, long maxY) =>
        Chain(
            GraphicStructure.FrameSegment(basis.P(minX, minY), basis.P(maxX, minY)),
            GraphicStructure.FrameSegment(basis.P(maxX, minY), basis.P(maxX, maxY)),
            GraphicStructure.FrameSegment(basis.P(maxX, maxY), basis.P(minX, maxY)),
            GraphicStructure.FrameSegment(basis.P(minX, maxY), basis.P(minX, minY)));

    private static Relation Circle(TestBasis basis, long centerX, long centerY, long radius) =>
        GraphicStructure.FrameCircle(basis.P(centerX, centerY), basis.P(centerX + radius, centerY));

    private static Relation Chain(params IElement[] elements)
    {
        IElement result = elements[^1];
        for (var index = elements.Length - 2; index >= 0; index--)
        {
            result = new Relation(elements[index], result);
        }

        return (Relation)result;
    }

    private sealed class TestBasis
    {
        private readonly Carrier frame = new();
        private readonly Carrier x = new();
        private readonly Carrier y = new();

        public Relation P(long xTicks, long yTicks) =>
            GraphicStructure.FramePoint(
                frame,
                new CarrierLocation(x, xTicks),
                new CarrierLocation(y, yTicks));
    }

    private sealed class DirectProjection : IGojoCadSkiaProjection
    {
        public SKPoint Project(CarrierLocation location) => new(location.Ticks, 0f);

        public SKPoint ProjectFrame(IReadOnlyList<CarrierLocation> coordinates) =>
            new(coordinates[0].Ticks, coordinates[1].Ticks);

        public SKPoint ProjectFrame(SharedFramePointRead point) => ProjectFrame(point.Coordinates);

        public bool UsesSampledProjection(CarrierLocation location) => false;

        public bool UsesSampledCarrierShape(Carrier carrier) => false;
    }
}
