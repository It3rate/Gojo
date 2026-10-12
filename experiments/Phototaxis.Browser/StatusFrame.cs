using System.Text.Json.Serialization;

namespace Phototaxis.Browser;

// Read-only presentation of local observations and execution controls.
internal sealed record StatusFrame(int Seed, long Ticks, double Seconds, int Population, long Food, long Births,
    ulong Credits, bool Learning, bool Paused, bool Rocks, bool Risk, int Rate, bool Inspector,
    double ActualRate, double ComputeLoad, bool Training, double TrainingRemaining, double Energy,
    int Selected, ulong LeftForce, ulong RightForce, int Components, long Rewrites,
    double ResourceYield, long ComputeCalories, bool MeasuredResources,
    int? SelectedComponents, int? SmallestCircuit, int? LargestCircuit, double? MeanComponents,
    int Operations, double MeanSpeed, double StationaryShare, double? HungryTowardLight, double? FullAwayFromLight,
    bool PulsedLights, int ActiveLights, string? DesignedController,
    bool CognitiveEcology, bool BreedingSelection, long Injuries, long InjuryCalories,
    double? InferredSpeed, long? PredictionError, long? PredictionPressure, long? Appetite,
    bool InquiriesEnabled, bool JevReady, bool JevBusy, int JevRequests, double JevDollars,
    bool FocusedEcology, bool RovingEcology, double LightRadius, double LightOnScale, int PatchHarvestPerPulse,
    double LightDriftSpeed,
    ulong LocalFoodShortfall, double? HungryGradientAlignment, long HungryGradientSamples,
    long? InjuryStrength, long? PainMemory, double? RecoverySeconds,
    double ArenaWidth, double ArenaHeight, ulong? ProgressEscapes, bool? Courting, ulong? LeftMate, ulong? RightMate);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(StatusFrame))]
internal partial class StatusJson : JsonSerializerContext;
