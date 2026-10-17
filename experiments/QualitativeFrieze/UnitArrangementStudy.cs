using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Finite authored definitions, executed through acknowledged local
/// units. Repetition counts construct visible native words, never measured
/// coordinates. Only the separate renderer interprets reaches as screen sizes.</summary>
public sealed class UnitArrangementStudy
{
    public sealed record Case(string Id, string Name, string Description,
        UnitArrangementNode[] Units, UnitArrangementRevision[] Before,
        UnitArrangementNode[] Construction, UnitLandmarkTemplate[] Templates,
        UnitArrangementConnection[] Connections, Relation Evidence)
    { public QualitativeGraphJoin? SeamView { get; init; } }

    public QualitativeMotionFrame Frame { get; } = new();
    public IElement Reach { get; } = new Placeholder();
    public Case[] Cases { get; }
    public QuiltCase Quilt { get; }
    public CrossingCase Crossing { get; }
    public Relation Surface { get; }
    public QualitativePatternTransform Mirror { get; }
    public QualitativePatternTransform Turn { get; }
    private readonly List<IElement> differences = [];

    public sealed record QuiltCase(UnitArrangementNode[] Units, QualitativeGraphJoin Join,
        ScaffoldQuestionScaffold Scaffold, ScaffoldQuestion Triangles,
        ScaffoldQuestion Quadrilaterals, ScaffoldQuestionWithdrawal Withdrawal,
        ScaffoldQuestion Opened, Relation Bindings, Relation Definition);
    public sealed record CrossingCase(UnitArrangementNode Independent, UnitArrangementNode[] Arms,
        QualitativeGraphJoin Joined, Relation Surface);

    public UnitArrangementStudy()
    {
        Mirror = Map((Frame.Right, W(Frame.Right)), (Frame.Up, W(Frame.Down)),
            (Frame.Left, W(Frame.Left)), (Frame.Down, W(Frame.Up)));
        Turn = Map((Frame.Right, W(Frame.Up)), (Frame.Up, W(Frame.Left)),
            (Frame.Left, W(Frame.Down)), (Frame.Down, W(Frame.Right)));
        Cases = [Drops(), MirroredNeighbours(), RotatedLandmarks(), RefinedDiamonds()];
        Quilt = BuildQuilt();
        Crossing = BuildCrossing();
        Surface = new(Frame.Surface, new Relation(S(Cases.Select(x => (IElement)x.Evidence).ToArray()),
            new Relation(Quilt.Definition, new Relation(Crossing.Surface,S(differences.ToArray())))));
    }

    private Case Drops()
    {
        var roles = Enumerable.Range(0, 12).Select(_ => (IElement)new Placeholder()).ToArray();
        var placements = Enumerable.Range(0, 12).Select(i => P((i % 4) * 6 + (i / 4 % 2) * 3, i / 4 * 5)).ToArray();
        var supplier = Markers("half-drop landmark supplier", placements, roles);
        var template = new UnitLandmarkTemplate(roles.Select(role => ((IUnitArrangementEndpoint)supplier, role)), supplier.Surface);
        var diamond = Polygon("four opposed diamond sides", [P(2,0),P(0,2),P(-2,0),P(0,-2)], out _);
        var dependencies = new List<IElement>();
        var units = template.Landmarks.Select((landmark,i) => Place($"diamond {i+1}", diamond, template, landmark, dependencies)).ToArray();
        return Result("A01", "Half-drop diamond units", "One repeated four-side definition is placed at twelve public template landmarks. The row drop is a repeated word; no pixel coordinate enters execution.", units, [supplier], [template], [], dependencies);
    }

