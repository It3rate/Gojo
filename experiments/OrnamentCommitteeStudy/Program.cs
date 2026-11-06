using System.IO.Compression;
using System.Text.Json;
using Gojo.Experiments.OrnamentTension;

var commands=args;
if(commands.Length is <3 or >4)throw new ArgumentException("train|assay PRIMARY RUN [RELEASE]");
var source=Path.GetFullPath(commands[1]);
var dir=Path.GetFullPath(commands[2]);
var old=Files.Load<JsonElement>(Path.Combine(source,"finalists.json"))
    .GetProperty("winners").Deserialize<Winner[]>(Files.Json)!;
var pilots=Files.Load<JsonElement>(Path.Combine(source,"finalists.json"))
    .GetProperty("panels").Deserialize<PilotResult[]>(Files.Json)!;
string spec=Path.Combine("experiments","OrnamentCommitteeStudy","Protocol.md");
var seal=new {protocol=Files.Digest(spec),primary=Files.Digest(Path.Combine(source,"finalists.json")),
    broad=Files.Digest(Path.Combine(source,"blind.lock")),runtime=Files.Digest(typeof(TensionMachine).Assembly.Location),
    host=Files.Digest(typeof(Program).Assembly.Location)};
if(seal.runtime!=Files.Load<JsonElement>(Path.Combine(source,"seal.json")).GetProperty("host").GetString())
    throw new InvalidDataException("Use registered kernel.");
if(commands[0]=="train")
{
    if(Directory.Exists(dir)&&Directory.EnumerateFileSystemEntries(dir).Any())throw new IOException("Fresh run required.");
    Directory.CreateDirectory(dir);File.Copy(spec,Path.Combine(dir,"Protocol.md"));Files.Save(Path.Combine(dir,"seal.json"),seal);
    var winners=new Winner[pilots.Length];var restricted=new PilotResult[pilots.Length];
    Parallel.For(0,pilots.Length,new ParallelOptions{MaxDegreeOfParallelism=8},i=>{
        var p=pilots[i];
        using var csv=new StreamReader(new GZipStream(File.OpenRead(Path.Combine(source,"panels",$"{p.Family}-{p.Replicate}.csv.gz")),CompressionMode.Decompress));
        csv.ReadLine();
        var eligible=new Dictionary<int,(string Key,double Score)>();
        string? line;
        while((line=csv.ReadLine())!=null)
        {
            var fields=line.Split(',');var weights=Convert.FromHexString(fields[1]);
            if(weights[1]==0||(p.Family==1 ? weights[2]<4 : weights[0]<6))continue;
            int slots=int.Parse(fields[0]);double score=double.Parse(fields[2],System.Globalization.CultureInfo.InvariantCulture);
            if(!eligible.TryGetValue(slots,out var incumbent)||score>incumbent.Score||
                score==incumbent.Score&&string.CompareOrdinal(fields[1],incumbent.Key)<0)
                eligible[slots]=(fields[1],score);
        }
        var panel=new PilotResult(p.Family,p.Replicate,p.Panels.Select(v=>eligible.TryGetValue(v.Slots,out var selected)
            ? v with{Selected=selected.Key,Validation=selected.Score} : v with{Selected=Convert.ToHexString(Design.Declared().Shares),Validation=v.DeclaredValue}).ToArray());
        restricted[i]=panel;
        var island=Search.Train(dir,p.Family,p.Replicate,"coupled",panel);
        winners[i]=island.Winner;Files.Save(Path.Combine(dir,"islands",i+".json"),island);
    });
    Files.Save(Path.Combine(dir,"panels.json"),restricted);Files.Save(Path.Combine(dir,"finalists.json"),winners);
    Files.Save(Path.Combine(dir,"lock.json"),new {sha256=Files.Digest(Path.Combine(dir,"finalists.json")),utc=DateTime.UtcNow.ToString("O")});
}
else if(commands[0]=="assay")
{
    var winnerFile=Path.Combine(dir,"finalists.json");
    var digest=Files.Digest(winnerFile);
    if(commands.Length!=4||!File.ReadAllText(commands[3]).Contains(digest,StringComparison.Ordinal)||
        Files.Load<JsonElement>(Path.Combine(dir,"lock.json")).GetProperty("sha256").GetString()!=digest ||
        Files.DigestObject(Files.Load<JsonElement>(Path.Combine(dir,"seal.json")))!=Files.DigestObject(seal))
        throw new InvalidDataException("Check the common gate and host.");
    var trials=Files.Load<Winner[]>(winnerFile);
    var rows=new List<(int F,int R,string A,string Parent,int Slots,string Key,double Broad,double Constituent,double Reference)>();
    foreach(var w in trials)foreach(string arm in new[]{"restricted","coupled","declared"})
    {
        var recipe=arm=="restricted"? w.Recipe:old.Single(x=>x.Family==w.Family&&x.Replicate==w.Replicate&&x.Arm==arm).Recipe;
        foreach(var s in Design.Settings("reserve"))
        {
            var read=Search.Draw(recipe,s,w.Family,FeedbackMode.Live,true).Read;
            rows.Add((w.Family,w.Replicate,arm,s.Parent,s.Slots,s.Key,read.Broad,read.Values[w.Family==1 ? 2 : 0],read.Reference));
        }
    }
    using(var csv=File.CreateText(Path.Combine(dir,"cases.csv")))
    {
        csv.WriteLine("family,rep,arm,parent,slots,scenario,broad,constituent,reference");
        foreach(var r in rows)csv.WriteLine($"{EngineLibrary.Names[r.F]},{r.R},{r.A},{r.Parent},{r.Slots},{r.Key},{Files.F(r.Broad)},{Files.F(r.Constituent)},{Files.F(r.Reference)}");
    }
    using(var csv=File.CreateText(Path.Combine(dir,"verdicts.csv")))
    {
        csv.WriteLine("family,comparison,mean_difference,positive,negative,within,status");
        foreach(int f in Enumerable.Range(0,EngineLibrary.Names.Length))
        foreach(var (control,read,margin,equiv) in new[]{("coupled","broad",.012,false),("coupled","constituent",.04,false),("declared","broad",.025,true),("coupled","reference",.04,true)})
        {
            var deltas=rows.Where(r=>r.F==f).GroupBy(r=>r.Parent).Select(g=>{
                double Read(string arm)=>g.Where(r=>r.A==arm).Average(r=>read=="broad"?r.Broad:read=="reference"?r.Reference:r.Constituent);
                return Read("restricted")-Read(control);
            }).ToArray();
            int up=deltas.Count(x=>x>=margin),down=deltas.Count(x=>x<=-margin),within=deltas.Length-up-down;
            string status=equiv?(within>=8?"supported":Math.Max(up,down)>=8?"contrary":"inconclusive"):
                up>=8?"supported":down>=8?"contrary":"inconclusive";
            csv.WriteLine($"{EngineLibrary.Names[f]},{control}-{read},{Files.F(deltas.Average())},{up},{down},{within},{status}");
        }
    }
    Files.Save(Path.Combine(dir,"complete.json"),new{lockHash=digest,cases=rows.Count,
        caseHash=Files.Digest(Path.Combine(dir,"cases.csv")),verdictHash=Files.Digest(Path.Combine(dir,"verdicts.csv"))});
}
else throw new ArgumentException("Unknown phase");
