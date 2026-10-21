using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using P = Gojo.Experiments.QualitativeFrieze.StructuralPotential;

namespace Gojo.Experiments.QualitativeFrieze;

public static class NativeLatticeExperiment
{
    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "native-lattice-evidence");
        var capacity = 32;
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else if (args[i] == "--candidates" && i + 1 < args.Length && int.TryParse(args[++i], out var selected) && selected is >= 0 and <= 256) capacity = selected;
            else throw new ArgumentException("Usage: --native-lattice-evidence [--output DIRECTORY] [--candidates 0..256]");
        Directory.CreateDirectory(output);
        var watch = Stopwatch.StartNew();
        var suppliers = NativeLatticeSources.Cases().Select(source => new NativeLineSupplier(source, capacity)).ToArray();
        var cases = new List<object>(); var archive = new List<IElement>(); var checks = 0; var reads = 0;
        foreach (var supplier in suppliers)
        {
            var restored = Restore(supplier.Port);
            if (!NativeLineSupplier.TryReadPort(restored, out var imported, out var importProof, capacity) ||
                supplier.Candidates.Count != imported.Candidates.Count)
                throw new InvalidOperationException("An exposed native source failed independent import.");
            archive.Add(new Relation(supplier.Port, importProof));
            var outcomes = new Dictionary<string, Dictionary<string, int>>(); var requests = new List<int>();
            for (var c = 0; c < supplier.Candidates.Count; c++)
            {
                var candidate = supplier.Candidates[c]; var counterpart = imported.Candidates[c];
                for (var q = 0; q < candidate.Frame.Goals.Count; q++)
                {
                    var goal = candidate.Frame.Goals[q]; var read = candidate.Read(goal.Predicate);
                    var transported = counterpart.Read(counterpart.Frame.Goals[q].Predicate); reads++; checks++;
                    if (transported.Answer != read.Answer || P.Items(transported.ServiceRequests).Length != P.Items(read.ServiceRequests).Length)
                        throw new InvalidOperationException("A derived source read changed after public-port transport.");
                    if (!outcomes.TryGetValue(goal.Name, out var counts)) outcomes.Add(goal.Name, counts = []);
                    counts[read.Answer] = counts.GetValueOrDefault(read.Answer) + 1;
                    requests.Add(P.Items(read.ServiceRequests).Length);
                    // Keep representative native reads, not every repeated
                    // diagnostic comparison. All source ports remain replayable.
                    if (c == 0) archive.Add(new Relation(candidate.Surface, read.Surface));
                }
            }
            if (supplier.Candidates.FirstOrDefault() is { } first)
            {
                var outcome = first.Complete(first.Frame.FiniteBoundary, P.S(first.Frame.SpanCoverage.Cast<IElement>().ToArray()));
                if (outcome.Completion?.Sample is { } sample && P.Items(sample.CycleWitness).Length != 1)
                    throw new InvalidOperationException("An emitted boundary lacks its independent cycle read.");
                archive.Add(outcome.Surface);
            }
            cases.Add(new { supplier.Source.Id, supplier.Source.Name,
                OriginalStrokes = P.Items(supplier.Source.Graph.Strokes).Length,
                SupportStrokes = P.Items(supplier.Support.Graph.Strokes).Length,
                Candidates = supplier.Candidates.Count, UnsearchedSelections = P.Items(supplier.Unsearched).Length,
                UnsupportedPassages = P.Items(supplier.Unsupported).Length, Outcomes = outcomes,
                ServiceRequests = requests.Sum(), MaximumRequestsPerRead = requests.Count == 0 ? 0 : requests.Max() });
        }
        NativeLatticeGallery.Write(output, suppliers);
        File.WriteAllText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(new {
            Cases = suppliers.Length, CandidateCapacity = capacity, SourceReads = reads, TransportChecks = checks,
            CasesWithCuts = suppliers.Count(supplier => P.Items(supplier.Unsearched).Length != 0), Results = cases,
            SecondsBeforeArchive = watch.Elapsed.TotalSeconds }, new JsonSerializerOptions { WriteIndented = true }));
        var packet = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("native-lattice-exposed-ports-and-read-witnesses-v1", P.S(archive.ToArray())));
        using (var file = File.Create(Path.Combine(output, "sources.gojo.json.gz")))
        using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(packet);
        Console.WriteLine($"Completed {suppliers.Length} native sources, {reads} question reads and {checks} public-port transport comparisons.");
        Console.WriteLine($"Elapsed {watch.Elapsed.TotalSeconds:0.00}s. {Path.GetFullPath(output)}");
        return 0;
    }

    private static Relation Restore(Relation source)
    {
        var packet = GojoStructureCompactJson.FromElement("native-lattice-public-port", source);
        if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(GojoStructureCompactJson.Serialize(packet)), out var world) || world.Root is not Relation restored)
            throw new InvalidOperationException("A source word port failed transport.");
        return restored;
    }
}
