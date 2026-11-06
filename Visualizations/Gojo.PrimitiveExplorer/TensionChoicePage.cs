using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private static readonly string[] NeedNames = ["Purse", "Standing", "Pickup"];
    private const string PriorityChoice = "Let a due pickup take priority";
    private TensionChoiceTrial? choiceTrial;
    private int choiceTurn;

    private void ClearTensionChoice() { choiceTrial = null; choiceTurn = 0; }

    private ExplorerPage TensionChoicePage()
    {
        var beat = choiceTrial?.Timeline[choiceTurn];
        var page = new ExplorerPage("Which action helps?",
            "A jockey has bills to pay, standing to earn and children to pick up. Three outlets share each turn. Follow which one earns the next try.",
            "TensionChoice.md")
        {
            DrawingGuide = beat is null
                ? ("Try the three outlets", "Run a trial, then move through its recorded choices. Each outlet predicts relief for the need in focus.")
                : (ChoiceTitle(beat), ChoiceGuide(beat)),
            Boundary = "Each need has its own basis. The apparatus supplies access and opportunities. Only the need in focus ranks the available outlets."
        };
        Choose(page, "Trial", TensionChoiceExperiments.Names.Append(PriorityChoice).ToArray(), Mode,
            index => { Mode = index; ClearTensionChoice(); }, true);
        if (choiceTrial is { } recorded)
        {
            Choose(page, "Recorded turn", recorded.Timeline.Select(frame =>
                $"{frame.Turn} · {FocusName(frame)} · {ActionText(frame.Action)}").ToArray(),
                choiceTurn, index => choiceTurn = index, true);
        }
        page.Actions.Add(new("Run trial", () =>
        {
            choiceTrial = Mode == TensionChoiceExperiments.Names.Length
                ? NeedPriority.RunExample() : TensionChoiceExperiments.Run(Mode);
            choiceTurn = choiceTrial.Timeline.Length - 1;
            Refresh();
        }));
        page.Actions.Add(new("Reset", () => { ClearTensionChoice(); Refresh(); }));
        DrawChoices(page, beat);
        if (choiceTrial is null || beat is null)
        {
            page.Status = "Ready to run";
            page.Result = "Run the trial to see which outlet acts and what changes.";
            return page;
        }

        EncodeChoice(page, choiceTrial, beat);
        page.Status = $"Turn {beat.Turn} · {FocusName(beat)}";
        page.Result = ChoiceSummary(beat);
        for (var action = 0; action < 3; action++)
        {
            var attempts = choiceTrial.Timeline.Take(choiceTurn + 1).Count(previous => previous.Action == action);
            string retained = beat.Focus >= 0 ? $" · retained {FocusName(beat).ToLowerInvariant()} relief {Relief(beat.Samples[action][beat.Focus])}" : "";
            page.Events.Add($"Outlet {Outlet(action)} · {attempts} selections so far{retained}");
        }
        return page;
    }

    private static string Outlet(int action) => ((char)('A' + action)).ToString();
    private static string FocusName(TensionChoiceBeat beat) => beat.Focus >= 0 ? NeedNames[beat.Focus] :
        beat.FocusSupport switch { 0 => "No focus read", 1 => "No need in focus", _ => "Focus unresolved" };
    private static string ActionText(int action) => action < 0 ? "Hold" : $"Outlet {Outlet(action)}";
    private static string Relief(int? amount) => amount switch
    {
        null => "unknown", > 0 => "+" + amount, _ => amount.ToString()!
    };

    private static string ChoiceTitle(TensionChoiceBeat b) => b.Action < 0
        ? "The opportunity waits" : $"Outlet {Outlet(b.Action)} gets the opportunity";

    private static string ChoiceGuide(TensionChoiceBeat b)
    {
        if (b.Event.Length > 0) return b.Event;
        if (b.Focus < 0) return FocusName(b) + ".";
        var need = FocusName(b).ToLowerInvariant();
        if (b.OfferMask == 0) return $"The {need} need is in focus. None of the three outlets offers a connection to it.";
        if ((b.AccessMask & b.OfferMask) == 0) return $"All outlets offering {need} relief are closed.";
        var untested = Enumerable.Range(0, 3).Any(action => b.Forecasts[action] is null &&
            (b.AccessMask & b.OfferMask & (1 << action)) != 0);
        var rule = untested ? "Open, untested offers have priority" : $"Higher observed {need} relief has priority";
        return $"{rule}. Equal ranks are considered starting at outlet {Outlet(b.Phase)}.";
    }

    private static string ChoiceSummary(TensionChoiceBeat b)
    {
        if (b.Focus < 0) return FocusName(b) + ". The opportunity waits.";
        var need = NeedNames[b.Focus].ToLowerInvariant();
        if (b.Action < 0) return $"The {need} need has {b.After[b.Focus]} outstanding.";
        int change = b.Before[b.Focus] - b.After[b.Focus];
        var blocked = b.BlockedEffect[b.Focus];
        return $"Expected {need} relief {Relief(b.Prediction[b.Focus])}. Measured {Relief(change)}. " +
            $"Feedback to the chooser {Relief(b.Observed[b.Focus])}." +
            (blocked == 0 ? "" : $" The bank could not accept {Relief(blocked)} further {need} change.");
    }

    private static void DrawChoices(ExplorerPage p, TensionChoiceBeat? b)
    {
        p.Label("ONE OPPORTUNITY", 360, 7);
        p.Node("opportunity", b is null ? "Ready" : ActionText(b.Action),
            "The stored opportunity occupies the idle site or one selected outlet.", 360, 58,
            active: b?.Action >= 0);
        for (var action = 0; action < 3; action++)
        {
            int x = 118 + action * 242;
            string key = "outlet" + action;
            bool open = b is not null && (b.AccessMask & (1 << action)) != 0;
            bool offers = b is not null && (b.OfferMask & (1 << action)) != 0;
            bool selected = b?.Action == action;
            p.Edge(key, "", "An open passage offers this outlet the opportunity.", 360, 82, x, 135,
                active: selected, uncertain: !open);
            p.Node(key, $"Outlet {Outlet(action)}", b is null ? "Access is read when the trial runs." : $"Access {(open ? "open" : "closed")}.",
                x, 159, active: selected, uncertain: !open);
            if (b is null) continue;
            p.Label((open ? "Open" : "Closed") + (b.Focus < 0 ? "" : offers ? " · offers relief" : " · no offer"), x, 190);
            p.Label($"Forecast {Relief(b.Forecasts[action])}", x, 212);
        }
        p.Label("NEED", 85, 264);
        p.Label("BEFORE", 214, 264);
        p.Label("FORECAST", 317, 264);
        p.Label("ACTUAL RELIEF", 430, 264);
        p.Label("FEEDBACK", 541, 264);
        p.Label("AFTER", 641, 264);
        for (var need = 0; need < NeedNames.Length; need++)
        {
            var y = 312 + need * 59;
            var detail = b is null ? "Outstanding need in its own local basis." :
                $"{NeedNames[need]} went from {b.Before[need]} to {b.After[need]}. " +
                $"Signed service held at the bank boundary: {Relief(b.BlockedEffect[need])}.";
            p.Node("need" + need, NeedNames[need], detail, 85, y,
                active: b?.Focus == need);
            if (b is null) continue;
            p.Label(b.Before[need].ToString(), 214, y - 7);
            p.Label(b.Action < 0 ? "—" : Relief(b.Prediction[need]), 317, y - 7);
            p.Label(Relief(b.Before[need] - b.After[need]), 430, y - 7);
            p.Label(b.Action < 0 ? "—" : Relief(b.Observed[need]), 541, y - 7);
            p.Label(b.After[need].ToString(), 641, y - 7);
        }
        if (b is not null && b.BlockedEffect.Any(effect => effect != 0))
            p.Label("Held at a bank boundary · " + string.Join(" · ", NeedNames.Select((name,i) =>
                (name, amount:b.BlockedEffect[i])).Where(item => item.amount != 0)
                .Select(item => $"{item.name} {Relief(item.amount)}")), 360, 462);
    }

    private static void EncodeChoice(ExplorerPage page, TensionChoiceTrial trial, TensionChoiceBeat beat)
    {
        page.Bits("One opportunity", "Bit 0 is idle. Bits 1 through 3 are the three action sites.",
            [beat.OpportunityBits], 4, "opportunity");
        BitMark[] outletMarks = Enumerable.Range(0, 3).Select(i =>
            new BitMark(i, 1, ["outlet" + i], "Outlet " + Outlet(i))).ToArray();
        page.Bits("Open outlets", "Bits 0 through 2 report access to A, B and C.", [beat.AccessMask], 3, "open", outletMarks);
        page.Bits("Advertised outlets", "Each bit identifies an outlet offering a service in the selected need's basis.",
            [beat.OfferMask], 3, "offers", outletMarks);
        page.Bits("Outlets admitted to selection", "These guards can accept the opportunity in the current trial.",
            [beat.SelectionMask], 3, "selection", outletMarks);
        page.Bits("Feedback connections", "An open return path can report a change in that need to the chooser.",
            [beat.FeedbackMask], 3, "feedback", Enumerable.Range(0, NeedNames.Length)
                .Select(i => new BitMark(i, 1, ["need" + i], NeedNames[i])).ToArray());
        var sampleMarks = (from action in Enumerable.Range(0, 3)
                           from need in Enumerable.Range(0, 3)
                           select new BitMark(3 * (3 * action + need), 3, ["outlet" + action, "need" + need],
                               $"Outlet {Outlet(action)} · {NeedNames[need]}")).ToArray();
        page.Bits("Learned relief", "After this turn, 0 means unseen, 1 means harm, 2 means unchanged, and 3 or 4 means one or two steps of relief.",
            beat.MemoryBytes, 27, "memory", sampleMarks);
        if (trial.PriorityRows.Length > 0)
        {
            page.Bits("Focus read", "The bits admit waiting, purse, standing and pickup, in that order.",
                [beat.FocusSupport], 4, "focus");
            page.Bits("Pickup due", "Bit 0 admits no. Bit 1 admits yes.",
                [beat.PickupDueSupport], 2, "due");
            string[] priorityRoles = ["Standing", "Purse", "Pickup"];
            for (var row = 0; row < priorityRoles.Length; row++)
                page.Bits("Focus connection · " + priorityRoles[row], "Five-bit references connect eligibility, fallback and result.",
                    trial.PriorityRows.AsSpan(row * 2, 2).ToArray(), 15, "focus");
        }
        page.Bits("Choice passages", "Each stored row connects idle, a guard and its action site.",
            trial.ChoiceRows, 27, "opportunity");
        page.Bits("Outcome passages", "Selected service connections in the apparatus. Each row binds an outlet to a change in one need.",
            trial.ReliefRows, trial.ReliefRows.Length * 8, "outcomes");
        if (trial.FeedbackRows.Length > 0)
            page.Bits("Feedback passages", "Four-bit references bind an active outlet, a need bank and its evidence site.",
                trial.FeedbackRows, 9 * 3 * 4, "feedback");
    }
}