    private Case MirroredNeighbours()
    {
        var hook = OpenPath("folded passage", [P(-3,-1),P(-2,-1),P(-2,1),P(0,1),P(0,-1),P(2,-1),P(2,1),P(3,1)], out var ends);
        var reflected = hook.Transform("reflected folded passage", Mirror);
        var flip = new Placeholder();
        var policy = Policy([hook,reflected], flip, reflected);
        var units = Enumerable.Range(0, 8).Select(i => new UnitArrangementNode($"passage {i+1}", [hook,reflected], hook, P(i%4*6,i/4*6), policy)).ToList();
        // One additional fixed pair is a deliberate visible counterexample.
        units.Add(new("fixed passage A", [hook], hook, P(0,12)));
        units.Add(new("fixed passage B", [hook], hook, P(6,12)));
        var before = units.Select(x => x.Current).ToArray();
        var connections = new List<UnitArrangementConnection>();
        for (var row=0;row<2;row++)
        for (var column=0;column<3;column++)
        {
            var left=units[row*4+column]; var right=units[row*4+column+1];
            var connection = new UnitArrangementConnection(Frame, left, ends[1], right, ends[0]);
            connection.Observe();
            if (!connection.IsAligned) connection.Negotiate(right,flip);
            connections.Add(connection);
        }
        var held = new UnitArrangementConnection(Frame,units[8],ends[1],units[9],ends[0]);
        held.Observe(); held.Negotiate(units[9],flip); connections.Add(held);
        var seamView=new QualitativeGraphJoin(new QualitativeStrokeGraph(Frame,S(units.SelectMany(x=>E(x.Current.Graph.Sites)).ToArray()),S(units.SelectMany(x=>E(x.Current.Graph.Strokes)).ToArray())),
            S(units.SelectMany(x=>E(x.Current.Bindings)).ToArray()),S(connections.SelectMany(connection=>E(connection.AcceptedJoins).Cast<Relation>().Where(connection.IsCurrentAgreement)).ToArray()));
        return new("A02","Neighbours negotiate a mirror", "Each connection sees two endpoint publications, not either recipe. A small flip signal changes a local program through its finite law. Six seams align; the fixed pair keeps its failed request visible.",
            units.ToArray(), before, [], [], connections.ToArray(), new Relation(seamView.Surface,S(units.Select(x=>(IElement)x.Surface).Concat(connections.Select(x=>(IElement)x.Timeline)).ToArray()))) {SeamView=seamView};
    }

    private Case RotatedLandmarks()
    {
        var outer = Polygon("coarse diamond construction",[P(6,0),P(0,6),P(-6,0),P(0,-6)],out var roles);
        var supplier = new UnitArrangementNode("coarse four-landmark scaffold",[outer],outer,S());
        var template = new UnitLandmarkTemplate(roles.Select(role=>((IUnitArrangementEndpoint)supplier,role)),supplier.Surface);
        var seed=OpenPath("one stepped arm",[P(0,0),P(2,0),P(2,2),P(1,2),P(1,1)],out _);
        var programs=new[]{seed,seed.Transform("turned arm",Turn),seed.Transform("twice turned arm",Turn).Transform("half turned arm",Turn),seed.Transform("three-quarter arm A",Turn).Transform("three-quarter arm B",Turn).Transform("three-quarter arm C",Turn)};
        var dependencies=new List<IElement>();
        var units=template.Landmarks.Select((landmark,i)=>Place($"turn orbit {i+1}",programs[i],template,landmark,dependencies)).ToArray();
        // The four completed units now become a second, coarser port surface.
        var secondary=new UnitLandmarkTemplate(units.Select(unit=>((IUnitArrangementEndpoint)unit,FiniteLaw.Entries(unit.Current.Program.Ports).Cast<Relation>().First().Left)),template.Surface);
        var cap=Polygon("small diamond marker",[P(1,0),P(0,1),P(-1,0),P(0,-1)],out _);
        var caps=secondary.Landmarks.Select((landmark,i)=>Place($"landmark cap {i+1}",cap,secondary,landmark,dependencies)).ToArray();
        return Result("A03","A scaffold becomes another template", "A completed diamond supplies four landmarks. Quarter-turn word correspondences orient four stepped arms. Their published starts become a new template for a third layer of diamond caps.", [..units,..caps],[supplier],[template,secondary],[],dependencies);
    }

