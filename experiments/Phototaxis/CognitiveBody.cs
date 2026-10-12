using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using static Phototaxis.Native;

namespace Phototaxis;

/// <summary>Equal physical observation ports. There is deliberately no velocity or reward port.</summary>
public sealed class BodyObservations
{
    public Carrier X { get; }
    public Carrier Y { get; }
    public Carrier Heading { get; }
    public Carrier Time { get; }
    public Carrier LeftContact { get; }
    public Carrier RightContact { get; }
    public Carrier LeftProximity { get; }
    public Carrier RightProximity { get; }
    public Carrier Injury { get; }
    public Carrier Presence { get; }
    public Carrier LeftMate { get; }
    public Carrier RightMate { get; }
    public Carrier MateAhead { get; }
    public Relation LastAdmission { get; private set; } = G(new Placeholder());
    public Relation Root => G(X, Y, Heading, Time, LeftContact, RightContact, LeftProximity,
        RightProximity, Injury, Presence, LastAdmission, G(LeftMate, RightMate, MateAhead));

    public BodyObservations(Units u)
    {
        X = new(u.Position.UnitShape); Y = new(u.Position.UnitShape);
        Heading = new(u.Heading.UnitShape); Time = new(u.Clock.UnitShape);
        LeftContact = Limited(u.Feature, 15); RightContact = Limited(u.Feature, 15);
        LeftProximity = Limited(u.Feature, 15); RightProximity = Limited(u.Feature, 15);
        Injury = Limited(u.Feature, 15); Presence = new(u.Count.UnitShape);
        LeftMate = Limited(u.Feature, 15); RightMate = Limited(u.Feature, 15); MateAhead = new(u.Feature.UnitShape);
    }

    public void Admit(long x, long y, long heading, long time, int leftContact, int rightContact,
        int leftProximity, int rightProximity, int injury, int leftMate = 0, int rightMate = 0, int mateAhead = 0)
    {
        LastAdmission = Carrier.ReceiveTogether(G(L(X, x - Held(X)), L(Y, y - Held(Y)),
            L(Heading, heading - Held(Heading)), L(Time, time - Held(Time)),
            L(LeftContact, leftContact - Held(LeftContact)), L(RightContact, rightContact - Held(RightContact)),
            L(LeftProximity, leftProximity - Held(LeftProximity)), L(RightProximity, rightProximity - Held(RightProximity)),
            L(Injury, injury - Held(Injury)), L(Presence, 1 - Held(Presence)),
            L(LeftMate, leftMate - Held(LeftMate)), L(RightMate, rightMate - Held(RightMate)),
            L(MateAhead, mateAhead - Held(MateAhead))));
    }
}

/// <summary>Native motion inference, frozen forecasts, residual pressure and observed utility.</summary>
public sealed class CognitiveBody
{
    public BodyObservations Observations { get; }
    public Carrier PreviousX { get; }
    public Carrier PreviousY { get; }
    public Carrier PreviousTime { get; }
    public Carrier PreviousEnergy { get; }
    public Carrier PreviousIntake { get; }
    public Carrier Ready { get; }
    public Carrier SpeedSquared { get; }
    public Carrier Forecast { get; }
    public Carrier PersistenceForecast { get; }
    public Carrier ForecastTime { get; }
    public Carrier Pressure { get; }
    public Carrier Error { get; }
    public Carrier Blocked { get; }
    public Carrier PainMemory { get; }
    public Carrier Samples { get; }
    public Carrier PredictionChecks { get; }
    public Carrier ErrorTotal { get; }
    public Relation MotionRecipe { get; }
    public Relation ForecastRecipe { get; }
    public Relation PredictionErrorRecipe { get; }
    public Relation OutcomeRecipe { get; }
    public Relation LastMotion { get; private set; } = G(new Placeholder());
    public Relation LastPrediction { get; private set; } = G(new Placeholder());
    public Relation LastOutcome { get; private set; } = G(new Placeholder());
    public Relation LastMemory { get; private set; } = G(new Placeholder());
    public Relation LastIssue { get; private set; } = G(new Placeholder());
    public Relation AgeRecipe { get; }
    public Utility Utility { get; }
    public CognitivePilot Pilot { get; }
    public NativeInquiry Inquiry { get; }
    private readonly Units units;
    private readonly Creature creature;
    private readonly Relation blockedRecipe, painRecipe;
    private readonly Relation[] memories;
    private readonly Relation persistenceRecipe, issueClockRecipe;
    private readonly List<Relation> recent = [];
    public Relation Root => G(Observations.Root,
        G(PreviousX, PreviousY, PreviousTime, PreviousEnergy, PreviousIntake, Ready, G(memories), LastMemory, AgeRecipe),
        G(MotionRecipe, SpeedSquared, LastMotion),
        G(ForecastRecipe, Forecast, PersistenceForecast, ForecastTime, LastPrediction,
            Pressure, Error, Blocked, PainMemory, blockedRecipe, painRecipe, PredictionChecks, ErrorTotal, LastIssue,
            PredictionErrorRecipe),
        G(OutcomeRecipe, Utility.Root, LastOutcome), Pilot.Root, Inquiry.Root,
        G(Samples, G(recent.ToArray())));

