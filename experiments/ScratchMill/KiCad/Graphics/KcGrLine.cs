namespace ScratchMill.KiCad.Graphics
{
    public class KcGrLine : KcGrBase
    {
        public static KcGrLine Parse(List<object> items)
        {
            var result = new KcGrLine();
            result.ParseBase(items);
            return result;
        }
    }
}
