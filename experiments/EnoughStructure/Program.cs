using System.Text;
using System.Security.Cryptography;
using Gojo.Core;
using Gojo.Structures;
using Gojo.Experiments.EnoughStructure;

var unit = new Carrier().UnitShape;
var worlds = new[] { Surfaces.CreateDirect(unit: unit), Surfaces.CreateBound(unit: unit), Surfaces.CreateDirect(3, unit), Surfaces.CreateBound(3, unit) };
var labels = new[] { "direct / free", "bound / free", "direct / clamp 3", "bound / clamp 3" };
var roots = new List<IElement>(worlds);
Relation? lastResponse = null;
var report = new StringBuilder("# Enough structure: retained run\n\n");
report.AppendLine("This is a construction and inspection experiment. The runner supplies the pulses and binding proposals. No learning or automatic topology growth is claimed.\n");
report.AppendLine("## Receivers\n\n| Construction | Supplied total | Sender value | Sender tension | Receiver value | Unmet | Occupied answer region | Goal reduction on this step |\n| --- | ---: | ---: | ---: | ---: | ---: | --- | ---: |");
foreach (var world in worlds) roots.Add(Surfaces.Activate(world));
long total = 0;
foreach (var pulse in new long[] { 0, 1, 1, 1, 1, 4 })
{
    total += pulse;
    for (var i = 0; i < worlds.Length; i++)
    {
        var transition = Surfaces.Pulse(worlds[i], pulse); roots.Add(transition);
        if (i == 2) lastResponse = transition;
        var witness = Surfaces.Inspect(Surfaces.Question(worlds[i])); roots.Add(witness);
        Surfaces.TryWorld(worlds[i], out var sp, out var rp, out _, out _, out _);
        Surfaces.TryPort(sp, out var s, out var pressure, out _); Surfaces.TryPort(rp, out var r, out _, out _);
        if (!Surfaces.TryDemand(witness, out var demand) || !Surfaces.TryAnswer(witness, out var answer) ||
            !Surfaces.TryResponse(transition, out _, out _, out var progress) || !Surfaces.Span(answer, out var low, out var high))
            throw new InvalidOperationException("A supplied fixture did not produce a qualified result.");
        report.AppendLine($"| {labels[i]} | {total} | {s.Ticks} | {pressure.Ticks} | {r.Ticks} | {demand.Ticks} | [{low.Ticks},{high.Ticks}] | {progress.Ticks} |");
    }
    foreach (var pair in new[] { (0, 1), (2, 3), (0, 2) })
    {
        var witness = Surfaces.Compare(Surfaces.Inspect(Surfaces.Question(worlds[pair.Item1])),
            Surfaces.Inspect(Surfaces.Question(worlds[pair.Item2])));
        roots.Add(witness);
        if (!Surfaces.TryEquality(witness, out var equal) || !equal) throw new InvalidOperationException("Enough correspondence failed.");
    }
}
var amountComparison = Surfaces.Compare(Surfaces.Inspect(Surfaces.QuantityQuestion(worlds[0])),
    Surfaces.Inspect(Surfaces.QuantityQuestion(worlds[2])));