    private Case RefinedDiamonds()
    {
        var diamond=Polygon("coarse four-side contour",[P(4,0),P(0,4),P(-4,0),P(0,-4)],out var roles);
        var path=(Relation)E(diamond.Paths)[0]; var old=E((Relation)path.Right).Cast<Relation>().ToArray();
        // Each old side receives an ordered four-edge replacement. Corner
        // positions and net displacement survive; straightness is not claimed.
        var quarter=new[]{P(4,0),P(2,1),P(3,3),P(1,2),P(0,4)};
        var replacement=new List<IElement>();
        for(var side=0;side<4;side++)
        {
            var vertices=quarter.Select(word=>ApplyTurns(word,side)).ToArray();
            var chords=Enumerable.Range(0,4).Select(i=>Difference(vertices[i+1],vertices[i])).ToArray();
            replacement.Add(new Relation(old[side],S(chords)));
        }
        var star=diamond.Refine("eight-tip inserted contour",S(replacement.ToArray()));
        var signal=new Placeholder();var policy=Policy([diamond,star],signal,star);
        var units=Enumerable.Range(0,6).Select(i=>new UnitArrangementNode($"refinable contour {i+1}",[diamond,star],diamond,P(i%3*12,i/3*12),policy)).ToArray();
        var before=units.Select(x=>x.Current).ToArray();var changes=new List<IElement>();
        foreach(var node in units)
        {
            var proposal=node.Propose(signal,new Relation(diamond.Surface,star.Surface));
            changes.Add(node.Commit(proposal));
        }
        var template=new UnitLandmarkTemplate(units.SelectMany(unit=>roles.Select(role=>((IUnitArrangementEndpoint)unit,role))),S(changes.ToArray()));
        return new("A04","Diamond sides elaborate into eight tips", "A higher-layer request replaces each side by four local edges. All four old corner landmarks and each side’s net displacement remain justified. The old straight boundary remains historical; the new boundary adds eight tips and eight recesses.",units,before,[],[template],[],new Relation(diamond.Surface,new Relation(star.Surface,S(changes.ToArray()))));
    }

    private QuiltCase BuildQuilt()
    {
        const int columns=2,rows=2;
        var nodes=new List<UnitArrangementNode>(); var raw=new List<Relation>();var bindings=new List<IElement>();var sites=new List<IElement>();
        var representative=new Dictionary<(int,int),IElement>();var joins=new List<IElement>();
        var edges=new Dictionary<((int,int),(int,int)),Relation>();var seamKeys=new List<((int,int),(int,int))>();
        var boundaryPlans=new List<((int,int),(int,int))[]>();
        // The construction declares incidence first. Every edge then acts as
        // an independent two-port unit and supplies its actual accepted stroke.
        for(var y=0;y<=rows;y++)for(var x=0;x<columns;x++)Edge((x,y),(x+1,y));
        for(var x=0;x<=columns;x++)for(var y=0;y<rows;y++)Edge((x,y),(x,y+1));
        for(var y=0;y<rows;y++)for(var x=0;x<columns;x++)
        {
            var a=(x,y);var b=(x+1,y);var c=(x+1,y+1);var d=(x,y+1);
            Edge(a,c);seamKeys.Add((a,c));
            boundaryPlans.Add([(a,b),(b,c),(c,a)]);boundaryPlans.Add([(a,c),(c,d),(d,a)]);
        }
        var source=new QualitativeStrokeGraph(Frame,S(sites.ToArray()),S(raw.ToArray()));
        var join=new QualitativeGraphJoin(source,S(bindings.ToArray()),S(joins.ToArray()));
        if(!join.TryReadPassed(join.Evaluate()))throw new InvalidOperationException("Quilt landmark joins must be formally justified.");
        var strokeMap=new Dictionary<IElement,Relation>(ReferenceEqualityComparer.Instance);
        foreach(var row in E(join.StrokeMap).Cast<Relation>())strokeMap.Add(row.Left,(Relation)row.Right);
        var siteMap=new Dictionary<IElement,IElement>(ReferenceEqualityComparer.Instance);
        foreach(var row in E(join.SiteMap).Cast<Relation>())siteMap.Add(row.Left,row.Right);
        Relation Boundary(IEnumerable<((int,int) From,(int,int) To)> plan)=>ScaffoldQuestionScaffold.Boundary(plan.Select(pair=>
        {
            var forward=edges.TryGetValue((pair.From,pair.To),out var edge);edge??=edges[(pair.To,pair.From)];
            QualitativeStrokeGraph.TryReadStroke(edge,out _,out var a,out var b,out _);
            return ScaffoldQuestionScaffold.Oriented(strokeMap[edge],siteMap[forward?a:b],siteMap[forward?b:a]);
        }).ToArray());
        var bounded=S(boundaryPlans.Select(plan=>(IElement)Boundary(plan)).ToArray());
        var exteriorPlan=new List<((int,int),(int,int))>();
        for(var y=0;y<rows;y++)exteriorPlan.Add(((0,y),(0,y+1)));
        for(var x=0;x<columns;x++)exteriorPlan.Add(((x,rows),(x+1,rows)));
        for(var y=rows;y>0;y--)exteriorPlan.Add(((columns,y),(columns,y-1)));
        for(var x=columns;x>0;x--)exteriorPlan.Add(((x,0),(x-1,0)));
        var premise=new Relation(S(nodes.Select(x=>(IElement)x.Current.Surface).ToArray()),new Relation(join.Surface,bounded));
        var scaffold=new ScaffoldQuestionScaffold(join.Graph,bounded,S(Boundary(exteriorPlan)),premise);
        var triangles=new ScaffoldQuestion(scaffold,S(new Placeholder(),new Placeholder(),new Placeholder()),new Placeholder());
        var quadrilaterals=new ScaffoldQuestion(scaffold,S(new Placeholder(),new Placeholder(),new Placeholder(),new Placeholder()),new Placeholder());
        var withdrawn=S(seamKeys.Where((_,i)=>i%2==0).Select(key=>(IElement)strokeMap[edges[key]]).ToArray());
        var withdrawal=new ScaffoldQuestionWithdrawal(scaffold,withdrawn,new Relation(triangles.Surface,quadrilaterals.Surface));
        var opened=new ScaffoldQuestion(withdrawal.Result,S(new Placeholder(),new Placeholder(),new Placeholder(),new Placeholder()),withdrawal.Surface);
        return new(nodes.ToArray(),join,scaffold,triangles,quadrilaterals,withdrawal,opened,join.Bindings,
            new Relation(premise,new Relation(new Relation(triangles.Surface,quadrilaterals.Surface),new Relation(withdrawal.Surface,opened.Surface))));

        void Edge((int X,int Y) a,(int X,int Y) b)
        {
            var program=OpenPath("one two-port construction edge",[S(),P((b.X-a.X)*4,(b.Y-a.Y)*4)],out var roles);
            var unit=new UnitArrangementNode("scaffold edge",[program],program,P(a.X*4,a.Y*4));nodes.Add(unit);
            var stroke=E(unit.Current.Graph.Strokes).Cast<Relation>().Single();raw.Add(stroke);edges.Add((a,b),stroke);
            QualitativeStrokeGraph.TryReadStroke(stroke,out _,out var from,out var to,out _);
            Register(a,from);Register(b,to);
            bindings.AddRange(E(unit.Current.Bindings));sites.AddRange(E(unit.Current.Graph.Sites));
            void Register((int,int) key,IElement site)
            {
                if(representative.TryGetValue(key,out var actual))joins.Add(new Relation(new Relation(site,actual),new Relation(unit.Surface,program.Ports)));
                else representative.Add(key,site);
            }
        }
    }

