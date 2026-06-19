namespace ScratchMill.KiCad;

public class KcSetup : IKiCadElement
{
    public KcPcbPlotParams PcbPlotParams { get; set; } = new KcPcbPlotParams();
    public object Stackup { get; set; } = new object();
    public float PadToMaskClearance { get; set; }
    public float SolderMaskMinWidth { get; set; }
    public float PadToPasteClearance { get; set; }
    public float PadToPasteClearanceRatio { get; set; }
    public bool AllowSoldermaskBridgesInFootprints { get; set; }
    public string Tenting { get; set; }
    public KcPosition AuxAxisOrigin { get; set; } = KcPosition.Empty;
    public KcPosition GridOrigin { get; set; } = KcPosition.Empty;

    public static KcSetup Parse(List<object> items)
    {
        var result = new KcSetup();
        foreach (var item in items)
        {
            if (item is List<object> list && list.Count > 1)
            {
                string name = (string)list[0];
                switch (name)
                {
                    case "stackup":
                        break;
                    case "pad_to_mask_clearance":
                        result.PadToMaskClearance = Convert.ToSingle(list[1]);
                        break;
                    case "solder_mask_min_width":
                        result.SolderMaskMinWidth = Convert.ToSingle(list[1]);
                        break;
                    case "pad_to_paste_clearance":
                        result.SolderMaskMinWidth = Convert.ToSingle(list[1]);
                        break;
                    case "pad_to_paste_clearance_ratio":
                        result.SolderMaskMinWidth = Convert.ToSingle(list[1]); // has '%'?
                        break;
                    case "allow_soldermask_bridges_in_footprints":
                        result.AllowSoldermaskBridgesInFootprints = ((string)list[1] == "no") || ((string)list[1] == "false") ? false : true;
                        break;
                    case "aux_axis_origin":
                        result.AuxAxisOrigin = KcPosition.Parse(list);
                        break;
                    case "grid_origin ":
                        result.GridOrigin = KcPosition.Parse(list);
                        break;
                    case "tenting":
                        break;
                    case "pcbplotparams":
                        result.PcbPlotParams = KcPcbPlotParams.Parse(list);
                        break;
                }
            }
        }
        return result;
    }
}
