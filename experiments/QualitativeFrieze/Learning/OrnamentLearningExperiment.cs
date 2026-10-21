using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using C = Gojo.Experiments.QualitativeFrieze.Learning.OrnamentLearningCorpus;
using M = Gojo.Experiments.QualitativeFrieze.Learning.OrnamentLearningMemory;

namespace Gojo.Experiments.QualitativeFrieze.Learning;

/// <summary>A streaming symbolic learning pilot. Teacher construction and
/// discovered teacher boundaries supervise memory and evaluate retrieval;
/// only exposed clue streams reach the receiver during evaluation.</summary>
public static class OrnamentLearningExperiment
{
    public const string Protocol = "ornament-learning-pilot-v1";
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public sealed record Row(int Index, string Group, string Split, int Family, int Edit,
        int TeacherBoundaries, bool SeenExact, bool SeenShape, bool Conflicting,
        int Streams, int Admissible, int FirstStreamAdmissible,
        bool ExactTop1, bool ShapeTop1, bool ShapeTop3, bool FirstStreamShapeTop1,
        bool UniformShapeTop1, bool FrequencyShapeTop1, bool RandomShapeTop1,
        bool GeneratedClosed, int GeneratedOpenClaims, double Milliseconds,
        bool TeacherShapeAdmissible = false, bool TeacherExactAdmissible = false);

    public sealed record Partition(string Split, int Rows, int Groups, int WithTeacherBoundary,
        int SeenExact, int SeenShape, int Conflicting, int ConflictAbstentions, int Abstentions,
        int MeaningsProposed, int FirstStreamMeaningsProposed, int ExactTop1, int ShapeTop1,
        int ShapeTop3, int FirstStreamShapeTop1, int UniformShapeTop1, int FrequencyShapeTop1,
        int RandomShapeTop1, int UnseenShapeRows, int UnseenShapeTop1, int GeneratedClosed,
        int GeneratedWithOpenClaims, double Milliseconds,
        int CleanFamiliarRows, int CleanFamiliarAdmitted, int CleanFamiliarTop1,
        int SingleBoundaryFamiliarRows, int SingleBoundaryFamiliarAdmitted);

    public static int Run(string[] args)
    {
        var count = 1536; var seed = 1729; var familyHoldout = false; var exampleLimit = 24; var refresh = false;
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "ornament-learning");
        string? checkpoint = null;
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else if (args[i] == "--count" && i + 1 < args.Length && int.TryParse(args[++i], out var value) && value is >= 96 and <= 1000000) count = value;
            else if (args[i] == "--seed" && i + 1 < args.Length && int.TryParse(args[++i], out var valueSeed) && valueSeed >= 0) seed = valueSeed;
            else if (args[i] == "--examples" && i + 1 < args.Length && int.TryParse(args[++i], out var examples) && examples is >= 0 and <= 96) exampleLimit = examples;
            else if (args[i] == "--family-holdout") familyHoldout = true;
            else if (args[i] == "--refresh-gallery") refresh = true;
            else if (args[i] == "--model" && i + 1 < args.Length) checkpoint = args[++i];
            else throw new ArgumentException("Usage: --ornament-learning [--count 96..1000000] [--seed N] [--family-holdout] [--examples 0..96] [--model FILE] [--refresh-gallery] [--output DIRECTORY]");

