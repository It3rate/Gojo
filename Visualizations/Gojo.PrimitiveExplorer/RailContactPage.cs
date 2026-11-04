using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private ExplorerPage RailContactPage()
    {
        var p = new ExplorerPage("When two rails meet",
            "Seat a second stop and see whether a receiver can draw from a collecting Y.", "RailContacts.md")
        {
            DrawingGuide = ("Two local orders", "Follow each rail's arrows. A line between rails joins seated stops in one socket. Screen spacing carries no length evidence.")
        };
        Choose(p, "Construction", ["Across a latch", "Inside bracket", "B reversed", "Unknown A order", "Different starts", "Tips meet"], Mode, n => Mode = n, true);
        Choose(p, "Sockets", ["Feet meet", "Two contacts", "Empty", "A half only", "Foot twice", "Contradiction"], Variant, n => Variant = n);
        Choose(p, "Permit draw if", ["Either is longer", "A is longer", "B is longer", "Equal extent"], Control, n => Control = n);
        Stepper(p, 3, "Next request");

        int[] privateA = [2,0,3,1], privateB = [1,3,0,2];
        int[] bOrder = Mode switch { 1 => [0,1,2,3], 2 => [3,1,2,0], _ => [0,2,1,3] };
        int[][] aEdges = Mode == 3 ? [[0,1],[0,2],[1,3],[2,3]] : [[0,1],[1,2],[2,3]];
        var a = new LandmarkRun(2, privateA, Mode == 3 ? aEdges.Select(edge => edge.Select(i => privateA[i]).ToArray()).ToArray() : null);
        var b = new LandmarkRun(2, bOrder.Select(i => privateB[i]).ToArray());
        var dock = new RailContacts(new(a, privateA), new(b, privateB));
        var probe = Mode == 4 ? dock.Compare((0,2),(2,3)) : dock.Compare((0,1),(0,1));
        int foot = dock.OpenSocket(), latch = dock.OpenSocket();
        if (Variant != 2) dock.SeatLeft(foot, 0);
        if (Variant is not (2 or 3)) dock.SeatRight(foot, 0);
        if (Variant is 1 or 5)
        {
            dock.SeatLeft(latch, Mode == 5 ? 1 : 2);
            dock.SeatRight(latch, Mode is 3 or 5 ? 1 : 2);
        }
        if (Variant == 4) { dock.SeatLeft(latch, 0); dock.SeatRight(latch, 0); }
        if (Variant == 5) dock.Touch(0,1);
        var read = probe.Inspect();
        var links = new PairedLinks(2, (3,0), (3,1), probe.Map(0x24));
        var winners = links.From(3);
        byte[] truths = [6,2,4,1];
        var y = GlyphJunction.YCollect(3);
        y.Advance();
        var drawer = new ObservedDraw(y, winners.Test(truths[Control]));
        SeamStep[]? last = null;
        int requests = Math.Min(3, Steps);
        for (int i = 0; i < requests; i++)
        {
            if (i == 2) drawer.Park();
            last = drawer.Request();
        }

        p.Rows("Socket seats", "Left and right public stop plus one. Zero is an open half.", dock.SeatBits,
            Enumerable.Range(0, dock.SocketCount).Select(dock.SeatCodes).ToArray(), "socket", actual: dock.Bytes());
        int[][] ends = Mode == 4 ? [[0,2],[2,3]] : [[0,1],[0,1]];
        p.Rows("Two occupied spans", "Start and tip on each owner's port surface.", dock.Bits, ends, "span", actual: probe.Bytes());
        p.Bits("Admitted orientations", "Low bit follows B's local order. Next bit reverses it.", [read.Faces], 2, "socket");
        p.Bits("Tip order", "Neither, A farther along the diagram's A frame, B farther, coincident.", [read.TipPairs], 4, "span");
        p.Bits("Extent order", "Neither, A longer, B longer, equal extent.", [read.ExtentPairs], 4, "extent");
        p.Bits("Projected intersection", "Bits 0, 1 and 3 admit disjoint, one position and positive overlap.", [read.IntersectionPairs], 4, "extent");
        p.Bits("Longer-span links", "Neither wins a tie. One or both alternatives may remain possible.", [winners.ReadPairs()], 4, "extent");
        p.Bits("Collecting Y", "The experimenter can inspect the two receiving slots at bits 2 and 4.", [y.Flow.Stored], 5, "source");
        p.Bits("Receiving owner", "Last sampled block, inbox, shelf.", [drawer.Stored], 3, "inbox");

        double[] ax = [88,256,437,602], ay = [90,90,90,90], bx = new double[4];
        if (Mode == 3) { ax[1] = ax[2] = 340; ay[1] = 58; ay[2] = 128; }
        for (int i = 0; i < bOrder.Length; i++) bx[bOrder[i]] = 88 + 171*i;
        void OrderArrow(double x1, double y1, double x2, double y2)
        {
            // Stop before the label box so the order arrow remains visible.
            double endX = x2 - 42;
            p.Edge("span", "", "Strict precedence published by this owner.", x1, y1,
                endX, y1 + (y2-y1) * (endX-x1) / (x2-x1));
        }
        foreach (var edge in aEdges)
            OrderArrow(ax[edge[0]], ay[edge[0]], ax[edge[1]], ay[edge[1]]);
        for (int i = 1; i < bOrder.Length; i++)
            OrderArrow(bx[bOrder[i-1]], 238, bx[bOrder[i]], 238);
        for (int i = 0; i < dock.SocketCount; i++)
        {
            var codes = dock.SeatCodes(i);
            if (codes[0] > 0 && codes[1] > 0)
                p.Stroke("socket", "Two occupied halves witness one projected position.", ax[codes[0]-1], ay[codes[0]-1]+18, bx[codes[1]-1], 220, true);
            else if (codes[0] > 0)
                p.Stroke("socket", "Only A is seated in this socket.", ax[codes[0]-1], ay[codes[0]-1]+18, ax[codes[0]-1], 159, true);
        }
        for (int i = 0; i < 4; i++)
        {
            string Label(string owner, int[] span) => owner+i+(i == span[0] ? " start" : i == span[1] ? " tip" : "");
            p.Node("span", Label("A", ends[0]), "A public stop.", ax[i], ay[i]);
            p.Node("span", Label("B", ends[1]), "B public stop.", bx[i], 238);
        }
        string extent = read.ExtentPairs switch { 0 when read.Contacts == 0 => "No shared reference", 0 => "Conflicting contacts",
            2 => "A is longer", 4 => "B is longer", 6 => "Exactly one is longer", 8 => "Equal extent", _ => "Shorter, equal or longer" };
        p.Node("extent", extent, "Published order of the two spans.", 345, 312, read.ExtentPairs != 0);
        p.Node("source", y.Stem.ReadSupport() == 2 ? "Y reports present" : "Y reports empty", "Non-draining stem observation.", 161, 385);
        p.Edge("inbox", "", "Whole material handoff.", 244, 385, 462, 385);
        p.Node("inbox", (drawer.Stored & 2) != 0 ? "Inbox occupied" : "Inbox empty", "Received material.", 535, 385, (drawer.Stored & 2) != 0);
        p.Result = extent + ". " + (requests == 0 ? "No draw requested." : last is null ? "Draw deferred." :
            last.Any(s => s.Transfer.Moved) ? "One arrival delivered." : "Draw held.");
        p.Boundary = "Both owners offer straight ordered positions on one docking line. Stop spacing is uncalibrated.";
        p.Events.Add("First request tries to fill the inbox. The second retries. The third parks the inbox contents before trying again.");
        return p;
    }
}
