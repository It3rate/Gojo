using CompositeEmergence;
using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using PrimitiveLearning;
using System.Text.Json.Serialization;

namespace UtilityEcology;

public sealed record EcologyArm(string Name, bool Learning = true, bool Always = false,
    bool Opportunity = true, bool SuccessPaid = true, bool ResetIdle = false,
    bool Communicate = true, bool Renew = true, int TrialAllowance = 16, int? BuyerProbePeriod = null,
    bool MonitorProgress = false, bool ProgressGate = false, bool UseProgress = true, bool ChangeInterest = false, int RetestPeriod = 16);
public sealed record EcologySettings(int PhaseLength = 512, long InitialCapital = 16384,
    long Fee = 2, long InitialSponsorship = 64, int Encoding = 0);
public sealed record EcologyStep(string Arm, int Seed, int Tick, int Phase, int Source,
    bool Bought, bool Interpreted, long? Advice, int Action, bool Correct, long Gross,
    long Burned, bool LearningGranted, bool Refined, bool Reset, int Landmarks,
    long Pressure, long BuyerReserve, long SupplierReserve, long Attention,
    long AccountedMoney, long ExternalNet, ProgressDecision? Progress = null);
public sealed record ProgressDecision(long Version, long Observations, long? ErrorProgress256, long? CoverageChange256,
    long? RecentError256, bool ErrorRequested, bool GateRequested, bool Funded, long EpisodeRemaining,
    long PositiveEpisodes, long ChangeEpisodes, long CoverageEpisodes, long RetestEpisodes, long SuppressedRequests, string Trigger);
public sealed record EcologyRow(string Arm, int Seed, int Phase, int Source, int Steps,
    int Correct, int Answered, int Interpreted, int Purchases, int LearningUpdates,
    int Refinements, int Resets, long Gross, long Burned, double NetPerStep,
    int Comparisons, long FinalLandmarks);
public sealed record EcologyRun(string Arm, int Seed, EcologySettings Settings, string TapeDigest,
    long Gross, long Burned, double NetPerStep, long AccountedMoney, long ExternalNet,
    IReadOnlyList<EcologyRow> Rows, IReadOnlyList<EcologyStep> Trace,
    IReadOnlyList<GraphCounts> ProducerGraphs, GraphCounts WholeGraph)
{
    [JsonIgnore] public Relation NativeRoot { get; init; } = null!;
}

/// <summary>Host scheduling and projections only. Local native gates, model
/// evidence, claims and conserved payment routes determine the decisions.</summary>
public static class EcologyRunner
{
    public static readonly EcologyArm[] Arms = [
        new("no-interpretation-learning", Learning: false, Opportunity: false, SuccessPaid: false, Renew: false),
        new("always/upfront", Always: true, Opportunity: false, SuccessPaid: false, Renew: false),
        new("error/upfront", Opportunity: false, SuccessPaid: false, Renew: false),
        new("matched/error/upfront", Opportunity: false, SuccessPaid: false, BuyerProbePeriod: 8),
        new("opportunity/upfront", SuccessPaid: false), new("opportunity/success-paid"),
        new("opportunity/success-paid/use-reset", ResetIdle: true),
        new("opportunity/success-paid/withdrawn", Communicate: false),
        new("opportunity/success-paid/no-renewal", Renew: false),
        new("opportunity/success-paid/early-cutoff", TrialAllowance: 8)
    ];
    public static readonly EcologyArm[] ProgressArms = [
        new("progress/error-only/upfront", Opportunity: false, SuccessPaid: false, BuyerProbePeriod: 8, MonitorProgress: true),
        new("progress/signed/upfront", Opportunity: false, SuccessPaid: false, BuyerProbePeriod: 8, MonitorProgress: true, ProgressGate: true),
        new("progress/cycle-only/upfront", Opportunity: false, SuccessPaid: false, BuyerProbePeriod: 8, MonitorProgress: true, ProgressGate: true, UseProgress: false),
        new("progress/change/upfront", Opportunity: false, SuccessPaid: false, BuyerProbePeriod: 8, MonitorProgress: true, ProgressGate: true, ChangeInterest: true)
    ];

