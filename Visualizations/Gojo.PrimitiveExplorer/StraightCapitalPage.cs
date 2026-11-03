using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    // The atlas and its published notes read the same verbal recipes.
    private static readonly Lazy<Dictionary<string, string>> CapitalRecipes = new(ReadCapitalRecipes);

    private static Dictionary<string, string> ReadCapitalRecipes()
    {
        using var notes = new StreamReader(typeof(ExplorerSession).Assembly.GetManifestResourceStream("StraightCapitals.md")!);
        var recipes = new Dictionary<string, string>();
        while (notes.ReadLine() is { } heading && heading != "## How to build each letter") { }
        while (notes.ReadLine() is { } line && !line.StartsWith("## ", StringComparison.Ordinal))
        {
            if (!line.StartsWith("- **", StringComparison.Ordinal)) continue;
            var end = line.IndexOf("** ", 4, StringComparison.Ordinal);
            if (end > 4) recipes.Add(line[4..end], line[(end + 3)..]);
        }
        return recipes;
    }

    // Presentation only. The primitive project cannot access this realization.
    private static (int X, int Y)[] CapitalDrawing(string name) => name switch
    {
        "A" => [(0,-90),(-35,0),(-70,90),(35,0),(70,90)],
        "E" => [(-60,-90),(-60,0),(-60,90),(65,-90),(45,0),(65,90)],
        "F" => [(-60,-90),(-60,0),(-60,90),(65,-90),(45,0)],
        "H" => [(-66,-90),(-66,0),(-66,90),(66,-90),(66,0),(66,90)],
        "I" => [(0,-90),(0,90)],
        "Barred I" => [(-45,-90),(0,-90),(45,-90),(-45,90),(0,90),(45,90)],
        "K" => [(-60,-90),(-60,0),(-60,90),(65,-90),(65,90)],
        "L" => [(-60,-90),(-60,90),(65,90)],
        "M" => [(-70,90),(-70,-90),(0,25),(70,-90),(70,90)],
        "N" => [(-66,90),(-66,-90),(66,90),(66,-90)],
        "T" => [(-70,-90),(0,-90),(70,-90),(0,90)],
        "V" => [(-70,-90),(0,90),(70,-90)],
        "W" => [(-85,-90),(-43,90),(0,-32),(43,90),(85,-90)],
        "X" => [(-65,-90),(0,0),(65,90),(-65,90),(0,0),(65,-90)],
        "Joined X" => [(-65,-90),(0,0),(65,90),(-65,90),(65,-90)],
        "Y" => [(-65,-90),(0,0),(65,-90),(0,90)],
        "Z" => [(-90,-66),(90,-66),(-90,66),(90,66)],
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    private ExplorerPage StraightCapitalPage()
    {
        var name = StraightCapitals.Names[Mode];
        var recipes = CapitalRecipes.Value;
        var p = new ExplorerPage("The straight capital atlas", $"{name}  ·  {recipes[name]}", "StraightCapitals.md")
        {
            Status = "Click a letter in the grid. In Construction, use ← or ↑ for the previous form and → or ↓ for the next."
        };
        var construction = Choose(p, "Construction", StraightCapitals.Names.ToArray(), Mode, n => Mode = n, browseWithArrows: true);
        Choose(p, "Inspect", ["Whole glyph", "Joints and crossings", "Trace closed walks"], Variant, n => Variant = n);
        Choose(p, "Letter convention", ["Withdraw ground", "Bottom-left · positive right"], Control, n => Control = n);
        var ink = StraightCapitals.Build(name, Control == 1);
        var layer = new StrokeFeatures(ink);
        var candidates = StraightCapitals.Candidates(ink);
        var loopCandidates = StraightCapitals.CandidatesByLoop(layer.Loops.Count != 0);
        var points = CapitalDrawing(name);
        const int cx = 174, cy = 202;
        var corners = layer.Joints.Count(j => j.ProvedCorner);
        var branches = layer.Joints.Count(j => j.Arms >= 3);
        var crossings = ink.ProjectedCrossings();
        p.Bits("Higher read · contains a loop", "Bit 1 admits a present word, bit 0 an absent word. This public read can guard a passage.", [layer.ReadSupport()], 8, "loop");
        foreach (var loop in layer.Loops)
            p.Rows("Loop witness · signed span uses", "Each row selects run, span slot and reverse bit. The last arrival is the first departure.", loop.Bits,
                loop.Walk.Select(e => new[] { e.Run,e.Slot,e.Reversed ? 1 : 0 }).ToArray(), "loop", actual: loop.Bytes(),
                rowKeys: loop.Walk.Select(e => $"span{e.Run}.{e.Slot}").ToArray());
        if (crossings.Length > 0)
            p.Rows("Projected crossing", "Two separate interior sites have one projected position.", ink.SiteBits, crossings, "cross", actual: ink.CrossingBytes());
        if (ink.Frame is { } frame)
        {
            p.Rows("Construction axes", "0→1 is positive right. 0→2 is positive up. Both start at the common zero.", 2, [[0,1],[0,2]], "zero", actual: frame.AxisBytes());
            p.Rows("Directed changes in X and Y", "Along each authored run: 0 unchanged, 1 increasing, 3 decreasing. These say only which way.", 2,
                Enumerable.Range(0,ink.RunCount).Select(r => frame.Motion(r)).ToArray(), "motion", actual: frame.MotionBytes(),
                rowKeys: Enumerable.Range(0,ink.RunCount).Select(r => "run" + r).ToArray());
            var zero = frame.ZeroLinks();
            p.Rows("Contacts with zero", "Site and projected axis. X=0 lies on the left, Y=0 on the bottom.", frame.ContactBits, zero, "zero",
                roleKeys: zero.Select(row => new[] { "site" + row[0],"zero" }).ToArray(), actual: frame.ContactBytes());
            var originX = cx + points.Min(s => s.X); var originY = cy + points.Max(s => s.Y);
            p.Edge("zero", "", "Construction +X", originX, originY, originX + 165, originY, uncertain:true);
            p.Edge("zero", "", "Construction +Y", originX, originY, originX, originY - 192, uncertain:true);
            p.Label("origin", originX - 18, originY + 8); p.Label("+X", originX + 169, originY + 8);
            p.Label("+Y", originX - 19, originY - 197);
        }
        InkBlocks(p, ink);
        p.Label(name + "  /  one possible rendering", cx, 38);
        for (var run = 0; run < ink.RunCount; run++)
        {
            var sites = ink.Run(run).Sites();
            for (var i = 1; i < sites.Length; i++)
            {
                var a = points[sites[i-1]]; var b = points[sites[i]];
                p.Stroke("run" + run, $"Run {run}, site {sites[i-1]} to {sites[i]}", cx+a.X,cy+a.Y,cx+b.X,cy+b.Y,false,Variant == 0);
            }
        }
        if (Variant == 2)
            foreach (var loop in layer.Loops)
            foreach (var edge in loop.Walk)
            {
                var a = points[edge.From]; var b = points[edge.To];
                p.Edge($"span{edge.Run}.{edge.Slot}", "", $"Loop use {edge.From} → {edge.To} on run {edge.Run}.",
                    cx+a.X,cy+a.Y,cx+b.X,cy+b.Y,true);
            }
        foreach (var group in layer.Joints.GroupBy(j => points[j.Site]))
        {
            var js = group.ToArray(); var j = js[0];
            var xy = group.Key;
            var key = js.Length > 1 ? "cross" : "site" + j.Site;
            var detail = js.Length > 1 ? "Separate interior references at one supplied projection." :
                $"Site {j.Site}: {j.Ends} run ends and {j.Through} through runs, giving {j.Arms} arms.";
            p.Circle(key, detail, cx+xy.X, cy+xy.Y, Variant == 1 && (js.Length > 1 || j.Arms > 1) ? 8 : 3);
            p.Label(string.Join("/", js.Select(site => "s" + site.Site)), cx+xy.X+16, cy+xy.Y-17);
        }
        p.Label("Choose a letter  ·  15 capitals, 17 forms", 512, 38);
        for (var i = 0; i < construction.Values.Length; i++)
        {
            var form = construction.Values[i];
            var sample = StraightCapitals.Build(form, false);
            var xy = CapitalDrawing(form);
            var x = 378 + (i % 5) * 67; var y = 88 + (i / 5) * 64;
            var key = "roster" + form;
            var detail = $"Select {form}. {recipes[form]}";
            p.Drawing.Add(new DrawingItem("tile", key, form, detail, x, y + 5, 61, 58, Active: i == Mode)
                { Selects = (construction, i) });
            for (var r = 0; r < sample.RunCount; r++)
            {
                var sites = sample.Run(r).Sites();
                for (var s = 1; s < sites.Length; s++)
                {
                    var a = xy[sites[s-1]]; var b = xy[sites[s]];
                    p.Stroke(key, detail, x+a.X*.19,y+a.Y*.19,x+b.X*.19,y+b.Y*.19,false,i == Mode || candidates.Contains(form));
                }
            }
        }
        p.Node("loop", $"Loop {(layer.Loops.Count == 0 ? "absent" : "present")}", "Higher-level occurrence read. Hover to trace its support byte and witness rows.", cx, 360, layer.Loops.Count != 0);
        p.Node("matches", string.Join(" or ", candidates), "Compatible letter labels in this authored roster.", 515, 360, true);
        p.Label($"Proved two-arm corners  {corners}    ·    Branch sites  {branches}    ·    Separate crossings  {crossings.Length}", 355, 419);
        p.Result = $"Compatible: {string.Join(" or ", candidates)}. The loop read alone admits {loopCandidates.Length} of 15 labels.";
        p.Boundary = "The origin and axes belong to construction. Labels s0, s1, … are ink site references. The matcher reads signed changes and connections.";
        p.Events.Add(layer.Loops.Count > 0 ? "Closed walk: " + string.Join(" → ", layer.Loops[0].Sites()) :
            $"{ink.Incidence().Components} ink component(s); no closed ink walk.");
        foreach (var j in layer.Joints.Where(j => j.Arms >= 3))
            p.Events.Add($"Site {j.Site}: {j.Arms} arms = {j.Ends} terminating + 2 × {j.Through} through.");
        if (crossings.Length > 0) p.Events.Add($"Sites {crossings[0][0]} and {crossings[0][1]} coincide in projection; each strand keeps its own site.");
        return p;
    }
}
