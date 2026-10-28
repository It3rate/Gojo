using System.Text.Json;
using Gojo.Core;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "coupled")
        {
            CoupledExperiment.Write(args[1]);
            return;
        }
        if (args.Length == 2 && args[0] == "joints")
        {
            JointExperiment.Write(args[1]);
            return;
        }
        if (args.Length == 2 && args[0] == "negotiate")
        {
            NegotiationExperiment.Write(args[1]);
            return;
        }
        if (args.Length != 1) throw new ArgumentException("Supply one output folder for the native experiment and review.");
        var folder = Path.GetFullPath(args[0]); Directory.CreateDirectory(folder);
        var c = new ConstructionRecipes(); var width = Parameter(120); var height = Parameter(80);
        var halfWidth = Parameter(60); var halfHeight = Parameter(40); var rise = Parameter(42); var doorHeight = Parameter(30);
        var path = c.Path(c.Point(0, 0), c.Point(1, 0), [width, height], 2);
        var offsets = c.Offsets(c.Point(0, 0), width, height);
        var pairs = c.OppositePairs(c.Point(60, 40), halfWidth, halfHeight);
        var facade = new ShapeInterface(); var roof = Attachments.Roof(c, facade.Side(2), rise);
        var door = Attachments.Door(c, facade.Side(0), doorHeight);
        var cases = new List<object>(); var roots = new List<IElement>();
        cases.Add(new { id = "open", title = "Before measurement", description = "Four connected corner roles; roof and door already reference public sides.",
            lines = Array.Empty<Line>(), notes = new[] { "No coordinate was invented for an open corner.", "The topology is present; a metric realization is still unbound." } });
        AddCase(G(facade.Surface, roof, door));

        AddScene("path", "Two beats, repeated twice", path,
            "Move long, turn; move short, turn. Repeat the phrase twice.",
            "Four moves and four witnessed turns; the terminal-to-start displacement is zero.");
        AddScene("offsets", "Two independent offset families", offsets,
            "Two perpendicular spans each supply a shared offset for the opposite side.",
            "The public corner and side identities survive this construction swap.");
        AddScene("pairs", "Centre and opposite pairs", pairs,
            "Two perpendicular half-spans create four radial offsets; each has an antipodal partner.",
            "Equal radial reach is not equal angular spacing. This rectangle is not a square.",
            guides: (Relation)F(F(F(pairs)[4])[2])[0]);

        var beforeWiden = SceneLines(facade, roof, door);
        width.Add(40);
        AddScene("shared-beat", "Change the shared long beat", path,
            "A parameter change enters its carrier as tension; the same retained phrase reads it twice.",
            "Both long sides become 160. The facade closes and the attached roof and door follow.", beforeWiden);

        halfWidth.Add(20);
        AddScene("centre-widen", "The same widening, a different response", pairs,
            "Increase the two half-spans while keeping the centre reference fixed.",
            "The width is again 160, but both sides move: left to -20 and right to 140. The path kept its starting corner fixed.",
            beforeWiden, (Relation)F(F(F(pairs)[4])[2])[0]);
        if (!facade.Publish(MachineRunner.Run(path))) throw new InvalidDataException("The path could not reclaim the same interface.");

        var badPath = c.Path(c.Point(0, 0), c.Point(1, 0), [Parameter(160), Parameter(80), Parameter(145), Parameter(80)], 1);
        var bad = MachineRunner.Run(badPath); var kept = facade.CaptureBindings();
        if (facade.Publish(bad)) throw new InvalidOperationException("A non-closing path was incorrectly published.");
        var bp = F(F(bad)[2]); var badLines = new List<Line>();
        for (var i = 1; i < bp.Length; i++) badLines.Add(Segment(bp[i - 1], bp[i], "path"));
        badLines.Add(Segment(bp[^1], bp[0], "residue", true));
        cases.Add(new { id = "closure", title = "Change only one occurrence", description = "One long move is 145 while the other remains 160.",
            lines = badLines, notes = new[] { "The 15-tick closure mismatch remains visible in red.",
                "The incompatible candidate is retained. The connected facade keeps its previous accepted state." } });
        AddCase(G(bad, facade.LastAttempt!, kept, facade.Surface));

        // Context extensions act through the same interface, without inspecting
        // which private machine currently realizes the rectangle.
        var sloped = Attachments.Roof(c, facade.Side(2), rise, 1, 3);
        AddAttachmentScene("roof-shift", "Move the roof ridge", sloped, door,
            "The ridge moves along the exposed top span; wall corners and door remain attached.");
        var arched = Attachments.Door(c, facade.Side(0), doorHeight, arch: true);
        AddAttachmentScene("arch", "Replace the door cap", roof, arched,
            "The straight cap becomes a quadratic arch. Door jamb and floor attachment roles remain available.");
        var movedDoor = Attachments.Door(c, facade.Side(0), doorHeight, 1, 3);
        AddAttachmentScene("opening", "Move an opening, keep the wall", roof, movedDoor,
            "Only the selected opening interval and its carried complement change. The complete rectangle remains underneath.");

        var squareLength = Parameter(80);
        var square = c.Path(c.Point(0, 0), c.Point(1, 0), [squareLength], 4);
        var squareRead = MachineRunner.Run(square); var sp = F(F(squareRead)[2]);
        cases.Add(new { id = "square", title = "One beat, four repeats", description = "A square shares the same move/turn mechanism with a shorter phrase.",
            lines = Enumerable.Range(1, sp.Length - 1).Select(i => Segment(sp[i - 1], sp[i], "path")).ToArray(),
            notes = new[] { "One shared length source and one quarter-turn; four repetitions.", "The terminal returns to the start without an invented closing segment." } });
        AddCase(squareRead);

        var pentagon = c.Path(c.Point(0, 0), c.Point(1, 0), [Parameter(40), Parameter(80), Parameter(40), Parameter(40), Parameter(80)], 1, new Placeholder());
        var pentagonRead = MachineRunner.Run(pentagon);
        cases.Add(new { id = "pentagon", title = "A five-beat phrase still needs turns", description = "Short, long, short, short, long is retained as a phrase with an unresolved turning instruction.",
            lines = Array.Empty<Line>(), notes = new[] { "The rhythm does not determine a unique pentagon or guarantee closure.",
                "The missing turn law stays open. No arbitrary angle is supplied to finish the drawing." } });
        AddCase(pentagonRead);

        var hinge = c.Between(facade.Corner(0), facade.Corner(2), 1, 2);
        var leaf = Attachments.WindowLeaf(c, hinge, c.Point(24, 0), c.QuarterTurn);
        var leafRead = Attachments.Read(leaf, facade.CurrentBindings); var lf = F(F(leafRead)[2]);
        var openingEnd = Evaluate(c.Plus(hinge, c.Point(24, 0)), facade.CurrentBindings, []);
        cases.Add(new { id = "hinge", title = "A window leaf rotates about its hinge", description = "The leaf changes direction while the opening reference stays in place.",
            lines = new[] { Segment(lf[0], openingEnd, "guide", true), Segment(lf[0], lf[1], "window") },
            notes = new[] { "An explicit contour traversal supplies the rotation.", "This tests a hinge connection; it does not infer door or window affordances from a picture." } });
        AddCase(G(leaf, leafRead, facade.Surface));

        // Store complete executable definitions and outcomes. Decode independently
        // before publishing the artifact; a JSON drawing is only a presentation.
        var archive = GojoNumericFormat.Encode(G(G(roots.ToArray()),
            G(facade.Surface, path, offsets, pairs, roof, door, width, height, halfWidth, halfHeight)));
        var restored = GojoNumericFormat.Decode(archive);
        if (restored.Length != 1 || F(restored[0]).Length != 2 || F(F(restored[0])[0]).Length != roots.Count)
            throw new InvalidDataException("Native archive verification failed.");
        File.WriteAllBytes(Path.Combine(folder, "construction-machines.gojo.bin"), archive);
        var report = new { summary = new[] { "Three private construction graphs share stable public corner and side roles.",
                "Numeric reads and explicit structural publication are separate; this is not an autonomous constraint solver.",
                "The native archive retains recipes, repeat feedback, bindings, closure demands and publication history." }, cases };
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(folder, "observations.json"), json);
        ReviewPage.Write(Path.Combine(folder, "review.html"), json);
        Console.WriteLine($"{cases.Count} cases; native archive restored; review: {Path.Combine(folder, "review.html")}");

        void AddScene(string id, string title, Relation machine, string description, string note, Line[]? ghosts = null, Relation? guides = null)
        {
            var read = MachineRunner.Run(machine);
            if (!facade.Publish(read)) throw new InvalidDataException($"Fixture {id} failed to publish.");
            var visible = SceneLines(facade, roof, door).ToList();
            if (guides is not null)
                foreach (var edge in F(F(Attachments.Read(guides, facade.CurrentBindings))[2]).Cast<Relation>())
                    visible.Add(Segment(edge.Left, edge.Right, "guide", true));
            cases.Add(new { id, title, description, lines = visible, ghostLines = ghosts ?? [], notes = new[] { note } });
            AddCase(G(read, facade.Surface, Attachments.Read(roof, facade.CurrentBindings), Attachments.Read(door, facade.CurrentBindings)));
        }
        void AddAttachmentScene(string id, string title, Relation roofRead, Relation doorRead, string description)
        {
            cases.Add(new { id, title, description, lines = SceneLines(facade, roofRead, doorRead), notes = new[] {
                "The complete base side is a gray guide. The opening is an independently selected interval." } });
            AddCase(G(facade.Surface, Attachments.Read(roofRead, facade.CurrentBindings), Attachments.Read(doorRead, facade.CurrentBindings)));
        }
        void AddCase(IElement observation) => roots.Add(CaptureObservation(observation));
    }

    // Deliberate historical artifact capture, not the live operational mechanism.
    // Each case becomes an independently identified world with all internal aliases
    // intact. The archive also retains the final connected live world separately.
    public static IElement CaptureObservation(IElement observation) =>
        GojoNumericFormat.Decode(GojoNumericFormat.Encode(observation))[0];

    public static Carrier Parameter(long value) { var c = new Carrier(); c.Set(value); return c; }
    public sealed record Line(double[][] points, string role, bool dashed = false);
    public static double[] XY(IElement point)
    {
        if (!MachineRunner.TryNumbers(point, out var values)) throw new InvalidDataException("A render coordinate is unresolved.");
        return values.Select(v => (double)v.N / (double)v.D).ToArray();
    }
    public static Line Segment(IElement a, IElement b, string role, bool dashed = false) => new([XY(a), XY(b)], role, dashed);
    private static Line[] SceneLines(ShapeInterface facade, Relation roof, Relation door)
    {
        var points = facade.CurrentBindings.Select(b => b.Right).ToArray(); var lines = new List<Line>();
        for (var i = 1; i < 4; i++) lines.Add(Segment(points[i], points[(i + 1) % 4], "wall"));
        lines.Add(Segment(points[0], points[1], "guide", true));
        var roofEdges = F(F(Attachments.Read(roof, facade.CurrentBindings))[2]).Cast<Relation>();
        foreach (var edge in roofEdges) lines.Add(Segment(edge.Left, edge.Right, "roof"));
        var doorParts = F(F(Attachments.Read(door, facade.CurrentBindings))[2]);
        foreach (var edge in F(doorParts[2]).Cast<Relation>()) lines.Add(Segment(edge.Left, edge.Right, "wall"));
        foreach (var stroke in F(doorParts[3]))
        {
            var structure = F(stroke);
            double[][] samples;
            if (structure.Length == 5)
            {
                // The native machine determines the curve. The renderer merely
                // chooses sample resolution and converts its exact outputs to pixels.
                var parameter = F(structure[0])[0];
                var grade = (Carrier)F(structure[4])[4];
                samples = Enumerable.Range(0, 33).Select(i =>
                {
                    var read = MachineRunner.Run((Relation)stroke,
                        new Relation(parameter, ProjectionResidue.Create(grade, i, 32)));
                    return XY(F(F(read)[2])[0]);
                }).ToArray();
            }
            else samples = structure.Select(XY).ToArray();
            lines.Add(new Line(samples, "door"));
        }
        return lines.ToArray();
    }
}
