using System.Globalization;

namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>After-the-fact experiment records. None of these counts or reports
/// feed a route, select a default, or build structure in the running trials.</summary>
public static class BehaviorSurvey
{
    public static void Export(string folder)
    {
        Directory.CreateDirectory(folder);
        var walkRows = 0; var moves = 0; var conflicts = 0; var unresolved = 0;
        using (var output = File.CreateText(Path.Combine(folder, "walks.csv")))
        {
            output.WriteLine("case,title,observation,pulse,from_site,to_site,input_transfer,committed,moved,conflict,missing,resolved,potential_destinations,compatible_uses,occupied_activations,family_bytes_unchanged");
            for (var trial = 0; trial < ConstructionBehavior.Cases.Count; trial++)
            foreach (byte observation in new byte[] { 1, 2, 3, 0 })
            {
                var run = new ConstructionBehavior(trial);
                var s = run.Example.Surface;
                var saved = FamilyUseCodec.Encode(s.Table, s.Entry, s.Uses);
                run.SetRouteObservation(observation);
                for (var beat = 0; beat < 9; beat++)
                {
                    var step = run.Walk.Pulse();
                    var witness = run.Walk.Trace[^1];
                    var unchanged = saved.AsSpan().SequenceEqual(FamilyUseCodec.Encode(s.Table, s.Entry, s.Uses));
                    if (!unchanged || run.Walk.OccupiedStateCount != 1)
                        throw new InvalidOperationException("A route changed retained encoding or duplicated/lost activation.");
                    walkRows++; moves += step.Moved ? 1 : 0; conflicts += step.Prediction.Conflict ? 1 : 0;
                    unresolved += !step.Prediction.Resolved || step.Prediction.Missing ? 1 : 0;
                    output.WriteLine(Csv(trial, run.Case.Title, observation, beat + 1,
                        witness.FromSite, witness.ToSite, witness.InputTransfer, step.Committed, step.Moved,
                        step.Prediction.Conflict, step.Prediction.Missing, step.Prediction.Resolved,
                        string.Join(";", step.Prediction.Targets), string.Join(";", witness.CompatibleUses),
                        run.Walk.OccupiedStateCount, unchanged));
                }
            }
        }

        var adjustmentRows = 0; var reachMoves = 0; var waiting = 0;
        using (var output = File.CreateText(Path.Combine(folder, "adjustments.csv")))
        {
            output.WriteLine("case,title,initially_hidden,push,control_hidden,before_support,possible_settlement,predicted_execution_after,actual_after,moved,waiting,reserve,affected_fields,affected_links,affected_groups,accounted_requests,family_bytes_unchanged");
            for (var trial = 0; trial < ConstructionBehavior.Cases.Count; trial++)
            foreach (var hidden in new[] { false, true })
            {
                var run = new ConstructionBehavior(trial); var s = run.Example.Surface;
                var saved = FamilyUseCodec.Encode(s.Table, s.Entry, s.Uses);
                for (var beat = 0; beat < 2; beat++)
                {
                    var concealed = hidden && beat == 0;
                    var step = run.Adjustment.Push(concealed);
                    var fields = run.Adjustment.AffectedFields();
                    var count = (run.Adjustment.ReservePresent ? 1 : 0) + (run.Adjustment.Waiting ? 1 : 0) + fields.Length;
                    var unchanged = saved.AsSpan().SequenceEqual(FamilyUseCodec.Encode(s.Table, s.Entry, s.Uses));
                    if (count != 1 || !unchanged) throw new InvalidOperationException("The reach trial lost or copied its request.");
                    if ((step.ActualScopeSupport & ~step.Prediction.AfterSupport) != 0)
                        throw new InvalidOperationException("The executed observation exceeded its predicted support.");
                    adjustmentRows++; reachMoves += step.Moved ? 1 : 0; waiting += step.Waiting ? 1 : 0;
                    output.WriteLine(Csv(trial, run.Case.Title, hidden, beat + 1, concealed,
                        step.Prediction.BeforeSupport.ToString("X8"), step.Prediction.SettlementSupport.ToString("X8"), step.Prediction.AfterSupport.ToString("X8"),
                        step.ActualScopeSupport.ToString("X8"), step.Moved, step.Waiting, run.Adjustment.ReservePresent,
                        string.Join(";", fields), string.Join(";", run.Adjustment.AffectedLinks()),
                        string.Join(";", run.Adjustment.AffectedGroups()), count, unchanged));
                }
            }
        }
        var summary = $"{ConstructionBehavior.Cases.Count} authored behavior cases loaded from independently decoded family/use artifacts.\n" +
            $"{walkRows} route pulses: {moves} movements, {conflicts} conflicting-route observations, {unresolved} unresolved/missing-control observations.\n" +
            "Every route pulse preserves exactly one activation and all retained family bytes. Nine pulses are an observation budget, not a stop rule.\n" +
            $"{adjustmentRows} adjustment attempts: {reachMoves} reach changes; {waiting} observations retain a waiting request.\n" +
            "Every adjustment preserves exactly one request across reserve/waiting/dials; every actual joint read is contained by its execution prediction.\n" +
            "Initially hidden controls are revealed on the second push; no new input is supplied. Coarse participation bytes remain unchanged.\n" +
            "The path and radial square artifacts are identical after their optional geometric bases are removed, so identical attached laws produce identical behavior.\n" +
            "No builder names, geometric bases or drawing coordinates enter either executor. The input binding, passage laws, pins and adjustment rungs are authored operational hypotheses.\n" +
            "These trials do not learn defaults, create geometry, settle numerical lengths, execute simultaneous merges, or reconstruct discarded construction history.\n";
        File.WriteAllText(Path.Combine(folder, "summary.txt"), summary);
        Console.Write(summary);
    }

    private static string Csv(params object?[] fields) => string.Join(",", fields.Select(field =>
    {
        var value = Convert.ToString(field, CultureInfo.InvariantCulture) ?? "";
        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? '"' + value.Replace("\"", "\"\"") + '"' : value;
    }));
}
