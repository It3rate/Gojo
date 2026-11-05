namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Numerical witnesses of the declared normal/offset/intersection
/// law. These coordinates never feed the controller or establish stop evidence.</summary>
public static class StripDrawing
{
    public static PenPoint Normal(StripWalk walk,int normal,double rotation=0)
    {
        double angle=-Math.PI/2+Math.PI/walk.Sides+2*Math.PI*normal/walk.Sides+rotation;
        return new(Math.Cos(angle),Math.Sin(angle));
    }
    public static PenPoint Point(StripWalk walk,int joint,double rotation=0)
    {
        var pair=walk.JointRow(joint);var a=walk.LineRow(pair[0]);var b=walk.LineRow(pair[1]);
        var u=Normal(walk,a[0],rotation);var v=Normal(walk,b[0],rotation);
        double first=walk.Depth.Count-a[1],second=walk.Depth.Count-b[1],det=u.X*v.Y-u.Y*v.X;
        return new((first*v.Y-second*u.Y)/det,(u.X*second-v.X*first)/det);
    }
    public static HintScene Draw(StripWalk walk)
    {
        var scene=new HintScene();double scale=145*Math.Cos(Math.PI/walk.Sides)/walk.Depth.Count;
        PenPoint Map(PenPoint p)=>new(223+p.X*scale,205+p.Y*scale);
        void Line(string key,PenPoint a,PenPoint b,bool guide=false)=>scene.Strokes.Add(new(key,Map(a),Map(b),guide));
        scene.Label(walk.ClosedPass?"CLOSED PERIMETERS / SHARED INWARD GAP":"ONE OPEN PATH / REUSED TURN CYCLE",360,22);
        for(int i=0;i<walk.Sides;i++)Line("source",Point(walk,i),Point(walk,(i+1)%walk.Sides),true);
        for(int i=0;i<walk.EdgeCount;i++)
        {
            var e=walk.Edge(i);Line("edge"+i,Point(walk,e[0]),Point(walk,e[1]));
        }
        for(int i=0;i<walk.BandCount;i++)
        {
            var bandRow=walk.Band(i);var outer=walk.LineRow(bandRow[0]);var inner=walk.LineRow(bandRow[1]);
            var normal=Normal(walk,outer[0]);double a=walk.Depth.Count-outer[1],b=walk.Depth.Count-inner[1];
            Line("band"+i,new(normal.X*a,normal.Y*a),new(normal.X*b,normal.Y*b),true);
        }
        // The protected core is the last positive depth slot, not an assertion
        // that every white region has been discovered or completely filled.
        for(int i=0;i<walk.Sides;i++)
        {
            var a=Point(walk,i);var b=Point(walk,(i+1)%walk.Sides);
            Line("core",new(a.X/walk.Depth.Count,a.Y/walk.Depth.Count),new(b.X/walk.Depth.Count,b.Y/walk.Depth.Count),true);
        }
        int tip=walk.EdgeCount==0?0:walk.Edge(walk.EdgeCount-1)[1];
        scene.Dots.Add(new("head",Map(Point(walk,tip)),4));
        scene.Label("Dashed: source, core and gap rungs",223,375);

        scene.Label("A SPACE HAS TWO SIDES",563,72);
        string band=walk.BandCount>0?"band"+(walk.BandCount-1):"width";
        scene.Strokes.Add(new(band,new(454,110),new(680,110),true));
        scene.Strokes.Add(new(band,new(454,168),new(680,168)));
        foreach(int x in new[]{476,520,564,608,652})scene.Strokes.Add(new(band,new(x,110),new(x,168),true));
        scene.Label("earlier supporting line",565,99);scene.Label("next parallel supporting line",565,190);
        scene.Label("same gap unit at every rung",565,217);
        if(walk.BandCount>0)
        {
            var row=walk.Band(walk.BandCount-1);var a=walk.LineRow(row[0]);var b=walk.LineRow(row[1]);
            scene.Label($"normal {a[0]}: depth site {a[1]} → {b[1]}",565,242);
        }
        scene.Label("CHECK → CORNER → NEXT SIDE",565,291);
        scene.Label("last side → retain gap → CHECK",565,316);
        scene.Label(walk.Complete?"STOP: another positive inset cannot fit":walk.Fit.ReadSupport()!=2?"HOLD: the required read is unresolved":"feedback remains connected",565,344);
        var counts=walk.Depth.AuditCounts();
        scene.Label($"AVAILABLE {counts[0]}     PENDING {counts[1]}     RETAINED {counts[2]}",360,408);
        for(int i=0;i<walk.Depth.Count;i++)
        {
            double x=110+500.0*i/Math.Max(1,walk.Depth.Count-1);
            string text=i==walk.Depth.Pending?"P":i<walk.Depth.Frontier?"R":"·";
            scene.Label(text,x,435);
        }
        scene.Label("The body repeats. Its output grows; its instructions do not.",360,465);
        return scene;
    }
}
