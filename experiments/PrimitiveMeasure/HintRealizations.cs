using System.Globalization;
using System.Text;

namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Illustrative numerical witnesses, deliberately outside the encoded
/// sketch. Samples choose positive gaps and a presentation frame. They are not
/// inferred measurements, a general constraint solver, or additional evidence.</summary>
public sealed record RailRealization(double[] Across,double[] Rise,double Angle,PenPoint[] Points)
{
    public static RailRealization? Sample(OrdinalSketch sketch,int variation)
    {
        var xo=sketch.Across.Orders().ToArray(); var yo=sketch.Rise.Orders().ToArray();
        if (xo.Length==0 || yo.Length==0) return null;
        static double[] Positions(int[] order,int seed,double scale)
        {
            var result=new double[order.Max()+1]; double at=0;
            for (int i=0;i<order.Length;i++)
            {
                result[order[i]]=at;
                at+=scale*(0.6+((i*17+seed*13)%19)/10.0);
            }
            return result;
        }
        int v=Math.Abs(variation%1000);
        var x=Positions(xo[v%xo.Length],v,1+(v%3)*0.5);
        var y=Positions(yo[v%yo.Length],v+7,1+((v+1)%3)*0.4);
        double angle=sketch.Perpendicular?Math.PI/2:(v%2==0?Math.PI/3:Math.PI*2/3);
        var points=Enumerable.Range(0,sketch.SiteCount).Select(i=>
        {
            var site=sketch.Site(i);
            return new PenPoint(x[site[0]]+Math.Cos(angle)*y[site[1]],Math.Sin(angle)*y[site[1]]);
        }).ToArray();
        return new(x,y,angle,points);
    }

    public bool Admitted(OrdinalSketch sketch)
    {
        bool Fits(LandmarkRun rail,double[] samples) => rail.Sites().All(a=>rail.Sites().All(b=>rail.Compare(a,b) switch
        {
            LandmarkRead.Before=>samples[a]<samples[b], LandmarkRead.After=>samples[a]>samples[b],
            LandmarkRead.SameSite=>samples[a]==samples[b], LandmarkRead.Conflict or LandmarkRead.Outside=>false, _=>true
        }));
        return Fits(sketch.Across,Across) && Fits(sketch.Rise,Rise) &&
            (!sketch.Perpendicular || Math.Abs(Angle-Math.PI/2)<1e-12) &&
            Points.Length==sketch.SiteCount && Enumerable.Range(0,Points.Length).All(i=>
            {
                var site=sketch.Site(i);
                return Math.Abs(Points[i].X-Across[site[0]]-Math.Cos(Angle)*Rise[site[1]])<1e-10 &&
                    Math.Abs(Points[i].Y-Math.Sin(Angle)*Rise[site[1]])<1e-10;
            });
    }
}

public sealed record ContourRealization(PenPoint[] Points,double[] Shares)
{
    public static ContourRealization Sample(MarkedContour contour,int variation)
    {
        int v=Math.Abs(variation%1000),n=contour.Marks.Count;
        var parts=Enumerable.Range(0,n).Select(i=>contour.Contour.ArcRow(i)[2]).ToArray();
        var weights=parts.Distinct().Select((part,i)=>(part,weight:1.0+(i*(v+2)%5)))
            .ToDictionary(p=>p.part,p=>p.weight);
        double total=parts.Sum(p=>weights[p]);
        var shares=parts.Select(p=>weights[p]/total).ToArray();
        double at=-Math.PI/2+v*0.23,radius=1+v%3*0.25;
        var points=new PenPoint[n];
        for(int i=0;i<n;i++)
        {
            points[i]=new(radius*Math.Cos(at),radius*Math.Sin(at));
            at+=2*Math.PI*shares[i];
        }
        return new(points,shares);
    }
}

// Presentation primitives, shared by the live page and downloadable SVG. No
// point in these lists is read back as a structural proof.
public sealed record HintStroke(string Key,PenPoint From,PenPoint To,bool Guide = false);
public sealed record HintDot(string Key,PenPoint At,double Radius,bool Guide = false);
public sealed record HintCaption(string Text,PenPoint At);
public sealed class HintScene
{
    public List<HintStroke> Strokes { get; } = [];
    public List<HintDot> Dots { get; } = [];
    public List<HintCaption> Captions { get; } = [];
    public void Label(string text,double x,double y) => Captions.Add(new(text,new(x,y)));

