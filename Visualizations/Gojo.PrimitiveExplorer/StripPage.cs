using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private StripWalk? stripWalk;
    private (int Shape,int Form,int Depth,bool Incompatible)? stripKey;
    private readonly Observation stripWidth=new(2),stripCorner=new(2);
    private ExplorerPage StripPage()
    {
        var p=new ExplorerPage("Whitespace becomes a path",
            "One side cycle repeats inward. Paired lines share a gap; the loop stops while a positive central space remains.","WhitespaceStrips.md");
        Choose(p,"Shape",["Triangle","Square","Pentagon"],Variant,n=>Variant=n);
        Choose(p,"Form",["Spiral","Perimeters"],Mode,n=>Mode=n);
        Choose(p,"Depth chain",["8 steps","4 steps","12 steps","24 steps"],Control,n=>Control=n);
        p.Choices.Add(new("Read",["Known","Gap hidden","Corner open","Units differ"],B,n=>
        {
            if((B==3)!=(n==3))Steps=0;
            B=n;Refresh();
        }));
        stripWidth.Support=B==1?(byte)3:(byte)2;stripCorner.Support=B==2?(byte)0:(byte)2;
        var key=(Variant,Mode,Control,B==3);
        if(stripWalk is null || stripKey!=key || Steps<stripWalk.Visits)
        { stripWalk=new(Variant+3,new[]{8,4,12,24}[Control],Mode==1,stripWidth,stripCorner,B!=3);stripKey=key; }
        var walk=stripWalk;
        while(walk.Visits<Steps && !walk.Complete)if(!walk.Step().Drew)break;
        p.Actions.Add(new("Next side",()=>{if(!walk.Complete)walk.Step();Steps=walk.Visits;Refresh();}));
        p.Actions.Add(new("Run to stop",()=>{walk.Run();Steps=walk.Visits;Refresh();}));
        p.Actions.Add(new("Reset",()=>{stripWalk=null;Steps=0;Refresh();}));
        ShowHintScene(p,StripDrawing.Draw(walk));
        var counts=walk.Depth.AuditCounts();
        string state=walk.Complete?"Stopped at the positive core.":walk.Fit.ReadSupport()==2?"Next side permitted.":"Waiting for a compatible read.";
        p.Result=$"{state} {walk.EdgeCount} drawn sides. Depth: {counts[0]} available / {counts[1]} pending / {counts[2]} retained.";
        p.Bits("Available depth","Each occupied slot is one shared, unmeasured width unit. The last positive slot is protected.",walk.Depth.BankBytes(0),walk.Depth.Count,"core");
        p.Bits("Pending strip","A pass reserves one gap before drawing. An interrupted pass keeps it here.",walk.Depth.BankBytes(1),walk.Depth.Count,"pending");
        p.Bits("Retained strips","The final corner transfers the reserved gap here. These are depth intervals, not area material.",walk.Depth.BankBytes(2),walk.Depth.Count,"retained");
        p.Rows("Reusable body","[Current line role, next line role]. The last target closes this pass or enters the next inward frame.",StripWalk.TemplateBits,
            Enumerable.Range(0,walk.Sides).Select(walk.Body).ToArray(),"body",actual:walk.BodyBytes());
        p.Rows("Frame bindings","[Line role, normal direction, depth role]. Depth role 0 is this pass; 1 is one shared step inward.",StripWalk.TemplateBits,
            Enumerable.Range(0,walk.FrameCount).Select(walk.Frame).ToArray(),"body",actual:walk.FrameBytes());
        p.Bits("Equal turn construction","All contour arcs reuse one positive part of the same closed whole. A clock by itself supplies no angle.",walk.Contour.ArcBytes(),walk.Sides*18,"turns");
        p.Bits("Turn closure","The unwrapped contour end returns to its actual reference site.",walk.Contour.ClosureBytes(),12,"turns");
        p.Bits("Normal bindings","[Phase activation site, contour ray site]. Each supporting line uses its own normal direction.",walk.NormalBytes(),walk.Sides*12,"turns");
        p.Bits("Unit correspondence","[Parameter role, unit reference]. Gap and depth must share this basis.",walk.UnitBytes(),8,"width");
        p.Bits("Current fit read","Support bits admit false / true. Both means unresolved; no bits means a required read is unavailable.",[walk.Fit.ReadSupport()],2,"check");
        p.Rows("Supporting lines","[Normal direction, inward depth site]. Matching normal references establish parallelism in this selected geometry.",walk.ReferenceBits,
            Enumerable.Range(0,walk.LineCount).Select(walk.LineRow).ToArray(),"lines",actual:walk.LineBytes());
        p.Rows("Whitespace strips","[Earlier support line, next support line, shared width]. Adjacent depth sites are separated by one unit.",walk.ReferenceBits,
            Enumerable.Range(0,walk.BandCount).Select(walk.Band).ToArray(),"bands",actual:walk.BandBytes(),
            rowKeys:Enumerable.Range(0,walk.BandCount).Select(i=>"band"+i).ToArray());
        p.Rows("Shared corner sites","[Incoming support line, outgoing support line]. A single row is the site shared by both incident edges.",walk.ReferenceBits,
            Enumerable.Range(0,walk.JointCount).Select(walk.JointRow).ToArray(),"joints",actual:walk.JointBytes());
        p.Rows("Retained ink","[Start joint, end joint, support line]. These are generated outputs, not additional loop instructions.",walk.ReferenceBits,
            Enumerable.Range(0,walk.EdgeCount).Select(walk.Edge).ToArray(),"ink",actual:walk.InkBytes(),
            rowKeys:Enumerable.Range(0,walk.EdgeCount).Select(i=>"edge"+i).ToArray());
        p.Rows("Non-inking relocation","Closed perimeters restart at the next inner corner. These movements do not join the rings with ink.",walk.ReferenceBits,
            Enumerable.Range(0,walk.LiftCount).Select(walk.Lift).ToArray(),"lifts",actual:walk.LiftBytes());
        p.Bits("Depth passages","Prewired available → pending → retained transfers. The protected last slot has no outgoing transfer.",
            walk.Depth.PassageBytes(),(walk.Depth.Count-1)*6*walk.Depth.Bits,"depth");
        p.Bits("Passage law","Eight bytes list allowed before/after states for one conserved handoff.",walk.Depth.LawBytes(),64,"depth");
        p.Bits("Continue route","Check → commit when the public fit read is certainly true.",walk.Circuit.PackedRows(walk.PresentTable),9,"check");
        p.Bits("Stop route","Check → terminal when the public fit read is certainly false.",walk.Circuit.PackedRows(walk.AbsentTable),9,"check");
        p.Bits("Feedback route","Commit → check. This same row is reused for every side and every pass.",walk.Circuit.PackedRows(walk.FeedbackTable),9,"body");
        p.Bits("Phase connections","The final phase reconnects to the first. There is no stored repeat count.",walk.Phase.Circuit.PackedRows(walk.Phase.Table),walk.Sides*18,"turns");
        p.Bits("Actual phase","One activation selects the next side. The last bit is its observed enable guard.",walk.Phase.Circuit.ValueBytes(),walk.Sides+1,"turns");
        p.Bits("Actual controller","Check, commit, done, fit mirror and unconditional guard.",walk.Circuit.ValueBytes(),5,"check");
        p.Events.Add(walk.ClosedPass?"Each completed pass returns to the same joint. A non-inking relocation begins the next perimeter.":"The last corner reaches the next inward support line. Its actual joint starts the next pass.");
        p.Events.Add("Hide the gap or open a corner to pause; restore Known to resume the same construction.");
        p.Events.Add("The final positive core remains. Stopping does not claim that every possible shorter continuation has been exhausted.");
        p.Boundary="Selected regular triangle / square / pentagon law. Spacing is between paired supports; arbitrary outlines, rounded corners and general collision discovery remain open.";
        return p;
    }
}
