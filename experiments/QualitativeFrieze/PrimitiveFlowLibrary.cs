using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Authored experiments over one call/return mechanism. Labels and
/// display bases do not select execution. Actual actions, shared reaches,
/// transformations, call rows and completion ports remain in Definition.</summary>
public sealed class PrimitiveFlowRecipe
{
    // Definition = root.Surface -> (derivation-evidence -> closure-ports)
    // goal = root.CompletionPort -> Closed(requested completion slots)
    // capture = (Definition -> goal) -> (root.Capture -> goal correspondences)
    public string Id { get; }
    public string Name { get; }
    public string Read { get; }
    public string Atlas { get; }
    public string DisplayBasis { get; }
    public PrimitiveFlowNode Root { get; }
    public QualitativeDrawingReceiver Drawing => Root.Drawing;
    public IReadOnlyList<PrimitiveFlowNode> ClosureNodes { get; }
    public IReadOnlyList<PrimitiveFlowLeaf> ClosureLeaves => ClosureNodes.OfType<PrimitiveFlowLeaf>().ToArray();
    public Relation Definition { get; }
    public Relation Goal { get; }
    public Relation LastRun { get; private set; }
    public bool GoalMet => E((Relation)Goal.Right).Length == E(Root.CompletionEvents).Length;

    public PrimitiveFlowRecipe(string id, string name, string read, string atlas,
        PrimitiveFlowNode root, int cycles = 1, Relation? evidence = null,
        string displayBasis = "axes", params PrimitiveFlowNode[] closureNodes)
    {
        Id = id; Name = name; Read = read; Atlas = atlas; Root = root; DisplayBasis = displayBasis;
        ClosureNodes = Array.AsReadOnly(closureNodes);
        Definition = new(root.Surface, new Relation(evidence ?? S(), S(closureNodes.Select(node => (IElement)node.CompletionPort).ToArray())));
        Goal = new(root.CompletionPort, S(Enumerable.Range(0, cycles).Select(_ => (IElement)new Placeholder()).ToArray()));
        LastRun = new(new Relation(Definition, Goal), new Relation(root.Capture(), S()));
    }

    /// <summary>External occurrence supplier. The budget is a harness bound;
    /// only authenticated completion events discharge the requested slots.</summary>
    public Relation Run(int budget = 512)
    {
        for (var drive = 0; drive < budget && !GoalMet; drive++)
        {
            var request = Root.Propose(new Placeholder(), S(new Placeholder()));
            if (!Root.TryReadRequest(request, out var before, out var offer)) break;
            var ack = Root.Acknowledge(request, Drawing.Step(before, offer));
            if (!Root.TryReadAccepted(ack, out _)) break;
        }
        var matches = E((Relation)Goal.Right).Zip(E(Root.CompletionEvents)).Select(pair =>
            (IElement)new Relation(pair.First, pair.Second)).ToArray();
        return LastRun = new(new Relation(Definition, Goal), new Relation(Root.Capture(), S(matches)));
    }

    /// <summary>Explicit closure observation from selected nodes' actual
    /// completed drawing receipts. Equal positions alone never fuse raw sites.</summary>
    public QualitativeGraphJoin ClosureView()
    {
        var declarations = new List<IElement>();
        foreach (var node in ClosureNodes)
        foreach (var item in E(node.CompletionEvents).OfType<Relation>())
        {
            var strokes = new List<Relation>();
            foreach (var receipt in Effects(node,item))
            {
                if (!Drawing.TryReadStep(receipt, out var before, out _, out var after, out _, out _, out _) ||
                    !Drawing.TryReadState(before, out _, out _, out _, out _, out _, out var prior, out _) ||
                    !Drawing.TryReadState(after, out _, out _, out _, out _, out _, out var later, out _)) continue;
                strokes.AddRange(E(later).Skip(E(prior).Length).OfType<Relation>());
            }
            if (strokes.Count == 0 ||
                !QualitativeStrokeGraph.TryReadStroke(strokes[0], out _, out var start, out _, out _) ||
                !QualitativeStrokeGraph.TryReadStroke(strokes[^1], out _, out _, out var end, out _)) continue;
            declarations.Add(new Relation(new Relation(end, start), item));
        }
        var sites = S(E(Drawing.SiteBindings).Cast<Relation>().Select(row => row.Left).ToArray());
        return new(new QualitativeStrokeGraph(Drawing.Frame, sites, Drawing.Strokes), Drawing.SiteBindings, S(declarations.ToArray()));
    }

