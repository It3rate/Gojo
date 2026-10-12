using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using static Phototaxis.Native;

namespace Phototaxis;

/// <summary>UTF8 transport text stored as native byte locations. These are boundary labels, not a policy AST.</summary>
public static class InquiryText
{
    private static readonly Carrier bytes = new();
    public static Relation Encode(string text) => G(Encoding.UTF8.GetBytes(text).Select(b => (IElement)L(bytes, b)).ToArray());
    public static string Decode(Relation text) => Encoding.UTF8.GetString(Parts(text).OfType<CarrierLocation>().Select(l => (byte)l.Ticks).ToArray());
}

public sealed class InquiryOption
{
    public Relation Description { get; }
    public Relation Effect { get; }
    public Carrier Probability { get; } = new();
    public Relation Root => G(Description, Effect, Probability);
    public InquiryOption(string description, Carrier? target = null, long desired = 0)
    {
        Description = InquiryText.Encode(description);
        Effect = target is null ? G(new Placeholder()) : G(target, L(target, desired));
    }
}

/// <summary>An issue freezes evidence and offered effects. The transport only admits typed answers.</summary>
public sealed class InquiryQuestion
{
    public Relation Prompt { get; }
    public Relation Premises { get; }
    public Relation Focus { get; }
    public InquiryOption[] Options { get; }
    public CarrierLocation IssuedAt { get; }
    public CarrierLocation ExpiresAt { get; }
    public Carrier Confidence { get; } = new();
    public Carrier Answered { get; } = new();
    public Relation Freshness { get; }
    public Relation Admission { get; set; } = G(new Placeholder());
    public Relation Selection { get; set; } = G(new Placeholder());
    public Relation Root => G(Prompt, Premises, Focus, G(Options.Select(o => (IElement)o.Root).ToArray()),
        IssuedAt, ExpiresAt, Confidence, Answered, Freshness, Admission, Selection);
    public InquiryQuestion(Units u, Creature c, CognitiveBody body, string prompt, InquiryOption[] options, long time)
    {
        Prompt = InquiryText.Encode(prompt); Options = options;
        // Snapshots, not live carrier slots. Only information this body has actually observed.
        Premises = G(L(c.LeftEye.Pixels[0], Held(c.LeftEye.Pixels[0])), L(c.RightEye.Pixels[0], Held(c.RightEye.Pixels[0])),
            L(c.Energy, Held(c.Energy)), L(body.Pilot.Appetite, Held(body.Pilot.Appetite)),
            L(body.SpeedSquared, Held(body.SpeedSquared)), L(body.Error, Held(body.Error)),
            L(body.Blocked, Held(body.Blocked)), L(body.PainMemory, Held(body.PainMemory)),
            L(body.Observations.LeftContact, Held(body.Observations.LeftContact)),
            L(body.Observations.RightContact, Held(body.Observations.RightContact)),
            L(body.Observations.LeftProximity, Held(body.Observations.LeftProximity)),
            L(body.Observations.RightProximity, Held(body.Observations.RightProximity)),
            L(body.Observations.X, Held(body.Observations.X)), L(body.Observations.Y, Held(body.Observations.Y)),
            L(body.Observations.Time, Held(body.Observations.Time)),
            G(body.LastPrediction, body.LastOutcome),
            G(body.Pilot.Modules.Select(m => (IElement)G(G(m.Parameters.Select(p => (IElement)L(p, Held(p))).ToArray()),
                L(m.Utility.Visits, Held(m.Utility.Visits)), L(m.Utility.Returns, Held(m.Utility.Returns)),
                L(m.Utility.RecentReturn, Held(m.Utility.RecentReturn)), L(m.EstimatedUtility, Held(m.EstimatedUtility)))).ToArray()));
        Focus = G(c.LeftMuscle.Current.Root, c.RightMuscle.Current.Root, body.Pilot.Root);
        IssuedAt = L(u.Clock, time); ExpiresAt = L(u.Clock, time + 80);
        var withinTime = ExpressionGraph.Less(u, body.Observations.Time.ValueSlot, ExpiresAt);
        var a = ExpressionGraph.Subtract(u, c.LeftEye.Pixels[0].ValueSlot, Parts(Premises)[0]);
        var b = ExpressionGraph.Subtract(u, c.RightEye.Pixels[0].ValueSlot, Parts(Premises)[1]);
        IElement Stable(IElement d, Eye eye) => ExpressionGraph.And(u,
            ExpressionGraph.Less(u, d, Scale(L(u.Feature, 3), eye.Maximum.Ticks, 15)),
            ExpressionGraph.Greater(u, d, Scale(L(u.Feature, -3), eye.Maximum.Ticks, 15)));
        var sameAppetite = ExpressionGraph.Equal(u, body.Pilot.Appetite.ValueSlot, Parts(Premises)[3]);
        Freshness = ExpressionGraph.And(u, withinTime, ExpressionGraph.And(u, sameAppetite,
            ExpressionGraph.And(u, Stable(a, c.LeftEye), Stable(b, c.RightEye))));
    }
}

