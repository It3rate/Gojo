using ScratchMill.KiCad.Graphics;
using SkiaSharp;

namespace ScratchMill.GCode;

public class GraphicData : IKcData
{
    public List<IKcGraphic> Elements { get; } = new List<IKcGraphic>();
    private SKMatrix Transform { get; set; }
    public PassKind PassKind { get; set; }
    public bool IsRecutPass { get; set; } = false;

    public SKPath BasePath { get; private set; }
    public SKRect Bounds { get; private set; }
    public string GCode { get; set; }
    public string Svg { get; set; }
    public int ContourIndex { get; set; } = -1;
    public bool OverlapTail { get; set; } = false;
    public GraphicData(List<IKcGraphic> graphics, SKMatrix transform, PassKind passKind = PassKind.Graphic, int countourIndex = 0)
    {
        Elements.AddRange(graphics);
        Transform = transform;
        PassKind = passKind;
        ContourIndex = countourIndex;
    }
    public GraphicData(IKcGraphic graphic, SKMatrix transform, PassKind passKind = PassKind.Graphic, int countourIndex = 0) :
        this([graphic], transform, passKind, countourIndex)
    {
    }
    public void GeneratePath()
    {
        BasePath = ToSkia();
    }
    private SKPath ToSkia()
    {
        var result = new SKPath();
        foreach (var element in Elements)
        {
            var path = element.ToSkiaPath(Transform);
            if (!path.IsEmpty)
            {
                result.AddPath(path);
            }
        }

        if (Bounds.IsEmpty)
        {
            Bounds = result.Bounds;
        }
        else
        {
            Bounds.Union(result.Bounds);
        }

        return result;
    }
    private static SKPaint _wirePaint = new SKPaint
    {
        Style = SKPaintStyle.Stroke,
        StrokeCap = SKStrokeCap.Round,
        StrokeJoin = SKStrokeJoin.Round,
        StrokeWidth = 1, // to be set by wires
    };
}
