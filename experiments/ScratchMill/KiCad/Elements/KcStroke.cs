namespace ScratchMill.KiCad;

public class KcStroke : IKiCadElement
{
    public double Width { get; set; }
    public string Type { get; set; }
    public static KcStroke Parse(List<object> items)
    {
        var result = new KcStroke();

        foreach (var item in items)
        {
            if (item is List<object> list && list.Count > 1)
            {
                string name = (string)list[0];
                switch (name)
                {
                    case "width":
                        result.Width = Convert.ToDouble(list[1]);
                        break;
                    case "type":
                        result.Type = (string)list[1];
                        break;
                }
            }
        }
        return result;
    }
}
