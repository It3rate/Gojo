namespace ScratchMill.KiCad.Graphics;

public class KcFpLine : IKiCadElement
{
    public KcPosition Start { get; set; } = new KcPosition();
    public KcPosition End { get; set; } = new KcPosition();
    public string Layer { get; set; }
    public double Width { get; set; }
    public KcStroke Stroke { get; set; }
    public string UUID { get; set; }
    public static KcFpLine Parse(List<object> items)
    {
        var result = new KcFpLine();
        foreach (var item in items)
        {
            if (item is List<object> list && list.Count > 1)
            {
                string name = (string)list[0];
                switch (name)
                {
                    case "start":
                        result.Start = KcPosition.Parse(list);
                        break;
                    case "end":
                        result.End = KcPosition.Parse(list);
                        break;
                    case "layer":
                        result.Layer = (string)list[1];
                        break;
                    case "stroke":
                        result.Stroke = KcStroke.Parse(list);
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
