namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Observer snapshots. Unseen samples remain null; even when a feedback
/// connection is withdrawn the diagnostic need banks can be inspected separately.</summary>
public sealed record TensionChoiceBeat(int Turn, int Focus, byte AccessMask, byte FeedbackMask, int Action,
    int[] Before, int[] After, int?[] Prediction, int?[] Forecasts, int?[] Observed, int?[][] Samples,
    byte OpportunityBits, byte SelectionMask, int Phase, byte[] MemoryBytes)
{
    public byte FocusSupport { get; init; }
    public byte PickupDueSupport { get; init; }
    public byte OfferMask { get; init; }
    public int[] BlockedEffect { get; init; } = new int[3];
    public string Event { get; init; } = "";
}

public sealed record TensionChoiceTrial(string Name, TensionChoiceBeat[] Timeline, byte[] ChoiceRows, byte[] ReliefRows)
{
    public byte[] PriorityRows { get; init; } = [];
    public byte[] FeedbackRows { get; init; } = [];
}

public static class TensionChoiceExperiments
{
    public static readonly string[] Names =
    [
        "Three offers, B opens a second service", "Move that service to C",
        "Attend to standing instead of purse", "Close the learned favorite, then reopen it",
        "No access until the fourth opportunity", "Attend to pickup without an outlet",
        "An optimistic purse forecast meets a debit", "Withhold purse feedback for six opportunities",
        "A stale negative forecast prevents a new trial"
    ];

    public static TensionChoiceTrial Run(int scenario)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)scenario, (uint)Names.Length);
        int[][] effects = [[1,2,0], [1,1,0], [1,1,0]];
        // Each entitlement buys one equal service interval. The worksite's
        // second connected service matures after that station has been used.
        int fast = scenario == 1 ? 2 : 1;
        int[,]? prior = null;
        if (scenario == 6)
        {
            effects[1] = [-1,2,0];
            fast = -1;
            prior = new int[,] { {1,1,0}, {2,1,0}, {1,1,0} };
        }
        if (scenario == 8)
        {
            effects[1] = [2,1,0];
            fast = -1;
            prior = new int[,] { {1,1,0}, {-1,1,0}, {0,1,0} };
        }
        var machine = new TensionChoiceMachine([36,32,6], effects, fast, prior);
        var history = new List<TensionChoiceBeat>();
        for (int turn = 1; turn <= 12; turn++)
        {
            int focus = scenario == 5 ? 2 : scenario == 2 && turn > 6 ? 1 : 0;
            byte access = scenario == 3 && turn is >= 7 and <= 9 ? (byte)5 :
                scenario == 4 && turn <= 3 ? (byte)0 : (byte)7;
            byte feedback = scenario == 7 && turn <= 6 ? (byte)6 : (byte)7;
            history.Add(machine.Step(focus, access, feedback));
        }
        return new(Names[scenario], history.ToArray(), machine.ChoiceRows(), machine.ReliefRows())
            { FeedbackRows = machine.FeedbackRows() };
    }
}
