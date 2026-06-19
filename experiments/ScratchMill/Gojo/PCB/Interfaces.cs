namespace ScratchMill.Gojo.PCB
{
    public interface IPCBElement
    {
    }
    public interface IPCBDrillElement : IPCBElement
    {
    }
    public interface IPCBCopperElement : IPCBElement
    {
    }
    // tracks have a start and end terminal element that is abs position.
    // They are routed relative to this, and can use other terminal elements as landmarks
    // (like, route between pad7 and pad8)
    // they can have branches that separate or join, all segments are connected with a connector element.
    // they define a centerline and a width. The width can be per element or interpolate along the whole track
    // they can interpolate into the terminal copper as well.
    public interface IPCBTrackElement : IPCBCopperElement
    {
    }
    public interface IPCBTrackTerminal : IPCBCopperElement
    {
    }
}
