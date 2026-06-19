namespace ScratchMill.Gojo.PCB
{
    public class LayerPCB
    {
        public List<NetPCB> Nets { get; set; } = new List<NetPCB>();
        public List<ViaPCB> Vias { get; set; } = new List<ViaPCB>();
    }
}
