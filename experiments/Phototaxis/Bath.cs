using Gojo.Core;
using System.Diagnostics;
using static Phototaxis.Native;

namespace Phototaxis;

public readonly record struct Rock(double X, double Y, double Radius);
public readonly record struct StingZone(double X, double Y, double Radius);
public readonly record struct Trend(double Seconds, double Food, double Energy, double Exposure, long Deaths);

/// <summary>Physical world and execution host; decisions and accounting are connected Gojo reads.</summary>
public sealed class Bath
{
    public const double Width = 600;
    public const double Height = 408;
    public const int TicksPerSecond = 20;
    public const double VisionDistance = 10;
    public const int PopulationCeiling = 128;
    public const int BreedingTicks = 4;
    public const int MaturityTicks = 400;
    public const int MatingCooldownTicks = 200;
    public const int ParentEnergy = 780;
    public const int BirthContribution = 260;
    public const double BreedingDarkness = .28;
    public const int MaximumHarvestPerPulse = 10;
    public const double PhotosyntheticFloor = .12;
    public const int InactivityCostPerPulse = 12;
    public const double SwimmingSpeed = 8;
    public const double StationaryRiskMultiplier = 12;
    public const double RockRiskMultiplier = 3;
    public const double BodyRadius = 9;
    public const double MatingDistance = BodyRadius * 2;
    public const double MateSensingDistance = 80;
    private const double MaximumStepDistance = 30 * .083;
    public int Seed { get; }
    public int InitialPopulation { get; }
    public int EyeLevels { get; }
    public double ArenaScale { get; }
    public double ArenaWidth => Width * ArenaScale;
    public double ArenaHeight => Height * ArenaScale;
    public PilotFeatures PilotFeatures { get; }
    public EcologySettings Ecology { get; private set; }
    public bool BinocularVision { get; }
    public Units Units { get; } = new();
    public Carrier Clock { get; }
    public Carrier CalorieReserve { get; }
    public Carrier MetabolicSink { get; }
    public Carrier DeathSink { get; }
    public Carrier PhotoPool { get; }
    public Carrier[] LocalPhotoPools { get; private set; }
    public Carrier LocalFoodShortfall { get; }
    public Carrier AttentionReserve { get; }
    public Carrier SpentAttention { get; }
    public Carrier MachineDuty { get; }
    public Carrier ResourceYield { get; }
    public Carrier NextBreeding { get; }
    public Carrier BreedingInterval { get; }
    public Carrier BirthCount { get; }
    public Carrier PredationCount { get; }
    public Carrier StarvationCount { get; }
    public Carrier IntakeCount { get; }
    public Carrier RewriteCount { get; }
    public Carrier ComputeCostCount { get; }
    public Relation SupplyLaw { get; }
    public Relation LastSupply { get; private set; } = G(new Placeholder());
    public Relation LastPool { get; private set; } = G(new Placeholder());
    public Relation LastBreeding { get; private set; } = G(new Placeholder());
    public Relation LastDeath { get; private set; } = G(new Placeholder());
    public Creature[] Creatures { get; private set; }
    public Rock[] Rocks { get; } = [new(171, 126, 20), new(432, 282, 25), new(291, 294, 17), new(444, 117, 15)];
    public StingZone[] StingZones { get; } = [new(159, 233, 26), new(415, 174, 29)];
    public bool CognitiveEcology { get; }
    public bool BreedingSelection { get; set; } = true;
    public bool InquiriesEnabled { get; set; }
    public bool RecoverableHazards { get; set; }
    public long Injuries { get; private set; }
    public long InjuryCalories { get; private set; }
    public List<Trend> Trends { get; } = [];
    public MachineResources Resources { get; }
    public TimedLightField TimedLights { get; private set; }
    private readonly Action<Units, Creature>? configureController;
    public bool AuthoredControllers => configureController is not null;
    private bool pulsedLights;
    public bool PulsedLights { get => pulsedLights; set { pulsedLights = value; lightTick = -1; } }
    private long lightTick = -1;
    private LightPatch[] lightFrame = [];
    public LightPatch[] ActiveLights
    {
        get
        {
            if (lightTick == Tick) return lightFrame;
            lightTick = Tick;
            return lightFrame = PulsedLights ? TimedLights.At(Tick) :
                [new(LightX, LightY, 90 * Ecology.LightRadius / 72, double.PositiveInfinity)];
        }
    }
    public bool Learning { get; set; } = true;
    public bool ExposureRisk { get; set; } = true;
    public bool RocksEnabled { get; set; }
    public bool Breeding { get; set; } = true;
    public PhaseCosts? Measure { get; set; }
    public long PredationDeaths => (long)PredationCount.Value;
    public long StarvationDeaths => (long)StarvationCount.Value;
    public long Births => (long)BirthCount.Value;
    public long TotalFood => (long)IntakeCount.Value;
    public long Rewrites => (long)RewriteCount.Value;
    public double FoodAvailability => ResourceYield.Value / 1000.0;
    public long Tick => (long)Clock.Value;
    public double Seconds => Tick / (double)TicksPerSecond;
    public double LightX => WeatherFrame().X;
    public double LightY => WeatherFrame().Y;
    public double Daylight => WeatherFrame().Day;
    public double Clouds => WeatherFrame().Cloud;
    public double MeanEnergy => Creatures.Length == 0 ? 0 : Creatures.Average(c => c.Fullness);
    public double MeanExposure => Creatures.Length == 0 ? 0 : Creatures.Average(c => Illumination(c.Px, c.Py));
    public double MeanSpeed => Creatures.Length == 0 ? 0 : Creatures.Average(c => movement.GetValueOrDefault(c).Speed);
    public double StationaryFraction => Creatures.Length == 0 ? 0 : Creatures.Count(c => movement.GetValueOrDefault(c).Speed < 1) / (double)Creatures.Length;
    public int Operations => Creatures.Sum(c => ExpressionGraph.Count(c.Features,
        c.LeftMuscle.Current.Components.Concat(c.RightMuscle.Current.Components).Select(t => t.Source).ToArray()));
    public double StationaryShare => movementSamples == 0 ? 0 : stationarySamples / (double)movementSamples;
    public double? HungryTowardLight => hungrySamples == 0 ? null : hungryTowardSamples / (double)hungrySamples;
    public double? FullAwayFromLight => fullSamples == 0 ? null : fullAwaySamples / (double)fullSamples;
    public long HungryMovementSamples => hungrySamples;
    public long FullMovementSamples => fullSamples;
    public double? HungryGradientAlignment => gradientSamples == 0 ? null : gradientAlignment / gradientSamples;
    public long HungryGradientSamples => gradientSamples;
    public int Components => Creatures.Sum(c => c.ComponentCount);
    public long ComputeCalories => (long)ComputeCostCount.Value;
    public long TotalCalories => checked((long)CalorieReserve.Value + (long)MetabolicSink.Value + (long)DeathSink.Value +
        (long)PhotoPool.Value + LocalPhotoPools.Sum(p => (long)p.Value) + Creatures.Sum(c => (long)c.Energy.Value));
    public long TotalAttention => checked((long)AttentionReserve.Value + (long)SpentAttention.Value + Creatures.Sum(c => (long)c.Wallet.Value));
    public Relation Root => G(Units.Root, Clock,
        G(new IElement[] { CalorieReserve, MetabolicSink, DeathSink, PhotoPool }.Concat(LocalPhotoPools).ToArray()),
        G(AttentionReserve, SpentAttention), G(Creatures.Select(c => (IElement)c.Root).ToArray()),
        G(MachineDuty, ResourceYield, SupplyLaw, LastSupply, NextBreeding, BreedingInterval, LastBreeding,
            G(BirthCount, PredationCount, StarvationCount, IntakeCount, RewriteCount, ComputeCostCount), LastDeath, LastPool,
            G(LocalFoodShortfall, G(LocalPhotoPools.Select(p => (IElement)G(p, L(p, Ecology.PatchHarvestPerPulse))).ToArray()))));
    private readonly Random births;
    private int nextIdentity;
    private long foodAtLastTrend;
    private const long CalorieBudget = 20_000_000;
    private const long AttentionBudget = 10_000_000;
    private long weatherTick = -1;
    private (double X, double Y, double Day, double Cloud) weatherFrame;
    private readonly Relation pulseSupply;
    // External kinematics, sampled from the body's actual displacement. These
    // observations do not participate in the learner's signal catalogue.
    private readonly Dictionary<Creature, (double Speed, bool RockContact)> movement = [];
    private readonly Dictionary<Creature, (long Until, int Strength)> injuries = [];
    // Read-only world diagnostics. These observations never enter market bids or circuit ports.
    private long movementSamples, stationarySamples, hungrySamples, hungryTowardSamples, fullSamples, fullAwaySamples;
    private long gradientSamples;
    private double gradientAlignment;