    public static EcologyRun Run(int seed, EcologyArm arm, EcologySettings? settings = null)
    {
        settings ??= new();
        if (settings.PhaseLength < 16 || settings.InitialCapital < 1024 || settings.Fee < 0 || settings.InitialSponsorship < 32)
            throw new ArgumentOutOfRangeException(nameof(settings));
        var tape = WorldTape.Create(seed, settings.PhaseLength); var world = new EcologyWorld(settings.Encoding);
        var economy = new ServiceEconomy(world.ResourceAxis, world.TimeAxis, settings.InitialCapital, 4);
        var providers = new InterpretationProvider[4]; var services = new EconomyService[4];
        var acquisitionPhases = new Carrier[4]; var selectors = new Relation[4];
        var buyAxes = new Carrier[4]; var countRoles = new Carrier[4]; var quoteRoles = new Carrier[4]; var phaseRoles = new Carrier[4];
        var fallbackPhases = new Carrier[4]; var fallbackPolicies = new Relation[4]; var fallbackRoles = new Carrier[4];
        var timeAxis = new Carrier(); var lastAdmission = NativeGraph.Open(new Placeholder(), new Placeholder());
        for (var index = 0; index < providers.Length; index++)
        {
            var provider = providers[index] = new InterpretationProvider(world.Sources[index], world.PresenceAxes[index], world.OrderAxes[index],
                world.ActionAxis, world.ResourceAxis, arm.Opportunity, arm.Always, arm.MonitorProgress,
                arm.ProgressGate, arm.UseProgress, arm.ChangeInterest, arm.RetestPeriod);
            var law = arm.SuccessPaid ? ServiceEconomy.SuccessPaidLaw(world.ResourceAxis) : ServiceEconomy.UpfrontLaw(world.ResourceAxis);
            if (!economy.AddService(provider.Root, settings.Fee, 8, law, out var service, out _, activeRent: 1, parkedRent: 0) || service is null)
                throw new InvalidOperationException("Declared initial capital must fund four service bodies.");
            services[index] = service; economy.Sponsor(service, settings.InitialSponsorship);
            economy.RegisterWorkSink(provider.Gate.Spent);
            if (!economy.PayWork(service, provider.Gate.Attention, 32, out _)) throw new InvalidOperationException("Initial attention must be sponsored.");
            var cycle = acquisitionPhases[index] = new();
            var period = arm.BuyerProbePeriod ?? (arm.Opportunity ? 8 : 32); cycle.Attach(new Relation(Relation.Point(cycle, period), Relation.Point(cycle, 0)));
            var buyAxis = buyAxes[index] = new(); var countRole = countRoles[index] = new();
            var quoteRole = quoteRoles[index] = new(); var phaseRole = phaseRoles[index] = new();
            var count = NativeGraph.Count(service.UtilityLedger);
            selectors[index] = SupportDecisionSurface.Create(new Carrier(),
                SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(buyAxis, 1),
                    SupportDecisionSurface.Condition(new Carrier(), countRole, Relation.Segment(count, 0, arm.TrialAllowance - 1))),
                SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(buyAxis, 1),
                    SupportDecisionSurface.Condition(new Carrier(), quoteRole, Relation.Segment(service.UtilityAxis, 256, 1_000_000))),
                SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(buyAxis, 1),
                    SupportDecisionSurface.Condition(new Carrier(), phaseRole, Relation.Segment(cycle, 0, 0))),
                SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(buyAxis, 0)));
            var fallbackPhase = fallbackPhases[index] = new(); fallbackPhase.Attach(new Relation(Relation.Point(fallbackPhase, 2), Relation.Point(fallbackPhase, 0)));
            var fallbackRole = fallbackRoles[index] = new();
            fallbackPolicies[index] = SupportDecisionSurface.Create(new Carrier(),
                SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(provider.AdviceAxis, -1),
                    SupportDecisionSurface.Condition(new Carrier(), fallbackRole, Relation.Segment(fallbackPhase, 0, 0))),
                SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(provider.AdviceAxis, 1)));
        }
        var trace = new List<EcologyStep>(); var stats = new Dictionary<(int Phase, int Source), Aggregate>();
        var grossSum = 0L; var previousBurned = 0L;
        for (var tick = 0; tick < tape.Count; tick++)
        {
            var trial = tape[tick]; var sample = world.Request(trial);
            if (!world.TrySource(sample, out var sourceIndex)) throw new InvalidOperationException("An advertised request source must resolve locally.");
            var provider = providers[sourceIndex]; var service = services[sourceIndex];
            var reset = false; var refined = false; var permission = false;
            ProgressDecision? progressDecision = null;
            if (arm.ResetIdle && provider.Requests > 0)
            {
                reset = provider.ResetAfterIdle(economy, service, tick, 64, out var resetWitness);
                if (reset) economy.UpdateBody(service, provider.Root, resetWitness);
            }
            if (!service.Active) { if (service.Wallet.Value == 0) economy.Sponsor(service, 8); economy.Revive(service); }
            var score = service.QuoteUtility();
            var counts = NativeGraph.Count(service.UtilityLedger);
            // Bounded paid exploration remains possible for a rejected service.
            // Only an observed utility claim or a local phase can admit it.
            var selection = SupportDecisionSurface.Select(new Carrier(), selectors[sourceIndex],
                SupportDecisionSurface.Binding(new Carrier(), countRoles[sourceIndex], NativeGraph.Publication(counts, checked((long)counts.Value), counts)),
                SupportDecisionSurface.Binding(new Carrier(), quoteRoles[sourceIndex], score),
                SupportDecisionSurface.Binding(new Carrier(), phaseRoles[sourceIndex], NativeGraph.Publication(acquisitionPhases[sourceIndex], checked((long)acquisitionPhases[sourceIndex].Value), acquisitionPhases[sourceIndex])));
            var buy = NativeGraph.TryValue(selection, buyAxes[sourceIndex], out var admitted) && admitted.Ticks == 1;
            acquisitionPhases[sourceIndex].Add(1);
            lastAdmission = selection;
            var comparisons = 0;
            var comparisonReceipts = new List<Relation>();
            Relation Compare(Relation candidate, Relation anchor)
            {
                foreach (var receipt in comparisonReceipts)
                    if (SamplingSurface.TryReadComparison(receipt, candidate, anchor, provider.OrderAxis, out _)) return receipt;
                if (!economy.BurnWork(economy.Buyer, 1, out var paid)) return NativeGraph.Open(NativeGraph.G(candidate, anchor), provider.OrderAxis);
                comparisons++; var comparison = world.Compare(candidate, anchor);
                if (!ReadWitness.TryRead(comparison, out var selected, out var basis, out var result, out var residue, out var provenance)) return comparison;
                var captured = ReadWitness.Create(new Carrier(), selected, basis, result, residue, NativeGraph.G(paid, provenance));
                comparisonReceipts.Add(captured); return captured;
            }
            var offered = NativeGraph.Open(provider.Source, provider.AdviceAxis);
            var supplied = offered; var interpreted = false; long? adviceValue = null;
            var command = WorldPublication.Value(world.ActionAxis, 0, new Carrier());
            EconomyPurchase? purchase = null; var updatesBefore = provider.Updates;
            if (buy)
            {
                if (provider.Gate.Attention.Value < 2 && service.Wallet.Value >= 8)
                    economy.PayWork(service, provider.Gate.Attention, 8, out _);
                if (arm.Learning)
                {
                    var grantCount = provider.Gate.GrantCount;
                    var positive = provider.Gate.PositiveEpisodes; var change = provider.Gate.ChangeEpisodes;
                    var coverage = provider.Gate.CoverageEpisodes; var retests = provider.Gate.RetestEpisodes;
                    var traffic = WorldPublication.Value(provider.Gate.TrafficAxis, 1, new Carrier());
                    var canSponsor = OperationClaim.AtLeast(new Carrier(), economy.Buyer.ValueSlot, Relation.Point(economy.Buyer, 16), new Carrier());
                    var demand = WorldPublication.Value(provider.Gate.DemandAxis, NativeGraph.Pass(canSponsor) ? 1 : 0, canSponsor);
                    provider.Gate.TryPermission(provider.Gate.Decide(traffic, demand), out permission);
                    if (provider.Gate.Progress is { } progress)
                    {
                        var gate = provider.Gate;
                        var trigger = !gate.ProgressGated ? "error-only" : gate.PositiveEpisodes > positive ? "progress" :
                            gate.ChangeEpisodes > change ? "change" : gate.CoverageEpisodes > coverage ? "coverage" :
                            gate.RetestEpisodes > retests ? "retest" : grantCount < 16 ? "bootstrap" :
                            gate.RequestedLearning ? "episode" : gate.ErrorRequestedLearning ? "paused" : "settled";
                        progressDecision = new(checked((long)progress.Version), checked((long)progress.ObservationCount),
                            Read(progress.ReadProgress(), progress.ProgressAxis), Read(progress.ReadCoverageChange(), progress.CoverageAxis),
                            Read(progress.ReadRecentError(), progress.MeanErrorAxis), gate.ErrorRequestedLearning, gate.RequestedLearning, permission,
                            gate.EpisodeRemaining, gate.PositiveEpisodes, gate.ChangeEpisodes, gate.CoverageEpisodes, gate.RetestEpisodes, gate.SuppressedRequests, trigger);
                    }
                }
                offered = provider.Interpret(sample, Compare, tick);
                interpreted = WorldPublication.TryValue(offered, provider.AdviceAxis, out var advice) && advice is -1 or 1;
                if (interpreted) adviceValue = advice;
                supplied = arm.Communicate ? offered : NativeGraph.Open(NativeGraph.Identity(offered), provider.AdviceAxis);
                // The action calibration consumes only the advertised direction.
                var fallback = SupportDecisionSurface.Select(new Carrier(), fallbackPolicies[sourceIndex],
                    SupportDecisionSurface.Binding(new Carrier(), fallbackRoles[sourceIndex],
                        NativeGraph.Publication(fallbackPhases[sourceIndex], checked((long)fallbackPhases[sourceIndex].Value), fallbackPhases[sourceIndex])));
                NativeGraph.TryValue(fallback, provider.AdviceAxis, out var fallbackDirection); fallbackPhases[sourceIndex].Add(1);
                var chosenDirection = NativeFallback.Choose(supplied, provider.AdviceAxis, fallbackDirection.Ticks);
                var action = NativeGraph.TryValue(chosenDirection, provider.AdviceAxis, out var direction) && direction.Ticks is -1 or 1 ? direction.Ticks : 0;
                var actionLaw = Equation.Affine(provider.AdviceAxis, -1, 1, world.ActionAxis, -1, 1);
                var predicted = Equation.Predict(actionLaw, new CarrierLocation(provider.AdviceAxis, action), world.ActionAxis);
                command = WorldPublication.Value(world.ActionAxis, NativeGraph.TryValue(predicted, world.ActionAxis, out var motor) ? motor.Ticks : 0, supplied);
                if (!economy.TryPurchase(service, command, supplied, 1, out purchase, out _))
                { command = WorldPublication.Value(world.ActionAxis, 0, new Carrier()); buy = false; }
            }
            var physical = world.Execute(trial, command); var bound = NativeGraph.BindFeedback(command, physical, world.ResourceAxis);
            if (purchase is not null) economy.Settle(purchase, bound); else economy.Grant(physical);
            WorldPublication.TryValue(physical, world.ResourceAxis, out var gross);
            if (buy)
            {
                provider.Observe(sample, offered, command, bound, permission, Compare);
                refined = provider.TryRefine(economy, service, arm.Renew, permission, out _);
                economy.UpdateBody(service, provider.Root, NativeGraph.G(Relation.Point(timeAxis, tick), provider.Root));
            }
            if ((tick + 1) % 32 == 0)
            {
                for (var index = 0; index < providers.Length; index++)
                {
                    var idle = OperationClaim.AtLeast(new Carrier(), new CarrierLocation(timeAxis, tick),
                        new CarrierLocation(timeAxis, providers[index].LastSeen + 64), new Carrier());
                    if (NativeGraph.Pass(idle)) economy.Park(services[index]);
                }
                economy.Advance(1);
            }
            // Every registered wallet and sink participates once. Internal
            // fees cannot make this total exceed the declared external packets.
            if (economy.AccountedMoney != economy.ExternalNet) throw new InvalidOperationException($"Currency mismatch at {tick}.");
            var burnedNow = Burned(); var burned = burnedNow - previousBurned; previousBurned = burnedNow;
            var actionNow = WorldPublication.TryValue(command, world.ActionAxis, out var issued) ? checked((int)issued) : 0;
            var correct = gross > 0; grossSum += gross;
            var key = (trial.Phase, sourceIndex); if (!stats.TryGetValue(key, out var row)) stats.Add(key, row = new());
            row.Steps++; if (correct) row.Correct++; if (actionNow != 0) row.Answered++; if (interpreted) row.Interpreted++; if (buy) row.Purchases++;
            row.Updates += checked((int)(provider.Updates - updatesBefore)); if (refined) row.Refinements++; if (reset) row.Resets++;
            row.Gross += gross; row.Burned += burned; row.Comparisons += comparisons; row.Landmarks = provider.Landmarks;
            trace.Add(new(arm.Name, seed, tick, trial.Phase, sourceIndex, buy, interpreted, adviceValue, actionNow, correct, gross, burned,
                permission, refined, reset, provider.Landmarks, provider.UnmetPressure, checked((long)economy.Buyer.Value),
                checked((long)service.Wallet.Value), checked((long)provider.Gate.Attention.Value), economy.AccountedMoney, economy.ExternalNet, progressDecision));
        }
        var rows = stats.OrderBy(pair => pair.Key.Phase).ThenBy(pair => pair.Key.Source).Select(pair =>
        {
            var row = pair.Value; return new EcologyRow(arm.Name, seed, pair.Key.Phase, pair.Key.Source, row.Steps, row.Correct, row.Answered,
                row.Interpreted, row.Purchases, row.Updates, row.Refinements, row.Resets, row.Gross, row.Burned,
                (double)(row.Gross - row.Burned) / row.Steps, row.Comparisons, row.Landmarks);
        }).ToArray();
        var root = NativeGraph.G(economy.Root, NativeGraph.G(providers.Select(provider => (IElement)provider.Root).ToArray()),
            NativeGraph.G(acquisitionPhases), NativeGraph.G(selectors), lastAdmission, Relation.Point(timeAxis, tape.Count),
            NativeGraph.G(fallbackPhases), NativeGraph.G(fallbackPolicies), NativeGraph.G(countRoles.Concat(quoteRoles).Concat(phaseRoles).Concat(fallbackRoles).ToArray()));
        return new(arm.Name, seed, settings, tape.Digest(), grossSum, Burned(), (double)(grossSum - Burned()) / tape.Count,
            economy.AccountedMoney, economy.ExternalNet, rows, trace, providers.Select(provider => NativeInspection.Count(provider.Root)).ToArray(), NativeInspection.Count(root))
            { NativeRoot = root };

        long Burned() => checked((long)economy.CostSink.Value + providers.Where(provider => provider is not null).Sum(provider => checked((long)provider.Gate.Spent.Value)));
        static long? Read(Relation publication, Carrier axis) => NativeGraph.TryValue(publication, axis, out var point) ? point.Ticks : null;
    }
    private sealed class Aggregate
    {
        public int Steps, Correct, Answered, Interpreted, Purchases, Updates, Refinements, Resets, Comparisons;
        public long Gross, Burned, Landmarks;
    }
}
