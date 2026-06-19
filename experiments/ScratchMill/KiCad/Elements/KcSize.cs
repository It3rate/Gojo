namespace ScratchMill.KiCad;

public class KcSize : IKiCadElement
{
    public double Width { get; set; }
    public double Height { get; set; }
    public static KcSize Empty => new KcSize(0, 0);
    public KcSize() { }
    public KcSize(double width, double height) { Width = width; Height = height; }
    public static KcSize Parse(List<object> items)
    {
        var result = KcSize.Empty;
        if (items.Count >= 2 && items[0] is string name && name == "size")
        {
            result.Width = Convert.ToDouble(items[1]);
            if (items.Count > 2)
            {
                result.Height = Convert.ToDouble(items[2]);
            }
            else
            {
                result.Height = result.Width;
            }
        }
        return result;
    }
}
