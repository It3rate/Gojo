using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using C = BreakDrivenPrediction.Continuation;
using N = BreakDrivenPrediction.NativeCircuit;
using L = BreakDrivenPrediction.TimingLadder;
using A = BreakDrivenPrediction.TimingReadAttention;
using Q = BreakDrivenPrediction.CadenceScope;

namespace BreakDrivenPrediction;

/// <summary>Bounded prospective scheduling over a public occurrence port.
/// The two-site trace, cadence calculation, forecast, demand and support are
/// connected Gojo structure. This helper applies the supplied local policy.</summary>
public static class TimingPrediction
{
    // Root: port, ladder, two occurrences, pending, availability, support,
    // demand, policy, latest assessment, latest report, physical admission cursor,
    // cursor-to-public-clock map. Availability is the root's version witness.
    public static Relation Create(Relation port, long demandedWidth = 3, params long[] widths)
        => CreateCore(port, demandedWidth, widths, false);

    /// <summary>
    /// Adds a connected selective-read policy. Zero demand withdraws new work;
    /// already issued promises retain their frozen demand. Probe cadence counts
    /// eligible new issues, not reports or elapsed physical time.
    /// </summary>
    public static Relation CreateAttentive(Relation port, long demandedWidth = 3,
        long probeEvery = 4, params long[] widths)
    {
        if (probeEvery <= 0) return Relation.UnresolvedRead(port, port, Carrier.Unknown);
        var root = CreateCore(port, demandedWidth, widths, true);
        var p = C.Parts(root);
        return p.Length == 12 ? C.G([.. p, A.Create(root, probeEvery)]) : root;
    }

    /// <summary>Adds source-local cadence qualification independently of output
    /// demand and support. Existing attentive learners keep their earlier law.</summary>
    public static Relation CreateScoped(Relation port, long demandedWidth = 3,
        long probeEvery = 4, long slackTicks = 2, params long[] widths)
    {
        if (slackTicks < 0) return Relation.UnresolvedRead(port, port, Carrier.Unknown);
        var root = CreateAttentive(port, demandedWidth, probeEvery, widths);
        var p = C.Parts(root);
        return p.Length == 13 ? C.G([.. p, Q.Create(port, (Relation)p[2], (CarrierLocation)p[4], slackTicks)]) : root;
    }

    private static Relation CreateCore(Relation port, long demandedWidth, long[] widths, bool allowWithdrawal)
    {
        var p = C.Parts(port);
        if (p.Length != 3 || p[0] is not Carrier clock || clock.IsUnknown || demandedWidth < 0 ||
            (demandedWidth == 0 && !allowWithdrawal))
            return Relation.UnresolvedRead(port, port, Carrier.Unknown);
        if (widths.Length == 0) widths = [1, 3, 12];
        var ladder = L.Create(clock, widths);
        var support = new Carrier();
        support.Attach(new Relation(Relation.Point(support, 8), Relation.Point(support, 8)));
        var demand = new Carrier(clock.UnitShape);
        Carrier.ReceiveTogether(C.G(new CarrierLocation(demand, demandedWidth)));
        var policy = C.G(new CarrierLocation(support, 0), new CarrierLocation(support, 8),
            new CarrierLocation(support, 1), new CarrierLocation(support, -1));
        var cursor = new Carrier(clock.UnitShape);
        var cursorMap = Equation.Affine(cursor, 0, 1, clock, 0, 1);
        return C.G(port, ladder, C.G(new Placeholder(), new Placeholder()), new Placeholder(),
            new CarrierLocation(clock, 0), support, demand, policy, new Placeholder(), new Placeholder(), cursor, cursorMap);
    }

    public static Relation Port(Relation root) => (Relation)C.Parts(root)[0];
    public static Relation Ladder(Relation root) => (Relation)C.Parts(root)[1];
    public static IElement[] Events(Relation root) => C.Parts((Relation)C.Parts(root)[2]);
    public static IElement Pending(Relation root) => C.Parts(root)[3];
    public static CarrierLocation Available(Relation root) => (CarrierLocation)C.Parts(root)[4];
    public static Carrier Support(Relation root) => (Carrier)C.Parts(root)[5];
    public static Carrier Demand(Relation root) => (Carrier)C.Parts(root)[6];

