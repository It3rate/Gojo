using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private static readonly int[] RaceCourseSizes = [16, 16_384];
    private RaceTrial? raceTrial;
    private int raceMoment;

    private ExplorerPage RacePage()
    {
        var beat = raceTrial?.Timeline[raceMoment];
        var p = new ExplorerPage("Who crossed first?",
            "Two horses share a start signal and a finish. A closed passage can hold a horse back. A separate observer compares the finish with the pre-race favorite.",
            "RaceOrder.md")
        {
            DrawingGuide = (beat?.Label ?? "Follow a race",
                beat is null ? "Run a trial, then select a recorded moment to follow the start, the crossings, and the observer’s answer."
                             : $"A is {RaceState(beat.AState)}. B is {RaceState(beat.BState)}. " +
                                (beat.GoalReached == 0 ? "The goal read is disconnected." : "The goal asks whether both have reached the finish.")),
            Boundary = "The track marks three states. Screen positions give no distances or speeds. A shared finish event and unresolved order have different encodings."
        };
        Choose(p, "Race to try", RaceExperiments.Cases.ToArray(), Mode, value => { Mode = value; ClearRace(); }, true);
        Choose(p, "Private course", RaceCourseSizes.Select(n => $"{n:N0} steps").ToArray(), Variant, value => { Variant = value; ClearRace(); }, true);
        if (raceTrial is { } recorded)
            Choose(p, "Recorded moment", recorded.Timeline.Select((b, i) => $"{i + 1}. {b.Label}").ToArray(),
                raceMoment, value => raceMoment = value, true);
        p.Actions.Add(new("Run race", () =>
        {
            raceTrial = RaceExperiments.Run(Mode, RaceCourseSizes[Variant]);
            raceMoment = raceTrial.Timeline.Length - 1;
            Refresh();
        }));
        p.Actions.Add(new("Reset", () => { ClearRace(); Refresh(); }));

        RaceDrawing(p, raceTrial, beat);
        if (raceTrial is null || beat is null)
        {
            p.Status = "Ready to run";
            p.Result = "The goal is for both horses to finish. Press Run race to record what happens.";
            p.Events.Add("Change the private course size to compare work inside the supplier with reads made by the observer.");
            return p;
        }

        RaceEncoding(p, raceTrial, beat, raceMoment == raceTrial.Timeline.Length - 1);
        p.Status = $"Moment {raceMoment + 1} of {raceTrial.Timeline.Length} · beat {beat.Elapsed} · finish roles {(raceTrial.FirstRoleIsB ? "B, A" : "A, B")}";
        p.Result = $"Finish order · {RaceOrderText(beat.FinishOrder, raceTrial.FirstRoleIsB)}. Did the favorite win outright? {RaceYesNo(beat.FavoriteWon)}.";
        p.Events.Add($"Start signal {RaceYesNo(beat.StartSignal).ToLowerInvariant()} · finish attempts A {RaceYesNo(beat.ARequest).ToLowerInvariant()}, B {RaceYesNo(beat.BRequest).ToLowerInvariant()}");
        p.Events.Add($"Both finished? {RaceYesNo(beat.GoalReached)}. " +
            (beat.Assistance == 0 ? $"Favorite before the start · {BoardOrder(raceTrial)}."
                                  : $"Assistance released? {RaceYesNo(beat.Assistance)}. B’s effective passage is {RaceAccess(beat.BEffectiveAccess)}."));
        p.Events.Add(raceMoment == raceTrial.Timeline.Length - 1
            ? $"Private course begins {string.Join(" → ", raceTrial.CoursePrefix)} · {raceTrial.PrivateSteps:N0} steps in all."
            : $"The supplier owns the {raceTrial.PrivateSteps:N0}-step course. The work counts include the inspections through this moment.");
        return p;
    }

    private void ClearRace() { raceTrial = null; raceMoment = 0; }

    private static string RaceState(int state) => state switch { 0 => "ready", 1 => "racing", 2 => "finished", _ => throw new ArgumentOutOfRangeException(nameof(state)) };
    private static string RaceAccess(byte support) => support switch { 1 => "held", 2 => "open", 3 => "unresolved", 0 => "no reading", _ => throw new ArgumentOutOfRangeException(nameof(support)) };
    private static string RaceYesNo(byte support) => support switch { 1 => "No", 2 => "Yes", 3 => "Unresolved", 0 => "No comparable read", _ => throw new ArgumentOutOfRangeException(nameof(support)) };
    private static string BoardOrder(RaceTrial trial) => (trial.ForecastMatchesRace, trial.FavoriteOrder) switch
    {
        (true, 1) => trial.FirstRoleIsB ? "horse B" : "horse A",
        (true, 2) => trial.FirstRoleIsB ? "horse A" : "horse B",
        (false, _) => "unmatched board", _ => "no single favorite"
    };

    private static string RaceOrderText(byte mask, bool firstRoleIsB) => mask switch
    {
        0 => "no comparable read", 1 => firstRoleIsB ? "B first" : "A first", 2 => firstRoleIsB ? "A first" : "B first", 4 => "same event",
        3 => "A or B first", 5 => firstRoleIsB ? "B first or same event" : "A first or same event",
        6 => firstRoleIsB ? "A first or same event" : "B first or same event", 7 => "order unresolved",
        _ => throw new ArgumentOutOfRangeException(nameof(mask))
    };

    private static void RaceEncoding(ExplorerPage p, RaceTrial trial, RaceBeat beat, bool final)
    {
        const string OrderBasis = "Low bits admit first role earlier, second role earlier, or equal placing. A shared event witnesses equality. Multiple bits retain the possible answers.";
        const string TruthBasis = "Bit 0 admits no. Bit 1 admits yes.";
        p.Bits("Finish order", OrderBasis, [beat.FinishOrder], 3, "finish");
        p.Bits("Favorite won?", TruthBasis, [beat.FavoriteWon], 2, "outcome");
        p.Bits("Favorite order", OrderBasis, [trial.FavoriteOrder], 3, "favorite");
        p.Bits("Landmark order", OrderBasis, [trial.LandmarkOrder], 3, "landmarks");
        p.Bits("Horse A · stored state", "Bits 0–2 hold ready, running and finished. Bits 3–4 hold the last start and finish guard samples.", [beat.AStore], 5, "horseA");
        p.Bits("Horse B · stored state", "Bits 0–2 hold ready, running and finished. Bits 3–4 hold the last start and finish guard samples.", [beat.BStore], 5, "horseB");
        p.Bits("Both horses finished?", TruthBasis, [beat.GoalReached], 2, "goal");
        p.Bits("Assistance released?", "A connected help outlet can release its own activation to open B’s passage on a following beat. " + TruthBasis,
            [beat.Assistance], 2, "assistance");
        if (trial.AssistanceDevice is { } reserve)
        {
            p.Bits("Reserve · goal and obstruction", "Two triples of four-bit read references combine B running with an unmet goal, and B requesting with closed ordinary access.",
                reserve.RefusalAndGoalRows(), 24, "assistance");
            p.Bits("Reserve · eligibility", "One triple of four-bit references requires both reads to hold.",
                reserve.EligibilityRow(), 12, "assistance");
            p.Bits("Reserve · release", "Two-bit endpoint references connect the reserve through its eligibility guard to the released site.",
                reserve.ReleaseRow(), 6, "assistance");
        }
        p.Bits("Start signal", TruthBasis, [beat.StartSignal], 2, "signal");
        p.Bits("A’s passage", TruthBasis, [beat.AAccess], 2, "accessA");
        p.Bits("B’s passage", TruthBasis, [beat.BAccess], 2, "accessB");
        p.Bits("B’s effective passage", "Ordinary permission or a released reserve can open this gate. " + TruthBasis, [beat.BEffectiveAccess], 2, "effectiveB");
        p.Bits("A’s finish attempt", TruthBasis, [beat.ARequest], 2, "requestA");
        p.Bits("B’s finish attempt", TruthBasis, [beat.BRequest], 2, "requestB");
        p.Bits("Landmark member references", "The spatial run stores references to the two landmarks in order.",
            trial.LandmarkBytes, 2, "landmarks");
        if (final && trial.FinishBytes.Length > 0)
        {
            p.Bits("Arrival sites · A and B", "Two one-bit references in physical horse order. A shared reference records a common event.",
                [trial.FinishBytes[0]], 2, "finish");
            if (trial.FinishBytes.Length > 1)
                p.Bits("Arrival precedence", "One-bit references to the earlier and later event on the ordered finish record.",
                    [trial.FinishBytes[1]], 2, "finish");
        }
        p.Rows("Passage connections", "Roles in each row are the source, the controlling read and the destination.",
            3, [[0, 3, 1], [1, 4, 2]], "passages",
            roleKeys: [["passages", "signal", "passages"], ["passages", "passages", "goal"]], actual: trial.PassageRows);
        p.Bits("Passage law", "Stored tuples for a controlled passage.",
            trial.PassageLawBytes, trial.PassageLawBytes.Length * 8, "passagelaw");
        p.Bits("Favorite and finish law", "The consumer reads the two published order masks through this relation.",
            trial.ConsumerLawBytes, trial.ConsumerLawBytes.Length * 8, "consumerlaw");
        p.Rows("Favorite and finish connection", "Forecast, finish and answer refer to public read endpoints.",
            2, [[0, 2, 1]], "consumerlaw", roleKeys: [["favorite", "finish", "outcome"]], actual: trial.ConsumerRow);
    }

    private static void RaceDrawing(ExplorerPage p, RaceTrial? trial, RaceBeat? beat)
    {
        double[] positions = [155, 360, 565];
        p.Label("START", positions[0], 10); p.Label("ON COURSE", positions[1], 10); p.Label("FINISH", positions[2], 10);
        p.Stroke("signal", "A shared start signal gates both entrants.", positions[0], 33, positions[0], 160, beat?.StartSignal != 2);
        p.Stroke("goal", "The goal reads both finish sites.", positions[2], 33, positions[2], 160, beat?.GoalReached != 2);
        Lane("A", 70, beat?.AState, beat?.AAccess);
        Lane("B", 133, beat?.BState, beat?.BEffectiveAccess);
        if (beat is { Assistance: > 0 })
        {
            p.Edge("effectiveB", "", "Released assistance can open B’s next finish passage.", 662, 98, 495, 132,
                active: beat.Assistance == 2, uncertain: beat.Assistance == 3);
            p.Node("assistance", "Help · " + RaceYesNo(beat.Assistance), "A distinct help circuit can open B’s finish passage after a held attempt.",
                662, 70, active: beat.Assistance == 2, uncertain: beat.Assistance == 3);
        }
        p.Label("ONE ORDER READER, TWO SOURCES", 360, 193);
        p.Node("landmarks", "Stroke · " + Order(trial?.LandmarkOrder), "Order of the two bound landmarks on a stroke.", 165, 246,
            active: trial?.LandmarkOrder is 1 or 2 or 4, uncertain: trial?.LandmarkOrder is not (1 or 2 or 4));
        p.Node("finish", "Race · " + Order(beat?.FinishOrder), "Order published by the finish owners for this recorded moment.", 555, 246,
            active: beat?.FinishOrder is 1 or 2 or 4, uncertain: beat?.FinishOrder is not (1 or 2 or 4));
        p.Label("UPPER OBSERVER", 360, 306);
        p.Node("favorite", trial is null ? "Pre-race favorite" : "Favorite · " + BoardOrder(trial), "The board's favorite is named when its race and both entrants match the finish.", 165, 368,
            active: trial?.FavoriteOrder is 1 or 2 or 4, uncertain: trial?.FavoriteOrder is not (1 or 2 or 4));
        bool connected = trial is not null && trial.ConsumerLawBytes.Any(value => value != 0);
        p.Edge("consumerlaw", "finish", "The consumer admits the finish read only with corresponding race and entrant roles.", 555, 272, 439, 396, uncertain: !connected);
        p.Edge("consumerlaw", "board", "The consumer admits the board read only with corresponding race and entrant roles.", 253, 368, 359, 398, uncertain: !connected);
        p.Node("outcome", beat is null ? "Did the favorite win?" : "Favorite won? " + RaceYesNo(beat.FavoriteWon),
            "The comparison law reads favorite and observed order in the bound event and entrant roles.", 399, 423,
            active: beat?.FavoriteWon == 2, uncertain: beat?.FavoriteWon is not (1 or 2));
        p.Label(beat is null ? "The host receives the exposed orders." : $"Host reads {beat.HostReads:N0}  ·  Supplier inspections {beat.SupplierInspections:N0}", 360, 461);

        string Order(byte? mask) => mask.HasValue ? RaceOrderText(mask.Value, trial!.FirstRoleIsB) : "awaiting run";
        void Lane(string name, int y, int? state, byte? access)
        {
            var key = "horse" + name;
            p.Label("Horse " + name, 44, y - 8);
            p.Stroke("passages", $"Horse {name} moves from ready to racing to finished through its own connected passages.",
                positions[0], y, positions[2], y, state is null);
            if (state is int position)
                p.Node(key, name + " · " + RaceState(position), $"State of horse {name} on this beat. Its finish passage is {RaceAccess(access!.Value)}.",
                    positions[position], y, active: position == 2, uncertain: access.Value is 0 or 3);
            else
                foreach (var x in positions) p.Circle(key, "A stored moment places the horse at one of these three states.", x, y, 4, true);
        }
    }
}
