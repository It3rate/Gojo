namespace ScratchMill.KiCad;

public class KcArc : KcInnerSegment
{
    public KcPosition Mid { get; set; } = new KcPosition();
    public double Width { get; set; }

    public override string ToString() => $"arc{NetId}[{At.X} {At.Y}, {End.X} {End.Y}]";

    public static KcArc Parse(List<object> items)
    {
        var result = new KcArc();
        foreach (var item in items)
        {
            if (item is List<object> list && list.Count > 1)
            {
                string name = (string)list[0];
                switch (name)
                {
                    case "start":
                        result.At = KcPosition.Parse(list);
                        break;
                    case "mid":
                        result.Mid = KcPosition.Parse(list);
                        break;
                    case "end":
                        result.End = KcPosition.Parse(list);
                        break;
                    case "width":
                        result.Width = Convert.ToDouble(list[1]);
                        break;
                    case "layer":
                        result.Layer = (string)list[1];
                        break;
                    case "net":
                        result.NetId = Convert.ToInt32(list[1]);
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
