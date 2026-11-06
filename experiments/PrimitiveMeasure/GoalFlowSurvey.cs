using System.Globalization;

namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Records the selected experiments after their local passages run.</summary>
public static class GoalFlowSurvey
{
    public static void Export(string folder)
    {
        Directory.CreateDirectory(folder);
        using var drive = File.CreateText(Path.Combine(folder, "runner.csv"));
        using var wiring = File.CreateText(Path.Combine(folder, "wiring.csv"));
        drive.WriteLine("trial,moment,target,finish_observed,finish_read,gap,access,outside_request,fuel,pending,spent,assistance,on_course");
        wiring.WriteLine("trial,gap_row_hex,work_rows_hex,body_rows_hex,fill_rows_hex,use_rows_hex");
        for (var index = 0; index < GoalDriveExperiments.Names.Length; index++)
        {
            var trial = GoalDriveExperiments.Run(index);
            Row(wiring, trial.Name, Convert.ToHexString(trial.GapRow), Convert.ToHexString(trial.WorkRows), Convert.ToHexString(trial.BodyRows), "", "");
            foreach (var beat in trial.Timeline)
                Row(drive, trial.Name, beat.Label, beat.Target, beat.Finished, beat.FinishRead, beat.Gap,
                    beat.Access, beat.ExternalRequest, beat.Fuel, beat.Pending, beat.Spent, beat.HelpReleased, beat.OnCourse);
        }

        using var maintenance = File.CreateText(Path.Combine(folder, "maintenance.csv"));
        maintenance.WriteLine("moment,target,gap,material_hex");
        var reference = new PortStructure();
        var binding = reference.Create(0, 0, 1, 0);
        var cell = new GoalMaintenance(reference.Connect(reference.Publish(binding)));
        Row(wiring, "Maintain an occupied cell", Convert.ToHexString(cell.GapRow()), "", "",
            Convert.ToHexString(cell.FillRows()), Convert.ToHexString(cell.UseRows()));
        void Sample(string label)
        {
            var beat = cell.Read();
            Row(maintenance, label, beat.Target, beat.Gap, beat.Material.ToString("X2"));
        }
        Sample("Target off");
        reference.StoreObservation(binding, false, false, 1);
        Sample("Set target to occupied");
        cell.Settle(); Sample("Supply fills the cell");
        cell.Settle(); Sample("Settle again while full");
        cell.TakeOne(); Sample("First external use empties the cell");
        cell.Settle(); Sample("Supply fills it again");
        cell.TakeOne(); Sample("Second external use empties the cell");
        cell.Settle(); Sample("Supply exhausted, target still active");
        Console.WriteLine($"Recorded {GoalDriveExperiments.Names.Length} drive trials and one standing-target run.");
    }

    private static void Row(TextWriter file, params object[] values) => file.WriteLine(string.Join(',',
        values.Select(value => "\"" + Convert.ToString(value, CultureInfo.InvariantCulture)!.Replace("\"", "\"\"") + "\"")));
}