    /// <summary>These are local inspection adapters over retained native state.</summary>
    public static bool IsAttentive(Relation root) => C.Parts(root) is { Length: 13 or 14 } p && p[12] is Relation;
    public static Relation? Scope(Relation root) => C.Parts(root) is { Length: 14 } p ? p[13] as Relation : null;
    public static bool HasCurrentCadence(Relation root) => Scope(root) is Relation scope && Q.TryExpected(scope, out _);
    public static IElement RetainedCadence(Relation root) => Scope(root) is Relation scope
        ? C.Parts(scope)[3] : new Placeholder();
    public static IElement ContinuationWindow(Relation root) => Scope(root) is Relation scope
        ? Q.Window(scope) : new Placeholder();
    public static bool TryExpected(Relation root, out CarrierLocation expected)
    {
        expected = default;
        return Scope(root) is Relation scope && Q.TryExpected(scope, out expected);
    }
    public static bool IsDormant(Relation root) => IsAttentive(root) &&
        C.Pass(A.On(A.Sample((Relation)C.Parts(root)[12], 2)));
    public static CarrierLocation Phase(Relation root) => IsAttentive(root)
        ? A.Sample((Relation)C.Parts(root)[12], 0) : default;
    public static Relation ProbeSpan(Relation root) => IsAttentive(root)
        ? (Relation)C.Parts((Relation)C.Parts(root)[12])[1] : C.G();
    public static Relation? SelectedRung(IElement pending) => IsPending(pending)
        ? C.Parts((Relation)pending)[2] as Relation : null;
    public static Relation[] DiagnosticRows(IElement pending) => IsPending(pending)
        ? L.Rungs((Relation)C.Parts((Relation)pending)[0])
            .Where(row => !ReferenceEquals(row, SelectedRung(pending))).ToArray() : [];
    public static Relation[] IssuedRows(Relation assessment) => C.Parts(assessment) is { Length: 6 } p &&
        !ReferenceEquals(p[0], p[4]) && IsPending(p[4])
        ? L.Rungs((Relation)C.Parts((Relation)p[4])[0]) : [];
    public static Relation[] AssessedRows(Relation assessment)
    {
        if (C.Parts(assessment) is not { Length: 6 } p || p[1] is not Relation outcome ||
            C.Parts(outcome) is not { Length: 4 } o || o[0] is not Relation read ||
            C.Parts(read) is not { Length: 3 } r || r[2] is not Relation rows) return [];
        var parts = C.Parts(rows);
        return parts.All(part => part is Relation row && L.TryDifference(row, out _))
            ? parts.Cast<Relation>().ToArray() : [];
    }

    // Assessment: previous pending, outcome/open, support before/after,
    // new pending, incoming report. Outcome: ladder assessment or absence/open
    // evidence, selected assessment row/open, verdict/open, support response.
    public static bool Settled(Relation assessment) => C.Parts(assessment) is { Length: 6 } a &&
        a[1] is Relation outcome && C.Parts(outcome) is { Length: 4 } o &&
        o[2] is Relation claim && OperationClaim.TryReadResolvedViolation(claim, out _);
    public static bool Successful(Relation assessment) => Settled(assessment) &&
        C.Pass((Relation)C.Parts((Relation)C.Parts(assessment)[1])[2]);