    private (double X, double Y, double Day, double Cloud) WeatherFrame()
    {
        if (weatherTick == Tick) return weatherFrame;
        weatherTick = Tick;
        var time = Seconds;
        return weatherFrame = (ArenaWidth * .5 + Math.Sin(time / 140) * ArenaWidth * .22, ArenaHeight * .5 + Math.Cos(time / 170) * ArenaHeight * .18,
            .4 + .6 * (.5 + .5 * Math.Cos(time * Math.Tau / 240)),
            1 - .48 * Math.Pow(Math.Max(0, Math.Sin(time / 19 + .4)), 6));
    }

    public Bath(int seed = 7, int population = 20, bool rocks = false, MachineResources? resources = null,
        bool pulsedLights = false, Action<Units, Creature>? configureController = null,
        bool cognitiveEcology = false, bool inquiries = false, bool breedingSelection = true, int? eyeLevels = null,
        EcologySettings? ecology = null, bool binocular = true, double arenaScale = 1, PilotFeatures? pilotFeatures = null)
    {
        if (population < 0 || population > PopulationCeiling) throw new ArgumentOutOfRangeException(nameof(population));
        Seed = seed; InitialPopulation = population; RocksEnabled = rocks;
        if (!double.IsFinite(arenaScale) || arenaScale < 1 || arenaScale > 4) throw new ArgumentOutOfRangeException(nameof(arenaScale));
        ArenaScale = arenaScale; PilotFeatures = pilotFeatures ?? PilotFeatures.Current;
        EyeLevels = eyeLevels ?? (cognitiveEcology ? 256 : 16);
        if (EyeLevels is < 2 or > 65536) throw new ArgumentOutOfRangeException(nameof(eyeLevels));
        Resources = resources ?? new MachineResources();
        Ecology = ecology ?? EcologySettings.Original; Ecology.Validate(); BinocularVision = binocular;
        TimedLights = new TimedLightField(seed, Ecology, arenaWidth:ArenaWidth, arenaHeight:ArenaHeight); PulsedLights = pulsedLights;
        this.configureController = configureController;
        CognitiveEcology = cognitiveEcology; InquiriesEnabled = inquiries; BreedingSelection = breedingSelection;
        RecoverableHazards = cognitiveEcology;
        births = new Random(seed ^ 0x1f3ab7);
        Clock = new Carrier(Units.Clock.UnitShape);
        CalorieReserve = new Carrier(Units.Calorie.UnitShape);
        MetabolicSink = new Carrier(Units.Calorie.UnitShape);
        DeathSink = new Carrier(Units.Calorie.UnitShape);
        PhotoPool = new Carrier(Units.Calorie.UnitShape);
        LocalPhotoPools = Ecology.PatchHarvestPerPulse == 0 ? [] :
            TimedLights.Schedules.Select(_ => Limited(Units.Calorie, Ecology.PatchHarvestPerPulse)).ToArray();
        LocalFoodShortfall = new Carrier(Units.Calorie.UnitShape);
        AttentionReserve = new Carrier(Units.Attention.UnitShape);
        SpentAttention = new Carrier(Units.Attention.UnitShape);
        MachineDuty = Limited(Units.Count, 1000);
        ResourceYield = Limited(Units.Count, 1000);
        NextBreeding = new Carrier(Units.Clock.UnitShape);
        BreedingInterval = new Carrier(Units.Clock.UnitShape);
        BirthCount = new Carrier(Units.Count.UnitShape);
        PredationCount = new Carrier(Units.Count.UnitShape);
        StarvationCount = new Carrier(Units.Count.UnitShape);
        IntakeCount = new Carrier(Units.Calorie.UnitShape);
        RewriteCount = new Carrier(Units.Count.UnitShape);
        ComputeCostCount = new Carrier(Units.Calorie.UnitShape);
        SupplyLaw = SumTo(new Carrier(Units.Count.UnitShape),
            (L(MachineDuty, 1000), 1), (Scale(MachineDuty.ValueSlot, 4, 5), -1));
        pulseSupply = Scale(ResourceYield.ValueSlot, 360, 1000, new Carrier(Units.Calorie.UnitShape));
        UpdateSupply();
        Aim(BreedingInterval, BreedingTicks); Aim(NextBreeding, BreedingTicks);
        CalorieReserve.Add((ulong)CalorieBudget); AttentionReserve.Add((ulong)AttentionBudget);
        Creatures = Enumerable.Range(0, population).Select(_ => MakeCreature()).ToArray();
        RecordTrend();
    }

