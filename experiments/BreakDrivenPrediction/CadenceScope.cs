using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using C = BreakDrivenPrediction.Continuation;
using N = BreakDrivenPrediction.NativeCircuit;

namespace BreakDrivenPrediction;

/// <summary>
/// A remembered local interval and its currently qualified continuation are
/// separate relation surfaces. These reads stage structure only; the enclosing
/// learner owns admission. Timing slack is supplied permission, not confidence.
/// </summary>
public static class CadenceScope
{
    // Scope7: public port, slack span, fresh trace, remembered candidate/open,
    // active continuation/open, availability, latest local assessment.
    // Candidate3: two frozen event grounds, interval equation, interval read.
    // Continuation4: candidate, expected center, closed window, native evidence.
    internal static Relation Create(Relation port, Relation trace, CarrierLocation available, long slackTicks) =>
        C.G(port, Relation.Segment(available.Carrier, 0, slackTicks), trace,
            new Placeholder(), new Placeholder(), available, C.G());

    public static bool TryInterval(Relation scope, out CarrierLocation interval)
    {
        interval = default;
        return C.Parts(scope) is { Length: 7 } p && TryCandidate(p[3], out interval);
    }

    public static bool TryExpected(Relation scope, out CarrierLocation expected)
    {
        expected = default;
        return C.Parts(scope) is { Length: 7 } p &&
            TryContinuation(p[4], out expected, out _, out _);
    }

    public static IElement Window(Relation scope) => C.Parts(scope) is { Length: 7 } p &&
        TryContinuation(p[4], out _, out _, out _) ? C.Parts((Relation)p[4])[2] : new Placeholder();

    // Assessment6: packet, event steps, empty-tail reads, unknown reads,
    // final candidate, final continuation. Each collection is bounded by this
    // report only. Diagnostic reads do not earn prospective utility credit.
    // Event step6: prior continuation/open, arrival, bound claims,
    // resulting candidate/open, resulting continuation/open, fresh trace.
    // Tail read4: prior continuation, covered tail span, inclusive deadline
    // claim, resulting continuation/open.
    public static Relation[] EventBreaks(Relation scope) => Rows(scope, 1).Where(step =>
        C.Parts(step) is { Length: 6 } p && TryContinuation(p[0], out _, out _, out _) &&
        p[2] is Relation claims && C.Parts(claims) is { Length: 2 } checks &&
        checks.Any(check => check is Relation claim &&
            OperationClaim.TryReadResolvedViolation(claim, out _) && !C.Pass(claim))).ToArray();

    public static Relation[] AbsenceBreaks(Relation scope) => Rows(scope, 2).Where(step =>
        C.Parts(step) is { Length: 4 } p && p[2] is Relation claim && C.Pass(claim)).ToArray();

    public static Relation[] UnknownOpens(Relation scope) => Rows(scope, 3);

    private static Relation[] Rows(Relation scope, int index) =>
        C.Parts(scope) is { Length: 7 } p && p[6] is Relation assessment &&
        C.Parts(assessment) is { Length: 6 } a && a[index] is Relation group
            ? C.Parts(group).OfType<Relation>().ToArray() : [];

    /// <summary>
    /// Takes fields already validated by the learner's public port and version
    /// reader. The bool is that reader's local completeness result, never stored
    /// model state. All generated calculations and temporal claims remain native.
    /// </summary>
    internal static bool TryAdvance(Relation scope, Relation packet, CarrierLocation available,
        CarrierLocation[] occurrences, bool complete, out Relation next, out Relation assessment)
    {
        next = scope;
        assessment = Relation.UnresolvedRead(packet, scope, available.Carrier);
        var p = C.Parts(scope);
        if (p.Length != 7 || p[2] is not Relation fresh || p[1] is not Relation slackSpan ||
            p[5] is not CarrierLocation previous || !ReferenceEquals(previous.Carrier, available.Carrier) ||
            C.Parts(fresh) is not { Length: 2 }) return false;
        var trace = fresh;
        IElement candidate = p[3], continuation = p[4];
        var eventSteps = new List<IElement>();
        var tailSteps = new List<IElement>();
        var unknownSteps = new List<IElement>();
        var tailStart = previous;
        try
        {
            if (!complete)
            {
                unknownSteps.Add(Relation.UnresolvedRead(packet, C.G(candidate, continuation), available.Carrier));
                continuation = new Placeholder();
                trace = C.G(new Placeholder(), new Placeholder());
            }
            else
            {
                foreach (var occurrence in occurrences)
                {
                    var arrival = C.Copy(occurrence);
                    var priorContinuation = continuation;
                    var checks = C.G();
                    if (TryContinuation(continuation, out _, out var lower, out var upper))
                    {
                        var lowerClaim = OperationClaim.AtLeast(new Carrier(), arrival, lower, new Carrier());
                        var upperClaim = OperationClaim.AtMost(new Carrier(), arrival, upper, new Carrier());
                        if (!OperationClaim.TryReadResolvedViolation(lowerClaim, out _) ||
                            !OperationClaim.TryReadResolvedViolation(upperClaim, out _)) return false;
                        checks = C.G(lowerClaim, upperClaim);
                        if (!C.Pass(lowerClaim) || !C.Pass(upperClaim))
                        {
                            // Break before learning the out-of-window event.
                            // It is the first new ground, never the far endpoint
                            // of the old remembered interval.
                            continuation = new Placeholder();
                            trace = C.G(new Placeholder(), new Placeholder());
                        }
                    }

                    var grounds = C.Parts(trace);
                    trace = C.G(grounds[1], arrival);
                    if (grounds[1] is CarrierLocation earlier)
                    {
                        if (!TryBuild(earlier, arrival, slackSpan, out var builtCandidate,
                                out var builtContinuation, out var buildRead))
                        { assessment = buildRead; return false; }
                        candidate = builtCandidate;
                        continuation = builtContinuation;
                    }
                    eventSteps.Add(C.G(priorContinuation, arrival, checks, candidate, continuation, trace));
                    tailStart = arrival;
                }

                if (TryContinuation(continuation, out _, out _, out var deadline))
                {
                    var priorContinuation = continuation;
                    var exhausted = OperationClaim.AtLeast(new Carrier(), available, deadline, new Carrier());
                    if (!OperationClaim.TryReadResolvedViolation(exhausted, out _)) return false;
                    if (C.Pass(exhausted))
                    {
                        continuation = new Placeholder();
                        trace = C.G(new Placeholder(), new Placeholder());
                    }
                    tailSteps.Add(C.G(priorContinuation, Span(tailStart, available), exhausted, continuation));
                }
            }

            assessment = C.G(packet, C.G(eventSteps.ToArray()), C.G(tailSteps.ToArray()),
                C.G(unknownSteps.ToArray()), candidate, continuation);
            // Preserve one candidate and this batch's finite evidence, never
            // the prior scope, assessment or root. These exact trace and time
            // sites are also returned as the enclosing learner's fields 2/4.
            next = C.G(p[0], p[1], trace, candidate, continuation, available, assessment);
            return true;
        }
        catch (OverflowException) { return false; }
    }

