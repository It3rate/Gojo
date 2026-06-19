namespace ScratchMill.KiCad.Design;

public class KcFont : IKiCadElement
{
    public KcSize Size { get; set; } = new KcSize();
    public double Thickness { get; set; }
}
