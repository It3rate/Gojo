namespace ScratchMill.Machine;

public class GrblData
{
    public int NumberOfAxis { get; set; } = 3;
    public int Changed { get; set; } = -1;
    public float[] WPosition { get; set; } = { 0.0f, 0.0f, 0.0f };
    public float[] MPosition { get; set; } = { 0.0f, 0.0f, 0.0f };
    public float[] ProbePosition { get; set; } = { 0.0f, 0.0f, 0.0f };
    public float[] Offset { get; set; } = { 0.0f, 0.0f, 0.0f };
    public bool AbsDistance { get; set; } = true;
    public bool XModeDiameter { get; set; } = false;
    public float OverrideFeedRate { get; set; } = 100f;
    public float OverrideRapidRate { get; set; } = 100f;
    public float OverrideSpindleRpm { get; set; } = 100f;
    public GrblActiveState State { get; set; } = GrblActiveState.Unknown;
    public string State_color { get; set; } = "White";
    public string State_text { get; set; } = "";
    public int Substate { get; set; } = 0;
    public bool MpgMode { get; set; } = false;
    public int Alarm { get; set; } = 0;
    public int Error { get; set; } = 0;
    public string Pins { get; set; } = "";
    public string Message { get; set; } = "";
    public bool UseWPos { get; set; } = false;
    public bool AwaitWCO { get; set; } = true;
    public float[] SpindleState { get; set; } = { 0 };
    public float[] Coolant { get; set; } = { 0 };
}
