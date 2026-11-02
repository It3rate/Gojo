using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private BoundaryMeasurement? measurement;
    private static readonly int[] WorkSizes = [16, 256, 4096, 16384];

    private static void BoundaryBits(ExplorerPage p, OrderedBoundary s)
    {
        var rows = Enumerable.Range(0, s.EdgeCount).Select(s.Edge).ToArray();
        p.Rows("Ordered edge endpoints", "Departure first, arrival second. These references carry site identity, not lengths.",
            s.SiteBits, rows, "edges", rows.Select(r => r.Select(v => "site" + v).ToArray()).ToArray(), s.EdgeBytes(),
            Enumerable.Range(0, s.EdgeCount).Select(i => "edge" + i).ToArray());
        var marks = new List<BitMark>();
        for (var i = 0; i < s.UseCount; i++)
        {
            var code = s.SignedUse(i); var key = "edge" + (code >> 1);
            marks.Add(new(i * s.UseBits, 1, [key, "uses"], $"use {i}: reversal {(code & 1)}; this is NOT the ODV direction bit"));
            if (s.UseBits > 1) marks.Add(new(i * s.UseBits + 1, s.UseBits - 1, [key, "uses"], $"use {i} names edge {code >> 1}"));
        }
        p.Bits("Ordered signed uses", "Code = (edge position << 1) | reverse. Repeated uses remain separate occurrences.",
            s.UseBytes(), s.UseCount * s.UseBits, "uses", marks.ToArray());
        if (s.PatchUseCount > 0)
            p.Rows("Supplied patch → boundary uses", "This separate attached structure supplies a patch with this boundary; a loop alone supplies no patch or area.",
                s.PatchBits, Enumerable.Range(0, s.PatchUseCount).Select(i => new[] { s.PatchUse(i) }).ToArray(), "patch", actual: s.PatchBytes());
    }

    private ExplorerPage BoundaryPage()
    {
        var p = new ExplorerPage("What closes before we know any lengths?",
            "Match departures and arrivals at the same site. Compare signed balance, parity and ordered closure; none measures an amount of flow.", "BoundaryExperiments.md");
        Choose(p, "Structure", ["Open chain", "Closed cycle", "One reversed use", "Two separate loops", "Coincident, separate sites", "Cycle + supplied patch", "Self-loop", "Out and back"], Mode, n => Mode = n);
        Choose(p, "Read work", ["Inspect every use", "Inspect first use only"], Variant, n => Variant = n);
        int[][] edges = [[0, 1], [1, 2], [2, 0]]; int[] uses = [0, 2, 4]; var sites = 3;
        if (Mode == 0) { edges = [[0, 1], [1, 2]]; uses = [0, 2]; }
        if (Mode == 2) uses = [0, 3, 4];
        if (Mode == 3) { sites = 6; edges = [[0, 1], [1, 2], [2, 0], [3, 4], [4, 5], [5, 3]]; uses = [0, 2, 4, 6, 8, 10]; }
        if (Mode == 4) { sites = 4; edges = [[0, 1], [1, 2], [2, 3]]; }
        if (Mode == 6) { sites = 1; edges = [[0, 0]]; uses = [0]; }
        if (Mode == 7) { sites = 2; edges = [[0, 1]]; uses = [0, 1]; }
        var s = new OrderedBoundary(sites, edges, uses, Mode == 5 ? [0, 1, 2] : null);
        var r = s.Read(Variant == 0 ? int.MaxValue : 1); BoundaryBits(p, s);
        (double X, double Y)[] points = Mode == 3
            ? [(100, 130), (260, 130), (180, 290), (440, 130), (600, 130), (520, 290)]
            : Mode == 7 ? [(190, 180), (510, 180)] : [(150, 130), (550, 130), (350, 310), (150, 215)];
        if (Mode == 4)
        {
            points[3] = points[0];
            p.Circle("projection", "One display position, two distinct references: A and D. The renderer overlays their endpoints without binding them.", 150, 130, 7);
        }
        if (Mode == 5) p.Node("patch", "Supplied patch", "Rows attach this patch to uses 0,1,2. Its measure remains unspecified. No patch is inferred in the other cases.", 350, 205, true);
        for (var i = 0; i < s.UseCount; i++)
        {
            var pair = s.DirectedUse(i); var a = points[pair[0]]; var b = points[pair[1]];
            var key = "edge" + (s.SignedUse(i) >> 1);
            if (pair[0] == pair[1]) p.Circle(key, "A self-loop has zero endpoint boundary. The edge still exists; boundary zero does not erase activity.", 210, 170, 75);
            else p.Edge(key, $"use {i}: {pair[0]} → {pair[1]}", $"Departure −site {pair[0]}, arrival +site {pair[1]}. Signed reference {s.SignedUse(i)}. No magnitude cancellation is asserted.",
                a.X, a.Y + (Mode == 7 ? i * 35 : 0), b.X, b.Y + (Mode == 7 ? i * 35 : 0), i < r.Visited, i >= r.Visited);
        }
        for (var i = 0; i < sites; i++)
        {
            var balance = r.Balance.GetValueOrDefault(i);
            p.Node("site" + i, $"{(char)('A' + i)} · {balance:+0;-0;0}", $"Local site {i}. {(r.Complete ? "Total" : "Inspected-prefix")} arrivals minus departures = {balance}. This is an occurrence count, not V or charge.",
                points[i].X, Mode == 4 && (i == 0 || i == 3) ? i == 0 ? 80 : 195 : points[i].Y, balance != 0, !r.Complete);
        }
        if (Mode == 4) p.Label("A and D share the circled endpoint position; their badges are separated.", 350, 385);
        else p.Label("Zero boundary does not establish a rectangle, filled area or zero activity.", 350, 385);
        static string Verdict(bool? value) => value is null ? "unresolved" : value.Value ? "yes" : "no";
        p.Result = $"Signed balance: {Verdict(r.Balanced)} · even parity: {Verdict(r.EvenParity)} · one ordered closed walk: {Verdict(r.OrderedClosed)}.";
        p.Events.Add($"Inspected {r.Visited}/{r.Total} uses · {r.VisitedComponents} component(s) in the inspected portion · {s.PayloadBytes} packed bytes");
        p.Events.Add("Unmatched: " + string.Join("  ", r.Balance.Where(v => v.Value != 0).Select(v => $"site {v.Key}: {v.Value:+0;-0}")));
        p.Boundary = !r.Complete ? "Budget exhausted: the unmatched prefix can still be canceled by unread uses. No whole-structure verdict is made."
            : Mode == 5 ? "The supplied patch passes the boundary check. Its area and geometric realization remain unknown."
            : "Parity forgets signs; signed balance forgets traversal order. Even an ordered closed walk may retrace or cross itself.";
        return p;
    }

    private ExplorerPage CyclePage()
    {
        var p = new ExplorerPage("A loop can contradict an order and carry a signal",
            "The endpoints are the same. A strict-order claim asks whether values can satisfy them; a passage law asks whether an activation can move.", "BoundaryExperiments.md");
        Choose(p, "Attached law", ["Passage / repeating activation", "Strict order / source > target"], Mode, n => Mode = n);
        Choose(p, "Closing connection observation", Supports, Control, n => Control = n);
        var s = new OrderedBoundary(3, [[0, 1], [1, 2], [2, 0]], [0, 2, 4]);
        BoundaryBits(p, s); SupportBlock(p, "control", "Closing connection support", Support(Control));
        var active = -1;
        if (Mode == 0)
        {
            Stepper(p, 48);
            var circuit = new PulseCircuit(2);
            var a = circuit.State(true); var b = circuit.State(); var c = circuit.State(); var guard = circuit.Guard();
            circuit.Bind(guard, new Observation(Support(Control)));
            var free = circuit.AddTable(PassageLaw.Unconditional()); var present = circuit.AddTable(PassageLaw.Controlled());
            circuit.Link(free, a, guard, b); circuit.Link(free, b, guard, c); circuit.Link(present, c, guard, a);
            for (var i = 0; i < Steps; i++)
            {
                var step = circuit.Pulse();
                p.Events.Add($"beat {i + 1}: {step.Before} → {step.After} · {(step.Moved ? "moved" : step.Committed ? "held" : "unresolved, unchanged")}");
            }
            active = circuit.Active ?? -1;
            StateBlock(p, "Actual activation and guard bits", circuit.ValueBytes(), ["site0", "site1", "site2", "control"]);
            p.Rows("Unconditional passage rows", "Guard 3 is irrelevant to these two transitions; its supplier is not sampled.", 2, [[0, 3, 1], [1, 3, 2]], "free", actual: circuit.PackedRows(free));
            p.Rows("Present-controlled closing row", "Only C → A requires the public guard observation.", 2, [[2, 3, 0]], "closing", actual: circuit.PackedRows(present));
            p.Bits("Unconditional transition law", "Eight bytes admit whole before/after S,C,T states. The activation moves; the source clears.", circuit.LawBytes(free), 64, "free");
            p.Bits("Present transition law", "The closing route moves only with present control. Unknown control admits alternatives but commits neither.", circuit.LawBytes(present), 64, "closing");
            var next = circuit.Predict();
            p.Result = $"Activation at {(char)('A' + active)} · next admitted sites: {string.Join(", ", next.Targets.Select(t => ((char)('A' + t)).ToString()))}{(next.Missing ? "none (missing observation)" : "")}.";
            p.Boundary = "One activation circulates without duplication. Closed topology persists when a guard stops flow; observing a guard consumes none of its source.";
        }
        else
        {
            var verdict = StrictOrderRead.Evaluate(s, [2, 2, Support(Control)]);
            p.Result = verdict + (verdict == "Contradiction" ? ": A > B > C > A cannot hold in one strict scalar order." : ": only the participating claims constrain this read.");
            p.Events.Add("A > B and B > C participate; the closing C > A claim follows its public observation.");
            p.Boundary = "Acyclicity is a selected common-basis strict-order law. It provides no calibrated values, culprit or repair; no material is moved.";
        }
        (double X, double Y)[] xy = [(160, 130), (550, 130), (350, 320)];
        for (var i = 0; i < 3; i++)
        {
            var j = (i + 1) % 3;
            p.Edge("edge" + i, Mode == 0 ? $"{(char)('A' + i)} → {(char)('A' + j)}" : $"{(char)('A' + i)} > {(char)('A' + j)}",
                "The ordered endpoint references are shared by both selected interpretations; each law supplies its own semantics.", xy[i].X, xy[i].Y, xy[j].X, xy[j].Y,
                i != 2 || Control == 1, i == 2 && Control >= 2);
            p.Node("site" + i, ((char)('A' + i)).ToString(), Mode == 0 ? "Ordinary stored activation site, not an unknown amount of drawing material." : "A scalar role under a common strict-order premise, with no assigned magnitude.", xy[i].X, xy[i].Y, i == active);
        }
        p.Node("control", Control == 2 ? "Uncertain" : Control == 3 ? "Missing" : Control == 1 ? "Present" : "Absent", "One exposed observation governs the closing connection. Support is read evidence, not another material amount.", 130, 320, Control == 1, Control >= 2);
        return p;
    }

    private ExplorerPage WorkPage()
    {
        var p = new ExplorerPage("How much work does a structural read need?",
            "Grow a ring, then limit how many edge uses a reader may inspect. Construction, packed storage and read cost are measured separately.", "BoundaryExperiments.md");
        Choose(p, "Ring size", WorkSizes.Select(n => $"{n:N0} sites / edges").ToArray(), Mode, n => Mode = n);
        Choose(p, "Read allowance", ["Whole ring", "First quarter", "One edge use"], Variant, n => Variant = n);
        var size = WorkSizes[Mode]; var budget = Variant == 0 ? size : Variant == 1 ? size / 4 : 1;
        p.Actions.Add(new("Measure this read", () => { measurement = BoundaryScaling.Measure(size, budget); Refresh(); }));
        // A small literal sample only. Never render thousands of bytes or rebuild the measured ring on hover.
        var sample = BoundaryScaling.Ring(8); BoundaryBits(p, sample);
        p.Blocks[0] = p.Blocks[0] with { Title = "Illustrative eight-site ring (not benchmark bytes)" };
        for (var i = 0; i < 8; i++)
        {
            var angle = i * Math.PI / 4; var next = (i + 1) * Math.PI / 4;
            p.Edge("edge" + i, "", "Preview only: the measured ring has the selected number of distinct sites and edges.",
                350 + 190 * Math.Cos(angle), 210 + 150 * Math.Sin(angle), 350 + 190 * Math.Cos(next), 210 + 150 * Math.Sin(next), i < (Variant == 0 ? 8 : Variant == 1 ? 2 : 1));
            p.Node("site" + i, i.ToString(), "Illustrative site in the eight-site encoding sample.", 350 + 190 * Math.Cos(angle), 210 + 150 * Math.Sin(angle));
        }
        if (measurement is { } m && m.Sites == size && m.Budget == budget)
        {
            p.Result = $"{m.PayloadBytes:N0} packed bytes · read median {m.MedianReadMilliseconds:F3} ms · {m.Read.Visited:N0}/{size:N0} uses inspected.";
            p.Events.Add($"Build {m.BuildMilliseconds:F3} ms · read allocations {m.MedianReadAllocatedBytes:N0} bytes (median of 9 warm samples)");
            p.Events.Add(m.Read.Complete ? "Complete read: ordered closure established." : "Partial read: whole-ring closure remains unresolved, even though this fixture built a ring.");
        }
        else p.Result = $"Ready to inspect {budget:N0} of {size:N0} uses. Choose Measure this read for actual timings.";
        p.Label($"{size:N0} sites · allowance {budget:N0} uses", 350, 420);
        p.Boundary = "Payload excludes host containers and scratch allocations. This measures one boundary reader, not all computation; automatic splitting and thin-channel coordination remain future experiments.";
        return p;
    }
}
