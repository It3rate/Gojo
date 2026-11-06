using Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;

namespace Gojo.Experiments.OrnamentFollowup;

public static partial class Followup
{
    private sealed record Result(Winner Winner,CampaignEpisode Episode,CampaignScore Score,RailResult Rail);
    private sealed record Completion(string WinnerLock,Dictionary<string,string> Files);
    private static void Assay(string folder,FollowupSpec spec,ChampionLock champions,Func<string,string,CampaignEpisode[]> supply)
    {
        var dest = Path.Combine(folder,"reserve");
        Directory.CreateDirectory(dest);
        var marker = Path.Combine(dest,"complete.json");
        string[] files = ["cases.csv","groups.csv","contrasts.csv","replay-inputs.json","replay-expected.json"];
        if(File.Exists(marker))
        {
            var done = Read<Completion>(marker);
            if(done.WinnerLock!=champions.Hash || files.Any(f=>!done.Files.TryGetValue(f,out var value) || value!=FileHash(Path.Combine(dest,f))))
                throw new InvalidDataException("Completed reserve assay differs from its bound outputs.");
            return;
        }
        var results = new List<Result>();
        foreach(var winner in champions.Winners)
        {
            var program = OrnamentProgram.Decode(Convert.FromHexString(winner.Finalists[0].Source.GenomeHex)).Compile();
            foreach (var episode in supply("reserve",winner.Goal))
            {
                var rail = program.Run(episode.Input);
                results.Add(new(winner,episode,CampaignScience.Evaluate(episode,rail),rail));
            }
        }
        CsvFile(Path.Combine(dest,files[0]), output=>
        {
            output.WriteLine("goal,arm,replicate,parent_group,variant,family,utility,exact,violations,genome_bytes,ink_hex,edits,blocked");
            foreach(var q in results) Row(output,q.Winner.Goal,q.Winner.Arm,q.Winner.Replicate,q.Episode.Group,
                q.Episode.Variant,q.Episode.Family,q.Score.Utility,q.Score.Exact,q.Score.Violations,
                q.Winner.Finalists[0].Source.GenomeHex.Length/2,q.Rail.Ink.ToString("X"),q.Rail.Edits,q.Rail.Blocked);
        });
        var lookup = results.ToDictionary(q=>(q.Winner.Goal,q.Winner.Arm,q.Winner.Replicate,q.Episode.Group,q.Episode.Variant));
        CsvFile(Path.Combine(dest,files[1]),groups=> CsvFile(Path.Combine(dest,files[2]),contrasts=>
        {
            groups.WriteLine("goal,control,group,pairs,learned_mean,control_mean,delta");
            contrasts.WriteLine("goal,control,prediction,margin,groups,positive,negative,within,group_mean_difference,status");
            foreach (var goal in spec.Goals)
            foreach(var control in new[]{"uniform","permuted"})
            {
                var pairs = results.Where(q=>q.Winner.Goal==goal && q.Winner.Arm=="learned").Select(q=>
                {
                    var other = lookup[(goal,control,q.Winner.Replicate,q.Episode.Group,q.Episode.Variant)];
                    return new CampaignPair(q.Episode.Group,q.Score.Utility,other.Score.Utility,q.Rail.Ink==other.Rail.Ink);
                }).ToArray();
                var hypothesis = new CampaignHypothesis(spec.Id+"-"+goal+"-"+control,"proposer-transfer",goal,"all",
                    new("learned",WireMode.Live),new(control,WireMode.Live),spec.ExpectedRelation,spec.Margin,spec.Prediction);
                var verdict = CampaignScience.Classify(hypothesis,pairs);
                Row(contrasts,goal,control,spec.ExpectedRelation,spec.Margin,verdict.Groups,verdict.Positive,verdict.Negative,
                    verdict.Within,verdict.MeanDelta,verdict.Status);
                foreach(var group in pairs.GroupBy(p=>p.Group)) Row(groups,goal,control,group.Key,group.Count(),
                    group.Average(p=>p.LeftUtility),group.Average(p=>p.RightUtility),group.Average(p=>p.LeftUtility-p.RightUtility));
            }
        }));
        var replay = results.GroupBy(x=>(x.Winner.Goal,x.Winner.Arm,x.Winner.Replicate)).Select(g=>g.First()).ToArray();
        Write(Path.Combine(dest,files[3]), replay.Select(q=>
            new CampaignCli.ReplayInput(q.Winner.Finalists[0].Source.GenomeHex,q.Episode.Input)).ToArray());
        Write(Path.Combine(dest,files[4]), replay.Select(q=>q.Rail).ToArray());
        Write(marker,new Completion(champions.Hash,files.ToDictionary(f=>f,f=>FileHash(Path.Combine(dest,f)))));
    }
}
