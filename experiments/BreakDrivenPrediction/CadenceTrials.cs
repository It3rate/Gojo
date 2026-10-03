using System.Globalization;
using System.Text;
using Gojo.Core;
using Gojo.Structures;
using C = BreakDrivenPrediction.Continuation;
using L = BreakDrivenPrediction.TimingLadder;
using S = BreakDrivenPrediction.TimingSource;
using T = BreakDrivenPrediction.TimingPrediction;

namespace BreakDrivenPrediction;

internal static class CadenceTrials
{
    // Output apparatus only: these records and counters never enter a learner.
    // They preserve prospective issue identity for later paired reporting.
    private sealed class Forecast(long issuedAt, long predictedAt, long width, bool wanted)
    {
        public readonly long IssuedAt = issuedAt, PredictedAt = predictedAt, Width = width;
        public readonly bool Wanted = wanted;
        public long? AssessedAt;
        public bool? Hit;
        public bool Absence, Unknown;
        public (long IssuedAt, long PredictedAt, long Width) Key => (IssuedAt, PredictedAt, Width);
    }

    private sealed class Counts
    {
        public int IssueRows, AssessmentRows, ExtraRows, Dormant, Wakes,
            CandidateReports, ActiveReports, EventBreaks, AbsenceBreaks, UnknownOpens;
        public long AllocatedPositive, AllocatedZero;
        public readonly List<Forecast> Forecasts = [];
    }