    public CognitiveBody(Units u, Creature c, PilotModule[]? inherited = null, bool asksJev = false,
        Relation? ancestry = null, bool binocular = true, PilotFeatures? features = null)
    {
        units = u; creature = c; Observations = new(u);
        PreviousX = new(u.Position.UnitShape); PreviousY = new(u.Position.UnitShape);
        PreviousTime = new(u.Clock.UnitShape); PreviousEnergy = new(u.Calorie.UnitShape);
        PreviousIntake = new(u.Calorie.UnitShape); Ready = new(u.Count.UnitShape);
        SpeedSquared = new(); Forecast = new(SpeedSquared.UnitShape);
        PersistenceForecast = new(SpeedSquared.UnitShape); ForecastTime = new(u.Clock.UnitShape);
        Pressure = Limited(u.Feature, 1); Aim(Pressure, 1);
        Error = new(SpeedSquared.UnitShape); Blocked = Limited(u.Feature, 15);
        PainMemory = Limited(u.Feature, 15); Samples = new(u.Count.UnitShape);
        PredictionChecks = new(u.Count.UnitShape); ErrorTotal = new(SpeedSquared.UnitShape);
        Utility = new(u);
        AgeRecipe = SumTo(new Carrier(u.Clock.UnitShape), (Observations.Time.ValueSlot, 1), (L(u.Clock, c.BornAt), -1));
        var frame = Parts(c.PositionFrame);
        var dx = Wrapped(u, ExpressionGraph.Subtract(u, Observations.X.ValueSlot, PreviousX.ValueSlot), (Relation)frame[0]);
        var dy = Wrapped(u, ExpressionGraph.Subtract(u, Observations.Y.ValueSlot, PreviousY.ValueSlot), (Relation)frame[1]);
        var distanceSquared = Sum((Product(dx, dx), 1), (Product(dy, dy), 1));
        var elapsed = Sum((Observations.Time.ValueSlot, 1), (PreviousTime.ValueSlot, -1));
        // 16 position ticks / pixel, 20 time ticks / second. Calibration remains in the graph.
        MotionRecipe = Divide(Scale(distanceSquared, 400, 1), Scale(Product(elapsed, elapsed), 256, 1),
            new Carrier(SpeedSquared.UnitShape));
        var command = Sum((c.LeftMuscle.Force.ValueSlot, 1), (c.RightMuscle.Force.ValueSlot, 1));
        // An explicit initial hypothesis of .083 pixels per thrust tick, not a world velocity sensor.
        ForecastRecipe = Scale(Product(command, command), 27_556, 10_000, new Carrier(SpeedSquared.UnitShape));
        // Reuse a connected signed difference and absolute-value read. Claims
        // still freeze each forecast/outcome and gate the later pressure import.
        var differenceAxis = new Carrier(Forecast.UnitShape);
        var difference = SumTo(differenceAxis, (Signed(SpeedSquared), 1), (Signed(Forecast), -1));
        var negativeAxis = new Carrier(u.Feature.UnitShape);
        var negative = Scale(difference, -1, 1, negativeAxis);
        var errorAxis = new Carrier(u.Feature.UnitShape);
        // The former read selected on a nonnegative claim magnitude < extent.
        // Exclude both signed endpoints so negative extent does not gain a read.
        var extent = ExpressionGraph.SelectionExtent;
        PredictionErrorRecipe = ScopedChoice.Create(difference,
            Relation.Segment(differenceAxis, 1 - extent, extent), errorAxis,
            ScopedChoice.Offer(Relation.Segment(differenceAxis, 1 - extent, 0), negative,
                Equation.Affine(negativeAxis, 0, 1, errorAxis, 0, 1)),
            ScopedChoice.Offer(Relation.Segment(differenceAxis, 0, extent), difference,
                Equation.Affine(differenceAxis, 0, 1, errorAxis, 0, 1)));
        persistenceRecipe = Scale(SpeedSquared.ValueSlot, 1, 1, PersistenceForecast);
        issueClockRecipe = Scale(Observations.Time.ValueSlot, 1, 1, ForecastTime);
        memories = [Scale(Observations.X.ValueSlot, 1, 1, PreviousX), Scale(Observations.Y.ValueSlot, 1, 1, PreviousY),
            Scale(Observations.Time.ValueSlot, 1, 1, PreviousTime), Scale(c.Energy.ValueSlot, 1, 1, PreviousEnergy),
            Scale(c.Eaten.ValueSlot, 1, 1, PreviousIntake)];
        blockedRecipe = ExpressionGraph.Choose(u,
            ExpressionGraph.And(u, ExpressionGraph.Less(u, SpeedSquared.ValueSlot, L(u.Feature, 64)),
                ExpressionGraph.Greater(u, Error.ValueSlot, L(u.Feature, 80))),
            SumTo(new Carrier(u.Feature.UnitShape), (Blocked.ValueSlot, 1), (L(u.Feature, 1), 1)), L(u.Feature, 0));
        painRecipe = ExpressionGraph.Choose(u, ExpressionGraph.Greater(u, Observations.Injury.ValueSlot, L(u.Feature, 0)),
            L(u.Feature, 15), ExpressionGraph.Choose(u, ExpressionGraph.Greater(u, PainMemory.ValueSlot, L(u.Feature, 0)),
                SumTo(new Carrier(u.Feature.UnitShape), (PainMemory.ValueSlot, 1), (L(u.Feature, -1), 1)), L(u.Feature, 0)));
        // Internal valuation of its own material changes. No environment score is admitted here.
        OutcomeRecipe = SumTo(new Carrier(u.Return.UnitShape),
            (Sum((c.Energy.ValueSlot, 1), (PreviousEnergy.ValueSlot, -1)), 1),
            (Scale(Sum((c.Eaten.ValueSlot, 1), (PreviousIntake.ValueSlot, -1)), 1, 4, new Carrier(u.Return.UnitShape)), 1),
            (Scale(Observations.Injury.ValueSlot, 2, 1, new Carrier(u.Return.UnitShape)), -1));
        Pilot = new(u, c, this, inherited ?? CognitivePilot.Founder(u, c.Slot), ancestry, binocular, features);
        Inquiry = new(u, c, this, asksJev);
    }

