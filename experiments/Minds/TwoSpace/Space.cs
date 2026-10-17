using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;
using static Minds.TwoSpace.Graph;

namespace Minds.TwoSpace;

/// <summary>A view of one participant's local graph, with no reference to its peer.</summary>
public sealed class Space
{
    private readonly IElement[] definition;
    private readonly IElement[] requests;
    private readonly IElement[] slots;
    public Relation State { get; private set; }
    public Relation History { get; private set; }
    public Relation Inbox { get; private set; }
    public Relation? LastPlan { get; private set; }
    public Relation? LastApplication { get; private set; }
    public Relation? LastPublishedMessage { get; private set; }
    public Relation Reference { get; private set; }
    public Carrier Actuator => (Carrier)F(definition[0])[2];
    public Carrier Output => ((CarrierLocation)F(definition[2])[2]).Carrier;
    private Relation Report => R(definition[1]);
    private Carrier Clock => ((CarrierLocation)F(Report)[1]).Carrier;
    public int Attempts => F(History).Length;

    public Space(IElement definition, IElement inbox, IElement requests, IElement slots)
    {
        this.definition = F(definition); this.requests = F(requests); this.slots = F(slots);
        Reference = R(this.definition[2]);
        Actuator.Add(checked((ulong)((CarrierLocation)R(this.definition[10]).Left).Ticks));
        Require(LocalCalibration.TryCapture(R(this.definition[0]), out var state), "initial capture");
        State = state; History = R(this.definition[12]); Inbox = R(inbox);
    }

    private Relation Bind(int index, params (int Slot, IElement Value)[] bindings) =>
        R(StructuralPattern.Refine(requests[index],
            bindings.Select(binding => new Relation(slots[binding.Slot], binding.Value)).ToArray()));

    public Relation Predict()
    {
        Require(LocalCalibration.TryPredict(Bind(5, (20, Report), (21, F(State)[2])), out var result), "prediction");
        return result;
    }

    public Relation Publish(long sequence)
    {
        IElement antecedents = G();
        if (F(Inbox)[2] is Relation consumed)
        {
            var observation = F(F(F(consumed)[0])[0]);
            antecedents = G(new Relation(observation[0], observation[1]));
        }
        Require(LocalCalibration.TryPublish(Bind(0, (0, State), (1, Predict()), (2, definition[3]),
            (3, new CarrierLocation(Clock, sequence)), (4, antecedents), (5, new Placeholder())),
            out var publication), "publication");
        LastPublishedMessage = R(F(publication)[2]);
        return LastPublishedMessage;
    }

    public Relation Receive(Relation message, long arrival)
    {
        Require(TryReceive(message, arrival, out var receipt), "receipt");
        return receipt;
    }

    public bool TryReceive(Relation message, long arrival, out Relation receipt)
    {
        var accepted = LocalCalibration.TryReceive(Bind(1, (6, message), (7, Inbox),
            (8, new CarrierLocation(Clock, arrival))), out receipt, out var inbox);
        if (accepted) Inbox = inbox;
        return accepted;
    }

    // A new external observation, not a direct write to the calibration actuator.
    // Previous reference observations remain connected to earlier comparisons.
    public Relation ObserveReference(long nativeValue, long eventTime)
    {
        var prior = F(Reference);
        Reference = G(prior[0], new CarrierLocation(Clock, eventTime),
            new CarrierLocation(Output, nativeValue), prior[3], prior[4]);
        return Reference;
    }

    public Relation ReferenceComparison() => Compare(Reference);

    private Relation Compare(IElement observation)
    {
        Require(LocalCalibration.TryCompare(Bind(6, (1, Predict()), (22, observation),
            (23, definition[9]), (20, Report)), out var comparison), "comparison");
        return comparison;
    }

    public Relation Plan(Relation receipt, long denominator) =>
        PlanComparison(Compare(F(receipt)[2]), denominator);

    private Relation PlanComparison(Relation comparison, long denominator)
    {
        var response = new Relation(Relation.Segment(Output, 0, denominator), Relation.Segment(Output, 0, 1));
        // False remains an unresolved plan that the application reader can refuse.
        LocalCalibration.TryPlanResponse(Bind(2, (0, State), (9, comparison),
            (10, response), (11, definition[8]), (12, definition[5])), out var plan);
        LastPlan = plan;
        return plan;
    }

    public bool Apply(Relation plan, Relation receipt)
    {
        var accepted = LocalCalibration.TryApplyReceived(Bind(3, (13, plan), (0, State),
            (14, History), (15, definition[11]), (16, receipt), (7, Inbox)),
            out var application, out var history, out var inbox);
        LastApplication = application;
        if (accepted) { State = R(F(F(application)[2])[2]); History = history; Inbox = inbox; }
        return accepted;
    }

    public bool RespondToReference()
    {
        var plan = PlanComparison(ReferenceComparison(), 1);
        var accepted = LocalCalibration.TryApplyResponse(Bind(4, (13, plan), (0, State),
            (14, History), (15, definition[11])), out var application, out var history);
        LastApplication = application;
        if (accepted) { State = R(F(F(application)[2])[2]); History = history; }
        return accepted;
    }
}
