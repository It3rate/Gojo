using ScratchMill.KiCad.Design;
using ScratchMill.KiCad.Elements;
using ScratchMill.KiCad.Graphics;
using ScratchMill.Utils;
using SkiaSharp;
using System.Diagnostics;

namespace ScratchMill.KiCad;

public class KcPcb : IKiCadElement
{
    public int Version { get; set; }
    public string Generator { get; set; } = "";
    public string GeneratorVersion { get; set; } = "";
    public KcGeneral General { get; set; } = new KcGeneral();
    public string Paper { get; set; } = "";
    public List<KcLayer> Layers { get; set; } = new List<KcLayer>();
    public KcSetup Setup { get; set; } = new KcSetup();
    public List<KcNet> Nets { get; set; } = new List<KcNet>();
    public List<KcFootprint> Footprints { get; set; } = new List<KcFootprint>();
    public List<KcSegment> Segments { get; set; } = new List<KcSegment>();
    public List<KcArc> Arcs { get; set; } = new List<KcArc>();
    public List<KcVia> Vias { get; set; } = new List<KcVia>();
    public List<KcZone> Zones { get; set; } = new List<KcZone>();
    public List<KcGrBase> GrElements { get; set; } = new List<KcGrBase>();

    public (KcPosition, KcPosition) Bounds => GetEdgeBoundsBL_TR();
    public KcPosition OriginCenter { get; private set; } = new KcPosition(0, 0);

    public KcPosition SetOrigin()
    {
        OriginCenter = EdgeCutCenter();
        return OriginCenter;
    }
    public List<KcPad> Pads => Footprints.SelectMany(fp => fp.Pads).ToList();
    public KcLayer? GetLayer(string name)
    {
        return Layers.FirstOrDefault(layer => layer.Name == name);
    }
    public KcLayer? GetFrontLayer()
    {
        return Layers.FirstOrDefault(layer => layer.Name == "F.Cu");
    }
    public KcLayer? GetBackLayer()
    {
        return Layers.FirstOrDefault(layer => layer.Name == "B.Cu");
    }
    public KcLayer? GetEdgeCutsLayer()
    {
        return Layers.FirstOrDefault(layer => layer.Name == "Edge.Cuts");
    }
    public KcGraphics GetGraphicsForLayers(params string[] names)
    {
        var result = new KcGraphics();
        foreach (var name in names)
        {
            var layer = GetLayer(name);
            if (layer != null)
            {
                result.AddRange(GrElements.OfType<IKcGraphic>().
                    Where(ge => ge.Layer == name).
                    OfType<IKcGraphic>());
            }
        }
        return result;
    }

    public List<KcPad> GetPadDrillElements()
    {
        var result = new List<KcPad>();
        var pads = Pads.Where(pad => pad.IsThroughHole).ToList();
        result.AddRange(pads);
        return result;
    }
    public List<IKcDrillable> GetViaDrillElements()
    {
        var vias = Vias.Where(via => via.IsThroughHole).ToList();
        var result = new List<IKcDrillable>();
        result.AddRange(vias);
        return result;
    }
    public List<List<IKcLayerElement>> GetCopperLayers() => new List<List<IKcLayerElement>>()
    {
        Segments.OfType<IKcLayerElement>().ToList(),
        Arcs.OfType<IKcLayerElement>().ToList(),
        Pads.OfType<IKcLayerElement>().ToList(),
        Vias.OfType<IKcLayerElement>().ToList(),
        Zones.Where(zone=> zone.IsTeardrop).OfType<IKcLayerElement>().ToList(),
    };

