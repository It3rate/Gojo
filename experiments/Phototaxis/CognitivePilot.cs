using Gojo.Core;
using static Phototaxis.Native;

namespace Phototaxis;

/// <summary>A local handle to parameters that participate in connected recipes and witnessed utility.</summary>
public sealed class PilotModule
{
    public Carrier[] Parameters { get; }
    public Utility Utility { get; }
    public Relation Uses { get; set; } = G();
    public IElement Activity { get; set; } = new Placeholder();
    public Relation Admission { get; }
    public Carrier EstimatedUtility { get; }
    public Relation Quote { get; }
    public Relation Root => G(G(Parameters), Uses, Utility.Root, Admission, Activity, EstimatedUtility, Quote);
    public PilotModule(Units u, params long[] values)
    {
        Parameters = values.Select(_ => new Carrier(u.Feature.UnitShape)).ToArray();
        Admission = Carrier.ReceiveTogether(G(values.Select((v, i) => (IElement)L(Parameters[i], v)).ToArray()));
        Utility = new(u);
        EstimatedUtility = new(u.Return.UnitShape);
        Quote = SumTo(new Carrier(u.Return.UnitShape), (Utility.Bid, 1), (Signed(EstimatedUtility), 1));
    }

    public static PilotModule Copy(Units u, PilotModule parent, out Relation correspondence)
    {
        var child = new PilotModule(u, parent.Parameters.Select(_ => 0L).ToArray());
        correspondence = G(parent.Root, child.Root, G(parent.Parameters.Select((p, i) =>
            (IElement)G(L(p, Held(p)), new Relation(p, child.Parameters[i]),
                Import(Scale(Signed(p), 1, 1, child.Parameters[i]), child.Parameters[i]))).ToArray()));
        return child;
    }
}

/// <summary>Our connected light/recovery pilot. The builder is disposable; the graph is the program.</summary>
public sealed class CognitivePilot
{
    public PilotModule[] Modules { get; }
    public Carrier Appetite { get; }
    public Carrier Phase { get; }
    public Carrier AdviceTurn { get; }
    public Carrier AdviceUntil { get; }
    public Carrier RecoveryTurnUntil { get; }
    public Carrier RecoveryUntil { get; }
    public IElement RecoveryActive { get; }
    public ProgressEscape? Progress { get; }
    public IElement EscapeActive { get; }
    public Carrier CourtshipSignal { get; }
    public IElement CourtshipEligible { get; }
    public IElement CourtshipActive { get; }
    public Relation CourtshipTurn { get; }
    public Relation Courtship { get; }
    public Relation Drive { get; }
    public Relation Turn { get; }
    public Relation Ancestry { get; }
    public Relation LastCredit { get; private set; } = G(new Placeholder());
    public Relation Root => G(G(Modules.Select(m => (IElement)m.Root).ToArray()),
        G(Appetite, Phase, AdviceTurn, AdviceUntil, RecoveryTurnUntil, RecoveryUntil), Drive, Turn, Ancestry, LastCredit,
        Progress?.Root ?? G(), Courtship);
    private readonly Units units;

    // Coherent modules: motion/feeding, gradient response, appetite memory, recovery/search.
    // Values initialize native carriers; they never remain as a parallel runtime genome.
    public static PilotModule[] Founder(Units u, int slot) =>
    [
        new(u, 8, 5, 8, 12),
        new(u, slot % 5 == 1 ? 4 : 8, slot % 5 == 1 ? 6 : 8),
        new(u, slot % 5 == 2 ? 12 : 14, slot % 5 == 2 ? 0 : 12),
        new(u, slot % 5 == 0 ? 0 : slot % 5 == 3 ? 10 : 6, 40, 5,
            slot % 5 == 4 ? -1 : 1, 20, 100, slot % 5 == 0 ? 0 : slot % 5 == 1 ? 6 : 4, 160, 768, 80)
    ];

