using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Gojo.Experiments.OrnamentFollowup;
using Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;

namespace Gojo.Tests.OrnamentFollowup;

public sealed class FollowupTests
{
    [Fact]
    public void TicketsKeepFailuresInTheirDenominatorAndPlaceboOnlyPermutesEndpoints()
    {
        var memory = new ProposalMemory();
        memory.Observe(0,10,11);
        memory.Observe(0,10,10);
        memory.Observe(0,10,9);
        var evidence = memory.Evidence();
        Assert.Equal(new MutationEvidence("flip-sensor-law",3,1,6),evidence[0]);
        var learned = ProposalMemory.Pack(evidence,"learned").Decode();
        var permuted = ProposalMemory.Pack(evidence,"permuted").Decode();
        Assert.Equal(6,learned.Count(x=>x==0));
        Assert.Equal(6,permuted.Count(x=>x==8));
        Assert.Equal(learned.Length,permuted.Length);
        Assert.Equal(Enumerable.Range(0,9).Select(i=>(byte)i),ProposalMemory.Pack(evidence,"uniform").Decode());
    }

    [Fact]
    public void SyntheticTrainingBindsSourceEvidenceAndCannotReadReserveUntilLock()
    {
        using var f = new ArtificialCampaign();
        var queried = new System.Collections.Concurrent.ConcurrentBag<string>();
        CampaignEpisode[] Supply(string split,string goal)
        {
            queried.Add(split);
            return Enumerable.Range(0,12).Select(i=>
            {
                ulong ink = 1UL | 1UL << (1 + i%7);
                return new CampaignEpisode(goal,"artificial",split+"-"+i,0,new(9,ink,ink,1,2,1,2,5,12));
            }).ToArray();
        }
        Followup.Freeze(f.Spec,f.Primary,f.Report,f.Run);
        Assert.Throws<FileNotFoundException>(()=>Followup.Execute("assay",f.Run,1,Supply));
        Assert.Empty(queried);
        Followup.Execute("train",f.Run,2,Supply);
        var sources = File.ReadAllLines(Path.Combine(f.Run,"mutation-evidence.csv"));
        Assert.Contains(sources,x=>x=="\"balance\",\"flip-sensor-law\",\"8\",\"4\",\"8\"");
        Assert.DoesNotContain("reserve",queried);
        Assert.Throws<FileNotFoundException>(()=>Followup.Execute("assay",f.Run,1,Supply));
        var firstHash = SHA256.HashData(File.ReadAllBytes(Path.Combine(f.Run,"islands","balance","learned","00","winner.json")));
        Followup.Execute("train",f.Run,1,Supply);
        Assert.Equal(firstHash,SHA256.HashData(File.ReadAllBytes(Path.Combine(f.Run,"islands","balance","learned","00","winner.json"))));
        Followup.Execute("lock",f.Run,1,Supply);
        Assert.DoesNotContain("reserve",queried);
        Followup.Execute("assay",f.Run,1,Supply);
        Assert.Contains("reserve",queried);
        Assert.Equal(37,File.ReadAllLines(Path.Combine(f.Run,"reserve","cases.csv")).Length);
        Assert.Equal(25,File.ReadAllLines(Path.Combine(f.Run,"reserve","groups.csv")).Length);
        Assert.Equal(3,File.ReadAllLines(Path.Combine(f.Run,"reserve","contrasts.csv")).Length);
        Followup.Execute("assay",f.Run,1,(_,_)=>throw new Exception("Completed assay was rerun."));
        File.AppendAllText(Path.Combine(f.Run,"reserve","groups.csv"),"changed");
        Assert.Throws<InvalidDataException>(()=>Followup.Execute("assay",f.Run,1,Supply));
    }

    [Fact]
    public void ChangingPrimaryHistoryOrUsingAnAlreadyOpenedReserveRejectsTransfer()
    {
        using var f = new ArtificialCampaign();
        var reserve = Path.Combine(f.Primary,"reserve");
        Directory.CreateDirectory(reserve);
        File.WriteAllText(Path.Combine(reserve,"seen"),"synthetic");
        Assert.Throws<InvalidDataException>(()=>Followup.Freeze(f.Spec,f.Primary,f.Report,f.Run));
        File.Delete(Path.Combine(reserve,"seen"));
        Followup.Freeze(f.Spec,f.Primary,f.Report,f.Run);
        var ledger = Path.Combine(f.Primary,"islands","balance","evolve","00","g0001.json.gz");
        using (var file = File.Create(ledger))
        using (var zip = new GZipStream(file,CompressionLevel.Fastest))
            JsonSerializer.Serialize(zip,new { Hash = "invalid", Content = new {Index=1,PreviousHash="",Entries=Array.Empty<TrainingEntry>()} });
        Assert.Throws<InvalidDataException>(()=>Followup.Execute("train",f.Run,1,(_,_)=>throw new Exception("Invalid source was used.")));
    }

