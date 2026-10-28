using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>Authored proposals and a fixed schedule; no global shape answer is supplied to local response.</summary>
public static class JointExperiment
{
    private sealed record Node(string id, string label, double[] point, int degree, int[] external);
    private sealed record Edge(string id, string from, string to);
    private sealed record Geometry(Node[] nodes, Edge[] edges);
    private sealed record Stats(double gap, double movement, int sites, int branches, int groups, int openPaths);
    private sealed record Frame(int index, int round, string phase, string caption, Geometry geometry, Stats stats, string[] events);
    private sealed record Scene(string id, string title, string description, string[] lookFor, Geometry initial, Geometry target, List<Frame> frames);

    public static void Write(string outputFolder)
    {
        var folder = Path.GetFullPath(outputFolder); Directory.CreateDirectory(folder);
        var scenes = new List<Scene>(); var roots = new List<IElement>();
        Add("rigid-fan", "A T cannot follow diagonal targets", "A control: the arms share a fixed horizontal construction. The diagonal requests stay visible.",
            ["The hub and three branches are connected throughout.", "A continuing mismatch does not itself authorize changing the construction."], BuildFan(false));
        Add("relax", "Release the arms: T → flat Y", "At pass 4, replace the fixed arms with a shared rise construction at exactly the same positions. Subsequent requests can raise both arms.",
            ["The reinterpretation frame has no geometric jump.", "Connectivity stays the same. The changed internal dependency allows coordinated diagonal motion."], BuildFan(true));
        Add("rigid-split", "A shared stem cannot become two legs", "A control: two lower ports read one internal foot. Their separating targets cannot be followed by this construction.",
            ["Two exposed ports can read the same actual internal site.", "Giving them different target positions does not silently split that site."], BuildSplit(false));
        Add("split", "Split the hub and foot: T → two corners", "At pass 4, an explicit split creates two hubs and two feet. They initially coincide, then the corner constructions can move apart.",
            ["The new graph has two connected groups, even in the unchanged-position frame.", "The public ports retain identity while their internal correspondence changes."], BuildSplit(true));
        Add("retire", "Remove a branch, retain its obligation", "The upper branch is retired at pass 3 and restored at pass 7. Its remembered upper site remains visible in the graph.",
            ["Coordinate mismatch stays zero while the requested upper-to-left path becomes unresolved.", "Restoring the retained edge restores the path. Removing structure does not erase the neighbour's demand."], BuildRetirement());
        Add("fold", "Coincident corners still need a merge", "Two corner constructions start on top of one another. At pass 4 an explicit fold makes their hubs and feet shared.",
            ["Before merging, equal coordinates leave two disconnected groups.", "After merging, the drawing stays in place while the requested path becomes readable."], BuildFold());

        var archive = GojoNumericFormat.Encode(G(roots.ToArray()));
        var restored = GojoNumericFormat.Decode(archive);
        if (restored.Length != 1 || F(restored[0]).Length != scenes.Count) throw new InvalidDataException("The native joint archive is incomplete.");
        foreach (var saved in F(restored[0]))
        {
            var f = F(saved); var world = NegotiationWorld.Restore((Relation)f[2]);
            var joint = JointAssembly.Restore((Relation)f[1], world.Machines[0]);
            joint.Step(world);
        }
        File.WriteAllBytes(Path.Combine(folder, "joints.gojo.bin"), archive);
        var json = JsonSerializer.Serialize(new
        {
            summary = new[] { "Construction names are presentation labels. Native incidence, dependencies, correspondence and response routes determine behaviour.",
                "The connection graph is a schematic view, not a spatial measurement. Coincident positions do not join sites.",
                "Targets and starting drawings are interpreted, authored references. Rewrite proposals and their timing are authored; no classifier or learning model is used.",
                "Each run retains twelve numeric passes. Reinterpretations are separate frames and separate native transitions.",
                "Gap is a display sum of absolute endpoint-coordinate residuals in this fixture's calibrated page basis. Path obligations are a separate structural read.",
                "The native archive retains executable constructions, rewrites, obligations, correspondence and histories. Every saved world was independently restored and advanced." }, scenarios = scenes
        }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(folder, "joint-observations.json"), json);
        JointReview.Write(Path.Combine(folder, "review.html"), json);
        Console.WriteLine($"{scenes.Count} joint experiments, {scenes.Sum(s => s.frames.Count)} observations; native worlds restored and continued; {Path.Combine(folder, "review.html")}");

        void Add(string id, string title, string description, string[] lookFor, Study study)
        {
            var joint = study.Joint;
            // A target is a captured external reference, not another invocation
            // sharing the live construction's mutable parameter carriers.
            var targetDefinition = (Relation)Program.CaptureObservation(study.Target);
            var target = new NegotiatingMachine(JointGraph.Machine(targetDefinition));
            var localCarriers = JointGraph.Machine(joint.Definition).Carriers().Where(c => !c.IsUnknown).ToHashSet();
            if (target.Definition.Carriers().Where(c => !c.IsUnknown).Any(localCarriers.Contains))
                throw new InvalidDataException("The reference must not share private live parameters or bases.");
            var links = Enumerable.Range(0, 4).SelectMany(i => Enumerable.Range(0, 2).Select(axis =>
                new NegotiationLink(joint.Mechanical.Port(i, axis), target.Port(i, axis)))).ToArray();
            var world = new NegotiationWorld([joint.Mechanical, target], links);
            var nodeIds = new Dictionary<IElement, string>(ReferenceEqualityComparer.Instance);
            var edgeIds = new Dictionary<IElement, string>(ReferenceEqualityComparer.Instance);
            var frames = new List<Frame>();
            var initial = Draw(joint.Definition, joint.CurrentRead);
            var targetGeometry = Draw(targetDefinition, JointGraph.Observe(targetDefinition, G()));
            var previousCoordinates = Coordinates();
            Capture(0, "Start", "Initial construction and exposed target positions.", []);
            var controller = Relation.Segment(new Carrier(), 0, 12);
            TimeStepSequence.TryReadSpan(controller, out var from, out var to);
            for (var round = (int)from.Ticks + 1; round <= to.Ticks; round++)
            {
                var change = study.Change(round);
                if (change is not null) Capture(round, "Reinterpretation", change, [change, "Existing public point/port identities and their coordinates were retained."]);
                joint.Step(world);
                var outcomes = joint.Mechanical.LastReceipt is Relation receipt ? NegotiatingMachine.Outcomes(receipt) : [];
                var events = outcomes.Select(o => $"Selected parameter request {Scalar(o.Requested)} → admitted {Scalar(o.Accepted)}; " +
                    $"bounds blocked {Scalar(o.BoundsBlocked)}, step deferred {Scalar(o.CapacityDeferred)}, quantum remainder {Scalar(o.QuantumRemainder)}, candidate blocked {Scalar(o.CandidateBlocked)}.").ToArray();
                Capture(round, "Local response", change is null ? "Captured publications → addressed requests → local parameter response → new read." : "Requests now enter the newly selected construction.", events);
            }
            scenes.Add(new Scene(id, title, description, lookFor, initial, targetGeometry, frames));
            roots.Add(G(controller, joint.Surface, world.Surface, targetDefinition));
            Console.WriteLine($"{id}: gap {frames[^1].stats.gap}, groups {frames[^1].stats.groups}, open paths {frames[^1].stats.openPaths}");

            void Capture(int round, string phase, string caption, string[] events)
            {
                var current = Coordinates(); var motion = current.Zip(previousCoordinates).Select(p => Math.Abs(p.First - p.Second)).DefaultIfEmpty().Max();
                previousCoordinates = current;
                var geometry = Draw(joint.Definition, joint.CurrentRead);
                var gap = world.Compare().Sum(p => Math.Abs(Number(NegotiationLink.Residual(p))));
                var pathReads = F(F(joint.CurrentRead)[2])[2];
                var open = F(pathReads).Count(p => F(p)[2] is Placeholder);
                frames.Add(new Frame(frames.Count, round, phase, caption, geometry,
                    new Stats(gap, motion, geometry.nodes.Length, geometry.edges.Length, Groups(geometry), open), events));
            }
            double[] Coordinates() => joint.Mechanical.Points.SelectMany(Program.XY).ToArray();
            Geometry Draw(Relation definition, Relation read)
            {
                var exposures = JointGraph.Exposures(definition); var edges = JointGraph.Edges(definition);
                var positions = F(F(read)[2])[0];
                var nodes = F(positions).Cast<Relation>().Select(binding =>
                {
                    var identity = Id(nodeIds, binding.Left, "n");
                    var degree = edges.Count(e => ReferenceEquals(e.Left, binding.Left) || ReferenceEquals(e.Right, binding.Left));
                    return new Node(identity, $"Site {identity[1..]}", Program.XY(binding.Right), degree,
                        exposures.Select((e, i) => ReferenceEquals(e.Right, binding.Left) ? i + 1 : -1).Where(i => i > 0).ToArray());
                }).ToArray();
                return new Geometry(nodes, edges.Select(e => new Edge(Id(edgeIds, e, "e"), Id(nodeIds, e.Left, "n"), Id(nodeIds, e.Right, "n"))).ToArray());
            }
        }
    }

