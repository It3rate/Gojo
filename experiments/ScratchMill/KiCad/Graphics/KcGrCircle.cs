namespace ScratchMill.KiCad.Graphics
{
    public class KcGrCircle : KcGrBase
    {
        public static KcGrCircle Parse(List<object> items)
        {
            var result = new KcGrCircle();
            result.ParseBase(items);
            return result;
        }
    }
}
