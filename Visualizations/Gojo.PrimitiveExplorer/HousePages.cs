using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private static readonly string[] HouseSites = ["A", "B", "P", "C", "D", "M"];
    private static string Names(int[] sites) => string.Join(" → ", sites.Select(i => HouseSites[i]));

    private ExplorerPage HousePage()
    {
        var p = new ExplorerPage("Two houses, one difference at the seam",
            "Both rooms meet their roofs at A and B. Add a landmark through the roof reader and compare what each room can recover.", "SeamLadders.md");
        Choose(p, "Landmark", ["Original seam", "Cut the roof seam", "Also bind the room to M"], Mode, n => Mode = n);
        Choose(p, "Flow observation", ["As wired", "Flip roof meter", "Reverse roof flow sample", "Zero roof flow sample"], Variant, n => Variant = n);
        Choose(p, "Boundary read", ["Combined boundary", "Both face walks"], Control, n => Control = n);
        var shared = new HouseSeam(true);
        var separate = new HouseSeam(false);
        void Inspect(HouseSeam house, string name, string key, double center)
        {
            // Retain both ports before introducing the rung.
            var roof = house.Roof;
            var room = house.Room;
            if (Mode >= 1) house.AddRoofCut();
            if (Mode == 2) house.BindRoomToRoofCut();
            if (Variant == 2) house.SetRoofSample(true);
            if (Variant == 3) house.SetRoofSample(false, false);
            var balance = house.EdgeBalance();
            var roofKey = key + "roof";
            var roomKey = key + "room";
            var roles = new string[7][];
            for (var i = 0; i < roles.Length; i++)
                roles[i] = [i == 0 ? roofKey : i == 3 ? roomKey : key + "e" + (house.SignedUse(i) >> 1)];
            p.Rows(name + " · face uses", "One four-bit field per use. Its low bit reverses that use. Roof then room in stored order.",
                4, Enumerable.Range(0, 7).Select(i => new[] { house.SignedUse(i) }).ToArray(), key + "uses", roles, house.UseBytes());
            var edges = Enumerable.Range(0, house.EdgeCount).Select(house.EdgeEnds).ToArray();
            p.Rows(name + " · edge endpoints", "A=0, B=1, P=2, C=3, D=4. M=5 is available for a rung.", 3, edges, key + "edges",
                actual: house.EdgeBytes(), rowKeys: Enumerable.Range(0, house.EdgeCount).Select(i => key + "e" + i).ToArray());
            p.Rows(name + " · face ranges", "Half-open ranges of signed use positions.", 3, [house.FaceRange(0), house.FaceRange(1)], key + "faces", actual: house.Faces.Bytes());
            if (house.CutCount > 0)
                p.Rows(name + " · roof cut", "Edge and interior landmark. The original pair remains addressable.", 3, [[roof.Edge, 5]], key + "M", actual: house.CutBytes());
            if (house.BindingCount > 0)
                p.Rows(name + " · room correspondence", "Receiving edge and roof cut row. The room retains its edge and its own sample.", 3, [[room.Edge, 0]], roomKey, actual: house.BindingBytes());
            p.Bits(name + " · samples", "Fixed O=0. Each edge owns V then D. Each meter observes one edge in its selected frame.", house.SampleBytes(),
                house.EdgeCount * 2, key + "flow", Enumerable.Range(0, house.EdgeCount).Select(e => new BitMark(e * 2, 2,
                    new[] { key + "e" + e, e == roof.Edge ? roofKey : "", e == room.Edge ? roomKey : "" }.Where(s => s.Length > 0).ToArray(), $"Sample on edge {e}")).ToArray());

            (double X, double Y)[] xy = [(center-100,190),(center+100,190),(center,75),(center-100,285),(center+100,285)];
            p.Label(name, center, 25);
            for (var i = 0; i < edges.Length; i++)
            {
                if (i == roof.Edge || i == room.Edge) continue;
                var a = xy[edges[i][0]]; var b = xy[edges[i][1]];
                p.Edge(key + "e" + i, "", $"edge {i}, {Names(edges[i])}", a.X, a.Y, b.X, b.Y, true);
            }
            if (Control == 1 || balance[roof.Edge] != 0)
                p.Edge(roofKey, "roof →", $"Roof uses edge {roof.Edge} forward.", center-100, 185, center+100, 185, true);
            if (Control == 1 || balance[room.Edge] != 0)
                p.Edge(roomKey, "", $"Room uses edge {room.Edge} backwards.", center+100, 205, center-100, 205, true);
            p.Label("A", center-116, 180);
            p.Label("B", center+116, 180);
            if (Mode > 0 && Control == 1)
            {
                p.Circle(key + "M", "The roof can observe M through its cut row.", center, 185, 7);
                if (room.Landmarks().Length == 3)
                    p.Circle(roomKey, "The room can observe the same M through this reversed use.", center, 205, 7);
            }
            p.Label($"Roof   {Names(roof.Landmarks())}    ·    {Word(roof.Flow(Variant == 1))}", center, 310);
            p.Label($"Room   {Names(room.Landmarks())}    ·    {Word(room.Flow())}", center, 337);
            p.Node(key + "faces", $"{balance.Count(count => count != 0)} surviving edges", "Sum signed occurrences by edge reference across the two faces.", center, 402);
        }
        Inspect(shared, "Shared edge", "shared", 175);
        Inspect(separate, "Independent edges", "separate", 540);
        p.Result = Mode switch
        {
            0 => "Both roof seams read A → B and both rooms read B → A. The signed edge sums already distinguish the houses.",
            1 => "Both roofs read A → M → B. Only the room sharing the seam recovers B → M → A.",
            _ => "Both rooms recover B → M → A. The second room has an explicit landmark binding and still owns its own edge."
        };
        p.Boundary = "Arrows are offset so each face can show its own read. A circle on an arrow means that face can see M. The landmark’s distance is unspecified.";
        p.Events.Add(Variant switch { 1 => "Only the roof meter flips. Both room observations keep their previous words.",
            2 => "The roof sample changes. The room meter changes only where it reads the same sample.",
            3 => "Zero observed roof flow leaves the boundary and the landmark readable.", _ => "Compare the same cut through two port reads. Hover either seam arrow to see its signed-use field." });
        return p;
    }

    private ExplorerPage StrokePage()
    {
        var p = new ExplorerPage("Where did the closing stroke come from?",
            "Follow the same three uses from the source into its variant. The fourth use closes the walk. The rung says which construction supplied it.", "SeamLadders.md");
        Choose(p, "Source", ["Open stroke → proposed O", "Closed template → gapped variant"], Mode, n => Mode = n);
        var stroke = new StrokeRung(Mode == 1);
        var pair = stroke.Correspondence();
        p.Rows("Rung · source and derived view", "One bit per view reference. View 0 selects three uses and view 1 selects four.", 1, [pair], "rung", actual: stroke.RungBytes());
        p.Rows("Views · cuts in the use array", "Each row names a start cut and an exclusive end cut.", stroke.Views.Bits,
            [stroke.Views.Range(0), stroke.Views.Range(1)], "views", actual: stroke.Views.Bytes(), rowKeys: ["open","closed"]);
        p.Rows("Edge endpoints", "A=0, B=1, C=2, D=3. The closing edge is D → A.", 2,
            Enumerable.Range(0, 4).Select(stroke.Catalogue.Edge).ToArray(), "edges", actual: stroke.Catalogue.EdgeBytes(), rowKeys: ["e0","e1","e2","bridge"]);
        p.Rows("Signed uses", "Both views use the same occurrences. Only view 1 includes use 3.", 3, Enumerable.Range(0,4).Select(i => new[] { stroke.Catalogue.SignedUse(i) }).ToArray(),
            "uses", actual: stroke.Catalogue.UseBytes(), rowKeys: ["e0","e1","e2","bridge"]);
        for (var side = 0; side < pair.Length; side++)
        {
            var view = pair[side];
            var cx = 175 + side * 365;
            var range = stroke.Views.Range(view);
            double[] angles = [-65, 45, 155, 250, 295];
            p.Label(side == 0 ? "Source" : "Derived view", cx, 45);
            for (var i = range[0]; i < range[1]; i++)
            {
                var e = stroke.Catalogue.SignedUse(i) >> 1;
                for (var part = 0; part < 14; part++)
                {
                    double Angle(int k) => (angles[e] + (angles[e+1] - angles[e]) * k / 14) * Math.PI / 180;
                    var a = Angle(part); var b = Angle(part + 1);
                    p.Stroke(e == 3 ? "bridge" : "e" + e, e == 3 ? "D → A belongs to the completed view." : $"Use {i} survives in both views.",
                        cx + 98*Math.Cos(a), 205 + 115*Math.Sin(a), cx + 98*Math.Cos(b), 205 + 115*Math.Sin(b), e == 3 && Mode == 0);
                }
            }
            var closed = stroke.Views.IsClosed(view);
            p.Node(closed ? "closed" : "open", closed ? "Closed" : "Open", $"Selected uses [{range[0]}, {range[1]}). Closure checks every join.", cx, 385, closed);
        }
        p.Result = Mode == 0 ? "The measured walk ends at D. The completion supplies D → A, so its own walk closes." :
            "The template includes D → A. The derived walk omits that interval while the template remains closed.";
        p.Boundary = "The curves illustrate the topology. Recognizing a glyph, estimating the gap size and installing a material passage each require further evidence.";
        return p;
    }
}
