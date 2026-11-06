using System.IO.Compression;
using System.Text.Json;
using Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class OrnamentCampaignTournamentTests
{
    // These deliberately small calibration rings do not read any primary
    // recipe, validation family, blind row or reserve row.
    private static CampaignEpisode[] Fixtures(string split, string goal)
    {
        string[] goals = goal == "mixed" ? CampaignScience.Goals : [goal];
        return (from g in goals
                from i in Enumerable.Range(0,4)
                select new CampaignEpisode(g,"synthetic",split+"-fixture-"+i,i,
                    new RailInput(7, 1UL | (1UL << (2+i)),1UL | (1UL << (2+i)),1UL,2,
                        (byte)(1 << Array.IndexOf(CampaignScience.Goals,g)),2,2,7))).ToArray();
    }

    [Fact]
    public void ResumingAnInterruptedOffCampaignLeagueAndChangingWorkersPreserveEveryFrozenArtifact()
    {
        string root = Path.Combine(Path.GetTempPath(),"gojo-league-test-"+Guid.NewGuid().ToString("N"));
        string serial = Path.Combine(root,"serial"), parallel = Path.Combine(root,"parallel");
        var config = new TournamentConfig(173,2,16,1,2,3);
        try
        {
            CampaignTournament.Train(serial,config,"fixture",1,readBank:Fixtures);
            // Simulate a completed generation from one worker and an interrupted
            // final write from another. Existing earlier generations must survive.
            string interrupted = Path.Combine(serial,"islands","balance","evolve","00");
            var finalPath = Path.Combine(interrupted,"g0001.json.gz");
            var expected = File.ReadAllBytes(finalPath);
            File.Delete(finalPath);
            File.WriteAllText(finalPath+".writing","uncommitted");
            CampaignTournament.Train(serial,config,"fixture",4,readBank:Fixtures);
            Assert.Equal(expected,File.ReadAllBytes(finalPath));
            var first = CampaignTournament.LockWinners(serial,config,"fixture");
            CampaignTournament.Train(parallel,config,"fixture",4,readBank:Fixtures);
            var second = CampaignTournament.LockWinners(parallel,config,"fixture");
            Assert.Equal(20,first.Length);
            Assert.Equal(first,second);
            Assert.Equal(first,CampaignTournament.ReadFrozen(serial,config,"fixture"));
            Assert.Throws<InvalidOperationException>(() => CampaignTournament.Train(serial,config with { Seed=174 },"fixture",2,readBank:Fixtures));
            Assert.Throws<InvalidOperationException>(() => CampaignTournament.ReadFrozen(serial,config,"different-fixture"));
            using var input = File.OpenRead(finalPath);
            using var gzip = new GZipStream(input,CompressionMode.Decompress);
            using var journal = JsonDocument.Parse(gzip);
            var entries = journal.RootElement.GetProperty("Content").GetProperty("Entries");
            Assert.Equal(16,entries.GetArrayLength());
            Assert.Contains(entries.EnumerateArray(), row => row.GetProperty("ParentB").ValueKind == JsonValueKind.Number);
            foreach (var row in entries.EnumerateArray())
            {
                var hex = row.GetProperty("GenomeHex").GetString()!;
                Assert.Equal(hex,Convert.ToHexString(OrnamentProgram.Decode(Convert.FromHexString(hex)).Encode()));
            }
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root,recursive:true); }
    }

    [Fact]
    public void AForeignOrCorruptGenerationCannotResumeOrBecomeABlindWinner()
    {
        string folder = Path.Combine(Path.GetTempPath(),"gojo-league-check-"+Guid.NewGuid().ToString("N"));
        var config = new TournamentConfig(231,1,4,1,2,1);
        try
        {
            CampaignTournament.Train(folder,config,"fixture",1,readBank:Fixtures);
            string source = Path.Combine(folder,"islands","balance","parameter","00","g0000.json.gz");
            string target = Path.Combine(folder,"islands","balance","random","00","g0000.json.gz");
            File.Copy(source,target,true);
            Assert.Throws<InvalidDataException>(()=>CampaignTournament.Train(folder,config,"fixture",2,readBank:Fixtures));
            Assert.Throws<InvalidDataException>(()=>CampaignTournament.LockWinners(folder,config,"fixture"));
            Assert.False(File.Exists(Path.Combine(folder,"frozen.json")));
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder,recursive:true); }
    }
}
