using Gojo.Core;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>Authored construction library and operational route specifications.</summary>
public sealed class CoupledRecipes
{
    public ConstructionRecipes Space { get; } = new();
    public Carrier ShapeX { get; } = Program.Parameter(40);
    public Carrier Width { get; } = Program.Parameter(120);
    public Carrier Height { get; } = Program.Parameter(80);
    public Carrier RoofRise { get; } = Program.Parameter(0);
    public Carrier Centre { get; } = Program.Parameter(100);
    public Carrier HalfSpan { get; } = Program.Parameter(60);
    public Carrier HubHeight { get; } = Program.Parameter(100);
    public Placeholder[] External { get; } = Enumerable.Range(0, 4).Select(_ => new Placeholder()).ToArray();
    public Placeholder[] Nodes { get; } = Enumerable.Range(0, 4).Select(_ => new Placeholder()).ToArray();
    private readonly Relation[] edges;

    public CoupledRecipes() => edges = [new(Nodes[3], Nodes[0]), new(Nodes[3], Nodes[1]), new(Nodes[3], Nodes[2])];

    public Relation Outline(Carrier x, Carrier width, Carrier height, Carrier rise)
    {
        var origin = Space.Plus(Space.Point(0, 20), Space.Times(Space.Point(1, 0), x.ValueSlot, x));
        var across = Space.Times(Space.Point(1, 0), width.ValueSlot, width);
        var upright = Space.Times(Space.Point(0, 1), height.ValueSlot, height);
        var a = origin; var b = Space.Plus(a, across); var d = Space.Plus(a, upright); var c = Space.Plus(b, upright);
        var riseVector = Space.Times(Space.Point(0, 1), rise.ValueSlot, rise);
        var peak = Space.Plus(Space.Between(d, c, 1, 2), riseVector);
        var ground = Space.Between(a, b, 1, 2);
        // The final output is a synthetic span read, not another drawn vertex.
        var walls = G(new Relation(a, b), new Relation(b, c), new Relation(c, d), new Relation(d, a));
        var roof = G(new Relation(d, peak), new Relation(peak, c));
        var readSurfaces = G(new Relation(new Relation(a, b), ground),
            new Relation(new Relation(Space.Between(d, c, 1, 2), peak), riseVector));
        return Direct(G(x, width, height, rise), G(a, b, c, d, peak, ground, riseVector), G(walls, roof, readSurfaces));
    }

    public Relation Joint(Carrier? displacement = null, long direction = -1, bool support = true)
    {
        var x = Space.Times(Space.Point(1, 0), Centre.ValueSlot, Centre);
        var hub = Space.Plus(x, Space.Times(Space.Point(0, 1), HubHeight.ValueSlot, HubHeight));
        var half = Space.Times(Space.Point(1, 0), HalfSpan.ValueSlot, HalfSpan);
        var shift = displacement is null ? Space.Zero : Space.Times(Space.Times(Space.Point(0, 1), displacement.ValueSlot, displacement), direction);
        var left = Space.Plus(Space.Plus(hub, Space.Times(half, -1)), shift);
        var right = Space.Plus(Space.Plus(hub, half), shift);
        var foot = Space.Plus(x, Space.Point(0, 20));
        IElement[] points = [left, right, foot, hub];
        var machine = Direct(displacement is null ? G(Centre, HalfSpan, HubHeight) : G(Centre, HalfSpan, HubHeight, displacement), G(points));
        return JointGraph.Create(machine, G(points.Select((p, i) => (IElement)new Relation(Nodes[i], p)).ToArray()),
            G((support ? edges : edges.Take(2)).Cast<IElement>().ToArray()),
            G(External.Select((role, i) => (IElement)new Relation(role, Nodes[i])).ToArray()), G(Space.Basis, Space.Metric));
    }