    public static bool TryObserve(Relation root, Relation packet, out Relation next, out Relation assessment)
    {
        next = root;
        assessment = Relation.UnresolvedRead(packet, root, Carrier.Unknown);
        try
        {
            var p = C.Parts(root);
            if (p.Length is not (12 or 13 or 14) || !TryReport(root, packet, out var available, out var occurrences,
                    out var complete, out var receipt)) return false;
            var supportBefore = C.Copy(Support(root).ValueSlot);
            var supportAfter = supportBefore;
            var attention = p.Length >= 13 ? (Relation)p[12] : null;
            var scope = Scope(root);
            var scopeAfter = scope;
            var phaseBefore = attention is null ? default : A.Sample(attention, 0);
            var dormantBefore = attention is null ? default : A.Sample(attention, 2);
            var probeBefore = attention is null ? default : A.Sample(attention, 4);
            var phaseAfter = phaseBefore;
            var dormantAfter = dormantBefore;
            var probeAfter = probeBefore;
            IElement attentionEvidence = new Placeholder();
            IElement pending = p[3], outcome = new Placeholder();
            var history = Events(root);
            var pendingFields = pending is Relation pendingRelation ? C.Parts(pendingRelation) : [];
            var hasPending = IsPending(pending);
            var absenceMiss = false;

            // An actual public occurrence can reopen a dormant detector. The
            // silent interval is not promoted to a recurring period: learn a
            // fresh two-site trace, including any other events in this batch.
            if (attention is not null)
            {
                var sleep = A.On(dormantBefore);
                var wake = complete && C.Pass(sleep) && occurrences.Length != 0;
                if (wake)
                {
                    dormantAfter = new CarrierLocation(dormantBefore.Carrier, 0);
                    if (scope is null) history = [new Placeholder(), new Placeholder()];
                }
                attentionEvidence = C.G(sleep, C.G(occurrences.Cast<IElement>().ToArray()),
                    C.G(dormantBefore, dormantAfter));
            }

            // All grading precedes using this batch to propose the next forecast.
            if (!complete)
            {
                if (hasPending)
                    outcome = C.G(Relation.UnresolvedRead(packet, pending, available.Carrier),
                        new Placeholder(), new Placeholder(), receipt);
                pending = new Placeholder();
                history = [new Placeholder(), new Placeholder()];
            }
            else
            {
                if (hasPending && occurrences.Length != 0)
                {
                    var issue = (Relation)pendingFields[0];
                    var read = L.Assess(issue, occurrences[0]);
                    var rows = C.Parts(read) is { Length: 3 } a && a[2] is Relation group
                        ? C.Parts(group) : [];
                    var selected = rows.OfType<Relation>().FirstOrDefault(row =>
                        C.Parts(row) is { Length: 6 } r && ReferenceEquals(r[0], pendingFields[2]));
                    if (selected is null || !TryVerdict(selected, out var verdict)) return false;
                    outcome = C.G(read, selected, verdict, receipt);
                    pending = new Placeholder();
                }
                else if (hasPending && Expired((Relation)pendingFields[0],
                    attention is null ? null : (Relation)pendingFields[2], available, out var deadlineRead))
                {
                    // Contiguous, complete empty reports establish absence.
                    // Zero occurrences cannot meet this one-occurrence promise.
                    var count = new Carrier();
                    var missed = OperationClaim.Equality(new Carrier(), new CarrierLocation(count, 0),
                        new CarrierLocation(count, 1), new Carrier());
                    outcome = C.G(C.G(pendingFields[0], deadlineRead, receipt), new Placeholder(), missed, receipt);
                    pending = new Placeholder();
                    absenceMiss = true;
                }
                if (scope is null)
                    foreach (var occurrence in occurrences)
                        history = [history[1], C.Copy(occurrence)];
            }

            // Frozen exported promises have now been graded. The source-local
            // continuity read stages its own chronological checks even when
            // consumer demand is zero. It never receives utility credit.
            if (scope is not null)
            {
                if (!Q.TryAdvance(scope, packet, available, occurrences, complete, out scopeAfter, out var scopeRead))
                { assessment = scopeRead; return false; }
                history = C.Parts((Relation)C.Parts(scopeAfter)[2]);
            }

            CarrierLocation? admitted = null;
            IElement response = new Placeholder();
            if (outcome is Relation result && C.Parts(result) is { Length: 4 } o && o[2] is Relation claim)
            {
                var policy = C.Parts((Relation)p[7]);
                var requested = C.Copy((CarrierLocation)(C.Pass(claim) ? policy[2] : policy[3]));
                var target = N.Value(N.Reframe(N.Sum((supportBefore, 1), (requested, 1)), Support(root)));
                var lower = OperationClaim.AtLeast(new Carrier(), target, policy[0], new Carrier());
                var upper = OperationClaim.AtMost(new Carrier(), target, policy[1], new Carrier());
                response = C.G(requested, lower, upper);
                if (C.Pass(lower) && C.Pass(upper)) { admitted = requested; supportAfter = target; }
                if (attention is not null && !C.Pass(claim))
                    probeAfter = new CarrierLocation(probeBefore.Carrier, 1);
            }

            if (attention is not null)
            {
                var exhausted = A.Zero(supportAfter);
                if (absenceMiss && C.Pass(exhausted))
                    dormantAfter = new CarrierLocation(dormantBefore.Carrier, 1);
                attentionEvidence = C.G(attentionEvidence, outcome,
                    C.G(supportBefore, supportAfter, exhausted), C.G(dormantBefore, dormantAfter),
                    C.G(probeBefore, probeAfter));
            }

            // Read support after its admitted response, before scheduling work.
            // All deltas and projections are staged before the physical batch.
            if (!IsPending(pending) && complete)
            {
                if (attention is null) pending = NewIssue(root, history, available);
                else
                {
                    pending = NewAttentiveIssue(root, attention, scopeAfter, history, available,
                        dormantAfter, probeAfter, out var plannedPhase, out var plannedProbe,
                        out var selectionEvidence);
                    attentionEvidence = C.G(attentionEvidence, selectionEvidence);
                    if (IsPending(pending)) { phaseAfter = plannedPhase; probeAfter = plannedProbe; }
                }
            }

            var cursor = (Carrier)p[10];
            var advance = N.Read(N.Reframe(N.Sum((available, 1), (Available(root), -1)), cursor));
            if (!Equation.TryReadPrediction(advance, out var cursorDelta, out _)) return false;
            // The shared admission cursor prevents an old root from crediting an
            // issue twice. Cursor and support enter one whole-arrival settlement.
            var incoming = new List<IElement> { cursorDelta };
            if (admitted is CarrierLocation delta) incoming.Add(delta);
            if (attention is not null)
            {
                if (!A.TryDelta(phaseAfter, phaseBefore, out var phaseDelta, out var phaseRead) ||
                    !A.TryDelta(dormantAfter, dormantBefore, out var dormantDelta, out var dormantRead) ||
                    !A.TryDelta(probeAfter, probeBefore, out var probeDelta, out var probeRead)) return false;
                incoming.AddRange([phaseDelta, dormantDelta, probeDelta]);
                attentionEvidence = C.G(attentionEvidence, phaseRead, dormantRead, probeRead);
            }
            var admission = Carrier.ReceiveTogether(C.G(incoming.ToArray()));
            if (!SettlementObservation.ObservedQuiet(admission)) return false;
            response = C.G(response, advance, admission);
            if (outcome is Relation oldOutcome)
            {
                var outcomeFields = C.Parts(oldOutcome);
                outcome = C.G(outcomeFields[0], outcomeFields[1], outcomeFields[2], C.G(outcomeFields[3], response));
            }
            else outcome = C.G(new Placeholder(), new Placeholder(), new Placeholder(), C.G(receipt, response));
            assessment = C.G(p[3], outcome, supportBefore, C.Copy(Support(root).ValueSlot), pending, packet);
            var freshTrace = scopeAfter is null ? C.G(history) : (Relation)C.Parts(scopeAfter)[2];
            var retained = new IElement[] { p[0], p[1], freshTrace, pending,
                scopeAfter is null ? C.Copy(available) : available,
                p[5], p[6], p[7], assessment, packet, p[10], p[11] };
            if (attention is null) next = C.G(retained);
            else
            {
                var retainedAttention = A.Retain(attention, C.G(attentionEvidence, admission));
                next = scopeAfter is null ? C.G([.. retained, retainedAttention]) :
                    C.G([.. retained, retainedAttention, scopeAfter]);
            }
            return true;
        }
        catch (OverflowException) { return false; }
    }

