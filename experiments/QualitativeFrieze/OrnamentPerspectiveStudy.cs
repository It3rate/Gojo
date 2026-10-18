using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Authored observation contexts over executed source incidence.
/// Different covers may overlap at any level; exact source participation, not
/// labels or colour, relates them. Derived topology is consumed by new drawing
/// flow through a separately witnessed word-landmark interpretation.</summary>
public sealed class OrnamentPerspectiveStudy
{
    private readonly Action<string>? progress;
    public sealed record NamedView(string Id,string Name,string Description,OrnamentView View);
    public sealed record Level(string Id,OrnamentDerivedScaffold Read,Relation Bindings,Relation CellWords,Relation Interpretation);
    public sealed record ViewNetwork(NamedView View,OrnamentViewNetwork Read,Relation Bindings,Relation LandmarkReads,OrnamentNetworkConstruction Execution);
    public sealed record Patch(string Id,string Name,string Basis,OrnamentSourcePatch Source,
        NamedView[] Views,OrnamentViewComposition[] Compositions,OrnamentViewOverlap[] Comparisons,
        OrnamentWordLandmarks Landmarks,Level[] Levels,ViewNetwork[] Networks,
        OrnamentNetworkConstruction[] Executions,UnitArrangementNode[] Decorations,Relation Surface);

    public Patch[] Patches { get; }
    public Relation Surface { get; }

    public OrnamentPerspectiveStudy(Action<string>? progress=null)
    {
        this.progress=progress;
        Patches=[Triangles(),Courts()];
        Surface=S(Patches.Select(patch=>(IElement)patch.Surface).ToArray());
    }

    private Patch Triangles()
    {
        progress?.Invoke("Constructing triangular source");
        var source=OrnamentSourcePatch.TriangleGrid(6,5);var records=source.FaceRecords;
        var a=new List<IElement>();var b=new List<IElement>();var c=new List<IElement>();
        for(var y=0;y<source.Rows;y++)for(var x=0;x<source.Columns;x++)
        {
            var first=records.Single(face=>face.CellX==x&&face.CellY==y&&face.Sector==0).Face;
            a.Add(OrnamentView.Group(new Placeholder(),first,records.Single(face=>face.CellX==x&&face.CellY==y&&face.Sector==1).Face));
            if(y>0)b.Add(OrnamentView.Group(new Placeholder(),first,records.Single(face=>face.CellX==x&&face.CellY==y-1&&face.Sector==1).Face));
            if(x>0)c.Add(OrnamentView.Group(new Placeholder(),first,records.Single(face=>face.CellX==x-1&&face.CellY==y&&face.Sector==1).Face));
        }
        var views=new[]{
            View("T-A","Lozenges across the diagonal","Pairs use each coarse cell's actual diagonal seam.",a),
            View("T-B","Lozenges across one edge family","The same triangles pair across a different seam family. Border triangles remain complementary.",b),
            View("T-C","Lozenges across the other family","A third pairing shifts the read again. A source triangle can participate in all three interpretations.",c)};
        return Finish("V01-T","Three lozenge readings of one field","triangular",source,views,[]);
        NamedView View(string id,string name,string description,List<IElement> definitions)=>new(id,name,description,new(source.Scaffold,S(definitions.ToArray()),new Placeholder()));
    }

