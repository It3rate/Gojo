using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.OrnamentTension;

namespace Gojo.Experiments.OrnamentTensionSeries;

internal sealed record SeriesLock(string Seal,Champion[] Winners);
internal sealed record Champion(Tray Tray,string Rule,double Training,double Validation,int Trial);
internal sealed record SeriesSeal(string Host,string Generator,string Parent,string ParentBlind,string ParentReport,
    string Settings,string Protocol);
internal sealed record Scored(Champion Pick,Setting Setting,SeriesMode Mode,Collection Measures);

internal static partial class Program
{
    private const int Budget=96;
    private static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase,PropertyNameCaseInsensitive=true};
    private static string Project=>Path.Combine("experiments","OrnamentTensionSeries");
    private static T Load<T>(string path)=>JsonSerializer.Deserialize<T>(File.ReadAllText(path),Json)!;
    private static string Hash(string p)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p)));
    private static void Save<T>(string file,T obj)
    {
        string contents=JsonSerializer.Serialize(obj,Json);
        if(File.Exists(file))
        {
            if(File.ReadAllText(file)!=contents)throw new IOException("Existing record differs: "+file);
            return;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
        string temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllText(temp,contents); File.Move(temp,file);}
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
    private static string F(double d)=>d.ToString("R",CultureInfo.InvariantCulture);
    private static string Source(string run,string suffix)=>Path.Combine(Directory.GetParent(Path.GetFullPath(run))!.FullName,"wave1",suffix);
    private static int Main(string[] args)
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
        if(args is ["play",var job,var output])
        {
            var sheet=Load<Sheet>(job);
            Play(sheet,output);return 0;
        }
        if(args.Length<2)throw new ArgumentException("freeze RUN PARENT-REPORT | train RUN [workers] | assay RUN RELEASE | verify RUN");
        string dir=Path.GetFullPath(args[1]);
        switch(args[0])
        {
            case "freeze" when args.Length==3: Freeze(dir,args[2]);break;
            case "train": Verify(dir);Train(dir,args.Length>2 ? int.Parse(args[2]) : 6);break;
            case "assay" when args.Length==3: Verify(dir);Assay(dir,args[2]);break;
            case "verify": Verify(dir);break;
            default: throw new ArgumentException("Unknown phase.");
        }
        return 0;
    }
    private static void Freeze(string dir,string report)
    {
        if(Directory.Exists(dir) && Directory.EnumerateFileSystemEntries(dir).Any())throw new IOException("Run directory used.");
        Directory.CreateDirectory(dir);
        File.Copy(Path.Combine(Project,"Protocol.md"),Path.Combine(dir,"Protocol.md"));
        File.Copy(report,Path.Combine(dir,"primary-report.md"));
        var seal=new SeriesSeal(Hash(typeof(Program).Assembly.Location),Hash(typeof(TensionMachine).Assembly.Location),
            Hash(Source(dir,"finalists.json")),Hash(Source(dir,"blind.lock")),Hash(report),
            Hash(Source(dir,"design/settings.json")),Hash(Path.Combine(dir,"Protocol.md")));
        Save(Path.Combine(dir,"seal.json"),seal);
    }
    private static void Verify(string dir)
    {
        var seal=Load<SeriesSeal>(Path.Combine(dir,"seal.json"));
        if(seal.Host!=Hash(typeof(Program).Assembly.Location) || seal.Generator!=Hash(typeof(TensionMachine).Assembly.Location)
            || seal.Parent!=Hash(Source(dir,"finalists.json")) || seal.ParentBlind!=Hash(Source(dir,"blind.lock"))
            || seal.ParentReport!=Hash(Path.Combine(dir,"primary-report.md")) || seal.Settings!=Hash(Source(dir,"design/settings.json"))
            || seal.Protocol!=Hash(Path.Combine(dir,"Protocol.md")))throw new InvalidDataException("Sealed evidence changed.");
        var primary=Load<JsonElement>(Source(dir,"seal.json"));
        if(!primary.GetProperty("host").GetString()!.Equals(seal.Generator,StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Replay requires the frozen primary tension runtime.");
    }
    private static Tray[] Enroll(string dir)
    {
        var winners=Load<JsonElement>(Source(dir,"finalists.json")).GetProperty("winners").EnumerateArray()
            .Where(r=>r.GetProperty("arm").GetString()=="coupled").ToDictionary(
                r=>(r.GetProperty("family").GetInt32(),r.GetProperty("replicate").GetInt32()),
                r=>r.GetProperty("recipe").Deserialize<Recipe>(Json)!);
        return (from family in Enumerable.Range(0,EngineLibrary.Names.Length) from rep in Enumerable.Range(0,4)
            select new Tray(family,rep,winners[(family,rep)],winners[(family,(rep+1)%4)])).ToArray();
    }
    private static Setting[] Cases(string dir,string split,int rep=0)
    {
        var element=Load<JsonElement>(Source(dir,"design/settings.json")).GetProperty(split);
        if(split=="reserve")return element.Deserialize<Setting[]>(Json)!;
        var samples=element[rep].Deserialize<Setting[]>(Json)!;
        return split=="meta" ? samples.Where(s=>(s.Parent[^1]-'0'+rep)%2==0).ToArray() : samples;
    }
    private static void Train(string dir,int workers)
    {
        if(workers is <1 or >32)throw new ArgumentOutOfRangeException(nameof(workers));
        var trays=Enroll(dir);
        var champs=new Champion[trays.Length];
        Directory.CreateDirectory(Path.Combine(dir,"training"));
        Parallel.For(0,trays.Length,new ParallelOptions{MaxDegreeOfParallelism=workers},i=>{
            var tray=trays[i];var rng=new Random(6732059+7919*tray.Family+101*tray.Replicate);
            var train=Cases(dir,"meta",tray.Replicate).Select(s=>new SeriesDraw.Bank(tray,s,false)).ToArray();
            var validation=Cases(dir,"validation",tray.Replicate).Select(s=>new SeriesDraw.Bank(tray,s,false)).ToArray();
            var bank=new Champion[Budget];
            for(int j=0;j<Budget;j++)
            {
                byte[] rule=j==0 ? [255,10,10,0] : [(byte)rng.Next(1,256),(byte)rng.Next(16),(byte)rng.Next(16),
                    (byte)(rng.Next(3)|(rng.Next(2)<<2)|(rng.Next(3)<<3))];
                double merit=Rate(train,rule,tray.Replicate&1);
                bank[j]=new(tray,Convert.ToHexString(rule),merit,0,j);
            }
            var shortlist=bank.OrderByDescending(x=>x.Training).ThenBy(x=>x.Trial).DistinctBy(x=>x.Rule).Take(8).ToArray();
            var finalists=shortlist.Select(c=>c with {Validation=Rate(validation,Convert.FromHexString(c.Rule),tray.Replicate&1)})
                .OrderByDescending(x=>x.Validation).ThenByDescending(x=>x.Training).ThenBy(x=>x.Trial).ToArray();
            Save(Path.Combine(dir,"training",$"pool-{i:D2}.json"),new {bank,finalists});
            champs[i]=finalists[0];
        });
        Save(Path.Combine(dir,"finalists.json"),new SeriesLock(Hash(Path.Combine(dir,"seal.json")),champs));
        Save(Path.Combine(dir,"lock.json"),new { finalists=Hash(Path.Combine(dir,"finalists.json")),count=champs.Length,
            paid=champs.Length*Budget, actor="two-slot memory and signed three-unit carrier" });
    }
    private static double Rate(SeriesDraw.Bank[] bank,byte[] rule,int first)=>bank.Average(b=>
        SeriesDraw.Measure(SeriesDraw.Members(b,rule,SeriesMode.Live,first)).Grade);
    private static void Play(Sheet sheet,string output)
    {
        var owner=new SeriesOwner(Convert.FromHexString(sheet.Rule),sheet.Tray.Replicate&1);
        var draws=new List<object>();
        for(int i=0;i<SeriesDraw.Length;i++)
        {
            var move=owner.Next(sheet.Mode);
            var result=SeriesDraw.Execute(sheet.Tray,sheet.Setting,i,move);
            owner.Receive(result.Publication);
            draws.Add(new{move,result.Publication,result.Trace});
        }
        Save(output,draws);
    }
}
