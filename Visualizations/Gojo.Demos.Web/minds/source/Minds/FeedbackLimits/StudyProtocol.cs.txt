using System.Globalization;
using Minds.TwoSpace;

namespace Minds.FeedbackLimits;

// Experimental inputs and host schedules. These records do not own participant meaning.
public sealed record StudyCase(string Id, string Name, string Factor,
    long ResponseDenominator = 2, long StartA = 5, long StartB = 9,
    long ATicks = 10, long BTicks = 10, long BUnits = 2,
    int Rounds = 12, int ApplicationLimit = 24, string Schedule = "captured",
    bool Respond = true, int ReferenceRound = 0, int ReferencePeriod = 0,
    long ReferenceA = 2, long ReferenceB = 2, bool SelectReferences = true);

public enum TapeAction { Publish, Deliver, Drop, Respond, Skip, ObserveReference, RespondToReference }
public sealed record TapeEvent(int Round, TapeAction Action, int Side, int Message = 0, long Value = 0, string Note = "");

public static class StudyProtocol
{
    public const string Version = "feedback-limits-v1";
    public const int Repetitions = 3;
    public static IReadOnlyList<StudyCase> Cases { get; } = [
        new("half", "Half response, longer window", "Reference condition"),
        new("full", "Full response", "Response strength", ResponseDenominator: 1),
        new("quarter", "Quarter response", "Response strength", ResponseDenominator: 4),
        new("reversed", "Reverse the initial disagreement", "Starting values", StartA: 9, StartB: 5),
        new("small-gap", "Start one unit apart", "Starting values", StartB: 6),
        new("equal", "Start in agreement", "Starting values", StartB: 5),
        new("zero", "Start at zero and one", "Starting values", StartA: 0, StartB: 1),
        new("equal-ticks", "Quarter response, equal physical ticks", "Response resolution", ResponseDenominator: 4, BTicks: 5),
        new("fine", "Quarter response, finer ticks", "Response resolution", ResponseDenominator: 4, ATicks: 100, BTicks: 100),
        new("coarse", "Quarter response, coarser ticks", "Response resolution", ResponseDenominator: 4, ATicks: 1, BTicks: 1),
        new("unit-one", "One B unit per A unit", "Calibration with physical resolution held fixed", BTicks: 20, BUnits: 1),
        new("unit-four", "Four B units per A unit", "Calibration with physical resolution held fixed", BTicks: 5, BUnits: 4),
        new("no-response", "Receive without responding", "No-response control", Respond: false),
        new("disconnected", "No deliveries", "Disconnected transport control", Schedule: "disconnected"),
        new("one-way", "Only A reaches B", "Missing messages", Schedule: "one-way"),
        new("missing", "Every other B message is lost", "Missing messages", Schedule: "missing"),
        new("delay", "Full response, one-round delivery delay", "Message timing", ResponseDenominator: 1, Schedule: "delay"),
        new("fresh-a", "Full response, A responds first", "Message timing", ResponseDenominator: 1, Schedule: "fresh-a"),
        new("fresh-b", "Full response, B responds first", "Message timing", ResponseDenominator: 1, Schedule: "fresh-b"),
        new("late", "An old message arrives after a new one", "Receipt refusal", Schedule: "late"),
        new("reference", "Select changed references in round six", "External reference", ReferenceRound: 6, ReferenceA: 4, ReferenceB: 4),
        new("reference-read", "Observe conflicting references only", "Observation without response", ReferenceRound: 6, ReferenceA: 2, ReferenceB: 8, SelectReferences: false),
        new("conflict", "Periodically select conflicting references", "Competing supplied requirements", ReferenceRound: 3, ReferencePeriod: 3, ReferenceA: 2, ReferenceB: 8),
        new("budget", "Quarter response, two applications allowed", "Controller limit", ResponseDenominator: 4, ApplicationLimit: 2)
    ];

