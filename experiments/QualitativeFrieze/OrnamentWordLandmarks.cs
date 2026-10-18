using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>An optional equal-share landmark interpretation in a declared
/// additive word basis. It is observation, not a new physical junction or a
/// universal geometric centre. Indivisible participation stays as remainder.
/// The native slots, actual input bindings and sharing witnesses carry the law.</summary>
public sealed class OrnamentWordLandmarks
{
    // read = ((frame -> actual-bindings) -> distinct share-slots)
    //        -> (result-word -> (sharing-witnesses -> (remainder -> unresolved)))
    // sharing-witness = (Closed(actual input move occurrences) -> share-slots)
    //                   -> one output move with the same direction and reach
    // publication = actual source face -> (fresh representative -> complete word)
    // A word's repeated occurrences are participation, including equal refs.
    public ScaffoldQuestionScaffold Source { get; }
    public Relation SourceBindings { get; }
    public Relation Reads { get; }
    public Relation Publications { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }

    public OrnamentWordLandmarks(ScaffoldQuestionScaffold source, Relation sourceBindings)
    {
        Source=source;SourceBindings=sourceBindings;
        var reads=new List<IElement>();var publications=new List<IElement>();var pending=new List<IElement>();
        var ready=Closed(sourceBindings,out var bindings)&&source.IsComplete;
        if(!ready)pending.Add(new Relation(source.Surface,sourceBindings));
        foreach(var face in E(source.Faces).OfType<Relation>())
        {
            if(!ready){pending.Add(face);continue;}
            if(!source.TryReadFace(face,out _,out var vertices)){pending.Add(face);continue;}
            var actual=new List<IElement>();var valid=true;
            foreach(var vertex in E(vertices))
            {
                var matches=bindings.OfType<Relation>().Where(row=>ReferenceEquals(row.Left,vertex)).ToArray();
                if(matches is not [var binding]){pending.Add(new Relation(face,vertex));valid=false;continue;}
                actual.Add(binding);
            }
            if(!valid)continue;
            var slots=S(actual.Select(_=>(IElement)new Placeholder()).ToArray());
            var read=Share(source.Graph.Frame,S(actual.ToArray()),slots);
            reads.Add(new Relation(face,read));
            if(TryReadComplete(source.Graph.Frame,read,out var word))publications.Add(new Relation(face,new Relation(new Placeholder(),word)));
            else pending.Add(new Relation(face,read));
        }
        Reads=S(reads.ToArray());Publications=S(publications.ToArray());Unresolved=S(pending.ToArray());
        Surface=new(new Relation(source.Surface,sourceBindings),new Relation(Reads,new Relation(Publications,Unresolved)));
    }

    public bool TryRead(IElement sourceFace,out Relation binding,out Relation read)
    {
        binding=read=null!;
        var offers=E(Publications).OfType<Relation>().Where(row=>ReferenceEquals(row.Left,sourceFace)).ToArray();
        var evidence=E(Reads).OfType<Relation>().Where(row=>ReferenceEquals(row.Left,sourceFace)).ToArray();
        if(!Source.IsComplete||!Closed(SourceBindings,out _)||offers is not [var offer]||offer.Right is not Relation actual||evidence is not [var proof]||proof.Right is not Relation calculation||
            !TryReadComplete(Source.Graph.Frame,calculation,out var word)||!ReferenceEquals(actual.Right,word))return false;
        binding=actual;read=calculation;return true;
    }

    public static Relation Share(QualitativeMotionFrame frame,Relation actualBindings,Relation slots)
    {
        var pending=new List<IElement>();var remainder=new List<IElement>();var groups=new List<IElement>();var output=new List<IElement>();
        var bindingsClosed=Closed(actualBindings,out var bindings);var sharesClosed=Closed(slots,out var shares);
        var valid=bindingsClosed&&sharesClosed&&shares.Length!=0&&
            N.Distinct(shares)&&shares.All(slot=>slot is Placeholder{IsInverse:false})&&bindings.Length==shares.Length;
        var moves=new List<IElement>();
        foreach(var binding in bindings)
        {
            if(binding is not Relation{Right:Relation word}||!Closed(word,out var terms)||
                terms.Any(term=>term is not Relation move||!frame.Direction(move,out _,out _,out _)))
            {pending.Add(binding);valid=false;continue;}
            moves.AddRange(terms);
        }
        var sum=frame.Reduce(S(),S(moves.ToArray()));
        if(!frame.TryReadResult(sum,out var residual,out _,out var unknown)||E(unknown).Length!=0||!Closed(residual,out _))
        {pending.Add(sum);valid=false;}
        if(!valid)pending.Add(new Relation(actualBindings,slots));
        else
        {
            var unused=E(residual).OfType<Relation>().ToList();
            while(unused.Count!=0)
            {
                var first=unused[0];var bucket=unused.Where(move=>ReferenceEquals(move.Left,first.Left)&&ReferenceEquals(move.Right,first.Right)).ToArray();
                foreach(var move in bucket)unused.Remove(move);
                var at=0;
                while(at+shares.Length<=bucket.Length)
                {
                    var term=QualitativeMotionFrame.Move(first.Left,first.Right);output.Add(term);
                    groups.Add(new Relation(new Relation(S(bucket.Skip(at).Take(shares.Length).Cast<IElement>().ToArray()),slots),term));at+=shares.Length;
                }
                remainder.AddRange(bucket.Skip(at));
            }
        }
        var result=valid&&remainder.Count==0?S(output.ToArray()):StructuralConstraint.Open(output.ToArray());
        return new(new Relation(new Relation(frame.Surface,actualBindings),slots),
            new Relation(result,new Relation(S(groups.ToArray()),new Relation(S(remainder.ToArray()),S(pending.Append(sum).ToArray())))));
    }

