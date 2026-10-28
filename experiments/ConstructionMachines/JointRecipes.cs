using Gojo.Core;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>Authored examples. Joint names select builders, never execution laws.</summary>
public sealed class JointRecipes
{
    public ConstructionRecipes Space { get; } = new();
    public Carrier Reach { get; } = Program.Parameter(80);
    public Placeholder[] External { get; } = Enumerable.Range(0, 4).Select(_ => new Placeholder()).ToArray();
    public Placeholder Hub { get; } = new();
    public Placeholder Left { get; } = new();
    public Placeholder Right { get; } = new();
    public Placeholder Foot { get; } = new();
    public Placeholder Upper { get; } = new();
    public Placeholder LeftHub { get; } = new();
    public Placeholder RightHub { get; } = new();
    public Placeholder LeftFoot { get; } = new();
    public Placeholder RightFoot { get; } = new();
    private readonly Relation leftArm, rightArm, stem, upperArm;
    private readonly Relation leftCornerArm, rightCornerArm, leftLeg, rightLeg;

    public JointRecipes()
    {
        leftArm = new(Hub, Left); rightArm = new(Hub, Right); stem = new(Hub, Foot); upperArm = new(Hub, Upper);
        leftCornerArm = new(LeftHub, Left); rightCornerArm = new(RightHub, Right);
        leftLeg = new(LeftHub, LeftFoot); rightLeg = new(RightHub, RightFoot);
    }

    private (IElement Hub, IElement Left, IElement Right, IElement Foot, IElement Upper) Motif()
    {
        var hub = Space.Point(100, 100);
        var across = Space.Times(Space.Point(1, 0), Reach.ValueSlot, Reach);
        var upright = Turn(Space.QuarterTurn, across);
        return (hub, Space.Plus(hub, Space.Times(across, -1)), Space.Plus(hub, across),
            Space.Plus(hub, Space.Times(upright, -1)), Space.Plus(hub, upright));
    }

    public Relation Tee()
    {
        var p = Motif();
        return Create(G(Reach), [(Hub, p.Hub), (Left, p.Left), (Right, p.Right), (Foot, p.Foot)],
            [leftArm, rightArm, stem], [Left, Right, Foot, Foot]);
    }

    public Relation Fan(Carrier rise)
    {
        var p = Motif(); var dy = Space.Times(Space.Point(0, 1), rise.ValueSlot, rise);
        return Create(G(Reach, rise), [(Hub, p.Hub),
            (Left, Space.Plus(p.Left, dy)), (Right, Space.Plus(p.Right, dy)), (Foot, p.Foot)],
            [leftArm, rightArm, stem], [Left, Right, Foot, Foot]);
    }

    public Relation Corners(Carrier spread)
    {
        var p = Motif(); var dx = Space.Times(Space.Point(1, 0), spread.ValueSlot, spread);
        var opposed = Space.Times(dx, -1);
        return Create(G(Reach, spread), [(Left, p.Left), (Right, p.Right),
            (LeftHub, Space.Plus(p.Hub, opposed)), (RightHub, Space.Plus(p.Hub, dx)),
            (LeftFoot, Space.Plus(p.Foot, opposed)), (RightFoot, Space.Plus(p.Foot, dx))],
            [leftCornerArm, rightCornerArm, leftLeg, rightLeg], [Left, Right, LeftFoot, RightFoot]);
    }

    public Relation Cross(bool upperConnected = true)
    {
        var p = Motif();
        return Create(G(Reach), [(Hub, p.Hub), (Left, p.Left), (Right, p.Right), (Foot, p.Foot), (Upper, p.Upper)],
            upperConnected ? [leftArm, rightArm, stem, upperArm] : [leftArm, rightArm, stem], [Left, Right, Foot, Upper]);
    }

    public Relation TeeToCornersNodes() => G(new Relation(Left, Left), new Relation(Right, Right),
        TransformSurface.Split(Hub, G(LeftHub, RightHub)), TransformSurface.Split(Foot, G(LeftFoot, RightFoot)));
    public Relation TeeToCornersEdges() => G(new Relation(leftArm, leftCornerArm), new Relation(rightArm, rightCornerArm),
        TransformSurface.Split(stem, G(leftLeg, rightLeg)));
    public Relation CornersToTeeNodes() => G(new Relation(Left, Left), new Relation(Right, Right),
        TransformSurface.Fold(G(LeftHub, RightHub), Hub), TransformSurface.Fold(G(LeftFoot, RightFoot), Foot));
    public Relation CornersToTeeEdges() => G(new Relation(leftCornerArm, leftArm), new Relation(rightCornerArm, rightArm),
        TransformSurface.Fold(G(leftLeg, rightLeg), stem));
    public static Relation Carry(IEnumerable<IElement> participants) => G(participants.Select(p => (IElement)new Relation(p, p)).ToArray());

    public Relation CrossEdgeChange(bool add) => add
        ? G(new Relation(leftArm, leftArm), new Relation(rightArm, rightArm), new Relation(stem, stem), new Relation(new Placeholder(), upperArm))
        : G(new Relation(leftArm, leftArm), new Relation(rightArm, rightArm), new Relation(stem, stem), new Relation(upperArm, new Placeholder()));

    private Relation Create(Relation inputs, (IElement Role, IElement Expression)[] nodes, Relation[] edges, IElement[] exposed)
    {
        var bindings = G(nodes.Select(n => (IElement)new Relation(n.Role, n.Expression)).ToArray());
        var outputExpressions = exposed.Select(role => nodes.Single(n => ReferenceEquals(n.Role, role)).Expression).ToArray();
        var machine = TransformSurface.Create(new Carrier(), inputs, G(outputExpressions),
            G(exposed.Select(_ => (IElement)new Placeholder()).ToArray()), G(), G(Space.Basis, Space.Metric));
        var exposure = G(exposed.Select((role, i) => (IElement)new Relation(External[i], role)).ToArray());
        return JointGraph.Create(machine, bindings, G(edges), exposure, G(Space.Basis, Space.Metric));
    }

    public NegotiatingMachine Target(params (long X, long Y)[] points)
    {
        if (points.Length != External.Length) throw new ArgumentException("Supply one target per exposed role.");
        return new NegotiatingMachine(TransformSurface.Create(new Carrier(), G(), G(points.Select(p => (IElement)Space.Point(p.X, p.Y)).ToArray()),
            G(points.Select(_ => (IElement)new Placeholder()).ToArray()), G(), G(Space.Basis, Space.Metric)));
    }
}
