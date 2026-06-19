using ScratchMill.KiCad.Design;

namespace ScratchMill.KiCad;

public class KcEffects : IKiCadElement
{
    public KcFont Font { get; set; } = new KcFont();
}
