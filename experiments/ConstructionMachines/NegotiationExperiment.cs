using System.Numerics;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>
/// Authored starting worlds and derived presentation only. The bounded driver
/// schedules the native connections; it never selects a final geometry.
/// </summary>
public static class NegotiationExperiment
{
    private sealed record Setup(string Id, string Title, string Description, string[] Notes,
        Func<ConstructionRecipes, Policy, NegotiatingMachine> Wall,
        long ResponseN = 1, long ResponseD = 4, long Step = 10000, long Quantum = 1,
        bool FixedWall = false, bool FixedPeers = false, bool SingleLink = false,
        int Rounds = 16, int SwapAt = 0);

    // This is a builder convenience. Its contents become connected native routes;
    // the engine does not receive the object or dispatch on a construction name.
    private sealed record Policy(long N, long D, long Step, long Quantum, bool Fixed);
    private sealed record DisplayPort(string label, double[] point, string value);
    private sealed record DisplayLink(string label, double[] from, double[] to, string residual);
    private sealed record Metrics(double mismatch, double movement, double blocked, double remainder);
    private sealed record Round(int index, string status, Program.Line[] lines, Program.Line[] ghostLines,
        DisplayPort[] ports, DisplayLink[] links, Metrics metrics, string[] events);
    private sealed record Scene(string id, string title, string description, string[] notes, List<Round> rounds);

