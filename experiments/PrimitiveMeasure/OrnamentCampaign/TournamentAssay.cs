using System.Globalization;
using System.Security.Cryptography;

namespace Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;

public static partial class CampaignTournament
{
    private sealed record AssayRow(FrozenWinner Winner, CampaignEpisode Episode, CampaignArm Arm,
        RailResult Result, CampaignScore Score, CampaignScore Idle);
    private readonly record struct ArmKey(string TrainingGoal, string Method, int Replicate, string Goal,
        string Family, string Group, int Variant, WireMode Wire, string Treatment);
    private sealed record CompletedAssay(string LockHash, string Split, int Cases, int Hypotheses,
        Dictionary<string,string> FileHashes);
    private static readonly string[] AssayFiles = ["cases.csv", "champions.csv", "hypotheses.csv", "groups.csv"];

    public static void Assay(string folder, TournamentConfig config, string protocolHash, int workers,
        string split = "test")
    {
        if (split is not ("test" or "reserve")) throw new ArgumentException("Use an independent assessment split.", nameof(split));
        Check(config, protocolHash, workers);
        using var lease = Lease(folder);
        var winners = ReadFrozen(folder, config, protocolHash);
        var freeze = ReadJson<FreezeFile>(Path.Combine(folder, "frozen.json"));
        var output = Path.Combine(folder, split);
        Directory.CreateDirectory(output);
        var marker = Path.Combine(output, "complete.json");
        if (File.Exists(marker))
        {
            var previous = ReadJson<CompletedAssay>(marker);
            if (previous.LockHash != freeze.LockHash || previous.Split != split ||
                AssayFiles.Any(name => !previous.FileHashes.TryGetValue(name, out var h) || h != HashFile(Path.Combine(output, name))))
                throw new InvalidDataException("A completed assay no longer matches its locked artifacts.");
            return;
        }

        var hypotheses = CampaignScience.Hypotheses;
        var banks = Objectives.ToDictionary(g => g, g => CampaignScience.Episodes(split, g));
        var idle = TournamentSearch.Idle().Compile();
        var observations = new List<AssayRow>[winners.Length];
        Parallel.For(0, winners.Length, new ParallelOptions {MaxDegreeOfParallelism=workers}, i =>
        {
            var winner = winners[i];
            var machine = OrnamentProgram.Decode(Convert.FromHexString(winner.GenomeHex)).Compile();
            var list = observations[i] = [];
            foreach (var episode in banks[winner.Goal])
            {
                var arms = hypotheses.Where(h => h.Goal == episode.Goal && h.Family == episode.Family)
                    .SelectMany(h => new[] { h.Left, h.Right }).Where(a => a.Method == winner.Method)
                    .Append(new CampaignArm(winner.Method, WireMode.Live)).Distinct();
                foreach (var arm in arms)
                {
                    var input = CampaignScience.ArmInput(episode, arm);
                    var result = machine.Run(input, arm.Wire);
                    var score = CampaignScience.Evaluate(episode, result, input);
                    var unchanged = CampaignScience.Evaluate(episode, idle.Run(input, arm.Wire), input);
                    list.Add(new(winner, episode, arm, result, score, unchanged));
                }
            }
        });
        var rows = observations.SelectMany(x => x).ToArray();
        var index = rows.ToDictionary(x => new ArmKey(x.Winner.Goal, x.Winner.Method, x.Winner.Replicate,
            x.Episode.Goal, x.Episode.Family, x.Episode.Group, x.Episode.Variant, x.Arm.Wire, x.Arm.Treatment));
        WriteCsv(Path.Combine(output, "cases.csv"), writer =>
        {
            writer.WriteLine("training_goal,method,replicate,search_seed,winner_id,goal,family,group,variant,wire,treatment,utility,error,violations,exact,idle_utility,delta_vs_idle,final_ink_hex,stock,edits,attempts,blocked,budget_refusals,unknown,missing,external_delta,trace_hash");
            foreach (var row in rows)
                Csv(writer, row.Winner.Goal, row.Winner.Method, row.Winner.Replicate, row.Winner.Seed, row.Winner.Id,
                    row.Episode.Goal, row.Episode.Family, row.Episode.Group, row.Episode.Variant,
                    row.Arm.Wire, row.Arm.Treatment, row.Score.Utility, row.Score.Error, row.Score.Violations,
                    row.Score.Exact, row.Idle.Utility, row.Score.Utility - row.Idle.Utility,
                    row.Result.Ink.ToString("X"), row.Result.Stock, row.Result.Edits, row.Result.Attempts,
                    row.Result.Blocked, row.Result.BudgetRefusals, row.Result.Unresolved, row.Result.Missing,
                    row.Result.ExternalDelta, row.Result.TraceHash.ToString("X16"));
        });
        WriteCsv(Path.Combine(output, "champions.csv"), writer =>
        {
            writer.WriteLine("training_goal,method,replicate,winner_id,goal,cases,mean_utility,mean_idle,mean_change,exact,improved,tied,regressed,mean_edits,violations");
            var native = rows.Where(r => r.Arm.Wire == WireMode.Live && r.Arm.Treatment == "native")
                .GroupBy(r => (r.Winner.Goal,r.Winner.Method,r.Winner.Replicate,r.Winner.Id, ReadGoal:r.Episode.Goal));
            foreach (var g in native)
                Csv(writer,g.Key.Goal,g.Key.Method,g.Key.Replicate,g.Key.Id,g.Key.ReadGoal,g.Count(),
                    g.Average(r=>r.Score.Utility),g.Average(r=>r.Idle.Utility),g.Average(r=>r.Score.Utility-r.Idle.Utility),
                    g.Count(r=>r.Score.Exact),g.Count(r=>r.Score.Utility>r.Idle.Utility),g.Count(r=>r.Score.Utility==r.Idle.Utility),
                    g.Count(r=>r.Score.Utility<r.Idle.Utility),g.Average(r=>r.Result.Edits),g.Sum(r=>r.Score.Violations));
        });
        WriteCsv(Path.Combine(output, "hypotheses.csv"), hypothesisWriter =>
            WriteCsv(Path.Combine(output, "groups.csv"), groupWriter =>
            {
                hypothesisWriter.WriteLine("id,mechanism,goal,family,relation,margin,groups,positive,negative,within,mean_delta,status");
                groupWriter.WriteLine("id,goal,family,group,matched_pairs,mean_left,mean_right,delta,all_same_material");
                foreach (var h in hypotheses)
                {
                    var pairs = new List<CampaignPair>();
                    foreach (var e in banks[h.Goal].Where(e => e.Family == h.Family))
                    for (int rep = 0; rep < config.Replicates; rep++)
                    {
                        AssayRow Read(CampaignArm arm) => index[new(h.Goal,arm.Method,rep,e.Goal,e.Family,
                            e.Group,e.Variant,arm.Wire,arm.Treatment)];
                        var a = Read(h.Left); var b = Read(h.Right);
                        bool same = a.Result.Ink == b.Result.Ink && a.Result.Stock == b.Result.Stock && a.Result.Edits == b.Result.Edits;
                        pairs.Add(new(e.Group,a.Score.Utility,b.Score.Utility,same));
                    }
                    var verdict = CampaignScience.Classify(h,pairs);
                    Csv(hypothesisWriter,h.Id,h.Mechanism,h.Goal,h.Family,h.Relation,h.Margin,verdict.Groups,
                        verdict.Positive,verdict.Negative,verdict.Within,verdict.MeanDelta,verdict.Status);
                    foreach (var g in pairs.GroupBy(p=>p.Group))
                        Csv(groupWriter,h.Id,h.Goal,h.Family,g.Key,g.Count(),g.Average(p=>p.LeftUtility),
                            g.Average(p=>p.RightUtility),g.Average(p=>p.LeftUtility-p.RightUtility),g.All(p=>p.SameMaterial));
                }
            }));
        SaveJson(marker,new CompletedAssay(freeze.LockHash,split,rows.Length,hypotheses.Length,
            AssayFiles.ToDictionary(n=>n,n=>HashFile(Path.Combine(output,n)))));
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
    private static void WriteCsv(string path, Action<TextWriter> write) => Atomic(path, stream =>
    {
        using var writer = new StreamWriter(stream, leaveOpen:true);
        write(writer);
    });
    private static void Csv(TextWriter writer, params object?[] fields) =>
        writer.WriteLine(string.Join(',', fields.Select(f => "\"" +
            (Convert.ToString(f, CultureInfo.InvariantCulture) ?? "").Replace("\"", "\"\"") + "\"")));
}
