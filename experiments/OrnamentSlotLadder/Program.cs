using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.OrnamentTension;

namespace Gojo.Experiments.OrnamentSlotLadder;

internal sealed record Basis(int A,int B,int Pitch);
internal sealed record Case(int Engine,string Name,int Basis,int Slots,string Mode,string Hex,PathInput Input,
    int Owners,int Edges,int OptionalEdges,int ThirdEdges,int SpineHandoffs,int Stops,int Overlap,int Crossings,
    int Forks,int Merges,int PhysicalBlocks,int GateBlocks,int VerticalRange,double Mirror,double NeighborMirror,
    double Translation,string Ink);
internal sealed record Exhibit(Case Case,Draw Draw);
internal static class Program
{
    internal static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase};
    private const string Project="experiments/OrnamentSlotLadder";
    private static string Hash(string p)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p)));
    private static string Digest<T>(T data)=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(data,Json)));
    private static void Save<T>(string p,T value)
    {
        if(File.Exists(p))throw new IOException("Output exists: "+p);
        File.WriteAllBytes(p,JsonSerializer.SerializeToUtf8Bytes(value,Json));
    }
    private static T Read<T>(string path)=>JsonSerializer.Deserialize<T>(File.ReadAllText(path),Json)!;
    private static Dictionary<string,string> Identity()=>new(){
        ["host"]=Hash(typeof(Program).Assembly.Location),["path"]=Hash(typeof(PathMachine).Assembly.Location),
        ["tension"]=Hash(typeof(TensionMachine).Assembly.Location),["relations"]=Hash(typeof(Gojo.Experiments.PrimitiveMeasure.BitRelation).Assembly.Location),
        ["protocol"]=Hash(Path.Combine(Project,"Protocol.md"))};
    private static void Main(string[] args)
    {
        if(args.Length!=2 || args[0] is not ("freeze" or "run"))throw new ArgumentException("freeze|run DIRECTORY");
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
        string root=Path.GetFullPath(args[1]),seal=Path.Combine(root,"seal.json");
        if(args[0]=="freeze")
        {
            if(Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any())throw new IOException("Use fresh run");
            Directory.CreateDirectory(root);File.Copy(Path.Combine(Project,"Protocol.md"),Path.Combine(root,"Protocol.md"));
            var basis=new[]{(7,3),(9,4),(8,3),(11,4),(9,5),(13,5)}.Select(b=>new Basis(b.Item1,b.Item2,b.Item1+2*b.Item2)).ToArray();
            string settings="artifacts/local-tension-ornament/wave1/design/settings.json";
            using var previous=JsonDocument.Parse(File.ReadAllBytes(settings));
            var triples=new HashSet<(int,int,int)>();
            void Collect(JsonElement json)
            {
                if(json.ValueKind==JsonValueKind.Array){foreach(var v in json.EnumerateArray())Collect(v);return;}
                if(json.ValueKind!=JsonValueKind.Object)return;
                if(json.TryGetProperty("a",out var a) && json.TryGetProperty("b",out var b) && json.TryGetProperty("pitch",out var p))
                    triples.Add(Key(a.GetInt32(),b.GetInt32(),p.GetInt32()));
                foreach(var prop in json.EnumerateObject())Collect(prop.Value);
            }
            Collect(previous.RootElement);
            if(triples.Count<40)throw new InvalidDataException("Missing prior cohort");
            foreach(var b in basis)if(!triples.Add(Key(b.A,b.B,b.Pitch)))throw new InvalidDataException("Reused basis "+b);
            string design=Path.Combine(root,"design.json");
            Save(design,new{basis,prior=Hash(settings),genomes=Enumerable.Range(0,3).Select(i=>Convert.ToHexString(SlotBodies.Program(i)))});
            var id=Identity();id["design"]=Hash(design);Save(seal,id);Console.WriteLine("Registered "+Hash(seal));return;
        }
        var known=Read<Dictionary<string,string>>(seal);
        string file=Path.Combine(root,"design.json");
        if(Identity().Any(x=>known[x.Key]!=x.Value) || known["design"]!=Hash(file) ||
            Hash(Path.Combine(root,"Protocol.md"))!=known["protocol"])throw new InvalidDataException("Study differs");
        if(File.Exists(Path.Combine(root,"complete.json")))throw new IOException("Already completed");
        var spec=Read<JsonElement>(file);
        var bases=spec.GetProperty("basis").Deserialize<Basis[]>(Json)!;
        var genomes=spec.GetProperty("genomes").Deserialize<string[]>(Json)!;
        var records=new List<Case>();var exhibits=new List<Exhibit>();
        foreach(int engine in Enumerable.Range(0,3))
        foreach(int i in Enumerable.Range(0,bases.Length))
        foreach(int slots in new[]{2,5,9})
        foreach(Mode mode in Enum.GetValues<Mode>())
        {
            var b=bases[i];var inp=new PathInput(b.A,b.B,b.Pitch,6);
            var draw=SlotBodies.Run(Convert.FromHexString(genomes[engine]),inp,slots,mode);
            var result=Measure(engine,i,slots,mode,genomes[engine],inp,draw);
            records.Add(result);
            if(i==0)exhibits.Add(new(result,draw));
        }
        Save(Path.Combine(root,"cases.json"),records);
        Save(Path.Combine(root,"exhibits.json"),exhibits);
        var verdicts=Verdicts(records);
        Save(Path.Combine(root,"verdicts.json"),verdicts);
        Save(Path.Combine(root,"complete.json"),new{seal=Hash(seal),cases=records.Count,
            files=new[]{"cases.json","exhibits.json","verdicts.json"}.ToDictionary(n=>n,n=>Hash(Path.Combine(root,n)))});
    }

    private static (int,int,int) Key(int a,int b,int p)
    {
        static int Gcd(int x,int y){while(y>0)(x,y)=(y,x%y);return x;}
        int g=Gcd(a,Gcd(b,p));return(a/g,b/g,p/g);
    }
    private readonly record struct Atom(int X,int Y,int Axis);
    private static double Similarity(HashSet<Atom> a,HashSet<Atom> b)
    {
        double Weight(Atom p)=>(p.Axis&1)==1 ? Math.Sqrt(2):1;
        double all=a.Union(b).Sum(Weight);
        return all==0 ? 0 : a.Intersect(b).Sum(Weight)/all;
    }
    private static HashSet<Atom> Reflect(HashSet<Atom> set)=>set.Select(p=>new Atom(p.X,-p.Y,(4-p.Axis)%4)).ToHashSet();
    private static Case Measure(int engine,int i,int slots,Mode mode,string bytes,PathInput input,Draw draw)
    {
        var tr=draw.Trace;var joints=JointCensus.Read(tr);
        var cells=Enumerable.Range(0,tr.Cells).Select(_=>new HashSet<Atom>()).ToArray();
        int min=0,max=0;
        foreach(var e in tr.Edges)
        {
            var a=tr.Sites[e.From];var b=tr.Sites[e.To];
            min=Math.Min(min,Math.Min(a.Y,b.Y));max=Math.Max(max,Math.Max(a.Y,b.Y));
            int dx=Math.Sign(b.X-a.X),dy=Math.Sign(b.Y-a.Y),length=Math.Max(Math.Abs(b.X-a.X),Math.Abs(b.Y-a.Y));
            for(int j=0;j<length;j++)cells[e.Cell].Add(new(2*(a.X-e.Cell*tr.Pitch)+(2*j+1)*dx,
                2*a.Y+(2*j+1)*dy,e.Direction&3));
        }
        double motif=cells.Average(c=>Similarity(c,Reflect(c)));
        double alternation=Enumerable.Range(1,tr.Cells-1).Average(c=>Similarity(cells[c],Reflect(cells[c-1])));
        double translation=Enumerable.Range(1,tr.Cells-1).Average(c=>Similarity(cells[c],cells[c-1]));
        return new(engine,SlotBodies.Names[engine],i,slots,mode.ToString(),bytes,input,tr.Edges.Select(e=>e.Owner).Distinct().Count(),
            tr.Edges.Length,tr.Edges.Count(e=>e.Owner!=0),tr.Edges.Count(e=>e.Owner==2),
            tr.Events.Count(e=>e.Kind=="continue" && e.Owner==0),joints.IntentionalStops,joints.CollinearOverlaps,joints.InteriorCrossings,
            joints.BranchSites,joints.MergeSites,draw.PhysicalBlocks.Sum(),draw.GateBlocks.Sum(),max-min,motif,alternation,translation,
            Digest(cells.Select(c=>c.OrderBy(a=>a.X).ThenBy(a=>a.Y).ThenBy(a=>a.Axis))));
    }
    private static List<object> Verdicts(List<Case> rows)
    {
        Case Read(int e,int b,int w,Mode m=Mode.Ladder)=>rows.Single(r=>r.Engine==e && r.Basis==b && r.Slots==w && r.Mode==m.ToString());
        int Count(Func<int,bool> f)=>Enumerable.Range(0,6).Count(f);
        var results=new List<object>();
        void Add(string id,int e,int passed,int n,int threshold)=>results.Add(new{id,engine=e,passed,total=n,required=threshold,
            status=passed>=threshold ? "supported" : passed==0 ? "contrary" : "inconclusive"});
        for(int e=0;e<3;e++)
        {
            Add("W1",e,Count(b=>Read(e,b,2).Owners==1 && Read(e,b,5).Owners==2 && Read(e,b,9).Owners==3),6,6);
            Add("W2",e,Count(b=>new[]{2,5,9}.All(w=>Read(e,b,w).SpineHandoffs==5)),6,6);
            Add("W3",e,Count(b=>Read(e,b,5,Mode.CapacityOnly).ThirdEdges>Read(e,b,5).ThirdEdges),6,4);
        }
        Add("W4",1,Count(b=>Read(1,b,9).Stops==12 && Read(1,b,9).Mirror-Read(1,b,5).Mirror>=.15),6,4);
        Add("W5",2,Count(b=>Read(2,b,5).NeighborMirror-Read(2,b,5).Translation>=.15 &&
            Math.Abs(Read(2,b,9).NeighborMirror-Read(2,b,9).Translation)<=.02),6,4);
        Add("W6",-1,rows.Count(r=>r.Mode=="Ladder" && r.Slots==9 && r.Overlap==0),18,12);
        return results;
    }
}
