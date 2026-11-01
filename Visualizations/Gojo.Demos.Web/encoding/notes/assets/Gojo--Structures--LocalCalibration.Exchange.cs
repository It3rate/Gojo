using Gojo.Core;
using Scalar = Gojo.Core.ExactScalar;

namespace Gojo.Structures;

public static partial class LocalCalibration
{
    /// <summary>
    /// Publishes group(current state, prediction, port, event, antecedents,
    /// exposed expectation). The witness keeps private premises; only its result
    /// message crosses the boundary. Scalar/event coordinates are copied.
    /// </summary>
    public static bool TryPublish(Relation request, out Relation publication)
    {
        if (!Fields(request, 6, out var r) || !State(r[0], out var state) || state.Tension != 0 ||
            state.Actuator.Value != (ulong)state.Value || state.Actuator.Tension != 0 ||
            !Prediction(r[1], out var p, out var basis, out var value) || !Same(p[1], state.Fields[2]) ||
            !Observation(p[0], out var o) || !PublicationPort(r[2], out var output, out var clock) ||
            !output.SameIdentityAs(basis) || !SequenceEvent(r[3], clock, out var stamp) || !Antecedents(r[4]))
            return Unresolved(request, out publication);

        var observation = Group(r[2], new CarrierLocation(clock, stamp.Ticks), value.Encode(output), o[3], o[4]);
        publication = ReadWitness.Create(new Carrier(), r[1], request, Group(observation, r[4], r[5]),
            p[3], r[0]);
        return true;
    }

    /// <summary>
    /// Receives group(message, inbox, local arrival event). True means a newer
    /// message was installed, including a well-formed open/pending sample; it
    /// does not mean a numeric comparison is available. Failed admission leaves
    /// the inbox unchanged and retains the request in an unresolved witness.
    /// </summary>
    public static bool TryReceive(Relation request, out Relation receipt, out Relation inbox)
    {
        inbox = Group();
        if (!Fields(request, 3, out var r) || r[1] is not Relation previous)
            return Unresolved(request, out receipt);
        inbox = previous;
        if (!Inbox(previous, out var prior, out var link) ||
            !Message(r[0], out var m, out var o, out var stamp) || !Same(o[0], link[0]) ||
            !PublicationPort(link[1], out var output, out var localClock) ||
            !SequenceEvent(r[2], localClock, out var arrival) ||
            link[2] is not Relation subject || !Same(subject.Left, o[3]) ||
            link[3] is not Relation context || !Same(context.Left, o[4]) ||
            prior[1] is not Placeholder && (!Received(prior[1], prior[0], out _, out var latest) || stamp.Ticks <= latest.Ticks))
            return Unresolved(request, out receipt);

        try
        {
            IElement sample = o[2];
            IElement residue = Group();
            if (sample is Placeholder) residue = r[0];
            else
            {
                TryScalar(sample, out _, out var value);
                Map(link[4], out _, out var a, out var b, out _, out var c, out var d);
                sample = (c + (value - a) * (d - c) / (b - a)).Encode(output);
            }
            var observation = Group(link[1], new CarrierLocation(localClock, arrival.Ticks), sample,
                subject.Right, context.Right);
            receipt = ReadWitness.Create(new Carrier(), r[0], request, observation, residue,
                Group(new Relation(o[0], o[1]), m[1], m[2], prior[0]));
            inbox = Group(prior[0], receipt, prior[2]);
            return true;
        }
        catch (OverflowException) { return Unresolved(request, out receipt); }
    }

