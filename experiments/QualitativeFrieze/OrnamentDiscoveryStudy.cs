using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Bounded discovery questions followed by explicit observational
/// emphasis. The whole constructed field remains available in every reading.
/// Native membership, correspondence and context define each candidate;
/// display names and ornamental styling belong to the gallery.</summary>
public sealed class OrnamentDiscoveryStudy
{
    public sealed record CellLevel(OrnamentPerspectiveStudy.Patch Patch,int Index,OrnamentCellDiscovery Discovery);
    public sealed record Reading(string Id,string Name,string Description,string Style,
        OrnamentPerspectiveStudy.Patch Patch,int Level,Relation Candidate,Relation Occurrences,
        Relation Anchors,Relation Whole,Relation Selected,Relation Complement,Relation Correspondence,
        Relation Surface,OrnamentOccurrenceRhythm? Rhythm=null,OrnamentStrandDiscovery? Strands=null,OrnamentCellDiscovery? Cells=null);
    public OrnamentPerspectiveStudy Source { get; }
    public CellLevel[] CellLevels { get; }
    public Reading[] Readings { get; }
    public Relation Surface { get; }

    public OrnamentDiscoveryStudy(Action<string>? progress=null)
    {
        Source=new(progress);var levels=new List<CellLevel>();var readings=new List<Reading>();
        foreach(var patch in Source.Patches)
        {
            progress?.Invoke(patch.Id+": inspecting exposed cell cycles without generating views");
            for(var i=0;i<patch.Levels.Length;i++)
                levels.Add(new(patch,i,new(patch.Levels[i].Read.Surface,patch.Levels[i].Read.Topology,new Placeholder())));
        }
        CellLevels=levels.ToArray();
        var fan=Source.Patches.Single(patch=>patch.Id=="V01-F");
        var triangle=Source.Patches.Single(patch=>patch.Id=="V01-T");
        History(fan);
        Cells("D02","Alternating diamond gardens","Repeated four-edge cell boundaries are found without the court labels. A declared translation question then supports two alternating accent phases.","flowers",fan,0,4,true);
        Cells("D03","Eight-sided chambers","The same field has a second recurring cell family. Its eight-edge boundaries become the dominant enclosure.","chambers",fan,0,8,false);
        Cells("D04","Triangular star field","A finer read finds recurring three-edge cells. Each receives a three-part ornamental accent while the complete lattice remains.","triangles",fan,1,3,false);
        Cells("D05","Six-sided rosettes","The triangular source supplies a different repeated six-edge family at its first derived level.","rosettes",triangle,0,6,false);
        Strand("D06","Growing vine strands","A local falling / rising / forward heading cycle traces connected diamond halves and their horizontal passages. Leaves follow the accepted routes.",fan,
            S(S(fan.Source.Frame.Right,fan.Source.Frame.Down),S(fan.Source.Frame.Right,fan.Source.Frame.Up),S(fan.Source.Frame.Right)));
        Strand("D07","The other vine reading","The reversed transverse phase finds the complementary rising / falling / forward strands over the same field.",fan,
            S(S(fan.Source.Frame.Right,fan.Source.Frame.Up),S(fan.Source.Frame.Right,fan.Source.Frame.Down),S(fan.Source.Frame.Right)));
        Readings=readings.ToArray();
        Surface=new(Source.Surface,new Relation(S(CellLevels.Select(level=>(IElement)level.Discovery.Surface).ToArray()),S(Readings.Select(reading=>(IElement)reading.Surface).ToArray())));

        void History(OrnamentPerspectiveStudy.Patch patch)
        {
            var program=patch.Decorations[0].Current.Program;
            var actual=patch.Decorations.Where(node=>ReferenceEquals(node.Current.Program,program)).ToArray();
            var occurrences=S(actual.Select(node=>(IElement)new Relation(node.Current.Surface,new Relation(program.Surface,node.Current.Execution.Capture()))).ToArray());
            var anchors=S(actual.Select(node=>(IElement)new Relation(node.Current.Surface,node.PlacementWord)).ToArray());
            var selected=S(actual.SelectMany(node=>E(node.Current.Join.Graph.Strokes)).ToArray());
            var candidate=new Relation(new Relation(patch.Surface,program.Surface),occurrences);
            Add("D01","Remembered marker blossoms","A historical read recovers the shared generating program and its actual completed executions. Blossoms punctuate those published placements.","history",patch,1,candidate,occurrences,anchors,selected,
                S(actual.Select(node=>(IElement)new Relation(node.Current.Surface,node.Current.Join.StrokeMap)).ToArray()));
        }
        void Cells(string id,string name,string description,string style,OrnamentPerspectiveStudy.Patch patch,int index,int arity,bool alternating)
        {
            var discovery=CellLevels.Single(level=>ReferenceEquals(level.Patch,patch)&&level.Index==index).Discovery;
            var family=E(discovery.Families).Cast<Relation>().Select(row=>{discovery.TryReadFamily(row,out var parts);return parts;}).Single(parts=>parts.BoundaryArity==arity);
            var cells=new List<IElement>();var anchors=new List<IElement>();var edgeSet=new HashSet<IElement>(ReferenceEqualityComparer.Instance);
            foreach(var occurrence in E(family.Occurrences).Cast<Relation>())
            {
                if(!discovery.TryReadOccurrence(family.Read,occurrence,out var face,out var boundary,out _))throw new InvalidOperationException("An emphasis needs a witnessed discovered cell.");
                cells.Add(face);
                var binding=E(patch.Levels[index].CellWords).Cast<Relation>().Single(row=>ReferenceEquals(row.Left,face));anchors.Add(binding);
                foreach(var part in E(boundary).Cast<Relation>())edgeSet.Add(part.Left);
            }
            var projection=Project(patch,index,edgeSet);
            OrnamentOccurrenceRhythm? rhythm=null;
            if(alternating)
            {
                var frame=patch.Source.Frame;
                Relation W(IElement direction)=>S(Enumerable.Range(0,patch.Source.FineScale).Select(_=>(IElement)QualitativeMotionFrame.Move(direction,patch.Source.Reach)).ToArray());
                rhythm=new(frame,family.Read,S(cells.ToArray()),S(anchors.ToArray()),S(W(frame.Right),W(frame.Up)),S(new Placeholder(),new Placeholder()),new Placeholder());
            }
            Add(id,name,description,style,patch,index,family.Read,family.Occurrences,S(anchors.ToArray()),projection.Selected,
                new Relation(discovery.Surface,new Relation(projection.Correspondence,rhythm?.Surface??S())),rhythm,cells:discovery);
        }
        void Strand(string id,string name,string description,OrnamentPerspectiveStudy.Patch patch,Relation pattern)
        {
            var level=patch.Levels[0];
            var read=new OrnamentStrandDiscovery(patch.Source.Frame,level.Read.Surface,level.Read.Topology,level.Bindings,pattern,new Placeholder());
            var edges=new HashSet<IElement>(ReferenceEqualityComparer.Instance);var chosen=new List<IElement>();var qualifications=new List<IElement>();
            var minimum=S([..E(pattern),..E(pattern)]);
            foreach(var route in E(read.Routes).Cast<Relation>())
            {
                read.TryReadRoute(route,out var parts);
                if(parts.Uses.Length<E(minimum).Length)continue;
                chosen.Add(route);qualifications.Add(new Relation(new Relation(minimum,S(E(parts.States).Take(E(minimum).Length).ToArray())),route));
                foreach(var use in parts.Uses)edges.Add(use.Left);
            }
            var projection=Project(patch,0,edges);
            Add(id,name,description,"vine",patch,0,read.Surface,S(chosen.ToArray()),S(),projection.Selected,new Relation(S(qualifications.ToArray()),projection.Correspondence),strands:read);
        }
        void Add(string id,string name,string description,string style,OrnamentPerspectiveStudy.Patch patch,int level,Relation candidate,Relation occurrences,Relation anchors,Relation selected,Relation correspondence,OrnamentOccurrenceRhythm? rhythm=null,OrnamentStrandDiscovery? strands=null,OrnamentCellDiscovery? cells=null)
        {
            var whole=Whole(patch);var chosen=E(selected).ToHashSet(ReferenceEqualityComparer.Instance);
            if(chosen.Count!=E(selected).Length||chosen.Any(item=>!E(whole).Any(actual=>ReferenceEquals(actual,item))))throw new InvalidOperationException("A focus must select actual field participation.");
            var complement=S(E(whole).Where(item=>!chosen.Contains(item)).ToArray());
            var context=new Placeholder();
            var surface=new Relation(new Relation(patch.Surface,new Relation(candidate,context)),
                new Relation(new Relation(whole,selected),new Relation(complement,new Relation(correspondence,new Relation(occurrences,anchors)))));
            readings.Add(new(id,name,description,style,patch,level,candidate,occurrences,anchors,whole,selected,complement,correspondence,surface,rhythm,strands,cells));
        }
    }

