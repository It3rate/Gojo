using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private ExplorerPage ForkPage()
    {
        var p = new ExplorerPage("When an exit closes after the report",
            "Step the fork. Follow its pulse from waiting to an offer, across the seam into an inbox, and on to arrival. Try the same input with a wire removed.", "ForkExperiment.md");
        Choose(p, "External input", ["Close left on 2, release on 4", "Close left on 2 and hold", "Both exits stay free",
            "Only right starts free", "Both start blocked"], Mode, n => Mode = n);
        Choose(p, "Connections", ["All connected", "Cut left report to fork", "Cut left material seam", "Cut arrival reply"], Variant, n => Variant = n);
        Choose(p, "Refusal meter", ["Disconnected", "Connected"], Control, n => Control = n);
        Stepper(p, 48);
        ForkTape[] inputs = [new(false, false, 2, 4), new(false, false, 2), new(false, false), new(true, false), new(true, true)];
        var tape = inputs[Mode];
        var machine = new ForkFixture(tape.LeftBusy, tape.RightBusy, connectLeft: Variant != 2, connectReceipt: Variant != 3, advertiseLeft: Variant != 1);
        var counter = Control == 1 ? machine.AttachRefusalCounter() : null;
        var frames = new List<ForkFrame> { machine.Snapshot() };
        for (var beat = 1; beat <= Steps; beat++)
        {
            tape.Apply(machine, beat);
            frames.Add(machine.Advance());
            counter?.Sample();
        }
        var frame = frames[^1];
        var counts = ForkCounts.From(frames);
        string[] forkKeys = ["waiting", "offerL", "offerR", "leftReport", "rightReport"];
        string[] exitKeys = ["gateL", "gateR", "boxL", "boxR", "endL", "endR", "reserve", "clock"];
        StateBlock(p, "Junction · material and held guard reads", [frame.Fork], forkKeys);
        StateBlock(p, "Exit owner · gates, inboxes, ends, blocker reserve, clock", [frame.Exits], exitKeys);
        StateBlock(p, "Arrival reply · waiting, seen, sampled guard", [frame.Receipt], ["reply", "seen", "arrived"]);
        if (counter != null) StateBlock(p, "Separate refusal meter · zero, one, two or more, guard", counter.Bytes(), ["meter", "meter", "meter", "refused"]);
        p.Rows("Junction · controlled passage rows", "Each source/control/target row draws from the one waiting site.",
            3, [[0,3,1], [0,4,2]], "fork", [["waiting","leftReport","offerL"], ["waiting","rightReport","offerR"]], machine.RouteRows());
        p.Rows("Receiver · vacancy observations", "Gate clear AND inbox empty; calculated results are endpoints 1 and 3.",
            3, [[0,2,1], [4,6,3]], "vacancy", actual: machine.VacancyRows());
        p.Rows("Right offer guard", "Right free AND left unavailable. The preference is in this connected table.", 3, [[1,3,5]], "rightReport", actual: machine.FallbackRows());
        int[][] wires = Variant == 2 ? [[1,1]] : [[0,0],[1,1]];
        p.Rows("Seams · outlet and intake positions", "Each intake binds its live gate and inbox. A read attachment cannot perform the paired transfer.", 1,
            wires, "seams", actual: machine.SeamRows(), rowKeys: wires.Select(row => row[0] == 0 ? "seamL" : "seamR").ToArray());
        p.Bits("Route law", "A set bit admits a complete before/after state for S,C,T.", machine.RouteLaw(), 64, "fork");
        p.Bits("Seam law", "Admission when C=0 and T=0. The paired before/after transition accounts for both owners.", machine.SeamLaw(), 64, "seams");
        p.Bits("Vacancy read law", "Bit gate + 8 × inbox + 64 × answer admits that tuple.", machine.VacancyLaw(), 512, "vacancy");
        p.Bits("Fallback read law", "Bit leftFree + 8 × rightFree + 64 × answer admits that tuple.", machine.FallbackLaw(), 512, "rightReport");

        p.Label("JUNCTION", 140, 40); p.Label("EXIT OWNER", 510, 40);
        if (Variant != 1) p.Edge("leftReport", "left free read", "This public observation feeds the left offer guard.", 445, 80, 220, 80);
        p.Edge("rightReport", "right free and left unavailable", "Both exposed vacancy reads feed this calculated guard.", 445, 245, 220, 245);
        p.Node("waiting", "Waiting", "The junction initially holds one whole pulse.", 62, 208, (frame.Fork & 1) != 0);
        for (var lane = 0; lane < 2; lane++)
        {
            var suffix = lane == 0 ? "L" : "R";
            var y = lane == 0 ? 120 : 305;
            p.Edge("fork", "", "The guard controls a local whole handoff.", 100, 208, 172, y);
            p.Node("offer" + suffix, "Offer " + suffix, "This stored offer remains here through a refusal.", 220, y, (frame.Fork & (2 << lane)) != 0);
            if (lane != 0 || Variant != 2)
                p.Edge("seam" + suffix, "seam", "Owner-to-owner whole handoff. Both owners recheck their published material sites together.", 270, y, 395, y);
            p.Node("box" + suffix, "Inbox " + suffix, "Newly accepted pulse waits here until the next local delivery opportunity.", 445, y, (frame.Exits & (4 << lane)) != 0);
            p.Edge("delivery" + suffix, "", "Separate unconditional local passage.", 495, y, 572, y);
            p.Node("end" + suffix, "End " + suffix, "An arrival can raise the public reply.", 620, y, (frame.Exits & (16 << lane)) != 0);
            p.Label((frame.Exits & (1 << lane)) != 0 ? "Gate occupied" : "Gate clear", 445, y + 34);
        }
        p.Node("seen", (frame.Receipt & 2) != 0 ? "Reply received" : "Awaiting reply", "A separate signal circuit observes the exposed ends.", 217, 414, (frame.Receipt & 2) != 0);
        if (Variant != 3) p.Edge("arrived", "either end occupied", "Observation of the two ends feeds the reply latch.", 620, 365, 258, 399);
        if (counter != null) p.Node("meter", $"Refusals · {(counter.Reading == 2 ? "2+" : counter.Reading)}", "One-way observation of actual offered-but-held seam transitions.", 511, 414, counter.Reading > 0);
        p.Result = $"Beat {frame.Beat} · retrospective counts: {counts.Offers} offers tested, {counts.Refusals} refused, {counts.Accepted} accepted, {counts.Arrivals} arrived.";
        p.Boundary = "Only one material pulse participates. Gate obstacles, guard observations, reply and meter activation have separate sites. The apparatus supplies the clock and the announced gate changes.";
        foreach (var past in frames.TakeLast(3))
            p.Events.Add($"beat {past.Beat}: fork {past.Fork:X2} · exits {past.Exits:X2} · reply {past.Receipt:X2} · " +
                string.Join("  ", past.Transfers.Where(s => s.Transfer.Offered).Select(s => $"{(s.Outlet == 0 ? "L" : "R")} {s.Transfer.Before}→{s.Transfer.After}")));
        return p;
    }
}