    private Creature MakeCreature(Creature? a = null, Creature? b = null)
    {
        var x = a is null ? 30 + births.NextDouble() * (ArenaWidth - 60) : Wrap(a.Px + WrappedDelta(a.Px, b!.Px, ArenaWidth) / 2 + births.NextDouble() * 12 - 6, ArenaWidth);
        var y = a is null ? 30 + births.NextDouble() * (ArenaHeight - 60) : Wrap(a.Py + WrappedDelta(a.Py, b!.Py, ArenaHeight) / 2 + births.NextDouble() * 12 - 6, ArenaHeight);
        if (RocksEnabled && Rocks.Any(r => Distance(x, y, r.X, r.Y) < r.Radius + 10)) { x = 80; y = 80; }
        var child = new Creature(Units, nextIdentity++, Tick, x, y, births.NextDouble() * Math.Tau, births, SpentAttention, ResourceYield,
            CognitiveEcology ? 100 : Computation.CaloriesPerRuntimeSecond, EyeLevels, ArenaWidth, ArenaHeight);
        child.LastCredit = Transfer(AttentionReserve, child.Wallet, 24);
        if (a is null) child.LastIntake = Transfer(CalorieReserve, child.Energy, 720);
        else
        {
            if (CognitiveEcology && a.Cognition is { } ca && b!.Cognition is { } cb)
            {
                var modules = CognitivePilot.Mate(Units, ca.Pilot, cb.Pilot, child, Tick, BreedingSelection, out var market);
                child.ConfigureCognition(modules, InquiriesEnabled && (ca.Inquiry.Enabled.Value != 0 || cb.Inquiry.Enabled.Value != 0), market,
                    binocular: BinocularVision, features:PilotFeatures);
                child.RecordDesignedInheritance(a, b);
            }
            else if (configureController is null) child.Inherit(a, b!, Tick);
            else { configureController(Units, child); child.RecordDesignedInheritance(a, b!); }
            child.LastIntake = child.LeftMuscle.Current.Components.Length == 0 || child.RightMuscle.Current.Components.Length == 0
                ? G(child.Inheritance, new Placeholder())
                : G(Transfer(a.Energy, child.Energy, BirthContribution), Transfer(b!.Energy, child.Energy, BirthContribution));
        }
        if (a is null)
        {
            if (CognitiveEcology) child.ConfigureCognition(asksJev: InquiriesEnabled && child.Slot % 3 == 0, binocular: BinocularVision, features:PilotFeatures);
            else configureController?.Invoke(Units, child);
        }
        return child;
    }

    public void ApplyEcology(EcologySettings settings)
    {
        settings.Validate();
        if (Ecology == settings) return;
        var oldPools = G(LocalPhotoPools);
        var release = G(LocalPhotoPools.Select(p => (IElement)Transfer(p, CalorieReserve, (long)p.Value)).ToArray());
        TimedLights = TimedLights.WithSettings(settings, Tick); Ecology = settings;
        LocalPhotoPools = settings.PatchHarvestPerPulse == 0 ? [] :
            TimedLights.Schedules.Select(_ => Limited(Units.Calorie, settings.PatchHarvestPerPulse)).ToArray();
        LastPool = G(L(Clock, Tick), oldPools, release, G(LocalPhotoPools));
        lightTick = -1;
    }

