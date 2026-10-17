using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Optional numeric display of native captures. Segment intersection
/// is used solely to draw overpass gaps; it never changes native incidence,
/// finds faces, negotiates a unit, or answers a structural question.</summary>
public static class UnitArrangementExperiment
{
    private static readonly string[] Colours=["#216e79","#b25b39","#72578e","#657942","#9e782c","#3b708e"];
    private sealed record Point(double X,double Y);
    private sealed record Segment(IElement Edge,IElement From,IElement To,Point A,Point B,string Colour,bool Guide=false,bool Removed=false);
    private sealed record Fill(Point[] Points,string Colour);
    private sealed class Picture
    {
        public List<Segment> Lines { get; }=[];
        public List<Point> Joins { get; }=[];
        public List<Fill> Fills { get; }=[];
        public List<(Point A,Point B)> Blocked { get; }=[];
    }

    public static int Run(string[] args)
    {
        var output=Path.Combine("experiments","QualitativeFrieze","results","unit-arrangements");
        for(var i=0;i<args.Length;i++)
            if(args[i]=="--output"&&i+1<args.Length)output=args[++i];
            else throw new ArgumentException("Usage: --unit-arrangements [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var study=new UnitArrangementStudy();var cards=new StringBuilder();
        foreach(var example in study.Cases)
        {
            var after=new Picture();var before=new Picture();
            foreach(var (revision,index) in example.Before.Select((revision,index)=>(revision,index)))Add(before,revision.Join.Graph,revision.Join.Bindings,Colours[index%Colours.Length]);
            if(example.SeamView is {} joined)
            {
                var owners=new Dictionary<IElement,string>(ReferenceEqualityComparer.Instance);
                foreach(var (unit,index) in example.Units.Select((unit,index)=>(unit,index)))
                foreach(var stroke in E(unit.Current.Graph.Strokes))owners.Add(stroke,Colours[index%Colours.Length]);
                var colours=new Dictionary<IElement,string>(ReferenceEqualityComparer.Instance);
                foreach(var map in E(joined.StrokeMap).Cast<Relation>())colours.Add(map.Right,owners[map.Left]);
                Add(after,joined.Graph,joined.Bindings,Colours[0],edgeColours:colours);
                foreach(var group in E(joined.Classes).Cast<Relation>())
                    if(group.Left is Relation sites&&E((Relation)sites.Left).Length>1)
                    {
                        var binding=E(joined.Bindings).Cast<Relation>().Single(row=>ReferenceEquals(row.Left,sites.Right));
                        after.Joins.Add(Word(joined.Graph.Frame,(Relation)binding.Right));
                    }
            }
            else foreach(var (unit,index) in example.Units.Select((unit,index)=>(unit,index)))Add(after,unit.Current.Join.Graph,unit.Current.Join.Bindings,Colours[index%Colours.Length]);
            foreach(var supplier in example.Construction)
            {Add(after,supplier.Current.Join.Graph,supplier.Current.Join.Bindings,"#acb6b4",guide:true);Add(before,supplier.Current.Join.Graph,supplier.Current.Join.Bindings,"#acb6b4",guide:true);}
            foreach(var connection in example.Connections.Where(connection=>!connection.IsAligned))
                if(UnitArrangementConnection.TryReadPublication(connection.Left,connection.LeftRole,out _,out var a)&&UnitArrangementConnection.TryReadPublication(connection.Right,connection.RightRole,out _,out var b))
                    after.Blocked.Add((Word(study.Frame,(Relation)a.Right),Word(study.Frame,(Relation)b.Right)));
            var changed=example.Before.Where((revision,index)=>!ReferenceEquals(revision,example.Units[index].Current)).Any();
            var stats=$"{example.Units.Length} units · {example.Templates.Length} template surfaces · {example.Connections.Count(x=>x.IsAligned)} aligned / {example.Connections.Count(x=>!x.IsAligned)} held seams";
            cards.Append($"<article id='{example.Id}'><header><span class=eyebrow>{example.Id} · ARRANGEMENT</span><h2>{H(example.Name)}</h2><p>{H(example.Description)}</p></header>");
            if(changed)cards.Append($"<div class=compare><figure><figcaption>Before local requests</figcaption>{Svg(before,"Before local requests")}</figure><figure><figcaption>After accepted local requests</figcaption>{Svg(after,example.Name)}</figure></div>");
            else cards.Append($"<figure>{Svg(after,example.Name)}</figure>");
            cards.Append($"<p class=stats>{H(stats)}</p><details><summary>Inspect the production layers and channel contract</summary>{Details(example)}</details></article>");
            File.WriteAllText(Path.Combine(output,example.Id+".svg"),Svg(after,example.Name));
        }
        var crossing=new Picture();
        Add(crossing,study.Crossing.Independent.Current.Graph,study.Crossing.Independent.Current.Bindings,Colours[1]);
        Add(crossing,study.Crossing.Joined.Graph,study.Crossing.Joined.Bindings,Colours[0]);
        cards.Append($"<article id='A05'><header><span class=eyebrow>A05 · PARTICIPATION</span><h2>Crossing threads and a joined node</h2><p>Left: two independent diagonal strokes pass over one another. Right: four separate ray units publish centre ports; three explicit, formally checked joins produce one shared site. The renderer marks the distinction without changing it.</p></header><figure>{Svg(crossing,"Independent overpass on the left, joined node on the right")}</figure><p class=stats>2 independent strokes · 4 joined rays · 3 admitted centre joins</p></article>");
        File.WriteAllText(Path.Combine(output,"A05.svg"),Svg(crossing,"Crossing versus joining"));

        AddQuestion("Q01","Select three-edge chambers", "The question has three anonymous boundary slots. All eight bounded chambers supply a complete slot-to-edge correspondence. Colour observes the selected participation; the exterior remains separate.",study.Quilt.Triangles,study.Quilt.Bindings,null);
        AddQuestion("Q02","Ask for four-edge chambers instead", "The exact same scaffold answers a different question: none of its bounded chambers has four edges. The eight three-edge chambers remain in the complement. No ink, packet or receiver head changes.",study.Quilt.Quadrilaterals,study.Quilt.Bindings,null);
        AddQuestion("Q03","Withdraw seams, then ask again", "An explicit application withdraws two interior diagonal connections. The result has two four-edge chambers and four three-edge chambers. The same four-slot question now selects the larger spaces; removed connections remain visible as dashed history.",study.Quilt.Opened,study.Quilt.Bindings,study.Quilt.Withdrawal);
        var allUnits=study.Cases.SelectMany(x=>x.Units.Concat(x.Construction)).Concat(study.Quilt.Units).Concat(study.Crossing.Arms).Append(study.Crossing.Independent).ToArray();
        var revisions=allUnits.Select(node=>node.Current).Concat(study.Cases.SelectMany(example=>example.Before)).Distinct().ToArray();
        var capture=new Relation(study.Surface,StructuralConstraint.Closed(allUnits.Select(node=>(IElement)new Relation(node.Revisions,
            new Relation(node.PlacementExecution.Capture(),new Relation(node.Timeline,node.Unresolved)))).ToArray()));
        var numeric=Traverse(capture).Count(element=>element is Carrier or CarrierLocation);
        var packetAccounts=allUnits.Select(node=>node.PlacementExecution.Account()).Concat(revisions.Select(revision=>revision.Execution.Account())).ToArray();
        var accounted=packetAccounts.All(Accounted);
        var diagnostics=new
        {
            Schema="gojo-unit-arrangements-v1",NativeNumericElements=numeric,
            Units=allUnits.Length,HistoricalAndCurrentPacketAccountsChecked=packetAccounts.Length,AllExecutionsAccounted=accounted,
            ImportedPackets=packetAccounts.Sum(account=>E((Relation)((Relation)account.Left).Right).Length),
            Cases=study.Cases.Select(example=>new{example.Id,example.Name,Units=example.Units.Length,Templates=example.Templates.Length,
                Replaced=example.Before.Where((revision,index)=>!ReferenceEquals(revision,example.Units[index].Current)).Count(),
                Aligned=example.Connections.Count(x=>x.IsAligned),Held=example.Connections.Count(x=>!x.IsAligned),
                StaleTemplateLandmarks=example.Templates.Sum(template=>template.Landmarks.Count(landmark=>!template.TryRead(landmark,out _)))}),
            Quilt=new{study.Quilt.Scaffold.IsComplete,Faces=E(study.Quilt.Scaffold.Faces).Length,TriangleSelections=E(study.Quilt.Triangles.Selected).Length,
                FourEdgeSelectionsBefore=E(study.Quilt.Quadrilaterals.Selected).Length,Removed=E(study.Quilt.Withdrawal.Removed).Length,
                AfterComplete=study.Quilt.Withdrawal.Result.IsComplete,AfterFaces=E(study.Quilt.Withdrawal.Result.Faces).Length,
                FourEdgeSelectionsAfter=E(study.Quilt.Opened.Selected).Length,RemainingComplement=E(study.Quilt.Opened.Complement).Length},
            Crossings=new{IndependentContours=study.Crossing.Independent.Current.Graph.Describe(study.Crossing.Independent.Current.Graph.Evaluate()).Contours,
                JoinedX=study.Crossing.Joined.Graph.Describe(study.Crossing.Joined.Graph.Evaluate()).X,AcceptedCentreJoins=E(study.Crossing.Joined.Accepted).Length},
            Limits=new[]{"Finite authored laws; no learned policy or global harmony optimizer.","Requests and held seam demand are structural signals, not calibrated Carrier.Tension.",
                "Recursive landmark placements are captured dependencies with stale detection; later revisions do not silently move existing dependants.",
                "Supplied cellular embedding, not discovery of planar faces from an arbitrary drawing. Three edges do not establish metric triangle geometry.",
                "Connection withdrawal changes a derived participation view, not prior material delivery or original ink history.",
                "Bridge placement uses optional screen geometry solely for rendering; no projected crossing creates a native site."}
        };
        File.WriteAllText(Path.Combine(output,"diagnostics.json"),JsonSerializer.Serialize(diagnostics,new JsonSerializerOptions{WriteIndented=true}));
        File.WriteAllText(Path.Combine(output,"gallery.html"),Html(cards.ToString()));
        Console.WriteLine(JsonSerializer.Serialize(diagnostics,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"Wrote {Path.GetFullPath(output)}");
        return numeric!=0||!accounted||!study.Quilt.Scaffold.IsComplete||!study.Quilt.Withdrawal.Result.IsComplete||
            E(study.Quilt.Triangles.Selected).Length!=8||E(study.Quilt.Opened.Selected).Length!=2?1:0;

        void AddQuestion(string id,string name,string explanation,ScaffoldQuestion question,Relation bindings,ScaffoldQuestionWithdrawal? withdrawal)
        {
            var picture=new Picture();Add(picture,question.Source.Graph,bindings,Colours[0]);
            if(withdrawal is not null)
            {
                var removed=new QualitativeStrokeGraph(study.Frame,study.Quilt.Scaffold.Graph.Sites,withdrawal.Removed);
                Add(picture,removed,bindings,"#b06b5d",removed:true);
            }
            foreach(var match in E(question.Selected).Cast<Relation>())
            {
                if(!question.TryReadSelected(match,out _,out var boundary,out _))continue;
                var points=E(boundary).Cast<Relation>().Select(part=>((Relation)part.Right).Left).Select(site=>
                    E(bindings).Cast<Relation>().Single(row=>ReferenceEquals(row.Left,site))).Select(row=>Word(study.Frame,(Relation)row.Right)).ToArray();
                picture.Fills.Add(new(points,"#e6bb62"));
            }
            var selected=E(question.Selected).Length;var complement=E(question.Complement).Length;
            cards.Append($"<article id='{id}'><header><span class=eyebrow>{id} · QUESTION / CONTEXT</span><h2>{H(name)}</h2><p>{H(explanation)}</p></header><figure>{Svg(picture,name)}</figure><p class=stats>{selected} selected · {complement} complementary chambers · {E(question.Unresolved).Length} unresolved · 1 explicit exterior</p><details><summary>Inspect the question and its evidence</summary><p>The native question connects this exact scaffold, a closed ordered scope of {E(question.BoundarySlots).Length} distinct slots, and its context. Every selected match retains an actual face, its ordered edge boundary, and slot-to-edge correspondence. Complement and unresolved participation have separate scopes.</p><p>{(withdrawal is null?"This is observation. The source graph and actuator history are unchanged.":"The withdrawal names actual interior seam edges and records the two prior boundaries, merged boundary and cause for each accepted application. Original source, removed participation and provenance remain reachable.")}</p></details></article>");
            File.WriteAllText(Path.Combine(output,id+".svg"),Svg(picture,name));
        }
    }

    private static string Details(UnitArrangementStudy.Case example)
    {
        var rows=string.Join("",example.Units.Select(unit=>$"<tr><td>{H(unit.Name)}</td><td>{E(unit.Current.Program.Paths).Length}</td><td>{E(unit.Current.Ports).Length}</td><td>{E(unit.Revisions).Length}</td><td>{E(unit.Current.Execution.CompletionEvents).Length}</td></tr>"));
        return $"<p>Template → authenticated public landmark → placement word → local acknowledged unit → current publication. Revisions retain actual source paths, transform or refinement witnesses, drawing receipts, packet accounts and prior focus. Names and colours do not select execution.</p><p>Neighbour channels see only public surface, current head, frame, role and site binding. Their finite signal selects a local native policy; they cannot inspect either path recipe. An accepted seam still needs its separate shared-site join view.</p><div class=table-wrap><table><thead><tr><th>Unit</th><th>Paths</th><th>Ports</th><th>Revisions</th><th>Completions</th></tr></thead><tbody>{rows}</tbody></table></div><p>Template surfaces: {example.Templates.Length}. They republish selected actual ports with dependency provenance. Captured placements detect stale suppliers; automatic downstream replay is a separate experiment.</p>";
    }

    private static void Add(Picture picture,QualitativeStrokeGraph graph,Relation bindings,string colour,bool guide=false,bool removed=false,Dictionary<IElement,string>? edgeColours=null)
    {
        var positions=new Dictionary<IElement,Point>(ReferenceEqualityComparer.Instance);
        foreach(var binding in E(bindings).Cast<Relation>())positions[binding.Left]=Word(graph.Frame,(Relation)binding.Right);
        var degrees=new Dictionary<IElement,int>(ReferenceEqualityComparer.Instance);
        foreach(var stroke in E(graph.Strokes).Cast<Relation>())
        {
            if(!QualitativeStrokeGraph.TryReadStroke(stroke,out _,out var a,out var b,out _)||!positions.TryGetValue(a,out var first)||!positions.TryGetValue(b,out var second))throw new InvalidOperationException("A displayed stroke needs actual complete endpoint bindings.");
            picture.Lines.Add(new(stroke,a,b,first,second,edgeColours?.GetValueOrDefault(stroke)??colour,guide,removed));
            degrees[a]=degrees.GetValueOrDefault(a)+1;degrees[b]=degrees.GetValueOrDefault(b)+1;
        }
        if(!guide&&!removed)foreach(var site in degrees.Where(row=>row.Value>=3))picture.Joins.Add(positions[site.Key]);
    }

    private static Point Word(QualitativeMotionFrame frame,Relation word)
    {
        var x=0d;var y=0d;
        if(!StructuralConstraint.TryReadScope(word,out var terms,out var seam,out var tail)||!ReferenceEquals(seam,tail))throw new InvalidOperationException("The renderer cannot guess an open word.");
        foreach(var move in terms.Cast<Relation>())
        {
            if(!frame.Direction(move,out var axis,out var forward,out _))throw new InvalidOperationException("The renderer needs a declared axis.");
            if(ReferenceEquals(axis,frame.Horizontal))x+=forward?1:-1;else y+=forward?1:-1;
        }
        return new(x,y);
    }

    private static string Svg(Picture picture,string title)
    {
        const double width=840,height=340,padding=30;
        var points=picture.Lines.SelectMany(line=>new[]{line.A,line.B}).ToArray();
        var minX=points.Min(p=>p.X);var maxX=points.Max(p=>p.X);var minY=points.Min(p=>p.Y);var maxY=points.Max(p=>p.Y);
        var scale=Math.Min((width-2*padding)/Math.Max(1,maxX-minX),(height-2*padding)/Math.Max(1,maxY-minY));
        Point Display(Point p)=>new((p.X-(minX+maxX)/2)*scale+width/2,height/2-(p.Y-(minY+maxY)/2)*scale);
        string Position(Point p){var q=Display(p);return $"{F(q.X)},{F(q.Y)}";}
        string Line(Point a,Point b,string colour,string extra="")
        {var first=Display(a);var second=Display(b);return $"<line x1='{F(first.X)}' y1='{F(first.Y)}' x2='{F(second.X)}' y2='{F(second.Y)}' stroke='{colour}' {extra}/>";}
        var b=new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {width} {height}' width='{width}' height='{height}' role='img' aria-label='{H(title)}'><title>{H(title)}</title><rect width='100%' height='100%' fill='#faf8f1'/>");
        foreach(var fill in picture.Fills)b.Append($"<polygon points='{string.Join(" ",fill.Points.Select(Position))}' fill='{fill.Colour}' fill-opacity='.48'/>");
        foreach(var segment in picture.Lines.OrderByDescending(x=>x.Guide))
            b.Append(Line(segment.A,segment.B,segment.Colour,$"class='{(segment.Guide?"guide":segment.Removed?"removed":"ink")}' stroke-width='{(segment.Guide?1.4:segment.Removed?1.8:3.8)}' stroke-linecap='round' {(segment.Guide||segment.Removed?"stroke-dasharray='5 5'":"")}"));
        var ink=picture.Lines.Where(line=>!line.Guide&&!line.Removed).ToArray();
        for(var i=0;i<ink.Length;i++)for(var j=i+1;j<ink.Length;j++)
        {
            var lower=ink[i];var upper=ink[j];
            if(ReferenceEquals(lower.From,upper.From)||ReferenceEquals(lower.From,upper.To)||ReferenceEquals(lower.To,upper.From)||ReferenceEquals(lower.To,upper.To)||!Crossing(lower,upper,out var point))continue;
            var at=Display(point);var a=Display(upper.A);var c=Display(upper.B);var length=Math.Sqrt((c.X-a.X)*(c.X-a.X)+(c.Y-a.Y)*(c.Y-a.Y));
            if(length==0)continue;var dx=(c.X-a.X)/length*7;var dy=(c.Y-a.Y)/length*7;
            b.Append($"<g class='overpass'><title>Independent projected crossing; no shared site</title><circle cx='{F(at.X)}' cy='{F(at.Y)}' r='6.5' fill='#faf8f1'/><line x1='{F(at.X-dx)}' y1='{F(at.Y-dy)}' x2='{F(at.X+dx)}' y2='{F(at.Y+dy)}' stroke='{upper.Colour}' stroke-width='3.8'/></g>");
        }
        foreach(var point in picture.Joins.Distinct())
        {var at=Display(point);b.Append($"<circle class='join' cx='{F(at.X)}' cy='{F(at.Y)}' r='5' fill='#183f46' stroke='#faf8f1' stroke-width='1.5'><title>Actual shared site</title></circle>");}
        foreach(var (first,second) in picture.Blocked)
        {
            b.Append(Line(first,second,"#b3443c","stroke-width='2' stroke-dasharray='3 4'"));
            var at=Display(new((first.X+second.X)/2,(first.Y+second.Y)/2));
            b.Append($"<path d='M {F(at.X-5)} {F(at.Y-5)} l 10 10 m -10 0 l 10 -10' stroke='#b3443c' stroke-width='2.5'><title>Held endpoint mismatch</title></path>");
        }
        return b.Append("</svg>").ToString();
    }

    private static bool Crossing(Segment a,Segment b,out Point point)
    {
        point=null!;var ux=a.B.X-a.A.X;var uy=a.B.Y-a.A.Y;var vx=b.B.X-b.A.X;var vy=b.B.Y-b.A.Y;
        var denominator=ux*vy-uy*vx;if(Math.Abs(denominator)<1e-8)return false;
        var dx=b.A.X-a.A.X;var dy=b.A.Y-a.A.Y;var t=(dx*vy-dy*vx)/denominator;var u=(dx*uy-dy*ux)/denominator;
        if(t<=1e-6||t>=1-1e-6||u<=1e-6||u>=1-1e-6)return false;
        point=new(a.A.X+t*ux,a.A.Y+t*uy);return true;
    }

    private static bool Accounted(Relation account)
    {
        if(account.Left is not Relation source||source.Right is not Relation imported||account.Right is not Relation {Left:Relation active,Right:Relation {Left:Relation retained,Right:Relation {Left:Relation released,Right:Relation problems}}})return false;
        return E(imported).Length==E(active).Length+E(retained).Length+E(released).Length&&E(problems).Length==0;
    }
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var todo=new Stack<IElement>();var seen=new HashSet<IElement>(ReferenceEqualityComparer.Instance);todo.Push(root);
        while(todo.TryPop(out var item)){if(!seen.Add(item))continue;yield return item;if(item is Relation relation){todo.Push(relation.Left);todo.Push(relation.Right);}}
    }
    private static string Html(string cards)=>"""
<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Gojo · Units, templates and questions</title><style>
:root{font-family:Segoe UI,system-ui,sans-serif;color:#263a3b;background:#e9ede8}*{box-sizing:border-box}body{margin:0}main{max-width:1240px;margin:auto;padding:36px 24px 70px}h1{font-size:clamp(28px,4vw,44px);line-height:1.15;margin:10px 0 16px}h2{font-size:25px;margin:7px 0 12px}p{line-height:1.6;max-width:980px}.intro{padding:0 12px 20px}.eyebrow{font:700 12px/1.5 ui-monospace,monospace;letter-spacing:.09em;color:#60726b}nav{display:flex;flex-wrap:wrap;gap:8px;margin:22px 0}nav a,a{color:#176b76}nav a{border:1px solid #abbcb3;border-radius:4px;padding:6px 11px;text-decoration:none}article{background:#fffdf7;border:1px solid #c9d2c8;border-radius:10px;margin:24px 0;padding:26px;scroll-margin-top:15px}figure{margin:14px 0;background:#faf8f1;border:1px solid #e6e4d9;border-radius:6px;overflow:hidden}svg{display:block;width:100%;height:auto;min-height:180px}figcaption{padding:10px 14px;color:#63736c;font-size:13px}.compare{display:grid;grid-template-columns:1fr 1fr;gap:16px}.stats{font-size:13px;color:#52675d;border-top:1px solid #e0e5db;padding-top:12px}details{background:#f2f4ed;padding:14px;border-radius:5px}summary{cursor:pointer;font-weight:600}.table-wrap{overflow:auto}table{border-collapse:collapse;width:100%;font-size:13px}th,td{text-align:left;padding:9px;border-bottom:1px solid #d9e1d5}th{color:#5d7065}.legend{display:flex;flex-wrap:wrap;gap:18px;font-size:13px}.dot{display:inline-block;width:9px;height:9px;background:#183f46;border-radius:50%;margin-right:6px}.held{color:#b3443c;font-weight:700}.controls{display:flex;gap:16px;flex-wrap:wrap;padding:12px 0}.hide-guides .guide{display:none}.limits{background:#dce5dc;padding:20px;border-radius:8px}@media(max-width:760px){main{padding:22px 12px}.compare{grid-template-columns:1fr}article{padding:18px}h2{font-size:22px}}@media print{nav,.controls{display:none}article{break-inside:avoid}body{background:white}main{padding:0}details{display:none}}
</style></head><body><main><div class="intro"><span class="eyebrow">GOJŌ / PRE-NUMERIC EXPERIMENT</span><h1>Units → templates → questions</h1><p>Small acknowledged path machines become units. Their public landmarks let other structures place, transform and request changes from them. A completed arrangement can then become another construction scaffold—or support a different interpretation when the question changes.</p><div class="legend"><span><i class="dot"></i>Shared site / join</span><span>Bridge gap = independent crossing</span><span class="held">× Held connection</span><span>Dashed = construction or withdrawn history</span><span>Gold = selected chamber</span></div><div class="controls"><label><input id="guides" type="checkbox" checked> Show construction lines</label><a href="../primitive-flows/gallery.html">Primitive recipes</a><a href="../skeleton-atlas/goals.html#G12">G12 · One scaffold, two readings</a></div><nav><a href="#A01">A01 Drops</a><a href="#A02">A02 Negotiation</a><a href="#A03">A03 Layers</a><a href="#A04">A04 Refinement</a><a href="#A05">A05 Join / overpass</a><a href="#Q01">Q01 Three sides</a><a href="#Q02">Q02 Four sides</a><a href="#Q03">Q03 Open spaces</a></nav></div>
"""+cards+"""
<section class="limits"><h2>What this establishes</h2><p>Execution and correspondence use native relations and shared unmeasured reaches, with no numeric carriers or numeric locations. The request laws and embedding are authored premises. Screen geometry is an optional interpretation, used for display and bridge gaps only.</p><p>Neighbour requests are small structural signals with visible held demand, not continuous calibrated tension. Recursive placements retain dependencies and detect stale sources; automatic propagation after later source revisions is still open. Chamber queries validate an explicitly supplied finite embedding; they do not discover all faces in an arbitrary picture. Withdrawal changes a derived connection view and retains original history.</p><p><a href="diagnostics.json">Captured diagnostics</a> · <a href="../../../../Gojo/docs/GojoUnitArrangementExperiment.md">Experiment report</a></p></section></main><script>document.getElementById('guides').addEventListener('change',event=>document.body.classList.toggle('hide-guides',!event.target.checked));</script></body></html>
""";
    private static IElement[] E(Relation scope)=>FiniteLaw.Entries(scope);
    private static string F(double value)=>value.ToString("0.###",CultureInfo.InvariantCulture);
    private static string H(string value)=>WebUtility.HtmlEncode(value);
}