        Directory.CreateDirectory(output);
        if (refresh)
        {
            var summaryRead = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "summary.json"))).RootElement.Clone();
            var saved = Load(Path.Combine(output, "model.gojo.json.gz"));
            var evaluated = File.ReadLines(Path.Combine(output, "evaluation.jsonl")).Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => JsonSerializer.Deserialize<Row>(line) ?? throw new InvalidOperationException("Unreadable evaluation row.")).ToArray();
            var described = C.Describe(summaryRead.GetProperty("count").GetInt32(), summaryRead.GetProperty("seed").GetInt32(),
                summaryRead.GetProperty("splitProtocol").GetString() == "whole-family-held-out");
            OrnamentLearningGallery.Write(output, summaryRead, SelectExamples(saved, described, evaluated, exampleLimit));
            Console.WriteLine($"Refreshed {Path.GetFullPath(output)} without training or changing evaluation.");
            return 0;
        }
        var watch = Stopwatch.StartNew(); var process = Process.GetCurrentProcess();
        var descriptors = C.Describe(count, seed, familyHoldout);
        EnsurePartitionIsolation(descriptors);
        var trainRows = 0; var usableTrainRows = 0; var trainBoundaries = 0;
        using var manifest = new StreamWriter(Path.Combine(output, "manifest.jsonl"), false, new UTF8Encoding(false));
        IEnumerable<M.TrainingItem> Training()
        {
            foreach (var descriptor in descriptors.Where(item => item.Split == "train"))
            {
                var sample = C.Materialize(descriptor, includePattern: false);
                var features = Read(sample.Target);
                var patterns = TeacherPatterns(features);
                trainRows++; trainBoundaries += patterns.Length;
                if (patterns.Length != 0) usableTrainRows++;
                manifest.WriteLine(JsonSerializer.Serialize(new { descriptor, keys = patterns.Select(item => item.Key), shapes = patterns.Select(item => item.ShapeKey) }));
                // A small independent native identity records the episode.
                // Holding the complete sample here would retain every program.
                yield return new(new Relation(new Placeholder(), new Placeholder()), features);
                if (trainRows % 128 == 0)
                {
                    process.Refresh();
                    Console.WriteLine($"train {trainRows}: {usableTrainRows} usable scenes; {watch.Elapsed.TotalSeconds:0.0}s; {process.WorkingSet64 / 1048576} MiB");
                }
            }
        }
        Console.WriteLine($"{Protocol}; {count} descriptors; {(familyHoldout ? "whole-family" : "recipe/edit-group")} holdout; seed {seed}.");
        var model = checkpoint is null ? M.Train(Training()) : Load(checkpoint);
        manifest.Flush();
        var trainedSeconds = watch.Elapsed.TotalSeconds;
        var modelFile = Path.Combine(output, "model.gojo.json.gz");
        Save(model, modelFile);
        var restored = Load(modelFile);
        if (!model.Prototypes.Select(item => (item.Key, item.Support)).SequenceEqual(restored.Prototypes.Select(item => (item.Key, item.Support))) ||
            !model.Fragments.Select(item => (item.Pattern.Key, item.Support)).SequenceEqual(restored.Fragments.Select(item => (item.Pattern.Key, item.Support))))
            throw new InvalidOperationException("The learned native model changed during checkpoint transport.");
        var exactKeys = model.Prototypes.Select(item => item.Key).ToHashSet(StringComparer.Ordinal);
        var shapeKeys = model.Prototypes.Select(item => item.ShapeKey).ToHashSet(StringComparer.Ordinal);
        var rows = new List<Row>();
        var replayChecks = 0; var generation = new Dictionary<string, (bool Closed, int Open)>();
        foreach (var descriptor in descriptors.Where(item => item.Split != "train"))
        {
            var timer = Stopwatch.StartNew();
            var sample = C.Materialize(descriptor, includePattern: false);
            var teacher = TeacherPatterns(Read(sample.Target));
            var teacherKeys = teacher.Select(item => item.Key).ToHashSet(StringComparer.Ordinal);
            var teacherShapes = teacher.Select(item => item.ShapeKey).ToHashSet(StringComparer.Ordinal);
            var queries = sample.Streams.Select(stream => M.FromSource(stream.Source)).ToArray();
            var query = M.CombineQueries(queries);
            var ranked = model.Rank(query, model.Prototypes.Count);
            var possible = ranked.Where(item => item.IsAdmissible).ToArray();
            var first = model.Rank(queries[0], model.Prototypes.Count).Where(item => item.IsAdmissible).ToArray();
            var uniform = possible.OrderBy(item => item.Prototype.Key, StringComparer.Ordinal).FirstOrDefault();
            var frequency = possible.OrderByDescending(item => item.Prototype.Support).ThenBy(item => item.Prototype.Key, StringComparer.Ordinal).FirstOrDefault();
            var random = possible.OrderBy(item => StableHash(seed, descriptor.Index, item.Prototype.Key)).FirstOrDefault();
            bool Shape(M.Prediction? prediction) => prediction is not null && teacherShapes.Contains(prediction.Prototype.ShapeKey);
            var top = possible.FirstOrDefault();
            var generated = (Closed: false, Open: 0);
            if (top is not null)
            {
                if (!generation.TryGetValue(top.Prototype.Key, out generated))
                {
                    var actual = M.Realize(top.Prototype);
                    var checkedOutput = Read(actual.Source);
                    generated = (checkedOutput.Reads.Any(boundary => boundary.Find("Closed").Status == "Supported"),
                        checkedOutput.Reads.Sum(boundary => boundary.Claims.Count(claim => claim.Status == "Open")));
                    generation.Add(top.Prototype.Key, generated);
                }
            }
            if (replayChecks < 24)
            {
                var replay = restored.Rank(query, model.Prototypes.Count);
                if (!ranked.Select(Signature).SequenceEqual(replay.Select(Signature)))
                    throw new InvalidOperationException("A native checkpoint changed evidence-conditioned ranking.");
                replayChecks++;
            }
            rows.Add(new(descriptor.Index, descriptor.Group, descriptor.Split, descriptor.Family, descriptor.Edit,
                teacher.Length, teacherKeys.Overlaps(exactKeys), teacherShapes.Overlaps(shapeKeys), E(query.Contradictions).Length != 0,
                queries.Length, possible.Length, first.Length, top is not null && teacherKeys.Contains(top.Prototype.Key),
                Shape(top), possible.Take(3).Any(Shape), Shape(first.FirstOrDefault()), Shape(uniform), Shape(frequency), Shape(random),
                generated.Closed, generated.Open, timer.Elapsed.TotalMilliseconds,
                possible.Any(Shape), possible.Any(prediction => teacherKeys.Contains(prediction.Prototype.Key))));
            manifest.WriteLine(JsonSerializer.Serialize(new { descriptor, teacherKeys, teacherShapes, row = rows[^1] }));
            if (rows.Count % 64 == 0) Console.WriteLine($"evaluate {rows.Count}: {rows.Count(row => row.ShapeTop1)} coarse teacher top choices; {rows.Count(row => row.Admissible == 0)} abstentions; {watch.Elapsed.TotalSeconds:0.0}s");
        }
        manifest.Flush();
        var partitions = rows.GroupBy(row => row.Split).Select(group => Summarize(group.Key, group.ToArray())).ToArray();
        var repairs = RepairProbes(model, output);
        var transfer = TransferProbes(model);
        File.WriteAllText(Path.Combine(output, "transfer.json"), JsonSerializer.Serialize(transfer, Json));
        process.Refresh();
        var summary = new {
            Schema = Protocol, Corpus = C.Version, Count = count, Seed = seed,
            SplitProtocol = familyHoldout ? "whole-family-held-out" : "recipe-edit-group-held-out",
            PartitionIsolation = true, TrainingProvenanceVerified = checkpoint is null,
            FrozenBeforeValidation = true, CheckpointLoaded = checkpoint is not null,
            RequestedTrainRows = descriptors.Count(item => item.Split == "train"), MaterializedTrainRows = trainRows,
            UsableTrainRows = usableTrainRows, TrainBoundaries = trainBoundaries, ModelEpisodes = model.TrainingEpisodes,
            Prototypes = model.Prototypes.Count, CoarseRouteTypes = shapeKeys.Count, Fragments = model.Fragments.Count,
            AuditWitnessesRetained = E(model.TrainingWitnesses).Length, DetailedAuditLimit = 32,
            Partitions = partitions, ReplayChecks = replayChecks, ModelGzipBytes = new FileInfo(modelFile).Length,
            ModelSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(modelFile))).ToLowerInvariant(),
            TrainSeconds = trainedSeconds, TotalSecondsBeforeGallery = watch.Elapsed.TotalSeconds,
            PeakWorkingSetMiB = process.PeakWorkingSet64 / 1048576.0, RepairProbes = repairs,
            TransferProbes = transfer,
            LearningScope = "Native cyclic-route recall and recurrent subroute mining. No shape names supervise memory. No novel whole-route composition, learned edit policy, learned defaults, or learned layout grammar yet.",
            EvidenceScope = "Joint heading/actual-site consistency under one shared turn/reflection; lost calibration, reach extents, figure/ground and source ownership remain explicit unresolved projection. Admissible is not full metric reconstruction.",
            Baselines = "Same admissible learned dictionary: canonical uniform ordering; exposure frequency alone; deterministic random ordering. Multi-stream and first-stream use the same frozen model. Test target agreement is a teacher preference, not correctness of every alternative.",
            Budget = "128 observed route slots; 128 topology faces; 512 joint-binding search states. Search cuts remain Open. Dataset is streamed; at most 32 detailed training audits retained.",
            Machine = new { Environment.ProcessorCount, Runtime = Environment.Version.ToString(), ProcessArchitecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString() }
        };
        File.WriteAllText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(summary, Json));
        File.WriteAllText(Path.Combine(output, "evaluation.jsonl"), string.Join('\n', rows.Select(row => JsonSerializer.Serialize(row))));
        OrnamentLearningGallery.Write(output, summary, SelectExamples(model, descriptors, rows, exampleLimit));
        Console.WriteLine(JsonSerializer.Serialize(new { summary.Prototypes, summary.Fragments, summary.UsableTrainRows, summary.Partitions, summary.ReplayChecks, Seconds = watch.Elapsed.TotalSeconds }, Json));
        Console.WriteLine(Path.GetFullPath(Path.Combine(output, "gallery.html")));
        return 0;
    }

    public static void EnsurePartitionIsolation(IReadOnlyList<C.Descriptor> descriptors)
    {
        if (descriptors.GroupBy(item => item.Group).Any(group => group.Select(item => item.Split).Distinct().Count() != 1))
            throw new InvalidOperationException("Construction siblings leaked across dataset partitions.");
    }

    public static Partition Summarize(string split, IReadOnlyList<Row> rows) => new(split, rows.Count,
        rows.Select(row => row.Group).Distinct().Count(), rows.Count(row => row.TeacherBoundaries > 0),
        rows.Count(row => row.SeenExact), rows.Count(row => row.SeenShape), rows.Count(row => row.Conflicting),
        rows.Count(row => row.Conflicting && row.Admissible == 0), rows.Count(row => row.Admissible == 0),
        rows.Sum(row => row.Admissible), rows.Sum(row => row.FirstStreamAdmissible), rows.Count(row => row.ExactTop1),
        rows.Count(row => row.ShapeTop1), rows.Count(row => row.ShapeTop3), rows.Count(row => row.FirstStreamShapeTop1),
        rows.Count(row => row.UniformShapeTop1), rows.Count(row => row.FrequencyShapeTop1), rows.Count(row => row.RandomShapeTop1),
        rows.Count(row => row.TeacherBoundaries > 0 && !row.SeenShape), rows.Count(row => !row.SeenShape && row.ShapeTop1),
        rows.Count(row => row.GeneratedClosed), rows.Count(row => row.GeneratedClosed && row.GeneratedOpenClaims > 0), rows.Sum(row => row.Milliseconds),
        rows.Count(row => row.TeacherBoundaries > 0 && row.SeenShape && !row.Conflicting),
        rows.Count(row => row.TeacherBoundaries > 0 && row.SeenShape && !row.Conflicting && row.TeacherShapeAdmissible),
        rows.Count(row => row.TeacherBoundaries > 0 && row.SeenShape && !row.Conflicting && row.ShapeTop1),
        rows.Count(row => row.TeacherBoundaries == 1 && row.SeenExact && !row.Conflicting),
        rows.Count(row => row.TeacherBoundaries == 1 && row.SeenExact && !row.Conflicting && row.TeacherExactAdmissible));

    public static M.Model Load(string path)
    {
        using var file = File.OpenRead(path); using var zip = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new StreamReader(zip);
        if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(reader.ReadToEnd()), out var world) ||
            world.Root is not Relation native || !M.Model.TryRead(native, out var model))
            throw new InvalidOperationException("This checkpoint does not expose a readable native learned-route model.");
        return model;
    }

    private static void Save(M.Model model, string path)
    {
        var text = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement(Protocol, model.Surface));
        using var file = File.Create(path); using var zip = new GZipStream(file, CompressionLevel.SmallestSize);
        using var writer = new StreamWriter(zip, new UTF8Encoding(false)); writer.Write(text);
    }

    private static NativeInteriorFeatures Read(NativeLatticeSources.SourceCase source) => new(new NativeInteriorTopology(source, 128));
    private static M.Pattern[] TeacherPatterns(NativeInteriorFeatures features) => features.Reads
        .Where(read => read.Find("Closed").Status == "Supported" && read.SideCountCertain)
        .Select(read => M.Pattern.FromBoundary(read, features.Topology.Source.Graph.Frame)).ToArray();
    private static string Signature(M.Prediction prediction) => prediction.Prototype.Key + ":" + prediction.Score.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" + prediction.IsAdmissible;
    private static ulong StableHash(int seed, int index, string key)
    {
        var hash = 14695981039346656037UL ^ (uint)seed ^ (ulong)(uint)index << 32;
        foreach (var letter in key) hash = (hash ^ letter) * 1099511628211;
        return hash;
    }

    // Human inspection is selected after the model and all metrics are fixed.
    // Cover construction families, edit schedules, conflicts and unseen keys
    // rather than displaying only the first easy rows of the stream.
    private static IReadOnlyList<OrnamentLearningGallery.Example> SelectExamples(M.Model model,
        IReadOnlyList<C.Descriptor> descriptors, IReadOnlyList<Row> rows, int capacity)
    {
        var selected = new List<Row>(); var seen = new HashSet<int>();
        void Add(Func<Row, bool> predicate)
        {
            var row = rows.FirstOrDefault(row => !seen.Contains(row.Index) && predicate(row));
            if (row is not null && selected.Count < capacity) { selected.Add(row); seen.Add(row.Index); }
        }
        Add(row => row.Conflicting); Add(row => row.ShapeTop1); Add(row => row.TeacherBoundaries > 0 && !row.SeenShape);
        Add(row => row.Admissible > 1 && !row.ShapeTop1); Add(row => row.TeacherBoundaries == 0);
        foreach (var family in rows.Select(row => row.Family).Distinct())
            if (!selected.Any(row => row.Family == family)) Add(row => row.Family == family);
        foreach (var edit in rows.Select(row => row.Edit).Distinct())
            if (!selected.Any(row => row.Edit == edit)) Add(row => row.Edit == edit);
        foreach (var group in rows.Select(row => row.Group).Distinct())
            if (!selected.Any(row => row.Group == group)) Add(row => row.Group == group);
        var result = new List<OrnamentLearningGallery.Example>();
        foreach (var row in selected)
        {
            var sample = C.Materialize(descriptors[row.Index]);
            var query = M.CombineQueries(sample.Streams.Select(stream => M.FromSource(stream.Source)));
            var ranked = model.Rank(query, model.Prototypes.Count);
            var proposals = ranked.Where(item => item.IsAdmissible).Take(3).Concat(ranked.Where(item => !item.IsAdmissible).Take(1)).ToArray();
            var top = ranked.FirstOrDefault(item => item.IsAdmissible);
            var note = row.Conflicting ? "Native landmark observations conflict. Frequency must not overrule that evidence." :
                row.TeacherBoundaries == 0 ? "No usable closed teacher boundary. Absence of a training target is not a negative shape label." :
                !row.SeenShape ? "The teacher boundary has an unseen coarse route; retrieval alone cannot invent it." :
                top is null ? "No one learned boundary accommodates these streams together. A multi-part scene can exceed the current one-boundary hypothesis." :
                row.ShapeTop1 ? "A coarse teacher boundary recurs among the learned proposals. Exact dimensions and source construction are separate questions." :
                "An admissible completion differs from the teacher. Partial evidence need not determine the intended boundary.";
            result.Add(new($"L{row.Index:00000}", row.Split, note, sample, proposals, top is null ? null : M.Realize(top.Prototype)));
        }
        return result;
    }

    private static object[] TransferProbes(M.Model model)
    {
        var library = new BourgoinPatternLibrary(); var rows = new List<object>();
        var keys = model.Prototypes.Select(item => item.Key).ToHashSet(StringComparer.Ordinal);
        var shapes = model.Prototypes.Select(item => item.ShapeKey).ToHashSet(StringComparer.Ordinal);
        foreach (var id in new[] { "B01", "B04", "B06.B", "B13", "B17", "B32" })
        {
            var recipe = library.Recipes.Single(item => item.Id == id);
            var phase = (Relation)recipe.Window.Left;
            var window = OrnamentProgram.Window(phase.Left, phase.Right, 2, 2);
            var run = recipe.Program.Generate(recipe.Bindings, window);
            var field = new OrnamentProgramField(run, recipe.SeamContract);
            if (!run.IsComplete || !field.IsComplete) throw new InvalidOperationException("A transfer source did not execute.");
            var raw = new NativeLatticeSources.SourceCase(id, recipe.Name,
                "Untrained existing Bourgoin program; no label or construction history enters the receiver.",
                field.Graph, field.Bindings, field.Graph.Strokes, field.Surface, id != "B04");
            var source = new NativeLinePublication(raw).Published;
            var features = Read(source); var boundaries = TeacherPatterns(features);
            var query = M.FromSource(source);
            var proposed = model.Rank(query, model.Prototypes.Count).Where(item => item.IsAdmissible).ToArray();
            rows.Add(new { Id = id, recipe.Name, recipe.SourceUrl,
                Passages = E(source.Graph.Strokes).Length, DiscoveredBoundaries = features.Reads.Count,
                UsableBoundaries = boundaries.Length, LocalExactDictionaryCoverage = boundaries.Count(item => keys.Contains(item.Key)),
                LocalCoarseDictionaryCoverage = boundaries.Count(item => shapes.Contains(item.ShapeKey)),
                WholeSceneCandidates = proposed.Length, WholeSceneQueries = query.Streams.Sum(stream => stream.Routes.Count),
                ReadScope = "Local coverage uses independent native face discovery; it is not learned scene recognition. Whole-scene candidates still contain only one learned boundary. Neither pass trains the model." });
        }
        return rows.ToArray();
    }

    private static object[] RepairProbes(M.Model model, string? output = null)
    {
        var results = new List<object>(); var proofs = new List<IElement>();
        foreach (var prototype in model.Prototypes.Where(item => item.Pattern.Words.Count is >= 4 and <= 12))
        {
            if (results.Count == 6) break;
            var actual = M.Realize(prototype);
            var paths = E(actual.Unit.Paths).Cast<Relation>().ToArray();
            var first = paths[0]; var chords = E((Relation)first.Right).Cast<Relation>().ToArray();
            var frame = actual.Unit.Frame;
            // Select actual repeated participation in an axis chord. Keeping a
            // before and after portion avoids shifting the whole side into an
            // adjacent corner or immediately retracing its incoming passage.
            var selected = chords.FirstOrDefault(chord =>
            {
                var moves = E(chord).OfType<Relation>().ToArray();
                return moves.Length >= 3 && moves.Length == E(chord).Length &&
                    moves.All(move => ReferenceEquals(move.Left, moves[0].Left));
            });
            if (selected is null) continue;
            var terms = E(selected); var cut = Math.Max(1, terms.Length / 3);
            var middleCount = terms.Length - cut * 2;
            var before = StructuralConstraint.Closed(terms.Take(cut).ToArray());
            var middle = StructuralConstraint.Closed(terms.Skip(cut).Take(middleCount).ToArray());
            var after = StructuralConstraint.Closed(terms.Skip(cut + middleCount).ToArray());
            IElement[] directions = [frame.Right, frame.Up, frame.Left, frame.Down];
            var direction = Array.FindIndex(directions, item => ReferenceEquals(item, ((Relation)terms[0]).Left));
            if (direction < 0) continue;
            // This is an explicit quarter-turn interpretation, the same native
            // model selected by Realize. Winding selects its inward side; no
            // measured angle, midpoint, distance or renderer geometry is used.
            var winding = prototype.Pattern.Headings.Select((heading, index) =>
            {
                var turn = (prototype.Pattern.Headings[(index + 1) % prototype.Pattern.Headings.Count] - heading + 8) % 8;
                return turn > 4 ? turn - 8 : turn;
            }).Sum();
            var inward = directions[(direction + (winding >= 0 ? 1 : 3)) % directions.Length];
            var offset = StructuralConstraint.Closed(QualitativeMotionFrame.Move(inward, ((Relation)terms[cut]).Right));
            var inverse = UnitArrangementRead.Inverse(frame, offset);
            var replacement = StructuralConstraint.Closed(before, offset, middle, inverse, after);
            var refined = actual.Unit.Refine("one interior perpendicular excursion", StructuralConstraint.Closed(new Relation(selected, replacement)));
            var execution = new UnitArrangementNode("learned local edit", [actual.Unit, refined], refined, StructuralConstraint.Closed());
            var joined = execution.Current;
            var source = new NativeLatticeSources.SourceCase("edit", "Local native refinement", "An authored edit policy applied to learned native structure.", joined.Graph, joined.Bindings, joined.Graph.Strokes, joined.Surface, true);
            var features = Read(source);
            var refinedWords = E((Relation)E(refined.Paths).Cast<Relation>().First().Right);
            var cancellation = frame.Reduce(StructuralConstraint.Closed(E(replacement).Cast<Relation>().SelectMany(E).ToArray()),
                UnitArrangementRead.Inverse(frame, selected));
            var endpointsPreserved = frame.TryReadResult(cancellation, out var remaining, out _, out var unknown) &&
                E(remaining).Length == 0 && E(unknown).Length == 0;
            proofs.Add(new Relation(new Relation(actual.Surface, refined.Surface),
                new Relation(execution.Current.Surface, new Relation(cancellation, features.Surface))));
            results.Add(new { prototype.Key, BeforeSides = prototype.Pattern.Words.Count, AfterBoundaries = features.Reads.Count,
                ContainsNotch = features.Reads.Any(read => read.Find("Contains notch").Status == "Supported"),
                Closed = features.Reads.Any(read => read.Find("Closed").Status == "Supported"),
                PreservedParent = refined.Evidence is Relation evidence && ReferenceEquals(evidence.Left, actual.Unit.Surface),
                UnchangedOtherChords = chords.Where(chord => !ReferenceEquals(chord, selected))
                    .All(chord => refinedWords.Any(word => ReferenceEquals(word, chord))),
                NetEndpointsPreserved = endpointsPreserved,
                AfterShapeKeys = TeacherPatterns(features).Select(pattern => pattern.ShapeKey).ToArray() });
        }
        if (output is not null)
        {
            var native = GojoStructureCompactJson.FromElement("ornament-learning-repair-probes-v1", StructuralConstraint.Closed(proofs.ToArray()));
            using var file = File.Create(Path.Combine(output, "repair-probes.gojo.json.gz"));
            using var zip = new GZipStream(file, CompressionLevel.SmallestSize);
            using var writer = new StreamWriter(zip, new UTF8Encoding(false));
            writer.Write(GojoStructureCompactJson.Serialize(native));
        }
        return results.ToArray();
    }

    private static IElement[] E(Relation scope) => StructuralPotential.Items(scope);
}
