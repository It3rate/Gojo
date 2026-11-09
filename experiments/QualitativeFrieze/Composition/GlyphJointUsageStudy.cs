using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.Composition.GlyphJointUsage;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Authored experiments, not learned handwriting or automatic noun
/// recognition. The builder supplies connected rules; the shared reader and
/// packet transport execute them without inspecting a glyph or policy name.</summary>
public static class GlyphJointUsageStudy
{
    public sealed class Fixture
    {
        public GlyphJointUsage Model { get; }
        public IReadOnlyDictionary<string, Relation> Ports { get; }
        public IReadOnlyDictionary<string, IElement> Controls { get; }
        public IReadOnlyDictionary<string, Relation> Routes { get; }
        public GlyphJointUsageCase Description { get; }
        private readonly List<GlyphJointUsageProbe> probes = [];

        internal Fixture(GlyphJointUsage model, Dictionary<string, Relation> ports,
            Dictionary<string, IElement> controls, Dictionary<string, Relation> routes, GlyphJointUsageCase description)
        { Model = model; Ports = ports; Controls = controls; Routes = routes; Description = description; }

        public void Seed(string port, int packets = 1)
        {
            var receipt = Model.Import(Ports[port], Enumerable.Range(0, packets).Select(_ => (IElement)new Placeholder()).ToArray());
            if (!Model.Transport.TryReadAccepted(receipt, out _, out _)) throw new InvalidOperationException("Fixture import exceeds its declared capacity.");
        }

        public GlyphJointUsage.Read Probe(string id, string[] inputs, string[] controls, string explanation,
            bool openControl = false)
        {
            var support = controls.Select(name => Controls[name]).ToArray();
            var read = Model.Apply(S(inputs.Select(name => (IElement)Ports[name]).ToArray()),
                openControl ? StructuralConstraint.Open(support) : S(support));
            string RouteName(IElement route) => Routes.First(pair => ReferenceEquals(pair.Value, route)).Key;
            var destinations = E(read.Accepted).SelectMany(packet =>
                Ports.Where(pair => E(Model.Transport.PortPackets(pair.Value)).Any(held => ReferenceEquals(held, packet)))
                    .Select(pair => pair.Key)).Distinct().ToArray();
            probes.Add(new(id, string.Join(" + ", inputs), controls, E(read.Candidates).Select(RouteName).ToArray(),
                read.Outcome, E(read.Incoming).Length, E(read.Accepted).Length, E(read.Blocked).Length,
                E(read.Remainder).Length, destinations, read.OwnershipPreserved, explanation));
            return read;
        }

        public GlyphJointUsageCase Result() => Description with
        {
            Probes = probes.ToArray(),
            NativeRoot = new Relation(Model.Surface, new Relation(Model.Transport.Timeline,
                new Relation(Model.Timeline, Model.Transport.State)))
        };
    }

    private sealed record Rule(string Input, string Control, string[] Routes, bool Open = false);