    private static bool IsPending(IElement element) => element is Relation relation &&
        C.Parts(relation) is { Length: 4 } p && p[0] is Relation issue && L.Rungs(issue).Length > 0;

    private static IElement NewAttentiveIssue(Relation root, Relation attention, Relation? scope, IElement[] history,
        CarrierLocation now, CarrierLocation dormant, CarrierLocation requestedProbe,
        out CarrierLocation phaseAfter, out CarrierLocation probeAfter, out Relation selectionEvidence)
    {
        phaseAfter = A.Sample(attention, 0);
        probeAfter = requestedProbe;
        var demand = C.Copy(Demand(root).ValueSlot);
        var demandPressure = C.Copy(Demand(root).LiveTensionSlot);
        var quietDemand = A.Zero(demandPressure);
        var positive = A.On(demand);
        var awake = A.Zero(dormant);
        selectionEvidence = C.G(demand, dormant, positive, awake, demandPressure, quietDemand);
        if (!C.Pass(quietDemand))
            return Relation.UnresolvedRead(C.G(demand, demandPressure), selectionEvidence, now.Carrier);
        // Demand is a computation gate. No cadence prediction or ladder rung is
        // constructed while withdrawn, dormant or without two fresh sites.
        if (!C.Pass(positive) || !C.Pass(awake) || history.Length != 2 ||
            history[0] is not CarrierLocation earlier || history[1] is not CarrierLocation latest ||
            !StrictlyAfter(latest, earlier)) return new Placeholder();
        CarrierLocation expected = default;
        if (scope is not null)
        {
            if (!Q.TryExpected(scope, out expected))
            {
                selectionEvidence = C.G(selectionEvidence, C.Parts(scope)[3], C.Parts(scope)[4]);
                return new Placeholder();
            }
            // An allowance can remain open after its center. Wait for evidence;
            // do not jump over the unresolved occurrence to a later cycle.
            var nextTickCircuit = N.Reframe(N.Sum((now, 1), (N.Constant(1), 1)), now.Carrier);
            var nextTickRead = N.Read(nextTickCircuit);
            if (!Equation.TryReadPrediction(nextTickRead, out var nextTick, out _))
            {
                selectionEvidence = C.G(selectionEvidence, nextTickCircuit, nextTickRead);
                return Relation.UnresolvedRead(now, selectionEvidence, now.Carrier);
            }
            var future = OperationClaim.AtLeast(new Carrier(), expected, nextTick, new Carrier());
            selectionEvidence = C.G(selectionEvidence, C.Parts(scope)[4], nextTickCircuit, nextTickRead, future);
            if (!C.Pass(future)) return new Placeholder();
        }
        if (!A.TrySelect(attention, Ladder(root), demand, requestedProbe, out var selectedLadder,
                out phaseAfter, out probeAfter, out var selection))
        {
            selectionEvidence = C.G(selectionEvidence, selection);
            return Relation.UnresolvedRead(demand, selectionEvidence, now.Carrier);
        }
        selectionEvidence = C.G(selectionEvidence, selection);
        if (scope is not null)
        {
            // Connect the qualified candidate and continuation, not the whole
            // scope or its batch assessment, to avoid a historical root chain.
            var scopedEvidence = C.G(C.Parts(scope)[3], C.Parts(scope)[4], selectionEvidence);
            var scopedIssue = L.Issue(selectedLadder, now, expected, scopedEvidence);
            var scopedRows = L.Rungs(scopedIssue);
            return scopedRows.Length == 0 ? scopedIssue : C.G(scopedIssue, demand, scopedRows[0], scopedEvidence);
        }
        var period = N.Reframe(N.Sum((C.Copy(latest), 1), (C.Copy(earlier), -1)), now.Carrier);
        var age = N.Reframe(N.Sum((C.Copy(now), 1), (C.Copy(latest), -1)), now.Carrier);
        var cycles = N.Divide(age, period);
        var nextCycle = N.Sum((cycles, 1), (N.Constant(1), 1));
        var forecast = N.Reframe(N.Sum((C.Copy(latest), 1), (N.Product(nextCycle, period), 1)), now.Carrier);
        var read = N.Read(forecast);
        if (!Equation.TryReadPrediction(read, out var predicted, out _))
            return Relation.UnresolvedRead(C.G(history), forecast, now.Carrier);
        var evidence = C.G(C.G(C.Copy(earlier), C.Copy(latest)), forecast, read, selectionEvidence);
        var issue = L.Issue(selectedLadder, now, predicted, evidence);
        var rows = L.Rungs(issue);
        if (rows.Length == 0) return issue;
        // Selection order is demanded map first, optional finer diagnostic next.
        return C.G(issue, demand, rows[0], C.G(evidence, selectionEvidence));
    }