    /// <summary>
    /// Applies group(plan, state, application history, count controller, receipt,
    /// inbox). Both local state and current receipt must still match the proposal.
    /// One receipt permits one application in the supplied sequential inbox.
    /// Updated history retains the receipt and inbox correspondence alongside the
    /// existing local signed-tension application; the remote actuator is not read.
    /// </summary>
    public static bool TryApplyReceived(Relation request, out Relation application,
        out Relation history, out Relation inbox)
    {
        history = Group(); inbox = Group();
        if (!Fields(request, 6, out var r) || r[2] is not Relation previousHistory || r[5] is not Relation previousInbox)
            return Unresolved(request, out application);
        history = previousHistory; inbox = previousInbox;
        if (!Inbox(previousInbox, out var current, out _) || !Same(current[1], r[4]) || Same(current[2], r[4]) ||
            !Received(r[4], current[0], out var received, out _) || received[1] is not Relation delivery ||
            !TryReceive(delivery, out var checkedReceipt, out _) ||
            !Observation(received[2], out var observed) || !Fields(checkedReceipt, 5, out var checkedFields) ||
            !Observation(checkedFields[2], out var checkedObservation) || !ScalarEqual(observed[2], checkedObservation[2]) ||
            !Same(observed[0], checkedObservation[0]) || !Same(observed[3], checkedObservation[3]) ||
            !Same(observed[4], checkedObservation[4]) || !ScalarEqual(observed[1], checkedObservation[1]) ||
            !Fields(r[0], 5, out var plan) || !Comparison(plan[0], out var comparison, out _, out _) ||
            !Fields(comparison[1], 4, out var operands) || !Same(operands[1], received[2]))
            return Unresolved(request, out application);

        var nextInbox = Group(current[0], r[4], r[4]);
        if (!TryApplyResponse(Group(r[0], r[1], r[2], r[3]), out var localApplication, out var nextHistory))
            return Unresolved(request, out application);
        Fields(localApplication, 5, out var applied);
        application = ReadWitness.Create(new Carrier(), applied[0], request, applied[2], applied[3],
            Group(applied[4], r[4], previousInbox, nextInbox));
        nextHistory.TryReadOrderedGroup(out var attempts);
        attempts[^1] = application;
        history = Relation.OrderedGroup((Carrier)nextHistory.Right, attempts);
        inbox = nextInbox;
        return true;
    }

    private static bool PublicationPort(IElement element, out Carrier basis, out Carrier clock)
    {
        if (Fields(element, 2, out var p) && Range(p[0], out basis, out var a, out var b) &&
            a.IsZero && b == Scalar.One && p[1] is Carrier { IsUnknown: false, IsInverse: false } time)
        { clock = time; return true; }
        basis = clock = null!; return false;
    }

    private static bool SequenceEvent(IElement element, Carrier clock, out CarrierLocation stamp) =>
        Site(element, out stamp) && stamp.Carrier.SameIdentityAs(clock) && stamp.Ticks >= 0;

    private static bool Antecedents(IElement element) =>
        element is Placeholder { IsInverse: false } ||
        element is Relation r && r.TryReadOrderedGroup(out var refs) && refs.All(reference =>
            reference is Relation edge && PublicationPort(edge.Left, out _, out var clock) &&
            SequenceEvent(edge.Right, clock, out _));

    private static bool Message(IElement element, out IElement[] m, out IElement[] o, out CarrierLocation stamp)
    {
        if (Fields(element, 3, out m) && Observation(m[0], out o) && PublicationPort(o[0], out var basis, out var clock) &&
            SequenceEvent(o[1], clock, out stamp) && Antecedents(m[1]) &&
            (o[2] is Placeholder { IsInverse: false } || TryScalar(o[2], out var b, out _) && b.SameIdentityAs(basis)))
            return true;
        o = []; stamp = default; return false;
    }

    private static bool Link(IElement element, out IElement[] link) =>
        Fields(element, 5, out link) && PublicationPort(link[0], out var remote, out _) &&
        PublicationPort(link[1], out var local, out _) &&
        link[2] is Relation { Left: Placeholder { IsInverse: false }, Right: Placeholder { IsInverse: false } } &&
        link[3] is Relation && Map(link[4], out var source, out _, out _, out var target, out var c, out var d) &&
        source.SameIdentityAs(remote) && target.SameIdentityAs(local) && c != d;

    // Only the selected receipt surface is inspected, never remote prediction ancestry.
    private static bool Received(IElement element, IElement link, out IElement[] receipt, out CarrierLocation stamp)
    {
        if (Fields(element, 5, out receipt) && Fields(receipt[1], 3, out var request) &&
            Same(request[0], receipt[0]) && Fields(request[1], 3, out var inbox) && Same(inbox[0], link) &&
            Message(receipt[0], out _, out var o, out stamp) && Fields(link, 5, out var l) && Same(o[0], l[0])) return true;
        stamp = default; return false;
    }

    private static bool Inbox(IElement element, out IElement[] fields, out IElement[] link)
    {
        if (Fields(element, 3, out fields) && Link(fields[0], out link))
        {
            if (fields[1] is Placeholder) return fields[2] is Placeholder;
            if (Received(fields[1], fields[0], out _, out var latest) &&
                (fields[2] is Placeholder || Received(fields[2], fields[0], out _, out var consumed) &&
                    (consumed.Ticks < latest.Ticks || Same(fields[2], fields[1])))) return true;
        }
        link = []; return false;
    }
}