    public CognitivePilot(Units u, Creature c, CognitiveBody body, PilotModule[] modules, Relation? ancestry,
        bool binocular = true, PilotFeatures? features = null)
    {
        units = u; Modules = modules; Ancestry = ancestry ?? G(); features ??= PilotFeatures.Current;
        IElement N(long n) => L(u.Feature, n);
        var ports = modules.Select(m => m.Parameters.Select(p => (IElement)Signed(p)).ToArray()).ToArray();
        IElement P(int m, int p) => ports[m][p];
        IElement C(IElement condition, IElement a, IElement b) => ExpressionGraph.Choose(u, condition, a, b);
        IElement Greater(IElement a, IElement b) => ExpressionGraph.Greater(u, a, b);
        IElement Less(IElement a, IElement b) => ExpressionGraph.Less(u, a, b);
        IElement Abs(IElement a) => C(Less(a, N(0)), Scale(a, -1, 1), a);
        IElement leftEye = c.LeftEye.Signal, rightEye = c.RightEye.Signal;
        var brightness = ExpressionGraph.Add(u, leftEye, rightEye);
        if (!binocular)
        {
            // Control experiment: both spatial inputs consume the same native
            // mean. Original brightness, clocks, appetite and recovery remain.
            leftEye = rightEye = Scale(brightness, 1, 2, new Carrier(u.Feature.UnitShape));
        }
        var delta = ExpressionGraph.Subtract(u, rightEye, leftEye);
        IElement LightThreshold(IElement value) => Scale(value, c.LeftEye.Maximum.Ticks, 15, new Carrier(u.Feature.UnitShape));
        var fullness = c.Sources[4];
        Appetite = new(u.Feature.UnitShape); Aim(Appetite, 15);
        Phase = new(u.Feature.UnitShape); AdviceTurn = new(u.Feature.UnitShape); AdviceUntil = new(u.Clock.UnitShape);
        RecoveryTurnUntil = new(u.Clock.UnitShape); RecoveryUntil = new(u.Clock.UnitShape);
        var cells = new List<Relation>();
        IElement hunger;
        if (Held(modules[2].Parameters[1]) > 0)
        {
            var next = C(Less(fullness, P(2, 1)), N(15),
                C(Greater(fullness, ExpressionGraph.Subtract(u, P(2, 0), N(1))), N(0), Appetite.ValueSlot));
            cells.Add(ControllerFeedback.Create(u, Appetite, next, 4, c.BornAt)); hunger = Appetite.ValueSlot;
            modules[2].Uses = G(next, cells[^1], fullness);
        }
        else
        {
            var next = Less(fullness, P(2, 0));
            cells.Add(ControllerFeedback.Create(u, Appetite, next, 4, c.BornAt));
            hunger = Appetite.ValueSlot;
            modules[2].Uses = G(next, cells[^1], fullness);
        }
        var hasTurning = Held(modules[3].Parameters[0]) != 0;
        RecoveryActive = Greater(RecoveryUntil.ValueSlot, body.Observations.Time.ValueSlot);
        // G(clock stock, next recipe, calibration, cadence, next clock, witnessed step).
        // A fresh injury starts one bounded turn. Continued pain extends the
        // straight escape interval, never the turn interval. These clocks are
        // private memory, derived only from the body's observation ports.
        var injured = Greater(body.Observations.Injury.ValueSlot, N(0));
        IElement Duration(int index, long fallback) => Scale(index < ports[3].Length ? P(3, index) : N(fallback),
            1, 1, new Carrier(u.Clock.UnitShape));
        IElement Deadline(IElement duration) => SumTo(new Carrier(u.Clock.UnitShape),
            (body.Observations.Time.ValueSlot, 1), (duration, 1));
        var turnDeadline = C(ExpressionGraph.And(u, injured, ExpressionGraph.Not(u, RecoveryActive)),
            Deadline(Duration(4, 20)), RecoveryTurnUntil.ValueSlot);
        var escapeDeadline = C(injured, Deadline(Duration(5, 100)), RecoveryUntil.ValueSlot);
        // Ordered imports: the turn onset reads the previous escape stock.
        Relation[] escapeCells = [ControllerFeedback.Create(u, RecoveryTurnUntil, turnDeadline, 4, c.BornAt),
            ControllerFeedback.Create(u, RecoveryUntil, escapeDeadline, 4, c.BornAt)];
        cells.AddRange(escapeCells);
        IElement Parameter(int index, long fallback) => index < ports[3].Length ? P(3, index) : N(fallback);
        if (features.ProgressEscape)
        {
            Progress = new(u, c, body.Observations, Parameter(7, 160), Parameter(8, 768), Parameter(9, 80));
            cells.AddRange(Progress.Cells);
        }
        EscapeActive = Progress is null ? RecoveryActive : ExpressionGraph.Or(u, RecoveryActive, Progress.Active);
        CourtshipSignal = Limited(u.Feature, 15);
        var mature = ExpressionGraph.Not(u, Less(c.Age.ValueSlot, L(u.Clock, Bath.MaturityTicks)));
        var available = ExpressionGraph.Not(u, Less(body.Observations.Time.ValueSlot, c.NextMating.ValueSlot));
        // An organism publishes its own readiness; the world only senses that
        // emitted signal over a finite range. No world-supplied fitness port.
        CourtshipEligible = features.Courtship ? ExpressionGraph.And(u, mature,
            ExpressionGraph.And(u, available, ExpressionGraph.And(u, Greater(fullness, N(9)),
                ExpressionGraph.And(u, Less(brightness, LightThreshold(N(8))), ExpressionGraph.Not(u, EscapeActive))))) : N(0);
        Relation[] courtshipCells = features.Courtship ? [ControllerFeedback.Create(u, CourtshipSignal, CourtshipEligible, 4, c.BornAt)] : [];
        cells.AddRange(courtshipCells);
        var matePresence = Greater(ExpressionGraph.Add(u, body.Observations.LeftMate.ValueSlot, body.Observations.RightMate.ValueSlot), N(0));
        CourtshipActive = ExpressionGraph.And(u, CourtshipEligible,
            ExpressionGraph.And(u, matePresence, Greater(Parameter(6, 4), N(0))));
        var mateDelta = ExpressionGraph.Subtract(u, body.Observations.RightMate.ValueSlot, body.Observations.LeftMate.ValueSlot);
        var towardMate = Scale(Product(mateDelta, Parameter(6, 4)), 1, 15, new Carrier(u.Feature.UnitShape));
        var rearTurn = C(Greater(Abs(mateDelta), N(1)), C(Greater(mateDelta, N(0)), Parameter(6, 4), Scale(Parameter(6, 4), -1, 1)),
            Product(Parameter(6, 4), P(3, 3)));
        CourtshipTurn = (Relation)C(Less(Signed(body.Observations.MateAhead), N(0)), rearTurn, towardMate);
        Courtship = G(CourtshipEligible, CourtshipSignal, CourtshipActive, CourtshipTurn, G(courtshipCells),
            G(body.Observations.LeftMate, body.Observations.RightMate, body.Observations.MateAhead), c.NextMating, c.Age);
        var ordinaryDrive = C(hunger, C(Greater(brightness, LightThreshold(P(0, 3))), P(0, 1), P(0, 0)), P(0, 2));
        Drive = (Relation)C(EscapeActive, P(0, 2), C(CourtshipActive, P(0, 1), ordinaryDrive));
        var rawTurn = Scale(Product(delta, P(1, 0)), 15, c.LeftEye.Maximum.Ticks, new Carrier(u.Feature.UnitShape));
        var bounded = C(Greater(rawTurn, P(1, 1)), P(1, 1),
            C(Less(rawTurn, Scale(P(1, 1), -1, 1)), Scale(P(1, 1), -1, 1), rawTurn));
        IElement turn = C(hunger, bounded, Scale(bounded, -1, 1));
        var left = new List<Component> { new(u, Drive, 15) };
        var right = new List<Component> { new(u, Drive, 15) };
        modules[0].Uses = G(Drive, brightness, hunger);
        modules[1].Uses = G(rawTurn, bounded, turn, delta);
        modules[0].Activity = N(15); modules[1].Activity = Greater(Abs(delta), N(0));
        modules[2].Activity = N(15); modules[3].Activity = ExpressionGraph.Or(u, EscapeActive, CourtshipActive);
        // A zero-turn module retains a complete straight escape response and
        // receives witnessed participation credit while that response is active.
        modules[3].Uses = G(RecoveryActive, RecoveryTurnUntil, RecoveryUntil, G(escapeCells), Drive,
            body.Observations.Injury.ValueSlot, Progress?.Root ?? G(), Courtship);
        if (hasTurning)
        {
            var nextPhase = C(Greater(brightness, LightThreshold(N(2))), N(0),
                C(Greater(Phase.ValueSlot, ExpressionGraph.Subtract(u, P(3, 1), N(2))), N(0),
                    ExpressionGraph.Add(u, Phase.ValueSlot, N(1))));
            Aim(Phase, c.Slot * 17L % Math.Max(1, Held(modules[3].Parameters[1])));
            cells.Add(ControllerFeedback.Create(u, Phase, nextPhase, 4, c.BornAt));
            var search = C(Less(brightness, LightThreshold(N(3))), C(Less(Phase.ValueSlot, P(3, 2)),
                Product(P(3, 0), P(3, 3)), N(0)), N(0));
            var contactDifference = ExpressionGraph.Subtract(u, body.Observations.RightContact.ValueSlot,
                body.Observations.LeftContact.ValueSlot);
            // A right obstacle requires a left turn (negative left-minus-right thrust).
            var contactTurn = C(Greater(Abs(contactDifference), N(0)),
                C(Greater(contactDifference, N(0)), Scale(P(3, 0), -1, 1), P(3, 0)), Product(P(3, 0), P(3, 3)));
            var blocked = Greater(body.Blocked.ValueSlot, N(1));
            var injuryTurn = C(Greater(RecoveryTurnUntil.ValueSlot, body.Observations.Time.ValueSlot),
                Product(P(3, 0), P(3, 3)), N(0));
            // Rock contact remains directional. Pain gets a finite turn followed
            // by straight thrust, with light/search/advice suppressed until clear.
            var recovery = C(blocked, contactTurn, C(RecoveryActive, injuryTurn,
                C(EscapeActive, N(0), C(CourtshipActive, N(0), search))));
            left.Add(new(u, recovery, 15)); right.Add(new(u, recovery, -15));
            modules[3].Uses = G(nextPhase, cells[^1], recovery, body.Pressure, body.LastPrediction,
                RecoveryActive, RecoveryTurnUntil, RecoveryUntil, G(escapeCells), Progress?.Root ?? G(), Courtship);
            modules[3].Activity = ExpressionGraph.Or(u, ExpressionGraph.Or(u, EscapeActive, CourtshipActive), Greater(Abs(recovery), N(0)));
        }
        var activeAdvice = C(Greater(AdviceUntil.ValueSlot, body.Observations.Time.ValueSlot), Signed(AdviceTurn), N(0));
        Turn = (Relation)C(EscapeActive, N(0), C(CourtshipActive, CourtshipTurn, ExpressionGraph.Add(u, turn, activeAdvice)));
        left.Add(new(u, Turn, 15)); right.Add(new(u, Turn, -15));
        c.ConfigureFeedback(cells.ToArray());
        c.LeftMuscle.InstallInherited(new(u, left.ToArray())); c.RightMuscle.InstallInherited(new(u, right.ToArray()));
    }

