using System.Globalization;
using System.Text;
using Gojo.Core;
using Gojo.Structures;
using C = BreakDrivenPrediction.Continuation;
using E = BreakDrivenPrediction.EventCoupling;
using S = BreakDrivenPrediction.EventSource;

namespace BreakDrivenPrediction;

internal static class EventTrials
{
    // Environment state, labels and all report counters are host apparatus.
    // Neither structure can access the driver, random generators or future input.
    public static void Run(string directory)
    {
        var output = Path.GetFullPath(directory);
        Directory.CreateDirectory(output);
        var csv = new StringBuilder("condition,seed,arrival,actual_delta,baseline,shadow,export,baseline_correct,export_correct,event_read,shadow_better,shadow_worse,support,demand,connected,event_used\n");
        var report = new StringBuilder("# Independent event supplier: first results\n\n");
        report.AppendLine("Each producer reading precedes the future target it may help predict. All 240 arrivals per trial are scored, including startup. The same native reversal reader and support policy are used throughout.\n");
        report.AppendLine("| Condition | Seed | Correct direction: existing / combined | Shadow better / worse / tied | Charged examinations | Fresh complete readings | Link additions / withdrawals | Final support | Mean magnitude error |");
        report.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        var graphNotes = new List<string>();
        string[] conditions = ["leading-irregular", "lagging-irregular", "unrelated",
            "relationship-change", "dropouts", "demand-pause", "leading-regular"];
        foreach (var condition in conditions)
        foreach (var seed in new[] { 113, 907, 2029 })
        {
            var random = new Random(seed);
            var unrelatedRandom = new Random(seed + 10007);
            long driver = 1000, unrelated = 1000, target = 1000;
            long driverDelta = 2, unrelatedDelta = 2;
            var remaining = condition == "leading-regular" ? 3 : random.Next(3, 10);
            var unrelatedRemaining = unrelatedRandom.Next(3, 10);
            var source = S.Create(driver);
            var receiver = E.Create(C.Create(target), S.Port(source));
            var packet = S.Packet(source);
            int baseCorrect = 0, exportCorrect = 0, better = 0, worse = 0, ties = 0,
                costs = 0, covered = 0, added = 0, removed = 0;
            long magnitude = 0;
            var sizes = new List<string>();
            for (var t = 0; t < 240; t++)
            {
                var wanted = condition == "demand-pause" && t >= 80 && t < 160 ? 0 : 1;
                E.Demand(receiver).Set(wanted);
                IElement delivered = condition == "dropouts" && t % 11 < 3 ? new Placeholder() : packet;
                var issue = E.Issue(receiver, delivered);
                var f = C.Parts(issue);
                var receipt = C.Parts((Relation)f[1]);
                if (receipt[4] is CarrierLocation) covered++;
                var beforeConnection = E.Participates(receiver);
                var eventUsed = beforeConnection && wanted > 0 && f[3] is CarrierLocation;

                // The already-observed current driver change causes the next
                // target change. No future driver sample is needed here.
                target += driverDelta;
                if (!E.TryObserve(receiver, issue, new CarrierLocation(C.Sensor(E.Base(receiver)), target),
                        out receiver, out var assessment))
                    throw new InvalidOperationException("An event trial target was not admitted.");
                var a = C.Parts(assessment);
                var basePass = C.Pass((Relation)a[2]);
                var exportPass = C.Pass((Relation)a[4]);
                if (basePass) baseCorrect++;
                if (exportPass) exportCorrect++;
                var shadowBetter = false;
                var shadowWorse = false;
                if (a[3] is Relation claim)
                {
                    costs++;
                    var shadowPass = C.Pass(claim);
                    shadowBetter = shadowPass && !basePass;
                    shadowWorse = !shadowPass && basePass;
                    if (shadowBetter) better++;
                    else if (shadowWorse) worse++;
                    else ties++;
                }
                if (!OperationClaim.TryReadResolvedViolation((Relation)a[5], out var error))
                    throw new InvalidOperationException("An unresolved magnitude is not zero error.");
                magnitude += error.Ticks;
                var afterConnection = E.Participates(receiver);
                if (!beforeConnection && afterConnection) added++;
                if (beforeConnection && !afterConnection) removed++;
                var shadow = f[3] is CarrierLocation candidate ? candidate.Ticks.ToString(CultureInfo.InvariantCulture) : "";
                var reading = receipt[4] is CarrierLocation bit ? bit.Ticks.ToString(CultureInfo.InvariantCulture) : "open";
                csv.AppendLine($"{condition},{seed},{t + 1},{((CarrierLocation)a[10]).Ticks},{((CarrierLocation)f[2]).Ticks},{shadow},{((CarrierLocation)f[4]).Ticks},{(basePass ? 1 : 0)},{(exportPass ? 1 : 0)},{reading},{(shadowBetter ? 1 : 0)},{(shadowWorse ? 1 : 0)},{E.Credit(receiver).Value},{wanted},{(afterConnection ? 1 : 0)},{(eventUsed ? 1 : 0)}");

                // Advance the two external source processes only after this
                // outcome. The producer sees just one new numeric observation.
                var previousDriver = driver;
                remaining--;
                if (remaining == 0)
                {
                    driverDelta = -driverDelta;
                    remaining = condition == "leading-regular" ? (driverDelta > 0 ? 3 : 5) : random.Next(3, 10);
                }
                driver += driverDelta;
                unrelatedRemaining--;
                if (unrelatedRemaining == 0)
                {
                    unrelatedDelta = -unrelatedDelta;
                    unrelatedRemaining = unrelatedRandom.Next(3, 10);
                }
                unrelated += unrelatedDelta;
                var input = condition == "unrelated" ? unrelated :
                    condition == "lagging-irregular" || (condition == "relationship-change" && t + 1 >= 120)
                        ? previousDriver : driver;
                if (!S.TryAdvance(source, new CarrierLocation(S.Sensor(source), input), out source, out packet))
                    throw new InvalidOperationException("A source trial observation remained unresolved.");
                if ((t + 1) % 80 == 0)
                {
                    static string Size(params IElement[] roots)
                    {
                        var graph = GojoEncodedSnapshot.CaptureGraph(roots);
                        return $"{graph.Carriers.Length}/{graph.Relations.Length}/{graph.TickRefs.Length}/{graph.CarrierLocations.Length}";
                    }
                    sizes.Add($"{t + 1}: source {Size(source)}, consumer {Size(receiver)}, joint {Size(source, receiver)}");
                }
            }
            report.AppendLine($"| {condition} | {seed} | {baseCorrect}/240 / {exportCorrect}/240 | {better} / {worse} / {ties} | {costs} | {covered}/240 | {added} / {removed} | {E.Credit(receiver).Value} | {((double)magnitude / 240).ToString("F2", CultureInfo.InvariantCulture)} |");
            graphNotes.Add($"- {condition}, {seed}: {string.Join("; ", sizes)}");
            Console.WriteLine($"{condition}/{seed}: {baseCorrect}->{exportCorrect}, shadow +{better}/-{worse}/={ties}, links +{added}/-{removed}, support {E.Credit(receiver).Value}");
        }
        report.AppendLine("\nExisting predictions include the base structure's internal timing connections. Shadow gains alone are not exported improvements. A tie can mean the existing structure already predicted the turn. Magnitude error deliberately ignores direction; an inverted prediction may preserve scale. Charged examinations count shadow opportunities after recruitment, not every published event. Each costs one supplied utility tick, not measured runtime or conserved input energy.\n");
        report.AppendLine("A fresh packet from a lagging physical process is graded normally. Missing publications remain open and earn neither event reward nor event cost. The demand-pause condition requests no event-assisted output during arrivals 81–160, then restores demand.\n");
        report.AppendLine("The CSV records connection presence after assessment separately from event use in the issued forecast. Demand can disable export before the connection withdraws. The three leading-regular rows repeat the same deterministic control; they are not independent trials.\n");
        report.AppendLine("Dropouts suppress delivery, while the producer continues observing. Switching to the lagging process introduces one held source level as that delay begins. The reversal predicate and reversal response are supplied structures; these trials do not discover either family.\n");
        report.AppendLine("Reachable graph samples, observation count: carriers/relations/tick sites/locations for each source, consumer and their joint world. These exclude ephemeral read allocations.\n");
        report.AppendLine(string.Join("\n", graphNotes));
        File.WriteAllText(Path.Combine(output, "first-run.md"), report.ToString());
        File.WriteAllText(Path.Combine(output, "traces.csv"), csv.ToString());
    }
}
