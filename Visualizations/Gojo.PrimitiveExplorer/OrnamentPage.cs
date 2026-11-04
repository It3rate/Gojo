using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private (OrnamentDesign Design,int Rhythm,int Join)? ornamentKey;
    private OrnamentImpression? ornamentPrint;
    private static readonly string[] ToolNames=["Ground","First arm","Second arm","Finishing tool"];

    private ExplorerPage OrnamentPage()
    {
        var design=Mode==2 && A==2?OrnamentPlates.PairedKey:OrnamentPlates.All[Mode];
        var p=new ExplorerPage("Ornament workshop",
            "Two pen arms share a fork and an eye. Change which arms go to work and what must arrive before the finishing tool can draw.", "OrnamentWorkshop.md");
        Choose(p,"Line plate",OrnamentPlates.All.Select(d=>d.Name).ToArray(),Mode,n=>Mode=n,true);
        Choose(p,"Rhythm",["Plate rhythm","Original timing","Earned timing","Both arms","Alternate arms","First only","Second only",
            "Neither arm","Which arm?","One pending","Missing punch"],Variant,n=>Variant=n);
        Choose(p,"Finish when",["Either arrives","Both arrive","Exactly one arrives"],Control,n=>Control=n);
        p.Choices.Add(new Choice("Inspect",["Full plate","One repeat","Show joints"],B,n=>{ B=n; Refresh(); }));
        if (ornamentKey!=(design,Variant,Control))
        {
            ornamentPrint=new(design,Variant,new byte[]{14,8,6}[Control]);
            ornamentKey=(design,Variant,Control);
        }
        var print=ornamentPrint!;
        int visible=Steps==0?print.Passes:Math.Min(Steps,print.Passes);
        p.Actions.Add(new("Next pass",()=>{ Steps=Steps==0?1:Math.Min(visible+1,print.Passes); Refresh(); }));
        p.Actions.Add(new("Next repeat",()=>
        {
            int start=Steps==0?0:visible;
            if (start<print.Passes)
            {
                int cell=print.Timeline[start].Cell;
                while (start<print.Passes && print.Timeline[start].Cell==cell) start++;
            }
            Steps=start; Refresh();
        }));
        p.Actions.Add(new("Full plate",()=>{Steps=0;Refresh();}));
        if (design.HookScore!=null)
            p.Actions.Add(new(A==2?"Separate steps":"Pair steps",()=>{A=A==2?1:2;Steps=0;Refresh();}));
        var pass=print.Timeline[visible-1];
        var witness=pass.Witness;
        var view=print.Draw(visible,B==1);
        foreach (var path in view.Paths)
        {
            string key=path.Nested?"inlay":"tool"+path.Tool;
            string detail=$"Repeat {path.Cell+1}. {(path.Nested?"Window flower · ":"")}{ToolNames[path.Tool]} drew {path.Strokes} local strokes.";
            p.Drawing.Add(new DrawingItem("ink",key,"",detail,0,0,Active:path.Tool!=0)
                {PathData=path.Path,LineWidth=B==1?1.65:path.Nested?0.95:path.Tool==0?0.9:1.34});
            p.Details[key]=detail;
        }
        if (B==2)
            foreach(var (fork,eye) in view.Landmarks)
            {
                p.Circle("fork","Fork site · both routes start at this local reference.",fork.X,fork.Y,3.3);
                p.Circle("eye","Shared arrival site · a read here can enable a third tool.",eye.X,eye.Y,4.5);
            }
        p.Label(design.Name+"   /   "+(B==1?$"repeat {pass.Cell+1}":$"{design.Count} places on the ground"),360,13);
        string stage=pass.Stage switch {0=>"ground",1=>"branch strokes",_=>"finish and join"};
        p.Label($"Pass {visible} of {print.Passes}   ·   repeat {pass.Cell+1}   ·   "+
            (pass.ChildCell<0?"":$"inner petal {pass.ChildCell+1}   ·   ")+stage,360,437);
        var tape=print.Tape;
        var packed=new byte[(tape.Length+1)/2];
        for(int i=0;i<tape.Length;i++) packed[i/2]|=(byte)(tape[i]<<(4*(i%2)));
        p.Bits("Repeat drum","From low to high, the four bits admit neither, first, second and both.",packed,4*tape.Length,"fork");
        p.Bits("Outer eye law","Truth bit A + 2B requests the outer finishing tool for that pair of arrivals.",[print.Join],4,"tool3");
        p.Bits("Pair at this pass","Actual drum field for this outer cell or inner petal.",[witness.Pair],4,"fork");
        if(pass.Stage>=1)
            p.Bits("Arms that drew","Two independently supplied pen activations. Each set bit records a settled ink branch.",[witness.Arms],2,"eye");
        if(pass.Stage==2)
        {
            p.Bits("Predicted / witnessed permission","Two ODV support bits per field. A joint prediction can outpace the actual arrivals.",
                [(byte)(witness.Prediction|(witness.Arrival<<2))],4,"tool3");
        }
        foreach(int tool in Enumerable.Range(0,4))
        {
            int arity=design.RowCount(tool,4)>0?4:2;
            var row=design.Stroke(tool,arity,0);
            p.Bits(ToolNames[tool]+" · first "+(arity==2?"line":"curve"),
                "Local site references in pen order. A cubic has two intermediate handles.",row.Select(r=>(byte)r).ToArray(),8*arity,"tool"+tool);
        }
        if(design.Inlay is {} insert)
        {
            p.Bits("Window socket","The parent finisher starts a child at this local site.",[(byte)insert.Anchor],8,"inlay");
            p.Bits("Child join law","The flower's finishing tool requires both petal flanks.",[8],4,"inlay");
        }
        if(design.Rim is {} rim)
            p.Bits("Equal rim divisions","All arc rows reuse one part reference and the end is bound to the start.",
                rim.ArcBytes(),rim.PartCount*3*rim.ReferenceBits,"fork");
        if(design.HookScore is {} motor)
            p.Bits("Hook commit clock","The same two four-move strips stamp axes separately or together.",motor.ClockBytes(),motor.ClockLength*2,"tool1");
        if(Variant is 1 or 2)
            p.Bits("Timing socket","Parent and retained child words from the previous plotter experiment.",
                [OrnamentPress.Timing.Parent.Word,OrnamentPress.Timing.Child.Word],16,"fork");
        string state=visible==print.Passes && !print.Cells[^1].Finished?"Waiting at an unresolved repeat.":
            visible==print.Passes?"Impression complete.":"Press in motion.";
        p.Result=$"{state} {view.Paths.Sum(x=>x.Strokes)} visible pen strokes. {design.Name}.";
        p.Events.Add(design.Instruction);
        if (visible==print.Passes && !print.Cells[^1].Finished)
            p.Events.Add("Settled marks remain. The drum waits for the unresolved tool before advancing.");
        else if (Steps==0) p.Events.Add("Next pass starts the press. One repeat enlarges the last visible cell.");
        p.Boundary="Each tool owns its activation. A shared eye can trigger one finishing pass. A crossing elsewhere only shares a position on the drawing.";
        return p;
    }
}
