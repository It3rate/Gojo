using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    // This is the observer's selected apparatus. The laws and state live in
    // the experiment owners, and reading this page never advances them.
    private StructuralRoute? guaranteeRoute;
    private SharedVariation? guaranteeVariation;
    private ClosurePassage? guaranteeGate;
    private int guaranteeCase = -1;
    private byte[]? guaranteeBeforeTopology, guaranteeBeforeWords, guaranteeBeforeRuntime;
    private int guaranteeBeforeTopologyBits, guaranteeBeforeWordBits;
    private int[] guaranteeAffected = [];
    private string guaranteeEvent = "No edit has been attempted. The diagram is an owner inspection, not a completed public proof.";

    private static readonly string[] GuaranteeCases =
    [
        "Loop · separate endpoint pairs", "Loop · shared vertex chain", "Loop · positional cycle",
        "Loose pairs · fixed O/D", "Chain · owned O/D", "Closed loop · controlled gate",
        "Four uses · shared reach", "Four uses · independent reaches",
        "Opposed uses · shared source", "Opposed uses · detached copies"
    ];
    private static readonly string[] GuaranteeChanges =
    [
        "Split the final arrival", "Move an interior junction", "Unpack to endpoint pairs",
        "Reload the encoded artifact", "Toggle first participation", "Flip first direction", "Flip first orientation"
    ];
    private static readonly string[] GuaranteeDescriptions =
    [
        "Each step stores two site references. Inspect the joins, then split one arrival. Equal endpoint numbers refer to shared sites, but the stored reference fields can be edited independently.",
        "Neighbours read the same junction field. Move the interior junction to see both adjacent steps follow it. The two outer reference fields still specify closure explicitly.",
        "Successor order wraps from the last stored site to the first. There is no independent final-arrival field to split. Unpack first to make that edit available.",
        "Every word inherits the same orientation and direction. The endpoint rows nevertheless form disconnected pairs. Matching O/D does not establish a continuous path.",
        "This chain owns all three ODV bits per step. Changing a direction observation leaves its shared junctions intact. Value faces and traversal topology are separate.",
        "One activation follows a closed route. A law on the final passage can hold it. A closed gate changes circulation, while the endpoint connections still form a loop.",
        "Four occurrences refer to one unknown reach. One local request advances that source to a farther ordinal stop, changing all four reads together.",
        "Four matching participation reads come from separate sources. The request is attached only to the first. Equal initial drawings do not make the unknown reaches identical.",
        "Two opposite uses refer to the same link and reach. An adjustment is seen through both directions. This is an opposed read correspondence, not a claimed geometric reflection.",
        "Two independently retained links initially give matching opposed reads. Only the first source receives the adjustment. Matching values alone do not create a live correspondence."
    ];

    private void EnsureGuaranteeRuntime()
    {
        if (guaranteeCase == Mode) return;
        guaranteeRoute = null; guaranteeVariation = null; guaranteeGate = null;
        guaranteeBeforeTopology = guaranteeBeforeWords = guaranteeBeforeRuntime = null;
        guaranteeBeforeTopologyBits = guaranteeBeforeWordBits = 0;
        guaranteeAffected = [];
        guaranteeEvent = "No edit has been attempted. The diagram is an owner inspection, not a completed public proof.";
        guaranteeCase = Mode;
        if (Mode == 0) guaranteeRoute = StructuralRoute.Pairs(5, [[0, 1], [1, 2], [2, 3], [3, 0]]);
        else if (Mode == 1) guaranteeRoute = StructuralRoute.Chain(5, [0, 1, 2, 3, 0]);
        else if (Mode == 2) guaranteeRoute = StructuralRoute.Cycle(5, [0, 1, 2, 3]);
        else if (Mode == 3) guaranteeRoute = StructuralRoute.Pairs(5, [[0, 1], [2, 3], [0, 1], [2, 3]]);
        else if (Mode == 4) guaranteeRoute = StructuralRoute.Chain(5, [0, 1, 2, 3, 0], 2, 0);
        else if (Mode == 5) guaranteeGate = new();
        else if (Mode is 6 or 7) guaranteeVariation = SharedVariation.Reaches(Mode == 6);
        else guaranteeVariation = SharedVariation.OpposedReads(Mode == 8);
        if (guaranteeVariation is not null) guaranteeEvent = "One request is held in reserve; no response has been attempted.";
    }

    private ExplorerPage GuaranteePage()
    {
        EnsureGuaranteeRuntime();
        var p = new ExplorerPage("What survives a change?",
            "Start with matching appearances, then change a value, a connection or a shared source. Inspect what is established now, what the layout preserves and what requires structural revision.", "StructuralGuarantees.md")
        {
            DrawingGuide = (GuaranteeCases[Mode], GuaranteeDescriptions[Mode])
        };
        Choose(p, "Case", GuaranteeCases, Mode, value => { Mode = value; guaranteeCase = -1; Variant = Control = 0; }, true);
        if (guaranteeRoute is not null) GuaranteeRoutePage(p, guaranteeRoute);
        else if (guaranteeGate is not null) GuaranteeGatePage(p, guaranteeGate);
        else GuaranteeVariationPage(p, guaranteeVariation!);
        p.Boundary = "The shapes are schematic. No angle, physical length, enclosed area or construction history is inferred from these bits. Unchecked reads, missing observations and absent geometric claims are different limitations.";
        return p;
    }

    private static string GuaranteeTruth(byte support, string yes, string no) => support switch
    {
        2 => yes, 1 => no, 3 => "not yet established", _ => "no observation"
    };

    private void GuaranteeRoutePage(ExplorerPage p, StructuralRoute route)
    {
        p.Choices.Add(new("Change to try", GuaranteeChanges, Variant, value => { Variant = value; Refresh(); }));
        p.Actions.Add(new("Inspect one check", () =>
        {
            var used = route.Inspect(1);
            guaranteeEvent = used == 0 ? "The selected topology read is already settled; no further inspection was needed." :
                "One bounded inspection was performed. Payload bits were not changed.";
            Refresh();
        }));
        p.Actions.Add(new("Apply change", () => { ApplyGuaranteeChange(); Refresh(); }));
        p.Actions.Add(new("Reset", () => { guaranteeCase = -1; Steps = 0; Refresh(); }));

        var rows = route.TopologyRows();
        var marks = new List<BitMark>();
        for (var row = 0; row < rows.Length; row++)
        for (var role = 0; role < rows[row].Length; role++)
        {
            var keys = new List<string> { "topology", "site" + rows[row][role] };
            if (route.Layout == 0) keys.Add("edge" + row);
            else
            {
                if (row < route.StepCount) keys.Add("edge" + row);
                if (row > 0) keys.Add("edge" + (row - 1));
                else if (route.Layout == 2) keys.Add("edge" + (route.StepCount - 1));
            }
            marks.Add(new((row * route.RowArity + role) * route.SiteBits, route.SiteBits, keys.ToArray(),
                $"Stored row {row}, role {role} → site {rows[row][role]}. Shared field use is established by the selected layout."));
        }
        var rule = route.Layout switch
        {
            0 => "Each row owns [departure, arrival]. Every successive join must be inspected.",
            1 => "Successive fields define each step. Adjacent steps use the exact same stored field at their shared junction.",
            _ => "Successive fields define each step; after the last field, the successor is the first field. Closure is positional."
        };
        p.Bits("Source · topology", rule + "  " + string.Join(" · ", rows.Select(row => "[" + string.Join(",", row) + "]")),
            route.TopologyBytes(), route.RowCount * route.RowArity * route.SiteBits, "topology", marks.ToArray());
        var wordMarks = Enumerable.Range(0, route.StepCount).Select(i => new BitMark(i * route.WordWidth, route.WordWidth,
            ["words", "edge" + i], $"Step {i}: owned payload bits. Its complete ODV read is {Word(route.Word(i))}.")).ToArray();
        p.Bits("Source · value observations", $"Bucket O={route.OrientationGroup}, D={route.DirectionGroup}; {route.WordWidth} stored bit(s) per step. Group 2 owns its face bits. These observations do not connect endpoints.",
            route.WordBytes(), route.StepCount * route.WordWidth, "words", wordMarks);
        p.Bits("Read · continuity", "01 rejects continuity; 10 establishes it; 11 means the limited inspection has not yet established either answer. This is a derived read, not a stored direction or value.", [route.ContinuitySupport], 2, "continuity");
        p.Bits("Read · closure", "Closure concerns this selected ordered route. A cycle is not proof of a square, simple boundary or filled region.", [route.ClosureSupport], 2, "closure");
        if (guaranteeBeforeTopology is not null)
            p.Bits("Witness · topology before attempt", "Captured before the most recent change attempt. It is not used as the current answer. Byte padding is not a participant.", guaranteeBeforeTopology, guaranteeBeforeTopologyBits, "topology");
        if (guaranteeBeforeWords is not null)
            p.Bits("Witness · values before attempt", "The prior packed observation array. An inherited face cannot be changed by writing its payload.", guaranteeBeforeWords, guaranteeBeforeWordBits, "words");
        var serialized = route.Encode();
        p.Bits("Transport · complete route", "The retained artifact includes its layout, bounds, face ownership and payload. Reloading preserves that structure but does not preserve an unfinished inspection as a proved answer.", serialized, serialized.Length * 8, "transport");

        GuaranteeRouteDrawing(p, route);
        var guaranteed = route.Layout == 0 ? "Interior joins require agreement between separate fields." :
            route.Layout == 1 ? "Interior adjacency follows from shared junction fields." : "Interior adjacency and wraparound follow from the cyclic layout.";
        p.Label("PRESERVED BY VALUE CHANGES", 350, 405);
        p.Label("Stored endpoints and junction sharing", 350, 431);
        p.Label("GEOMETRIC SHAPE: NOT REPRESENTED", 350, 468);
        p.Result = $"Current published read: {GuaranteeTruth(route.ContinuitySupport, "continuous", "not continuous in this order")} · {GuaranteeTruth(route.ClosureSupport, "closed", "not a closed walk")}. {guaranteed}";
        p.Status = $"{route.Inspections} inspection checks · {GuaranteeLayout(route.Layout)} · revision {route.TopologyRevision} · {route.RowsRead} topology rows read / {route.RowsWritten} written";
        p.Events.Add(guaranteeEvent);
        p.Events.Add(route.Layout == 2 ? "A seam split needs conversion to explicit endpoints. Retargeting a shared cyclic site keeps wraparound." :
            route.Layout == 1 ? "Moving one interior vertex changes both adjacent steps. The outer fields are independent: changing the last one can open the seam directly." :
            "An endpoint edit can break a join. Any previous inspection result must be re-established after a topology change.");
        p.Events.Add("Seeing the complete owner diagram does not give the bounded reader free inspections.");
    }

    private void ApplyGuaranteeChange()
    {
        var route = guaranteeRoute!;
        guaranteeBeforeTopology = route.TopologyBytes();
        guaranteeBeforeWords = route.WordBytes();
        guaranteeBeforeTopologyBits = checked((int)route.TopologyBits);
        guaranteeBeforeWordBits = checked((int)route.ValueBits);
        guaranteeAffected = [];
        StructuralRouteEdit? edit = null;
        switch (Variant)
        {
            case 0:
                edit = route.Layout == 1 ? route.RetargetVertex(route.RowCount - 1, 4) :
                    route.TryRetargetEndpoint(route.StepCount - 1, 1, 4);
                guaranteeEvent = edit.Applied ? "The final arrival now refers to spare site 4. The actual connection changed." :
                    route.Layout != 2 ? "The final arrival already names site 4. This attempt made no further change." :
                    "No independent final-arrival field is editable in this layout. Unpack first; no connection was changed.";
                break;
            case 1:
                edit = route.Layout == 0 ? route.TryRetargetEndpoint(0, 1, 4) : route.RetargetVertex(1, 4);
                guaranteeEvent = !edit.Applied ? "The selected field already names site 4. This attempt made no further change." :
                    route.Layout == 0 ? "Only the first pair's arrival moved to site 4. The next pair still names its own departure." :
                    "The shared interior field moved to site 4. Both neighbouring steps read that same field.";
                break;
            case 2:
                edit = route.Unpack();
                guaranteeEvent = edit.Applied ? "The topology was expanded into explicit endpoint pairs. Current connections are preserved; independent endpoint edits are now expressible." :
                    "The topology already uses explicit endpoint pairs. No conversion was needed.";
                break;
            case 3:
                guaranteeRoute = StructuralRoute.Decode(route.Encode());
                guaranteeEvent = "Reloaded the encoded artifact into a new owner. Layout and observations survive; inspection starts unresolved.";
                break;
            default:
                var bit = Variant == 4 ? 1 : Variant == 5 ? 2 : 4;
                try
                {
                    var changed = route.SetWord(0, (byte)(route.Word(0) ^ bit));
                    if (changed) guaranteeAffected = [0];
                    guaranteeEvent = "The first observation changed. The topology and its completed read remain unchanged; no material transfer was requested.";
                }
                catch (ArgumentException)
                {
                    guaranteeEvent = "That face is inherited from the bucket and has no editable payload bit. The attempted observation was rejected; all bytes remain unchanged.";
                }
                break;
        }
        if (edit is { } witness) guaranteeAffected = witness.AffectedEdges;
    }

    private static (double X, double Y) GuaranteeSite(int site) => site switch
    {
        0 => (155, 105), 1 => (545, 105), 2 => (545, 300), 3 => (155, 300), _ => (350, 350)
    };

    private static string GuaranteeLayout(int layout) => layout switch
    { 0 => "endpoint pairs", 1 => "shared vertex chain", _ => "positional cycle" };

    private void GuaranteeRouteDrawing(ExplorerPage p, StructuralRoute route)
    {
        for (var i = 0; i < route.StepCount; i++)
        {
            var edge = route.Edge(i); var from = GuaranteeSite(edge[0]); var to = GuaranteeSite(edge[1]);
            var offset = Mode == 3 && i >= 2 ? 25 : 0;
            var changed = guaranteeAffected.Contains(i);
            p.Edge("edge" + i, $"{i}: {edge[0]} → {edge[1]}  ·  {Word(route.Word(i))}",
                $"Step {i} follows actual site references {edge[0]} → {edge[1]}. ODV {Word(route.Word(i))} is a separate observation.",
                from.X, from.Y + offset, to.X, to.Y + offset, true);
            if (changed) p.Drawing[^1] = p.Drawing[^1] with { Color = "#D66B2A", LineWidth = 4 };
        }
        for (var i = 0; i < route.SiteCount; i++)
        {
            var point = GuaranteeSite(i);
            p.Node("site" + i, i == 4 ? "4 · spare" : "site " + i,
                $"Local site {i}. Its displayed position supplies no metric or identity evidence.", point.X, point.Y, i != 4);
        }
        p.Label("CURRENT LAYOUT: " + GuaranteeLayout(route.Layout).ToUpperInvariant(), 350, 38);
    }

    private void GuaranteeGatePage(ExplorerPage p, ClosurePassage gate)
    {
        p.Choices.Add(new("Gate read", ["Open · 1", "Blocked · 0", "Uncertain · {0,1}", "Missing observation"], Control, value =>
        {
            Control = value; gate.ObserveControl(value switch { 0 => 2, 1 => 1, 2 => 3, _ => 0 }); Refresh();
        }));
        p.Actions.Add(new("Advance one beat", () =>
        {
            guaranteeBeforeRuntime = gate.Walk.ValueBytes(); gate.Pulse(); Steps = gate.Walk.Trace.Count; Refresh();
        }));
        p.Actions.Add(new("Reset", () => { guaranteeCase = -1; Control = Steps = 0; Refresh(); }));
        var walk = gate.Walk; var prediction = walk.Predict();
        var keys = Enumerable.Range(0, walk.StateCount + walk.GuardCount).Select(state => BehaviorStateKey(walk, state)).ToArray();
        BehaviorStore(p, "Runtime · current activation", "One supplied activation occupies input or a family site. Guard state is observation, not another activation.", walk.ValueBytes(), keys);
        if (guaranteeBeforeRuntime is not null) BehaviorStore(p, "Witness · activation before beat", "Actual state immediately before the last attempt.", guaranteeBeforeRuntime, keys);
        p.Bits("Read · gate support", "A two-bit observation governs only the controlled passage. Missing or uncertain evidence holds a request whose next move depends on it.", [gate.ControlSupport], 2, "control");
        var source = new ConstructionSurface(gate.Table, gate.Entry, gate.Uses);
        ConstructionBits(p, source);
        for (var table = 0; table < walk.TableCount; table++)
            BehaviorLaw(p, "Runtime · passage law " + table, walk.LawBytes(table), "law" + table);
        p.Rows("Runtime · route correspondence", "[Signed use, from, guard, to, law table, row]. The final route has a controlling law; the endpoint cycle does not depend on its current observation.", walk.ReferenceBits,
            Enumerable.Range(0, walk.RouteCount).Select(walk.RouteRow).ToArray(), "routes", actual: walk.RouteBytes());
        for (var i = 0; i < gate.Table.Template.LinkCount; i++)
        {
            var edge = gate.Table.Template.Link(i); var from = GuaranteeSite(edge.Start); var to = GuaranteeSite(edge.End);
            p.Edge("link" + i, i * 2 == gate.ControlledUse ? "controlled passage" : "passage", "The stored connection remains present whether or not its law admits this transfer.",
                from.X, from.Y, to.X, to.Y, true);
        }
        for (var i = 0; i < gate.Table.Template.SiteCount; i++)
        {
            var point = GuaranteeSite(i);
            var occupied = walk.ActiveFamilySite == i;
            p.Node("site" + i, "site " + i + (occupied ? " · activation" : ""), "Actual family site. Orange marks the single activation when present here.", point.X, point.Y, true);
            // Nodes are rendered above other primitives; a small circle at the
            // same centre would be hidden beneath the node's filled rectangle.
            if (occupied) p.Drawing[^1] = p.Drawing[^1] with { Color = "#D66B2A", LineWidth = 4 };
        }
        p.Node("request", walk.ReadStateSupport(walk.InputState) == 2 ? "input · present" : "input · empty", "One initial activation; this is not replenished during the trial.", 120, 405, walk.ReadStateSupport(walk.InputState) == 2);
        p.Node("control", "gate · " + (gate.ControlSupport switch { 2 => "open", 1 => "blocked", 3 => "uncertain", _ => "missing" }),
            "The control changes admission through a passage; it does not retarget or remove its endpoints.", 525, 405, gate.ControlSupport == 2, gate.ControlSupport is 0 or 3);
        p.Label("TOPOLOGY: CLOSED · GEOMETRY: NOT REPRESENTED", 350, 468);
        p.Result = $"Topology: {(gate.Closed ? "closed" : "open")}. Actual activation: {BehaviorState(walk, prediction.Before)}. " +
            (prediction.Missing ? "Needed gate observation is missing; the activation holds." : !prediction.Resolved ? "Needed gate read is uncertain; the activation holds." :
             prediction.Targets.Length == 0 || prediction.Targets.All(target => target == prediction.Before) ? "No departing transfer is admitted; the activation holds." : "Next passage has a resolved read.");
        p.Status = $"{walk.Trace.Count} attempted beats · {walk.OccupiedStateCount} occupied activation slot";
        p.Events.Add("Topology remains closed through every gate observation. The current law determines circulation.");
        if (walk.Trace.LastOrDefault() is { } last)
            p.Events.Add($"Last attempt: {BehaviorState(walk, last.Step.Before)} → {BehaviorState(walk, last.Step.After)} · " + (last.Step.Moved ? "transferred" : "held"));
        p.Events.Add("Uncertain control does not make a known stored activation uncertain or erase the route.");
    }

    private void GuaranteeVariationPage(ExplorerPage p, SharedVariation variation)
    {
        p.Choices.Add(new("Response read", ["Known control", "Concealed control"], Control, value => { Control = value; Refresh(); }));
        p.Actions.Add(new("Offer / retry request", () =>
        {
            guaranteeBeforeRuntime = variation.StoreBytes(); var step = variation.Push(Control == 1);
            guaranteeEvent = step.Moved ? "One request advanced the selected source dial. Every use reads through its actual source binding." :
                step.Waiting ? "The supplied request remains waiting. No new input or assumed answer was added." : "No further request is available; the retained state is unchanged.";
            Refresh();
        }));
        p.Actions.Add(new("Reset", () => { guaranteeCase = -1; Refresh(); }));
        var adjustment = variation.Adjustment; var predicted = variation.Predict(Control == 1);
        var reads = Enumerable.Range(0, variation.UseCount).Select(i => variation.Read(i)).ToArray();
        var rows = variation.BindingRows();
        p.Rows("Read · occurrence correspondence", "Derived [occurrence, signed link use, source field] rows. These expose the actual retained references; they are not a second source of sharing.", variation.BindingBits, rows, "bindings",
            roleKeys: rows.Select(row => new[] { "use" + row[0], "use" + row[0], "field" + row[2] }).ToArray(), actual: variation.BindingBytes());
        var keys = Enumerable.Range(0, adjustment.LogicalSiteCount).Select(slot => slot switch
        { 0 => "reserve", 1 => "waiting", 2 => "launch", 3 => "free", 4 => "locked", _ => "field" + (slot - 5) }).ToArray();
        BehaviorStore(p, "Runtime · current adjustment", "Reserve, waiting, three guard slots, then source dials. Dial positions are rest/farther on each field's own rail. Independent rails do not supply a common physical distance.", variation.StoreBytes(), keys);
        if (guaranteeBeforeRuntime is not null) BehaviorStore(p, "Witness · adjustment before attempt", "Actual runtime before the last offer or retry.", guaranteeBeforeRuntime, keys);
        p.Rows("Runtime · dial bindings", "[Source field, runtime dial, resting stop, farther stop]. Sharing names the same dial and rail; matching labels on separate rails do not prove equal hidden extents.", adjustment.ReferenceBits,
            adjustment.FieldRows(), "fields", roleKeys: adjustment.FieldRows().Select(row => Enumerable.Repeat("field" + row[0], 4).ToArray()).ToArray(), actual: adjustment.FieldBytes());
        p.Rows("Runtime · offer passage", "[Reserve, launch, waiting]. The one actual request enters the local response structure.", adjustment.ReferenceBits, [adjustment.OfferRow()], "offer", actual: adjustment.OfferBytes());
        p.Rows("Runtime · response passage", "[Waiting, control, dial]. These are the response's executable connections.", adjustment.ReferenceBits, adjustment.SettleRows(), "response", actual: adjustment.SettleBytes());
        BehaviorLaw(p, "Runtime · offer law", adjustment.OfferLaw(), "offer");
        BehaviorLaw(p, "Runtime · response law", adjustment.SettleLaw(), "response");
        BehaviorScope(p, "Prediction · actual policy", predicted.AfterSupport, predicted.ScopePorts, "prediction");
        if (Control == 1) BehaviorScope(p, "Prediction · possibilities if resolved", predicted.SettlementSupport, predicted.ScopePorts, "resolved");
        ConstructionBits(p, new(variation.Table, variation.Entry, variation.Uses));

        var changed = variation.ChangedUses();
        for (var i = 0; i < reads.Length; i++)
        {
            var read = reads[i];
            var from = Mode < 8 ? GuaranteeSite(read.From) : (X: read.From == 0 ? 155d : 545d, Y: 145d + i * 125);
            var to = Mode < 8 ? GuaranteeSite(read.To) : (X: read.To == 0 ? 155d : 545d, Y: 145d + i * 125);
            p.Edge("use" + i, $"use {i} · source {read.Field} · {(read.Tip == 1 ? "rest" : "farther")}",
                $"Occurrence {i} reads source field {read.Field} through {(read.Reversed ? "reversed" : "forward")} use of link {read.Link}. Reversal is not an ODV sign edit.",
                from.X, from.Y, to.X, to.Y, true);
            p.Drawing[^1] = p.Drawing[^1] with { RelatedKeys = ["field" + read.Field, "use" + i], Color = changed.Contains(i) ? "#D66B2A" : "#467B9A", LineWidth = changed.Contains(i) ? 4 : 2 };
        }
        for (var field = 0; field < variation.Table.Template.FieldCount; field++)
            p.Node("field" + field, $"source {field} · {(adjustment.Tip(field) == 1 ? "rest" : "farther")}",
                "One local source dial. Several reads of it do not create additional material requests.", 115 + field * 155, 370, adjustment.Tip(field) != 1);
        p.Label("Orange = changed ordinal read · drawing distances stay schematic", 350, 422);
        p.Label("COARSE PARTICIPATION REMAINS 1", 350, 465);
        var shared = Enumerable.Range(1, variation.UseCount - 1).All(i => variation.SharesSource(0, i));
        var relation = Mode >= 8 ? variation.OpposedSource(0, 1) ? "opposed uses of one source" : "separate sources with opposed endpoint reads" :
            shared ? "all occurrences share one source" : "occurrences refer to separate sources";
        p.Result = $"Retained relation: {relation}. Changed uses: {(changed.Length == 0 ? "none" : string.Join(", ", changed))}. " +
            (adjustment.Waiting ? "Actual request remains waiting." : adjustment.ReservePresent ? "One request remains in reserve." : "The request has settled in the selected dial.");
        p.Status = $"{adjustment.Trace.Count} response attempts · {variation.Table.Template.FieldCount} source field(s) · {variation.UseCount} read occurrences";
        p.Events.Add(guaranteeEvent);
        p.Events.Add(shared ? "Shared references guarantee common variation at this selected source. Directional uses can still differ." :
            "Matching coarse words or ordinal stop labels do not establish equality of independent unknown reaches.");
        p.Events.Add("The single request is conserved across reserve, waiting and dials. Observing its result several times does not duplicate it.");
    }
}