    public void Record(Relation outcome, long duration)
    {
        if (outcome.Left is not CarrierLocation value) { LastCredit = G(outcome, new Placeholder()); return; }
        var share = ReadMany(Scale(value, 1, Modules.Length, new Carrier(units.Return.UnitShape)))[0];
        var activities = ReadMany(Modules.Select(m => m.Activity).ToArray());
        if (share.Left is CarrierLocation returned && TryValue(share, out _))
            for (var i = 0; i < Modules.Length; i++)
                if (TryValue(activities[i], out var active) && active == 15) Modules[i].Utility.Record(returned, duration, 0);
        // This is shared-body credit, explicitly not causal credit for one component.
        LastCredit = G(outcome, share, G(activities), G(Modules.Select(m => (IElement)m.Utility.Root).ToArray()));
    }

    public static PilotModule[] Mate(Units u, CognitivePilot a, CognitivePilot b, Creature child,
        long time, bool selection, out Relation market)
    {
        var chosen = new List<PilotModule>(); var records = new List<IElement>();
        for (var i = 0; i < a.Modules.Length; i++)
        {
            var first = a.Modules[i]; var second = b.Modules[i]; var accepted = first;
            var quotes = ReadMany(first.Quote, second.Quote);
            var funded = AtLeast(L(child.Wallet, Held(child.Wallet)), L(child.Wallet, 1), out var funding);
            var payment = selection && funded ? Transfer(child.Wallet, child.SpentAttention, 1) : G(funding);
            Relation comparison = G(new Placeholder());
            if (selection && funded && TryValue(quotes[0], out _) && TryValue(quotes[1], out _) &&
                AtLeast((CarrierLocation)quotes[1].Left, L(((CarrierLocation)quotes[0].Left).Carrier,
                    ((CarrierLocation)quotes[0].Left).Ticks + 1), out comparison)) accepted = second;
            chosen.Add(PilotModule.Copy(u, accepted, out var link));
            records.Add(G(first.Root, second.Root, quotes[0], quotes[1], comparison, payment, link));
        }
        market = G(L(u.Clock, time), G(records.ToArray()), G(chosen.Select(m => (IElement)m.Root).ToArray()));
        return chosen.ToArray();
    }
}