    private static IElement NewIssue(Relation root, IElement[] history, CarrierLocation now)
    {
        if (history.Length != 2 || history[0] is not CarrierLocation earlier ||
            history[1] is not CarrierLocation latest || !StrictlyAfter(latest, earlier)) return new Placeholder();
        var period = N.Reframe(N.Sum((C.Copy(latest), 1), (C.Copy(earlier), -1)), now.Carrier);
        var age = N.Reframe(N.Sum((C.Copy(now), 1), (C.Copy(latest), -1)), now.Carrier);
        var cycles = N.Divide(age, period);
        var nextCycle = N.Sum((cycles, 1), (N.Constant(1), 1));
        var forecast = N.Reframe(N.Sum((C.Copy(latest), 1), (N.Product(nextCycle, period), 1)), now.Carrier);
        var read = N.Read(forecast);
        if (!Equation.TryReadPrediction(read, out var predicted, out _))
            return Relation.UnresolvedRead(C.G(history), forecast, now.Carrier);
        var evidence = C.G(C.G(C.Copy(earlier), C.Copy(latest)), forecast, read);
        var issue = L.Issue(Ladder(root), now, predicted, evidence);
        if (C.Parts(issue).Length != 4) return issue;
        var demand = C.Copy(Demand(root).ValueSlot);
        var demandRead = N.Read(N.Reframe(demand, now.Carrier));
        if (!Equation.TryReadPrediction(demandRead, out var wanted, out _)) return demandRead;
        IElement selected = new Placeholder();
        foreach (var row in L.Rungs(issue))
        {
            var r = C.Parts(row);
            if (r.Length != 4 || r[1] is not Relation cell) continue;
            var width = C.Length(cell);
            var match = OperationClaim.Equality(new Carrier(), width, wanted, new Carrier());
            if (C.Pass(match)) { selected = row; break; }
        }
        if (selected is Placeholder)
            return Relation.UnresolvedRead(demand, issue, now.Carrier);
        return C.G(issue, demand, selected, C.G(evidence, demandRead));
    }