    public Relation JointRules(Carrier? displacement = null, long direction = -1, long step = 4)
    {
        var x = Space.X; var y = Space.Y;
        var rows = new List<IElement>
        {
            Rule(External[0], x, HalfSpan, -1, 8, 20, 140, step), Rule(External[1], x, HalfSpan, 1, 8, 20, 140, step),
            Rule(External[2], x, Centre, 1, 8, 1, 300, step), Rule(External[3], x, Centre, 1, 8, 1, 300, step),
            Rule(External[3], y, HubHeight, 1, displacement is null ? 8 : 4, 30, 260, step)
        };
        foreach (var role in External.Take(2)) rows.Add(displacement is null
            ? Rule(role, y, HubHeight, 1, 8, 30, 260, step)
            : Rule(role, y, displacement, direction, 8, 0, 120, step));
        return G(rows.ToArray());
    }

    public Relation Proposal(long direction, bool support, long step = 4)
    {
        var displacement = Program.Parameter(0);
        var definition = Joint(displacement, direction, support);
        var nodeMap = JointRecipes.Carry(Nodes);
        var edgeMap = G(edges.Select((edge, i) => (IElement)new Relation(edge, support || i < 2 ? edge : new Placeholder())).ToArray());
        return G(definition, nodeMap, edgeMap, JointRules(displacement, direction, step));
    }

    public void BindOutline(NegotiatingMachine machine, long step)
    {
        foreach (var i in new[] { 0, 3, 4, 5 }) machine.AddResponse(machine.Port(i, 0), ShapeX, responseD: 8, min: 1, max: 240, maxStep: step, quantum: 2);
        foreach (var i in new[] { 1, 2 }) machine.AddResponse(machine.Port(i, 0), Width, responseD: 8, min: 40, max: 280, maxStep: step, quantum: 2);
        foreach (var i in new[] { 2, 3 }) machine.AddResponse(machine.Port(i, 1), Height, responseD: 8, min: 40, max: 220, maxStep: step);
        foreach (var i in new[] { 4, 6 }) machine.AddResponse(machine.Port(i, 1), RoofRise, responseD: 8, min: 0, max: 120, maxStep: step);
    }

    private Relation Direct(Relation inputs, Relation outputs, IElement? incidence = null) => TransformSurface.Create(new Carrier(), inputs, outputs,
        G(F(outputs).Select(_ => (IElement)new Placeholder()).ToArray()), G(), G(Space.Basis, Space.Metric, incidence ?? G()));

    // G(external role, public-to-actuator displacement calibration, response
    // fraction, G(permitted interval, step span, quantum)). No concept labels.
    private static Relation Rule(IElement role, Carrier axis, Carrier actuator, long gain, long denominator, long min, long max, long step) =>
        G(role, new Relation(Relation.Segment(axis, 0, 1), Relation.Segment(actuator, 0, gain)),
            ProjectionResidue.Create(new Carrier(), 1, denominator),
            G(Relation.Segment(actuator, min, max), Relation.Segment(actuator, -step, step), Relation.Segment(actuator, 0, 1)));

    public static Relation BindRules(JointAssembly joint, Relation specifications)
    {
        var correspondence = new List<IElement>();
        foreach (var rule in F(specifications).Cast<Relation>())
        {
            var f = F(rule); var calibration = (Relation)f[1];
            TimeStepSequence.TryReadSpan((Relation)calibration.Left, out var a, out var b);
            TimeStepSequence.TryReadSpan((Relation)calibration.Right, out var c, out var d);
            var portPair = (Relation)F(joint.PortMap).Cast<Relation>().Single(binding => ReferenceEquals(binding.Left, f[0])).Right;
            var port = F(portPair).Cast<Relation>().Single(p => NegotiatingMachine.PortAxis(p).SameIdentityAs(a.Carrier));
            ProjectionResidue.TryRead(f[2], out _, out var n, out var denominator);
            var cap = F(f[3]);
            TimeStepSequence.TryReadSpan((Relation)cap[0], out var min, out var max);
            TimeStepSequence.TryReadSpan((Relation)cap[1], out _, out var step);
            TimeStepSequence.TryReadSpan((Relation)cap[2], out _, out var quantum);
            var route = joint.Mechanical.AddResponse(port, c.Carrier, d.Ticks - c.Ticks, b.Ticks - a.Ticks,
                (long)n, (long)denominator, min.Ticks, max.Ticks, step.Ticks, quantum.Ticks);
            correspondence.Add(new Relation(rule, route));
        }
        return G(correspondence.ToArray());
    }
}
