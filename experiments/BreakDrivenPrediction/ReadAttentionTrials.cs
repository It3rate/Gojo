using System.Globalization;
using System.Text;
using Gojo.Core;
using Gojo.Structures;
using C = BreakDrivenPrediction.Continuation;
using L = BreakDrivenPrediction.TimingLadder;
using S = BreakDrivenPrediction.TimingSource;
using T = BreakDrivenPrediction.TimingPrediction;

namespace BreakDrivenPrediction;

internal static class ReadAttentionTrials
{
    // These counters are output apparatus, reconstructible from traces.csv.
    // Neither learner can read them, the environment schedule, or future input.
    private sealed class Counts
    {
        public int Issued, Exported, Settled, Unassessable, Absence, IssueRows, AssessmentRows,
            ExtraRows, UnusedIssues, UnusedIssueRows, AssessmentRowsFromUnused, Dormant,
            Reactivations, Suspensions;
        public long ManagedAllocatedBytes;
        // Fixed display buckets: width 1/3/12; wanted hit/miss, unused hit/miss.
        public readonly int[,] Scores = new int[3, 4];
    }

    public static void Run(string directory)
    {
        var output = Path.GetFullPath(directory);
        Directory.CreateDirectory(output);
        var csv = new StringBuilder("condition,seed,learner,report_at,current_demand,learner_demand,complete,known_events,occurrence_times,presence_reference_issued_at,constant_absence_correct,constant_presence_correct,prior_issued_at,prior_predicted_at,prior_frozen_width,prior_wanted_at_issue,selected_cell_lower,selected_cell_upper,settled,selected_hit,absence_miss,unassessable,graded_event,signed_event_minus_prediction,new_issued_at,new_predicted_at,new_frozen_width,new_forecast,exported,issued_rows,extra_issue_rows,assessed_rows,assessed_width_matches,unused_issue_rows,assessment_rows_from_unused_issue,dormant_before,dormant_after,reactivated,phase,support_before,support_after,managed_allocated_bytes\n");
        var sizes = new StringBuilder("condition,seed,prefix,structure,carriers,relations,tick_refs,locations,unit_shapes,placeholders\n");
        var report = new StringBuilder("# Demand-directed timing reads: first results\n\n");
        report.AppendLine("The fixed protocol uses 720 fine acquisitions and 60 twelve-tick reports per environment. One source publishes the identical packet to two independently controlled learners on the same public port. The eager reference constructs all three rungs; the attentive learner selects demand and applies the supplied probe/dormancy policy.\n");
        report.AppendLine("| Condition | Seed | Learner | Issued / exported / pending | Settled / absence / unassessable | Issue / event-assessment rows | Extra issue rows | Unused issues / issue rows / assessed rows | Dormant / sleep / wake reports | Final support | Managed allocated bytes |");
        report.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        var scoreRows = new List<string>();
        var presenceRows = new List<string>();
        string[] conditions = ["regular", "jitter", "intermittent", "changed-cadence"];
        string[] learnerNames = ["attentive", "eager"];
        foreach (var condition in conditions)
        foreach (var seed in new[] { 113, 907, 2029 })
        {
            var random = new Random(seed);
            long level = 1000;
            var direction = 1;
            var remaining = condition == "jitter" ? random.Next(4, 7) : 5;
            var publicClock = new Carrier();
            var source = S.Create(level, publicClock, 1);
            Relation[] learners = [T.CreateAttentive(S.Port(source)), T.Create(S.Port(source))];
            Counts[] counts = [new(), new()];
            // One bounded observer note per consumer records utility at issuance,
            // not model state. A later demand change cannot relabel an old issue.
            long?[] requestedAtIssue = [null, null];
            var acquisitions = 0;
            var reports = 0;
            var completeReports = 0;
            var absenceCorrect = 0;
            var presenceCorrect = 0;
            long lastPositiveDemand = 3;
            // Both reference predictions are constants declared before all input:
            // no occurrence / at least one occurrence in the next report window.
            const bool absencePrediction = false, presencePrediction = true;
            for (var tick = 1; tick <= 720; tick++)
            {
                var moving = condition switch
                {
                    "intermittent" => ((tick - 1) / 120) % 2 == 0,
                    "changed-cadence" => tick <= 180 || tick > 360,
                    _ => true
                };
                if ((condition == "intermittent" && moving && (tick - 1) % 120 == 0) ||
                    (condition == "changed-cadence" && tick is 361 or 541))
                {
                    direction = 1;
                    remaining = condition == "changed-cadence" ? tick == 361 ? 8 : 4 : 5;
                }
                if (moving)
                {
                    level += direction;
                    if (--remaining == 0)
                    {
                        direction = -direction;
                        remaining = condition == "jitter" ? random.Next(4, 7) :
                            condition == "changed-cadence" ? tick < 360 ? 5 : tick < 540 ? 8 : 4 : 5;
                    }
                }
                if (!S.TryObserve(source, new CarrierLocation(publicClock, tick),
                        new CarrierLocation(S.Sensor(source), level), out source))
                    throw new InvalidOperationException("A fine source observation remained unresolved.");
                acquisitions++;
                if (tick % 12 != 0) continue;
                if (!S.Publish(source, new CarrierLocation(publicClock, tick), out source, out var packet))
                    throw new InvalidOperationException("A scheduled source report remained unresolved.");
                reports++;
                var p = C.Parts(packet);
                var occurrences = p[3] is Relation collection && collection.TryReadOrderedGroup(out var items) ? items : [];
                var complete = p[3] is Relation && p[5] is Relation residue &&
                    residue.TryReadOrderedGroup(out var excluded) && excluded.Length == 0;
                bool? absencePass = null, presencePass = null;
                if (complete)
                {
                    var hasEvent = occurrences.Length > 0;
                    absencePass = absencePrediction == hasEvent;
                    presencePass = presencePrediction == hasEvent;
                    completeReports++;
                    if (absencePass == true) absenceCorrect++;
                    if (presencePass == true) presenceCorrect++;
                }
                var times = string.Join(";", occurrences.Select(item => Text(OccurrenceTime(item))));
                var demand = DemandAt(tick);
                if (demand > 0) lastPositiveDemand = demand;
                for (var consumer = 0; consumer < learners.Length; consumer++)
                {
                    var learner = learners[consumer];
                    T.Demand(learner).Set(consumer == 0 ? demand : lastPositiveDemand);
                    var before = T.Pending(learner);
                    var priorValid = TryPending(before, out var priorIssue, out var frozenWidth);
                    var priorWanted = priorValid && requestedAtIssue[consumer] is > 0;
                    var dormantBefore = T.IsAttentive(learner) && T.IsDormant(learner);
                    // The shared packet is the complete source input. No source
                    // trace, environment label, run duration or future is passed.
                    var allocationBefore = GC.GetAllocatedBytesForCurrentThread();
                    var admitted = T.TryObserve(learner, packet, out var next, out var assessment);
                    var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationBefore;
                    if (!admitted)
                        throw new InvalidOperationException($"Attention report rejected: {condition}/{seed}/{learnerNames[consumer]}/{tick}.");
                    learners[consumer] = next;
                    var a = C.Parts(assessment);
                    var after = T.Pending(next);
                    var nextValid = TryPending(after, out var newIssue, out var newWidth);
                    var fresh = nextValid && !ReferenceEquals(before, after);
                    var issuedRows = T.IssuedRows(assessment);
                    var assessedRows = T.AssessedRows(assessment);
                    var extraRows = fresh ? T.DiagnosticRows(after).Length : 0;
                    var wasSettled = T.Settled(assessment);
                    var outcome = a[1] is Relation outcomeGroup ? C.Parts(outcomeGroup) : [];
                    var unassessable = priorValid && !wasSettled && after is Placeholder &&
                        outcome.Length == 4 && outcome[2] is Placeholder;
                    var absent = wasSettled && assessedRows.Length == 0;
                    var selectedHit = wasSettled ? T.Successful(assessment) : (bool?)null;
                    var dormantAfter = T.IsAttentive(next) && T.IsDormant(next);
                    var reactivated = dormantBefore && !dormantAfter;
                    var totals = counts[consumer];
                    totals.ManagedAllocatedBytes += allocatedBytes;
                    if (fresh)
                    {
                        totals.Issued++;
                        if (demand > 0) totals.Exported++;
                        else totals.UnusedIssues++;
                    }
                    if (wasSettled)
                    {
                        totals.Settled++;
                        totals.Scores[WidthIndex(frozenWidth), (priorWanted ? 0 : 2) + (selectedHit == true ? 0 : 1)]++;
                    }
                    if (absent) totals.Absence++;
                    if (unassessable) totals.Unassessable++;
                    if (dormantAfter) totals.Dormant++;
                    if (reactivated) totals.Reactivations++;
                    if (!dormantBefore && dormantAfter) totals.Suspensions++;
                    totals.IssueRows += issuedRows.Length;
                    totals.AssessmentRows += assessedRows.Length;
                    totals.ExtraRows += extraRows;
                    var unusedRows = demand == 0 ? issuedRows.Length : 0;
                    var unusedAssessment = priorValid && !priorWanted ? assessedRows.Length : 0;
                    totals.UnusedIssueRows += unusedRows;
                    totals.AssessmentRowsFromUnused += unusedAssessment;
                    CarrierLocation? gradedEvent = null, signedError = null, lower = null, upper = null;
                    if (priorValid && T.SelectedRung(before) is Relation selected &&
                        C.Parts(selected) is { Length: 4 } selectedParts && selectedParts[1] is Relation cell &&
                        TimeStepSequence.TryReadSpan(cell, out var lo, out var hi)) { lower = lo; upper = hi; }
                    if (wasSettled && !absent)
                    {
                        if (outcome[1] is not Relation selectedAssessment || !L.TryDifference(selectedAssessment, out var error))
                            throw new InvalidOperationException("A selected event assessment has no native signed error.");
                        signedError = error;
                        var read = C.Parts((Relation)outcome[0]);
                        if (read.Length != 3 || read[1] is not CarrierLocation actual)
                            throw new InvalidOperationException("A timing assessment lost its observed event.");
                        gradedEvent = actual;
                    }
                    var rowMatches = string.Join(";", assessedRows.Select(RowMatch));
                    var prior = priorValid ? C.Parts(priorIssue) : [];
                    var future = fresh ? C.Parts(newIssue) : [];
                    csv.AppendLine(string.Join(",", new[]
                    {
                        condition, Text(seed), learnerNames[consumer], Text(tick), Text(demand),
                        Text(T.Demand(next).ValueSlot.Ticks), Bit(complete), Text(occurrences.Length), times,
                        Text(tick - 12), Bit(absencePass), Bit(presencePass),
                        priorValid ? Text(((CarrierLocation)prior[0]).Ticks) : "",
                        priorValid ? Text(((CarrierLocation)prior[1]).Ticks) : "",
                        priorValid ? Text(frozenWidth) : "", priorValid ? Bit(priorWanted) : "",
                        Coordinate(lower), Coordinate(upper), Bit(wasSettled), Bit(selectedHit), Bit(absent), Bit(unassessable),
                        Coordinate(gradedEvent), Coordinate(signedError),
                        fresh ? Text(((CarrierLocation)future[0]).Ticks) : "",
                        fresh ? Text(((CarrierLocation)future[1]).Ticks) : "", fresh ? Text(newWidth) : "",
                        Bit(fresh), Bit(fresh && demand > 0), Text(issuedRows.Length), Text(extraRows),
                        Text(assessedRows.Length), rowMatches, Text(unusedRows), Text(unusedAssessment),
                        Bit(dormantBefore), Bit(dormantAfter), Bit(reactivated),
                        T.IsAttentive(next) ? Text(T.Phase(next).Ticks) : "",
                        Text(((CarrierLocation)a[2]).Ticks), Text(((CarrierLocation)a[3]).Ticks), Text(allocatedBytes)
                    }));
                    if (fresh) requestedAtIssue[consumer] = demand;
                    else if (!nextValid) requestedAtIssue[consumer] = null;
                }
                if (tick % 120 == 0)
                {
                    AppendSize(sizes, condition, seed, tick, "source", source);
                    for (var consumer = 0; consumer < learners.Length; consumer++)
                        AppendSize(sizes, condition, seed, tick, learnerNames[consumer], learners[consumer]);
                }
            }
            for (var consumer = 0; consumer < learners.Length; consumer++)
            {
                var totals = counts[consumer];
                var pending = TryPending(T.Pending(learners[consumer]), out _, out _) ? 1 : 0;
                report.AppendLine($"| {condition} | {seed} | {learnerNames[consumer]} | {totals.Issued} / {totals.Exported} / {pending} | {totals.Settled} / {totals.Absence} / {totals.Unassessable} | {totals.IssueRows} / {totals.AssessmentRows} | {totals.ExtraRows} | {totals.UnusedIssues} / {totals.UnusedIssueRows} / {totals.AssessmentRowsFromUnused} | {totals.Dormant} / {totals.Suspensions} / {totals.Reactivations} | {T.Support(learners[consumer]).Value} | {totals.ManagedAllocatedBytes} |");
                scoreRows.Add($"| {condition} | {seed} | {learnerNames[consumer]} | {Score(totals, 0)} | {Score(totals, 1)} | {Score(totals, 2)} |");
                Console.WriteLine($"attention {condition}/{seed}/{learnerNames[consumer]}: {totals.Issued} issues, {totals.IssueRows}/{totals.AssessmentRows} issue/assessment rows; {totals.Dormant} dormant reports, {totals.Reactivations} wakes");
            }
            presenceRows.Add($"| {condition} | {seed} | {acquisitions} / {reports} | {completeReports} | {absenceCorrect} | {presenceCorrect} |");
        }
        report.AppendLine("\nEach cell below reports wanted hits/misses; unused hits/misses, grouped by the width frozen at issuance. Wanted means consumer demand was positive when the forecast was issued. Eager forecasts created during withdrawal are retained and graded honestly but are not retroactively useful when demand returns. Different widths are different promises, and different issued sets are not a paired accuracy comparison.\n");
        report.AppendLine("| Condition | Seed | Learner | Width 1: wanted; unused | Width 3: wanted; unused | Width 12: wanted; unused |");
        report.AppendLine("| --- | --- | --- | --- | --- | --- |");
        report.AppendLine(string.Join("\n", scoreRows));
        report.AppendLine("\nThe following constant host reference predictions were fixed before input: no event, or at least one event, in the next twelve-tick report window. They use only complete public reports for grading; startup uncertainty is excluded. These easier event-presence scores are environment diagnostics, not Gojo reasoning claims or exact-timing baselines. Their shared denominator is the complete-report count.\n");
        report.AppendLine("| Condition | Seed | Fine acquisitions / reports | Complete reports | Constant absence correct | Constant presence correct |");
        report.AppendLine("| --- | --- | --- | --- | --- | --- |");
        report.AppendLine(string.Join("\n", presenceRows));
        report.AppendLine("\nDemand is width 3 through tick 240, zero at 241–360, width 12 at 361–480, width 1 at 481–600, then width 3. It is applied before report consumption. The eager reference retains its last positive width during withdrawal and continues all-rung work; unused rows are counted separately. Outstanding promises keep their original width and wanted-at-issue classification. The trace records both that frozen promise and current demand.\n");
        report.AppendLine("Regular runs last five ticks; jitter draws lengths 4–6. Intermittent alternates 120 moving and 120 flat ticks, restarting a positive five-tick run at each moving block. Changed cadence runs at five through tick 180, holds through 360, restarts at eight at 361, then restarts at four at 541. Only jitter is random: the other seed-labelled rows repeat deterministic environments and are not independent trials. Reversal times are detector observations, not physical inflection claims.\n");
        report.AppendLine("New forecasts are issued only after the current report has been assessed, strictly beyond its availability; there is no same-batch grading. Event assessments use the native selected-rung verdict and signed difference. Absence misses have no invented event time or zero timing error. Forecasts still pending at tick 720 are neither successes nor misses. Dormant means the native event gate, distinct from zero utility demand. A wake can preserve knowledge that an event arrived without immediately supplying the two fresh events needed for a new cadence.\n");
        report.AppendLine("Issued rows and event-assessment rows are exposed structural work proxies. Extra attentive rows are diagnostic probes; eager extra rows are its always-created unselected reads. All rows share a forecast, so they are not independent evidence or separate rewards. The proxies omit width selection, report validation, absence probes, attention/control witnesses, source arithmetic and transient allocations; they do not measure runtime or net compute savings. Source acquisition remains 720 for both consumers. No sensor savings or adaptive sampler is claimed.\n");
        report.AppendLine("Managed allocated bytes are the raw same-thread counter difference immediately around each learner's `TryObserve` call, with all sixty calls and startup included. Source observation/publication, demand assignment, report scoring/formatting and graph capture are outside that interval. The CSV records each call and the table their cumulative sum. This diagnostic can expose controller overhead hidden by rung counts; it is not a benchmark, CPU time, total process allocation or retained-memory measurement. No warmup rows are removed.\n");
        report.AppendLine("The native policy supplies demand, four-issue probe cadence, failure-triggered probing and absence-driven dormancy; it does not learn those rules. Diagnostics do not yet revise which width is demanded. `graph-sizes.csv` samples each source and learner every 120 ticks after report processing. Counts describe reachable retained structure, not cumulative allocation. `traces.csv` keeps report-level outputs outside the learners; neither model retains this report history.\n");
        File.WriteAllText(Path.Combine(output, "first-run.md"), report.ToString().TrimEnd() + "\n");
        File.WriteAllText(Path.Combine(output, "traces.csv"), csv.ToString());
        File.WriteAllText(Path.Combine(output, "graph-sizes.csv"), sizes.ToString());
    }