    private static IEnumerable<Relation> Effects(PrimitiveFlowNode node,Relation completion)
    {
        if(completion.Right is not Relation {Left:Relation cycle})yield break;
        if(node is PrimitiveFlowLeaf)
        {foreach(var receipt in E(cycle).OfType<Relation>())yield return receipt;yield break;}
        if(node is not PrimitiveFlowGroup group)yield break;
        var rows=E(group.CallRows);
        foreach(var returned in E(cycle).OfType<Relation>())
        {
            if(returned.Left is not Relation {Left:Relation row,Right:Relation acknowledgment})continue;
            var index=Array.FindIndex(rows,actual=>ReferenceEquals(actual,row));
            if(index<0||!group.Children[index].TryReadAccepted(acknowledgment,out var childCompletions))continue;
            foreach(var childCompletion in E(childCompletions).OfType<Relation>())
            foreach(var effect in Effects(group.Children[index],childCompletion))yield return effect;
        }
    }

    public IEnumerable<PrimitiveFlowNode> Nodes()
    {
        var seen = new HashSet<PrimitiveFlowNode>(); var todo = new Stack<PrimitiveFlowNode>(); todo.Push(Root);
        while (todo.TryPop(out var node))
        {
            if (!seen.Add(node)) continue;
            yield return node;
            if (node is PrimitiveFlowGroup group) foreach (var child in group.Children.Reverse()) todo.Push(child);
        }
    }

    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var values, out _, out _) ? values : [];
}

