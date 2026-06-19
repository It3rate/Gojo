namespace ScratchMill.KiCad.Graphics
{
    public class KcGrPoly : KcGrBase
    {
        public static KcGrPoly Parse(List<object> items)
        {
            var result = new KcGrPoly();
            result.ParseBase(items);
            return result;
        }
    }
}
