using System.Security.Cryptography;
using System.Text.Json;
using Gojo.Core;
using PosterScratchpad;

var output = Path.GetFullPath("experiments/PosterScratchpad/out");
Directory.CreateDirectory(output);
if (args.FirstOrDefault() == "generate-more")
{
    IndependentPosters.Generate(Path.Combine(output, "series-2"));
    return;
}
if (args.FirstOrDefault() == "assess-more")
{
    SeriesTwoAssessment.Run(Path.Combine(output, "series-2"));
    return;
}
var a = PosterSurfaces.A();
var b = PosterSurfaces.B();
if (args.FirstOrDefault() == "assess")
{
    PosterAssessment.Run(a, b, output);
    return;
}
PosterRenderer.Render(a, Path.Combine(output, "poster-a.png"));
PosterRenderer.Render(b, Path.Combine(output, "poster-b.png"));
File.WriteAllText(Path.Combine(output, "poster-a.snapshot.gojo.json"),
    JsonSerializer.Serialize(GojoEncodedSnapshot.Capture(a), new JsonSerializerOptions { WriteIndented = true }));
File.WriteAllText(Path.Combine(output, "poster-b.snapshot.gojo.json"),
    JsonSerializer.Serialize(GojoEncodedSnapshot.Capture(b), new JsonSerializerOptions { WriteIndented = true }));
File.WriteAllText(Path.Combine(output, "generation-manifest.txt"),
    $"Generation completed UTC: {DateTime.UtcNow:O}\n" +
    $"A SHA256: {Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(output, "poster-a.png"))))}\n" +
    $"B SHA256: {Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(output, "poster-b.png"))))}\n" +
    "No comparison or suitability ratings have been created in this generation phase.\n");
Console.WriteLine($"Generated both posters and Gojo snapshots at {output}");
