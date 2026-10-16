using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// A declared two-ended categorical reflex. The native policy connects stock
/// state and a comparison of the endpoint eyes to a movement port. There is no
/// learning, numeric light sample, hidden classifier or propulsion model here.
/// </summary>
public sealed class QualitativeReflexFixture
{
    public IElement Agent { get; } = new Placeholder();
    public IElement LightSource { get; } = new Placeholder();
    public IElement EndA { get; } = new Placeholder();
    public IElement Interior { get; } = new Placeholder();
    public IElement EndB { get; } = new Placeholder();
    public Relation Body { get; }
    public Relation EyeA { get; }
    public Relation EyeB { get; }
    public Relation EyePair { get; }
    public Relation MoveToA { get; }
    public Relation Rest { get; }
    public Relation MoveToB { get; }
    public Relation Actions { get; }
    public Relation SensorPort { get; }
    public Relation ModePort { get; }
    public Relation MotorPort { get; }
    public OrdinalReferenceFrame LightFrame { get; } = new();
    public QualitativeStockFixture Stock { get; } = new();
    public Relation Policy { get; }
    public FiniteLawPlan Plan { get; }
    public Relation Surface { get; }

    public QualitativeReflexFixture()
    {
        Body = new Relation(new Relation(EndA, Interior), new Relation(Interior, EndB));
        EyeA = new Relation(EndA, LightSource);
        EyeB = new Relation(EndB, LightSource);
        EyePair = new Relation(EyeA, EyeB);
        MoveToA = new Relation(Interior, EndA);
        Rest = new Relation(Interior, Interior);
        MoveToB = new Relation(Interior, EndB);
        Actions = StructuralConstraint.Closed(MoveToA, Rest, MoveToB);
        SensorPort = new Relation(new Relation(LightSource, EyePair), LightFrame.Surface);
        ModePort = new Relation(Agent, Stock.Surface);
        MotorPort = new Relation(Body, Actions);
        Policy = FiniteLaw.Create(Stock.StateDomain, LightFrame.Domain, Actions,
            FiniteLaw.Row(Stock.Empty, LightFrame.Lower, StructuralConstraint.Closed(MoveToB)),
            FiniteLaw.Row(Stock.Empty, LightFrame.Equal, StructuralConstraint.Closed(Rest)),
            FiniteLaw.Row(Stock.Empty, LightFrame.Higher, StructuralConstraint.Closed(MoveToA)),
            FiniteLaw.Row(Stock.Available, LightFrame.Lower, StructuralConstraint.Closed(MoveToB)),
            FiniteLaw.Row(Stock.Available, LightFrame.Equal, StructuralConstraint.Closed(Rest)),
            FiniteLaw.Row(Stock.Available, LightFrame.Higher, StructuralConstraint.Closed(MoveToA)),
            FiniteLaw.Row(Stock.Full, LightFrame.Lower, StructuralConstraint.Closed(MoveToA)),
            FiniteLaw.Row(Stock.Full, LightFrame.Equal, StructuralConstraint.Closed(Rest)),
            FiniteLaw.Row(Stock.Full, LightFrame.Higher, StructuralConstraint.Closed(MoveToB)));
        Plan = FiniteLawPlan.Capture(Policy) ?? throw new InvalidOperationException("Unreadable authored policy.");
        Surface = new Relation(new Relation(Agent, Body),
            new Relation(new Relation(SensorPort, ModePort), new Relation(MotorPort, Policy)));
    }

    /// <summary>
    /// Supplies the eye comparator's categorical publication. Its context
    /// explicitly names the memory event it is to be combined with. Private
    /// source amplitudes are neither stored nor queried.
    /// </summary>
    public Relation See(IElement eventIdentity, IElement stateEvent, Relation lightSupport,
        IElement? evidence = null) =>
        LightFrame.Observe(LightSource, EyeA, EyeB, new Relation(stateEvent, eventIdentity),
            lightSupport, evidence);

    public Relation Read(Relation state, Relation observation, bool compiled = false)
    {
        var premises = new Relation(new Relation(state, observation),
            new Relation(SensorPort, new Relation(ModePort, MotorPort)));
        if (!PortsConnected() || !Stock.TryReadState(state, out var owner, out var stateEvent, out var modes) ||
            !FiniteLaw.SameParticipant(owner, Agent) ||
            !LightFrame.TryRead(observation, out var source, out var subject, out var reference,
                out var context, out var light, out _) ||
            !FiniteLaw.SameParticipant(source, LightSource) ||
            !FiniteLaw.SameParticipant(subject, EyeA) || !FiniteLaw.SameParticipant(reference, EyeB) ||
            context is not Relation dependency || !FiniteLaw.SameParticipant(dependency.Left, stateEvent))
            return FiniteLaw.Unresolved(Policy, StructuralConstraint.Open(), StructuralConstraint.Open(), premises);

        var read = compiled ? Plan.Evaluate(modes, light) : FiniteLaw.Evaluate(Policy, modes, light);
        return FiniteLaw.WithProvenance(read, premises);
    }

