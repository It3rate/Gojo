using CompositeEmergence;
using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using PrimitiveLearning;

namespace UtilityEcology;

/// <summary>Native interpretation begins as an empty presence-conditioned
/// consequence predictor. A funded local proposal can add an ordinal model.
/// The source's private measurement is never a model coordinate.</summary>
public sealed class InterpretationProvider
{
    public Carrier Source { get; }
    public Carrier PresenceAxis { get; }
    public Carrier OrderAxis { get; }
    public Carrier AdviceAxis { get; }
    public Carrier LabelAxis { get; }
    public Carrier ActionAxis { get; }
    public Carrier ResourceAxis { get; }
    public LearningGate Gate { get; }
    public OrdinalPredictor PresenceModel { get; private set; }
    public AdaptiveOrdinal? OrdinalModel { get; private set; }
    private readonly Carrier requests, pressure, updates, proposals, lastProposalAt, lastSeen, reliefPhase;
    private Relation lastPresencePrediction, lastInterpretation, lastFeedback, lastProposal;
    public long Requests => checked((long)requests.Value);
    public long UnmetPressure => checked((long)pressure.Value);
    public long Updates => checked((long)updates.Value);
    public long Proposals => checked((long)proposals.Value);
    public long LastSeen => checked((long)lastSeen.Value);
    public int Landmarks => OrdinalModel?.LandmarkCount ?? 0;
    public Relation Root => NativeGraph.G(NativeGraph.G(Source, PresenceAxis, OrderAxis, AdviceAxis, LabelAxis, ActionAxis, ResourceAxis),
        NativeGraph.G(PresenceModel.Root, OrdinalModel?.Root ?? (IElement)new Placeholder()), Gate.Root,
        NativeGraph.G(requests, pressure, updates, proposals, lastProposalAt, lastSeen, reliefPhase),
        lastPresencePrediction, lastInterpretation, lastFeedback, lastProposal,
        NativeGraph.G(Relation.Point(requests, 16), Relation.Point(pressure, 4), Relation.Point(proposals, 8),
            Relation.Point(ResourceAxis, 16), Relation.Point(reliefPhase, 4)));

    public InterpretationProvider(Carrier source, Carrier presenceAxis, Carrier orderAxis,
        Carrier actionAxis, Carrier resourceAxis, bool opportunity, bool always, bool monitorProgress = false,
        bool progressGate = false, bool useProgress = true, bool changeInterest = false, int retestPeriod = 16)
    {
        Source = source; PresenceAxis = presenceAxis; OrderAxis = orderAxis; ActionAxis = actionAxis; ResourceAxis = resourceAxis;
        AdviceAxis = new(); LabelAxis = new();
        PresenceModel = new OrdinalPredictor(AdviceAxis, PresenceAxis);
        Gate = LearningGate.Create(new Carrier(resourceAxis.UnitShape), 0, opportunity: opportunity, always: always,
            progressSource: monitorProgress ? source : null, progressGate: progressGate, useProgress: useProgress,
            changeInterest: changeInterest, retestPeriod: retestPeriod);
        requests = new(); pressure = new(); updates = new(); proposals = new(); lastProposalAt = new(); lastSeen = new();
        reliefPhase = new(); reliefPhase.Attach(new Relation(Relation.Point(reliefPhase, 4), Relation.Point(reliefPhase, 0)));
        pressure.Attach(new Relation(Relation.Point(pressure, 64), Relation.Point(pressure, 64)));
        lastPresencePrediction = lastInterpretation = lastFeedback = lastProposal = NativeGraph.Open(Source, new Placeholder());
    }

    private InterpretationProvider(Relation root)
    {
        var fields = NativeGraph.Parts(root, 9); var axes = NativeGraph.Parts((Relation)fields[0], 7).Cast<Carrier>().ToArray();
        Source = axes[0]; PresenceAxis = axes[1]; OrderAxis = axes[2]; AdviceAxis = axes[3]; LabelAxis = axes[4];
        ActionAxis = axes[5]; ResourceAxis = axes[6];
        var models = NativeGraph.Parts((Relation)fields[1], 2); PresenceModel = OrdinalPredictor.FromRoot((Relation)models[0]);
        OrdinalModel = models[1] is Relation ordinal ? AdaptiveOrdinal.FromRoot(ordinal) : null;
        Gate = LearningGate.FromRoot((Relation)fields[2]);
        var counters = NativeGraph.Parts((Relation)fields[3], 7).Cast<Carrier>().ToArray();
        requests = counters[0]; pressure = counters[1]; updates = counters[2]; proposals = counters[3]; lastProposalAt = counters[4]; lastSeen = counters[5];
        reliefPhase = counters[6];
        lastPresencePrediction = (Relation)fields[4]; lastInterpretation = (Relation)fields[5]; lastFeedback = (Relation)fields[6]; lastProposal = (Relation)fields[7];
    }
    public static InterpretationProvider FromRoot(Relation root) => new(root);

