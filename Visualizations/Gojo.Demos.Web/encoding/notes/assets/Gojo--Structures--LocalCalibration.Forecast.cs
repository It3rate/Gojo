using Gojo.Core;
using Scalar = Gojo.Core.ExactScalar;

namespace Gojo.Structures;

public static partial class LocalCalibration
{
    /// <summary>
    /// Appends group(state, observation, local arrival event) to one selected
    /// source stream. Result holds copied observation coordinates and arrival.
    /// Open values are admitted as pending. Source times and local events must
    /// each advance; these axes are never compared to each other.
    /// </summary>
    public static bool TryRecordTemporalSample(Relation request, out Relation receipt, out Relation nextState)
    {
        nextState = Group();
        if (!Fields(request, 3, out var r) || r[0] is not Relation previous)
            return Unresolved(request, out receipt);
        nextState = previous;
        if (!TemporalState(previous, out var state, out var stream, out var samples, out _, out _) ||
            !NextLocalEvent(state, stream, r[2], out var arrival) ||
            !StreamObservation(r[1], state[0], out var o, out var time) ||
            samples.Length != 0 && (!TemporalReceipt(samples[^1], state[0], out _, out _, out var last, out _) ||
                time.Ticks <= last.Ticks))
            return Unresolved(request, out receipt);

        IElement sample = o[2];
        if (TryScalar(sample, out var basis, out var value)) sample = value.Encode(basis);
        var copied = Group(o[0], Copy(time), sample, o[3], o[4]);
        receipt = ReadWitness.Create(new Carrier(), r[1], request, Group(copied, Copy(arrival)),
            sample is Placeholder ? r[1] : Group(), state[0]);
        nextState = Group(state[0], Copy(arrival), Group([.. samples, receipt]), state[3], state[4]);
        return true;
    }

    /// <summary>
    /// Fits group(state, first receipt, second receipt, encoding frame).
    /// Two available observations supply an affine source-time to encoded-value
    /// frame. The affine family is selected, not discovered. No forecast is
    /// implied by an in-sample fit. Numeric endpoints are copied.
    /// </summary>
    public static bool TryFitTemporalFrame(Relation request, out Relation fit)
    {
        if (!Fields(request, 4, out var r) ||
            !TemporalState(r[0], out var state, out _, out var samples, out _, out _) ||
            !Contains(samples, r[1]) || !Contains(samples, r[2]) ||
            !TemporalReceipt(r[1], state[0], out _, out var first, out var t0, out _) ||
            !TemporalReceipt(r[2], state[0], out _, out var second, out var t1, out _) || t0.Ticks >= t1.Ticks ||
            !TryScalar(first[2], out var input, out var x0) ||
            !TryScalar(second[2], out var other, out var x1) || !input.SameIdentityAs(other) ||
            !Map(r[3], out var source, out var a, out var b, out var encoded, out var c, out var d) ||
            !source.SameIdentityAs(input) || c == d)
            return Unresolved(request, out fit);

        try
        {
            var y0 = c + (x0 - a) * (d - c) / (b - a);
            var y1 = c + (x1 - a) * (d - c) / (b - a);
            var frame = new Relation(Relation.Segment(t0.Carrier, t0.Ticks, t1.Ticks),
                new Relation(y0.Encode(encoded), y1.Encode(encoded)));
            fit = ReadWitness.Create(new Carrier(), Group(r[1], r[2]), request, frame, Group(), r[3]);
            return true;
        }
        catch (OverflowException) { return Unresolved(request, out fit); }
    }

