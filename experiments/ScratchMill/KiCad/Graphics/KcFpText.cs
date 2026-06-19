namespace ScratchMill.KiCad.Graphics;

public class KcFpText : IKiCadElement
{
    public string Type { get; set; }
    public string Value { get; set; }
    public KcPosition At { get; set; } = new KcPosition();
    public string Layer { get; set; }
    public KcEffects Effects { get; set; } = new KcEffects();
    public string UUID { get; set; }
}
