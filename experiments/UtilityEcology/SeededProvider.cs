using CompositeEmergence;
using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using PrimitiveLearning;

namespace UtilityEcology;

/// <summary>Opt-in finite candidate experiments. The incumbent remains the
/// only serving model until a paid common-experience comparison admits a new
/// body. Initial seed resource and demand-funded later purses are separate.</summary>
public sealed class SeededProvider
{
    public InterpretationProvider Base { get; }
    public Carrier SeedEnergy { get; }
    public AdaptiveOrdinal? Current { get; private set; }
    public AdaptiveOrdinal? Candidate => activeTrial is { Closed: false } ? activeTrial.Candidate : null;
    private readonly List<SeedTrial> history;
    private SeedTrial? activeTrial;
    private readonly Carrier attempts, accepted, rejected, lastLaunch, comparisonEnabled, countedClosure;
    private readonly Relation law;
    private Relation lastDecision, seedFunding;
    public long Trials => checked((long)attempts.Value);
    public long Accepted => checked((long)accepted.Value);
    public long Rejected => checked((long)rejected.Value);
    public long SeedSpent => history.Where(t => t.SeedFunded).Sum(t => t.WorkSpent);
    public long EarnedSpent => history.Where(t => !t.SeedFunded).Sum(t => t.WorkSpent);
    public long TrialWork => SeedSpent + EarnedSpent;
    public int TrainingCount => activeTrial?.TrainingCount ?? 0;
    public int ValidationCount => activeTrial?.ValidationCount ?? 0;
    public bool FreezeIncumbent => activeTrial is { Validating: true };
    public string Status => activeTrial is null ? "waiting" : activeTrial.Closed ? activeTrial.Accepted ? "accepted" : "rejected" : activeTrial.Validating ? "validation" : "training";
    public long LastTrialId => activeTrial?.Id ?? 0;
    public long LastGain => activeTrial?.Gain ?? 0;
    public int LastCoverage => activeTrial?.SupportedCount ?? 0;
    public long LastTrialBurn => activeTrial?.WorkSpent ?? 0;
    public string LastTrialReason => activeTrial?.Reason ?? "waiting";
    public long? CandidateValidationAdvice => activeTrial?.CandidateValidationAdvice;
    public long? IncumbentValidationAdvice => activeTrial?.IncumbentValidationAdvice;
    public Relation LastDecision => activeTrial?.LastDecision ?? lastDecision;
    public IReadOnlyList<SeedTrial> TrialHistory => history;
    public Relation Root => NativeGraph.G(Base.Root, Current?.Root ?? (IElement)new Placeholder(), SeedEnergy,
        NativeGraph.G(history.Select(t => (IElement)t.Root).ToArray()), NativeGraph.G(attempts, accepted, rejected, lastLaunch, comparisonEnabled, countedClosure), law, lastDecision, seedFunding);
    private IElement[] Limits => NativeGraph.Parts(law, 5);
    private long SeedBudget => Point(Limits[0]).Ticks;
    private int TrainEvents => checked((int)Point(Limits[1]).Ticks);
    private int ValidateEvents => checked((int)Point(Limits[2]).Ticks);
    private long RetryPeriod => Point(Limits[3]).Ticks;
    private long MaxAttempts => Point(Limits[4]).Ticks;

    public SeededProvider(Carrier source, Carrier presenceAxis, Carrier orderAxis, Carrier actionAxis, Carrier resourceAxis,
        bool comparisonEnabled = true, long seedBudget = 320, int trainingEvents = 16, int validationEvents = 32,
        long retryPeriod = 64, int maxAttempts = 8)
    {
        if (seedBudget < 16 || trainingEvents is < 1 or > 32 || validationEvents is < 1 or > 32 || retryPeriod < 1 || maxAttempts is < 1 or > 8)
            throw new ArgumentOutOfRangeException(nameof(seedBudget));
        Base = new InterpretationProvider(source, presenceAxis, orderAxis, actionAxis, resourceAxis, false, false, monitorProgress: true);
        SeedEnergy = new(resourceAxis.UnitShape); history = []; attempts = new(); accepted = new(); rejected = new(); lastLaunch = new();
        this.comparisonEnabled = new(); if (comparisonEnabled) this.comparisonEnabled.Add(1);
        countedClosure = new();
        law = NativeGraph.G(Relation.Point(resourceAxis, seedBudget), Relation.Point(new Carrier(), trainingEvents),
            Relation.Point(new Carrier(), validationEvents), Relation.Point(lastLaunch, retryPeriod), Relation.Point(attempts, maxAttempts));
        lastDecision = NativeGraph.Open(Base.Source, law);
        seedFunding = NativeGraph.Open(SeedEnergy, law);
    }
    private SeededProvider(Relation root)
    {
        var f = NativeGraph.Parts(root, 8); Base = InterpretationProvider.FromRoot((Relation)f[0]);
        Current = f[1] is Relation current ? AdaptiveOrdinal.FromRoot(current) : null; SeedEnergy = (Carrier)f[2];
        history = NativeGraph.Parts((Relation)f[3]).Cast<Relation>().Select(SeedTrial.FromRoot).ToList(); activeTrial = history.LastOrDefault();
        var c = NativeGraph.Parts((Relation)f[4], 6).Cast<Carrier>().ToArray(); attempts = c[0]; accepted = c[1]; rejected = c[2]; lastLaunch = c[3]; comparisonEnabled = c[4]; countedClosure = c[5];
        law = (Relation)f[5]; lastDecision = (Relation)f[6];
        seedFunding = (Relation)f[7];
    }
    public static SeededProvider FromRoot(Relation root) => new(root);

