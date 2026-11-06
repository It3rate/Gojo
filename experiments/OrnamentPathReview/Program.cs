using System.Text.Json;
using Gojo.Experiments.OrnamentPathCampaign;

var options=new JsonSerializerOptions{ PropertyNamingPolicy=JsonNamingPolicy.CamelCase,PropertyNameCaseInsensitive=true };
if(args.Length!=2)throw new ArgumentException("Supply a packed draw-request list and a new output path.");
var requests=JsonSerializer.Deserialize<Request[]>(File.ReadAllText(args[0]),options)!;
using var stream=new FileStream(args[1],FileMode.CreateNew,FileAccess.Write);
using var writer=new Utf8JsonWriter(stream);
writer.WriteStartArray();
foreach(var r in requests)
{
    var trace=PathMachine.Run(Convert.FromHexString(r.Hex),r.Input,r.Mode);
    var metrics=PathJudge.Measure(trace).Select(x=>double.IsNaN(x) ? (double?)null : x);
    JsonSerializer.Serialize(writer,new { r.Id,trace,metrics },options);
}
writer.WriteEndArray();
internal sealed record Request(string Id,string Hex,PathInput Input,PathMode Mode=PathMode.Live);