    private static bool TryVerdict(Relation row, out Relation verdict)
    {
        verdict = Relation.UnresolvedRead(row, row, Carrier.Unknown);
        var r = C.Parts(row);
        if (r.Length != 6) return false;
        var residuals = new List<(IElement Value, long Coefficient)>();
        for (var i = 1; i <= 3; i++)
        {
            if (r[i] is not Relation claim || !OperationClaim.TryReadResolvedViolation(claim, out var violation))
                return false;
            residuals.Add((violation, 1));
        }
        var circuit = N.Sum(residuals.ToArray());
        var read = N.Read(circuit);
        if (!Equation.TryReadPrediction(read, out var combined, out _)) return false;
        verdict = OperationClaim.Equality(new Carrier(), combined, new CarrierLocation(combined.Carrier, 0), new Carrier());
        return true;
    }

    private static bool Expired(Relation issue, Relation? selected, CarrierLocation available, out Relation evidence)
    {
        Relation[] rows = selected is null ? L.Rungs(issue) : [selected];
        var claims = new List<IElement>();
        var all = rows.Length > 0;
        foreach (var row in rows)
        {
            var r = C.Parts(row);
            if (r.Length != 4 || r[1] is not Relation cell ||
                !TimeStepSequence.TryReadSpan(cell, out _, out var end)) { all = false; continue; }
            var claim = OperationClaim.AtMost(new Carrier(), end, available, new Carrier());
            claims.Add(claim);
            all &= C.Pass(claim);
        }
        evidence = C.G(claims.ToArray());
        return all;
    }

