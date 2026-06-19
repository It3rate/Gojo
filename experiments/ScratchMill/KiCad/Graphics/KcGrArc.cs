namespace ScratchMill.KiCad.Graphics
{
    public class KcGrArc : KcGrBase
    {
        public static KcGrArc Parse(List<object> items)
        {
            var result = new KcGrArc();
            result.ParseBase(items);
            return result;
        }
    }
}