    public static GlyphJointUsageReport Run()
    {
        var cases = new List<GlyphJointUsageCase>();
        var bend = Bend(false, true); bend.Seed("top");
        bend.Probe("forward", ["top"], ["run"], "One packet follows the declared turn from the top through the corner to the right.");
        bend.Probe("backward", ["right"], ["run"], "The same visible bend does not imply a reverse route. That closed law admits no return."); cases.Add(bend.Result());

        var attached = Bend(true, false); attached.Seed("top");
        attached.Probe("unprovided-use", ["top"], ["run"], "Two strokes meet at a shared site, but the use law is open. Attachment is known; passage is not."); cases.Add(attached.Result());

        var connected = Bend(true, true); connected.Seed("top");
        connected.Probe("connected-use", ["top"], ["run"], "An explicit passage makes the separately attached pieces behave like the continuous bend for this input."); cases.Add(connected.Result());

        var stop = Tee("t-stop", "T as a terminal", "The stem reaches a shared crossbar site.",
            "A load or message can arrive without permission to leave through either arm.", [], []);
        stop.Seed("bottom"); stop.Probe("stop", ["bottom"], ["run"], "The empty closed law blocks the packet, which remains at its source."); cases.Add(stop.Result());

        var through = Tee("t-through", "T as a through path with an unused stem", "The bar is drawn continuously; the stem is attached later.",
            "The bar can carry a stream while the stem remains an unselected possibility.",
            [R("across", "left", "right", "joint"), R("return", "right", "left", "joint")],
            [new("left", "run", ["across"]), new("right", "run", ["return"])]);
        through.Seed("left"); through.Probe("across", ["left"], ["run"], "The legal horizontal route crosses the branch site without turning.");
        through.Seed("bottom"); through.Probe("stem-blocked", ["bottom"], ["run"], "A geometric T does not grant its stem an outgoing route."); cases.Add(through.Result());

        var choice = Tee("t-choice", "T as an unresolved choice", "Three arms share a site.",
            "Two legal outlets are alternatives for one packet, not copies of it.",
            [R("take-left", "bottom", "left", "joint"), R("take-right", "bottom", "right", "joint")],
            [new("bottom", "run", ["take-left", "take-right"])]);
        choice.Seed("bottom"); choice.Probe("choose-later", ["bottom"], ["run"], "Both alternatives are preserved. No packet moves until a choice is supplied."); cases.Add(choice.Result());

        var knownSwitch = Switch(); knownSwitch.Seed("bottom");
        knownSwitch.Probe("control-left", ["bottom"], ["left"], "The selected control admits only the left outlet."); cases.Add(knownSwitch.Result());
        var uncertainSwitch = Switch("t-switch-uncertain", "T switch with uncertain control"); uncertainSwitch.Seed("bottom");
        uncertainSwitch.Probe("either-control", ["bottom"], ["left", "right"], "The control could select either side. The packet stays owned once at the source."); cases.Add(uncertainSwitch.Result());
        var incompleteSwitch = Switch("t-switch-open", "T switch with an open control read"); incompleteSwitch.Seed("bottom");
        incompleteSwitch.Probe("open-control", ["bottom"], ["left"], "A known left possibility plus an open tail does not justify committing to left.", openControl: true); cases.Add(incompleteSwitch.Result());

        var gate = Gate(); gate.Seed("bottom");
        gate.Probe("go-or-stop", ["bottom"], ["go", "stop"], "There is only one candidate route, but one possible control blocks it. The packet cannot move yet.");
        gate.Probe("two-controls-agree", ["bottom"], ["go", "also-go"], "Both remaining controls permit the same route. This coarser action is certain even though the control is not."); cases.Add(gate.Result());

        var merge = Merge(2); merge.Seed("left"); merge.Seed("right");
        merge.Probe("merge", ["left", "right"], ["run"], "Two independent packets enter one receiver with two sites. Both identities survive the merge."); cases.Add(merge.Result());
        var crowdedMerge = Merge(1); crowdedMerge.Seed("left"); crowdedMerge.Seed("right");
        crowdedMerge.Probe("capacity-conflict", ["left", "right"], ["run"], "One receiving site cannot hold two packets. The atomic batch is refused; neither packet is lost."); cases.Add(crowdedMerge.Result());

        var bridge = Bridge(false); bridge.Seed("left");
        bridge.Probe("cross", ["left"], ["run"], "The crossbar connects the stems and permits left-to-right passage.");
        bridge.Probe("return-blocked", ["right"], ["run"], "Drawing a connector, or calling it a bridge, does not supply its return route."); cases.Add(bridge.Result());
        var twoWay = Bridge(true); twoWay.Seed("left");
        twoWay.Probe("outward", ["left"], ["run"], "One packet crosses the explicit forward route.");
        twoWay.Probe("homeward", ["right"], ["run"], "The same packet returns through a separately encoded reverse route."); cases.Add(twoWay.Result());

        var crossing = Cross(false); crossing.Seed("west"); crossing.Seed("south");
        crossing.Probe("independent", ["west", "south"], ["run"], "Two streams cross in the drawing but use separate middle sites and never exchange."); cases.Add(crossing.Result());
        var joined = Cross(true); joined.Seed("west");
        joined.Probe("turn-at-cross", ["west"], ["run"], "A genuinely shared site plus an explicit turning route lets the west stream leave north."); cases.Add(joined.Result());

        return new(cases.ToArray()) { ArchiveRoot = S(cases.Select(item => (IElement)item.NativeRoot).ToArray()) };
    }

