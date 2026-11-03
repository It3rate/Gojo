using Avalonia;
using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private static readonly Lazy<Dictionary<string,string>> CurveRecipes = new(() => ReadCapitalRecipes("CurvedCapitals.md"));

    // One illustrative placement of the local ink sites. Only the viewer has measurements.
    private static Point[] CurveSites(string name) => name switch
    {
        "D" or "D · ends only" => [new(-48,-78),new(-48,78)],
        "D · corners" => [new(-48,-78),new(-48,78),new(52,-78),new(52,78)],
        "D · half of O" => [new(0,-78),new(0,78)],
        "C" or "C · gap in O" or "C · closure sought" => [new(52,-52),new(52,52)],
        "O" => [new(0,-74)],
        "B" => [new(-48,-82),new(-48,0),new(-48,82)],
        "G" => [new(52,-52),new(52,52),new(52,12),new(7,12)],
        "J" => [new(45,-80),new(45,37),new(-50,37)],
        "P" => [new(-48,-80),new(-48,0),new(-48,80)],
        "Q" => [new(52,52),new(86,86)],
        "R" => [new(-48,-80),new(-48,0),new(-48,80),new(40,80)],
        "S" => [new(54,-62),new(0,0),new(-54,62)],
        "U" => [new(-55,-80),new(-55,34),new(55,34),new(55,-80)],
        "Straight · closure sought" => [new(-67,0),new(67,0)],
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    private static Vector Direction(int heading)
    {
        var angle = heading * Math.PI / 4;
        return new(Math.Cos(angle), Math.Sin(angle));
    }

    private static void DrawPen(ExplorerPage p, CurveInk ink, string name, Point centre, double scale,
        bool interior, bool source, string? tile = null, string prefix = "", double tangentScale = 1, Point[]? placement = null)
    {
        var sites = placement ?? CurveSites(name);
        var range = ink.Probe;
        Point Place(Point pos) => new(centre.X + pos.X * scale, centre.Y + pos.Y * scale);
        var wires = ink.DriveLinks().ToDictionary(row => row[0], row => row[1]);
        var radial = ink.ArcLinks().ToDictionary(row => row[0], row => row[1]);
        for (var i = 0; i < (source ? ink.Catalogue.EdgeCount : ink.InkCount); i++)
        {
            var endpoints = ink.Catalogue.Edge(i);
            var a = Place(sites[endpoints[0]]); var b = Place(sites[endpoints[1]]);
            var key = tile ?? prefix + "edge" + i;
            var ghost = i >= ink.InkCount;
            var note = ghost ? "Construction-only part of the retained contour." : $"Ink span {i} goes from site {endpoints[0]} to {endpoints[1]}.";
            var h = ink.Heading(i);
            if ((!interior && i >= range[0] && i < range[1]) || (!radial.ContainsKey(i) && !wires.ContainsKey(i)))
            {
                if (i == range[0] || i == range[1]-1)
                {
                    if (i == range[0]) { var to = a + Direction(h[0]) * (30 * scale); p.Edge(key,"",note,a.X,a.Y,to.X,to.Y); }
                    if (i == range[1]-1) { var from = b - Direction(h[1]) * (30 * scale); p.Edge(key,"",note,from.X,from.Y,b.X,b.Y); }
                }
                continue;
            }
            if (radial.ContainsKey(i))
            {
                // Every radial specimen here illustrates the source centre at local (0,0).
                var radius = new Vector(a.X - centre.X, a.Y - centre.Y).Length;
                if (endpoints[0] == endpoints[1]) p.Circle(key,note,centre.X,centre.Y,radius);
                else
                {
                    var r = a - centre;
                    var clockwise = ink.ClockwiseContour()!.Value;
                    var sweep = Math.Atan2(b.Y-centre.Y,b.X-centre.X) - Math.Atan2(r.Y,r.X);
                    var extent = ((clockwise ? sweep : -sweep) + Math.Tau) % Math.Tau;
                    var path = FormattableString.Invariant($"M {a.X} {a.Y} A {radius} {radius} 0 {(extent > Math.PI ? 1 : 0)} {(clockwise ? 1 : 0)} {b.X} {b.Y}");
                    p.Drawing.Add(new("curve",key,"",note,a.X,a.Y,b.X,b.Y,!ghost,ghost) { PathData = path });
                }
            }
            else if (wires[i] == 0) p.Stroke(key,note,a.X,a.Y,b.X,b.Y,false);
            else
            {
                double reach = name switch
                {
                    "C" or "G" or "C · closure sought" => 190, "S" => 114,
                    "J" or "U" => 68, _ => 133
                };
                var start = a + Direction(h[0]) * (reach * scale * tangentScale);
                var end = b - Direction(h[1]) * (reach * scale * tangentScale);
                var path = FormattableString.Invariant($"M {a.X} {a.Y} C {start.X} {start.Y} {end.X} {end.Y} {b.X} {b.Y}");
                p.Drawing.Add(new("curve",key,"",note,a.X,a.Y,b.X,b.Y,true) { PathData = path });
            }
            p.Details[key] = note;
        }
        if (tile != null) return;
        var tips = new[] { ink.Catalogue.Edge(range[0])[0], ink.Catalogue.Edge(range[1]-1)[1] };
        for (var i = 0; i < sites.Length; i++)
        {
            if (!interior && !tips.Contains(i)) continue;
            var at = Place(sites[i]); p.Circle(prefix+"site"+i,$"Local ink site {i}.",at.X,at.Y,3);
            p.Label("s"+i,at.X+12,at.Y-15);
        }
        if (source && ink.SourceRead.HasValue)
        {
            var seam = Place(sites[ink.Catalogue.Edge(radial.Keys.Min())[0]]);
            p.Stroke(prefix+"radial","One common reach from the centre.",centre.X,centre.Y,seam.X,seam.Y,true);
            p.Circle(prefix+"radial","Centre of the radial source.",centre.X,centre.Y,3);
        }
    }

    private ExplorerPage CurvedCapitalPage()
    {
        var name = CurvedCapitals.Names[Mode]; var ink = CurvedCapitals.Ink(Mode);
        var interior = Control != 0; var source = Variant == 1; var read = ink.Read(interior);
        var p = new ExplorerPage("Curves in capitals", "Start with D. Hide the inside of its return stroke, then compare the cornered and radial versions.", "CurvedCapitals.md")
        {
            DrawingGuide = (name.Contains('·') ? name : "How to draw " + name, CurveRecipes.Value[name]),
            Status = "Choose a form in the grid or browse Construction with the arrow keys. Dashed strokes belong to construction."
        };
        var choose = Choose(p,"Construction",CurvedCapitals.Names.ToArray(),Mode,n=>Mode=n,browseWithArrows:true);
        Choose(p,"Probe evidence",["Ends + ink topology","Include interior law","Include source and goal"],Control,n=>Control=n);
        Choose(p,"Drawing",["Ink","Ink + construction"],Variant,n=>Variant=n);
        var cat = ink.Catalogue;
        var indices = Enumerable.Range(0,cat.EdgeCount).ToArray();
        var keys = indices.Select(i=>"edge"+i).ToArray();
        p.Bits("Turns during travel", "Support 01 means absent, 02 present, 03 either. This read concerns the selected probe.",[read.BendSupport],8,"bend");
        p.Rows("Span endpoints","Two local site references per span.",cat.SiteBits,indices.Select(cat.Edge).ToArray(),"ink",
            indices.Select(i=>cat.Edge(i).Select(v=>"site"+v).ToArray()).ToArray(),cat.EdgeBytes(),keys);
        p.Rows("Writing order","A signed use selects its span. Shared endpoints permit the pen to continue.",cat.UseBits,indices.Select(i=>new[]{cat.SignedUse(i)}).ToArray(),"uses",actual:cat.UseBytes(),rowKeys:keys);
        p.Rows("Read windows","Half-open use ranges for ink, probe and optional retained source.",ink.Views.Bits,
            Enumerable.Range(0,ink.Views.Count).Select(ink.Views.Range).ToArray(),"views",actual:ink.Views.Bytes());
        p.Rows("Heading at each end","0 right, 2 down, 4 left, 6 up. Odd positions are the intervening directions.",3,
            indices.Select(ink.Heading).ToArray(),"heading",actual:ink.HeadingBytes(),rowKeys:keys);
        void Links(string title, string note, int bits, int[][] rows, byte[] bytes, string key)
        {
            if (rows.Length > 0) p.Rows(title,note,bits,rows,key,actual:bytes,rowKeys:rows.Select(r=>"edge"+r[0]).ToArray());
        }
        Links("Steering during advance","Span and ODV steering word. 000 holds heading, 001 turns clockwise, 011 counterclockwise.",ink.WireBits,ink.DriveLinks(),ink.DriveBytes(),"drives");
        Links("Contour correspondence","Ink span and its source arc slot. Matching ends share the source cut.",ink.WireBits,ink.ArcLinks(),ink.ArcBytes(),"radial");
        if (ink.SourceRead is { } circle)
        {
            p.Bits("Source arcs","Six-bit fields select start, end and share. Repeated shares are equal.",ink.SourceArcBytes(),18*circle.Parts,"radial");
            p.Bits("Common reach","Each radial row selects centre, boundary site and the same reach.",ink.SourceRadialBytes(),18*(circle.Parts+1),"radial");
            p.Bits("Reference ray","The outward heading at the source seam and its writing tangent select the sense of traversal.",ink.SourceRayBytes(),3,"radial");
            p.Bits("Whole traversal","Start, unwrapped end and whole.",ink.SourceWholeBytes(),18,"radial");
            p.Bits("Source seam","The unwrapped end binds back to the starting site.",ink.SourceClosureBytes(),12,"radial");
        }
        if (ink.RungLinks().Length > 0) p.Rows("Retained rung","Source window to the visible probe window.",2,ink.RungLinks(),"views",actual:ink.RungBytes());
        Links("Closure sought","Last span and desired arrival site. Compare with the actual endpoint.",ink.WireBits,ink.GoalLinks(),ink.GoalBytes(),"goal");
        p.Label(interior ? "One possible rendering" : "Only probe ends shown",165,35);
        DrawPen(p,ink,name,new(165,195),1,interior,source);
        p.Label("Choose a construction",510,35);
        for (var i=0;i<choose.Values.Length;i++)
        {
            var form = choose.Values[i]; var key = "form"+i;
            var x = 354 + i%5*72; var y = 91 + i/5*64;
            var caption = form switch { "D · corners"=>"corners","D · half of O"=>"half O","D · ends only"=>"ends",
                "C · gap in O"=>"gap in O","C · closure sought"=>"goal C","Straight · closure sought"=>"goal line",_=>form };
            p.Drawing.Add(new DrawingItem("tile",key,caption,"Select "+form,x,y+5,65,58,Active:i==Mode){Selects=(choose,i)});
            DrawPen(p,CurvedCapitals.Ink(i),form,new(x,y),.17,true,false,key);
        }
        var bend = read.BendSupport switch {1=>"At joints only",2=>"Turns on the span",_=>"Interior unresolved"};
        if (read.BendSupport==1 && read.JointTurns==0) bend="Holds its heading";
        p.Node("bend",bend,"Whether the pen turns during a participating span.",165,350,read.BendSupport==2);
        p.Node("ink",$"{read.Loops} ink loop(s)","Independent cycles in the visible ink.",508,350,read.Loops>0);
        p.Result = "Compatible with this probe: " + string.Join(", ",CurvedCapitals.Compatible(read,Control == 2 ? ink.ReadOrigin() : null)) + ".";
        p.Boundary = "Matching uses loop count, probe headings and available turning evidence. The recipes supply one upright hand. Length, filled area and later flow routes stay open.";
        p.Events.Add($"Probe {read.Enter} → {read.Leave}. " + (read.RequiresHeadingChange ? "One forward straight stroke cannot satisfy both ends." : "Matching end headings leave the intervening turn open."));
        if (ink.CrosswiseStem() is int stem) p.Events.Add($"Both ends attach across straight stem {stem}, each at a right angle.");
        if (ink.ClosureGoalMet() is bool met) p.Events.Add(met ? "Requested closure reached." : "Requested closure missed. The two tips retain separate sites.");
        if (ink.RungLinks().Length != 0) p.Events.Add($"Whole source closed: {ink.Views.IsClosed(2)}. Selected portion closed: {ink.Views.IsClosed(1)}.");
        return p;
    }
}
