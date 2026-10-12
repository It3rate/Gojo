using Gojo.Core;
using Gojo.Structures;
using static Phototaxis.Native;

namespace Phototaxis;

public sealed class Eye
{
    public const int RayCount = 1;
    public const double OffsetDegrees = 35;
    public Carrier[] Pixels { get; }
    public CarrierLocation Maximum { get; }
    public int Levels => checked((int)Maximum.Ticks + 1);
    public Carrier Presence { get; }
    public Carrier SampleTime { get; }
    public Carrier NextTime { get; }
    public Carrier Interval { get; }
    public int Period => (int)Interval.Value;
    public double Offset { get; }
    public Relation Previous { get; private set; }
    public Relation Mean { get; }
    public Relation Signal { get; }
    public Relation LastAdmission { get; private set; } = G(new Placeholder());
    public Relation Root => G(G(Pixels), Presence, SampleTime, NextTime, Interval, Previous, LastAdmission,
        G(Maximum, Signal, Mean));

    public Eye(Units units, double offset, int period, int phase, int levels = 16)
    {
        if (levels is < 2 or > 65536) throw new ArgumentOutOfRangeException(nameof(levels));
        Offset = offset;
        Pixels = [Limited(units.Light, levels - 1)];
        Maximum = L(Pixels[0], levels - 1);
        Presence = new Carrier(units.Count.UnitShape);
        SampleTime = new Carrier(units.Clock.UnitShape);
        NextTime = new Carrier(units.Clock.UnitShape);
        Interval = new Carrier(units.Clock.UnitShape);
        Aim(Interval, period); Aim(NextTime, phase);
        Previous = G(L(Pixels[0], 0));
        Signal = Scale(Present(Pixels[0], Presence), 1, 1, new Carrier(units.Feature.UnitShape));
        // Legacy circuits retain their 0..15 port. Fine steering reads Signal
        // and calibrates after differencing and gain, so detail survives.
        Mean = levels == 16 ? Signal : Scale(Signal, 15, Maximum.Ticks, new Carrier(units.Feature.UnitShape));
    }

    public void Admit(ReadOnlySpan<int> samples, long time)
    {
        if (samples.IsEmpty) { LastAdmission = G(new Placeholder()); return; }
        Previous = G(L(Pixels[0], (long)Pixels[0].Value));
        LastAdmission = Carrier.ReceiveTogether(G(
            L(Pixels[0], samples[0] - Held(Pixels[0])), L(Presence, 1 - Held(Presence)),
            L(SampleTime, time - Held(SampleTime)), L(NextTime, time + Period - Held(NextTime))));
    }
}

public sealed class Muscle
{
    public Carrier Force { get; }
    public Carrier NextTime { get; }
    public Carrier Interval { get; }
    public Carrier Cursor { get; }
    public Carrier FailedReads { get; }
    public Carrier ReviewCount { get; }
    public Carrier SearchCount { get; }
    public Carrier RewriteCount { get; }
    public Circuit Current { get; private set; }
    public Relation Pending { get; private set; } = G(new Placeholder());
    public Relation LastRead { get; private set; } = G(new Placeholder());
    public Relation LastReview { get; private set; } = G(new Placeholder());
    public Relation LastAuction { get; private set; } = G(new Placeholder());
    public Relation LastRewrite { get; private set; } = G(new Placeholder());
    public Relation Root => G(Force, NextTime, Interval, Current.Root,
        G(remembered.Select(c => (IElement)c.Root).ToArray()), Cursor, Pending, LastRead,
        LastReview, LastAuction, LastRewrite, FailedReads, ReviewCount, SearchCount, RewriteCount, G(sources));
    public int Period => (int)Interval.Value;
    public long Reviews => (long)ReviewCount.Value;
    public long Unresolved => (long)FailedReads.Value;
    private readonly Units units;
    private readonly IElement[] sources;
    private readonly List<Circuit> remembered = [];

    public Muscle(Units units, int period, int phase, IElement[] sources)
    {
        this.units = units; this.sources = sources;
        Force = Limited(units.Force, 15);
        NextTime = new Carrier(units.Clock.UnitShape);
        Interval = new Carrier(units.Clock.UnitShape);
        Cursor = new Carrier(units.Count.UnitShape);
        FailedReads = new Carrier(units.Count.UnitShape);
        ReviewCount = new Carrier(units.Count.UnitShape);
        SearchCount = new Carrier(units.Count.UnitShape);
        RewriteCount = new Carrier(units.Count.UnitShape);
        Aim(Interval, period); Aim(NextTime, phase);
        // Same neutral constant-drive seed for everyone; no steering rule.
        Current = new Circuit(units, new Component(units, sources[0], 4));
    }

