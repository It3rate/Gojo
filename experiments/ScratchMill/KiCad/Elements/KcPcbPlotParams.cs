namespace ScratchMill.KiCad;

public class KcPcbPlotParams : IKiCadElement
{
    public string LayerSelection { get; set; }
    public bool DisableApertMacros { get; set; }
    public bool UseGerberExtensions { get; set; }
    public bool UseGerberAttributes { get; set; }
    public bool UseGerberAdvancedAttributes { get; set; }
    public bool CreateGerberJobFile { get; set; }
    public bool SvgUseInch { get; set; }
    public float SvgPrecision { get; set; }
    public bool ExcludeEdgeLayer { get; set; }
    public bool PlotFrameRef { get; set; }
    public bool ViasOnMask { get; set; }
    public int Mode { get; set; }
    public bool UseAuxOrigin { get; set; }
    public int HpglPenNumber { get; set; }
    public double HpglPenSpeed { get; set; }
    public double HpglPenDiameter { get; set; }
    public bool DxfPolygonMode { get; set; }
    public bool DxfImperialUnits { get; set; }
    public bool DxfUsePcbNewFont { get; set; }
    public bool PsNegative { get; set; }
    public bool PsA4Output { get; set; }
    public bool PlotReference { get; set; }
    public bool PlotValue { get; set; }
    public bool PlotInvisibleText { get; set; }
    public bool SketchPadsOnFab { get; set; }
    public bool SubtractMaskFromSilk { get; set; }
    public int OutputFormat { get; set; }
    public bool Mirror { get; set; }
    public string DrillShape { get; set; }
    public int ScaleSelection { get; set; }
    public string OutputDirectory { get; set; }

    public static KcPcbPlotParams Parse(List<object> items)
    {
        var result = new KcPcbPlotParams();
        foreach (var item in items)
        {
            if (item is List<object> list && list.Count > 1)
            {
                string name = (string)list[0];
                switch (name)
                {
                    case "layerselection":
                        result.LayerSelection = (string)list[1];
                        break;
                    case "disableapertmacros":
                        result.DisableApertMacros = ((string)list[1] == "no") || ((string)list[1] == "false") ? false : true;
                        break;
                    case "usegerberextensions":
                        result.UseGerberExtensions = ((string)list[1] == "no") || ((string)list[1] == "false") ? false : true;
                        break;
                    case "usegerberattributes":
                        result.UseGerberAttributes = ((string)list[1] == "no") || ((string)list[1] == "false") ? false : true;
                        break;
                    case "usegerberadvancedattributes":
                        result.UseGerberAdvancedAttributes = ((string)list[1] == "no") || ((string)list[1] == "false") ? false : true;
                        break;
                    case "creategerberjobfile":
                        result.CreateGerberJobFile = ((string)list[1] == "no") || ((string)list[1] == "false") ? false : true;
                        break;
                    case "svguseinch":
                        result.SvgUseInch = ((string)list[1] == "no") || ((string)list[1] == "false") ? false : true;
                        break;
                    case "svgprecision":
                        result.SvgPrecision = Convert.ToSingle(list[1]);
                        break;
                    case "excludeedgelayer":
                        result.ExcludeEdgeLayer = ((string)list[1] == "no") || ((string)list[1] == "false") ? false : true;
                        break;
                    case "plotframeref":
                        result.PlotFrameRef = ((string)list[1] == "no") || ((string)list[1] == "false") ? false : true;
                        break;
                    case "viasonmask":
                        result.ViasOnMask = ((string)list[1] == "no") || ((string)list[1] == "false") ? false : true;
                        break;
                    case "mode":
                        result.Mode = Convert.ToInt32(list[1]);
                        break;
                    case "useauxorigin":
                        result.UseAuxOrigin = ((string)list[1] == "no") || ((string)list[1] == "false") ? false : true;
                        break;
                    case "hpglpennumber":
                        result.HpglPenDiameter = Convert.ToDouble(list[1]);
                        break;
                    case "hpglpenspeed":
                        result.HpglPenSpeed = Convert.ToDouble(list[1]);
                        break;
                    case "hpglpendiameter":
                        result.HpglPenDiameter = Convert.ToDouble(list[1]);
                        break;
                    case "dxfpolygonmode":
                        result.DxfPolygonMode = ((string)list[1] == "no") || ((string)list[1] == "false") ? false : true;
                        break;
                    case "dxfimperialunits":
                        result.DxfImperialUnits = ((string)list[1] == "no") || ((string)list[1] == "false") ? false : true;
                        break;
                    case "dxfusepcbnewfont":
                        result.DxfUsePcbNewFont = ((string)list[1] == "no") || ((string)list[1] == "false") ? false : true;
                        break;
                    case "psnegative":
                        result.PsNegative = ((string)list[1] == "no") || ((string)list[1] == "false") ? false : true;
                        break;
                    case "psa4output":
                        result.PsA4Output = ((string)list[1] == "no") || ((string)list[1] == "false") ? false : true;
                        break;
                    case "plotreference":
                        result.PlotReference = ((string)list[1] == "no") || ((string)list[1] == "false") ? false : true;
                        break;
                    case "plotvalue":
                        result.PlotValue = ((string)list[1] == "no") || ((string)list[1] == "false") ? false : true;
                        break;
                    case "plotinvisibletext":
                        result.PlotInvisibleText = ((string)list[1] == "no") || ((string)list[1] == "false") ? false : true;
                        break;
                    case "sketchpadsonfab":
                        result.SketchPadsOnFab = ((string)list[1] == "no") || ((string)list[1] == "false") ? false : true;
                        break;
                    case "subtractmaskfromsilk":
                        result.SubtractMaskFromSilk = ((string)list[1] == "no") || ((string)list[1] == "false") ? false : true;
                        break;
                    case "outputformat":
                        result.OutputFormat = Convert.ToInt32(list[1]);
                        break;
                    case "mirror":
                        result.Mirror = ((string)list[1] == "no") || ((string)list[1] == "false") ? false : true;
                        break;
                    case "drillshape":
                        result.DrillShape = (string)list[1];
                        break;
                    case "scaleselection":
                        result.ScaleSelection = Convert.ToInt32(list[1]);
                        break;
                    case "outputdirectory":
                        result.OutputDirectory = (string)list[1];
                        break;
                }
            }
        }
        return result;
    }
}

