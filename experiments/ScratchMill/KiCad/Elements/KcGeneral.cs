namespace ScratchMill.KiCad;

public class KcGeneral : IKiCadElement
{
    public double Thickness { get; set; }
    public bool LegacyTeardrops { get; set; }
    public static KcGeneral Parse(List<object> items)
    {
        var result = new KcGeneral();
        foreach (var item in items)
        {
            if (item is List<object> list && list.Count > 1)
            {
                string name = (string)list[0];
                switch (name)
                {
                    case "thickness":
                        result.Thickness = Convert.ToDouble(list[1]);
                        break;
                    case "legacy_teardrops":
                        result.LegacyTeardrops = Convert.ToBoolean(list[1]);
                        break;
                }
            }
        }
        return result;
    }
}