    public static void Write(string outputFolder)
    {
        var folder = Path.GetFullPath(outputFolder);
        Directory.CreateDirectory(folder);
        Setup[] setups =
        [
            new("path", "A path holds its starting side", "The roof asks for a wider span. The doorway shares the lower-left reference. Requests enter the retained repeated path through its shared long step.",
                ["Changing the long step moves both right-hand corners together.", "The path's starting side is fixed in this selected construction. A request there remains blocked; the neighbouring machine may still move."], BuildPath),
            new("offsets", "Offset families can translate and widen", "The same starting drawing and connections now use a movable origin and an independent span.",
                ["A left-side request can move the origin. That also moves the right side, so later publications affect the other connection.", "Requests at both sides enter distinct, explicitly connected parameter routes."], Offsets),
            new("centre", "Opposite pairs preserve their centre", "The same requests enter one shared half-span with opposite signs at its two sides.",
                ["Widening the right side moves the left side the other way.", "Roof and doorway requests can oppose one another on the same actuator. The combined request is accounted before the parameter moves."], Centre),
            new("swap", "Replace the inside, retain the connections", "After the first round, a repeated path is replaced by centre-and-opposite-pair construction with exactly the same visible points.",
                ["The replacement itself causes no geometric jump. The actual public sites and links remain the same.", "Old response routes are explicitly retired and new ones bound. Subsequent roof and doorway requests can now move the two sides in opposite directions."], BuildPath,
                SwapAt: 2),
            new("agreement", "An isolated pair reaches agreement", "Both machines take half of one captured mismatch at the roof's right foot.",
                ["Both proposals use the same earlier publications; the second machine does not chase the first machine's new value.", "Remaining rounds are retained even when there is no further change. Agreement concerns this selected connection only."], BuildPath,
                ResponseD: 2, SingleLink: true, Rounds: 8),
            new("anchored", "An anchored boundary still communicates", "The wall's response capacity is zero; the roof and doorway can adapt to its published sites.",
                ["A fixed response policy does not claim that the wall's observation is more certain.", "Blocked proposals remain in the receipts. They are not repeatedly injected as new carrier tension."], Offsets,
                FixedWall: true),
            new("conflict", "No movement does not mean agreement", "All three constructions retain their starting parameters, despite the visible mismatches.",
                ["Every round records the still-unmet requests.", "The fixed round budget ends without forcing a compromise or declaring the drawing settled."], Offsets,
                FixedWall: true, FixedPeers: true, Rounds: 8),
            new("capacity", "The same links, a smaller movement budget", "Each actuator can admit at most two ticks of change per round after combining incoming requests.",
                ["Capacity belongs to the receiving parameter route. Two connections do not each receive a fresh copy of that capacity.", "The final round can still be moving. A budget ending is a scheduling event, not convergence."], Offsets,
                ResponseD: 2, Step: 2, Rounds: 12),
            new("coarse", "Coarse changes leave a visible remainder", "The response routes move in eight-tick packets. Smaller proposals stay visible without manufacturing motion.",
                ["A small mismatch can remain when neither side admits a full packet.", "This experiment does not automatically increase resolution or reinterpret a quantization remainder as measurement noise."], Centre,
                ResponseD: 2, Quantum: 8, Rounds: 12),
            new("oscillation", "Taking the whole correction can oscillate", "Each side of the isolated connection takes the full earlier mismatch.",
                ["The two right-hand positions exchange places each round.", "Repeated-state observations are shown, but the selected budget still controls this run. There is no automatic damping policy yet."], BuildPath,
                ResponseD: 1, SingleLink: true, Rounds: 10)
        ];
        var scenes = new List<Scene>();
        var worlds = new List<IElement>();
        foreach (var setup in setups)
        {
            var c = new ConstructionRecipes();
            var wallPolicy = new Policy(setup.ResponseN, setup.ResponseD, setup.Step, setup.Quantum, setup.FixedWall);
            var peerPolicy = wallPolicy with { Fixed = setup.FixedPeers };
            var wall = setup.Wall(c, wallPolicy);
            var roof = Roof(c, peerPolicy, setup.SingleLink ? 100 : 80);
            var door = Door(c, peerPolicy);
            var left = new NegotiationLink(wall.Port(3, 0), roof.Port(0, 0));
            var right = new NegotiationLink(wall.Port(2, 0), roof.Port(1, 0));
            var floor = new NegotiationLink(wall.Port(0, 0), door.Port(0, 0), offset: 30);
            var links = setup.SingleLink ? new[] { right } : new[] { left, right, floor };
            var world = new NegotiationWorld([wall, roof, door], links);
            var controller = Relation.Segment(new Carrier(), 0, setup.Rounds);
            TimeStepSequence.TryReadSpan(controller, out var startRound, out var endRound);
            var roundBudget = checked((int)(endRound.Ticks - startRound.Ticks));
            var initial = Lines(wall, roof, door);
            var rounds = new List<Round>();
            var earlierCoordinates = Coordinates(wall, roof, door);
            var stateHistory = new List<string>();
            Capture(0, 0, []);
            for (var i = 1; i <= roundBudget; i++)
            {
                var changed = i == setup.SwapAt;
                if (changed) ReplacePathByCentre(c, wall, wallPolicy);
                world.Step();
                var current = Coordinates(wall, roof, door);
                var movement = current.Zip(earlierCoordinates).Select(pair => Math.Abs(pair.First - pair.Second)).DefaultIfEmpty().Max();
                earlierCoordinates = current;
                var events = Receipts(wall, roof, door);
                if (changed) events = ["Before this round: the path was replaced by an opposite-pair machine at identical coordinates. Public sites and links were retained; response routes were explicitly rebound.", .. events];
                Capture(i, movement, events);
            }
            scenes.Add(new Scene(setup.Id, setup.Title, setup.Description, setup.Notes, rounds));
            worlds.Add(G(controller, world.Surface));
            Console.WriteLine($"{setup.Id}: {setup.Rounds} rounds, mismatch {rounds[^1].metrics.mismatch:0.###}, movement {rounds[^1].metrics.movement:0.###}");

            void Capture(int index, double movement, string[] events)
            {
                var portPairs = setup.SingleLink
                    ? new[] { (right, wall.Port(2, 0), roof.Port(1, 0), "Right roof foot", wall.Point(2), roof.Point(1)) }
                    : new[] { (left, wall.Port(3, 0), roof.Port(0, 0), "Left roof foot", wall.Point(3), roof.Point(0)),
                        (right, wall.Port(2, 0), roof.Port(1, 0), "Right roof foot", wall.Point(2), roof.Point(1)),
                        (floor, wall.Port(0, 0), door.Port(0, 0), "Door jamb · base reference + 30", wall.Point(0), door.Point(0)) };
                var displayLinks = new List<DisplayLink>(); double mismatch = 0;
                foreach (var (link, a, b, label, pa, pb) in portPairs)
                {
                    var ownerB = label.StartsWith("Door", StringComparison.Ordinal) ? door : roof;
                    var residual = NegotiationLink.Residual(link.Plan(wall.Publish(a), ownerB.Publish(b)));
                    mismatch += Math.Abs(Number(residual));
                    displayLinks.Add(new DisplayLink(label, Program.XY(pa), Program.XY(pb), Text(residual)));
                }
                var currentSignature = string.Join(",", Coordinates(wall, roof, door).Select(value => value.ToString("R", System.Globalization.CultureInfo.InvariantCulture)));
                var repeating = stateHistory.Count >= 2 && stateHistory[^2] == currentSignature && movement > 0;
                var status = index == 0 ? "Starting publications"
                    : mismatch == 0 ? "Agreement on the selected connections"
                    : repeating ? "A previous state has returned"
                    : movement == 0 ? "No accepted motion · disagreement remains"
                    : "Continuing local adjustment";
                if (index == setup.Rounds) status += " · round budget ended";
                stateHistory.Add(currentSignature);
                var (blocked, remainder) = ReceiptMetrics(wall, roof, door);
                var ports = new[]
                {
                    Port("Boundary left", wall.Point(3)), Port("Boundary right", wall.Point(2)),
                    Port("Roof left", roof.Point(0)), Port("Roof right", roof.Point(1)),
                    Port("Door jamb", door.Point(0))
                };
                rounds.Add(new Round(index, status, Lines(wall, roof, door), initial, ports, displayLinks.ToArray(),
                    new Metrics(mismatch, movement, index == 0 ? 0 : blocked, index == 0 ? 0 : remainder), events));
            }
        }
        var native = GojoNumericFormat.Encode(G(worlds.ToArray()));
        var restored = GojoNumericFormat.Decode(native);
        if (restored.Length != 1 || F(restored[0]).Length != worlds.Count)
            throw new InvalidDataException("Negotiation archive did not retain every operational world.");
        // Rehydrate an independent world and take one more local round. The
        // encoded instructions and correspondences must suffice without builders.
        foreach (var state in F(restored[0])) NegotiationWorld.Restore((Relation)F(state)[1]).Step();
        File.WriteAllBytes(Path.Combine(folder, "negotiation.gojo.bin"), native);
        var report = new
        {
            summary = new[]
            {
                "Each publication is a selected interpretation. Grey starting shapes are constructed task references, not unprocessed measurements.",
                "Neighbours use exposed coordinates, correspondence and calibrated requests. Human shape names do not choose the engine's responses.",
                "All runs retain a fixed round budget. No motion, agreement, repeated states and the end of a budget are distinct observations.",
                "Mismatch is the sum of absolute connection residuals in this common X basis. Movement is the greatest public-coordinate change in the round. Response accounting is in the private actuator bases.",
                "The native archive retains the executable machines, response routes, connections and histories. This review only plays back derived observations."
            },
            scenarios = scenes
        };
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(folder, "negotiation-observations.json"), json);
        NegotiationReview.Write(Path.Combine(folder, "review.html"), json);
        Console.WriteLine($"{scenes.Count} worlds restored and continued; review: {Path.Combine(folder, "review.html")}");
    }

    private static NegotiatingMachine BuildPath(ConstructionRecipes c, Policy policy)
    {
        var width = Program.Parameter(120); var height = Program.Parameter(80);
        var local = new NegotiatingMachine(c.Path(c.Point(100, 0), c.Point(1, 0), [width, height], 2));
        Response(local, local.Port(2, 0), width, policy);
        return local;
    }

    private static NegotiatingMachine Offsets(ConstructionRecipes c, Policy policy)
    {
        var origin = Program.Parameter(100); var width = Program.Parameter(120); var height = Program.Parameter(80);
        var local = new NegotiatingMachine(c.Offsets(c.Times(c.Point(1, 0), origin.ValueSlot, origin), width, height));
        Response(local, local.Port(0, 0), origin, policy);
        Response(local, local.Port(3, 0), origin, policy);
        Response(local, local.Port(2, 0), width, policy);
        return local;
    }

    private static NegotiatingMachine Centre(ConstructionRecipes c, Policy policy)
    {
        var halfWidth = Program.Parameter(60); var halfHeight = Program.Parameter(40);
        var local = new NegotiatingMachine(c.OppositePairs(c.Point(160, 40), halfWidth, halfHeight));
        Response(local, local.Port(0, 0), halfWidth, policy, -1);
        Response(local, local.Port(3, 0), halfWidth, policy, -1);
        Response(local, local.Port(2, 0), halfWidth, policy);
        return local;
    }

    private static NegotiatingMachine Roof(ConstructionRecipes c, Policy policy, long start)
    {
        var origin = Program.Parameter(start); var width = Program.Parameter(260 - start);
        var left = c.Plus(c.Point(0, 80), c.Times(c.Point(1, 0), origin.ValueSlot, origin));
        var right = c.Plus(left, c.Times(c.Point(1, 0), width.ValueSlot, width));
        var ridge = c.Plus(c.Between(left, right, 1, 2), c.Point(0, 45));
        // Two-tick width changes keep the ridge midpoint representable in this
        // selected whole-point interface. This is a response quantum, not a
        // claim about measurement accuracy or confidence.
        var local = new NegotiatingMachine(Direct(c, G(origin, width), G(left, right, ridge)));
        var roofPolicy = policy with { Quantum = Math.Max(2, policy.Quantum) };
        Response(local, local.Port(0, 0), origin, roofPolicy);
        Response(local, local.Port(1, 0), width, roofPolicy);
        return local;
    }

    private static void ReplacePathByCentre(ConstructionRecipes c, NegotiatingMachine machine, Policy policy)
    {
        static long Whole(IElement scalar)
        {
            if (!ProjectionResidue.TryRead(scalar, out _, out var n, out var d) || d != 1)
                throw new InvalidDataException("This authored replacement requires whole public coordinates.");
            return checked((long)n);
        }
        var lo = F(machine.Point(0)).Select(Whole).ToArray();
        var hi = F(machine.Point(2)).Select(Whole).ToArray();
        if ((hi[0] - lo[0]) % 2 != 0 || (hi[1] - lo[1]) % 2 != 0)
            throw new InvalidDataException("This authored opposite-pair replacement requires whole half-spans.");
        var halfWidth = Program.Parameter((hi[0] - lo[0]) / 2);
        var halfHeight = Program.Parameter((hi[1] - lo[1]) / 2);
        var definition = c.OppositePairs(c.Point((hi[0] + lo[0]) / 2, (hi[1] + lo[1]) / 2), halfWidth, halfHeight);
        var fields = F(definition); var points = F(fields[1]);
        // The retained path exposes its terminal return as a fifth role. The
        // replacement explicitly preserves that ordered public contract.
        var replacement = TransformSurface.Create(new Carrier(), fields[0], G([.. points, points[0]]),
            G(Enumerable.Range(0, 5).Select(_ => (IElement)new Placeholder()).ToArray()), fields[3], fields[4]);
        if (!machine.TryReplaceDefinition(replacement, out _))
            throw new InvalidDataException("A geometry-preserving local replacement was rejected.");
        Response(machine, machine.Port(0, 0), halfWidth, policy, -1);
        Response(machine, machine.Port(3, 0), halfWidth, policy, -1);
        Response(machine, machine.Port(2, 0), halfWidth, policy);
    }

    private static NegotiatingMachine Door(ConstructionRecipes c, Policy policy)
    {
        var origin = Program.Parameter(130);
        var local = new NegotiatingMachine(c.Offsets(c.Times(c.Point(1, 0), origin.ValueSlot, origin), Program.Parameter(60), Program.Parameter(30)));
        Response(local, local.Port(0, 0), origin, policy);
        return local;
    }

    private static Relation Direct(ConstructionRecipes c, Relation inputs, Relation points) =>
        TransformSurface.Create(new Carrier(), inputs, points,
            G(F(points).Select(_ => (IElement)new Placeholder()).ToArray()), G(), G(c.Basis, c.Metric));

    private static void Response(NegotiatingMachine machine, Relation port, Carrier actuator, Policy policy, long gain = 1) =>
        machine.AddResponse(port, actuator, gainN: gain, responseN: policy.N, responseD: policy.D,
            min: 1, max: 1000, maxStep: policy.Fixed ? 0 : policy.Step, quantum: policy.Quantum);

    private static Program.Line[] Lines(NegotiatingMachine wall, NegotiatingMachine roof, NegotiatingMachine door)
    {
        var lines = new List<Program.Line>();
        for (var i = 0; i < 4; i++) lines.Add(Program.Segment(wall.Point(i), wall.Point((i + 1) % 4), "wall"));
        lines.Add(Program.Segment(roof.Point(0), roof.Point(2), "roof"));
        lines.Add(Program.Segment(roof.Point(2), roof.Point(1), "roof"));
        lines.Add(Program.Segment(roof.Point(1), roof.Point(0), "roof"));
        for (var i = 0; i < 4; i++) lines.Add(Program.Segment(door.Point(i), door.Point((i + 1) % 4), "door"));
        return lines.ToArray();
    }

    private static double[] Coordinates(params NegotiatingMachine[] machines) =>
        machines.SelectMany(machine => machine.Points.SelectMany(Program.XY)).ToArray();
    private static DisplayPort Port(string label, IElement point) => new(label, Program.XY(point),
        $"({string.Join(", ", F(point).Select(Text))})");
    private static double Number(IElement element) => ProjectionResidue.TryRead(element, out _, out var n, out var d)
        ? (double)n / (double)d : throw new InvalidDataException("An unresolved scalar cannot be displayed as zero.");
    private static string Text(IElement element) => ProjectionResidue.TryRead(element, out _, out var n, out var d)
        ? d == BigInteger.One ? n.ToString() : $"{n}/{d}" : "unresolved";

    // These are reads of native receipts. No statistics are inferred from pixels
    // or from a human-facing construction label.
    private static string[] Receipts(params NegotiatingMachine[] machines)
    {
        var events = new List<string>();
        for (var i = 0; i < machines.Length; i++)
        {
            var name = new[] { "Boundary", "Roof", "Doorway" }[i];
            if (machines[i].LastReceipt is not Relation receipt)
            { events.Add($"{name}: no request was addressed to this machine in this round."); continue; }
            foreach (var result in NegotiatingMachine.Outcomes(receipt))
                events.Add($"{name}: combined parameter request {Text(result.Requested)} → accepted {Text(result.Accepted)}; " +
                    $"bounds blocked {Text(result.BoundsBlocked)}, step deferred {Text(result.CapacityDeferred)}, " +
                    $"quantum remainder {Text(result.QuantumRemainder)}, candidate blocked {Text(result.CandidateBlocked)}.");
            foreach (var request in Unrouted(receipt))
                if (Number(F(request)[3]) != 0)
                    events.Add($"{name}: public displacement {Text(F(request)[3])} has no selected response route and remains unadmitted.");
        }
        return events.ToArray();
    }

    private static (double Blocked, double Remainder) ReceiptMetrics(params NegotiatingMachine[] machines)
    {
        double blocked = 0, remainder = 0;
        foreach (var machine in machines)
        {
            if (machine.LastReceipt is not Relation receipt) continue;
            foreach (var result in NegotiatingMachine.Outcomes(receipt))
            {
                blocked += Math.Abs(Number(result.BoundsBlocked)) + Math.Abs(Number(result.CapacityDeferred)) + Math.Abs(Number(result.CandidateBlocked));
                remainder += Math.Abs(Number(result.QuantumRemainder));
            }
            // Unrouted proposals remain public-coordinate requests, not private
            // actuator amounts. Display them individually in receipts instead
            // of adding unlike bases to this parameter diagnostic.
        }
        return (blocked, remainder);
    }

    private static Relation[] Unrouted(Relation receipt) => F(F(receipt)[3]) is { Length: 2 } parts
        ? F(parts[0]).Cast<Relation>().ToArray() : [];
}
