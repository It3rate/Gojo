namespace Minds.Predictions;

// Host-owned sensor tapes. Values are released only after the target's issue step.
public sealed record Sample(long Time, long? Value);
public sealed record PredictionCase(string Id, string Name, string Split, string Description, Sample[] Samples);

public static class PredictionProtocol
{
    public const string Version = "predictions-v1";
    public static readonly PredictionCase[] Cases = [
        Regular("dev-flat", "Constant (development)", "development", "A constant signal checks a flat fitted frame and its retained time residue.", 3,3,3,3,3,3),
        Regular("dev-ramp", "Ramp (development)", "development", "A fixed rise checks prospective continuation.", 3,7,11,15,19,23),
        new("dev-fraction", "Unequal intervals (development)", "development", "Unequal source times require an exact fractional prediction.", [new(0,0),new(3,1),new(4,2),new(7,3),new(9,4)]),
        Regular("constant", "A constant below zero", "evaluation", "Repeated negative observations do not imply an unknown value.", -4,-4,-4,-4,-4,-4,-4,-4),
        new("unequal-ramp", "A ramp with unequal intervals", "evaluation", "Actual elapsed source time matters more than the number of arrivals.", [new(0,2),new(2,8),new(5,17),new(6,20),new(10,32),new(11,35),new(15,47)]),
        Regular("reversal", "The trend reverses", "evaluation", "The rising line must face a later fall before the next fit can change.", 2,5,8,11,8,5,2,-1,-4),
        Regular("alternating", "The signal alternates", "evaluation", "Extending the latest direction can be worse than holding the last value.", 2,8,2,8,2,8,2,8,2,8),
        Regular("step", "A sudden step", "evaluation", "A flat period changes level, then becomes flat again.", 4,4,4,4,12,12,12,12,12),
        Regular("gaps", "Missing samples across a change", "evaluation", "Unknown targets stay unassessed while the latest available line ages.", 1,3,5,null,null,4,2,0,null,-4),
        Regular("missing-start", "Missing observations at the start", "evaluation", "One known sample enables hold-last before two samples enable a fitted line.", null,null,6,null,9,12,15,18),
        Regular("blackout", "Observations stop arriving", "evaluation", "Predictions continue, but no later values exist to validate them.", 1,4,7,null,null,null,null,null),
        Regular("all-missing", "No measured values", "evaluation", "A stream of open observations supplies no numeric training evidence.", null,null,null,null,null,null)
    ];

    private static PredictionCase Regular(string id, string name, string split, string description, params long?[] values) =>
        new(id, name, split, description, values.Select((v,i) => new Sample(i,v)).ToArray());

    public static string Fixture() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "prospective-calibration.gojo"));
}
