using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private static void InkBlocks(ExplorerPage page, StrokeGraph ink)
    {
        for (var i = 0; i < ink.RunCount; i++)
        {
            var run = ink.Run(i);
            var sites = run.Sites();
            page.Rows($"Run {i} · " + (run.IsChain ? "ordered landmarks" : "member sites"),
                run.IsChain ? "Successive fields follow the selected run." : "Field order here supplies membership only. Read the comparison rows below.",
                run.Bits, sites.Select(s => new[] { s }).ToArray(), "run" + i,
                sites.Select(s => new[] { "site" + s }).ToArray(), run.SiteBytes());
            if (run.ClaimCount > 0)
                page.Rows($"Run {i} · strict precedence", "Each row says first site precedes second on this run.", run.Bits, run.Claims(), "order", actual: run.ClaimBytes());
        }
        var events = ink.PenUses();
        page.Bits("Writing history", "High bits select a run. Bit 1 starts a pen stroke. Bit 0 reverses that run’s traversal.",
            ink.PenBytes(), ink.PenBits * events.Length, "pen", events.Select((code, i) => new BitMark(i * ink.PenBits,
                ink.PenBits, ["pen", "run" + (code >> 2)], $"Event {i} uses run {code >> 2}, start={(code >> 1) & 1}, reverse={code & 1}")).ToArray());
        if (ink.AxisCount != 0)
            page.Rows("Local ground correspondence", "Each row binds an ink run to the supplied along (0) or perpendicular (1) axis.", ink.RunBits,
                ink.GroundLinks(), "ground", actual: ink.GroundBytes());
    }

    private ExplorerPage CapitalPage()
    {
        var p = new ExplorerPage("Which capitals can a stroke graph distinguish?",
            "Rotate the illustration with the ground unbound. The reader sees shared sites and ordered runs. On N or Z, try attaching a local ground.", "FrameFreeCapitals.md");
        Choose(p, "Construction", CapitalStudy.Labels.ToArray(), Mode, n => Mode = n);
        Choose(p, "Rotate illustration", ["Original", "Quarter turn", "Half turn", "Three quarters"], Variant, n => Variant = n);
        Choose(p, "Local ground", ["Attach along / perpendicular", "Unbound"], Control, n => Control = n);
        var label = CapitalStudy.Labels[Mode];
        var ink = CapitalStudy.Build(label, ground: Control == 0);
        InkBlocks(p, ink);
        var candidates = CapitalStudy.Candidates(ink);
        var counts = ink.Incidence();
        // One possible realization in screen coordinates. N rotated a quarter turn
        // uses precisely the Z drawing. Coordinates never enter the experiment.
        var points = CapitalDrawing(label);
        (int X, int Y) Screen(int x, int y)
        {
            for (var turn = 0; turn < Variant; turn++) (x,y) = (-y,x);
            return (200 + x, 220 + y);
        }
        p.Label("One possible rendering", 200, 43);
        for (var r = 0; r < ink.RunCount; r++)
        {
            var sites = ink.Run(r).Sites();
            for (var i = 1; i < sites.Length; i++)
            {
                var a = points[sites[i-1]]; var b = points[sites[i]];
                var from = Screen(a.X, a.Y); var to = Screen(b.X, b.Y);
                p.Stroke("run" + r, $"Run {r}: site {sites[i-1]} to site {sites[i]}.", from.X, from.Y, to.X, to.Y, false);
            }
        }
        for (var i = 0; i < points.Length; i++)
        {
            var at = Screen(points[i].X, points[i].Y);
            p.Circle("site" + i, $"Local site {i}; every occurrence of this reference meets here.", at.X, at.Y, 4);
            p.Label(i.ToString(), at.X + 13, at.Y - 17);
        }
        if (Control == 0)
        {
            var a = Screen(-95,117); var b = Screen(95,117);
            p.Edge("ground", "", "Selected local ground. The dashed construction arrow travels with the illustration.", a.X, a.Y, b.X, b.Y, uncertain: true);
        }
        p.Node("matches", string.Join(" or ", candidates), "Compatible reference constructions from the five-entry roster.", 530, 146, true);
        var strokes = ink.PenUses().Count(e => (e & 2) != 0);
        p.Label($"{ink.RunCount} straight runs · {strokes} pen {(strokes == 1 ? "stroke" : "strokes")}", 530, 207);
        p.Label($"{counts.SharedSites} shared sites · {counts.Cycles} independent cycles", 530, 240);
        p.Label(Control == 0 ? "Local ground supplied" : "No ground supplied", 530, 276);
        p.Result = $"Compatible among H, A, E, N and Z: {string.Join(" or ", candidates)}.";
        p.Boundary = "Matching preserves shared identity and landmark order while permitting run reversal and renumbering. Ink contains no box, lengths or screen coordinates.";
        p.Events.Add("Ink runs: " + string.Join("   ", Enumerable.Range(0, ink.RunCount).Select(r => "[" + string.Join(",", ink.Run(r).Sites()) + "]")));
        return p;
    }

    private ExplorerPage LandmarkPage()
    {
        var p = new ExplorerPage("E has a spatial order and a writing order",
            "Withdraw one spatial comparison, then switch writers. The two possible spine orders survive either writing history.", "FrameFreeCapitals.md");
        Choose(p, "Spine evidence", ["Known chain P Q R", "P before Q and R", "Conflicting P/Q claims"], Mode, n => Mode = n);
        Choose(p, "Writing history", ["Spine then arms P Q R", "Arms R Q P then spine"], Variant, n => Variant = n);
        Choose(p, "Read direction", ["Reversed spine", "Authored spine"], Control, n => Control = n);
        var ink = CapitalStudy.Build("E", otherHand: Variant != 0, partialE: Mode == 1);
        if (Mode == 2)
            ink = new StrokeGraph([new LandmarkRun(3, [2,0,1], [[0,1],[1,0],[0,2]]), ink.Run(1), ink.Run(2), ink.Run(3)], ink.PenUses(), []);
        InkBlocks(p, ink);
        var spine = ink.Run(0);
        var orders = spine.Orders().ToArray();
        static char Mark(int site) => "PQR"[site];
        for (var n = 0; n < orders.Length; n++)
        {
            var order = orders[n]; int x = orders.Length == 1 ? 287 : 92 + n * 360;
            p.Label(orders.Length == 1 ? "Only admitted order" : $"Admitted order {n+1}", x + 74, 47);
            p.Stroke("run0", "One possible traversal of the spine.", x, 116, x, 314, false);
            for (var level = 0; level < order.Length; level++)
            {
                var site = order[level]; int y = 116 + level * 99;
                p.Stroke("run" + (site + 1), $"Arm attached at {Mark(site)}.", x, y, x + 148, y, false);
                p.Circle("site" + site, $"Landmark {Mark(site)}.", x, y, 5);
                p.Label(Mark(site).ToString(), x - 20, y - 6);
            }
            p.Label(string.Join(" → ", order.Select(Mark)), x + 72, 344);
        }
        if (orders.Length == 0) p.Node("order", "P < Q and Q < P", "The strict claims form a cycle in the same basis.", 355, 215, uncertain: true);
        var candidates = CapitalStudy.Candidates(ink);
        p.Node("pen", string.Join(" | ", ink.PenUses().Select(e => (e >> 2) == 0 ? "spine" : Mark((e >> 2)-1).ToString())),
            "Each separator starts another pen stroke in this authored example.", 355, 414);
        p.Result = $"Q relative to R: {spine.Compare(1,2, Control == 0)}. P relative to Q: {spine.Compare(0,1, Control == 0)}. Compatible: {(candidates.Length == 0 ? "none" : string.Join(" or ",candidates))}.";
        p.Boundary = "The drawings show completions in the authored traversal. Reversing a read changes its answer. The writing tape records run visits and pen lifts independently.";
        return p;
    }
}