    public static void Run(string directory)
    {
        var output = Path.GetFullPath(directory);
        Directory.CreateDirectory(output);
        var csv = new StringBuilder("condition,seed,learner,report_at,current_demand,masked,complete,known_events,occurrence_times,prior_issued_at,prior_predicted_at,prior_frozen_width,prior_wanted_at_issue,selected_cell_lower,selected_cell_upper,settled,selected_hit,absence_miss,unassessable,graded_event,signed_event_minus_prediction,new_issued_at,new_predicted_at,new_frozen_width,new_forecast,exported,issued_rows,extra_issue_rows,assessed_rows,assessed_width_matches,dormant_before,dormant_after,reactivated,phase,support_before,support_after,managed_allocated_bytes,trace_before,trace_after,candidate_before,candidate_after,interval_before,interval_after,active_before,active_after,expected_before,expected_after,window_lower_before,window_upper_before,window_lower_after,window_upper_after,scope_event_breaks,scope_absence_breaks,scope_unknown_opens\n");
        var sizes = new StringBuilder("condition,seed,prefix,structure,carriers,relations,tick_refs,locations,unit_shapes,placeholders\n");
        var pairs = new StringBuilder("condition,seed,issued_at,predicted_at,frozen_width,scoped_issued,attentive_issued,scoped_wanted,attentive_wanted,scoped_assessed_at,attentive_assessed_at,scoped_hit,attentive_hit,scoped_absence,attentive_absence,scoped_unknown,attentive_unknown\n");
        var boundaries = new StringBuilder("condition,seed,learner,boundary_at,first_later_issue_at,predicted_at,frozen_width\n");
        var report = new StringBuilder("# Cadence applicability: first fixed results\n\n");
        report.AppendLine("The fixed protocol compares a reader with an explicit cadence scope against the preceding attentive reader. Both consume the same public packet, have separate control carriers, and use the same demand and attention policy. Each environment supplies 720 fine acquisitions and 60 reports. The scope allowance is the supplied fixed two-tick policy, independent of requested width.\n");
        report.AppendLine("| Condition | Seed | Learner | Issues / wanted / pending | Settled hit / miss / absence / unknown | Issue / assessment / extra rows | Dormant / wake reports | Final support | Bytes positive / zero demand |");
        report.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        var widthRows = new List<string>();
        var scopeRows = new List<string>();
        var pairedRows = new List<string>();
        var inputRows = new List<string>();
        string[] conditions = ["regular", "jitter", "restart", "doubling", "long-period", "alternating-intervals", "unknown-coverage"];
        string[] names = ["scoped", "attentive"];
        foreach (var condition in conditions)
        foreach (var seed in new[] { 113, 907, 2029 })
        {
            var random = new Random(seed);
            long level = 1000;
            var direction = 1;
            var alternatingLong = false;
            var remaining = condition == "jitter" ? random.Next(4, 7) : condition == "long-period" ? 20 : 5;
            var publicClock = new Carrier();
            var source = S.Create(level, publicClock, 1);
            Relation[] learners = [T.CreateScoped(S.Port(source)), T.CreateAttentive(S.Port(source))];
            Counts[] counts = [new(), new()];
            Forecast?[] outstanding = [null, null];
            var completeReports = 0;
            var incompleteReports = 0;
            var publicEvents = 0;
            for (var tick = 1; tick <= 720; tick++)
            {
                // Environment generation is independent of both model graphs.
                // A changed delta is observed on the tick after a run completes.
                var moving = condition != "restart" || tick <= 180 || tick > 360;
                if ((condition == "restart" && tick is 361 or 541) ||
                    (condition == "doubling" && tick is 241 or 481))
                {
                    direction = 1;
                    remaining = condition == "restart" ? tick == 361 ? 8 : 4 : tick == 241 ? 10 : 20;
                }
                if (moving)
                {
                    level += direction;
                    if (--remaining == 0)
                    {
                        direction = -direction;
                        if (condition == "alternating-intervals") alternatingLong = !alternatingLong;
                        remaining = condition switch
                        {
                            "jitter" => random.Next(4, 7),
                            "restart" => tick <= 180 ? 5 : tick <= 540 ? 8 : 4,
                            "doubling" => tick <= 240 ? 5 : tick <= 480 ? 10 : 20,
                            "long-period" => 20,
                            "alternating-intervals" => alternatingLong ? 13 : 5,
                            _ => 5
                        };
                    }
                }
                if (!S.TryObserve(source, new CarrierLocation(publicClock, tick),
                        new CarrierLocation(S.Sensor(source), level), out source))
                    throw new InvalidOperationException("A fine source observation remained unresolved.");
                if (tick % 12 != 0) continue;
                if (!S.Publish(source, new CarrierLocation(publicClock, tick), out source, out var packet))
                    throw new InvalidOperationException("A scheduled source report remained unresolved.");
                var masked = condition == "unknown-coverage" && tick is >= 252 and <= 300;
                if (masked)
                {
                    var original = C.Parts(packet);
                    var open = new Placeholder();
                    packet = C.G(original[0], original[1], original[2], open, original[4], C.G(open));
                }
                var p = C.Parts(packet);
                var collectionKnown = p[3] is Relation collection && collection.TryReadOrderedGroup(out _);
                var occurrences = collectionKnown ? C.Parts((Relation)p[3]) : [];
                var complete = collectionKnown && p[5] is Relation residue &&
                    residue.TryReadOrderedGroup(out var excluded) && excluded.Length == 0;
                if (complete) completeReports++; else incompleteReports++;
                publicEvents += occurrences.Length;
                var times = string.Join(";", occurrences.Select(item => Text(OccurrenceTime(item))));
                var demand = DemandAt(tick);
                for (var consumer = 0; consumer < learners.Length; consumer++)
                {
                    var learner = learners[consumer];
                    T.Demand(learner).Set(demand);
                    var before = T.Pending(learner);
                    var priorValid = TryPending(before, out var priorIssue, out var frozenWidth);
                    var priorRecord = outstanding[consumer];
                    if (priorValid != (priorRecord is not null))
                        throw new InvalidOperationException("The report apparatus lost a pending native issue.");
                    var dormantBefore = T.IsDormant(learner);
                    var traceBefore = Trace(learner);
                    var scopeBefore = ScopeRead(learner);
                    // Fixed call order: scoped then attentive. Only this native
                    // admission is inside the allocation diagnostic interval.
                    var allocationBefore = GC.GetAllocatedBytesForCurrentThread();
                    var admitted = T.TryObserve(learner, packet, out var next, out var assessment);
                    var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationBefore;
                    if (!admitted)
                        throw new InvalidOperationException($"Cadence report rejected: {condition}/{seed}/{names[consumer]}/{tick}.");
                    learners[consumer] = next;
                    var a = C.Parts(assessment);
                    var after = T.Pending(next);
                    var nextValid = TryPending(after, out var newIssue, out var newWidth);
                    var fresh = nextValid && !ReferenceEquals(before, after);
                    var issuedRows = T.IssuedRows(assessment);
                    var assessedRows = T.AssessedRows(assessment);
                    var extraRows = fresh ? T.DiagnosticRows(after).Length : 0;
                    var settled = T.Settled(assessment);
                    var outcome = a[1] is Relation outcomeGroup ? C.Parts(outcomeGroup) : [];
                    var unknown = priorValid && !settled && after is Placeholder &&
                        outcome.Length == 4 && outcome[2] is Placeholder;
                    var absence = settled && assessedRows.Length == 0;
                    var hit = settled ? T.Successful(assessment) : (bool?)null;
                    var dormantAfter = T.IsDormant(next);
                    var reactivated = dormantBefore && !dormantAfter;
                    var scopeAfter = ScopeRead(next);
                    var scope = T.Scope(next);
                    var eventBreaks = scope is null ? 0 : CadenceScope.EventBreaks(scope).Length;
                    var absenceBreaks = scope is null ? 0 : CadenceScope.AbsenceBreaks(scope).Length;
                    var unknownOpens = scope is null ? 0 : CadenceScope.UnknownOpens(scope).Length;
                    var totals = counts[consumer];
                    if (demand > 0) totals.AllocatedPositive += allocatedBytes;
                    else totals.AllocatedZero += allocatedBytes;
                    totals.IssueRows += issuedRows.Length;
                    totals.AssessmentRows += assessedRows.Length;
                    totals.ExtraRows += extraRows;
                    if (dormantAfter) totals.Dormant++;
                    if (reactivated) totals.Wakes++;
                    if (scopeAfter.Candidate == true) totals.CandidateReports++;
                    if (scopeAfter.Active == true) totals.ActiveReports++;
                    totals.EventBreaks += eventBreaks;
                    totals.AbsenceBreaks += absenceBreaks;
                    totals.UnknownOpens += unknownOpens;
                    if (settled || unknown)
                    {
                        if (priorRecord is null || priorRecord.AssessedAt.HasValue)
                            throw new InvalidOperationException("An outcome lacks a unique preceding issue.");
                        priorRecord.AssessedAt = tick;
                        priorRecord.Hit = hit;
                        priorRecord.Absence = absence;
                        priorRecord.Unknown = unknown;
                        outstanding[consumer] = null;
                    }
                    CarrierLocation? gradedEvent = null, signedError = null, lower = null, upper = null;
                    if (priorValid && T.SelectedRung(before) is Relation selected &&
                        C.Parts(selected) is { Length: 4 } selectedParts && selectedParts[1] is Relation cell &&
                        TimeStepSequence.TryReadSpan(cell, out var lo, out var hi)) { lower = lo; upper = hi; }
                    if (settled && !absence)
                    {
                        if (outcome[1] is not Relation selectedAssessment || !L.TryDifference(selectedAssessment, out var error))
                            throw new InvalidOperationException("A selected event assessment has no native signed error.");
                        signedError = error;
                        var read = C.Parts((Relation)outcome[0]);
                        if (read.Length != 3 || read[1] is not CarrierLocation actual)
                            throw new InvalidOperationException("A timing assessment lost its observed event.");
                        gradedEvent = actual;
                    }
                    var prior = priorValid ? C.Parts(priorIssue) : [];
                    var future = fresh ? C.Parts(newIssue) : [];
                    if (fresh)
                    {
                        if (outstanding[consumer] is not null)
                            throw new InvalidOperationException("A new forecast replaced an unaccounted promise.");
                        var record = new Forecast(((CarrierLocation)future[0]).Ticks,
                            ((CarrierLocation)future[1]).Ticks, newWidth, demand > 0);
                        if (record.PredictedAt <= tick || record.IssuedAt != tick)
                            throw new InvalidOperationException("A forecast is not prospective from this report.");
                        totals.Forecasts.Add(record);
                        outstanding[consumer] = record;
                    }
                    else if (!nextValid && outstanding[consumer] is not null)
                        throw new InvalidOperationException("A pending forecast disappeared without an outcome.");
                    csv.AppendLine(string.Join(",", new[]
                    {
                        condition, Text(seed), names[consumer], Text(tick), Text(demand), Bit(masked), Bit(complete),
                        collectionKnown ? Text(occurrences.Length) : "", times,
                        priorValid ? Text(((CarrierLocation)prior[0]).Ticks) : "",
                        priorValid ? Text(((CarrierLocation)prior[1]).Ticks) : "",
                        priorValid ? Text(frozenWidth) : "", priorValid ? Bit(priorRecord!.Wanted) : "",
                        Coordinate(lower), Coordinate(upper), Bit(settled), Bit(hit), Bit(absence), Bit(unknown),
                        Coordinate(gradedEvent), Coordinate(signedError),
                        fresh ? Text(((CarrierLocation)future[0]).Ticks) : "",
                        fresh ? Text(((CarrierLocation)future[1]).Ticks) : "", fresh ? Text(newWidth) : "",
                        Bit(fresh), Bit(fresh && demand > 0), Text(issuedRows.Length), Text(extraRows),
                        Text(assessedRows.Length), string.Join(";", assessedRows.Select(RowMatch)),
                        Bit(dormantBefore), Bit(dormantAfter), Bit(reactivated), Text(T.Phase(next).Ticks),
                        Text(((CarrierLocation)a[2]).Ticks), Text(((CarrierLocation)a[3]).Ticks), Text(allocatedBytes),
                        traceBefore, Trace(next), Bit(scopeBefore.Candidate), Bit(scopeAfter.Candidate),
                        Coordinate(scopeBefore.Interval), Coordinate(scopeAfter.Interval), Bit(scopeBefore.Active), Bit(scopeAfter.Active),
                        Coordinate(scopeBefore.Expected), Coordinate(scopeAfter.Expected),
                        Coordinate(scopeBefore.Lower), Coordinate(scopeBefore.Upper), Coordinate(scopeAfter.Lower), Coordinate(scopeAfter.Upper),
                        scope is null ? "" : Text(eventBreaks), scope is null ? "" : Text(absenceBreaks), scope is null ? "" : Text(unknownOpens)
                    }));
                }
                if (tick % 120 == 0)
                {
                    AppendSize(sizes, condition, seed, tick, "source", source);
                    for (var consumer = 0; consumer < learners.Length; consumer++)
                        AppendSize(sizes, condition, seed, tick, names[consumer], learners[consumer]);
                }
            }
            for (var consumer = 0; consumer < learners.Length; consumer++)
            {
                var totals = counts[consumer];
                var all = totals.Forecasts;
                report.AppendLine($"| {condition} | {seed} | {names[consumer]} | {all.Count} / {all.Count(x => x.Wanted)} / {all.Count(x => !x.AssessedAt.HasValue)} | {all.Count(x => x.Hit == true)} / {all.Count(x => x.Hit == false)} / {all.Count(x => x.Absence)} / {all.Count(x => x.Unknown)} | {totals.IssueRows} / {totals.AssessmentRows} / {totals.ExtraRows} | {totals.Dormant} / {totals.Wakes} | {T.Support(learners[consumer]).Value} | {totals.AllocatedPositive} / {totals.AllocatedZero} |");
                foreach (var width in new long[] { 1, 3, 12 })
                {
                    var rows = all.Where(x => x.Width == width).ToArray();
                    widthRows.Add($"| {condition} | {seed} | {names[consumer]} | {width} | {rows.Length} / {rows.Count(x => x.Wanted)} | {rows.Count(x => x.Wanted && x.Hit == true)} / {rows.Count(x => x.Wanted && x.Hit == false)} | {rows.Count(x => x.Wanted && x.Absence)} / {rows.Count(x => x.Wanted && x.Unknown)} / {rows.Count(x => x.Wanted && !x.AssessedAt.HasValue)} |");
                }
                if (consumer == 0)
                    scopeRows.Add($"| {condition} | {seed} | {totals.CandidateReports} / {totals.ActiveReports} | {totals.EventBreaks} / {totals.AbsenceBreaks} / {totals.UnknownOpens} |");
                foreach (var boundary in new[] { 0, 180, 240, 252, 300, 360, 480, 540, 600 })
                {
                    var first = all.FirstOrDefault(x => x.IssuedAt > boundary);
                    boundaries.AppendLine($"{condition},{seed},{names[consumer]},{boundary},{(first is null ? ",," : $"{Text(first.IssuedAt)},{Text(first.PredictedAt)},{Text(first.Width)}")}");
                }
                Console.WriteLine($"cadence {condition}/{seed}/{names[consumer]}: {all.Count} issues, {all.Count(x => x.Hit == true)}/{all.Count(x => x.Hit == false)} hits/misses, {totals.IssueRows}/{totals.AssessmentRows} issue/assessment rows");
            }
            AppendPairs(pairs, pairedRows, condition, seed, counts[0].Forecasts, counts[1].Forecasts);
            inputRows.Add($"| {condition} | {seed} | 720 / 60 | {completeReports} / {incompleteReports} | {publicEvents} |");
        }
        report.AppendLine("\nAbsence is a subset of settled misses; unknown outcomes remain unscored. Outstanding promises keep their original width and wanted-at-issue classification even during withdrawal. Pending at the final boundary is neither hit nor miss.\n");
        report.AppendLine("| Condition | Seed | Learner | Frozen width | Issues / wanted | Wanted hit / miss | Wanted absence / unknown / pending |");
        report.AppendLine("| --- | --- | --- | --- | --- | --- | --- |");
        report.AppendLine(string.Join("\n", widthRows));
        report.AppendLine("\nPaired identity requires the same issue availability, target coordinate and frozen width. Only wanted settled reference issues enter omitted hit/miss counts; unknown and final-pending reference issues remain separate. An omission is not credited as a scoped success. Scoped extras likewise have no matched reference forecast. Outcome differences compare assessment time, native verdict, absence and unknown state for exact matched issues. `paired-issues.csv` preserves every union member and both outcomes.\n");
        report.AppendLine("| Condition | Seed | Same issues / outcome differences | Omitted reference hit / miss / unknown / pending | Extra scoped hit / miss / unknown / pending |");
        report.AppendLine("| --- | --- | --- | --- | --- |");
        report.AppendLine(string.Join("\n", pairedRows));
        report.AppendLine("\nScope reads below are retrospective applicability evidence, not prospective success or extra support. Candidate presence and active continuation are counted after each report, separately; an old candidate can remain inspectable with no current continuation. Unknown opens include the source's initial incomplete report.\n");
        report.AppendLine("| Condition | Seed | Candidate / active reports | Event breaks / absence breaks / unknown opens |");
        report.AppendLine("| --- | --- | --- | --- |");
        report.AppendLine(string.Join("\n", scopeRows));
        report.AppendLine("\n| Condition | Seed | Acquisitions / reports | Complete / incomplete reports | Known public occurrences |");
        report.AppendLine("| --- | --- | --- | --- | --- |");
        report.AppendLine(string.Join("\n", inputRows));
        report.AppendLine("\nRegular and unknown-coverage use five-tick alternating runs; jitter draws each length from 4–6. Restart uses five through 180, flat through 360, eight through 540 and four thereafter, resetting a positive run at 361 and 541. Doubling uses five through 240, ten through 480 and twenty thereafter, resetting at 241 and 481. Long-period uses twenty throughout. Alternating-intervals repeats run lengths five and thirteen. Only jitter depends on its seed; other repeated rows are deterministic duplicates, not independent evidence. Event times are strict reversal detector observations.\n");
        report.AppendLine("Demand is width 3 through 240, zero at 241–360, width 12 at 361–480, width 1 at 481–600, then width 3. Both readers receive the same demand before admission. Unknown-coverage masks reports 252, 264, 276, 288 and 300 using an open occurrence slot and open coverage evidence; port, directed coverage span, timestamp and scalar endpoint remain exactly the published objects. Hidden fine events never enter either reader or its score. Startup known events remain visible with incomplete coverage and are conservatively unused.\n");
        report.AppendLine("New issues follow the current report's grading and are strictly later than its availability. Native selected-rung verdicts supply hits and misses; different widths are different promises. An absence miss has no invented event coordinate or zero timing error. Withholding a forecast is not a correct no-event prediction. The scope policy can reject a legitimate longer interval; its fixed allowance is not inferred uncertainty and has not been tuned from these results. Alternating intervals remain a challenge for a one-interval continuation.\n");
        report.AppendLine("Issued and assessed rung counts are structural work proxies, not total calculation or runtime. Scope checks, validation, absence evidence and attention control also cost work. Managed allocated bytes are the same-thread counter change around each native admission, all sixty calls including startup, in fixed scoped-then-attentive order. Construction, source acquisition/publication, demand assignment, reporting and graph capture are excluded. Positive/zero columns use current demand, so grading an older wanted promise during withdrawal remains included in zero-demand cost. This is a diagnostic, not a benchmark, CPU measurement or retained-memory estimate. Acquisition is fixed and unchanged.\n");
        report.AppendLine("`traces.csv` exposes native before/after fresh trace, remembered interval, expected coordinate and window, plus current scope break counts and prospective outcomes. Blank scope columns mean the attentive reference has no scope, not an inactive known scope. `boundaries.csv` lists the first issue strictly after each fixed inspection boundary, with its target and width; it does not assert those boundaries were visible to the learner. `graph-sizes.csv` samples retained source and reader structure every 120 ticks. Report history and pairing records are host apparatus; neither reader retains this archive.\n");
        File.WriteAllText(Path.Combine(output, "first-run.md"), report.ToString().TrimEnd() + "\n");
        File.WriteAllText(Path.Combine(output, "traces.csv"), csv.ToString());
        File.WriteAllText(Path.Combine(output, "graph-sizes.csv"), sizes.ToString());
        File.WriteAllText(Path.Combine(output, "paired-issues.csv"), pairs.ToString());
        File.WriteAllText(Path.Combine(output, "boundaries.csv"), boundaries.ToString());
    }

