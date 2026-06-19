namespace ScratchMill.Machine;

public class MachineJogSettings
{
    public float SpindleRpm { get; set; } = .0f;
    public float SpindleRpmMin { get; set; } = .0f;
    public float SpindleRpmMax { get; set; } = 000.0f;
    public float JogStepSpeed { get; set; } = 00.0f;
    public float JogSlowSpeed { get; set; } = 00.0f;
    public float JogFastSpeed { get; set; } = 000.0f;
    public float JogStepDistance { get; set; } = .1f;
    public float JogSlowDistance { get; set; } = 00.0f;
    public float JogFastDistance { get; set; } = 500.0f;
}