    public static Fixture Bend(bool attached, bool passage)
    {
        var points = new[] { P("top", 0, 2), P("joint", 0, 0), P("right", 2, 0) };
        var lines = new[] { L("stem", "top", "joint", 0), L("arm", "joint", "right", attached ? 1 : 0) };
        return Build(attached ? passage ? "l-attached-passage" : "l-attached-open" : "l-bend", attached ? passage ? "Attached corner with passage" : "Attached corner, use unresolved" : "L as a continuous bend", "L / F corner",
            attached ? "Draw the stem, lift, then attach the arm at the same site." : "Draw the stem and continue around the corner without lifting.",
            passage ? "A stream can follow the explicit turn." : "The pieces are joined, but the passage rule has not been supplied.",
            points, lines, [R("turn", "top", "right", "joint")], ["run"],
            [new("top", "run", passage ? ["turn"] : [], !passage)]);
    }

    public static Fixture Switch(string id = "t-switch", string title = "T as a controlled switch") => Tee(id, title,
        "Three arms share a site; a separate control is connected to the permission law.",
        "The control selects an outlet while the other outlet remains physically present.",
        [R("take-left", "bottom", "left", "joint"), R("take-right", "bottom", "right", "joint")],
        [new("bottom", "left", ["take-left"]), new("bottom", "right", ["take-right"])], controls: ["left", "right"]);

    public static Fixture Merge(int capacity) => Tee($"t-merge-{capacity}", capacity == 2 ? "T as a material merge" : "T merge with insufficient capacity",
        "The two arms and stem share one junction; two inlet routes point to one receiver.",
        "The shared receiver limits the combined input. Material identity is retained, unlike a Boolean OR read.",
        [R("left-in", "left", "bottom", "joint"), R("right-in", "right", "bottom", "joint")],
        [new("left", "run", ["left-in"]), new("right", "run", ["right-in"])], receiverCapacity: capacity);

    public static Fixture Gate() => Tee("t-move-or-stop", "T with uncertain permission to move",
        "The stem can reach the right arm through a control-gated passage.",
        "An uncertain control can mean move or hold, even when only one route is visible in the candidate union.",
        [R("outlet", "bottom", "right", "joint")],
        [new("bottom", "go", ["outlet"]), new("bottom", "also-go", ["outlet"]), new("bottom", "stop", [])],
        controls: ["go", "also-go", "stop"]);

    private static Fixture Tee(string id, string title, string construction, string use, GlyphJointUsageRoute[] routes,
        Rule[] rules, string[]? controls = null, int receiverCapacity = 2) => Build(id, title, "T", construction, use,
        [P("left", -2, 2), P("joint", 0, 2), P("right", 2, 2), P("bottom", 0, 0)],
        [L("bar-left", "left", "joint", 0), L("bar-right", "joint", "right", 0), L("stem", "joint", "bottom", 1)],
        routes, controls ?? ["run"], rules, new Dictionary<string, int> { ["bottom"] = receiverCapacity });

    public static Fixture Bridge(bool twoWay) => Build(twoWay ? "h-two-way" : "h-one-way", twoWay ? "H as a two-way bridge" : "H as a one-way bridge", "H",
        "Draw two separate stems, then connect their intermediate sites with a bar.",
        "Connection is structural; direction of permitted use is an additional relation.",
        [P("upper-left", 0, 2), P("left", 0, 1), P("lower-left", 0, 0), P("upper-right", 3, 2), P("right", 3, 1), P("lower-right", 3, 0)],
        [L("left-upper", "upper-left", "left", 0), L("left-lower", "left", "lower-left", 0),
            L("right-upper", "upper-right", "right", 1), L("right-lower", "right", "lower-right", 1), L("bridge", "left", "right", 2)],
        twoWay ? [R("outward", "left", "right"), R("return", "right", "left")] : [R("outward", "left", "right")], ["run"],
        twoWay ? [new("left", "run", ["outward"]), new("right", "run", ["return"])] : [new("left", "run", ["outward"])]);