    public double Illumination(double x, double y)
    {
        if (RocksEnabled && Rocks.Any(r => Distance(x, y, r.X, r.Y) <= r.Radius)) return 0;
        return AmbientIllumination(x, y);
    }

    private double AmbientIllumination(double x, double y)
    {
        var patch = 0.0;
        foreach (var source in ActiveLights)
        {
            var dx = x - source.X; var dy = y - source.Y;
            patch = Math.Max(patch, Math.Exp(-(dx * dx + dy * dy) / (2 * source.Radius * source.Radius)));
        }
        return Math.Clamp(.025 + ((PulsedLights ? 0 : .07) + .9 * patch) * Daylight * Clouds, 0, 1);
    }

    private (double X, double Y) LocalLightDirection(double x, double y)
    {
        if (ActiveLights.Length == 0) return (0, 0);
        var source = ActiveLights.MinBy(s => Math.Pow((s.X - x) / s.Radius, 2) + Math.Pow((s.Y - y) / s.Radius, 2));
        return (source.X - x, source.Y - y);
    }

    // This observer reads the physical field, never a creature's utility or bids.
    public (double X, double Y) LightGradient(double x, double y)
    {
        var best = 0.0; var gx = 0.0; var gy = 0.0;
        foreach (var source in ActiveLights)
        {
            var dx = source.X - x; var dy = source.Y - y;
            var strength = Math.Exp(-(dx * dx + dy * dy) / (2 * source.Radius * source.Radius));
            if (strength <= best) continue;
            best = strength;
            var scale = .9 * Daylight * Clouds * strength / (source.Radius * source.Radius);
            gx = dx * scale; gy = dy * scale;
        }
        return (gx, gy);
    }

    private void BeginPhotoPulse()
    {
        var expired = G(LocalPhotoPools.Select(p => (IElement)Transfer(p, CalorieReserve, (long)p.Value)).ToArray());
        var recycled = Transfer(PhotoPool, CalorieReserve, (long)PhotoPool.Value);
        var allowance = ReadMany(pulseSupply)[0];
        var supply = TryValue(allowance, out var budget) ? Transfer(CalorieReserve, PhotoPool, budget) : G(new Placeholder());
        var allocations = new List<IElement>();
        if (LocalPhotoPools.Length != 0 && ActiveLights.Length != 0)
        {
            // Native finite stocks: reserve -> shared pool -> local pool -> body.
            // Equal whole-packet allocation, with rotating remainder access.
            var count = ActiveLights.Length;
            var total = Math.Min((long)PhotoPool.Value, (long)Ecology.PatchHarvestPerPulse * count);
            for (var ordinal = 0; ordinal < count; ordinal++)
            {
                var source = ActiveLights[(ordinal + (int)(Tick / 4 % count)) % count];
                var amount = total / count + (ordinal < total % count ? 1 : 0);
                var pool = LocalPhotoPools[source.Source];
                allocations.Add(G(G(Relation.Point(Units.Position, (long)(source.X * 16)), Relation.Point(Units.Position, (long)(source.Y * 16))),
                    L(pool, Ecology.PatchHarvestPerPulse), Transfer(PhotoPool, pool, amount)));
            }
        }
        LastPool = G(L(Clock, Tick), pulseSupply, expired, recycled, allowance, supply, G(allocations.ToArray()));
    }

    public Carrier? FeedingPool(double x, double y)
    {
        if (LocalPhotoPools.Length == 0) return PhotoPool;
        if (ActiveLights.Length == 0) return null;
        var source = ActiveLights.MinBy(s => Math.Pow((s.X - x) / s.Radius, 2) + Math.Pow((s.Y - y) / s.Radius, 2));
        return LocalPhotoPools[source.Source];
    }

    public int[] Photograph(Creature creature, Eye eye)
    {
        var origin = EyeOrigin(creature, eye);
        var angle = creature.Angle + eye.Offset * Math.PI / 180;
        var dx = Math.Cos(angle); var dy = Math.Sin(angle);
        var integrated = 0.0;
        for (var distance = 1; distance <= (int)VisionDistance; distance++)
        {
            var brightness = Illumination(Wrap(origin.X + dx * distance, ArenaWidth), Wrap(origin.Y + dy * distance, ArenaHeight));
            if (brightness <= 0) break; // Nothing beyond the opaque/dark hit can add light.
            integrated += brightness;
        }
        return [(int)Math.Round(integrated / VisionDistance * eye.Maximum.Ticks)];
    }

    public static (double X, double Y) EyeOrigin(Creature creature, Eye eye)
    {
        var side = eye.Offset < 0 ? -3.5 : 3.5;
        var c = Math.Cos(creature.Angle); var s = Math.Sin(creature.Angle);
        return (creature.Px + c * 6 - s * side, creature.Py + s * 6 + c * side);
    }

    private void UpdateSupply()
    {
        var duty = (long)Math.Round(Resources.Duty * 1000);
        if (MachineDuty.Value == (ulong)duty && ResourceYield.Value != 0) return;
        var previous = L(MachineDuty, (long)MachineDuty.Value);
        var admission = Aim(MachineDuty, duty);
        var read = ReadMany(SupplyLaw)[0];
        var yield = TryValue(read, out var amount) ? Aim(ResourceYield, amount) : read;
        LastSupply = G(previous, L(MachineDuty, duty), admission, read, yield);
    }

