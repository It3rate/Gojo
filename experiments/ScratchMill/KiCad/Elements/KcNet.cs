namespace ScratchMill.KiCad;

public class KcNet : IKiCadElement
{
    public int Id { get; set; }
    public string Name { get; set; }
    public KcNet(int id, string name)
    {
        Id = id;
        Name = name;
    }
    public static KcNet Parse(List<object> items)
    {
        var result = new KcNet(Convert.ToInt32(items[1]), (string)items[2]);
        return result;
    }
}
