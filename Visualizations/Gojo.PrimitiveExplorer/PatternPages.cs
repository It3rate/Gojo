using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private ExplorerPage RectanglePage()
    {
        var p = new ExplorerPage("A rectangle comes from correspondence", "Four ordered edges occupy a declared perpendicular basis. Opposite sides refer to the same unknown reaches; the last edge returns to the actual first corner.", "SaturationExperiments.md") { Status = "Executable constraint read · illustrative geometry" };
        Choose(p, "Reach references", ["A, B, A, B · rectangle", "A, A, A, A · equal reaches", "A, B, C, D · unshared"], Mode, n => Mode = n);
        Choose(p, "Reach A", Supports, Control, n => Control = n);
        Choose(p, "Last attachment", ["Return to corner 0", "Attach to corner 1"], Variant, n => Variant = n);
        var reaches = new[] { new Observation(Support(Control)), new Observation(2), new Observation(2), new Observation(2) };
        var path = new OrthogonalPathFixture(reaches);
        int[] refs = Mode switch { 0 => [4, 5, 4, 5], 1 => [4, 4, 4, 4], _ => [4, 5, 6, 7] };
        double[] x = Mode == 1 ? [200, 500, 500, 200] : [120, 580, 580, 120]; double[] y = [90, 90, 370, 370];
        for (var i = 0; i < 4; i++)
        {
            var end = i == 3 && Variant == 1 ? 1 : (i + 1) % 4;
            path.Connect(i, i, end, refs[i]);
            var label = new[] { "+u", "+v", "−u", "−v" }[i]; var key = "reach" + refs[i];
            p.Rows($"Lane {i} · {label}", "[start corner, end corner, shared reach]; each reference three bits.", 3, [[i, end, refs[i]]], "edge" + i,
                [["corner" + i, "corner" + end, key]], path.PackedLane(i));
            p.Edge("edge" + i, $"{label} · {(char)('A' + refs[i] - 4)}", $"Edge {i}: start {i}, end {end}, reach reference {refs[i]}. Hover the reach bits to inspect shared sides.", x[i], y[i], x[end], y[end], uncertain: Mode == 2 || Control != 1);
            // A second visual, near the side, links the shared parameter field.
            p.Node(key, ((char)('A' + refs[i] - 4)).ToString(), $"All uses of reach {refs[i]} promise the same unknown length. Equal V bits at different references do not make this promise.", (x[i] + x[end]) / 2, (y[i] + y[end]) / 2, Control == 1, Control != 1);
        }
        for (var i = 0; i < 4; i++) p.Node("corner" + i, "Corner " + i, "A local site, not a coordinate value. Matching references join the sides.", x[i], y[i]);
        SupportBlock(p, "reach4", "Reach A observation", reaches[0].ReadSupport());
        var read = path.Read();
        p.Result = read.GuaranteesRectangle ? "Rectangle guaranteed under the selected planar perpendicular basis and nonzero shared reaches." :
            !read.FourCornerCycle ? "The final attachment breaks the four-corner cycle." : !read.SharedOppositeReaches ? "Closed topology, but opposite-length correspondence is missing: rectangle not established." :
            read.NondegeneracySupport == 1 ? "A zero reach gives a degenerate boundary, not a nonzero rectangle." : read.NondegeneracySupport == 0 ? "A required reach observation is missing." : "A rectangle remains possible; nondegeneracy is unresolved.";
        p.Boundary = "Pixels are a possible layout, not encoded dimensions or proof of closure. The four geometric lanes are a declared basis, not the four O/D bit combinations.";
        return p;
    }

    private ExplorerPage ContourPage()
    {
        var p = new ExplorerPage("One whole contour, shared radial reach", "A path keeps the same radius throughout, returns to its reference, and can be partitioned. Shared part references supply equality; chords connect the resulting landmarks.", "RepetitionExperiments.md") { Status = "Executable constraint read · illustrative geometry" };
        Choose(p, "Construction", ["Circle · one whole arc", "Triangle · 3 equal parts", "Square · 4 equal parts", "Pentagon · 5 equal parts", "Hexagon · 6 equal parts", "Rectangle · alternating parts"], Mode, n => Mode = n);
        Choose(p, "Radius read", Supports, Control, n => Control = n);
        Choose(p, "Seam", ["End binds to actual start", "Leave end unbound"], Variant, n => Variant = n);
        var n = Mode switch { 0 => 1, 1 => 3, 2 => 4, 3 => 5, 4 => 6, _ => 4 };
        var contour = new ContourPattern(new Observation(Support(Control)), new Observation(2));
        var rows = new List<int[]>(); var chordRows = new List<int[]>(); var radialRows = new List<int[]> { new[] { 0, 1, 3 } };
        for (var i = 0; i < n; i++)
        {
            var from = i == 0 ? 1 : i + 4; var to = i == n - 1 ? 2 : i + 5; var part = Mode == 5 ? 32 + i % 2 : 32;
            contour.Arc(from, to, part); rows.Add([from, to, part]); radialRows.Add([0, to, 3]);
            if (n > 1) { contour.Chord(from, to == 2 ? 1 : to); chordRows.Add([from, to == 2 ? 1 : to]); }
        }
        if (Variant == 0) contour.Close();
        p.Rows("Arc parts", "[from site, to site, part reference]. Equal references promise equal shares of the same whole.", 6, rows.ToArray(), "arcs",
            rows.Select(r => new[] { "site" + r[0], "site" + r[1], "part" + r[2] }).ToArray(), contour.ArcBytes());
        p.Rows("Radial attachments", "[centre, contour site, shared radius]. The selected arc law maintains that reach throughout the arc.", 6, radialRows.ToArray(), "radius", actual: contour.RadialBytes());
        p.Rows("Whole traversal", "[reference start, unwrapped end, whole]. Nonzero whole is separate from endpoint coincidence.", 6, [[1, 2, 4]], "whole", actual: contour.WholeBytes());
        if (Variant == 0) p.Rows("Seam binding", "The end is connected to the actual start, not a lookalike coordinate.", 6, [[2, 1]], "seam", [["site2", "site1"]], contour.ClosureBytes());
        if (n > 1) p.Rows("Successive straight chords", "Adjacent landmarks are joined, including the return to reference site 1.", 6, chordRows.ToArray(), "chords", actual: contour.ChordBytes());
        SupportBlock(p, "radius", "Radius support", Support(Control));
        p.Circle("arcs", "The circle is justified only under the explicitly selected homogeneous Euclidean radial-path law.", 350, 235, 165, Control != 1 || Variant != 0);
        p.Node("radius", "Centre 0", "One shared centre and unknown radius. No numeric radius or angle is stored.", 350, 235);
        var angles = Mode == 5 ? new[] { -.55, .55, Math.PI - .55, Math.PI + .55 } : Enumerable.Range(0, n).Select(i => -Math.PI / 2 + i * Math.Tau / n).ToArray();
        var pts = angles.Select(a => (X: 350 + 165 * Math.Cos(a), Y: 235 + 165 * Math.Sin(a))).ToArray();
        for (var i = 0; i < n; i++)
        {
            var refid = i == 0 ? 1 : i + 4; var next = (i + 1) % n; var part = Mode == 5 ? 32 + i % 2 : 32;
            if (n > 1) p.Edge("chords", "", "A straight chord joins these successive contour landmarks.", pts[i].X, pts[i].Y, pts[next].X, pts[next].Y, true, Control != 1 || Variant != 0);
            p.Node("site" + refid, "Site " + refid, "A landmark supplied by the partition, not a measured Cartesian position.", pts[i].X, pts[i].Y);
            if (n > 1) p.Node("part" + part, $"part {part}", "Shared part references establish equal shares. A distinct part leaves relative extent open.", 350 + 235 * Math.Cos(angles[i] + (Mode == 5 ? .35 : Math.PI / n)), 235 + 195 * Math.Sin(angles[i] + (Mode == 5 ? .35 : Math.PI / n)));
        }
        p.Node("seam", Variant == 0 ? "End 2 → start 1" : "End 2 is open", "Topological closure is explicit. A renderer drawing a closed circle cannot supply this missing binding.", 120, 430, Variant == 0, Variant != 0);
        var read = contour.Read();
        p.Result = !read.Circle ? "No guaranteed nondegenerate circle: check the seam and radius observation." : read.Square ? "Square established by four equal contour parts and successive chords." : read.Rectangle ? "Rectangle established by alternating shared parts and successive chords." : read.RegularPolygon ? $"Regular {n}-gon established by equal parts and successive chords." : "A circle is established; no polygon has been connected.";
        p.Boundary = "The chosen radial-path law is an explicit premise, not discovered field physics. Screen radius and arc proportions are illustrative; zero or uncertainty must not be read as known geometry.";
        return p;
    }

    private ExplorerPage RepetitionPage()
    {
        var p = new ExplorerPage("Reuse a body. Let a connected read stop it.", "Participation and turn sites alternate. A separate phase cycle advances on turns. At the check site, a stop read chooses Done or the feedback path.", "RepetitionExperiments.md");
        Choose(p, "Body", ["A, turn · square cycle", "A, turn, B, turn · rectangle cycle", "A, B, C · 4-phase cycle"], Mode, n => Mode = n);
        Choose(p, "Stop source", ["Phase returns to start", "Keep repeating", "Contact absent", "Contact present", "Contact uncertain", "Contact missing"], Variant, n => Variant = n);
        Stepper(p, 48);
        int[] refs = Mode switch { 0 => [0], 1 => [0, 1], _ => [0, 1, 2] };
        var stop = Variant >= 2 ? new Observation(Variant switch { 2 => 1, 3 => 2, 4 => 3, _ => 0 }) : null;
        var body = new RepeatedBody(refs, 4, stop: stop, continuous: Variant == 1);
        body.BindTurns(RepetitionExamples.Contour(4, true, false));
        var trace = new List<BodyStep>();
        for (var i = 0; i < Steps; i++)
        {
            var step = body.Pulse(); trace.Add(step);
            p.Events.Add($"Beat {i + 1}: site {step.Before} → {step.After}, phase {step.Phase}" + (step.Parameter is int r ? $", participate {(char)('A' + r)}" : "") + (step.Completed ? " · DONE" : !step.Committed ? " · unresolved, held" : ""));
            if (step.Completed || !step.Committed) break;
        }
        var stateCount = refs.Length * 2 + 5;
        var keys = Enumerable.Range(0, stateCount).Select(i => "body" + i).ToArray();
        StateBlock(p, "Body state and guard mirrors", body.Circuit.ValueBytes(), keys);
        SupportBlock(p, "stop", "Current stop read · separate from stored mirror", stop?.ReadSupport() ?? (Variant == 1 ? (byte)1 : body.Phase.AtStart.ReadSupport()));
        p.Rows("Parameter attachments", "[participation site, shared local parameter]. These are unknown stable reach roles, not measured values.", 6,
            Enumerable.Range(0, refs.Length).Select(body.ParameterRow).ToArray(), "parameters",
            Enumerable.Range(0, refs.Length).Select(i => new[] { "body" + body.ParticipationSites[i], "reach" + refs[i] }).ToArray(), body.ParameterBytes());
        var forwardRows = Enumerable.Range(0, refs.Length * 2).Select(i => body.Circuit.Wiring(body.ForwardTable, new(3, 6, i))).ToArray();
        p.Rows("Body forward connections", "[source, irrelevant guard, target]. One body transition per beat.", 6, forwardRows, "forward",
            forwardRows.Select(r => r.Select(v => "body" + v).ToArray()).ToArray(), body.Circuit.PackedRows(body.ForwardTable), Enumerable.Range(0, forwardRows.Length).Select(i => "forward" + i).ToArray());
        p.Rows("Stop → Done", "Check's present-control row.", 6, [body.Circuit.Wiring(body.PresentTable, new(3, 6, 0))], "stop", actual: body.Circuit.PackedRows(body.PresentTable));
        p.Rows("Not stopped → feedback", "Check's absent-control row returns to the SAME first participation site.", 6, [body.Circuit.Wiring(body.AbsentTable, new(3, 6, 0))], "feedback", actual: body.Circuit.PackedRows(body.AbsentTable));
        StateBlock(p, "Phase-cycle state and enable mirror", body.Phase.Circuit.ValueBytes(), ["phase0", "phase1", "phase2", "phase3", "enable"]);
        p.Rows("Phase-cycle connections", "Each turn enables one step; the final row targets actual phase 0.", 6,
            Enumerable.Range(0, 4).Select(i => body.Phase.Circuit.Wiring(body.Phase.Table, new(3, 6, i))).ToArray(), "phase", actual: body.Phase.Circuit.PackedRows(body.Phase.Table));
        p.Bits("Phase → contour correspondence", "Four [phase, arc start, arc end] rows bind this cycle to four equal parts of an actual selected contour.", body.TurnBytes(), 72, "turns");
        var positions = refs.Length * 2 + 2;
        var points = Enumerable.Range(0, positions).Select(i => (X: 250 + Math.Cos(-Math.PI / 2 + i * Math.Tau / positions) * 175, Y: 245 + Math.Sin(-Math.PI / 2 + i * Math.Tau / positions) * 155)).ToArray();
        for (var i = 0; i < forwardRows.Length; i++)
        {
            var row = forwardRows[i]; var from = points[row[0]]; var to = points[row[2]];
            p.Edge("forward" + i, "", $"Forward row [{string.Join(',', row)}]. The target becomes the next active stage.", from.X, from.Y, to.X, to.Y, body.Circuit.Active == row[0]);
        }
        p.Edge("stop", "stop", "A present stop read routes Check to Done.", points[body.Check].X, points[body.Check].Y, points[body.Done].X, points[body.Done].Y, uncertain: Variant >= 4);
        for (var i = 0; i < positions; i++)
        {
            var angle = -Math.PI / 2 + i * Math.Tau / positions; var x = 250 + Math.Cos(angle) * 175; var y = 245 + Math.Sin(angle) * 155;
            var label = i == body.Check ? "Check" : i == body.Done ? "Done" : i % 2 == 0 ? $"Use {(char)('A' + refs[i / 2])}" : "Turn";
            p.Node("body" + i, $"{i} · {label}", $"Local body site {i}. Occupied means this controller stage is active, not a packet of ink.", x, y, body.Circuit.Active == i);
        }
        p.Edge("feedback", "feedback", "The absent stop read returns activation to the same body start.", points[body.Check].X, points[body.Check].Y, points[0].X, points[0].Y, uncertain: Variant >= 4);
        p.Node("stop", Variant >= 4 ? "Stop ?" : "Stop read", "Observed at the check site. Unknown contact holds the actual activation; it does not pick a branch.", 580, 120, uncertain: Variant >= 4);
        double[] px = [515, 635, 635, 515], py = [240, 240, 350, 350];
        for (var i = 0; i < 4; i++)
        {
            p.Edge("phase", "", "A connected phase transition advances only when the body requests a turn.", px[i], py[i], px[(i + 1) % 4], py[(i + 1) % 4]);
            p.Node("phase" + i, "Phase " + i, "A local cycle position. Its geometric meaning comes from the separate turn bindings.", px[i], py[i], body.Phase.Position == i);
        }
        var uses = trace.Where(s => s.Parameter is not null).Select(s => $"{(char)('A' + s.Parameter!.Value)}@{s.Phase}");
        p.Result = $"Body site {body.Circuit.Active}; phase {body.Phase.Position}. Participation: {string.Join(" → ", uses)}";
        p.Boundary = "Reused reaches must remain the same unknown lengths. A cycle alone supplies no angle or geometric closure. Contact here is supplied evidence; shrinking and proximity detection remain missing laws.";
        return p;
    }

    private ExplorerPage CompositionPage()
    {
        var p = new ExplorerPage("A pattern can repeat while its frame changes", "Independent small controllers communicate through exposed reads. Their connections determine when a body runs and which placement its participation belongs to.", "RepetitionExperiments.md");
        Choose(p, "Composition", ["Rectangle → five wait stages → repeat", "Continuous body + moving frame"], Mode, n => Mode = n);
        Stepper(p, 48);
        if (Mode == 0)
        {
            var pattern = new PatternComposition();
            for (var i = 0; i < Steps; i++)
            {
                var s = pattern.Pulse(); p.Events.Add($"Beat {i + 1}: parent {s.ParentBefore} → {s.ParentAfter}" + (s.StartedAt is int at ? $", begin motif at placement {at}" : "") + (s.PlacementAdvanced ? ", placement advanced" : ""));
            }
            StateBlock(p, "Parent state / observations", pattern.Parent.ValueBytes(), Enumerable.Range(0, 9).Select(i => "parent" + i).ToArray());
            p.Bits("Body state / observations", "The child has its own local arrays and request/completion surface.", pattern.Body.Circuit.ValueBytes(), 10, "body");
            p.Bits("Displacement driver", "Ready, Done, request mirror. One accepted displacement per wait interval.", pattern.Driver.ValueBytes(), 3, "driver");
            p.Rows("Parent present-control connections", "Body completion enters waiting; driver completion releases the final wait.", 6,
                Enumerable.Range(0, 2).Select(i => pattern.Parent.Wiring(0, new(3, 6, i))).ToArray(), "parent", actual: pattern.Parent.PackedRows(0));
            p.Rows("Parent waiting connections", "Five waiting sites; four internal rows and a final completion-controlled exit.", 6,
                Enumerable.Range(0, 4).Select(i => pattern.Parent.Wiring(1, new(3, 6, i))).ToArray(), "wait", actual: pattern.Parent.PackedRows(1));
            p.Bits("Retained instance → placement rows", "Each row uses two 8-bit local references. These bytes are bindings, not coordinates.", pattern.FrameBytes(), pattern.FrameBytes().Length * 8, "frames",
                Enumerable.Range(0, pattern.Instances).Select(i => new BitMark(i * 16, 16, ["frames", "frame" + i], $"motif {i} → placement {pattern.Frame(i)[1]}")).ToArray());
            p.Bits("Retained translation rows", "[old placement, new placement, shared offset reference 0], eight bits per reference.", pattern.TranslationBytes(), pattern.TranslationBytes().Length * 8, "driver");
            p.Node("parent0", "Request body", "Parent Draw is exposed as the child's request read.", 130, 100, pattern.Parent.Active == pattern.Draw);
            p.Node("body", "Reusable body", "Child Done is exposed to the parent. No parent reaches into the child's private values.", 520, 100, pattern.Body.Circuit.Active != pattern.Body.Idle);
            p.Edge("parent", "request / completion", "Exposed public reads connect owners; ordered scheduler is body, driver, parent.", 190, 100, 460, 100);
            for (var i = 0; i < 5; i++)
            {
                p.Node("parent" + (i + 1), "Wait " + (i + 1), "One explicit waiting stage. Five stage transitions plus request propagation determine restart timing.", 95 + i * 125, 225, pattern.Parent.Active == pattern.Waiting[i]);
                if (i < 4) p.Edge("wait", "", "An explicit unconditional waiting-stage transition.", 95 + i * 125, 225, 220 + i * 125, 225);
            }
            p.Node("driver", "Move frame", "The wait surface requests a displacement. It appends a formal translation relation.", 170, 315, pattern.Driver.Active == pattern.DriverDone);
            p.Node("frames", $"{pattern.Instances} motif bindings", "Each invocation uses the current exposed placement. Distinct references do not alone prove distinct geometric positions.", 520, 315, pattern.Instances > 0);
            for (var i = 0; i < Math.Min(4, pattern.Instances); i++)
            {
                var x = 105 + i * 145;
                p.Edge("frame" + i, $"placement {pattern.Frame(i)[1]}", "Illustrative copy bound to this invocation's placement; pixel dimensions and offset are not encoded.", x, 390, x + 90, 390);
                p.Edge("frame" + i, "", "Same motif binding.", x + 90, 390, x + 90, 430);
                p.Edge("frame" + i, "", "Same motif binding.", x + 90, 430, x, 430);
                p.Edge("frame" + i, "", "Same motif binding.", x, 430, x, 390);
            }
            p.Label("Possible placement illustration · not measured coordinates", 350, 453);
            p.Result = $"After {Steps} beats: {pattern.Instances} invocation(s), placement reference {pattern.Placement}, parent stage {pattern.Parent.Active}.";
        }
        else
        {
            var pattern = new MovingFramePattern();
            for (var i = 0; i < Steps; i++) { var s = pattern.Pulse(); p.Events.Add($"Beat {i + 1}: phase {s.Phase}, placement now {pattern.Placement}" + (s.Parameter is int r ? $", used {(char)('A' + r)}" : "")); }
            p.Bits("Body state", "Continuous reusable body has its own activation and guards.", pattern.Body.Circuit.ValueBytes(), 9, "body");
            p.Bits("Independent driver state", "Two connected phase sites; one advances each beat.", pattern.Driver.Circuit.ValueBytes(), 3, "driver");
            p.Bits("Samples [reach, phase, placement]", "Three local 8-bit references per sample. These preserve which frame was current at participation.", pattern.SampleBytes(), pattern.SampleBytes().Length * 8, "frames");
            p.Bits("Translation rows", "[old placement, new placement, shared offset 0]. The offset remains a formal unresolved transform.", pattern.TranslationBytes(), pattern.TranslationBytes().Length * 8, "driver");
            p.Node("body", "Repeating A / B", "Local phase and parameter references recur.", 160, 130, true);
            p.Node("driver", "Independent frame", "Placement advances on the driver's own connected cycle.", 540, 130, true);
            p.Edge("frames", "sample both exposed surfaces", "Binding a recurring local edge to a different placement changes the composed result.", 160, 180, 350, 270);
            p.Edge("frames", "", "No claim of world-space closure follows from a local phase return.", 540, 180, 350, 270);
            p.Node("frames", $"{pattern.SampleCount} bound samples", "Inspect the exact parameter/phase/placement references on the left.", 350, 320, pattern.SampleCount > 0);
            p.Result = $"After {Steps} beats: placement reference {pattern.Placement}, {pattern.SampleCount} participation samples.";
        }
        p.Boundary = "These are executable controller and correspondence structures, not measured frieze trajectories. Up/right offsets, shrinking, actual contact and automatic structure growth still need explicit laws. Moving the frame during a body need not preserve world-space closure.";
        return p;
    }
}
