using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Discrete controls select captured, natively checked counterfactual
/// states. Browser code presents witnesses; it does not infer match validity.</summary>
public static class OrnamentPerturbationExperiment
{
    private sealed record Point(double X,double Y);
    private sealed record Verdict(int Supported,int Refuted,int Unresolved);
    public static int Run(string[] args)
    {
        var output=Path.Combine("experiments","QualitativeFrieze","results","ornament-perturbations");
        for(var i=0;i<args.Length;i++)if(args[i]=="--output"&&i+1<args.Length)output=args[++i];else throw new ArgumentException("Usage: --ornament-perturbations [--output DIRECTORY]");
        Directory.CreateDirectory(output);var watch=System.Diagnostics.Stopwatch.StartNew();
        var study=new OrnamentPerturbationStudy(message=>Console.WriteLine($"[{watch.Elapsed.TotalSeconds:0.0}s] {message}"));
        var models=new List<object>();var cards=new StringBuilder();
        foreach(var series in study.Experiments)
        {
            var frames=new List<object>();
            foreach(var state in series.States)
            {
                var views=new Dictionary<string,string>();
                foreach(var mode in new[]{"cells","forward","reverse","repeat"})
                {
                    var svg=Picture(study,state,mode);views.Add(mode,svg);
                    if(mode=="forward")File.WriteAllText(Path.Combine(output,series.Id+"-"+state.Id+".svg"),svg);
                }
                frames.Add(new{state.Id,state.Name,state.Description,Views=views,Counts=Counts(state),Questions=QuestionTables(study,state),
                    FreshComplete=state.Fresh.IsComplete,FreshRoutes=E(state.Fresh.Routes).Length});
            }
            models.Add(new{series.Id,Frames=frames});
            cards.Append($"<article id='{series.Id}' class=perturb-card data-series='{models.Count-1}'><div class=copy><span class=eyebrow>{series.Id} · PRESERVED CANDIDATE → REAPPLICATION</span><h2>{H(series.Name)}</h2><p>{H(series.Description)}</p></div><div class=controls><button class=previous aria-label='Previous state'>←</button><input class=step type=range min=0 max='{series.States.Length-1}' value=0 aria-label='Perturbation state'><button class=next aria-label='Next state'>→</button><button class=reset>Original</button><label>Read <select class=mode><option value=forward>Falling / rising vine</option><option value=reverse>Complementary vine</option><option value=cells>Closed cell patterns</option><option value=repeat>Exact landmark repetition</option></select></label></div><div class=state-copy><h3 class=state-title></h3><p class=state-description></p><p class=stats></p></div><div class=compare><figure><figcaption>Preserved original candidate</figcaption><div class=before></div></figure><figure><figcaption>Same candidate in the selected state</figcaption><div class=after></div></figure></div><div class=copy><p class=scan></p><details><summary>Compare heading and translation questions</summary><div class=questions></div><p class=small>Training and held-out evidence lie on opposite sides of a declared landmark cut. Crossing edges remain carried. Counts come from native recurrence, coverage, branching and question-description witnesses; there is no weighted beauty score. Held-out frontiers are diagnostics and do not feed training selection.</p></details></div></article>");
        }
        var native=Traverse(study.Surface).ToArray();
        var diagnostics=new{Schema="gojo-ornament-perturbations-v1",Seconds=watch.Elapsed.TotalSeconds,NativeElements=native.Length,NativeNumericElements=native.Count(item=>item is Carrier or CarrierLocation),
            TrainingEdges=E(study.TrainingEdges).Length,HeldOutEdges=E(study.HeldOutEdges).Length,TrainingOccurrences=E(study.TrainingOccurrences).Length,HeldOutOccurrences=E(study.HeldOutOccurrences).Length,
            Series=study.Experiments.Select(series=>new{series.Id,series.Name,States=series.States.Select(state=>new{state.Id,state.Name,Counts=Counts(state),FreshComplete=state.Fresh.IsComplete,FreshRoutes=E(state.Fresh.Routes).Length,
                HeadingQuestions=Measures(state.HeadingComparison,study.HeadingQuestions),TranslationQuestions=Measures(state.TranslationComparison,study.TranslationQuestions)})}),
            Limits=new[]{"Controlled counterfactual structural patches, not rerun material drawing or automatic learning.","Old claims remain historically valid; statuses concern their application to the changed source.","Cell recurrence checks supplied topological boundaries, not nondegenerate Euclidean geometry.","Route revalidation checks the old ordered walk and headings, not continued maximality or uniqueness.","Fresh full-field discovery can remain globally incomplete while local old-candidate checks still support remote walks.","Held-out recurrence is deterministic conditional evidence; questions, cut and perturbation probe are authored. No independent statistical generalization or universal aesthetic law is claimed."}};
        File.WriteAllText(Path.Combine(output,"diagnostics.json"),JsonSerializer.Serialize(diagnostics,new JsonSerializerOptions{WriteIndented=true}));
        File.WriteAllText(Path.Combine(output,"gallery.html"),Head+cards+"</main><script id=native-data type=application/json>"+JsonSerializer.Serialize(models)+"</script>"+Tail);
        Console.WriteLine(JsonSerializer.Serialize(diagnostics,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("Wrote "+Path.GetFullPath(output));return 0;
    }

    private static object Counts(OrnamentPerturbationStudy.State state)=>new{Cells=V(state.Cells),Forward=V(state.Forward),Reverse=V(state.Reverse),ExactRepeat=V(state.Rhythm),CompleteLandmarks=E(state.LandmarkBindings).Cast<Relation>().Count(binding=>N.Read((Relation)binding.Right,out _))};
    private static Verdict V(params OrnamentCandidateRevalidation[] reads)=>new(reads.Sum(read=>E(read.Supported).Length),reads.Sum(read=>E(read.Refuted).Length),reads.Sum(read=>E(read.Unresolved).Length));
    private static object[] Measures(OrnamentQuestionComparison comparison,OrnamentPerturbationStudy.NamedQuestion[] names)=>comparison.Candidates.Select(candidate=>(object)new{Name=names.Single(name=>ReferenceEquals(name.Definition,candidate.Definition)).Name,
        Training=M(candidate.Training),HeldOut=M(candidate.HeldOut),TrainingFrontier=E(comparison.TrainingPareto).Any(row=>ReferenceEquals(row,candidate.Surface)),HeldOutFrontier=E(comparison.HeldOutPareto).Any(row=>ReferenceEquals(row,candidate.Surface))}).ToArray();
    private static object M(OrnamentQuestionMeasure measure)=>new{measure.IsComplete,Recurrences=measure.RecurrenceCount,Covered=measure.CoverageCount,Uncovered=measure.UncoveredCount,Branching=measure.BranchingCount,Description=measure.DescriptionCount};
    private static string QuestionTables(OrnamentPerturbationStudy study,OrnamentPerturbationStudy.State state)
    {
        return Table("Heading questions",state.HeadingComparison,study.HeadingQuestions)+Table("Translation questions",state.TranslationComparison,study.TranslationQuestions)+"<p class=small>Support count is question-specific: qualified ordered walks for headings, repeated successor chains for translations. A broken long walk can become several qualifying fragments. Cheap questions with no matches can remain on the Pareto frontier; choosing one requires an explicit utility or minimum-support requirement.</p>";
        static string Table(string title,OrnamentQuestionComparison comparison,OrnamentPerturbationStudy.NamedQuestion[] names)
        {
            var body=new StringBuilder();
            foreach(var candidate in comparison.Candidates)
            {
                var a=candidate.Training;var b=candidate.HeldOut;
                var frontier=E(comparison.TrainingPareto).Any(row=>ReferenceEquals(row,candidate.Surface))?"training":"";
                if(E(comparison.HeldOutPareto).Any(row=>ReferenceEquals(row,candidate.Surface)))frontier+=(frontier.Length==0?"":" + ")+"held-out";
                body.Append($"<tr><td>{H(names.Single(name=>ReferenceEquals(name.Definition,candidate.Definition)).Name)}</td><td>{(a.IsComplete?a.RecurrenceCount.ToString():"open")}</td><td>{(b.IsComplete?b.RecurrenceCount.ToString():"open")}</td><td>{(b.IsComplete?b.CoverageCount+" / "+(b.CoverageCount+b.UncoveredCount):"open")}</td><td>{(b.IsComplete?b.BranchingCount.ToString():"open")}</td><td>{b.DescriptionCount}</td><td>{frontier}</td></tr>");
            }
            return $"<h3>{title}</h3><div class=table-wrap><table><thead><tr><th>Question</th><th>Train supports</th><th>Held-out supports</th><th>Held-out coverage</th><th>Branches / conflicts</th><th>Description</th><th>Pareto frontier</th></tr></thead><tbody>{body}</tbody></table></div>";
        }
    }

    private static string Picture(OrnamentPerturbationStudy study,OrnamentPerturbationStudy.State state,string mode)
    {
        const double width=900,height=590,pad=66;var field=study.Field;var basis=field.Levels[0];
        var old=Positions(field,basis.Bindings);var current=Positions(field,state.Bindings);var positions=new Dictionary<IElement,Point>(old,ReferenceEqualityComparer.Instance);
        foreach(var pair in current)positions[pair.Key]=pair.Value;
        var bounds=Positions(field,field.Source.Bindings).Values.ToArray();var minX=bounds.Min(p=>p.X);var maxX=bounds.Max(p=>p.X);var minY=bounds.Min(p=>p.Y);var maxY=bounds.Max(p=>p.Y);
        var scale=Math.Min((width-2*pad)/(maxX-minX),(height-2*pad)/(maxY-minY));
        Point P(Point p)=>new(width/2+(p.X-(minX+maxX)/2)*scale,height/2-(p.Y-(minY+maxY)/2)*scale);
        var svg=new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}' viewBox='0 0 {width} {height}' role='img' aria-label='{H(state.Name+" / "+mode)}'><title>{H(state.Name+" / "+mode)}</title><rect width='100%' height='100%' fill='#fbf7ec'/>");
        var cut=P(Positions(field,N.S(new Relation(new Placeholder(),study.PartitionEvidence.Left))).Values.Single());svg.Append($"<path d='M 35 {F(cut.Y)} H {width-35}' stroke='#aaa995' stroke-dasharray='3 6' opacity='.45'/><text x='38' y='{F(cut.Y-8)}' font-family='sans-serif' font-size='11' fill='#858575'>held-out ↑ · training ↓</text><g class='carried-history' opacity='.20'>");
        foreach(var execution in field.Executions)Background(execution.Join!.Graph,execution.Join.Bindings);
        foreach(var node in field.Decorations)Background(node.Current.Join.Graph,node.Current.Join.Bindings);
        svg.Append("</g><g class='active-topology' opacity='.43'>");
        foreach(var edge in E((Relation)((Relation)state.Topology.Right).Left).Cast<Relation>())
        {var ends=(Relation)edge.Right;var known=current.ContainsKey(ends.Left)&&current.ContainsKey(ends.Right);svg.Append(Line(P(positions[ends.Left]),P(positions[ends.Right]),known?"#607c76":"#b18036",1.1,!known));}
        svg.Append("</g>");
        if(mode=="cells")foreach(var read in state.Cells)foreach(var (occurrence,status) in Verdicts(read))
        {
            study.Diamonds.Cells!.TryReadOccurrence((Relation)read.Candidate,occurrence,out _,out var boundary,out _);
            var points=E(boundary).Cast<Relation>().Select(part=>P(positions[((Relation)part.Right).Left])).ToArray();
            svg.Append($"<polygon data-status='{status}' points='{string.Join(" ",points.Select(point=>F(point.X)+","+F(point.Y)))}' fill='{Colour(status)}' fill-opacity='.035' stroke='{Colour(status)}' stroke-width='2.2' {(status=="supported"?"":"stroke-dasharray='5 4'")}/>");
        }
        if(mode is "forward" or "reverse")
        {
            var read=mode=="forward"?state.Forward:state.Reverse;var previous=mode=="forward"?study.Forward.Strands!:study.Reverse.Strands!;
            foreach(var (occurrence,status) in Verdicts(read))
            {
                previous.TryReadRoute(occurrence,out var route);
                foreach(var use in route.Uses)
                {var ends=(Relation)use.Right;svg.Append(Line(P(positions[ends.Left]),P(positions[ends.Right]),Colour(status),3.4,status!="supported",status));}
            }
        }
        if(mode=="repeat")
        {
            var anchors=Positions(field,state.LandmarkBindings);var prior=Positions(field,study.Diamonds.Rhythm!.Bindings);
            foreach(var (occurrence,status) in Verdicts(state.Rhythm))
            {
                study.Diamonds.Rhythm.TryReadSuccessor(occurrence,out var from,out var to,out _,out _);
                var a=P(anchors.GetValueOrDefault(from,prior[from]));var b=P(anchors.GetValueOrDefault(to,prior[to]));
                svg.Append(Line(a,b,Colour(status),2.5,status!="supported",status));
            }
            foreach(var item in E(state.LandmarkBindings).Cast<Relation>())
            {
                var known=anchors.TryGetValue(item.Left,out var at);at??=prior[item.Left];var p=P(at);
                svg.Append($"<circle cx='{F(p.X)}' cy='{F(p.Y)}' r='5' fill='{(known?"#fbf7ec":"#e5b465")}' stroke='{(known?"#4b8070":"#ad762b")}' stroke-width='1.5'><title>{(known?"Complete equal-share landmark":"Old landmark position; current equal-share read is open")}</title></circle>");
            }
        }
        if(mode=="cells")
        {
            var anchors=Positions(field,state.LandmarkBindings);var prior=Positions(field,study.Diamonds.Rhythm!.Bindings);
            foreach(var binding in E(state.LandmarkBindings).Cast<Relation>())
            {
                var known=anchors.TryGetValue(binding.Left,out var at);at??=prior[binding.Left];var p=P(at);
                svg.Append($"<g class='landmark'><circle cx='{F(p.X)}' cy='{F(p.Y)}' r='4' fill='{(known?"#7a9471":"#d3a052")}'/><title>{(known?"Complete selected landmark":"Current landmark unresolved; historical guide only")}</title></g>");
            }
        }
        var target=P(positions[study.TargetRole]);var targetKnown=current.ContainsKey(study.TargetRole);
        var degree=new Dictionary<IElement,int>(ReferenceEqualityComparer.Instance);
        foreach(var edge in E((Relation)((Relation)state.Topology.Right).Left).Cast<Relation>())foreach(var role in new[]{((Relation)edge.Right).Left,((Relation)edge.Right).Right})degree[role]=degree.GetValueOrDefault(role)+1;
        foreach(var pair in degree.Where(pair=>pair.Value>=3&&current.ContainsKey(pair.Key)))
        {var at=P(current[pair.Key]);svg.Append($"<circle class='shared-branch' cx='{F(at.X)}' cy='{F(at.Y)}' r='2.2' fill='#537369'><title>Actual shared branch role</title></circle>");}
        svg.Append($"<circle class='target' cx='{F(target.X)}' cy='{F(target.Y)}' r='9' fill='none' stroke='{(targetKnown?"#645086":"#c1842d")}' stroke-width='2'/><text x='{F(target.X+13)}' y='{F(target.Y-10)}' font-family='sans-serif' font-size='12' fill='#645086'>{(state.Id=="original"?"probe role":targetKnown?"edited role":"unresolved role · prior position")}</text>");
        if(state.Id=="detach")svg.Append($"<circle class='detached' cx='{F(target.X)}' cy='{F(target.Y)}' r='4' fill='#fbf7ec' stroke='#b94d4d' stroke-width='2'/><text x='{F(target.X+13)}' y='{F(target.Y+16)}' font-family='sans-serif' font-size='12' fill='#a14d4d'>two roles · same word</text>");
        if(state.Id=="withdraw")
        {var ends=(Relation)study.TargetEdge.Right;var a=P(old[ends.Left]);var b=P(old[ends.Right]);svg.Append(Line(a,b,"#b84e4f",5,true));}
        return svg.Append("</svg>").ToString();

        void Background(QualitativeStrokeGraph graph,Relation bindings)
        {
            var words=Positions(field,bindings);
            foreach(var stroke in E(graph.Strokes).Cast<Relation>())
            {QualitativeStrokeGraph.TryReadStroke(stroke,out _,out var a,out var b,out _);svg.Append(Line(P(words[a]),P(words[b]),"#7c8b78",1.1));}
        }
    }
    private static IEnumerable<(Relation Occurrence,string Status)> Verdicts(OrnamentCandidateRevalidation read)
    {foreach(var scope in new[]{(read.Supported,"supported"),(read.Refuted,"refuted"),(read.Unresolved,"unresolved")})foreach(var witness in E(scope.Item1).Cast<Relation>())if(witness.Left is Relation occurrence)yield return(occurrence,scope.Item2);}
    private static Dictionary<IElement,Point> Positions(OrnamentPerspectiveStudy.Patch field,Relation bindings)
    {
        var output=new Dictionary<IElement,Point>(ReferenceEqualityComparer.Instance);
        foreach(var binding in E(bindings).Cast<Relation>())
        {
            if(binding.Right is not Relation word||!N.Read(word,out var moves))continue;var x=0d;var y=0d;var ready=true;
            foreach(var item in moves)
            {
                if(item is not Relation move||!field.Source.Frame.Direction(move,out var axis,out var forward,out var reach)||!ReferenceEquals(reach,field.Source.Reach)){ready=false;break;}
                if(ReferenceEquals(axis,field.Source.Frame.Horizontal))x+=forward?1:-1;else y+=forward?1:-1;
            }
            if(ready)output.Add(binding.Left,new(x,y));
        }
        return output;
    }
    private static string Colour(string status)=>status=="supported"?"#397b64":status=="refuted"?"#b64e50":"#bd842e";
    private static string Line(Point a,Point b,string colour,double width,bool dashed=false,string? status=null)=>$"<line {(status is null?"":"data-status='"+status+"'")} x1='{F(a.X)}' y1='{F(a.Y)}' x2='{F(b.X)}' y2='{F(b.Y)}' stroke='{colour}' stroke-width='{F(width)}' {(dashed?"stroke-dasharray='5 4'":"")} stroke-linecap='round'/>";
    private static string F(double number)=>number.ToString("0.###",CultureInfo.InvariantCulture);
    private static string H(string text)=>WebUtility.HtmlEncode(text);
    private static IElement[] E(Relation scope)=>N.E(scope);
    private static IEnumerable<IElement> Traverse(IElement root)
    {var seen=new HashSet<IElement>(ReferenceEqualityComparer.Instance);var todo=new Stack<IElement>();todo.Push(root);while(todo.TryPop(out var item)){if(!seen.Add(item))continue;yield return item;if(item is Relation pair){todo.Push(pair.Left);todo.Push(pair.Right);}}}
    private const string Head="""
<!doctype html><html lang=en><meta charset=utf-8><meta name=viewport content='width=device-width,initial-scale=1'><title>What survives a structural change</title><style>
*{box-sizing:border-box}body{margin:0;background:#eae8df;color:#2c413a;font:16px/1.6 system-ui,sans-serif}header,main{max-width:1420px;margin:auto;padding:28px}header{padding-top:50px}h1{font:clamp(34px,5vw,60px)/1.1 Georgia,serif;max-width:950px}h2{font:32px/1.2 Georgia,serif;margin:12px 0}h3{font-size:18px}p{max-width:1000px}a{color:#765374}article{border:1px solid #cdc8b8;background:#fbf7ec;margin:12px 0 40px;border-radius:5px;overflow:hidden;scroll-margin-top:20px}.copy,.state-copy{padding:22px 28px}.eyebrow{font-size:11px;letter-spacing:.14em;color:#7c6b52;font-weight:700}.controls{display:flex;align-items:center;flex-wrap:wrap;gap:12px;padding:15px 28px;background:#f3efe3;border-block:1px solid #dfd8c6}.controls label{display:flex;align-items:center;gap:8px}input{width:200px;accent-color:#725b84}select,button{font:inherit;font-size:14px;padding:7px;border:1px solid #bfb6a2;background:#faf7ed;border-radius:4px;color:#30463e;cursor:pointer}.compare{display:grid;grid-template-columns:1fr 1fr;gap:1px;background:#ddd7c9}figure{margin:0;background:#fbf7ec}figcaption{padding:10px 28px;font-size:12px;font-weight:600}svg{width:100%;height:auto;display:block}.stats{font-size:14px;font-weight:600;max-width:none}.state-title{margin:0}.state-description{margin:8px 0}.small,summary{font-size:14px}.table-wrap{overflow:auto}table{border-collapse:collapse;width:100%;font-size:13px;white-space:nowrap}th,td{text-align:left;padding:8px 12px;border-bottom:1px solid #ded6c4}th{font-weight:600}summary{cursor:pointer}.legend{display:flex;gap:20px;flex-wrap:wrap;font-size:14px}.legend span:before{content:'●';margin-right:8px}.yes:before{color:#397b64}.no:before{color:#b64e50}.open:before{color:#bd842e}@media(max-width:700px){header,main{padding:15px}.copy,.state-copy,.controls{padding:18px}.compare{grid-template-columns:1fr}input{width:130px}h2{font-size:27px}}
</style><header><span class=eyebrow>GOJŌ · PERTURBATION AND QUESTION COMPARISON</span><h1>What survives a structural change?</h1><p>Each step reapplies the preserved original candidate to one explicit structural edit. Closed-cell cycles, qualitative routes and exact landmark repetition can respond differently to the same change.</p><p>The pale complete field carries the previously executed D1, D2 and marker layers. Stronger lines show the current counterfactual D1 interpretation. These are structural patch experiments; sibling machines have not been rerun.</p><div class=legend><span class=yes>Supported in this state</span><span class=no>Counterexample in this state</span><span class=open>Unresolved obligations</span></div><p><a href='#P01'>P01 Word steps</a> · <a href='#P02'>P02 Connections</a> · <a href='#P03'>P03 Unavailable detail</a> · <a href='../ornament-discovery/gallery.html'>Original dominant readings</a> · <a href='../../../../Gojo/docs/GojoOrnamentPerturbations.md'>Mechanics and findings</a> · <a href='diagnostics.json'>Native diagnostics</a></p></header><main>
""";
    private const string Tail="""
<script>
const experiments=JSON.parse(document.getElementById('native-data').textContent);for(const card of document.querySelectorAll('.perturb-card')){const experiment=experiments[Number(card.dataset.series)],step=card.querySelector('.step'),mode=card.querySelector('.mode');if(experiment.Id==='P02')mode.value='cells';function draw(){const frame=experiment.Frames[Number(step.value)],basis=experiment.Frames[0],key=mode.value,counts=frame.Counts,names={cells:'Cells',forward:'Forward',reverse:'Reverse',repeat:'ExactRepeat'},c=counts[names[key]];card.dataset.state=frame.Id;card.dataset.mode=key;card.querySelector('.state-title').textContent=frame.Name;card.querySelector('.state-description').textContent=frame.Description;card.querySelector('.before').innerHTML=basis.Views[key];card.querySelector('.after').innerHTML=frame.Views[key];card.querySelector('.stats').textContent=c.Supported+' supported · '+c.Refuted+' refuted · '+c.Unresolved+' unresolved | '+counts.CompleteLandmarks+' / 24 complete selected landmarks';card.querySelector('.questions').innerHTML=frame.Questions;card.querySelector('.scan').textContent=frame.FreshComplete?'Fresh full-field route scan: '+frame.FreshRoutes+' route candidates, including short fragments.':'Fresh full-field route scan is incomplete. Local revalidation still checks each preserved walk from its own dependencies.';card.querySelector('.previous').disabled=Number(step.value)===0;card.querySelector('.next').disabled=Number(step.value)===experiment.Frames.length-1;}step.oninput=draw;mode.onchange=draw;card.querySelector('.previous').onclick=()=>{step.value=Number(step.value)-1;draw();};card.querySelector('.next').onclick=()=>{step.value=Number(step.value)+1;draw();};card.querySelector('.reset').onclick=()=>{step.value=0;draw();};draw();}
</script></html>
""";
}