    public static bool TryReadComplete(QualitativeMotionFrame frame,Relation read,out Relation word)
    {
        word=null!;
        if(read.Left is not Relation{Left:Relation{Left:var basis,Right:Relation bindings},Right:Relation slots}||!ReferenceEquals(basis,frame.Surface)||
            !Closed(bindings,out var sources)||!Closed(slots,out var shares)||shares.Length==0||shares.Length!=sources.Length||
            !N.Distinct(shares)||shares.Any(slot=>slot is not Placeholder{IsInverse:false})||
            read.Right is not Relation{Left:Relation result,Right:Relation{Left:Relation groups,Right:Relation{Left:Relation remainder,Right:Relation evidence}}}||
            !Closed(result,out var terms)||!Closed(remainder,out var remaining)||remaining.Length!=0||!Closed(evidence,out var proofs)||
            proofs is not [Relation sum]||!Closed(groups,out var witnesses)||witnesses.Length!=terms.Length)return false;
        // The summation is an actual formal read whose residual occurrences
        // must all be discharged exactly once by the sharing witnesses.
        if(sum.Left is not Relation{Left:var sumBasis,Right:Relation{Left:Relation before,Right:Relation incoming}}||!ReferenceEquals(sumBasis,frame.Surface)||
            !Closed(before,out var previous)||previous.Length!=0||!Closed(incoming,out var offered)||
            sum.Right is not Relation{Left:Relation residual,Right:Relation{Right:Relation unresolved}}||
            !Closed(residual,out var input)||!Closed(unresolved,out var pending)||pending.Length!=0)return false;
        if(!frame.TryReadResult(sum,out _,out _,out _))return false;
        var actualInput=new List<IElement>();
        foreach(var binding in sources)
        {
            if(binding is not Relation{Right:Relation sourceWord}||!Closed(sourceWord,out var sourceMoves)||
                sourceMoves.Any(move=>move is not Relation term||!frame.Direction(term,out _,out _,out _)))return false;
            actualInput.AddRange(sourceMoves);
        }
        if(offered.Length!=actualInput.Count||!offered.Zip(actualInput).All(pair=>ReferenceEquals(pair.First,pair.Second)))return false;
        var check=frame.Reduce(S(),S(actualInput.ToArray()));
        if(!frame.TryReadResult(check,out var expected,out _,out var checkUnknown)||E(checkUnknown).Length!=0)return false;
        var expectedInput=E(expected).ToList();
        foreach(var item in input){var at=expectedInput.FindIndex(actual=>ReferenceEquals(actual,item));if(at<0)return false;expectedInput.RemoveAt(at);}
        if(expectedInput.Count!=0)return false;
        var consumed=new List<IElement>();
        for(var i=0;i<terms.Length;i++)
        {
            if(terms[i] is not Relation term||witnesses[i] is not Relation{Left:Relation{Left:Relation members,Right:var actualSlots},Right:var output}||
                !ReferenceEquals(actualSlots,slots)||!ReferenceEquals(output,term)||!Closed(members,out var participants)||participants.Length!=shares.Length||
                participants.Any(item=>item is not Relation move||!ReferenceEquals(move.Left,term.Left)||!ReferenceEquals(move.Right,term.Right)))return false;
            consumed.AddRange(participants);
        }
        var available=input.ToList();
        foreach(var item in consumed){var at=available.FindIndex(actual=>ReferenceEquals(actual,item));if(at<0)return false;available.RemoveAt(at);}
        if(available.Count!=0)return false;
        word=result;return true;
    }
    private static bool Closed(Relation scope,out IElement[] entries)=>StructuralConstraint.TryReadScope(scope,out entries,out var seam,out var tail)&&seam.Equals(tail);
    private static Relation S(params IElement[] values)=>StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope)=>FiniteLaw.Entries(scope);
}