roots.Add(amountComparison);
Surfaces.TryEquality(amountComparison, out var sameAmount);
report.AppendLine($"\nAt total 8, the amount question agrees: **{sameAmount.ToString().ToLowerInvariant()}**. The enough question still agrees.\n");
report.AppendLine("## Open and failed bindings\n\n| Candidate | Receiver after 4 ticks | Unmet before / after activation | Reduction |\n| --- | ---: | --- | ---: |");
foreach (var kind in new[] { "open", "nonzero target", "reversed", "completed after arrival" })
{
    var world = Surfaces.CreateOpen(3); roots.Add(Surfaces.Pulse(world, 4));
    Surfaces.TryWorld(world, out var sp, out var rp, out _, out _, out _);
    Surfaces.TryPort(sp, out var source, out _, out _); Surfaces.TryPort(rp, out var receiver, out _, out _);
    if (kind == "nonzero target")
        world = Surfaces.ReplaceApplication(world, Surfaces.Direct(new Relation(Relation.Point(source.Carrier, 1), Relation.Point(receiver.Carrier, 1))));
    if (kind == "reversed")
        world = Surfaces.ReplaceApplication(world, Surfaces.Direct(new Relation(Relation.Point(receiver.Carrier, 1), Relation.Point(source.Carrier, 0))));
    if (kind == "completed after arrival")
    {
        Surfaces.Fields(Surfaces.Application(world), 3, out var application);
        var pattern = (Relation)application[0];
        ((Relation)application[1]).TryReadOrderedGroup(out var priorBindings);
        world = Surfaces.ReplaceApplication(world, Surfaces.Bind(pattern, (Relation)priorBindings[0],
            new Relation(pattern.Right, Relation.Point(receiver.Carrier, 0))));
    }
    var activation = Surfaces.Activate(world); roots.Add(world); roots.Add(activation);
    Surfaces.TryResponse(activation, out var before, out var after, out var reduction);
    report.AppendLine($"| {kind} | {receiver.Ticks} | {before.Ticks} / {after.Ticks} | {reduction.Ticks} |");
}
var sample = Surfaces.Inspect(Surfaces.Question(worlds[2]));
report.AppendLine("\n## Calculation inspection\n");
report.AppendLine("Each interval inspection captures its exposed premises, performs one affine projection, checks a domain interval (two one-sided claims), checks the acceptable interval (two more), and publishes membership in a supplied two-region partition. An amount inspection omits the acceptable-interval comparison and answer partition.\n");
report.AppendLine("Each material accounting witness retains seven native equation predictions: four before/after held-amount sums, available supply, accepted receiver change, and accounted total. An equality claim checks available = accepted + post-source-held. Each goal response adds one before-minus-after demand equation. The fixture does not replace these with detached scalar bookkeeping.\n");
report.AppendLine("| Selected root | Reachable carriers | Relations | Tick references | Placeholders | Native bytes |\n| --- | ---: | ---: | ---: | ---: | ---: |");
ReadWitness.TryRead(lastResponse!, out _, out var finalStep, out _, out _, out _);
TimeStepSequence.TryReadStep((Relation)finalStep, out _, out _, out _, out _, out var finalTransition, out _, out _);
Surfaces.Fields(finalTransition, 3, out var transitionParts);
foreach (var entry in new (string Label, Relation Root)[]
{
    ("live limited world", worlds[2]),
    ("enough witness", sample),
    ("amount witness", Surfaces.Inspect(Surfaces.QuantityQuestion(worlds[2]))),
    ("material account", (Relation)transitionParts[2]),
    ("goal response", lastResponse!)
})
{
    var graph = GojoEncodedSnapshot.CaptureGraph(entry.Root);
    report.AppendLine($"| {entry.Label} | {graph.Carriers.Length} | {graph.Relations.Length} | {graph.TickRefs.Length} | {graph.Placeholders.Length} | {GojoNumericFormat.Encode(graph).Length} |");
}
report.AppendLine("\nThese are retained reachable graph counts including physical attachments and unit evidence, not minimal query complexity or a benchmark. The material and goal witnesses retain substantially more evidence than the live construction.\n");
report.AppendLine("Comparisons qualify the question domain, acceptable interval and partition under a selected native 1:1 correspondence before comparing occupied answer regions. Equality is evidence about these observations; the runner's sample campaign is not a learned universal theorem.\n");
report.AppendLine("Current limits: the host schedules arrivals, proposes bindings and requests inspection. Goal response is observational and creates no carrier tension. No autonomous retention, reinforcement, decay, general solver or biological self-maintenance is implemented. Captured exposed numeric premises remain stable; the archive contains the final live worlds plus retained observations, not a full independent physical world per historical step.\n");
var projectDirectory = new DirectoryInfo(AppContext.BaseDirectory);
while (projectDirectory is not null && !File.Exists(Path.Combine(projectDirectory.FullName, "Surfaces.cs")))
    projectDirectory = projectDirectory.Parent;
if (projectDirectory is not null)
{
    report.AppendLine("## Source receipts\n");
    foreach (var file in new[] { "Surfaces.cs", "Program.cs", "EnoughStructure.csproj", "../EnoughStructure.Tests/ConstructionTests.cs" })
    {
        var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(projectDirectory.FullName, file)))).ToLowerInvariant();
        report.AppendLine($"- `{file}`: `{hash}`");
    }
}
var reportText = report.ToString().Replace("\r\n", "\n");
Console.Write(reportText);
if (args.Length != 0)
{
    if (args.Length != 2 || args[0] != "--output") throw new ArgumentException("Usage: --output <new directory>");
    var directory = Path.GetFullPath(args[1]);
    if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
        throw new IOException("Use a new empty output directory to preserve earlier runs.");
    Directory.CreateDirectory(directory);
    File.WriteAllText(Path.Combine(directory, "report.md"), reportText);
    File.WriteAllBytes(Path.Combine(directory, "experiment.gojo"), GojoNumericFormat.Encode(Surfaces.Group(roots.ToArray())));
}