    public Dictionary<int, KcNetTracks> GetTracksForLayer(string name)
    {
        var result = new Dictionary<int, KcNetTracks>();
        var copperElements = new Dictionary<int, List<IKcCopperElement>>();
        var layer = GetLayer(name);
        var elements = ElementsOnLayer(layer, GetCopperLayers().ToArray());
        foreach (var element in elements)
        {
            if (element is IKcCopperElement copper)
            {
                int id = copper.NetId;
                if (!copperElements.ContainsKey(id))
                {
                    copperElements.Add(id, new List<IKcCopperElement>());
                }
                copperElements[id].Add(copper);
            }
        }

        foreach (var id in copperElements.Keys)
        {
            result.Add(id, new KcNetTracks(copperElements[id]));
        }
        return result;
    }
    private List<IKcLayerElement> ElementsOnLayer(KcLayer layer, params List<IKcLayerElement>[] lists)
    {
        var result = new List<IKcLayerElement>();
        foreach (var list in lists)
        {
            foreach (var element in list)
            {
                if (element.IsOnLayer(layer))
                {
                    result.Add(element);
                }
            }
        }
        return result;
    }
    private (double, SKPoint) GetDrillInfo(IKcDrillable drillElement, SKMatrix transform)
    {
        var pt = drillElement.At.SKPointTransformed(transform);
        return (drillElement.DrillSize, pt);
    }
    public KcPosition EdgeCutCenter()
    {
        var (tl, br) = GetEdgeBoundsBL_TR();
        return new KcPosition((br.X - tl.X) / 2f + tl.X, (br.Y - tl.Y) / 2f + tl.Y);
    }
    public KcPosition EdgeCutOriginBottomLeft()
    {
        float offsetX = 0;
        float offsetY = 0;
        var edgeCuts = GetGraphicsForLayers("Edge.Cuts");
        if (edgeCuts.Count == 1 && edgeCuts[0] is KcGrRect rect)
        {
            offsetX = (float)Math.Min(rect.Start.X, rect.End.X); // left
            offsetY = (float)Math.Max(rect.Start.Y, rect.End.Y); // bottom (positive down)
        }
        return new KcPosition(offsetX, offsetY);
    }
    public KcSize GetEdgeCutSize()
    {
        KcSize result = new KcSize(0, 0);
        var edgeCuts = GetGraphicsForLayers("Edge.Cuts");
        // todo: allow for non rect edge cuts.
        // todo: create a rect bounds on copper if there are no edge cuts.
        if (edgeCuts.Count == 1 && edgeCuts[0] is KcGrRect rect)
        {
            result.Width = (float)Math.Abs(rect.End.X - rect.Start.X);
            result.Height = (float)Math.Abs(rect.End.Y - rect.Start.Y);
        }
        return result;
    }
    /// <summary>
    /// Returns topLeft, and bottomRight positions.
    /// </summary>
	public (KcPosition, KcPosition) GetEdgeBoundsBL_TR()
    {
        var min = new KcPosition(double.MaxValue, double.MaxValue);
        var max = new KcPosition(double.MinValue, double.MinValue);
        var edgeCuts = GetGraphicsForLayers("Edge.Cuts");
        if (edgeCuts.Count > 0 && edgeCuts[0] is KcGrRect rect)
        {
            min.X = (float)Math.Min(rect.Start.X, rect.End.X);
            min.Y = (float)Math.Min(rect.Start.Y, rect.End.Y);
            max.X = (float)Math.Max(rect.Start.X, rect.End.X);
            max.Y = (float)Math.Max(rect.Start.Y, rect.End.Y);
        }
        else
        {
            CalcSegementsMinMaxBounds(min, max);
        }
        return (min, max);
    }
    private void CalcSegementsMinMaxBounds(KcPosition min, KcPosition max)
    {
        foreach (KcSegment seg in Segments)
        {
            if (seg.MinX < min.X) { min.X = seg.MinX; }
            if (seg.MinY < min.Y) { min.Y = seg.MinY; }
            if (seg.MaxX > max.X) { max.X = seg.MaxX; }
            if (seg.MaxY > max.Y) { max.Y = seg.MaxY; }
        }
    }
    public static KcPcb Parse(List<object> root)
    {
        var result = new KcPcb();
        foreach (var item in root)
        {
            if (item is List<object> list && list.Count > 1)
            {
                string name = (string)list[0];
                switch (name)
                {
                    case "arc":
                        result.Arcs.Add(KcArc.Parse(list));
                        break;
                    case "dimension":
                        //result.
                        break;
                    case "footprint":
                        result.Footprints.Add(KcFootprint.Parse(list));
                        break;
                    case "generator":
                        result.Generator = (string)list[1];
                        break;
                    case "generator_version":
                        result.GeneratorVersion = (string)list[1];
                        break;
                    case "general":
                        result.General = KcGeneral.Parse(list);
                        break;
                    case "gr_arc":
                        result.GrElements.Add(KcGrArc.Parse(list));
                        break;
                    case "gr_circle":
                        result.GrElements.Add(KcGrCircle.Parse(list));
                        break;
                    case "gr_line":
                        result.GrElements.Add(KcGrLine.Parse(list));
                        break;
                    case "gr_poly":
                        result.GrElements.Add(KcGrPoly.Parse(list));
                        break;
                    case "gr_rect":
                        result.GrElements.Add(KcGrRect.Parse(list));
                        break;
                    case "gr_text":
                        //result.
                        break;
                    case "gr_text_box":
                        //result.
                        break;
                    case "image":
                        //result.
                        break;
                    case "layers":
                        result.Layers = KcLayer.Parse(list);
                        break;
                    case "net":
                        result.Nets.Add(KcNet.Parse(list));
                        break;
                    case "paper":
                        result.Paper = (string)list[1];
                        break;
                    case "setup":
                        result.Setup = KcSetup.Parse(list);
                        break;
                    case "segment":
                        result.Segments.Add(KcSegment.Parse(list));
                        break;
                    case "version":
                        result.Version = Convert.ToInt32(list[1]);
                        break;
                    case "via":
                        result.Vias.Add(KcVia.Parse(list));
                        break;
                    case "zone":
                        result.Zones.Add(KcZone.Parse(list));
                        break;
                    default:
                        Trace.WriteLine("Missing root level parse: " + name);
                        break;
                }
            }
        }
        result.SetOrigin();

        return result;
    }
}