public sealed class NativeInquiry
{
    public Carrier Fuel { get; }
    public Carrier Enabled { get; }
    public Carrier NextTime { get; }
    public Carrier Cursor { get; }
    public Carrier Requests { get; }
    public Carrier Answers { get; }
    public Carrier Stale { get; }
    public Carrier Failed { get; }
    public Carrier AdviceTrials { get; }
    public Carrier AcceptedKeeps { get; }
    public Relation Trigger { get; }
    public InquiryQuestion? Pending { get; private set; }
    public Relation Last { get; private set; } = G(new Placeholder());
    public Relation LastTrial { get; private set; } = G(new Placeholder());
    public Relation Root => G(Fuel, Enabled, NextTime, Cursor, Requests, Answers, Stale, Failed,
        AdviceTrials, Trigger, Pending?.Root ?? G(new Placeholder()), Last, LastTrial, Menu, questionChoice, EffectPorts, AcceptedKeeps);
    public bool Available => Pending is { } q && q.Answered.Value == 0;
    private readonly Units units;
    private readonly Creature creature;
    private readonly CognitiveBody body;
    private readonly Relation questionChoice;
    public Relation Menu { get; }
    public Relation EffectPorts { get; }
    private readonly Carrier trialStartEnergy, trialStartIntake;
    private long trialStart = -1;

    public NativeInquiry(Units u, Creature c, CognitiveBody body, bool enabled)
    {
        units = u; creature = c; this.body = body;
        Fuel = new(u.Count.UnitShape); Enabled = new(u.Count.UnitShape); NextTime = new(u.Clock.UnitShape);
        Cursor = new(u.Count.UnitShape); Requests = new(u.Count.UnitShape); Answers = new(u.Count.UnitShape);
        Stale = new(u.Count.UnitShape); Failed = new(u.Count.UnitShape); AdviceTrials = new(u.Count.UnitShape);
        AcceptedKeeps = new(u.Count.UnitShape);
        trialStartEnergy = new(u.Calorie.UnitShape); trialStartIntake = new(u.Calorie.UnitShape);
        Aim(Fuel, enabled ? 2 : 0); Aim(Enabled, enabled ? 1 : 0); Aim(NextTime, c.BornAt + 100 + c.Slot % 5 * 8);
        Aim(Cursor, c.Slot % 4);
        questionChoice = ExpressionGraph.Choose(u, ExpressionGraph.Equal(u, Cursor.ValueSlot, L(u.Feature, 0)), L(u.Feature, 0),
            ExpressionGraph.Choose(u, ExpressionGraph.Equal(u, Cursor.ValueSlot, L(u.Feature, 1)), L(u.Feature, 1),
                ExpressionGraph.Choose(u, ExpressionGraph.Equal(u, Cursor.ValueSlot, L(u.Feature, 2)), L(u.Feature, 2), L(u.Feature, 3))));
        EffectPorts = G(body.Pilot.AdviceTurn, G(body.Pilot.Modules.Select(m => (IElement)m.EstimatedUtility).ToArray()));
        Relation Turns(string prompt) => G(InquiryText.Encode(prompt), G(
            new InquiryOption("Keep the current native strategy; no additional turn bias.", body.Pilot.AdviceTurn, 0).Root,
            new InquiryOption("Add +6 to left thrust and -6 to right thrust; heading increases (right turn in screen coordinates).", body.Pilot.AdviceTurn, 6).Root,
            new InquiryOption("Add -6 to left thrust and +6 to right thrust; heading decreases (left turn in screen coordinates).", body.Pilot.AdviceTurn, -6).Root));
        Menu = G(Turns("Which offered small turn intervention best supports my current appetite: seek more light when hungry, or travel into darkness when full? Consider asynchronous eyes and native actuator equations."),
            Turns("Which offered actuator intervention is most likely to escape my observed blocked or painful situation? Use my observations and frozen predictions. Preserve motion when obstruction evidence is weak."),
            Turns("I want to test a right turn. Which offered variable change makes my body turn right, according to my connected actuator equations? Choose keep if the wiring is unclear."),
            G(InquiryText.Encode("Which offered connected module deserves a small ESTIMATED utility premium at my next breeding auction, given my own experience? This is a hypothesis, not an observed reward. Prefer keep if evidence is weak."),
                G(new InquiryOption("Leave current bids unchanged; add no new estimate premium.", body.Pilot.Modules[3].EstimatedUtility, 0).Root,
                    new InquiryOption("Give the obstacle/pain recovery and dark-search module an estimated +20 bid premium.", body.Pilot.Modules[3].EstimatedUtility, 20).Root,
                    new InquiryOption("Give the eye-gradient steering module an estimated +20 bid premium.", body.Pilot.Modules[1].EstimatedUtility, 20).Root,
                    new InquiryOption("Give the appetite hysteresis memory module an estimated +20 bid premium.", body.Pilot.Modules[2].EstimatedUtility, 20).Root)));
        var needsHelp = ExpressionGraph.Or(u, ExpressionGraph.Greater(u, body.Pressure.LiveTensionSlot, L(u.Feature, 80)),
            ExpressionGraph.Or(u, ExpressionGraph.Greater(u, body.Blocked.ValueSlot, L(u.Feature, 1)),
                ExpressionGraph.Greater(u, c.Deficit, L(u.Feature, 4))));
        Trigger = ExpressionGraph.And(u, ExpressionGraph.Greater(u, Enabled.ValueSlot, L(u.Feature, 0)),
            ExpressionGraph.And(u, ExpressionGraph.Greater(u, Fuel.ValueSlot, L(u.Feature, 0)), needsHelp));
    }

