using Gojo.Core;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

public static class EnsembleRecipes
{
    // These are display/setup selectors only. The executor consumes the native
    // filter equations, shares, capacities, intervals and actual port references.
    public static readonly long[][] Shares = [[6, 0, 0], [0, 6, 0], [0, 0, 6], [2, 2, 2], [2, 2, 2]];
    public static readonly string[] MethodNames = ["Direct projection", "Bounded steps", "Trend-aware response", "Equal blend", "Blend + local negotiation"];
    public sealed record Scene(EnsembleSettlement Model, string[] ObligationNames, long[] Reference,
        Relation Initial, int Seed, string Condition);

    public static Scene Create(int method = 3, int seed = 0, long reviewBudget = 2, string condition = "ordinary")
    {
        var recipes = new CoupledRecipes(); var s = recipes.Space;
        var wallX = Program.Parameter(54 + seed % 4 * 4); var width = Program.Parameter(116 + seed % 3 * 8);
        var height = Program.Parameter(80); var doorX = Program.Parameter(174 + seed % 3 * 4);
        var goalLeft = 86 + seed % 4 * 6; var goalRight = goalLeft + 160 + seed % 3 * 8; var goalTop = 120 + seed % 3 * 4;
        var goalPeak = goalTop + 40 + seed % 2 * 8; var goalDoor = (goalLeft + goalRight) / 2 - 12;
        recipes.Centre.Set(110L + seed % 3 * 4); recipes.HalfSpan.Set(90L + seed % 2 * 6); recipes.HubHeight.Set(112);
        if (condition == "quiet")
        {
            wallX.Set(60); width.Set(120); doorX.Set(108);
            recipes.Centre.Set(120); recipes.HalfSpan.Set(68);
            recipes.HubHeight.Set(100); goalLeft = 60; goalRight = 180; goalTop = 100; goalPeak = 100; goalDoor = 108;
        }
        var origin = s.Plus(s.Point(0, 20), s.Times(s.Point(1, 0), wallX.ValueSlot, wallX));
        var wall = new NegotiatingMachine(s.Offsets(origin, width, height));
        var doorOrigin = s.Plus(s.Point(0, 20), s.Times(s.Point(1, 0), doorX.ValueSlot, doorX));
        var opening = new NegotiatingMachine(s.Offsets(doorOrigin, Program.Parameter(24), Program.Parameter(36)));
        var joint = new JointAssembly(recipes.Joint(), G(new Relation(recipes.External[2], recipes.External[3])));
        var fixedAll = condition == "blocked"; var fixedWall = fixedAll || condition == "fixed-wall";
        foreach (var i in new[] { 0, 3 }) wall.AddResponse(wall.Port(i, 0), wallX, responseD: 4, min: 1, max: 260, maxStep: fixedWall ? 0 : 8, quantum: 2);
        foreach (var i in new[] { 1, 2 }) wall.AddResponse(wall.Port(i, 0), width, responseD: 4, min: 80, max: 280, maxStep: fixedWall ? 0 : 8, quantum: 2);
        wall.AddResponse(wall.Port(3, 1), height, responseD: 4, min: 50, max: 200, maxStep: fixedWall ? 0 : 8);
        foreach (var i in new[] { 0, 1 }) opening.AddResponse(opening.Port(i, 0), doorX, responseD: 4, min: 1, max: 360, maxStep: fixedAll ? 0 : 8);
        Relation Reweight(Relation rules) => G(F(rules).Select(rule =>
        {
            var f = F(rule); var cap = F(f[3]); TimeStepSequence.TryReadSpan((Relation)cap[1], out var span, out _);
            return (IElement)G(f[0], f[1], ProjectionResidue.Create(new Carrier(), 1, 4),
                fixedAll ? G(cap[0], Relation.Segment(span.Carrier, 0, 0), cap[2]) : f[3]);
        }).ToArray());
        CoupledRecipes.BindRules(joint, Reweight(recipes.JointRules(step: 8)));
        Relation Proposal(long direction, bool support)
        {
            var f = F(recipes.Proposal(direction, support, 8)); return G(f[0], f[1], f[2], Reweight((Relation)f[3]));
        }
        var proposals = condition == "unsuitable" ? G(Proposal(1, true), Proposal(-1, false))
            : G(Proposal(-1, true), Proposal(1, true), Proposal(-1, false));
        var shares = Shares[method]; var rows = new List<(string Name, EvidenceObligation Obligation)>();
        void Add(string name, long lo, long hi, params (Relation Port, long Coefficient)[] terms) =>
            rows.Add((name, new EvidenceObligation(terms, lo, hi, shares)));
        var roof = joint.Mechanical;
        Add("Left permitted overhang", -8, 4096, (roof.Port(0, 0), 1), (wall.Port(0, 0), -1));
        Add("Right permitted overhang", -8, 4096, (wall.Port(1, 0), 1), (roof.Port(1, 0), -1));
        Add("Left eave attachment", -2, 2, (roof.Port(0, 1), 1), (wall.Port(3, 1), -1));
        Add("Right eave attachment", -2, 2, (roof.Port(1, 1), 1), (wall.Port(3, 1), -1));
        Add("Roof centred on wall", -6, 6, (roof.Port(3, 0), 2), (wall.Port(0, 0), -1), (wall.Port(1, 0), -1));
        Add("Roof span observation", 128, 156, (roof.Port(1, 0), 1), (roof.Port(0, 0), -1));
        Add("Ridge height observation", goalPeak - 4, goalPeak + 4, (roof.Port(3, 1), 1));
        Add("Wall left observation", goalLeft - 4, goalLeft + 4, (wall.Port(0, 0), 1));
        Add("Wall right observation", goalRight - 4, goalRight + 4, (wall.Port(1, 0), 1));
        Add("Wall top observation", goalTop - 2, goalTop + 2, (wall.Port(3, 1), 1));
        Add("Opening inside left", 8, 4096, (opening.Port(0, 0), 1), (wall.Port(0, 0), -1));
        Add("Opening inside right", 8, 4096, (wall.Port(1, 0), 1), (opening.Port(1, 0), -1));
        Add("Opening centred on wall", -10, 10, (opening.Port(0, 0), 1), (opening.Port(1, 0), 1), (wall.Port(0, 0), -1), (wall.Port(1, 0), -1));
        Add("Opening location observation", goalDoor - 8, goalDoor + 8, (opening.Port(0, 0), 1));
        if (condition == "outlier") Add("Conflicting opening opinion", goalRight + 80, goalRight + 88, (opening.Port(0, 0), 1));
        if (condition is "pooled-copies" or "unpooled-copies")
        {
            var key = new Placeholder();
            for (var copy = 0; copy < 5; copy++) rows.Add(($"Repeated opening opinion {copy + 1}",
                new EvidenceObligation([(opening.Port(0, 0), 1)], goalRight + 80, goalRight + 88, shares, support: key)));
        }
        if (condition == "unknown")
        {
            var pending = new EvidenceObligation([(opening.Port(0, 0), 1)], 0, 1, shares); var f = F(pending.Surface);
            rows.Add(("Unbound applicability / permitted region", EvidenceObligation.Restore(G(f[0], f[1],
                new Relation(Relation.Point(pending.Axis, 0), new Placeholder()), f[3], f[4], f[5]))));
        }
        var active = condition == "unpooled-copies" ? rows.Select(r => r.Obligation)
            : rows.Select(r => r.Obligation).DistinctBy(o => o.Support, ReferenceEqualityComparer.Instance);
        var model = new EnsembleSettlement([wall, roof, opening], joint, rows.Select(r => r.Obligation).ToArray(),
            G(active.Select(o => (IElement)o.Surface).ToArray()), proposals, fixedAll ? 0 : reviewBudget,
            negotiationPassages: method == 4 ? G(new[] { wall, roof, opening }.SelectMany(m => m.Ports)
                .Select(p => (IElement)GuardedPassage(p)).ToArray()) : G());
        return new Scene(model, active.Select(o => rows.First(r => ReferenceEquals(r.Obligation, o)).Name).ToArray(),
            [goalLeft, goalRight, goalTop, goalPeak, goalDoor], (Relation)Program.CaptureObservation(model.Surface), seed, condition);
    }

    // A conjunction of occupied direction groups. Each ordered direction frame
    // has its own required count interval; the guard is not a hidden mode flag.
    private static Relation GuardedPassage(Relation port)
    {
        var axis = NegotiatingMachine.PortAxis(port);
        return G(port, Relation.Segment(axis, -12, 12), G(
            new Relation(Relation.Segment(axis, 0, 1), Relation.Segment(new Carrier(), 1, 4096)),
            new Relation(Relation.Segment(axis, 0, -1), Relation.Segment(new Carrier(), 1, 4096))));
    }
}
