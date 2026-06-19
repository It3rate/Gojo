namespace ScratchMill.KiCad;

public class KcModel : IKiCadElement
{
    public string Path { get; set; }
    public KcOffset Offset { get; set; } = new KcOffset();
    public KcScale Scale { get; set; } = new KcScale();
    public KcRotate Rotate { get; set; } = new KcRotate();
}
