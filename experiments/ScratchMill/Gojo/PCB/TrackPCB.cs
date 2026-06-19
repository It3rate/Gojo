namespace ScratchMill.Gojo.PCB
{
    public class TrackPCB
    {
        public List<ArcPCB> Arcs { get; set; } = new List<ArcPCB>();
        public List<SegmentPCB> Segments { get; set; } = new List<SegmentPCB>();
        public List<PadPCB> Pads { get; set; } = new List<PadPCB>();
        public List<HolePCB> Holes { get; set; } = new List<HolePCB>();
        public List<TeardropPCB> Teardrops { get; set; } = new List<TeardropPCB>();
    }
}
