using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Experiments.OrnamentTension;

internal sealed record Basis(string Name,int A,int B,int Pitch);
internal sealed record Setting(string Key,string Parent,int Slots,PathInput Input);
internal sealed record Recipe(string Path,string Policy);
internal sealed record Winner(int Family,string Arm,int Replicate,Recipe Recipe,double Training,double Validation,int Position,int Unique);
internal static class Design
{
    public const int Replicates=4,Generations=20,Population=24,Pilot=96;
    public static readonly int[] Slots=[2,5,9];
    public static readonly string[] Arms=["coupled","quiet","fixed","declared","random-panel","random-draw","policy-only"];
    public static readonly string[] Contrasts=["quiet","fixed","declared","random-panel","random-draw","policy-only","source",
        "disconnect","static","cut-up","cut-down","wrong-route","wrong-reference"];
    public static readonly Dictionary<string,Basis[]> Bases=Enroll();

    // A, B and pitch name a shared physical realization. No normalized basis is
    // reused across these partitions. Membership is fixed before any drawing.
    private static Dictionary<string,Basis[]> Enroll()
    {
        var rng=new Random(26063871);
        var used=new HashSet<(int,int,int)>();
        var result=new Dictionary<string,Basis[]>();
        foreach(var (split,count) in new[]{("train",8),("meta",4),("validation",4),("blind",12),("reserve",12)})
        {
            var group=new List<Basis>();
            while(group.Count<count)
            {
                int a=rng.Next(1,8), b=rng.Next(1,8);
                int pitch=(group.Count%4) switch {0=>a+2*b,1=>2*a,2=>2*(a+b),_=>2*a+b+1};
                int Gcd(int x,int y) {while(y!=0) (x,y)=(y,x%y); return x;}
                int common=Gcd(Gcd(a,b),pitch);
                if(!used.Add((a/common,b/common,pitch/common)))continue;
                group.Add(new($"{split}-{group.Count:D2}",a,b,pitch));
            }
            result.Add(split,group.ToArray());
        }
        return result;
    }
    public static Setting[] Settings(string split,int rep=0)
    {
        var result=new List<Setting>();
        var bases=Bases[split];
        int n=split=="train" ? 2 : bases.Length;
        for(int i=0;i<n;i++)
        for(int j=0;j<Slots.Length;j++)
        for(int v=0;v<(split is "blind" or "reserve" ? 2 : 1);v++)
        {
            var b=bases[split=="train" ? (2*rep+i+j)%bases.Length : i];
            var input=new PathInput(b.A,b.B,b.Pitch,(split is "blind" or "reserve" ? 4 : 3)+(i+v)%3,
                (i+rep+2*v)%4,(i+v+rep)%2);
            result.Add(new($"{b.Name}/{Slots[j]}/{v}/{input.Phase}",b.Name,Slots[j],input));
        }
        return result.ToArray();
    }
    public static JudgePortfolio Declared() => new([7,4,0,0,0,0,2,3]);
    public static FeedbackMode Mode(string arm) => arm switch {
        "quiet" or "disconnect"=>FeedbackMode.Quiet, "fixed" or "static"=>FeedbackMode.Fixed,
        "cut-up"=>FeedbackMode.CutUp, "cut-down"=>FeedbackMode.CutDown, "wrong-route"=>FeedbackMode.Misrouted,_=>FeedbackMode.Live };
}

internal static class Files
{
    public static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase,PropertyNameCaseInsensitive=true};
    public static string Digest(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    public static string DigestObject<T>(T obj)=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(obj,Json)));
    public static T Load<T>(string path)=>JsonSerializer.Deserialize<T>(File.ReadAllText(path),Json)??throw new InvalidDataException(path);
    public static void Save<T>(string path,T obj)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string tmp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try {File.WriteAllText(tmp,JsonSerializer.Serialize(obj,Json));File.Move(tmp,path,true);}
        finally {if(File.Exists(tmp))File.Delete(tmp);}
    }
    public static StreamWriter Gzip(string path)=>new(new GZipStream(File.Create(path),CompressionLevel.Fastest));
    public static string F(double x)=>x.ToString("R",CultureInfo.InvariantCulture);
}
