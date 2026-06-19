namespace ScratchMill.KiCad;

public class KcSegment : KcInnerSegment
{
    public double Width { get; set; }
    public double MinX => Math.Min(At.X, End.X);
    public double MinY => Math.Min(At.Y, End.Y);
    public double MaxX => Math.Max(At.X, End.X);
    public double MaxY => Math.Max(At.Y, End.Y);


    public override string ToString() => $"seg{NetId}[{At.X} {At.Y}, {End.X} {End.Y}]";

    public static KcSegment Parse(List<object> items)
    {
        var result = new KcSegment();
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
    public void SetAt(KcPosition pos)
    {
        At = pos;
    }
}