    private Patch Courts()
    {
        progress?.Invoke("Constructing fan source");
        var source=OrnamentSourcePatch.SquareFans(6,4);var records=source.FaceRecords;
        var courts=new List<IElement>();
        for(var y=0;y<source.Rows;y++)for(var x=0;x<source.Columns;x++)
            courts.Add(OrnamentView.Group(new Placeholder(),records.Where(face=>face.CellX==x&&face.CellY==y).Select(face=>face.Face).ToArray()));
        var courtView=new OrnamentView(source.Scaffold,S(courts.ToArray()),new Placeholder());
        var vertexStars=new List<IElement>();
        for(var y=1;y<source.Rows;y++)for(var x=1;x<source.Columns;x++)
            vertexStars.Add(OrnamentView.Group(new Placeholder(),records.Where(face=>face.LogicalVertices.Contains((x*6,y*6))).Select(face=>face.Face).ToArray()));
        var starView=new OrnamentView(source.Scaffold,S(vertexStars.ToArray()),new Placeholder());
        // These parents select actual lower READS. Expanded source membership
        // is computed by the compositional mechanism, with child occurrences
        // and shared membership retained rather than a hidden flattened list.
        var lower=E(courtView.Groups).Cast<Relation>().ToArray();
        var blocks=new List<IElement>();
        for(var y=0;y<source.Rows;y+=2)for(var x=0;x<source.Columns;x+=2)
            blocks.Add(new Relation(new Placeholder(),S(lower[y*6+x],lower[y*6+x+1],lower[(y+1)*6+x],lower[(y+1)*6+x+1])));
        var blockView=new OrnamentViewComposition(courtView,S(blocks.ToArray()),new Placeholder());
        var rows=new OrnamentViewComposition(courtView,S(Enumerable.Range(0,source.Rows).Select(y=>(IElement)new Relation(new Placeholder(),S(lower.Skip(y*6).Take(6).ToArray()))).ToArray()),new Placeholder());
        var whole=new OrnamentViewComposition(blockView.View,S(new Relation(new Placeholder(),blockView.View.Groups)),new Placeholder());
        var views=new[]{
            new NamedView("F-A","Four-triangle courts","Twenty-four courts group four actual triangular chambers each.",courtView),
            new NamedView("F-B","Overlapping vertex diamonds","Fifteen eight-triangle stars overlap even inside this single view. Shared triangles do not merge their group identities.",starView),
            new NamedView("F-C","Courts become super-courts","Six groups are made from actual child court reads, not copied geometry.",blockView.View),
            new NamedView("F-D","The same courts become rows","Four row parents cross the super-court organization and retain their common children.",rows.View),
            new NamedView("F-E","A third grouping level","One field groups the six actual super-court reads. Source, courts and higher groups remain simultaneously available.",whole.View)};
        return Finish("V01-F","Courts, diamonds and overlapping parents","axes",source,views,[blockView,rows,whole]);
    }