    private static (Relation Selected,Relation Correspondence) Project(OrnamentPerspectiveStudy.Patch patch,int level,HashSet<IElement> edges)
    {
        var execution=patch.Executions[level];var join=execution.Join!;var selected=new List<IElement>();var evidence=new List<IElement>();
        foreach(var pair in E(execution.Commands).Cast<Relation>().Zip(execution.Units))
        {
            if(!edges.Contains(pair.First.Left))continue;
            foreach(var raw in E(pair.Second.Current.Graph.Strokes))
            {
                var map=E(join.StrokeMap).Cast<Relation>().Single(row=>ReferenceEquals(row.Left,raw));
                selected.Add(map.Right);evidence.Add(new Relation(pair.First,new Relation(pair.Second.Current.Surface,map)));
            }
        }
        if(evidence.Count!=edges.Count)throw new InvalidOperationException("Every selected edge needs its actual completed construction correspondence.");
        return(S(selected.ToArray()),S(evidence.ToArray()));
    }
    private static Relation Whole(OrnamentPerspectiveStudy.Patch patch)=>S(patch.Executions.SelectMany(execution=>E(execution.Join!.Graph.Strokes))
        .Concat(patch.Decorations.SelectMany(node=>E(node.Current.Join.Graph.Strokes))).ToArray());
    private static Relation S(params IElement[] entries)=>StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope)=>N.E(scope);
}
