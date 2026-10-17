using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Structures;
using static Minds.TwoSpace.Graph;

namespace Minds.TwoSpace;

// These records are host schedules and observation indexes, not participant state.
public sealed record FeedbackCase(string Id, string Name, string Description, long ResponseDenominator,
    bool FreshPublication = false, bool UseReference = false);
public sealed record ObservedEvent(string Description, Relation Observation, Relation Evidence,
    bool? Applied, int AAttempts, int BAttempts, Relation? PublishedA, Relation? PublishedB);
public sealed record ObservedRound(string Description, Relation Observation, int EventIndex);
public sealed record FeedbackRun(FeedbackCase Case, IReadOnlyList<ObservedEvent> Events,
    IReadOnlyList<ObservedRound> Rounds, Relation Timeline);

public static class FeedbackExperiment
{
    public static IReadOnlyList<FeedbackCase> Cases { get; } = [
        new("half", "Half response", "Both publish before either changes; each closes half its received mismatch.", 2),
        new("full", "Full response", "The same captured publications, with each taking the entire correction.", 1),
        new("quarter", "Quarter response", "Smaller responses meet whole-actuator-tick limits; inspect any remainder.", 4),
        new("fresh", "Full response, fresh message", "A responds to B, then B receives A's newly changed view.", 1, FreshPublication: true),
        new("reference", "Agreement, then local evidence", "After two peer rounds, both use their already supplied reference observation of two.", 2, UseReference: true)
    ];

    public static string FixtureText() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "two-space-feedback.gojo"));

    public static FeedbackRun Run(FeedbackCase condition, string? fixture = null)
    {
        var roots = GojoSyntax.Parse(fixture ?? FixtureText()).Roots;
        var a = new Space(roots[0], roots[2], roots[4], roots[5]);
        var b = new Space(roots[1], roots[3], roots[4], roots[5]);
        var observer = new PairObserver(a.Output);
        var events = new List<ObservedEvent>(); var rounds = new List<ObservedRound>();
        var steps = new List<Relation>(); var clock = new Carrier();
        Relation? previousEvent = null; Relation? previousRound = null;
        Capture("Initial views", G());
        Complete("Start");

        for (var round = 1; round <= 6; round++)
        {
            if (condition.UseReference && round == 3)
            {
                var appliedA = a.RespondToReference(); Capture("A uses its local reference", a.LastApplication!, appliedA);
                var appliedB = b.RespondToReference(); Capture("B uses its local reference", b.LastApplication!, appliedB);
                Complete("Use references");
                continue;
            }

            if (condition.FreshPublication)
            {
                var bm = b.Publish(100 + round); Capture("B publishes", bm);
                var ar = a.Receive(bm, 20 + round); var ap = a.Plan(ar, condition.ResponseDenominator);
                Capture("A receives and plans", G(ar, ap));
                var acceptedA = a.Apply(ap, ar); Capture("A applies its response", a.LastApplication!, acceptedA);
                var am = a.Publish(round); Capture("A publishes its current view", am);
                var br = b.Receive(am, 120 + round); var bp = b.Plan(br, condition.ResponseDenominator);
                Capture("B receives and plans", G(br, bp));
                var acceptedB = b.Apply(bp, br); Capture("B applies its response", b.LastApplication!, acceptedB);
            }
            else
            {
                var am = a.Publish(round); var bm = b.Publish(100 + round);
                Capture("Both publish their current views", G(am, bm));
                var ar = a.Receive(bm, 20 + round); var br = b.Receive(am, 120 + round);
                var ap = a.Plan(ar, condition.ResponseDenominator); var bp = b.Plan(br, condition.ResponseDenominator);
                Capture("Both receive and plan from those messages", G(ar, br, ap, bp));
                var acceptedA = a.Apply(ap, ar); Capture("A applies its response", a.LastApplication!, acceptedA);
                var acceptedB = b.Apply(bp, br); Capture("B applies its response", b.LastApplication!, acceptedB);
            }
            Complete($"Round {round}");
        }

        var timeline = TimeStepSequence.Create(new Carrier(), clock, events[0].Observation, steps.ToArray());
        return new FeedbackRun(condition, events, rounds, timeline);

        void Capture(string description, Relation evidence, bool? applied = null)
        {
            var observation = observer.Observe(a, b, previousEvent);
            // Include current plan/remainder, copied tension, and histories as structural evidence.
            var retained = G(evidence, a.LastPlan ?? G(), b.LastPlan ?? G(),
                new CarrierLocation(a.Actuator, a.Actuator.Tension), new CarrierLocation(b.Actuator, b.Actuator.Tension),
                a.History, b.History);
            if (previousEvent is not null)
                steps.Add(TimeStepSequence.Step(new Carrier(), clock, events.Count - 1, events.Count,
                    previousEvent, retained, observation, G(
                        a.LastPlan is null ? G() : F(a.LastPlan)[3],
                        b.LastPlan is null ? G() : F(b.LastPlan)[3],
                        new CarrierLocation(a.Actuator, a.Actuator.Tension),
                        new CarrierLocation(b.Actuator, b.Actuator.Tension))));
            events.Add(new ObservedEvent(description, observation, retained, applied, a.Attempts, b.Attempts,
                a.LastPublishedMessage, b.LastPublishedMessage));
            previousEvent = observation;
        }

        void Complete(string description)
        {
            var observation = observer.Observe(a, b, previousRound);
            rounds.Add(new ObservedRound(description, observation, events.Count - 1));
            previousRound = observation;
        }
    }
}