    /// <summary>
    /// Issues group(state, candidate, target source time, local issue event).
    /// Candidate is group(fit witness, readout frame, expectation). The target
    /// must be beyond all currently received samples. Result captures both
    /// numeric frames and their exposed prediction before feedback is admitted.
    /// </summary>
    public static bool TryIssueForecast(Relation request, out Relation issue, out Relation nextState)
    {
        nextState = Group();
        if (!Fields(request, 4, out var r) || r[0] is not Relation previous)
            return Unresolved(request, out issue);
        nextState = previous;
        if (!TemporalState(previous, out var state, out var stream, out var samples, out var issues, out _) ||
            !NextLocalEvent(state, stream, r[3], out var issued) ||
            !PublicationPort(stream[0], out var output, out var sourceClock) ||
            !SequenceEvent(r[2], sourceClock, out var targetTime) || samples.Length == 0 ||
            !TemporalReceipt(samples[^1], state[0], out _, out _, out var latest, out _) ||
            targetTime.Ticks <= latest.Ticks || !Fields(r[1], 3, out var candidate) ||
            !Fields(candidate[0], 5, out var fitted) || fitted[1] is not Relation fitRequest ||
            !Fields(fitRequest, 4, out var fr) || !TemporalPrefix(fr[0], state) ||
            !Contains(samples, fr[1]) || !Contains(samples, fr[2]) ||
            !TryFitTemporalFrame(fitRequest, out var checkedFit) || !Fields(checkedFit, 5, out var cf) ||
            !FrameEqual(fitted[2], cf[2]) ||
            !Map(fitted[2], out var timeBasis, out var a, out var b, out var latent, out var c, out var d) ||
            !timeBasis.SameIdentityAs(sourceClock) ||
            !Map(candidate[1], out var readInput, out var ra, out var rb, out var readOutput, out var rc, out var rd) ||
            !readInput.SameIdentityAs(latent) || !readOutput.SameIdentityAs(output) || rc == rd)
            return Unresolved(request, out issue);

        try
        {
            var internalValue = c + (new Scalar(targetTime.Ticks) - a) * (d - c) / (b - a);
            var prediction = rc + (internalValue - ra) * (rd - rc) / (rb - ra);
            var frames = Group(CopyFrame(fitted[2]), CopyFrame(candidate[1]));
            var forecast = Group(state[0], Copy(issued), Copy(targetTime), frames,
                prediction.Encode(output), candidate[2]);
            var residue = c == d ? Group(new Relation(Copy(targetTime), CopyFrame(fitted[2]))) : Group();
            issue = ReadWitness.Create(new Carrier(), r[1], request, forecast, residue,
                Group(candidate[0], internalValue.Encode(latent)));
            nextState = Group(state[0], Copy(issued), state[2], Group([.. issues, issue]), state[4]);
            return true;
        }
        catch (OverflowException) { return Unresolved(request, out issue); }
    }

    /// <summary>
    /// Assesses group(state, issue, receipt, local assessment event). The selected
    /// receipt must match the forecast's exact target and arrive after issuance.
    /// One assessment per issue along the supplied sequential state. Result is
    /// observed minus predicted, not truth, reward, confidence or material delta.
    /// </summary>
    public static bool TryAssessForecast(Relation request, out Relation assessment, out Relation nextState)
    {
        nextState = Group();
        if (!Fields(request, 4, out var r) || r[0] is not Relation previous)
            return Unresolved(request, out assessment);
        nextState = previous;
        if (!TemporalState(previous, out var state, out var stream, out var samples, out var issues, out var assessed) ||
            !NextLocalEvent(state, stream, r[3], out var now) || !Contains(issues, r[1]) || !Contains(samples, r[2]) ||
            assessed.Any(a => Fields(a, 5, out var af) && Same(af[0], r[1])) ||
            !IssuedForecast(r[1], state[0], out var forecast, out var issued, out var target, out var prediction) ||
            !TemporalReceipt(r[2], state[0], out _, out var o, out var sampled, out var arrival) ||
            target.Ticks != sampled.Ticks || arrival.Ticks <= issued.Ticks ||
            !TryScalar(o[2], out var output, out var observed))
            return Unresolved(request, out assessment);

        try
        {
            Fields(r[1], 5, out var issuedWitness);
            assessment = ReadWitness.Create(new Carrier(), r[1], request,
                (observed - prediction).Encode(output), issuedWitness[3],
                Group(new Relation(forecast[2], o[1]), r[2],
                    Relation.Segment(now.Carrier, issued.Ticks, now.Ticks)));
            nextState = Group(state[0], Copy(now), state[2], state[3], Group([.. assessed, assessment]));
            return true;
        }
        catch (OverflowException) { return Unresolved(request, out assessment); }
    }