    private CrossingCase BuildCrossing()
    {
        var first=new Relation(P(-3,-3),S(P(6,6)));
        var second=new Relation(P(-3,3),S(P(6,-6)));
        var program=new UnitArrangementProgram(Frame,"two independent crossing threads",S(first,second),S());
        var independent=new UnitArrangementNode("unjoined crossing",[program],program,S());
        var arms=new List<UnitArrangementNode>();var declarations=new List<IElement>();IElement? centre=null;
        foreach(var tip in new[]{P(3,3),P(-3,3),P(-3,-3),P(3,-3)})
        {
            var arm=OpenPath("one joined ray",[S(),tip],out var ends);
            var node=new UnitArrangementNode("joined ray",[arm],arm,P(10,0));arms.Add(node);
            if(!UnitArrangementConnection.TryReadPublication(node,ends[0],out var publication,out var binding))throw new InvalidOperationException("A ray needs its public centre.");
            if(centre is null)centre=binding.Left;
            else declarations.Add(new Relation(new Relation(binding.Left,centre),publication));
        }
        var graph=new QualitativeStrokeGraph(Frame,S(arms.SelectMany(x=>E(x.Current.Graph.Sites)).ToArray()),S(arms.SelectMany(x=>E(x.Current.Graph.Strokes)).ToArray()));
        var join=new QualitativeGraphJoin(graph,S(arms.SelectMany(x=>E(x.Current.Bindings)).ToArray()),S(declarations.ToArray()));
        return new(independent,arms.ToArray(),join,new Relation(independent.Current.Surface,join.Surface));
    }

    private Case Result(string id,string name,string description,UnitArrangementNode[] units,UnitArrangementNode[] construction,
        UnitLandmarkTemplate[] templates,UnitArrangementConnection[] connections,List<IElement> dependencies)=>
        new(id,name,description,units,units.Select(x=>x.Current).ToArray(),construction,templates,connections,
            new Relation(S(construction.Select(x=>(IElement)x.Current.Surface).ToArray()),new Relation(S(templates.Select(x=>(IElement)x.Surface).ToArray()),
                new Relation(S(units.Select(x=>(IElement)x.Current.Surface).ToArray()),S(dependencies.ToArray())))));

