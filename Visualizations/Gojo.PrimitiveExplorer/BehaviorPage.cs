using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private ConstructionBehavior? behavior;
    private int behaviorCase = -1;
    private byte[]? behaviorRouteBefore, behaviorReachBefore;

    private ConstructionBehavior BehaviorRuntime()
    {
        if (behavior is null || behaviorCase != Mode)
        {
            behavior = new(Mode);
            behavior.SetRouteObservation(Support(Control));
            behaviorCase = Mode;
            behaviorRouteBefore = behaviorReachBefore = null;
        }
        return behavior;
    }

    private ExplorerPage BehaviorPage()
    {
        var runtime = BehaviorRuntime();
        var route = Variant == 0;
        var p = new ExplorerPage("Let the structure respond",
            "The builder is gone; the connected artifact remains. Advance a request through its uses, or try one outward reach adjustment. Watch actual bits separately from possible outcomes.", "ConstructionBehavior.md")
        {
            DrawingGuide = (runtime.Case.Title, runtime.Case.Description)
        };
        Choose(p, "Case", ConstructionBehavior.Cases.Select(item => item.Title).ToArray(), Mode,
            value => { Mode = value; behavior = null; }, true);
        p.Choices.Add(new("Trial", ["Follow routes", "Adjust a reach"], Variant, value => { Variant = value; Refresh(); }));
        if (route)
            p.Choices.Add(new("Selector read", ["First arm · 0", "Second arm · 1", "Uncertain · {0,1}", "Missing observation"], Control,
                value => { Control = value; runtime.SetRouteObservation(Support(value)); Refresh(); }));
        else
            p.Choices.Add(new("Response read", ["Known control", "Concealed control"], A, value => { A = value; Refresh(); }));
        p.Actions.Add(new(route ? "Advance one beat" : "Offer / retry request", () =>
        {
            if (route)
            {
                behaviorRouteBefore = runtime.Walk.ValueBytes();
                runtime.Walk.Pulse();
                Steps = runtime.Walk.Trace.Count;
            }
            else
            {
                behaviorReachBefore = runtime.Adjustment.StoreBytes();
                runtime.Adjustment.Push(A == 1);
            }
            Refresh();
        }));
        p.Actions.Add(new("Reset both trials", () => { behavior = null; Steps = 0; Refresh(); }));

        ConstructionDrawing(p, runtime.Example, false, 0);
        // The same fixed placement is used in both trials. Lower annotations
        // explain executable state, replacing the gallery's group legend.
        p.Drawing.RemoveAll(item => item.Y >= 400);
        if (route) BehaviorRoute(p, runtime, behaviorRouteBefore, Support(Control));
        else BehaviorReach(p, runtime, behaviorReachBefore, A == 1);
        BehaviorFieldCorrespondences(p, runtime.Example.Surface);
        ConstructionBits(p, runtime.Example.Surface);
        p.Boundary = "Each trial owns one supplied request and explicit passage laws. The retained artifact has no construction builder or geometric basis. Reach is ordinal: the fixed picture shows affected references, not a newly solved length, angle or deformation.";
        return p;
    }

    private static string BehaviorState(FamilyWalk walk, int? state)
    {
        if (state is null) return "no active site";
        if (state == walk.InputState) return "input";
        for (var site = 0; site < walk.Family.Template.SiteCount; site++)
            if (state == walk.StateForSite(site)) return "s" + site;
        return "local " + state;
    }

    private static string BehaviorStateKey(FamilyWalk walk, int state)
    {
        if (state == walk.InputState) return "request";
        if (state == walk.GuardState) return "control";
        for (var site = 0; site < walk.Family.Template.SiteCount; site++)
            if (state == walk.StateForSite(site)) return "site" + site;
        return "runtime" + state;
    }

    private static void BehaviorStore(ExplorerPage p, string title, string note, byte[] bytes, string[] keys)
        => p.Bits(title, note, bytes, keys.Length, "runtime",
            keys.Select((key, bit) => new BitMark(bit, 1, ["runtime", key], $"Stored slot {bit}: {key}. Zero is known absence; it is not uncertainty.")).ToArray());

    private static void BehaviorRoute(ExplorerPage p, ConstructionBehavior runtime, byte[]? before, byte selectorSupport)
    {
        var walk = runtime.Walk;
        var predicted = walk.Predict();
        var keys = Enumerable.Range(0, walk.StateCount + walk.GuardCount).Select(state => BehaviorStateKey(walk, state)).ToArray();
        const string storeNote = "One V bit per runtime slot: input, guard mirror, then family sites. A 1 on input or a family site is the single request. The guard mirror is separate evidence, not another request.";
        BehaviorStore(p, "Runtime · current route state", storeNote, walk.ValueBytes(), keys);
        if (before is not null) BehaviorStore(p, "Witness · before last beat", "Retained observation of the same slots before the last attempt. This snapshot does not drive execution.", before, keys);
        p.Bits("Read · supplied selector", "Two-bit support mask: 01 admits known 0; 10 admits known 1; 11 admits both; 00 means no observation. Only passages whose result depends on this control need to consult it.", [selectorSupport], 2, "control");
        p.Rows("Prediction · destinations if resolved", "Derived destinations once control resolves, not the actual result of an unresolved beat. Uncertain control keeps alternatives while execution holds; conflicting departures supply no agreed destination.", walk.ReferenceBits,
            predicted.Targets.Select(state => new[] { state }).ToArray(), "targets",
            roleKeys: predicted.Targets.Select(state => new[] { BehaviorStateKey(walk, state) }).ToArray());
        p.Rows("Runtime · family site bindings", "[Family site, runtime state]. This makes the active bit's physical correspondence explicit.", walk.ReferenceBits,
            Enumerable.Range(0, walk.Family.Template.SiteCount).Select(walk.SiteRow).ToArray(), "sitebindings",
            roleKeys: Enumerable.Range(0, walk.Family.Template.SiteCount).Select(site => new[] { "site" + site, "site" + site }).ToArray(), actual: walk.SiteBytes());
        var input = walk.InputRow();
        p.Rows("Runtime · input passage", "[Input, guard, first site, law table, row]. The input request is handed off, never copied into the drawing.", walk.ReferenceBits,
            [input], "inputpassage", roleKeys: [["request", "control", "site" + walk.StartSite, "law" + input[3], "inputpassage"]], actual: walk.InputBytes());
        var rows = Enumerable.Range(0, walk.RouteCount).Select(walk.RouteRow).ToArray();
        string UseKey(int signed) => "use" + Enumerable.Range(0, walk.Uses.UseCount).First(i => walk.Uses.SignedUse(i) == signed);
        p.Rows("Runtime · route correspondence", "[Signed use, from, guard, to, law table, row]. Distinct uses compile to passages; repeated uses of the same signed link do not create duplicate routes.", walk.ReferenceBits,
            rows, "routes", roleKeys: rows.Select(row => new[] { UseKey(row[0]), BehaviorStateKey(walk, row[1]), "control", BehaviorStateKey(walk, row[3]), "law" + row[4], UseKey(row[0]) }).ToArray(),
            actual: walk.RouteBytes(), rowKeys: rows.Select(row => UseKey(row[0])).ToArray());
        for (var table = 0; table < walk.TableCount; table++)
        {
            var passageRows = rows.Where(row => row[4] == table).Select(row => (Index: row[5], Fields: row[1..4])).ToList();
            if (input[3] == table) passageRows.Add((input[4], input[..3]));
            var sorted = passageRows.OrderBy(row => row.Index).Select(row => row.Fields).ToArray();
            p.Rows($"Runtime · passages {table}", "[From, guard, to]. These are the actual executable connection rows in this law table.", walk.ReferenceBits,
                sorted, "law" + table, roleKeys: sorted.Select(row => row.Select(state => BehaviorStateKey(walk, state)).ToArray()).ToArray(), actual: walk.PackedRows(table));
            BehaviorLaw(p, "Runtime · transition law " + table, walk.LawBytes(table), "law" + table);
        }
        var active = BehaviorState(walk, predicted.Before);
        var targetText = predicted.Targets.Length == 0 ? "none admitted" : string.Join(" or ", predicted.Targets.Select(state => BehaviorState(walk, state)));
        var verdict = predicted.Conflict ? "Competing departures: the request holds." : predicted.Missing ? "The needed observation is missing: the request holds." :
            !predicted.Resolved ? "Several outcomes remain: the request holds until the selector resolves." : "The next beat has one resolved outcome.";
        p.Result = $"Actual request: {active}. Potential destinations: {targetText}. {verdict}";
        p.Status = $"Route trial · {walk.Trace.Count} attempted beats · {walk.OccupiedStateCount} occupied request slot";
        p.Node("request", walk.ReadStateSupport(walk.InputState) == 2 ? "Input · present" : "Input · empty",
            "The single supplied request starts here. Once it enters a family site, this slot becomes empty.", 115, 418, walk.ReadStateSupport(walk.InputState) == 2);
        p.Node("control", "Selector · " + (selectorSupport switch { 1 => "0", 2 => "1", 3 => "{0,1}", _ => "missing" }),
            "A known 0 and a known 1 can select opposite arms. {0,1} preserves both possible reads. The local guard mirror may remain unchanged while the published read is uncertain.", 355, 418,
            uncertain: selectorSupport is 0 or 3);
        p.Label("Orange = actual request · dashed = possible destinations once resolved", 355, 457);
        var siteItems = p.Drawing.Where(item => item.Kind == "circle" && item.Key.StartsWith("site")).ToArray();
        foreach (var siteItem in siteItems)
        {
            var site = int.Parse(siteItem.Key[4..]); var state = walk.StateForSite(site);
            if (state == predicted.Before)
                p.Drawing.Add(new("circle", siteItem.Key, "", $"The actual request is at family site {site}.", siteItem.X, siteItem.Y, 13)
                    { Color = "#D66B2A", LineWidth = 4 });
            if (predicted.Targets.Contains(state))
                p.Drawing.Add(new("circle", siteItem.Key, "", $"Site {site} is a possible destination once control resolves. An unresolved beat still holds the actual request at its current site.", siteItem.X, siteItem.Y, 20, Uncertain: true)
                    { Color = "#467B9A", LineWidth = 2 });
        }
        p.Events.Add("One beat attempts one passage. A natural cycle keeps circulating; this page supplies no hidden stop condition.");
        if (walk.Trace.LastOrDefault() is { } last)
        {
            var step = last.Step;
            p.Events.Add($"Last beat: {BehaviorState(walk, step.Before)} → {BehaviorState(walk, step.After)}; " +
                (step.Moved ? "request transferred." : step.Committed ? "resolved hold; no transfer." : "unresolved or conflicting; no stored bit changed."));
        }
        p.Events.Add("The source template, uses and participation bits are retained below. Routing has its own activation store; following a link does not consume that link's value.");
    }

    private static void BehaviorLaw(ExplorerPage p, string title, byte[] bytes, string key)
        => p.Bits(title, "Eight bytes encode the 64 allowed before/after pairs of [source, control, target]. Bit before + 8×after admits a complete transition. This is the executable rule, not a stored amount.", bytes, 64, key);

    private static void BehaviorReach(ExplorerPage p, ConstructionBehavior runtime, byte[]? before, bool hidden)
    {
        var adjustment = runtime.Adjustment;
        var prediction = adjustment.Predict(hidden);
        var fields = adjustment.FieldRows();
        var keys = Enumerable.Range(0, adjustment.LogicalSiteCount).Select(slot => slot switch
        {
            0 => "reserve", 1 => "waiting", 2 => "launch", 3 => "free", 4 => "locked", _ => "field" + (slot - 5)
        }).ToArray();
        BehaviorStore(p, "Runtime · current adjustment state", "Slots: reserve, waiting request, launch guard, free guard, locked guard, then one dial per value field. Dial 0 selects rest; dial 1 selects farther. These are local ordinal positions, not lengths.", adjustment.StoreBytes(), keys);
        if (before is not null) BehaviorStore(p, "Witness · before last request", "The same actual slots before the last offer/retry. Hidden controls remain visible here to the owner inspector; the predictor receives only its chosen read.", before, keys);
        p.Rows("Runtime · field adjustment bindings", "[Field, dial, rest position, farther position]. Every field has its own ordered rail; equal position labels on independent rails do not prove equal extent.", adjustment.ReferenceBits,
            fields, "adjustments", roleKeys: fields.Select(row => Enumerable.Repeat("field" + row[0], 4).ToArray()).ToArray(), actual: adjustment.FieldBytes());
        for (var railField = 0; railField < adjustment.FieldCount; railField++)
        {
            var rail = adjustment.Rail(railField);
            p.Bits($"Runtime · ordinal rail {railField}", "Ordered local sites [zero, rest, farther]. Position supplies order without specifying the gaps. This rail belongs only to its bound field.",
                rail.SiteBytes(), rail.Count * rail.Bits, "field" + railField);
        }
        var pins = adjustment.PinRows();
        p.Rows("Runtime · pins", "[Field, dial, locked control]. An explicit pin blocks its response passage. An empty table means no pin is connected.", adjustment.ReferenceBits,
            pins, "pins", roleKeys: pins.Select(row => new[] { "field" + row[0], "field" + row[0], "locked" }).ToArray(), actual: adjustment.PinBytes());
        p.Rows("Runtime · offer passage", "[Reserve, launch, waiting]. One supplied request leaves reserve and waits for a response.", adjustment.ReferenceBits,
            [adjustment.OfferRow()], "offer", roleKeys: [["reserve", "launch", "waiting"]], actual: adjustment.OfferBytes());
        var responses = adjustment.SettleRows();
        p.Rows("Runtime · response passage", "[Waiting, control, dial]. Without a row there is no invented adjustment target. The waiting request remains present.", adjustment.ReferenceBits,
            responses, "response", roleKeys: responses.Select(row => row.Select(slot => keys[slot]).ToArray()).ToArray(), actual: adjustment.SettleBytes());
        BehaviorLaw(p, "Runtime · offer law", adjustment.OfferLaw(), "offer");
        BehaviorLaw(p, "Runtime · response law", adjustment.SettleLaw(), "response");
        BehaviorScope(p, "Prediction · observed scope", prediction.BeforeSupport, prediction.ScopePorts, "readscope");
        BehaviorScope(p, "Prediction · next actual policy", prediction.AfterSupport, prediction.ScopePorts, "nextscope");
        if (hidden && adjustment.HasResponse)
            BehaviorScope(p, "Prediction · possible resolved settlement", prediction.SettlementSupport, prediction.ScopePorts, "settlementscope");
        var affected = adjustment.AffectedLinks(); var changedFields = adjustment.AffectedFields();
        for (var i = 0; i < p.Drawing.Count; i++)
        {
            var item = p.Drawing[i];
            if (changedFields.Any(field => item.RelatedKeys?.Contains("field" + field) == true))
                p.Drawing[i] = item with { Color = "#D66B2A", LineWidth = item.Kind == "stroke" ? 5 : 3 };
        }
        var chosen = adjustment.SelectedField;
        var tip = chosen is int field ? adjustment.Tip(field) == 1 ? "rest" : "farther" : "unbound";
        var outcome = !adjustment.HasResponse ? "No response is connected; the request can only wait." : hidden ?
            "Concealed control prevents a new response commitment; possible settlement remains a prediction." : adjustment.PinCount > 0 ?
            "The pin blocks this adjustment." : "The connected response may transfer the waiting request into its dial.";
        p.Result = $"Actual reach: {tip}. Reserve {(adjustment.ReservePresent ? "present" : "empty")}; waiting request {(adjustment.Waiting ? "present" : "empty")}. " +
            (affected.Length > 0 ? $"Farther is read by links {string.Join(", ", affected)}. " : "No field has moved farther. ") + outcome;
        p.Status = $"Reach trial · {adjustment.Trace.Count} offer/retry attempts · source geometry absent";
        p.Node("reserve", adjustment.ReservePresent ? "Reserve · present" : "Reserve · empty", "One request was supplied at initialization. Retrying does not replenish it.", 115, 418, adjustment.ReservePresent);
        p.Node("waiting", adjustment.Waiting ? "Waiting · present" : "Waiting · empty", "A known present or absent request. Hidden control does not make this actual state unknown.", 350, 418, adjustment.Waiting);
        p.Node(chosen is int chosenField ? "field" + chosenField : "response", "Reach · " + tip,
            "A local ordered read: rest precedes farther. Shared links observe one dial. No metric size or distance is assigned.", 590, 418, tip == "farther");
        p.Label("Orange = reads of a changed field · drawing positions remain illustrative", 355, 457);
        p.Events.Add("The two trials have separate request stores. Switching views preserves both; Reset both trials creates a fresh independently decoded artifact.");
        if (adjustment.Trace.LastOrDefault() is { } last)
        {
            p.Events.Add($"Last attempt: {(last.Moved ? "dial moved farther" : "dial stayed")}; " +
                (last.SettleAfterState is null ? "no resolved response was committed." : "response resolved, possibly as a hold."));
            BehaviorScope(p, "Witness · observed after request", last.ActualScopeSupport, last.Prediction.ScopePorts, "actualscope");
        }
        p.Events.Add("A hidden control is a set of possible whole states, not zero. The owner bit chart shows actual storage; the prediction masks show only the published observation.");
    }

    private static void BehaviorScope(ExplorerPage p, string title, uint support, int[] scope, string key)
    {
        var states = 1 << scope.Length;
        var bytes = Enumerable.Range(0, (states + 7) / 8).Select(i => (byte)(support >> (8 * i))).ToArray();
        var possible = Enumerable.Range(0, states).Where(state => (support & (1U << state)) != 0)
            .Select(state => Convert.ToString(state, 2).PadLeft(scope.Length, '0')).ToArray();
        p.Bits(title, "Read mask, not stored flow. Each 1 admits one complete state of local slots [" + string.Join(",", scope) +
            "]. Rightmost state digit is the first slot. Admitted states: " + (possible.Length == 0 ? "none" : string.Join(" or ", possible)) + ".", bytes, states, key);
    }

    private static void BehaviorFieldCorrespondences(ExplorerPage p, ConstructionSurface surface)
    {
        for (var block = 0; block < p.Blocks.Count; block++)
        {
            var current = p.Blocks[block];
            p.Blocks[block] = current with { Marks = current.Marks.Select(mark =>
            {
                var fieldKey = mark.Keys.LastOrDefault(key => key.StartsWith("field") && int.TryParse(key[5..], out _));
                if (fieldKey is null) return mark;
                var field = int.Parse(fieldKey[5..]);
                var links = Enumerable.Range(0, surface.Table.Template.LinkCount).Where(link => surface.Table.Template.Link(link).Field == field).ToArray();
                var uses = Enumerable.Range(0, surface.Uses.UseCount).Where(use => links.Contains(surface.Uses.SignedUse(use) >> 1));
                return mark with { Keys = mark.Keys.Where(key => key != fieldKey).Concat(links.Select(link => "link" + link))
                    .Concat(uses.Select(use => "use" + use)).Append(fieldKey).Distinct().ToArray() };
            }).ToArray() };
        }
    }
}
