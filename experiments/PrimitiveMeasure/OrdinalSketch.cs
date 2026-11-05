namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Two ordered rail surfaces and a path through paired rail references.
/// Each site row is [across landmark, rise landmark]. Reusing a reference fixes
/// alignment without assigning a distance. The optional axis binding uses two
/// adjacent rays of a closed, equal four-part radial construction.</summary>
public sealed class OrdinalSketch
{
    public LandmarkRun Across { get; }
    public LandmarkRun Rise { get; }
    public ContourPattern TurnBasis { get; } = RepetitionExamples.Contour(4,true,false);
    public OrderedBoundary Path { get; }
    private readonly EndpointRows sites = new(), axes = new();
    public int Bits { get; }
    public int SiteCount => sites.Count(2,Bits);
    public bool Perpendicular => axes.Count(3,6)==2 && TurnBasis.Read() is {Circle:true,EqualParts:true,Parts:4};
    public byte[] SiteBytes() => sites.PackedBytes(2,Bits);
    public byte[] AxisBytes() => axes.PackedBytes(3,6);
    public int[] Site(int index) => sites.Read(new(2,Bits,index));

    public OrdinalSketch(LandmarkRun across,LandmarkRun rise,int[][] points,bool close,bool perpendicular = true)
    {
        Across=across; Rise=rise; Bits=Math.Max(across.Bits,rise.Bits);
        if (points.Length<2 || points.Any(p=>p.Length!=2 || !across.Sites().Contains(p[0]) || !rise.Sites().Contains(p[1])))
            throw new ArgumentException("Each site needs a landmark on each rail.");
        foreach (var p in points) sites.Append(Bits,p);
        if (perpendicular)
        {
            axes.Append(6,0,ContourPattern.Centre,ContourPattern.Start);
            axes.Append(6,1,ContourPattern.Centre,TurnBasis.ArcRow(1)[0]);
        }
        var edges=Enumerable.Range(0,points.Length-1).Select(i=>new[]{i,i+1}).ToList();
        if (close) edges.Add([points.Length-1,0]);
        Path=new(points.Length,edges.ToArray(),Enumerable.Range(0,edges.Count).Select(i=>i*2).ToArray());
    }

    public SketchRead Read()
    {
        bool conflict=Across.Compare(Across.Sites()[0],Across.Sites()[0])==LandmarkRead.Conflict ||
            Rise.Compare(Rise.Sites()[0],Rise.Sites()[0])==LandmarkRead.Conflict;
        bool order=CompleteOrder(Across) && CompleteOrder(Rise);
        int[] directions=Enumerable.Range(0,Path.UseCount).Select(i=>
        {
            var edge=Path.DirectedUse(i); var a=Site(edge[0]); var b=Site(edge[1]);
            if (a[0]==b[0] && Rise.Compare(a[1],b[1]) is LandmarkRead.Before or LandmarkRead.After) return 1;
            if (a[1]==b[1] && Across.Compare(a[0],b[0]) is LandmarkRead.Before or LandmarkRead.After) return 0;
            return -1;
        }).ToArray();
        var first=Path.DirectedUse(0); var last=Path.DirectedUse(Path.UseCount-1);
        bool closed=last[1]==first[0];
        int turns=Perpendicular && !conflict ? Enumerable.Range(closed?0:1,directions.Length-(closed?0:1))
            .Count(i=>directions[i]>=0 && directions[(i+directions.Length-1)%directions.Length]>=0 &&
                directions[i]!=directions[(i+directions.Length-1)%directions.Length]) : 0;
        bool rectangle=closed && SiteCount==4 && Path.UseCount==4 && turns==4 &&
            Enumerable.Range(0,4).Select(i=>Site(i)[0]).Distinct().Count()==2 &&
            Enumerable.Range(0,4).Select(i=>Site(i)[1]).Distinct().Count()==2;
        return new(conflict,order,closed,turns,rectangle);
    }
    private static bool CompleteOrder(LandmarkRun rail) => rail.Sites().All(a=>rail.Sites().All(b=>
        rail.Compare(a,b) is LandmarkRead.Before or LandmarkRead.After or LandmarkRead.SameSite));
}

public readonly record struct SketchRead(bool Conflict,bool Ordered,bool Closed,int RightTurns,bool Rectangle);

/// <summary>The origin, motif's far edge and next origin share an ordered
/// placement rail. This is a spacing contract, separate from the motif and
/// from the finite strip that chooses occupied repeat places.</summary>
public sealed record OrdinalFrieze(OrdinalSketch Motif,ParticipationStrip Repeats,LandmarkRun Placement);

public static class HintExamples
{
    public static OrdinalSketch Sketch(bool rectangle,int relaxed = 0)
    {
        int[] x=rectangle?[0,1]:[0,1,2,3,4], y=rectangle?[0,1]:[0,1,2,3];
        int[][] claims=x.Zip(x.Skip(1),(a,b)=>new[]{a,b}).ToArray();
        LandmarkRun across=relaxed==1?new(3,x,claims.Where((_,i)=>i!=(rectangle?0:1)).ToArray()):
            relaxed==4?new(3,x,claims.Append(new[]{x[^1],x[0]}).ToArray()):new(3,x);
        int[][] points=rectangle?[[0,0],[1,0],[1,1],[0,1]]:
            [[0,0],[0,3],[4,3],[4,0],[1,0],[1,2],[3,2],[3,1],[2,1]];
        return new(across,new(3,y),points,rectangle && relaxed!=3,relaxed!=2);
    }
    public static OrdinalFrieze Frieze(bool rectangle,int relaxed = 0,bool skipping = false) =>
        new(Sketch(rectangle,relaxed),new(skipping?[1,0,1,1,0,1]:[1,1,1,1,1,1]),
            relaxed==3 && !rectangle?new(2,[0,1,2],[[0,1],[0,2]]):new(2,[0,1,2]));

    public static MarkedContour Circle(int selection,int relaxed = 0)
    {
        byte[] marks=selection switch
        {
            0=>[1,0,1,1,0,1], 1=>[1,1,1,1,1,1], 2=>[1,0,1,0,1,0],
            3=>[1,0,0,1,0,0,1,0,0,1,0,0], 4=>[1,1,1,1,0,0],
            _=>[0,0,0,0,0,0]
        };
        return new(RepetitionExamples.Contour(marks.Length,relaxed!=1,false),new(marks),relaxed!=2,relaxed==3?1:-1);
    }

    // These are two one-bit clock streams. Their paired fields compile directly
    // into the already-tested BorderScore clock. Voice heads advance only when
    // selected, unlike the always-advancing contour step beneath a mark strip.
    public static (ParticipationStrip First,ParticipationStrip Second,BorderScore Score) Streams(int selection)
    {
        byte[] a=selection switch {0=>[1,0,1,0,1,0,1,0],1=>[1,1,1,1],2=>[1,0,1,0,1,0,1,0],_=>[1,1,0,1,1,0]};
        byte[] b=selection switch {0=>[0,1,0,1,0,1,0,1],1=>[1,1,1,1],2=>[0,0,0,0,0,0,0,0],_=>[0,1,1,0,1,1]};
        // Opposed uses of the same unmeasured positive reach on each rail.
        var score=new BorderScore([1,1,3,3],[9,11,11,9],a.Zip(b,(x,y)=>(byte)(x|(y<<1))).ToArray());
        return (new(a),new(b),score);
    }
}
