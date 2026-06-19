using ScratchMill.KiCad;
using SkiaSharp;

namespace ScratchMill.GCode
{
    public class DrillData : IKcData
    {
        public SKPath BasePath { get; }
        public IKcDrillable Drill { get; }
        public string Svg { get; set; }
        public string GCode { get; set; }
        public int ContourIndex { get; set; } = 0;
        public PassKind PassKind { get; } = PassKind.Drill;
        public bool IsRecutPass { get; set; } = false;
        public bool OverlapTail { get; set; } = false;
        public SKPoint HoleLocation { get; set; }
        public float HoleSize { get; set; }
        public DrillData(SKPath path, SKPoint location, float holeSize, IKcDrillable drill)
        {
            BasePath = path;
            Drill = drill;
            HoleLocation = location;
            HoleSize = holeSize;
            if (drill is KcVia)
            {
                PassKind = PassKind.Via;
            }
        }
    }
}