    public void Step(int ticks = 1)
    {
        for (var step = 0; step < ticks; step++)
        {
            var stepStart = Resources.Begin();
            var stamp = Stopwatch.GetTimestamp();
            Clock.Add(1); UpdateSupply();
            _ = ActiveLights; // Shared world photometry is outside individual body bills.
            if (Tick % 4 == 0)
                BeginPhotoPulse();
            Measure?.Add("supply", ref stamp);
            // Rotate access to the finite physical pool; array position is not fitness.
            var count = Creatures.Length;
            for (var ordinal = 0; ordinal < count; ordinal++)
            {
                var creature = Creatures[(ordinal + (int)(Tick / 4 % count)) % count];
                var workStart = Resources.Begin();
                if (creature.Cognition is { } brain)
                {
                    if (Tick % 4 == 0)
                    {
                        AdmitBodyObservations(creature, brain.Observations);
                        brain.Observe();
                    }
                }
                else creature.Age.Add(1);
                Measure?.Add("cognition", ref stamp);
                if (Tick >= (long)creature.LeftEye.NextTime.Value) creature.LeftEye.Admit(Photograph(creature, creature.LeftEye), Tick);
                if (Tick >= (long)creature.RightEye.NextTime.Value) creature.RightEye.Admit(Photograph(creature, creature.RightEye), Tick);
                Measure?.Add("eyes", ref stamp);
                creature.AdvanceController(Tick);
                if (Tick >= (long)creature.LeftMuscle.NextTime.Value) creature.LeftMuscle.Wake(creature, Tick);
                if (Tick >= (long)creature.RightMuscle.NextTime.Value) creature.RightMuscle.Wake(creature, Tick);
                Measure?.Add("circuit", ref stamp);
                Move(creature);
                creature.Cognition?.ForecastNext(Tick);
                Measure?.Add("motion-prediction", ref stamp);
                if (Tick % 4 == 0)
                {
                    creature.LastWork = creature.ReadMotion(out var effort, out _)
                        ? Transfer(creature.Energy, MetabolicSink, 1 + effort / 12 + (long)Math.Round(InactivityCostPerPulse * Stillness(creature))) : creature.LastMotion;
                    Photosynthesize(creature);
                }
                if (Tick >= (long)creature.NextInspection.Value)
                {
                    creature.LastCredit = Transfer(AttentionReserve, creature.Wallet, Math.Max(0, 24 - (long)creature.Wallet.Value));
                    var before = creature.Rewrites;
                    if (creature.Cognition is null) creature.Inspect(Tick, Learning);
                    else
                    {
                        // Native predictive pressure is owned by the body. This is only an execution clock.
                        Aim(creature.NextInspection, Tick + Creature.EvaluationTicks);
                    }
                    RewriteCount.Add((ulong)(creature.Rewrites - before));
                }
                if (Tick % TicksPerSecond == 0) Bill(creature);
                if (RecoverableHazards && Tick % TicksPerSecond == 0 &&
                    StingZones.Any(z => ArenaDistance(creature.Px, creature.Py, z.X, z.Y) < z.Radius + BodyRadius))
                    Injure(creature, 40);
                creature.Computation.Runtime.Add((ulong)Resources.BodyWorkSince(workStart));
                Measure?.Add("work-photosynthesis-search", ref stamp);
            }
            if (Tick % 4 == 0) LastPool = G(LastPool, Transfer(PhotoPool, CalorieReserve, (long)PhotoPool.Value),
                G(LocalPhotoPools.Select(p => (IElement)Transfer(p, CalorieReserve, (long)p.Value)).ToArray()));
            RemoveDeaths();
            if (Tick >= (long)NextBreeding.Value)
            {
                if (Breeding) Breed();
                Aim(NextBreeding, Tick + (long)BreedingInterval.Value);
            }
            if (Tick % 100 == 0) RecordTrend();
            Measure?.Add("environment-mating", ref stamp);
            Resources.ObserveStep(stepStart);
        }
    }

