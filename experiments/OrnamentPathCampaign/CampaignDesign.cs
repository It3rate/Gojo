using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gojo.Experiments.OrnamentPathCampaign;

internal sealed record Scenario(string Name, string Parent, PathInput Input);
internal sealed record Brief(string Name, string Aim, string[] Emphasis);

internal static class Design
{
    public const int Replicates = 4, Population = 24, Generations = 18, Shortlist = 6;
    public static readonly string[] Groups = ["craft", "rhythm", "field", "symmetry", "topology", "seam"];
    public static readonly string[] Arms = ["full", "random", "unseeded", .. Groups.Select(x => "omit-" + x),
        "field", "craft+field", "seam+rhythm", "topology+craft", "symmetry+rhythm"];
    public static readonly Brief[] Briefs = [
        new("quiet-border", "A clear border with breathing room and a legible repeat.",
            ["breathing-room","gap-evenness","no-overdraw","register-balance","period-match","frame-respect"]),
        new("turning-key", "A continuous angular course that carries its turn rhythm across the seam.",
            ["connected-run","turn-recurrence","turn-variety","shared-continuation","boundary-match","tangent-match"]),
        new("countermarch", "Opposing figures answer one another across a band.",
            ["half-turn","half-glide","parallel-mirror","register-balance","accent-alternation","period-match"]),
        new("branched-spray", "A connected main stroke issues and receives subsidiary strokes.",
            ["branch-use","merge-use","owner-dialogue","connected-run","weight-contrast","no-false-joins"]),
        new("stepped-lace", "Returning paths enclose openings, with varied accents and clear crossings.",
            ["loop-use","owner-dialogue","extent-contrast","turn-variety","breathing-room","no-overdraw"])];

    // One equal voice per perspective before weighting its nominated reads.
    public static double[] Weights(Brief brief, string arm)
    {
        var criteria = PathJudge.Criteria;
        var weights = criteria.Select(c => brief.Emphasis.Contains(c.Key) ? 5.0 : 1.0).ToArray();
        foreach (string group in Groups)
        {
            var members = Enumerable.Range(0, weights.Length).Where(i => criteria[i].Group == group).ToArray();
            if (members.Length == 0) throw new InvalidDataException(group);
            double groupWeight = 1 + 2 * members.Count(i => brief.Emphasis.Contains(criteria[i].Key));
            bool active = arm is "random" or "full" or "unseeded" || (arm.StartsWith("omit-") ? arm[5..] != group : arm.Split('+').Contains(group));
            double sum = members.Sum(i => weights[i]);
            foreach (int i in members) weights[i] = active ? groupWeight * weights[i] / sum : 0;
        }
        double total = weights.Sum();
        return weights.Select(x => x / total).ToArray();
    }

    public static Scenario[] Scenarios(string split, int replicate = 0)
    {
        (int a, int b, int pitch)[] basis = split switch {
            "train" => [(1,1,2),(2,1,6),(2,3,4),(3,2,10)],
            "validation" => [(3,1,6),(1,3,8),(3,4,7),(4,2,12)],
            "blind" => [(4,1,8),(1,4,10),(3,5,6),(5,3,16),(2,5,9),(5,2,11),
                         (4,3,14),(2,4,4),(5,1,7),(3,3,6),(1,5,9),(4,5,18)],
            "reserve" => [(6,1,12),(1,6,14),(4,7,8),(7,4,22),(3,7,13),(7,3,15),
                          (5,4,18),(3,6,6),(6,2,9),(4,4,8),(2,7,12),(5,6,22)],
            _ => throw new ArgumentException(split)
        };
        var result = new List<Scenario>();
        for (int i=0; i<basis.Length; i++)
        {
            var (a,b,pitch) = basis[i];
            for (int v=0; v<(split=="train" ? 1 : 2); v++)
            {
                string parent = $"{split}-{i:D2}";
                result.Add(new($"{parent}-{v}",parent,new(a,b,pitch,3+(i+v)%3,
                    (i+v+(split=="train" ? replicate : 0))%4,(i+v+(split=="train" ? replicate : 0))%2)));
            }
        }
        return result.ToArray();
    }
}

internal static class Record
{
    public static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy=JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive=true, NumberHandling=JsonNumberHandling.AllowNamedFloatingPointLiterals };
    public static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    public static void Write<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try { File.WriteAllText(temp,JsonSerializer.Serialize(value,Json)); File.Move(temp,path,true); }
        finally { if(File.Exists(temp)) File.Delete(temp); }
    }
    public static T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path),Json)
        ?? throw new InvalidDataException(path);
    public static string F(double n) => n.ToString("R",CultureInfo.InvariantCulture);
    public static string Csv(params object[] fields) => string.Join(",",fields.Select(x =>
        "\"" + Convert.ToString(x,CultureInfo.InvariantCulture)!.Replace("\"","\"\"") + "\""));
    public static StreamWriter GzipWriter(string path) =>
        new(new GZipStream(File.Create(path),CompressionLevel.Fastest),new UTF8Encoding(false));
}