    public string Svg()
    {
        static string F(double x)=>x.ToString("0.###",CultureInfo.InvariantCulture);
        var svg=new StringBuilder("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 720 480\"><rect width=\"720\" height=\"480\" fill=\"#f7f5ee\"/>");
        foreach(var s in Strokes) svg.Append($"<path data-key=\"{s.Key}\" d=\"M{F(s.From.X)} {F(s.From.Y)} L{F(s.To.X)} {F(s.To.Y)}\" fill=\"none\" stroke=\"{(s.Guide?"#a0b3aa":"#167c6c")}\" stroke-width=\"{(s.Guide?"1":"1.8")}\"{(s.Guide?" stroke-dasharray=\"4 5\"":"")}/>");
        foreach(var d in Dots) svg.Append($"<circle data-key=\"{d.Key}\" cx=\"{F(d.At.X)}\" cy=\"{F(d.At.Y)}\" r=\"{F(d.Radius)}\" fill=\"none\" stroke=\"{(d.Guide?"#a0b3aa":"#167c6c")}\"{(d.Guide?" stroke-dasharray=\"3 5\"":"")}/>");
        foreach(var t in Captions) svg.Append($"<text x=\"{F(t.At.X)}\" y=\"{F(t.At.Y)}\" text-anchor=\"middle\" font-family=\"system-ui,sans-serif\" font-size=\"12\" fill=\"#213b3a\">{System.Security.SecurityElement.Escape(t.Text)}</text>");
        return svg.Append("</svg>").ToString();
    }
}

public static class HintDrawing
{
    private static PenPoint[] Fit(PenPoint[] points,double cx,double cy,double width,double height)
    {
        double minx=points.Min(p=>p.X),maxx=points.Max(p=>p.X),miny=points.Min(p=>p.Y),maxy=points.Max(p=>p.Y);
        double scale=Math.Min(width/Math.Max(maxx-minx,0.01),height/Math.Max(maxy-miny,0.01));
        return points.Select(p=>new PenPoint(cx+(p.X-(minx+maxx)/2)*scale,cy-(p.Y-(miny+maxy)/2)*scale)).ToArray();
    }
    public static HintScene Rails(OrdinalFrieze frieze,int variation = 0)
    {
        var scene=new HintScene(); var sketch=frieze.Motif;
        scene.Label("ONE ENCODING / THREE POSSIBLE DRAWINGS",360,25);
        for(int sample=0;sample<3;sample++)
        {
            double cx=125+235*sample;
            var realization=RailRealization.Sample(sketch,variation+sample);
            if(realization is null) {scene.Label("Conflicting order",cx,147);continue;}
            var points=Fit(realization.Points,cx,146,172,170);
            for(int edge=0;edge<sketch.Path.UseCount;edge++)
            {
                var row=sketch.Path.DirectedUse(edge);
                scene.Strokes.Add(new("edge"+edge,points[row[0]],points[row[1]]));
            }
            for(int i=0;i<points.Length;i++) scene.Dots.Add(new("site"+i,points[i],2.7));
            scene.Label("Layout "+(sample+1),cx,259);
        }
        scene.Label("SIX REPEAT PLACES / A ZERO LEAVES ITS PLACE EMPTY",360,303);
        var basis=RailRealization.Sample(sketch,variation);
        if(basis!=null)
        {
            double width=basis.Across.Max()-basis.Across.Min();
            bool separate=frieze.Placement.Compare(1,2)==LandmarkRead.Before;
            double pitch=width*(separate?1.3+variation%3*0.2:0.65);
            var cloud=Enumerable.Range(0,frieze.Repeats.Count).SelectMany(i=>basis.Points.Select(p=>new PenPoint(p.X+pitch*i,p.Y))).ToArray();
            var fitted=Fit(cloud,360,379,640,100);
            var walk=new GateWalk(frieze.Repeats);
            foreach(var v in walk.Run())
            {
                if(!v.Marked) continue;
                for(int edge=0;edge<sketch.Path.UseCount;edge++)
                {
                    var row=sketch.Path.DirectedUse(edge);
                    scene.Strokes.Add(new("repeat"+v.Slot,fitted[v.Slot*sketch.SiteCount+row[0]],fitted[v.Slot*sketch.SiteCount+row[1]]));
                }
            }
        }
        scene.Label("Shared references fix alignment. Positive gaps choose the proportions.",360,460);
        return scene;
    }

    public static HintScene Circle(MarkedContour circle,int variation = 0,int visits = int.MaxValue)
    {
        var scene=new HintScene(); int shown=Math.Min(visits,circle.Trace.Length);
        scene.Label("KEEP THE STEP / CHOOSE WHETHER IT LEAVES A MARK",360,25);
        for(int sample=0;sample<3;sample++)
        {
            var realization=ContourRealization.Sample(circle,variation+sample);
            double cx=sample==0?225:555,cy=sample==0?233:sample==1?145:339,r=sample==0?148:76;
            double radius=Math.Sqrt(realization.Points[0].X*realization.Points[0].X+realization.Points[0].Y*realization.Points[0].Y);
            var points=realization.Points.Select(p=>new PenPoint(cx+r*p.X/radius,cy+r*p.Y/radius)).ToArray();
            if(sample==0) scene.Dots.Add(new("contour",new(cx,cy),r,true));
            var marked=circle.Trace.Take(shown).Where(v=>v.Completed && v.Marked).Select(v=>v.Slot).ToArray();
            for(int i=1;i<marked.Length;i++) scene.Strokes.Add(new("slot"+marked[i],points[marked[i-1]],points[marked[i]]));
            if(shown==circle.Trace.Length && circle.Read().Closed) scene.Strokes.Add(new("seam",points[marked[^1]],points[marked[0]]));
            for(int i=0;i<circle.Marks.Count;i++)
            {
                bool ink=marked.Contains(i);
                if(sample==0 || ink) scene.Dots.Add(new("slot"+i,points[i],ink?4.5:2.5,!ink));
                if(sample==0)
                {
                    var p=points[i]; var label=new PenPoint(cx+(p.X-cx)*1.12,cy+(p.Y-cy)*1.12);
                    scene.Label((i+1).ToString(),label.X,label.Y+4);
                }
            }
            if(sample>0) scene.Label("Same rows, another frame",cx,cy+r+23);
        }
        scene.Label(string.Join("   ",circle.Trace.Select(v=>v.Support==3?"?":v.Support==2?"1":"0")),225,440);
        scene.Label("A skipped mark is still a place in the circular chain.",360,460);
        return scene;
    }

    public static HintScene Streams(int selection,int variation = 0,int beats = int.MaxValue)
    {
        var scene=new HintScene(); var (a,b,score)=HintExamples.Streams(selection);
        scene.Label("TWO GATE CHAINS / ONE SAMPLED PATH",360,25);
        double h=1+variation%3*.45,v=1+(variation+1)%3*.4;
        var points=new List<PenPoint>{new(0,0)}; var phase=new BorderPhase(0,0,0);
        // The finite all-one chain supplies the stop. BorderScore is the derived
        // two-stream executor and exposes the separate held voice positions.
        var clock=new GateWalk(new(Enumerable.Repeat((byte)1,a.Count).ToArray()));
        foreach(var visit in clock.Run())
        {
            var p=points[^1]; double x=p.X,y=p.Y;
            foreach(var emission in score.Step(ref phase))
            {
                if(emission.Move.Axis==0) x+=emission.Move.Sense*h;
                else y+=emission.Move.Sense*v;
            }
            points.Add(new(x,y));
        }
        var fitted=Fit(points.ToArray(),360,212,540,280);
        for(int i=0;i<a.Count;i++)
            scene.Strokes.Add(new("beat"+i,fitted[i],fitted[i+1],i>=beats));
        scene.Dots.Add(new("clock",fitted[Math.Min(beats,a.Count)],5));
        for(int i=0;i<a.Count;i++)
        {
            double x=120+480.0*i/Math.Max(1,a.Count-1);
            scene.Label(a.At(i).ToString(),x,407);scene.Label(b.At(i).ToString(),x,438);
        }
        scene.Label("Across",50,407);scene.Label("Rise",50,438);
        scene.Label("00 waits · 10 across · 01 rise · 11 commits both as one diagonal",360,460);
        return scene;
    }
}
