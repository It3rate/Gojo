using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Structures;
using Minds.TwoSpace;
using static Minds.TwoSpace.Graph;

namespace Minds.FeedbackLimits;

// Immutable diagnostic projections. Participants never receive these records.
public sealed record StudyFrame(int Index, int Round, string Action, string Side, int Message,
    string Note, bool? Accepted, IReadOnlyDictionary<string, string> Values);
public sealed record StudyTrace(IReadOnlyList<StudyFrame> Events, IReadOnlyList<StudyFrame> Rounds);
public sealed record GojoStudyRun(StudyTrace Trace, Relation Timeline);

public static class GojoStudy
{
    public static readonly string[] MeasureNames = ["a", "b", "mean", "gap", "da", "db", "movement", "meanChange", "aReferenceGap", "bReferenceGap"];

    public static GojoStudyRun Run(StudyCase condition, IReadOnlyList<TapeEvent> tape)
    {
        var roots = GojoSyntax.Parse(StudyProtocol.Fixture(condition)).Roots;
        var spaces = new[] { new Space(roots[0], roots[2], roots[4], roots[5]), new Space(roots[1], roots[3], roots[4], roots[5]) };
        var observer = new PairObserver(spaces[0].Output);
        var messages = new Dictionary<int, Relation>();
        var pending = new HashSet<int>();
        var receipts = new Relation?[2];
        var events = new List<StudyFrame>(); var rounds = new List<StudyFrame>(); var steps = new List<Relation>();
        var clock = new Carrier();
        Relation? priorEvent = null, priorRound = null, first = null;
        var publications = 0; var deliveryCalls = 0; var admissions = 0; var drops = 0; var applicationCalls = 0;
        Capture(null, G(), null); Complete(0);
        for (var index = 0; index < tape.Count; index++)
        {
            var e = tape[index]; var space = spaces[e.Side]; Relation evidence; bool? accepted = null;
            switch (e.Action)
            {
                case TapeAction.Publish:
                    evidence = space.Publish(e.Message); messages.Add(e.Message, evidence); pending.Add(e.Message); publications++; break;
                case TapeAction.Deliver:
                    accepted = space.TryReceive(messages[e.Message], index + 1000, out evidence);
                    deliveryCalls++; pending.Remove(e.Message);
                    if (accepted.Value) { receipts[e.Side] = evidence; admissions++; }
                    break;
                case TapeAction.Drop:
                    evidence = messages[e.Message]; pending.Remove(e.Message); drops++; break;
                case TapeAction.Respond:
                    var receipt = receipts[e.Side] ?? throw new InvalidDataException("Tape requested a response before any receipt.");
                    var plan = space.Plan(receipt, condition.ResponseDenominator);
                    accepted = space.Apply(plan, receipt); applicationCalls++;
                    evidence = space.LastApplication!; break;
                case TapeAction.ObserveReference:
                    evidence = space.ObserveReference(e.Value, index + 1000); break;
                case TapeAction.RespondToReference:
                    accepted = space.RespondToReference(); applicationCalls++; evidence = space.LastApplication!; break;
                case TapeAction.Skip:
                    evidence = G(space.State, space.Inbox); break;
                default: throw new InvalidDataException("Unknown host event.");
            }
            Capture(e, evidence, accepted);
            if (index == tape.Count - 1 || tape[index + 1].Round != e.Round) Complete(e.Round);
        }
        return new GojoStudyRun(new StudyTrace(events, rounds), TimeStepSequence.Create(new Carrier(), clock, first!, steps.ToArray()));

        void Capture(TapeEvent? e, Relation evidence, bool? accepted)
        {
            var observation = observer.Observe(spaces[0], spaces[1], priorEvent);
            var retained = G(evidence, spaces[0].History, spaces[1].History,
                spaces[0].Inbox, spaces[1].Inbox, spaces[0].Reference, spaces[1].Reference,
                G(pending.Order().Select(id => (IElement)messages[id]).ToArray()));
            if (priorEvent is not null)
                steps.Add(TimeStepSequence.Step(new Carrier(), clock, events.Count - 1, events.Count,
                    priorEvent, retained, observation, G(spaces.Select(s => (IElement)G(
                        s.LastPlan is null ? G() : F(s.LastPlan)[3], new CarrierLocation(s.Actuator, s.Actuator.Tension))).ToArray())));
            first ??= observation;
            events.Add(Frame(e, observation, accepted)); priorEvent = observation;
        }

        void Complete(int round)
        {
            var observation = observer.Observe(spaces[0], spaces[1], priorRound);
            rounds.Add(Frame(null, observation, null) with { Index = events.Count - 1, Round = round, Action = "Round" });
            priorRound = observation;
        }

        StudyFrame Frame(TapeEvent? e, Relation observation, bool? accepted)
        {
            var values = MeasureNames.Select((name, i) => (name, value: Number(PairObserver.Scalar(R(F(observation)[i])))))
                .ToDictionary(p => p.name, p => p.value);
            for (var i = 0; i < 2; i++)
            {
                var s = spaces[i]; var prefix = i == 0 ? "a." : "b.";
                values[prefix + "actuator"] = s.Actuator.Value.ToString();
                values[prefix + "tension"] = s.Actuator.Tension.ToString();
                values[prefix + "applications"] = s.Attempts.ToString();
                values[prefix + "remaining"] = (condition.ApplicationLimit - s.Attempts).ToString();
                values[prefix + "reference"] = Number(F(s.Reference)[2]);
                values[prefix + "published"] = s.LastPublishedMessage is null ? "none" : Number(F(F(s.LastPublishedMessage)[0])[2]);
                var inbox = F(s.Inbox);
                values[prefix + "received"] = inbox[1] is Placeholder ? "none" : Number(F(F(inbox[1])[2])[2]);
                values[prefix + "latestSequence"] = Sequence(inbox[1]);
                values[prefix + "consumedSequence"] = Sequence(inbox[2]);
                var p = s.LastPlan is null ? null : F(s.LastPlan);
                values[prefix + "requested"] = p is null ? "none" : Number(F(p[2])[0]);
                values[prefix + "whole"] = p is null ? "none" : Number(F(p[2])[1]);
                values[prefix + "fraction"] = p is null ? "none" : Number(F(p[3])[0]);
            }
            values["publications"] = publications.ToString(); values["deliveryCalls"] = deliveryCalls.ToString();
            values["admissions"] = admissions.ToString(); values["drops"] = drops.ToString();
            values["applicationCalls"] = applicationCalls.ToString(); values["pending"] = pending.Count.ToString();
            return new StudyFrame(events.Count, e?.Round ?? 0, e?.Action.ToString() ?? "Initial", e is null ? "" : e.Side == 0 ? "A" : "B",
                e?.Message ?? 0, e?.Note ?? "", accepted, values);
        }
    }

    private static string Sequence(IElement receipt) => receipt is Placeholder ? "none" :
        Number(F(F(F(receipt)[0])[0])[1]);
    private static string Number(IElement value)
    {
        Require(ProjectionResidue.TryRead(value, out _, out var n, out var d), "exact study read");
        return d == 1 ? n.ToString() : $"{n}/{d}";
    }
}
