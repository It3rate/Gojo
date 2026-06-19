using ScratchMill.Utils;
using SkiaSharp;

namespace ScratchMill.KiCad;

public class KcPad : IKcCopperElement, IKcDrillable
{

    public static KcPad CreateProbePad(float x, float y, float size, string layer)
    {
        var result = new KcPad();
        result.PadName = "ProbePad";
        result.Type = "smd";
        result.Shape = "roundrect";
        result.PadNet = new KcNet(99999, "ProbePad");
        result.RoundrectRatio = 0.1f;
        result.PadOffset = new KcPosition(x, y);
        result.Size = new KcSize(size, size);
        result.IsThroughHole = false;
        result.Layers.Add(layer);
        result.UUID = Guid.NewGuid().ToString();

        return result;
    }
    public KcPosition PadOffset { get; set; } = new KcPosition(0, 0, 0);
    public KcPosition FootprintOffset { get; set; } = new KcPosition(0, 0, 0);
    public KcPosition At
    {
        get
        {
            var x = PadOffset.X;
            var y = PadOffset.Y;
            if (FootprintOffset.Orientation != 0 && (PadOffset.X != 0 || PadOffset.Y != 0))
            {
                var thetaRadians = -FootprintOffset.Orientation * (Math.PI / 180.0);
                var cosTheta = Math.Cos(thetaRadians);
                var sinTheta = Math.Sin(thetaRadians);
                var xRot = x * cosTheta - y * sinTheta;
                var yRot = x * sinTheta + y * cosTheta;
                x = xRot;
                y = yRot;
            }
            return new KcPosition(x + FootprintOffset.X, y + FootprintOffset.Y);
        }
    }
    public KcSize Size { get; set; }
    public double DrillSize { get; set; }
    public string PadName { get; set; }
    public string Type { get; set; }
    public string Shape { get; set; }
    public bool Locked { get; set; }
    public List<string> Layers { get; set; } = new List<string>();
    public bool RemoveUnusedLayers { get; set; }
    public double RoundrectRatio { get; set; }
    public string PinFunction { get; set; }
    public string PinType { get; set; }
    public KcNet PadNet { get; set; }
    public KcTeardrop Teardrop { get; set; }
    public string UUID { get; set; }

    public SKPoint HoleLocation { get; private set; }
    public float HoleSize { get; private set; }
    public int NetId => PadNet != null ? PadNet.Id : -1;

    public double RectArea => Size.Width * Size.Height;
    public bool IsThroughHole { get; private set; }

    //public bool IsOnLayer(KcLayer layer) => Layers.Any(name => name == layer.Name);
    public bool IsOnLayer(KcLayer layer)
    {
        var result = Layers.Any(name => name == layer.Name);
        if (!result) // can be *.mask rather than F.mask, etc
        {
            int lastIndex = layer.Name.LastIndexOf('.');
            var suffix = lastIndex != -1 && lastIndex < layer.Name.Length - 1 ? layer.Name.Substring(lastIndex + 1) : string.Empty;
            result = Layers.Any(name => name == "*." + suffix);
        }
        return result;
    }
    public bool IsOnNet(KcNet net) => PadNet.Id == net.Id;

    public SKPoint Center { get; private set; }
    public SKPath PadPath(SKMatrix transform)
    {
        var result = new SKPath();
        var curPt = At.SKPointTransformed(transform);
        Center = curPt;
        //result.MoveTo(curPt); // circles and rects don't need a moveto
        var radius = (float)Size.Width * transform.ScaleX * .5f;
        if (Shape.Contains("rect"))
        {
            float halfWidth = radius;
            float halfHeight = (float)Size.Height * transform.ScaleX * .5f;
            var orientation = FootprintOffset.Orientation;
            // todo: only parsing 90 degree rotations for now.
            // Footprints need to render normally and then apply scale and rotate etc.
            if (Math.Abs(orientation) == 90)
            {
                var temp = halfWidth;
                halfWidth = halfHeight;
                halfHeight = temp;
            }
            var rect = new SKRect(
                curPt.X - halfWidth, curPt.Y - halfHeight,
                curPt.X + halfWidth, curPt.Y + halfHeight);
            if (Shape == "rect")
            {
                result.AddRect(rect);
            }
            else // roundrect
            {
                var rrect = new SKRoundRect(rect, (float)RoundrectRatio);
                result.AddRoundRect(rrect);
            }
        }
        else
        {
            result.AddCircle(curPt.X, curPt.Y, radius);
        }

        if (IsThroughHole)
        {
            HoleLocation = curPt;
            HoleSize = (float)DrillSize * transform.ScaleX * .5f;
        }
        return result;
    }

    public SKPath PadHolePath()
    {
        var result = new SKPath();
        result.MoveTo(HoleLocation);
        result.AddCircle(HoleLocation.X, HoleLocation.Y, HoleSize);
        return result;
    }

    public override string ToString() => $"pad{NetId}[{At.X} {At.Y}] fOffset[{FootprintOffset.X} {FootprintOffset.Y}] pOffset[{PadOffset.X} {PadOffset.Y}]";



    public static KcPad Parse(KcFootprint? footprint, List<object> items)
    {
        var result = new KcPad();

        result.PadName = (string)items[1];
        result.Type = (string)items[2];
        result.Shape = (string)items[3];
        result.IsThroughHole = result.Type == "thru_hole";

        foreach (var item in items)
        {
            if (item is List<object> list && list.Count > 1)
            {
                string name = (string)list[0];
                switch (name)
                {
                    case "at":
                        result.PadOffset = KcPosition.Parse(list);
                        break;
                    case "size":
                        result.Size = KcSize.Parse(list);
                        break;
                    case "drill":
                        result.DrillSize = Convert.ToDouble(list[1]);
                        break;
                    case "layers":
                        for (int i = 1; i < list.Count; i++)
                        {
                            result.Layers.Add((string)list[i]);
                        }
                        break;
                    case "remove_unused_layers":
                        result.RemoveUnusedLayers = Convert.ToBoolean(list[1]);
                        break;
                    case "roundrect_rratio":
                        result.RoundrectRatio = Convert.ToDouble(list[1]);
                        break;
                    case "net":
                        result.PadNet = KcNet.Parse(list);
                        break;
                    case "pinfunction":
                        result.PinFunction = (string)list[1];
                        break;
                    case "pintype":
                        result.PinType = (string)list[1];
                        break;
                    case "teardrops":
                        result.Teardrop = KcTeardrop.Parse(list);
                        break;
                    case "uuid":
                    case "tstamp":
                        result.UUID = (string)list[1];
                        break;
                }
            }
        }
        return result;
    }
}
