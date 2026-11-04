using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private ExplorerPage PairedReachPage()
    {
        var p = new ExplorerPage("Two reaches, one choice",
            "Hide which tip extends. How much can another structure still know about their shared reach?", "PairedReach.md");
        Choose(p, "Construction", ["Two reaches", "One loose dot"], Mode, n => Mode = n, true);
        Choose(p, "Allowed pairs", ["Exactly one", "Independent", "Together"], Variant, n => Variant = n);
        Choose(p, "Left evidence", ["Unobserved", "Off", "On", "No reading"], Control, n => Control = n);
        byte admitted = Variant switch { 0 => 6, 1 => 15, _ => 9 };
        var left = Control == 0 ? null : new Observation(Control switch { 1 => 1, 2 => 2, _ => 0 });
        var settings = PresencePairPort.Bind(admitted, left);
        p.Bits("Allowed pairs", "One truth bit each for neither, left only, right only and both.",
            [admitted], 4, "rule");
        p.Bits("Pairs after evidence", "The left observation filters each complete pair.", [settings.ReadPairs()], 4, "rule");

        PresencePairPort output;
        string subject;
        if (Mode == 0)
        {
            string[] stops = ["Near", "Far", "Beyond"];
            Choose(p, "Read at", stops, A, n => A = n);
            var rail = new LandmarkRun(2, [2, 0, 3, 1]);
            var gauge = new PairedReach(rail, settings, (0, 3), (0, 3));
            var sites = rail.Sites();
            output = gauge.At(rail, sites[A + 1]);
            subject = stops[A];
            p.Rows("Stops in outward order", "Spacing is illustrative. The first stop is the shared origin.", 2,
                sites.Select(site => new[] { site }).ToArray(), "rail", actual: rail.SiteBytes());
            p.Rows("Two dial bindings", "Each row holds the resting and extended tip of one occupied prefix.",
                2, [[0, 3], [0, 3]], "tips", actual: gauge.TipBytes());
            p.Bits("Tip comparison", "Pair roles are left reaches at least as far, and right reaches at least as far.",
                [gauge.CompareTips().ReadPairs()], 4, "tips");
            double[] x = [105, 255, 450, 585];
            for (int i = 0; i < sites.Length; i++) p.Label(i == 0 ? "Origin" : stops[i - 1], x[i], 55);
            for (int lane = 0; lane < 2; lane++)
            {
                int y = 112 + lane * 65;
                p.Label(lane == 0 ? "Left" : "Right", 52, y - 7);
                for (int i = 1; i < sites.Length; i++)
                {
                    var included = gauge.At(rail, sites[i]).Test(lane == 0 ? (byte)10 : (byte)12).ReadSupport();
                    p.Stroke("rail", "The rail supplies the order of these stops.", x[i-1], y+6, x[i], y+6, false, false);
                    if (included is 2 or 3)
                        p.Stroke("tips", "Solid reach is certain. Dashed reach is possible.", x[i-1], y, x[i], y, included == 3);
                }
            }
            p.Label("Solid = certain reach     Dashed = possible reach", 341, 217);
            p.Boundary = "Both tips project onto the same ordered rail. Only the named stops and their order are supplied.";
        }
        else
        {
            var links = new PairedLinks(3, (4, 0), (4, 2), settings);
            output = links.From(4);
            subject = "Dot ownership";
            p.Rows("Candidate memberships", "Both ordered links start at mark 4 and lead to different stem heads.",
                3, [[4, 0], [4, 2]], "links", actual: links.LinkBytes());
            p.Node("links", "Dot", "One participating mark.", 345, 75, true);
            for (int i = 0; i < 2; i++)
            {
                int x = 190 + i * 310;
                var present = links.From(4, 2*i).Test(14).ReadSupport();
                if (present is 2 or 3) p.Stroke("links", "An admitted membership.", 345, 96, x, 162, present == 3);
                p.Node("links", i == 0 ? "Left head" : "Right head", "A possible owner of the mark.", x, 178);
            }
            p.Boundary = "These links express ownership of a mark. The stems and dot remain separate ink components.";
        }

        byte pairs = output.ReadPairs();
        string[] names = ["Neither", "Left only", "Right only", "Both"];
        p.Bits("Public joint read", "Highlighted cases can be handed to a neighbor as one relation.", [pairs], 4, "public",
            names.Select((name, i) => new BitMark(i, 1, ["public", "case"+i], name)).ToArray());
        for (int i = 0; i < 4; i++)
            p.Node("case"+i, names[i], "One possible joint answer when lit.", 113 + 152*i, 288, (pairs & (1 << i)) != 0);
        var handoff = new GuardedOffer(output.Test(6));
        for (int i = 0; i < Math.Min(Steps, 2); i++) handoff.Request();
        Stepper(p, 2, "Try handoff");
        p.Bits("Sender", "Waiting, offer, sampled permission. The guard asks whether exactly one role is active.",
            [handoff.Sender], 3, "sender");
        p.Bits("Receiver", "Gate, inbox, spare site. A single pulse may enter the inbox.",
            [handoff.Receiver], 3, "receiver");
        p.Node("sender", (handoff.Sender & 1) != 0 ? "Waiting pulse" : "Sender empty", "The sender supplies the pulse.",
            193, 355, (handoff.Sender & 1) != 0);
        p.Edge("receiver", "", "One material seam.", 262, 355, 426, 355);
        p.Node("receiver", handoff.Receiver == 0 ? "Inbox empty" : "Pulse received", "Only an accepted handoff fills this site.",
            500, 355, handoff.Receiver != 0);
        p.Result = pairs == 0 ? subject + " · no supported reading." :
            subject + " · " + string.Join(" or ", names.Where((_, i) => (pairs & (1 << i)) != 0)) + ".";
        p.Events.Add("Try handoff. The sender releases its pulse if every admitted case has exactly one participating role.");
        return p;
    }
}
