namespace Phototaxis;

/// <summary>External world controls. These are never creature observations or fitness marks.</summary>
public sealed record EcologySettings(double LightRadius = 72, double LightOnScale = 1,
    int PatchHarvestPerPulse = 0, double LightDriftSpeed = 0)
{
    public static EcologySettings Original { get; } = new();
    public static EcologySettings Focused { get; } = new(60, .85, 24);
    public static EcologySettings Roving { get; } = new(72, 1, 36, 1.5);

    internal void Validate()
    {
        if (!double.IsFinite(LightRadius) || LightRadius <= 0 ||
            !double.IsFinite(LightOnScale) || LightOnScale <= 0 || LightOnScale > 1 ||
            PatchHarvestPerPulse < 0 || !double.IsFinite(LightDriftSpeed) || LightDriftSpeed < 0)
            throw new ArgumentOutOfRangeException(nameof(EcologySettings));
    }
}

/// <summary>Construction choices for controlled trials over our native pilot structures.</summary>
public sealed record PilotFeatures(bool ProgressEscape = true, bool Courtship = true)
{
    public static PilotFeatures Baseline { get; } = new(false, false);
    public static PilotFeatures Current { get; } = new();
}
