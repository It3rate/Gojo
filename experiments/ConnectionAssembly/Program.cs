using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using N = ConnectionAssembly.AssemblyNetwork;

namespace ConnectionAssembly;

internal static class Program
{
    // These names are solely display labels for the six declared constructors.
    private static readonly string[] Names = ["Sustained rise", "Sustained fall", "Flat", "Jump", "Reversal", "Scale change"];

    private static int Main(string[] args)
    {
        var checks = Checks.Run();
        Console.WriteLine($"Connection checks passed: {checks.Length}");
        if (args.Contains("--checks-only")) return 0;
        var output = Path.GetFullPath(args.FirstOrDefault(a => !a.StartsWith("--")) ??
            "experiments/ConnectionAssembly/results-dynamic");
        Directory.CreateDirectory(output);
        var report = new StringBuilder("# Connection assembly: reusable dynamic equations\n\n");
        report.AppendLine("These are local recognition and help results, not forecasts of future turns. The six readers receive identical raw inputs. Names and phase boundaries exist only in the environment and observer. A new probe is available on the next arrival; current receipts use the previous gates.\n");
        report.AppendLine("## Focused checks\n");
        foreach (var check in checks) report.AppendLine($"- {check}");
        report.AppendLine("\n## Fixed trial measurements\n");
        report.AppendLine("| Run / phase | Complete arrivals | Raw recognitions: rise / fall / flat / jump / reverse / scale | Requests / served / unmet | Probes / useful receipts / releases | Gates at phase end |");
        report.AppendLine("| --- | --- | --- | --- | --- | --- |");
        var costs = new List<string>();
        var boundaryNotes = new StringBuilder();
        foreach (var (name, samples) in TrialInputs.All())
        {
            var root = N.Create();
            var rawPorts = N.Nodes(root).Select(N.Raw).ToArray();
            var requestPorts = N.Nodes(root).Select(N.Request).ToArray();
            var frames = new List<Frame>();
            long allocated = 0;
            var elapsed = new Stopwatch();
            var sizes = new List<(int Step, int Carriers, int Relations, int Attachments)>();
            var csv = new StringBuilder("step,phase,value,complete,requests,served,unmet,probes,useful,releases,gates,allocated_bytes\n");
            for (var index = 0; index < samples.Length; index++)
            {
                var (phase, value) = samples[index];
                var before = GC.GetAllocatedBytesForCurrentThread();
                elapsed.Start();
                var admitted = N.TryObserve(root, index + 1, value, out var next, out var witness);
                elapsed.Stop();
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                allocated += bytes;
                if (!admitted) throw new InvalidOperationException($"Public input rejected: {name}/{index + 1}");
                root = next;
                var nodes = N.NodeSamples(witness).Select((s, i) => new NodeFrame(
                    rawPorts[i].Id.ToString(CultureInfo.InvariantCulture),
                    N.SampleRaw(s), N.SampleRequest(s), N.SampleHelp(s), N.SamplePressure(s),
                    N.SampleEngagement(s), N.SampleAccounted(s))).ToArray();
                var links = N.LinkSamples(witness).Select(s => new LinkFrame(
                    Array.FindIndex(rawPorts, p => p.SameIdentityAs(N.SampleProducer(s))),
                    Array.FindIndex(requestPorts, p => p.SameIdentityAs(N.SampleConsumer(s))),
                    N.SampleSupport(s), N.SampleGate(s), N.SampleReceipt(s),
                    N.WasProposed(s), N.WasUseful(s), N.WasReleased(s)))
                    .Where(l => l.Support != 0 || l.Receipt != 0 || l.Proposed || l.Released).ToArray();
                if (links.Any(l => l.From < 0 || l.To < 0)) throw new InvalidOperationException("Unbound observed port identity.");
                int? carrierCount = null, relationCount = null, attachedCount = null;
                if ((index + 1) % 240 == 0)
                {
                    var graph = GojoEncodedSnapshot.CaptureGraph(root);
                    carrierCount = graph.Carriers.Length;
                    relationCount = graph.Relations.Length;
                    var attached = graph.Carriers.SelectMany(c => c.AttachedRelationIndexes ?? []).Distinct().Count();
                    attachedCount = attached;
                    sizes.Add((index + 1, carrierCount.Value, relationCount.Value, attached));
                }
                var frame = new Frame(index + 1, phase, value, N.Complete(witness), nodes, links, relationCount, carrierCount, attachedCount);
                frames.Add(frame);
                csv.AppendLine(string.Join(',', index + 1, phase, value, frame.Complete ? 1 : 0,
                    nodes.Sum(n => n.Request), nodes.Count(n => n.Request > 0 && n.Pressure == 0),
                    nodes.Sum(n => n.Pressure), links.Count(l => l.Proposed), links.Count(l => l.Useful),
                    links.Count(l => l.Released), links.Count(l => l.Gate > 0), bytes));
            }
            foreach (var phase in frames.GroupBy(f => f.Phase))
            {
                var phaseFrames = phase.ToArray();
                var raw = Enumerable.Range(0, Names.Length).Select(i => phaseFrames.Sum(f => f.Nodes[i].Raw));
                var nodeRows = phaseFrames.SelectMany(f => f.Nodes).ToArray();
                var linkRows = phaseFrames.SelectMany(f => f.Links).ToArray();
                report.AppendLine($"| {name} / {phase.Key} | {phaseFrames.Count(f => f.Complete)} | {string.Join(" / ", raw)} | {nodeRows.Sum(n => n.Request)} / {nodeRows.Count(n => n.Request > 0 && n.Pressure == 0)} / {nodeRows.Sum(n => n.Pressure)} | {linkRows.Count(l => l.Proposed)} / {linkRows.Count(l => l.Useful)} / {linkRows.Count(l => l.Released)} | {phaseFrames[^1].Links.Count(l => l.Gate > 0)} |");
                var end = phaseFrames[^1];
                var tail = phaseFrames.TakeLast(64).ToArray();
                boundaryNotes.AppendLine($"### {name}: {phase.Key}\n");
                boundaryNotes.AppendLine($"End engagement, rise / fall / flat / jump / reverse / scale: {string.Join(" / ", end.Nodes.Select(n => n.Engagement))}.\n");
                boundaryNotes.AppendLine($"Complete arrivals with at least one raw recognizer: {phaseFrames.Count(f => f.Complete && f.Nodes.Any(n => n.Raw > 0))} / {phaseFrames.Count(f => f.Complete)}. Multiple raw recognizers: {phaseFrames.Count(f => f.Complete && f.Nodes.Count(n => n.Raw > 0) > 1)}. These measure the supplied feature coverage, not prediction or learned composition.\n");
                boundaryNotes.AppendLine("| Open connection at phase end | Support | Receipts in last 64 arrivals | Individually useful receipts in last 64 |\n| --- | --- | --- | --- |");
                foreach (var link in end.Links.Where(l => l.Gate > 0))
                {
                    var recent = tail.SelectMany(f => f.Links).Where(l => l.From == link.From && l.To == link.To).ToArray();
                    boundaryNotes.AppendLine($"| {Names[link.From]} → {Names[link.To]} | {link.Support} | {recent.Sum(l => l.Receipt)} | {recent.Count(l => l.Useful)} |");
                }
                boundaryNotes.AppendLine();
            }
            var json = JsonSerializer.Serialize(new Replay(name, Names, frames.ToArray()),
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            File.WriteAllText(Path.Combine(output, name + ".json"), json);
            File.WriteAllText(Path.Combine(output, name + ".csv"), csv.ToString());
            File.WriteAllBytes(Path.Combine(output, name + ".gojo"), GojoNumericFormat.Encode(root));
            costs.Add($"| {name} | {samples.Length} | {allocated:N0} | {elapsed.Elapsed.TotalSeconds:F3} | {string.Join("; ", sizes.Select(s => $"{s.Step}: {s.Carriers}/{s.Relations}/{s.Attachments}"))} |");
            Console.WriteLine($"{name}: {samples.Length} arrivals, {frames.Sum(f => f.Links.Count(l => l.Proposed))} probes, {frames.Sum(f => f.Links.Count(l => l.Released))} releases, {allocated:N0} allocated bytes");
        }
        report.AppendLine("\nRequests are from engaged readers whose own current interpretation failed. Served means at least one previously connected helper accounted for this arrival. It does not make the recipient's original interpretation true. Useful receipts are stricter: removing that one receipt would leave the request unanswered. Simultaneous redundant help does not earn duplicate support.\n");
        report.AppendLine("## Connections remaining at each boundary\n");
        report.Append(boundaryNotes);
        report.AppendLine("## Cost and retained structure\n");
        report.AppendLine("| Run | Arrivals | Allocated bytes during admission | Admission seconds | Step: carriers / relations / distinct attached relations |");
        report.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (var cost in costs) report.AppendLine(cost);
        report.AppendLine("\nThese are one-run diagnostics, not controlled performance benchmarks. Admission excludes construction, generators, graph capture, serialization and rendering. Retained graphs include reusable operational equations, the latest selected witness and all finite connection slots; they are not the complete observer history. Numeric samples are frozen; references to the operational graph remain live. The native .gojo files save only each final world. Replay JSON and CSV are observer artifacts and never feed the learner.\n");
        report.AppendLine("## Interpretation limits\n\nThe six feature scopes, finite 30-slot neighborhood, scan order, support grants, cap and fade interval are supplied. No waveform name or future value is an input. This demonstrates native recognition and executable connection participation, not discovery of new primitives, exact timing prediction, new higher-order concepts, or genetic search. A larger-step zigzag deliberately challenges the fixed jump boundary. Shuffling preserves the pulse-increment multiset while changing order; similar recognition/help results expose the limited temporal model. Supported publications now physically attach; expired ones detach after the current pulses clear. The finite dormant relation templates remain available for later reattachment. The broad generic-help and elapsed-arrival fade policies are intentionally unchanged from the first probe, to isolate equation reuse and the selected evidence contract. The new consumer-specific interval-response read has separate focused coverage; it has not been substituted into these consumers.\n");
        File.WriteAllText(Path.Combine(output, "first-run.md"), report.ToString().TrimEnd() + Environment.NewLine);
        return 0;
    }

    // Reporting projections only. These never own learner meaning or feed it.
    private sealed record Replay(string Title, string[] NodeNames, Frame[] Frames);
    private sealed record Frame(int Step, string Phase, long? Value, bool Complete, NodeFrame[] Nodes,
        LinkFrame[] Links, int? NativeRelations, int? NativeCarriers, int? NativeAttachedRelations);
    private sealed record NodeFrame(string Id, long Raw, long Request, long Help, long Pressure, long Engagement, bool Accounted);
    private sealed record LinkFrame(int From, int To, long Support, long Gate, long Receipt, bool Proposed, bool Useful, bool Released);
}