    private Patch Finish(string id,string name,string basis,OrnamentSourcePatch source,NamedView[] views,OrnamentViewComposition[] compositions)
    {
        progress?.Invoke(id+": comparing views");
        var comparisons=new List<OrnamentViewOverlap>();
        for(var i=0;i<views.Length;i++)for(var j=i;j<views.Length;j++)comparisons.Add(new(views[i].View,views[j].View,new Placeholder()));
        progress?.Invoke(id+": deriving word landmarks and incidence levels");
        var landmarks=new OrnamentWordLandmarks(source.Scaffold,source.Bindings);
        var levels=new List<Level>();
        var read=new OrnamentDerivedScaffold(source.Scaffold,new Placeholder());
        var inputs=source.Bindings;Relation? priorCellWords=null;
        for(var rung=0;rung<3;rung++)
        {
            var bindings=new List<IElement>();var witnesses=new List<IElement>();
            foreach(var row in E(read.Representatives).Cast<Relation>())
            {
                Relation word;
                if(rung==0)
                {
                    if(!landmarks.TryRead(row.Left,out var binding,out var proof))throw new InvalidOperationException("A source cell needs a complete selected word-landmark read.");
                    word=(Relation)binding.Right;witnesses.Add(new Relation(row,new Relation(binding,proof)));
                }
                else
                {
                    var inherited=E(priorCellWords!).Cast<Relation>().Single(actual=>ReferenceEquals(actual.Left,row.Left));
                    word=(Relation)inherited.Right;witnesses.Add(new Relation(row,inherited));
                }
                bindings.Add(new Relation(row.Right,word));
            }
            var cellWordProofs=new List<IElement>();
            var cellWords=E(read.CellSources).Cast<Relation>().Select(row=>
            {
                var word=E(inputs).Cast<Relation>().Single(actual=>ReferenceEquals(actual.Left,row.Left));
                var publication=new Relation(row.Right,word.Right);
                cellWordProofs.Add(new Relation(new Relation(row,word),publication));return (IElement)publication;
            }).ToArray();
            var output=S(bindings.ToArray());
            levels.Add(new($"D{rung+1}",read,output,S(cellWords),new Relation(read.Surface,new Relation(output,
                new Relation(S(witnesses.ToArray()),new Relation(S(cellWords),S(cellWordProofs.ToArray())))))));
            inputs=output;priorCellWords=S(cellWords);
            if(rung<2)read=new(read,new Placeholder());
        }
        // Alternative V01 covers cause alternative V02 networks. Boundary
        // contact and shared membership remain separate native edge families.
        var networks=views.Take(2).Select(view=>
        {
            progress?.Invoke(id+": constructing view network "+view.Id);
            var network=new OrnamentViewNetwork(view.View,new Placeholder());
            var bindings=new List<IElement>();var reads=new List<IElement>();
            foreach(var representative in E(network.Representatives).Cast<Relation>())
            {
                if(!view.View.TryReadGroup((Relation)representative.Left,out var group))throw new InvalidOperationException("A view network needs its actual source group.");
                var sourceBindings=S(E(group.Sites).Select(site=>(IElement)E(source.Bindings).Cast<Relation>().Single(binding=>ReferenceEquals(binding.Left,site))).ToArray());
                var slots=S(E(group.Sites).Select(_=>(IElement)new Placeholder()).ToArray());
                var calculation=OrnamentWordLandmarks.Share(source.Frame,sourceBindings,slots);
                if(!OrnamentWordLandmarks.TryReadComplete(source.Frame,calculation,out var word))throw new InvalidOperationException("Selected group landmarks must be representable at this word resolution.");
                bindings.Add(new Relation(representative.Right,word));reads.Add(new Relation(representative,calculation));
            }
            var positions=S(bindings.ToArray());
            var execution=new OrnamentNetworkConstruction(source.Frame,network.Topology,positions,network.Surface);execution.Run();
            return new ViewNetwork(view,network,positions,S(reads.ToArray()),execution);
        }).ToArray();
        var executions=new[]{new OrnamentNetworkConstruction(source.Frame,levels[0].Read,levels[0].Bindings,levels[0].Interpretation),
            new OrnamentNetworkConstruction(source.Frame,levels[1].Read,levels[1].Bindings,levels[1].Interpretation)};
        foreach(var execution in executions){progress?.Invoke(id+": executing derived scaffold");execution.Run();}
        // A sibling ornament layer consumes the derived level's published
        // word bindings. Reusable path programs, not rendered copies, act here.
        var program=MarkerProgram(source.Frame,source.Reach);
        progress?.Invoke(id+": placing sibling ornaments");
        var decorations=E(levels[1].Bindings).Cast<Relation>().Select(binding=>new UnitArrangementNode("derived landmark ornament",[program],program,(Relation)binding.Right)).ToArray();
        var decorationInputs=S(E(levels[1].Bindings).Cast<Relation>().Zip(decorations).Select(pair=>(IElement)new Relation(pair.First,
            new Relation(pair.Second.PlacementExecution.InputPort,new Relation(pair.Second.PlacementExecution.Capture(),pair.Second.Current.Surface)))).ToArray());
        var surface=new Relation(source.Surface,new Relation(S(views.Select(view=>(IElement)view.View.Evaluate()).ToArray()),
            new Relation(S(compositions.Select(item=>(IElement)item.Evaluate()).ToArray()),
                new Relation(S(comparisons.Select(item=>(IElement)item.Evaluate()).ToArray()),
                    new Relation(landmarks.Surface,new Relation(S(levels.Select(level=>(IElement)level.Interpretation).ToArray()),
                        new Relation(S(networks.Select(network=>(IElement)new Relation(network.Read.Surface,new Relation(network.LandmarkReads,network.Execution.Capture()))).ToArray()),
                            new Relation(S(executions.Select(execution=>(IElement)execution.Capture()).ToArray()),
                                decorationInputs))))))));
        return new(id,name,basis,source,views,compositions,comparisons.ToArray(),landmarks,levels.ToArray(),networks,executions,decorations,surface);
    }

    private static UnitArrangementProgram MarkerProgram(QualitativeMotionFrame frame,IElement reach)
    {
        Relation W(IElement direction)=>S(QualitativeMotionFrame.Move(direction,reach));
        var first=new Relation(W(frame.Right),S(S([..E(W(frame.Left)),..E(W(frame.Up))]),S([..E(W(frame.Left)),..E(W(frame.Down))]),
            S([..E(W(frame.Right)),..E(W(frame.Down))]),S([..E(W(frame.Right)),..E(W(frame.Up))])));
        return new(frame,"four opposed marker chords",S(first),S(),S(first));
    }
    private static Relation S(params IElement[] values)=>StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope)=>FiniteLaw.Entries(scope);
}