    /// <summary>
    /// Joins independently scheduled, explicitly permitted observations at one
    /// receiving decision event. The producer events remain unchanged, so held
    /// memory is not misrepresented as a fresh measurement. This is a read;
    /// command consumption and physical movement still need a separate route.
    /// </summary>
    public Relation ReadConnected(EventScheduleConnection modeConnection, Relation modeDelivery,
        EventScheduleConnection lightConnection, Relation lightDelivery, bool compiled = false)
    {
        var premises = new Relation(new Relation(modeDelivery, lightDelivery),
            new Relation(new Relation(modeConnection.Surface, lightConnection.Surface),
                new Relation(ModePort, new Relation(SensorPort, MotorPort))));
        if (!PortsConnected() || !ReadPort(modeConnection, modeDelivery, Agent, ModePort, Stock.Receiver,
                Stock.Surface, Stock.StateDomain, out var modeEvent, out var modes) ||
            !ReadPort(lightConnection, lightDelivery, LightSource, SensorPort, EyePair,
                LightFrame.Surface, LightFrame.Domain, out var lightEvent, out var light) ||
            !FiniteLaw.SameParticipant(modeEvent, lightEvent))
            return FiniteLaw.Unresolved(Policy, StructuralConstraint.Open(), StructuralConstraint.Open(), premises);
        return FiniteLaw.WithProvenance(compiled ? Plan.Evaluate(modes, light) :
            FiniteLaw.Evaluate(Policy, modes, light), premises);
    }

    private static bool ReadPort(EventScheduleConnection connection, Relation delivery,
        IElement source, IElement port, IElement subject, IElement basis, Relation domain,
        out IElement receiverEvent, out Relation support)
    {
        receiverEvent = null!; support = null!;
        if (!FiniteLaw.SameParticipant(connection.Producer, source) ||
            !ReferenceEquals(connection.Receiver, port) ||
            !connection.TryReadAccepted(delivery, out var offered, out var producerEvent, out var receiving) ||
            offered is not Relation publication ||
            !QualitativePublication.TryRead(publication, out var owner, out var participant,
                out var eventIdentity, out var frame, out var scope, out var alternatives, out _) ||
            !FiniteLaw.SameParticipant(owner, source) || !FiniteLaw.SameParticipant(participant, subject) ||
            !FiniteLaw.SameParticipant(eventIdentity, producerEvent) || !FiniteLaw.SameParticipant(frame, basis) ||
            !ReferenceEquals(scope, domain)) return false;
        receiverEvent = receiving; support = alternatives;
        return true;
    }

    private bool PortsConnected() =>
        FiniteLaw.SameParticipant(ModePort.Left, Agent) && ReferenceEquals(ModePort.Right, Stock.Surface) &&
        SensorPort.Left is Relation sensor && FiniteLaw.SameParticipant(sensor.Left, LightSource) &&
        ReferenceEquals(sensor.Right, EyePair) && ReferenceEquals(EyePair.Left, EyeA) && ReferenceEquals(EyePair.Right, EyeB) &&
        ReferenceEquals(SensorPort.Right, LightFrame.Surface) &&
        ReferenceEquals(MotorPort.Left, Body) && ReferenceEquals(MotorPort.Right, Actions) &&
        Surface.Left is Relation identity && FiniteLaw.SameParticipant(identity.Left, Agent) &&
        ReferenceEquals(identity.Right, Body) && Surface.Right is Relation ports &&
        ports.Left is Relation inputs && ReferenceEquals(inputs.Left, SensorPort) && ReferenceEquals(inputs.Right, ModePort) &&
        ports.Right is Relation output && ReferenceEquals(output.Left, MotorPort) && ReferenceEquals(output.Right, Policy);

    /// <summary>A complete one-command read can feed an explicit actuator law.</summary>
    public bool TryReadCommand(Relation read, out IElement command)
    {
        command = null!;
        if (!PortsConnected() || read.Left is not Relation { Left: Relation source, Right: Relation outputDomain } ||
            !ReferenceEquals(source.Left, Policy) || !ReferenceEquals(outputDomain, Actions) ||
            !FiniteLaw.TryReadResult(read, out var support, out var residue, out _) ||
            FiniteLaw.Entries(residue).Length != 0 ||
            !FiniteLaw.ClosedEntries(support, out var entries) || entries.Length != 1 ||
            FiniteLaw.IndexOf(FiniteLaw.Entries(Actions), entries[0]) < 0) return false;
        command = entries[0];
        return true;
    }
}