    internal static IElement Wrapped(Units u, IElement delta, Relation frame)
    {
        if (!ScopedChoice.TrySpan(frame, out _, out var start, out var end)) return new Placeholder();
        var span = end - start;
        return ExpressionGraph.Choose(u,
            ExpressionGraph.Greater(u, delta, L(u.Feature, span / 2)), Sum((delta, 1), (L(u.Feature, -span), 1)),
            ExpressionGraph.Choose(u, ExpressionGraph.Less(u, delta, L(u.Feature, -span / 2)),
                Sum((delta, 1), (L(u.Feature, span), 1)), delta));
    }

    public void Observe()
    {
        var time = (long)Observations.Time.Value;
        if (Observations.Presence.Value == 0 || (Ready.Value != 0 && time <= (long)PreviousTime.Value)) return;
        if (Ready.Value != 0)
        {
            var reads = ReadMany(MotionRecipe);
            var before = G(L(SpeedSquared, Held(SpeedSquared)), L(PreviousX, Held(PreviousX)), L(PreviousY, Held(PreviousY)),
                L(PreviousTime, Held(PreviousTime)));
            var imported = TryValue(reads[0], out var speed) ? Aim(SpeedSquared, speed) : reads[0];
            LastMotion = TimeStepSequence.Step(new Carrier(), units.Clock, (long)PreviousTime.Value, time,
                before, G(MotionRecipe, reads[0], imported), G(L(SpeedSquared, Held(SpeedSquared)), Observations.Root), reads[0].Right);
            // Forecast is a retained stock frozen at issue; outcomes cannot revise it.
            var claim = OperationClaim.Equality(new Carrier(), L(Forecast, Held(Forecast)),
                L(Forecast, Held(SpeedSquared)), new Carrier(Forecast.UnitShape));
            Relation admission = G(new Placeholder());
            if (TryValue(reads[0], out _) && OperationClaim.TryReadResolvedViolation(claim, out _))
            {
                var error = ReadMany(PredictionErrorRecipe)[0];
                if (TryValue(error, out var amount))
                {
                    admission = G(error, Aim(Error, amount), Aim(Pressure, 1 + amount), L(ErrorTotal, amount));
                    ErrorTotal.Add((ulong)amount); PredictionChecks.Add(1);
                }
            }
            var persistenceClaim = OperationClaim.Equality(new Carrier(), L(PersistenceForecast, Held(PersistenceForecast)),
                L(PersistenceForecast, Held(SpeedSquared)), new Carrier(SpeedSquared.UnitShape));
            LastPrediction = G(L(ForecastTime, Held(ForecastTime)), L(units.Clock, time),
                L(Forecast, Held(Forecast)), L(PersistenceForecast, Held(PersistenceForecast)), reads[0], claim,
                persistenceClaim, admission); // A claim becomes pressure only through this explicit import.
            var states = ReadMany(blockedRecipe, painRecipe);
            if (TryValue(states[0], out var blocked)) Aim(Blocked, blocked);
            if (TryValue(states[1], out var pain)) Aim(PainMemory, pain);
            if (time % Bath.TicksPerSecond == 0)
            {
                var outcome = ReadMany(OutcomeRecipe)[0];
                if (outcome.Left is CarrierLocation value && TryValue(outcome, out _))
                {
                    Utility.Record(value, Bath.TicksPerSecond, 0);
                    Pilot.Record(outcome, Bath.TicksPerSecond);
                }
                LastOutcome = G(L(units.Clock, time), outcome, Utility.Root, Pilot.LastCredit);
                recent.Add(G(LastPrediction, LastOutcome)); if (recent.Count > 3) recent.RemoveAt(0);
            }
        }
        else Aim(Ready, 1);
        // All memory imports are later-input steps with explicit read/receive witnesses.
        var imports = new List<IElement> { Import(memories[0], PreviousX), Import(memories[1], PreviousY),
            Import(memories[2], PreviousTime), Import(AgeRecipe, creature.Age) };
        if (time % Bath.TicksPerSecond == 0 || Samples.Value == 0)
        { imports.Add(Import(memories[3], PreviousEnergy)); imports.Add(Import(memories[4], PreviousIntake)); }
        LastMemory = G(L(units.Clock, time), G(imports.ToArray()));
        Samples.Add(1);
    }

    public void ForecastNext(long time)
    {
        if (time % 4 != 0) return;
        var read = ReadMany(ForecastRecipe)[0];
        LastIssue = G(read, TryValue(read, out var expected) ? Aim(Forecast, expected) : read,
            Import(persistenceRecipe, PersistenceForecast), Import(issueClockRecipe, ForecastTime));
        Inquiry.Prepare(time);
    }
}
