using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private ExplorerPage WordPage()
    {
        var p = new ExplorerPage("A distinction, before a yardstick", "Three bits describe a perspective and whether something is there. They do not say how much.", "README.md");
        Choose(p, "Word", WordChoices, A, n => A = n);
        p.Actions.Add(new("Reorient · XOR 110", () => { A = ThreeBitMeasure.Reorient((byte)A); Refresh(); }));
        p.Actions.Add(new("Reverse direction · XOR 010", () => { A = ThreeBitMeasure.ReverseDirection((byte)A); Refresh(); }));
        p.Bits("One O D V word", "Word notation is O D V, high bit first. The strip below follows physical bit offsets, low bit first.", [(byte)A], 3, "word",
            new(0, 1, ["V", "word"], "V · presence"), new(1, 1, ["D", "word"], "D · direction"), new(2, 1, ["O", "word"], "O · orientation"));
        var all = Enumerable.Range(0, 8).Select(i => (byte)i).ToArray();
        p.Bits("All eight words, packed", "Eight 3-bit words take exactly three bytes: 88 C6 FA. No header or per-word ID.", ThreeBitMeasure.Pack(all), 24, "all",
            all.Select((w, i) => new BitMark(i * 3, 3, ["w" + w, "all"], $"word at position {i}: {Word(w)}")).ToArray());
        p.Edge("O", (A & 4) == 0 ? "positive-right" : "positive-left", "O selects the orientation. It does not change magnitude.", 190, 145, (A & 4) == 0 ? 520 : 90, 145, true);
        p.Node("word", Word(A), "The actual stored word. No hidden fine amount is attached.", 350, 250, (A & 1) != 0);
        p.Node("D", (A & 2) == 0 ? "D = 0  /  +" : "D = 1  /  −", "Direction is positive or negative WITHIN this orientation.", 160, 250);
        p.Node("V", (A & 1) == 0 ? "V = 0" : "V = 1", (A & 1) == 0 ? "Exactly zero in this read." : "Some nonzero amount; not the number one.", 540, 250, (A & 1) != 0);
        for (var i = 0; i < 8; i++) p.Node("w" + i, Word(i), $"Stream position {i}; its three bits begin at offset {i * 3}.", 70 + i * 82, 365, i == A);
        p.Result = $"{Word(A)} means {((A & 4) == 0 ? "positive-right" : "positive-left")}, {((A & 2) == 0 ? "positive" : "negative")}, {((A & 1) == 0 ? "zero" : "nonzero")}.";
        p.Boundary = "A nonzero mark has no calibrated size. Different orientations remain incomparable until explicitly reoriented.";
        return p;
    }

    private ExplorerPage PredictionPage()
    {
        var p = new ExplorerPage("What can we know before a merge?", "Addition predicts a set of possible words. A shared word, once settled, can expose two opposed reads without owning two amounts.", "README.md");
        Choose(p, "A", WordChoices, A, n => A = n); Choose(p, "B", WordChoices, B, n => B = n);
        var answers = ThreeBitMeasure.Add((byte)A, (byte)B);
        var support = (byte)answers.Aggregate(0, (s, w) => s | 1 << w);
        p.Bits("Independent input words", "Two words remain two participants. Equal codes do not establish equal hidden magnitudes.", ThreeBitMeasure.Pack([(byte)A, (byte)B]), 6, "inputs",
            new(0, 3, ["A"], "A"), new(3, 3, ["B"], "B"));
        SupportBlock(p, "result", "Predicted addition support", support);
        p.Node("A", "A  " + Word(A), "First independent coarse input.", 160, 100, (A & 1) != 0);
        p.Node("B", "B  " + Word(B), "Second independent coarse input.", 540, 100, (B & 1) != 0);
        p.Edge("result", "read only", "Prediction executes no material merge.", 160, 140, 350, 215, uncertain: answers.Length > 1);
        p.Edge("result", "", "Both input words contribute to the candidate set.", 540, 140, 350, 215, uncertain: answers.Length > 1);
        p.Node("result", answers.Length == 0 ? "No common read" : answers.Length == 1 ? Word(answers[0]) : "Several outcomes", Words(support), 350, 240, uncertain: answers.Length != 1);
        var positive = ThreeBitMeasure.ReadSide((byte)A, false); var negative = ThreeBitMeasure.ReadSide((byte)A, true);
        p.Bits("Opposed reads of A", "DERIVED READS only. Both consult the same A word; no second material store.", ThreeBitMeasure.Pack([positive, negative]), 6, "sides",
            new(0, 3, ["positive", "A"], "positive side of A"), new(3, 3, ["negative", "A"], "negative side of A"));
        p.Node("positive", "+ side " + Word(positive), "Positive-side occupancy of A, calculated from its D and V.", 205, 375, (positive & 1) != 0);
        p.Node("negative", "− side " + Word(negative), "Negative-side occupancy of the SAME A. Both cannot be occupied.", 495, 375, (negative & 1) != 0);
        var order = ThreeBitMeasure.Compare((byte)A, (byte)B);
        p.Result = $"A + B → {Words(support)}. Comparison: {(order is null ? "incomparable orientations" : order == 0 ? "read-equal, not necessarily equal amounts" : order < 0 ? "A < B" : "A > B")}.";
        p.Boundary = "Opposing nonzero inputs require actual settlement or more evidence to choose a sign. The inspector never picks a candidate at random.";
        return p;
    }

    private ExplorerPage StoragePage()
    {
        var p = new ExplorerPage("The location supplies the constants", "A value can gain owned bits while its outside port stays attached. Retired payload remains dirty; it is no longer a live value.", "StablePortExperiment.md");
        Choose(p, "Layout", ["Fixed O / fixed D", "Release direction", "Widen value field", "Release orientation"], Mode, n => Mode = n);
        var owner = new PortStructure(); var binding = owner.Create(0, 1, 1, 1); var port = owner.Publish(binding);
        var addresses = new List<SlotAddress> { owner.Locate(binding) };
        (int O, int D, int W)[] moves = [(0, 2, 2), (0, 2, 3), (2, 0, 4)];
        for (var i = 0; i < Mode; i++) { owner.TryMove(binding, moves[i].O, moves[i].D, moves[i].W); addresses.Add(owner.Locate(binding)); }
        for (var i = 0; i < addresses.Count; i++)
        {
            var a = addresses[i]; var live = owner.TryReadLocation(a, out _); var key = "slot" + i;
            p.Bits($"root[{a.OrientationGroup}] / child[{a.DirectionGroup}] / width {a.Width}",
                $"Entry {a.Index}. {(live ? "LIVE" : "RETIRED · dirty payload")}. Offset = index × width = {PositionalBuckets.BitPosition(a)}. Constants are not stored in these bits.",
                owner.PackedBytes(a.OrientationGroup, a.DirectionGroup, a.Width), a.Width, key);
            p.Node(key, live ? "Live payload" : "Retired payload", $"Local address ({a.OrientationGroup},{a.DirectionGroup},{a.Width},{a.Index}). {(live ? "This is the binding's current destination." : "Old bytes remain, but live reads refuse this slot.")}", 130 + i * 155, 310, live);
        }
        p.Node("port", "Public port 0", "Outside reference: owner + port index. No private bucket address escapes.", 145, 110, true);
        p.Node("binding", "Binding 0", "Owner-local mapping follows migration. These mappings use host references/integers, not a compact wire format.", 365, 110);
        p.Edge("port", "read attachment", p.Details["port"], 205, 110, 305, 110, true);
        p.Edge("slot" + Mode, "current address", "Moving the value retargets this one local binding.", 365, 145, 130 + Mode * 155, 275, true);
        p.Label("root[0] fixes O=0       root[1] fixes O=1       root[2] owns O and D", 350, 400);
        p.Result = $"Port {port} still reads {Word(owner.ReadPort(port))}. The payload changed location and width, not meaning.";
        p.Boundary = "More value bits add capacity, not calibration. Automatic garbage collection and direct packed cross-owner addressing are not implemented.";
        p.Details["state"] = "The containing array supplies fixed O/D. The stored payload cannot overwrite a constant it does not own.";
        return p;
    }

    private ExplorerPage LawPage()
    {
        var p = new ExplorerPage("Wiring says where. The bitmap says what fits.", "The same ordered row can participate in different read laws. A set law bit admits one combination of input and output words.", "ConnectionExperiments.md");
        Choose(p, "Law", ["OR · either present", "XOR · exactly one", "AND · both present", "Coarse addition"], Mode, n => Mode = n);
        Choose(p, "A read", Supports, Control, n => Control = n);
        Choose(p, "B word", WordChoices, B, n => B = n);
        var law = Mode switch { 0 => ConnectionLaws.Presence(), 1 => ConnectionLaws.ExclusivePresence(),
            2 => BitRelation.FromTuples(3, from a in Enumerable.Range(0, 2) from b in Enumerable.Range(0, 2) select new byte[] { (byte)a, (byte)b, (byte)(a & b) }), _ => ConnectionLaws.Addition() };
        var output = law.Predict([Support(Control), ReadSupport.Of((byte)B)]);
        p.Rows("Connection row · 2 bits per reference", "Imported endpoints 0 and 2; calculated endpoint 1. No stored result amount.", 2, [[0, 2, 1]], "wire", [["A", "B", "output"]]);
        SupportBlock(p, "A", "A support", Support(Control)); SupportBlock(p, "B", "B support", ReadSupport.Of((byte)B));
        SupportBlock(p, "output", "Calculated output support", output);
        var lookup = Enumerable.Range(0, 512).Where(bit => (Support(Control) & (1 << (bit & 7))) != 0 && ((bit >> 3) & 7) == B && law.Allows((byte)(bit & 7), (byte)((bit >> 3) & 7), (byte)(bit >> 6))).ToArray();
        p.Bits("Read-law bitmap · 64 bytes", "Bit a + 8b + 64c admits (a,b,c). Hover Allowed tuples to trace just the admitted cells for these inputs.", law.PackedBytes(), 512, "law",
            Enumerable.Range(0, 512).Select(bit => new BitMark(bit, 1, lookup.Contains(bit) ? ["law", "lookup"] : ["law"],
                $"tuple ({Word(bit & 7)}, {Word((bit >> 3) & 7)}, {Word(bit >> 6)}); membership at bit {bit}")).ToArray());
        p.Node("A", "A / endpoint 0", Words(Support(Control)), 145, 120, Control == 1, Control == 2);
        p.Node("B", "B / endpoint 2", Word(B), 145, 320, (B & 1) != 0);
        p.Node("lookup", "Allowed tuples", "Admitted bitmap offsets for these inputs: " + (lookup.Length == 0 ? "none" : string.Join(", ", lookup)) + ". Click to locate their exact law bits. This read moves no material.", 365, 220);
        p.Node("output", "Result / endpoint 1", Words(output), 590, 220, output == 2, ReadSupport.Words(output).Length != 1);
        p.Edge("wire", "input role 0", "First two reference bits name endpoint 0.", 200, 135, 310, 205);
        p.Edge("wire", "input role 1", "Next two bits name endpoint 2.", 200, 305, 310, 235);
        p.Edge("wire", "output role", "Last two reference bits name endpoint 1.", 420, 220, 535, 220);
        p.Result = "Output: " + Words(output) + ". Changing the law changes the read without changing the row.";
        p.Boundary = "The support mask is a set of possibilities, not another value. Boolean presence laws require compatible O/D context; missing or incompatible inputs do not become zero.";
        return p;
    }

    private ExplorerPage RefinementPage()
    {
        var p = new ExplorerPage("More detail, without losing the coarse read", "Two parts can supply a coarse presence read. Shared references also preserve what must be the same observation across several roles.", "ConnectionExperiments.md");
        Choose(p, "Fine occupancy", ["00 · neither", "01 · first only", "10 · second only", "11 · both"], Mode, n => Mode = n);
        Choose(p, "XOR inputs", ["Two distinct uncertain sources", "Same uncertain source twice"], Variant, n => Variant = n);
        var a = (byte)(Mode & 1); var b = (byte)((Mode >> 1) & 1);
        var coarse = ConnectionLaws.Presence().Predict([ReadSupport.Of(a), ReadSupport.Of(b)]);
        var refs = Variant == 0 ? new[] { 0, 2 } : new[] { 0, 0 };
        var xor = ConnectionLaws.ExclusivePresence().Predict([3, 3], refs);
        p.Bits("Fine V bits", "These are two distinguishable parts, not digits of a calibrated binary magnitude.", [(byte)Mode], 2, "parts", new(0, 1, ["a"], "first part"), new(1, 1, ["b"], "second part"));
        p.Rows("Presence correspondence", "Two part endpoints feed one calculated coarse read.", 2, [[0, 2, 1]], "presence", [["a", "b", "coarse"]]);
        SupportBlock(p, "coarse", "Coarse presence support", coarse);
        p.Rows("XOR correspondence", "Repeated endpoint references must choose the same candidate word.", 2, [[refs[0], refs[1], 1]], "xor", [["input", "input", "xor"]]);
        SupportBlock(p, "xor", "XOR result of uncertain inputs", xor);
        p.Node("a", $"Part A · V={a}", "A distinguishable finer participation.", 165, 100, a != 0);
        p.Node("b", $"Part B · V={b}", "B remains separately readable.", 535, 100, b != 0);
        p.Edge("presence", "", "Coarse presence OR.", 165, 140, 350, 220);
        p.Edge("presence", "", "No finer pattern is invented by the coarse result.", 535, 140, 350, 220);
        p.Node("coarse", "Coarse " + Words(coarse), "Presence 1 permits fine patterns 01, 10 or 11. It cannot choose one.", 350, 250, coarse == 2);
        p.Node("input", Variant == 0 ? "Two unknown reads" : "One read, used twice", "Each published input admits 000 or 001. Only an actual shared reference enforces correspondence.", 190, 380, uncertain: true);
        p.Node("xor", "XOR → " + Words(xor), "The same observation XOR itself is empty at this presence read. Two equally uncertain sources may differ.", 530, 380, uncertain: xor == 3);
        p.Result = $"Fine pattern {Convert.ToString(Mode, 2).PadLeft(2, '0')} → coarse {Words(coarse)}. XOR → {Words(xor)}.";
        p.Boundary = "This is an occupancy ladder, not a calibrated magnitude ladder. Shared observed presence does not establish exact hidden amount equality.";
        return p;
    }

    private ExplorerPage OppositionPage()
    {
        var p = new ExplorerPage("Zero net can still contain participation", "The fine demonstration world settles opposing flows while retaining the matched portions. The primitive encoding only sees occupancy.", "ConnectionExperiments.md") { Status = "Fine apparatus + coarse observation" };
        Choose(p, "Demonstration amounts", ["+5 with −2", "+5 with −5", "+2 with −5"], Mode, n => Mode = n);
        Stepper(p, 1, "Settle opposed participation");
        long[] cells = Mode switch { 0 => [5, 2, 0, 0], 1 => [5, 5, 0, 0], _ => [2, 5, 0, 0] };
        var rows = new EndpointRows(); var row = rows.Append(2, 0, 1, 2, 3);
        if (Steps > 0) OpposedConnectionFixture.Settle(cells, rows, row);
        var occupancy = (byte)cells.Select((v, i) => v != 0 ? 1 << i : 0).Sum();
        StateBlock(p, "Four occupancy bits · fixed signs by role", [occupancy], ["P", "N", "Rp", "Rn"]);
        // Override generic context note: these four roles explicitly have opposed signs.
        p.Blocks[0] = p.Blocks[0] with { Note = "Coarse observation: [active +, active −, retained +, retained −]. O/D are role context, not all positive." };
        p.Rows("Four-ended retention row", "No third net amount is stored in this connector.", 2, [[0, 1, 2, 3]], "route", [["P", "N", "Rp", "Rn"]], rows.PackedBytes(4, 2));
        string[] keys = ["P", "N", "Rp", "Rn"];
        for (var i = 0; i < 4; i++) p.Node(keys[i], keys[i] + $" · V={(cells[i] == 0 ? 0 : 1)}", $"Fine apparatus contains {cells[i]}; this number is NOT stored beside the primitive bit.", 190 + i % 2 * 320, 130 + i / 2 * 200, cells[i] != 0);
        p.Edge("route", "matched +", "Matching positive material is transferred to retained +.", 190, 170, 190, 290, Steps > 0);
        p.Edge("route", "matched −", "Matching negative material is separately retained. Nothing is copied.", 510, 170, 510, 290, Steps > 0);
        p.Result = $"Apparatus [P,N,Rp,Rn] = [{string.Join(',', cells)}]. Net = {cells[0] + cells[2] - cells[1] - cells[3]}; gross participation = {cells.Sum()}.";
        p.Boundary = "A single net ODV word cannot reconstruct this pair. The fine amounts are explicit demonstration apparatus, never hidden extra precision in the coarse predictor.";
        return p;
    }

    private ExplorerPage MemoryPage()
    {
        var p = new ExplorerPage("Zero need not erase direction memory", "This fixed feedback law keeps the previous D bit when the new observation resolves no magnitude. A later nonzero input can change it.", "EncodingChart.md");
        Choose(p, "Previous direction", ["Positive", "Negative"], Mode, n => Mode = n);
        Choose(p, "Observation", ["Zero · 000", "Positive nonzero · 001", "Negative nonzero · 011"], Variant, n => Variant = n);
        Stepper(p, 1, "Apply feedback");
        byte observed = Variant switch { 1 => 1, 2 => 3, _ => 0 };
        var previous = (byte)(Mode == 0 ? 1 : 3); byte[] frame = [(byte)(observed | previous << 3)];
        if (Steps > 0) DirectionMemory.UpdateFrame(frame);
        var memory = (byte)(frame[0] >> 3);
        p.Bits("Input + memory frame", "Input in bits 0–2, memory in bits 3–5. Only memory is changed by this step.", frame, 6, "frame", new(0, 3, ["input"], "observed ODV"), new(3, 3, ["memory"], "retained ODV"));
        p.Node("input", "Input " + Word(observed), "Exact zero here. A threshold reader can separately expose unresolved magnitude, but does not redefine the base V bit.", 170, 200, observed != 0);
        p.Node("memory", "Memory " + Word(memory), "D is retained at zero. This is controller state, not a duplicate material store.", 530, 200, (memory & 1) != 0);
        p.Edge("frame", "V selects fresh D or previous D", "Update(previous,input) uses previous D only when input V=0.", 225, 200, 475, 200, Steps > 0);
        p.Edge("memory", "feedback", "The caller explicitly keeps the returned word for the next observation.", 530, 250, 530, 350);
        p.Result = $"{Word(previous)} with input {Word(observed)} {(Steps == 0 ? "would update to" : "updated to")} {Word(DirectionMemory.Update(previous, observed)!.Value)}.";
        p.Boundary = "Resolution alone is not hysteresis. Retaining the previous direction requires state and an explicit feedback rule.";
        return p;
    }

    private ExplorerPage PotentialPage()
    {
        var p = new ExplorerPage("Equal readings do not erase the structure", "One site, two distinct sites, and one site referenced twice are different constructions—even when every value reads zero.", "ConnectionExperiments.md");
        Choose(p, "Construction", ["One site", "Two distinct zero sites", "One site in two roles"], Mode, n => Mode = n);
        int[] endpoints = Mode switch { 0 => [0], 1 => [0, 1], _ => [0, 0] };
        p.Bits("Site values", "Zero is an observation. It does not delete endpoints or infer coordinate coincidence.", [0], Mode == 1 ? 2 : 1, "values");
        p.Rows("Ordered roles", "Repeated addresses remain repeated; equal values at separate addresses remain separate.", 1, [endpoints], "roles");
        p.Node("values", "Site 0 · zero", "An existing participant with a zero read.", Mode == 0 ? 350 : 190, 220);
        if (Mode == 1) { p.Node("values", "Site 1 · zero", "A second participant. Its zero word is not proof of shared identity or geometric location.", 510, 220); p.Edge("roles", "0 → 1", "Two distinct endpoint references preserve a possible relation even at zero.", 245, 220, 455, 220); }
        if (Mode == 2) { p.Node("roles", "Role 0", "First use of endpoint 0.", 510, 120); p.Node("roles", "Role 1", "Second use of that SAME endpoint 0.", 510, 320); p.Edge("roles", "0", "Both roles refer to one site.", 245, 200, 455, 130); p.Edge("roles", "0", "Repeated participation is visible in the row.", 245, 240, 455, 310); }
        p.Result = Mode switch { 0 => "One participant; no ordered pair has been encoded.", 1 => "Two participants; their relationship survives equal zero reads.", _ => "Two roles, one participant. Repetition is preserved without inventing a second site." };
        p.Boundary = "These rows preserve structural potential. Radial fields, a geometric line and an orthogonal direction need additional connected laws; the zero bits alone supply none of them.";
        return p;
    }
}
