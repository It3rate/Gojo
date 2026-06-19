namespace ScratchMill.KiCad;

public class KcLayer : IKiCadElement
{
    public int Index { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }

    public static List<KcLayer> Parse(List<object> items)
    {
        var result = new List<KcLayer>();
        foreach (var item in items)
        {
            if (item is List<object> list && (list.Count > 1))
            {
                var layer = new KcLayer();
                layer.Index = Convert.ToInt32(list[0]);
                layer.Name = (string)list[1];
                layer.Type = (string)list[2];
                if (list.Count > 3)
                {
                    layer.Description = (string)list[3];
                }
                result.Add(layer);
            }
        }
        return result;
    }
}