    public void Move(Creature creature)
    {
        var beforeX = creature.Px; var beforeY = creature.Py;
        if (!creature.ReadMotion(out var thrust, out var yaw))
        { movement[creature] = (0, TouchesRock(beforeX, beforeY)); return; }
        var angle = creature.Angle + yaw * .005;
        var speed = thrust * .083;
        var x = Wrap(creature.Px + Math.Cos(angle) * speed, ArenaWidth);
        var y = Wrap(creature.Py + Math.Sin(angle) * speed, ArenaHeight);
        var blocked = RocksEnabled && Rocks.Any(r => Distance(x, y, r.X, r.Y) < r.Radius + BodyRadius &&
            Distance(x, y, r.X, r.Y) <= Distance(creature.Px, creature.Py, r.X, r.Y));
        if (blocked)
        { x = creature.Px; y = creature.Py; }
        creature.X.Tick.Set((long)Math.Round(x * 16));
        creature.Y.Tick.Set((long)Math.Round(y * 16));
        creature.Heading.Tick.Set((long)Math.Round(Wrap(angle, Math.Tau) / Math.Tau * 4096));
        var dx = Math.Abs(creature.Px - beforeX); dx = Math.Min(dx, ArenaWidth - dx);
        var dy = Math.Abs(creature.Py - beforeY); dy = Math.Min(dy, ArenaHeight - dy);
        var actualSpeed = Math.Sqrt(dx * dx + dy * dy) * TicksPerSecond;
        movement[creature] = (actualSpeed, blocked || TouchesRock(creature.Px, creature.Py));
        movementSamples++; if (actualSpeed < 1) stationarySamples++;
        if (actualSpeed >= SwimmingSpeed)
        {
            var lightDirection = LocalLightDirection(beforeX, beforeY);
            var toward = WrappedDelta(beforeX, creature.Px, ArenaWidth) * lightDirection.X +
                WrappedDelta(beforeY, creature.Py, ArenaHeight) * lightDirection.Y;
            if (creature.Energy.Value < Creature.HealthyLow)
            {
                hungrySamples++; if (toward > 0) hungryTowardSamples++;
                var gradient = LightGradient(beforeX, beforeY);
                var magnitude = Math.Sqrt(gradient.X * gradient.X + gradient.Y * gradient.Y);
                // Exclude physically flat patches: one sensor tick over the beam.
                if (magnitude * VisionDistance >= 1.0 / (EyeLevels - 1))
                {
                    var mx = WrappedDelta(beforeX, creature.Px, ArenaWidth); var my = WrappedDelta(beforeY, creature.Py, ArenaHeight);
                    gradientAlignment += (mx * gradient.X + my * gradient.Y) /
                        (Math.Sqrt(mx * mx + my * my) * magnitude);
                    gradientSamples++;
                }
            }
            if (creature.Energy.Value >= ParentEnergy) { fullSamples++; if (toward < 0) fullAwaySamples++; }
        }
    }

    private bool TouchesRock(double x, double y) => RocksEnabled &&
        Rocks.Any(r => Distance(x, y, r.X, r.Y) <= r.Radius + BodyRadius + MaximumStepDistance + 1.0 / 16);

    public double PredationChance(Creature creature)
    {
        if (!ExposureRisk) return 0;
        var sample = movement.TryGetValue(creature, out var observed) ? observed :
            (Speed: 0.0, RockContact: TouchesRock(creature.Px, creature.Py));
        var stillness = Math.Clamp(1 - sample.Speed / SwimmingSpeed, 0, 1);
        var exposure = AmbientIllumination(creature.Px, creature.Py);
        var risk = (.000008 + .00012 * exposure * exposure) *
            (1 + (StationaryRiskMultiplier - 1) * stillness);
        if (RocksEnabled && sample.RockContact) risk *= 1 + (RockRiskMultiplier - 1) * stillness;
        return risk;
    }

    private double Stillness(Creature creature) => Math.Clamp(1 - movement.GetValueOrDefault(creature).Speed / SwimmingSpeed, 0, 1);

    private void Photosynthesize(Creature creature)
    {
        // World photometry is external. It enters a native optical-request
        // accumulator calibrated to calories, never a free calorie carrier.
        var lightForGrowth = Math.Max(0, (Illumination(creature.Px, creature.Py) - PhotosyntheticFloor) / (1 - PhotosyntheticFloor));
        var flux = Aim(creature.BodyLight, (long)Math.Round(lightForGrowth * 48_000));
        var capture = ReadMany(creature.HarvestCapture)[0];
        if (!TryValue(capture, out var light)) { creature.LastIntake = capture; return; }
        creature.HarvestCarry.Add((ulong)Math.Max(0, light));
        var request = ReadMany(creature.HarvestRequest)[0];
        if (!TryValue(request, out var calories)) { creature.LastIntake = request; return; }
        var room = ReadMany(creature.EnergyRoom)[0];
        if (!TryValue(room, out var remaining)) { creature.LastIntake = G(capture, request, room); return; }
        var before = creature.Energy.Value;
        var demand = Math.Max(0, Math.Min(MaximumHarvestPerPulse, Math.Min(calories, remaining)));
        var pool = FeedingPool(creature.Px, creature.Py);
        var delivery = pool is null ? G(new Placeholder()) : Transfer(pool, creature.Energy, demand);
        // Whole optical attempts expire with this light pulse. Keep sub-calorie residue.
        var release = Read(Scale(request.Left, -1000, 1, creature.HarvestCarry));
        if (release.Left is CarrierLocation delta) Carrier.ReceiveTogether(G(delta));
        var received = creature.Energy.Value - before;
        if (LocalPhotoPools.Length != 0) LocalFoodShortfall.Add((ulong)(demand - (long)received));
        creature.Eaten.Add(received); IntakeCount.Add(received);
        creature.LastIntake = G(capture, request, room, delivery, release, flux);
    }

    private void RemoveDeaths()
    {
        var survivors = new List<Creature>(Creatures.Length);
        foreach (var creature in Creatures)
        {
            var eaten = ExposureRisk && Noise(Seed, Tick, creature.Slot) < PredationChance(creature);
            if (eaten && RecoverableHazards) { Injure(creature, 90); eaten = false; }
            var starved = creature.Energy.Value == 0;
            if (!eaten && !starved) { survivors.Add(creature); continue; }
            if (eaten) PredationCount.Add(1); else StarvationCount.Add(1);
            Bill(creature);
            var draining = Transfer(creature.Energy, DeathSink, (long)creature.Energy.Value);
            creature.LeftMuscle.Review(creature, Tick); creature.RightMuscle.Review(creature, Tick);
            var attention = Transfer(creature.Wallet, AttentionReserve, (long)creature.Wallet.Value);
            LastDeath = G(L(Clock, Tick), G(creature.X, creature.Y), draining, attention,
                creature.LeftMuscle.LastReview, creature.RightMuscle.LastReview, creature.Computation.Root);
            movement.Remove(creature);
            injuries.Remove(creature);
        }
        if (survivors.Count != Creatures.Length) Creatures = survivors.ToArray();
    }

