using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using C = BreakDrivenPrediction.Continuation;
using N = BreakDrivenPrediction.NativeCircuit;
using L = BreakDrivenPrediction.TimingLadder;

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
    {
        var p = C.Parts(port);
        if (p.Length != 3 || p[0] is not Carrier clock || clock.IsUnknown || demandedWidth <= 0)
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
            if (p.Length != 12 || !TryReport(root, packet, out var available, out var occurrences,
                    out var complete, out var receipt)) return false;
            var supportBefore = C.Copy(Support(root).ValueSlot);
            IElement pending = p[3], outcome = new Placeholder();
            var history = Events(root);
            var pendingFields = pending is Relation pendingRelation ? C.Parts(pendingRelation) : [];
            var hasPending = IsPending(pending);

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
                else if (hasPending && Expired((Relation)pendingFields[0], available, out var deadlineRead))
                {
                    // Contiguous, complete empty reports establish absence.
                    // Zero occurrences cannot meet this one-occurrence promise.
                    var count = new Carrier();
                    var missed = OperationClaim.Equality(new Carrier(), new CarrierLocation(count, 0),
                        new CarrierLocation(count, 1), new Carrier());
                    outcome = C.G(C.G(pendingFields[0], deadlineRead, receipt), new Placeholder(), missed, receipt);
                    pending = new Placeholder();
                }
                foreach (var occurrence in occurrences)
                    history = [history[1], C.Copy(occurrence)];
            }

            // A known finer event group is authorized public input. There is no
            // route from the report into the producer's private trace.
            if (!IsPending(pending) && complete)
                pending = NewIssue(root, history, available);

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
                if (C.Pass(lower) && C.Pass(upper)) admitted = requested;
            }

            var cursor = (Carrier)p[10];
            var advance = N.Read(N.Reframe(N.Sum((available, 1), (Available(root), -1)), cursor));
            if (!Equation.TryReadPrediction(advance, out var cursorDelta, out _)) return false;
            // The shared admission cursor prevents an old root from crediting an
            // issue twice. Cursor and support enter one whole-arrival settlement.
            var incoming = admitted is CarrierLocation delta ? C.G(cursorDelta, delta) : C.G(cursorDelta);
            var admission = Carrier.ReceiveTogether(incoming);
            if (!SettlementObservation.ObservedQuiet(admission)) return false;
            response = C.G(response, advance, admission);
            if (outcome is Relation oldOutcome)
            {
                var outcomeFields = C.Parts(oldOutcome);
                outcome = C.G(outcomeFields[0], outcomeFields[1], outcomeFields[2], C.G(outcomeFields[3], response));
            }
            else outcome = C.G(new Placeholder(), new Placeholder(), new Placeholder(), C.G(receipt, response));
            assessment = C.G(p[3], outcome, supportBefore, C.Copy(Support(root).ValueSlot), pending, packet);
            next = C.G(p[0], p[1], C.G(history), pending, C.Copy(available), p[5], p[6], p[7], assessment, packet, p[10], p[11]);
            return true;
        }
        catch (OverflowException) { return false; }
    }

    private static bool IsPending(IElement element) => element is Relation relation &&
        C.Parts(relation) is { Length: 4 } p && p[0] is Relation issue && L.Rungs(issue).Length > 0;

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

    private static bool Expired(Relation issue, CarrierLocation available, out Relation evidence)
    {
        var rows = L.Rungs(issue);
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