    /// <summary>Retains the already settled seed transfer without introducing
    /// resource. The target must be this provider's actual seed carrier.</summary>
    public Relation RecordSeedFunding(Relation receipt)
    {
        if (!ReadWitness.TryRead(receipt, out _, out _, out var target, out _, out _) || target is not Carrier carrier || !carrier.SameIdentityAs(SeedEnergy))
            return NativeGraph.Open(receipt, SeedEnergy);
        return seedFunding = receipt;
    }

    public Relation Interpret(Relation sample, Func<Relation, Relation, Relation> buyerPaidCompare, long time)
    {
        var presence = Base.Interpret(sample, buyerPaidCompare, time);
        return Current is null ? presence : Calibrate(Current.Predict(sample, buyerPaidCompare));
    }

    public Relation Observe(Relation sample, Relation offered, Relation command, Relation bound, bool permission,
        Func<Carrier, Relation, Relation, Relation> comparePaid, ServiceEconomy economy, EconomyService service)
    {
        var freeze = FreezeIncumbent;
        var baseFeedback = Base.Observe(sample, offered, command, bound, permission && !freeze,
            (a, b) => comparePaid(economy.Buyer, a, b));
        var currentFeedback = NativeGraph.Open(offered, Base.Source);
        if (Current is not null && permission && !freeze && TryDirection(command, bound, out var direction))
        {
            NativeGraph.TryValue(direction, Base.AdviceAxis, out var actualDirection);
            var label = Equation.PredictWitness(new Carrier(), Equation.Affine(Base.AdviceAxis, -1, 1, Base.LabelAxis, 0, 1), actualDirection, Base.LabelAxis);
            currentFeedback = Current.Observe(sample, label, (a, b) => comparePaid(economy.Buyer, a, b), true);
        }
        if (activeTrial is { Closed: false } trial)
        {
            var feedback = trial.Observe(sample, command, bound, comparePaid, economy);
            AccountClosure();
            lastDecision = ReadWitness.Create(new Carrier(), sample, law, trial.LastDecision,
                NativeGraph.G(baseFeedback, currentFeedback, feedback), NativeGraph.G(trial.Root, WalletRead(service)));
        }
        else lastDecision = ReadWitness.Create(new Carrier(), sample, law, baseFeedback, currentFeedback, WalletRead(service));
        return lastDecision;
    }

    public Relation PrepareValidation(Relation sample, Relation actualCommand,
        Func<Carrier, Relation, Relation, Relation> comparePaid, ServiceEconomy economy, EconomyService service)
    {
        if (activeTrial is not { Validating: true } trial) return NativeGraph.Open(sample, law);
        var prepared = trial.PrepareValidation(sample, actualCommand, ReadIncumbent, comparePaid, economy);
        AccountClosure();
        return lastDecision = ReadWitness.Create(new Carrier(), sample, law, prepared, NativeGraph.G(), NativeGraph.G(WalletRead(service), trial.Root));
    }

