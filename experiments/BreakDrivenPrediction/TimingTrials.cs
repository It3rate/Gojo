using System.Globalization;
using System.Text;
using Gojo.Core;
using Gojo.Structures;
using C = BreakDrivenPrediction.Continuation;
using L = BreakDrivenPrediction.TimingLadder;
using S = BreakDrivenPrediction.TimingSource;
using T = BreakDrivenPrediction.TimingPrediction;

namespace BreakDrivenPrediction;

internal static class TimingTrials
{
    // Environment generation, counters, labels and file formatting are host
    // apparatus. Native structures receive only current observations or public
    // reports. The apparatus never supplies a cadence, label or future event.
    public static void Run(string directory)
    {
        var output = Path.GetFullPath(directory);
        Directory.CreateDirectory(output);
        var csv = new StringBuilder("condition,seed,source,availability,endpoint,acquisitions,reports,coverage_start,coverage_end,coverage_complete,public_collection_open,known_occurrences,occurrence_times,prior_issued_at,prior_predicted_at,frozen_demand,new_issued_at,new_predicted_at,new_forecast,settled,demanded_match,unassessable,absence_miss,hit_width1,hit_width3,hit_width12,graded_event_at,signed_event_minus_prediction,native_verdict_violation,support_before,support_after,forecast_cell_rows,assessment_rows\n");
        var sizes = new StringBuilder("condition,seed,source,prefix,structure,carriers,relations,tick_refs,locations,unit_shapes,placeholders\n");
        var report = new StringBuilder("# Shared timing ladder: first results\n\n");
        report.AppendLine("The fixed protocol ran 360 base ticks per condition and seed. Two independent sources received the same environment: fine acquisition every tick and sparse acquisition every twelve ticks, both reporting every twelve ticks. Each independent learner used the same native cadence construction, demanded width 3, and diagnostic widths 1, 3 and 12.\n");
        report.AppendLine("| Condition | Seed | Acquisition | Acquisitions / reports | Known events / incomplete reports | Issued / settled / unassessable / pending | Hits: width 1 / 3 / 12 | Final support | Forecast cells / assessment rows |");
        report.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        string[] conditions = ["regular", "jitter", "drift", "stopped"];
        string[] sourceNames = ["fine", "sparse"];
        int[] strides = [1, 12];
        foreach (var condition in conditions)
        foreach (var seed in new[] { 113, 907, 2029 })
        {
            var random = new Random(seed);
            // Each run's duration is selected only by the external environment.
            // A drift boundary changes the next run, not a partly completed run.
            int RunLength(int beginsAt) => condition switch
            {
                "jitter" => random.Next(4, 7),
                "drift" => 5 + Math.Min((beginsAt - 1) / 90, 3),
                _ => 5
            };
            long level = 1000;
            var direction = 1;
            var remaining = RunLength(1);
            Carrier[] publicClocks = [new(), new()];
            Relation[] sources = [S.Create(level, publicClocks[0], strides[0]),
                S.Create(level, publicClocks[1], strides[1])];
            Relation[] learners = [T.Create(S.Port(sources[0])), T.Create(S.Port(sources[1]))];
            var acquisitions = new int[2];
            var reports = new int[2];
            var knownEvents = new int[2];
            var incompleteReports = new int[2];
            var issued = new int[2];
            var settled = new int[2];
            var unassessable = new int[2];
            var hits = new int[2, 3];
            var projectedRows = new int[2];
            var assessedRows = new int[2];
            for (var tick = 1; tick <= 360; tick++)
            {
                if (condition != "stopped" || tick <= 180)
                {
                    level += direction;
                    remaining--;
                    if (remaining == 0)
                    {
                        direction = -direction;
                        remaining = RunLength(tick + 1);
                    }
                }
                for (var stream = 0; stream < sources.Length; stream++)
                {
                    if (tick % strides[stream] == 0)
                    {
                        if (!S.TryObserve(sources[stream], new CarrierLocation(publicClocks[stream], tick),
                                new CarrierLocation(S.Sensor(sources[stream]), level), out var observed))
                            throw new InvalidOperationException($"Timing source rejected {condition}/{seed}/{sourceNames[stream]} at {tick}.");
                        sources[stream] = observed;
                        acquisitions[stream]++;
                    }
                    if (tick % 12 != 0) continue;
                    if (!S.Publish(sources[stream], new CarrierLocation(publicClocks[stream], tick),
                            out var published, out var packet))
                        throw new InvalidOperationException("A scheduled timing report remained unresolved.");
                    sources[stream] = published;
                    reports[stream]++;
                    var publicFields = C.Parts(packet);
                    var openCollection = publicFields[3] is Placeholder;
                    var occurrences = publicFields[3] is Relation collection && collection.TryReadOrderedGroup(out var contents)
                        ? contents : [];
                    var complete = !openCollection && publicFields[5] is Relation residue &&
                        residue.TryReadOrderedGroup(out var excluded) && excluded.Length == 0;
                    knownEvents[stream] += occurrences.Length;
                    if (!complete) incompleteReports[stream]++;
                    var occurrenceTimes = occurrences.Select(OccurrenceTime).ToArray();
                    if (!TimeStepSequence.TryReadSpan((Relation)publicFields[1], out var coveredFrom, out var coveredTo))
                        throw new InvalidOperationException("A timing report has no directed coverage.");

                    var before = T.Pending(learners[stream]);
                    var priorValid = TryPending(before, out var priorIssue, out var priorFields);
                    // The only source input received by this learner is packet.
                    if (!T.TryObserve(learners[stream], packet, out var learned, out var assessment))
                        throw new InvalidOperationException("A public timing report was not admitted by its learner.");
                    learners[stream] = learned;
                    var a = C.Parts(assessment);
                    var after = T.Pending(learned);
                    var afterValid = TryPending(after, out var nextIssue, out _);
                    var fresh = afterValid && !ReferenceEquals(before, after);
                    var newRows = fresh ? L.Rungs(nextIssue).Length : 0;
                    if (fresh) { issued[stream]++; projectedRows[stream] += newRows; }
                    var wasSettled = T.Settled(assessment);
                    var outcome = a[1] is Relation outcomeGroup ? C.Parts(outcomeGroup) : [];
                    var wasUnassessable = priorValid && !wasSettled && after is Placeholder &&
                        outcome.Length == 4 && outcome[2] is Placeholder;
                    if (wasSettled) settled[stream]++;
                    if (wasUnassessable) unassessable[stream]++;

                    // Read the already-performed ladder assessment. Do not grade
                    // again from the generator's hidden direction or turn schedule.
                    var matched = new bool?[3];
                    CarrierLocation? gradedEvent = null, signedError = null, verdictViolation = null;
                    var absence = false;
                    var readRows = 0;
                    if (wasSettled)
                    {
                        if (!OperationClaim.TryReadResolvedViolation((Relation)outcome[2], out var nativeVerdict))
                            throw new InvalidOperationException("A settled forecast has no native verdict.");
                        verdictViolation = nativeVerdict;
                        var evidence = outcome[0] is Relation nativeRead ? C.Parts(nativeRead) : [];
                        if (evidence.Length == 3 && evidence[1] is CarrierLocation actual && evidence[2] is Relation rowGroup)
                        {
                            var rows = C.Parts(rowGroup);
                            if (rows.Length != 3) throw new InvalidOperationException("The fixed timing ladder lost a rung.");
                            gradedEvent = actual;
                            for (var rung = 0; rung < rows.Length; rung++)
                            {
                                if (rows[rung] is not Relation row || !L.TryDifference(row, out var error))
                                    throw new InvalidOperationException("A timing row has no native signed difference.");
                                if (C.Parts(row).Skip(1).Take(3).Any(element => element is not Relation claim ||
                                        !OperationClaim.TryReadResolvedViolation(claim, out _)))
                                    throw new InvalidOperationException("An unresolved rung claim is not a timing miss.");
                                matched[rung] = L.Matches(row);
                                if (matched[rung] == true) hits[stream, rung]++;
                                if (rung == 0) signedError = error;
                                else if (signedError is not CarrierLocation firstError || firstError.Ticks != error.Ticks)
                                    throw new InvalidOperationException("Shared forecast evidence produced contradictory signed errors.");
                            }
                            readRows = rows.Length;
                            assessedRows[stream] += readRows;
                        }
                        else
                        {
                            // The source certified a complete interval with no
                            // event until every cell closed. No invented event
                            // time or signed error is assigned to this absence.
                            if (T.Successful(assessment))
                                throw new InvalidOperationException("An absence cannot satisfy this occurrence promise.");
                            absence = true;
                            matched = [false, false, false];
                        }
                    }
                    var prior = priorValid ? C.Parts(priorIssue) : [];
                    var future = fresh ? C.Parts(nextIssue) : [];
                    csv.AppendLine(string.Join(",", new[]
                    {
                        condition, Text(seed), sourceNames[stream], Text(((CarrierLocation)publicFields[2]).Ticks),
                        Text(((CarrierLocation)publicFields[4]).Ticks), Text(acquisitions[stream]), Text(reports[stream]),
                        Text(coveredFrom.Ticks), Text(coveredTo.Ticks), Bit(complete), Bit(openCollection),
                        openCollection ? "" : Text(occurrences.Length), string.Join(";", occurrenceTimes.Select(Text)),
                        priorValid ? Text(((CarrierLocation)prior[0]).Ticks) : "",
                        priorValid ? Text(((CarrierLocation)prior[1]).Ticks) : "",
                        priorValid ? Text(((CarrierLocation)priorFields[1]).Ticks) : "",
                        fresh ? Text(((CarrierLocation)future[0]).Ticks) : "",
                        fresh ? Text(((CarrierLocation)future[1]).Ticks) : "", Bit(fresh), Bit(wasSettled),
                        wasSettled ? Bit(T.Successful(assessment)) : "",
                        Bit(wasUnassessable), Bit(absence), Bit(matched[0]), Bit(matched[1]), Bit(matched[2]),
                        Coordinate(gradedEvent), Coordinate(signedError), Coordinate(verdictViolation),
                        Text(((CarrierLocation)a[2]).Ticks), Text(((CarrierLocation)a[3]).Ticks), Text(newRows), Text(readRows)
                    }));

                    if (tick % 120 == 0)
                    {
                        AppendSize(sizes, condition, seed, sourceNames[stream], tick, "source", sources[stream]);
                        AppendSize(sizes, condition, seed, sourceNames[stream], tick, "learner", learners[stream]);
                    }
                }
            }
            for (var stream = 0; stream < sources.Length; stream++)
            {
                var pending = TryPending(T.Pending(learners[stream]), out _, out _) ? 1 : 0;
                var support = T.Support(learners[stream]).Value;
                report.AppendLine($"| {condition} | {seed} | {sourceNames[stream]} | {acquisitions[stream]} / {reports[stream]} | {knownEvents[stream]} / {incompleteReports[stream]} | {issued[stream]} / {settled[stream]} / {unassessable[stream]} / {pending} | {hits[stream, 0]} / {hits[stream, 1]} / {hits[stream, 2]} | {support} | {projectedRows[stream]} / {assessedRows[stream]} |");
                Console.WriteLine($"timing {condition}/{seed}/{sourceNames[stream]}: {issued[stream]} issued, {settled[stream]} settled, {unassessable[stream]} unassessable; hits {hits[stream, 0]}/{hits[stream, 1]}/{hits[stream, 2]}; support {support}");
            }
        }
        report.AppendLine("\nKnown events count only explicit public point spans, including the first fine report's partial occurrences. An open sparse collection contributes no known events; that is not a zero-event assertion. Incomplete reports include open collections and the fine startup report's explicit missing-difference residue. The learner does not bootstrap from either.\n");
        report.AppendLine("Hits use settled forecasts as their denominator, including absence misses. Widths 1, 3 and 12 make different promises: wider-cell agreement is not an accuracy improvement over a narrower task. Every rung shares the same forecast and event, so the three reads are not independent evidence. Only the frozen width-3 verdict changes support. A forecast still pending at tick 360 is not counted as a miss.\n");
        report.AppendLine("The trace CSV records each report, prior forecast issuance/prediction, new issuance, native verdict, all three rung matches, and one signed event-minus-prediction value per event-assessed forecast. Negative is early and positive is late. Unknown assessments and absence misses leave event time and signed error blank; they are not zero error.\n");
        report.AppendLine("Regular uses five-tick runs. Jitter draws each run length uniformly from 4, 5 or 6 using the stated seed. Drift selects lengths 5, 6, 7 and 8 for runs starting in successive ninety-tick blocks; a run crossing a boundary completes before the new duration applies. Stopped follows five-tick runs through tick 180, then holds its endpoint. Reversal detection occurs on the first tick with the changed nonzero delta. Only jitter uses randomness: regular, drift and stopped repeat identical environments across seeds and are not independent replicates.\n");
        report.AppendLine("Sparse acquisition's open fine-event contract cannot bootstrap this event-based learner; no sparse forecasts demonstrate a limitation of this supplied interface and learner, not an impossibility of learning from endpoints. Support is a bounded supplied utility trace, not calibrated confidence or a gating rule. This pass neither prunes rungs nor learns atrophy; an old cadence may continue proposing after misses.\n");
        report.AppendLine("Forecast-cell rows and event-assessment rows count exposed native read products as a work proxy. They exclude receipt checks, absence-deadline probes, source calculations, native subreads and temporary allocations; they are not measured runtime or evidence of compute savings. `graph-sizes.csv` samples each source and learner at prefixes 120, 240 and 360 after publication/assessment, counting reachable carriers, relations, tick references, locations, unit shapes and placeholders. These counts describe retained graphs, not total allocation or processing cost.\n");
        File.WriteAllText(Path.Combine(output, "first-run.md"), report.ToString());
        File.WriteAllText(Path.Combine(output, "traces.csv"), csv.ToString());
        File.WriteAllText(Path.Combine(output, "graph-sizes.csv"), sizes.ToString());
    }

