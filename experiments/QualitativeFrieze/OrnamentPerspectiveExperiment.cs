using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Presentation projections of captured native covers, overlap
/// witnesses, repeated incidence reads and acknowledged construction. Screen
/// coordinates and colour never propose or validate a native grouping.</summary>
public static class OrnamentPerspectiveExperiment
{
    private sealed record Point(double X,double Y);
    private sealed record Line(Point A,Point B,string Colour,double Width=2,bool Dashed=false,IElement? From=null,IElement? To=null,bool Guide=false);
    private static readonly string[] Palette=["#c3a148","#397fa9","#9b6084","#4c947d","#af7045","#7977b1"];

    public static int Run(string[] args)
    {
        var output=Path.Combine("experiments","QualitativeFrieze","results","ornament-perspectives");
        for(var i=0;i<args.Length;i++)if(args[i]=="--output"&&i+1<args.Length)output=args[++i];else throw new ArgumentException("Usage: --ornament-perspectives [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var watch=System.Diagnostics.Stopwatch.StartNew();
        var study=new OrnamentPerspectiveStudy(message=>Console.WriteLine($"[{watch.Elapsed.TotalSeconds:0.0}s] {message}"));
        var cards=new StringBuilder();var models=new List<object>();
        foreach(var patch in study.Patches)
        {
            var model=Model(patch);models.Add(model);
            cards.Append($"<article id='{patch.Id}' class='view-card' data-patch='{models.Count-1}'><span class=eyebrow>{patch.Id} · OBSERVATION COVERS</span><h2>{H(patch.Name)}</h2><p>One executed field, several connected questions. Select two reads and two groups; green marks the exact source chambers shared by both. You can also compare two groups inside the same read.</p><div class=controls><label>Left read<select class=left-view></select></label><label>Left group<select class=left-group></select></label><label>Right read<select class=right-view></select></label><label>Right group<select class=right-group></select></label></div><div class=triple><figure><figcaption>Left interpretation</figcaption><div class=left-picture></div></figure><figure><figcaption>Right interpretation</figcaption><div class=right-picture></div></figure><figure><figcaption>Shared source · yellow / green / blue</figcaption><div class=overlap-picture></div></figure></div><p class=overlap-stat></p><details><summary>All memberships and production layers</summary><p class=view-description></p><div class=all-picture></div><p>Striped source chambers participate in several groups of this read. Colour distinguishes observations, not material owners.</p>{ViewTable(patch)}</details></article>");

            var sourceLines=SourceLines(patch).Select(line=>line with{Colour="#d3d8cf",Width=.7,Guide=true}).ToArray();
            var bounds=Positions(patch,patch.Source.Bindings).Values.ToArray();
            var rungCards=new StringBuilder();
            for(var rung=0;rung<patch.Levels.Length;rung++)
            {
                var level=patch.Levels[rung];var words=Positions(patch,level.Bindings);
                var lines=E(level.Read.Edges).Cast<Relation>().Select(edge=>
                {level.Read.TryReadEdge(edge,out _,out var a,out var b,out _);return new Line(words[a],words[b],Palette[rung%Palette.Length],2,From:a,To:b);}).ToArray();
                var svg=Svg([..sourceLines,..lines],bounds,$"{patch.Name} {level.Id}");
                File.WriteAllText(Path.Combine(output,patch.Id+"-"+level.Id+".svg"),svg);
                rungCards.Append($"<figure><figcaption>{level.Id} · {E(level.Read.Sites).Length} roles / {E(level.Read.Edges).Length} edges / {E(level.Read.Faces).Length} cells</figcaption>{svg}</figure>");
            }
            cards.Append($"<article id='V02-{patch.Id}'><span class=eyebrow>V02 · CELLS BECOME SCAFFOLDS</span><h2>{H(patch.Name)} · three derived levels</h2><p>Actual shared seams create new neighbour edges. Complete interior vertex cycles create new cells, which can be read again. Finite boundary participation stays in an explicit complement at every level.</p><div class=triple>{rungCards}</div><p class=stats>D1 and D2 are also rebuilt by acknowledged path flow. D3 is an observed native topology with a published word interpretation. None of these roles identifies its source sites or asserts a universal geometric centre.</p></article>");
            var generated=new List<Line>();
            foreach(var (execution,index) in patch.Executions.Select((execution,index)=>(execution,index)))
                if(execution.Join is {} join)generated.AddRange(GraphLines(patch,join.Graph,join.Bindings,Palette[index+1],index==0?2.8:1.6));
            foreach(var node in patch.Decorations)generated.AddRange(GraphLines(patch,node.Current.Join.Graph,node.Current.Join.Bindings,"#b37d32",1.8));
            var final=Svg(generated,bounds,patch.Name+" / generated layers");
            File.WriteAllText(Path.Combine(output,patch.Id+"-generated.svg"),final);
            var networkFigures=new StringBuilder();
            foreach(var network in patch.Networks)
            {
                var lines=network.Execution.Join is {} join?GraphLines(patch,join.Graph,join.Bindings,"#287f83",2).ToList():[];
                var positions=Positions(patch,network.Bindings);
                foreach(var edge in E(network.Read.Overlaps).Cast<Relation>())
                {var ends=(Relation)edge.Right;lines.Add(new(positions[ends.Left],positions[ends.Right],"#98568c",2,true));}
                networkFigures.Append($"<figure><figcaption>{H(network.View.Name)} · {E(network.Read.Contacts).Length} contact edges / {E(network.Read.Overlaps).Length} overlap relations</figcaption>{Svg(lines,bounds,network.View.Name)}</figure>");
            }
            cards.Append($"<article id='V02-build-{patch.Id}'><span class=eyebrow>V02 · READ → CONSTRUCTION → ORNAMENT</span><h2>New flow consumes the derived structure</h2><p>Solid lines below are new accepted drawing output: blue D1, purple D2, and gold sibling marker units. The marker program consumes the second level's actual landmark bindings. Dots mark shared branch roles; bridge gaps distinguish independent projected crossings.</p><figure>{final}</figure><details><summary>Different V01 reads produce different construction networks</summary><p>Teal solid edges are witnessed boundary contacts, executed as new strokes. Purple dashed relations are shared chamber membership. They remain observation channels and are never silently converted into physical connections.</p><div class=compare>{networkFigures}</div></details><p class=stats>{patch.Executions.Sum(execution=>E(execution.Commands).Length)} executed derived edges · {patch.Decorations.Length} sibling marker units · finite boundary retained</p></article>");
        }
        var native=Traverse(study.Surface).ToArray();var numeric=native.Count(item=>item is Carrier or CarrierLocation);
        var diagnostics=new
        {
            Schema="gojo-ornament-perspectives-v1",NativeElements=native.Length,NativeNumericElements=numeric,Seconds=watch.Elapsed.TotalSeconds,
            Patches=study.Patches.Select(patch=>new
            {
                patch.Id,patch.Name,SourceFaces=E(patch.Source.Scaffold.Faces).Length,SourceEdges=patch.Source.Units.Length,
                Views=patch.Views.Select(view=>new{view.Id,view.Name,Complete=view.View.IsComplete,Groups=E(view.View.Groups).Length,
                    Selected=E(view.View.SelectedFaces).Length,Complement=E(view.View.Complement).Length,Contacts=E(view.View.Adjacency).Length,Overlaps=E(view.View.Overlaps).Length}),
                Compositions=patch.Compositions.Length,CompleteCompositions=patch.Compositions.All(composition=>composition.IsComplete),
                Levels=patch.Levels.Select(level=>new{level.Id,Complete=level.Read.IsComplete,Sites=E(level.Read.Sites).Length,Edges=E(level.Read.Edges).Length,
                    Cells=E(level.Read.Faces).Length,BoundaryEvidence=E(level.Read.Boundary).Length,Bindings=E(level.Bindings).Length,
                    CellArities=E(level.Read.Faces).Cast<Relation>().GroupBy(cell=>E((Relation)cell.Right).Length).ToDictionary(group=>group.Key,group=>group.Count())}),
                CompleteLandmarkReads=E(patch.Landmarks.Publications).Length,UnresolvedLandmarks=E(patch.Landmarks.Unresolved).Length,
                AcceptedConstruction=patch.Executions.Concat(patch.Networks.Select(network=>network.Execution)).All(execution=>execution.Join is {} join&&join.TryReadPassed(join.Evaluate())),
                Markers=patch.Decorations.Length
            }),
            GoalChecks=new[]{"G06: actual closed triangular source chambers and shared-edge incidence.","G12: multiple views share the exact source; explicit complements and pair correspondences, including overlapping membership.",
                "G07: fan D1 has four- and eight-edge cells with actual 4-8-8 interior incidence; regular Euclidean octagons are not asserted.","G11: sixfold source neighbourhood is related exploration; full star-corridor goal not claimed."},
            Limits=new[]{"Authored grouping proposals; no image parsing, blind grouping discovery or learned view selection.","Overlapping observations do not duplicate material, merge groups or physically join source sites.",
                "The optional equal-share word read is a selected additive-basis landmark interpretation, not a universal centre law.","Repeated dual reads preserve source and visible boundary complement, not exact involutive recovery of a finite patch.",
                "View-defined contact networks need not be cellular embeddings; overlap relations do not imply contact or a new bounded area."}
        };
        File.WriteAllText(Path.Combine(output,"diagnostics.json"),JsonSerializer.Serialize(diagnostics,new JsonSerializerOptions{WriteIndented=true}));
        File.WriteAllText(Path.Combine(output,"gallery.html"),Html(cards.ToString(),JsonSerializer.Serialize(models)));
        Console.WriteLine(JsonSerializer.Serialize(diagnostics,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"Wrote {Path.GetFullPath(output)}");
        return numeric!=0||study.Patches.Any(patch=>patch.Views.Any(view=>!view.View.IsComplete)||patch.Levels.Any(level=>!level.Read.IsComplete)||E(patch.Landmarks.Unresolved).Length!=0||
            patch.Executions.Concat(patch.Networks.Select(network=>network.Execution)).Any(execution=>execution.Join is null||!execution.Join.TryReadPassed(execution.Join.Evaluate())))?1:0;
    }

    private static object Model(OrnamentPerspectiveStudy.Patch patch)
    {
        var positions=Positions(patch,patch.Source.Bindings);var faces=E(patch.Source.Scaffold.Faces).Cast<Relation>().ToArray();
        var faceIds=new Dictionary<IElement,int>(ReferenceEqualityComparer.Instance);for(var i=0;i<faces.Length;i++)faceIds.Add(faces[i],i);
        var viewIds=new Dictionary<OrnamentView,int>(ReferenceEqualityComparer.Instance);for(var i=0;i<patch.Views.Length;i++)viewIds.Add(patch.Views[i].View,i);
        var groupIds=new Dictionary<IElement,int>(ReferenceEqualityComparer.Instance);
        foreach(var view in patch.Views)foreach(var (group,index) in E(view.View.Groups).Select((group,index)=>(group,index)))groupIds.TryAdd(group,index);
        var pairs=new Dictionary<string,object>();
        foreach(var comparison in patch.Comparisons)
        {
            var li=viewIds[comparison.Left];var ri=viewIds[comparison.Right];
            foreach(var row in E(comparison.Correspondences).Cast<Relation>())
            {
                comparison.TryReadCorrespondence(row,out var parts);
                Add(li,ri,parts.Left,parts.Right,parts.SharedFaces,parts.LeftOnlyFaces,parts.RightOnlyFaces);
                Add(ri,li,parts.Right,parts.Left,parts.SharedFaces,parts.RightOnlyFaces,parts.LeftOnlyFaces);
            }
            foreach(var pair in E(comparison.Unrelated).Cast<Relation>())
            {
                comparison.Left.TryReadGroup((Relation)pair.Left,out var a);comparison.Right.TryReadGroup((Relation)pair.Right,out var b);
                Add(li,ri,a,b,S(),a.Faces,b.Faces);Add(ri,li,b,a,S(),b.Faces,a.Faces);
            }
        }
        int[] Ids(Relation scope)=>E(scope).Select(face=>faceIds[face]).ToArray();
        return new
        {
            patch.Id,patch.Name,FaceCount=faces.Length,
            Faces=faces.Select((face,index)=>
            {patch.Source.Scaffold.TryReadFace(face,out _,out var vertices);return new{Id=index,Points=E(vertices).Select(site=>new[]{positions[site].X,positions[site].Y}).ToArray()};}),
            Lines=SourceLines(patch).Select(line=>new[]{line.A.X,line.A.Y,line.B.X,line.B.Y}),
            Views=patch.Views.Select(view=>new{view.Id,view.Name,view.Description,Complement=Ids(view.View.Complement),Groups=E(view.View.Groups).Cast<Relation>().Select((group,index)=>
            {view.View.TryReadGroup(group,out var parts);return new{Id=index,Faces=Ids(parts.Faces),Boundary=E(parts.Boundary).Cast<Relation>().Select(seam=>
            {QualitativeStrokeGraph.TryReadStroke((Relation)seam.Left,out _,out var a,out var b,out _);return new[]{positions[a].X,positions[a].Y,positions[b].X,positions[b].Y};}).ToArray()};})}),
            Pairs=pairs
        };
        void Add(int left,int right,OrnamentViewGroup a,OrnamentViewGroup b,Relation common,Relation onlyA,Relation onlyB)=>
            pairs[$"{left}/{groupIds[a.Read]}/{right}/{groupIds[b.Read]}"]=new{Shared=Ids(common),Left=Ids(onlyA),Right=Ids(onlyB),Witness=true};
    }

    private static string ViewTable(OrnamentPerspectiveStudy.Patch patch)=>"<div class=table-wrap><table><thead><tr><th>Read</th><th>Groups</th><th>Selected / complementary chambers</th><th>Contact / overlap pairs</th></tr></thead><tbody>"+
        string.Join("",patch.Views.Select(view=>$"<tr><td>{H(view.Id+" · "+view.Name)}</td><td>{E(view.View.Groups).Length}</td><td>{E(view.View.SelectedFaces).Length} / {E(view.View.Complement).Length}</td><td>{E(view.View.Adjacency).Length} / {E(view.View.Overlaps).Length}</td></tr>"))+"</tbody></table></div>";
    private static Line[] SourceLines(OrnamentPerspectiveStudy.Patch patch)=>GraphLines(patch,patch.Source.Scaffold.Graph,patch.Source.Bindings,"#8a9995",1).ToArray();
    private static IEnumerable<Line> GraphLines(OrnamentPerspectiveStudy.Patch patch,QualitativeStrokeGraph graph,Relation bindings,string colour,double width)
    {
        var positions=Positions(patch,bindings);
        foreach(var edge in E(graph.Strokes).Cast<Relation>())
        {QualitativeStrokeGraph.TryReadStroke(edge,out _,out var a,out var b,out _);yield return new(positions[a],positions[b],colour,width,From:a,To:b);}
    }
    private static Dictionary<IElement,Point> Positions(OrnamentPerspectiveStudy.Patch patch,Relation bindings)
    {
        var positions=new Dictionary<IElement,Point>(ReferenceEqualityComparer.Instance);
        foreach(var binding in E(bindings).Cast<Relation>())
        {
            var x=0d;var y=0d;
            foreach(var term in E((Relation)binding.Right).Cast<Relation>())
            {
                if(!patch.Source.Frame.Direction(term,out var axis,out var forward,out _))throw new InvalidOperationException("A display word needs a declared direction.");
                if(ReferenceEquals(axis,patch.Source.Frame.Horizontal))x+=forward?1:-1;else y+=forward?1:-1;
            }
            positions.Add(binding.Left,patch.Basis=="triangular"?new(x+y*.5,y*Math.Sqrt(3)/2):new(x,y));
        }
        return positions;
    }
    private static string Svg(IEnumerable<Line> source,Point[] bounds,string title)
    {
        var lines=source.ToArray();const double width=880,height=420,pad=25;
        var minX=bounds.Min(point=>point.X);var maxX=bounds.Max(point=>point.X);var minY=bounds.Min(point=>point.Y);var maxY=bounds.Max(point=>point.Y);
        var scale=Math.Min((width-2*pad)/Math.Max(1,maxX-minX),(height-2*pad)/Math.Max(1,maxY-minY));
        Point P(Point point)=>new(width/2+(point.X-(minX+maxX)/2)*scale,height/2-(point.Y-(minY+maxY)/2)*scale);
        var svg=new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}' viewBox='0 0 {width} {height}' role='img' aria-label='{H(title)}'><title>{H(title)}</title><rect width='100%' height='100%' fill='#faf8f1'/>");
        foreach(var line in lines){var a=P(line.A);var b=P(line.B);svg.Append($"<line x1='{F(a.X)}' y1='{F(a.Y)}' x2='{F(b.X)}' y2='{F(b.Y)}' stroke='{line.Colour}' stroke-width='{F(line.Width)}' {(line.Dashed?"stroke-dasharray='4 4'":"")}/>");}
        var ink=lines.Where(line=>!line.Guide&&!line.Dashed&&line.From is not null&&line.To is not null).ToArray();
        var incidence=new Dictionary<IElement,List<(Point Point,string Colour)>>(ReferenceEqualityComparer.Instance);
        foreach(var line in ink)foreach(var end in new[]{(line.From!,line.A),(line.To!,line.B)})
        {if(!incidence.TryGetValue(end.Item1,out var members))incidence.Add(end.Item1,members=[]);members.Add((end.Item2,line.Colour));}
        foreach(var entry in incidence.Values.Where(members=>members.Count>=3))
        {var at=P(entry[0].Point);svg.Append($"<circle class='join' cx='{F(at.X)}' cy='{F(at.Y)}' r='2.6' fill='{entry[0].Colour}'><title>Actual shared branch role</title></circle>");}
        var bridges=new HashSet<string>();
        for(var i=0;i<ink.Length;i++)for(var j=i+1;j<ink.Length;j++)
        {
            var a=ink[i];var b=ink[j];
            if(ReferenceEquals(a.From,b.From)||ReferenceEquals(a.From,b.To)||ReferenceEquals(a.To,b.From)||ReferenceEquals(a.To,b.To))continue;
            var ux=a.B.X-a.A.X;var uy=a.B.Y-a.A.Y;var vx=b.B.X-b.A.X;var vy=b.B.Y-b.A.Y;var denominator=ux*vy-uy*vx;
            if(Math.Abs(denominator)<1e-8)continue;var dx=b.A.X-a.A.X;var dy=b.A.Y-a.A.Y;var t=(dx*vy-dy*vx)/denominator;var u=(dx*uy-dy*ux)/denominator;
            if(t< -1e-6||t>1+1e-6||u< -1e-6||u>1+1e-6)continue;
            var at=P(new(a.A.X+t*ux,a.A.Y+t*uy));if(!bridges.Add(F(at.X)+"/"+F(at.Y)+"/"+b.Colour))continue;
            var first=P(b.A);var last=P(b.B);var length=Math.Sqrt((last.X-first.X)*(last.X-first.X)+(last.Y-first.Y)*(last.Y-first.Y));if(length<1e-8)continue;
            var hx=(last.X-first.X)/length;var hy=(last.Y-first.Y)/length;
            var before=Math.Max(-4,-Math.Clamp(u,0,1)*length);var after=Math.Min(4,(1-Math.Clamp(u,0,1))*length);
            svg.Append($"<g class='overpass'><title>Projected crossing, separate native roles</title><circle cx='{F(at.X)}' cy='{F(at.Y)}' r='3.8' fill='#faf8f1'/><line x1='{F(at.X+hx*before)}' y1='{F(at.Y+hy*before)}' x2='{F(at.X+hx*after)}' y2='{F(at.Y+hy*after)}' stroke='{b.Colour}' stroke-width='{F(b.Width)}'/></g>");
        }
        return svg.Append("</svg>").ToString();
    }
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen=new HashSet<IElement>(ReferenceEqualityComparer.Instance);var todo=new Stack<IElement>();todo.Push(root);
        while(todo.TryPop(out var item)){if(!seen.Add(item))continue;yield return item;if(item is Relation relation){todo.Push(relation.Left);todo.Push(relation.Right);}}
    }
    private static string Html(string cards,string data)=>"""
<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Gojo · Overlapping ornament perspectives</title><style>
:root{font-family:Segoe UI,system-ui,sans-serif;color:#233c3c;background:#e8ede8}*{box-sizing:border-box}body{margin:0}main{max-width:1440px;margin:auto;padding:35px 24px 70px}h1{font-size:clamp(30px,4vw,47px);line-height:1.12;margin:10px 0 18px}h2{font-size:25px;margin:8px 0 13px}p{line-height:1.6;max-width:1080px}.eyebrow{font:700 12px/1.5 ui-monospace,monospace;color:#60756b;letter-spacing:.08em}.intro{padding:8px 14px 20px}a{color:#186d79}nav{display:flex;gap:10px;flex-wrap:wrap;margin:20px 0}nav a,.goals a{padding:7px 10px;border:1px solid #bac9bf;border-radius:4px;text-decoration:none}.goals{display:flex;flex-wrap:wrap;gap:10px;font-size:13px}article{background:#fffdf7;border:1px solid #c7d1c7;border-radius:9px;padding:25px;margin:24px 0;scroll-margin-top:12px}.controls{display:flex;flex-wrap:wrap;gap:14px;margin:20px 0}label{display:flex;flex-direction:column;gap:6px;font-size:13px;color:#536b5d}select{font:inherit;max-width:300px;padding:7px;border:1px solid #bdcabe;border-radius:4px;background:#fffdf7;color:#233c3c}.triple{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:14px}.compare{display:grid;grid-template-columns:1fr 1fr;gap:14px}figure{margin:10px 0;border:1px solid #e1e5da;background:#faf8f1;border-radius:5px;overflow:hidden}figcaption{padding:11px 12px;font-size:12px;color:#63766b}svg{width:100%;height:auto;display:block}details{padding:14px;background:#f0f4ec;border-radius:5px;margin-top:14px}summary{cursor:pointer;font-weight:600}.stats,.overlap-stat{font-size:13px;color:#51685b;border-top:1px solid #dfe6d8;padding-top:12px}.table-wrap{overflow:auto}table{width:100%;border-collapse:collapse;font-size:13px}td,th{text-align:left;padding:9px;border-bottom:1px solid #d7e0d1}.limits{padding:22px;background:#dce6dc;border-radius:8px}.legend{display:flex;gap:18px;flex-wrap:wrap;font-size:13px}.swatch{display:inline-block;width:12px;height:12px;margin-right:5px;border-radius:2px}@media(max-width:850px){.triple,.compare{grid-template-columns:1fr}.controls label{width:calc(50% - 7px)}select{max-width:100%}main{padding:20px 12px}article{padding:18px}}@media print{nav,.controls,details{display:none}article{break-inside:avoid}main{padding:0}body{background:white}}
</style></head><body><main><div class=intro><span class=eyebrow>GOJŌ / V01 + V02 / PRE-NUMERIC EXECUTION</span><h1>One ornament, overlapping concepts</h1><p>These are different questions over the exact same source participation. Yellow and blue readings share green components; neither reading owns or duplicates them. Grouped children can form rows, larger courts or a field, while those groupings overlap at other levels.</p><div class=legend><span><i class=swatch style="background:#e3c756"></i>Left only</span><span><i class=swatch style="background:#73ab55"></i>Shared source chambers</span><span><i class=swatch style="background:#65a0ce"></i>Right only</span><span>Dashed purple = overlap relation, not contact</span></div><nav><a href="#V01-T">Three lozenge views</a><a href="#V01-F">Overlapping courts</a><a href="#V02-V01-F">Three derived levels</a><a href="#V02-build-V01-F">Generated ornament</a></nav><div class=goals><a href="../skeleton-atlas/goals.html#G06">G06 · Triangular chambers / structural target</a><a href="../skeleton-atlas/goals.html#G07">G07 · Octagon-square incidence / structural target</a><a href="../skeleton-atlas/goals.html#G12">G12 · Shared scaffold / direct target</a><a href="../skeleton-atlas/goals.html#G11">G11 · Star corridors / related, incomplete</a></div></div>
"""+cards+"""
<section class=limits><h2>What is defined here</h2><p>A read is source + actual membership/relationship constraints + context + correspondence + complement. Names identify these definitions for discussion; they do not select their meaning. Authored grouping proposals are checked against actual source faces, seams and sites. This is symbolic derivation from existing Gojo structure, not blind image recognition or learned discovery.</p><p>The optional word-landmark read shares repeated participation in the declared additive basis. It retains its source bindings, share slots, calculation and any remainder. Equal spacing, metric centres and regular polygon angles are additional geometric claims. Three successive cell-incidence reads retain their finite boundary complements.</p><p><a href="diagnostics.json">Captured diagnostics</a> · <a href="../../../../Gojo/docs/GojoOrnamentPerspectives.md">Mechanics and findings</a> · <a href="../unit-arrangements/gallery.html">Previous unit arrangement experiment</a></p></section></main><script id=native-data type=application/json>
"""+data+"""
</script><script>
const patches=JSON.parse(document.getElementById('native-data').textContent),colours=['#d5b84e','#5794b3','#9a6b9d','#589777','#b57c55','#787ab4'];
function render(p,paint,boundary=[],stripes=null){const all=p.Faces.flatMap(f=>f.Points),xs=all.map(q=>q[0]),ys=all.map(q=>q[1]),minX=Math.min(...xs),maxX=Math.max(...xs),minY=Math.min(...ys),maxY=Math.max(...ys),s=Math.min(770/Math.max(1,maxX-minX),400/Math.max(1,maxY-minY)),pt=q=>[410+(q[0]-(minX+maxX)/2)*s,225-(q[1]-(minY+maxY)/2)*s],line=(q,c,w)=>{const a=pt(q.slice(0,2)),b=pt(q.slice(2));return `<line x1="${a[0]}" y1="${a[1]}" x2="${b[0]}" y2="${b[1]}" stroke="${c}" stroke-width="${w}"/>`};let defs='',body='';if(stripes)for(const [id,list] of Object.entries(stripes)){const colors=list.map(i=>colours[i%colours.length]);defs+=`<pattern id="m${p.Id}${id}" width="${colors.length*7}" height="10" patternUnits="userSpaceOnUse" patternTransform="rotate(35)">${colors.map((c,i)=>`<rect x="${i*7}" width="7" height="10" fill="${c}"/>`).join('')}</pattern>`;}for(const f of p.Faces){let fill=paint[f.Id]||'#f5f3eb';if(stripes&&stripes[f.Id])fill=`url(#m${p.Id}${f.Id})`;body+=`<polygon data-face="${f.Id}" points="${f.Points.map(q=>pt(q).join(',')).join(' ')}" fill="${fill}" fill-opacity=".75"><title>Actual source chamber ${f.Id+1}${stripes&&stripes[f.Id]?' · groups '+stripes[f.Id].map(i=>i+1).join(', '):''}</title></polygon>`;}body+=p.Lines.map(q=>line(q,'#9da89f',.9)).join('');body+=boundary.map(q=>line(q,'#435b50',2.8)).join('');return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 820 450" role="img" aria-label="Captured source participation"><defs>${defs}</defs>${body}</svg>`;}
document.querySelectorAll('.view-card').forEach((card,index)=>{const p=patches[index],lv=card.querySelector('.left-view'),rv=card.querySelector('.right-view'),lg=card.querySelector('.left-group'),rg=card.querySelector('.right-group');for(const select of [lv,rv])p.Views.forEach((v,i)=>select.add(new Option(v.Id+' · '+v.Name,i)));rv.value='1';function options(select,view){select.replaceChildren();p.Views[view].Groups.forEach((g,i)=>select.add(new Option('Group '+(i+1)+' · '+g.Faces.length+' chambers',i)));}function update(){const a=p.Views[lv.value].Groups[lg.value],b=p.Views[rv.value].Groups[rg.value],key=`${lv.value}/${lg.value}/${rv.value}/${rg.value}`,w=p.Pairs[key];if(!w){card.querySelector('.overlap-stat').textContent='No captured pair witness available.';return;}const left={},right={},both={};a.Faces.forEach(id=>left[id]='#e3c756');b.Faces.forEach(id=>right[id]='#65a0ce');w.Left.forEach(id=>both[id]='#e3c756');w.Right.forEach(id=>both[id]='#65a0ce');w.Shared.forEach(id=>both[id]='#73ab55');card.querySelector('.left-picture').innerHTML=render(p,left,a.Boundary);card.querySelector('.right-picture').innerHTML=render(p,right,b.Boundary);card.querySelector('.overlap-picture').innerHTML=render(p,both);card.querySelector('.overlap-stat').textContent=`${w.Left.length} left-only · ${w.Shared.length} shared · ${w.Right.length} right-only source chambers. Membership comes from the captured native correspondence.`;const memberships={};p.Views[lv.value].Groups.forEach((g,i)=>g.Faces.forEach(id=>(memberships[id]??=[]).push(i)));card.querySelector('.all-picture').innerHTML=render(p,{},[],memberships);card.querySelector('.view-description').textContent=p.Views[lv.value].Description;}function choose(){let best=null;for(const a of p.Views[lv.value].Groups)for(const b of p.Views[rv.value].Groups){const w=p.Pairs[`${lv.value}/${a.Id}/${rv.value}/${b.Id}`];if(w&&w.Shared.length&&w.Left.length&&w.Right.length&&(!best||Math.abs(a.Id-p.Views[lv.value].Groups.length/2)<best.distance))best={a:a.Id,b:b.Id,distance:Math.abs(a.Id-p.Views[lv.value].Groups.length/2)};}if(best){lg.value=best.a;rg.value=best.b;}update();}options(lg,lv.value);options(rg,rv.value);choose();lv.addEventListener('change',()=>{options(lg,lv.value);choose();});rv.addEventListener('change',()=>{options(rg,rv.value);choose();});lg.addEventListener('change',update);rg.addEventListener('change',update);});
</script></body></html>
""";
    private static Relation S(params IElement[] values)=>StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope)=>FiniteLaw.Entries(scope);
    private static string H(string value)=>WebUtility.HtmlEncode(value);
    private static string F(double value)=>value.ToString("0.###",CultureInfo.InvariantCulture);
}