    public void InstallInherited(Circuit circuit) => Current = circuit;

    public void Wake(Creature creature, long time)
    {
        if (Parts(Pending).Length != 6) BeginTrial(creature, time);
        LastRead = ReadMany(Current.Output)[0];
        if (TryValue(LastRead, out var force)) Aim(Force, force);
        else FailedReads.Add(1);
        Aim(NextTime, time + Period);
    }

    private void BeginTrial(Creature creature, long time)
    {
        var comfort = creature.ReadComfort();
        Pending = G(L(units.Clock, time), L(creature.Energy, (long)creature.Energy.Value),
            comfort, L(creature.Computation.Runtime, (long)creature.Computation.Runtime.Value), Current.Root,
            L(FailedReads, (long)FailedReads.Value));
    }

    public void Review(Creature creature, long time)
    {
        var prior = Parts(Pending);
        if (prior.Length != 6 || prior[0] is not CarrierLocation start || time <= start.Ticks) return;
        var after = creature.ReadComfort();
        var beforeRead = (Relation)Parts((Relation)prior[2])[2];
        var afterRead = (Relation)Parts(after)[2];
        // Shared-body comfort relief; no claim that a term independently caused it.
        var failures = Sum((FailedReads.ValueSlot, 1), (prior[5], -1));
        var relief = Read(SumTo(new Carrier(units.Return.UnitShape),
            (Scale(Sum((beforeRead.Left, 1), (afterRead.Left, -1)), 1, 2, new Carrier(units.Return.UnitShape)), 1),
            (Scale(failures, 12, 1, new Carrier(units.Return.UnitShape)), -1)));
        var work = Read(Sum((creature.Computation.Runtime.ValueSlot, 1), (prior[3], -1)));
        var sharedWork = Read(Scale(work.Left, 1, 2, new Carrier(units.Work.UnitShape)));
        var recorded = Current.Record(units, relief, time - start.Ticks,
            TryValue(sharedWork, out var ticks) ? ticks : 0);
        ReviewCount.Add(1);
        LastReview = TimeStepSequence.Step(new Carrier(), units.Clock, start.Ticks, time,
            G(prior[1], prior[2], prior[3], prior[5]), G(Current.Root, recorded, failures),
            G(L(creature.Energy, (long)creature.Energy.Value), after, relief), G(relief.Right, work.Right, sharedWork.Right));
        Pending = G(new Placeholder());
    }

    public void Search(Creature creature, long time)
    {
        var previous = Current;
        var proposal = CircuitMarket.Propose(units, previous, sources, Cursor, out var correspondence);
        var candidates = new[] { previous, proposal }.Concat(remembered).Distinct().Take(4).ToArray();
        Current = CircuitMarket.Select(units, candidates, creature.Wallet, creature.SpentAttention, time, out var auction) ?? previous;
        LastAuction = auction; SearchCount.Add(1);
        if (!ReferenceEquals(Current, previous))
        {
            RewriteCount.Add(1);
            remembered.Remove(Current);
            if (!remembered.Contains(previous)) remembered.Add(previous);
            while (remembered.Count > 2) remembered.RemoveAt(0);
            LastRewrite = TimeStepSequence.Step(new Carrier(), units.Clock, time, time,
                previous.Root, G(correspondence, auction), Current.Root, new Relation(previous.Root, Current.Root));
        }
        BeginTrial(creature, time);
    }

    public void ContinueTrial(Creature creature, long time) => BeginTrial(creature, time);
}

