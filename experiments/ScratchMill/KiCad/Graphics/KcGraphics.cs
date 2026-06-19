namespace ScratchMill.KiCad.Graphics
{
    public class KcGraphics : KcGrBase
    {
        public List<IKcGraphic> Elements { get; private set; }
        public int Count => Elements.Count;
        public IKcGraphic this[int index]
        {
            get { return Elements[index]; }
            //set { Elements[index] = value; }
        }
        public KcGraphics()
        {
            Elements = new List<IKcGraphic>();
        }
        public KcGraphics(IEnumerable<IKcGraphic> elements)
        {
            Elements = new List<IKcGraphic>(elements);
        }
        public void AddRange(IEnumerable<IKcGraphic> elements)
        {
            Elements.AddRange(elements);
        }

    }
}