    private void Bill(Creature creature)
    {
        var before = creature.Computation.Billed.Value;
        creature.Computation.Bill(creature.Energy, MetabolicSink);
        ComputeCostCount.Add(creature.Computation.Billed.Value - before);
    }

    private void Injure(Creature creature, int calories)
    {
        var before = creature.Energy.Value;
        var physicalTransfer = Transfer(creature.Energy, MetabolicSink, calories);
        var loss = before - creature.Energy.Value;
        Injuries++; InjuryCalories += (long)loss;
        injuries[creature] = (Tick + TicksPerSecond, (int)Math.Min(15, loss / 6 + 1));
        creature.LastWork = G(creature.LastWork, physicalTransfer);
    }

    private void AdmitBodyObservations(Creature creature, BodyObservations ports)
    {
        int Contact(double angle)
        {
            var x = Wrap(creature.Px + Math.Cos(angle) * (BodyRadius + 2), ArenaWidth);
            var y = Wrap(creature.Py + Math.Sin(angle) * (BodyRadius + 2), ArenaHeight);
            return RocksEnabled && Rocks.Any(r => ArenaDistance(x, y, r.X, r.Y) <= r.Radius + 2) ? 15 : 0;
        }
        var left = 0; var right = 0;
        var nearest = double.PositiveInfinity; var mateLeft = 0; var mateRight = 0; var mateAhead = 0;
        foreach (var other in Creatures)
        {
            if (ReferenceEquals(other, creature)) continue;
            var dx = WrappedDelta(creature.Px, other.Px, ArenaWidth); var dy = WrappedDelta(creature.Py, other.Py, ArenaHeight);
            var distance = Math.Sqrt(dx * dx + dy * dy);
            var side = -Math.Sin(creature.Angle) * dx + Math.Cos(creature.Angle) * dy;
            if (distance <= 48)
            {
                var strength = (int)Math.Round(15 * (1 - distance / 48));
                if (side < 0) left = Math.Max(left, strength); else right = Math.Max(right, strength);
            }
            // Sense an actual body-owned emission, never the other body's
            // private energy, age, utility, intended destination or fitness.
            if (!PilotFeatures.Courtship || other.Cognition?.Pilot.CourtshipSignal.Value != 15 ||
                distance > MateSensingDistance || distance >= nearest || !VisibleSegment(creature.Px, creature.Py, dx, dy)) continue;
            nearest = distance;
            var amount = Math.Max(1, (int)Math.Round(15 * (1 - distance / MateSensingDistance)));
            var sideways = distance > 0 ? side / distance : 0;
            var forward = distance > 0 ? (Math.Cos(creature.Angle) * dx + Math.Sin(creature.Angle) * dy) / distance : 1;
            mateLeft = (int)Math.Round(amount * (1 - sideways) / 2);
            mateRight = (int)Math.Round(amount * (1 + sideways) / 2);
            mateAhead = (int)Math.Round(15 * forward);
        }
        var injury = injuries.TryGetValue(creature, out var eventSample) && Tick <= eventSample.Until ? eventSample.Strength : 0;
        ports.Admit(creature.X.Ticks, creature.Y.Ticks, creature.Heading.Ticks, Tick,
            Contact(creature.Angle - Math.PI / 4), Contact(creature.Angle + Math.PI / 4), left, right, injury,
            mateLeft, mateRight, mateAhead);
    }

    private bool VisibleSegment(double x, double y, double dx, double dy)
    {
        if (!RocksEnabled) return true;
        var lengthSquared = dx * dx + dy * dy;
        return !Rocks.Any(r =>
        {
            var rx = WrappedDelta(x, r.X, ArenaWidth); var ry = WrappedDelta(y, r.Y, ArenaHeight);
            var fraction = lengthSquared > 0 ? Math.Clamp((rx * dx + ry * dy) / lengthSquared, 0, 1) : 0;
            var offsetX = rx - fraction * dx; var offsetY = ry - fraction * dy;
            return offsetX * offsetX + offsetY * offsetY <= r.Radius * r.Radius;
        });
    }

