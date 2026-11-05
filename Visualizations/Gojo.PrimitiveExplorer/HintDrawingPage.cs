using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private ExplorerPage HintDrawingPage()
    {
        var p=new ExplorerPage("Draw from hints",
            "Order and shared references hold a shape together. One-bit chains decide how many places are visited and which leave marks.","DrawingFromHints.md");
        Choose(p,"Construction",["Rail fret","Rail rectangle","Circle marks","Two gate streams"],Mode,n=>{Mode=n;Variant=Control=Steps=0;},true);
        if(Mode<2) HintRails(p);
        else if(Mode==2) HintCircle(p);
        else HintStreams(p);
        p.Actions.Add(new("Vary drawing",()=>{A=A%9+1;Refresh();}));
        return p;
    }
    private static void ShowHintScene(ExplorerPage p,HintScene scene)
    {
        foreach(var s in scene.Strokes) p.Stroke(s.Key,"A numerical drawing of the connected references. Hover its encoding for the structural promise.",s.From.X,s.From.Y,s.To.X,s.To.Y,s.Guide);
        foreach(var d in scene.Dots) p.Circle(d.Key,"A named site or a construction guide.",d.At.X,d.At.Y,d.Radius,d.Guide);
        foreach(var t in scene.Captions) p.Label(t.Text,t.At.X,t.At.Y);
    }
    private void HintRails(ExplorerPage p)
    {
        bool rectangle=Mode==1;
        Choose(p,"Hints",["All connected","Order open","Turn basis open",rectangle?"Seam open":"Gap open","Order conflict"],Variant,n=>Variant=n);
        Choose(p,"Repeat strip",["111111","101101"],Control,n=>Control=n);
        var frieze=HintExamples.Frieze(rectangle,Variant,Control==1);var sketch=frieze.Motif;var read=sketch.Read();
        ShowHintScene(p,HintDrawing.Rails(frieze,A-1));
        p.Bits("Across landmarks",sketch.Across.IsChain?"Stored order supplies strict before/after. The gaps have no lengths.":
            "Membership only. The separate comparison rows supply the retained order.",sketch.Across.SiteBytes(),3*sketch.Across.Count,"across");
        if(!sketch.Across.IsChain) p.Bits("Across comparisons","Strict before/after rows. A cycle is a conflict.",sketch.Across.ClaimBytes(),6*sketch.Across.ClaimCount,"across");
        p.Bits("Rise landmarks","A second ordered rail. Reusing a landmark aligns any attached sites.",sketch.Rise.SiteBytes(),3*sketch.Rise.Count,"rise");
        p.Rows("Site attachments","Each row is [across landmark, rise landmark]. No coordinates or curve handles.",sketch.Bits,
            Enumerable.Range(0,sketch.SiteCount).Select(sketch.Site).ToArray(),"sites",actual:sketch.SiteBytes(),
            rowKeys:Enumerable.Range(0,sketch.SiteCount).Select(i=>"site"+i).ToArray());
        p.Rows("Pen connections","Ordered endpoints. Closure must reference the same start site.",sketch.Path.SiteBits,
            Enumerable.Range(0,sketch.Path.EdgeCount).Select(sketch.Path.Edge).ToArray(),"path",actual:sketch.Path.EdgeBytes(),
            rowKeys:Enumerable.Range(0,sketch.Path.EdgeCount).Select(i=>"edge"+i).ToArray());
        p.Bits("Rail to ray bindings","Two rails use adjacent rays of the shared equal four-part contour. Removing the rows removes the right-angle promise.",
            sketch.AxisBytes(),sketch.AxisBytes().Length==0?0:36,"basis");
        if(sketch.Perpendicular) p.Bits("Quarter-turn construction","The same positive part reference is used four times around a closed radial whole.",
            sketch.TurnBasis.ArcBytes(),72,"basis");
        p.Bits("Repeat participation","O and D are fixed by array position. Every slot occupies a place, including zero. Padding is not a seventh place.",
            frieze.Repeats.Bytes(),frieze.Repeats.Count,"repeats",Enumerable.Range(0,frieze.Repeats.Count)
                .Select(i=>new BitMark(i,1,["repeats","repeat"+i],$"Repeat place {i+1}")).ToArray());
        p.Bits("Placement rail","Roles 0 = origin, 1 = far edge, 2 = next origin. Ordered 0 < 1 < 2 promises a positive gap.",
            frieze.Placement.SiteBytes(),6,"placement");
        if(!frieze.Placement.IsChain) p.Bits("Placement comparisons","No comparison between the far edge and next origin remains.",frieze.Placement.ClaimBytes(),4*frieze.Placement.ClaimCount,"placement");
        p.Result=read.Conflict?"Conflicting rail order. No admissible drawing is supplied.":
            $"{(read.Rectangle?"Rectangle proved.":rectangle?"Rectangle not proved.":"Open fret.")} {read.RightTurns} right-angle joints proved. Order {(read.Ordered?"settled":"open")}.";
        p.Events.Add("The three drawings share every structural bit. Only the chosen positive gap sizes and presentation frame vary.");
        if(Variant==1) p.Events.Add("The renderer samples possible orders. Its choice is not added to the evidence.");
        if(Variant==2) p.Events.Add("Shared alignments remain. The free rail angle admits slanted corners.");
        if(Variant==3) p.Events.Add(rectangle?"The last-to-first connection is absent. Drawing near the start cannot restore closure.":"The next repeat can start before the previous far edge. Overlap is now admitted.");
        p.Boundary="The stored sketch contains references and order. Numerical gap choices exist only in the drawing witness. Sampled layouts do not prove every possible layout.";
    }

    private void HintCircle(ExplorerPage p)
    {
        Choose(p,"Mark strip",["101101 · rectangle","111111 · hexagon","101010 · triangle","100100100100 · square","111100 · unpaired","000000 · no marks"],Variant,n=>Variant=n);
        Choose(p,"Read",["All hints","Unequal shares allowed","No closing chord","Second gate hidden"],Control,n=>Control=n);
        var circle=HintExamples.Circle(Variant,Control);var read=circle.Read();
        int visible=Steps==0?circle.Trace.Length:Math.Min(Steps,circle.Trace.Length);
        ShowHintScene(p,HintDrawing.Circle(circle,A-1,visible));
        p.Actions.Add(new("Next slot",()=>{Steps=Steps==0?1:Math.Min(Steps+1,circle.Trace.Length);Refresh();}));
        p.Actions.Add(new("Full traversal",()=>{Steps=0;Refresh();}));
        p.Bits("Mark slots","One stored value bit per place. This is a finite chain, not a binary numeral. O/D come from array position.",circle.Marks.Bytes(),circle.Marks.Count,"marks",
            Enumerable.Range(0,circle.Marks.Count).Select(i=>new BitMark(i,1,["marks","slot"+i],$"Visit place {i+1}, {(circle.Marks.At(i)==1?"leave a landmark":"skip the landmark")}")).ToArray());
        if(Control==3) p.Bits("Visible slots","A zero here hides a stored gate from this reader. It does not turn that gate off.",
            LowBytes(((1UL<<circle.Marks.Count)-1)&~2UL,(circle.Marks.Count+7)/8),circle.Marks.Count,"marks");
        p.Rows("Step to contour site","Slot i binds the start of arc i. Skipping a landmark keeps this correspondence.",6,
            Enumerable.Range(0,circle.Marks.Count).Select(i=>new[]{i,circle.Site(i)}).ToArray(),"contour",actual:circle.BindingBytes(),
            rowKeys:Enumerable.Range(0,circle.Marks.Count).Select(i=>"slot"+i).ToArray());
        p.Bits("Contour parts","Rows are [from, to, positive part reference]. A shared reference promises equal angular shares of this whole.",
            circle.Contour.ArcBytes(),circle.Marks.Count*18,"contour");
        p.Bits("Contour return","The end of the unwrapped traversal binds back to its start.",circle.Contour.ClosureBytes(),12,"contour");
        var chords=Enumerable.Range(0,circle.ChordCount).Select(circle.Chord).ToArray();
        string SlotKey(int site)=>"slot"+Enumerable.Range(0,circle.Marks.Count).Single(i=>circle.Site(i)==site);
        p.Rows("Settled chord rows","Only completed mark visits leave sites for chords. The final row requires explicit closure.",6,
            chords,"chords",roleKeys:chords.Select(row=>row.Select(SlotKey).ToArray()).ToArray(),actual:circle.ChordBytes(),
            rowKeys:chords.Select((row,i)=>read.Closed && i==chords.Length-1?"seam":SlotKey(row[1])).ToArray());
        p.Bits("Ink return","The last retained site returns to the first. Open or unresolved traversals have no closing row.",
            circle.SeamBytes(),circle.SeamBytes().Length==0?0:12,"seam");
        var walk=circle.Walk;
        if(Steps>0)
        {
            walk=new GateWalk(circle.Marks,Control==3?1:-1);
            for(int i=0;i<visible;i++) {var visit=walk.Step();if(!visit.Completed)break;}
        }
        p.Bits("Mark passage law","Eight bytes list allowed before/after states. The complementary skip law rejoins the next slot.",walk.Circuit.LawBytes(walk.MarkTable),64,"walk");
        p.Bits("Mark route references","One [visit, gate, mark] row per place. Each destination rejoins the following visit.",
            walk.Circuit.PackedRows(walk.MarkTable),circle.Marks.Count*3*walk.Circuit.ReferenceBits,"walk");
        p.Bits("Skip route references","One [visit, gate, skip] row per place. Zero takes this route and still advances.",
            walk.Circuit.PackedRows(walk.SkipTable),circle.Marks.Count*3*walk.Circuit.ReferenceBits,"walk");
        p.Bits("Actual cursor","One activation moves through mark/skip routes to the next slot or the terminal site.",walk.Circuit.ValueBytes(),4*circle.Marks.Count+1,"walk");
        p.Result=$"{read.Landmarks} settled landmarks from {circle.Marks.Count} places. "+
            (read.Square?"Square proved.":read.Rectangle?"Rectangle proved.":read.RegularPolygon?$"Regular {read.Landmarks}-gon proved.":"No rectangle or regular-polygon guarantee.");
        if(!read.Complete) p.Result+=" Cursor waiting at hidden gate.";
        if(Steps>0) p.Events.Add($"Playback shows {visible} of {circle.Trace.Length} attempted slots. The result below describes the whole recorded run.");
        p.Events.Add("Six equal steps with the second and fifth unmarked retain two diameters. The four joined endpoints form a rectangle with a fixed aspect ratio.");
        p.Events.Add("Equal spacing is an explicit shared-part assumption. The six gate bits alone do not establish it.");
        p.Boundary="The proof reads arc references and opposed positions, without inspecting pixels. Unknown is a read over 0/1. It is never silently interpreted as a skipped mark.";
    }

    private void HintStreams(ExplorerPage p)
    {
        Choose(p,"Clock pattern",["Take turns","Move together","Rise held","Mix singles and pairs"],Variant,n=>Variant=n);
        var (a,b,score)=HintExamples.Streams(Variant);
        ShowHintScene(p,HintDrawing.Streams(Variant,A-1,Steps==0?int.MaxValue:Steps));
        p.Actions.Add(new("Next beat",()=>{Steps=Steps==0?1:Math.Min(Steps+1,a.Count);Refresh();}));
        p.Actions.Add(new("Full traversal",()=>{Steps=0;Refresh();}));
        for(int voice=0;voice<2;voice++)
        {
            var strip=voice==0?a:b;
            p.Bits(voice==0?"Across gates":"Rise gates","One bit per clock place. Zero holds this voice head while the clock still visits the place.",strip.Bytes(),strip.Count,"gates"+voice,
                Enumerable.Range(0,strip.Count).Select(i=>new BitMark(i,1,["gates"+voice,"beat"+i],$"Clock place {i+1}")).ToArray());
            p.Bits($"Voice {voice} movements","Four ordered ODV/axis/reach references. Positive and negative uses share an unknown positive span.",score.VoiceBytes(voice),32,"voice"+voice);
        }
        p.Bits("Paired clock","Derived by placing the two gate bits beside each other. 11 samples the two movements as one diagonal.",score.ClockBytes(),score.ClockLength*2,"clock");
        var phase=new BorderPhase(0,0,0);var end=score.Predict().End;
        for(int i=0;i<a.Count;i++) score.Step(ref phase);
        p.Result=$"{a.Count} clock places. Final voice heads {phase.First}, {phase.Second}. Displacement H {SignText(end.SignSupport(0))}, V {SignText(end.SignSupport(1))}.";
        p.Events.Add("Taking turns and moving together consume the same four movements from each voice, but leave different intermediate paths.");
        p.Boundary="A zero movement gate holds its voice. A zero circle mark keeps advancing around the contour. These different consequences come from what the gate is connected to.";
    }
}
