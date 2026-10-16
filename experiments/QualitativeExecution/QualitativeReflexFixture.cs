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
        MoveToA = new Relation(Interior, EndA);
        Rest = new Relation(Interior, Interior);
        MoveToB = new Relation(Interior, EndB);
        Actions = StructuralConstraint.Closed(MoveToA, Rest, MoveToB);
        SensorPort = new Relation(new Relation(LightSource, new Relation(EyeA, EyeB)), LightFrame.Surface);
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
        if (!Stock.TryReadState(state, out var owner, out var stateEvent, out var modes) ||
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

    /// <summary>A complete one-command read can feed an explicit actuator law.</summary>
    public bool TryReadCommand(Relation read, out IElement command)
    {
        command = null!;
        if (read.Left is not Relation { Left: Relation source, Right: Relation outputDomain } ||
            !ReferenceEquals(source.Left, Policy) || !ReferenceEquals(outputDomain, Actions) ||
            !FiniteLaw.TryReadResult(read, out var support, out var residue, out _) ||
            FiniteLaw.Entries(residue).Length != 0 ||
            !FiniteLaw.ClosedEntries(support, out var entries) || entries.Length != 1 ||
            FiniteLaw.IndexOf(FiniteLaw.Entries(Actions), entries[0]) < 0) return false;
        command = entries[0];
        return true;
    }
}
