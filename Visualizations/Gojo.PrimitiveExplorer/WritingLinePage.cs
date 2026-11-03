using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private ExplorerPage WritingLinePage()
    {
        var p = new ExplorerPage("One stem, two inputs","Collect two arrivals at a Y, place i and j on one writing line, or change the upper joint of a 4.","WritingLine.md")
        {
            DrawingGuide = Mode switch
            {
                0 => ("Give the Y both arrivals","Trace one beat. Compare what the stem reports with the contents still held at the inlets and receiving slots."),
                1 => ("Which stem owns the dot?", "The stems share an upright writing frame. Dashed links assign separate marks. Select a stem, then hide a clue or change the assignment."),
                _ => ("Two ways to write 4","Follow the slant to the elbow, then across the upright. Hide the upper joint to see which constructions still fit.")
            }
        };
        Choose(p,"Experiment",["Y · two inlets","i and j","Two forms of 4"],Mode,n=>{Mode=n;Variant=Control=B=0;A=3;},true);
        if (Mode==0) ShowCollector(p);
        else if (Mode==1) ShowWritingLine(p);
        else ShowFour(p);
        return p;
    }

    private void ShowCollector(ExplorerPage p)
    {
        Choose(p,"Connection",["Collect both · OR","One receiving slot","Select an inlet"],Variant,n=>{Variant=n;Control=0;});
        Choose(p,"Arrivals",["Neither","Left","Right","Both"],A,n=>A=n,true);
        if (Variant==2) Choose(p,"Switch",["Left","Right","Withheld"],Control,n=>Control=n);
        byte initial = (byte)(A | (Variant==2 && Control==1 ? 8 : 0));
        var y = Variant switch { 0 => GlyphJunction.YCollect(initial), 1 => GlyphJunction.YMerge(initial), _ => GlyphJunction.YSelectInlet(initial) };
        var unknown = Variant==2 && Control==2;
        var after = Steps>0 && !unknown ? y.Advance() : y.Before;
        Stepper(p,1,"Trace passage");
        p.Rows("Ink arm bindings","Left inlet, right inlet and stem. Fields are run, span and reverse-on-arrival.",2,
            Enumerable.Range(0,3).Select(y.Arm).ToArray(),"joint",actual:y.ArmBytes(),rowKeys:["left","right","stem"]);
        foreach (var (table,row) in y.Flow.Steps)
        {
            var route=table.Wiring(row);
            string gate = table==y.Flow.Forward ? "Open" : table==y.Flow.Absent ? "If clear" : "If set";
            p.Rows(gate+" · inlet "+route[0],"Source, gate, receiving slot.",row.ReferenceBits,[route],"joint");
        }
        if (y.Collection is {} reads && y.OutletBinding is {} binding)
        {
            var loc=reads.LocationOf(binding[1]);
            p.Rows("Public stem read","Two imported receiving slots and their calculated output.",2,[reads.Wiring(loc.Table,loc.Row)],"stem",actual:reads.PackedRows(loc.Table,2));
            p.Rows("Outlet binding","Publish the calculated read on ink arm 2.",2,[binding],"stem",actual:y.OutletBytes());
        }
        string[] armNames=["Left","Right"];
        for(int i=0;i<2;i++)
        {
            int x=185+i*320;
            string k=i==0 ? "left" : "right";
            p.Edge(k,"","Input span toward the joint.",x,100,345,193,(A & (1<<i))!=0);
            p.Node(k,armNames[i],(after & (1<<i))==0 ? "Empty inlet." : "Occupied inlet.",x,78,(after & (1<<i))!=0);
        }
        var stem = y.Stem.ReadSupport()==2;
        if (y.Collection!=null)
        {
            for (var i=0;i<2;i++)
            {
                int x=267+i*156;
                p.Stroke("joint","Local transfer to receiving slot.",345,193,x,239,false);
                p.Stroke("stem","Non-draining read of this slot.",x,239,345,306,true);
                int slot=i==0 ? 2 : 4;
                p.Node("slot"+slot,"Slot "+slot,"Stored contribution.",x,239,(after & (1<<slot))!=0);
            }
        }
        else p.Stroke("stem","One physical receiving slot.",345,193,345,306,false);
        p.Node("stem","Stem now "+(stem ? 1 : 0),"Current participation before or after the selected beat.",345,315,stem);
        p.Node("joint",unknown ? "Gate ?" : "Joint","The private passage law controls transfers.",345,193,false,unknown);
        static string Occupied(int n) => (n & 3) switch {0=>"neither",1=>"left",2=>"right",_=>"both"};
        p.Bits("Occupied sites",unknown ? "Only the three arm sites are published." : "Packed private state, including the control site.",
            [(byte)(unknown ? after & 7 : after)],unknown ? 3 : y.Flow.Ports.Length,"joint");
        foreach (var table in y.Flow.Steps.Select(s=>s.Table).Distinct())
        {
            string condition=table==y.Flow.Forward ? "Open" : table==y.Flow.Absent ? "Clear gate" : "Set gate";
            p.Bits(condition+" transition law","Eight bytes encode allowed before and after states of a passage.",table.LawBytes(),64,"joint");
        }
        if (y.Collection is {} collection)
            p.Bits("Presence law","Allowed input / input / output triples in compatible O/D contexts.",collection.LawBytes(0),512,"stem");
        p.Result = $"Stem reads {(stem ? 1 : 0)}. Occupied inlets: {Occupied(after)}. Store {after:X2}.";
        if (unknown)
        {
            var predicted=y.Flow.Predict(y.Flow.Observe(7));
            var possible=Enumerable.Range(0,16).Where(i=>(predicted&(1u<<i))!=0);
            var outcomes=possible.Select(i=>$"stem {(i>>2)&1}, waiting {Occupied(i)}").Distinct();
            p.Result="After the withheld switch: "+string.Join(" or ",outcomes)+".";
        }
        p.Boundary = "A present bit has no calibrated amount. Each accepted arrival occupies one receiving slot.";
        p.Events.Add(unknown ? "Choose a switch position to settle an actual arrival." : Steps==0 ? "Trace passage to settle the supplied inputs." :
            y.Collection!=null ? "The stem observes the two receiving slots. Reading it leaves both available." : "A pulse remains at an inlet if the chosen passage cannot accept it.");
    }

    private void ShowWritingLine(ExplorerPage p)
    {
        Choose(p,"Focus",["Left stem","Right stem"],Variant,n=>Variant=n,true);
        Choose(p,"Evidence",["Line and dots","Hide baseline","Hide membership","Hide both","Dots and hook"],Control,n=>Control=n);
        Choose(p,"Assignments",["Each gets its dot","Right unassigned","Swap dots"],B,n=>B=n);
        var page=new WritingLine(B!=1,B==2);
        var line=Control is 0 or 2;
        var grouping=Control is 0 or 1 or 4;
        var hook=Control==4;
        var read=page.Read(Variant,grouping,line,hook);
        p.Rows("Ink stems","Two ordered spans. The dots do not appear in their endpoints.",page.Stems.SiteBits,
            [page.Stems.Edge(0),page.Stems.Edge(1)],"ink",actual:page.Stems.EdgeBytes());
        p.Rows("Point marks","Two independent ink points.",3,page.DotSites().Select(d=>new[]{d}).ToArray(),"dots",actual:page.DotBytes());
        if (grouping) p.Rows("Dot assignments","Stem head, assigned dot site.",3,page.Membership(),"dots",actual:page.MembershipBytes());
        if (line)
        {
            p.Rows("Shared writing frame","Baseline and mid-height on the ordered level run.",2,[page.UprightFrame],"line");
            p.Rows("Levels in upward order","Spacing is unmeasured.",2,page.Levels.Sites().Select(v=>new[]{v}).ToArray(),"line",actual:page.Levels.SiteBytes());
            p.Rows("Ink site projections","Row position is ink site. Its value is the page level.",2,
                Enumerable.Range(0,6).Select(i=>new[]{page.Level(i)}).ToArray(),"line",actual:page.LevelBytes());
        }
        if (hook) p.Rows("Steering","Ink span and steering word.",page.Ink.WireBits,page.Ink.DriveLinks(),"ink",actual:page.Ink.DriveBytes());
        double Height(int level) => 315-80*Array.IndexOf(page.Levels.Sites(),level);
        double X(int site) => site is 0 or 1 or 4 ? 220 : 490;
        if(line)
        {
            var y=Height(page.UprightFrame[0]);
            p.Stroke("line","Common reference for both feet.",115,y,605,y,true);
            p.Label("baseline",590,y+13);
        }
        foreach(var dot in page.DotSites()) p.Circle("dots","Point mark "+dot,X(dot),Height(page.Level(dot)),6);
        if(grouping) foreach(var row in page.Membership())
            p.Stroke("dots","Grouping correspondence.",X(row[0]),Height(page.Level(row[0])),X(row[1]),Height(page.Level(row[1])),true);
        for(int i=0;i<2;i++)
        {
            var edge=page.Stems.Edge(i);
            var x=X(edge[0]); var top=Height(page.Level(edge[0])); var foot=Height(page.Level(edge[1]));
            // On the axial read the bend is withdrawn, so draw only the vertical projection.
            if(hook && page.Read(i,showBend:true).Bends==true)
                p.Drawing.Add(new("curve","ink","","One rendering of the turn.",x,top,x-36,foot,i==Variant)
                    {PathData=FormattableString.Invariant($"M {x} {top} C {x} {foot-15} {x} {foot} {x-36} {foot}")});
            else p.Stroke("ink","Vertical projection of the stem.",x,top,x,foot,false,i==Variant);
        }
        var labels=WritingLine.Candidates(read);
        var dotRead=read.Dotted switch {true=>"dot attached",false=>"no assigned dot",_=>"dot ownership open"};
        p.Result=dotRead+" · "+(labels.Length>0 ? "compatible dotted forms: "+string.Join(" or ",labels) : "neither dotted model fits")+".";
        p.Events.Add("The owner's two strokes and two point marks form "+page.InkComponents+" ink components in every assignment.");
        p.Boundary="The picture shows the page. Only published clues enter the comparison. A withheld dot assignment leaves dotted identity provisional.";
    }

    private void ShowFour(ExplorerPage p)
    {
        Choose(p,"Hand",["Open 4","Closed 4"],Variant,n=>Variant=n,true);
        Choose(p,"Read",["Runs and upper joint","Hide upper joint","Only loop count"],Control,n=>Control=n);
        var ink=FourHands.Build(Variant==1);
        var features=new StrokeFeatures(ink);
        var loops=features.Loops.Count;
        var names=Control==2 ? FourHands.ByLoops(loops) : FourHands.Candidates(ink,Control==0);
        if(Control!=1)
        {
            p.Bits("Independent ink loops","Count on the complete source graph.",[(byte)loops],1,"loop");
            p.Node("loop",loops+" closed walk(s)","Count of independent loops.",350,340,loops>0);
        }
        if(Control!=2)
        {
            var surface=Control==1 ? FourHands.ReleaseFirstTip(ink) : ink;
            for(int i=0;i<surface.RunCount;i++)
            {
                var run=surface.Run(i);
                p.Rows("Run "+i,"Sites in order on this span.",run.Bits,run.Sites().Select(s=>new[]{s}).ToArray(),"run"+i,actual:run.SiteBytes());
            }
            var closed=ink.Run(0).Sites()[0]==ink.Run(2).Sites()[0];
            var top=Control==1 ? 124 : closed ? 62 : 85;
            var startX=410-(top-62);
            p.Stroke("run0","Slant to the elbow.",startX,top,255,217,false);
            p.Stroke("run1","Bar through the upright.",255,217,480,217,false);
            p.Stroke("run2","Upright through the bar.",410,Control==1 ? 124 : closed ? 62 : 90,410,298,false);
            if(Control==1) p.Label("Upper correspondence withheld",350,87);
            if(Control==0 && loops>0)
            {
                var walk=features.Loops[0];
                p.Rows("Closed walk","Run, slot, reversal.",walk.Bits,walk.Walk.Select(s=>new[]{s.Run,s.Slot,s.Reversed ? 1 : 0}).ToArray(),"loop",actual:walk.Bytes());
            }
        }
        else p.Label("Only closure was published",350,170);
        p.Result="Compatible constructions: "+string.Join(" · ",names)+".";
        p.Boundary="Open 4 and closed 4 share a supplied digit label. A and T are comparison forms.";
        p.Events.Add(Control==1 ? "The cut view forgets whether the two upper ends coincide. The owner's ink is unchanged." :
            Control==2 ? "Other shapes have the same count. The reader has no run evidence here." : "Sharing one upper ink site closes the triangle.");
    }
}
