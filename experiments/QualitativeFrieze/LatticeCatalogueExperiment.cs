using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A bounded construction catalogue, not a universal lattice or
/// aesthetic solver. Native programs supply every path, period and seam.
/// Presentation compares witnessed edits and offers revisable human ratings.</summary>
public static partial class LatticeCatalogueExperiment
{
    private sealed record StagePlate(int Index, string Name, string Description, string Kind,
        string Field, string Change, int Selected, int Carried, int Introduced, int WithdrawnPorts, int Depth);
    private sealed record Plate(string Id, string Name, string Family, string Description,
        LatticeCatalogueSummary Read, int Complexity, string ReviewCue, int NativeObjects, int Numeric,
        int AncestorAnswers, string PacketSha256, StagePlate[] Stages);
    private sealed record RenderedStage(StagePlate Plate);
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "lattice-catalogue");
        var samples = 3; var fullWitnesses = false;
        for (var index = 0; index < args.Length; index++)
            if (args[index] == "--output" && index + 1 < args.Length) output = args[++index];
            else if (args[index] == "--samples" && index + 1 < args.Length && int.TryParse(args[++index], out var size) && size is >= 2 and <= 4) samples = size;
            else if (args[index] == "--full-witnesses") fullWitnesses = true;
            else throw new ArgumentException("Usage: --lattice-catalogue [--samples 2..4] [--output DIRECTORY] [--full-witnesses]");
        Directory.CreateDirectory(output);
        var study = new LatticeCatalogueRecipes(samples, samples);
        var plates = new List<Plate>();
        var stageCache = new Dictionary<Relation, RenderedStage>(ReferenceEqualityComparer.Instance);
        foreach (var item in study.Cases)
        {
            var stages = new List<StagePlate>();
            foreach (var stage in item.Stages)
            {
                if (stageCache.TryGetValue(stage.Recipe.Program.Surface, out var cached))
                { stages.Add(cached.Plate with { Index = stages.Count }); continue; }
                var run = stage.Recipe.Program.Generate(stage.Recipe.Bindings, stage.Recipe.Window);
                var field = new OrnamentProgramField(run, stage.Recipe.SeamContract);
                if (!run.IsComplete || !field.IsComplete || !stage.Ladder.Current.IsComplete)
                    throw new InvalidOperationException(item.Id + ": a displayed construction stage must execute its actual native declarations.");
                var read = LatticeCatalogueStepRead.Read(stage.Ladder.Current);
                var prefix = item.Id + "-s" + stages.Count;
                Save(output, prefix + ".svg", FieldSvg(stage.Name, field, stage.Recipe.DisplayBasis));
                Save(output, prefix + "-change.svg", ChangeSvg(stage.Name, read, stage.Recipe.DisplayBasis));
                var plate = new StagePlate(stages.Count, stage.Name, stage.Description, read.Kind,
                    prefix + ".svg", prefix + "-change.svg", N.E(read.SelectedPaths).Length,
                    N.E(read.CarriedResultPaths).Length, N.E(read.IntroducedPaths).Length,
                    N.E(read.WithdrawnPorts).Length, stage.Ladder.Rungs.Count - 1);
                stages.Add(plate); stageCache.Add(stage.Recipe.Program.Surface, new(plate));
            }

            var current = item.Current;
            // All construction stages accompany the final executable packet.
            // Shared actual participants are serialized once within this world.
            var root = Packet(current, N.S(item.Stages.Select(stage => (IElement)Packet(stage, stage.Evidence)).ToArray()));
            var archive = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("lattice-catalogue-v1", root));
            Native(output, item.Id + ".gojo.json.gz", archive);
            var imported = LatticeLadderExperiment.Import(archive);
            var actualRun = imported.Program.Generate(imported.Bindings, imported.Window);
            var actualField = new OrnamentProgramField(actualRun, imported.Contract);
            var assessment = LatticeCatalogueAssessment.Read(imported.Ladder.Current.Program, actualRun, actualField);
            var numeric = Reachable(root).Count(element => element is Carrier or CarrierLocation);
            if (numeric != 0) throw new InvalidOperationException("Catalogue construction roots must contain only native unmeasured participants.");
            var queries = 0;
            foreach (var rung in imported.Ladder.Rungs.Take(imported.Ladder.Rungs.Count - 1))
            {
                if (!imported.Ladder.TryReadAncestor(rung.Program.Surface, out var result, out _) ||
                    !ReferenceEquals(result.Surface, rung.Program.Surface))
                    throw new InvalidOperationException("Accepted edits must preserve exact retained source identities.");
                queries++;
            }
            Save(output, item.Id + ".svg", FieldSvg(item.Name, actualField, current.Recipe.DisplayBasis));
            if (fullWitnesses || plates.Count % 20 == 0)
                Native(output, item.Id + "-rules.gojo.json.gz", GojoStructureCompactJson.Serialize(
                    GojoStructureCompactJson.FromElement("lattice-catalogue-rules-v1", assessment.Surface)));
            var summary = assessment.Summary;
            var complexity = Math.Clamp(1 + summary.MotifChords / 10 + Math.Max(0, summary.MaxDegree - 3) / 2, 1, 5);
            var cue = !summary.IsStructurallyAdmissible ? "Revise failed obligations" :
                summary.GeometricOverlaps > 0 ? "Inspect overlapping support" :
                summary.UnjoinedContacts > 0 ? "Inspect unjoined contacts" :
                summary.UnjoinedCrossings > 0 ? "Crossing strands" :
                summary.Components > samples * samples ? "Open or separate units" : "Ready for visual rating";
            plates.Add(new(item.Id, item.Name, item.Family, item.Description, summary, complexity, cue,
                Reachable(root).Count(), numeric, queries,
                Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(archive))).ToLowerInvariant(), stages.ToArray()));
            Console.WriteLine($"{item.Id}: {item.Family}; {(summary.IsStructurallyAdmissible ? "admitted" : "REVISE")}, " +
                $"{summary.MotifChords} chords, {summary.ImmediateRetraces}/{summary.RetracedPathPairs} retraces, {stages.Count} stages.");
        }

        var counterexamples = Counterexamples(study.Cases[0], output, samples);
        var duplicates = plates.Where(plate => plate.Read.Fingerprint is not null)
            .GroupBy(plate => plate.Read.Fingerprint).Where(group => group.Count() > 1)
            .Select(group => group.Select(plate => plate.Id).ToArray()).ToArray();
        var admitted = plates.Count(plate => plate.Read.IsStructurallyAdmissible);
        var distinct = plates.Select(plate => plate.Read.Fingerprint).Where(value => value is not null).Distinct().Count();
        Save(output, "diagnostics.json", JsonSerializer.Serialize(new
        {
            Schema = "gojo-lattice-catalogue-v1", Cases = plates, Admitted = admitted, DistinctFiniteSupportReads = distinct,
            DuplicateGroups = duplicates, Families = plates.GroupBy(plate => plate.Family).Select(group => new { Name = group.Key, Cases = group.Count() }),
            UniqueRenderedStages = stageCache.Count, Counterexamples = counterexamples, FullRuleWitnesses = fullWitnesses,
            Limits = new[] {
                "Acceptance concerns selected native obligations, not universal aesthetic merit or infinite occupied-area tiling.",
                "Shared boundary histories, unjoined crossings, unjoined contacts and overlapping projected support are different reads.",
                "The comparison fingerprint uses a selected common-reach finite window and is not general graph isomorphism.",
                "Complexity and review cues are provisional calculated diagnostics. Human ratings are a separate revisable question.",
                "Current native units are independent of display colours; change highlights explain correspondence, not material flow."
            }
        }, Options));
        Save(output, "gallery.html", Gallery(plates));
        Save(output, "overview.html", Overview(plates));
        Save(output, "overview.svg", OverviewSvg(plates, output));
        Save(output, "construction-strips.html", ConstructionStrips(plates));
        Console.WriteLine($"Saved {admitted}/{plates.Count} admitted cases; {distinct} distinct finite support reads; " + Path.GetFullPath(output));
        return admitted >= 100 && distinct >= 100 ? 0 : 1;
    }

    private static object[] Counterexamples(LatticeCatalogueCase source, string output, int samples)
    {
        var stage = source.Current; var unit = stage.Ladder.Current.Program;
        var first = N.E(unit.Paths).Cast<Relation>().First();
        var word = N.E((Relation)first.Right).Cast<Relation>().First();
        var reach = N.E(word).Cast<Relation>().First().Right;
        var author = new LatticeLadderAuthor(unit.Frame, reach);
        var path = LatticeLadderAuthor.Path((Relation)first.Left, word, author.Inverse(word));
        var backtrack = author.Unit("deliberate immediate retrace", path);
        var recipe = author.Compile("X01", "Immediate retrace", "Deliberately rejected", "Counterexample", backtrack,
            stage.Recipe.ColumnPitch, stage.Recipe.RowPitch, samples, samples);
        var run = recipe.Program.Generate(recipe.Bindings, recipe.Window);
        var field = new OrnamentProgramField(run, recipe.SeamContract);
        var bad = LatticeCatalogueAssessment.Read(backtrack, run, field);
        if (bad.Summary.IsStructurallyAdmissible) throw new InvalidOperationException("The actual retrace must fail its rule.");
        Save(output, "X01.svg", FieldSvg("Rejected immediate retrace", field, recipe.DisplayBasis));
        Native(output, "X01-rules.gojo.json.gz", GojoStructureCompactJson.Serialize(
            GojoStructureCompactJson.FromElement("lattice-retrace-counterexample-v1", bad.Surface)));
        var cleanRun = stage.Recipe.Program.Generate(stage.Recipe.Bindings, stage.Recipe.Window);
        var falseField = new OrnamentProgramField(cleanRun, LatticeLadderAuthor.BlockedContract(stage.Recipe));
        var blocked = LatticeCatalogueAssessment.Read(unit, cleanRun, falseField);
        if (blocked.Summary.IsStructurallyAdmissible) throw new InvalidOperationException("A failed exposed seam must remain rejected.");
        Save(output, "X02.svg", FieldSvg("Rejected false seam offer", falseField, stage.Recipe.DisplayBasis));
        Native(output, "X02-rules.gojo.json.gz", GojoStructureCompactJson.Serialize(
            GojoStructureCompactJson.FromElement("lattice-seam-counterexample-v1", blocked.Surface)));
        return [new { Id = "X01", Name = "Immediate line retrace", Summary = bad.Summary },
            new { Id = "X02", Name = "False neighbouring-role agreement", Summary = blocked.Summary }];
    }

    private static Relation Packet(LatticeCatalogueStage stage, IElement evidence) => new(stage.Ladder.Surface,
        new Relation(new Relation(new Relation(stage.Recipe.Program.Surface, new Relation(stage.Recipe.Bindings, stage.Recipe.Window)),
            stage.Recipe.SeamContract), evidence));
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(N.Participants.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item))
        { if (!seen.Add(item)) continue; yield return item; if (item is Relation row) { pending.Push(row.Left); pending.Push(row.Right); } }
    }
    private static void Save(string output, string name, string text)
    { var target = Path.Combine(output, name); File.WriteAllText(target + ".pending", text); File.Move(target + ".pending", target, true); }
    private static void Native(string output, string name, string json)
    {
        var target = Path.Combine(output, name);
        using (var stream = File.Create(target + ".pending")) using (var zip = new GZipStream(stream, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(json);
        File.Move(target + ".pending", target, true);
    }
}