    public static Fixture Cross(bool shared)
    {
        var points = new List<GlyphJointUsagePoint> { P("west", -2, 0), P("east", 2, 0), P("south", 0, -2), P("north", 0, 2), P("middle-h", 0, 0) };
        if (!shared) points.Add(P("middle-v", 0, 0));
        var vertical = shared ? "middle-h" : "middle-v";
        return Build(shared ? "x-joined" : "x-crossing", shared ? "Cross as a shared turning junction" : "Cross as independent passages", "X / +",
            shared ? "The horizontal and vertical strokes share their actual middle site." : "Two strokes have coincident middle coordinates but different actual sites.",
            shared ? "A connected turning route changes the output direction." : "Geometric intersection does not create a material connection.", points.ToArray(),
            [L("west-half", "west", "middle-h", 0), L("east-half", "middle-h", "east", 0),
                L("south-half", "south", vertical, 1), L("north-half", vertical, "north", 1)],
            shared ? [R("turn", "west", "north", "middle-h")] : [R("horizontal", "west", "east", "middle-h"), R("vertical", "south", "north", vertical)], ["run"],
            shared ? [new("west", "run", ["turn"])] : [new("west", "run", ["horizontal"]), new("south", "run", ["vertical"])]);
    }

    private static Fixture Build(string id, string title, string glyph, string construction, string use,
        GlyphJointUsagePoint[] points, GlyphJointUsageLine[] lines, GlyphJointUsageRoute[] routes,
        string[] controlNames, Rule[] rules, Dictionary<string, int>? capacityOverrides = null)
    {
        var sites = points.ToDictionary(point => point.Id, _ => (IElement)new Placeholder());
        var ports = points.ToDictionary(point => point.Id, point => new Relation(sites[point.Id], new Placeholder()));
        var edges = lines.ToDictionary(line => line.Id, line => new Relation(sites[line.From], sites[line.To]));
        var actualRoutes = routes.ToDictionary(route => route.Id, route => new Relation(ports[route.From], ports[route.To]));
        var controls = controlNames.ToDictionary(name => name, _ => (IElement)new Placeholder());
        Relation Oriented(GlyphJointUsageLine line, string from, string to) => new(edges[line.Id], new Relation(sites[from], sites[to]));
        var paths = routes.Select(route =>
        {
            var names = new[] { route.From }.Concat(route.Via).Append(route.To).ToArray();
            var uses = names.Zip(names.Skip(1)).Select(pair =>
            {
                var edge = lines.Single(line => line.From == pair.First && line.To == pair.Second || line.From == pair.Second && line.To == pair.First);
                return (IElement)Oriented(edge, pair.First, pair.Second);
            }).ToArray();
            return (IElement)new Relation(actualRoutes[route.Id], S(uses));
        }).ToArray();
        var history = S(lines.GroupBy(line => line.Stroke).OrderBy(group => group.Key)
            .Select(group => (IElement)S(group.Select(line => (IElement)Oriented(line, line.From, line.To)).ToArray())).ToArray());
        var portScope = S(ports.Values.Cast<IElement>().ToArray());
        var law = FiniteLaw.Create(portScope, S(controls.Values.ToArray()), S(actualRoutes.Values.Cast<IElement>().ToArray()),
            rules.Select(rule => FiniteLaw.Row(ports[rule.Input], controls[rule.Control],
                rule.Open ? StructuralConstraint.Open(rule.Routes.Select(name => (IElement)actualRoutes[name]).ToArray()) : S(rule.Routes.Select(name => (IElement)actualRoutes[name]).ToArray()))).ToArray());
        var capacities = S(points.Select(point => (IElement)new Relation(ports[point.Id],
            S(Enumerable.Range(0, capacityOverrides?.GetValueOrDefault(point.Id, 2) ?? 2).Select(_ => (IElement)new Placeholder()).ToArray()))).ToArray());
        var model = new GlyphJointUsage(S(edges.Values.Cast<IElement>().ToArray()), portScope, S(paths), history, capacities, law);
        if (!GlyphJointUsage.TryRead(model.Surface, out _)) throw new InvalidOperationException($"Disconnected authored fixture: {id}");
        return new(model, ports, controls, actualRoutes, new(id, title, glyph, construction, use,
            "This fixture demonstrates connected permissions and packet accounting. It does not infer handwriting, mechanical stiffness, timing, or a physical load law from the outline.",
            points, lines, routes, [], E(history).Length));
    }

    private static GlyphJointUsagePoint P(string id, double x, double y) => new(id, id.Replace('-', ' '), x, y);
    private static GlyphJointUsageLine L(string id, string from, string to, int stroke) => new(id, from, to, stroke);
    private static GlyphJointUsageRoute R(string id, string from, string to, params string[] via) => new(id, from, to, via);
}
