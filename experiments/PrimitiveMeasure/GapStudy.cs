namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Correspondence from the existing coarse sketch/contour to a unary
/// gap rung. The subdivision strip records how many fine uses share one parent
/// step. Choosing this rung starts an independent calibrated experiment; it
/// does not silently split material in an already running pool.</summary>
public sealed class GapStudy
{
    public OrdinalSketch? Sketch { get; }
    public ContourPattern? Contour { get; }
    public ParticipationStrip Subdivision { get; }
    public ParticipationStrip[] ParentChains { get; }
    public GapSettlement Pool { get; }
    public int[] Initial { get; }
    public int[] ParentAllocation { get; }
    private readonly EndpointRows bindings = new(), calibration = new(), fineUses = new();
    public int BindingArity=>Sketch is null?2:4;
    public int BindingBits=>Sketch is null?6:3;
    public int[] Binding(int i)=>bindings.Read(new(BindingArity,BindingBits,i));
    public byte[] BindingBytes()=>bindings.PackedBytes(BindingArity,BindingBits);
    public byte[] CalibrationBytes()=>calibration.PackedBytes(2,2);
    public byte[] FineUseBytes()=>fineUses.PackedBytes(1,2);
    public bool RegularNow=>Contour?.Read().Circle==true && Pool.Audit().Even;

    public GapStudy(int shape,int budget = 0,int wiring = 0)
    {
        if(shape is < 0 or > 5 || budget is < 0 or > 2 || wiring is < 0 or > 3)
            throw new ArgumentOutOfRangeException(nameof(shape));
        int n=new[]{7,3,4,5,6,8}[shape];
        int[] allocation=shape==0?[8,1,5,4,1,6,3]:Enumerable.Repeat(4,n).ToArray();
        if(shape!=0){allocation[0]+=3;allocation[1]-=2;allocation[2]--;}
        if(budget!=0)allocation[0]++;
        ParentAllocation=allocation.ToArray();
        ParentChains=allocation.Select(n=>new ParticipationStrip(Enumerable.Repeat((byte)1,n).ToArray())).ToArray();
        int fine=budget==2?n:1;
        Subdivision=new(Enumerable.Repeat((byte)1,fine).ToArray());
        // Parent unit 0 binds to run 2, whose members use fine unit 1.
        calibration.Append(2,0,2);
        for(int i=0;i<fine;i++)fineUses.Append(2,1);
        Initial=allocation.Select(a=>a*fine).ToArray();
        int[][]? links=wiring switch
        {
            1=>Enumerable.Range(0,n-1).Select(i=>new[]{i,i+1}).ToArray(),
            3=>(from a in Enumerable.Range(1,n-1) from b in Enumerable.Range(a+1,n-a-1) select new[]{a,b}).ToArray(),
            _=>null
        };
        // With three gaps an isolated first gap leaves one physical pair. Two
        // visits to that same pair preserve the existing two-site clock grammar.
        if(links?.Length==1)links=[links[0],links[0]];
        Pool=new(Initial,links,wiring==2?0:-1,Enumerable.Repeat(1,n).ToArray());
        if(shape==0)
        {
            Sketch=HintExamples.Sketch(false);
            for(int i=0;i<4;i++)bindings.Append(3,0,i,i+1,i);
            for(int i=0;i<3;i++)bindings.Append(3,1,i,i+1,i+4);
        }
        else
        {
            Contour=new(new Present(),new Present());
            int[] sites=[ContourPattern.Start,..Enumerable.Range(5,n-1),ContourPattern.End];
            for(int i=0;i<n;i++)
            {
                Contour.Arc(sites[i],sites[i+1],32+i);
                Contour.Chord(sites[i],i==n-1?ContourPattern.Start:sites[i+1]);
                bindings.Append(6,32+i,i);
            }
            Contour.Close();
        }
    }
    private sealed class Present : IPrimitiveReadPort { public byte ReadSupport()=>2; }
}

public static class GapDrawing
{
    public static HintScene Draw(GapStudy study,int presentation = 0)
    {
        var scene=new HintScene();int[] current=study.Pool.Counts();
        scene.Label("THE WHITESPACE HAS ITS OWN STEP CHAINS",360,22);
        scene.Label("Initial allocation",175,53);scene.Label("Current allocation",535,53);
        if(study.Sketch is {} sketch)
        {
            PenPoint[] Points(int[] steps)
            {
                double[] x=new double[5],y=new double[4];
                for(int i=0;i<study.Pool.Count;i++)
                {
                    var link=study.Binding(i);var axis=link[0]==0?x:y;
                    axis[link[2]]=axis[link[1]]+(double)steps[link[3]]/study.Subdivision.Count;
                }
                return Enumerable.Range(0,sketch.SiteCount).Select(i=>
                {
                    var point=sketch.Site(i);return new PenPoint(x[point[0]],y[point[1]]);
                }).ToArray();
            }
            var before=Points(study.Initial);var after=Points(current);
            double extent=Math.Max(before.Concat(after).Max(p=>p.X),before.Concat(after).Max(p=>p.Y));
            double scale=240/extent;
            for(int frame=0;frame<2;frame++)
            {
                var points=frame==0?before:after;double left=frame==0?55:415;
                foreach(int edge in Enumerable.Range(0,sketch.Path.EdgeCount))
                {
                    var pair=sketch.Path.Edge(edge);var a=points[pair[0]];var b=points[pair[1]];
                    scene.Strokes.Add(new("ink",new(left+a.X*scale,327-a.Y*scale),new(left+b.X*scale,327-b.Y*scale)));
                }
                var steps=frame==0?study.Initial:current;
                double x=left,y=327;
                for(int i=0;i<4;i++)
                {
                    double next=x+steps[i]*scale/study.Subdivision.Count;
                    scene.Strokes.Add(new("gap"+i,new(x,345),new(next,345),true));
                    scene.Dots.Add(new("gap"+i,new(x,345),2));x=next;
                }
                scene.Dots.Add(new("gaps",new(x,345),2));
                for(int i=4;i<7;i++)
                {
                    double next=y-steps[i]*scale/study.Subdivision.Count;
                    scene.Strokes.Add(new("gap"+i,new(left-15,y),new(left-15,next),true));
                    scene.Dots.Add(new("gap"+i,new(left-15,y),2));y=next;
                }
            }
        }
        else
        {
            for(int frame=0;frame<2;frame++)
            {
                var steps=frame==0?study.Initial:current;double total=steps.Sum(),angle=-Math.PI/2+presentation*.15;
                double cx=frame==0?175:535;const double cy=210,radius=132;
                var points=new List<PenPoint>();
                scene.Dots.Add(new("radial",new(cx,cy),radius,true));
                foreach(int step in steps){points.Add(new(cx+radius*Math.Cos(angle),cy+radius*Math.Sin(angle)));angle+=2*Math.PI*step/total;}
                for(int i=0;i<points.Count;i++)
                {
                    scene.Strokes.Add(new("gap"+i,points[i],points[(i+1)%points.Count]));
                    scene.Dots.Add(new("gap"+i,points[i],3.5));
                }
            }
        }
        scene.Label("Step participation in gap order",360,382);
        for(int i=0;i<current.Length;i++)
        {
            double x=85+i*550.0/(current.Length-1);
            scene.Label(study.Initial[i]+" → "+current[i],x,412);
            scene.Label("gap "+i,x,434);
        }
        scene.Label("One step leaves a longer gap and enters a shorter gap. Nothing is discarded.",360,463);
        return scene;
    }
}