    private sealed record Study(JointAssembly Joint, Relation Target, Func<int, string?> Change);
    private static Study BuildFan(bool relax)
    {
        var r = new JointRecipes(); var joint = new JointAssembly(r.Tee(), G());
        var rise = Program.Parameter(0); var target = r.Fan(Program.Parameter(40));
        return new(joint, target, round =>
        {
            if (!relax || round != 4) return null;
            Rewrite(joint, r.Fan(rise), JointRecipes.Carry(JointGraph.Nodes(joint.Definition)), JointRecipes.Carry(JointGraph.Edges(joint.Definition)));
            joint.Mechanical.AddResponse(joint.Mechanical.Port(0, 1), rise, min: 0, max: 100, responseD: 4);
            joint.Mechanical.AddResponse(joint.Mechanical.Port(1, 1), rise, min: 0, max: 100, responseD: 4);
            return "The fixed arm-height dependency was replaced by a shared rise parameter. The common hub and all three edges were carried.";
        });
    }
    private static Study BuildSplit(bool split)
    {
        var r = new JointRecipes(); var joint = new JointAssembly(r.Tee(), G()); var spread = Program.Parameter(0);
        return new(joint, r.Corners(Program.Parameter(40)), round =>
        {
            if (!split || round != 4) return null;
            Rewrite(joint, r.Corners(spread), r.TeeToCornersNodes(), r.TeeToCornersEdges());
            joint.Mechanical.AddResponse(joint.Mechanical.Port(2, 0), spread, gainN: -1, min: 0, max: 60, responseD: 4);
            joint.Mechanical.AddResponse(joint.Mechanical.Port(3, 0), spread, min: 0, max: 60, responseD: 4);
            return "The common hub and foot were each split into two distinct sites. The stem became two legs; the two corner groups now have a shared spread parameter.";
        });
    }
    private static Study BuildRetirement()
    {
        var r = new JointRecipes(); var joint = new JointAssembly(r.Cross(), G(new Relation(r.External[3], r.External[0])));
        return new(joint, r.Cross(), round =>
        {
            if (round is not (3 or 7)) return null;
            var add = round == 7;
            Rewrite(joint, r.Cross(add), JointRecipes.Carry(JointGraph.Nodes(joint.Definition)), r.CrossEdgeChange(add));
            return add ? "The retained upper branch was reintroduced. The upper-to-left route is readable again."
                : "The upper branch was retired. Its coordinate remains remembered, but its requested path to the left neighbour is unresolved.";
        });
    }
    private static Study BuildFold()
    {
        var r = new JointRecipes(); var joint = new JointAssembly(r.Corners(Program.Parameter(0)), G(new Relation(r.External[2], r.External[3])));
        return new(joint, r.Tee(), round =>
        {
            if (round != 4) return null;
            Rewrite(joint, r.Tee(), r.CornersToTeeNodes(), r.CornersToTeeEdges());
            return "Two distinct hubs were folded into one shared hub, and the feet into one shared foot. The lower-to-lower path is now readable without moving any public coordinate.";
        });
    }
    private static void Rewrite(JointAssembly joint, Relation candidate, Relation nodes, Relation edges)
    {
        if (!joint.TryRewrite(candidate, nodes, edges, out _)) throw new InvalidDataException("An authored same-position rewrite failed.");
    }
    private static string Id(Dictionary<IElement, string> map, IElement element, string prefix)
    {
        if (!map.TryGetValue(element, out var id)) map[element] = id = prefix + (map.Count + 1);
        return id;
    }
    private static int Groups(Geometry geometry)
    {
        var remaining = geometry.nodes.Select(n => n.id).ToHashSet(); int count = 0;
        while (remaining.Count > 0)
        {
            count++; var queue = new Queue<string>(); var first = remaining.First(); remaining.Remove(first); queue.Enqueue(first);
            while (queue.Count > 0)
            {
                var at = queue.Dequeue();
                foreach (var edge in geometry.edges)
                {
                    var next = edge.from == at ? edge.to : edge.to == at ? edge.from : null;
                    if (next is not null && remaining.Remove(next)) queue.Enqueue(next);
                }
            }
        }
        return count;
    }
    private static double Number(IElement scalar) => ProjectionResidue.TryRead(scalar, out _, out var n, out var d) ? (double)n / (double)d : throw new InvalidDataException("Unresolved is not numeric zero.");
    private static string Scalar(IElement scalar) => ProjectionResidue.TryRead(scalar, out _, out var n, out var d) ? d == 1 ? n.ToString() : $"{n}/{d}" : "unresolved";
}
