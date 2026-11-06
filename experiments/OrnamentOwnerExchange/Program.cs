using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;
using Microsoft.VisualBasic.FileIO;

namespace Gojo.Experiments.OrnamentOwnerExchange;

internal static class Program
{
    private sealed record Owner(FrozenWinner Winner, OrnamentProgram Program)
    {
        public string Name => $"{Winner.Goal}-{Winner.Method}-{Winner.Replicate}";
    }
    private sealed record Control(CampaignEpisode Episode, RailResult Native, int Original, RailResult Cut, int CutUtility, int Idle);
    private sealed record Panel(Owner Writer, string Goal, Control[] Cases, int[][] Parents);

    public static void Main(string[] args)
    {
        if (args.Length is < 2 or > 3) throw new ArgumentException("Supply PRIMARY OUTPUT [WORKERS].");
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        var primary = Path.GetFullPath(args[0]);
        var output = Path.GetFullPath(args[1]);
        var workers = args.Length == 3 ? int.Parse(args[2]) : 8;
        if (workers is < 1 or > 12 || Directory.Exists(output)) throw new ArgumentException("Use 1–12 workers and a new output folder.");
        var protocolPath = Path.Combine(primary, "protocol.lock.json");
        var blind = Path.Combine(primary, "test");
        using var protocol = JsonDocument.Parse(File.ReadAllText(protocolPath));
        using var complete = JsonDocument.Parse(File.ReadAllText(Path.Combine(blind, "complete.json")));
        var primaryHash = protocol.RootElement.GetProperty("ProtocolHash").GetString()!;
        var runtimeHash = Hash(typeof(OrnamentProgram).Assembly.Location);
        Verify(runtimeHash == protocol.RootElement.GetProperty("RuntimeHash").GetString(), "Primary runtime hash");
        var casesPath = Path.Combine(blind, "cases.csv");
        var casesHash = Hash(casesPath);
        Verify(casesHash == complete.RootElement.GetProperty("FileHashes").GetProperty("cases.csv").GetString(), "Primary case records");
        var owners = CampaignTournament.ReadFrozen(primary, new TournamentConfig(), primaryHash)
            .Where(w => w.Method is "random" or "evolve")
            .Select(w => new Owner(w, OrnamentProgram.Decode(Convert.FromHexString(w.GenomeHex))))
            .OrderBy(o => o.Name, StringComparer.Ordinal).ToArray();
        Verify(owners.Length == 40 && owners.Select(o => o.Name).Distinct().Count() == 40, "All full-grammar winners");

        Directory.CreateDirectory(output);
        var specPath = Path.Combine(output, "Protocol.md");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Protocol.md"), specPath);
        WriteJson(Path.Combine(output, "design.json"), new { CreatedUtc = DateTimeOffset.UtcNow, RuntimeHash = runtimeHash,
            CasesHash = casesHash, PrimaryProtocol = primaryHash, FrozenFileHash = Hash(Path.Combine(primary, "frozen.json")),
            DesignHash = Hash(specPath), HostHash = Hash(typeof(Program).Assembly.Location), Scope = "post-hoc primary test" });
        var manifest = new List<string> { "owner,winner_id,genome_hex,sensor_with_idle_writer_hex,writer_flags,sensor_rows,writer_rows" };
        foreach (var o in owners)
        {
            var sensorOnly = new OrnamentProgram(o.Program.Sensor.ToArray(), [new(0, 0, 0, 0, 0)]);
            manifest.Add($"{o.Name},{o.Winner.Id},{o.Winner.GenomeHex},{Convert.ToHexString(sensorOnly.Encode())},{o.Program.Flags},{o.Program.Sensor.Count},{o.Program.Writer.Count}");
        }
        File.WriteAllLines(Path.Combine(output, "owners.csv"), manifest);
        var sealedRows = ReadNativeRows(casesPath);
        var sealedCount = sealedRows.Count;
        var idle = new OrnamentProgram([new(0,0,0)], [new(0,0,0,0,0)]).Compile();
        var panels = new List<Panel>();
        foreach (var writer in owners)
        {
            var native = writer.Program.Compile();
            foreach (var goal in writer.Winner.Goal == "mixed" ? CampaignScience.Goals : [writer.Winner.Goal])
            {
                var episodes = CampaignScience.Episodes("test", goal);
                var controls = new Control[episodes.Length];
                for (int i = 0; i < controls.Length; i++)
                {
                    var e = episodes[i];
                    var live = native.Run(e.Input);
                    var score = CampaignScience.Evaluate(e,live);
                    int unchanged = CampaignScience.Evaluate(e,idle.Run(e.Input)).Utility;
                    var key = (writer.Name, goal, e.Group, e.Variant);
                    Verify(sealedRows.Remove(key, out var fields), "Missing sealed diagonal " + key);
                    string[] expected = [writer.Winner.Goal, writer.Winner.Method, S(writer.Winner.Replicate),
                        S(writer.Winner.Seed), S(writer.Winner.Id), goal, e.Family, e.Group, S(e.Variant),
                        "Live", "native", S(score.Utility), S(score.Error), S(score.Violations), score.Exact.ToString(),
                        S(unchanged), S(score.Utility - unchanged), live.Ink.ToString("X"), S(live.Stock), S(live.Edits),
                        S(live.Attempts), S(live.Blocked), S(live.BudgetRefusals), S(live.Unresolved), S(live.Missing),
                        S(live.ExternalDelta), live.TraceHash.ToString("X16")];
                    Verify(expected.SequenceEqual(fields!), "Diagonal differed " + key);
                    var cut = native.Run(e.Input, WireMode.Cut);
                    controls[i] = new(e,live,score.Utility,cut,CampaignScience.Evaluate(e,cut).Utility,unchanged);
                }
                var parents = episodes.Select((e,index) => (e.Group,index)).GroupBy(x => x.Group)
                    .Select(group => group.Select(x=>x.index).ToArray()).ToArray();
                Verify(controls.Length == 144 && parents.Length == 72 && parents.All(p=>p.Length == 2), "Paired recipe coverage");
                panels.Add(new(writer,goal,controls,parents));
            }
        }
        Verify(panels.Count == 64 && sealedCount == 9216 && sealedRows.Count == 0, "Expected diagonal scope");
        var jobs = panels.SelectMany(p=>owners.Select(o=>(Panel:p,Donor:o))).ToArray();
        var lines = new string[jobs.Length];
        Parallel.For(0, jobs.Length, new ParallelOptions { MaxDegreeOfParallelism = workers }, i =>
            lines[i] = Run(jobs[i].Panel, jobs[i].Donor));
        var headers = "writer,donor,goal,program_hex,cases,parents,sum_utility,sum_original,sum_cut,sum_idle,changed_original,changed_cut,trace_changed_original,improved_original,lost_original,improved_cut,lost_cut,material_digest,violations";
        var results = Path.Combine(output, "panels.csv");
        File.WriteAllLines(results, new[] {headers}.Concat(lines));
        WriteJson(Path.Combine(output, "complete.json"), new { DiagonalCaseMatches = sealedCount, OrderedOwnerPairs = 40 * 40,
            ReceiverBriefs = panels.Count, EvaluatedPanels = lines.Length, TransplantEpisodes = lines.Length * 144,
            PanelHash = Hash(results), OwnerHash = Hash(Path.Combine(output,"owners.csv")), CompletedUtc = DateTimeOffset.UtcNow });
        Console.WriteLine($"{lines.Length} panels; {lines.Length * 144} native runs; {sealedCount} certified diagonals.");
    }

