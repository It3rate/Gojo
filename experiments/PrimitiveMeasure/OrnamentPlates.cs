namespace Gojo.Experiments.PrimitiveMeasure;

public static class OrnamentPlates
{
    public static IReadOnlyList<OrnamentDesign> All { get; } =
        Array.AsReadOnly(new[]{WindRose(),Palmettes(),DoubleKey(false),DropField(),RoseArcade()});
    public static OrnamentDesign PairedKey { get; } = DoubleKey(true);

    private static OrnamentDesign WindRose()
    {
        var rim=RepetitionExamples.Contour(12,true,true);
        var d=new OrnamentDesign("Compass rosette",
            "Turn a split lance around twelve equal rim divisions. Where the two flanks meet at the outer eye, the finishing tool can strike a small crown.",
            [8,8,2,8,8,4],new(25,0),new(87,0),rim.PartCount,1,0,0,rim:rim);
        var basePen=new OrnamentPen(d,0);
        double turn=2*Math.PI/rim.PartCount;
        foreach (int radius in new[]{14,20,25,98,102})
        {
            int start=radius==25?d.Fork:basePen.P(radius,0);
            basePen.Line(start,radius*Math.Cos(turn),radius*Math.Sin(turn));
        }
        basePen.Leaf(basePen.P(5,0),basePen.P(18,0),2.5);
        basePen.To(basePen.P(20,0),d.Fork);
        foreach (int s in new[]{1,-1})
        {
            var p=new OrnamentPen(d,s==1?1:2,1,s);
            int shoulder=p.Curve(d.Fork,40,0,43,18,57,16);
            p.CurveTo(shoulder,69,15,71,5,d.Eye);
            p.CurveTo(shoulder,52,12,48,6,d.Fork);
            p.Leaf(shoulder,p.P(77,3),3.4);
            int hook=p.Curve(shoulder,61,18,65,22,69,19);
            p.Curve(hook,74,15,67,12,66,16);
        }
        var f=new OrnamentPen(d,3);
        f.Leaf(d.Eye,f.P(98,0),4.6,true);
        f.Leaf(d.Eye,f.P(47,0),3.7);
        return d;
    }

    private static OrnamentDesign Palmettes()
    {
        var d=new OrnamentDesign("Palmette and volute",
            "Grow mirrored fan leaves from a running stem. Reflect the second course and open a central spear where the fan joins.",
            [8,8,8,2,8,4],new(0,18),new(0,-25),5,2,100,98,0,true);
        var g=new OrnamentPen(d,0);
        int seat=g.P(0,27);
        g.CurveTo(g.P(-50,12),-34,12,-18,29,seat);
        g.CurveTo(seat,18,29,34,12,g.P(50,12));
        g.To(seat,d.Fork);
        g.Line(g.P(-50,34),50,34); g.Line(g.P(-50,37),50,37);
        foreach (int s in new[]{1,-1})
        {
            var p=new OrnamentPen(d,s==1?1:2,s,1);
            int curl=p.Curve(d.Fork,8,21,20,15,29,10);
            int back=p.Curve(curl,47,-1,48,16,37,18);
            p.Curve(back,30,20,28,11,34,10);
            int hub=p.Curve(d.Fork,4,10,4,5,6,0);
            p.CurveTo(hub,7,-8,3,-20,d.Eye);
            p.Leaf(hub,p.P(13,-32),3.7,true);
            p.Leaf(hub,p.P(27,-24),5.0,true);
            p.Leaf(hub,p.P(39,-11),5.4);
            p.Leaf(curl,p.P(48,1),3.3);
        }
        var f=new OrnamentPen(d,3);
        f.Leaf(d.Eye,f.P(0,-40),4.2,true);
        f.Leaf(d.Eye,d.Fork,2.8);
        return d;
    }

