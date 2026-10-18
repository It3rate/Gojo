using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Explicit counterfactual edits with preserved earlier candidates,
/// local reapplication evidence and fixed training/held-out memberships.</summary>
public sealed class OrnamentPerturbationStudy
{
    public sealed record NamedQuestion(string Name,Relation Definition);
    public sealed record State(string Id,string Name,string Description,Relation Topology,Relation Bindings,
        IElement Change,OrnamentStructurePerturbation? Patch,OrnamentCandidateRevalidation[] Cells,
        OrnamentCandidateRevalidation Forward,OrnamentCandidateRevalidation Reverse,OrnamentCandidateRevalidation Rhythm,
        Relation LandmarkReads,Relation LandmarkBindings,OrnamentQuestionComparison HeadingComparison,
        OrnamentQuestionComparison TranslationComparison,OrnamentStrandDiscovery Fresh,Relation Surface);
    public sealed record Series(string Id,string Name,string Description,State[] States);
    public OrnamentDiscoveryStudy Original { get; }
    public OrnamentPerspectiveStudy.Patch Field { get; }
    public OrnamentDiscoveryStudy.Reading Diamonds { get; }
    public OrnamentDiscoveryStudy.Reading Forward { get; }
    public OrnamentDiscoveryStudy.Reading Reverse { get; }
    public IElement TargetRole { get; }
    public Relation TargetBinding { get; }
    public Relation TargetEdge { get; }
    public Relation TargetSelection { get; }
    public Relation PartitionEvidence { get; }
    public NamedQuestion[] HeadingQuestions { get; }
    public NamedQuestion[] TranslationQuestions { get; }
    public Relation TrainingEdges { get; }
    public Relation HeldOutEdges { get; }
    public Relation TrainingOccurrences { get; }
    public Relation HeldOutOccurrences { get; }
    public Series[] Experiments { get; }
    public Relation Surface { get; }