    private static bool TryReport(Relation root, Relation packet, out CarrierLocation available,
        out CarrierLocation[] occurrences, out bool complete, out Relation receipt)
    {
        available = default;
        occurrences = [];
        complete = false;
        receipt = Relation.UnresolvedRead(packet, Port(root), Available(root).Carrier);
        var p = C.Parts(packet);
        var port = C.Parts(Port(root));
        if (p.Length != 6 || port.Length != 3 || !ReferenceEquals(p[0], Port(root)) ||
            p[1] is not Relation span || !TimeStepSequence.TryReadSpan(span, out var start, out var end) ||
            p[2] is not CarrierLocation stamp || !ReferenceEquals(stamp.Carrier, port[0]) ||
            !ReferenceEquals(start.Carrier, port[0]) || !ReferenceEquals(end.Carrier, port[0]) ||
            p[4] is not CarrierLocation level || !ReferenceEquals(level.Carrier, port[1])) return false;
        var state = C.Parts(root);
        var cursorRead = Equation.Predict((Relation)state[11], C.Copy(((Carrier)state[10]).ValueSlot), stamp.Carrier);
        if (!Equation.TryReadPrediction(cursorRead, out var current, out _)) return false;
        var currentVersion = OperationClaim.Equality(new Carrier(), Available(root), current, new Carrier());
        var contiguous = OperationClaim.Equality(new Carrier(), start, Available(root), new Carrier());
        var atEnd = OperationClaim.Equality(new Carrier(), stamp, end, new Carrier());
        receipt = C.G(packet, cursorRead, currentVersion, contiguous, atEnd);
        if (!C.Pass(currentVersion) || !C.Pass(contiguous) || !C.Pass(atEnd) || !StrictlyAfter(stamp, start)) return false;
        available = C.Copy(stamp);
        if (p[3] is Placeholder) return true;
        if (p[3] is not Relation group || !group.TryReadOrderedGroup(out var items)) return false;
        var observed = new List<CarrierLocation>();
        var previous = start;
        var evidence = new List<IElement> { cursorRead, currentVersion, contiguous, atEnd };
        foreach (var item in items)
        {
            if (item is not Relation point || !TimeStepSequence.TryReadSpan(point, out var a, out var b) ||
                !ReferenceEquals(a.Carrier, stamp.Carrier) || !ReferenceEquals(b.Carrier, stamp.Carrier)) return false;
            var isPoint = OperationClaim.Equality(new Carrier(), a, b, new Carrier());
            var notFuture = OperationClaim.AtMost(new Carrier(), a, stamp, new Carrier());
            evidence.Add(C.G(point, isPoint, notFuture));
            if (!C.Pass(isPoint) || !C.Pass(notFuture) || !StrictlyAfter(a, previous)) return false;
            observed.Add(C.Copy(a));
            previous = a;
        }
        occurrences = observed.ToArray();
        // A concrete collection can still be a partial observation. In this
        // narrow port contract, only an empty exposed residue certifies full
        // detector coverage. Retain partial events in the packet for inspection.
        complete = p[5] is Relation residue && residue.TryReadOrderedGroup(out var excluded) && excluded.Length == 0;
        receipt = C.G(packet, C.G(evidence.ToArray()));
        return true;
    }

    private static bool StrictlyAfter(CarrierLocation a, CarrierLocation b)
    {
        if (!ReferenceEquals(a.Carrier, b.Carrier)) return false;
        var ordered = OperationClaim.AtLeast(new Carrier(), a, b, new Carrier());
        var equal = OperationClaim.Equality(new Carrier(), a, b, new Carrier());
        return C.Pass(ordered) && OperationClaim.TryReadResolvedViolation(equal, out var difference) &&
            C.Pass(OperationClaim.AtLeast(new Carrier(), difference, new CarrierLocation(difference.Carrier, 1), new Carrier()));
    }
}