    public Relation Interpret(Relation sample, Func<Relation, Relation, Relation> compare, long time)
    {
        requests.Add(1); lastSeen.Set(checked((ulong)time));
        if (!SamplingSurface.TryReadSample(sample, out var presence, out _) || !presence.Carrier.SameIdentityAs(PresenceAxis))
            return lastInterpretation = NativeGraph.Open(sample, Source);
        var presencePublication = NativeGraph.Publication(PresenceAxis, presence.Ticks, NativeGraph.Identity(sample));
        lastPresencePrediction = PresenceModel.Predict(presencePublication);
        var read = OrdinalModel is null ? lastPresencePrediction : OrdinalModel.Predict(sample, compare);
        Relation interpreted = read;
        if (OrdinalModel is not null && AdaptiveOrdinal.TryReadPrediction(read, out var label))
        {
            var calibration = Equation.Affine(LabelAxis, 0, 1, AdviceAxis, -1, 1);
            interpreted = Equation.Predict(calibration, new CarrierLocation(LabelAxis, label.Ticks), AdviceAxis);
        }
        var available = NativeGraph.TryValue(interpreted, AdviceAxis, out var result) && result.Ticks is -1 or 1;
        var boundary = new Carrier();
        var publication = available ? WorldPublication.Value(AdviceAxis, result.Ticks, boundary) : WorldPublication.Open(AdviceAxis, boundary);
        lastInterpretation = ReadWitness.Create(new Carrier(), sample, NativeGraph.G(read, Source),
            available ? new CarrierLocation(AdviceAxis, result.Ticks) : new Placeholder(), read, publication);
        return publication;
    }

    /// <summary>Binary opposed actions are an authored physical premise. From
    /// actual issued action and sign of its gross consequence, a native product
    /// infers which direction succeeded. No desired-action label is received.</summary>
    public Relation Observe(Relation sample, Relation offeredAdvice, Relation actualCommand, Relation gross,
        bool learn, Func<Relation, Relation, Relation> compare)
    {
        if (!NativeGraph.IsBoundTo(gross, actualCommand, ResourceAxis))
            return lastFeedback = NativeGraph.Open(gross, actualCommand);
        var sign = OutcomeRetrodiction.ResourceSign(gross, Gate.ResourceAxis);
        var product = Equation.Product(AdviceAxis, Equation.UnitPort(ActionAxis), Equation.UnitPort(Gate.ResourceAxis));
        Relation correctDirection = NativeGraph.Open(NativeGraph.G(actualCommand, sign), product);
        if (NativeGraph.TryValue(actualCommand, ActionAxis, out var action) && action.Ticks is -1 or 1 &&
            NativeGraph.TryValue(sign, Gate.ResourceAxis, out var consequence) && consequence.Ticks is -1 or 1)
            correctDirection = PublishCalculation(Equation.Predict(product, new Relation(action, consequence), AdviceAxis), AdviceAxis);
        Relation expected = NativeGraph.Open(offeredAdvice, sign);
        if (NativeGraph.TryValue(offeredAdvice, AdviceAxis, out var advice) && advice.Ticks is -1 or 1 &&
            NativeGraph.TryValue(actualCommand, ActionAxis, out action) && action.Ticks is -1 or 1)
        {
            var expectation = Equation.Product(Gate.ResourceAxis, Equation.UnitPort(AdviceAxis), Equation.UnitPort(ActionAxis));
            expected = Equation.Predict(expectation, new Relation(advice, action), Gate.ResourceAxis);
        }
        var feedback = Gate.ObservePrediction(expected, sign);
        var supported = NativeGraph.TryValue(correctDirection, AdviceAxis, out var actual);
        var agreement = supported && NativeGraph.TryValue(offeredAdvice, AdviceAxis, out var predicted)
            ? OperationClaim.Equality(new Carrier(), predicted, actual, new Carrier()) : NativeGraph.Open(offeredAdvice, correctDirection);
        var matched = NativeGraph.Pass(agreement);
        var beforePressure = pressure.Value;
        if (matched) { reliefPhase.Add(1); if (reliefPhase.Value == 0 && pressure.Value > 0) pressure.Remove(1); }
        else pressure.Add(1);
        var changes = new List<IElement> { feedback, agreement,
            NativeGraph.G(Relation.Point(reliefPhase, 4), new CarrierLocation(reliefPhase, checked((long)reliefPhase.Value))),
            new Relation(Relation.Point(pressure, checked((long)beforePressure)), new CarrierLocation(pressure, checked((long)pressure.Value))) };
        if (learn && supported)
        {
            PresenceModel.Observe(lastPresencePrediction, correctDirection, true);
            if (OrdinalModel is not null)
            {
                var labelLaw = Equation.Affine(AdviceAxis, -1, 1, LabelAxis, 0, 1);
                var label = PublishCalculation(Equation.Predict(labelLaw, actual, LabelAxis), LabelAxis);
                changes.Add(OrdinalModel.Observe(sample, label, compare, true));
            }
            updates.Add(1);
        }
        return lastFeedback = ReadWitness.Create(new Carrier(), sample, NativeGraph.G(product, actualCommand, gross),
            supported ? actual : new Placeholder(), NativeGraph.G(changes.ToArray()), NativeGraph.G(correctDirection, expected, sign));
    }

