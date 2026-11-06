using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;

namespace Gojo.Experiments.OrnamentFollowup;

public static partial class Followup
{
    private static string Island(string folder,string goal,string arm,int rep) =>
        Path.Combine(folder,"islands",goal,arm,rep.ToString("D2",CultureInfo.InvariantCulture));
    private static string StatisticsBank(CampaignEpisode[] episodes) => Digest(episodes.Select(e=>new
        {e.Goal,e.Family,e.Group,e.Variant,e.Input}).ToArray());

    internal static CampaignEpisode[] Bank(CampaignEpisode[] source,int n,int seed)
    {
        if (source.Length<n) throw new ArgumentException("Requested bank exceeds available source episodes.");
        // Sample strata in turn; each group's first variant precedes its
        // second regardless of how many transformed siblings it contains.
        var strata = source.GroupBy(e=>(e.Goal,e.Family)).OrderBy(x=>x.Key.Goal,StringComparer.Ordinal)
            .ThenBy(x=>x.Key.Family,StringComparer.Ordinal).Select(stratum=>
            {
                var groups = stratum.GroupBy(x=>x.Group).OrderBy(g=>Digest(new{seed,g.Key}),StringComparer.Ordinal)
                    .Select(g=>g.OrderBy(e=>Digest(new {seed,e.Group,e.Variant}),StringComparer.Ordinal).ToArray()).ToArray();
                return new Queue<CampaignEpisode>(Enumerable.Range(0,groups.Max(g=>g.Length))
                    .SelectMany(index=>groups.Where(g=>g.Length>index).Select(g=>g[index])));
            }).ToArray();
        var bank = new List<CampaignEpisode>(n);
        for (int at=0; bank.Count<n; at++)
            if (strata[at%strata.Length].TryDequeue(out var e)) bank.Add(e);
        return bank.ToArray();
    }

    private static IOrderedEnumerable<TrainingEntry> Rank(IEnumerable<TrainingEntry> rows) => rows
        .OrderByDescending(x=>x.Training.Utility).ThenBy(x=>x.Training.Violations).ThenByDescending(x=>x.Training.Exact)
        .ThenBy(x=>x.GenomeHex.Length).ThenBy(x=>x.GenomeHex,StringComparer.Ordinal).ThenBy(x=>x.Id);

    private static TournamentMerit Score(OrnamentMachine program,CampaignEpisode[] bank,out string fingerprint)
    {
        long utility=0, violations=0;
        int exact=0;
        ulong hash=14695981039346656037UL;
        foreach (var e in bank)
        {
            var result = program.Run(e.Input);
            var score = CampaignScience.Evaluate(e,result);
            utility += score.Utility;
            violations += score.Violations;
            if (score.Exact) exact++;
            hash = unchecked((hash ^ result.Ink) * 1099511628211UL);
        }
        fingerprint = hash.ToString("X16",CultureInfo.InvariantCulture);
        return new(utility,violations,exact,bank.Length);
    }

    private static int FreshSeed(FollowupSpec spec,int rep) => (int)(ProposalRandom.Key(spec.Seed,rep) & 0x7FFFFFFF);
    private static string ChainRoot(FollowupSpec spec,string goal,string arm,int rep,string bank,string tickets) =>
        Digest(new{Protocol,Spec=Digest(spec),goal,arm,rep,bank,tickets});

    private static void TrainIsland(string folder,FollowupSpec spec,GoalMemory memory,string arm,int rep,Func<string,string,CampaignEpisode[]> supply)
    {
        var dir = Island(folder,memory.Goal,arm,rep);
        Directory.CreateDirectory(dir);
        int seed = FreshSeed(spec,rep);
        var bank = Bank(supply("train",memory.Goal),spec.TrainingEpisodes,seed);
        var bankHash = StatisticsBank(bank);
        var ticketHash = Digest(memory.Arms[arm]);
        var deck = memory.Arms[arm].Decode();
        var top = Array.Empty<TrainingEntry>();
        var previous = ChainRoot(spec,memory.Goal,arm,rep,bankHash,ticketHash);
        int goalIndex = Array.IndexOf(CampaignTournament.Objectives,memory.Goal);
        for (int gen=0;gen<spec.Generations;gen++)
        {
            var path = Path.Combine(dir,$"g{gen:D4}.json.gz");
            Ledger ledger;
            if (File.Exists(path)) ledger = LoadLedger(path,gen,previous,spec.Population);
            else
            {
                var rows = new TrainingEntry[spec.Population];
                for (int index=0;index<rows.Length;index++)
                {
                    int id = gen*rows.Length+index;
                    var rng = new ProposalRandom(ProposalRandom.Key(seed,goalIndex,gen,index));
                    OrnamentProgram candidate;
                    int? parentId = null;
                    string edit = "immigrant";
                    if (gen==0 || index%8==0) candidate = TournamentSearch.Random(false,ref rng);
                    else
                    {
                        var parent = top[rng.Below(Math.Min(4,top.Length))];
                        parentId = parent.Id;
                        var family = deck[rng.Below(deck.Length)];
                        edit = ProposalMemory.Families[family];
                        candidate = FollowupEdits.Mutate(OrnamentProgram.Decode(Convert.FromHexString(parent.GenomeHex)),family,ref rng);
                    }
                    var training = Score(candidate.Compile(),bank,out var signature);
                    rows[index] = new(id,Convert.ToHexString(candidate.Encode()),training,signature,parentId,null,edit);
                }
                var content = new Gen(gen,previous,rows);
                ledger = new(Digest(content),content);
                Atomic(path,outStream=>
                {
                    using var zip = new GZipStream(outStream,CompressionLevel.Fastest,leaveOpen:true);
                    JsonSerializer.Serialize(zip,ledger);
                });
            }
            previous = ledger.Hash;
            top = Rank(top.Concat(ledger.Content.Entries)).DistinctBy(e=>e.GenomeHex)
                .Take(Math.Max(4,spec.Shortlist)).ToArray();
        }
        var validation = supply("validation",memory.Goal);
        var finalists = top.Take(spec.Shortlist).Select(candidate=>new Finalist(candidate,
                Score(OrnamentProgram.Decode(Convert.FromHexString(candidate.GenomeHex)).Compile(),validation,out _)))
            .OrderByDescending(f=>f.Validation.Utility).ThenBy(f=>f.Validation.Violations).ThenByDescending(f=>f.Validation.Exact)
            .ThenByDescending(f=>f.Source.Training.Utility).ThenBy(f=>f.Source.GenomeHex.Length)
            .ThenBy(f=>f.Source.GenomeHex,StringComparer.Ordinal).ThenBy(f=>f.Source.Id).ToArray();
        Write(Path.Combine(dir,"winner.json"),new Winner(memory.Goal,arm,rep,seed,bankHash,previous,ticketHash,finalists));
        Atomic(Path.Combine(dir,"winner.genome"),s=>s.Write(Convert.FromHexString(finalists[0].Source.GenomeHex)));
    }