    private static bool TemporalState(IElement element, out IElement[] state, out IElement[] stream,
        out IElement[] samples, out IElement[] issues, out IElement[] assessed)
    {
        stream = samples = issues = assessed = [];
        if (!Fields(element, 5, out state) || !Fields(state[0], 4, out stream) ||
            !PublicationPort(stream[0], out _, out _) || stream[1] is not Placeholder { IsInverse: false } ||
            stream[3] is not Carrier { IsUnknown: false, IsInverse: false } local ||
            state[2] is not Relation sg || !sg.TryReadOrderedGroup(out samples) ||
            state[3] is not Relation ig || !ig.TryReadOrderedGroup(out issues) ||
            state[4] is not Relation ag || !ag.TryReadOrderedGroup(out assessed)) return false;

        if (state[1] is Placeholder { IsInverse: false })
            return samples.Length == 0 && issues.Length == 0 && assessed.Length == 0;
        if (!SequenceEvent(state[1], local, out var last)) return false;
        var events = new HashSet<long>(); // Derived index over the connected local history.
        long priorTime = -1, priorArrival = -1;
        foreach (var sample in samples)
        {
            if (!TemporalReceipt(sample, state[0], out _, out _, out var time, out var arrival) ||
                time.Ticks <= priorTime || arrival.Ticks <= priorArrival || arrival.Ticks > last.Ticks) return false;
            priorTime = time.Ticks; priorArrival = arrival.Ticks;
            events.Add(arrival.Ticks);
        }
        long priorIssue = -1;
        foreach (var issue in issues)
        {
            if (!IssuedForecast(issue, state[0], out _, out var issued, out var target, out _) ||
                issued.Ticks <= priorIssue || issued.Ticks > last.Ticks || !events.Add(issued.Ticks))
                return false;
            foreach (var sample in samples)
            {
                TemporalReceipt(sample, state[0], out _, out _, out var time, out var arrival);
                if (arrival.Ticks < issued.Ticks && time.Ticks >= target.Ticks) return false;
            }
            priorIssue = issued.Ticks;
        }
        long priorAssessment = -1;
        var consumed = new List<IElement>();
        foreach (var assessment in assessed)
        {
            if (!Fields(assessment, 5, out var af) || !Contains(issues, af[0]) ||
                consumed.Any(e => Same(e, af[0])) || !Fields(af[1], 4, out var ar) || !Same(ar[1], af[0]) ||
                !Contains(samples, ar[2]) || !Fields(af[4], 3, out var provenance) ||
                provenance[2] is not Relation span || !SequenceEvent(span.Right, local, out var evaluated) ||
                evaluated.Ticks <= priorAssessment || evaluated.Ticks > last.Ticks || !events.Add(evaluated.Ticks) ||
                !IssuedForecast(af[0], state[0], out _, out var issued, out var target, out var predicted) ||
                !TemporalReceipt(ar[2], state[0], out _, out var o, out var time, out var arrival) ||
                time.Ticks != target.Ticks || arrival.Ticks <= issued.Ticks || evaluated.Ticks <= arrival.Ticks ||
                !TryScalar(o[2], out var basis, out var actual) ||
                !TryScalar(af[2], out var resultBasis, out var difference) || !basis.SameIdentityAs(resultBasis) ||
                difference != actual - predicted) return false;
            consumed.Add(af[0]); priorAssessment = evaluated.Ticks;
        }
        return events.Count != 0 && events.Max() == last.Ticks;
    }

    private static bool NextLocalEvent(IElement[] state, IElement[] stream, IElement element, out CarrierLocation stamp) =>
        SequenceEvent(element, (Carrier)stream[3], out stamp) &&
        (state[1] is Placeholder || Site(state[1], out var last) && stamp.Ticks > last.Ticks);