    private static string Run(Panel panel, Owner donor)
    {
        var writer = panel.Writer;
        var program = new OrnamentProgram(donor.Program.Sensor.ToArray(), writer.Program.Writer.ToArray(), writer.Program.Flags);
        var hex = Convert.ToHexString(program.Encode());
        bool diagonal = donor.Name == writer.Name;
        if (diagonal) Verify(hex == writer.Winner.GenomeHex, "Diagonal genome identity");
        var machine = program.Compile();
        var scores = new int[panel.Cases.Length];
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> observation = stackalloc byte[16];
        int sameOriginal = 0, sameCut = 0, traceChanged = 0, violations = 0;
        for (int i = 0; i < panel.Cases.Length; i++)
        {
            var control = panel.Cases[i];
            var outcome = machine.Run(control.Episode.Input);
            if (diagonal) Verify(outcome == control.Native, "Transplant diagonal observation");
            var score = CampaignScience.Evaluate(control.Episode, outcome);
            scores[i] = score.Utility;
            violations += score.Violations;
            sameOriginal += SameMaterial(outcome,control.Native) ? 1 : 0;
            sameCut += SameMaterial(outcome,control.Cut) ? 1 : 0;
            traceChanged += outcome.TraceHash != control.Native.TraceHash ? 1 : 0;
            BinaryPrimitives.WriteUInt64LittleEndian(observation,outcome.Ink);
            BinaryPrimitives.WriteInt32LittleEndian(observation[8..],outcome.Stock);
            BinaryPrimitives.WriteInt32LittleEndian(observation[12..],outcome.Edits);
            digest.AppendData(observation);
        }
        int improvedOriginal = 0, lostOriginal = 0, improvedCut = 0, lostCut = 0;
        foreach (var group in panel.Parents)
        {
            int originalDelta = group.Sum(i=>scores[i] - panel.Cases[i].Original);
            int cutDelta = group.Sum(i=>scores[i] - panel.Cases[i].CutUtility);
            int margin = 20 * group.Length;
            if (originalDelta >= margin) improvedOriginal++;
            if (originalDelta <= -margin) lostOriginal++;
            if (cutDelta >= margin) improvedCut++;
            if (cutDelta <= -margin) lostCut++;
        }
        return string.Join(',', writer.Name, donor.Name, panel.Goal, hex, scores.Length, panel.Parents.Length, scores.Sum(),
            panel.Cases.Sum(x=>x.Original), panel.Cases.Sum(x=>x.CutUtility), panel.Cases.Sum(x=>x.Idle),
            scores.Length-sameOriginal, scores.Length-sameCut, traceChanged, improvedOriginal, lostOriginal,
            improvedCut, lostCut, Convert.ToHexString(digest.GetHashAndReset()), violations);
    }

    private static Dictionary<(string,string,string,int),string[]> ReadNativeRows(string path)
    {
        using var csv = new TextFieldParser(path) { TextFieldType = FieldType.Delimited };
        csv.SetDelimiters(",");
        _ = csv.ReadFields();
        var rows = new Dictionary<(string,string,string,int),string[]>();
        while (!csv.EndOfData)
        {
            var fields = csv.ReadFields()!;
            if (fields[1] is not ("random" or "evolve") || fields[9] != "Live" || fields[10] != "native") continue;
            rows.Add(($"{fields[0]}-{fields[1]}-{fields[2]}",fields[5],fields[7],int.Parse(fields[8])),fields);
        }
        return rows;
    }

    private static bool SameMaterial(RailResult a, RailResult b) =>
        a.Ink == b.Ink && a.Stock == b.Stock && a.Edits == b.Edits;
    private static void Verify(bool condition,string check)
    {
        if (!condition) throw new InvalidDataException(check);
    }
    private static string S(int value) => value.ToString(CultureInfo.InvariantCulture);
    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
    private static void WriteJson<T>(string path,T data) =>
        File.WriteAllText(path,JsonSerializer.Serialize(data,new JsonSerializerOptions { WriteIndented = true }));
}
