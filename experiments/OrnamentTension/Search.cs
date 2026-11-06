using System.Collections.Concurrent;
using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Experiments.OrnamentTension;

internal sealed record PanelChoice(int Slots,string Selected,string Random,double Validation,double DeclaredValue);
internal sealed record PilotResult(int Family,int Replicate,PanelChoice[] Panels);
internal sealed record Trial(int Index,Recipe Recipe,double Score);
internal sealed record Island(Winner Winner,string HistoryHash);
internal static class Search
{
    private static readonly ConcurrentDictionary<(PathInput,int),PatternSignature[]> Memory=new();
    private static readonly byte[][] Commonplace=[
        PathMachine.Encode(new PathOwner(8,85,0,[StrokeRow.Bind(1),StrokeRow.Bind(7),StrokeRow.Bind(5),StrokeRow.Bind(3)])),
        PathMachine.Encode(new PathOwner(8,85,0,[StrokeRow.Bind(0),StrokeRow.Bind(2,1),StrokeRow.Bind(4),StrokeRow.Bind(6,1)])),
        PathMachine.Encode(new PathOwner(8,85,0,[StrokeRow.Bind(0),StrokeRow.Bind(1,1),StrokeRow.Bind(4),StrokeRow.Bind(5,1)]))];
    public static PatternSignature[] Archive(Setting s)=>Memory.GetOrAdd((s.Input,s.Slots),key=>
        Commonplace.Select(p=>TensionCritics.Describe(PathMachine.Run(p,key.Item1),key.Item2)).ToArray());
    public static (TensionResult Run,CriticRead Read) Draw(Recipe recipe,Setting s,int family,FeedbackMode mode,bool heldout=false,bool witness=false)
    {
        var result=TensionMachine.Run(Convert.FromHexString(recipe.Path),s.Input,s.Slots,
            Convert.FromHexString(recipe.Policy),mode,witness);
        var read=TensionCritics.Measure(result.Trace,s.Slots,EngineLibrary.Names[family],heldout,Archive(s));
        if(!double.IsFinite(read.Broad)||read.Broad is <0 or >1)throw new InvalidDataException("Invalid broad reading");
        return (result,read);
    }
    private static Recipe Founder(Random rng,int f,bool literal=false)
    {
        byte[] path=EngineLibrary.Create(f);
        if(!literal)for(int i=0,edits=rng.Next(1,5);i<edits;i++)path=EngineLibrary.Mutate(path,rng);
        return new(Convert.ToHexString(path),Convert.ToHexString(literal ? TensionMachine.DefaultPolicy() : TensionMachine.RandomPolicy(rng)));
    }
    private static string PanelKey(byte[] shares)=>Convert.ToHexString(shares);

