using System.Globalization;

namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Exports observations from the same trials that the explorer runs.</summary>
public static class RaceSurvey
{
    public static void Export(string folder)
    {
        Directory.CreateDirectory(folder);
        using var cases = File.CreateText(Path.Combine(folder, "trials.csv"));
        using var beats = File.CreateText(Path.Combine(folder, "beats.csv"));
        using var work = File.CreateText(Path.Combine(folder, "work.csv"));
        cases.WriteLine("trial,first_role_is_b,landmark_order,finish_order,favorite_order,favorite_won,goal_reached,landmarks_hex,finish_hex,passage_rows_hex,passage_law_hex,consumer_row_hex,consumer_law_hex,assistance_unless_rows_hex,assistance_both_row_hex,assistance_release_row_hex");
        beats.WriteLine("trial,beat,event,start_signal,request_a,request_b,base_access_a,base_access_b,effective_access_b,state_a,state_b,store_a_hex,store_b_hex,goal_reached,finish_order,favorite_won,assistance_released,host_reads,supplier_inspections");
        work.WriteLine("trial,private_steps,recorded_moments,host_reads,supplier_inspections");
        for (var scenario = 0; scenario < RaceExperiments.Cases.Length; scenario++)
        {
            var trial = RaceExperiments.Run(scenario);
            var assistance = trial.AssistanceDevice;
            Write(cases, trial.Name, trial.FirstRoleIsB, trial.LandmarkOrder, trial.FinishOrder, trial.FavoriteOrder,
                trial.FavoriteWon, trial.GoalReached, Hex(trial.LandmarkBytes), Hex(trial.FinishBytes),
                Hex(trial.PassageRows), Hex(trial.PassageLawBytes), Hex(trial.ConsumerRow), Hex(trial.ConsumerLawBytes),
                Hex(assistance?.RefusalAndGoalRows() ?? []), Hex(assistance?.EligibilityRow() ?? []),
                Hex(assistance?.ReleaseRow() ?? []));
            foreach (var beat in trial.Timeline)
                Write(beats, trial.Name, beat.Elapsed, beat.Label, beat.StartSignal, beat.ARequest, beat.BRequest, beat.AAccess, beat.BAccess,
                    beat.BEffectiveAccess, beat.AState, beat.BState, beat.AStore.ToString("X2"), beat.BStore.ToString("X2"),
                    beat.GoalReached, beat.FinishOrder, beat.FavoriteWon,
                    beat.Assistance, beat.HostReads, beat.SupplierInspections);
        }

        // Hold the question and script fixed while changing private course size.
        // Count the comparison owner's reads separately from producing its inputs.
        foreach (var scenario in new[] { 0, 10 })
        foreach (var size in new[] { 16, 256, 16_384 })
        {
            var trial = RaceExperiments.Run(scenario, size);
            Write(work, trial.Name, size, trial.Timeline.Length, trial.HostReads, trial.SupplierInspections);
        }
        Console.WriteLine($"Exported {RaceExperiments.Cases.Length} race trials, their timelines and six work comparisons.");
    }

    private static string Hex(byte[] bytes) => Convert.ToHexString(bytes);

    private static void Write(TextWriter file, params object[] fields)
    {
        var escaped = fields.Select(field => "\"" +
            Convert.ToString(field, CultureInfo.InvariantCulture)!.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"");
        file.WriteLine(string.Join(',', escaped));
    }
}