    public OrnamentPerturbationStudy(Action<string>? progress=null)
    {
        Original=new(progress);Field=Original.Source.Patches.Single(patch=>patch.Id=="V01-F");
        Diamonds=Original.Readings.Single(read=>read.Id=="D02");Forward=Original.Readings.Single(read=>read.Id=="D06");Reverse=Original.Readings.Single(read=>read.Id=="D07");
        var frame=Field.Source.Frame;var basis=Field.Levels[0];var edges=E((Relation)((Relation)basis.Read.Topology.Right).Left).Cast<Relation>().ToArray();
        var forwardRoutes=E(Forward.Occurrences).Cast<Relation>().Select(route=>{Forward.Strands!.TryReadRoute(route,out var parts);return parts;}).ToArray();
        var reverseEdges=E(Reverse.Occurrences).Cast<Relation>().SelectMany(route=>{Reverse.Strands!.TryReadRoute(route,out var parts);return parts.Uses.Select(use=>use.Left);}).ToHashSet(ReferenceEqualityComparer.Instance);
        var probe=forwardRoutes[forwardRoutes.Length/2];
        var pairs=probe.Uses.Zip(probe.Uses.Skip(1)).Where(pair=>Heading(pair.First,frame.Right,frame.Down)&&Heading(pair.Second,frame.Right,frame.Up)).ToArray();
        var chosen=pairs[pairs.Length/2];TargetRole=((Relation)chosen.First.Right).Right;
        TargetBinding=E(basis.Bindings).Cast<Relation>().Single(row=>ReferenceEquals(row.Left,TargetRole));
        TargetEdge=edges.Single(edge=>ReferenceEquals(edge.Left,chosen.First.Left)&&!reverseEdges.Contains(edge.Left));
        TargetSelection=new(new Relation(Forward.Candidate,probe.Read),new Relation(new Relation(chosen.First,chosen.Second),new Relation(TargetBinding,TargetEdge)));

        // A declared source landmark partitions evidence before any candidate
        // comparison. Neither successful routes nor hypothesis labels choose
        // the split. Edges crossing the cut remain carried participation.
        var cut=W(frame.Up,Field.Source.FineScale*2);var partitionProofs=new List<IElement>();
        var sides=E(basis.Bindings).Cast<Relation>().ToDictionary<Relation,IElement,int>(row=>row.Left,Side,ReferenceEqualityComparer.Instance);
        TrainingEdges=S(edges.Where(edge=>sides[((Relation)edge.Right).Left]<0&&sides[((Relation)edge.Right).Right]<0).Cast<IElement>().ToArray());
        HeldOutEdges=S(edges.Where(edge=>sides[((Relation)edge.Right).Left]>0&&sides[((Relation)edge.Right).Right]>0).Cast<IElement>().ToArray());
        var originalMeans=E(Diamonds.Rhythm!.Bindings).Cast<Relation>().ToDictionary<Relation,IElement,int>(row=>row.Left,Side,ReferenceEqualityComparer.Instance);
        TrainingOccurrences=S(E(Diamonds.Rhythm.Occurrences).Where(face=>originalMeans[face]<0).ToArray());
        HeldOutOccurrences=S(E(Diamonds.Rhythm.Occurrences).Where(face=>originalMeans[face]>0).ToArray());
        PartitionEvidence=new(cut,S(partitionProofs.ToArray()));
        var down=S(frame.Right,frame.Down);var up=S(frame.Right,frame.Up);var flat=S(frame.Right);
        HeadingQuestions=[Q("Falling / rising / forward",S(down,up,flat)),Q("Rising / falling / forward",S(up,down,flat)),Q("Straight forward",S(flat)),
            Q("Two copies of the same recipe",S(down,up,flat,down,up,flat)),Q("Two flats then a diagonal",S(flat,flat,down))];
        TranslationQuestions=[Translation("Six shared reaches",6),Translation("Three shared reaches",3),Translation("Five shared reaches",5),Translation("Seven shared reaches",7)];
        progress?.Invoke("Preparing original candidate reapplications");
        var baseline=Evaluate("original","Original field","Original candidates reapplied to their original exposed state.",basis.Read.Topology,basis.Bindings,basis.Interpretation,null);
        var moves=new List<State>{baseline};
        for(var amount=1;amount<=4;amount++)
        {
            progress?.Invoke("Checking landmark delta "+amount);
            var patch=OrnamentStructurePerturbation.WordDelta(frame,basis.Interpretation,basis.Read.Topology,basis.Bindings,TargetBinding,W(frame.Right,amount),new Placeholder());
            moves.Add(Evaluate("right-"+amount,"+ "+amount+" reach participation" ,"One role is rebound through formal additive reduction. The original topology and candidates remain captured.",patch.Topology,patch.Bindings,patch.Surface,patch));
        }
        var withdrawn=OrnamentStructurePerturbation.WithdrawEdge(basis.Interpretation,basis.Read.Topology,basis.Bindings,TargetEdge,new Placeholder());
        var detached=OrnamentStructurePerturbation.DetachEndpoint(basis.Interpretation,basis.Read.Topology,basis.Bindings,TargetEdge,TargetRole,new Placeholder());
        var open=OrnamentStructurePerturbation.ReplaceWord(basis.Interpretation,basis.Read.Topology,basis.Bindings,TargetBinding,StructuralConstraint.Open(E((Relation)TargetBinding.Right)),new Placeholder());
        var foreign=OrnamentStructurePerturbation.ReplaceWord(basis.Interpretation,basis.Read.Topology,basis.Bindings,TargetBinding,S([..E((Relation)TargetBinding.Right),QualitativeMotionFrame.Move(frame.Right,new Placeholder())]),new Placeholder());
        Experiments=[new("P01","One landmark, several resolutions","Each slider position is a checked counterfactual branch from the original. Cell closure, qualitative heading and exact word repetition ask different questions.",moves.ToArray()),
            new("P02","Withdraw a passage or detach its endpoint","The detached endpoint retains the same word position and a different identity. The drawing can look closed while the original connection claim fails.",[baseline,
                Evaluate("withdraw","Passage withdrawn","The actual passage and its two incident supplied cell declarations are withdrawn. No merged face is invented.",withdrawn.Topology,withdrawn.Bindings,withdrawn.Surface,withdrawn),
                Evaluate("detach","Endpoint detached","One edge has a fresh endpoint with the original word. Equal projected position does not restore the connection.",detached.Topology,detached.Bindings,detached.Surface,detached)]),
            new("P03","Unavailable detail stays local","Incomplete words and incomparable reach identities leave dependent claims unresolved. Remote original routes can still be supported by their own evidence.",[baseline,
                Evaluate("open-word","Open word tail","Known word participation is retained with an open tail. Its old display position is a historical guide.",open.Topology,open.Bindings,open.Surface,open),
                Evaluate("foreign-reach","Another reach identity","An additional forward participation uses another uncalibrated reach. It cannot be drawn as one ordinary reach or canceled against it.",foreign.Topology,foreign.Bindings,foreign.Surface,foreign)])];
        Surface=new(Original.Surface,new Relation(TargetSelection,new Relation(PartitionEvidence,new Relation(new Relation(TrainingEdges,HeldOutEdges),new Relation(new Relation(TrainingOccurrences,HeldOutOccurrences),S(Experiments.SelectMany(series=>series.States).Distinct(ReferenceEqualityComparer.Instance).Cast<State>().Select(state=>(IElement)state.Surface).ToArray()))))));

        NamedQuestion Q(string name,Relation question)=>new(name,new(new Placeholder(),question));
        NamedQuestion Translation(string name,int repeats)=>Q(name,S(W(frame.Right,repeats),W(frame.Up,repeats)));
        Relation W(IElement direction,int count)=>S(Enumerable.Range(0,count).Select(_=>(IElement)QualitativeMotionFrame.Move(direction,Field.Source.Reach)).ToArray());
        int Side(Relation binding)
        {
            var inverse=S(E(cut).Cast<Relation>().Select(move=>(IElement)QualitativeMotionFrame.Move(frame.Down,move.Right)).ToArray());
            var comparison=frame.Reduce((Relation)binding.Right,inverse);partitionProofs.Add(new Relation(binding,comparison));
            if(!frame.TryReadResult(comparison,out var remaining,out _,out var pending)||E(pending).Length!=0)throw new InvalidOperationException("The declared partition requires a resolved source-side read.");
            var vertical=E(remaining).Cast<Relation>().Where(move=>ReferenceEquals(move.Left,frame.Up)||ReferenceEquals(move.Left,frame.Down)).ToArray();
            return vertical.Length==0?0:ReferenceEquals(vertical[0].Left,frame.Up)?1:-1;
        }
        bool Heading(Relation use,params IElement[] expected)
        {
            var read=E(Forward.Strands!.EdgeReads).Cast<Relation>().Single(row=>row.Left is Relation actual&&ReferenceEquals(actual.Left,use.Left)&&actual.Right is Relation ends&&use.Right is Relation other&&ReferenceEquals(ends.Left,other.Left)&&ReferenceEquals(ends.Right,other.Right));
            var heading=(Relation)((Relation)((Relation)read.Right).Right).Right;
            return E(heading).Length==expected.Length&&expected.All(direction=>E(heading).Any(actual=>ReferenceEquals(actual,direction)));
        }
        State Evaluate(string id,string name,string description,Relation topology,Relation bindings,IElement change,OrnamentStructurePerturbation? patch)
        {
            var cells=E(Diamonds.Cells!.Families).Cast<Relation>().Select(family=>OrnamentCandidateRevalidation.Cells(Diamonds.Cells,family,topology,bindings,new Placeholder(),change)).ToArray();
            var forward=OrnamentCandidateRevalidation.Strands(Forward.Strands!,Forward.Occurrences,topology,bindings,new Placeholder(),change);
            var reverse=OrnamentCandidateRevalidation.Strands(Reverse.Strands!,Reverse.Occurrences,topology,bindings,new Placeholder(),change);
            var fourCheck=cells.Single(read=>ReferenceEquals(read.Candidate,Diamonds.Candidate));
            var meanReads=new List<IElement>();var meanBindings=new List<IElement>();
            foreach(var occurrence in E(Diamonds.Occurrences).Cast<Relation>())
            {
                Diamonds.Cells.TryReadOccurrence(Diamonds.Candidate,occurrence,out var face,out var boundary,out _);
                var relevant=E(boundary).Cast<Relation>().Select(part=>((Relation)part.Right).Left).ToArray();
                var sourceBindings=S(relevant.Select(role=>E(bindings).Cast<Relation>().Single(row=>ReferenceEquals(row.Left,role))).Cast<IElement>().ToArray());
                var read=OrnamentWordLandmarks.Share(frame,sourceBindings,S(relevant.Select(_=>(IElement)new Placeholder()).ToArray()));
                var supported=E(fourCheck.Supported).OfType<Relation>().Any(witness=>ReferenceEquals(witness.Left,occurrence));
                var word=supported?(Relation)((Relation)read.Right).Left:StructuralConstraint.Open(); // Never publish a landmark from a retired chamber.
                var binding=new Relation(face,word);meanBindings.Add(binding);meanReads.Add(new Relation(occurrence,new Relation(fourCheck.Surface,new Relation(read,binding))));
            }
            var means=S(meanBindings.ToArray());var proofs=S(meanReads.ToArray());
            var rhythm=OrnamentCandidateRevalidation.Rhythm(Diamonds.Rhythm!,means,new Placeholder(),new Relation(change,proofs));
            Relation ActiveSplit(Relation original)=>S(E(original).Cast<Relation>().Select(old=>E((Relation)((Relation)topology.Right).Left).Cast<Relation>().SingleOrDefault(current=>ReferenceEquals(current.Left,old.Left))).Where(row=>row is not null).Cast<IElement>().ToArray());
            var heading=OrnamentQuestionComparison.Heading(frame,change,topology,bindings,ActiveSplit(TrainingEdges),ActiveSplit(HeldOutEdges),S(HeadingQuestions.Select(q=>(IElement)q.Definition).ToArray()),new Placeholder());
            var translations=OrnamentQuestionComparison.Translation(frame,new Relation(change,proofs),Diamonds.Rhythm!.Occurrences,means,TrainingOccurrences,HeldOutOccurrences,S(TranslationQuestions.Select(q=>(IElement)q.Definition).ToArray()),Diamonds.Rhythm.Phases,new Placeholder());
            var fresh=new OrnamentStrandDiscovery(frame,change,topology,bindings,Forward.Strands!.Pattern,new Placeholder());
            var surface=new Relation(change,new Relation(S(cells.Select(read=>(IElement)read.Surface).ToArray()),new Relation(new Relation(forward.Surface,reverse.Surface),new Relation(new Relation(proofs,rhythm.Surface),new Relation(new Relation(heading.Surface,translations.Surface),fresh.Surface)))));
            return new(id,name,description,topology,bindings,change,patch,cells,forward,reverse,rhythm,proofs,means,heading,translations,fresh,surface);
        }
    }
    private static Relation S(params IElement[] entries)=>StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope)=>N.E(scope);
}
