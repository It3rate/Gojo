using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;

namespace Gojo.Experiments.QuickDraw;

public static class ConstructionLadderRunner
{
    public static int Run(string[] args)
    {
        if (args.Length != 5 || args[0] != "--construct" || args[1] != "--input" || args[3] != "--output")
        { Console.Error.WriteLine("Usage: --construct --input <recipes.ndjson> --output <directory>"); return 2; }
        var destination = Path.GetFullPath(args[4]); Directory.CreateDirectory(destination);
        using var reader = File.OpenText(args[2]);
        using var output = new StreamWriter(Path.Combine(destination, "inspector.ndjson"), false, new UTF8Encoding(false));
        var summaries = new List<object>(); var ids = new HashSet<string>(StringComparer.Ordinal); string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            using var document = JsonDocument.Parse(line); var source = document.RootElement;
            var id = Text(source, "id", "");
            if (id.Length == 0 || !ids.Add(id)) throw new InvalidDataException("Each recipe needs a unique id.");
            var program = ConstructionLadder.Import(source);
            if (!ConstructionLadder.TryExecute(program, out var initial)) throw new InvalidDataException("Initial construction did not execute.");
            var archive = GojoStructureCompactJson.FromElement("drawing-construction", initial.Surface);
            var json = GojoStructureCompactJson.Serialize(archive); var filename = $"{summaries.Count + 1:D3}.construction.gojo.json";
            File.WriteAllText(Path.Combine(destination, filename), json, new UTF8Encoding(false));
            var programArchive = GojoStructureCompactJson.FromElement("drawing-construction-program", program);
            var programJson = GojoStructureCompactJson.Serialize(programArchive);
            var programFilename = $"{summaries.Count + 1:D3}.program.gojo.json";
            File.WriteAllText(Path.Combine(destination, programFilename), programJson, new UTF8Encoding(false));
            if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored) ||
                restored.Root is not Relation root || !root.TryReadOrderedGroup(out var restoredFields) || restoredFields.Length != 7 ||
                restoredFields[0] is not Relation restoredProgram || !ConstructionLadder.TryExecute(restoredProgram, out var replay))
                throw new InvalidDataException("Restored construction program did not replay.");
            if (!initial.Sites.Select(s => s.Position).SequenceEqual(replay.Sites.Select(s => s.Position)) ||
                !initial.Constraints.Select(c => c.Satisfied).SequenceEqual(replay.Constraints.Select(c => c.Satisfied)))
                throw new InvalidDataException("Restored replay changed geometry or containment evidence.");
            if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(programJson), out var restoredOnly) ||
                restoredOnly.Root is not Relation onlyProgram || !ConstructionLadder.TryExecute(onlyProgram, out var onlyReplay) ||
                !initial.Sites.Select(s => s.Position).SequenceEqual(onlyReplay.Sites.Select(s => s.Position)))
                throw new InvalidDataException("Program-only archive did not independently reproduce geometry.");
            output.WriteLine(JsonSerializer.Serialize(Inspect(onlyReplay, source, filename, programFilename)));
            summaries.Add(new { id, archive = programFilename, witnessArchive = filename, sites = replay.Sites.Length, edges = replay.Edges.Length, rungs = replay.Rungs.Length,
                parameters = replay.Parameters.Length, dependencies = replay.Sites.Sum(s => s.Dependencies.Length),
                constraints = replay.Constraints.Length, satisfiedConstraints = replay.Constraints.Count(c => c.Satisfied == true),
                failedConstraints = replay.Constraints.Count(c => c.Satisfied == false), openConstraints = replay.Constraints.Count(c => c.Satisfied is null),
                repeats = replay.Repeats.Length, circularConstructions = replay.Circular.Length,
                nativeRestoreVerified = true, nativeReplayVerified = true, archiveNodes = programArchive.Counts.Nodes, witnessArchiveNodes = archive.Counts.Nodes });
        }
        File.WriteAllText(Path.Combine(destination, "summary.json"), JsonSerializer.Serialize(summaries,
            new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
        Console.WriteLine($"Restored and replayed {summaries.Count} native drawing constructions into {destination}"); return 0;
    }

    public static object Inspect(ConstructionLadder.Execution read, JsonElement source, string archive, string? programArchive = null)
    {
        var sites = source.GetProperty("sites").EnumerateArray().ToArray(); var edges = source.GetProperty("edges").EnumerateArray().ToArray();
        var rungs = source.GetProperty("rungs").EnumerateArray().ToArray(); var parameters = source.GetProperty("parameters").EnumerateArray().ToArray();
        var constraints = source.TryGetProperty("constraints", out var cr) ? cr.EnumerateArray().ToArray() : [];
        string S(int index) => Text(sites[index], "id", "s" + index);
        string E(int index) => Text(edges[index], "id", "e" + index);
        string R(int index) => Text(rungs[index], "id", "r" + index);
        object Point(ConstructionLadder.Site s) => new { id = S(s.Index), x = s.Position.X, y = s.Position.Y, at = s.At,
            label = Text(sites[s.Index], "label", S(s.Index)), dependencies = s.Dependencies.Select(S).ToArray() };
        object Edge(ConstructionLadder.Edge edge, ConstructionLadder.Rung rung) => new {
            id = E(edge.Index), label = Text(edges[edge.Index], "label", E(edge.Index)), at = edge.At,
            controls = edge.Controls.Select(s => new[] { s.Position.X, s.Position.Y }).ToArray(),
            controlSiteIds = edge.Controls.Select(s => S(s.Index)).ToArray(), from = S(edge.Controls[0].Index), to = S(edge.Controls[^1].Index),
            guide = rung.Guides.Contains(edge), introduced = rung.Introduced.Contains(edge),
            dependencies = edge.Controls.SelectMany(s => s.Dependencies.Concat(s.At < edge.At ? [s.Index] : [])).Distinct().Select(i => {
                var site = read.Sites[i]; var owner = read.Edges.FirstOrDefault(e => e.At <= site.At && e.Controls.Contains(site));
                return new { rungId = R(site.At), edgeId = owner is null ? null : E(owner.Index), siteId = S(i),
                    label = Text(sites[i], "label", S(i)), sameRung = site.At == edge.At, x = site.Position.X, y = site.Position.Y };
            }).ToArray()
        };
        return new {
            id = Text(source, "id", ""), family = Text(source, "family", Text(source, "id", "")),
            title = Text(source, "title", "Drawing construction"), description = Text(source, "description", ""),
            variantLabel = Text(source, "variantLabel", "Default"), viewBox = read.ViewBox, archive, programArchive = programArchive ?? archive,
            nativeRestoreVerified = true, nativeReplayVerified = true,
            numericPolicy = "Integer affine displacement; truncation toward zero with native source-basis residue retained.",
            parameters = read.Parameters.Select((p, i) => new { id = Text(parameters[i], "id", "p" + i), label = Text(parameters[i], "label", Text(parameters[i], "id", "p" + i)), value = p.Value }).ToArray(),
            rungs = read.Rungs.Select(rung => new { id = R(rung.Index), title = Text(rungs[rung.Index], "title", "Step " + (rung.Index + 1)),
                note = Text(rungs[rung.Index], "note", ""), index = rung.Index, archive, verifiedNativeRestore = true, verifiedNativeReplay = true,
                edges = rung.Visible.Concat(rung.Guides).Select(e => Edge(e, rung)).ToArray(),
                sites = read.Sites.Where(s => s.At <= rung.Index).Select(Point).ToArray(),
                constraints = read.Constraints.Where(c => c.At <= rung.Index).Select(c => new {
                    label = Text(constraints[c.Index], "label", "Convex control hull containment"),
                    status = c.Satisfied == true ? "satisfied" : c.Satisfied == false ? "failed" : "open",
                    note = c.Satisfied == true ? "All selected curve controls lie inside the authored convex boundary; the Bézier hull therefore stays inside."
                        : c.Satisfied == false ? "Some controls lie outside the authored boundary; the claim remains failed and geometry is unchanged."
                        : "The selected boundary is not a strict convex polygon, or no curve controls were supplied.",
                    edgeIds = c.Edges.Select(e => E(e.Index)).ToArray(), containerSiteIds = c.Container.Select(s => S(s.Index)).ToArray()
                }).ToArray()
            }).ToArray(),
            analysis = new { sites = read.Sites.Length, edges = read.Edges.Length, rungs = read.Rungs.Length,
                dependencies = read.Sites.Sum(s => s.Dependencies.Length), repeats = read.Repeats.Length, circularConstructions = read.Circular.Length }
        };
    }
    private static string Text(JsonElement source, string key, string fallback) => source.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
        ? value.GetString() ?? fallback : fallback;
}
