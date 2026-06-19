using ScratchMill.KiCad.Design;
using ScratchMill.Utils;
using SkiaSharp;

namespace ScratchMill.KiCad.Graphics
{
    public interface IKcGraphic : IKiCadElement
    {
        KcPosition Start { get; }
        KcPosition End { get; }
        List<KcPosition> Points { get; }
        KcStroke Stroke { get; }
        string Fill { get; }
        string Layer { get; set; }
        int NetId { get; set; }
        string UUID { get; }

        void ParseBase(List<object> items);
        SKPath ToSkiaPath(SKMatrix transform);
    }
    public class KcGrBase : IKcGraphic
    {
        public KcPosition Start => Points[0];
        public KcPosition End => Points[Points.Count - 1];
        public List<KcPosition> Points { get; set; } = new List<KcPosition>();

        public KcStroke Stroke { get; private set; }
        public string Fill { get; set; }
        public string Layer { get; set; }
        public int NetId { get; set; }
        public string UUID { get; set; }

        public void ParseBase(List<object> items)
        {
            foreach (var item in items)
            {
                if (item is List<object> list && list.Count > 1)
                {
                    string name = (string)list[0];
                    switch (name)
                    {
                        case "start":
                            Points.Insert(0, KcPosition.Parse(list));
                            break;
                        case "end":
                            Points.Add(KcPosition.Parse(list));
                            break;
                        case "mid":
                            var pos = Points.Count == 0 ? 0 : 1;
                            Points.Insert(pos, KcPosition.Parse(list));
                            break;
                        case "pts":
                            Points = KcPosition.ParsePoints((List<object>)list[1]);
                            break;
                        case "stroke":
                            Stroke = KcStroke.Parse(list);
                            break;
                        case "fill":
                            Fill = (string)list[1];
                            break;
                        case "layer":
                            Layer = (string)list[1];
                            break;
                        case "net":
                            NetId = Convert.ToInt32(list[1]);
                            break;
                        case "uuid":
                        case "tstamp":
                            UUID = (string)list[1];
                            break;
                    }
                }
            }
        }

        public SKPath ToSkiaPath(SKMatrix transform)
        {
            var result = new SKPath();
            if (this is KcGrRect rectGraphic)
            {
                result.AddPath(RectPath(rectGraphic, transform));
            }
            else if (this is KcGrArc arcGraphic)
            {
            }
            else if (this is KcGrCircle circleGraphic)
            {
            }
            else if (this is KcGrLine lineGraphic)
            {
            }
            else if (this is KcGrPoly polyGraphic)
            {
            }
            else
            {
                //result.Add(new SKPath());
            }
            return result;
        }
        private static SKPath RectPath(KcGrRect rect, SKMatrix transform)
        {
            var result = new SKPath();
            var curPt = rect.Start.SKPointTransformed(transform);
            var endPt = rect.End.SKPointTransformed(transform);
            var skRect = SKRect.Create(curPt.X, curPt.Y, endPt.X - curPt.X, endPt.Y - curPt.Y);
            result.AddRect(skRect);
            return result;
        }
    }
}