public static class PrimitiveFlowLibrary
{
    public static PrimitiveFlowRecipe[] Create()
    {
        var frame = new QualitativeMotionFrame(); var h = new Placeholder(); var v = new Placeholder();
        var cases = new List<PrimitiveFlowRecipe>();
        Builder B() => new(frame, h, v);
        {
            var b=B(); var step=b.Step();
            Add("P01", "Single step", "Two accepted children make one step; no parent completion after only the run.", "S01, S03", step);
        }
        {
            var b=B(); var step=b.Step();
            Add("P02", "Staircase from one step", "Three call occurrences reuse one operational step. Every call waits for a whole step.", "S03", b.Group("stair flight",step,step,step));
        }
        {
            var b=B(); var step=b.Step(); var flight=b.Group("stair flight",step,step,step);
            var flip=b.Actions("reverse transverse face",b.Drawing.DirectionAction(b.Drawing.VerticalMirror));
            Add("P03", "Alternating stair flights", "A completed flight releases the higher layer to reverse the next flight's transverse face.", "G03", b.Group("flight and feedback",flight,flip),2);
        }
        {
            var b=B();
            Add("P04", "Square wave by ordered turns", "An explicit turn cycle creates axial corners; phase and interrupted completion remain inspectable.", "S03",b.Words("turn cycle",b.U,b.R,b.D,b.R),4);
        }
        {
            var b=B(); var step=b.Step(); var flip=b.Actions("reverse transverse face",b.Drawing.DirectionAction(b.Drawing.VerticalMirror));
            var half=b.Group("one step then reverse",step,flip);
            Add("P05", "Square wave from a reused step", "The same run-and-rise step alternates its transverse interpretation after each completion.", "S03",b.Group("opposed step pair",half,half),4);
        }
        {
            var b=B();
            Add("P06", "Zigzag by diagonal turns", "Each accepted chord carries both directions; there is no intermediate elbow site.", "S03",b.Words("diagonal turn cycle",b.Join(b.R,b.U),b.Join(b.R,b.D)),5);
        }
        {
            var b=B();var leaf=b.Words("four ordered sides",b.Repeat(b.R,3),b.Repeat(b.U,2),b.Repeat(b.L,3),b.Repeat(b.D,2));
            Add("P07","Rectangle by four sides","Shared opposite reaches cancel; a separate witnessed join closes the derived contour.","S09",leaf,closed:[leaf]);
        }
        {
            var b=B();var longEdge=b.Words("long edge",b.Repeat(b.R,3));var shortEdge=b.Words("short edge",b.Repeat(b.R,2));
            var turn=b.Actions("quarter-turn correspondence",b.Drawing.DirectionAction(b.Turn));
            var cornerPair=b.Group("two unequal sides",longEdge,turn,shortEdge,turn);
            var rectangle=b.Group("rectangle traversal",cornerPair,cornerPair);
            Add("P08","Rectangle by reused corners","The same two-side component runs twice; the turn action changes future output, not existing strokes.","S09",rectangle,closed:[rectangle]);
        }
        {
            var b=B();var edge=b.Words("one edge",b.Repeat(b.R,2));var turn=b.Actions("quarter-turn correspondence",b.Drawing.DirectionAction(b.Turn));
            var corner=b.Group("edge and turn",edge,turn);
            var square=b.Group("four corners",corner,corner,corner,corner);
            Add("P09","Square by one edge and turn","Four calls reuse one edge. Formal quarter-turn order is native; Euclidean squarehood belongs to the selected equal-axis view.","S07, S09",square,closed:[square]);
        }
        {
            var b=B();var leaf=b.Words("four diagonal sides",b.Join(b.R,b.U),b.Join(b.R,b.D),b.Join(b.L,b.D),b.Join(b.L,b.U));
            Add("P10","Diamond by opposed chords","Four diagonal chords return to the start displacement; a declared join preserves the raw fresh endpoint.","S04",leaf,closed:[leaf]);
        }
        {
            var b=B();var square=S(b.R,b.D,b.L,b.U);
            var transform=new QualitativePatternTransform(frame,frame,S(
                new Relation(E(b.R)[0],b.Join(b.R,b.U)),new Relation(E(b.U)[0],b.Join(b.L,b.U)),
                new Relation(E(b.L)[0],b.Join(b.L,b.D)),new Relation(E(b.D)[0],b.Join(b.R,b.D))));
            var reads=E(square).Cast<Relation>().Select(transform.Apply).ToArray();
            var words=reads.Select(read=>transform.TryReadComplete(read,out var word)?word:StructuralConstraint.Open(read)).ToArray();
            var leaf=b.Words("transported square sides",words);
            Add("P11","Diamond from a square correspondence","One square-side program is transported through an explicit word map; the source and every derivation survive.","S02, S04",leaf,evidence:new Relation(square,new Relation(transform.Surface,S(reads))),closed:[leaf]);
        }
        {
            var b=B();var words=Meander(b);
            Add("P12","Meander by a turn cycle","Returns travel on distinct lanes. Shared reaches specify corridors without assigning their measured size.","G01",b.Words("key turn cycle",words),3);
        }
        {
            var b=B();var words=Meander(b);
            var rise=b.Words("outer rise",words[0]);var head=b.Words("inward head",words.Skip(1).Take(6).ToArray());
            var returnPath=b.Words("return passage",words.Skip(7).ToArray());
            Add("P13","Meander by nested parts","Outer rise, inner fold and return passage communicate only by accepted completion; the output matches the flat turn cycle.","G01",b.Group("key assembly",rise,head,returnPath),3);
        }
        {
            var b=new Builder(frame,h,h);var bar=b.Words("open-spaced bar",b.Repeat(b.R,3),b.U,b.Repeat(b.L,3),b.D);
            var swap=S(new Relation(frame.Right,frame.Up),new Relation(frame.Up,frame.Right),new Relation(frame.Left,frame.Down),new Relation(frame.Down,frame.Left));
            var placeB=b.Actions("place opposed bar",b.Drawing.Jump(b.Repeat(b.R,4)),b.Drawing.DirectionAction(swap));
            var restore=b.Actions("restore pair base",b.Drawing.DirectionAction(swap),b.Drawing.Jump(b.Repeat(b.L,4)));
            var pair=b.Group("herringbone pair",bar,placeB,bar,restore);
            var across=b.Actions("neighbour passage",b.Drawing.Jump(b.Join(b.Repeat(b.L,2),b.Repeat(b.U,2))));
            var row=b.Group("three neighbouring pairs",pair,across,pair,across,pair);
            var nextRow=b.Actions("next row passage",b.Drawing.Jump(b.Repeat(b.R,8)));
            Add("P14","Herringbone from repeated bar pairs","One rectangle supplies both bar orientations. Paired calls form rows; rows form a field. Open spacing avoids coincident boundary overdraw.","G10",b.Group("two-row field",row,nextRow,row),basis:"diagonal",closed:[bar]);
        }
        {
            var b=B();var six=new QualitativeSixfoldWords(frame,h);var orbit=E(six.Orbit).Cast<Relation>().ToArray();
            var vertices=new List<Relation>();
            for(var i=0;i<6;i++){vertices.Add(b.Repeat(orbit[i],3));vertices.Add(b.Join(orbit[i],orbit[(i+1)%6]));}
            var edges=vertices.Select((word,i)=>b.Difference(vertices[(i+1)%vertices.Count],word)).ToArray();
            var outline=b.Words("twelve outline edges",edges);
            var start=b.Actions("admit outer landmark",b.Drawing.Jump(vertices[0]));
            Add("P15","Star as one outline","An order-six word action supplies six related tips. Twelve contour edges are generated by formal differences, not coordinates.","S05, S07, G11",b.Group("placed star outline",start,outline),evidence:new Relation(six.Surface,new Relation(S(vertices.ToArray()),S(b.DifferenceReads.ToArray()))),basis:"triangular",closed:[outline]);
        }
        {
            var b=B();var six=new QualitativeSixfoldWords(frame,h);var orbit=E(six.Orbit).Cast<Relation>().ToArray();
            var a=new[]{b.Repeat(orbit[0],3),b.Repeat(orbit[2],3),b.Repeat(orbit[4],3)};
            var d=new[]{b.Repeat(orbit[1],3),b.Repeat(orbit[3],3),b.Repeat(orbit[5],3)};
            var first=b.Words("first triangular loop",a.Select((word,i)=>b.Difference(a[(i+1)%3],word)).ToArray());
            var second=b.Words("second triangular loop",d.Select((word,i)=>b.Difference(d[(i+1)%3],word)).ToArray());
            var placeA=b.Actions("first loop placement",b.Drawing.Jump(a[0]));
            var placeB=b.Actions("second loop placement",b.Drawing.Jump(b.Difference(d[0],a[0])));
            Add("P16","Star as two independent loops","Two triangles retain their identities. Projected crossings are not junctions, and a filled union is a different read from this line graph.","S05, S08",b.Group("two loop assembly",placeA,first,placeB,second),evidence:new Relation(six.Surface,S(b.DifferenceReads.ToArray())),basis:"triangular",closed:[first,second]);
        }
        return cases.ToArray();

        void Add(string id,string name,string read,string atlas,PrimitiveFlowNode root,int cycles=1,
            Relation? evidence=null,string basis="axes",PrimitiveFlowNode[]? closed=null) =>
            cases.Add(new(id,name,read,atlas,root,cycles,evidence,basis,closed??[]));
    }

