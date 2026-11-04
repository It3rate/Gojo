using System.Numerics;
using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private ExplorerPage TimingSocketPage()
    {
        var p = new ExplorerPage("Can a test earn a socket?",
            "A factory test bench can move one timing pulse. A spare socket on its rack can keep a test that tells two dispensers or two border plotters apart.", "TimingSocket.md");
        string[] examples = ["Washer · return wire","Washer · spare stock","Border · drive swap","Border · frame flip",
            "Border · turn and feed","Border · rewind","Border · ink only"];
        Choose(p,"Apparatus",examples,Mode,n=>{Mode=n; Control=n==4?1:0;});
        string[] settings = ["Both directions","Earlier only","Later only","No edits","Protect material","Even split",
            "Inspect promises","Question settled","Short desk clock","Short bench clock","Low supplies",
            "Missing reference","Uncertain reference","Changing reference","First unknown missing","Which of the pair?"];
        Choose(p,"Bench mode",settings,Variant,n=>Variant=n);
        Choose(p,"Original pulse",["Last beat","First beat"],Control,n=>Control=n);
        Choose(p,"Fresh unknown",["Machine A","Machine B"],A-1,n=>A=n+1);
        Stepper(p,48,"Next event");
        p.Actions.Add(new("Run to end",()=>{ Steps=48; Refresh(); }));

        var (left,right,sensors) = Mode switch {0=>(5,11,4),1=>(1,2,7),3=>(0,4,7),4=>(1,6,4),6=>(0,1,1),_=>(0,1,2)};
        var domain = Mode < 2 ? TimingStands.Factory : Mode == 5 ? TimingStands.ReverseBorder : TimingStands.Border;
        int pair=(1<<left)|(1<<right);
        // Factory examples let the old desk narrow the full family. The plotter
        // is supplied a design pair, as is the explicitly pair-specific question.
        int prior=Mode<2 && Variant!=(int)SocketMode.PairQuestion ? domain.Family : pair;
        var s=SocketSurvey.Start(domain,prior,A==1?left:right,sensors,(SocketMode)Variant,Control==1);
        for (int i=0;i<Steps && s.Step();i++) { }
        var learned=s.Bench?.Retained;
        bool used=s.Trials.Any(t=>t.Row==WasherProbeBank.Feedback);
        string stage = used ? "Desk used the new socket" : learned.HasValue ? "Socket filled" :
            s.Stopped ? "Socket stays empty" : s.Checks.Count!=0 ? s.Checks[^1].Outcome :
            s.Bench!=null ? "Desk requested a timing test" : s.Trials.Count!=0 ? "Desk sampled the unknown" : "Old rack ready";
        p.Bits("Possible machines","One bit for each possible design at the desk.",LowBytes((uint)s.Desk.Live,2),domain.Types,"desk");
        p.Bits("Bench request","Two reference contacts leave the desk only when no installed, connected test can separate them.",
            LowBytes((uint)s.RequestedPair,2),domain.Types,"request");
        p.Bits("Socket scope","Empty until a repeat succeeds. Predictions in this row apply only to its two measured references.",
            LowBytes((uint)s.Rack.Scope(6),2),domain.Types,"socket");
        PulseEdit? edit=s.Checks.Count>0?s.Checks[^1].Edit:null;
        if (edit is {} e)
        {
            p.Bits("Original test","Request or main drive, return or accent drive, then sensor.",[e.Parent.Word],8,"parent");
            p.Bits("Edited test","One pulse moved into a neighboring empty beat on the same wire.",[e.Child.Word],8,"child");
        }
        p.Bits("Repeat law","Bit first + 4 × repeat admits only the same resolved, opposite pair on both runs.",
            LowBytes(TimingBench.RetentionLaw,2),16,"repeat");
        if (learned.HasValue)
        {
            p.Bits("Installed row","Program, measured ON contacts, and cases that used more than one unit.",
                LowBytes(s.Rack.Row(6),4),32,"socket");
        }
        p.Node("desk",$"Desk · {BitOperations.PopCount((uint)s.Desk.Live)} fit","Each remaining candidate agrees with the desk's samples.",114,55,true);
        p.Node("request",s.Bench==null?"Bench idle":"Bench requested","Carries two reference tickets, legal parent words and limits.",350,55,s.Bench!=null);
        p.Node("socket",used?"Socket used":learned.HasValue?"Socket filled":"Socket empty","One local slot for a retained test.",592,55,learned.HasValue);
        p.Edge("request","","Request surface",182,55,267,55,s.Bench!=null);
        p.Edge("socket","","Installation surface",435,55,514,55,learned.HasValue);
        if (Mode >= 2)
        {
            var parent=edit?.Parent??new WasherProbe((byte)((BitOperations.TrailingZeroCount((uint)sensors)<<6)|(Control==0?16:8)|3));
            p.Label("Reference A",235,102); p.Label("Reference B",505,102);
            void Swatches(WasherProbe program,int y,string key,string label)
            {
                p.Label(label,74,y);
                DrawSwatch(p,TimingStands.Swatch(left,program,Mode==5,3),program.Beats,235,y,key);
                DrawSwatch(p,TimingStands.Swatch(right,program,Mode==5,3),program.Beats,505,y,key);
            }
            Swatches(parent,165,"parent",edit.HasValue?"Original":"Original example");
            if (edit is {} child) Swatches(child.Child,267,"child","Edited");
            else p.Label("An edited reference swatch appears when a variant is proposed.",350,263);
            p.Label("Three uninterrupted repeats. The ring marks the first sample. Lengths are illustrative.",350,325);
        }
        else
        {
            p.Label(Mode==0?"Both machines release on a retry. A has a working return lead and B has a cut lead.":
                "Both machines remember delivery. A starts with one washer and B starts with two.",350,113);
            string Wire(WasherProbe? x) => x is {} w ? string.Join("  ",Enumerable.Range(0,w.Beats)
                .Select(i=>(w.Back & 1<<i)!=0?"open":"shut")) : "waiting";
            p.Node("parent","Original · "+Wire(edit?.Parent),"Return wire in chronological order.",350,174);
            p.Node("child","Edited · "+Wire(edit?.Child),"Same sampling beat, new timing.",350,239,edit.HasValue);
        }
        if (s.Checks.Count>0)
        {
            var c=s.Checks[^1];
            static string Read(TimingRead? r)=>r?.Support switch {0=>"missing",1=>"off",2=>"on",3=>"uncertain",_=>"unrun"};
            p.Label($"Last reference pair   A {Read(c.Left)}   /   B {Read(c.Right)}     ·     {c.Outcome}",350,369);
        }
        p.Result=$"{stage}. {BitOperations.PopCount((uint)s.Desk.Live)} possible design(s).";
        if (s.Trials.Count!=0) p.Events.Add("Desk sockets used in order: "+string.Join(" → ",s.Trials.Select(t=>t.Row)));
        if (used) p.Events.Add("Fresh unknown at the retained socket: "+(s.Trials.Last(t=>t.Row==6).Sample switch {
            1=>"off",2=>"on",0=>"missing",_=>"uncertain"}));
        else if (s.Stopped && s.Bench == null) p.Events.Add(s.Desk.Answered ? "The desk's question already has an answer." :
            "A bench request needs two live designs, time, and no distinguishing test on the installed wires.");
        if (s.Bench is {} bench) p.Events.Add($"Bench remaining: {bench.BeatsLeft} beats and {bench.UnitsLeft} {(Mode<2?"washers":"impressions")}.");
        p.Boundary="Fresh references on every run. A and B change order on the repeat. The unknown supplies no readings to the timing bench.";
        return p;
    }

    private static void DrawSwatch(ExplorerPage p,BorderEmission[][] trace,int sample,int cx,int cy,string key)
    {
        var points=new List<(int X,int Y)>{(0,0)};
        foreach (var beat in trace)
        {
            var (x,y)=points[^1];
            foreach (var e in beat)
                if(e.Move.Axis==0) x+=e.Move.Sense; else y-=e.Move.Sense;
            points.Add((x,y));
        }
        int xmin=points.Min(a=>a.X),xmax=points.Max(a=>a.X),ymin=points.Min(a=>a.Y),ymax=points.Max(a=>a.Y);
        double scale=Math.Min(24,Math.Min(155.0/Math.Max(1,xmax-xmin),55.0/Math.Max(1,ymax-ymin)));
        double dx=cx-scale*(xmax+xmin)/2,dy=cy-scale*(ymax+ymin)/2;
        p.Circle(key,"End of the measured first window. Later repeats are an illustrative continuation.",
            dx+scale*points[sample].X,dy+scale*points[sample].Y,4);
        for(int i=1;i<points.Count;i++)
        {
            var a=points[i-1];var b=points[i];
            p.Stroke(key,"One sampled pen displacement.",dx+scale*a.X,dy+scale*a.Y,dx+scale*b.X,dy+scale*b.Y,i>sample,i<=sample);
        }
    }
}