    private static void AppendPairs(StringBuilder csv, List<string> report, string condition, int seed,
        List<Forecast> scoped, List<Forecast> attentive)
    {
        var s = scoped.ToDictionary(x => x.Key);
        var a = attentive.ToDictionary(x => x.Key);
        var same = s.Keys.Count(a.ContainsKey);
        var differences = s.Keys.Where(a.ContainsKey).Count(key =>
            s[key].AssessedAt != a[key].AssessedAt || s[key].Hit != a[key].Hit ||
            s[key].Absence != a[key].Absence || s[key].Unknown != a[key].Unknown);
        var omitted = attentive.Where(x => x.Wanted && !s.ContainsKey(x.Key)).ToArray();
        var extra = scoped.Where(x => x.Wanted && !a.ContainsKey(x.Key)).ToArray();
        report.Add($"| {condition} | {seed} | {same} / {differences} | {Outcomes(omitted)} | {Outcomes(extra)} |");
        foreach (var key in s.Keys.Union(a.Keys).OrderBy(x => x.IssuedAt).ThenBy(x => x.PredictedAt).ThenBy(x => x.Width))
        {
            s.TryGetValue(key, out var left);
            a.TryGetValue(key, out var right);
            csv.AppendLine(string.Join(",", new[]
            {
                condition, Text(seed), Text(key.IssuedAt), Text(key.PredictedAt), Text(key.Width),
                Bit(left is not null), Bit(right is not null), Bit(left?.Wanted), Bit(right?.Wanted),
                left?.AssessedAt is long ls ? Text(ls) : "", right?.AssessedAt is long rs ? Text(rs) : "",
                Bit(left?.Hit), Bit(right?.Hit), Bit(left?.Absence), Bit(right?.Absence), Bit(left?.Unknown), Bit(right?.Unknown)
            }));
        }
    }