    public void Prepare(long time)
    {
        if (trialStart >= 0 && time >= (long)body.Pilot.AdviceUntil.Value)
        {
            var outcome = ReadMany(SumTo(new Carrier(units.Return.UnitShape),
                (creature.Energy.ValueSlot, 1), (trialStartEnergy.ValueSlot, -1),
                (Scale(Sum((creature.Eaten.ValueSlot, 1), (trialStartIntake.ValueSlot, -1)), 1, 4), 1)))[0];
            LastTrial = TimeStepSequence.Step(new Carrier(), units.Clock, trialStart, time,
                G(L(trialStartEnergy, Held(trialStartEnergy)), L(trialStartIntake, Held(trialStartIntake))), Last,
                G(outcome, L(creature.Energy, Held(creature.Energy)), L(creature.Eaten, Held(creature.Eaten))), outcome.Right);
            trialStart = -1;
        }
        if (Available || time < (long)NextTime.Value) return;
        var reads = ReadMany(Trigger, questionChoice);
        if (!TryValue(reads[0], out var triggered) || triggered != 15) return;
        if (!TryValue(reads[1], out var selected) || selected < 0 || selected >= Parts(Menu).Length) return;
        var template = Parts((Relation)Parts(Menu)[selected]);
        var options = Parts((Relation)template[1]).Cast<Relation>().Select(root =>
        {
            var fields = Parts(root); var effect = Parts((Relation)fields[1]);
            return new InquiryOption(InquiryText.Decode((Relation)fields[0]), effect.Length == 2 ? effect[0] as Carrier : null,
                effect.Length == 2 && effect[1] is CarrierLocation target ? target.Ticks : 0);
        }).ToArray();
        Ask(InquiryText.Decode((Relation)template[0]), options);
        Aim(Cursor, selected == 3 ? 0 : selected + 1); Aim(NextTime, time + 1200);
    }

    /// <summary>Any authored question can be asked with native alternatives, including read-only alternatives.</summary>
    public InquiryQuestion? Ask(string prompt, params InquiryOption[] options)
    {
        if (Available || options.Length is < 2 or > 255) return null;
        var time = (long)body.Observations.Time.Value;
        var funded = AtLeast(L(Fuel, Held(Fuel)), L(Fuel, 1), out var fuelClaim) &&
            AtLeast(L(creature.Wallet, Held(creature.Wallet)), L(creature.Wallet, 1), out _);
        if (!funded) { Last = G(fuelClaim, new Placeholder()); return null; }
        var payment = Transfer(creature.Wallet, creature.SpentAttention, 1);
        var consumed = Aim(Fuel, Held(Fuel) - 1);
        Pending = new(units, creature, body, prompt, options, time);
        Requests.Add(1); Last = G(Pending.Root, payment, consumed);
        return Pending;
    }