    public bool TryRefine(ServiceEconomy economy, EconomyService service, bool allowRenewal, bool learning, out Relation witness)
    {
        var claims = NativeGraph.G(
            OperationClaim.AtLeast(new Carrier(), requests.ValueSlot, Relation.Point(requests, checked((long)lastProposalAt.Value + 16)), new Carrier()),
            OperationClaim.AtLeast(new Carrier(), pressure.ValueSlot, Relation.Point(pressure, 4), new Carrier()),
            OperationClaim.AtMost(new Carrier(), proposals.ValueSlot, Relation.Point(proposals, 7), new Carrier()));
        if (!learning || (OrdinalModel is not null && !allowRenewal) ||
            !NativeGraph.Parts(claims).Cast<Relation>().All(NativeGraph.Pass) || !economy.BurnWork(economy.Buyer, 16, out var payment))
        { witness = NativeGraph.Open(NativeGraph.G(claims, service.Root), Relation.Point(ResourceAxis, 16)); return false; }
        var prior = OrdinalModel?.Root ?? PresenceModel.Root;
        OrdinalModel = new AdaptiveOrdinal(Source, PresenceAxis, OrderAxis, LabelAxis, 4);
        var pressureBefore = new CarrierLocation(pressure, checked((long)pressure.Value));
        var pressureLoss = pressure.TensionSlot; pressure.Remove(checked(pressure.Value + (ulong)pressure.Tension));
        proposals.Add(1); lastProposalAt.Set(requests.Value);
        witness = lastProposal = ReadWitness.Create(new Carrier(), prior, NativeGraph.G(claims, payment), OrdinalModel.Root,
            NativeGraph.G(pressureBefore, pressureLoss), NativeGraph.G(prior, OrdinalModel.Root,
                Relation.Point(proposals, checked((long)proposals.Value)), Relation.Point(ResourceAxis, 16)));
        return true;
    }

    public bool ResetAfterIdle(ServiceEconomy economy, EconomyService service, long time, long period, out Relation witness)
    {
        var claim = OperationClaim.AtLeast(new Carrier(), new CarrierLocation(lastSeen, time),
            new CarrierLocation(lastSeen, checked((long)lastSeen.Value + period)), new Carrier());
        if (!NativeGraph.Pass(claim) || !economy.BurnWork(economy.Buyer, 16, out var payment))
        { witness = NativeGraph.Open(claim, service.Root); return false; }
        var previous = NativeGraph.G(PresenceModel.Root, OrdinalModel?.Root ?? (IElement)new Placeholder());
        PresenceModel = new OrdinalPredictor(AdviceAxis, PresenceAxis); OrdinalModel = null; lastSeen.Set(checked((ulong)time));
        pressure.Remove(checked(pressure.Value + (ulong)pressure.Tension)); lastProposalAt.Set(requests.Value);
        lastProposal = witness = ReadWitness.Create(new Carrier(), previous, NativeGraph.G(claim, payment), PresenceModel.Root,
            previous, NativeGraph.G(Relation.Point(lastSeen, time), payment));
        return true;
    }

    private static Relation PublishCalculation(Relation calculation, Carrier axis) => ReadWitness.Create(new Carrier(), calculation,
        Relation.Segment(axis, -1, 1), NativeGraph.TryValue(calculation, axis, out var value) ? value : new Placeholder(),
        calculation, calculation);
}
