using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.OrnamentTension;

namespace Gojo.Experiments.OrnamentRouteDiagnosis;

internal sealed record Sample(int Basis,string Fixture,int Useful,bool WrongPrior,int Bays,PathInput Input,string Path,string Policy);
internal sealed record Seal(string Host,string Generator,string PathRuntime,string Protocol,string Samples,string Source);
internal sealed record Case(Sample Sample,FeedbackMode Mode,int FirstRelief,int Relief,int Misses,int Strength,
    int UsefulLate,int FixedLate,int UsefulEarly,int FixedEarly,int UsefulAfter,int FixedAfter,int CreditAfter,int Joined,double Settled,
    int PenDebt,int MachineDebt,int FriezeDebt,int Actuations,string Ink);

internal static class Program
{
    private static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase};
    private static readonly FeedbackMode[] Modes=[FeedbackMode.Live,FeedbackMode.Fixed,FeedbackMode.CutDown,
        FeedbackMode.CutUp,FeedbackMode.Quiet,FeedbackMode.Misrouted];
    private static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static void Save<T>(string file,T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
        string text=JsonSerializer.Serialize(value,Json);
        if(File.Exists(file)){if(File.ReadAllText(file)!=text)throw new IOException("Different existing record."); return;}
        File.WriteAllText(file,text);
    }
    private static T Read<T>(string file)=>JsonSerializer.Deserialize<T>(File.ReadAllText(file),Json)!;
    private static int Main(string[] args)
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
        if(args.Length!=2)throw new ArgumentException("freeze|run|verify RUN");
        string dir=Path.GetFullPath(args[1]);
        if(args[0]=="freeze")
        {
            if(Directory.Exists(dir) && Directory.EnumerateFileSystemEntries(dir).Any())throw new IOException("Existing run");
            Directory.CreateDirectory(dir);
            File.Copy(Path.Combine("experiments","OrnamentRouteDiagnosis","Protocol.md"),Path.Combine(dir,"Protocol.md"));
            Save(Path.Combine(dir,"samples.json"),Enroll().ToArray());
            Save(Path.Combine(dir,"seal.json"),Identity(dir));
            return 0;
        }
        if(Identity(dir)!=Read<Seal>(Path.Combine(dir,"seal.json")))throw new InvalidDataException("Changed host or design.");
        if(args[0]=="run")Run(dir);
        else if(args[0]=="verify")Verify(dir);
        else throw new ArgumentException("Unknown command.");
        return 0;
    }
    private static Seal Identity(string dir)=>new(Hash(typeof(Program).Assembly.Location),Hash(typeof(TensionMachine).Assembly.Location),
        Hash(typeof(PathMachine).Assembly.Location),Hash(Path.Combine(dir,"Protocol.md")),Hash(Path.Combine(dir,"samples.json")),
        Hash(Path.Combine(Directory.GetParent(dir)!.FullName,"wave1","blind.lock")));
    private static IEnumerable<Sample> Enroll()
    {
        for(int i=0;i<12;i++)foreach(string kind in new[]{"early","synchronous","unreachable"})
        foreach(int useful in new[]{0,1})foreach(bool wrong in new[]{false,true})foreach(int bays in new[]{32,56})
        {
            var wait=StrokeRow.Bind(0,pen:false);
            var free=Enumerable.Repeat(wait,kind=="synchronous"?8:7).ToArray();
            free[^1]=StrokeRow.Bind(2);
            if(kind=="unreachable"){free[0]=StrokeRow.Bind(0);free[1]=StrokeRow.Bind(2,1);}
            var bound=Enumerable.Repeat(wait,8).ToArray();
            bound[0]=StrokeRow.Bind(0);
            bound[^1]=StrokeRow.Bind(4,target:1,stop:true);
            PathOwner f=new(8,85,0,free),b=new(9,85,0,bound);
            var path=useful==0 ? PathMachine.Encode(f,b) : PathMachine.Encode(b,f);
            byte[] policy=[136,0,4,0,(byte)(wrong ? 1<<((1-useful)+2) : 0),0,15,0];
            int a=3+i,bb=2+i%4;
            yield return new(i,kind,useful,wrong,bays,new(a,bb,13*a+11*bb+1,bays,i%4,i%2),
                Convert.ToHexString(path),Convert.ToHexString(policy));
        }
    }
    private static TensionResult Draw(Sample s,FeedbackMode mode)=>
        TensionMachine.Run(Convert.FromHexString(s.Path),s.Input,5,Convert.FromHexString(s.Policy),mode,true);
    private static Case Inspect(Sample s,FeedbackMode mode,TensionResult r,int boundary)
    {
        var timeline=r.Timeline;
        int useful=s.Useful,other=1-useful;
        var down=timeline.Where(e=>e.Kind=="delivery" && e.Source==2 && e.Lane==2 && e.Accepted!=0).ToArray();
        int Count(int target,Func<PressureEvent,bool> admits)=>down.Count(e=>e.Target==target && admits(e));
        int early=8*8,late=(s.Bays-8)*8;
        bool after(PressureEvent e)=>boundary>=0 && e.Beat>boundary;
        var route=r.Stats.Routes.Single(x=>x.Source==2 && x.Target==useful && x.Lane==2);
        var first=timeline.FirstOrDefault(e=>e.Kind=="relief" && e.Source==2 && e.Target==useful && e.Lane==2);
        int joined=r.Trace.Edges.Count(e=>e.Owner==useful && e.To==useful*(s.Bays+1)+e.Cell+1);
        string ink=Ink(r.Trace);
        return new(s,mode,first?.Beat??-1,route.Relief,route.Misses,route.Strength,
            Count(useful,e=>e.Beat>=late),Count(other,e=>e.Beat>=late),Count(useful,e=>e.Beat<early),Count(other,e=>e.Beat<early),
            Count(useful,after),Count(other,after),boundary,joined,(double)joined/s.Bays,r.Stats.PenDebt,r.Stats.MachineDebt,
            r.Stats.FriezeDebt,r.Stats.Actuations,ink);
    }
    private static string Ink(PathTrace t)
    {
        var atoms=new HashSet<(int X,int Y,int Dx,int Dy)>();
        foreach(var e in t.Edges)
        {
            var a=t.Sites[e.From];var b=t.Sites[e.To];
            if(a.X>b.X || a.X==b.X && a.Y>b.Y)(a,b)=(b,a);
            int dx=Math.Sign(b.X-a.X),dy=Math.Sign(b.Y-a.Y),steps=Math.Max(Math.Abs(b.X-a.X),Math.Abs(b.Y-a.Y));
            for(int j=0;j<steps;j++)atoms.Add((a.X+j*dx,a.Y+j*dy,dx,dy));
        }
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(atoms.Order().Select(a=>new[]{a.X,a.Y,a.Dx,a.Dy}))));
    }
    private static void Run(string dir)
    {
        var samples=Read<Sample[]>(Path.Combine(dir,"samples.json"));
        var cases=new Case[samples.Length][];
        Parallel.For(0,samples.Length,new ParallelOptions {MaxDegreeOfParallelism=6},i=>{
            var s=samples[i]; var live=Draw(s,FeedbackMode.Live);
            int beat=live.Timeline.FirstOrDefault(e=>e.Kind=="relief" && e.Source==2 && e.Target==s.Useful && e.Lane==2)?.Beat??-1;
            cases[i]=Modes.Select(mode=>{
                var r=mode==FeedbackMode.Live?live:Draw(s,mode);
                if(r.Stats.Offered!=r.Stats.Accepted+r.Stats.Blocked+r.Stats.Remainder)throw new InvalidDataException("Accounting");
                if(s.Basis==0 && s.Useful==0 && s.Bays==32 && !s.WrongPrior && mode is FeedbackMode.Live or FeedbackMode.Fixed)
                    Save(Path.Combine(dir,"examples",$"{s.Fixture}-{mode}.json"),new {s,mode,result=r});
                return Inspect(s,mode,r,beat);
            }).ToArray();
        });
        var all=cases.SelectMany(x=>x).ToArray();
        Save(Path.Combine(dir,"cases.json"),all);
        Decide(dir,all);
        Save(Path.Combine(dir,"complete.json"),new {seal=Hash(Path.Combine(dir,"seal.json")),samples=samples.Length,
            draws=all.Length,cases=Hash(Path.Combine(dir,"cases.json")),verdicts=Hash(Path.Combine(dir,"verdicts.json"))});
    }
    private static double Share(int n,int d)=>d==0 ? 0 : (double)n/d;
    private static void Decide(string dir,Case[] all)
    {
        var findings=new List<object>();
        var by=all.ToDictionary(x=>(x.Sample,x.Mode));
        double Late(Case c)=>Share(c.UsefulLate,c.UsefulLate+c.FixedLate);
        void Decide(string name,string fixture,bool biased,double margin,bool equal,Func<Case,double> evaluate,
            Func<Case,Case> opponent,bool requireReceipt=false,int? position=null)
        {
            var rows=all.Where(c=>c.Mode==FeedbackMode.Live && c.Sample.Fixture==fixture &&
                c.Sample.WrongPrior==biased && (position==null || position==c.Sample.Useful)).ToArray();
            var groups=rows.GroupBy(c=>c.Sample.Basis).Select(g=>(Basis:g.Key,Value:g.Average(c=>evaluate(c)-evaluate(opponent(c))),
                Confirmed:g.Any(c=>c.FirstRelief>=0 && c.UsefulAfter>0))).ToArray();
            int up=groups.Count(g=>g.Value>=margin && (!requireReceipt || g.Confirmed)),dn=groups.Count(g=>g.Value<=-margin),
                neutral=groups.Count(g=>Math.Abs(g.Value)<margin);
            string status=equal ? (neutral>=8 ? "supported" : Math.Max(up,dn)>=8 ? "contrary":"inconclusive") :
                (up>=8 ? "supported" : dn>=8 ? "contrary" : "inconclusive");
            findings.Add(new {name,margin,prediction=equal?"equivalent":"higher",up,down=dn,within=neutral,status,
                receiptGroups=groups.Count(g=>g.Confirmed),mean=groups.Average(g=>g.Value),parents=groups.Select(g=>g.Value)});
        }
        Case Fixed(Case c)=>by[(c.Sample,FeedbackMode.Fixed)];
        Decide("R1-late-traffic","early",false,.08,false,Late,Fixed,true);
        foreach(int p in new[]{0,1})Decide("R1-position-"+p,"early",false,.08,false,Late,Fixed,true,p);
        Decide("R2-seam","early",false,.08,false,c=>c.Settled,Fixed);
        Decide("R3-unreachable","unreachable",false,.02,true,Late,Fixed);
        Decide("R4-biased","early",true,.02,true,Late,Fixed);
        var controls=all.Where(c=>c.Sample.Fixture=="early" && c.Mode is FeedbackMode.CutDown or FeedbackMode.Misrouted).ToArray();
        findings.Add(new{name="R5-disabled",status=controls.All(c=>c.Joined==0 && c.Relief==0)?"supported":"contrary",cases=controls.Length});
        Decide("R6-delay","early",false,.08,false,Late,c=>by[(c.Sample with {Fixture="synchronous",
            Path=all.First(d=>d.Sample.Fixture=="synchronous" && d.Sample.Useful==c.Sample.Useful).Sample.Path},FeedbackMode.Live)]);
        Save(Path.Combine(dir,"verdicts.json"),findings);
    }
    private static void Verify(string dir)
    {
        var rows=Read<Case[]>(Path.Combine(dir,"cases.json"));
        foreach(var c in rows)
        {
            var actual=Inspect(c.Sample,c.Mode,Draw(c.Sample,c.Mode),c.CreditAfter);
            if(actual!=c)throw new InvalidDataException("Recorded material or receipt statistics differ.");
        }
        Console.WriteLine($"Replayed {rows.Length} drawings.");
    }
}
