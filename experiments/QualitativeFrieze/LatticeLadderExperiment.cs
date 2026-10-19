using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Runs native construction histories, then separately asks for a
/// history-blind read of their executed incidence. Captions never choose a
/// transform, certify a seam or supply a recognized grid.</summary>
public static partial class LatticeLadderExperiment
{
    private sealed record Candidate(int Channels, int Selected, int Commutations, int Triangles,
        int UnexpectedTriangles, int Residue, int Competing, int Boundary, int Unsettled);
    private sealed record Ancestor(string Id, string Name, bool SameActualProgram);
    private sealed record Plate(string Id, string Name, string? ParentId, string Description, string Atlas,
        int Depth, int Paths, int Chords, int Units, int Accepted, int Blocked, int Open, int Frontier,
        int Numeric, int Objects, int RawStrokes, int IncidenceStrokes, int SupportStrokes, Candidate[] Raw, Candidate[] Support,
        int Unsearched, Ancestor[] Ancestors);
    private sealed record Imported(QualitativeLatticeLadder Ladder, OrnamentProgram Program,
        Relation Bindings, Relation Window, Relation Contract, Relation Root);
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "lattice-ladders");
        var samples = 3;
        for (var index = 0; index < args.Length; index++)
            if (args[index] == "--output" && index + 1 < args.Length) output = args[++index];
            else if (args[index] == "--samples" && index + 1 < args.Length && int.TryParse(args[++index], out var size) && size is >= 2 and <= 4) samples = size;
            else throw new ArgumentException("Usage: --lattice-ladders [--samples 2..4] [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var study = new LatticeLadderRecipes(samples, samples);
        var plates = new List<Plate>();
        foreach (var item in study.Cases)
        {
            var root = new Relation(item.Ladder.Surface, new Relation(
                Packet(item.Recipe), item.PitchEvidence));
            var archive = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("lattice-ladder-v1", root));
            WriteNative(Path.Combine(output, item.Id + ".gojo.json.gz"), archive);
            var imported = Import(archive);
            var run = imported.Program.Generate(imported.Bindings, imported.Window);
            var field = new OrnamentProgramField(run, imported.Contract);
            if (!run.IsComplete || !field.IsComplete || !imported.Ladder.Current.IsComplete)
                throw new InvalidOperationException(item.Id + " must retain its complete native execution and interior seams.");
            var raw = LatticeLadderObservation.Read(run, field);
            var support = LatticeLadderObservation.ReadStrandSupport(run, field);
            if (N.E(raw.Blocked).Length != 0 || N.E(support.Blocked).Length != 0)
                throw new InvalidOperationException("Current graph observation must not silently accept a broken publication.");
            var elements = Reachable(root).ToArray();
            var numeric = elements.Count(element => element is Carrier or CarrierLocation);
            if (numeric != 0) throw new InvalidOperationException("The native ladder must remain pre-numeric.");
            var ancestors = new List<Ancestor>(); var reads = new List<IElement>();
            for (var rung = 0; rung < imported.Ladder.Rungs.Count - 1; rung++)
            {
                var actual = imported.Ladder.Rungs[rung].Program;
                if (!imported.Ladder.TryReadAncestor(actual.Surface, out var returned, out var witness) || !ReferenceEquals(returned.Surface, actual.Surface))
                    throw new InvalidOperationException("An ancestor question must return the actual retained native program.");
                var authored = item.Ladder.Rungs[rung].Program;
                var source = study.Cases.First(candidate => ReferenceEquals(candidate.Unit.Surface, authored.Surface));
                ancestors.Add(new(source.Id, source.Name, true)); reads.Add(witness);
            }
            WriteNative(Path.Combine(output, item.Id + "-questions.gojo.json.gz"), GojoStructureCompactJson.Serialize(
                GojoStructureCompactJson.FromElement("lattice-ladder-questions-v1", new Relation(N.S(reads.ToArray()),
                    new Relation(raw.Surface, support.Surface)))));
            var plate = new Plate(item.Id, item.Name, item.ParentId, item.Description, item.AtlasReferences,
                imported.Ladder.Rungs.Count - 1, N.E(imported.Ladder.Current.Program.Paths).Length,
                N.E(imported.Ladder.Current.Program.Paths).Cast<Relation>().Sum(path => N.E((Relation)path.Right).Length),
                run.Units.Count, N.E(field.Accepted).Length, N.E(field.Blocked).Length, N.E(field.Unresolved).Length,
                N.E(field.Frontier).Length, numeric, elements.Length, raw.SourceStrokeCount, support.IncidenceStrokeCount, support.ObservedStrokeCount,
                Counts(raw), Counts(support), N.E(support.Unsearched).Length, ancestors.ToArray());
            plates.Add(plate);
            WriteText(Path.Combine(output, item.Id + ".svg"), Svg(item.Name, field, item.Recipe.DisplayBasis));
            Console.WriteLine($"{item.Id}: depth {plate.Depth}, {plate.Paths} paths, {plate.Chords} chords; {plate.Accepted} accepted / {plate.Blocked} blocked; {plate.Ancestors.Length} actual ancestor answers.");
        }

        var seed = study.Cases[0];
        var seedRun = seed.Recipe.Program.Generate(seed.Recipe.Bindings, seed.Recipe.Window);
        var falseSeam = new OrnamentProgramField(seedRun, study.BlockedSeamContract);
        if (N.E(falseSeam.Blocked).Length == 0) throw new InvalidOperationException("The displaced seam counterexample must remain blocked.");
        WriteText(Path.Combine(output, "blocked-seam.svg"), Svg("A deliberately displaced seam remains blocked", falseSeam, seed.Recipe.DisplayBasis));
        WriteNative(Path.Combine(output, "blocked-seam.gojo.json.gz"), GojoStructureCompactJson.Serialize(
            GojoStructureCompactJson.FromElement("lattice-blocked-seam-v1", falseSeam.Surface)));

        var crystal = Crystallize(study.Cases.Single(item => item.Id == "L05"), output, samples);
        var diagnostics = new
        {
            Schema = "gojo-lattice-ladders-v1", Cases = plates,
            Crystallization = crystal, BlockedCounterexample = new { Blocked = N.E(falseSeam.Blocked).Length, Open = N.E(falseSeam.Unresolved).Length },
            Limits = new[] {
                "Historical basis is an actual retained participant, not proof that the current graph still has every ancestral property.",
                "Transforms, endpoint-preserving refinements, topology edits and assembly are different witnessed transitions.",
                "History-blind candidates use actual shared incidence and qualitative headings; they do not receive recipe labels or construction history.",
                "Incidence support is a non-draining coarse question with explicit many-to-one stroke correspondence; drawing histories remain available.",
                "Boundary obligations and unsearched candidates remain distinct from failed interior seams.",
                "Atlas references name construction families; exact identity with every source engraving is not claimed.",
                "No regular polygon metric, universal pattern recognition, aesthetic score or autonomous learning is established."
            }
        };
        WriteText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(diagnostics, Options));
        WriteText(Path.Combine(output, "gallery.html"), Html(plates, study.Cases, crystal));
        WriteText(Path.Combine(output, "overview.html"), OverviewHtml(plates));
        WriteText(Path.Combine(output, "overview.svg"), OverviewSvg(plates, output));
        Console.WriteLine("Saved lattice ladders at " + Path.GetFullPath(output));
        return 0;
    }

    private static object Crystallize(LatticeLadderCase source, string output, int samples)
    {
        var capture = source.Ladder.Crystallize("standalone current lattice");
        var fresh = capture.Standalone;
        var recipe = LatticeLadderAuthor.Rebuild(source.Recipe, fresh.Current.Program, samples, samples);
        var root = new Relation(fresh.Surface, new Relation(Packet(recipe), N.S()));
        var archive = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("crystallized-lattice-v1", root));
        WriteNative(Path.Combine(output, "crystallized.gojo.json.gz"), archive);
        var imported = Import(archive);
        var run = imported.Program.Generate(imported.Bindings, imported.Window);
        var field = new OrnamentProgramField(run, imported.Contract);
        if (!run.IsComplete || !field.IsComplete) throw new InvalidOperationException("Crystallized current geometry must execute independently.");
        var originalNodes = Reachable(source.Ladder.Surface).ToHashSet(N.Participants.Instance);
        if (Reachable(root).Any(originalNodes.Contains)) throw new InvalidOperationException("The standalone world must not retain ancestral participants.");
        if (fresh.TryReadAncestor(source.Ladder.Rungs[0].Program.Surface, out _, out var unanswered))
            throw new InvalidOperationException("Forgotten construction history cannot answer an exact ancestry question.");
        var support = LatticeLadderObservation.ReadStrandSupport(run, field);
        var inferredGrid = support.Summaries.Any(candidate => candidate.Channels == 2 && candidate.Commutations > 0 && candidate.Residue == 0);
        WriteText(Path.Combine(output, "crystallized.svg"), Svg("Standalone current lattice · history forgotten", field, recipe.DisplayBasis));
        WriteNative(Path.Combine(output, "crystallization-loss.gojo.json.gz"), GojoStructureCompactJson.Serialize(
            GojoStructureCompactJson.FromElement("crystallization-loss-v1", capture.LossWitness)));
        WriteNative(Path.Combine(output, "crystallized-questions.gojo.json.gz"), GojoStructureCompactJson.Serialize(
            GojoStructureCompactJson.FromElement("crystallized-questions-v1", new Relation(unanswered, support.Surface))));
        if (!inferredGrid) throw new InvalidOperationException("The selected clean sheared incidence must support a fresh grid reading after history loss.");
        return new { SourceId = source.Id, BeforeDepth = source.Ladder.Rungs.Count - 1, AfterDepth = 0,
            IndependentParticipants = true, ExactAncestorAvailable = false, BlindGridRecovered = inferredGrid,
            ForgottenRungs = source.Ladder.Rungs.Count,
            ForgottenEvidenceEntries = N.E(capture.Forgotten).Length, Support = Counts(support) };
    }

    private static Relation Packet(BourgoinPatternLibrary.Recipe recipe) =>
        new(new Relation(recipe.Program.Surface, new Relation(recipe.Bindings, recipe.Window)), recipe.SeamContract);

    private static Imported Import(string archive)
    {
        var document = GojoStructureCompactJson.Deserialize(archive);
        if (document.Carriers.Count != 0 || document.Nodes.Any(node => node.Kind is not ("relation" or "placeholder")))
            throw new InvalidOperationException("Only native pre-numeric participants may enter the experiment.");
        var captured = GojoStructureCompactJson.ToElement(document).Root;
        if (captured is not Relation { Right: Relation { Left: Relation { Left: Relation { Left: Relation program } } } } root ||
            program.Left is not Relation frame || !QualitativeMotionFrameRead.TryReadStandard(frame, out var frameRead) ||
            !frameRead.TryRebind(root, out var result, out _) || result is not Relation rebound ||
            rebound.Right is not Relation { Left: Relation packet } || packet.Left is not Relation sample ||
            sample.Left is not Relation definition || sample.Right is not Relation chosen ||
            definition.Right is not Relation { Left: Relation motifs, Right: Relation { Left: Relation transforms,
                Right: Relation { Left: Relation parameters, Right: Relation layout } } } ||
            chosen.Left is not Relation bindings || chosen.Right is not Relation window || packet.Right is not Relation contract ||
            rebound.Left is not Relation ladderSurface || !QualitativeLatticeLadder.TryRead(frameRead.Target, ladderSurface, out var ladder))
            throw new InvalidOperationException("The exact native ladder and program declarations must survive transport together.");
        var executable = new OrnamentProgram(frameRead.Target, motifs, transforms, parameters, layout, definition);
        if (!N.E(motifs).Any(motif => ReferenceEquals(motif, ladder.Current.Program.Surface)))
            throw new InvalidOperationException("The executed motif must be the actual imported current rung, not a lookalike.");
        return new(ladder, executable, bindings, window, contract, rebound);
    }

    private static Candidate[] Counts(LatticeLadderObservation read) => read.Summaries.Select(candidate =>
        new Candidate(candidate.Channels, candidate.Selected, candidate.Commutations, candidate.Closures,
            candidate.UnexpectedClosures, candidate.Residue, candidate.Competing, candidate.Boundary, candidate.Unsettled)).ToArray();
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(N.Participants.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item))
        { if (!seen.Add(item)) continue; yield return item; if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); } }
    }
    private static void WriteNative(string path, string json)
    {
        var pending = path + ".pending";
        using (var file = File.Create(pending)) using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(json);
        File.Move(pending, path, true);
    }
    private static void WriteText(string path, string text)
    { var pending = path + ".pending"; File.WriteAllText(pending, text); File.Move(pending, path, true); }
    private static string H(string value) => WebUtility.HtmlEncode(value);
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
