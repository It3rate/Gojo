using CompositeEmergence;
using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using PrimitiveLearning;
using System.Text.Json.Serialization;

namespace UtilityEcology;

public sealed record SeedStudyArm(string Name, bool ProtectedTrials = true, bool CompareAdmission = true,
    long SeedEndowment = 320, bool ProgressBaseline = false);
public sealed record SeedState(int Tick, int Source, long Trials, long Accepted, long Rejected, long SeedSpent,
    long EarnedSpent, long SeedRemaining, long TrialWork, long TrainingCount, long ValidationCount, string Status,
    bool Launched, bool Promoted, bool Refused, long LastTrialId, long LastGain, long LastCoverage,
    long LastTrialBurn, string LastTrialReason, bool ValidationPrepared, long? IncumbentValidationAdvice, long? CandidateValidationAdvice);
public sealed record SeedStudyRun(string Arm, int Seed, EcologyRun Ecology, IReadOnlyList<SeedState> SeedTrace)
{
    [JsonIgnore] public Relation NativeRoot { get; init; } = null!;
}

/// <summary>Opt-in host apparatus over native seed purses, candidate support,
/// shared opaque comparison ports and conserved service payments. Legacy runner
/// remains unchanged. All candidate decisions live in SeededProvider's graph.</summary>
public static class SeedTrialRunner
{
    public static readonly SeedStudyArm[] Arms = [
        new("seed/error-only", ProtectedTrials: false),
        new("seed/progress", ProtectedTrials: false, ProgressBaseline: true),
        new("seed/paired"), new("seed/unfiltered", CompareAdmission: false),
        new("seed/no-seed", SeedEndowment: 0)
    ];
    public static SeedStudyRun Run(int seed, SeedStudyArm study, EcologySettings? settings = null)
    {
        settings ??= new();
        if (!study.ProtectedTrials)
        {
            var baseline = EcologyRunner.Run(seed, (study.ProgressBaseline ? EcologyRunner.ProgressArms[1] : EcologyRunner.ProgressArms[0]) with { Name = study.Name, RetestPeriod = 8 }, settings);
            return new(study.Name, seed, baseline, []) { NativeRoot = baseline.NativeRoot };
        }
        var arm = new EcologyArm(study.Name, Opportunity: false, SuccessPaid: false, BuyerProbePeriod: 8, MonitorProgress: true);
        if (settings.PhaseLength < 16 || settings.InitialCapital < 1024 || settings.Fee < 0 || settings.InitialSponsorship < 32)
            throw new ArgumentOutOfRangeException(nameof(settings));
        var tape = WorldTape.Create(seed, settings.PhaseLength); var world = new EcologyWorld(settings.Encoding);
        var economy = new ServiceEconomy(world.ResourceAxis, world.TimeAxis, settings.InitialCapital, 4);
        var providers = new SeededProvider[4]; var services = new EconomyService[4];
        var acquisitionPhases = new Carrier[4]; var selectors = new Relation[4];
        var buyAxes = new Carrier[4]; var countRoles = new Carrier[4]; var quoteRoles = new Carrier[4]; var phaseRoles = new Carrier[4];
        var fallbackPhases = new Carrier[4]; var fallbackPolicies = new Relation[4]; var fallbackRoles = new Carrier[4];
        var timeAxis = new Carrier(); var lastAdmission = NativeGraph.Open(new Placeholder(), new Placeholder());
        for (var index = 0; index < providers.Length; index++)
        {
            var provider = providers[index] = new SeededProvider(world.Sources[index], world.PresenceAxes[index], world.OrderAxes[index],
                world.ActionAxis, world.ResourceAxis, comparisonEnabled: study.CompareAdmission, seedBudget: 320);
            var law = arm.SuccessPaid ? ServiceEconomy.SuccessPaidLaw(world.ResourceAxis) : ServiceEconomy.UpfrontLaw(world.ResourceAxis);
            if (!economy.AddService(provider.Root, settings.Fee, 8, law, out var service, out _, activeRent: 1, parkedRent: 0) || service is null)
                throw new InvalidOperationException("Declared initial capital must fund four service bodies.");
            services[index] = service; economy.Sponsor(service, study.SeedEndowment + 64);
            economy.RegisterWorkSink(provider.Base.Gate.Spent);
            if (!economy.PayWork(service, provider.Base.Gate.Attention, 64, out _)) throw new InvalidOperationException("Initial attention must be sponsored.");
            if (!economy.PayWork(service, provider.SeedEnergy, study.SeedEndowment, out var seedFunding)) throw new InvalidOperationException("Initial seed energy must be conserved funding.");
            provider.RecordSeedFunding(seedFunding);
            economy.UpdateBody(service, provider.Root, seedFunding);
            if (service.Wallet.Value != 0) throw new InvalidOperationException("Only future fees may fund later candidates.");
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
                SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(provider.Base.AdviceAxis, -1),
                    SupportDecisionSurface.Condition(new Carrier(), fallbackRole, Relation.Segment(fallbackPhase, 0, 0))),
                SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(provider.Base.AdviceAxis, 1)));
        }
        var trace = new List<EcologyStep>(); var seedTrace = new List<SeedState>(); var stats = new Dictionary<(int Phase, int Source), Aggregate>();
        var grossSum = 0L; var previousBurned = 0L;
        for (var tick = 0; tick < tape.Count; tick++)
        {
            var trial = tape[tick]; var sample = world.Request(trial);
            if (!world.TrySource(sample, out var sourceIndex)) throw new InvalidOperationException("An advertised request source must resolve locally.");
            var provider = providers[sourceIndex]; var service = services[sourceIndex];
            var reset = false; var refined = false; var permission = false;
            ProgressDecision? progressDecision = null;
            if (!service.Active) economy.Revive(service);
            var beforeTrials = provider.Trials; var beforeAccepted = provider.Accepted; var beforeRejected = provider.Rejected;
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
            var comparisonReceipts = new List<(Carrier Payer, Relation Receipt)>();
            Relation ComparePaid(Carrier payer, Relation candidate, Relation anchor)
            {
                foreach (var item in comparisonReceipts)
                    if (item.Payer.SameIdentityAs(payer) && SamplingSurface.TryReadComparison(item.Receipt, candidate, anchor, provider.Base.OrderAxis, out _)) return item.Receipt;
                if (!economy.BurnWork(payer, 1, out var paid)) return NativeGraph.Open(NativeGraph.G(candidate, anchor), provider.Base.OrderAxis);
                comparisons++; var comparison = world.Compare(candidate, anchor);
                if (!ReadWitness.TryRead(comparison, out var selected, out var basis, out var result, out var residue, out var provenance)) return comparison;
                var captured = ReadWitness.Create(new Carrier(), selected, basis, result, residue, NativeGraph.G(paid, provenance));
                comparisonReceipts.Add((payer, captured)); return captured;
            }
            Relation Compare(Relation candidate, Relation anchor) => ComparePaid(economy.Buyer, candidate, anchor);
            var offered = NativeGraph.Open(provider.Base.Source, provider.Base.AdviceAxis);
            var supplied = offered; var interpreted = false; long? adviceValue = null;
            var command = WorldPublication.Value(world.ActionAxis, 0, new Carrier());
            EconomyPurchase? purchase = null; var updatesBefore = provider.Base.Updates;
            if (buy)
            {
                if (provider.Base.Gate.Attention.Value < 2 && service.Wallet.Value >= 8)
                    economy.PayWork(service, provider.Base.Gate.Attention, 8, out _);
                if (arm.Learning)
                {
                    var grantCount = provider.Base.Gate.GrantCount;
                    var positive = provider.Base.Gate.PositiveEpisodes; var change = provider.Base.Gate.ChangeEpisodes;
                    var coverage = provider.Base.Gate.CoverageEpisodes; var retests = provider.Base.Gate.RetestEpisodes;
                    var traffic = WorldPublication.Value(provider.Base.Gate.TrafficAxis, 1, new Carrier());
                    var canSponsor = OperationClaim.AtLeast(new Carrier(), economy.Buyer.ValueSlot, Relation.Point(economy.Buyer, 16), new Carrier());
                    var demand = WorldPublication.Value(provider.Base.Gate.DemandAxis, NativeGraph.Pass(canSponsor) ? 1 : 0, canSponsor);
                    provider.Base.Gate.TryPermission(provider.Base.Gate.Decide(traffic, demand), out permission);
                    if (provider.Base.Gate.Progress is { } progress)
                    {
                        var gate = provider.Base.Gate;
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
                interpreted = WorldPublication.TryValue(offered, provider.Base.AdviceAxis, out var advice) && advice is -1 or 1;
                if (interpreted) adviceValue = advice;
                supplied = arm.Communicate ? offered : NativeGraph.Open(NativeGraph.Identity(offered), provider.Base.AdviceAxis);
                // The action calibration consumes only the advertised direction.
                var fallback = SupportDecisionSurface.Select(new Carrier(), fallbackPolicies[sourceIndex],
                    SupportDecisionSurface.Binding(new Carrier(), fallbackRoles[sourceIndex],
                        NativeGraph.Publication(fallbackPhases[sourceIndex], checked((long)fallbackPhases[sourceIndex].Value), fallbackPhases[sourceIndex])));
                NativeGraph.TryValue(fallback, provider.Base.AdviceAxis, out var fallbackDirection); fallbackPhases[sourceIndex].Add(1);
                var chosenDirection = NativeFallback.Choose(supplied, provider.Base.AdviceAxis, fallbackDirection.Ticks);
                var action = NativeGraph.TryValue(chosenDirection, provider.Base.AdviceAxis, out var direction) && direction.Ticks is -1 or 1 ? direction.Ticks : 0;
                var actionLaw = Equation.Affine(provider.Base.AdviceAxis, -1, 1, world.ActionAxis, -1, 1);
                var predicted = Equation.Predict(actionLaw, new CarrierLocation(provider.Base.AdviceAxis, action), world.ActionAxis);
                command = WorldPublication.Value(world.ActionAxis, NativeGraph.TryValue(predicted, world.ActionAxis, out var motor) ? motor.Ticks : 0, supplied);
                if (!economy.TryPurchase(service, command, supplied, 1, out purchase, out _))
                { command = WorldPublication.Value(world.ActionAxis, 0, new Carrier()); buy = false; }
            }
            var validationPrepared = buy && provider.FreezeIncumbent;
            long? incumbentValidationAdvice = null; long? candidateValidationAdvice = null;
            if (buy)
            {
                provider.PrepareValidation(sample, command, ComparePaid, economy, service);
                if (validationPrepared)
                {
                    incumbentValidationAdvice = provider.IncumbentValidationAdvice;
                    candidateValidationAdvice = provider.CandidateValidationAdvice;
                }
            }
            var physical = world.Execute(trial, command); var bound = NativeGraph.BindFeedback(command, physical, world.ResourceAxis);
            if (purchase is not null) economy.Settle(purchase, bound); else economy.Grant(physical);
            WorldPublication.TryValue(physical, world.ResourceAxis, out var gross);
            if (buy)
            {
                provider.Observe(sample, offered, command, bound, permission, ComparePaid, economy, service);
                provider.TryLaunch(economy, service, out _);
                refined = provider.Accepted > beforeAccepted;
                economy.UpdateBody(service, provider.Root, NativeGraph.G(Relation.Point(timeAxis, tick), provider.Root));
            }
            if ((tick + 1) % 32 == 0)
            {
                for (var index = 0; index < providers.Length; index++)
                {
                    var idle = OperationClaim.AtLeast(new Carrier(), new CarrierLocation(timeAxis, tick),
                        new CarrierLocation(timeAxis, providers[index].Base.LastSeen + 64), new Carrier());
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
            row.Updates += checked((int)(provider.Base.Updates - updatesBefore)); if (refined) row.Refinements++; if (reset) row.Resets++;
            row.Gross += gross; row.Burned += burned; row.Comparisons += comparisons; row.Landmarks = (provider.Current?.LandmarkCount ?? 0);
            trace.Add(new(arm.Name, seed, tick, trial.Phase, sourceIndex, buy, interpreted, adviceValue, actionNow, correct, gross, burned,
                permission, refined, reset, (provider.Current?.LandmarkCount ?? 0), provider.Base.UnmetPressure, checked((long)economy.Buyer.Value),
                checked((long)service.Wallet.Value), checked((long)provider.Base.Gate.Attention.Value), economy.AccountedMoney, economy.ExternalNet, progressDecision));
            seedTrace.Add(new(tick, sourceIndex, provider.Trials, provider.Accepted, provider.Rejected,
                provider.SeedSpent, provider.EarnedSpent, checked((long)provider.SeedEnergy.Value), provider.TrialWork,
                provider.TrainingCount, provider.ValidationCount, provider.Status,
                provider.Trials > beforeTrials, provider.Accepted > beforeAccepted, provider.Rejected > beforeRejected,
                provider.LastTrialId, provider.LastGain, provider.LastCoverage, provider.LastTrialBurn, provider.LastTrialReason,
                validationPrepared, incumbentValidationAdvice, candidateValidationAdvice));
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
        var ecology = new EcologyRun(arm.Name, seed, settings, tape.Digest(), grossSum, Burned(), (double)(grossSum - Burned()) / tape.Count,
            economy.AccountedMoney, economy.ExternalNet, rows, trace, providers.Select(provider => NativeInspection.Count(provider.Root)).ToArray(), NativeInspection.Count(root))
            { NativeRoot = root };
        return new(study.Name, seed, ecology, seedTrace) { NativeRoot = root };

        long Burned() => checked((long)economy.CostSink.Value + providers.Where(provider => provider is not null).Sum(provider => checked((long)provider.Base.Gate.Spent.Value)));
        static long? Read(Relation publication, Carrier axis) => NativeGraph.TryValue(publication, axis, out var point) ? point.Ticks : null;
    }
    private sealed class Aggregate
    {
        public int Steps, Correct, Answered, Interpreted, Purchases, Updates, Refinements, Resets, Comparisons;
        public long Gross, Burned, Landmarks;
    }
}