public sealed class Creature
{
    public const int Capacity = 1200;
    public const int HealthyLow = 540;
    public const int HealthyHigh = 900;
    public const int PressureThreshold = 60;
    public const int EvaluationTicks = 160;
    public int Slot { get; }
    public long BornAt { get; }
    public CarrierLocation X { get; }
    public CarrierLocation Y { get; }
    public CarrierLocation Heading { get; }
    public Eye LeftEye { get; }
    public Eye RightEye { get; }
    public Muscle LeftMuscle { get; }
    public Muscle RightMuscle { get; }
    public Carrier Energy { get; }
    public Carrier Eaten { get; }
    public Carrier Age { get; }
    public Carrier Generation { get; }
    public Carrier NextMating { get; }
    public Carrier Wallet { get; }
    public Carrier SpentAttention { get; }
    public Carrier Pressure { get; }
    public Carrier InspectionPeriod { get; }
    public Carrier NextInspection { get; }
    public Carrier InspectionCount { get; }
    public Carrier HarvestCarry { get; }
    public Carrier BodyLight { get; }
    public Relation HarvestCapture { get; }
    public Relation HarvestRequest { get; }
    public Relation EnergyRoom { get; }
    public Computation Computation { get; }
    public Relation Deficit { get; }
    public Relation Features { get; }
    public IElement[] Sources { get; }
    public Relation Inheritance { get; private set; } = G();
    private Relation[] controllerCells = [];
    public CognitiveBody? Cognition { get; private set; }
    public void ConfigureCognition(PilotModule[]? modules = null, bool asksJev = false, Relation? ancestry = null,
        bool binocular = true, PilotFeatures? features = null) => Cognition = new(units, this, modules, asksJev, ancestry, binocular, features);
    public Relation ControllerState => G(controllerCells);
    public void ConfigureFeedback(params Relation[] cells) => controllerCells = cells;
    public void AdvanceController(long time)
    {
        for (var i = 0; i < controllerCells.Length; i++)
            controllerCells[i] = ControllerFeedback.Advance(controllerCells[i], units, time);
    }
    public Relation LastInspection { get; private set; } = G(new Placeholder());
    public Relation LastMotion { get; private set; } = G(new Placeholder());
    public Relation LastIntake { get; set; } = G(new Placeholder());
    public Relation LastWork { get; set; } = G(new Placeholder());
    public Relation LastCredit { get; set; } = G(new Placeholder());
    public Placeholder AdviceProposal { get; } = new();
    public Relation Advice => Cognition?.Inquiry.Root ?? G(G(Features, LastInspection, LeftMuscle.LastAuction,
        RightMuscle.LastAuction, LeftMuscle.LastReview, RightMuscle.LastReview), AdviceProposal);
    public Relation PositionFrame { get; }
    public Relation Root => G(G(X, Y, Heading, PositionFrame), G(LeftEye.Root, RightEye.Root),
        G(LeftMuscle.Root, RightMuscle.Root, LastMotion, ControllerState, Cognition?.Root ?? G()),
        G(Energy, L(Energy, Capacity), Eaten, LastIntake, LastWork, Deficit, Computation.Root, HarvestCarry,
            G(BodyLight, HarvestCapture, HarvestRequest, EnergyRoom)),
        Features, G(Wallet, SpentAttention, LastCredit), G(Age, Generation, Inheritance, NextMating),
        Advice, G(Pressure, InspectionPeriod, NextInspection, InspectionCount, LastInspection));
    public int ComponentCount => LeftMuscle.Current.Components.Length + RightMuscle.Current.Components.Length;
    public long Searches => (long)(LeftMuscle.SearchCount.Value + RightMuscle.SearchCount.Value);
    public long Rewrites => (long)(LeftMuscle.RewriteCount.Value + RightMuscle.RewriteCount.Value);
    public double Px => X.Ticks / 16.0;
    public double Py => Y.Ticks / 16.0;
    public double Angle => Heading.Ticks * Math.Tau / 4096;
    public double Fullness => Energy.Value / (double)Capacity;
    private readonly Units units;
    private readonly Relation forwardEquation;
    private readonly Relation turnEquation;
    private readonly Carrier forwardOutput;
    private readonly Carrier turnOutput;
    private long motionLeft = -1, motionRight = -1, cachedThrust, cachedYaw;