    public void Breed()
    {
        var newBodies = new List<Creature>();
        var used = new HashSet<Creature>();
        var records = new List<IElement>();
        foreach (var a in Creatures)
        {
            if (used.Contains(a) || Creatures.Length + newBodies.Count >= PopulationCeiling ||
                a.Age.Value < MaturityTicks || Tick < (long)a.NextMating.Value || a.Energy.Value < ParentEnergy ||
                Illumination(a.Px, a.Py) > BreedingDarkness) continue;
            if (!AtLeast(L(a.Energy, (long)a.Energy.Value), L(a.Energy, ParentEnergy), out var aFunded))
            { records.Add(aFunded); continue; }
            Creature? partner = null;
            CarrierLocation best = default;
            var candidates = Creatures.Where(b => b != a && !used.Contains(b) && b.Age.Value >= MaturityTicks &&
                Tick >= (long)b.NextMating.Value && b.Energy.Value >= ParentEnergy &&
                Illumination(b.Px, b.Py) <= BreedingDarkness && ArenaDistance(a.Px, a.Py, b.Px, b.Py) <= MatingDistance).Take(4);
            var partnerQuotes = new List<IElement>();
            foreach (var b in candidates)
            {
                if (!AtLeast(L(b.Energy, (long)b.Energy.Value), L(b.Energy, ParentEnergy), out var bFunded))
                { partnerQuotes.Add(bFunded); continue; }
                // Cognitive bodies publish their own witnessed native utility bids.
                // The world hosts contact; it supplies no fitness formula to these bodies.
                var score = b.Cognition is { } partnerBody ? ReadMany(partnerBody.Utility.Bid)[0] :
                    ReadMany(Scale(b.Eaten.ValueSlot, 1000, Math.Max(1, (long)b.Age.Value), new Carrier(Units.Return.UnitShape)))[0];
                partnerQuotes.Add(G(bFunded, score));
                if (!TryValue(score, out _)) continue;
                var bid = (CarrierLocation)score.Left;
                if (partner is not null)
                {
                    var better = AtLeast(bid, L(best.Carrier, best.Ticks + 1), out var comparison);
                    partnerQuotes.Add(comparison);
                    if (!better) continue;
                }
                partner = b; best = bid;
            }
            if (partner is null) { records.Add(G(aFunded, G(partnerQuotes.ToArray()), new Placeholder())); continue; }
            var matingStart = Resources.Begin();
            var child = MakeCreature(a, partner);
            var work = Resources.BodyWorkSince(matingStart);
            a.Computation.Runtime.Add((ulong)(work / 2));
            partner.Computation.Runtime.Add((ulong)(work - work / 2));
            if (child.LeftMuscle.Current.Components.Length == 0 || child.RightMuscle.Current.Components.Length == 0)
            {
                Transfer(child.Energy, CalorieReserve, (long)child.Energy.Value);
                Transfer(child.Wallet, AttentionReserve, (long)child.Wallet.Value);
                records.Add(G(child.Inheritance, new Placeholder())); continue;
            }
            used.Add(a); used.Add(partner); newBodies.Add(child); BirthCount.Add(1);
            Aim(a.NextMating, Tick + MatingCooldownTicks); Aim(partner.NextMating, Tick + MatingCooldownTicks);
            records.Add(G(L(Clock, Tick), aFunded, G(partnerQuotes.ToArray()),
                G(G(L(a.X.Carrier, a.X.Ticks), L(a.Y.Carrier, a.Y.Ticks)),
                    G(L(partner.X.Carrier, partner.X.Ticks), L(partner.Y.Carrier, partner.Y.Ticks)),
                    L(Units.Position, (long)Math.Round(ArenaDistance(a.Px, a.Py, partner.Px, partner.Py) * 16)),
                    L(Units.Position, (long)(MatingDistance * 16))),
                child.Inheritance, child.LastIntake, child.Computation.Root));
        }
        if (newBodies.Count != 0) Creatures = [.. Creatures, .. newBodies];
        LastBreeding = G(L(Clock, Tick), G(records.ToArray()), L(BirthCount, (long)BirthCount.Value));
    }

    private void RecordTrend()
    {
        Trends.Add(new Trend(Seconds, TotalFood - foodAtLastTrend, MeanEnergy, MeanExposure, PredationDeaths + StarvationDeaths));
        foodAtLastTrend = TotalFood;
        if (Trends.Count > 180) Trends.RemoveAt(0);
    }

    public void CheckConservation()
    {
        if (TotalCalories != CalorieBudget || TotalAttention != AttentionBudget)
            throw new InvalidOperationException($"Bath adapter conservation failure: calories={TotalCalories}, attention={TotalAttention}");
    }
    public static double AngleDifference(double a, double b) => Math.Atan2(Math.Sin(a - b), Math.Cos(a - b));
    public static double Distance(double x, double y, double a, double b) => Math.Sqrt((x - a) * (x - a) + (y - b) * (y - b));
    public static double WrappedDelta(double a, double b, double span)
    {
        var delta = b - a;
        return delta > span / 2 ? delta - span : delta < -span / 2 ? delta + span : delta;
    }
    public static double WrappedDistance(double x, double y, double a, double b)
    {
        var dx = WrappedDelta(x, a, Width); var dy = WrappedDelta(y, b, Height);
        return Math.Sqrt(dx * dx + dy * dy);
    }
    public double ArenaDistance(double x, double y, double a, double b)
    {
        var dx = WrappedDelta(x, a, ArenaWidth); var dy = WrappedDelta(y, b, ArenaHeight);
        return Math.Sqrt(dx * dx + dy * dy);
    }
    public static double Wrap(double value, double span) => (value % span + span) % span;
    private static double Noise(int seed, long tick, int slot)
    {
        var bits = unchecked((ulong)tick * 0x9e3779b97f4a7c15UL + (ulong)(slot + 1) * 0xbf58476d1ce4e5b9UL + (ulong)(uint)seed);
        bits = (bits ^ (bits >> 30)) * 0xbf58476d1ce4e5b9UL;
        bits = (bits ^ (bits >> 27)) * 0x94d049bb133111ebUL;
        bits ^= bits >> 31;
        return (bits >> 11) / (double)(1UL << 53);
    }
}

public sealed class PhaseCosts
{
    private readonly Dictionary<string, long> elapsed = [];
    public void Add(string phase, ref long stamp)
    {
        var now = Stopwatch.GetTimestamp();
        elapsed[phase] = elapsed.GetValueOrDefault(phase) + now - stamp; stamp = now;
    }
    public Dictionary<string, double> Seconds => elapsed.ToDictionary(p => p.Key, p => p.Value / (double)Stopwatch.Frequency);
}