    public bool TryLaunch(ServiceEconomy economy, EconomyService service, out Relation witness)
    {
        var initial = Trials == 0;
        var fromSeed = initial && SeedEnergy.Value >= checked((ulong)SeedBudget) && SeedEnergy.Tension == 0;
        var requestAxis = (Carrier)NativeGraph.Parts((Relation)NativeGraph.Parts(Base.Root, 9)[3], 7)[0];
        var pressureAxis = (Carrier)NativeGraph.Parts((Relation)NativeGraph.Parts(Base.Root, 9)[3], 7)[1];
        var claims = NativeGraph.G(
            OperationClaim.AtLeast(new Carrier(), requestAxis.ValueSlot, Relation.Point(requestAxis, initial ? 16 : checked((long)lastLaunch.Value + RetryPeriod)), new Carrier()),
            OperationClaim.AtLeast(new Carrier(), pressureAxis.ValueSlot, Relation.Point(pressureAxis, 4), new Carrier()),
            OperationClaim.AtMost(new Carrier(), attempts.ValueSlot, Relation.Point(attempts, MaxAttempts - 1), new Carrier()));
        if (activeTrial is { Closed: false } || !NativeGraph.Parts(claims).Cast<Relation>().All(NativeGraph.Pass))
        { witness = NativeGraph.Open(claims, law); return false; }
        var purse = fromSeed ? SeedEnergy : new Carrier(Base.ResourceAxis.UnitShape);
        var funding = seedFunding;
        if (!fromSeed && !economy.PayWork(service, purse, SeedBudget, out funding))
        { witness = NativeGraph.Open(service.Root, law); return false; }
        if (purse.Value < checked((ulong)SeedBudget) || purse.Tension != 0 || !economy.BurnWork(purse, 16, out var construction))
        { witness = NativeGraph.Open(purse, law); return false; }
        var candidate = new AdaptiveOrdinal(Base.Source, Base.PresenceAxis, Base.OrderAxis, Base.LabelAxis, 4);
        attempts.Add(1); lastLaunch.Set(checked((ulong)Base.Requests));
        activeTrial = new SeedTrial(candidate, Base.Source, purse, Base.ResourceAxis, Base.ActionAxis, Base.AdviceAxis, Base.LabelAxis,
            Trials, fromSeed, SeedBudget, TrainEvents, ValidateEvents, comparisonEnabled.Value == 1,
            Current?.Root ?? Base.PresenceModel.Root, funding, construction); history.Add(activeTrial);
        witness = lastDecision = ReadWitness.Create(new Carrier(), Current?.Root ?? Base.PresenceModel.Root, claims, candidate.Root,
            NativeGraph.G(), NativeGraph.G(construction, activeTrial.Root, law)); return true;
    }

    private void AccountClosure()
    {
        if (activeTrial is not { Closed: true } trial || countedClosure.Value >= checked((ulong)trial.Id)) return;
        if (trial.Accepted) { Current = trial.Candidate; accepted.Add(1); } else rejected.Add(1);
        countedClosure.Set(checked((ulong)trial.Id));
    }

    private Relation ReadIncumbent(Relation sample, Func<Relation, Relation, Relation> compare)
    {
        if (Current is not null) return Calibrate(Current.Predict(sample, compare));
        if (!SamplingSurface.TryReadSample(sample, out var presence, out _)) return NativeGraph.Open(sample, Base.Source);
        return Base.PresenceModel.Predict(NativeGraph.Publication(Base.PresenceAxis, presence.Ticks, NativeGraph.Identity(sample)));
    }
    private Relation Calibrate(Relation prediction) => AdaptiveOrdinal.TryReadPrediction(prediction, out var label)
        ? Equation.PredictWitness(new Carrier(), Equation.Affine(Base.LabelAxis, 0, 1, Base.AdviceAxis, -1, 1), label, Base.AdviceAxis)
        : NativeGraph.Open(prediction, Base.Source);
    private bool TryDirection(Relation command, Relation bound, out Relation direction)
    {
        direction = NativeGraph.Open(bound, command);
        if (!NativeGraph.IsBoundTo(bound, command, Base.ResourceAxis) || !NativeGraph.TryValue(command, Base.ActionAxis, out var action) || action.Ticks is not (-1 or 1)) return false;
        var sign = OutcomeRetrodiction.ResourceSign(bound, Base.Gate.ResourceAxis);
        if (!NativeGraph.TryValue(sign, Base.Gate.ResourceAxis, out var consequence) || consequence.Ticks is not (-1 or 1)) return false;
        direction = Equation.PredictWitness(new Carrier(), Equation.Product(Base.AdviceAxis, Equation.UnitPort(Base.ActionAxis), Equation.UnitPort(Base.Gate.ResourceAxis)),
            new Relation(action, consequence), Base.AdviceAxis); return NativeGraph.TryValue(direction, Base.AdviceAxis, out _);
    }
    private static CarrierLocation Point(IElement value) => NativeGraph.TryPoint(value, out var point) ? point : throw new ArgumentException("The seeded provider's native law is not readable.");
    private static Relation WalletRead(EconomyService service) => NativeGraph.G(new CarrierLocation(service.Wallet, checked((long)service.Wallet.Value)), service.Wallet.TensionSlot);
}
