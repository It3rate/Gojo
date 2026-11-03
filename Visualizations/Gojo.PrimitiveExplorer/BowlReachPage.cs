using Avalonia;
using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private BowlReach[]? bowlOwners;

    private ExplorerPage BowlReachPage()
    {
        bowlOwners ??= Mode switch
        {
            0 => [BowlReach.AdjustableD(),BowlReach.RadialD()],
            1 => [BowlReach.B(false),BowlReach.B(true)],
            _ => [BowlReach.EndsOnlyD()]
        };
        var owners = bowlOwners;
        var body = owners[Mode == 2 ? 0 : Variant];
        string[] names = Mode switch { 0 => ["Adjustable D","Radial D"], 1 => ["Independent B","Linked B"], _ => ["Ends only"] };
        var p = new ExplorerPage("Push a bowl outward","Pin the stem. Give each construction one outward request. Watch which tips move and where the request settles.","BowlReach.md")
        {
            DrawingGuide = Mode switch
            {
                0 => ("D · two outlines in one place","The solid adjustable bow and the dashed half-circle start on the same arc. Push outward. The half-circle shares its radius with half the pinned stem."),
                1 => ("B · push the upper bowl","Both letters start with a wider lower bowl. The left B has independent controls. The right B shares one displacement between two different starting widths."),
                _ => ("D · ends only","The two arrows and the stem constrain a return. This owner supplies no marked tip or width control. A request can wait while the tip's response stays unresolved.")
            },
            Status = "One push per owner. Reset starts a fresh trial."
        };
        Choose(p,"Experiment",["D · overlay","B · compare","D · ends only"],Mode,n=>{Mode=n; Variant=0; bowlOwners=null;},true);
        if (owners.Length > 1)
            Choose(p,"Inspect encoding",names,Variant,n=>Variant=n);
        p.Actions.Add(new("Push outward",()=>{foreach (var owner in owners) owner.Push(); Refresh();}));
        p.Actions.Add(new("Reset",()=>{bowlOwners=null; Refresh();}));

        var state = body.StoreBytes();
        var after = body.ReadTips();
        var prediction = BowlMotionReader.Predict(body.Before);
        const string key = "motion";
        p.Bits("Reader before the push","Joint movement support from previous example trials with compatible visible tips. Bit 0 stays, bit 1 upper only, bit 3 both.",[prediction],8,key);
        if (body.Pushed)
            p.Bits("Reader after the push","Compare the actual tip publications along the outward ray.",
                [BowlMotionReader.Compare(body.Before,after)],8,key);
        p.Bits("Owner's live sites","Reserve, waiting, open gate, pin gate, half-span dial, then any independent dials.",
            state,body.SiteCount,"request");
        var tips = body.TipRows();
        if (tips.Length > 0)
            p.Rows("Tip connections","Ink span, dial port, resting landmark, farther landmark.",3,tips,"reach",
                actual:body.TipBytes(),rowKeys:tips.Select(t=>"tip"+t[0]).ToArray());
        var pins=body.PinRows();
        p.Rows("Stem pins","Stem span, fixed half-span dial, occupied pin gate.",3,pins,"stem",actual:body.PinBytes());
        p.Rows("Outward order","The stem origin comes first. Successive sites are farther from the stem. Names do not encode distance.",3,
            BowlReach.Outward.Sites().Select(s=>new[]{s}).ToArray(),"reach",actual:BowlReach.Outward.SiteBytes());
        p.Rows("Offer","Reserve, guard, waiting site. This passage is unconditional.",3,[body.OfferRow()],"request");
        var routes=body.SettleRows();
        if (routes.Length > 0)
        {
            p.Rows("Width passage","Waiting site, pin gate or open gate, destination dial.",3,routes,"request",actual:body.SettleBytes());
            p.Bits("Passage law","An empty gate permits transfer into an empty dial. An occupied pin holds the request.",body.SettleLaw(),64,"request");
        }
        if (Mode == 0)
        {
            p.Label("Solid · adjustable     /     Dashed · radial",355,42);
            DrawReach(p,owners[0],300,Variant==0 ? "" : "other",false);
            DrawReach(p,owners[1],300,Variant==1 ? "" : "other",true);
        }
        else if (Mode == 1)
        {
            p.Label("Independent controls",192,42); p.Label("Shared control",530,42);
            DrawReach(p,owners[0],150,Variant==0 ? "" : "other",false);
            DrawReach(p,owners[1],490,Variant==1 ? "" : "other",false);
        }
        else DrawPen(p,body.Ink,"D · ends only",new Point(365,202),1,true,false);
        string Readout(BowlReach item) => BowlMotionReader.Compare(item.Before,item.ReadTips()) switch
        {
            1 => "Tip stayed", 2 => item.HasLower ? "Upper moved alone" : "Tip moved out",
            8 => "Both tips moved", _ => "Motion unresolved"
        };
        for (var i=0;i<owners.Length;i++)
        {
            var item=owners[i];
            p.Node(i==Variant ? key : "otherMotion",item.Pushed ? Readout(item) : names[i],
                names[i],owners.Length==1 ? 360 : 195+i*330,352,item.Pushed && !item.Waiting);
        }
        p.Result = body.Pushed ? names[Mode==2 ? 0 : Variant]+" · "+Readout(body)+
            (body.Waiting ? ". Request remains at the intake." : ". One request reached the dial.") :
            Mode==1 ? "From initial ink, the reader admits upper alone or both together." :
            "From initial ink, the reader admits staying or widening.";
        p.Boundary = "The diagram gives one placement of ordered landmarks. Their spacing, time, speed and curvature magnitude are unmeasured.";
        return p;
    }

    private static void DrawReach(ExplorerPage p,BowlReach body,double stemX,string prefix,bool dashed)
    {
        var tips = body.ReadTips();
        var levels = body.HasLower ? new double[]{94,196,298} : [105,285];
        var widths = body.HasLower ? new double[]{0,62,80,95,116} : [0,90,125,142,162];
        var order = BowlReach.Outward.Sites();
        p.Stroke(prefix+"stem","Pinned stem",stemX,levels[0],stemX,levels[^1],false);
        foreach (double y in levels) p.Circle(prefix+"stem","Pinned attachment",stemX,y,3);
        var rows=body.TipRows();
        for (var i=0;i<rows.Length;i++)
        {
            var edge=body.Ink.Catalogue.Edge(rows[i][0]);
            double top=levels[edge[0]], foot=levels[edge[1]], mid=(top+foot)/2;
            double Width(int site) => widths[Array.IndexOf(order,site)];
            var site=(i==0 ? tips.Upper : tips.Lower)!.Value;
            var width=Width(site);
            var k=prefix+"tip"+rows[i][0];
            void Arc(double reach,bool ghost)
            {
                var path=FormattableString.Invariant($"M {stemX} {top} A {reach} {(foot-top)/2} 0 0 1 {stemX} {foot}");
                p.Drawing.Add(new("curve",k,"",ghost ? "Resting outline or pinned radial outline." : "Current bow.",stemX,top,stemX,foot,!ghost,ghost){PathData=path});
            }
            if (body.HasLower && site != rows[i][2]) Arc(Width(rows[i][2]),true);
            Arc(width,dashed);
            p.Stroke(k,"Perpendicular reach to the marked tip.",stemX,mid,stemX+width,mid,true);
            p.Circle(k,"Marked tip on the outward ray.",stemX+width,mid,4);
        }
    }
}
