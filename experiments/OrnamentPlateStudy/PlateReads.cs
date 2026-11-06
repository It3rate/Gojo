using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.OrnamentTension;

namespace Gojo.Experiments.OrnamentPlateStudy;

internal sealed record Scores(double Ink,double Order,double Course,double Clean,double Historical,
    double General,int Edges,int Overlaps,int Crossings,int Handoffs,bool ExactInk);

internal static class PlateReads
{
    // Directions and lengths in divisions of the plate's printed construction grid.
    // These are the external observer's registered readings of two different figures.
    internal static readonly (int D,int L)[][] Figures=[
        [(2,4),(0,4),(6,2),(4,2),(6,2),(0,4)],
        [(2,6),(0,6),(6,4),(4,2),(2,2),(4,2),(6,4),(0,6)]
    ];
    internal static readonly int[] Period=[6,8];
    private static readonly int[] DX=[1,1,0,-1,-1,-1,0,1],DY=[0,1,1,1,0,-1,-1,-1];
    internal static byte[] Author(int figure)
    {
        var rows=Figures[figure].Select(s=>StrokeRow.Bind(s.D,s.L switch {2=>1,4=>0,6=>2,_=>throw new InvalidOperationException()})).ToArray();
        return PathMachine.Encode(new PathOwner(8,85,0,rows));
    }

    private readonly record struct Line(int Axis,int Intercept);
    private readonly record struct Span(int Start,int End);
    internal static string InkKey(PathTrace trace)
    {
        var actual=new Ink();
        foreach(var e in trace.Edges)
        {
            var a=trace.Sites[e.From]; var b=trace.Sites[e.To];
            actual.Add(a.X,a.Y,b.X,b.Y);
        }
        actual.Settle();
        return string.Join(";",actual.Lines.OrderBy(p=>p.Key.Axis).ThenBy(p=>p.Key.Intercept).SelectMany(p=>
            p.Value.Select(s=>$"{p.Key.Axis}/{p.Key.Intercept}/{s.Start}/{s.End}")));
    }
    private sealed class Ink
    {
        internal readonly Dictionary<Line,List<Span>> Lines=[];
        internal void Add(int x,int y,int x2,int y2)
        {
            int dx=Math.Sign(x2-x),dy=Math.Sign(y2-y);
            int axis=dx==0 ? 2 : dy==0 ? 0 : dx==dy ? 1 : 3;
            var line=new Line(axis,axis switch{0=>y,1=>y-x,2=>x,_=>y+x});
            int u=axis==2 ? y : x,v=axis==2 ? y2 : x2;
            if(!Lines.TryGetValue(line,out var spans))Lines.Add(line,spans=[]);
            spans.Add(new(Math.Min(u,v),Math.Max(u,v)));
        }
        internal double Settle()
        {
            double total=0;
            foreach(var (line,spans) in Lines)
            {
                spans.Sort((a,b)=>a.Start.CompareTo(b.Start));
                int outCount=0;
                for(int i=0;i<spans.Count;i++)
                {
                    var next=spans[i];
                    if(outCount>0 && next.Start<=spans[outCount-1].End)
                    {
                        var last=spans[outCount-1];
                        spans[outCount-1]=new(last.Start,Math.Max(last.End,next.End));
                    }
                    else spans[outCount++]=next;
                }
                spans.RemoveRange(outCount,spans.Count-outCount);
                total+=spans.Sum(s=>(double)(s.End-s.Start))*Weight(line.Axis);
            }
            return total;
        }
        private static double Weight(int axis)=>axis%2==0?1:Math.Sqrt(2);
        internal double Intersection(Ink other)
        {
            double sum=0;
            foreach(var (line,a) in Lines)
            {
                if(!other.Lines.TryGetValue(line,out var b))continue;
                int i=0,j=0;
                while(i<a.Count && j<b.Count)
                {
                    sum+=Math.Max(0,Math.Min(a[i].End,b[j].End)-Math.Max(a[i].Start,b[j].Start))*Weight(line.Axis);
                    if(a[i].End<b[j].End)i++;else j++;
                }
            }
            return sum;
        }
    }

    // Longest common subsequence compares actual connected row order, preserving
    // genuine stops and breaks. No order is inferred from coincident positions.
    private static int Lcs(List<int> run,(int D,int L)[] word)
    {
        Span<int> values=stackalloc int[word.Length+1];
        foreach(int dir in run)
        {
            int diagonal=0;
            for(int j=1;j<=word.Length;j++)
            {
                int prior=values[j];
                values[j]=dir==word[j-1].D ? diagonal+1 : Math.Max(values[j],values[j-1]);
                diagonal=prior;
            }
        }
        return values[^1];
    }

    internal static Scores Measure(PathTrace trace,int figure)
    {
        int scale=Period[figure];
        var word=Figures[figure];
        var source=new Ink();var actual=new Ink();
        for(int cell=0;cell<trace.Cells;cell++)
        {
            int x=cell*trace.Pitch*scale,y=0;
            foreach(var (d,l) in word)
            {
                int nx=x+DX[d]*l*trace.Pitch,ny=y+DY[d]*l*trace.Pitch;
                source.Add(x,y,nx,ny);x=nx;y=ny;
            }
        }
        double deposited=0;
        foreach(var e in trace.Edges)
        {
            var a=trace.Sites[e.From];var b=trace.Sites[e.To];
            actual.Add(a.X*scale,a.Y*scale,b.X*scale,b.Y*scale);
            deposited+=scale*Math.Sqrt((double)(b.X-a.X)*(b.X-a.X)+(double)(b.Y-a.Y)*(b.Y-a.Y));
        }
        double original=source.Settle(),drawn=actual.Settle(),inCommon=source.Intersection(actual);
        double ink=inCommon/(original+drawn-inCommon);
        double clean=deposited==0 ? 0 : Math.Clamp(drawn/deposited,0,1);
        int seams=trace.Events.Where(e=>e.Kind=="continue").Select(e=>e.Cell).Distinct().Count();
        double handoffs=(double)seams/(trace.Cells-1);
        double order=0;
        for(int c=0;c<trace.Cells;c++)
        {
            double best=0;
            for(int o=0;o<trace.Owners;o++)
            {
                var run=new List<int>();int site=-1;
                void EndRun()
                {
                    if(run.Count>0)best=Math.Max(best,2.0*Lcs(run,word)/(run.Count+word.Length));
                    run.Clear();
                }
                foreach(var e in trace.Edges)
                {
                    if(e.Owner!=o || e.Cell!=c)continue;
                    if(e.From!=site)EndRun();
                    run.Add(e.Direction);site=e.To;
                }
                EndRun();
            }
            order+=best/trace.Cells;
        }
        var reads=PathJudge.Measure(trace);
        double general=0;
        string[] requested=["no-overdraw","connected-run","turn-variety","period-match","boundary-match","register-balance","gap-evenness"];
        for(int i=0;i<reads.Length;i++)
            if(requested.Contains(PathJudge.Criteria[i].Key) && !double.IsNaN(reads[i]))general+=reads[i]/requested.Length;
        var joint=JointCensus.Read(trace);
        double utility=.55*ink+.20*order+.15*handoffs+.10*clean;
        return new(ink,order,handoffs,clean,utility,general,trace.Edges.Length,joint.CollinearOverlaps,
            joint.InteriorCrossings,seams,Math.Abs(ink-1)<1e-10);
    }
}
