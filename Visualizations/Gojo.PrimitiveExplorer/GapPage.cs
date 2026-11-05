using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private GapStudy? gapStudy;
    private (int Shape,int Budget,int Wiring)? gapKey;
    private void HintGaps(ExplorerPage p)
    {
        Choose(p,"Shape",["Fret","Triangle","Square","Pentagon","Hexagon","Octagon"],Variant,n=>Variant=n);
        Choose(p,"Step basis",["Divisible budget","One extra step","Finer extra step"],Control,n=>Control=n);
        Choose(p,"Connections",["All pairs","Neighbors only","First gap unseen","First gap isolated"],B,n=>B=n);
        var key=(Variant,Control,B);
        if(gapStudy is null || gapKey!=key || Steps<gapStudy.Pool.Visits){gapStudy=new(Variant,Control,B);gapKey=key;}
        var study=gapStudy;var pool=study.Pool;
        while(pool.Visits<Steps && !pool.Quiet)pool.Step();
        p.Actions.Add(new("One comparison",()=>{if(!pool.Quiet)pool.Step();Steps=pool.Visits;Refresh();}));
        p.Actions.Add(new("Settle",()=>{pool.Settle();Steps=pool.Visits;Refresh();}));
        p.Actions.Add(new("Reset",()=>{gapStudy=null;Steps=0;Refresh();}));
        ShowHintScene(p,GapDrawing.Draw(study));
        var audit=pool.Audit();
        string outcome=audit.Even?"All gap chains equal.":audit.WithinOne?"Gaps differ by at most one step.":
            audit.Spread is null?"Whole-spacing read unresolved.":$"Gap spread {audit.Spread} steps.";
        p.Result=$"{(pool.Quiet?"Local scan quiet.":"Settlement available.")} {outcome} {pool.Moves} transfers / {pool.Visits} comparisons.";
        for(int i=0;i<pool.Count;i++)
            p.Bits($"Gap {i} · step slots","Each one is an equal share of SPACE. Trailing zero slots are unused storage capacity. Positive gap participation is preserved.",
                pool.GapBytes(i),pool.Capacity,"gap"+i);
        p.Rows("Whitespace attachments",study.Sketch is null?"[Contour part reference, gap chain]. Each arc has its own budget.":
            "[Rail, earlier landmark, later landmark, gap chain]. Rail 0 is across and rail 1 is rise.",study.BindingBits,
            Enumerable.Range(0,pool.Count).Select(study.Binding).ToArray(),"gaps",actual:study.BindingBytes(),
            rowKeys:Enumerable.Range(0,pool.Count).Select(i=>"gap"+i).ToArray());
        p.Bits("Common step bindings","[Gap, shared unit reference]. Comparing raw chain lengths requires the same step basis.",pool.UnitBytes(),6*pool.Count,"units");
        p.Bits("One parent step","This many equal fine steps correspond to one parent step. Selecting a finer basis starts a new calibrated run.",
            study.Subdivision.Bytes(),study.Subdivision.Count,"units");
        p.Bits("Parent to fine-run binding","Parent unit 0 binds to run 2, the whole ordered run of fine unit 1 uses.",study.CalibrationBytes(),4,"units");
        p.Bits("Fine unit uses","Each two-bit reference names the same fine unit 1. Their repeated participation makes one parent step.",
            study.FineUseBytes(),study.Subdivision.Count*2,"units");
        p.Rows("Permitted gap pairs","Only these pairs can exchange a step. An incomplete graph may become quiet before the spacing is even.",3,
            Enumerable.Range(0,pool.PairCount).Select(pool.Pair).ToArray(),"pairs",actual:pool.PairBytes());
        p.Bits("Excess truth table","Bit A + 2B. A is donor slot k+1 and B is receiver slot k. Only A=1, B=0 requests an exchange.",[GapSettlement.ExcessTruth],4,"excess");
        p.Bits("Combine excess witnesses","OR the shifted slot comparisons. This compares prefix chains without computing an average.",[GapSettlement.AnyTruth],4,"excess");
        p.Bits("Transfer law","The existing eight-byte whole-step handoff preserves source plus receiver participation.",pool.LawBytes(),64,"transfer");
        if(pool.Last is {} last)
        {
            p.Bits("Last pair read","Forward and backward request supports. Each two-bit field admits absent, present, or both.",
                [(byte)(last.Forward|(last.Backward<<2))],4,"excess");
            p.Bits("Shifted slot references","[Donor slot k+1, receiver slot k] for the last pair's forward comparison.",
                pool.DemandBytes(last.Row),(pool.Capacity-1)*2*pool.ReferenceBits,"excess");
            p.Events.Add(last.Moved?$"Gap {last.Donor} gave one step to gap {last.Receiver}. Both bits changed in one passage.":
                $"Compared gaps {last.First} and {last.Second}. No permitted transfer on this visit.");
        }
        p.Bits("Transfer witnesses","Derived [occupied donor tip, observed guard, empty receiver tip] rows specialize existing gap-pair routes.",
            pool.TransferBytes(),pool.Moves*3*pool.ReferenceBits,"transfer");
        p.Bits("Comparison cursor","One activation visits the stored pair rows in order.",pool.Scan.Circuit.ValueBytes(),pool.PairCount+1,"pairs");
        p.Bits("Stop circuit","A complete scan with no transfer moves the controller to its terminal state.",pool.Stop.ValueBytes(),3,"stop");
        if(study.Sketch is null)p.Events.Add(study.RegularNow?"Equal arc budgets now give a regular polygon on the shared circle.":"The circular construction stays closed while its arc budgets settle.");
        else p.Events.Add("One common step basis spans both rails. Width and height may change while their combined step budget stays fixed.");
        p.Events.Add(Control==2?$"Each parent step contains {study.Subdivision.Count} smaller steps. The original space budget is unchanged.":
            "All space remains in the gap chains, including an indivisible extra step.");
        p.Boundary="This balances declared rail or arc gaps. It does not find all empty regions or optimize distances between arbitrary curves. Quiet is a local observation, not a global success flag.";
    }
}
