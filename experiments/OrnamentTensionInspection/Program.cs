using System.Security.Cryptography;
using System.Text.Json;
using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.OrnamentTension;

if(args.Length!=3)throw new ArgumentException("Supply registration seal, draw requests, and new output file.");
var options=new JsonSerializerOptions { PropertyNamingPolicy=JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive=true };
var seal=JsonDocument.Parse(File.ReadAllText(args[0])).RootElement;
if(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(TensionMachine).Assembly.Location)))!=seal.GetProperty("host").GetString())
    throw new InvalidDataException("The tension runtime differs from the recorded competition.");
var requests=JsonSerializer.Deserialize<Request[]>(File.ReadAllText(args[1]),options)!;
using var stream=new FileStream(args[2],FileMode.CreateNew,FileAccess.Write);
using var writer=new Utf8JsonWriter(stream);
writer.WriteStartArray();
foreach(var r in requests)
{
    var run=TensionMachine.Run(Convert.FromHexString(r.Path),r.Input,r.Slots,Convert.FromHexString(r.Policy),
        Enum.Parse<FeedbackMode>(r.Mode),true);
    JsonSerializer.Serialize(writer,new {r.Id,r.Family,r.Path,r.Policy,r.Input,r.Slots,r.Mode,trace=run.Trace,
        pressure=run.Stats,timeline=run.Timeline,run.Publication,joints=JointCensus.Read(run.Trace,true)},options);
}
writer.WriteEndArray();
internal sealed record Request(string Id,string Family,string Path,string Policy,PathInput Input,int Slots,string Mode);
