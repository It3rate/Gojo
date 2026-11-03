using Avalonia;
using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private ExplorerPage GlyphCompletionPage()
    {
        var p = new ExplorerPage("Across a glyph gap",
            "Move a cut in O, then try the same read on open ink. Choose D to try constructions between two fixed ends.",
            "GlyphCompletion.md")
        {
            DrawingGuide = Mode == 2 ? ("D · fixed ends", "The stem and its return are connected. The arrows tell you how the return leaves and arrives. Try different interiors against both arrows.")
                : ("C · one fixed cut, one moving cut", "The solid ink joins two tips. Move the second cut or reverse the walk. Ask whether this owner has a construction path from the last tip back to the first.")
        };
        Choose(p,"Glyph",["C from O","Open C","D · ends only"],Mode,n=>Mode=n,true);
        if (Mode == 2) { DrawReturnTrial(p); return p; }

        Choose(p,"Ink extent",["Seven parts","Six parts","Five parts","Four parts","Three parts","Two parts","One part"],A,n=>A=n);
        Choose(p,"Walk",["Forward","Reverse"],Variant,n=>Variant=n);
        Choose(p,"Reader",["Ink only","Ask owner","Try passage"],Control,n=>Control=n);
        var cut = new GlyphCut(7-A, Variant == 1, Mode == 0);
        var support = GlyphCut.Predict(true,cut.InkCloses,Control == 0 ? null : cut.InspectReturn());
        var walk = cut.Walk;
        var indices = Enumerable.Range(0,walk.UseCount).ToArray();
        var keys = indices.Select(i=>"edge"+(walk.SignedUse(i) >> 1)).ToArray();
        p.Bits("Reader's prediction","Return through existing construction. 01 means absent, 02 present, 03 either.",[support],8,"reply");
        p.Rows("Ink and construction windows","Half-open ranges into the ordered uses. First is ink, then the optional complement and whole.",
            cut.Windows.Bits,Enumerable.Range(0,cut.Windows.Count).Select(cut.Windows.Range).ToArray(),"window",actual:cut.Windows.Bytes());
        p.Rows("Writing order","The low bit reverses one use. Reversing the walk also reverses order inside each window.",
            walk.UseBits,indices.Select(i=>new[]{walk.SignedUse(i)}).ToArray(),"order",actual:walk.UseBytes(),rowKeys:keys);
        p.Rows("Local span ends","Shared site references. Each diagram arc highlights its own row.",walk.SiteBits,
            indices.Select(walk.Edge).ToArray(),"ends",actual:walk.EdgeBytes(),rowKeys:indices.Select(i=>"edge"+i).ToArray());
        if (cut.RadialRows().Length > 0)
            p.Bits("Retained O","Each source arc binds two sites and the same unmeasured share. The source seam binds its end back to its start.",
                cut.RadialRows(),18*GlyphCut.Landmarks,"radial");
        const double cx = 350, cy = 190, radius = 115;
        Point At(int site)
        {
            int rank = site == ContourPattern.Start ? 0 : site-4;
            return new(cx+radius*Math.Cos(-(rank+1)*Math.PI/4),cy+radius*Math.Sin(-(rank+1)*Math.PI/4));
        }
        for (var i = 0; i < (Control == 0 ? cut.InkCount : walk.UseCount); i++)
        {
            var pair = walk.DirectedUse(i); var a = At(pair[0]); var b = At(pair[1]);
            bool ink = i < cut.InkCount;
            var note = $"{(ink ? "Ink" : "Retained construction")} from s{pair[0]} to s{pair[1]}.";
            var path = FormattableString.Invariant($"M {a.X} {a.Y} A {radius} {radius} 0 0 {walk.SignedUse(i)%2} {b.X} {b.Y}");
            p.Drawing.Add(new("curve",keys[i],"",note,a.X,a.Y,b.X,b.Y,ink,!ink){PathData=path});
            p.Details[keys[i]]=note;
        }
        foreach (var (site,label) in new[]{(cut.Start,"Start"),(cut.Tip,"Arrival")})
        {
            var pt = At(site);
            p.Circle("window",label,pt.X,pt.Y,4);
            p.Label($"{label} · s{site}",pt.X+(pt.X >= cx ? 60 : -60),pt.Y-7);
        }
        p.Node("reply", support switch { 3=>"Return unresolved",2=>"Return available",_=>"No retained return" },
            "Answer available to the second reader.",cx,365,support==2,support==3);
        p.Result = $"Ink is open. Its walk goes from s{cut.Start} to s{cut.Tip}.";
        if (Control == 2)
        {
            var attempt = cut.AttemptReturn();
            p.Events.Add(attempt.Arrived ? $"Followed signed uses {string.Join(", ",attempt.Uses)} to s{cut.Start}." : $"No construction passage leaves the arrival tip. The reader stays at s{cut.Tip}.");
        }
        p.Boundary = "The left panel is the owner's encoding. The reader can see only the selected reply. Dashed arcs are construction outside the visible ink.";
        return p;
    }

    private void DrawReturnTrial(ExplorerPage p)
    {
        string[] names = ["D","D · corners","D · half of O","D · ends only","Straight · closure sought"];
        Choose(p,"Proposed interior",["Steer as you travel","Two corners","Half a circle","No proposal","One straight"],B,n=>B=n);
        var observed = CurvedCapitals.Ink("D · ends only");
        DrawPen(p,observed,"D · ends only",new(190,205),1,true,false);
        p.Label("Observed attachments",160,52);
        void Ends(CurveInk ink,string title,string prefix)
        {
            var edges=Enumerable.Range(0,ink.Catalogue.EdgeCount).ToArray();
            p.Rows(title,"Two three-bit headings per span in the same upright frame.",3,edges.Select(ink.Heading).ToArray(),
                prefix+"heading",actual:ink.HeadingBytes(),rowKeys:edges.Select(i=>prefix+"edge"+i).ToArray());
        }
        Ends(observed,"Observed headings","");
        p.Bits("Observed interior","Both turning and holding between joints remain possible.",[observed.Read().BendSupport],8,"edge1");
        p.Result = "One closed ink loop. The inside of the return is unresolved.";
        p.Boundary = "Trying a construction leaves the observed D intact. A fitting rule may admit many paths and widths.";
        if (B == 3) { p.Label("Choose a proposed interior",505,190); return; }
        var proposal = CurvedCapitals.Ink(names[B]);
        var trial = new ReturnTrial(observed,proposal);
        if (B == 0) Choose(p,"Illustrated bow",["Shallow","Deep"],Variant,n=>Variant=n);
        DrawPen(p,proposal,names[B],new(515,205),1,true,proposal.SourceRead.HasValue,prefix:"trial",
            tangentScale:B == 0 && Variant == 0 ? 0.55 : 1);
        p.Label("Proposed construction",505,52);
        Ends(proposal,"Proposed headings","trial");
        var drives = proposal.DriveLinks();
        p.Rows("Proposed steering","Span and steering word. Zero holds heading and one steers clockwise.",
            proposal.WireBits,drives,"trialDrive",actual:proposal.DriveBytes(),rowKeys:drives.Select(r=>"trialedge"+r[0]).ToArray());
        if (proposal.SourceRead is { } contour)
        {
            var arcs = proposal.ArcLinks();
            p.Rows("Proposed contour bindings","Each span names an arc of the candidate's radial source.",
                proposal.WireBits,arcs,"trialradial",actual:proposal.ArcBytes(),rowKeys:arcs.Select(r=>"trialedge"+r[0]).ToArray());
            p.Bits("Proposed radial shares","Both arc rows name the same share. The dashed half stays in construction.",
                proposal.SourceArcBytes(),18*contour.Parts,"trialradial");
        }
        p.Rows("Trial correspondence","Each row binds a proposed local site to an observed local site. Rows follow departure and arrival.",
            trial.Bits,[trial.Binding(0),trial.Binding(1)],"binding",actual:trial.BindingBytes());
        p.Node("binding",trial.Fits ? "Fits both ends" : "Conflicts with the ends","Trial in the shared upright frame.",505,340,trial.Fits);
        p.Events.Add(trial.Fits ? "This is one compatible interior. Its construction belongs to the proposal." :
            "A forward straight keeps one heading. The required departure and arrival are opposed.");
    }
}