    public static string Fixture(StudyCase c)
    {
        if (c.ATicks <= 0 || c.BTicks <= 0 || c.BUnits <= 0 || c.ResponseDenominator < 1 ||
            c.StartA < 0 || c.StartB < 0 || c.Rounds < 1 || c.ApplicationLimit < 0)
            throw new ArgumentOutOfRangeException(nameof(c));
        var text = FeedbackExperiment.FixtureText();
        // These are authored initial premises, before any world is parsed or settled.
        Replace("span($ag,0,10) -> span($aq,0,1)", $"span($ag,0,{c.ATicks}) -> span($aq,0,1)");
        Replace("span($bg,0,10) -> span($bq,0,1)", $"span($bg,0,{c.BTicks}) -> span($bq,0,1)");
        Replace("span($bq,0,2) -> span($aq,0,1)", $"span($bq,0,{c.BUnits}) -> span($aq,0,1)");
        Replace("span($aq,0,1) -> span($bq,0,2)", $"span($aq,0,1) -> span($bq,0,{c.BUnits})");
        Replace("at($bq,4)", $"at($bq,{2 * c.BUnits})");
        Replace("point($ag,50)", $"point($ag,{c.StartA * c.ATicks})");
        Replace("point($bg,180)", $"point($bg,{c.StartB * c.BUnits * c.BTicks})");
        // Controller appears both in the definition and the repeat structure.
        Replace("count($a_steps,6)", $"count($a_steps,{c.ApplicationLimit})", 2);
        Replace("count($b_steps,6)", $"count($b_steps,{c.ApplicationLimit})", 2);
        return text;

        void Replace(string old, string value, int count = 1)
        {
            if (text.Split(old, StringSplitOptions.None).Length - 1 != count)
                throw new InvalidDataException("Fixture parameter surface changed: " + old);
            text = text.Replace(old, value, StringComparison.Ordinal);
        }
    }

    public static IReadOnlyList<TapeEvent> Tape(StudyCase c)
    {
        var result = new List<TapeEvent>();
        for (var round = 1; round <= c.Rounds; round++)
        {
            var reference = c.ReferenceRound > 0 && round >= c.ReferenceRound &&
                (round == c.ReferenceRound || c.ReferencePeriod > 0 && (round - c.ReferenceRound) % c.ReferencePeriod == 0);
            if (reference)
            {
                Add(TapeAction.ObserveReference, 0, value: c.ReferenceA);
                Add(TapeAction.ObserveReference, 1, value: c.ReferenceB * c.BUnits);
                if (c.SelectReferences)
                { Add(TapeAction.RespondToReference, 0); Add(TapeAction.RespondToReference, 1); continue; }
            }
            if (c.Schedule is "fresh-a" or "fresh-b")
            {
                var first = c.Schedule == "fresh-a" ? 0 : 1; var second = 1 - first;
                Add(TapeAction.Publish, second, Message(round, second));
                Add(TapeAction.Deliver, first, Message(round, second)); Respond(first);
                Add(TapeAction.Publish, first, Message(round, first));
                Add(TapeAction.Deliver, second, Message(round, first)); Respond(second);
                continue;
            }
            Add(TapeAction.Publish, 0, Message(round, 0)); Add(TapeAction.Publish, 1, Message(round, 1));
            var delivered = new bool[2];
            foreach (var side in new[] { 0, 1 })
            {
                var source = 1 - side;
                if (c.Schedule == "disconnected" || side == 0 &&
                    (c.Schedule == "one-way" || c.Schedule == "missing" && round % 2 == 0))
                { Add(TapeAction.Drop, side, Message(round, source), note: "Selected transport loss"); continue; }
                if (c.Schedule == "delay" && round == 1) continue;
                var publishedRound = c.Schedule == "delay" ? round - 1 : round;
                Add(TapeAction.Deliver, side, Message(publishedRound, source)); delivered[side] = true;
            }
            if (c.Schedule == "late" && round == 2)
                Add(TapeAction.Deliver, 0, Message(1, 1), note: "Old publication deliberately redelivered after the newer one");
            for (var side = 0; side < 2; side++)
                if (delivered[side]) Respond(side); else Add(TapeAction.Skip, side, note: "No new delivery scheduled");

            void Respond(int side)
            { if (c.Respond) Add(TapeAction.Respond, side); else Add(TapeAction.Skip, side, note: "Declared no-response control"); }
            void Add(TapeAction action, int side, int message = 0, long value = 0, string note = "") =>
                result.Add(new TapeEvent(round, action, side, message, value, note));
        }
        return result;
    }
    public static int Message(int round, int side) => checked(round * 2 + side);
}