    [Fact]
    public void ACorrectlyHashedGenerationWithAnUnboundGenesisIsRejectedAtTheFirstLink()
    {
        using var f = new ArtificialCampaign();
        Followup.VerifySource(f.Spec,f.Primary);
        string path = Path.Combine(f.Primary,"islands","balance","evolve","00","g0000.json.gz");
        JsonNode ledger;
        using (var file = File.OpenRead(path))
        using (var zip = new GZipStream(file,CompressionMode.Decompress))
            ledger=JsonNode.Parse(zip)!;
        var content=ledger["Content"]!;
        content["PreviousHash"]="";
        ledger["Hash"]=Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(content)));
        using (var file = File.Create(path))
        using (var zip = new GZipStream(file,CompressionLevel.Fastest))
            JsonSerializer.Serialize(zip,ledger);
        var mismatch=Assert.Throws<InvalidDataException>(()=>Followup.VerifySource(f.Spec,f.Primary));
        Assert.Contains("g0000.json.gz",mismatch.Message);
    }

    // All artifacts and every episode are synthesized here. The study's real
    // protocol, source ledgers and reserve cases are never opened in these tests.
    private sealed class ArtificialCampaign : IDisposable
    {
        private readonly string temp = Path.Combine(Path.GetTempPath(),"gojo-followup-tests-"+Guid.NewGuid().ToString("N"));
        public string Primary => Path.Combine(temp,"primary");
        public string Report => Path.Combine(temp,"report.md");
        public string Spec => Path.Combine(temp,"spec.json");
        public string Run => Path.Combine(temp,"study");
        private static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
        private static string Digest<T>(T item)=>Hash(JsonSerializer.SerializeToUtf8Bytes(item));
        private static void Put<T>(string path,T item)=>File.WriteAllBytes(path,JsonSerializer.SerializeToUtf8Bytes(item));

        public ArtificialCampaign()
        {
            Directory.CreateDirectory(Path.Combine(Primary,"manifest"));
            var cfg = new TournamentConfig(13,2,8,1,6,2);
            var runtime = Hash(File.ReadAllBytes(typeof(CampaignCli).Assembly.Location));
            var protocol = Hash(Encoding.UTF8.GetBytes(runtime+"\n"+JsonSerializer.Serialize(cfg)+"\n"));
            Put(Path.Combine(Primary,"protocol.lock.json"),new{Version="ornament-1",RuntimeHash=runtime,Config=cfg,
                Manifest=new Dictionary<string,string>(),ProtocolHash=protocol});
            var parameters = new{Format="ornament-league-1",ProtocolHash=protocol,Config=cfg};
            Put(Path.Combine(Primary,"parameters.json"),parameters);
            var genome = Convert.ToHexString(new OrnamentProgram([new(1,1,10)],[new(2,2,10,0,1)]).Encode());
            string ledgerDir = Path.Combine(Primary,"islands","balance","evolve","00");
            Directory.CreateDirectory(ledgerDir);
            string previous = Digest(new{protocolHash=protocol,config=cfg,goal="balance",method="evolve",
                replicate=0,seed=42,train="t",validation="v"});
            for(int index=0;index<2;index++)
            {
                var entries=Enumerable.Range(0,8).Select(i=>new TrainingEntry(index*8+i,genome,
                    new(index==0 ? 10 : i%2==0 ? 11 : 10,0,0,6),"x",index==0?null:i,null,
                    index==0?"sample-wiring":"flip-sensor-law")).ToArray();
                var content=new{Index=index,PreviousHash=previous,Entries=entries};
                previous=Digest(content);
                using var file=File.Create(Path.Combine(ledgerDir,$"g{index:D4}.json.gz"));
                using var zip=new GZipStream(file,CompressionLevel.Fastest);
                JsonSerializer.Serialize(zip,new{Hash=previous,Content=content});
            }
            var all=(from goal in CampaignTournament.Objectives from arm in CampaignTournament.Methods
                select new FrozenWinner(goal,arm,42,0,0,genome,new(1,0,0,6),new(1,0,0,6),null,null,"synthetic","t","v",previous)).ToArray();
            var frozen=new{ConfigHash=Digest(parameters),ProtocolHash=protocol,Winners=all};
            string frozenPath = Path.Combine(Primary,"frozen.json"), locked=Digest(frozen);
            Put(frozenPath,new{LockHash=locked,Content=frozen});
            string test=Path.Combine(Primary,"test"); Directory.CreateDirectory(test);
            var sums=new Dictionary<string,string>();
            foreach(var n in new[]{"cases.csv","champions.csv","hypotheses.csv","groups.csv"})
            {
                File.WriteAllText(Path.Combine(test,n),"synthetic");
                sums[n]=Hash(File.ReadAllBytes(Path.Combine(test,n)));
            }
            Put(Path.Combine(test,"complete.json"),new{LockHash=locked,Split="test",FileHashes=sums});
            File.WriteAllText(Report,"Synthetic first report for an isolated unit test.");
            Put(Spec,new FollowupSpec("synthetic","Exercise phase isolation.",["balance"],protocol,Hash(File.ReadAllBytes(frozenPath)),
                Hash(File.ReadAllBytes(Report)),283,1,2,8,6,2,1));
        }
        public void Dispose() => Directory.Delete(temp,true);
    }
}