    private static string Outcomes(Forecast[] rows) =>
        $"{rows.Count(x => x.Hit == true)} / {rows.Count(x => x.Hit == false)} / {rows.Count(x => x.Unknown)} / {rows.Count(x => !x.AssessedAt.HasValue)}";

    private static (bool? Candidate, bool? Active, CarrierLocation? Interval, CarrierLocation? Expected,
        CarrierLocation? Lower, CarrierLocation? Upper) ScopeRead(Relation root)
    {
        var scope = T.Scope(root);
        if (scope is null) return (null, null, null, null, null, null);
        CarrierLocation? lower = null, upper = null;
        if (T.ContinuationWindow(root) is Relation window &&
            TimeStepSequence.TryReadSpan(window, out var lo, out var hi)) { lower = lo; upper = hi; }
        return (T.RetainedCadence(root) is Relation, T.HasCurrentCadence(root),
            CadenceScope.TryInterval(scope, out var interval) ? interval : null,
            T.TryExpected(root, out var expected) ? expected : null, lower, upper);
    }

    private static bool TryPending(IElement pending, out Relation issue, out long width)
    {
        issue = null!;
        width = 0;
        if (pending is not Relation group || C.Parts(group) is not { Length: 4 } p ||
            p[0] is not Relation forecast || p[1] is not CarrierLocation demand || T.SelectedRung(pending) is null) return false;
        issue = forecast;
        width = demand.Ticks;
        return true;
    }

    private static string RowMatch(Relation row)
    {
        var p = C.Parts(row);
        if (p.Length != 6 || p[0] is not Relation rung || C.Parts(rung) is not { Length: 4 } r ||
            r[1] is not Relation cell || !L.TryDifference(row, out _) ||
            p.Skip(1).Take(3).Any(element => element is not Relation claim || !OperationClaim.TryReadResolvedViolation(claim, out _)))
            throw new InvalidOperationException("An unresolved rung is not a failed timing prediction.");
        return $"{Text(C.Length(cell).Ticks)}:{Bit(L.Matches(row))}";
    }

    private static string Trace(Relation root) => string.Join(";", T.Events(root).Select(item =>
        item is CarrierLocation location ? Text(location.Ticks) : "open"));
    private static long DemandAt(int tick) => tick <= 240 ? 3 : tick <= 360 ? 0 : tick <= 480 ? 12 : tick <= 600 ? 1 : 3;
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