    private static bool StreamObservation(IElement element, IElement streamElement, out IElement[] observation,
        out CarrierLocation time)
    {
        time = default;
        return Observation(element, out observation) && Fields(streamElement, 4, out var stream) &&
            Same(observation[0], stream[0]) && Same(observation[3], stream[1]) && Same(observation[4], stream[2]) &&
            PublicationPort(stream[0], out var output, out var clock) && SequenceEvent(observation[1], clock, out time) &&
            (observation[2] is Placeholder { IsInverse: false } ||
                TryScalar(observation[2], out var basis, out _) && basis.SameIdentityAs(output));
    }

    private static bool TemporalReceipt(IElement element, IElement stream, out IElement[] receipt,
        out IElement[] observation, out CarrierLocation time, out CarrierLocation arrival)
    {
        observation = []; time = arrival = default;
        return Fields(element, 5, out receipt) && Same(receipt[4], stream) && Fields(receipt[2], 2, out var result) &&
            StreamObservation(result[0], stream, out observation, out time) && Fields(stream, 4, out var s) &&
            SequenceEvent(result[1], (Carrier)s[3], out arrival);
    }

    private static bool IssuedForecast(IElement element, IElement stream, out IElement[] forecast,
        out CarrierLocation issued, out CarrierLocation target, out Scalar prediction)
    {
        forecast = []; issued = target = default; prediction = Scalar.Zero;
        if (!Fields(element, 5, out var witness) || !Fields(witness[2], 6, out forecast) || !Same(forecast[0], stream) ||
            !Fields(stream, 4, out var s) || !PublicationPort(s[0], out var output, out var clock) ||
            !SequenceEvent(forecast[1], (Carrier)s[3], out issued) || !SequenceEvent(forecast[2], clock, out target) ||
            !TryScalar(forecast[4], out var basis, out prediction) || !basis.SameIdentityAs(output) ||
            !Fields(forecast[3], 2, out var frames) ||
            !Map(frames[0], out var t, out var a, out var b, out var z, out var c, out var d) ||
            !t.SameIdentityAs(clock) ||
            !Map(frames[1], out var zi, out var ra, out var rb, out var q, out var rc, out var rd) ||
            !zi.SameIdentityAs(z) || !q.SameIdentityAs(output) || rc == rd) return false;
        var internalValue = c + (new Scalar(target.Ticks) - a) * (d - c) / (b - a);
        return prediction == rc + (internalValue - ra) * (rd - rc) / (rb - ra);
    }

    private static bool Contains(IElement[] elements, IElement element) => elements.Any(e => Same(e, element));

    private static bool TemporalPrefix(IElement prior, IElement[] current)
    {
        if (!TemporalState(prior, out var p, out _, out _, out _, out _) || !Same(p[0], current[0])) return false;
        if (p[1] is not Placeholder && (!Site(p[1], out var before) || !Site(current[1], out var after) ||
            before.Ticks > after.Ticks)) return false;
        for (var index = 2; index < 5; index++)
        {
            ((Relation)p[index]).TryReadOrderedGroup(out var earlier);
            ((Relation)current[index]).TryReadOrderedGroup(out var later);
            if (earlier.Length > later.Length) return false;
            for (var n = 0; n < earlier.Length; n++) if (!Same(earlier[n], later[n])) return false;
        }
        return true;
    }

    private static CarrierLocation Copy(CarrierLocation site) => new(site.Carrier, site.Ticks);
    private static Relation CopyFrame(IElement element)
    {
        Map(element, out var source, out var a, out var b, out var target, out var c, out var d);
        return new Relation(new Relation(a.Encode(source), b.Encode(source)), new Relation(c.Encode(target), d.Encode(target)));
    }
    private static bool FrameEqual(IElement first, IElement second) =>
        Map(first, out var s1, out var a1, out var b1, out var t1, out var c1, out var d1) &&
        Map(second, out var s2, out var a2, out var b2, out var t2, out var c2, out var d2) &&
        s1.SameIdentityAs(s2) && t1.SameIdentityAs(t2) && a1 == a2 && b1 == b2 && c1 == c2 && d1 == d2;
}