    public Creature(Units units, int slot, long time, double x, double y, double heading, Random birth, Carrier spent, Carrier resourceYield,
        long computationRate = Computation.CaloriesPerRuntimeSecond, int eyeLevels = 16,
        double arenaWidth = Bath.Width, double arenaHeight = Bath.Height)
    {
        this.units = units; Slot = slot; BornAt = time;
        X = L(new Carrier(units.Position.UnitShape), (long)(x * 16));
        Y = L(new Carrier(units.Position.UnitShape), (long)(y * 16));
        Heading = L(new Carrier(units.Heading.UnitShape), (long)(heading / Math.Tau * 4096));
        PositionFrame = G(Relation.Segment(X.Carrier, 0, (long)(arenaWidth * 16)),
            Relation.Segment(Y.Carrier, 0, (long)(arenaHeight * 16)));
        Energy = Limited(units.Calorie, Capacity);
        Eaten = new Carrier(units.Calorie.UnitShape);
        Age = new Carrier(units.Clock.UnitShape);
        Generation = new Carrier(units.Count.UnitShape);
        NextMating = new Carrier(units.Clock.UnitShape);
        Wallet = Limited(units.Attention, 30); SpentAttention = spent;
        // An occupied one-tick stop leaves each extra demand tick as actual
        // unresolved pressure. A zero-span site has no stop basis in the kernel.
        Pressure = Limited(units.Calorie, 1); Aim(Pressure, 1);
        InspectionPeriod = new Carrier(units.Clock.UnitShape);
        NextInspection = new Carrier(units.Clock.UnitShape);
        InspectionCount = new Carrier(units.Count.UnitShape);
        HarvestCarry = new Carrier(units.Light.UnitShape);
        BodyLight = new Carrier(units.Light.UnitShape);
        HarvestCapture = Scale(Product(BodyLight.ValueSlot, resourceYield.ValueSlot), 1, 1000, new Carrier(units.Light.UnitShape));
        HarvestRequest = Scale(HarvestCarry.ValueSlot, 1, 1000, new Carrier(units.Calorie.UnitShape));
        EnergyRoom = SumTo(new Carrier(units.Calorie.UnitShape), (L(Energy, Capacity), 1), (Energy.ValueSlot, -1));
        Aim(InspectionPeriod, EvaluationTicks); Aim(NextInspection, time + EvaluationTicks);
        Computation = new Computation(units, computationRate);
        LeftEye = new Eye(units, -Eye.OffsetDegrees, 3, (int)time + birth.Next(1, 4), eyeLevels);
        RightEye = new Eye(units, Eye.OffsetDegrees, 5, (int)time + birth.Next(1, 6), eyeLevels);
        Deficit = Scale(Sum((L(Energy, Capacity), 1), (Energy.ValueSlot, -1)), 15, Capacity, new Carrier(units.Feature.UnitShape));
        var fullness = Scale(Energy.ValueSlot, 15, Capacity, new Carrier(units.Feature.UnitShape));
        Sources = [L(units.Feature, 15), LeftEye.Mean, RightEye.Mean, Deficit, fullness];
        Features = G(Sources);
        LeftMuscle = new Muscle(units, 11, (int)time + birth.Next(5, 13), Sources);
        // Homologous local port order on the mirrored side. Coefficients may
        // take either sign; this wiring supplies no light-seeking preference.
        IElement[] mirrored = [Sources[0], Sources[2], Sources[1], Sources[3], Sources[4]];
        RightMuscle = new Muscle(units, 13, (int)time + birth.Next(6, 15), mirrored);
        var forward = Parts(Sum((LeftMuscle.Force.ValueSlot, 1), (RightMuscle.Force.ValueSlot, 1)));
        var turn = Parts(Sum((LeftMuscle.Force.ValueSlot, 1), (RightMuscle.Force.ValueSlot, -1)));
        forwardEquation = (Relation)forward[0]; forwardOutput = (Carrier)forward[2];
        turnEquation = (Relation)turn[0]; turnOutput = (Carrier)turn[2];
    }

    public Relation ReadComfort()
    {
        var energy = L(Energy, (long)Energy.Value);
        var lower = OperationClaim.AtLeast(new Carrier(), energy, L(Energy, HealthyLow), new Carrier(Energy.UnitShape));
        var upper = OperationClaim.AtMost(new Carrier(), energy, L(Energy, HealthyHigh), new Carrier(Energy.UnitShape));
        if (!OperationClaim.TryReadResolvedViolation(lower, out var low) || !OperationClaim.TryReadResolvedViolation(upper, out var high))
            return G(lower, upper, Relation.UnresolvedRead(energy, lower, Pressure));
        return G(lower, upper, Read(SumTo(new Carrier(Energy.UnitShape), (low, 1), (high, 1))));
    }