    // Include every two-, three- and four-way subset and additional nonuniform allocations.
    public static JudgePortfolio[] CommitteePool(int rep)
    {
        var choices=new Dictionary<string,JudgePortfolio>();
        void Add(JudgePortfolio p)=>choices.TryAdd(PanelKey(p.Shares),p);
        Add(Design.Declared());
        for(int mask=1;mask<256;mask++)
        {
            var parts=Enumerable.Range(0,8).Where(i=>(mask&(1<<i))!=0).ToArray();
            if(parts.Length is <2 or >4)continue;
            var shares=new byte[8];
            for(int i=0;i<parts.Length;i++)shares[parts[i]]=(byte)(16/parts.Length+(i<16%parts.Length ? 1 : 0));
            Add(new(shares));
        }
        var rng=new Random(98653+rep*7919);
        while(choices.Count<384)Add(JudgePortfolio.Sample(rng));
        return choices.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>p.Value).ToArray();
    }
    public static PilotResult Nominate(int family,int rep,string root)
    {
        var train=Design.Settings("train",rep);
        var meta=Design.Settings("meta",rep);
        var pool=CommitteePool(rep);
        var bank=Enumerable.Range(0,Design.Pilot)
            .Select(i=>Founder(new Random(610163+rep*104729+i*16699),family,i==0)).ToArray();
        double[,,] local=new double[Design.Pilot,3,8];
        double[,] transferred=new double[Design.Pilot,3];
        for(int i=0;i<bank.Length;i++)
        {
            foreach(var s in train)
            {
                var read=Draw(bank[i],s,family,FeedbackMode.Live).Read;
                int slot=Array.IndexOf(Design.Slots,s.Slots);
                for(int j=0;j<8;j++)local[i,slot,j]+=read.Values[j]/2;
            }
            foreach(var s in meta)transferred[i,Array.IndexOf(Design.Slots,s.Slots)]+=
                Draw(bank[i],s,family,FeedbackMode.Live).Read.Broad/Design.Bases["meta"].Length;
        }
        var result=new List<PanelChoice>();
        Directory.CreateDirectory(Path.Combine(root,"panels"));
        using var ledger=Files.Gzip(Path.Combine(root,"panels",$"{family}-{rep}.csv.gz"));
        ledger.WriteLine("slots,shares,transfer,first,second,third,fourth");
        for(int slot=0;slot<3;slot++)
        {
            var candidates=new List<(string Key,double Broad)>();
            foreach(var panel in pool)
            {
                var shares=panel.Shares;
                double NominationScore(int i)
                {
                    double x=0;
                    for(int k=0;k<8;k++)x+=shares[k]*local[i,slot,k];
                    return x;
                }
                var ids=Enumerable.Range(0,bank.Length).OrderByDescending(NominationScore)
                    .ThenBy(i=>i).DistinctBy(i=>bank[i]).Take(4).ToArray();
                var score=ids.Average(i=>transferred[i,slot]);
                string key=PanelKey(shares);
                candidates.Add((key,score));
                ledger.WriteLine($"{Design.Slots[slot]},{key},{Files.F(score)},{string.Join(",",ids)}");
            }
            var best=candidates.OrderByDescending(x=>x.Broad).ThenBy(x=>x.Key,StringComparer.Ordinal).First();
            var random=pool[new Random(10709+rep*277+slot*331+family*499).Next(pool.Length)];
            result.Add(new(Design.Slots[slot],best.Key,PanelKey(random.Shares),best.Broad,
                candidates.Single(x=>x.Key==PanelKey(Design.Declared().Shares)).Broad));
        }
        return new(family,rep,result.ToArray());
    }
    public static Island Train(string root,int family,int rep,string arm,PilotResult pilot)
    {
        var committee=pilot.Panels.ToDictionary(x=>x.Slots,x=>arm=="declared" ? Design.Declared() :
            new JudgePortfolio(Convert.FromHexString(arm=="random-panel" ? x.Random : x.Selected)));
        var mode=Design.Mode(arm);
        var train=Design.Settings("train",rep);var validation=Design.Settings("validation",rep);
        double Rate(Recipe recipe,Setting[] cases)=>cases.Average(s=>committee[s.Slots].Score(Draw(recipe,s,family,mode).Read));
        var distinct=new HashSet<Recipe>();
        var elites=Array.Empty<Trial>();
        Directory.CreateDirectory(Path.Combine(root,"histories"));
        string file=Path.Combine(root,"histories",$"{family}-{arm}-{rep}.csv.gz");
        using(var ledger=Files.Gzip(file))
        {
            ledger.WriteLine("id,parent,donor,edit,path,policy,score");
            for(int g=0;g<Design.Generations;g++)
            {
                var population=new List<Trial>();
                for(int p=0;p<Design.Population;p++)
                {
                    int id=g*Design.Population+p;
                    var rng=new Random(681793+rep*104729+id*16699);
                    Trial? parent=null,donor=null;Recipe recipe;string edit;
                    if(g==0||p%6==0||arm=="random-draw")
                    {
                        recipe=Founder(rng,family,id==0);
                        if(arm=="policy-only")recipe=recipe with {Path=Convert.ToHexString(EngineLibrary.Create(family))};
                        edit="founder";
                    }
                    else
                    {
                        parent=elites[rng.Next(elites.Length)]; recipe=parent.Recipe;
                        if(arm=="policy-only"||rng.Next(3)==0)
                        {
                            recipe=recipe with {Policy=Convert.ToHexString(TensionMachine.MutatePolicy(Convert.FromHexString(recipe.Policy),rng))};
                            edit="policy";
                        }
                        else if(p%5==0)
                        {
                            donor=elites[rng.Next(elites.Length)];
                            recipe=recipe with {Path=Convert.ToHexString(EngineLibrary.Cross(Convert.FromHexString(recipe.Path),
                                Convert.FromHexString(donor.Recipe.Path),rng))};
                            edit="exchange";
                        }
                        else
                        {
                            recipe=recipe with {Path=Convert.ToHexString(EngineLibrary.Mutate(Convert.FromHexString(recipe.Path),rng))};
                            edit="path";
                        }
                    }
                    double score=Rate(recipe,train);
                    population.Add(new(id,recipe,score));distinct.Add(recipe);
                    ledger.WriteLine($"{id},{parent?.Index},{donor?.Index},{edit},{recipe.Path},{recipe.Policy},{Files.F(score)}");
                }
                elites=elites.Concat(population).OrderByDescending(t=>t.Score).ThenBy(t=>t.Recipe.Path.Length)
                    .ThenBy(t=>t.Index).DistinctBy(t=>t.Recipe).Take(10).ToArray();
            }
        }
        var winner=elites.Take(6).Select(t=>(t,score:Rate(t.Recipe,validation)))
            .OrderByDescending(w=>w.score).ThenByDescending(w=>w.t.Score).ThenBy(w=>w.t.Index).First();
        return new(new(family,arm,rep,winner.t.Recipe,winner.t.Score,winner.score,winner.t.Index,distinct.Count),Files.Digest(file));
    }
}
