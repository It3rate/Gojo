namespace ScratchMill.GCode
{
    public class PassData
    {
        public float[] ContourOffsets { get; set; }
        public float[] Depths { get; set; }
        public float FeedRate { get; set; }
        public float SpindleSpeed { get; set; }
        public float ClearingOverlap { get; set; } = 0;
        public PassData(float[] contourOffsets, float[] depths,
            float feedRate = -1, float spindleSpeed = -1, float clearingOverlap = 0)
        {
            ContourOffsets = contourOffsets;
            Depths = depths;
            FeedRate = feedRate;
            SpindleSpeed = spindleSpeed;
            ClearingOverlap = clearingOverlap;
        }
    }
}