    private static bool TryBuild(CarrierLocation earlier, CarrierLocation latest, Relation slackSpan,
        out Relation candidate, out Relation continuation, out Relation evidence)
    {
        candidate = continuation = evidence = Relation.UnresolvedRead(earlier, latest, latest.Carrier);
        var a = C.Copy(earlier);
        var b = C.Copy(latest);
        var intervalCircuit = N.Reframe(N.Sum((b, 1), (a, -1)), b.Carrier);
        var intervalRead = N.Read(intervalCircuit);
        if (!Equation.TryReadPrediction(intervalRead, out var interval, out _)) return false;
        var positive = OperationClaim.AtLeast(new Carrier(), interval, new CarrierLocation(b.Carrier, 1), new Carrier());
        if (!C.Pass(positive)) { evidence = C.G(intervalCircuit, intervalRead, positive); return false; }
        var slack = C.Length(slackSpan);
        var permitted = OperationClaim.AtLeast(new Carrier(), slack, new CarrierLocation(b.Carrier, 0), new Carrier());
        if (!C.Pass(permitted)) { evidence = C.G(slackSpan, permitted); return false; }
        var expectedCircuit = N.Reframe(N.Sum((b, 1), (interval, 1)), b.Carrier);
        var lowerCircuit = N.Reframe(N.Sum((expectedCircuit, 1), (slack, -1)), b.Carrier);
        var upperCircuit = N.Reframe(N.Sum((expectedCircuit, 1), (slack, 1)), b.Carrier);
        var reads = N.ReadMany(expectedCircuit, lowerCircuit, upperCircuit);
        evidence = C.G(positive, permitted, new Relation(expectedCircuit, reads[0]),
            new Relation(lowerCircuit, reads[1]), new Relation(upperCircuit, reads[2]));
        if (!Equation.TryReadPrediction(reads[0], out var expected, out _) ||
            !Equation.TryReadPrediction(reads[1], out var lower, out _) ||
            !Equation.TryReadPrediction(reads[2], out var upper, out _)) return false;
        candidate = C.G(C.G(a, b), intervalCircuit, intervalRead);
        continuation = C.G(candidate, expected, Span(lower, upper), C.G(slackSpan, evidence));
        return true;
    }

    private static bool TryCandidate(IElement element, out CarrierLocation interval)
    {
        interval = default;
        return element is Relation candidate && C.Parts(candidate) is { Length: 3 } p &&
            p[0] is Relation grounds && C.Parts(grounds) is { Length: 2 } g &&
            g[0] is CarrierLocation earlier && g[1] is CarrierLocation latest &&
            ReferenceEquals(earlier.Carrier, latest.Carrier) && p[1] is Relation && p[2] is Relation read &&
            Equation.TryReadPrediction(read, out interval, out _) &&
            ReferenceEquals(interval.Carrier, latest.Carrier);
    }

    private static bool TryContinuation(IElement element, out CarrierLocation expected,
        out CarrierLocation lower, out CarrierLocation upper)
    {
        expected = lower = upper = default;
        if (element is not Relation continuation || C.Parts(continuation) is not { Length: 4 } p ||
            !TryCandidate(p[0], out var interval) || p[1] is not CarrierLocation at ||
            p[2] is not Relation span || !TimeStepSequence.TryReadSpan(span, out var from, out var to) ||
            !ReferenceEquals(at.Carrier, interval.Carrier) || !ReferenceEquals(from.Carrier, at.Carrier) ||
            !ReferenceEquals(to.Carrier, at.Carrier) || p[3] is not Relation) return false;
        expected = at;
        lower = from;
        upper = to;
        return true;
    }

    private static Relation Span(CarrierLocation from, CarrierLocation to) =>
        new(new Relation(from, from.Carrier), new Relation(to, to.Carrier));
}
