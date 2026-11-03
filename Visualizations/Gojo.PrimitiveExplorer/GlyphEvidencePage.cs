using Avalonia;
using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private ExplorerPage GlyphEvidencePage()
    {
        string[] modes = ["Glyph clues","Y · merge","Y · branch","T · two corners","T · blocked stem","T · optional turn"];
        var p = new ExplorerPage("Glyph clues & junctions",
            "Hide clues to a letter or digit. Then send a pulse through two different uses of the same ink.","GlyphEvidence.md")
        {
            DrawingGuide = Mode == 0 ? ("What could this be?", "The reader sees only the listed clues. Each drawing on the right is one possible completion from the supplied hand.")
                : ("Read the joint from an entrance", "A pulse arrives along a named arm. Follow the arrows after settlement. A pulse with no available exit stays at its inlet.")
        };
        Choose(p,"Experiment",modes,Mode,n=>{ Mode=n; Variant=0; Control=0; A=0; },true);
        if (Mode == 0) ShowClues(p);
        else ShowJunction(p);
        return p;
    }

    private void ShowClues(ExplorerPage p)
    {
        var examples = GlyphEvidence.Observations;
        var body = examples[Variant].Body;
        string[] views = ["Keep five clues","Hide extension","Hide bowl side","Hide both axes","Hide tail bend","Loop & ends","Loop only","Hide all","Hide loop & extension"];
        Choose(p,"Observation",examples.Select(s=>s.Name).ToArray(),Variant,n=>{Variant=n; A=0;},true);
        Choose(p,"Evidence",views,Control,n=>{Control=n; A=0;});
        var full = body.Read();
        var read = Control switch
        {
            1 => full with { TailAbove=null }, 2 => full with { BowlRight=null },
            3 => full with { TailAbove=null,BowlRight=null }, 4 => full with { TailBends=null },
            5 => new(full.Loops,full.FreeEnds,null,null,null), 6 => new(full.Loops,null,null,null,null),
            7 => default, 8 => full with { Loops=null,TailAbove=null }, _ => full
        };
        var admitted = GlyphEvidence.Admitted(read);
        var names = admitted.Select(e=>e.Name).ToArray();
        void Clue(string title, int? value, int bits, string caption, double y)
        {
            if (value is int v) p.Bits(title,"Published observation.",[(byte)v],bits,title);
            p.Node(title,value.HasValue ? caption : title+"?",title,180,y,value.HasValue,!value.HasValue);
        }
        static int? Bit(bool? b) => b is bool value ? value ? 1 : 0 : null;
        Clue("Independent loops",read.Loops,2,$"{read.Loops} loop(s)",60);
        Clue("Free ends",read.FreeEnds,2,$"{read.FreeEnds} free end(s)",119);
        Clue("Free tail bends",Bit(read.TailBends),1,read.TailBends==true ? "Curved tail" : "Straight tail",178);
        Clue("Bowl right of joint",Bit(read.BowlRight),1,read.BowlRight==true ? "Bowl to right" : "Bowl to left",237);
        Clue("Tip above joint",Bit(read.TailAbove),1,read.TailAbove==true ? "Tail extends up" : "Tail extends down",296);
        void ShowOrder(LandmarkRun run, string axis, string key) =>
            p.Rows(axis+" witness","Ordered local ink sites on the supplied axis.",run.Bits,
                run.Sites().Select(site=>new[]{site}).ToArray(),key,actual:run.SiteBytes());
        if (read.BowlRight.HasValue) ShowOrder(body.Across!,"Horizontal","Bowl right of joint");
        if (read.TailAbove.HasValue) ShowOrder(body.Upward!,"Vertical","Tip above joint");
        if (read.EndCountRequiresLoop && !read.Loops.HasValue)
            p.Events.Add("The complete free-end count guarantees at least one loop.");
        if (names.Length == 0)
        {
            p.Node("open","No matching completion","The observation remains available.",518,185,uncertain:true);
            p.Result = "No supplied construction fits all the published clues.";
            p.Boundary = "The extra bowl introduces a second loop while preserving the tail and its marked right-side landmark.";
            p.Events.Add("Hide loop & extension to see which completions would have been proposed.");
            return;
        }
        int selected = Math.Min(A,names.Length-1);
        var name = names[selected];
        Choose(p,"Try completion",names,selected,n=>A=n,true);
        p.Label("One proposed completion",518,60);
        var proposal = admitted[selected].Body;
        var shape = proposal.Read();
        Point[]? sites = null;
        double reach = 1;
        var form = name;
        if (shape.TailAbove is bool above && shape.BowlRight is bool right)
        {
            var sign = right ? 1 : -1;
            if (shape.TailBends == true)
            {
                var rise = above ? -1 : 1;
                sites = [new(-40*sign,-30*rise), default, new(35*sign,85*rise),new(40*sign,-30*rise)];
                reach = .44;
            }
            else
            {
                var y = above ? 35 : -35;
                sites = [new(-40*sign,y-43),new(-40*sign,y+43),new(-40*sign,above ? -93 : 93),new(40*sign,y)];
                reach = .38;
            }
        }
        else if (shape.Loops == 2) { sites = [new(0,0),new(0,-83),new(0,83)]; reach = .7; }
        else form = shape.Loops == 1 ? "O" : "C";
        DrawPen(p,proposal.Ink,form,new(518,203),1,true,false,"proposal",tangentScale:reach,placement:sites);
        p.Rows("Proposed ink spans","These rows belong to the selected completion.",proposal.Ink.Catalogue.SiteBits,
            Enumerable.Range(0,proposal.Ink.InkCount).Select(proposal.Ink.Catalogue.Edge).ToArray(),"proposal",actual:proposal.Ink.Catalogue.EdgeBytes());
        p.Label("Candidate   "+name,518,332);
        p.Result = "Compatible in this hand: " + string.Join("  ·  ",names);
        p.Boundary = "The same round construction is supplied for o, 0 and O. A candidate contributes no new observations.";
        p.Events.Add("Change Try completion to inspect a different drawing against the same evidence.");
        if (!read.Loops.HasValue)
        {
            var possible = admitted.Select(e=>e.Body.Read().Loops).Distinct().Order().ToArray();
            p.Events.Add("Predicted loop count across compatible constructions: "+string.Join(" or ",possible)+".");
        }
    }

    private void ShowJunction(ExplorerPage p)
    {
        var merge = Mode is 1 or 3;
        var choice = Mode is 2 or 5;
        string[] starts = merge ? ["Left","Right","Both inlets"] : Mode == 4 ? ["Stem","Left"] : Mode == 2 ? ["Stem"] : ["Left"];
        Choose(p,"Arrival",starts,Variant,n=>Variant=n,true);
        if (choice) Choose(p,"Local gate",["Withheld","Clear","Set"],Control,n=>Control=n);
        var arrivals = merge ? Variant+1 : Mode == 2 || Mode == 4 && Variant == 0 ? 4 : 1;
        var initial = (byte)(arrivals | (choice && Control==2 ? 8 : 0));
        var joint = Mode switch
        {
            1 => GlyphJunction.YMerge(initial), 2 => GlyphJunction.YBranch(initial),
            3 => GlyphJunction.TCorners(initial), _ => GlyphJunction.TBar(initial,Mode==5)
        };
        var visible = choice && Control==0 ? 7 : 15;
        var support = joint.Flow.Predict(joint.Flow.Observe(visible));
        var actual = Steps>0 && visible==15;
        var after = actual ? joint.Advance() : initial;
        Stepper(p,1,"Trace passage");
        var armRows = Enumerable.Range(0,3).Select(joint.Arm).ToArray();
        p.Rows("Arms in port order","Left, right, stem. Each row binds an incoming run, span slot and reversal bit.",2,armRows,"joint",
            actual:joint.ArmBytes(),rowKeys:["arm0","arm1","arm2"]);
        foreach (var (table,row) in joint.Flow.Steps)
        {
            var kind = table==joint.Flow.Forward ? "Open" : table==joint.Flow.Absent ? "If clear" : "If set";
            var route = table.Wiring(row);
            p.Rows(kind + $" · {route[0]} to {route[2]}","Source arm, gate, destination arm.",2,[route],"joint");
        }
        p.Bits("Predicted whole states","Bit n admits state n after one passage. Low three state bits refer to arms. The fourth holds the gate.",
            [(byte)support,(byte)(support>>8)],16,"prediction");
        p.Bits("Occupied arms",actual ? "After settlement." : "At arrival.",[(byte)(after & 7)],3,"joint");
        Point[] sites = [new(215,Mode<=2 ? 70 : 175),new(370,175),new(525,Mode<=2 ? 70 : 175),new(370,314)];
        var centre = sites[joint.Centre];
        string[] arms = ["Left","Right","Stem"];
        for (var i=0;i<3;i++)
        {
            var at = sites[joint.Arrival(i)[0]];
            var k = "arm"+i;
            p.Stroke(k,"Underlying ink span.",at.X,at.Y,centre.X,centre.Y,false,false);
            var came = (arrivals & (1<<i))!=0;
            var arrived = actual && !came && (after & (1<<i))!=0;
            if (came || arrived)
            {
                var from = came ? at : centre; var to = came ? centre : at;
                p.Edge(k,"",came ? "Approach to the joint." : "Settled outgoing trace.",from.X,from.Y,to.X,to.Y,true);
            }
            p.Node(k,arms[i],$"Local port {i}.",at.X,at.Y+26,(after & (1<<i))!=0);
        }
        p.Circle("joint","Shared ink site "+joint.Centre,centre.X,centre.Y,5);
        string Occupied(int bits) => string.Join(" + ",arms.Where((_,i)=>(bits & (1<<i))!=0));
        var outcomes = Enumerable.Range(0,16).Where(i=>(support & (1u<<i))!=0).Select(i=>Occupied(i)).Distinct();
        p.Result = actual ? $"Arrival: {Occupied(arrivals)}. Settled occupancy: {Occupied(after)}."
            : "Admitted final occupancy: " + string.Join(" or ",outcomes) + ".";
        p.Boundary = merge ? "Both inlets address one outlet slot. The left passage gets the first opportunity when both arrive together."
            : "The arrows trace one admitted use of these ink spans. An occupied arm holds one pulse.";
        p.Events.Add(actual ? ((after & arrivals)!=0 ? "An arrival remains held at its inlet." : "All arrivals passed through the joint.")
            : visible==7 ? "The gate is withheld. Choose Clear or Set to run one actual trace." : "Trace passage to execute one local settlement.");
    }
}