    public void Admit(InquiryQuestion question, string response)
    {
        if (!ReferenceEquals(question, Pending) || question.Answered.Value != 0) return;
        try
        {
            using var doc = JsonDocument.Parse(response);
            var answer = doc.RootElement.GetProperty("answers").GetProperty("decision");
            if (answer.GetProperty("type").GetString() != "choice") { Fail(question, "Unexpected reply shape"); return; }
            var probabilities = answer.GetProperty("probabilities");
            var arrivals = new List<IElement>();
            for (var i = 0; i < question.Options.Length; i++)
            {
                var p = probabilities.GetProperty("option_" + i).GetDouble();
                if (!double.IsFinite(p) || p < 0 || p > 1) { Fail(question, "Invalid probability"); return; }
                arrivals.Add(L(question.Options[i].Probability, (long)Math.Round(p * 1_000_000)));
            }
            var confidence = answer.GetProperty("confidence").GetDouble();
            if (!double.IsFinite(confidence) || confidence < 0 || confidence > 1) { Fail(question, "Invalid confidence"); return; }
            arrivals.Add(L(question.Confidence, (long)Math.Round(confidence * 1_000_000)));
            question.Admission = G(Carrier.ReceiveTogether(G(arrivals.ToArray())),
                Relation.Segment(question.Confidence, 0, 1_000_000), InquiryText.Encode(response));
            Aim(question.Answered, 1); Answers.Add(1);
            var freshness = ReadMany(question.Freshness)[0];
            if (!TryValue(freshness, out var fresh) || fresh != 15)
            { Stale.Add(1); question.Selection = G(freshness, new Placeholder()); Last = question.Root; return; }
            var quotes = new List<IElement> { freshness }; var best = 0;
            for (var i = 1; i < question.Options.Length; i++)
            {
                if (AtLeast(L(question.Options[i].Probability, Held(question.Options[i].Probability)),
                    L(question.Options[best].Probability, Held(question.Options[best].Probability) + 1), out var comparison)) best = i;
                quotes.Add(comparison);
            }
            var confident = AtLeast(L(question.Confidence, Held(question.Confidence)), L(question.Confidence, 350_000), out var confidenceClaim);
            quotes.Add(confidenceClaim);
            var effect = Parts(question.Options[best].Effect);
            if (confident && effect.Length == 2 && effect[0] is Carrier target && effect[1] is CarrierLocation desired &&
                (target.SameIdentityAs(body.Pilot.AdviceTurn) || body.Pilot.Modules.Any(m => target.SameIdentityAs(m.EstimatedUtility))))
            {
                if (desired.Ticks == 0) { AcceptedKeeps.Add(1); quotes.Add(G(desired, new Placeholder())); }
                else
                {
                quotes.Add(target.SameIdentityAs(body.Pilot.AdviceTurn) ? Aim(target, desired.Ticks) :
                    Import(Scale(Product(desired, question.Confidence.ValueSlot), 1, 1_000_000, target), target));
                quotes.Add(Import(SumTo(new Carrier(units.Clock.UnitShape), (body.Observations.Time.ValueSlot, 1), (L(units.Clock, 40), 1)), body.Pilot.AdviceUntil));
                quotes.Add(Import(Scale(creature.Energy.ValueSlot, 1, 1, trialStartEnergy), trialStartEnergy));
                quotes.Add(Import(Scale(creature.Eaten.ValueSlot, 1, 1, trialStartIntake), trialStartIntake));
                trialStart = (long)body.Observations.Time.Value; AdviceTrials.Add(1);
                }
            }
            else if (confident && effect.Length == 2 && effect[0] is Carrier)
                quotes.Add(Relation.UnresolvedRead(question.Options[best].Root, EffectPorts, question.Confidence));
            question.Selection = G(G(quotes.ToArray()), question.Options[best].Root); Last = question.Root;
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        { Fail(question, "Incomplete typed reply"); }
    }

    public void Fail(InquiryQuestion question, string reason)
    {
        if (question.Answered.Value != 0) return;
        Aim(question.Answered, 1); Failed.Add(1);
        question.Admission = G(InquiryText.Encode(reason), Relation.UnresolvedRead(question.Premises, question.Focus, question.Confidence));
        Last = question.Root;
    }

    public static string Request(InquiryQuestion q, Creature c)
    {
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream))
        {
            w.WriteStartObject(); w.WriteString("model", "jev-1.13.0"); w.WriteStartObject("state");
            string[] labels = ["left_eye_0_to_15", "right_eye_0_to_15", "energy_0_to_1200", "appetite_15_hungry_0_full",
                "inferred_speed_squared_pixels_per_second_squared", "forecast_absolute_error", "blocked_persistence_0_to_15",
                "pain_memory_0_to_15", "left_contact", "right_contact", "left_proximity", "right_proximity", "x_ticks_16_per_pixel",
                "y_ticks_16_per_pixel", "time_ticks_20_per_second"];
            var premises = Parts(q.Premises);
            for (var i = 0; i < labels.Length; i++) if (premises[i] is CarrierLocation l) w.WriteNumber(labels[i], l.Ticks);
            void Program(string label, Circuit circuit)
            {
                w.WriteStartArray(label);
                foreach (var component in circuit.Components)
                {
                    w.WriteStartObject();
                    var expression = ExpressionGraph.Describe(c.Features, component.Source);
                    w.WriteString("expression", expression.Length <= 1800 ? expression : expression[..1800]);
                    w.WriteBoolean("description_truncated", expression.Length > 1800);
                    w.WriteNumber("coefficient_numerator_over_15", Held(component.Coefficient));
                    w.WriteEndObject();
                }
                w.WriteEndArray();
            }
            Program("left_terms", c.LeftMuscle.Current); Program("right_terms", c.RightMuscle.Current);
            w.WriteStartArray("native_modules");
            foreach (var module in Parts((Relation)premises[16]).Cast<Relation>())
            {
                var fields = Parts(module); w.WriteStartObject(); w.WriteStartArray("parameters");
                foreach (var parameter in Parts((Relation)fields[0]).Cast<CarrierLocation>()) w.WriteNumberValue(parameter.Ticks);
                w.WriteEndArray(); w.WriteNumber("observed_shared_return", ((CarrierLocation)fields[2]).Ticks);
                w.WriteNumber("support", ((CarrierLocation)fields[1]).Ticks);
                w.WriteNumber("recent_return", ((CarrierLocation)fields[3]).Ticks);
                w.WriteNumber("estimated_bid_premium", ((CarrierLocation)fields[4]).Ticks); w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteString("module_parameter_order", "drive=[seek,feed,depart,bright-threshold]; gradient=[gain,turn-limit]; appetite=[leave-at,resume-below, with zero resume meaning stateless]; recovery=[turn-strength,phase-cycle,phase-on,turn-polarity]");
            w.WriteNumber("eye_maximum", c.LeftEye.Maximum.Ticks);
            w.WriteString("light_calibration", "Raw eye readings use eye_maximum. Light thresholds retain their 0..15 scale; turn = (right-left)*gain*15/eye_maximum before the unchanged turn limit.");
            w.WriteNumber("left_force_0_to_15", c.LeftMuscle.Force.Value);
            w.WriteNumber("right_force_0_to_15", c.RightMuscle.Force.Value);
            var experience = Parts((Relation)premises[15]);
            var prediction = Parts((Relation)experience[0]);
            if (prediction.Length == 8 && prediction[2] is CarrierLocation forecast && prediction[4] is Relation actual && actual.Left is CarrierLocation motion)
            {
                w.WriteNumber("previous_frozen_forecast_speed_squared", forecast.Ticks);
                w.WriteNumber("previous_observed_speed_squared", motion.Ticks);
            }
            var outcome = Parts((Relation)experience[1]);
            if (outcome.Length == 4 && outcome[1] is Relation r && r.Left is CarrierLocation returned)
                w.WriteNumber("most_recent_own_outcome", returned.Ticks);
            w.WriteString("mechanics", "Screen x points right, y points down. Positive left-force minus right-force increases heading, a right turn. Eyes at heading minus/plus35 degrees. Each eye averages one10px ray at its own sampling interval. Light is food; darkness permits mating when full. These are authored hypotheses to evaluate, not additional sensor readings.");
            w.WriteEndObject(); w.WriteStartObject("questions"); w.WriteStartObject("decision");
            w.WriteString("type", "choice"); w.WriteString("instructions", InquiryText.Decode(q.Prompt));
            w.WriteStartObject("criteria");
            for (var i = 0; i < q.Options.Length; i++) w.WriteString("option_" + i, InquiryText.Decode(q.Options[i].Description));
            w.WriteEndObject(); w.WriteEndObject(); w.WriteEndObject(); w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