    private static Relation[] Meander(Builder b) => [b.Repeat(b.U,4),b.Repeat(b.R,4),b.Repeat(b.D,3),b.Repeat(b.L,2),
        b.U,b.R,b.U,b.Repeat(b.L,2),b.Repeat(b.D,3),b.Repeat(b.R,4)];

    private sealed class Builder
    {
        public QualitativeDrawingReceiver Drawing { get; }
        public QualitativeMotionFrame Frame => Drawing.Frame;
        public Relation R { get; } public Relation L { get; } public Relation U { get; } public Relation D { get; }
        public Relation Turn { get; }
        public List<IElement> DifferenceReads { get; }=[];
        public Builder(QualitativeMotionFrame frame,IElement h,IElement v)
        {
            Drawing=new(frame);R=S(QualitativeMotionFrame.Move(frame.Right,h));L=S(QualitativeMotionFrame.Move(frame.Left,h));
            U=S(QualitativeMotionFrame.Move(frame.Up,v));D=S(QualitativeMotionFrame.Move(frame.Down,v));
            Turn=S(new Relation(frame.Right,frame.Up),new Relation(frame.Up,frame.Left),new Relation(frame.Left,frame.Down),new Relation(frame.Down,frame.Right));
        }
        public PrimitiveFlowLeaf Actions(string name,params Relation[] actions)=>new(name,Drawing,S(actions));
        public PrimitiveFlowLeaf Words(string name,params Relation[] words)=>Actions(name,words.Select(Drawing.Draw).ToArray());
        public PrimitiveFlowGroup Group(string name,params PrimitiveFlowNode[] children)=>new(name,children);
        public PrimitiveFlowGroup Step()=>Group("one step",Words("run",R),Words("rise",U));
        public Relation Repeat(Relation word,int copies)=>S(Enumerable.Range(0,copies).SelectMany(_=>E(word)).ToArray());
        public Relation Join(params Relation[] words)=>S(words.SelectMany(E).ToArray());
        public Relation Difference(Relation target,Relation source)
        {
            var inverse=S(E(source).Cast<Relation>().Reverse().Select(move=>(IElement)QualitativeMotionFrame.Move(
                E(Frame.Opposites).Cast<Relation>().Single(row=>ReferenceEquals(row.Left,move.Left)).Right,move.Right)).ToArray());
            var read=Frame.Reduce(target,inverse);
            DifferenceReads.Add(read);
            return Frame.TryReadResult(read,out var result,out _,out var pending)&&E(pending).Length==0?result:StructuralConstraint.Open(read);
        }
    }
    private static Relation S(params IElement[] values)=>StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope)=>StructuralConstraint.TryReadScope(scope,out var values,out _,out _)?values:[];
}
