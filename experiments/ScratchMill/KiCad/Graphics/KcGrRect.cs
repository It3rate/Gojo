using ScratchMill.KiCad.Graphics;

namespace ScratchMill.KiCad.Design;

public class KcGrRect : KcGrBase
{
    public static KcGrRect Parse(List<object> items)
    {
        var result = new KcGrRect();
        result.ParseBase(items);
        return result;
    }
}