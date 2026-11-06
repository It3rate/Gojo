using System.Numerics;
using Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;

namespace Gojo.Experiments.OrnamentSeamFollowup;

internal static partial class SeamSearch
{
    private sealed record Observation(int Rep,string TrainingCue,string Cue,CampaignEpisode Episode,int Marker,
        bool MarkerVisible,Result Outcome,Result Idle);
    private sealed record Contrast(string Name,string LeftTraining,string LeftCue,string RightTraining,string RightCue);
    private static readonly Contrast[] Contrasts =
    [
        new("learn-location","seam","seam","decoy","decoy"),
        new("displace-same-winner","seam","seam","seam","decoy"),
        new("supplied-marker-vs-original","seam","seam","original","original"),
        new("marker-vs-hold","seam","seam","idle","seam")
    ];

    private static void Assay(string folder,Seal seal)
    {
        var locked=VerifyLock(folder,seal);
        string output=Path.Combine(folder,"reserve");
        Directory.CreateDirectory(output);
        var episodes=CampaignScience.Episodes("reserve","seam");
        var all=new List<Observation>();
        var noEdits=TournamentSearch.Idle().Compile();
        foreach(var winner in locked.Winners)
        {
            var runtime=OrnamentProgram.Decode(Convert.FromHexString(winner.GenomeHex)).Compile();
            foreach(string cue in Cues)
            foreach(var episode in episodes)
            {
                var frame=WithCue(episode,cue);
                var experiment=episode with { Input=frame };
                int marker=cue=="original"? -1 : BitOperations.TrailingZeroCount(frame.Seed);
                all.Add(new(winner.Replicate,winner.Cue,cue,episode,marker,
                    marker>=0 && ((frame.Hidden>>marker)&1)==0,Evaluate(runtime,experiment),Evaluate(noEdits,experiment)));
            }
        }
        Rows(Path.Combine(output,"cases.csv"),w=>
        {
            w.WriteLine("replicate,training_cue,executed_cue,family,group,variant,width,start,marker,marker_visible,marker_ink,marker_protected,utility,error,violations,exact,idle_utility,delta_vs_idle,ink_hex,edits,attempts,blocked,stock");
            foreach(var row in all)
            {
                var input=row.Episode.Input;
                Csv(w,row.Rep,row.TrainingCue,row.Cue,row.Episode.Family,row.Episode.Group,row.Episode.Variant,input.Width,
                    input.Start,row.Marker,row.Marker<0?"":row.MarkerVisible.ToString(),
                    row.Marker<0?"":(((input.Initial>>row.Marker)&1)!=0).ToString(),
                    row.Marker<0?"":(((input.Protected>>row.Marker)&1)!=0).ToString(),
                    row.Outcome.Utility,row.Outcome.Error,row.Outcome.Violations,row.Outcome.Exact,row.Idle.Utility,
                    row.Outcome.Utility-row.Idle.Utility,row.Outcome.Ink.ToString("X"),row.Outcome.Edits,row.Outcome.Attempts,row.Outcome.Blocked,row.Outcome.Stock);
            }
        });
        Rows(Path.Combine(output,"matrix.csv"),w=>
        {
            w.WriteLine("training_cue,executed_cue,cases,mean_utility,mean_idle,mean_delta_vs_idle,mean_edits,improved,tied,regressed");
            foreach(var group in all.GroupBy(r=>(r.TrainingCue,r.Cue)))
                Csv(w,group.Key.TrainingCue,group.Key.Cue,group.Count(),group.Average(r=>r.Outcome.Utility),
                    group.Average(r=>r.Idle.Utility),group.Average(r=>r.Outcome.Utility-r.Idle.Utility),
                    group.Average(r=>r.Outcome.Edits),group.Count(r=>r.Outcome.Utility>r.Idle.Utility),
                    group.Count(r=>r.Outcome.Utility==r.Idle.Utility),group.Count(r=>r.Outcome.Utility<r.Idle.Utility));
        });
        var inputs=all.ToDictionary(r=>(r.Rep,r.TrainingCue,r.Cue,r.Episode.Group,r.Episode.Variant));
        Rows(Path.Combine(output,"verdicts.csv"),vw=>Rows(Path.Combine(output,"groups.csv"),gw=>
        {
            vw.WriteLine("contrast,margin,parent_groups,positive,negative,within,mean_delta,material_changed,status");
            gw.WriteLine("contrast,family,group,pairs,mean_left,mean_right,mean_delta,material_changed");
            foreach(var contrast in Contrasts)
            {
                var pairs=new List<CampaignPair>();
                foreach(var ep in episodes)
                for(int rep=0;rep<Replicates;rep++)
                {
                    var left=inputs[(rep,contrast.LeftTraining,contrast.LeftCue,ep.Group,ep.Variant)].Outcome;
                    var right=contrast.RightTraining=="idle"
                        ?inputs[(rep,contrast.LeftTraining,contrast.RightCue,ep.Group,ep.Variant)].Idle
                        :inputs[(rep,contrast.RightTraining,contrast.RightCue,ep.Group,ep.Variant)].Outcome;
                    pairs.Add(new(ep.Group,left.Utility,right.Utility,left.Ink==right.Ink && left.Stock==right.Stock && left.Edits==right.Edits));
                }
                var family=episodes.GroupBy(e=>e.Group).ToDictionary(g=>g.Key,g=>g.First().Family);
                foreach(var group in pairs.GroupBy(p=>p.Group))
                    Csv(gw,contrast.Name,family[group.Key],group.Key,group.Count(),group.Average(p=>p.LeftUtility),
                        group.Average(p=>p.RightUtility),group.Average(p=>p.LeftUtility-p.RightUtility),group.Count(p=>!p.SameMaterial));
                var hypothesis=new CampaignHypothesis(contrast.Name,"seam-cue","seam","all",
                    new CampaignArm("uniform",WireMode.Live),new CampaignArm("uniform",WireMode.Live),
                    "higher",20,"A correct seam cue should improve this contrast by the preregistered margin.");
                var verdict=CampaignScience.Classify(hypothesis,pairs);
                Csv(vw,contrast.Name,20,verdict.Groups,verdict.Positive,verdict.Negative,verdict.Within,
                    verdict.MeanDelta,pairs.Count(p=>!p.SameMaterial),verdict.Status);
            }
        }));
        string[] files=["cases.csv","matrix.csv","groups.csv","verdicts.csv"];
        Save(Path.Combine(output,"complete.json"),new { locked.WinnerHash, Cases=all.Count, Parents=episodes.Select(e=>e.Group).Distinct().Count(),
            Files=files.ToDictionary(f=>f,f=>HashFile(Path.Combine(output,f))) });
        Console.WriteLine("Seam reserve assessment complete: "+all.Count+" controller/cue/scenario readings.");
    }
}
