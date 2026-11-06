namespace Gojo.Experiments.OrnamentPathCampaign;

internal sealed record Snapshot(double[] Means, int[] Available, double Viable);
internal sealed record Candidate(int Id, string Hex, Snapshot Training, double Score);
internal sealed record Winner(string Brief, string Arm, int Replicate, string Hex, int Trial,
    double TrainingUtility, double ValidationUtility, int Charged, int Distinct);
internal sealed record Votes(int[] Eligible, int[] Positive, int[] Negative);
internal sealed record Island(Winner Winner, Votes Pivots);

internal static class Competition
{
    public static bool Viable(PathTrace trace) => trace.Edges.Length >= 2 &&
        trace.Edges.Any(e => (e.Direction & 3) != (trace.Edges[0].Direction & 3));

    // Unavailable desired evidence contributes zero. A blank or one-axis trace
    // cannot collect positive evidence by avoiding all opportunities to draw.
    public static Snapshot Assess(byte[] bytes, Scenario[] cases, PathMode mode=PathMode.Live)
    {
        int n=PathJudge.Criteria.Length;
        var sums=new double[n];
        var opportunities=new int[n];
        int viable=0;
        foreach (var condition in cases)
        {
            var trace=PathMachine.Run(bytes,condition.Input,mode);
            var reads=PathJudge.Measure(trace);
            bool admissible=Viable(trace);
            if (admissible) viable++;
            for(int i=0;i<n;i++)
            {
                double read=reads[i];
                if(double.IsNaN(read)) continue;
                if (!double.IsFinite(read) || read < -1e-8 || read > 1+1e-8)
                    throw new InvalidDataException($"Out-of-range {PathJudge.Criteria[i].Key}: {read}");
                opportunities[i]++;
                if(admissible) sums[i]+=Math.Clamp(read,0,1);
            }
        }
        for(int i=0;i<n;i++) sums[i]/=cases.Length;
        return new(sums,opportunities,(double)viable/cases.Length);
    }
    public static double Score(Snapshot s,double[] weights)
    {
        double score=0;
        for(int i=0;i<weights.Length;i++) score+=s.Means[i]*weights[i];
        return score;
    }

    public static IReadOnlyList<Candidate> Rank(IEnumerable<Candidate> candidates,int take) =>
        candidates.OrderByDescending(x=>x.Score).ThenBy(x=>x.Hex.Length).ThenBy(x=>x.Id)
            .DistinctBy(x=>x.Hex).Take(take).ToArray();

    public static Island Train(string directory,Brief brief,string arm,int replicate)
    {
        var train=Design.Scenarios("train",replicate);
        var validation=Design.Scenarios("validation");
        var weights=Design.Weights(brief,arm);
        int n=weights.Length;
        var votes=new Votes(new int[n],new int[n],new int[n]);
        var distinct=new HashSet<string>();
        IReadOnlyList<Candidate> elite=[];
        string file=Path.Combine(directory,"histories",$"{brief.Name}_{arm}_{replicate}.csv.gz");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        using var ledger=Record.GzipWriter(file);
        ledger.WriteLine("trial,generation,parent_a,parent_b,operator,genome,score,viable,"+
            string.Join(",",PathJudge.Criteria.Select(c=>c.Key)));
        for(int gen=0;gen<Design.Generations;gen++)
        {
            var generation=new List<Candidate>();
            for(int slot=0;slot<Design.Population;slot++)
            {
                int id=gen*Design.Population+slot;
                var rng=new Random(720411+104729*replicate+16699*id);
                Candidate? a=null,b=null;
                byte[] bytes;
                string operation;
                if(gen==0 || arm=="random" || slot%6==0)
                {
                    bytes=arm=="unseeded" ? PathMachine.CreateUnrestricted(rng) :
                        PathMachine.Create(rng,gen==0 ? slot%6 : rng.Next(6));
                    operation="founder";
                }
                else
                {
                    a=elite[rng.Next(elite.Count)];
                    if(slot%5==0)
                    {
                        b=elite[rng.Next(elite.Count)];
                        bytes=PathMachine.Cross(Convert.FromHexString(a.Hex),Convert.FromHexString(b.Hex),rng);
                        operation="exchange";
                    }
                    else
                    {
                        bytes=PathMachine.Mutate(Convert.FromHexString(a.Hex),rng);
                        operation="mutate";
                    }
                }
                var snap=Assess(bytes,train);
                var child=new Candidate(id,Convert.ToHexString(bytes),snap,Score(snap,weights));
                if(arm=="full" && a!=null && b==null) Attribute(votes,a,child,weights);
                generation.Add(child);
                distinct.Add(child.Hex);
                ledger.WriteLine($"{id},{gen},{a?.Id},{b?.Id},{operation},{child.Hex},{Record.F(child.Score)},{Record.F(snap.Viable)},"+
                    string.Join(",",snap.Means.Select(Record.F)));
            }
            elite=Rank(elite.Concat(generation),12);
        }
        var ranked=elite.Take(Design.Shortlist).Select(c=>
            (Candidate:c,Validation:Score(Assess(Convert.FromHexString(c.Hex),validation),weights)))
            .OrderByDescending(x=>x.Validation).ThenByDescending(x=>x.Candidate.Score)
            .ThenBy(x=>x.Candidate.Hex.Length).ThenBy(x=>x.Candidate.Id).First();
        var w=new Winner(brief.Name,arm,replicate,ranked.Candidate.Hex,ranked.Candidate.Id,
            ranked.Candidate.Score,ranked.Validation,Design.Population*Design.Generations,distinct.Count);
        return new(w,votes);
    }

    private static void Attribute(Votes votes,Candidate parent,Candidate child,double[] weights)
    {
        double delta=child.Score-parent.Score;
        for(int i=0;i<weights.Length;i++)
        {
            if(weights[i]==0 || parent.Training.Available[i]+child.Training.Available[i]==0) continue;
            votes.Eligible[i]++;
            double rest=delta-weights[i]*(child.Training.Means[i]-parent.Training.Means[i]);
            if(delta>1e-9 && rest<=1e-9) votes.Positive[i]++;
            if(delta < -1e-9 && rest>= -1e-9) votes.Negative[i]++;
        }
    }
}
