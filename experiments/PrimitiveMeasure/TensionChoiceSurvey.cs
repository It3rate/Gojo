using System.Globalization;

namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>The survey records the experiment's reads. It never selects an
/// outlet or writes the forecasts that the next opportunity will use.</summary>
public static class TensionChoiceSurvey
{
    public static void Export(string folder)
    {
        Directory.CreateDirectory(folder);
        using var turns = File.CreateText(Path.Combine(folder, "turns.csv"));
        using var wiring = File.CreateText(Path.Combine(folder, "wiring.csv"));
        turns.WriteLine("trial,turn,event,focus,focus_support,pickup_due_support,access_mask,offer_mask,feedback_mask,outlet,winner_mask,phase,opportunity_bits,before_purse,before_standing,before_pickup,after_purse,after_standing,after_pickup,forecast_a,forecast_b,forecast_c,predicted_purse,predicted_standing,predicted_pickup,observed_purse,observed_standing,observed_pickup,blocked_purse,blocked_standing,blocked_pickup,memory_hex");
        wiring.WriteLine("trial,opportunity_rows_hex,effect_rows_hex,feedback_rows_hex,priority_rows_hex");
        var trials = Enumerable.Range(0, TensionChoiceExperiments.Names.Length).Select(TensionChoiceExperiments.Run)
            .Append(NeedPriority.RunExample());
        foreach (var trial in trials)
        {
            Write(wiring, trial.Name, Hex(trial.ChoiceRows), Hex(trial.ReliefRows), Hex(trial.FeedbackRows), Hex(trial.PriorityRows));
            foreach (var beat in trial.Timeline)
                Write(turns, trial.Name, beat.Turn, beat.Event, beat.Focus, beat.FocusSupport, beat.PickupDueSupport,
                    beat.AccessMask, beat.OfferMask, beat.FeedbackMask, Outlet(beat.Action), beat.SelectionMask, beat.Phase, beat.OpportunityBits,
                    beat.Before[0], beat.Before[1], beat.Before[2], beat.After[0], beat.After[1], beat.After[2],
                    beat.Forecasts[0], beat.Forecasts[1], beat.Forecasts[2], beat.Prediction[0], beat.Prediction[1], beat.Prediction[2],
                    beat.Observed[0], beat.Observed[1], beat.Observed[2], beat.BlockedEffect[0], beat.BlockedEffect[1], beat.BlockedEffect[2],
                    Hex(beat.MemoryBytes));
        }
        ExportPermutations(folder);
    }

    private static void ExportPermutations(string folder)
    {
        using var output = File.CreateText(Path.Combine(folder, "permutations.csv"));
        output.WriteLine("faster_outlet,purse_feedback,choices,choices_a,choices_b,choices_c,end_purse,end_standing,end_pickup");
        for (var fast = -1; fast < 3; fast++)
        foreach (byte feedback in fast < 0 ? new byte[] { 7 } : new byte[] { 7, 6 })
        {
            var machine = new TensionChoiceMachine([48, 16, 6], [[1,0,0], [1,0,0], [1,0,0]], fast);
            // Each matched run begins fresh and receives precisely twelve equal opportunities.
            var choices = Enumerable.Range(0, 12).Select(_ => machine.Step(0, 7, feedback).Action).ToArray();
            var remaining = machine.Needs;
            Write(output, fast < 0 ? "none" : Outlet(fast), (feedback & 1) != 0, string.Join(" ", choices.Select(Outlet)),
                choices.Count(x => x == 0), choices.Count(x => x == 1), choices.Count(x => x == 2),
                remaining[0], remaining[1], remaining[2]);
        }
    }

    private static string Outlet(int action) => action < 0 ? "hold" : ((char)('A' + action)).ToString();
    private static string Hex(byte[] bytes) => Convert.ToHexString(bytes);
    private static void Write(TextWriter file, params object?[] fields) => file.WriteLine(string.Join(',',
        fields.Select(field => "\"" + (Convert.ToString(field, CultureInfo.InvariantCulture) ?? "").Replace("\"", "\"\"") + "\"")));
}