    private static OrnamentDesign DoubleKey(bool paired)
    {
        byte[] vertical=[BorderStudy.Down,BorderStudy.Up|32,BorderStudy.Down|64,BorderStudy.Up|96];
        byte[] horizontal=[BorderStudy.Right|16,BorderStudy.Left|48,BorderStudy.Right|80,BorderStudy.Left|112];
        var score=new BorderScore(vertical,horizontal,paired?[(byte)3]:[(byte)1,(byte)2]);
        var d=new OrnamentDesign(paired?"Double-return fret · paired":"Double-return fret",
            "Two right-angle courses leave one fork, turn inward as keys and meet at the far eye. The joined tool engraves a lozenge clasp.",
            [8,2,8,4],new(-43,0),new(43,0),5,2,100,80,0,true,hookScore:score);
        var g=new OrnamentPen(d,0);
        g.To(g.P(-50,0),d.Fork); g.Line(d.Eye,50,0);
        g.Line(g.P(-50,-36),50,-36); g.Line(g.P(-50,36),50,36);
        foreach (int s in new[]{1,-1})
        {
            var p=new OrnamentPen(d,s==1?1:2,1,s);
            int elbow=p.Walk(d.Fork,(-38,0),(-38,-30));
            int hookStart=p.Line(elbow,-32,-30);
            int crown=p.Line(hookStart,24,-30);
            int foot=p.Walk(crown,(24,-5),(36,-5),(36,0));
            p.To(foot,d.Eye);
            // Reuse the border score's cardinal step engine and named reaches
            // to wind the inward key from its actual attachment at the elbow.
            // Eight reach slots have an explicit illustrative calibration.
            int[] lengths=[25,52,22,26,6,21,4,12];
            int x=-32,y=-30, current=hookStart;
            foreach (var beat in BorderStudy.Trace(score,score.Predict().Beats))
            {
                foreach (var emission in beat)
                {
                    var move=emission.Move;
                    int delta=move.Sense*lengths[move.Reach];
                    if (move.Axis==0) x+=delta; else y-=delta;
                }
                current=p.Line(current,x,y);
            }
        }
        var f=new OrnamentPen(d,3);
        int end=f.Walk(d.Eye,(48,-4),(53,0),(48,4));
        f.To(end,d.Eye);
        return d;
    }

    private static OrnamentDesign DropField()
    {
        var d=new OrnamentDesign("Half-drop ogee",
            "Hang adjacent columns half a repeat lower. Two curved ribs frame each seed pod and meet at its foot to release the hanging bud.",
            [8,8,4,8,8,2],new(0,-38),new(0,35),7,3,59,86,43);
        var g=new OrnamentPen(d,0);
        g.To(g.P(0,-51),d.Fork);
        g.Leaf(g.P(0,-43),d.Fork,2.2);
        foreach (int s in new[]{1,-1})
        {
            var p=new OrnamentPen(d,s==1?1:2,s,1);
            int hip=p.Curve(d.Fork,2,-23,25,-24,28,-6);
            p.CurveTo(hip,32,12,6,18,d.Eye);
            int knot=p.Curve(d.Fork,4,-19,18,-18,20,-4);
            p.CurveTo(knot,23,9,8,13,d.Eye);
            p.Leaf(knot,p.P(6,5),2.6);
            p.Leaf(hip,p.P(35,-14),2.3);
        }
        var f=new OrnamentPen(d,3);
        int heart=f.P(0,9);
        f.Leaf(d.Eye,heart,6.4,true);
        f.Leaf(heart,f.P(0,-11),4.3);
        return d;
    }

    private static OrnamentDesign RoseArcade()
    {
        var d=new OrnamentDesign("Rose-window arcade",
            "Two cusped ribs climb from a sill. Their crown starts a second press that turns six petals on its own clock.",
            [8,2,8,4],new(0,37),new(0,-41),4,2,86,94);
        var g=new OrnamentPen(d,0);
        g.Line(g.P(-43,41),43,41); g.Line(g.P(-43,44),43,44);
        g.To(g.P(0,41),d.Fork);
        foreach (int s in new[]{1,-1})
        {
            var p=new OrnamentPen(d,s==1?1:2,s,1);
            int sill=p.Line(d.Fork,36,37);
            int spring=p.Line(sill,36,1);
            p.CurveTo(spring,36,-19,16,-27,d.Eye);
            int inset=p.Walk(d.Fork,(31,32),(31,2));
            int cusp=p.Curve(inset,31,-4,27,-8,23,-7);
            p.CurveTo(cusp,27,-19,10,-25,d.Eye);
            int spur=p.Curve(spring,30,3,28,14,25,20);
            p.Leaf(spur,p.P(17,30),2.8);
            p.Leaf(spur,p.P(23,8),2.4);
        }
        var f=new OrnamentPen(d,3);
        int anchor=f.P(0,0);
        f.Leaf(d.Eye,f.P(0,-23),3.8);
        f.Leaf(d.Fork,f.P(0,23),3.8);
        var child=new OrnamentDesign("Window flower","Six independent petal sectors meet at the rim.",
            [8,8,2,8,8,4],new(3,0),new(18,0),6,1,0,0,rim:RepetitionExamples.Contour(6,true,true));
        var hub=new OrnamentPen(child,0);
        hub.To(hub.P(0,0),child.Fork);
        foreach (int s in new[]{1,-1})
        {
            var p=new OrnamentPen(child,s==1?1:2,1,s);
            p.CurveTo(child.Fork,8,3,12,8,child.Eye);
        }
        var rim=new OrnamentPen(child,3);
        rim.Line(child.Eye,21,0);
        rim.Curve(child.Eye,20,6,15,13,9,9*Math.Sqrt(3));
        d.Attach(anchor,child);
        return d;
    }
}
