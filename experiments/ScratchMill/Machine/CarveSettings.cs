
using ScratchMill.GCode;

namespace ScratchMill.Machine
{
    public class CarveSettings
    {
        public PassData ContourPass = new PassData([0.15f, 0.19f], [-0.05f, -0.075f]);
        public PassData BurrPass = new PassData([0.14f, 0.18f], [-0.095f, -0.11f]);
        public PassData RecutPass = new PassData([0.15f, 0.19f], [-0.10f, -0.12f]);
        public PassData GraphicPass = new PassData([0.00f], [-0.08f]);
        public PassData MaskRemovalPass = new PassData([-0.2f], [-0.001f], 600, 5000, 1.6f);
        public PassData EdgeCutPass = new PassData([0, 0], [-0.14f, -0.24f], 300);

        public float PcbHeight { get; set; } = 1.6f;
        public float Margin { get; set; } = 5f;
        public float SafeZMove { get; set; } = 1.00f;
        public float ZSpeed { get; set; } = 400;
        public int HorzScratchSpeed { get; set; } = 1500;

    }
}
