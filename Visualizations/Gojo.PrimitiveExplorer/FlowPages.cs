using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private ExplorerPage PassagePage(bool conditional)
    {
        var p = new ExplorerPage(conditional ? "A conditional is a choice of connected outlets" : "One flow, two passages",
            conditional ? "A shared control opens one outlet and closes the other. Both outlets have a continuation into the same Done site." : "A passage moves the whole source into an empty receiver. Two rows share the middle site, so contents can continue along the chain.",
            conditional ? "ConditionalExperiments.md" : "PassageExperiments.md");
        Choose(p, "Control read", ["Absent", "Present", "Uncertain"], Control, n => Control = n);
        Choose(p, "Receiver", ["Empty", conditional ? "Yes already occupied" : "Middle already occupied"], Variant, n => Variant = n);
        Stepper(p, 3, conditional ? "Step selection / continuation" : "Step next passage");
        var owner = new PortStructure();
        var count = conditional ? 5 : 4;
        var initial = 1 | (Control == 0 ? 0 : 2) | (Variant == 1 ? 4 : 0);
        var ports = Enumerable.Range(0, count).Select(i => owner.Publish(owner.Create(0, 0, 1, (ulong)((initial >> i) & 1)))).ToArray();
        var present = new PassageConnections(owner, PassageLaw.Controlled());
        var absent = new PassageConnections(owner, PassageLaw.Controlled(false));
        var forward = new PassageConnections(owner, PassageLaw.Unconditional());
        var first = present.Connect(3, 0, 1, 2);
        var second = conditional ? absent.Connect(3, 0, 1, 3) : present.Connect(3, 2, 1, 3);
        var nextYes = conditional ? forward.Connect(3, 2, 1, 4) : default;
        var nextNo = conditional ? forward.Connect(3, 3, 1, 4) : default;
        var visible = ((1 << count) - 1) & (Control == 2 ? ~2 : -1);
        var before = present.ObserveScope(ports, visible);
        var prediction = present.PredictScope(first, ports, before);
        prediction = (conditional ? absent : present).PredictScope(second, ports, prediction);
        var lastAccepted = true;
        for (var i = 0; i < Steps; i++)
        {
            var old = owner.PackedBytes(0, 0, 1)[0];
            if (conditional)
            {
                if (i == 0) lastAccepted = PassageConnections.TrySequence(ports[..4], present.ObserveScope(ports[..4], Control == 2 ? 13 : 15), out _, (present, first), (absent, second));
                else { var row = i == 1 ? nextYes : nextNo; lastAccepted = forward.TryStep(row, forward.Observe(row, Control == 2 ? 5 : 7), out _); }
            }
            else
            {
                var row = i % 2 == 0 ? first : second;
                lastAccepted = present.TryStep(row, present.Observe(row, Control == 2 ? 5 : 7), out _);
            }
            p.Events.Add($"Step {i + 1}: {old:X2} → {owner.PackedBytes(0, 0, 1)[0]:X2}  {(lastAccepted ? "accepted (may hold)" : "unresolved · no write")}");
        }
        var state = owner.PackedBytes(0, 0, 1);
        string[] names = conditional ? ["S", "C", "Yes", "No", "Done"] : ["S", "C", "Middle", "Target"];
        StateBlock(p, "Actual value array", state, names);
        var predBytes = BitConverter.GetBytes(prediction)[..(count == 5 ? 4 : 2)];
        p.Bits("Joint prediction · starting state through both rows", "Bit n admits an ENTIRE site pattern n, not an ODV word. Correlated alternatives stay together; none are executed.", predBytes, 1 << count, "prediction");
        p.Rows("Present-control rows", "[source, control, receiver], each reference three bits.", 3,
            conditional ? [[0, 1, 2]] : [[0, 1, 2], [2, 1, 3]], "present",
            conditional ? [["S", "C", "Yes"]] : [["S", "C", "Middle"], ["Middle", "C", "Target"]], present.PackedRows(3));
        p.Bits("Present transition law · 8 bytes", "Law bit before + 8×after. Local state = source + 2×control + 4×receiver. 3→6 moves; other states hold.", present.LawBytes(), 64, "present");
        if (conditional)
        {
            p.Rows("Absent-control row", "Same three roles; complementary law.", 3, [[0, 1, 3]], "absent", [["S", "C", "No"]], absent.PackedRows(3));
            p.Bits("Absent transition law", "1→4 moves; a blocked selected outlet does not choose the other outlet.", absent.LawBytes(), 64, "absent");
            p.Rows("Continuation rows", "The selected outlet is an ordinary site, now used as a source.", 3, [[2, 1, 4], [3, 1, 4]], "forward", [["Yes", "C", "Done"], ["No", "C", "Done"]], forward.PackedRows(3));
            p.Bits("Unconditional transition law", "Both 1→4 and 3→6 move. This law permits either C value.", forward.LawBytes(), 64, "forward");
        }
        bool Occupied(int site) => (state[0] & (1 << site)) != 0;
        p.Node("S", "Source S", "One store of unmeasured contents. A step cannot copy it into both fresh outlets.", 100, 240, Occupied(0));
        p.Node("C", Control == 2 ? "Control ?" : $"Control {Control}", "The stored C bit and an uncertain READ of C are different. Hiding C does not mutate its stored value.", 350, 65, Occupied(1), Control == 2);
        if (conditional)
        {
            p.Edge("present", "C present", "Row [0,1,2], with the present-control law.", 155, 225, 295, 150, Control == 1, Control == 2);
            p.Edge("absent", "C absent", "Row [0,1,3], with the absent-control law.", 155, 255, 295, 330, Control == 0, Control == 2);
            p.Node("Yes", "Yes", "Receiver of selection; source of its attached continuation.", 350, 140, Occupied(2));
            p.Node("No", "No", "A separate receiver with its own continuation.", 350, 340, Occupied(3));
            p.Node("Done", "Done", "Shared destination. Its occupancy alone does not retain which route was taken.", 600, 240, Occupied(4));
            p.Edge("forward", "continue", "Unconditional Yes→Done. Occupied Done still blocks.", 405, 155, 545, 225);
            p.Edge("forward", "continue", "Unconditional No→Done.", 405, 325, 545, 255);
        }
        else
        {
            p.Node("Middle", "Middle", "The SAME site is receiver of the first row and source of the second.", 350, 240, Occupied(2));
            p.Node("Target", "Target", "A whole handoff needs an empty target.", 600, 240, Occupied(3));
            p.Edge("present", "[0,1,2]", "First controlled passage.", 155, 240, 295, 240, Control == 1, Control == 2);
            p.Edge("present", "[2,1,3]", "Second passage shares the middle site and the same C.", 405, 240, 545, 240, Control == 1, Control == 2);
        }
        p.Label("Filled = actual occupancy     Dashed = unresolved read / possible route", 350, 420);
        p.Result = $"Stored byte {state[0]:X2}. {(Steps == 0 ? "Ready to step." : lastAccepted ? "Last step resolved; a resolved step can hold." : "Last step refused to commit an uncertain observation.")}";
        p.Boundary = "Prediction does not move contents. This owner-local material executor requires a complete resolved observation, even for a continuation whose law accepts either control. The later pulse controller can skip irrelevant guard reads.";
        return p;
    }

    private ExplorerPage SaturationPage()
    {
        var p = new ExplorerPage("When forward is occupied, an outlet can accept flow", "First try the forward receiver. Then use its occupancy as the side gate. This is an explicit ordered route, not spontaneous creation of a new direction.", "SaturationExperiments.md");
        Choose(p, "Initial occupancy", ["Source only", "Source + forward", "Source + forward + side"], Mode, n => Mode = n);
        Choose(p, "Forward read", ["Known", "Uncertain"], Variant, n => Variant = n);
        Stepper(p, 1, "Settle both rows");
        var initial = Mode switch { 0 => 1, 1 => 3, _ => 7 };
        var owner = new PortStructure(); var ports = Enumerable.Range(0, 3).Select(i => owner.Publish(owner.Create(0, 0, 1, (ulong)((initial >> i) & 1)))).ToArray();
        var forward = new PassageConnections(owner, PassageLaw.Unconditional()); var side = new PassageConnections(owner, PassageLaw.Controlled());
        var f = forward.Connect(2, 0, 2, 1); var s = side.Connect(2, 0, 1, 2);
        var before = forward.ObserveScope(ports, Variant == 0 ? 7 : 5);
        var after = side.PredictScope(s, ports, forward.PredictScope(f, ports, before));
        bool? committed = Steps == 0 ? null : PassageConnections.TrySequence(ports, before, out _, (forward, f), (side, s));
        var state = owner.PackedBytes(0, 0, 1);
        StateBlock(p, "Source / forward / side occupancy", state, ["P", "F", "L"]);
        p.Rows("Try forward first", "Unconditional [P,L,F]; L is an irrelevant control role here.", 2, [[0, 2, 1]], "forward", [["P", "L", "F"]], forward.PackedRows(2));
        p.Rows("Then try side", "Controlled [P,F,L]; occupied F opens the side passage.", 2, [[0, 1, 2]], "side", [["P", "F", "L"]], side.PackedRows(2));
        p.Bits("Forward law", "Move whole contents into F only if F is empty.", forward.LawBytes(), 64, "forward");
        p.Bits("Side law", "Move whole contents into L only if F is present and L is empty.", side.LawBytes(), 64, "side");
        p.Bits("Predicted whole states", "Bit n admits the full [P,F,L] occupancy pattern n. Unknown F does not authorize a write.", [(byte)after], 8, "prediction");
        p.Node("P", "Source P", "Unaccepted contents remain here when both receivers are occupied.", 130, 200, (state[0] & 1) != 0);
        p.Node("F", "Forward F", "Its occupancy is both a capacity constraint and the side gate's control input.", 570, 200, (state[0] & 2) != 0, Variant == 1);
        p.Node("L", "Side L", "This is an explicitly attached outlet. Its screen angle is illustrative.", 350, 370, (state[0] & 4) != 0);
        p.Edge("forward", "1. try forward", "The first row runs before the side row; priority is explicit.", 185, 200, 515, 200);
        p.Edge("side", "2. if F occupied", "The second row may transfer remaining source contents to L.", 165, 235, 310, 340);
        p.Result = $"{initial:X2} → {state[0]:X2}. {(committed == false ? "Uncertain observation: no stored bits changed." : "No material is split or duplicated.")}";
        p.Boundary = "L/T/branch layouts fit as explicit passages. Distributed radiation, multiplication of area, automatic hardening and discovered outlet directions remain open laws; they are not simulated here.";
        return p;
    }
}
