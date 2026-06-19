namespace ScratchMill.KiCad;

public class KcTeardrop : IKiCadElement
{
    public double BestLengthRatio { get; set; }
    public double MaxLength { get; set; }
    public double BestWidthRatio { get; set; }
    public double MaxWidth { get; set; }
    public int CurvePoints { get; set; }
    public double FilterRatio { get; set; }
    public bool Enabled { get; set; }
    public bool AllowTwoSegments { get; set; }
    public bool PreferZoneConnections { get; set; }
    public static KcTeardrop Parse(List<object> items)
    {
        var result = new KcTeardrop();
        foreach (var item in items)
        {
            if (item is List<object> list && list.Count > 1)
            {
                string name = (string)list[0];
                switch (name)
                {
                    case "best_length_ratio":
                        result.BestLengthRatio = Convert.ToDouble(list[1]);
                        break;
                    case "max_length":
                        result.MaxLength = Convert.ToDouble(list[1]);
                        break;
                    case "best_width_ratio":
                        result.BestWidthRatio = Convert.ToDouble(list[1]);
                        break;
                    case "max_width":
                        result.MaxWidth = Convert.ToDouble(list[1]);
                        break;
                    case "curve_points":
                        result.CurvePoints = Convert.ToInt32(list[1]);
                        break;
                    case "filter_ratio":
                        result.FilterRatio = Convert.ToDouble(list[1]);
                        break;
                    case "enabled":
                        result.Enabled = Convert.ToBoolean(list[1]);
                        break;
                    case "allow_two_segments":
                        result.AllowTwoSegments = Convert.ToBoolean(list[1]);
                        break;
                    case "prefer_zone_connections":
                        result.PreferZoneConnections = Convert.ToBoolean(list[1]);
                        break;
                }
            }
        }
        return result;
    }
}