    private UnitArrangementNode Place(string name,UnitArrangementProgram program,UnitLandmarkTemplate template,
        UnitLandmarkTemplate.Landmark landmark,List<IElement> dependencies)
    {
        if(!template.TryRead(landmark,out var binding)||binding.Right is not Relation word)throw new InvalidOperationException("A stale landmark cannot place a unit.");
        var unit=new UnitArrangementNode(name,[program],program,word);
        dependencies.Add(new Relation(landmark.Surface,new Relation(template.Validate(),unit.Surface)));return unit;
    }

    private UnitArrangementNode Markers(string name,Relation[] starts,IElement[] roles)
    {
        var paths=starts.Select(start=>new Relation(start,S(W(Frame.Right)))).ToArray();
        var ports=S(paths.Select((path,i)=>(IElement)new Relation(roles[i],new Relation(path,E((Relation)path.Right)[0]))).ToArray());
        var program=new UnitArrangementProgram(Frame,name,S(paths),ports);
        return new(name,[program],program,S());
    }

    private UnitArrangementProgram Polygon(string name,Relation[] vertices,out IElement[] roles)
    {
        var chords=vertices.Select((word,i)=>Difference(vertices[(i+1)%vertices.Length],word)).ToArray();
        var path=new Relation(vertices[0],S(chords));roles=vertices.Select(_=>(IElement)new Placeholder()).ToArray();
        return new(Frame,name,S(path),S(roles.Select((role,i)=>(IElement)new Relation(role,new Relation(path,chords[i]))).ToArray()),S(path),S(differences.ToArray()));
    }

    private UnitArrangementProgram OpenPath(string name,Relation[] vertices,out IElement[] ends)
    {
        var chords=vertices.Skip(1).Select((word,i)=>Difference(word,vertices[i])).ToArray();
        var path=new Relation(vertices[0],S(chords));ends=[new Placeholder(),new Placeholder()];
        return new(Frame,name,S(path),S(new Relation(ends[0],new Relation(path,chords[0])),new Relation(ends[1],new Relation(path,path))));
    }

    private Relation Policy(UnitArrangementProgram[] programs,IElement signal,UnitArrangementProgram target)=>
        FiniteLaw.Create(S(programs.Select(x=>(IElement)x.Surface).ToArray()),S(signal),S(programs.Select(x=>(IElement)x.Surface).ToArray()),
            programs.Select(program=>FiniteLaw.Row(program.Surface,signal,S(target.Surface))).ToArray());
    private Relation ApplyTurns(Relation word,int turns)
    {
        for(var i=0;i<turns;i++){var read=Turn.Apply(word);if(!Turn.TryReadComplete(read,out word))throw new InvalidOperationException("Complete turn required.");differences.Add(read);}return word;
    }
    private QualitativePatternTransform Map(params (IElement From,Relation To)[] rows)=>new(Frame,Frame,S(rows.Select(row=>(IElement)new Relation(QualitativeMotionFrame.Move(row.From,Reach),row.To)).ToArray()));
    public Relation P(int x,int y)=>S([..E(W(x>=0?Frame.Right:Frame.Left,Math.Abs(x))),..E(W(y>=0?Frame.Up:Frame.Down,Math.Abs(y)))]);
    private Relation W(IElement direction,int count=1)=>S(Enumerable.Range(0,count).Select(_=>(IElement)QualitativeMotionFrame.Move(direction,Reach)).ToArray());
    private Relation Difference(Relation target,Relation source)
    {
        var inverse=S(E(source).Cast<Relation>().Reverse().Select(move=>(IElement)QualitativeMotionFrame.Move(E(Frame.Opposites).Cast<Relation>().Single(row=>ReferenceEquals(row.Left,move.Left)).Right,move.Right)).ToArray());
        var read=Frame.Reduce(target,inverse);differences.Add(read);
        if(!Frame.TryReadResult(read,out var word,out _,out var pending)||E(pending).Length!=0)throw new InvalidOperationException("A constructed chord needs a complete shared-reach difference.");return word;
    }
    private static Relation S(params IElement[] values)=>StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope)=>FiniteLaw.Entries(scope);
}