    private static bool TryPending(IElement pending, out Relation issue, out IElement[] fields)
    {
        issue = null!;
        fields = pending is Relation group ? C.Parts(group) : [];
        if (fields.Length != 4 || fields[0] is not Relation forecast ||
            fields[1] is not CarrierLocation || fields[2] is not Relation || L.Rungs(forecast).Length != 3) return false;
        issue = forecast;
        return true;
    }

    private static long OccurrenceTime(IElement occurrence) =>
        occurrence is Relation span && TimeStepSequence.TryReadSpan(span, out var start, out var end) && start.Ticks == end.Ticks
            ? start.Ticks : throw new InvalidOperationException("A public occurrence is not a point span.");

    private static void AppendSize(StringBuilder output, string condition, int seed,
        string source, int prefix, string structure, IElement root)
    {
        var graph = GojoEncodedSnapshot.CaptureGraph(root);
        output.AppendLine($"{condition},{seed},{source},{prefix},{structure},{graph.Carriers.Length},{graph.Relations.Length},{graph.TickRefs.Length},{graph.CarrierLocations.Length},{graph.UnitShapes.Length},{graph.Placeholders.Length}");
    }

    private static string Text(long value) => value.ToString(CultureInfo.InvariantCulture);
    private static string Bit(bool? value) => value.HasValue ? value.Value ? "1" : "0" : "";
    private static string Coordinate(CarrierLocation? value) => value.HasValue ? Text(value.Value.Ticks) : "";
}
