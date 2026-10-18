using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Ornamental emphasis over native discovery/focus witnesses. Flowers,
/// leaves, screen geometry and colours are deliberately presentation glyphs;
/// all selected cells, anchors and routes come from the retained native read.</summary>
public static class OrnamentDiscoveryExperiment
{
    private sealed record Point(double X,double Y);
    private sealed record Stroke(IElement Identity,IElement From,IElement To,Point A,Point B,int Layer);
    public static int Run(string[] args)
    {
        var output=Path.Combine("experiments","QualitativeFrieze","results","ornament-discovery");
        for(var i=0;i<args.Length;i++)if(args[i]=="--output"&&i+1<args.Length)output=args[++i];else throw new ArgumentException("Usage: --ornament-discovery [--output DIRECTORY]");
        Directory.CreateDirectory(output);var watch=System.Diagnostics.Stopwatch.StartNew();
        var study=new OrnamentDiscoveryStudy(message=>Console.WriteLine($"[{watch.Elapsed.TotalSeconds:0.0}s] {message}"));
        var cards=new StringBuilder();
        foreach(var read in study.Readings)
        {
            var svg=Picture(read);File.WriteAllText(Path.Combine(output,read.Id+".svg"),svg);
            var routes=read.Strands is {} strands?$"<p>{E(read.Occurrences).Length} growing strands emphasized from {E(strands.Routes).Length} route candidates; {E(strands.Competing).Length} branch alternatives retained. The emphasis question requires at least two heading cycles. Short fragments remain available in the candidate read.</p>":"";
            cards.Append($"<article id='{read.Id}' data-selected='{E(read.Selected).Length}' data-complement='{E(read.Complement).Length}'><div class=copy><span class=eyebrow>{read.Id} · {H(read.Style=="history"?"HISTORY RECOVERY":"STRUCTURAL DISCOVERY")} · {H(read.Patch.Id)} / D{read.Level+1}</span><h2>{H(read.Name)}</h2><p>{H(read.Description)}</p></div><figure>{svg}</figure><div class=controls><button class=glyphs aria-pressed=true>Ornament on</button><label>Field prominence <input class=field type=range min=15 max=65 value=34></label><a href='{read.Id}.svg'>Standalone SVG</a></div><details><summary>What was read, and what the image adds</summary><p>{E(read.Occurrences).Length} retained occurrences · {E(read.Selected).Length} emphasized strokes + {E(read.Complement).Length} complementary strokes = {E(read.Whole).Length} strokes in the complete constructed field.</p>{routes}<p>The native focus retains the source, candidate, actual construction correspondences, selected participation and complement. Colour, petals and leaves communicate the reading; they are display glyphs, not claims that the reader discovered flowers or botanical growth.</p><p>Four-edge and eight-edge families describe closed cycles. Their apparent angles and centres use the separately declared word-landmark and display interpretation.</p></details></article>");
        }
        var seen=Traverse(study.Surface).ToArray();
        var diagnostics=new
        {
            Schema="gojo-ornament-discovery-v1",Seconds=watch.Elapsed.TotalSeconds,NativeElements=seen.Length,NativeNumericElements=seen.Count(item=>item is Carrier or CarrierLocation),
            CellLevels=study.CellLevels.Select(level=>new{Patch=level.Patch.Id,Level=level.Index+1,Complete=level.Discovery.IsComplete,Families=E(level.Discovery.Families).Cast<Relation>().Select(row=>{level.Discovery.TryReadFamily(row,out var family);return new{Arity=family.BoundaryArity,Occurrences=E(family.Occurrences).Length};}),SingletonComplement=E(level.Discovery.Complement).Length}),
            Readings=study.Readings.Select(read=>new{read.Id,read.Name,Patch=read.Patch.Id,Level=read.Level+1,Occurrences=E(read.Occurrences).Length,Whole=E(read.Whole).Length,Selected=E(read.Selected).Length,Complement=E(read.Complement).Length,
                RhythmComplete=read.Rhythm?.IsComplete,RhythmConflicts=read.Rhythm is {} rhythm?E(rhythm.Conflicts).Length:0,
                RouteCandidates=read.Strands is {} strand?E(strand.Routes).Length:0,BranchAlternatives=read.Strands is {} flow?E(flow.Competing).Length:0}),
            Limits=new[]{"Bounded supplied cellular topology, not image parsing or unrestricted motif discovery.","Cycle role-renaming is a coarse arity read, not metric congruence or geometric shape classification.","Heading grammars and translation/alternation questions are declared; routes and occurrence matches are found from actual structure.","Ornamental glyphs and visual contrast are presentation interpretations; no aesthetic preference was learned.","All complementary constructed strokes remain visible. Independent projected crossings do not become joins."}
        };
        File.WriteAllText(Path.Combine(output,"diagnostics.json"),JsonSerializer.Serialize(diagnostics,new JsonSerializerOptions{WriteIndented=true}));
        var navigation=string.Join("",study.Readings.Select(read=>$"<a href='#{read.Id}'>{read.Id} {H(read.Name)}</a>"));
        File.WriteAllText(Path.Combine(output,"gallery.html"),Head+navigation+"</nav><main>"+cards+Tail);
        Console.WriteLine(JsonSerializer.Serialize(diagnostics,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("Wrote "+Path.GetFullPath(output));return 0;
    }

    private static string Picture(OrnamentDiscoveryStudy.Reading read)
    {
        const double width=1080,height=610,pad=65;var patch=read.Patch;
        var all=new List<Stroke>();
        foreach(var (execution,index) in patch.Executions.Select((execution,index)=>(execution,index)))AddGraph(execution.Join!.Graph,execution.Join.Bindings,index);
        foreach(var node in patch.Decorations)AddGraph(node.Current.Join.Graph,node.Current.Join.Bindings,2);
        var source=Positions(patch,patch.Source.Bindings).Values.ToArray();
        var minX=source.Min(p=>p.X);var maxX=source.Max(p=>p.X);var minY=source.Min(p=>p.Y);var maxY=source.Max(p=>p.Y);
        var scale=Math.Min((width-2*pad)/(maxX-minX),(height-2*pad)/(maxY-minY));
        Point P(Point p)=>new(width/2+(p.X-(minX+maxX)/2)*scale,height/2-(p.Y-(minY+maxY)/2)*scale);
        var svg=new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}' viewBox='0 0 {width} {height}' role='img' aria-label='{H(read.Name)}'><title>{H(read.Name)}</title><defs><pattern id='paper-{read.Id}' width='12' height='12' patternUnits='userSpaceOnUse'><circle cx='2' cy='2' r='.45' fill='#d6c7ae' opacity='.22'/></pattern></defs><rect width='100%' height='100%' fill='#fbf6e9'/><rect width='100%' height='100%' fill='url(#paper-{read.Id})'/>");
        var emphasis=new HashSet<IElement>(E(read.Selected),ReferenceEqualityComparer.Instance);
        var quiet=new StringBuilder();var strong=new StringBuilder();var fill=new StringBuilder();var glyph=new StringBuilder();
        foreach(var line in all)
        {
            var a=P(line.A);var b=P(line.B);quiet.Append(Line(a,b,line.Layer==2?"#a7936c":"#557b7b",line.Layer==0?2.1:1.2));
            if(emphasis.Contains(line.Identity)&&read.Style is "history" or "vine")strong.Append(Line(a,b,read.Style=="vine"?"#356950":"#8f493f",read.Style=="vine"?4.3:2.8));
        }
        if(read.Style!="vine"&&read.Style!="history")
        {
            var positions=Positions(patch,patch.Levels[read.Level].Bindings);var anchors=Positions(patch,read.Anchors);
            var family=read.Candidate;var discovery=read.Cells!;
            foreach(var occurrence in E(read.Occurrences).Cast<Relation>())
            {
                if(!discovery.TryReadOccurrence(family,occurrence,out var face,out var boundary,out _))throw new InvalidOperationException("A display must consume an actual cell occurrence.");
                var points=E(boundary).Cast<Relation>().Select(part=>P(positions[((Relation)part.Right).Left])).ToArray();
                var phase=-1;if(read.Rhythm is {} rhythm&&rhythm.TryReadAssignment(face,out var actual))phase=Array.FindIndex(E(rhythm.Phases),p=>N.Same(p,actual));
                var ink=read.Style switch{"chambers"=>"#326e6a","rosettes"=>"#754a78","triangles"=>"#b47529",_=>phase==0?"#ab393d":phase==1?"#356b80":"#7d7d71"};
                fill.Append($"<polygon points='{Points(points)}' fill='{ink}' fill-opacity='{(read.Style=="chambers"?".10":".07")}'/>");
                strong.Append($"<polygon points='{Points(points)}' fill='none' stroke='{ink}' stroke-opacity='.8' stroke-width='{(read.Style=="chambers"?"3":"2.2")}' stroke-linejoin='round'/>");
                var center=P(anchors[face]);var size=read.Style switch{"chambers"=>13,"rosettes"=>17,"triangles"=>8,_=>phase==0?15:11};
                if(read.Style=="triangles")glyph.Append(Star(center,size,3,ink));
                else glyph.Append(Flower(center,size,read.Style=="chambers"?8:read.Style=="rosettes"?6:4,ink,read.Style=="flowers"&&phase==0));
            }
        }
        if(read.Style=="history")foreach(var at in Positions(patch,read.Anchors).Values)glyph.Append(Flower(P(at),13,4,"#b14440",true));
        if(read.Strands is {} strands)
        {
            var positions=Positions(patch,patch.Levels[read.Level].Bindings);
            foreach(var route in E(read.Occurrences).Cast<Relation>())
            {
                strands.TryReadRoute(route,out var path);
                foreach(var use in path.Uses)
                {
                    var ends=(Relation)use.Right;var a=P(positions[ends.Left]);var b=P(positions[ends.Right]);
                    glyph.Append(Leaf(a,b));
                }
                var end=P(positions[path.End]);glyph.Append(Flower(end,9,5,"#b55045",true));
            }
        }
        svg.Append(fill).Append("<g class='full-field' opacity='.34'>").Append(quiet).Append("</g><g class='selected-structure'>").Append(strong).Append("</g>");
        // Presentation overpasses preserve the independently identified native
        // layers even when stronger contrast would make a crossing look joined.
        foreach(var (lower,i) in all.Select((line,i)=>(line,i)))foreach(var upper in all.Skip(i+1))
        {
            if(!emphasis.Contains(upper.Identity)&&!emphasis.Contains(lower.Identity))continue;
            if(ReferenceEquals(lower.From,upper.From)||ReferenceEquals(lower.From,upper.To)||ReferenceEquals(lower.To,upper.From)||ReferenceEquals(lower.To,upper.To))continue;
            var a=P(lower.A);var b=P(lower.B);var c=P(upper.A);var d=P(upper.B);var ux=b.X-a.X;var uy=b.Y-a.Y;var vx=d.X-c.X;var vy=d.Y-c.Y;var denominator=ux*vy-uy*vx;
            if(Math.Abs(denominator)<1e-8)continue;var t=((c.X-a.X)*vy-(c.Y-a.Y)*vx)/denominator;var u=((c.X-a.X)*uy-(c.Y-a.Y)*ux)/denominator;
            if(t< -1e-6||t>1+1e-6||u< -1e-6||u>1+1e-6)continue;
            var crossing=new Point(a.X+t*ux,a.Y+t*uy);var length=Math.Sqrt(vx*vx+vy*vy);if(length<1e-8)continue;
            var before=Math.Max(-3,-Math.Clamp(u,0,1)*length);var after=Math.Min(3,(1-Math.Clamp(u,0,1))*length);
            svg.Append($"<g class='overpass'><title>Independent projected crossing</title><circle cx='{F(crossing.X)}' cy='{F(crossing.Y)}' r='2.5' fill='#fbf6e9'/>");
            svg.Append(Line(new(crossing.X+vx/length*before,crossing.Y+vy/length*before),new(crossing.X+vx/length*after,crossing.Y+vy/length*after),"#64837a",1.2)).Append("</g>");
        }
        return svg.Append("<g class='ornaments'>").Append(glyph).Append("</g></svg>").ToString();

        void AddGraph(QualitativeStrokeGraph graph,Relation bindings,int layer)
        {
            var words=Positions(patch,bindings);
            foreach(var actual in E(graph.Strokes).Cast<Relation>())
            {QualitativeStrokeGraph.TryReadStroke(actual,out _,out var a,out var b,out _);all.Add(new(actual,a,b,words[a],words[b],layer));}
        }
    }
    private static Dictionary<IElement,Point> Positions(OrnamentPerspectiveStudy.Patch patch,Relation bindings)
    {
        var output=new Dictionary<IElement,Point>(ReferenceEqualityComparer.Instance);
        foreach(var binding in E(bindings).Cast<Relation>())
        {
            var x=0d;var y=0d;
            foreach(var move in E((Relation)binding.Right).Cast<Relation>())
            {patch.Source.Frame.Direction(move,out var axis,out var forward,out _);if(ReferenceEquals(axis,patch.Source.Frame.Horizontal))x+=forward?1:-1;else y+=forward?1:-1;}
            output.Add(binding.Left,patch.Basis=="triangular"?new(x+y*.5,y*Math.Sqrt(3)/2):new(x,y));
        }
        return output;
    }
    private static string Flower(Point at,double radius,int petals,string ink,bool filled)
    {
        var svg=new StringBuilder($"<g class='flower' transform='translate({F(at.X)} {F(at.Y)})'>");
        for(var i=0;i<petals;i++)svg.Append($"<ellipse cx='0' cy='{F(-radius*.55)}' rx='{F(radius*.31)}' ry='{F(radius*.56)}' transform='rotate({F(i*360d/petals+45)})' fill='{(filled?ink:"#fbf6e9")}' stroke='{ink}' stroke-width='1.4'/>");
        return svg.Append($"<circle r='{F(radius*.22)}' fill='#dcad50' stroke='{ink}' stroke-width='1.2'/></g>").ToString();
    }
    private static string Star(Point at,double radius,int points,string ink)
    {
        var vertices=Enumerable.Range(0,points*2).Select(i=>{var r=i%2==0?radius:radius*.3;var angle=-Math.PI/2+i*Math.PI/points;return new Point(at.X+r*Math.Cos(angle),at.Y+r*Math.Sin(angle));}).ToArray();
        return $"<polygon class='flower' points='{Points(vertices)}' fill='{ink}' stroke='{ink}' stroke-width='1'/>";
    }
    private static string Leaf(Point a,Point b)
    {
        var dx=b.X-a.X;var dy=b.Y-a.Y;var length=Math.Sqrt(dx*dx+dy*dy);if(length<8)return "";
        var ux=dx/length;var uy=dy/length;var size=Math.Min(13,length*.28);var at=new Point((a.X+b.X)/2,(a.Y+b.Y)/2);
        var tip=new Point(at.X+ux*size-uy*size,at.Y+uy*size+ux*size);var opposite=new Point(at.X-ux*size+uy*size,at.Y-uy*size-ux*size);
        string One(Point end)=>$"<path class='leaf' d='M {F(at.X)} {F(at.Y)} Q {F(end.X+uy*size*.7)} {F(end.Y-ux*size*.7)} {F(end.X)} {F(end.Y)} Q {F(at.X-uy*size*.9)} {F(at.Y+ux*size*.9)} {F(at.X)} {F(at.Y)}' fill='#62844b' stroke='#356950' stroke-width='1.1'/>";
        return One(tip)+One(opposite);
    }
    private static string Line(Point a,Point b,string colour,double width)=>$"<line x1='{F(a.X)}' y1='{F(a.Y)}' x2='{F(b.X)}' y2='{F(b.Y)}' stroke='{colour}' stroke-width='{F(width)}' stroke-linecap='round'/>";
    private static string Points(Point[] points)=>string.Join(" ",points.Select(p=>F(p.X)+","+F(p.Y)));
    private static string F(double value)=>value.ToString("0.###",CultureInfo.InvariantCulture);
    private static string H(string value)=>WebUtility.HtmlEncode(value);
    private static IElement[] E(Relation scope)=>N.E(scope);
    private static IEnumerable<IElement> Traverse(IElement root)
    {var seen=new HashSet<IElement>(ReferenceEqualityComparer.Instance);var stack=new Stack<IElement>();stack.Push(root);while(stack.TryPop(out var item)){if(!seen.Add(item))continue;yield return item;if(item is Relation pair){stack.Push(pair.Left);stack.Push(pair.Right);}}}
    private const string Head="""
<!doctype html><html lang=en><meta charset=utf-8><meta name=viewport content='width=device-width,initial-scale=1'><title>Readings that shape an ornament</title><style>
*{box-sizing:border-box}body{margin:0;background:#eee9dc;color:#273f3b;font:16px/1.6 system-ui,sans-serif}header,nav,main{max-width:1220px;margin:auto;padding:24px}header{padding-top:55px}h1{font:clamp(36px,5vw,64px)/1.1 Georgia,serif;max-width:850px;margin:14px 0 24px}h2{font:32px/1.2 Georgia,serif;margin:12px 0}p{max-width:900px}a{color:#7c3a3a}nav{display:flex;gap:10px;flex-wrap:wrap}nav a{padding:7px 13px;border:1px solid #c8c2af;border-radius:20px;text-decoration:none;font-size:13px}article{background:#fbf6e9;border:1px solid #d3c9b6;margin:12px 0 40px;border-radius:5px;overflow:hidden;scroll-margin-top:18px}.copy,details{padding:26px 32px}.eyebrow{font-size:11px;font-weight:700;letter-spacing:.14em;color:#7c6254}figure{margin:0}svg{width:100%;height:auto;display:block}.controls{display:flex;gap:22px;align-items:center;flex-wrap:wrap;padding:12px 32px;border-top:1px solid #e0d7c3;font-size:13px}button{background:#f3e8d5;border:1px solid #b4a78d;border-radius:4px;padding:8px 14px;color:#30473d;cursor:pointer}label{display:flex;align-items:center;gap:12px}input{width:150px;accent-color:#5c7e64}details{border-top:1px solid #e0d7c3;font-size:14px}summary{cursor:pointer;font-weight:600}.no-glyphs .ornaments{display:none}footer{padding:25px 32px 60px;font-size:14px}@media(max-width:600px){header,nav,main{padding:15px}.copy,details,.controls{padding:20px}h2{font-size:26px}.controls{gap:12px}input{width:100px}}
</style><header><span class=eyebrow>GOJŌ · DISCOVERY AND EMPHASIS</span><h1>One field, different dominant readings</h1><p>The complete constructed lattice stays present. A selected structural reading receives stronger colour, contrast and ornamental punctuation. Compare a remembered generating motif with repeated cells and growing strands found through exposed structure.</p><p>These are bounded discovery experiments: the questions specify what kind of recurrence to inspect. The matches and routes are computed from actual topology and published motion words. Flowers and leaves make the chosen interpretation explicit.</p><p><a href='../ornament-perspectives/gallery.html'>V01 / V02 mechanics</a> · <a href='../skeleton-atlas/goals.html#G12'>G12 · one scaffold, several readings</a> · <a href='../../../../Gojo/docs/GojoOrnamentDiscovery.md'>Mechanics and findings</a></p></header><nav>
""";
    private const string Tail="""
</main><footer><a href='diagnostics.json'>Native-read diagnostics</a> · No aesthetic preference, image recognition or unrestricted concept discovery is claimed. All field strokes remain accounted for as selected or complementary participation.</footer><script>
for(const card of document.querySelectorAll('article')){const button=card.querySelector('.glyphs');button.onclick=()=>{const off=card.classList.toggle('no-glyphs');button.textContent=off?'Ornament off':'Ornament on';button.setAttribute('aria-pressed',String(!off));};card.querySelector('.field').oninput=event=>card.querySelector('.full-field').setAttribute('opacity',event.target.value/100);}
</script></html>
""";
}