    public void Inspect(long time, bool learning)
    {
        if (time < (long)NextInspection.Value) return;
        var comfort = ReadComfort();
        var read = (Relation)Parts(comfort)[2];
        var before = G(L(Pressure, Held(Pressure)), L(Energy, (long)Energy.Value));
        var request = Read(SumTo(new Carrier(Pressure.UnitShape), (read.Left, 1), (L(Pressure, 1), 1)));
        var admitted = TryValue(request, out var excess) ? Aim(Pressure, excess) : request;
        LeftMuscle.Review(this, time); RightMuscle.Review(this, time);
        var pressure = ReadMany(Pressure.LiveTensionSlot)[0];
        Relation trigger = G(new Placeholder());
        var excessive = TryValue(pressure, out _) && pressure.Left is CarrierLocation amount &&
            AtLeast(amount, L(amount.Carrier, PressureThreshold), out trigger);
        if (learning && excessive)
        {
            // Coordinate trials perturb one actuator circuit, leaving the other
            // operating circuit in place for a clearer shared-body outcome.
            if (InspectionCount.Value % 2 == 0) { LeftMuscle.Search(this, time); RightMuscle.ContinueTrial(this, time); }
            else { RightMuscle.Search(this, time); LeftMuscle.ContinueTrial(this, time); }
        }
        else
        { LeftMuscle.ContinueTrial(this, time); RightMuscle.ContinueTrial(this, time); }
        InspectionCount.Add(1);
        LastInspection = TimeStepSequence.Step(new Carrier(), units.Clock,
            Math.Max(BornAt, time - (long)InspectionPeriod.Value), time, before,
            G(comfort, request, admitted, pressure, trigger, LeftMuscle.LastAuction, RightMuscle.LastAuction),
            G(L(Pressure, Held(Pressure)), LeftMuscle.Current.Root, RightMuscle.Current.Root), read.Right);
        Aim(NextInspection, time + (long)InspectionPeriod.Value);
    }

    public void Inherit(Creature a, Creature b, long time)
    {
        var left = CircuitMarket.Mate(units, a.LeftMuscle, b.LeftMuscle, a.Sources, b.Sources, Sources,
            Wallet, SpentAttention, time, out var leftAuction, out var leftLinks);
        var right = CircuitMarket.Mate(units, a.RightMuscle, b.RightMuscle, a.Sources, b.Sources, Sources,
            Wallet, SpentAttention, time, out var rightAuction, out var rightLinks);
        LeftMuscle.InstallInherited(left); RightMuscle.InstallInherited(right);
        Aim(Generation, Math.Max((long)a.Generation.Value, (long)b.Generation.Value) + 1);
        Inheritance = G(G(L(a.Generation, (long)a.Generation.Value), L(b.Generation, (long)b.Generation.Value)),
            leftAuction, rightAuction, leftLinks, rightLinks);
    }

    public void RecordDesignedInheritance(Creature a, Creature b)
    {
        Aim(Generation, Math.Max((long)a.Generation.Value, (long)b.Generation.Value) + 1);
        // Authored trial colonies rebuild the same fixed recipe at child-local ports.
        Inheritance = G(G(L(a.Generation, (long)a.Generation.Value), L(b.Generation, (long)b.Generation.Value)),
            G(new Relation(a.Features, Features), new Relation(b.Features, Features)),
            G(new Relation(a.LeftMuscle.Current.Root, LeftMuscle.Current.Root),
                new Relation(a.RightMuscle.Current.Root, RightMuscle.Current.Root)), ControllerState);
    }

    public bool ReadMotion(out long thrust, out long yaw)
    {
        var left = (long)LeftMuscle.Force.Value; var right = (long)RightMuscle.Force.Value;
        if (left == motionLeft && right == motionRight) { thrust = cachedThrust; yaw = cachedYaw; return true; }
        var frame = new Relation(L(LeftMuscle.Force, left), L(RightMuscle.Force, right));
        if (!forwardEquation.TryReadOperationFrom(frame, forwardOutput, out var f))
            f = Relation.UnresolvedRead(frame, forwardEquation, forwardOutput);
        if (!turnEquation.TryReadOperationFrom(frame, turnOutput, out var t))
            t = Relation.UnresolvedRead(frame, turnEquation, turnOutput);
        LastMotion = G(frame, f, t);
        var complete = TryValue(f, out thrust);
        if (!TryValue(t, out yaw) || !complete) return false;
        motionLeft = left; motionRight = right; cachedThrust = thrust; cachedYaw = yaw;
        return true;
    }

    public byte[] ExportNativeGraph() => GojoNumericFormat.Encode(Root);
}