    private static bool TryPending(IElement pending, out Relation issue, out long width)
    {
        issue = null!;
        width = 0;
        if (pending is not Relation group || C.Parts(group) is not { Length: 4 } p ||
            p[0] is not Relation forecast || p[1] is not CarrierLocation demand ||
            T.SelectedRung(pending) is null) return false;
        issue = forecast;
        width = demand.Ticks;
        return true;
    }

    private static string RowMatch(Relation row)
    {
        var p = C.Parts(row);
        if (p.Length != 6 || p[0] is not Relation rung || C.Parts(rung) is not { Length: 4 } r ||
            r[1] is not Relation cell || !L.TryDifference(row, out _) ||
            p.Skip(1).Take(3).Any(element => element is not Relation claim ||
                !OperationClaim.TryReadResolvedViolation(claim, out _)))
            throw new InvalidOperationException("An unresolved rung is not a failed timing prediction.");
        return $"{Text(C.Length(cell).Ticks)}:{Bit(L.Matches(row))}";
    }

    private static long DemandAt(int tick) => tick <= 240 ? 3 : tick <= 360 ? 0 : tick <= 480 ? 12 : tick <= 600 ? 1 : 3;
    private static int WidthIndex(long width) => width switch { 1 => 0, 3 => 1, 12 => 2,
        _ => throw new InvalidOperationException("An unexpected frozen width has no fixed report bucket.") };
    private static string Score(Counts counts, int width) =>
        $"{counts.Scores[width, 0]}/{counts.Scores[width, 1]}; {counts.Scores[width, 2]}/{counts.Scores[width, 3]}";
    private static long OccurrenceTime(IElement occurrence) =>
        occurrence is Relation span && TimeStepSequence.TryReadSpan(span, out var a, out var b) && a.Ticks == b.Ticks
            ? a.Ticks : throw new InvalidOperationException("A public occurrence is not a point span.");
    private static void AppendSize(StringBuilder output, string condition, int seed, int prefix, string name, IElement root)
    {
        var graph = GojoEncodedSnapshot.CaptureGraph(root);
        output.AppendLine($"{condition},{seed},{prefix},{name},{graph.Carriers.Length},{graph.Relations.Length},{graph.TickRefs.Length},{graph.CarrierLocations.Length},{graph.UnitShapes.Length},{graph.Placeholders.Length}");
    }
    private static string Text(long value) => value.ToString(CultureInfo.InvariantCulture);
    private static string Bit(bool? value) => value.HasValue ? value.Value ? "1" : "0" : "";
    private static string Coordinate(CarrierLocation? value) => value.HasValue ? Text(value.Value.Ticks) : "";
}
