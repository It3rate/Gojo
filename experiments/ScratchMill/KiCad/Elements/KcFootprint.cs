using ScratchMill.KiCad.Graphics;
using System.Diagnostics;

namespace ScratchMill.KiCad;

public class KcFootprint : IKcCopperElement
{
    public string Name { get; set; }
    public string Layer { get; set; }
    public string Tedit { get; set; }
    public string UUID { get; set; }
    public KcPosition At { get; set; } = new KcPosition();
    public string Description { get; set; }
    public string Tags { get; set; }
    public List<KcProperty> Properties { get; set; } = new List<KcProperty>();
    public string Attr { get; set; }
    public string Path { get; set; }
    public string Sheetname { get; set; }
    public string Sheetfile { get; set; }
    public List<KcFpText> FpTexts { get; set; } = new List<KcFpText>();
    public List<KcFpLine> FpLines { get; set; } = new List<KcFpLine>();
    public List<KcFpRect> FpRects { get; set; } = new List<KcFpRect>();
    public List<KcFpCircle> FpCircles { get; set; } = new List<KcFpCircle>();
    public List<KcFpArc> FpArcs { get; set; } = new List<KcFpArc>();
    public List<KcPad> Pads { get; set; } = new List<KcPad>();
    public KcModel Model { get; set; } = new KcModel();

    public int NetId => Pads.Count > 0 ? Pads[0].NetId : -1;
    public bool IsOnLayer(KcLayer layer) => Pads.Any(pad => pad.IsOnLayer(layer));
    public bool IsOnNet(KcNet net) => Pads.Any(pad => pad.IsOnNet(net));

    public override string ToString() => $"foot{NetId}[{At.X} {At.Y}]";

    public static KcFootprint Parse(List<object> items)
    {
        var result = new KcFootprint();
        result.Name = (string)items[0];
        foreach (var item in items)
        {
            if (item is List<object> list && list.Count > 1)
            {
                string name = (string)list[0];
                switch (name)
                {
                    case "layer":
                        result.Layer = (string)list[1];
                        break;
                    case "at":
                        result.At = KcPosition.Parse(list);
                        break;
                    case "descr":
                        //result.Description = ;
                        break;
                    case "tags":
                        result.Tags = (string)list[1];
                        break;
                    case "property":
                        result.Properties.Add(KcProperty.Parse(list));
                        break;
                    case "path":
                        result.Path = (string)list[1];
                        break;
                    case "sheetname":
                        result.Sheetname = (string)list[1];
                        break;
                    case "sheetfile":
                        result.Sheetfile = (string)list[1];
                        break;
                    case "attr":
                        result.Attr = (string)list[1];
                        break;
                    case "fp_line":
                        result.FpLines.Add(KcFpLine.Parse(list));
                        break;
                    case "fp_rect":
                        result.FpRects.Add(KcFpRect.Parse(list));
                        break;
                    case "fp_circle":
                        result.FpCircles.Add(KcFpCircle.Parse(list));
                        break;
                    case "fp_arc":
                        result.FpArcs.Add(KcFpArc.Parse(list));
                        break;
                    case "fp_text":
                        break;
                    case "fp_poly":
                        break;
                    case "embedded_fonts":
                        break;
                    case "pad":
                        result.Pads.Add(KcPad.Parse(result, list));
                        break;
                    case "model":
                        //result.Model = ;
                        break;
                    case "uuid":
                    case "tstamp":
                        result.UUID = (string)list[1];
                        break;
                    default:
                        Trace.WriteLine("Missing footprint parse: " + name);
                        break;
                }
            }
        }

        foreach (var pad in result.Pads)
        {
            pad.FootprintOffset = result.At;
        }
        return result;
    }
}
