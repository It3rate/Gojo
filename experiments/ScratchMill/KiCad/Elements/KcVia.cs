namespace ScratchMill.KiCad;

public class KcVia : IKcCopperElement, IKcDrillable
{
    public KcPosition At { get; set; } = new KcPosition();
    public KcSize Size { get; set; }
    public double DrillSize { get; set; }
    public List<string> Layers { get; set; } = new List<string>();
    public int NetId { get; set; }
    public string UUID { get; set; }
    public bool IsOnLayer(KcLayer layer) => Layers.Contains(layer.Name);
    public bool IsOnNet(KcNet net) => NetId == net.Id;
    public bool IsThroughHole => true;
    public override string ToString() => $"via{NetId}[{At.X} {At.Y}]";


    public static KcVia Parse(List<object> items)
    {
        var result = new KcVia();
        foreach (var item in items)
        {
            if (item is List<object> list && list.Count > 1)
            {
                string name = (string)list[0];
                switch (name)
                {
                    case "at":
                        result.At = KcPosition.Parse(list);
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
