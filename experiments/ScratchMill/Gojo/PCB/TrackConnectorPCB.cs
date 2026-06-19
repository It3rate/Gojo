using Gojo.Core;

namespace ScratchMill.Gojo.PCB
{
    /// <summary>
    /// Connects track sections tip to tail, branches in tracks, and vias to new layers
    /// </summary>
    public class TrackConnectorPCB : IPCBElement
    {
        IPCBCopperElement StartElement { get; set; }
        CarrierLocation StartPosition { get; set; }
        IPCBCopperElement EndElement { get; set; }
        CarrierLocation EndPosition { get; set; }
    }
}