    private static void Lock(string folder,FollowupSpec spec,string sealHash,Func<string,string,CampaignEpisode[]> supply)
    {
        string path = Path.Combine(folder,"frozen.json");
        if (File.Exists(path)) { ReadLock(folder,sealHash,spec); return; }
        var memories = Read<GoalMemory[]>(Path.Combine(folder,"proposer.json"));
        var winners = new List<Winner>();
        var learning = new List<(string Goal,string Arm,int Rep,int Total,int Improved)>();
        foreach (var goal in spec.Goals)
        foreach (var arm in Arms)
        for (int rep=0;rep<spec.Replicates;rep++)
        {
            var dir = Island(folder,goal,arm,rep);
            var winner = Read<Winner>(Path.Combine(dir,"winner.json"));
            var bankHash = StatisticsBank(Bank(supply("train",goal),spec.TrainingEpisodes,FreshSeed(spec,rep)));
            var ticketHash = Digest(memories.Single(m=>m.Goal==goal).Arms[arm]);
            if(winner.Goal!=goal || winner.Arm!=arm || winner.Replicate!=rep || winner.Seed!=FreshSeed(spec,rep) ||
                winner.TrainingBankHash!=bankHash || winner.TicketHash!=ticketHash || winner.Finalists.Length is 0 ||
                winner.Finalists.Length>spec.Shortlist || !File.ReadAllBytes(Path.Combine(dir,"winner.genome"))
                    .SequenceEqual(Convert.FromHexString(winner.Finalists[0].Source.GenomeHex)))
                throw new InvalidDataException("Winner binding differs from its island: "+dir);
            string last = ChainRoot(spec,goal,arm,rep,bankHash,ticketHash);
            var byId = new Dictionary<int,TrainingEntry>();
            int attempted=0, improved=0;
            for (var gen=0;gen<spec.Generations;gen++)
            {
                var ledger = LoadLedger(Path.Combine(dir,$"g{gen:D4}.json.gz"),gen,last,spec.Population);
                foreach (var row in ledger.Content.Entries)
                {
                    if (row.ParentA is int parent)
                    {
                        if (parent >= gen*spec.Population || !byId.TryGetValue(parent,out var p)) throw new InvalidDataException("Unbound ancestry.");
                        attempted++; if (row.Training.Utility>p.Training.Utility) improved++;
                    }
                    byId.Add(row.Id,row);
                }
                last = ledger.Hash;
            }
            if (last != winner.HistoryHash || winner.Finalists.Any(f=>byId[f.Source.Id]!=f.Source))
                throw new InvalidDataException("Winner is missing from its paid history: "+dir);
            winners.Add(winner);
            learning.Add((goal,arm,rep,attempted,improved));
        }
        CsvFile(Path.Combine(folder,"new-proposal-yield.csv"), w=>
        {
            w.WriteLine("goal,arm,replicate,paid_mutations,strict_training_improvements");
            foreach (var row in learning) Row(w,row.Goal,row.Arm,row.Rep,row.Total,row.Improved);
        });
        var array = winners.ToArray();
        Write(path,new ChampionLock(Digest(new{sealHash,Winners=array}),sealHash,array));
    }

    private static ChampionLock ReadLock(string folder,string sealHash,FollowupSpec spec)
    {
        var item = Read<ChampionLock>(Path.Combine(folder,"frozen.json"));
        if (item.SealHash!=sealHash || item.Hash!=Digest(new{sealHash,item.Winners}) ||
            item.Winners.Length!=spec.Goals.Length*Arms.Length*spec.Replicates ||
            item.Winners.Select(w=>(w.Goal,w.Arm,w.Replicate)).Distinct().Count()!=item.Winners.Length ||
            item.Winners.Any(w=>!spec.Goals.Contains(w.Goal) || !Arms.Contains(w.Arm) ||
                w.Replicate < 0 || w.Replicate >= spec.Replicates || w.Finalists.Length==0))
            throw new InvalidDataException("Follow-up winner lock mismatch.");
        return item;
    }
}
